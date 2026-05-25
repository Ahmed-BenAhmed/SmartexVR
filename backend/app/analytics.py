from __future__ import annotations

import statistics
from datetime import datetime, timezone

from app.config import Settings
from app.models import AnomalyRecord, MachineData, MachineTelemetry, RiskSummary


def machine_from_telemetry(
    row: MachineTelemetry,
    settings: Settings,
    now: datetime | None = None,
) -> MachineData:
    now = now or datetime.now(timezone.utc)
    ts = row.timestamp.astimezone(timezone.utc)
    suffix = row.device_id.rsplit("_", 1)[-1]
    age_seconds = (now - ts).total_seconds()
    return MachineData(
        device_id=row.device_id,
        machine_id=f"MTX-{suffix}",
        display_name=f"Loom {suffix}",
        shift=shift_for(ts),
        avg_power_watts=round(row.avg_power_watts, 2),
        co2_kg_h=round(row.co2_kg_h, 4),
        grid_ef=round(row.grid_ef, 4),
        rms_vib=round(row.rms_vib, 3),
        dye_tank_temp_c=round(row.dye_tank_temp_c, 2),
        fabric_temp_c=round(row.fabric_temp_c, 2),
        tension_grams=round(row.tension_grams, 2),
        wifi_rssi=round(row.wifi_rssi, 1),
        is_online=age_seconds <= settings.stale_after_seconds,
        last_seen=ts.isoformat(),
    )


def shift_for(ts: datetime) -> str:
    hour = ts.astimezone(timezone.utc).hour
    if 6 <= hour < 14:
        return "morning"
    if 14 <= hour < 22:
        return "afternoon"
    return "night"


def health_score(power_watts: float) -> float:
    if power_watts <= 0:
        return 0.0
    return max(0.0, min(1.0, 1.0 - (power_watts - 400.0) / 600.0))


def detect_power_anomalies(
    device_id: str,
    points: list[MachineTelemetry],
    settings: Settings,
) -> list[AnomalyRecord]:
    ordered = sorted(points, key=lambda p: p.timestamp)
    anomalies: list[AnomalyRecord] = []
    window = max(4, settings.anomaly_window_points)

    for index in range(window, len(ordered)):
        history = [p.avg_power_watts for p in ordered[index - window : index]]
        current = ordered[index]
        median = statistics.median(history)
        deviations = [abs(v - median) for v in history]
        mad = statistics.median(deviations)
        if mad <= 0.001:
            score = 0.0 if abs(current.avg_power_watts - median) < 1.0 else 999.0
        else:
            score = abs(current.avg_power_watts - median) / (1.4826 * mad)

        above_threshold = score >= settings.anomaly_mad_threshold
        above_power_limit = current.avg_power_watts >= settings.power_critical_watts
        if not (above_threshold or above_power_limit):
            continue

        severity = (
            "critical"
            if above_power_limit or score >= settings.anomaly_mad_threshold * 1.5
            else "warning"
        )
        delta = current.avg_power_watts - median
        direction = "above" if delta >= 0 else "below"
        anomalies.append(
            AnomalyRecord(
                device_id=device_id,
                timestamp=current.timestamp,
                value=round(current.avg_power_watts, 2),
                baseline=round(median, 2),
                score=round(score, 2),
                severity=severity,
                risk_level=severity,
                message=(
                    f"Power is {abs(delta):.0f} W {direction} the rolling "
                    f"baseline ({median:.0f} W)."
                ),
            )
        )

    return anomalies


def summarize_risk(
    machine: MachineData,
    recent_anomalies: list[AnomalyRecord],
    settings: Settings,
) -> RiskSummary:
    score = health_score(machine.avg_power_watts)
    if not machine.is_online:
        return RiskSummary(
            device_id=machine.device_id,
            risk_level="offline",
            health_score=score,
            explanation="Machine has not reported telemetry within the online window.",
            actions=[
                "Check ESP32 power and WiFi connectivity.",
                "Verify the loom gateway can reach InfluxDB.",
            ],
            latest_anomalies=recent_anomalies[-5:],
        )

    critical_anomalies = [a for a in recent_anomalies if a.severity == "critical"]
    if machine.avg_power_watts >= settings.power_critical_watts or critical_anomalies:
        return RiskSummary(
            device_id=machine.device_id,
            risk_level="critical",
            health_score=score,
            explanation="Power draw is in the critical band or repeatedly outside the rolling baseline.",
            actions=[
                "Inspect fabric tension and current sensor placement.",
                "Check for mechanical friction around heddle and shuttle rails.",
                "Schedule maintenance if the alert persists for another polling cycle.",
            ],
            latest_anomalies=recent_anomalies[-5:],
        )

    if machine.avg_power_watts >= settings.power_warning_watts or recent_anomalies:
        return RiskSummary(
            device_id=machine.device_id,
            risk_level="warning",
            health_score=score,
            explanation="Power draw is elevated compared with the normal operating profile.",
            actions=[
                "Compare current power with the last 30 minutes.",
                "Inspect tension sensor readings and operator notes.",
            ],
            latest_anomalies=recent_anomalies[-5:],
        )

    return RiskSummary(
        device_id=machine.device_id,
        risk_level="normal",
        health_score=score,
        explanation="Machine is online and power remains near the rolling baseline.",
        actions=["Continue routine monitoring."],
        latest_anomalies=recent_anomalies[-5:],
    )
