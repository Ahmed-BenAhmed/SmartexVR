using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Smartex.Core.Models;

namespace Smartex.Core
{
    public class InfluxDBClient : MonoBehaviour
    {
        private SmartexConfig _cfg;

        void Awake() => _cfg = SmartexConfig.Instance;

        public IEnumerator FetchLatestSnapshot(
            Action<FactorySnapshot> onSuccess, Action<string> onError)
        {
            // Measurement: smartex_derived  |  Fields: avg_power_watts, co2_kg_h, grid_ef
            // Tags: device_id (ESP32_TEX_001…008), machine_id, shift
            // Data range: last 30 days because the ESP32s don't push continuously
            string flux = $@"
from(bucket: ""{_cfg.influxBucket}"")
  |> range(start: -30d)
  |> filter(fn: (r) => r._measurement == ""smartex_derived"" and r.device_id != """")
  |> last()
  |> pivot(rowKey:[""device_id"",""_time""], columnKey:[""_field""], valueColumn:""_value"")";

            yield return PostFlux(flux, onSuccess, onError);
        }

        public IEnumerator FetchMachineHistory(
            string deviceId, string range,
            Action<string> onSuccess, Action<string> onError)
        {
            string flux = $@"
from(bucket: ""{_cfg.influxBucket}"")
  |> range(start: -{range})
  |> filter(fn: (r) => r._measurement == ""machine_telemetry"" and r.device_id == ""{deviceId}"")
  |> aggregateWindow(every: 1m, fn: mean)";

            using var req = new UnityWebRequest(_cfg.influxUrl + "/api/v2/query", "POST");
            byte[] body = Encoding.UTF8.GetBytes($"{{\"query\":\"{flux.Replace("\"","\\\"").Replace("\n"," ")}\",\"type\":\"flux\"}}");
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Token " + _cfg.influxToken);
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Accept", "application/csv");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                onError?.Invoke($"InfluxDB history error: {req.error}");
            else
                onSuccess?.Invoke(req.downloadHandler.text);
        }

        IEnumerator PostFlux(string flux,
            Action<FactorySnapshot> onSuccess, Action<string> onError)
        {
            string url  = _cfg.influxUrl + "/api/v2/query?org=" + _cfg.influxOrg;
            byte[] body = Encoding.UTF8.GetBytes(flux);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Token " + _cfg.influxToken);
            req.SetRequestHeader("Content-Type",  "application/vnd.flux");
            req.SetRequestHeader("Accept",        "application/csv");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"InfluxDB HTTP {req.responseCode}: {req.error}");
                yield break;
            }

            try
            {
                var snap = ParseAnnotatedCSV(req.downloadHandler.text);
                onSuccess?.Invoke(snap);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"InfluxDB CSV parse: {ex.Message}");
            }
        }

        FactorySnapshot ParseAnnotatedCSV(string csv)
        {
            var snap = new FactorySnapshot { timestamp = DateTime.UtcNow.ToString("o") };
            var lines = csv.Split('\n');

            string[] headers = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                var cols = line.Split(',');
                if (headers == null) { headers = cols; continue; }
                if (cols.Length != headers.Length) continue;

                var md = new MachineData();
                for (int i = 0; i < headers.Length; i++)
                {
                    string h = headers[i].Trim(), v = cols[i].Trim();
                    switch (h)
                    {
                        case "device_id":       md.device_id  = v; break;
                        case "machine_id":      md.machine_id = v; break;
                        case "shift":           md.shift      = v; break;
                        case "avg_power_watts": float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out md.avg_power_watts); break;
                        case "co2_kg_h":        float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out md.co2_kg_h);        break;
                        case "grid_ef":         float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out md.grid_ef);         break;
                        case "_time":           md.last_seen  = v; break;
                    }
                }
                if (!string.IsNullOrEmpty(md.device_id))
                {
                    // Derive display name from device_id if not provided
                    if (string.IsNullOrEmpty(md.display_name))
                        md.display_name = md.device_id.Replace("ESP32_TEX_", "Loom ");

                    // After pivot(), _time may be absent from the CSV columns.
                    // Determine is_online from staleness: online if last_seen is within
                    // 30 minutes, or if we have no timestamp at all (data presence = online).
                    if (!string.IsNullOrEmpty(md.last_seen) &&
                        DateTime.TryParse(md.last_seen,
                            null,
                            System.Globalization.DateTimeStyles.RoundtripKind,
                            out var lastSeenUtc))
                    {
                        md.is_online = (DateTime.UtcNow - lastSeenUtc).TotalMinutes < 30;
                    }
                    else
                    {
                        // No parseable timestamp — any data from InfluxDB counts as online
                        md.is_online = true;
                    }
                    snap.machines.Add(md);
                }
            }

            float totalPow = 0f, totalCO2 = 0f, totalCBAM = 0f;
            foreach (var m in snap.machines)
            {
                totalPow  += m.avg_power_watts;
                totalCO2  += m.co2_kg_today;
                totalCBAM += m.cbam_contribution;
            }
            snap.factory.total_power_kw     = totalPow  / 1000f;
            snap.factory.total_co2_today_kg = totalCO2;
            snap.factory.cbam_exposure_mad  = totalCBAM * SmartexConfig.Instance.eurToMAD;
            Debug.Log($"[InfluxDBClient] Parsed {snap.machines.Count} machines. Total power={snap.factory.total_power_kw:F2} kW, CO2={snap.factory.total_co2_today_kg:F2} kg/day");
            return snap;
        }
    }
}
