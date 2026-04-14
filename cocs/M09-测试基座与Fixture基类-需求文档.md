# M09 测试基座与Fixture基类 需求文档

版本：1.3.0  
日期：2026-04-11  
状态：更新基线（补充当前环境官方宿主缺失与 strict 边界）

## 1. 模块职责

- 负责：提供统一的 `UnityUIFlowFixture<TWindow>` 测试基座，管理测试窗口、根节点、共享查找器、截图管理器与 YAML 执行桥接。
- 负责：为 C# 测试、Page Object 测试、YAML 回归测试提供一致的宿主环境。
- 负责：定义当前官方宿主桥接（`OfficialEditorWindowPanelSimulator`）与可选直接继承 `EditorWindowUITestFixture<TWindow>` 路径之间的边界。
- 负责：在当前 Unity `6000.6.0a2` 环境中，把 `com.unity.ui.test-framework@6.3.0` 的官方宿主能力接入现有 fixture 生命周期。
- 不负责：不负责 YAML 语法解析，不负责具体动作实现，不负责 CLI 参数解析。
- 输入/输出：输入为目标 `EditorWindow` 类型、测试生命周期事件、YAML 文本或动作调用；输出为初始化完成的测试上下文、执行结果与清理状态。

## 2. 数据模型

### UnityUIFlowFixture<TWindow>

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| Window | TWindow | 必填 | 当前被测 `EditorWindow` 实例 | 非空，且必须继承 `EditorWindow` | 无 |
| Root | VisualElement | 必填 | 当前窗口根节点 | 非空 | 无 |
| Finder | ElementFinder | 必填 | 当前测试共享查找器 | 非空 | 无 |
| Screenshot | ScreenshotManager | 必填 | 当前测试共享截图管理器 | 非空 | 无 |
| CurrentOptions | TestOptions | 必填 | 当前测试运行选项 | 非空 | `CreateDefaultOptions()` 返回值 |
| CurrentContext | ExecutionContext | 可选 | 当前 YAML 执行上下文 | `null` 或执行上下文实例 | `null` |
| IsWindowReady | bool | 必填 | 当前窗口是否完成初始化 | `true`、`false` | `false` |
| YamlSource | string | 可选 | 最近一次执行的 YAML 文本或来源标识 | `null` 或非空字符串 | `null` |

### FixtureHostMode（设计提案，实现时确认）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| modeName | string | 必填 | 宿主模式名称 | `fallback_get_window`、`official_editor_window_fixture` | 无 |
| hostFactory | string | 必填 | 窗口创建路径 | `EditorWindow.GetWindow<TWindow>()`、`OfficialEditorWindowPanelSimulator`、`EditorWindowUITestFixture<TWindow>` | 无 |
| packageDependency | string | 必填 | 宿主模式所需依赖 | `none`、`com.unity.ui.test-framework` | `none` |
| acceptanceAllowed | bool | 必填 | 是否可作为正式验收宿主 | `true`、`false` | `false` |
| migrationStatus | string | 必填 | 当前状态 | `current`、`target`、`deprecated_after_migration` | 无 |
| notes | string | 可选 | 备注 | 非空字符串或 `null` | `null` |

### FixtureExecutionBridge（设计提案，实现时确认）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| rootSource | string | 必填 | 执行根节点来源 | `fixture_root` | `fixture_root` |
| finderReuse | bool | 必填 | 是否复用 fixture 内共享 Finder | `true`、`false` | `true` |
| screenshotReuse | bool | 必填 | 是否复用 fixture 内共享 ScreenshotManager | `true`、`false` | `true` |
| allowSecondaryHost | bool | 必填 | 是否允许在 fixture 内再创建第二宿主窗口 | `true`、`false` | `false` |
| captureYamlSource | bool | 必填 | 是否记录最近一次 YAML 来源 | `true`、`false` | `true` |
| currentSupport | string | 必填 | 当前实现支持度 | `implemented`、`planned` | `implemented` |

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 创建默认测试选项 | Fixture 初始化前 | 无 | `UnityUIFlowFixture<TWindow>.CreateDefaultOptions` | 不涉及；每次测试重建 |
| 初始化测试宿主 | NUnit `[UnitySetUp]` | `TWindow` 无法创建；根节点缺失 | `UnityUIFlowFixture<TWindow>.SetUp` | 不涉及；初始化失败直接终止测试 |
| 执行 YAML 内容 | 测试代码调用 | Fixture 未就绪；YAML 为空 | `UnityUIFlowFixture<TWindow>.ExecuteYamlStepsAsync` | 不涉及；由执行器管理 UI 状态 |
| 执行单个动作 | 测试代码调用 | Fixture 未就绪；动作上下文缺失 | `UnityUIFlowFixture<TWindow>.ExecuteActionAsync` | 不涉及 |
| 清理宿主与上下文 | NUnit `[UnityTearDown]` | 当前窗口为空 | `UnityUIFlowFixture<TWindow>.TearDown` | 不涉及；关闭窗口并释放上下文 |

## 4. 交互规格

- 触发事件：NUnit 进入单个测试前执行 `SetUp`，退出测试后执行 `TearDown`。
- 中间状态：`Uninitialized -> WindowCreated -> RootReady -> ContextReady -> Executing/Idle -> TornDown`。
- 数据提交时机：`SetUp` 成功后立即初始化 `Finder`、`Screenshot`、`CurrentOptions`；调用 `ExecuteYamlStepsAsync` 时记录 `YamlSource` 并创建 `CurrentContext`。
- 取消/回退行为：测试失败时仍必须进入 `TearDown`；`TearDown` 失败不能阻断 NUnit 后续清理链路。

### 当前实现

1. `SetUp` 通过 `EditorWindow.GetWindow<TWindow>()` 创建窗口。
2. `SetUp` 调用 `Window.Show()` 后等待一帧，再从 `Window.rootVisualElement` 取得 `Root`。
3. `Finder` 与 `Screenshot` 在当前测试内只初始化一次。
4. `ExecuteYamlStepsAsync` 始终使用当前 `Root` 作为执行根节点，不允许在 fixture 内另开第二宿主窗口。
5. 当前环境已完成 `com.unity.ui.test-framework@6.3.0` 安装验证，fixture 会在 `SetUp` 后优先绑定 `OfficialEditorWindowPanelSimulator` 官方宿主桥接；仅在非官方入口时才保留 fallback 标识。

### 目标基线

1. 正式验收模式下，`UnityUIFlowFixture<TWindow>` 必须建立到 `com.unity.ui.test-framework@6.3.0` 的官方宿主桥接；当前实现为 `OfficialEditorWindowPanelSimulator`，可选增强路径为直接继承 `EditorWindowUITestFixture<TWindow>`。
2. `Window`、`Root`、`Finder`、`Screenshot`、`CurrentOptions`、`CurrentContext` 这些对外字段名保持不变。
3. 当前 `EditorWindow.GetWindow<TWindow>()` 路径只保留为迁移兼容模式，不再作为正式宿主能力宣称。
4. Headed、YAML 执行、C# 直接动作执行必须共用同一宿主窗口与同一 `Root`。

## 5. 视觉规格

不涉及

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `TWindow` 必须继承 `EditorWindow` | 编译期 / Fixture 定义期 | Error | `测试窗口类型必须继承 EditorWindow` |
| `SetUp` 完成后 `Window` 不能为空 | `SetUp` 完成前 | Error | `测试窗口创建失败：{windowType}` |
| `Root` 不能为空 | `SetUp` 完成前 | Error | `测试窗口根节点缺失：{windowType}` |
| `ExecuteYamlStepsAsync` 的 YAML 内容不能为空 | 执行 YAML 前 | Error | `YAML 内容不能为空` |
| 正式验收模式下不得继续使用 fallback 宿主 | Fixture 初始化前 | Error | `正式验收模式下必须使用官方宿主桥接` |
| 当前环境未成功绑定官方宿主桥接时不得把 fallback 宿主记为官方宿主 | Fixture 初始化后 / 报告写入前 | Error | `当前环境缺失官方宿主能力，禁止把 fallback 宿主记为官方宿主` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 测试窗口创建失败 | FIXTURE_WINDOW_CREATE_FAILED | `测试窗口创建失败：{windowType}` | 抛异常并终止当前测试 |
| 根节点缺失 | FIXTURE_ROOT_MISSING | `测试窗口根节点缺失：{windowType}` | 抛异常并终止当前测试 |
| YAML 内容为空 | FIXTURE_YAML_EMPTY | `YAML 内容不能为空` | 抛异常并终止当前测试 |
| Fixture 上下文未就绪 | FIXTURE_CONTEXT_NOT_READY | `测试基座上下文未初始化` | 抛异常并终止当前测试 |
| 清理失败 | FIXTURE_TEARDOWN_FAILED | `测试清理失败：{detail}` | 记录错误并继续 NUnit 清理流程 |
| strict 模式下官方宿主缺失 | FIXTURE_WINDOW_CREATE_FAILED | `正式验收模式下未能创建官方测试宿主：{windowType}` | 抛异常并终止当前测试 |

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 主动通知 | 将 `Root`、`CurrentOptions` 与 YAML 内容交给执行引擎运行 | `TestRunner.RunAsync` |
| M04 元素定位与等待 | 主动通知 | 通过 fixture 暴露统一 `Finder` 给测试代码与动作系统复用 | `ElementFinder` |
| M05 动作系统与CSharp扩展 | 被动接收 | 通过 `ExecuteActionAsync` 为单动作测试提供标准 `ActionContext` | `UnityUIFlowFixture<TWindow>.ExecuteActionAsync` |
| M07 报告与截图 | 被动接收 | 通过 fixture 复用 `ScreenshotManager` | `ScreenshotManager` |
| M12 官方UI测试框架与输入系统测试接入 | 被动接收 | 将当前宿主升级为 `com.unity.ui.test-framework` 的官方宿主桥接（当前为 `OfficialEditorWindowPanelSimulator`，可选直接继承 `EditorWindowUITestFixture<TWindow>`） | `UnityUIFlowFixture<TWindow>`、`EditorWindowPanelSimulator`、`EditorWindowUITestFixture<TWindow>` |

## 8. 技术实现要点

- 关键类与职责：
  - `UnityUIFlowFixture<TWindow>`：当前统一测试基座。
  - `UnityUIFlowFixture<TWindow>.SetUp` / `TearDown`：当前生命周期入口。
  - `UnityUIFlowFixture<TWindow>.ExecuteYamlStepsAsync`：当前 YAML 执行桥接入口。
  - `OfficialUiToolkitTestAvailability`：负责探测当前环境是否真实存在官方宿主符号。
  - `UnityUIFlowSimulationSession`：负责记录当前测试实际使用的宿主标识与驱动标识，并绑定官方 `EditorWindowPanelSimulator`。
  - `OfficialEditorWindowHostBridge`：负责把 `EditorWindow` 与官方 `EditorWindowPanelSimulator` 对齐。
  - `EditorWindowUITestFixture<TWindow>`：官方宿主基类；当前不是唯一实现路径，但仍是后续可选增强方向。

- 核心流程：

```text
[UnitySetUp]
-> CreateDefaultOptions()
-> Create host window
   -> current: EditorWindow.GetWindow<TWindow>()
   -> current official path: OfficialEditorWindowPanelSimulator bridge
   -> optional future path: direct EditorWindowUITestFixture<TWindow>
-> Wait one frame until Root is ready
-> Bind UnityUIFlowSimulationSession official host if available
-> Initialize Finder / Screenshot / IsWindowReady
-> Execute test body / ExecuteYamlStepsAsync / ExecuteActionAsync
-> [UnityTearDown]
-> Dispose CurrentContext
-> Close host window
```

- 性能约束：
  - 单个测试只允许存在一个当前宿主窗口。
  - `Finder` 和 `ScreenshotManager` 必须在单个测试内复用，不得每步重建。
  - `TearDown` 必须在当前测试结束时完成资源释放，不允许跨测试保留宿主窗口。

- 禁止项：
  - 禁止 `ExecuteYamlStepsAsync` 在当前 fixture 内再次创建第二业务宿主窗口。
  - 禁止正式验收模式下继续把 `EditorWindow.GetWindow<TWindow>()` 记为最终宿主实现。
  - 禁止通过覆写字段的方式绕开基类统一上下文初始化。

## 9. 验收标准

1. [定义继承 `UnityUIFlowFixture<MyWindow>` 的测试类] -> [执行测试] -> [`SetUp` 后可以访问非空的 `Window`、`Root`、`Finder`、`Screenshot`]
2. [调用 `ExecuteYamlStepsAsync(validYaml)`] -> [执行测试] -> [YAML 使用当前 fixture 的 `Root` 运行，不创建第二宿主窗口]
3. [调用 `ExecuteActionAsync(action, parameters)`] -> [执行单动作测试] -> [动作能获取完整 `ActionContext` 并正确作用于当前窗口]
4. [正式验收模式开启且官方宿主已接入] -> [执行 fixture 生命周期回归] -> [宿主通过 `OfficialEditorWindowPanelSimulator` 正常绑定并清理]
5. [窗口创建失败] -> [进入 `SetUp`] -> [抛出 `FIXTURE_WINDOW_CREATE_FAILED`，测试立即失败]
6. [测试主体抛出异常] -> [结束测试] -> [`TearDown` 仍执行，并释放窗口与上下文]
7. [当前环境未成功绑定官方宿主桥接且 `RequireOfficialHost=true`] -> [进入 `SetUp`] -> [立即失败，不得继续以 fallback 宿主执行]

## 10. 边界规范

- 空数据：
  - 纯 C# 动作测试允许不执行 YAML；此时 `YamlSource` 允许为 `null`。

- 单元素：
  - 根节点下只有一个可测元素时，fixture 仍必须正常初始化，并支持 `Finder` 查询与单动作执行。

- 上下限临界值：
  - 单个测试仅允许有一个当前宿主窗口。
  - `SetUp` 至少等待一帧再读取 `Root`，不允许在同帧假定 UI 已完全就绪。

- 异常数据恢复：
  - `TearDown` 失败时必须记录 `FIXTURE_TEARDOWN_FAILED`，但不得污染后续测试执行。
  - 官方宿主接入失败时必须返回明确失败结论，不得静默回退为正式基线通过。
  - 当前环境缺失官方宿主符号时，fixture 仍可继续服务 fallback 回归，但必须在报告与调试信息中明确宿主模式不是官方宿主。

## 11. 周边可选功能

- P1：支持为 fixture 注入官方宿主模式与迁移兼容模式的显式切换开关。
- P1：支持多窗口协同测试宿主，但不进入 V1 正式验收范围。
- P2：支持同步 `[SetUp]` / `[TearDown]` 分支，与纯同步 `[Test]` 场景对齐。
