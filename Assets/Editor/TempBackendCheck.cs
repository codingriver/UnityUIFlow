using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityUIFlow.Tests
{
    public class TempBackendCheck
    {
        [MenuItem("UnityUIFlow/Test/Backend Check")]
        public static void Check()
        {
            // Find any IMGUI window
            EditorWindow window = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.GetType().Name.Contains("Imgui"))
                {
                    window = w;
                    break;
                }
            }
            if (window == null)
            {
                window = EditorWindow.GetWindow<EditorWindow>("Test Window");
                window.Show();
            }

            Debug.Log($"Window type: {window.GetType().Name}");
            Debug.Log($"rootVisualElement: {window.rootVisualElement != null}");
            Debug.Log($"rootVisualElement.childCount: {window.rootVisualElement?.childCount ?? -1}");

            // Check for m_WindowBackend or m_Parent
            var fields = window.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fields)
            {
                var val = f.GetValue(window);
                if (val != null)
                {
                    Debug.Log($"Field: {f.Name} = {val.GetType().Name}");
                    if (f.Name.Contains("Backend") || f.Name.Contains("backend"))
                    {
                        CheckBackend(val);
                    }
                }
            }

            // Check parent
            var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
            var parent = parentField?.GetValue(window);
            Debug.Log($"m_Parent: {parent?.GetType().Name}");
            if (parent != null)
            {
                var parentFields = parent.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                foreach (var f in parentFields)
                {
                    var val = f.GetValue(parent);
                    if (val != null && (f.Name.Contains("Backend") || f.Name.Contains("IMGUI") || f.Name.Contains("imgui")))
                    {
                        Debug.Log($"  Parent field: {f.Name} = {val.GetType().Name}");
                    }
                }
            }
        }

        private static void CheckBackend(object backend)
        {
            var backendType = backend.GetType();
            var fields = backendType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fields)
            {
                var val = f.GetValue(backend);
                if (val != null && (f.Name.Contains("IMGUI") || f.Name.Contains("imgui") || f.Name.Contains("Handler") || f.Name.Contains("onGUI")))
                {
                    Debug.Log($"    Backend field: {f.Name} = {val.GetType().Name}");
                }
            }
        }
    }
}
