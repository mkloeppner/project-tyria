using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 4 / M18: Beherrschungs-Training, Punkte, Wirkungen, Speicherstand.</summary>
    public class MasteryTests
    {
        static MasteryModel Model()
        {
            var m = new MasteryModel();
            m.SetCosts(MasteryLine.Gleiten, new[] { new MasteryRankCost { Name = "G1", Xp = 1000, Points = 1 }, new MasteryRankCost { Name = "G2", Xp = 2000, Points = 2 } });
            m.SetCosts(MasteryLine.Umwelt, new[] { new MasteryRankCost { Xp = 500, Points = 1 }, new MasteryRankCost { Xp = 500, Points = 1 }, new MasteryRankCost { Xp = 500, Points = 1 }, new MasteryRankCost { Xp = 500, Points = 1 } });
            m.SetCosts(MasteryLine.Reittiere, new[] { new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 } });
            m.SetCosts(MasteryLine.Kommandant, new[] { new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 } });
            return m;
        }

        [Test]
        public void Xp_GoesIntoSelectedLine_RankNeedsPoints()
        {
            var m = Model();
            m.AddXp(5000);
            Assert.AreEqual(0, m.Rank(MasteryLine.Gleiten), "ohne gewählte Linie geht nichts in einen Rang");
            Assert.AreEqual(5000, m.TotalXp);
            m.Selected = MasteryLine.Gleiten;
            m.AddXp(1500);
            Assert.AreEqual(0, m.Rank(MasteryLine.Gleiten), "trainiert, aber kein Punkt");
            Assert.AreEqual(1f, m.Progress(MasteryLine.Gleiten));
            int learned = 0; m.RankLearned += (l, r) => learned++;
            m.AddPoints(1);
            Assert.AreEqual(1, m.Rank(MasteryLine.Gleiten));
            Assert.AreEqual(1, learned);
            Assert.AreEqual(0, m.PointsAvailable);
            Assert.AreEqual(0f, m.Progress(MasteryLine.Gleiten), "Überschuss verfällt nicht in den nächsten Rang (Deckelung)");
        }

        [Test]
        public void PointsSpent_AreDerivedFromRanks_MaxedClearsSelection()
        {
            var m = Model();
            m.AddPoints(3);
            m.Selected = MasteryLine.Gleiten;
            m.AddXp(1000); m.AddXp(2000);
            Assert.AreEqual(2, m.Rank(MasteryLine.Gleiten));
            Assert.AreEqual(3, m.PointsSpent);
            Assert.IsTrue(m.IsMaxed(MasteryLine.Gleiten));
            Assert.IsNull(m.Selected);
        }

        [Test]
        public void Effects_FollowRanks()
        {
            var m = Model();
            Assert.IsFalse(MasteryEffects.CanGlide(m));
            Assert.AreEqual(1f, MasteryEffects.LavaFactor(m));
            Assert.AreEqual(0f, MasteryEffects.TravelDiscount(m));
            m.AddPoints(20);
            foreach (var l in new[] { MasteryLine.Gleiten, MasteryLine.Umwelt, MasteryLine.Reittiere, MasteryLine.Kommandant })
            { m.Selected = l; while (!m.IsMaxed(l)) m.AddXp(m.XpNeeded(l)); }
            Assert.IsTrue(MasteryEffects.CanGlide(m) && MasteryEffects.UsesUpdrafts(m));
            Assert.IsTrue(MasteryEffects.PoisonMistImmune(m) && MasteryEffects.CanBounce(m) && MasteryEffects.SeesHiddenChests(m));
            Assert.AreEqual(0f, MasteryEffects.LavaFactor(m));
            Assert.AreEqual(1.25f, MasteryEffects.MountHealth(m));
            Assert.AreEqual(1.2f, MasteryEffects.Leap(m));
            Assert.Less(MasteryEffects.Charge(m), 1f);
            Assert.IsTrue(MasteryEffects.AutoLoot(m));
            Assert.AreEqual(0.4f, MasteryEffects.TravelDiscount(m), 1e-4f);
        }

        [Test]
        public void Save_RoundTripsMastery()
        {
            var st = new ProfileState();
            st.Mastery.SetCosts(MasteryLine.Gleiten, new[] { new MasteryRankCost { Xp = 100, Points = 1 }, new MasteryRankCost { Xp = 100, Points = 1 } });
            st.Mastery.AddPoints(3); st.Mastery.Selected = MasteryLine.Gleiten; st.Mastery.AddXp(100); st.Mastery.AddXp(40);
            var back = new ProfileState();
            back.LoadDto(JsonUtility.FromJson<SaveGameDto>(JsonUtility.ToJson(st.ToDto())), null, null);
            Assert.AreEqual(1, back.Mastery.Rank(MasteryLine.Gleiten));
            Assert.AreEqual(40, back.Mastery.Tracks[MasteryLine.Gleiten].Xp);
            Assert.AreEqual(3, back.Mastery.PointsEarned);
            Assert.AreEqual(140, back.Mastery.TotalXp);
            Assert.AreEqual(MasteryLine.Gleiten, back.Mastery.Selected);
        }

        [Test]
        public void TravelCost_UsesCommanderDiscount()
        {
            int full = WorldModel.TravelCost(Vector3.zero, new Vector3(100, 0, 0), 30, true, 0f);
            int disc = WorldModel.TravelCost(Vector3.zero, new Vector3(100, 0, 0), 30, true, 0.4f);
            Assert.AreEqual(Mathf.RoundToInt(full * 0.6f), disc);
        }
    }
}
