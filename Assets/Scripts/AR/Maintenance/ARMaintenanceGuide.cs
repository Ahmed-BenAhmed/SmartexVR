// MODULE D — AR Maintenance Workflow  (Member 4)
// Owner   : assign to member 4
// Purpose : When a scanned machine has health_score < 0.4, show a step-by-step
//           AR repair guide with numbered callouts pointing at machine parts.
//           Each completed step is logged to the IEIA backend.
//
// Backend endpoints to implement in smartex-agent-v2/backend/main.py:
//   GET  /maintenance/procedures/{device_id}  → MaintenanceProcedure JSON
//   POST /maintenance/logs                    → record completed task
//   GET  /maintenance/logs/{device_id}        → history
//
// MaintenanceProcedure JSON schema:
//   { "device_id": "ESP32_TEX_003",
//     "steps": [
//       { "id": 1, "title": "Power down loom", "description": "...",
//         "anchor_offset": {"x":0.1,"y":0.5,"z":0.0} },
//       ...
//     ] }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Smartex.Core;
using Smartex.AR.Recognition;

namespace Smartex.AR.Maintenance
{
    [System.Serializable]
    public class MaintenanceStep
    {
        public int    id;
        public string title;
        public string description;
        public Vector3 anchorOffset;   // position relative to QR anchor
    }

    [System.Serializable]
    public class MaintenanceProcedure
    {
        public string device_id;
        public List<MaintenanceStep> steps = new();
    }

    /// <summary>
    /// Fetches and drives the AR maintenance guide for a machine.
    /// Spawn one per machine when health_score &lt; healthThreshold.
    /// </summary>
    public class ARMaintenanceGuide : MonoBehaviour
    {
        [Header("UI Prefabs")]
        public GameObject stepCalloutPrefab;   // floating numbered label in AR
        public GameObject checklistPanelPrefab;

        [Header("Threshold")]
        public float healthThreshold = 0.4f;

        private string _deviceId;
        private Pose   _anchorPose;
        private int    _currentStep = 0;
        private MaintenanceProcedure _procedure;

        void OnEnable()  => MachineQRTracker.OnMachineRecognised += OnMachineScanned;
        void OnDisable() => MachineQRTracker.OnMachineRecognised -= OnMachineScanned;

        void OnMachineScanned(string deviceId, Pose pose)
        {
            var md = DataManager.Instance?.GetMachine(deviceId);
            if (md == null || md.health_score >= healthThreshold) return;

            _deviceId   = deviceId;
            _anchorPose = pose;
            StartCoroutine(FetchProcedure(deviceId));
        }

        IEnumerator FetchProcedure(string deviceId)
        {
            string url = $"{SmartexConfig.Instance.relayBaseUrl}/maintenance/procedures/{deviceId}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Maintenance] Could not fetch procedure: {req.error}");
                // TODO Member 4: fall back to a bundled JSON asset in Resources/
                yield break;
            }

            _procedure   = JsonUtility.FromJson<MaintenanceProcedure>(req.downloadHandler.text);
            _currentStep = 0;
            ShowStep(_currentStep);
        }

        public void AdvanceStep()
        {
            if (_procedure == null) return;
            StartCoroutine(LogStep(_procedure.steps[_currentStep].id));
            _currentStep++;
            if (_currentStep < _procedure.steps.Count)
                ShowStep(_currentStep);
            else
                OnGuidanceComplete();
        }

        void ShowStep(int index)
        {
            // TODO Member 4: instantiate stepCalloutPrefab at
            //   _anchorPose.position + _procedure.steps[index].anchorOffset
            // and populate text with steps[index].title / description
            Debug.Log($"[Maintenance] Step {index + 1}: {_procedure.steps[index].title}");
        }

        void OnGuidanceComplete()
        {
            Debug.Log($"[Maintenance] All steps complete for {_deviceId}.");
            // TODO Member 4: show completion banner, hide callouts
        }

        IEnumerator LogStep(int stepId)
        {
            string url  = $"{SmartexConfig.Instance.relayBaseUrl}/maintenance/logs";
            string body = JsonUtility.ToJson(new { device_id = _deviceId, step_id = stepId,
                                                    completed_at = System.DateTime.UtcNow.ToString("o") });
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[Maintenance] Log POST failed: {req.error}");
        }
    }
}
