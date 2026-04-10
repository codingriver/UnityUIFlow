using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    public sealed class UnityUIFlowLocatorFixtureTests : UnityUIFlowFixture<SampleLoginWindow>
    {
        [UnityTest]
        public IEnumerator Finder_FindsElementsByMultipleSelectorTypes()
        {
            yield return null;

            var compiler = new SelectorCompiler();

            Assert.That(Finder.Find(compiler.Compile("#login-button"), Root).Element, Is.Not.Null);
            Assert.That(Finder.Find(compiler.Compile(".item:first-child"), Root).Element.name, Is.EqualTo("menu-item-1"));
            Assert.That(Finder.Find(compiler.Compile("TextField"), Root).Element, Is.TypeOf<TextField>());
            Assert.That(Finder.Find(compiler.Compile("[tooltip=Save]"), Root).Element.name, Is.EqualTo("save-button"));
            Assert.That(Finder.Find(compiler.Compile("[data-role=primary]"), Root, false).Element.name, Is.EqualTo("login-button"));
        }

        [UnityTest]
        public IEnumerator Finder_DistinguishesExistenceFromVisibility()
        {
            yield return null;

            SelectorExpression toastSelector = new SelectorCompiler().Compile("#toast-message");

            Assert.That(Finder.Exists(toastSelector, Root, false), Is.True);
            Assert.That(Finder.Exists(toastSelector, Root, true), Is.False);

            Window.ShowToastForFrames(2);
            yield return null;

            Assert.That(Finder.Exists(toastSelector, Root, true), Is.True);
        }

        [UnityTest]
        public IEnumerator WaitForElementAsync_ReturnsWhenToastBecomesVisible()
        {
            Window.ShowToastForFrames(3);

            Task<FindResult> task = Finder.WaitForElementAsync(
                new SelectorCompiler().Compile("#toast-message"),
                Root,
                new WaitOptions
                {
                    TimeoutMs = 1000,
                    PollIntervalMs = 16,
                    RequireVisible = true,
                },
                System.Threading.CancellationToken.None);

            yield return UnityUIFlowTestTaskUtility.Await(task, result =>
            {
                Assert.That(result.Element, Is.Not.Null);
                Assert.That(result.Element.name, Is.EqualTo("toast-message"));
            });
        }

        [UnityTest]
        public IEnumerator BasicLoginYaml_CompletesSuccessfullyAndCreatesAttachment()
        {
            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(
                File.ReadAllText(Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml")),
                "01-basic-login.yaml"),
                result =>
                {
                    Assert.That(result.Status, Is.EqualTo(TestStatus.Passed));
                    Assert.That(result.StepResults[result.StepResults.Count - 1].Attachments, Is.Not.Empty);
                });
        }

        [UnityTest]
        public IEnumerator ClickAction_OnLoginButton_UpdatesStatusLabel()
        {
            Root.Q<TextField>("username-input").value = "alice";
            Root.Q<TextField>("password-input").value = "secret";

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteActionAsync(new ClickAction(), new Dictionary<string, string>
            {
                ["selector"] = "#login-button",
            }));

            Assert.That(Root.Q<Label>("status-label").text, Is.EqualTo("Welcome alice"));
        }

        [UnityTest]
        public IEnumerator AssertionsAndSelectorsYaml_CompletesSuccessfully()
        {
            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(
                File.ReadAllText(Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/03-assertions-and-selectors.yaml")),
                "03-assertions-and-selectors.yaml"),
                result =>
                {
                    Assert.That(result.Status, Is.EqualTo(TestStatus.Passed));
                });
        }

        [UnityTest]
        public IEnumerator LoopYaml_WaitsForToastToDisappear()
        {
            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(
                File.ReadAllText(Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/04-conditional-and-loop.yaml")),
                "04-conditional-and-loop.yaml"),
                result =>
                {
                    Assert.That(result.Status, Is.EqualTo(TestStatus.Passed));
                    Assert.That(Root.Q<Label>("toast-message").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                });
        }

        [UnityTest]
        public IEnumerator CustomActionYaml_CompletesSuccessfully()
        {
            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(
                File.ReadAllText(Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/05-custom-action-and-json.yaml")),
                "05-custom-action-and-json.yaml"),
                result =>
                {
                    Assert.That(result.Status, Is.EqualTo(TestStatus.Passed));
                    Assert.That(result.StepResults[0].Status, Is.EqualTo(TestStatus.Passed));
                });
        }
    }

    public sealed class UnityUIFlowActionFixtureTests : UnityUIFlowFixture<SampleInteractionWindow>
    {
        [UnityTest]
        public IEnumerator ClickAction_UpdatesButtonStatus()
        {
            Task task = ExecuteActionAsync(new ClickAction(), new Dictionary<string, string>
            {
                ["selector"] = "#click-button",
            });

            yield return UnityUIFlowTestTaskUtility.Await(task);

            Assert.That(Root.Q<Label>("click-status").text, Is.EqualTo("Clicks: 1"));
        }

        [UnityTest]
        public IEnumerator DoubleClickAction_UpdatesButtonStatusTwice()
        {
            Task task = ExecuteActionAsync(new DoubleClickAction(), new Dictionary<string, string>
            {
                ["selector"] = "#double-click-button",
            });

            yield return UnityUIFlowTestTaskUtility.Await(task);

            Assert.That(Root.Q<Label>("double-click-status").text, Is.EqualTo("Double Clicks: 2"));
        }

        [UnityTest]
        public IEnumerator TypeTextAndPressKeyActions_UpdateWindowState()
        {
            Window.FocusInput();
            yield return null;

            Task typeTask = ExecuteActionAsync(new TypeTextAction(), new Dictionary<string, string>
            {
                ["selector"] = "#interaction-input",
                ["value"] = "hello",
            });

            yield return UnityUIFlowTestTaskUtility.Await(typeTask);

            Task pressKeyTask = ExecuteActionAsync(new PressKeyAction(), new Dictionary<string, string>
            {
                ["key"] = "A",
            });

            yield return UnityUIFlowTestTaskUtility.Await(pressKeyTask);

            Assert.That(Root.Q<TextField>("interaction-input").value, Is.EqualTo("hello"));
            Assert.That(Root.Q<Label>("key-status").text, Does.Contain("A"));
        }

        [UnityTest]
        public IEnumerator HoverScrollAndDragActions_UpdateInteractionState()
        {
            yield return UnityUIFlowTestTaskUtility.Await(ExecuteActionAsync(new HoverAction(), new Dictionary<string, string>
            {
                ["selector"] = "#hover-target",
                ["duration"] = "50ms",
            }));

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteActionAsync(new ScrollAction(), new Dictionary<string, string>
            {
                ["selector"] = "#scroll-view",
                ["delta"] = "0,120",
            }));

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteActionAsync(new DragAction(), new Dictionary<string, string>
            {
                ["from"] = "10,10",
                ["to"] = "80,80",
                ["duration"] = "64ms",
            }));

            Assert.That(Root.Q<Label>("hover-status").text, Is.EqualTo("Hover: active"));
            Assert.That(Root.Q<ScrollView>("scroll-view").scrollOffset.y, Is.GreaterThan(0f));
            Assert.That(Root.Q<Label>("drag-status").text, Is.EqualTo("Drag: completed"));
        }
    }
}
