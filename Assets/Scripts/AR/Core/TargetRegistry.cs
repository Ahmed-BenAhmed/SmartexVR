// MODULE A — Target Registry (part of Vuforia Core)
// Owner   : assign to member 1
// Purpose : Maps Vuforia target names (machine_ESP32_TEX_001, etc.)
//           to clean device_ids (ESP32_TEX_001).
//
// This is the lookup table that Module B uses when a QR is detected.
// Example flow:
//   1. Vuforia detects ImageTarget named "machine_ESP32_TEX_003"
//   2. Module B calls TargetRegistry.GetDeviceId("machine_ESP32_TEX_003")
//   3. Returns "ESP32_TEX_003"
//   4. Module B fires OnMachineRecognised with that device_id
//
// The machines are registered here, so future changes (adding/removing looms)
// only require editing this file.

using UnityEngine;
using System.Collections.Generic;

namespace Smartex.AR.Core
{
    public class TargetRegistry : MonoBehaviour
    {
        public static TargetRegistry Instance { get; private set; }

        // ── Configuration ──────────────────────────────────────────────────────
        // Vuforia target names are prefixed with "machine_" to avoid confusion
        private const string PREFIX = "machine_";

        // Dictionnaire : Vuforia target name → clean device_id
        // e.g. "machine_ESP32_TEX_001" → "ESP32_TEX_001"
        private Dictionary<string, string> _registry = new Dictionary<string, string>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // ── Singleton ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register all 8 textile machines
            RegisterMachines();
        }

        private void RegisterMachines()
        {
            // The 8 looms in the factory — must match Vuforia target names
            string[] machineIds = new[]
            {
                "ESP32_TEX_001",
                "ESP32_TEX_002",
                "ESP32_TEX_003",
                "ESP32_TEX_004",
                "ESP32_TEX_005",
                "ESP32_TEX_006",
                "ESP32_TEX_007",
                "ESP32_TEX_008",
            };

            foreach (var id in machineIds)
            {
                string vuforiaTargetName = PREFIX + id;  // e.g. "machine_ESP32_TEX_001"
                Register(vuforiaTargetName);
            }

            Log($"[TargetRegistry] Registered {_registry.Count} machines");
        }

        private void Register(string vuforiaTargetName)
        {
            // Strip the "machine_" prefix to get the clean device_id
            string deviceId = vuforiaTargetName;
            if (vuforiaTargetName.StartsWith(PREFIX))
                deviceId = vuforiaTargetName.Substring(PREFIX.Length);

            _registry[vuforiaTargetName] = deviceId;
            Log($"[TargetRegistry] Registered: {vuforiaTargetName} → {deviceId}");
        }

        // ── Public API ─────────────────────────────────────────────────────────
        /// <summary>
        /// Lookup the clean device_id from a Vuforia target name.
        /// Called by Module B (MachineQRTracker) when a QR is detected.
        /// </summary>
        /// <param name="vuforiaTargetName">The target name from Vuforia (e.g. "machine_ESP32_TEX_001")</param>
        /// <returns>The clean device_id (e.g. "ESP32_TEX_001"), or null if not registered</returns>
        public string GetDeviceId(string vuforiaTargetName)
        {
            if (_registry.TryGetValue(vuforiaTargetName, out string deviceId))
                return deviceId;

            LogWarning($"[TargetRegistry] Unknown target: {vuforiaTargetName} — not registered!");
            return null;
        }

        /// <summary>
        /// Check if a target is registered (useful for validation).
        /// </summary>
        public bool IsRegistered(string vuforiaTargetName)
        {
            return _registry.ContainsKey(vuforiaTargetName);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private void Log(string msg)
        {
            if (enableDebugLogs)
                Debug.Log(msg);
        }

        private void LogWarning(string msg)
        {
            if (enableDebugLogs)
                Debug.LogWarning(msg);
        }
    }
}