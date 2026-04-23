# UIToolkit 控件自动化覆盖与限制说明

版本：2.0.0
日期：2026-04-23
状态：已全量验证（115 份 YAML 全部执行通过，含负向测试）

---

## 1. 文档目标

本文档列出 UnityUIFlow V1 对 UIToolkit 所有控件类型的自动化覆盖范围，明确以下分类：
- **全面支持**：可通过 YAML 声明式步骤完成定位、交互、赋值、断言。
- **局部支持**：可定位和部分交互，但部分高级功能需 C# Page Object 编程。
- **仅定位/断言**：可通过选择器定位和读取属性，但交互必须通过 C# 编程。
- **不可自动化**：官方 API 不开放或控件特性导致无法通过任何自动化手段操作。

当前示例验收基线：
- `Assets/Examples/Yaml` 已扩展到 **115 份** YAML（含示例用例 `01-35`、`41-44`、场景扩展 `50-57`、`66-90`、IMGUI 用例 `97-99`、Host Window 用例、条件/循环用例、高级参数别名、负向测试等）。
- **全量验证结果**：正向测试全部通过 / 负向测试按设计失败（预期行为）/ 0 份错误。

---

## 2. 何时用 YAML 测试用例，何时用 C# 代码测试用例

### 2.1 优先选择 YAML 测试用例的场景

| 场景 | 原因 |
|------|------|
| 端到端用户流程（点击 → 输入 → 断言） | YAML 步骤与用户操作一一对应，可读性高 |
| 控件可见性、文本、值断言 | `assert_visible`、`assert_text`、`assert_value` 覆盖 90% 以上的验证需求 |
| 表单填写（TextField、Toggle、Dropdown、Slider） | `set_value`、`type_text_fast`、`select_option` 直接支持 |
| 数据驱动测试（多组输入参数） | 内建 `data.rows` / `from_csv` / `from_json` 数据源支持 |
| 截图回归测试 | `screenshot` 动作内建，无需 C# 代码 |
| 有条件步骤（如"若元素存在则点击"） | `if: exists/not_exists` + `repeat_while` 控制流 |
| 菜单操作（上下文菜单、弹出菜单） | `open_context_menu`、`select_context_menu_item`、`menu_item` 专用动作 |
| 键盘快捷键（Ctrl+S、Ctrl+A 等） | `press_key_combination` 内建 |
| 跨多窗口无关的单窗口 EditorWindow 测试 | Fixture 仅需声明 `host_window.type` |

### 2.2 需要 C# 代码测试用例的场景

| 场景 | 原因 |
|------|------|
| 操作系统弹窗（Color Picker、Curve Editor、Object Picker 浮窗） | 独立 `EditorWindow`，不在被测窗口 UI 树内，YAML 无法定位 |
| 需要访问 `SerializedObject`/`SerializedProperty` 的复杂验证 | YAML 动作层不暴露序列化对象 API |
| 自定义 `PropertyDrawer` 内部布局的精细断言 | 动态生成的子元素路径不稳定，需 C# 运行时遍历 |
| 真实文件拖放（`DragAndDrop` 生命周期） | 依赖 Editor `DragAndDrop` API，不等价于 UIToolkit 指针事件 |
| 多窗口协同（跨窗口拖拽、跨窗口断言） | V1 测试模型按单宿主窗口组织 |
| 验证业务逻辑副作用（C# 状态变化、ScriptableObject 修改） | 需要直接访问被测对象实例 |
| IME/输入法组合输入的精确验证 | InputSystem 测试 API 不覆盖 IME 组合态 |
| 性能基准测试（帧率、GC 分配） | 需要 `ProfilerRecorder` 等 C# API |
| 像素级视觉对比 | 需要引入图像处理库，框架当前无内建像素 diff |
| 自定义 `EditorWindow` 的非标准事件处理 | 需要直接反射或调用被测窗口的内部方法 |

### 2.3 YAML + C# 混合场景

- **推荐模式**：C# Fixture 子类覆盖 `SetUp`/`TearDown` 做数据准备和清理，YAML 负责 UI 交互步骤。
- 在 C# Fixture 中调用 `await ExecuteYamlStepsAsync(yamlContent)` 注入 YAML 流程。
- 在 C# Fixture 中调用 `await ExecuteActionAsync(action, parameters)` 注入单步骤动作。

---

## 3. V1 全面支持的控件

以下控件可通过 YAML 内置动作完成完整的定位、交互、赋值和断言：

| 控件 | 可用 YAML 动作 | 赋值方式 | 断言方式 |
| --- | --- | --- | --- |
| `Button` | `click`、`double_click`、`hover` | 不适用（无 value） | `assert_visible`、`assert_text`、`assert_enabled`、`assert_disabled` |
| `RepeatButton` | `click`、`hover` | 不适用 | `assert_visible`、`assert_text`、`assert_enabled` |
| `Label` | `hover` | 不适用（纯显示） | `assert_visible`、`assert_text`、`assert_text_contains`、`assert_property` |
| `Toggle` | `click`、`set_value` | `set_value`（`true`/`false`） | `assert_value`、`assert_property` |
| `TextField` | `type_text`、`type_text_fast`、`set_value`、`click`、`focus` | `type_text`、`type_text_fast`、`set_value` | `assert_text`、`assert_value`、`assert_property` |
| `IntegerField` | `type_text_fast`、`set_value`、`click`、`focus` | `set_value`（整数文本） | `assert_value`、`assert_property` |
| `LongField` | 同 `IntegerField` | `set_value`（长整数文本） | 同上 |
| `FloatField` | 同 `IntegerField` | `set_value`（浮点文本） | 同上 |
| `DoubleField` | 同 `IntegerField` | `set_value`（浮点文本） | 同上 |
| `UnsignedIntegerField` | 同 `IntegerField` | `set_value`（无符号整数文本） | 同上 |
| `UnsignedLongField` | 同 `IntegerField` | `set_value`（无符号长整数文本） | 同上 |
| `Slider` | `set_slider`、`set_value`、`drag`、`click` | `set_slider`（float value）、`set_value` | `assert_value`、`assert_property` |
| `SliderInt` | 同 `Slider` | 同 `Slider` | 同上 |
| `DropdownField` | `select_option`、`click` | `select_option`（by text/index）、`set_value` | `assert_value`、`assert_property` |
| `PopupField<string>` | `select_option`、`click_popup_item`、`click` | `select_option`、`set_value` | `assert_value`、`assert_property` |
| `EnumField` | `select_option`、`click` | `select_option`（by enum name/index）、`set_value` | `assert_value` |
| `RadioButton` | `click`、`set_value` | `set_value`（`true`/`false`） | `assert_value` |
| `RadioButtonGroup` | `select_option`、`set_value` | `select_option`（按索引） | `assert_value` |
| `Foldout` | `toggle_foldout`、`click`、`set_value` | `toggle_foldout`、`set_value`（`true`/`false`） | `assert_value`、`assert_property` |
| `ScrollView` | `scroll`、`drag` | 不适用 | `assert_visible`、`assert_property` |
| `ListView` | `select_list_item`（`index`/`indices`）、`drag_reorder`、`scroll`、`click` | `select_list_item` | `assert_property`（`selectedIndex`/`selectedIndices`） |
| `TreeView` | `select_tree_item`（`id`/`index`）、`scroll`、`click` | `select_tree_item` | `assert_property`（`selectedIndex`） |
| `Vector2Field` | `set_value`、`focus` | `set_value`（`"x,y"`） | `assert_value` |
| `Vector3Field` | `set_value`、`focus` | `set_value`（`"x,y,z"`） | `assert_value` |
| `Vector4Field` | `set_value`、`focus` | `set_value`（`"x,y,z,w"`） | `assert_value` |
| `Vector2IntField` | `set_value`、`focus` | `set_value`（`"x,y"`） | `assert_value` |
| `Vector3IntField` | `set_value`、`focus` | `set_value`（`"x,y,z"`） | `assert_value` |
| `RectField` | `set_value`、`focus` | `set_value`（`"x,y,w,h"`） | `assert_value` |
| `RectIntField` | `set_value`、`focus` | `set_value`（`"x,y,w,h"`） | `assert_value` |
| `BoundsField` | `set_value`、`focus` | `set_value`（`"cx,cy,cz,ex,ey,ez"`） | `assert_value` |
| `BoundsIntField` | `set_value`、`focus` | `set_value`（`"px,py,pz,sx,sy,sz"`） | `assert_value` |
| `MinMaxSlider` | `set_slider`（`min_value`/`max_value`）、`set_value`、`drag` | `set_slider`、`set_value` | `assert_value` |
| `Hash128Field` | `set_value`、`focus` | `set_value`（32 位十六进制字符串） | `assert_value` |
| `ProgressBar` | 无（纯显示） | 不适用 | `assert_property`（`value`、`title`） |
| `Image` | `click`、`hover` | 不适用（纯显示） | `assert_visible`、`assert_property` |
| `HelpBox` | 无（纯显示） | 不适用 | `assert_visible`、`assert_text` |
| `Box` | `click`、`hover` | 不适用（纯容器） | `assert_visible` |
| `GroupBox` | `click`、`hover` | 不适用（纯容器） | `assert_visible` |
| `VisualElement` | `click`、`hover`、`drag` | 不适用（基础容器） | `assert_visible`、`assert_property` |
| `TabView` | `select_tab`、`close_tab` | 不适用（容器/导航） | `assert_visible`、`assert_property` |
| `Toolbar` | `hover` | 不适用（纯容器） | `assert_visible` |
| `ToolbarButton` | `click`、`double_click`、`hover` | 不适用（继承自 `Button`） | `assert_visible`、`assert_text`、`assert_enabled` |
| `ToolbarToggle` | `click`、`set_value` | `set_value`（`true`/`false`，继承自 `Toggle`） | `assert_value`、`assert_property` |
| `ToolbarSearchField` | `type_text`、`type_text_fast`、`set_value`、`click`、`focus` | `type_text`、`type_text_fast`、`set_value` | `assert_text`、`assert_value`、`assert_property` |

---

## 4. V1 局部支持的控件

| 控件 | 可用 YAML 动作 | 不支持的交互 | 原因与替代方案 |
| --- | --- | --- | --- |
| `EnumFlagsField` | `select_option`（`value`/`index`/`indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 弹出面板真实逐项点击 | 弹出面板为独立浮窗；`select_option` 整值直写 / `toggle_mask_option` 单位 toggle / `click_popup_item` 字段值操作 为替代方案 |
| `MaskField` | `select_option`（`value`/`index`/`indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 弹出面板真实逐项点击 | 同上 |
| `LayerMaskField` | `select_option`（`value`/`index`/`indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 弹出面板逐项操作 | 同上 |
| `ColorField` | `set_value`、`assert_value`、`click` | 拾色器面板交互、Eye Dropper | 拾色器为独立 `EditorWindow`；使用 `set_value`/`assert_value` 直写 `Color` 值（支持 `#RRGGBB`/`#RRGGBBAA` 十六进制格式） |
| `MultiColumnListView` | `select_list_item`（`index`/`indices`）、`sort_column`、`resize_column`、`scroll`、`click` | 真实 UI 拖拽排序/列宽 | `sort_column` 直接写 `sortColumnDescriptions`；`resize_column` 直接设置 `Column.width` |
| `MultiColumnTreeView` | `select_tree_item`（`id`/`index`）、`sort_column`、`resize_column`、`scroll`、`click` | 真实 UI 拖拽排序/列宽 | 同上 |
| `TwoPaneSplitView` | `drag`（拖拽分割条）、`set_split_view_size` | 跨窗口拖分割条联动 | `set_split_view_size` 按 `pane`（0/1）指定目标尺寸（像素） |
| `Scroller` | `set_value`、`click`、`drag`、`page_scroller`、`drag_scroller` | 无 | `drag_scroller` 支持按 `ratio` 或方向/距离拖拽 thumb；`page_scroller` 支持分页方向与步数 |
| `TagField` | `select_option`（`value`/`index`）、`click_popup_item`、`assert_value` | 弹出菜单内部逐项点击 | 继承自 `PopupField<string>`，`select_option` / `click_popup_item` 直写设定值 |
| `LayerField` | `select_option`（`value`/`index`）、`click_popup_item`、`assert_value` | 弹出菜单内部逐项点击 | 同上 |
| `ObjectField` | `set_value`（`guid:`/`path:`/`name:`/`asset-name:`/`search:`/`search:TypeName:Needle`）、`assert_value`、`assert_property` | Object Picker 对话框、真实拖放赋值 | 直接加载资产并赋值；对象选择浮窗与真实 DragAndDrop 不可自动化 |
| `CurveField` | `set_value`（键帧 DSL：`time:value:inTangent:outTangent;...`）、`assert_value`、`assert_property` | 曲线编辑器窗口交互 | 键帧 DSL 可直写完整 `AnimationCurve`；曲线编辑器浮窗不在被测树内 |
| `GradientField` | `set_value`（渐变 DSL：`time:#RRGGBBAA;...|time:alpha;...`）、`assert_value`、`assert_property` | 渐变编辑器窗口交互 | 渐变 DSL 可直写完整 `Gradient`；渐变编辑器浮窗不在被测树内 |
| `ToolbarMenu` | `click`、`open_popup_menu`、`select_popup_menu_item`、`assert_menu_item`、`assert_menu_item_disabled`、`menu_item` | 直接定位弹出浮窗内部 VisualTree | 官方 `PopupMenuSimulator` + `DropdownMenu` reflection fallback 支持菜单项选择与断言 |
| `ToolbarPopupSearchField` | `type_text`、`type_text_fast`、`set_value`、`assert_value`、`click`、`focus` | 弹出菜单内部项点击 | 搜索文本输入本体可自动化；弹出结果菜单仍为独立浮窗 |
| `ToolbarBreadcrumbs` | `click`、`navigate_breadcrumb`、`read_breadcrumbs` | 弹出式导航菜单 | 已支持按 `label`/`index` 导航及自动枚举（`read_breadcrumbs`）；弹出式历史路径菜单仍属独立浮窗边界 |
| `PropertyField` | `set_bound_value`、`assert_bound_value`，以及通过后代控件执行 `click`/`set_value`/`assert_value` | 直接对未绑定或无稳定 `bindingPath` 的复杂子结构做统一赋值 | 按 `binding_path` 统一语义赋值；若子树无稳定绑定路径仍需回退到后代控件定位 |
| `InspectorElement` | `set_bound_value`、`assert_bound_value`，以及通过后代控件执行 `click`/`set_value`/`assert_value` | 完全脱离 Inspector 生成结构做任意字段推断 | 按 `binding_path` 穿透到 Inspector 绑定字段 |

---

## 5. V1 不可自动化的控件与功能

| 控件/功能 | 原因 | 建议 |
| --- | --- | --- |
| `IMGUIContainer` 内部控件 | IMGUI 内容不进入 UIToolkit VisualTree，选择器无法拿到内部元素 | 将被测 UI 迁移到纯 UIToolkit 实现；或使用 `imgui_*` IMGUI 专属动作 |
| `ProjectSettingsProvider`（项目设置面板） | 当前实现使用 `EditorGUILayout`（IMGUI），不生成 UIToolkit 子元素 | 属于 Unity 技术边界；需重写为 UIToolkit 才能覆盖 |
| `ColorField` 的 Color Picker / Eye Dropper | 弹出独立编辑器窗口 | 使用 `set_value` 直写颜色值 |
| `CurveField` 曲线编辑器浮窗 | 独立编辑器窗口 | 使用键帧 DSL 直写 |
| `GradientField` 渐变编辑器浮窗 | 独立编辑器窗口 | 使用渐变 DSL 直写 |
| Object Picker 对话框 | Unity 资产选择对话框不在当前窗口树内 | 使用 `set_value` 的 `guid:`/`path:` 策略直接赋值 |
| Tooltip 浮窗 | 由 Editor 全局管理，不在目标窗口 UI 树内 | 可通过 `assert_property` 断言 `tooltip` 属性值，但无法验证可视渲染 |
| `ToolbarPopupSearchField` 弹出结果菜单 | 结果面板不在被测窗口树内，官方未暴露稳定结果项遍历 API | 搜索输入本体可自动化；结果项选择需 C# 编程 |
| 多窗口协同 | V1 每个测试仅支持一个宿主窗口 | 单窗口设计；跨窗口测试需拆为多个独立用例 |
| 剪贴板操作（Copy/Paste） | 需要系统级剪贴板 API + 键盘快捷键组合 | 使用 `set_value` 快速路径替代粘贴 |
| IME / 输入法组合输入 | InputSystem 测试 API 不覆盖 IME 组合态 | 使用 `type_text_fast` 直接写入最终文本 |
| 拖放文件到控件 | 依赖 Editor `DragAndDrop` 生命周期，不等价于 UIToolkit 事件 | C# 编程模拟 `DragAndDrop` |
| 动态生成的浮窗式 UI | `ShowAsDropdown`、`GenericMenu.ShowAsContext` 等创建的浮窗不在被测树内 | 若可绕过弹出直接 `set_value` 则使用直接写值方案 |
| 像素级视觉对比 | 无内建视觉基线与差异分析链路 | P5 扩展 |

---

## 6. 全部内置动作详细说明

### 6.1 鼠标/指针动作

| 动作名 | 关键参数 | 底层机制 | YAML 示例 |
|--------|---------|---------|-----------|
| `click` | `selector`、`button`（left/right/middle）、`modifiers`（shift/ctrl/alt/cmd） | `DispatchClick` → `PointerDownEvent`+`PointerUpEvent`；`Button` 子类额外五级 fallback | `- click: { selector: "#ok-btn" }` |
| `double_click` | 同 `click`，`clickCount=2` | 同 `click` | `- double_click: { selector: "#item" }` |
| `hover` | `selector`、`duration`（毫秒字符串） | `MouseMoveEvent` 到元素中心 | `- hover: { selector: "#tooltip-target", duration: "200ms" }` |
| `drag` | `from`（选择器或 `"x,y"`）、`to`（同上）、`duration`、`button`、`modifiers` | `MouseDown` → 多帧 `MouseMove`（线性插值）→ `MouseUp`；同时派发 `PointerDown/Up` | `- drag: { from: "#thumb", to: "#target" }` |
| `scroll` | `selector`、`delta_x`、`delta_y` | `WheelEvent` 到元素中心 | `- scroll: { selector: "#scroll-view", delta_y: -100 }` |
| `open_context_menu` | `selector`、`modifiers` | 右键点击元素（`button=1`） | `- open_context_menu: { selector: "#item" }` |
| `select_context_menu_item` | `value`（菜单项文本，可用 `item` 别名） | 官方 `ContextMenuSimulator` + `FloatingPanelLocator` fallback | `- select_context_menu_item: { value: "Delete" }` |
| `open_popup_menu` | `selector`、`modifiers` | 左键点击元素打开弹出菜单 | `- open_popup_menu: { selector: "#toolbar-menu" }` |
| `select_popup_menu_item` | `value`（菜单项文本，可用 `item` 别名） | `FloatingPanelLocator` 遍历浮层面板 | `- select_popup_menu_item: { value: "New Item" }` |
| `assert_menu_item` | `value`/`item`（菜单项文本） | 断言菜单项存在且已启用 | `- assert_menu_item: { value: "Copy" }` |
| `assert_menu_item_disabled` | `value`/`item` | 断言菜单项存在且已禁用 | `- assert_menu_item_disabled: { value: "Paste" }` |
| `menu_item` | `item`（Unity 菜单路径）、`type`（select/validate） | `EditorApplication.ExecuteMenuItem`；`kind=popup/context/auto` | `- menu_item: { item: "Edit/Undo" }` |
| `click_popup_item` | `selector`、`value`/`index` | 直接操作字段值（`MaskField`/`EnumFlagsField`/`PopupField`）；不依赖浮层面板点击 | `- click_popup_item: { selector: "#mask", value: "Layer 1" }` |

### 6.2 键盘动作

| 动作名 | 关键参数 | 底层机制 | YAML 示例 |
|--------|---------|---------|-----------|
| `type_text` | `selector`、`value`（要输入的文本） | 逐字符：官方 UIToolkit 驱动 → InputSystem `SendText` → `KeyDownEvent`+`KeyUpEvent` → `set_value` 直写兜底 | `- type_text: { selector: "#search", value: "hello" }` |
| `type_text_fast` | `selector`、`value` | 直接赋值 `.value` 属性（不模拟键盘事件）；支持空字符串 `""` 清空 | `- type_text_fast: { selector: "#name", value: "Alice" }` |
| `press_key` | `selector`（可选）、`key`（KeyCode 枚举名，如 `Return`、`Escape`、`Delete`、`Tab`） | 官方驱动 → InputSystem `PressKey` → `KeyDownEvent`+`KeyUpEvent` | `- press_key: { key: "Return" }` |
| `press_key_combination` | `keys`（如 `"Ctrl+A"`、`"Ctrl+Shift+Z"`） | 解析修饰键；依次 `KeyDown` → 主键（带修饰符）→ `ExecuteCommandEvent`/`ValidateCommandEvent`（对已知命令）→ 主键 `KeyUp` → 修饰键 `KeyUp` | `- press_key_combination: { keys: "Ctrl+Z" }` |
| `execute_command` | `command`（UIToolkit 命令名，如 `Copy`、`Paste`、`SelectAll`、`Delete`、`Undo`、`Redo`） | `ExecuteCommandEvent` 派发到当前聚焦元素 | `- execute_command: { command: "SelectAll" }` |
| `validate_command` | `command` | `ValidateCommandEvent` 派发到当前聚焦元素 | `- validate_command: { command: "Copy" }` |
| `focus` | `selector` | `element.Focus()` | `- focus: { selector: "#input" }` |

### 6.3 值赋值动作

| 动作名 | 关键参数 | 支持的控件类型 | YAML 示例 |
|--------|---------|--------------|-----------|
| `set_value` | `selector`、`value`（字符串） | 所有 `BaseField<T>` 子类（TextField、Toggle、IntegerField、FloatField、DropdownField、Slider、Vector*、Rect*、Bounds*、ColorField、CurveField、GradientField、ObjectField、Hash128Field 等） | `- set_value: { selector: "#slider", value: "0.75" }` |
| `set_slider` | `selector`、`value`（单值）或 `min_value`+`max_value`（双值，MinMaxSlider） | `Slider`、`SliderInt`、`MinMaxSlider` | `- set_slider: { selector: "#volume", value: "0.5" }` |
| `select_option` | `selector`、`value`/`index`/`indices`（逗号分隔多索引） | `DropdownField`、`EnumField`、`EnumFlagsField`、`RadioButtonGroup`、`MaskField`、`LayerMaskField`、`TagField`、`LayerField`、`PopupField<string>` | `- select_option: { selector: "#quality", value: "High" }` |
| `toggle_mask_option` | `selector`、`value`/`index`（单项） | `EnumFlagsField`、`MaskField`、`LayerMaskField` | `- toggle_mask_option: { selector: "#flags", value: "OptionA" }` |
| `toggle_foldout` | `selector`、`expand`（可选，`true`/`false`） | `Foldout` | `- toggle_foldout: { selector: "#settings" }` |
| `select_tab` | `selector`（TabView）、`label`/`index` | `TabView` | `- select_tab: { selector: "#tabs", label: "Settings" }` |
| `close_tab` | `selector`（TabView）、`label`/`index` | `TabView` | `- close_tab: { selector: "#tabs", index: 0 }` |
| `select_list_item` | `selector`、`index`（单选）或 `indices`（多选，逗号分隔） | `ListView`、`MultiColumnListView` | `- select_list_item: { selector: "#list", index: 2 }` |
| `drag_reorder` | `selector`、`from_index`、`to_index` | `ListView` | `- drag_reorder: { selector: "#list", from_index: 0, to_index: 3 }` |
| `select_tree_item` | `selector`、`id`/`index` | `TreeView`、`MultiColumnTreeView` | `- select_tree_item: { selector: "#tree", id: "node-1" }` |
| `set_bound_value` | `selector`、`binding_path`、`value` | `PropertyField`、`InspectorElement` 以及任何有绑定路径的后代控件 | `- set_bound_value: { selector: "#inspector", binding_path: "speed", value: "10.5" }` |
| `navigate_breadcrumb` | `selector`（BreadcrumbBar）、`label`/`index` | `ToolbarBreadcrumbs` | `- navigate_breadcrumb: { selector: "#breadcrumbs", label: "Root" }` |
| `read_breadcrumbs` | `selector`（BreadcrumbBar）、`bag_key`（存入 SharedBag） | `ToolbarBreadcrumbs` | `- read_breadcrumbs: { selector: "#breadcrumbs", bag_key: "crumbs" }` |
| `set_split_view_size` | `selector`（TwoPaneSplitView）、`pane`（0/1）、`size`（像素） | `TwoPaneSplitView` | `- set_split_view_size: { selector: "#split", pane: 0, size: 300 }` |
| `page_scroller` | `selector`（ScrollView/Scroller）、`direction`（up/down/left/right）、`count`（页数）、`page_size`（可选，像素） | `ScrollView`、`Scroller` | `- page_scroller: { selector: "#scroll", direction: "down", count: 2 }` |
| `drag_scroller` | `selector`（Scroller）、`ratio`（0~1）或 `direction`+`distance` | `Scroller` | `- drag_scroller: { selector: "#vscroll", ratio: 0.5 }` |
| `sort_column` | `selector`（MultiColumn*）、`column`（名称/标题）或 `index`、`direction`（ascending/descending） | `MultiColumnListView`、`MultiColumnTreeView` | `- sort_column: { selector: "#table", column: "Name", direction: "ascending" }` |
| `resize_column` | `selector`（MultiColumn*）、`column`/`index`、`width`（像素） | `MultiColumnListView`、`MultiColumnTreeView` | `- resize_column: { selector: "#table", column: "Size", width: 120 }` |

### 6.4 断言动作

| 动作名 | 关键参数 | 说明 |
|--------|---------|------|
| `assert_visible` | `selector`、`timeout` | 断言元素可见（display≠none、visibility≠hidden、opacity>0），支持超时轮询 |
| `assert_not_visible` | `selector`、`timeout` | 断言元素不可见 |
| `wait_for_element` | `selector`、`timeout` | 等待元素出现（与 `assert_visible` 相同，但语义强调等待） |
| `assert_text` | `selector`、`expected` | 断言 `.text` / `.value`（字符串）精确等于 `expected`（忽略大小写） |
| `assert_text_contains` | `selector`、`expected` | 断言文本包含 `expected`（忽略大小写） |
| `assert_value` | `selector`、`expected` | 断言元素 `.value` 的字符串表示。支持类型级比较：`Color`（#RRGGBBAA）、`Vector*`、`Rect*`、`Bounds*`、`Hash128`、枚举名称等 |
| `assert_bound_value` | `selector`、`binding_path`、`expected` | 断言绑定字段的值 |
| `assert_property` | `selector`、`property`（属性名，如 `display`、`value`、`name`、`visible`、`tabIndex`、`lowValue`、`highValue`）、`expected` | 断言 USS 样式属性或 UIElement 反射属性 |
| `assert_enabled` | `selector` | 断言元素未被 `SetEnabled(false)` 禁用 |
| `assert_disabled` | `selector` | 断言元素已被 `SetEnabled(false)` 禁用 |

### 6.5 工具/控制流动作

| 动作名 | 关键参数 | 说明 |
|--------|---------|------|
| `screenshot` | `tag`（文件名标签） | 截取当前宿主窗口截图；路径写入 `CurrentAttachments` |
| `wait` | `duration`（如 `"500ms"`、`"2s"`；上限 600s） | 等待固定时长 |

---

## 7. 输入模拟支持详情

### 7.1 鼠标/指针输入

| 操作 | 支持情况 | 技术机制 |
|------|---------|---------|
| 左键单击 | ✅ 完整支持 | `PointerDownEvent`+`PointerUpEvent`；`Button` 子类额外 `SendClickEvent`→`Button.clicked`→`Clickable.clicked`→`SimulateSingleClick`→`ClickEvent` 五级 fallback |
| 左键双击 | ✅ 完整支持 | `clickCount=2` 的 `PointerDownEvent`+`PointerUpEvent` |
| 右键单击 | ✅ 完整支持 | `button=1` 的 `PointerDownEvent`+`PointerUpEvent` |
| 中键单击 | ✅ 支持 | `button=2` 的 `PointerDownEvent`+`PointerUpEvent` |
| 修饰键+点击（Shift/Ctrl/Alt/Cmd） | ✅ 完整支持 | `EventModifiers` 参数传递到 `PointerDownEvent` |
| 悬停（MouseMove） | ✅ 完整支持 | `MouseMoveEvent` 到元素中心；支持持续时长 |
| 拖拽（两点间） | ✅ 完整支持 | 分帧线性插值 `MouseMove`；支持 `button`（0/1/2）和 `modifiers` |
| 滚轮 | ✅ 完整支持 | `WheelEvent` 的 `delta.x`/`delta.y` |
| 跨窗口拖拽 | ❌ 不支持 | V1 单窗口限制 |
| 文件/对象拖放 | ❌ 不支持 | 依赖 Editor `DragAndDrop` 生命周期 |
| 鼠标坐标精确定位 | ✅ 支持（`from`/`to` 参数可直接传 `"x,y"` 窗口坐标） | 元素中心自动计算；或显式坐标输入 |

### 7.2 键盘输入

| 操作 | 支持情况 | 技术机制 |
|------|---------|---------|
| 单字符文本输入 | ✅ 完整支持 | `type_text`：逐字符 `KeyDownEvent`+`KeyUpEvent`；官方驱动优先，InputSystem 次之，UIToolkit 事件兜底 |
| 批量文本快速写入 | ✅ 完整支持 | `type_text_fast`：直接赋值 `.value`；支持空字符串 `""` |
| 功能键（Enter/Escape/Delete/Tab/方向键等） | ✅ 完整支持 | `press_key`：官方驱动 → InputSystem `PressKey` → `KeyDownEvent`+`KeyUpEvent` |
| 组合键（Ctrl+A/Ctrl+C/Ctrl+S/Ctrl+Z 等） | ✅ 完整支持 | `press_key_combination`：依次发送修饰键 `KeyDown`→主键（带 `modifiers`）→ 已知命令的 `ExecuteCommandEvent`→主键 `KeyUp`→修饰键 `KeyUp` |
| UIToolkit 命令（Copy/Paste/SelectAll/Undo/Redo/Delete） | ✅ 完整支持 | `execute_command`：`ExecuteCommandEvent` 派发 |
| UIToolkit 命令验证 | ✅ 完整支持 | `validate_command`：`ValidateCommandEvent` 派发 |
| 焦点切换（Tab 导航） | ❌ 不支持 | V1 不支持 Tab 焦点链遍历；可用 `focus` 动作直接聚焦指定元素 |
| IME/输入法组合输入 | ❌ 不支持 | InputSystem 测试 API 不覆盖 IME 组合态 |
| 系统剪贴板真实 Ctrl+V | ❌ 不支持 | 需要平台级剪贴板 API；使用 `set_value` 替代 |

---

## 8. 选择器语法完整参考

```yaml
# 基本选择器
"#button-id"               # 按 name/id（等价 VisualElement.name）
".my-class"                # 按 USS 类名
"Button"                   # 按 UIToolkit 控件类型名
"VisualElement"            # 基础类型（匹配所有）

# 组合选择器
"#panel Label"             # 后代（空格）
"#panel > Label"           # 直接子元素
"#panel > .item > Button"  # 多级 child

# 属性选择器
"[name=my-button]"         # 属性等值
"[data-role=primary]"      # userData Dictionary 桥接
"[data-testid=foo]"        # 自定义测试 ID

# 伪类
"Button:hover"             # 伪类（支持范围有限）
```

**选择器优先级**（框架内部）：
1. 快速路径：`#id`（UQuery `.Q<T>(name)`）、`.class`（UQuery `.Q<T>(className:`...`)`）
2. 全树遍历：类型 + 属性组合
3. 浮层面板搜索：`FloatingPanelLocator` 枚举所有 panel（用于菜单/popup 内元素）

---

## 9. IMGUI 自动化支持

> IMGUI（Immediate Mode GUI）自动化已作为独立子系统实现，详细覆盖范围、动作列表、选择器语法、设计原理与限制说明，请参见《IMGUI 控件自动化覆盖与限制说明.md》。

IMGUI 动作（`imgui_*`）与 UIToolkit 动作可在同一 YAML 中混用。简要信息：

- **15 个 IMGUI 动作**：`imgui_click`、`imgui_double_click`、`imgui_right_click`、`imgui_hover`、`imgui_type`、`imgui_focus`、`imgui_scroll`、`imgui_select_option`、`imgui_press_key`、`imgui_press_key_combination`、`imgui_read_value`、`imgui_assert_text`、`imgui_assert_visible`、`imgui_assert_value`、`imgui_wait`。
- **选择器语法**：`gui(button)`、`gui(textfield, control_name="xxx")`、`gui(group="Settings" > button, text="Apply")`、`gui(focused)`。
- **验收基线**：`Assets/Examples/Yaml/99-imgui-example.yaml`、`98-imgui-advanced.yaml`、`97-imgui-negative-assert.yaml` 等 8 份用例已全部通过验证。

---

## 10. 按交互类型的覆盖总结

| 交互类型 | V1 覆盖范围 | 未覆盖范围 |
| --- | --- | --- |
| 单击 | 所有可点击控件；菜单项通过 `select_context_menu_item`/`select_popup_menu_item`/`menu_item` 专用动作；`com.unity.ui 2.0.0` 下自动降级到 `DispatchClick`+五级 fallback | 独立浮窗中的通用选择器直接点击 |
| 双击 | 所有可点击控件 | 无 |
| 悬停 | 所有可见控件 | Tooltip 可视渲染验证 |
| 拖拽 | 任意两元素间；`TwoPaneSplitView` 分割条；`Scroller` thumb；`drag_reorder` 列表重排 | 跨窗口拖拽、文件拖放 |
| 滚动 | `ScrollView`、`ListView`、`TreeView`、`Scroller` | 自定义吸附滚动高级行为 |
| 文本输入（真实键盘） | `TextField` 及所有子类 | IME 组合输入 |
| 快速写值 | 所有 `BaseField<T>` 子类的字符串/数值/枚举/向量/矩形/边界/颜色/哈希/CurveField/GradientField/ObjectField | 对象选择器浮窗、曲线/渐变编辑器浮窗 |
| 选择（下拉/单选） | `DropdownField`、`PopupField<string>`、`EnumField`、`EnumFlagsField`、`RadioButtonGroup`、`MaskField`、`LayerMaskField`、`TagField`、`LayerField` | 弹出面板逐项交互 |
| 列表选择 | `ListView`、`MultiColumnListView`（单选+多选） | 无 |
| 树选择 | `TreeView`、`MultiColumnTreeView` | 展开/折叠动画内部状态 |
| 列排序 | `MultiColumnListView`、`MultiColumnTreeView`（`sort_column`） | `ColumnSortingMode.Custom` 的自定义排序回调 |
| 列宽调整 | `MultiColumnListView`、`MultiColumnTreeView`（`resize_column`） | 无 |
| Split 拖拽 | `TwoPaneSplitView`（分割条锚点 `drag` + `set_split_view_size`） | 复杂跨窗口 split 联动 |
| Scroller 赋值 | `Scroller`（`set_value`、`page_scroller`、`drag_scroller`） | 无 |
| 折叠/展开 | `Foldout` | 无 |
| 滑块 | `Slider`、`SliderInt`、`MinMaxSlider` | 无 |
| Tab 切换 | `TabView`（`select_tab`、`close_tab`） | 无 |
| Toolbar 控件 | `ToolbarButton`、`ToolbarToggle`、`ToolbarSearchField`、`ToolbarMenu`（官方 `PopupMenuSimulator`+`DropdownMenu` fallback）、`ToolbarPopupSearchField`（输入本体）、`ToolbarBreadcrumbs`（`navigate_breadcrumb`、`read_breadcrumbs`） | `ToolbarPopupSearchField` 弹出结果列表 |
| 按键 | 任意获焦元素；`press_key`、`press_key_combination`、`type_text`、`type_text_fast`、指针类 `modifiers` 组合 | IME、系统剪贴板真实粘贴 |
| 焦点 | 所有可聚焦元素（`focus` 动作） | 焦点链导航（Tab 切换焦点） |
| 断言 | 可见性、文本、value、property、enabled/disabled、bound value | 视觉像素对比 |
| 截图 | 当前窗口 | 多窗口截图 |
| 等待 | `wait`（固定时长）、`wait_for_element`（轮询等待，最长 600s） | 无 |

---

## 11. 不能实现或当前受 Unity 接口边界阻断

| 项目 | 当前阻断原因 | 结论 |
| --- | --- | --- |
| `IMGUIContainer` 内部控件级自动化 | IMGUI 内容不进入 UIToolkit VisualTree | 属于 Unity 技术边界 |
| `ProjectSettingsProvider`（项目设置面板） | 当前实现使用 `EditorGUILayout`（IMGUI） | 属于 Unity 技术边界 |
| `ColorField` Color Picker / Eye Dropper | 独立编辑器窗口 | 属于 Unity 窗口边界 |
| `CurveField` 曲线编辑器浮窗 | 独立编辑器窗口 | 属于 Unity 窗口边界 |
| `GradientField` 渐变编辑器浮窗 | 独立编辑器窗口 | 属于 Unity 窗口边界 |
| `ObjectField` Object Picker | 资产选择对话框不在当前窗口树内 | 属于 Unity 窗口边界 |
| `ObjectField` 真实 DragAndDrop | 依赖 Editor `DragAndDrop` 生命周期 | 属于 Unity DragAndDrop 边界 |
| `ToolbarPopupSearchField` 结果列表 | 结果面板不在被测窗口树内 | 当前受 Unity/包接口边界阻断 |
| Tooltip 可视渲染断言 | Tooltip 由 Editor 全局管理 | 属于 Unity 全局 UI 边界 |
| IME 组合输入 | InputSystem 测试 API 不覆盖 IME 组合态 | 属于 Unity/InputSystem 能力边界 |
| 系统剪贴板真实粘贴 | 需要系统剪贴板+平台级快捷键链路 | 属于系统/平台边界 |
| 多窗口协同拖拽/断言 | 当前测试模型按单宿主窗口组织 | 当前受框架设计与 Unity 多窗口边界共同限制 |
| 像素级视觉 diff | 无内建视觉基线与差异分析链路 | 当前未接入 |

---

## 12. 工程上可实现但当前尚未落地的功能

| 优先级 | 功能 | 难度 | 说明 |
| --- | --- | --- | --- |
| P2 | `ToolbarMenu` 通用选择器支持浮窗菜单项 | 中 | 需要让框架感知并遍历浮窗 VisualTree |
| P4 | `ColorField`/`CurveField`/`GradientField` 编辑器浮窗交互 | 中~高 | 独立 `EditorWindow`；`set_value`/`assert_value` 直写已覆盖主路径 |
| P4 | `ToolbarPopupSearchField` 弹出结果列表项选择 | 高 | 官方未暴露稳定结果项遍历/选择 API |
| P5 | `ObjectField` 真实 DragAndDrop | 高 | 需模拟完整 `DragAndDrop` 生命周期 |
| P5 | 系统剪贴板操作（真实 Ctrl+C/V） | 高 | 需要平台级剪贴板 API，跨平台兼容性复杂 |
| P5 | 多窗口协同测试 | 高 | 需重构 `UnityUIFlowSimulationSession` 和窗口管理逻辑 |
| P5 | 视觉像素对比断言 | 高 | 需要引入图像处理库并建立基线图片管理流程 |
| P6 | IME 组合输入 | 极高 | 需要操作系统级输入法模拟，超出 Unity 范围 |

---

## 13. 修订历史

### 2026-04-23 2.0.0

- 基于代码库完整复核（所有 Action 类、Fixture 基类、ActionHelpers、ImguiActions）重写文档。
- 新增 §2「何时用 YAML 测试用例，何时用 C# 代码测试用例」——包含优先选择 YAML 的场景清单、必须用 C# 的场景清单、以及 YAML+C# 混合模式说明。
- 新增 §6「全部内置动作详细说明」——按鼠标、键盘、值赋值、断言、工具/控制流五类，逐动作列出关键参数、底层机制、YAML 示例。
- 新增 §7「输入模拟支持详情」——逐条列出鼠标/指针和键盘输入的支持情况及底层技术机制。
- 新增 §8「选择器语法完整参考」——补充属性选择器、`[data-*]` 用法。
- 更新 §3（全面支持控件）：补充 `assert_disabled` 动作覆盖；确认 YAML 基线 115 份。
- 更新 §4（局部支持控件）：修正 `ColorField` `set_value` 支持 `#RRGGBBAA` 十六进制格式的说明。

### 2026-04-21 1.8.0 第三轮扩展修订

- 新增 13 份 YAML，全量套件从 102 份扩展到 115 份。
- 新增 `imgui_wait` 动作（对应 IMGUI 文档同步更新）。
- 新增 `_91-negative-set-value-invalid.yaml` 等负向测试。

### 2026-04-21 1.7.0 大规模扩展修订

- 新增 31 份 YAML，全量套件从 71 份扩展到 102 份。
- 替代参数覆盖（6 份）、功能增强覆盖、负向测试扩展（23 份）。

### 2026-04-21 1.6.0 全量验证与修复修订

- 全量验证 71 份 YAML；修复 Empty String 参数、`AssertionFailed` 状态映射、编译监控等 4 个问题。
- IMGUI 文档独立为《IMGUI 控件自动化覆盖与限制说明.md》。

### 2026-04-16 1.5.0

- 适配 `com.unity.ui@2.0.0`：`DispatchClick`、五级 Button fallback、`DropdownMenu` reflection fallback。
- 所有历史待开发项（P1/P2）标记为已完成。

### 2026-04-13 / 2026-04-12

- `PropertyField`/`InspectorElement` 提升为局部支持。
- `sort_column`/`resize_column`/`close_tab`/`press_key_combination`/`read_breadcrumbs`/`drag_scroller`/`menu_item` 等动作全部标记为已完成。
