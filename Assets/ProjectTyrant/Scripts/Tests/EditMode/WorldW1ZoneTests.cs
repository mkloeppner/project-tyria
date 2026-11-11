using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Landmarks;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Welt-Lauf W1: gespeicherte Zonen-Pläne (aus „Alle Zonen neu planen“ / Rebuild Everything) – je Zone gültig, sichtbar, im Zonen-Rechteck.</summary>
    public class WorldW1ZoneTests
    {
        static IEnumerable<string> Zones => WorldPlanner.Specs.Keys;

        static ZoneDefinition Zone(string id)
        {
            var z = WorldBuilder.Zone(id);
            Assert.IsNotNull(z, id);
            Assert.IsNotNull(z.WorldPlan, id + ": kein Plan");
            Assert.AreEqual(WorldPlanner.PlanVersion, z.WorldPlan.Version, id + ": Plan veraltet");
            return z;
        }

        [TestCaseSource(nameof(Zones))] public void ZonePlan_PassesValidator(string id)
        {
            var z = Zone(id);
            var r = WorldValidator.Validate(z.Id, z.LevelMax, z.WorldPlan, null, z.WorldPlan.MainVisibility);
            Assert.IsTrue(r.Ok, string.Join("\n", r.Errors));
        }

        [TestCaseSource(nameof(Zones))] public void ZonePlan_MainLandmarkVisible(string id)
        {
            var z = Zone(id);
            Assert.IsNotNull(z.WorldPlan.Main, id);
            if (!z.LegacyLayout) Assert.GreaterOrEqual(z.WorldPlan.MainVisibility, WorldValidator.MinVisibility, id);
        }

        [TestCaseSource(nameof(Zones))] public void ZonePlan_ContentInsideZone(string id)
        {
            var z = Zone(id); float half = (z.LegacyLayout ? Procedural.ZoneLayout.Size : z.Size) * 0.5f;
            void In(Vector3 p, string what) => Assert.IsTrue(Mathf.Abs(p.x) <= half && Mathf.Abs(p.z) <= half, id + ": " + what + " außerhalb " + p);
            foreach (var l in z.WorldPlan.Landmarks) In(l.Position, l.Id);
            foreach (var s in z.WorldPlan.Secrets) In(s.Position, s.Id);
            foreach (var l in z.WorldPlan.Lore) In(l.Position, l.Id);
            foreach (var c in z.WorldPlan.Climbs) { In(c.Base, c.Id); In(c.Peak, c.Id); }
            var ids = new HashSet<string>();
            foreach (var s in z.WorldPlan.Secrets) Assert.IsTrue(ids.Add(s.Id), "doppelt: " + s.Id);
        }
    }
}
