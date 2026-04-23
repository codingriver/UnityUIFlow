# UnityUIFlow Agent Instructions

本文件为 AI coding agent 提供关于 UnityUIFlow 项目的机器可读约定与全景信息。阅读者应对本项目一无所知，因此本文力求自包含、准确、可操作。

---

## 1. 项目概览

**UnityUIFlow** 是一个基于 Unity Editor + UIToolkit 的 YAML 驱动 UI 自动化测试框架。它让开发者和自动化工具能够通过编写 YAML 用例，对 `EditorWindow` 中的 `VisualElement` 树执行点击、输入、拖拽、断言、截图等操作，而无需手写大量 C# 测试代码。

- **核心定位**：Editor-Only 的自动化测试基础设施（非 Runtime）。
- **测试对象**：Unity Editor 内基于 UIToolkit 的 `EditorWindow`（非 Game View / Play Mode）。
- **用例形式**：YAML 文件为主，C# Fixture 为辅。
- **执行模式**：
  - **Headed 模式**：带可视化窗口、高亮、步进，用于本地开发与 Agent 验证（强制）。
  - **Headless / CI 模式**：通过 Unity CLI `-executeMethod` 批量执行，用于持续集成。

---

## 2. 技术栈与版本约束

| 组件 | 版本 / 说明 | 来源 |
|------|-------------|------|
| Unity Editor | `6000.6.0a2` | `ProjectSettings/ProjectVersion.txt` |
| `com.unity.test-framework` | `1.7.0` | `Packages/manifest.json` |
| `com.unity.ui.test-framework` | `6.3.0` | 官方 UI Test Framework（PanelSimulator 桥接） |
| `com.unity.inputsystem` | `1.19.0` | 键盘/输入高保真链路 |
| `com.unity.ui` | `2.0.0` | UIToolkit 核心 |
| YAML 解析 | `YamlDotNet.dll` | 内嵌预编译 DLL |
| 单元测试 | NUnit + Unity Test Framework | 仅 Editor Mode，无 PlayMode 测试 |
| MCP 服务器 | `unityuiflow` | WebSocket / HTTP / stdio 多协议支持 |

**关键约束**：
- 所有生产代码均在 `Assets/UnityUIFlow/Editor/` 下，编译目标为 **Editor only**。
- 测试代码在 `Assets/Examples/Tests/` 下，同样为 **Editor only**。
- 不存在 PlayMode 测试，也不存在 Runtime 逻辑。

---

## 3. 代码组织与模块划分

### 3.1 目录结构

```
Assets/
├── UnityUIFlow/Editor/          ← 框架核心（15 个 C# 文件，单一名称空间 UnityUIFlow）
│   ├── Actions/                 ← 38 个内置动作实现（IAction）
│   ├── Cli/                     ← 命令行解析与 CI 入口
│   ├── Core/                    ← 领域模型、异常、配置、工具类
│   ├── Execution/               ← 执行引擎（TestRunner、StepExecutor、ElementFinder）
│   ├── Fixtures/                ← NUnit Fixture 基类与官方测试框架桥接
│   ├── Headed/                  ← Editor 可视化窗口（TestRunnerWindow，含批量执行与单条步进调试）
│   ├── Parsing/                 ← YAML 解析、选择器编译、执行计划构建
│   └── Reporting/               ← 截图、Markdown/JSON 报告
├── Examples/                    ← 示例窗口、测试界面 UI 与测试用例
│   ├── Editor/                  ← 示例 EditorWindow（Acceptance + Coverage）
│   ├── Uxml/                    ← UXML 布局文件（示例 + 测试界面）
│   ├── Uss/                     ← 共享样式表
│   ├── Yaml/                    ← 自动化用例（示例 + 回归测试）
│   └── Tests/                   ← 测试汇编（UnityUIFlow.Tests）
└── Plugins/                     ← YamlDotNet.dll
```

### 3.2 核心模块关系

```
YAML 文件
    ↓
Parsing（YamlTestCaseParser → SelectorCompiler → ExecutionPlanBuilder）
    ↓
Execution（TestRunner → StepExecutor）
    ↓
Actions（ActionRegistry 解析 ~38 个 IAction） + Locators（ElementFinder）
    ↓
Fixtures / TestIntegrations（UnityUIFlowFixture<TWindow> + PanelSimulator 桥接）
    ↓
Reporting（ScreenshotManager + MarkdownReporter + JsonResultWriter）
```

### 3.3 关键类型速查

| 类型 | 文件 | 职责 |
|------|------|------|
| `TestRunner` | `Execution/UnityUIFlow.Execution.cs` | 主 API：`RunFileAsync`、`RunSuiteAsync` |
| `StepExecutor` | `Execution/UnityUIFlow.Execution.cs` | 单步执行、超时、高亮、失败截图 |
| `ElementFinder` | `Execution/UnityUIFlow.Locators.cs` | CSS-like 选择器引擎 |
| `ActionRegistry` | `Actions/UnityUIFlow.Actions.cs` | 动作发现与解析 |
| `UnityUIFlowFixture<TWindow>` | `Fixtures/UnityUIFlow.Fixtures.cs` | C# 测试基类 |
| `UnityUIFlowCliEntry` | `Cli/UnityUIFlow.Cli.cs` | CI 入口：`RunAllFromCommandLine` |
| `UnityUIFlowProjectSettings` | `Core/UnityUIFlow.Settings.cs` | `ProjectSettings/UnityUIFlowSettings.asset` |

---

## 4. 开发规范（UXML / USS / C# / YAML）

### 4.1 UXML 命名规范

- **关键交互元素必须设置唯一 `name`**：输入框、按钮、状态标签、toast 根节点、列表根节点、滚动容器。
- **命名风格**：全小写短横线，如 `username-input`、`login-button`、`status-label`、`toast-host`。
- **class 负责样式，不负责主定位**：YAML 优先使用 `#name`，避免 `.class` 作为主选择器。
- **业务语义用 `userData` 提供 `data-*`**：
  ```csharp
  saveButton.userData = new Dictionary<string, string>(StringComparer.Ordinal)
  {
      ["data-role"] = "primary",
  };
  ```
  对应 YAML 选择器：`[data-role=primary]`

### 4.2 UXML 结构规范

- 页面根节点推荐 `feature-root`，主面板推荐 `feature-panel`。
- 动态内容保留稳定宿主：`toast-host`、`dialog-host`、`result-panel`。
- 断言文本必须落在稳定命名元素上（如 `#status-label`），不要只打印到 Console。

### 4.3 USS 规范

- `display: none`、`visibility: hidden`、`opacity: 0` 的元素会被框架视为**不可见**。
- 按钮不要被透明遮罩覆盖，确保可点击区域稳定。

### 4.4 C# 交互规范

- 按钮点击逻辑**优先注册 `MouseUpEvent`**，与当前自动化点击路径兼容性最好：
  ```csharp
  loginButton.RegisterCallback<MouseUpEvent>(_ => HandleLogin());
  ```
- 表单输入优先使用 `TextField`。
- 页面初始化必须可重复：重复打开窗口后应回到稳定初始状态。
- 若页面通过 YAML `fixture.host_window` 打开，可实现 `IUnityUIFlowTestHostWindow.PrepareForAutomatedTest()` 统一构建入口。

### 4.5 YAML 设计规范

- 选择器**优先使用 `#name`**。
- 对动态场景先 `wait_for_element` 再断言。
- 输入测试冒烟场景优先 `type_text_fast`，需要观察逐字输入时用 `type_text`。
- 用例文件扩展名必须是 `.yaml`（不支持 `.yml`）。
- 新页面交付至少包含：`.uxml`、`.uss`、`.cs`、`.yaml`（最小冒烟用例）。

---

## 5. 测试策略

### 5.1 测试分层

| 层级 | 位置 | 说明 |
|------|------|------|
| 单元测试 | `Assets/Examples/Tests/UnityUIFlow.ParsingAndPlanningTests.cs` | 解析器、选择器编译、计划构建、模板渲染 |
| 集成测试 | `Assets/Examples/Tests/UnityUIFlow.LocatorsAndActionsTests.cs` | 真实 EditorWindow 上的动作与定位器 |
| 验收测试 | `Assets/Examples/Tests/UnityUIFlow.ExamplesAcceptanceTests.cs` | 端到端执行 `Assets/Examples/Yaml/*.yaml` |
| Headed/Batch 测试 | `Assets/Examples/Tests/UnityUIFlow.HeadedTests.cs` | 可视化面板、RuntimeController、偏好设置 |
| CLI/报告测试 | `Assets/Examples/Tests/UnityUIFlow.ExecutionReportingCliTests.cs` | CLI 参数、报告生成、ProjectSettings 覆盖 |

### 5.2 测试命名与模式

- 测试汇编：`UnityUIFlow.Tests.asmdef`（Editor only，引用 `UnityUIFlow`、`UnityUIFlow.Examples`、官方 UI Test Framework）。
- 同步测试用 `[Test]`，需要 Editor 窗口生命周期的用 `[UnityTest]` 返回 `IEnumerator`。
- Fixture 测试继承 `UnityUIFlowFixture<TWindow>`，泛型参数为具体的 `EditorWindow` 类型。
- Strict 模式测试验证官方驱动强制要求：
  - `RequireOfficialPointerDriver = true`
  - `RequireInputSystemKeyboardDriver = true`
  - `RequireOfficialHost = true`

---

## 6. MCP 测试强制规范（Agent 执行 YAML 必读）

> **核心原则：YAML 测试 = MCP 服务器 + Headed 模式。缺一不可。**

### 6.1 硬性规则

1. **YAML 测试只能通过 MCP 服务器执行。**
2. **YAML 测试必须使用 Headed 模式。**
3. **没有可用 MCP 服务器时，禁止运行 YAML 测试。**
4. **禁止用 CLI、Unity Test Runner、临时脚本、手工点击或其他替代方式冒充 YAML MCP 测试结果。**
5. **Agent 可以在没有 MCP 的情况下修改代码、修复 Bug、实现需求，但不能声称已完成 YAML 测试验证。**
6. 输出“已验证 YAML 测试通过”的前提，必须是 MCP 工具真实执行成功。

### 6.1.1 MCP 服务器可用性探测规则（强制）

> **在任何情况下，Agent 不得在未完成探测的情况下断言“MCP 服务器不可用”或“没有 MCP 服务器”。**

判定 MCP 服务器不可用之前，必须按顺序完成以下探测步骤：

1. **读取配置文件**：检查 `.vscode/mcp.json`、`.kimi/mcp.json`、`.cursor/mcp.json`、`.opencode/` 等目录中的 MCP 服务器配置，确认 server URL、端口、命令。
2. **网络探测**：使用 `Get-NetTCPConnection` / `netstat` / `curl` 等工具检查配置的端口（如 `8011`、`8765`）是否处于 `Listen` 状态。
3. **协议握手**：实际发送请求调用 `tools/list` 或 `unity_mcp_status`，验证 server 是否响应、Unity Editor 是否已连接（`connected: true`）。
4. **结论**：只有当上述步骤**全部失败**后，才能判定 MCP 服务器不可用，并明确记录每一步的失败原因。

**违规示例**：在未检查端口、未调用 `unity_mcp_status` 的情况下，仅凭“代码中看不到 MCP server 定义”就声称 MCP 不可用。

### 6.2 Headed 模式检查

执行前必须确认项目根目录 `.unityuiflow.json` 满足：

```json
{
  "headed": true,
  "reportPath": "./Reports",
  "screenshotOnFailure": true,
  "defaultTimeoutMs": 10000,
  "customActionAssemblies": ["UnityUIFlow.Tests"]
}
```

若 `headed` 不是 `true`，Agent 应先修正配置，再继续测试流程。

### 6.3 MCP 服务器接管流程

1. 检查当前是否已存在可用 MCP 服务器（不能只看进程，必须能直接调用 tool）。
2. 对 `stdio` 型 MCP，确认“当前执行环境已经接管，并且可以直接调用 MCP tool”。
3. 若可接管，直接复用；若不可接管，关闭旧进程。
4. 按 `.vscode/mcp.json` 重新启动 `unitypilot` MCP 服务器（后台常驻）。
5. 再次确认以下工具真实可调用：
   - `unity_mcp_status`（或宿主映射后的前缀版本，如 `mcp_unitypilot_unity_mcp_status`）
   - `unity_editor_e2e_run`（或 `mcp_unitypilot_unity_editor_e2e_run`）

### 6.4 标准 YAML 执行工具示例

```text
工具: mcp_unitypilot_unity_editor_e2e_run
参数:
  - specPath: Assets/Examples/Yaml/01-basic-login.yaml
  - artifactDir: D:\UnityUIFlow\artifacts
  - exportZip: true
  - stopOnFirstFailure: true
  - webhookOnFailure: true
```

Agent 应根据当前会话实际暴露的工具名选择等价调用方式。

### 6.5 无 MCP 时的正确表述

- ✅ “代码已修改，但 YAML 测试尚未执行，因为当前没有可用 MCP 服务器。”
- ✅ “当前仅完成实现，未完成 MCP 验证。”
- ❌ “测试已通过”（未真实执行时禁止）。

### 6.6 批量测试分片强制规则

> **核心原则：单次调用不得超过 15 个 YAML 文件；超过时必须由调用方分片，逐批发送。**

Unity Editor 同一时间只能执行一个 `ExecutionContext`（`EDITOR_BUSY` 锁）。单次传入过多 YAML 文件会导致：
- MCP 调用响应超时（默认 30s，Unity 侧实际执行可能 300s+）
- Unity 主线程被长时间占用，无法干预
- 中途若发生 Domain Reload，执行状态丢失且无法恢复

#### 硬性规则

1. **单次 `unity_uiflow_run_batch` 的 `yamlPaths` 不得超过 15 个文件。**
2. **用例总数超过 15 时，调用方必须在 Agent 侧分片，逐批发送。** 默认 `batch_size = 10`，可根据单文件平均耗时调整，但上限为 15。
3. **必须等待上一批 `status` 为 `completed` / `failed` / `aborted` 后，才能发送下一批。** Unity 侧存在 `EDITOR_BUSY` 锁，并发调用会返回 `EDITOR_BUSY` 错误。
4. **优先使用 `tools/batch_yaml_runner.py`** 执行批量测试。该脚本自动完成：目录扫描 → 按 `batch_size` 分片 → 逐批调用 MCP → 等待完成 → 汇总结果 → 保存失败清单支持重试。
5. **若直接调用 MCP 工具**，必须复现相同的分片逻辑：
   - 切片后每批调用 `unity_uiflow_run_batch`（或 `unity_uiflow_run_file` 逐条）
   - 轮询等待当前 batch 完成（通过返回的 `executionId` 查询 `unity_uiflow_run_file` / `unity_uiflow_run_suite` 等结果）
   - 再发送下一批

#### 标准批量执行示例

```powershell
# 使用 Agent 侧批量脚本（推荐）
python tools/batch_yaml_runner.py `
  --yaml-dir Assets/Examples/Yaml `
  --batch-size 10 `
  --report-dir Reports/AgentBatch `
  --headed true

# 重试某一批
python tools/batch_yaml_runner.py `
  --retry-from Reports/AgentBatch/batch_003_failed.json
```

```text
# 单条 MCP 工具调用示例（仅适用于 ≤15 个文件）
工具: unity_uiflow_run_batch
参数:
  - yamlPaths: [file1.yaml, file2.yaml, ..., file10.yaml]
  - batchSize: 10
  - batchOffset: 0
  - headed: true
  - reportOutputPath: Reports/AgentBatch/batch_000
  - stopOnFirstFailure: false
  - defaultTimeoutMs: 10000
```

#### 执行模型

Agent 分片 → MCP 转发 → Unity 串行执行（同 batch 内文件逐个执行）→ MCP 轮询等待 → Agent 收到结果 → 发送下一批

- 同一 batch 内的用例在 Unity 主线程上**串行执行**（打开窗口 → 执行 → 关闭窗口，循环）
- MCP 轮询间隔 500ms，不阻塞 Unity
- 每批有独立超时（默认 `120s + defaultTimeoutMs/1000 × batch_size + 120s`）

---

## 7. 构建与执行命令

### 7.1 本地开发执行

- **Headed 单条调试执行**：通过 `UnityUIFlow > Test Runner` Editor 窗口选中单个用例，在 Details 面板使用 Run Mode = Step 进行步进调试。
- **Headed 批量执行**：`UnityUIFlow > Test Runner`，选择 YAML 目录批量运行。
- **C# 单元/集成测试**：Unity Test Runner → PlayMode / EditorMode 标签 → 仅 Editor Mode 可用。

### 7.2 CI / 命令行执行

项目仅通过 Unity 原生 `-executeMethod` 驱动，无 Makefile、Docker 或外部构建脚本。

**入口类**：`UnityUIFlow.UnityUIFlowCliEntry`
**入口方法**：`RunAllFromCommandLine`

示例命令（来自 `.github/workflows/unity-uiflow-sample.yml`）：

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.6.0a2\Editor\Unity.exe" `
  -projectPath $PWD `
  -quit `
  -executeMethod UnityUIFlow.UnityUIFlowCliEntry.RunAllFromCommandLine `
  -unityUIFlow.headed false `
  -unityUIFlow.reportPath ./Reports `
  -unityUIFlow.screenshotOnFailure true `
  -unityUIFlow.testFilter *
```

**注意**：CLI 明确禁止 `-batchmode`，必须使用带窗口的编辑器模式执行测试（即使 `headed=false`，也不能加 `-batchmode`）。

### 7.3 配置优先级

运行时配置按以下优先级合并（高覆盖低）：
1. CLI 参数（`-unityUIFlow.*`）
2. 环境变量（如 `UNITY_UI_FLOW_HEADED`）
3. 配置文件（默认 `.unityuiflow.json`，CI 使用 `ci/unity-uiflow.config.json`）
4. ProjectSettings（`UnityUIFlowSettings.asset`）
5. 代码硬编码默认值

### 7.4 退出码

| 码值 | 含义 |
|------|------|
| `0` | 全部通过 |
| `1` | 存在测试失败 |
| `2` | 执行错误或异常 |

---

## 8. CI/CD 与自动化

### 8.1 GitHub Actions

- **唯一工作流**：`.github/workflows/unity-uiflow-sample.yml`
- **触发方式**：`workflow_dispatch`（仅手动触发）
- **运行环境**：`windows-latest`
- **产物**：`./Reports` 目录（含 `.md`、`.json`、失败截图）通过 `actions/upload-artifact@v4` 上传

### 8.2 报告目录

所有执行输出默认落入 `./Reports/`（或传入的 `reportPath`）。**报告根目录下仅保留两个 Markdown 汇总文件**，其余文件全部归入子目录：

```
Reports/
├── full_reports.md              ← Suite 执行后生成（全量汇总：结果、耗时、起止时间）
├── single_reports.md            ← 单文件执行后生成（单个用例详情）
├── Cases/
│   ├── {caseName}.md            ← 单用例 Markdown 报告
│   ├── {caseName}.json          ← 单用例 JSON 报告
│   └── suite-report.json        ← Suite JSON 汇总
├── Screenshots/
│   └── {caseName}-{step}-{tag}-{timestamp}.png
└── Artifacts/
    └── artifacts.json           ← CLI 产物清单
```

**规则**：
- `RunFileAsync`（单文件）→ 生成 `single_reports.md` + `Cases/{caseName}.md+json`
- `RunSuiteAsync`（Suite）→ 生成 `full_reports.md` + `Cases/` 下各用例报告
- MCP `unity_editor_e2e_run`（单文件）→ `single_reports.md`
- MCP `unity_uiflow_run_batch`（多文件）→ `full_reports.md`
- 旧的 `suite-report.md` 已更名为 `full_reports.md`，旧的 `{caseName}.md` 已移至 `Cases/` 子目录

| 子目录 | 来源 |
|--------|------|
| `Examples/` | 示例用例执行 |
| `HeadedAll/` | Headed 模式全量套件 |
| `McpQuickTest/`、`McpRegression/`、`McpUiFlowFile/` 等 | MCP 驱动执行 |

---

## 9. Unity 编译触发约定（强制）

当任何 C# 脚本（`.cs`）被创建或修改，且需要 Unity 重新编译时，**必须通过 MCP 服务器触发编译**。严禁使用 `SendKeys`、`AppActivate`、手动删除 `Library/ScriptAssemblies/*.dll` 或其他投机手段。

### 为什么

- Unity Editor 通过 WebSocket 桥 `UnityPilotBridge` 连接到 `unityuiflow-mcp` 服务器（WS 端口 `8765` / HTTP `8011`）。
- `SendKeys` 不可靠（需要窗口焦点、存在竞态条件、超时不可控）。
- 删除 DLL 不会通知 AssetDatabase；Unity 可能忽略缺失的程序集，直到显式刷新。

### 正确流程

1. 确认 MCP 端点可达（如 `http://127.0.0.1:8011/mcp`）。
2. 通过 MCP 调用 `unity_compile` 工具。
3. 轮询目标程序集 `Library/ScriptAssemblies/<AssemblyName>.dll` 的 `LastWriteTime`，确保其晚于源文件修改时间（或使用 `unity_compile_status` / `unity_compile_errors`）。
4. 确认编译成功后，再继续后续操作。

### 示例（Python）

```python
import asyncio
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client

async def compile_unity():
    async with streamable_http_client(
        'http://127.0.0.1:8011/mcp',
        timeout=10,
        sse_read_timeout=1800,
        terminate_on_close=True,
    ) as (read, write, _):
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.call_tool('unity_compile', {}, read_timeout_seconds=None)
            return result.content[0].text
```

### 若 Unity 未连接

如果 `unity_compile` 返回 `UNITY_NOT_CONNECTED`，应调用 `unity_open_editor` 或等待 Editor 重新连接。**禁止回退到 `SendKeys`。**

### 业务状态卡死（EDITOR_BUSY）的轻量恢复

当 MCP 调用返回 `EDITOR_BUSY: A UnityUIFlow execution is already running`，且对应执行已明显超时（超过 5 分钟无进展，或外部脚本已退出但 Unity 内部仍报告 `running`）时，按以下优先级恢复：

1. **首选：`unity_uiflow_force_reset`**（Unity 侧已支持）
   - **触发条件**：MCP 调用返回 `EDITOR_BUSY`，且当前执行已持续 **≥ 60 秒** 无进展；或外部脚本/轮询已因超时而退出，但 Unity 内部仍报告 `running`。
   - **操作**：立即调用 `unity_uiflow_force_reset`，无需等待更久。
   - **效果**：毫秒级返回，强制 `Dispose` 当前的 `ExecutionContext`/`RuntimeController`，关闭测试窗口，清空 `_isRunning` 锁，并将所有进行中的执行标记为 `aborted`。不触发脚本重载，不影响其他编辑状态。
   - **风险**：若执行卡在某个 Unity 同步阻塞 API 上，`force_reset` 后该后台任务仍可能在进程中残留，直到 Unity 下次脚本重载；但 `EDITOR_BUSY` 锁已被释放，新的测试可以立即开始。

2. **备选：`unity_compile` 强制编译**
   - **触发条件**：在调用 `unity_uiflow_force_reset` 后 **5 秒内**，再次尝试测试仍返回 `EDITOR_BUSY`；或 `force_reset` 本身调用失败/超时，说明锁已深埋或主线程也被污染。
   - **操作**：调用 `unity_compile`。
   - **效果**：触发 AppDomain Reload，彻底重置所有单例状态，通常 3–15 秒完成（取决于项目脚本量）。会中断当前编辑上下文（所有静态状态丢失）。

**禁止使用 `taskkill`、重启 Unity 或任何进程级操作作为恢复手段。** 现有 `force_reset` + `unity_compile` 的组合已能覆盖全部 `EDITOR_BUSY` 场景，无需破坏编辑器进程。

**禁止**在 `EDITOR_BUSY` 状态下盲目等待或重复调用测试工具。

---

## 10. 安全与通用约束

- **优先使用 MCP 工具调用** 与 Unity 交互，避免 OS 级 GUI 自动化。
- **保持最小变更原则**：只改与目标相关的代码，不要大规模重构。
- **遵循现有代码风格**：C# 使用现有缩进与命名习惯。
- **非平凡变更后必须跑回归**：通过 MCP 的 `unity_uiflow_run_file` 或 `unity_uiflow_run_suite` 执行回归验证。
- **不要编造文件路径或类名**：所有路径和类型名必须与代码中真实存在的一致。
- **git 操作谨慎**：除非用户明确请求，否则不要执行 `git commit`、`git push`、`git reset`、`git rebase`。

---

## 11. 当前已知自动化边界

以下能力尚未实现，文档与代码保持一致，不应写成“已全面支持”：

- `ObjectField` 的 Object Picker 弹窗与 DragAndDrop 不支持。
- `CurveField` / `GradientField` 的独立编辑器浮窗不支持。
- `ToolbarPopupSearchField` 的弹出结果列表不支持。
- `ToolbarBreadcrumbs` 没有统一的“按 label / index 导航”封装动作。
- `PropertyField` / `InspectorElement` 不提供对自身的统一语义赋值，只能通过已生成的后代控件自动化。
- `IMGUIContainer`、IME、系统剪贴板、多窗口协同、像素级视觉 diff 不在 V1 边界内。

---

## 12. 快速参考链接

| 路径 | 内容 |
|------|------|
| `docs/01-UnityUIFlow-UXML-USS自动化开发规范.md` | UXML/USS 详细规范 |
| `docs/02-UnityUIFlow-新页面接入最小模板.md` | 最小页面模板与复制流程 |
| `docs/03-UnityUIFlow-Agent-MCP测试强制规范.md` | Agent MCP 测试强制规范全文 |
| `cocs/00-Overview.md` | 需求文档总览与模块清单 |
| `cocs/00-API速查与最佳实践.md` | 38 个动作、选择器、CLI 速查 |
| `.unityuiflow.json` | 本地开发配置（`headed: true`） |
| `ci/unity-uiflow.config.json` | CI 配置（`headed: false`） |
| `.vscode/mcp.json` | MCP 服务器启动配置 |
