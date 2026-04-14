# M12 官方UI测试框架与输入系统测试接入 需求文档

版本：1.4.1
日期：2026-04-13
状态：更新基线（官方 host / PanelSimulator / command actions 已接入）

## 1. 模块职责

- 负责：定义 `com.unity.ui.test-framework`、`com.unity.test-framework` 与 `com.unity.inputsystem` 在 UnityUIFlow 中的接入边界、严格失败条件与迁移兼容范围。
- 负责：定义 `UnityUIFlowSimulationSession`、`OfficialUiToolkitTestAvailability`、`IUiPointerDriver` 的职责分层。
- 负责：规定 `click`、`double_click`、`hover`、`drag`、`scroll`、`press_key`、`type_text`、`type_text_fast`、`execute_command`、`validate_command` 的驱动基线。
- 负责：规定 `UnityUIFlowFixture<TWindow>` 与 YAML 执行入口在“官方宿主可用”和“官方宿主缺失”两种环境下的行为。
- 负责：规定 Headed、报告、StepResult 对“实际执行驱动”和“可用性探测结果”的记录口径。
- 不负责：不负责新增 YAML 语法，不负责新增业务动作，不负责真实截图方案。
- 输入/输出：输入为 Unity 版本、包版本、已探测到的程序集符号、动作类型与运行选项；输出为驱动选择结论、失败边界、报告字段与测试覆盖要求。

> 当前环境结论：`com.unity.ui.test-framework` 是一个独立包（从未合并到 `com.unity.test-framework`），版本 `6.3.0`（发布于 2026-01-21）。已添加到 `Packages/manifest.json`。包提供 `EditorWindowUITestFixture<T>`（命名空间 `UnityEditor.UIElements.TestFramework`，程序集 `Unity.UI.TestFramework.Editor`）和 `PanelSimulator`（命名空间 `UnityEngine.UIElements.TestFramework`，程序集 `Unity.UI.TestFramework.Runtime`）。当前 Unity `6000.6.0a2` 环境下已完成安装与基础 PoC 验证，官方 host / `PanelSimulator` 主执行链已接入。

## 2. 数据模型

### PackageBaseline

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| packageName | string | 必填 | 包名 | `com.unity.test-framework`、`com.unity.inputsystem` | 无 |
| required | bool | 必填 | 是否为 V1 必需依赖 | `true`、`false` | `true` |
| currentState | string | 必填 | 当前状态 | `installed`、`installed_but_probe_only`、`installed_but_unused`、`not_installed` | 无 |
| installedVersion | string | 必填 | 当前安装版本 | 非空字符串 | 无 |
| targetRole | string | 必填 | 目标职责 | `ui_host_and_interaction`、`keyboard_input`、`test_runner` | 无 |
| acceptanceBlocking | bool | 必填 | 缺失时是否阻断正式验收 | `true`、`false` | `true` |
| notes | string | 可选 | 备注 | 非空字符串或 `null` | `null` |

### PackageBaseline 实例

| packageName | required | currentState | installedVersion | targetRole | acceptanceBlocking | notes |
| --- | --- | --- | --- | --- | --- | --- |
| `com.unity.test-framework` | `true` | `installed` | `1.7.0` | `test_runner` | `false` | NUnit 集成与测试运行器，不提供 UI 交互仿真能力 |
| `com.unity.ui.test-framework` | `true` | `installed` | `6.3.0` | `ui_host_and_interaction` | `true` | 独立包，提供 `EditorWindowUITestFixture<T>`、`EditorWindowPanelSimulator` 与 `PanelSimulator`；已完成安装验证并接入主执行链 |
| `com.unity.inputsystem` | `true` | `installed` | `1.19.0` | `keyboard_input` | `true` | 当前已接入 `InputTestFixture`、测试键盘与文本事件桥接 |

### OfficialUiToolkitAvailability

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| editorWindowFixtureTypeName | string | 可选 | 已探测到的官方宿主类型名 | `null` 或非空字符串 | `null` |
| panelSimulatorTypeName | string | 可选 | 已探测到的官方指针仿真类型名 | `null` 或非空字符串 | `null` |
| hasEditorWindowFixture | bool | 必填 | 是否探测到官方宿主符号 | `true`、`false` | `false` |
| hasPanelSimulator | bool | 必填 | 是否探测到官方仿真符号 | `true`、`false` | `false` |
| hasConfirmedUiDriver | bool | 必填 | 是否同时探测到官方宿主与官方仿真符号 | `true`、`false` | `false` |
| describeText | string | 必填 | 当前探测说明文本 | 非空字符串 | 无 |

### DriverBindingState

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| hostDriverName | string | 必填 | 当前宿主标识 | `OfficialEditorWindowPanelSimulator`、`EditorWindow.GetWindow<TWindow>()`、`HostWindowManager(EditorWindow.GetWindow)`、`RootOverrideOnly` | `RootOverrideOnly` |
| pointerDriverName | string | 必填 | 当前指针驱动标识 | `PanelSimulator`、`UIToolkitFallbackOnly` | `UIToolkitFallbackOnly` |
| keyboardDriverName | string | 必填 | 当前键盘驱动标识 | `PanelSimulator`、`InputSystemTestFramework+UIToolkitBridge`、`UIToolkitFallbackOnly` | `UIToolkitFallbackOnly` |
| driverDetails | string | 必填 | 组合说明文本 | 非空字符串 | 无 |

### ActionSimulationCapability

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| actionName | string | 必填 | 动作名 | `click`、`double_click`、`hover`、`drag`、`scroll`、`press_key`、`type_text`、`type_text_fast`、`execute_command`、`validate_command` | 无 |
| currentDriver | string | 必填 | 当前实际驱动 | `fallback_event_dispatch`、`inputsystem_bridge`、`direct_value_write` | 无 |
| targetDriver | string | 必填 | 目标驱动 | `official_ui_test_framework`、`inputsystem_test`、`direct_value_write` | 无 |
| fallbackAllowed | bool | 必填 | 当前是否允许 fallback 继续执行 | `true`、`false` | `false` |
| coverageLevel | string | 必填 | V1 支持度 | `full`、`partial` | 无 |
| currentStatus | string | 必填 | 当前状态 | `implemented`、`implemented_as_transition`、`blocked_by_environment` | 无 |

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 探测官方 UI 测试能力 | `UnityUIFlowSimulationSession` 初始化 | 无 | `OfficialUiToolkitTestAvailability.Detect` | 不涉及；只读探测 |
| 绑定指针驱动 | `UnityUIFlowSimulationSession` 初始化 | 无 | `UnityUIFlowSimulationSession` + `IUiPointerDriver` | 不涉及；测试结束释放 |
| 校验官方宿主 strict 边界 | fixture 初始化 / YAML 执行前 | `RequireOfficialHost=true` 且官方宿主符号缺失 | `UnityUIFlowFixture<TWindow>.SetUp`、`TestRunner.RunDefinitionAsync` | 不涉及；直接失败 |
| 校验官方指针驱动 strict 边界 | 指针动作执行前 | `RequireOfficialPointerDriver=true` 且官方指针驱动不可执行 | `ActionHelpers.RequireOfficialPointerDriver` | 不涉及；直接失败 |
| 校验高保真键盘 strict 边界 | `press_key` / `type_text` 执行前 | `RequireInputSystemKeyboardDriver=true` 且 InputSystem 桥接不可用或需回退写值 | `PressKeyAction`、`TypeTextAction` | 不涉及；直接失败 |
| 执行 fallback 指针动作 | `click` / `double_click` / `hover` / `drag` / `scroll` | 当前入口不是官方 host（如 `RootOverrideOnly`） | `FallbackUiPointerDriver` | 不涉及 |
| 执行 InputSystem 键盘动作 | `press_key` / `type_text` | InputSystem 会话不可初始化 | `UnityUIFlowSimulationSession.EnsureInputSystemReady` | 不涉及 |
| 执行编辑器命令动作 | `execute_command` / `validate_command` | 无官方 host 时回退到 pooled command event 派发 | `UnityUIFlowSimulationSession.TryExecuteCommandWithOfficialDriver`、`TryValidateCommandWithOfficialDriver` | 不涉及 |
| 执行快速写值 | `type_text_fast` | 目标元素不可写入 | `TypeTextFastAction` | 不涉及 |

## 4. 交互规格

- 触发事件：fixture 或 YAML 运行创建 `UnityUIFlowSimulationSession`，先探测官方 UI 能力，再绑定宿主标识、指针驱动与键盘驱动。
- 中间状态：`Probe -> BindHost -> BindPointerDriver -> BindKeyboardDriver -> Execute -> Report -> TearDown`。
- 数据提交时机：每个动作执行前写入 `SharedBag` 中的 host/pointer/keyboard/driver-details；步骤完成后写入 `StepResult`。
- 取消/回退行为：运行期始终检查 `CancellationToken`；strict 模式失败时直接抛异常，不允许静默回退。

### 动作驱动基线

| 动作 | 当前实现 | 目标实现 | 当前支持结论 | 接入后支持结论 |
| --- | --- | --- | --- | --- |
| `click` | `PanelSimulator`（官方 host 路径）/ `FallbackUiPointerDriver`（非官方入口） | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径全面支持；非官方入口保留兼容 fallback |
| `double_click` | `PanelSimulator`（官方 host 路径）/ `FallbackUiPointerDriver`（非官方入口） | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径全面支持；非官方入口保留兼容 fallback |
| `hover` | `PanelSimulator`（官方 host 路径）/ `FallbackUiPointerDriver`（非官方入口） | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径全面支持；非官方入口保留兼容 fallback |
| `drag` | `PanelSimulator`（官方 host 路径）/ `FallbackUiPointerDriver`（非官方入口） | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径全面支持；非官方入口保留兼容 fallback |
| `scroll` | `PanelSimulator`（官方 host 路径）/ `FallbackUiPointerDriver`（非官方入口） | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径全面支持；非官方入口保留兼容 fallback |
| `press_key` | `PanelSimulator`（默认官方路径）/ `InputSystemTestFramework+UIToolkitBridge`（strict 或补充链路） | `PanelSimulator` + InputSystem 测试键盘 | 已支持，双链路 | 默认优先官方键盘；需要设备级语义时切到 InputSystem |
| `type_text` | `PanelSimulator`（默认官方路径）/ `InputSystemTestFramework+UIToolkitBridge` + 条件性补偿写值 | `PanelSimulator` + InputSystem 测试文本输入 | 已支持，双链路 | 默认优先官方文本输入；IME 不纳入 V1 |
| `type_text_fast` | 直接写 `value` | 保持直接写值 | 已支持 | 局部支持；不宣称真实输入 |
| `execute_command` | `PanelSimulator.ExecuteCommand`（官方 host 路径）/ `ExecuteCommandEvent.GetPooled` fallback | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径优先走真实命令事件；非官方入口保留兼容 fallback |
| `validate_command` | `PanelSimulator.ValidateCommand`（官方 host 路径）/ `ValidateCommandEvent.GetPooled` fallback | `com.unity.ui.test-framework` PanelSimulator | 已支持 | 官方 host 路径优先走真实命令事件；非官方入口保留兼容 fallback |

### 驱动切换规则

1. 指针类动作必须先走 `IUiPointerDriver` 抽象，不允许动作类直接判断“是否使用官方 API”。
2. `com.unity.ui.test-framework@6.3.0` 已完成安装验证，`IUiPointerDriver` 的默认可执行实现已切换为基于 `PanelSimulator` 的 `OfficialUiPointerDriver`；仅在非官方入口时才回退到 `FallbackUiPointerDriver`。
3. 当 official host 绑定成功时，`pointerDriverName` 必须为 `PanelSimulator`，`hostDriverName` 必须为 `OfficialEditorWindowPanelSimulator`。
4. `RequireOfficialHost=true` 时，fixture 或 YAML 执行入口必须在动作执行前失败。
5. `RequireOfficialPointerDriver=true` 时，`click`、`double_click`、`hover`、`drag`、`scroll` 必须在动作入口失败。
6. `RequireInputSystemKeyboardDriver=true` 时，`press_key` 必须要求成功映射到 InputSystem 键盘；`type_text` 不允许使用直接写值补偿。
7. Headed 与非 Headed 必须共用同一套驱动选择逻辑。

## 5. 视觉规格

不涉及

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `RequireOfficialHost=true` 时不得继续使用 fallback 宿主 | fixture 初始化前 / YAML 执行前 | Error | `正式验收模式下必须使用官方宿主桥接，当前环境未探测到该能力` |
| `RequireOfficialPointerDriver=true` 时不得继续使用 fallback 指针驱动 | 指针动作执行前 | Error | `com.unity.ui.test-framework 官方指针驱动不可用，无法以官方交互基线执行动作 {actionName}` |
| `RequireInputSystemKeyboardDriver=true` 时 `press_key` 不得退回到纯 UIToolkit 事件派发 | `press_key` 执行前 | Error | `InputSystem 测试输入能力不可用，无法以高保真模式执行动作 {actionName}` |
| `RequireInputSystemKeyboardDriver=true` 时 `type_text` 不得补偿写值 | `type_text` 执行时 | Error | `动作 type_text 在高保真模式下禁止回退到直接写值实现` |
| `type_text_fast` 目标元素必须可写入 `value` | `type_text_fast` 执行前 | Error | `动作 type_text_fast 的目标元素不可写入 value` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| strict 模式下官方宿主不可用 | FIXTURE_WINDOW_CREATE_FAILED | `正式验收模式下未能创建官方测试宿主：{windowType}` | 抛异常并终止当前测试 |
| strict 模式下官方指针驱动不可用 | OFFICIAL_UI_TEST_FRAMEWORK_UNAVAILABLE | `com.unity.ui.test-framework 官方指针驱动不可用，动作 {actionName} 无法执行` | 抛异常并终止当前步骤 |
| strict 模式下 InputSystem 键盘桥接不可用 | INPUT_SYSTEM_TEST_FRAMEWORK_UNAVAILABLE | `缺少 InputSystem 测试输入能力，动作 {actionName} 无法执行` | 抛异常并终止当前步骤 |
| strict 模式下 `type_text` 需要补偿写值 | ACTION_EXECUTION_FAILED | `动作 type_text 在高保真模式下禁止回退到直接写值实现` | 抛异常并终止当前步骤 |
| 当前环境误报官方驱动 | ACTION_EXECUTION_FAILED | `动作 {actionName} 未绑定官方驱动，但结果被错误标记为官方链路` | 抛异常并终止当前步骤 |

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M05 动作系统与CSharp扩展 | 被动接收 | 指针动作必须经过 `IUiPointerDriver` 抽象，键盘动作必须记录真实驱动名 | `ActionHelpers`、`ClickAction`、`PressKeyAction` |
| M06 Headed可视化执行 | 被动接收 | Headed 面板必须显示实际宿主与实际驱动，而不是仅显示“理论目标驱动” | `HeadedTestWindow`、`HeadedPanelState` |
| M07 报告与截图 | 被动接收 | `StepResult` 与 Markdown 报告必须记录 host/pointer/keyboard/driver-details | `StepResult`、`MarkdownReporter` |
| M09 测试基座与Fixture基类 | 被动接收 | fixture 必须在 strict 模式下阻断 fallback 宿主冒充官方宿主 | `UnityUIFlowFixture<TWindow>` |
| M10 测试用例说明与编写规范 | 主动通知 | 新增 probe、strict fail-fast、fallback-driver 报告测试要求 | `Assets/Tests/*.cs` |

## 8. 技术实现要点

- 关键类与职责：
  - `OfficialUiToolkitTestAvailability`：只负责探测符号与输出说明，不直接执行动作。
  - `UnityUIFlowSimulationSession`：持有当前测试的 `OfficialUiToolkitTestAvailability`、`IUiPointerDriver`、InputSystem 会话与 host/pointer/keyboard 标识。
  - `IUiPointerDriver`：统一封装 `Click`、`HoverAsync`、`DragAsync`、`Scroll`。
  - `FallbackUiPointerDriver`：非官方宿主入口下的 fallback 指针驱动，实现 `SendEvent` 兼容链路。
  - `OfficialUiPointerDriver`：基于 `PanelSimulator` 的官方指针驱动实现。
  - `OfficialEditorWindowHostBridge`：把 `EditorWindow` 绑定为 `EditorWindowPanelSimulator` 官方宿主。
  - `UnityUIFlowFixture<TWindow>`：绑定宿主模式，并在 `RequireOfficialHost=true` 时 fail-fast。
  - `ActionHelpers.RequireOfficialPointerDriver`：负责 strict 模式的官方指针驱动校验。

- 官方包引用方式：
  - 包名：`com.unity.ui.test-framework`
  - 版本：`6.3.0`
  - 程序集引用：`Unity.UI.TestFramework.Runtime`、`Unity.UI.TestFramework.Editor`
  - using 语句：`using UnityEngine.UIElements.TestFramework;`（Runtime）、`using UnityEditor.UIElements.TestFramework;`（Editor）

- PanelSimulator 已确认 API（来自官方文档）：

| 方法 | 签名 | 对应 UnityUIFlow 动作 |
| --- | --- | --- |
| `Click` | `Click(VisualElement ve, MouseButton button, EventModifiers modifiers)` | `click` |
| `DoubleClick` | `DoubleClick(VisualElement ve, MouseButton button, EventModifiers modifiers)` | `double_click` |
| `MouseMove` | `MouseMove(VisualElement ve, EventModifiers modifiers)` | `hover` |
| `DragAndDrop` | `DragAndDrop(Vector2 from, Vector2 to, MouseButton button, EventModifiers modifiers)` | `drag` |
| `ScrollWheel` | `ScrollWheel(VisualElement ve, Vector2 delta)` | `scroll` |
| `TypingText` | `TypingText(string text, bool useKeypad)` | `type_text` |
| `KeyDown` | `KeyDown(KeyCode keyCode, EventModifiers modifiers)` | `press_key` |
| `KeyPress` | `KeyPress(KeyCode keyCode, EventModifiers modifiers)` | `press_key` |
| `KeyUp` | `KeyUp(KeyCode keyCode, EventModifiers modifiers)` | `press_key` |
| `TabKeyPress` | `TabKeyPress(EventModifiers modifiers)` | 焦点导航 |
| `ReturnKeyPress` | `ReturnKeyPress(EventModifiers modifiers)` | 提交 |
| `FrameUpdate` | `FrameUpdate()` / `FrameUpdateMs(long timeMs)` | UI 刷新 |
| `ExecuteCommand` | `ExecuteCommand(string commandName)` | 编辑器命令 |
| `ValidateCommand` | `ValidateCommand(string commandName)` | 编辑器命令预校验 |
| `MouseDown/Up` | `MouseDown(ve)`、`MouseUp(ve)` | 底层鼠标事件 |

- 核心流程：

```text
Initialize session
-> Detect EditorWindowUITestFixture / PanelSimulator
-> Bind hostDriverName
-> Bind pointerDriver
   -> official host path: OfficialUiPointerDriver
   -> non-official path: FallbackUiPointerDriver
-> Bind keyboard driver
-> Execute step
   -> pointer action => pointerDriver
   -> press_key/type_text => InputSystem bridge
   -> execute_command/validate_command => PanelSimulator command path or pooled command fallback
   -> strict mode => fail before fallback
-> Write StepResult host/pointer/keyboard/driver-details
```

- 当前已接入的用户入口（2026-04-11）：
  - CLI 已支持 `-unityUIFlow.requireOfficialHost`、`-unityUIFlow.requireOfficialPointerDriver`、`-unityUIFlow.requireInputSystemKeyboardDriver`、`-unityUIFlow.preStepDelayMs`。
  - Headed 面板已提供 `Require Official Host`、`Require Official Pointer Driver`、`Require InputSystem Keyboard Driver` 开关，并显示 `Driver Details`。
  - Batch Runner 面板已提供同名 strict 开关，并映射到 `TestOptions`。
  - Project Settings 已新增 strict 默认值开关，作为项目级最低要求，不允许入口层静默放宽。
  - 动作层已暴露 `execute_command` 与 `validate_command`，默认优先走 `PanelSimulator.ExecuteCommand/ValidateCommand`。

- 性能约束：
  - 单次测试会话内只探测一次官方 UI 能力。
  - 指针驱动对象在单次测试会话内复用。
  - InputSystem 会话必须按测试用例隔离，不允许跨测试复用。

- 禁止项：
  - 禁止把“仅探测到程序集符号”写成“官方链路已执行”。
  - 禁止 strict 模式下自动回退到 fallback 宿主或 fallback 指针驱动。
  - 禁止 `type_text_fast` 的通过结果宣称覆盖真实文本输入语义。

## 9. 验收标准

1. [当前环境初始化 `UnityUIFlowSimulationSession`] -> [执行 probe] -> [`OfficialUiToolkitTestAvailability.Describe()` 返回非空字符串，且当前环境不把 fallback 指针驱动误写为官方驱动]
2. [`RequireOfficialHost=true` 且当前环境缺失官方宿主符号] -> [初始化 `UnityUIFlowFixture<TWindow>` 或 YAML 执行入口] -> [立即失败并返回官方宿主缺失结论]
3. [`RequireOfficialPointerDriver=true` 且当前环境缺失官方指针驱动] -> [执行 `click`、`double_click`、`hover`、`drag`、`scroll`] -> [动作在入口失败，不执行 fallback]
4. [`RequireInputSystemKeyboardDriver=true`] -> [执行 `press_key`] -> [成功走 InputSystem 键盘桥接，且结果中记录 `InputSystemTestFramework+UIToolkitBridge`]
5. [`RequireInputSystemKeyboardDriver=true`] -> [执行 `type_text`] -> [如果需要直接写值补偿则立即失败，不得把补偿写值记为高保真输入通过]
6. [以 `RootOverrideOnly` 等非官方入口执行 `click`、`double_click`、`hover`、`drag`、`scroll`] -> [动作通过 fallback 驱动执行] -> [报告与 Headed 中明确显示 `UIToolkitFallbackOnly`]
7. [fixture 或 YAML host-window 成功绑定 official host] -> [`OfficialUiPointerDriver` / `PanelSimulator` 成为默认执行链] -> [`pointerDriverName=PanelSimulator`，`hostDriverName=OfficialEditorWindowPanelSimulator`]
8. [fixture 或 YAML host-window 已绑定 official host，当前焦点元素存在] -> [执行 `execute_command` / `validate_command`] -> [优先通过 `PanelSimulator` 分发命令事件，命令回调可被目标 panel 捕获]

## 10. 边界规范

- 空数据：
  - 当前环境缺失官方宿主或官方指针驱动时，strict 模式必须直接失败。
  - 当前环境不要求阻断兼容模式；兼容模式仍允许 fallback 驱动运行。

- 单元素：
  - 单个 `Button`、单个 `TextField`、单个 `ScrollView` 在兼容模式下必须可由 fallback 驱动独立完成回归。

- 上下限临界值：
  - `type_text_fast` 只覆盖值写入，不覆盖 Enter、Tab、快捷键、IME。
  - `press_key` 的高保真路径只覆盖已实现的按键映射；未映射按键在 strict 模式下必须失败。

- 异常数据恢复：
  - strict 模式失败后不得静默切回 fallback 继续执行。
  - InputSystem 会话异常后必须在 `Dispose` / `TearDown` 中释放测试设备与上下文。

## 11. 周边可选功能

- P1：继续补强“官方主链路已接入但仍非全面真实用户语义”的边界说明，例如 IME、系统剪贴板和多设备输入。
- P1：补充直接继承 `EditorWindowUITestFixture<TWindow>` 的可选实现路径评估与专项回归。
- P1：在报告中补充更细粒度的官方/兼容驱动来源说明。
- P2：支持 IME 组合输入、剪贴板粘贴、多设备同时输入等高级场景。
- P2：支持真实截图回归与像素级差异报告。

---

## 2026-04-11 实现更新

- 上述 3 个 P1 项当前已落地：
  - `ContextMenuSimulator` / `PopupMenuSimulator` 已通过 `open_context_menu`、`select_context_menu_item`、`open_popup_menu`、`select_popup_menu_item`、`assert_menu_item`、`assert_menu_item_disabled` 暴露到动作层。
  - `Shift+Click`、`Ctrl+Click`、`Alt+Drag` 等修饰键/鼠标按钮组合已由 `PanelSimulator` 官方链路优先承接，fallback 链路也同步传递 `EventModifiers`。
  - Headed 面板已显示 `host/pointer/keyboard` 与 `driver details`；报告同时记录 `screenshot source`，可区分窗口抓取、屏幕裁剪与 fallback。
- 已补充高层动作封装来承接官方宿主链路之上的复杂控件语义：
  - `navigate_breadcrumb`
  - `set_split_view_size`
  - `page_scroller`
  - `menu_item`
- 仍保持为边界外的范围：
  - IME 组合输入、剪贴板系统级操作、多设备并行输入。
  - 像素级截图比对与视觉 diff 报告。
