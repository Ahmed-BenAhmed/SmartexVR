using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Smartex.AR.Contracts;
using Smartex.Core;

namespace Smartex.AR.Maintenance
{
    /// <summary>
    /// Displays maintenance AR UI when a machine needs maintenance.
    /// - Banner: floating "Maintenance Required" that appears when health_score < threshold
    /// - Step Panel: numbered callouts at hotspot_position for each step
    /// - Navigation: next/prev/skip buttons
    /// - Confirmation: submit button to log completion
    /// </summary>
    public class MaintenanceUIController : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _bannerPrefab;    // floating canvas with "Maintenance Required"
        [SerializeField] private GameObject _stepCalloutPrefab; // floating step label (e.g., "1. Power down")

        [Header("Settings")]
        [SerializeField] private float _healthThreshold = 0.4f;
        [SerializeField] private float _bannerYOffset = 0.3f;  // above the machine

        private IMaintenanceService _maintenanceService;
        private IMachineRecognizer _recognizer;
        private Dictionary<string, GameObject> _activeBanners = new();
        private Dictionary<string, List<GameObject>> _stepCallouts = new();

        void Start()
        {
            // Find the maintenance service (could be MaintenanceService or MockMaintenanceService)
            _maintenanceService = FindFirstObjectByType<IMaintenanceService>();
            if (_maintenanceService == null)
            {
                Debug.LogWarning("[MaintenanceUI] No IMaintenanceService found. Some features will be disabled.");
            }

            // Get recognizer from service registry
            _recognizer = ARServices.Get<IMachineRecognizer>();
            if (_recognizer != null)
            {
                Debug.Log("[MaintenanceUI] Connected to machine recognizer");
            }
        }

        void OnDestroy()
        {
            // Clean up all banners and callouts when controller is destroyed
            foreach (var banner in _activeBanners.Values)
                if (banner != null) Destroy(banner);
            
            foreach (var callouts in _stepCallouts.Values)
                foreach (var callout in callouts)
                    if (callout != null) Destroy(callout);
        }

        /// <summary>
        /// Called when a machine is recognized (from IMachineRecognizer.OnMachineRecognized)
        /// </summary>
        public void OnMachineRecognized(RecognizedMachine machine)
        {
            if (machine.Data.health_score >= _healthThreshold)
                return;  // Machine is healthy, no maintenance needed

            Debug.Log($"[MaintenanceUI] Machine {machine.DeviceId} needs maintenance (health={machine.Data.health_score})");
            
            // Create banner
            CreateMaintenanceBanner(machine);
        }

        /// <summary>
        /// Called when a machine is lost (from IMachineRecognizer.OnMachineLost)
        /// </summary>
        public void OnMachineLost(string deviceId)
        {
            // Clean up banner
            if (_activeBanners.TryGetValue(deviceId, out var banner))
            {
                Destroy(banner);
                _activeBanners.Remove(deviceId);
            }

            // Clean up step callouts
            if (_stepCallouts.TryGetValue(deviceId, out var callouts))
            {
                foreach (var callout in callouts)
                    Destroy(callout);
                _stepCallouts.Remove(deviceId);
            }
        }

        private void CreateMaintenanceBanner(RecognizedMachine machine)
        {
            if (_bannerPrefab == null)
            {
                Debug.LogWarning("[MaintenanceUI] Banner prefab not assigned.");
                return;
            }

            // Instantiate banner as child of machine anchor
            var banner = Instantiate(_bannerPrefab, machine.AnchorTransform);
            banner.name = $"MaintenanceBanner_{machine.DeviceId}";
            
            // Position above machine
            banner.transform.localPosition = new Vector3(0, _bannerYOffset, 0);
            banner.transform.localRotation = Quaternion.identity;

            _activeBanners[machine.DeviceId] = banner;

            // Add button listener if it has one
            var btn = banner.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowMaintenanceGuide(machine));
            }

            Debug.Log($"[MaintenanceUI] Created banner for {machine.DeviceId}");
        }

        private async void ShowMaintenanceGuide(RecognizedMachine machine)
        {
            Debug.Log($"[MaintenanceUI] Fetching procedure for {machine.DeviceId}...");
            
            try
            {
                var procedure = await _maintenanceService.GetProcedure(machine.DeviceId);
                CreateStepCallouts(machine, procedure);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MaintenanceUI] Error fetching procedure: {ex.Message}");
            }
        }

        private void CreateStepCallouts(RecognizedMachine machine, Procedure procedure)
        {
            if (_stepCalloutPrefab == null)
            {
                Debug.LogWarning("[MaintenanceUI] Step callout prefab not assigned.");
                return;
            }

            // Clear old callouts
            if (_stepCallouts.TryGetValue(machine.DeviceId, out var oldCallouts))
            {
                foreach (var callout in oldCallouts)
                    Destroy(callout);
            }

            var callouts = new List<GameObject>();

            // Create a callout for each step
            foreach (var step in procedure.steps)
            {
                var callout = Instantiate(_stepCalloutPrefab, machine.AnchorTransform);
                callout.name = $"Step_{step.id}";
                callout.transform.localPosition = step.hotspot_position;
                callout.transform.localRotation = Quaternion.identity;

                // Update text
                var tmpLabel = callout.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpLabel != null)
                {
                    tmpLabel.text = $"{step.id}. {step.text}";
                }

                callouts.Add(callout);
                Debug.Log($"[MaintenanceUI] Created callout for step {step.id}");
            }

            _stepCallouts[machine.DeviceId] = callouts;
        }
    }
}
