using System;
using System.Collections.Generic;
using UnityEngine;

namespace Smartex.Core.Models
{
    [Serializable]
    public class MachineData
    {
        // Identity
        public string device_id;      // ESP32_TEX_001 … 008
        public string machine_id;     // MTX-001 … 008
        public string display_name;   // human-readable label (derived from device_id if empty)
        public string shift;          // morning / afternoon / night

        // Fields present in live smartex_derived measurement
        public float  avg_power_watts;
        public float  co2_kg_h;       // kg CO2 per hour (live)
        public float  grid_ef;        // grid emission factor

        // Fields NOT in the current measurement — kept so existing UI scripts compile.
        // Values will be 0 until the ESP32s push richer telemetry.
        public float  rms_vib;
        public float  dye_tank_temp_c;
        public float  fabric_temp_c;
        public float  tension_grams;
        public float  wifi_rssi;

        // Derived from live fields
        public float  co2_kg_today    => co2_kg_h * 8f;   // rough 8-hour shift estimate
        public float  cbam_contribution => co2_kg_today * SmartexConfig.Instance.carbonPriceEUR / 1000f;
        public float  health_score    => avg_power_watts > 0 ? Mathf.Clamp01(1f - (avg_power_watts - 400f) / 600f) : 0f;
        public float  alert_level     => avg_power_watts > 900f ? 2f : avg_power_watts > 750f ? 1f : 0f;

        public bool   is_online;
        public string last_seen;

        public string HealthLabel()
        {
            if (!is_online)        return "OFFLINE";
            if (alert_level >= 2f) return "CRITICAL";
            if (alert_level >= 1f) return "WARNING";
            return "HEALTHY";
        }
    }

    [Serializable]
    public class FactoryStats
    {
        public float total_power_kw;
        public float total_co2_today_kg;
        public float cbam_exposure_mad;
    }

    [Serializable]
    public class FactorySnapshot
    {
        public string            timestamp;
        public List<MachineData> machines = new List<MachineData>();
        public FactoryStats      factory  = new FactoryStats();
    }

    [Serializable]
    public class AlertEvent
    {
        public string device_id;
        public string message;
        public float  alert_level;
        public string timestamp;
    }

    [Serializable]
    public class RelayResponse
    {
        public bool            ok;
        public string          error;
        public FactorySnapshot data;
    }
}
