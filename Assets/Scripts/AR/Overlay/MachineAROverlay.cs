// MODULE C — Real-Time AR Data Overlay  (Member 3)
// Owner   : assign to member 3
// Purpose : Float a world-anchored UI panel above each recognised machine.
//           Panel shows health ring, power (W), vibration (mm/s), CBAM (MAD/yr).
//           Billboard faces the user. Alert halo pulses red when alert_level >= 1.
//           Tap panel → opens full MachineDetailPanel (reuses existing UI).
//
// Data source: DataManager.OnSnapshotUpdated — same event as the 3D twin.
// Zero duplication — DO NOT fetch data independently.
//
// Prefab structure (create this prefab, assign to MachineAROverlaySpawner):
//   AROverlayRoot  [BillboardFacer]
//     ├── HealthRing      (Image, filled, radial)
//     ├── PowerLabel      (TextMeshProUGUI)
//     ├── VibrationLabel  (TextMeshProUGUI)
//     ├── CBAMLabel       (TextMeshProUGUI)
//     ├── AlertHalo       (Particle System or pulsing ring)
//     └── TapTarget       (Button → NotifyClicked)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Smartex.Core;
using Smartex.Core.Models;
using Smartex.AR.Recognition;

namespace Smartex.AR.Overlay
{
    /// <summary>
    /// Spawns per-machine AR overlay panels when QR is recognised,
    /// keeps them updated via DataManager events.
    /// </summary>
    public class MachineAROverlaySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject overlayPrefab;

        [Header("Offset above QR anchor (metres)")]
        public float heightOffset = 0.4f;

        void OnEnable()
        {
            MachineQRTracker.OnMachineRecognised += SpawnOverlay;
            MachineQRTracker.OnMachineLost       += DespawnOverlay;
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated += RefreshAll;
        }

        void OnDisable()
        {
            MachineQRTracker.OnMachineRecognised -= SpawnOverlay;
            MachineQRTracker.OnMachineLost       -= DespawnOverlay;
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated -= RefreshAll;
        }

        void SpawnOverlay(string deviceId, Pose anchorPose)
        {
            if (overlayPrefab == null) return;
            var pos = anchorPose.position + Vector3.up * heightOffset;
            var go  = Instantiate(overlayPrefab, pos, Quaternion.identity);
            go.name = $"AROverlay_{deviceId}";
            var panel = go.GetComponent<MachineARPanel>();
            if (panel != null) panel.Bind(deviceId);
        }

        void DespawnOverlay(string deviceId)
        {
            var go = GameObject.Find($"AROverlay_{deviceId}");
            if (go != null) Destroy(go);
        }

        void RefreshAll(FactorySnapshot snap)
        {
            foreach (var panel in FindObjectsByType<MachineARPanel>(FindObjectsSortMode.None))
                panel.Refresh(snap);
        }
    }

    /// <summary>
    /// Lives on the overlay prefab. Binds to a device_id and updates UI.
    /// </summary>
    public class MachineARPanel : MonoBehaviour
    {
        [Header("UI References")]
        public Image          healthRing;
        public TextMeshProUGUI powerLabel;
        public TextMeshProUGUI vibrationLabel;
        public TextMeshProUGUI cbamLabel;
        public GameObject     alertHalo;

        private string _deviceId;

        public void Bind(string deviceId)
        {
            _deviceId = deviceId;
            var snap = DataManager.Instance?.LastSnapshot;
            if (snap != null) Refresh(snap);
        }

        public void Refresh(FactorySnapshot snap)
        {
            if (string.IsNullOrEmpty(_deviceId)) return;
            var md = snap.machines.Find(m => m.device_id == _deviceId);
            if (md == null) return;

            var cfg = Smartex.Core.SmartexConfig.Instance;

            if (healthRing    != null) healthRing.fillAmount = md.health_score;
            if (powerLabel    != null) powerLabel.text       = $"{md.avg_power_watts:F0} W";
            if (vibrationLabel != null) vibrationLabel.text  = $"{md.rms_vib:F1} mm/s";
            if (cbamLabel     != null) cbamLabel.text        = $"{md.cbam_contribution * cfg.eurToMAD:F0} MAD/yr";
            if (alertHalo     != null) alertHalo.SetActive(md.alert_level >= 1f && md.is_online);

            // TODO Member 3: colour healthRing based on cfg.GetHealthColor(md.health_score)
        }

        public void NotifyClicked()
        {
            // Reuse existing 3D twin detail panel
            Smartex.UI.MachineDetailPanel.Instance?.OpenById(_deviceId);
        }
    }
}
