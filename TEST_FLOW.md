# UnityUIFlow 测试流程指南

## 📌 项目概述

**UnityUIFlow** 是一个 Unity Editor UIToolkit 自动化测试框架，支持 YAML 声明式脚本和 C# 代码两种方式驱动 EditorWindow 界面流程测试。该框架提供 16 个内置动作、数据驱动、条件循环、可视化 Headed 模式等完整功能。

---

## 🎯 测试执行方式

根据项目配置和代码结构，UnityUIFlow 支持以下 **4 种测试执行方式**：

### 1️⃣ **YAML 驱动测试** (推荐用于快速验证)

#### 文件位置
- **测试文件目录**: `Assets/Examples/Yaml/`
- **测试文件数量**: 19 个示例 YAML 文件
- **命名规范**: `01-basic-login.yaml`, `02-selectors-and-assertions.yaml` 等

#### 执行方式 (Editor UI)
1. 打开 Unity Editor
2. 导航到菜单: **UnityUIFlow > Samples > [示例窗口]**
3. 点击对应的示例窗口打开 EditorWindow
4. 框架自动加载并执行 YAML 测试文件

#### 支持的 YAML 特性
| 特性 | 说明 |
|------|------|
| **内置动作** | click、type_text、assert、screenshot、wait、double_click、press_key、hover、drag、scroll 等 16 种 |
| **数据驱动** | CSV、JSON、内联数据三种数据源 |
| **条件控制** | if 条件语句、repeat_while 循环 |
| **选择器** | UIToolkit 选择器、自定义选择器 |
| **断言** | HaveText、HaveClass、IsVisible、IsEnabled 等 |

#### 示例 YAML 测试文件
```yaml
# Assets/Examples/Yaml/01-basic-login.yaml
steps:
  - action: click
    selector: "#username"
  - action: type_text
    selector: "#username"
    text: "admin"
  - action: click
    selector: "#password"
  - action: type_text
    selector: "#password"
    text: "password123"
  - action: click
    selector: "#login-btn"
  - action: assert
    selector: ".dashboard"
    assertion: IsVisible
```

---

### 2️⃣ **C# 单元测试** (Fixture 方式)

#### 文件位置
- **测试文件目录**: `Assets/Tests/`
- **测试程序集**: `UnityUIFlow.Tests.asmdef`
- **关键测试文件**:
  - `UnityUIFlow.ParsingAndPlanningTests.cs` - YAML 解析测试
  - `UnityUIFlow.LocatorsAndActionsTests.cs` - 动作和定位器测试
  - `UnityUIFlow.ExamplesAcceptanceTests.cs` - 接收度测试
  - `UnityUIFlow.ExecutionReportingCliTests.cs` - 报告和 CLI 测试

#### Fixture 继承方式
```csharp
using UnityUIFlow;
using UnityEngine.UIElements;

// 继承 UnityUIFlowFixture<TWindow>
public class MyWindowTests : UnityUIFlowFixture<ExampleBasicLoginWindow>
{
    [Test]
    public async Task TestLoginFlow()
    {
        // 内置支持通过 Fixture 驱动测试
        await ExecuteYamlTest("Assets/Examples/Yaml/01-basic-login.yaml");
    }
}
```

#### 执行方式 (Unity Test Framework)
1. 打开 **Window > General > Test Runner**
2. 切换到 **EditMode** 或 **PlayMode**
3. 选择测试用例 (如 `UnityUIFlow.Tests`)
4. 点击 **Run** 执行

#### 测试类型
| 测试类型 | 文件 | 用途 |
|----------|------|------|
| **Parsing** | ParsingAndPlanningTests.cs | 验证 YAML 解析和执行计划构建 |
| **Actions** | LocatorsAndActionsTests.cs | 验证动作执行和定位器 |
| **Acceptance** | ExamplesAcceptanceTests.cs | 集成测试 - 端到端验证 |
| **Reporting** | ExecutionReportingCliTests.cs | 验证报告生成和 CLI 功能 |
| **Headed** | HeadedTests.cs | 验证可视化交互模式 |

---

### 3️⃣ **CLI 命令行执行** (持续集成)

#### 执行命令
```bash
# 运行单个 YAML 测试文件
$UNITY_PATH -projectPath . \
  -executeMethod UnityUIFlow.Cli.RunYamlTest \
  Assets/Examples/Yaml/01-basic-login.yaml

# 运行所有 YAML 测试 (带报告输出)
$UNITY_PATH -projectPath . \
  -executeMethod UnityUIFlow.Cli.RunAllYamlTests \
  --reportPath ./Reports \
  --format json
```

#### 输出格式
- **Markdown 报告**: `./Reports/report.md`
- **JSON 报告**: `./Reports/report.json`
- **截图**: `./Reports/screenshots/` (失败时)

#### CI 配置文件
- **项目配置**: `.unityuiflow.json`
- **CI 配置**: `ci/unity-uiflow.config.json`

---

### 4️⃣ **Headed 可视化模式** (交互式调试)

#### 启用方式
编辑 `.unityuiflow.json`:
```json
{
  "headed": true,
  "reportPath": "./Reports",
  "screenshotOnFailure": true,
  "defaultTimeoutMs": 10000
}
```

#### 功能特性
- **实时可视化**: Editor 中高亮目标元素
- **伪步进调试**: 支持逐步执行测试步骤
- **交互式控制**: 暂停、恢复、跳过测试步骤
- **实时反馈**: 即时看到元素查找和动作执行结果

---

## �️ MCP 服务器执行测试（推荐方式）

**MCP (Model Context Protocol) 服务器** 是 UnityUIFlow 的核心执行引擎，提供远程测试执行、实时反馈和可视化调试能力。

### 🌐 MCP 服务器信息

当前 UnityUIFlow MCP 服务器配置：

```json
{
  "label": "UnityUIFlow",
  "host": "127.0.0.1",
  "port": 8767,
  "status": "connected",
  "serverReady": true,
  "sessionId": "2a928d0b29fc475cb0f3383ab37da326",
  "projectPath": "D:\\UnityUIFlow",
  "unityVersion": "6000.6.0a2",
  "platform": "windows"
}
```

**关键参数说明：**
| 参数 | 值 | 说明 |
|------|-----|------|
| `host` | 127.0.0.1 | 本地主机（编辑器所在机器） |
| `port` | 8767 | MCP 服务通信端口 |
| `status` | connected | 连接状态 ✅ |
| `serverReady` | true | 服务器准备就绪 ✅ |
| `projectPath` | D:\UnityUIFlow | Unity 项目路径 |
| `unityVersion` | 6000.6.0a2 | Unity 编辑器版本 |

### 完整的 MCP 测试执行流程

#### **步骤 1: 启用 Headed 模式并配置 MCP**

编辑项目根目录的 `.unityuiflow.json` 配置文件：

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

**配置说明：**
- `"headed": true` - **启用可视化模式**（关键！）
  - 编辑器中可见目标元素高亮
  - 支持交互式调试和步骤控制
  - 实时显示执行进度
- `"defaultTimeoutMs": 10000` - 默认超时 10 秒（可根据需要调整）

#### **执行前补充：stdio MCP 服务器接管与后台保活策略**

当 MCP 服务器采用 `.vscode/mcp.json` 中的 `stdio` 方式启动时，执行测试前建议按以下规则处理：

- 优先检查当前是否已经存在可用的 `unitypilot` MCP 服务器。
- 对 `stdio` 型 MCP，不要只看“进程是否存在”，而要以“当前执行环境是否已经成功接管并能直接调用 MCP tool”为准。
- 如果当前环境已经接管该 MCP 服务器，并且可以直接调用相关工具，则**直接复用现有 MCP 服务器**，不要重复启动。
- 如果 MCP 服务器未启动，或者虽然已有进程但当前环境**无法接管 / 无法直接调用**，则应先关闭旧的 MCP 进程，避免保留无效占用或端口冲突。
- 关闭旧进程后，按 [`.vscode/mcp.json`](d:/UnityUIFlow/.vscode/mcp.json) 的配置重新启动 `unitypilot` MCP 服务器。
- 新启动的 MCP 服务器应保持**后台常驻运行**，后续测试直接复用；除非明确要求关闭，否则不要在每次测试后自动停止。

**推荐决策顺序：**

1. 检查当前 MCP 是否已存在且当前环境可直接使用。
2. 若可直接使用，复用现有服务器并继续执行测试。
3. 若不可使用，关闭旧 MCP 进程。
4. 重新在后台启动 MCP 服务器，并保持其持续运行。
5. 确认 MCP 工具可调用后，再执行 YAML E2E 测试。

#### **步骤 2: 通过 MCP 服务器指定 YAML 测试并启动**

使用 MCP 工具运行 E2E 测试：

**示例 1: 运行基础登录测试**
```
工具调用: mcp_unitypilot_unity_editor_e2e_run
参数:
  - specPath: Assets/Examples/Yaml/01-basic-login.yaml
  - artifactDir: D:\UnityUIFlow\artifacts
  - exportZip: true
  - stopOnFirstFailure: true
  - webhookOnFailure: true
```

**对应的测试文件内容** (`Assets/Examples/Yaml/01-basic-login.yaml`):
```yaml
name: Example Basic Login
description: 基础登录流程测试 - 包含文本输入、点击、断言和截图
fixture:
  host_window:
    type: UnityUIFlow.Examples.ExampleBasicLoginWindow
    reopen_if_open: true
steps:
  - name: 填充用户名
    action: type_text_fast
    selector: "#username-input"
    value: "alice"
  - name: 填充密码
    action: type_text_fast
    selector: "#password-input"
    value: "secret"
  - name: 提交登录
    action: click
    selector: "#login-button"
  - name: 验证欢迎信息
    action: assert_text_contains
    selector: "#status-label"
    expected: "alice"
  - name: 保存截图
    action: screenshot
    tag: "basic-login"
```

**Headed 模式执行流程可视化：**
```
时间轴 │ 执行步骤                          │ 编辑器内可视化
────────────────────────────────────────────────────────────
T0    │ 打开 ExampleBasicLoginWindow      │ 窗口弹出
      │ 加载测试计划                       │ 
────────────────────────────────────────────────────────────
T1    │ 【步骤 1】填充用户名               │ 
      │ action: type_text_fast            │ ✨ 高亮 #username-input
      │ selector: "#username-input"        │ 光标在输入框
      │ value: "alice"                     │ 输入: "alice"
      │ → ✅ PASS                          │
────────────────────────────────────────────────────────────
T2    │ 【步骤 2】填充密码                 │
      │ action: type_text_fast            │ ✨ 高亮 #password-input
      │ selector: "#password-input"        │ 光标在输入框
      │ value: "secret"                    │ 输入: "secret"
      │ → ✅ PASS                          │
────────────────────────────────────────────────────────────
T3    │ 【步骤 3】提交登录                 │
      │ action: click                     │ ✨ 高亮 #login-button
      │ selector: "#login-button"          │ 模拟点击事件
      │ → ✅ PASS                          │ 窗口响应登录请求
────────────────────────────────────────────────────────────
T4    │ 【步骤 4】验证欢迎信息             │
      │ action: assert_text_contains      │ ✨ 高亮 #status-label
      │ selector: "#status-label"          │ 检查文本: "alice"
      │ expected: "alice"                  │ 验证通过！
      │ → ✅ PASS                          │
────────────────────────────────────────────────────────────
T5    │ 【步骤 5】保存截图                │
      │ action: screenshot                │ 📸 截图已保存
      │ tag: "basic-login"                 │ Reports/basic-login.png
      │ → ✅ PASS                          │
────────────────────────────────────────────────────────────
结束    │ 测试完成                         │ ✅ 所有步骤通过
      │ 报告已生成                        │ Reports/report.md
```

#### **步骤 3: 实时监控和交互**

在 Headed 模式下，测试执行时可以：

- **🔍 观察高亮效果**：
  - 每个步骤执行时，目标元素会高亮显示（边框、背景变色）
  - 便于验证选择器是否正确定位

- ⏸️ **暂停/恢复执行**（伪步进调试）：
  - 在编辑器中设置断点
  - 逐步单步执行
  - 观察每一步的实际效果

- 📸 **截图证据**：
  - 每个关键步骤自动截图
  - 失败时强制截图保存
  - 位置：`Reports/screenshots/`

#### **步骤 4: 选择器和断言的动态验证**

**示例 2：选择器和断言测试** (`Assets/Examples/Yaml/02-selectors-and-assertions.yaml`)
```yaml
name: Example Selectors And Assertions
description: 验证多种选择器形式和断言方法
fixture:
  host_window:
    type: UnityUIFlow.Examples.ExampleSelectorsWindow
    reopen_if_open: true
steps:
  - name: 匹配子元素选择器
    action: assert_visible
    selector: "#selector-list > .selector-item:first-child"
    # Headed 模式：✨ 高亮第一个子元素

  - name: 匹配属性选择器
    action: assert_visible
    selector: "[tooltip=Inspect]"
    # Headed 模式：✨ 高亮 tooltip="Inspect" 的元素

  - name: 匹配数据属性选择器
    action: assert_visible
    selector: "[data-role=primary]"
    # Headed 模式：✨ 高亮 data-role="primary" 的元素

  - name: 验证按钮属性
    action: assert_property
    selector: "Button"
    property: "tooltip"
    expected: "Inspect"
    # Headed 模式：✨ 检查 Button 的 tooltip 属性

  - name: 点击检查按钮
    action: click
    selector: "#inspect-button"
    # Headed 模式：✨ 高亮并模拟点击 #inspect-button

  - name: 最终状态验证
    action: assert_text
    selector: "#selector-status"
    expected: "Inspect ready"
    # Headed 模式：✨ 验证文本为 "Inspect ready"
```

#### **步骤 5: 等待和动态元素测试**

**示例 3：等待元素加载** (`Assets/Examples/Yaml/03-wait-for-element.yaml`)
```yaml
name: Example Wait For Element
description: 测试异步加载元素和等待超时
fixture:
  host_window:
    type: UnityUIFlow.Examples.ExampleWaitForElementWindow
    reopen_if_open: true
steps:
  - name: 启动延迟显示
    action: click
    selector: "#start-button"
    # Headed 模式：点击后开始异步加载

  - name: 等待消息元素出现
    action: wait_for_element
    selector: "#delayed-message"
    timeout: "2s"
    # Headed 模式：⏳ 等待最多 2 秒，轮询 #delayed-message
    # → T0-0.5s: 元素不可见，继续等待
    # → T0.5-1.0s: 元素不可见，继续等待
    # → T1.0-1.5s: 元素不可见，继续等待
    # → T1.5-2.0s: ✨ 元素出现！高亮显示

  - name: 最终断言
    action: assert_text
    selector: "#delayed-message"
    expected: "Ready"
    # 验证文本内容为 "Ready"
```

**Headed 模式中的等待可视化：**
```
轮询周期 │ 元素状态           │ 编辑器反馈
─────────────────────────────────────────
0ms     │ ❌ 元素不可见      │ "正在等待..."
250ms   │ ❌ 元素不可见      │ "正在等待..." (旋转进度)
500ms   │ ❌ 元素不可见      │ "正在等待..." (旋转进度)
750ms   │ ❌ 元素不可见      │ "正在等待..." (旋转进度)
1000ms  │ ❌ 元素不可见      │ "正在等待..." (旋转进度)
1250ms  │ ❌ 元素不可见      │ "正在等待..." (旋转进度)
1500ms  │ ✅ 元素出现！      │ ✨ 高亮显示，通过！
```

### 🎬 MCP 服务器测试完整示例

**完整命令流：**

```
1️⃣  启用 Headed 配置
    Action: 编辑 .unityuiflow.json
    ├─ "headed": true
    └─ "defaultTimeoutMs": 10000

2️⃣  通过 MCP 启动 E2E 测试
    Tool: mcp_unitypilot_unity_editor_e2e_run
    Parameters:
      specPath: Assets/Examples/Yaml/01-basic-login.yaml
      artifactDir: D:\UnityUIFlow\artifacts
      exportZip: true
      stopOnFirstFailure: true

3️⃣  实时监控执行过程
    ✨ 编辑器中观察：
      - 元素高亮变化
      - 步骤执行顺序
      - 文本输入和点击

4️⃣  测试完成收集报告
    Output:
      ├─ Report: Reports/report.json
      ├─ Report: Reports/report.md
      ├─ Screenshots: Reports/screenshots/*.png
      └─ Artifacts: artifacts/e2e-bundle.zip (如果启用)

5️⃣  分析失败原因（如有）
    Headed 模式优势：
      ✅ 能直观看到选择器命中的元素
      ✅ 能观察动作执行过程（如文本输入）
      ✅ 能诊断超时或断言失败的原因
      ✅ 支持伪步进调试快速定位问题
```

### 📊 Headed 模式 vs 无头模式对比

| 场景 | 无头模式 | Headed 模式 |
|------|---------|-----------|
| **执行速度** | ⚡ 快 (无 UI 绘制) | 🔄 慢 (有 UI 绘制) |
| **诊断能力** | 📋 报告为主 | 👁️ 可视化为主 |
| **选择器验证** | ⚠️ 难 (看不到高亮) | ✨ 易 (实时高亮) |
| **动作调试** | ⚠️ 难 (只有日志) | ✅ 易 (看到过程) |
| **超时问题** | ⚠️ 难诊断 | ✅ 易诊断 |
| **CI/CD 环境** | ✅ 推荐 | ❌ 不适用 (无 UI) |
| **开发调试** | ⚠️ 可用 | ✅ 强烈推荐 |
| **性能测试** | ✅ 准确 | ⚠️ 有绘制开销 |

**使用建议：**
- **开发阶段**：用 Headed 模式 + MCP 服务器快速调试
- **CI/CD 环线**：用无头模式 + CLI 命令自动化测试
- **失败排查**：切换到 Headed 模式查看具体问题

---

## �📋 测试配置与对应关系

项目包含两个层级的配置文件：

### `.unityuiflow.json` - 项目级配置
```json
{
  "headed": false,
  "reportPath": "./Reports",
  "screenshotOnFailure": true,
  "defaultTimeoutMs": 10000,
  "customActionAssemblies": ["UnityUIFlow.Tests"]
}
```

**配置选项说明**:
| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `headed` | 启用可视化模式 | false |
| `reportPath` | 测试报告输出路径 | ./Reports |
| `screenshotOnFailure` | 失败时截图 | true |
| `defaultTimeoutMs` | 默认超时时间(毫秒) | 10000 |
| `customActionAssemblies` | 自定义动作程序集列表 | [] |

### `ci/unity-uiflow.config.json` - CI 配置
用于持续集成环境的测试配置，通常包含：
- 测试文件路径范围
- 报告输出格式 (JSON/Markdown)
- CI 触发规则

---

## 🗂️ 测试文件组织结构

```
Assets/Examples/Yaml/
├── 01-basic-login.yaml                 ✅ 基础流程 - 登录
├── 02-selectors-and-assertions.yaml    ✅ UI 定位和断言
├── 03-wait-for-element.yaml            ✅ 元素等待
├── 04-conditional-and-loop.yaml        ✅ 条件和循环
├── 05-data-driven-csv.yaml             ✅ CSV 数据驱动
├── 06-custom-action-and-json.yaml      ✅ 自定义动作 + JSON
├── 07-double-click.yaml                ✅ 双击操作
├── 08-press-key.yaml                   ✅ 按键操作
├── 09-hover.yaml                       ✅ 悬停操作
├── 10-drag.yaml                        ✅ 拖拽操作
├── 11-scroll.yaml                      ✅ 滚动操作
├── 12-type-text.yaml                   ✅ 文本输入
├── 13-advanced-controls.yaml           ✅ 高级控件
├── 14-15-16-fields.yaml                ✅ 字段操作
├── 17-collections.yaml                 ✅ 集合操作
├── 18-layout-and-scroller.yaml         ✅ 布局和滚动条
├── 19-menus-and-commands.yaml          ✅ 菜单和命令
└── ...
```

---

## 🔧 程序集依赖关系

```
┌─────────────────────────┐
│   UnityUIFlow.Tests     │  ← 测试程序集 (C# 测试用例)
└────────────┬────────────┘
             │ references
             ▼
┌─────────────────────────┐
│     UnityUIFlow         │  ← 核心框架程序集 (解析、执行、报告)
└────────────┬────────────┘
             │ references
             ▼
┌─────────────────────────┐
│  External Dependencies  │
├─────────────────────────┤
│ • Unity Test Framework  │
│ • InputSystem           │
│ • UI TestFramework      │
│ • YamlDotNet.dll        │
│ • nunit.framework.dll   │
└─────────────────────────┘
```

---

## 🚀 测试执行流程 (完整步骤)

### **第 1 步: 检查项目环境** ✅
```
前置条件:
  ✓ Unity 6000.6.0a2 或更高版本
  ✓ Assets/UnityUIFlow/Editor 核心框架存在
  ✓ Assets/Examples/Yaml YAML 测试文件存在
  ✓ .unityuiflow.json 配置文件存在
  ✓ Packages/manifest.json 依赖已安装
```

### **第 2 步: 加载 YAML 测试文件** 📝
```
流程:
  1. 扫描 Assets/Examples/Yaml/ 目录
  2. 读取所有 *.yaml 文件
  3. 使用 YamlTestCaseParser 解析 YAML 结构
     - 验证 steps 节点
     - 验证 action 字段
     - 验证 selector 字段
     - 解析数据源 (data: CSV/JSON/inline)
  4. 构建 ExecutionPlan (执行计划)
     - 拆解各个测试步骤
     - 验证选择器语法
     - 绑定数据迭代
```

### **第 3 步: 初始化测试窗口** 🪟
```
流程:
  1. 加载示例 EditorWindow (如 ExampleBasicLoginWindow)
  2. 初始化 UIDocument 和 VisualElement 树
  3. 绑定示例窗口到 UnityUIFlowFixture
  4. 准备元素选择器上下文
```

### **第 4 步: 执行测试步骤** ⚡
```
流程 (逐步执行):
  For Each step in ExecutionPlan:
    1. 解析 selector (UIToolkit 选择器)
    2. 定位目标 VisualElement
    3. 验证元素状态:
       - IsVisible? IsEnabled? IsInViewport?
    4. 根据 action 类型执行:
       - click        → 模拟点击事件
       - type_text    → 输入文本
       - assert       → 验证断言
       - screenshot   → 截图
       - wait         → 等待条件
       - 其他...
    5. 记录执行结果:
       - ✅ PASS
       - ❌ FAIL (产生错误码)
       - ⏭️  SKIP (条件跳过)
```

### **第 5 步: 收集测试结果** 📊
```
流程:
  1. 汇总执行统计:
     - Total Steps
     - Passed Steps
     - Failed Steps
     - Skipped Steps
  2. 记录失败信息:
     - 错误码 (40+ 标准错误码)
     - 错误消息
     - 失败步骤号
  3. 如果启用 screenshotOnFailure:
     - 保存失败时的截图到 Reports/screenshots/
```

### **第 6 步: 生成测试报告** 📄
```
流程:
  1. 格式化报告数据
  2. 输出格式选项:
     - Markdown: ./Reports/report.md
     - JSON: ./Reports/report.json
  3. 包含内容:
     - 测试用例名称
     - 执行时间
     - 通过率
     - 失败详情
     - 截图链接 (如有)
```

### **第 7 步: 输出最终结果** ✨
```
成功场景:
  ✅ 所有测试通过
  → 退出代码: 0
  → 生成报告
  
失败场景:
  ❌ 部分/全部测试失败
  → 退出代码: 1
  → 生成报告 + 失败截图
  → 输出错误码和错误消息
```

---

## 🔍 核心类说明

### 1. **YamlTestCaseParser** (资源路径: 待定)
- **职责**: 解析 YAML 文件为结构化测试用例
- **输入**: `.yaml` 文件路径
- **输出**: `YamlTestCase` 对象 (包含 steps 列表)
- **关键方法**: `Parse()`, `ValidateSyntax()`

### 2. **ExecutionPlanBuilder** (资源路径: 待定)
- **职责**: 将 YamlTestCase 转换为可执行的 ExecutionPlan
- **输入**: `YamlTestCase`
- **输出**: `ExecutionPlan` (步骤序列)
- **关键处理**: 数据驱动展开、条件判断、循环构建

### 3. **ExecutionEngine** (资源路径: 待定)
- **职责**: 逐步执行 ExecutionPlan
- **输入**: `ExecutionPlan` + 目标窗口引用
- **输出**: `ExecutionResult` (执行结果)
- **关键方法**: `Execute()`, `HandleAction()`, `FindElement()`

### 4. **ActionFactory** (资源路径: 待定)
- **职责**: 根据 action 名称创建对应的动作实例
- **实现**: 工厂模式，支持 16+ 内置动作
- **扩展**: 通过 `customActionAssemblies` 配置加载自定义动作
- **接口**: 所有动作实现 `IAction` 接口

### 5. **ReportGenerator** (资源路径: 待定)
- **职责**: 生成 Markdown/JSON 格式测试报告
- **输入**: `ExecutionResult` 集合
- **输出**: 报告文件 (Markdown/JSON)
- **配置**: `reportPath`, `format`

### 6. **UnityUIFlowFixture<TWindow>** (资源路径: 待定)
- **职责**: C# 单元测试的基类，提供 YAML 测试驱动支持
- **泛型**: `TWindow` 为目标 EditorWindow 类型
- **关键方法**: `ExecuteYamlTest(string path)`, `Setup()`, `TearDown()`
- **框架**: 与 Unity Test Framework 无缝集成

---

## 📌 关键文件清单

| 文件路径 | 用途 |
|----------|------|
| [README.md](README.md) | 项目文档、YAML 语法、快速开始 |
| [.unityuiflow.json](.unityuiflow.json) | 项目配置 |
| [ci/unity-uiflow.config.json](ci/unity-uiflow.config.json) | CI 配置 |
| [Assets/UnityUIFlow/Editor/](Assets/UnityUIFlow/Editor/) | 核心框架代码 |
| [Assets/Examples/Yaml/](Assets/Examples/Yaml/) | 19 个示例 YAML 测试 |
| [Assets/Examples/Editor/](Assets/Examples/Editor/) | 示例 EditorWindow 实现 |
| [Assets/Tests/](Assets/Tests/) | C# 单元测试 |
| [Packages/manifest.json](Packages/manifest.json) | 项目依赖配置 |

---

## 🎓 Headed 模式测试场景示例

### 场景 1: 简单的UI交互和验证 ✅
**文件**: `Assets/Examples/Yaml/01-basic-login.yaml`

测试目标：输入用户名/密码 → 点击登录 → 验证欢迎信息

```yaml
name: Example Basic Login
steps:
  - name: 填充用户名
    action: type_text_fast
    selector: "#username-input"
    value: "alice"
    # ✨ Headed 模式：
    #   1. 定位 #username-input 元素
    #   2. 高亮显示（边框变色）
    #   3. 快速输入 "alice"
    #   4. 编辑器实时显示输入过程
```

**Headed 执行效果**：
```
┌─────────────────────────────────┐
│      登录窗口 (可视化)           │
├─────────────────────────────────┤
│ 用户名: [alice            ]  ✨  │ ← 高亮边框，实时显示输入
│ 密码:   [secret           ]      │
│ [登 录]                          │
│ 状态: alice 欢迎！              │
└─────────────────────────────────┘
```

---

### 场景 2: 选择器验证（关键用途！）✨
**文件**: `Assets/Examples/Yaml/02-selectors-and-assertions.yaml`

测试目标：验证多种 CSS 选择器是否正确命中元素

```yaml
name: Example Selectors And Assertions
steps:
  - name: 匹配子元素选择器
    action: assert_visible
    selector: "#selector-list > .selector-item:first-child"
    # ✨ Headed 优势：
    #   能看到 #selector-list 中的第一个 .selector-item 高亮
    #   快速验证选择器语法是否正确

  - name: 匹配属性选择器
    action: assert_visible
    selector: "[tooltip=Inspect]"
    # ✨ 高亮所有 tooltip="Inspect" 的元素
    # 避免选择错误的元素！

  - name: 匹配数据属性
    action: assert_visible
    selector: "[data-role=primary]"
    # ✨ 高亮所有 data-role="primary" 的元素
```

**Headed 模式的诊断能力**：
```
【选择器测试 - 无头模式】
❌ 断言失败：找不到 "[tooltip=Inspect]"
   原因不明，需要查看 HTML 结构

【选择器测试 - Headed 模式】
✅ 看到高亮效果，能立即识别：
   ✓ 选择器是否匹配正确元素
   ✓ 元素是否真的不可见（display:none）
   ✓ 元素是否被其他元素遮挡
   ✓ 选择器语法是否有误
```

---

### 场景 3: 异步加载和等待 ⏳
**文件**: `Assets/Examples/Yaml/03-wait-for-element.yaml`

测试目标：测试动态出现的元素

```yaml
name: Example Wait For Element
steps:
  - name: 启动延迟显示
    action: click
    selector: "#start-button"
    # ✨ 点击后，触发异步加载

  - name: 等待消息元素出现
    action: wait_for_element
    selector: "#delayed-message"
    timeout: "2s"
    # ✨ Headed 模式显示：
    #   ⏳ 等待进度条动画
    #   📍 轮询次数计数
    #   ✨ 元素出现时立即高亮

  - name: 验证最终状态
    action: assert_text
    selector: "#delayed-message"
    expected: "Ready"
    # 确认加载完成的文本内容
```

**实时等待过程可视化**：
```
时间    等待状态         控制台输出              编辑器可视
──────────────────────────────────────────────────────
0ms    [==          ] "轮询 1/20..."         ⏳ 进度条
250ms  [====        ] "轮询 2/20..."         ⏳ 进度条
500ms  [======      ] "轮询 3/20..."         ⏳ 进度条
750ms  [========    ] "轮询 4/20..."         ⏳ 进度条
1000ms [==========  ] "轮询 5/20..."         ⏳ 进度条
1500ms [============] "轮询 6/20..."         ⏳ 进度条
1750ms [✓ 找到！]   "元素已出现"            ✨ 元素高亮
2000ms [✅ 完成]    "步骤通过"              ✨ 保存截图
```

---

### 场景 4: 条件控制和循环 🔄
**文件**: `Assets/Examples/Yaml/04-conditional-and-loop.yaml`

测试目标：条件执行和等待循环完成

```yaml
name: Example Conditional And Loop
steps:
  - name: 如果保存按钮存在则点击
    action: click
    selector: "#save-button"
    if:
      exists: "#save-button"
    # ✨ Headed 模式：
    #   看到条件是否满足
    #   按钮是否真的存在/不存在

  - name: 等待 Toast 消息出现
    action: assert_visible
    selector: "#toast-message"
    timeout: "1s"
    # ✨ 高亮 Toast 消息

  - name: 循环等待 Toast 消失
    repeat_while:
      condition:
        exists: "#toast-message"
      max_iterations: 20
      steps:
        - name: 等待 50ms
          action: wait
          duration: "50ms"
    # ✨ Headed 模式显示：
    #   ├─ 循环迭代计数
    #   ├─ Toast 元素闪烁（每次检查）
    #   └─ 最终消失时变暗

  - name: 验证 Toast 已移除
    action: assert_not_visible
    selector: "#toast-message"
    timeout: "500ms"
    # ✓ 确认元素不可见
```

**循环执行可视化**：
```
迭代  条件检查        Headed 显示
─────────────────────────────────
1    条件复核中...    Toast 元素闪烁 ✨
2    条件复核中...    Toast 元素闪烁 ✨
3    条件复核中...    Toast 元素闪烁 ✨
...
18   条件复核中...    Toast 元素闪烁 ✨
19   条件复核中...    Toast 消失！✅
20   条件退出         ✓ 循环完成
     ✅ 总耗时: ~950ms
```

---

### 场景 5: 数据驱动测试 📊
**文件**: `Assets/Examples/Yaml/05-data-driven-csv.yaml`

测试目标：多用户登录测试（从 CSV 数据源）

```yaml
name: Example Data Driven Csv
fixture:
  host_window:
    type: UnityUIFlow.Examples.ExampleCsvLoginWindow
    reopen_if_open: true
  setup:
    - name: 每行前重置
      action: click
      selector: "#reset-button"
      # ✨ 数据行之前的初始化动作
  teardown:
    - name: 每行后重置
      action: click
      selector: "#reset-button"
      # ✨ 数据行之后的清理动作

data:
  from_csv: example-users.csv
  # 加载 CSV 文件，每行作为一次测试迭代

steps:
  - name: 填充用户名 {{ username }}
    action: type_text_fast
    selector: "#username-input"
    value: "{{ username }}"
    # ✨ 数据绑定：{{ username }} 替换为 CSV 值

  - name: 填充密码
    action: type_text_fast
    selector: "#password-input"
    value: "{{ password }}"

  - name: 提交
    action: click
    selector: "#login-button"

  - name: 验证结果
    action: assert_text
    selector: "#status-label"
    expected: "{{ expected }}"
    # ✨ 验证预期结果
```

**CSV 文件示例** (`example-users.csv`):
```csv
username,password,expected
alice,secret,alice 欢迎！
bob,pass123,bob 欢迎！
charlie,xyz,charlie 欢迎！
```

**Headed 模式迭代过程**：
```
【迭代 1 - Alice】
─────────────────────────────────
Setup: 点击重置 ✓
步骤 1: 输入 "alice" ✨ 高亮
步骤 2: 输入 "secret" ✨ 高亮
步骤 3: 点击提交 ✨ 高亮
步骤 4: 验证 "alice 欢迎！" ✅
Teardown: 点击重置 ✓
✅ 迭代完成

【迭代 2 - Bob】
─────────────────────────────────
Setup: 点击重置 ✓
步骤 1: 输入 "bob" ✨ 高亮
步骤 2: 输入 "pass123" ✨ 高亮
步骤 3: 点击提交 ✨ 高亮
步骤 4: 验证 "bob 欢迎！" ✅
Teardown: 点击重置 ✓
✅ 迭代完成

【迭代 3 - Charlie】
─────────────────────────────────
Setup: 点击重置 ✓
步骤 1: 输入 "charlie" ✨ 高亮
步骤 2: 输入 "xyz" ✨ 高亮
步骤 3: 点击提交 ✨ 高亮
步骤 4: 验证 "charlie 欢迎！" ✅
Teardown: 点击重置 ✓
✅ 迭代完成

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
总统计: 3 行 × 4 步 = 12 条测试通过 ✅
```

---

### 场景 6: 文本输入测试 ⌨️
**文件**: `Assets/Examples/Yaml/12-type-text.yaml`

测试目标：验证不同的文本输入速度

```yaml
name: Example Type Text
steps:
  - name: 慢速输入（逐个字符）
    action: type_text
    selector: "#type-text-input"
    value: "typed slowly"
    # ✨ Headed 模式：
    #   看到每个字符逐个输入
    #   模拟真实用户打字速度
    # 用途：测试输入事件是否正确触发

  - name: 验证输入值
    action: assert_property
    selector: "#type-text-input"
    property: "value"
    expected: "typed slowly"
    # ✓ 确认值已正确设置
```

**两种输入模式对比**：
```
【type_text_fast】
输入过程: "typed slowly" 一次性输入
Headed 显示: 文本瞬间填充 ⚡
用途: 高效测试，速度快
风险: 可能跳过事件处理

【type_text】
输入过程: t-y-p-e-d-...-l-y 逐个字符
Headed 显示: ✨ 每个字符依次出现
用途: 测试输入事件处理，发现隐藏 bug
速度: 较慢，但更真实
```

---

## 🐛 Headed 模式故障排除

### 常见问题 1: 选择器找不到元素

**症状**：
```
❌ [ERROR] Element not found: #username-input
```

**Headed 模式调试**：
1. 启用 Headed 模式
2. 观察编辑器窗口：
   - ✨ 没有高亮 → 选择器语法错误或元素不存在
   - ✨ 高亮错误的元素 → 选择器定位有误
3. 检查点：
   - 选择器是否使用了正确的 ID 或 Class？
   - 元素在 Dom 树中的层级是否正确？
   - 是否需要使用子选择器 (>) 或后代选择器 (空格)？

**解决方案**：
```yaml
# ❌ 错误：不存在的 ID
selector: "#login-box"

# ✅ 正确：检查元素实际 ID
selector: "#login-button"

# ✅ 正确：使用子选择器精确定位
selector: "#form > #username-input"

# ✅ 正确：使用属性选择器
selector: "[tooltip=Login]"
```

---

### 常见问题 2: 超时等待失败

**症状**：
```
❌ [ERROR] Element not appeared within 2000ms
```

**Headed 模式调试**：
1. 观察编辑器中的等待过程：
   - ⏳ 进度条是否在动？
   - 📍 元素是否真的没有出现？
   - ⏰ 是否超时时间不够长？

2. 检查点：
   - 触发异步加载的前置条件是否执行？
   - 异步操作是否正常（如网络请求、动画）？
   - timeout 值是否设置过短？

**解决方案**：
```yaml
# ❌ 超时设置过短
action: wait_for_element
selector: "#delayed-message"
timeout: "500ms"  # 太短

# ✅ 增加超时时间
action: wait_for_element
selector: "#delayed-message"
timeout: "3s"  # 给足时间

# ✅ 或者添加前置 wait
- name: 等待一段时间
  action: wait
  duration: "500ms"

- name: 再等待元素
  action: wait_for_element
  selector: "#delayed-message"
  timeout: "2s"
```

---

### 常见问题 3: 断言失败但看不出原因

**症状**：
```
❌ [ERROR] Assert failed: expected "alice" but was "Alice"
```

**Headed 模式调试**：
1. 打开 Headed 模式，观察编辑器：
   - ✨ 元素是否高亮？
   - 📝 实际显示的文本是什么？
   - 🔍 是否存在大小写差异？

2. 仔细检查：
   - 文本大小写是否匹配（区分大小写）？
   - 是否有前后空格？
   - 是否使用了正确的断言类型？

**解决方案**：
```yaml
# ❌ 大小写不匹配
expected: "alice"  # 预期小写

# ✅ 检查实际输出的大小写
expected: "Alice"  # 实际输出是大写

# ✅ 或使用包含断言（不区分大小写）
action: assert_text_contains
selector: "#status-label"
expected: "alice"  # 只要包含即可
```

---

### 常见问题 4: 元素被遮挡

**症状**：
```
❌ [ERROR] Element is not interactable: overlapped or hidden
```

**Headed 模式调试**：
1. 观察 Headed 模式中的元素：
   - ✨ 是否有其他元素遮挡？
   - 📍 元素是否真的在可视区域内？
   - 🎨 是否被父元素的 overflow:hidden 隐藏？

2. 检查点：
   - 是否需要先滚动页面（Scroll）到元素？
   - 是否需要关闭遮挡的 Modal？
   - 是否需要展开折叠的容器？

**解决方案**：
```yaml
# ❌ 元素被遮挡
- name: 直接点击
  action: click
  selector: "#hidden-button"

# ✅ 先滚动到元素可见
- name: 滚动到元素
  action: scroll
  selector: "#container"
  direction: down

- name: 然后点击
  action: click
  selector: "#hidden-button"

# ✅ 或者先关闭 Modal
- name: 关闭弹窗
  action: click
  selector: "#close-modal"

- name: 再点击
  action: click
  selector: "#hidden-button"
```

---

### 常见问题 5: 循环无法结束

**症状**：
```
⚠️ [WARNING] Repeat loop reached max_iterations (20), exiting
```

**Headed 模式调试**：
1. 观察循环过程：
   - 📍 条件是否能正确判断？
   - 🔄 每次迭代是否有进度？
   - ⏰ 是否需要增加 max_iterations？

2. 检查点：
   - 循环条件是否永远为 true？
   - 是否遗漏了某个清理步骤？
   - 元素是否真的会消失？

**解决方案**：
```yaml
# ❌ 条件可能永不满足
repeat_while:
  condition:
    exists: "#modal"
  max_iterations: 20
  steps:
    - name: 等待
      action: wait
      duration: "50ms"

# ✅ 确保循环体能改变条件
repeat_while:
  condition:
    exists: "#toast-message"
  max_iterations: 20
  steps:
    - name: 等待一段时间
      action: wait
      duration: "100ms"
    # 或者添加主动关闭步骤
    - name: 关闭通知
      action: click
      selector: "#toast-close"
      if:
        exists: "#toast-close"
```

---

## 📞 Headed 模式快速参考

| 任务 | 步骤 | Headed 观察 |
|------|------|-----------|
| **验证选择器** | 运行 assert_visible | ✨ 元素高亮 |
| **调试文本输入** | 运行 type_text 或 type_text_fast | 📝 看字符输入过程 |
| **诊断超时** | 运行 wait_for_element | ⏳ 看轮询进度 |
| **查看条件判断** | 运行 if 或 repeat_while | 🔍 看条件高亮 |
| **验证点击** | 运行 click | 🖱️ 看元素闪烁响应 |
| **检查断言** | 运行 assert_* | ✓ 看验证结果 |
| **速度对比** | type_text vs type_text_fast | ⚡ 看执行速度 |

---



## ⚙️ 故障排除

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| YAML 文件解析失败 | 语法错误 | 检查 YAML 缩进、字段名是否正确 |
| 元素找不到 | 选择器错误 | 验证 UI 树结构，调整选择器 |
| 超时错误 | 元素加载慢 | 增加 timeout 或 `defaultTimeoutMs` |
| 动作执行失败 | 元素不可见/禁用 | 检查前置条件，添加 wait 步骤 |
| 报告未生成 | 路径权限 | 检查 `reportPath` 目录权限 |

---

## 📚 扩展和自定义

### 添加自定义动作步骤
1. 创建实现 `IAction` 接口的 C# 类
2. 在 `.unityuiflow.json` 中注册程序集:
   ```json
   {
     "customActionAssemblies": ["YourCustomAssembly"]
   }
   ```
3. 在 YAML 中使用自定义动作:
   ```yaml
   steps:
     - action: your_custom_action
       param1: value1
       param2: value2
   ```

### 创建新的示例 EditorWindow
1. 继承 `EditorWindow`
2. 实现 `OnGUI()` 或使用 UIToolkit
3. 使用命名规范: `Example[YourWindow]Window`
4. 将类放在 `Assets/Examples/Editor/` 中
5. 对应的 YAML 测试放在 `Assets/Examples/Yaml/` 中

---

## ✅ Headed 模式测试完整准备清单

### 环境检查
- [ ] Unity 版本 6000.6.0a2 或更高
- [ ] 所有依赖包已安装 (Test Framework、InputSystem、UI TestFramework 等)
- [ ] MCP 服务器已连接 (状态: connected ✅)
- [ ] 项目未存在编译错误

### 配置检查
- [ ] `.unityuiflow.json` 文件存在且可读
- [ ] `"headed": true` 已启用
- [ ] `"reportPath": "./Reports"` 目录有写权限
- [ ] `"screenshotOnFailure": true` 已启用（便于调试）
- [ ] `"defaultTimeoutMs": 10000` 设置合理

### 测试文件检查
- [ ] YAML 测试文件存在于 `Assets/Examples/Yaml/`
  - [ ] 01-basic-login.yaml ✅
  - [ ] 02-selectors-and-assertions.yaml ✅
  - [ ] 03-wait-for-element.yaml ✅
  - [ ] 其他测试文件完整 ✅
- [ ] 示例 EditorWindow 存在于 `Assets/Examples/Editor/`
  - [ ] ExampleBasicLoginWindow
  - [ ] ExampleSelectorsWindow
  - [ ] ExampleWaitForElementWindow
  - [ ] 其他示例窗口

### Headed 模式特殊检查
- [ ] 编辑器显示器正常连接（需要 UI 渲染）
- [ ] 编辑器未最小化（窗口可见）
- [ ] GPU 加速启用（改善渲染性能）
- [ ] 足够的 RAM（Headed 会增加内存占用）
- [ ] 预留磁盘空间用于截图 (最少 100MB)

### 第一次 Headed 测试建议
1. **从简单测试开始**: 先运行 `01-basic-login.yaml`
2. **使用默认超时**: 不要过度优化超时时间
3. **启用截图**: 便于后续分析
4. **监控编辑器**: 观察元素高亮效果
5. **记录问题**: 记下遇到的任何选择器或等待问题

---

## 📊 Headed 模式 vs CLI 模式选择表

| 场景 | 建议模式 | 理由 |
|------|---------|------|
| **开发调试** | ✅ Headed | 可视化快速定位问题 |
| **选择器验证** | ✅ Headed | 看到高亮效果最直观 |
| **超时问题诊断** | ✅ Headed | 能观察等待过程 |
| **新测试编写** | ✅ Headed | 交互式开发更高效 |
| **CI/CD 流水线** | ✅ CLI | 无 GUI 环境，速度快 |
| **性能基准测试** | ⚠️ CLI | Headed 有绘制开销 |
| **自动化测试套件** | ✅ CLI | 可靠稳定，不依赖显示 |
| **团队测试报告** | 📊 两者 | 先用 Headed 调试，再用 CLI 生产 |

---

## 🚀 MCP 服务器执行命令速查表

### 单个 YAML 测试
```powershell
# 基础登录测试 - Headed 模式
Tool: mcp_unitypilot_unity_editor_e2e_run
Parameters:
  specPath: Assets/Examples/Yaml/01-basic-login.yaml
  artifactDir: D:\UnityUIFlow\artifacts
  exportZip: false
  stopOnFirstFailure: true
```

### 选择器验证测试
```powershell
# 验证多种选择器
Tool: mcp_unitypilot_unity_editor_e2e_run
Parameters:
  specPath: Assets/Examples/Yaml/02-selectors-and-assertions.yaml
  artifactDir: D:\UnityUIFlow\artifacts
  exportZip: false
```

### 异步等待测试
```powershell
# 测试等待动态元素
Tool: mcp_unitypilot_unity_editor_e2e_run
Parameters:
  specPath: Assets/Examples/Yaml/03-wait-for-element.yaml
  artifactDir: D:\UnityUIFlow\artifacts
  exportZip: false
```

### 条件和循环测试
```powershell
# 测试条件执行和循环
Tool: mcp_unitypilot_unity_editor_e2e_run
Parameters:
  specPath: Assets/Examples/Yaml/04-conditional-and-loop.yaml
  artifactDir: D:\UnityUIFlow\artifacts
  exportZip: false
```

### 数据驱动测试
```powershell
# 多行数据同时测试
Tool: mcp_unitypilot_unity_editor_e2e_run
Parameters:
  specPath: Assets/Examples/Yaml/05-data-driven-csv.yaml
  artifactDir: D:\UnityUIFlow\artifacts
  exportZip: true
```

---

## 📈 测试执行的完整工作流

```
┌─────────────────────────────────────────────────────────┐
│                  开始 Headed 模式测试                     │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
      ┌─────────────────────────────────┐
      │ 第 1 步：启用 Headed 配置       │
      │ (.unityuiflow.json)            │
      │ - "headed": true               │
      │ - "defaultTimeoutMs": 10000    │
      └────────────┬────────────────────┘
                   │
                   ▼
      ┌─────────────────────────────────┐
      │ 第 2 步：选择测试文件            │
      │ (Assets/Examples/Yaml/...)      │
      │ ✅ 01-basic-login.yaml          │
      │ ✅ 02-selectors-and-assertions  │
      │ ✅ 03-wait-for-element.yaml     │
      │ ✅ ...其他测试文件              │
      └────────────┬────────────────────┘
                   │
                   ▼
      ┌─────────────────────────────────┐
      │ 第 3 步：MCP 服务器执行          │
      │ Tool: mcp_unitypilot_...e2e_run │
      │ - specPath: 测试文件路径        │
      │ - artifactDir: 输出目录         │
      │ - exportZip: 导出压缩包         │
      └────────────┬────────────────────┘
                   │
                   ▼
      ┌─────────────────────────────────┐
      │ 第 4 步：Headed 模式执行         │
      │ 📺 编辑器可视化显示             │
      │ - 元素高亮                      │
      │ - 步骤执行进度                  │
      │ - 截图保存                     │
      └────────────┬────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
    ✅ 全部通过          ❌ 出现失败
    ┌──────────┐        ┌──────────────┐
    │ 生成报告 │        │ 查看问题    │
    │ result   │        │ - 选择器错误│
    │ PASS     │        │ - 超时      │
    └──────────┘        │ - 断言失败  │
                        └─────┬───────┘
                              │
                              ▼
                        ┌──────────────┐
                        │ 调试和修复   │
                        │ - 更新 YAML  │
                        │ - 修改配置   │
                        └─────┬───────┘
                              │
                              ▼
                        【重新执行】
                        
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 Headed 模式最佳实践

### ✅ 推荐做法
```yaml
# 1. 命名清晰的步骤
- name: 点击登录按钮
  action: click
  selector: "#login-button"
  # 明确的步骤名称，便于调试时快速定位

# 2. 合理的超时设置
action: wait_for_element
selector: "#result-message"
timeout: "3s"  # 给足冗余时间，Headed 下能看到轮询过程

# 3. 完整的断言
- name: 验证最终状态
  action: assert_text
  selector: "#status-label"
  expected: "登录成功"  # 具体的预期值

# 4. 必要的截图
- name: 保存测试证据
  action: screenshot
  tag: "final-state"  # 有意义的标签
```

### ❌ 避免做法
```yaml
# 1. 超短超时
timeout: "500ms"  # 可能导致误判

# 2. 模糊的步骤名称
- action: click
  selector: "#btn"
  # 无法快速理解测试意图

# 3. 过度的等待
action: wait
duration: "5000ms"  # 太长浪费时间

# 4. 缺少断言
- action: click
  selector: "#submit"
# 没有验证操作结果是否正确
```

---

## 📞 快速参考命令

| 需求 | 命令/步骤 | 说明 |
|------|---------|------|
| **启用 Headed** | 编辑 `.unityuiflow.json` + `"headed": true` | 立即启用可视化模式 |
| **运行单个测试** | MCP 工具 + specPath + 文件路径 | 执行指定的 YAML 测试 |
| **运行多个测试** | 循环调用 MCP 工具 + 不同的 specPath | 依次执行不同的测试文件 |
| **查看报告** | 打开 `./Reports/report.md` | 查看完整的测试报告 |
| **查看截图** | 打开 `./Reports/screenshots/` | 查看失败时的证据截图 |
| **禁用 Headed** | 编辑 `.unityuiflow.json` + `"headed": false` | 切换回快速模式 |
| **增加超时** | 编辑 `.unityuiflow.json` + `"defaultTimeoutMs": 15000` | 给慢速 PC 更多时间 |
| **减少过程输出** | 配置日志级别（如有） | 简化控制台输出 |

---

**文档版本**: 2.0 (Headed 模式完全指南)  
**最后更新**: 2026 年 4 月 14 日  
**适用项目**: UnityUIFlow 自动化测试框架  
**重点**: MCP 服务器 + Headed 可视化调试完整工作流
