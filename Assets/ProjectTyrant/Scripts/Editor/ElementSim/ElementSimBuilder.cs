using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectTyrant.Visuals.ElementSim;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// VFX-Lauf V1: Rezepte anlegen und Flipbooks backen (Teil von Rebuild Everything). Inkrementell über den Parameter-Hash:
    /// nur geänderte Rezepte werden neu gerechnet. Jede Bake-Ausgabe wird geprüft (Budget, Naht, Hash).
    /// </summary>
    public static class ElementSimBuilder
    {
        public const string Dir = "Assets/ProjectTyrant/Generated/Flipbooks";
        public static readonly List<string> Report = new List<string>();
        public static int BakedLast, SkippedLast;

        static Gradient Ramp(params (float t, Color c)[] keys)
        {
            var g = new Gradient();
            g.SetKeys(keys.Select(k => new GradientColorKey(k.c, k.t)).ToArray(), new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        static readonly Gradient FireRamp = Ramp((0f, new Color(0.18f, 0.05f, 0.12f)), (0.35f, new Color(0.85f, 0.2f, 0.04f)), (0.7f, new Color(1f, 0.6f, 0.12f)), (1f, new Color(1f, 0.97f, 0.8f)));
        static readonly Gradient SmokeRamp = Ramp((0f, new Color(0.12f, 0.11f, 0.12f)), (1f, new Color(0.55f, 0.53f, 0.52f)));
        static readonly Gradient WaterRamp = Ramp((0f, new Color(0.1f, 0.35f, 0.65f)), (0.6f, new Color(0.35f, 0.8f, 0.9f)), (1f, new Color(0.95f, 1f, 1f)));

        /// <summary>Basis-Rezepte: je Modus ein Test-Flipbook, dazu die Leuchtturm-Grundformen für Feuer, Rauch und Wasser.</summary>
        public static List<SimRecipe> DefaultRecipes()
        {
            var list = new List<SimRecipe>();
            SimRecipe R(string id, string element, FlipbookMode mode, int seed, Gradient ramp, bool temp)
            {
                var r = ScriptableObject.CreateInstance<SimRecipe>();
                r.Id = id; r.Element = element; r.Mode = mode; r.Seed = seed; r.Ramp = ramp; r.ColorFromTemperature = temp;
                r.Grid = 128; r.Columns = 8; r.Rows = 8; r.TileSize = 128; r.StepsPerFrame = 1; r.Dt = mode == FlipbookMode.Loop ? 1f / 60f : 1f / 48f;   // Loops feiner, damit die Naht ≤ 2 % bleibt
                r.WarmupSteps = mode == FlipbookMode.Loop ? 60 : 0; list.Add(r); return r;
            }
            var t = R("v1_test_burst", "Test", FlipbookMode.Burst, 11, FireRamp, true);
            t.Buoyancy = 2f; t.Vorticity = 3f; t.Emitters.Add(new SimEmitter { Y = 0.15f, Radius = 0.1f, Density = 25f, Temperature = 25f, VelY = 0.6f, End = 0.3f });
            var tl = R("v1_test_loop", "Test", FlipbookMode.Loop, 12, SmokeRamp, false);
            tl.Buoyancy = 1.2f; tl.Vorticity = 2f; tl.Emitters.Add(new SimEmitter { Y = 0.12f, Radius = 0.12f, Density = 8f, Temperature = 10f, VelY = 0.3f });

            var flames = R("feuer_flammen_loop", "Feuer", FlipbookMode.Loop, 21, FireRamp, true);
            flames.Dt = 1f / 75f; flames.Buoyancy = 2.6f;   // schnelle Zungen: feinere Bildfolge, sonst Naht > 2 %
            flames.Buoyancy = 2.6f; flames.Vorticity = 6f; flames.Cooling = 1.6f; flames.DensityFade = 0.9f; flames.Exposure = 1.1f; flames.CoreThreshold = 0.75f;
            flames.Emitters.Add(new SimEmitter { Shape = EmitterShape.Line, X = 0.35f, X2 = 0.65f, Y = 0.08f, Y2 = 0.08f, Radius = 0.06f, Density = 20f, Temperature = 35f, VelY = 0.7f, Jitter = 0.6f });
            var boom = R("feuer_explosion_burst", "Feuer", FlipbookMode.Burst, 22, FireRamp, true);
            boom.Buoyancy = 1.8f; boom.Vorticity = 5f; boom.Cooling = 1.1f; boom.DensityFade = 0.5f;
            boom.Emitters.Add(new SimEmitter { Shape = EmitterShape.Ring, X = 0.5f, Y = 0.4f, X2 = 0.56f, Radius = 0.08f, Density = 40f, Temperature = 45f, VelY = 0.4f, End = 0.12f, Jitter = 0.5f });
            var smoke = R("rauch_loop", "Feuer", FlipbookMode.Loop, 23, SmokeRamp, false);
            smoke.Buoyancy = 0.9f; smoke.Vorticity = 2.5f; smoke.Cooling = 0.3f; smoke.DensityFade = 0.25f; smoke.AlphaScale = 1.2f;
            smoke.Emitters.Add(new SimEmitter { Y = 0.1f, Radius = 0.14f, Density = 6f, Temperature = 6f, VelY = 0.25f, Jitter = 0.4f });

            var splash = R("wasser_spritzer_burst", "Wasser", FlipbookMode.Burst, 31, WaterRamp, false);
            splash.Buoyancy = 0f; splash.Gravity = 5f; splash.Weight = 0f; splash.FloorHeight = 0.1f; splash.DensityFade = 0.4f; splash.CoreThreshold = 0.85f; splash.AlphaScale = 0.8f;
            splash.Emitters.Add(new SimEmitter { X = 0.5f, Y = 0.18f, Radius = 0.08f, Density = 35f, VelY = 2.2f, End = 0.1f, Jitter = 0.7f });
            var spray = R("wasser_gischt_loop", "Wasser", FlipbookMode.Loop, 32, WaterRamp, false);
            spray.Buoyancy = 0f; spray.Gravity = 2f; spray.Weight = 0f; spray.DensityFade = 0.8f; spray.AlphaScale = 0.55f; spray.Exposure = 0.45f;   // Gischt bleibt durchscheinend
            spray.Emitters.Add(new SimEmitter { Shape = EmitterShape.Line, X = 0.2f, X2 = 0.8f, Y = 0.15f, Y2 = 0.15f, Radius = 0.05f, Density = 10f, VelY = 1.2f, Jitter = 0.7f });
            var side = R("wasser_spritzer_seitlich_burst", "Wasser", FlipbookMode.Burst, 33, WaterRamp, false);
            side.Buoyancy = 0f; side.Gravity = 4f; side.Weight = 0f; side.FloorHeight = 0.08f; side.DensityFade = 0.5f; side.CoreThreshold = 0.85f; side.AlphaScale = 0.8f;
            side.Emitters.Add(new SimEmitter { X = 0.2f, Y = 0.3f, Radius = 0.08f, Density = 30f, VelX = 1.8f, VelY = 1.2f, End = 0.1f, Jitter = 0.6f });
            return list;
        }

        [MenuItem("Project Tyrant/VFX V1/Flipbooks backen (inkrementell)")]
        public static void BakeMenu() => Debug.Log("[ProjectTyrant] V1 Flipbooks: " + BakeAll(false));

        [MenuItem("Project Tyrant/VFX V1/Flipbooks neu backen (alle)")]
        public static void BakeForceMenu() => Debug.Log("[ProjectTyrant] V1 Flipbooks: " + BakeAll(true));

        /// <summary>Legt fehlende Rezepte an und backt alle, deren Parameter sich geändert haben (oder alle bei force).</summary>
        public static string BakeAll(bool force)
        {
            Report.Clear(); EnsureFolder("Assets/ProjectTyrant/Generated"); EnsureFolder(Dir);
            foreach (var r in DefaultRecipes())
            {
                string path = Dir + "/" + r.Id + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<SimRecipe>(path);
                if (existing == null) AssetDatabase.CreateAsset(r, path);
                else
                {
                    // Builder-Werte gewinnen (Rebuild erzeugt dasselbe); Ergebnis-Hashes bleiben für das inkrementelle Backen erhalten
                    string ph = existing.ParamHash, rh = existing.ResultHash; float seam = existing.LoopSeam, peak = existing.PeakTime;
                    EditorUtility.CopySerialized(r, existing); existing.name = r.Id;
                    existing.ParamHash = ph; existing.ResultHash = rh; existing.LoopSeam = seam; existing.PeakTime = peak;
                    EditorUtility.SetDirty(existing); Object.DestroyImmediate(r);
                }
            }
            BakedLast = 0; SkippedLast = 0;
            foreach (var r in AllRecipes())
            {
                foreach (var p in r.Problems()) Report.Add(r.Id + ": " + p);
                bool filesMissing = !File.Exists(TexPath(r, "farbe"));
                if (!force && !r.NeedsBake && !filesMissing) { SkippedLast++; continue; }
                Bake(r); BakedLast++;
            }
            BakeCaustics(force);
            BuildLibrary();
            AssetDatabase.SaveAssets();
            return BakedLast + " gebacken, " + SkippedLast + " unverändert, Texturbudget " + (EstimatedCompressedBytes() / (1024f * 1024f)).ToString("0.0") + " MB, " + Report.Count + " Hinweise";
        }

        public static IEnumerable<SimRecipe> AllRecipes() =>
            AssetDatabase.FindAssets("t:SimRecipe", new[] { Dir }).Select(g => AssetDatabase.LoadAssetAtPath<SimRecipe>(AssetDatabase.GUIDToAssetPath(g))).Where(r => r != null).OrderBy(r => r.Id);

        public static string TexPath(SimRecipe r, string kind) => Dir + "/" + r.Id + "_" + kind + ".png";

        public static FlipbookResult Bake(SimRecipe r)
        {
            var res = FlipbookBaker.Bake(r);
            Write(TexPath(r, "farbe"), res.Color, res.AtlasSize, true);
            Write(TexPath(r, "normalen"), res.Normal, res.AtlasSize, false);
            Write(TexPath(r, "emissiv"), res.Emissive, res.AtlasSize, false);
            Write(TexPath(r, "bewegung"), res.Motion, res.MotionAtlasSize, false);
            r.ParamHash = r.ComputeParamHash(); r.ResultHash = Fnv.Hex(res.Hash); r.LoopSeam = res.LoopSeam; r.PeakTime = res.PeakTime;
            if (r.Mode == FlipbookMode.Loop && res.LoopSeam > 0.02f) Report.Add(r.Id + ": Naht " + (res.LoopSeam * 100f).ToString("0.0") + " % > 2 %");
            EditorUtility.SetDirty(r);
            return res;
        }

        static void Write(string path, Color32[] px, int size, bool srgb)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, !srgb);
            tex.SetPixels32(px); tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                bool changed = ti.sRGBTexture != srgb || ti.mipmapEnabled || ti.wrapMode != TextureWrapMode.Clamp;
                ti.sRGBTexture = srgb; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.alphaIsTransparency = srgb;
                ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.maxTextureSize = 2048;
                if (changed) ti.SaveAndReimport();
            }
        }

        public const string CausticId = "wasser_kaustik_loop";
        public const string MatDir = Dir + "/Materialien";
        public const string LibraryPath = Dir + "/ElementFxLibrary.asset";

        /// <summary>Kaustik-Loop (8 × 8 Kacheln à 128 px) aus dem Kaustik-Generator; nahtlos, weil die Wellen zeitlich periodisch sind.</summary>
        public static void BakeCaustics(bool force)
        {
            string path = Dir + "/" + CausticId + "_farbe.png";
            if (!force && File.Exists(path)) return;
            const int tile = 128, cols = 8; int atlas = tile * cols; var px = new Color32[atlas * atlas];
            for (int f = 0; f < 64; f++)
            {
                var d = CausticGenerator.Frame(tile, f / 64f, 41);
                int cx = f % cols, cy = cols - 1 - f / cols;
                for (int y = 0; y < tile; y++) for (int x = 0; x < tile; x++)
                    {
                        float v = d[x + y * tile];
                        px[cx * tile + x + (cy * tile + y) * atlas] = new Color(0.75f + 0.25f * v, 0.95f, 1f, v * v);
                    }
            }
            Write(path, px, atlas, true);
        }

        static Material MatAsset(string name, Shader sh)
        {
            EnsureFolder(MatDir); string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            return m;
        }

        static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        /// <summary>Laufzeit-Bibliothek: Flipbook-Materialien je Rezept, Element-Materialien, GPU-Vorlagen; Verknüpfung in der GraphicsLibrary.</summary>
        public static ElementFxLibrary BuildLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<ElementFxLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<ElementFxLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            var fbShader = Shader.Find("ProjectTyrant/FX/Flipbook"); var meshShader = Shader.Find("ProjectTyrant/FX/ElementMesh"); var refrShader = Shader.Find("ProjectTyrant/FX/Refract");
            var noise = GraphicsBuilder.Tex("Noise_Perlin");
            lib.Flipbooks.Clear();
            foreach (var r in AllRecipes())
            {
                var m = MatAsset("FB_" + r.Id, fbShader);
                m.SetTexture("_MainTex", Tex(TexPath(r, "farbe"))); m.SetTexture("_NormalTex", Tex(TexPath(r, "normalen")));
                m.SetTexture("_MotionTex", Tex(TexPath(r, "bewegung"))); m.SetTexture("_EmissiveTex", Tex(TexPath(r, "emissiv")));
                m.SetFloat("_Columns", r.Columns); m.SetFloat("_Rows", r.Rows); m.SetFloat("_Fps", r.PlaybackFps); m.SetFloat("_Loop", r.Mode == FlipbookMode.Loop ? 1f : 0f);
                bool fire = r.ColorFromTemperature; m.SetFloat("_Additive", fire ? 1f : 0f); m.SetFloat("_Lighting", fire ? 0.1f : 0.7f); m.SetFloat("_CoreIntensity", fire ? 3f : 2.2f);
                m.SetFloat("_MotionStrength", 0.02f);
                m.SetColor("_Tint", r.Element == "Wasser" ? new Color(0.85f, 0.95f, 1f, r.Mode == FlipbookMode.Loop ? 0.4f : 0.65f) : Color.white);   // Wasser durchsichtig (Brechung ist seine Signatur), nie eine weiße Fläche
                EditorUtility.SetDirty(m);
                lib.Flipbooks.Add(new ElementFxLibrary.FlipbookEntry { Id = r.Element.ToLowerInvariant() + "/" + r.Id, Material = m, Loop = r.Mode == FlipbookMode.Loop, Fps = r.PlaybackFps, PeakTime = r.PeakTime });
            }
            var ca = MatAsset("FB_" + CausticId, fbShader);
            ca.SetTexture("_MainTex", Tex(Dir + "/" + CausticId + "_farbe.png")); ca.SetTexture("_NormalTex", null); ca.SetTexture("_MotionTex", null); ca.SetTexture("_EmissiveTex", null);
            ca.SetFloat("_Columns", 8); ca.SetFloat("_Rows", 8); ca.SetFloat("_Fps", 24); ca.SetFloat("_Loop", 1); ca.SetFloat("_Additive", 1); ca.SetFloat("_Lighting", 0); ca.SetFloat("_CoreIntensity", 0); ca.SetFloat("_MotionStrength", 0);
            ca.SetColor("_Tint", new Color(0.55f, 0.7f, 0.75f, 0.55f));   // Kaustik additiv: dezent, sonst überstrahlt heller Boden EditorUtility.SetDirty(ca);
            lib.Flipbooks.Add(new ElementFxLibrary.FlipbookEntry { Id = "wasser/" + CausticId, Material = ca, Loop = true, Fps = 24 });

            lib.Materials.Clear();
            var wm = MatAsset("Wasser_Mesh", meshShader); wm.SetColor("_Body", new Color(0.25f, 0.62f, 0.95f, 0.35f)); wm.SetFloat("_FoamStart", 0.88f); wm.SetColor("_Halo", new Color(0.35f, 0.9f, 0.85f)); wm.SetColor("_Foam", new Color(1.3f, 1.5f, 1.7f)); wm.SetFloat("_Mode", 0); wm.SetTexture("_NoiseTex", noise);
            var wp = MatAsset("Wasser_Pfuetze", meshShader); wp.SetColor("_Body", new Color(0.2f, 0.35f, 0.5f, 1f)); wp.SetFloat("_Mode", 1); wp.SetTexture("_NoiseTex", noise);
            var wn = MatAsset("Wasser_Nass", meshShader); wn.SetFloat("_Mode", 2); wn.SetTexture("_NoiseTex", noise);
            var rf = MatAsset("Brechung_Stark", refrShader); rf.SetFloat("_Strength", 24f); rf.SetFloat("_Dispersion", 0.6f); rf.SetFloat("_EdgePower", 1.6f); rf.SetFloat("_Center", 0.2f);   // Auftraggeber: Brechung „stark“ → Obergrenze 24 px
            var rw = MatAsset("Wasser_Brechung", refrShader); rw.SetFloat("_Strength", 20f); rw.SetFloat("_Dispersion", 0.35f); rw.SetFloat("_NoiseAmount", 0.5f); rw.SetTexture("_NoiseTex", noise);
            foreach (var m in new[] { wm, wp, wn, rf, rw }) EditorUtility.SetDirty(m);
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "wasser", Key = "Mesh", Material = wm });
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "wasser", Key = "Pfuetze", Material = wp });
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "wasser", Key = "Nass", Material = wn });
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "wasser", Key = "Refract", Material = rw });
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "", Key = "Refract", Material = rf });

            lib.GpuPresets.Clear();
            lib.GpuPresets.Add(new ElementFxLibrary.GpuPreset { Id = "wasser/sammeln", Duration = 0.25f, Settings = new GpuParticleSettings
                { Capacity = 2048, EmitPerSecond = 2500f, Shape = GpuEmitterShape.Sphere, ShapeRadius = 1.1f, Speed = 0.2f, LifeMin = 0.25f, LifeMax = 0.4f, SizeStart = 0.04f, SizeEnd = 0.015f,
                  Gravity = Vector3.zero, Attract = 9f, Drag = 1f, CollideDepth = false, ColorStart = new Color(0.5f, 0.8f, 1.1f, 0.35f), ColorEnd = new Color(0.9f, 1.1f, 1.4f, 0f) } });
            lib.GpuPresets.Add(new ElementFxLibrary.GpuPreset { Id = "wasser/tropfen", Duration = 0.15f, Settings = new GpuParticleSettings
                { Capacity = 4096, EmitPerSecond = 0f, Burst = 350f, Shape = GpuEmitterShape.Cone, ConeAngle = 55f, Speed = 4.5f, SpeedJitter = 0.5f, LifeMin = 0.8f, LifeMax = 1.4f, SizeStart = 0.035f, SizeEnd = 0.02f,
                  Gravity = new Vector3(0, -9.81f, 0), Drag = 0.2f, CollideDepth = true, Bounce = 0.15f, StickBelowSpeed = 1.2f, Render = GpuRenderMode.Stretched,
                  ColorStart = new Color(0.7f, 0.95f, 1.2f, 0.5f), ColorEnd = new Color(0.4f, 0.7f, 1f, 0f) } });
            lib.GpuPresets.Add(new ElementFxLibrary.GpuPreset { Id = "wasser/gischt", Duration = 0.8f, Settings = new GpuParticleSettings
                { Capacity = 2048, EmitPerSecond = 500f, Shape = GpuEmitterShape.RingEdge, ShapeRadius = 1.2f, Speed = 1.2f, SpeedJitter = 0.6f, LifeMin = 1.2f, LifeMax = 2.2f, SizeStart = 0.2f, SizeEnd = 0.45f,
                  Gravity = new Vector3(0, -0.6f, 0), Drag = 0.9f, CurlStrength = 1.2f, WindFactor = 0.6f, CollideDepth = false,
                  ColorStart = new Color(0.5f, 0.65f, 0.75f, 0.06f), ColorEnd = new Color(0.45f, 0.6f, 0.7f, 0f) } });   // additiv: viele Partikel addieren sich, daher sehr niedriges Alpha
            lib.ParticleCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ProjectTyrant/Shaders/Compute/PT_GpuParticles.compute");
            lib.ParticleMaterial = MatAsset("GpuPartikel", Shader.Find("ProjectTyrant/FX/GpuParticle"));
            EditorUtility.SetDirty(lib);

            var glib = AssetDatabase.LoadAssetAtPath<ProjectTyrant.Visuals.GraphicsLibrary>(GraphicsBuilder.LibraryPath);
            if (glib != null && glib.ElementFx != lib) { glib.ElementFx = lib; EditorUtility.SetDirty(glib); }
            return lib;
        }

        /// <summary>Komprimierte Größe aller Flipbooks (BC7/DXT5 ≈ 1 Byte je Pixel, ohne Mips).</summary>
        public static long EstimatedCompressedBytes()
        {
            long sum = 0;
            foreach (var r in AllRecipes()) { long a = r.AtlasSize; long m = r.Columns * System.Math.Max(8, r.TileSize / 2); sum += 3 * a * a + m * m; }
            return sum;
        }

        /// <summary>Abnahme V1: zwei Läufe → identischer Hash.</summary>
        public static bool VerifyDeterminism(SimRecipe r, out string a, out string b)
        {
            a = Fnv.Hex(FlipbookBaker.Bake(r).Hash); b = Fnv.Hex(FlipbookBaker.Bake(r).Hash); return a == b;
        }
    }
}
