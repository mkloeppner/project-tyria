using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G2/G3: hebt alle Material-Assets unter Assets/ProjectTyrant auf die Shader-Bibliothek 2.0 –
    /// ToonRim → ToonLit (gleiche Eigenschaften), URP/Lit → ToonLit (Farbe, Textur, Emission), URP-Partikel → PT/FX/Particle,
    /// GradientSkybox → PT/Sky. Danach darf kein Material mehr auf einem alten oder Unity-Shader liegen (<see cref="Validate"/>).
    /// </summary>
    public static class MaterialMigrator
    {
        public static readonly List<string> Report = new List<string>();

        [MenuItem("Project Tyrant/Grafik G2/Materialien migrieren")]
        public static int MigrateAll()
        {
            Report.Clear();
            var toon = Shader.Find(PTShaders.ToonLit); var sky = Shader.Find(PTShaders.Sky); var part = Shader.Find(PTShaders.FxParticle);
            if (toon == null || sky == null || part == null) { Report.Add("Zielshader fehlen"); return 0; }
            int n = StripModelMaterials();
            var counts = new Dictionary<string, int>();
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { PTEditorUtil.Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith(".mat")) continue;   // eingebettete Modell-Materialien: siehe StripModelMaterials
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;
                string from = m.shader.name;
                if (!Migrate(m, toon, sky, part)) continue;
                EditorUtility.SetDirty(m); n++;
                counts[from] = counts.TryGetValue(from, out int c) ? c + 1 : 1;
            }
            foreach (var kv in counts) Report.Add(kv.Key + " → " + kv.Value);
            AssetDatabase.SaveAssets();
            Report.Add(n + " Materialien migriert");
            Debug.Log("[ProjectTyrant] Material-Migration: " + string.Join(" · ", Report));
            return n;
        }

        /// <summary>
        /// Eingebettete Modell-Materialien (Meshy-FBX → URP/Lit) sind schreibgeschützt und ungenutzt: die normalisierten Prefabs
        /// setzen ToonLit-Materialien. Der Import erzeugt sie deshalb nicht mehr (Material-Import aus).
        /// </summary>
        public static int StripModelMaterials()
        {
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { PTEditorUtil.Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi) || mi.materialImportMode == ModelImporterMaterialImportMode.None) continue;
                bool embedded = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Any();
                bool overridden = System.IO.File.Exists(path.Substring(0, path.Length - System.IO.Path.GetExtension(path).Length) + "_Normalized.prefab");
                if (!embedded || !overridden) continue;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.SaveAndReimport(); n++;
            }
            if (n > 0) Report.Add(n + " Modelle ohne eingebettete Unity-Materialien");
            return n;
        }

        /// <summary>Toon-Familie nach Materialname (G3): Gelände, Vegetation, Stoff, Gefahr, Lava, Kristall, Glas – sonst ToonLit.</summary>
        public static string FamilyShaderFor(string materialName)
        {
            string n = (materialName ?? "").ToLowerInvariant();
            if (n.StartsWith("m_terrain")) return PTShaders.Terrain;
            if (n.Contains("w1_hazard")) return PTShaders.Hazard;
            if (n.Contains("vegetation") || n.Contains("_leaf") || n.EndsWith("grass") || n.Contains("foliage")) return PTShaders.Vegetation;
            if (n.Contains("banner") || n.Contains("sheltercloth") || n.Contains("scenecloth")) return PTShaders.Cloth;
            if (n.Contains("lava") && !n.Contains("orb")) return PTShaders.Lava;
            if (n.Contains("crystal")) return PTShaders.Crystal;
            if (n.EndsWith("_glass")) return PTShaders.Glass;
            return PTShaders.ToonLit;
        }

        /// <summary>Texturen und Werte, die die Familien-Shader zusätzlich brauchen (Rauschen aus TextureForge).</summary>
        public static void ApplyFamilyDefaults(Material m)
        {
            string s = m.shader.name, n = m.name.ToLowerInvariant();
            // G4: Laternen, Fenster, Fackeln leuchten nur in der Dämmerung und nachts
            if (m.HasProperty("_NightEmission") && (n.Contains("lantern") || n.Contains("laterne") || n.Contains("window") || n.Contains("fenster") || n.Contains("lamp") || n.Contains("torch") || n.Contains("fackel")))
            {
                m.SetFloat("_NightEmission", 1f);
                if (m.GetColor("_EmissionColor").maxColorComponent < 0.05f) m.SetColor("_EmissionColor", new Color(1.6f, 1.1f, 0.5f));
            }
            if (s == PTShaders.Terrain) { m.SetTexture("_VariationTex", GraphicsBuilder.Tex("Noise_Perlin")); m.SetFloat("_Variation", 0.08f); m.SetFloat("_PaintStrength", 0.15f); }
            else if (s == PTShaders.Lava) { m.SetTexture("_CrackTex", GraphicsBuilder.Tex("Noise_Worley")); m.SetFloat("_WeatherMask", 0f); m.SetFloat("_CrackScale", 0.03f); }
            else if (s == PTShaders.Hazard)
            {
                m.SetTexture("_PatternTex", GraphicsBuilder.Tex("Noise_Worley"));
                m.SetFloat("_Kind", n.Contains("lava") ? 0 : n.Contains("ice") ? 1 : n.Contains("toxic") || n.Contains("poison") ? 2 : n.Contains("sand") ? 3 : 4);
                m.SetFloat("_Edge", n.Contains("hazardedge") ? 1f : 0f); m.SetFloat("_WeatherMask", 0f);
            }
            else if (s == PTShaders.Vegetation) { if (m.GetFloat("_Wind") <= 0f) m.SetFloat("_Wind", 0.6f); }
            else if (s == PTShaders.Cloth) { if (m.GetFloat("_Wind") <= 0f) m.SetFloat("_Wind", 0.8f); m.SetFloat("_WeatherMask", 0.3f); }
            else if (s == PTShaders.Crystal) { var e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black; var b = m.GetColor("_BaseColor"); m.SetColor("_Glow", (e.maxColorComponent > 0.05f ? e : b) * 1.6f); m.SetFloat("_WeatherMask", 0f); }
            else if (s == PTShaders.Water2) { m.SetTexture("_NoiseTex", GraphicsBuilder.Tex("Noise_Simplex")); m.SetTexture("_CausticTex", GraphicsBuilder.Tex("Caustics")); }
            else if (s == PTShaders.Glass) { var b = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white; b.a = 0.35f; m.SetColor("_BaseColor", b); }
        }

        /// <summary>Glühende Wasserflächen (Lavaseen der Glut-Zonen) → Lava-Shader mit Kruste und Glut.</summary>
        public static void ToLava(Material m)
        {
            Color e = m.HasProperty("_Emission") ? m.GetColor("_Emission") : new Color(1f, 0.4f, 0.1f);
            Color deep = m.HasProperty("_DeepColor") ? m.GetColor("_DeepColor") : new Color(0.2f, 0.08f, 0.05f);
            m.shader = Shader.Find(PTShaders.Lava);
            m.SetColor("_BaseColor", Color.white); m.SetTexture("_BaseMap", PTEditorUtil.PainterlyTexture()); m.SetFloat("_PaintStrength", 0.1f);
            m.SetColor("_GlowColor", new Color(e.r, e.g, e.b, 1f) * 1.15f); m.SetColor("_CrustColor", deep * 0.45f); m.SetColor("_EmissionColor", Color.black);
            m.SetColor("_ShadowColor", new Color(0.4f, 0.3f, 0.45f)); m.SetFloat("_UseVertexColor", 0f); m.renderQueue = -1;
            ApplyFamilyDefaults(m);
        }

        public static bool Migrate(Material m, Shader toon, Shader sky, Shader part)
        {
            string s = m.shader.name;
            bool changed = false;
            if (s == "ProjectTyrant/ToonRim") { int q = m.renderQueue; m.shader = toon; m.renderQueue = q; s = toon.name; changed = true; }
            if (s == PTShaders.ToonLit)
            {
                string fam = FamilyShaderFor(m.name);
                if (fam == PTShaders.ToonLit) { float before = m.HasProperty("_NightEmission") ? m.GetFloat("_NightEmission") : 0f; ApplyFamilyDefaults(m); return changed || (m.HasProperty("_NightEmission") && m.GetFloat("_NightEmission") != before); }
                var fs = Shader.Find(fam); if (fs == null) return false;
                m.shader = fs; m.renderQueue = -1; ApplyFamilyDefaults(m);
                return true;
            }
            if ((s == "ProjectTyrant/Water" || s == PTShaders.Water2) && m.HasProperty("_Emission") && m.GetColor("_Emission").maxColorComponent > 0.5f) { ToLava(m); return true; }
            if (s == "ProjectTyrant/Water") { var w = Shader.Find(PTShaders.Water2); if (w == null) return false; m.shader = w; ApplyFamilyDefaults(m); return true; }
            if (s == "Universal Render Pipeline/Lit" || s == "Universal Render Pipeline/Simple Lit" || s == "Standard")
            {
                Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                Texture t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color e = m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION") ? m.GetColor("_EmissionColor") : Color.black;
                m.shader = toon; m.shaderKeywords = new string[0];
                m.SetColor("_BaseColor", c); if (t != null) m.SetTexture("_AlbedoMap", t);
                m.SetTexture("_BaseMap", PTEditorUtil.PainterlyTexture()); m.SetFloat("_PaintStrength", 0.15f);
                m.SetColor("_EmissionColor", e); m.SetColor("_ShadowColor", new Color(0.5f, 0.55f, 0.85f)); m.SetColor("_RimColor", new Color(1f, 0.88f, 0.65f));
                m.enableInstancing = true;
                return true;
            }
            if (s.StartsWith("Universal Render Pipeline/Particles/"))
            {
                Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                Texture t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                bool additive = m.HasProperty("_Blend") && (int)m.GetFloat("_Blend") == 2 || m.HasProperty("_DstBlend") && (int)m.GetFloat("_DstBlend") == (int)UnityEngine.Rendering.BlendMode.One;
                m.shader = part; m.shaderKeywords = new string[0];
                m.SetColor("_Color", c); if (t != null) m.SetTexture("_MainTex", t);
                m.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.renderQueue = 3000;
                return true;
            }
            if (s == "ProjectTyrant/GradientSkybox")
            {
                Color top = m.HasProperty("_TopColor") ? m.GetColor("_TopColor") : Color.cyan, hor = m.HasProperty("_HorizonColor") ? m.GetColor("_HorizonColor") : Color.white, bot = m.HasProperty("_BottomColor") ? m.GetColor("_BottomColor") : Color.gray;
                m.shader = sky;
                m.SetColor("_Zenith", top); m.SetColor("_Horizon", hor); m.SetColor("_Ground", bot);
                m.SetTexture("_CloudTex", GraphicsBuilder.Tex("Clouds")); m.SetTexture("_StarTex", GraphicsBuilder.Tex("Starfield"));
                return true;
            }
            return false;
        }

        /// <summary>Materialien, die noch auf alten oder Unity-Shadern liegen (Pfad → Shader). Leer = bestanden.</summary>
        public static List<string> Validate()
        {
            var bad = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { PTEditorUtil.Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) continue;
                if (PTShaders.IsRetired(m.shader) || !PTShaders.IsProjectShader(m.shader)) bad.Add(path + " → " + m.shader.name);
            }
            return bad;
        }
    }
}
