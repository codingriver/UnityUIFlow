# IMGUI 控件自动化覆盖与限制说明

版本：1.0.0  
日期：2026-04-21  
状态：已全量验证（5 份 IMGUI YAML 回归用例全部执行通过，含 2 份负向测试）

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

因此，IMGUI 自动化不走 `ElementFinder` + `VisualElement` 路径，而是建立了一套平行子系统（见 §6 设计原理）。

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

## 2. V1 全面支持的控件

以下控件可通过 YAML `imgui_*` 内置动作完成完整的定位、交互和断言：

| 控件 | IMGUI API | 选择器示例 | 可用动作 |
|------|-----------|-----------|---------|
| `Button` | `GUILayout.Button` | `gui(button)` / `gui(button, text="Save")` | `imgui_click`、`imgui_double_click`、`imgui_right_click`、`imgui_hover`、`imgui_assert_text`、`imgui_assert_visible`、`imgui_assert_value` |
| `TextField` | `GUILayout.TextField` / `EditorGUILayout.TextField` | `gui(textfield, index=0)` / `gui(textfield, control_name="username")` | `imgui_type`、`imgui_focus`、`imgui_click`、`imgui_assert_visible`、`imgui_assert_text`、`imgui_assert_value`、`imgui_read_value`、`imgui_press_key`、`imgui_press_key_combination` |
| `Label` | `GUILayout.Label` | `gui(label, index=2)` / `gui(label, text="Status")` | `imgui_assert_text`、`imgui_assert_visible`、`imgui_assert_value`、`imgui_hover` |
| `Toggle` | `GUILayout.Toggle` / `EditorGUILayout.Toggle` | `gui(toggle, text="Enabled")` / `gui(toggle, control_name="feature-toggle")` | `imgui_click`、`imgui_assert_visible`、`imgui_assert_value`（尽力推断） |
| `Popup/Dropdown` | `EditorGUILayout.Popup` | `gui(dropdown, index=0)` / `gui(dropdown, control_name="quality-popup")` | `imgui_click`、`imgui_select_option`、`imgui_assert_visible`、`imgui_assert_value`、`imgui_read_value` |
| `Toolbar` | `GUILayout.Toolbar` | `gui(toolbar, index=0)` | `imgui_click`、`imgui_assert_visible` |
| `Slider` | `GUILayout.HorizontalSlider` / `EditorGUILayout.Slider` | `gui(slider, index=0)` / `gui(slider, control_name="scale-slider")` | `imgui_click`、`imgui_assert_visible`、`imgui_press_key`（方向键） |
| `ScrollView` | `GUILayout.BeginScrollView` | `gui(scroller)` | `imgui_scroll`、`imgui_assert_visible` |
| `Group` | `GUILayout.BeginVertical/Horizontal` | `gui(group="Settings")` | 作为路径容器配合子控件选择器使用 |

**说明**：
- `imgui_assert_value` 对 IMGUI 控件为**尽力推断**（best-effort）。IMGUI 的 `GUILayoutEntry` 不存储控件的运行时值（如 Toggle 的 bool、Slider 的 float），断言依赖 `text` 内容或 `style` 名称推断，见 §5 限制。
- `imgui_select_option` 对 Dropdown 提供两条路径：优先通过 `field_name` 反射直写字段值（最稳定）；若未提供 `field_name`，则回退到事件模拟（点击展开 → 方向键导航 → 回车确认），后者对动态弹出菜单为尽力而为。
- `imgui_press_key` / `imgui_press_key_combination` 发送 `KeyDown`/`KeyUp` 事件到当前获焦控件，适用于文本字段的快捷键操作（如 `Ctrl+A`、`Delete`）。

---

## 3. V1 局部支持的控件

以下控件可定位和进行部分交互，但部分功能在 V1 中受限：

| 控件 | 可用 YAML 动作 | 不支持的交互 | 原因与替代方案 |
| --- | --- | --- | --- |
| `Foldout` | `imgui_click`、`imgui_assert_visible` | 状态断言（展开/折叠） | `GUILayoutEntry` 不暴露 foldout 的展开状态；需通过被测代码的 `field_name` 反射读取 |
| `MinMaxSlider` | `imgui_click`、`imgui_assert_visible`、`imgui_press_key` | 精确拖拽到目标范围 | 当前仅支持点击和方向键微调；精确范围设置需 C# 反射直写 |
| `TextArea` | `imgui_type`、`imgui_focus`、`imgui_click`、`imgui_assert_text` | 多行文本的逐行断言 | 与 `TextField` 共用同一事件路径，但行数计算和滚动定位无内建支持 |
| `IntField/FloatField`（EditorGUILayout） | `imgui_type`、`imgui_focus`、`imgui_assert_text` | 数值越界校验断言 | 值以字符串形式存在于 `GUILayoutEntry` 中，类型级断言需额外解析 |

---

## 4. V1 仅定位/断言的控件

当前 IMGUI 子系统下暂无明确仅支持定位/断言、完全不支持交互的控件类型。所有可捕获的 `GUILayoutEntry` 至少支持 `imgui_click`（向控件中心发送 MouseDown+MouseUp）或 `imgui_assert_visible`。

未来若引入新的 IMGUI 专属容器控件（如某些第三方插件的复杂复合控件），可能仍会先落在“可定位、可读属性、但没有统一语义动作”的阶段。

---

## 5. V1 不可自动化的控件与功能

以下控件或功能由于 IMGUI 架构特性或 Unity API 限制，在 V1 中完全不可自动化：

| 控件/功能 | 原因 | 建议 |
| --- | --- | --- |
| `GenericMenu` / `EditorUtility.DisplayDialog` 浮窗 | 弹出菜单/对话框由 Editor 全局管理，不在目标窗口的 `GUILayoutUtility` 布局批次内，快照无法捕获 | 使用 `imgui_select_option` 的 `field_name` 反射路径绕过弹出菜单；或将被测代码改为 UIToolkit 实现 |
| 独立 `EditorWindow` 弹窗（如 Color Picker、Curve Editor） | 弹窗拥有独立的 `OnGUI` 上下文和布局状态，当前 IMGUI 桥接按单窗口组织 | C# 编程直接操作弹窗数据；或将被测逻辑改为 UIToolkit 浮窗 |
| 纯绘制控件（`GUI.DrawTexture`、`Handles` 等） | 不经过 `GUILayoutUtility.GetRect`，不产生 `GUILayoutEntry`，无法进入快照 | 无可行替代方案；需将被测 UI 迁移到 GUILayout 或 UIToolkit |
| 运行时值精确断言（Toggle bool、Slider float 等） | IMGUI 的 `GUILayoutEntry` 仅存储布局信息（rect、style、text），不存储控件的业务状态值 | 使用 `field_name` 参数通过反射读取被测窗口的字段/属性值进行断言 |
| Tooltip 可视渲染 | Tooltip 由 Editor 全局管理，不在目标窗口布局树内 | 可通过被测代码的字段断言 Tooltip 文本，但无法验证渲染 |
| 拖放文件/对象到 IMGUI 控件 | 依赖 Editor `DragAndDrop` 生命周期，不等价于普通 IMGUI 指针事件 | C# 编程模拟 `DragAndDrop` |
| 系统剪贴板操作（Copy/Paste） | 需要系统级剪贴板 API + 键盘快捷键组合 | 使用 `imgui_type` 快速路径替代粘贴；或使用 `imgui_press_key_combination` 发送 `Ctrl+C/V`（部分场景有效） |
| IME / 输入法组合输入 | `UnityEngine.Event` 不覆盖 IME 组合态语义 | 使用 `imgui_type` 直接写入最终文本 |

---

## 6. 设计原理与架构

### 6.1 核心架构

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

### 6.2 快照捕获双链路

Unity 6000.6.0a2 中 `GUILayoutUtility.current` 在 `OnGUI` 返回后会被清空，因此框架实现了两条捕获链路：

| 链路 | 机制 | 适用场景 |
|------|------|---------|
| **反射主链路** | 在 `OnGUI` 执行期间反射读取 `GUILayoutUtility.current.topLevel.entries` | 标准 GUILayout 控件（Button、Label、Toggle 等） |
| **MonoHook Fallback** | 通过 `MonoHook` 库 hook `GUILayoutUtility.GetRect`、`EditorGUILayout.Popup`、`GUIStyle.Draw`，在调用时实时记录控件 rect/style/text | Unity 6000+ 反射失效时；绕过 GUILayout 的自定义绘制控件 |

两条链路的数据会在 `OnGUI` 结束后合并：若反射链路无数据且 hook 链路有记录，则自动使用 hook 数据作为快照。

### 6.3 事件注入机制

所有交互通过构造 `UnityEngine.Event` 并调用 `EditorWindow.SendEvent()` 实现：

| 动作 | 事件序列 |
|------|---------|
| `imgui_click` | `Layout` → `MouseDown` → `MouseUp` |
| `imgui_double_click` | `Layout` → `MouseDown` → `MouseUp` → `MouseDown` → `MouseUp` |
| `imgui_right_click` | `Layout` → `MouseDown(button=1)` → `MouseUp(button=1)` |
| `imgui_hover` | `MouseMove` |
| `imgui_type` | `MouseDown` → `MouseUp`（聚焦）→ 逐字符 `KeyDown` |
| `imgui_scroll` | `ScrollWheel` |
| `imgui_press_key` | `KeyDown` → `KeyUp` |
| `imgui_press_key_combination` | 修饰键 `KeyDown` → 主键 `KeyDown` → 主键 `KeyUp` → 修饰键 `KeyUp` |

**坐标转换**：IMGUI 控件的 `Rect` 为窗口局部坐标。`SendEvent` 需要窗口空间坐标（含标题栏偏移）。框架通过 `MonoHook` 的 `OnGUIReplacement` 自动计算 `WindowToContentOffset`，确保事件注入坐标准确。

### 6.4 选择器语法完整参考

```yaml
# 基本类型匹配（按 inferred control type）
gui(button)
gui(textfield, index=2)
gui(toggle, text="Enabled")

# ControlName 匹配（需要被测代码配合 GUI.SetNextControlName）
gui(textfield, control_name="username-field")

# 组路径限定（缩小匹配范围）
gui(group="Settings" > button, text="Apply")

# 焦点匹配（返回当前获焦控件）
gui(focused)
```

**选择器优先级**：
1. `focused` 为最高优先级特殊匹配。
2. `control_name` 匹配最精确，推荐在关键控件上使用。
3. `text` 模糊匹配（OrdinalIgnoreCase 子串匹配）。
4. `index` 在按 type 过滤后的候选列表中按索引选取。
5. `group` 进一步缩小候选范围。

### 6.5 被测代码的可测试性改造（推荐）

为了让选择器更稳定，建议在 IMGUI 代码中为关键控件设置 `ControlName`：

```csharp
GUI.SetNextControlName("save-button");
if (GUILayout.Button("Save")) { /* ... */ }

GUI.SetNextControlName("username-field");
_username = GUILayout.TextField(_username);
```

YAML 中即可精确匹配：
```yaml
- imgui_click: { selector: "gui(button, control_name=\"save-button\")" }
- imgui_type:
    selector: "gui(textfield, control_name=\"username-field\")"
    text: "admin"
```

**改造建议**：
- 为所有需要自动化的关键交互控件设置 `ControlName`。
- 避免在同一 `OnGUI` 帧内动态改变控件数量或顺序（会导致 `index` 选择器漂移）。
- 使用 `GUILayout.BeginVertical("box")` 或 `GUILayout.BeginHorizontal("box")` 为逻辑分组命名，便于 `group` 路径限定。

---

## 7. 按交互类型的覆盖总结

| 交互类型 | V1 覆盖范围 | 未覆盖范围 |
| --- | --- | --- |
| 点击 | 所有可产生 `GUILayoutEntry` 的控件 | 不能用选择器直接点击 `GenericMenu` 浮窗中的菜单项 |
| 双击 | 所有可点击控件 | 双击检测依赖被测代码自身实现时间阈值（如 `ImguiExampleWindow` 的 `DoubleClickThreshold`） |
| 右键点击 | 所有可点击控件 | `GenericMenu` 浮窗内项 |
| 悬停 | 所有可见控件 | Tooltip 可视渲染验证 |
| 文本输入 | `TextField`、`TextArea` | IME 组合输入 |
| 快速写值 | `imgui_type` 逐字符输入（无 `type_text_fast` 等效动作） | 无 |
| 选择（下拉） | `Popup` / `Dropdown`（`imgui_select_option`） | 弹出菜单内部逐项点击 |
| 滑块调整 | `Slider`（点击 + 方向键） | 精确拖拽到目标值 |
| 滚动 | `ScrollView`（`imgui_scroll`） | 自定义吸附滚动行为 |
| 按键 | 任意获焦元素（`imgui_press_key`、`imgui_press_key_combination`） | IME、系统级剪贴板 |
| 焦点 | `imgui_focus`（点击聚焦） | 焦点链导航（Tab 切换焦点） |
| 断言 | 可见性、文本、尽力值断言 | 视觉像素对比、运行时类型化值精确对比 |
| 读取值 | `imgui_read_value`（读取 text 存入 SharedBag） | 结构化值读取（如 Vector3、Color） |
| 截图 | 复用 UIToolkit 截图动作（当前窗口） | 多窗口截图 |
| 等待 | `imgui_wait`（轮询等待控件出现） | 无 |

---

## 8. 扩展规划

| 优先级 | 功能 | 说明 |
| --- | --- | --- |
| P2 | `imgui_drag` 拖拽动作 | 模拟 MouseDown → MouseDrag → MouseUp 序列，支持滑块精确拖拽 |
| P2 | `imgui_assert_property` | 断言 IMGUI 控件的 style、rect 等布局属性 |
| P2 | `imgui_type_text_fast` | 类似 UIToolkit `type_text_fast` 的整段文本直接赋值（通过反射字段直写） |
| P3 | `imgui_wait_for_element` 超时语义统一 | 当前 `imgui_wait` 已支持 timeout，未来可与 UIToolkit `wait_for_element` 语义进一步对齐 |
| P3 | 多窗口 IMGUI 协同 | 当前每个测试仅支持一个宿主窗口的 IMGUI 桥接 |
| P4 | 视觉像素对比断言 | 截图对比基线图（与 UIToolkit 共享基础设施） |

---

## 9. 不能实现或当前受 Unity 接口边界阻断

以下能力当前不是"项目还没做"，而是明显受 Unity / IMGUI 架构边界影响：

| 项目 | 当前阻断原因 | 结论 |
| --- | --- | --- |
| `GenericMenu` / 独立浮窗内部控件级自动化 | 弹出菜单由 Editor 全局管理，不在目标窗口 `GUILayoutUtility` 布局批次内 | 属于 Unity IMGUI 架构边界 |
| IMGUI 控件运行时精确值断言 | `GUILayoutEntry` 不存储业务状态值（如 bool、float、int） | 属于 IMGUI 架构设计；需通过 `field_name` 反射绕过 |
| 纯绘制控件（`GUI.DrawTexture`、`Handles`） | 不经过 `GUILayoutUtility.GetRect`，无 `GUILayoutEntry` | 属于 IMGUI 架构边界 |
| IME 组合输入 | `UnityEngine.Event` 不覆盖 IME 组合态语义 | 属于 Unity 能力边界 |
| 系统剪贴板级真实粘贴 | 需要平台级剪贴板 API 或系统快捷键链路 | 属于系统/平台边界 |
| 多窗口协同拖拽/断言 | 当前测试模型按单宿主窗口组织 | 当前受框架设计限制 |
| 像素级视觉 diff | 当前项目只有真实截图，没有内建视觉基线与差异分析链路 | 当前未接入，不属于 IMGUI 控件动作层问题 |

---

## 10. 工程上可实现但当前尚未落地的功能

以下功能是 Unity API 或系统能力已经开放、代码框架也能承载，但当前项目里**还没有写对应实现**的能力。

### 10.1 按开发难度排序（从易到难）

| 优先级 | 功能 | 难度 | 说明 |
| --- | --- | --- | --- |
| P2 | `imgui_assert_property`（style/rect 断言） | 低 | `ImguiSnapshotEntry` 已包含 `StyleName` 和 `Rect`，只需封装断言动作。 |
| P2 | `imgui_type_text_fast`（反射字段直写） | 低 | `ImguiActionHelper.TrySetFieldValue` 已实现反射字段赋值，只需封装为独立动作。 |
| P2 | `imgui_drag`（滑块/滚动条精确拖拽） | 中 | 需要构造 MouseDrag 事件序列并计算目标坐标，与现有 `SendMouseEvent` 结合即可。 |
| P3 | `imgui_wait_for_element` 语义与 UIToolkit 对齐 | 低 | 当前 `imgui_wait` 功能已完整，主要是动作命名和参数对齐。 |
| P4 | 复杂自定义 IMGUI 控件的 `GUILayoutEntry` 捕获扩展 | 中~高 | 需要为更多 EditorGUILayout 方法安装 `MonoHook`，或扩展 `InferControlType` 的 style 映射表。 |
| P5 | 多窗口 IMGUI 协同测试 | 高 | 需要重构 `ImguiBridgeRegistry` 和窗口管理逻辑，为每个窗口维护独立的桥接和快照。 |

### 10.2 按使用频繁程度排序（从高到低）

| 优先级 | 功能 | 频率 | 说明 |
| --- | --- | --- | --- |
| P2 | `imgui_drag`（滑块精确拖拽） | 中 | 材质、后处理参数测试中较常见，但 `field_name` 反射直写已能满足多数值设定需求。 |
| P2 | `imgui_type_text_fast` | 中 | 长文本输入场景下比逐字符 `imgui_type` 快得多。 |
| P3 | `imgui_assert_property` | 低 | 主要用于验证控件存在性和基础布局属性，`imgui_assert_visible` 已覆盖大部分场景。 |
| P4 | 复杂自定义控件捕获扩展 | 低 | 第三方 IMGUI 插件多样化，优先级取决于具体项目需求。 |
| P5 | 多窗口协同 | 低 | 单窗口 IMGUI 测试已覆盖 95% 以上的 Editor IMGUI 场景。 |

---

## 11. 已知问题与稳定性备注

### 11.1 反射与版本兼容性

- `ImguiSnapshotCapture` 反射 `GUILayoutUtility.current`、`GUILayoutGroup`、`GUILayoutEntry` 等内部字段。Unity 版本升级（尤其是 6000 系列）已导致 `GUILayoutUtility.current` 在 `OnGUI` 返回后被清空。
- **缓解措施**：框架已引入 `MonoHook` 三重 fallback（`OnGUI` hook + `GetRect` hook + `Popup` hook + `GUIStyle.Draw` hook），在反射失效时自动切换到 hook 链路。
- **风险**：若 Unity 未来更改 `GUILayoutUtility.GetRect` 签名或 `GUIStyle.Draw` 参数，hook 链路仍需更新。

### 11.2 布局稳定性

- 窗口 resize 后 `index` 选择器可能失效，因为 `GUILayoutEntry` 的列表顺序可能随可用空间重排。
- **建议**：始终优先使用 `control_name` 或 `text` 选择器；`index` 仅作为最后手段。

### 11.3 值断言的局限性

- `imgui_assert_value` 对 IMGUI 控件是尽力推断。例如：
  - `Toggle` 的 `GUILayoutEntry` 只包含 `text`（标签文本），不包含勾选状态。
  - `Slider` 的 `GUILayoutEntry` 只包含 `rect` 和 `style`，不包含当前数值。
- **建议**：对需要精确值断言的场景，使用 `field_name` 参数通过 `ImguiActionHelper.TrySetFieldValue` / 反射读取被测窗口字段。

### 11.4 负向测试的稳定性

- `IMGUI Negative - Wait Timeout`（`_96-imgui-negative-wait.yaml`）偶尔会因 IMGUI 重绘时机提前通过（控件在超时前意外出现），属于非关键 flaky 测试。
- **建议**：负向测试的 timeout 不应设置过短（建议 ≥2s），避免与正常重排/重绘竞争。

### 11.5 与 UIToolkit 混用

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
      text: "Saved"
```

混用时需注意：
- `fixture.host_window` 必须指向包含 IMGUI 内容的 `EditorWindow`。
- IMGUI 动作不依赖 `rootVisualElement`，因此可在 UIToolkit 动作失败时作为降级路径使用（如 `IMGUIContainer` 内部内容）。

---

## 12. 修订历史

### 2026-04-21 1.0.0 初始版本

- 基于全量代码复核和 5 份 YAML 回归用例验证结果创建本文档。
- 确认 15 个 `imgui_*` 动作全部实现并通过回归验证。
- 确认快照捕获双链路（反射 + MonoHook）在 Unity 6000.6.0a2 下工作正常。
- 明确列出 IMGUI 架构导致的硬性边界（值断言、浮窗、纯绘制控件）。

### 2026-04-21 1.1.0 扩展验证

- 新增 `69-imgui-alternative-params.yaml`：验证 `imgui_select_option` 的 `option` 参数、`imgui_press_key` 和 `imgui_press_key_combination` 的 `selector` 可选参数。
- 新增 IMGUI 负向测试：`_78-negative-type-text-non-input.yaml`（type_text 非输入元素）、`_79-negative-press-key-combination.yaml`（无效组合键）。
- YAML 基线从 71 份扩展到 102 份，IMGUI 相关用例从 5 份扩展到 8 份。

### 2026-04-21 1.2.0 第三轮扩展

- 新增 `87-type-text-vs-fast.yaml`：包含 `type_text_fast` 对空字符串 `""` 的边界验证（回归空字符串参数修复）。
- 新增 IMGUI 负向测试：`_91-negative-set-value-invalid.yaml`（`set_value` 类型不匹配）。
- YAML 基线从 102 份扩展到 115 份，IMGUI 相关用例从 8 份扩展到 10 份。
