# SmartexVR++ — Three PRDs (Niveaux de difficulté)

---

## Projet A — Niveau Accessible ⭐
### SmartTwin-Monitor : Jumeau Numérique Industriel Connecté avec Assistant IA Conversationnel

**Titre :**
SmartTwin-Monitor : Supervision Énergétique Temps Réel et Dialogue Intelligent avec un Jumeau Numérique d'Usine Textile

**Description du concept :**
L'utilisateur accède depuis un poste Desktop à un jumeau numérique 3D d'une usine textile
marocaine. Le jumeau reflète en temps réel les données des 8 métiers à tisser (puissance,
vibration, température, score de santé) captées par des microcontrôleurs ESP32 via un broker
MQTT → InfluxDB. L'utilisateur peut naviguer librement dans la scène (orbite, vol, vue
isométrique), cliquer sur chaque machine pour obtenir un panneau de détail complet, et
dialoguer en langage naturel avec un agent IA (IEIA) intégré dans l'interface pour poser des
questions du type "Quelle machine consomme le plus ce mois ?" ou "Quel est le risque
d'effondrement du roulement de Loom 3 ?".

**Le rôle de l'IA et/ou la Big Data :**
L'agent IEIA (Industrial Energy Intelligence Agent) est un LLM orchestré via OpenRouter,
alimenté en continu par le flux télémetrique d'InfluxDB. Il applique un modèle de régression
causale (Paper 4 — coefficient d'usure des roulements 0.12 kWh/unité) pour générer des
scénarios contrefactuels : "Si vous effectuez la maintenance de Loom 5 aujourd'hui, vous
économisez 840 MAD/an en exposition CBAM." Les alertes prédictives sont poussées en
temps réel via WebSocket. La couche Big Data repose sur InfluxDB v2 et des requêtes Flux
pour l'historique énergétique et les tendances sur fenêtre glissante.

**Type d'interaction :**
Mono-utilisateur, Desktop. Interface de supervision enrichie d'un chatbot IA contextuel.
L'utilisateur navigue dans la scène 3D, inspecte les machines et consulte l'agent textuel
intégré dans le HUD pour obtenir des recommandations actionnables.

**Contributions scientifiques possibles :**
- Évaluation de l'efficacité d'un assistant IA conversationnel intégré à un jumeau numérique
  pour la prise de décision en maintenance prédictive ;
- Comparaison entre un tableau de bord 2D classique et un jumeau 3D interactif pour la
  détection d'anomalies énergétiques dans le secteur textile ;
- Quantification de l'exposition CBAM par machine via un modèle causal validé sur données
  réelles (capteurs ESP32, réseau électrique ONEE).

**Fonctionnalités clés Unity :**
- URP 17 + émission procédurale des matériaux (couleur santé : vert → orange → rouge)
- `UnityWebRequest` + coroutines pour le polling InfluxDB et relay FastAPI
- New Input System (`Keyboard.current`, `Mouse.current`) pour la navigation caméra
- TextMeshPro pour le HUD de KPIs (puissance totale, CO2 journalier, exposition CBAM)
- `Physics.Raycast` + `IPointerClickHandler` pour la sélection des machines
- Panneau de détail avec slider what-if (scénario contrefactuel Paper 4 en temps réel)
- Chat UI scrollable connecté à l'endpoint `/chat` de l'agent IEIA via UnityWebRequest POST

---

## Projet B — Niveau Intermédiaire ⭐⭐
### SmartTwin-AR : Jumeau Numérique Hybride Desktop/Mobile avec Superposition AR et Agent IA Incarné

**Titre :**
SmartTwin-AR : Inspection Terrain Assistée par Réalité Augmentée et Agent Conversationnel
Incarné dans un Jumeau Numérique Industriel

**Description du concept :**
Le système opère sur deux interfaces simultanées et complémentaires. Sur Desktop, le
superviseur dispose du jumeau 3D complet (Projet A). Sur mobile Android/iOS, le technicien
de terrain pointe sa caméra vers un QR code fixé sur chaque métier à tisser : un panneau AR
ancré dans l'espace réel surgit au-dessus de la machine avec ses capteurs en temps réel
(puissance, vibration, température de cuve, niveau d'alerte). Une bague de santé colorée
pulse autour du socle physique de la machine. L'agent IEIA est incarné sous la forme d'un
drone virtuel visible dans la vue AR du technicien et dans le jumeau Desktop du superviseur :
il se déplace vers la machine à risque le plus élevé, y ancre un hologramme d'alerte, et
répond aux questions vocales du technicien via reconnaissance de la parole.

**Le rôle de l'IA et/ou la Big Data :**
IEIA orchestre deux flux de Big Data en parallèle : (1) le flux temps réel InfluxDB pour les
alertes immédiates (seuils dynamiques par machine, détection d'anomalie par z-score glissant)
et (2) l'historique de maintenance PostgreSQL pour contextualiser chaque alerte ("dernier
remplacement de roulement il y a 847h — durée de vie nominale 900h"). La reconnaissance
vocale (Whisper STT, exécuté localement sur le relay FastAPI) transforme les questions
orales du technicien en requêtes IEIA, dont les réponses sont synthétisées (TTS) et affichées
comme bulle de dialogue AR au-dessus du drone. Le drone lui-même utilise Unity AI
Navigation pour planifier ses déplacements vers les machines prioritaires.

**Type d'interaction :**
Hybride Desktop + Mobile AR. Coopération asymétrique légère : les deux utilisateurs voient
les mêmes données (via le même DataManager connecté au relay), mais depuis des
perspectives différentes. Le superviseur voit la globalité ; le technicien voit le détail local en
AR. La synchronisation est assurée par le relay FastAPI commun (pas de Netcode requis à
ce niveau).

**Contributions scientifiques possibles :**
- Évaluation de l'utilisabilité d'une interface AR mobile pour l'inspection de machines
  industrielles en comparaison avec une inspection papier ou tablette classique ;
- Mesure de l'impact de l'incarnation spatiale d'un agent IA (drone visible) sur la confiance
  de l'opérateur dans les recommandations de maintenance prédictive ;
- Étude de l'efficacité de la communication vocale homme-agent dans un environnement
  industriel bruyant (usine textile).

**Fonctionnalités clés Unity :**
- AR Foundation 6 (`ARTrackedImageManager`, `ARAnchorManager`, `ARPlaneManager`)
- `XRReferenceImageLibrary` avec QR codes encodant `device_id` des machines
- Billboard shaders pour les panneaux AR (LookAt caméra en `LateUpdate`)
- `UnityEngine.AI.NavMeshAgent` pour le déplacement du drone IEIA dans la scène 3D
- Whisper STT via HTTP POST au relay FastAPI (`/voice/query`) + TTS audio clip retourné
- Panneaux AR world-anchored persistant sur `ARAnchor` même lors du déplacement
- Particules volumétriques CO2 scalées par `cbam_contribution` (système existant étendu)
- Build Android ARCore + Build Desktop dans le même projet Unity (platform switching)

---

## Projet C — Niveau Avancé ⭐⭐⭐
### SmartTwin-Collab : Plateforme Multi-Utilisateurs Asymétrique avec Analytique Immersive et Formation Adaptative par IA

**Titre :**
SmartTwin-Collab : Supervision Asymétrique Multi-Utilisateurs, Analytique Immersive de
Données Industrielles et Formation Adaptative Pilotée par la Charge Cognitive en Réalité
Mixte

**Description du concept :**
La plateforme connecte simultanément trois types d'utilisateurs autour du même jumeau
numérique industriel : (1) Le Superviseur Desktop dispose d'une vue isométrique globale avec
un mode "Data Dive" qui transforme l'usine en espace analytique — les machines flottent,
se redimensionnent selon leur consommation énergétique, et des arcs de corrélation les
relient selon leurs signatures de défaillance communes. (2) Le Technicien AR navigue
physiquement dans l'usine réelle, guidé pas à pas par des instructions holographiques
contextuelles pour chaque procédure de maintenance, pendant que son comportement
cinématique (position des mains, orientation de la tête, temps de réaction par étape) est
loggé à haute fréquence. (3) Le Stagiaire VR suit une formation adaptative sur des machines
simulées — l'environnement (pannes, bruit, complexité des tâches) s'ajuste en temps réel en
fonction de sa charge cognitive inférée depuis ses micro-mouvements. Les trois utilisateurs
partagent le même espace de données via Netcode for GameObjects ; les marqueurs
holographiques placés par l'un apparaissent chez les autres instantanément.

**Le rôle de l'IA et/ou la Big Data :**
Quatre couches IA/Big Data opèrent en parallèle : (1) IEIA orchestrateur — LLM incarné en
drone autonome, répond à la voix, navigue vers les anomalies, génère des scénarios
contrefactuels CBAM en temps réel ; (2) Modèle de charge cognitive — réseau de neurones
LSTM entraîné sur les télémétries cinématiques (micro-mouvements, saccades de la tête,
temps par étape) pour inférer le niveau de charge sans capteur biométrique externe, et
ajuster la difficulté de la formation via Unity ML-Agents ; (3) Analytique immersive — pipeline
Spark/InfluxDB exécutant t-SNE et clustering DBSCAN sur l'historique énergétique multi-
machines pour regrouper visuellement les défaillances corrélées dans le "Data Mode" ; (4)
Pathfinding adaptatif — A* pondéré par les prédictions IEIA pour générer des itinéraires
d'inspection optimaux pour le technicien AR en fonction de l'urgence prédite de chaque
machine.

**Type d'interaction collaborative :**
Tripartite et asymétrique. Superviseur (omniscience des données, aucune action physique),
Technicien AR (action locale, vision limitée), Stagiaire VR (apprentissage guidé, feedback
adaptatif). Chaque rôle dépend des deux autres : le superviseur envoie des missions au
technicien via marqueurs holographiques synchronisés ; le technicien valide les étapes de
maintenance qui débloquent les modules de formation du stagiaire ; le stagiaire peut poser
des questions à IEIA qui répond simultanément dans les trois vues. La synchronisation
complète de l'état partagé est assurée par Netcode for GameObjects avec un hôte dédié
(relay FastAPI étendu comme serveur NGO).

**Contributions scientifiques possibles :**
- Évaluation de la performance de détection d'anomalies industrielles en analytique
  immersive 3D versus tableaux de bord 2D classiques (temps de détection, taux d'erreur) ;
- Validation de proxys cinématiques (sans biométrie externe) pour l'estimation de la charge
  cognitive en environnement de formation VR industriel ;
- Mesure de l'impact des interfaces asymétriques multi-rôles sur l'efficacité collective en
  maintenance industrielle préventive ;
- Étude de la confiance inter-utilisateurs et envers l'agent IA dans une collaboration
  homme-machine-homme spatialisée (Theory of Mind appliquée aux agents incarnés) ;
- Quantification de la réduction d'exposition CBAM achievable par optimisation IA du
  planning de maintenance dans le secteur textile marocain.

**Fonctionnalités clés Unity :**
- `Netcode for GameObjects 2.x` — synchronisation d'état tripartite, NetworkTransform
  pour les avatars et marqueurs holographiques, NetworkVariable pour les états machines
- AR Foundation 6 (mobile) + XR Interaction Toolkit 3 (VR casque) dans le même projet
- `Compute Shaders` pour le rendu de milliers d'arcs de corrélation en Data Mode sans
  chute de framerate (ECS/DOTS pour les nœuds dynamiques)
- `Unity ML-Agents` — environnement de formation dont la difficulté est pilotée par
  le modèle de charge cognitive LSTM entraîné hors ligne sur données synthétiques
- `Unity AI Navigation 2` (NavMesh dynamique) pour le pathfinding du drone IEIA et
  les itinéraires d'inspection adaptatifs du technicien AR
- Logging haute fréquence (60Hz) des télémétries cinématiques vers InfluxDB via
  batch write pour minimiser la latence réseau
- Whisper STT local (relay FastAPI) + TTS synchronisé dans les trois vues simultanément
- Pipeline Python Spark/Pandas exécuté côté relay pour le clustering temps réel
  (t-SNE, DBSCAN) avec envoi des coordonnées 3D résultantes vers Unity via WebSocket
