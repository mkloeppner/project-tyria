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

        // Nach Lauf 11 (Spieler-Wunsch) neu gefasst: senkrechter Sturz, kein automatisches Hochziehen, S wandelt Sturz-Energie in Höhe,
        // Flügelschläge im Sturz heben das Höchsttempo auf 4× / 5×. Ersetzt Griffon_DiveCapsSpeed / _PullUpRecoversHeight_WithLoss / _RhythmTravelsFaster_MasteryKeepsHeight.
        [Test] public void Griffon_DiveAt80Degrees_CapsAt3xRun()
        {
            var m = Air(FlightParams.Griffon(0), 400f); m.Ceiling = 999f;
            for (int i = 0; i < 300 && m.Airborne; i++) m.Step(Dt, new FlightInput { Dive = true });   // 6 s
            Assert.AreEqual(FlightState.Dive, m.State);
            Assert.AreEqual(m.Run * 3f, m.DiveSpeed, 0.01f, "ohne Flügelschlag 3 × Lauf");
            float angle = UnityEngine.Mathf.Atan2(-m.Vertical, m.Forward) * UnityEngine.Mathf.Rad2Deg;
            Assert.AreEqual(80f, angle, 0.1f, "steil, aber nicht senkrecht");
            Assert.AreEqual(m.DiveSpeed, UnityEngine.Mathf.Sqrt(m.Forward * m.Forward + m.Vertical * m.Vertical), 0.01f);
        }

        [Test] public void Griffon_DiveFlaps_Reach4xAnd5x_OncePerSecond()
        {
            var m = Air(FlightParams.Griffon(0), 900f); m.Ceiling = 9999f; int charges = m.Charges;
            for (int i = 0; i < 25; i++) m.Step(Dt, new FlightInput { Dive = true });                 // 0,5 s
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            Assert.AreEqual(0, m.DiveFlaps, "erst nach 1 s Sturz");
            for (int i = 0; i < 30; i++) m.Step(Dt, new FlightInput { Dive = true });
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            Assert.AreEqual(1, m.DiveFlaps);
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            Assert.AreEqual(1, m.DiveFlaps, "höchstens ein Schlag je Sekunde");
            for (int i = 0; i < 400; i++) m.Step(Dt, new FlightInput { Dive = true });
            Assert.AreEqual(m.Run * 4f, m.DiveSpeed, 0.01f, "ein Schlag: 4 × Lauf");
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            for (int i = 0; i < 400; i++) m.Step(Dt, new FlightInput { Dive = true });
            Assert.AreEqual(m.Run * 5f, m.DiveSpeed, 0.01f, "zwei Schläge: 5 × Lauf");
            for (int i = 0; i < 60; i++) m.Step(Dt, new FlightInput { Dive = true, Jump = i % 55 == 0 });
            Assert.AreEqual(2, m.DiveFlaps); Assert.AreEqual(m.Run * 5f, m.DiveSpeed, 0.01f, "nicht über 5 ×");
            Assert.AreEqual(charges - 2, m.Charges, "je Flügelschlag eine Ladung (im Spiel: 1 Ausweich-Balken)");
        }

        [Test] public void Griffon_DiveFlap_NeedsCharge()
        {
            var m = Air(FlightParams.Griffon(0), 900f); m.Ceiling = 9999f; m.Charges = 0;
            for (int i = 0; i < 60; i++) m.Step(Dt, new FlightInput { Dive = true });
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            Assert.AreEqual(0, m.DiveFlaps, "ohne Balken kein Flügelschlag");
        }

        [Test] public void Griffon_ReleaseKeepsSpeed_NoAutoLift()
        {
            var m = Air(FlightParams.Griffon(0), 300f); m.Ceiling = 999f;
            for (int i = 0; i < 150; i++) m.Step(Dt, new FlightInput { Dive = true });   // 3 s
            float speed = m.DiveSpeed, low = m.Altitude;
            m.Step(Dt, new FlightInput());
            Assert.AreEqual(FlightState.Glide, m.State);
            Assert.AreEqual(speed, m.Forward, 0.5f, "Sturz-Tempo bleibt erhalten");
            float peak = m.Altitude;
            for (int i = 0; i < 100; i++) { m.Step(Dt, new FlightInput()); peak = System.Math.Max(peak, m.Altitude); }
            Assert.LessOrEqual(peak, low + 0.01f, "kein automatisches Hochziehen");
            Assert.Greater(m.Forward, speed * 0.85f, "Tempo hält sich (nur langsamer Verlust)");
            Assert.Less(m.Altitude, low - 0.5f, "verliert leicht an Höhe"); Assert.Greater(m.Altitude, low - 3f, "nur leicht (≈ 1 m/s)");
        }

        /// <summary>Sturz bis zur Stufe (0/1/2 Flügelschläge = 3×/4×/5×), loslassen, S halten bis die Energie verbraucht ist → Endhöhe, Ausgangshöhe.</summary>
        static float[] DiveAndPull(int flaps)
        {
            var m = Air(FlightParams.Griffon(0), 600f); m.Ceiling = 9999f; float start = m.Altitude;
            for (int i = 0; i < 60; i++) m.Step(Dt, new FlightInput { Dive = true });
            for (int k = 0; k < flaps; k++) { m.Step(Dt, new FlightInput { Dive = true, Jump = true }); for (int i = 0; i < 55; i++) m.Step(Dt, new FlightInput { Dive = true }); }
            for (int i = 0; i < 200; i++) m.Step(Dt, new FlightInput { Dive = true });
            float times = m.DiveSpeed / m.Run, low = m.Altitude, energy = m.Energy;
            for (int i = 0; i < 3000 && (m.Energy > 0f || i == 0); i++) m.Step(Dt, new FlightInput { Pitch = -1f });
            return new[] { m.Altitude, start, times, start - low, energy, m.Forward };
        }

        [Test] public void Griffon_SConvertsEnergy_3xBelow_4xExact_5xAbove()
        {
            var r3 = DiveAndPull(0); var r4 = DiveAndPull(1); var r5 = DiveAndPull(2);
            Assert.AreEqual(3f, r3[2], 0.01f); Assert.AreEqual(4f, r4[2], 0.01f); Assert.AreEqual(5f, r5[2], 0.01f);
            Assert.AreEqual(r3[3], r3[4], 0.5f, "Energie = gestürzte Höhe");
            Assert.Less(r3[0], r3[1] - 2f, "3 ×: knapp unter der Ausgangshöhe"); Assert.Greater(r3[0], r3[1] - r3[3] * 0.15f);
            Assert.AreEqual(r4[1], r4[0], 1f, "4 ×: genau zurück auf die Ausgangshöhe");
            Assert.Greater(r5[0], r5[1] + 2f, "5 ×: etwas über der Ausgangshöhe"); Assert.Less(r5[0], r5[1] + r5[3] * 0.15f);
            Assert.AreEqual(new FlightMotor(FlightParams.Griffon(0)).Run * 1.6f, r4[5], 0.5f, "Energie verbraucht → Gleittempo");
        }

        [Test] public void Griffon_WaitingBeforeS_CostsHeight_NeverAbovePeak()
        {
            float now = DiveWaitPull(0f, out float peakNow, out float startNow), late = DiveWaitPull(3f, out float peakLate, out float startLate);
            Assert.AreEqual(peakNow, now, 1f, "sofort S: bis zur gemerkten Höchstmarke");
            Assert.Less(late, now - 3f, "warten kostet Höhe (Sinken + Tempoverlust)");
            Assert.LessOrEqual(late, peakLate + 0.01f, "nie über die Höchstmarke");
        }

        static float DiveWaitPull(float wait, out float peak, out float start)
        {
            var m = Air(FlightParams.Griffon(0), 400f); m.Ceiling = 9999f; start = m.Altitude;
            for (int i = 0; i < 200; i++) m.Step(Dt, new FlightInput { Dive = true });
            m.Step(Dt, new FlightInput()); peak = m.PeakAltitude;
            for (float t = 0f; t < wait; t += Dt) m.Step(Dt, new FlightInput());
            for (int i = 0; i < 3000 && m.Energy > 0f; i++) m.Step(Dt, new FlightInput { Pitch = -1f });
            return m.Altitude;
        }

        [Test] public void Griffon_SPullUpIs50Degrees()
        {
            var m = Air(FlightParams.Griffon(0), 300f); m.Ceiling = 999f;
            for (int i = 0; i < 150; i++) m.Step(Dt, new FlightInput { Dive = true });
            for (int i = 0; i < 40; i++) m.Step(Dt, new FlightInput { Pitch = -1f });   // Steigen eingeschwungen
            float angle = UnityEngine.Mathf.Atan2(m.Vertical, m.Forward) * UnityEngine.Mathf.Rad2Deg;
            Assert.AreEqual(50f, angle, 1.5f, "steiler Aufschwung");
            Assert.AreEqual(m.EnergySpeed, UnityEngine.Mathf.Sqrt(m.Forward * m.Forward + m.Vertical * m.Vertical), 0.05f, "Tempo verteilt sich, bleibt erhalten");
        }

        [Test] public void Griffon_DiveAndConvert_TravelsFarther_MasteryKeepsEnergy()
        {
            System.Func<int, bool, float> run = (rank, diveConvert) =>
            {
                var m = Air(FlightParams.Griffon(rank), 150f); m.Ceiling = 999f;
                float t = 0f, dist = 0f;
                while (m.Airborne && t < 30f)
                {
                    float c = t % 10f;
                    var i = !diveConvert ? new FlightInput() : c < 2f ? new FlightInput { Dive = true } : new FlightInput { Pitch = c > 8f ? -1f : 0f };
                    m.Step(Dt, i); dist += m.Speed * Dt * (m.State == FlightState.Dive ? 0f : 1f); t += Dt;
                }
                return dist;
            };
            Assert.Greater(run(0, true), run(0, false) * 1.2f, "Sturz + Energie-Flug legt mehr Strecke zurück als reines Gleiten");
            var a = Air(FlightParams.Griffon(0), 300f); var b = Air(FlightParams.Griffon(3), 300f); a.Ceiling = b.Ceiling = 999f;
            for (int i = 0; i < 100; i++) { a.Step(Dt, new FlightInput { Dive = true }); b.Step(Dt, new FlightInput { Dive = true }); }
            float ea0 = 0f, eb0 = 0f;
            a.Step(Dt, new FlightInput()); b.Step(Dt, new FlightInput()); ea0 = a.Energy; eb0 = b.Energy;
            for (int i = 0; i < 500; i++) { a.Step(Dt, new FlightInput()); b.Step(Dt, new FlightInput()); }
            Assert.Greater(b.Energy / eb0, a.Energy / ea0 + 0.1f, "Sturzflug-Meister verliert die Energie langsamer");
            Assert.Greater(eb0, ea0, "Sturzflug-Meister beschleunigt schneller");
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
