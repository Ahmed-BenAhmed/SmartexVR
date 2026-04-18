// MODULE E — Remote Expert Assist  (Member 5)
// Owner   : assign to member 5
// Purpose : Technician streams AR camera via WebRTC.
//           Remote expert draws annotations → appear in technician AR view.
//           IEIA agent recommendation shown as floating text.
//
// Architecture:
//   Technician device  ──WebRTC──►  Relay server  ──WebRTC──►  Expert browser
//                      ◄──WS annotations──          ◄──WS annotations──
//
// Backend endpoints to add to smartex-agent-v2/backend/main.py:
//   POST /sessions                          → create session, returns session_id
//   WebSocket /ws/ar-session/{session_id}   → bidirectional annotation stream
//   GET  /sessions/{id}/recording           → playback URL
//
// Annotation message schema (JSON over WebSocket):
//   { "type": "annotation",
//     "world_pos": {"x":1.2,"y":0.5,"z":0.3},
//     "color":     "#FF0000",
//     "text":      "Check belt tension here",
//     "author":    "remote_expert" }
//
// Recommended WebRTC package: com.unity.webrtc (add to manifest.json when ready)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Smartex.AR.RemoteAssist
{
    [Serializable]
    public class AnnotationMessage
    {
        public string type;
        public Vector3 world_pos;
        public string color;
        public string text;
        public string author;
    }

    /// <summary>
    /// Manages one remote assist session: creates it on the backend,
    /// opens the WebSocket annotation channel, and spawns AR annotation
    /// objects when expert messages arrive.
    /// </summary>
    public class ARRemoteSession : MonoBehaviour
    {
        [Header("Session")]
        public string sessionId;     // filled after StartSession()

        [Header("Annotation prefab")]
        public GameObject annotationPrefab;   // world-space label + arrow

        [Header("IEIA Agent Panel")]
        public GameObject agentRecommendationPanel;
        public TMPro.TextMeshProUGUI agentRecommendationText;

        private bool _connected = false;

        public IEnumerator StartSession(string deviceId)
        {
            string url  = $"{Smartex.Core.SmartexConfig.Instance.relayBaseUrl}/sessions";
            string body = JsonUtility.ToJson(new { device_id = deviceId });
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            { Debug.LogError($"[RemoteAssist] Session create failed: {req.error}"); yield break; }

            // TODO Member 5: parse session_id from response, then open WebSocket
            // sessionId = JsonUtility.FromJson<SessionResponse>(req.downloadHandler.text).session_id;
            // ConnectWebSocket();
            Debug.Log("[RemoteAssist] Session created. WebSocket TODO.");
        }

        // TODO Member 5: implement ConnectWebSocket() using ClientWebSocket or
        // a Unity WebSocket asset. On message → call SpawnAnnotation(msg).

        void SpawnAnnotation(AnnotationMessage msg)
        {
            if (annotationPrefab == null) return;
            var go = Instantiate(annotationPrefab, msg.world_pos, Quaternion.identity);
            var label = go.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label != null) label.text = $"{msg.author}: {msg.text}";
            // TODO Member 5: parse msg.color string → set label/arrow color
        }

        public void ShowAgentRecommendation(string text)
        {
            if (agentRecommendationPanel != null) agentRecommendationPanel.SetActive(true);
            if (agentRecommendationText  != null) agentRecommendationText.text = text;
        }
    }
}
