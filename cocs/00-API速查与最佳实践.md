# UnityUIFlow API 速查与最佳实践

版本：1.0.0  
日期：2026-04-08  
状态：补充版

## 内置动作速查

| 动作 | 关键参数 | 说明 |
| --- | --- | --- |
| `click` | `selector` | 单击元素 |
| `double_click` | `selector` | 双击元素 |
| `type_text` | `selector`, `value` | 模拟键盘输入 |
| `type_text_fast` | `selector`, `value` | 直接写入文本值 |
| `press_key` | `key` | 发送按键 |
| `drag` | `from`, `to` | 拖拽元素或坐标 |
| `scroll` | `selector`, `delta` | 发送滚轮事件 |
| `hover` | `selector` | 悬停到目标元素 |
| `wait` | `duration` | 固定等待 |
| `wait_for_element` | `selector`, `timeout` | 等待目标元素出现 |
| `assert_visible` | `selector` | 断言元素可见 |
| `assert_not_visible` | `selector` | 断言元素不可见 |
| `assert_text` | `selector`, `expected` | 断言文本完全相等 |
| `assert_text_contains` | `selector`, `expected` | 断言文本包含目标片段 |
| `assert_property` | `selector`, `property`, `expected` | 断言属性值 |
| `screenshot` | `name` | 生成截图附件 |

## 选择器速查

| 语法 | 含义 |
| --- | --- |
| `#login-button` | 按 `element.name` 查找 |
| `.btn-primary` | 按 class 查找 |
| `Button` | 按类型名查找 |
| `[tooltip=Save]` | 按属性值查找 |
| `#panel .btn` | 后代选择器 |
| `#panel > .btn` | 直接子级选择器 |
| `.item:first-child` | 仅支持首子元素伪类 |

## 测试入口速查

| API | 说明 |
| --- | --- |
| `TestRunner.RunTest(yamlPath, options)` | 运行单个 YAML 用例 |
| `TestRunner.RunSuite(directory, options)` | 运行目录下所有 YAML 用例 |
| `UnityUIFlowFixture<TWindow>.ExecuteYamlSteps(yamlContent)` | 在 C# 测试中直接桥接 YAML 步骤 |

## CLI 参数速查

| 参数 | 说明 |
| --- | --- |
| `-unityUIFlow.testFilter` | 按 YAML 文件名或用例名过滤 |
| `-unityUIFlow.headed` | 开启或关闭 Headed 模式 |
| `-unityUIFlow.reportPath` | 指定报告输出目录 |
| `-unityUIFlow.screenshotOnFailure` | 控制失败是否自动截图 |

## 最佳实践

1. 为所有可测试元素设置稳定的 `name`，优先使用 `#name` 选择器。
2. 优先使用 `wait_for_element`，尽量避免固定 `wait`。
3. 对复杂交互优先写成自定义动作或 Page Object，不把复杂逻辑堆进 YAML。
4. 只在关键断言前和失败场景截图，避免 I/O 膨胀。
5. 数据驱动数据尽量外置，避免在一个 YAML 文件中堆叠过多重复步骤。
6. Headed 模式只用于开发调试，CI 默认使用无头模式。

## 已知限制

| 限制 | 建议规避方式 |
| --- | --- |
| 键盘输入兼容性依赖 `com.unity.test-framework` UI 测试子系统与 InputSystem | 对中文或复杂输入优先使用 `type_text_fast` |
| 自动等待能力有限 | 用 `wait_for_element` 显式声明等待点 |
| 拖拽细节依赖底层模拟器实现 | 在 M0/M1 先做原型验证 |
| 复杂条件表达式未进入 V1 | 用自定义动作承接复杂分支 |
| 截图当前为占位 PNG，非真实窗口截图 | 正式截图能力在 M3 阶段升级 |

## 高价值周边功能路线图

以下功能未进入 V1 强制范围，但具有较高的实用价值，建议在首版稳定后优先评估：

| 功能 | 优先级 | 说明 |
| --- | --- | --- |
| 失败步骤自动重试 | P1 | 支持配置重试次数，减少环境噪音导致的误报 |
| `assert_screenshot_matches` 视觉回归 | P1 | 对比截图与基准图，输出像素差异报告 |
| 步骤耗时记录与慢步骤告警 | P1 | 将每步执行时间写入报告，标注超阈值步骤 |
| Headed 面板显示当前驱动类型 | P1 | 实时显示当前步骤使用的驱动（官方 / InputSystem / fallback） |
| `Shift+Click`、`Ctrl+Click` 修饰键组合 | P1 | 通过 `com.unity.test-framework` UI 测试子系统修饰键 API 实现 |
| HTML 格式报告（内嵌截图缩略图） | P1 | 替代或补充当前 Markdown 报告，更适合 CI 可视化 |
| Watch 模式（文件变更自动重跑） | P2 | YAML 或动作代码变更后自动触发对应用例重跑 |
| 程序集白名单通过配置文件扩展 | P2 | 通过 `.unityuiflow.json` 扩展自定义动作扫描范围 |
| IME / 剪贴板 / 多设备输入 | P2 | 高级输入场景，IME 不纳入 V1 验收范围 |
| 并行套件执行 | P2 | 多 YAML 套件并发执行，需解决 EditorWindow 隔离问题 |

## 与 Playwright 的类比

| Playwright | UnityUIFlow |
| --- | --- |
| `page.locator('#btn')` | `finder.Find(\"#btn\", root)` |
| `locator.click()` | `click` 动作 |
| `page.fill()` | `type_text` 或 `type_text_fast` |
| `page.dragTo()` | `drag` 动作 |
| `page.waitForSelector()` | `wait_for_element` |
| `expect(locator).toHaveText()` | `assert_text` |
| `headed` | `HeadedTestWindow` |
