using NUnit.Framework;
using ProjectTyrant.Visuals;
using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>VFX-Lauf V1: GPU-Partikel (Budgets, Fallback, CPU-Referenz der Kräfte) und Verzerrungs-Puffer.</summary>
    public class GpuDistortV1Tests
    {
        [Test]
        public void Budget_PerTier()
        {
            Assert.AreEqual(200000, GpuParticleModel.Budget(QualityTier.Ultra)); Assert.AreEqual(100000, GpuParticleModel.Budget(QualityTier.Hoch));
            Assert.AreEqual(0, GpuParticleModel.Budget(QualityTier.Mittel)); Assert.AreEqual(0, GpuParticleModel.Budget(QualityTier.Niedrig));
        }

        [TestCase(QualityTier.Niedrig, true, false)] [TestCase(QualityTier.Mittel, true, false)] [TestCase(QualityTier.Hoch, true, true)]
        [TestCase(QualityTier.Ultra, true, true)] [TestCase(QualityTier.Ultra, false, false)]
        public void UseGpu_OnlyHighTiersWithCompute(QualityTier t, bool compute, bool expected) => Assert.AreEqual(expected, GpuParticleModel.UseGpu(t, compute));

        [Test]
        public void Fallback_NeverZero_AndRisesWithTier()
        {
            Assert.Greater(GpuParticleModel.FallbackEmissionShare(QualityTier.Niedrig), 0f);
            Assert.Greater(GpuParticleModel.FallbackEmissionShare(QualityTier.Mittel), GpuParticleModel.FallbackEmissionShare(QualityTier.Niedrig));
        }

        [Test] public void ParticleStride_Matches48Bytes() => Assert.AreEqual(GpuParticle.Stride, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuParticle)));

        static GpuParticle Run(GpuParticleSettings s, int steps, float ground = float.NegativeInfinity, Vector3 start = default, Vector3 vel = default)
        {
            var p = new GpuParticle { Position = start, Velocity = vel, Life = 100f };
            for (int i = 0; i < steps; i++) GpuParticleModel.Step(ref p, s, Vector3.zero, Vector3.zero, 1f / 60f, ground);
            return p;
        }

        [Test]
        public void Step_GravityPullsDown_DragSlows()
        {
            var s = new GpuParticleSettings { Drag = 0f }; Assert.Less(Run(s, 30).Position.y, -0.5f);
            var d = new GpuParticleSettings { Gravity = Vector3.zero, Drag = 2f }; Assert.Less(Run(d, 60, vel: Vector3.right * 5f).Velocity.x, 1f);
        }

        [Test]
        public void Step_VortexSpinsAroundAxis()
        {
            var s = new GpuParticleSettings { Gravity = Vector3.zero, Drag = 0f, VortexStrength = 5f };
            var p = Run(s, 30, start: Vector3.right);
            Assert.Less(p.Velocity.z, -0.5f, "Drehung um +y (Kreuzprodukt up × radial)"); Assert.AreEqual(0f, p.Velocity.y, 1e-5f);
        }

        [Test]
        public void Step_AttractAndRepel()
        {
            var a = new GpuParticleSettings { Gravity = Vector3.zero, Drag = 0f, Attract = 5f }; Assert.Less(Run(a, 20, start: Vector3.right * 2f).Position.x, 2f);
            var r = new GpuParticleSettings { Gravity = Vector3.zero, Drag = 0f, Attract = -5f }; Assert.Greater(Run(r, 20, start: Vector3.right * 2f).Position.x, 2f);
        }

        [Test]
        public void Step_DropsStickToGround_SparksBounce()
        {
            var drop = new GpuParticleSettings { StickBelowSpeed = 100f };
            var p = Run(drop, 120, 0f, Vector3.up); Assert.AreEqual(0f, p.Position.y, 1e-4f); Assert.AreEqual(1u, p.Flags & 1u); Assert.AreEqual(Vector3.zero, p.Velocity);
            var spark = new GpuParticleSettings { StickBelowSpeed = 0.1f, Bounce = 0.8f, Drag = 0f };
            var q = new GpuParticle { Position = new Vector3(0, 0.01f, 0), Velocity = Vector3.down * 5f, Life = 10f };
            GpuParticleModel.Step(ref q, spark, Vector3.zero, Vector3.zero, 1f / 60f, 0f);
            Assert.Greater(q.Velocity.y, 0f, "Funke springt");
        }

        [Test]
        public void Step_LifetimeAndSizeCurve()
        {
            var s = new GpuParticleSettings { SizeStart = 0.2f, SizeEnd = 0.05f };
            var p = new GpuParticle { Life = 0.5f };
            for (int i = 0; i < 40; i++) GpuParticleModel.Step(ref p, s, Vector3.zero, Vector3.zero, 1f / 60f);
            Assert.IsFalse(p.Alive); Assert.AreEqual(0.05f, p.Size, 0.02f);
        }

        [TestCase(GpuEmitterShape.Point)] [TestCase(GpuEmitterShape.Sphere)] [TestCase(GpuEmitterShape.Cone)] [TestCase(GpuEmitterShape.RingEdge)] [TestCase(GpuEmitterShape.Line)]
        public void Spawn_ShapesProduceLivingParticles(GpuEmitterShape shape)
        {
            var s = new GpuParticleSettings { Shape = shape, ShapeRadius = 1f };
            for (uint i = 1; i < 20; i++)
            {
                var p = GpuParticleModel.Spawn(s, Vector3.zero, Vector3.up, i);
                Assert.IsTrue(p.Alive); Assert.LessOrEqual(p.Position.magnitude, 1.01f); Assert.Greater(p.Velocity.magnitude, 0f);
                Assert.AreEqual(p.Position, GpuParticleModel.Spawn(s, Vector3.zero, Vector3.up, i).Position, "deterministisch je Seed");
            }
        }

        [Test]
        public void Spawn_ConeStaysInsideAngle()
        {
            var s = new GpuParticleSettings { Shape = GpuEmitterShape.Cone, ConeAngle = 20f };
            for (uint i = 1; i < 50; i++) Assert.LessOrEqual(Vector3.Angle(Vector3.up, GpuParticleModel.Spawn(s, Vector3.zero, Vector3.up, i).Velocity), 20.5f);
        }

        [Test]
        public void Distortion_RegistryAndClamps()
        {
            DistortionAccumulator.Clear(); DistortionAccumulator.Force = false;
            Assert.IsFalse(DistortionAccumulator.Wanted);
            var go = new GameObject("q"); DistortionAccumulator.Register(go); Assert.AreEqual(1, DistortionAccumulator.ActiveSources); Assert.IsTrue(DistortionAccumulator.Wanted);
            DistortionAccumulator.Register(go); Assert.AreEqual(1, DistortionAccumulator.ActiveSources, "doppelte Anmeldung zählt einmal");
            DistortionAccumulator.Unregister(go); Assert.IsFalse(DistortionAccumulator.Wanted);
            Object.DestroyImmediate(go);
            Assert.AreEqual(24f, DistortionAccumulator.ClampShift(40f, false)); Assert.AreEqual(12f, DistortionAccumulator.ClampShift(40f, true));
            Assert.AreEqual(6f, DistortionAccumulator.MaxDispersionPx);
        }

        [Test]
        public void RefractShader_ExistsWithDistortPass()
        {
            var sh = Shader.Find("ProjectTyrant/FX/Refract"); Assert.IsNotNull(sh);
            var mat = new Material(sh); Assert.Greater(mat.FindPass("PTDistort"), -1); Object.DestroyImmediate(mat);
            Assert.IsNotNull(Shader.Find("ProjectTyrant/FX/GpuParticle"));
        }
    }
}
