var window = UnityEditor.EditorWindow.GetWindow<UnityUIFlow.Examples.ImguiExampleWindow>();
window.Show();
window.Focus();

// Try SendEvent with Repaint
var guiLayoutUtilityType = typeof(UnityEngine.GUILayoutUtility);
var currentField = guiLayoutUtilityType.GetField("current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

UnityEngine.Debug.Log("Before SendEvent: current=" + (currentField?.GetValue(null) != null));

window.SendEvent(new UnityEngine.Event { type = UnityEngine.EventType.Repaint });

UnityEngine.Debug.Log("After SendEvent: current=" + (currentField?.GetValue(null) != null));

// Try capturing snapshot manually
var snapshot = UnityUIFlow.ImguiSnapshotCapture.Capture(window);
UnityEngine.Debug.Log("Snapshot entries: " + (snapshot?.Entries.Count ?? -1));
