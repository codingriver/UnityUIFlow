# IMGUI 自动化测试方案设计

版本：1.0
日期：2026-04-17
状态：方案设计（待评审）

---

## 1. 设计前提

IMGUI 与 UIToolkit 是 Unity 的两套独立 UI 系统，架构差异决定了 IMGUI 无法通过 UIToolkit 的 `VisualElement` 树进行自动化。本方案的核心思路是：**不为 IMGUI 构建虚假的 VisualElement 映射，而是建立一套与 UIToolkit 平行的 IMGUI 专用定位-执行子系统**。

方案遵循以下原则：
- 不修改 Unity 引擎源码。
- 尽量使用反射访问 IMGUI 内部状态，而非像素级图像识别。
- 要求被测 IMGUI 代码做**最小可测试性改造**（如设置 ControlName、暴露关键字段）。
- IMGUI 动作与 UIToolkit 动作在 YAML 中通过前缀区分，不混用选择器语法。

---

## 2. 技术方案架构

```
YAML 用例
    │
    ├─ uitk::click       → 原有 UIToolkit 路径（VisualElement + ElementFinder）
    ├─ imgui::click      → IMGUI 路径（ImguiLocator + 反射/坐标）
    ├─ imgui::type       → IMGUI 路径
    └─ imgui::assert     → IMGUI 路径

IMGUI 专用子系统
    │
    ├── ImguiSelectorCompiler     ← 编译 YAML 选择器为 ImguiSelector
    │   语法：gui(button, text="OK")
    │        gui(textfield, index=2)
    │        gui(group="Settings")
    │
    ├── ImguiElementLocator       ← 在 OnGUI 执行后捕获布局快照
    │   输入：ImguiSelector
    │   输出：GUILayoutEntry[]（含 rect、type、text 等元数据）
    │
    ├── ImguiActionRegistry       ← 注册 imgui_* 前缀动作
    │   imgui_click、imgui_type、imgui_assert_text、
    │   imgui_assert_value、imgui_focus、imgui_wait
    │
    ├── ImguiExecutionBridge      ← 注入 OnGUI 钩子
    │   在每帧 EditorApplication.update 中：
    │   1. 标记"需要执行自动化指令"
    │   2. 触发窗口 Repaint（强制 OnGUI 执行）
    │   3. OnGUI 内钩子读取 GUILayoutUtility 快照
    │   4. 根据快照执行点击/输入
    │   5. 再次 Repaint 让 IMGUI 响应状态变化
    │
    └── ImguiScreenDriver         ← OS 级输入兜底
        当反射定位失败时，回退到屏幕坐标点击（SendInput）
```

---

## 3. 核心模块设计

### 3.1 IMGUI 选择器语法（ImguiSelector）

IMGUI 控件没有 `name` 属性，选择器基于**控件类型 + 匹配条件**：

| 选择器示例 | 含义 |
|-----------|------|
| `gui(button)` | 匹配第一个 Button |
| `gui(button, text="Save")` | 匹配文本为"Save"的 Button |
| `gui(button, index=3)` | 匹配第 4 个 Button（0-based） |
| `gui(textfield, index=0)` | 匹配第一个 TextField |
| `gui(toggle, text="Enabled")` | 匹配 label 为"Enabled"的 Toggle |
| `gui(group="Settings")` | 匹配名为 Settings 的 GUILayout 组 |
| `gui(group="Settings" > button, text="Apply")` | 在 Settings 组内匹配 Apply 按钮 |
| `gui(focused)` | 匹配当前获得焦点的控件 |

**YAML 中使用方式**：

```yaml
steps:
  # UIToolkit 动作（前缀 uitk::，可省略）
  - click: { selector: "#submit-button" }

  # IMGUI 动作（前缀 imgui::，必须显式声明）
  - imgui::click: { selector: "gui(button, text=\"OK\")" }
  - imgui::type: { selector: "gui(textfield, index=0)", text: "admin" }
  - imgui::assert_text: { selector: "gui(label, index=2)", text: "Saved" }
```

### 3.2 布局快照捕获（ImguiElementLocator）

Unity 的 `GUILayoutUtility` 内部维护了当前绘制批次的布局状态。通过反射可获取：

```csharp
// 反射入口
var topLevel = typeof(GUILayoutUtility).GetField("current", BindingFlags.NonPublic | BindingFlags.Static)
    ?.GetValue(null);

var group = topLevel?.GetType().GetField("topLevel", BindingFlags.NonPublic | BindingFlags.Instance)
    ?.GetValue(topLevel);

var entries = group?.GetType().GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
    ?.GetValue(group) as IList;

// 每个 entry 包含：
// - rect: Rect（控件在窗口内的位置）
// - minWidth, maxWidth, minHeight, maxHeight
// - style: GUIStyle（可读取 name 判断控件类型）
```

**捕获时机**：在 `OnGUI` 执行完毕后立即捕获。通过 `IMGUIContainer` 的 `onGUIHandler` 注入后处理：

```csharp
// 在被测窗口的 IMGUIContainer 上注入钩子
var imguiContainer = window.rootVisualElement.Q<IMGUIContainer>();
if (imguiContainer != null)
{
    var originalHandler = imguiContainer.onGUIHandler;
    imguiContainer.onGUIHandler = () =>
    {
        originalHandler?.Invoke();
        // OnGUI 执行完毕后，立即捕获快照
        ImguiSnapshot.CaptureFromCurrentGUILayoutBatch();
    };
}
```

**控件类型识别**：通过 `GUIStyle.name` 推断控件类型：

| GUIStyle.name | 推断类型 |
|--------------|---------|
| `Button` | button |
| `textField` / `TextField` | textfield |
| `toggle` | toggle |
| `label` / `Label` | label |
| `popup` / `Popup` | dropdown |
| `MiniPullDown` | toolbar_dropdown |
| `box` / `Box` | group |

### 3.3 IMGUI 动作实现（ImguiActionRegistry）

所有 `imgui_*` 动作基于**布局快照**执行，不依赖 VisualElement。

#### imgui::click

```csharp
public async Task ExecuteAsync(...)
{
    var entry = ImguiLocator.Find(selector);
    if (entry == null) throw new ElementNotFoundException();

    Vector2 center = entry.Rect.center;
    // 将窗口局部坐标转换为屏幕坐标
    Vector2 screenPos = EditorGUIUtility.GUIToScreenPoint(center);

    // 方式 A：通过 Event 队列注入（优先）
    var evtDown = new Event { type = EventType.MouseDown, button = 0, mousePosition = center };
    var evtUp = new Event { type = EventType.MouseUp, button = 0, mousePosition = center };
    window.SendEvent(evtDown);
    await Task.Delay(50);
    window.SendEvent(evtUp);

    // 方式 B：OS 级鼠标事件（兜底，当 Event 注入失效时）
    // SendMouseInput(screenPos, MouseButton.Left, click: true);
}
```

#### imgui::type

```csharp
public async Task ExecuteAsync(...)
{
    var entry = ImguiLocator.Find(selector);
    if (entry == null) throw new ElementNotFoundException();

    // 先点击获取焦点
    await ClickAsync(entry.Rect.center);
    await Task.Delay(100);

    // 方式 A：通过 EditorGUI.FocusedControl + 反射设置值（TextField 专用）
    if (entry.InferredType == "textfield")
    {
        // 利用 SendKeys 或反射设置 GUIUtility.keyboardControl 关联的值
        // IMGUI TextField 没有公共值访问器，需反射 GUIView/EditorWindow 内部状态
        foreach (char c in text)
        {
            var evt = new Event { type = EventType.KeyDown, character = c };
            window.SendEvent(evt);
            await Task.Delay(10);
        }
    }

    // 方式 B：系统级键盘输入（兜底）
    // SendKeys(text);
}
```

#### imgui::assert_text

```csharp
public async Task ExecuteAsync(...)
{
    var entry = ImguiLocator.Find(selector);
    if (entry == null) throw new ElementNotFoundException();

    // IMGUI Label 的文本无法直接读取，需通过以下途径之一：
    // 1. 反射 GUILayoutEntry 的 text 字段（Unity 内部有存储）
    // 2. 截图 + OCR（兜底）
    // 3. 要求被测代码通过 GUI.SetNextControlName 暴露标识

    string actualText = entry.Text; // 反射获取
    if (actualText != expectedText)
        throw new AssertionException($"期望: {expectedText}, 实际: {actualText}");
}
```

#### imgui::wait

```csharp
public async Task ExecuteAsync(...)
{
    // 轮询：每帧捕获快照，直到选择器匹配到元素
    await Poller.WaitUntil(() => ImguiLocator.Find(selector) != null, timeoutMs);
}
```

### 3.4 执行桥接（ImguiExecutionBridge）

IMGUI 是帧驱动的，自动化指令必须在正确的帧时机执行：

```csharp
public sealed class ImguiExecutionBridge
{
    private Queue<ImguiCommand> _pendingCommands = new();
    private EditorWindow _targetWindow;

    public void Attach(EditorWindow window)
    {
        _targetWindow = window;
        var container = window.rootVisualElement.Q<IMGUIContainer>();
        if (container == null) return;

        var original = container.onGUIHandler;
        container.onGUIHandler = () =>
        {
            // 阶段 1：执行前置命令（如设置 FocusedControl）
            ExecutePreOnGuiCommands();

            // 阶段 2：执行原始 OnGUI
            original?.Invoke();

            // 阶段 3：捕获布局快照
            ImguiSnapshot.Capture();

            // 阶段 4：执行后置命令（如点击、断言）
            ExecutePostOnGuiCommands();
        };
    }

    public void Enqueue(ImguiCommand cmd) => _pendingCommands.Enqueue(cmd);

    // 触发重绘以推进自动化
    public void Repaint() => _targetWindow?.Repaint();
}
```

**关键时序**：

```
Frame N:   Enqueue(click cmd) → Repaint()
Frame N+1: OnGUI 开始 → PreCommands → 原始绘制 → Capture → PostCommands(click) → Repaint()
Frame N+2: OnGUI 开始 → IMGUI 响应点击状态 → Capture → Assert...
```

---

## 4. 覆盖范围清单

### 4.1 完全可覆盖（Tier 1）

以下 IMGUI 控件通过反射 `GUILayoutUtility` + `GUIStyle.name` 可稳定定位和交互：

| 控件 | 定位方式 | 动作支持 | 断言支持 | 风险等级 |
|------|---------|---------|---------|---------|
| `GUILayout.Button` | `gui(button)` / `gui(button, text="xxx")` | click, double_click, hover | assert_visible（通过 rect 存在性） | 低 |
| `EditorGUILayout.TextField` | `gui(textfield, index=N)` | type, focus, click | assert_visible | 低 |
| `GUILayout.Label` | `gui(label, index=N)` | — | assert_text（反射 text 字段） | 低 |
| `EditorGUILayout.Toggle` | `gui(toggle, text="xxx")` | click（切换） | assert_value（反射 internal state） | 低 |
| `EditorGUILayout.Popup` | `gui(dropdown, index=N)` | click（展开）、select_option（需二次定位选项） | assert_value | 中 |
| `GUILayout.Toolbar` | `gui(toolbar, index=N)` | click（选中 tab） | assert_value | 低 |
| `EditorGUILayout.Slider` | `gui(slider, index=N)` | drag（复杂，需计算 handle 位置） | assert_value | 中 |
| `GUILayout.BeginScrollView` | `gui(scroller)` | scroll（发送 WheelEvent） | assert_visible | 低 |
| `GUILayout.BeginHorizontal/Vertical` | `gui(group="xxx")` | — | — | 低（仅作为容器路径） |

### 4.2 部分可覆盖（Tier 2）

需要额外的反射技巧或屏幕坐标计算：

| 控件 | 问题 | 方案 | 风险等级 |
|------|------|------|---------|
| `EditorGUILayout.ObjectField` | 无公共 rect 细分（picker 按钮与字段在同一 GUILayoutEntry） | 通过 rect 子区域划分：左侧 80% 为字段，右侧 20% 为 picker 按钮 | 高（布局变化即失效） |
| `EditorGUILayout.EnumPopup` | 与 Popup 结构类似，但选项是动态枚举值 | 与 Popup 共用方案，但选项文本需枚举反射 | 中 |
| `EditorGUILayout.LayerField` / `TagField` | 与 Popup 结构类似 | 与 Popup 共用方案 | 中 |
| `GUILayout.Space` / `FlexibleSpace` | 无视觉实体，但作为布局路径的一部分 | 计入 index 路径 | 低 |
| `GUILayout.Foldout` | 可点击区域小（仅箭头） | 精确定位箭头 rect（通常左侧 16px） | 中 |
| `EditorGUILayout.InspectorTitlebar` | 复合控件（foldout + 帮助按钮 + 设置按钮） | 通过 rect 子区域划分 | 高 |

### 4.3 不可覆盖（Tier 3）

以下场景超出反射方案的能力边界，需借助屏幕坐标/OS 级输入，或完全无法自动化：

| 控件/场景 | 不可覆盖原因 | 变通方案 |
|----------|-------------|---------|
| `EditorGUILayout.PropertyField`（复杂类型） | 内部递归绘制大量子控件，GUILayoutEntry 是一整片区域，无法拆分 | 要求被测代码使用 UIToolkit 的 PropertyField 替代；或在 C# 中直接操作 SerializedProperty |
| `EditorGUI.DrawPreviewTexture` / `GUI.DrawTexture` | 纯绘制，无 GUILayoutEntry，无交互元数据 | 图像识别/OCR（超出框架范围） |
| `Handle`（SceneView 操作柄） | 在 SceneView 中通过 `Handles` 绘制，不在 EditorWindow 的 IMGUI 内 | 需独立的 SceneView 自动化方案 |
| `EditorWindow` 模态弹窗（如 `EditorUtility.DisplayDialog`） | 阻塞主线程，冻结 EditorApplication.update | OS 级自动化（SendKeys/点击） |
| 自定义 `Editor`（Inspector 绘制） | `OnInspectorGUI` 内部状态复杂，且 Inspector 是嵌入的 | 将自定义 Editor 重写为 `UIToolkit` 的 `PropertyDrawer` / `InspectorElement` |
| `GUI.Window` / `GUILayout.Window` | 浮动窗口，独立坐标系 | 捕获窗口坐标后递归定位（高复杂度） |
| 拖拽（Drag & Drop）到 IMGUI 区域 | IMGUI 的 drag 事件是全局 Event 队列，接收方需在 OnGUI 中手动检测 | OS 级鼠标拖拽 |
| 上下文菜单（`GenericMenu`） | 弹出层不在 GUILayout 树内，是独立 Overlay | 通过菜单项名称点击（需屏幕坐标） |
| 系统剪贴板 + IMGUI | `EditorGUIUtility.systemCopyBuffer` 可读写，但 Ctrl+V 的 IMGUI 响应依赖焦点 | C# 直接读写 buffer，跳过 UI |
| IME 中文输入 | 输入法候选框是 OS 级窗口 | 无法自动化，只能直接赋值 |

---

## 5. 可测试性改造要求

为了让 IMGUI 自动化更可靠，被测代码需要做以下最小改造：

### 改造 1：给关键控件设置 ControlName

```csharp
// 改造前
GUILayout.Button("Save");
var userName = EditorGUILayout.TextField("Username", "");

// 改造后（添加 GUI.SetNextControlName）
GUI.SetNextControlName("save-button");
GUILayout.Button("Save");

GUI.SetNextControlName("username-field");
var userName = EditorGUILayout.TextField("Username", "");
```

改造后 YAML 选择器可精确匹配：
```yaml
- imgui::click: { selector: "gui(button, control_name=\"save-button\")" }
- imgui::type: { selector: "gui(textfield, control_name=\"username-field\")" }
```

### 改造 2：给 GUILayout 组设置名称（用于路径导航）

```csharp
// 改造前
GUILayout.BeginVertical();
...
GUILayout.EndVertical();

// 改造后
GUILayout.BeginVertical("Settings");  // 使用 GUIStyle 名称作为组标识
...
GUILayout.EndVertical();
```

改造后选择器可限定范围：
```yaml
- imgui::click: { selector: "gui(group=\"Settings\" > button, text=\"Apply\")" }
```

### 改造 3：暴露关键状态字段（用于断言）

```csharp
public class MyToolWindow : EditorWindow
{
    // 改造：将私有状态改为内部可访问，或直接暴露只读属性
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public string _lastErrorMessage; // 自动化测试专用

    public bool HasError => !string.IsNullOrEmpty(_lastErrorMessage);
}
```

---

## 6. 实现工作清单

如果要在 UnityUIFlow 中落地 IMGUI 自动化支持，需要以下开发任务：

### Phase 1：基础设施（2 周）

| # | 任务 | 文件 | 工作量 |
|---|------|------|--------|
| 1 | 定义 `ImguiSelector` 语法模型 | `Core/UnityUIFlow.ImguiModels.cs` | 2d |
| 2 | 实现 `ImguiSelectorCompiler`（选择器解析） | `Parsing/UnityUIFlow.ImguiParsing.cs` | 3d |
| 3 | 实现 `ImguiSnapshot`（反射捕获 GUILayoutUtility 布局快照） | `Execution/UnityUIFlow.ImguiLocators.cs` | 4d |
| 4 | 实现 `ImguiElementLocator`（基于快照的查询引擎） | `Execution/UnityUIFlow.ImguiLocators.cs` | 3d |
| 5 | 实现 `ImguiExecutionBridge`（OnGUI 钩子注入） | `Execution/UnityUIFlow.ImguiBridge.cs` | 3d |

### Phase 2：动作实现（2 周）

| # | 任务 | 工作量 |
|---|------|--------|
| 6 | `imgui_click`：基于 rect 中心发送 Event | 2d |
| 7 | `imgui_type`：TextField 字符输入 + 系统键盘兜底 | 3d |
| 8 | `imgui_focus`：设置 GUIUtility.keyboardControl | 1d |
| 9 | `imgui_assert_text`：反射读取 GUILayoutEntry text | 2d |
| 10 | `imgui_assert_value`：反射读取 Toggle/Slider/Popup 内部值 | 3d |
| 11 | `imgui_assert_visible`：判断 rect 存在且尺寸 > 0 | 1d |
| 12 | `imgui_wait`：轮询等待快照中出现目标 | 1d |
| 13 | `imgui_scroll`：发送 WheelEvent 到窗口 | 1d |
| 14 | `imgui_select_option`：Popup/Dropdown 选项展开 + 点击 | 3d |
| 15 | `imgui_screenshot`：基于窗口 rect 截屏 | 1d |

### Phase 3：集成与兜底（1 周）

| # | 任务 | 工作量 |
|---|------|--------|
| 16 | `ActionRegistry` 支持 `imgui_*` 前缀路由 | 2d |
| 17 | `StepExecutor` 识别 IMGUI 动作并绕过 VisualElement | 2d |
| 18 | `TestRunnerWindow` 高亮支持 IMGUI rect | 2d |
| 19 | OS 级输入兜底（`SendInput` / `SendKeys`） | 2d |
| 20 | 单元测试（快照捕获、选择器匹配、动作执行） | 3d |

**总计：约 5 周（1 人全职）**

---

## 7. 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| Unity 版本升级破坏 `GUILayoutUtility` 内部反射 | 高 | 封装反射访问层，版本适配集中处理；单元测试尽早暴露兼容性问题 |
| 窗口尺寸变化导致 IMGUI 布局重排 | 高 | 要求测试在固定分辨率下运行；使用 ControlName 而非 index 定位 |
| IMGUI 控件的 `GUIStyle.name` 在不同皮肤/主题下变化 | 中 | 建立控件类型推断映射表，支持多套 style name |
| Event 注入被 IMGUI 控件忽略（某些控件只响应原生输入） | 中 | 提供 OS 级输入兜底；记录哪些控件需要兜底 |
| 性能：每帧反射捕获快照导致 Editor 卡顿 | 低 | 仅在自动化执行期间启用快照；缓存反射委托 |

---

## 8. 建议的实施策略

### 策略 A：全量实现（适合 IMGUI 工具占主力的大型团队）
- 投入 5 周开发上述完整方案。
- 要求被测代码做可测试性改造（ControlName、组名、状态暴露）。
- 适用于有大量遗留 IMGUI 工具且短期无法迁移到 UIToolkit 的团队。

### 策略 B：最小可用（MVP，适合只需要覆盖核心按钮/输入的团队）
- 只实现 Phase 1 的 `ImguiSnapshot` + `imgui_click` + `imgui_type` + `imgui_assert_text`。
- 只支持通过 `index` 和 `text` 定位，不支持 ControlName。
- 投入约 **1.5-2 周**。
- 覆盖 IMGUI 中最常见的按钮点击、文本输入、标签断言。

### 策略 C：绕过 UI 层（适合业务逻辑与 UI 解耦良好的团队）
- 不实现 IMGUI 自动化。
- 要求开发团队将 IMGUI 窗口的业务逻辑抽离到独立 Service 类。
- C# Fixture 直接调用 Service 方法，绕过 UI 绘制。
- 投入最小（改造现有代码），但无法验证 UI 布局正确性。

### 策略 D：迁移到 UIToolkit（推荐长期）
- 不投入 IMGUI 自动化开发。
- 将 IMGUI 工具逐步重写为 UXML + USS。
- 一次迁移，永久享受 UIToolkit 的完整自动化能力。
- 适合新项目和有重构资源的团队。

---

## 9. 总结

| 维度 | 评估 |
|------|------|
| **技术可行性** | ✅ 可行，基于反射 `GUILayoutUtility` + Event 注入 + OS 级兜底 |
| **实现成本** | 中等偏高（5 周全职开发） |
| **维护成本** | 高（Unity 版本升级可能破坏反射） |
| **覆盖范围** | 常见控件（Button、TextField、Label、Toggle、Popup、Slider）约 70-80%；复杂自定义绘制约 20-30% |
| **稳定性** | 低于 UIToolkit 自动化（依赖内部 API） |
| **推荐策略** | **短期用策略 B（MVP）或策略 C（绕过 UI）兜底，长期用策略 D（迁移 UIToolkit）** |

> **核心建议**：IMGUI 自动化是**技术债的止痛药，不是万能药**。如果你正准备开发新工具，直接用 UIToolkit；如果你有一堆 legacy IMGUI 工具需要冒烟测试，投入 2 周做个 MVP 能覆盖按钮和输入即可；如果你需要严格的 IMGUI UI 回归，要么接受高维护成本的反射方案，要么逐步迁移到 UIToolkit。
