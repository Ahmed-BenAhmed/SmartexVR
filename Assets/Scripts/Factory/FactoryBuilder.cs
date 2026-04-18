using UnityEngine;
using Smartex.Core;
using Smartex.Machines;
using Smartex.Visualization;

namespace Smartex.Factory
{
    public class FactoryBuilder : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject machinePrefab;

        [Header("Layout override (optional)")]
        public FactoryLayout layoutOverride;

        [Header("Materials")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material ceilingMaterial;
        public Material gridLineMaterial;

        [Header("Factory dimensions")]
        public float floorWidth  = 40f;
        public float floorDepth  = 30f;
        public float wallHeight  = 7f;

        [Header("Runtime behavior")]
        [Tooltip("If true, BuildFactory() runs on Start(). Disable when the scene was prebuilt by the Editor builder, so you keep the authored hierarchy.")]
        public bool buildOnStart = true;

        private static readonly string[] MachineIDs = {
            "ESP32_TEX_001","ESP32_TEX_002","ESP32_TEX_003","ESP32_TEX_004",
            "ESP32_TEX_005","ESP32_TEX_006","ESP32_TEX_007","ESP32_TEX_008"
        };

        void Start()
        {
            if (buildOnStart) BuildFactory();
            else Debug.Log("[FactoryBuilder] buildOnStart disabled — using authored hierarchy from the scene.");
        }

        [ContextMenu("Rebuild Factory")]
        public void BuildFactory()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.CompareTag("Generated")) DestroyImmediate(child.gameObject);
            }
            BuildFloor();
            BuildWalls();
            BuildLighting();
            SpawnMachines();
            Debug.Log("[FactoryBuilder] Factory built.");
        }

        void BuildFloor()
        {
            var floor = CreateBox("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(floorWidth, 0.1f, floorDepth), floorMaterial);
            float step = 4f;
            for (float x = -floorWidth/2f; x <= floorWidth/2f; x += step)
                CreateBox($"GridLine_X_{x}", new Vector3(x, 0.001f, 0f), new Vector3(0.03f, 0.001f, floorDepth), gridLineMaterial).transform.SetParent(floor.transform);
            for (float z = -floorDepth/2f; z <= floorDepth/2f; z += step)
                CreateBox($"GridLine_Z_{z}", new Vector3(0f, 0.001f, z), new Vector3(floorWidth, 0.001f, 0.03f), gridLineMaterial).transform.SetParent(floor.transform);
        }

        void BuildWalls()
        {
            float hw = floorWidth/2f, hd = floorDepth/2f, hy = wallHeight/2f;
            CreateBox("Wall_N",  new Vector3(0,  hy,  hd), new Vector3(floorWidth, wallHeight, 0.3f), wallMaterial);
            CreateBox("Wall_S",  new Vector3(0,  hy, -hd), new Vector3(floorWidth, wallHeight, 0.3f), wallMaterial);
            CreateBox("Wall_E",  new Vector3(hw, hy, 0),   new Vector3(0.3f, wallHeight, floorDepth), wallMaterial);
            CreateBox("Wall_W",  new Vector3(-hw,hy, 0),   new Vector3(0.3f, wallHeight, floorDepth), wallMaterial);
            CreateBox("Ceiling", new Vector3(0, wallHeight, 0), new Vector3(floorWidth, 0.2f, floorDepth), ceilingMaterial);
        }

        void BuildLighting()
        {
            var sun = new GameObject("Sun"); sun.tag = "Generated"; sun.transform.SetParent(transform);
            var dir = sun.AddComponent<Light>(); dir.type = LightType.Directional; dir.intensity = 0.7f;
            dir.color = new Color(0.95f, 0.92f, 0.85f); sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            int cols = 4; float xStep = floorWidth / (cols + 1f);
            for (int c = 0; c < cols; c++)
            {
                var lg = new GameObject($"StripLight_{c}"); lg.tag = "Generated"; lg.transform.SetParent(transform);
                lg.transform.position = new Vector3(-floorWidth/2f + xStep * (c + 1f), wallHeight - 0.5f, 0f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 14f; l.intensity = 1.4f;
                l.color = new Color(0.9f, 0.95f, 1.0f);
            }
        }

        void SpawnMachines()
        {
            if (machinePrefab == null) { Debug.LogWarning("[FactoryBuilder] machinePrefab not assigned. Creating placeholder boxes."); SpawnPlaceholders(); return; }
            var cfg = SmartexConfig.Instance;
            int cols = 4, rows = Mathf.CeilToInt(MachineIDs.Length / (float)cols);
            float xOff = -(cols - 1) * cfg.machineSpacingX / 2f, zOff = -(rows - 1) * cfg.machineSpacingZ / 2f;
            for (int i = 0; i < MachineIDs.Length; i++)
            {
                int row = i / cols, col = i % cols;
                Vector3 pos = layoutOverride != null && i < layoutOverride.positions.Length
                    ? layoutOverride.positions[i]
                    : new Vector3(xOff + col * cfg.machineSpacingX, 0f, zOff + row * cfg.machineSpacingZ);
                var go = Instantiate(machinePrefab, pos, Quaternion.identity, transform);
                go.name = $"Machine_{MachineIDs[i]}"; go.tag = "Generated";

                // The FBX ships with its own Camera + AudioListener (and possibly Lights).
                // With 8 copies instantiated, one of them out-depths the real Main Camera
                // and the Game view renders from a machine's viewpoint. Strip them.
                // URP adds UniversalAdditionalCameraData / UniversalAdditionalLightData as
                // required components — those must be destroyed first or Unity refuses to
                // remove Camera / Light.
                foreach (var c in go.GetComponentsInChildren<UnityEngine.Camera>(true))
                {
                    var urpData = c.GetComponent("UniversalAdditionalCameraData");
                    if (urpData != null) SafeDestroy(urpData);
                    SafeDestroy(c);
                }
                foreach (var a in go.GetComponentsInChildren<AudioListener>(true)) SafeDestroy(a);
                // Lights inside the FBX would double-count with our rig — remove them.
                foreach (var l in go.GetComponentsInChildren<Light>(true))
                {
                    var urpData = l.GetComponent("UniversalAdditionalLightData");
                    if (urpData != null) SafeDestroy(urpData);
                    SafeDestroy(l);
                }

                // Auto-fit: scale the FBX so its bounding box max-dimension is ~2m,
                // and lift it so the bottom sits on the floor (y=0).
                // Important: Renderer.bounds uses the mesh's *cached* local bounds, which for
                // many FBX exports is much smaller than the real vertex extents. We force
                // RecalculateBounds on each shared mesh so we measure the true geometry.
                const float targetMaxDim = 2.0f;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null) mf.sharedMesh.RecalculateBounds();
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if (smr.sharedMesh != null) smr.sharedMesh.RecalculateBounds();

                // Compute true world bounds by iterating mesh vertices through their transforms.
                Bounds trueBounds = ComputeTrueBounds(go);
                float maxDim = Mathf.Max(trueBounds.size.x, Mathf.Max(trueBounds.size.y, trueBounds.size.z));
                Debug.Log($"[FactoryBuilder] {MachineIDs[i]} true bounds size={trueBounds.size} maxDim={maxDim:F2}");
                if (maxDim > 0.001f)
                {
                    float scale = targetMaxDim / maxDim;
                    go.transform.localScale *= scale;
                    Debug.Log($"[FactoryBuilder] {MachineIDs[i]} scaled by {scale:F4} → localScale={go.transform.localScale}");
                }
                // Recompute and lift so the bottom of the mesh sits on the floor (y=0).
                Bounds finalBounds = ComputeTrueBounds(go);
                go.transform.position += new Vector3(0f, -finalBounds.min.y, 0f);

                // Ensure a collider exists on root for raycasting
                if (go.GetComponent<Collider>() == null)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    var rends2 = go.GetComponentsInChildren<Renderer>();
                    if (rends2.Length > 0)
                    {
                        var bounds = rends2[0].bounds;
                        foreach (var r in rends2) bounds.Encapsulate(r.bounds);
                        // World-space size → local size via lossyScale (safer than InverseTransformVector)
                        var ls = go.transform.lossyScale;
                        bc.center = go.transform.InverseTransformPoint(bounds.center);
                        bc.size   = new Vector3(
                            bounds.size.x / Mathf.Max(Mathf.Abs(ls.x), 0.0001f),
                            bounds.size.y / Mathf.Max(Mathf.Abs(ls.y), 0.0001f),
                            bounds.size.z / Mathf.Max(Mathf.Abs(ls.z), 0.0001f));
                    }
                }

                // Wire up MachineController
                var mc = go.GetComponent<MachineController>() ?? go.AddComponent<MachineController>();
                mc.deviceId      = MachineIDs[i];
                mc.bodyRenderer  = go.GetComponentInChildren<Renderer>();

                // HealthAura disc and EnergyBar pole are intentionally kept as children of
                // FactoryRoot (this transform, scale 1,1,1) rather than parented to the FBX
                // machine. The FBX is auto-scaled to ~0.1 lossyScale, so any child's
                // localScale operations (e.g. bar height lerp) would be 10x too small in
                // world-space. Since machines are static, they don't need to follow via the
                // transform hierarchy — the world positions are set correctly here once.
                Vector3 auraWorldPos = new Vector3(go.transform.position.x, 0.02f, go.transform.position.z);
                var auraGO = CreateBox($"Aura_{MachineIDs[i]}", auraWorldPos, new Vector3(2.4f, 0.05f, 2.4f), null);
                // already parented to 'transform' (FactoryRoot) by CreateBox — leave it there
                SafeDestroy(auraGO.GetComponent<Collider>());
                var ha = auraGO.AddComponent<HealthAura>(); ha.auraRenderer = auraGO.GetComponent<Renderer>(); mc.healthAura = ha;

                // Bar: 3 m tall column to the right of the machine, clearly above the 2 m FBX.
                // Pivot is at center so we start at Y=1.5 (half of 3 m) so the bottom sits on the floor.
                Vector3 barWorldPos = new Vector3(go.transform.position.x + 1.4f, 1.5f, go.transform.position.z);
                var barGO = CreateBox($"EnergyBar_{MachineIDs[i]}", barWorldPos, new Vector3(0.25f, 3.0f, 0.25f), null);
                SafeDestroy(barGO.GetComponent<Collider>());
                var eb = barGO.AddComponent<EnergyBar>();
                eb.barFill       = barGO.transform;
                eb.barRenderer   = barGO.GetComponent<Renderer>();
                eb.maxHeight     = 3.0f;   // match the initial scale.y
                eb.minHeight     = 0.1f;
                mc.energyBar     = eb;
            }
        }

        void SpawnPlaceholders()
        {
            var cfg = SmartexConfig.Instance;
            int cols = 4, rows = 2;
            float xOff = -(cols - 1) * cfg.machineSpacingX / 2f, zOff = -(rows - 1) * cfg.machineSpacingZ / 2f;
            for (int i = 0; i < MachineIDs.Length; i++)
            {
                int row = i / cols, col = i % cols;
                Vector3 pos = new Vector3(xOff + col * cfg.machineSpacingX, 0f, zOff + row * cfg.machineSpacingZ);
                var body = CreateBox($"Machine_{MachineIDs[i]}", pos, new Vector3(1.6f, 1.8f, 1.2f), null);
                body.tag = "Generated"; body.transform.SetParent(transform);
                var mc = body.AddComponent<MachineController>();
                mc.deviceId = MachineIDs[i]; mc.bodyRenderer = body.GetComponent<Renderer>();
                var auraGO = CreateBox("Aura", new Vector3(pos.x, 0.02f, pos.z), new Vector3(2.4f, 0.05f, 2.4f), null);
                SafeDestroy(auraGO.GetComponent<Collider>());
                var ha = auraGO.AddComponent<HealthAura>(); ha.auraRenderer = auraGO.GetComponent<Renderer>(); mc.healthAura = ha;
                var barGO = CreateBox("EnergyBar", new Vector3(pos.x, 0f, pos.z - 0.7f), new Vector3(0.15f, 1.0f, 0.15f), null);
                SafeDestroy(barGO.GetComponent<Collider>());
                var eb = barGO.AddComponent<EnergyBar>(); eb.barFill = barGO.transform; eb.barRenderer = barGO.GetComponent<Renderer>(); mc.energyBar = eb;
            }
        }

        /// <summary>
        /// Computes a world-space bounding box by walking every vertex of every mesh under `root`.
        /// Unity's Renderer.bounds uses cached mesh bounds which FBX imports often under-report,
        /// leading to auto-scaling being wildly off. This is slower but correct.
        /// </summary>
        static Bounds ComputeTrueBounds(GameObject root)
        {
            bool first = true;
            Bounds b = new Bounds(root.transform.position, Vector3.zero);

            // Prefer vertex-level scan (accurate), but only for readable meshes.
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var verts = mesh.vertices;
                var t = mf.transform;
                for (int vi = 0; vi < verts.Length; vi++)
                {
                    Vector3 wp = t.TransformPoint(verts[vi]);
                    if (first) { b = new Bounds(wp, Vector3.zero); first = false; }
                    else b.Encapsulate(wp);
                }
            }
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var verts = mesh.vertices;
                var t = smr.transform;
                for (int vi = 0; vi < verts.Length; vi++)
                {
                    Vector3 wp = t.TransformPoint(verts[vi]);
                    if (first) { b = new Bounds(wp, Vector3.zero); first = false; }
                    else b.Encapsulate(wp);
                }
            }

            // Fallback: if no readable meshes, use Renderer.bounds (cached AABB).
            // Less accurate but always available.
            if (first)
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; }
                    else b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        /// <summary>
        /// Destroys an Object safely regardless of play/edit mode. In the editor during
        /// scene authoring, Destroy() is illegal — we must use DestroyImmediate().
        /// </summary>
        static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else                       Object.DestroyImmediate(obj);
        }

        GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.tag = "Generated"; go.transform.SetParent(transform);
            go.transform.position = position; go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
