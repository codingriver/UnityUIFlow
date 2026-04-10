# UnityUIFlow 需求文档总览

版本：1.1.0  
日期：2026-04-10  
状态：更新基线  
适用对象：开发者、测试框架实现者、技术负责人

## 文档范围

本目录用于承接 `UnityUIFlow` 的首版需求设计文档。

当前文档集将用户给出的“UnityUIFlow 需求设计文档（最终版）”拆分为可分工、可实现、可验收的模块需求文档。

## 模块清单

| 模块 ID | 文档名 | 范围 |
| --- | --- | --- |
| M01 | `M01-用例编排与数据驱动-需求文档.md` | YAML 用例结构、fixture、数据驱动、变量替换 |
| M02 | `M02-YAML解析与执行计划-需求文档.md` | YAML 解析、AST、ExecutionPlan、选择器与时长字面量编译 |
| M03 | `M03-执行引擎与运行控制-需求文档.md` | `TestRunner`、`StepExecutor`、运行选项、失败策略、套件调度 |
| M04 | `M04-元素定位与等待-需求文档.md` | `ElementFinder`、选择器语法、等待策略、元素可见性判定 |
| M05 | `M05-动作系统与CSharp扩展-需求文档.md` | `ActionRegistry`、内置动作、自定义动作、Page Object 扩展 |
| M06 | `M06-Headed可视化执行-需求文档.md` | Editor Headed 面板、高亮、步进、失败暂停 |
| M07 | `M07-报告与截图-需求文档.md` | Markdown 报告、截图管理、产物目录、失败证据 |
| M08 | `M08-命令行与CI集成-需求文档.md` | CLI 参数、批处理运行、退出码、CI 产物约束 |
| M09 | `M09-测试基座与Fixture基类-需求文档.md` | `UnityUIFlowFixture<TWindow>`、生命周期、测试宿主、YAML 执行桥接 |
| M10 | `M10-测试用例说明与编写规范.md` | 当前测试覆盖、测试资源规范、用例编写与维护规则 |
| M11 | `M11-Examples验收测试与自动宿主-需求文档.md` | `Assets/Examples` 验收示例、自动宿主窗口、用例组织约定 |
| M12 | `M12-官方UI测试框架与输入系统测试接入-需求文档.md` | `com.unity.ui.test-framework`、InputSystem 测试输入、动作驱动与官方 fixture 接入 |

## 补充总览文档

| 文档名 | 范围 |
| --- | --- |
| `00-架构设计与技术约束.md` | 项目背景、目标、分层架构、技术栈、版本约束、非目标 |
| `00-开发路线图与交付物.md` | 里程碑、阶段目标、交付物清单、完成定义 |
| `00-API速查与最佳实践.md` | 选择器、动作、CLI 参数、最佳实践、已知限制、Playwright 对照 |

## 当前仓库已确认事实

| 项目项 | 当前值 | 来源 |
| --- | --- | --- |
| Unity Editor 版本 | `6000.6.0a2`（实现基准版本） | `ProjectSettings/ProjectVersion.txt` |
| `com.unity.test-framework` | `1.7.0`（含 UI 测试宿主与交互仿真子系统） | `Packages/manifest.json` |
| `com.unity.inputsystem` | `1.19.0` | `Packages/manifest.json` |
| `com.unity.editorcoroutines` | `1.1.0`（间接依赖，已安装） | `Packages/packages-lock.json` |
| `com.unity.ui.test-framework` | 已由 Unity 官方移除，功能并入 `com.unity.test-framework` | 无需额外安装 |
| `UnityUIFlow` 代码实现 | 已实现 Editor 侧 MVP，覆盖 Core / Parsing / Execution / Actions / Fixtures / Headed / Reporting / Cli | `Assets/UnityUIFlow/Editor/` |
| 当前交互模拟基线 | `Event.GetPooled` + `SendEvent` + 直接写入 `value` | `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs` |
| 当前 Fixture 基线 | `UnityUIFlowFixture<TWindow>` + `EditorWindow.GetWindow<TWindow>()` | `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.Fixtures.cs` |

## 当前文档编写约定

1. 所有 `MXX` 文档均严格遵循 `docs/00-需求文档编写规范.md` 的固定 11 章结构。
2. 当前仓库已经存在 `UnityUIFlow` Editor 侧实现代码；文档中的类名、方法名、字段名、错误码必须优先与 `Assets/UnityUIFlow/Editor/` 保持一致。
3. `com.unity.ui.test-framework` 已由 Unity 官方移除，其 UI 测试宿主与交互仿真能力已并入 `com.unity.test-framework@1.7.0`（已安装）。文档中所有 `com.unity.ui.test-framework` 引用均已更新为 `com.unity.test-framework`（UI 测试子系统）；当前 fallback 实现只作为过渡状态说明。
4. 新设计若暂无代码事实来源，统一标注为“设计提案，实现时确认”或 `TODO(待确认)`。
5. 与当前仓库现状不一致、或需要原型验证的内容，统一登记到 `TODO.md`。

## 阅读顺序

1. 先读 `TODO.md`，确认当前实现阻塞项。
2. 再读 `00-架构设计与技术约束.md`、`M03`、`M05`、`M09`、`M12`，把控整体基线、执行主流程、动作能力、Fixture 宿主和官方测试框架接入目标。
3. 最后读 `M01`、`M02`、`M04`、`M06`、`M07`、`M08`、`M10`、`M11` 与三份 `00-*` 补充文档，完成编排、解析、定位、Headed、报告、测试覆盖与 Examples 设计。
