from __future__ import annotations

import asyncio
import math
import random
from datetime import datetime, timedelta, timezone
from typing import Protocol

from app.config import Settings
from app.models import MachineTelemetry


class TelemetryRepository(Protocol):
    async def latest_all(self) -> list[MachineTelemetry]:
        ...

    async def latest(self, device_id: str) -> MachineTelemetry | None:
        ...

    async def series(
        self,
        device_id: str,
        since: datetime,
        until: datetime,
    ) -> list[MachineTelemetry]:
        ...


class MockTelemetryRepository:
    def __init__(self, settings: Settings) -> None:
        self.settings = settings

    async def latest_all(self) -> list[MachineTelemetry]:
        now = datetime.now(timezone.utc)
        return [self._point(device_id, self._latest_timestamp(device_id, now)) for device_id in self.settings.device_ids]

    async def latest(self, device_id: str) -> MachineTelemetry | None:
        if device_id not in self.settings.device_ids:
            return None
        now = datetime.now(timezone.utc)
        return self._point(device_id, self._latest_timestamp(device_id, now))

    async def series(
        self,
        device_id: str,
        since: datetime,
        until: datetime,
    ) -> list[MachineTelemetry]:
        if device_id not in self.settings.device_ids:
            return []
        since = since.astimezone(timezone.utc)
        until = until.astimezone(timezone.utc)
        step = timedelta(minutes=5)
        points: list[MachineTelemetry] = []
        cursor = since.replace(second=0, microsecond=0)
        minute_mod = cursor.minute % 5
        if minute_mod:
            cursor += timedelta(minutes=5 - minute_mod)
        while cursor <= until:
            if device_id == "ESP32_TEX_006" and cursor > until - timedelta(minutes=45):
                cursor += step
                continue
            points.append(self._point(device_id, cursor))
            cursor += step
        return points

    def _latest_timestamp(self, device_id: str, now: datetime) -> datetime:
        if device_id == "ESP32_TEX_006":
            return now - timedelta(minutes=45)
        return now

    def _point(self, device_id: str, ts: datetime) -> MachineTelemetry:
        index = int(device_id.rsplit("_", 1)[-1])
        minute_of_day = ts.hour * 60 + ts.minute
        phase = (minute_of_day / 1440.0) * 2.0 * math.pi
        base = 460.0 + index * 34.0
        daily = math.sin(phase + index * 0.37) * 38.0
        rng = random.Random(f"{device_id}:{ts.strftime('%Y%m%d%H%M')}")
        noise = rng.uniform(-18.0, 18.0)
        power = base + daily + noise

        if device_id == "ESP32_TEX_003" and ts >= datetime.now(timezone.utc) - timedelta(minutes=50):
            power += 330.0
        if device_id == "ESP32_TEX_008" and 10 <= ts.hour <= 11:
            power += 180.0

        power = max(0.0, power)
        grid_ef = self.settings.grid_emission_factor
        co2_kg_h = (power / 1000.0) * grid_ef
        wear = min(1.0, max(0.0, (power - 430.0) / 620.0))
        return MachineTelemetry(
            device_id=device_id,
            timestamp=ts.astimezone(timezone.utc),
            avg_power_watts=power,
            co2_kg_h=co2_kg_h,
            grid_ef=grid_ef,
            rms_vib=2.2 + wear * 6.2 + rng.uniform(-0.15, 0.15),
            dye_tank_temp_c=58.0 + wear * 13.0 + rng.uniform(-0.5, 0.5),
            fabric_temp_c=40.0 + wear * 6.0 + rng.uniform(-0.3, 0.3),
            tension_grams=22.0 + wear * 8.0 + rng.uniform(-0.4, 0.4),
            wifi_rssi=-54.0 - index * 2.7 + rng.uniform(-4.0, 2.0),
        )


class InfluxTelemetryRepository:
    def __init__(self, settings: Settings) -> None:
        self.settings = settings
        if not all([settings.influx_url, settings.influx_token, settings.influx_org, settings.influx_bucket]):
            raise ValueError("InfluxDB source selected but INFLUX_URL/TOKEN/ORG/BUCKET are not fully configured.")

    async def latest_all(self) -> list[MachineTelemetry]:
        rows = await asyncio.gather(*(self.latest(device_id) for device_id in self.settings.device_ids))
        return [row for row in rows if row is not None]

    async def latest(self, device_id: str) -> MachineTelemetry | None:
        until = datetime.now(timezone.utc)
        since = until - timedelta(hours=48)
        rows = await self.series(device_id, since, until)
        return rows[-1] if rows else None

    async def series(
        self,
        device_id: str,
        since: datetime,
        until: datetime,
    ) -> list[MachineTelemetry]:
        return await asyncio.to_thread(self._query_series, device_id, since, until)

    def _query_series(
        self,
        device_id: str,
        since: datetime,
        until: datetime,
    ) -> list[MachineTelemetry]:
        try:
            from influxdb_client import InfluxDBClient
        except ImportError as exc:
            raise RuntimeError("Install influxdb-client to use SMARTEX_DATA_SOURCE=influx.") from exc

        fields = [
            "avg_power_watts",
            "co2_kg_h",
            "grid_ef",
            "rms_vib",
            "dye_tank_temp_c",
            "fabric_temp_c",
            "tension_grams",
            "wifi_rssi",
        ]
        field_filter = " or ".join([f'r._field == "{field}"' for field in fields])
        flux = f'''
from(bucket: "{self.settings.influx_bucket}")
  |> range(start: time(v: "{since.isoformat()}"), stop: time(v: "{until.isoformat()}"))
  |> filter(fn: (r) => r._measurement == "{self.settings.influx_measurement}")
  |> filter(fn: (r) => r.device_id == "{device_id}")
  |> filter(fn: (r) => {field_filter})
  |> pivot(rowKey: ["_time"], columnKey: ["_field"], valueColumn: "_value")
  |> sort(columns: ["_time"])
'''
        with InfluxDBClient(
            url=self.settings.influx_url,
            token=self.settings.influx_token,
            org=self.settings.influx_org,
        ) as client:
            tables = client.query_api().query(flux)

        rows: list[MachineTelemetry] = []
        for table in tables:
            for record in table.records:
                values = record.values
                timestamp = values.get("_time")
                if timestamp is None or values.get("avg_power_watts") is None:
                    continue
                power = float(values.get("avg_power_watts") or 0.0)
                grid_ef = float(values.get("grid_ef") or self.settings.grid_emission_factor)
                rows.append(
                    MachineTelemetry(
                        device_id=device_id,
                        timestamp=timestamp,
                        avg_power_watts=power,
                        co2_kg_h=float(values.get("co2_kg_h") or (power / 1000.0) * grid_ef),
                        grid_ef=grid_ef,
                        rms_vib=float(values.get("rms_vib") or 0.0),
                        dye_tank_temp_c=float(values.get("dye_tank_temp_c") or 0.0),
                        fabric_temp_c=float(values.get("fabric_temp_c") or 0.0),
                        tension_grams=float(values.get("tension_grams") or 0.0),
                        wifi_rssi=float(values.get("wifi_rssi") or 0.0),
                    )
                )
        return rows


def build_telemetry_repository(settings: Settings) -> TelemetryRepository:
    if settings.smartex_data_source.lower() == "influx":
        return InfluxTelemetryRepository(settings)
    return MockTelemetryRepository(settings)
