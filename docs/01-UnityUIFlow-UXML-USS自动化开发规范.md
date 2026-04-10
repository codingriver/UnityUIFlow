# UnityUIFlow UXML/USS 自动化友好开发规范

## 1. 目标

本文档用于约束基于 UIToolkit 的 `EditorWindow` 页面开发方式，让页面从一开始就适合被 `UnityUIFlow` 的 YAML 用例稳定驱动，减少以下问题：

- YAML 选择器经常失效
- 按钮能手点但自动化点不动
- 提示、弹层、状态文案难以断言
- 页面结构一改，批量用例大量报错

本文档只针对当前仓库里已经实现的 `UnityUIFlow` 能力，不讨论尚未支持的通用 Web/CSS 测试规范。

## 2. 当前项目里真正支持的选择器能力

`UnityUIFlow` 当前支持的选择器语法来自以下实现：

- `#id` 对应 `VisualElement.name`
- `.class` 对应 USS class
- `Button`、`Label`、`TextField` 这类类型选择器
- `[name=xxx]`
- `[tooltip=xxx]`
- `[data-xxx=yyy]`，值来自 `element.userData`
- 后代选择器：`#panel Button`
- 直接子元素选择器：`#panel > .item`
- 伪类：`:first-child`

当前不建议依赖的能力：

- 不要假设支持完整 CSS
- 不要假设支持复杂属性比较、模糊匹配、正则匹配
- 不要使用 `.yml`，当前文件入口按 `.yaml` 校验

项目内的高稳定性优先级建议如下：

1. `#name`
2. `[data-xxx=yyy]`
3. `[tooltip=xxx]`
4. `.class`
5. 类型选择器和层级组合
6. `:first-child`

原因：

- `#name` 在当前实现中有快速路径，查找最稳定、最快
- `.class` 更适合样式语义，不适合长期承担自动化主选择器
- `:first-child` 对结构改动最敏感，应尽量少用

## 3. UXML 命名规范

### 3.1 必须给关键交互元素设置 `name`

以下元素必须设置唯一 `name`：

- 输入框
- 按钮
- 标签页切换入口
- 状态标签
- toast、dialog、loading、empty state 的根节点
- 滚动容器
- 列表根节点
- 需要断言文本的 `Label`

推荐命名风格：

- 页面根节点：`xxx-root`
- 面板容器：`xxx-panel`
- 输入框：`username-input`、`search-input`
- 按钮：`login-button`、`save-button`
- 状态文本：`status-label`
- 弹层或 toast：`toast-message`、`confirm-dialog`
- 列表：`order-list`
- 列表项：`order-item-1`、`order-item-2`

要求：

- 全部使用小写短横线命名
- 一个窗口内 `name` 必须唯一
- 不要把自动生成编号作为主选择器，除非它天然稳定

### 3.2 `class` 负责样式，不负责主定位

`class` 推荐承担视觉职责，例如：

- `page-root`
- `page-panel`
- `primary-button`
- `danger-button`
- `form-row`

不推荐把 YAML 主选择器写成：

```yaml
selector: ".primary-button"
```

推荐写成：

```yaml
selector: "#save-button"
```

### 3.3 需要业务语义时，用 `userData` 提供 `data-*`

当前 `UnityUIFlow` 支持 `[data-xxx=yyy]`，但数据来源不是 UXML 原生属性，而是运行时在 C# 中写入 `element.userData`。

推荐用法：

```csharp
saveButton.userData = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["data-role"] = "primary",
};
```

然后 YAML 可写：

```yaml
selector: "[data-role=primary]"
```

适用场景：

- 同类元素很多，但业务角色明确
- 同一个 `UXML` 会复用到多个页面
- 需要给 agent 更强的语义锚点

## 4. UXML 结构规范

### 4.1 页面根结构保持清晰

推荐最小结构：

```xml
<ui:VisualElement name="feature-root" class="page-root">
    <ui:VisualElement name="feature-panel" class="page-panel">
        <!-- 表单 -->
        <!-- 操作按钮 -->
        <!-- 状态标签 -->
        <!-- 动态区域 -->
    </ui:VisualElement>
</ui:VisualElement>
```

建议：

- 每个功能块有独立容器
- 列表、弹层、toast 容器单独命名
- 不要为了排版把语义无关的空容器堆太深

### 4.2 对动态内容保留稳定宿主节点

推荐：

- 固定一个 `toast-host`
- 固定一个 `dialog-host`
- 固定一个 `result-panel`

这样即使动态元素会创建/销毁，自动化仍然能围绕稳定容器设计 YAML。

例如：

```xml
<ui:VisualElement name="toast-host" />
```

而不是让 toast 随机插到页面任意位置。

### 4.3 断言文本必须落在稳定元素上

推荐：

- 登录结果放在 `#status-label`
- 校验信息放在 `#form-error-label`
- 保存结果放在 `#save-result-label`

不推荐：

- 只在控制台打印结果
- 文案只出现在临时、无名元素里
- 一个标签既承担标题又承担状态输出

## 5. USS 规范

### 5.1 USS 不应影响元素可测试性

当前 `UnityUIFlow` 对可见性的判断依赖：

- `display != None`
- `visibility != Hidden`
- `opacity > 0`
- `panel != null`

因此要注意：

- `display: none` 的元素会被视为不可见
- `visibility: hidden` 的元素会被视为不可见
- `opacity: 0` 的元素会被视为不可见

如果 YAML 要用 `assert_visible` 或 `wait_for_element`，对应元素必须真的可见。

### 5.2 不要为了动画长期把关键元素保持在“不可见但存在”

不推荐：

- 状态标签长期 `opacity: 0`
- toast 永远在树里但默认 `visibility: hidden`

推荐：

- 要么显式切换为可见
- 要么创建后显示，用完后移除或 `display: none`

### 5.3 布局要保证可点击区域稳定

建议：

- 按钮不要被透明遮罩覆盖
- 不要把可点击元素缩到极小
- 不要依赖特殊层叠关系让按钮“看起来能点，实际命中别的元素”

这是因为 `click` 动作会基于元素实际位置发送指针/鼠标事件。

## 6. C# 交互接入规范

### 6.1 页面应实现稳定的自动化构建入口

如果页面通过 YAML `fixture.host_window` 打开，推荐实现：

- `EditorWindow`
- `IUnityUIFlowTestHostWindow`
- `PrepareForAutomatedTest()`

这样 `UnityUIFlow` 能在运行前统一构建页面并清理脏状态。

### 6.2 统一在 `BuildUi()` 中完成这几件事

推荐顺序：

1. 加载 `VisualTreeAsset`
2. 加载 `StyleSheet`
3. `rootVisualElement.Clear()`
4. 挂载样式
5. `CloneTree`
6. `Q<T>()` 获取关键控件
7. 校验关键控件不为空
8. 注册事件
9. 设置初始状态

### 6.3 按钮优先注册 `MouseUpEvent`

当前 `UnityUIFlow` 的 `click` 动作会派发指针和鼠标事件，并在样例窗口中优先通过 `MouseUpEvent` 驱动逻辑。

推荐：

```csharp
loginButton.RegisterCallback<MouseUpEvent>(_ => HandleLogin());
```

这样和当前自动化点击路径兼容性最好。

说明：

- 如果你使用 `Button.clicked`，理论上也可能工作
- 但从当前项目实践看，`MouseUpEvent` 是更直接、可控、已被样例验证的接法

### 6.4 输入框要使用标准可写字段

当前框架对 `type_text_fast` / `type_text` 的支持重点是：

- `TextField`
- 常见数值字段
- 带 `value` 可写属性的控件

推荐：

- 表单输入优先使用 `TextField`
- 不要把业务关键输入做成只读视觉壳，再靠额外逻辑同步

### 6.5 页面初始化必须可重复

重复打开窗口时，页面必须回到稳定初始状态。

例如：

- `status-label` 初始化为 `Idle`
- toast 默认隐藏或不存在
- 输入框默认清空
- 列表恢复默认项

否则批量执行和重复执行时容易相互污染。

## 7. 动态元素规范

### 7.1 Toast

推荐做法：

- 有独立宿主 `toast-host`
- toast 元素命名为 `toast-message`
- 出现时创建或显示
- 消失时移除或 `display: none`

推荐 YAML：

```yaml
- action: assert_visible
  selector: "#toast-message"
  timeout: "1s"

- repeat_while:
    condition:
      exists: "#toast-message"
    max_iterations: 20
    steps:
      - action: wait
        duration: "50ms"

- action: assert_not_visible
  selector: "#toast-message"
  timeout: "500ms"
```

### 7.2 Loading

推荐：

- 使用明确命名的 `loading-indicator`
- 显示和隐藏逻辑可预测
- 不要只靠遮罩 alpha 变化但永远不移除

### 7.3 列表

推荐：

- 列表根有 `name`
- 列表项具备稳定类名，例如 `.item`
- 若要断言第一项，可配合 `.item:first-child`

不推荐：

- 列表项完全匿名
- 依赖随机生成顺序

## 8. YAML 设计规范

### 8.1 优先使用 `#name`

推荐：

```yaml
selector: "#status-label"
```

不推荐：

```yaml
selector: "Label"
selector: ".status"
```

### 8.2 对动态场景先等待，再断言

推荐：

- 点击后有延迟出现的元素，先 `wait_for_element` 或 `assert_visible`
- 会消失的元素，用 `repeat_while + wait`

### 8.3 输入测试优先使用 `type_text_fast`

建议：

- 冒烟、回归用例优先 `type_text_fast`
- 需要观察逐字输入、调试节奏时再用 `type_text`

### 8.4 断言落到稳定结果上

推荐：

- 断言 `#status-label`
- 断言 `#error-label`
- 断言 `#toast-message`

不推荐：

- 断言临时布局文本
- 断言容易被视觉改版影响的装饰文案

## 9. 页面交付清单

新页面准备接入 `UnityUIFlow` 时，至少检查以下项目：

1. 关键输入、按钮、状态元素都设置了唯一 `name`
2. 页面根节点、主面板、动态宿主节点有清晰命名
3. 关键逻辑通过 `Q<T>(name)` 获取并绑定
4. 页面支持重复打开后恢复初始状态
5. 动态元素有稳定宿主和稳定名称
6. YAML 选择器优先使用 `#name`
7. 用例文件使用 `.yaml`
8. 至少有一个最小冒烟用例覆盖主流程

## 10. 推荐目录约定

建议一个页面至少包含：

- `Assets/YourFeature/Uxml/YourPageWindow.uxml`
- `Assets/YourFeature/Uss/YourPageWindow.uss`
- `Assets/YourFeature/Editor/YourPageWindow.cs`
- `Assets/YourFeature/Yaml/your-page-smoke.yaml`

如果页面要作为标准示例，可参考当前仓库：

- `Assets/Examples/Uxml`
- `Assets/Examples/Uss`
- `Assets/Examples/Yaml`
- `Assets/Examples/Editor`

## 11. 最重要的三条

如果你只记三条，请记这三条：

1. 关键元素一定要有稳定 `name`
2. 点击逻辑优先绑定 `MouseUpEvent`
3. 所有可断言结果都要落在稳定、命名明确的元素上
