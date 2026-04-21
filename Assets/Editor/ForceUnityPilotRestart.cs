using UnityEditor;
using UnityEngine;

public class ForceUnityPilotRestart
{
    [MenuItem("UnityPilot/Force Restart Bridge")]
    public static void RestartBridge()
    {
        Debug.Log("[ForceUnityPilotRestart] Attempting bridge restart...");
        var bridgeType = System.Type.GetType("codingriver.unity.pilot.UnityPilotBridge, unitypilot-editor");
        if (bridgeType != null)
        {
            var instanceProperty = bridgeType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var instance = instanceProperty?.GetValue(null);
            if (instance != null)
            {
                var restartMethod = bridgeType.GetMethod("Restart", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                restartMethod?.Invoke(instance, null);
                Debug.Log("[ForceUnityPilotRestart] Restart called.");
            }
            else
            {
                Debug.LogWarning("[ForceUnityPilotRestart] Instance is null.");
            }
        }
        else
        {
            Debug.LogWarning("[ForceUnityPilotRestart] Bridge type not found.");
        }
    }

    [MenuItem("UnityPilot/Force Restart Bridge", true)]
    public static bool ValidateRestartBridge()
    {
        return !Application.isPlaying;
    }
}
