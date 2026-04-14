using System.Collections;
using System.IO;
using System.Linq;
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
    }
}
