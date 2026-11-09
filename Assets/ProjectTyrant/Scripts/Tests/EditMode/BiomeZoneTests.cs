using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 4 / M16: Inhalte der drei Biom-Zonen und die Erreichbarkeits-Flutfüllung.</summary>
    public class BiomeZoneTests
    {
        [Test]
        public void EachZone_MeetsMinimumContent()
        {
            Assert.AreEqual(3, BiomeZoneContent.All.Length);
            foreach (var l in BiomeZoneContent.All)
            {
                Assert.GreaterOrEqual(l.Waypoints.Length, 4, l.ZoneId + " Wegmarken");
                Assert.GreaterOrEqual(l.Pois.Length, 4, l.ZoneId + " POIs");
                Assert.GreaterOrEqual(l.Vistas.Length, 3, l.ZoneId + " Aussichtspunkte");
                Assert.GreaterOrEqual(l.Challenges.Length, 4, l.ZoneId + " Heldenherausforderungen");
                Assert.GreaterOrEqual(l.Hearts.Length, 3, l.ZoneId + " Herzen");
                Assert.GreaterOrEqual(l.Insights.Length, 3, l.ZoneId + " Einblicke");
                var kinds = new HashSet<string>(); foreach (var c in l.Critters) kinds.Add(c.Kind);
                Assert.GreaterOrEqual(kinds.Count, 2, l.ZoneId + " Tierarten");
                Assert.GreaterOrEqual(l.Archetypes.Length, 3, l.ZoneId + " Gegnertypen");
                foreach (var camp in l.Camps) CollectionAssert.Contains(l.Archetypes, camp.Archetype);
            }
        }

        [Test]
        public void Ids_AreUniqueAcrossAllZones_AndSpotsInsideZone()
        {
            var ids = new HashSet<string>(ZoneContentIdsStartZone());
            var sizes = new Dictionary<string, float> { { "nebelmoor", 1000f }, { "frostkamm", 1100f }, { "gluetoede", 1200f } };
            foreach (var l in BiomeZoneContent.All)
            {
                float lim = sizes[l.ZoneId] * 0.5f * 0.88f;   // innerhalb, vor dem Randgebirge
                foreach (var s in l.AllSpots())
                {
                    Assert.IsTrue(ids.Add(s.Id), "doppelte Id " + s.Id);
                    Assert.LessOrEqual(Mathf.Max(Mathf.Abs(s.Pos.x), Mathf.Abs(s.Pos.y)), lim, s.Id + " liegt im Randgebirge");
                }
                foreach (var h in l.Hearts) Assert.IsTrue(ids.Add(h.Id), "doppelte Id " + h.Id);
            }
        }

        static IEnumerable<string> ZoneContentIdsStartZone()
        {
            foreach (var w in ZoneContent.Waypoints) yield return w.Id;
            foreach (var p in ZoneContent.Pois) yield return p.Id;
            foreach (var v in ZoneContent.Vistas) yield return v.Id;
            foreach (var c in ZoneContent.Challenges) yield return c.Id;
            foreach (var h in ZoneContent.Hearts) yield return h.Id;
        }

        [Test]
        public void EachZone_UsesItsSignatureMountGate()
        {
            bool Has(BzLayout l, SpotGate g) { foreach (var s in l.AllSpots()) if (s.Gate == g) return true; return false; }
            Assert.IsTrue(Has(BiomeZoneContent.Nebelmoor, SpotGate.Island), "Nebelmoor: Skimmer-Insel");
            Assert.IsTrue(Has(BiomeZoneContent.Frostkamm, SpotGate.Mesa), "Frostkamm: Springer-Tafelberg");
            Assert.IsTrue(Has(BiomeZoneContent.Gluetoede, SpotGate.LavaRing), "Glutöde: Raptor-Lavaring");
            foreach (var l in BiomeZoneContent.All)
                foreach (var w in l.Waypoints) Assert.AreEqual(SpotGate.None, w.Gate, "Wegmarken sind zu Fuß erreichbar: " + w.Id);
        }

        // ---------- Flutfüllung auf synthetischen Rastern ----------
        static ReachabilityGrid Flat(int n, float h = 0f)
        {
            var g = new ReachabilityGrid(n, 4f);
            for (int i = 0; i < g.H.Length; i++) g.H[i] = h;
            return g;
        }

        [Test]
        public void Reach_CliffNeedsSpringer()
        {
            var g = Flat(20);
            // Tafelberg 8 m in der rechten Hälfte
            for (int z = 0; z < 20; z++) for (int x = 12; x < 20; x++) g.H[z * 20 + x] = 8f;
            var start = g.World(2, 10); var target = g.World(16, 10);
            Assert.IsTrue(ReachabilityModel.Required(g, start, target, -100f, out var req, 2f));
            Assert.AreEqual(Traversal.Springer, req);
            // hinunter geht es zu Fuß (Absprung ≤ MaxDrop)
            Assert.IsTrue(ReachabilityModel.Required(g, target, start, -100f, out req, 2f));
            Assert.AreEqual(Traversal.None, req);
        }

        [Test]
        public void Reach_DeepWaterNeedsSkimmer_LavaNeedsRaptor()
        {
            var g = Flat(30, 2f);
            for (int z = 0; z < 30; z++) for (int x = 12; x < 18; x++) { g.H[z * 30 + x] = -3f; g.Kind[z * 30 + x] = ReachabilityGrid.DeepWater; }   // 24 m Wasser
            Assert.IsTrue(ReachabilityModel.Required(g, g.World(2, 15), g.World(25, 15), 0f, out var req, 2f));
            Assert.AreEqual(Traversal.Skimmer, req, "24 m tiefes Wasser: zu breit für den Raptor");

            var lava = Flat(30, 2f);
            for (int z = 0; z < 30; z++) for (int x = 14; x < 16; x++) { lava.H[z * 30 + x] = -1f; lava.Kind[z * 30 + x] = ReachabilityGrid.Lava; }   // 8 m Lava
            Assert.IsTrue(ReachabilityModel.Required(lava, lava.World(2, 15), lava.World(25, 15), 0f, out req, 2f));
            Assert.AreEqual(Traversal.Raptor, req);
        }

        [Test]
        public void Reach_GentleSlopeIsWalkable_AndLabelsAreGerman()
        {
            var g = Flat(20);
            for (int z = 0; z < 20; z++) for (int x = 0; x < 20; x++) g.H[z * 20 + x] = x * 1.5f;   // 1,5 m je 4 m
            Assert.IsTrue(ReachabilityModel.Required(g, g.World(0, 0), g.World(19, 19), -100f, out var req, 2f));
            Assert.AreEqual(Traversal.None, req);
            Assert.AreEqual("Springer", ReachabilityModel.Label(Traversal.Springer));
            Assert.AreEqual("Raptor + Skimmer", ReachabilityModel.Label(Traversal.Raptor | Traversal.Skimmer));
        }

        [Test]
        public void BiomeTerrain_FeaturesShapeGates()
        {
            var b = ScriptableObject.CreateInstance<BiomeProfile>();
            b.Amplitude = 2f; b.BaseHeight = 4f; b.WaterLevel = 1f; b.EdgeMountainHeight = 0f; b.CanyonDepth = 0f;
            var t = new BiomeTerrain(b, 5, 600f);
            t.Mesa(new Vector2(0, 0), 15f, 30f);
            t.Pool(new Vector2(150, 0), 30f, 3f);
            t.Ring(new Vector2(-150, 0), 15f, 23f, 2f);
            t.Road(new Vector2(0, -200), new Vector2(0, -100), 6f, 6f);
            Assert.AreEqual(30f, t.Height(0, 0), 0.01f);
            Assert.AreEqual(b.WaterLevel - 3f, t.Height(150, 0), 0.01f);
            Assert.AreEqual(b.WaterLevel - 2f, t.Height(-131, 0), 0.01f);
            Assert.Greater(t.Height(-150, 0), b.WaterLevel, "Ring-Innenfläche bleibt trocken");
            Assert.AreEqual(6f, t.Height(0, -150), 0.01f);
            Assert.Less(t.RoadDistance(0, -150), 0f);
        }
    }
}
