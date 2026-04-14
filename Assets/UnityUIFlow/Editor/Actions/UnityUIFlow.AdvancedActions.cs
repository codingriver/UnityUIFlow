using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    internal static class AdvancedActionHelpers
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        public static bool TryAssignFieldValue(VisualElement element, string value)
        {
            switch (element)
            {
                case Toggle toggle when TryParseBool(value, out bool boolValue):
                    toggle.value = boolValue;
                    return true;
                case Foldout foldout when TryParseBool(value, out bool expanded):
                    foldout.value = expanded;
                    return true;
                case Slider slider when TryParseFloat(value, out float sliderValue):
                    slider.value = Mathf.Clamp(sliderValue, slider.lowValue, slider.highValue);
                    return true;
                case SliderInt sliderInt when TryParseInt(value, out int sliderIntValue):
                    sliderInt.value = Mathf.Clamp(sliderIntValue, sliderInt.lowValue, sliderInt.highValue);
                    return true;
                case MinMaxSlider minMaxSlider when TryParseFloatPair(value, out float minValue, out float maxValue):
                    minValue = Mathf.Clamp(minValue, minMaxSlider.lowLimit, minMaxSlider.highLimit);
                    maxValue = Mathf.Clamp(maxValue, minMaxSlider.lowLimit, minMaxSlider.highLimit);
                    if (maxValue < minValue)
                    {
                        maxValue = minValue;
                    }

                    minMaxSlider.value = new Vector2(minValue, maxValue);
                    return true;
                case DropdownField dropdown:
                    if (TryResolveChoice(dropdown.choices, value, null, out int _, out object selectedChoice))
                    {
                        dropdown.value = selectedChoice?.ToString() ?? string.Empty;
                        return true;
                    }

                    return false;
                case EnumField enumField when enumField.value != null && TryConvertStringValue(value, enumField.value.GetType(), out object enumValue):
                    enumField.value = (Enum)enumValue;
                    return true;
                case EnumFlagsField enumFlagsField when enumFlagsField.value != null && TryConvertStringValue(value, enumFlagsField.value.GetType(), out object enumFlagsValue):
                    enumFlagsField.value = (Enum)enumFlagsValue;
                    return true;
                case MaskField maskField when TryResolveMaskValue(maskField.choices, value, null, null, out int maskValue):
                    maskField.value = maskValue;
                    return true;
                case LayerMaskField layerMaskField when TryResolveMaskValue(layerMaskField.choices, value, null, null, out int layerMaskValue):
                    layerMaskField.value = layerMaskValue;
                    return true;
                case RadioButtonGroup radioButtonGroup when TryResolveIndex(value, null, out int radioIndex):
                    radioButtonGroup.value = radioIndex;
                    return true;
            }

            PropertyInfo valueProperty = element.GetType().GetProperty("value", PublicInstance);
            if (valueProperty == null || !valueProperty.CanWrite)
            {
                return false;
            }

            if (!TryConvertStringValue(value, valueProperty.PropertyType, out object converted))
            {
                return false;
            }

            valueProperty.SetValue(element, converted);
            return true;
        }

        public static bool TryReadValue(VisualElement element, out object value, out Type valueType)
        {
            value = null;
            valueType = null;

            PropertyInfo valueProperty = element?.GetType().GetProperty("value", PublicInstance);
            if (valueProperty == null || !valueProperty.CanRead)
            {
                return false;
            }

            value = valueProperty.GetValue(element);
            valueType = valueProperty.PropertyType;
            return true;
        }

        public static bool TryReadValueAsString(VisualElement element, out string value)
        {
            value = null;
            if (!TryReadValue(element, out object rawValue, out Type valueType))
            {
                return false;
            }

            value = FormatValue(rawValue, valueType);
            return true;
        }

        public static bool ValuesEqual(object actual, object expected, Type valueType)
        {
            Type underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            if (actual == null || expected == null)
            {
                return Equals(actual, expected);
            }

            if (underlyingType == typeof(float))
            {
                return Mathf.Approximately((float)actual, (float)expected);
            }

            if (underlyingType == typeof(double))
            {
                return Math.Abs((double)actual - (double)expected) <= 0.000001d;
            }

            if (underlyingType == typeof(Vector2))
            {
                return Vector2.Distance((Vector2)actual, (Vector2)expected) <= 0.0001f;
            }

            if (underlyingType == typeof(Vector3))
            {
                return Vector3.Distance((Vector3)actual, (Vector3)expected) <= 0.0001f;
            }

            if (underlyingType == typeof(Vector4))
            {
                Vector4 left = (Vector4)actual;
                Vector4 right = (Vector4)expected;
                return Mathf.Abs(left.x - right.x) <= 0.0001f
                    && Mathf.Abs(left.y - right.y) <= 0.0001f
                    && Mathf.Abs(left.z - right.z) <= 0.0001f
                    && Mathf.Abs(left.w - right.w) <= 0.0001f;
            }

            if (underlyingType == typeof(Color))
            {
                Color left = (Color)actual;
                Color right = (Color)expected;
                return Mathf.Abs(left.r - right.r) <= 0.0001f
                    && Mathf.Abs(left.g - right.g) <= 0.0001f
                    && Mathf.Abs(left.b - right.b) <= 0.0001f
                    && Mathf.Abs(left.a - right.a) <= 0.0001f;
            }

            if (underlyingType == typeof(AnimationCurve))
            {
                return CurvesEqual((AnimationCurve)actual, (AnimationCurve)expected);
            }

            if (underlyingType == typeof(Gradient))
            {
                return GradientsEqual((Gradient)actual, (Gradient)expected);
            }

            return Equals(actual, expected);
        }

        public static void SelectOptionOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            parameters.TryGetValue("value", out string valueLiteral);
            parameters.TryGetValue("index", out string indexLiteral);
            parameters.TryGetValue("indices", out string indicesLiteral);

            if (string.IsNullOrWhiteSpace(valueLiteral) && string.IsNullOrWhiteSpace(indexLiteral) && string.IsNullOrWhiteSpace(indicesLiteral))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires value, index, or indices.");
            }

            if (element is DropdownField dropdown)
            {
                ApplyChoiceSelection(element, dropdown.choices, dropdown, valueLiteral, indexLiteral, actionName);
                return;
            }

            if (element is EnumField enumField && enumField.value != null)
            {
                if (TryResolveEnumSelection(enumField.value.GetType(), valueLiteral, indexLiteral, out Enum enumValue))
                {
                    enumField.value = enumValue;
                    return;
                }

                throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} option {valueLiteral ?? indexLiteral} was not found for {element.GetType().Name}.");
            }

            if (element is EnumFlagsField enumFlagsField && enumFlagsField.value != null)
            {
                if (TryResolveEnumFlagsSelection(enumFlagsField.value.GetType(), valueLiteral, indexLiteral, indicesLiteral, out Enum enumFlagsValue))
                {
                    enumFlagsField.value = enumFlagsValue;
                    return;
                }

                throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} option {valueLiteral ?? indicesLiteral ?? indexLiteral} was not found for {element.GetType().Name}.");
            }

            if (element is MaskField maskField)
            {
                if (TryResolveMaskValue(maskField.choices, valueLiteral, indexLiteral, indicesLiteral, out int maskValue))
                {
                    maskField.value = maskValue;
                    return;
                }

                throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} option {valueLiteral ?? indicesLiteral ?? indexLiteral} was not found for {element.GetType().Name}.");
            }

            if (element is LayerMaskField layerMaskField)
            {
                if (TryResolveMaskValue(layerMaskField.choices, valueLiteral, indexLiteral, indicesLiteral, out int layerMaskValue))
                {
                    layerMaskField.value = layerMaskValue;
                    return;
                }

                throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} option {valueLiteral ?? indicesLiteral ?? indexLiteral} was not found for {element.GetType().Name}.");
            }

            if (element is RadioButtonGroup radioButtonGroup)
            {
                if (!TryResolveIndex(valueLiteral, indexLiteral, out int radioIndex))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} requires a numeric index for {element.GetType().Name}.");
                }

                radioButtonGroup.value = radioIndex;
                return;
            }

            PropertyInfo choicesProperty = element.GetType().GetProperty("choices", PublicInstance);
            PropertyInfo valueProperty = element.GetType().GetProperty("value", PublicInstance);
            PropertyInfo indexProperty = element.GetType().GetProperty("index", PublicInstance);

            if (choicesProperty?.GetValue(element) is IList choices)
            {
                ApplyChoiceSelection(element, choices, valueProperty, indexProperty, valueLiteral, indexLiteral, actionName);
                return;
            }

            if (valueProperty != null && valueProperty.CanWrite)
            {
                if (TryResolveValueForSelection(valueProperty.PropertyType, valueLiteral, indexLiteral, out object converted))
                {
                    valueProperty.SetValue(element, converted);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(indexLiteral))
                {
                    throw new UnityUIFlowException(
                        ErrorCodes.ActionIndexOutOfRange,
                        $"Action {actionName} index {indexLiteral} is out of range for {element.GetType().Name}.");
                }

                throw new UnityUIFlowException(
                    ErrorCodes.ActionOptionNotFound,
                    $"Action {actionName} option {valueLiteral} was not found for {element.GetType().Name}.");
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a selectable control: {element.GetType().Name}");
        }

        public static void SelectListItemOrThrow(VisualElement element, int index, string actionName)
        {
            IList itemsSource = GetItemsSource(element);
            if (itemsSource == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a list control: {element.GetType().Name}");
            }

            ValidateIndex(index, itemsSource.Count, actionName);
            if (TryApplySingleSelection(element, index))
            {
                return;
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target does not expose selection APIs: {element.GetType().Name}");
        }

        public static void SelectListItemsOrThrow(VisualElement element, IList<int> indices, string actionName)
        {
            IList itemsSource = GetItemsSource(element);
            if (itemsSource == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a list control: {element.GetType().Name}");
            }

            if (indices == null || indices.Count == 0)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires indices.");
            }

            var distinctIndices = new List<int>();
            var seen = new HashSet<int>();
            foreach (int index in indices)
            {
                ValidateIndex(index, itemsSource.Count, actionName);
                if (seen.Add(index))
                {
                    distinctIndices.Add(index);
                }
            }

            if (TryApplyMultiSelection(element, distinctIndices))
            {
                return;
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target does not expose multi-selection APIs: {element.GetType().Name}");
        }

        public static void ReorderListItemOrThrow(VisualElement element, int fromIndex, int toIndex, string actionName)
        {
            IList itemsSource = GetItemsSource(element);
            if (itemsSource == null)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a list control: {element.GetType().Name}");
            }

            ValidateIndex(fromIndex, itemsSource.Count, actionName);
            ValidateIndex(toIndex, itemsSource.Count, actionName);

            if (fromIndex == toIndex)
            {
                return;
            }

            object item = itemsSource[fromIndex];
            itemsSource.RemoveAt(fromIndex);
            itemsSource.Insert(Math.Min(toIndex, itemsSource.Count), item);

            RefreshCollectionView(element);
            TryApplySingleSelection(element, Math.Min(toIndex, itemsSource.Count - 1));
        }

        public static void SelectTreeItemOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            parameters.TryGetValue("id", out string idLiteral);
            parameters.TryGetValue("index", out string indexLiteral);

            if (string.IsNullOrWhiteSpace(idLiteral) && string.IsNullOrWhiteSpace(indexLiteral))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires id or index.");
            }

            if (!string.IsNullOrWhiteSpace(idLiteral))
            {
                if (!TryParseInt(idLiteral, out int itemId))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} id is invalid: {idLiteral}");
                }

                if (TryApplySelectionById(element, itemId))
                {
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(indexLiteral))
            {
                if (!TryParseInt(indexLiteral, out int index))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} index is invalid: {indexLiteral}");
                }

                SelectListItemOrThrow(element, index, actionName);
                return;
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a tree control: {element.GetType().Name}");
        }

        public static void ToggleFoldoutOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            if (!(element is Foldout foldout))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a Foldout: {element.GetType().Name}");
            }

            if (parameters.TryGetValue("expand", out string expandLiteral) && !string.IsNullOrWhiteSpace(expandLiteral))
            {
                if (!TryParseBool(expandLiteral, out bool expand))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} expand is invalid: {expandLiteral}");
                }

                foldout.value = expand;
                return;
            }

            foldout.value = !foldout.value;
        }

        public static void SetSliderOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            if (element is Slider slider)
            {
                string valueLiteral = ActionHelpers.Require(parameters, actionName, "value");
                if (!TryParseFloat(valueLiteral, out float value))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} value is invalid: {valueLiteral}");
                }

                slider.value = Mathf.Clamp(value, slider.lowValue, slider.highValue);
                return;
            }

            if (element is SliderInt sliderInt)
            {
                string valueLiteral = ActionHelpers.Require(parameters, actionName, "value");
                if (!TryParseInt(valueLiteral, out int value))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} value is invalid: {valueLiteral}");
                }

                sliderInt.value = Mathf.Clamp(value, sliderInt.lowValue, sliderInt.highValue);
                return;
            }

            if (element is MinMaxSlider minMaxSlider)
            {
                float minValue;
                float maxValue;

                if (parameters.TryGetValue("value", out string valueLiteral) && !string.IsNullOrWhiteSpace(valueLiteral))
                {
                    if (!TryParseFloatPair(valueLiteral, out minValue, out maxValue))
                    {
                        throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} value is invalid: {valueLiteral}");
                    }
                }
                else
                {
                    string minLiteral = ActionHelpers.Require(parameters, actionName, "min_value");
                    string maxLiteral = ActionHelpers.Require(parameters, actionName, "max_value");
                    if (!TryParseFloat(minLiteral, out minValue) || !TryParseFloat(maxLiteral, out maxValue))
                    {
                        throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} min/max values are invalid.");
                    }
                }

                minValue = Mathf.Clamp(minValue, minMaxSlider.lowLimit, minMaxSlider.highLimit);
                maxValue = Mathf.Clamp(maxValue, minMaxSlider.lowLimit, minMaxSlider.highLimit);
                if (maxValue < minValue)
                {
                    maxValue = minValue;
                }

                minMaxSlider.value = new Vector2(minValue, maxValue);
                return;
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a slider control: {element.GetType().Name}");
        }

        public static void SelectTabOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName, ActionContext context)
        {
            parameters.TryGetValue("label", out string labelLiteral);
            parameters.TryGetValue("index", out string indexLiteral);

            if (string.IsNullOrWhiteSpace(labelLiteral) && string.IsNullOrWhiteSpace(indexLiteral))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires label or index.");
            }

            if (!string.IsNullOrWhiteSpace(indexLiteral) && TryParseInt(indexLiteral, out int index))
            {
                if (TrySetIntProperty(element, "selectedTabIndex", index) || TrySetIntProperty(element, "selectedIndex", index))
                {
                    return;
                }

                if (TryClickTabByIndex(element, index, context))
                {
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(labelLiteral) && TryClickTabByLabel(element, labelLiteral, context))
            {
                return;
            }

            throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a supported TabView: {element.GetType().Name}");
        }

        public static void SetBoundValueOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            string propertyPath = ResolveBoundPropertyPath(element, parameters, actionName, requireExplicitProperty: false);
            string value = ActionHelpers.Require(parameters, actionName, "value");
            VisualElement targetField = FindBoundTargetOrThrow(element, propertyPath, actionName);
            if (!ActionHelpers.TryAssignFieldValue(targetField, value))
            {
                throw new UnityUIFlowException(
                    ErrorCodes.ActionTargetTypeInvalid,
                    $"Action {actionName} target property '{propertyPath}' is not writable: {targetField.GetType().Name}");
            }
        }

        public static void AssertBoundValueOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            string propertyPath = ResolveBoundPropertyPath(element, parameters, actionName, requireExplicitProperty: false);
            string expected = ActionHelpers.Require(parameters, actionName, "expected");
            VisualElement targetField = FindBoundTargetOrThrow(element, propertyPath, actionName);
            if (TryReadValue(targetField, out object actualValue, out Type valueType)
                && TryConvertStringValue(expected, valueType, out object expectedValue))
            {
                if (!ValuesEqual(actualValue, expectedValue, valueType))
                {
                    throw new UnityUIFlowException(
                        ErrorCodes.ActionExecutionFailed,
                        $"Action {actionName} failed for bound property '{propertyPath}': expected '{expected}', actual '{FormatValue(actualValue, valueType)}'");
                }

                return;
            }

            string actual = ActionHelpers.GetValueText(targetField);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new UnityUIFlowException(
                    ErrorCodes.ActionExecutionFailed,
                    $"Action {actionName} failed for bound property '{propertyPath}': expected '{expected}', actual '{actual}'");
            }
        }

        public static void NavigateBreadcrumbOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName, ActionContext context)
        {
            parameters.TryGetValue("label", out string labelLiteral);
            parameters.TryGetValue("index", out string indexLiteral);

            List<Button> buttons = element.Query<Button>().ToList();
            if (buttons.Count == 0)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target does not contain breadcrumb buttons: {element.GetType().Name}");
            }

            Button target = null;
            if (!string.IsNullOrWhiteSpace(labelLiteral))
            {
                target = buttons.Find(button => string.Equals(button.text, labelLiteral, StringComparison.Ordinal));
                if (target == null)
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} breadcrumb label was not found: {labelLiteral}");
                }
            }
            else
            {
                string indexValue = ActionHelpers.Require(parameters, actionName, "index");
                if (!TryParseInt(indexValue, out int index) || index < 0 || index >= buttons.Count)
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionIndexOutOfRange, $"Action {actionName} breadcrumb index is out of range: {indexValue}");
                }

                target = buttons[index];
            }

            if (context?.SimulationSession?.PointerDriver != null)
            {
                context.SimulationSession.PointerDriver.Click(target, 1, MouseButton.LeftMouse, EventModifiers.None, context);
            }
            else
            {
                ActionHelpers.DispatchClick(target, 1, MouseButton.LeftMouse, EventModifiers.None, context);
            }
        }

        public static void SetSplitViewSizeOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            if (!(element is TwoPaneSplitView splitView))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a TwoPaneSplitView: {element.GetType().Name}");
            }

            string sizeLiteral = ActionHelpers.Require(parameters, actionName, "size");
            if (!TryParseFloat(sizeLiteral, out float size) || size <= 0f)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} size is invalid: {sizeLiteral}");
            }

            int paneIndex = 0;
            if (parameters.TryGetValue("pane", out string paneLiteral) && !string.IsNullOrWhiteSpace(paneLiteral))
            {
                if (!TryParseInt(paneLiteral, out paneIndex) || paneIndex < 0 || paneIndex > 1)
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} pane is invalid: {paneLiteral}");
                }
            }

            TrySetFloatProperty(splitView, "fixedPaneInitialDimension", size);
            TrySetIntProperty(splitView, "fixedPaneIndex", paneIndex);

            if (splitView.childCount > paneIndex)
            {
                VisualElement pane = splitView[paneIndex];
                if (splitView.orientation == TwoPaneSplitViewOrientation.Horizontal)
                {
                    pane.style.width = size;
                    pane.style.minWidth = size;
                }
                else
                {
                    pane.style.height = size;
                    pane.style.minHeight = size;
                }
            }
        }

        public static void PageScrollerOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            if (!(element is Scroller scroller))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target is not a Scroller: {element.GetType().Name}");
            }

            int pages = 1;
            if (parameters.TryGetValue("pages", out string pagesLiteral) && !string.IsNullOrWhiteSpace(pagesLiteral))
            {
                if (!TryParseInt(pagesLiteral, out pages) || pages <= 0)
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} pages is invalid: {pagesLiteral}");
                }
            }

            string directionLiteral = ActionHelpers.Require(parameters, actionName, "direction").Trim().ToLowerInvariant();
            float sign;
            switch (directionLiteral)
            {
                case "up":
                case "left":
                case "decrease":
                case "previous":
                    sign = -1f;
                    break;
                case "down":
                case "right":
                case "increase":
                case "next":
                    sign = 1f;
                    break;
                default:
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action {actionName} direction is invalid: {directionLiteral}");
            }

            float pageSize = ResolveScrollerPageSize(scroller, parameters);
            float next = Mathf.Clamp(scroller.value + (pageSize * pages * sign), scroller.lowValue, scroller.highValue);
            scroller.value = next;
        }

        private static float ResolveScrollerPageSize(Scroller scroller, Dictionary<string, string> parameters)
        {
            if (parameters.TryGetValue("page_size", out string literal) && !string.IsNullOrWhiteSpace(literal) && TryParseFloat(literal, out float explicitPageSize) && explicitPageSize > 0f)
            {
                return explicitPageSize;
            }

            PropertyInfo pageSizeProperty = scroller.GetType().GetProperty("pageSize", PublicInstance);
            if (pageSizeProperty?.CanRead == true && pageSizeProperty.GetValue(scroller) is float reflectedPageSize && reflectedPageSize > 0f)
            {
                return reflectedPageSize;
            }

            return Mathf.Max(1f, (scroller.highValue - scroller.lowValue) * 0.1f);
        }

        private static string ResolveBoundPropertyPath(VisualElement element, Dictionary<string, string> parameters, string actionName, bool requireExplicitProperty)
        {
            if (parameters.TryGetValue("property", out string propertyLiteral) && !string.IsNullOrWhiteSpace(propertyLiteral))
            {
                return propertyLiteral.Trim();
            }

            IBindable bindable = element as IBindable;
            if (bindable != null && !string.IsNullOrWhiteSpace(bindable.bindingPath))
            {
                return bindable.bindingPath;
            }

            if (requireExplicitProperty)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires property.");
            }

            throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing, $"Action {actionName} requires property or a directly bound target.");
        }

        private static VisualElement FindBoundTargetOrThrow(VisualElement root, string propertyPath, string actionName)
        {
            List<VisualElement> candidates = root.Query<VisualElement>().ToList();
            if (candidates.Count == 0)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target does not contain bound child controls.");
            }

            foreach (VisualElement candidate in candidates)
            {
                IBindable bindable = candidate as IBindable;
                if (bindable == null || string.IsNullOrWhiteSpace(bindable.bindingPath))
                {
                    continue;
                }

                if (!string.Equals(bindable.bindingPath, propertyPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (candidate.GetType().GetProperty("value", PublicInstance)?.CanWrite == true)
                {
                    return candidate;
                }
            }

            throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound, $"Action {actionName} could not find bound property '{propertyPath}'.");
        }

        private static void ApplyChoiceSelection(VisualElement element, IList choices, DropdownField dropdown, string valueLiteral, string indexLiteral, string actionName)
        {
            if (!TryResolveChoice(choices, valueLiteral, indexLiteral, out int _, out object selectedChoice))
            {
                ThrowChoiceSelectionError(element, choices?.Count ?? 0, valueLiteral, indexLiteral, actionName);
            }

            dropdown.value = selectedChoice?.ToString() ?? string.Empty;
        }

        private static void ApplyChoiceSelection(VisualElement element, IList choices, PropertyInfo valueProperty, PropertyInfo indexProperty, string valueLiteral, string indexLiteral, string actionName)
        {
            if (!TryResolveChoice(choices, valueLiteral, indexLiteral, out int selectedIndex, out object selectedChoice))
            {
                ThrowChoiceSelectionError(element, choices?.Count ?? 0, valueLiteral, indexLiteral, actionName);
            }

            if (indexProperty != null && indexProperty.CanWrite)
            {
                indexProperty.SetValue(element, selectedIndex);
                return;
            }

            if (valueProperty == null || !valueProperty.CanWrite)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} target does not expose writable choice state: {element.GetType().Name}");
            }

            object valueToAssign = selectedChoice;
            Type targetType = valueProperty.PropertyType;
            if (valueToAssign == null)
            {
                valueToAssign = targetType == typeof(string) ? string.Empty : null;
            }
            else if (!targetType.IsInstanceOfType(valueToAssign))
            {
                if (!TryConvertStringValue(valueToAssign.ToString(), targetType, out valueToAssign))
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid, $"Action {actionName} choice cannot be assigned to {targetType.Name}");
                }
            }

            valueProperty.SetValue(element, valueToAssign);
        }

        private static void ThrowChoiceSelectionError(VisualElement element, int choiceCount, string valueLiteral, string indexLiteral, string actionName)
        {
            if (!string.IsNullOrWhiteSpace(indexLiteral))
            {
                throw new UnityUIFlowException(
                    ErrorCodes.ActionIndexOutOfRange,
                    $"Action {actionName} index {indexLiteral} is out of range for {element.GetType().Name}; valid range is [0, {Math.Max(choiceCount - 1, 0)}].");
            }

            throw new UnityUIFlowException(
                ErrorCodes.ActionOptionNotFound,
                $"Action {actionName} option {valueLiteral} was not found for {element.GetType().Name}.");
        }

        private static bool TryResolveChoice(IList choices, string valueLiteral, string indexLiteral, out int selectedIndex, out object selectedChoice)
        {
            selectedIndex = -1;
            selectedChoice = null;

            if (choices == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(valueLiteral))
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    if (string.Equals(choices[i]?.ToString(), valueLiteral, StringComparison.Ordinal))
                    {
                        selectedIndex = i;
                        selectedChoice = choices[i];
                        return true;
                    }
                }

                return false;
            }

            if (!TryParseInt(indexLiteral, out int parsedIndex) || parsedIndex < 0 || parsedIndex >= choices.Count)
            {
                return false;
            }

            selectedIndex = parsedIndex;
            selectedChoice = choices[parsedIndex];
            return true;
        }

        private static bool TryResolveEnumSelection(Type enumType, string valueLiteral, string indexLiteral, out Enum value)
        {
            value = null;
            if (!string.IsNullOrWhiteSpace(valueLiteral))
            {
                if (TryConvertStringValue(valueLiteral, enumType, out object converted))
                {
                    value = (Enum)converted;
                    return true;
                }

                return false;
            }

            Array values = Enum.GetValues(enumType);
            if (!TryParseInt(indexLiteral, out int index) || index < 0 || index >= values.Length)
            {
                return false;
            }

            value = (Enum)values.GetValue(index);
            return true;
        }

        private static bool TryResolveEnumFlagsSelection(Type enumType, string valueLiteral, string indexLiteral, string indicesLiteral, out Enum value)
        {
            value = null;
            if (!string.IsNullOrWhiteSpace(valueLiteral))
            {
                if (TryConvertStringValue(valueLiteral, enumType, out object converted))
                {
                    value = (Enum)converted;
                    return true;
                }

                return false;
            }

            Array enumValues = Enum.GetValues(enumType);
            if (!string.IsNullOrWhiteSpace(indicesLiteral))
            {
                if (!TryParseIndexList(indicesLiteral, out List<int> indices))
                {
                    return false;
                }

                long combined = 0L;
                foreach (int index in indices)
                {
                    if (index < 0 || index >= enumValues.Length)
                    {
                        return false;
                    }

                    combined |= Convert.ToInt64(enumValues.GetValue(index), InvariantCulture);
                }

                value = (Enum)Enum.ToObject(enumType, combined);
                return true;
            }

            if (!TryParseInt(indexLiteral, out int singleIndex) || singleIndex < 0 || singleIndex >= enumValues.Length)
            {
                return false;
            }

            value = (Enum)enumValues.GetValue(singleIndex);
            return true;
        }

        private static bool TryResolveMaskValue(IList choices, string valueLiteral, string indexLiteral, string indicesLiteral, out int value)
        {
            value = 0;

            if (!string.IsNullOrWhiteSpace(valueLiteral) && TryParseInt(valueLiteral, out int rawMask))
            {
                value = rawMask;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(indicesLiteral))
            {
                if (!TryParseIndexList(indicesLiteral, out List<int> indices))
                {
                    return false;
                }

                int mask = 0;
                foreach (int index in indices)
                {
                    if (choices == null || index < 0 || index >= choices.Count)
                    {
                        return false;
                    }

                    mask |= 1 << index;
                }

                value = mask;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(indexLiteral))
            {
                if (choices == null || !TryParseInt(indexLiteral, out int index) || index < 0 || index >= choices.Count)
                {
                    return false;
                }

                value = 1 << index;
                return true;
            }

            if (string.IsNullOrWhiteSpace(valueLiteral) || choices == null)
            {
                return false;
            }

            int combinedMask = 0;
            string[] names = valueLiteral.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawName in names)
            {
                string name = rawName.Trim();
                bool matched = false;
                for (int i = 0; i < choices.Count; i++)
                {
                    if (string.Equals(choices[i]?.ToString(), name, StringComparison.Ordinal))
                    {
                        combinedMask |= 1 << i;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return false;
                }
            }

            value = combinedMask;
            return true;
        }

        private static bool TryResolveValueForSelection(Type targetType, string valueLiteral, string indexLiteral, out object converted)
        {
            converted = null;

            if (!string.IsNullOrWhiteSpace(valueLiteral))
            {
                return TryConvertStringValue(valueLiteral, targetType, out converted);
            }

            if (targetType.IsEnum)
            {
                Array values = Enum.GetValues(targetType);
                if (!TryParseInt(indexLiteral, out int index) || index < 0 || index >= values.Length)
                {
                    return false;
                }

                converted = values.GetValue(index);
                return true;
            }

            return TryConvertStringValue(indexLiteral, targetType, out converted);
        }

        private static IList GetItemsSource(object control)
        {
            return control?.GetType().GetProperty("itemsSource", PublicInstance)?.GetValue(control) as IList;
        }

        private static void ValidateIndex(int index, int count, string actionName)
        {
            if (index < 0 || index >= count)
            {
                throw new UnityUIFlowException(
                    ErrorCodes.ActionIndexOutOfRange,
                    $"Action {actionName} index {index} is out of range; valid range is [0, {Math.Max(count - 1, 0)}].");
            }
        }

        private static bool TryApplySingleSelection(object control, int index)
        {
            if (TrySetIntProperty(control, "selectedIndex", index))
            {
                return true;
            }

            foreach (string methodName in new[] { "SetSelection", "SetSelectionWithoutNotify" })
            {
                MethodInfo[] methods = control.GetType().GetMethods(PublicInstance);
                foreach (MethodInfo method in methods)
                {
                    if (!string.Equals(method.Name, methodName, StringComparison.Ordinal) || method.GetParameters().Length != 1)
                    {
                        continue;
                    }

                    if (TryBuildSelectionArgument(method.GetParameters()[0].ParameterType, index, out object argument))
                    {
                        method.Invoke(control, new[] { argument });
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryApplyMultiSelection(object control, IList<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                return false;
            }

            if (TrySetProperty(control, "selectedIndices", new List<int>(indices)))
            {
                return true;
            }

            foreach (string methodName in new[] { "SetSelection", "SetSelectionWithoutNotify" })
            {
                MethodInfo[] methods = control.GetType().GetMethods(PublicInstance);
                foreach (MethodInfo method in methods)
                {
                    if (!string.Equals(method.Name, methodName, StringComparison.Ordinal) || method.GetParameters().Length != 1)
                    {
                        continue;
                    }

                    if (TryBuildSelectionArgument(method.GetParameters()[0].ParameterType, indices, out object argument))
                    {
                        method.Invoke(control, new[] { argument });
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryApplySelectionById(object control, int itemId)
        {
            foreach (string propertyName in new[] { "selectedId", "selectedItemId" })
            {
                if (TrySetIntProperty(control, propertyName, itemId))
                {
                    return true;
                }
            }

            foreach (string methodName in new[] { "SetSelectionById", "SetSelectionByIds", "SetSelectionInternal" })
            {
                MethodInfo[] methods = control.GetType().GetMethods(PublicInstance);
                foreach (MethodInfo method in methods)
                {
                    if (!string.Equals(method.Name, methodName, StringComparison.Ordinal) || method.GetParameters().Length != 1)
                    {
                        continue;
                    }

                    if (TryBuildSelectionArgument(method.GetParameters()[0].ParameterType, itemId, out object argument))
                    {
                        method.Invoke(control, new[] { argument });
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryBuildSelectionArgument(Type parameterType, int index, out object argument)
        {
            argument = null;

            if (parameterType == typeof(int))
            {
                argument = index;
                return true;
            }

            if (parameterType == typeof(int[]))
            {
                argument = new[] { index };
                return true;
            }

            if (parameterType.IsAssignableFrom(typeof(List<int>)))
            {
                argument = new List<int> { index };
                return true;
            }

            if (parameterType.IsInterface && parameterType.IsGenericType && parameterType.GetGenericArguments().Length == 1 && parameterType.GetGenericArguments()[0] == typeof(int))
            {
                argument = new List<int> { index };
                return true;
            }

            return false;
        }

        private static bool TryBuildSelectionArgument(Type parameterType, IList<int> indices, out object argument)
        {
            argument = null;

            if (parameterType == typeof(int))
            {
                argument = indices[0];
                return true;
            }

            if (parameterType == typeof(int[]))
            {
                int[] values = new int[indices.Count];
                for (int i = 0; i < indices.Count; i++)
                {
                    values[i] = indices[i];
                }

                argument = values;
                return true;
            }

            if (parameterType.IsAssignableFrom(typeof(List<int>)))
            {
                argument = new List<int>(indices);
                return true;
            }

            if (parameterType.IsInterface
                && parameterType.IsGenericType
                && parameterType.GetGenericArguments().Length == 1
                && parameterType.GetGenericArguments()[0] == typeof(int))
            {
                argument = new List<int>(indices);
                return true;
            }

            return false;
        }

        private static bool TrySetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target?.GetType().GetProperty(propertyName, PublicInstance);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            try
            {
                if (value is List<int> values && property.PropertyType == typeof(int[]))
                {
                    int[] array = new int[values.Count];
                    values.CopyTo(array, 0);
                    property.SetValue(target, array);
                    return true;
                }

                if (value != null && !property.PropertyType.IsInstanceOfType(value))
                {
                    return false;
                }

                property.SetValue(target, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RefreshCollectionView(object control)
        {
            foreach (string methodName in new[] { "RefreshItems", "Refresh", "Rebuild" })
            {
                MethodInfo method = control?.GetType().GetMethod(methodName, PublicInstance, null, Type.EmptyTypes, null);
                if (method == null)
                {
                    continue;
                }

                method.Invoke(control, null);
                return;
            }
        }

        private static bool TrySetIntProperty(object target, string propertyName, int value)
        {
            PropertyInfo property = target?.GetType().GetProperty(propertyName, PublicInstance);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(target, value);
            return true;
        }

        private static bool TrySetFloatProperty(object target, string propertyName, float value)
        {
            PropertyInfo property = target?.GetType().GetProperty(propertyName, PublicInstance);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(target, value);
            return true;
        }

        private static bool TryClickTabByIndex(VisualElement element, int index, ActionContext context)
        {
            List<VisualElement> tabs = CollectTabCandidates(element);
            if (index < 0 || index >= tabs.Count)
            {
                return false;
            }

            ActionHelpers.DispatchClick(tabs[index], 1, MouseButton.LeftMouse, EventModifiers.None, context);
            return true;
        }

        private static bool TryClickTabByLabel(VisualElement element, string label, ActionContext context)
        {
            foreach (VisualElement candidate in CollectTabCandidates(element))
            {
                if (string.Equals(ActionHelpers.GetText(candidate), label, StringComparison.Ordinal))
                {
                    ActionHelpers.DispatchClick(candidate, 1, MouseButton.LeftMouse, EventModifiers.None, context);
                    return true;
                }
            }

            return false;
        }

        private static List<VisualElement> CollectTabCandidates(VisualElement root)
        {
            var results = new List<VisualElement>();
            foreach (VisualElement element in EnumerateDescendants(root))
            {
                string typeName = element.GetType().Name;
                if (!typeName.Contains("Tab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ActionHelpers.GetText(element)))
                {
                    continue;
                }

                results.Add(element);
            }

            return results;
        }

        private static IEnumerable<VisualElement> EnumerateDescendants(VisualElement root)
        {
            if (root == null)
            {
                yield break;
            }

            foreach (VisualElement child in root.Children())
            {
                yield return child;
                foreach (VisualElement nested in EnumerateDescendants(child))
                {
                    yield return nested;
                }
            }
        }

        private static bool TryResolveIndex(string valueLiteral, string indexLiteral, out int value)
        {
            value = 0;
            if (!string.IsNullOrWhiteSpace(indexLiteral))
            {
                return TryParseInt(indexLiteral, out value);
            }

            if (!string.IsNullOrWhiteSpace(valueLiteral))
            {
                return TryParseInt(valueLiteral, out value);
            }

            return false;
        }

        private static bool TryParseBool(string value, out bool parsed)
        {
            return bool.TryParse(value, out parsed);
        }

        private static bool TryParseInt(string value, out int parsed)
        {
            return int.TryParse(value, NumberStyles.Integer, InvariantCulture, out parsed);
        }

        private static bool TryParseFloat(string value, out float parsed)
        {
            return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, InvariantCulture, out parsed);
        }

        private static bool TryParseFloatPair(string value, out float left, out float right)
        {
            left = 0f;
            right = 0f;
            if (!TryParseFloatArray(value, 2, out float[] values))
            {
                return false;
            }

            left = values[0];
            right = values[1];
            return true;
        }

        private static bool TryParseFloatArray(string value, int expectedCount, out float[] values)
        {
            values = null;
            string[] parts = value.Split(',');
            if (parts.Length != expectedCount)
            {
                return false;
            }

            values = new float[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!TryParseFloat(parts[i].Trim(), out values[i]))
                {
                    values = null;
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseIntArray(string value, int expectedCount, out int[] values)
        {
            values = null;
            string[] parts = value.Split(',');
            if (parts.Length != expectedCount)
            {
                return false;
            }

            values = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!TryParseInt(parts[i].Trim(), out values[i]))
                {
                    values = null;
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseIndexList(string value, out List<int> indices)
        {
            indices = new List<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string part in value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!TryParseInt(part.Trim(), out int index))
                {
                    indices = null;
                    return false;
                }

                indices.Add(index);
            }

            return indices.Count > 0;
        }

        public static bool TryConvertStringValue(string value, Type targetType, out object converted)
        {
            converted = null;
            if (targetType == null)
            {
                return false;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (Nullable.GetUnderlyingType(targetType) != null && string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (underlyingType == typeof(string))
            {
                converted = value;
                return true;
            }

            if (underlyingType == typeof(bool) && TryParseBool(value, out bool boolValue))
            {
                converted = boolValue;
                return true;
            }

            if (underlyingType == typeof(int) && TryParseInt(value, out int intValue))
            {
                converted = intValue;
                return true;
            }

            if (underlyingType == typeof(long) && long.TryParse(value, NumberStyles.Integer, InvariantCulture, out long longValue))
            {
                converted = longValue;
                return true;
            }

            if (underlyingType == typeof(uint) && uint.TryParse(value, NumberStyles.Integer, InvariantCulture, out uint uintValue))
            {
                converted = uintValue;
                return true;
            }

            if (underlyingType == typeof(ulong) && ulong.TryParse(value, NumberStyles.Integer, InvariantCulture, out ulong ulongValue))
            {
                converted = ulongValue;
                return true;
            }

            if (underlyingType == typeof(float) && TryParseFloat(value, out float floatValue))
            {
                converted = floatValue;
                return true;
            }

            if (underlyingType == typeof(double) && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, InvariantCulture, out double doubleValue))
            {
                converted = doubleValue;
                return true;
            }

            if (underlyingType == typeof(Vector2) && TryParseFloatArray(value, 2, out float[] vector2Values))
            {
                converted = new Vector2(vector2Values[0], vector2Values[1]);
                return true;
            }

            if (underlyingType == typeof(Vector3) && TryParseFloatArray(value, 3, out float[] vector3Values))
            {
                converted = new Vector3(vector3Values[0], vector3Values[1], vector3Values[2]);
                return true;
            }

            if (underlyingType == typeof(Vector4) && TryParseFloatArray(value, 4, out float[] vector4Values))
            {
                converted = new Vector4(vector4Values[0], vector4Values[1], vector4Values[2], vector4Values[3]);
                return true;
            }

            if (underlyingType == typeof(Vector2Int) && TryParseIntArray(value, 2, out int[] vector2IntValues))
            {
                converted = new Vector2Int(vector2IntValues[0], vector2IntValues[1]);
                return true;
            }

            if (underlyingType == typeof(Vector3Int) && TryParseIntArray(value, 3, out int[] vector3IntValues))
            {
                converted = new Vector3Int(vector3IntValues[0], vector3IntValues[1], vector3IntValues[2]);
                return true;
            }

            if (underlyingType == typeof(Rect) && TryParseFloatArray(value, 4, out float[] rectValues))
            {
                converted = new Rect(rectValues[0], rectValues[1], rectValues[2], rectValues[3]);
                return true;
            }

            if (underlyingType == typeof(RectInt) && TryParseIntArray(value, 4, out int[] rectIntValues))
            {
                converted = new RectInt(rectIntValues[0], rectIntValues[1], rectIntValues[2], rectIntValues[3]);
                return true;
            }

            if (underlyingType == typeof(Bounds) && TryParseFloatArray(value, 6, out float[] boundsValues))
            {
                converted = new Bounds(
                    new Vector3(boundsValues[0], boundsValues[1], boundsValues[2]),
                    new Vector3(boundsValues[3] * 2f, boundsValues[4] * 2f, boundsValues[5] * 2f));
                return true;
            }

            if (underlyingType == typeof(BoundsInt) && TryParseIntArray(value, 6, out int[] boundsIntValues))
            {
                converted = new BoundsInt(
                    new Vector3Int(boundsIntValues[0], boundsIntValues[1], boundsIntValues[2]),
                    new Vector3Int(boundsIntValues[3], boundsIntValues[4], boundsIntValues[5]));
                return true;
            }

            if (underlyingType == typeof(Color))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color colorValue))
                {
                    converted = colorValue;
                    return true;
                }

                if (TryParseFloatArray(value, 4, out float[] rgbaValues))
                {
                    converted = new Color(rgbaValues[0], rgbaValues[1], rgbaValues[2], rgbaValues[3]);
                    return true;
                }
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(underlyingType))
            {
                if (TryLoadUnityObject(value, underlyingType, out UnityEngine.Object asset))
                {
                    converted = asset;
                    return true;
                }

                return false;
            }

            if (underlyingType == typeof(AnimationCurve) && TryParseAnimationCurve(value, out AnimationCurve curve))
            {
                converted = curve;
                return true;
            }

            if (underlyingType == typeof(Gradient) && TryParseGradient(value, out Gradient gradient))
            {
                converted = gradient;
                return true;
            }

            if (underlyingType.IsEnum)
            {
                try
                {
                    converted = Enum.Parse(underlyingType, value, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            MethodInfo parseMethod = underlyingType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (parseMethod != null)
            {
                try
                {
                    converted = parseMethod.Invoke(null, new object[] { value });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                converted = Convert.ChangeType(value, underlyingType, InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatValue(object value, Type valueType)
        {
            if (value == null)
            {
                return string.Empty;
            }

            Type underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            if (underlyingType == typeof(string))
            {
                return (string)value;
            }

            if (underlyingType == typeof(bool))
            {
                return ((bool)value) ? "true" : "false";
            }

            if (underlyingType == typeof(float))
            {
                return ((float)value).ToString("0.###", InvariantCulture);
            }

            if (underlyingType == typeof(double))
            {
                return ((double)value).ToString("0.###", InvariantCulture);
            }

            if (underlyingType == typeof(Vector2))
            {
                Vector2 vector = (Vector2)value;
                return $"{vector.x.ToString("0.###", InvariantCulture)},{vector.y.ToString("0.###", InvariantCulture)}";
            }

            if (underlyingType == typeof(Vector3))
            {
                Vector3 vector = (Vector3)value;
                return $"{vector.x.ToString("0.###", InvariantCulture)},{vector.y.ToString("0.###", InvariantCulture)},{vector.z.ToString("0.###", InvariantCulture)}";
            }

            if (underlyingType == typeof(Vector4))
            {
                Vector4 vector = (Vector4)value;
                return $"{vector.x.ToString("0.###", InvariantCulture)},{vector.y.ToString("0.###", InvariantCulture)},{vector.z.ToString("0.###", InvariantCulture)},{vector.w.ToString("0.###", InvariantCulture)}";
            }

            if (underlyingType == typeof(Vector2Int))
            {
                Vector2Int vector = (Vector2Int)value;
                return $"{vector.x},{vector.y}";
            }

            if (underlyingType == typeof(Vector3Int))
            {
                Vector3Int vector = (Vector3Int)value;
                return $"{vector.x},{vector.y},{vector.z}";
            }

            if (underlyingType == typeof(Rect))
            {
                Rect rect = (Rect)value;
                return $"{rect.x.ToString("0.###", InvariantCulture)},{rect.y.ToString("0.###", InvariantCulture)},{rect.width.ToString("0.###", InvariantCulture)},{rect.height.ToString("0.###", InvariantCulture)}";
            }

            if (underlyingType == typeof(RectInt))
            {
                RectInt rect = (RectInt)value;
                return $"{rect.x},{rect.y},{rect.width},{rect.height}";
            }

            if (underlyingType == typeof(Bounds))
            {
                Bounds bounds = (Bounds)value;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;
                return $"{center.x.ToString("0.###", InvariantCulture)},{center.y.ToString("0.###", InvariantCulture)},{center.z.ToString("0.###", InvariantCulture)},{extents.x.ToString("0.###", InvariantCulture)},{extents.y.ToString("0.###", InvariantCulture)},{extents.z.ToString("0.###", InvariantCulture)}";
            }

            if (underlyingType == typeof(BoundsInt))
            {
                BoundsInt bounds = (BoundsInt)value;
                Vector3Int position = bounds.position;
                Vector3Int size = bounds.size;
                return $"{position.x},{position.y},{position.z},{size.x},{size.y},{size.z}";
            }

            if (underlyingType == typeof(Color))
            {
                Color color = (Color)value;
                return "#" + ColorUtility.ToHtmlStringRGBA(color);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(underlyingType))
            {
                UnityEngine.Object asset = (UnityEngine.Object)value;
                string assetPath = AssetDatabase.GetAssetPath(asset);
                return string.IsNullOrWhiteSpace(assetPath) ? asset.name : assetPath;
            }

            if (underlyingType == typeof(AnimationCurve))
            {
                return FormatAnimationCurve((AnimationCurve)value);
            }

            if (underlyingType == typeof(Gradient))
            {
                return FormatGradient((Gradient)value);
            }

            return Convert.ToString(value, InvariantCulture) ?? string.Empty;
        }

        private static bool TryLoadUnityObject(string value, Type objectType, out UnityEngine.Object asset)
        {
            asset = null;
            if (string.IsNullOrWhiteSpace(value) || objectType == null || !typeof(UnityEngine.Object).IsAssignableFrom(objectType))
            {
                return false;
            }

            string path = value.Trim();
            if (path.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
            {
                path = AssetDatabase.GUIDToAssetPath(path.Substring(5).Trim());
            }
            else if (path.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(5).Trim();
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.StartsWith("name:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("asset-name:", StringComparison.OrdinalIgnoreCase))
            {
                string assetName = path.Substring(path.IndexOf(':') + 1).Trim();
                return TryLoadUnityObjectByName(assetName, objectType, out asset);
            }

            if (path.StartsWith("search:", StringComparison.OrdinalIgnoreCase))
            {
                string searchQuery = path.Substring("search:".Length).Trim();
                return TryLoadUnityObjectBySearch(searchQuery, objectType, out asset);
            }

            asset = AssetDatabase.LoadAssetAtPath(path, objectType);
            return asset != null;
        }

        private static bool TryLoadUnityObjectByName(string assetName, Type objectType, out UnityEngine.Object asset)
        {
            asset = null;
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            string filter = $"{assetName} t:{objectType.Name}";
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object candidate = AssetDatabase.LoadAssetAtPath(assetPath, objectType);
                if (candidate == null)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                if (string.Equals(candidate.name, assetName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fileName, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    asset = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadUnityObjectBySearch(string searchQuery, Type objectType, out UnityEngine.Object asset)
        {
            asset = null;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return false;
            }

            string typeName = objectType.Name;
            string needle = searchQuery;
            int separator = searchQuery.IndexOf(':');
            if (separator > 0)
            {
                string explicitType = searchQuery.Substring(0, separator).Trim();
                if (!string.IsNullOrWhiteSpace(explicitType))
                {
                    typeName = explicitType;
                }

                needle = searchQuery.Substring(separator + 1).Trim();
            }

            string filter = string.IsNullOrWhiteSpace(needle)
                ? $"t:{typeName}"
                : $"{needle} t:{typeName}";

            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object candidate = AssetDatabase.LoadAssetAtPath(assetPath, objectType);
                if (candidate == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(needle)
                    || candidate.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
                    || assetPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
                    || Path.GetFileNameWithoutExtension(assetPath).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    asset = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseAnimationCurve(string value, out AnimationCurve curve)
        {
            curve = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] keyTokens = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var keys = new List<Keyframe>();
            foreach (string token in keyTokens)
            {
                string[] parts = token.Split(new[] { ':' }, StringSplitOptions.None);
                if (parts.Length != 2 && parts.Length != 4)
                {
                    return false;
                }

                if (!TryParseFloat(parts[0].Trim(), out float time)
                    || !TryParseFloat(parts[1].Trim(), out float keyValue))
                {
                    return false;
                }

                if (parts.Length == 4)
                {
                    if (!TryParseFloat(parts[2].Trim(), out float inTangent)
                        || !TryParseFloat(parts[3].Trim(), out float outTangent))
                    {
                        return false;
                    }

                    keys.Add(new Keyframe(time, keyValue, inTangent, outTangent));
                }
                else
                {
                    keys.Add(new Keyframe(time, keyValue));
                }
            }

            if (keys.Count == 0)
            {
                return false;
            }

            curve = new AnimationCurve(keys.ToArray());
            return true;
        }

        private static bool TryParseGradient(string value, out Gradient gradient)
        {
            gradient = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] sections = value.Split('|');
            if (sections.Length != 2)
            {
                return false;
            }

            if (!TryParseGradientColorKeys(sections[0], out GradientColorKey[] colorKeys)
                || !TryParseGradientAlphaKeys(sections[1], out GradientAlphaKey[] alphaKeys))
            {
                return false;
            }

            gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return true;
        }

        private static bool TryParseGradientColorKeys(string literal, out GradientColorKey[] colorKeys)
        {
            colorKeys = null;
            string[] keyTokens = literal.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyTokens.Length == 0)
            {
                return false;
            }

            var keys = new List<GradientColorKey>();
            foreach (string token in keyTokens)
            {
                string[] parts = token.Split(new[] { ':' }, StringSplitOptions.None);
                if (parts.Length != 2
                    || !TryParseFloat(parts[0].Trim(), out float time)
                    || !ColorUtility.TryParseHtmlString(parts[1].Trim(), out Color color))
                {
                    return false;
                }

                keys.Add(new GradientColorKey(color, time));
            }

            colorKeys = keys.ToArray();
            return true;
        }

        private static bool TryParseGradientAlphaKeys(string literal, out GradientAlphaKey[] alphaKeys)
        {
            alphaKeys = null;
            string[] keyTokens = literal.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyTokens.Length == 0)
            {
                return false;
            }

            var keys = new List<GradientAlphaKey>();
            foreach (string token in keyTokens)
            {
                string[] parts = token.Split(new[] { ':' }, StringSplitOptions.None);
                if (parts.Length != 2
                    || !TryParseFloat(parts[0].Trim(), out float time)
                    || !TryParseFloat(parts[1].Trim(), out float alpha))
                {
                    return false;
                }

                keys.Add(new GradientAlphaKey(alpha, time));
            }

            alphaKeys = keys.ToArray();
            return true;
        }

        private static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.length != right.length)
            {
                return false;
            }

            for (int i = 0; i < left.length; i++)
            {
                Keyframe l = left.keys[i];
                Keyframe r = right.keys[i];
                if (Mathf.Abs(l.time - r.time) > 0.0001f
                    || Mathf.Abs(l.value - r.value) > 0.0001f
                    || Mathf.Abs(l.inTangent - r.inTangent) > 0.0001f
                    || Mathf.Abs(l.outTangent - r.outTangent) > 0.0001f)
                {
                    return false;
                }
            }

            return left.preWrapMode == right.preWrapMode && left.postWrapMode == right.postWrapMode;
        }

        private static bool GradientsEqual(Gradient left, Gradient right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            GradientColorKey[] leftColorKeys = left.colorKeys;
            GradientColorKey[] rightColorKeys = right.colorKeys;
            GradientAlphaKey[] leftAlphaKeys = left.alphaKeys;
            GradientAlphaKey[] rightAlphaKeys = right.alphaKeys;
            if (leftColorKeys.Length != rightColorKeys.Length || leftAlphaKeys.Length != rightAlphaKeys.Length)
            {
                return false;
            }

            for (int i = 0; i < leftColorKeys.Length; i++)
            {
                if (Mathf.Abs(leftColorKeys[i].time - rightColorKeys[i].time) > 0.0001f
                    || !ValuesEqual(leftColorKeys[i].color, rightColorKeys[i].color, typeof(Color)))
                {
                    return false;
                }
            }

            for (int i = 0; i < leftAlphaKeys.Length; i++)
            {
                if (Mathf.Abs(leftAlphaKeys[i].time - rightAlphaKeys[i].time) > 0.0001f
                    || Mathf.Abs(leftAlphaKeys[i].alpha - rightAlphaKeys[i].alpha) > 0.0001f)
                {
                    return false;
                }
            }

            return left.mode == right.mode;
        }

        private static string FormatAnimationCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return string.Empty;
            }

            var parts = new string[curve.length];
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                parts[i] = string.Join(":",
                    key.time.ToString("0.###", InvariantCulture),
                    key.value.ToString("0.###", InvariantCulture),
                    key.inTangent.ToString("0.###", InvariantCulture),
                    key.outTangent.ToString("0.###", InvariantCulture));
            }

            return string.Join(";", parts);
        }

        private static string FormatGradient(Gradient gradient)
        {
            if (gradient == null)
            {
                return string.Empty;
            }

            var colorParts = new string[gradient.colorKeys.Length];
            for (int i = 0; i < gradient.colorKeys.Length; i++)
            {
                GradientColorKey key = gradient.colorKeys[i];
                colorParts[i] = $"{key.time.ToString("0.###", InvariantCulture)}:#{ColorUtility.ToHtmlStringRGBA(key.color)}";
            }

            var alphaParts = new string[gradient.alphaKeys.Length];
            for (int i = 0; i < gradient.alphaKeys.Length; i++)
            {
                GradientAlphaKey key = gradient.alphaKeys[i];
                alphaParts[i] = $"{key.time.ToString("0.###", InvariantCulture)}:{key.alpha.ToString("0.###", InvariantCulture)}";
            }

            return $"{string.Join(";", colorParts)}|{string.Join(";", alphaParts)}";
        }
    }

    internal sealed class SelectOptionAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "select_option");
            context.Log($"select_option: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SelectOptionOrThrow(element, parameters, "select_option");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SelectListItemAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "select_list_item");
            if (parameters.TryGetValue("indices", out string indicesLiteral) && !string.IsNullOrWhiteSpace(indicesLiteral))
            {
                var indices = new List<int>();
                foreach (string part in indicesLiteral.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedIndex))
                    {
                        throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action select_list_item indices is invalid: {indicesLiteral}");
                    }

                    indices.Add(parsedIndex);
                }

                context.Log($"select_list_item: target {ActionContext.ElementInfo(element)} indices={indicesLiteral}");
                AdvancedActionHelpers.SelectListItemsOrThrow(element, indices, "select_list_item");
                await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
                return;
            }

            string indexLiteral = ActionHelpers.Require(parameters, "select_list_item", "index");
            if (!int.TryParse(indexLiteral, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action select_list_item index is invalid: {indexLiteral}");
            }

            context.Log($"select_list_item: target {ActionContext.ElementInfo(element)} index={index}");
            AdvancedActionHelpers.SelectListItemOrThrow(element, index, "select_list_item");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class DragReorderAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "drag_reorder");
            string fromLiteral = ActionHelpers.Require(parameters, "drag_reorder", "from_index");
            string toLiteral = ActionHelpers.Require(parameters, "drag_reorder", "to_index");
            if (!int.TryParse(fromLiteral, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fromIndex)
                || !int.TryParse(toLiteral, NumberStyles.Integer, CultureInfo.InvariantCulture, out int toIndex))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid, $"Action drag_reorder indices are invalid: from={fromLiteral}, to={toLiteral}");
            }

            context.Log($"drag_reorder: target {ActionContext.ElementInfo(element)} from={fromIndex} to={toIndex}");
            AdvancedActionHelpers.ReorderListItemOrThrow(element, fromIndex, toIndex, "drag_reorder");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SelectTreeItemAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "select_tree_item");
            context.Log($"select_tree_item: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SelectTreeItemOrThrow(element, parameters, "select_tree_item");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class ToggleFoldoutAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "toggle_foldout");
            context.Log($"toggle_foldout: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.ToggleFoldoutOrThrow(element, parameters, "toggle_foldout");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SetSliderAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "set_slider");
            context.Log($"set_slider: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SetSliderOrThrow(element, parameters, "set_slider");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SelectTabAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "select_tab");
            context.Log($"select_tab: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SelectTabOrThrow(element, parameters, "select_tab", context);
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SetBoundValueAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "set_bound_value");
            context.Log($"set_bound_value: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SetBoundValueOrThrow(element, parameters, "set_bound_value");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class AssertBoundValueAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "assert_bound_value");
            context.Log($"assert_bound_value: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.AssertBoundValueOrThrow(element, parameters, "assert_bound_value");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class NavigateBreadcrumbAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "navigate_breadcrumb");
            context.Log($"navigate_breadcrumb: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.NavigateBreadcrumbOrThrow(element, parameters, "navigate_breadcrumb", context);
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class SetSplitViewSizeAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "set_split_view_size");
            context.Log($"set_split_view_size: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.SetSplitViewSizeOrThrow(element, parameters, "set_split_view_size");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class PageScrollerAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "page_scroller");
            context.Log($"page_scroller: target {ActionContext.ElementInfo(element)}");
            AdvancedActionHelpers.PageScrollerOrThrow(element, parameters, "page_scroller");
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal static class ColumnActionHelpers
    {
        public static Columns GetColumnsOrThrow(VisualElement element, string actionName)
        {
            switch (element)
            {
                case MultiColumnListView mclv:
                    return mclv.columns;
                case MultiColumnTreeView mctv:
                    return mctv.columns;
                default:
                    throw new UnityUIFlowException(ErrorCodes.ActionTargetTypeInvalid,
                        $"Action {actionName} target is not a MultiColumnListView or MultiColumnTreeView: {element.GetType().Name}");
            }
        }

        public static Column FindColumnOrThrow(VisualElement element, Dictionary<string, string> parameters, string actionName)
        {
            Columns columns = GetColumnsOrThrow(element, actionName);
            parameters.TryGetValue("column", out string columnParam);
            parameters.TryGetValue("index", out string indexParam);

            if (string.IsNullOrWhiteSpace(columnParam) && string.IsNullOrWhiteSpace(indexParam))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterMissing,
                    $"Action {actionName} requires 'column' (name or title) or 'index' parameter.");
            }

            if (!string.IsNullOrWhiteSpace(columnParam))
            {
                foreach (Column col in columns)
                {
                    if (col.name == columnParam || col.title == columnParam)
                    {
                        return col;
                    }
                }

                throw new UnityUIFlowException(ErrorCodes.ActionOptionNotFound,
                    $"Action {actionName}: column '{columnParam}' was not found.");
            }

            if (!int.TryParse(indexParam, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid,
                    $"Action {actionName}: column index '{indexParam}' is not a valid integer.");
            }

            if (idx < 0 || idx >= columns.Count)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionIndexOutOfRange,
                    $"Action {actionName}: column index {idx} is out of range [0,{columns.Count - 1}].");
            }

            return columns[idx];
        }
    }

    internal sealed class SortColumnAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "sort_column");
            Column column = ColumnActionHelpers.FindColumnOrThrow(element, parameters, "sort_column");
            parameters.TryGetValue("direction", out string directionParam);

            SortDirection direction = SortDirection.Ascending;
            if (!string.IsNullOrWhiteSpace(directionParam))
            {
                string dir = directionParam.Trim().ToLowerInvariant();
                if (dir == "descending" || dir == "desc")
                {
                    direction = SortDirection.Descending;
                }
                else if (dir != "ascending" && dir != "asc")
                {
                    throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid,
                        $"Action sort_column: invalid direction '{directionParam}'. Use 'ascending' or 'descending'.");
                }
            }

            context.Log($"sort_column: column={column.name} direction={direction}");

            switch (element)
            {
                case MultiColumnListView mclv:
                    if (mclv.sortingMode == ColumnSortingMode.None)
                    {
                        mclv.sortingMode = ColumnSortingMode.Default;
                    }

                    mclv.sortColumnDescriptions.Clear();
                    mclv.sortColumnDescriptions.Add(new SortColumnDescription(column.name, direction));
                    break;
                case MultiColumnTreeView mctv:
                    if (mctv.sortingMode == ColumnSortingMode.None)
                    {
                        mctv.sortingMode = ColumnSortingMode.Default;
                    }

                    mctv.sortColumnDescriptions.Clear();
                    mctv.sortColumnDescriptions.Add(new SortColumnDescription(column.name, direction));
                    break;
            }

            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }

    internal sealed class ResizeColumnAction : IAction
    {
        public async Task ExecuteAsync(VisualElement root, ActionContext context, Dictionary<string, string> parameters)
        {
            VisualElement element = await ActionHelpers.RequireElementAsync(context, parameters, "resize_column");
            Column column = ColumnActionHelpers.FindColumnOrThrow(element, parameters, "resize_column");
            string widthLiteral = ActionHelpers.Require(parameters, "resize_column", "width");

            if (!float.TryParse(widthLiteral, NumberStyles.Float, CultureInfo.InvariantCulture, out float widthPx) || widthPx <= 0f)
            {
                throw new UnityUIFlowException(ErrorCodes.ActionParameterInvalid,
                    $"Action resize_column: invalid width '{widthLiteral}'. Must be a positive pixel value.");
            }

            context.Log($"resize_column: column={column.name} width={widthPx}px");
            column.width = new Length(widthPx, LengthUnit.Pixel);
            await EditorAsyncUtility.NextFrameAsync(context.CancellationToken);
        }
    }
}
