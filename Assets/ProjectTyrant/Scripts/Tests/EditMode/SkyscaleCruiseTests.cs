using NUnit.Framework;
using ProjectTyrant.Flight;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Spieler-Wunsch 2026-11-12: Himmelsschuppe hat immer dasselbe horizontale Tempo – am Boden wie in der Luft, ohne Tempo-Effekte.</summary>
    public class SkyscaleCruiseTests
    {
        [TestCase(FlightState.Glide)] [TestCase(FlightState.Hover)] [TestCase(FlightState.Climb)] [TestCase(FlightState.Sink)] [TestCase(FlightState.Descend)]
        public void Cruise_IsAlwaysTheSame(FlightState s) => Assert.AreEqual(SkyscaleSteer.FixedSpeed, SkyscaleSteer.Cruise(s), 1e-5f);

        [Test] public void Cruise_EqualsGroundSpeedOfTheMount() => Assert.AreEqual(MountStats.Default(MountKind.Skyscale).Speed, SkyscaleSteer.FixedSpeed, 1e-5f);

        [Test] public void Cruise_WallClingStands() => Assert.AreEqual(0f, SkyscaleSteer.Cruise(FlightState.WallCling));

        [Test]
        public void Steering_ReachesFixedSpeed_NotMore()
        {
            var v = Vector3.zero;
            for (int i = 0; i < 100; i++) v = SkyscaleSteer.Step(v, Vector3.forward, SkyscaleSteer.Cruise(FlightState.Glide), 0.02f);
            Assert.AreEqual(SkyscaleSteer.FixedSpeed, v.magnitude, 0.01f);
            for (int i = 0; i < 100; i++) v = SkyscaleSteer.Step(v, Vector3.right, SkyscaleSteer.Cruise(FlightState.Sink), 0.02f);
            Assert.AreEqual(SkyscaleSteer.FixedSpeed, v.magnitude, 0.01f, "auch erschöpft sinkend gleich schnell");
        }
    }
}
