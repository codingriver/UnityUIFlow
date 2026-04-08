## 1. 模块职责

- 负责：把 YAML 文本解析为 AST，并编译为运行时 `ExecutionPlan`。
- 负责：将时长字面量、选择器字符串、条件树、循环树转换为执行引擎可消费的数据结构。
- 不负责：实际执行动作、不负责元素查找、不负责生成 Editor 可视化面板。
- 输入/输出：输入为 YAML 文本和 `TestCaseDefinition`；输出为 `ExecutionPlan`、`ExecutableStep`、编译诊断。

## 2. 数据模型
| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| caseName | string | 必填 | 目标用例名称 | 非空字符串，长度范围 `[1, 120]` | 无 |
| steps | List<ExecutableStep> | 必填 | 扁平化后的执行步骤 | 列表长度范围 `[1, 5000]` | 无 |
| defaultTimeoutMs | int | 必填 | 编译后默认超时 | 范围 `[100, 600000]`，包含端点 | 无 |
| sourcePath | string | 必填 | 源文件路径 | 非空字符串 | 无 |
| diagnostics | List<CompileDiagnostic> | 可选 | 编译告警与错误 | 空列表表示无诊断 | 空列表 |
| stepId | string | 必填 | 步骤唯一 ID | 非空字符串；构造时初始化唯一值 | 无 |
| displayName | string | 必填 | 执行期展示名称 | 非空字符串，长度范围 `[1, 120]` | 无 |
| actionName | string | 必填 | 动作名 | 非空字符串，长度范围 `[1, 64]` | 无 |
| selector | SelectorExpression | 可选 | 已编译选择器 | `null` 表示动作不依赖元素 | `null` |
| parameters | Dictionary<string, string> | 可选 | 已归一化参数表 | 空字典表示无参数 | 空字典 |
| timeoutMs | int | 必填 | 步骤超时值 | 范围 `[100, 600000]`，包含端点 | 无 |
| continueOnFailure | bool | 必填 | 失败后是否继续 | `true` 或 `false` | `false` |
| kind | string | 必填 | 诊断类型 | 仅允许 `Error`、`Warning` | 无 |
| code | string | 必填 | 诊断码 | `UPPER_SNAKE_CASE` | 无 |
| message | string | 必填 | 诊断消息 | 非空字符串 | 无 |
| line | int | 可选 | 诊断起始行 | `null` 表示无行号；范围 `[1, 1000000]` | `null` |
| column | int | 可选 | 诊断起始列 | `null` 表示无列号；范围 `[1, 1000]` | `null` |

## 3. CRUD 操作
| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 解析 YAML 文本为 AST | `YamlTestCaseParser.Parse(string yamlText, string sourcePath)` | `yamlText` 为空；根节点不是对象 | `YamlTestCaseParser.Parse` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 编译 AST 为执行计划 | `ExecutionPlanBuilder.Build(TestCaseDefinition testCase, TestOptions options)` | `testCase.steps` 为空；存在未注册动作名 | `ExecutionPlanBuilder.Build` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 解析时长字面量 | 编译步骤参数时自动触发 | 字面量非 `ms` 或 `s` 结尾；数值超界 | `DurationParser.ParseToMilliseconds` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 编译选择器表达式 | 编译步骤时对 `selector` 预处理 | 选择器为空字符串；语法不闭合 | `SelectorCompiler.Compile` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 生成编译诊断 | 解析或编译任一步骤时 | 无；诊断生成不应被禁用 | `CompileDiagnosticFactory.Create` `(设计提案，实现时确认)` | 不涉及；只读操作 |

## 4. 交互规格

- 触发事件：执行引擎在读取完 YAML 原文后调用解析器。
- 状态变化：`RawYamlText -> AST -> ValidatedCaseDefinition -> ExecutionPlan`。
- 数据提交时机：所有步骤完成编译后一次性返回 `ExecutionPlan`；若存在 `Error` 级诊断，不返回可执行计划。
- 取消/回退：解析阶段出错时，不产出部分可执行步骤；调用方只接收完整成功结果或失败异常。
- 条件编译：`if.exists` 与 `repeat_while.condition` 保留为条件表达式对象，在执行期求值，不在编译期预判结果。
- 超时继承：步骤未声明 `timeout` 时，编译器直接写入用例级或 `TestOptions` 的最终毫秒值，执行期不再二次推导。

## 5. 视觉规格
不涉及

## 6. 校验规则
### 输入校验
| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| YAML 根节点必须是映射对象 | 解析开始后 | Error | `YAML 根节点必须是对象` |
| 步骤必须声明 `action` 或 `repeat_while` 之一 | 编译单步前 | Error | `步骤 {stepName} 缺少 action 或 repeat_while` |
| 动作名必须在动作注册表中可解析 | 编译单步时 | Error | `步骤 {stepName} 的动作 {action} 未注册` |
| 选择器字符串不能为空串 | 编译选择器前 | Error | `步骤 {stepName} 的 selector 不能为空` |
| 时长字面量只能使用 `ms` 或 `s` | 编译时长前 | Error | `步骤 {stepName} 的时长格式仅支持 ms 或 s` |
| 诊断行列号只在解析器可确认时写入 | 生成诊断对象时 | Info | `诊断 {code} 未提供源位置` |

### 错误响应
| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| YAML 语法错误 | YAML_PARSE_ERROR | `YAML 解析失败：{detail}` | 抛异常并终止当前用例 |
| 顶层字段类型错误 | YAML_FIELD_TYPE_INVALID | `字段 {field} 的类型非法` | 抛异常并终止当前用例 |
| 未注册动作 | ACTION_NOT_REGISTERED | `动作未注册：{action}` | 抛异常并终止当前用例 |
| 选择器语法错误 | SELECTOR_COMPILE_ERROR | `选择器编译失败：{selector}` | 抛异常并终止当前用例 |
| 时长字面量非法 | DURATION_LITERAL_INVALID | `时长字面量非法：{value}` | 抛异常并终止当前用例 |
| 编译后步骤为空 | EXECUTION_PLAN_EMPTY | `测试用例 {name} 编译后无可执行步骤` | 抛异常并终止当前用例 |

## 7. 跨模块联动
| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M01 用例编排与数据驱动 | 被动接收 | 接收 `TestCaseDefinition` 与模板渲染后的步骤结构 | `TestCaseDefinition`、`StepDefinition` `(设计提案)` |
| M03 执行引擎与运行控制 | 主动通知 | 向执行引擎返回完整 `ExecutionPlan` 或编译异常 | `ExecutionPlanBuilder.Build`、`TestRunner.RunTest` `(设计提案)` |
| M04 元素定位与等待 | 主动通知 | 把编译后的选择器表达式传给定位模块使用 | `SelectorCompiler.Compile`、`ElementFinder.Find` `(设计提案)` |
| M05 动作系统与CSharp扩展 | 被动接收 | 编译时向动作注册表确认动作存在和参数模型 | `ActionRegistry.HasAction` `(设计提案)` |

## 8. 技术实现要点

- 关键类与职责：
  - `YamlTestCaseParser` `(设计提案，实现时确认)`：负责 YAML 文本到 AST 的解析。
  - `ExecutionPlanBuilder` `(设计提案，实现时确认)`：负责将 AST 转换为扁平执行计划。
  - `SelectorCompiler` `(设计提案，实现时确认)`：负责把字符串选择器编译成分段表达式。
  - `DurationParser` `(设计提案，实现时确认)`：负责把 `5s`、`500ms` 转为毫秒整数。
- 核心流程：
```text
YamlTestCaseParser.Parse
-> TestCaseSchemaValidator.Validate
-> ActionRegistry.HasAction for each step
-> SelectorCompiler.Compile when selector exists
-> DurationParser.ParseToMilliseconds when timeout/duration exists
-> ExecutionPlanBuilder.Build flattened steps
```
- 性能约束：
  - 单个用例编译耗时目标不超过 `100ms`，基线为 `200` 步以内的 YAML。
  - 选择器编译结果必须缓存到 `ExecutionPlan`，禁止执行阶段重复解析字符串。
  - 禁止在编译阶段访问 `VisualElement` 实例或 Editor UI 状态。
  - 编译诊断列表按源文件顺序输出，禁止无序聚合导致日志不稳定。
- TODO(待确认)：YAML 解析器选型在 `YamlDotNet` 与 `VYaml` 之间尚未在仓库验证，需要原型比较解析速度、AOT 兼容性和错误定位能力。

## 9. 验收标准
1. [提供合法 YAML 文本] -> [调用 `YamlTestCaseParser.Parse`] -> [返回结构完整的 AST，且无 `Error` 级诊断]
2. [步骤声明 `timeout: 5s`] -> [调用执行计划编译] -> [对应 `ExecutableStep.timeoutMs` 等于 `5000`]
3. [步骤声明 `selector: "#panel .btn"`] -> [调用执行计划编译] -> [返回已编译选择器对象，不在执行阶段再解析原字符串]
4. [步骤声明未注册动作 `foo_bar`] -> [调用执行计划编译] -> [抛出 `ACTION_NOT_REGISTERED`]
5. [YAML 根节点为数组] -> [调用解析] -> [抛出 `YAML_PARSE_ERROR` 或 `YAML_FIELD_TYPE_INVALID`，不生成执行计划]

## 10. 边界规范

- 空数据：`diagnostics` 为空时必须返回空列表，不允许返回 `null`。
- 单元素：只有 1 个步骤的用例也必须生成合法 `ExecutionPlan`，且 `steps.Count` 为 `1`。
- 上下限临界值：`100ms` 与 `600000ms` 合法；`99ms` 与 `600001ms` 必须报错。
- 异常数据恢复：任何编译阶段错误都不得返回部分构建的 `ExecutionPlan`；调用方只能收到失败异常。

## 11. 周边可选功能

- P1：支持编译缓存，按 `sourcePath + fileHash` 复用 `ExecutionPlan`；当前预留 `CompileCacheKey` 扩展点。
- P1：支持选择器语法诊断建议；当前预留 `CompileDiagnostic.suggestion` 字段。
- P2：支持将编译结果序列化到磁盘缓存；当前不预留持久化文件格式。
