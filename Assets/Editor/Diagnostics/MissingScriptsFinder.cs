#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Smartex.EditorBuildFixes.Diagnostics
{
    internal static class MissingScriptsFinder
    {
        [MenuItem("Smartex/Diagnostics/Find Missing Scripts")]
        public static void FindMissingScripts()
        {
            var results = new List<string>();

            try
            {
                ScanBuildScenes(results);
                ScanAllPrefabs(results);
            }
            finally
            {
                Debug.Log($"[Diagnostics] Missing script scan finished. Issues: {results.Count}");
                if (results.Count > 0)
                {
                    foreach (var r in results)
                        Debug.LogWarning(r);
                }
                else
                {
                    Debug.Log("[Diagnostics] No missing scripts found in build scenes or prefabs.");
                }
            }
        }

        private static void ScanBuildScenes(List<string> results)
        {
            var scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();

            if (scenePaths.Length == 0)
                return;

            var activeScene = SceneManager.GetActiveScene();
            var originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (var path in scenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ScanScene(scene, results);
                }
            }
            finally
            {
                // restore what the user had open
                try
                {
                    if (originalSetup != null && originalSetup.Length > 0)
                        EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                    else if (activeScene.IsValid())
                        EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
                }
                catch
                {
                    // ignore restore errors
                }
            }
        }

        private static void ScanScene(Scene scene, List<string> results)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (var root in scene.GetRootGameObjects())
                ScanGameObject(root, $"Scene: {scene.path}", results);
        }

        private static void ScanAllPrefabs(List<string> results)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
                if (missingCount <= 0)
                    continue;

                results.Add($"[MissingScript] Prefab '{path}' has {missingCount} missing script(s). Open it and remove missing components.");
            }
        }

        private static void ScanGameObject(GameObject go, string context, List<string> results)
        {
            var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missingCount > 0)
            {
                var path = GetHierarchyPath(go.transform);
                results.Add($"[MissingScript] {context} -> '{path}' has {missingCount} missing script(s). Remove missing component(s) in Inspector.");
            }

            foreach (Transform child in go.transform)
                ScanGameObject(child.gameObject, context, results);
        }

        private static string GetHierarchyPath(Transform t)
        {
            var parts = new Stack<string>();
            while (t != null)
            {
                parts.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }
    }
}
#endif
