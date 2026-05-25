using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

namespace Smartex.AR.Training
{
    public class ProgressDashboard : MonoBehaviour
    {
        [Header("UI")]
        public GameObject      dashboardPanel;
        public TextMeshProUGUI certificationsText;

        [Header("Settings")]
        public string userId = "operator_01";

        void Start()
        {
            if (dashboardPanel != null)
                dashboardPanel.SetActive(false);
        }

        public void ShowDashboard()
        {
            if (dashboardPanel != null)
                dashboardPanel.SetActive(true);
            StartCoroutine(FetchProgress());
        }

        public void HideDashboard()
        {
            if (dashboardPanel != null)
                dashboardPanel.SetActive(false);
        }

        IEnumerator FetchProgress()
        {
            if (certificationsText != null)
                certificationsText.text = "Chargement...";

            string url = $"{Smartex.Core.SmartexConfig.Instance.relayBaseUrl}/training/progress/{userId}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                if (certificationsText != null)
                    certificationsText.text = "Aucune certification disponible";
                yield break;
            }

            if (certificationsText != null)
                certificationsText.text = "Certifications chargées ✓";
        }
    }
}