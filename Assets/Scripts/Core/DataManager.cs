using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Smartex.Core.Models;

namespace Smartex.Core
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public event Action<FactorySnapshot> OnSnapshotUpdated;
        public event Action<AlertEvent>      OnAlertReceived;
        public event Action<string>          OnConnectionError;
        public event Action                  OnConnectionRestored;

        public FactorySnapshot LastSnapshot  { get; private set; }
        public bool            IsConnected   { get; private set; }
        public DateTime        LastUpdateUTC { get; private set; }

        private SmartexConfig   _cfg;
        private InfluxDBClient  _influx;
        private bool            _relayAvailable = true;
        private bool            _wasConnected   = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _cfg    = SmartexConfig.Instance;
            _influx = GetComponent<InfluxDBClient>();
            if (_influx == null) _influx = gameObject.AddComponent<InfluxDBClient>();
        }

        void Start()
        {
            StartCoroutine(InitAndPoll());
        }

        IEnumerator InitAndPoll()
        {
            // Wait one frame so every MachineController.Start() has run and wired up
            // its healthAura / energyBar references before the first snapshot fires.
            yield return null;
            InjectMockSnapshot();
            yield return StartCoroutine(PollLoop());
        }

        IEnumerator PollLoop()
        {
            while (true)
            {
                yield return FetchData();
                yield return new WaitForSeconds(_cfg.pollIntervalSeconds);
            }
        }

        IEnumerator FetchData()
        {
            if (!string.IsNullOrEmpty(_cfg.relayBaseUrl) && _relayAvailable)
                yield return FetchFromRelay();
            else
                yield return StartCoroutine(
                    _influx.FetchLatestSnapshot(OnSnapshotArrived, OnFetchError));
        }

        IEnumerator FetchFromRelay()
        {
            string url = $"{_cfg.relayBaseUrl}/snapshot";
            bool httpBlocked = false;
            using var req = UnityWebRequest.Get(url);
            req.timeout = 8;
            AsyncOperation op = null;
            try   { op = req.SendWebRequest(); }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning(
                    "[DataManager] HTTP blocked by Player Settings " +
                    $"({ex.Message}).\n" +
                    "Fix: Edit -> Project Settings -> Player -> Other Settings -> " +
                    "\"Allow downloads over HTTP\" -> Always allowed.\n" +
                    "Running on mock data until then.");
                httpBlocked     = true;
                _relayAvailable = false;
            }

            if (httpBlocked) yield break;
            yield return op;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[DataManager] Relay unreachable ({req.error}), falling back to InfluxDB direct.");
                _relayAvailable = false;
                yield return StartCoroutine(
                    _influx.FetchLatestSnapshot(OnSnapshotArrived, OnFetchError));
            }
            else
            {
                try
                {
                    var resp = JsonUtility.FromJson<RelayResponse>(req.downloadHandler.text);
                    if (resp.ok && resp.data != null)
                        OnSnapshotArrived(resp.data);
                    else
                        OnFetchError(resp.error ?? "Relay returned ok=false");
                }
                catch (Exception ex)
                {
                    OnFetchError($"Relay JSON parse: {ex.Message}");
                }
            }
        }

        void OnSnapshotArrived(FactorySnapshot snapshot)
        {
            LastSnapshot  = snapshot;
            LastUpdateUTC = DateTime.UtcNow;
            IsConnected   = true;
            if (!_wasConnected) { _wasConnected = true; OnConnectionRestored?.Invoke(); }
            OnSnapshotUpdated?.Invoke(snapshot);
            Debug.Log($"[DataManager] Snapshot: {snapshot.machines.Count} machines, {snapshot.factory.total_power_kw:F1} kW total");
        }

        void OnFetchError(string error)
        {
            IsConnected   = false;
            _wasConnected = false;
            Debug.LogWarning($"[DataManager] Fetch error: {error}");
            OnConnectionError?.Invoke(error);
        }

        public MachineData GetMachine(string deviceId)
        {
            if (LastSnapshot == null) return null;
            return LastSnapshot.machines.Find(m => m.device_id == deviceId);
        }

        public void ForceRefresh() => StartCoroutine(FetchData());

        void InjectMockSnapshot()
        {
            var snap = new FactorySnapshot { timestamp = DateTime.UtcNow.ToString("o") };
            string[] ids = { "ESP32_TEX_001","ESP32_TEX_002","ESP32_TEX_003","ESP32_TEX_004",
                             "ESP32_TEX_005","ESP32_TEX_006","ESP32_TEX_007","ESP32_TEX_008" };
            var rng = new System.Random(42);
            foreach (var id in ids)
            {
                float wear = (float)rng.NextDouble();
                snap.machines.Add(new MachineData
                {
                    device_id       = id,
                    display_name    = $"Loom {id[^3..]}",
                    avg_power_watts = 400f + wear * 400f,
                    rms_vib         = 2.5f + wear * 6f,
                    dye_tank_temp_c = 58f  + (float)rng.NextDouble() * 15f,
                    fabric_temp_c   = 41f  + (float)rng.NextDouble() * 6f,
                    tension_grams   = 23f  + (float)rng.NextDouble() * 5f,
                    // health_score, alert_level, co2_kg_today, cbam_contribution
                    // are computed properties on MachineData — derived automatically
                    // from avg_power_watts and co2_kg_h, no assignment needed.
                    wifi_rssi       = -60f - (float)rng.NextDouble() * 20f,
                    is_online       = rng.NextDouble() > 0.1,
                    last_seen       = DateTime.UtcNow.ToString("o"),
                });
            }
            snap.factory.total_power_kw     = 3.8f;
            snap.factory.total_co2_today_kg = 112f;
            snap.factory.cbam_exposure_mad  = 48f;
            OnSnapshotArrived(snap);
        }
    }
}
