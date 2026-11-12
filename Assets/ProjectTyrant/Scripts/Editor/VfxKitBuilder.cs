using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M23: VFX-Baukasten. Erzeugt aus Form × Palette × Größe (13 × 11 × 4 = 572) gepoolte Prefabs aus Shuriken-Systemen,
    /// Mesh-Effekten (Ring, Kuppel, Säule, Siegel mit HLSL-FX-Shadern), Blitz-Linien und Licht-Anforderung; dazu den Katalog.
    /// </summary>
    public static class VfxKitBuilder
    {
        public const string PrefabRoot = PrefabDir + "/VFX";
        public const string CatalogPath = DataDir + "/VfxCatalog.asset";
        public const string MeshDirFx = ProcDir + "/Meshes/FX";
        public static VfxCatalog Catalog => AssetDatabase.LoadAssetAtPath<VfxCatalog>(CatalogPath);

        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        static Mesh _ring, _dome, _cyl, _quad;

        public static float Scale(VfxSize s) => s == VfxSize.S ? 0.6f : s == VfxSize.M ? 1f : s == VfxSize.L ? 1.6f : 2.4f;
        static float CountK(VfxSize s) => s == VfxSize.S ? 0.5f : s == VfxSize.M ? 1f : s == VfxSize.L ? 1.8f : 3.4f;

        public static List<VfxCatalog.PaletteDef> PaletteDefs()
        {
            VfxCatalog.PaletteDef P(VfxPalette id, Color a, Color b, Color l, float grav, float rise, float turb, float spd, bool alpha, string p1, string p2, bool bolt = false)
                => new VfxCatalog.PaletteDef { Id = id, A = a, B = b, Light = l, Gravity = grav, Rise = rise, Turbulence = turb, Speed = spd, Alpha = alpha, Particle = p1, Secondary = p2, Lightning = bolt, SoundFamily = id.ToString() };
            return new List<VfxCatalog.PaletteDef>
            {
                P(VfxPalette.Feuer, new Color(1f, 0.55f, 0.12f), new Color(1f, 0.2f, 0.05f), new Color(1f, 0.55f, 0.2f), -0.3f, 1.2f, 0.8f, 1f, false, "Glow", "Ember"),
                P(VfxPalette.Wasser, new Color(0.4f, 0.8f, 1f), new Color(0.15f, 0.45f, 1f), new Color(0.35f, 0.7f, 1f), 0.8f, 0f, 0.3f, 1f, false, "Drop", "Glow"),
                P(VfxPalette.Luft, new Color(0.85f, 0.85f, 1f), new Color(0.55f, 0.45f, 1f), new Color(0.7f, 0.65f, 1f), 0f, 0.3f, 1.2f, 1.5f, false, "Glow", "Spark", true),
                P(VfxPalette.Erde, new Color(0.7f, 0.55f, 0.35f), new Color(0.45f, 0.33f, 0.2f), new Color(0.9f, 0.7f, 0.4f), 1.6f, 0f, 0.2f, 0.8f, true, "Shard", "Smoke"),
                P(VfxPalette.Natur, new Color(0.55f, 0.95f, 0.35f), new Color(0.2f, 0.65f, 0.25f), new Color(0.5f, 1f, 0.4f), 0.15f, 0.3f, 0.9f, 0.8f, false, "Leaf", "Glow"),
                P(VfxPalette.Schatten, new Color(0.35f, 0.25f, 0.45f), new Color(0.08f, 0.05f, 0.12f), new Color(0.55f, 0.3f, 0.8f), -0.1f, 0.5f, 0.6f, 0.6f, true, "Smoke", "Glow"),
                P(VfxPalette.Licht, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.78f, 0.3f), new Color(1f, 0.9f, 0.55f), -0.1f, 0.8f, 0.3f, 1f, false, "Glow", "Spark"),
                P(VfxPalette.Blut, new Color(0.85f, 0.1f, 0.12f), new Color(0.45f, 0.02f, 0.05f), new Color(1f, 0.25f, 0.2f), 1.2f, 0f, 0.3f, 1f, true, "Drop", "Smoke"),
                P(VfxPalette.Arkan, new Color(0.95f, 0.45f, 1f), new Color(0.5f, 0.2f, 1f), new Color(0.8f, 0.4f, 1f), 0f, 0.4f, 1f, 1.1f, false, "Glow", "Sigil"),
                P(VfxPalette.Frost, new Color(0.8f, 0.95f, 1f), new Color(0.45f, 0.75f, 1f), new Color(0.6f, 0.85f, 1f), 0.3f, 0.1f, 0.4f, 0.7f, false, "Flake", "Shard"),
                P(VfxPalette.Glut, new Color(1f, 0.75f, 0.3f), new Color(0.95f, 0.35f, 0.1f), new Color(1f, 0.6f, 0.25f), -0.5f, 1.5f, 1.1f, 1f, false, "Ember", "Spark"),
                // Lauf 5 / M26: Signaturen der Elite-Spezialisierungen
                P(VfxPalette.Mondlicht, new Color(0.85f, 0.9f, 1f), new Color(0.55f, 0.85f, 0.6f), new Color(0.8f, 0.9f, 1f), 0.2f, 0.3f, 0.7f, 0.7f, false, "Leaf", "Glow"),          // Hainwächter: Silber, Blütenblätter
                P(VfxPalette.Wildblut, new Color(0.75f, 0.3f, 0.15f), new Color(0.4f, 0.18f, 0.1f), new Color(0.9f, 0.4f, 0.2f), 0.6f, 0.1f, 0.8f, 1.1f, true, "Shard", "Smoke"),       // Bestienbund: Rot-Braun, Fell
                P(VfxPalette.Dorngift, new Color(0.55f, 0.95f, 0.2f), new Color(0.15f, 0.5f, 0.1f), new Color(0.5f, 1f, 0.25f), 0.8f, 0f, 0.4f, 0.9f, true, "Shard", "Leaf"),        // Dornenwächter: Giftgrün, Ranken
                P(VfxPalette.Himmel, new Color(0.55f, 0.85f, 1f), new Color(0.95f, 0.98f, 1f), new Color(0.6f, 0.85f, 1f), -0.1f, 0.4f, 0.6f, 1.6f, false, "Glow", "Spark"),        // Sturmfalke: Himmelblau, Zielstrahl
                P(VfxPalette.Gewitter, new Color(0.95f, 0.97f, 1f), new Color(0.5f, 0.55f, 0.85f), new Color(0.85f, 0.9f, 1f), 0f, 0.4f, 1.4f, 1.6f, false, "Spark", "Glow", true),  // Sturmrufer: Gewitterweiß
                P(VfxPalette.Zwielicht, new Color(1f, 0.55f, 0.35f), new Color(0.35f, 0.55f, 1f), new Color(0.8f, 0.55f, 0.9f), 0f, 0.5f, 1.0f, 1.0f, false, "Glow", "Sigil"),      // Weber: zweifarbige Bänder
                P(VfxPalette.Jade, new Color(0.35f, 0.95f, 0.6f), new Color(0.95f, 0.8f, 0.3f), new Color(0.5f, 1f, 0.6f), 0f, 0.3f, 0.8f, 1.2f, false, "Sigil", "Glow"),          // Katalysator: Jadegrün-Gold, Ringe
                P(VfxPalette.Lava, new Color(1f, 0.45f, 0.08f), new Color(0.12f, 0.08f, 0.08f), new Color(1f, 0.4f, 0.1f), 0.9f, 0.5f, 0.6f, 0.8f, true, "Ember", "Shard"),      // Glutbinder: Lava auf Obsidian
                P(VfxPalette.Pulver, new Color(0.9f, 0.15f, 0.15f), new Color(0.12f, 0.1f, 0.1f), new Color(1f, 0.25f, 0.2f), -0.2f, 0.6f, 0.9f, 1.8f, true, "Smoke", "Spark"),    // Wegelagerer: Schwarz-Rot, Pulverrauch
                P(VfxPalette.Staubgold, new Color(0.95f, 0.8f, 0.45f), new Color(0.6f, 0.45f, 0.25f), new Color(1f, 0.85f, 0.5f), 0.4f, 0.2f, 1.2f, 1.3f, true, "Smoke", "Spark"),  // Phantomtänzer: Staubgold, Schlieren
                P(VfxPalette.Tiefviolett, new Color(0.45f, 0.15f, 0.7f), new Color(0.08f, 0.02f, 0.15f), new Color(0.55f, 0.2f, 0.9f), -0.2f, 0.4f, 0.9f, 0.7f, true, "Smoke", "Glow"), // Schattenhüter: Tentakel-Schatten
                P(VfxPalette.Stahl, new Color(0.75f, 0.9f, 0.95f), new Color(0.3f, 0.85f, 0.95f), new Color(0.6f, 0.9f, 1f), 0.3f, 0f, 0.5f, 1.7f, false, "Shard", "Spark"),      // Klingenwirbel: Stahlsilber-Cyan
                P(VfxPalette.Richtglut, new Color(1f, 0.8f, 0.35f), new Color(1f, 0.5f, 0.12f), new Color(1f, 0.7f, 0.3f), -0.2f, 0.9f, 0.5f, 1.5f, false, "Glow", "Ember"),     // Glutrichter: Gold-Orange, Lichtpfeile
                P(VfxPalette.Bollwerk, new Color(0.85f, 0.92f, 1f), new Color(0.35f, 0.55f, 1f), new Color(0.7f, 0.85f, 1f), 0f, 0.2f, 0.2f, 0.8f, false, "Sigil", "Glow"),      // Bollwerkmeister: Blauweiß, Schildsegmente
                P(VfxPalette.Sturmblau, new Color(0.35f, 0.7f, 1f), new Color(0.15f, 0.3f, 1f), new Color(0.4f, 0.7f, 1f), 0f, 0.5f, 1.3f, 1.7f, false, "Spark", "Glow", true),   // Sturmherold: Elektroblau, Blitzsäulen
                P(VfxPalette.Smaragd, new Color(0.25f, 0.85f, 0.45f), new Color(0.95f, 0.8f, 0.3f), new Color(0.35f, 0.95f, 0.5f), 0.3f, 0.3f, 0.5f, 0.9f, false, "Leaf", "Sigil"),  // Dornenschild: Smaragd-Gold
            };
        }

        [MenuItem("Project Tyrant/Lauf 5/VFX-Baukasten bauen")]
        public static void BuildAll()
        {
            FxTextureKit.BuildAll();
            EnsureUrpTextures();
            EnsureFolder(PrefabRoot); EnsureFolder(MeshDirFx);
            _mats.Clear(); BuildMeshes();
            var vlib = AssetDatabase.LoadAssetAtPath<VfxLibrary>("Assets/ProjectTyrant/Data/VfxLibrary.asset");
            if (vlib != null) { vlib.ParticleGlow = AmbientMat("Glow", false); vlib.ParticleDrop = AmbientMat("Drop", true); EditorUtility.SetDirty(vlib); }
            var cat = AssetDatabase.LoadAssetAtPath<VfxCatalog>(CatalogPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<VfxCatalog>(); AssetDatabase.CreateAsset(cat, CatalogPath); }
            cat.Palettes = PaletteDefs(); cat.Entries.Clear();
            foreach (var pal in cat.Palettes)
            {
                string dir = PrefabRoot + "/" + pal.Id; EnsureFolder(dir);
                foreach (VfxShape shape in System.Enum.GetValues(typeof(VfxShape)))
                    foreach (VfxSize size in System.Enum.GetValues(typeof(VfxSize)))
                    {
                        string key = VfxIds.Compose(shape, pal.Id, size);
                        var go = Build(shape, pal, size, out float dur, out bool loop);
                        int max = 0; foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) max += ps.main.maxParticles;
                        var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + shape + "_" + size + ".prefab");
                        Object.DestroyImmediate(go);
                        cat.Entries.Add(new VfxCatalog.Entry { Key = key, Prefab = prefab, MaxParticles = max, Duration = dur, Loop = loop });
                    }
            }
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] VFX-Baukasten: " + cat.Entries.Count + " Kompositionen.");
        }

        /// <summary>Soft-Particles brauchen die Tiefen-Textur, Verzerrung die Opaque-Textur.</summary>
        static void EnsureUrpTextures()
        {
            var rp = GraphicsSettings.defaultRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (rp == null) return;
            rp.supportsCameraDepthTexture = true; rp.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(rp);
        }

        // ------------------------------------------------------------ Materialien/Meshes
        static Material PMat(VfxCatalog.PaletteDef p, string tex, bool alpha)
        {
            string key = p.Id + "_" + tex + (alpha ? "_A" : "");
            if (_mats.TryGetValue(key, out var m)) return m;
            m = FxMat("P_" + key, "ProjectTyrant/FX/Particle", Color.white);
            m.SetTexture("_MainTex", FxTextureKit.Get(tex));
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", alpha ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.One);
            m.SetFloat("_Intensity", alpha ? 1f : 1.25f);
            m.renderQueue = alpha ? 3000 : 3010;
            _mats[key] = m; return m;
        }

        static Material MMat(VfxCatalog.PaletteDef p, string tex, float fresnel, Vector4 scroll)
        {
            string key = "M_" + p.Id + "_" + tex + "_" + fresnel.ToString("0");
            if (_mats.TryGetValue(key, out var m)) return m;
            m = FxMat(key, "ProjectTyrant/FX/Mesh", p.A);
            m.SetTexture("_MainTex", FxTextureKit.Get(tex)); m.SetTexture("_NoiseTex", FxTextureKit.Get("Noise"));
            m.SetColor("_EdgeColor", p.Light); m.SetFloat("_Fresnel", fresnel); m.SetVector("_Scroll", scroll); m.SetFloat("_Intensity", p.Alpha ? 1.1f : 1.3f);
            _mats[key] = m; return m;
        }

        static void BuildMeshes()
        {
            _ring = SaveMesh(RingMesh(1f, 0.82f, 48), "FX_Ring");
            _dome = SaveMesh(DomeMesh(1f, 24, 10), "FX_Dome");
            _cyl = SaveMesh(CylMesh(0.5f, 1f, 24), "FX_Cylinder");
            _quad = SaveMesh(QuadMesh(), "FX_FlatQuad");
        }

        public static Mesh SaveMesh(Mesh m, string name)
        {
            string path = MeshDirFx + "/" + name + ".asset";
            var ex = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (ex != null) { ex.Clear(); ex.SetVertices(m.vertices); ex.SetUVs(0, m.uv); ex.SetNormals(m.normals); ex.SetTriangles(m.triangles, 0); ex.RecalculateBounds(); EditorUtility.SetDirty(ex); return ex; }
            m.name = name; AssetDatabase.CreateAsset(m, path); return m;
        }

        static Mesh RingMesh(float outer, float inner, int seg)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * inner); v.Add(d * outer); uv.Add(new Vector2(i / (float)seg * 4f, 0)); uv.Add(new Vector2(i / (float)seg * 4f, 1));
                if (i < seg) { int k = i * 2; t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
            }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        static Mesh DomeMesh(float r, int lon, int lat)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= lat; j++)
                for (int i = 0; i <= lon; i++)
                {
                    float th = j / (float)lat * Mathf.PI * 0.5f, ph = i / (float)lon * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(th) * Mathf.Cos(ph), Mathf.Sin(th), Mathf.Cos(th) * Mathf.Sin(ph)) * r); uv.Add(new Vector2(i / (float)lon * 3f, j / (float)lat));
                }
            for (int j = 0; j < lat; j++) for (int i = 0; i < lon; i++) { int a = j * (lon + 1) + i, b = a + lon + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        static Mesh CylMesh(float r, float h, int seg)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                v.Add(d); v.Add(d + Vector3.up * h); uv.Add(new Vector2(i / (float)seg * 2f, 0)); uv.Add(new Vector2(i / (float)seg * 2f, 1));
                if (i < seg) { int k = i * 2; t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
            }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        static Mesh QuadMesh()
        {
            var m = new Mesh();
            m.SetVertices(new List<Vector3> { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0); m.RecalculateNormals(); return m;
        }

        // ------------------------------------------------------------ Welt (Wasser, Wind, Ambient)
        /// <summary>Wasser-Material (Wellen, Ufer-Schaum, Fresnel); Lava über Eigenleuchten.</summary>
        public static Material WaterMat(string name, Color shallow, Color deep, Color emission)
        {
            var m = FxMat("Water_" + name, Visuals.PTShaders.Water2, Color.white);   // Grafik-Lauf G3: Wasser 2.0
            m.SetColor("_ShallowColor", shallow); m.SetColor("_DeepColor", deep); m.SetColor("_Emission", emission);
            if (emission.maxColorComponent > 0.5f) { m.SetColor("_FoamColor", new Color(1f, 0.85f, 0.4f)); m.SetFloat("_WaveAmp", 0.03f); m.SetColor("_SkyColor", deep); m.SetFloat("_CausticStrength", 0f); m.SetFloat("_Reflection", 0.2f); }
            if (emission.maxColorComponent > 0.5f) MaterialMigrator.ToLava(m); else MaterialMigrator.ApplyFamilyDefaults(m);   // Grafik-Lauf G3: Lavaseen
            EditorUtility.SetDirty(m); return m;
        }

        public static Material AmbientMat(string tex, bool alpha)
        {
            var m = FxMat("Ambient_" + tex + (alpha ? "_A" : ""), "ProjectTyrant/FX/Particle", Color.white);
            m.SetTexture("_MainTex", FxTextureKit.Get(tex));
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", alpha ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.One);
            m.SetFloat("_Intensity", alpha ? 1f : 1.4f); m.SetFloat("_SoftFade", 1.2f);
            EditorUtility.SetDirty(m); return m;
        }

        public static Material GhostMat()
        {
            var m = FxMat("DodgeGhost", "ProjectTyrant/FX/Mesh", new Color(0.55f, 0.75f, 1f, 0.8f));
            m.SetTexture("_MainTex", FxTextureKit.Get("Glow")); m.SetTexture("_NoiseTex", FxTextureKit.Get("Noise")); m.SetFloat("_Fresnel", 2f); m.SetFloat("_Intensity", 1.5f);
            m.SetTextureScale("_MainTex", Vector2.zero);   // flache Farbe (Textur-Mitte)
            m.SetTextureOffset("_MainTex", new Vector2(0.5f, 0.5f));
            EditorUtility.SetDirty(m); return m;
        }

        /// <summary>Vogel (V-Flügel, Rumpf) als einfaches Mesh; Flügelschlag skaliert die Spannweite.</summary>
        public static Mesh BirdMesh()
        {
            EnsureFolder(MeshDirFx);
            var m = new Mesh();
            var v = new List<Vector3> { new Vector3(0, 0, 0.35f), new Vector3(0, 0.05f, -0.3f), new Vector3(-0.8f, 0.12f, -0.05f), new Vector3(0.8f, 0.12f, -0.05f), new Vector3(0, -0.05f, -0.3f) };
            m.SetVertices(v); m.SetTriangles(new[] { 0, 2, 1, 0, 1, 3, 0, 1, 2, 0, 3, 1, 0, 4, 1 }, 0); m.RecalculateNormals();
            return SaveMesh(m, "FX_Bird");
        }

        /// <summary>Ambient-Komponente je Zone konfigurieren.</summary>
        public static AmbientFx Ambient(Transform parent, WeatherKind weather, Vector2 wind, float windStrength, bool leaves, bool fireflies, bool butterflies, int birds)
        {
            var go = new GameObject("PT_AmbientFx"); go.transform.SetParent(parent, false);
            var a = go.AddComponent<AmbientFx>();
            a.Weather = weather; a.WindDir = wind; a.WindStrength = windStrength; a.Leaves = leaves; a.Fireflies = fireflies; a.Butterflies = butterflies; a.Birds = birds;
            a.ParticleAdditive = AmbientMat("Glow", false); a.ParticleAlpha = AmbientMat("Smoke", true); a.LeafMat = AmbientMat("Leaf", true); a.FlakeMat = AmbientMat("Flake", false);
            a.BirdMesh = BirdMesh(); a.BirdMat = Mat("Bird", new Color(0.18f, 0.16f, 0.15f), 0.2f, null, 0.2f);
            return a;
        }

        /// <summary>Rauch aus Kaminen / Fackeln (Schleife, dezent).</summary>
        public static ParticleSystem Smoke(Transform parent, Vector3 localPos, bool fire)
        {
            var p = PaletteDefs().Find(x => x.Id == (fire ? VfxPalette.Feuer : VfxPalette.Schatten));
            var ps = PS(parent, fire ? "Fackel" : "Kaminrauch", fire ? AmbientMat("Ember", false) : AmbientMat("Smoke", true), fire ? 40 : 30,
                fire ? new Vector2(0.3f, 0.6f) : new Vector2(3f, 5f), fire ? new Vector2(0.5f, 1f) : new Vector2(0.4f, 0.8f), fire ? new Vector2(0.12f, 0.25f) : new Vector2(0.6f, 1.3f), p, 1f, true);
            ps.transform.localPosition = localPos;
            var main = ps.main; main.playOnAwake = true; main.gravityModifier = fire ? -0.2f : -0.03f;
            if (!fire) main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.85f, 0.85f, 0.45f), new Color(0.65f, 0.65f, 0.68f, 0.35f));
            Rate(ps, fire ? 30f : 6f); Shape(ps, ParticleSystemShapeType.Cone, 0.1f, 8f, new Vector3(-90, 0, 0));
            return ps;
        }

        // ------------------------------------------------------------ Bausteine
        static ParticleSystem PS(Transform parent, string name, Material mat, int max, Vector2 life, Vector2 speed, Vector2 size, VfxCatalog.PaletteDef p, float duration, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = Mathf.Max(0.05f, duration); main.loop = loop; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y); main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x * p.Speed, speed.y * p.Speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x * 1.3f, size.y * 1.3f);   // Lesbarkeit aus Spielkamera-Distanz (QA M23) main.maxParticles = max;
            main.startColor = new ParticleSystem.MinMaxGradient(p.A, p.B); main.gravityModifier = p.Gravity;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission; em.rateOverTime = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1, 0.4f)));
            if (p.Turbulence > 0.05f) { var nz = ps.noise; nz.enabled = true; nz.strength = p.Turbulence * 0.6f; nz.frequency = 0.8f; nz.scrollSpeed = 0.4f; nz.quality = ParticleSystemNoiseQuality.Low; }
            if (p.Rise > 0.05f) { var f = ps.forceOverLifetime; f.enabled = true; f.y = p.Rise; }
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.sortingFudge = 0; r.maxParticleSize = 2f;
            return ps;
        }

        static void Burst(ParticleSystem ps, float n) { var em = ps.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, Mathf.Max(1f, n)) }); }
        static void Rate(ParticleSystem ps, float n) { var em = ps.emission; em.rateOverTime = n; }
        static void Shape(ParticleSystem ps, ParticleSystemShapeType t, float radius, float angle = 25f, Vector3? rot = null)
        {
            var sh = ps.shape; sh.enabled = true; sh.shapeType = t; sh.radius = radius; sh.angle = angle; if (rot.HasValue) sh.rotation = rot.Value;
        }
        static void Stretch(ParticleSystem ps, float k) { var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 1f; r.velocityScale = k; }

        static MeshRenderer MeshFx(Transform parent, string name, Mesh mesh, Material mat, Vector3 scale, Vector3 pos = default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return r;
        }

        static LineRenderer Bolt(Transform parent, VfxCatalog.PaletteDef p, float len)
        {
            var go = new GameObject("Bolt"); go.transform.SetParent(parent, false);
            var l = go.AddComponent<LineRenderer>(); l.useWorldSpace = false; l.positionCount = 9; l.widthMultiplier = 0.08f;
            l.sharedMaterial = MMat(p, "Beam", 0f, new Vector4(2f, 0, 0, 0)); l.startColor = p.A; l.endColor = p.Light;
            l.shadowCastingMode = ShadowCastingMode.Off; l.receiveShadows = false;
            for (int i = 0; i < 9; i++) l.SetPosition(i, Vector3.forward * len * i / 8f);
            return l;
        }

        static void AreaTag(Transform root, string child, AreaLayer layer)
        {
            var c = root.Find(child); if (c == null) return;
            var tag = c.gameObject.GetComponent<VfxAreaLayer>(); if (tag == null) tag = c.gameObject.AddComponent<VfxAreaLayer>(); tag.Layer = layer;
        }

        /// <summary>Eine Komposition bauen (Form × Palette × Größe).</summary>
        public static GameObject Build(VfxShape shape, VfxCatalog.PaletteDef p, VfxSize size, out float duration, out bool loop)
        {
            float s = Scale(size), k = CountK(size);
            var root = new GameObject(shape + "_" + p.Id + "_" + size);
            var inst = root.AddComponent<VfxInstance>();
            var t = root.transform;
            var main = PMat(p, p.Particle, p.Alpha); var sec = PMat(p, p.Secondary, p.Alpha && p.Secondary == "Smoke"); var glow = PMat(p, "Glow", false);
            var meshes = new List<Renderer>(); var bolts = new List<LineRenderer>();
            duration = 1f; loop = false;
            inst.UseLight = size >= VfxSize.M; inst.LightColor = p.Light; inst.LightRange = 3f + 3f * s; inst.LightIntensity = 1.2f + 0.6f * s;
            switch (shape)
            {
                case VfxShape.Charge:
                {
                    duration = 0.9f;
                    var core = PS(t, "Core", glow, 4, new Vector2(0.8f, 0.9f), Vector2.zero, new Vector2(0.6f, 0.75f) * s, p, duration, false); Burst(core, 2);
                    var conv = PS(t, "Converge", sec, (int)(40 * k), new Vector2(0.35f, 0.5f), new Vector2(-2.2f, -1.4f) * s, new Vector2(0.05f, 0.12f) * s, p, duration, false);
                    Rate(conv, 40 * k); Shape(conv, ParticleSystemShapeType.Sphere, 0.7f * s); var m2 = conv.main; m2.gravityModifier = 0f;
                    break;
                }
                case VfxShape.Release:
                {
                    duration = 0.5f;
                    var flash = PS(t, "Flash", glow, 2, new Vector2(0.15f, 0.2f), Vector2.zero, new Vector2(1.1f, 1.3f) * s, p, duration, false); Burst(flash, 1);
                    var burst = PS(t, "Burst", sec, (int)(36 * k), new Vector2(0.25f, 0.5f), new Vector2(3f, 7f) * s, new Vector2(0.06f, 0.14f) * s, p, duration, false);
                    Burst(burst, 30 * k); Shape(burst, ParticleSystemShapeType.Cone, 0.1f, 28f); Stretch(burst, 0.06f);
                    if (p.Lightning) bolts.Add(Bolt(t, p, 2.5f * s));
                    break;
                }
                case VfxShape.Projectile:
                {
                    duration = 2.5f; loop = true;
                    var core = PS(t, "Core", glow, (int)(30 * k), new Vector2(0.18f, 0.25f), Vector2.zero, new Vector2(0.35f, 0.5f) * s, p, 1f, true); Rate(core, 40);
                    var trail = PS(t, "Trail", main, (int)(60 * k), new Vector2(0.3f, 0.5f), new Vector2(0.1f, 0.6f), new Vector2(0.08f, 0.18f) * s, p, 1f, true); Rate(trail, 60 * k); Shape(trail, ParticleSystemShapeType.Sphere, 0.12f * s);
                    inst.UseLight = true;
                    break;
                }
                case VfxShape.Beam:
                {
                    duration = 0.8f;
                    var l = new GameObject("Beam").AddComponent<LineRenderer>(); l.transform.SetParent(t, false); l.useWorldSpace = false; l.positionCount = 2;
                    l.SetPosition(0, Vector3.zero); l.SetPosition(1, Vector3.forward * 7f * s); l.widthMultiplier = 0.35f * s;
                    l.sharedMaterial = MMat(p, "Beam", 0f, new Vector4(-3f, 0, 0, 0)); l.startColor = p.A; l.endColor = p.B; l.shadowCastingMode = ShadowCastingMode.Off;
                    meshes.Add(l);
                    var sp = PS(t, "Sparks", sec, (int)(40 * k), new Vector2(0.2f, 0.4f), new Vector2(0.5f, 2f), new Vector2(0.05f, 0.1f) * s, p, duration, false);
                    Rate(sp, 50 * k); Shape(sp, ParticleSystemShapeType.Box, 0f); var sh = sp.shape; sh.scale = new Vector3(0.2f, 0.2f, 7f * s); sh.position = Vector3.forward * 3.5f * s;
                    if (p.Lightning) bolts.Add(Bolt(t, p, 7f * s));
                    inst.MeshScaleFrom = new Vector3(1f, 1f, 1f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.7f;
                    break;
                }
                case VfxShape.Cone:
                {
                    duration = 0.6f;
                    var c = PS(t, "Cone", main, (int)(70 * k), new Vector2(0.3f, 0.55f), new Vector2(5f, 9f) * s, new Vector2(0.15f, 0.35f) * s, p, duration, false);
                    Burst(c, 60 * k); Shape(c, ParticleSystemShapeType.Cone, 0.15f, 35f);
                    var sp = PS(t, "Sparks", sec, (int)(30 * k), new Vector2(0.25f, 0.45f), new Vector2(6f, 10f) * s, new Vector2(0.04f, 0.08f) * s, p, duration, false);
                    Burst(sp, 24 * k); Shape(sp, ParticleSystemShapeType.Cone, 0.1f, 30f); Stretch(sp, 0.05f);
                    break;
                }
                case VfxShape.GroundCircle:
                {
                    duration = 2.6f;
                    meshes.Add(MeshFx(t, "Ring", _ring, MMat(p, "Beam", 0f, new Vector4(0.2f, 0, 0, 0)), Vector3.one * 2f * s, Vector3.up * 0.06f));
                    var up = PS(t, "Rise", main, (int)(50 * k), new Vector2(0.6f, 1.1f), new Vector2(0.4f, 1.2f), new Vector2(0.08f, 0.2f) * s, p, duration, false);
                    Rate(up, 25 * k); Shape(up, ParticleSystemShapeType.Circle, 1.9f * s, 0f, new Vector3(-90, 0, 0)); var m2 = up.main; m2.gravityModifier = -0.15f;
                    inst.MeshScaleFrom = new Vector3(0.3f, 1f, 0.3f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.75f;
                    break;
                }
                case VfxShape.Pillar:
                {
                    duration = 1.2f;
                    meshes.Add(MeshFx(t, "Column", _cyl, MMat(p, "Beam", 0f, new Vector4(0, 1.5f, 0, 0.3f)), new Vector3(1.4f, 4f, 1.4f) * s));
                    var up = PS(t, "Rise", main, (int)(60 * k), new Vector2(0.5f, 0.9f), new Vector2(3f, 6f) * s, new Vector2(0.1f, 0.25f) * s, p, duration, false);
                    Rate(up, 70 * k); Shape(up, ParticleSystemShapeType.Circle, 0.6f * s, 0f, new Vector3(-90, 0, 0)); var m2 = up.main; m2.gravityModifier = 0f;
                    if (p.Lightning) { var b = Bolt(t, p, 4f * s); b.transform.localRotation = Quaternion.Euler(-90, 0, 0); bolts.Add(b); }
                    inst.MeshScaleFrom = new Vector3(0.3f, 0.1f, 0.3f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.55f;
                    break;
                }
                case VfxShape.Shockwave:
                {
                    duration = 0.7f;
                    meshes.Add(MeshFx(t, "Wave", _ring, MMat(p, "Beam", 0f, new Vector4(0.5f, 0, 0, 0)), Vector3.one * 3f * s, Vector3.up * 0.1f));
                    var dust = PS(t, "Dust", PMat(p, p.Alpha ? "Smoke" : "Glow", p.Alpha), (int)(50 * k), new Vector2(0.4f, 0.7f), new Vector2(4f, 7f) * s, new Vector2(0.25f, 0.5f) * s, p, duration, false);
                    Burst(dust, 40 * k); Shape(dust, ParticleSystemShapeType.Circle, 0.4f * s, 0f, new Vector3(-90, 0, 0)); var m2 = dust.main; m2.gravityModifier = 0f;
                    var d = MeshFx(t, "Distort", _quad, AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_FX_Distort.mat") ?? FxMat("Distort", "ProjectTyrant/FX/Distort", Color.white), Vector3.one * 3f * s, Vector3.up * 0.15f);
                    d.sharedMaterial.SetTexture("_NoiseTex", FxTextureKit.Get("Noise")); meshes.Add(d);
                    inst.MeshScaleFrom = new Vector3(0.15f, 1f, 0.15f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.4f;
                    break;
                }
                case VfxShape.Aura:
                {
                    duration = 2.8f;
                    var domeMat = MMat(p, "Smoke", 1.5f, new Vector4(0, 0, 0, 0.2f));   // kein UV-Scroll: flache Farbe aus der Glow-Mitte
                    domeMat.SetTexture("_MainTex", FxTextureKit.Get("Glow")); domeMat.SetTextureScale("_MainTex", Vector2.zero); domeMat.SetTextureOffset("_MainTex", new Vector2(0.5f, 0.5f));   // flache Farbe, Fresnel formt den Rand
                    meshes.Add(MeshFx(t, "Dome", _dome, domeMat, Vector3.one * 1.3f * s));
                    var orb = PS(t, "Orbit", sec, (int)(40 * k), new Vector2(0.8f, 1.2f), Vector2.zero, new Vector2(0.06f, 0.12f) * s, p, duration, false);
                    Rate(orb, 18 * k); Shape(orb, ParticleSystemShapeType.Circle, 0.9f * s, 0f, new Vector3(-90, 0, 0));
                    var vel = orb.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.orbitalY = 2.5f; vel.y = 0.6f;
                    var m2 = orb.main; m2.simulationSpace = ParticleSystemSimulationSpace.Local; m2.gravityModifier = 0f;
                    inst.MeshScaleFrom = new Vector3(0.6f, 0.6f, 0.6f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.8f;
                    break;
                }
                case VfxShape.Impact:
                {
                    duration = 0.5f;
                    var flash = PS(t, "Flash", glow, 2, new Vector2(0.1f, 0.14f), Vector2.zero, new Vector2(0.8f, 1f) * s, p, duration, false); Burst(flash, 1);
                    var sp = PS(t, "Sparks", sec, (int)(36 * k), new Vector2(0.2f, 0.45f), new Vector2(3f, 8f) * s, new Vector2(0.04f, 0.1f) * s, p, duration, false);
                    Burst(sp, 26 * k); Shape(sp, ParticleSystemShapeType.Sphere, 0.15f); Stretch(sp, 0.05f);
                    var bits = PS(t, "Bits", main, (int)(16 * k), new Vector2(0.3f, 0.6f), new Vector2(1.5f, 3.5f) * s, new Vector2(0.08f, 0.16f) * s, p, duration, false);
                    Burst(bits, 12 * k); Shape(bits, ParticleSystemShapeType.Hemisphere, 0.2f, 0f, new Vector3(-90, 0, 0));
                    if (p.Lightning) bolts.Add(Bolt(t, p, 1.2f * s));
                    break;
                }
                case VfxShape.Trail:
                {
                    duration = 1.2f; loop = true;
                    var tr = PS(t, "Trail", main, (int)(50 * k), new Vector2(0.25f, 0.45f), Vector2.zero, new Vector2(0.08f, 0.16f) * s, p, 1f, true);
                    var em = tr.emission; em.rateOverDistance = 30f * k; Shape(tr, ParticleSystemShapeType.Sphere, 0.08f * s);
                    inst.UseLight = false;
                    break;
                }
                case VfxShape.Sigil:
                {
                    duration = 1.6f;
                    meshes.Add(MeshFx(t, "Sigil", _quad, MMat(p, "Sigil", 0f, Vector4.zero), Vector3.one * 1.6f * s, Vector3.up * 0.07f));
                    var g = PS(t, "Glimmer", glow, (int)(30 * k), new Vector2(0.4f, 0.8f), new Vector2(0.2f, 0.8f), new Vector2(0.06f, 0.12f) * s, p, duration, false);
                    Rate(g, 20 * k); Shape(g, ParticleSystemShapeType.Circle, 1.5f * s, 0f, new Vector3(-90, 0, 0));
                    inst.MeshScaleFrom = new Vector3(0.4f, 1f, 0.4f); inst.MeshScaleTo = Vector3.one; inst.DissolveFrom = 0.7f;
                    break;
                }
                case VfxShape.Veil:
                {
                    duration = 2.6f;
                    var sm = PS(t, "Veil", PMat(p, "Smoke", true), (int)(60 * k), new Vector2(1f, 1.6f), new Vector2(0.2f, 0.6f), new Vector2(0.6f, 1.1f) * s, p, duration, false);
                    Rate(sm, 30 * k); Shape(sm, ParticleSystemShapeType.Box, 0f); var sh = sm.shape; sh.scale = new Vector3(2f, 0.3f, 2f) * s;
                    var m2 = sm.main; m2.gravityModifier = -0.05f;
                    break;
                }
            }
            inst.Duration = duration; inst.Loop = loop;
            inst.MeshFx = meshes.ToArray(); inst.Bolts = bolts.ToArray(); inst.BoltLength = shape == VfxShape.Beam ? 7f * s : shape == VfxShape.Pillar ? 4f * s : 2f * s;
            // Budget: Summe maxParticles ≤ Budget der Größe (Kürzung proportional)
            int budget = VfxCatalog.Budget(size), sum = 0; var all = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in all) sum += ps.main.maxParticles;
            if (sum > budget) foreach (var ps in all) { var m = ps.main; m.maxParticles = Mathf.Max(1, Mathf.FloorToInt(m.maxParticles * budget / (float)sum)); }
            // Fix-Lauf F1: Boden-Kompositionen tragen zwei Ebenen (Flächenrahmen / Effekt)
            if (shape == VfxShape.GroundCircle) { AreaTag(t, "Ring", AreaLayer.AreaOutline); AreaTag(t, "Rise", AreaLayer.AreaEffect); }
            if (shape == VfxShape.Sigil) { AreaTag(t, "Sigil", AreaLayer.AreaOutline); AreaTag(t, "Glimmer", AreaLayer.AreaEffect); }
            return root;
        }
    }
}
