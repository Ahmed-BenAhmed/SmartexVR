from __future__ import annotations

import secrets

from fastapi import Header, HTTPException, Query, WebSocket, status

from app.config import Settings


def _matches_configured_token(candidate: str | None, settings: Settings) -> bool:
    token = settings.smartex_api_token
    if not token:
        return True
    return bool(candidate) and secrets.compare_digest(candidate, token)


def _bearer_token(value: str | None) -> str | None:
    if not value:
        return None
    scheme, _, token = value.partition(" ")
    if scheme.lower() != "bearer" or not token:
        return None
    return token


def require_api_token(
    settings: Settings,
    x_smartex_token: str | None = Header(default=None),
    authorization: str | None = Header(default=None),
) -> None:
    candidate = x_smartex_token or _bearer_token(authorization)
    if _matches_configured_token(candidate, settings):
        return
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Missing or invalid Smartex API token.",
    )


def optional_snapshot_token(
    settings: Settings,
    x_smartex_token: str | None = Header(default=None),
    authorization: str | None = Header(default=None),
) -> None:
    if not settings.require_auth_for_snapshot:
        return
    require_api_token(settings, x_smartex_token=x_smartex_token, authorization=authorization)


async def require_websocket_token(
    websocket: WebSocket,
    settings: Settings,
    token: str | None = Query(default=None),
) -> bool:
    candidate = token or websocket.headers.get("x-smartex-token") or _bearer_token(websocket.headers.get("authorization"))
    if _matches_configured_token(candidate, settings):
        return True
    await websocket.close(code=1008)
    return False
