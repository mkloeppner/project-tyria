using NUnit.Framework;
using ProjectTyrant.Balance;

namespace ProjectTyrant.Tests
{
    /// <summary>Belebung: Zieldichte, Lagerabstand, Gruppengrößen und Veteranen nach Gefahrenstufe (fair wie Balance B1).</summary>
    public class LivelinessTests
    {
        [Test] public void Level0_GetsNoExtraSpawns() => Assert.AreEqual(0, PopulationModel.TargetSpawns(1e6f, DangerTable.Default(0)));

        [Test] public void Density_GrowsWithDanger()
        {
            int prev = 0;
            foreach (int l in new[] { 1, 2, 3, 4 })
            {
                int t = PopulationModel.TargetSpawns(1e6f, DangerTable.Default(l));
                Assert.Greater(t, prev, "Stufe " + l); prev = t;
            }
            Assert.Greater(PopulationModel.TargetSpawns(1e6f, DangerTable.Default(3)), 20, "dichter als vorher (≈ 20/km²)");
        }

        [Test] public void CampSpacing_KeepsCampsApart()
        {
            for (int l = 0; l <= DangerTable.MaxLevel; l++)
            {
                var s = DangerTable.Default(l);
                Assert.GreaterOrEqual(PopulationModel.CampSpacing(s), 2f * Units.M(s.AggroRange) + 8f - 0.01f, "Stufe " + l);
                Assert.Greater(PopulationModel.CampSpacing(s), 20f);
            }
        }

        [Test] public void CampSize_RespectsGroupLimit()
        {
            for (int l = 0; l <= DangerTable.MaxLevel; l++)
            {
                var s = DangerTable.Default(l);
                for (int i = 0; i < 12; i++)
                {
                    int n = PopulationModel.CampSize(s, i);
                    Assert.GreaterOrEqual(n, 1); Assert.LessOrEqual(n, System.Math.Max(1, s.MaxGroupSize), "Stufe " + l);
                }
            }
            Assert.AreEqual(1, PopulationModel.CampSize(DangerTable.Default(0), 0), "Stufe 0: einzeln");
            Assert.LessOrEqual(PopulationModel.CampSize(DangerTable.Default(1), 0), 2, "Stufe 1: höchstens Paare");
        }

        [Test] public void Veterans_OnlyWhereAllowed_OnePerFiveCamps()
        {
            Assert.IsFalse(PopulationModel.VeteranCamp(DangerTable.Default(0), 4));
            var s = DangerTable.Default(3);
            int v = 0; for (int i = 0; i < 20; i++) if (PopulationModel.VeteranCamp(s, i)) v++;
            Assert.AreEqual(4, v);
        }

        [Test] public void Missing_ClampedAndNeverNegative()
        {
            Assert.AreEqual(0, PopulationModel.Missing(50, 30));
            Assert.AreEqual(10, PopulationModel.Missing(20, 30));
            Assert.AreEqual(PopulationModel.MaxAddPerZone, PopulationModel.Missing(0, 1000));
        }
    }
}
