using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G2: URP-Asset und Renderer je Qualitätsstufe (Forward+, Schatten, SSAO, Decals, PTRenderFeature), Zuordnung zu den
    /// Unity-Qualitätsstufen, Volume-Profil (ACES) und Himmel-Material je Zone, Vollbild-Material.
    /// </summary>
    public static partial class GraphicsBuilder
    {
        public const string RenderDir = Dir + "/Rendering", VolumeDir = Dir + "/Volumes", SkyDir = Dir + "/Sky";
        public const string SourceUrp = DataDir + "/Rendering/PT_URP.asset", SourceRenderer = DataDir + "/Rendering/PT_Renderer.asset";
        public static readonly QualityTier[] Tiers = { QualityTier.Niedrig, QualityTier.Mittel, QualityTier.Hoch, QualityTier.Ultra };
        public static readonly List<string> RenderReport = new List<string>();

        public static string UrpPath(QualityTier t) => RenderDir + "/PT_URP_" + t + ".asset";
        public static string RendererPath(QualityTier t) => RenderDir + "/PT_Renderer_" + t + ".asset";
        public static UniversalRenderPipelineAsset Urp(QualityTier t) => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath(t));

        [MenuItem("Project Tyrant/Grafik G2/Rendering (Stufen, Volumes, Himmel)")]
        public static void BuildRendering()
        {
            RenderReport.Clear();
            EnsureFolder(Dir); EnsureFolder(RenderDir); EnsureFolder(VolumeDir); EnsureFolder(SkyDir);
            var post = PostFxMaterial();
            foreach (var t in Tiers) BuildTier(t, post);
            AssignQualityLevels();
            BuildZoneLooks();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Grafik G2 Rendering: " + string.Join(" · ", RenderReport));
        }

        public static Material PostFxMaterial()
        {
            string path = RenderDir + "/M_PostFx.mat";
            var sh = Shader.Find("ProjectTyrant/Fullscreen/PostFx");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh; EditorUtility.SetDirty(m);
            return m;
        }

        static void Set(SerializedObject so, string name, System.Action<SerializedProperty> a)
        {
            var p = so.FindProperty(name);
            if (p == null) { RenderReport.Add("fehlt: " + name); return; }
            a(p);
        }

        static void BuildTier(QualityTier t, Material post)
        {
            var s = QualityTiers.Of(t);
            // Renderer: Kopie des bestehenden (Shader- und PostProcess-Daten bleiben), dann Forward+ und Features
            string rp = RendererPath(t);
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rp) == null) AssetDatabase.CopyAsset(SourceRenderer, rp);
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rp);
            rd.renderingMode = RenderingMode.ForwardPlus;
            // G7-Befund: ohne PostProcessData schaltet URP das gesamte Post-Processing ab (Bloom, Tonemapping, Volumes waren seit Lauf 1 wirkungslos)
            if (rd.postProcessData == null) rd.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (rd.postProcessData == null) RenderReport.Add("PostProcessData fehlt");
            rd.depthPrimingMode = DepthPrimingMode.Disabled;
            rd.copyDepthMode = CopyDepthMode.AfterOpaques;   // Nebel/Kontur nach dem Himmel brauchen die Tiefe auch ohne SSAO-Vorpass (Niedrig)
            ClearFeatures(rd);
            if (s.Ssao) AddSsao(rd, t);
            AddDecals(rd);
            var f = ScriptableObject.CreateInstance<PTRenderFeature>();
            f.name = "PT Render-Feature"; f.PostFx = post;
            f.Fog = true; f.Outline = true; f.Damage = true; f.Underwater = true;
            f.Godrays = t != QualityTier.Niedrig; f.Distortion = s.Distortion || t == QualityTier.Mittel;
            AddFeature(rd, f);
            EditorUtility.SetDirty(rd);

            string up = UrpPath(t);
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(up) == null) AssetDatabase.CopyAsset(SourceUrp, up);
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(up);
            var so = new SerializedObject(urp);
            Set(so, "m_RendererDataList", p => { p.arraySize = 1; p.GetArrayElementAtIndex(0).objectReferenceValue = rd; });
            Set(so, "m_DefaultRendererIndex", p => p.intValue = 0);
            Set(so, "m_RequireDepthTexture", p => p.boolValue = true);
            Set(so, "m_RequireOpaqueTexture", p => p.boolValue = t != QualityTier.Niedrig);
            Set(so, "m_SupportsHDR", p => p.boolValue = true);
            Set(so, "m_MSAA", p => p.intValue = t == QualityTier.Niedrig ? 1 : t == QualityTier.Mittel ? 2 : 4);
            Set(so, "m_RenderScale", p => p.floatValue = s.RenderScale);
            Set(so, "m_MainLightShadowsSupported", p => p.boolValue = true);
            Set(so, "m_MainLightShadowmapResolution", p => p.intValue = t == QualityTier.Ultra ? 4096 : t == QualityTier.Niedrig ? 1024 : 2048);
            Set(so, "m_AdditionalLightsRenderingMode", p => p.intValue = 1);   // pro Pixel
            Set(so, "m_AdditionalLightShadowsSupported", p => p.boolValue = s.AdditionalShadows);
            Set(so, "m_ShadowDistance", p => p.floatValue = s.ShadowDistance);
            Set(so, "m_ShadowCascadeCount", p => p.intValue = s.Cascades);
            Set(so, "m_Cascade2Split", p => p.floatValue = 0.3f);
            Set(so, "m_Cascade4Split", p => p.vector3Value = new Vector3(0.06f, 0.18f, 0.42f));
            Set(so, "m_CascadeBorder", p => p.floatValue = 0.15f);
            Set(so, "m_SoftShadowsSupported", p => p.boolValue = s.SoftShadows);
            Set(so, "m_SoftShadowQuality", p => p.intValue = t == QualityTier.Ultra ? 3 : 2);
            Set(so, "m_SupportsLightCookies", p => p.boolValue = true);
            Set(so, "m_UseSRPBatcher", p => p.boolValue = true);
            Set(so, "m_ColorGradingMode", p => p.intValue = t == QualityTier.Niedrig ? 0 : 1);   // HDR-Grading ab Mittel
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
            RenderReport.Add(t + ": Forward+, " + s.Cascades + " Kaskaden/" + s.ShadowDistance + " m" + (s.Ssao ? ", SSAO" : "") + ", Skala " + s.RenderScale);
        }

        static void ClearFeatures(ScriptableRendererData rd)
        {
            foreach (var f in rd.rendererFeatures.ToList()) if (f != null) { AssetDatabase.RemoveObjectFromAsset(f); Object.DestroyImmediate(f, true); }
            rd.rendererFeatures.Clear();
            var so = new SerializedObject(rd); var map = so.FindProperty("m_RendererFeatureMap"); if (map != null) map.arraySize = 0; so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddFeature(ScriptableRendererData rd, ScriptableRendererFeature f)
        {
            AssetDatabase.AddObjectToAsset(f, rd);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long id);
            rd.rendererFeatures.Add(f);
            var so = new SerializedObject(rd);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null) { map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSsao(ScriptableRendererData rd, QualityTier t)
        {
            var type = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (type == null) { RenderReport.Add("SSAO-Typ fehlt"); return; }
            var f = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type); f.name = "SSAO";
            AddFeature(rd, f);
            var so = new SerializedObject(f);
            Set(so, "m_Settings.Intensity", p => p.floatValue = 0.9f);
            Set(so, "m_Settings.Radius", p => p.floatValue = 0.35f);
            Set(so, "m_Settings.DirectLightingStrength", p => p.floatValue = 0.2f);
            Set(so, "m_Settings.Downsample", p => p.boolValue = t != QualityTier.Ultra);
            Set(so, "m_Settings.Source", p => p.intValue = 1);        // Tiefe + Normalen: rekonstruierte Normalen erzeugen auf flachem Low-Poly-Gelände Flecken-Rauschen
            Set(so, "m_Settings.NormalSamples", p => p.intValue = 1); // mittel
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddDecals(ScriptableRendererData rd)
        {
            var f = ScriptableObject.CreateInstance<DecalRendererFeature>(); f.name = "Decals";
            AddFeature(rd, f);
            var so = new SerializedObject(f);
            Set(so, "m_Settings.technique", p => p.intValue = 2);            // Screen Space: kein Vorpass, Shader ohne DBuffer-Varianten
            Set(so, "m_Settings.dBufferSettings.surfaceData", p => p.intValue = 0);   // nur Albedo
            Set(so, "m_Settings.maxDrawDistance", p => p.floatValue = 120f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Unity-Stufen 0–1 Niedrig, 2 Mittel, 3–4 Hoch, 5 Ultra (wie QualityTiers.FromUnityLevel); Standard-Pipeline = Hoch.</summary>
        static void AssignQualityLevels()
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qs == null) { RenderReport.Add("QualitySettings nicht gefunden"); return; }
            var so = new SerializedObject(qs);
            var levels = so.FindProperty("m_QualitySettings");
            for (int i = 0; i < levels.arraySize; i++)
            {
                var lvl = levels.GetArrayElementAtIndex(i);
                var tier = QualityTiers.FromUnityLevel(i);
                var crp = lvl.FindPropertyRelative("customRenderPipeline");
                if (crp != null) crp.objectReferenceValue = Urp(tier);
                var vs = lvl.FindPropertyRelative("vSyncCount"); if (vs != null) vs.intValue = 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline = Urp(QualityTier.Hoch);
            RenderReport.Add("Qualitätsstufen zugeordnet (" + levels.arraySize + ")");
        }

        // ---------------- Zonen: Volume + Himmel ----------------
        static void BuildZoneLooks()
        {
            var sky = Shader.Find("ProjectTyrant/Sky");
            int n = 0;
            foreach (var (zone, _, _) in ZoneBiomes)
            {
                var p = Profile(zone); if (p == null) continue;
                p.Volume = ZoneVolume(p);
                string mp = SkyDir + "/M_Sky_" + zone + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(sky); AssetDatabase.CreateAsset(m, mp); }
                m.shader = sky;
                m.SetColor("_Zenith", p.SkyZenith); m.SetColor("_Horizon", p.SkyHorizon); m.SetColor("_Ground", p.SkyGround);
                m.SetColor("_CloudColor", p.CloudColor); m.SetFloat("_CloudCover", p.CloudCover);
                m.SetTexture("_CloudTex", Tex("Clouds")); m.SetTexture("_StarTex", Tex("Starfield"));
                m.SetFloat("_Stars", p.Stars ? 1f : 0f); m.SetFloat("_Aurora", p.Aurora ? 1f : 0f); m.SetFloat("_Ash", p.AshSky ? 1f : 0f); m.SetFloat("_Sand", p.SandVeil ? 1f : 0f);
                EditorUtility.SetDirty(m);
                p.SkyMaterial = m;
                EditorUtility.SetDirty(p); n++;
            }
            RenderReport.Add(n + " Zonen mit ACES-Volume und Himmel 2.0");
        }

        /// <summary>ACES nimmt dunkle Mitteltöne zurück und lässt helle Böden (Schnee, Salz, Sand) clippen: Ausgleich nach Boden-Helligkeit.</summary>
        public static float AcesExposureOffset(Color ground)
        {
            float lum = 0.2126f * ground.r + 0.7152f * ground.g + 0.0722f * ground.b;
            return Mathf.Clamp(1.0f - 0.95f * lum, -0.2f, 1.0f);   // nach Aktivierung des Post-Processings (G7) neu eingemessen
        }

        public static VolumeProfile ZoneVolume(ZoneArtProfile p)
        {
            string path = VolumeDir + "/PT_Vol_" + p.ZoneId + ".asset";
            var v = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (v == null) { v = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(v, path); }
            foreach (var c in v.components.ToList()) { v.Remove(c.GetType()); Object.DestroyImmediate(c, true); }
            var tm = v.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var ca = v.Add<ColorAdjustments>(true);
            ca.postExposure.Override(p.Exposure + AcesExposureOffset(p.Ground) + p.ExposureCalibration);
            ca.contrast.Override(p.Contrast); ca.saturation.Override(p.Saturation + 4f);
            var wb = v.Add<WhiteBalance>(true); wb.temperature.Override(p.WhiteBalance);
            var st = v.Add<SplitToning>(true); st.shadows.Override(p.SplitShadows); st.highlights.Override(p.SplitHighlights); st.balance.Override(-15f);
            var bl = v.Add<Bloom>(true); bl.threshold.Override(p.BloomThreshold); bl.intensity.Override(p.BloomIntensity); bl.scatter.Override(0.65f);
            var vi = v.Add<Vignette>(true); vi.intensity.Override(p.Vignette); vi.smoothness.Override(0.45f); vi.color.Override(p.ShadowColor * 0.25f);
            var smh = v.Add<ShadowsMidtonesHighlights>(true);
            var sc = p.ShadowColor; smh.shadows.Override(new Vector4(sc.r, sc.g, sc.b, -0.05f));
            var mb = v.Add<MotionBlur>(true); mb.intensity.Override(0f); mb.active = false;   // Barrierefreiheit: Bewegungsunschärfe aus
            foreach (var c in v.components) { AssetDatabase.AddObjectToAsset(c, v); c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; }
            EditorUtility.SetDirty(v);
            return v;
        }
    }
}
