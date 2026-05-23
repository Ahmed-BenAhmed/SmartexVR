// MODULE A — Vuforia Core (Member 1)
// Purpose : Bootstrap Vuforia session, expose tracking events to Module B
// Dependencies: Vuforia Engine 11.x

using UnityEngine;
using Vuforia;
using System;

namespace Smartex.AR.Core
{
    /// <summary>
    /// Singleton that manages Vuforia session lifecycle.
    /// Emits events when Vuforia starts/pauses so Module B can listen.
    /// 
    /// Attach to: AR_Main GameObject in SmartexAR scene
    /// </summary>
    [RequireComponent(typeof(VuforiaBehaviour))]
    public class VuforiaSessionManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────
        public static VuforiaSessionManager Instance { get; private set; }

        // ── Events (listened by Module B: MachineQRTracker) ──────────────
        public static event Action OnSessionStarted;
        public static event Action OnSessionLost;
        public static event Action OnTrackingQualityChanged;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private VuforiaBehaviour _vuforiaBehaviour;
        private bool _isInitialized = false;

        void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Log("[Module A] VuforiaSessionManager singleton created");
        }

        void OnEnable()
        {
            _vuforiaBehaviour = GetComponent<VuforiaBehaviour>();
            if (_vuforiaBehaviour == null)
            {
                LogError("[Module A] VuforiaBehaviour not found! Add it to this GameObject.");
                return;
            }

            // Subscribe to Vuforia lifecycle events
            VuforiaApplication.Instance.OnVuforiaInitialized += HandleVuforiaInitialized;
            VuforiaApplication.Instance.OnVuforiaStarted += HandleSessionStarted;
            VuforiaApplication.Instance.OnVuforiaPaused += HandleSessionLost;
            VuforiaApplication.Instance.OnVuforiaError += HandleVuforiaError;

            Log("[Module A] Subscribed to Vuforia events");
        }

        void OnDisable()
        {
            // Unsubscribe to avoid memory leaks
            if (VuforiaApplication.Instance != null)
            {
                VuforiaApplication.Instance.OnVuforiaInitialized -= HandleVuforiaInitialized;
                VuforiaApplication.Instance.OnVuforiaStarted -= HandleSessionStarted;
                VuforiaApplication.Instance.OnVuforiaPaused -= HandleSessionLost;
                VuforiaApplication.Instance.OnVuforiaError -= HandleVuforiaError;
            }

            Log("[Module A] Unsubscribed from Vuforia events");
        }

        private void HandleSessionStarted()
        {
            _isInitialized = true;
            Log("[Module A]  Vuforia session STARTED");
            OnSessionStarted?.Invoke();
        }

        private void HandleVuforiaInitialized(VuforiaInitError initError)
        {
            try
            {
                var version = VuforiaApplication.GetVuforiaLibraryVersion();
                Log($"[Module A] Vuforia initialized (initError={initError}, version={version}, isInitialized={VuforiaApplication.Instance.IsInitialized})");
            }
            catch
            {
                Log($"[Module A] Vuforia initialized (initError={initError})");
            }

            try
            {
                var cfg = VuforiaConfiguration.Instance.Vuforia;
                var licLen = (cfg.LicenseKey ?? string.Empty).Length;
                Log($"[Module A] Vuforia config at init: licLen={licLen}, delayedInit={cfg.DelayedInitialization}, logLevel={cfg.LogLevel}");
            }
            catch
            {
                // ignore
            }
        }

        private void HandleVuforiaError(VuforiaEngineError error)
        {
            // This is the easiest way to tell 'missing key' apart from 'invalid key'.
            LogError($"[Module A] Vuforia error: {error}");
        }

        private void HandleSessionLost(bool paused)
        {
            if (paused)
            {
                _isInitialized = false;
                Log("[Module A]  Vuforia session LOST / paused");
                OnSessionLost?.Invoke();
            }
        }

        /// <summary>
        /// Check if Vuforia is initialized and ready.
        /// </summary>
        public bool IsReady => _isInitialized;

        /// <summary>
        /// Manually pause/resume Vuforia if needed.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_vuforiaBehaviour != null)
            {
                _vuforiaBehaviour.enabled = !paused;
            }
        }

        // -- Helper ----------------------------------------------------
        private void Log(string msg)
        {
            if (enableDebugLogs)
                Debug.Log(msg);
        }

        private void LogError(string msg)
        {
            Debug.LogError(msg);
        }

    }
}
