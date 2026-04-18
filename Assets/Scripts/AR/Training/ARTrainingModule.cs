// MODULE F — Training & Onboarding  (Member 6)
// Owner   : assign to member 6
// Purpose : New operator scans machine → AR overlays component names.
//           Interactive quiz: "Tap the tension sensor" → highlight correct part.
//           Multilingual: Arabic / French / English.
//           Scores stored in IEIA backend.
//
// Backend endpoints to add:
//   GET  /training/modules/{device_type}     → TrainingModule JSON
//   POST /training/assessments               → submit score
//   GET  /training/progress/{user_id}        → certifications earned
//
// TrainingModule JSON schema:
//   { "device_type": "jacquard_loom",
//     "components": [
//       { "id": "tension_sensor",
//         "label_ar": "حساس الشد", "label_fr": "Capteur de tension", "label_en": "Tension sensor",
//         "anchor_offset": {"x":0.05,"y":0.2,"z":-0.1} }
//     ],
//     "quiz": [
//       { "question_en": "Tap the tension sensor", "correct_component": "tension_sensor" }
//     ] }

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Smartex.Core;
using Smartex.AR.Recognition;

namespace Smartex.AR.Training
{
    public enum AppLanguage { English, French, Arabic }

    [Serializable]
    public class ComponentLabel
    {
        public string id;
        public string label_en, label_fr, label_ar;
        public Vector3 anchor_offset;
    }

    [Serializable]
    public class QuizQuestion
    {
        public string question_en, question_fr, question_ar;
        public string correct_component;
    }

    [Serializable]
    public class TrainingModule
    {
        public string device_type;
        public List<ComponentLabel> components = new();
        public List<QuizQuestion>   quiz       = new();
    }

    /// <summary>
    /// Drives the AR training flow for a scanned machine.
    /// </summary>
    public class ARTrainingModule : MonoBehaviour
    {
        [Header("Language")]
        public AppLanguage language = AppLanguage.French;

        [Header("Prefabs")]
        public GameObject componentLabelPrefab;   // floating label in AR
        public GameObject quizPromptPanel;
        public TextMeshProUGUI quizPromptText;

        private string        _deviceId;
        private Pose          _anchorPose;
        private TrainingModule _module;
        private int           _quizIndex  = 0;
        private int           _score      = 0;

        void OnEnable()  => MachineQRTracker.OnMachineRecognised += OnMachineScanned;
        void OnDisable() => MachineQRTracker.OnMachineRecognised -= OnMachineScanned;

        void OnMachineScanned(string deviceId, Pose pose)
        {
            _deviceId   = deviceId;
            _anchorPose = pose;
            // device_type derived from machine_id prefix — extend as needed
            StartCoroutine(FetchModule("jacquard_loom"));
        }

        IEnumerator FetchModule(string deviceType)
        {
            string url = $"{SmartexConfig.Instance.relayBaseUrl}/training/modules/{deviceType}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Training] Module fetch failed: {req.error}");
                // TODO Member 6: fall back to bundled JSON in Resources/Training/
                yield break;
            }

            _module = JsonUtility.FromJson<TrainingModule>(req.downloadHandler.text);
            ShowComponentLabels();
            StartQuiz();
        }

        void ShowComponentLabels()
        {
            if (_module == null || componentLabelPrefab == null) return;
            foreach (var comp in _module.components)
            {
                var pos = _anchorPose.position + comp.anchor_offset;
                var go  = Instantiate(componentLabelPrefab, pos, Quaternion.identity);
                var lbl = go.GetComponentInChildren<TextMeshPro>();
                if (lbl != null)
                    lbl.text = language switch
                    {
                        AppLanguage.French  => comp.label_fr,
                        AppLanguage.Arabic  => comp.label_ar,
                        _                  => comp.label_en,
                    };
                go.name = $"Label_{comp.id}";
            }
        }

        void StartQuiz()
        {
            _quizIndex = 0;
            _score     = 0;
            ShowQuestion(_quizIndex);
        }

        void ShowQuestion(int index)
        {
            if (_module == null || index >= _module.quiz.Count) { FinishQuiz(); return; }
            var q = _module.quiz[index];
            if (quizPromptPanel != null) quizPromptPanel.SetActive(true);
            if (quizPromptText  != null)
                quizPromptText.text = language switch
                {
                    AppLanguage.French => q.question_fr,
                    AppLanguage.Arabic => q.question_ar,
                    _                 => q.question_en,
                };
        }

        /// <summary>Called when user taps a component label in AR.</summary>
        public void OnComponentTapped(string componentId)
        {
            if (_module == null || _quizIndex >= _module.quiz.Count) return;
            bool correct = _module.quiz[_quizIndex].correct_component == componentId;
            if (correct) _score++;
            // TODO Member 6: show visual feedback (green flash / red X)
            _quizIndex++;
            ShowQuestion(_quizIndex);
        }

        void FinishQuiz()
        {
            float pct = _module.quiz.Count > 0 ? (float)_score / _module.quiz.Count : 0f;
            Debug.Log($"[Training] Quiz complete for {_deviceId}: {_score}/{_module.quiz.Count} ({pct:P0})");
            if (quizPromptPanel != null) quizPromptPanel.SetActive(false);
            StartCoroutine(SubmitScore(pct));
        }

        IEnumerator SubmitScore(float score)
        {
            string url  = $"{SmartexConfig.Instance.relayBaseUrl}/training/assessments";
            string body = JsonUtility.ToJson(new {
                device_id = _deviceId, score = score,
                completed_at = DateTime.UtcNow.ToString("o") });
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[Training] Score submit failed: {req.error}");
        }
    }
}
