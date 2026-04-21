using UnityEngine;

namespace Smartex.Core
{
    [CreateAssetMenu(fileName = "SmartexConfig", menuName = "Smartex/Config")]
    public class SmartexConfig : ScriptableObject
    {
        private static SmartexConfig _instance;
        public static SmartexConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<SmartexConfig>("SmartexConfig");
                if (_instance == null)
                {
                    _instance = CreateInstance<SmartexConfig>();
                    Debug.LogWarning("[SmartexConfig] No asset found in Resources — using defaults.");
                }
                return _instance;
            }
        }

        [Header("Network")]
        public string relayBaseUrl     = "http://localhost:8000";
        public string influxUrl        = "https://influxdb.smartex.ahmedbenahmed.com";
        public string influxToken      = "smartex-dev-token-change-me";
        public string influxOrg        = "smartex";
        public string influxBucket     = "telemetry";
        public float  pollIntervalSeconds = 5f;

        [Header("CBAM / Economics")]
        public float carbonPriceEUR    = 65f;
        public float gridEmissionFactor = 0.742f;
        public float eurToMAD          = 10.8f;
        public float annualProduction  = 50000f;

        [Header("Layout")]
        public float machineSpacingX   = 6f;
        public float machineSpacingZ   = 7f;

        [Header("Health Thresholds")]
        public float healthyThreshold  = 0.7f;
        public float warnThreshold     = 0.4f;

        [Header("Colors")]
        public Color healthyColor  = new Color(0.2f, 0.9f, 0.3f);
        public Color warnColor     = new Color(1.0f, 0.6f, 0.0f);
        public Color criticalColor = new Color(0.9f, 0.1f, 0.1f);
        public Color offlineColor  = new Color(0.4f, 0.4f, 0.4f);

        public Color GetHealthColor(float score)
        {
            if (score >= healthyThreshold) return healthyColor;
            if (score >= warnThreshold)    return warnColor;
            return criticalColor;
        }
    }
}
