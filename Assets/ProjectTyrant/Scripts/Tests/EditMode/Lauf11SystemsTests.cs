using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 11 / M62–M66: Dünenläufer, Kopfgeld, Rennen, Angeln, Boot, Jadegeist, Aufzüge, Erfolge, Marken.</summary>
    public class Lauf11SystemsTests
    {
        // ---------------- Dünenläufer ----------------
        [Test] public void Dune_TwoCharges_ThirdWithMastery()
        {
            Assert.AreEqual(2, new DuneRunnerModel(0).MaxCharges); Assert.AreEqual(3, new DuneRunnerModel(1).MaxCharges);
        }

        [Test] public void Dune_Blink15m_ConsumesCharge()
        {
            var d = new DuneRunnerModel(0);
            Assert.IsTrue(d.TryBlink(true, Vector3.zero, new Vector3(0, 5, 2), out var to));
            Assert.AreEqual(15f, to.magnitude, 1e-3f); Assert.AreEqual(0f, to.y, 1e-4f, "flach");
            Assert.AreEqual(1, d.Charges);
            d.TryBlink(true, Vector3.zero, Vector3.forward, out _); Assert.IsFalse(d.TryBlink(true, Vector3.zero, Vector3.forward, out _), "leer");
        }

        [Test] public void Dune_RegenOnGroundOnly()
        {
            var d = new DuneRunnerModel(0); d.TryBlink(true, Vector3.zero, Vector3.forward, out _); d.TryBlink(true, Vector3.zero, Vector3.forward, out _);
            d.Tick(5f, false); Assert.AreEqual(0, d.Charges, "in der Luft keine Regeneration");
            d.Tick(1.5f, true); Assert.AreEqual(1, d.Charges); d.Tick(1.5f, true); Assert.AreEqual(2, d.Charges); d.Tick(10f, true); Assert.AreEqual(2, d.Charges);
        }

        [Test] public void Dune_AirBlinkOnlyWithSandgleiten()
        {
            Assert.IsFalse(new DuneRunnerModel(2).CanBlink(false)); Assert.IsTrue(new DuneRunnerModel(3).CanBlink(false));
        }

        [Test] public void Dune_PortalsOnlyMountedAndInRadius()
        {
            Assert.IsTrue(DuneRunnerModel.CanUsePortal(MountKind.DuneRunner, 2f, 0)); Assert.IsFalse(DuneRunnerModel.CanUsePortal(MountKind.Raptor, 1f, 3));
            Assert.IsFalse(DuneRunnerModel.CanUsePortal(MountKind.DuneRunner, 3.5f, 0)); Assert.IsTrue(DuneRunnerModel.CanUsePortal(MountKind.DuneRunner, 3.5f, 2), "Portalmeister");
            Assert.AreEqual(200f, DuneRunnerModel.EngageDefiance);
        }

        [Test] public void Dune_MountKindAndStats()
        {
            var s = MountStats.Default(MountKind.DuneRunner);
            Assert.AreEqual(MountKind.DuneRunner, s.Kind); Assert.Greater(s.Speed, MountStats.Default(MountKind.Raptor).Speed, "Sprinter");
        }

        // ---------------- Kopfgeld ----------------
        [Test] public void Bounty_PoolHasTenModifiers()
        {
            Assert.GreaterOrEqual(System.Enum.GetValues(typeof(BountyModifier)).Length, 10);
            foreach (BountyModifier m in System.Enum.GetValues(typeof(BountyModifier))) { Assert.IsTrue(BountyModel.Pressure.ContainsKey(m)); Assert.IsNotEmpty(BountyModel.ModifierName(m)); }
        }

        [Test] public void Bounty_RollAlwaysSolvable_CountByRank()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                var v = BountyModel.Roll(seed, false); var c = BountyModel.Roll(seed, true);
                Assert.AreEqual(2, v.Count); Assert.AreEqual(3, c.Count);
                Assert.IsTrue(BountyModel.Solvable(v)); Assert.IsTrue(BountyModel.Solvable(c));
                Assert.AreEqual(c.Count, new HashSet<BountyModifier>(c).Count, "verschieden");
            }
        }

        [Test] public void Bounty_CcBudgetAndForbiddenPairs()
        {
            Assert.IsFalse(BountyModel.Solvable(new[] { BountyModifier.FesselFelder, BountyModifier.ReittierAbwurf }), "max. eine Kontrolle");
            Assert.IsFalse(BountyModel.Solvable(new[] { BountyModifier.Orbkranz, BountyModifier.FesselFelder }));
            Assert.IsTrue(BountyModel.Solvable(new[] { BountyModifier.TrotzFenster, BountyModifier.Schnelligkeit, BountyModifier.Zorn }));
        }

        [Test] public void Bounty_StateMachine()
        {
            var b = new BountyModel("gesucht_1", true);
            Assert.IsTrue(b.Accept("2026-11-10", 7)); Assert.AreEqual(BountyState.Searching, b.State); Assert.AreEqual(3, b.Modifiers.Count);
            Assert.IsFalse(b.Accept("2026-11-10", 7), "läuft schon");
            Assert.IsFalse(b.Reach(80f)); Assert.IsTrue(b.Reach(30f)); Assert.AreEqual(BountyState.Spawned, b.State);
            Assert.AreEqual(BountyModel.ChampionTokens, b.Defeat("2026-11-10")); Assert.AreEqual(BountyState.Locked, b.State);
            Assert.IsFalse(b.Accept("2026-11-10", 8), "tägliche Sperre");
            b.NewDay("2026-11-11"); Assert.IsTrue(b.Accept("2026-11-11", 9));
        }

        [Test] public void Bounty_TimeLimit10Minutes()
        {
            var b = new BountyModel("g", false); b.Accept("d", 1); b.Reach(0f);
            b.Tick(599f); Assert.AreEqual(BountyState.Spawned, b.State); b.Tick(2f); Assert.AreEqual(BountyState.Failed, b.State);
            Assert.AreEqual(0, b.Defeat("d"));
        }

        // ---------------- Rennen ----------------
        static RaceModel Track() => new RaceModel("leere", new[] { new Vector3(0, 0, 50), new Vector3(0, 0, 100), new Vector3(0, 0, 150) }, 60f);

        [Test] public void Race_CheckpointsInOrder_BestTimeAndGhost()
        {
            var r = Track(); Assert.IsTrue(r.Start(MountKind.Raptor));
            Assert.IsFalse(r.Tick(1f, new Vector3(0, 0, 100)), "zweiter Checkpoint zählt erst nach dem ersten"); Assert.AreEqual(0, r.Next);
            r.Tick(1f, new Vector3(0, 0, 50)); r.Tick(1f, new Vector3(0, 0, 100)); Assert.IsTrue(r.Tick(1f, new Vector3(0, 0, 150)));
            Assert.AreEqual(RaceModel.State.Finished, r.Phase); Assert.AreEqual(4f, r.BestTimes[MountKind.Raptor], 1e-3f);
            Assert.Greater(r.Ghost.Count, 0, "Geisterfahrer aufgezeichnet");
        }

        [Test] public void Race_BestTimeOnlyImproves()
        {
            var r = Track();
            r.Start(MountKind.Raptor); r.Tick(1f, new Vector3(0, 0, 50)); r.Tick(1f, new Vector3(0, 0, 100)); r.Tick(1f, new Vector3(0, 0, 150));
            r.Start(MountKind.Raptor); r.Tick(5f, new Vector3(0, 0, 50)); r.Tick(5f, new Vector3(0, 0, 100)); r.Tick(5f, new Vector3(0, 0, 150));
            Assert.AreEqual(3f, r.BestTimes[MountKind.Raptor], 1e-3f);
        }

        [Test] public void Race_TimeLimit()
        {
            var r = Track(); r.Start(MountKind.Raptor); r.Tick(61f, Vector3.zero); Assert.AreEqual(RaceModel.State.TimedOut, r.Phase);
        }

        [Test] public void Race_ShortcutOnlyDuneRunner()
        {
            var r = Track(); r.Start(MountKind.Raptor); Assert.IsFalse(r.UseShortcut(0, 2));
            var d = Track(); d.Start(MountKind.DuneRunner); Assert.IsTrue(d.UseShortcut(0, 2)); Assert.AreEqual(2, d.Next);
        }

        // ---------------- Angeln ----------------
        [Test] public void Fish_CastDistanceByPower()
        {
            var f = new FishingModel(1); f.BeginCast(); f.ReleaseCast(); Assert.AreEqual(5f, f.Distance, 1e-3f);
            f = new FishingModel(1); f.BeginCast(); f.HoldCast(3f); f.ReleaseCast(); Assert.AreEqual(25f, f.Distance, 1e-3f);
        }

        [Test] public void Fish_BiteBetween2And8s()
        {
            for (int s = 0; s < 50; s++) { var f = new FishingModel(s); f.BeginCast(); f.ReleaseCast(); Assert.That(f.BiteAt, Is.InRange(2f, 8f)); }
            var g = new FishingModel(3); g.BeginCast(); g.ReleaseCast(); var pool = FishCatalog.For("gruene_flut");
            Assert.IsFalse(g.Wait(1.9f, pool, false)); Assert.IsTrue(g.Wait(7f, pool, false)); Assert.AreEqual(FishingModel.Phase.Hooked, g.State);
        }

        [Test] public void Fish_TensionGame_HoldInWindowCatches_OverReelLoses()
        {
            var pool = FishCatalog.For("gruene_flut");
            var f = new FishingModel(4); f.BeginCast(); f.ReleaseCast(); f.Wait(10f, pool, false);
            for (int i = 0; i < 4000 && f.State == FishingModel.Phase.Hooked; i++) f.Fight(0.02f, f.Tension < 0.45f);
            Assert.AreEqual(FishingModel.Phase.Caught, f.State, "Spannung im Fenster halten → Fang");
            var g = new FishingModel(5); g.BeginCast(); g.ReleaseCast(); g.Wait(10f, pool, false);
            for (int i = 0; i < 2000 && g.State == FishingModel.Phase.Hooked; i++) g.Fight(0.02f, true);
            Assert.AreEqual(FishingModel.Phase.Lost, g.State, "ständig einholen → Schnur reißt");
        }

        [Test] public void Fish_BetterRodMoreRare()
        {
            Assert.Greater(FishingModel.RarityWeight(FishRarity.Exotisch, 4), FishingModel.RarityWeight(FishRarity.Exotisch, 1));
            Assert.AreEqual(0f, FishingModel.RarityWeight(FishRarity.Legendaer, 1), "Legendär erst mit guter Rute/Köder/Boot");
            Assert.Greater(FishingModel.RarityWeight(FishRarity.Legendaer, 3), 0f);
        }

        [Test] public void Fish_CollectionsAtLeast8PerZone()
        {
            foreach (var z in FishCatalog.FishingZones)
            {
                var l = FishCatalog.For(z); Assert.GreaterOrEqual(l.Count, 8, z);
                var ids = new HashSet<string>(); foreach (var f in l) Assert.IsTrue(ids.Add(f.Id));
                Assert.IsFalse(FishCatalog.Complete(z, new HashSet<string>())); Assert.IsTrue(FishCatalog.Complete(z, ids));
            }
            Assert.AreEqual(0, FishCatalog.For("sonnfurt").Count);
        }

        [Test] public void Fish_NightFishOnlyAtNight()
        {
            var pool = new List<FishDef> { new FishDef("a", "A", "z", FishRarity.Einfach), new FishDef("n", "Nacht", "z", FishRarity.Einfach, true) };
            var f = new FishingModel(9);
            for (int i = 0; i < 100; i++) Assert.AreEqual("a", f.Roll(pool, false).Id);
        }

        // ---------------- Boot ----------------
        [Test] public void Boat_SummonOnlyOnWater()
        {
            var b = new BoatModel(); Assert.IsFalse(b.Summon(0.5f)); Assert.IsTrue(b.Summon(3f));
        }

        [Test] public void Boat_InertiaAndAnchor()
        {
            var b = new BoatModel(); b.Summon(3f);
            b.Step(0.1f, 1f, 0f); float early = b.Speed; for (int i = 0; i < 300; i++) b.Step(0.1f, 1f, 0f);
            Assert.Less(early, 1f, "Trägheit"); Assert.Greater(b.Speed, 5f); Assert.LessOrEqual(b.Speed, BoatModel.MaxSpeed + 1e-3f);
            b.ToggleAnchor(); for (int i = 0; i < 100; i++) b.Step(0.1f, 1f, 0f); Assert.AreEqual(0f, b.Speed, 1e-3f);
        }

        [Test] public void Boat_PassengersAndMasteries()
        {
            var b = new BoatModel(); b.Summon(2f); Assert.IsTrue(b.Board()); Assert.IsTrue(b.Board()); Assert.IsFalse(b.Board(), "max. 2");
            Assert.IsFalse(b.Boost(), "Jade-Schub erst Rang 2");
            var r = new BoatModel { Rank = 2 }; r.Summon(2f); float top = r.Top; Assert.IsTrue(r.Boost()); Assert.Greater(r.Top, top); Assert.IsFalse(r.Boost(), "Abklingzeit");
            Assert.AreEqual(BoatModel.MaxSpeed * 1.2f, new BoatModel { Rank = 1 }.Top, 1e-3f);
        }

        // ---------------- Jadegeist / Aufzüge ----------------
        [Test] public void JadeBot_RescueOncePer10Minutes()
        {
            var j = new JadeBotModel { Rank = 1 };
            Assert.AreEqual(0.5f, j.TryRescue()); Assert.AreEqual(0f, j.TryRescue());
            j.Tick(599f); Assert.AreEqual(0f, j.TryRescue()); j.Tick(1f); Assert.AreEqual(0.5f, j.TryRescue());
            Assert.AreEqual(0f, new JadeBotModel { Rank = 0 }.TryRescue());
        }

        [Test] public void JadeBot_RanksGateFeatures()
        {
            Assert.AreEqual(1f, new JadeBotModel { Rank = 1 }.GlideEnduranceFactor); Assert.AreEqual(1.5f, new JadeBotModel { Rank = 2 }.GlideEnduranceFactor, 1e-4f);
            Assert.IsFalse(new JadeBotModel { Rank = 2 }.CanPowerDevices); Assert.IsTrue(new JadeBotModel { Rank = 3 }.CanPowerDevices);
            Assert.AreEqual(0f, new JadeBotModel { Rank = 3 }.LootRange); Assert.AreEqual(20f, new JadeBotModel { Rank = 4 }.LootRange);
        }

        [Test] public void Elevator_CityFree_WorldNeedsJade()
        {
            var city = new ElevatorModel(new[] { 0f, 20f, 40f }, false);
            Assert.IsTrue(city.Call(2, null)); for (int i = 0; i < 200 && !city.Tick(0.1f); i++) { }
            Assert.AreEqual(2, city.Current); Assert.AreEqual(40f, city.Height, 1e-3f);
            var world = new ElevatorModel(new[] { 0f, 15f }, true);
            Assert.IsFalse(world.Call(1, new JadeBotModel { Rank = 2 })); Assert.IsTrue(world.Call(1, new JadeBotModel { Rank = 3 }));
            var bot = new JadeBotModel { Rank = 3 }; for (int i = 0; i < 4; i++) bot.UseDevice(); Assert.IsFalse(world.Call(1, bot), "Ladung leer"); bot.Recharge(); Assert.IsTrue(world.Call(1, bot));
        }

        // ---------------- Erfolge / Marken ----------------
        [Test] public void Achievements_PointsTitlesAndRegionCompletion()
        {
            var a = new AchievementModel(); int fired = 0; a.Completed += _ => fired++;
            var done = new HashSet<string>(); foreach (var s in RegionZones.InRegion(WorldRegion.Glutwueste)) done.Add("zonechest:" + s.Id);
            a.SyncRegions(done);
            Assert.IsTrue(a.Done.Contains("region_glutwueste")); Assert.IsFalse(a.Done.Contains("region_kernland"));
            Assert.AreEqual(25, a.Points); Assert.Contains("Dünenwanderer", a.Titles); Assert.AreEqual(1, fired);
            a.SyncRegions(done); Assert.AreEqual(25, a.Points, "nicht doppelt");
        }

        [Test] public void Achievements_CategoriesCovered()
        {
            foreach (AchievementCategory c in System.Enum.GetValues(typeof(AchievementCategory)))
            { int n = 0; foreach (var _ in new AchievementModel().InCategory(c)) n++; Assert.Greater(n, 0, c.ToString()); }
            var m = new AchievementModel(); for (int i = 0; i < 9; i++) Assert.IsFalse(m.Add("bounty_10")); Assert.IsTrue(m.Add("bounty_10")); Assert.IsFalse(m.Add("bounty_10"));
        }

        [Test] public void Currency_QuartermasterAndGifts()
        {
            var w = new Dictionary<string, int> { { RegionCurrency.BountyTokens, 40 } };
            Assert.IsTrue(RegionCurrency.Buy(w, RegionCurrency.Sonnentor[0])); Assert.AreEqual(10, w[RegionCurrency.BountyTokens]);
            Assert.IsFalse(RegionCurrency.Buy(w, RegionCurrency.Sonnentor[0]));
            Assert.IsFalse(RegionCurrency.Buy(w, RegionCurrency.Jadehafen[0]), "andere Marke");
            Assert.IsTrue(RegionCurrency.GiftForLegendary(new[] { "gift_jade" })); Assert.IsFalse(RegionCurrency.GiftForLegendary(new string[0]));
        }
    }
}
