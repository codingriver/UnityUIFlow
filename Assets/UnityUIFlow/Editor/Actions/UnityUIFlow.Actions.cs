using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static UnityEditor.TypeCache;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    /// <summary>
    /// Describes an execution-time reporter sink.
    /// </summary>
    public interface IExecutionReporter
    {
        void RecordAction(string stepId, string actionName, string message);
    }

    /// <summary>
    /// Attribute used to declare a YAML action name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ActionNameAttribute : Attribute
    {
        public ActionNameAttribute(string actionName)
        {
            ActionName = actionName;
        }

        /// <summary>
        /// YAML action name.
        /// </summary>
        public string ActionName { get; }
    }

    /// <summary>
    /// Action execution context.
    /// </summary>
    public sealed class ActionContext
    {
        public VisualElement Root;
        public ElementFinder Finder;
        public TestOptions Options;
        public IExecutionReporter Reporter;
        public object Simulator;
        public string CurrentStepId;
        public string CurrentCaseName;
        public int CurrentStepIndex;
        public Dictionary<string, object> SharedBag = new Dictionary<string, object>(StringComparer.Ordinal);
        public CancellationToken CancellationToken;
        public ScreenshotManager ScreenshotManager;
        public RuntimeController RuntimeController;
        public readonly List<string> CurrentAttachments = new List<string>();

        public void AddAttachment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (CurrentAttachments.Count >= 10)
            {
                Debug.LogWarning($"[UnityUIFlow] {ErrorCodes.AttachmentLimitExceeded}: step {CurrentStepId} already has 10 attachments.");
                return;
            }

            CurrentAttachments.Add(path);
        }

        /// <summary>
        /// Writes a verbose log entry when EnableVerboseLog is true.
        /// </summary>
        public void Log(string message)
        {
            if (Options?.EnableVerboseLog == true)
            {
                Debug.Log($"[UnityUIFlow][{CurrentCaseName}][{CurrentStepId}] {message}");
            }
        }

        /// <summary>
        /// Returns a short display string for a visual element.
        /// </summary>
        public static string ElementInfo(VisualElement element)
        {
            if (element == null)
            {
                return "(null)";
            }

            string name = string.IsNullOrEmpty(element.name) ? string.Empty : $"#{element.name}";
            return $"{element.GetType().Name}{name}";
        }
    }

    /// <summary>
    /// Contract implemented by all actions.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Executes the action.
        /// </summary>
        Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters);
    }

    /// <summary>
    /// Resolves built-in and custom actions.
    /// </summary>
    public sealed class ActionRegistry
    {
        private readonly Dictionary<string, Type> _actions = new Dictionary<string, Type>(StringComparer.Ordinal);

        public ActionRegistry()
        {
            RegisterBuiltIns();
            RegisterCustomActions();
        }

        /// <summary>
        /// Returns true when the action exists.
        /// </summary>
        public bool HasAction(string actionName)
        {
            return !string.IsNullOrWhiteSpace(actionName) && _actions.ContainsKey(actionName);
        }

        /// <summary>
        /// Registers a specific action type.
        /// </summary>
        public void Register(string actionName, Type actionType)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionNameConflict, "Action name cannot be empty.");
            }

            if (!typeof(IAction).IsAssignableFrom(actionType))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionNameConflict, $"Action {actionName} does not implement IAction.");
            }

            if (_actions.ContainsKey(actionName))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionNameConflict, $"Duplicate action name: {actionName}");
            }

            _actions[actionName] = actionType;
        }

        /// <summary>
        /// Resolves an action instance by name.
        /// </summary>
        public IAction Resolve(string actionName)
        {
            if (!_actions.TryGetValue(actionName, out Type actionType))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionNotFound, $"Action not found: {actionName}");
            }

            try
            {
                return (IAction)Activator.CreateInstance(actionType);
            }
            catch (Exception ex)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, $"Failed to construct action {actionName}: {ex.Message}", ex);
            }
        }

        private void RegisterBuiltIns()
        {
            Register("click", typeof(ClickAction));
            Register("double_click", typeof(DoubleClickAction));
            Register("type_text", typeof(TypeTextAction));
            Register("type_text_fast", typeof(TypeTextFastAction));
            Register("press_key", typeof(PressKeyAction));
            Register("drag", typeof(DragAction));
            Register("scroll", typeof(ScrollAction));
            Register("hover", typeof(HoverAction));
            Register("wait", typeof(WaitAction));
            Register("wait_for_element", typeof(WaitForElementAction));
            Register("assert_visible", typeof(AssertVisibleAction));
            Register("assert_not_visible", typeof(AssertNotVisibleAction));
            Register("assert_text", typeof(AssertTextAction));
            Register("assert_text_contains", typeof(AssertTextContainsAction));
            Register("assert_property", typeof(AssertPropertyAction));
            Register("screenshot", typeof(ScreenshotAction));
        }

        private void RegisterCustomActions()
        {
            var discoveredTypes = new HashSet<Type>();
            foreach (Type type in GetTypesWithAttribute<ActionNameAttribute>())
            {
                if (type != null)
                {
                    discoveredTypes.Add(type);
                }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type != null && type.GetCustomAttribute<ActionNameAttribute>() != null)
                    {
                        discoveredTypes.Add(type);
                    }
                }
            }

            foreach (Type type in discoveredTypes)
            {
                if (!typeof(IAction).IsAssignableFrom(type))
                {
                    continue;
                }

                var attribute = type.GetCustomAttribute<ActionNameAttribute>();
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.ActionName) || _actions.ContainsKey(attribute.ActionName))
                {
                    continue;
                }

                _actions[attribute.ActionName] = type;
            }
        }
    }

    internal static class ActionHelpers
    {
        public static string Require(Dictionary<string, string> parameters, string actionName, string key)
        {
            if (!parameters.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} is missing parameter {key}.");
            }

            return value;
        }

        public static async Task<VisualElement> RequireElementAsync(ActionContext context, Dictionary<string, string> parameters, string actionName)
        {
            string selector = Require(parameters, actionName, "selector");
            context.Log($"{actionName}: waiting for {selector}");
            var compiledSelector = new SelectorCompiler().Compile(selector);
            FindResult result = await context.Finder.WaitForElementAsync(
                compiledSelector,
                context.Root,
                new WaitOptions
                {
                    TimeoutMs = parameters.TryGetValue("timeout", out string timeoutLiteral)
                        ? DurationParser.ParseToMilliseconds(timeoutLiteral, actionName)
                        : context.Options.DefaultTimeoutMs,
                    PollIntervalMs = 16,
                    RequireVisible = true,
                },
                context.CancellationToken);

            context.Log($"{actionName}: found {ActionContext.ElementInfo(result.Element)}");
            return result.Element;
        }

        public static string GetText(VisualElement element)
        {
            switch (element)
            {
                case TextElement textElement:
                    return textElement.text;
                default:
                    PropertyInfo valueProperty = element.GetType().GetProperty("value");
                    if (valueProperty != null)
                    {
                        object value = valueProperty.GetValue(element);
                        return value?.ToString() ?? string.Empty;
                    }

                    return string.Empty;
            }
        }

        public static bool TryAssignFieldValue(VisualElement element, string value)
        {
            switch (element)
            {
                case TextField textField:
                    textField.value = value;
                    return true;
                case IntegerField integerField when int.TryParse(value, out int intValue):
                    integerField.value = intValue;
                    return true;
                case FloatField floatField when float.TryParse(value, out float floatValue):
                    floatField.value = floatValue;
                    return true;
                case LongField longField when long.TryParse(value, out long longValue):
                    longField.value = longValue;
                    return true;
                case DoubleField doubleField when double.TryParse(value, out double doubleValue):
                    doubleField.value = doubleValue;
                    return true;
            }

            PropertyInfo property = element.GetType().GetProperty("value");
            if (property != null && property.CanWrite)
            {
                try
                {
                    object converted = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(element, converted);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static void DispatchClick(VisualElement element, int clickCount, ActionContext context = null)
        {
            var dispatchRoot = element?.panel?.visualTree ?? element;
            if (dispatchRoot == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, "Click target is unavailable.");
            }

            // worldPos is in panel/screen coordinates — required for correct hit-testing when the
            // panel dispatches the event. We intentionally dispatch through the panel visual tree so
            // UI Toolkit can perform normal picking/compatibility-event generation.
            Vector2 worldPos = element.worldBound.center;
            Vector2 localPos = element.WorldToLocal(worldPos);
            context?.Log($"click: focus {ActionContext.ElementInfo(element)} local={localPos} world={worldPos}");
            element.Focus();

            for (int index = 0; index < clickCount; index++)
            {
                int currentClickCount = index + 1;
                bool pointerDownReceived = false;
                bool pointerUpReceived = false;
                bool mouseDownReceived = false;
                bool mouseUpReceived = false;
                bool clickEventReceived = false;
                bool clickPropagationStopped = false;

                void OnPointerDown(PointerDownEvent evt) { pointerDownReceived = true; }
                void OnPointerUp(PointerUpEvent evt) { pointerUpReceived = true; }
                void OnMouseDown(MouseDownEvent evt) { mouseDownReceived = true; }
                void OnMouseUp(MouseUpEvent evt) { mouseUpReceived = true; }
                void OnClickEvt(ClickEvent evt) { clickEventReceived = true; clickPropagationStopped = evt.isPropagationStopped; }

                element.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                element.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                element.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                element.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                element.RegisterCallback<ClickEvent>(OnClickEvt, TrickleDown.TrickleDown);

                var imgui = new UnityEngine.Event { type = EventType.MouseDown, mousePosition = worldPos, button = 0, clickCount = currentClickCount };
                using (PointerDownEvent pointerDown = PointerDownEvent.GetPooled(imgui))
                {
                    dispatchRoot.SendEvent(pointerDown);
                }

                imgui = new UnityEngine.Event { type = EventType.MouseUp, mousePosition = worldPos, button = 0, clickCount = currentClickCount };
                using (PointerUpEvent pointerUp = PointerUpEvent.GetPooled(imgui))
                {
                    dispatchRoot.SendEvent(pointerUp);
                }

                element.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                element.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                element.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                element.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                element.UnregisterCallback<ClickEvent>(OnClickEvt, TrickleDown.TrickleDown);

                if (context != null)
                {
                    string pointerStatus = pointerDownReceived && pointerUpReceived
                        ? "PointerDown+Up 均已接收"
                        : pointerDownReceived
                            ? "PointerDown 已接收，PointerUp 未接收"
                            : "PointerDown 未接收（元素未响应指针事件）";
                    string mouseStatus = mouseDownReceived && mouseUpReceived
                        ? "MouseDown+Up 均已接收"
                        : mouseDownReceived
                            ? "MouseDown 已接收，MouseUp 未接收"
                            : "MouseDown 未接收（未生成兼容鼠标事件）";
                    string clickStatus = clickEventReceived
                        ? clickPropagationStopped
                            ? "ClickEvent 已触发（传播已停止，处理器已响应）"
                            : "ClickEvent 已触发（传播继续，无处理器消费）"
                        : "ClickEvent 未触发（Clickable 未响应）";
                    context.Log($"click[{currentClickCount}/{clickCount}]: {pointerStatus}  |  {mouseStatus}  |  {clickStatus}");
                }

                if (!pointerDownReceived && !mouseDownReceived)
                {
                    throw new UnityUIFlowException(
                        ErrorCodes.ActionExecutionFailed,
                        $"click failed: {ActionContext.ElementInfo(element)} did not receive pointer or mouse down.");
                }

                if (!pointerUpReceived && !mouseUpReceived)
                {
                    throw new UnityUIFlowException(
                        ErrorCodes.ActionExecutionFailed,
                        $"click failed: {ActionContext.ElementInfo(element)} did not receive pointer or mouse up.");
                }
            }
        }

        public static void DispatchKeyboardEvent(VisualElement target, EventType eventType, KeyCode keyCode)
        {
            var dispatchRoot = target?.panel?.visualTree ?? target;
            if (dispatchRoot == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, "Keyboard event target is unavailable.");
            }

            var imguiEvent = new UnityEngine.Event
            {
                type = eventType,
                keyCode = keyCode,
                character = ToCharacter(keyCode),
            };

            using (var keyboardEvent = eventType == EventType.KeyDown
                ? (EventBase)KeyDownEvent.GetPooled(imguiEvent)
                : KeyUpEvent.GetPooled(imguiEvent))
            {
                keyboardEvent.target = target;
                dispatchRoot.SendEvent(keyboardEvent);
            }
        }

        public static void DispatchMouseEvent(VisualElement target, EventType eventType, Vector2 mousePosition, Vector2 delta, int button = 0, int clickCount = 1)
        {
            var dispatchRoot = target?.panel?.visualTree ?? target;
            if (dispatchRoot == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, "Mouse event target is unavailable.");
            }

            var imguiEvent = new UnityEngine.Event
            {
                type = eventType,
                mousePosition = mousePosition,
                delta = delta,
                button = button,
                clickCount = clickCount,
            };

            EventBase evt;
            switch (eventType)
            {
                case EventType.MouseDown:
                    evt = MouseDownEvent.GetPooled(imguiEvent);
                    break;
                case EventType.MouseUp:
                    evt = MouseUpEvent.GetPooled(imguiEvent);
                    break;
                case EventType.MouseMove:
                    evt = MouseMoveEvent.GetPooled(imguiEvent);
                    break;
                default:
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Unsupported mouse event type: {eventType}");
            }

            using (evt)
            {
                evt.target = target;
                dispatchRoot.SendEvent(evt);
            }
        }

        public static void DispatchWheelEvent(VisualElement target, Vector2 mousePosition, Vector2 delta)
        {
            var dispatchRoot = target?.panel?.visualTree ?? target;
            if (dispatchRoot == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, "Wheel event target is unavailable.");
            }

            var imguiEvent = new UnityEngine.Event
            {
                type = EventType.ScrollWheel,
                mousePosition = mousePosition,
                delta = delta,
            };

            using (WheelEvent wheelEvent = WheelEvent.GetPooled(imguiEvent))
            {
                wheelEvent.target = target;
                dispatchRoot.SendEvent(wheelEvent);
            }
        }

        private static char ToCharacter(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                return (char)('A' + (keyCode - KeyCode.A));
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                return (char)('0' + (keyCode - KeyCode.Alpha0));
            }

            return '\0';
        }
    }

    internal sealed class ClickAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "click");
            context.Log($"click: dispatch to {ActionContext.ElementInfo(element)} at {element.worldBound.center}");
            ActionHelpers.DispatchClick(element, 1, context);
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class DoubleClickAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "double_click");
            context.Log($"double_click: dispatch to {ActionContext.ElementInfo(element)} at {element.worldBound.center}");
            ActionHelpers.DispatchClick(element, 2, context);
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class TypeTextAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "type_text");
            string value = ActionHelpers.Require(parameters, "type_text", "value");
            context.Log($"type_text: writing {value.Length} chars to {ActionContext.ElementInfo(element)}");

            string current = string.Empty;
            foreach (char ch in value)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                current += ch;
                if (!ActionHelpers.TryAssignFieldValue(element, current))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"type_text target type is not writable: {element.GetType().Name}");
                }

                await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
            }

            context.Log($"type_text: final value \"{value}\"");
        }
    }

    internal sealed class TypeTextFastAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "type_text_fast");
            string value = ActionHelpers.Require(parameters, "type_text_fast", "value");
            context.Log($"type_text_fast: setting {ActionContext.ElementInfo(element)} to \"{value}\"");
            if (!ActionHelpers.TryAssignFieldValue(element, value))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"type_text_fast target type is not writable: {element.GetType().Name}");
            }
        }
    }

    internal sealed class PressKeyAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string key = ActionHelpers.Require(parameters, "press_key", "key");
            if (!Enum.TryParse(key, true, out KeyCode keyCode))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, "press_key parameter 'key' is invalid.");
            }

            VisualElement target = root.focusController?.focusedElement as VisualElement ?? root;
            target.Focus();
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
            context.Log($"press_key: sending {keyCode} to {ActionContext.ElementInfo(target)}");
            ActionHelpers.DispatchKeyboardEvent(target, EventType.KeyDown, keyCode);
            ActionHelpers.DispatchKeyboardEvent(target, EventType.KeyUp, keyCode);
        }
    }

    internal sealed class DragAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string from = ActionHelpers.Require(parameters, "drag", "from");
            string to = ActionHelpers.Require(parameters, "drag", "to");
            int delayMs = parameters.TryGetValue("duration", out string duration)
                ? DurationParser.ParseToMilliseconds(duration, "drag")
                : 100;

            context.Log($"drag: resolve from {from}");
            Vector2 fromPos = await ResolvePositionAsync(from, root, context);
            context.Log($"drag: resolve to {to}");
            Vector2 toPos = await ResolvePositionAsync(to, root, context);
            int frameCount = Math.Max(1, delayMs / 16);
            context.Log($"drag: {fromPos} -> {toPos} in {delayMs}ms across {frameCount} frames");

            ActionHelpers.DispatchMouseEvent(root, EventType.MouseDown, fromPos, Vector2.zero);

            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);

            for (int i = 1; i <= frameCount; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                Vector2 prev = Vector2.Lerp(fromPos, toPos, (float)(i - 1) / frameCount);
                Vector2 pos = Vector2.Lerp(fromPos, toPos, (float)i / frameCount);
                Vector2 delta = pos - prev;
                ActionHelpers.DispatchMouseEvent(root, EventType.MouseMove, pos, delta);

                await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
            }

            ActionHelpers.DispatchMouseEvent(root, EventType.MouseUp, toPos, Vector2.zero);

            context.Log($"drag: completed {from} -> {to}");
            context.SharedBag["lastDrag"] = $"{from}->{to}";
        }

        private static async Task<Vector2> ResolvePositionAsync(string selectorOrCoord, VisualElement root, ActionContext context)
        {
            if (TryParseCoordinate(selectorOrCoord, out Vector2 coord))
            {
                return coord;
            }

            SelectorExpression compiled = new SelectorCompiler().Compile(selectorOrCoord);
            FindResult result = await context.Finder.WaitForElementAsync(
                compiled,
                root,
                new WaitOptions
                {
                    TimeoutMs = context.Options.DefaultTimeoutMs,
                    PollIntervalMs = 16,
                    RequireVisible = true,
                },
                context.CancellationToken);
            return result.Element.worldBound.center;
        }

        private static bool TryParseCoordinate(string value, out Vector2 result)
        {
            result = Vector2.zero;
            string[] parts = value.Split(',');
            if (parts.Length != 2)
            {
                return false;
            }

            if (int.TryParse(parts[0].Trim(), out int x) && int.TryParse(parts[1].Trim(), out int y) && x >= 0 && y >= 0)
            {
                result = new Vector2(x, y);
                return true;
            }

            return false;
        }
    }

    internal sealed class ScrollAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "scroll");
            string delta = ActionHelpers.Require(parameters, "scroll", "delta");
            string[] parts = delta.Split(',');
            if (parts.Length != 2 || !float.TryParse(parts[0], out float dx) || !float.TryParse(parts[1], out float dy))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, "scroll parameter 'delta' is invalid.");
            }

            context.Log($"scroll: {ActionContext.ElementInfo(element)} delta=({dx},{dy})");
            if (element is ScrollView scrollView)
            {
                float nextX = scrollView.scrollOffset.x + dx;
                float nextY = scrollView.scrollOffset.y + dy;
                if (Math.Abs(dx) > 0.01f && scrollView.horizontalScroller != null)
                {
                    scrollView.horizontalScroller.value += dx;
                    nextX = scrollView.horizontalScroller.value;
                }

                if (Math.Abs(dy) > 0.01f && scrollView.verticalScroller != null)
                {
                    scrollView.verticalScroller.value += dy;
                    nextY = scrollView.verticalScroller.value;
                }

                scrollView.scrollOffset = new Vector2(nextX, nextY);
                ActionHelpers.DispatchWheelEvent(scrollView, scrollView.worldBound.center, new Vector2(dx, dy));
                context.Log($"scroll: offset is now {scrollView.scrollOffset}");
                return;
            }

            ActionHelpers.DispatchWheelEvent(element, element.worldBound.center, new Vector2(dx, dy));
        }
    }

    internal sealed class HoverAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "hover");
            Vector2 center = element.worldBound.center;
            context.Log($"hover: {ActionContext.ElementInfo(element)} at {center}");
            element.Focus();
            ActionHelpers.DispatchMouseEvent(element, EventType.MouseMove, center, Vector2.zero, 0, 0);

            if (parameters.TryGetValue("duration", out string durationLiteral))
            {
                int delay = DurationParser.ParseToMilliseconds(durationLiteral, "hover");
                if (delay > 0)
                {
                    context.Log($"hover: wait {delay}ms");
                    await EditorAsyncUtility.DelayAsync(delay, context.CancellationToken);
                }
            }
        }
    }

    internal sealed class WaitAction : IAction
    {
        public Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string duration = ActionHelpers.Require(parameters, "wait", "duration");
            int ms = DurationParser.ParseToMilliseconds(duration, "wait");
            context.Log($"wait: {ms}ms");
            return EditorAsyncUtility.DelayAsync(ms, context.CancellationToken);
        }
    }

    internal sealed class WaitForElementAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string selector = ActionHelpers.Require(parameters, "wait_for_element", "selector");
            int timeout = parameters.TryGetValue("timeout", out string timeoutLiteral)
                ? DurationParser.ParseToMilliseconds(timeoutLiteral, "wait_for_element")
                : context.Options.DefaultTimeoutMs;

            context.Log($"wait_for_element: {selector}, timeout={timeout}ms");
            await context.Finder.WaitForElementAsync(
                new SelectorCompiler().Compile(selector),
                context.Root,
                new WaitOptions
                {
                    TimeoutMs = timeout,
                    PollIntervalMs = 16,
                    RequireVisible = true,
                },
                context.CancellationToken);
            context.Log($"wait_for_element: {selector} is visible");
        }
    }

    internal sealed class AssertVisibleAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string selector = parameters.TryGetValue("selector", out string s) ? s : string.Empty;
            context.Log($"assert_visible: {selector}");
            await new WaitForElementAction().ExecuteAsync(root, context, parameters);
            context.Log("assert_visible: passed");
        }
    }

    internal sealed class AssertNotVisibleAction : IAction
    {
        public Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            string selector = ActionHelpers.Require(parameters, "assert_not_visible", "selector");
            int timeout = parameters.TryGetValue("timeout", out string timeoutLiteral)
                ? DurationParser.ParseToMilliseconds(timeoutLiteral, "assert_not_visible")
                : context.Options.DefaultTimeoutMs;

            context.Log($"assert_not_visible: {selector}, timeout={timeout}ms");
            return AssertAsync(selector, timeout);

            async Task AssertAsync(string currentSelector, int currentTimeout)
            {
                DateTimeOffset startedAt = DateTimeOffset.UtcNow;
                SelectorExpression compiled = new SelectorCompiler().Compile(currentSelector);
                while (true)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    if (!context.Finder.Exists(compiled, context.Root, true))
                    {
                        context.Log("assert_not_visible: passed");
                        return;
                    }

                    if (UnityUIFlowUtility.DurationMs(startedAt, DateTimeOffset.UtcNow) >= currentTimeout)
                    {
                        throw new UnityUIFlowException(ErrorCodes.ElementNotVisible, $"Element is still visible: {currentSelector}");
                    }

                    await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
                }
            }
        }
    }

    internal sealed class AssertTextAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "assert_text");
            string expected = ActionHelpers.Require(parameters, "assert_text", "expected");
            string actual = ActionHelpers.GetText(element);
            context.Log($"assert_text: expected={expected}, actual={actual}");
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, $"assert_text failed: expected '{expected}', actual '{actual}'");
            }

            context.Log("assert_text: passed");
        }
    }

    internal sealed class AssertTextContainsAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "assert_text_contains");
            string expected = ActionHelpers.Require(parameters, "assert_text_contains", "expected");
            string actual = ActionHelpers.GetText(element);
            context.Log($"assert_text_contains: expected token={expected}, actual={actual}");
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, $"assert_text_contains failed: missing '{expected}'");
            }

            context.Log("assert_text_contains: passed");
        }
    }

    internal sealed class AssertPropertyAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "assert_property");
            string propertyName = ActionHelpers.Require(parameters, "assert_property", "property");
            string expected = ActionHelpers.Require(parameters, "assert_property", "expected");

            PropertyInfo property = element.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"assert_property property is invalid: {propertyName}");
            }

            object actual = property.GetValue(element);
            context.Log($"assert_property: {propertyName} expected={expected}, actual={actual}");
            if (!string.Equals(actual?.ToString(), expected, StringComparison.Ordinal))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionExecutionFailed, $"assert_property failed: expected '{expected}', actual '{actual}'");
            }

            context.Log("assert_property: passed");
        }
    }

    internal sealed class ScreenshotAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            if (context.ScreenshotManager == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ScreenshotSaveFailed, "Screenshot manager is not initialized.");
            }

            if (!parameters.TryGetValue("tag", out string tag) || string.IsNullOrWhiteSpace(tag))
            {
                tag = context.CurrentStepId;
            }

            context.Log($"screenshot: tag={tag}, case={context.CurrentCaseName}, step={context.CurrentStepIndex}");
            string path = await context.ScreenshotManager.CaptureAsync(context.CurrentCaseName, context.CurrentStepIndex, tag, context.CancellationToken);
            context.Log($"screenshot: saved {path}");
            context.AddAttachment(path);
        }
    }
}
