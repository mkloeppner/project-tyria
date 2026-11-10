using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Balance;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 11 / M60–M65: 20 Regions-Zonen – Vollständigkeit (Abschnitt 3 Punkt 6), Level, Gefahrenstufen, Regionen, Fahrplan-Bezug.</summary>
    public class Lauf11RegionTests
    {
        static IEnumerable<RegionZoneSpec> All => RegionZones.All;

        [Test] public void Twenty_Zones_8_6_6()
        {
            Assert.AreEqual(20, RegionZones.All.Length);
            int k = 0, g = 0, d = 0; foreach (var s in All) { if (s.Region == WorldRegion.Kernland) k++; else if (s.Region == WorldRegion.Glutwueste) g++; else d++; }
            Assert.AreEqual(8, k); Assert.AreEqual(6, g); Assert.AreEqual(6, d);
            var ids = new HashSet<string>(); foreach (var s in All) Assert.IsTrue(ids.Add(s.Id), "doppelte Id " + s.Id);
        }

        [Test] public void Every_Zone_Has_FullCompletion()
        {
            foreach (var s in All)
            {
                var l = BiomeZoneContent.For(s.Id);
                Assert.IsNotNull(l, s.Id);
                Assert.GreaterOrEqual(l.Waypoints.Length, 4, s.Id + " Wegmarken"); Assert.GreaterOrEqual(l.Pois.Length, 4, s.Id + " POIs");
                Assert.GreaterOrEqual(l.Vistas.Length, 3, s.Id + " Aussichtspunkte"); Assert.GreaterOrEqual(l.Challenges.Length, 4, s.Id + " Heldenherausforderungen");
                Assert.GreaterOrEqual(l.Hearts.Length, 4, s.Id + " Herzen"); Assert.GreaterOrEqual(l.Insights.Length, 3, s.Id + " Einblicke");
                Assert.GreaterOrEqual(l.Archetypes.Length, 3, s.Id + " Gegnerarchetypen");
                Assert.GreaterOrEqual(l.Critters.Length, 2, s.Id + " Ambient-Tiere");
                if (!s.Hub) Assert.GreaterOrEqual(l.Camps.Length, 8, s.Id + " Lager");
            }
        }

        [Test] public void Content_InsideZone_SpacedAndUniqueIds()
        {
            foreach (var s in All)
            {
                var l = RegionZones.Layout(s.Id); var ids = new HashSet<string>();
                foreach (var sp in l.AllSpots())
                {
                    Assert.IsTrue(ids.Add(sp.Id), s.Id + " doppelt: " + sp.Id);
                    Assert.LessOrEqual(Mathf.Abs(sp.Pos.x), s.Size * 0.45f, sp.Id); Assert.LessOrEqual(Mathf.Abs(sp.Pos.y), s.Size * 0.45f, sp.Id);
                }
                foreach (var h in l.Hearts) { Assert.IsTrue(ids.Add(h.Id), h.Id); Assert.AreEqual(2, h.Tasks.Length); Assert.IsNotEmpty(h.Npc); }
            }
        }

        [Test] public void Layout_Deterministic()
        {
            var a = RegionZones.Layout("grosse_leere"); var b = RegionZones.Layout("grosse_leere");
            Assert.AreSame(a, b);
            Assert.AreEqual(a.Waypoints[2].Pos, b.Waypoints[2].Pos);
        }

        [Test] public void Levels_KernlandBands_ExpansionsAt80()
        {
            foreach (var s in All)
            {
                if (s.Region == WorldRegion.Kernland) { Assert.That(s.LevelMin, Is.InRange(30, 75), s.Id); Assert.That(s.LevelMax, Is.InRange(40, 80), s.Id); Assert.LessOrEqual(s.LevelMax - s.LevelMin, 10, s.Id); }
                else { Assert.AreEqual(80, s.LevelMin, s.Id); Assert.AreEqual(80, s.LevelMax, s.Id); }
                var l = RegionZones.Layout(s.Id);
                foreach (var sp in l.AllSpots()) Assert.That(sp.Level, Is.InRange(s.LevelMin, s.LevelMax), sp.Id);
            }
        }

        [Test] public void Kernland_TwoZonesPerBand()
        {
            var bands = new Dictionary<int, int>();
            foreach (var s in RegionZones.InRegion(WorldRegion.Kernland)) { int b = s.LevelMin / 10; bands[b] = bands.TryGetValue(b, out var n) ? n + 1 : 1; }
            int pairs = 0; foreach (var kv in bands) if (kv.Value >= 2) pairs++;
            Assert.GreaterOrEqual(pairs, 2, "Wahl zwischen zwei Zonen je Band (30–40, 70–80 …)");
        }

        [Test] public void XpSimulation_KernlandZonesCarryTheirBand()
        {
            foreach (var s in RegionZones.InRegion(WorldRegion.Kernland))
            {
                var l = RegionZones.Layout(s.Id);
                System.Func<BzSpot[], int[]> lv = a => System.Array.ConvertAll(a, x => x.Level);
                long xp = XpRewards.ZoneXp(lv(l.Waypoints), lv(l.Pois), lv(l.Vistas), lv(l.Challenges), lv(l.Insights), System.Array.ConvertAll(l.Hearts, h => h.Level), (s.LevelMin + s.LevelMax) / 2, 6, 2, s.LevelMax);
                Assert.GreaterOrEqual(xp, XpRewards.BandXp(s.LevelMin, s.LevelMax), s.Id);
            }
        }

        [Test] public void XpSimulation_30To80_OverKernlandAlone()
        {
            long total = 0;
            foreach (var s in RegionZones.InRegion(WorldRegion.Kernland))
            {
                var l = RegionZones.Layout(s.Id);
                System.Func<BzSpot[], int[]> lv = a => System.Array.ConvertAll(a, x => x.Level);
                total += XpRewards.ZoneXp(lv(l.Waypoints), lv(l.Pois), lv(l.Vistas), lv(l.Challenges), lv(l.Insights), System.Array.ConvertAll(l.Hearts, h => h.Level), (s.LevelMin + s.LevelMax) / 2, 6, 2, s.LevelMax);
            }
            Assert.GreaterOrEqual(total, XpRewards.BandXp(30, 80));
        }

        [Test] public void Danger_EntryLimits_HubsZero_NoJumps()
        {
            foreach (var s in All)
            {
                Assert.LessOrEqual(s.DangerEntry, RegionZones.MaxEntryDanger(s.Region), s.Id + " Eingang");
                Assert.That(s.DangerCore, Is.InRange(s.DangerEntry, 6), s.Id);
                if (s.Hub) Assert.AreEqual(0, s.DangerEntry, s.Id);
                var spec = ZoneDangerTable.RegionSpec(s.Id);
                Assert.AreEqual(s.DangerEntry, spec.Entry); Assert.AreEqual(s.DangerCore, spec.Core);
                if (s.Hub) Assert.IsNotNull(spec.Cities);
            }
            foreach (var id in new[] { "zarathis", "herz_der_flut", "schaedelkueste" }) Assert.AreEqual(6, RegionZones.Get(id).DangerCore, id + ": Drachenland");
        }

        [Test] public void Danger_MapBuildsWithoutStageJumps()
        {
            foreach (var s in All)
            {
                var l = RegionZones.Layout(s.Id);
                var regions = new List<(string, Vector2, float)>(); foreach (var p in l.Pois) regions.Add((p.Name, p.Pos, 110f));
                var wps = new List<Vector2>(); foreach (var w in l.Waypoints) wps.Add(w.Pos);
                var spawn = RegionZones.Spawn(s);
                var m = ZoneDangerTable.Build(s.Id, s.Size, new Vector2(spawn.x, spawn.z), new[] { new Vector2(spawn.x, spawn.z) }, wps, regions, false);
                for (int i = 1; i < m.BandLevels.Count; i++) Assert.LessOrEqual(m.BandLevels[i] - m.BandLevels[i - 1], 1, s.Id + " Band " + i);
            }
        }

        [Test] public void Regions_HaveHubAndNames()
        {
            Assert.IsTrue(RegionZones.Get("sonnentor").Hub); Assert.IsTrue(RegionZones.Get("jadehafen").Hub);
            Assert.AreEqual("Glutwüste", RegionZones.RegionName(WorldRegion.Glutwueste));
            Assert.AreEqual(RegionZones.Get("sonnentor").Region, WorldRegion.Glutwueste); Assert.AreEqual(RegionZones.Get("jadehafen").Region, WorldRegion.Dracheninseln);
        }

        [Test] public void Schedule_ZonesExist()
        {
            foreach (var e in WorldBossSchedule.All())
            {
                bool found = LevelBands.TryGet(e.Zone, out _) || RegionZones.Get(e.Zone) != null;
                Assert.IsTrue(found, e.Id + " → Zone " + e.Zone);
            }
        }

        [Test] public void Signature_And_Tips()
        {
            foreach (var s in All) { Assert.IsNotEmpty(s.Tip, s.Id); Assert.IsNotEmpty(s.Signature, s.Id); Assert.GreaterOrEqual(s.Places.Length, 8, s.Id); }
        }
    }
}
