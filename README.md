# UnityUIFlow

> Unity Editor UIToolkit 自动化测试框架 —— 像 Playwright 一样测试你的 EditorWindow

---

## 目录

1. [概述](#概述)
2. [环境要求](#环境要求)
3. [快速开始](#快速开始)
4. [YAML 用例编写](#yaml-用例编写)
   - [基本结构](#基本结构)
   - [选择器语法](#选择器语法)
   - [内置动作参考](#内置动作参考)
5. [数据驱动测试](#数据驱动测试)
   - [内联数据行](#内联数据行)
   - [CSV 数据源](#csv-数据源)
   - [JSON 数据源](#json-数据源)
6. [条件执行与循环](#条件执行与循环)
7. [Headed 可视化模式](#headed-可视化模式)
8. [C# Fixture 集成](#c-fixture-集成)
9. [自定义动作](#自定义动作)
10. [CLI 与 CI 集成](#cli-与-ci-集成)
11. [测试报告](#测试报告)
12. [常见问题](#常见问题)

---

## 概述

UnityUIFlow 是一个 Unity Editor UIToolkit 自动化测试框架，让你能够用 YAML 声明式脚本或 C# 代码驱动 EditorWindow 界面流程测试。

**核心能力：**

- **YAML 驱动**：无需代码即可编写 UI 自动化测试用例
- **16 个内置动作**：覆盖点击、输入、断言、截图、等待等常见操作
- **数据驱动**：支持内联行、CSV、JSON 三种数据源，自动多轮执行
- **条件与循环**：支持 `if` 条件跳过和 `repeat_while` 循环步骤
- **Headed 模式**：在 Editor 中可视化执行，高亮目标元素，支持步进调试
- **C# 集成**：提供 `UnityUIFlowFixture<TWindow>` 基类，与 Unity Test Framework 无缝集成
- **自定义动作**：通过 `[ActionName]` 特性注册 C# 自定义动作
- **CI 友好**：输出 Markdown + JSON 报告，标准退出码 0/1/2

---

## 环境要求

| 项目 | 要求 |
| --- | --- |
| Unity Editor | 6000.6.0a2 及以上 |
| com.unity.test-framework | 1.7.0 及以上 |
| com.unity.inputsystem | 1.19.0 及以上 |
| 平台 | Windows / macOS（Editor 模式） |

---

## 快速开始

### 第一步：打开示例窗口

在菜单栏选择 **UnityUIFlow > Samples > Login Window**，打开示例登录窗口。

### 第二步：创建 YAML 测试用例

在 `Assets/` 目录下创建文件 `Tests/my-first-test.yaml`：

```yaml
name: My First Login Test
description: 验证基础登录流程

steps:
  - name: 输入用户名
    action: type_text_fast
    selector: "#username-input"
    value: "alice"

  - name: 输入密码
    action: type_text_fast
    selector: "#password-input"
    value: "secret"

  - name: 点击登录
    action: click
    selector: "#login-button"

  - name: 断言欢迎消息
    action: assert_text_contains
    selector: "#status-label"
    expected: "Welcome alice"

  - name: 截图存档
    action: screenshot
    tag: "after-login"
```

### 第三步：运行测试

**方式一：Headed 可视化模式**

菜单栏选择 **UnityUIFlow > Headed Runner**，在面板中选择 YAML 文件，点击"运行"。

**方式二：C# 测试类**

```csharp
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections;
using UnityUIFlow;

public class LoginTests : UnityUIFlowFixture<SampleLoginWindow>
{
    [UnityTest]
    public IEnumerator BasicLogin_ShouldShowWelcome()
    {
        yield return SetUp();

        string yaml = @"
name: Basic Login
steps:
  - action: type_text_fast
    selector: '#username-input'
    value: alice
  - action: type_text_fast
    selector: '#password-input'
    value: secret
  - action: click
    selector: '#login-button'
  - action: assert_text_contains
    selector: '#status-label'
    expected: 'Welcome alice'
";
        var task = ExecuteYamlStepsAsync(yaml);
        while (!task.IsCompleted) yield return null;
        Assert.AreEqual(TestStatus.Passed, task.Result.Status);
    }
}
```

---

## YAML 用例编写

### 基本结构

每个 YAML 文件对应一个测试用例，顶层字段如下：

```yaml
# 必填：用例名称（1-120 字符）
name: My Test Case

# 可选：用例说明
description: 描述这个用例做什么

# 可选：数据驱动配置（见数据驱动章节）
data:
  from_csv: test-data.csv

# 可选：前后置步骤
fixture:
  setup:
    - action: click
      selector: "#reset-button"
  teardown:
    - action: screenshot
      tag: "teardown"

# 可选：标签，用于 CLI 过滤
tags:
  - smoke
  - login

# 可选：用例级超时（覆盖全局默认值）
timeout: "30s"

# 必填：步骤列表
steps:
  - name: 步骤说明
    action: click
    selector: "#my-button"
```

### 步骤字段说明

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `name` | string | 否 | 步骤显示名，默认使用动作名 |
| `action` | string | 是（非循环步骤） | 动作名，如 `click`、`type_text_fast` |
| `selector` | string | 视动作而定 | 元素选择器 |
| `value` | string | 视动作而定 | 输入值，支持 `{{ variable }}` 模板 |
| `expected` | string | 视动作而定 | 期望值，支持 `{{ variable }}` 模板 |
| `timeout` | string | 否 | 步骤级超时，如 `5s`、`500ms` |
| `duration` | string | 视动作而定 | 等待时长，如 `1s`、`200ms` |
| `tag` | string | 否（screenshot 动作的截图标签） | 截图文件标签 |
| `if` | 对象 | 否 | 条件执行（见条件章节） |
| `repeat_while` | 对象 | 否 | 循环步骤（见循环章节） |

### 选择器语法

UnityUIFlow 支持类 CSS 选择器语法：

| 语法 | 示例 | 说明 |
| --- | --- | --- |
| ID 选择器 | `#login-button` | 匹配 `name == "login-button"` 的元素 |
| 类选择器 | `.primary` | 匹配包含 USS 类 `primary` 的元素 |
| 类型选择器 | `Button` | 匹配类型为 `Button` 的元素 |
| 属性选择器 | `[tooltip=Login]` | 匹配属性 `tooltip` 值为 `Login` 的元素 |
| 后代选择器 | `#panel Button` | `#panel` 内所有 `Button`（任意层级） |
| 直接子选择器 | `#panel > .item` | `#panel` 的直接子元素中含 `.item` 类的 |
| 伪类选择器 | `.item:first-child` | 父元素的第一个子元素且含 `.item` 类 |

**组合示例：**

```yaml
# ID 选择器
selector: "#username-input"

# 属性选择器
selector: "[tooltip=Save]"

# 类选择器
selector: ".menu > .item:first-child"

# 后代选择器
selector: "#login-panel TextField"
```

### 内置动作参考

UnityUIFlow 提供 16 个内置动作：

#### 交互动作

**click** — 单击元素

```yaml
- action: click
  selector: "#login-button"
```

**double_click** — 双击元素

```yaml
- action: double_click
  selector: "#my-element"
```

**type_text** — 逐字符键盘模拟输入（触发键盘事件）

```yaml
- action: type_text
  selector: "#username-input"
  value: "alice"
```

**type_text_fast** — 直接写入文本值（快速，目标必须是 TextField 或兼容控件）

```yaml
- action: type_text_fast
  selector: "#username-input"
  value: "alice"
```

**press_key** — 发送键盘按键（`key` 为 Unity KeyCode 名称）

```yaml
- action: press_key
  key: "Return"
```

常用 KeyCode：`Return`、`Tab`、`Escape`、`Space`、`Delete`、`Backspace`

**drag** — 拖拽（`from`/`to` 为选择器或 `x,y` 坐标）

```yaml
# 用选择器指定起止点
- action: drag
  from: "#source-item"
  to: "#target-area"
  duration: "300ms"

# 用坐标指定起止点（非负整数）
- action: drag
  from: "100,200"
  to: "400,300"
```

**scroll** — 滚动（`delta` 格式为 `dx,dy`）

```yaml
- action: scroll
  selector: "#scroll-view"
  delta: "0,-100"
```

**hover** — 悬停到元素

```yaml
- action: hover
  selector: "#menu-item"
  duration: "500ms"
```

#### 等待动作

**wait** — 固定等待

```yaml
- action: wait
  duration: "1s"

# 也可用毫秒
- action: wait
  duration: "500ms"
```

**wait_for_element** — 等待元素出现

```yaml
- action: wait_for_element
  selector: "#result-panel"
  timeout: "10s"
```

#### 断言动作

**assert_visible** — 断言元素可见

```yaml
- action: assert_visible
  selector: "#success-banner"
  timeout: "3s"
```

**assert_not_visible** — 断言元素不可见或不存在

```yaml
- action: assert_not_visible
  selector: "#error-message"
```

**assert_text** — 断言元素文本完全匹配

```yaml
- action: assert_text
  selector: "#status-label"
  expected: "Idle"
```

**assert_text_contains** — 断言元素文本包含指定片段

```yaml
- action: assert_text_contains
  selector: "#status-label"
  expected: "Welcome"
```

**assert_property** — 断言元素属性值（UIToolkit 样式/绑定属性）

```yaml
- action: assert_property
  selector: "#login-button"
  property: "tooltip"
  expected: "Login"
```

#### 截图动作

**screenshot** — 捕获截图

```yaml
- action: screenshot
  tag: "after-login"
```

截图文件保存到 `Reports/Screenshots/` 目录，文件名格式：`{caseName}-{stepIndex:D3}-{tag}-{timestamp}.png`。

---

## 数据驱动测试

### 内联数据行

直接在 YAML 中写数据行：

```yaml
name: Inline Data Test
data:
  rows:
    - username: "alice"
      password: "pass1"
      expected: "Welcome alice"
    - username: "bob"
      password: "pass2"
      expected: "Welcome bob"

steps:
  - action: type_text_fast
    selector: "#username-input"
    value: "{{ username }}"
  - action: type_text_fast
    selector: "#password-input"
    value: "{{ password }}"
  - action: click
    selector: "#login-button"
  - action: assert_text_contains
    selector: "#status-label"
    expected: "{{ expected }}"
```

### CSV 数据源

创建 `users.csv`（与 YAML 文件同目录或指定路径）：

```csv
username,password,expected
alice,pass1,Welcome alice
bob,pass2,Welcome bob
charlie,pass3,Welcome charlie
```

YAML 中引用：

```yaml
name: CSV Driven Login
data:
  from_csv: users.csv

steps:
  - action: type_text_fast
    selector: "#username-input"
    value: "{{ username }}"
  - action: type_text_fast
    selector: "#password-input"
    value: "{{ password }}"
  - action: click
    selector: "#login-button"
  - action: assert_text_contains
    selector: "#status-label"
    expected: "{{ expected }}"
```

### JSON 数据源

创建 `users.json`（根节点必须是对象数组）：

```json
[
  { "username": "alice", "password": "pass1", "expected": "Welcome alice" },
  { "username": "bob",   "password": "pass2", "expected": "Welcome bob" }
]
```

YAML 中引用：

```yaml
name: JSON Driven Test
data:
  from_json: users.json

steps:
  - action: type_text_fast
    selector: "#username-input"
    value: "{{ username }}"
```

> **注意：** `rows`、`from_csv`、`from_json` 三者只能声明一种，同时声明会报错。

---

## 条件执行与循环

### 条件执行（if）

当指定选择器存在时才执行该步骤，不存在时跳过并记录为 `Skipped`：

```yaml
steps:
  - name: 存在重置按钮时点击
    action: click
    selector: "#reset-button"
    if:
      exists: "#reset-button"

  - name: 继续后续步骤
    action: click
    selector: "#save-button"
```

### 循环步骤（repeat_while）

循环体持续执行，直到条件不满足：

```yaml
steps:
  - name: 点击保存
    action: click
    selector: "#save-button"

  - name: 等待 Toast 出现
    action: assert_visible
    selector: "#toast-message"
    timeout: "1s"

  - name: 等待 Toast 消失
    repeat_while:
      condition:
        exists: "#toast-message"
      max_iterations: 200    # 可选，默认 1000
      steps:
        - action: wait
          duration: "50ms"

  - name: 确认 Toast 已隐藏
    action: assert_not_visible
    selector: "#toast-message"
    timeout: "2s"
```

> **注意：** 单个循环最大迭代次数默认 1000 次，超出后报 `TEST_LOOP_LIMIT_EXCEEDED` 错误。

---

## Headed 可视化模式

Headed 模式在 Unity Editor 中以可视化面板执行测试，支持：

- **实时高亮**：用红色半透明遮罩标记当前操作的目标元素
- **步进调试**：逐步执行，每步暂停等待用户确认
- **失败保留**：测试失败后保留当前 UI 现场，便于排查
- **连续/步进模式**切换

**打开方式：** 菜单 **UnityUIFlow > Headed Runner**

**面板操作：**

| 按钮 | 说明 |
| --- | --- |
| 选择 YAML | 选择要执行的 YAML 测试文件 |
| 运行 | 开始执行测试 |
| 暂停 | 暂停当前执行 |
| 继续 | 从暂停处继续 |
| 下一步 | 步进模式下执行一步 |
| 停止 | 强制终止执行 |

**失败策略：**
- `Pause`（默认）：步骤失败时自动暂停，保留现场供检查
- `Continue`：步骤失败后继续执行后续步骤

---

## C# Fixture 集成

`UnityUIFlowFixture<TWindow>` 提供与 Unity Test Framework 的深度集成，自动管理窗口生命周期。

### 基础用法

```csharp
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading.Tasks;
using UnityUIFlow;

[TestFixture]
public class LoginWindowTests : UnityUIFlowFixture<SampleLoginWindow>
{
    // SetUp / TearDown 由基类自动处理
    // Window、Root、Finder、Screenshot 属性已就绪

    [UnityTest]
    public IEnumerator Login_WithValidCredentials_ShouldSucceed()
    {
        yield return SetUp();

        Task<TestResult> task = ExecuteYamlStepsAsync(@"
name: Valid Login
steps:
  - action: type_text_fast
    selector: '#username-input'
    value: alice
  - action: type_text_fast
    selector: '#password-input'
    value: secret
  - action: click
    selector: '#login-button'
  - action: assert_text_contains
    selector: '#status-label'
    expected: 'Welcome'
");

        while (!task.IsCompleted) yield return null;

        Assert.AreEqual(TestStatus.Passed, task.Result.Status);

        yield return TearDown();
    }
}
```

### 可用属性

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `Window` | `TWindow` | 当前测试窗口实例 |
| `Root` | `VisualElement` | 窗口根视觉元素 |
| `Finder` | `ElementFinder` | 元素查找器实例 |
| `Screenshot` | `ScreenshotManager` | 截图管理器实例 |
| `CurrentOptions` | `TestOptions` | 当前测试选项 |
| `CurrentContext` | `ExecutionContext` | 当前执行上下文（YAML 执行后可用） |
| `IsWindowReady` | `bool` | 窗口是否已就绪 |

### 自定义测试选项

重写 `CreateDefaultOptions` 来自定义选项：

```csharp
protected override TestOptions CreateDefaultOptions()
{
    return new TestOptions
    {
        Headed = false,
        ReportOutputPath = "TestReports",
        ScreenshotPath = "TestReports/Screenshots",
        ScreenshotOnFailure = true,
        DefaultTimeoutMs = 10000,
    };
}
```

### 直接使用 Finder

```csharp
[UnityTest]
public IEnumerator DirectFinder_Example()
{
    yield return SetUp();

    // 同步查找
    var button = Finder.Find(
        new SelectorCompiler().Compile("#login-button"),
        Root
    );
    Assert.IsNotNull(button.Element);

    yield return TearDown();
}
```

---

## 自定义动作

通过实现 `IAction` 接口并标记 `[ActionName]` 特性来注册自定义动作。

### 创建自定义动作

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UnityUIFlow;

[ActionName("my_fill_form")]
public sealed class MyFillFormAction : IAction
{
    public async Task ExecuteAsync(
        VisualElement root,
        ActionContext context,
        Dictionary<string, string> parameters)
    {
        // 读取必填参数（缺失时自动抛 ACTION_PARAMETER_MISSING）
        string username = ActionHelpers.Require(parameters, "my_fill_form", "username");
        string password = ActionHelpers.Require(parameters, "my_fill_form", "password");

        // 等待元素出现
        FindResult userResult = await context.Finder.WaitForElementAsync(
            new SelectorCompiler().Compile("#username-input"),
            root,
            new WaitOptions
            {
                TimeoutMs = context.Options.DefaultTimeoutMs,
                PollIntervalMs = 16,
                RequireVisible = true,
            },
            context.CancellationToken
        );

        // 填充值
        ActionHelpers.TryAssignFieldValue(userResult.Element, username);

        FindResult passResult = await context.Finder.WaitForElementAsync(
            new SelectorCompiler().Compile("#password-input"),
            root,
            new WaitOptions { TimeoutMs = context.Options.DefaultTimeoutMs, PollIntervalMs = 16, RequireVisible = true },
            context.CancellationToken
        );

        ActionHelpers.TryAssignFieldValue(passResult.Element, password);

        // 检查取消
        context.CancellationToken.ThrowIfCancellationRequested();
    }
}
```

### 在 YAML 中使用自定义动作

```yaml
name: Custom Action Test
steps:
  - name: 使用自定义填表动作
    action: my_fill_form
    username: "alice"
    password: "secret"

  - action: click
    selector: "#login-button"

  - action: assert_text_contains
    selector: "#status-label"
    expected: "Welcome alice"
```

### Page Object 模式

```csharp
public sealed class LoginPage
{
    private readonly VisualElement _root;

    public LoginPage(VisualElement root)
    {
        _root = root;
    }

    public Task LoginAsync(string username, string password)
    {
        if (_root.Q<TextField>("username-input") is TextField u)
            u.value = username;

        if (_root.Q<TextField>("password-input") is TextField p)
            p.value = password;

        if (_root.Q<Button>("login-button") is Button btn)
            ActionHelpers.DispatchClick(btn, 1);

        return Task.CompletedTask;
    }
}

// 在测试中使用
[UnityTest]
public IEnumerator PageObject_Login()
{
    yield return SetUp();
    var page = new LoginPage(Root);
    var task = page.LoginAsync("alice", "secret");
    while (!task.IsCompleted) yield return null;
    // ... 断言
    yield return TearDown();
}
```

---

## CLI 与 CI 集成

### 命令行参数

在 Unity 批处理模式下运行测试时，通过 `-unityUIFlow.*` 参数控制行为：

```bash
Unity.exe -batchmode -nographics -projectPath /path/to/project \
  -runTests \
  -unityUIFlow.testFilter "login*" \
  -unityUIFlow.headed false \
  -unityUIFlow.reportPath "CI/Reports" \
  -unityUIFlow.screenshotOnFailure true \
  -unityUIFlow.screenshotPath "CI/Reports/Screenshots"
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `-unityUIFlow.testFilter` | string | 无（不过滤） | YAML 文件名或用例 `name` 的通配过滤，`*` 匹配任意字符，大小写不敏感 |
| `-unityUIFlow.headed` | bool | `true` | 是否启用 Headed 模式（批处理模式下自动设为 `false`） |
| `-unityUIFlow.reportPath` | string | `"Reports"` | 报告输出目录 |
| `-unityUIFlow.screenshotOnFailure` | bool | `true` | 步骤失败时是否自动截图 |
| `-unityUIFlow.screenshotPath` | string | `{reportPath}/Screenshots` | 截图输出目录 |

### 配置文件

在项目根目录创建 `.unityuiflow.json` 作为默认配置（CLI 参数优先级高于配置文件）：

```json
{
  "headed": false,
  "reportPath": "Reports",
  "screenshotOnFailure": true,
  "screenshotPath": "Reports/Screenshots"
}
```

**参数优先级：** CLI 参数 > 配置文件 > 默认值

### 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 全部通过（所有用例 Passed 或 Skipped） |
| `1` | 存在测试失败（至少 1 个用例 Failed） |
| `2` | 框架级错误（参数非法、报告目录不可写、YAML 解析失败等） |

### GitHub Actions 示例

```yaml
- name: Run UnityUIFlow Tests
  run: |
    Unity.exe -batchmode -nographics \
      -projectPath ${{ github.workspace }} \
      -runTests \
      -unityUIFlow.reportPath "CI/Reports" \
      -unityUIFlow.screenshotOnFailure true \
      -logFile CI/unity.log

- name: Upload Test Reports
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: test-reports
    path: CI/Reports/
```

---

## 测试报告

每次测试执行后，在 `ReportOutputPath`（默认 `Reports/`）目录生成：

### 用例报告（Markdown）

文件：`Reports/{CaseName}.md`

```markdown
# 测试报告：Basic Login

**状态**：Passed
**开始时间**：2026-04-09T10:00:00Z
**结束时间**：2026-04-09T10:00:03Z
**耗时**：3021ms

## 步骤详情

| 步骤 | 状态 | 耗时(ms) | 错误码 | 截图 |
| --- | --- | --- | --- | --- |
| Fill username | Passed | 12 | | |
| Fill password | Passed | 8 | | |
| Click login | Passed | 45 | | |
| Assert welcome message | Passed | 16 | | |
| Capture result | Passed | 220 | | [查看](Screenshots/...) |
```

### 用例报告（JSON）

文件：`Reports/{CaseName}.json`

包含完整的结构化测试结果，便于 CI 系统进一步处理。

### 套件报告

文件：`Reports/suite-report.md` 和 `Reports/suite-report.json`

汇总所有用例的执行状态和耗时。

### 截图

失败时自动截图（若 `ScreenshotOnFailure=true`），以及执行 `screenshot` 动作时手动截图，保存至 `Reports/Screenshots/` 目录。

---

## 常见问题

### Q: YAML 中 `selector` 找不到元素怎么办？

确认以下几点：
1. 元素的 `name` 属性已正确设置（`#id` 选择器匹配 `name`）
2. 元素已添加对应 USS 类（`.class` 选择器匹配 USS 类名）
3. 元素当前可见（`display != None`，`visibility != Hidden`，`opacity > 0`）
4. 增加 `timeout` 等待元素出现：`timeout: "5s"`

### Q: `type_text_fast` 报类型不兼容错误？

`type_text_fast` 要求目标元素是 `TextField`、`IntegerField`、`FloatField` 等继承 `TextInputBaseField` 的控件。对于其他元素，使用 `type_text`（逐键盘事件输入）。

### Q: 数据驱动时变量替换不生效？

确认变量占位符格式为 `{{ variable }}`（双大括号，各有一个空格，区分大小写）。

### Q: 循环步骤无限执行？

添加 `max_iterations` 限制：

```yaml
repeat_while:
  condition:
    exists: "#loading-indicator"
  max_iterations: 100
  steps:
    - action: wait
      duration: "100ms"
```

### Q: 自定义动作没有被识别？

确认：
1. 类有 `[ActionName("your_action_name")]` 特性
2. 类实现了 `IAction` 接口
3. 类所在程序集在白名单中（默认包含 `Assembly-CSharp`、`Assembly-CSharp-Editor`）

### Q: Headed 模式下元素高亮不显示？

Headed 模式下会在目标元素上叠加半透明红色遮罩（`rgba(255,0,0,0.20)`）和红色描边。若不显示，检查被测窗口是否在 Editor 中正常打开。

### Q: CI 环境下 Headed 模式应该如何处理？

CI 环境使用 `-batchmode` 启动 Unity 时，框架会自动检测并将 `headed` 设为 `false`，无需手动设置。

---

## 示例文件

项目内置示例位于 `Assets/Examples/Yaml/`：

| 文件 | 说明 |
| --- | --- |
| `01-basic-login.yaml` | 基础登录流程：输入、点击、断言、截图 |
| `02-data-driven-csv.yaml` | CSV 数据驱动登录测试 |
| `03-assertions-and-selectors.yaml` | 各种选择器和断言动作演示 |
| `04-conditional-and-loop.yaml` | 条件执行和 repeat_while 循环演示 |
| `05-custom-action-and-json.yaml` | 自定义动作 + JSON 数据源演示 |

---

## 文档与规格

详细设计文档位于 `cocs/` 目录：

| 文档 | 说明 |
| --- | --- |
| `00-Overview.md` | 模块总览与阅读指引 |
| `00-架构设计与技术约束.md` | 分层架构、技术栈、版本约束 |
| `00-API速查与最佳实践.md` | 选择器、动作、CLI 参数速查，Playwright 对照表 |
| `M01-用例编排与数据驱动-需求文档.md` | YAML 结构、数据驱动、变量替换 |
| `M02-YAML解析与执行计划-需求文档.md` | YAML 解析、AST、ExecutionPlan |
| `M03-执行引擎与运行控制-需求文档.md` | TestRunner、StepExecutor、失败策略 |
| `M04-元素定位与等待-需求文档.md` | ElementFinder、选择器语法、等待策略 |
| `M05-动作系统与CSharp扩展-需求文档.md` | ActionRegistry、内置/自定义动作 |
| `M06-Headed可视化执行-需求文档.md` | Headed 面板、高亮、步进调试 |
| `M07-报告与截图-需求文档.md` | Markdown/JSON 报告、截图管理 |
| `M08-命令行与CI集成-需求文档.md` | CLI 参数、批处理、退出码 |
| `M09-测试基座与Fixture基类-需求文档.md` | UnityUIFlowFixture&lt;TWindow&gt; 生命周期 |

---

*UnityUIFlow — 让 Unity Editor UI 测试像 Playwright 一样简单*
