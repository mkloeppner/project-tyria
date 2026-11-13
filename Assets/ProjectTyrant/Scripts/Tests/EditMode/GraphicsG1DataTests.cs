using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Visuals;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G1: erzeugte Daten – Kunstprofil je Zone (vollständig, eindeutig, ohne Gefahrenfarben), zehn Elemente, sechs Lesezeichen je Zone.</summary>
    public class GraphicsG1DataTests
    {
        static System.Collections.Generic.IEnumerable<string> Zones => GraphicsBuilder.ZoneBiomes.Select(z => z.zone);

        [TestCaseSource(nameof(Zones))] public void Profile_CompleteAndSafe(string zone)
        {
            var p = GraphicsBuilder.Profile(zone);
            Assert.IsNotNull(p, zone);
            Assert.GreaterOrEqual(p.Weathers.Count, 2, "≥ 2 Wetterzustände");
            Assert.GreaterOrEqual(p.AmbientFx.Count, 4, "≥ 4 Umgebungs-Effekte");
            foreach (var c in new[] { p.Ground, p.VegetationRock, p.Sky }) Assert.IsFalse(DangerLanguage.Conflicts(c), zone + ": Gefahrenfarbe in der Palette " + c);
            Assert.AreNotEqual(p.LightColor, p.ShadowColor);
            Assert.Greater(p.ShadowColor.maxColorComponent, 0.1f, "Schatten eingefärbt, nie schwarz");
        }

        [Test] public void Profiles_AllPalettesUnique()
        {
            var ps = Zones.Select(GraphicsBuilder.Profile).Where(p => p != null).ToList();
            Assert.AreEqual(Zones.Count(), ps.Count);
            for (int i = 0; i < ps.Count; i++) for (int j = i + 1; j < ps.Count; j++)
                Assert.Greater(DangerReadability.DeltaE(ps[i].Ground, ps[j].Ground) + DangerReadability.DeltaE(ps[i].VegetationRock, ps[j].VegetationRock) + DangerReadability.DeltaE(ps[i].Sky, ps[j].Sky), 6f, ps[i].ZoneId + " ~ " + ps[j].ZoneId);
        }

        [Test] public void Elements_TenWithIdentity()
        {
            foreach (FxElement e in System.Enum.GetValues(typeof(FxElement)))
            {
                var p = GraphicsBuilder.Element(e);
                Assert.IsNotNull(p, e.ToString());
                Assert.Greater(p.Core.maxColorComponent, 1f, e + ": Kern HDR");
                Assert.IsFalse(string.IsNullOrEmpty(p.SoundFamily));
            }
        }

        [TestCaseSource(nameof(Zones))] public void Bookmarks_SixPerZone(string zone)
        {
            var b = GraphicsBuilder.Bookmarks(zone);
            Assert.AreEqual(6, b.Count, zone);
            CollectionAssert.AreEquivalent(new[] { BookmarkKind.Wegmarke, BookmarkKind.Wahrzeichen, BookmarkKind.Kampf, BookmarkKind.Innenraum, BookmarkKind.WasserHuerde, BookmarkKind.Horizont }, b.Select(x => x.Kind));
        }

        [Test] public void Textures_RecipesDeterministic() => Assert.AreEqual(0, GraphicsBuilder.VerifyDeterminism());
    }
}
