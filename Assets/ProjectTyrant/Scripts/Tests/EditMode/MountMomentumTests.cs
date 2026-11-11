using NUnit.Framework;
using ProjectTyrant.Flight;

namespace ProjectTyrant.Tests
{
    /// <summary>Nach Lauf 11 (Spieler-Wunsch): Schwung bleibt beim Abheben/Aufsitzen erhalten; Himmelsschuppe sprintet mit V.</summary>
    public class MountMomentumTests
    {
        static FlightMotor Sky(float groundSpeed, int rank = 0) { var m = new FlightMotor(FlightParams.Skyscale(rank)); m.GroundSpeed = groundSpeed; m.Charges = 2; return m; }   // 2 Ausweich-Balken

        [Test] public void Skyscale_TakeOffWhileRunning_KeepsGroundSpeed()
        {
            var m = Sky(9.1f);
            m.Step(0.02f, new FlightInput { Jump = true, JumpHeld = true });
            Assert.AreEqual(FlightState.Climb, m.State);
            Assert.AreEqual(9.1f, m.Forward, 0.01f, "kein Neustart bei 0");
            for (int i = 0; i < 25; i++) m.Step(0.02f, new FlightInput { JumpHeld = true });   // 0,5 s steigen
            Assert.AreEqual(9.1f, m.Forward, 0.01f, "Steigen behält das Tempo");
            Assert.Greater(m.Vertical, 0f);
        }

        [Test] public void Skyscale_WalkOffEdge_KeepsGroundSpeed()
        {
            var m = Sky(8f); m.LeaveGround();
            Assert.AreEqual(FlightState.Hover, m.State); Assert.AreEqual(8f, m.Forward, 0.01f);
        }

        [Test] public void Griffon_TakeOffFasterThanGlide_KeepsGroundSpeed()
        {
            var g = new FlightMotor(FlightParams.Griffon(0)) { GroundSpeed = 14f };
            g.Step(0.02f, new FlightInput { Jump = true });
            Assert.AreEqual(14f, g.Forward, 0.3f, "schnelles Bodentempo bleibt (über dem Gleit-Minimum)");
            var slow = new FlightMotor(FlightParams.Griffon(0)) { GroundSpeed = 2f };
            slow.Step(0.02f, new FlightInput { Jump = true });
            Assert.AreEqual(slow.Run * 1.6f, slow.Forward, 0.3f, "Gleit-Minimum unverändert");
        }

        [Test] public void Skyscale_Dash_Short_CostsBarAndPower_ThenCarriesMomentum()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 40f;
            float e0 = m.Endurance;
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.Started, m.LastDash);
            Assert.AreEqual(1, m.Charges, "1 Ausweich-Balken je Sprint");
            Assert.AreEqual(e0 - m.P.DashCost, m.Endurance, 0.2f, "dazu etwas Flugkraft");
            float dist = m.Forward * 0.02f;
            for (int i = 0; i < 30 && m.State == FlightState.Dash; i++) { m.Step(0.02f, new FlightInput()); if (m.State == FlightState.Dash) dist += m.Forward * 0.02f; }
            Assert.That(dist, Is.InRange(7f, 11f), "kürzer als vorher (≈ 9,5 m statt 14 m)");
            Assert.AreEqual(FlightState.Hover, m.State, "Sprint endet");
            Assert.Greater(m.Forward, m.Run, "Schwung bleibt nach dem Sprint");
        }

        [Test] public void Skyscale_Dash_NeedsBar_RespectsCooldown()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 40f;
            m.Step(0.02f, new FlightInput { Dash = true });
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.Cooldown, m.LastDash, "kein Dauer-Sprint");
            for (int i = 0; i < 60; i++) m.Step(0.02f, new FlightInput());
            m.Charges = 0;
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.NoCharge, m.LastDash, "ohne Ausweich-Balken kein Sprint");
            m.Charges = 1; m.Endurance = 2f;
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.NoEndurance, m.LastDash);
            Assert.AreNotEqual(FlightState.Dash, m.State);
        }

        [Test] public void Skyscale_StandingStillInAir_IsFree_EmptyPowerOnlyStopsMoving()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 40f; m.Endurance = 0f;
            for (int i = 0; i < 50; i++) m.Step(0.02f, new FlightInput());
            Assert.AreEqual(FlightState.Hover, m.State, "leer, aber still: schwebt weiter (leichtes Sinken)");
            Assert.AreEqual(40f - 0.5f, m.Altitude, 0.05f);
            m.Step(0.02f, new FlightInput { Moving = true });
            Assert.AreEqual(FlightState.Sink, m.State, "leer und Bewegung: sinkt");
            m.Step(0.02f, new FlightInput { JumpHeld = true });
            Assert.AreEqual(FlightState.Sink, m.State, "leer: kein Steigen");
        }

        [Test] public void Skyscale_Dash_OnlyInAir()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(FlightState.Ground, m.State);
            Assert.AreEqual(DashResult.None, m.LastDash);
        }
        [Test] public void Skyscale_DescendWithC_SinksAndRegensEndurance()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 60f; m.Endurance = 0f;
            for (int i = 0; i < 50; i++) m.Step(0.02f, new FlightInput { Descend = true });   // 1 s
            Assert.AreEqual(FlightState.Descend, m.State);
            Assert.AreEqual(-m.P.DescendSpeed, m.Vertical, 0.01f, "sinkt kontrolliert");
            Assert.AreEqual(54f, m.Altitude, 0.3f, "6 m/s");
            Assert.AreEqual(20f, m.Endurance, 0.5f, "lädt Flugausdauer, auch aus 0");
            m.Step(0.02f, new FlightInput());
            Assert.AreNotEqual(FlightState.Descend, m.State, "Loslassen beendet das Sinken");
        }

        [Test] public void Skyscale_Descend_RegenCappedAndLandsOnGround()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 3f;
            for (int i = 0; i < 100; i++) m.Step(0.02f, new FlightInput { Descend = true });
            Assert.AreEqual(FlightState.Ground, m.State, "landet");
            Assert.LessOrEqual(m.Endurance, m.P.Endurance);
        }

        [Test] public void Griffon_IgnoresDescend()
        {
            var g = new FlightMotor(FlightParams.Griffon(0)); g.Step(0.02f, new FlightInput { Jump = true }); g.Altitude = 50f;
            g.Step(0.02f, new FlightInput { Descend = true });
            Assert.AreNotEqual(FlightState.Descend, g.State);
        }
        // Himmelsschuppe: freie Richtung in der Luft (nach Lauf 11)
        static UnityEngine.Vector3 Run(UnityEngine.Vector3 v, UnityEngine.Vector3 wish, float seconds, float cruise = 9.8f)
        { for (float t = 0f; t < seconds; t += 0.02f) v = SkyscaleSteer.Step(v, wish, cruise, 0.02f); return v; }

        [Test] public void SkyscaleSteer_ReversesFast()
        {
            var v = Run(new UnityEngine.Vector3(0, 0, 9.8f), UnityEngine.Vector3.back, 0.75f);
            Assert.Less(v.z, -9f, "volle Umkehr in unter 0,8 s");
        }

        [Test] public void SkyscaleSteer_StrafesAndStopsFast()
        {
            var side = Run(new UnityEngine.Vector3(0, 0, 9.8f), UnityEngine.Vector3.right, 0.6f);
            Assert.Greater(side.x, 8f); Assert.AreEqual(0f, side.z, 0.5f, "seitlich ohne Vorwärtsdrift");
            var stop = Run(new UnityEngine.Vector3(0, 0, 9.8f), UnityEngine.Vector3.zero, 0.4f);
            Assert.AreEqual(0f, stop.magnitude, 0.01f, "schnelles Abbremsen");
        }

        [Test] public void SkyscaleSteer_SameDirection_KeepsMomentum()
        {
            var v = Run(new UnityEngine.Vector3(0, 0, 15f), UnityEngine.Vector3.forward, 1f);
            Assert.Greater(v.z, 11.5f, "Schwung vom Aufsitzen/Sprint klingt nur langsam ab");
            var turned = Run(new UnityEngine.Vector3(0, 0, 15f), UnityEngine.Vector3.left, 1f);
            Assert.AreEqual(9.8f, turned.magnitude, 0.1f, "beim Abbiegen gilt sofort das Reisetempo");
        }
    }
}
