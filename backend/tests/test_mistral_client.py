import asyncio

import httpx

from app.config import Settings
from app.mistral_client import AssistantClient
from app.models import RiskSummary, StoredMessage


def test_mistral_answer_query_parses_json_response() -> None:
    client = AssistantClient(Settings(mistral_api_key="test-key"))
    risk = RiskSummary(
        device_id="ESP32_TEX_003",
        risk_level="warning",
        health_score=0.55,
        explanation="Power is elevated.",
        actions=["Inspect tension sensor."],
    )

    async def fake_complete(payload: dict) -> str:
        assert payload["messages"][0]["role"] == "system"
        return '{"answer":"Inspect the tension path.","actions":["Check sensor"],"risk_level":"warning","sources":["snapshot"]}'

    client._complete = fake_complete  # type: ignore[method-assign]

    response = asyncio.run(
        client.answer_query(
            question="Pourquoi cette machine est en alerte ?",
            locale="fr",
            risk=risk,
            context={"sources": ["snapshot"], "snapshot": {"device_id": "ESP32_TEX_003"}},
        )
    )

    assert response.ai_provider == "mistral"
    assert response.answer == "Inspect the tension path."
    assert response.actions == ["Check sensor"]


def test_mistral_answer_query_falls_back_when_response_is_plain_text() -> None:
    client = AssistantClient(Settings(mistral_api_key="test-key"))
    risk = RiskSummary(
        device_id="ESP32_TEX_003",
        risk_level="critical",
        health_score=0.25,
        explanation="Power is critical.",
        actions=["Stop the loom safely."],
    )

    async def fake_complete(payload: dict) -> str:
        return "Stop the loom and inspect the current sensor."

    client._complete = fake_complete  # type: ignore[method-assign]

    response = asyncio.run(
        client.answer_query(
            question="What now?",
            locale="en",
            risk=risk,
            context={"sources": ["snapshot"]},
        )
    )

    assert response.ai_provider == "mistral"
    assert response.risk_level == "critical"
    assert response.actions == ["Stop the loom safely."]
    assert "current sensor" in response.answer


def test_mistral_answer_query_falls_back_to_deterministic_on_http_error() -> None:
    client = AssistantClient(Settings(mistral_api_key="test-key"))
    risk = RiskSummary(
        device_id="ESP32_TEX_003",
        risk_level="critical",
        health_score=0.25,
        explanation="Power is critical.",
        actions=["Stop the loom safely."],
    )

    async def fake_complete(payload: dict) -> str:
        raise httpx.ReadTimeout("timeout")

    client._complete = fake_complete  # type: ignore[method-assign]

    response = asyncio.run(
        client.answer_query(
            question="What now?",
            locale="en",
            risk=risk,
            context={"sources": ["snapshot"]},
        )
    )

    assert response.ai_provider == "deterministic"
    assert response.risk_level == "critical"
    assert response.actions == ["Stop the loom safely."]


def test_mistral_session_summary_falls_back_to_deterministic_on_http_error() -> None:
    client = AssistantClient(Settings(mistral_api_key="test-key"))
    messages = [
        StoredMessage(
            id=1,
            session_id="s1",
            created_at_utc="2026-05-25T00:00:00Z",
            type="annotation",
            author="expert",
            payload={"text": "Check belt tension"},
        )
    ]

    async def fake_complete(payload: dict) -> str:
        raise httpx.ReadTimeout("timeout")

    client._complete = fake_complete  # type: ignore[method-assign]

    response = asyncio.run(client.summarize_session("en", messages))

    assert response["ai_provider"] == "deterministic"
    assert response["message_count"] == 1
    assert "Check belt tension" in response["summary"]
