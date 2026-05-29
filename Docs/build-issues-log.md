# Build Issues & Fixes Log

All issues encountered during Android and VR headset builds, with root causes and fixes.

---

## Unity Compilation / Assembly Definition

### Issue 1 — CS0246 TMPro + AR assemblies couldn't see VR types
**Commit:** `658f8e4`

AR `.asmdef` files were implicitly depending on `Assembly-CSharp` (the default implicit assembly) instead of named assemblies. This broke cross-module type resolution.

Additionally, `FactoryBuilder.cs` crashed when removing a Camera or Light component because URP marks `UniversalAdditionalCameraData` / `LightData` as required dependencies — Unity refuses to delete the base component while those are still attached.

**Fix:**
- Created `Smartex.Core.asmdef`, `Smartex.Game.asmdef`, and `Smartex.Editor.asmdef`.
- Updated all 7 AR `.asmdef` files to reference those assemblies by name.
- Added `Unity.TextMeshPro` to the Recognition asmdef.
- In `FactoryBuilder.cs`: destroy the URP data components *before* removing Camera/Light.

---

### Issue 2 — `Unity.XR.CoreUtils` not transitively exposed + missing Editor references
**Commit:** `94f5009`

`ARTrackablesChangedEventArgs` uses `ReadOnlyList<>` from `Unity.XR.CoreUtils`, which AR Foundation does not re-export transitively. Every AR asmdef needed to declare it explicitly.

`Smartex.Editor.asmdef` was also missing `Unity.TextMeshPro`, `Unity.InputSystem`, and `Unity.EditorCoroutines.Editor` — all used by `SmartexSceneBuilder` and `SmartexConfigEditor`.

**Fix:**
- Added `Unity.XR.CoreUtils` to every AR asmdef.
- Added `Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.EditorCoroutines.Editor` to `Smartex.Editor.asmdef`.
- Suppressed `CS0067` on `OnAlertReceived` (unused event reserved for Wave 2 alert streaming).

---

### Issue 3 — EditMode tests couldn't resolve the Mocks namespace
**Commit:** `5dcf150`

The test asmdef only referenced `Smartex.AR.Contracts` (interfaces + DTOs). The MonoBehaviour mocks live in a separate `Smartex.AR.Contracts.Mocks` assembly under `Contracts/Mocks/`. Missing that reference caused the entire test DLL to fail compilation.

**Fix:** Added `Smartex.AR.Contracts.Mocks` to the EditMode test asmdef references.

---

## CI / GitHub Actions Android Build

### Issue 4 — `androidAppBundle: false` invalid input
**Commit:** `a760e4f`

`unity-builder@v4` removed the `androidAppBundle` input. v4 uses `androidExportType` (already set to `androidPackage`). The leftover key triggered an "Unexpected input(s)" warning on every run.

**Fix:** Removed the redundant `androidAppBundle: false` line from the workflow.

---

### Issue 5 — Runner out of disk space (ENOSPC)
**Commit:** `a760e4f`

`ubuntu-latest` runners have ~14 GB free. The Unity Android editor Docker image alone is ~10 GB, and the build itself needs additional space on top. The `docker pull` was dying mid-layer.

**Fix:** Added `jlumbroso/free-disk-space` as a pre-build step. It strips dotnet / haskell / swift / android-sdk (none used by Unity) and reclaims ~30 GB before the build starts. Applied to both the build-android and test jobs.

---

### Issue 6 — Unity 6 LicensingClient error 20110 "serial invalid"
**Commits:** `c0583c9` → `b8d8754` → `fdd5e4a` → `75ef59d`

`unity-builder@v4` defaults `UNITY_SERIAL` to the placeholder string `'x'`. On Unity 6's LicensingClient, even after a successful Personal entitlement activation, the client re-validates any `UNITY_SERIAL` it finds. It treats `'x'` as a malformed Pro serial and aborts with error 20110 — even though the Personal license is already active.

Three failed workaround attempts:

1. **Force `UNITY_SERIAL` to empty string (`c0583c9`)** — didn't work. unity-builder's entrypoint doesn't honor empty-string env overrides; it still exported `UNITY_SERIAL=x`.

2. **`skipActivation: true` + `preBuildSteps` to pre-seed the license XML (`b8d8754`)** — didn't work. `preBuildSteps` silently no-ops on local Docker runners (it's a cloud-only feature).

3. **Write license to host path before container starts (`fdd5e4a`)** — wrote `UnityEntitlementLicense.xml` to `$RUNNER_TEMP/_github_home/.local/share/unity3d/Unity/licenses/`, which is bind-mounted to `/root` inside the container. Combined with `skipActivation: true`. Bypassed the broken activation path, but ultimately still not reliable.

**Root cause:** Unity Personal entitlements cannot be activated in ephemeral Linux containers at all. The LicensingClient requires a live interactive access token that GitHub-hosted runners do not have.

**Final fix (`75ef59d`):** Moved Android and iOS builds to **Unity Build Automation** (cloud.unity.com), which provisions Personal licenses natively. GitHub Actions now only runs tests and secrets-guard jobs.

---

### Issue 7 — Generic Android bundle ID rejected
**Commit:** `ae0e1e3`

The default `com.DefaultCompany.SmartexVR` bundle ID is invalid for Unity Build Automation and doesn't match the school's reverse-domain convention.

**Fix:** Set the Android package name to `ma.ensa.smartexvr` in Player Settings.

---

## VR Headset (Quest 2) Deployment

### Known issues and fixes at deploy time

| Symptom | Root cause | Fix |
|---|---|---|
| Black screen when app launches | Vulkan driver crash on some Quest firmware | Player Settings → remove Vulkan, keep OpenGLES3 only, rebuild |
| Flat 2D view / "No XR device found" | OpenXR loader not enabled for Android | XR Plug-in Management → Android tab → check OpenXR |
| Quest not in "Run Device" dropdown | ADB doesn't see the device | Cable is charge-only OR Developer Mode is off on the headset |
| "Allow USB debugging?" prompt never appeared | Charge-only USB-C cable | Swap to a data-capable USB-C cable |
| `[InfluxDBClient] HTTP 401` | Wrong InfluxDB token | Token must be `smartex-dev-token-change-me` in `SmartexConfig.cs` or the `Resources/` asset |
| `HTTP 0` / cannot resolve host | Headset not on WiFi or network blocks the domain | Connect headset to WiFi; verify `https://influxdb.smartex.ahmedbenahmed.com/health` is reachable from the headset browser |
| Choppy FPS | 8 real-time point lights + URP shadow maps | Disable shadows on machine prefabs for the demo |
| Unity crashes on Build And Run | Gradle JVM out of memory | Enable Custom Gradle Properties Template; bump heap in `gradleTemplate.properties` |

See also [vr-quest2-runbook.md](vr-quest2-runbook.md) for the full step-by-step setup guide.
