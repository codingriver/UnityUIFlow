## 1. 模块职责

- 负责：提供 Editor 内 Headed 运行面板、当前步骤展示、元素高亮、步进/连续模式、失败暂停与现场保留。
- 负责：作为调试入口承接“运行/暂停/步进/停止”等人工控制。
- 不负责：解析 YAML、不负责执行底层 UI 动作、不负责生成最终 CI 报告。
- 输入/输出：输入为当前运行状态、步骤事件、目标元素、用户点击操作；输出为 UI 状态更新、运行控制命令、高亮绘制状态。

## 2. 数据模型
| 字段名 | 类型 | 必填/可选 | 语义 | 合法范围 | 默认值 |
| --- | --- | --- | --- | --- | --- |
| selectedYamlPath | string | 可选 | 当前选中的 YAML 路径 | `null` 表示未选择文件 | `null` |
| runMode | string | 必填 | 执行模式 | 仅允许 `Continuous`、`Step` | `Continuous` |
| runnerState | string | 必填 | 面板运行状态 | 仅允许 `Idle`、`Running`、`Paused`、`Failed`、`Stopped` | `Idle` |
| currentStepName | string | 可选 | 当前步骤名称 | `null` 表示当前无活动步骤 | `null` |
| currentSelector | string | 可选 | 当前步骤选择器 | `null` 表示当前步骤无选择器 | `null` |
| lastErrorMessage | string | 可选 | 最近错误消息 | `null` 表示当前无错误 | `null` |
| highlightColor | string | 必填 | 高亮填充色 | 固定为 `rgba(255,0,0,0.20)` | `rgba(255,0,0,0.20)` |
| outlineColor | string | 必填 | 高亮描边色 | 固定为 `#FF0000` | `#FF0000` |
| failurePolicy | string | 必填 | 失败后的面板行为 | 仅允许 `Pause`、`Continue` | `Pause` |
| retainSceneOnFailure | bool | 必填 | 失败时是否保留现场 | `true` 或 `false` | `true` |

## 3. CRUD 操作
| 操作 | 入口 | 禁用条件 | 实现标识 | Undo语义 |
| --- | --- | --- | --- | --- |
| 打开 Headed 面板 | Unity 菜单 `UnityUIFlow > Headed Test Runner` | 当前不是 Editor 环境 | `HeadedTestWindow.Open` `(设计提案，实现时确认)` | 不涉及；UI 打开关闭不进历史 |
| 运行选中文件 | 面板点击“运行” | 未选择 YAML；当前已有运行任务 | `HeadedTestWindow.RunSelected` `(设计提案，实现时确认)` | 不涉及；运行后可停止，不支持 Undo |
| 暂停执行 | 面板点击“暂停” | 当前不是 `Running` | `RuntimeController.Pause` `(设计提案，实现时确认)` | 不涉及；暂停仅切换状态 |
| 单步执行下一步 | 面板点击“下一步” | `runMode != Step`；当前不是 `Paused` | `RuntimeController.StepOnce` `(设计提案，实现时确认)` | 不涉及；推进一步后不可回退 |
| 停止执行 | 面板点击“停止” | 当前不是 `Running` 或 `Paused` | `RuntimeController.Stop` `(设计提案，实现时确认)` | 不涉及；停止后不能恢复到中断点继续 |

## 4. 交互规格

- 触发事件：用户打开面板、选择 YAML、点击运行控制按钮，或执行引擎发布步骤事件。
- 状态变化：`Idle -> Running -> Paused -> Running -> Failed/Stopped/Idle`。
- 数据提交时机：步骤开始事件到达时立即刷新当前步骤名与高亮目标；步骤完成事件到达时刷新状态与日志；失败事件到达时按 `failurePolicy` 进入 `Paused` 或 `Running`。
- 取消/回退：点击“停止”后，面板立即禁用“下一步”和“暂停”；当前高亮在收到停止完成事件后清空。
- 步进模式：`runMode=Step` 时，每完成 1 步都自动进入 `Paused`；只有用户点击“下一步”才继续下 1 步。
- 失败策略：`failurePolicy=Pause` 时，失败后立即暂停并保留高亮目标；`Continue` 时记录错误但面板不切换为暂停。

## 5. 视觉规格
| 状态 | 背景色 | 边框 | 文字颜色 | 备注 |
| --- | --- | --- | --- | --- |
| 正常 | `#1E1E1E` | `1px solid #3C3C3C` | `#F0F0F0` | 面板空闲态 |
| Hover | `#2A2A2A` | `1px solid #5A5A5A` | `#FFFFFF` | 按钮悬停态 |
| Selected | `#2C4F7C` | `1px solid #7AB0FF` | `#FFFFFF` | 当前选中文件或当前模式 |
| Disabled | `#2B2B2B` | `1px solid #404040` | `#7A7A7A` | 禁用按钮 |
| 错误 | `#4A1F1F` | `1px solid #FF5A5A` | `#FFEAEA` | 失败消息区域与失败暂停状态 |

## 6. 校验规则
### 输入校验
| 规则 | 检查时机 | 级别 | 提示文案 |
| --- | --- | --- | --- |
| 运行前必须选中 YAML 文件 | 点击“运行”时 | Error | `请先选择一个 YAML 用例文件` |
| 步进按钮仅在 `runMode=Step` 且当前为暂停态可用 | 刷新面板按钮状态时 | Error | `当前状态不允许单步执行` |
| 高亮元素必须仍在有效面板中 | 每次重绘前 | Warning | `当前高亮元素已失效，已自动清空高亮` |
| Headed 模式禁止在 `-batchmode` 环境中强制开启 | 运行前环境检查时 | Error | `当前环境不支持 Headed 模式` |
| 失败消息长度超过 500 字时只显示前 500 字 | 渲染错误区域时 | Info | `错误信息已截断显示` |

### 错误响应
| 错误场景 | 错误码 | 错误消息模板 | 恢复行为 |
| --- | --- | --- | --- |
| 未选择 YAML 即点击运行 | HEADED_FILE_NOT_SELECTED | `未选择 YAML 用例文件` | 在面板显示错误，不启动运行 |
| Headed 模式环境不可用 | HEADED_ENVIRONMENT_UNSUPPORTED | `当前环境不支持 Headed 模式` | 在面板显示错误并保持 `Idle` |
| 高亮元素失效 | HEADED_TARGET_INVALID | `当前高亮目标已失效` | 清空高亮并继续运行 |
| 用户在无运行任务时点击暂停/停止 | HEADED_INVALID_TRANSITION | `当前状态不允许执行该操作：{action}` | 静默忽略并保持当前状态 |
| 面板与运行控制状态不同步 | HEADED_STATE_OUT_OF_SYNC | `Headed 面板状态不同步：{detail}` | 回退到 `Idle` 并提示重新运行 |

## 7. 跨模块联动
| 模块 | 方向 | 说明 | 代码依赖点 |
| --- | --- | --- | --- |
| M03 执行引擎与运行控制 | 被动接收 | 通过运行控制器驱动暂停、继续、单步、停止 | `RuntimeController.Pause`、`RuntimeController.StepOnce`、`RuntimeController.Stop` `(设计提案)` |
| M04 元素定位与等待 | 被动接收 | 获取当前命中元素用于高亮绘制 | `HeadedRunEventBus.PublishHighlightedElement` `(设计提案)` |
| M07 报告与截图 | 主动通知 | 失败暂停时触发失败截图与错误消息写入 | `ScreenshotManager.CaptureAsync`、`MarkdownReporter.RecordStepResult` `(设计提案)` |
| M08 命令行与CI集成 | 被动接收 | 当 `headed=false` 时完全不创建面板实例 | `TestOptions.Headed` `(设计提案)` |

## 8. 技术实现要点

- 关键类与职责：
  - `HeadedTestWindow` `(设计提案，实现时确认)`：Editor 面板主体。
  - `HeadedRunEventBus` `(设计提案，实现时确认)`：接收执行引擎广播的步骤事件。
  - `HighlightOverlayRenderer` `(设计提案，实现时确认)`：绘制目标元素高亮。
  - `RuntimeController` `(设计提案，实现时确认)`：处理面板控制命令。
- 核心流程：
```text
Open HeadedTestWindow
-> Select yaml file
-> Click Run
-> RuntimeController starts test
-> Engine publishes current step + highlighted element
-> Window repaints
-> On failure, pause or continue according to failurePolicy
```
- 性能约束：
  - 面板重绘频率不超过 Editor 正常重绘频率，禁止额外 `while` 轮询。
  - 高亮绘制必须只针对当前元素，禁止遍历整棵 UI 树做全量绘制。
  - 失败日志区最多保留最近 `200` 行文本。
  - Headed 关闭时不得订阅运行事件，避免无头模式产生额外开销。
- 禁止项：
  - 禁止直接修改被测 `VisualElement` 的样式来实现高亮。
  - 禁止把运行控制状态存为未清理的静态全局单例。
- TODO(待确认)：Editor 内坐标换算、叠加绘制方式和 `GUIClip.Unclip` 可用性需原型验证。

## 9. 验收标准
1. [Editor 环境已打开项目] -> [执行菜单 `UnityUIFlow > Headed Test Runner`] -> [成功打开 Headed 面板]
2. [已选择合法 YAML，模式为连续] -> [点击“运行”] -> [用例自动连续执行到结束]
3. [已选择合法 YAML，模式为步进] -> [点击“运行”] -> [第 1 步执行后自动暂停，点击“下一步”后继续第 2 步]
4. [当前步骤命中某个元素] -> [面板收到步骤事件] -> [目标元素区域显示红色填充和红色描边高亮]
5. [步骤失败且 `failurePolicy=Pause`] -> [执行用例] -> [面板进入 `Paused`，保留失败元素高亮和错误信息]
6. [Headed 模式关闭] -> [执行无头运行] -> [不创建窗口、不绘制高亮]

## 10. 边界规范

- 空数据：未选择文件时面板允许打开和切换模式，但不允许启动运行。
- 单元素：只有 1 个步骤的用例在步进模式下仍必须先执行该步，再进入暂停或完成态。
- 上下限临界值：错误消息显示区最大展示 `500` 字；日志列表保留上限 `200` 行。
- 异常数据恢复：高亮元素在重绘前失效时，必须自动清空高亮并保持面板可继续操作。

## 11. 周边可选功能

- P1：支持步骤时间线面板；当前预留 `HeadedRunEvent.timestamp` 字段。
- P1：支持面板内搜索 YAML 文件；当前预留文件列表数据源接口。
- P2：支持录制执行过程为 GIF/视频；当前不预留编码实现。
