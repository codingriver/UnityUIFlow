using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    public sealed class UnityUIFlowParsingAndPlanningTests
    {
        [Test]
        public void Parser_BuildsExecutionPlan_FromCsvCase()
        {
            string yamlPath = Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/02-data-driven-csv.yaml");
            var parser = new YamlTestCaseParser();
            TestCaseDefinition definition = parser.ParseFile(yamlPath);
            var builder = new ExecutionPlanBuilder(new SelectorCompiler(), new ActionRegistry());
            ExecutionPlan plan = builder.Build(definition, new TestOptions());

            Assert.That(plan.CaseName, Is.EqualTo("CSV Driven Login"));
            Assert.That(plan.Steps.Count, Is.EqualTo(8));
            Assert.That(plan.Steps[0].Parameters["value"], Is.EqualTo("alice"));
            Assert.That(plan.Steps[4].Parameters["value"], Is.EqualTo("bob"));
        }

        [Test]
        public void Parser_CapturesAdditionalActionParameters()
        {
            const string yaml = @"
name: Custom Parameter Case
steps:
  - name: Login
    action: custom_login
    username_selector: '#username-input'
    password_selector: '#password-input'
    button_selector: '#login-button'
    username: 'alice'
    password: 'secret'
";

            TestCaseDefinition definition = new YamlTestCaseParser().Parse(yaml, "inline.yaml");

            Assert.That(definition.Steps, Has.Count.EqualTo(1));
            Assert.That(definition.Steps[0].Parameters["username_selector"], Is.EqualTo("#username-input"));
            Assert.That(definition.Steps[0].Parameters["button_selector"], Is.EqualTo("#login-button"));
        }

        [Test]
        public void Parser_RejectsStepWithActionAndLoop()
        {
            const string yaml = @"
name: Invalid Case
steps:
  - name: Invalid step
    action: click
    repeat_while:
      condition:
        exists: '#target'
      steps:
        - action: wait
          duration: '50ms'
";

            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() => new YamlTestCaseParser().Parse(yaml, "invalid.yaml"));

            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.TestCaseSchemaInvalid));
            Assert.That(ex.Message, Does.Contain("repeat_while"));
        }

        [Test]
        public void TemplateRenderer_ThrowsOnMissingVariable()
        {
            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() =>
                TemplateRenderer.Render("Hello {{ username }}", new Dictionary<string, string>(), "Missing variable step"));

            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.TestDataVariableMissing));
        }

        [Test]
        public void DurationParser_ParsesMillisecondsAndSeconds()
        {
            Assert.That(DurationParser.ParseToMilliseconds("50ms", "step"), Is.EqualTo(50));
            Assert.That(DurationParser.ParseToMilliseconds("1.5s", "step"), Is.EqualTo(1500));
        }

        [Test]
        public void TestDataResolver_LoadsJsonRows()
        {
            TestCaseDefinition definition = new YamlTestCaseParser().Parse(@"
name: Json Data
data:
  from_json: users.json
steps:
  - action: wait
    duration: '10ms'
", Path.GetFullPath("Assets/UnityUIFlow/Samples/Yaml/05-custom-action-and-json.yaml"));

            List<Dictionary<string, string>> rows = TestDataResolver.ResolveRows(definition);

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0]["username"], Is.EqualTo("alice"));
            Assert.That(rows[1]["expected"], Does.Contain("bob"));
        }

        [Test]
        public void TestDataResolver_LoadsUtf8BomCsvRows()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                string csvPath = Path.Combine(tempDirectory, "users.csv");
                File.WriteAllText(csvPath, "username,expected\r\nalice,ok\r\n", new UTF8Encoding(true));

                var definition = new TestCaseDefinition
                {
                    Name = "CSV BOM",
                    SourceFile = Path.Combine(tempDirectory, "case.yaml"),
                    Data = new DataSourceDefinition
                    {
                        FromCsv = "users.csv",
                    },
                    Steps = new List<StepDefinition>
                    {
                        new StepDefinition
                        {
                            Action = "wait",
                            Duration = "10ms",
                        },
                    },
                };

                List<Dictionary<string, string>> rows = TestDataResolver.ResolveRows(definition);

                Assert.That(rows, Has.Count.EqualTo(1));
                Assert.That(rows[0].ContainsKey("username"), Is.True);
                Assert.That(rows[0].ContainsKey("\ufeffusername"), Is.False);
                Assert.That(rows[0]["username"], Is.EqualTo("alice"));
                Assert.That(rows[0]["expected"], Is.EqualTo("ok"));
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        [Test]
        public void SelectorCompiler_ParsesPseudoAndChildSelectors()
        {
            SelectorExpression expression = new SelectorCompiler().Compile("#panel > .item:first-child");

            Assert.That(expression.Segments.Count, Is.EqualTo(3));
            Assert.That(expression.Segments[0].TokenType, Is.EqualTo(SelectorTokenType.Id));
            Assert.That(expression.Segments[1].Combinator, Is.EqualTo(SelectorCombinator.Child));
            Assert.That(expression.Segments[2].TokenType, Is.EqualTo(SelectorTokenType.Pseudo));
        }

        [Test]
        public void ExecutionPlanBuilder_ExpandsSetupMainTeardownForEveryRow()
        {
            var definition = new TestCaseDefinition
            {
                Name = "Plan Expansion",
                SourceFile = "inline.yaml",
                Data = new DataSourceDefinition
                {
                    Rows = new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string> { ["username"] = "alice" },
                        new Dictionary<string, string> { ["username"] = "bob" },
                    },
                },
                Fixture = new FixtureDefinition
                {
                    Setup = new List<StepDefinition>
                    {
                        new StepDefinition { Action = "wait", Duration = "10ms" },
                    },
                    Teardown = new List<StepDefinition>
                    {
                        new StepDefinition { Action = "wait", Duration = "10ms" },
                    },
                },
                Steps = new List<StepDefinition>
                {
                    new StepDefinition
                    {
                        Name = "Type {{ username }}",
                        Action = "type_text_fast",
                        Selector = "#username-input",
                        Value = "{{ username }}",
                    },
                },
            };

            ExecutionPlan plan = new ExecutionPlanBuilder(new SelectorCompiler(), new ActionRegistry()).Build(definition, new TestOptions());

            Assert.That(plan.Steps, Has.Count.EqualTo(6));
            Assert.That(plan.Steps[0].Phase, Is.EqualTo(StepPhase.Setup));
            Assert.That(plan.Steps[1].Phase, Is.EqualTo(StepPhase.Main));
            Assert.That(plan.Steps[1].Parameters["value"], Is.EqualTo("alice"));
            Assert.That(plan.Steps[4].Parameters["value"], Is.EqualTo("bob"));
            Assert.That(plan.Steps[5].Phase, Is.EqualTo(StepPhase.Teardown));
        }

        [Test]
        public void ActionRegistry_ResolvesBuiltInAndCustomActions()
        {
            var registry = new ActionRegistry();

            Assert.That(registry.HasAction("click"), Is.True);
            Assert.That(registry.HasAction("execute_command"), Is.True);
            Assert.That(registry.HasAction("validate_command"), Is.True);
            Assert.That(registry.Resolve("click"), Is.Not.Null);
            Assert.That(registry.Resolve("execute_command"), Is.Not.Null);
            Assert.That(registry.Resolve("validate_command"), Is.Not.Null);
            Assert.That(registry.HasAction("custom_login"), Is.True);
            Assert.That(registry.Resolve("custom_login"), Is.TypeOf<CustomLoginAction>());
        }

        [Test]
        public void TestAssets_CanBeLoadedFromProject()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SampleLoginWindow.UxmlPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<StyleSheet>(SampleLoginWindow.UssPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SampleInteractionWindow.UxmlPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<StyleSheet>(SampleInteractionWindow.UssPath), Is.Not.Null);
        }
    }
}
