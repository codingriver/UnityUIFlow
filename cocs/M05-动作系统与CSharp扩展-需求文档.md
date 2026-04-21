# M05 动作系统与CSharp扩展 需求文档

版本：1.4.0
日期：2026-04-13
状态：更新基线

## 1. 模块职责

- 负责：注册内置动作、自定义动作，解析动作名并创建 `IAction` 实例。
- 负责：为动作执行绑定 `ActionContext`，统一提供 `Finder`、`ScreenshotManager`、`RuntimeController`、取消令牌与共享数据。
- 负责：明确当前 fallback 动作实现与目标官方动作驱动之间的边界。
- 负责：支持 Page Object 与 C# 测试直接复用动作系统。
- 不负责：不负责 YAML 解析，不负责最终测试报告格式，不负责被测窗口业务逻辑。
- 输入/输出：输入为动作名、参数字典、`VisualElement` 根节点与 `ActionContext`；输出为动作执行结果、附件、异常与状态写回。

## 2. 数据模型

### ActionContext

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| Root | VisualElement | 必填 | 当前测试根节点 | 非空 | 无 |
| Finder | ElementFinder | 必填 | 当前元素查找器 | 非空 | 无 |
| Options | TestOptions | 必填 | 当前运行选项 | 非空 | 无 |
| Reporter | IExecutionReporter | 可选 | 动作执行期日志接收器 | `null` 或实现对象 | `null` |
| Simulator | object | 可选 | 动作底层驱动绑定点；当前为保留字段，目标用于官方 UI / InputSystem 驱动包装 | `null` 或包装对象 | `null` |
| CurrentStepId | string | 必填 | 当前步骤 ID | 非空字符串 | 无 |
| CurrentCaseName | string | 必填 | 当前用例名 | 非空字符串 | 无 |
| CurrentStepIndex | int | 必填 | 当前步骤序号 | `>= 0` | `0` |
| SharedBag | Dictionary<string, object> | 必填 | 当前用例共享数据 | 非空字典 | 空字典 |
| CancellationToken | CancellationToken | 必填 | 取消令牌 | 有效令牌 | 无 |
| ScreenshotManager | ScreenshotManager | 可选 | 当前步骤截图管理器 | `null` 或实现对象 | `null` |
| RuntimeController | RuntimeController | 可选 | Headed 运行控制器 | `null` 或实现对象 | `null` |
| CurrentAttachments | List<string> | 必填 | 当前步骤附件列表 | 长度范围 `[0,10]` | 空列表 |

### ActionRegistryEntry（设计提案，实现时确认）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| actionName | string | 必填 | 动作注册名 | 非空字符串，长度范围 `[1,64]` | 无 |
| actionType | Type | 必填 | 动作实现类型 | 必须实现 `IAction` | 无 |
| source | string | 必填 | 动作来源 | `built_in`、`custom` | `custom` |
| currentDriver | string | 必填 | 当前代码实际驱动 | `fallback_event_dispatch`、`direct_value_write`、`engine_only` | 无 |
| targetDriver | string | 必填 | 目标基线驱动 | `official_ui_test_framework`、`inputsystem_test`、`direct_value_write`、`engine_only` | 无 |
| fallbackAllowed | bool | 必填 | 正式验收模式下是否允许 fallback | `true`、`false` | `false` |

### BuiltInActionCapability（设计提案，实现时确认）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| actionName | string | 必填 | 内置动作名 | 当前 38 个内置动作之一 | 无 |
| currentImplementation | string | 必填 | 当前代码路径 | `ActionHelpers.DispatchClick`、`TryAssignFieldValue`、`ElementFinder.WaitForElementAsync` 等 | 无 |
| targetCapability | string | 必填 | 接入后目标语义 | `official_pointer`、`official_keyboard`、`fast_write`、`engine_assertion` | 无 |
| supportLevel | string | 必填 | 当前支持等级 | `full`、`partial`、`transition_only` | 无 |
| acceptanceLevel | string | 必填 | 接入后验收等级 | `full`、`partial` | 无 |
| notes | string | 可选 | 备注 | 非空字符串或 `null` | `null` |

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 注册内置动作 | `ActionRegistry` 构造时自动触发 | 动作名重复；动作类型未实现 `IAction` | `ActionRegistry.RegisterBuiltIns`、`ActionRegistry.Register` | 不涉及；注册表只增不撤销 |
| 注册自定义动作 | `ActionRegistry` 构造时自动扫描 | `[ActionName]` 缺失；动作名冲突；类型未实现 `IAction` | `ActionRegistry.RegisterCustomActions`、`ActionRegistry.Register` | 不涉及 |
| 解析动作实例 | 执行步骤前 | 动作未注册；实例创建失败 | `ActionRegistry.Resolve` | 不涉及；只读解析 |
| 执行动作 | `StepExecutor`、`UnityUIFlowFixture<TWindow>.ExecuteActionAsync` | 上下文未就绪；参数无效；目标元素无效 | `IAction.ExecuteAsync` | 不涉及；动作对 UI 的影响由被测系统承担 |
| 追加动作附件 | 动作内部 | 附件路径为空；附件数已达 10 个 | `ActionContext.AddAttachment` | 不涉及；仅追加当前步骤附件 |

## 4. 交互规格

- 触发事件：执行器进入动作步骤时，根据步骤 `action` 调用 `ActionRegistry.Resolve` 创建动作实例。
- 中间状态：`Resolve -> Build ActionContext -> ExecuteAsync -> Record attachments/log -> Return StepResult`。
- 数据提交时机：动作执行结束后统一返回执行器；中途产生的附件通过 `ActionContext.AddAttachment` 挂到当前步骤。
- 取消/回退行为：每个动作在等待点必须检查 `CancellationToken`；取消后立即抛出 `OperationCanceledException`；动作系统不做通用 Undo。

### 当前实现与目标基线

| 动作 | 当前实现 | 目标基线 | 当前结论 | 目标结论 |
| --- | --- | --- | --- | --- |
| `click` | `ActionHelpers.DispatchClick` | `com.unity.test-framework` UI 测试子系统指针点击 | 已实现，过渡态 | 全面支持 |
| `double_click` | `DispatchClick(..., 2)` | `com.unity.test-framework` UI 测试子系统双击 | 已实现，过渡态 | 全面支持 |
| `hover` | `DispatchMouseEvent(MouseMove)` | `com.unity.test-framework` UI 测试子系统悬停 | 已实现，过渡态 | 全面支持 |
| `drag` | `DispatchMouseEvent(MouseDown/Move/Up)` | `com.unity.test-framework` UI 测试子系统拖拽 | 已实现，过渡态 | 全面支持 |
| `scroll` | `DispatchWheelEvent` | `com.unity.test-framework` UI 测试子系统滚动 | 已实现，过渡态 | 局部到全面支持 |
| `press_key` | `DispatchKeyboardEvent(KeyDown/KeyUp)` | InputSystem 测试键盘输入 | 已实现，过渡态 | 全面支持 |
| `type_text` | 逐帧调用 `TryAssignFieldValue` | InputSystem 测试文本输入 | 已实现，过渡态 | 局部到全面支持 |
| `type_text_fast` | 直接调用 `TryAssignFieldValue` | 保持直接写值 | 已实现 | 局部支持，长期保留 |
| `wait` | `Task.Delay` / 帧等待链路 | 保持当前实现 | 已实现 | 全面支持 |
| `wait_for_element` | `ElementFinder.WaitForElementAsync` | 保持当前实现 | 已实现 | 全面支持 |
| `assert_*` | Finder + 反射 / 文本读取 | 保持当前实现 | 已实现 | 全面支持 |
| `screenshot` | `ScreenshotManager.CaptureAsync` | 保持 API，不在本模块内决定真实截图方案 | 已实现，过渡态 | 局部支持 |

### 驱动选择规则

1. `click`、`double_click`、`hover`、`drag`、`scroll` 目标驱动是 `com.unity.ui.test-framework@6.3.0` 的 `PanelSimulator`（独立包，已添加到 `manifest.json`）。
2. `press_key`、`type_text` 目标驱动是基于 `com.unity.inputsystem` 的测试输入能力。
3. `type_text_fast` 永远不升级为真实输入链路，只负责快速写值。
4. 当前 `SendEvent` / `Event.GetPooled` / 直接赋值版行为必须在文档中标记为“当前实现”或“迁移兼容模式”。
5. 正式验收模式下，动作系统不得把 fallback 执行结果记为“真实用户输入通过”。
### V1 完整内置动作清单

V1 内置动作从 16 个扩展至 38 个，以覆盖 UIToolkit 全部可交互控件类型：

#### 指针交互动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `click` | `selector` | 所有可点击元素 | 单击目标元素 |
| `double_click` | `selector` | 所有可点击元素 | 双击目标元素 |
| `hover` | `selector`、`duration`(可选) | 所有元素 | 鼠标悬停到目标元素上 |
| `drag` | `selector`、`to`(目标选择器)、`duration`(可选) | 所有元素 | 从目标元素拖拽到 `to` 指定元素 |
| `scroll` | `selector`、`delta`(滚动量) | `ScrollView`、`ListView`、`TreeView`、其他可滚动容器 | 在目标元素上触发滚轮滚动 |

#### 文本与键盘动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `press_key` | `key`、`selector`(可选) | 任意获焦元素 | 按下并释放指定按键 |
| `type_text` | `selector`、`value` | `TextField` 及其子类文本输入控件 | 通过真实键盘链路逐字符输入文本 |
| `type_text_fast` | `selector`、`value` | 所有实现 `INotifyValueChanged<string>` 的控件 | 直接写入 `value` 属性（快速路径） |

#### 命令动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `execute_command` | `selector`、`command` | 任意 VisualElement | 向目标元素发送 `ExecuteCommandEvent`，用于触发编辑器内置命令（如 Copy/Paste/SelectAll） |
| `validate_command` | `selector`、`command` | 任意 VisualElement | 向目标元素发送 `ValidateCommandEvent`，用于检查命令是否可用 |

#### 菜单动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `open_context_menu` | `selector` | 任意可呼出上下文菜单的元素 | 在目标元素上触发上下文菜单（右键菜单）弹出 |
| `select_context_menu_item` | `item`（菜单项文本） | 已弹出的上下文菜单 | 在当前弹出的上下文菜单中选择指定菜单项 |
| `open_popup_menu` | `selector` | 任意可呼出弹出菜单的元素 | 在目标元素上触发弹出菜单弹出 |
| `select_popup_menu_item` | `item`（菜单项文本） | 已弹出的弹出菜单 | 在当前弹出的弹出菜单中选择指定菜单项 |
| `assert_menu_item` | `item`（菜单项文本） | 已弹出的菜单 | 断言当前弹出菜单中存在指定菜单项且未禁用 |
| `assert_menu_item_disabled` | `item`（菜单项文本） | 已弹出的菜单 | 断言当前弹出菜单中指定菜单项处于禁用状态 |

#### 值操作动作（V1 新增）

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `set_value` | `selector`、`value` | 所有 `BaseField<T>` 子类 | 直接写入任意控件的 `value`，支持类型自动转换 |
| `select_option` | `selector`、`value`(选项文本) 或 `index`(选项序号) | `DropdownField`、`EnumField`、`EnumFlagsField`、`RadioButtonGroup`、`PopupField`、`MaskField`、`TagField`、`LayerField`、`LayerMaskField` | 在下拉/选择控件中选中指定选项；对 `PopupField<string>`、`TagField`、`LayerField` 走直写值/索引路径，不依赖弹出浮窗 |
| `select_list_item` | `selector`、`index` | `ListView`、`MultiColumnListView` | 选中列表中指定索引的行 |
| `select_tree_item` | `selector`、`id` 或 `index` | `TreeView`、`MultiColumnTreeView` | 选中树视图中指定节点 |
| `toggle_foldout` | `selector`、`expand`(`true`/`false`，可选) | `Foldout` | 展开或折叠 Foldout；不传 `expand` 则切换当前状态 |
| `set_slider` | `selector`、`value` | `Slider`、`SliderInt`、`MinMaxSlider` | 设置滑块值；`MinMaxSlider` 使用 `min_value` 和 `max_value` 参数 |
| `select_tab` | `selector`、`index` 或 `label` | `TabView` | 切换到指定 Tab 页签 |
| `focus` | `selector` | 所有可聚焦元素 | 将键盘焦点设置到目标元素 |
| `drag_reorder` | `selector`、`from_index`、`to_index` | `ListView` | 将 `ListView` 中指定行从 `from_index` 拖拽排序到 `to_index`；基于 `itemsSource` 逻辑重排 |
| `sort_column` | `selector`、`column`(列名或标题) 或 `index`、`direction`(`ascending`/`descending`，可选，默认 ascending) | `MultiColumnListView`、`MultiColumnTreeView` | 对目标列表/树视图按指定列排序；直接写入 `sortColumnDescriptions`，不依赖 UI 拖拽 |
| `resize_column` | `selector`、`column`(列名或标题) 或 `index`、`width`(正数像素) | `MultiColumnListView`、`MultiColumnTreeView` | 设置目标列的像素宽度；直接写入 `Column.width`，不依赖 UI 拖拽 |
| `set_bound_value` | `selector`、`binding_path`、`value` | `PropertyField`、`InspectorElement`、任意绑定容器 | 按 `bindingPath` 找到绑定字段并执行语义赋值 |
| `assert_bound_value` | `selector`、`binding_path`、`expected` | `PropertyField`、`InspectorElement`、任意绑定容器 | 断言绑定字段的当前值 |
| `navigate_breadcrumb` | `selector`，以及 `label` 或 `index` | `ToolbarBreadcrumbs` | 按文本或索引导航 breadcrumb 子项 |
| `set_split_view_size` | `selector`、`size`，可选 `pane` | `TwoPaneSplitView` | 设置固定 pane 目标尺寸 |
| `page_scroller` | `selector`，可选 `direction`、`pages`、`page_size` | `Scroller` | 按分页语义驱动滚动条 |
| `menu_item` | 可选 `selector`，必填 `item`，可选 `kind`、`mode` | `ToolbarMenu`、上下文菜单入口、Popup 类入口 | 统一菜单 DSL，支持打开并选择或断言菜单项状态 |

#### 等待动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `wait` | `duration` | 无 | 等待指定时长 |
| `wait_for_element` | `selector`、`timeout`(可选) | 所有元素 | 等待目标元素出现 |

#### 断言动作

| 动作名 | YAML 参数 | 目标元素类型 | 语义 |
| --- | --- | --- | --- |
| `assert_visible` | `selector` | 所有元素 | 断言元素可见 |
| `assert_not_visible` | `selector` | 所有元素 | 断言元素不可见 |
| `assert_text` | `selector`、`expected` | `Label`、`Button`、`TextField`、任何含 `text` 属性的元素 | 断言元素文本完全等于 `expected` |
| `assert_text_contains` | `selector`、`expected` | 同上 | 断言元素文本包含 `expected` |
| `assert_property` | `selector`、`property`、`expected` | 所有元素 | 断言元素指定属性值等于 `expected` |
| `assert_value` | `selector`、`expected` | 所有 `BaseField<T>` 子类 | 断言控件 `value` 按目标值类型与 `expected` 比较；复杂值沿用 `set_value` 的字符串格式 |
| `assert_enabled` | `selector` | 所有元素 | 断言元素 `enabledSelf=true` 且 `enabledInHierarchy=true` |
| `assert_disabled` | `selector` | 所有元素 | 断言元素 `enabledSelf=false` 或 `enabledInHierarchy=false` |
| `screenshot` | `name`(可选) | 无 | 截取当前窗口截图 |

### `set_value` 控件类型兼容矩阵

`set_value` 动作通过识别目标元素的实际类型，自动将 YAML 中的字符串 `value` 转换为对应 C# 类型后写入。V1 必须覆盖以下控件类型：

| 控件类型 | value 字符串格式 | 转换方式 | 示例 |
| --- | --- | --- | --- |
| `TextField` | 原始字符串 | 直接赋值 | `"hello"` |
| `IntegerField` | 整数文本 | `int.Parse` | `"42"` |
| `LongField` | 长整数文本 | `long.Parse` | `"9999999999"` |
| `FloatField` | 浮点文本 | `float.Parse(InvariantCulture)` | `"3.14"` |
| `DoubleField` | 浮点文本 | `double.Parse(InvariantCulture)` | `"3.14159"` |
| `UnsignedIntegerField` | 无符号整数文本 | `uint.Parse` | `"100"` |
| `UnsignedLongField` | 无符号长整数文本 | `ulong.Parse` | `"100"` |
| `Toggle` | `true`/`false` | `bool.Parse` | `"true"` |
| `Slider` | 浮点文本 | `float.Parse`，写入前 Clamp 到 `[lowValue, highValue]` | `"0.5"` |
| `SliderInt` | 整数文本 | `int.Parse`，写入前 Clamp 到 `[lowValue, highValue]` | `"5"` |
| `MinMaxSlider` | `min,max` 格式 | 解析为两个 float，写入 `Vector2(min, max)` | `"0.2,0.8"` |
| `DropdownField` | 选项文本 | 匹配 `choices` 列表中的值 | `"Option A"` |
| `EnumField` | 枚举名称 | `Enum.Parse` | `"Running"` |
| `EnumFlagsField` | 枚举名称（逗号分隔） | `Enum.Parse` (flags) | `"Read,Write"` |
| `RadioButtonGroup` | 索引文本 | `int.Parse` | `"1"` |
| `Vector2Field` | `x,y` 格式 | 解析为 `Vector2` | `"1.0,2.0"` |
| `Vector3Field` | `x,y,z` 格式 | 解析为 `Vector3` | `"1.0,2.0,3.0"` |
| `Vector4Field` | `x,y,z,w` 格式 | 解析为 `Vector4` | `"1,2,3,4"` |
| `Vector2IntField` | `x,y` 格式 | 解析为 `Vector2Int` | `"1,2"` |
| `Vector3IntField` | `x,y,z` 格式 | 解析为 `Vector3Int` | `"1,2,3"` |
| `RectField` | `x,y,w,h` 格式 | 解析为 `Rect` | `"0,0,100,50"` |
| `RectIntField` | `x,y,w,h` 格式 | 解析为 `RectInt` | `"0,0,100,50"` |
| `BoundsField` | `cx,cy,cz,ex,ey,ez` 格式 | 解析为 `Bounds(center, extents)` | `"0,0,0,1,1,1"` |
| `BoundsIntField` | `px,py,pz,sx,sy,sz` 格式 | 解析为 `BoundsInt(position, size)` | `"0,0,0,2,2,2"` |
| `ColorField` | `r,g,b,a` 格式（0-1）或 `#RRGGBB`/`#RRGGBBAA` | 解析为 `Color` | `"1,0,0,1"` 或 `"#FF0000"` |
| `Hash128Field` | 32 位十六进制字符串 | `Hash128.Parse` | `"0123456789abcdef0123456789abcdef"` |
| `ObjectField` | 资源路径（`Assets/...`）或 `guid:...` | `AssetDatabase.LoadAssetAtPath` / `GUIDToAssetPath` | `"Assets/Examples/Uxml/SampleInteractionWindow.uxml"` |
| `CurveField` | `time:value:inTangent:outTangent;...` | 解析为 `AnimationCurve` | `"0:0:1:1;1:2:0:0"` |
| `GradientField` | `time:#RRGGBBAA;...|time:alpha;...` | 解析为 `Gradient` 的颜色/透明度 key 集合 | `"0:#FF0000FF;1:#00FF00FF|0:1;1:0.5"` |
| `Foldout` | `true`/`false` | `bool.Parse`，控制展开/折叠 | `"true"` |
| 其他 `BaseField<T>` | 字符串 | 反射尝试 `value` 属性赋值；失败时报 `ACTION_TARGET_TYPE_INVALID` | — |

对于 `ObjectField`、`CurveField`、`GradientField`，当前已支持“值级直写 / 断言”路径，但这不等价于真实的 Object Picker、Curve Editor、Gradient Editor 浮窗交互。浮窗链路限制仍以 `cocs/00-UIToolkit控件自动化覆盖与限制说明.md` 为准。

### `select_option` 动作规则

- 当 `value` 参数存在时，在选项列表中查找文本完全匹配的项并选中。
- 当 `index` 参数存在时，按 0-based 索引选中。
- `value` 与 `index` 同时存在时以 `value` 为准。
- 选中后必须触发对应控件的 `ChangeEvent<T>`。
- 对于 `EnumFlagsField` 和 `MaskField`，`value` 允许逗号分隔多个选项表示多选。
- 若目标选项不存在或索引越界，报 `ACTION_PARAMETER_INVALID`。

### `select_list_item` / `select_tree_item` 动作规则

- `select_list_item` 按 0-based `index` 选中列表行，触发 `selectionChanged` 事件。
- `select_tree_item` 按 `id`（若列表提供了 `id`）或 0-based `index` 选中树节点。
- 索引越界时报 `ACTION_PARAMETER_INVALID`。
- `select_list_item` 已支持 `indices` 多选参数；是否真正允许多选仍取决于目标控件是否暴露标准多选 API。

### `set_slider` 动作规则

- 对 `Slider`/`SliderInt`，`value` 参数值写入前 Clamp 到 `[lowValue, highValue]`。
- 对 `MinMaxSlider`，使用 `min_value` 和 `max_value` 两个参数，分别 Clamp 到 `[lowLimit, highLimit]`，且 `min_value <= max_value`。
- 写入后必须触发 `ChangeEvent<T>`。

### `assert_value` 动作规则

- 当目标元素公开可读 `value` 属性时，必须先按该属性的实际 C# 类型把 `expected` 字符串转换为目标值，再进行类型级比较。
- `assert_value` 对 `bool/int/long/uint/ulong/float/double/enum/Vector2/3/4/Vector2Int/3Int/Rect/RectInt/Bounds/BoundsInt/Color/Hash128` 必须复用与 `set_value` 一致的字符串解析规则。
- `float/double/Vector/Color` 比较允许 `0.0001` 量级的浮点容差；整数、枚举、布尔与结构体值使用精确比较。
- 当目标元素不存在 `value` 属性，或 `expected` 无法转换为目标值类型时，回退到字符串比较。
- `Color` 的日志显示允许采用规范化的 `RRGGBBAA` 文本，但断言比较必须接受 `#RRGGBB`、`#RRGGBBAA` 与 `r,g,b,a` 两类输入格式。
## 5. 视觉规格

不涉及

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| 动作类型必须实现 `IAction` | 注册动作时 | Error | `动作 {actionName} 未实现 IAction` |
| 动作名必须唯一 | 注册动作时 | Error | `动作名冲突：{actionName}` |
| 依赖 `selector` 的动作必须成功定位元素 | 执行动作前 | Error | `动作 {actionName} 未找到目标元素` |
| `type_text_fast` 目标元素必须可写入 `value` | 执行 `type_text_fast` 前 | Error | `动作 type_text_fast 的目标元素不可写入 value` |
| `set_value` 的 `value` 必须可转换为目标控件类型 | 执行 `set_value` 前 | Error | `动作 set_value 的值 {value} 无法转换为 {targetType}` |
| `select_option` 的目标必须是选择类控件 | 执行 `select_option` 前 | Error | `动作 select_option 的目标不是选择类控件：{targetType}` |
| `select_option` 指定的选项必须存在 | 执行 `select_option` 时 | Error | `动作 select_option 的选项 {value} 不存在` |
| `select_list_item` / `select_tree_item` 的索引不能越界 | 执行前 | Error | `动作 {actionName} 的索引 {index} 越界` |
| `set_slider` 的目标必须是滑块控件 | 执行 `set_slider` 前 | Error | `动作 set_slider 的目标不是滑块控件：{targetType}` |
| `select_tab` 的目标必须是 `TabView` | 执行 `select_tab` 前 | Error | `动作 select_tab 的目标不是 TabView：{targetType}` |
| `sort_column` / `resize_column` 的目标必须是 `MultiColumnListView` 或 `MultiColumnTreeView` | 执行前 | Error | `动作 {actionName} 的目标不是 MultiColumnListView 或 MultiColumnTreeView：{targetType}` |
| `sort_column` / `resize_column` 指定的 `column` 必须存在 | 执行前 | Error | `动作 {actionName}: 列 {column} 不存在` |
| `resize_column` 的 `width` 必须是正数 | 执行 `resize_column` 前 | Error | `动作 resize_column: width 必须是正数像素值` |
| 正式验收模式下，`type_text` 不得继续走逐帧写值 fallback | 执行 `type_text` 前 | Error | `动作 type_text 在正式验收模式下禁止使用逐帧写值 fallback` |
| 正式验收模式下，指针类动作必须绑定官方 UI 驱动 | 执行 `click`、`double_click`、`hover`、`drag`、`scroll` 前 | Error | `动作 {actionName} 未绑定官方 UI 驱动` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 动作名重复注册 | ACTION_NAME_CONFLICT | `动作名冲突：{actionName}` | 抛异常并阻断初始化 |
| 动作未注册 | ACTION_NOT_FOUND | `未找到动作：{actionName}` | 抛异常并终止当前步骤 |
| 动作参数缺失 | ACTION_PARAMETER_MISSING | `动作 {actionName} 缺少参数 {parameter}` | 抛异常并终止当前步骤 |
| 动作参数非法 | ACTION_PARAMETER_INVALID | `动作 {actionName} 的参数 {parameter} 非法` | 抛异常并终止当前步骤 |
| 动作执行失败或驱动未就绪 | ACTION_EXECUTION_FAILED | `动作 {actionName} 执行失败：{detail}` | 抛异常并标记当前步骤失败 |
| 目标元素类型不兼容 | ACTION_TARGET_TYPE_INVALID | `动作 {actionName} 的目标类型不兼容：{targetType}` | 抛异常并终止当前步骤 |
| 值类型转换失败 | ACTION_VALUE_CONVERSION_FAILED | `动作 {actionName} 的值 {value} 无法转换为 {targetType}` | 抛异常并终止当前步骤 |
| 选项不存在 | ACTION_OPTION_NOT_FOUND | `动作 {actionName} 的选项 {value} 不存在于目标控件中` | 抛异常并终止当前步骤 |
| 索引越界 | ACTION_INDEX_OUT_OF_RANGE | `动作 {actionName} 的索引 {index} 越界，有效范围 [0, {max}]` | 抛异常并终止当前步骤 |

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 被动接收 | 接收执行器构建的步骤参数与运行上下文 | `StepExecutor`、`TestRunner` |
| M04 元素定位与等待 | 被动接收 | 通过 `ActionContext.Finder` 完成定位与等待 | `ElementFinder.WaitForElementAsync` |
| M06 Headed可视化执行 | 主动通知 | 动作执行时向 Headed 状态与日志系统暴露当前步骤信息 | `RuntimeController`、`ActionContext.Log` |
| M07 报告与截图 | 主动通知 | 动作日志、截图附件、失败信息统一写入报告链路 | `IExecutionReporter.RecordAction`、`ScreenshotManager.CaptureAsync` |
| M09 测试基座与Fixture基类 | 被动接收 | Fixture 负责为动作系统提供稳定 `Root`、`Finder`、`ScreenshotManager` 和后续官方宿主能力 | `UnityUIFlowFixture<TWindow>` |
| M12 官方UI测试框架与输入系统测试接入 | 被动接收 | 动作系统按 M12 规定切换到官方 UI 驱动与 InputSystem 驱动 | `ActionContext.Simulator`、`ClickAction`、`PressKeyAction` |

## 8. 技术实现要点

- 关键类与职责：
  - `IAction`：动作统一契约，实际方法签名为 `Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)`。
  - `ActionRegistry`：维护动作名到类型的映射，构造时注册内置动作并扫描自定义动作。
  - `ActionHelpers`：承载当前 fallback 交互能力，如 `DispatchClick`、`DispatchKeyboardEvent`、`DispatchMouseEvent`、`DispatchWheelEvent`、`TryAssignFieldValue`。
  - `IUiInteractionDriver` `(设计提案，实现时确认)`：接入 `com.unity.test-framework` UI 测试子系统后承载指针类动作。
  - `IKeyboardInputDriver` `(设计提案，实现时确认)`：接入 InputSystem 测试输入能力后承载 `press_key` 与 `type_text`。

- 核心流程：

```text
Build ActionRegistry
-> Resolve action by name
-> Build ActionContext
-> Determine driver
   -> pointer action => official UI driver
   -> keyboard/text action => InputSystem test driver
   -> fast write/assert/wait => current local implementation
-> ExecuteAsync
-> Attach logs / screenshots / attachments
```

- 性能约束：
  - `ActionRegistry` 只允许在测试上下文初始化时扫描一次动作类型。
  - `ActionContext` 必须在单个测试内复用 `Finder`、`ScreenshotManager`、`RuntimeController`。
  - 拖拽与滚动的官方驱动绑定必须复用单个宿主上下文，不允许每个步骤重建驱动对象。

- 禁止项：
  - 禁止在动作内部直接写 Markdown 报告文件。
  - 禁止在正式验收模式下静默回退到当前 `SendEvent` fallback。
  - 禁止把 `type_text_fast` 的通过结果表述为“真实键盘输入”。

## 9. 验收标准

1. [创建 `ActionRegistry`] -> [检查内置动作注册表] -> [38 个内置动作均可被 `Resolve` 正确解析]
2. [存在带 `[ActionName("custom_login")]` 的自定义动作类型] -> [创建 `ActionRegistry`] -> [可通过 `custom_login` 解析到该动作类型]
3. [正式验收模式开启且 `com.unity.test-framework` UI 测试子系统已接入] -> [执行 `click`、`double_click`、`hover`、`drag`、`scroll` 回归用例] -> [动作全部通过官方指针链路执行]
4. [正式验收模式开启且 InputSystem 测试输入已接入] -> [执行 `press_key`、`type_text` 回归用例] -> [动作全部通过高保真键盘链路执行]
5. [目标元素是 `TextField`] -> [执行 `type_text_fast`] -> [元素值被直接写入，且用例结果标记为快速路径而非真实输入]
6. [动作执行过程中收到取消信号] -> [动作命中下一等待点] -> [立即抛出 `OperationCanceledException`，步骤终止]
7. [目标元素是 `IntegerField`] -> [执行 `set_value` 传入 `"42"`] -> [`IntegerField.value` 等于 `42`]
8. [目标元素是 `Vector3Field`] -> [执行 `set_value` 传入 `"1.0,2.0,3.0"`] -> [`Vector3Field.value` 等于 `Vector3(1,2,3)`]
9. [目标元素是 `DropdownField`，选项为 `["A","B","C"]`] -> [执行 `select_option` 传入 `value:"B"`] -> [`DropdownField.value` 等于 `"B"`，且触发了 `ChangeEvent<string>`]
10. [目标元素是 `DropdownField`] -> [执行 `select_option` 传入 `value:"不存在的选项"`] -> [报 `ACTION_OPTION_NOT_FOUND`]
11. [目标元素是 `ListView`，含 5 行] -> [执行 `select_list_item` 传入 `index:2`] -> [`ListView.selectedIndex` 等于 `2`]
12. [目标元素是 `Foldout`，当前折叠] -> [执行 `toggle_foldout`] -> [`Foldout.value` 等于 `true`（展开）]
13. [目标元素是 `Slider`，`lowValue=0`，`highValue=10`] -> [执行 `set_slider` 传入 `value:"5"`] -> [`Slider.value` 等于 `5`]
14. [目标元素是 `Toggle`] -> [执行 `set_value` 传入 `"true"`] -> [`Toggle.value` 等于 `true`]
15. [目标元素是 `ColorField`] -> [执行 `set_value` 传入 `"#FF0000"`] -> [`ColorField.value` 等于 `Color(1,0,0,1)`]
16. [目标元素任意] -> [执行 `assert_enabled`] -> [当 `enabledInHierarchy=true` 时断言通过]
17. [目标元素任意] -> [执行 `assert_disabled`] -> [当 `enabledSelf=false` 时断言通过]
18. [目标元素是 `BoundsField`] -> [执行 `set_value` 传入 `"1,2,3,4,5,6"` 并执行 `assert_value`] -> [断言按 `Bounds(center=(1,2,3), extents=(4,5,6))` 通过]
19. [目标元素是 `TreeView`] -> [执行 `select_tree_item` 传入 `id:"120"`] -> [树节点选中状态与绑定状态文本同步更新]
20. [目标元素是 `TabView`] -> [执行 `select_tab` 传入 `label:"About"`] -> [激活页签切换为 `About`]
21. [目标元素是 `MultiColumnListView`，含 `name` 列，`sortable=true`] -> [执行 `sort_column` 传入 `column:"name"`、`direction:"descending"`] -> [`sortColumnDescriptions` 中包含 `name` 列且方向为 `Descending`]
22. [目标元素是 `MultiColumnListView`，`sort_column` 传入不存在的列名] -> [执行] -> [报 `ACTION_OPTION_NOT_FOUND`]
23. [目标元素是 `MultiColumnTreeView`] -> [执行 `resize_column` 传入 `index:0`、`width:"120"`] -> [`columns[0].width` 等于 `Length(120, Pixel)`]
24. [目标元素是普通 `ListView`（非 MultiColumn）] -> [执行 `sort_column`] -> [报 `ACTION_TARGET_TYPE_INVALID`]
25. [目标元素是 `ListView`，含 5 行，`allowReordering=true`] -> [执行 `drag_reorder` 传入 `from_index:0`、`to_index:4`] -> [`itemsSource` 中索引 0 的行移动到索引 4 位置]
26. [目标元素可呼出上下文菜单] -> [执行 `open_context_menu` 然后执行 `select_context_menu_item` 传入 `item:"Copy"`] -> [上下文菜单消失，对应命令被触发]
27. [目标元素可呼出弹出菜单] -> [执行 `open_popup_menu` 然后执行 `assert_menu_item` 传入 `item:"Option A"`] -> [断言通过；执行 `assert_menu_item_disabled` 传入不存在或禁用项] -> [断言通过]
28. [目标元素已打开菜单，菜单中存在禁用项] -> [执行 `assert_menu_item_disabled` 传入该禁用项文本] -> [断言通过；对启用项断言失败]

## 10. 边界规范

- 空数据：
  - 不依赖参数的动作在参数字典为空时仍必须能执行。
  - 依赖 `selector`、`value`、`key` 的动作在参数缺失时必须返回 `ACTION_PARAMETER_MISSING`。

- 单元素：
  - 当选择器只命中一个元素时，动作必须直接作用于该元素，不继续扫描其他候选。

- 上下限临界值：
  - 动作名长度范围固定为 `[1,64]`。
  - 单步附件数量上限固定为 `10`。
  - `drag.duration` 缺省值固定为 `100ms`；`hover.duration` 缺省值固定为 `0ms`。
  - `set_slider` 对 `Slider`/`SliderInt` 写入前 Clamp 到 `[lowValue, highValue]`；`MinMaxSlider` Clamp 到 `[lowLimit, highLimit]`。
  - `select_list_item` 索引范围 `[0, itemsSource.Count - 1]`；空列表报 `ACTION_INDEX_OUT_OF_RANGE`。
  - `set_value` 的 `ColorField` 接受 `#RGB`、`#RRGGBB`、`#RRGGBBAA` 或 `r,g,b,a`（0-1 浮点）格式。

- 异常数据恢复：
  - 单个动作执行失败后，不得污染 `ActionRegistry`。
  - InputSystem 或官方 UI 驱动初始化失败后，不得静默降级为正式基线通过。

## 11. 周边可选功能

- ~~P1：支持动作级驱动显式声明，例如在报告中记录”本步使用 official_ui_test_framework”。~~ **已完成**（步骤结果、报告与 Headed 面板均记录 `host/pointer/keyboard` 与 `driver details`，截图另记录 `screenshot source`）
- ~~P1：支持自定义动作通过依赖注入获取官方 UI 驱动或 InputSystem 驱动。~~ **已完成**（`ActionContext.SimulationSession` / `Simulator` 已向动作层暴露）
- ~~P1：支持 `select_list_item` / `select_tree_item` 多选模式（`indices` 参数逗号分隔多个索引）。~~ **已完成**（`select_list_item` 支持 `indices` 参数）
- ~~P1：支持 `drag_reorder` 动作用于 `ListView` 行拖拽排序。~~ **已完成**
- ~~P2：支持 `MultiColumnListView`/`MultiColumnTreeView` 列头排序点击动作与列宽拖拽动作。~~ **已完成**（`sort_column` / `resize_column` 直接操作 API）
- P2：支持更细粒度的组合键、剪贴板、IME、手势轨迹回放。
- P2：支持 `ObjectField` 的 drag-drop 赋值动作；当前已支持资源路径 / guid 直写，但真实 DragAndDrop 仍需额外接入。

---

## 2026-04-11 实现更新

- 已实现 `assert_menu_item` 与 `assert_menu_item_disabled`，用于断言当前已打开的 `ContextMenuSimulator` / `PopupMenuSimulator` 菜单项状态。
- 已实现 `drag_reorder`，当前边界为基于 `ListView.itemsSource` 的逻辑重排，并刷新集合视图；不覆盖多窗口拖放、文件拖放、`ObjectField` 拖放。
- `select_list_item` 已支持 `indices` 参数，多选场景依赖目标控件暴露标准 selection API。
- `click` / `double_click` / `drag` / `hover` 已支持 `button` 与 `modifiers` 参数；官方链路优先走 `PanelSimulator`，fallback 链路同步透传修饰键和鼠标按钮。
- 已新增高层语义动作：`set_bound_value`、`assert_bound_value`、`navigate_breadcrumb`、`set_split_view_size`、`page_scroller`、`menu_item`。
- `set_value` 针对 `ObjectField` 已扩展资源查找策略：`guid:`、`path:`、`name:`、`asset-name:`、`search:`、`search:TypeName:Needle`。

## 2026-04-12 实现更新

- 内置动作总数从 30 更新为 38，完整清单已补充到 §4 动作清单表格。
- 新增"命令动作"分类：`execute_command`、`validate_command`（UIToolkit-IMGUI 桥接命令，已注册）。
- 新增"菜单动作"分类：`open_context_menu`、`select_context_menu_item`、`open_popup_menu`、`select_popup_menu_item`、`assert_menu_item`、`assert_menu_item_disabled`（已注册，2026-04-11 实现）。
- `drag_reorder` 已从 §11 待开发迁移至 §4 值操作动作表，状态变更为已完成。
- `select_list_item` `indices` 多选参数已从 §11 待开发迁移为已完成。
- `sort_column` / `resize_column` P2 项已从 §11 待开发迁移为已完成（2026-04-12）。
