from datetime import datetime, timedelta, timezone

from app.analytics import detect_power_anomalies, machine_from_telemetry, summarize_risk
from app.config import Settings
from app.models import MachineTelemetry


def test_detect_power_anomalies_flags_spike() -> None:
    settings = Settings()
    start = datetime.now(timezone.utc) - timedelta(hours=2)
    points = [
        MachineTelemetry(
            device_id="ESP32_TEX_001",
            timestamp=start + timedelta(minutes=5 * index),
            avg_power_watts=500.0 if index != 20 else 980.0,
            co2_kg_h=0.35,
            grid_ef=0.7,
        )
        for index in range(30)
    ]

    anomalies = detect_power_anomalies("ESP32_TEX_001", points, settings)

    assert anomalies
    assert anomalies[-1].severity == "critical"


def test_risk_summary_marks_offline_machine() -> None:
    settings = Settings(stale_after_seconds=60)
    stale = datetime.now(timezone.utc) - timedelta(minutes=10)
    machine = machine_from_telemetry(
        MachineTelemetry(
            device_id="ESP32_TEX_001",
            timestamp=stale,
            avg_power_watts=520.0,
            co2_kg_h=0.38,
            grid_ef=0.742,
        ),
        settings,
    )

    risk = summarize_risk(machine, [], settings)

    assert risk.risk_level == "offline"
    assert "WiFi" in " ".join(risk.actions)
