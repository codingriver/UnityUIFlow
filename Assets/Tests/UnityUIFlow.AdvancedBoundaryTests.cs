using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    public sealed class UnityUIFlowAdvancedBoundaryTests : UnityUIFlowFixture<SampleLoginWindow>
    {
        private class FailingScreenshotManager : ScreenshotManager
        {
            public FailingScreenshotManager() : base(new TestOptions { ScreenshotPath = Path.GetTempPath() }) { }
            public override async Task<string> CaptureAsync(string caseName, int stepIndex, string tag, CancellationToken cancellationToken)
            {
                await Task.Yield();
                throw new System.InvalidOperationException("screenshot-failure");
            }
        }

        [UnityTest]
        public IEnumerator StepExecutor_CapturesScreenshotExceptionInErrorMessage()
        {
            yield return null;
            var step = new ExecutableStep
            {
                DisplayName = "Fail With Screenshot Error",
                ActionName = "assert_text",
                Parameters = new Dictionary<string, string>
                {
                    ["selector"] = "#status-label",
                    ["expected"] = "ThisWillFail",
                },
                TimeoutMs = 500,
            };

            var context = new ExecutionContext
            {
                Root = Root,
                Finder = new ElementFinder(),
                Options = new TestOptions { ScreenshotOnFailure = true },
                ActionRegistry = new ActionRegistry(),
                CancellationToken = CancellationToken.None,
                CaseName = "ScreenshotExceptionTest",
                ScreenshotManager = new FailingScreenshotManager(),
            };

            Task<StepResult> task = new StepExecutor().ExecuteStepAsync(step, context, 1);
            yield return UnityUIFlowTestTaskUtility.Await(task, result =>
            {
                Assert.That(result.Status, Is.EqualTo(TestStatus.Failed));
                Assert.That(result.ErrorMessage, Does.Contain("screenshot-failure"));
            });
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_RendersFailureCard()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "fail.yaml"), @"
name: Fail Case
fixture:
  host_window:
    type: SampleLoginWindow
    reopen_if_open: true
steps:
  - action: assert_text
    selector: '#status-label'
    expected: 'DefinitelyNotThisText'
");

            BatchRunnerWindow window = EditorWindow.GetWindow<BatchRunnerWindow>();
            yield return null;

            var state = new BatchRunnerViewState
            {
                TargetMode = BatchRunTargetMode.Directory,
                TargetPath = tempDir,
                ReportPath = Path.Combine(tempDir, "Reports"),
                Headed = false,
            };
            typeof(BatchRunnerWindow).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(window, state);

            System.Reflection.MethodInfo executeMethod = typeof(BatchRunnerWindow).GetMethod("ExecuteBatchAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var yamlPaths = new List<string> { Path.Combine(tempDir, "fail.yaml") };
            Task batchTask = (Task)executeMethod.Invoke(window, new object[] { yamlPaths, CancellationToken.None });
            yield return UnityUIFlowTestTaskUtility.Await(batchTask);

            var resultsScroll = typeof(BatchRunnerWindow).GetField("_resultsScrollView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(window) as ScrollView;
            Assert.That(resultsScroll?.childCount, Is.GreaterThan(0));

            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_ExecuteBatchAsync_HandlesGenericException()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            // Write a non-yaml file to cause a parse error, which gets caught in the batch loop
            File.WriteAllText(Path.Combine(tempDir, "bad.yaml"), "not valid yaml: [");

            BatchRunnerWindow window = EditorWindow.GetWindow<BatchRunnerWindow>();
            yield return null;

            var state = new BatchRunnerViewState
            {
                TargetMode = BatchRunTargetMode.Directory,
                TargetPath = tempDir,
                ReportPath = Path.Combine(tempDir, "Reports"),
                Headed = false,
            };
            typeof(BatchRunnerWindow).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(window, state);

            System.Reflection.MethodInfo executeMethod = typeof(BatchRunnerWindow).GetMethod("ExecuteBatchAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var yamlPaths = new List<string> { Path.Combine(tempDir, "bad.yaml") };
            Task batchTask = (Task)executeMethod.Invoke(window, new object[] { yamlPaths, CancellationToken.None });
            yield return UnityUIFlowTestTaskUtility.Await(batchTask);

            // The batch should complete without crashing, and the suite report should reflect the error
            Assert.That(File.Exists(Path.Combine(tempDir, "Reports", "suite-report.md")), Is.True);

            window.Close();
            yield return null;
        }
    }
}
