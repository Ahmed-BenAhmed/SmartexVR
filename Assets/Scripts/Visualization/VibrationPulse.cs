using UnityEngine;
using Smartex.Core.Models;

namespace Smartex.Visualization
{
    public class VibrationPulse : MonoBehaviour
    {
        public Renderer[] rings;
        public float maxVib = 8.5f;

        private float _vib;
        private float[] _offsets;

        void Awake()
        {
            _offsets = new float[rings.Length];
            for (int i = 0; i < rings.Length; i++)
                _offsets[i] = i * (1f / rings.Length);
        }

        public void SetVibration(float rmsVib) => _vib = rmsVib;

        void Update()
        {
            float speed = Mathf.Clamp01((_vib - 2.5f) / (maxVib - 2.5f));
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                float t = Mathf.Repeat(Time.time * speed * 2f + _offsets[i], 1f);
                float s = Mathf.Lerp(0.8f, 2.5f, t);
                rings[i].transform.localScale = new Vector3(s, rings[i].transform.localScale.y, s);
                var mat = rings[i].material;
                mat.color = new Color(1f, 0.5f, 0f, Mathf.Lerp(0.6f, 0f, t));
            }
        }
    }
}
