using NUnit.Framework;
using ProjectTyrant.Visuals;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G6: Reittier-Paletten, Boss-Elemente, Reise-Erkennung, Rang-Farben.</summary>
    public class GraphicsG6Tests
    {
        [Test]
        public void MountPalette_EveryMountHasOwnLook()
        {
            foreach (MountKind k in System.Enum.GetValues(typeof(MountKind)))
                if (k != MountKind.None) Assert.AreNotEqual(VfxPalette.Licht, GraphicsDirector.MountPalette(k), k.ToString());
        }

        [TestCase("glut", VfxPalette.Lava)] [TestCase("frost", VfxPalette.Frost)] [TestCase("jade", VfxPalette.Jade)] [TestCase("verderbnis", VfxPalette.Tiefviolett)]
        public void BiomePalette_BossElement(string biome, VfxPalette expected) => Assert.AreEqual(expected, GraphicsDirector.BiomePalette(biome));

        [Test] public void TravelJump_DetectsTeleport() => Assert.IsTrue(GraphicsDirector.IsTravelJump(Vector3.zero, new Vector3(300, 0, 0), 0.016f));
        [Test] public void TravelJump_IgnoresRunningAndMounts() => Assert.IsFalse(GraphicsDirector.IsTravelJump(Vector3.zero, new Vector3(0.6f, 0, 0), 0.016f));
        [Test] public void TravelJump_IgnoresLowFramerateMovement() => Assert.IsFalse(GraphicsDirector.IsTravelJump(Vector3.zero, new Vector3(30f, 0, 0), 0.5f));

        [Test]
        public void RankColors_DistinctAndNotDanger()
        {
            var v = RankAuraFx.RankColor(RankAuraFx.Rank.Veteran, Color.white); var c = RankAuraFx.RankColor(RankAuraFx.Rank.Champion, Color.white);
            Assert.Greater(DangerReadability.DeltaE(v, c), 20f);
            Assert.IsFalse(DangerLanguage.Conflicts(v)); Assert.IsFalse(DangerLanguage.Conflicts(c));
        }
    }
}
