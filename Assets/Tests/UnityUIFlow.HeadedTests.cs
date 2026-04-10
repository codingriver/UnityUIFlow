using System.Collections;
using System.IO;
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
    }
}
