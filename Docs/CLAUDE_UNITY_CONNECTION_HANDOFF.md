# Claude Handoff: Unity/Vuforia Connection

Date: 2026-05-25

This repo now has a separate FastAPI backend under `SmartexVR/backend/`. The next agent should focus on the Unity/Vuforia connection layer, not backend scaffolding.

## Read First

- `backend/README.md` - backend run commands, env, API list.
- `Docs/smartexvr-backend-todo.md` - current done/left status.
- `Assets/Scripts/Contracts/README.md` - stable AR service contracts.
- `Assets/Scripts/Contracts/DataTypes.cs` - `RecognizedMachine`, `Annotation`, maintenance/training DTOs.
- `Assets/Scripts/Core/DataManager.cs` - Unity already polls `SmartexConfig.Instance.relayBaseUrl + "/snapshot"` first.

## Backend Status

The backend is implemented and tested locally:

- `GET /health`
- `GET /snapshot`
- `GET /machines`
- `GET /machines/{device_id}/latest`
- `GET /machines/{device_id}/timeseries?range=24h`
- `GET /machines/{device_id}/anomalies?range=24h`
- `GET /maintenance/procedures/{device_id}`
- `POST /maintenance/logs`
- `GET /maintenance/logs/{device_id}`
- `GET /training/modules/{device_type}`
- `POST /training/assessments`
- `GET /training/progress/{user_id}`
- `POST /sessions`
- `GET /sessions/{session_id}`
- `GET /sessions/{session_id}/recording`
- `WS /ws/ar-session/{session_id}`
- `POST /assist/query`
- `POST /assist/sessions/{session_id}/summary`
- `POST /assist/sessions/{session_id}/report`

Local backend run:

```bash
cd SmartexVR/backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Validation:

```bash
cd SmartexVR/backend
UV_CACHE_DIR=/tmp/uv-cache uv run pytest -q
docker compose config
```

Expected test result at handoff: `14 passed`.

## Environment

The backend reads env files in this order from `SmartexVR/backend`:

1. `../../.env` - course-level env at `/home/elwalid/projects/ensa/ar/.env`
2. `.env` - backend-local override

The parent `.env` currently provides `MISTRAL_API_KEY`. Do not commit it.

Docker Compose intentionally does not load `../../.env` by default to avoid printing secrets through `docker compose config`. To run Docker with the parent env:

```bash
cd SmartexVR/backend
docker compose --env-file ../../.env up --build
```

## Important Unity Direction

Use **Vuforia** for machine recognition.

Some older AR Foundation scripts remain in the repo:

- `Assets/Scripts/AR/Core/ARSessionManager.cs`
- `Assets/Scripts/AR/Recognition/MachineQRTracker.cs`

Do not expand those as the final recognition path. Instead, implement Vuforia behind the stable contract:

- `Assets/Scripts/Contracts/IMachineRecognizer.cs`
- `Assets/Scripts/Contracts/DataTypes.cs`
- `Assets/Scripts/Contracts/ARServices.cs`

Target design:

```text
Vuforia ImageTarget recognized
  -> VuforiaTargetScanner emits RecognizedMachine
  -> RecognizedMachine.DeviceId = ESP32_TEX_00N
  -> RecognizedMachine.AnchorTransform = ImageTarget transform
  -> Overlay / Maintenance / Training / RemoteAssist parent AR content under AnchorTransform
```

Device IDs:

```text
ESP32_TEX_001
ESP32_TEX_002
ESP32_TEX_003
ESP32_TEX_004
ESP32_TEX_005
ESP32_TEX_006
ESP32_TEX_007
ESP32_TEX_008
```

Vuforia target names should match these device IDs exactly.

## First Unity Task

Create a production recognizer, suggested path:

```text
Assets/Scripts/AR/Recognition/VuforiaTargetScanner.cs
```

Responsibilities:

- Register itself with `ARServices.Register((IMachineRecognizer)this)`.
- Map each Vuforia ImageTarget / ObserverBehaviour target name to a `device_id`.
- On target found:
  - get latest machine data from `DataManager.Instance.GetMachine(deviceId)`
  - emit `OnMachineRecognized(new RecognizedMachine(deviceId, targetTransform, data))`
- On target lost:
  - emit `OnMachineLost(deviceId)`
- Never call Mistral from Unity.
- Never call InfluxDB directly from AR modules.

## Refactor Consumers

Current AR modules still subscribe to `MachineQRTracker.OnMachineRecognised` in places. Replace those direct static dependencies with `ARServices.Recognizer` where feasible.

Primary files to update:

- `Assets/Scripts/AR/Overlay/MachineAROverlay.cs`
- `Assets/Scripts/AR/Maintenance/ARMaintenanceGuide.cs`
- `Assets/Scripts/AR/Training/ARTrainingModule.cs`
- `Assets/Scripts/AR/RemoteAssist/ARRemoteSession.cs`

Keep the existing mock services under `Assets/Scripts/Contracts/Mocks/` for editor development without Vuforia.

## Remote Assist Coordinates

Backend annotations use target-local coordinates:

```json
{
  "type": "annotation",
  "device_id": "ESP32_TEX_003",
  "local_pos": { "x": 0.12, "y": 0.18, "z": 0.0 },
  "color": "#FF0000",
  "text": "Check belt tension",
  "author": "remote_expert"
}
```

Unity should instantiate markers as children of the recognized machine anchor:

```csharp
marker.transform.SetParent(recognizedMachine.AnchorTransform, false);
marker.transform.localPosition = localPos;
```

The backend accepts legacy `world_pos` only for compatibility and normalizes it to `local_pos`.

## Ask AI Button

Unity should call:

```http
POST /assist/query
```

Example body:

```json
{
  "device_id": "ESP32_TEX_003",
  "locale": "fr",
  "question": "Pourquoi cette machine est en alerte ?",
  "include_recent_anomalies": true,
  "include_maintenance_history": true
}
```

Response:

```json
{
  "answer": "...",
  "actions": ["..."],
  "risk_level": "critical",
  "sources": ["snapshot", "recent_anomalies", "maintenance_history"],
  "ai_provider": "mistral"
}
```

If Mistral times out or fails, backend falls back to deterministic guidance with `ai_provider: "deterministic"`.

## Unity Test Gate On A Unity Machine

Run these once Unity/Vuforia is available:

- Unity EditMode tests.
- Vuforia target recognition smoke:
  - target `ESP32_TEX_003` recognized
  - overlay parents under target transform
  - machine data comes from `/snapshot`
- Maintenance smoke:
  - fetch procedure
  - complete step
  - POST log succeeds
- Training smoke:
  - fetch module
  - submit assessment
  - progress updates
- Remote assist smoke:
  - create session
  - connect WebSocket
  - receive target-local annotation
- Ask AI smoke:
  - call `/assist/query`
  - render answer in floating recommendation panel

## Do Not Do Yet

- Do not merge `smartex-grid` into this backend.
- Do not put Mistral keys in Unity.
- Do not commit Vuforia license keys.
- Do not implement WebRTC video before text/annotation signaling is stable.
- Do not convert target-local annotations back to world-space.
