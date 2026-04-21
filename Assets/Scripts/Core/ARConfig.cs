using UnityEngine;

namespace Smartex.Core
{
    /// <summary>
    /// Central AR-specific configuration. Lives at Resources/ARConfig.asset so
    /// every module can load it via ARConfig.Instance without Inspector wiring.
    ///
    /// Rule: NEVER hardcode URLs or the Vuforia license anywhere in scripts or
    /// scene files. Bind to ARConfig.Instance.<fieldName> instead. CI has a
    /// guard that fails the build if a Vuforia-shaped key shows up in a
    /// committed file.
    ///
    /// This is separate from SmartexConfig (which holds InfluxDB / economics
    /// stuff from Wave 0). We can unify them later; for now keep AR-specific
    /// fields here so the migration touches fewer files.
    /// </summary>
    [CreateAssetMenu(fileName = "ARConfig", menuName = "Smartex/AR Config")]
    public class ARConfig : ScriptableObject
    {
        static ARConfig _instance;
        public static ARConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<ARConfig>("ARConfig");
                if (_instance == null)
                {
                    _instance = CreateInstance<ARConfig>();
                    Debug.LogWarning(
                        "[ARConfig] No asset at Resources/ARConfig.asset — using defaults. " +
                        "Menu: Smartex VR → Create ARConfig Asset.");
                }
                return _instance;
            }
        }

        // ── Marker / recognition (Module B) ──────────────────────────────────
        [Header("Markers (Module B)")]
        [Tooltip("Backend URL that lists machine markers and logs scan events.")]
        public string markerBackendUrl = "https://api.smartex.ahmedbenahmed.com";

        [Tooltip("Target DB name on developer.vuforia.com. Used by the target-generator CLI.")]
        public string vuforiaDatabaseName = "SmartexMachines";

        // ── Remote assist (Module E) ─────────────────────────────────────────
        [Header("Remote assist (Module E)")]
        [Tooltip("WebSocket signaling endpoint for WebRTC.")]
        public string webrtcSignalingUrl = "wss://api.smartex.ahmedbenahmed.com/ws/ar-session";

        [Tooltip("STUN servers (comma-separated). Google's public STUN is fine for dev.")]
        public string stunServers = "stun:stun.l.google.com:19302,stun:stun1.l.google.com:19302";

        [Tooltip("TURN server URL. REQUIRED for production (NAT traversal).")]
        public string turnUrl = "";
        public string turnUser = "";
        public string turnSecret = "";

        // ── Vuforia (Module A) ───────────────────────────────────────────────
        [Header("Vuforia (Module A)")]
        [Tooltip("Vuforia license key. Keep this OUT of scene files. " +
                 "Load at runtime via VuforiaApplication.Instance.SetLicense(...).")]
        public string vuforiaLicenseKey = "";

        [Tooltip("Preferred tracking origin; 'target' = target-relative (factory default).")]
        public string trackingMode = "target";

        // ── Training (Module F) ──────────────────────────────────────────────
        [Header("Training (Module F)")]
        public string trainingBackendUrl = "https://api.smartex.ahmedbenahmed.com";

        [Tooltip("Default UI locale. Supported: en, fr, ar.")]
        public string defaultLocale = "en";

        // ── Performance (Module G) ───────────────────────────────────────────
        [Header("Performance / feature flags")]
        [Tooltip("Hard cap for AR overlay panels visible at once.")]
        public int maxConcurrentPanels = 8;

        [Tooltip("Disable URP shadows on machine prefabs at runtime (mobile perf).")]
        public bool disableMachineShadowsOnMobile = true;
    }
}
