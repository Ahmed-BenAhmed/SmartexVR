from datetime import datetime, timezone
from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


class Vector3(BaseModel):
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0


class MachineTelemetry(BaseModel):
    device_id: str
    timestamp: datetime
    avg_power_watts: float
    co2_kg_h: float
    grid_ef: float
    rms_vib: float = 0.0
    dye_tank_temp_c: float = 0.0
    fabric_temp_c: float = 0.0
    tension_grams: float = 0.0
    wifi_rssi: float = 0.0


class MachineData(BaseModel):
    device_id: str
    machine_id: str
    display_name: str
    shift: str = "morning"
    avg_power_watts: float
    co2_kg_h: float
    grid_ef: float
    rms_vib: float = 0.0
    dye_tank_temp_c: float = 0.0
    fabric_temp_c: float = 0.0
    tension_grams: float = 0.0
    wifi_rssi: float = 0.0
    is_online: bool
    last_seen: str


class FactoryStats(BaseModel):
    total_power_kw: float = 0.0
    total_co2_today_kg: float = 0.0
    cbam_exposure_mad: float = 0.0


class FactorySnapshot(BaseModel):
    timestamp: str
    machines: list[MachineData] = Field(default_factory=list)
    factory: FactoryStats = Field(default_factory=FactoryStats)


class RelayResponse(BaseModel):
    ok: bool = True
    error: str | None = None
    data: FactorySnapshot | None = None


class TimeSeriesPoint(BaseModel):
    timestamp: datetime
    avg_power_watts: float
    co2_kg_h: float
    grid_ef: float
    rms_vib: float = 0.0
    dye_tank_temp_c: float = 0.0
    fabric_temp_c: float = 0.0
    tension_grams: float = 0.0
    wifi_rssi: float = 0.0


class AnomalyRecord(BaseModel):
    device_id: str
    timestamp: datetime
    metric: str = "avg_power_watts"
    value: float
    baseline: float
    score: float
    severity: Literal["warning", "critical"]
    risk_level: Literal["warning", "critical"]
    message: str


class RiskSummary(BaseModel):
    device_id: str
    risk_level: Literal["normal", "warning", "critical", "offline"]
    health_score: float
    explanation: str
    actions: list[str]
    latest_anomalies: list[AnomalyRecord] = Field(default_factory=list)


class MaintenanceStep(BaseModel):
    id: int
    title: str
    description: str
    anchorOffset: Vector3


class MaintenanceProcedure(BaseModel):
    procedure_id: str
    device_id: str
    title: str
    schema_version: int = 1
    steps: list[MaintenanceStep]


class MaintenanceLogRequest(BaseModel):
    model_config = ConfigDict(extra="allow")

    device_id: str
    procedure_id: str | None = None
    user_id: str | None = None
    step_id: int | None = None
    completed_steps: list[int] | None = None
    completed_at: str | None = None
    completed_at_utc: datetime | None = None


class ComponentLabel(BaseModel):
    id: str
    label_en: str
    label_fr: str
    label_ar: str
    anchor_offset: Vector3


class QuizQuestion(BaseModel):
    question_en: str
    question_fr: str
    question_ar: str
    correct_component: str


class TrainingModule(BaseModel):
    device_type: str
    components: list[ComponentLabel]
    quiz: list[QuizQuestion]


class TrainingAssessmentRequest(BaseModel):
    model_config = ConfigDict(extra="allow")

    user_id: str | None = None
    device_id: str | None = None
    device_type: str | None = None
    score: float | None = None
    score_percent: int | None = None
    duration_seconds: int | None = None
    completed_at: str | None = None
    completed_at_utc: datetime | None = None


class CertifiedModule(BaseModel):
    device_type: str
    score_percent: int
    completed_at_utc: str


class UserProgress(BaseModel):
    user_id: str
    certifications: list[CertifiedModule] = Field(default_factory=list)


class SessionCreateRequest(BaseModel):
    device_id: str
    user_id: str | None = None


class SessionResponse(BaseModel):
    session_id: str
    device_id: str
    status: str = "active"
    created_at_utc: str
    stun_url: str
    turn_url: str | None = None
    turn_user: str | None = None
    turn_secret: str | None = None


class AssistQueryRequest(BaseModel):
    device_id: str
    locale: Literal["en", "fr", "ar"] = "fr"
    question: str
    include_recent_anomalies: bool = True
    include_maintenance_history: bool = True


class AssistResponse(BaseModel):
    answer: str
    actions: list[str]
    risk_level: Literal["normal", "warning", "critical", "offline"]
    sources: list[str]
    ai_provider: Literal["deterministic", "mistral"]


class SessionTextRequest(BaseModel):
    locale: Literal["en", "fr", "ar"] = "fr"
    include_messages: bool = True


class StoredMessage(BaseModel):
    id: int
    session_id: str
    created_at_utc: str
    type: str
    author: str | None = None
    payload: dict[str, Any]
