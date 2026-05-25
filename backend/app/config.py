from functools import lru_cache
from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=("../../.env", ".env"),
        env_prefix="",
        case_sensitive=False,
        extra="ignore",
    )

    app_name: str = "SmartexVR Backend"
    environment: str = "local"

    smartex_data_source: str = Field(default="mock")
    smartex_db_path: Path = Field(default=Path("./data/smartex_backend.sqlite3"))
    smartex_api_token: str | None = None
    require_auth_for_snapshot: bool = False
    device_count: int = 8
    stale_after_seconds: int = 30 * 60

    grid_emission_factor: float = 0.742
    carbon_price_eur: float = 65.0
    eur_to_mad: float = 10.8
    power_warning_watts: float = 750.0
    power_critical_watts: float = 900.0
    anomaly_window_points: int = 12
    anomaly_mad_threshold: float = 3.5

    influx_url: str | None = None
    influx_token: str | None = None
    influx_org: str | None = None
    influx_bucket: str | None = None
    influx_measurement: str = "smartex_derived"

    mistral_api_key: str | None = None
    mistral_model: str = "mistral-medium-latest"
    mistral_timeout_seconds: float = 20.0
    mistral_chat_url: str = "https://api.mistral.ai/v1/chat/completions"

    stun_url: str = "stun:stun.l.google.com:19302"
    turn_url: str | None = None
    turn_user: str | None = None
    turn_secret: str | None = None

    @property
    def device_ids(self) -> list[str]:
        return [f"ESP32_TEX_{i:03d}" for i in range(1, self.device_count + 1)]


@lru_cache
def get_settings() -> Settings:
    return Settings()
