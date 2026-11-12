using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Baut die komplette Zone prozedural (Terrain, Hub, POIs, Vegetation, Licht, Post-Processing).</summary>
    public static partial class ZoneBuilder
    {
        public const string ZonePath = Root + "/Scenes/Zones/PT_Zone_Sonnfurt.unity";
        public const string LegacyZonePath = Root + "/Scenes/PT_Zone.unity";
        const string MeshDir = ProcDir + "/Meshes";
        const string EnvPrefabDir = PrefabDir + "/Environment";

        static Transform _env;
        static ZoneAnchors _anchors;
        static System.Random _rnd;
        static readonly List<string> _placeholderLog = new List<string>();

        public static float H(float x, float z) => ZoneLayout.Height(x, z);
        public static Vector3 G(float x, float z, float yOff = 0f) => new Vector3(x, H(x, z) + yOff, z);
        public static Vector3 G(Vector2 p, float yOff = 0f) => G(p.x, p.y, yOff);

        /// <summary>Lauf 4: Startzone Sonnfurt als reine Zonen-Szene (Systeme/Spieler/Kamera liegen in PT_Core).</summary>
        public static void BuildZone()
        {
            _rnd = new System.Random(4242);
            _placeholderLog.Clear();
            _prefabCache.Clear();
            EnsureFolder(MeshDir); EnsureFolder(EnvPrefabDir);
            ConfigureRenderPipeline();
            var w1Zone = WorldBuilder.Zone("sonnfurt");
            if (w1Zone != null && WorldW1Builder.NeedsPlan(w1Zone)) { WorldW1Builder.PlanZone(w1Zone); AssetDatabase.SaveAssetIfDirty(w1Zone); }   // Welt-Lauf W1

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneBuilder.CreateSun();
            SetupEnvironmentLighting();
            CreatePostProcessing();

            _env = new GameObject("Environment").transform;
            var anchorsGo = new GameObject("PT_ZoneAnchors");
            _anchors = anchorsGo.AddComponent<ZoneAnchors>();

            BuildTerrain();
            BuildWater();
            BuildBoundary();
            BuildHub();
            BuildOutpost();
            BuildCamp();
            BuildRuins();
            ScatterVegetation();
            BuildWildSpawns();
            BakeNavMesh();
            MapBaker.Bake(_env);

            var rootGo = new GameObject("PT_ZoneRoot");
            var root = rootGo.AddComponent<ZoneRoot>();
            VfxKitBuilder.Ambient(rootGo.transform, WeatherKind.Pollen, new Vector2(0.8f, 0.5f), 0.22f, true, false, true, 7);   // Lauf 5: Pollen, Blätter, Schmetterlinge, Vögel
            root.Definition = WorldBuilder.Zone("sonnfurt");
            root.Waypoint = _anchors.Waypoint;
            if (root.Definition != null)
            {
                root.Definition.DefaultSpawn = _anchors.Waypoint.position + _anchors.Waypoint.forward * 3f;
                root.Definition.Waypoints = new List<ZoneWaypointInfo> { new ZoneWaypointInfo { Id = "wp_sonnfurt", Name = "Wegmarke Sonnfurt", Position = _anchors.Waypoint.position } };
                // Fix-Lauf F1: alle Szenen-Wegmarken der Startzone in die Definition (Besiegt-Liste, Kontinentkarte)
                foreach (var w in ZoneContent.Waypoints) if (w.Id != "wp_sonnfurt") root.Definition.Waypoints.Add(new ZoneWaypointInfo { Id = w.Id, Name = w.Name, Position = G(w.Pos) });
                EditorUtility.SetDirty(root.Definition);
            }
            if (root.Definition != null)
            {
                foreach (var p in root.Definition.Portals) BiomeZoneBuilder.BuildPortal(p, _env, root.Definition);
                ArrivalWaypointBuilder.Ensure(root.Definition, rootGo.transform, ZoneContentBuilder.Dir, 120);   // Fix-Lauf F1
            }
            root.MapTexture = MapBaker.Bake(_env);

            PopulateGameplay(root);
            if (root.Definition != null)   // Lauf 6: Zonenabschluss-Ziele der Startzone (bisher leer → Kontinentkarte 0 %): alle Definitionen direkt in ZoneContentBuilder.Dir außer Einsichten
            {
                root.Definition.CompletionIds = new List<string>();
                foreach (var g in AssetDatabase.FindAssets("t:ZoneObjectDefinition", new[] { ZoneContentBuilder.Dir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(g); if (System.IO.Path.GetDirectoryName(path).Replace('\\', '/') != ZoneContentBuilder.Dir) continue;
                    var d = AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(path);
                    if (d != null && d.Kind != MapMarkerKind.Insight) root.Definition.CompletionIds.Add(d.Id);
                }
                EditorUtility.SetDirty(root.Definition);
            }
            if (root.Definition != null) WorldStoryBuilder.Populate(root.Definition, rootGo.transform, (x, zz) => G(x, zz).y);   // Lauf 6: Späher, Herzen, Bündel
            if (root.Definition != null && root.Definition.WorldPlan != null && !root.Definition.WorldPlan.IsEmpty)   // Welt-Lauf W1: bauen, danach NavMesh neu (Bauwerke als Hindernis)
            {
                WorldW1Builder.Populate(root.Definition, rootGo.transform, (x, zz) => G(x, zz).y);
                var oldNav = GameObject.Find("PT_NavMesh"); if (oldNav != null) Object.DestroyImmediate(oldNav);
                BakeNavMesh();
            }

            foreach (var r in _env.GetComponentsInChildren<Transform>(true))
                if (r.GetComponent<Light>() == null && r.GetComponent<WorldBanner>() == null && r.GetComponentInParent<WorldBanner>() == null && r.GetComponent<Bobber>() == null && r.GetComponentInParent<Bobber>() == null)
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);

            EnsureFolder(Root + "/Scenes/Zones");
            EditorSceneManager.SaveScene(scene, ZonePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyZonePath) != null) AssetDatabase.DeleteAsset(LegacyZonePath);   // Lauf-3-Szene → migriert
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Zone gebaut: " + ZonePath);
        }

        static partial void PopulateGameplayImpl(ZoneRoot root, ZoneAnchors anchors);
        static void PopulateGameplay(ZoneRoot root) => PopulateGameplayImpl(root, _anchors);

        // ---------- Rendering ----------
        static void ConfigureRenderPipeline()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(DataDir + "/Rendering/PT_URP.asset");
            if (rp != null)
            {
                rp.shadowDistance = 110f;
                rp.shadowCascadeCount = 2;
                rp.supportsHDR = true;
                rp.msaaSampleCount = 4;
                EditorUtility.SetDirty(rp);
            }
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(DataDir + "/Rendering/PT_Renderer.asset");
            if (rd != null) { rd.renderingMode = RenderingMode.Forward; EditorUtility.SetDirty(rd); }
        }

        static void SetupEnvironmentLighting()
        {
            var skyShader = Shader.Find(Visuals.PTShaders.Sky);   // Grafik-Lauf G2: Himmel 2.0
            string skyPath = MatDir + "/M_Skybox.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(skyShader); AssetDatabase.CreateAsset(sky, skyPath); }
            sky.shader = skyShader;
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.75f, 0.65f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.40f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.80f, 0.75f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 280f;
        }

        static void CreatePostProcessing()
        {
            string path = DataDir + "/Rendering/PT_PostProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            foreach (var c in new List<VolumeComponent>(profile.components)) { profile.Remove(c.GetType()); Object.DestroyImmediate(c, true); }

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.45f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.6f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(18f); ca.contrast.Override(8f); ca.postExposure.Override(0.15f);
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f); vig.smoothness.Override(0.45f); vig.color.Override(new Color(0.12f, 0.08f, 0.18f));
            var st = profile.Add<SplitToning>(true);
            st.shadows.Override(new Color(0.35f, 0.45f, 0.75f)); st.highlights.Override(new Color(1f, 0.82f, 0.6f)); st.balance.Override(-10f);
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(8f);
            foreach (var c in profile.components) { AssetDatabase.AddObjectToAsset(c, profile); c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var go = new GameObject("PT_PostProcessing");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 10; vol.sharedProfile = profile;
        }

        // ---------- Terrain ----------
        static void BuildTerrain()
        {
            var mesh = ZoneLayout.BuildTerrainMesh(2f);
            string mp = MeshDir + "/PT_ZoneTerrain.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, mp);
            var go = new GameObject("Terrain_Grenzmark");
            go.transform.SetParent(_env, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat("Terrain", Color.white, 0.4f, null, 0.12f);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void BuildWater()
        {
            var w = Part(_env, PrimitiveType.Cylinder, new Vector3(ZoneLayout.Lake.x, ZoneLayout.WaterLevel, ZoneLayout.Lake.y), new Vector3(ZoneLayout.LakeRadius * 2.3f, 0.02f, ZoneLayout.LakeRadius * 2.3f),
                VfxKitBuilder.WaterMat("sonnfurt", new Color(0.4f, 0.8f, 0.9f, 0.7f), new Color(0.1f, 0.35f, 0.55f, 0.95f), Color.black), "Lake_Spiegelsee");
        }

        static void BuildBoundary()
        {
            var root = Child(_env, "Boundary", Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject("Wall" + i);
                g.transform.SetParent(root.transform, false);
                var bc = g.AddComponent<BoxCollider>();
                bool x = i < 2; float s = (i % 2 == 0) ? 1 : -1;
                g.transform.position = x ? new Vector3(97f * s, 20f, 0) : new Vector3(0, 20f, 97f * s);
                bc.size = x ? new Vector3(1f, 80f, 200f) : new Vector3(200f, 80f, 1f);
            }
        }

        // ---------- Prefabs (Tier 3: prozedural) ----------
        static readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

        static GameObject MeshPrefab(string name, Mesh mesh, Material mat, bool collider, bool convex, string prompt)
        {
            if (_prefabCache.TryGetValue(name, out var cached) && cached != null) return cached;
            mesh.name = name;
            string mp = MeshDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, mp);
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                if (convex) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.convex = true; }
                else { var cap = go.AddComponent<CapsuleCollider>(); cap.radius = 0.35f; cap.height = 3f; cap.center = new Vector3(0, 1.5f, 0); }
            }
            AddPlaceholder(go, name, prompt);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, EnvPrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            _prefabCache[name] = prefab;
            LogPlaceholder(name, "Prefabs/Environment", "MeshKit (C#)", prompt);
            return prefab;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            mesh.name = name;
            string mp = MeshDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); return existing; }
            AssetDatabase.CreateAsset(mesh, mp);
            return mesh;
        }

        static void LogPlaceholder(string name, string where, string how, string prompt)
        {
            _placeholderLog.Add("| " + name + " | " + where + " | " + how + " | " + prompt + " |");
        }

        public static IEnumerable<string> PlaceholderLog => _placeholderLog;

        static GameObject Place(GameObject prefab, Vector3 pos, float yaw, float scale, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static Material VC => Mat("VertexColor", Color.white, 0.3f, null, 0.35f);

        static float R01() => (float)_rnd.NextDouble();

        // ---------- Dorfbewohner (Lauf 5 / M21) ----------
        static readonly (string name, Color robe, (float x, float z, float yaw, string act, float dur)[] stops)[] VillagerPlan =
        {
            ("Bauer Ansgar", new Color(0.45f, 0.6f, 0.35f), new[] { (-5.5f, -2.5f, 90f, "Work", 9f), (2.5f, -6.5f, 200f, "Talk", 7f), (-2.5f, 4.5f, 0f, "Idle2", 5f) }),
            ("Magd Hilde", new Color(0.6f, 0.35f, 0.3f), new[] { (3.5f, -6f, 20f, "Talk", 7f), (9f, -1f, 270f, "Interact", 4f) }),
            ("Fischer Jorn", new Color(0.4f, 0.45f, 0.65f), new[] { (-9f, 3f, 120f, "Sit", 12f), (-4f, 7.5f, 300f, "Work", 8f), (0f, 9.5f, 180f, "Idle2", 5f) }),
            ("Kind Ilse", new Color(0.85f, 0.65f, 0.25f), new[] { (4f, 3f, 45f, "Cheer", 4f), (-1f, -9f, 160f, "Idle2", 4f) }),
            ("Händler Bodo", new Color(0.55f, 0.45f, 0.35f), new[] { (-7f, -5f, 60f, "Talk", 8f), (-3f, -7.5f, 0f, "Interact", 5f), (-8.5f, 0.5f, 90f, "Work", 7f) }),
            ("Wäscherin Grete", new Color(0.7f, 0.55f, 0.6f), new[] { (7.5f, 5.5f, 230f, "Work", 10f), (10f, -4f, 270f, "Sit", 9f) }),
        };

        static void PlaceVillagers(Transform hub)
        {
            int n = 0;
            foreach (var v in VillagerPlan)
            {
                var first = v.stops[0];
                var go = Child(hub, "Villager_" + n, G(ZoneLayout.Hub.Center + new Vector2(first.x, first.z)) - hub.position);
                var vis = CharacterRigs.Villager(go.transform, v.robe, 7100 + n * 17, "Sonnfurt_" + n, "", n % 3 == 1);
                var d = CharacterRigs.Drive(go, vis, Anim.AnimDriver.Role.Villager); d.LookAtPlayer = true;
                var r = go.AddComponent<Anim.VillagerRoutine>();
                r.Anim = vis.GetComponent<Anim.CharacterAnimator>();
                r.Stops = new Anim.VillagerRoutine.Stop[v.stops.Length];
                for (int i = 0; i < v.stops.Length; i++)
                {
                    var s0 = v.stops[i];
                    r.Stops[i] = new Anim.VillagerRoutine.Stop { Position = G(ZoneLayout.Hub.Center + new Vector2(s0.x, s0.z)), Yaw = s0.yaw, Activity = s0.act, Duration = s0.dur };
                }
                ZoneContentBuilder.Nameplate(go.transform, v.name, "Dorfbewohner", new Color(0.85f, 0.9f, 1f));
                n++;
            }
        }

        // ---------- Hub ----------
        static void BuildHub()
        {
            var hub = Child(_env, "POI_Hub_WeilerSonnfurt", G(ZoneLayout.Hub.Center));
            _anchors.HubCenter = hub.transform;
            var stone = Mat("Stone", Palette.Stone);
            var wood = Mat("Wood", Palette.Wood);
            var plaster = Mat("Plaster", new Color(0.95f, 0.88f, 0.72f));
            var roofMat = VC;

            // Wahrzeichen: Leuchtfeuerturm (Placeholder für Meshy Tier 1)
            var tower = Child(hub.transform, "Landmark_Leuchtfeuerturm", Vector3.zero);
            Part(tower.transform, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(6f, 0.4f, 6f), stone, "Base", null, true);
            Part(tower.transform, PrimitiveType.Cylinder, new Vector3(0, 5f, 0), new Vector3(3.6f, 5f, 3.6f), stone, "Shaft", null, true);
            Part(tower.transform, PrimitiveType.Cylinder, new Vector3(0, 10.2f, 0), new Vector3(4.6f, 0.25f, 4.6f), wood, "Balcony");
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Part(tower.transform, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 2.15f, 10.9f, Mathf.Sin(a) * 2.15f), new Vector3(0.18f, 1.2f, 0.18f), wood, "Post" + i);
            }
            var roof = new GameObject("Roof"); roof.transform.SetParent(tower.transform, false); roof.transform.localPosition = new Vector3(0, 12.6f, 0);
            var rk = new MeshKit(); rk.Cone(Vector3.zero, 10, 3.0f, 3.6f, Palette.Roof);
            var roofMesh = SaveMesh(rk.ToMesh("BeaconRoof"), "BeaconRoof");
            roof.AddComponent<MeshFilter>().sharedMesh = roofMesh; roof.AddComponent<MeshRenderer>().sharedMaterial = roofMat;
            var brazier = Part(tower.transform, PrimitiveType.Sphere, new Vector3(0, 11.4f, 0), Vector3.one * 1.3f, Mat("Brazier", Palette.Fire, 0f, new Color(2.5f, 1.2f, 0.2f), 0f), "BeaconFire");
            var bl = Child(tower.transform, "BeaconLight", new Vector3(0, 11.6f, 0)).AddComponent<Light>();
            bl.type = LightType.Point; bl.range = 25f; bl.color = new Color(1f, 0.7f, 0.3f); bl.intensity = 3f;
            bl.gameObject.AddComponent<FlickerLight>().BaseIntensity = 3f;
            var beacon = MeshyIntegrator.GetOrBuild("PT_Meshy_HubBeacon", 16f, false, 0f, true);
            if (beacon != null)
            {
                for (int i = tower.transform.childCount - 1; i >= 0; i--)
                {
                    var ch = tower.transform.GetChild(i);
                    if (ch.GetComponent<Light>() == null) Object.DestroyImmediate(ch.gameObject);
                }
                var bi = (GameObject)PrefabUtility.InstantiatePrefab(beacon, tower.transform);
                bi.name = "MeshyBeacon";
                bl.transform.localPosition = new Vector3(0, 14.5f, 0);
            }
            AddPlaceholder(tower, "hub_landmark", "stylized painterly fantasy village beacon tower, tall stone lighthouse-like watchtower with wooden balconies, red tiled conical roof and a glowing golden brazier on top, game-ready low poly, hand-painted texture");
            if (beacon != null) tower.GetComponent<PlaceholderMarker>().ReplacedByMeshy = true;
            else LogPlaceholder("Landmark_Leuchtfeuerturm (Tier 1)", "Szene: POI_Hub_WeilerSonnfurt", "Primitive-Komposition + MeshKit-Dach", "stylized painterly fantasy village beacon tower, tall stone lighthouse-like watchtower with wooden balconies, red tiled conical roof and a glowing golden brazier on top, game-ready low poly, hand-painted texture");

            // Häuser im Kreis
            var housePrompt = "stylized painterly fantasy timber-frame cottage, cream plaster walls, dark wooden beams, red gable roof, low poly, hand-painted";
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + 0.4f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 45f)) < 25f) continue; // Ausgang Richtung Grenzposten
                Vector2 p = ZoneLayout.Hub.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 14f;
                var house = Child(hub.transform, "House_" + i, Vector3.zero);
                house.transform.position = G(p);
                house.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Cos(a), 0, -Mathf.Sin(a)));
                float w = 4.5f + R01() * 1.5f, l = 6f + R01() * 2f, h = 3.2f;
                Part(house.transform, PrimitiveType.Cube, new Vector3(0, h * 0.5f, 0), new Vector3(l, h, w), plaster, "Walls", new Vector3(0, 90, 0), true);
                Part(house.transform, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(l + 0.3f, 0.5f, w + 0.3f), stone, "Foundation", new Vector3(0, 90, 0));
                for (int b = -1; b <= 1; b++)
                    Part(house.transform, PrimitiveType.Cube, new Vector3(b * l * 0.33f, h * 0.5f, w * 0.5f + 0.02f), new Vector3(0.25f, h, 0.08f), wood, "Beam" + b);
                Part(house.transform, PrimitiveType.Cube, new Vector3(0, 1f, w * 0.5f + 0.05f), new Vector3(1.1f, 2f, 0.1f), wood, "Door");
                var rf = new GameObject("Roof"); rf.transform.SetParent(house.transform, false); rf.transform.localPosition = new Vector3(0, h, 0);
                rf.transform.localRotation = Quaternion.Euler(0, 90, 0);
                var rm = SaveMesh(MeshKit.Roof(w, l, 2.2f, i % 2 == 0 ? Palette.Roof : new Color(0.55f, 0.35f, 0.25f)), "HouseRoof_" + i);
                rf.AddComponent<MeshFilter>().sharedMesh = rm; rf.AddComponent<MeshRenderer>().sharedMaterial = roofMat;
                Part(house.transform, PrimitiveType.Cube, new Vector3(l * 0.3f, h + 1.6f, 0), new Vector3(0.6f, 1.6f, 0.6f), stone, "Chimney");
                VfxKitBuilder.Smoke(house.transform, new Vector3(l * 0.3f, h + 2.5f, 0), false);   // Lauf 5: Kaminrauch
                AddPlaceholder(house, "hub_house_" + i, housePrompt);
            }
            LogPlaceholder("House_0..5", "Szene: POI_Hub_WeilerSonnfurt", "Primitive + MeshKit-Satteldach", housePrompt);

            // Wegmarke
            var wp = Child(hub.transform, "Waypoint_Sonnfurt", Vector3.zero);
            wp.transform.position = G(ZoneLayout.Hub.Center + new Vector2(6f, 6f));
            Part(wp.transform, PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(2.2f, 0.15f, 2.2f), stone, "Pad", null, true);
            var crystal = Child(wp.transform, "Crystal", new Vector3(0, 2.2f, 0));
            Part(crystal.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 1.2f, 0.6f), Mat("WaypointCrystal", new Color(0.4f, 0.8f, 1f), 0f, new Color(0.4f, 1.2f, 2f), 1f), "Gem", new Vector3(45, 0, 45));
            crystal.AddComponent<Bobber>();
            var wl = Child(wp.transform, "Light", new Vector3(0, 2.2f, 0)).AddComponent<Light>();
            wl.type = LightType.Point; wl.color = new Color(0.5f, 0.85f, 1f); wl.range = 8f; wl.intensity = 2f;
            _anchors.Waypoint = wp.transform;

            // Marktstand + Kisten + Laternen
            var crateMat = Mat("Crate", new Color(0.70f, 0.48f, 0.26f));
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = ZoneLayout.Hub.Center + new Vector2(-6f + R01() * 4f, -4f + R01() * 4f);
                Part(hub.transform, PrimitiveType.Cube, G(p, 0.4f) - hub.transform.position, Vector3.one * (0.7f + R01() * 0.3f), crateMat, "Crate" + i, new Vector3(0, R01() * 90f, 0), true);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
                Vector2 p = ZoneLayout.Hub.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8f;
                var lamp = Child(hub.transform, "Lantern" + i, G(p) - hub.transform.position);
                Part(lamp.transform, PrimitiveType.Cylinder, new Vector3(0, 1.4f, 0), new Vector3(0.12f, 1.4f, 0.12f), wood, "Pole", null, true);
                Part(lamp.transform, PrimitiveType.Cube, new Vector3(0, 2.9f, 0), Vector3.one * 0.35f, Mat("LanternGlow", Palette.AllyGold, 0f, new Color(2f, 1.4f, 0.4f), 0f), "Lamp");
                var ll = Child(lamp.transform, "Light", new Vector3(0, 2.9f, 0)).AddComponent<Light>();
                ll.type = LightType.Point; ll.range = 7f; ll.intensity = 1.5f; ll.color = new Color(1f, 0.75f, 0.4f);
                VfxKitBuilder.Smoke(lamp.transform, new Vector3(0, 3.1f, 0), true);   // Lauf 5 / M23: Laternen-Flackern (Funken)
            }
            // Lauf 5: Dorfbewohner mit Tages-Routine (2–3 Punkte, Tätigkeit je Punkt) + Heldenhain (Meshy-Statuen)
            PlaceVillagers(hub.transform);
            CharacterRigs.PlaceStatues(hub.transform, G(ZoneLayout.Hub.Center + new Vector2(-3f, -11f)), 0f);

            // Zaun am Hub-Rand
            var fenceRoot = Child(hub.transform, "Fence", Vector3.zero);
            for (int i = 0; i < 48; i++)
            {
                float a0 = i / 48f * Mathf.PI * 2f, a1 = (i + 1) / 48f * Mathf.PI * 2f;
                float deg = a0 * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 45f)) < 14f || Mathf.Abs(Mathf.DeltaAngle(deg, -20f)) < 12f) continue; // Tore
                Vector2 p0 = ZoneLayout.Hub.Center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 21f;
                Vector2 p1 = ZoneLayout.Hub.Center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 21f;
                Vector3 w0 = G(p0), w1 = G(p1);
                Part(fenceRoot.transform, PrimitiveType.Cube, w0 + Vector3.up * 0.6f - hub.transform.position, new Vector3(0.18f, 1.2f, 0.18f), wood, "Post" + i);
                var rail = Part(fenceRoot.transform, PrimitiveType.Cube, (w0 + w1) * 0.5f + Vector3.up * 0.85f - hub.transform.position, new Vector3(0.1f, 0.12f, Vector3.Distance(w0, w1)), wood, "Rail" + i);
                rail.transform.rotation = Quaternion.LookRotation(w1 - w0);
            }
        }

        // ---------- Grenzposten ----------
        static WorldBanner Banner(Transform parent, Vector3 worldPos, Faction f, string name)
        {
            var root = Child(parent, name, Vector3.zero);
            root.transform.position = worldPos;
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0, 2.5f, 0), new Vector3(0.14f, 2.5f, 0.14f), Mat("Wood", Palette.Wood), "Pole", null, true);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0, 4.9f, 0), new Vector3(1.6f, 0.1f, 0.1f), Mat("Wood", Palette.Wood), "Bar");
            var pivot = Child(root.transform, "ClothPivot", new Vector3(0, 4.85f, 0));
            var clothMat = Mat("BannerCloth", Color.white, 0.2f, null, 0.5f); clothMat.SetFloat("_Wind", 1f); clothMat.SetFloat("_WindPivot", 0.5f);   // Lauf 5: wehendes Banner
            var cloth = Part(pivot.transform, PrimitiveType.Cube, new Vector3(0, -1.0f, 0), new Vector3(1.4f, 2.0f, 0.04f), clothMat, "Cloth");
            var b = root.AddComponent<WorldBanner>();
            b.Cloth = cloth.GetComponent<Renderer>();
            b.Owner = f;
            return b;
        }

        static void BuildOutpost()
        {
            var c = ZoneLayout.Outpost.Center;
            var op = Child(_env, "POI_Grenzposten_Dornwacht", G(c));
            _anchors.OutpostCenter = op.transform;
            var log = Mat("Log", new Color(0.50f, 0.32f, 0.20f));
            var stone = Mat("Stone", Palette.Stone);
            var pal = Child(op.transform, "Palisade", Vector3.zero);
            int posts = 60; float r = 13f;
            for (int i = 0; i < posts; i++)
            {
                float a = i / (float)posts * Mathf.PI * 2f;
                float deg = a * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 225f)) < 13f || Mathf.Abs(Mathf.DeltaAngle(deg, 45f)) < 13f || Mathf.Abs(Mathf.DeltaAngle(deg, 315f)) < 10f) continue; // Tore: Hub, Lager, Ruinen
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                float hgt = 3.2f + R01() * 0.8f;
                Part(pal.transform, PrimitiveType.Cylinder, G(p, hgt * 0.5f - 0.3f) - op.transform.position, new Vector3(0.55f, hgt * 0.5f, 0.55f), log, "Post" + i, new Vector3(R01() * 4f - 2f, 0, R01() * 4f - 2f), true);
            }
            // Wachturm
            var tw = Child(op.transform, "Watchtower", new Vector3(-6f, 0, 6f));
            tw.transform.position = G(c + new Vector2(-7f, 6f));
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 1.2f, z = (i < 2 ? -1 : 1) * 1.2f;
                Part(tw.transform, PrimitiveType.Cube, new Vector3(x, 3f, z), new Vector3(0.3f, 6f, 0.3f), log, "Leg" + i, null, true);
            }
            Part(tw.transform, PrimitiveType.Cube, new Vector3(0, 6f, 0), new Vector3(3.2f, 0.25f, 3.2f), Mat("Wood", Palette.Wood), "Platform", null, true);
            var twRoof = new GameObject("Roof"); twRoof.transform.SetParent(tw.transform, false); twRoof.transform.localPosition = new Vector3(0, 7.6f, 0);
            var trm = new MeshKit(); trm.Cone(Vector3.zero, 4, 2.6f, 1.8f, Palette.Roof); var trmesh = SaveMesh(trm.ToMesh("WatchtowerRoof"), "WatchtowerRoof");
            twRoof.AddComponent<MeshFilter>().sharedMesh = trmesh; twRoof.AddComponent<MeshRenderer>().sharedMaterial = VC;
            twRoof.transform.localRotation = Quaternion.Euler(0, 45, 0);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 1.4f, z = (i < 2 ? -1 : 1) * 1.4f;
                Part(tw.transform, PrimitiveType.Cube, new Vector3(x, 6.9f, z), new Vector3(0.15f, 1.6f, 0.15f), log, "Strut" + i);
            }

            // Banner
            _anchors.OutpostBanners = new[]
            {
                Banner(op.transform, G(c + new Vector2(-2.5f, -1f)), Faction.Ally, "Banner_A"),
                Banner(op.transform, G(c + new Vector2(2.5f, 1f)), Faction.Ally, "Banner_B"),
                Banner(op.transform, G(c + new Vector2(-9.5f, -9.5f)), Faction.Ally, "Banner_Gate"),
            };
            // Feuerstelle, Lager-Zelte
            var tentPrefab = MeshPrefab("Env_Tent_Ally", MeshKit.Tent(new Color(0.85f, 0.78f, 0.6f)), VC, true, true, "stylized painterly canvas army tent, cream fabric with blue trim, low poly, hand-painted");
            Place(tentPrefab, G(c + new Vector2(6f, -6f)), 30f, 1f, op.transform);
            Place(tentPrefab, G(c + new Vector2(8f, -1f)), 80f, 0.9f, op.transform);

            // Anker
            var crates = new List<Transform>();
            Vector2[] cratePos = { new Vector2(-3.5f, 3.5f), new Vector2(3.5f, 4.5f), new Vector2(0.5f, -4.5f) };
            for (int i = 0; i < 3; i++) crates.Add(Anchor("CrateSpot_" + i, G(c + cratePos[i])));
            _anchors.CrateSpots = crates.ToArray();
            var guards = new List<Transform>();
            Vector2[] guardPos = { new Vector2(-8f, -8f), new Vector2(-5f, 0f), new Vector2(5f, -3f), new Vector2(7f, 7f) };
            for (int i = 0; i < guardPos.Length; i++) guards.Add(Anchor("GuardSpot_" + i, G(c + guardPos[i])));
            _anchors.GuardSpots = guards.ToArray();
            var atk = new List<Transform>();
            Vector2[] atkPos = { new Vector2(24f, 20f), new Vector2(22f, -14f), new Vector2(-6f, 26f), new Vector2(18f, 28f) };
            for (int i = 0; i < atkPos.Length; i++) atk.Add(Anchor("OutpostAttackSpawn_" + i, G(c + atkPos[i])));
            _anchors.OutpostAttackSpawns = atk.ToArray();
            var occ = new List<Transform>();
            for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2f; occ.Add(Anchor("OccupierSpot_" + i, G(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 6f))); }
            _anchors.OutpostOccupierSpots = occ.ToArray();
        }

        static Transform Anchor(string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_anchors.transform, true);
            t.position = pos;
            return t;
        }

        // ---------- Lager der Aschkrallen ----------
        static void BuildCamp()
        {
            var c = ZoneLayout.Camp.Center;
            var camp = Child(_env, "POI_Rabenschanze_Lager", G(c));
            _anchors.CampCenter = camp.transform;
            var tent = MeshPrefab("Env_Tent_Raider", MeshKit.Tent(new Color(0.45f, 0.25f, 0.20f)), VC, true, true, "stylized painterly crude raider hide tent with bones and red war paint, low poly, hand-painted");
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + 0.3f;
                Place(tent, G(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 10f), R01() * 360f, 0.9f + R01() * 0.3f, camp.transform);
            }
            // Lagerfeuer
            var fire = Child(camp.transform, "Campfire", Vector3.zero);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Part(fire.transform, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.1f, 0.15f, Mathf.Sin(a) * 1.1f), new Vector3(0.6f, 0.4f, 0.5f), Mat("StoneDark", Palette.StoneDark), "Stone" + i);
            }
            Part(fire.transform, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 1.1f, 0.9f), Mat("Flame", Palette.Fire, 0f, new Color(3f, 1.1f, 0.15f), 0f), "Flame");
            var fl = Child(fire.transform, "FireLight", new Vector3(0, 1.4f, 0)).AddComponent<Light>();
            fl.type = LightType.Point; fl.range = 14f; fl.color = new Color(1f, 0.55f, 0.2f); fl.intensity = 3f;
            fl.gameObject.AddComponent<FlickerLight>().BaseIntensity = 3f;
            _anchors.CampFireFx = fire;
            // Totem + Spieße
            var bone = Mat("Bone", new Color(0.92f, 0.87f, 0.75f), 0.1f);
            var totem = Child(camp.transform, "Totem", new Vector3(5f, 0, -3f));
            totem.transform.position = G(c + new Vector2(5f, -3f));
            Part(totem.transform, PrimitiveType.Cylinder, new Vector3(0, 2.2f, 0), new Vector3(0.5f, 2.2f, 0.5f), Mat("Log", new Color(0.5f, 0.32f, 0.2f)), "Pole", null, true);
            Part(totem.transform, PrimitiveType.Sphere, new Vector3(0, 4.6f, 0), new Vector3(1.0f, 0.8f, 0.9f), bone, "Skull");
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 15f;
                Part(camp.transform, PrimitiveType.Cube, G(p, 0.8f) - camp.transform.position, new Vector3(0.18f, 2.2f, 0.18f), Mat("Log", new Color(0.5f, 0.32f, 0.2f)), "Spike" + i, new Vector3(-30f * Mathf.Cos(a) * 0 + 25f, a * Mathf.Rad2Deg * -1f + 90f, 0));
            }
            _anchors.CampBanners = new[]
            {
                Banner(camp.transform, G(c + new Vector2(-4f, 4f)), Faction.Enemy, "WarBanner_A"),
                Banner(camp.transform, G(c + new Vector2(-12f, -8f)), Faction.Enemy, "WarBanner_Gate"),
            };
            var spawns = new List<Transform>();
            for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2f; spawns.Add(Anchor("CampSpawn_" + i, G(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (5f + (i % 2) * 3f)))); }
            _anchors.CampSpawns = spawns.ToArray();
            _anchors.ChampionSpawn = Anchor("ChampionSpawn", G(c + new Vector2(2f, 2f)));
        }

        // ---------- Ruinen ----------
        static void BuildRuins()
        {
            var c = ZoneLayout.Ruins.Center;
            var ru = Child(_env, "POI_Ruinen_Elmhain", G(c));
            _anchors.RuinsCenter = ru.transform;
            var stone = Mat("RuinStone", new Color(0.78f, 0.74f, 0.66f));
            var moss = Mat("Moss", new Color(0.38f, 0.58f, 0.30f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9f;
                float h = 1.5f + R01() * 4.5f;
                Part(ru.transform, PrimitiveType.Cylinder, G(p, h * 0.5f) - ru.transform.position, new Vector3(1.1f, h * 0.5f, 1.1f), stone, "Pillar" + i, new Vector3(R01() * 6f, 0, R01() * 6f), true);
                Part(ru.transform, PrimitiveType.Cube, G(p, h + 0.05f) - ru.transform.position, new Vector3(1.2f, 0.15f, 1.2f), moss, "PillarMoss" + i);
            }
            var arch = Child(ru.transform, "Arch", Vector3.zero); arch.transform.position = G(c + new Vector2(-3f, -3f));
            Part(arch.transform, PrimitiveType.Cube, new Vector3(-2.2f, 2.5f, 0), new Vector3(1f, 5f, 1f), stone, "L", null, true);
            Part(arch.transform, PrimitiveType.Cube, new Vector3(2.2f, 2.5f, 0), new Vector3(1f, 5f, 1f), stone, "R", null, true);
            Part(arch.transform, PrimitiveType.Cube, new Vector3(0, 5.4f, 0), new Vector3(5.6f, 0.9f, 1.1f), stone, "Top", null, true);
            arch.transform.rotation = Quaternion.Euler(0, 40, 0);
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = c + new Vector2(R01() * 16f - 8f, R01() * 16f - 8f);
                Part(ru.transform, PrimitiveType.Cube, G(p, 0.3f) - ru.transform.position, new Vector3(1.4f + R01(), 0.6f + R01() * 0.6f, 0.8f + R01()), stone, "Rubble" + i, new Vector3(R01() * 20f, R01() * 360f, R01() * 20f), true);
            }
            var sp = new List<Transform>();
            for (int i = 0; i < 4; i++) { float a = i / 4f * Mathf.PI * 2f + 0.5f; sp.Add(Anchor("RuinsSpawn_" + i, G(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4f))); }
            _anchors.RuinsSpawns = sp.ToArray();
            LogPlaceholder("Ruinen (Säulen, Bogen, Schutt)", "Szene: POI_Ruinen_Elmhain", "Primitive-Komposition", "stylized painterly ancient elven ruin pillars and broken arch, mossy pale stone, low poly, hand-painted");
            LogPlaceholder("Palisade/Wachturm", "Szene: POI_Grenzposten_Dornwacht", "Primitive-Komposition + MeshKit-Dach", "stylized painterly wooden frontier palisade wall segment and watchtower, low poly, hand-painted");
        }

        // ---------- Vegetation ----------
        static void ScatterVegetation()
        {
            var veg = Child(_env, "Vegetation", Vector3.zero);
            var trees = new List<GameObject>();
            var windVc = Mat("VertexColor_Wind", Color.white, 0.3f, null, 0.35f); windVc.SetFloat("_Wind", 1f); EditorUtility.SetDirty(windVc);   // Lauf 5: Laub im Wind
            for (int i = 0; i < 4; i++) trees.Add(MeshPrefab("Env_Tree_" + i, MeshKit.Tree(100 + i, i == 3), windVc, true, false, "stylized painterly low poly broadleaf tree with chunky rounded canopy, hand-painted"));
            for (int i = 0; i < 2; i++) trees.Add(MeshPrefab("Env_Pine_" + i, MeshKit.Pine(200 + i), windVc, true, false, "stylized painterly low poly pine tree, layered cone canopy, hand-painted"));
            var rocks = new List<GameObject>();
            for (int i = 0; i < 4; i++) rocks.Add(MeshPrefab("Env_Rock_" + i, MeshKit.Rock(300 + i, i % 2 == 0 ? Palette.Stone : Palette.StoneDark), VC, true, true, "stylized painterly low poly boulder, faceted, hand-painted"));

            int placedTrees = 0, placedRocks = 0, guard = 0;
            var portal = WorldBuilder.FindStartZonePortalSpot();
            var portal2 = new Vector2(portal.x, portal.z);
            while (placedTrees < 170 && guard++ < 6000)
            {
                float x = R01() * 190f - 95f, z = R01() * 190f - 95f;
                if (!ZoneLayout.IsFreeForScatter(x, z, 3f)) continue;
                if (Vector2.Distance(new Vector2(x, z), portal2) < 11f) continue;
                float cluster = Mathf.PerlinNoise(x * 0.04f + 5f, z * 0.04f + 9f);
                if (cluster < 0.45f && R01() > 0.25f) continue;
                float h = H(x, z);
                var prefab = h > 7f ? trees[4 + _rnd.Next(2)] : trees[_rnd.Next(trees.Count)];
                Place(prefab, new Vector3(x, h - 0.1f, z), R01() * 360f, 0.85f + R01() * 0.6f, veg.transform);
                placedTrees++;
            }
            guard = 0;
            while (placedRocks < 70 && guard++ < 4000)
            {
                float x = R01() * 190f - 95f, z = R01() * 190f - 95f;
                if (!ZoneLayout.IsFreeForScatter(x, z, 1f)) continue;
                if (Vector2.Distance(new Vector2(x, z), portal2) < 9f) continue;
                Place(rocks[_rnd.Next(rocks.Count)], new Vector3(x, H(x, z) - 0.2f, z), R01() * 360f, 0.6f + R01() * 1.2f, veg.transform);
                placedRocks++;
            }
            // Felsen am Rand der Arena (Rahmung)
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                float rr = 90f + R01() * 4f;
                float x = Mathf.Clamp(Mathf.Cos(a) * rr * 1.2f, -93f, 93f), z = Mathf.Clamp(Mathf.Sin(a) * rr * 1.2f, -93f, 93f);
                Place(rocks[_rnd.Next(rocks.Count)], new Vector3(x, H(x, z) - 0.5f, z), R01() * 360f, 2.5f + R01() * 2.5f, veg.transform);
            }
        }

        static void BakeNavMesh()
        {
            // Kern-API (UnityEngine.AI.NavMeshBuilder) statt NavMeshSurface: keine Package-Abhängigkeit, Daten als Asset.
            var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
            var bounds = new Bounds(new Vector3(0, 10, 0), new Vector3(ZoneLayout.Size, 80f, ZoneLayout.Size));
            UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, 1, UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0,
                new List<UnityEngine.AI.NavMeshBuildMarkup>(), sources);
            var settings = UnityEngine.AI.NavMesh.GetSettingsByID(0);
            settings.overrideVoxelSize = true; settings.voxelSize = 0.3f;
            settings.agentRadius = 0.5f; settings.agentClimb = 0.6f; settings.agentSlope = 45f;
            var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            string path = ProcDir + "/PT_Zone_NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
            if (data == null) { Debug.LogWarning("[ProjectTyrant] NavMesh-Bake lieferte keine Daten."); return; }
            data.name = "PT_Zone_NavMesh";
            AssetDatabase.CreateAsset(data, path);
            var go = new GameObject("PT_NavMesh");
            go.AddComponent<NavMeshDataLoader>().Data = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
            Debug.Log("[ProjectTyrant] NavMesh gebacken: " + sources.Count + " Quellen.");
        }

        static void BuildWildSpawns()
        {
            var list = new List<Transform>();
            Vector2[] pts = { new Vector2(-30, -10), new Vector2(-20, 15), new Vector2(30, 0), new Vector2(-5, -45), new Vector2(35, 75), new Vector2(75, 20), new Vector2(-70, 10), new Vector2(20, -40) };
            for (int i = 0; i < pts.Length; i++) list.Add(Anchor("WildSpawn_" + i, G(pts[i])));
            _anchors.WildSpawns = list.ToArray();
        }
    }
}
