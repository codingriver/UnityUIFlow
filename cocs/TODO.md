### TODO-001：统一 Unity 编辑器基线版本

- **来源**：`cocs/00-Overview.md` / `ProjectSettings/ProjectVersion.txt`
- **问题**：需求基线写明“Unity 6.4 及以上”，但当前仓库实际版本为 `6000.6.0a2`。当前版本属于 Alpha 分支，是否作为首版实现基线未确认。
- **当前处理**：需求文档按“目标产品基线”继续写为 `Unity 6.4+`，实现阶段暂不默认沿用当前仓库版本。
- **需要决策**：是否要求在实现前将本仓库统一到 `Unity 6.4` 正式版或明确的稳定版本？
- **优先级**：P0

### TODO-002：统一 UI 测试依赖包版本

- **来源**：`cocs/00-Overview.md` / `Packages/manifest.json` / `Packages/packages-lock.json`
- **问题**：需求基线声明依赖 `com.unity.ui.test-framework@1.0.0`、`com.unity.inputsystem@1.7.0`、`com.unity.test-framework@1.3.0`，但当前仓库实际为 `com.unity.inputsystem@1.19.0`、`com.unity.test-framework@1.7.0`，且未安装 `com.unity.ui.test-framework`。
- **当前处理**：文档按目标依赖继续描述能力边界；实现阶段默认先补齐 `com.unity.ui.test-framework`，并在正式开发前统一版本。
- **需要决策**：是否以需求文档中的目标依赖版本为准，先升级/降级当前仓库依赖后再开发？
- **优先级**：P0

### TODO-003：确认 YAML 解析库选型

- **来源**：`cocs/M02-YAML解析与执行计划-需求文档.md §8`
- **问题**：当前仅确认需要支持手写 YAML 用例，但 `YamlDotNet` 与 `VYaml` 的错误定位能力、性能、AOT 兼容性尚未在仓库原型中验证。
- **当前处理**：文档保持两者皆可的选型状态，不把具体库名写成已确定实现。
- **需要决策**：首版实现是否确定使用 `YamlDotNet` 作为唯一 YAML 解析库？
- **优先级**：P1

### TODO-004：确认 EditMode 协程调度与等待策略

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md §8`
- **问题**：需求文档假设 V1 运行于 EditMode，且动作与等待步骤以协程方式推进，但当前仓库中没有 `UnityUIFlow` 原型验证 EditMode 下暂停、恢复、等待的精确行为。
- **当前处理**：文档保守要求“每帧轮询一次，禁止 busy wait”，不写更细的调度实现。
- **需要决策**：首版执行引擎是否以 EditMode 协程驱动为唯一实现路径？
- **优先级**：P1

### TODO-005：确认 PanelSimulator 与输入能力边界

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md §8`
- **问题**：文档基于用户提供内容假设 `PanelSimulator` 可支撑点击、双击、输入、拖拽、滚轮、悬停等动作，但当前仓库未安装相关包，也未验证各 API 在目标 Unity 版本下的可用性与方法签名。
- **当前处理**：所有动作 API 均以“设计提案，实现时确认”标注，错误码和动作名仍按需求基线固定。
- **需要决策**：是否确认首版内置动作全集严格以 `PanelSimulator` 已验证的 API 为准？
- **优先级**：P1

### TODO-006：确认 Headed 高亮叠加实现方式

- **来源**：`cocs/M06-Headed可视化执行-需求文档.md §8`
- **问题**：需求文档示例使用 `GUIClip.Unclip` 做坐标转换，但当前仓库未验证该 API 在目标 Unity 版本下的可访问性、替代方案和绘制稳定性。
- **当前处理**：文档只固定“高亮效果”和“失败暂停行为”，不固定最终绘制 API。
- **需要决策**：Headed 高亮是否确认采用 IMGUI 叠加绘制，而不是修改被测窗口样式或使用其他绘制层？
- **优先级**：P1

### TODO-007：确认异步截图完成判定机制

- **来源**：`cocs/M07-报告与截图-需求文档.md §8`
- **问题**：需求要求截图异步保存并回传文件路径，但当前仓库未验证 Editor 模式下截图落盘完成的可靠判定方式。
- **当前处理**：文档只固定对外接口 `CaptureAsync` 和“不得阻塞主流程超过 1 帧”的约束，不固定内部完成判定实现。
- **需要决策**：异步截图完成是否允许通过轮询目标文件存在与稳定文件长度来判定？
- **优先级**：P1

### TODO-008：确认 CLI 参数优先级与 Unity 原生参数协作规则

- **来源**：`cocs/M08-命令行与CI集成-需求文档.md §8`
- **问题**：文档定义了 `unityUIFlow.*` 参数，但尚未验证它与 Unity 原生 `-testFilter`、`-runTests`、`-batchmode` 的组合顺序和优先级。
- **当前处理**：文档暂定“显式 `unityUIFlow.*` 优先于默认值，不覆盖 Unity 原生参数本身”。
- **需要决策**：当 `-testFilter` 与 `-unityUIFlow.testFilter` 同时存在时，是否只保留二者交集？
- **优先级**：P1

### TODO-009：确认 Fixture 基座与官方测试宿主的生命周期兼容性

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md §8`
- **问题**：当前仓库还未验证 `EditorWindowUITestFixture<TWindow>` 在目标 Unity 版本和目标 UI Test Framework 版本下的窗口创建、销毁、SetUp/TearDown 钩子是否与文档设想完全一致。
- **当前处理**：文档把 `UnityUIFlowFixture<TWindow>` 作为设计提案保留，并要求实现前先做 PoC 验证。
- **需要决策**：是否确认首版测试基座必须继承 `EditorWindowUITestFixture<TWindow>`，而不是自建窗口宿主？
- **优先级**：P1
