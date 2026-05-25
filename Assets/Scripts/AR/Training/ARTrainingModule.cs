// MODULE F — Training & Onboarding  (Member 6)
// Backend endpoints:
//   GET  /training/modules/{device_type}?locale=fr   → TrainingModuleDto JSON
//   POST /training/assessments                        → submit score
//   GET  /training/progress/{user_id}                 → certifications earned
//
// Vuforia notes:
//   - OnMachineRecognized gives us RecognizedMachine.AnchorTransform
//     (the Vuforia ImageTarget's transform).
//   - Every label/hotspot prefab is instantiated as a CHILD of AnchorTransform
//     → Vuforia tracking is inherited for free, no manual anchor management.
//   - On MachineLost all spawned children are destroyed automatically because
//     they are parented under the target.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Smartex.Core;
using Smartex.AR.Contracts;   // RecognizedMachine, IMachineRecognizer, TrainingModule…

namespace Smartex.AR.Training
{
    // ────────────────────────────────────────────────────────────────
    //  Serialisable DTOs for the backend JSON
    // ────────────────────────────────────────────────────────────────

    [Serializable]
    public class HotspotDto
    {
        public string component_id;
        public string display_name;        // already localised by the server
        public float  local_x, local_y, local_z;   // target-local, metres
        public Vector3 LocalPos => new(local_x, local_y, local_z);
    }

    [Serializable]
    public class QuizQuestionDto
    {
        public string question_id;
        public string prompt;              // already localised
        public string correct_hotspot_id;
    }

    [Serializable]
    public class TrainingModuleDto
    {
        public string                device_type;
        public string                locale;
        public List<HotspotDto>      hotspots  = new();
        public List<QuizQuestionDto> questions = new();
    }

    [Serializable]
    public class AssessmentDto
    {
        public string user_id;
        public string device_type;
        public int    score_percent;
        public int    duration_seconds;
        public string completed_at;
    }

    // ────────────────────────────────────────────────────────────────
    //  Main component
    // ────────────────────────────────────────────────────────────────

    public class ARTrainingModule : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────
        [Header("Language")]
        public Locale language = Locale.Fr;

        [Header("Prefabs")]
        [Tooltip("Floating label prefab — must have a TextMeshPro child + Button component")]
        public GameObject hotspotLabelPrefab;

        [Tooltip("Small sphere shown at hotspot position")]
        public GameObject hotspotMarkerPrefab;

        [Header("Quiz UI  (world-space canvas or screen-space — your choice)")]
        public GameObject      quizPanel;
        public TextMeshProUGUI quizPromptText;
        public TextMeshProUGUI feedbackText;      // "✓ Correct !" or "✗ Essaie encore"
        public TextMeshProUGUI scoreText;         // "2 / 4"

        [Header("Progress Panel")]
        public GameObject      progressPanel;
        public TextMeshProUGUI progressText;

        [Header("Settings")]
        [Tooltip("User identifier — use SystemInfo.deviceUniqueIdentifier in production")]
        public string userId = "operator_01";

        [Tooltip("Seconds the feedback message stays visible")]
        public float feedbackDuration = 1.5f;

        // ── Runtime state ──────────────────────────────────────────
        IMachineRecognizer  _recognizer;
        string              _deviceId;
        Transform           _anchorTransform;   // Vuforia ImageTarget transform
        TrainingModuleDto   _module;

        int   _quizIndex;
        int   _score;
        float _startTime;

        readonly List<GameObject> _spawnedObjects = new();

        // ── Unity lifecycle ────────────────────────────────────────

        void Start()
        {
            // Hide UI at startup
            SetPanelActive(quizPanel,      false);
            SetPanelActive(progressPanel,  false);
        }

        void OnEnable()
        {
            // Resolve the recognizer — real or mock, doesn't matter
            _recognizer = ARServices.Recognizer;
            if (_recognizer == null)
            {
                Debug.LogError("[Training] IMachineRecognizer not registered. " +
                               "Add MockMachineRecognizer to the scene for editor testing.");
                return;
            }
            _recognizer.OnMachineRecognized += HandleMachineRecognized;
            _recognizer.OnMachineLost       += HandleMachineLost;
        }

        void OnDisable()
        {
            if (_recognizer == null) return;
            _recognizer.OnMachineRecognized -= HandleMachineRecognized;
            _recognizer.OnMachineLost       -= HandleMachineLost;
        }

        // ── Recognition callbacks ──────────────────────────────────

        void HandleMachineRecognized(RecognizedMachine machine)
        {
            _deviceId        = machine.DeviceId;
            _anchorTransform = machine.AnchorTransform;   // ← Vuforia ImageTarget

            // Derive device_type from device_id  (ESP32_TEX_001 → "loom")
            // Extend this mapping as needed.
            string deviceType = DeviceTypeFor(_deviceId);

            StartCoroutine(FetchAndStartTraining(deviceType));
        }

        void HandleMachineLost(string deviceId)
        {
            if (deviceId != _deviceId) return;
            // Children of AnchorTransform are already gone when Vuforia loses the target.
            // Clean up any objects we may have parented elsewhere.
            ClearSpawnedObjects();
            SetPanelActive(quizPanel, false);
            _module = null;
        }

        // ── Fetch training content ─────────────────────────────────

        IEnumerator FetchAndStartTraining(string deviceType)
        {
            string localeParam = language switch
            {
                Locale.Fr => "fr",
                Locale.Ar => "ar",
                _         => "en"
            };

            string url = $"{SmartexConfig.Instance.relayBaseUrl}" +
                         $"/training/modules/{deviceType}?locale={localeParam}";

            using var req = UnityWebRequest.Get(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Training] Fetch failed: {req.error}. " +
                                  "Falling back to bundled JSON.");
                // Fallback: load from Resources/Training/<deviceType>.json
                _module = LoadFallbackModule(deviceType, language);
            }
            else
            {
                _module = JsonUtility.FromJson<TrainingModuleDto>(req.downloadHandler.text);
            }

            if (_module == null) yield break;

            SpawnHotspotLabels();
            BeginQuiz();
        }

        // ── Hotspot labels (Vuforia-anchored) ──────────────────────

        void SpawnHotspotLabels()
        {
            if (_anchorTransform == null || hotspotLabelPrefab == null) return;

            foreach (var hs in _module.hotspots)
            {
                // Instantiate as child of the Vuforia ImageTarget transform.
                // localPosition = target-local offset → tracks with the machine automatically.
                var label = Instantiate(hotspotLabelPrefab, _anchorTransform);
                label.transform.localPosition = hs.LocalPos;
                label.transform.localRotation = Quaternion.identity;
                label.name = $"Label_{hs.component_id}";

                // Set display text
                var tmp = label.GetComponentInChildren<TextMeshPro>();
                if (tmp != null) tmp.text = hs.display_name;

                // Wire the tap button — pass component_id to the quiz handler
                var btn = label.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    string capturedId = hs.component_id;   // closure capture
                    btn.onClick.AddListener(() => OnHotspotTapped(capturedId));
                }

                // Optional: small sphere marker
                if (hotspotMarkerPrefab != null)
                {
                    var marker = Instantiate(hotspotMarkerPrefab, _anchorTransform);
                    marker.transform.localPosition = hs.LocalPos;
                    _spawnedObjects.Add(marker);
                }

                _spawnedObjects.Add(label);
            }
        }

        // ── Quiz logic ─────────────────────────────────────────────

        void BeginQuiz()
        {
            _quizIndex = 0;
            _score     = 0;
            _startTime = Time.time;
            ShowQuestion(_quizIndex);
        }

        void ShowQuestion(int index)
        {
            if (_module == null || index >= _module.questions.Count)
            {
                FinishQuiz();
                return;
            }

            SetPanelActive(quizPanel, true);

            if (quizPromptText != null)
                quizPromptText.text = _module.questions[index].prompt;

            if (scoreText != null)
                scoreText.text = $"{_score} / {_module.questions.Count}";

            if (feedbackText != null)
                feedbackText.text = "";
        }

        /// <summary>
        /// Called by the Button on each hotspot label prefab.
        /// Wire: Button.onClick → ARTrainingModule.OnHotspotTapped(componentId)
        /// (already wired in SpawnHotspotLabels above via AddListener)
        /// </summary>
        public void OnHotspotTapped(string componentId)
        {
            if (_module == null || _quizIndex >= _module.questions.Count) return;

            bool correct = _module.questions[_quizIndex].correct_hotspot_id == componentId;
            if (correct) _score++;

            // Show feedback then advance
            StartCoroutine(ShowFeedbackThenAdvance(correct));
        }

        IEnumerator ShowFeedbackThenAdvance(bool correct)
        {
            if (feedbackText != null)
            {
                feedbackText.text  = correct
                    ? LocalStr("✓ Correct !", "✓ Correct !", "✓ صحيح !")
                    : LocalStr("✗ Try again", "✗ Essaie encore", "✗ حاول مجدداً");
                feedbackText.color = correct ? Color.green : Color.red;
            }

            yield return new WaitForSeconds(feedbackDuration);

            _quizIndex++;
            ShowQuestion(_quizIndex);
        }

        void FinishQuiz()
        {
            SetPanelActive(quizPanel, false);

            int total      = _module.questions.Count;
            int pct        = total > 0 ? Mathf.RoundToInt((float)_score / total * 100) : 0;
            int durationSec = Mathf.RoundToInt(Time.time - _startTime);

            bool passed = pct >= 70;

            // Show result
            if (progressPanel != null && progressText != null)
            {
                SetPanelActive(progressPanel, true);
                string resultLine = passed
                    ? LocalStr($"✓ Certified! {pct}%", $"✓ Certifié(e) ! {pct}%", $"✓ مُعتمَد ! {pct}%")
                    : LocalStr($"✗ {pct}% — retry", $"✗ {pct}% — réessaie", $"✗ {pct}% — أعد المحاولة");
                progressText.text = resultLine;
            }

            Debug.Log($"[Training] {_deviceId} — {_score}/{total} ({pct}%) in {durationSec}s");
            StartCoroutine(SubmitAssessment(pct, durationSec));
        }

        // ── Backend submission ──────────────────────────────────────

        IEnumerator SubmitAssessment(int scorePct, int durationSec)
        {
            var dto = new AssessmentDto
            {
                user_id          = userId,
                device_type      = _module.device_type,
                score_percent    = scorePct,
                duration_seconds = durationSec,
                completed_at     = DateTime.UtcNow.ToString("o")
            };

            string url  = $"{SmartexConfig.Instance.relayBaseUrl}/training/assessments";
            string body = JsonUtility.ToJson(dto);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[Training] Score submit failed: {req.error}");
            else
                Debug.Log($"[Training] Score submitted for {userId}");
        }

        // ── Helpers ────────────────────────────────────────────────

        void ClearSpawnedObjects()
        {
            foreach (var go in _spawnedObjects)
                if (go != null) Destroy(go);
            _spawnedObjects.Clear();
        }

        static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        /// Returns localised string based on current language setting.
        string LocalStr(string en, string fr, string ar) =>
            language switch { Locale.Fr => fr, Locale.Ar => ar, _ => en };

        /// Map device_id prefix → device_type sent to the backend.
        static string DeviceTypeFor(string deviceId) =>
            deviceId.ToUpper() switch
            {
                var s when s.Contains("TEX") => "loom",
                var s when s.Contains("DYE") => "dyer",
                var s when s.Contains("SPN") => "spinner",
                _                            => "loom"
            };

        /// Load bundled JSON from Resources/Training/<deviceType>_<locale>.json
        static TrainingModuleDto LoadFallbackModule(string deviceType, Locale locale)
        {
            string locStr = locale switch { Locale.Fr => "fr", Locale.Ar => "ar", _ => "en" };
            string path   = $"Training/{deviceType}_{locStr}";
            var    asset  = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Training] Fallback JSON not found at Resources/{path}.json");
                return null;
            }
            return JsonUtility.FromJson<TrainingModuleDto>(asset.text);
        }
    }
}
