using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G2: Beleuchtungsmodell, Nebel-Regel, Stufen-Assets, Volumes, Himmel, Migration.</summary>
    public class GraphicsG2Tests
    {
        static ZoneArtProfile TestProfile()
        {
            var p = ScriptableObject.CreateInstance<ZoneArtProfile>();
            p.SunAzimuth = 135f; p.SunNoonElevation = 55f; p.SunIntensity = 1.3f; p.FogDensity = 0.004f; p.FogHeight = 8f; p.FogHeightFalloff = 0.08f;
            p.AmbientSky = new Color(0.5f, 0.6f, 0.8f); p.AmbientEquator = new Color(0.6f, 0.6f, 0.6f); p.AmbientGround = new Color(0.3f, 0.3f, 0.3f);
            p.CloudCover = 0.4f; p.Stars = true;
            return p;
        }
        static WeatherLook Clear => WeatherModel.LookOf(WeatherState.Klar);

        [TearDown] public void Reset() { SceneLighting.ExternalFogScale = 1f; SceneLighting.ExternalFogTint = Color.clear; }

        [Test]
        public void Compute_Noon_SunAboveHorizonAndBrighterThanNight()
        {
            var p = TestProfile();
            var noon = SceneLighting.Compute(p, 12f, Clear);
            var night = SceneLighting.Compute(p, 0f, Clear);
            Assert.Greater(noon.LightDir.y, 0.5f);
            Assert.IsFalse(noon.Moon); Assert.IsTrue(night.Moon);
            Assert.Greater(noon.LightIntensity, night.LightIntensity * 2f);
            Assert.Greater(night.LightDir.y, 0.3f, "Mondlicht kommt von oben");
            Assert.Greater(night.StarAlpha, 0.9f);
        }

        [Test]
        public void Compute_StormWeather_DarkerAndFoggier()
        {
            var p = TestProfile();
            var clear = SceneLighting.Compute(p, 12f, Clear);
            var storm = SceneLighting.Compute(p, 12f, WeatherModel.LookOf(WeatherState.Gewitter));
            Assert.Less(storm.LightIntensity, clear.LightIntensity);
            Assert.Greater(storm.FogDensity, clear.FogDensity);
            Assert.Greater(storm.CloudExtra, 0f);
        }

        [Test]
        public void Compute_ExternalFogScale_FollowsGameplayVision()
        {
            var p = TestProfile();
            float a = SceneLighting.Compute(p, 12f, Clear).FogDensity;
            SceneLighting.ExternalFogScale = 4f;
            Assert.AreEqual(a * 4f, SceneLighting.Compute(p, 12f, Clear).FogDensity, 1e-6f);
        }

        [Test]
        public void Compute_Underground_IgnoresTimeOfDay()
        {
            var p = TestProfile(); p.Underground = true;
            var a = SceneLighting.Compute(p, 0f, Clear); var b = SceneLighting.Compute(p, 12f, Clear);
            Assert.AreEqual(a.LightIntensity, b.LightIntensity, 1e-5f);
            Assert.IsFalse(a.Moon);
        }

        [Test]
        public void Compute_Emissive_OnAtNightOffAtNoon()
        {
            var p = TestProfile();
            Assert.AreEqual(0f, SceneLighting.Compute(p, 12f, Clear).Emissive, 1e-5f);
            Assert.AreEqual(1f, SceneLighting.Compute(p, 23f, Clear).Emissive, 1e-5f);
        }

        [Test]
        public void FogAmount_GrowsWithDistanceAndCapped()
        {
            float a = SceneLighting.FogAmount(50f, 10f, 0f, 0.004f, 8f, 0.08f);
            float b = SceneLighting.FogAmount(500f, 10f, 0f, 0.004f, 8f, 0.08f);
            float c = SceneLighting.FogAmount(100000f, 10f, 0f, 0.004f, 8f, 0.08f);
            Assert.Less(a, b); Assert.LessOrEqual(c, SceneLighting.MaxFog + 1e-5f);
        }

        [Test]
        public void FogAmount_HigherCameraSeesThinnerFog()
        {
            Assert.Less(SceneLighting.FogAmount(300f, 60f, 0f, 0.004f, 8f, 0.08f), SceneLighting.FogAmount(300f, 9f, 0f, 0.004f, 8f, 0.08f));
        }

        [Test]
        public void FogAmount_LandmarkSilhouetteStaysReadable_AllZones()
        {
            // ART_BIBLE Regel 4: Nebel ≤ 60 % Deckung bis zur Wahrzeichen-Distanz (250 m, Blick vom Boden leicht nach oben), klar, Mittag
            foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
            {
                var p = GraphicsBuilder.Profile(zone); Assert.IsNotNull(p, zone);
                var f = SceneLighting.Compute(p, 12f, Clear);
                float fog = SceneLighting.FogAmount(250f, f.FogBase + 2f, 25f, f.FogDensity, f.FogBase, f.FogFalloff);
                Assert.LessOrEqual(fog, 0.6f, zone);
            }
        }

        [Test]
        public void AcesExposureOffset_BrightGroundGetsLessExposure()
        {
            Assert.Less(GraphicsBuilder.AcesExposureOffset(new Color(0.95f, 0.95f, 0.97f)), GraphicsBuilder.AcesExposureOffset(new Color(0.2f, 0.25f, 0.15f)));
            Assert.GreaterOrEqual(GraphicsBuilder.AcesExposureOffset(Color.white), -0.2f);
            Assert.LessOrEqual(GraphicsBuilder.AcesExposureOffset(Color.black), 1.0f);
        }

        [TestCase(QualityTier.Niedrig)]
        [TestCase(QualityTier.Mittel)]
        [TestCase(QualityTier.Hoch)]
        [TestCase(QualityTier.Ultra)]
        public void TierAsset_ForwardPlusWithTierShadows(QualityTier t)
        {
            var urp = GraphicsBuilder.Urp(t); Assert.IsNotNull(urp, t.ToString());
            var s = QualityTiers.Of(t);
            Assert.AreEqual(s.Cascades, urp.shadowCascadeCount);
            Assert.AreEqual(s.ShadowDistance, urp.shadowDistance, 0.01f);
            Assert.AreEqual(s.RenderScale, urp.renderScale, 0.01f);
            Assert.IsTrue(urp.useSRPBatcher);
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(GraphicsBuilder.RendererPath(t));
            Assert.AreEqual(RenderingMode.ForwardPlus, rd.renderingMode);
            var pt = rd.rendererFeatures.OfType<PTRenderFeature>().SingleOrDefault();
            Assert.IsNotNull(pt); Assert.IsNotNull(pt.PostFx);
            Assert.IsTrue(pt.Fog && pt.Outline, "Nebel und Kontur auf jeder Stufe");
            Assert.AreEqual(s.Ssao, rd.rendererFeatures.Any(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion"));
            Assert.IsTrue(rd.rendererFeatures.Any(f => f is DecalRendererFeature));
        }

        [Test]
        public void QualityLevels_MapToTierAssets()
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
                Assert.AreEqual(GraphicsBuilder.Urp(QualityTiers.FromUnityLevel(i)), QualitySettings.GetRenderPipelineAssetAt(i), names[i]);
            Assert.AreEqual(GraphicsBuilder.Urp(QualityTier.Hoch), GraphicsSettings.defaultRenderPipeline);
        }

        [Test]
        public void ZoneProfiles_HaveAcesVolumeWithoutMotionBlur_AndSky2()
        {
            foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
            {
                var p = GraphicsBuilder.Profile(zone);
                Assert.IsNotNull(p.Volume, zone);
                Assert.IsTrue(p.Volume.TryGet<Tonemapping>(out var tm) && tm.mode.value == TonemappingMode.ACES, zone);
                Assert.IsFalse(p.Volume.TryGet<MotionBlur>(out var mb) && mb.active && mb.intensity.value > 0f, zone + ": Bewegungsunschärfe aus");
                Assert.IsNotNull(p.SkyMaterial, zone); Assert.AreEqual(PTShaders.Sky, p.SkyMaterial.shader.name);
            }
        }

        [Test]
        public void PostFxShader_HasOnePassPerFeature()
        {
            var sh = Shader.Find(PTShaders.PostFx); Assert.IsNotNull(sh);
            var m = new Material(sh);
            try { Assert.AreEqual(PTRenderFeature.PassNames.Length, m.passCount); }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void Materials_NoneOnRetiredOrUnityShaders()
        {
            var bad = MaterialMigrator.Validate();
            Assert.IsEmpty(bad, string.Join("\n", bad.Take(10)));
        }

        [Test]
        public void ToonLit_HasAllRequiredPasses_AndSrpBatcherBuffer()
        {
            var sh = Shader.Find(PTShaders.ToonLit); Assert.IsNotNull(sh);
            var m = new Material(sh);
            try
            {
                foreach (var pass in new[] { "ForwardToon", "ShadowCaster", "DepthOnly", "DepthNormals" }) Assert.GreaterOrEqual(m.FindPass(pass), 0, pass);
                var src = System.IO.File.ReadAllText("Assets/ProjectTyrant/Shaders/PT_ToonLit.shader");
                StringAssert.Contains("CBUFFER_START(UnityPerMaterial)", src);
                Assert.LessOrEqual(src.Split('\n').Count(l => l.Contains("#pragma multi_compile")), 8 + 4, "≤ 8 multi_compile im Forward-Pass (+ je 1 Instancing in den Neben-Passes)");
            }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void PTShaders_RetiredDetection()
        {
            Assert.IsTrue(PTShaders.IsRetired(Shader.Find("ProjectTyrant/ToonRim")));
            Assert.IsFalse(PTShaders.IsRetired(Shader.Find(PTShaders.ToonLit)));
            Assert.IsFalse(PTShaders.IsProjectShader(Shader.Find("Universal Render Pipeline/Lit")));
        }
    }
}
