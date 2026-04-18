using UnityEngine;
using TMPro;
using Smartex.Core;
using Smartex.Core.Models;

namespace Smartex.UI
{
    public class FactoryHUD : MonoBehaviour
    {
        [Header("KPI Labels")]
        public TextMeshProUGUI totalPowerText;
        public TextMeshProUGUI co2TodayText;
        public TextMeshProUGUI cbamExposureText;
        public TextMeshProUGUI machineStatusText;
        public TextMeshProUGUI lastUpdateText;
        public TextMeshProUGUI connectionStatusText;

        [Header("Alert Summary")]
        public TextMeshProUGUI alertCountText;
        public GameObject      alertWarningIcon;
        public GameObject      alertCriticalIcon;

        [Header("Branding")]
        public TextMeshProUGUI factoryNameText;

        private SmartexConfig _cfg;
        private float         _blinkTimer;

        void Awake()
        {
            _cfg = SmartexConfig.Instance;
            if (factoryNameText != null) factoryNameText.text = "TNG-01  .  SmartTex Digital Twin";
        }

        void Start()
        {
            if (DataManager.Instance == null) return;
            DataManager.Instance.OnSnapshotUpdated    += Refresh;
            DataManager.Instance.OnConnectionError    += OnError;
            DataManager.Instance.OnConnectionRestored += OnRestored;
        }

        void OnDisable()
        {
            if (DataManager.Instance == null) return;
            DataManager.Instance.OnSnapshotUpdated    -= Refresh;
            DataManager.Instance.OnConnectionError    -= OnError;
            DataManager.Instance.OnConnectionRestored -= OnRestored;
        }

        void Update()
        {
            _blinkTimer += Time.deltaTime;
            if (connectionStatusText != null && DataManager.Instance != null && DataManager.Instance.IsConnected)
                connectionStatusText.color = (Mathf.Sin(_blinkTimer * 2f) > 0f) ? Color.green : new Color(0f, 0.5f, 0f);
        }

        void Refresh(FactorySnapshot snap)
        {
            var f = snap.factory;
            if (totalPowerText   != null) totalPowerText.text   = $"{f.total_power_kw:F1} kW";
            if (co2TodayText     != null) co2TodayText.text     = $"{f.total_co2_today_kg:F1} kg CO2";
            if (cbamExposureText != null) cbamExposureText.text = $"{f.cbam_exposure_mad:F0} MAD";

            int ok = 0, warn = 0, crit = 0, offline = 0;
            foreach (var m in snap.machines)
            {
                if (!m.is_online)        { offline++; continue; }
                if (m.alert_level >= 2f) crit++;
                else if (m.alert_level >= 1f) warn++;
                else ok++;
            }

            if (machineStatusText != null)
                machineStatusText.text =
                    $"<color=green>{ok} OK</color>  " +
                    $"<color=orange>{warn} WARN</color>  " +
                    $"<color=red>{crit} CRIT</color>  " +
                    $"<color=grey>{offline} OFF</color>";

            if (alertCountText != null)
            {
                int total = warn + crit;
                alertCountText.text  = total > 0 ? $"{total} ALERT{(total > 1 ? "S" : "")}" : "ALL CLEAR";
                alertCountText.color = crit > 0 ? _cfg.criticalColor : warn > 0 ? _cfg.warnColor : _cfg.healthyColor;
            }

            if (alertWarningIcon  != null) alertWarningIcon.SetActive(warn > 0);
            if (alertCriticalIcon != null) alertCriticalIcon.SetActive(crit > 0);
            if (lastUpdateText    != null && DataManager.Instance != null)
                lastUpdateText.text = $"Updated {DataManager.Instance.LastUpdateUTC:HH:mm:ss} UTC";
        }

        void OnError(string err)
        {
            if (connectionStatusText != null) { connectionStatusText.text = "OFFLINE"; connectionStatusText.color = Color.red; }
        }

        void OnRestored()
        {
            if (connectionStatusText != null) { connectionStatusText.text = "LIVE"; connectionStatusText.color = Color.green; }
        }
    }
}
