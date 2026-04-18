using UnityEngine;
using Smartex.Core;

namespace Smartex.Machines
{
    public class HealthAura : MonoBehaviour
    {
        public Renderer auraRenderer;
        public float minPulseSpeed = 0.5f;
        public float maxPulseSpeed = 4f;

        private Material _mat;
        private float    _health = 1f;
        private bool     _online = true;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (auraRenderer != null)
            {
                _mat = new Material(auraRenderer.sharedMaterial);
                _mat.EnableKeyword("_EMISSION");
                auraRenderer.material = _mat;
            }
        }

        public void SetHealth(float health, bool online)
        {
            _health = health;
            _online = online;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_Color"); // Standard pipeline uses _Color

        void Update()
        {
            if (_mat == null)
            {
                if (auraRenderer != null)
                {
                    _mat = new Material(auraRenderer.sharedMaterial);
                    _mat.EnableKeyword("_EMISSION");
                    auraRenderer.material = _mat;
                }
                return;
            }
            var cfg = SmartexConfig.Instance;
            Color healthColor = _online ? cfg.GetHealthColor(_health) : cfg.offlineColor;
            float speed = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, 1f - _health);
            float pulse  = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            Color glow   = healthColor * (0.5f + pulse * 1.5f);
            // Set both base colour (always visible) and emission (glow in HDR/bloom)
            _mat.SetColor(BaseColorId,     healthColor);
            _mat.SetColor(EmissionColorId, glow);
        }
    }
}
