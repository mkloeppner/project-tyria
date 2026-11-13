using System.Linq;
using NUnit.Framework;
using ProjectTyrant.Visuals;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G1: Metriken, Lesbarkeit, Texturen, Tageszeit, Wetter, Laternen, Governor, Qualitätsstufen, Lichtempfindlich, Elemente (rein).</summary>
    public class GraphicsG1Tests
    {
        static Color32[] Fill(int n, Color32 c) { var a = new Color32[n]; for (int i = 0; i < n; i++) a[i] = c; return a; }

        // ---------------- VisualMetrics ----------------
        [Test] public void Metrics_FlatGrayHasNoContrastNoEdgesOneCluster()
        {
            var px = Fill(64 * 64, new Color32(128, 128, 128, 255));
            var m = VisualMetrics.Measure(px, 64, 64);
            Assert.AreEqual(0f, m.RmsContrast, 1e-4f); Assert.AreEqual(0f, m.EdgeDensity, 1e-4f); Assert.AreEqual(0f, m.MeanSaturation, 1e-4f);
            Assert.AreEqual(1, m.Clusters); Assert.AreEqual(0f, m.ClippedShare);
        }

        [Test] public void Metrics_CheckerboardHasMaxContrastAndClipping()
        {
            var px = new Color32[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) px[y * 32 + x] = ((x / 4 + y / 4) % 2 == 0) ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
            var m = VisualMetrics.Measure(px, 32, 32);
            Assert.AreEqual(0.5f, m.RmsContrast, 0.01f); Assert.AreEqual(1f, m.ClippedShare, 1e-4f); Assert.AreEqual(0.5f, m.BlackShare, 0.01f);
            Assert.Greater(m.EdgeDensity, 0.1f); Assert.AreEqual(2, m.Clusters);
        }

        [Test] public void Metrics_SaturationOfPureColors()
        {
            var m = VisualMetrics.Measure(Fill(16, new Color32(255, 0, 0, 255)), 4, 4);
            Assert.AreEqual(1f, m.MeanSaturation, 1e-4f);
            Assert.AreEqual(0.5f, VisualMetrics.Measure(Fill(16, new Color32(200, 100, 100, 255)), 4, 4).MeanSaturation, 0.01f);
        }

        [Test] public void Metrics_ClustersCountDistinctColors()
        {
            var cols = new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255), new Color32(255, 255, 0, 255), new Color32(30, 30, 30, 255) };
            var px = new Color32[100 * 50]; for (int i = 0; i < px.Length; i++) px[i] = cols[(i / 1000) % cols.Length];
            Assert.AreEqual(5, VisualMetrics.Measure(px, 100, 50).Clusters);
        }

        [Test] public void Metrics_TinyColorBelowOnePercentNotACluster()
        {
            var px = Fill(10000, new Color32(50, 100, 150, 255)); for (int i = 0; i < 50; i++) px[i] = new Color32(255, 0, 255, 255);
            Assert.AreEqual(1, VisualMetrics.Clusters(px));
        }

        [Test] public void Metrics_SkyShareFromMask()
        {
            var mask = new bool[100]; for (int i = 0; i < 30; i++) mask[i] = true;
            Assert.AreEqual(0.3f, VisualMetrics.Measure(Fill(4, new Color32(1, 2, 3, 255)), 2, 2, mask).SkyShare, 1e-4f);
        }

        [Test] public void Metrics_HistogramSumsToPixelCount()
        {
            var px = new Color32[256]; for (int i = 0; i < 256; i++) px[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
            var m = VisualMetrics.Measure(px, 16, 16);
            Assert.AreEqual(256, m.Histogram.Sum()); StringAssert.Contains(";", m.ToCsv());
        }

        // ---------------- Lesbarkeit ----------------
        [Test] public void DeltaE_IdenticalIsZero_BlackWhiteIsHundred()
        {
            Assert.AreEqual(0f, DangerReadability.DeltaE(Color.red, Color.red), 1e-3f);
            Assert.AreEqual(100f, DangerReadability.DeltaE(Color.black, Color.white), 0.5f);
        }

        [Test] public void Readability_DangerOnGrassReadable_OnRedBrownNot()
        {
            Assert.IsTrue(DangerReadability.Readable(DangerLanguage.Danger, new Color(0.43f, 0.6f, 0.27f)));
            Assert.IsFalse(DangerReadability.Readable(DangerLanguage.Danger, new Color(0.95f, 0.35f, 0.15f)));
            Assert.IsTrue(DangerReadability.MinDeltaEColorblind < DangerReadability.MinDeltaE);
        }

        [Test] public void Readability_LuminanceJump()
        {
            Assert.AreEqual(0.5f, DangerReadability.MaxLuminanceJump(new[] { 0.2f, 0.25f, 0.75f, 0.7f }), 1e-4f);
            Assert.AreEqual(0f, DangerReadability.MaxLuminanceJump(null));
        }

        [Test] public void DangerLanguage_ConflictDetection()
        {
            Assert.IsTrue(DangerLanguage.Conflicts(new Color(1f, 0.34f, 0.14f)));
            Assert.IsFalse(DangerLanguage.Conflicts(new Color(0.3f, 0.5f, 0.9f)));
        }

        // ---------------- Texturen ----------------
        [TestCase(TextureKind.Perlin)] [TestCase(TextureKind.Simplex)] [TestCase(TextureKind.Worley)] [TestCase(TextureKind.Voronoi)] [TestCase(TextureKind.Ridged)]
        [TestCase(TextureKind.DomainWarp)] [TestCase(TextureKind.Caustics)] [TestCase(TextureKind.FlowMap)] [TestCase(TextureKind.Flipbook)] [TestCase(TextureKind.CookieLeaves)]
        [TestCase(TextureKind.CookieGrid)] [TestCase(TextureKind.Starfield)] [TestCase(TextureKind.Clouds)] [TestCase(TextureKind.Ramp)]
        public void Texture_DeterministicAndSized(TextureKind k)
        {
            var a = TextureForge.Generate(k, 64, 7, 4f, 3, 4, FlipbookShape.Feuer);
            var b = TextureForge.Generate(k, 64, 7, 4f, 3, 4, FlipbookShape.Feuer);
            Assert.AreEqual(64 * 64, a.Length);
            Assert.AreEqual(TextureForge.HashOf(a), TextureForge.HashOf(b));
        }

        [Test] public void Texture_SeedChangesNoise()
        {
            Assert.AreNotEqual(TextureForge.HashOf(TextureForge.Generate(TextureKind.Simplex, 32, 1)), TextureForge.HashOf(TextureForge.Generate(TextureKind.Simplex, 32, 2)));
        }

        [Test] public void Texture_SizeClampedToPowerOfTwoMax1024()
        {
            Assert.AreEqual(1024 * 1024, TextureForge.Generate(TextureKind.Perlin, 5000, 1).Length);
            Assert.AreEqual(64 * 64, TextureForge.Generate(TextureKind.Perlin, 50, 1).Length);
        }

        [Test] public void Noise_IsTileable()
        {
            for (int i = 0; i < 10; i++)
            {
                float y = i * 0.37f;
                Assert.AreEqual(TextureForge.Perlin(0f, y, 3, 8), TextureForge.Perlin(8f, y, 3, 8), 1e-4f, "Periode in x");
                Assert.AreEqual(TextureForge.Perlin(y, 0f, 3, 8), TextureForge.Perlin(y, 8f, 3, 8), 1e-4f, "Periode in y");
            }
        }

        [Test] public void Noise_InRange()
        {
            for (int i = 0; i < 200; i++) { float n = TextureForge.Fbm(i * 0.173f, i * 0.311f, 5, 16, 4); Assert.LessOrEqual(Mathf.Abs(n), 1.01f); }
        }

        [Test] public void Flipbook_FramesDiffer()
        {
            var px = TextureForge.Generate(TextureKind.Flipbook, 128, 3, 1, 1, 4, FlipbookShape.Explosion);
            float A(int tile) { int tx = tile % 4, ty = 3 - tile / 4; float s = 0; for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) s += px[(ty * 32 + y) * 128 + tx * 32 + x].a; return s; }
            Assert.AreNotEqual(A(0), A(15), "erste und letzte Phase unterschiedlich");
        }

        // ---------------- Tageszeit ----------------
        [Test] public void TimeOfDay_CycleIs48MinutesFor24h()
        {
            var t = new TimeOfDayModel(); t.Set(0f);
            for (int i = 0; i < 48 * 60; i++) t.Tick(1f);
            Assert.AreEqual(0f, Mathf.Min(t.Hour, 24f - t.Hour), 0.01f);
            t.Set(6f); t.Tick(120f); Assert.AreEqual(7f, t.Hour, 0.01f, "2 Minuten = 1 Stunde");
        }

        [Test] public void TimeOfDay_PauseSpeedForceRelease()
        {
            var t = new TimeOfDayModel(); t.Set(10f); t.Paused = true; t.Tick(600f); Assert.AreEqual(10f, t.Hour);
            t.Paused = false; t.Speed = 2f; t.Tick(120f); Assert.AreEqual(12f, t.Hour, 0.01f);
            t.Force(21f); Assert.IsTrue(t.Forced); Assert.AreEqual(21f, t.EffectiveHour); t.Tick(120f); Assert.AreEqual(21f, t.EffectiveHour);
            t.Release(); Assert.IsFalse(t.Forced); Assert.AreEqual(t.Hour, t.EffectiveHour);
        }

        [Test] public void TimeOfDay_SunRisesAt6SetsAt18()
        {
            Assert.AreEqual(0f, TimeOfDayModel.SunElevation(6f, 60f), 0.01f);
            Assert.AreEqual(60f, TimeOfDayModel.SunElevation(12f, 60f), 0.01f);
            Assert.AreEqual(0f, TimeOfDayModel.SunElevation(18f, 60f), 0.01f);
            Assert.Less(TimeOfDayModel.SunElevation(0f, 60f), 0f);
            Assert.IsTrue(TimeOfDayModel.IsNight(23f, 60f)); Assert.IsFalse(TimeOfDayModel.IsNight(12f, 60f));
            Assert.Greater(TimeOfDayModel.SunDirection(12f, 135f, 55f).y, 0.8f);
        }

        [Test] public void TimeOfDay_KeyframesContinuousNoJumps()
        {
            var prev = TimeOfDayModel.Sample(0f);
            for (float h = 0.1f; h <= 24f; h += 0.1f)
            {
                var k = TimeOfDayModel.Sample(h);
                Assert.Less(Mathf.Abs(k.LightIntensity - prev.LightIntensity), 0.05f, "Sprung bei " + h);
                Assert.Less(Mathf.Abs(k.StarAlpha - prev.StarAlpha), 0.08f);
                prev = k;
            }
            Assert.AreEqual(1f, TimeOfDayModel.Sample(12f).LightIntensity, 1e-4f);
            Assert.Greater(TimeOfDayModel.Sample(0f).StarAlpha, 0.9f);
        }

        [Test] public void TimeOfDay_NightIsBlueNotGray()
        {
            var n = TimeOfDayModel.Sample(1f).LightTint;
            Assert.Greater(n.b, n.r + 0.2f, "Nacht ist blau (Regel 9)");
        }

        // ---------------- Wetter ----------------
        [Test] public void Weather_ChangesWithinInterval()
        {
            var w = new WeatherModel(new[] { WeatherState.Klar, WeatherState.Regen }, 3);
            Assert.AreEqual(WeatherState.Klar, w.Current);
            Assert.GreaterOrEqual(w.NextChangeIn, WeatherModel.MinInterval); Assert.LessOrEqual(w.NextChangeIn, WeatherModel.MaxInterval);
            bool changed = false; for (int i = 0; i < 900 && !changed; i++) changed = w.Tick(1f);
            Assert.IsTrue(changed); Assert.AreEqual(WeatherState.Regen, w.Current);
        }

        [Test] public void Weather_BlendsOver30Seconds()
        {
            var w = new WeatherModel(new[] { WeatherState.Klar, WeatherState.Regen }, 3);
            w.SetState(WeatherState.Regen, false);
            Assert.AreEqual(0f, w.Blend); w.Tick(15f); Assert.AreEqual(0.5f, w.Blend, 0.01f);
            float mid = w.Look.Wetness; Assert.Greater(mid, 0.1f); Assert.Less(mid, 0.9f);
            w.Tick(20f); Assert.AreEqual(1f, w.Blend); Assert.AreEqual(1f, w.Look.Wetness, 1e-4f);
        }

        [Test] public void Weather_LockAndSingleState()
        {
            var w = new WeatherModel(new[] { WeatherState.Klar, WeatherState.Schnee }, 1); w.Locked = true;
            for (int i = 0; i < 2000; i++) Assert.IsFalse(w.Tick(1f));
            var one = new WeatherModel(new[] { WeatherState.Sandsturm }, 1);
            for (int i = 0; i < 2000; i++) one.Tick(1f);
            Assert.AreEqual(WeatherState.Sandsturm, one.Current);
            Assert.AreEqual(WeatherState.Klar, new WeatherModel(null, 1).Current);
        }

        [Test] public void Weather_ArenaCapAndDeterminism()
        {
            var storm = WeatherModel.LookOf(WeatherState.Schneesturm);
            Assert.AreEqual(WeatherModel.ArenaParticleCap, WeatherModel.ParticleShare(storm, true), 1e-4f);
            Assert.AreEqual(storm.Particles, WeatherModel.ParticleShare(storm, false));
            var a = new WeatherModel(new[] { WeatherState.Klar, WeatherState.Regen, WeatherState.Gewitter }, 9); var b = new WeatherModel(new[] { WeatherState.Klar, WeatherState.Regen, WeatherState.Gewitter }, 9);
            for (int i = 0; i < 3000; i++) { a.Tick(1f); b.Tick(1f); Assert.AreEqual(a.Current, b.Current); }
        }

        [Test] public void Weather_EveryStateHasLook()
        {
            foreach (WeatherState s in System.Enum.GetValues(typeof(WeatherState))) { var l = WeatherModel.LookOf(s); Assert.Greater(l.LightScale, 0.3f, s.ToString()); Assert.GreaterOrEqual(l.FogScale, 1f); }
        }

        // ---------------- Laternen, Governor, Stufen, Lichtempfindlich, Elemente ----------------
        [Test] public void Emissive_OnAtDuskOffAtDawn()
        {
            Assert.AreEqual(0f, EmissiveSchedule.Level(12f)); Assert.AreEqual(1f, EmissiveSchedule.Level(23f)); Assert.AreEqual(1f, EmissiveSchedule.Level(2f));
            Assert.Greater(EmissiveSchedule.Level(18.8f), 0f); Assert.Less(EmissiveSchedule.Level(18.8f), 1f);
            Assert.Greater(EmissiveSchedule.Level(6.2f), 0f); Assert.AreEqual(0f, EmissiveSchedule.Level(7f));
        }

        [Test] public void Governor_HysteresisDownAndUp()
        {
            var g = new PerformanceGovernor();
            Assert.AreEqual(0, g.Tick(40f, 2f)); Assert.AreEqual(-1, g.Tick(40f, 1.1f)); Assert.AreEqual(0.6f, g.AmbientFactor, 1e-4f);
            Assert.AreEqual(0, g.Tick(50f, 30f), "Band 45–55: nichts");
            Assert.AreEqual(0, g.Tick(60f, 9f)); Assert.AreEqual(1, g.Tick(60f, 1.1f)); Assert.AreEqual(1f, g.AmbientFactor);
            for (int i = 0; i < 10; i++) g.Tick(30f, 3.1f);
            Assert.AreEqual(PerformanceGovernor.MaxSteps, g.Steps);
        }

        [Test] public void QualityTiers_MatchSpec()
        {
            var lo = QualityTiers.Of(QualityTier.Niedrig); var hi = QualityTiers.Of(QualityTier.Hoch); var ul = QualityTiers.Of(QualityTier.Ultra);
            Assert.AreEqual(1, lo.Cascades); Assert.AreEqual(40f, lo.ShadowDistance); Assert.IsFalse(lo.Ssao); Assert.AreEqual(0.75f, lo.RenderScale); Assert.AreEqual(4000, lo.ParticleCap);
            Assert.AreEqual(4, hi.Cascades); Assert.AreEqual(150f, hi.ShadowDistance); Assert.AreEqual(20000, hi.ParticleCap);
            Assert.IsTrue(ul.AdditionalShadows); Assert.AreEqual(200f, ul.ShadowDistance);
            Assert.AreEqual(QualityTier.Hoch, QualityTiers.FromUnityLevel(3)); Assert.AreEqual(QualityTier.Niedrig, QualityTiers.FromUnityLevel(0)); Assert.AreEqual(QualityTier.Ultra, QualityTiers.FromUnityLevel(5));
        }

        [Test] public void QualityTiers_Suggestion()
        {
            Assert.AreEqual(QualityTier.Niedrig, QualityTiers.Suggest(1024, 10f));
            Assert.AreEqual(QualityTier.Mittel, QualityTiers.Suggest(3000, 10f));
            Assert.AreEqual(QualityTier.Hoch, QualityTiers.Suggest(8000, 8f));
            Assert.AreEqual(QualityTier.Ultra, QualityTiers.Suggest(12000, 4f));
        }

        [Test] public void Photosensitive_CapsFlashBloomShake()
        {
            Photosensitive.Enabled = true;
            try
            {
                Assert.AreEqual(Photosensitive.FlashCap, Photosensitive.ClampFlash(1f)); Assert.AreEqual(Photosensitive.BloomCap, Photosensitive.ClampBloom(2f)); Assert.AreEqual(0f, Photosensitive.Shake(1f));
            }
            finally { Photosensitive.Enabled = false; }
            Assert.AreEqual(1f, Photosensitive.ClampFlash(1f)); Assert.AreEqual(0.5f, Photosensitive.Shake(0.5f));
        }

        [Test] public void ElementMap_CoversAllPalettesAndAllElements()
        {
            var used = new System.Collections.Generic.HashSet<FxElement>();
            foreach (VfxPalette p in System.Enum.GetValues(typeof(VfxPalette))) used.Add(ElementMap.Of(p));
            Assert.AreEqual(10, used.Count, "alle zehn Elemente erreichbar");
            Assert.AreEqual(FxElement.Feuer, ElementMap.Of(VfxPalette.Lava)); Assert.AreEqual(FxElement.Jade, ElementMap.Of(VfxPalette.Jade));
        }
    }
}
