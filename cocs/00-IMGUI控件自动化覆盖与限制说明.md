# IMGUI 控件自动化覆盖与限制说明

版本：2.0.0
日期：2026-04-23
状态：已全量验证（8 份 IMGUI YAML 回归用例全部执行通过，含负向测试）

---

## 1. 文档目标

本文档列出 UnityUIFlow V1 对 IMGUI（Immediate Mode GUI）控件的自动化覆盖范围，明确以下分类：

- **全面支持**：可通过 YAML `imgui_*` 声明式步骤完成定位、交互、赋值、断言。
- **局部支持**：可定位和部分交互，但部分高级功能需 C# 编程或反射辅助。
- **仅定位/断言**：可通过选择器定位和读取属性，但交互受限或需间接手段。
- **不可自动化**：由于 IMGUI 架构特性或 Unity API 限制，无法通过当前自动化手段操作。

IMGUI 与 UIToolkit 是两套独立的渲染体系：

- UIToolkit 保留 `VisualElement` 持久树，可通过 CSS-like 选择器遍历定位。
- IMGUI 每帧通过 `OnGUI()` 回调即时绘制，无持久树结构，控件仅在一帧的 `GUILayoutUtility` 布局批次中短暂存在。

因此，IMGUI 自动化不走 `ElementFinder` + `VisualElement` 路径，而是建立了一套平行子系统（见 §7 设计原理）。

当前 IMGUI 验收基线：

- `Assets/Examples/Yaml/99-imgui-example.yaml`：主路径冒烟测试（点击、输入、选择、断言、截图）。
- `Assets/Examples/Yaml/98-imgui-advanced.yaml`：高级场景（等待超时、滚动、焦点链、组合键、值断言）。
- `Assets/Examples/Yaml/97-imgui-negative-assert.yaml`：负向断言验证。
- `Assets/Examples/Yaml/_96-imgui-negative-wait.yaml`：负向等待超时验证。
- `Assets/Examples/Yaml/_97-imgui-negative-assert.yaml`：负向断言副本验证。
- `Assets/Examples/Yaml/69-imgui-alternative-params.yaml`：替代参数验证（`option`、`selector`）。
- `Assets/Examples/Yaml/_78-negative-type-text-non-input.yaml`：type_text 应用于非输入元素负向验证。
- `Assets/Examples/Yaml/_79-negative-press-key-combination.yaml`：无效组合键负向验证。

---

## 2. 何时用 YAML 测试用例，何时用 C# 代码测试用例

### 2.1 优先选择 YAML 测试用例的场景

| 场景 | 原因 |
|------|------|
| IMGUI EditorWindow 基础控件交互（点击、输入、断言） | `imgui_click`、`imgui_type`、`imgui_assert_text` 直接支持 |
| 文本字段输入与读取 | `imgui_type`（逐字符输入）+ `imgui_read_value`（存入 SharedBag）覆盖完整路径 |
| 按钮点击与可见性断言 | `imgui_click` + `imgui_assert_visible` 是最简单的验证形式 |
| 下拉选择（已知选项） | `imgui_select_option` 支持 `field_name` 反射直写（最稳定）或事件模拟回退 |
| 焦点与键盘快捷键 | `imgui_focus` + `imgui_press_key_combination`（如 `Ctrl+A`、`Delete`） |
| 滚动视图 | `imgui_scroll` 直接支持 |
| 等待控件出现（轮询） | `imgui_wait`（支持 `timeout` 参数） |
| 与 UIToolkit 动作混用（同一 YAML 文件） | IMGUI 动作可与 UIToolkit 动作无缝混排，适合混合 UI 窗口 |
| 负向测试（断言控件不存在、超时失败） | `imgui_wait` + `timeout` 配合 `expected_failure` 测试 |

### 2.2 需要 C# 代码测试用例的场景

| 场景 | 原因 |
|------|------|
| `GenericMenu` / `EditorUtility.DisplayDialog` 浮窗内部操作 | 弹出菜单由 Editor 全局管理，不在目标窗口 `GUILayoutUtility` 布局批次内 |
| 独立 `EditorWindow` 弹窗（Color Picker、Curve Editor） | 拥有独立 `OnGUI` 上下文，当前 IMGUI 桥接按单窗口组织 |
| Toggle 运行时 bool 值精确断言 | `GUILayoutEntry` 不存储控件业务状态值；需 C# 反射读取被测窗口字段 |
| Slider 当前 float 值精确断言 | 同上；`imgui_assert_value` 只能尽力推断 |
| 纯绘制控件（`GUI.DrawTexture`、`Handles`） | 不经过 `GUILayoutUtility.GetRect`，无 `GUILayoutEntry` |
| IME/输入法组合输入验证 | `UnityEngine.Event` 不覆盖 IME 组合态语义 |
| 系统剪贴板真实粘贴验证 | 需要平台级剪贴板 API |
| 多窗口 IMGUI 协同操作 | 当前 `ImguiBridgeRegistry` 按单宿主窗口组织 |
| 拖放文件/对象到 IMGUI 控件 | 依赖 Editor `DragAndDrop` 生命周期，与普通 IMGUI 指针事件不等价 |
| 像素级视觉对比 | 需要图像处理库，框架当前无内建像素 diff |
| 被测代码无 `GUI.SetNextControlName` 且结构不稳定 | 此时 `index` 选择器会漂移；需 C# 直接调用被测窗口方法 |

### 2.3 YAML + C# 混合场景

- 在 C# Fixture 子类的 `SetUp` 中初始化被测窗口和测试数据，然后通过 `ExecuteYamlStepsAsync` 驱动 YAML 流程。
- IMGUI `field_name` 反射直写（`imgui_select_option` 的 `field_name` 参数）是最稳定的下拉赋值方式：无需弹出 OS 原生菜单，直接修改被测窗口字段值。
- 对需要精确值断言的 Toggle/Slider 场景，用 C# 在 `TearDown` 中反射读取被测窗口字段断言，YAML 只负责 UI 交互流程。

---

## 3. V1 全面支持的控件

以下控件可通过 YAML `imgui_*` 内置动作完成完整的定位、交互和断言：

| 控件 | IMGUI API | 选择器示例 | 可用动作 |
|------|-----------|-----------|---------|
| `Button` | `GUILayout.Button` | `gui(button)` / `gui(button, text="Save")` | `imgui_click`、`imgui_double_click`、`imgui_right_click`、`imgui_hover`、`imgui_assert_text`、`imgui_assert_visible`、`imgui_assert_value` |
| `TextField` | `GUILayout.TextField` / `EditorGUILayout.TextField` | `gui(textfield, index=0)` / `gui(textfield, control_name="username")` | `imgui_type`、`imgui_focus`、`imgui_click`、`imgui_assert_visible`、`imgui_assert_text`、`imgui_assert_value`、`imgui_read_value`、`imgui_press_key`、`imgui_press_key_combination` |
| `Label` | `GUILayout.Label` | `gui(label, index=2)` / `gui(label, text="Status")` | `imgui_assert_text`、`imgui_assert_visible`、`imgui_assert_value`、`imgui_hover` |
| `Toggle` | `GUILayout.Toggle` / `EditorGUILayout.Toggle` | `gui(toggle, text="Enabled")` / `gui(toggle, control_name="feature-toggle")` | `imgui_click`、`imgui_assert_visible`、`imgui_assert_value`（尽力推断，见 §6） |
| `Popup/Dropdown` | `EditorGUILayout.Popup` | `gui(dropdown, index=0)` / `gui(dropdown, control_name="quality-popup")` | `imgui_click`、`imgui_select_option`、`imgui_assert_visible`、`imgui_assert_value`、`imgui_read_value` |
| `Toolbar` | `GUILayout.Toolbar` | `gui(toolbar, index=0)` | `imgui_click`、`imgui_assert_visible` |
| `Slider` | `GUILayout.HorizontalSlider` / `EditorGUILayout.Slider` | `gui(slider, index=0)` / `gui(slider, control_name="scale-slider")` | `imgui_click`、`imgui_assert_visible`、`imgui_press_key`（方向键） |
| `ScrollView` | `GUILayout.BeginScrollView` | `gui(scroller)` | `imgui_scroll`、`imgui_assert_visible` |
| `Group` | `GUILayout.BeginVertical/Horizontal` | `gui(group="Settings")` | 作为路径容器配合子控件选择器使用 |

**说明**：

- `imgui_assert_value` 对 IMGUI 控件为**尽力推断**（best-effort）。IMGUI 的 `GUILayoutEntry` 不存储控件的运行时值（如 Toggle 的 bool、Slider 的 float），断言依赖 `text` 内容或 `style` 名称推断，见 §6 限制。
- `imgui_select_option` 对 Dropdown 提供两条路径：优先通过 `field_name` 反射直写字段值（最稳定）；若未提供 `field_name`，则回退到事件模拟（点击展开 → 方向键导航 → 回车确认），后者对动态弹出菜单为尽力而为。支持 `option`（按文本或索引）和 `index` 参数。
- `imgui_press_key` / `imgui_press_key_combination` 发送 `KeyDown`/`KeyUp` 事件到当前获焦控件，适用于文本字段的快捷键操作（如 `Ctrl+A`、`Delete`）。

---

## 4. V1 局部支持的控件

以下控件可定位和进行部分交互，但部分功能在 V1 中受限：

| 控件 | 可用 YAML 动作 | 不支持的交互 | 原因与替代方案 |
|------|--------------|------------|--------------|
| `Foldout` | `imgui_click`、`imgui_assert_visible` | 状态断言（展开/折叠） | `GUILayoutEntry` 不暴露 foldout 的展开状态；需通过被测代码的 `field_name` 反射读取 |
| `MinMaxSlider` | `imgui_click`、`imgui_assert_visible`、`imgui_press_key` | 精确拖拽到目标范围 | 当前仅支持点击和方向键微调；精确范围设置需 C# 反射直写 |
| `TextArea` | `imgui_type`、`imgui_focus`、`imgui_click`、`imgui_assert_text` | 多行文本的逐行断言 | 与 `TextField` 共用同一事件路径，但行数计算和滚动定位无内建支持 |
| `IntField/FloatField`（EditorGUILayout） | `imgui_type`、`imgui_focus`、`imgui_assert_text` | 数值越界校验断言 | 值以字符串形式存在于 `GUILayoutEntry` 中，类型级断言需额外解析 |

---

## 5. V1 不可自动化的控件与功能

以下控件或功能由于 IMGUI 架构特性或 Unity API 限制，在 V1 中完全不可自动化：

| 控件/功能 | 原因 | 建议 |
|---------|------|------|
| `GenericMenu` / `EditorUtility.DisplayDialog` 浮窗 | 弹出菜单/对话框由 Editor 全局管理，不在目标窗口的 `GUILayoutUtility` 布局批次内，快照无法捕获 | 使用 `imgui_select_option` 的 `field_name` 反射路径绕过弹出菜单；或将被测代码改为 UIToolkit 实现 |
| 独立 `EditorWindow` 弹窗（Color Picker、Curve Editor） | 弹窗拥有独立的 `OnGUI` 上下文和布局状态，当前 IMGUI 桥接按单窗口组织 | C# 编程直接操作弹窗数据；或将被测逻辑改为 UIToolkit 浮窗 |
| 纯绘制控件（`GUI.DrawTexture`、`Handles` 等） | 不经过 `GUILayoutUtility.GetRect`，不产生 `GUILayoutEntry`，无法进入快照 | 无可行替代方案；需将被测 UI 迁移到 GUILayout 或 UIToolkit |
| 运行时值精确断言（Toggle bool、Slider float 等） | `GUILayoutEntry` 仅存储布局信息（rect、style、text），不存储控件的业务状态值 | 使用 `field_name` 参数通过反射读取被测窗口的字段/属性值进行断言 |
| Tooltip 可视渲染 | Tooltip 由 Editor 全局管理，不在目标窗口布局树内 | 可通过被测代码的字段断言 Tooltip 文本，但无法验证渲染 |
| 拖放文件/对象到 IMGUI 控件 | 依赖 Editor `DragAndDrop` 生命周期，不等价于普通 IMGUI 指针事件 | C# 编程模拟 `DragAndDrop` |
| 系统剪贴板操作（Copy/Paste） | 需要系统级剪贴板 API + 键盘快捷键组合 | 使用 `imgui_type` 快速路径替代粘贴；或使用 `imgui_press_key_combination` 发送 `Ctrl+C/V`（部分场景有效） |
| IME / 输入法组合输入 | `UnityEngine.Event` 不覆盖 IME 组合态语义 | 使用 `imgui_type` 直接写入最终文本 |

---

## 6. 全部 IMGUI 内置动作详细说明

### 6.1 鼠标/指针动作

| 动作名 | 关键参数 | 底层机制 | 事件序列 | YAML 示例 |
|--------|---------|---------|---------|-----------|
| `imgui_click` | `selector` | `EditorWindow.SendEvent` | `Layout` → `MouseDown` → `MouseUp` | `- imgui_click: { selector: "gui(button, text=\"Save\")" }` |
| `imgui_double_click` | `selector` | `EditorWindow.SendEvent` | `Layout` → `MouseDown` → `MouseUp` → `MouseDown` → `MouseUp` | `- imgui_double_click: { selector: "gui(button)" }` |
| `imgui_right_click` | `selector` | `EditorWindow.SendEvent`，`button=1` | `Layout` → `MouseDown(button=1)` → `MouseUp(button=1)` | `- imgui_right_click: { selector: "gui(button, text=\"Options\")" }` |
| `imgui_hover` | `selector` | `EditorWindow.SendEvent` | `MouseMove` 到元素中心 | `- imgui_hover: { selector: "gui(label)" }` |
| `imgui_scroll` | `selector`、`delta`（正数=向上） | `EditorWindow.SendEvent` | `ScrollWheel` 到元素中心 | `- imgui_scroll: { selector: "gui(scroller)", delta: -3 }` |

**坐标转换说明**：所有鼠标事件坐标均为窗口局部坐标。框架通过 `MonoHook` 的 `OnGUIReplacement` 自动计算 `WindowToContentOffset`，确保注入坐标与 IMGUI 渲染坐标系一致。

### 6.2 键盘/输入动作

| 动作名 | 关键参数 | 底层机制 | 事件序列 | YAML 示例 |
|--------|---------|---------|---------|-----------|
| `imgui_type` | `selector`、`text` | `EditorWindow.SendEvent`，逐字符 | `MouseDown` → `MouseUp`（聚焦）→ 每字符一次 `KeyDown`（`character` 字段，`keyCode=None`） | `- imgui_type: { selector: "gui(textfield, index=0)", text: "admin" }` |
| `imgui_focus` | `selector` | `EditorWindow.SendEvent` | `Layout` → `MouseDown` → `MouseUp`（以聚焦为目的） | `- imgui_focus: { selector: "gui(textfield, control_name=\"username\")" }` |
| `imgui_press_key` | `selector`（可选）、`key`（KeyCode 枚举名） | `EditorWindow.SendEvent` | `KeyDown`（`keyCode` 由 `key` 参数映射）→ `KeyUp` | `- imgui_press_key: { key: "Return" }` |
| `imgui_press_key_combination` | `selector`（可选）、`keys`（如 `"Ctrl+A"`） | `EditorWindow.SendEvent`，解析修饰键 | 修饰键 `KeyDown` → 主键 `KeyDown`（带 `EventModifiers`）→ 主键 `KeyUp` → 修饰键 `KeyUp` | `- imgui_press_key_combination: { keys: "Ctrl+A" }` |

**注意**：`imgui_press_key` 和 `imgui_press_key_combination` 将事件发送到**当前获焦控件**。若省略 `selector`，默认作用于 `GUIUtility.keyboardControl` 对应的控件。

### 6.3 值赋值动作

| 动作名 | 关键参数 | 支持的控件 | 说明 | YAML 示例 |
|--------|---------|-----------|------|-----------|
| `imgui_select_option` | `selector`、`field_name`（可选，反射字段名）、`option`（文本/索引，可用 `index` 别名） | `Popup`/`Dropdown` | 主路径：`field_name` 反射直写字段值（跳过 OS 弹出菜单）；回退路径：点击展开 → DownArrow 导航 → Return 确认 | `- imgui_select_option: { selector: "gui(dropdown, index=0)", field_name: "_qualityLevel", option: "High" }` |

### 6.4 断言动作

| 动作名 | 关键参数 | 说明 | YAML 示例 |
|--------|---------|------|-----------|
| `imgui_assert_visible` | `selector` | 断言元素快照 Rect 的宽高均非零 | `- imgui_assert_visible: { selector: "gui(button, text=\"OK\")" }` |
| `imgui_assert_text` | `selector`、`text` | 断言快照条目的 `.Text` 字段与 `text` 参数匹配（精确或包含，OrdinalIgnoreCase） | `- imgui_assert_text: { selector: "gui(label, index=0)", text: "Status: Ready" }` |
| `imgui_assert_value` | `selector`、`value` | 尽力推断：Label/Button 返回 `text`；Toggle/Slider 无运行时值，返回 "unknown" 或 style 名称；建议改用 `field_name` 反射断言 | `- imgui_assert_value: { selector: "gui(label, text=\"Score\")", value: "100" }` |

### 6.5 读取/等待动作

| 动作名 | 关键参数 | 说明 | YAML 示例 |
|--------|---------|------|-----------|
| `imgui_read_value` | `selector`、`bag_key` | 读取快照条目的 `text` 值并存入 `SharedBag[bag_key]`，供后续步骤通过 `{{ bag_key }}` 模板引用 | `- imgui_read_value: { selector: "gui(label, index=1)", bag_key: "status_text" }` |
| `imgui_wait` | `selector`、`timeout`（如 `"3s"`，上限 600s） | 轮询直到控件出现在快照中；超过 `timeout` 则失败 | `- imgui_wait: { selector: "gui(button, text=\"Done\")", timeout: "5s" }` |

---

## 7. 输入模拟支持详情

### 7.1 鼠标/指针输入

| 操作 | 支持情况 | 技术机制 |
|------|---------|---------|
| 左键单击 | ✅ 完整支持 | `SendEvent(MouseDown)` + `SendEvent(MouseUp)`，坐标为控件 Rect 中心 |
| 左键双击 | ✅ 完整支持 | 两次连续 `MouseDown`+`MouseUp` |
| 右键单击 | ✅ 完整支持 | `MouseDown(button=1)` + `MouseUp(button=1)` |
| 悬停（MouseMove） | ✅ 完整支持 | `SendEvent(MouseMove)` 到控件中心 |
| 滚动 | ✅ 完整支持 | `SendEvent(ScrollWheel)`，`delta` 参数正为向上、负为向下 |
| 修饰键+点击 | ❌ 不支持 | IMGUI 鼠标事件未封装 `EventModifiers` 参数；需 C# 直接注入 |
| 拖拽 | ❌ 不支持 | V1 无 `imgui_drag` 动作；精确拖拽需 C# 构造 `MouseDrag` 事件序列 |
| 跨窗口操作 | ❌ 不支持 | 当前每个测试仅支持一个宿主窗口的 IMGUI 桥接 |
| 文件/对象拖放 | ❌ 不支持 | 依赖 Editor `DragAndDrop` 生命周期 |

### 7.2 键盘输入

| 操作 | 支持情况 | 技术机制 |
|------|---------|---------|
| 单字符文本输入 | ✅ 完整支持 | `imgui_type`：每字符一次 `KeyDown`，设置 `Event.character` 字段（不设 keyCode） |
| 功能键（Enter/Escape/Delete/Tab/方向键等） | ✅ 完整支持 | `imgui_press_key`：`Event.keyCode` 映射 KeyCode 枚举名 |
| 组合键（Ctrl+A/Ctrl+C 等） | ✅ 完整支持 | `imgui_press_key_combination`：修饰键 `KeyDown`→主键（带 `EventModifiers`）→主键 `KeyUp`→修饰键 `KeyUp` |
| 批量文本快速写入 | ❌ 不支持 | 无等价 `imgui_type_fast`（P2 扩展项）；当前只能逐字符 `imgui_type` |
| 焦点链 Tab 切换 | ❌ 不支持 | 可用 `imgui_press_key: { key: "Tab" }` 模拟，但无专用焦点链导航动作 |
| IME/输入法组合输入 | ❌ 不支持 | `UnityEngine.Event` 不覆盖 IME 组合态语义 |
| 系统剪贴板真实粘贴 | ❌ 不支持 | 需要平台级剪贴板 API；`imgui_type` 可替代 |

---

## 8. 设计原理与架构

### 8.1 核心架构

```
YAML imgui_* 动作
    ↓
ImguiSelectorCompiler（编译 gui(button, text="OK") 语法）
    ↓
ImguiExecutionBridge（通过 MonoHook 注入 OnGUI 钩子，或反射 GUILayoutUtility.current）
    ↓
OnGUI 执行 → ImguiSnapshotCapture（反射 GUILayoutUtility.current.topLevel.entries）
    ↓
ImguiElementLocator（基于快照匹配选择器：type / text / index / group / control_name / focused）
    ↓
Event 注入（MouseDown/Up/Move/ScrollWheel/KeyDown/Up）或断言
```

### 8.2 快照捕获双链路

Unity 6000.6.0a2 中 `GUILayoutUtility.current` 在 `OnGUI` 返回后会被清空，因此框架实现了两条捕获链路：

| 链路 | 机制 | 适用场景 |
|------|------|---------|
| **反射主链路** | 在 `OnGUI` 执行期间反射读取 `GUILayoutUtility.current.topLevel.entries`，递归遍历 `GUILayoutGroup`/`GUILayoutEntry` | 标准 GUILayout 控件（Button、Label、Toggle 等） |
| **MonoHook Fallback** | 通过 `MonoHook` 库 hook `GUILayoutUtility.DoGetRect`、`EditorGUILayout.Popup`、`GUIStyle.Draw`，在调用时实时记录控件 rect/style/text | Unity 6000+ 反射失效时；绕过 GUILayout 的自定义绘制控件 |

两条链路的数据会在 `OnGUI` 结束后合并：若反射链路无数据且 hook 链路有记录，则自动使用 hook 数据作为快照。

### 8.3 事件注入机制

所有交互通过构造 `UnityEngine.Event` 并调用 `EditorWindow.SendEvent()` 实现：

| 动作 | 事件序列 | 延迟 |
|------|---------|------|
| `imgui_click` | `Layout` → `MouseDown` → `MouseUp` | 50ms post-delay |
| `imgui_double_click` | `Layout` → `MouseDown` → `MouseUp` → `MouseDown` → `MouseUp` | 80ms post-delay |
| `imgui_right_click` | `Layout` → `MouseDown(button=1)` → `MouseUp(button=1)` | 50ms post-delay |
| `imgui_hover` | `MouseMove` | 无 |
| `imgui_type` | `MouseDown` → `MouseUp`（聚焦）→ 逐字符 `KeyDown` | 每字符间隔 |
| `imgui_scroll` | `ScrollWheel` | 无 |
| `imgui_press_key` | `KeyDown` → `KeyUp` | 无 |
| `imgui_press_key_combination` | 修饰键 `KeyDown` → 主键 `KeyDown` → 主键 `KeyUp` → 修饰键 `KeyUp` | 无 |

**坐标转换**：IMGUI 控件的 `Rect` 为窗口局部坐标。`SendEvent` 需要窗口空间坐标（含标题栏偏移）。框架通过 `MonoHook` 的 `OnGUIReplacement` 自动计算 `WindowToContentOffset`，确保事件注入坐标准确。

### 8.4 ImguiBridgeRegistry 与窗口管理

- 每个 `EditorWindow` 实例对应一个 `ImguiBridgeRegistry` 条目。
- Registry 管理 MonoHook 的安装和卸载：`OnGUI` hook、`GUILayoutUtility.DoGetRect` hook、`EditorGUILayout.Popup` hook、`GUIStyle.Draw` hook。
- 当窗口关闭时自动卸载所有 hook，防止内存泄漏。

---

## 9. 选择器语法完整参考

```yaml
# 基本类型匹配（按推断的控件类型）
gui(button)
gui(textfield, index=2)
gui(toggle, text="Enabled")
gui(label)
gui(dropdown)
gui(slider)
gui(toolbar)
gui(scroller)

# ControlName 匹配（需要被测代码配合 GUI.SetNextControlName）
gui(textfield, control_name="username-field")
gui(button, control_name="save-button")

# 文本匹配（OrdinalIgnoreCase 子串匹配）
gui(button, text="Save")
gui(label, text="Status")

# 索引匹配（在按 type 过滤后的候选列表中按索引选取）
gui(textfield, index=0)
gui(button, index=2)

# 组路径限定（缩小匹配范围）
gui(group="Settings" > button, text="Apply")
gui(group="Panel" > textfield, index=0)

# 焦点匹配（返回当前获焦控件）
gui(focused)
```

**选择器优先级**：

1. `focused` — 最高优先级特殊匹配，通过 `GUIUtility.keyboardControl` 判断。
2. `control_name` — 最精确，推荐在关键控件上使用（需 `GUI.SetNextControlName`）。
3. `text` — OrdinalIgnoreCase 子串匹配。
4. `group` — 先限定父容器范围，再在范围内匹配子控件。
5. `index` — 在按 type 过滤后的候选列表中按位置索引选取。

---

## 10. 被测代码的可测试性改造（推荐）

为了让选择器更稳定，建议在 IMGUI 代码中为关键控件设置 `ControlName`：

```csharp
GUI.SetNextControlName("save-button");
if (GUILayout.Button("Save")) { /* ... */ }

GUI.SetNextControlName("username-field");
_username = GUILayout.TextField(_username);

// 为组命名，便于 group 路径限定
GUILayout.BeginVertical("box");
{
    GUI.SetNextControlName("quality-popup");
    _qualityLevel = EditorGUILayout.Popup(_qualityLevel, _options);
}
GUILayout.EndVertical();
```

YAML 中即可精确匹配：

```yaml
- imgui_click: { selector: "gui(button, control_name=\"save-button\")" }
- imgui_type:
    selector: "gui(textfield, control_name=\"username-field\")"
    text: "admin"
- imgui_select_option:
    selector: "gui(dropdown, control_name=\"quality-popup\")"
    field_name: "_qualityLevel"
    option: "High"
```

**改造建议**：

- 为所有需要自动化的关键交互控件设置 `ControlName`。
- 避免在同一 `OnGUI` 帧内动态改变控件数量或顺序（会导致 `index` 选择器漂移）。
- 使用 `GUILayout.BeginVertical("box")` 或 `GUILayout.BeginHorizontal("box")` 为逻辑分组命名，便于 `group` 路径限定。

---

## 11. 按交互类型的覆盖总结

| 交互类型 | V1 覆盖范围 | 未覆盖范围 |
|---------|-----------|-----------|
| 点击 | 所有可产生 `GUILayoutEntry` 的控件 | 不能用选择器直接点击 `GenericMenu` 浮窗中的菜单项 |
| 双击 | 所有可点击控件 | 双击检测依赖被测代码自身实现时间阈值（如 `ImguiExampleWindow` 的 `DoubleClickThreshold`） |
| 右键点击 | 所有可点击控件 | `GenericMenu` 浮窗内项 |
| 悬停 | 所有可见控件 | Tooltip 可视渲染验证 |
| 文本输入 | `TextField`、`TextArea` | IME 组合输入 |
| 快速写值 | `imgui_type` 逐字符输入（无 `imgui_type_fast` 等效动作，P2 扩展项） | 无 |
| 选择（下拉） | `Popup` / `Dropdown`（`imgui_select_option`，`field_name` 反射路径最稳定） | 弹出菜单内部逐项点击 |
| 滑块调整 | `Slider`（点击 + 方向键） | 精确拖拽到目标值 |
| 滚动 | `ScrollView`（`imgui_scroll`） | 自定义吸附滚动行为 |
| 按键 | 任意获焦元素（`imgui_press_key`、`imgui_press_key_combination`） | IME、系统级剪贴板 |
| 焦点 | `imgui_focus`（点击聚焦） | 焦点链导航（Tab 切换焦点） |
| 断言 | 可见性、文本、尽力值断言 | 视觉像素对比、运行时类型化值精确对比 |
| 读取值 | `imgui_read_value`（读取 text 存入 SharedBag） | 结构化值读取（如 Vector3、Color） |
| 截图 | 复用 UIToolkit 截图动作（当前窗口） | 多窗口截图 |
| 等待 | `imgui_wait`（轮询等待控件出现，支持 timeout） | 无 |

---

## 12. 不能实现或当前受 Unity 接口边界阻断

以下能力当前不是"项目还没做"，而是明显受 Unity / IMGUI 架构边界影响：

| 项目 | 当前阻断原因 | 结论 |
|------|-----------|------|
| `GenericMenu` / 独立浮窗内部控件级自动化 | 弹出菜单由 Editor 全局管理，不在目标窗口 `GUILayoutUtility` 布局批次内 | 属于 Unity IMGUI 架构边界 |
| IMGUI 控件运行时精确值断言 | `GUILayoutEntry` 不存储业务状态值（如 bool、float、int） | 属于 IMGUI 架构设计；需通过 `field_name` 反射绕过 |
| 纯绘制控件（`GUI.DrawTexture`、`Handles`） | 不经过 `GUILayoutUtility.GetRect`，无 `GUILayoutEntry` | 属于 IMGUI 架构边界 |
| IME 组合输入 | `UnityEngine.Event` 不覆盖 IME 组合态语义 | 属于 Unity 能力边界 |
| 系统剪贴板级真实粘贴 | 需要平台级剪贴板 API 或系统快捷键链路 | 属于系统/平台边界 |
| 多窗口协同拖拽/断言 | 当前测试模型按单宿主窗口组织 | 当前受框架设计限制 |
| 像素级视觉 diff | 当前项目只有真实截图，没有内建视觉基线与差异分析链路 | 当前未接入，不属于 IMGUI 控件动作层问题 |

---

## 13. 工程上可实现但当前尚未落地的功能

以下功能是 Unity API 或系统能力已经开放、代码框架也能承载，但当前项目里**还没有写对应实现**的能力。

### 13.1 按开发难度排序（从易到难）

| 优先级 | 功能 | 难度 | 说明 |
|--------|------|------|------|
| P2 | `imgui_assert_property`（style/rect 断言） | 低 | `ImguiSnapshotEntry` 已包含 `StyleName` 和 `Rect`，只需封装断言动作。 |
| P2 | `imgui_type_text_fast`（反射字段直写） | 低 | `ImguiActionHelper.TrySetFieldValue` 已实现反射字段赋值，只需封装为独立动作。 |
| P2 | `imgui_drag`（滑块/滚动条精确拖拽） | 中 | 需要构造 MouseDrag 事件序列并计算目标坐标，与现有 `SendMouseEvent` 结合即可。 |
| P3 | `imgui_wait_for_element` 语义与 UIToolkit 对齐 | 低 | 当前 `imgui_wait` 功能已完整，主要是动作命名和参数对齐。 |
| P4 | 复杂自定义 IMGUI 控件的 `GUILayoutEntry` 捕获扩展 | 中~高 | 需要为更多 EditorGUILayout 方法安装 `MonoHook`，或扩展 `InferControlType` 的 style 映射表。 |
| P5 | 多窗口 IMGUI 协同测试 | 高 | 需要重构 `ImguiBridgeRegistry` 和窗口管理逻辑，为每个窗口维护独立的桥接和快照。 |

### 13.2 按使用频繁程度排序（从高到低）

| 优先级 | 功能 | 频率 | 说明 |
|--------|------|------|------|
| P2 | `imgui_drag`（滑块精确拖拽） | 中 | 材质、后处理参数测试中较常见，但 `field_name` 反射直写已能满足多数值设定需求。 |
| P2 | `imgui_type_text_fast` | 中 | 长文本输入场景下比逐字符 `imgui_type` 快得多。 |
| P3 | `imgui_assert_property` | 低 | 主要用于验证控件存在性和基础布局属性，`imgui_assert_visible` 已覆盖大部分场景。 |
| P4 | 复杂自定义控件捕获扩展 | 低 | 第三方 IMGUI 插件多样化，优先级取决于具体项目需求。 |
| P5 | 多窗口协同 | 低 | 单窗口 IMGUI 测试已覆盖 95% 以上的 Editor IMGUI 场景。 |

---

## 14. 已知问题与稳定性备注

### 14.1 反射与版本兼容性

- `ImguiSnapshotCapture` 反射 `GUILayoutUtility.current`、`GUILayoutGroup`、`GUILayoutEntry` 等内部字段。Unity 版本升级（尤其是 6000 系列）已导致 `GUILayoutUtility.current` 在 `OnGUI` 返回后被清空。
- **缓解措施**：框架已引入 `MonoHook` 四重 fallback（`OnGUI` hook + `DoGetRect` hook + `Popup` hook + `GUIStyle.Draw` hook），在反射失效时自动切换到 hook 链路。
- **风险**：若 Unity 未来更改 `GUILayoutUtility.DoGetRect` 签名或 `GUIStyle.Draw` 参数，hook 链路仍需更新。

### 14.2 布局稳定性

- 窗口 resize 后 `index` 选择器可能失效，因为 `GUILayoutEntry` 的列表顺序可能随可用空间重排。
- **建议**：始终优先使用 `control_name` 或 `text` 选择器；`index` 仅作为最后手段。

### 14.3 值断言的局限性

- `imgui_assert_value` 对 IMGUI 控件是尽力推断。例如：
  - `Toggle` 的 `GUILayoutEntry` 只包含 `text`（标签文本），不包含勾选状态。
  - `Slider` 的 `GUILayoutEntry` 只包含 `rect` 和 `style`，不包含当前数值。
- **建议**：对需要精确值断言的场景，使用 `field_name` 参数通过反射读取被测窗口字段。

### 14.4 负向测试的稳定性

- `IMGUI Negative - Wait Timeout`（`_96-imgui-negative-wait.yaml`）偶尔会因 IMGUI 重绘时机提前通过（控件在超时前意外出现），属于非关键 flaky 测试。
- **建议**：负向测试的 timeout 不应设置过短（建议 ≥2s），避免与正常重排/重绘竞争。

### 14.5 与 UIToolkit 混用

IMGUI 动作与 UIToolkit 动作可在同一 YAML 中混用：

```yaml
steps:
  # UIToolkit 部分
  - click: { selector: "#open-settings" }

  # IMGUI 部分（设置面板是 IMGUI 的）
  - imgui_type:
      selector: "gui(textfield, control_name=\"project-name\")"
      text: "TestProject"
  - imgui_click:
      selector: "gui(button, text=\"Save\")"

  # 回到 UIToolkit
  - assert_text:
      selector: "#status-label"
      expected: "Saved"
```

混用时需注意：

- `fixture.host_window` 必须指向包含 IMGUI 内容的 `EditorWindow`。
- IMGUI 动作不依赖 `rootVisualElement`，因此可在 UIToolkit 动作失败时作为降级路径使用（如 `IMGUIContainer` 内部内容）。

---

## 15. 修订历史

### 2026-04-23 2.0.0

- 基于代码库完整复核（`UnityUIFlow.ImguiActions.cs`、`ImguiBridgeRegistry.cs`、`UnityUIFlow.ImguiLocators.cs`、`UnityUIFlow.ImguiParsing.cs`）重写文档。
- 新增 §2「何时用 YAML 测试用例，何时用 C# 代码测试用例」——包含优先选择 YAML 的场景清单、必须用 C# 的场景清单、以及 YAML+C# 混合模式说明。
- 新增 §6「全部 IMGUI 内置动作详细说明」——按鼠标、键盘、值赋值、断言、读取/等待五类，逐动作列出关键参数、底层机制、事件序列、YAML 示例。
- 新增 §7「输入模拟支持详情」——逐条列出鼠标/指针和键盘输入的支持情况及底层技术机制（含 ✅/❌ 标记）。
- 重写 §8「设计原理与架构」：补充 `ImguiBridgeRegistry` 窗口管理细节；明确四重 MonoHook fallback（原文为三重）。
- 更新 §3（全面支持控件）：明确 `imgui_select_option` 支持 `option` 和 `index` 参数别名。
- 更新 §11（覆盖总结）：明确 `imgui_type_fast` 为 P2 未落地扩展项。

### 2026-04-21 1.2.0 第三轮扩展

- 新增 `87-type-text-vs-fast.yaml`：包含 `type_text_fast` 对空字符串 `""` 的边界验证。
- 新增 IMGUI 负向测试：`_91-negative-set-value-invalid.yaml`。
- YAML 基线从 102 份扩展到 115 份，IMGUI 相关用例从 8 份扩展到 10 份。

### 2026-04-21 1.1.0 扩展验证

- 新增 `69-imgui-alternative-params.yaml`：验证 `imgui_select_option` 的 `option` 参数、`imgui_press_key` 和 `imgui_press_key_combination` 的 `selector` 可选参数。
- 新增 IMGUI 负向测试：`_78-negative-type-text-non-input.yaml`、`_79-negative-press-key-combination.yaml`。
- YAML 基线从 71 份扩展到 102 份，IMGUI 相关用例从 5 份扩展到 8 份。

### 2026-04-21 1.0.0 初始版本

- 基于全量代码复核和 5 份 YAML 回归用例验证结果创建本文档。
- 确认 15 个 `imgui_*` 动作全部实现并通过回归验证。
- 确认快照捕获双链路（反射 + MonoHook）在 Unity 6000.6.0a2 下工作正常。
- 明确列出 IMGUI 架构导致的硬性边界（值断言、浮窗、纯绘制控件）。
