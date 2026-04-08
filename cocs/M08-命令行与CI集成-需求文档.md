## 1. 模块职责

- 负责：解析 `Unity.exe` 运行参数中的 `unityUIFlow.*` 扩展参数，构造 `TestOptions`，返回标准退出码，并定义 CI 产物约束。
- 负责：无头模式开关、测试过滤、产物目录、失败截图开关与日志输出。
- 不负责：测试执行细节、不负责具体报告内容、不负责 Headed 面板实现。
- 输入/输出：输入为命令行参数、运行环境变量、测试结果；输出为 `TestOptions`、过滤结果、进程退出码、CI 产物路径。

## 2. 数据模型
| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| testFilter | string | 可选 | YAML 用例过滤表达式 | `null` 表示不过滤；长度范围 `[1, 256]` | `null` |
| headed | bool | 必填 | 是否启用 Headed 模式 | `true` 或 `false` | `true` |
| reportPath | string | 必填 | 报告输出目录 | 非空字符串 | `"Reports"` |
| screenshotOnFailure | bool | 必填 | 失败时是否自动截图 | `true` 或 `false` | `true` |
| screenshotPath | string | 可选 | 截图输出目录 | `null` 表示使用 `reportPath/Screenshots` | `null` |
| exitCode | int | 必填 | 进程退出码 | 仅允许 `0`、`1`、`2` | 无 |
| batchmode | bool | 必填 | 是否运行在批处理模式 | `true` 或 `false` | `false` |
| nographics | bool | 必填 | 是否启用无图形模式 | `true` 或 `false` | `false` |
| parsedAtUtc | string | 必填 | 参数解析时间 | ISO 8601 UTC 字符串 | 无 |

## 3. CRUD 操作
| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 解析命令行参数 | Unity 启动后初始化测试环境 | 参数格式非法；键重复冲突 | `CommandLineOptionsParser.Parse` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 构造 `TestOptions` | 参数解析成功后 | 必需目录为空；Headed 与批处理模式冲突未处理 | `CommandLineOptionsParser.ToTestOptions` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 过滤用例列表 | 套件扫描后 | `testFilter` 非法 | `YamlTestCaseFilter.Match` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 计算退出码 | 套件结束后 | 测试结果为空 | `ExitCodeResolver.Resolve` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 输出 CI 产物清单 | 套件结束后 | 报告目录不存在 | `CiArtifactManifestWriter.Write` `(设计提案，实现时确认)` | 不涉及；覆盖旧文件 |

## 4. 交互规格

- 触发事件：Unity Test Runner 启动流程中读取进程参数。
- 状态变化：`Read command line -> Parse options -> Build TestOptions -> Execute suite -> Resolve exit code -> Emit artifacts`。
- 数据提交时机：参数解析完成后一次性构造 `TestOptions`；套件结束后一次性计算退出码。
- 取消/回退：参数解析失败时直接返回退出码 `2`，不进入测试执行。
- Headed 开关：显式 `-unityUIFlow.headed false` 时，必须关闭 Headed 面板和元素高亮，即使本地 Editor 环境可用。
- 过滤行为：`testFilter` 作用于 YAML 文件名和用例 `name`，采用大小写不敏感的通配匹配。

## 5. 视觉规格
不涉及

## 6. 校验规则
### 输入校验
| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `headed=false` 是批处理环境默认值 | 解析运行环境后 | Info | `批处理环境默认关闭 Headed 模式` |
| `reportPath` 必须为非空路径 | 构造 `TestOptions` 前 | Error | `报告输出目录不能为空` |
| `testFilter` 长度上限为 256 | 解析过滤器时 | Error | `测试过滤表达式过长` |
| `screenshotPath` 为空时自动派生到 `reportPath/Screenshots` | 构造 `TestOptions` 时 | Info | `截图目录未指定，已使用默认目录` |
| 参数布尔值只允许 `true` 或 `false` | 参数解析时 | Error | `参数 {name} 的布尔值非法：{value}` |

### 错误响应
| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 参数格式非法 | CLI_ARGUMENT_INVALID | `命令行参数非法：{detail}` | 返回退出码 `2`，终止执行 |
| 报告目录不可写 | CLI_REPORT_PATH_INVALID | `报告目录不可写：{path}` | 返回退出码 `2`，终止执行 |
| 过滤表达式非法 | CLI_FILTER_INVALID | `测试过滤表达式非法：{filter}` | 返回退出码 `2`，终止执行 |
| 有测试失败 | CLI_TESTS_FAILED | `测试存在失败：{failedCount}` | 返回退出码 `1` |
| YAML 解析或执行异常 | CLI_EXECUTION_ERROR | `测试执行异常：{detail}` | 返回退出码 `2` |

## 7. 跨模块联动
| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 主动通知 | 构造 `TestOptions` 并调用 `RunTest/RunSuite` | `CommandLineOptionsParser.ToTestOptions`、`TestRunner.RunSuite` `(设计提案)` |
| M06 Headed可视化执行 | 主动通知 | 当 `headed=false` 时明确关闭面板与事件订阅 | `TestOptions.Headed` `(设计提案)` |
| M07 报告与截图 | 主动通知 | 提供报告与截图目录，用于 CI 收集产物 | `TestOptions.ReportOutputPath`、`TestOptions.ScreenshotPath` `(设计提案)` |
| CI 系统 | 主动通知 | 输出标准退出码与产物目录约定，供 GitHub Actions / GitLab CI 上传 | `ExitCodeResolver.Resolve`、`CiArtifactManifestWriter.Write` `(设计提案)` |

## 8. 技术实现要点

- 关键类与职责：
  - `CommandLineOptionsParser` `(设计提案，实现时确认)`：负责读取并解析 `unityUIFlow.*` 参数。
  - `YamlTestCaseFilter` `(设计提案，实现时确认)`：负责按文件名或用例名过滤执行目标。
  - `ExitCodeResolver` `(设计提案，实现时确认)`：负责把测试汇总映射为 `0/1/2`。
  - `CiArtifactManifestWriter` `(设计提案，实现时确认)`：输出产物索引文本或 JSON。
- 核心流程：
```text
Read Environment.GetCommandLineArgs()
-> Parse unityUIFlow.* options
-> Validate option values
-> Build TestOptions
-> Run suite/test
-> Resolve exit code
-> Write artifact manifest
```
- 性能约束：
  - 参数解析必须在 `10ms` 内完成。
  - 过滤仅对候选用例列表做线性扫描，禁止重复读文件内容做过滤。
  - 退出码计算只读套件结果对象，不重复统计步骤级信息。
  - 产物清单只写 1 次，禁止每个用例都重写。
- 禁止项：
  - 禁止在批处理环境强制打开 Headed 面板。
  - 禁止把 CI 是否成功的判断散落在多个模块；必须统一由 `ExitCodeResolver` 计算。
- TODO(待确认)：`unityUIFlow.*` 参数与 Unity 原生 `-testFilter` 的组合顺序、冲突优先级需在真实命令行原型中验证。

## 9. 验收标准
1. [命令行包含 `-unityUIFlow.headed false -unityUIFlow.reportPath ./Reports`] -> [启动测试] -> [生成 `TestOptions.Headed=false` 且报告目录为 `./Reports`]
2. [套件全部通过] -> [执行完成] -> [进程退出码为 `0`]
3. [至少 1 个测试失败] -> [执行完成] -> [进程退出码为 `1`]
4. [YAML 解析失败] -> [执行完成] -> [进程退出码为 `2`]
5. [命令行包含 `-unityUIFlow.testFilter Login*`] -> [执行套件] -> [仅执行匹配文件名或用例名的用例]

## 10. 边界规范

- 空数据：未传任何 `unityUIFlow.*` 参数时，按默认值构造 `TestOptions`。
- 单元素：只有 1 个 YAML 文件时，过滤匹配成功后仍按套件流程生成汇总结果。
- 上下限临界值：`testFilter` 长度 `256` 合法，`257` 报错。
- 异常数据恢复：参数解析失败时立即返回退出码 `2`，不尝试回退到部分默认值混合运行。

## 11. 周边可选功能

- P1：支持从环境变量读取默认输出目录；当前预留 `EnvironmentOptionSource` 扩展点。
- P1：支持 JSON 格式产物清单；当前预留 `CiArtifactManifestWriter.WriteJson` 扩展方法。
- P2：支持 JUnit XML 适配输出；当前不预留 V1 文件结构。
