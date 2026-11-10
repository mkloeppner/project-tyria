using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Fix-Lauf F1 – Wegmarken-Sicherheit: Eingänge × Wegmarken ≤ 40 m, Besiegt-Optionen nie leer, Auto-Freischaltung, Migration.</summary>
    public class WaypointSafetyTests
    {
        static List<ZoneDefinition> AllZones()
        {
            var l = new List<ZoneDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:ZoneDefinition")) { var z = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(AssetDatabase.GUIDToAssetPath(g)); if (z != null) l.Add(z); }
            Assert.GreaterOrEqual(l.Count, 4, "Zonen-Definitionen (Rebuild)");
            return l;
        }

        static ZoneDefinition Zone(string id, Vector3 spawn, params (string id, Vector3 pos)[] wps)
        {
            var z = ScriptableObject.CreateInstance<ZoneDefinition>(); z.Id = id; z.DisplayName = id; z.DefaultSpawn = spawn;
            foreach (var w in wps) z.Waypoints.Add(new ZoneWaypointInfo { Id = w.id, Name = "WP " + w.id, Position = w.pos });
            return z;
        }

        [Test]
        public void AllZones_EveryEntranceHasWaypointWithin40m()
        {
            var bad = new List<string>();
            foreach (var z in AllZones())
            {
                bad.AddRange(WaypointSafety.Uncovered(z));
                var entrances = WaypointSafety.Entrances(z);
                Assert.AreEqual(entrances.Count, z.Arrivals.Count, z.Id + ": Ankunfts-Zuordnung je Eingang");
                foreach (var a in z.Arrivals)
                {
                    var w = z.Waypoints.Find(x => x.Id == a.WaypointId);
                    if (w == null) { bad.Add(z.Id + "/" + a.Entrance + ": Wegmarke " + a.WaypointId + " fehlt"); continue; }
                    if (WaypointSafety.FlatDistance(w.Position, a.Position) > WaypointSafety.MaxDistance) bad.Add(z.Id + "/" + a.Entrance + ": " + Mathf.RoundToInt(WaypointSafety.FlatDistance(w.Position, a.Position)) + " m");
                }
            }
            // Städte gibt es erst ab Lauf 7 – sie laufen über dieselben ZoneDefinition-Eingänge und damit durch diesen Test.
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void AllZones_DefeatOptionsNeverEmpty_WithNothingUnlocked()
        {
            var zones = AllZones();
            foreach (var z in zones)
            {
                var opts = WaypointSafety.DefeatOptions(z, zones, new HashSet<string>(), null, z.DefaultSpawn + new Vector3(15f, 0f, 15f));
                Assert.IsNotEmpty(opts, z.Id);
                Assert.IsTrue(opts[0].Emergency && opts[0].Zone == z, z.Id + ": Notfall-Wegmarke der Zone");
                Assert.AreEqual(WaypointSafety.ArrivalFor(z, WaypointSafety.SpawnEntrance).Id, opts[0].Waypoint.Id, z.Id + ": nächste Ankunfts-Wegmarke");
            }
            var sonn = zones.Find(z => z.Id == "sonnfurt");
            Assert.AreEqual("wp_sonnfurt", WaypointSafety.ArrivalFor(sonn, WaypointSafety.SpawnEntrance).Id, "Charakterstart → Wegmarke Sonnfurt");
        }

        [Test]
        public void DefeatOptions_OpenSortedContestedFallbackAndOtherZones()
        {
            var a = Zone("a", Vector3.zero, ("a1", new Vector3(10, 0, 0)), ("a2", new Vector3(100, 0, 0)), ("a3", new Vector3(300, 0, 0)));
            var b = Zone("b", Vector3.zero, ("b1", new Vector3(0, 0, 5)));
            var unlocked = new HashSet<string> { "a2", "a3", "b1" };
            var o = WaypointSafety.DefeatOptions(a, new[] { a, b }, unlocked, null, new Vector3(280, 0, 0));
            Assert.AreEqual("a3", o[0].Waypoint.Id, "nächste freie zuerst"); Assert.AreEqual("a2", o[1].Waypoint.Id);
            Assert.IsFalse(o[0].Emergency); Assert.IsTrue(o[2].OtherZone); Assert.AreEqual("b1", o[2].Waypoint.Id);
            // alle entdeckten umkämpft → Notfall-Wegmarke (Ankunft), andere Zonen weiter angeboten
            o = WaypointSafety.DefeatOptions(a, new[] { a, b }, unlocked, p => true, new Vector3(280, 0, 0));
            Assert.IsTrue(o[0].Emergency); Assert.AreEqual("a1", o[0].Waypoint.Id, "Ankunft am Spawn, auch wenn umkämpft");
            Assert.IsTrue(o.Exists(x => x.OtherZone && x.Waypoint.Id == "b1"));
            // umkämpfte Ankunft wird übersprungen, wenn eine freie Ankunft existiert
            a.Portals.Add(new PortalDef { Id = "p", Position = new Vector3(300, 0, -6), Yaw = 0f });
            o = WaypointSafety.DefeatOptions(a, new[] { a }, new HashSet<string>(), p => p.x < 50f, new Vector3(0, 0, 0));
            Assert.IsTrue(o[0].Emergency); Assert.AreEqual("a3", o[0].Waypoint.Id, "freie Portal-Ankunft statt umkämpfter Spawn-Ankunft");
            Assert.IsNotEmpty(WaypointSafety.DefeatOptions(Zone("leer", Vector3.zero, ("x", new Vector3(500, 0, 0))), null, null, null, Vector3.zero), "auch ohne Ankunft in 40 m: nächste Wegmarke");
        }

        [Test]
        public void UnlockOnEnter_PortalAndSpawn_Once()
        {
            var z = Zone("z", new Vector3(0, 0, 0), ("spawnwp", new Vector3(5, 0, 5)), ("portalwp", new Vector3(200, 0, 10)));
            z.Portals.Add(new PortalDef { Id = "nord", Position = new Vector3(200, 0, 0), Yaw = 0f });
            var unlocked = new HashSet<string>();
            Assert.AreEqual("portalwp", WaypointSafety.UnlockOnEnter(z, WaypointSafety.PortalEntrance("nord"), unlocked).Id);
            Assert.IsNull(WaypointSafety.UnlockOnEnter(z, WaypointSafety.PortalEntrance("nord"), unlocked), "nur einmal");
            Assert.AreEqual("spawnwp", WaypointSafety.UnlockOnEnter(z, WaypointSafety.SpawnEntrance, unlocked).Id);
            Assert.AreEqual(2, unlocked.Count);
            // Ankunftspunkt = 6 m vor dem Portal (wie der Zonenwechsel)
            Assert.AreEqual(6f, WaypointSafety.FlatDistance(WaypointSafety.ArrivalPoint(z.Portals[0]), z.Portals[0].Position), 0.01f);
        }

        [Test]
        public void Migration_UnlocksArrivalsOfVisitedZonesOnly()
        {
            var a = Zone("a", Vector3.zero, ("a_spawn", new Vector3(3, 0, 0)), ("a_far", new Vector3(500, 0, 0)));
            var b = Zone("b", Vector3.zero, ("b_spawn", new Vector3(0, 0, 3)));
            var unlocked = new List<string> { "a_far" };
            int n = WaypointSafety.Migrate(unlocked, new[] { a, b }, new[] { "a" });
            Assert.AreEqual(1, n); CollectionAssert.Contains(unlocked, "a_spawn"); CollectionAssert.DoesNotContain(unlocked, "b_spawn");
            Assert.AreEqual(0, WaypointSafety.Migrate(unlocked, new[] { a, b }, new[] { "a" }), "idempotent");
        }

        [Test]
        public void WorldFloor_FallingOutOfMapTriggersRescue()
        {
            // QA-Fund F1-Dauertest: Spieler fiel neben der Karte auf y = -37.000 ohne Rückholung
            Assert.IsFalse(WaypointSafety.BelowWorld(new Vector3(0f, -20f, 0f)), "Höhlen/Täler bleiben unberührt");
            Assert.IsFalse(WaypointSafety.BelowWorld(new Vector3(0f, WaypointSafety.WorldFloorY + 1f, 0f)));
            Assert.IsTrue(WaypointSafety.BelowWorld(new Vector3(0f, -37131f, 0f)));
            Assert.IsTrue(WaypointSafety.BelowWorld(new Vector3(0f, float.NaN, 0f)), "kaputte Position zählt als gefallen");
            Assert.Less(WaypointSafety.WorldFloorY, -100f, "deutlich unter jedem Zonen-Boden");
        }
    }
}
