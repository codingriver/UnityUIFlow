var window = UnityEditor.EditorWindow.GetWindow<UnityUIFlow.Examples.ImguiExampleWindow>();
window.Show();
window.Focus();

// Explore m_Parent
var parentField = typeof(UnityEditor.EditorWindow).GetField("m_Parent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var parent = parentField?.GetValue(window);
UnityEngine.Debug.Log("=== m_Parent ===");
UnityEngine.Debug.Log("Type: " + (parent != null ? parent.GetType().FullName : "null"));

if (parent != null)
{
    var pt = parent.GetType();
    // Search all fields for IMGUIContainer or Action/Delegate
    var allFields = pt.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
    foreach (var f in allFields)
    {
        var name = f.Name;
        var ftype = f.FieldType;
        if (ftype.Name.Contains("IMGUI") || ftype.Name.Contains("Container") || ftype.Name.Contains("Action") || ftype.Name.Contains("Delegate") || ftype.Name.Contains("Handler"))
        {
            var val = f.GetValue(parent);
            UnityEngine.Debug.Log("Field: " + name + " | Type: " + ftype.Name + " | ValueNull=" + (val == null));
        }
    }
    
    // Also search properties
    var allProps = pt.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
    foreach (var p in allProps)
    {
        if (p.PropertyType.Name.Contains("IMGUI") || p.PropertyType.Name.Contains("Container") || p.PropertyType.Name.Contains("Action"))
        {
            UnityEngine.Debug.Log("Prop: " + p.Name + " | Type: " + p.PropertyType.Name);
        }
    }
}

// Explore EditorWindow itself for GUI-related internals
UnityEngine.Debug.Log("=== EditorWindow ===");
var ewFields = typeof(UnityEditor.EditorWindow).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
foreach (var f in ewFields)
{
    if (f.FieldType.Name.Contains("IMGUI") || f.FieldType.Name.Contains("Container") || f.FieldType.Name.Contains("GUIView") || f.FieldType.Name.Contains("View"))
    {
        var val = f.GetValue(window);
        UnityEngine.Debug.Log("EW Field: " + f.Name + " | Type: " + f.FieldType.Name + " | ValueNull=" + (val == null));
    }
}

// Try to find any method named OnGUI or DoOnGUI
UnityEngine.Debug.Log("=== Methods ===");
var methods = parent.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
foreach (var m in methods)
{
    if (m.Name.Contains("OnGUI") || m.Name.Contains("DoOnGUI") || m.Name.Contains("IMGU"))
    {
        UnityEngine.Debug.Log("Method: " + m.Name + " | Declaring: " + m.DeclaringType.Name);
    }
}
