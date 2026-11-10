using NUnit.Framework;
using ProjectTyrant.Flight;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 10: Flugmodell (Greif, Himmelsschuppe) als Simulation mit festem Zeitschritt.</summary>
    public class Lauf10FlightTests
    {
        const float Dt = 0.02f;
        static FlightMotor Air(FlightParams p, float alt) { var m = new FlightMotor(p); m.Altitude = alt; m.TakeOff(); m.Altitude = alt; m.Vertical = 0f; return m; }
        static float Fly(FlightMotor m, System.Func<float, FlightInput> input, float maxT = 600f)
        {
            float t = 0f;
            while (m.Airborne && t < maxT) { m.Step(Dt, input(t)); t += Dt; }
            return t;
        }

        [Test] public void Griffon_ChargesAndGroundRegen()
        {
            var m = new FlightMotor(FlightParams.Griffon(0));
            Assert.AreEqual(3, m.Charges);
            m.Step(Dt, new FlightInput { Jump = true });
            Assert.AreEqual(2, m.Charges); Assert.IsTrue(m.Airborne);
            m.Step(Dt, new FlightInput { Jump = true }); m.Step(Dt, new FlightInput { Jump = true });
            Assert.AreEqual(0, m.Charges);
            Fly(m, t => new FlightInput());
            for (int i = 0; i < 100; i++) m.Step(Dt, new FlightInput());   // 2 s am Boden
            Assert.AreEqual(3, m.Charges);
            Assert.AreEqual(5, new FlightMotor(FlightParams.Griffon(1)).Charges, "Flügelkraft +2");
        }

        [Test] public void Griffon_GlideRatioAbout6To1()
        {
            var m = Air(FlightParams.Griffon(0), 100f);
            float dist = 0f; float t = 0f;
            while (m.Airborne && t < 600f) { m.Step(Dt, new FlightInput()); dist += m.Forward * Dt; t += Dt; }
            Assert.That(dist / 100f, Is.InRange(5f, 7f));
            Assert.That(t, Is.InRange(45f, 62f), "600 m bei 1,6 × 7 m/s ≈ 54 s reines Gleiten aus 100 m");
        }

        [Test] public void Griffon_DiveCapsSpeed()
        {
            var m = Air(FlightParams.Griffon(0), 300f);
            for (int i = 0; i < 1000 && m.Airborne; i++) m.Step(Dt, new FlightInput { Dive = true });
            Assert.LessOrEqual(m.Forward, m.Run * 4f + 0.01f);
            Assert.Greater(m.Forward, m.Run * 3.5f);
        }

        [Test] public void Griffon_PullUpRecoversHeight_WithLoss()
        {
            var m = Air(FlightParams.Griffon(0), 200f); m.Ceiling = 999f;   // ohne Höhendecke messen
            for (int i = 0; i < 300; i++) m.Step(Dt, new FlightInput { Dive = true });
            float low = m.Altitude, lost = 200f - low;
            m.Step(Dt, new FlightInput());
            float peak = m.Altitude;
            for (int i = 0; i < 400; i++) { m.Step(Dt, new FlightInput()); peak = System.Math.Max(peak, m.Altitude); }
            Assert.Greater(peak - low, lost * 0.4f); Assert.LessOrEqual(peak - low, lost * 0.65f, "höchstens ≈ 60 %");
        }

        [Test] public void Griffon_RhythmTravelsFaster_MasteryKeepsHeight()
        {
            System.Func<int, bool, float[]> run = (rank, rhythm) =>
            {
                var m = Air(FlightParams.Griffon(rank), 150f);
                float t = 0f, dist = 0f;
                while (m.Airborne && t < 30f) { m.Step(Dt, new FlightInput { Dive = rhythm && (t % 9f) < 3f }); dist += m.Forward * Dt; t += Dt; }
                return new[] { dist, m.Altitude };
            };
            var plain = run(0, false); var r0 = run(0, true); var r3 = run(3, true);
            Assert.Greater(r0[0], plain[0] * 1.4f, "Sturz-Rhythmus legt in 30 s deutlich mehr Strecke zurück");
            Assert.Greater(r3[1], r0[1] + 5f, "Sturzflug-Meister (85 %) hält mehr Höhe als 60 %");
        }

        [Test] public void Griffon_StallBelowThreshold()
        {
            var m = Air(FlightParams.Griffon(0), 100f); m.Forward = m.Run * 0.4f;
            m.Step(Dt, new FlightInput());
            Assert.AreEqual(FlightState.Stall, m.State);
        }

        [Test] public void Ceiling_PushesDown_Banned_Sinks()
        {
            var m = Air(FlightParams.Skyscale(0), 150f); m.Ceiling = 150f;
            for (int i = 0; i < 300; i++) m.Step(Dt, new FlightInput { JumpHeld = true });
            Assert.Less(m.Altitude, 165f);
            var b = Air(FlightParams.Skyscale(0), 50f); b.Banned = true;
            b.Step(Dt, new FlightInput { JumpHeld = true });
            Assert.AreEqual(FlightState.Sink, b.State);
        }

        [Test] public void Skyscale_EnduranceCosts()
        {
            var m = Air(FlightParams.Skyscale(0), 10f);
            for (int i = 0; i < 50; i++) m.Step(Dt, new FlightInput { JumpHeld = true });   // 1 s steigen
            Assert.AreEqual(80f, m.Endurance, 0.5f);
            float alt = m.Altitude;
            for (int i = 0; i < 50; i++) m.Step(Dt, new FlightInput());   // 1 s schweben
            Assert.AreEqual(75f, m.Endurance, 0.5f); Assert.AreEqual(alt, m.Altitude, 0.01f, "Schweben hält die Höhe");
            for (int i = 0; i < 50; i++) m.Step(Dt, new FlightInput { Pitch = 1f });
            Assert.AreEqual(75f, m.Endurance, 0.5f, "Vorwärtsflug kostet nichts");
        }

        [Test] public void Skyscale_EmptySinksGently_NoCrash()
        {
            var m = Air(FlightParams.Skyscale(0), 30f); m.Endurance = 0f;
            m.Step(Dt, new FlightInput { JumpHeld = true });
            Assert.AreEqual(FlightState.Sink, m.State); Assert.GreaterOrEqual(m.Vertical, -2f);
        }

        [Test] public void Skyscale_WallClingOnlySteep_Max20s()
        {
            var m = Air(FlightParams.Skyscale(1), 30f);
            m.Step(Dt, new FlightInput { JumpHeld = true, NearWall = true, WallSlope = 45f });
            Assert.AreNotEqual(FlightState.WallCling, m.State);
            m.Step(Dt, new FlightInput { JumpHeld = true, NearWall = true, WallSlope = 70f });
            Assert.AreEqual(FlightState.WallCling, m.State);
            for (int i = 0; i < 1100; i++) m.Step(Dt, new FlightInput { NearWall = true, WallSlope = 70f });
            Assert.AreNotEqual(FlightState.WallCling, m.State, "höchstens 20 s");
        }

        [Test] public void Skyscale_WallClimbInStages()
        {
            // 300-m-Wand: steigen bis leer, haften bis voll, weiter
            System.Func<bool, float> climb = cling =>
            {
                var m = Air(FlightParams.Skyscale(1), 0.5f); m.Ceiling = 999f;
                for (int step = 0; step < 200000 && m.Altitude < 300f; step++)
                {
                    bool needRest = m.Endurance < 1f;
                    if (cling && (needRest || m.State == FlightState.WallCling) && m.Endurance < m.P.Endurance - 1f) m.Step(Dt, new FlightInput { JumpHeld = m.State != FlightState.WallCling, NearWall = true, WallSlope = 80f });
                    else m.Step(Dt, new FlightInput { JumpHeld = true });
                    if (!m.Airborne) return m.Altitude;
                }
                return m.Altitude;
            };
            Assert.GreaterOrEqual(climb(true), 300f);
            Assert.Less(climb(false), 300f);
        }
    }
}
