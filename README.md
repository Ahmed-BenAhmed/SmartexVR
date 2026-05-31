# SmartexVR + AR
**Jumeau Numérique Industriel & Réalité Augmentée — Usine textile marocaine**

> Industrial IoT Digital Twin & Augmented Reality for a Moroccan textile factory.
> Unity 6 · Vuforia · InfluxDB · Apache NiFi · ESP32 · FastAPI · Mistral AI · 7-member team.

**Groupe 1** — Filière *Ingénierie en Systèmes d'Information et Big Data (ISIBD)*, ENSA Berrechid — Université Hassan 1er.
Module : *Ingénierie et maquette numérique – projet AR*. Encadré par **Pr. Hrimech Hamid** & **Pr. Oumeima**.

---

## Table of contents

1. [Overview](#1-overview)
2. [Documentation & reports](#2-documentation--reports)
3. [Demo video](#3-demo-video)
4. [Architecture](#4-architecture)
5. [Repository structure](#5-repository-structure)
6. [Prerequisites](#6-prerequisites)
7. [Backend — run & configure](#7-backend--run--configure)
8. [Unity client — open, scenes & build](#8-unity-client--open-scenes--build)
9. [Verifying the application works](#9-verifying-the-application-works)
10. [Team & module assignments](#10-team--module-assignments)
11. [Git & LFS workflow](#11-git--lfs-workflow)
12. [Data conventions](#12-data-conventions)

---

## 1. Overview

Eight Jacquard looms in a Moroccan textile factory each carry an **ESP32** that measures power
consumption, vibration, dye/fabric temperature and thread tension every few seconds. The telemetry is
ingested with **Apache NiFi** into an **InfluxDB** time-series database, exposed through a **FastAPI**
backend (relay + analytics + AI assistance), and visualised in Unity two ways:

| Mode | What you see | Platform |
|------|--------------|----------|
| **VR / Desktop digital twin** | A 3D replica of the factory; each machine's health in real time | PC / VR headset |
| **AR overlay** | Point a phone at a real loom → live sensor data floats above it | Android / iOS (Vuforia) |

Both modes consume **the same backend contracts** (`DataManager → /snapshot`), so the data shown is always
consistent. The project also targets **CBAM** (Carbon Border Adjustment Mechanism) traceability: per-machine
CO₂ and carbon-cost estimates are computed by the backend.

Key capabilities: real-time data overlay, guided AR maintenance, multilingual (ar/fr/en) operator training,
remote-expert assistance (backend relay), and a **grounded AI maintenance assistant** (Mistral) that never
invents sensor readings and degrades gracefully to deterministic guidance.

---

## 2. Documentation & reports

The full project documentation is delivered as French academic reports under [`Docs/reports/`](Docs/reports):

| Report | File |
|--------|------|
| **Final report + all member annexes** (complete submission, 147 p) | [`Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf`](Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf) |
| Final project report (synthesis, standalone) | [`Docs/reports/Rapport_Final_SmartexVR.pdf`](Docs/reports/Rapport_Final_SmartexVR.pdf) |
| Individual report — Chef de projet / Backend / QA (Ahmed Ben Ahmed) | [`Docs/reports/Rapport_Individuel_AhmedBenAhmed_ChefDeProjet.pdf`](Docs/reports/Rapport_Individuel_AhmedBenAhmed_ChefDeProjet.pdf) |
| Module report — Assistant IA (Aboulaakoul Elwalid) | [`Docs/reports/Rapport_Module_AssistantIA_Elwalid.pdf`](Docs/reports/Rapport_Module_AssistantIA_Elwalid.pdf) |

Individual member module reports (also embedded as annexes in the complete submission):

| Module report | Member | File |
|---------------|--------|------|
| Module A — Cœur AR (Vuforia / Android) | Zahra JABER | [`Docs/reports/_annexes/annexe_A_ModuleA_Zahra.pdf`](Docs/reports/_annexes/annexe_A_ModuleA_Zahra.pdf) |
| Module B — Reconnaissance de machine | Wissal CHEIKH | [`Docs/reports/_annexes/annexe_B_ModuleB_Wissal.pdf`](Docs/reports/_annexes/annexe_B_ModuleB_Wissal.pdf) |
| Module C — Interface / Overlay de données AR | Radwa Tourabi | [`Docs/reports/_annexes/annexe_C_ModuleC_Radwa.pdf`](Docs/reports/_annexes/annexe_C_ModuleC_Radwa.pdf) |
| Module D — Flux de maintenance AR | Maryam Mouaki | [`Docs/reports/_annexes/annexe_D_ModuleD_Maryam.pdf`](Docs/reports/_annexes/annexe_D_ModuleD_Maryam.pdf) |
| Module F — Formation & onboarding | Hiba Marir | [`Docs/reports/_annexes/annexe_F_ModuleF_Hiba.pdf`](Docs/reports/_annexes/annexe_F_ModuleF_Hiba.pdf) |

The annexed final report embeds every member's individual report (Modules A, B, C, D, F, Assistant IA, and
the project-lead report). Report sources (docx-js generators) and the member PDFs live in
`Docs/reports/_build/` and `Docs/reports/_annexes/`.

Additional design docs: [`Docs/CLAUDE_UNITY_CONNECTION_HANDOFF.md`](Docs/CLAUDE_UNITY_CONNECTION_HANDOFF.md),
[`Docs/performance-baseline.md`](Docs/performance-baseline.md), [`Docs/deployment.md`](Docs/deployment.md),
[`backend/README.md`](backend/README.md).

---

## 3. Demo video

A walkthrough/demo video of the working application will be added to the repository at:

```
Docs/media/SmartexVR_demo.mp4
```

> 🎥 **Status: to be added.** The video (tracked via Git LFS, see [§11](#11-git--lfs-workflow)) will be pushed
> at the above path once recorded; this section will then link to it directly.

---

## 4. Architecture

<p align="center">
  <img src="Docs/reports/images/arch_pipeline.png" alt="SmartexVR data pipeline" width="360">
  &nbsp;&nbsp;&nbsp;
  <img src="Docs/reports/images/m_arch_real.png" alt="Architecture technique détaillée SmartTex" width="420">
</p>

*Left: end-to-end data pipeline. Right: detailed technical architecture (sensors, MQTT, k3s cluster, users).*

The Unity client **never** talks to InfluxDB or Mistral directly — it consumes the backend's stable
contracts (`IMachineRecognizer` / `RecognizedMachine`, `/snapshot`, `/assist/query`, …). A Grafana
dashboard provides web-based supervision of the same InfluxDB data.

---

## 5. Repository structure

```
SmartexVR/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/            DATA LAYER — DataManager, InfluxDBClient, SmartexConfig, Models
│   │   ├── Machines/        VR visuals — MachineController, HealthAura, EnergyBar
│   │   ├── UI/              Shared UI — MachineDetailPanel
│   │   ├── Contracts/       Stable AR service contracts (IMachineRecognizer, DataTypes, ARServices) + Mocks
│   │   └── AR/              AR modules — Core, Recognition, Overlay/ModuleC, Maintenance, RemoteAssist, Training, QA
│   ├── Scenes/              SmartexAR*.unity (AR), …
│   ├── ARTrainingScene.unity
│   ├── vrscene.unity        VR / desktop digital twin
│   ├── Resources/           ARConfig, VuforiaConfiguration, training & maintenance JSON
│   └── StreamingAssets/Vuforia/   SmartexMachines.dat/.xml (target database)
├── Packages/                Vuforia 11.4.4 (.tgz via LFS), AR Foundation 6.1, manifest/lock
├── backend/                 FastAPI service (app/, tests/, Dockerfile, docker-compose.yml)
├── Docs/
│   ├── reports/             Final report, member reports, annexed PDF (see §2)
│   └── media/               Demo video (see §3)
└── README.md
```

---

## 6. Prerequisites

- **Unity** `6000.3.11f1` (exact — install via Unity Hub). URP project.
- **Git** + **Git LFS** (`git lfs install` once after cloning — required for the Vuforia `.tgz`, FBX, images).
- **Python ≥ 3.11** and [**uv**](https://docs.astral.sh/uv/) for the backend (or **Docker** + Docker Compose).
- For AR builds: **Android SDK** (API 24+, ARM64) — bundled with Unity's Android module — or Xcode for iOS.
- A **Vuforia** license key (free dev key) and, optionally, a **Mistral API key** for live AI answers.

```bash
git clone https://github.com/Ahmed-BenAhmed/SmartexVR.git
cd SmartexVR
git lfs install && git lfs pull
```

---

## 7. Backend — run & configure

Location: [`backend/`](backend). FastAPI app at `app.main:app`. Mock telemetry is enabled by default, so the
backend runs with **no external dependencies** out of the box.

### Run locally (uv)

```bash
cd backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
#  or:  make run
```

The API is then at `http://127.0.0.1:8000` (Unity defaults to this in `SmartexConfig.relayBaseUrl`).
Interactive docs: `http://127.0.0.1:8000/docs`.

### Run with Docker

```bash
cd backend
docker compose up --build          # or: make docker-up
```

### Configuration (environment)

Copy `backend/.env.example` → `backend/.env` only to override defaults. Key variables:

| Variable | Purpose |
|----------|---------|
| `SMARTEX_DATA_SOURCE` | `mock` (default, generated telemetry) or `influx` (live InfluxDB) |
| `INFLUX_URL` / `INFLUX_TOKEN` / `INFLUX_ORG` / `INFLUX_BUCKET` | InfluxDB connection (when `influx`) |
| `MISTRAL_API_KEY` / `MISTRAL_MODEL` | Live AI answers; **without a key**, `/assist/query` returns deterministic guidance |
| `SMARTEX_API_TOKEN` | Optional shared-token auth for protected routes |

> 🔐 Never commit secrets (Mistral key, Vuforia license). They are read from the environment only.

### Main endpoints

```
GET  /health
GET  /snapshot                                  # Unity relay contract (FactorySnapshot)
GET  /machines · /machines/{id}/latest
GET  /machines/{id}/timeseries?range=24h
GET  /machines/{id}/anomalies?range=24h         # median/MAD anomaly detection
GET  /maintenance/procedures/{id} · POST /maintenance/logs
GET  /training/modules/{type}    · POST /training/assessments
POST /sessions · WS /ws/ar-session/{id}         # remote-assist relay
POST /assist/query                              # grounded AI assistant (Mistral → deterministic fallback)
POST /assist/sessions/{id}/summary · /report
```

---

## 8. Unity client — open, scenes & build

### Open the project

1. Open **Unity Hub → Add** → select this folder → open with **Unity 6000.3.11f1**.
2. First import takes a few minutes (packages + Vuforia compile). Ensure `git lfs pull` ran first, or the
   Vuforia `.tgz` will be an invalid LFS pointer.
3. Set the Vuforia license key in `Assets/Resources/VuforiaConfiguration.asset` (or via environment) — it is
   intentionally **not** committed.

### Scenes (committed in the repo)

| Scene | Purpose |
|-------|---------|
| `Assets/vrscene.unity` | VR / desktop **digital twin** — 8 looms with health aura + energy bar |
| `Assets/Scenes/SmartexAR.unity` | **AR scene** — Vuforia recognition + overlay + modules |
| `Assets/ARTrainingScene.unity` | Operator **training / onboarding** (multilingual) |

These three scenes are tracked and pushed — open them directly after import. (Scratch scenes `scene.unity`
and `scene1.unity` are early prototypes and can be ignored.)

> Test without hardware: start the backend (§7), open `SmartexAR.unity`, and in the AR rig swap the active
> recognizer for the **mock recognizer** under `Assets/Scripts/Contracts/Mocks/` (it emits a recognized
> machine in the Editor). The overlay then spawns and refreshes from `/snapshot` without a phone or printed
> target.

### Assembling / rebuilding an AR scene from the menu

If you need to (re)build an AR scene from scratch, Vuforia adds its objects under Unity's **GameObject** menu:

1. `GameObject → Vuforia Engine → AR Camera` (replaces the Main Camera; holds the Vuforia behaviour).
2. `GameObject → Vuforia Engine → Image Target` — set **Type = From Database**, **Database = SmartexMachines**,
   and **Image Target = machine_ESP32_TEX_00N** (one per loom, names matching the device IDs).
3. Add an empty `SmartexManager` and attach the recognizer (`VuforiaTargetScanner`) + `DataManager`; it
   registers itself with `ARServices` and maps each target to a `device_id`.
4. Parent the overlay / maintenance / training content under each Image Target's transform so it stays
   anchored, then add the scene to `File → Build Settings → Scenes In Build`.
5. Set the Vuforia license in `Window → Vuforia Configuration` (or `Assets/Resources/VuforiaConfiguration.asset`).

The target database (`Assets/StreamingAssets/Vuforia/SmartexMachines.dat/.xml`) and target textures are
already in the repo, so step 2 finds the eight machines without re-importing anything.

### Build to Android (AR)

1. `File → Build Settings → Android → Switch Platform`.
2. Player Settings → Minimum API **24**, Target Architecture **ARM64**, scripting backend **IL2CPP** for release.
3. Add `Assets/Scenes/SmartexAR.unity` (and `ARTrainingScene.unity`) to *Scenes In Build*.
4. Set `SmartexConfig.relayBaseUrl` to a backend reachable from the phone (LAN IP, not `localhost`).
5. Connect the device (USB debugging) → **Build And Run**.

---

## 9. Verifying the application works

### Backend tests & smoke

```bash
cd backend
uv run pytest                 # 16 tests: API + analytics + AI client  → all pass
make smoke                    # curls /health, /snapshot, /anomalies against a running server
```

End-to-end sanity against a running server:

```bash
curl http://127.0.0.1:8000/health
curl http://127.0.0.1:8000/snapshot
curl "http://127.0.0.1:8000/machines/ESP32_TEX_003/anomalies?range=24h"
curl -X POST http://127.0.0.1:8000/assist/query -H "Content-Type: application/json" \
  -d '{"device_id":"ESP32_TEX_003","locale":"fr","question":"Pourquoi cette machine est en alerte ?"}'
```

The assist call returns `ai_provider: "mistral"` when a key is set, otherwise `ai_provider: "deterministic"`
— both are valid, HTTP 200.

### Unity compile check (headless)

```bash
# Windows (adjust the path to your Unity install)
"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" ^
  -batchmode -quit -nographics ^
  -projectPath "%CD%" -logFile compile.log -buildTarget Android
# Success = return code 0 and no "error CS" lines in compile.log
```

The integrated project compiles cleanly (0 C# errors) across Vuforia, AR Foundation 6.1 and all `Smartex.*`
assemblies.

### AR / runtime smoke checklist

- **Twin:** open `vrscene.unity`, Play → 8 looms update colour/health from the backend.
- **AR (editor):** open `SmartexAR_MockDemo.unity`, Play → overlay spawns above the mock-recognized machine.
- **AR (device):** point the phone at a printed `ESP32_TEX_00N` target → overlay anchors to the loom and shows
  live data; `/assist/query` answers render in the recommendation panel.

---

## 10. Team & module assignments

**Groupe 1** — 7 members. Encadré par Pr. Hrimech Hamid & Pr. Oumeima.

| Module | Responsable | Scope |
|--------|-------------|-------|
| Backend, Ingestion, QA/DevOps & Gestion de projet | **Ahmed Ben Ahmed** (chef) | NiFi→InfluxDB pipeline, FastAPI relay/analytics, CI, integration |
| A — Cœur AR (Vuforia / Android) | **Zahra JABER** | AR session, anchoring, Vuforia lifecycle, target registry |
| B — Reconnaissance de machine | **Wissal CHEIKH** | Vuforia image targets, target→machine bridge, `SmartexMachines.dat` |
| C — Interface / Overlay temps réel | **Radwa Tourabi** | Floating data panel, data binding, billboard |
| D — Flux de maintenance AR | **Maryam Mouaki** | Step-by-step procedures, AR callouts, step logging |
| F — Formation & onboarding | **Hiba Marir** | Multilingual (ar/fr/en) labelling + quiz, progress |
| Assistant IA | **Aboulaakoul Elwalid** | Grounded Mistral assistant, deterministic fallback |

Detailed per-module documentation is in each member's report (annexed in the final PDF, see §2).

---

## 11. Git & LFS workflow

- Large binaries (FBX, PNG/JPG, DLL, `.tgz`, `.mp4`, …) are tracked via **Git LFS** (`.gitattributes`).
  Run `git lfs install` once, and `git lfs pull` after cloning.
- The demo video (`Docs/media/SmartexVR_demo.mp4`, §3) will be committed through LFS.
- Branch per feature (`feature/...`); `master` stays buildable; integration was done on a dedicated branch
  and fast-forwarded after a clean headless compile.

---

## 12. Data conventions

| Thing | Convention |
|-------|-----------|
| Device IDs | `ESP32_TEX_001` … `ESP32_TEX_008` — read from `MachineData.device_id`, never hardcode |
| Backend URL | `SmartexConfig.Instance.relayBaseUrl` — never hardcode `localhost` |
| Data updates | Subscribe to `DataManager.OnSnapshotUpdated` — never poll manually |
| Recognition | Vuforia behind `IMachineRecognizer` / `RecognizedMachine.AnchorTransform` |
| Namespaces | One assembly per module: `Smartex.AR.Core`, `Smartex.AR.Recognition`, … |
| Secrets | Vuforia license & Mistral key from environment only — never committed |

`FactorySnapshot` carries `machines: List<MachineData>` (with `avg_power_watts`, `health_score`,
`alert_level`, `co2_kg_today`, `cbam_contribution`, `is_online`, …) plus factory-level totals — the single
source of truth shared by the VR twin and the AR overlay.

---

*SmartexVR + AR — Groupe 1, ISIBD, ENSA Berrechid / Université Hassan 1er — 2025–2026.*
