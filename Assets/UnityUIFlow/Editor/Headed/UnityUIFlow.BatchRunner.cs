using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    internal enum BatchRunTargetMode
    {
        SingleFile,
        Directory,
    }

    [Serializable]
    internal sealed class BatchRunnerCaseSnapshot
    {
        public string YamlPath;
        public string CaseName;
        public TestStatus Status;
        public int DurationMs;
        public string ErrorCode;
        public string ErrorMessage;
        public string FailedStepName;
        public string FailedStepError;
        public string ReportMarkdownPath;
        public string ReportJsonPath;
    }

    [Serializable]
    internal sealed class BatchRunnerViewState
    {
        public BatchRunTargetMode TargetMode = BatchRunTargetMode.SingleFile;
        public string TargetPath = string.Empty;
        public string ReportPath = "Reports/BatchRunner";
        public bool Headed = true;
        public bool StopOnFirstFailure;
        public bool ContinueOnStepFailure;
        public bool ScreenshotOnFailure = true;
        public bool EnableVerboseLog;
        public int DefaultTimeoutMs = 3000;
        public bool IsRunning;
        public string StatusText = "Idle";
        public string CurrentYamlPath;
        public string CurrentCaseName;
        public string LastError;
        public int Total;
        public int Passed;
        public int Failed;
        public int Errors;
        public int Skipped;
        public string StartedAtUtc;
        public string EndedAtUtc;
        public List<BatchRunnerCaseSnapshot> Cases = new List<BatchRunnerCaseSnapshot>();
    }

    public sealed class BatchRunnerWindow : EditorWindow
    {
        private readonly BatchRunnerViewState _state = new BatchRunnerViewState();
        private CancellationTokenSource _runCts;
        private ExecutionContext _activeContext;

        private EnumField _targetModeField;
        private TextField _targetPathField;
        private TextField _reportPathField;
        private Toggle _headedToggle;
        private Toggle _stopOnFirstFailureToggle;
        private Toggle _continueOnStepFailureToggle;
        private Toggle _screenshotOnFailureToggle;
        private Toggle _verboseLogToggle;
        private IntegerField _defaultTimeoutField;
        private Button _runButton;
        private Button _cancelButton;
        private Label _statusLabel;
        private Label _currentYamlLabel;
        private Label _currentCaseLabel;
        private Label _summaryLabel;
        private HelpBox _messageBox;
        private ScrollView _resultsScrollView;

        [MenuItem("UnityUIFlow/Batch Runner", priority = 101)]
        public static void Open()
        {
            BatchRunnerWindow window = GetWindow<BatchRunnerWindow>();
            window.titleContent = new GUIContent("UIFlow Batch");
            window.minSize = new Vector2(620f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            BatchRunnerPreferences.Load(_state);
            if (string.IsNullOrWhiteSpace(_state.TargetPath))
            {
                _state.TargetPath = _state.TargetMode == BatchRunTargetMode.SingleFile
                    ? "Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml"
                    : "Assets/UnityUIFlow/Samples/Yaml";
            }

            if (string.IsNullOrWhiteSpace(_state.ReportPath))
            {
                _state.ReportPath = "Reports/BatchRunner";
            }

            BuildUi();
            RefreshUi();
        }

        private void OnDisable()
        {
            BatchRunnerPreferences.Save(_state);
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;

            var targetModeRow = new VisualElement();
            targetModeRow.style.flexDirection = FlexDirection.Row;
            targetModeRow.style.alignItems = Align.Center;
            rootVisualElement.Add(targetModeRow);

            _targetModeField = new EnumField("Target Mode", _state.TargetMode);
            _targetModeField.style.minWidth = 260;
            _targetModeField.RegisterValueChangedCallback(evt =>
            {
                _state.TargetMode = (BatchRunTargetMode)evt.newValue;
                if (string.IsNullOrWhiteSpace(_state.TargetPath))
                {
                    _state.TargetPath = _state.TargetMode == BatchRunTargetMode.SingleFile
                        ? "Assets/UnityUIFlow/Samples/Yaml/01-basic-login.yaml"
                        : "Assets/UnityUIFlow/Samples/Yaml";
                }

                _targetPathField?.SetValueWithoutNotify(_state.TargetPath ?? string.Empty);
                BatchRunnerPreferences.Save(_state);
            });
            targetModeRow.Add(_targetModeField);

            targetModeRow.Add(CreateButton("Open Headed", HeadedTestWindow.Open));
            targetModeRow.Add(CreateButton("Settings", OpenSettings));

            var targetPathRow = new VisualElement();
            targetPathRow.style.flexDirection = FlexDirection.Row;
            targetPathRow.style.alignItems = Align.Center;
            targetPathRow.style.marginTop = 8;
            rootVisualElement.Add(targetPathRow);

            _targetPathField = new TextField("YAML Target")
            {
                value = _state.TargetPath ?? string.Empty,
            };
            _targetPathField.style.flexGrow = 1;
            _targetPathField.RegisterValueChangedCallback(evt =>
            {
                _state.TargetPath = evt.newValue ?? string.Empty;
                BatchRunnerPreferences.Save(_state);
            });
            targetPathRow.Add(_targetPathField);
            targetPathRow.Add(CreateButton("Browse", BrowseTarget));

            var reportPathRow = new VisualElement();
            reportPathRow.style.flexDirection = FlexDirection.Row;
            reportPathRow.style.alignItems = Align.Center;
            reportPathRow.style.marginTop = 8;
            rootVisualElement.Add(reportPathRow);

            _reportPathField = new TextField("Report Path")
            {
                value = _state.ReportPath ?? "Reports/BatchRunner",
            };
            _reportPathField.style.flexGrow = 1;
            _reportPathField.RegisterValueChangedCallback(evt =>
            {
                _state.ReportPath = evt.newValue ?? "Reports/BatchRunner";
                BatchRunnerPreferences.Save(_state);
            });
            reportPathRow.Add(_reportPathField);
            reportPathRow.Add(CreateButton("Browse", BrowseReportPath));
            reportPathRow.Add(CreateButton("Open", OpenReportDirectory));

            var optionsGrid = new VisualElement();
            optionsGrid.style.flexDirection = FlexDirection.Row;
            optionsGrid.style.flexWrap = Wrap.Wrap;
            optionsGrid.style.marginTop = 10;
            rootVisualElement.Add(optionsGrid);

            _headedToggle = CreateToggle("Headed", _state.Headed, value => _state.Headed = value);
            _stopOnFirstFailureToggle = CreateToggle("Stop On First Failure", _state.StopOnFirstFailure, value => _state.StopOnFirstFailure = value);
            _continueOnStepFailureToggle = CreateToggle("Continue On Step Failure", _state.ContinueOnStepFailure, value => _state.ContinueOnStepFailure = value);
            _screenshotOnFailureToggle = CreateToggle("Screenshot On Failure", _state.ScreenshotOnFailure, value => _state.ScreenshotOnFailure = value);
            _verboseLogToggle = CreateToggle("Verbose Log", _state.EnableVerboseLog, value => _state.EnableVerboseLog = value);

            optionsGrid.Add(_headedToggle);
            optionsGrid.Add(_stopOnFirstFailureToggle);
            optionsGrid.Add(_continueOnStepFailureToggle);
            optionsGrid.Add(_screenshotOnFailureToggle);
            optionsGrid.Add(_verboseLogToggle);

            _defaultTimeoutField = new IntegerField("Default Timeout (ms)")
            {
                value = _state.DefaultTimeoutMs,
            };
            _defaultTimeoutField.style.marginTop = 6;
            _defaultTimeoutField.RegisterValueChangedCallback(evt =>
            {
                _state.DefaultTimeoutMs = evt.newValue;
                BatchRunnerPreferences.Save(_state);
            });
            rootVisualElement.Add(_defaultTimeoutField);

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 10;
            actionRow.style.marginBottom = 8;
            rootVisualElement.Add(actionRow);

            _runButton = CreateButton("Run", RunSelected);
            _cancelButton = CreateButton("Cancel", CancelRun);
            actionRow.Add(_runButton);
            actionRow.Add(_cancelButton);

            _statusLabel = new Label();
            _currentYamlLabel = new Label();
            _currentCaseLabel = new Label();
            _summaryLabel = new Label();
            rootVisualElement.Add(_statusLabel);
            rootVisualElement.Add(_currentYamlLabel);
            rootVisualElement.Add(_currentCaseLabel);
            rootVisualElement.Add(_summaryLabel);

            _messageBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            _messageBox.style.marginTop = 6;
            rootVisualElement.Add(_messageBox);

            _resultsScrollView = new ScrollView(ScrollViewMode.Vertical);
            _resultsScrollView.style.flexGrow = 1;
            _resultsScrollView.style.marginTop = 10;
            rootVisualElement.Add(_resultsScrollView);
        }

        private Toggle CreateToggle(string label, bool currentValue, Action<bool> onChanged)
        {
            var toggle = new Toggle(label)
            {
                value = currentValue,
            };
            toggle.style.minWidth = 220;
            toggle.RegisterValueChangedCallback(evt =>
            {
                onChanged(evt.newValue);
                BatchRunnerPreferences.Save(_state);
            });
            return toggle;
        }

        private Button CreateButton(string text, Action onClick)
        {
            var button = new Button(onClick)
            {
                text = text,
            };
            button.style.marginRight = 6;
            return button;
        }

        private async void RunSelected()
        {
            if (_state.IsRunning)
            {
                SetMessage("A batch execution is already running.", HelpBoxMessageType.Warning);
                return;
            }

            List<string> yamlPaths;
            try
            {
                yamlPaths = BatchRunnerPathUtility.ResolveYamlPaths(_state.TargetMode, _state.TargetPath);
            }
            catch (Exception ex)
            {
                SetMessage(ex.Message, HelpBoxMessageType.Error);
                return;
            }

            _runCts = new CancellationTokenSource();
            _activeContext = null;
            _state.IsRunning = true;
            _state.StatusText = "Running";
            _state.StartedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            _state.EndedAtUtc = null;
            _state.LastError = null;
            _state.CurrentYamlPath = null;
            _state.CurrentCaseName = null;
            _state.Total = yamlPaths.Count;
            _state.Passed = 0;
            _state.Failed = 0;
            _state.Errors = 0;
            _state.Skipped = 0;
            _state.Cases.Clear();
            BatchRunnerPreferences.Save(_state);
            SetMessage($"Queued {yamlPaths.Count} YAML file(s).", HelpBoxMessageType.Info);
            RefreshUi();

            try
            {
                await ExecuteBatchAsync(yamlPaths, _runCts.Token);
            }
            finally
            {
                _runCts?.Dispose();
                _runCts = null;
                _activeContext = null;
            }
        }

        private void CancelRun()
        {
            if (!_state.IsRunning)
            {
                SetMessage("No active batch execution.", HelpBoxMessageType.Info);
                return;
            }

            _runCts?.Cancel();
            _activeContext?.RuntimeController?.Stop();
            SetMessage("Cancellation requested.", HelpBoxMessageType.Warning);
        }

        private async System.Threading.Tasks.Task ExecuteBatchAsync(List<string> yamlPaths, CancellationToken cancellationToken)
        {
            string reportRoot = string.IsNullOrWhiteSpace(_state.ReportPath) ? "Reports/BatchRunner" : _state.ReportPath;
            string screenshotRoot = Path.Combine(reportRoot, "Screenshots");
            var suite = new TestSuiteResult
            {
                StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            };
            var runner = new TestRunner();
            var reportPaths = new ReportPathBuilder();

            try
            {
                foreach (string yamlPath in yamlPaths)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _state.CurrentYamlPath = BatchRunnerPathUtility.MakeProjectRelative(yamlPath);
                    _state.CurrentCaseName = null;
                    RefreshUi();

                    TestResult result;
                    try
                    {
                        result = await runner.RunFileAsync(
                            yamlPath,
                            new TestOptions
                            {
                                Headed = _state.Headed,
                                ReportOutputPath = reportRoot,
                                ScreenshotPath = screenshotRoot,
                                ScreenshotOnFailure = _state.ScreenshotOnFailure,
                                StopOnFirstFailure = _state.StopOnFirstFailure,
                                ContinueOnStepFailure = _state.ContinueOnStepFailure,
                                DefaultTimeoutMs = _state.DefaultTimeoutMs,
                                EnableVerboseLog = _state.EnableVerboseLog,
                            },
                            null,
                            context =>
                            {
                                _activeContext = context;
                                _state.CurrentCaseName = context.CaseName;
                                RefreshUi();
                            });
                    }
                    catch (Exception ex)
                    {
                        result = new TestResult
                        {
                            CaseName = Path.GetFileNameWithoutExtension(yamlPath),
                            Status = TestStatus.Error,
                            StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                            EndedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                            ErrorCode = ex is UnityUIFlowException flowException ? flowException.ErrorCode : ErrorCodes.CliExecutionError,
                            ErrorMessage = ex.Message,
                        };
                    }
                    finally
                    {
                        _activeContext = null;
                    }

                    suite.CaseResults.Add(result);
                    ApplyCaseCounters(result.Status);
                    _state.Cases.Add(BuildSnapshot(result, yamlPath, reportRoot, reportPaths));
                    _state.CurrentCaseName = null;
                    RefreshUi();

                    if (_state.StopOnFirstFailure && (result.Status == TestStatus.Failed || result.Status == TestStatus.Error))
                    {
                        break;
                    }
                }

                suite.Total = suite.CaseResults.Count;
                suite.Passed = _state.Passed;
                suite.Failed = _state.Failed;
                suite.Errors = _state.Errors;
                suite.Skipped = _state.Skipped;
                suite.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                suite.ExitCode = ExitCodeResolver.Resolve(suite);

                var reporter = new MarkdownReporter(new ReporterOptions
                {
                    ReportRootPath = reportRoot,
                    ScreenshotRootPath = screenshotRoot,
                    SuiteName = BuildSuiteName(),
                });
                reporter.WriteSuiteReport(suite);
                new CiArtifactManifestWriter().Write(reportRoot);

                _state.StatusText = cancellationToken.IsCancellationRequested ? "Aborted" : "Completed";
                if (cancellationToken.IsCancellationRequested)
                {
                    _state.LastError = "Execution was cancelled by user.";
                    SetMessage(_state.LastError, HelpBoxMessageType.Warning);
                }
                else
                {
                    SetMessage($"Completed {suite.Total} case(s).", HelpBoxMessageType.Info);
                }
            }
            catch (Exception ex)
            {
                _state.StatusText = "Failed";
                _state.LastError = ex.Message;
                SetMessage(ex.Message, HelpBoxMessageType.Error);
            }
            finally
            {
                _state.IsRunning = false;
                _state.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                _state.CurrentYamlPath = null;
                _state.CurrentCaseName = null;
                BatchRunnerPreferences.Save(_state);
                RefreshUi();
            }
        }

        private void ApplyCaseCounters(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed:
                    _state.Passed++;
                    break;
                case TestStatus.Failed:
                    _state.Failed++;
                    break;
                case TestStatus.Error:
                    _state.Errors++;
                    break;
                case TestStatus.Skipped:
                    _state.Skipped++;
                    break;
            }
        }

        private BatchRunnerCaseSnapshot BuildSnapshot(TestResult result, string yamlPath, string reportRoot, ReportPathBuilder reportPaths)
        {
            StepResult failedStep = result.StepResults?.FirstOrDefault(step => step.Status == TestStatus.Failed || step.Status == TestStatus.Error);
            return new BatchRunnerCaseSnapshot
            {
                YamlPath = BatchRunnerPathUtility.MakeProjectRelative(yamlPath),
                CaseName = result.CaseName,
                Status = result.Status,
                DurationMs = result.DurationMs,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                FailedStepName = failedStep?.DisplayName,
                FailedStepError = failedStep?.ErrorMessage,
                ReportMarkdownPath = BatchRunnerPathUtility.MakeProjectRelative(reportPaths.BuildCaseMarkdownPath(reportRoot, result.CaseName)),
                ReportJsonPath = BatchRunnerPathUtility.MakeProjectRelative(reportPaths.BuildCaseJsonPath(reportRoot, result.CaseName)),
            };
        }

        private string BuildSuiteName()
        {
            string suffix = _state.TargetMode == BatchRunTargetMode.SingleFile
                ? Path.GetFileNameWithoutExtension(_state.TargetPath)
                : Path.GetFileName(_state.TargetPath?.TrimEnd('/', '\\'));
            suffix = string.IsNullOrWhiteSpace(suffix) ? "batch-runner" : suffix;
            return $"batch-{suffix}";
        }

        private void BrowseTarget()
        {
            string initialDirectory = BatchRunnerPathUtility.GetInitialDirectory(_state.TargetPath);
            if (_state.TargetMode == BatchRunTargetMode.SingleFile)
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select YAML Test Case", initialDirectory, "yaml");
                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    return;
                }

                _state.TargetPath = BatchRunnerPathUtility.NormalizePath(selectedPath);
            }
            else
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select YAML Directory", initialDirectory, string.Empty);
                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    return;
                }

                _state.TargetPath = BatchRunnerPathUtility.NormalizePath(selectedPath);
            }

            _targetPathField.SetValueWithoutNotify(_state.TargetPath);
            BatchRunnerPreferences.Save(_state);
        }

        private void BrowseReportPath()
        {
            string initialDirectory = BatchRunnerPathUtility.GetInitialDirectory(_state.ReportPath);
            string selectedPath = EditorUtility.OpenFolderPanel("Select Report Directory", initialDirectory, string.Empty);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            _state.ReportPath = BatchRunnerPathUtility.NormalizePath(selectedPath);
            _reportPathField.SetValueWithoutNotify(_state.ReportPath);
            BatchRunnerPreferences.Save(_state);
        }

        private void OpenReportDirectory()
        {
            string fullPath = Path.GetFullPath(string.IsNullOrWhiteSpace(_state.ReportPath) ? "Reports/BatchRunner" : _state.ReportPath);
            Directory.CreateDirectory(fullPath);
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(UnityUIFlowProjectSettingsUtility.SettingsPath);
        }

        private void SetMessage(string message, HelpBoxMessageType messageType)
        {
            _state.LastError = messageType == HelpBoxMessageType.Error ? message : _state.LastError;
            _messageBox.text = message ?? string.Empty;
            _messageBox.messageType = messageType;
            _messageBox.style.display = string.IsNullOrWhiteSpace(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void RefreshUi()
        {
            if (_statusLabel == null)
            {
                return;
            }

            _statusLabel.text = $"Status: {_state.StatusText}";
            _currentYamlLabel.text = $"Current YAML: {(_state.CurrentYamlPath ?? "-")}";
            _currentCaseLabel.text = $"Current Case: {(_state.CurrentCaseName ?? "-")}";
            _summaryLabel.text = $"Summary: total={_state.Total} passed={_state.Passed} failed={_state.Failed} errors={_state.Errors} skipped={_state.Skipped}";

            if (_runButton != null)
            {
                _runButton.SetEnabled(!_state.IsRunning);
            }

            if (_cancelButton != null)
            {
                _cancelButton.SetEnabled(_state.IsRunning);
            }

            RenderCaseResults();
            Repaint();
        }

        private void RenderCaseResults()
        {
            if (_resultsScrollView == null)
            {
                return;
            }

            _resultsScrollView.Clear();
            if (_state.Cases.Count == 0)
            {
                _resultsScrollView.Add(new Label("No case results yet."));
                return;
            }

            foreach (BatchRunnerCaseSnapshot snapshot in _state.Cases)
            {
                var card = new VisualElement();
                card.style.marginBottom = 8;
                card.style.paddingLeft = 8;
                card.style.paddingRight = 8;
                card.style.paddingTop = 6;
                card.style.paddingBottom = 6;
                card.style.borderLeftWidth = 2;
                card.style.borderRightWidth = 2;
                card.style.borderTopWidth = 2;
                card.style.borderBottomWidth = 2;
                card.style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
                card.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
                card.style.borderTopColor = new Color(0.22f, 0.22f, 0.22f);
                card.style.borderBottomColor = new Color(0.22f, 0.22f, 0.22f);

                card.Add(new Label($"{snapshot.CaseName} [{snapshot.Status}] {snapshot.DurationMs}ms"));
                card.Add(new Label($"YAML: {snapshot.YamlPath}"));

                if (!string.IsNullOrWhiteSpace(snapshot.ErrorMessage))
                {
                    card.Add(new Label($"Error: {snapshot.ErrorCode} {snapshot.ErrorMessage}"));
                }

                if (!string.IsNullOrWhiteSpace(snapshot.FailedStepName))
                {
                    card.Add(new Label($"Failed Step: {snapshot.FailedStepName}"));
                }

                if (!string.IsNullOrWhiteSpace(snapshot.FailedStepError))
                {
                    card.Add(new Label($"Step Error: {snapshot.FailedStepError}"));
                }

                card.Add(new Label($"Report: {snapshot.ReportMarkdownPath}"));
                _resultsScrollView.Add(card);
            }
        }
    }

    internal static class BatchRunnerPathUtility
    {
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string fullPath = Path.GetFullPath(path);
            string projectRoot = UnityUIFlowUtility.AppendDirectorySeparator(Path.GetFullPath(Directory.GetCurrentDirectory()));
            if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(projectRoot.Length).Replace('\\', '/');
            }

            return fullPath;
        }

        public static string GetInitialDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return Path.GetDirectoryName(fullPath);
                }

                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.GetFullPath(Directory.GetCurrentDirectory());
        }

        public static List<string> ResolveYamlPaths(BatchRunTargetMode mode, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new UnityUIFlowException(ErrorCodes.TestCasePathInvalid, "YAML target path is required.");
            }

            if (mode == BatchRunTargetMode.SingleFile)
            {
                string fullPath = Path.GetFullPath(targetPath);
                if (!File.Exists(fullPath))
                {
                    throw new UnityUIFlowException(ErrorCodes.TestCaseFileNotFound, $"YAML file not found: {targetPath}");
                }

                if (!fullPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnityUIFlowException(ErrorCodes.TestCasePathInvalid, $"Target must be a .yaml file: {targetPath}");
                }

                return new List<string> { fullPath };
            }

            string directory = Path.GetFullPath(targetPath);
            if (!Directory.Exists(directory))
            {
                throw new UnityUIFlowException(ErrorCodes.TestSuiteDirectoryNotFound, $"YAML directory not found: {targetPath}");
            }

            List<string> yamlFiles = Directory.GetFiles(directory, "*.yaml", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (yamlFiles.Count == 0)
            {
                throw new UnityUIFlowException(ErrorCodes.TestSuiteEmpty, $"No YAML files found under: {targetPath}");
            }

            return yamlFiles;
        }

        public static string MakeProjectRelative(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string fullPath = Path.GetFullPath(path);
            string projectRoot = UnityUIFlowUtility.AppendDirectorySeparator(Path.GetFullPath(Directory.GetCurrentDirectory()));
            if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(projectRoot.Length).Replace('\\', '/');
            }

            return fullPath;
        }
    }

    internal static class BatchRunnerPreferences
    {
        private const string Prefix = "UnityUIFlow.BatchRunner.";

        public static void Load(BatchRunnerViewState state)
        {
            state.TargetMode = (BatchRunTargetMode)EditorPrefs.GetInt(Prefix + nameof(BatchRunnerViewState.TargetMode), (int)BatchRunTargetMode.SingleFile);
            state.TargetPath = EditorPrefs.GetString(Prefix + nameof(BatchRunnerViewState.TargetPath), string.Empty);
            state.ReportPath = EditorPrefs.GetString(Prefix + nameof(BatchRunnerViewState.ReportPath), "Reports/BatchRunner");
            state.Headed = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.Headed), true);
            state.StopOnFirstFailure = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.StopOnFirstFailure), false);
            state.ContinueOnStepFailure = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.ContinueOnStepFailure), false);
            state.ScreenshotOnFailure = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.ScreenshotOnFailure), true);
            state.EnableVerboseLog = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.EnableVerboseLog), UnityUIFlowMenuItems.IsVerboseLogEnabled);
            state.DefaultTimeoutMs = EditorPrefs.GetInt(Prefix + nameof(BatchRunnerViewState.DefaultTimeoutMs), 3000);
        }

        public static void Save(BatchRunnerViewState state)
        {
            EditorPrefs.SetInt(Prefix + nameof(BatchRunnerViewState.TargetMode), (int)state.TargetMode);
            EditorPrefs.SetString(Prefix + nameof(BatchRunnerViewState.TargetPath), state.TargetPath ?? string.Empty);
            EditorPrefs.SetString(Prefix + nameof(BatchRunnerViewState.ReportPath), state.ReportPath ?? "Reports/BatchRunner");
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.Headed), state.Headed);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.StopOnFirstFailure), state.StopOnFirstFailure);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.ContinueOnStepFailure), state.ContinueOnStepFailure);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.ScreenshotOnFailure), state.ScreenshotOnFailure);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.EnableVerboseLog), state.EnableVerboseLog);
            EditorPrefs.SetInt(Prefix + nameof(BatchRunnerViewState.DefaultTimeoutMs), state.DefaultTimeoutMs);
        }
    }
}
