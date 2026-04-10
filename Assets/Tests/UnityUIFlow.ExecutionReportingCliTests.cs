using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    public sealed class UnityUIFlowExecutionFixtureTests : UnityUIFlowFixture<SampleLoginWindow>
    {
        [UnityTest]
        public IEnumerator ContinueOnStepFailure_RunsTeardownAndMarksCaseFailed()
        {
            const string yaml = @"
name: Continue On Step Failure
fixture:
  teardown:
    - action: click
      selector: '#reset-button'
steps:
  - action: type_text_fast
    selector: '#username-input'
    value: 'alice'
  - action: type_text_fast
    selector: '#password-input'
    value: 'secret'
  - action: assert_text
    selector: '#status-label'
    expected: 'Should Fail'
";

            CurrentOptions.ContinueOnStepFailure = false;

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(yaml, "continue-on-step-failure.yaml"), result =>
            {
                Assert.That(result.Status, Is.EqualTo(TestStatus.Failed));
                Assert.That(Root.Q<TextField>("username-input").value, Is.EqualTo(string.Empty));
                Assert.That(Root.Q<TextField>("password-input").value, Is.EqualTo(string.Empty));
            });
        }

        [UnityTest]
        public IEnumerator StepTimeout_MarksStepAsFailedWithTimeoutCode()
        {
            const string yaml = @"
name: Timeout Case
steps:
  - name: Short timeout wait
    action: wait
    duration: '500ms'
    timeout: '100ms'
";

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(yaml, "timeout-case.yaml"), result =>
            {
                Assert.That(result.Status, Is.EqualTo(TestStatus.Failed));
                Assert.That(result.StepResults[0].ErrorCode, Is.EqualTo(ErrorCodes.StepTimeout));
            });
        }

        [UnityTest]
        public IEnumerator Fixture_CapturesCurrentContextDuringYamlExecution()
        {
            const string yaml = @"
name: Fixture Context
steps:
  - action: wait
    duration: '16ms'
";

            yield return UnityUIFlowTestTaskUtility.Await(ExecuteYamlStepsAsync(yaml, "fixture-context.yaml"), result =>
            {
                Assert.That(result.Status, Is.EqualTo(TestStatus.Passed));
                Assert.That(CurrentContext, Is.Not.Null);
                Assert.That(CurrentContext.Root, Is.SameAs(Root));
                Assert.That(CurrentContext.Finder, Is.Not.Null);
            });
        }
    }

    public sealed class UnityUIFlowReportingAndCliTests
    {
        [Test]
        public void MarkdownReporter_WritesCaseSuiteAndArtifactManifest()
        {
            string reportRoot = CreateTempDirectory();
            string screenshotRoot = Path.Combine(reportRoot, "Screenshots");

            var reporter = new MarkdownReporter(new ReporterOptions
            {
                ReportRootPath = reportRoot,
                ScreenshotRootPath = screenshotRoot,
                SuiteName = "editor-suite",
            });

            string screenshotPath = Path.Combine(screenshotRoot, "case-001.png");
            Directory.CreateDirectory(screenshotRoot);
            File.WriteAllText(screenshotPath, "png");

            var step = new StepResult
            {
                DisplayName = "Click Login",
                Status = TestStatus.Passed,
                StartedAtUtc = "2026-04-09T00:00:00.0000000Z",
                EndedAtUtc = "2026-04-09T00:00:01.0000000Z",
                DurationMs = 1000,
                ScreenshotPath = "Screenshots/case-001.png",
                Attachments = new List<string> { "Screenshots/case-001.png" },
            };

            var caseResult = new TestResult
            {
                CaseName = "Reporter Case",
                Status = TestStatus.Passed,
                StartedAtUtc = "2026-04-09T00:00:00.0000000Z",
                EndedAtUtc = "2026-04-09T00:00:01.0000000Z",
                DurationMs = 1000,
                StepResults = new List<StepResult> { step },
            };

            reporter.RecordStepResult(caseResult.CaseName, step, step.Attachments);
            reporter.WriteCaseReport(caseResult);
            reporter.WriteSuiteReport(new TestSuiteResult
            {
                Total = 1,
                Passed = 1,
                CaseResults = new List<TestResult> { caseResult },
            });

            new CiArtifactManifestWriter().Write(reportRoot);

            Assert.That(File.Exists(Path.Combine(reportRoot, "Reporter Case.md")), Is.True);
            Assert.That(File.Exists(Path.Combine(reportRoot, "Reporter Case.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(reportRoot, "suite-editor-suite.md")), Is.True);
            Assert.That(File.Exists(Path.Combine(reportRoot, "suite-editor-suite.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(reportRoot, "artifacts.json")), Is.True);
        }

        [Test]
        public void CommandLineParser_PrefersCliValuesOverConfig()
        {
            string tempDir = CreateTempDirectory();
            string configPath = Path.Combine(tempDir, ".unityuiflow.json");
            File.WriteAllText(configPath, @"
headed: true
reportPath: ConfigReports
screenshotOnFailure: false
defaultTimeoutMs: 1500
");

            string[] args =
            {
                "Unity.exe",
                "-unityUIFlow.configFile", configPath,
                "-unityUIFlow.headed", "false",
                "-unityUIFlow.reportPath", "CliReports",
                "-unityUIFlow.screenshotOnFailure", "true",
                "-unityUIFlow.defaultTimeoutMs", "3000",
            };

            CliOptions options = new CommandLineOptionsParser().Parse(args);

            Assert.That(options.Headed, Is.False);
            Assert.That(options.ReportPath, Is.EqualTo("CliReports"));
            Assert.That(options.ScreenshotOnFailure, Is.True);
            Assert.That(options.DefaultTimeoutMs, Is.EqualTo(3000));
            Assert.That(options.ScreenshotPath, Is.EqualTo(Path.Combine("CliReports", "Screenshots")));
        }

        [Test]
        public void CommandLineParser_RejectsDuplicateArguments()
        {
            string[] args =
            {
                "Unity.exe",
                "-unityUIFlow.headed", "true",
                "-unityUIFlow.headed", "false",
            };

            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() => new CommandLineOptionsParser().Parse(args));

            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.CliArgumentInvalid));
        }

        [Test]
        public void CommandLineParser_RejectsExplicitYamlPathAndDirectoryTogether()
        {
            string[] args =
            {
                "Unity.exe",
                "-unityUIFlow.yamlPath", "Assets/Examples/Yaml/01-basic-login.yaml",
                "-unityUIFlow.yamlDirectory", "Assets/Examples/Yaml",
            };

            UnityUIFlowException ex = Assert.Throws<UnityUIFlowException>(() => new CommandLineOptionsParser().Parse(args));

            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCodes.CliArgumentInvalid));
        }

        [Test]
        public void CommandLineParser_ReadsExplicitYamlPath()
        {
            string[] args =
            {
                "Unity.exe",
                "-unityUIFlow.yamlPath", "Assets/Examples/Yaml/01-basic-login.yaml",
            };

            CliOptions options = new CommandLineOptionsParser().Parse(args);

            Assert.That(options.YamlPath, Is.EqualTo("Assets/Examples/Yaml/01-basic-login.yaml"));
            Assert.That(options.YamlDirectory, Is.Null);
        }

        [Test]
        public void ProjectSettings_AlwaysEnableVerboseLog_OverridesRuntimeFlags()
        {
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;
            bool previousVerbose = settings.AlwaysEnableVerboseLog;
            int previousDelay = settings.PreStepDelayMs;

            try
            {
                settings.AlwaysEnableVerboseLog = true;
                settings.PreStepDelayMs = 1000;

                TestOptions resolved = UnityUIFlowProjectSettingsUtility.ApplyOverrides(new TestOptions
                {
                    Headed = true,
                    EnableVerboseLog = false,
                    PreStepDelayMs = 0,
                });

                Assert.That(resolved.EnableVerboseLog, Is.True);
                Assert.That(resolved.PreStepDelayMs, Is.EqualTo(1000));
            }
            finally
            {
                settings.AlwaysEnableVerboseLog = previousVerbose;
                settings.PreStepDelayMs = previousDelay;
            }
        }

        [Test]
        public void ProjectSettings_ProjectDelay_DoesNotOverrideNonHeadedRuns()
        {
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;
            bool previousVerbose = settings.AlwaysEnableVerboseLog;
            int previousDelay = settings.PreStepDelayMs;

            try
            {
                settings.AlwaysEnableVerboseLog = false;
                settings.PreStepDelayMs = 1000;

                TestOptions resolved = UnityUIFlowProjectSettingsUtility.ApplyOverrides(new TestOptions
                {
                    Headed = false,
                    EnableVerboseLog = false,
                    PreStepDelayMs = 0,
                });

                Assert.That(resolved.EnableVerboseLog, Is.False);
                Assert.That(resolved.PreStepDelayMs, Is.EqualTo(0));
            }
            finally
            {
                settings.AlwaysEnableVerboseLog = previousVerbose;
                settings.PreStepDelayMs = previousDelay;
            }
        }

        [Test]
        public void ProjectSettings_KeepRuntimeDelayWhenProjectDelayDisabled()
        {
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;
            bool previousVerbose = settings.AlwaysEnableVerboseLog;
            int previousDelay = settings.PreStepDelayMs;

            try
            {
                settings.AlwaysEnableVerboseLog = false;
                settings.PreStepDelayMs = 0;

                TestOptions resolved = UnityUIFlowProjectSettingsUtility.ApplyOverrides(new TestOptions
                {
                    EnableVerboseLog = false,
                    PreStepDelayMs = 250,
                });

                Assert.That(resolved.EnableVerboseLog, Is.False);
                Assert.That(resolved.PreStepDelayMs, Is.EqualTo(250));
            }
            finally
            {
                settings.AlwaysEnableVerboseLog = previousVerbose;
                settings.PreStepDelayMs = previousDelay;
            }
        }

        [Test]
        public void YamlTestCaseFilter_MatchesFileNameAndCaseName()
        {
            Assert.That(YamlTestCaseFilter.Match("01-*", "Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml", "Basic Login"), Is.True);
            Assert.That(YamlTestCaseFilter.Match("*Selectors", "Assets/UnityUIFlow/Samples/Yaml/03-assertions-and-selectors.yaml", "Assertions And Selectors"), Is.True);
            Assert.That(YamlTestCaseFilter.Match("NoMatch*", "Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml", "Basic Login"), Is.False);
        }

        [Test]
        public void ExitCodeResolver_PrioritizesErrorsOverFailures()
        {
            Assert.That(ExitCodeResolver.Resolve(new TestSuiteResult { Failed = 1 }), Is.EqualTo(1));
            Assert.That(ExitCodeResolver.Resolve(new TestSuiteResult { Errors = 1, Failed = 1 }), Is.EqualTo(2));
            Assert.That(ExitCodeResolver.Resolve(new TestSuiteResult { Passed = 1 }), Is.EqualTo(0));
        }

        [Test]
        public void ActionContext_AddAttachment_CapsAtTen()
        {
            var context = new ActionContext();

            for (int index = 0; index < 12; index++)
            {
                context.AddAttachment($"attachment-{index}.png");
            }

            Assert.That(context.CurrentAttachments, Has.Count.EqualTo(10));
            Assert.That(context.CurrentAttachments[0], Is.EqualTo("attachment-0.png"));
            Assert.That(context.CurrentAttachments[9], Is.EqualTo("attachment-9.png"));
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "UnityUIFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
