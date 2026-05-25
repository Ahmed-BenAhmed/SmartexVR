# SmartexVR + AR
**Industrial IoT Digital Twin & Augmented Reality — Moroccan Textile Factory**

Unity 6 · Vuforia target recognition · InfluxDB · ESP32 · FastAPI backend · 7-member team

---

## What Is This Project?

Eight Jacquard looms in a Moroccan textile factory each carry an **ESP32 microcontroller** that measures power consumption, vibration, dye temperature, and fabric tension every few seconds. That data flows into an **InfluxDB** time-series database on a cloud server.

This Unity project visualises that data in two ways:

| Mode | How you see it | Platform |
|------|---------------|----------|
| **VR Digital Twin** | Walk through a 3D replica of the factory, see each machine's health in real time | PC / VR headset |
| **AR Overlay** *(your work)* | Point your phone at a real loom → see its live sensor data floating above it | Android / iOS |

The two modes share **exactly the same data pipeline** (`DataManager` → `OnSnapshotUpdated`). AR module work should consume the backend contracts instead of calling InfluxDB or AI services directly.

Backend note: `SmartexVR/backend/` now provides the `/snapshot` relay plus maintenance, training, remote assist, anomaly, and AI-assist APIs. Recognition target: **Vuforia**. Some older AR Foundation scaffolding remains in the repo and should be replaced behind the stable `IMachineRecognizer`/`RecognizedMachine.AnchorTransform` contract during the Unity pass.

---

## Current State — What Is Already Done

```
✅ DataManager        polls backend `/snapshot` first, then falls back to InfluxDB direct
✅ MachineController  receives snapshot, updates 3D body colour + health ring + energy bar
✅ HealthAura         pulsing disc on the floor — green (healthy) → red (critical)
✅ EnergyBar          vertical pole showing power consumption in real time
✅ MachineDetailPanel slide-in panel with full sensor readings + CBAM cost calculator
✅ FactoryBuilder     procedural 3D factory scene with 8 auto-scaled FBX looms
✅ AR scaffold        7 stub scripts, one per module, with TODOs and wiring comments
✅ Assembly defs      each module compiles independently — no accidental cross-deps
✅ Backend            FastAPI service in backend/ with mock telemetry, analytics, sessions, and AI assist
⚠️ Vuforia wiring     Target-recognition Unity setup still needs a Unity machine
```

**You are building the AR layer.** The data is already flowing. Your job is to make it visible on a phone.

---

## Project Structure

```
Assets/Scripts/
├── Core/               ← DATA LAYER — do not modify
│   ├── DataManager.cs          polls InfluxDB, fires events
│   ├── InfluxDBClient.cs       parses annotated CSV from InfluxDB
│   ├── SmartexConfig.cs        all tunable settings (colours, thresholds, URLs)
│   └── Models/
│       └── MachineData.cs      one instance per loom — your data source
│
├── Machines/           ← VR VISUALS — reference only, do not modify
│   ├── MachineController.cs    subscribes to snapshots, drives 3D visuals
│   ├── HealthAura.cs           pulsing floor disc
│   └── EnergyBar.cs            power bar pole
│
├── UI/                 ← SHARED UI — you can call these from AR
│   └── MachineDetailPanel.cs   call .OpenById("ESP32_TEX_003") from AR overlay
│
├── Editor/             ← EDITOR TOOLS — ignore unless you are Member 7
│
└── AR/                 ← YOUR WORK — one folder per member
    ├── Core/           Member 1 — ARSessionManager.cs
    ├── Recognition/    Member 2 — MachineQRTracker.cs, ManualMachineSelector.cs
    ├── Overlay/        Member 3 — MachineAROverlaySpawner.cs, MachineARPanel.cs, BillboardFacer.cs
    ├── Maintenance/    Member 4 — ARMaintenanceGuide.cs
    ├── RemoteAssist/   Member 5 — ARRemoteSession.cs
    ├── Training/       Member 6 — ARTrainingModule.cs
    └── QA/             Member 7 — ARPerformanceProfiler.cs
```

---

## Getting Started

### First-time setup (everyone)

1. **Clone the repo**
   ```
   git clone https://github.com/Ahmed-BenAhmed/SmartexVR.git
   ```

2. **Open in Unity Hub** → Unity **6000.3.11f1** (exact version — use Unity Hub to install if missing)

3. **Let Unity import** — first open takes ~5 min while it compiles packages. AR Foundation packages download automatically.

4. **Enable AR backends** (once per machine):
   - `Edit → Project Settings → XR Plug-in Management`
   - ✅ Android tab → **ARCore**
   - ✅ iOS tab → **ARKit**

5. **Open the VR scene first** to see what you're augmenting:
   `Assets/Scenes/SmartexVR.unity` → press Play → you should see 8 coloured looms updating live.

6. **Create your branch** before writing any code:
   ```
   git checkout -b feature/ar-member-N-module-name
   ```

### Build to Android (for AR testing)

- `File → Build Settings → Android → Switch Platform`
- Player Settings → Minimum API Level: **Android 7.0 (API 24)**
- Target Architecture: **ARM64**
- Connect phone with USB debugging → `Build and Run`

---

## The One Thing You Must Understand: The Data Flow

Every 5 seconds, `DataManager` fetches a snapshot from InfluxDB and fires an event:

```csharp
// DataManager fires this — you subscribe to it
public event Action<FactorySnapshot> OnSnapshotUpdated;

// FactorySnapshot contains:
snap.machines          // List<MachineData> — one per loom
snap.factory.total_power_kw
snap.factory.total_co2_today_kg
```

Each `MachineData` has:

```csharp
md.device_id           // "ESP32_TEX_001" … "ESP32_TEX_008"
md.display_name        // "Loom 001"
md.avg_power_watts     // e.g. 782.8 — main sensor value
md.health_score        // 0.0 (critical) … 1.0 (healthy) — computed automatically
md.alert_level         // 0 = OK, 1 = warning (>750W), 2 = critical (>900W)
md.is_online           // true if last reading < 30 min ago
md.co2_kg_today        // kg CO2 emitted today (estimated)
md.cbam_contribution   // EUR/year carbon tax cost
md.last_seen           // ISO 8601 timestamp
```

**Subscribe in `OnEnable`, unsubscribe in `OnDisable`:**
```csharp
void OnEnable()  => DataManager.Instance.OnSnapshotUpdated += OnSnapshot;
void OnDisable() => DataManager.Instance.OnSnapshotUpdated -= OnSnapshot;

void OnSnapshot(FactorySnapshot snap)
{
    var md = snap.machines.Find(m => m.device_id == _myDeviceId);
    // update your AR UI here
}
```

You can also get the last snapshot immediately (without waiting for the next poll):
```csharp
var snap = DataManager.Instance.LastSnapshot;
```

---

## Member Assignments

---

### Member 1 — AR Foundation Core
**File:** `Assets/Scripts/AR/Core/ARSessionManager.cs`
**Assembly:** `Smartex.AR.Core`

**What you build:**
The foundation that makes AR work on the phone. Without this, nothing else runs. You configure the AR session, enable plane detection so overlays snap to the real factory floor, and provide a shared `ARAnchor` factory that other modules call.

**What to implement:**

1. In Unity, create the **SmartexAR scene**:
   - `File → New Scene`
   - Add an `XR Origin` GameObject (`GameObject → XR → XR Origin (AR Rig)`)
   - Add an `AR Session` GameObject (`GameObject → XR → AR Session`)
   - Attach `ARSessionManager.cs` to the AR Session object
   - Assign the `ARPlaneManager`, `ARAnchorManager`, `ARRaycastManager` fields in the Inspector

2. Configure plane detection:
```csharp
planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
// Set a plane prefab so the floor lights up during development
// (disable it for the final factory build — the real floor is visible)
```

3. The `CreateAnchor(Pose)` method is already stubbed — fill in the implementation using `ARAnchorManager`.

**Concepts to understand:**
- **ARSession** — the "AR brain"; must be in every AR scene. It manages the phone's tracking.
- **XR Origin** — the coordinate system bridge between AR world and Unity world. Think of it as "where is the user in the scene".
- **ARPlane** — AR Foundation detects flat surfaces and gives them a `Pose` (position + rotation). Overlays anchor to these.
- **ARAnchor** — a point in real-world space that Unity keeps tracking even as the camera moves. Other modules attach UI to anchors.
- **Pose** — a struct combining `Vector3 position` and `Quaternion rotation`.

**How it connects to the final product:**
Every AR overlay (Module C), maintenance callout (Module D), and training label (Module F) is anchored to a real-world position via an `ARAnchor` that your session creates. Without your planes and anchors, all overlays drift as the user moves.

---

### Member 2 — Machine Recognition
**Files:** `Assets/Scripts/AR/Recognition/MachineQRTracker.cs`, `ManualMachineSelector.cs`
**Assembly:** `Smartex.AR.Recognition`

**What you build:**
The bridge between the physical loom and the data. When a technician points their phone at a loom, your code figures out *which* loom it is and fires an event. Everything else (overlay, maintenance guide, training) listens to that event.

**What to implement:**

1. **Create the QR label image library** in Unity:
   - `Assets → Create → XR → Reference Image Library`
   - Save as `Assets/AR/MarkerLibrary.asset`
   - For each loom, add an entry — **name must equal the device_id** (`ESP32_TEX_001` … `ESP32_TEX_008`)
   - For now, generate placeholder QR codes at `qrcode.tec-it.com` with the device_id as content, print them, photograph them, add as textures

2. Set up `ARTrackedImageManager` on the XR Origin:
   - Assign `MarkerLibrary.asset` to `referenceLibrary`
   - Set `maxNumberOfMovingImages = 1`

3. The event is already declared — just make sure the `HandleImage` method fires correctly:
```csharp
// This event is what Modules C, D, F listen to:
public static event Action<string, Pose> OnMachineRecognised;
```

4. `ManualMachineSelector.cs` — populate the list from `DataManager.Instance.LastSnapshot.machines` and fire the same event on button tap (fallback when QR is covered).

**Concepts to understand:**
- **ARTrackedImageManager** — scans camera frames for images in your reference library. When found, creates an `ARTrackedImage` with `trackingState` and world `Pose`.
- **Reference Image Library** — a compiled asset containing the images to recognise. Each image has a name (string) — you use this as the device_id.
- **TrackingState** — `Tracking` means currently visible, `Limited` means partially occluded, `None` means lost.
- **Static events** — `OnMachineRecognised` is `static` so any module can subscribe without needing a reference to your object.

**How it connects:**
Module C (Overlay) subscribes to `OnMachineRecognised` and spawns the data panel. Module D subscribes and shows maintenance guide if health is low. Module F subscribes and shows training content. Without your recognition, none of them activate.

---

### Member 3 — Real-Time AR Data Overlay
**Files:** `Assets/Scripts/AR/Overlay/MachineAROverlay.cs`, `BillboardFacer.cs`
**Assembly:** `Smartex.AR.Overlay`

**What you build:**
The floating panel that appears above a loom when it's recognised. It shows live sensor data that updates every 5 seconds automatically. This is the most visible part of the AR experience.

**What to implement:**

1. **Create the AR Overlay Prefab** in Unity:
   - Create a World Space Canvas (set `Render Mode = World Space`, scale to ~0.5m wide)
   - Add a circular **health ring** (Image component, `Image Type = Filled, Fill Method = Radial 360`)
   - Add `TextMeshProUGUI` labels: power, vibration, CBAM cost
   - Add a pulsing red halo child object (toggled by `alertHalo`)
   - Add `BillboardFacer.cs` to the root — it auto-rotates toward the camera every frame
   - Add `MachineARPanel.cs` to the root
   - Save as `Assets/AR/Prefabs/MachineAROverlay.prefab`

2. Assign the prefab to `MachineAROverlaySpawner.overlayPrefab` in the scene

3. Fill in the `Refresh()` method — it's already called for you on every snapshot:
```csharp
// Already wired up — just fill in the colour logic:
if (healthRing != null)
{
    healthRing.fillAmount = md.health_score;          // 0.0 → 1.0
    healthRing.color = cfg.GetHealthColor(md.health_score);  // green/orange/red
}
```

4. `NotifyClicked()` already calls `MachineDetailPanel.Instance?.OpenById(_deviceId)` — wire this to a Button on the canvas.

**Concepts to understand:**
- **World Space Canvas** — a Unity UI canvas placed in 3D world space (not screen-space). Scale it down — 1 Unity unit = 1 metre, so a canvas of scale (0.001, 0.001, 0.001) is 1mm, which is too small. Use ~(0.002, 0.002, 0.002) for a 0.5m panel.
- **Billboard shader / BillboardFacer** — rotates a GameObject to always face the camera. Without it, the panel is readable from only one direction.
- **Image.fillAmount** — drives the health ring: `0.0` = empty ring (dead), `1.0` = full ring (healthy).
- **Event subscription pattern** — your `MachineAROverlaySpawner` already subscribes to `MachineQRTracker.OnMachineRecognised` (Module 2's event) and `DataManager.OnSnapshotUpdated` (core event). You don't poll — the data comes to you.

**How it connects:**
This is the centrepiece the factory manager sees. The maintenance team sees Module D stacked below yours. The training screen (Module F) replaces yours temporarily during onboarding. Your `MachineARPanel` prefab is what makes the demo impressive in 30 seconds.

---

### Member 4 — AR Maintenance Workflow
**File:** `Assets/Scripts/AR/Maintenance/ARMaintenanceGuide.cs`
**Assembly:** `Smartex.AR.Maintenance`
**Also needs:** Backend endpoints in `SmartexVR/backend/app/main.py`

**What you build:**
When a technician scans a machine with `health_score < 0.4` (critical), your code fetches a step-by-step repair procedure and shows numbered AR callouts pointing at the parts to inspect. Each confirmed step logs to the backend.

**What to implement:**

1. **Backend first** (`SmartexVR/backend/app/main.py`):
```python
@app.get("/maintenance/procedures/{device_id}")
async def get_procedure(device_id: str):
    return {
        "device_id": device_id,
        "steps": [
            {"id": 1, "title": "Coupe l'alimentation", "description": "...",
             "anchor_offset": {"x": 0.1, "y": 0.5, "z": 0.0}},
            {"id": 2, "title": "Inspecte la courroie", "description": "...",
             "anchor_offset": {"x": -0.2, "y": 0.3, "z": 0.1}},
        ]
    }

@app.post("/maintenance/logs")
async def log_step(body: dict):
    # store to DB
    return {"ok": True}
```

2. **Unity side** — implement `ShowStep(index)`:
```csharp
void ShowStep(int index)
{
    var step = _procedure.steps[index];
    var pos  = _anchorPose.position + step.anchorOffset;
    var go   = Instantiate(stepCalloutPrefab, pos, Quaternion.identity);
    go.GetComponentInChildren<TextMeshPro>().text = $"{step.id}. {step.title}";
}
```

3. Create the `stepCalloutPrefab`: a floating sphere + arrow + TextMeshPro label. Number it (1, 2, 3…). The `AdvanceStep()` method is called when the technician taps "Done" on the checklist.

**Concepts to understand:**
- **Coroutines (`IEnumerator` / `yield return`)** — already used in `FetchProcedure()`. A coroutine is a function that can pause mid-execution (`yield return req.SendWebRequest()`) and resume when the web request completes without freezing the app.
- **UnityWebRequest** — Unity's HTTP client. The pattern `yield return req.SendWebRequest()` waits for the response, then `req.result` tells you if it succeeded.
- **Target-local anchor offsets** — `step.anchorOffset` is a `Vector3` relative to the recognized machine target. `(0.1, 0.5, 0.0)` means "10cm to the right, 50cm up, same depth as the target".
- **health_score < 0.4** — means the machine is consuming > 840W (see `MachineData.cs` line 32). This is the trigger threshold; you can adjust it in `ARMaintenanceGuide.healthThreshold`.

**How it connects:**
`ARMaintenanceGuide` activates only for damaged machines. The completed maintenance log feeds back to the SmartexVR backend, which tracks repair history and exposes it to deterministic analytics plus AI assistance. Your POST to `/maintenance/logs` closes the loop between the AR app and the AI.

---

### Member 5 — Remote Expert Assist
**File:** `Assets/Scripts/AR/RemoteAssist/ARRemoteSession.cs`
**Assembly:** `Smartex.AR.RemoteAssist`
**Also needs:** Backend WebSocket endpoint + WebRTC signaling

**What you build:**
A technician in the factory can call a remote expert who watches the AR camera feed from a browser. The expert draws annotations (circles, arrows, text) that appear floating in the technician's AR view in real time.

**What to implement:**

1. **Add WebRTC package** to `Packages/manifest.json`:
```json
"com.unity.webrtc": "3.0.0-pre.7"
```

2. **Backend WebSocket** (`SmartexVR/backend/app/main.py`):
```python
from fastapi import WebSocket

@app.post("/sessions")
async def create_session(body: dict):
    session_id = str(uuid.uuid4())
    # store session
    return {"session_id": session_id}

@app.websocket("/ws/ar-session/{session_id}")
async def ar_session_ws(websocket: WebSocket, session_id: str):
    await websocket.accept()
    # relay messages between technician and expert
    while True:
        data = await websocket.receive_text()
        await websocket.send_text(data)  # broadcast to other clients in session
```

3. **Unity WebSocket client** — implement `ConnectWebSocket()` in `ARRemoteSession.cs`:
```csharp
// Use NativeWebSocket (free, MIT) or Unity's ClientWebSocket
// On message → deserialize JSON → call SpawnAnnotation(msg)
```

4. Annotation message format (already defined in the stub):
```json
{ "type": "annotation", "local_pos": {"x":0.12,"y":0.18,"z":0.0},
  "color": "#FF0000", "text": "Check here", "author": "expert" }
```

**Concepts to understand:**
- **WebRTC** — peer-to-peer video streaming protocol. `com.unity.webrtc` lets you stream the AR camera as a video track to a browser.
- **WebSocket** — a persistent two-way connection (unlike HTTP which is request/response). Used here for the annotation channel (low-latency, bidirectional).
- **Target-local annotation** — `local_pos` in the JSON is a coordinate relative to the recognized machine target. Consumers should spawn the marker as a child of the machine anchor so it stays tracked.
- **AI recommendation** — `ShowAgentRecommendation(string text)` is called from outside with the backend AI-assist suggestion text. Wire it to a `TextMeshProUGUI` floating panel.

**How it connects:**
This module is triggered on-demand when a technician presses "Call Expert". The session ID is shared with the expert via a link. The SmartexVR backend can also push anomaly analysis text through the same channel via `ShowAgentRecommendation`.

---

### Member 6 — Training & Onboarding
**File:** `Assets/Scripts/AR/Training/ARTrainingModule.cs`
**Assembly:** `Smartex.AR.Training`
**Also needs:** Backend training content endpoints

**What you build:**
A new operator scans any loom → AR labels name every component. Then a quiz starts: "Tap the tension sensor". The correct part highlights. Score is recorded. Supports Arabic, French, and English.

**What to implement:**

1. **Backend content** (`SmartexVR/backend/app/main.py`):
```python
@app.get("/training/modules/{device_type}")
async def get_module(device_type: str):
    return {
        "device_type": "jacquard_loom",
        "components": [
            {"id": "tension_sensor", "label_ar": "حساس الشد",
             "label_fr": "Capteur de tension", "label_en": "Tension sensor",
             "anchor_offset": {"x": 0.05, "y": 0.2, "z": -0.1}}
        ],
        "quiz": [
            {"question_en": "Tap the tension sensor",
             "question_fr": "Touchez le capteur de tension",
             "question_ar": "انقر على حساس الشد",
             "correct_component": "tension_sensor"}
        ]
    }

@app.post("/training/assessments")
async def submit_score(body: dict):
    return {"ok": True, "passed": body.get("score", 0) >= 0.7}
```

2. **Language switching** — a dropdown on the training UI sets `ARTrainingModule.language`:
```csharp
public AppLanguage language = AppLanguage.French; // default: French (Morocco)
```

3. **`OnComponentTapped(string componentId)`** — call this from a Button on each label prefab. The method already handles scoring; you just need to wire the Button.

4. **Component label prefab** — floating pin (sphere + line) with a TextMeshPro label. Tappable (Button component). Pass its `componentId` to `OnComponentTapped`.

**Concepts to understand:**
- **Enum switch expression** (`language switch { AppLanguage.French => comp.label_fr, ... }`) — already in the stub, read it to understand the pattern.
- **Coroutine for HTTP** — same pattern as Module 4: `yield return req.SendWebRequest()`.
- **AR label placement** — `comp.anchor_offset` is a `Vector3` relative to the QR anchor. Labels are Instantiated at `_anchorPose.position + comp.anchor_offset`.
- **Pass/Fail threshold** — a score ≥ 70% means the operator is certified on this machine. The backend stores this per `user_id` (use the device's unique ID for now).

**How it connects:**
Training data feeds the backend's user progress records. Factory managers can see which operators are certified on which looms, and AI assistance can use certification context when it is provided by the backend.

---

### Member 7 — QA, DevOps & Documentation
**File:** `Assets/Scripts/AR/QA/ARPerformanceProfiler.cs`
**Assembly:** `Smartex.AR.QA`

**What you build:**
The CI/CD pipeline, automated tests, performance budget enforcement, and the final QA pass before the factory pilot demo. You also own the QR label printing workflow.

**What to implement:**

1. **GitHub Actions CI** — create `.github/workflows/unity-build.yml`:
```yaml
name: Unity Build
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
        with: { lfs: true }
      - uses: game-ci/unity-builder@v4
        with:
          unityVersion: 6000.3.11f1
          targetPlatform: Android
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL:   ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
```

2. **Play Mode tests** — add to `Assets/Tests/AR/` (create folder):
```csharp
[UnityTest]
public IEnumerator ARSessionManager_InitialisesInOneFrame()
{
    yield return null; // wait one frame
    Assert.IsNotNull(ARSessionManager.Instance);
}

[UnityTest]
public IEnumerator MachineARPanel_Refresh_UpdatesAllLabels()
{
    var panel = /* setup */;
    var fakeSnapshot = TestFixtures.MakeSnapshot("ESP32_TEX_001", 782f);
    panel.Bind("ESP32_TEX_001");
    panel.Refresh(fakeSnapshot);
    yield return null;
    Assert.AreEqual("782 W", panel.powerLabel.text);
}
```

3. **`ARPerformanceProfiler`** — attach it to a `DebugHUD` Canvas in the AR scene. In the Inspector, enable the GameObject only in `Development Build` (use `#if DEVELOPMENT_BUILD`).

4. **QR Label template** — create `Docs/AR/QR-Labels/` with:
   - 8 QR code PNG files (generated from `qrcode.tec-it.com`, content = device_id)
   - A printable A4 PDF with all 8 labels + machine names
   - Lamination instructions (factory floor is humid)

5. **Performance targets to enforce:**
   - ≥ 60 fps on Snapdragon 665 (mid-range 2021 Android)
   - AR tracking latency < 100ms
   - Memory < 800 MB
   - No more than 2 `FindObjectsByType` calls per frame (expensive — search codebase for them)

**Concepts to understand:**
- **Unity Test Framework** — `[UnityTest]` for play-mode tests (can test MonoBehaviour logic), `[Test]` for pure C# unit tests.
- **Git LFS** — already configured in `.gitattributes`. Large files (FBX, PNG, PDF) go through LFS automatically. Team members need `git lfs install` once after cloning.
- **Unity Cloud Build** — alternative to GitHub Actions; free for student projects at `unity.com/products/unity-devops`.
- **IL2CPP vs Mono** — for Android release builds, use **IL2CPP** (faster runtime) + **ARM64**. For dev iteration, **Mono** is faster to build.

**How it connects:**
You are the safety net. Every other member's code must pass your performance profiler and test suite before it merges to `master`. You own the final APK that goes into the factory for the pilot.

---

## Git Workflow

```
master          ← stable, always buildable
  └── feature/ar-member-1-session-core      (Member 1)
  └── feature/ar-member-2-recognition       (Member 2)
  └── feature/ar-member-3-overlay           (Member 3)
  └── feature/ar-member-4-maintenance       (Member 4)
  └── feature/ar-member-5-remote-assist     (Member 5)
  └── feature/ar-member-6-training          (Member 6)
  └── feature/ar-member-7-qa-devops         (Member 7)
```

**Rules:**
- Never push directly to `master`
- Open a Pull Request → at least one other member reviews
- Member 7 is the final approver for merges to master
- Always `git pull origin master` before starting a new session
- Scene files (`.unity`) merge badly — coordinate with Member 1 before editing the AR scene

---

## Key Shared Conventions

| Thing | Convention |
|-------|-----------|
| Device IDs | `ESP32_TEX_001` … `ESP32_TEX_008` — never hardcode, read from `MachineData.device_id` |
| Backend URL | Read from `SmartexConfig.Instance.relayBaseUrl` — never hardcode `localhost` |
| Polling | Never poll manually — subscribe to `DataManager.OnSnapshotUpdated` |
| Coroutines | Always `yield return req.SendWebRequest()` — never `await` (Unity's networking is not async-friendly in coroutines) |
| Destroying objects | Use `Destroy()` in Play mode, `DestroyImmediate()` in Editor — see `FactoryBuilder.SafeDestroy()` for the pattern |
| Colours | Use `SmartexConfig.Instance.GetHealthColor(health_score)` — consistent with VR twin |
| Namespaces | Each module has its own: `Smartex.AR.Core`, `Smartex.AR.Recognition`, etc. |

---

## InfluxDB — Live Data

The 8 ESP32s push data to:
- **URL:** `https://smartex.ahmedbenahmed.com/influxdb`
- **Bucket:** `telemetry`
- **Measurement:** `smartex_derived`
- **Fields:** `avg_power_watts`, `co2_kg_h`, `grid_ef`
- **Tags:** `device_id` (ESP32_TEX_001…008), `machine_id`, `shift`

The relay server at `http://localhost:8000/snapshot` (when running) pre-processes the InfluxDB data and returns a `FactorySnapshot` JSON directly. `DataManager` tries the relay first, falls back to InfluxDB direct if unavailable.

**You never talk to InfluxDB directly.** `DataManager` does it for you.

---

## Delivery Waves

| Wave | Owner | Target |
|------|-------|--------|
| **Wave 1** | Members 1 + 2 + 3 | Scan loom QR → live data panel in AR |
| **Wave 2** | Members 4 + 7 | Step-by-step guided repair |
| **Wave 3** | Member 6 | Operator onboarding flow |
| **Wave 4** | Member 5 | Remote expert with live annotations |
| **Wave 5** | All | Final QA + factory pilot APK |

Wave 1 is the demo milestone. Focus there first.
