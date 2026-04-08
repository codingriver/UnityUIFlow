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
| 键盘输入兼容性依赖底层 UI 测试包 | 对中文或复杂输入优先使用 `type_text_fast` |
| 自动等待能力有限 | 用 `wait_for_element` 显式声明等待点 |
| 拖拽细节依赖底层模拟器实现 | 在 M0/M1 先做原型验证 |
| 复杂条件表达式未进入 V1 | 用自定义动作承接复杂分支 |

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
