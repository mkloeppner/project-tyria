using NUnit.Framework;
using ProjectTyrant.Flight;

namespace ProjectTyrant.Tests
{
    /// <summary>Nach Lauf 11 (Spieler-Wunsch): Schwung bleibt beim Abheben/Aufsitzen erhalten; Himmelsschuppe sprintet mit V.</summary>
    public class MountMomentumTests
    {
        static FlightMotor Sky(float groundSpeed, int rank = 0) { var m = new FlightMotor(FlightParams.Skyscale(rank)); m.GroundSpeed = groundSpeed; return m; }

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

        [Test] public void Skyscale_Dash_IsFast_CostsEndurance_ThenCarriesMomentum()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 40f;
            float e0 = m.Endurance;
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.Started, m.LastDash);
            Assert.AreEqual(e0 - m.P.DashCost, m.Endurance, 0.2f);
            Assert.Less(m.P.DashCost, m.P.Endurance * 0.15f, "nur etwas Flugausdauer (< 15 %)");
            float dist = 0f;
            for (int i = 0; i < 30 && m.State == FlightState.Dash; i++) { dist += m.Forward * 0.02f; m.Step(0.02f, new FlightInput()); }
            Assert.Greater(dist, 12f, "Sprint trägt deutlich nach vorn");
            Assert.AreEqual(FlightState.Hover, m.State, "Sprint endet");
            Assert.Greater(m.Forward, m.Run, "Schwung bleibt nach dem Sprint");
            float alt = m.Altitude; Assert.AreEqual(40f, alt, 0.5f, "Sprint hält die Höhe");
        }

        [Test] public void Skyscale_Dash_RespectsCooldownAndEndurance()
        {
            var m = Sky(0f); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 40f;
            m.Step(0.02f, new FlightInput { Dash = true });
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.Cooldown, m.LastDash, "kein Dauer-Sprint");
            for (int i = 0; i < 60; i++) m.Step(0.02f, new FlightInput());
            m.Endurance = 5f;
            m.Step(0.02f, new FlightInput { Dash = true });
            Assert.AreEqual(DashResult.NoEndurance, m.LastDash);
            Assert.AreNotEqual(FlightState.Dash, m.State);
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
    }
}
