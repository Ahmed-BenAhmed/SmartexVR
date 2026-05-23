using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Vuforia;
using Smartex.AR.Core;

namespace Smartex.AR.Recognition
{
    /// <summary>
    /// Bridge for projects that use Vuforia Image Targets (Module A) but want to
    /// reuse Module B's event contract (MachineQRTracker) for quick validation.
    ///
    /// Attach this to a Vuforia ImageTarget GameObject (it has an ObserverBehaviour).
    /// When the target is TRACKED, emits MachineQRTracker events with:
    ///   deviceId = TargetRegistry.GetDeviceId(vuforiaTargetName)
    ///   pose     = this transform's pose
    /// </summary>
    [RequireComponent(typeof(ObserverBehaviour))]
    public sealed class VuforiaTargetToMachineBridge : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private ObserverBehaviour _observer;
        private bool _isTracked;
        private string _deviceId;

        private const string Prefix = "machine_";

        private void Awake()
        {
            _observer = GetComponent<ObserverBehaviour>();
        }

        private void OnEnable()
        {
            if (_observer != null)
                _observer.OnTargetStatusChanged += HandleTargetStatusChanged;
        }

        private void OnDisable()
        {
            if (_observer != null)
                _observer.OnTargetStatusChanged -= HandleTargetStatusChanged;
        }

        private void HandleTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
        {
            bool nowTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
            if (nowTracked == _isTracked)
                return;

            _isTracked = nowTracked;

            if (nowTracked)
            {
                string vuforiaTargetName = behaviour != null ? behaviour.TargetName : null;
                _deviceId = ResolveDeviceId(vuforiaTargetName);

                Log($"[VuforiaBridge] FOUND '{vuforiaTargetName}' -> deviceId='{_deviceId}'");
                if (!string.IsNullOrEmpty(_deviceId))
                    MachineQRTracker.EmitRecognised(_deviceId, new Pose(transform.position, transform.rotation));
            }
            else
            {
                Log($"[VuforiaBridge] LOST deviceId='{_deviceId}'");
                if (!string.IsNullOrEmpty(_deviceId))
                    MachineQRTracker.EmitLost(_deviceId);
            }
        }

        private static string ResolveDeviceId(string vuforiaTargetName)
        {
            // Preferred: use the central registry (keeps mapping rules in one place)
            var registry = TargetRegistry.Instance;
            if (registry != null)
            {
                string id = registry.GetDeviceId(vuforiaTargetName);
                if (!string.IsNullOrEmpty(id))
                    return id;
            }

            // Fallback: strip prefix if present
            if (!string.IsNullOrEmpty(vuforiaTargetName) && vuforiaTargetName.StartsWith(Prefix))
                return vuforiaTargetName.Substring(Prefix.Length);

            return vuforiaTargetName;
        }

        private void Log(string msg)
        {
            if (enableDebugLogs)
                Debug.Log(msg);
        }
    }
}
