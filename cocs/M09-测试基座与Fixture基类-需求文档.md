# M09 测试基座与Fixture基类 需求文档

版本：1.1.0
日期：2026-04-08
状态：补充版

---

## 1. 模块职责

- 负责：封装 `EditorWindowUITestFixture<TWindow>` 的统一测试基座，向上层测试提供窗口生命周期、根节点访问、默认工具对象和 YAML 执行桥接。
- 负责：为 C# 测试、Page Object 测试、自定义动作测试提供一致的宿主环境。
- 不负责：YAML 解析细节、不负责具体动作实现、不负责命令行参数解析。
- 输入/输出：输入为目标 `EditorWindow` 类型、测试生命周期事件、YAML 文本或路径；输出为初始化完成的测试上下文、执行结果与清理状态。

---

## 2. 数据模型

### UnityUIFlowFixture\<TWindow\>（测试基类字段）

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| Window | TWindow | 必填 | 当前被测 EditorWindow 实例 | 非空；必须继承 `EditorWindow` | 无 |
| Root | VisualElement | 必填 | 当前窗口根节点（即 `Window.rootVisualElement`） | 非空 | 无 |
| Finder | ElementFinder | 必填 | 当前测试使用的元素查找器 | 非空 | 无 |
| Screenshot | ScreenshotManager | 必填 | 当前测试使用的截图管理器 | 非空 | 无 |
| CurrentOptions | TestOptions | 必填 | 当前测试运行选项 | 非空 | 默认 `TestOptions` 实例 |
| CurrentContext | ExecutionContext | 可选 | 当前 YAML 执行上下文 | `null` 表示尚未开始 YAML 执行 | `null` |
| IsWindowReady | bool | 必填 | 窗口是否已完成初始化 | `true` 或 `false` | `false` |
| YamlSource | string | 可选 | 当前执行的 YAML 文本或路径 | `null` 表示未执行 YAML | `null` |

---

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 初始化测试基座 | NUnit `[UnitySetUp]` 或 `[SetUp]` 生命周期（见§4） | `TWindow` 无法创建；窗口初始化失败 | `UnityUIFlowFixture<TWindow>.SetUp` `(设计提案，实现时确认)` | 不涉及；每次测试前重新初始化 |
| 获取窗口根节点 | 测试代码访问 `Root` 属性 | 窗口未创建；窗口已销毁 | `UnityUIFlowFixture<TWindow>.Root` `(设计提案，实现时确认)` | 不涉及；只读操作 |
| 执行 YAML 步骤 | 测试代码调用 `ExecuteYamlSteps(string yamlContent)` | `yamlContent` 为空；上下文未初始化 | `UnityUIFlowFixture<TWindow>.ExecuteYamlSteps` `(设计提案，实现时确认)` | 不涉及；由执行引擎管理实际 UI 状态 |
| 创建默认测试选项 | 测试基座初始化时自动触发 | 无 | `UnityUIFlowFixture<TWindow>.CreateDefaultOptions` `(设计提案，实现时确认)` | 不涉及；每次测试覆盖旧实例 |
| 清理窗口与上下文 | NUnit `[UnityTearDown]` 或 `[TearDown]` 生命周期（见§4） | 当前未创建窗口 | `UnityUIFlowFixture<TWindow>.TearDown` `(设计提案，实现时确认)` | 不涉及；销毁测试现场，不支持恢复 |

---

## 4. 交互规格

- 触发事件：NUnit 进入单个测试方法前触发 `SetUp`，退出测试方法后触发 `TearDown`。
- 状态变化：`Uninitialized -> WindowCreated -> ContextReady -> Executing/Idle -> TornDown`。
- 数据提交时机：窗口创建成功后立即初始化 `Finder`、`Screenshot`、`CurrentOptions`；YAML 文本在调用 `ExecuteYamlSteps` 时提交给执行引擎。
- 取消/回退：测试中抛异常时仍必须执行 `TearDown`，并释放窗口与上下文。
- YAML 桥接：`ExecuteYamlSteps` 必须使用当前窗口的 `rootVisualElement` 作为执行根节点，不允许重新创建独立宿主窗口。
- 扩展入口：派生类允许覆写 `SetUp`、`TearDown`、`CreateDefaultOptions`，但必须调用基类实现（`base.SetUp()`、`base.TearDown()`）。
- NUnit 属性选择（更优方案）：
  - 若测试方法标注 `[UnityTest]`，则 `SetUp`/`TearDown` 必须使用 `[UnitySetUp]`/`[UnityTearDown]` 并返回 `IEnumerator`，以支持异步等待窗口初始化。
  - 若测试方法标注 `[Test]`（纯 C# 同步测试），则使用 `[SetUp]`/`[TearDown]`。
  - V1 优先实现 `[UnitySetUp]`/`[UnityTearDown]` 版本，因为窗口创建和 UI 就绪通常需要等待至少 1 帧（见 `TODO-009`）。

---

## 5. 视觉规格

不涉及

---

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `TWindow` 必须继承 `EditorWindow` | 创建测试基座前（泛型约束编译期检查） | Error | `测试窗口类型必须继承 EditorWindow` |
| `Window` 创建成功后 `rootVisualElement` 不能为空 | `SetUp` 完成前 | Error | `测试窗口根节点初始化失败` |
| `ExecuteYamlSteps` 的 `yamlContent` 不能为空 | 执行 YAML 前 | Error | `YAML 内容不能为空` |
| 派生类覆写 `SetUp` 时必须先或后调用基类实现 | 测试基座运行时自检 | Warning | `派生类未调用基类 SetUp 可能导致上下文不完整` |
| `TearDown` 中窗口释放失败必须输出显式日志 | 测试结束后 | Warning | `测试窗口释放失败：{detail}` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 测试窗口创建失败 | FIXTURE_WINDOW_CREATE_FAILED | `测试窗口创建失败：{windowType}` | 抛异常并终止当前测试 |
| 根节点初始化失败 | FIXTURE_ROOT_MISSING | `测试窗口根节点缺失：{windowType}` | 抛异常并终止当前测试 |
| YAML 内容为空 | FIXTURE_YAML_EMPTY | `YAML 内容不能为空` | 抛异常并终止当前测试 |
| 基座上下文未初始化 | FIXTURE_CONTEXT_NOT_READY | `测试基座上下文未初始化` | 抛异常并终止当前测试 |
| 测试清理失败 | FIXTURE_TEARDOWN_FAILED | `测试清理失败：{detail}` | 记录错误并继续 NUnit 清理流程，不阻断后续测试 |

---

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 主动通知 | 将当前窗口根节点、运行选项和 YAML 文本交给执行引擎运行 | `UnityUIFlowFixture<TWindow>.ExecuteYamlSteps`、`TestRunner.RunTest` `(设计提案)` |
| M04 元素定位与等待 | 主动通知 | 向派生测试和 Page Object 暴露统一的 `Finder` 实例 | `UnityUIFlowFixture<TWindow>.Finder` `(设计提案)` |
| M05 动作系统与CSharp扩展 | 被动接收 | 为 Page Object 和自定义动作测试提供 `ActionContext` 基础设施 | `ActionContext.Root`、`ActionContext.Finder` `(设计提案)` |
| M07 报告与截图 | 被动接收 | 暴露统一的 `ScreenshotManager` 给测试与执行引擎 | `UnityUIFlowFixture<TWindow>.Screenshot` `(设计提案)` |

---

## 8. 技术实现要点

- 关键类与职责：
  - `UnityUIFlowFixture<TWindow>` `(设计提案，实现时确认)`：统一测试基类，继承自 `EditorWindowUITestFixture<TWindow>`（Unity 官方测试框架提供）。
  - `EditorWindowUITestFixture<TWindow>`：底层生命周期宿主，由 `com.unity.ui.test-framework` 提供，负责窗口的实际创建和销毁。
  - `FixtureExecutionBridge` `(设计提案，实现时确认)`：把基座的 `Window`、`Root`、`Finder` 转换为执行引擎可消费的 `ExecutionContext`。

- NUnit 属性与异步生命周期（关键实现细节）：

```csharp
// [UnityTest] 场景（推荐首版实现）
[UnitySetUp]
public IEnumerator SetUp()
{
    // 调用基类创建窗口，等待至少1帧让UI就绪
    yield return base.SetUp();
    Finder = new ElementFinder(Root);
    Screenshot = new ScreenshotManager(CurrentOptions);
    IsWindowReady = true;
}

[UnityTearDown]
public IEnumerator TearDown()
{
    CurrentContext?.Dispose();
    IsWindowReady = false;
    yield return base.TearDown();
}
```

- 核心流程：

```text
NUnit [UnitySetUp]
-> EditorWindowUITestFixture creates TWindow
-> Wait 1 frame for rootVisualElement to be ready
-> Initialize Finder/Screenshot/TestOptions
-> Optional test-specific setup (derived class)
-> Execute test body or ExecuteYamlSteps
-> NUnit [UnityTearDown]
-> Dispose context and close window
```

- 性能约束：
  - 单个测试只创建 1 个被测窗口实例。
  - `Finder` 与 `ScreenshotManager` 在单测试内复用，不在每个步骤重复创建。
  - 清理阶段必须在当前测试结束时完成，禁止跨测试保留窗口实例。

- 禁止项：
  - 禁止派生测试绕过基类直接维护另一套 `Finder` 和 `Screenshot` 生命周期。
  - 禁止 `ExecuteYamlSteps` 在当前测试内打开额外窗口作为执行宿主。

- TODO(待确认)：`EditorWindowUITestFixture<TWindow>` 在目标 Unity 版本和目标 UI Test Framework 版本下的具体生命周期钩子与窗口创建方式需 PoC 验证（见 `TODO-009`）。

---

## 9. 验收标准

1. [定义继承 `UnityUIFlowFixture<MyWindow>` 的测试类] -> [执行测试] -> [`SetUp` 后可访问非空的 `Window`、`Root`、`Finder`、`Screenshot`]
2. [派生测试调用 `ExecuteYamlSteps(validYaml)`] -> [执行测试] -> [YAML 步骤使用当前窗口根节点运行，不创建第二个宿主窗口]
3. [测试方法内部抛异常] -> [结束测试] -> [`TearDown` 仍执行，并释放测试上下文]
4. [派生类覆写 `CreateDefaultOptions`] -> [执行测试] -> [当前测试运行选项使用派生类提供的覆盖值]
5. [窗口创建失败] -> [进入 `SetUp`] -> [抛出 `FIXTURE_WINDOW_CREATE_FAILED`，测试直接失败]
6. [派生类覆写 `SetUp` 未调用 `base.SetUp()`] -> [执行测试] -> [输出 `Warning` 日志提示上下文不完整]

---

## 10. 边界规范

- 空数据：未执行 YAML 的纯 C# Page Object 测试也必须允许使用该基座，不强制要求 `YamlSource` 存在。
- 单元素：窗口根节点下只有 1 个可测试元素时，基座仍必须正常初始化并允许 Finder 查找。
- 上下限临界值：单测试内只能存在 1 个当前宿主窗口；尝试重复创建同类型窗口时必须先关闭旧实例。
- 异常数据恢复：若 `TearDown` 失败，不得影响后续测试继续启动；必须记录 `FIXTURE_TEARDOWN_FAILED` 并尽可能释放资源。

---

## 11. 周边可选功能

- P1：支持基座级测试数据注入；当前预留 `CreateExecutionContext` 虚方法（返回 `ExecutionContext`），派生类可覆写以注入自定义数据行。
- P1：支持多窗口测试宿主；当前不进入 V1，仅预留 `AdditionalWindows: List<EditorWindow>` 扩展集合字段，V1 始终为空列表。
- P2：支持自动收集窗口布局快照；当前不预留持久化格式。
