using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class ZoneContentTests
    {
        [Test]
        public void Vistas_ReachableByStepsOfAtMostOneMeter()
        {
            Assert.GreaterOrEqual(ZoneContent.Vistas.Length, 3);
            foreach (var v in ZoneContent.Vistas)
            {
                var steps = ZoneContent.StairSteps(v);
                Assert.GreaterOrEqual(steps.Count, 5, v.Id);
                float max = ZoneContent.MaxStepRise(v);
                Assert.LessOrEqual(max, 1f, v.Id + ": größte Stufe " + max.ToString("0.00") + " m");
                Assert.Greater(ZoneContent.PlatformTop(v).y - ZoneLayout.Height(v.Pos.x, v.Pos.y), 3f, v.Id + " liegt erhöht");
                for (int i = 1; i < steps.Count; i++) Assert.Greater(steps[i].y, steps[i - 1].y, "Treppe steigt stetig");
            }
        }

        [Test]
        public void ZoneObjects_AreDry_AndApproachIsGentle()
        {
            float dry = ZoneLayout.WaterLevel + 0.3f;
            void Check(string id, Vector2 p)
            {
                float h = ZoneLayout.Height(p.x, p.y);
                Assert.Greater(h, dry, id + " liegt im Wasser");
                float maxDiff = 0f;
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; maxDiff = Mathf.Max(maxDiff, Mathf.Abs(ZoneLayout.Height(p.x + Mathf.Cos(a) * 4f, p.y + Mathf.Sin(a) * 4f) - h)); }
                Assert.Less(maxDiff, 2.0f, id + ": Umgebung zu steil (" + maxDiff.ToString("0.0") + " m auf 4 m)");
            }
            foreach (var w in ZoneContent.Waypoints) Check(w.Id, w.Pos);
            foreach (var c in ZoneContent.Challenges) Check(c.Id, c.Pos);
            foreach (var v in ZoneContent.Vistas) { var f = ZoneContent.StairSteps(v)[0]; Check(v.Id + " (Treppenfuß)", new Vector2(f.x, f.z)); }
            foreach (var h in ZoneContent.Hearts) { Check(h.Id + " (NPC)", h.NpcPos); foreach (var t in h.Tasks) Check(h.Id + " Aufgabe", t.Pos); }
        }

        [Test]
        public void JumpChallenge_NeedsJumping_ButIsReachable()
        {
            bool found = false;
            foreach (var c in ZoneContent.Challenges)
            {
                if (c.PillarHeight <= 0f) continue;
                found = true;
                var steps = ZoneContent.PillarSteps(c);
                float g = ZoneLayout.Height(c.Pos.x, c.Pos.y), prev = g;
                foreach (var s in steps)
                {
                    Assert.LessOrEqual(s.y - prev, 1.0f, c.Id + ": Sprunghöhe 1,2 m reicht");
                    Assert.Greater(s.y - prev, 0.5f, c.Id + ": ohne Sprung nicht erreichbar");
                    prev = s.y;
                }
                Assert.LessOrEqual(g + c.PillarHeight - prev, 1.0f, "letzter Sprung auf den Felsen");
            }
            Assert.IsTrue(found, "mindestens eine Herausforderung nur springend erreichbar");
        }

        [Test]
        public void Content_MeetsMinimums_AndIdsAreUnique()
        {
            var ids = new HashSet<string>();
            void Id(string id) => Assert.IsTrue(ids.Add(id), "doppelte Id " + id);
            foreach (var w in ZoneContent.Waypoints) Id(w.Id);
            foreach (var p in ZoneContent.Pois) Id(p.Id);
            foreach (var v in ZoneContent.Vistas) Id(v.Id);
            foreach (var c in ZoneContent.Challenges) Id(c.Id);
            foreach (var h in ZoneContent.Hearts) Id(h.Id);
            Assert.GreaterOrEqual(ZoneContent.Waypoints.Length, 4);
            Assert.GreaterOrEqual(ZoneContent.Challenges.Length, 4);
            var kinds = new HashSet<ZoneContent.ChallengeKind>(); foreach (var c in ZoneContent.Challenges) kinds.Add(c.Kind);
            Assert.AreEqual(3, kinds.Count, "3 Typen von Heldenherausforderungen");
            Assert.GreaterOrEqual(ZoneContent.Hearts.Length, 3);
            foreach (var h in ZoneContent.Hearts)
            {
                var ways = new HashSet<ZoneContent.TaskKind>(); foreach (var t in h.Tasks) ways.Add(t.Kind);
                Assert.GreaterOrEqual(ways.Count + (h.KillValue > 0f ? 1 : 0), 2, h.Id + ": mehrere Beitragswege");
                Assert.LessOrEqual(Mathf.Max(h.KillValue, h.TaskValue), 0.25f, "Herz nicht mit 1–2 Aktionen erledigt");
                foreach (var t in h.Tasks) Assert.LessOrEqual(Vector2.Distance(t.Pos, h.Center), h.Radius, h.Id + ": Aufgabe im Herz-Radius");
            }
            Assert.AreEqual(10, ZoneContent.ChallengeHeroPoints, "GW2-HoT-Modell: 10 Heldenpunkte");
        }
    }

    public class ZoneRuleTests
    {
        [Test]
        public void Contested_WhenInsideRunningEventRadius()
        {
            var events = new List<(Vector3, float)> { (new Vector3(5, 0, 5), 32f) };
            var dornwacht = System.Array.Find(ZoneContent.Waypoints, w => w.Id == "wp_dornwacht");
            var sonnfurt = System.Array.Find(ZoneContent.Waypoints, w => w.Id == "wp_sonnfurt");
            Assert.IsTrue(ZoneRules.IsContested(new Vector3(dornwacht.Pos.x, 3, dornwacht.Pos.y), events), "Dornwacht während der Kette umkämpft");
            Assert.IsFalse(ZoneRules.IsContested(new Vector3(sonnfurt.Pos.x, 1, sonnfurt.Pos.y), events));
            Assert.IsFalse(ZoneRules.IsContested(new Vector3(dornwacht.Pos.x, 3, dornwacht.Pos.y), new List<(Vector3, float)>()), "ohne Event frei");
        }

        [Test]
        public void HeartProgress_Accumulates_CompletesOnce()
        {
            var hearts = new Dictionary<string, float>(); var done = new HashSet<string>();
            Assert.IsFalse(ZoneRules.AddHeartProgress(hearts, done, "h", 0.4f));
            Assert.IsFalse(ZoneRules.AddHeartProgress(hearts, done, "h", 0.4f));
            Assert.AreEqual(0.8f, ZoneRules.HeartProgress(hearts, done, "h"), 1e-4f);
            Assert.IsTrue(ZoneRules.AddHeartProgress(hearts, done, "h", 0.4f), "erfüllt genau jetzt");
            Assert.AreEqual(1f, hearts["h"], 1e-4f, "auf 100 % begrenzt");
            Assert.IsFalse(ZoneRules.AddHeartProgress(hearts, done, "h", 0.4f), "kein zweites Mal");
            Assert.IsTrue(done.Contains("h"));
            Assert.IsFalse(ZoneRules.AddHeartProgress(hearts, done, "x", 0f));
        }

        static ZoneObjectDefinition D(string id, MapMarkerKind k) { var d = ScriptableObject.CreateInstance<ZoneObjectDefinition>(); d.Id = id; d.Kind = k; return d; }

        [Test]
        public void Completion_CountsAndGrantsChestOnceAtHundredPercent()
        {
            var defs = new List<ZoneObjectDefinition> { D("w1", MapMarkerKind.Waypoint), D("w2", MapMarkerKind.Waypoint), D("p", MapMarkerKind.PointOfInterest), D("v", MapMarkerKind.Vista), D("h", MapMarkerKind.Heart), D("c", MapMarkerKind.HeroChallenge) };
            var done = new HashSet<string> { "w1", "p" };
            var c = ZoneRules.Count(defs, done);
            Assert.AreEqual(new Vector2Int(1, 2), c.Waypoints);
            Assert.AreEqual(2f / 6f, c.Percent, 1e-4f);
            Assert.IsFalse(ZoneRules.ShouldGrantChest(c, false));
            foreach (var d in defs) done.Add(d.Id);
            c = ZoneRules.Count(defs, done);
            Assert.AreEqual(1f, c.Percent, 1e-4f);
            Assert.IsTrue(ZoneRules.ShouldGrantChest(c, false));
            Assert.IsFalse(ZoneRules.ShouldGrantChest(c, true), "nur einmal");
            Assert.IsFalse(ZoneRules.ShouldGrantChest(ZoneRules.Count(new List<ZoneObjectDefinition>(), done), false), "leere Zone gibt keine Truhe");
        }

        [Test]
        public void Save_RoundTripsZoneProgress()
        {
            var s = new ProfileState();
            s.Completed.Add("wp_sonnfurt"); s.Completed.Add("vista_elmhain");
            s.Hearts["heart_dornwacht"] = 0.36f;
            s.ZoneChestGranted = true;
            var t = new ProfileState();
            t.LoadDto(JsonUtility.FromJson<SaveGameDto>(JsonUtility.ToJson(s.ToDto())), null, null);
            Assert.IsTrue(t.Completed.SetEquals(s.Completed));
            Assert.AreEqual(0.36f, t.Hearts["heart_dornwacht"], 1e-4f);
            Assert.IsTrue(t.ZoneChestGranted);
        }
    }
}
