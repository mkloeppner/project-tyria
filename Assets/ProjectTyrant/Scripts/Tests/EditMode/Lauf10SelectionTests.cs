using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Flight;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 10: Reittier-Auswahl (Account), Flugregeln, Beherrschungen Greif/Himmelsschuppe, Speichern.</summary>
    public class Lauf10SelectionTests
    {
        static MountSelectionModel Sel(params MountKind[] owned) { var m = new MountSelectionModel(); foreach (var k in owned) m.Grant(k); return m; }

        [Test] public void Selection_FirstGrantBecomesDefault_TapUsesDefault()
        {
            var m = Sel(MountKind.Raptor, MountKind.Griffon);
            Assert.AreEqual(MountKind.Raptor, m.Default);
            Assert.AreEqual(MountKind.Raptor, m.ForTap());
            Assert.IsTrue(m.SetDefault(MountKind.Griffon)); Assert.AreEqual(MountKind.Griffon, m.ForTap());
            Assert.IsFalse(m.SetDefault(MountKind.Skyscale), "nicht besessen");
        }

        [Test] public void Selection_NothingOwned_TapGivesNone()
        {
            Assert.AreEqual(MountKind.None, new MountSelectionModel().ForTap());
        }

        [Test] public void Selection_LastUsedMode()
        {
            var m = Sel(MountKind.Raptor, MountKind.Skimmer);
            m.Used(MountKind.Skimmer);
            Assert.AreEqual(MountKind.Raptor, m.ForTap(), "Standard-Modus");
            m.UseLastInsteadOfDefault = true;
            Assert.AreEqual(MountKind.Skimmer, m.ForTap(), "Zuletzt-Modus");
        }

        [Test] public void Selection_RadialSetsDefaultOnlyWhenEnabled()
        {
            var m = Sel(MountKind.Raptor, MountKind.Springer);
            Assert.AreEqual(MountKind.Springer, m.RadialPick(MountKind.Springer)); Assert.AreEqual(MountKind.Springer, m.Default);
            m.RadialSetsDefault = false;
            Assert.AreEqual(MountKind.Raptor, m.RadialPick(MountKind.Raptor)); Assert.AreEqual(MountKind.Springer, m.Default);
            Assert.AreEqual(MountKind.None, m.RadialPick(MountKind.Skyscale));
        }

        [Test] public void Selection_RadialHoldIsAtLeast250ms()
        {
            Assert.GreaterOrEqual(MountSelectionModel.RadialHoldSeconds, 0.25f);
            Assert.GreaterOrEqual(MountMotor.TapThreshold, MountSelectionModel.RadialHoldSeconds);
        }

        [Test] public void Selection_KeyBindings_ReservedAndConflicts()
        {
            var m = Sel(MountKind.Raptor, MountKind.Griffon);
            Assert.IsFalse(m.Bind(MountKind.Raptor, "X", out var e1)); StringAssert.Contains("belegt", e1);
            Assert.IsTrue(m.Bind(MountKind.Raptor, "F6", out _));
            Assert.IsFalse(m.Bind(MountKind.Griffon, "F6", out var e2)); StringAssert.Contains("Raptor", e2);
            Assert.AreEqual(MountKind.Raptor, m.ForKey("F6"));
            Assert.IsTrue(m.Bind(MountKind.Raptor, "F7", out _)); Assert.AreEqual(MountKind.None, m.ForKey("F6"), "Neubelegung ersetzt");
            Assert.IsTrue(m.Bind(MountKind.Raptor, null, out _)); Assert.AreEqual(MountKind.None, m.ForKey("F7"));
        }

        [Test] public void Selection_AirSwitchOnlyFlyersWithTrustBond()
        {
            Assert.IsTrue(MountSelectionModel.CanSwitch(MountKind.Raptor, MountKind.Springer, false, false));
            Assert.IsFalse(MountSelectionModel.CanSwitch(MountKind.Griffon, MountKind.Skyscale, true, false));
            Assert.IsTrue(MountSelectionModel.CanSwitch(MountKind.Griffon, MountKind.Skyscale, true, true));
            Assert.IsFalse(MountSelectionModel.CanSwitch(MountKind.Griffon, MountKind.Raptor, true, true));
            Assert.IsFalse(MountSelectionModel.CanSwitch(MountKind.Raptor, MountKind.Raptor, false, false));
        }

        [Test] public void Selection_PrefsRoundTripThroughProfileAndSave()
        {
            var st = new ProfileState();
            st.Mounts.Add("Raptor"); st.Mounts.Add("Skyscale");
            var m = st.MountSelection();
            m.SetDefault(MountKind.Skyscale); m.Used(MountKind.Raptor); m.UseLastInsteadOfDefault = true; m.RadialSetsDefault = false; m.Bind(MountKind.Skyscale, "N", out _);
            st.StoreMountSelection(m); st.AdminMark("mount:Skyscale");
            var dto = JsonUtility.FromJson<SaveGameDto>(JsonUtility.ToJson(st.ToDto()));
            var st2 = new ProfileState(); st2.LoadDto(dto, null, new List<SkillDefinition>());
            var m2 = st2.MountSelection();
            Assert.AreEqual(MountKind.Skyscale, m2.Default); Assert.AreEqual(MountKind.Raptor, m2.LastUsed);
            Assert.IsTrue(m2.UseLastInsteadOfDefault); Assert.IsFalse(m2.RadialSetsDefault);
            Assert.AreEqual(MountKind.Skyscale, m2.ForKey("N"));
            Assert.IsTrue(st2.IsAdminMarked("mount:Skyscale")); CollectionAssert.Contains(m2.AdminUnlocked, MountKind.Skyscale);
        }

        [Test] public void Selection_AccountCarriesPrefs()
        {
            var d = new SaveGameDto(); d.MountPrefs.UseLast = true; d.MountPrefs.Keys.Add("Raptor=F6"); d.AdminMarks.Add("mount:Raptor");
            var acc = new AccountDto(); AccountModel.ExtractAccount(d, acc);
            var d2 = new SaveGameDto(); AccountModel.ApplyAccount(acc, d2);
            Assert.IsTrue(d2.MountPrefs.UseLast); CollectionAssert.Contains(d2.MountPrefs.Keys, "Raptor=F6"); CollectionAssert.Contains(d2.AdminMarks, "mount:Raptor");
            d.MountPrefs.Keys.Clear(); CollectionAssert.Contains(d2.MountPrefs.Keys, "Raptor=F6", "Kopie, keine geteilte Liste");
        }

        [Test] public void Selection_OldSaveWithoutPrefs_Defaults()
        {
            var dto = JsonUtility.FromJson<SaveGameDto>("{\"Mounts\":[\"Raptor\"],\"SelectedMount\":\"Raptor\"}");
            var st = new ProfileState(); st.LoadDto(dto, null, new List<SkillDefinition>());
            var m = st.MountSelection();
            Assert.AreEqual(MountKind.Raptor, m.ForTap()); Assert.IsTrue(m.RadialSetsDefault); Assert.IsFalse(m.UseLastInsteadOfDefault);
        }

        // ---------------- Flugregeln ----------------
        static FlightContext Open(string zone) => new FlightContext { Zone = zone, StoryFinished = true };

        [Test] public void Rules_Ceilings()
        {
            Assert.AreEqual(150f, FlightRules.Evaluate(Open("nebelmoor")).Ceiling);
            Assert.AreEqual(400f, FlightRules.Evaluate(Open("himmelsspitzen")).Ceiling);
            var city = Open("himmelsspitzen"); city.InCity = true;
            Assert.AreEqual(80f, FlightRules.Evaluate(city).Ceiling, "Stadt schlägt Zone");
            Assert.Greater(FlightRules.Evaluate(Open("nebelmoor"), false, true).Ceiling, 10000f, "/flight ceiling off");
        }

        [Test] public void Rules_Bans()
        {
            Assert.IsTrue(FlightRules.Evaluate(Open("nebelmoor")).Allowed);
            var c = Open("nebelmoor"); c.InStoryInstance = true; Assert.IsFalse(FlightRules.Evaluate(c).Allowed);
            c = Open("nebelmoor"); c.InCutscene = true; Assert.IsFalse(FlightRules.Evaluate(c).Allowed);
            c = Open("nebelmoor"); c.UnderRoof = true; Assert.IsFalse(FlightRules.Evaluate(c).Allowed);
            c = Open("nebelmoor"); c.NearBossFight = true; Assert.IsFalse(FlightRules.Evaluate(c).Allowed);
            c = Open("tyrannenfeste"); c.StoryFinished = false; var v = FlightRules.Evaluate(c);
            Assert.IsFalse(v.Allowed); StringAssert.Contains("Tyrannenfeste", v.Reason);
            Assert.IsTrue(FlightRules.Evaluate(Open("tyrannenfeste")).Allowed, "nach der Geschichte frei");
            c.InStoryInstance = true; Assert.IsTrue(FlightRules.Evaluate(c, true).Allowed, "/flight rules off");
        }

        [Test] public void Rules_ReasonPriority_CutsceneFirst()
        {
            var c = Open("tyrannenfeste"); c.StoryFinished = false; c.InCutscene = true; c.UnderRoof = true;
            StringAssert.Contains("Zwischensequenz", FlightRules.Evaluate(c).Reason);
        }

        [Test] public void Rules_FlyersOnly()
        {
            Assert.IsTrue(FlightRules.IsFlyer(MountKind.Griffon)); Assert.IsTrue(FlightRules.IsFlyer(MountKind.Skyscale));
            Assert.IsFalse(FlightRules.IsFlyer(MountKind.Raptor)); Assert.IsFalse(FlightRules.IsFlyer(MountKind.None));
        }

        // ---------------- Beherrschungen ----------------
        static MasteryModel Ranked(MasteryLine l, int rank)
        {
            var m = new MasteryModel();
            var costs = new MasteryRankCost[4]; for (int i = 0; i < 4; i++) costs[i] = new MasteryRankCost { Xp = 10, Points = 1 };
            m.SetCosts(l, costs); m.AddPoints(10); m.Selected = l;
            for (int i = 0; i < rank; i++) m.AddXp(10);
            return m;
        }

        [Test] public void Mastery_GriffonRanks()
        {
            Assert.AreEqual(0, MasteryEffects.GriffonRank(null));
            Assert.AreEqual(3, FlightParams.Griffon(MasteryEffects.GriffonRank(Ranked(MasteryLine.Greif, 0))).Charges);
            Assert.AreEqual(5, FlightParams.Griffon(MasteryEffects.GriffonRank(Ranked(MasteryLine.Greif, 1))).Charges, "Flügelkraft");
            Assert.IsFalse(MasteryEffects.GriffonUpdrafts(Ranked(MasteryLine.Greif, 1))); Assert.IsTrue(MasteryEffects.GriffonUpdrafts(Ranked(MasteryLine.Greif, 2)));
            Assert.AreEqual(0.6f, FlightParams.Griffon(2).PullUpEfficiency, 1e-4f); Assert.AreEqual(0.85f, FlightParams.Griffon(3).PullUpEfficiency, 1e-4f);
            Assert.AreEqual(0f, FlightParams.Griffon(3).AirChargeRegen); Assert.Greater(FlightParams.Griffon(4).AirChargeRegen, 0f, "Luftakrobatik");
        }

        [Test] public void Mastery_SkyscaleRanks()
        {
            Assert.IsFalse(MasteryEffects.SkyscaleWallCling(Ranked(MasteryLine.Himmelsschuppe, 0)));
            Assert.IsTrue(MasteryEffects.SkyscaleWallCling(Ranked(MasteryLine.Himmelsschuppe, 1)));
            Assert.AreEqual(100f, FlightParams.Skyscale(1).Endurance); Assert.AreEqual(150f, FlightParams.Skyscale(2).Endurance, "Flugkondition");
            Assert.AreEqual(5f, FlightParams.Skyscale(1).HoverCost); Assert.AreEqual(3f, FlightParams.Skyscale(2).HoverCost);
            Assert.IsTrue(MasteryEffects.SkyscaleUpdrafts(Ranked(MasteryLine.Himmelsschuppe, 3)));
            Assert.IsTrue(MasteryEffects.TrustBond(Ranked(MasteryLine.Himmelsschuppe, 4))); Assert.IsTrue(FlightParams.Skyscale(4).AirRegen);
        }

        [Test] public void Mastery_LineNames()
        {
            Assert.AreEqual("Himmelsschuppe", MasteryModel.LineName(MasteryLine.Himmelsschuppe));
            Assert.AreEqual(MountKind.Skyscale, MountStats.Default(MountKind.Skyscale).Kind);
            Assert.Less(MountStats.Default(MountKind.Skyscale).Speed, MountStats.Default(MountKind.Raptor).Speed, "langsam am Boden");
        }

        // ---------------- Flugmodell: weitere Fälle ----------------
        [Test] public void Griffon_UpdraftRefillsCharges()
        {
            var m = new FlightMotor(FlightParams.Griffon(0)); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 50f;
            m.Charges = 0; m.Step(0.02f, new FlightInput { InUpdraft = true });
            Assert.AreEqual(3, m.Charges); Assert.Greater(m.Vertical, 0f);
        }

        [Test] public void Griffon_AirChargeRegenWithAcrobatics()
        {
            var m = new FlightMotor(FlightParams.Griffon(4)); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 300f; m.Charges = 0;
            for (int i = 0; i < 250; i++) m.Step(0.02f, new FlightInput());   // 5 s Gleiten
            Assert.AreEqual(1, m.Charges);
        }

        [Test] public void Griffon_DiveEngageFactor()
        {
            var m = new FlightMotor(FlightParams.Griffon(0)); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 300f;
            Assert.AreEqual(0f, m.EngageFactor);
            for (int i = 0; i < 400; i++) m.Step(0.02f, new FlightInput { Dive = true });
            Assert.Greater(m.EngageFactor, 0.9f);
        }

        [Test] public void Skyscale_GroundRegen25PerSecond()
        {
            var m = new FlightMotor(FlightParams.Skyscale(0)); m.Endurance = 0f;
            for (int i = 0; i < 100; i++) m.Step(0.02f, new FlightInput());
            Assert.AreEqual(50f, m.Endurance, 0.5f);
        }

        [Test] public void Skyscale_ClimbsVertically_DiveFast()
        {
            var m = new FlightMotor(FlightParams.Skyscale(0)); m.Step(0.02f, new FlightInput { Jump = true, JumpHeld = true });
            Assert.AreEqual(FlightState.Climb, m.State); Assert.AreEqual(0f, m.Forward, 0.01f, "senkrechter Start");
            m.Altitude = 80f; m.Step(0.02f, new FlightInput { Dive = true });
            Assert.AreEqual(-m.Run * 3f, m.Vertical, 0.01f);
        }

        [Test] public void Skyscale_TrustBondRegensInAir()
        {
            var m = new FlightMotor(FlightParams.Skyscale(4)); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 50f; m.Endurance = 50f;
            for (int i = 0; i < 50; i++) m.Step(0.02f, new FlightInput { Pitch = 1f });
            Assert.AreEqual(55f, m.Endurance, 0.5f);
        }

        [Test] public void Flight_TouchdownAndLeaveGround()
        {
            var g = new FlightMotor(FlightParams.Griffon(0));
            g.LeaveGround(); Assert.AreEqual(FlightState.Glide, g.State);
            g.Touchdown(); Assert.AreEqual(FlightState.Ground, g.State);
            var s = new FlightMotor(FlightParams.Skyscale(0)); s.LeaveGround(); Assert.AreEqual(FlightState.Hover, s.State);
            var b = new FlightMotor(FlightParams.Griffon(0)) { Banned = true }; b.LeaveGround(); b.TakeOff(); Assert.IsFalse(b.Airborne, "Verbot: kein Start");
        }

        [Test] public void Flight_Deterministic()
        {
            System.Func<float> run = () => { var m = new FlightMotor(FlightParams.Griffon(2)); m.Step(0.02f, new FlightInput { Jump = true }); m.Altitude = 120f;
                for (int i = 0; i < 2000; i++) m.Step(0.02f, new FlightInput { Dive = i % 300 < 90, Pitch = (i % 50) / 50f - 0.5f }); return m.Altitude * 1000f + m.Forward; };
            Assert.AreEqual(run(), run());
        }
    }
}
