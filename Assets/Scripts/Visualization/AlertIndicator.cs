using System.Collections;
using UnityEngine;
using TMPro;
using Smartex.Core.Models;

namespace Smartex.Visualization
{
    public class AlertIndicator : MonoBehaviour
    {
        public TextMeshPro messageText;
        public Renderer    backgroundRenderer;
        public float       displayDuration = 4f;

        public void ShowAlert(AlertEvent alert)
        {
            if (messageText != null) messageText.text = $"! {alert.message}";
            if (backgroundRenderer != null)
                backgroundRenderer.material.color =
                    alert.alert_level >= 2f ? new Color(0.9f, 0.1f, 0.1f, 0.85f)
                                            : new Color(1.0f, 0.6f, 0.0f, 0.85f);
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AutoDismiss());
        }

        IEnumerator AutoDismiss()
        {
            yield return new WaitForSeconds(displayDuration);
            float t = 0f;
            var r = backgroundRenderer;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                if (r != null)
                {
                    var c = r.material.color;
                    c.a = Mathf.Lerp(0.85f, 0f, t);
                    r.material.color = c;
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
