# SmartexVR — Module A — Vuforia (Android)

- Branche : `feature/a-vuforia-core`
- Date : 2026-05-23

Ce README décrit **tout le travail réalisé pour le Module A (Vuforia Android)** : corrections runtime, pipeline de build Android, gestion de secret (clé Vuforia) **sans fuite Git**, scène de base, validation sur téléphone, et points restants.

---

## 1) Résumé (ce qui a été livré)

- **APK Android fonctionnel** (objectif : démarrer Vuforia de façon déterministe sur Android).
- Correction de l’erreur Vuforia Android :
  - `Failed to create ImageTargetObserver... Make sure that a license key is provided...`
- Mise en place d’une **provision de licence déterministe** :
  - injection de licence au build (temporaire, restaurée après build)
  - bootstrap runtime très tôt (avant scène)
  - `DelayedInitialization` + `Initialize()` explicite
- **Gestion de la clé Vuforia** : clé fournie localement (EditorPrefs / variables d’environnement), pas stockée dans les assets du projet.

---

## 2) Contexte technique (confirmé dans le dépôt)

- Unity : `6000.3.11f1`
- Vuforia Engine : `11.4.4`
- Android :
  - Application Id : `ma.ensa.smartexvr`
  - `minSdkVersion` : 29
  - `targetSdkVersion` : 34
  - arm64 only : `AndroidTargetArchitectures: 2`
  - `stripEngineCode: 1`

---

## 3) Dataset Vuforia (targets)

Dataset embarqué :
- `Assets/StreamingAssets/Vuforia/SmartexMachines.xml`
- `Assets/StreamingAssets/Vuforia/SmartexMachines.dat`

Targets présents (8) :
- `machine_ESP32_TEX_001..008`

Mapping vers le device id :
- `machine_ESP32_TEX_00X` → `ESP32_TEX_00X`

---

## 4) Scène de base (wiring)

Scènes :
- `Assets/SmatexA.unity` : **scène principale** (activée dans Build Settings).
- `Assets/Scenes/SmartexAR.unity` : scène existante mais **non activée** au moment du rapport.

Dans `SmatexA.unity` :
- `ARCamera`
  - `VuforiaBehaviour`
  - `DefaultInitializationErrorHandler`
  - `VuforiaSessionManager`
- `TargetRegistry` (mapping 8 machines)
- `ImageTarget_001..008`
  - dataset : `Vuforia/SmartexMachines.xml`
  - bridge : `VuforiaTargetToMachineBridge` (événements FOUND/LOST)

---

## 5) Architecture de la correction (licence + init)

### 5.1 Problème initial
Sur Android, Vuforia peut déclencher l’initialisation native **avant** que la scène ne soit prête. Si la clé licence n’est pas déjà appliquée au bon moment, Vuforia échoue à créer les observers d’ImageTarget.

### 5.2 Solution retenue
- **Build-time injection** : on injecte la clé dans `VuforiaConfiguration` uniquement pendant le build, puis on restaure l’asset après build.
- **Bootstrap runtime très tôt** : applique la clé dès les hooks Unity les plus précoces.
- **Delayed initialization** : `delayedInitialization = true` et appel explicite à `VuforiaApplication.Instance.Initialize()` après chargement de la première scène.

---

## 6) Ce qui a été réalisé (Module A)

### 6.1 Runtime (Module A)
- `Assets/Scripts/AR/Core/VuforiaLicenseBootstrap.cs`
  - applique la licence **très tôt**
  - normalise la clé (whitespace)
  - aligne aussi les champs internes (`vuforiaLicenseKey`, `ufoLicenseKey`) via réflexion
  - déclenche `Initialize()` quand `DelayedInitialization` est activé

- `Assets/Scripts/AR/Core/VuforiaSessionManager.cs`
  - diagnostics + logs d’état (Initialized/Started/Paused/Error)

- `Assets/Scripts/AR/Core/TargetRegistry.cs`
  - mapping `machine_*` → `ESP32_*` (8 machines)

- `Assets/Scripts/AR/Recognition/VuforiaTargetToMachineBridge.cs`
  - transforme un status Vuforia (TRACKED/EXTENDED_TRACKED) en `FOUND/LOST`
  - émet vers le contrat `MachineQRTracker` (Module B)

### 6.2 Editor / Build tooling
- `Assets/Editor/SmartexBuildFixes/AndroidApkBuilder.cs`
  - menus :
    - `Smartex/Build/Set Vuforia License (Local)` (EditorPrefs)
    - `Smartex/Build/Build Android APK`
    - `Smartex/Build/Clear Vuforia License Cache (Project)`
  - injection + restauration de la licence pendant un build Android
  - build processor pour garantir l’injection sur Android

- `Assets/Editor/SmartexBuildFixes/UrpPipelineSetup.cs`
  - utilitaire d’alignement/config URP côté projet (setup cohérent pour Android)

- `Assets/Editor/Migration/AddVuforiaEnginePackage.cs`
  - script `InitializeOnLoad` (Editor) : détecte si Vuforia 11.4.4 est présent
  - peut proposer de copier un `.tgz` depuis `Assets/Editor/Migration/` vers `Packages/` puis mettre à jour `Packages/manifest.json`

- `Assets/Editor/Diagnostics/MissingScriptsFinder.cs`
  - menu `Smartex/Diagnostics/Find Missing Scripts`

### 6.3 Config / Assets (sans secret)
- `Assets/Resources/VuforiaConfiguration.asset`
  - `delayedInitialization` activé

- `Assets/Resources/ARConfig.asset`
  - asset de config utilisé par le Module A

---

## 6ter) À propos du package Vuforia (`.tgz`)

Le projet référence Vuforia via :

- `Packages/manifest.json` → `"com.ptc.vuforia.engine": "file:com.ptc.vuforia.engine-11.4.4.tgz"`

Le package utilisé est fourni dans le dépôt sous : `Packages/com.ptc.vuforia.engine-11.4.4.tgz`.

---

## 7) Gestion de la clé Vuforia (ce que j’ai utilisé)

- **EditorPrefs** : via le menu `Smartex/Build/Set Vuforia License (Local)`.
- **Variables d’environnement** :
  - `SMARTEX_VUFORIA_LICENSE_KEY`
  - `VUFORIA_LICENSE_KEY`

---

## 8) Build Android APK (ce qui a été fait)

- clé renseignée localement via `Smartex/Build/Set Vuforia License (Local)`
- build déclenché via `Smartex/Build/Build Android APK`
- sortie APK : `Builds/Android/SmartexAR.apk`
- installation test via `adb install -r Builds/Android/SmartexAR.apk`

---

## 9) Validation sur téléphone (logs utilisés)

Signaux utilisés pour valider l’exécution :
- licence appliquée très tôt (bootstrap)
- `Initialize()` appelé après 1ère scène si delayed init
- `Vuforia initialized (initError=NONE, ...)`
- `FOUND/LOST` émis par `VuforiaTargetToMachineBridge`

---

## 10) Point connu : confusion entre cibles (misrecognition)

Les cibles sont très similaires ("QR-ish"). Vuforia fait du **matching d’image** (features), il ne "décode" pas le QR.
Conséquence : il peut confondre `machine_ESP32_TEX_008` avec `..._002`.

Constat pendant les tests : avec des cibles visuellement proches, il peut y avoir des faux positifs (matching d’image).
