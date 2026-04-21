using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Smartex.AR.Contracts.Mocks
{
    public class MockMaintenanceService : MonoBehaviour, IMaintenanceService
    {
        readonly List<MaintenanceLog> _logs = new();

        void Awake() => ARServices.Register((IMaintenanceService)this);

        public Task<Procedure> GetProcedure(string deviceId, CancellationToken ct = default)
        {
            var p = new Procedure
            {
                procedure_id   = "proc_cleaning_v1",
                device_id      = deviceId,
                title          = "Weekly cleaning",
                schema_version = 1,
                steps          = new List<ProcedureStep>
                {
                    new() { id = 1, text = "Power down the machine",               hotspot_position = new Vector3(-0.15f,  0.10f, 0f) },
                    new() { id = 2, text = "Remove dust from tension sensor",      hotspot_position = new Vector3( 0.00f,  0.20f, 0f) },
                    new() { id = 3, text = "Inspect heddle for broken wires",      hotspot_position = new Vector3( 0.12f,  0.05f, 0f) },
                    new() { id = 4, text = "Lubricate shuttle rail",               hotspot_position = new Vector3( 0.18f, -0.05f, 0f) },
                    new() { id = 5, text = "Power up and verify dashboard green",  hotspot_position = new Vector3(-0.15f,  0.10f, 0f) },
                }
            };
            return Task.FromResult(p);
        }

        public Task LogCompletion(string deviceId, string procedureId, int[] completedSteps, string userId)
        {
            var log = new MaintenanceLog
            {
                device_id        = deviceId,
                procedure_id     = procedureId,
                user_id          = userId,
                completed_steps  = completedSteps,
                completed_at_utc = DateTime.UtcNow,
            };
            _logs.Add(log);
            Debug.Log($"[MockMaintenance] logged {procedureId} for {deviceId} ({completedSteps.Length} steps)");
            return Task.CompletedTask;
        }

        public IReadOnlyList<MaintenanceLog> Logs => _logs;
    }
}
