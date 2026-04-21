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
    internal sealed class BatchRunnerCaseItem
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
        public bool IsChecked = true;
        public bool IsRunning;
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
        public bool RequireOfficialHost;
        public bool RequireOfficialPointerDriver;
        public bool RequireInputSystemKeyboardDriver;
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
        public List<BatchRunnerCaseItem> Cases = new List<BatchRunnerCaseItem>();
    }

    public sealed class BatchRunnerWindow : EditorWindow
    {
        private readonly BatchRunnerViewState _state = new BatchRunnerViewState();
        private CancellationTokenSource _runCts;
        private ExecutionContext _activeContext;
        private HighlightOverlayRenderer _overlayRenderer;

        private EnumField _targetModeField;
        private TextField _targetPathField;
        private TextField _reportPathField;
        private Toggle _headedToggle;
        private Toggle _stopOnFirstFailureToggle;
        private Toggle _continueOnStepFailureToggle;
        private Toggle _screenshotOnFailureToggle;
        private Toggle _verboseLogToggle;
        private Toggle _requireOfficialHostToggle;
        private Toggle _requireOfficialPointerDriverToggle;
        private Toggle _requireInputSystemKeyboardDriverToggle;
        private IntegerField _defaultTimeoutField;
        private Button _runAllButton;
        private Button _runSelectedButton;
        private Button _rerunFailedButton;
        private Button _cancelButton;
        private Button _selectAllButton;
        private Button _selectNoneButton;
        private Button _selectFailedButton;
        private Label _statusLabel;
        private Label _currentYamlLabel;
        private Label _currentCaseLabel;
        private Label _summaryLabel;
        private HelpBox _messageBox;
        private ListView _caseListView;

        [MenuItem("UnityUIFlow/Batch Runner", priority = 101)]
        public static void Open()
        {
            BatchRunnerWindow window = GetWindow<BatchRunnerWindow>();
            window.titleContent = new GUIContent("UIFlow Test Runner");
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

            _overlayRenderer = new HighlightOverlayRenderer();
            SubscribeEvents();
            BuildUi();
            RefreshUi();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            _overlayRenderer?.Detach();
            _overlayRenderer = null;
            BatchRunnerPreferences.Save(_state);
        }

        private void SubscribeEvents()
        {
            HeadedRunEventBus.RunAttached += OnRunAttached;
            HeadedRunEventBus.StepStarted += OnStepStarted;
            HeadedRunEventBus.StepCompleted += OnStepCompleted;
            HeadedRunEventBus.HighlightedElementChanged += OnHighlightedElementChanged;
            HeadedRunEventBus.Failure += OnFailure;
            HeadedRunEventBus.RunFinished += OnRunFinished;
        }

        private void UnsubscribeEvents()
        {
            HeadedRunEventBus.RunAttached -= OnRunAttached;
            HeadedRunEventBus.StepStarted -= OnStepStarted;
            HeadedRunEventBus.StepCompleted -= OnStepCompleted;
            HeadedRunEventBus.HighlightedElementChanged -= OnHighlightedElementChanged;
            HeadedRunEventBus.Failure -= OnFailure;
            HeadedRunEventBus.RunFinished -= OnRunFinished;
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

            _targetModeField = new EnumField("Target Mode", _state.TargetMode) { name = "batch-target-mode" };
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

            targetModeRow.Add(CreateButton("Open Test Runner", TestRunnerWindow.Open));
            targetModeRow.Add(CreateButton("Settings", OpenSettings));

            var targetPathRow = new VisualElement();
            targetPathRow.style.flexDirection = FlexDirection.Row;
            targetPathRow.style.alignItems = Align.Center;
            targetPathRow.style.marginTop = 8;
            rootVisualElement.Add(targetPathRow);

            _targetPathField = new TextField("YAML Target")
            {
                name = "batch-target-path",
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
                name = "batch-report-path",
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

            _headedToggle = CreateToggle("Headed", "batch-headed-toggle", _state.Headed, value => _state.Headed = value);
            _stopOnFirstFailureToggle = CreateToggle("Stop On First Failure", "batch-stop-on-first-failure-toggle", _state.StopOnFirstFailure, value => _state.StopOnFirstFailure = value);
            _continueOnStepFailureToggle = CreateToggle("Continue On Step Failure", "batch-continue-on-step-failure-toggle", _state.ContinueOnStepFailure, value => _state.ContinueOnStepFailure = value);
            _screenshotOnFailureToggle = CreateToggle("Screenshot On Failure", "batch-screenshot-on-failure-toggle", _state.ScreenshotOnFailure, value => _state.ScreenshotOnFailure = value);
            _verboseLogToggle = CreateToggle("Verbose Log", "batch-verbose-log-toggle", _state.EnableVerboseLog, value => _state.EnableVerboseLog = value);
            _requireOfficialHostToggle = CreateToggle("Require Official Host", "batch-require-official-host-toggle", _state.RequireOfficialHost, value => _state.RequireOfficialHost = value);
            _requireOfficialPointerDriverToggle = CreateToggle("Require Official Pointer Driver", "batch-require-official-pointer-toggle", _state.RequireOfficialPointerDriver, value => _state.RequireOfficialPointerDriver = value);
            _requireInputSystemKeyboardDriverToggle = CreateToggle("Require InputSystem Keyboard Driver", "batch-require-inputsystem-keyboard-toggle", _state.RequireInputSystemKeyboardDriver, value => _state.RequireInputSystemKeyboardDriver = value);

            optionsGrid.Add(_headedToggle);
            optionsGrid.Add(_stopOnFirstFailureToggle);
            optionsGrid.Add(_continueOnStepFailureToggle);
            optionsGrid.Add(_screenshotOnFailureToggle);
            optionsGrid.Add(_verboseLogToggle);
            optionsGrid.Add(_requireOfficialHostToggle);
            optionsGrid.Add(_requireOfficialPointerDriverToggle);
            optionsGrid.Add(_requireInputSystemKeyboardDriverToggle);

            _defaultTimeoutField = new IntegerField("Default Timeout (ms)")
            {
                name = "batch-timeout",
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

            _runAllButton = CreateButton("Run All", RunAll);
            _runAllButton.name = "batch-run-all-button";
            _runSelectedButton = CreateButton("Run Selected", RunSelected);
            _runSelectedButton.name = "batch-run-selected-button";
            _rerunFailedButton = CreateButton("Rerun Failed", RunFailed);
            _rerunFailedButton.name = "batch-rerun-failed-button";
            _cancelButton = CreateButton("Cancel", CancelRun);
            _cancelButton.name = "batch-cancel-button";
            actionRow.Add(_runAllButton);
            actionRow.Add(_runSelectedButton);
            actionRow.Add(_rerunFailedButton);
            actionRow.Add(_cancelButton);

            var selectionRow = new VisualElement();
            selectionRow.style.flexDirection = FlexDirection.Row;
            selectionRow.style.marginBottom = 4;
            rootVisualElement.Add(selectionRow);

            _selectAllButton = CreateButton("Select All", SelectAll);
            _selectNoneButton = CreateButton("Select None", SelectNone);
            _selectFailedButton = CreateButton("Select Failed", SelectFailed);
            selectionRow.Add(_selectAllButton);
            selectionRow.Add(_selectNoneButton);
            selectionRow.Add(_selectFailedButton);

            _statusLabel = new Label { name = "batch-status-label" };
            _currentYamlLabel = new Label { name = "batch-current-yaml-label" };
            _currentCaseLabel = new Label { name = "batch-current-case-label" };
            _summaryLabel = new Label { name = "batch-summary-label" };
            rootVisualElement.Add(_statusLabel);
            rootVisualElement.Add(_currentYamlLabel);
            rootVisualElement.Add(_currentCaseLabel);
            rootVisualElement.Add(_summaryLabel);

            _messageBox = new HelpBox(string.Empty, HelpBoxMessageType.None) { name = "batch-message-box" };
            _messageBox.style.marginTop = 6;
            rootVisualElement.Add(_messageBox);

            _caseListView = new ListView
            {
                name = "batch-case-list",
                selectionType = SelectionType.Single,
                reorderable = false,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                fixedItemHeight = 28,
                style = { flexGrow = 1, marginTop = 8 },
            };
            _caseListView.itemsSource = _state.Cases;
            _caseListView.makeItem = MakeCaseListItem;
            _caseListView.bindItem = BindCaseListItem;
            rootVisualElement.Add(_caseListView);
        }

        private Toggle CreateToggle(string label, string name, bool currentValue, Action<bool> onChanged)
        {
            var toggle = new Toggle(label)
            {
                name = name,
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

        private VisualElement MakeCaseListItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;

            var checkToggle = new Toggle { name = "case-check" };
            checkToggle.style.marginRight = 4;
            checkToggle.style.marginLeft = 2;

            var statusLabel = new Label { name = "case-status" };
            statusLabel.style.width = 20;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLabel.style.marginRight = 4;

            var nameLabel = new Label { name = "case-name" };
            nameLabel.style.flexGrow = 1;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            var durationLabel = new Label { name = "case-duration" };
            durationLabel.style.width = 70;
            durationLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            durationLabel.style.color = new Color(0.6f, 0.6f, 0.6f);

            row.Add(checkToggle);
            row.Add(statusLabel);
            row.Add(nameLabel);
            row.Add(durationLabel);

            return row;
        }

        private void BindCaseListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _state.Cases.Count)
            {
                return;
            }

            BatchRunnerCaseItem item = _state.Cases[index];

            var checkToggle = element.Q<Toggle>("case-check");
            checkToggle.SetValueWithoutNotify(item.IsChecked);
            checkToggle.RegisterValueChangedCallback(evt => item.IsChecked = evt.newValue);

            var statusLabel = element.Q<Label>("case-status");
            statusLabel.text = GetStatusIcon(item.Status);
            statusLabel.style.color = GetStatusColor(item.Status);

            var nameLabel = element.Q<Label>("case-name");
            nameLabel.text = item.CaseName;
            if (item.IsRunning)
            {
                nameLabel.style.color = new Color(0.3f, 0.5f, 1f);
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                nameLabel.style.color = Color.white;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            var durationLabel = element.Q<Label>("case-duration");
            durationLabel.text = item.DurationMs > 0 ? $"{item.DurationMs}ms" : string.Empty;
        }

        private static string GetStatusIcon(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed: return "●";
                case TestStatus.Failed: return "●";
                case TestStatus.Error: return "●";
                case TestStatus.Skipped: return "○";
                default: return " ";
            }
        }

        private static Color GetStatusColor(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed: return new Color(0.2f, 0.8f, 0.2f);
                case TestStatus.Failed: return new Color(0.9f, 0.2f, 0.2f);
                case TestStatus.Error: return new Color(0.8f, 0.1f, 0.1f);
                case TestStatus.Skipped: return new Color(0.5f, 0.5f, 0.5f);
                default: return Color.gray;
            }
        }

        private void SelectAll()
        {
            foreach (BatchRunnerCaseItem item in _state.Cases)
            {
                item.IsChecked = true;
            }
            _caseListView?.Rebuild();
        }

        private void SelectNone()
        {
            foreach (BatchRunnerCaseItem item in _state.Cases)
            {
                item.IsChecked = false;
            }
            _caseListView?.Rebuild();
        }

        private void SelectFailed()
        {
            foreach (BatchRunnerCaseItem item in _state.Cases)
            {
                item.IsChecked = item.Status == TestStatus.Failed || item.Status == TestStatus.Error;
            }
            _caseListView?.Rebuild();
        }

        private async void RunAll()
        {
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

            await ExecuteBatchAsync(yamlPaths, _ => true, _runCts?.Token ?? default);
        }

        private async void RunSelected()
        {
            if (_state.Cases.Count == 0)
            {
                SetMessage("No cases loaded. Please run 'Run All' first or set a valid target.", HelpBoxMessageType.Warning);
                return;
            }

            List<string> selectedPaths = _state.Cases
                .Where(c => c.IsChecked)
                .Select(c => c.YamlPath)
                .Distinct()
                .Select(p => Path.GetFullPath(p))
                .ToList();

            if (selectedPaths.Count == 0)
            {
                SetMessage("No cases selected.", HelpBoxMessageType.Warning);
                return;
            }

            await ExecuteBatchAsync(selectedPaths, _ => true, _runCts?.Token ?? default);
        }

        private async void RunFailed()
        {
            if (_state.Cases.Count == 0)
            {
                SetMessage("No cases loaded. Please run 'Run All' first.", HelpBoxMessageType.Warning);
                return;
            }

            List<string> failedPaths = _state.Cases
                .Where(c => c.Status == TestStatus.Failed || c.Status == TestStatus.Error)
                .Select(c => c.YamlPath)
                .Distinct()
                .Select(p => Path.GetFullPath(p))
                .ToList();

            if (failedPaths.Count == 0)
            {
                SetMessage("No failed cases to rerun.", HelpBoxMessageType.Info);
                return;
            }

            await ExecuteBatchAsync(failedPaths, _ => true, _runCts?.Token ?? default);
        }

        private async System.Threading.Tasks.Task ExecuteBatchAsync(List<string> yamlPaths, Func<string, bool> filter, CancellationToken cancellationToken)
        {
            if (_state.IsRunning)
            {
                SetMessage("A batch execution is already running.", HelpBoxMessageType.Warning);
                return;
            }

            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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

            // Reset case states for this run: keep existing items if they match, otherwise rebuild
            var existingCases = new Dictionary<string, BatchRunnerCaseItem>(StringComparer.OrdinalIgnoreCase);
            foreach (BatchRunnerCaseItem existing in _state.Cases)
            {
                existingCases[existing.YamlPath] = existing;
            }
            _state.Cases.Clear();

            foreach (string yamlPath in yamlPaths)
            {
                string relPath = BatchRunnerPathUtility.MakeProjectRelative(yamlPath);
                if (existingCases.TryGetValue(relPath, out BatchRunnerCaseItem existing))
                {
                    existing.Status = TestStatus.None;
                    existing.DurationMs = 0;
                    existing.ErrorCode = null;
                    existing.ErrorMessage = null;
                    existing.IsRunning = false;
                    _state.Cases.Add(existing);
                }
                else
                {
                    _state.Cases.Add(new BatchRunnerCaseItem { YamlPath = relPath, IsChecked = true });
                }
            }

            BatchRunnerPreferences.Save(_state);
            SetMessage($"Queued {yamlPaths.Count} YAML file(s).", HelpBoxMessageType.Info);
            RefreshUi();

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
                    if (_runCts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    _state.CurrentYamlPath = BatchRunnerPathUtility.MakeProjectRelative(yamlPath);
                    _state.CurrentCaseName = null;

                    BatchRunnerCaseItem caseItem = _state.Cases.FirstOrDefault(c =>
                        StringComparer.OrdinalIgnoreCase.Equals(c.YamlPath, _state.CurrentYamlPath));
                    if (caseItem != null)
                    {
                        caseItem.IsRunning = true;
                    }

                    RefreshUi();

                    TestResult result;
                    try
                    {
                        result = await runner.RunFileAsync(
                            yamlPath,
                            new TestOptions
                            {
                                Headed = _state.Headed,
                                DebugOnFailure = false,
                                ReportOutputPath = reportRoot,
                                ScreenshotPath = screenshotRoot,
                                ScreenshotOnFailure = _state.ScreenshotOnFailure,
                                StopOnFirstFailure = _state.StopOnFirstFailure,
                                ContinueOnStepFailure = _state.ContinueOnStepFailure,
                                DefaultTimeoutMs = _state.DefaultTimeoutMs,
                                RequireOfficialHost = _state.RequireOfficialHost,
                                RequireOfficialPointerDriver = _state.RequireOfficialPointerDriver,
                                RequireInputSystemKeyboardDriver = _state.RequireInputSystemKeyboardDriver,
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
                        string fallbackCaseName = Path.GetFileNameWithoutExtension(yamlPath);
                        result = new TestResult
                        {
                            CaseName = fallbackCaseName,
                            Status = TestStatus.Error,
                            StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                            EndedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                            ErrorCode = ex is UnityUIFlowException flowException ? flowException.ErrorCode : ErrorCodes.CliExecutionError,
                            ErrorMessage = ex.Message,
                            ReportMarkdownPath = BatchRunnerPathUtility.MakeProjectRelative(reportPaths.BuildCaseMarkdownPath(reportRoot, fallbackCaseName)),
                        };
                    }
                    finally
                    {
                        _activeContext = null;
                    }

                    suite.CaseResults.Add(result);
                    ApplyCaseCounters(result.Status);

                    if (caseItem != null)
                    {
                        caseItem.IsRunning = false;
                        caseItem.CaseName = result.CaseName;
                        caseItem.Status = result.Status;
                        caseItem.DurationMs = result.DurationMs;
                        caseItem.ErrorCode = result.ErrorCode;
                        caseItem.ErrorMessage = result.ErrorMessage;
                        caseItem.FailedStepName = result.StepResults?.FirstOrDefault(s => s.Status == TestStatus.Failed || s.Status == TestStatus.Error)?.DisplayName;
                        caseItem.FailedStepError = result.StepResults?.FirstOrDefault(s => s.Status == TestStatus.Failed || s.Status == TestStatus.Error)?.ErrorMessage;
                        caseItem.ReportMarkdownPath = BatchRunnerPathUtility.MakeProjectRelative(reportPaths.BuildCaseMarkdownPath(reportRoot, result.CaseName));
                        caseItem.ReportJsonPath = BatchRunnerPathUtility.MakeProjectRelative(reportPaths.BuildCaseJsonPath(reportRoot, result.CaseName));
                        result.ReportMarkdownPath = caseItem.ReportMarkdownPath;
                    }
                    else
                    {
                        var snapshot = BuildSnapshot(result, yamlPath, reportRoot, reportPaths);
                        _state.Cases.Add(snapshot);
                        result.ReportMarkdownPath = snapshot.ReportMarkdownPath;
                    }

                    _state.CurrentCaseName = null;
                    _state.StatusText = result.Status == TestStatus.Passed
                        ? $"Passed: {result.CaseName}"
                        : $"Failed: {result.CaseName}";

                    if (result.Status == TestStatus.Failed || result.Status == TestStatus.Error)
                    {
                        string failureMessage = BuildFailureSummary(result);
                        _state.LastError = failureMessage;
                        SetMessage(failureMessage, HelpBoxMessageType.Error);
                    }
                    else
                    {
                        SetMessage($"Completed case: {result.CaseName}", HelpBoxMessageType.Info);
                    }

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

                // Overwrite unified suite report with batch results
                try
                {
                    MarkdownReporter.WriteUnifiedSuiteReport(suite, overwrite: true);
                }
                catch (Exception unifiedEx)
                {
                    Debug.LogWarning($"[UnityUIFlow] 统一套件报告写入失败: {unifiedEx.Message}");
                }

                if (_runCts.Token.IsCancellationRequested)
                {
                    _state.StatusText = "Aborted";
                    _state.LastError = "Execution was cancelled by user.";
                    SetMessage(_state.LastError, HelpBoxMessageType.Warning);
                }
                else if (suite.Failed > 0 || suite.Errors > 0)
                {
                    _state.StatusText = "Failed";
                    SetMessage($"Completed {suite.Total} case(s): passed={suite.Passed}, failed={suite.Failed}, errors={suite.Errors}, skipped={suite.Skipped}.", HelpBoxMessageType.Error);
                }
                else
                {
                    _state.StatusText = "Completed";
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
                _runCts?.Dispose();
                _runCts = null;
                _activeContext = null;
                BatchRunnerPreferences.Save(_state);
                RefreshUi();
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

        private string BuildFailureSummary(TestResult result)
        {
            StepResult failedStep = result.StepResults?.FirstOrDefault(step => step.Status == TestStatus.Failed || step.Status == TestStatus.Error);
            string detail = failedStep?.ErrorMessage ?? result.ErrorMessage ?? "Unknown failure.";
            string code = failedStep?.ErrorCode ?? result.ErrorCode;
            string stepName = failedStep?.DisplayName;

            if (!string.IsNullOrWhiteSpace(stepName))
            {
                return string.IsNullOrWhiteSpace(code)
                    ? $"Case {result.CaseName} failed at step '{stepName}': {detail}"
                    : $"Case {result.CaseName} failed at step '{stepName}' ({code}): {detail}";
            }

            return string.IsNullOrWhiteSpace(code)
                ? $"Case {result.CaseName} failed: {detail}"
                : $"Case {result.CaseName} failed ({code}): {detail}";
        }

        private BatchRunnerCaseItem BuildSnapshot(TestResult result, string yamlPath, string reportRoot, ReportPathBuilder reportPaths)
        {
            StepResult failedStep = result.StepResults?.FirstOrDefault(step => step.Status == TestStatus.Failed || step.Status == TestStatus.Error);
            return new BatchRunnerCaseItem
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
                IsChecked = true,
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

            if (_runAllButton != null)
            {
                _runAllButton.SetEnabled(!_state.IsRunning);
            }

            if (_runSelectedButton != null)
            {
                _runSelectedButton.SetEnabled(!_state.IsRunning && _state.Cases.Any(c => c.IsChecked));
            }

            if (_rerunFailedButton != null)
            {
                _rerunFailedButton.SetEnabled(!_state.IsRunning && _state.Cases.Any(c => c.Status == TestStatus.Failed || c.Status == TestStatus.Error));
            }

            if (_cancelButton != null)
            {
                _cancelButton.SetEnabled(_state.IsRunning);
            }

            _caseListView?.Rebuild();
            Repaint();
        }

        // ── HeadedRunEventBus handlers ──

        private void OnRunAttached(RuntimeController controller, string caseName)
        {
            if (_activeContext?.RuntimeController != controller)
            {
                return;
            }

            _state.CurrentCaseName = caseName;
            RefreshUi();
        }

        private void OnStepStarted(ExecutableStep step)
        {
            // Optional: could show current step name in a sub-label
        }

        private void OnStepCompleted(ExecutableStep step, StepResult result, VisualElement element)
        {
            if (element != null)
            {
                _overlayRenderer.Highlight(element);
            }
            else if (result.Status != TestStatus.Failed)
            {
                _overlayRenderer.Clear();
            }
        }

        private void OnHighlightedElementChanged(ExecutableStep step, VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EditorWindow targetWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(w => w != null && w.rootVisualElement?.panel == element.panel);

            if (targetWindow != null && targetWindow != this)
            {
                _overlayRenderer.Attach(targetWindow);
            }

            _overlayRenderer.Highlight(element);
        }

        private void OnFailure(ExecutableStep step, StepResult result)
        {
            // Error is already captured in RunFileAsync result handling
        }

        private void OnRunFinished(TestResult result)
        {
            _overlayRenderer.Clear();
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
            state.RequireOfficialHost = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.RequireOfficialHost), UnityUIFlowProjectSettings.instance.RequireOfficialHostByDefault);
            state.RequireOfficialPointerDriver = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.RequireOfficialPointerDriver), UnityUIFlowProjectSettings.instance.RequireOfficialPointerDriverByDefault);
            state.RequireInputSystemKeyboardDriver = EditorPrefs.GetBool(Prefix + nameof(BatchRunnerViewState.RequireInputSystemKeyboardDriver), UnityUIFlowProjectSettings.instance.RequireInputSystemKeyboardDriverByDefault);
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
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.RequireOfficialHost), state.RequireOfficialHost);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.RequireOfficialPointerDriver), state.RequireOfficialPointerDriver);
            EditorPrefs.SetBool(Prefix + nameof(BatchRunnerViewState.RequireInputSystemKeyboardDriver), state.RequireInputSystemKeyboardDriver);
            EditorPrefs.SetInt(Prefix + nameof(BatchRunnerViewState.DefaultTimeoutMs), state.DefaultTimeoutMs);
        }
    }
}
