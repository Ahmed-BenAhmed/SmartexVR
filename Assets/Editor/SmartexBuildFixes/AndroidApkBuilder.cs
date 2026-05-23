#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Vuforia;
using Smartex.Core;

namespace Smartex.EditorBuildFixes
{
    internal static class AndroidApkBuilder
    {
        private const string EditorPrefsLicenseKey = "Smartex.VuforiaLicenseKey";

        [MenuItem("Smartex/Build/Build Android APK")]
        [MenuItem("Smartex VR/Build/Build Android APK")]
        public static void BuildApkMenu()
        {
            var outputPath = GetDefaultOutputPath();
            BuildApk(outputPath);
            EditorUtility.RevealInFinder(Path.GetDirectoryName(outputPath));
        }

        [MenuItem("Smartex/Build/Set Vuforia License (Local)")]
        [MenuItem("Smartex VR/Build/Set Vuforia License (Local)")]
        public static void OpenSetVuforiaLicenseWindow()
        {
            VuforiaLicenseWindow.Open();
        }

        [MenuItem("Smartex/Build/Clear Vuforia License Cache (Project)")]
        [MenuItem("Smartex VR/Build/Clear Vuforia License Cache (Project)")]
        public static void ClearVuforiaLicenseCacheInProject()
        {
            try
            {
                var vuforiaCfg = VuforiaConfiguration.Instance;
                var section = vuforiaCfg.Vuforia;

                section.LicenseKey = string.Empty;
                SetFieldIfPresent(section, "vuforiaLicenseKey", string.Empty);
                SetFieldIfPresent(section, "ufoLicenseKey", string.Empty);

                EditorUtility.SetDirty(vuforiaCfg);
                AssetDatabase.SaveAssets();

                Debug.Log("[Smartex] Cleared Vuforia license fields in VuforiaConfiguration.asset (project cache). ");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Smartex] Could not clear VuforiaConfiguration license fields: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Batchmode: -executeMethod Smartex.EditorBuildFixes.AndroidApkBuilder.BuildApkBatch
        public static void BuildApkBatch()
        {
            BuildApk(GetDefaultOutputPath());
        }

        private static string GetDefaultOutputPath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;
            var outDir = Path.Combine(projectRoot, "Builds", "Android");
            Directory.CreateDirectory(outDir);
            return Path.Combine(outDir, "SmartexAR.apk");
        }

        private static void BuildApk(string outputPath)
        {
            EditorUserBuildSettings.buildAppBundle = false; // APK

            // Defensive: ensure Vuforia sees the license key during native init.
            // Some player builds can ignore late runtime assignment, so we apply it here pre-build.
            using var _ = new VuforiaLicenseKeyBuildScope();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes in Build Settings.");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[Smartex] Building Android APK to: {outputPath}");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Android build failed: {report.summary.result} (errors: {report.summary.totalErrors})");

            Debug.Log($"[Smartex] Android build succeeded. APK: {outputPath}");
        }

        internal sealed class VuforiaLicenseKeyBuildScope : IDisposable
        {
            private readonly string _prevPlain;
            private readonly string _prevUfo;
            private readonly string _prevField;
            private readonly bool _didApply;

            public VuforiaLicenseKeyBuildScope()
            {
                try
                {
                    if (!TryGetLicenseKey(out var key, out var source) || string.IsNullOrWhiteSpace(key))
                    {
                        Debug.LogWarning(
                            "[Smartex] No Vuforia license key found. Provide it via one of: " +
                            "(1) Smartex/Build/Set Vuforia License (Local) (stored in EditorPrefs), " +
                            "(2) env var SMARTEX_VUFORIA_LICENSE_KEY/VUFORIA_LICENSE_KEY, " +
                            "(3) ARConfig (not recommended to commit). Vuforia may fail on device.");
                        return;
                    }

                    key = NormalizeLicenseKey(key);
                    var ufo = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

                    var vuforiaCfg = VuforiaConfiguration.Instance;
                    var section = vuforiaCfg.Vuforia;

                    // Snapshot current values (plain + obfuscated).
                    _prevPlain = section.LicenseKey;
                    _prevUfo = GetFieldIfPresent<string>(section, "ufoLicenseKey") ?? string.Empty;
                    _prevField = GetFieldIfPresent<string>(section, "vuforiaLicenseKey") ?? string.Empty;

                    // Apply plain key, clear obfuscated cache so build-time tasks can regenerate it.
                    section.LicenseKey = key;
                    SetFieldIfPresent(section, "vuforiaLicenseKey", key);
                    SetFieldIfPresent(section, "ufoLicenseKey", ufo);

                    EditorUtility.SetDirty(vuforiaCfg);
                    AssetDatabase.SaveAssets();

                    _didApply = true;
                    Debug.Log($"[Smartex] Applied Vuforia license key to VuforiaConfiguration for build (source={source}).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Smartex] Could not apply Vuforia license pre-build: {ex.GetType().Name}: {ex.Message}");
                }
            }

            private static string NormalizeLicenseKey(string raw)
            {
                if (string.IsNullOrEmpty(raw))
                    return raw;

                var sb = new StringBuilder(raw.Length);
                for (int i = 0; i < raw.Length; i++)
                {
                    var c = raw[i];
                    if (!char.IsWhiteSpace(c))
                        sb.Append(c);
                }

                return sb.ToString();
            }

            public void Dispose()
            {
                if (!_didApply)
                    return;

                try
                {
                    var vuforiaCfg = VuforiaConfiguration.Instance;
                    var section = vuforiaCfg.Vuforia;

                    section.LicenseKey = _prevPlain ?? string.Empty;
                    SetFieldIfPresent(section, "vuforiaLicenseKey", _prevField ?? string.Empty);
                    SetFieldIfPresent(section, "ufoLicenseKey", _prevUfo ?? string.Empty);

                    EditorUtility.SetDirty(vuforiaCfg);
                    AssetDatabase.SaveAssets();

                    Debug.Log("[Smartex] Restored VuforiaConfiguration license fields after build.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Smartex] Could not restore VuforiaConfiguration after build: {ex.GetType().Name}: {ex.Message}");
                }
            }

            private static T GetFieldIfPresent<T>(object instance, string fieldName) where T : class
            {
                var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null ? field.GetValue(instance) as T : null;
            }

            private static void SetFieldIfPresent(object instance, string fieldName, object value)
            {
                var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(instance, value);
            }
        }

        private static bool TryGetLicenseKey(out string key, out string source)
        {
            key = null;
            source = "<none>";

            // 1) Local per-machine storage (no files in the repo).
            var prefsKey = EditorPrefs.GetString(EditorPrefsLicenseKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(prefsKey))
            {
                key = prefsKey;
                source = "EditorPrefs";
                return true;
            }

            // 2) ARConfig (kept empty in repo; can be used temporarily if you really want).
            var arCfg = ARConfig.Instance;
            var arKey = arCfg != null ? arCfg.vuforiaLicenseKey : null;
            if (!string.IsNullOrWhiteSpace(arKey))
            {
                key = arKey;
                source = "ARConfig";
                return true;
            }

            // 3) Environment variables (CI-friendly).
            var env = Environment.GetEnvironmentVariable("SMARTEX_VUFORIA_LICENSE_KEY");
            if (string.IsNullOrWhiteSpace(env))
                env = Environment.GetEnvironmentVariable("VUFORIA_LICENSE_KEY");

            if (!string.IsNullOrWhiteSpace(env))
            {
                key = env;
                source = "env";
                return true;
            }

            return false;
        }

        private static void SetFieldIfPresent(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(instance, value);
        }

        private sealed class VuforiaLicenseWindow : EditorWindow
        {
            private string _key;

            public static void Open()
            {
                var w = GetWindow<VuforiaLicenseWindow>(true, "Smartex Vuforia License", true);
                w.minSize = new Vector2(520, 180);
                w._key = EditorPrefs.GetString(EditorPrefsLicenseKey, string.Empty);
                w.ShowUtility();
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField("Local Vuforia license key (stored in EditorPrefs, not in the project)", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(8);

                EditorGUILayout.LabelField("License key", EditorStyles.boldLabel);
                _key = EditorGUILayout.TextArea(_key, GUILayout.Height(70));

                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save"))
                    {
                        var normalized = NormalizeKey(_key);
                        EditorPrefs.SetString(EditorPrefsLicenseKey, normalized ?? string.Empty);
                        _key = normalized ?? string.Empty;
                        Debug.Log("[Smartex] Saved Vuforia license key to EditorPrefs (local). ");
                    }

                    if (GUILayout.Button("Clear"))
                    {
                        EditorPrefs.DeleteKey(EditorPrefsLicenseKey);
                        _key = string.Empty;
                        Debug.Log("[Smartex] Cleared Vuforia license key from EditorPrefs (local). ");
                    }

                    if (GUILayout.Button("Close"))
                    {
                        Close();
                    }
                }

                var stored = EditorPrefs.GetString(EditorPrefsLicenseKey, string.Empty);
                var len = string.IsNullOrEmpty(stored) ? 0 : stored.Length;
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox($"Stored key length: {len}", MessageType.Info);
            }

            private static string NormalizeKey(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return string.Empty;

                var sb = new StringBuilder(raw.Length);
                for (int i = 0; i < raw.Length; i++)
                {
                    var c = raw[i];
                    if (!char.IsWhiteSpace(c))
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Automatically injects the Vuforia license into VuforiaConfiguration during Android builds,
    /// even if the user builds via Unity's default Build button.
    /// The project asset is restored after the build.
    /// </summary>
    internal sealed class VuforiaLicenseBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static AndroidApkBuilder.VuforiaLicenseKeyBuildScope _scope;

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            _scope?.Dispose();
            _scope = new AndroidApkBuilder.VuforiaLicenseKeyBuildScope();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            _scope?.Dispose();
            _scope = null;
        }
    }
}
#endif
