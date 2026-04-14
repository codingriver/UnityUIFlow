# UnityUIFlow TODO

## 2026-04-11 新增已解决项

### RESOLVED-009: 菜单断言、动作白名单与列表增强能力已落地

- **Status**: Resolved on 2026-04-11
- **Resolution**:
  - 自定义动作扫描已收口到程序集白名单，默认包含 `Assembly-CSharp`、`Assembly-CSharp-Editor`、`Assembly-CSharp-firstpass`，并支持通过 `.unityuiflow.json` 的 `customActionAssemblies` 扩展。
  - 动作层已补齐 `assert_menu_item`、`assert_menu_item_disabled`，复用官方 `ContextMenuSimulator` / `PopupMenuSimulator` 当前打开菜单状态。
  - 列表交互已补齐 `drag_reorder` 与 `select_list_item.indices`。
- **References**:
  - `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
  - `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.AdvancedActions.cs`
  - `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs`
  - `.unityuiflow.json`
  - `Assets/Tests/UnityUIFlow.LocatorsAndActionsTests.cs`
  - `Assets/Tests/UnityUIFlow.ExecutionReportingCliTests.cs`

## 待决策项

### TODO-003：确认 YAML 解析库选型

- **来源**：`cocs/M02-YAML解析与执行计划-需求文档.md §8`
- **问题**：当前仅确认需要支持手写 YAML 用例，但 `YamlDotNet` 与 `VYaml` 的错误定位能力、性能、AOT 兼容性尚未在仓库原型中验证。
- **当前处理**：文档推荐使用 `YamlDotNet`（Unity AOT 兼容性更成熟），但未经实际验证。
- **需要决策**：是否确认以 `YamlDotNet` 作为首版唯一 YAML 解析库？
- **推荐方案**：确认使用 `YamlDotNet`。理由：Unity AOT 环境下有大量成熟案例；`VYaml` 性能更优但 AOT 文档不完整；`YamlDotNet` 的行列错误定位能力对 QA 调试 YAML 文件有直接价值，而 EditMode 测试框架对解析性能要求不高。
- **优先级**：P1

### TODO-004：确认 EditMode 执行驱动方式

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md §8`
- **问题**：需求文档新增推荐方案"使用 `EditorApplication.update` 注册每帧回调替代协程"，原方案为 `EditorCoroutineUtility.StartCoroutineOwnerless`（`com.unity.editorcoroutines` 已间接安装）。两种方案的暂停/恢复/超时行为需原型验证对比。
- **当前处理**：文档推荐 `EditorApplication.update` 状态机方案，但标记为待验证。
- **需要决策**：是否确认首版执行引擎使用 `EditorApplication.update` 状态机驱动，而不是 `EditorCoroutineUtility`？
- **推荐方案**：确认使用 `EditorApplication.update` 状态机方案。理由：Unity 编辑器原生回调，无额外包依赖；暂停/恢复/超时均通过状态枚举显式控制，比协程的 `yield` 中止语义更可预测；当前代码实现已按状态机方案落地，切换协程方案反而需要重构。
- **优先级**：P1

### TODO-005：PanelSimulator 与输入能力边界已完成收口（保留记录）

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md §8`
- **问题**：`PanelSimulator` 位于独立包 `com.unity.ui.test-framework@6.3.0`（已添加到 `manifest.json`），API 签名已通过官方文档确认（`Click`、`DoubleClick`、`DragAndDrop`、`MouseMove`、`ScrollWheel`、`TypingText`、`KeyDown/Press/Up`、`FrameUpdate` 等）。剩余风险为 Unity `6000.6.0a2`（alpha）下的安装兼容性。
- **当前处理**：已完成。`OfficialUiPointerDriver` 已基于 `PanelSimulator` 接入，fixture / host-window 路径默认优先走官方驱动；fallback 驱动仅作为 `RootOverrideOnly` 等非官方入口的降级选项。
- **需要决策**：无，V1 当前实现已收口。
- **推荐方案**：保持现状，并把后续工作限定为边界增强而非重复验证。
- **优先级**：Resolved

### TODO-006：确认 Headed 高亮叠加实现方式

- **来源**：`cocs/M06-Headed可视化执行-需求文档.md §8`
- **问题**：推荐方案"VisualElement 透明覆盖层 + `element.worldBound` 坐标"需在 M2 开始前验证 `worldBound` 在 UIToolkit 中的坐标系可靠性，以及 `picking-mode: Ignore` 是否完全屏蔽鼠标事件。
- **当前处理**：文档采用 VisualElement 覆盖层方案，标注为待 PoC 验证。
- **需要决策**：是否确认采用 VisualElement 覆盖层方案，放弃 IMGUI 方案？
- **推荐方案**：确认采用 VisualElement 覆盖层方案，放弃 IMGUI 方案。理由：UIToolkit 原生渲染，不引入混合管线；`worldBound` 在 UIToolkit 中已是稳定 API，社区广泛使用；`picking-mode: Ignore` 是官方文档支持的属性，优先于 `GUIClip.Unclip` 等非文档化的 IMGUI 坐标转换路径；在 M2 开始前写一个 10 行 PoC 即可闭合风险。
- **优先级**：P1

### TODO-007：确认异步截图完成判定机制

- **来源**：`cocs/M07-报告与截图-需求文档.md §8`
- **问题**：需求要求截图异步保存并回传文件路径，但当前仓库未验证 Editor 模式下截图落盘完成的可靠判定方式。
- **当前处理**：文档只固定对外接口 `CaptureAsync` 和"不得阻塞主流程超过 1 帧"的约束，不固定内部完成判定实现。
- **需要决策**：异步截图完成是否允许通过轮询目标文件存在与稳定文件长度来判定？
- **推荐方案**：允许通过轮询目标文件存在且连续两次采样文件长度不变来判定完成，超时上限 30 帧（约 0.5s @ 60fps）。理由：Unity EditMode 下无官方截图完成回调；文件长度稳定是 CI 环境中最可靠的磁盘写入完成检测手段；30 帧超时在正常截图场景下不会触发，异常情况下不会无限阻塞。
- **优先级**：P1

### TODO-008：确认 CLI 参数优先级与 Unity 原生参数协作规则

- **来源**：`cocs/M08-命令行与CI集成-需求文档.md §8`
- **问题**：文档定义了 `unityUIFlow.*` 参数，但尚未验证它与 Unity 原生 `-testFilter`、`-runTests`、`-batchmode` 的组合顺序和优先级。
- **当前处理**：文档暂定"显式 `unityUIFlow.*` 优先于默认值，不覆盖 Unity 原生参数本身"。
- **需要决策**：当 `-testFilter` 与 `-unityUIFlow.testFilter` 同时存在时，是否只保留二者交集？
- **推荐方案**：取**交集**——只运行同时满足 `-testFilter`（Unity 原生）与 `-unityUIFlow.testFilter` 两个过滤条件的用例。理由：交集语义最符合"在 Unity 原生过滤基础上叠加框架级过滤"的直觉；能防止框架过滤意外跑出 Unity 原生过滤范围之外的用例，CI 行为可预期；实现上只需在 UnityUIFlow 入口判断双方条件的 AND 组合。
- **优先级**：P1

### TODO-009：Fixture 基座与官方测试宿主生命周期策略已收口（保留记录）

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md §8`
- **问题**：`EditorWindowUITestFixture<TWindow>` 来自独立包 `com.unity.ui.test-framework@6.3.0`（已安装并可直接引用），其同步 fixture 生命周期与 `UnityUIFlowFixture<TWindow>` 现有 `[UnitySetUp]` / `[UnityTearDown]` 模型如何长期共存，需要明确最终收口策略。
- **当前处理**：当前已选用组合/桥接方案：`UnityUIFlowFixture<TWindow>` 保持现有生命周期，对内通过 `OfficialEditorWindowPanelSimulator` 绑定官方宿主与 `PanelSimulator`，不强制改为继承官方 fixture。
- **需要决策**：V1 主基线已明确，无需再作为阻塞项；“直接继承官方 fixture”仅保留为后续可选增强。
- **推荐方案**：保持当前组合模式作为主基线，仅把“直接继承官方 fixture”保留为 P1 可选增强。理由：当前桥接方案已经完成官方 host/simulator 接入，同时不破坏现有 `UnitySetUp`、YAML 执行桥接和 Headed 集成。
- **优先级**：Resolved for V1 / P1 optional enhancement

### ~~TODO-010~~：确认 CSV 数据源编码兼容范围

- **状态**：✅ 已完成，移至 RESOLVED-008
- **优先级**：~~P2~~

### TODO-011：确认 UIToolkit 元素可见性判定标准

- **来源**：`cocs/M04-元素定位与等待-需求文档.md §8`
- **问题**：文档定义可见性需满足 `display != None`、`visibility != Hidden`、`opacity > 0`、`panel != null`，但 `pickingMode`、`enabledInHierarchy`、裁剪区域是否也影响"可见"定义尚未在目标 Unity 版本上验证。
- **当前处理**：仅检查上述 4 个属性，额外属性暂不检查。
- **需要决策**：是否需要在可见性判定中同时检查 `pickingMode != Ignore` 和 `enabledInHierarchy`？
- **推荐方案**：V1 **不**将 `pickingMode != Ignore` 和 `enabledInHierarchy` 纳入 `assert_visible` 的必要条件，但在 `assert_enabled` / `assert_disabled` 动作中覆盖 `enabledInHierarchy`。理由：`pickingMode = Ignore` 的元素对用户视觉上仍然可见（如 Headed 高亮遮罩层本身）；灰化控件（`enabledInHierarchy = false`）用户也能看见；将其纳入可见性判断会导致合理断言意外失败，混淆"可见"与"可交互"两个不同概念。
- **优先级**：P2

### TODO-012：确认同步查找性能目标 10ms 是否可达

- **来源**：`cocs/M04-元素定位与等待-需求文档.md §8`
- **问题**：文档约束"同步查找目标耗时不超过 10ms，基线为 2000 节点以内的 UI 树"，但该数值未经基准测试验证。UQuery 在不同 UIToolkit 版本下的实际性能特征未知。
- **当前处理**：保留 10ms 目标作为设计约束，标注为需原型验证。
- **需要决策**：是否确认 10ms / 2000 节点作为首版性能基线，还是调整为其他数值？
- **推荐方案**：保留 10ms / 2000 节点作为首版**设计目标**，不作为强制验收门槛；在 M1 PoC 期间实测典型 Editor 窗口（约 200–500 节点）的实际 UQuery 耗时，结果写入文档作为实测基准。若实测 P95 耗时超过 10ms 再按实测调整文档阈值。理由：UIToolkit UQuery 底层有缓存，大多数 Editor 界面节点数远少于 2000；10ms 作为设计目标合理；先测后调比预防性放宽更有依据。
- **优先级**：P1

### TODO-013：确认自定义动作程序集白名单配置方式

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md §8`
- **问题**：文档定义自定义动作扫描范围为"配置程序集白名单"，默认包含 `Assembly-CSharp`、`Assembly-CSharp-Editor`、`Assembly-CSharp-firstpass`，但白名单的配置方式（硬编码 vs 配置文件 vs 特性标注）未确定。
- **当前处理**：默认硬编码三个标准程序集，扩展机制待 PoC 验证。
- **需要决策**：是否通过 `.unityuiflow.json` 配置文件提供白名单扩展能力，还是仅硬编码？
- **推荐方案**：通过 `.unityuiflow.json` 提供白名单扩展，格式为 `"customActionAssemblies": ["MyPlugin.Tests"]`，默认三个标准程序集始终包含无需配置。理由：多 asmdef 项目在独立程序集中定义自定义动作是常见场景；配置文件方案比特性标注更显式、更易于 CI 传参；与现有 `.unityuiflow.json` CLI 配置体系统一，不增加新的配置入口。
- **优先级**：P1

### TODO-014：明确 Headed 面板 failurePolicy 与执行引擎 ContinueOnStepFailure 的优先级映射

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档分别定义了 `ContinueOnStepFailure`（M03）、`runMode`（M06）、`failurePolicy`（M06），但未明确三者在 Headed 运行时如何映射。
- **当前处理**：已在 M06 §4 写入优先级规则：`failurePolicy=Pause` 始终暂停；`failurePolicy=Continue` 时由 `ContinueOnStepFailure` 决定；非 Headed 模式下 `failurePolicy` 不生效。
- **需要决策**：是否确认上述优先级规则为最终基线？
- **推荐方案**：确认当前 M06 §4 写入的优先级规则为最终基线。已有实现对齐，无需额外开发工作，直接关闭。
- **优先级**：P2
- **已解决**：2026-04-10，优先级规则已写入 M06 §4 和 §2。

### TODO-017：明确 StepDefinition 对"动作自定义参数"的 schema 边界

- **来源**：`cocs/M01-用例编排与数据驱动-需求文档.md` / `cocs/M05-动作系统与CSharp扩展-需求文档.md`
- **问题**：`M01` 的 `StepDefinition` 显式定义了 `name/action/selector/value/expected/timeout/duration/if/repeat_while` 等公共字段，但 `M05` 的内置动作与自定义动作又要求 `from/to/key/property` 等额外参数。文档未明确"未知键是否统一视为动作参数字典"。
- **当前处理**：当前代码实现已将 `StepDefinition` 未知字段归并到 `Parameters: Dictionary<string, string>`，支持内置动作和自定义动作扩展。
- **需要决策**：是否在文档中正式补充"步骤允许附带任意字符串键值，并作为动作参数字典传递"的 schema 规则？
- **推荐方案**：是。直接在 M01 §2 StepDefinition 数据模型表后补充一条注释规则："步骤中除公共字段外，允许附带任意字符串键值对，统一归入 `Parameters: Dictionary<string, string>` 传递给动作实现。"此为当前实现已有行为，补充文档规则无任何实现成本。
- **优先级**：P1

### TODO-018：明确 Headed 面板状态与执行引擎选项的映射关系

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档分别在 M03 定义了 `runMode`、`failurePolicy`，在 M06 定义了 `HeadedPanelState` 与 `ContinueOnStepFailure`，尚未明确它们的完整映射关系，尤其是 `failurePolicy=Pause/Continue` 与 `ContinueOnStepFailure=true/false` 是交叉关系还是联动关系。
- **当前处理**：当前代码已将 `runMode` 注入 `RuntimeController`，并将 `failurePolicy` 与 `ContinueOnStepFailure` 拆开处理，但这属于实现推斜，尚未由文档明确授权。
- **需要决策**：是否在文档中显式定义 Headed 运行时"面板控制状态"与"用例执行失败策略"的映射优先级？
- **推荐方案**：是，在 M03/M06 中显式定义以下映射规则（当前实现已与此一致）：① `failurePolicy` 仅在 `runMode=Headed` 时生效；② `failurePolicy=Pause` 覆盖 `ContinueOnStepFailure`，强制暂停；③ `failurePolicy=Continue` 时，`ContinueOnStepFailure` 决定执行引擎是否继续下一步；④ `runMode=Headless` 时 `failurePolicy` 字段被忽略，`ContinueOnStepFailure` 直接控制执行流。补充规则无实现成本。
- **优先级**：P2

### TODO-020：明确 `if.exists` / `repeat_while.condition.exists` 的"存在"是否需要可见性约束

- **来源**：`cocs/M01-用例编排与数据驱动-需求文档.md` / `cocs/M04-元素定位与等待-需求文档.md`
- **问题**：文档中 `exists` 语义描述为"选择器存在"，但没有明确这是 DOM/UI 树层面的存在，还是"可见且存在"。当前实现已按"仅存在，不要求可见"处理，以避免与 `assert_visible` 的语义重叠。
- **当前处理**：执行器在判断 `if.exists` 和 `repeat_while.condition.exists` 时调用 `Finder.Exists(..., requireVisible: false)`。
- **需要决策**：是否在文档中明确 `exists` 与"可见性"完全解耦，仅判断元素树命中？
- **推荐方案**：是。在 M01 §2 和 M04 §4 中明确写入"`exists` 仅判断选择器在 UI 树中命中，不要求元素可见；可见性断言由 `assert_visible` 动作负责"。当前实现已一致，补充文档规则无实现成本；与 Playwright 中 `locator.count() > 0` 的语义对齐，降低用户学习成本。
- **优先级**：P2

### TODO-021：明确 `UnityUIFlowFixture<TWindow>` 是否必须覆盖同步 `[SetUp]` / `[TearDown]` 分支

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md`
- **问题**：文档 §4 同时描述了 `[UnitySetUp]`/`[UnityTearDown]` 与同步 `[SetUp]`/`[TearDown]` 的选择规则，但当前实现只提供了异步的 `[UnitySetUp]` / `[UnityTearDown]` 路径，未覆盖纯同步 `[Test]` 用例。
- **当前处理**：当前优先满足文档推荐的 `[UnityTest]` 主路径；同步测试生命周期尚未提供单独实现。
- **需要决策**：首版是否需要补齐同步基座分支，还是明确 V1 仅保证 `[UnityTest]` 场景？
- **推荐方案**：V1 **不**补齐同步分支，明确文档"V1 仅支持 `[UnityTest]`（`IEnumerator`）用例场景"。理由：EditorWindow 初始化需要帧等待，同步 `[SetUp]` 技术上无法稳定完成宿主初始化；实现同步分支需要引入阻塞等待或虚假完成状态，带来正确性风险；将其列入 §11 P2，有需求时再评估。
- **优先级**：P2

### TODO-022：明确文档中的"派生类未调用 `base.SetUp()` 需输出 Warning"是否要求强制检测

- **来源**：`cocs/M09-测试基座与Fixture基类-需求文档.md §6 / §9`
- **问题**：文档要求"派生类覆写 `SetUp` 未调用 `base.SetUp()` 时输出 Warning"，但在 NUnit 生命周期模型下，基类无法可靠检测派生类是否显式调用了 `base.SetUp()`，除非改成模板方法模式或引入额外约束。
- **当前处理**：现有代码依赖使用约定，尚未实现该 Warning 检测。
- **需要决策**：是否要调整文档，改成"通过模板方法模式强制调用基类实现"，而不是保留当前不可验证的约束？
- **推荐方案**：改为**模板方法模式**：将基类 `SetUp`/`TearDown` 标记为 `sealed`，提供可覆写的 `OnSetUp` / `OnTearDown` 虚方法。理由：这是 C# 中"强制调用基类实现"的标准模式；彻底消除"未调用 `base.SetUp()` 导致宿主未初始化"的静默失败风险；不影响现有用例，只需将覆写目标从 `override SetUp` 改为 `override OnSetUp`；文档约束从"应当调用"变为"无法绕过"，零维护成本。
- **优先级**：P2

### TODO-023：确认 `DebugOnFailure` 的生效语义与落点

- **来源**：`cocs/M03-执行引擎与运行控制-需求文档.md` / `cocs/M06-Headed可视化执行-需求文档.md`
- **问题**：文档声明 `DebugOnFailure` 用于"失败时是否保留现场"，但当前实现主要通过 Headed 面板的 `failurePolicy` 和失败截图来承接失败调试，`TestOptions.DebugOnFailure` 字段本身尚未形成独立行为。
- **当前处理**：字段已保留在 `TestOptions` 中，但未单独控制失败暂停、现场保留或高亮清理。
- **需要决策**：是否要求 `DebugOnFailure=false` 时显式关闭失败暂停/现场保留，仅保留结果与报告？若是，需要定义它与 `failurePolicy` 的优先级关系。
- **推荐方案**：将 `DebugOnFailure` 定义为 Headed 专属的"失败暂停与现场保留"快捷开关，映射规则如下：`DebugOnFailure=true` 等价于 `failurePolicy=Pause`（在 Headed 模式下）；`DebugOnFailure=false` 时 `failurePolicy` 由 `TestOptions` 单独控制；非 Headed 模式下 `DebugOnFailure` 无效（失败截图仍正常触发）。将 `DebugOnFailure` 的优先级定为低于显式 `failurePolicy` 设置——即显式设置 `failurePolicy` 时 `DebugOnFailure` 不再覆盖。
- **优先级**：P2

### TODO-025：Define Fallback Click Simulation Contract

- **来源**：`cocs/M05-动作系统与CSharp扩展-需求文档.md` / `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
- **问题**：当前 fallback 实现直接派发 UIToolkit 鼠标事件以保持与当前 Unity API 面的兼容。`click` 的 fallback 事件序列属于推断实现约定，尚未形成文档产品规则。
- **当前处理**：测试与示例窗口已按当前 fallback 行为对齐，但这是推断实现约定，而非文档化产品规则。
- **需要决策**：若 V1 继续允许在无官方驱动时回退到 fallback 模式，文档是否应显式定义 `click`/`double_click` 必须模拟的事件序列，以及与官方驱动相比的保真度声明？
- **推荐方案**：在 M05 §4 或 §10 中正式写入 fallback 事件序列契约：`click` fallback = `PointerEnter → PointerDown → PointerUp → PointerLeave → Click`；`double_click` fallback = 上述序列执行两次 + `DblClick` 事件；并标注"fallback 事件序列不保证与官方驱动等价，不作为正式验收依据，仅用于迁移期兼容性测试"。这样既锁定实现稳定性，又防止 fallback 结果被误认为官方能力。
- **优先级**：P2

### ~~TODO-026~~：验证 `com.unity.ui.test-framework@6.3.0` 在 Unity `6000.6.0a2` 下的 API 可用性

- **状态**：✅ 已完成，移至 RESOLVED-007
- **优先级**：~~P0~~

### ~~TODO-028~~：asmdef 文件添加 `Unity.UI.TestFramework` 程序集引用

- **状态**：✅ 已完成，移至 RESOLVED-007
- **优先级**：~~P0~~

### TODO-027：验证 InputSystem 测试设备映射与 `press_key` / `type_text` 的实现路径

- **来源**：`cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md` / `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
- **问题**：目标方向已确认，但 `PressKeyAction` 和 `TypeTextAction` 映射到 InputSystem 测试 API 的具体方式、焦点链路行为、文本提交语义，以及 EditorWindow 兼容性仍需原型验证。
- **当前处理**：当前代码已接入 `InputSystemTestFramework+UIToolkitBridge` 过渡链路：`PressKeyAction` 会先尝试通过 InputSystem 测试键盘发键，再补发 UIToolkit 键盘事件；`TypeTextAction` 会先注入文本事件，再按需补偿写值。该链路已通过本地编译与单元测试宿主回归，但仍未达到"官方最终语义基线已锁定"的结论。
- **需要决策**：确认测试设备创建方式、事件注入路径，以及 `type_text` 与 `type_text_fast` 在 V1 中的具体支持范围。
- **推荐方案**：接受当前 `InputSystemTestFramework+UIToolkitBridge` 过渡链路作为 V1 的 `press_key` / `type_text` 基线，并补充以下原型验证点：① 确认 `InputTestFixture` 的 `SetUp`/`TearDown` 与 `UnityUIFlowFixture<TWindow>` 的生命周期可以正确嵌套；② 验证 `Keyboard.current` 在 EditMode Editor 窗口中的焦点路由是否经过 UIToolkit 的键盘事件分发链；③ 将 `type_text` 的 V1 支持范围明确定义为"ASCII 文本输入、Tab/Enter 提交键、常见快捷键（Ctrl+A/C/V/Z）"，IME 组合输入不纳入 V1 验收。
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
- **Status**: Decision locked on 2026-04-10; **corrected 2026-04-11** — previous claim that `com.unity.ui.test-framework` was "removed and merged" was **wrong**.
- **Resolution**:
  - `com.unity.ui.test-framework` is an **independent package** (never merged into `com.unity.test-framework`). Version `6.3.0` released 2026-01-21. Available from Unity 6.3+ in Unity Registry.
  - Package provides `EditorWindowUITestFixture<T>` (`UnityEditor.UIElements.TestFramework`) and `PanelSimulator` (`UnityEngine.UIElements.TestFramework`).
  - Assembly references: `Unity.UI.TestFramework.Runtime`, `Unity.UI.TestFramework.Editor`.
  - `com.unity.ui.test-framework@6.3.0` has been added to `Packages/manifest.json`. Installation compatibility with Unity `6000.6.0a2` (alpha) pending PoC verification (see TODO-028).
  - `com.unity.test-framework@1.7.0` remains installed but only provides NUnit integration and TestRunner UI — **no UI interaction simulation**.
  - InputSystem test support based on `com.unity.inputsystem@1.19.0` confirmed as V1 dependency.
  - Current `SendEvent` / `Event.GetPooled` / direct-value-write behavior remains as transition fallback.
  - Formal requirements updated in `cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md`.

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
- **状态**：已解决，2026-04-10；**2026-04-11 勘误** — 原结论 "`com.unity.ui.test-framework` 已移除并合并" 系**误判**，已更正。
- **决策**：
  - Unity 编辑器版本固定为 `6000.6.0a2`（当前仓库实际版本），不再回退或切换到正式发布版本。所有文档以此为实现基准。
  - `com.unity.ui.test-framework` 是 **独立包**（从未合并到 `com.unity.test-framework`），版本 `6.3.0`（2026-01-21 发布），适用于 Unity 6.3+。
  - `com.unity.test-framework@1.7.0` **仅提供** NUnit 集成与 TestRunner UI，不含任何 UI 交互仿真能力。
  - 依赖基线更新：`com.unity.test-framework@1.7.0`（NUnit/TestRunner）、`com.unity.ui.test-framework@6.3.0`（EditorWindowUITestFixture / PanelSimulator）、`com.unity.inputsystem@1.19.0`、`com.unity.editorcoroutines@1.1.0`（间接依赖，已安装）。
  - `com.unity.ui.test-framework@6.3.0` 已安装并完成 PoC 验证，官方 host / `PanelSimulator` 主执行链已接入。
- **取代**：TODO-001、TODO-002
### RESOLVED-006: 当前环境官方 UI host / PanelSimulator 探测结论已固化为 strict 边界与入口开关

- **Supersedes / narrows**: `TODO-027`
- **Status**: Implemented on 2026-04-11；**2026-04-11 勘误** — 原结论基于"包不存在"的误判；`com.unity.ui.test-framework@6.3.0` 现已添加至 manifest.json。
- **Resolution**:
  - 原调查在 `com.unity.test-framework@1.7.0` 中搜索 `EditorWindowUITestFixture` 和 `PanelSimulator` 未找到，因为这些类型从未存在于该包中。
  - 实际来源为独立包 `com.unity.ui.test-framework@6.3.0`，已添加至 `Packages/manifest.json`。待编辑器 Resolve 后类型应可直接引用。
  - 项目仍保留 strict gates（`RequireOfficialHost`、`RequireOfficialPointerDriver`、`RequireInputSystemKeyboardDriver`），在 PoC 验证通过前作为安全回退机制。
  - 待 TODO-026 PoC 验证通过后，反射探测应切换为直接引用，strict gates 的默认值可调整为启用状态。
- **References**:
  - `cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md`
  - `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs`
  - `Assets/UnityUIFlow/Editor/Cli/UnityUIFlow.Cli.cs`
  - `Assets/UnityUIFlow/Editor/Headed/UnityUIFlow.Headed.cs`
  - `Assets/UnityUIFlow/Editor/Headed/UnityUIFlow.BatchRunner.cs`

### RESOLVED-007: `com.unity.ui.test-framework@6.3.0` 安装验证与代码集成完成

- **Supersedes**: `TODO-026`, `TODO-028`
- **Status**: Resolved on 2026-04-11
- **Resolution**:
  - `com.unity.ui.test-framework@6.3.0` 在 Unity `6000.6.0a2`（alpha）下**安装成功**，包正常 Resolve。
  - 包缓存路径：`Library/PackageCache/com.unity.ui.test-framework@ff44d56cd017/`
  - 确认的程序集：`Unity.UI.TestFramework.Runtime`（Runtime）、`Unity.UI.TestFramework.Editor`（Editor）
  - 确认的命名空间：`UnityEngine.UIElements.TestFramework`（PanelSimulator）、`UnityEditor.UIElements.TestFramework`（EditorWindowUITestFixture / EditorWindowPanelSimulator）
  - `PanelSimulator` 为 `abstract class`，提供 `Click`、`DoubleClick`、`DragAndDrop`、`MouseDown/Up/Move`、`ScrollWheel`、`KeyPress`、`TypingText`、`ExecuteCommand` 等完整 API
  - 代码变更：
    - `UnityUIFlow.asmdef` / `UnityUIFlow.Tests.asmdef`：添加 `Unity.UI.TestFramework.Runtime` + `Unity.UI.TestFramework.Editor` 引用
    - `UnityUIFlow.TestIntegrations.cs`：反射探测改为 `typeof(EditorWindowUITestFixture<>)` / `typeof(EditorWindowPanelSimulator)` / `typeof(PanelSimulator)` 直接引用
    - 新增 `OfficialUiPointerDriver`：基于 `PanelSimulator` 实现 `IUiPointerDriver`，`IsOfficial = true`
    - 新增 `OfficialEditorWindowHostBridge`：把 `EditorWindow` 真实绑定到 `EditorWindowPanelSimulator`
    - `UnityUIFlowFixture<TWindow>` / `TestRunner`：fixture 与 YAML host-window 入口现在会调用 `BindEditorWindowHost(...)`
    - `UnityUIFlowSimulationSession.BindPanelSimulator()` / `BindEditorWindowHost()`：绑定官方 host 后自动切换指针与键盘默认驱动
    - `HasExecutableOfficialHost` 改为“当前是否已真实绑定 official host bridge”，而不是仅看符号存在
- **References**:
  - `Assets/UnityUIFlow/Editor/UnityUIFlow.asmdef`
  - `Assets/Tests/UnityUIFlow.Tests.asmdef`
  - `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs`

### RESOLVED-008: CSV UTF-8 BOM、CLI 环境变量优先级与 command actions 已落地

- **Supersedes / narrows**: `TODO-010`, M08 §11 中“环境变量默认值预留项”, M12 §11 中 “ExecuteCommand 尚未在动作层暴露”
- **Status**: Resolved on 2026-04-11
- **Resolution**:
  - CSV 数据源读取已切换为 `StreamReader(path, detectEncodingFromByteOrderMarks: true)`，V1 正式支持 UTF-8 with BOM。
  - CLI 已实现 `UNITY_UI_FLOW_*` 环境变量读取，优先级固定为 `CLI > environment > config > defaults`，并支持 `UNITY_UI_FLOW_CONFIG_FILE`。
  - 动作层已新增 `execute_command` / `validate_command`，官方 host 路径优先走 `PanelSimulator.ExecuteCommand/ValidateCommand`，非官方入口回退到 pooled command event 兼容链路。
  - 对应解析测试、CLI 测试、动作夹具测试与需求文档已同步更新。
- **References**:
  - `Assets/UnityUIFlow/Editor/Parsing/UnityUIFlow.Parsing.cs`
  - `Assets/UnityUIFlow/Editor/Cli/UnityUIFlow.Cli.cs`
  - `Assets/UnityUIFlow/Editor/Actions/UnityUIFlow.Actions.cs`
  - `Assets/UnityUIFlow/Editor/Fixtures/UnityUIFlow.TestIntegrations.cs`
  - `Assets/Tests/UnityUIFlow.ParsingAndPlanningTests.cs`
  - `Assets/Tests/UnityUIFlow.ExecutionReportingCliTests.cs`
  - `Assets/Tests/UnityUIFlow.LocatorsAndActionsTests.cs`
  - `cocs/M08-命令行与CI集成-需求文档.md`
  - `cocs/M12-官方UI测试框架与输入系统测试接入-需求文档.md`
