# Quest 2 VR session — runbook

Everything you need to get the factory floor running on an Oculus Quest 2 for
tomorrow's school session. Follow top-to-bottom — should take **20–30 minutes**
from a fresh `git pull` to "headset on, looking at live data."

## What you have going in

- SmartexVR Unity project (Unity 6000.3.11f1, URP) already builds the 8-machine
  factory scene with live data from InfluxDB on desktop.
- **InfluxDB URL is already updated** to `https://influxdb.smartex.ahmedbenahmed.com`
  (committed). Verified live with 8 machines returning data.
- **XR packages added to `Packages/manifest.json`** — Unity will fetch OpenXR
  + XR Interaction Toolkit automatically the first time you open the project.
- **`Smartex VR → Convert Scene to VR (Quest 2)` menu** is ready to use.

## What you need at school

- Oculus Quest 2 + USB-C cable (data-capable — check this, some are charge-only)
- The headset must already have **Developer Mode enabled** (Meta Horizon app on
  a phone → your headset → Developer Mode → ON). If not, enable it as a
  developer at https://developer.oculus.com first, then toggle.
- Laptop with Unity 6000.3.11f1 + Android Build Support + OpenJDK + Android SDK
  & NDK modules
- WiFi the headset can reach (it needs HTTPS out to
  `influxdb.smartex.ahmedbenahmed.com` on 443)

## Step 1 — Pull the repo, let packages resolve (5 min)

```
git pull
# Open SmartexVR in Unity Hub → it will auto-import + fetch XR packages
# Wait for Package Manager spinner to stop (console: no red errors)
```

If the console shows red errors about missing `UnityEngine.XR.OpenXR`, wait
another minute — sometimes resolution takes two passes.

## Step 2 — Enable OpenXR for Android (5 min, one-time per machine)

**Edit → Project Settings → XR Plug-in Management**

1. Click the **Android tab** (the little robot icon).
2. If it says "Install XR Plug-in Management", click it and wait.
3. Check ✅ **OpenXR**. Uncheck ARCore if it's checked — they fight.
4. A yellow triangle appears next to OpenXR → click it → "Fix All" → "Fix".
5. In the left panel under **XR Plug-in Management**, click **OpenXR**.
6. Android tab → under **Interaction Profiles**, click `+` → add **Meta Quest
   Touch Pro Controller Profile** (or "Oculus Touch Controller Profile" — either
   works for Quest 2).
7. Under **OpenXR Feature Groups** → check ✅ **Meta Quest Support**.

## Step 3 — Switch build target to Android (1 min)

**File → Build Profiles → Android → Switch Platform**

(If the Android platform is greyed out, you forgot the Android Build Support
module in Unity Hub — install it, relaunch.)

Then in **Player Settings → Other Settings**:

- **Color Space** = Linear (usually already set by URP)
- **Graphics APIs** = OpenGLES3 ONLY (remove Vulkan — some Quest drivers crash)
- **Scripting Backend** = IL2CPP
- **Target Architectures** = ARM64 only
- **Minimum API Level** = Android 10.0 (API 29) — Quest 2 needs this
- **Target API Level** = Automatic (highest installed)

## Step 4 — Convert the scene to VR (15 seconds)

Open the factory scene (`Assets/scene.unity` or `scene1.unity`).

**Menu: Smartex VR → Convert Scene to VR (Quest 2)**

This:

- Disables the desktop `CameraController` (WASD/orbit)
- Disables the old Main Camera
- Creates an `XR Origin (VR)` with a head-tracked camera at 1.6 m eye height
- Leaves everything else untouched (DataManager, machines, UI)

If it pops up a "VR packages not ready" dialog, wait for Package Manager to
finish and retry.

To undo: **Smartex VR → Revert Scene to Desktop**.

## Step 5 — Test in the Editor without the headset (optional, 30 s)

Press **Play**. The view renders twice (left + right eye). If you see two
warped views of the factory floor, rendering is configured correctly — stop.
If the console logs `[InfluxDBClient] Parsed 8 machines …` you also have
live data. If not, check the console and re-read §3 below.

## Step 6 — Build and run to the headset (5 min first time)

1. Plug the Quest into the laptop via USB-C.
2. Put the headset on → you'll see "Allow USB debugging?" → **Always allow**
   → OK. (If you don't see this, the cable is charge-only — swap cables.)
3. In Unity: **File → Build Profiles → Android → Run Device → Refresh** →
   pick your Quest in the dropdown.
4. Click **Build And Run**.
5. First build is 3–5 min. You'll see a progress bar.
6. When it finishes, the app launches automatically on the headset. Put it
   on — you should be standing in the factory, 1.6 m tall, with 8 machines
   arranged around you.

## Step 7 — Run it stand-alone (so you can walk around untethered)

After the first Build And Run, the APK is installed on the Quest. Unplug.
Open the headset → **Apps** → top-right filter → **Unknown Sources** → you'll
see `com.DefaultCompany.SmartexVR` (or similar) → launch. You can now walk
around within your Guardian boundary and see the factory.

## What you should see working tomorrow

- ✅ You're standing on the factory floor
- ✅ 8 textile machines (Loom 1..8) positioned in a 4×2 grid roughly 6–7 m
  apart, color-coded by health (green/orange/red)
- ✅ Data refreshes every 5 s (watch `avg_power_watts` tick)
- ✅ You can physically walk up to a machine (Guardian permitting)
- ✅ Head tracking is smooth, 72 / 90 Hz depending on Quest settings

## What likely WON'T work yet (don't panic)

- ❌ **Controller interaction.** No grab, no teleport, no ray-cast for machine
  info. The `CameraController`'s desktop input (RMB / scroll / 1-8 jump) is
  disabled in VR. Adding controller interaction is post-session work.
- ❌ **Machine detail panels on click.** `MachineClickHandler` uses mouse
  raycasts; those don't fire in VR. Same fix path as above.
- ❌ **Teleport locomotion.** The XRI toolkit has this but we haven't wired
  it. For tomorrow, walking + Guardian is fine.

If the class asks "can we click a machine?" — honest answer: "Not yet. The
rig is minimal for today. Next session: controllers + teleport + detail
panels." (That's genuinely a small-ish follow-up — a teammate can pick that
up as Module C's first real deliverable.)

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Black screen when app launches on headset | Graphics API mismatch | Player Settings → remove Vulkan, keep only OpenGLES3, rebuild |
| "No XR device found" / flat 2D view on Quest | OpenXR loader not enabled for Android | Project Settings → XR Plug-in Management → Android tab → check OpenXR |
| `[InfluxDBClient] HTTP 401` | Wrong token | Token should be `smartex-dev-token-change-me`. Check `SmartexConfig.cs` default or the asset in `Resources/` |
| No data, `HTTP 0` / `Cannot resolve host` | Headset not on WiFi, or WiFi blocks the domain | Headset Settings → WiFi → connect. Test by opening the browser inside the headset and hitting `https://influxdb.smartex.ahmedbenahmed.com/health` — should show a tiny JSON blob |
| Empty factory, no machines | DataManager didn't get a snapshot | Check console — `InfluxDBClient` logs explain why. If `HTTP 200` but "Parsed 0 machines", the schema changed; run the curl in the doc to confirm |
| FPS choppy | URP + forward lights + 8 real-time point lights | Turn off shadows on the machine prefabs for the demo |
| Unity crashes on Build And Run | Gradle OOM or misconfigured JDK | Player Settings → Publishing → Custom Gradle Properties Template (should already be on from `AR_TP2` pattern); bump heap in `gradleTemplate.properties` |
| Quest not listed in "Run Device" dropdown | adb doesn't see it | Run `adb devices` from the terminal in Unity's installed Android SDK path; if empty, the cable is charge-only or Developer Mode is off |

## Post-session follow-ups (nice-to-have, not blocking tomorrow)

1. **Controllers + ray interaction** — drop an XR Interaction Toolkit
   `XR Origin (VR) + Controllers` prefab instead of the minimal rig; add
   `XR Ray Interactor` to each controller; add an `XR Simple Interactable` to
   each machine with `Select Entered` wired to the existing
   `MachineClickHandler.OpenDetail(...)`.
2. **Teleport locomotion** — XRI sample has a one-file setup.
3. **Floating info HUD** — a world-space Canvas locked to the headset that
   shows total factory kW + CO₂ at all times.
4. **Performance pass** — turn off per-pixel lighting on distant machines,
   disable URP shadows for the demo.

---

*Prepared for the ENSA Berrechid session — test early, don't troubleshoot in
front of the class.*
