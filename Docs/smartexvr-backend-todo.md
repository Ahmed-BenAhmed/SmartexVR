# SmartexVR Backend TODO

Status date: 2026-05-25

## Done Locally

- Separate backend lives in `SmartexVR/backend/`; `smartex-grid` is not a runtime dependency.
- Unity-compatible `GET /snapshot` returns the existing `RelayResponse` shape.
- Mock textile telemetry covers `ESP32_TEX_001` through `ESP32_TEX_008`.
- Optional InfluxDB adapter is implemented behind `SMARTEX_DATA_SOURCE=influx`.
- Deterministic rolling MAD anomaly detection and risk summaries are implemented.
- Maintenance procedures/logs, training modules/assessments/progress, sessions, WebSocket relay, and AI assist endpoints are implemented.
- Mistral is backend-only; when `MISTRAL_API_KEY` is absent, deterministic guidance is returned.
- Optional shared-token protection is available with `SMARTEX_API_TOKEN`.
- Docker, Compose, Makefile, `.env.example`, and pytest coverage are in place.

## Test Gate

- `cd SmartexVR/backend && uv run pytest`
- `cd SmartexVR/backend && make run`
- In another shell: `cd SmartexVR/backend && make smoke`

Unity is not available on this machine, so the local completion gate is backend tests plus live API smoke checks. Unity/Vuforia integration must be verified on a machine with Unity installed.

## Vuforia-Aware Unity Work Left

- Install/configure Vuforia in Unity and keep the license in `ARConfig`, never in scene files.
- Replace the old AR Foundation recognition stubs with a `VuforiaTargetScanner` that implements `IMachineRecognizer`.
- Preserve the stable contract: recognized target -> `RecognizedMachine.AnchorTransform` -> AR content parented under that target.
- Connect Vuforia ImageTarget names to `device_id` values: `ESP32_TEX_001` through `ESP32_TEX_008`.
- Update Module C/D/E/F consumers to prefer `ARServices.Recognizer` over direct `MachineQRTracker` static events.
- Parent remote annotations under the recognized machine target using backend `local_pos`.
- Add Unity tests on a Unity machine for Vuforia recognition events, overlay parenting, and backend URL wiring.

## Backend Work Left After Local Pass

- Verify live InfluxDB credentials and the `smartex_derived` field names against the real bucket.
- Test `MISTRAL_API_KEY` against the real Mistral service from the deployment environment.
- Decide whether SQLite is enough for the demo or migrate metadata storage to Postgres before pilot usage.
- Add expert browser UI for remote assist text and target-local annotations.
- Add WebRTC video/audio and recording after signaling is stable.
- Add deployment target configuration once the hosting environment is chosen.
