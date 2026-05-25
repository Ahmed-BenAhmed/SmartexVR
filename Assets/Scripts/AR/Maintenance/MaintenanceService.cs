using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Smartex.AR.Contracts;
using Smartex.Core;

namespace Smartex.AR.Maintenance
{
    /// <summary>
    /// Production IMaintenanceService — fetches procedures from the IEIA FastAPI backend.
    /// 
    /// Usage:
    ///   var svc = FindFirstObjectByType<MaintenanceService>();
    ///   var proc = await svc.GetProcedure("ESP32_TEX_001");
    ///   await svc.LogCompletion("ESP32_TEX_001", proc.procedure_id, new[] { 1, 2, 3 }, "user123");
    /// 
    /// Fallback: If backend is unreachable, loads bundled JSON from Resources/maintenance/fallback.json
    /// </summary>
    public class MaintenanceService : MonoBehaviour, IMaintenanceService
    {
        [Header("Backend Configuration")]
        [SerializeField] private string _baseUrl = "http://localhost:8000";  // Override in inspector or ARConfig
        [SerializeField] private int _timeoutSeconds = 8;

        private SmartexConfig _cfg;

        void Awake()
        {
            ARServices.Register((IMaintenanceService)this);
            _cfg = SmartexConfig.Instance;
            if (_cfg != null && !string.IsNullOrEmpty(_cfg.relayBaseUrl))
                _baseUrl = _cfg.relayBaseUrl;
        }

        /// <summary>
        /// Fetch maintenance procedure for a device from the backend.
        /// Falls back to bundled JSON if backend unreachable.
        /// </summary>
        public async Task<Procedure> GetProcedure(string deviceId, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/maintenance/procedures/{deviceId}";
            
            try
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    req.timeout = _timeoutSeconds;
                    var asyncOp = req.SendWebRequest();
                    
                    // Convert to async/await
                    while (!asyncOp.isDone)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            req.Abort();
                            throw new OperationCanceledException();
                        }
                        await Task.Delay(10, ct);
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[Maintenance] Backend error ({req.responseCode}): {req.error}. Using fallback.");
                        return LoadFallbackProcedure(deviceId);
                    }

                    var json = req.downloadHandler.text;
                    var proc = JsonUtility.FromJson<Procedure>(json);
                    
                    if (proc == null)
                    {
                        Debug.LogWarning($"[Maintenance] Failed to parse procedure JSON. Using fallback.");
                        return LoadFallbackProcedure(deviceId);
                    }
                    
                    Debug.Log($"[Maintenance] Fetched procedure for {deviceId}: {proc.steps.Count} steps");
                    return proc;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Maintenance] Exception fetching procedure: {ex.Message}. Using fallback.");
                return LoadFallbackProcedure(deviceId);
            }
        }

        /// <summary>
        /// Log completed maintenance steps to the backend.
        /// </summary>
        public async Task LogCompletion(string deviceId, string procedureId, int[] completedSteps, string userId)
        {
            var url = $"{_baseUrl}/maintenance/logs";
            
            var log = new MaintenanceLog
            {
                device_id       = deviceId,
                procedure_id    = procedureId,
                user_id         = userId,
                completed_steps = completedSteps,
                completed_at_utc = DateTime.UtcNow,
            };

            var json = JsonUtility.ToJson(log);

            try
            {
                using (var req = new UnityWebRequest(url, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = _timeoutSeconds;
                    
                    var asyncOp = req.SendWebRequest();
                    
                    while (!asyncOp.isDone)
                        await Task.Delay(10);

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[Maintenance] Failed to log completion: {req.error} ({req.responseCode})");
                    }
                    else
                    {
                        Debug.Log($"[Maintenance] Logged {completedSteps.Length} steps for {deviceId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Maintenance] Exception logging completion: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a fallback procedure from bundled JSON.
        /// This allows development without the backend.
        /// </summary>
        private Procedure LoadFallbackProcedure(string deviceId)
        {
            var fallback = Resources.Load<TextAsset>("maintenance/fallback");
            if (fallback == null)
            {
                Debug.LogWarning("[Maintenance] No fallback JSON found. Creating empty procedure.");
                return new Procedure
                {
                    procedure_id   = "proc_fallback",
                    device_id      = deviceId,
                    title          = "Fallback Procedure",
                    schema_version = 1,
                    steps          = new List<ProcedureStep>(),
                };
            }

            return JsonUtility.FromJson<Procedure>(fallback.text);
        }
    }
}
