using UnityEngine;
using TMPro;
using Smartex.Core;
using Smartex.Core.Models;
using Smartex.UI;

namespace Smartex.Machines
{
    [SelectionBase]
    public class MachineController : MonoBehaviour
    {
        [Header("Identity")]
        public string deviceId = "ESP32_TEX_001";

        [Header("Child References")]
        public Renderer     bodyRenderer;
        public HealthAura   healthAura;
        public EnergyBar    energyBar;
        public SensorLabel  sensorLabel;

        [Header("Alert Pulse")]
        public GameObject alertBeacon;
        public float      alertPulseSpeed = 3f;

        public MachineData CurrentData { get; private set; }

        private SmartexConfig _cfg;
        private Material      _bodyMat;
        private bool          _selected;

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId     = Shader.PropertyToID("_Color"); // Standard pipeline uses _Color

        void Awake()
        {
            _cfg = SmartexConfig.Instance;
            if (bodyRenderer != null)
            {
                _bodyMat = new Material(bodyRenderer.sharedMaterial);
                _bodyMat.EnableKeyword("_EMISSION");
                bodyRenderer.material = _bodyMat;
            }
        }

        void Start()
        {
            // Re-find visual components by name in case serialized refs are stale
            if (healthAura == null)
            {
                var go = GameObject.Find($"Aura_{deviceId}");
                if (go != null) healthAura = go.GetComponent<HealthAura>();
            }
            if (energyBar == null)
            {
                var go = GameObject.Find($"EnergyBar_{deviceId}");
                if (go != null) energyBar = go.GetComponent<EnergyBar>();
            }

            // Subscribe here — DataManager.Instance is null during OnEnable() because
            // DontDestroyOnLoad() reorders Awake() relative to OnEnable(). By Start()
            // all Awake() calls are done and Instance is guaranteed set.
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated += OnSnapshot;
            else
                Debug.LogError($"[MC.Start] {deviceId} — DataManager still null in Start!");
        }

        void OnDisable()
        {
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated -= OnSnapshot;
        }

        void Update()
        {
            if (alertBeacon == null || CurrentData == null) return;
            bool alerting = CurrentData.alert_level >= 1f && CurrentData.is_online;
            alertBeacon.SetActive(alerting);
            if (alerting)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * alertPulseSpeed));
                alertBeacon.transform.Rotate(Vector3.up, alertPulseSpeed * 30f * Time.deltaTime);
                var r = alertBeacon.GetComponent<Renderer>();
                if (r != null)
                {
                    Color c = CurrentData.alert_level >= 2f ? _cfg.criticalColor : _cfg.warnColor;
                    r.material.SetColor(EmissionColorId, c * (0.5f + pulse * 2f));
                }
            }
        }

        void OnSnapshot(FactorySnapshot snap)
        {
            var md = snap.machines.Find(m => m.device_id == deviceId);
            if (md == null)
            {
                Debug.LogWarning($"[MC.OnSnapshot] {deviceId} NOT FOUND in snapshot of {snap.machines.Count} machines. IDs: {string.Join(",", snap.machines.ConvertAll(m => m.device_id))}");
                MarkOffline(); return;
            }
            CurrentData = md;
            Refresh();
        }

        void OnEnable()
        {
            if (DataManager.Instance != null)
                DataManager.Instance.OnSnapshotUpdated += OnSnapshot;
            else
                Debug.LogWarning($"[MC.OnEnable] {deviceId} — DataManager.Instance is NULL, cannot subscribe!");
        }

        public void Refresh()
        {
            if (CurrentData == null) return;
            Debug.Log($"[MC.Refresh] {deviceId} online={CurrentData.is_online} power={CurrentData.avg_power_watts:F0}W health={CurrentData.health_score:F2} aura={healthAura != null} bar={energyBar != null}");
            Color healthColor = CurrentData.is_online
                ? _cfg.GetHealthColor(CurrentData.health_score)
                : _cfg.offlineColor;

            if (_bodyMat != null)
            {
                _bodyMat.SetColor(BaseColorId, healthColor * 0.4f);
                _bodyMat.SetColor(EmissionColorId, healthColor * (CurrentData.is_online ? 0.6f : 0.1f));
            }

            healthAura?.SetHealth(CurrentData.health_score, CurrentData.is_online);
            energyBar?.SetPower(CurrentData.avg_power_watts, CurrentData.is_online);
            sensorLabel?.UpdateLabel(CurrentData);
        }

        void MarkOffline()
        {
            if (_bodyMat != null)
            {
                _bodyMat.SetColor(BaseColorId,     _cfg.offlineColor * 0.3f);
                _bodyMat.SetColor(EmissionColorId, Color.black);
            }
            healthAura?.SetHealth(0f, false);
            energyBar?.SetPower(0f, false);
            if (alertBeacon != null) alertBeacon.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (_bodyMat != null && CurrentData != null)
            {
                float boost = selected ? 3f : 1f;
                _bodyMat.SetColor(EmissionColorId,
                    _cfg.GetHealthColor(CurrentData.health_score) * boost);
            }
        }

        public void NotifyClicked()
        {
            MachineDetailPanel.Instance?.Open(this);
        }
    }
}
