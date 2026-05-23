using UnityEngine;
using Vuforia;
using Smartex.Core;
using System;
using System.Reflection;
using System.Text;

namespace Smartex.AR.Core
{
    /// <summary>
    /// Ensures Vuforia gets a valid license key before any scene loads.
    ///
    /// Vuforia requires the license key to be set very early (before the first scene),
    /// otherwise ImageTarget observers fail to create at runtime.
    ///
    /// License sources (in order):
    /// - Resources/ARConfig_Local.asset (gitignored) or Resources/ARConfig.asset (committed)
    /// - VuforiaConfiguration injected at build-time (Smartex/Build/Build Android APK)
    /// - Environment variables (SMARTEX_VUFORIA_LICENSE_KEY / VUFORIA_LICENSE_KEY) as a last resort
    /// </summary>
    internal static class VuforiaLicenseBootstrap
    {
        private static bool _initializeRequested;

        // Some Vuforia initialization happens very early (before first scene load).
        // We therefore apply the key at the earliest runtime hook, and again before scene load
        // (harmless re-apply) to protect against any late config reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ApplyLicenseKey_SubsystemRegistration()
            => ApplyLicenseKey("SubsystemRegistration");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyLicenseKey_BeforeSceneLoad()
            => ApplyLicenseKey("BeforeSceneLoad");

        // When delayed initialization is enabled, Vuforia will not initialize itself.
        // We explicitly trigger initialization once the first scene has loaded (so VuforiaBehaviour exists).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeVuforia_AfterSceneLoad()
            => EnsureVuforiaInitialized();

        private static void EnsureVuforiaInitialized()
        {
            if (_initializeRequested)
                return;

            try
            {
                var cfg = VuforiaConfiguration.Instance.Vuforia;
                if (!cfg.DelayedInitialization)
                    return;

                var key = cfg.LicenseKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    Debug.LogWarning("[Module A] (AfterSceneLoad) DelayedInitialization is enabled but Vuforia license key is empty; not calling Initialize().");
                    return;
                }
            }
            catch
            {
                // If VuforiaConfiguration isn't available for any reason, do nothing.
                return;
            }

            try
            {
                if (VuforiaApplication.Instance != null && VuforiaApplication.Instance.IsInitialized)
                    return;
            }
            catch
            {
                // ignore
            }

            _initializeRequested = true;
            try
            {
                Debug.Log("[Module A] (AfterSceneLoad) Calling VuforiaApplication.Initialize() (DelayedInitialization=true).");
                VuforiaApplication.Instance.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Module A] (AfterSceneLoad) Could not call VuforiaApplication.Initialize(): {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void ApplyLicenseKey(string phase)
        {
            var key = ResolveLicenseKey(out var source);
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning(
                    $"[Module A] ({phase}) Vuforia license key is empty. " +
                    "Provide it via one of: " +
                    "(1) Assets/Resources/ARConfig_Local.asset (gitignored) field vuforiaLicenseKey, " +
                    "(2) build-time injection (Smartex/Build/Build Android APK), " +
                    "(3) env var SMARTEX_VUFORIA_LICENSE_KEY/VUFORIA_LICENSE_KEY for editor builds.");
                return;
            }

            key = NormalizeLicenseKey(key);

            // Some Vuforia paths may rely on an obfuscated/base64 field (ufoLicenseKey)
            // rather than the plain key. Compute it deterministically from the key.
            var expectedUfo = ComputeUfoLicenseKey(key);

            // 1) Preferred API.
            VuforiaConfiguration.Instance.Vuforia.LicenseKey = key;

            // 2) Defensive: some Vuforia builds read serialized fields directly (or cache early).
            // These field names match what is serialized in Assets/Resources/VuforiaConfiguration.asset.
            try
            {
                var vuforiaSection = VuforiaConfiguration.Instance.Vuforia;
                var sectionType = vuforiaSection.GetType();

                // Set the plain key defensively (some paths read serialized fields directly).
                SetFieldIfPresent(sectionType, vuforiaSection, "vuforiaLicenseKey", key);

                // Align ufoLicenseKey with the provided key (base64 form).
                // If a stale ufoLicenseKey was saved in the asset, native may ignore the plain key.
                SetFieldIfPresent(sectionType, vuforiaSection, "ufoLicenseKey", expectedUfo);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Module A] ({phase}) Could not force-write Vuforia config fields: {ex.GetType().Name}: {ex.Message}");
            }

            // Read back what VuforiaConfiguration exposes, without leaking the full key.
            var readBack = VuforiaConfiguration.Instance.Vuforia.LicenseKey ?? string.Empty;

            // Also inspect internal serialized fields (some native paths read those directly).
            int plainFieldLen = -1;
            int ufoFieldLen = -1;
            bool ufoMatchesExpected = false;
            string ufoPreview = null;
            string expectedUfoPreview = null;
            try
            {
                var vuforiaSection = VuforiaConfiguration.Instance.Vuforia;
                var sectionType = vuforiaSection.GetType();

                plainFieldLen = (GetFieldStringIfPresent(sectionType, vuforiaSection, "vuforiaLicenseKey") ?? string.Empty).Length;
                var ufo = GetFieldStringIfPresent(sectionType, vuforiaSection, "ufoLicenseKey") ?? string.Empty;
                ufoFieldLen = ufo.Length;
                ufoMatchesExpected = !string.IsNullOrEmpty(ufo) && ufo == expectedUfo;
                ufoPreview = KeyPreview(ufo);
                expectedUfoPreview = KeyPreview(expectedUfo);
            }
            catch
            {
                // ignore
            }

            Debug.Log(
                $"[Module A] ({phase}) Applied Vuforia license from {source} " +
                $"(arLen={key.Length}, cfgLen={readBack.Length}, fieldLen={plainFieldLen}, ufoLen={ufoFieldLen}, ufoOk={ufoMatchesExpected}, " +
                $"ar='{KeyPreview(key)}', cfg='{KeyPreview(readBack)}', ufo='{ufoPreview}', ufoExp='{expectedUfoPreview}').");
        }

        private static string ResolveLicenseKey(out string source)
        {
            source = "<none>";

            // 1) Preferred: local override asset (ARConfig_Local) or committed ARConfig.
            var arCfg = ARConfig.Instance;
            var arKey = arCfg != null ? arCfg.vuforiaLicenseKey : null;
            if (!string.IsNullOrWhiteSpace(arKey))
            {
                source = "ARConfig";
                return arKey;
            }

            // 2) If build-time injection already populated VuforiaConfiguration (typical for Android), use it.
            try
            {
                var cfgKey = VuforiaConfiguration.Instance.Vuforia.LicenseKey;
                if (!string.IsNullOrWhiteSpace(cfgKey))
                {
                    source = "VuforiaConfiguration";
                    return cfgKey;
                }
            }
            catch
            {
                // ignore
            }

            // 3) Editor/CI convenience fallback.
            // On Android runtime this is usually null, but it's harmless.
            var env = Environment.GetEnvironmentVariable("SMARTEX_VUFORIA_LICENSE_KEY");
            if (string.IsNullOrWhiteSpace(env))
                env = Environment.GetEnvironmentVariable("VUFORIA_LICENSE_KEY");

            if (!string.IsNullOrWhiteSpace(env))
            {
                source = "env";
                return env;
            }

            return null;
        }

        private static string ComputeUfoLicenseKey(string key)
        {
            // Vuforia serializes an additional license field named ufoLicenseKey.
            // In practice this is a base64 representation of the plain license key.
            // Using UTF8 is safe because the key is ASCII; UTF8 preserves bytes.
            var bytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
            return Convert.ToBase64String(bytes);
        }

        private static string NormalizeLicenseKey(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return raw;

            var trimmed = raw.Trim();

            // Vuforia license keys should be a single continuous token.
            // If the user pasted it with spaces/newlines, remove all whitespace.
            var hasWhitespace = false;
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (char.IsWhiteSpace(trimmed[i]))
                {
                    hasWhitespace = true;
                    break;
                }
            }

            if (!hasWhitespace)
                return trimmed;

            var sb = new StringBuilder(trimmed.Length);
            for (int i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }

            Debug.LogWarning("[Module A] Vuforia license key contained whitespace; it was normalized (whitespace removed)." );
            return sb.ToString();
        }

        private static void SetFieldIfPresent(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(instance, value);
        }

        private static string GetFieldStringIfPresent(Type type, object instance, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(instance) as string : null;
        }

        private static string KeyPreview(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "<empty>";

            var starts = key.Length >= 5 ? key.Substring(0, 5) : key;
            var ends = key.Length >= 5 ? key.Substring(key.Length - 5, 5) : key;
            return $"{starts}…{ends}";
        }
    }
}
