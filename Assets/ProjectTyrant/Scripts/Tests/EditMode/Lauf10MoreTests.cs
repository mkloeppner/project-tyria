using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Flight;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 10: Randfälle von Flugmodell, Regeln und Auswahl.</summary>
    public class Lauf10MoreTests
    {
        const float Dt = 0.02f;
        static FlightMotor Up(FlightParams p, float alt) { var m = new FlightMotor(p); m.TakeOff(); m.Altitude = alt; m.Vertical = 0f; m.Ceiling = 999f; return m; }

        [Test] public void Griffon_FlapNeedsCharge()
        {
            var m = Up(FlightParams.Griffon(0), 50f); m.Charges = 0; m.Vertical = -2f;
            m.Step(Dt, new FlightInput { Jump = true });
            Assert.Less(m.Vertical, 0f, "ohne Ladung kein Flügelschlag");
            m.Charges = 1; m.Step(Dt, new FlightInput { Jump = true });
            Assert.Greater(m.Vertical, 3f); Assert.AreEqual(0, m.Charges);
        }

        [Test] public void Griffon_NoFlapWhileDiving()
        {
            var m = Up(FlightParams.Griffon(0), 100f); int c = m.Charges;
            m.Step(Dt, new FlightInput { Dive = true, Jump = true });
            Assert.AreEqual(c, m.Charges);
        }

        [Test] public void Griffon_PitchDownFasterThanUp()
        {
            var a = Up(FlightParams.Griffon(0), 100f); var b = Up(FlightParams.Griffon(0), 100f);
            for (int i = 0; i < 200; i++) { a.Step(Dt, new FlightInput { Pitch = 1f }); b.Step(Dt, new FlightInput { Pitch = -1f }); }
            Assert.Greater(a.Forward, b.Forward);
            Assert.LessOrEqual(a.Forward, a.Run * 1.6f * 1.25f + 0.01f);
        }

        [Test] public void Griffon_StallRecovers()
        {
            var m = Up(FlightParams.Griffon(0), 100f); m.Forward = m.Run * 0.4f;
            for (int i = 0; i < 300; i++) m.Step(Dt, new FlightInput());
            Assert.AreEqual(FlightState.Glide, m.State);
        }

        [Test] public void Griffon_LandsAtZero()
        {
            var m = Up(FlightParams.Griffon(0), 1f);
            for (int i = 0; i < 200 && m.Airborne; i++) m.Step(Dt, new FlightInput());
            Assert.AreEqual(FlightState.Ground, m.State); Assert.AreEqual(0f, m.Altitude);
        }

        [Test] public void Griffon_LandingCapsGroundSpeed()
        {
            var m = Up(FlightParams.Griffon(0), 0.5f); m.Forward = 25f;
            m.Touchdown();
            Assert.LessOrEqual(m.Forward, m.Run * m.P.GroundFactor + 0.01f);
        }

        [Test] public void Skyscale_ClimbStopsWhenEmpty()
        {
            var m = Up(FlightParams.Skyscale(0), 10f);
            for (int i = 0; i < 400; i++) m.Step(Dt, new FlightInput { JumpHeld = true });   // 8 s > 5 s Tank
            Assert.AreEqual(0f, m.Endurance, 0.01f); Assert.AreEqual(FlightState.Sink, m.State);
        }

        [Test] public void Skyscale_ClimbHeightPerTank25m()
        {
            var m = Up(FlightParams.Skyscale(0), 0.5f); float start = m.Altitude;
            while (m.Endurance > 0f) m.Step(Dt, new FlightInput { JumpHeld = true });
            Assert.AreEqual(25f, m.Altitude - start, 0.6f);
        }

        [Test] public void Skyscale_UpdraftRefills()
        {
            var m = Up(FlightParams.Skyscale(0), 30f); m.Endurance = 10f;
            m.Step(Dt, new FlightInput { InUpdraft = true });
            Assert.AreEqual(100f, m.Endurance, 0.2f);
        }

        [Test] public void Skyscale_ClingRegens15()
        {
            var m = Up(FlightParams.Skyscale(1), 30f); m.Endurance = 0f;
            m.Step(Dt, new FlightInput { JumpHeld = true, NearWall = true, WallSlope = 80f });
            for (int i = 0; i < 100; i++) m.Step(Dt, new FlightInput { NearWall = true, WallSlope = 80f });
            Assert.AreEqual(30f, m.Endurance, 0.6f); Assert.AreEqual(0f, m.Vertical);
        }

        [Test] public void Skyscale_ClingEndsWhenWallGone()
        {
            var m = Up(FlightParams.Skyscale(1), 30f);
            m.Step(Dt, new FlightInput { JumpHeld = true, NearWall = true, WallSlope = 80f });
            m.Step(Dt, new FlightInput { NearWall = false });
            Assert.AreEqual(FlightState.Hover, m.State);
        }

        [Test] public void Skyscale_NoStall()
        {
            var m = Up(FlightParams.Skyscale(0), 30f);
            for (int i = 0; i < 100; i++) m.Step(Dt, new FlightInput());
            Assert.AreNotEqual(FlightState.Stall, m.State);
        }

        [Test] public void Banned_NoTakeOff_SinksGently()
        {
            var m = new FlightMotor(FlightParams.Skyscale(0)) { Banned = true };
            m.Step(Dt, new FlightInput { Jump = true, JumpHeld = true });
            Assert.IsFalse(m.Airborne);
            var a = Up(FlightParams.Griffon(0), 40f); a.Banned = true;
            a.Step(Dt, new FlightInput { Jump = true });
            Assert.AreEqual(-4f, a.Vertical, 0.01f);
        }

        [Test] public void Ceiling_GriffonPushedDown()
        {
            var m = Up(FlightParams.Griffon(0), 160f); m.Ceiling = 150f; m.Vertical = 8f;
            m.Step(Dt, new FlightInput());
            Assert.Less(m.Vertical, 0f);
        }

        [Test] public void Rules_CityCeilingOnlyInCity()
        {
            Assert.AreEqual(80f, FlightRules.CeilingFor("sonnfurt", true));
            Assert.AreEqual(150f, FlightRules.CeilingFor("sonnfurt", false));
        }

        [Test] public void Rules_FortressReasonOnlyBeforeEnd()
        {
            var c = new FlightContext { Zone = "tyrannenfeste" };
            Assert.IsFalse(FlightRules.Evaluate(c).Allowed);
            c.StoryFinished = true; Assert.IsTrue(FlightRules.Evaluate(c).Allowed);
        }

        [Test] public void Selection_UsedIgnoresUnowned()
        {
            var m = new MountSelectionModel(); m.Grant(MountKind.Raptor);
            m.Used(MountKind.Griffon); Assert.AreEqual(MountKind.None, m.LastUsed);
            m.UseLastInsteadOfDefault = true; Assert.AreEqual(MountKind.Raptor, m.ForTap(), "Rückfall auf Standard");
        }

        [Test] public void Selection_AdminGrantMarks()
        {
            var m = new MountSelectionModel(); m.Grant(MountKind.Skyscale, true);
            CollectionAssert.Contains(m.AdminUnlocked, MountKind.Skyscale); Assert.IsTrue(m.Owns(MountKind.Skyscale));
            m.Grant(MountKind.None); Assert.IsFalse(m.Owns(MountKind.None));
        }

        [Test] public void Selection_KeyForUnownedIgnored()
        {
            var m = new MountSelectionModel(); m.KeyBindings.Add("Griffon=F6");
            Assert.AreEqual(MountKind.None, m.ForKey("F6"));
        }

        [Test] public void Collection_HatchOnlyOnce()
        {
            var c = new HashSet<string> { SkyscaleCollection.Egg };
            for (int i = 0; i < SkyscaleCollection.Stones; i++) c.Add(SkyscaleCollection.StoneId(i));
            Assert.AreEqual(SkyscaleCollection.Result.Ok, SkyscaleCollection.HatchEgg(c));
            Assert.AreEqual(SkyscaleCollection.Result.Already, SkyscaleCollection.HatchEgg(c));
            Assert.AreEqual(SkyscaleCollection.Result.WrongStep, SkyscaleCollection.PickStone(c, 0), "nach dem Schlüpfen keine Steine mehr");
        }
    }
}
