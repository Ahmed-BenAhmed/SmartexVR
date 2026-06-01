# SmartexVR + AR

**Jumeau numérique industriel et réalité augmentée pour une usine textile marocaine**

Unity 6 · Vuforia · InfluxDB · Apache NiFi · ESP32 · FastAPI · Mistral AI

**Groupe 1** - Filière *Ingénierie en Systèmes d'Information et Big Data (ISIBD)*, ENSA Berrechid, Université Hassan 1er.

Module : *Ingénierie et maquette numérique - projet AR*.

Encadrement : **Pr. Hrimech Hamid** et **Pr. Oumeima**.

---

## Vue d'ensemble

SmartexVR modélise une ligne textile composée de huit métiers Jacquard. Chaque machine est équipée d'un **ESP32** qui remonte des mesures de consommation électrique, vibration, température et tension du tissu. Les données sont ingérées par **Apache NiFi**, stockées dans **InfluxDB**, puis exposées à Unity via un backend **FastAPI**.

Le projet propose deux expériences :

| Mode | Description | Plateforme |
|------|-------------|------------|
| **Jumeau numérique VR / desktop** | Usine 3D avec état de santé, aura visuelle, barre d'énergie et panneau de détail par machine | PC / casque VR |
| **Overlay AR** | Reconnaissance Vuforia d'une machine réelle et affichage des mesures temps réel au-dessus de la cible | Android / iOS |

Les modules Unity consomment le même contrat backend (`/snapshot`) afin de garder les vues VR et AR cohérentes. Les services de maintenance, formation, assistance distante et assistant IA passent également par le backend.

---

## Livrables

Les livrables publics conservés dans le dépôt sont :

| Livrable | Fichier |
|----------|---------|
| Rapport final avec annexes | [`Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf`](Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf) |
| Rapport final de synthèse | [`Docs/reports/Rapport_Final_SmartexVR.pdf`](Docs/reports/Rapport_Final_SmartexVR.pdf) |
| Vidéo de démonstration | [`Docs/media/SmartexVR_demo.mkv`](Docs/media/SmartexVR_demo.mkv) |

Lien externe de la vidéo :

[`https://drive.google.com/file/d/1v2tkMdKLTbN0WfvOwtlpBEgO-JCsPM08/view`](https://drive.google.com/file/d/1v2tkMdKLTbN0WfvOwtlpBEgO-JCsPM08/view?usp=sharing)

---

## Architecture

```text
ESP32 -> MQTT/NiFi -> InfluxDB -> FastAPI -> Unity DataManager -> VR / AR
```

Le client Unity ne contacte pas InfluxDB ou Mistral directement. Il consomme les données via `DataManager`, `SmartexConfig.relayBaseUrl` et les contrats AR partagés (`ARServices`, `IMachineRecognizer`, `RecognizedMachine`).

---

## Structure du dépôt

```text
SmartexVR/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              DataManager, InfluxDBClient, configuration et modèles
│   │   ├── Machines/          Visuels du jumeau numérique
│   │   ├── UI/                Interface partagée
│   │   ├── Contracts/         Interfaces AR stables et mocks éditeur
│   │   └── AR/                Modules AR, reconnaissance, overlay, maintenance, assistance, formation, QA
│   ├── Scenes/SmartexAR.unity Scène AR principale
│   ├── ARTrainingScene.unity  Scène de formation opérateur
│   ├── vrscene.unity          Jumeau numérique VR / desktop
│   └── StreamingAssets/Vuforia/
│       ├── SmartexMachines.dat
│       └── SmartexMachines.xml
├── backend/                   API FastAPI, tests, Dockerfile, docker-compose
├── Docs/
│   ├── media/                 Vidéo de démonstration
│   └── reports/               Deux rapports finaux conservés
├── Packages/                  Dépendances Unity et Vuforia
└── ProjectSettings/           Configuration Unity
```

---

## Prérequis

- **Unity 6000.3.11f1** avec Android Build Support pour les builds Android.
- **Git LFS** pour récupérer les fichiers lourds.
- **Python 3.11+** avec [`uv`](https://docs.astral.sh/uv/) pour le backend.
- **Docker** et Docker Compose si vous lancez le backend en conteneur.
- Une clé **Vuforia** pour les tests AR sur appareil.
- Une clé **Mistral** seulement pour les réponses IA live ; sans clé, le backend retourne une réponse déterministe.

```bash
git clone https://github.com/Ahmed-BenAhmed/SmartexVR.git
cd SmartexVR
git lfs install
git lfs pull
```

---

## Backend

Le backend se trouve dans [`backend/`](backend). Il fonctionne par défaut avec des données mockées pour `ESP32_TEX_001` à `ESP32_TEX_008`.

```bash
cd backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Documentation interactive :

```text
http://127.0.0.1:8000/docs
```

Endpoints principaux :

```text
GET  /health
GET  /snapshot
GET  /machines
GET  /machines/{device_id}/latest
GET  /machines/{device_id}/anomalies?range=24h
GET  /maintenance/procedures/{device_id}
POST /maintenance/logs
GET  /training/modules/{device_type}
POST /training/assessments
POST /sessions
WS   /ws/ar-session/{session_id}
POST /assist/query
```

---

## Unity

Scènes principales :

| Scène | Usage |
|-------|------|
| `Assets/vrscene.unity` | Jumeau numérique VR / desktop |
| `Assets/Scenes/SmartexAR.unity` | Scène AR principale avec Vuforia |
| `Assets/ARTrainingScene.unity` | Formation et onboarding opérateur |

Configuration AR :

1. Ouvrir le projet avec **Unity 6000.3.11f1**.
2. Renseigner la licence Vuforia localement dans `Assets/Resources/VuforiaConfiguration.asset`.
3. Vérifier que la base `SmartexMachines` existe dans `Assets/StreamingAssets/Vuforia/`.
4. Configurer `SmartexConfig.relayBaseUrl` avec une URL backend accessible depuis l'appareil.
5. Pour Android : `File -> Build Settings -> Android -> Switch Platform`, API minimum **24**, architecture **ARM64**.

---

## Vérification

Tests backend :

```bash
cd backend
uv run pytest
```

Smoke test API :

```bash
curl http://127.0.0.1:8000/health
curl http://127.0.0.1:8000/snapshot
curl "http://127.0.0.1:8000/machines/ESP32_TEX_003/anomalies?range=24h"
```

Vérification Unity :

- ouvrir `Assets/vrscene.unity` et vérifier les huit machines en Play Mode ;
- ouvrir `Assets/Scenes/SmartexAR.unity` et vérifier l'absence d'erreurs C# ;
- tester une cible Vuforia `ESP32_TEX_00N` sur appareil Android.

---

## Équipe

| Module | Responsable | Périmètre |
|--------|-------------|-----------|
| Backend, ingestion, QA/DevOps, gestion de projet | **Ahmed Ben Ahmed** | NiFi, InfluxDB, FastAPI, analytics, intégration |
| A - Coeur AR | **Zahra JABER** | Session AR, Vuforia, ancrage |
| B - Reconnaissance machine | **Wissal CHEIKH** | Image Targets, mapping cible -> machine |
| C - Overlay temps réel | **Radwa Tourabi** | Panneau AR et visualisation des mesures |
| D - Maintenance AR | **Maryam Mouaki** | Procédures guidées et journalisation |
| E - Assistant IA | **Aboulaakoul Elwalid** | Mistral, fallback déterministe, assistance maintenance |
| F - Formation et onboarding | **Hiba Marir** | Modules multilingues, quiz, progression |

---

*SmartexVR + AR - Groupe 1, ISIBD, ENSA Berrechid / Université Hassan 1er - 2025-2026.*
