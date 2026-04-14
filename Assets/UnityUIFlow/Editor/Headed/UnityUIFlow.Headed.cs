using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    /// <summary>
    /// Event bus used by the headed UI.
    /// </summary>
    public static class HeadedRunEventBus
    {
        public static event Action<RuntimeController, string> RunAttached;
        public static event Action<ExecutableStep> StepStarted;
        public static event Action<ExecutableStep, StepResult, VisualElement> StepCompleted;
        public static event Action<ExecutableStep, VisualElement> HighlightedElementChanged;
        public static event Action<ExecutableStep, StepResult> Failure;
        public static event Action<TestResult> RunFinished;

        public static void PublishRunAttached(RuntimeController controller, string caseName) => RunAttached?.Invoke(controller, caseName);
        public static void PublishStepStarted(ExecutableStep step) => StepStarted?.Invoke(step);
        public static void PublishStepCompleted(ExecutableStep step, StepResult result, VisualElement element) => StepCompleted?.Invoke(step, result, element);
        public static void PublishHighlightedElement(ExecutableStep step, VisualElement element) => HighlightedElementChanged?.Invoke(step, element);
        public static void PublishFailure(ExecutableStep step, StepResult result) => Failure?.Invoke(step, result);
        public static void PublishRunFinished(TestResult result) => RunFinished?.Invoke(result);
    }

    /// <summary>
    /// Renders a highlight overlay above a target window.
    /// </summary>
    public sealed class HighlightOverlayRenderer
    {
        private EditorWindow _window;
        private VisualElement _overlayRoot;
        private VisualElement _marker;

        public void Attach(EditorWindow window)
        {
            if (_window == window && _overlayRoot != null)
            {
                return;
            }

            Detach();
            _window = window;
            if (_window == null || _window.rootVisualElement == null)
            {
                return;
            }

            _overlayRoot = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            _overlayRoot.style.position = Position.Absolute;
            _overlayRoot.style.left = 0;
            _overlayRoot.style.top = 0;
            _overlayRoot.style.right = 0;
            _overlayRoot.style.bottom = 0;

            _marker = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            _marker.style.position = Position.Absolute;
            _marker.style.backgroundColor = new Color(1f, 0f, 0f, 0.2f);
            _marker.style.borderBottomColor = Color.red;
            _marker.style.borderLeftColor = Color.red;
            _marker.style.borderRightColor = Color.red;
            _marker.style.borderTopColor = Color.red;
            _marker.style.borderBottomWidth = 2;
            _marker.style.borderLeftWidth = 2;
            _marker.style.borderRightWidth = 2;
            _marker.style.borderTopWidth = 2;
            _marker.style.display = DisplayStyle.None;

            _overlayRoot.Add(_marker);
            _window.rootVisualElement.Add(_overlayRoot);
        }

        public void Highlight(VisualElement target)
        {
            if (target == null || target.panel == null || _marker == null)
            {
                Clear();
                return;
            }

            Rect worldBound = target.worldBound;
            _marker.style.left = worldBound.xMin;
            _marker.style.top = worldBound.yMin;
            _marker.style.width = worldBound.width;
            _marker.style.height = worldBound.height;
            _marker.style.display = DisplayStyle.Flex;
        }

        public void Clear()
        {
            if (_marker != null)
            {
                _marker.style.display = DisplayStyle.None;
            }
        }

        public void Detach()
        {
            if (_overlayRoot != null && _overlayRoot.parent != null)
            {
                _overlayRoot.parent.Remove(_overlayRoot);
            }

            _overlayRoot = null;
            _marker = null;
            _window = null;
        }
    }

    /// <summary>
    /// Editor headed runner panel.
    /// </summary>
    public sealed class HeadedTestWindow : EditorWindow
    {
        private readonly HeadedPanelState _state = new HeadedPanelState();
        private readonly HighlightOverlayRenderer _overlayRenderer = new HighlightOverlayRenderer();
        private RuntimeController _runtimeController;
        private TextField _yamlPathField;
        private Label _statusLabel;
        private Label _stepLabel;
        private Label _driverLabel;
        private Label _driverDetailsLabel;
        private HelpBox _errorBox;

        [MenuItem("UnityUIFlow/UnityUIFlow")]
        public static void Open()
        {
            HeadedTestWindow window = GetWindow<HeadedTestWindow>();
            window.titleContent = new GUIContent("UnityUIFlow");
            window.minSize = new Vector2(420f, 260f);
            window.Show();
        }

        private void OnEnable()
        {
            HeadedWindowPreferences.Load(_state);
            _state.SelectedYamlPath = HeadedYamlPathPreferences.Load(_state.SelectedYamlPath);
            BuildUi();
            HeadedRunEventBus.RunAttached += OnRunAttached;
            HeadedRunEventBus.StepStarted += OnStepStarted;
            HeadedRunEventBus.StepCompleted += OnStepCompleted;
            HeadedRunEventBus.HighlightedElementChanged += OnHighlightedElementChanged;
            HeadedRunEventBus.Failure += OnFailure;
            HeadedRunEventBus.RunFinished += OnRunFinished;
        }

        private void OnDisable()
        {
            HeadedRunEventBus.RunAttached -= OnRunAttached;
            HeadedRunEventBus.StepStarted -= OnStepStarted;
            HeadedRunEventBus.StepCompleted -= OnStepCompleted;
            HeadedRunEventBus.HighlightedElementChanged -= OnHighlightedElementChanged;
            HeadedRunEventBus.Failure -= OnFailure;
            HeadedRunEventBus.RunFinished -= OnRunFinished;
            _overlayRenderer.Detach();
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;

            var yamlPathRow = new VisualElement();
            yamlPathRow.style.flexDirection = FlexDirection.Row;
            yamlPathRow.style.alignItems = Align.Center;
            rootVisualElement.Add(yamlPathRow);

            _yamlPathField = new TextField("YAML Path")
            {
                value = _state.SelectedYamlPath ?? string.Empty,
            };
            _yamlPathField.style.flexGrow = 1;
            _yamlPathField.RegisterValueChangedCallback(evt => SetSelectedYamlPath(evt.newValue));
            yamlPathRow.Add(_yamlPathField);

            Button browseButton = CreateButton("Browse", BrowseYamlPath);
            browseButton.style.marginRight = 0;
            yamlPathRow.Add(browseButton);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 8;
            buttonRow.style.marginBottom = 8;
            rootVisualElement.Add(buttonRow);

            buttonRow.Add(CreateButton("Run", RunSelected));
            buttonRow.Add(CreateButton("Pause", () => _runtimeController?.Pause()));
            buttonRow.Add(CreateButton("Resume", () => _runtimeController?.Resume()));
            buttonRow.Add(CreateButton("Step", () => _runtimeController?.StepOnce()));
            buttonRow.Add(CreateButton("Stop", () => _runtimeController?.Stop()));
            buttonRow.Add(CreateButton("Settings", OpenSettings));

            var runModeField = new EnumField("Run Mode", _state.RunMode);
            runModeField.RegisterValueChangedCallback(evt =>
            {
                _state.RunMode = (HeadedRunMode)evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(runModeField);

            var failurePolicyField = new EnumField("Failure Policy", _state.FailurePolicy);
            failurePolicyField.RegisterValueChangedCallback(evt =>
            {
                _state.FailurePolicy = (HeadedFailurePolicy)evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(failurePolicyField);

            var continueToggle = new Toggle("Continue After Step Failure")
            {
                value = _state.ContinueOnStepFailure,
            };
            continueToggle.RegisterValueChangedCallback(evt =>
            {
                _state.ContinueOnStepFailure = evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(continueToggle);

            var requireOfficialHostToggle = new Toggle("Require Official Host")
            {
                value = _state.RequireOfficialHost,
            };
            requireOfficialHostToggle.RegisterValueChangedCallback(evt =>
            {
                _state.RequireOfficialHost = evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(requireOfficialHostToggle);

            var requireOfficialPointerToggle = new Toggle("Require Official Pointer Driver")
            {
                value = _state.RequireOfficialPointerDriver,
            };
            requireOfficialPointerToggle.RegisterValueChangedCallback(evt =>
            {
                _state.RequireOfficialPointerDriver = evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(requireOfficialPointerToggle);

            var requireInputSystemKeyboardToggle = new Toggle("Require InputSystem Keyboard Driver")
            {
                value = _state.RequireInputSystemKeyboardDriver,
            };
            requireInputSystemKeyboardToggle.RegisterValueChangedCallback(evt =>
            {
                _state.RequireInputSystemKeyboardDriver = evt.newValue;
                HeadedWindowPreferences.Save(_state);
            });
            rootVisualElement.Add(requireInputSystemKeyboardToggle);

            _statusLabel = new Label("Status: Idle");
            _stepLabel = new Label("Current Step: -");
            _driverLabel = new Label("Drivers: H=- | P=- | K=-");
            _driverDetailsLabel = new Label("Driver Details: -");
            _driverDetailsLabel.style.whiteSpace = WhiteSpace.Normal;
            _errorBox = new HelpBox(string.Empty, HelpBoxMessageType.None);
            _errorBox.style.display = DisplayStyle.None;

            rootVisualElement.Add(_statusLabel);
            rootVisualElement.Add(_stepLabel);
            rootVisualElement.Add(_driverLabel);
            rootVisualElement.Add(_driverDetailsLabel);
            rootVisualElement.Add(_errorBox);
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
            if (string.IsNullOrWhiteSpace(_state.SelectedYamlPath))
            {
                ShowError("YAML Path is null");
                return;
            }

            if (_state.RunnerState == HeadedRunnerState.Running || _state.RunnerState == HeadedRunnerState.Paused)
            {
                ShowError("Runner is already running");
                return;
            }

            if (Application.isBatchMode)
            {
                ShowError("Cannot run headed tests in batch mode");
                _state.RunnerState = HeadedRunnerState.Idle;
                RefreshLabels();
                return;
            }

            ClearError();
            _state.RunnerState = HeadedRunnerState.Running;
            RefreshLabels();

            try
            {
                var runner = new TestRunner();
                await runner.RunFileAsync(_state.SelectedYamlPath, new TestOptions
                {
                    Headed = true,
                    DebugOnFailure = _state.FailurePolicy == HeadedFailurePolicy.Pause,
                    ReportOutputPath = "Reports",
                    ScreenshotPath = "Reports/Screenshots",
                    ScreenshotOnFailure = true,
                    ContinueOnStepFailure = _state.ContinueOnStepFailure,
                    RequireOfficialHost = _state.RequireOfficialHost,
                    RequireOfficialPointerDriver = _state.RequireOfficialPointerDriver,
                    RequireInputSystemKeyboardDriver = _state.RequireInputSystemKeyboardDriver,
                    EnableVerboseLog = UnityUIFlowMenuItems.IsVerboseLogEnabled,
                });
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                _state.RunnerState = HeadedRunnerState.Failed;
                RefreshLabels();
            }
        }

        private void BrowseYamlPath()
        {
            string initialDirectory = HeadedYamlPathPreferences.GetInitialDirectory(_state.SelectedYamlPath);
            string selectedPath = EditorUtility.OpenFilePanel("Select YAML Test Case", initialDirectory, "yaml");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            string normalizedPath = HeadedYamlPathPreferences.NormalizePath(selectedPath);
            SetSelectedYamlPath(normalizedPath);
            _yamlPathField?.SetValueWithoutNotify(normalizedPath);
        }

        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(UnityUIFlowProjectSettingsUtility.SettingsPath);
        }

        private void SetSelectedYamlPath(string path)
        {
            _state.SelectedYamlPath = path;
            HeadedYamlPathPreferences.Save(path);
        }

        private void OnRunAttached(RuntimeController controller, string caseName)
        {
            _runtimeController = controller;
            _runtimeController.RunMode = _state.RunMode;
            _state.RunnerState = HeadedRunnerState.Running;
            _state.CurrentHostDriver = null;
            _state.CurrentPointerDriver = null;
            _state.CurrentKeyboardDriver = null;
            _state.CurrentDriverDetails = null;
            RefreshLabels();
        }

        private void OnStepStarted(ExecutableStep step)
        {
            _state.CurrentStepName = step.DisplayName;
            _state.CurrentSelector = step.Selector?.Raw;
            RefreshLabels();
            TryHighlightCurrent(step.Selector);
        }

        private void OnHighlightedElementChanged(ExecutableStep step, VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EditorWindow targetWindow = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(window => window != null && window.rootVisualElement?.panel == element.panel);

            if (targetWindow != null && targetWindow != this)
            {
                _overlayRenderer.Attach(targetWindow);
            }

            _overlayRenderer.Highlight(element);
        }

        private void OnStepCompleted(ExecutableStep step, StepResult result, VisualElement element)
        {
            _state.CurrentHostDriver = result.HostDriver;
            _state.CurrentPointerDriver = result.PointerDriver;
            _state.CurrentKeyboardDriver = result.KeyboardDriver;
            _state.CurrentDriverDetails = result.DriverDetails;

            if (element != null)
            {
                _overlayRenderer.Highlight(element);
            }
            else if (result.Status != TestStatus.Failed)
            {
                _overlayRenderer.Clear();
            }

            if (_runtimeController != null && _runtimeController.IsPaused)
            {
                _state.RunnerState = HeadedRunnerState.Paused;
                RefreshLabels();
            }
        }

        private void OnFailure(ExecutableStep step, StepResult result)
        {
            ShowError(result.ErrorMessage);
            _state.RunnerState = _runtimeController != null && _runtimeController.IsPaused
                ? HeadedRunnerState.Paused
                : HeadedRunnerState.Failed;
            RefreshLabels();
        }

        private void OnRunFinished(TestResult result)
        {
            _state.RunnerState = HeadedRunnerState.Idle;
            RefreshLabels();
            if (result.Status == TestStatus.Passed)
            {
                _overlayRenderer.Clear();
            }
        }

        private void TryHighlightCurrent(SelectorExpression selector)
        {
            if (selector == null)
            {
                _overlayRenderer.Clear();
                return;
            }

            var finder = new ElementFinder();
            EditorWindow targetWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(window => window != null && window != this && window.rootVisualElement != null && finder.Exists(selector, window.rootVisualElement, false));

            if (targetWindow == null)
            {
                _overlayRenderer.Clear();
                return;
            }

            _overlayRenderer.Attach(targetWindow);
            FindResult result = finder.Find(selector, targetWindow.rootVisualElement, false);
            _overlayRenderer.Highlight(result.Element);
        }

        private void ShowError(string message)
        {
            _state.LastErrorMessage = message;
            _errorBox.text = string.IsNullOrEmpty(message) ? string.Empty : message.Substring(0, Math.Min(500, message.Length));
            _errorBox.style.display = string.IsNullOrWhiteSpace(message) ? DisplayStyle.None : DisplayStyle.Flex;
            _errorBox.messageType = HelpBoxMessageType.Error;
        }

        private void ClearError()
        {
            _state.LastErrorMessage = null;
            _errorBox.text = string.Empty;
            _errorBox.style.display = DisplayStyle.None;
        }

        private void RefreshLabels()
        {
            _statusLabel.text = $"Status: {_state.RunnerState}";
            _stepLabel.text = $"Current Step: {(_state.CurrentStepName ?? "-")}";
            string host = string.IsNullOrWhiteSpace(_state.CurrentHostDriver) ? "-" : _state.CurrentHostDriver;
            string pointer = string.IsNullOrWhiteSpace(_state.CurrentPointerDriver) ? "-" : _state.CurrentPointerDriver;
            string keyboard = string.IsNullOrWhiteSpace(_state.CurrentKeyboardDriver) ? "-" : _state.CurrentKeyboardDriver;
            _driverLabel.text = $"Drivers: H={host} | P={pointer} | K={keyboard}";
            _driverDetailsLabel.text = $"Driver Details: {(_state.CurrentDriverDetails ?? "-")}";
            Repaint();
        }
    }

    internal static class HeadedWindowPreferences
    {
        private const string Prefix = "UnityUIFlow.Headed.";

        public static void Load(HeadedPanelState state)
        {
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;
            state.ContinueOnStepFailure = EditorPrefs.GetBool(Prefix + nameof(HeadedPanelState.ContinueOnStepFailure), false);
            state.RunMode = (HeadedRunMode)EditorPrefs.GetInt(Prefix + nameof(HeadedPanelState.RunMode), (int)HeadedRunMode.Continuous);
            state.FailurePolicy = (HeadedFailurePolicy)EditorPrefs.GetInt(Prefix + nameof(HeadedPanelState.FailurePolicy), (int)HeadedFailurePolicy.Pause);
            state.RequireOfficialHost = EditorPrefs.GetBool(Prefix + nameof(HeadedPanelState.RequireOfficialHost), settings.RequireOfficialHostByDefault);
            state.RequireOfficialPointerDriver = EditorPrefs.GetBool(Prefix + nameof(HeadedPanelState.RequireOfficialPointerDriver), settings.RequireOfficialPointerDriverByDefault);
            state.RequireInputSystemKeyboardDriver = EditorPrefs.GetBool(Prefix + nameof(HeadedPanelState.RequireInputSystemKeyboardDriver), settings.RequireInputSystemKeyboardDriverByDefault);
        }

        public static void Save(HeadedPanelState state)
        {
            EditorPrefs.SetBool(Prefix + nameof(HeadedPanelState.ContinueOnStepFailure), state.ContinueOnStepFailure);
            EditorPrefs.SetInt(Prefix + nameof(HeadedPanelState.RunMode), (int)state.RunMode);
            EditorPrefs.SetInt(Prefix + nameof(HeadedPanelState.FailurePolicy), (int)state.FailurePolicy);
            EditorPrefs.SetBool(Prefix + nameof(HeadedPanelState.RequireOfficialHost), state.RequireOfficialHost);
            EditorPrefs.SetBool(Prefix + nameof(HeadedPanelState.RequireOfficialPointerDriver), state.RequireOfficialPointerDriver);
            EditorPrefs.SetBool(Prefix + nameof(HeadedPanelState.RequireInputSystemKeyboardDriver), state.RequireInputSystemKeyboardDriver);
        }
    }

    internal static class HeadedYamlPathPreferences
    {
        internal const string YamlPathPrefKey = "UnityUIFlow.Headed.SelectedYamlPath";

        public static string Load(string currentValue = null)
        {
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                return currentValue;
            }

            return EditorPrefs.GetString(YamlPathPrefKey, string.Empty);
        }

        public static void Save(string path)
        {
            EditorPrefs.SetString(YamlPathPrefKey, path ?? string.Empty);
        }

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
            string candidate = string.IsNullOrWhiteSpace(path) ? Load() : path;
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                string fullPath = Path.GetFullPath(candidate);
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
    }

    /// <summary>
    /// UnityUIFlow menu bar items.
    /// </summary>
    internal static class UnityUIFlowMenuItems
    {
        private const string PrefKey = "UnityUIFlow.VerboseLog";
        private const string MenuPath = "UnityUIFlow/Enable Verbose Log";

        /// <summary>
        /// Returns whether verbose logging is currently enabled.
        /// </summary>
        public static bool IsVerboseLogEnabled => UnityUIFlowProjectSettingsUtility.IsVerboseLoggingEnabled(EditorPrefs.GetBool(PrefKey, false));

        [MenuItem(MenuPath, priority = 200)]
        private static void ToggleVerboseLog()
        {
            if (UnityUIFlowProjectSettings.instance.AlwaysEnableVerboseLog)
            {
                Debug.Log("[UnityUIFlow] Verbose log is forced on by Project Settings/UnityUIFlow.");
                SettingsService.OpenProjectSettings(UnityUIFlowProjectSettingsUtility.SettingsPath);
                return;
            }

            bool next = !EditorPrefs.GetBool(PrefKey, false);
            EditorPrefs.SetBool(PrefKey, next);
            Menu.SetChecked(MenuPath, next);
            Debug.Log($"[UnityUIFlow] Verbose log is now {(next ? "enabled" : "disabled")}.");
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleVerboseLogValidate()
        {
            Menu.SetChecked(MenuPath, IsVerboseLogEnabled);
            return true;
        }

        [MenuItem("UnityUIFlow/Settings", priority = 201)]
        private static void OpenSettingsMenu()
        {
            SettingsService.OpenProjectSettings(UnityUIFlowProjectSettingsUtility.SettingsPath);
        }
    }
}
