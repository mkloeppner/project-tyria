using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Visuals;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G4: Wetter, Umgebungs-Volumen, Kampf-Dämpfung, Blitz-Deckel, Laternen.</summary>
    public class GraphicsG4Tests
    {
        [TearDown] public void Reset() { Photosensitive.Enabled = false; }

        [Test] public void CombatDamp_InsideRadiusInCombat() => Assert.AreEqual(GraphicsDirector.CombatDamp, GraphicsDirector.CombatDampFactor(5f, true));
        [Test] public void CombatDamp_OutsideRadius() => Assert.AreEqual(1f, GraphicsDirector.CombatDampFactor(20f, true));
        [Test] public void CombatDamp_NotInCombat() => Assert.AreEqual(1f, GraphicsDirector.CombatDampFactor(2f, false));

        [Test]
        public void KindTime_FirefliesOnlyAtNight_PollenOnlyByDay()
        {
            Assert.AreEqual(0f, GraphicsDirector.KindTimeFactor(AmbientFxKind.Gluehwuermchen, 0f));
            Assert.Greater(GraphicsDirector.KindTimeFactor(AmbientFxKind.Gluehwuermchen, 1f), 0.9f);
            Assert.AreEqual(0f, GraphicsDirector.KindTimeFactor(AmbientFxKind.Pollen, 1f));
            Assert.AreEqual(1f, GraphicsDirector.KindTimeFactor(AmbientFxKind.Asche, 0.5f));
        }

        [Test]
        public void Volumes_AtLeastFour_EvenWithFewViews()
        {
            var v = GraphicsBuilder.Volumes(new List<(Vector3, Vector3)> { (Vector3.zero, Vector3.zero) }, new[] { AmbientFxKind.Pollen, AmbientFxKind.Voegel });
            Assert.GreaterOrEqual(v.Count, GraphicsBuilder.MinAmbientVolumes);
            Assert.IsTrue(v.Any(x => x.Kind == AmbientFxKind.Voegel));
        }

        [Test]
        public void Volumes_PlacedInFrontOfView()
        {
            var v = GraphicsBuilder.Volumes(new List<(Vector3, Vector3)> { (new Vector3(0, 10, 0), new Vector3(0, 90, 0)) }, new[] { AmbientFxKind.Sand });
            Assert.Greater(v[0].Center.x, 15f, "Blickrichtung +X");
        }

        [Test]
        public void AllZones_HaveFourAmbientVolumesFromTheirBiome()
        {
            foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
            {
                var p = GraphicsBuilder.Profile(zone);
                Assert.GreaterOrEqual(p.AmbientVolumes.Count, GraphicsBuilder.MinAmbientVolumes, zone);
                Assert.IsTrue(p.AmbientVolumes.All(a => p.AmbientFx.Contains(a.Kind)), zone + ": nur Biom-Effekte");
            }
        }

        [Test]
        public void ArenaCap_LimitsWeatherParticlesTo30Percent()
        {
            var storm = WeatherModel.LookOf(WeatherState.Gewitter);
            Assert.AreEqual(WeatherModel.ArenaParticleCap, WeatherModel.ParticleShare(storm, true), 1e-5f);
            Assert.AreEqual(storm.Particles, WeatherModel.ParticleShare(storm, false), 1e-5f);
        }

        [Test]
        public void Lightning_PhotosensitiveCapped()
        {
            Photosensitive.Enabled = true;
            Assert.LessOrEqual(Photosensitive.ClampFlash(0.9f), Photosensitive.FlashCap);
        }

        [Test]
        public void Weather_IsVisualOnly_NoGameplayReferences()
        {
            // Gameplay-Neutralität: kein Gameplay-Skript liest Wetter oder Tageszeit
            foreach (var f in System.IO.Directory.GetFiles("Assets/ProjectTyrant/Scripts", "*.cs", System.IO.SearchOption.AllDirectories))
            {
                if (f.Replace('\\', '/').Contains("/Visuals/") || f.Contains("Editor") || f.Contains("Tests")) continue;
                var src = System.IO.File.ReadAllText(f);
                Assert.IsFalse(src.Contains("GraphicsDirector.Instance.Weather") || src.Contains(".Time.EffectiveHour"), f);
            }
        }

        [Test]
        public void Lanterns_GetNightEmission()
        {
            var m = new Material(Shader.Find(PTShaders.ToonLit)) { name = "M_Laterne_Test" };
            try { MaterialMigrator.ApplyFamilyDefaults(m); Assert.AreEqual(1f, m.GetFloat("_NightEmission")); Assert.Greater(m.GetColor("_EmissionColor").maxColorComponent, 0.5f); }
            finally { Object.DestroyImmediate(m); }
        }
    }
}
