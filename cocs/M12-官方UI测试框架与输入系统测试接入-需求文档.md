# M12 官方UI测试框架与输入系统测试接入 需求文档

版本：1.1.0
日期：2026-04-10
状态：定稿基线（已更新：`com.unity.ui.test-framework` 并入 `com.unity.test-framework`）

## 1. 模块职责

- 负责：定义 `com.unity.test-framework`（含 UI 测试子系统，原 `com.unity.ui.test-framework` 功能已合并至此）与基于 `com.unity.inputsystem` 的测试输入能力在 UnityUIFlow 中的正式接入边界。
- 负责：规定哪些动作必须切换到官方仿真链路，哪些动作允许保留直接写值快速路径。
- 负责：规定 `UnityUIFlowFixture<TWindow>` 与官方 `EditorWindowUITestFixture<TWindow>` 的目标宿主关系。
- 负责：规定 Headed、报告、YAML 执行、C# Page Object 在接入后的能力提升范围与验收口径。
- 不负责：不负责重写 YAML 语法，不负责定义新的业务动作，不负责修改报告格式。
- 输入/输出：输入为当前仓库实现、已安装包状态、动作列表与测试需求；输出为包依赖基线、动作驱动分层、宿主切换规则、测试覆盖要求。

> **包变更说明**：Unity 官方已移除 `com.unity.ui.test-framework` 独立包，将其 UI 宿主（`EditorWindowUITestFixture<TWindow>`）与交互仿真（`PanelSimulator` 等）能力全部合并到 `com.unity.test-framework`。当前仓库已安装 `com.unity.test-framework@1.7.0`，无需额外安装其他 UI 测试包。

## 2. 数据模型

### PackageBaseline

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| packageName | string | 必填 | 包名 | `com.unity.test-framework`、`com.unity.inputsystem` | 无 |
| required | bool | 必填 | 是否为首版必需依赖 | `true`、`false` | `true` |
| currentState | string | 必填 | 当前仓库状态 | `installed`、`not_installed`、`installed_but_unused` | 无 |
| installedVersion | string | 必填 | 当前已安装版本 | 非空字符串 | 无 |
| targetRole | string | 必填 | 目标职责 | `ui_host_and_interaction`、`keyboard_input`、`test_runner` | 无 |
| acceptanceBlocking | bool | 必填 | 缺失时是否阻断正式验收 | `true`、`false` | `true` |
| notes | string | 可选 | 备注 | 非空字符串或 `null` | `null` |

### PackageBaseline 实例

| packageName | required | currentState | installedVersion | targetRole | acceptanceBlocking | notes |
| --- | --- | --- | --- | --- | --- | --- |
| `com.unity.test-framework` | `true` | `installed_but_unused`（UI 测试 API 未接入） | `1.7.0` | `ui_host_and_interaction` + `test_runner` | `true` | 含原 `com.unity.ui.test-framework` 全部能力 |
| `com.unity.inputsystem` | `true` | `installed_but_unused`（测试输入 API 未接入） | `1.19.0` | `keyboard_input` | `true` | 需启用 InputSystem 测试输入 API |

### ActionSimulationCapability

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| actionName | string | 必填 | 内置动作名 | `click`、`double_click`、`type_text`、`type_text_fast`、`press_key`、`drag`、`scroll`、`hover`、`wait`、`wait_for_element`、`assert_visible`、`assert_not_visible`、`assert_text`、`assert_text_contains`、`assert_property`、`screenshot` | 无 |
| currentDriver | string | 必填 | 当前仓库实际驱动 | `fallback_event_dispatch`、`direct_value_write`、`engine_only` | 无 |
| targetDriver | string | 必填 | 接入后目标驱动 | `official_ui_test_framework`、`inputsystem_test`、`direct_value_write`、`engine_only` | 无 |
| fallbackAllowed | bool | 必填 | 正式基线下是否允许 fallback 继续作为验收路径 | `true`、`false` | `false` |
| coverageLevel | string | 必填 | 接入后期望覆盖等级 | `full`、`partial` | 无 |
| currentStatus | string | 必填 | 当前实现状态 | `implemented`、`implemented_as_transition`、`not_applicable` | 无 |

### FixtureIntegrationCapability

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| hostMode | string | 必填 | Fixture 宿主模式 | `current_fallback_fixture`、`official_editor_window_fixture` | 无 |
| currentImplementation | string | 必填 | 当前实现标识 | `UnityUIFlowFixture<TWindow>` | 无 |
| targetImplementation | string | 必填 | 目标实现标识 | `EditorWindowUITestFixture<TWindow>` | 无 |
| packageDependency | string | 必填 | 宿主模式依赖的包 | `com.unity.test-framework`、`none` | `none` |
| acceptanceBaseline | bool | 必填 | 是否可作为正式验收宿主 | `true`、`false` | `false` |
| notes | string | 可选 | 备注 | 非空字符串或 `null` | `null` |

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 校验官方测试依赖基线 | 框架初始化前的预检流程 | `com.unity.test-framework` UI 测试子系统类型无法加载 | `UnityUIFlowProjectSettingsUtility` `(设计提案，实现时确认)` | 不涉及；只读校验 |
| 构建动作驱动能力映射 | `ActionRegistry` 初始化后、执行前 | 动作未注册；动作目标驱动未定义 | `ActionRegistry` + `ActionContext.Simulator` | 不涉及；运行期只读 |
| 通过官方 UI 宿主创建测试窗口 | `UnityUIFlowFixture<TWindow>` 初始化 | `EditorWindowUITestFixture<TWindow>` 类型不可加载 | `EditorWindowUITestFixture<TWindow>` `(目标基线，待接入)` | 不涉及；每次测试重新创建 |
| 通过官方 UI 测试框架执行指针类动作 | `IAction.ExecuteAsync` 执行 `click`、`double_click`、`drag`、`hover`、`scroll` | 官方驱动不可用且当前运行处于正式验收模式 | `ClickAction`、`DoubleClickAction`、`DragAction`、`HoverAction`、`ScrollAction` + `(设计提案，实现时确认)` | 不涉及；动作对 UI 的影响由被测窗口承担 |
| 通过 InputSystem 测试能力执行键盘类动作 | `IAction.ExecuteAsync` 执行 `press_key`、`type_text` | InputSystem 测试驱动不可用且当前运行处于正式验收模式 | `PressKeyAction`、`TypeTextAction` + `(设计提案，实现时确认)` | 不涉及 |
| 执行直接写值快速输入 | `IAction.ExecuteAsync` 执行 `type_text_fast` | 目标元素不可写 | `TypeTextFastAction` | 不涉及 |

## 4. 交互规格

- 触发事件：`StepExecutor` 进入动作步骤后，先通过 `ActionRegistry.Resolve` 获取 `IAction`，再根据动作类别绑定 `com.unity.test-framework` UI 驱动或 InputSystem 驱动。
- 中间状态：`当前代码 fallback -> 官方包 API 已加载 -> 动作驱动已绑定 -> 官方链路执行 -> 结果写回 StepResult`。
- 数据提交时机：动作执行结果在 `IAction.ExecuteAsync` 返回或抛出异常时统一提交给 `StepResult`。
- 取消/回退行为：动作执行期间必须持续检查 `ActionContext.CancellationToken`；取消后立即终止，不做通用回滚。

### 动作接入分层

| 动作 | 当前实现 | 接入后目标 | 当前支持结论 | 接入后支持结论 |
| --- | --- | --- | --- | --- |
| `click` | `PointerDown/Up` + `MouseDown/Up` 事件派发 | `com.unity.test-framework` UI 测试子系统指针点击 | 已支持，过渡实现 | 全面支持 |
| `double_click` | 两次 click 派发 | `com.unity.test-framework` UI 测试子系统双击 | 已支持，过渡实现 | 全面支持 |
| `hover` | `MouseMove` 派发 | `com.unity.test-framework` UI 测试子系统悬停 | 已支持，过渡实现 | 全面支持 |
| `drag` | `MouseDown -> MouseMove* -> MouseUp` 派发 | `com.unity.test-framework` UI 测试子系统拖拽 | 已支持，过渡实现 | 全面支持 |
| `scroll` | `WheelEvent` 派发 | `com.unity.test-framework` UI 测试子系统滚动 | 已支持，过渡实现 | 局部到全面支持；取决于 API 对 `ScrollView`/自定义控件的覆盖 |
| `press_key` | `KeyDownEvent` + `KeyUpEvent` 派发 | InputSystem 测试键盘输入 | 已支持，过渡实现 | 全面支持 |
| `type_text` | 逐帧写入 `value` | InputSystem 测试文本输入 | 已支持，过渡实现 | 局部到全面支持；文本输入、快捷键、提交键可覆盖，IME 组合输入不纳入 V1 |
| `type_text_fast` | 直接写入 `value` | 保持直接写值 | 已支持 | 局部支持；仅保证值写入，不保证真实输入链路 |
| `wait` | 引擎等待 | 保持引擎等待 | 已支持 | 全面支持 |
| `wait_for_element` | Finder 等待 | 保持 Finder 等待 | 已支持 | 全面支持 |
| `assert_*` | Finder/反射断言 | 保持 Finder/反射断言 | 已支持 | 全面支持 |
| `screenshot` | `ScreenshotManager` 占位截图 | 真实截图链路 | 已支持，过渡实现 | 局部支持；接入目标不由本模块单独完成 |

### 官方链路切换规则

1. 当动作属于指针类动作时，必须优先使用 `com.unity.test-framework` UI 测试子系统提供的官方交互能力。
2. 当动作属于键盘文本输入类动作时，必须优先使用基于 `com.unity.inputsystem` 的测试输入能力。
3. `type_text_fast` 始终保留为直接写值快速路径，不切换到真实输入链路。
4. 当前 `SendEvent` / 直接赋值版行为只允许在"迁移兼容模式"中存在，不作为正式验收结论。
5. Headed 模式与非 Headed 模式必须共用同一套官方驱动选择逻辑，不允许 Headed 走官方、CI 走 fallback。

## 5. 视觉规格

不涉及

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `com.unity.test-framework` UI 测试子系统不可加载时不得宣称支持官方 UI 交互基线 | 执行指针类动作前 | Error | `com.unity.test-framework UI 测试子系统不可用，无法以官方交互基线执行动作 {actionName}` |
| InputSystem 测试输入能力未就绪时不得宣称支持高保真键盘输入 | 执行 `press_key` 或 `type_text` 前 | Error | `InputSystem 测试输入能力未就绪，无法以高保真模式执行动作 {actionName}` |
| `type_text_fast` 目标元素必须可写入 `value` | 执行 `type_text_fast` 前 | Error | `动作 type_text_fast 的目标元素不可写入 value` |
| `type_text` 在正式验收模式下不得回退到直接写值 | 执行 `type_text` 前 | Error | `动作 type_text 在正式验收模式下禁止回退到直接写值实现` |
| `UnityUIFlowFixture<TWindow>` 在正式验收模式下不得继续使用 `EditorWindow.GetWindow<TWindow>()` 作为最终宿主 | Fixture 初始化前 | Error | `正式验收模式下必须使用官方 EditorWindowUITestFixture 宿主` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| `com.unity.test-framework` UI 测试子系统不可用 | ACTION_EXECUTION_FAILED | `com.unity.test-framework UI 测试子系统不可用，动作 {actionName} 无法执行` | 抛异常并终止当前步骤 |
| 缺少 InputSystem 测试输入能力 | ACTION_EXECUTION_FAILED | `缺少 InputSystem 测试输入能力，动作 {actionName} 无法执行` | 抛异常并终止当前步骤 |
| 指针类动作仍落到 fallback 派发路径 | ACTION_EXECUTION_FAILED | `动作 {actionName} 未绑定官方驱动` | 抛异常并终止当前步骤 |
| `type_text_fast` 目标元素不可写 | ACTION_TARGET_TYPE_INVALID | `动作 type_text_fast 的目标类型不支持直接写值：{targetType}` | 抛异常并终止当前步骤 |
| 正式验收模式下 fixture 未绑定官方宿主 | FIXTURE_WINDOW_CREATE_FAILED | `正式验收模式下未能创建官方测试宿主：{windowType}` | 抛异常并终止当前测试 |

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M05 动作系统与CSharp扩展 | 被动接收 | 指定内置动作的目标驱动与 fallback 退场边界 | `ActionRegistry.Resolve`、`IAction.ExecuteAsync`、`ActionContext.Simulator` |
| M06 Headed可视化执行 | 被动接收 | Headed 模式必须与正式动作驱动共享同一宿主和同一动作执行语义 | `HeadedTestWindow`、`RuntimeController` |
| M07 报告与截图 | 被动接收 | 官方驱动接入后，报告中记录的动作语义必须与实际执行链路一致 | `ScreenshotManager.CaptureAsync`、`MarkdownReporter.RecordAction` |
| M09 测试基座与Fixture基类 | 被动接收 | 将当前 fallback fixture 切换为 `com.unity.test-framework` 中的 `EditorWindowUITestFixture<TWindow>` 作为正式基线 | `UnityUIFlowFixture<TWindow>` |
| M10 测试用例说明与编写规范 | 主动通知 | 新增官方交互链路、InputSystem 链路、迁移期 fallback 的测试覆盖要求 | `Assets/Tests/*.cs` |

## 8. 技术实现要点

- 关键类与职责：
  - `ActionRegistry`：继续负责动作注册与解析，不负责具体驱动选择。
  - `ActionContext`：继续作为执行上下文；其中 `Simulator` 字段必须收敛为 `com.unity.test-framework` UI 驱动包装对象 `(设计提案，实现时确认)`。
  - `UnityUIFlowFixture<TWindow>`：迁移期仍存在，但正式验收时必须建立到 `EditorWindowUITestFixture<TWindow>`（位于 `com.unity.test-framework`）的官方宿主桥接。
  - `IUiInteractionDriver` `(设计提案，实现时确认)`：封装 `com.unity.test-framework` UI 测试子系统中的 `click`、`double_click`、`hover`、`drag`、`scroll` 官方实现。
  - `IKeyboardInputDriver` `(设计提案，实现时确认)`：封装 `press_key`、`type_text` 的 InputSystem 测试输入实现。

- 核心流程：

```text
Initialize fixture
-> Validate package baseline (com.unity.test-framework UI subsystem)
-> Create official EditorWindow host (EditorWindowUITestFixture<TWindow>)
-> Build ActionContext with official driver bindings
-> Execute step
   -> pointer action => com.unity.test-framework UI test driver
   -> keyboard/text action => InputSystem test driver
   -> fast write action => direct value write
-> Record result / screenshot / headed highlight
```

- 性能约束：
  - 驱动绑定必须在单次测试上下文内复用，不允许每个步骤重新反射查找官方类型。
  - 指针移动与拖拽仿真不得为每个中间点创建额外宿主窗口。
  - InputSystem 测试输入上下文必须按测试用例隔离，禁止跨测试复用状态化设备。

- 禁止项：
  - 禁止在正式验收模式下把 `SendEvent` fallback 结果写成"真实用户交互通过"。
  - 禁止在 `type_text_fast` 的通过结果中宣称覆盖了快捷键、按键导航、提交键或文本组合输入。
  - 禁止 Headed 模式与批处理模式使用不同的动作语义基线。

## 9. 验收标准

1. [`com.unity.test-framework@1.7.0` UI 测试子系统 API 可加载] -> [执行包含 `click`、`double_click`、`drag`、`hover`、`scroll` 的官方链路回归用例] -> [动作全部通过官方 UI 驱动执行，且测试结果不再依赖 fallback 派发事件]
2. [InputSystem 测试输入能力已就绪] -> [执行包含 `press_key`、`type_text` 的键盘链路回归用例] -> [目标控件响应真实按键行为，且不再通过逐帧写值达成通过]
3. [执行 `type_text_fast`] -> [输入文本到 `TextField` 或其他可写字段控件] -> [控件值被直接写入，且文档与测试明确标记为快速路径而非真实输入]
4. [Headed 模式开启] -> [执行指针类动作与键盘类动作] -> [高亮、步进、失败暂停与实际动作执行链路一致]
5. [正式验收模式开启且官方宿主 API 不可加载] -> [初始化 `UnityUIFlowFixture<TWindow>`] -> [测试立即失败，并返回官方宿主缺失结论]
6. [当前 fallback 实现仍存在于迁移期代码中] -> [执行兼容模式回归用例] -> [能验证迁移兼容性，但不会被纳入正式交互能力验收结论]

## 10. 边界规范

- 空数据：
  - `com.unity.test-framework` UI 测试子系统不可加载时，所有指针类动作在正式验收模式下必须直接失败。
  - 缺少 InputSystem 测试输入能力时，`press_key` 与 `type_text` 在正式验收模式下必须直接失败。

- 单元素：
  - 单个 `Button`、单个 `TextField`、单个 `ScrollView` 场景必须能独立完成官方链路回归，不依赖示例窗口外的额外上下文。

- 上下限临界值：
  - `type_text_fast` 只覆盖"值写入成功"；不覆盖 Enter、Tab、快捷键、组合键。
  - `scroll` 对 `ScrollView` 及常规可滚动容器必须纳入首版验收；对自定义吸附滚动控件的高级行为列为局部支持。

- 异常数据恢复：
  - 官方驱动执行失败后，必须返回明确错误并停止当前步骤；不得静默切回 fallback。
  - InputSystem 测试输入上下文异常后，必须在 `TearDown` 中释放设备与上下文，不得污染后续测试。

## 11. 周边可选功能

- P1：支持指针修饰键组合，如 `Shift+Click`、`Ctrl+Click`，通过 `com.unity.test-framework` UI 测试子系统的修饰键 API 实现。
- P1：支持在 Headed 面板中显式显示"当前步骤使用的驱动类型"（官方驱动 / InputSystem / fallback）。
- P1：支持失败步骤自动重试（可配置次数），配合官方驱动的幂等语义减少环境噪音导致的误报。
- P1：支持 `assert_screenshot_matches` 视觉回归动作，对比截图与基准图，输出像素差异报告。
- P1：步骤执行时间记录至报告，标注超出阈值的慢步骤。
- P2：支持 IME 组合输入、剪贴板粘贴、多设备同时输入等高级场景。
- P2：支持 Watch 模式：YAML 文件或 C# 动作文件变更时自动触发对应用例重跑。
- P2：支持 HTML 格式报告，内嵌截图缩略图与步骤执行时序图。
