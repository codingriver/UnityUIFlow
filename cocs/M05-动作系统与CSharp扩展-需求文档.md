# M05 动作系统与CSharp扩展 需求文档

版本：1.1.0
日期：2026-04-08
状态：补充版

---

## 1. 模块职责

- 负责：注册内置动作、自定义动作、动作参数映射、动作执行上下文与扩展发现机制。
- 负责：提供 `IAction` 契约与 `ActionName` 标记能力，支持 Page Object 场景复用。
- 不负责：YAML 解析、不负责元素定位规则细节、不负责生成最终测试报告。
- 输入/输出：输入为动作名、参数字典、`ActionContext`、根 `VisualElement`；输出为动作执行结果、失败异常、扩展注册表状态。

---

## 2. 数据模型

### ActionRegistryEntry（注册表条目）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| actionName | string | 必填 | 动作注册名 | 非空字符串，长度范围 `[1, 64]` | 无 |
| actionType | Type | 必填 | 动作实现类型 | 必须实现 `IAction` | 无 |
| isBuiltIn | bool | 必填 | 是否内置动作 | `true` 或 `false` | `false` |

### ActionContext（动作执行上下文）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| Root | VisualElement | 必填 | 当前根元素 | 非空 | 无 |
| Finder | ElementFinder | 必填 | 元素查找器 | 非空 | 无 |
| Options | TestOptions | 必填 | 当前运行选项 | 非空 | 无 |
| Reporter | IExecutionReporter | 可选 | 执行期事件写入器 | `null` 表示仅内存收集 | `null` |
| Simulator | object | 可选 | 官方 UI 测试模拟器封装 | `null` 表示当前动作不能使用底层模拟器 | `null` |
| CurrentStepId | string | 必填 | 当前步骤 ID | 非空字符串 | 无 |
| SharedBag | Dictionary\<string, object\> | 可选 | 用例内共享对象 | 空字典表示无共享数据 | 空字典 |
| CancellationToken | CancellationToken | 必填 | 运行取消令牌 | 运行停止时置为 Cancelled | 无 |

---

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 注册内置动作 | 引擎初始化时自动触发 | 动作名重复；实现类型未实现 `IAction` | `ActionRegistry.RegisterBuiltIns` `(设计提案，实现时确认)` | 不涉及；注册表构建完成后只读 |
| 注册自定义动作 | 扫描程序集或显式调用注册 API | 动作名重复；缺少 `ActionName`；构造失败 | `ActionRegistry.RegisterCustom` `(设计提案，实现时确认)` | 不涉及；注册表级别不支持撤销 |
| 解析动作实例 | 步骤执行前 | 动作未注册；实例创建失败 | `ActionRegistry.Resolve` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 执行动作 | `StepExecutor` 进入单步执行 | 当前步骤已终止；动作解析失败 | `IAction.Execute` | 不涉及；动作对 UI 的影响由目标系统承担，不做通用 Undo |
| 执行 Page Object 流程 | C# 测试代码直接调用 | `PageObject` 构造参数为空 | `LoginPage.Login` 等扩展方法 `(设计提案，实现时确认)` | 不涉及；由测试作者自行控制 |

---

## 4. 交互规格

- 触发事件：执行引擎根据步骤的 `actionName` 调用动作注册表解析动作。
- 状态变化：`Lookup action -> Create action instance -> Bind ActionContext -> Execute -> Return success/failure`。
- 数据提交时机：动作参数在执行前一次性绑定；执行结果在返回或抛异常时提交给 `StepResult`。
- 取消/回退：运行中止时，动作实现应在每次等待点检查 `CancellationToken.IsCancellationRequested`，发现取消后立即抛出 `OperationCanceledException`；动作本身不提供通用回滚。
- 自定义动作发现：V1 支持特性扫描与显式注册两种方式；发生名称冲突时，显式注册优先，且必须报 `ACTION_NAME_CONFLICT`。
- 参数映射：所有 YAML 字段统一映射到 `Dictionary<string, string>`；动作实现者自行读取并做类型转换。

---

## 5. 视觉规格

不涉及

---

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| 动作实现类型必须实现 `IAction` | 注册动作时 | Error | `动作 {actionName} 未实现 IAction` |
| 动作名必须唯一 | 注册动作时 | Error | `动作名冲突：{actionName}` |
| 内置动作必填参数必须存在 | 执行动作前 | Error | `步骤 {stepName} 缺少参数 {parameter}` |
| `selector` 依赖型动作必须先成功定位元素 | 动作执行前 | Error | `步骤 {stepName} 未找到目标元素` |
| `type_text_fast` 仅允许目标元素为 `TextField` 或兼容输入控件 | 动作执行前 | Error | `步骤 {stepName} 的目标元素不支持快速输入` |
| 自定义动作构造失败必须透出原始异常消息 | 创建实例时 | Warning | `动作 {actionName} 初始化失败：{detail}` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 动作名重复注册 | ACTION_NAME_CONFLICT | `动作名冲突：{actionName}` | 抛异常并阻断初始化 |
| 动作未注册 | ACTION_NOT_FOUND | `未找到动作：{actionName}` | 抛异常并终止当前步骤 |
| 动作参数缺失 | ACTION_PARAMETER_MISSING | `动作 {actionName} 缺少参数 {parameter}` | 抛异常并终止当前步骤 |
| 动作参数类型错误 | ACTION_PARAMETER_INVALID | `动作 {actionName} 的参数 {parameter} 非法` | 抛异常并终止当前步骤 |
| 动作执行异常 | ACTION_EXECUTION_FAILED | `动作 {actionName} 执行失败：{detail}` | 抛异常并标记当前步骤失败 |
| 目标元素类型不兼容 | ACTION_TARGET_TYPE_INVALID | `动作 {actionName} 的目标类型不兼容：{targetType}` | 抛异常并终止当前步骤 |

---

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 被动接收 | 接收步骤执行请求并返回动作结果 | `StepExecutor.ExecuteStep`、`ActionRegistry.Resolve` `(设计提案)` |
| M04 元素定位与等待 | 被动接收 | 通过 `ActionContext.Finder` 查找动作目标元素 | `ElementFinder.Find`、`ElementFinder.WaitForElement` `(设计提案)` |
| M06 Headed可视化执行 | 主动通知 | 动作开始前发布当前动作名与当前目标元素用于高亮 | `HeadedRunEventBus.PublishCurrentAction` `(设计提案)` |
| M07 报告与截图 | 主动通知 | 记录动作执行耗时、异常、截图请求与自定义日志 | `IExecutionReporter.RecordAction` `(设计提案)` |

---

## 8. 技术实现要点

- 关键类与职责：
  - `IAction` `(设计提案，实现时确认)`：统一动作执行契约，方法签名为 `void Execute(VisualElement root, ActionContext context, Dictionary<string, string> parameters)`，不返回值，异常即失败。
  - `ActionNameAttribute` `(设计提案，实现时确认)`：为自定义动作声明 YAML 动作名，用法：`[ActionName("custom_login")]`。
  - `ActionRegistry` `(设计提案，实现时确认)`：管理动作注册、解析与实例创建；初始化时注册全部内置动作，之后扫描自定义动作。
  - `ActionContext` `(设计提案，实现时确认)`：向动作提供 Finder、Options、Reporter、Simulator、SharedBag、CancellationToken。

- 内置动作参数规格：

| 动作名 | 必填参数 | 可选参数 | 说明 |
| --- | --- | --- | --- |
| `click` | `selector: string` | — | 单击元素；`selector` 通过 `Finder` 定位 |
| `double_click` | `selector: string` | — | 双击元素 |
| `type_text` | `selector: string`, `value: string` | — | 逐字符键盘模拟输入 |
| `type_text_fast` | `selector: string`, `value: string` | — | 直接写入文本值，目标必须是 `TextField` 或兼容控件 |
| `press_key` | `key: string` | — | 发送键盘按键；`key` 格式为 Unity `KeyCode` 名称，如 `Return`、`Tab` |
| `drag` | `from: string`, `to: string` | `duration: string` | `from`/`to` 均为选择器或 `x,y` 坐标字符串；`duration` 为拖拽时长字面量，默认 `100ms` |
| `scroll` | `selector: string`, `delta: string` | — | `delta` 格式为 `dx,dy`，如 `0,-100` |
| `hover` | `selector: string` | `duration: string` | 悬停到目标元素；`duration` 为悬停时长，默认 `0ms` |
| `wait` | `duration: string` | — | 固定等待，`duration` 为时长字面量，如 `1s`、`500ms` |
| `wait_for_element` | `selector: string` | `timeout: string` | 等待目标元素出现；`timeout` 未声明时沿用步骤默认超时 |
| `assert_visible` | `selector: string` | `timeout: string` | 断言元素可见；可等待到可见 |
| `assert_not_visible` | `selector: string` | `timeout: string` | 断言元素不可见或不存在 |
| `assert_text` | `selector: string`, `expected: string` | — | 断言元素文本完全等于 `expected` |
| `assert_text_contains` | `selector: string`, `expected: string` | — | 断言元素文本包含 `expected` 片段 |
| `assert_property` | `selector: string`, `property: string`, `expected: string` | — | 断言指定属性值；`property` 为 UIToolkit 样式/绑定属性名 |
| `screenshot` | `name: string` | — | 生成截图附件；`name` 作为截图文件 tag，长度范围 `[1, 64]` |

- 核心流程：

```text
Initialize ActionRegistry
-> Register built-in actions (16 built-in actions)
-> Scan custom actions by [ActionName] attribute in whitelist assemblies
-> On step execution:
   -> ActionRegistry.Resolve(actionName)
   -> Build ActionContext (Root, Finder, Options, Reporter, Simulator, SharedBag, CancellationToken)
   -> action.Execute(root, context, parameters)
   -> Convert result/exception to StepResult
```

- 性能约束：
  - 注册表初始化只执行一次，禁止每步重新扫描程序集。
  - 动作实例默认按步骤创建，禁止跨用例复用带状态实例。
  - 内置动作参数读取必须使用字典访问，禁止反射逐字段绑定。
  - 自定义动作扫描范围固定为配置程序集白名单，避免全域扫描。

- 禁止项：
  - 禁止在 `IAction.Execute` 中直接写 Markdown 报告文件。
  - 禁止自定义动作绕过 `ActionContext` 直接访问全局单例状态。
  - 禁止动作实现忽略 `CancellationToken`；必须在每个主要等待点检查取消状态。

- TODO(待确认)：`PanelSimulator` 在目标包版本下支撑的动作集合、方法签名与键盘输入限制需原型验证（见 `TODO-005`）。

---

## 9. 验收标准

1. [注册表完成初始化] -> [执行 `click` 动作] -> [能解析到内置动作实例并完成执行]
2. [存在带 `[ActionName("custom_login")]` 的自定义动作] -> [启动动作扫描] -> [注册表中可通过 `custom_login` 解析到该动作]
3. [步骤缺少 `selector` 参数但动作依赖元素] -> [执行动作] -> [返回 `ACTION_PARAMETER_MISSING`]
4. [自定义动作在 `Execute` 中抛异常] -> [执行步骤] -> [步骤失败并记录 `ACTION_EXECUTION_FAILED`]
5. [C# 测试直接构造 `LoginPage`] -> [调用 `Login(username, password)`] -> [Page Object 内部动作按既定顺序执行]
6. [运行停止，动作正在等待] -> [动作检查 `CancellationToken`] -> [立即抛出 `OperationCanceledException`，步骤结束]

---

## 10. 边界规范

- 空数据：不需要参数的动作（无此类内置动作，但自定义动作可能出现）在 `parameterMap` 为空字典时仍必须能执行。
- 单元素：对单个目标元素的 `click`、`hover`、`assert_visible` 必须在找到第一个元素后立即执行，不再继续查找其他候选。
- 上下限临界值：动作名长度 `1` 与 `64` 合法；超界注册时报 `ACTION_NAME_CONFLICT`。
- 异常数据恢复：动作执行异常后不得污染注册表；后续步骤仍可解析其他动作实例。

---

## 11. 周边可选功能

- P1：支持动作级参数 schema 声明；当前预留 `IActionDescriptor` 接口（`string ActionName`、`ActionParameterSchema[] Parameters` 属性），不进入 V1 主流程。
- P1：支持 DI 容器创建自定义动作；当前预留 `IActionFactory` 扩展点，默认实现使用 `Activator.CreateInstance`。
- P2：支持动作市场与包发现；当前不预留远程加载能力。
