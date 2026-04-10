using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityUIFlow
{
    [FilePath("ProjectSettings/UnityUIFlowSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UnityUIFlowProjectSettings : ScriptableSingleton<UnityUIFlowProjectSettings>
    {
        [SerializeField] private bool _alwaysEnableVerboseLog;
        [SerializeField] private int _preStepDelayMs;

        public bool AlwaysEnableVerboseLog
        {
            get => _alwaysEnableVerboseLog;
            set => _alwaysEnableVerboseLog = value;
        }

        public int PreStepDelayMs
        {
            get => Mathf.Clamp(_preStepDelayMs, 0, UnityUIFlowProjectSettingsUtility.MaxPreStepDelayMs);
            set => _preStepDelayMs = Mathf.Clamp(value, 0, UnityUIFlowProjectSettingsUtility.MaxPreStepDelayMs);
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }

    internal static class UnityUIFlowProjectSettingsUtility
    {
        public const string SettingsPath = "Project/UnityUIFlow";
        public const int MaxPreStepDelayMs = 60000;

        public static bool IsVerboseLoggingEnabled(bool runtimeEnabled)
        {
            return UnityUIFlowProjectSettings.instance.AlwaysEnableVerboseLog || runtimeEnabled;
        }

        public static TestOptions ApplyOverrides(TestOptions options)
        {
            TestOptions resolved = options?.Clone() ?? new TestOptions();
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;

            resolved.EnableVerboseLog = settings.AlwaysEnableVerboseLog || resolved.EnableVerboseLog;
            if (settings.PreStepDelayMs > 0 && resolved.Headed)
            {
                resolved.PreStepDelayMs = settings.PreStepDelayMs;
            }

            return resolved;
        }
    }

    internal static class UnityUIFlowProjectSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(UnityUIFlowProjectSettingsUtility.SettingsPath, SettingsScope.Project)
            {
                label = "UnityUIFlow",
                guiHandler = _ => DrawGui(),
                keywords = new HashSet<string>
                {
                    "UnityUIFlow",
                    "verbose",
                    "log",
                    "delay",
                    "highlight",
                    "debug",
                    "input",
                },
            };
        }

        private static void DrawGui()
        {
            UnityUIFlowProjectSettings settings = UnityUIFlowProjectSettings.instance;

            EditorGUI.BeginChangeCheck();

            bool forceLog = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Always Enable Verbose Log",
                    "When enabled, UnityUIFlow verbose logging is always on and runtime log flags are ignored."),
                settings.AlwaysEnableVerboseLog);

            EditorGUILayout.HelpBox(
                "This project setting has higher priority than CLI, window, or temporary verbose-log switches.",
                MessageType.Info);

            int delayMs = EditorGUILayout.IntField(
                new GUIContent(
                    "Pre-Step Delay (ms)",
                    "Adds a delay before each step action starts. Set 1000 for a 1 second debug pause."),
                settings.PreStepDelayMs);
            delayMs = Mathf.Clamp(delayMs, 0, UnityUIFlowProjectSettingsUtility.MaxPreStepDelayMs);

            EditorGUILayout.HelpBox(
                "The delay happens after the target element is highlighted and before simulated input or assertions run. It only applies to headed/debug runs and is ignored by non-headed automated tests.",
                MessageType.None);

            if (EditorGUI.EndChangeCheck())
            {
                settings.AlwaysEnableVerboseLog = forceLog;
                settings.PreStepDelayMs = delayMs;
                settings.SaveSettings();
            }
        }
    }
}
