using System.Collections.Generic;
using ProjectTyrant.Procedural;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Generalisierter Zonen-Builder (Lauf 4): baut aus einer <see cref="ZoneDefinition"/> + <see cref="BiomeProfile"/> eine Zonen-Szene –
    /// Licht/Himmel/Nebel/Post je Biom, Terrain in 128-m-Chunks (4-m-Raster, Collider je Chunk), Wasser, instanzierte Vegetation,
    /// Portale, ZoneRoot, NavMesh und Karten-Textur. Deterministisch über den Seed der Zone.
    /// </summary>
    public static partial class BiomeZoneBuilder
    {
        public const string ZoneSceneDir = Root + "/Scenes/Zones";
        public const float ChunkSize = 128f, Cell = 4f;
        static System.Random _rnd;
        static float R01() => (float)_rnd.NextDouble();

        public static string ScenePath(ZoneDefinition z) => ZoneSceneDir + "/" + z.SceneName + ".unity";

        /// <summary>Gelände-Sampler einer Zone inkl. eingeebneter Plätze (Spawn, Portale, Zonen-Inhalte).</summary>
        public static BiomeTerrain Sampler(ZoneDefinition z)
        {
            var t = new BiomeTerrain(z.Biome, z.Seed, z.Size);
            t.Flatten(new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z), 24f, FlatHeight(t, z.DefaultSpawn.x, z.DefaultSpawn.z));
            foreach (var p in z.Portals) t.Flatten(new Vector2(p.Position.x, p.Position.z), 16f, FlatHeight(t, p.Position.x, p.Position.z));
            ExtraFlats(z, t);
            WorldW1Builder.ApplyToSampler(z, t);   // Welt-Lauf W1: Gelände-Operationen, Fußwege, Plätze
            return t;
        }

        /// <summary>Erweiterungspunkt für Zonen-Inhalte (M16: Schauplätze, Wegmarken …).</summary>
        /// <summary>Platz-Höhe: Mittel der Rohhöhe im Umkreis, mindestens 1,2 m über Wasser/Lava.</summary>
        public static float FlatHeight(BiomeTerrain t, float x, float z)
        {
            float h = 0f; int n = 0;
            for (int i = -2; i <= 2; i++) for (int j = -2; j <= 2; j++) { h += t.RawHeight(x + i * 4f, z + j * 4f); n++; }
            return Mathf.Max(h / n, t.WaterLevel + 1.2f);
        }

        static partial void ExtraFlats(ZoneDefinition z, BiomeTerrain t);
        static partial void PopulateZone(ZoneDefinition z, BiomeTerrain t, Transform env, ZoneRoot root);

        public static void Build(ZoneDefinition z)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            EnsureFolder(ZoneSceneDir);
            _rnd = new System.Random(z.Seed);
            string defPath = AssetDatabase.GetAssetPath(z);
            if (WorldW1Builder.NeedsPlan(z)) { WorldW1Builder.PlanZone(z); AssetDatabase.SaveAssetIfDirty(z); }   // Welt-Lauf W1
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            z = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(defPath);   // NewScene entlädt ungenutzte Assets
            var b = z.Biome;
            var sun = SceneBuilder.CreateSun(); sun.color = b.SunColor; sun.intensity = b.SunIntensity;
            Atmosphere(z);
            PostProcessing(z);
            var env = new GameObject("Environment").transform;
            var sampler = Sampler(z);
            z.DefaultSpawn.y = sampler.Height(z.DefaultSpawn.x, z.DefaultSpawn.z);
            foreach (var p in z.Portals) p.Position.y = sampler.Height(p.Position.x, p.Position.z);
            if (z.Waypoints == null || z.Waypoints.Count == 0)
                z.Waypoints = new List<ZoneWaypointInfo> { new ZoneWaypointInfo { Id = "wp_" + z.Id + "_ankunft", Name = "Wegmarke " + z.DisplayName, Position = z.DefaultSpawn } };
            EditorUtility.SetDirty(z);
            Terrain(z, sampler, env);
            Water(z, env);
            Vegetation(z, sampler, env);
            foreach (var p in z.Portals) BuildPortal(p, env, z);
            var rootGo = new GameObject("PT_ZoneRoot");
            var root = rootGo.AddComponent<ZoneRoot>();
            root.Definition = z;
            var spawn = Child(rootGo.transform, "Spawn", new Vector3(z.DefaultSpawn.x, sampler.Height(z.DefaultSpawn.x, z.DefaultSpawn.z), z.DefaultSpawn.z));
            root.Waypoint = spawn.transform;
            PopulateZone(z, sampler, env, root);
            ArrivalWaypointBuilder.Ensure(z, rootGo.transform, DataDir + "/Zone/" + z.Id, 160);   // Fix-Lauf F1: jeder Eingang mit Wegmarke ≤ 40 m
            ZoneExtrasBuilder.Populate(z, rootGo.transform, (x, zz) => sampler.Height(x, zz));   // Lauf 6: Stadt, Verderbnis, Sandsturm, Dunkelheit, Inseln, Belagerung, Sprungrätsel
            WorldStoryBuilder.Populate(z, rootGo.transform, (x, zz) => sampler.Height(x, zz));   // Lauf 6: Späher, Herzen, Bündel
            WorldW1Builder.Populate(z, rootGo.transform, (x, zz) => sampler.Height(x, zz));   // Welt-Lauf W1: Wahrzeichen, Hürden, Aufstiege, Geheimnisse
            // Lauf 5 / M23: Welt-Lebendigkeit je Biom
            var w = b.Weather;
            VfxKitBuilder.Ambient(rootGo.transform, w, w == WeatherKind.AshWind ? new Vector2(1f, -0.4f) : new Vector2(0.8f, 0.6f), w == WeatherKind.Snow ? 0.35f : w == WeatherKind.AshWind ? 0.3f : 0.2f,
                false, w == WeatherKind.MistWisps, false, w == WeatherKind.MistWisps ? 6 : w == WeatherKind.Snow ? 4 : 0);
            BakeNavMesh(z);
            root.MapTexture = MapBaker.BakeSampler(sampler, env, MapPath(z), z.Size);
            EditorSceneManager.SaveScene(scene, ScenePath(z));
            Debug.Log("[ProjectTyrant] Zone " + z.DisplayName + " gebaut in " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s: " + ScenePath(z));
        }

        public static string MapPath(ZoneDefinition z) => ProcDir + "/PT_Zone_Map_" + z.Id + ".png";

        // ---------------- Atmosphäre ----------------
        static void Atmosphere(ZoneDefinition z)
        {
            var b = z.Biome;
            var shader = Shader.Find("ProjectTyrant/GradientSkybox");
            string path = MatDir + "/M_Skybox_" + z.Id + ".mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null) { sky = new Material(shader); AssetDatabase.CreateAsset(sky, path); }
            sky.shader = shader;
            sky.SetColor("_TopColor", b.SkyTop); sky.SetColor("_HorizonColor", b.SkyHorizon); sky.SetColor("_BottomColor", b.SkyBottom); sky.SetColor("_SunColor", b.SunColor);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(b.SkyTop, Color.white, 0.3f);
            RenderSettings.ambientEquatorColor = b.SkyHorizon * 0.85f;
            RenderSettings.ambientGroundColor = b.SkyBottom * 0.6f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = b.FogColor;
            RenderSettings.fogDensity = b.FogDensity;
        }

        static void PostProcessing(ZoneDefinition z)
        {
            var b = z.Biome;
            string path = DataDir + "/Rendering/PT_Post_" + z.Id + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            foreach (var c in new List<VolumeComponent>(profile.components)) { profile.Remove(c.GetType()); Object.DestroyImmediate(c, true); }
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.45f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.6f);
            var ca = profile.Add<ColorAdjustments>(true); ca.saturation.Override(b.Saturation); ca.contrast.Override(8f); ca.postExposure.Override(0.1f);
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.28f); vig.smoothness.Override(0.45f);
            var st = profile.Add<SplitToning>(true); st.shadows.Override(b.SplitShadows); st.highlights.Override(b.SplitHighlights); st.balance.Override(-10f);
            foreach (var c in profile.components) { AssetDatabase.AddObjectToAsset(c, profile); c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; }
            EditorUtility.SetDirty(profile);
            var go = new GameObject("PT_PostProcessing");
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10; vol.sharedProfile = profile;
        }

        // ---------------- Terrain in Chunks ----------------
        static void Terrain(ZoneDefinition z, BiomeTerrain s, Transform env)
        {
            var root = Child(env, "Terrain", Vector3.zero).transform;
            var mat = Mat("Terrain", Color.white, 0.4f, null, 0.12f);
            foreach (var r in WorldModel.SplitChunks(z.Size, ChunkSize))
            {
                var mesh = ChunkMesh(s, r, Cell);
                mesh.name = "Chunk_" + z.Id + "_" + Mathf.RoundToInt(r.x) + "_" + Mathf.RoundToInt(r.y);
                var go = new GameObject(mesh.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        public static Mesh ChunkMesh(ITerrainSampler s, Rect r, float cell)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt(r.width / cell)), nz = Mathf.Max(1, Mathf.RoundToInt(r.height / cell));
            float cx = r.width / nx, cz = r.height / nz;
            var hs = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++) hs[i, j] = s.Height(r.x + i * cx, r.y + j * cz);
            var k = new MeshKit();
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x0 = r.x + i * cx, z0 = r.y + j * cz, x1 = x0 + cx, z1 = z0 + cz;
                    Vector3 a = new Vector3(x0, hs[i, j], z0), b = new Vector3(x0, hs[i, j + 1], z1), c = new Vector3(x1, hs[i + 1, j + 1], z1), d = new Vector3(x1, hs[i + 1, j], z0);
                    if (((i + j) & 1) == 0) { Tri(k, s, a, b, c); Tri(k, s, a, c, d); } else { Tri(k, s, a, b, d); Tri(k, s, b, c, d); }
                }
            var m = k.ToMesh("Chunk");
            m.RecalculateBounds();
            return m;
        }

        static void Tri(MeshKit k, ITerrainSampler s, Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            var center = (a + b + c) / 3f;
            k.Tri(a, b, c, s.GroundColor(center.x, center.z, center.y, 1f - Mathf.Abs(n.y)));
        }

        static void Water(ZoneDefinition z, Transform env)
        {
            var b = z.Biome;
            if (b.WaterLevel < -50f) return;
            var emission = b.Ambient == AmbientKind.Embers ? new Color(b.Water.r, b.Water.g * 0.8f, b.Water.b * 0.5f) * 4.5f : new Color(0.02f, 0.06f, 0.08f);
            // Lauf 5 / M23: Wasser-Shader (Wellen, Ufer-Schaum, Fresnel); Lava leuchtet
            bool lava = b.Ambient == AmbientKind.Embers;
            var mat = VfxKitBuilder.WaterMat(z.Id, lava ? new Color(1f, 0.55f, 0.15f, 0.95f) : new Color(b.Water.r * 1.3f, b.Water.g * 1.3f, b.Water.b * 1.2f, 0.7f), new Color(b.Water.r * 0.5f, b.Water.g * 0.6f, b.Water.b * 0.8f, 0.95f), lava ? emission * 0.35f : Color.black);
            var w = Part(env, PrimitiveType.Cube, new Vector3(0, b.WaterLevel - 0.05f, 0), new Vector3(z.Size, 0.1f, z.Size), mat, "Water");
            w.layer = 4;   // Water
        }

        // ---------------- Vegetation (Instancing) ----------------
        static Mesh Tinted(Mesh src, Color canopy, Color trunk, string name, bool dead)
        {
            var m = Object.Instantiate(src); m.name = name;
            var cols = m.colors;
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                bool leaf = c.g > c.r * 1.05f;
                cols[i] = dead ? Color.Lerp(trunk, Color.black, leaf ? 0.25f : 0f) : leaf ? Color.Lerp(c, canopy, 0.75f) : Color.Lerp(c, trunk, 0.6f);
            }
            m.colors = cols;
            return m;
        }

        static void Vegetation(ZoneDefinition z, BiomeTerrain s, Transform env)
        {
            var b = z.Biome;
            var go = new GameObject("Vegetation"); go.transform.SetParent(env, false);
            var vr = go.AddComponent<VegetationRenderer>();
            var mat = Mat("Vegetation_Instanced", Color.white, 0.3f, null, 0.35f);
            mat.enableInstancing = true; EditorUtility.SetDirty(mat);
            string dir = ProcDir + "/Meshes/Zones"; EnsureFolder(dir);
            Mesh SaveMesh(Mesh m) { string p = dir + "/" + m.name + ".asset"; var ex = AssetDatabase.LoadAssetAtPath<Mesh>(p); if (ex != null) { EditorUtility.CopySerialized(m, ex); return ex; } AssetDatabase.CreateAsset(m, p); return m; }
            var trees = new List<Mesh>();
            for (int i = 0; i < 3; i++)
            {
                var src = b.Conifers ? MeshKit.Pine(500 + i + z.Seed % 50) : MeshKit.Tree(500 + i + z.Seed % 50, false);
                trees.Add(SaveMesh(Tinted(src, b.TreeColor * (0.9f + 0.1f * i), b.TrunkColor, "Veg_" + z.Id + "_Tree" + i, b.DeadTrees)));
            }
            var rocks = new List<Mesh>();
            for (int i = 0; i < 3; i++) rocks.Add(SaveMesh(Rename(MeshKit.Rock(600 + i + z.Seed % 50, Color.Lerp(b.Cliff, b.GroundHigh, i * 0.3f)), "Veg_" + z.Id + "_Rock" + i)));
            var chunks = WorldModel.SplitChunks(z.Size, ChunkSize);
            var treeBatches = new List<VegetationRenderer.Batch>(); var rockBatches = new List<VegetationRenderer.Batch>();
            // Lauf 5 / M23: Bäume mit Wind (eigenes Material, Felsen bleiben starr)
            var windMat = Mat("Vegetation_Wind", Color.white, 0.3f, null, 0.35f); windMat.enableInstancing = true; windMat.SetFloat("_Wind", 1f); EditorUtility.SetDirty(windMat);
            foreach (var t in trees) treeBatches.Add(NewBatch(t, windMat, 400f, chunks, true));
            foreach (var r in rocks) rockBatches.Add(NewBatch(r, mat, 300f, chunks, true));
            var treeLists = new List<List<Matrix4x4>[]>(); foreach (var _ in trees) treeLists.Add(NewLists(chunks.Count));
            var rockLists = new List<List<Matrix4x4>[]>(); foreach (var _ in rocks) rockLists.Add(NewLists(chunks.Count));
            float half = z.Size * 0.5f;
            int treeTarget = Mathf.RoundToInt(z.Size * z.Size / 10000f * 45f * b.TreeDensity);
            int rockTarget = Mathf.RoundToInt(z.Size * z.Size / 10000f * 18f * b.RockDensity);
            Scatter(s, z, treeTarget, half, 0.35f, true, (p, rot, scale) => { int k = _rnd.Next(trees.Count); treeLists[k][ChunkIndex(chunks, p)].Add(Matrix4x4.TRS(p, rot, Vector3.one * scale)); });
            Scatter(s, z, rockTarget, half, 0.8f, false, (p, rot, scale) => { int k = _rnd.Next(rocks.Count); rockLists[k][ChunkIndex(chunks, p)].Add(Matrix4x4.TRS(p + Vector3.down * 0.3f, rot, Vector3.one * scale * 1.4f)); });
            for (int i = 0; i < trees.Count; i++) Fill(treeBatches[i], treeLists[i]);
            for (int i = 0; i < rocks.Count; i++) Fill(rockBatches[i], rockLists[i]);
            vr.Batches.AddRange(treeBatches); vr.Batches.AddRange(rockBatches);
            vr.CellSize = ChunkSize; vr.CellHeight = b.Amplitude * 3f + b.EdgeMountainHeight + 30f;
        }

        static Mesh Rename(Mesh m, string n) { m.name = n; return m; }
        static List<Matrix4x4>[] NewLists(int n) { var a = new List<Matrix4x4>[n]; for (int i = 0; i < n; i++) a[i] = new List<Matrix4x4>(); return a; }

        static VegetationRenderer.Batch NewBatch(Mesh m, Material mat, float dist, List<Rect> chunks, bool shadows)
        {
            var b = new VegetationRenderer.Batch { Name = m.name, Mesh = m, Material = mat, ViewDistance = dist, CastShadows = shadows };
            foreach (var r in chunks) b.Cells.Add(new VegetationRenderer.Cell { Center = new Vector3(r.center.x, 0f, r.center.y) });
            return b;
        }

        static void Fill(VegetationRenderer.Batch b, List<Matrix4x4>[] lists)
        {
            for (int i = 0; i < lists.Length; i++)
            {
                b.Cells[i].Matrices = lists[i].ToArray();
                if (lists[i].Count > 0) { float avg = 0; foreach (var m in lists[i]) avg += m.m13; b.Cells[i].Center.y = avg / lists[i].Count; }
            }
            b.Cells.RemoveAll(c => c.Matrices.Length == 0);
        }

        static int ChunkIndex(List<Rect> chunks, Vector3 p)
        {
            for (int i = 0; i < chunks.Count; i++) if (chunks[i].Contains(new Vector2(p.x, p.z))) return i;
            return 0;
        }

        /// <summary>Freie Fläche für Vegetation (kein Wasser, nicht zu steil, nicht auf eingeebneten Plätzen/Portalen).</summary>
        public static bool FreeForVegetation(BiomeTerrain s, ZoneDefinition z, float x, float y, float maxSlope)
        {
            float h = s.Height(x, y);
            if (h < s.WaterLevel + 0.4f) return false;
            float sx = s.Height(x + 2f, y) - s.Height(x - 2f, y), sz = s.Height(x, y + 2f) - s.Height(x, y - 2f);
            if (Mathf.Sqrt(sx * sx + sz * sz) / 4f > maxSlope) return false;
            if (Vector2.Distance(new Vector2(x, y), new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z)) < 26f) return false;
            foreach (var p in z.Portals) if (Vector2.Distance(new Vector2(x, y), new Vector2(p.Position.x, p.Position.z)) < 18f) return false;
            return ExtraFree(z, x, y);
        }

        public static System.Func<ZoneDefinition, float, float, bool> ExtraFreeCheck;
        static bool ExtraFree(ZoneDefinition z, float x, float y) => ExtraFreeCheck == null || ExtraFreeCheck(z, x, y);

        static void Scatter(BiomeTerrain s, ZoneDefinition z, int target, float half, float maxSlope, bool clustered, System.Action<Vector3, Quaternion, float> place)
        {
            int placed = 0, guard = 0;
            while (placed < target && guard++ < target * 12)
            {
                float x = (R01() * 2f - 1f) * half * 0.93f, y = (R01() * 2f - 1f) * half * 0.93f;
                if (clustered && Mathf.PerlinNoise(x * 0.012f + z.Seed, y * 0.012f + 7f) < 0.42f && R01() > 0.2f) continue;
                if (!FreeForVegetation(s, z, x, y, maxSlope)) continue;
                place(new Vector3(x, s.Height(x, y) - 0.1f, y), Quaternion.Euler(0, R01() * 360f, 0), 0.8f + R01() * 0.7f);
                placed++;
            }
        }

        // ---------------- Portale ----------------
        public static GameObject BuildPortal(PortalDef p, Transform parent, ZoneDefinition from)
        {
            var world = WorldBuilder.LoadWorld();
            var target = world != null ? world.Zone(p.TargetZone) : null;
            var go = new GameObject("Portal_" + p.Id);
            go.transform.SetParent(parent, false);
            float h = p.Position.y;
            go.transform.position = new Vector3(p.Position.x, h, p.Position.z);
            go.transform.rotation = Quaternion.Euler(0, p.Yaw, 0);
            var stone = Mat("PortalStone", new Color(0.55f, 0.53f, 0.6f));
            var glow = Mat("PortalGlow", new Color(0.55f, 0.75f, 1f), 0.1f, new Color(0.4f, 0.65f, 1f) * 2.5f);
            Part(go.transform, PrimitiveType.Cube, new Vector3(-2.6f, 3f, 0), new Vector3(1f, 6f, 1f), stone, "PillarL", null, true);
            Part(go.transform, PrimitiveType.Cube, new Vector3(2.6f, 3f, 0), new Vector3(1f, 6f, 1f), stone, "PillarR", null, true);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 6.4f, 0), new Vector3(6.6f, 0.9f, 1.2f), stone, "Lintel", null, true);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 3f, 0), new Vector3(4.2f, 5.8f, 0.08f), glow, "Veil");
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(go.transform, false); l.transform.localPosition = new Vector3(0, 3f, 0.8f);
            l.type = LightType.Point; l.color = new Color(0.5f, 0.7f, 1f); l.range = 12f; l.intensity = 2.5f; l.shadows = LightShadows.None;
            var plate = new GameObject("Nameplate"); plate.transform.SetParent(go.transform, false); plate.transform.localPosition = new Vector3(0, 7.8f, 0);
            var tmp = plate.AddComponent<TextMeshPro>();
            string label = target != null ? target.DisplayName + "  " + target.LevelText : p.TargetZone;
            tmp.text = "<b>" + label + "</b>"; tmp.fontSize = 3f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color(0.8f, 0.9f, 1f);
            tmp.rectTransform.sizeDelta = new Vector2(12, 2);
            plate.AddComponent<Billboard>();
            var portal = go.AddComponent<Portal>();
            portal.PortalId = p.Id; portal.TargetZone = p.TargetZone; portal.TargetPortal = p.TargetPortal; portal.Label = "Portal: " + label; portal.Radius = 2.8f;
            AddPlaceholder(go, "Portal_Arch (Tier 2)", "stylized painterly fantasy zone portal, tall stone archway with glowing blue magical veil, game-ready low poly, hand-painted texture");
            var meshyPortal = MeshyIntegrator.GetOrBuild("PT_Meshy_PortalArch", 7.2f, false, 0f, true);
            if (meshyPortal != null)
            {
                foreach (var n in new[] { "PillarL", "PillarR", "Lintel" }) { var c = go.transform.Find(n); if (c != null) Object.DestroyImmediate(c.gameObject); }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(meshyPortal, go.transform);
                inst.name = "Meshy_PortalArch"; inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity;
                var ph = go.GetComponent<PlaceholderMarker>(); if (ph != null) ph.ReplacedByMeshy = true;
            }
            return go;
        }

        // ---------------- NavMesh ----------------
        static void BakeNavMesh(ZoneDefinition z)
        {
            var sources = new List<UnityEngine.AI.NavMeshBuildSource>();
            var bounds = new Bounds(new Vector3(0, 20, 0), new Vector3(z.Size, 160f, z.Size));
            UnityEngine.AI.NavMeshBuilder.CollectSources(bounds, ~(1 << 4), UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders, 0, new List<UnityEngine.AI.NavMeshBuildMarkup>(), sources);
            var settings = UnityEngine.AI.NavMesh.GetSettingsByID(0);
            settings.overrideVoxelSize = true; settings.voxelSize = 0.6f; settings.overrideTileSize = true; settings.tileSize = 128;
            settings.agentRadius = 0.5f; settings.agentClimb = 0.6f; settings.agentSlope = 45f;
            var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            string path = ProcDir + "/PT_NavMesh_" + z.Id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
            if (data == null) { Debug.LogWarning("[ProjectTyrant] NavMesh " + z.Id + ": keine Daten."); return; }
            data.name = "PT_NavMesh_" + z.Id;
            AssetDatabase.CreateAsset(data, path);
            new GameObject("PT_NavMesh").AddComponent<NavMeshDataLoader>().Data = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
        }
    }
}
