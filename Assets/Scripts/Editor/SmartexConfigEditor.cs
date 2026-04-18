#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Smartex.Core;

namespace Smartex.Editor
{
    [CustomEditor(typeof(SmartexConfig))]
    public class SmartexConfigEditor : UnityEditor.Editor
    {
        private string _testResult  = "";
        private Color  _resultColor = Color.white;
        private bool   _testing     = false;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var cfg = (SmartexConfig)target;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("-- Connection Tools --", EditorStyles.boldLabel);
            GUI.enabled = !_testing;
            if (GUILayout.Button("Test Relay Connection", GUILayout.Height(28)))
            {
                _testResult = "Testing..."; _resultColor = Color.yellow; _testing = true;
                EditorCoroutineUtility.StartCoroutine(TestRelay(cfg), this);
            }
            if (GUILayout.Button("Test InfluxDB Direct", GUILayout.Height(28)))
            {
                _testResult = "Testing..."; _resultColor = Color.yellow; _testing = true;
                EditorCoroutineUtility.StartCoroutine(TestInflux(cfg), this);
            }
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(_testResult))
            {
                var style = new GUIStyle(EditorStyles.helpBox);
                style.normal.textColor = _resultColor;
                EditorGUILayout.LabelField(_testResult, style);
            }
        }

        IEnumerator TestRelay(SmartexConfig cfg)
        {
            using var req = UnityWebRequest.Get(cfg.relayBaseUrl + "/health");
            req.timeout = 6;
            yield return req.SendWebRequest();
            _testResult  = req.result == UnityWebRequest.Result.Success
                ? "Relay OK - " + req.downloadHandler.text
                : "Relay FAILED - " + req.error;
            _resultColor = req.result == UnityWebRequest.Result.Success ? Color.green : Color.red;
            _testing = false; Repaint();
        }

        IEnumerator TestInflux(SmartexConfig cfg)
        {
            using var req = UnityWebRequest.Get(cfg.influxUrl + "/health");
            req.SetRequestHeader("Authorization", "Token " + cfg.influxToken);
            req.timeout = 6;
            yield return req.SendWebRequest();
            _testResult  = req.result == UnityWebRequest.Result.Success
                ? "InfluxDB OK - " + req.downloadHandler.text
                : "InfluxDB FAILED - " + req.error;
            _resultColor = req.result == UnityWebRequest.Result.Success ? Color.green : Color.red;
            _testing = false; Repaint();
        }
    }
}
#endif
