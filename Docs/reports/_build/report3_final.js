const L = require("./lib");
const { H1, H2, H3, P, bullet, numItem, run, code, caption, image, table, spacer, cover, buildDoc, save } = L;
const IMG = "images/";
// Auto-numbered figure captions (numbered in document order).
let __fig = 0;
const fcap = (t) => caption("Figure " + (++__fig) + " — " + t);

const SCHOOL = "École Nationale des Sciences Appliquées (ENSA)";
const FILIERE = "Filière Ingénierie en Systèmes d'Information et Big Data (ISIBD) — 2ème année (S8)";
const YEAR = "2025 – 2026";

const coverChildren = cover({
  school: SCHOOL,
  filiere: FILIERE,
  year: YEAR,
  projectTitle: "SmartexVR + AR",
  projectSub: "Jumeau Numérique Industriel & Réalité Augmentée\npour l'optimisation de la maintenance des usines textiles au Maroc",
  reportType: "RAPPORT FINAL DU PROJET",
  group: "Groupe 1",
  members: [
    { name: "Ahmed Ben Ahmed", role: "Chef de projet · Backend & QA" },
    { name: "Zahra JABER", role: "Module A — Cœur AR (Vuforia)" },
    { name: "Wissal CHEIKH", role: "Module B — Reconnaissance" },
    { name: "Radwa Tourabi", role: "Module C — Interface / Overlay" },
    { name: "Maryam Mouaki", role: "Module D — Maintenance" },
    { name: "Hiba Marir", role: "Module F — Formation" },
    { name: "Aboulaakoul Elwalid", role: "Module Assistant IA" },
  ],
  supervisor: "Pr. Hrimech Hamid & Pr. Oumeima",
  extra: [["Module", "Ingénierie et maquette numérique – projet AR"], ["Dépôt du code", "github.com/Ahmed-BenAhmed/SmartexVR"]],
  logos: [{ path: "images/logo_uh1.png", h: 52 }, { path: "images/logo_ensab.png", h: 52 }],
});

const body = [
  // 1
  H1("1. Introduction et contexte"),
  P("Le secteur textile représente l'un des piliers historiques de l'industrie marocaine, tant par le volume d'emplois qu'il génère que par sa contribution aux exportations vers l'Union Européenne. L'entrée en vigueur progressive du Mécanisme d'Ajustement Carbone aux Frontières (MACF, ou CBAM — Carbon Border Adjustment Mechanism) impose désormais aux producteurs exportateurs une traçabilité fine de l'empreinte carbone associée à chaque processus de fabrication. Dans ce contexte, la maîtrise de la consommation énergétique des machines de production devient à la fois un enjeu économique, environnemental et réglementaire."),
  P("Le projet SmartexVR s'inscrit dans cette problématique. Il propose une plateforme de supervision industrielle reposant sur un jumeau numérique (digital twin) d'une usine textile équipée de huit métiers à tisser Jacquard. Chaque métier est instrumenté par un microcontrôleur ESP32 qui mesure en continu la puissance consommée, les vibrations, la température du bain de teinture, la température du tissu et la tension du fil. Ces mesures alimentent une base de données temporelle, puis sont restituées à l'utilisateur de deux manières complémentaires : un jumeau numérique 3D consultable sur poste fixe ou casque VR, et une couche de réalité augmentée (AR) mobile permettant de visualiser, directement sur le terrain, les données vivantes d'une machine pointée par la caméra du téléphone."),
  P("Ce rapport final présente une synthèse de l'ensemble du projet : son architecture, sa pile technologique, la répartition du travail entre les sept membres de l'équipe, la méthodologie collaborative adoptée, les résultats obtenus et les difficultés rencontrées. Il accompagne les rapports individuels de module rédigés par chaque membre."),
  image(IMG + "fig_ar_overlay.png", { alt: "Overlay AR des données d'une machine" }),
  fcap("Vision du produit : superposition en réalité augmentée des données vivantes d'un métier à tisser."),

  // 2
  H1("2. Problématique et objectifs"),
  P("La question directrice du projet peut se formuler ainsi : comment rendre immédiatement intelligibles, pour un opérateur de terrain comme pour un superviseur, les données énergétiques et de santé d'un parc de machines textiles, tout en préparant la traçabilité carbone exigée par la réglementation CBAM ?"),
  H2("2.1. Objectifs fonctionnels"),
  numItem("Collecter en temps réel la télémétrie des huit métiers à tisser et la centraliser dans une base de données temporelle fiable."),
  numItem("Offrir un jumeau numérique 3D qui reflète l'état de santé de chaque machine (couleur du corps, anneau de santé, barre d'énergie)."),
  numItem("Permettre, en réalité augmentée mobile, la reconnaissance d'une machine physique et l'affichage d'un panneau de données flottant au-dessus d'elle."),
  numItem("Guider la maintenance par des callouts AR numérotés lorsqu'une machine entre en état critique."),
  numItem("Proposer un module de formation multilingue (arabe, français, anglais) pour l'onboarding des nouveaux opérateurs."),
  numItem("Autoriser l'assistance à distance par un expert qui annote en direct la vue AR du technicien."),
  numItem("Fournir un assistant conversationnel intelligent (IA) capable d'expliquer l'état d'une machine et de recommander des actions, à partir de données réelles et non inventées."),
  H2("2.2. Objectifs non fonctionnels"),
  bullet("Performance : fluidité d'au moins 45 à 60 images/seconde sur un téléphone Android de milieu de gamme, et 72 Hz sur casque Quest 2."),
  bullet("Robustesse : le système doit rester opérationnel même si un service externe (IA, base de données) est indisponible, grâce à des mécanismes de repli."),
  bullet("Séparation des responsabilités : chaque module compile indépendamment, sans dépendances croisées accidentelles."),
  bullet("Sécurité : aucune clé secrète (licence Vuforia, clé API d'IA) ne doit être versionnée dans le dépôt ni embarquée dans le client Unity."),

  // 3
  H1("3. Architecture générale du système"),
  P("L'architecture suit un flux de données unidirectionnel et clairement étagé, depuis le capteur physique jusqu'à l'affichage immersif. Le principe fondateur est que le client Unity ne parle jamais directement à la base de données ni au service d'IA : il consomme uniquement des contrats stables exposés par un service relais (backend)."),
  image(IMG + "fig_architecture.png", { alt: "Architecture de bout en bout du système" }),
  fcap("Vue conceptuelle de bout en bout : capteurs ESP32 → ingestion → base temporelle → backend → jumeau numérique VR et réalité augmentée mobile."),
  image(IMG + "m_arch_real.png", { alt: "Architecture technique détaillée SmartTex Morocco 4.0", maxH: 470 }),
  fcap("Architecture technique détaillée : capteurs (vibration, tension, température, courant, RFID), bus MQTT Mosquitto, cluster k3s (NiFi, InfluxDB, Grafana) et utilisateurs (responsable production, opérateur)."),
  P("Le backend joue un rôle pivot : il pré-traite la donnée InfluxDB et renvoie un instantané (FactorySnapshot) prêt à l'emploi. Côté Unity, la classe DataManager interroge en priorité le relais ; si celui-ci est indisponible, elle bascule automatiquement sur une lecture directe d'InfluxDB. Toutes les modalités (VR et AR) partagent strictement le même événement OnSnapshotUpdated, ce qui garantit la cohérence des données affichées quelle que soit l'interface."),
  H2("3.1. Contrats et localisation par cible (target-local)"),
  P("La reconnaissance de machine repose sur Vuforia. Lorsqu'une cible image est reconnue, un identifiant de machine (de ESP32_TEX_001 à ESP32_TEX_008) et la transformation 3D de la cible sont émis via le contrat IMachineRecognizer / RecognizedMachine. Les modules consommateurs (overlay, maintenance, formation, assistance distante) attachent leur contenu AR sous cette transformation d'ancrage, de sorte que les annotations restent solidaires de la machine même lorsque l'utilisateur se déplace. Les annotations distantes utilisent des coordonnées locales à la cible, garantissant un placement stable indépendamment du point de vue."),

  // 4
  H1("4. Pile technologique"),
  table(
    ["Couche", "Technologie", "Rôle"],
    [
      ["Capteurs", "ESP32 (8 unités)", "Mesure et émission de la télémétrie machine"],
      ["Ingestion", "Apache NiFi", "Collecte, nettoyage et routage des flux vers InfluxDB"],
      ["Stockage", "InfluxDB", "Base temporelle (bucket telemetry, mesure smartex_derived)"],
      ["Backend", "Python 3.12 / FastAPI", "Relais /snapshot, analytique, sessions, assistance IA"],
      ["IA", "Mistral (API Chat)", "Assistant de maintenance conversationnel ancré"],
      ["Moteur 3D", "Unity 6 (6000.3.11f1), URP", "Jumeau numérique et application AR"],
      ["AR", "Vuforia Engine 11.4.4", "Reconnaissance des machines par cible image"],
      ["Conteneurs", "Docker / docker-compose", "Déploiement reproductible du backend"],
      ["Versioning", "Git + Git LFS, GitHub", "Gestion du code et des binaires lourds"],
    ],
    [1800, 3200, 4026]
  ),
  caption("Tableau 1 — Synthèse de la pile technologique du projet."),

  // 5
  H1("5. Présentation des modules"),
  P("Le travail a été découpé en sept modules confiés chacun à un membre de l'équipe, auxquels s'ajoute le module transversal d'assistance par intelligence artificielle. Chaque module dispose de son propre assembly Unity (espace de noms Smartex.AR.*) afin d'isoler la compilation et d'éviter les dépendances croisées."),
  H2("5.1. Vue d'ensemble des responsabilités"),
  table(
    ["Module", "Responsable", "Contenu principal"],
    [
      ["A — Cœur AR (Vuforia)", "Zahra JABER", "Session AR, ancrage, cycle de vie Vuforia (session manager, bootstrap de licence, registre des cibles)"],
      ["B — Reconnaissance", "Wissal CHEIKH", "Cibles image Vuforia, pont cible→machine, base SmartexMachines.dat"],
      ["C — Interface / Overlay", "Radwa Tourabi", "Panneau flottant temps réel, liaison aux données, billboard face caméra"],
      ["D — Maintenance", "Maryam Mouaki", "Procédures pas-à-pas, callouts AR, journalisation des étapes"],
      ["F — Formation", "Hiba Marir", "Onboarding AR multilingue (ar/fr/en), quiz, tableau de progression"],
      ["Assistant IA", "Aboulaakoul Elwalid", "Assistant de maintenance Mistral ancré sur données réelles"],
      ["Backend, QA/DevOps & Gestion de projet", "Ahmed Ben Ahmed", "Pipeline NiFi→InfluxDB, relais /snapshot, analytique, services transverses, CI, intégration des modules"],
    ],
    [2050, 1700, 5276]
  ),
  caption("Tableau 2 — Répartition des modules entre les sept membres de l'équipe."),
  H2("5.2. Module A — Cœur AR (Vuforia) — Zahra JABER"),
  P("Ce module fournit la fondation sans laquelle aucune expérience AR ne fonctionne. Il gère le cycle de vie de la session Vuforia (VuforiaApplication), émet les événements de début et de perte de session, applique la licence Vuforia depuis la configuration ou l'environnement (jamais versionnée), et tient un registre associant le nom de chaque cible image à un identifiant de machine. Les autres modules s'appuient sur cette infrastructure pour ancrer leur contenu dans l'espace réel."),
  H2("5.3. Module B — Reconnaissance de machine — Wissal CHEIKH"),
  P("Le module B constitue le pont entre la machine physique et la donnée. Il embarque la base de données de cibles Vuforia (SmartexMachines.dat / .xml) et les textures de cibles pour les huit métiers. Lorsqu'une cible est détectée, un pont (VuforiaTargetToMachineBridge) traduit l'observation en un événement de reconnaissance de machine porteur de l'identifiant ESP32_TEX_00N. C'est cet événement qui déclenche l'overlay, le guide de maintenance et le module de formation."),
  H2("5.4. Module C — Overlay de données temps réel — Radwa Tourabi"),
  P("Pièce la plus visible de l'expérience AR, ce module fait apparaître au-dessus de la machine reconnue un panneau flottant qui affiche la puissance, les vibrations et le coût carbone CBAM, et se met à jour automatiquement à chaque instantané. Un anneau de santé coloré (vert → orange → rouge) et un halo d'alerte pulsé traduisent visuellement l'état de la machine. Un composant billboard maintient le panneau orienté vers la caméra."),
  H2("5.5. Module D — Workflow de maintenance — Maryam Mouaki"),
  P("Lorsqu'une machine présente un score de santé critique, ce module récupère auprès du backend une procédure de réparation pas-à-pas et affiche des callouts AR numérotés pointant vers les organes à inspecter. Chaque étape confirmée est journalisée côté backend (POST /maintenance/logs), bouclant la boucle entre l'application AR et l'historique de maintenance exploité par l'analytique et l'assistant IA."),
  H2("5.6. Assistance par expert distant (capacité backend) — Ahmed Ben Ahmed"),
  P("Le projet prévoit qu'un technicien sur le terrain puisse appeler un expert distant qui suit le flux AR et dessine des annotations (cercles, flèches, texte) apparaissant en temps réel, ancrées sur la machine. Le relais de cette fonctionnalité est implémenté au niveau du backend (création de session POST /sessions et canal WebSocket WS /ws/ar-session/{id}, avec annotations en coordonnées locales à la cible). L'intégration côté Unity constitue une vague ultérieure ; le socle serveur, lui, est en place."),
  H2("5.7. Module F — Formation et onboarding — Hiba Marir"),
  P("Destiné aux nouveaux opérateurs, ce module nomme en AR chaque composant d'un métier, puis lance un quiz interactif (« touchez le capteur de tension »). Le contenu est multilingue (arabe, français, anglais), avec le français comme langue par défaut. Le score est enregistré côté backend pour suivre la certification des opérateurs par type de machine."),
  H2("5.8. QA, DevOps, backend et gestion de projet — Ahmed Ben Ahmed"),
  P("Porté par le chef de projet (Ahmed Ben Ahmed), ce périmètre transversal couvre à la fois le socle backend (pipeline d'ingestion NiFi→InfluxDB, relais /snapshot, analytique, services transverses) et l'assurance qualité : profiler de performance embarqué, budgets de performance par device, intégration continue, suite de tests, et coordination de l'intégration des branches vers la branche principale. Il garantit que chaque contribution respecte les budgets de performance et compile proprement avant fusion."),
  H2("5.9. Module IA — Assistant de maintenance — Aboulaakoul Elwalid"),
  P("L'assistant IA, décrit en détail dans son rapport dédié, expose des points d'accès /assist/query, /assist/sessions/{id}/summary et /assist/sessions/{id}/report. Il s'appuie sur le modèle Mistral pour produire des conseils actionnables, mais uniquement à partir d'un contexte factuel assemblé par le backend (instantané machine, anomalies récentes, historique de maintenance, résumé de risque). En l'absence de clé API ou en cas d'échec du fournisseur, il bascule sur une réponse déterministe issue de l'analytique, sans jamais inventer de mesures."),

  // 6
  H1("6. Le pipeline d'ingestion des données"),
  P("La fiabilité de toute la plateforme repose sur la qualité de la donnée ingérée. Les huit ESP32 publient leurs mesures, qui sont collectées et normalisées par un flux Apache NiFi avant d'être écrites dans InfluxDB. La mesure smartex_derived expose les champs avg_power_watts, co2_kg_h et grid_ef, indexés par les tags device_id, machine_id et shift."),
  P("Le backend lit cette donnée selon deux sources interchangeables, sélectionnées par la variable d'environnement SMARTEX_DATA_SOURCE : une source mock (génération réaliste de signaux pour le développement et la démonstration, avec injection contrôlée d'anomalies et de pannes) et une source influx (lecture réelle via requête Flux). Cette dualité a permis à toute l'équipe de développer et tester sans dépendre de la disponibilité du serveur de production."),
  ...code([
    "from(bucket: \"telemetry\")",
    "  |> range(start: ..., stop: ...)",
    "  |> filter(fn: (r) => r._measurement == \"smartex_derived\")",
    "  |> filter(fn: (r) => r.device_id == \"ESP32_TEX_003\")",
    "  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")",
    "  |> sort(columns: [\"_time\"])",
  ]),
  fcap("Requête Flux type utilisée par le connecteur InfluxDB du backend."),

  // 7
  H1("7. Analytique et détection d'anomalies"),
  P("Au-delà du simple relais, le backend embarque une couche analytique déterministe qui qualifie l'état de chaque machine. Le score de santé est dérivé de la puissance consommée ; la détection d'anomalies repose sur une méthode statistique robuste fondée sur la médiane et l'écart absolu médian (MAD), insensible aux valeurs aberrantes."),
  H3("Principe de la détection MAD"),
  P("Pour chaque nouveau point, on calcule la médiane d'une fenêtre glissante d'historique, puis l'écart absolu médian. Le score d'anomalie est le rapport entre l'écart du point courant et un MAD mis à l'échelle (facteur 1,4826). Un point est signalé soit lorsque ce score dépasse un seuil, soit lorsque la puissance franchit la limite critique absolue. La sévérité (warning ou critical) et le sens de l'écart (au-dessus / au-dessous de la ligne de base) sont précisés dans le message."),
  P("Un soin particulier a été porté aux cas limites : lorsque l'historique est parfaitement plat (MAD nul), un point identique à la médiane ne déclenche aucune alerte (évitant une division par zéro et les faux positifs), tandis qu'un écart réel est correctement signalé. Cette robustesse a été vérifiée par des tests dédiés."),
  image(IMG + "fig_anomaly.png", { alt: "Détection d'anomalie par écart à la ligne de base" }),
  fcap("Détection d'anomalie : un pic de puissance s'écartant nettement de la ligne de base est signalé (méthode médiane / MAD)."),

  // 8
  H1("8. Méthodologie de travail et gestion de projet"),
  P("Le projet a été mené en équipe de sept personnes avec une organisation explicite, pilotée par le chef de projet. Le travail a été découpé en modules indépendants, documentés dans un fichier README détaillé précisant, pour chaque membre, le périmètre, les fichiers à produire, l'assembly concerné, les concepts à maîtriser et la manière dont le module s'articule avec le produit final."),
  H2("8.1. Vagues de livraison"),
  table(
    ["Vague", "Périmètre", "Objectif"],
    [
      ["Vague 1", "Modules A + B + C", "Scanner une machine → panneau de données AR en direct (jalon démo)"],
      ["Vague 2", "Modules D + G", "Réparation guidée pas-à-pas"],
      ["Vague 3", "Module F", "Parcours d'onboarding des opérateurs"],
      ["Vague 4", "Module E", "Expert distant avec annotations en direct"],
      ["Vague 5", "Toute l'équipe", "QA finale + APK pilote pour l'usine"],
    ],
    [1300, 3200, 4526]
  ),
  caption("Tableau 3 — Vagues de livraison successives du projet."),
  H2("8.2. Flux Git et discipline de fusion"),
  P("Chaque membre a développé sur une branche dédiée (feature/ar-member-N-…), avec interdiction de pousser directement sur master. Les fusions vers la branche principale passaient par une revue et étaient validées par le responsable QA, garant de la stabilité. Les fichiers de scène Unity (.unity), qui fusionnent mal, faisaient l'objet d'une coordination explicite. Git LFS a été configuré pour les binaires lourds (FBX, PNG, DLL, paquet Vuforia .tgz), évitant d'alourdir l'historique."),

  // 9
  H1("9. Assurance qualité et intégration"),
  P("La phase d'intégration a constitué un moment critique du projet. Les branches des différents membres, parfois issues d'états antérieurs de master, contenaient à la fois le code utile de chaque module et du bruit accidentel (scènes de test, exemples temporaires, conflits de verrous de paquets, régénérations de GUID). La stratégie retenue a été une intégration propre sur une branche dédiée, ne ramenant que le code réel de chaque module et ses scènes intentionnelles."),
  H2("9.1. Standardisation sur Vuforia"),
  P("Le dépôt contenait des vestiges de deux piles AR concurrentes (Vuforia et AR Foundation). Conformément à la direction technique, Vuforia a été retenu comme pile canonique : la logique des modules a été intégrée, les paquets AR Foundation conservés pour la résolution des assemblies, et les scènes actives basculées sur Vuforia derrière le contrat stable IMachineRecognizer."),
  H2("9.2. Vérification de la compilation"),
  P("L'intégration complète a été validée par une compilation Unity en mode headless (batchmode), confirmant zéro erreur de compilation sur l'ensemble des assemblies (Vuforia, ARCore/ARKit/AR Foundation 6.1, et tous les assemblies Smartex). Côté backend, la suite de tests (16 tests automatisés couvrant l'API, l'analytique et le client IA) passe intégralement, et un test de bout en bout a été exécuté contre un serveur réel."),

  // 10 — REALISATIONS GALLERY (real screenshots from each member's work)
  H1("10. Réalisations : captures du système réalisé"),
  P("Cette section rassemble des captures réelles du système livré, organisées par couche et par module. Elles attestent du fonctionnement effectif de la chaîne, de l'infrastructure backend jusqu'à la reconnaissance AR sur appareil et l'overlay de données."),

  H2("10.1. Infrastructure et données (backend — Ahmed Ben Ahmed)"),
  image(IMG + "m_ahmed_infra.png", { alt: "Déploiement Kubernetes du backend" }),
  fcap("Déploiement réel du backend sur un cluster Kubernetes (k3s) : pods InfluxDB, NiFi, Grafana, Mosquitto, Flink et simulateurs en exécution."),
  image(IMG + "m_ahmed_grafana.png", { alt: "Tableau de bord Grafana Fleet Overview" }),
  fcap("Tableau de bord de supervision Grafana « Fleet Overview » : machines en ligne, puissance totale, alertes sur 24 h et température moyenne du tissu."),
  image(IMG + "m_ahmed_influx.png", { alt: "Exploration des données InfluxDB" }),
  fcap("Exploration de la télémétrie dans InfluxDB (Data Explorer, bucket smartex_telemetry, par device_id)."),

  H2("10.2. Jumeau numérique 3D (Unity)"),
  image(IMG + "m_ahmed_twin.png", { alt: "Jumeau numérique 3D de l'usine" }),
  fcap("Le jumeau numérique 3D : les métiers à tisser avec leur anneau de santé au sol (vert → orange → rouge) et leur barre d'énergie."),

  H2("10.3. Module A — Cœur AR et reconnaissance Vuforia (Zahra JABER)"),
  image(IMG + "m_a_archi.png", { alt: "Flux d'initialisation et de reconnaissance Vuforia", maxH: 360 }),
  fcap("Flux d'initialisation et de reconnaissance Vuforia : du bootstrap de licence au registre des cibles."),
  image(IMG + "m_a_scene.png", { alt: "Scène SmartexA avec 8 ImageTargets" }),
  fcap("Scène SmartexA : les huit ImageTargets (ESP32_TEX_001 à 008) organisés sous l'ARCamera et le TargetRegistry."),
  image(IMG + "m_a_tracking.jpg", { alt: "Reconnaissance AR sur appareil", maxH: 380 }),
  fcap("Reconnaissance réelle sur appareil : la cible « Loom 005 » est détectée (overlay « AR Tracking: FOUND ESP32_TEX_005 »)."),

  H2("10.4. Module B — Reconnaissance de machine (Wissal CHEIKH)"),
  image(IMG + "m_b_imagetarget.jpg", { alt: "Configuration d'une Image Target Vuforia", maxH: 300 }),
  fcap("Configuration d'une Image Target Vuforia : source « From Database », base SmartexMachines, cible machine_ESP32_TEX."),
  image(IMG + "m_qr_loom.png", { alt: "Cible QR imprimée posée sur un métier à tisser", maxH: 360 }),
  fcap("Cible de reconnaissance (QR-code « Loom / ESP32_TEX_005 ») imprimée et apposée sur un métier à tisser réel."),
  image(IMG + "m_b_hierarchy.jpg", { alt: "Hiérarchie de la scène de reconnaissance" }),
  fcap("Hiérarchie de la scène de reconnaissance (SmartexManager, FactoryRoot, ARCamera, cible ESP32_TEX_001)."),

  H2("10.5. Module C — Overlay de données AR (Radwa Tourabi)"),
  image(IMG + "m_c_overlay.png", { alt: "Panneau d'overlay AR au-dessus d'une machine" }),
  fcap("Panneau d'overlay de données positionné au-dessus de la machine reconnue (nom, puissance, santé, halo d'alerte)."),
  image(IMG + "m_c_canvas.png", { alt: "Structure du Canvas d'overlay", maxH: 380 }),
  fcap("Structure du Canvas d'overlay : BackgroundPanel, MachineNameText, HealthText, PowerText et RedHaloAlert."),

  H2("10.6. Module Assistant IA (Aboulaakoul Elwalid)"),
  image(IMG + "m_ai_evidence.png", { alt: "Tableau de bord de preuve de la couche IA", maxH: 470 }),
  fcap("Preuve de fonctionnement de la couche IA : santé de l'API, IA configurée (fournisseur Mistral), machine analysée (ESP32_TEX_003) avec anomalies détectées, et réponse technicien réellement générée par l'IA avec ses actions recommandées."),

  // 11
  H1("11. Difficultés rencontrées et solutions"),
  table(
    ["Difficulté", "Solution apportée"],
    [
      ["Le paquet Vuforia (.tgz, 138 Mo) était suivi comme pointeur LFS et invalidait l'import Unity.", "Téléchargement effectif via git lfs pull, puis ajout d'une règle *.tgz dans .gitattributes pour garantir un suivi LFS correct (pointeur de 134 octets et non binaire brut)."],
      ["Régénération de GUID (.meta) sur des branches issues d'un master ancien, menaçant les liens scripts/scènes.", "Conservation des GUID canoniques et remappage des références dans les scènes (matériaux, mock recognizer) plutôt qu'écrasement des métadonnées."],
      ["Deux piles AR concurrentes (Vuforia vs AR Foundation).", "Standardisation sur Vuforia derrière un contrat stable, conservation des paquets nécessaires à la compilation."],
      ["Réponse Mistral malformée (corps 200 mais JSON invalide ou choices vide) pouvant provoquer une erreur 500 côté client.", "Élargissement de la gestion d'erreurs pour basculer systématiquement sur la réponse déterministe ; tests de régression ajoutés."],
      ["Fusion difficile des fichiers de scène .unity.", "Coordination explicite et intégration sur branche dédiée, jamais directement sur master."],
    ],
    [3800, 5226]
  ),
  caption("Tableau 4 — Principales difficultés et solutions."),

  // 12
  H1("12. Contributions et apports"),
  bullet("Une chaîne complète et opérationnelle de bout en bout : du capteur ESP32 jusqu'à la visualisation immersive, avec séparation stricte des responsabilités."),
  bullet("Une couche d'IA industrielle ancrée sur des faits, avec garde-fous explicites contre l'invention de mesures et repli déterministe — un modèle de confiance adapté à un contexte critique."),
  bullet("Une approche de la traçabilité carbone CBAM intégrée nativement (coût CBAM par machine, CO₂ estimé), pertinente pour le secteur textile MENA."),
  bullet("Une méthodologie d'intégration multi-équipes reproductible, documentée, sans conflits de fusion bloquants."),
  bullet("Une expérience AR comparée à un tableau de bord 2D classique pour la détection d'anomalies, ouvrant la voie à une évaluation ergonomique."),

  // 13
  H1("13. Conclusion et perspectives"),
  P("Le projet SmartexVR démontre la faisabilité d'une plateforme industrielle combinant jumeau numérique 3D, réalité augmentée mobile et assistance par intelligence artificielle, au service de la supervision énergétique et de la conformité carbone d'une usine textile marocaine. L'architecture étagée, la discipline d'intégration et les mécanismes de repli confèrent à l'ensemble une robustesse adaptée à un déploiement pilote."),
  P("Plusieurs perspectives se dégagent pour la suite : le câblage final des modules consommateurs sur le service de reconnaissance Vuforia (ARServices.Recognizer), la réalisation des tests de reconnaissance sur appareil physique avec cibles réelles, l'ajout du flux vidéo WebRTC pour l'assistance distante une fois la signalisation texte stabilisée, l'enrichissement du module d'analytique immersive, et une étude d'évaluation comparant l'efficacité de l'AR par rapport aux tableaux de bord 2D pour la prise de décision en maintenance."),
  P("Au-delà de ses résultats techniques, le projet aura constitué une expérience formatrice de travail collaboratif structuré, où la qualité du processus (découpage, documentation, intégration, tests) s'est révélée aussi déterminante que la qualité du code lui-même."),

  // 14 — ANNEXES
  new L.Paragraph({ children: [new L.PageBreak()] }),
  H1("14. Annexes — Rapports individuels des membres"),
  P("Les rapports individuels rédigés par chaque membre de l'équipe sont joints ci-après, dans l'ordre indiqué dans le tableau ci-dessous. Ils détaillent, pour chaque module, la conception, la mise en œuvre et les résultats obtenus."),
  table(
    ["Annexe", "Rapport", "Membre", "Pages"],
    [
      ["Annexe A", "Module A — Cœur AR (Vuforia / Android)", "Zahra JABER", "27"],
      ["Annexe B", "Module B — Reconnaissance de machine", "Wissal CHEIKH", "14"],
      ["Annexe C", "Module C — Interface / Overlay de données AR", "Radwa Tourabi", "26"],
      ["Annexe D", "Module D — Flux de maintenance AR", "Maryam Mouaki", "22"],
      ["Annexe E", "Module Assistant IA", "Aboulaakoul Elwalid", "12"],
      ["Annexe F", "Module F — Formation et onboarding", "Hiba Marir", "18"],
      ["Annexe G", "Rapport individuel — Chef de projet, Backend & QA", "Ahmed Ben Ahmed", "9"],
    ],
    [1200, 4226, 2400, 1200]
  ),
  caption("Tableau 5 — Liste des rapports individuels joints en annexe."),
  P("Note : ces annexes sont intégrées dans la version PDF complète du rapport (fichier « Rapport_Final_SmartexVR_avec_Annexes.pdf »), à la suite de la présente page."),
];

const doc = buildDoc({ coverChildren, tocTitle: "Sommaire", body });
save(doc, process.argv[2]).then((p) => console.log("WROTE", p));
