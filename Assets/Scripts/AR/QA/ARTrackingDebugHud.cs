using UnityEngine;
using Smartex.Core;
using Smartex.AR.Recognition;
using UnityEngine.XR.ARSubsystems;

namespace Smartex.AR.QA
{
    /// <summary>
    /// Minimal on-screen debug HUD to validate tracking on device.
    ///
    /// Enabled via Resources/ARConfig.asset -> showTrackingDebugHud.
    /// Displays the last FOUND/LOST device id emitted via MachineQRTracker event contract.
    /// </summary>
    public sealed class ARTrackingDebugHud : MonoBehaviour
    {
        private static ARTrackingDebugHud _instance;

        private string _status = "No target";
        private float _lastChangeTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfEnabled()
        {
            var cfg = ARConfig.Instance;
            if (cfg == null || !cfg.showTrackingDebugHud)
                return;

            if (_instance != null)
                return;

            var go = new GameObject("[QA] ARTrackingDebugHud");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ARTrackingDebugHud>();
        }

        private void OnEnable()
        {
            MachineQRTracker.OnMachineRecognised += HandleFound;
            MachineQRTracker.OnMachineLost += HandleLost;
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            MachineQRTracker.OnMachineRecognised -= HandleFound;
            MachineQRTracker.OnMachineLost -= HandleLost;
        }

        private void HandleFound(string deviceId, Pose pose)
        {
            _status = $"FOUND: {deviceId}";
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        private void HandleLost(string deviceId)
        {
            _status = $"LOST: {deviceId}";
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        private void OnGUI()
        {
            // Keep this very small and non-invasive.
            var elapsed = Time.realtimeSinceStartup - _lastChangeTime;
            var footer = $"t+{elapsed:F1}s";

            var rect = new Rect(12, 12, Screen.width - 24, 60);
            GUI.Label(rect, $"AR Tracking: {_status}\n{footer}");
        }
    }
}
