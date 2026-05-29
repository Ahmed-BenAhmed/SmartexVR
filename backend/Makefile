.PHONY: test run smoke docker-build docker-up docker-down

HOST ?= 127.0.0.1
PORT ?= 8000
BASE_URL ?= http://$(HOST):$(PORT)

test:
	uv run pytest

run:
	uv run uvicorn app.main:app --host $(HOST) --port $(PORT)

smoke:
	curl -fsS $(BASE_URL)/health
	curl -fsS $(BASE_URL)/snapshot
	curl -fsS '$(BASE_URL)/machines/ESP32_TEX_003/anomalies?range=2h'

docker-build:
	docker compose build

docker-up:
	docker compose up --build

docker-down:
	docker compose down
