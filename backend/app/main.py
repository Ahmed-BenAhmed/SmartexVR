from __future__ import annotations

import json
import re
import uuid
from contextlib import asynccontextmanager
from datetime import datetime, timedelta, timezone
from typing import Any

from fastapi import Depends, FastAPI, Header, HTTPException, Query, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware

from app.analytics import detect_power_anomalies, machine_from_telemetry, summarize_risk
from app.config import Settings, get_settings
from app.mistral_client import AssistantClient
from app.models import (
    AssistQueryRequest,
    FactorySnapshot,
    FactoryStats,
    MaintenanceLogRequest,
    MaintenanceProcedure,
    MaintenanceStep,
    RelayResponse,
    SessionCreateRequest,
    SessionResponse,
    SessionTextRequest,
    TimeSeriesPoint,
    TrainingAssessmentRequest,
    TrainingModule,
    utc_now,
    Vector3,
)
from app.security import optional_snapshot_token, require_api_token, require_websocket_token
from app.storage import MetadataStore
from app.telemetry import TelemetryRepository, build_telemetry_repository


@asynccontextmanager
async def lifespan(fastapi_app: FastAPI):
    settings = get_settings()
    fastapi_app.state.settings = settings
    fastapi_app.state.telemetry = build_telemetry_repository(settings)
    fastapi_app.state.store = MetadataStore(settings.smartex_db_path)
    fastapi_app.state.assistant = AssistantClient(settings)
    fastapi_app.state.hub = SessionHub()
    yield


app = FastAPI(title="SmartexVR Backend", version="0.1.0", lifespan=lifespan)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class SessionHub:
    def __init__(self) -> None:
        self._connections: dict[str, set[WebSocket]] = {}

    async def connect(self, session_id: str, websocket: WebSocket) -> None:
        await websocket.accept()
        self._connections.setdefault(session_id, set()).add(websocket)

    def disconnect(self, session_id: str, websocket: WebSocket) -> None:
        sockets = self._connections.get(session_id)
        if not sockets:
            return
        sockets.discard(websocket)
        if not sockets:
            self._connections.pop(session_id, None)

    async def broadcast(self, session_id: str, payload: dict[str, Any], sender: WebSocket | None = None) -> None:
        sockets = list(self._connections.get(session_id, set()))
        for socket in sockets:
            if socket is sender:
                continue
            await socket.send_json(payload)


def settings_dep() -> Settings:
    return app.state.settings


def telemetry_dep() -> TelemetryRepository:
    return app.state.telemetry


def store_dep() -> MetadataStore:
    return app.state.store


def assistant_dep() -> AssistantClient:
    return app.state.assistant


def hub_dep() -> SessionHub:
    return app.state.hub


def protected_endpoint(
    settings: Settings = Depends(settings_dep),
    x_smartex_token: str | None = Header(default=None),
    authorization: str | None = Header(default=None),
) -> None:
    require_api_token(settings, x_smartex_token=x_smartex_token, authorization=authorization)


def snapshot_auth_dependency(
    settings: Settings = Depends(settings_dep),
    x_smartex_token: str | None = Header(default=None),
    authorization: str | None = Header(default=None),
) -> None:
    optional_snapshot_token(settings, x_smartex_token=x_smartex_token, authorization=authorization)


@app.get("/health")
async def health(settings: Settings = Depends(settings_dep), assistant: AssistantClient = Depends(assistant_dep)) -> dict[str, Any]:
    return {
        "ok": True,
        "service": settings.app_name,
        "timestamp": utc_now().isoformat(),
        "data_source": settings.smartex_data_source,
        "ai_configured": assistant.is_configured,
        "auth_enabled": bool(settings.smartex_api_token),
    }


@app.get("/snapshot", response_model=RelayResponse)
async def snapshot(
    settings: Settings = Depends(settings_dep),
    telemetry: TelemetryRepository = Depends(telemetry_dep),
    _: None = Depends(snapshot_auth_dependency),
) -> RelayResponse:
    rows = await telemetry.latest_all()
    now = utc_now()
    machines = [machine_from_telemetry(row, settings, now=now) for row in rows]
    total_power_kw = sum(machine.avg_power_watts for machine in machines) / 1000.0
    total_co2_today_kg = sum(machine.co2_kg_h * 8.0 for machine in machines)
    cbam_mad = total_co2_today_kg * settings.carbon_price_eur * settings.eur_to_mad / 1000.0
    snap = FactorySnapshot(
        timestamp=now.isoformat(),
        machines=machines,
        factory=FactoryStats(
            total_power_kw=round(total_power_kw, 3),
            total_co2_today_kg=round(total_co2_today_kg, 3),
            cbam_exposure_mad=round(cbam_mad, 3),
        ),
    )
    return RelayResponse(ok=True, data=snap)


@app.get("/machines")
async def machines(response: RelayResponse = Depends(snapshot)) -> list[Any]:
    return response.data.machines if response.data else []


@app.get("/machines/{device_id}/latest")
async def latest_machine(
    device_id: str,
    settings: Settings = Depends(settings_dep),
    telemetry: TelemetryRepository = Depends(telemetry_dep),
) -> Any:
    row = await telemetry.latest(device_id)
    if row is None:
        raise HTTPException(status_code=404, detail=f"Unknown device_id: {device_id}")
    return machine_from_telemetry(row, settings)


@app.get("/machines/{device_id}/timeseries")
async def machine_timeseries(
    device_id: str,
    range: str = Query(default="24h", pattern=r"^\d+[mhd]$"),
    telemetry: TelemetryRepository = Depends(telemetry_dep),
) -> dict[str, Any]:
    until = utc_now()
    since = until - parse_range(range)
    rows = await telemetry.series(device_id, since, until)
    if not rows:
        raise HTTPException(status_code=404, detail=f"No telemetry for device_id: {device_id}")
    return {
        "device_id": device_id,
        "range": range,
        "points": [
            TimeSeriesPoint(
                timestamp=row.timestamp,
                avg_power_watts=round(row.avg_power_watts, 2),
                co2_kg_h=round(row.co2_kg_h, 4),
                grid_ef=round(row.grid_ef, 4),
                rms_vib=round(row.rms_vib, 3),
                dye_tank_temp_c=round(row.dye_tank_temp_c, 2),
                fabric_temp_c=round(row.fabric_temp_c, 2),
                tension_grams=round(row.tension_grams, 2),
                wifi_rssi=round(row.wifi_rssi, 1),
            ).model_dump(mode="json")
            for row in rows
        ],
    }


@app.get("/machines/{device_id}/anomalies")
async def machine_anomalies(
    device_id: str,
    range: str = Query(default="24h", pattern=r"^\d+[mhd]$"),
    settings: Settings = Depends(settings_dep),
    telemetry: TelemetryRepository = Depends(telemetry_dep),
) -> dict[str, Any]:
    until = utc_now()
    since = until - parse_range(range)
    rows = await telemetry.series(device_id, since, until)
    if not rows:
        raise HTTPException(status_code=404, detail=f"No telemetry for device_id: {device_id}")
    latest = machine_from_telemetry(rows[-1], settings)
    anomalies = detect_power_anomalies(device_id, rows, settings)
    risk = summarize_risk(latest, anomalies, settings)
    return {
        "device_id": device_id,
        "range": range,
        "summary": risk.model_dump(mode="json"),
        "anomalies": [item.model_dump(mode="json") for item in anomalies],
    }


@app.get("/maintenance/procedures/{device_id}", response_model=MaintenanceProcedure)
async def maintenance_procedure(device_id: str) -> MaintenanceProcedure:
    suffix = device_id.rsplit("_", 1)[-1]
    return MaintenanceProcedure(
        procedure_id="loom_power_anomaly_v1",
        device_id=device_id,
        title=f"Loom {suffix} elevated-power inspection",
        steps=[
            MaintenanceStep(
                id=1,
                title="Isolate the loom",
                description="Pause the loom and confirm the operator panel is safe before touching moving parts.",
                anchorOffset=Vector3(x=-0.16, y=0.15, z=0.0),
            ),
            MaintenanceStep(
                id=2,
                title="Inspect fabric tension",
                description="Check that the tension sensor and fabric path are not jammed or over-tight.",
                anchorOffset=Vector3(x=0.0, y=0.22, z=-0.03),
            ),
            MaintenanceStep(
                id=3,
                title="Check shuttle rail friction",
                description="Look for lint buildup or dry rail movement that can increase current draw.",
                anchorOffset=Vector3(x=0.18, y=-0.04, z=0.02),
            ),
            MaintenanceStep(
                id=4,
                title="Verify current sensor",
                description="Confirm the ESP32 current clamp is seated and the WiFi RSSI is stable.",
                anchorOffset=Vector3(x=-0.08, y=0.08, z=0.06),
            ),
        ],
    )


@app.post("/maintenance/logs")
async def add_maintenance_log(
    request: MaintenanceLogRequest,
    store: MetadataStore = Depends(store_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    log_id = store.add_maintenance_log(request)
    return {"ok": True, "log_id": log_id}


@app.get("/maintenance/logs/{device_id}")
async def get_maintenance_logs(device_id: str, store: MetadataStore = Depends(store_dep)) -> dict[str, Any]:
    return {"device_id": device_id, "logs": store.maintenance_logs(device_id)}


@app.get("/training/modules/{device_type}", response_model=TrainingModule)
async def training_module(device_type: str) -> TrainingModule:
    return TrainingModule(
        device_type=device_type,
        components=[
            {
                "id": "tension_sensor",
                "label_en": "Tension sensor",
                "label_fr": "Capteur de tension",
                "label_ar": "حساس الشد",
                "anchor_offset": {"x": 0.0, "y": 0.2, "z": 0.0},
            },
            {
                "id": "heddle",
                "label_en": "Heddle",
                "label_fr": "Lisse",
                "label_ar": "درأة النول",
                "anchor_offset": {"x": 0.12, "y": 0.05, "z": 0.0},
            },
            {
                "id": "shuttle",
                "label_en": "Shuttle",
                "label_fr": "Navette",
                "label_ar": "مكوك",
                "anchor_offset": {"x": 0.18, "y": -0.05, "z": 0.0},
            },
        ],
        quiz=[
            {
                "question_en": "Tap the tension sensor",
                "question_fr": "Touchez le capteur de tension",
                "question_ar": "انقر على حساس الشد",
                "correct_component": "tension_sensor",
            },
            {
                "question_en": "Tap the shuttle",
                "question_fr": "Touchez la navette",
                "question_ar": "انقر على المكوك",
                "correct_component": "shuttle",
            },
        ],
    )


@app.post("/training/assessments")
async def submit_assessment(
    request: TrainingAssessmentRequest,
    store: MetadataStore = Depends(store_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    return store.add_training_assessment(request)


@app.get("/training/progress/{user_id}")
async def training_progress(user_id: str, store: MetadataStore = Depends(store_dep)) -> Any:
    return store.training_progress(user_id)


@app.post("/sessions", response_model=SessionResponse)
async def create_session(
    request: SessionCreateRequest,
    settings: Settings = Depends(settings_dep),
    store: MetadataStore = Depends(store_dep),
    _: None = Depends(protected_endpoint),
) -> SessionResponse:
    response = SessionResponse(
        session_id=uuid.uuid4().hex,
        device_id=request.device_id,
        created_at_utc=utc_now().isoformat(),
        stun_url=settings.stun_url,
        turn_url=settings.turn_url,
        turn_user=settings.turn_user,
        turn_secret=settings.turn_secret,
    )
    store.create_session(response, user_id=request.user_id)
    return response


@app.get("/sessions/{session_id}")
async def get_session(
    session_id: str,
    store: MetadataStore = Depends(store_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    session = store.get_session(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail=f"Unknown session_id: {session_id}")
    return session


@app.get("/sessions/{session_id}/recording")
async def session_recording(
    session_id: str,
    store: MetadataStore = Depends(store_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    session = store.get_session(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail=f"Unknown session_id: {session_id}")
    return {
        "session_id": session_id,
        "recording_url": None,
        "status": "not_recorded",
        "message": "Recording is intentionally deferred until WebRTC signaling is stable.",
    }


@app.websocket("/ws/ar-session/{session_id}")
async def ar_session_ws(
    websocket: WebSocket,
    session_id: str,
    token: str | None = Query(default=None),
    settings: Settings = Depends(settings_dep),
    store: MetadataStore = Depends(store_dep),
    hub: SessionHub = Depends(hub_dep),
) -> None:
    if not await require_websocket_token(websocket, settings, token=token):
        return
    if store.get_session(session_id) is None:
        await websocket.close(code=1008)
        return
    await hub.connect(session_id, websocket)
    await websocket.send_json({"type": "session_joined", "session_id": session_id})
    try:
        while True:
            raw = await websocket.receive_text()
            payload = normalize_ws_payload(json.loads(raw), session_id)
            message_id = store.add_session_message(session_id, payload)
            payload["message_id"] = message_id
            await websocket.send_json({"type": "message_received", "session_id": session_id, "message_id": message_id})
            await hub.broadcast(session_id, payload, sender=websocket)
    except WebSocketDisconnect:
        hub.disconnect(session_id, websocket)
    except json.JSONDecodeError:
        await websocket.send_json({"type": "error", "error": "invalid_json"})
        hub.disconnect(session_id, websocket)


@app.post("/assist/query")
async def assist_query(
    request: AssistQueryRequest,
    settings: Settings = Depends(settings_dep),
    telemetry: TelemetryRepository = Depends(telemetry_dep),
    store: MetadataStore = Depends(store_dep),
    assistant: AssistantClient = Depends(assistant_dep),
    _: None = Depends(protected_endpoint),
) -> Any:
    row = await telemetry.latest(request.device_id)
    if row is None:
        raise HTTPException(status_code=404, detail=f"Unknown device_id: {request.device_id}")
    until = utc_now()
    since = until - timedelta(hours=24)
    series = await telemetry.series(request.device_id, since, until)
    machine = machine_from_telemetry(row, settings)
    anomalies = detect_power_anomalies(request.device_id, series, settings) if request.include_recent_anomalies else []
    risk = summarize_risk(machine, anomalies, settings)
    context: dict[str, Any] = {
        "sources": ["snapshot"],
        "snapshot": machine.model_dump(mode="json"),
        "risk_summary": risk.model_dump(mode="json"),
    }
    if request.include_recent_anomalies:
        context["sources"].append("recent_anomalies")
        context["recent_anomalies"] = [item.model_dump(mode="json") for item in anomalies[-10:]]
    if request.include_maintenance_history:
        context["sources"].append("maintenance_history")
        context["maintenance_history"] = store.maintenance_logs(request.device_id, limit=10)
    return await assistant.answer_query(request.question, request.locale, risk, context)


@app.post("/assist/sessions/{session_id}/summary")
async def assist_session_summary(
    session_id: str,
    request: SessionTextRequest,
    store: MetadataStore = Depends(store_dep),
    assistant: AssistantClient = Depends(assistant_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    if store.get_session(session_id) is None:
        raise HTTPException(status_code=404, detail=f"Unknown session_id: {session_id}")
    messages = store.session_messages(session_id) if request.include_messages else []
    return await assistant.summarize_session(request.locale, messages, report=False)


@app.post("/assist/sessions/{session_id}/report")
async def assist_session_report(
    session_id: str,
    request: SessionTextRequest,
    store: MetadataStore = Depends(store_dep),
    assistant: AssistantClient = Depends(assistant_dep),
    _: None = Depends(protected_endpoint),
) -> dict[str, Any]:
    if store.get_session(session_id) is None:
        raise HTTPException(status_code=404, detail=f"Unknown session_id: {session_id}")
    messages = store.session_messages(session_id) if request.include_messages else []
    return await assistant.summarize_session(request.locale, messages, report=True)


def parse_range(value: str) -> timedelta:
    match = re.fullmatch(r"(\d+)([mhd])", value)
    if not match:
        raise HTTPException(status_code=400, detail="range must look like 30m, 24h, or 7d")
    amount = int(match.group(1))
    unit = match.group(2)
    if unit == "m":
        return timedelta(minutes=amount)
    if unit == "h":
        return timedelta(hours=amount)
    return timedelta(days=amount)


def normalize_ws_payload(payload: dict[str, Any], session_id: str) -> dict[str, Any]:
    payload = dict(payload)
    payload["session_id"] = session_id
    payload.setdefault("type", "message")
    if "world_pos" in payload and "local_pos" not in payload:
        payload["local_pos"] = payload.pop("world_pos")
    return payload
