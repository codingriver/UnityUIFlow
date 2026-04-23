// -----------------------------------------------------------------------
// UnityPilot Editor — https://github.com/codingriver/unitypilot
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using UnityEditor;

namespace codingriver.unity.pilot
{
    [InitializeOnLoad]
    internal static class UnityPilotBootstrap
    {
        internal const string EnabledPrefKey = "codingriver.unity.pilot.BridgeEnabled";

        internal static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        static UnityPilotBootstrap()
        {
            UnityEngine.Debug.Log("[UnityPilotBootstrap] static constructor");
            EditorApplication.update += TryStartBridge;
            EditorApplication.quitting += () => UnityPilotBridge.Instance.Stop();
        }

        private static void TryStartBridge()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && IsEnabled)
            {
                UnityEngine.Debug.Log("[UnityPilotBootstrap] TryStartBridge -> EnsureStarted");
                EditorApplication.update -= TryStartBridge;
                UnityPilotBridge.Instance.EnsureStarted();
            }
        }
    }
}
