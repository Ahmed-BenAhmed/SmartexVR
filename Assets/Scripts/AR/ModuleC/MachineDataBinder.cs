using UnityEngine;
using TMPro;
using UnityEngine.UI;        // Added to support native UI controls
using Smartex.Core;          
using Smartex.Core.Models;   

namespace Smartex.AR.ModuleC
{
    public class MachineDataBinder : MonoBehaviour
    {
        [Header("TextMeshPro UI Bindings")]
        [SerializeField] private TextMeshProUGUI machineNameText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI vibrationText;
        [SerializeField] private TextMeshProUGUI carbonText;

        [Header("Alert Visuals (0 GC Alloc UI Graphic Pulse)")]
        [SerializeField] private Image redHaloImage; // FIXED: Changed type to Image to perfectly accept UI game objects!

        [Header("Interaction Layout")]
        [SerializeField] private GameObject machineDetailPanel;

        private string currentTrackingMachineId;

        public void Initialize(string machineId)
        {
            currentTrackingMachineId = machineId;
            if (machineNameText != null) machineNameText.text = $"ID: {machineId}";
            
            RefreshUIDataManual();
        }

        private void OnEnable()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSnapshotUpdated += OnSnapshotReceived;
            }
        }

        private void OnDisable()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSnapshotUpdated -= OnSnapshotReceived;
            }
        }

        private void OnSnapshotReceived(FactorySnapshot snapshot)
        {
            RefreshUIDataManual();
        }

        private void RefreshUIDataManual()
        {
            if (string.IsNullOrEmpty(currentTrackingMachineId) || DataManager.Instance == null) return;

            MachineData liveData = DataManager.Instance.GetMachine(currentTrackingMachineId);
            if (liveData == null) return;

            if (powerText != null) 
                powerText.text = $"Power: {liveData.avg_power_watts:F1} W";
            
            if (vibrationText != null) 
                vibrationText.text = $"Vib (RMS): {liveData.rms_vib:F2} mm/s";
                
            if (carbonText != null) 
                carbonText.text = $"CBAM: {liveData.cbam_contribution:F2} EUR";

            if (healthText != null)
            {
                healthText.text = $"Status: {liveData.HealthLabel()} ({(liveData.health_score * 100f):F0}%)";
                
                if (liveData.alert_level >= 2f) healthText.color = Color.red;
                else if (liveData.alert_level >= 1f) healthText.color = new Color(1f, 0.6f, 0f); 
                else healthText.color = Color.green;
            }

            // Alert Level Pulse Checking (alert_level >= 1)
            bool alertActive = liveData.alert_level >= 1f; 

            if (redHaloImage != null)
            {
                redHaloImage.gameObject.SetActive(alertActive);
                if (alertActive)
                {
                    // Ping-ponging the alpha opacity value smoothly from 0.2 to 1.0
                    // Uses 0 GC allocations per frame to satisfy the performance constraints
                    float alphaPulse = Mathf.PingPong(Time.time * 2f, 0.8f) + 0.2f;
                    Color dynamicColor = redHaloImage.color;
                    dynamicColor.a = alphaPulse;
                    redHaloImage.color = dynamicColor;
                }
            }
        }

        public void OnPanelTapped()
        {
            if (machineDetailPanel != null)
            {
                machineDetailPanel.SetActive(true);
            }
        }
    }
}