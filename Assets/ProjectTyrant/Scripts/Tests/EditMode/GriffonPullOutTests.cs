using NUnit.Framework;
using ProjectTyrant.Flight;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Spieler-Wunsch 2026-11-12: Loslassen von V fängt den Greif weich ab (kein Sprung der Neigung), schnell, Tempo und Höchstmarke bleiben.</summary>
    public class GriffonPullOutTests
    {
        const float Dt = 0.02f;

        static FlightMotor Diving(float seconds = 3f)
        {
            var m = new FlightMotor(FlightParams.Griffon(0)); m.Altitude = 400f; m.TakeOff(); m.Altitude = 400f; m.Vertical = 0f; m.Ceiling = 9999f;
            for (float t = 0f; t < seconds; t += Dt) m.Step(Dt, new FlightInput { Dive = true });
            return m;
        }

        static float Pitch(FlightMotor m) => Mathf.Atan2(-m.Vertical, Mathf.Max(0.01f, m.Forward)) * Mathf.Rad2Deg;

        [Test]
        public void Release_NoPitchJump_SmoothToHorizontal()
        {
            var m = Diving(); float last = Pitch(m); Assert.Greater(last, 70f, "steil im Sturz");
            float t = 0f, maxStep = 0f;
            while (t < 2f)
            {
                m.Step(Dt, new FlightInput()); t += Dt;
                float p = Pitch(m); maxStep = Mathf.Max(maxStep, Mathf.Abs(p - last)); last = p;
                if (!m.PullingOut) break;
            }
            Assert.LessOrEqual(maxStep, FlightMotor.PullRateMax * Dt + 0.5f, "kein Sprung: höchstens " + FlightMotor.PullRateMax + "°/s");
            Assert.Greater(t, 0.25f, "nicht sofort waagrecht"); Assert.Less(t, 0.75f, "trotzdem schnell");
            Assert.AreEqual(0f, Pitch(m), 0.5f, "endet waagrecht");
        }

        [Test]
        public void Release_KeepsSpeedThroughTheArc()
        {
            var m = Diving(); float speed = m.DiveSpeed;
            for (int i = 0; i < 10; i++) { m.Step(Dt, new FlightInput()); Assert.AreEqual(speed, Mathf.Sqrt(m.Forward * m.Forward + m.Vertical * m.Vertical), 0.5f); }
        }

        [Test]
        public void Release_SlowsNearHorizontal()
        {
            Assert.Greater(FlightMotor.PullRate(80f, false), FlightMotor.PullRate(5f, false), "schnell am Anfang, sanft am Ende");
            Assert.GreaterOrEqual(FlightMotor.PullRate(0f, false), FlightMotor.PullRateMin);
        }

        [Test]
        public void HoldingS_FlowsIntoClimb_WithoutJump()
        {
            var m = Diving(); float last = Pitch(m), maxStep = 0f;
            for (int i = 0; i < 50; i++) { m.Step(Dt, new FlightInput { Pitch = -1f }); float p = Pitch(m); maxStep = Mathf.Max(maxStep, Mathf.Abs(p - last)); last = p; }
            Assert.AreEqual(-50f, Pitch(m), 2f, "geht direkt in den 50°-Aufschwung über");
            Assert.LessOrEqual(maxStep, FlightMotor.PullRateS * Dt + 1f);
        }

        [Test]
        public void ArcDrop_IsRecoverable_PeakUnchanged()
        {
            var m = Diving(); m.Step(Dt, new FlightInput()); float peak = m.PeakAltitude, alt = m.Altitude;
            while (m.PullingOut) m.Step(Dt, new FlightInput());
            Assert.Less(m.Altitude, alt, "der Bogen kostet etwas Höhe");
            Assert.AreEqual(peak, m.Altitude + m.Energy, 0.6f, "die Höhe kommt als Energie zurück, Höchstmarke bleibt");
        }

        [Test]
        public void DiveAgain_DuringArc_ContinuesFromCurrentAngle()
        {
            var m = Diving(); for (int i = 0; i < 5; i++) m.Step(Dt, new FlightInput());
            float before = Pitch(m); m.Step(Dt, new FlightInput { Dive = true });
            Assert.AreEqual(FlightState.Dive, m.State); Assert.IsFalse(m.PullingOut);
            Assert.Less(Mathf.Abs(Pitch(m) - before), FlightMotor.DivePitchRate * Dt + 1f, "kein Sprung beim erneuten Abtauchen");
        }
    }
}
