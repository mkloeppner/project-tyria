using NUnit.Framework;
using ProjectTyrant.Flight;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Spieler-Wunsch 2026-11-12: Greif lenkt beim Richtungswechsel das ganze Tempo um, kein Weitertreiben in die alte Richtung.</summary>
    public class GriffonSteerTests
    {
        const float Dt = 0.02f;

        static (Vector3 dir, float t) TurnUntil(Vector3 from, Vector3 to, float maxT = 2f)
        {
            var d = from; float t = 0f;
            while (t < maxT && Vector3.Angle(d, to) > 0.5f) { d = GriffonSteer.Direction(d, to, Dt); t += Dt; }
            return (d, t);
        }

        [Test]
        public void Reversal_CompletesWithinThirdOfASecond()
        {
            var (d, t) = TurnUntil(Vector3.forward, Vector3.back);
            Assert.LessOrEqual(t, 0.34f, "Kehrtwende dauert zu lange"); Assert.Less(Vector3.Angle(d, Vector3.back), 0.5f);
        }

        [Test]
        public void SpeedIsKept_AndNoComponentLeftInOldDirection()
        {
            const float speed = 21f;   // 3 × Lauf nach einem Sturz
            var d = Vector3.forward;
            for (float t = 0f; t < 0.4f; t += Dt)
            {
                d = GriffonSteer.Direction(d, Vector3.right, Dt);
                Assert.AreEqual(speed, GriffonSteer.Velocity(d, speed).magnitude, 1e-3f, "Betrag bleibt beim Umlenken");
            }
            var v = GriffonSteer.Velocity(d, speed);
            Assert.AreEqual(0f, Vector3.Dot(v, Vector3.forward), 1e-3f, "kein Rest in der alten Richtung");
            Assert.AreEqual(speed, Vector3.Dot(v, Vector3.right), 1e-3f);
        }

        [Test]
        public void OldDirectionTravel_DuringReversal_IsShort()
        {
            // Weg, der beim Umkehren noch in die alte Richtung zurückgelegt wird (vorher bei 1,8 rad/s: ≈ 12 m bei 21 m/s)
            const float speed = 21f; var d = Vector3.forward; float along = 0f;
            for (float t = 0f; t < 1f; t += Dt) { d = GriffonSteer.Direction(d, Vector3.back, Dt); along += Mathf.Max(0f, Vector3.Dot(GriffonSteer.Velocity(d, speed), Vector3.forward)) * Dt; }
            Assert.Less(along, 2.5f);
        }

        [Test]
        public void StaysHorizontalAndNormalized()
        {
            var d = GriffonSteer.Direction(new Vector3(1f, 0.5f, 0f), new Vector3(0f, -3f, 2f), Dt);
            Assert.AreEqual(0f, d.y, 1e-6f); Assert.AreEqual(1f, d.magnitude, 1e-4f);
            Assert.AreEqual(Vector3.forward, GriffonSteer.Direction(Vector3.zero, Vector3.zero, Dt));
            Assert.AreEqual(Vector3.right, GriffonSteer.Direction(Vector3.zero, Vector3.right * 4f, Dt));
        }

        [Test]
        public void SmallCorrections_AreImmediate()
        {
            var to = Quaternion.Euler(0f, 5f, 0f) * Vector3.forward;
            Assert.Less(Vector3.Angle(GriffonSteer.Direction(Vector3.forward, to, Dt), to), 0.01f, "5° folgt im selben Bild");
        }
    }
}
