# M05 动作系统与CSharp扩展 需求文档

版本：1.2.0  
日期：2026-04-10  
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
| actionName | string | 必填 | 内置动作名 | 现有 16 个内置动作之一 | 无 |
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

1. `click`、`double_click`、`hover`、`drag`、`scroll` 目标驱动是 `com.unity.test-framework` UI 测试子系统（含原 `com.unity.ui.test-framework` 能力）。
2. `press_key`、`type_text` 目标驱动是基于 `com.unity.inputsystem` 的测试输入能力。
3. `type_text_fast` 永远不升级为真实输入链路，只负责快速写值。
4. 当前 `SendEvent` / `Event.GetPooled` / 直接赋值版行为必须在文档中标记为“当前实现”或“迁移兼容模式”。
5. 正式验收模式下，动作系统不得把 fallback 执行结果记为“真实用户输入通过”。

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

1. [创建 `ActionRegistry`] -> [检查内置动作注册表] -> [16 个内置动作均可被 `Resolve` 正确解析]
2. [存在带 `[ActionName("custom_login")]` 的自定义动作类型] -> [创建 `ActionRegistry`] -> [可通过 `custom_login` 解析到该动作类型]
3. [正式验收模式开启且 `com.unity.test-framework` UI 测试子系统已接入] -> [执行 `click`、`double_click`、`hover`、`drag`、`scroll` 回归用例] -> [动作全部通过官方指针链路执行]
4. [正式验收模式开启且 InputSystem 测试输入已接入] -> [执行 `press_key`、`type_text` 回归用例] -> [动作全部通过高保真键盘链路执行]
5. [目标元素是 `TextField`] -> [执行 `type_text_fast`] -> [元素值被直接写入，且用例结果标记为快速路径而非真实输入]
6. [动作执行过程中收到取消信号] -> [动作命中下一等待点] -> [立即抛出 `OperationCanceledException`，步骤终止]

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

- 异常数据恢复：
  - 单个动作执行失败后，不得污染 `ActionRegistry`。
  - InputSystem 或官方 UI 驱动初始化失败后，不得静默降级为正式基线通过。

## 11. 周边可选功能

- P1：支持动作级驱动显式声明，例如在报告中记录“本步使用 official_ui_test_framework”。
- P1：支持自定义动作通过依赖注入获取官方 UI 驱动或 InputSystem 驱动。
- P2：支持更细粒度的组合键、剪贴板、IME、手势轨迹回放。
