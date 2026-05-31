# SmartexVR + AR

**Jumeau numérique industriel et réalité augmentée pour une usine textile marocaine**

Unity 6 · Vuforia · InfluxDB · Apache NiFi · ESP32 · FastAPI · Mistral AI · Git LFS

**Groupe 1** — Filière *Ingénierie en Systèmes d'Information et Big Data (ISIBD)*, ENSA Berrechid, Université Hassan 1er.

Module : *Ingénierie et maquette numérique - projet AR*.

Encadrement : **Pr. Hrimech Hamid** et **Pr. Oumeima**.

---

## Sommaire

1. [Vue d'ensemble](#1-vue-densemble)
2. [Documentation et livrables](#2-documentation-et-livrables)
3. [Démonstration vidéo](#3-demonstration-video)
4. [Architecture](#4-architecture)
5. [Structure du dépôt](#5-structure-du-depot)
6. [Prérequis](#6-prerequis)
7. [Backend FastAPI](#7-backend-fastapi)
8. [Client Unity AR/VR](#8-client-unity-arvr)
9. [Vérification](#9-verification)
10. [Équipe et modules](#10-equipe-et-modules)
11. [Workflow Git et LFS](#11-workflow-git-et-lfs)
12. [Conventions techniques](#12-conventions-techniques)

---

<a id="1-vue-densemble"></a>

## 1. Vue d'ensemble

SmartexVR modélise une ligne textile composée de huit métiers Jacquard. Chaque machine est équipée d'un **ESP32** qui remonte des mesures de consommation électrique, vibration, température et tension du tissu. Les données sont ingérées par **Apache NiFi**, stockées dans **InfluxDB**, puis exposées à Unity via un backend **FastAPI**.

Le projet propose deux expériences complémentaires :

| Mode | Description | Plateforme |
|------|-------------|------------|
| **Jumeau numérique VR / Desktop** | Usine 3D avec 8 machines, état de santé, aura, barre d'énergie et panneau de détail | PC / casque VR |
| **Overlay AR** | Reconnaissance d'une machine réelle avec Vuforia, puis affichage des données temps réel au-dessus de la machine | Android / iOS |

Les deux modes utilisent le même contrat de données : `DataManager` interroge d'abord `/snapshot`, puis peut basculer vers InfluxDB si le relais backend n'est pas disponible. Les modules AR ne doivent pas appeler InfluxDB ou Mistral directement : ils consomment les contrats partagés (`ARServices`, `IMachineRecognizer`, `RecognizedMachine`, etc.).

Fonctionnalités principales :

- affichage temps réel des mesures machine ;
- estimation CO2 et contribution CBAM par machine ;
- reconnaissance Vuforia des cibles `ESP32_TEX_001` à `ESP32_TEX_008` ;
- guide de maintenance AR ;
- formation opérateur multilingue ;
- assistance distante via backend ;
- assistant IA de maintenance avec Mistral et fallback déterministe.

---

<a id="2-documentation-et-livrables"></a>

## 2. Documentation et livrables

Les rapports académiques sont disponibles dans [`Docs/reports/`](Docs/reports).

| Livrable | Fichier |
|----------|---------|
| Rapport final avec toutes les annexes | [`Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf`](Docs/reports/Rapport_Final_SmartexVR_avec_Annexes.pdf) |
| Rapport final de synthèse | [`Docs/reports/Rapport_Final_SmartexVR.pdf`](Docs/reports/Rapport_Final_SmartexVR.pdf) |
| Rapport individuel - chef de projet / backend / QA | [`Docs/reports/Rapport_Individuel_AhmedBenAhmed_ChefDeProjet.pdf`](Docs/reports/Rapport_Individuel_AhmedBenAhmed_ChefDeProjet.pdf) |
| Rapport module - assistant IA | [`Docs/reports/Rapport_Module_AssistantIA_Elwalid.pdf`](Docs/reports/Rapport_Module_AssistantIA_Elwalid.pdf) |

Annexes par module :

| Module | Responsable | Annexe |
|--------|-------------|--------|
| A - Coeur AR Vuforia / Android | Zahra JABER | [`annexe_A_ModuleA_Zahra.pdf`](Docs/reports/_annexes/annexe_A_ModuleA_Zahra.pdf) |
| B - Reconnaissance de machine | Wissal CHEIKH | [`annexe_B_ModuleB_Wissal.pdf`](Docs/reports/_annexes/annexe_B_ModuleB_Wissal.pdf) |
| C - Overlay AR temps réel | Radwa Tourabi | [`annexe_C_ModuleC_Radwa.pdf`](Docs/reports/_annexes/annexe_C_ModuleC_Radwa.pdf) |
| D - Maintenance AR | Maryam Mouaki | [`annexe_D_ModuleD_Maryam.pdf`](Docs/reports/_annexes/annexe_D_ModuleD_Maryam.pdf) |
| E - Assistant IA | Aboulaakoul Elwalid | [`annexe_E_AssistantIA_Elwalid.pdf`](Docs/reports/_annexes/annexe_E_AssistantIA_Elwalid.pdf) |
| F - Formation et onboarding | Hiba Marir | [`annexe_F_ModuleF_Hiba.pdf`](Docs/reports/_annexes/annexe_F_ModuleF_Hiba.pdf) |
| G - Gestion de projet / intégration | Ahmed Ben Ahmed | [`annexe_G_ChefDeProjet_Ahmed.pdf`](Docs/reports/_annexes/annexe_G_ChefDeProjet_Ahmed.pdf) |

Autres documents utiles :

- [`backend/README.md`](backend/README.md) : commandes backend, variables d'environnement et API ;
- [`Docs/deployment.md`](Docs/deployment.md) : guide de déploiement Android, iOS et Quest ;
- [`Docs/performance-baseline.md`](Docs/performance-baseline.md) : baseline performance ;
- [`Docs/CLAUDE_UNITY_CONNECTION_HANDOFF.md`](Docs/CLAUDE_UNITY_CONNECTION_HANDOFF.md) : notes d'intégration Unity/Vuforia.

---

<a id="3-demonstration-video"></a>

## 3. Démonstration vidéo

Vidéo de démonstration :

**[Voir la démo sur Google Drive](https://drive.google.com/file/d/1v2tkMdKLTbN0WfvOwtlpBEgO-JCsPM08/view?usp=sharing)**

La vidéo est aussi incluse dans le dépôt via Git LFS :

[`Docs/media/SmartexVR_demo.mkv`](Docs/media/SmartexVR_demo.mkv)

---

<a id="4-architecture"></a>

## 4. Architecture

<p align="center">
  <img src="Docs/reports/images/arch_pipeline.png" alt="Pipeline de données SmartexVR" width="360">
  &nbsp;&nbsp;&nbsp;
  <img src="Docs/reports/images/m_arch_real.png" alt="Architecture technique SmartTex" width="420">
</p>

Le flux principal est le suivant :

```text
ESP32 -> MQTT/NiFi -> InfluxDB -> FastAPI -> Unity DataManager -> VR / AR
```

Le backend sert de couche de protection et d'agrégation :

- `/snapshot` fournit l'état courant de l'usine au format attendu par Unity ;
- les endpoints maintenance, formation et sessions AR stockent les workflows métier ;
- `/assist/query` interroge Mistral si une clé est configurée, sinon retourne une réponse déterministe basée sur les mesures et anomalies disponibles ;
- Unity reste découplé de l'infrastructure data et IA.

---

<a id="5-structure-du-depot"></a>

## 5. Structure du dépôt

```text
SmartexVR/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              DataManager, InfluxDBClient, SmartexConfig, modèles
│   │   ├── Machines/          Visuels du jumeau numérique
│   │   ├── UI/                UI partagée
│   │   ├── Contracts/         Interfaces AR stables + mocks éditeur
│   │   └── AR/                Modules Core, Recognition, Overlay, Maintenance, RemoteAssist, Training, QA
│   ├── Scenes/
│   │   └── SmartexAR.unity    Scène AR principale
│   ├── ARTrainingScene.unity  Scène de formation opérateur
│   ├── vrscene.unity          Jumeau numérique VR / desktop
│   ├── Resources/             ARConfig, VuforiaConfiguration, contenus JSON
│   └── StreamingAssets/Vuforia/
│       ├── SmartexMachines.dat
│       └── SmartexMachines.xml
├── Packages/                  Manifest Unity + Vuforia 11.4.4 en `.tgz`
├── ProjectSettings/           Configuration Unity
├── backend/                   API FastAPI, tests, Dockerfile, compose
├── Docs/                      Rapports, médias et guides techniques
└── README.md
```

---

<a id="6-prerequis"></a>

## 6. Prérequis

- **Unity 6000.3.11f1** exactement, avec le module Android Build Support si vous ciblez Android.
- **Git LFS** pour récupérer les gros fichiers : Vuforia `.tgz`, vidéos, rapports, images, modèles.
- **Python 3.11+** avec [`uv`](https://docs.astral.sh/uv/) pour exécuter le backend localement.
- **Docker** et Docker Compose si vous préférez lancer le backend en conteneur.
- Une clé **Vuforia** pour les tests AR sur appareil.
- Une clé **Mistral** uniquement si vous voulez des réponses IA live ; sans clé, le backend reste fonctionnel avec un fallback déterministe.

Installation :

```bash
git clone https://github.com/Ahmed-BenAhmed/SmartexVR.git
cd SmartexVR
git lfs install
git lfs pull
```

---

<a id="7-backend-fastapi"></a>

## 7. Backend FastAPI

Le backend se trouve dans [`backend/`](backend). Par défaut, il utilise des données mockées pour les huit machines `ESP32_TEX_001` à `ESP32_TEX_008`, donc aucun InfluxDB réel n'est nécessaire pour lancer une démo locale.

Lancement avec `uv` :

```bash
cd backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Lancement avec Docker :

```bash
cd backend
docker compose up --build
```

Documentation interactive :

```text
http://127.0.0.1:8000/docs
```

Variables d'environnement principales :

| Variable | Rôle |
|----------|------|
| `SMARTEX_DATA_SOURCE` | `mock` par défaut, ou `influx` pour les données réelles |
| `INFLUX_URL`, `INFLUX_TOKEN`, `INFLUX_ORG`, `INFLUX_BUCKET` | Connexion InfluxDB |
| `MISTRAL_API_KEY`, `MISTRAL_MODEL` | Assistant IA live |
| `SMARTEX_API_TOKEN` | Protection optionnelle des routes backend |
| `REQUIRE_AUTH_FOR_SNAPSHOT` | Protège aussi `/snapshot` si défini à `true` |

Endpoints principaux :

```text
GET  /health
GET  /snapshot
GET  /machines
GET  /machines/{device_id}/latest
GET  /machines/{device_id}/timeseries?range=24h
GET  /machines/{device_id}/anomalies?range=24h
GET  /maintenance/procedures/{device_id}
POST /maintenance/logs
GET  /training/modules/{device_type}
POST /training/assessments
POST /sessions
WS   /ws/ar-session/{session_id}
POST /assist/query
POST /assist/sessions/{session_id}/summary
POST /assist/sessions/{session_id}/report
```

Ne commitez jamais les secrets : clé Vuforia, token InfluxDB, clé Mistral ou token API.

---

<a id="8-client-unity-arvr"></a>

## 8. Client Unity AR/VR

Ouvrir le projet :

1. Ouvrir Unity Hub.
2. Ajouter ce dossier comme projet Unity.
3. Sélectionner **Unity 6000.3.11f1**.
4. Attendre l'import complet des packages et assets.
5. Vérifier que `git lfs pull` a bien été exécuté avant l'import.

Scènes importantes :

| Scène | Usage |
|-------|------|
| `Assets/vrscene.unity` | Jumeau numérique VR / desktop |
| `Assets/Scenes/SmartexAR.unity` | Scène AR principale avec Vuforia |
| `Assets/ARTrainingScene.unity` | Formation et onboarding opérateur |

Les scènes `Assets/scene.unity` et `Assets/scene1.unity` sont des prototypes anciens.

Configuration Vuforia :

1. Ouvrir `Assets/Resources/VuforiaConfiguration.asset`.
2. Renseigner la licence Vuforia localement.
3. Vérifier la base de cibles `SmartexMachines` dans `Assets/StreamingAssets/Vuforia/`.
4. Les cibles doivent correspondre aux identifiants `ESP32_TEX_001` à `ESP32_TEX_008`.

Reconnaissance AR :

- `Assets/Scripts/AR/Recognition/VuforiaTargetToMachineBridge.cs` relie les Image Targets Vuforia au contrat existant `MachineQRTracker`.
- `Assets/Scripts/Contracts/IMachineRecognizer.cs` et `ARServices.cs` définissent l'interface stable à utiliser par les modules.
- Les contenus AR doivent être parentés sous `RecognizedMachine.AnchorTransform` ou sous le transform de l'Image Target pour rester correctement ancrés.

Build Android :

1. `File -> Build Settings -> Android -> Switch Platform`.
2. Minimum API : **24**.
3. Architecture : **ARM64**.
4. Backend URL : remplacer `localhost` par l'IP LAN de la machine qui lance FastAPI.
5. Ajouter `Assets/Scenes/SmartexAR.unity` aux scènes du build.
6. Connecter le téléphone en USB debugging puis lancer **Build And Run**.

---

<a id="9-verification"></a>

## 9. Vérification

Tests backend :

```bash
cd backend
uv run pytest
```

Smoke test API :

```bash
cd backend
uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Dans un autre terminal :

```bash
curl http://127.0.0.1:8000/health
curl http://127.0.0.1:8000/snapshot
curl "http://127.0.0.1:8000/machines/ESP32_TEX_003/anomalies?range=24h"
curl -X POST http://127.0.0.1:8000/assist/query \
  -H "Content-Type: application/json" \
  -d '{"device_id":"ESP32_TEX_003","locale":"fr","question":"Pourquoi cette machine est en alerte ?"}'
```

Contrôle Unity :

- ouvrir `Assets/vrscene.unity`, lancer Play, vérifier que les huit machines changent d'état avec les snapshots ;
- ouvrir `Assets/Scenes/SmartexAR.unity`, vérifier l'absence d'erreurs C# dans la console ;
- sur appareil Android, viser une cible Vuforia `ESP32_TEX_00N` et vérifier que l'overlay reste ancré ;
- vérifier que les appels maintenance, formation et assistant IA passent par le backend, pas directement par InfluxDB ou Mistral.

Compilation Unity en batch, exemple Windows :

```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" ^
  -batchmode -quit -nographics ^
  -projectPath "%CD%" -logFile compile.log -buildTarget Android
```

Succès attendu : code retour `0` et aucune erreur `CS` dans `compile.log`.

---

<a id="10-equipe-et-modules"></a>

## 10. Équipe et modules

| Module | Responsable | Périmètre |
|--------|-------------|-----------|
| Backend, ingestion, QA/DevOps, gestion de projet | **Ahmed Ben Ahmed** | Pipeline NiFi/InfluxDB, FastAPI, analytics, CI, intégration |
| A - Coeur AR | **Zahra JABER** | Session AR, Vuforia, ancrage et configuration Android |
| B - Reconnaissance machine | **Wissal CHEIKH** | Image Targets Vuforia, mapping cible -> machine |
| C - Overlay temps réel | **Radwa Tourabi** | Panneau AR, data binding, billboard |
| D - Maintenance AR | **Maryam Mouaki** | Procédures guidées, callouts, journalisation |
| E - Assistant IA | **Aboulaakoul Elwalid** | Mistral, fallback déterministe, explications de maintenance |
| F - Formation et onboarding | **Hiba Marir** | Modules multilingues, quiz, progression utilisateur |
| G - Intégration et documentation | **Ahmed Ben Ahmed** | Assemblage final, rapports, validation |

---

<a id="11-workflow-git-et-lfs"></a>

## 11. Workflow Git et LFS

- `master` doit rester buildable.
- Créer une branche par fonctionnalité : `feature/module-x-nom`.
- Ne pas commiter les dossiers générés par Unity (`Library/`, `Temp/`, `Obj/`, `Build/`, etc.).
- Utiliser Git LFS pour les fichiers lourds déjà couverts par `.gitattributes`.
- Après un clone ou un pull important, lancer :

```bash
git lfs pull
```

Avant une merge request :

```bash
git status
cd backend && uv run pytest
```

Pour les changements Unity, ouvrir le projet et vérifier la console avant de pousser.

---

<a id="12-conventions-techniques"></a>

## 12. Conventions techniques

| Élément | Convention |
|---------|------------|
| Identifiants machines | `ESP32_TEX_001` à `ESP32_TEX_008` |
| Source Unity | `DataManager.Instance.LastSnapshot` et `OnSnapshotUpdated` |
| URL backend | `SmartexConfig.Instance.relayBaseUrl` |
| Reconnaissance AR | Vuforia, puis contrat `IMachineRecognizer` / `RecognizedMachine` |
| Position des annotations | Coordonnées locales de la cible (`local_pos`) |
| Services IA | Toujours via le backend |
| Secrets | Jamais dans Git |

`FactorySnapshot` est la source de vérité côté Unity. Il contient `machines: List<MachineData>` avec les champs `device_id`, `avg_power_watts`, `health_score`, `alert_level`, `co2_kg_today`, `cbam_contribution`, `is_online`, ainsi que les totaux usine.

---

*SmartexVR + AR - Groupe 1, ISIBD, ENSA Berrechid / Université Hassan 1er - 2025-2026.*
