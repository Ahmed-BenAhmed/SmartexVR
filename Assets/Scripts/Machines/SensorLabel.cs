using UnityEngine;
using TMPro;
using Smartex.Core.Models;

namespace Smartex.Machines
{
    public class SensorLabel : MonoBehaviour
    {
        public TextMeshPro labelText;
        private Transform _cam;

        void Start() => _cam = Camera.main?.transform;

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main?.transform;
            if (_cam != null)
                transform.LookAt(transform.position + _cam.rotation * Vector3.forward,
                                 _cam.rotation * Vector3.up);
        }

        public void UpdateLabel(MachineData md)
        {
            if (labelText == null || md == null) return;
            if (!md.is_online) { labelText.text = "OFFLINE"; return; }
            labelText.text =
                $"VIB {md.rms_vib:F1} mm/s\n" +
                $"PWR {md.avg_power_watts:F0} W\n" +
                $"DYE {md.dye_tank_temp_c:F0}C\n" +
                $"CBAM {md.cbam_contribution:F0} EUR/yr";
        }
    }
}
