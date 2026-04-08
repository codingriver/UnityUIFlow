# UnityUIFlow 需求文档总览

版本：1.0.0  
日期：2026-04-08  
状态：定稿基线  
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

## 补充总览文档

| 文档名 | 范围 |
| --- | --- |
| `00-架构设计与技术约束.md` | 项目背景、目标、分层架构、技术栈、版本约束、非目标 |
| `00-开发路线图与交付物.md` | 里程碑、阶段目标、交付物清单、完成定义 |
| `00-API速查与最佳实践.md` | 选择器、动作、CLI 参数、最佳实践、已知限制、Playwright 对照 |

## 当前仓库已确认事实

| 项目项 | 当前值 | 来源 |
| --- | --- | --- |
| Unity Editor 版本 | `6000.6.0a2` | `ProjectSettings/ProjectVersion.txt` |
| `com.unity.test-framework` | `1.7.0` | `Packages/manifest.json` |
| `com.unity.inputsystem` | `1.19.0` | `Packages/manifest.json` |
| `com.unity.ui.test-framework` | 未安装 | `Packages/manifest.json`、`Packages/packages-lock.json` |
| `UnityUIFlow` 代码实现 | 未发现 | `Assets/`、`Packages/` 搜索结果 |

## 当前文档编写约定

1. 所有 `MXX` 文档均严格遵循 `docs/00-需求文档编写规范.md` 的固定 11 章结构。
2. 当前仓库不存在 `UnityUIFlow` 实现代码，因此模块中的类名、方法名、错误码若无现成事实来源，统一标注为“设计提案，实现时确认”。
3. 用户提供的目标版本、架构命名、动作名、CLI 参数作为本轮需求基线。
4. 与当前仓库现状不一致、或需要原型验证的内容，统一登记到 `TODO.md`。

## 阅读顺序

1. 先读 `TODO.md`，确认当前实现阻塞项。
2. 再读 `00-架构设计与技术约束.md`、`M03`、`M05`、`M06`，把控整体基线、主流程、动作能力与 Headed 调试能力。
3. 最后读 `M01`、`M02`、`M04`、`M07`、`M08`、`M09` 与三份 `00-*` 补充文档，完成编排、解析、定位、报告、集成和测试宿主设计。
