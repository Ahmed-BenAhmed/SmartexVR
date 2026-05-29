#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Smartex.Editor
{
    [InitializeOnLoad]
    public static class URPPipelineBootstrap
    {
        const string SettingsDir       = "Assets/Settings";
        const string RendererAssetPath = "Assets/Settings/URP-Quest-Renderer.asset";
        const string PipelineAssetPath = "Assets/Settings/URP-Quest.asset";

        static URPPipelineBootstrap()
        {
            EditorApplication.delayCall += EnsurePipeline;
        }

        [MenuItem("Smartex VR/Create & Assign URP Pipeline")]
        public static void CreateMenu() => EnsurePipeline(force: true);

        static void EnsurePipeline() => EnsurePipeline(force: false);

        static void EnsurePipeline(bool force)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (!force && pipeline != null && GraphicsSettings.defaultRenderPipeline == pipeline)
                return;

            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            }

            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = pipeline;

            int qualityCount = QualitySettings.names.Length;
            int prev = QualitySettings.GetQualityLevel();
            for (int i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(prev, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[URPPipelineBootstrap] Created and assigned {PipelineAssetPath} as Default Render Pipeline + per-quality-tier. Commit the new assets.");
        }
    }
}
#endif
