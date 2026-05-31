const L = require("./lib");
const { H1, H2, H3, P, bullet, numItem, run, code, caption, image, table, spacer, cover, buildDoc, save } = L;
const IMG = "images/";

const coverChildren = cover({
  school: "École Nationale des Sciences Appliquées (ENSA)",
  filiere: "Filière Ingénierie en Systèmes d'Information et Big Data (ISIBD) — 2ème année (S8)",
  year: "2025 – 2026",
  projectTitle: "SmartexVR + AR",
  projectSub: "Rapport de contribution au projet\nConception d'un jumeau numérique et d'une interface AR pour l'optimisation\nde la maintenance des usines textiles au Maroc",
  reportType: "RAPPORT INDIVIDUEL DE CONTRIBUTION",
  group: "Groupe 1",
  author: "Ahmed Ben Ahmed",
  role: "Chef de projet — Backend & pipeline de données — QA / DevOps & Intégration",
  supervisor: "Pr. Hrimech Hamid & Pr. Oumeima",
  extra: [["Module", "Ingénierie et maquette numérique – projet AR"], ["Dépôt du code", "github.com/Ahmed-BenAhmed/SmartexVR"]],
  logos: [{ path: "images/logo_uh1.png", h: 52 }, { path: "images/logo_ensab.png", h: 52 }],
});

const body = [
  H1("1. Introduction et périmètre de ma contribution"),
  P("Ce rapport présente ma contribution personnelle au projet SmartexVR, une plateforme de jumeau numérique et de réalité augmentée pour une usine textile marocaine. Contrairement à mes camarades, dont chacun s'est concentré sur un module fonctionnel précis, mon rôle a été à la fois transversal et technique. Il a couvert quatre volets complémentaires :"),
  bullet("la genèse et la proposition du projet, ainsi que la définition de sa vision ;"),
  bullet("la gestion de l'équipe : découpage du travail, attribution des modules, coordination et prévention des conflits ;"),
  bullet("la conception et la réalisation du backend et de la chaîne d'ingestion des données (des simulateurs/capteurs vers InfluxDB via Apache NiFi) ;"),
  bullet("l'assurance qualité (module QA/DevOps) et l'intégration finale des contributions de tous les membres vers la branche principale."),
  P("Ces responsabilités m'ont placé au point de jonction de toutes les briques du projet. Le présent document détaille chacune d'elles, les choix techniques opérés, les difficultés rencontrées et les résultats obtenus."),
  image(IMG + "fig_ar_overlay.png", { alt: "Produit visé : overlay AR des données machine" }),
  caption("Figure 1 — Le produit visé : visualisation en réalité augmentée des données vivantes d'une machine, alimentée par le pipeline de données dont j'ai eu la charge."),

  H1("2. Genèse et proposition du projet"),
  P("Je suis à l'origine de l'idée du projet. Le point de départ a été un constat double : d'une part, l'industrie textile marocaine, fortement exportatrice vers l'Union Européenne, va devoir se conformer au Mécanisme d'Ajustement Carbone aux Frontières (CBAM), qui exige une traçabilité de l'empreinte carbone ; d'autre part, les outils de supervision énergétique existants restent souvent des tableaux de bord 2D peu lisibles pour les opérateurs de terrain."),
  P("J'ai donc proposé de concevoir un jumeau numérique d'une usine textile équipée de huit métiers à tisser Jacquard instrumentés par des ESP32, restitué à la fois sous forme de twin 3D (VR / poste fixe) et de couche de réalité augmentée mobile, le tout adossé à une chaîne de données temps réel et à un assistant intelligent. Cette vision a structuré l'ensemble du travail de l'équipe et défini les sept modules confiés aux membres."),
  P("J'ai formalisé cette vision dans la documentation du projet (objectif final, contributions scientifiques visées, pile technologique), afin que chaque membre dispose d'un cap clair et partagé."),

  H1("3. Gestion d'équipe et découpage des tâches"),
  P("En tant que chef de projet, j'ai assuré l'organisation d'une équipe de sept personnes. Mon premier livrable de coordination a été la rédaction d'un fichier README exhaustif, véritable cahier des charges opérationnel du projet."),
  H2("3.1. Le README comme contrat d'équipe"),
  P("Pour chaque membre, le README précise : le périmètre du module, les fichiers exacts à produire, l'assembly Unity concerné, les concepts techniques à maîtriser, et surtout la manière dont le module s'articule avec les autres (« How it connects »). Il documente également le flux de données central, les conventions partagées (identifiants de machine, URL backend, abonnement aux événements, gestion des couleurs, espaces de noms) et les règles de collaboration Git."),
  P("Cette documentation a joué un rôle déterminant : elle a permis à chaque membre de travailler de façon autonome sur son module sans empiéter sur le travail des autres, et a servi de référence commune tout au long du projet."),
  H2("3.2. Attribution des modules et vagues de livraison"),
  P("J'ai réparti le travail en sept modules indépendants et défini un calendrier en vagues de livraison successives, en plaçant en priorité le chemin critique de la démonstration (scanner une machine → afficher ses données en AR)."),
  table(
    ["Module", "Membre", "Responsabilité"],
    [
      ["A — Cœur AR (Vuforia)", "Zahra JABER", "Session AR, ancrage, cycle de vie Vuforia"],
      ["B — Reconnaissance", "Wissal CHEIKH", "Cibles image, pont cible → machine"],
      ["C — Interface / Overlay", "Radwa Tourabi", "Panneau de données AR temps réel"],
      ["D — Maintenance", "Maryam Mouaki", "Procédures et callouts AR de réparation"],
      ["F — Formation", "Hiba Marir", "Onboarding multilingue + quiz"],
      ["Assistant IA", "Aboulaakoul Elwalid", "Assistant Mistral ancré sur données réelles"],
      ["Backend, QA/DevOps & Gestion", "Ahmed Ben Ahmed (moi)", "Ingestion NiFi→InfluxDB, relais, analytique, CI, intégration"],
    ],
    [2050, 1900, 5076]
  ),
  caption("Tableau 1 — Découpage du travail que j'ai défini pour l'équipe (7 membres)."),
  H2("3.3. Coordination et prévention des conflits"),
  P("J'ai mis en place une discipline Git stricte : une branche par membre, interdiction de pousser directement sur master, et coordination explicite sur les fichiers sensibles aux fusions (notamment les scènes .unity). J'ai veillé à ce que deux membres ne travaillent jamais simultanément sur les mêmes fichiers, en m'appuyant sur la séparation par assemblies (chaque module dans son propre Smartex.AR.*), ce qui isole la compilation et supprime les dépendances croisées accidentelles."),

  H1("4. Architecture du dépôt et conventions techniques"),
  P("J'ai défini la structure du dépôt et les conventions transversales qui garantissent la cohérence du code produit par sept personnes."),
  H2("4.1. Organisation des dossiers"),
  P("Le code Unity est organisé en couches explicites : une couche données (Core : DataManager, InfluxDBClient, modèles), une couche visuels VR (Machines), une couche UI partagée, et la couche AR découpée en un dossier par module. Cette organisation, documentée dans le README, rend la responsabilité de chaque fichier immédiatement lisible."),
  H2("4.2. Assemblies et isolation de compilation"),
  P("Chaque module possède son propre fichier d'assembly (.asmdef) avec des références explicites et non transitives. Ce choix architectural empêche un module de dépendre accidentellement d'un autre et permet à chaque membre de compiler son périmètre indépendamment — un filet de sécurité essentiel dans un projet collaboratif."),
  H2("4.3. Git LFS pour les binaires lourds"),
  P("J'ai configuré Git LFS pour suivre les fichiers binaires volumineux (FBX, PNG, DLL, paquets). Cela évite d'alourdir l'historique Git et de saturer le dépôt. Cette configuration s'est révélée critique lors de l'intégration du paquet Vuforia (138 Mo), comme détaillé en section 7."),

  H1("5. Le pipeline d'ingestion des données"),
  P("Le cœur technique de ma contribution réside dans la chaîne qui amène la donnée des machines jusqu'à l'application. Sans donnée fiable, ni le jumeau numérique ni l'AR n'ont de sens."),
  H2("5.1. De l'ESP32 à InfluxDB via Apache NiFi"),
  P("Chaque métier à tisser est instrumenté par un ESP32 qui mesure puissance, vibrations, températures (bain de teinture et tissu), tension du fil et qualité du signal WiFi. Ces flux sont collectés, nettoyés et routés par un pipeline Apache NiFi que j'ai mis en place, puis écrits dans une base de données temporelle InfluxDB."),
  image(IMG + "fig_architecture.png", { alt: "Architecture de bout en bout du pipeline de données" }),
  caption("Figure 2 — Architecture de bout en bout : des capteurs ESP32 vers Apache NiFi, puis InfluxDB et le backend, jusqu'aux clients VR et AR."),
  P("Dans InfluxDB, la mesure smartex_derived expose les champs avg_power_watts, co2_kg_h et grid_ef, indexés par les tags device_id (ESP32_TEX_001 à 008), machine_id et shift. Ce schéma a été pensé pour servir directement les besoins d'analytique (puissance, CO₂, facteur d'émission réseau) sans transformation lourde côté client."),
  H2("5.2. Le connecteur InfluxDB de production"),
  P("J'ai développé, côté backend, le connecteur qui lit la donnée de production. Il interroge InfluxDB en langage Flux, pivote les champs en lignes exploitables, et reconstruit des objets de télémétrie typés. Le connecteur est encapsulé derrière une interface commune (TelemetryRepository), ce qui permet de basculer entre une source réelle (influx) et une source simulée (mock) par une simple variable d'environnement."),
  ...code([
    "from(bucket: \"telemetry\")",
    "  |> range(start: ..., stop: ...)",
    "  |> filter(fn: (r) => r._measurement == \"smartex_derived\")",
    "  |> filter(fn: (r) => r.device_id == \"ESP32_TEX_00N\")",
    "  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")",
    "  |> sort(columns: [\"_time\"])",
  ]),
  caption("Figure 3 — Requête Flux du connecteur de production."),
  H2("5.3. Source simulée et instantané relais"),
  P("Pour permettre à toute l'équipe de développer sans dépendre du serveur de production, j'ai implémenté un dépôt de télémétrie simulé générant des signaux réalistes (variation journalière, bruit, injection contrôlée d'anomalies et de pannes). Le backend agrège ces données en un instantané (FactorySnapshot) exposé via l'endpoint /snapshot, que le client Unity interroge en priorité avant de retomber, le cas échéant, sur une lecture directe d'InfluxDB."),
  P("Au-delà du relais de télémétrie, j'ai également exposé côté backend les services transverses nécessaires aux autres modules : les procédures et journaux de maintenance, le contenu de formation, et le relais d'assistance distante (création de session et canal WebSocket d'annotations : POST /sessions, WS /ws/ar-session/{id}). Cette base de services communs a permis à chaque membre de brancher son module sur des contrats stables, sans réécrire de logique serveur."),

  H1("6. Module QA / DevOps"),
  P("En tant que responsable du module 7, j'ai porté l'assurance qualité et l'outillage du projet."),
  H2("6.1. Budgets de performance"),
  P("J'ai défini une référence de performance (performance baseline) fixant, pour chaque device cible, des objectifs mesurables : au moins 45–60 images/seconde sur Android de milieu de gamme, 72 Hz sur Quest 2, latence de tracking maîtrisée, budget mémoire et nombre d'appels de rendu plafonnés. Toute régression supérieure à 10 % bloque la fusion tant qu'elle n'est pas expliquée."),
  table(
    ["Cible de performance", "Objectif"],
    [
      ["Fréquence d'images (Android milieu de gamme)", "≥ 45–60 fps"],
      ["Fréquence d'images (Quest 2)", "≥ 72 Hz"],
      ["Latence d'acquisition du tracking", "≤ 600 ms"],
      ["Budget mémoire", "maîtrisé (< 800 Mo)"],
      ["Appels coûteux (FindObjectsByType) par frame", "≤ 2"],
    ],
    [5800, 3226]
  ),
  caption("Tableau 2 — Extrait des budgets de performance que j'ai fixés."),
  H2("6.2. Profiler embarqué, CI et tests"),
  P("Le module QA comprend un profiler de performance embarqué (activable uniquement en build de développement), un pipeline d'intégration continue pour la compilation Unity, et une stratégie de tests. Côté backend, j'ai mis en place une suite de 16 tests automatisés couvrant l'API, l'analytique (détection d'anomalies) et le client d'assistance IA."),
  H2("6.3. Analytique et détection d'anomalies"),
  P("J'ai conçu la couche analytique déterministe du backend, dont la détection d'anomalies fondée sur la médiane et l'écart absolu médian (MAD), robuste aux valeurs aberrantes. J'ai porté une attention particulière aux cas limites — historique parfaitement plat (MAD nul) sans faux positif ni division par zéro, distinction entre écart au-dessus et au-dessous de la ligne de base, et bornage du score de santé. Ces comportements sont garantis par des tests dédiés."),
  image(IMG + "fig_anomaly.png", { alt: "Détection d'anomalie de puissance" }),
  caption("Figure 4 — Détection d'anomalie : un pic s'écartant nettement de la ligne de base est signalé, tandis qu'un historique stable ne génère aucun faux positif."),

  H1("7. Intégration et fusion des contributions"),
  P("La phase finale, dont j'avais la charge, a consisté à fusionner le travail de tous les membres en un produit unique et fonctionnel, sans casser l'existant. Chaque membre ayant poussé son travail sur une branche, j'ai procédé à une intégration propre sur une branche dédiée, validée avant toute fusion vers master."),
  H2("7.1. Stratégie d'intégration propre"),
  P("Les branches, parfois issues d'états antérieurs de master, mêlaient le code utile de chaque module à du bruit accidentel (scènes de test, exemples temporaires, régénérations de GUID, conflits de verrous de paquets). Ma stratégie a été de ne ramener que le code réel de chaque module et ses scènes intentionnelles, en écartant systématiquement le bruit. J'ai intégré les modules un par un (A, B, C, D, F), en vérifiant à chaque étape la cohérence des références."),
  H2("7.2. Standardisation sur Vuforia"),
  P("Le dépôt contenait des vestiges de deux piles AR concurrentes (Vuforia et AR Foundation). J'ai retenu Vuforia comme pile canonique, conformément à la direction technique : intégration de la logique des modules, conservation des paquets AR Foundation nécessaires à la résolution des assemblies, et bascule des scènes actives sur Vuforia derrière le contrat stable IMachineRecognizer."),
  H2("7.3. Difficultés d'intégration et solutions"),
  table(
    ["Difficulté", "Solution"],
    [
      ["Paquet Vuforia (.tgz, 138 Mo) suivi comme pointeur LFS, invalidant l'import Unity.", "git lfs pull pour matérialiser le binaire, puis règle *.tgz dans .gitattributes pour un suivi LFS correct (pointeur de 134 octets, non binaire brut)."],
      ["Régénération de GUID (.meta) menaçant les liens scripts ↔ scènes.", "Conservation des GUID canoniques et remappage des références dans les scènes, plutôt qu'écrasement des métadonnées."],
      ["Deux piles AR concurrentes.", "Standardisation sur Vuforia derrière un contrat stable."],
      ["Fusion difficile des scènes .unity.", "Intégration sur branche dédiée et coordination explicite, jamais directement sur master."],
    ],
    [3800, 5226]
  ),
  caption("Tableau 3 — Difficultés d'intégration et solutions apportées."),
  H2("7.4. Vérification finale"),
  P("J'ai validé l'intégration complète par une compilation Unity en mode headless : zéro erreur de compilation sur l'ensemble des assemblies (Vuforia, ARCore/ARKit/AR Foundation 6.1, et tous les assemblies Smartex). La suite de tests backend (16 tests) passe intégralement, et un test de bout en bout a été exécuté contre un serveur réel. La fusion vers master n'a été réalisée qu'après cette vérification, garantissant le respect de la consigne : « tout doit continuer à fonctionner »."),

  H1("8. Bilan et compétences acquises"),
  P("Ma contribution a couvert l'amont (idée, vision, organisation), le cœur technique (ingestion, backend, analytique) et l'aval (QA, intégration). Cette position transversale m'a permis de garantir la cohérence d'ensemble du projet et la qualité du produit livré."),
  H2("8.1. Compétences mobilisées"),
  bullet("Gestion de projet et d'équipe : découpage du travail, documentation de référence, coordination, prévention des conflits."),
  bullet("Ingénierie de données : pipeline d'ingestion Apache NiFi, modélisation temporelle InfluxDB, requêtage Flux."),
  bullet("Développement backend : FastAPI, conception d'API, analytique déterministe, tests automatisés."),
  bullet("Ingénierie logicielle et DevOps : Git/Git LFS, intégration continue, stratégie de fusion, budgets de performance."),
  H2("8.2. Perspectives"),
  P("Les évolutions que j'identifie pour la suite concernent le câblage final des modules consommateurs sur le service de reconnaissance Vuforia, le branchement du connecteur InfluxDB de production sur le serveur réel en conditions d'usine, le renforcement de la couverture de tests d'intégration, et l'industrialisation du pipeline NiFi (supervision, reprise sur incident). Sur le plan personnel, ce projet a consolidé ma capacité à conduire une équipe technique tout en contribuant moi-même aux briques les plus critiques du système."),
];

const doc = buildDoc({ coverChildren, tocTitle: "Sommaire", body });
save(doc, process.argv[2]).then((p) => console.log("WROTE", p));
