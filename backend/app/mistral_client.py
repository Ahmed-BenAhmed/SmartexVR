from __future__ import annotations

import json
from typing import Any

import httpx

from app.config import Settings
from app.models import AssistResponse, RiskSummary, StoredMessage


PROMPT_INVARIANT = """Use only the provided machine snapshot, anomaly records, procedures, and session messages.
If evidence is insufficient, say what to inspect next.
Never invent sensor readings.
Return concise actionable advice for a technician."""


class AssistantClient:
    def __init__(self, settings: Settings) -> None:
        self.settings = settings

    @property
    def is_configured(self) -> bool:
        return bool(self.settings.mistral_api_key)

    async def answer_query(
        self,
        question: str,
        locale: str,
        risk: RiskSummary,
        context: dict[str, Any],
    ) -> AssistResponse:
        if not self.is_configured:
            return self._deterministic_response(risk, context)

        payload = {
            "model": self.settings.mistral_model,
            "messages": [
                {"role": "system", "content": PROMPT_INVARIANT},
                {
                    "role": "user",
                    "content": json.dumps(
                        {
                            "locale": locale,
                            "question": question,
                            "context": context,
                            "response_schema": {
                                "answer": "string",
                                "actions": ["string"],
                                "risk_level": risk.risk_level,
                                "sources": ["snapshot"],
                            },
                        },
                        ensure_ascii=False,
                    ),
                },
            ],
            "temperature": 0.2,
            "max_tokens": 450,
        }
        try:
            content = await self._complete(payload)
        except httpx.HTTPError:
            return self._deterministic_response(risk, context)
        parsed = self._parse_json_content(content)
        if parsed is None:
            return AssistResponse(
                answer=content.strip() or self._deterministic_response(risk, context).answer,
                actions=risk.actions,
                risk_level=risk.risk_level,
                sources=context.get("sources", ["snapshot"]),
                ai_provider="mistral",
            )
        return AssistResponse(
            answer=str(parsed.get("answer") or self._deterministic_response(risk, context).answer),
            actions=[str(item) for item in parsed.get("actions", risk.actions)],
            risk_level=parsed.get("risk_level") or risk.risk_level,
            sources=[str(item) for item in parsed.get("sources", context.get("sources", ["snapshot"]))],
            ai_provider="mistral",
        )

    async def summarize_session(
        self,
        locale: str,
        messages: list[StoredMessage],
        report: bool = False,
    ) -> dict[str, Any]:
        if not self.is_configured:
            return self._deterministic_session_summary(messages, report=report)

        payload = {
            "model": self.settings.mistral_model,
            "messages": [
                {"role": "system", "content": PROMPT_INVARIANT},
                {
                    "role": "user",
                    "content": json.dumps(
                        {
                            "locale": locale,
                            "task": "draft_maintenance_report" if report else "summarize_remote_assist_session",
                            "messages": [msg.model_dump(mode="json") for msg in messages],
                        },
                        ensure_ascii=False,
                    ),
                },
            ],
            "temperature": 0.2,
            "max_tokens": 700,
        }
        try:
            content = await self._complete(payload)
        except httpx.HTTPError:
            return self._deterministic_session_summary(messages, report=report)
        return {
            "ai_provider": "mistral",
            "summary" if not report else "report": content.strip(),
            "message_count": len(messages),
        }

    async def _complete(self, payload: dict[str, Any]) -> str:
        headers = {
            "Authorization": f"Bearer {self.settings.mistral_api_key}",
            "Content-Type": "application/json",
        }
        async with httpx.AsyncClient(timeout=self.settings.mistral_timeout_seconds) as client:
            response = await client.post(self.settings.mistral_chat_url, headers=headers, json=payload)
            response.raise_for_status()
            body = response.json()

        message = body.get("choices", [{}])[0].get("message", {})
        content = message.get("content", "")
        if isinstance(content, list):
            return "".join(str(part.get("text", "")) for part in content if isinstance(part, dict))
        return str(content)

    def _deterministic_response(self, risk: RiskSummary, context: dict[str, Any]) -> AssistResponse:
        source_names = context.get("sources", ["snapshot"])
        return AssistResponse(
            answer=f"{risk.explanation} First action: {risk.actions[0] if risk.actions else 'continue monitoring'}",
            actions=risk.actions,
            risk_level=risk.risk_level,
            sources=source_names,
            ai_provider="deterministic",
        )

    @staticmethod
    def _deterministic_session_summary(messages: list[StoredMessage], report: bool = False) -> dict[str, Any]:
        lines = [
            f"{msg.author or 'participant'}: {msg.payload.get('text') or msg.type}"
            for msg in messages
            if msg.type in {"message", "annotation", "agent_recommendation"}
        ]
        summary = "No session messages recorded yet." if not lines else " / ".join(lines[-5:])
        if report:
            summary = f"Maintenance report draft: {summary}"
        return {
            "ai_provider": "deterministic",
            "summary": summary,
            "message_count": len(messages),
        }

    @staticmethod
    def _parse_json_content(content: str) -> dict[str, Any] | None:
        text = content.strip()
        if text.startswith("```"):
            text = text.strip("`")
            if text.startswith("json"):
                text = text[4:].strip()
        try:
            parsed = json.loads(text)
        except json.JSONDecodeError:
            return None
        return parsed if isinstance(parsed, dict) else None
