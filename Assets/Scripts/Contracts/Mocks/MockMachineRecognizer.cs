using System;
using UnityEngine;
using Smartex.Core.Models;

namespace Smartex.AR.Contracts.Mocks
{
    /// <summary>
    /// Attach to any empty GameObject in a dev scene.
    ///   - Press 1..8 to fire OnMachineRecognized for ESP32_TEX_001..008
    ///   - Press 0      to fire OnMachineLost for the last-emitted deviceId
    ///
    /// Each fake machine gets its own empty GameObject child (this transform),
    /// positioned 2 m in front of Camera.main — that becomes the AnchorTransform.
    /// Consumers (C/D/F) parent their UI under it and develop without Vuforia.
    /// </summary>
    public class MockMachineRecognizer : MonoBehaviour, IMachineRecognizer
    {
        public event Action<RecognizedMachine> OnMachineRecognized;
        public event Action<string>            OnMachineLost;

        [Tooltip("Which device IDs the number keys 1..8 emit.")]
        public string[] deviceIds =
        {
            "ESP32_TEX_001", "ESP32_TEX_002", "ESP32_TEX_003", "ESP32_TEX_004",
            "ESP32_TEX_005", "ESP32_TEX_006", "ESP32_TEX_007", "ESP32_TEX_008"
        };

        bool   _scanning;
        string _lastEmitted;

        void Awake()
        {
            ARServices.Register((IMachineRecognizer)this);
        }

        void Update()
        {
            if (!_scanning) return;

            for (int i = 0; i < deviceIds.Length && i < 8; i++)
            {
                // Input.GetKeyDown(KeyCode.Alpha1) etc. — project uses "Both" input handling
                KeyCode k = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(k)) EmitFake(deviceIds[i]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0) && _lastEmitted != null)
            {
                OnMachineLost?.Invoke(_lastEmitted);
                Debug.Log($"[MockRecognizer] LOST {_lastEmitted}");
            }
        }

        public void StartScanning() { _scanning = true;  Debug.Log("[MockRecognizer] scanning on"); }
        public void StopScanning()  { _scanning = false; Debug.Log("[MockRecognizer] scanning off"); }

        /// <summary>Public so tests/editor buttons can drive it directly.</summary>
        public void EmitFake(string deviceId)
        {
            var anchor = FindOrCreateAnchor(deviceId);
            var data   = BuildFakeData(deviceId);
            _lastEmitted = deviceId;
            Debug.Log($"[MockRecognizer] RECOGNIZED {deviceId}");
            OnMachineRecognized?.Invoke(new RecognizedMachine(deviceId, anchor, data));
        }

        Transform FindOrCreateAnchor(string deviceId)
        {
            var t = transform.Find(deviceId);
            if (t != null) return t;

            var go  = new GameObject(deviceId);
            go.transform.SetParent(transform, false);
            var cam = Camera.main;
            if (cam != null)
            {
                // place 2 m in front of the camera with a small horizontal spread
                int slot = Array.IndexOf(deviceIds, deviceId);
                float x  = (slot - 3.5f) * 0.3f;
                go.transform.position = cam.transform.position + cam.transform.forward * 2f
                                      + cam.transform.right   * x;
                go.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
            }
            return go.transform;
        }

        static MachineData BuildFakeData(string deviceId)
        {
            // Keep deterministic so UI looks stable across re-emits
            int seed = 0; foreach (var c in deviceId) seed += c;
            var rng  = new System.Random(seed);

            return new MachineData
            {
                device_id       = deviceId,
                machine_id      = "MTX-" + deviceId.Substring(deviceId.Length - 3),
                display_name    = "Loom " + deviceId.Substring(deviceId.Length - 3),
                shift           = "morning",
                avg_power_watts = 500f + (float)rng.NextDouble() * 400f,
                co2_kg_h        = 0.2f + (float)rng.NextDouble() * 0.3f,
                grid_ef         = 0.48f,
                rms_vib         = (float)rng.NextDouble() * 4f,
                is_online       = true,
                last_seen       = DateTime.UtcNow.ToString("o"),
            };
        }
    }
}
