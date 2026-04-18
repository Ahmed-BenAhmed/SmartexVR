#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Smartex.Core;
using Smartex.Factory;
using Smartex.UI;
using Smartex.CameraControl;
using Smartex.Machines;

namespace Smartex.Editor
{
    public static class SmartexSceneBuilder
    {
        // ── Material creation ────────────────────────────────────────────────
        // Run this ONCE before "Build Scene" to create proper URP factory materials.
        // Manually-written .mat files get version:-1 from Unity's importer and render
        // magenta. Creating them through AssetDatabase.CreateAsset() here means Unity
        // owns the import process and the URP version stamp is set correctly.
        [MenuItem("Smartex/Setup/Create Factory Materials %#m")]
        public static void CreateFactoryMaterials()
        {
            const string dir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Materials");

            CreateOrUpdateMat($"{dir}/FloorMaterial.mat",    new Color(0.118f, 0.133f, 0.157f), 0.15f);
            CreateOrUpdateMat($"{dir}/WallMaterial.mat",     new Color(0.220f, 0.235f, 0.267f), 0.25f);
            CreateOrUpdateMat($"{dir}/CeilingMaterial.mat",  new Color(0.180f, 0.192f, 0.212f), 0.10f);
            CreateOrUpdateMat($"{dir}/GridLineMaterial.mat", new Color(0.180f, 0.350f, 0.550f), 0.60f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SmartexSceneBuilder] Factory materials created in Assets/Materials/");
            EditorUtility.DisplayDialog("Done",
                "4 factory materials created in Assets/Materials/.\n\nNow run Build Scene (Ctrl+Shift+B).", "OK");
        }

        static void CreateOrUpdateMat(string path, Color color, float smoothness)
        {
            // Project uses Built-in Render Pipeline (m_CustomRenderPipeline: {fileID: 0}).
            // Use Standard shader — it's what the FBX materials use and what renders correctly.
            // _Color is the base colour property; _SpecColor controls specularity.
            var shader = Shader.Find("Standard");
            if (shader == null) { Debug.LogError("[SmartexSceneBuilder] Standard shader not found."); return; }

            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var mat = new Material(shader);
            mat.SetColor("_Color",    color);
            mat.SetFloat("_Glossiness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }

        // ── Scene build ──────────────────────────────────────────────────────
        [MenuItem("Smartex/Setup/Build Scene %#b")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog("Rebuild SmartexVR Scene",
                "This will delete existing Smartex GameObjects and rebuild the full scene.\nContinue?",
                "Yes, rebuild", "Cancel")) return;

            SmartexTagsAutoCreate.EnsureTags();

            DestroyIfExists("SmartexManager");
            DestroyIfExists("FactoryRoot");
            DestroyIfExists("SmartexCanvas");
            DestroyIfExists("SmartexEventSystem");

            // SmartexManager
            var manager = new GameObject("SmartexManager");
            manager.AddComponent<SceneBootstrap>();
            manager.AddComponent<DataManager>();
            manager.AddComponent<InfluxDBClient>();

            // FactoryRoot
            var factoryRoot = new GameObject("FactoryRoot");
            var fb = factoryRoot.AddComponent<FactoryBuilder>();

            // Auto-assign FBX model
            var loomModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/SmartTex_Loom_V2.fbx");
            if (loomModel != null) { fb.machinePrefab = loomModel; Debug.Log("[SmartexSceneBuilder] Assigned SmartTex_Loom_V2.fbx as machine prefab."); }
            else Debug.LogWarning("[SmartexSceneBuilder] SmartTex_Loom_V2.fbx not found at Assets/Models/ — using placeholder boxes.");

            // Auto-assign materials created by "Smartex > Setup > Create Factory Materials".
            // If they don't exist yet, warn and leave null (Unity default white).
            fb.floorMaterial    = LoadMatOrNull("FloorMaterial");
            fb.wallMaterial     = LoadMatOrNull("WallMaterial");
            fb.ceilingMaterial  = LoadMatOrNull("CeilingMaterial");
            fb.gridLineMaterial = LoadMatOrNull("GridLineMaterial");
            if (fb.floorMaterial == null)
                Debug.LogWarning("[SmartexSceneBuilder] FloorMaterial not found — run Smartex > Setup > Create Factory Materials first.");

            // Build the entire factory hierarchy NOW (edit time) so every machine,
            // wall, floor tile, light, and aura is a real scene object visible in
            // the Hierarchy and editable in the Inspector.
            fb.BuildFactory();
            // Disable runtime rebuild so Play mode preserves the authored hierarchy
            // (and any manual tweaks you do to individual machines).
            fb.buildOnStart = false;

            // Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>(); camGO.AddComponent<AudioListener>();
            }
            var oldCC = cam.GetComponent<CameraController>();
            if (oldCC != null) Object.DestroyImmediate(oldCC);
            var cc = cam.gameObject.AddComponent<CameraController>();
            cc.factoryCenter = factoryRoot.transform;
            var oldMCH = cam.GetComponent<MachineClickHandler>();
            if (oldMCH != null) Object.DestroyImmediate(oldMCH);
            cam.gameObject.AddComponent<MachineClickHandler>();
            cam.transform.position = new Vector3(0f, 35f, -55f);
            cam.transform.LookAt(Vector3.zero);

            // Canvas
            var canvasGO = new GameObject("SmartexCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(es.gameObject);
            var esGO = new GameObject("SmartexEventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // HUD + Panel
            BuildFactoryHUD(canvasGO.transform);
            BuildMachineDetailPanel(canvasGO.transform);

            // Wire bootstrap
            var sb = manager.GetComponent<SceneBootstrap>();
            sb.dataManager    = manager.GetComponent<DataManager>();
            sb.factoryBuilder = fb;

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SmartexSceneBuilder] Scene built and saved.");
            EditorUtility.DisplayDialog("Done", "Scene built successfully!\n\nPress Play to run the factory.", "OK");
        }

        static FactoryHUD BuildFactoryHUD(Transform canvasParent)
        {
            var root = MakePanel("FactoryHUD", canvasParent, new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(8f,-8f), new Vector2(340f,280f));
            root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.82f);
            var hud = root.gameObject.AddComponent<FactoryHUD>();
            hud.factoryNameText      = MakeLabel(root.transform, "TitleText",   new Vector2(0f,260f), 13f, Color.cyan,  "TNG-01  .  SmartTex Digital Twin");
            hud.connectionStatusText = MakeLabel(root.transform, "ConnText",    new Vector2(0f,238f), 12f, Color.green, "LIVE");
            hud.totalPowerText       = MakeLabel(root.transform, "PowerText",   new Vector2(0f,218f), 12f, Color.white, "-- kW");
            hud.co2TodayText         = MakeLabel(root.transform, "CO2Text",     new Vector2(0f,198f), 12f, Color.white, "-- kg CO2");
            hud.cbamExposureText     = MakeLabel(root.transform, "CBAMText",    new Vector2(0f,178f), 12f, Color.white, "-- MAD");
            hud.machineStatusText    = MakeLabel(root.transform, "StatusText",  new Vector2(0f,155f), 11f, Color.white, "Loading...");
            hud.alertCountText       = MakeLabel(root.transform, "AlertText",   new Vector2(0f,133f), 12f, Color.green, "ALL CLEAR");
            hud.lastUpdateText       = MakeLabel(root.transform, "UpdateText",  new Vector2(0f,112f), 10f, new Color(0.6f,0.6f,0.6f), "Updated --:--:-- UTC");
            hud.alertWarningIcon     = MakeDot(root.transform, "WarnDot",  new Vector2(310f,133f), new Color(1f,0.6f,0f));
            hud.alertCriticalIcon    = MakeDot(root.transform, "CritDot",  new Vector2(328f,133f), Color.red);
            hud.alertWarningIcon.SetActive(false); hud.alertCriticalIcon.SetActive(false);
            return hud;
        }

        static MachineDetailPanel BuildMachineDetailPanel(Transform canvasParent)
        {
            var root = MakePanel("MachineDetailPanel", canvasParent, new Vector2(1f,0.5f), new Vector2(1f,0.5f), new Vector2(-8f,0f), new Vector2(360f,700f));
            root.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.13f, 0.93f);
            var mdp = root.gameObject.AddComponent<MachineDetailPanel>();
            mdp.panelRoot = root.gameObject.GetComponent<RectTransform>();
            float y = 330f, dy = 28f;
            mdp.machineNameText  = MakeLabel(root.transform, "MachineName", new Vector2(0f,y),     16f, Color.white,  "Machine");  y-=26f;
            mdp.statusBadge      = MakeLabel(root.transform, "StatusBadge", new Vector2(0f,y),     12f, Color.green,  "HEALTHY");  y-=22f;
            mdp.lastSeenText     = MakeLabel(root.transform, "LastSeen",    new Vector2(0f,y),     10f, new Color(0.6f,0.6f,0.6f), "Last seen: --");
            y-=30f;
            mdp.powerText        = MakeLabel(root.transform, "PowerVal",    new Vector2(-40f,y),   12f, Color.white, "-- W");     y-=dy;
            mdp.vibText          = MakeLabel(root.transform, "VibVal",      new Vector2(-40f,y),   12f, Color.white, "-- mm/s");  y-=dy;
            mdp.dyeTempText      = MakeLabel(root.transform, "DyeTemp",     new Vector2(-40f,y),   12f, Color.white, "-- C");     y-=dy;
            mdp.fabricTempText   = MakeLabel(root.transform, "FabricTemp",  new Vector2(-40f,y),   12f, Color.white, "-- C");     y-=dy;
            mdp.tensionText      = MakeLabel(root.transform, "Tension",     new Vector2(-40f,y),   12f, Color.white, "-- g");     y-=dy;
            mdp.rssiText         = MakeLabel(root.transform, "RSSI",        new Vector2(-40f,y),   12f, Color.white, "-- dBm");   y-=34f;
            mdp.healthText       = MakeLabel(root.transform, "HealthText",  new Vector2(-40f,y),   12f, Color.white, "Health: --%"); y-=22f;
            mdp.healthSlider     = MakeSlider(root.transform, "HealthSlider", new Vector2(0f,y), new Vector2(300f,18f)); y-=34f;
            mdp.cbamAnnualText   = MakeLabel(root.transform, "CBAM_Annual", new Vector2(-40f,y),   11f, new Color(1f,0.85f,0.3f), "-- MAD/yr"); y-=20f;
            mdp.cbamShareText    = MakeLabel(root.transform, "CBAM_Share",  new Vector2(-40f,y),   10f, new Color(0.7f,0.7f,0.7f), "-- % of factory"); y-=22f;
            mdp.cbamBarSlider    = MakeSlider(root.transform, "CBAMBar",    new Vector2(0f,y), new Vector2(300f,14f)); y-=34f;
            MakeLabel(root.transform, "WhatIfTitle", new Vector2(-40f,y), 12f, new Color(0.5f,0.9f,1f), "What-if Maintenance"); y-=22f;
            mdp.wearLabel        = MakeLabel(root.transform, "WearLabel",   new Vector2(-40f,y),   11f, Color.white, "Bearing wear: 0%"); y-=20f;
            mdp.wearSlider       = MakeSlider(root.transform, "WearSlider", new Vector2(0f,y), new Vector2(300f,18f)); y-=28f;
            mdp.whatIfPowerText  = MakeLabel(root.transform, "WhatIfPwr",   new Vector2(-40f,y),   11f, Color.white, "-- kWh/garment"); y-=dy;
            mdp.whatIfCBAMText   = MakeLabel(root.transform, "WhatIfCBAM",  new Vector2(-40f,y),   11f, Color.white, "-- MAD/yr");      y-=dy;
            mdp.whatIfSavingText = MakeLabel(root.transform, "WhatIfSave",  new Vector2(-40f,y),   11f, Color.green, "Calculating...");
            return mdp;
        }

        static RectTransform MakePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            return rt;
        }

        static TextMeshProUGUI MakeLabel(Transform parent, string name, Vector2 anchoredPos, float fontSize, Color color, string defaultText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f,1f); rt.anchorMax = new Vector2(1f,1f); rt.pivot = new Vector2(0f,1f);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = new Vector2(-16f, 22f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultText; tmp.fontSize = fontSize; tmp.color = color;
            return tmp;
        }

        static Slider MakeSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var sliderGO = new GameObject(name, typeof(RectTransform));
            sliderGO.transform.SetParent(parent, false);
            var srt = sliderGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f,1f); srt.anchorMax = new Vector2(1f,1f); srt.pivot = new Vector2(0.5f,1f);
            srt.anchoredPosition = anchoredPos; srt.sizeDelta = size;
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.2f,0.2f,0.3f,0.8f);
            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var faRT = fillAreaGO.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.offsetMin = faRT.offsetMax = Vector2.zero;
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f,1f); fillRT.offsetMin = fillRT.offsetMax = Vector2.zero; fillRT.sizeDelta = Vector2.zero;
            fillGO.GetComponent<Image>().color = new Color(0.2f,0.7f,0.4f,1f);
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRT; slider.targetGraphic = bgGO.GetComponent<Image>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        static GameObject MakeDot(Transform parent, string name, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f,1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(14f,14f);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Best-effort material loader — searches the whole project for a material
        /// whose asset name contains `hint` (case-insensitive). Returns null if none found.
        /// This lets the builder auto-assign sensible defaults without hard-coding paths,
        /// while still letting you override any slot manually in the Inspector afterwards.
        /// </summary>
        static Material LoadMatOrNull(string hint)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Material {hint}");
            if (guids == null || guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        /// <summary>
        /// Creates (or updates) a material saved under Assets/Materials/SmartexAuto/.
        /// We derive the shader by cloning a temp primitive's default material — this
        /// always yields the correct pipeline material (URP Lit in URP projects) without
        /// calling Shader.Find(), which returns null in editor scripts and causes the
        /// Standard-shader fallback to render magenta under URP.
        /// </summary>
        static Material MakeMat(string name, Color color)
        {
            const string dir = "Assets/Materials/SmartexAuto";
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Materials", "SmartexAuto");

            string path = $"{dir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor("_BaseColor", color);
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Spin up a temp primitive to grab the project's default material.
            // This is always the correct pipeline material (URP Lit here) with no
            // hard-coded shader name required.
            var temp    = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var srcMat  = temp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(temp);

            var mat = new Material(srcMat) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
