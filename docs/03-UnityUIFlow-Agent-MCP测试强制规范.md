# UnityUIFlow Agent MCP 测试强制规范

## 1. 目标

本文档是给 Agent 使用的强制执行规范。

目的只有一个：Agent 在本项目中执行 YAML 自动化测试时，必须通过 MCP 服务器，并且必须使用 Headed 模式。

这不是建议，而是硬性要求。

## 2. 适用范围

本文档适用于以下场景：

- Agent 验证 YAML 用例
- Agent 调试 YAML 用例失败
- Agent 开发新页面后补充或修改 YAML 测试
- Agent 修复自动化测试相关 Bug
- Agent 执行基于 UnityUIFlow 的 E2E 验收

本文档不限制 Agent 修改代码、修复 Bug、开发功能本身。

本文档只强制约束“测试执行方式”。

## 3. 硬性规则

Agent 必须遵守以下规则：

1. **YAML 测试只能通过 MCP 服务器执行。**
2. **YAML 测试必须使用 Headed 模式。**
3. **没有可用 MCP 服务器时，禁止运行 YAML 测试。**
4. **不能接管当前 MCP 服务器时，禁止假装测试已执行。**
5. **禁止用 CLI、Unity Test Runner、临时脚本、手工点击或其他替代方式冒充 YAML MCP 测试结果。**
6. **Agent 可以在没有 MCP 的情况下修改代码、修复 Bug、实现需求，但不能声称已完成 YAML 测试验证。**
7. **凡是输出“已验证 YAML 测试通过”这类结论，前提必须是 MCP 工具真实执行成功。**

## 4. Headed 模式要求

执行 YAML 测试前，Agent 必须确认项目根目录存在 [`.unityuiflow.json`](d:/UnityUIFlow/.unityuiflow.json)，且至少满足以下要求：

```json
{
  "headed": true,
  "reportPath": "./Reports",
  "screenshotOnFailure": true,
  "defaultTimeoutMs": 10000,
  "customActionAssemblies": [
    "UnityUIFlow.Tests"
  ]
}
```

其中：

- `"headed": true` 是强制项
- 若 `headed` 不是 `true`，Agent 不得执行 YAML 测试
- Agent 发现配置不满足要求时，应先修正配置，再继续后续测试流程

## 5. MCP 服务器强制策略

本项目的 MCP 服务器以 [`.vscode/mcp.json`](d:/UnityUIFlow/.vscode/mcp.json) 为准。

当前约定的服务器为 `unitypilot`，启动方式为 `stdio`。

Agent 必须按以下顺序处理：

1. 先检查当前是否已经存在可用 MCP 服务器。
2. 对 `stdio` 型 MCP，不能只检查进程存在与否，必须确认“当前执行环境已经接管，并且可以直接调用 MCP tool”。
3. 如果当前环境已经接管该 MCP 服务器，且 MCP tool 可以直接使用，则直接复用，不得重复启动。
4. 如果 MCP 服务器未启动，或者已有进程但当前环境不能接管、不能直接调用，则必须关闭旧 MCP 进程。
5. 关闭旧进程后，按 [`.vscode/mcp.json`](d:/UnityUIFlow/.vscode/mcp.json) 重新启动 `unitypilot` MCP 服务器。
6. 新启动的 MCP 服务器应保持后台常驻运行，除非用户明确要求关闭，否则不要在测试结束后自动停止。

## 6. MCP 工具调用与可用性检测

Agent 必须显式检测“工具是否可调用”，不能只看进程、端口、日志或配置文件。

推荐优先检查以下 MCP 工具是否已在当前环境中真实暴露并可调用：

- `unity_mcp_status`
- `unity_editor_e2e_run`

在部分宿主环境中，工具名可能带有前缀，例如：

- `mcp_unitypilot_unity_mcp_status`
- `mcp_unitypilot_unity_editor_e2e_run`

判定规则如下：

1. 若当前环境中未暴露 `unitypilot` 相关 MCP tool，则视为当前环境**不可用 MCP**。
2. 若工具名存在，但调用失败、超时或无法连接 Unity，也视为当前环境**不可用 MCP**。
3. 只有在当前环境中真实调用成功后，才可以认定“已接管 MCP 且可执行 YAML 测试”。
4. 文档中的工具名示例用于帮助识别和调用，不要求所有宿主都使用完全相同的最终前缀；以当前会话实际暴露的工具名为准。

推荐检测顺序：

1. 先检查 `unity_mcp_status` 或其宿主映射后的等价工具是否存在。
2. 调用该工具，确认 MCP 进程、当前工作目录、Unity 连接状态、编译状态正常。
3. 再调用 `unity_editor_e2e_run` 或其宿主映射后的等价工具执行 YAML 测试。

## 7. 标准 YAML E2E 工具示例

推荐使用的 YAML 执行工具调用示例如下：

```text
工具调用: mcp_unitypilot_unity_editor_e2e_run
参数:
  - specPath: Assets/Examples/Yaml/01-basic-login.yaml
  - artifactDir: D:\UnityUIFlow\artifacts
  - exportZip: true
  - stopOnFirstFailure: true
  - webhookOnFailure: true
```

若当前宿主不带 `mcp_unitypilot_` 前缀，则等价工具通常为：

```text
工具调用: unity_editor_e2e_run
参数:
  - specPath: Assets/Examples/Yaml/01-basic-login.yaml
  - artifactDir: D:\UnityUIFlow\artifacts
  - exportZip: true
  - stopOnFirstFailure: true
  - webhookOnFailure: true
```

推荐先执行的状态检查工具示例如下：

```text
工具调用: mcp_unitypilot_unity_mcp_status
```

或：

```text
工具调用: unity_mcp_status
```

Agent 应基于当前会话实际暴露的工具名选择等价调用方式。

## 8. 无 MCP 时的行为边界

当出现以下任一情况时，视为“没有可用 MCP 服务器”：

- MCP 服务器进程不存在
- MCP 服务器虽然存在，但当前环境无法接管
- MCP tool 无法直接调用
- MCP 与 Unity Editor 未建立有效连接
- Headed 模式不满足要求

此时 Agent 的允许行为只有：

- 阅读代码
- 修改代码
- 修复 Bug
- 开发需求
- 调整 YAML、UXML、USS、C# 实现
- 准备后续测试所需配置

此时 Agent 的禁止行为包括：

- 运行 YAML 测试并将结果作为正式验证结论
- 声称“测试已通过”
- 用其他方式替代 MCP 测试后给出等价结论

正确表述应为：

- “代码已修改，但 YAML 测试尚未执行，因为当前没有可用 MCP 服务器。”
- “当前仅完成实现，未完成 MCP 验证。”

## 9. 标准执行流程

Agent 执行 YAML 测试时，必须遵循以下流程：

1. 确认 [`.unityuiflow.json`](d:/UnityUIFlow/.unityuiflow.json) 中 `headed` 为 `true`。
2. 检查当前 MCP 服务器是否存在且当前环境可直接接管。
3. 检测 `unity_mcp_status` / `mcp_unitypilot_unity_mcp_status` 等状态工具是否真实可调用。
4. 若可接管，直接复用现有 MCP 服务器。
5. 若不可接管，先关闭旧 MCP 进程。
6. 按 [`.vscode/mcp.json`](d:/UnityUIFlow/.vscode/mcp.json) 在后台启动新的 `unitypilot` MCP 服务器。
7. 再次确认 MCP tool 已可调用，且 Unity Editor 已连接。
8. 通过 `unity_editor_e2e_run` / `mcp_unitypilot_unity_editor_e2e_run` 执行目标 YAML 用例。
9. 基于 MCP 返回结果、产物、截图、日志给出测试结论。

## 10. 测试结论输出规则

Agent 输出测试结论时，必须区分以下三种状态：

### 10.1 已通过 MCP 完成测试

只有在 MCP 工具真实执行成功后，才能输出：

- “已通过 MCP 执行 YAML 测试”
- “该 YAML 用例已验证通过”
- “该问题已通过 MCP 回归验证”

### 10.2 已完成实现，但未完成测试

当代码已改完，但 MCP 条件不满足时，应输出：

- “代码修改已完成”
- “尚未通过 MCP 执行 YAML 测试”
- “当前缺少可用 MCP 服务器或当前环境无法接管，因此不能给出正式 YAML 验证结论”

### 10.3 MCP 启动或接管失败

当 MCP 服务器无法接管或启动失败时，应明确说明失败点，例如：

- 无法接管已有 `stdio` MCP
- 旧进程关闭失败
- 新 MCP 进程启动失败
- Unity 未连接到 MCP

不得把这类失败写成“测试失败”与“代码失败”混为一谈。

## 11. 明确禁止的错误做法

以下做法一律禁止：

- 看到 MCP 进程存在，就默认当前环境一定可用
- 没有先检查 `unity_mcp_status` 或等价状态工具，就直接认定 MCP 可用
- 只做代码静态检查，就宣称 YAML 用例已验证
- 改完代码后直接用 CLI 跑 YAML，再把结果当作 MCP 测试
- 在 `headed=false` 的情况下继续跑 YAML 验收
- 启动了新的 MCP 后，在任务结束时私自关闭后台 MCP
- 没有真实调用 MCP tool，却在结论里写“已测试通过”

## 12. 对 Agent 的最终执行要求

Agent 在本项目中的工作原则如下：

- 开发可以先行
- 修 Bug 可以先行
- 改 YAML 可以先行
- 但只要进入“测试验证”阶段，就必须使用 MCP
- 只要执行 YAML 测试，就必须使用 Headed 模式
- 没有 MCP，就停止测试，不得伪造测试闭环

一句话总结：

**本项目中，YAML 测试 = MCP 服务器 + Headed 模式。缺一不可。**
