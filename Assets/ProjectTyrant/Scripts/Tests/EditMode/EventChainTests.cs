using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class EventChainTests
    {
        const string ChainPath = "Assets/ProjectTyrant/Data/Events/EventChain_Grenzposten.asset";

        [Test]
        public void Chain_BranchesOnSuccessAndFailure_AndConvergesOnChampion()
        {
            var chain = AssetDatabase.LoadAssetAtPath<EventChain>(ChainPath);
            Assert.IsNotNull(chain);
            var first = chain.Get(chain.FirstStage);
            Assert.AreEqual(EventStageType.DefendSupplies, first.Type);
            var a = chain.Next(first, true);
            var b = chain.Next(first, false);
            Assert.AreEqual(EventStageType.AssaultCamp, a.Type, "Erfolg → 2a Gegenschlag");
            Assert.AreEqual(EventStageType.RecaptureOutpost, b.Type, "Fehlschlag → 2b Zurückerobern");
            Assert.AreEqual(EventStageType.ChampionBoss, chain.Next(a, true).Type);
            Assert.AreEqual(EventStageType.ChampionBoss, chain.Next(b, true).Type);
            Assert.IsNull(chain.Next(chain.Next(a, true), true), "Kette endet nach dem Champion");
            Assert.IsNotNull(chain.Trash); Assert.IsNotNull(chain.Veteran); Assert.IsNotNull(chain.Champion);
            Assert.AreEqual(EnemyTier.Champion, chain.Champion.Tier);
            Assert.IsTrue(chain.Champion.HasTelegraph);
            Assert.AreEqual(1.5f, chain.Champion.TelegraphDelay, 0.001f);
        }

        [Test]
        public void RewardTiers_FollowContributionThresholds()
        {
            Assert.AreEqual(RewardTier.None, EventChain.ComputeTier(0f, 1000f));
            Assert.AreEqual(RewardTier.Bronze, EventChain.ComputeTier(150f, 1000f));
            Assert.AreEqual(RewardTier.Silver, EventChain.ComputeTier(500f, 1000f));
            Assert.AreEqual(RewardTier.Gold, EventChain.ComputeTier(1000f, 1000f));
            Assert.Greater(EventChain.TierMultiplier(RewardTier.Gold), EventChain.TierMultiplier(RewardTier.Silver));
            Assert.Greater(EventChain.TierMultiplier(RewardTier.Silver), EventChain.TierMultiplier(RewardTier.Bronze));
        }
    }

    public class ZoneLayoutTests
    {
        [Test]
        public void PointsOfInterest_AreFlat_AndZoneIsFramed()
        {
            foreach (var a in Procedural.ZoneLayout.Areas)
            {
                float h = Procedural.ZoneLayout.Height(a.Center.x, a.Center.y);
                Assert.AreEqual(a.Height, h, 0.05f, a.Name + " muss eben sein");
                Assert.Less(Mathf.Abs(Procedural.ZoneLayout.Height(a.Center.x + 5, a.Center.y) - h), 0.3f);
            }
            Assert.Greater(Procedural.ZoneLayout.Height(99f, 0f), 12f, "Randberge rahmen die Zone");
            Assert.AreEqual(Procedural.ZoneLayout.Height(12.3f, -40.1f), Procedural.ZoneLayout.Height(12.3f, -40.1f), "deterministisch");
        }
    }
}
