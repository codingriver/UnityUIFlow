# UnityUIFlow 需求文档总览

版本：1.4.0
日期：2026-04-13
状态：以当前代码实现为准更新
适用对象：开发者、测试框架实现者、技术负责人

## 文档范围

本目录用于承接 `UnityUIFlow` 的需求、实现边界、测试覆盖与维护约定。

当前文档集已经从“纯设计稿”收口为“代码优先的实现文档集”：
- 已落地能力按 `Assets/UnityUIFlow/Editor/` 与 `Assets/Examples/Tests/` 的真实实现描述。
- 尚未落地或仍受 Unity / 包边界限制的内容，必须在文档中显式标注为边界，不再写成“默认可用”。

## 模块清单

| 模块 ID | 文档名 | 范围 |
| --- | --- | --- |
| M01 | `M01-用例编排与数据驱动-需求文档.md` | YAML 用例结构、fixture、数据驱动、变量替换 |
| M02 | `M02-YAML解析与执行计划-需求文档.md` | YAML 解析、AST、ExecutionPlan、选择器与时长字面量编译 |
| M03 | `M03-执行引擎与运行控制-需求文档.md` | `TestRunner`、`StepExecutor`、运行选项、失败策略、套件调度 |
| M04 | `M04-元素定位与等待-需求文档.md` | `ElementFinder`、选择器语法、等待策略、元素可见性判定 |
| M05 | `M05-动作系统与CSharp扩展-需求文档.md` | `ActionRegistry`、38 个内置动作、自定义动作、Page Object 扩展 |
| M06 | `M06-Headed可视化执行-需求文档.md` | Editor Headed 面板、高亮、步进、失败暂停 |
| M07 | `M07-报告与截图-需求文档.md` | Markdown 报告、截图管理、产物目录、失败证据 |
| M08 | `M08-命令行与CI集成-需求文档.md` | CLI 参数、批处理运行、退出码、CI 产物约束 |
| M09 | `M09-测试基座与Fixture基类-需求文档.md` | `UnityUIFlowFixture<TWindow>`、生命周期、测试宿主、YAML 执行桥接 |
| M10 | `M10-测试用例说明与编写规范.md` | 当前测试覆盖、测试资源规范、用例编写与维护规则 |
| M11 | `M11-Examples验收测试与自动宿主-需求文档.md` | `Assets/Examples` 验收示例、自动宿主窗口、用例组织约定 |
| M12 | `M12-官方UI测试框架与输入系统测试接入-需求文档.md` | `com.unity.ui.test-framework`、`com.unity.inputsystem`、strict 边界、官方宿主与驱动接入 |
| M13 | `M13-真实截图与官方菜单模拟及调试增强-需求文档.md` | 真实截图、菜单模拟、`DebugOnFailure`、修饰键与调试增强 |

## 补充总览文档

| 文档名 | 范围 |
| --- | --- |
| `00-架构设计与技术约束.md` | 项目背景、目标、分层架构、技术栈、版本约束、非目标 |
| `00-开发路线图与交付物.md` | 当前交付阶段、已落地能力、剩余边界与后续路线 |
| `00-API速查与最佳实践.md` | 选择器、动作、CLI 参数、最佳实践、已知限制、Playwright 对照 |
| `00-UIToolkit控件自动化覆盖与限制说明.md` | UIToolkit 控件覆盖矩阵、局部支持与不可自动化边界 |

## 当前仓库已确认事实

| 项目项 | 当前值 | 来源 |
| --- | --- | --- |
| Unity Editor 版本 | `6000.6.0a2` | `ProjectSettings/ProjectVersion.txt` |
| `com.unity.test-framework` | `1.7.0` | `Packages/manifest.json` |
| `com.unity.ui.test-framework` | `6.3.0` | `Packages/manifest.json` |
| `com.unity.inputsystem` | `1.19.0` | `Packages/manifest.json` |
| `UnityUIFlow` 实现基线 | Editor 侧 MVP 已落地，覆盖 Core / Parsing / Execution / Actions / Fixtures / Headed / Reporting / CLI | `Assets/UnityUIFlow/Editor/` |
| 当前指针/键盘基线 | `PanelSimulator` 官方链路优先，InputSystem 键盘桥接补充，非官方入口保留 fallback | `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs` / `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs` |
| 当前官方宿主基线 | `UnityUIFlowFixture<TWindow>` + `OfficialEditorWindowPanelSimulator` 桥接 | `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.Fixtures.cs` / `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs` |
| 当前动作总数 | 38 个内置动作 | `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs` |
| 当前复杂字段支持 | `ObjectField` 资源路径 / `guid:`，`CurveField` 键帧 DSL，`GradientField` 渐变 DSL | `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.AdvancedActions.cs` |
| 当前扩展控件回归 | `PopupField<string>`、`TagField`、`LayerField`、Toolbar 系列、`PropertyField`、`InspectorElement`、`MultiColumn*View` 排序/列宽 | `Assets/Examples/Tests/UnityUIFlow.LocatorsAndActionsTests.cs` / `Assets/Examples/Tests/UnityUIFlow.AdvancedControlsWindow.cs` |

## 当前实现边界

以下边界在代码和文档中都必须保持一致，不得写成“已全面支持”：

1. `ObjectField` 的 Object Picker 与 DragAndDrop 仍未自动化。
2. `CurveField` / `GradientField` 的独立编辑器浮窗仍未自动化。
3. `ToolbarPopupSearchField` 仅覆盖输入本体，不覆盖弹出结果列表。
4. `ToolbarBreadcrumbs` 仅覆盖已生成且可定位子项点击，不提供统一“按 label / index 导航”动作。
5. `PropertyField` / `InspectorElement` 通过动态生成的后代控件复用现有动作，仍不是“对自身统一赋值”。
6. `IMGUIContainer`、IME、系统剪贴板、多窗口协同、像素级视觉 diff 仍不在 V1 自动化边界内。

## 当前文档编写约定

1. 所有 `MXX` 文档遵循 `docs/00-需求文档编写规范.md` 的固定结构。
2. 文档中的类名、方法名、字段名、错误码必须优先与 `Assets/UnityUIFlow/Editor/` 保持一致。
3. 与当前实现不一致时，以代码为准修正文档，而不是反向把代码描述成“待实现”。
4. 尚未验证或仍需未来版本/环境支持的内容，统一写成“边界”“可选增强”或 `TODO(待确认)`。
5. 所有已知不一致、后续决策项、未闭合风险统一登记到 `TODO.md`。

## 推荐阅读顺序

1. `00-架构设计与技术约束.md`
2. `M03-执行引擎与运行控制-需求文档.md`
3. `M05-动作系统与CSharp扩展-需求文档.md`
4. `M09-测试基座与Fixture基类-需求文档.md`
5. `M12-官方UI测试框架与输入系统测试接入-需求文档.md`
6. `M13-真实截图与官方菜单模拟及调试增强-需求文档.md`
7. `00-UIToolkit控件自动化覆盖与限制说明.md`
8. `M10-测试用例说明与编写规范.md`

## 2026-04-13 本轮修订摘要

- 已补入 `M13` 到总览模块清单。
- 已把动作系统基线更新为“38 个内置动作”。
- 已把当前复杂字段、Toolbar、`PropertyField`、`InspectorElement`、`MultiColumn*View` 能力纳入实现事实。
- 已把“官方宿主 + `PanelSimulator` + InputSystem 桥接 + fallback”的真实执行口径写入总览。
- 已显式列出当前仍然存在的自动化边界，防止文档夸大实现范围。
