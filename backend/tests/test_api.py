from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from app.config import get_settings
from app.main import app


@pytest.fixture()
def client(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> TestClient:
    monkeypatch.chdir(tmp_path)
    monkeypatch.setenv("SMARTEX_DATA_SOURCE", "mock")
    monkeypatch.setenv("SMARTEX_DB_PATH", str(tmp_path / "smartex_test.sqlite3"))
    monkeypatch.delenv("SMARTEX_API_TOKEN", raising=False)
    monkeypatch.delenv("REQUIRE_AUTH_FOR_SNAPSHOT", raising=False)
    monkeypatch.setenv("MISTRAL_API_KEY", "")
    get_settings.cache_clear()
    with TestClient(app) as test_client:
        yield test_client
    get_settings.cache_clear()


@pytest.fixture()
def authed_client(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> TestClient:
    monkeypatch.chdir(tmp_path)
    monkeypatch.setenv("SMARTEX_DATA_SOURCE", "mock")
    monkeypatch.setenv("SMARTEX_DB_PATH", str(tmp_path / "smartex_auth_test.sqlite3"))
    monkeypatch.setenv("SMARTEX_API_TOKEN", "secret-token")
    monkeypatch.delenv("REQUIRE_AUTH_FOR_SNAPSHOT", raising=False)
    monkeypatch.setenv("MISTRAL_API_KEY", "")
    get_settings.cache_clear()
    with TestClient(app) as test_client:
        yield test_client
    get_settings.cache_clear()


def test_health_and_snapshot_match_unity_relay_shape(client: TestClient) -> None:
    health = client.get("/health").json()
    assert health["ok"] is True
    assert health["data_source"] == "mock"

    payload = client.get("/snapshot").json()
    assert payload["ok"] is True
    assert payload["data"]["machines"]
    assert len(payload["data"]["machines"]) == 8
    assert payload["data"]["machines"][0]["device_id"].startswith("ESP32_TEX_")
    assert "total_power_kw" in payload["data"]["factory"]


def test_machine_timeseries_and_anomalies(client: TestClient) -> None:
    latest = client.get("/machines/ESP32_TEX_003/latest").json()
    assert latest["device_id"] == "ESP32_TEX_003"

    series = client.get("/machines/ESP32_TEX_003/timeseries?range=2h").json()
    assert len(series["points"]) > 10

    anomalies = client.get("/machines/ESP32_TEX_003/anomalies?range=2h").json()
    assert anomalies["summary"]["risk_level"] in {"warning", "critical"}
    assert anomalies["anomalies"]


def test_maintenance_training_and_progress(client: TestClient) -> None:
    procedure = client.get("/maintenance/procedures/ESP32_TEX_003").json()
    assert procedure["device_id"] == "ESP32_TEX_003"
    assert procedure["steps"][0]["anchorOffset"]["y"] > 0

    log = client.post(
        "/maintenance/logs",
        json={"device_id": "ESP32_TEX_003", "step_id": 1, "user_id": "u1"},
    ).json()
    assert log["ok"] is True
    logs = client.get("/maintenance/logs/ESP32_TEX_003").json()
    assert logs["logs"][0]["device_id"] == "ESP32_TEX_003"

    module = client.get("/training/modules/jacquard_loom").json()
    assert module["components"][0]["id"] == "tension_sensor"

    assessment = client.post(
        "/training/assessments",
        json={"user_id": "u1", "device_type": "jacquard_loom", "score": 0.85},
    ).json()
    assert assessment["passed"] is True
    progress = client.get("/training/progress/u1").json()
    assert progress["certifications"][0]["device_type"] == "jacquard_loom"


def test_assist_query_uses_deterministic_fallback_without_mistral_key(client: TestClient) -> None:
    response = client.post(
        "/assist/query",
        json={
            "device_id": "ESP32_TEX_003",
            "locale": "fr",
            "question": "Pourquoi cette machine est en alerte ?",
        },
    ).json()

    assert response["ai_provider"] == "deterministic"
    assert response["actions"]
    assert "snapshot" in response["sources"]


def test_session_websocket_and_summary(client: TestClient) -> None:
    session = client.post("/sessions", json={"device_id": "ESP32_TEX_001", "user_id": "tech"}).json()
    session_id = session["session_id"]

    with client.websocket_connect(f"/ws/ar-session/{session_id}") as ws:
        assert ws.receive_json()["type"] == "session_joined"
        ws.send_json(
            {
                "type": "annotation",
                "device_id": "ESP32_TEX_001",
                "local_pos": {"x": 0.1, "y": 0.2, "z": 0.0},
                "text": "Check belt tension",
                "author": "remote_expert",
            }
        )
        ack = ws.receive_json()
        assert ack["type"] == "message_received"

    summary = client.post(f"/assist/sessions/{session_id}/summary", json={"locale": "en"}).json()
    assert summary["message_count"] == 1


def test_protected_routes_require_token_when_configured(authed_client: TestClient) -> None:
    health = authed_client.get("/health").json()
    assert health["auth_enabled"] is True

    # Keep Unity's demo relay path open unless REQUIRE_AUTH_FOR_SNAPSHOT=true.
    assert authed_client.get("/snapshot").status_code == 200

    denied = authed_client.post(
        "/maintenance/logs",
        json={"device_id": "ESP32_TEX_003", "step_id": 1, "user_id": "u1"},
    )
    assert denied.status_code == 401

    allowed = authed_client.post(
        "/maintenance/logs",
        json={"device_id": "ESP32_TEX_003", "step_id": 1, "user_id": "u1"},
        headers={"x-smartex-token": "secret-token"},
    )
    assert allowed.status_code == 200
    assert allowed.json()["ok"] is True


def test_snapshot_can_be_protected_when_enabled(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.chdir(tmp_path)
    monkeypatch.setenv("SMARTEX_DATA_SOURCE", "mock")
    monkeypatch.setenv("SMARTEX_DB_PATH", str(tmp_path / "smartex_snapshot_auth_test.sqlite3"))
    monkeypatch.setenv("SMARTEX_API_TOKEN", "secret-token")
    monkeypatch.setenv("REQUIRE_AUTH_FOR_SNAPSHOT", "true")
    get_settings.cache_clear()

    with TestClient(app) as protected_client:
        assert protected_client.get("/snapshot").status_code == 401
        response = protected_client.get(
            "/snapshot",
            headers={"Authorization": "Bearer secret-token"},
        )
        assert response.status_code == 200
        assert len(response.json()["data"]["machines"]) == 8

    get_settings.cache_clear()


def test_websocket_relay_normalizes_legacy_world_pos(authed_client: TestClient) -> None:
    session = authed_client.post(
        "/sessions",
        json={"device_id": "ESP32_TEX_001", "user_id": "tech"},
        headers={"x-smartex-token": "secret-token"},
    ).json()
    session_id = session["session_id"]

    with authed_client.websocket_connect(f"/ws/ar-session/{session_id}?token=secret-token") as sender:
        assert sender.receive_json()["type"] == "session_joined"
        with authed_client.websocket_connect(f"/ws/ar-session/{session_id}?token=secret-token") as receiver:
            assert receiver.receive_json()["type"] == "session_joined"

            sender.send_json(
                {
                    "type": "annotation",
                    "device_id": "ESP32_TEX_001",
                    "world_pos": {"x": 0.1, "y": 0.2, "z": 0.0},
                    "text": "Check belt tension",
                    "author": "remote_expert",
                }
            )

            assert sender.receive_json()["type"] == "message_received"
            broadcast = receiver.receive_json()
            assert broadcast["type"] == "annotation"
            assert broadcast["local_pos"] == {"x": 0.1, "y": 0.2, "z": 0.0}
            assert "world_pos" not in broadcast
