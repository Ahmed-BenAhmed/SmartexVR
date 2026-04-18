using UnityEngine;
using TMPro;
using Smartex.Core;

namespace Smartex.Machines
{
    public class EnergyBar : MonoBehaviour
    {
        public Transform   barFill;
        public Renderer    barRenderer;
        public TextMeshPro label;
        public float       powerCritWatts = 800f;
        public float       minHeight      = 0.05f;
        public float       maxHeight      = 3.0f;

        private Material _mat;
        private Vector3  _basePos;
        private float    _watts  = 0f;
        private bool     _online = false;

        static readonly int BaseColorId     = Shader.PropertyToID("_Color");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (barFill != null)
                _basePos = new Vector3(barFill.position.x, 0f, barFill.position.z);

            if (barRenderer != null)
            {
                _mat = new Material(barRenderer.sharedMaterial);
                _mat.EnableKeyword("_EMISSION");
                barRenderer.material = _mat;
            }
        }

        public void SetPower(float watts, bool online)
        {
            _watts  = watts;
            _online = online;

            // Update bar height immediately
            if (barFill != null)
            {
                float t    = online ? Mathf.Clamp01(watts / powerCritWatts) : 0f;
                float newH = Mathf.Lerp(minHeight, maxHeight, t);
                var s = barFill.localScale;
                s.y = newH;
                barFill.localScale = s;
                // Keep bottom flush with floor — pivot is at cube centre
                var p = _basePos;
                p.y = newH * 0.5f;
                barFill.position = p;
            }

            if (label != null)
            {
                if (!online) { label.text = "OFF"; return; }
                label.text = watts >= 1000f ? $"{watts / 1000f:F1}kW" : $"{watts:F0}W";
            }
        }

        // Update colour every frame — same pattern as HealthAura — so the material
        // is always in sync even if _mat was null during the first SetPower() call.
        void Update()
        {
            if (_mat == null)
            {
                // Late init if Awake ran before barRenderer was assigned
                if (barRenderer != null)
                {
                    _mat = new Material(barRenderer.sharedMaterial);
                    _mat.EnableKeyword("_EMISSION");
                    barRenderer.material = _mat;
                }
                return;
            }

            var cfg = SmartexConfig.Instance;
            float t = _online ? Mathf.Clamp01(_watts / powerCritWatts) : 0f;
            Color c = _online
                ? Color.Lerp(cfg.healthyColor, cfg.criticalColor, t)
                : cfg.offlineColor;

            _mat.SetColor(BaseColorId,     c);
            _mat.SetColor(EmissionColorId, c * (_online ? 1.5f : 0.05f));
        }
    }
}
