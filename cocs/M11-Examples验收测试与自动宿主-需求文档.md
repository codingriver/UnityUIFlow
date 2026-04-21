# M11 Examples验收测试与自动宿主 需求文档

版本：1.1.0
日期：2026-04-10
状态：重写基线

---

## 1. 模块职责

- 负责：定义 `Assets/Examples` 目录下的验收测试组织方式，包括示例宿主 `EditorWindow`、UXML/USS 界面文件与验收 YAML 的目录结构。
- 负责：定义 YAML 运行时如何通过 `fixture.host_window` 自动打开被测 `EditorWindow` 宿主，并在用例结束后关闭。
- 负责：约束"一个示例 YAML 覆盖一个当前功能验收点"的编写规则。
- 不负责：录制与回放功能设计；不负责 PlayMode 专用示例；不负责替代解析、报告、CLI、Headed 模块的单元测试。
- 输入/输出：输入为 `Assets/Examples/Yaml/` 下的 YAML 文件和 `Assets/Examples/Editor/` 下的宿主窗口类型；输出为自动宿主窗口实例、测试根节点与验收执行结果。

---

## 2. 数据模型

### 目录约定

| 目录 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| `Assets/Examples/Editor/` | 目录 | 必填 | 示例宿主 `EditorWindow` 与示例行为逻辑 | 目录必须存在 | 无 |
| `Assets/Examples/Uxml/` | 目录 | 必填 | 每个验收用例对应的 UXML 界面文件 | 目录必须存在 | 无 |
| `Assets/Examples/Uss/` | 目录 | 可选 | 示例界面样式，允许共用或按用例拆分 | 目录可不存在 | 无 |
| `Assets/Examples/Yaml/` | 目录 | 必填 | 验收 YAML、CSV、JSON 等数据文件 | 目录必须存在 | 无 |

### HostWindowConfig（YAML fixture 宿主窗口扩展）

在 `fixture` 下新增 `host_window` 节点：

```yaml
fixture:
  host_window:
    type: "UnityUIFlow.Examples.ExampleBasicLoginWindow"
    reopen_if_open: true
```

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| type | string | 必填 | 宿主 `EditorWindow` 的完整类型名 | 非空字符串；允许使用 `FullName`，也允许在无歧义时使用短类名 | 无 |
| reopen_if_open | bool | 可选 | 执行前是否关闭同类型已打开窗口再重新打开 | `true` 或 `false` | `true` |

### IUnityUIFlowTestHostWindow（宿主窗口接口，设计提案，实现时确认）

```csharp
public interface IUnityUIFlowTestHostWindow
{
    void PrepareForAutomatedTest();
}
```

| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| PrepareForAutomatedTest | void 方法 | 可选实现 | 窗口由 YAML 运行器自动打开时，若窗口类型实现了该接口，执行器必须在打开后调用此方法 | 无参数；无返回值 | 无 |

含义：该方法用于重建界面、清空脏状态、重新绑定事件，确保窗口处于可自动化测试的初始状态。

---

## 3. CRUD 操作

| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 扫描 Examples YAML 目录 | `TestRunner.RunSuite("Assets/Examples/Yaml")` | 目录不存在；目录无 `.yaml` 文件 | `TestRunner.RunSuite` `(设计提案，实现时确认)` | 不涉及；只读扫描 |
| 解析 YAML 中的宿主窗口声明 | YAML 解析阶段读取 `fixture.host_window` | `type` 为空或不可解析 | `YamlTestCaseParser.ParseHostWindow` `(设计提案，实现时确认)` | 不涉及；只读解析 |
| 自动打开宿主窗口 | 执行器进入用例前、未传入 `rootOverride` 时 | 目标类型不继承 `EditorWindow`；窗口创建失败 | `HostWindowResolver.OpenHostWindow` `(设计提案，实现时确认)` | 不涉及；用例结束后自动关闭 |
| 调用 PrepareForAutomatedTest | 宿主窗口打开成功且实现了 `IUnityUIFlowTestHostWindow` | 窗口未实现该接口（跳过调用） | `HostWindowResolver.PrepareHost` `(设计提案，实现时确认)` | 不涉及 |
| 关闭自动打开的宿主窗口 | 用例执行结束后 | 窗口已被手动关闭 | `HostWindowResolver.CloseHostWindow` `(设计提案，实现时确认)` | 不涉及；关闭后不恢复先前状态 |

---

## 4. 交互规格

- 触发事件：`TestRunner.RunTestAsync` / `RunSuiteAsync` 在未显式传入 `rootOverride` 时，先检查 `fixture.host_window`。
- 状态变化：
  1. 若声明了宿主窗口，按 `type` 解析目标 `EditorWindow` 类型。
  2. 当 `reopen_if_open=true` 时，执行器必须先关闭该类型的所有已打开实例，再等待至少 1 帧后重新打开。
  3. 打开成功后，以该窗口的 `rootVisualElement` 作为测试根节点。
  4. 若窗口实现了 `IUnityUIFlowTestHostWindow`，执行器在打开后必须调用 `PrepareForAutomatedTest()`。
  5. 用例执行结束后，运行器负责关闭本次自动打开的宿主窗口，避免状态泄漏到后续用例。
- 数据提交时机：宿主窗口在 `fixture.setup` 之前完成打开和准备；在 `fixture.teardown` 之后关闭。
- 取消/回退：宿主窗口打开失败时，用例立即终止，不进入步骤执行阶段。

---

## 5. 视觉规格

不涉及

---

## 6. 校验规则

### 输入校验

| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| `fixture.host_window.type` 不能为空 | 解析 YAML fixture 后 | Error | `宿主窗口类型不能为空` |
| `host_window.type` 必须可解析为 `EditorWindow` 子类 | 解析目标类型时 | Error | `宿主窗口类型无法解析为 EditorWindow：{type}` |
| 宿主窗口必须能成功打开 | 调用 `EditorWindow.GetWindow` 后 | Error | `宿主窗口打开失败：{type}` |
| 自动打开的窗口必须有可用 `rootVisualElement` | 打开窗口并等待 1 帧后 | Error | `宿主窗口根节点缺失：{type}` |
| 用例未声明宿主窗口且运行器也拿不到默认根节点 | 检查可用根节点时 | Error | `无法获取测试根节点` |
| 每个示例 YAML 必须有对应的 UXML 文件 | 编写规范检查（非运行时） | Warning | `示例 YAML {name} 缺少对应 UXML 文件` |

### 错误响应

| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 宿主窗口类型为空 | HOST_WINDOW_TYPE_EMPTY | `宿主窗口类型不能为空` | 抛异常并终止当前用例 |
| 宿主窗口类型无法解析 | HOST_WINDOW_TYPE_INVALID | `宿主窗口类型无法解析为 EditorWindow：{type}` | 抛异常并终止当前用例 |
| 宿主窗口打开失败 | HOST_WINDOW_OPEN_FAILED | `宿主窗口打开失败：{type}` | 抛异常并终止当前用例 |
| 根节点缺失 | ROOT_ELEMENT_MISSING | `宿主窗口根节点缺失：{type}` | 抛异常并终止当前用例 |
| 宿主窗口关闭失败 | HOST_WINDOW_CLOSE_FAILED | `宿主窗口关闭失败：{type}，{detail}` | 记录 Warning 并继续后续用例 |

---

## 7. 跨模块联动

| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M01 用例编排与数据驱动 | 主动通知 | 在 `fixture` 中扩展 `host_window` 节点定义 | `FixtureDefinition`、`HostWindowConfig` `(设计提案)` |
| M03 执行引擎与运行控制 | 主动通知 | 为执行器增加自动宿主窗口解析、重开与释放能力 | `TestRunner.RunTestAsync`、`HostWindowResolver` `(设计提案)` |
| M05 动作系统与CSharp扩展 | 被动接收 | Examples YAML 直接复用现有内置动作与自定义动作能力 | `ActionRegistry.Resolve` |
| M09 测试基座与Fixture基类 | 主动通知 | 为 Fixture 以外的 YAML 运行模式补齐宿主来源定义 | `UnityUIFlowFixture<TWindow>`、`HostWindowResolver` `(设计提案)` |

---

## 8. 技术实现要点

- 关键类与职责：
  - `HostWindowResolver` `(设计提案，实现时确认)`：负责解析 `host_window.type` 为 `System.Type`，管理窗口创建、关闭与 `PrepareForAutomatedTest()` 调用。
  - `IUnityUIFlowTestHostWindow` `(设计提案，实现时确认)`：宿主窗口可选接口，提供自动化测试前的清理入口。
  - `ExamplesAcceptanceTests` `(已实现)`：`Assets/Examples/Tests/UnityUIFlow.ExamplesAcceptanceTests.cs`，覆盖 `Assets/Examples` 下示例 YAML 的端到端验收路径。

- 核心流程：

```text
TestRunner.RunTestAsync(yamlPath)
-> Parse YAML, extract fixture.host_window
-> If host_window declared AND rootOverride not provided:
   -> Resolve Type from host_window.type
   -> If reopen_if_open=true: close all instances of that type, wait >= 1 frame
   -> Open new window via EditorWindow.GetWindow<T>()
   -> Wait >= 1 frame for rootVisualElement ready
   -> If implements IUnityUIFlowTestHostWindow: call PrepareForAutomatedTest()
   -> Use window.rootVisualElement as test root
-> Execute fixture.setup -> steps -> fixture.teardown
-> Close auto-opened host window
```

- 编写规范：
  - 每个示例 YAML 只负责验证一个当前功能验收点。
  - 若一个功能需要多个断言步骤，仍视为同一个验收用例。
  - 不允许把多个无关功能拼接到一个大 YAML 中替代用例拆分。
  - 每个示例 YAML 必须有对应的测试 UXML，推荐命名为 `Example{Feature}Window.uxml`，与宿主窗口类名保持一致。
  - 允许多个示例共用一份 USS，也允许按示例拆分独立 USS。
  - CSV、JSON 数据文件应与使用它们的 YAML 放在同一目录，使用相对路径引用。
  - 数据驱动示例必须保持行级独立，不能依赖前一行执行结果。

- 性能约束：
  - 宿主窗口打开后至少等待 1 帧再读取 `rootVisualElement`，不允许同帧假定 UI 已就绪。
  - 用例结束后必须关闭本次自动打开的窗口，不允许跨用例保留窗口实例。

- 禁止项：
  - 禁止 Examples YAML 依赖外部显式传入的 `rootOverride`，必须能通过 `host_window` 自行打开宿主。
  - 禁止窗口初始化逻辑散落在 YAML 步骤中；必须收敛到 `PrepareForAutomatedTest()` 内。

---

## 9. 验收标准

1. [`Assets/Examples/Uxml/` 下存在与每个示例宿主窗口对应的 UXML 文件] -> [检查目录] -> [文件名与宿主窗口类名一致]
2. [`Assets/Examples/Yaml/` 下存在按功能拆分的多个验收 YAML] -> [检查目录] -> [每个 YAML 只覆盖一个功能验收点]
3. [某 YAML 声明了 `fixture.host_window.type`，且未传入 `rootOverride`] -> [执行用例] -> [自动打开对应宿主窗口并使用其 `rootVisualElement` 作为测试根节点执行]
4. [目标窗口已经打开且 `reopen_if_open=true`] -> [执行用例] -> [执行器先关闭旧窗口再重新打开，避免脏数据污染]
5. [在编辑器测试中逐个运行 Examples YAML] -> [执行完成] -> [所有用例均获得通过结果]
6. [宿主窗口类型不存在] -> [执行用例] -> [返回 `HOST_WINDOW_TYPE_INVALID`，用例立即终止]
7. [宿主窗口打开后无 `rootVisualElement`] -> [执行用例] -> [返回 `ROOT_ELEMENT_MISSING`，用例立即终止]

---

## 10. 边界规范

- 空数据：`Assets/Examples/Yaml/` 目录存在但无 YAML 文件时，套件返回 `total=0`，不抛异常。
- 单元素：仅 1 个示例 YAML 也必须能正常打开宿主窗口、执行、关闭。
- 上下限临界值：`reopen_if_open=true` 时必须至少等待 1 帧再重新打开；`reopen_if_open=false` 时直接复用已打开窗口（若存在）或新建（若不存在）。
- 异常数据恢复：宿主窗口关闭失败（如窗口已被手动关闭）时，记录 `HOST_WINDOW_CLOSE_FAILED` Warning 并继续后续用例，不阻断套件执行。

---

## 11. 周边可选功能

### 当前 Examples 覆盖矩阵

| YAML | 验收功能 |
| --- | --- |
| `01-basic-login.yaml` | `type_text_fast`、`click`、`assert_text_contains`、`screenshot` |
| `02-selectors-and-assertions.yaml` | 多种选择器、`assert_visible`、`assert_property`、`assert_text` |
| `03-wait-for-element.yaml` | `wait_for_element` |
| `04-conditional-and-loop.yaml` | `if.exists`、`repeat_while`、`wait`、`assert_not_visible` |
| `05-data-driven-csv.yaml` | CSV 数据驱动、`fixture.setup`、`fixture.teardown` |
| `06-custom-action-and-json.yaml` | JSON 数据驱动、自定义动作参数透传 |
| `07-double-click.yaml` | `double_click` |
| `08-press-key.yaml` | `press_key` |
| `09-hover.yaml` | `hover` |
| `10-drag.yaml` | `drag` |
| `11-scroll.yaml` | `scroll` |
| `12-type-text.yaml` | `type_text`、`assert_property` |
| `13-advanced-controls.yaml` | 基础高级控件验收：`select_option`、`toggle_foldout`、`set_slider`、`select_list_item`、`set_value`、`select_tree_item`、`select_tab` |
| `14-34` | 覆盖窗口扩展验收：绑定字段、复杂值类型、集合/列/布局、菜单与命令、修饰键输入、`menu_item`、`navigate_breadcrumb`、`set_split_view_size`、`page_scroller`、`drag_reorder`、`ObjectField search:`、数字字段、显示控件、`RepeatButton`、`ToolbarPopupSearchField` 输入本体等 |

补充口径：
- 当前 `Assets/Examples/Yaml/` 已扩展到 `01-34` 共 34 份 YAML。
- `UnityUIFlow.ExamplesAcceptanceTests` 已增加“自动扫描 Examples/Yaml 全目录”的验收入口，新 YAML 无需再逐个手写测试方法。
- 以当前代码基线核对，内置动作注册表中的全部 built-in actions 均已在 Examples YAML 中出现至少一次；此外还保留了 `custom_login` 作为自定义动作示例。

### 可选扩展

- P1：支持在 Examples 目录下按子目录分类管理验收 YAML（如 `Yaml/Actions/`、`Yaml/DataDriven/`），当前不预留子目录扫描策略。
- P1：新增官方 UI 驱动与 InputSystem 驱动对应的验收 YAML（`13-official-click.yaml` 等），待 M12 接入后补充。
- P2：支持 Examples YAML 自动生成覆盖矩阵报告；当前不预留报告模板。
