// -----------------------------------------------------------------------
// UnityPilot Editor — Menu items for bridge control.
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace codingriver.unity.pilot
{
    internal static class UnityPilotMenuItems
    {
        [MenuItem("UnityPilot/Force Restart Bridge")]
        public static void RestartBridge()
        {
            Logger.Log("[Menu] Force Restart Bridge triggered.");
            UnityPilotBridge.Instance.Restart();
        }

        [MenuItem("UnityPilot/Force Restart Bridge", true)]
        public static bool ValidateRestartBridge()
        {
            return !Application.isPlaying;
        }
    }
}
