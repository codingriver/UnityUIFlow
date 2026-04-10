using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow.Examples
{
    internal static class ExampleAssetPaths
    {
        public const string CommonUss = "Assets/Examples/Uss/ExampleCommon.uss";
    }

    public abstract class ExampleAcceptanceWindowBase : EditorWindow, IUnityUIFlowTestHostWindow
    {
        protected abstract string UxmlPath { get; }

        protected virtual string WindowTitle => GetType().Name;

        public void PrepareForAutomatedTest()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(420f, 260f);
            BuildUi();
        }

        protected virtual void OnEnable()
        {
            PrepareForAutomatedTest();
        }

        protected void BuildUi()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            StyleSheet common = AssetDatabase.LoadAssetAtPath<StyleSheet>(ExampleAssetPaths.CommonUss);
            if (tree == null)
            {
                throw new UnityUIFlowException(ErrorCodes.RootElementMissing, $"Missing UXML asset: {UxmlPath}");
            }

            if (common == null)
            {
                throw new UnityUIFlowException(ErrorCodes.RootElementMissing, $"Missing USS asset: {ExampleAssetPaths.CommonUss}");
            }

            rootVisualElement.Clear();
            if (!rootVisualElement.styleSheets.Contains(common))
            {
                rootVisualElement.styleSheets.Add(common);
            }

            tree.CloneTree(rootVisualElement);
            AfterBuild();
        }

        protected abstract void AfterBuild();
    }

    public sealed class ExampleBasicLoginWindow : ExampleAcceptanceWindowBase
    {
        private TextField _username;
        private TextField _password;
        private Label _status;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleBasicLoginWindow.uxml";

        protected override string WindowTitle => "Example Basic Login";

        protected override void AfterBuild()
        {
            _username = rootVisualElement.Q<TextField>("username-input");
            _password = rootVisualElement.Q<TextField>("password-input");
            _status = rootVisualElement.Q<Label>("status-label");
            Button loginButton = rootVisualElement.Q<Button>("login-button");

            _status.text = "Idle";
            loginButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                bool success = !string.IsNullOrWhiteSpace(_username.value) && !string.IsNullOrWhiteSpace(_password.value);
                _status.text = success ? $"Welcome {_username.value}" : "Missing credentials";
            });
        }
    }

    public sealed class ExampleSelectorsWindow : ExampleAcceptanceWindowBase
    {
        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleSelectorsWindow.uxml";

        protected override string WindowTitle => "Example Selectors";

        protected override void AfterBuild()
        {
            Button inspectButton = rootVisualElement.Q<Button>("inspect-button");
            Label status = rootVisualElement.Q<Label>("selector-status");
            inspectButton.tooltip = "Inspect";
            inspectButton.userData = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["data-role"] = "primary",
            };
            status.text = "Inspect ready";
            inspectButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                status.text = "Inspect ready";
            });
        }
    }

    public sealed class ExampleWaitForElementWindow : ExampleAcceptanceWindowBase
    {
        private Label _message;
        private int _revealFrames;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleWaitForElementWindow.uxml";

        protected override string WindowTitle => "Example Wait For Element";

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        protected override void AfterBuild()
        {
            _message = rootVisualElement.Q<Label>("delayed-message");
            Button startButton = rootVisualElement.Q<Button>("start-button");
            _revealFrames = 0;
            _message.style.display = DisplayStyle.None;
            _message.text = "Pending";

            startButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                _message.style.display = DisplayStyle.None;
                _message.text = "Pending";
                _revealFrames = 3;
            });
        }

        private void Tick()
        {
            if (_revealFrames <= 0 || _message == null)
            {
                return;
            }

            _revealFrames--;
            if (_revealFrames == 0)
            {
                _message.text = "Ready";
                _message.style.display = DisplayStyle.Flex;
            }
        }
    }

    public sealed class ExampleConditionalLoopWindow : ExampleAcceptanceWindowBase
    {
        private VisualElement _toastHost;
        private double _toastHideAtTime;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleConditionalLoopWindow.uxml";

        protected override string WindowTitle => "Example Conditional Loop";

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        protected override void AfterBuild()
        {
            _toastHost = rootVisualElement.Q<VisualElement>("toast-host");
            Button saveButton = rootVisualElement.Q<Button>("save-button");
            Button resetButton = rootVisualElement.Q<Button>("reset-button");

            RemoveToast();
            _toastHideAtTime = 0d;

            saveButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                ShowToast();
                _toastHideAtTime = EditorApplication.timeSinceStartup + 0.25d;
            });

            resetButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                RemoveToast();
                _toastHideAtTime = 0d;
            });
        }

        private void Tick()
        {
            if (_toastHideAtTime <= 0d || _toastHost == null)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup >= _toastHideAtTime)
            {
                RemoveToast();
                _toastHideAtTime = 0d;
            }
        }

        private void ShowToast()
        {
            RemoveToast();

            var toast = new Label("Saved")
            {
                name = "toast-message",
            };
            toast.AddToClassList("example-toast");
            _toastHost.Add(toast);
        }

        private void RemoveToast()
        {
            if (_toastHost == null)
            {
                return;
            }

            VisualElement toast = _toastHost.Q<VisualElement>("toast-message");
            if (toast != null)
            {
                toast.RemoveFromHierarchy();
            }
        }
    }

    public sealed class ExampleCsvLoginWindow : ExampleAcceptanceWindowBase
    {
        private TextField _username;
        private TextField _password;
        private Label _status;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleCsvLoginWindow.uxml";

        protected override string WindowTitle => "Example CSV Login";

        protected override void AfterBuild()
        {
            _username = rootVisualElement.Q<TextField>("username-input");
            _password = rootVisualElement.Q<TextField>("password-input");
            _status = rootVisualElement.Q<Label>("status-label");
            Button loginButton = rootVisualElement.Q<Button>("login-button");
            Button resetButton = rootVisualElement.Q<Button>("reset-button");

            _status.text = "Idle";
            loginButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                _status.text = $"Welcome {_username.value}";
            });

            resetButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                _username.value = string.Empty;
                _password.value = string.Empty;
                _status.text = "Idle";
            });
        }
    }

    public sealed class ExampleCustomActionWindow : ExampleAcceptanceWindowBase
    {
        private TextField _username;
        private TextField _password;
        private Label _status;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleCustomActionWindow.uxml";

        protected override string WindowTitle => "Example Custom Action";

        protected override void AfterBuild()
        {
            _username = rootVisualElement.Q<TextField>("username-input");
            _password = rootVisualElement.Q<TextField>("password-input");
            _status = rootVisualElement.Q<Label>("status-label");
            Button loginButton = rootVisualElement.Q<Button>("login-button");

            _status.text = "Idle";
            loginButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                _status.text = $"Welcome {_username.value}";
            });
        }
    }

    public sealed class ExampleDoubleClickWindow : ExampleAcceptanceWindowBase
    {
        private int _count;
        private Label _status;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleDoubleClickWindow.uxml";

        protected override string WindowTitle => "Example Double Click";

        protected override void AfterBuild()
        {
            _status = rootVisualElement.Q<Label>("double-status");
            Button button = rootVisualElement.Q<Button>("double-button");
            _count = 0;
            _status.text = "Double Count: 0";

            button.RegisterCallback<MouseUpEvent>(_ =>
            {
                _count++;
                _status.text = $"Double Count: {_count}";
            });
        }
    }

    public sealed class ExamplePressKeyWindow : ExampleAcceptanceWindowBase
    {
        protected override string UxmlPath => "Assets/Examples/Uxml/ExamplePressKeyWindow.uxml";

        protected override string WindowTitle => "Example Press Key";

        protected override void AfterBuild()
        {
            Label status = rootVisualElement.Q<Label>("key-status");
            status.text = "Key: none";
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                status.text = $"Key: {evt.keyCode}";
            });
        }
    }

    public sealed class ExampleHoverWindow : ExampleAcceptanceWindowBase
    {
        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleHoverWindow.uxml";

        protected override string WindowTitle => "Example Hover";

        protected override void AfterBuild()
        {
            Label status = rootVisualElement.Q<Label>("hover-status");
            VisualElement target = rootVisualElement.Q<VisualElement>("hover-box");
            status.text = "Hover: idle";
            target.RegisterCallback<MouseOverEvent>(_ =>
            {
                status.text = "Hover: active";
            });
        }
    }

    public sealed class ExampleDragWindow : ExampleAcceptanceWindowBase
    {
        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleDragWindow.uxml";

        protected override string WindowTitle => "Example Drag";

        protected override void AfterBuild()
        {
            Label status = rootVisualElement.Q<Label>("drag-status");
            bool dragStarted = false;
            status.text = "Drag: idle";

            rootVisualElement.RegisterCallback<MouseDownEvent>(_ =>
            {
                dragStarted = true;
                status.text = "Drag: started";
            });

            rootVisualElement.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (dragStarted && evt.mouseDelta.sqrMagnitude > 0f)
                {
                    status.text = "Drag: moving";
                }
            });

            rootVisualElement.RegisterCallback<MouseUpEvent>(_ =>
            {
                if (dragStarted)
                {
                    status.text = "Drag: completed";
                    dragStarted = false;
                }
            });
        }
    }

    public sealed class ExampleScrollWindow : ExampleAcceptanceWindowBase
    {
        private ScrollView _scrollView;
        private Label _status;
        private float _reportedScrollY;

        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleScrollWindow.uxml";

        protected override string WindowTitle => "Example Scroll";

        protected override void AfterBuild()
        {
            _scrollView = rootVisualElement.Q<ScrollView>("scroll-view");
            _status = rootVisualElement.Q<Label>("scroll-status");
            _reportedScrollY = 0f;
            _status.text = "Scroll: 0";
            _scrollView.scrollOffset = Vector2.zero;
            _scrollView.RegisterCallback<WheelEvent>(evt =>
            {
                _reportedScrollY += evt.delta.y;
                if (_status != null)
                {
                    _status.text = $"Scroll: {(int)Math.Round(_reportedScrollY)}";
                }
            });
        }
    }

    public sealed class ExampleTypeTextWindow : ExampleAcceptanceWindowBase
    {
        protected override string UxmlPath => "Assets/Examples/Uxml/ExampleTypeTextWindow.uxml";

        protected override string WindowTitle => "Example Type Text";

        protected override void AfterBuild()
        {
            TextField input = rootVisualElement.Q<TextField>("type-text-input");
            input.value = string.Empty;
        }
    }
}
