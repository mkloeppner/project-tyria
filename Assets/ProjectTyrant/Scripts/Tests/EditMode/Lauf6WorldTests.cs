using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 6 / M30 + M32: Sandsturm, Verderbnis, Dunkelheit, Belagerung, Greif-Flug, Zonen-Freischaltung und die Inhalte der sechs neuen Zonen.</summary>
    public class Lauf6WorldTests
    {
        [Test]
        public void Sandstorm_CyclesCalmWarningStorm_AndSheltersProtect()
        {
            Assert.AreEqual(SandstormModel.Phase.Ruhig, SandstormModel.At(100f));
            Assert.AreEqual(SandstormModel.Phase.Warnung, SandstormModel.At(SandstormModel.StormStart - 10f));
            Assert.AreEqual(SandstormModel.Phase.Sturm, SandstormModel.At(SandstormModel.StormStart + 60f));
            Assert.AreEqual(SandstormModel.Phase.Ruhig, SandstormModel.At(SandstormModel.Period + 100f), "periodisch");
            Assert.AreEqual(1f, SandstormModel.Intensity(SandstormModel.StormStart + 60f), 1e-3f);
            Assert.AreEqual(0f, SandstormModel.Intensity(SandstormModel.StormStart - 1f), 1e-3f);
            Assert.Less(SandstormModel.SpeedFactor(1f, false), 0.7f);
            Assert.AreEqual(1f, SandstormModel.SpeedFactor(1f, true), 1e-4f);
            Assert.AreEqual(SandstormModel.StormStart - 100f, SandstormModel.UntilStorm(100f), 1e-3f);
        }

        [Test]
        public void Corruption_GrowsOverTime_ShrinksOnSuccess_LightProtects()
        {
            var m = new CorruptionModel(14f);
            m.Tick(60f * 5f); Assert.AreEqual(24f, m.Radius, 0.01f);
            m.Tick(60f * 60f); Assert.AreEqual(m.MaxRadius, m.Radius, 0.01f, "Obergrenze");
            m.EventSucceeded(); Assert.AreEqual(m.MaxRadius - m.ShrinkOnSuccess, m.Radius, 0.01f);
            for (int i = 0; i < 10; i++) m.EventSucceeded();
            Assert.AreEqual(m.MinRadius, m.Radius, 0.01f, "Untergrenze");
            Assert.AreEqual(2, m.TormentPerSecond(1f, false, false));
            Assert.AreEqual(0, m.TormentPerSecond(1f, true, false), "Laterne");
            Assert.AreEqual(0, m.TormentPerSecond(1f, false, true), "Beherrschung");
            Assert.AreEqual(0, m.TormentPerSecond(m.Radius + 1f, false, false), "außerhalb");
        }

        [Test]
        public void Darkness_LanternAndCrystalsGiveLight()
        {
            Assert.IsFalse(DarknessModel.InLight(false, 50f));
            Assert.IsTrue(DarknessModel.InLight(false, DarknessModel.CrystalRadius - 1f));
            Assert.IsTrue(DarknessModel.InLight(true, 999f));
            Assert.Greater(DarknessModel.Vision(true, 999f), DarknessModel.Vision(false, 5f));
            Assert.Greater(DarknessModel.Vision(false, 5f), DarknessModel.Vision(false, 50f));
            Assert.Greater(DarknessModel.FogDensity(DarknessModel.BaseVision), DarknessModel.FogDensity(30f), "dunkel = dichter Nebel");
        }

        [Test]
        public void Siege_GatesFallInOrder_FailResets_ThroneOpensAfterThreeGates()
        {
            var s = new SiegeModel();
            s.Succeed("belagerung_tor2"); Assert.AreEqual(0, s.Phase, "falsche Reihenfolge zählt nicht");
            s.Succeed("belagerung_tor1"); s.Succeed("belagerung_tor2"); Assert.AreEqual(2, s.GatesDown); Assert.IsFalse(s.ThroneOpen);
            s.Fail("belagerung_tor3"); Assert.AreEqual(0, s.GatesDown, "Scheitern → zurück zu Tor 1");
            foreach (var id in SiegeModel.Phases.Take(4)) s.Succeed(id);
            Assert.AreEqual(3, s.GatesDown); Assert.IsTrue(s.ThroneOpen);
            s.Succeed("belagerung_thron"); Assert.IsTrue(s.Completed);
        }

        [Test]
        public void Griffon_DiveAccelerates_ReleaseClimbs_LandResets()
        {
            var g = new GriffonFlight();
            g.Tick(0.1f, true, false, out _); Assert.AreEqual(g.BaseSpeed, g.Speed, 1e-3f); Assert.AreEqual(g.GlideSink, g.Sink, 1e-3f);
            for (int i = 0; i < 10; i++) g.Tick(0.1f, true, true, out _);
            Assert.IsTrue(g.Diving); Assert.Greater(g.Speed, g.BaseSpeed + 5f); Assert.AreEqual(g.DiveSink, g.Sink, 1e-3f);
            g.Tick(0.1f, true, false, out float climb); Assert.Greater(climb, 3f, "Abfangen"); Assert.IsFalse(g.Diving);
            g.Endurance = true; Assert.Less(g.Sink, g.GlideSink, "Beherrschung „Ausdauer“");
            g.DiveUnlocked = false; g.Tick(0.1f, true, true, out _); Assert.IsFalse(g.Diving, "ohne Beherrschung kein Sturzflug");
            g.Tick(0.1f, false, false, out _); Assert.AreEqual(0f, g.Boost, 1e-4f);
        }

        [Test]
        public void ZoneGate_FortressOpensWithChapter10()
        {
            var s = StoryState.NewCharacter(Origin.Hof, Trait.Charme, Lodestar.Raetsel);
            Assert.IsTrue(ZoneGate.Allowed("silberkueste", s, out _));
            Assert.IsFalse(ZoneGate.Allowed("tyrannenfeste", s, out var why)); Assert.IsNotEmpty(why);
            s.SetFlag(ZoneGate.FortressFlag); Assert.IsTrue(ZoneGate.Allowed("tyrannenfeste", s, out _));
        }

        [Test]
        public void NewZones_HaveFullCompletionContent()
        {
            foreach (var id in WorldBuilder.Lauf6Zones)
            {
                var l = BiomeZoneContent.For(id); Assert.IsNotNull(l, id);
                Assert.GreaterOrEqual(l.Waypoints.Length, 4, id + " Wegmarken"); Assert.GreaterOrEqual(l.Pois.Length, 4, id + " POI");
                Assert.GreaterOrEqual(l.Vistas.Length, 3, id + " Aussicht"); Assert.GreaterOrEqual(l.Challenges.Length, 4, id + " HC");
                Assert.GreaterOrEqual(l.Insights.Length, 3, id + " Einblicke"); Assert.GreaterOrEqual(l.Hearts.Length, 3, id + " Herzen");
                Assert.GreaterOrEqual(l.Archetypes.Length, 3, id + " Gegnertypen");
                Assert.IsTrue(l.Vistas.Any(v => v.Gate != SpotGate.None) && l.Challenges.Any(c => c.Gate != SpotGate.None), id + ": Reittier-Regel");
                foreach (var h in l.Hearts) Assert.GreaterOrEqual(h.Tasks.Length, 2, h.Id + ": ≥ 2 Beitragswege (+ Gegner)");
                var ids = l.AllSpots().Select(s => s.Id).ToList(); Assert.AreEqual(ids.Count, ids.Distinct().Count(), id + ": doppelte Ids");
            }
        }

        [Test]
        public void World_PortalsValid_AndPlazaReachesEveryZone()
        {
            var w = WorldBuilder.LoadWorld(); Assert.IsNotNull(w);
            Assert.AreEqual(10, w.Zones.Count, "zehn Zonen");
            Assert.IsEmpty(WorldModel.ValidatePortals(w.Zones), string.Join(", ", WorldModel.ValidatePortals(w.Zones)));
            var kueste = w.Zone("silberkueste");
            foreach (var z in w.Zones) if (z.Id != "silberkueste") Assert.IsTrue(kueste.Portals.Any(p => p.TargetZone == z.Id && p.Id.StartsWith("plaza_")), "Portalplatz → " + z.Id);
        }
    }
}
