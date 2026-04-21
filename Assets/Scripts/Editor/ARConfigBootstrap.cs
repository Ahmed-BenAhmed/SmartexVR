#if UNITY_EDITOR
using System.IO;
using Smartex.Core;
using UnityEditor;
using UnityEngine;

namespace Smartex.Editor
{
    /// <summary>
    /// Auto-creates Assets/Resources/ARConfig.asset on first project open (or via
    /// the menu item below). This is the single source of truth for all AR-side
    /// URLs / keys / feature flags — every module binds to ARConfig.Instance
    /// instead of hardcoding anything.
    ///
    /// Why a bootstrap step at all? The ScriptableObject's defaults live in code,
    /// but Unity won't materialise an asset on disk unless someone clicks
    /// "Create → Smartex → AR Config". That's a trap for a 7-person team —
    /// someone pulls the repo, runs the scene, and ARConfig.Instance falls back
    /// to a throwaway in-memory instance whose edits they lose on the next
    /// domain reload. So we create the file eagerly and commit it.
    ///
    /// The first developer to open the project after this lands triggers asset
    /// creation; the asset + .meta get committed; after that, the
    /// InitializeOnLoad check is a no-op.
    /// </summary>
    [InitializeOnLoad]
    public static class ARConfigBootstrap
    {
        const string ResourcesDir = "Assets/Resources";
        const string AssetPath    = "Assets/Resources/ARConfig.asset";

        static ARConfigBootstrap()
        {
            // Defer one editor frame so AssetDatabase is ready during first import.
            EditorApplication.delayCall += EnsureAsset;
        }

        [MenuItem("Smartex VR/Create ARConfig Asset")]
        public static void CreateMenu() => EnsureAsset(force: true);

        static void EnsureAsset() => EnsureAsset(force: false);

        static void EnsureAsset(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<ARConfig>(AssetPath) != null)
                return;

            if (!Directory.Exists(ResourcesDir))
                Directory.CreateDirectory(ResourcesDir);

            var existing = AssetDatabase.LoadAssetAtPath<ARConfig>(AssetPath);
            if (existing != null && !force)
                return;

            var cfg = ScriptableObject.CreateInstance<ARConfig>();
            AssetDatabase.CreateAsset(cfg, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARConfigBootstrap] Created {AssetPath}. Commit it so the team shares defaults.");
        }
    }
}
#endif
