# SmartexVR Backend

FastAPI backend for the Unity AR/VR project. It keeps textile-machine telemetry separate from `smartex-grid`, serves Unity's `/snapshot` relay contract, stores AR workflow metadata, relays remote-assist messages, and wraps Mistral AI assistance behind deterministic analytics.

## Quickstart

```bash
cd SmartexVR/backend
uv run uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Unity already defaults to `http://localhost:8000` in `SmartexConfig.relayBaseUrl`.

## Local Verification

```bash
uv run pytest
curl http://127.0.0.1:8000/health
curl http://127.0.0.1:8000/snapshot
```

Mock telemetry is enabled by default and returns `ESP32_TEX_001` through `ESP32_TEX_008`.

## Configuration

The backend reads `../../.env` from the course folder first, then `SmartexVR/backend/.env` for local backend overrides. Copy `.env.example` to `.env` only when you want backend-specific values.

Key settings:

- `SMARTEX_DATA_SOURCE=mock|influx`
- `SMARTEX_DB_PATH=./data/smartex_backend.sqlite3`
- `SMARTEX_API_TOKEN` for optional shared-token protection
- `REQUIRE_AUTH_FOR_SNAPSHOT=true` to protect Unity's relay endpoint too
- `INFLUX_URL`, `INFLUX_TOKEN`, `INFLUX_ORG`, `INFLUX_BUCKET`
- `MISTRAL_API_KEY`, `MISTRAL_MODEL`, `MISTRAL_TIMEOUT_SECONDS`

Without `MISTRAL_API_KEY`, `/assist/query` returns deterministic technician guidance from the snapshot, anomaly records, and maintenance logs.

When `SMARTEX_API_TOKEN` is set, protected routes accept either `x-smartex-token: <token>` or `Authorization: Bearer <token>`. `/snapshot` stays open for local Unity demos unless `REQUIRE_AUTH_FOR_SNAPSHOT=true`.

## Docker

```bash
cd SmartexVR/backend
docker compose up --build
```

If you want Docker Compose to use the course-level env file, pass it explicitly:

```bash
docker compose --env-file ../../.env up --build
```

## Implemented API

Telemetry:

- `GET /health`
- `GET /snapshot`
- `GET /machines`
- `GET /machines/{device_id}/latest`
- `GET /machines/{device_id}/timeseries?range=24h`
- `GET /machines/{device_id}/anomalies?range=24h`

AR workflows:

- `GET /maintenance/procedures/{device_id}`
- `POST /maintenance/logs`
- `GET /maintenance/logs/{device_id}`
- `GET /training/modules/{device_type}`
- `POST /training/assessments`
- `GET /training/progress/{user_id}`

Remote assist:

- `POST /sessions`
- `GET /sessions/{session_id}`
- `GET /sessions/{session_id}/recording`
- `WS /ws/ar-session/{session_id}`

AI assistance:

- `POST /assist/query`
- `POST /assist/sessions/{session_id}/summary`
- `POST /assist/sessions/{session_id}/report`

Remote-assist annotations use `local_pos` target-local coordinates. `world_pos` is accepted only for backward compatibility and normalized to `local_pos`.
