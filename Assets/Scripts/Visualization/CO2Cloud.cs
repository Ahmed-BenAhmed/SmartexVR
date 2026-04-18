using UnityEngine;
using Smartex.Core.Models;

namespace Smartex.Visualization
{
    public class CO2Cloud : MonoBehaviour
    {
        public ParticleSystem particles;
        public float maxCBAM = 20f;

        public void SetData(MachineData md)
        {
            if (particles == null || md == null) return;
            var emission = particles.emission;
            float t = Mathf.Clamp01(md.cbam_contribution / maxCBAM);
            emission.rateOverTime = Mathf.Lerp(2f, 30f, t);
            var main = particles.main;
            main.startSize = Mathf.Lerp(0.2f, 1.2f, t);
            main.startColor = Color.Lerp(
                new Color(0.2f, 0.8f, 0.2f, 0.1f),
                new Color(0.5f, 0.2f, 0.1f, 0.6f), t);
        }
    }
}
