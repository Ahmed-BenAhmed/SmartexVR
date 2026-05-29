from __future__ import annotations

import json
import sqlite3
import threading
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from app.models import (
    CertifiedModule,
    MaintenanceLogRequest,
    SessionResponse,
    StoredMessage,
    TrainingAssessmentRequest,
    UserProgress,
)


def iso_now() -> str:
    return datetime.now(timezone.utc).isoformat()


class MetadataStore:
    def __init__(self, db_path: Path) -> None:
        self.db_path = db_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._lock = threading.RLock()
        self._init_schema()

    def _connect(self) -> sqlite3.Connection:
        conn = sqlite3.connect(self.db_path, check_same_thread=False)
        conn.row_factory = sqlite3.Row
        return conn

    def _init_schema(self) -> None:
        with self._lock, self._connect() as conn:
            conn.executescript(
                """
                create table if not exists maintenance_logs (
                    id integer primary key autoincrement,
                    device_id text not null,
                    procedure_id text,
                    user_id text,
                    step_id integer,
                    completed_steps_json text,
                    completed_at_utc text not null,
                    payload_json text not null,
                    created_at_utc text not null
                );

                create table if not exists training_assessments (
                    id integer primary key autoincrement,
                    user_id text not null,
                    device_id text,
                    device_type text not null,
                    score_percent integer not null,
                    duration_seconds integer,
                    passed integer not null,
                    completed_at_utc text not null,
                    payload_json text not null,
                    created_at_utc text not null
                );

                create table if not exists sessions (
                    session_id text primary key,
                    device_id text not null,
                    user_id text,
                    status text not null,
                    created_at_utc text not null,
                    ended_at_utc text
                );

                create table if not exists session_messages (
                    id integer primary key autoincrement,
                    session_id text not null,
                    type text not null,
                    author text,
                    payload_json text not null,
                    created_at_utc text not null,
                    foreign key(session_id) references sessions(session_id)
                );
                """
            )

    def add_maintenance_log(self, request: MaintenanceLogRequest) -> int:
        payload = request.model_dump(mode="json")
        completed_at = request.completed_at_utc.isoformat() if request.completed_at_utc else request.completed_at
        completed_at = completed_at or iso_now()
        steps = request.completed_steps
        if steps is None and request.step_id is not None:
            steps = [request.step_id]
        with self._lock, self._connect() as conn:
            cursor = conn.execute(
                """
                insert into maintenance_logs
                    (device_id, procedure_id, user_id, step_id, completed_steps_json,
                     completed_at_utc, payload_json, created_at_utc)
                values (?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    request.device_id,
                    request.procedure_id,
                    request.user_id,
                    request.step_id,
                    json.dumps(steps or []),
                    completed_at,
                    json.dumps(payload),
                    iso_now(),
                ),
            )
            return int(cursor.lastrowid)

    def maintenance_logs(self, device_id: str, limit: int = 50) -> list[dict[str, Any]]:
        with self._lock, self._connect() as conn:
            rows = conn.execute(
                """
                select * from maintenance_logs
                where device_id = ?
                order by completed_at_utc desc
                limit ?
                """,
                (device_id, limit),
            ).fetchall()
        return [self._row_to_dict(row) for row in rows]

    def add_training_assessment(self, request: TrainingAssessmentRequest) -> dict[str, Any]:
        raw_score = request.score_percent if request.score_percent is not None else request.score
        score_percent = int(round((raw_score or 0.0) * 100)) if raw_score is not None and raw_score <= 1 else int(round(raw_score or 0.0))
        score_percent = max(0, min(100, score_percent))
        passed = score_percent >= 70
        user_id = request.user_id or "local-device"
        device_type = request.device_type or "jacquard_loom"
        completed_at = request.completed_at_utc.isoformat() if request.completed_at_utc else request.completed_at
        completed_at = completed_at or iso_now()
        payload = request.model_dump(mode="json")
        with self._lock, self._connect() as conn:
            cursor = conn.execute(
                """
                insert into training_assessments
                    (user_id, device_id, device_type, score_percent, duration_seconds,
                     passed, completed_at_utc, payload_json, created_at_utc)
                values (?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    user_id,
                    request.device_id,
                    device_type,
                    score_percent,
                    request.duration_seconds,
                    1 if passed else 0,
                    completed_at,
                    json.dumps(payload),
                    iso_now(),
                ),
            )
        return {
            "assessment_id": int(cursor.lastrowid),
            "ok": True,
            "passed": passed,
            "score_percent": score_percent,
        }

    def training_progress(self, user_id: str) -> UserProgress:
        with self._lock, self._connect() as conn:
            rows = conn.execute(
                """
                select device_type, max(score_percent) as score_percent, max(completed_at_utc) as completed_at_utc
                from training_assessments
                where user_id = ? and passed = 1
                group by device_type
                order by completed_at_utc desc
                """,
                (user_id,),
            ).fetchall()
        return UserProgress(
            user_id=user_id,
            certifications=[
                CertifiedModule(
                    device_type=row["device_type"],
                    score_percent=int(row["score_percent"]),
                    completed_at_utc=row["completed_at_utc"],
                )
                for row in rows
            ],
        )

    def create_session(self, response: SessionResponse, user_id: str | None = None) -> None:
        with self._lock, self._connect() as conn:
            conn.execute(
                """
                insert into sessions (session_id, device_id, user_id, status, created_at_utc)
                values (?, ?, ?, ?, ?)
                """,
                (
                    response.session_id,
                    response.device_id,
                    user_id,
                    response.status,
                    response.created_at_utc,
                ),
            )

    def get_session(self, session_id: str) -> dict[str, Any] | None:
        with self._lock, self._connect() as conn:
            row = conn.execute(
                "select * from sessions where session_id = ?",
                (session_id,),
            ).fetchone()
        return self._row_to_dict(row) if row else None

    def add_session_message(self, session_id: str, payload: dict[str, Any]) -> int:
        message_type = str(payload.get("type") or "message")
        author = payload.get("author")
        with self._lock, self._connect() as conn:
            cursor = conn.execute(
                """
                insert into session_messages (session_id, type, author, payload_json, created_at_utc)
                values (?, ?, ?, ?, ?)
                """,
                (session_id, message_type, author, json.dumps(payload), iso_now()),
            )
            return int(cursor.lastrowid)

    def session_messages(self, session_id: str, limit: int = 100) -> list[StoredMessage]:
        with self._lock, self._connect() as conn:
            rows = conn.execute(
                """
                select * from session_messages
                where session_id = ?
                order by id desc
                limit ?
                """,
                (session_id, limit),
            ).fetchall()
        ordered = list(reversed(rows))
        return [
            StoredMessage(
                id=int(row["id"]),
                session_id=row["session_id"],
                created_at_utc=row["created_at_utc"],
                type=row["type"],
                author=row["author"],
                payload=json.loads(row["payload_json"]),
            )
            for row in ordered
        ]

    @staticmethod
    def _row_to_dict(row: sqlite3.Row) -> dict[str, Any]:
        result = dict(row)
        for key in ("payload_json", "completed_steps_json"):
            if key in result and result[key]:
                result[key.replace("_json", "")] = json.loads(result[key])
                del result[key]
        return result
