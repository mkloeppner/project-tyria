using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.Landmarks;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Welt-Lauf W1: Gelände-Operationen, Hürden, Tore, Baukasten, Aufstiege, Sichtbarkeit, Abkürzungen, Geheimnisse und Validator (rein).</summary>
    public class WorldW1Tests
    {
        static readonly TerrainOpKind[] AllOps = (TerrainOpKind[])System.Enum.GetValues(typeof(TerrainOpKind));
        static readonly HazardKind[] AllHazards = (HazardKind[])System.Enum.GetValues(typeof(HazardKind));
        static readonly LandmarkKind[] AllKinds = (LandmarkKind[])System.Enum.GetValues(typeof(LandmarkKind));
        static readonly LandmarkKind[] InteriorKinds = AllKinds.Where(k => k != LandmarkKind.Massif).ToArray();

        static float Flat(float x, float z) => 10f;
        static TerrainOpDef Op(TerrainOpKind k) => new TerrainOpDef { Kind = k, Center = new Vector2(20f, -15f), Radius = 60f, Height = k == TerrainOpKind.Mesa ? 50f : 40f, Param = 4f, Yaw = 30f };

        // ---------------- Gelände-Operationen ----------------
        [TestCaseSource(nameof(AllOps))] public void TerrainOp_IsDeterministic(TerrainOpKind k)
        {
            var ops = new List<TerrainOpDef> { Op(k) };
            Assert.AreEqual(TerrainOps.Hash(ops, Flat, 2f, 300f, 6f), TerrainOps.Hash(new List<TerrainOpDef> { Op(k) }, Flat, 2f, 300f, 6f));
        }

        [TestCaseSource(nameof(AllOps))] public void TerrainOp_OnlyChangesItsRadius(TerrainOpKind k)
        {
            var op = Op(k);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f; float x = op.Center.x + Mathf.Cos(a) * 200f, z = op.Center.y + Mathf.Sin(a) * 200f;
                Assert.IsFalse(TerrainOps.Affects(op, x, z));
                Assert.AreEqual(10f, TerrainOps.Apply(op, x, z, 10f, 2f), 1e-4f);
            }
        }

        [TestCaseSource(nameof(AllOps))] public void TerrainOp_ShapesTheGround(TerrainOpKind k)
        {
            var op = Op(k); float maxD = 0f;
            for (float x = -60f; x <= 60f; x += 5f) for (float z = -60f; z <= 60f; z += 5f)
                maxD = Mathf.Max(maxD, Mathf.Abs(TerrainOps.Apply(op, op.Center.x + x, op.Center.y + z, 10f, 2f) - 10f));
            Assert.Greater(maxD, 1f, k + " verändert das Gelände nicht");
        }

        [Test] public void TerrainOp_MassifAndPeakRiseAboveGround()
        {
            foreach (var k in new[] { TerrainOpKind.Massif, TerrainOpKind.Peak })
            {
                var op = Op(k);
                Assert.Greater(TerrainOps.Apply(op, op.Center.x, op.Center.y, 10f, 2f), 10f + op.Height * 0.6f, k.ToString());
            }
        }

        [Test] public void TerrainOp_CanyonCutsBelowGround()
        {
            var op = Op(TerrainOpKind.Canyon);
            Assert.Less(TerrainOps.Apply(op, op.Center.x, op.Center.y, 10f, 2f), 10f - 5f);
        }

        [Test] public void TerrainOp_ApplyAll_ChainsOperations()
        {
            var a = Op(TerrainOpKind.Peak); var b = Op(TerrainOpKind.Peak); b.Center = new Vector2(400f, 400f);
            float h1 = TerrainOps.ApplyAll(new List<TerrainOpDef> { a }, a.Center.x, a.Center.y, 10f, 2f);
            float h2 = TerrainOps.ApplyAll(new List<TerrainOpDef> { a, b }, a.Center.x, a.Center.y, 10f, 2f);
            Assert.AreEqual(h1, h2, 1e-4f, "entfernte Operation wirkt nicht");
            Assert.AreEqual(10f, TerrainOps.ApplyAll(new List<TerrainOpDef>(), 0f, 0f, 10f, 2f));
        }

        [Test] public void Serpentine_StaysWalkable()
        {
            var p = TerrainOps.Serpentine(Vector2.zero, 120f, 30f, 10f, 80f, 0.16f, 3);
            Assert.Greater(p.Count, 10);
            Assert.LessOrEqual(TerrainOps.MaxSlope(p), 0.16f + 0.02f);
            Assert.AreEqual(10f, p[0].y, 0.5f); Assert.AreEqual(80f, p[p.Count - 1].y, 0.5f);
            Assert.Greater(TerrainOps.Length(p), 70f / 0.16f * 0.9f, "Serpentine lang genug für die Steigung");
        }

        // ---------------- Hürden ----------------
        [TestCaseSource(nameof(AllHazards))] public void Hazard_HasRescueWindow(HazardKind k) => Assert.GreaterOrEqual(HazardRules.RescueWindow(k), HazardRules.MinRescueWindow);

        [TestCaseSource(nameof(AllHazards))] public void Hazard_OwnKeyProtects(HazardKind k)
        {
            var keys = HazardRules.Spec(k).Keys;
            Assert.AreNotEqual(Traversal.None, keys);
            foreach (var key in GateRules.Singles(keys)) Assert.IsTrue(HazardRules.Protected(k, key), k + " / " + key);
        }

        [TestCaseSource(nameof(AllHazards))] public void Hazard_OnFootNotProtected(HazardKind k)
        {
            Assert.IsFalse(HazardRules.Protected(k, Traversal.None));
            StringAssert.StartsWith("Benötigt: ", HazardRules.Requirement(k));
            Assert.IsFalse(string.IsNullOrEmpty(HazardRules.Spec(k).Name));
            Assert.Greater(HazardRules.Spec(k).EdgeColor.a, 0.5f, "Rand sichtbar");
        }

        [Test] public void Quicksand_SinksAfter4s_DeadAfter8s()
        {
            var e = new HazardExposure(HazardKind.Quicksand);
            HazardTick t = default;
            for (int i = 0; i < 39; i++) t = e.Tick(0.1f, true, Traversal.None);
            Assert.IsFalse(t.Sinking, "vor 4 s kein Versinken");
            for (int i = 0; i < 2; i++) t = e.Tick(0.1f, true, Traversal.None);
            Assert.IsTrue(t.Sinking); Assert.IsFalse(t.Dead);
            for (int i = 0; i < 40; i++) t = e.Tick(0.1f, true, Traversal.None);
            Assert.IsTrue(t.Dead);
        }

        [Test] public void Quicksand_DodgeBuysTime_LeavingResets()
        {
            var e = new HazardExposure(HazardKind.Quicksand);
            for (int i = 0; i < 30; i++) e.Tick(0.1f, true, Traversal.None);
            e.Dodge(); Assert.AreEqual(2f, e.Time, 0.01f);
            e.Tick(0.1f, false, Traversal.None); Assert.AreEqual(0f, e.Time);
        }

        [Test] public void Hazard_MountedSkimmerIgnoresToxicWater()
        {
            var e = new HazardExposure(HazardKind.ToxicWater);
            var t = e.Tick(1.1f, true, Traversal.Skimmer);
            Assert.AreEqual(1f, t.SpeedFactor); Assert.AreEqual(0f, t.HpFraction); Assert.AreEqual(0f, e.Time);
        }

        [Test] public void Lava_BurnsAndKillsAfter10s()
        {
            var e = new HazardExposure(HazardKind.Lava);
            var t = e.Tick(1.0f, true, Traversal.None);
            Assert.AreEqual(EffectType.Burning, t.Condition); Assert.Greater(t.Stacks, 0); Assert.Greater(t.HpFraction, 0f);
            for (int i = 0; i < 9; i++) t = e.Tick(1.0f, true, Traversal.None);
            Assert.IsTrue(t.Dead);
            Assert.IsTrue(HazardRules.Protected(HazardKind.Lava, Traversal.LavaSkimmer));
            Assert.IsFalse(HazardRules.Protected(HazardKind.Lava, Traversal.Skimmer), "normaler Skimmer schützt nicht vor Lava");
        }

        [Test] public void Corruption_DamageCappedPerTick()
        {
            var e = new HazardExposure(HazardKind.CorruptionField);
            var t = e.Tick(1.0f, true, Traversal.None);
            Assert.LessOrEqual(t.HpFraction, HazardRules.Spec(HazardKind.CorruptionField).HpCapPerTick);
            Assert.IsFalse(t.Dead);
            Assert.IsTrue(HazardRules.Protected(HazardKind.CorruptionField, Traversal.CorruptionWard));
        }

        [Test] public void WindWall_PushesBack_ThornsAndWallsBlock()
        {
            Assert.IsTrue(new HazardExposure(HazardKind.WindWall).Tick(0.1f, true, Traversal.None).Pushback);
            Assert.IsTrue(HazardRules.Spec(HazardKind.ThornHedge).Blocks);
            Assert.IsTrue(HazardRules.Spec(HazardKind.SteepWall).Blocks);
            Assert.IsTrue(HazardRules.Protected(HazardKind.SteepWall, Traversal.Springer));
            Assert.IsTrue(HazardRules.Protected(HazardKind.SandPortal, Traversal.DuneRunner));
        }

        // ---------------- Tore ----------------
        [Test] public void Gates_KeysByLevelBand()
        {
            Assert.AreEqual(1, GateRules.AvailableFromLevel(Traversal.Raptor));
            Assert.AreEqual(15, GateRules.AvailableFromLevel(Traversal.Skimmer));
            Assert.AreEqual(25, GateRules.AvailableFromLevel(Traversal.Springer));
            Assert.AreEqual(75, GateRules.AvailableFromLevel(Traversal.Griffon));
            Assert.AreEqual(80, GateRules.AvailableFromLevel(Traversal.DuneRunner));
            Assert.AreEqual(25, GateRules.AvailableFromLevel(Traversal.Raptor | Traversal.Springer), "kombiniert = spätester");
        }

        [Test] public void Gates_InBand_AlternativesUseEarliest()
        {
            Assert.IsTrue(GateRules.InBand(Traversal.Skimmer | Traversal.Griffon, 20));
            Assert.IsFalse(GateRules.InBand(Traversal.Griffon | Traversal.Skyscale, 45));
            Assert.IsTrue(GateRules.InBand(Traversal.None, 1));
        }

        [Test] public void Gates_ReturnGateValidOutsideBand()
        {
            var g = GateDef.Of(Traversal.Griffon);
            Assert.IsFalse(GateRules.Valid(g, 30));
            g.ReturnGate = true;
            Assert.IsTrue(GateRules.Valid(g, 30));
            StringAssert.Contains("komm später wieder", GateRules.Hint(g));
            Assert.AreEqual("", GateRules.Hint(GateDef.Foot));
        }

        [Test] public void Gates_SatisfiedByAnyKey()
        {
            var g = GateDef.Of(Traversal.Griffon | Traversal.Skyscale);
            Assert.IsTrue(GateRules.Satisfied(g, Traversal.Skyscale));
            Assert.IsFalse(GateRules.Satisfied(g, Traversal.Raptor | Traversal.Springer));
            Assert.IsTrue(GateRules.Satisfied(GateDef.Foot, Traversal.None));
        }

        [Test] public void Gates_MandatoryKinds()
        {
            foreach (var k in new[] { MapMarkerKind.Waypoint, MapMarkerKind.Heart, MapMarkerKind.PointOfInterest, MapMarkerKind.Scout, MapMarkerKind.Portal }) Assert.IsTrue(GateRules.IsMandatory(k), k.ToString());
            foreach (var k in new[] { MapMarkerKind.Vista, MapMarkerKind.Insight, MapMarkerKind.HeroChallenge }) Assert.IsFalse(GateRules.IsMandatory(k), k.ToString());
        }

        [Test] public void Gates_SinglesSplitsFlags()
        {
            var s = GateRules.Singles(Traversal.Raptor | Traversal.Griffon | Traversal.JadeBot).ToList();
            CollectionAssert.AreEquivalent(new[] { Traversal.Raptor, Traversal.Griffon, Traversal.JadeBot }, s);
            Assert.AreEqual(10, GateRules.Singles(Traversal.All).Count());
        }

        // ---------------- Baukasten ----------------
        [TestCaseSource(nameof(AllKinds))] public void Kit_RampsWalkable(LandmarkKind k)
        {
            var L = LandmarkKit.Build(k, 100f, 7);
            foreach (var m in L.Modules) if (m.Role == ModuleRole.Ramp) Assert.LessOrEqual(LandmarkKit.RampDegrees(m), LandmarkKit.MaxRampDeg + 0.01f, m.Name);
        }

        [TestCaseSource(nameof(AllKinds))] public void Kit_LedgesWithinSpringerReach(LandmarkKind k)
        {
            var L = LandmarkKit.Build(k, 100f, 7);
            var groups = new Dictionary<string, List<(int i, float y)>>();
            foreach (var m in L.Modules)
            {
                if (m.Role != ModuleRole.Ledge || m.Name == null) continue;
                int u = m.Name.LastIndexOf('_'); if (u < 0 || !int.TryParse(m.Name.Substring(u + 1), out int idx)) continue;
                string key = m.Name.Substring(0, u);
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<(int, float)>();
                list.Add((idx, m.Pos.y));
            }
            foreach (var g in groups)
            {
                var s = g.Value.OrderBy(v => v.i).ToList();
                for (int i = 1; i < s.Count; i++) Assert.LessOrEqual(s[i].y - s[i - 1].y, LandmarkKit.SpringerStep + 0.01f, g.Key + "_" + s[i].i);
            }
        }

        [TestCaseSource(nameof(AllKinds))] public void Kit_IsDeterministic(LandmarkKind k)
        {
            var a = LandmarkKit.Build(k, 90f, 42); var b = LandmarkKit.Build(k, 90f, 42);
            Assert.AreEqual(a.Modules.Count, b.Modules.Count);
            for (int i = 0; i < a.Modules.Count; i++) { Assert.AreEqual(a.Modules[i].Pos, b.Modules[i].Pos); Assert.AreEqual(a.Modules[i].Scale, b.Modules[i].Scale); }
            Assert.AreEqual(a.Footprint, b.Footprint);
        }

        [TestCaseSource(nameof(AllKinds))] public void Kit_HasAnchorsAndHeight(LandmarkKind k)
        {
            var L = LandmarkKit.Build(k, 100f, 7);
            Assert.IsTrue(L.Anchors.ContainsKey("Eingang"));
            Assert.GreaterOrEqual(L.Anchors.Count, 3, "Eingang + mindestens zwei Stufen");
            Assert.Greater(L.MaxTop, 55f, "Silhouette hoch genug");
            Assert.Greater(L.Footprint, 5f);
            foreach (var m in L.Modules) Assert.IsFalse(float.IsNaN(m.Pos.x) || float.IsNaN(m.Scale.y), m.Name);
        }

        [TestCaseSource(nameof(InteriorKinds))] public void Kit_HasWalkableInterior(LandmarkKind k)
        {
            var L = LandmarkKit.Build(k, 100f, 7);
            Assert.IsTrue(L.HasInterior);
            Assert.IsTrue(L.Modules.Any(m => m.Interior), "Innen-Bausteine");
        }

        [Test] public void Kit_LighthouseStairsOpenIntoLantern()
        {
            foreach (var h in new[] { 60f, 70f, 110f })
            {
                var L = LandmarkKit.Build(LandmarkKind.Lighthouse, h, 1);
                var top = L.Modules.Where(m => m.Name.StartsWith("Spiral_")).OrderByDescending(m => m.Pos.y).First();
                var f = Quaternion.Euler(top.Euler) * Vector3.forward * (top.Scale.z * 0.5f);
                var end = top.Pos + (f.y >= 0f ? f : -f);
                foreach (var m in L.Modules.Where(m => m.Role == ModuleRole.Floor && m.Name.StartsWith("LanternFloor")))
                {
                    bool inside = Mathf.Abs(end.x - m.Pos.x) < m.Scale.x * 0.5f - 0.3f && Mathf.Abs(end.z - m.Pos.z) < m.Scale.z * 0.5f - 0.3f;
                    Assert.IsFalse(inside, h + " m: Treppen-Ende " + end + " unter " + m.Name);
                }
                Assert.GreaterOrEqual(L.Modules.Count(m => m.Name.StartsWith("LanternFloor")), 3, "Boden mit Luke");
                Assert.Greater(L.Anchors["Laterne"].y, end.y - 1f);
            }
        }

        [Test] public void Kit_ScalesWithHeight()
        {
            foreach (var k in new[] { LandmarkKind.Cathedral, LandmarkKind.Lighthouse, LandmarkKind.Pagoda })
                Assert.Greater(LandmarkKit.Build(k, 140f, 1).MaxTop, LandmarkKit.Build(k, 70f, 1).MaxTop + 30f, k.ToString());
        }

        // ---------------- Aufstiege ----------------
        static List<StageDef> Climb(int lvl) => ClimbTemplate.Create(new Vector3(0, 10, 0), new Vector3(100, 210, 0), lvl);

        [Test] public void Climb_ThreeStagesWithKeys()
        {
            var s = Climb(80);
            Assert.AreEqual(3, s.Count);
            Assert.AreEqual(Traversal.None, s[0].Gate.Keys);
            Assert.AreEqual(Traversal.Springer, s[1].Gate.Keys); Assert.IsTrue(s[1].Gate.FootAlternative);
            Assert.AreNotEqual(Traversal.None, s[2].Gate.Keys & (Traversal.Griffon | Traversal.Skyscale));
            Assert.Less(s[0].Position.y, s[1].Position.y); Assert.Less(s[1].Position.y, s[2].Position.y);
        }

        [Test] public void Climb_ReturnGatesByBand()
        {
            var low = Climb(20);
            Assert.IsTrue(low[1].Gate.ReturnGate, "Springer erst ab 25");
            Assert.IsTrue(low[2].Gate.ReturnGate);
            var high = Climb(80);
            Assert.IsFalse(high[1].Gate.ReturnGate); Assert.IsFalse(high[2].Gate.ReturnGate);
        }

        [Test] public void Climb_CheckpointsEvery15m()
        {
            var s = Climb(80); var prev = new Vector3(0, 10, 0);
            foreach (var st in s)
            {
                Assert.Greater(st.Checkpoints.Count, 0);
                foreach (var c in st.Checkpoints) { Assert.LessOrEqual(c.y - prev.y, 15.01f); prev = c; }
                Assert.AreEqual(st.Position, st.Checkpoints[st.Checkpoints.Count - 1]);
            }
        }

        [Test] public void Climb_ReachableWithKeyOrLongFootPath()
        {
            var s = Climb(80);
            Assert.IsTrue(ClimbTemplate.Reachable(s[0], Traversal.None, false));
            Assert.IsFalse(ClimbTemplate.Reachable(s[1], Traversal.None, false));
            Assert.IsTrue(ClimbTemplate.Reachable(s[1], Traversal.Springer, false));
            Assert.IsTrue(ClimbTemplate.Reachable(s[1], Traversal.None, true), "Grat zu Fuß");
            Assert.IsTrue(ClimbTemplate.Reachable(s[2], Traversal.Griffon, false));
        }

        [Test] public void Climb_DescentGlide()
        {
            Assert.Less(ClimbTemplate.DescentSeconds(new Vector3(0, 200, 0), new Vector3(300, 10, 0)), 30f);
            Assert.IsTrue(float.IsPositiveInfinity(ClimbTemplate.DescentSeconds(new Vector3(0, 20, 0), new Vector3(800, 10, 0))), "zu flach zum Gleiten");
            Assert.IsTrue(float.IsPositiveInfinity(ClimbTemplate.DescentSeconds(new Vector3(0, 10, 0), new Vector3(10, 20, 0))));
        }

        [Test] public void Checkpoints_VisitAndReset()
        {
            var t = new CheckpointTracker(new List<Vector3> { new Vector3(0, 10, 0), new Vector3(0, 25, 0), new Vector3(0, 40, 0) });
            Assert.IsFalse(t.ShouldReset(new Vector3(0, -50, 0), out _), "vor dem ersten Kontrollpunkt kein Zurücksetzen");
            Assert.IsTrue(t.Visit(new Vector3(1, 25, 1)));
            Assert.AreEqual(1, t.Last);
            Assert.IsFalse(t.ShouldReset(new Vector3(0, 21, 0), out _), "kleiner Rutscher");
            Assert.IsTrue(t.ShouldReset(new Vector3(0, 12, 0), out var r));
            Assert.AreEqual(25.5f, r.y, 0.01f);
        }

        [Test] public void Checkpoints_NeverGoBackwards()
        {
            var t = new CheckpointTracker(new List<Vector3> { new Vector3(0, 10, 0), new Vector3(0, 25, 0) });
            t.Visit(new Vector3(0, 25, 0)); Assert.IsFalse(t.Visit(new Vector3(0, 10, 0)));
            Assert.AreEqual(1, t.Last);
            Assert.IsFalse(new CheckpointTracker(null).Visit(Vector3.zero));
        }

        // ---------------- Sichtbarkeit ----------------
        [Test] public void Visibility_FlatGroundSeesTower()
        {
            var s = VisibilityModel.WalkableSamples(Flat, 600f, 2f, 100, 3);
            Assert.AreEqual(100, s.Count);
            Assert.AreEqual(1f, VisibilityModel.Fraction(Flat, s, new[] { new Vector3(0, 110, 0) }), 1e-4f);
        }

        [Test] public void Visibility_RidgeBlocksHalf()
        {
            System.Func<float, float, float> h = (x, z) => x > 50f && x < 60f ? 300f : 10f;
            var samples = new List<Vector2> { new Vector2(-100, 0), new Vector2(200, 0), new Vector2(-200, 50), new Vector2(150, -40) };
            Assert.AreEqual(0.5f, VisibilityModel.Fraction(h, samples, new[] { new Vector3(0, 100, 0) }), 1e-4f);
            Assert.IsFalse(VisibilityModel.LineOfSight(h, new Vector3(200, 12, 0), new Vector3(0, 100, 0)));
        }

        [Test] public void Visibility_SamplesDeterministicAndDry()
        {
            System.Func<float, float, float> h = (x, z) => x < 0f ? 0f : 10f;   // linke Hälfte unter Wasser
            var a = VisibilityModel.WalkableSamples(h, 400f, 2f, 50, 9); var b = VisibilityModel.WalkableSamples(h, 400f, 2f, 50, 9);
            CollectionAssert.AreEqual(a, b);
            Assert.IsTrue(a.All(p => p.x >= 0f));
            Assert.AreEqual(0f, VisibilityModel.Fraction(Flat, new List<Vector2>(), new[] { Vector3.zero }));
        }

        [Test] public void Visibility_MaxDistance()
        {
            var s = new List<Vector2> { new Vector2(3000, 0) };
            Assert.AreEqual(0f, VisibilityModel.Fraction(Flat, s, new[] { new Vector3(0, 100, 0) }, 1600f));
        }

        // ---------------- Abkürzungen, Innenräume, Geheimnisse ----------------
        [Test] public void Shortcut_SavingMath()
        {
            var s = new ShortcutDef { Key = Traversal.Springer, FootPath = 1000f, MountPath = 100f };
            Assert.AreEqual(1000f / 7f - 100f / 8.5f, ShortcutModel.SavedSeconds(s), 0.01f);
            Assert.Greater(ShortcutModel.SavedSeconds(s), ShortcutModel.MinSaving);
            Assert.Greater(ShortcutModel.MountSpeed(Traversal.Griffon), ShortcutModel.RunSpeed);
            Assert.AreEqual(ShortcutModel.RunSpeed, ShortcutModel.MountSpeed(Traversal.None));
        }

        [Test] public void InteriorCulling_SwapsDetail()
        {
            InteriorCullingModel.State(true, out bool inV, out bool exV); Assert.IsTrue(inV); Assert.IsFalse(exV);
            InteriorCullingModel.State(false, out inV, out exV); Assert.IsFalse(inV); Assert.IsTrue(exV);
            var b = new Bounds(Vector3.zero, Vector3.one * 10f);
            Assert.IsTrue(InteriorCullingModel.Inside(b, new Vector3(1, 1, 1))); Assert.IsFalse(InteriorCullingModel.Inside(b, new Vector3(9, 0, 0)));
        }

        [Test] public void Secrets_OutsideMarkersAndDaily()
        {
            var markers = new[] { new Vector3(0, 0, 0), new Vector3(100, 50, 0) };
            Assert.IsFalse(SecretRules.OutsideMarkers(new Vector3(20, 80, 0), markers), "Höhe zählt nicht");
            Assert.IsTrue(SecretRules.OutsideMarkers(new Vector3(50, 0, 0), markers));
            Assert.AreEqual("secret:x:20261111", SecretRules.DailyFlag("x", new System.DateTime(2026, 11, 11)));
            Assert.AreNotEqual(SecretRules.DailyFlag("x", new System.DateTime(2026, 11, 11)), SecretRules.DailyFlag("x", new System.DateTime(2026, 11, 12)));
        }

        // ---------------- Validator ----------------
        static ZoneWorldPlan ValidPlan()
        {
            var p = new ZoneWorldPlan { Version = 1 };
            p.Landmarks.Add(new LandmarkDef { Id = "m", Name = "Dom", Kind = LandmarkKind.Cathedral, Main = true, Height = 100f, Interior = true, Primary = Color.white, Position = new Vector3(0, 0, 0) });
            p.Landmarks.Add(new LandmarkDef { Id = "n", Name = "Turm", Kind = LandmarkKind.Lighthouse, Height = 40f, Interior = true, Primary = Color.red, Position = new Vector3(300, 0, 0) });
            var c = new ClimbDef { Id = "c", Name = "Pfad", Base = new Vector3(-300, 10, 0), Peak = new Vector3(-300, 210, 200), DescentTarget = new Vector3(-300, 10, -500) };
            c.Stages = ClimbTemplate.Create(c.Base, c.Peak, 80);
            p.Climbs.Add(c);
            p.Shortcuts.Add(new ShortcutDef { Id = "s", Key = Traversal.Springer, FootPath = 1200f, MountPath = 150f });
            for (int i = 0; i < 6; i++) p.Secrets.Add(new SecretDef { Id = "g" + i, Position = new Vector3(500 + i * 60, 0, 500) });
            for (int i = 0; i < 3; i++) p.Lore.Add(new LoreDef { Id = "l" + i });
            for (int i = 0; i < 5; i++) p.Loops.Add(new LoopDef { Id = "o" + i, TriggerKind = "Rauchsäule", Target = "g" + i });
            for (int i = 0; i < 3; i++) p.Scenes.Add(new SceneDef { Id = "sc" + i, Kind = "Lager" });
            return p;
        }
        static List<MarkerCheck> Markers() => new List<MarkerCheck> { new MarkerCheck { Id = "wp", Kind = MapMarkerKind.Waypoint, Position = Vector3.zero }, new MarkerCheck { Id = "v", Kind = MapMarkerKind.Vista, Position = new Vector3(50, 0, 50) } };

        [Test] public void Validator_ValidPlanPasses()
        {
            var r = WorldValidator.Validate("t", 80, ValidPlan(), Markers(), 0.8f);
            Assert.IsTrue(r.Ok, string.Join("\n", r.Errors));
            Assert.Greater(r.BestShortcutSaving, 90f);
            Assert.Greater(r.Gates, 0);
        }

        [Test] public void Validator_GatedMandatoryMarkerFails()
        {
            var m = Markers(); m.Add(new MarkerCheck { Id = "herz", Kind = MapMarkerKind.Heart, Requires = Traversal.Griffon });
            var r = WorldValidator.Validate("t", 80, ValidPlan(), m, 0.8f);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Pflicht-Marker herz")));
        }

        [Test] public void Validator_MostVistasGatedFails()
        {
            var m = Markers(); m.Add(new MarkerCheck { Id = "v2", Kind = MapMarkerKind.Vista, Requires = Traversal.Griffon }); m.Add(new MarkerCheck { Id = "v3", Kind = MapMarkerKind.Vista, Requires = Traversal.Springer });
            Assert.IsTrue(WorldValidator.Validate("t", 80, ValidPlan(), m, 0.8f).Errors.Any(e => e.Contains("Aussichtspunkte hinter Toren")));
        }

        [Test] public void Validator_OutOfBandGateWithoutReturnFails()
        {
            var p = ValidPlan(); p.Secrets[0].Gate = GateDef.Of(Traversal.Griffon);
            Assert.IsTrue(WorldValidator.Validate("t", 30, p, Markers(), 0.8f).Errors.Any(e => e.Contains("nicht im Band")));
            p.Secrets[0].Gate.ReturnGate = true;
            Assert.IsFalse(WorldValidator.Validate("t", 30, p, Markers(), 0.8f).Errors.Any(e => e.Contains("Tor g0")));
        }

        [Test] public void Validator_LowVisibilityFails()
        {
            Assert.IsTrue(WorldValidator.Validate("t", 80, ValidPlan(), Markers(), 0.55f).Errors.Any(e => e.Contains("sichtbar")));
            Assert.IsTrue(WorldValidator.Validate("t", 80, ValidPlan(), Markers(), 0.6f).Ok);
        }

        [Test] public void Validator_MainLandmarkRules()
        {
            var p = ValidPlan(); p.Landmarks[0].Height = 50f;
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("außerhalb")));
            p = ValidPlan(); p.Landmarks[0].Interior = false;
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("Innenraum")));
            p = ValidPlan(); p.Landmarks.RemoveAt(0);
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("kein Haupt-Wahrzeichen")));
        }

        [Test] public void Validator_IndistinctLandmarksFail()
        {
            var p = ValidPlan(); p.Landmarks.Add(new LandmarkDef { Id = "x", Name = "Dom 2", Kind = LandmarkKind.Cathedral, Height = 95f, Primary = Color.white });
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("nicht unterscheidbar")));
        }

        [Test] public void Validator_ClimbStructure()
        {
            var p = ValidPlan(); p.Climbs[0].Stages.RemoveAt(2);
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("statt 3 Stufen")));
            p = ValidPlan(); p.Climbs[0].Stages[1].Checkpoints.Clear();
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("ohne Kontrollpunkt")));
            p = ValidPlan(); p.Climbs[0].Stages[0].Gate = GateDef.Of(Traversal.Raptor);
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("Stufe 1 nicht zu Fuß")));
            p = ValidPlan(); p.Climbs[0].Stages[1].Gate.FootAlternative = false;
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("Springer + Grat")));
        }

        [Test] public void Validator_SecretInsideMarkerFails()
        {
            var p = ValidPlan(); p.Secrets[0].Position = new Vector3(10, 0, 5);
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("Geheimnis g0")));
        }

        [Test] public void Validator_OwnStageMarkerAllowsSummitChest()
        {
            var p = ValidPlan(); p.Secrets[0].Position = new Vector3(10, 0, 5);
            var m = new List<MarkerCheck> { new MarkerCheck { Id = "w1_gipfel", Kind = MapMarkerKind.Vista, Position = new Vector3(12, 0, 5), OwnStage = true } };
            Assert.IsFalse(WorldValidator.Validate("t", 80, p, m, 0.8f).Errors.Any(e => e.Contains("Geheimnis g0")));
        }

        [Test] public void Validator_ShortShortcutFails_CompactZoneWarns()
        {
            var p = ValidPlan(); p.Shortcuts[0].FootPath = 300f;
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("spart nur")));
            p.CompactZone = true;
            var r = WorldValidator.Validate("t", 80, p, Markers(), 0.8f);
            Assert.IsTrue(r.Ok); Assert.IsTrue(r.Warnings.Any(w => w.Contains("spart nur")));
            p.Shortcuts.Clear();
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Ok);
            p.CompactZone = false;
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("keine Reittier-Abkürzung")));
        }

        [Test] public void Validator_MinimumCounts()
        {
            var p = ValidPlan(); p.Secrets.RemoveAt(0); p.Lore.RemoveAt(0); p.Loops.RemoveAt(0); p.Scenes.RemoveAt(0); p.Climbs.Clear();
            var r = WorldValidator.Validate("t", 80, p, Markers(), 0.8f);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Geheimnisse")));
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Lore")));
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Entdeckungsschleifen")));
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Umwelt-Szenen")));
            Assert.IsTrue(r.Errors.Any(e => e.Contains("kein Aufstieg")));
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f, false).Errors.All(e => !e.Contains("Lore")), "ohne Mengen-Prüfung");
        }

        [Test] public void Validator_LoopNeedsTriggerAndTarget()
        {
            var p = ValidPlan(); p.Loops[0].Target = "";
            Assert.IsTrue(WorldValidator.Validate("t", 80, p, Markers(), 0.8f).Errors.Any(e => e.Contains("ohne Auslöser")));
        }

        [Test] public void Validator_HeightRanges()
        {
            Assert.AreEqual((80f, 140f), WorldValidator.HeightRange(LandmarkKind.Cathedral, true));
            Assert.AreEqual((60f, 120f), WorldValidator.HeightRange(LandmarkKind.Pyramid, true));
            Assert.AreEqual((25f, 60f), WorldValidator.HeightRange(LandmarkKind.Pyramid, false));
            Assert.AreEqual((150f, 300f), WorldValidator.HeightRange(LandmarkKind.Massif, true));
        }
    }
}
