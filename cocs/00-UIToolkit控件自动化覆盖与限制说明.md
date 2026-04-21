# UIToolkit 控件自动化覆盖与限制说明

版本：1.5.2
日期：2026-04-17
状态：更新基线（已适配 `com.unity.ui@2.0.0`，补齐菜单高级动作）

---

## 1. 文档目标

本文档列出 UnityUIFlow V1 对 UIToolkit 所有控件类型的自动化覆盖范围，明确以下分类：
- **全面支持**：可通过 YAML 声明式步骤完成定位、交互、赋值、断言。
- **局部支持**：可定位和部分交互，但部分高级功能需 C# Page Object 编程。
- **仅定位/断言**：可通过选择器定位和读取属性，但交互必须通过 C# 编程。
- **不可自动化**：官方 API 不开放或控件特性导致无法通过任何自动化手段操作。

当前示例验收基线：
- `Assets/Examples/Yaml` 当前已扩展到 `01-35` 及 `41-44` 共 39 份 YAML。
- 新增覆盖窗口已补入字段值、集合与布局、输入与菜单 3 类宿主，见 `ExampleCoverageFieldsWindow`、`ExampleCoverageCollectionsWindow`、`ExampleCoverageInputWindow`。
- 这些 YAML 已覆盖当前代码中已实现的大部分内置动作，以及对应的 UIToolkit 控件族。

---

## 2. V1 全面支持的控件

以下控件可通过 YAML 内置动作完成完整的定位、交互、赋值和断言：

| 控件 | 可用动作 | 赋值方式 | 断言方式 |
| --- | --- | --- | --- |
| `Button` | `click`、`double_click`、`hover` | 不适用（无 value） | `assert_visible`、`assert_text`、`assert_enabled` |
| `RepeatButton` | `click`、`hover` | 不适用 | `assert_visible`、`assert_text`、`assert_enabled` |
| `Label` | `hover` | 不适用（纯显示） | `assert_visible`、`assert_text`、`assert_text_contains` |
| `Toggle` | `click`、`set_value` | `set_value`（`true`/`false`） | `assert_value`、`assert_property` |
| `TextField` | `type_text`、`type_text_fast`、`set_value`、`click`、`focus` | `type_text`、`type_text_fast`、`set_value` | `assert_text`、`assert_value`、`assert_property` |
| `IntegerField` | `type_text_fast`、`set_value`、`click`、`focus` | `set_value`（整数文本） | `assert_value`、`assert_property` |
| `LongField` | 同 `IntegerField` | `set_value`（长整数文本） | 同上 |
| `FloatField` | 同 `IntegerField` | `set_value`（浮点文本） | 同上 |
| `DoubleField` | 同 `IntegerField` | `set_value`（浮点文本） | 同上 |
| `UnsignedIntegerField` | 同 `IntegerField` | `set_value`（无符号整数文本） | 同上 |
| `UnsignedLongField` | 同 `IntegerField` | `set_value`（无符号长整数文本） | 同上 |
| `Slider` | `set_slider`、`set_value`、`drag`、`click` | `set_slider`、`set_value` | `assert_value`、`assert_property` |
| `SliderInt` | 同 `Slider` | 同 `Slider` | 同上 |
| `DropdownField` | `select_option`、`click` | `select_option`、`set_value` | `assert_value`、`assert_property` |
| `PopupField<string>` | `select_option`、`click_popup_item`、`click` | `select_option`、`set_value` | `assert_value`、`assert_property` |
| `EnumField` | `select_option`、`click` | `select_option`、`set_value` | `assert_value` |
| `RadioButton` | `click`、`set_value` | `set_value`（`true`/`false`） | `assert_value` |
| `RadioButtonGroup` | `select_option`、`set_value` | `select_option`（按索引） | `assert_value` |
| `Foldout` | `toggle_foldout`、`click`、`set_value` | `toggle_foldout`、`set_value`（`true`/`false`） | `assert_value`、`assert_property` |
| `ScrollView` | `scroll`、`drag` | 不适用 | `assert_visible`、`assert_property` |
| `ListView` | `select_list_item`、`drag_reorder`、`scroll`、`click` | `select_list_item`（按 `index` / `indices`） | `assert_property`（`selectedIndex` / `selectedIndices`） |
| `TreeView` | `select_tree_item`、`scroll`、`click` | `select_tree_item`（按索引或 id） | `assert_property`（`selectedIndex`） |
| `Vector2Field` | `set_value`、`focus` | `set_value`（`x,y`） | `assert_value` |
| `Vector3Field` | `set_value`、`focus` | `set_value`（`x,y,z`） | `assert_value` |
| `Vector4Field` | `set_value`、`focus` | `set_value`（`x,y,z,w`） | `assert_value` |
| `Vector2IntField` | `set_value`、`focus` | `set_value`（`x,y`） | `assert_value` |
| `Vector3IntField` | `set_value`、`focus` | `set_value`（`x,y,z`） | `assert_value` |
| `RectField` | `set_value`、`focus` | `set_value`（`x,y,w,h`） | `assert_value` |
| `RectIntField` | `set_value`、`focus` | `set_value`（`x,y,w,h`） | `assert_value` |
| `BoundsField` | `set_value`、`focus` | `set_value`（`cx,cy,cz,ex,ey,ez`） | `assert_value` |
| `BoundsIntField` | `set_value`、`focus` | `set_value`（`px,py,pz,sx,sy,sz`） | `assert_value` |
| `MinMaxSlider` | `set_slider`、`set_value`、`drag` | `set_slider`（`min_value`、`max_value`） | `assert_value` |
| `Hash128Field` | `set_value`、`focus` | `set_value`（32位十六进制字符串） | `assert_value` |
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
| `ToolbarSearchField` | `type_text`、`type_text_fast`、`set_value`、`click`、`focus` | `type_text`、`type_text_fast`、`set_value`（继承自 `TextInputBaseField<string>`） | `assert_text`、`assert_value`、`assert_property` |

---

## 3. V1 局部支持的控件

以下控件可定位和进行部分交互，但部分高级功能在 V1 中不支持或需 C# 编程：

| 控件 | 可用 YAML 动作 | 不支持的交互 | 原因与替代方案 |
| --- | --- | --- | --- |
| `EnumFlagsField` | `select_option`（支持 `value` / `index` / `indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 弹出面板中的真实逐项点击勾选/取消 | 弹出面板为独立浮窗，选择器无法直接定位弹出面板子项。V1 通过 `select_option` 整值直写、`toggle_mask_option` 单选项 toggle 或 `click_popup_item` 直接字段值 toggle 替代 |
| `MaskField` | `select_option`（支持 `value` / `index` / `indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 同上 | 同上 |
| `LayerMaskField` | `select_option`（支持 `value` / `index` / `indices`）、`toggle_mask_option`、`click_popup_item`、`set_value` | 弹出面板逐项操作 | 同上 |
| `ColorField` | `set_value`、`assert_value`、`click` | 拾色器面板交互、Eye Dropper 工具 | 拾色器为独立 `EditorWindow`，需 C# 编程操作。V1 通过 `set_value`/`assert_value` 直接读写 `Color` 值替代 |
| `MultiColumnListView` | `select_list_item`（支持 `index` / `indices`）、`sort_column`、`resize_column`、`scroll`、`click` | 列头排序点击（通过 `sort_column` 直接设置排序）、列宽拖拽（通过 `resize_column` 直接设置宽度） | 当前已通过通用集合选择管线承接行选择；`sort_column`/`resize_column` 通过直接写 API 实现，不依赖 UI 拖拽事件 |
| `MultiColumnTreeView` | `select_tree_item`（支持 `id` / `index`）、`sort_column`、`resize_column`、`scroll`、`click` | 列头排序点击（通过 `sort_column` 直接设置排序）、列宽拖拽（通过 `resize_column` 直接设置宽度） | 当前已通过通用树选择管线承接节点选择；`sort_column`/`resize_column` 通过直接写 API 实现，不依赖 UI 拖拽事件 |
| `TwoPaneSplitView` | `drag`（拖拽分割条）、`set_split_view_size` | 更复杂的跨窗口拖分割条联动 | 已支持选择器命中内部 dragline 进行拖拽，也支持 `set_split_view_size` 直接按 pane 指定目标尺寸 |
| `Scroller` | `set_value`、`click`、`drag`、`page_scroller`、`drag_scroller` | 无 | `drag_scroller` 已支持基于 thumb/track 结构的精确拖拽；`set_value` / `page_scroller` 为直接 API 写值 |
| `TagField` | `select_option`（按 `value` / `index`）、`click_popup_item`、`assert_value` | 弹出菜单内部逐项点击 | 继承自 `PopupField<string>`，`select_option` / `click_popup_item` 可通过 `choices` + `value` 或 `index` 直写设定值；弹出菜单内部不可定位 |
| `LayerField` | `select_option`（按 `value` / `index`）、`click_popup_item`、`assert_value` | 弹出菜单内部逐项点击 | 同上 |
| `ObjectField` | `set_value`、`assert_value`、`assert_property` | Object Picker 对话框、真实拖放赋值 | 当前支持通过 `guid:`、`path:`、`name:`、`asset-name:`、`search:`、`search:TypeName:Needle` 直接加载资产并赋值；Object Picker 与真实 DragAndDrop 浮窗链路仍不可直接定位 |
| `CurveField` | `set_value`、`assert_value`、`assert_property` | 曲线编辑器窗口交互 | 当前支持通过键帧 DSL 直接读写 `AnimationCurve` 值；曲线编辑器浮窗仍不在被测窗口树内 |
| `GradientField` | `set_value`、`assert_value`、`assert_property` | 渐变编辑器窗口交互 | 当前支持通过颜色/透明度 key DSL 直接读写 `Gradient` 值；渐变编辑器浮窗仍不在被测窗口树内 |
| `ToolbarMenu` | `click`、`open_popup_menu`、`select_popup_menu_item`、`assert_menu_item*`、`menu_item` | 直接定位弹出浮窗内部 VisualTree | 当前可借助官方 `PopupMenuSimulator` 完成菜单项选择与断言，并可用统一 `menu_item` DSL 承接；若官方模拟器不可用，还会自动降级到 `DropdownMenu` reflection fallback |
| `ToolbarPopupSearchField` | `type_text`、`type_text_fast`、`set_value`、`assert_value`、`click`、`focus` | 弹出菜单内部项点击 | 搜索文本输入本体可自动化，但弹出结果菜单仍为独立浮窗 |
| `ToolbarBreadcrumbs` | `click`、`navigate_breadcrumb`、`read_breadcrumbs` | 弹出式导航菜单 | 已支持按 `label` / `index` 导航及自动枚举（`read_breadcrumbs`）；弹出式历史路径菜单仍属独立浮窗边界 |
| `PropertyField` | `set_bound_value`、`assert_bound_value`，以及通过后代控件执行 `click` / `set_value` / `assert_value` | 直接对未绑定或无稳定 `bindingPath` 的复杂子结构做统一赋值 | 已支持按 `binding_path` 的统一语义赋值；若子树无稳定绑定路径，仍需回退到后代控件定位 |
| `InspectorElement` | `set_bound_value`、`assert_bound_value`，以及通过后代控件执行 `click` / `set_value` / `assert_value` | 完全脱离 Inspector 生成结构做任意字段推断 | 已支持按 `binding_path` 穿透到 Inspector 绑定字段；复杂自定义绘制仍可能需要结合具体子树定位 |

---

## 4. V1 仅定位/断言的控件

当前这类控件已基本被压缩到极少数残余边界；本版本暂无单独列出的“仅定位/断言”控件。

仍建议保留这个分类，原因是未来若引入新的 Editor 专属容器控件，可能仍会先落在“可定位、可读属性、但没有统一语义动作”的阶段。

---

## 5. V1 不可自动化的控件与功能

以下控件或功能由于 Unity 官方 API 限制或控件特性，在 V1 中完全不可自动化：

| 控件/功能 | 原因 | 建议 |
| --- | --- | --- |
| `IMGUIContainer` | 内部通过 IMGUI `OnGUI` 回调渲染，不生成 UIToolkit 子元素，UIToolkit 选择器无法定位内部内容 | 将被测 UI 迁移到纯 UIToolkit 实现；或在 C# 测试中直接调用 IMGUI 状态逻辑 |
| `ProjectSettingsProvider`（项目设置面板） | 当前实现使用 `EditorGUILayout`（IMGUI），不生成 UIToolkit 子元素 | 若需自动化，必须将该 Provider 重写为纯 UIToolkit 实现 |
| 拾色器（Color Picker Window） | `ColorField` 点击后弹出独立 `EditorWindow`，不在被测窗口 UI 树内 | 使用 `set_value` 直接写入 `Color` 值 |
| 曲线编辑器（Curve Editor Window） | `CurveField` 点击后弹出独立 `EditorWindow` | C# 编程赋值 |
| 渐变编辑器（Gradient Editor Window） | `GradientField` 点击后弹出独立 `EditorWindow` | C# 编程赋值 |
| Object Picker 对话框 | `ObjectField` 点击后弹出 Unity 资产选择对话框，不在当前窗口 UI 树内 | C# 编程赋值 |
| Tooltip 浮窗 | Tooltip 由 Editor 全局管理，不在目标窗口 UI 树内 | 可通过 `assert_property` 断言 `tooltip` 属性值，但无法验证 Tooltip 可视渲染 |
| `ToolbarPopupSearchField` 弹出结果菜单 | 搜索结果弹出面板不在被测窗口树内，且当前项目未拿到稳定可选择结果项 API | 搜索文本输入可自动化，结果项选择当前需 C# 编程或后续版本接口支持 |
| 多窗口协同 | V1 每个测试仅支持一个宿主窗口。跨窗口拖拽、多窗口断言不可实现 | 单窗口设计。跨窗口测试需拆为多个独立用例 |
| 剪贴板操作（Copy/Paste） | 需要系统级剪贴板 API + 键盘快捷键组合 | P2 扩展。V1 中可使用 `set_value` 快速路径替代粘贴 |
| IME / 输入法组合输入 | InputSystem 测试 API 不覆盖 IME 组合输入语义 | P2 扩展。V1 中可使用 `type_text_fast` 直接写入最终文本 |
| 拖放文件到控件 | `ObjectField` 和自定义控件的文件拖放依赖 Editor DragAndDrop API，无法通过 UIToolkit 事件模拟 | C# 编程模拟 `DragAndDrop` |
| 动态生成的浮窗式 UI | 任何通过 `EditorWindow.ShowAsDropdown`、`GenericMenu.ShowAsContext`、`DropdownMenu` 等创建的浮窗式 UI 面板，均不在被测窗口 `rootVisualElement` 树内 | 若弹出内容可通过 `set_value` / `select_option` 绕过弹出菜单直接写值，则使用直接写值方案 |

---

## 6. IMGUI 自动化支持（新增）

> 自 2026-04-17 起，UnityUIFlow 新增对 IMGUI（Immediate Mode GUI）的自动化测试能力。该能力通过反射 Unity 内部 `GUILayoutUtility` 状态、注入 `OnGUI` 钩子、发送 `UnityEngine.Event` 事件实现。

### 6.1 设计原理

IMGUI 与 UIToolkit 是两套独立的渲染体系：
- UIToolkit 保留 `VisualElement` 树，可通过选择器遍历定位。
- IMGUI 每帧通过 `OnGUI()` 回调即时绘制，无持久树结构。

因此，IMGUI 自动化不走 `ElementFinder` + `VisualElement` 路径，而是建立了一套平行子系统：

```
YAML imgui_* 动作
    ↓
ImguiSelectorCompiler（编译 gui(button, text="OK") 语法）
    ↓
ImguiExecutionBridge（注入 IMGUIContainer.onGUIHandler 钩子）
    ↓
OnGUI 执行 → ImguiSnapshotCapture（反射 GUILayoutUtility.current.topLevel.entries）
    ↓
ImguiElementLocator（基于快照匹配选择器）
    ↓
Event 注入（MouseDown/Up/ScrollWheel/KeyDown/Up）或断言
```

### 6.2 支持的 IMGUI 控件（Tier 1）

| 控件 | IMGUI API | 选择器示例 | 可用动作 |
|------|-----------|-----------|---------|
| Button | `GUILayout.Button` | `gui(button)` / `gui(button, text="Save")` | click、double_click、right_click、hover、assert_text、assert_visible |
| TextField | `EditorGUILayout.TextField` | `gui(textfield, index=0)` | type、focus、press_key、assert_visible |
| Label | `GUILayout.Label` | `gui(label, index=2)` | assert_text、assert_visible |
| Toggle | `EditorGUILayout.Toggle` | `gui(toggle, text="Enabled")` | click、assert_value、assert_visible |
| Popup/Dropdown | `EditorGUILayout.Popup` | `gui(dropdown, index=0)` | click、select_option、assert_value、assert_visible |
| Toolbar | `GUILayout.Toolbar` | `gui(toolbar, index=0)` | click、assert_visible |
| Slider | `EditorGUILayout.Slider` | `gui(slider, index=0)` | press_key（方向键）、assert_visible |
| ScrollView | `GUILayout.BeginScrollView` | `gui(scroller)` | scroll、assert_visible |
| Group | `GUILayout.BeginVertical/Horizontal` | `gui(group="Settings")` | 作为路径容器使用 |

### 6.3 IMGUI 动作列表（15 个）

| 动作 | 说明 |
|------|------|
| `imgui_click` | 左键点击控件中心 |
| `imgui_double_click` | 快速双击 |
| `imgui_right_click` | 右键点击（button=1） |
| `imgui_hover` | 发送 MouseMove 到控件中心 |
| `imgui_type` | 逐字符输入文本（Event 队列） |
| `imgui_focus` | 点击控件以获取焦点 |
| `imgui_scroll` | 发送 ScrollWheel 事件 |
| `imgui_select_option` | Dropdown 展开 + 方向键导航 + 回车确认 |
| `imgui_press_key` | 发送单按键（KeyDown + KeyUp） |
| `imgui_press_key_combination` | 发送组合键（Ctrl+A、Shift+Tab 等） |
| `imgui_read_value` | 读取控件文本/值，存入 `SharedBag` |
| `imgui_assert_text` | 断言 Label/Button 文本 |
| `imgui_assert_visible` | 断言控件存在且尺寸 > 0 |
| `imgui_assert_value` | 尽力断言值（依赖 text/style 推断） |
| `imgui_wait` | 轮询等待控件出现在快照中 |

### 6.4 选择器语法

```yaml
# 基本类型匹配
gui(button)
gui(textfield, index=2)
gui(toggle, text="Enabled")

# 组路径限定
gui(group="Settings" > button, text="Apply")

# ControlName 匹配（需要被测代码配合 GUI.SetNextControlName）
gui(textfield, control_name="username-field")

# 焦点匹配
gui(focused)
```

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
- imgui_type: { selector: "gui(textfield, control_name=\"username-field\")", text: "admin" }
```

### 6.6 已知限制

| 限制 | 说明 |
|------|------|
| 反射依赖 | `ImguiSnapshotCapture` 反射 `GUILayoutUtility.current` 等内部字段，Unity 版本升级可能破坏兼容性 |
| 布局重排 | 窗口 resize 后 `index` 选择器可能失效，建议优先使用 `text` 或 `control_name` |
| 浮窗菜单 | `GenericMenu` / `EditorUtility.DisplayDialog` 等独立浮窗不在 GUILayout 树内，无法定位 |
| 值断言 | Toggle/Slider 的真实值不存储在 `GUILayoutEntry` 中，`imgui_assert_value` 为尽力推断 |
| 复杂自定义绘制 | `GUI.DrawTexture`、`Handles` 等纯绘制控件无 `GUILayoutEntry`，无法定位 |

### 6.7 与 UIToolkit 混用

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

---

## 7. 按交互类型的覆盖总结

| 交互类型 | V1 覆盖范围 | 未覆盖范围 |
| --- | --- | --- |
| 点击 | 所有可点击控件；菜单项可通过 `select_context_menu_item` / `select_popup_menu_item` 专用动作驱动。`com.unity.ui 2.0.0` 下额外内置 `DispatchClick` + 反射 fallback 链路 | 不能用通用选择器直接点击独立浮窗中的菜单项 |
| 双击 | 所有可点击控件 | 无 |
| 悬停 | 所有可见控件 | Tooltip 可视渲染验证 |
| 拖拽 | 任意两元素间拖拽 | 跨窗口拖拽、文件拖放 |
| 滚动 | `ScrollView`、`ListView`、`TreeView` | 自定义吸附滚动控件的高级行为 |
| 文本输入（真实键盘） | `TextField` 及子类 | IME 组合输入 |
| 快速写值 | 所有 `BaseField<T>` 子类中的字符串、数值、枚举、向量、矩形、边界、颜色、哈希，以及 `ObjectField` / `CurveField` / `GradientField` 的直写值路径 | 仍不覆盖对象选择器浮窗、曲线编辑器浮窗、渐变编辑器浮窗 |
| 选择（下拉/单选） | `DropdownField`、`PopupField<string>`、`EnumField`、`EnumFlagsField`、`RadioButtonGroup`、`MaskField`、`LayerMaskField`、`TagField`、`LayerField` | 弹出面板逐项交互 |
| 列表选择 | `ListView`、`MultiColumnListView` | 无 |
| 树选择 | `TreeView`、`MultiColumnTreeView` | 展开/折叠动画内部状态 |
| 列排序 | `MultiColumnListView`、`MultiColumnTreeView`（通过 `sort_column`） | `ColumnSortingMode.Custom` 的自定义排序回调 |
| 列宽调整 | `MultiColumnListView`、`MultiColumnTreeView`（通过 `resize_column`） | 无 |
| Split 拖拽 | `TwoPaneSplitView`（通过分割条内部锚点）与 `set_split_view_size` | 更复杂的跨窗口 split 联动 |
| Scroller 赋值 | `Scroller` 的 `set_value`、`page_scroller`、`drag_scroller` | 无 |
| 折叠/展开 | `Foldout` | 无 |
| 滑块 | `Slider`、`SliderInt`、`MinMaxSlider` | 无 |
| Tab 切换 | `TabView`（`select_tab`、`close_tab`） | 无 |
| Toolbar 控件 | `Toolbar`（容器）、`ToolbarButton`、`ToolbarToggle`、`ToolbarSearchField`、`ToolbarMenu`（借助官方 PopupMenuSimulator / `menu_item` / `DropdownMenu` fallback）、`ToolbarPopupSearchField`（输入本体）、`ToolbarBreadcrumbs`（`navigate_breadcrumb`、`read_breadcrumbs`） | `ToolbarPopupSearchField` 弹出结果列表 |
| 按键 | 任意获焦元素；当前 YAML 已覆盖 `press_key`、`press_key_combination`、`type_text`、`type_text_fast`、指针类 `modifiers` 组合 | IME、系统级剪贴板 |
| 焦点 | 所有可聚焦元素 | 焦点链导航（Tab 切换焦点） |
| 断言 | 可见性、文本、value、property、enabled/disabled | 视觉像素对比 |
| 截图 | 当前窗口 | 多窗口截图 |

---

补充说明：
- `assert_value` 对复杂值控件不是简单字符串直比较。当前已按目标值类型支持 `Color`、`Vector*`、`Rect*`、`Bounds*`、`Hash128` 等字段的类型级比较。
- `TreeView` 的 YAML 自动化支持范围以“选中节点”为边界；其内部动画、浮窗式子面板不纳入 V1。`TabView` 现已支持 `select_tab` 与 `close_tab`。
- `com.unity.ui 2.0.0` 兼容性说明：由于官方 `PanelSimulator.Click` 在该版本不再触发 `Button`，框架已自动降级到 `DispatchClick`（手动派发 `PointerDownEvent` + `PointerUpEvent`）。对 `Button` 及继承自 `Button` 的控件（如 `ToolbarButton`、`ToolbarBreadcrumbs` 子项），还额外内置了 `SendClickEvent` → `Button.clicked` → `Clickable.clicked` → `SimulateSingleClick` → `ClickEvent` 五级反射 fallback，确保点击在升级包后仍然可用。

---

## 7. 扩展规划

| 优先级 | 功能 | 说明 |
| --- | --- | --- |
| P1 | `select_list_item` 多选支持 | 已完成，支持 `indices` 参数逗号分隔 |
| P1 | `drag_reorder` | 已完成，支持 `ListView` 逻辑重排 |
| P1 | 列头排序/列宽拖拽 | 已完成：`sort_column`（设置 `SortColumnDescriptions`）和 `resize_column`（直接写 `Column.width`）支持 `MultiColumnListView` / `MultiColumnTreeView` |
| P1 | 上下文菜单自动化 | 已完成，支持 `open_context_menu` / `select_context_menu_item` / `assert_menu_item*` |
| P1 | 弹出菜单自动化 | 已完成，支持 `open_popup_menu` / `select_popup_menu_item` / `assert_menu_item*` |
| P1 | Toolbar 控件覆盖 | 已完成：`Toolbar`（§2）、`ToolbarButton`/`ToolbarToggle`/`ToolbarSearchField`（§2，继承父类动作）、`ToolbarMenu`（§3，支持官方 `PopupMenuSimulator` + `DropdownMenu` fallback） |
| P1 | `TestRunnerWindow` 批量执行 | 已完成：`TestRunnerWindow` 已覆盖目录批量运行、勾选执行与报告生成 |
| P2 | `ObjectField` 拖放赋值 | 模拟 `DragAndDrop` API |
| P2 | 组合键（`Shift+Click` 等） | 已完成：`press_key_combination` 支持 `Ctrl+A` / `Ctrl+C` / `Ctrl+S` 等常见系统级键盘组合 |
| P2 | 剪贴板操作 | `Ctrl+C` / `Ctrl+V` 模拟 |
| P2 | IME 输入 | InputSystem 不覆盖，需系统级模拟 |
| P2 | 多窗口协同测试 | `com.unity.ui.test-framework` 已支持多窗口测试，需扩展 Fixture |
| P2 | 视觉像素对比断言 | 截图对比基线图 |

---

## 8. 能实现但当前未实现

原先列为“能实现但当前未实现”的 P1/P2 项，已在当前代码中全部完成收口，现状态如下：

| 原优先级 | 项目 | 当前实现 |
| --- | --- | --- |
| P1 | `PropertyField` / `InspectorElement` 统一语义赋值 | 已完成：新增 `set_bound_value` / `assert_bound_value`，按 `bindingPath` 穿透绑定字段并执行赋值、断言 |
| P1 | `ToolbarBreadcrumbs` 专用导航动作 | 已完成：新增 `navigate_breadcrumb`，支持 `label` 或 `index` |
| P1 | `TwoPaneSplitView` 专用动作 | 已完成：新增 `set_split_view_size`，支持按 pane 指定目标尺寸 |
| P2 | `Scroller` 分页语义动作 | 已完成：新增 `page_scroller`，支持方向、页数和自定义 page size |
| P2 | `TabView` 关闭按钮操作 | 已完成：新增 `close_tab`，支持按 `label` / `index` 关闭页签 |
| P2 | `press_key_combination` 系统级组合键 | 已完成：新增 `press_key_combination`，支持 `Ctrl+C` / `Ctrl+V` / `Ctrl+S` 等常见快捷键 |
| P2 | `ToolbarBreadcrumbs` 自动枚举 | 已完成：新增 `read_breadcrumbs`，支持读取所有 breadcrumb 文本到共享包 |
| P2 | `Scroller` thumb 级精确拖拽 | 已完成：新增 `drag_scroller`，支持按方向或比例拖拽 scroller thumb |
| P2 | `ToolbarMenu` / Popup 统一菜单 DSL | 已完成：新增 `menu_item`，统一 `context` / `popup` / `auto` 与 `select` / `assert_enabled` / `assert_disabled` |
| P2 | `ObjectField` 统一资源查找策略扩展 | 已完成：`set_value` 已支持 `guid:`、`path:`、`name:`、`asset-name:`、`search:`、`search:TypeName:Needle` |
| P2 | 报告中细分执行链路 | 已完成当前实现口径：步骤结果与 Headed 面板会记录 `host/pointer/keyboard`、`driver details`，截图还会记录 `screenshot source` |

补充说明：
- 上述项目都属于“Unity API 已可达，但此前还缺少高层 YAML 语义封装”的类型。
- 当前已不再把它们视为待开发项；真正仍未完成的内容以下一节“Unity 接口边界阻断”为准。

---

## 9. 不能实现或当前受 Unity 接口边界阻断

以下能力当前不是“项目还没做”，而是明显受 Unity / 包接口边界影响。即便继续开发，也无法诚实宣称在现有环境下稳定支持：

| 项目 | 当前阻断原因 | 结论 |
| --- | --- | --- |
| `IMGUIContainer` 内部控件级自动化 | IMGUI 内容不进入 UIToolkit VisualTree，选择器无法拿到内部元素 | 属于 Unity 技术边界 |
| `ProjectSettingsProvider`（项目设置面板） | 当前实现使用 `EditorGUILayout`（IMGUI），不生成 UIToolkit 子元素 | 属于 Unity 技术边界；需重写为 UIToolkit 才能覆盖 |
| `ColorField` 的 Color Picker / Eye Dropper | 弹出独立编辑器窗口，且当前自动化管线不拥有其内部稳定树结构 | 属于 Unity 窗口边界 |
| `CurveField` 曲线编辑器浮窗 | 独立编辑器窗口，不在当前被测树内 | 属于 Unity 窗口边界 |
| `GradientField` 渐变编辑器浮窗 | 独立编辑器窗口，不在当前被测树内 | 属于 Unity 窗口边界 |
| `ObjectField` Object Picker | Unity 资产选择对话框不在当前窗口树内，且当前项目拿不到稳定可遍历子项 API | 属于 Unity 窗口边界 |
| `ObjectField` 真实 DragAndDrop 文件/对象拖放 | 依赖 Editor `DragAndDrop` 生命周期与系统拖放语义，不等价于普通 UIToolkit 指针事件 | 属于 Unity DragAndDrop 边界 |
| `ToolbarPopupSearchField` 结果列表自动化 | 当前结果面板不在被测窗口树内，项目里也未发现稳定官方结果项遍历/选择接口 | 当前受 Unity/包接口边界阻断 |
| Tooltip 可视渲染断言 | Tooltip 由 Editor 全局管理，不暴露为当前窗口树中的稳定元素 | 属于 Unity 全局 UI 边界 |
| IME 组合输入 | InputSystem 测试输入不覆盖 IME 组合态语义 | 属于 Unity/InputSystem 能力边界 |
| 系统剪贴板级真实粘贴 | 需要系统剪贴板与平台级快捷键链路 | 属于系统/平台边界，不是纯 UIToolkit 问题 |
| 多窗口协同拖拽/断言 | 当前测试模型按单宿主窗口组织，跨窗口同步、焦点和坐标语义都会升级复杂度 | 当前受框架设计与 Unity 多窗口边界共同限制 |
| 像素级视觉 diff | 当前项目只有真实截图，没有内建视觉基线与差异分析链路 | 当前未接入，且不属于 UIToolkit 控件动作层问题 |

补充判断：
- 这里的“不能实现”更准确地说，是“在当前 Unity 版本、当前包暴露能力和当前项目宿主模型下，不能稳定实现”。
- 如果未来 Unity 或 `com.unity.ui.test-framework` 暴露新的宿主、浮窗、搜索结果或 DragAndDrop 官方测试接口，这一结论才有可能变化。

---

## 10. 工程上可实现但当前尚未落地的功能

以下功能是 Unity API 或系统能力已经开放、代码框架也能承载，但当前项目里**还没有写对应实现**的能力。它们不是受 Unity 接口边界阻断的“不能做”，而是“还没做”。

### 10.1 按开发难度排序（从易到难）

| 优先级 | 功能 | 难度 | 说明 |
| --- | --- | --- | --- |
| P4 | `ColorField` / `CurveField` / `GradientField` 编辑器浮窗交互 | 中~高 | 这些浮窗是独立 `EditorWindow`。`set_value` / `assert_value` 直写方案已稳定覆盖主路径。若 Unity 未来将其内部结构改为 UIToolkit 并暴露可遍历 API，即可通过跨窗口选择器覆盖浮窗交互。当前实现需要大量反射或官方接口支持。 |
| P4 | `ToolbarPopupSearchField` 弹出结果列表项选择 | 高 | 结果面板不在当前窗口树内，且官方未暴露稳定的结果项遍历/选择 API。实现需要深度反射或等待 Unity 包更新。 |
| P5 | `ObjectField` 真实 DragAndDrop | 高 | 需要模拟完整的 Editor `DragAndDrop` 生命周期（`PrepareStartDrag` → `StartDrag` → `AcceptDrag`），涉及系统拖放语义和坐标转换，不等于普通 UIToolkit 指针事件。 |
| P5 | 系统剪贴板操作（真实 `Ctrl+C/V`） | 高 | 需要平台级剪贴板 API（如 `System.Windows.Forms.Clipboard`）或系统快捷键模拟，跨平台兼容性复杂。 |
| P5 | 多窗口协同测试 | 高 | `com.unity.ui.test-framework` 已支持多窗口测试，但当前项目 Fixture 和宿主模型按单窗口组织。扩展需要重构 `UnityUIFlowSimulationSession` 和窗口管理逻辑。 |
| P5 | 视觉像素对比断言 | 高 | 需要引入图像处理库（如 ImageMagick）或自研像素 diff 算法，并建立基线图片管理流程。 |
| P6 | IME 组合输入 | 极高 | InputSystem 测试 API 不覆盖 IME 组合态。实现需要操作系统级输入法模拟，技术栈超出 Unity 范围。 |

### 10.2 按使用频繁程度排序（从高到低）

| 优先级 | 功能 | 频率 | 说明 |
| --- | --- | --- | --- |
| P4 | `ObjectField` 真实 DragAndDrop | 中 | 资产工作流测试（拖拽材质到模型）中较常见，但 `set_value` 的 guid/path 直写已能满足多数断言需求。 |
| P4 | `ColorField` / `CurveField` / `GradientField` 浮窗交互 | 中 | 材质、动画、粒子系统测试中会遇到，但 `set_value` 的 DSL 直写已能完成绝大多数值断言。 |
| P5 | `ToolbarPopupSearchField` 结果列表选择 | 低 | Toolbar 搜索使用场景有限，多见于大型编辑器的全局搜索。 |
| P5 | 视觉像素对比断言 | 低 | 属于 UI 验收测试的高级需求，日常功能回归中极少使用。 |
| P6 | 系统剪贴板操作 | 低 | 大部分粘贴场景可用 `type_text_fast` 直接替代，真实剪贴板需求主要集中在跨应用数据交换测试。 |
| P6 | 多窗口协同测试 | 低 | 单窗口测试已覆盖 95% 以上的 Editor UI 场景。 |
| P7 | IME 组合输入 | 极低 | 游戏/工具编辑器测试中几乎不会涉及 IME 组合态验证。 |

---

## 11. 已实现基础能力但尚未全面的功能

以下功能是项目中**已有代码和 YAML 覆盖**，但只完成了“主路径”或“简单场景”，复杂场景仍需手动回退到后代控件定位或 C# 编程的功能。

### 11.1 按开发难度排序（从易到难）

| 优先级 | 功能 | 难度 | 说明 |
| --- | --- | --- | --- |
| P2 | `ToolbarMenu` 通用选择器支持浮窗菜单项 | 中 | 已有专用菜单动作和 `DropdownMenu` fallback。若能让弹出浮窗中的菜单项也能被通用选择器命中（如 `#menu-item-refresh`），则需要让框架感知并遍历浮窗 VisualTree。 |
| P2 | `ToolbarBreadcrumbs` 弹出式导航菜单 | 中 | 已有 `navigate_breadcrumb` 按 `label` / `index` 点击。部分 Breadcrumbs 会弹出下拉菜单供用户选择历史路径，这需要结合菜单动作统一处理。 |
| P2 | `EnumFlagsField` / `MaskField` / `LayerMaskField` 弹出面板真实逐项点击 | 中 | 已有 `select_option` 整值直写、`toggle_mask_option` 单选项 toggle，以及 `click_popup_item` 直接字段值操作（按 `value` / `index` 设置单选项）。`46-popup-item-click.yaml` 已回归覆盖。若要在弹出浮窗内真实逐一点击勾选/取消，仍需拿到浮窗根元素并遍历选项。 |
| P3 | `MultiColumnListView` / `MultiColumnTreeView` 真实 UI 拖拽排序/列宽 | 中 | 当前 `sort_column` / `resize_column` 是直接写 API。若要通过真实鼠标拖拽事件完成，需要计算列头或分割条的屏幕坐标并执行 `drag`，与现有 `ActionHelpers.DispatchMouseEvent` 结合即可。 |
| P3 | 菜单自动化支持通用 `click` 动作点击浮窗菜单项 | 中 | 当前菜单项只能通过 `select_popup_menu_item` / `select_context_menu_item` 等专用动作驱动。让通用 `click` 动作也能定位到浮窗中的菜单项，需要扩展事件生命周期或浮窗发现机制。 |
| P4 | `ObjectField` 资产搜索策略扩展 | 中 | 已有 `guid:` / `path:` / `name:` / `asset-name:` / `search:` 策略。可继续扩展 `project:`（限定工程内）、`package:`（限定包内）、`fuzzy:`（模糊搜索）等语义。 |
| P4 | `ColorField` / `CurveField` / `GradientField` 独立编辑器窗口覆盖 | 中~高 | 已有 `set_value` / `assert_value` 直写。全面覆盖意味着要操作独立 `EditorWindow`，需要跨窗口宿主模型支持。 |

### 11.2 按使用频繁程度排序（从高到低）

| 优先级 | 功能 | 频率 | 说明 |
| --- | --- | --- | --- |
| P2 | `ToolbarMenu` 通用选择器支持浮窗菜单项 | 高 | Toolbar 菜单在编辑器中非常常见。当前必须用专用动作，不能与通用 `click` 混用，导致页面对象（Page Object）模式不够统一。 |
| P2 | 菜单自动化支持通用 `click` 动作 | 高 | 与 `ToolbarMenu` 同理。所有 `DropdownMenu` / `GenericMenu` 浮窗中的项若能被通用 `click` 命中，将大幅降低测试编写门槛。 |
| P2 | `EnumFlagsField` / `MaskField` / `LayerMaskField` 弹出面板真实逐项点击 | 高 | Unity 编辑器大量配置面板（Layer、Tag、Quality Settings）使用这些字段。当前 `toggle_mask_option` 与 `click_popup_item` 已能直接按 `value` / `index` toggle 单选项（通过字段值直写），但弹出面板内的真实 UI 点击路径仍未覆盖。 |
| P3 | `MultiColumnListView` / `MultiColumnTreeView` 真实 UI 拖拽排序/列宽 | 中 | 真实拖拽路径更符合用户行为，但当前直接写 API 的方案在回归测试中已足够稳定，真实拖拽主要用于验证交互体验而非功能正确性。 |
| P3 | `ToolbarBreadcrumbs` 弹出式导航菜单 | 中 | 复杂编辑器中较常见，但一般项目使用频率一般。 |
| P4 | `ObjectField` 资产搜索策略扩展 | 中 | 资产定位策略越丰富，测试数据准备越灵活。但现有策略已覆盖 90% 以上的常规资产引用场景。 |
| P5 | `ColorField` / `CurveField` / `GradientField` 独立编辑器窗口覆盖 | 低 | 浮窗交互属于“端到端”体验验证，但值断言层面已完全覆盖，日常功能回归需求不强烈。 |

## 2026-04-12 覆盖状态修订

- 菜单能力已从“待动作层暴露”更新为“已支持”：
  - 上下文菜单：支持 `open_context_menu`、`select_context_menu_item`、`assert_menu_item`、`assert_menu_item_disabled`。
  - 弹出菜单：支持 `open_popup_menu`、`select_popup_menu_item`、`assert_menu_item`、`assert_menu_item_disabled`。
- 修饰键能力已从 P2 更新为当前已支持范围：
  - `click` / `double_click` / `drag` / `hover` 支持 `modifiers=shift,ctrl,alt,cmd`。
  - `press_key` / `type_text` 仍不等价于 IME、剪贴板、系统级快捷键编排。
- 列表能力已扩展：
  - `select_list_item` 支持 `index` 与 `indices`。
  - `drag_reorder` 支持 `ListView` 的逻辑重排。
  - `MultiColumnListView` 已纳入 `select_list_item` 通用集合选择回归。
  - `MultiColumnTreeView` 已纳入 `select_tree_item` 通用树选择回归。
  - `MultiColumnListView` / `MultiColumnTreeView` 的列头排序、列宽调整现已由 `sort_column` / `resize_column` 补齐。
- 容器与滚动细分能力已补充回归：
  - `Scroller` 已纳入 `set_value` / `assert_value` 回归。
  - `TwoPaneSplitView` 已纳入基于 `.unity-two-pane-split-view__dragline-anchor` 的 `drag` 回归。
- Editor 专属字段能力已补充回归：
  - `PopupField<string>`、`TagField`、`LayerField`
  - `ObjectField`（资源路径 / guid 直写）
  - `CurveField`（键帧 DSL 直写）
  - `GradientField`（颜色/透明度 key DSL 直写）
  - `PropertyField` / `InspectorElement`（通过已生成子控件承接现有动作）
- Toolbar 能力已补充回归：
  - `ToolbarButton`、`ToolbarToggle`、`ToolbarSearchField`
  - `ToolbarMenu` + `open_popup_menu` / `select_popup_menu_item`
  - `ToolbarPopupSearchField` 输入本体
  - `ToolbarBreadcrumbs` 已生成子项点击
- 复杂字段覆盖已继续补齐并完成回归样板：
  - `EnumField`、`EnumFlagsField`、`MaskField`
  - `UnsignedIntegerField`、`UnsignedLongField`
  - `Vector2Field`、`Vector2IntField`、`Vector3Field`、`Vector4Field`
  - `RectField`、`RectIntField`、`BoundsField`、`BoundsIntField`
  - `ColorField`、`Hash128Field`

## 2026-04-12 口径统一修订

- 本文档现以“当前真实实现”为准，不再把已完成的菜单动作、修饰键点击/拖拽、多选列表、`drag_reorder` 继续保留为待开发项。
- `ListView` 现已支持：
  - `select_list_item.index`
  - `select_list_item.indices`
  - `drag_reorder`
- `MultiColumnListView` / `MultiColumnTreeView` 现已支持：
  - `MultiColumnListView.select_list_item.index`
  - `MultiColumnListView.select_list_item.indices`
  - `MultiColumnTreeView.select_tree_item.id`
  - `MultiColumnTreeView.select_tree_item.index`
- `Scroller` 现已确认支持 `set_value` / `assert_value`，`click` 与 `drag` 仍属于依赖内部结构的局部支持。
- `TwoPaneSplitView` 现已确认支持通过 `.unity-two-pane-split-view__dragline-anchor` 配合 `drag` 完成分割条拖拽。
- `ObjectField` 现已支持通过资源路径或 `guid:` 直接赋值，并可用 `assert_value` 断言同一资产引用。
- `CurveField` / `GradientField` 现已支持 DSL 直写与 `assert_value`：
  - `CurveField`：`time:value:inTangent:outTangent;...`
  - `GradientField`：`time:#RRGGBBAA;...|time:alpha;...`
- `PopupField<string>`、`TagField`、`LayerField` 现已纳入 `select_option` / `assert_value` 支持范围。
- `PropertyField` / `InspectorElement` 现已确认可通过其动态生成的后代控件复用现有动作，不再仅限于“定位/断言”。
- `ToolbarPopupSearchField` 现已确认支持输入本体的 `set_value` / `assert_value`；限制仍在弹出结果列表。
- `ToolbarBreadcrumbs` 现已确认支持对已生成且可定位的 breadcrumb 子项执行 `click`；统一“按 label / index 导航”动作仍未封装。
- `EnumFlagsField`、`MaskField`、`LayerMaskField` 现已支持 `select_option` 的 `value` / `index` / `indices` 语义，以及 `set_value` 直接写值。
- 仍然不能诚实宣称“全面覆盖”的范围保持不变：
  - `ObjectField` 的对象选择器与拖放
  - `CurveField` / `GradientField` 的独立编辑器窗口
  - IME、系统剪贴板、多窗口协同、像素级视觉 diff
- `MultiColumnListView` / `MultiColumnTreeView` 列头排序与列宽调整已于 2026-04-12 补齐：
  - `sort_column`：直接操作 `sortColumnDescriptions`，不依赖 UI 拖拽事件，支持 `column`（名称/标题）或 `index` + 可选 `direction`（ascending/descending）。
  - `resize_column`：直接设置 `Column.width`（`Length.Pixel`），支持 `column` 或 `index` + `width`（正数像素）。
- Toolbar 控件覆盖已于 2026-04-12 补齐：
  - `Toolbar`（纯容器）、`ToolbarButton`（继承 Button，支持 click/double_click/hover/assert_*）、`ToolbarToggle`（继承 Toggle，支持 click/set_value/assert_value）、`ToolbarSearchField`（继承 TextInputBaseField(string)，支持 type_text/set_value/assert_*）均列入 §2 全面支持。
  - `ToolbarMenu` 列入 §3 局部支持：按钮本体可 click，菜单项可借助 `open_popup_menu` / `select_popup_menu_item` / `assert_menu_item*` 通过官方菜单模拟链路操作；菜单浮窗本身仍不属于当前窗口树内元素。
  - 所有 Toolbar 控件的支持均通过继承机制自动获得，无需新增代码。

## 2026-04-13 继续迭代修订

- `PropertyField` / `InspectorElement` 已从“仅定位/断言”提升为“局部支持”：
  - 现已验证可通过其动态生成的后代 `TextField` / `Toggle` / 数值字段复用现有 `set_value` / `assert_value` / `click` 动作。
  - 仍未封装直接针对 `PropertyField` / `InspectorElement` 自身的统一语义动作。
- `ToolbarPopupSearchField` 已补充“输入本体自动化”口径：
  - `set_value` / `assert_value` / `focus` 可用于搜索输入本体。
  - 弹出结果列表仍属于独立浮窗边界。
- `ToolbarBreadcrumbs` 已补充“可定位子项点击”口径：
  - 对已生成且可命名/可定位的 breadcrumb 子项，可复用 `click`。
  - 自动枚举、按 `label` / `index` 导航的专用动作仍未提供。
- 新增“能实现但当前未实现”与“受 Unity 接口边界阻断”两类说明：
  - 前者用于标识仍可继续封装的工程项，例如 `ToolbarBreadcrumbs` 专用导航动作、`PropertyField` / `InspectorElement` 统一语义动作、`TwoPaneSplitView` 专用动作。
  - 后者用于标识当前主要受 Unity / 包接口限制的能力，例如 `IMGUIContainer`、Object Picker、Curve/Gradient 浮窗、IME、系统剪贴板、`ToolbarPopupSearchField` 结果列表。
- 修正了“点击未覆盖菜单项”的旧说法：
  - 当前菜单项已能通过 `select_context_menu_item` / `select_popup_menu_item` 专用动作驱动。
  - 真正未覆盖的是“用通用选择器直接点击独立浮窗中的菜单项”。

## 2026-04-16 1.5.0 修订

- **适配 `com.unity.ui@2.0.0`**：
  - 官方 `PanelSimulator.Click` 在该版本不再触发 `Button`。框架已全面切换到 `DispatchClick`（手动向 panel visual tree 派发 `PointerDownEvent` + `PointerUpEvent`）。
  - 对 `Button` 及其子类（`ToolbarButton`、`ToolbarBreadcrumbs` 子项等），额外内置了 `SendClickEvent` → `Button.clicked` → `Clickable.clicked` → `SimulateSingleClick` → `ClickEvent` 五级反射 fallback，确保点击在升级包后仍然可用。
  - `ExecuteCommandAction` / `ValidateCommandAction` 增加了 `DispatchCommandEvent` fallback，解决 `PanelSimulator.ExecuteCommand` 事件丢失问题。
  - `ToolbarMenu` 的菜单动作增加了 `DropdownMenu` reflection fallback：当官方 `PopupMenuSimulator` 无法打开或选择菜单项时，自动降级到直接操作 `ToolbarMenu.menu` 的 `DropdownMenuAction`。
- **YAML 基线扩展到 39 份**：
  - 新增 `35-menu-advanced.yaml`：覆盖 `open_popup_menu`、`select_popup_menu_item`、`open_context_menu`、`select_context_menu_item`、`assert_menu_item`、`assert_menu_item_disabled` 六个此前缺失的菜单动作。

- **动作层收口**：
  - `navigate_breadcrumb` 专用动作已实现（支持 `label` / `index`）。
  - `set_split_view_size` 专用动作已实现（支持 `size` / `pane`）。
  - `page_scroller` 专用动作已实现（支持 `direction` / `pages` / `page_size`）。
  - `set_bound_value` / `assert_bound_value` 已实现（按 `binding_path` 穿透赋值与断言）。
  - `menu_item` 统一菜单 DSL 已实现（支持 `kind=popup/context/auto` + `mode=select/assert_enabled/assert_disabled`）。
- **不可自动化清单更新**：
  - 明确加入 `ProjectSettingsProvider`（项目设置面板）：当前实现使用 `EditorGUILayout`（IMGUI），不生成 UIToolkit 子元素，在当前框架下不可自动化。若需覆盖，必须将该 Provider 重写为纯 UIToolkit 实现。
- **§8 能实现但当前未实现**：
  - 所有历史待开发项（`ToolbarBreadcrumbs` 导航、`PropertyField` / `InspectorElement` 统一语义、`TwoPaneSplitView` 专用动作、`Scroller` 分页、`menu_item` DSL、`ObjectField` 扩展策略）已全部标记为“已完成”。

## 2026-04-16 代码对齐修订

基于对当前代码库（`Assets/UnityUIFlow/Editor/Actions` 及 YAML 回归用例）的复核，修正以下覆盖口径：

- **`TabView` 提升为全面支持**：
  - 代码中已实现 `close_tab` 动作（`CloseTabAction`），支持按 `label` / `index` 关闭页签，且已有 `18-layout-and-scroller.yaml` 回归覆盖。
  - 将 `TabView` 从 §3 局部支持移至 §2 全面支持，§6 对应未覆盖范围删除“Tab 关闭按钮”。
- **`press_key_combination` 已实现**：
  - 代码中已注册并实现 `press_key_combination` 动作（`PressKeyCombinationAction`），支持 `Ctrl+C`、`Ctrl+S` 等系统级键盘组合，且已有 `20-input-combinations.yaml` 回归覆盖。
  - §6 按键覆盖范围更新为包含 `press_key_combination`；§7 组合键状态更新为“已完成”；§10 中移除对应的待开发项。
- **`read_breadcrumbs` 已实现**：
  - 代码中已注册并实现 `read_breadcrumbs` 动作（`ReadBreadcrumbsAction`），支持自动枚举 breadcrumb 按钮文本并写入共享包，且已有 `21-toolbar-breadcrumb-and-menu-item.yaml` 回归覆盖。
  - §3 `ToolbarBreadcrumbs` 可用动作更新为包含 `read_breadcrumbs`；§10 中移除“自动枚举”待开发项。
- **`drag_scroller` 已实现**：
  - 代码中已注册并实现 `drag_scroller` 动作（`DragScrollerAction`），支持基于 scroller thumb/track 的精确拖拽，且已有 `18-layout-and-scroller.yaml` 回归覆盖。
  - §3 `Scroller` 可用动作更新为包含 `drag_scroller`，并删除“thumb 级精确拖拽”限制；§6 Scroller 赋值覆盖同步更新；§10 中移除对应的待开发项。
- **§8 补充 newly completed 项**：
  - 补充 `close_tab`、`press_key_combination`、`read_breadcrumbs`、`drag_scroller` 为“已完成”。

## 2026-04-17 1.5.1 修订

基于对当前代码库（`Assets/UnityUIFlow/Editor/Actions`、`Assets/Examples/Editor/UnityUIFlow.ExampleCoverageWindows.cs` 及 YAML 回归用例）的复核，修正以下覆盖口径：

- **YAML 基线数量修正**：
  - `Assets/Examples/Yaml` 实际已扩展到 `01-35` 及 `41-44` 共 39 份示例 YAML（另含 1 份 `_debug-infer-status.yaml` 调试用例），文档由 36 份修正为 39 份。
- **`Image` 可用动作修正**：
  - `ClickAction` 对任意 `VisualElement` 均生效，`Image` 作为 `VisualElement` 子类天然支持 `click`。将 `Image` 的可用动作由 `hover` 修正为 `click`、`hover`。
- **`GroupBox` 可用动作修正**：
  - `GroupBox` 与 `Box` 同为纯容器型 `VisualElement` 子类，事件派发机制一致。将 `GroupBox` 的可用动作由“无”修正为 `click`、`hover`。
- **`ToolbarPopupSearchField` 可用动作修正**：
  - 其内部输入为 `TextField`，`type_text` 与 `type_text_fast` 均可作用于该输入本体。将可用动作补充为包含 `type_text`。

## 2026-04-17 Stage 2–5 覆盖扩展与机制修订

- **ClickPopupItemAction 方案 A 落地**：
  - 已重写为直接操作字段值（MaskField、EnumFlagsField、LayerMaskField、PopupField），不再依赖不可靠的浮层面板点击。
  - 新增 YAML 回归用例 46-popup-item-click.yaml 覆盖 click_popup_item 主路径。
- **YAML 基线扩展 8 份（50–57）**：
  - 覆盖负向断言超时、缺失元素、边缘条件与循环边界、严格模式、元素间拖拽、滚动到元素、内联数据行、截图附件等场景。
- **C# 测试 Fixture 补齐**：
  - 新增 CloseTabAction、ReadBreadcrumbsAction、SetSplitViewSizeAction、DragScrollerAction、PageScrollerAction 的 fixture 测试。
  - 新增 UnityUIFlowCoverageWindowSmokeTests，覆盖三个 Coverage 示例窗口的控件存在性断言。
- **稳定性修复对齐文档口径**：
  - RuntimeController NRE 修复、CoverageWindow 反射赋值修复、DragAction 兼容 com.unity.ui@2.0.0 的 legacy 事件派发、InputWindow 事件回调注册修复。
- **EDITOR_BUSY 恢复机制**：
  - Unity 桥接层与 MCP Server 新增 unity_uiflow_force_reset，可在毫秒级释放卡死的执行锁，无需重启 Unity。
