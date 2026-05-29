using UnityEngine;

namespace Smartex.AR.ModuleC
{
    public class ModuleCTestHarness : MonoBehaviour
    {
        [Header("Target Module C Setup")]
        [SerializeField] private MachineDataBinder overlayPanel;
        [SerializeField] private string testMachineId = "ESP32_TEX_001";

        [Header("🔴 LIVE TEST CONTROLS (Tweak These In Play Mode!)")]
        [Range(0f, 1f)] 
        [SerializeField] private float simulatedHealth = 0.85f;
        
        [Range(0f, 1000f)] 
        [SerializeField] private float simulatedPower = 450f;

        void Start()
        {
            if (overlayPanel == null) return;
            overlayPanel.Initialize(testMachineId);
        }

        // We bypass the read-only database completely and test the UI components directly!
        void Update()
        {
            if (overlayPanel == null) return;

            // We look inside the overlayPanel and manually update the text layers 
            // using our sliders so you can see your visual work action live!
            // We use a safe direct layout feedback method for our local editor test:
            UpdateDisplayFieldsVisually();
        }

        private void UpdateDisplayFieldsVisually()
        {
            // This reads your sliders and forces the UI layers to update manually for the test
            var nameText = overlayPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (nameText != null)
            {
                // Let's look for all text components attached to your canvas setup
                var fields = overlayPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                
                // Assuming standard order based on your hierarchy layout:
                // fields[0] is usually Name, fields[1] is Power, fields[2] is Health
                if (fields.Length >= 3)
                {
                    fields[0].text = $"ID: {testMachineId}";
                    fields[1].text = $"Power: {simulatedPower:F1} W";
                    fields[2].text = $"Health: {(simulatedHealth * 100f):F0}%";

                    // Test your color-coding thresholds visually!
                    if (simulatedHealth < 0.4f)
                        fields[2].color = Color.red;
                    else if (simulatedHealth < 0.7f)
                        fields[2].color = new Color(1f, 0.6f, 0f); // Orange
                    else
                        fields[2].color = Color.green;
                }
            }
        }
    }
}