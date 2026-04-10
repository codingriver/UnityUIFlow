# UnityUIFlow TODO

## 待决策项

### TODO-003：确认 YAML 解析库选型

- **来源**：`cocs/M02-YAML解析与执行计划-需求文档.md §8`
- **问题**：当前仅确认需要支持手写 YAML 用例，但 `YamlDotNet` 与 `VYaml` 的错误定位能力、性能、AOT 兼容性尚未在仓库原型中验证。
- **当前处理**：文档推荐使用 `YamlDotNet`（Unity AOT 兼容性更成熟），但未经实际验证。
- **需要决策**：是否确认以 `YamlDotNet` 作为首版唯一 YAML 解析库？
- **优先级**：P1

### TODO-004：确认 EditMode 执行驱动方式

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md §8`
- **问题**：需求文档新增推荐方案"使用 `EditorApplication.update` 注册每帧回调替代协程"，原方案为 `EditorCoroutineUtility.StartCoroutineOwnerless`（`com.unity.editorcoroutines` 已间接安装）。两种方案的暂停/恢复/超时行为需原型验证对比。
- **当前处理**：文档推荐 `EditorApplication.update` 状态机方案，但标记为待验证。
- **需要决策**：是否确认首版执行引擎使用 `EditorApplication.update` 状态机驱动，而不是 `EditorCoroutineUtility`？
- **优先级**：P1

### TODO-005：确认 PanelSimulator 与输入能力边界

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md §8`
- **问题**：文档假设 `PanelSimulator`（现属于 `com.unity.test-framework` UI 测试子系统）可支撑点击、双击、输入、拖拽、滚轮、悬停等动作，但 `com.unity.test-framework@1.7.0` 下的具体 API 可用性与方法签名尚未原型验证。
- **当前处理**：所有动作 API 均以"设计提案，实现时确认"标注，错误码和动作名仍按需求基线固定。
- **需要决策**：是否确认首版内置动作全集严格以 `com.unity.test-framework` UI 测试子系统已验证的 API 为准？
- **优先级**：P1

### TODO-006：确认 Headed 高亮叠加实现方式

- **来源**：`cocs/M06-Headed可视化执行-需求文档.md §8`
- **问题**：推荐方案"VisualElement 透明覆盖层 + `element.worldBound` 坐标"需在 M2 开始前验证 `worldBound` 在 UIToolkit 中的坐标系可靠性，以及 `picking-mode: Ignore` 是否完全屏蔽鼠标事件。
- **当前处理**：文档采用 VisualElement 覆盖层方案，标注为待 PoC 验证。
- **需要决策**：是否确认采用 VisualElement 覆盖层方案，放弃 IMGUI 方案？
- **优先级**：P1

### TODO-007：确认异步截图完成判定机制

- **来源**：`cocs/M07-报告与截图-需求文档.md §8`
- **问题**：需求要求截图异步保存并回传文件路径，但当前仓库未验证 Editor 模式下截图落盘完成的可靠判定方式。
- **当前处理**：文档只固定对外接口 `CaptureAsync` 和"不得阻塞主流程超过 1 帧"的约束，不固定内部完成判定实现。
- **需要决策**：异步截图完成是否允许通过轮询目标文件存在与稳定文件长度来判定？
- **优先级**：P1

### TODO-008：确认 CLI 参数优先级与 Unity 原生参数协作规则

- **来源**：`cocs/M08-命令行与CI集成-需求文档.md §8`
- **问题**：文档定义了 `unityUIFlow.*` 参数，但尚未验证它与 Unity 原生 `-testFilter`、`-runTests`、`-batchmode` 的组合顺序和优先级。
- **当前处理**：文档暂定"显式 `unityUIFlow.*` 优先于默认值，不覆盖 Unity 原生参数本身"。
- **需要决策**：当 `-testFilter` 与 `-unityUIFlow.testFilter` 同时存在时，是否只保留二者交集？
- **优先级**：P1

### TODO-009：确认 Fixture 基座与官方测试宿主的生命周期兼容性

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md §8`
- **问题**：`EditorWindowUITestFixture<TWindow>` 在 `com.unity.test-framework@1.7.0` + Unity `6000.6.0a2` 下的窗口创建、销毁、`[UnitySetUp]`/`[UnityTearDown]` 钩子是否与文档设想完全一致，尚未原型验证。
- **当前处理**：文档推荐优先使用 `[UnitySetUp]`/`[UnityTearDown]`（IEnumerator 异步），标注为待 PoC 验证。
- **需要决策**：是否确认首版测试基座使用 `[UnitySetUp]` 异步生命周期，而不是同步 `[SetUp]`？
- **优先级**：P1

### TODO-010：确认 CSV 数据源编码兼容范围

- **来源**：`cocs/M01-用例编排与数据驱动-需求文档.md §8`
- **问题**：文档确定支持 UTF-8 编码 CSV，但带 BOM 的 UTF-8 和 GB18030 是否需要支持尚未确认。
- **当前处理**：仅支持 UTF-8（无 BOM），读取时使用 `StreamReader` 默认编码。
- **需要决策**：是否需要同时支持带 BOM 的 UTF-8 和 GB18030 编码的 CSV 文件？
- **优先级**：P2

### TODO-011：确认 UIToolkit 元素可见性判定标准

- **来源**：`cocs/M04-元素定位与等待-需求文档.md §8`
- **问题**：文档定义可见性需满足 `display != None`、`visibility != Hidden`、`opacity > 0`、`panel != null`，但 `pickingMode`、`enabledInHierarchy`、裁剪区域是否也影响"可见"定义尚未在目标 Unity 版本上验证。
- **当前处理**：仅检查上述 4 个属性，额外属性暂不检查。
- **需要决策**：是否需要在可见性判定中同时检查 `pickingMode != Ignore` 和 `enabledInHierarchy`？
- **优先级**：P2

### TODO-012：确认同步查找性能目标 10ms 是否可达

- **来源**：`cocs/M04-元素定位与等待-需求文档.md §8`
- **问题**：文档约束"同步查找目标耗时不超过 10ms，基线为 2000 节点以内的 UI 树"，但该数值未经基准测试验证。UQuery 在不同 UIToolkit 版本下的实际性能特征未知。
- **当前处理**：保留 10ms 目标作为设计约束，标注为需原型验证。
- **需要决策**：是否确认 10ms / 2000 节点作为首版性能基线，还是调整为其他数值？
- **优先级**：P1

### TODO-013：确认自定义动作程序集白名单配置方式

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md §8`
- **问题**：文档定义自定义动作扫描范围为"配置程序集白名单"，默认包含 `Assembly-CSharp`、`Assembly-CSharp-Editor`、`Assembly-CSharp-firstpass`，但白名单的配置方式（硬编码 vs 配置文件 vs 特性标注）未确定。
- **当前处理**：默认硬编码三个标准程序集，扩展机制待 PoC 验证。
- **需要决策**：是否通过 `.unityuiflow.json` 配置文件提供白名单扩展能力，还是仅硬编码？
- **优先级**：P1

### TODO-014：明确 Headed 面板 failurePolicy 与执行引擎 ContinueOnStepFailure 的优先级映射

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档分别定义了 `ContinueOnStepFailure`（M03）、`runMode`（M06）、`failurePolicy`（M06），但未明确三者在 Headed 运行时如何映射。
- **当前处理**：已在 M06 §4 写入优先级规则：`failurePolicy=Pause` 始终暂停；`failurePolicy=Continue` 时由 `ContinueOnStepFailure` 决定；非 Headed 模式下 `failurePolicy` 不生效。
- **需要决策**：是否确认上述优先级规则为最终基线？
- **优先级**：P2
- **已解决**：2026-04-10，优先级规则已写入 M06 §4 和 §2。

### TODO-017：明确 StepDefinition 对"动作自定义参数"的 schema 边界

- **来源**：`cocs/M01-用例编排与数据驱动-需求文档.md` / `cocs/M05-动作系统与CSharp扩展-需求文档.md`
- **问题**：`M01` 的 `StepDefinition` 显式定义了 `name/action/selector/value/expected/timeout/duration/if/repeat_while` 等公共字段，但 `M05` 的内置动作与自定义动作又要求 `from/to/key/property` 等额外参数。文档未明确"未知键是否统一视为动作参数字典"。
- **当前处理**：当前代码实现已将 `StepDefinition` 未知字段归并到 `Parameters: Dictionary<string, string>`，支持内置动作和自定义动作扩展。
- **需要决策**：是否在文档中正式补充"步骤允许附带任意字符串键值，并作为动作参数字典传递"的 schema 规则？
- **优先级**：P1

### TODO-018：明确 Headed 面板状态与执行引擎选项的映射关系

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档分别在 M03 定义了 `runMode`、`failurePolicy`，在 M06 定义了 `HeadedPanelState` 与 `ContinueOnStepFailure`，尚未明确它们的完整映射关系，尤其是 `failurePolicy=Pause/Continue` 与 `ContinueOnStepFailure=true/false` 是交叉关系还是联动关系。
- **当前处理**：当前代码已将 `runMode` 注入 `RuntimeController`，并将 `failurePolicy` 与 `ContinueOnStepFailure` 拆开处理，但这属于实现推斜，尚未由文档明确授权。
- **需要决策**：是否在文档中显式定义 Headed 运行时"面板控制状态"与"用例执行失败策略"的映射优先级？
- **优先级**：P2

### TODO-020：明确 `if.exists` / `repeat_while.condition.exists` 的"存在"是否需要可见性约束

- **来源**：`cocs/M01-用例编排与数据驱动-需求文档.md` / `cocs/M04-元素定位与等待-需求文档.md`
- **问题**：文档中 `exists` 语义描述为"选择器存在"，但没有明确这是 DOM/UI 树层面的存在，还是"可见且存在"。当前实现已按"仅存在，不要求可见"处理，以避免与 `assert_visible` 的语义重叠。
- **当前处理**：执行器在判断 `if.exists` 和 `repeat_while.condition.exists` 时调用 `Finder.Exists(..., requireVisible: false)`。
- **需要决策**：是否在文档中明确 `exists` 与"可见性"完全解耦，仅判断元素树命中？
- **优先级**：P2

### TODO-021：明确 `UnityUIFlowFixture<TWindow>` 是否必须覆盖同步 `[SetUp]` / `[TearDown]` 分支

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md`
- **问题**：文档 §4 同时描述了 `[UnitySetUp]`/`[UnityTearDown]` 与同步 `[SetUp]`/`[TearDown]` 的选择规则，但当前实现只提供了异步的 `[UnitySetUp]` / `[UnityTearDown]` 路径，未覆盖纯同步 `[Test]` 用例。
- **当前处理**：当前优先满足文档推荐的 `[UnityTest]` 主路径；同步测试生命周期尚未提供单独实现。
- **需要决策**：首版是否需要补齐同步基座分支，还是明确 V1 仅保证 `[UnityTest]` 场景？
- **优先级**：P2

### TODO-022：明确文档中的"派生类未调用 `base.SetUp()` 需输出 Warning"是否要求强制检测

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md §6 / §9`
- **问题**：文档要求"派生类覆写 `SetUp` 未调用 `base.SetUp()` 时输出 Warning"，但在 NUnit 生命周期模型下，基类无法可靠检测派生类是否显式调用了 `base.SetUp()`，除非改成模板方法模式或引入额外约束。
- **当前处理**：现有代码依赖使用约定，尚未实现该 Warning 检测。
- **需要决策**：是否要调整文档，改成"通过模板方法模式强制调用基类实现"，而不是保留当前不可验证的约束？
- **优先级**：P2

### TODO-023：确认 `DebugOnFailure` 的生效语义与落点

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档声明 `DebugOnFailure` 用于"失败时是否保留现场"，但当前实现主要通过 Headed 面板的 `failurePolicy` 和失败截图来承接失败调试，`TestOptions.DebugOnFailure` 字段本身尚未形成独立行为。
- **当前处理**：字段已保留在 `TestOptions` 中，但未单独控制失败暂停、现场保留或高亮清理。
- **需要决策**：是否要求 `DebugOnFailure=false` 时显式关闭失败暂停/现场保留，仅保留结果与报告？若是，需要定义它与 `failurePolicy` 的优先级关系。
- **优先级**：P2

### TODO-025：Define Fallback Click Simulation Contract

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md` / `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
- **问题**：当前 fallback 实现直接派发 UIToolkit 鼠标事件以保持与当前 Unity API 面的兼容。`click` 的 fallback 事件序列属于推断实现约定，尚未形成文档产品规则。
- **当前处理**：测试与示例窗口已按当前 fallback 行为对齐，但这是推断实现约定，而非文档化产品规则。
- **需要决策**：若 V1 继续允许在无官方驱动时回退到 fallback 模式，文档是否应显式定义 `click`/`double_click` 必须模拟的事件序列，以及与官方驱动相比的保真度声明？
- **优先级**：P2

### TODO-026：验证 `com.unity.test-framework@1.7.0` 中 UI 测试子系统的 API 可用性

- **来源**：`cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md` / `ProjectSettings/ProjectVersion.txt`
- **问题**：`com.unity.ui.test-framework` 已由 Unity 官方移除，其 UI 宿主与交互仿真能力已合并到 `com.unity.test-framework`。当前仓库已安装 `com.unity.test-framework@1.7.0`，但 `EditorWindowUITestFixture<TWindow>` 的具体命名空间、程序集引用路径、生命周期签名、以及 Unity `6000.6.0a2` 下的实际可用性尚未通过原型验证。
- **当前处理**：文档已将包依赖更新为 `com.unity.test-framework`（含 UI 测试子系统），但 API 具体用法标注为"设计提案，实现时确认"。
- **需要决策**：确认在 `com.unity.test-framework@1.7.0` + Unity `6000.6.0a2` 下，`EditorWindowUITestFixture<TWindow>` 的正确引用路径与生命周期方法签名，以便 M09 能绑定官方宿主。
- **优先级**：P0

### TODO-027：验证 InputSystem 测试设备映射与 `press_key` / `type_text` 的实现路径

- **来源**：`cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md` / `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
- **问题**：目标方向已确认，但 `PressKeyAction` 和 `TypeTextAction` 映射到 InputSystem 测试 API 的具体方式、焦点链路行为、文本提交语义，以及 EditorWindow 兼容性仍需原型验证。
- **当前处理**：文档已将当前键盘和文本输入行为标注为仅过渡 fallback，不再作为最终语义基线。
- **需要决策**：确认测试设备创建方式、事件注入路径，以及 `type_text` 与 `type_text_fast` 在 V1 中的具体支持范围。
- **优先级**：P0

---

## 已解决

### RESOLVED-001: YAML host window auto-open and example asset conventions are implemented

- **Supersedes**: `TODO-019`, `TODO-024`
- **Status**: Implemented on 2026-04-09
- **Resolution**:
  - Added `fixture.host_window.type` and `fixture.host_window.reopen_if_open`
  - Added auto-open / close-on-dispose host window execution path
  - Added `Assets/Examples/Editor`, `Assets/Examples/Uxml`, `Assets/Examples/Uss`, `Assets/Examples/Yaml` conventions
  - Added acceptance tests that execute example YAML files without `rootOverride`
- **References**:
  - `cocs/M10-Examples验收测试与自动宿主-需求文档.md`
  - `Assets/UnityUIFlow/Editor/Execution/UnityUIFlow.Execution.cs`
  - `Assets/UnityUIFlow/Editor/Tests/UnityUIFlow.ExamplesAcceptanceTests.cs`

### RESOLVED-002: Official UI test framework dependencies locked as V1 baseline

- **Supersedes decision scope of**: `TODO-005`, `TODO-009`, `TODO-015`, `TODO-016`
- **Status**: Decision locked on 2026-04-10; updated 2026-04-10 to reflect package consolidation
- **Resolution**:
  - `com.unity.ui.test-framework` has been removed by Unity and its functionality merged into `com.unity.test-framework`. The currently installed `com.unity.test-framework@1.7.0` is the single authoritative UI test dependency.
  - `EditorWindowUITestFixture<TWindow>` and UI interaction simulation (`PanelSimulator` etc.) are now provided by `com.unity.test-framework`.
  - InputSystem test support based on `com.unity.inputsystem@1.19.0` is confirmed as V1 dependency for `press_key` and `type_text` high-fidelity input semantics.
  - Current `SendEvent` / `Event.GetPooled` / direct-value-write behavior remains documented as transition fallback only.
  - Formal requirements recorded in `cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md`.

### RESOLVED-003: M06 HeadedPanelState table structure and failurePolicy design gap fixed

- **Status**: Resolved on 2026-04-10
- **Resolution**:
  - Fixed `HeadedPanelState` data model table: moved `currentStepName`, `currentSelector`, `lastErrorMessage`, `failurePolicy`, `retainSceneOnFailure` fields back into the main table.
  - Added `Failed` state clarification: `Failed` is an informational visual state that allows continued step updates when `ContinueOnStepFailure=true`.
  - Added explicit failurePolicy/ContinueOnStepFailure priority mapping in M06 §4.
- **References**: `cocs/M06-Headed可视化执行-需求文档.md`

### RESOLVED-004: M11 rewritten due to encoding corruption and non-compliant structure

- **Status**: Resolved on 2026-04-10
- **Resolution**:
  - Original M11 file had all Chinese characters corrupted (encoding damage), making the document unreadable.
  - Original structure did not follow the mandatory 11-chapter format.
  - Document was rewritten from scratch using the 11-chapter structure, preserving the original design intent.
  - Fixed document header from garbled "M10" to correct "M11".
- **References**: `cocs/M11-Examples验收测试与自动宿主-需求文档.md`

### RESOLVED-005: Unity 版本与包依赖基线锁定

- **来源**：`ProjectSettings/ProjectVersion.txt` / `Packages/manifest.json` / `Packages/packages-lock.json`
- **状态**：已解决，2026-04-10
- **决策**：
  - Unity 编辑器版本固定为 `6000.6.0a2`（当前仓库实际版本），不再回退或切换到正式发布版本。所有文档以此为实现基准。
  - `com.unity.ui.test-framework` 已由 Unity 官方移除，其 UI 测试宿主与交互仿真能力已合并到 `com.unity.test-framework`。当前仓库安装的 `com.unity.test-framework@1.7.0` 即为唯一 UI 测试依赖，无需额外安装独立包。
  - 依赖基线锁定：`com.unity.test-framework@1.7.0`（含 UI 测试能力）、`com.unity.inputsystem@1.19.0`、`com.unity.editorcoroutines@1.1.0`（间接依赖，已安装）。
  - 所有文档中原有的 `com.unity.ui.test-framework` 包引用统一改为 `com.unity.test-framework`（UI 测试子系统）。
- **取代**：TODO-001、TODO-002
