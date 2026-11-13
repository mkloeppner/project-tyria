using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>VFX-Lauf V1: Fluid-Löser, Flipbook-Baker, SDF, Kaustik, Blitz, Bewegungs-Metrik, FxMeshKit 2.0.</summary>
    public class ElementSimV1Tests
    {
        static SimRecipe Recipe(FlipbookMode mode, int seed = 3, bool fire = true)
        {
            var r = ScriptableObject.CreateInstance<SimRecipe>();
            r.Id = "test_" + mode; r.Seed = seed; r.Mode = mode; r.Grid = 48; r.Columns = 4; r.Rows = 4; r.TileSize = 32; r.StepsPerFrame = 2; r.Iterations = 10;
            r.Dt = 1f / 60f; r.WarmupSteps = mode == FlipbookMode.Loop ? 30 : 0;
            r.Buoyancy = fire ? 2f : 0f; r.Gravity = fire ? 0f : 3f; r.Vorticity = fire ? 2f : 0f; r.ColorFromTemperature = fire;
            r.Emitters.Add(new SimEmitter { X = 0.5f, Y = fire ? 0.15f : 0.8f, Radius = 0.12f, Density = 30f, Temperature = fire ? 30f : 0f, VelY = fire ? 0.5f : -0.5f, End = mode == FlipbookMode.Burst ? 0.3f : 1f });
            return r;
        }

        static FluidSolver2D Solver(int seed = 1) => new FluidSolver2D(32, seed) { Iterations = 12 };

        [Test]
        public void Rng_IsDeterministicAndInRange()
        {
            var a = new SimRng(42); var b = new SimRng(42);
            for (int i = 0; i < 1000; i++) { float x = a.Next(); Assert.AreEqual(x, b.Next()); Assert.That(x, Is.InRange(0f, 0.9999999f)); }
            Assert.AreNotEqual(new SimRng(1).Next(), new SimRng(2).Next());
        }

        [Test]
        public void Solver_RejectsInvalidGrid() => Assert.Throws<System.ArgumentOutOfRangeException>(() => new FluidSolver2D(4, 1));

        [Test]
        public void Solver_SameSeed_IdenticalFields()
        {
            var e = new SimEmitter { Density = 20f, Temperature = 20f, VelY = 0.4f };
            var a = Solver(7); var b = Solver(7);
            for (int s = 0; s < 20; s++) { a.Emit(e, 0.5f, 1f / 60f); a.Step(1f / 60f); b.Emit(e, 0.5f, 1f / 60f); b.Step(1f / 60f); }
            CollectionAssert.AreEqual(a.Dens, b.Dens); CollectionAssert.AreEqual(a.U, b.U);
        }

        [Test]
        public void Solver_DifferentSeed_DifferentFlicker()
        {
            var e = new SimEmitter { Density = 20f, Jitter = 0.5f };
            var a = Solver(1); var b = Solver(2);
            a.Emit(e, 0.5f, 0.1f); b.Emit(e, 0.5f, 0.1f);
            Assert.AreNotEqual(a.TotalDensity(), b.TotalDensity());
        }

        [Test]
        public void Solver_Buoyancy_HotSmokeRises()
        {
            var s = Solver(); s.Buoyancy = 3f; var e = new SimEmitter { Y = 0.2f, Density = 20f, Temperature = 30f };
            for (int i = 0; i < 10; i++) { s.Emit(e, 0.5f, 1f / 60f); s.Step(1f / 60f); }
            float y0 = s.DensityCentroidY();
            for (int i = 0; i < 40; i++) s.Step(1f / 60f);
            Assert.Greater(s.DensityCentroidY(), y0 + 0.02f);
        }

        [Test]
        public void Solver_Gravity_WaterFalls()
        {
            var s = Solver(); s.Buoyancy = 0f; s.Gravity = 4f; var e = new SimEmitter { Y = 0.8f, Density = 20f };
            for (int i = 0; i < 10; i++) { s.Emit(e, 0.5f, 1f / 60f); s.Step(1f / 60f); }
            float y0 = s.DensityCentroidY();
            for (int i = 0; i < 40; i++) s.Step(1f / 60f);
            Assert.Less(s.DensityCentroidY(), y0 - 0.02f);
        }

        [Test]
        public void Solver_Projection_KeepsVelocityNearlyDivergenceFree()
        {
            var s = Solver(); s.Buoyancy = 3f; s.Iterations = 30; var e = new SimEmitter { Density = 20f, Temperature = 30f, VelX = 0.5f };
            for (int i = 0; i < 30; i++) { s.Emit(e, 0.5f, 1f / 60f); s.Step(1f / 60f); }
            Assert.Less(s.MeanAbsDivergence(), s.MeanSpeed() * 0.25f + 1e-4f);
        }

        [Test]
        public void Solver_Obstacle_BlocksDensity()
        {
            var s = Solver(); s.Gravity = 4f; s.Buoyancy = 0f; s.AddObstacle(0f, 0f, 1f, 0.3f); var e = new SimEmitter { Y = 0.7f, Density = 20f, VelY = -1f };
            for (int i = 0; i < 60; i++) { s.Emit(e, 0.5f, 1f / 60f); s.Step(1f / 60f); }
            for (int j = 1; j <= s.N; j++) for (int i = 1; i <= s.N; i++) if (s.Solid[s.IX(i, j)]) Assert.AreEqual(0f, s.Dens[s.IX(i, j)]);
            Assert.Greater(s.DensityCentroidY(), 0.3f);
        }

        [Test]
        public void Solver_Vorticity_AddsSwirl()
        {
            float Speed(float vort)
            {
                var s = Solver(5); s.Buoyancy = 3f; s.Vorticity = vort; var e = new SimEmitter { Density = 20f, Temperature = 30f, Jitter = 0.6f };
                for (int i = 0; i < 40; i++) { s.Emit(e, 0.5f, 1f / 60f); s.Step(1f / 60f); }
                return s.MeanSpeed();
            }
            Assert.Greater(Speed(8f), Speed(0f));
        }

        [TestCase(EmitterShape.Point)] [TestCase(EmitterShape.Line)] [TestCase(EmitterShape.Ring)]
        public void Emitter_ShapesAddDensity(EmitterShape shape)
        {
            var s = Solver(); s.Emit(new SimEmitter { Shape = shape, X = 0.3f, X2 = 0.7f, Y = 0.5f, Y2 = 0.5f, Radius = 0.08f, Density = 10f }, 0.5f, 0.1f);
            Assert.Greater(s.TotalDensity(), 0f);
        }

        [Test]
        public void Emitter_MaskShape_UsesMask()
        {
            var s = Solver(); var mask = new float[32 * 32]; mask[5 + 32 * 5] = 1f;
            s.Emit(new SimEmitter { Shape = EmitterShape.Mask, Mask = mask, Density = 10f }, 0.5f, 0.1f);
            Assert.Greater(s.Dens[s.IX(6, 6)], 0f); Assert.AreEqual(0f, s.Dens[s.IX(20, 20)]);
        }

        [Test]
        public void Emitter_InactiveOutsideWindow()
        {
            var s = Solver(); s.Emit(new SimEmitter { Density = 10f, Start = 0f, End = 0.2f }, 0.5f, 0.1f);
            Assert.AreEqual(0f, s.TotalDensity());
        }

        [Test]
        public void Baker_TwoRuns_IdenticalHash()
        {
            var a = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst)); var b = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst));
            Assert.AreEqual(a.Hash, b.Hash);
            Assert.AreNotEqual(a.Hash, FlipbookBaker.Bake(Recipe(FlipbookMode.Burst, 4)).Hash);
        }

        [Test]
        public void Baker_ProducesFourAtlasesWithHalfResMotion()
        {
            var r = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst));
            Assert.AreEqual(128 * 128, r.Color.Length); Assert.AreEqual(128 * 128, r.Normal.Length); Assert.AreEqual(128 * 128, r.Emissive.Length);
            Assert.AreEqual(64 * 64, r.Motion.Length); Assert.AreEqual(16, r.Frames);
        }

        [Test]
        public void Baker_Burst_StartsAndEndsEmpty()
        {
            var r = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst));
            float Sum(float[] d) { float s = 0f; foreach (var x in d) s += x; return s; }
            Assert.AreEqual(0f, Sum(r.FrameDensity[r.Frames - 1]), 1e-4f);
            Assert.Greater(Sum(r.FrameDensity[r.PeakFrame]), 1f);
            Assert.Greater(r.PeakFrame, 0); Assert.Less(r.PeakFrame, r.Frames - 1);
        }

        [Test]
        public void Baker_Loop_SeamWithinTwoPercent()
        {
            var rec = Recipe(FlipbookMode.Loop); rec.StepsPerFrame = 1;   // 60 FPS Wiedergabe
            var r = FlipbookBaker.Bake(rec);
            Assert.LessOrEqual(r.LoopSeam, 0.02f);
            Assert.Greater(r.MeanFrameDelta, 0f, "Loop darf nicht stillstehen");
        }

        [Test]
        public void Baker_Loop_SeamNoLargerThanOrdinaryFrameStep()
        {
            var r = FlipbookBaker.Bake(Recipe(FlipbookMode.Loop));
            Assert.LessOrEqual(r.LoopSeam, r.MeanFrameDelta * 1.25f, "Naht springt stärker als ein gewöhnlicher Bildwechsel");
        }

        [Test]
        public void Baker_NormalsAreUnitLength()
        {
            var r = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst));
            for (int i = 0; i < r.Normal.Length; i += 97)
            {
                var c = r.Normal[i]; var n = new Vector3(c.r / 255f * 2f - 1f, c.g / 255f * 2f - 1f, c.b / 255f * 2f - 1f);
                Assert.AreEqual(1f, n.magnitude, 0.03f);
            }
        }

        [Test]
        public void Baker_FireMotionPointsUp()
        {
            var r = FlipbookBaker.Bake(Recipe(FlipbookMode.Burst));
            float sum = 0f; int n = 0;
            foreach (var c in r.Motion) { float v = c.g / 255f * 2f - 1f; if (Mathf.Abs(v) > 0.02f) { sum += v; n++; } }
            Assert.Greater(n, 0); Assert.Greater(sum / n, 0f);
        }

        [Test]
        public void Recipe_ParamHash_ChangesWithParameters_AndDrivesIncrementalBake()
        {
            var r = Recipe(FlipbookMode.Burst); string h = r.ComputeParamHash();
            Assert.AreEqual(h, r.ComputeParamHash());
            r.ParamHash = h; r.ResultHash = "x"; Assert.IsFalse(r.NeedsBake);
            r.Vorticity += 0.5f; Assert.AreNotEqual(h, r.ComputeParamHash()); Assert.IsTrue(r.NeedsBake);
        }

        [Test]
        public void Recipe_Problems_EnforceBudgets()
        {
            var r = Recipe(FlipbookMode.Burst); Assert.IsEmpty(r.Problems().FindAll(p => !p.Contains("Loop")));
            r.Columns = 8; r.TileSize = 512; Assert.IsTrue(r.Problems().Exists(p => p.Contains("2048")));
            var slow = Recipe(FlipbookMode.Burst); slow.StepsPerFrame = 4; slow.Dt = 1f / 48f; Assert.IsTrue(slow.Problems().Exists(p => p.Contains("FPS")));
            var empty = Recipe(FlipbookMode.Burst); empty.Emitters.Clear(); Assert.IsTrue(empty.Problems().Exists(p => p.Contains("Emitter")));
        }

        [TestCase(SdfShape.Circle)] [TestCase(SdfShape.Ring)] [TestCase(SdfShape.Cone)] [TestCase(SdfShape.Star)] [TestCase(SdfShape.Blade)] [TestCase(SdfShape.Glyph)] [TestCase(SdfShape.Drop)]
        public void Sdf_EveryShapeHasInsideAndOutside(SdfShape s)
        {
            var m = Sdf.Mask(s, 64); float inside = 0f; foreach (var v in m) inside += v;
            Assert.Greater(inside / m.Length, 0.01f, "Form leer"); Assert.Less(inside / m.Length, 0.9f, "Form füllt alles");
            Assert.AreEqual(0f, m[0], 0.01f, "Ecke muss außen liegen");
        }

        [Test]
        public void Sdf_UnionSubtract()
        {
            Assert.AreEqual(-1f, Sdf.Union(-1f, 2f)); Assert.AreEqual(2f, Sdf.Subtract(1f, -2f));
            Assert.LessOrEqual(Sdf.SmoothUnion(0.1f, 0.1f, 0.2f), 0.1f);
            Assert.AreEqual(1f, Sdf.Edge(-1f, 0.1f)); Assert.AreEqual(0f, Sdf.Edge(1f, 0.1f));
        }

        [Test]
        public void Sdf_WarpIsDeterministic()
        {
            CollectionAssert.AreEqual(Sdf.Mask(SdfShape.Star, 32, 0.05f, 0.3f, 9), Sdf.Mask(SdfShape.Star, 32, 0.05f, 0.3f, 9));
            CollectionAssert.AreNotEqual(Sdf.Mask(SdfShape.Star, 32, 0.05f, 0.3f, 9), Sdf.Mask(SdfShape.Star, 32, 0.05f, 0f, 9));
        }

        [Test]
        public void Caustics_LoopSeamlessAndBright()
        {
            var a = CausticGenerator.Frame(48, 0f, 5); var b = CausticGenerator.Frame(48, 1f, 5); var mid = CausticGenerator.Frame(48, 0.5f, 5);
            Assert.Less(FlipbookBaker.FrameDelta(a, b), 0.01f);
            Assert.Greater(FlipbookBaker.FrameDelta(a, mid), 0.01f, "Kaustik muss sich bewegen");
            float max = 0f, mean = 0f; foreach (var v in a) { max = Mathf.Max(max, v); mean += v; } mean /= a.Length;
            Assert.AreEqual(1f, max, 1e-4f); Assert.Less(mean, 0.8f, "Kaustik braucht helle Linien auf dunklerem Grund");
        }

        [Test]
        public void Bolt_DeterministicBranchesAndThinning()
        {
            var a = BoltGenerator.Generate(3, Vector3.zero, Vector3.up * 10f); var b = BoltGenerator.Generate(3, Vector3.zero, Vector3.up * 10f);
            Assert.AreEqual(a.Count, b.Count); Assert.AreEqual(a[5].B, b[5].B);
            Assert.IsTrue(a.Exists(s => s.Depth > 0), "keine Verzweigung");
            foreach (var s in a) if (s.Depth > 0) Assert.Less(s.Width, 0.12f);
            Assert.IsTrue(a.Exists(s => (s.B - Vector3.up * 10f).sqrMagnitude < 1e-6f), "Hauptast erreicht das Ziel");
            var c = BoltGenerator.Generate(3, Vector3.zero, Vector3.up * 10f, frame: 1);
            Assert.AreNotEqual(a[1].B, c[1].B, "Zittern je Frame");
        }

        [Test]
        public void MotionMetrics_DetectsCoherentMotionAndDeadFrames()
        {
            const int w = 48, h = 48; var moving = new List<float[]>(); var still = new List<float[]>();
            for (int f = 0; f < 6; f++)
            {
                var m = new float[w * h]; var s = new float[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { m[x + y * w] = Noise.Value((x - f * 2) * 0.35f, y * 0.35f, 4); s[x + y * w] = Noise.Value(x * 0.35f, y * 0.35f, 4); }
                moving.Add(m); still.Add(s);
            }
            var rm = MotionMetrics.Measure(moving, w, h); var rs = MotionMetrics.Measure(still, w, h);
            Assert.Greater(rm.MeanFrameDiff, 0.01f); Assert.Greater(rm.Coherence, 0.6f); Assert.AreEqual(0f, rm.DeadShare);
            Assert.AreEqual(1f, rs.DeadShare); Assert.AreEqual(0f, rs.Coherence);
        }

        [Test]
        public void FxMeshKit_EveryShapeValidAndLowPoly()
        {
            foreach (FxMeshKit.Shape s in System.Enum.GetValues(typeof(FxMeshKit.Shape)))
            {
                var m = FxMeshKit.Build(s, 2);
                Assert.IsTrue(m.IsValid(), s + " ungültig"); Assert.Greater(m.TriangleCount, 0, s.ToString());
                Assert.LessOrEqual(m.TriangleCount, 1500, s + " nicht Low Poly");
                Assert.AreEqual(m.Vertices.Count, m.Uv.Count); Assert.AreEqual(m.Vertices.Count, m.Normals.Count);
                Assert.Less(m.Bounds.size.magnitude, 4f, s + " nicht auf 1 m normiert");
            }
        }

        [Test]
        public void FxMeshKit_ShardsSplitIntoRequestedPieces()
        {
            var m = FxMeshKit.Shards(12, 3); var ids = new HashSet<float>();
            foreach (var c in m.Colors) ids.Add(c.r);
            Assert.AreEqual(12, ids.Count);
            Assert.AreEqual(8, new HashSet<float>(ColorsR(FxMeshKit.Shards(3, 3))).Count, "mindestens 8 Splitter");
        }

        static IEnumerable<float> ColorsR(FxMeshData m) { foreach (var c in m.Colors) yield return c.r; }

        [Test]
        public void FxMeshKit_BoltMeshFromGenerator()
        {
            var segs = BoltGenerator.Generate(1, Vector3.zero, Vector3.up * 3f, 3);
            var m = FxMeshKit.Bolt(segs); Assert.IsTrue(m.IsValid()); Assert.AreEqual(segs.Count * 4, m.TriangleCount);
        }
    }
}
