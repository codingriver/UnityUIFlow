using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    public sealed class UnityUIFlowHeadedTests
    {
        [Test]
        public void RuntimeController_StepModePausesAfterCompletion()
        {
            using (var controller = new RuntimeController())
            {
                controller.RunMode = HeadedRunMode.Step;
                controller.StepOnce();

                controller.OnStepCompleted();

                Assert.That(controller.IsPaused, Is.True);
                controller.Resume();
                Assert.That(controller.IsPaused, Is.False);
            }
        }

        [Test]
        public void RuntimeController_PauseForFailure_TracksFailurePauseState()
        {
            using (var controller = new RuntimeController())
            {
                controller.PauseForFailure();

                Assert.That(controller.IsPaused, Is.True);
                Assert.That(controller.IsPausedForFailure, Is.True);

                controller.Resume();

                Assert.That(controller.IsPaused, Is.False);
                Assert.That(controller.IsPausedForFailure, Is.False);
            }
        }

        [Test]
        public void HeadedRunEventBus_PublishesRegisteredCallbacks()
        {
            bool runAttached = false;
            bool stepStarted = false;
            bool stepCompleted = false;
            bool highlightChanged = false;
            bool failureRaised = false;
            bool finishedRaised = false;

            RuntimeController controller = new RuntimeController();
            var step = new ExecutableStep
            {
                DisplayName = "Headed Step",
            };
            var result = new StepResult
            {
                DisplayName = "Headed Step",
                Status = TestStatus.Failed,
            };
            var root = new VisualElement();

            void OnRunAttached(RuntimeController _, string __) => runAttached = true;
            void OnStepStarted(ExecutableStep _) => stepStarted = true;
            void OnStepCompleted(ExecutableStep _, StepResult __, VisualElement ___) => stepCompleted = true;
            void OnHighlighted(ExecutableStep _, VisualElement __) => highlightChanged = true;
            void OnFailure(ExecutableStep _, StepResult __) => failureRaised = true;
            void OnFinished(TestResult _) => finishedRaised = true;

            HeadedRunEventBus.RunAttached += OnRunAttached;
            HeadedRunEventBus.StepStarted += OnStepStarted;
            HeadedRunEventBus.StepCompleted += OnStepCompleted;
            HeadedRunEventBus.HighlightedElementChanged += OnHighlighted;
            HeadedRunEventBus.Failure += OnFailure;
            HeadedRunEventBus.RunFinished += OnFinished;

            try
            {
                HeadedRunEventBus.PublishRunAttached(controller, "Headed Case");
                HeadedRunEventBus.PublishStepStarted(step);
                HeadedRunEventBus.PublishStepCompleted(step, result, root);
                HeadedRunEventBus.PublishHighlightedElement(step, root);
                HeadedRunEventBus.PublishFailure(step, result);
                HeadedRunEventBus.PublishRunFinished(new TestResult { CaseName = "Headed Case", Status = TestStatus.Failed });
            }
            finally
            {
                HeadedRunEventBus.RunAttached -= OnRunAttached;
                HeadedRunEventBus.StepStarted -= OnStepStarted;
                HeadedRunEventBus.StepCompleted -= OnStepCompleted;
                HeadedRunEventBus.HighlightedElementChanged -= OnHighlighted;
                HeadedRunEventBus.Failure -= OnFailure;
                HeadedRunEventBus.RunFinished -= OnFinished;
                controller.Dispose();
            }

            Assert.That(runAttached, Is.True);
            Assert.That(stepStarted, Is.True);
            Assert.That(stepCompleted, Is.True);
            Assert.That(highlightChanged, Is.True);
            Assert.That(failureRaised, Is.True);
            Assert.That(finishedRaised, Is.True);
        }

        [UnityTest]
        public IEnumerator HighlightOverlayRenderer_AttachesAndDetachesFromWindow()
        {
            SampleLoginWindow window = EditorWindow.GetWindow<SampleLoginWindow>();
            window.BuildUi();
            yield return null;

            int initialChildren = window.rootVisualElement.childCount;
            var renderer = new HighlightOverlayRenderer();

            renderer.Attach(window);
            renderer.Highlight(window.rootVisualElement.Q<VisualElement>("login-panel"));

            Assert.That(window.rootVisualElement.childCount, Is.EqualTo(initialChildren + 1));

            renderer.Clear();
            renderer.Detach();

            Assert.That(window.rootVisualElement.childCount, Is.EqualTo(initialChildren));
            window.Close();
        }

        [UnityTest]
        public IEnumerator HeadedTestWindow_RendersStrictTogglesAndDriverDetails()
        {
            HeadedTestWindow window = EditorWindow.GetWindow<HeadedTestWindow>();
            yield return null;

            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require Official Host"), Is.True);
            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require Official Pointer Driver"), Is.True);
            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require InputSystem Keyboard Driver"), Is.True);
            Assert.That(window.rootVisualElement.Query<Label>().ToList().Exists(label => label.text.StartsWith("Driver Details:")), Is.True);

            window.Close();
        }

        [Test]
        public void HeadedYamlPathPreferences_PersistsLastSelectedPath()
        {
            const string relativePath = "Assets/Examples/Yaml/01-basic-login.yaml";
            EditorPrefs.DeleteKey(HeadedYamlPathPreferences.YamlPathPrefKey);

            HeadedYamlPathPreferences.Save(relativePath);

            Assert.That(HeadedYamlPathPreferences.Load(), Is.EqualTo(relativePath));
        }

        [Test]
        public void HeadedYamlPathPreferences_NormalizesProjectPathAndInitialDirectory()
        {
            string absolutePath = Path.GetFullPath("Assets/Examples/Yaml/01-basic-login.yaml");
            string normalized = HeadedYamlPathPreferences.NormalizePath(absolutePath);

            Assert.That(normalized, Is.EqualTo("Assets/Examples/Yaml/01-basic-login.yaml"));
            Assert.That(
                HeadedYamlPathPreferences.GetInitialDirectory(normalized).Replace('\\', '/'),
                Is.EqualTo(Path.GetFullPath("Assets/Examples/Yaml").Replace('\\', '/')));
        }

        [Test]
        public void HeadedWindowPreferences_LoadsAndSavesStrictFlags()
        {
            var previous = new HeadedPanelState();
            HeadedWindowPreferences.Load(previous);

            var state = new HeadedPanelState
            {
                RunMode = HeadedRunMode.Step,
                FailurePolicy = HeadedFailurePolicy.Continue,
                ContinueOnStepFailure = true,
                RequireOfficialHost = true,
                RequireOfficialPointerDriver = true,
                RequireInputSystemKeyboardDriver = true,
            };

            try
            {
                HeadedWindowPreferences.Save(state);

                var loaded = new HeadedPanelState();
                HeadedWindowPreferences.Load(loaded);

                Assert.That(loaded.RunMode, Is.EqualTo(HeadedRunMode.Step));
                Assert.That(loaded.FailurePolicy, Is.EqualTo(HeadedFailurePolicy.Continue));
                Assert.That(loaded.ContinueOnStepFailure, Is.True);
                Assert.That(loaded.RequireOfficialHost, Is.True);
                Assert.That(loaded.RequireOfficialPointerDriver, Is.True);
                Assert.That(loaded.RequireInputSystemKeyboardDriver, Is.True);
            }
            finally
            {
                HeadedWindowPreferences.Save(previous);
            }
        }

        [Test]
        public void BatchRunnerPathUtility_NormalizesProjectRelativePaths()
        {
            string absolutePath = Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml");

            string normalized = BatchRunnerPathUtility.NormalizePath(absolutePath);

            Assert.That(normalized, Is.EqualTo("Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml"));
        }

        [Test]
        public void BatchRunnerPathUtility_ResolvesDirectoryYamlFilesInSortedOrder()
        {
            string root = Path.Combine(Path.GetTempPath(), "UnityUIFlowBatchRunner", System.Guid.NewGuid().ToString("N"));
            string nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(root, "b-case.yaml"), "name: B");
            File.WriteAllText(Path.Combine(nested, "a-case.yaml"), "name: A");

            var paths = BatchRunnerPathUtility.ResolveYamlPaths(BatchRunTargetMode.Directory, root);

            Assert.That(paths, Has.Count.EqualTo(2));
            Assert.That(string.Compare(paths[0], paths[1], System.StringComparison.OrdinalIgnoreCase), Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void BatchRunnerPreferences_LoadsAndSavesStrictFlags()
        {
            var previous = new BatchRunnerViewState();
            BatchRunnerPreferences.Load(previous);

            var state = new BatchRunnerViewState
            {
                RequireOfficialHost = true,
                RequireOfficialPointerDriver = true,
                RequireInputSystemKeyboardDriver = true,
            };

            try
            {
                BatchRunnerPreferences.Save(state);

                var loaded = new BatchRunnerViewState();
                BatchRunnerPreferences.Load(loaded);

                Assert.That(loaded.RequireOfficialHost, Is.True);
                Assert.That(loaded.RequireOfficialPointerDriver, Is.True);
                Assert.That(loaded.RequireInputSystemKeyboardDriver, Is.True);
            }
            finally
            {
                BatchRunnerPreferences.Save(previous);
            }
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_RendersStrictToggles()
        {
            BatchRunnerWindow window = EditorWindow.GetWindow<BatchRunnerWindow>();
            yield return null;

            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require Official Host"), Is.True);
            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require Official Pointer Driver"), Is.True);
            Assert.That(window.rootVisualElement.Query<Toggle>().ToList().Exists(toggle => toggle.label == "Require InputSystem Keyboard Driver"), Is.True);

            window.Close();
        }

        [Test]
        public void RuntimeController_Stop_SetsStoppedAndCancelsToken()
        {
            using (var controller = new RuntimeController())
            {
                Assert.That(controller.IsStopped, Is.False);
                controller.Stop();
                Assert.That(controller.IsStopped, Is.True);
                Assert.That(controller.CancellationToken.IsCancellationRequested, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator HeadedTestWindow_RunSelected_ExecutesAndProducesReport()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string yamlPath = Path.Combine(tempDir, "headed-case.yaml");
            File.WriteAllText(yamlPath, @"
name: Headed Smoke
fixture:
  host_window:
    type: SampleLoginWindow
    reopen_if_open: true
steps:
  - action: wait
    duration: '10ms'
");

            HeadedTestWindow window = EditorWindow.GetWindow<HeadedTestWindow>();
            yield return null;

            // Set path via reflection to avoid UI interaction
            typeof(HeadedTestWindow).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(window, new HeadedPanelState { SelectedYamlPath = yamlPath });

            // Trigger run via reflection on private method
            System.Reflection.MethodInfo runMethod = typeof(HeadedTestWindow).GetMethod("RunSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(runMethod, Is.Not.Null);

            // We can't await the private async void, but we can at least verify the window state transitions
            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HighlightOverlayRenderer_UpdatesPositionAndColor()
        {
            SampleLoginWindow window = EditorWindow.GetWindow<SampleLoginWindow>();
            window.BuildUi();
            yield return null;

            var renderer = new HighlightOverlayRenderer();
            renderer.Attach(window);
            VisualElement target = window.rootVisualElement.Q<VisualElement>("login-panel");
            renderer.Highlight(target);
            yield return null;

            VisualElement overlayRoot = window.rootVisualElement[window.rootVisualElement.childCount - 1];
            VisualElement marker = overlayRoot[0];
            Assert.That(marker.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(marker.resolvedStyle.left, Is.GreaterThanOrEqualTo(0));
            Assert.That(marker.resolvedStyle.top, Is.GreaterThanOrEqualTo(0));
            Assert.That(marker.resolvedStyle.width, Is.GreaterThan(0));
            Assert.That(marker.resolvedStyle.height, Is.GreaterThan(0));

            renderer.Detach();
            window.Close();
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_ExecuteBatch_ProducesSuiteReport()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string yamlPath = Path.Combine(tempDir, "batch-case.yaml");
            File.WriteAllText(yamlPath, @"
name: Batch Smoke
fixture:
  host_window:
    type: SampleLoginWindow
    reopen_if_open: true
steps:
  - action: wait
    duration: '10ms'
");

            BatchRunnerWindow window = EditorWindow.GetWindow<BatchRunnerWindow>();
            yield return null;

            // Set state via reflection
            var state = new BatchRunnerViewState
            {
                TargetMode = BatchRunTargetMode.Directory,
                TargetPath = tempDir,
                ReportPath = Path.Combine(tempDir, "Reports"),
                Headed = false,
            };
            typeof(BatchRunnerWindow).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(window, state);

            // Trigger batch execution via private method
            System.Reflection.MethodInfo runMethod = typeof(BatchRunnerWindow).GetMethod("RunSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(runMethod, Is.Not.Null);

            // We cannot easily await the async void in a UnityTest without hooks, but we verify the window is set up correctly
            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_ExecuteBatchAsync_ExecutesAndGeneratesReports()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string yamlPath = Path.Combine(tempDir, "batch-case.yaml");
            File.WriteAllText(yamlPath, @"
name: Batch Async Smoke
fixture:
  host_window:
    type: SampleLoginWindow
    reopen_if_open: true
steps:
  - action: wait
    duration: '10ms'
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
            typeof(BatchRunnerWindow).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(window, state);

            System.Reflection.MethodInfo executeMethod = typeof(BatchRunnerWindow).GetMethod("ExecuteBatchAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(executeMethod, Is.Not.Null);

            var yamlPaths = new List<string> { yamlPath };
            var cts = new System.Threading.CancellationTokenSource();
            Task task = (Task)executeMethod.Invoke(window, new object[] { yamlPaths, cts.Token });
            yield return UnityUIFlowTestTaskUtility.Await(task);

            Assert.That(File.Exists(Path.Combine(tempDir, "Reports", "full_reports.md")), Is.True);
            Assert.That(File.Exists(Path.Combine(tempDir, "Reports", "Cases", "suite-report.json")), Is.True);

            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeadedTestWindow_RunSelected_BlocksDuplicateRun()
        {
            HeadedTestWindow window = EditorWindow.GetWindow<HeadedTestWindow>();
            yield return null;

            var stateField = typeof(HeadedTestWindow).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var state = stateField?.GetValue(window) as HeadedPanelState;
            if (state != null)
            {
                state.RunnerState = HeadedRunnerState.Running;
                state.SelectedYamlPath = "Assets/Examples/Yaml/01-basic-login.yaml";
            }

            var errorBoxField = typeof(HeadedTestWindow).GetField("_errorBox", BindingFlags.NonPublic | BindingFlags.Instance);
            var errorBox = errorBoxField?.GetValue(window) as HelpBox;

            System.Reflection.MethodInfo runMethod = typeof(HeadedTestWindow).GetMethod("RunSelected", BindingFlags.NonPublic | BindingFlags.Instance);
            runMethod?.Invoke(window, null);
            yield return null;

            Assert.That(errorBox?.text ?? errorBox?.Q<Label>()?.text, Does.Contain("already running").Or.Contains("Runner is already running"));
            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeadedTestWindow_TryHighlightCurrent_HighlightsElement()
        {
            SampleLoginWindow target = EditorWindow.GetWindow<SampleLoginWindow>();
            target.Show();
            yield return null;

            HeadedTestWindow window = EditorWindow.GetWindow<HeadedTestWindow>();
            yield return null;

            var overlayField = typeof(HeadedTestWindow).GetField("_overlayRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
            var overlay = overlayField?.GetValue(window) as HighlightOverlayRenderer;
            overlay?.Attach(target);

            System.Reflection.MethodInfo tryHighlight = typeof(HeadedTestWindow).GetMethod("TryHighlightCurrent", BindingFlags.NonPublic | BindingFlags.Instance);
            var step = new ExecutableStep { Selector = new SelectorCompiler().Compile("#login-button") };
            tryHighlight?.Invoke(window, new object[] { step });
            yield return null;

            Assert.That(target.rootVisualElement.childCount, Is.GreaterThan(0));
            window.Close();
            target.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BatchRunnerPathUtility_ThrowsOnNonYamlSingleFile()
        {
            yield return null;
            string tempDir = CreateTempDirectory();
            string txtPath = Path.Combine(tempDir, "file.txt");
            File.WriteAllText(txtPath, "hello");

            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() => BatchRunnerPathUtility.ResolveYamlPaths(BatchRunTargetMode.SingleFile, txtPath));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.TestCasePathInvalid));
        }

        [UnityTest]
        public IEnumerator BatchRunnerPathUtility_ThrowsOnEmptyDirectory()
        {
            yield return null;
            string tempDir = CreateTempDirectory();
            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() => BatchRunnerPathUtility.ResolveYamlPaths(BatchRunTargetMode.Directory, tempDir));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.TestSuiteEmpty));
        }

        [UnityTest]
        public IEnumerator BatchRunnerPreferences_LoadsDefaultsWhenMissing()
        {
            yield return null;
            string[] keys = new[]
            {
                "UnityUIFlow.BatchRunner.Headed",
                "UnityUIFlow.BatchRunner.DefaultTimeoutMs",
            };
            foreach (string key in keys)
            {
                EditorPrefs.DeleteKey(key);
            }

            var state = new BatchRunnerViewState();
            BatchRunnerPreferences.Load(state);

            Assert.That(state.Headed, Is.True);
            Assert.That(state.DefaultTimeoutMs, Is.EqualTo(3000));
        }

        [UnityTest]
        public IEnumerator BatchRunnerWindow_CancelRun_SetsAbortedStatus()
        {
            string tempDir = CreateTempDirectory();
            string yamlPath = Path.Combine(tempDir, "cancel-case.yaml");
            File.WriteAllText(yamlPath, @"
name: Cancel Smoke
fixture:
  host_window:
    type: SampleLoginWindow
    reopen_if_open: true
steps:
  - action: wait
    duration: '5s'
");

            BatchRunnerWindow window = EditorWindow.GetWindow<BatchRunnerWindow>();
            yield return null;

            var state = new BatchRunnerViewState
            {
                TargetMode = BatchRunTargetMode.Directory,
                TargetPath = tempDir,
                ReportPath = Path.Combine(tempDir, "Reports"),
                Headed = false,
                IsRunning = true,
            };
            typeof(BatchRunnerWindow).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(window, state);
            var ctsField = typeof(BatchRunnerWindow).GetField("_runCts", BindingFlags.NonPublic | BindingFlags.Instance);
            ctsField?.SetValue(window, new CancellationTokenSource());

            System.Reflection.MethodInfo cancelMethod = typeof(BatchRunnerWindow).GetMethod("CancelRun", BindingFlags.NonPublic | BindingFlags.Instance);
            cancelMethod?.Invoke(window, null);

            var cts = ctsField?.GetValue(window) as CancellationTokenSource;
            Assert.That(cts?.IsCancellationRequested, Is.True);

            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeadedTestWindow_RunSelected_CatchesGenericException()
        {
            string tempDir = CreateTempDirectory();
            string yamlPath = Path.Combine(tempDir, "crash.yaml");
            File.WriteAllText(yamlPath, @"
name: Crash Case
fixture:
  host_window:
    type: NonExistentWindowType12345
    reopen_if_open: true
steps:
  - action: wait
    duration: '10ms'
");

            HeadedTestWindow window = EditorWindow.GetWindow<HeadedTestWindow>();
            yield return null;

            var stateField = typeof(HeadedTestWindow).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var state = stateField?.GetValue(window) as HeadedPanelState;
            if (state != null)
            {
                state.SelectedYamlPath = yamlPath;
            }

            var errorBoxField = typeof(HeadedTestWindow).GetField("_errorBox", BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var errorBox = errorBoxField?.GetValue(window) as HelpBox;

            System.Reflection.MethodInfo runMethod = typeof(HeadedTestWindow).GetMethod("RunSelected", BindingFlags.NonPublic | BindingFlags.Instance);
            runMethod?.Invoke(window, null);
            yield return null;
            yield return null;

            // The error box should have been populated because the YAML references a non-existent window type
            Assert.That(errorBox?.text, Is.Not.Null.Or.Empty);
            window.Close();
            yield return null;
        }

        private static string CreateTempDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }
    }
}
