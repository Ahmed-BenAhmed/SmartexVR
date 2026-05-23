#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Smartex.EditorBuildFixes
{
    internal static class UrpPipelineSetup
    {
        private const string UrpFolder = "Assets/Settings/URP";
        private const string UrpAssetPath = "Assets/Settings/URP/SmartexURP.asset";

        [MenuItem("Smartex/Build Fixes/Create + Assign URP Pipeline Asset")]
        public static void CreateAndAssignMenu()
        {
            CreateAndAssign();
            EditorUtility.DisplayDialog("Smartex", "URP Pipeline Asset created/assigned. You can retry Android Build.", "OK");
        }

        // Allow calling from batchmode: -executeMethod Smartex.EditorBuildFixes.UrpPipelineSetup.CreateAndAssign
        public static void CreateAndAssign()
        {
            EnsureFolder(UrpFolder);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(pipeline, UrpAssetPath);

                // Ensure renderer data is persisted as a sub-asset.
                try
                {
                    foreach (var rendererData in pipeline.rendererDataList.ToArray())
                    {
                        if (rendererData == null)
                            continue;

                        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(rendererData)))
                        {
                            AssetDatabase.AddObjectToAsset(rendererData, pipeline);
                        }
                    }
                }
                catch
                {
                    // rendererDataList is a ReadOnlySpan in newer URP; ToArray() above should work,
                    // but we keep this catch to avoid hard failures if URP changes.
                }

                EditorUtility.SetDirty(pipeline);
                AssetDatabase.SaveAssets();
            }

            AssignDefaultRenderPipeline(pipeline);
            AssignQualityRenderPipeline(pipeline);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Smartex] URP Pipeline assigned: {UrpAssetPath}");
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            var parts = assetFolder.Split('/');
            var current = parts[0]; // "Assets"
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void AssignDefaultRenderPipeline(RenderPipelineAsset pipeline)
        {
            // Unity versions differ: try known property names via reflection.
            var gs = typeof(GraphicsSettings);

            var prop = gs.GetProperty("defaultRenderPipeline", BindingFlags.Public | BindingFlags.Static)
                       ?? gs.GetProperty("renderPipelineAsset", BindingFlags.Public | BindingFlags.Static)
                       ?? gs.GetProperty("defaultRenderPipelineAsset", BindingFlags.Public | BindingFlags.Static);

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, pipeline);
                return;
            }

            Debug.LogWarning("[Smartex] Could not find a writable GraphicsSettings render pipeline property. You may need to assign the URP asset manually in Project Settings > Graphics.");
        }

        private static void AssignQualityRenderPipeline(RenderPipelineAsset pipeline)
        {
            // Best-effort: set the pipeline for every quality level (if API exists).
            var qsType = typeof(QualitySettings);
            var prop = qsType.GetProperty("renderPipeline", BindingFlags.Public | BindingFlags.Static);
            if (prop == null || !prop.CanWrite)
                return;

            var qualityNames = QualitySettings.names;
            var current = QualitySettings.GetQualityLevel();

            try
            {
                for (var i = 0; i < qualityNames.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    prop.SetValue(null, pipeline);
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(current, applyExpensiveChanges: false);
            }
        }
    }
}
#endif
