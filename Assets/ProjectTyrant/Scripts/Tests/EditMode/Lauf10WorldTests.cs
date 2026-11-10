using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Flight;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 10: Himmelsschuppen-Sammlung, Zwei-Wege-Erreichbarkeit, Story-Admin-Schalter.</summary>
    public class Lauf10WorldTests
    {
        [Test] public void Collection_StepsInOrder()
        {
            var c = new HashSet<string>();
            Assert.AreEqual(0, SkyscaleCollection.Step(c));
            Assert.AreEqual(SkyscaleCollection.Result.WrongStep, SkyscaleCollection.PickStone(c, 0), "Steine erst nach dem Ei");
            Assert.AreEqual(SkyscaleCollection.Result.WrongStep, SkyscaleCollection.HatchEgg(c));
            Assert.AreEqual(SkyscaleCollection.Result.Ok, SkyscaleCollection.TakeEgg(c));
            Assert.AreEqual(SkyscaleCollection.Result.Already, SkyscaleCollection.TakeEgg(c));
            Assert.AreEqual(1, SkyscaleCollection.Step(c));
            Assert.AreEqual(SkyscaleCollection.Result.NeedStones, SkyscaleCollection.HatchEgg(c));
            for (int i = 0; i < SkyscaleCollection.Stones; i++) Assert.AreEqual(SkyscaleCollection.Result.Ok, SkyscaleCollection.PickStone(c, i));
            Assert.AreEqual(SkyscaleCollection.Result.Already, SkyscaleCollection.PickStone(c, 0));
            Assert.AreEqual(SkyscaleCollection.Result.Ok, SkyscaleCollection.HatchEgg(c));
            Assert.AreEqual(2, SkyscaleCollection.Step(c));
        }

        [Test] public void Collection_SaddleCostsGold()
        {
            var c = new HashSet<string> { SkyscaleCollection.Egg, SkyscaleCollection.Hatch };
            var poor = new WalletModel { Copper = SkyscaleCollection.SaddleCopper - 1 };
            Assert.AreEqual(SkyscaleCollection.Result.NeedGold, SkyscaleCollection.BuySaddle(c, poor));
            Assert.AreEqual(SkyscaleCollection.SaddleCopper - 1, poor.Copper, "nichts abgebucht");
            var rich = new WalletModel { Copper = SkyscaleCollection.SaddleCopper + 5 };
            Assert.AreEqual(SkyscaleCollection.Result.Ok, SkyscaleCollection.BuySaddle(c, rich));
            Assert.AreEqual(5, rich.Copper);
            Assert.AreEqual(3, SkyscaleCollection.Step(c));
            Assert.AreEqual(SkyscaleCollection.Result.Already, SkyscaleCollection.BuySaddle(c, rich));
        }

        [Test] public void Collection_StepTextsGerman()
        {
            var c = new HashSet<string>();
            StringAssert.Contains("1/3", SkyscaleCollection.StepText(c));
            c.Add(SkyscaleCollection.Egg); c.Add(SkyscaleCollection.StoneId(2));
            StringAssert.Contains("1/5", SkyscaleCollection.StepText(c));
        }

        [Test] public void Reach_GriffonNeedsUpdraftOrHeight()
        {
            var p = FlightParams.Griffon(0);
            Assert.IsFalse(FlightReach.GriffonReaches(100f, 0f, 150f, 60f, p), "ohne Aufwind nicht nach oben");
            Assert.IsTrue(FlightReach.GriffonReaches(100f, 170f, 150f, 60f, p));
            Assert.IsFalse(FlightReach.GriffonReaches(100f, 155f, 150f, 60f, p), "60 m Gleiten kosten 10 m");
        }

        [Test] public void Reach_SkyscaleTankAndRests()
        {
            Assert.AreEqual(25f, FlightReach.SkyscaleClimbPerTank(FlightParams.Skyscale(0)), 0.01f);
            Assert.AreEqual(37.5f, FlightReach.SkyscaleClimbPerTank(FlightParams.Skyscale(2)), 0.01f, "Flugkondition");
            Assert.IsFalse(FlightReach.SkyscaleReaches(0f, 60f, 1, FlightParams.Skyscale(0)));
            Assert.IsTrue(FlightReach.SkyscaleReaches(0f, 60f, 2, FlightParams.Skyscale(0)));
            Assert.IsTrue(FlightReach.BothPaths(100f, 200f, 150f, 60f, 2, 0, 0));
            Assert.IsFalse(FlightReach.BothPaths(100f, 120f, 150f, 60f, 2, 0, 0));
        }

        [Test] public void StoryAdmin_DefaultsAndInactive()
        {
            Assert.IsFalse(StoryAdmin.Active, "nur während eines Admin-Ablaufs aktiv");
            Assert.IsTrue(StoryAdmin.PlayScenes, "Standard: echte Szenen abspielen");
        }

        [Test] public void FlightDebug_Reset()
        {
            FlightDebug.Infinite = true; FlightDebug.RulesOff = true; FlightDebug.CeilingOff = true; FlightDebug.Show = true;
            FlightDebug.Reset();
            Assert.IsFalse(FlightDebug.Infinite || FlightDebug.RulesOff || FlightDebug.CeilingOff || FlightDebug.Show);
        }

        [Test] public void AdminFlightCommand_Toggles()
        {
            FlightDebug.Reset();
            StringAssert.Contains("unendlich an", AdminCommands.Flight("/flight infinite"));
            StringAssert.Contains("Regeln aus an", AdminCommands.Flight("rules off"));
            StringAssert.Contains("Unbekannt", AdminCommands.Flight("fly"));
            AdminCommands.Flight("reset");
            Assert.IsFalse(FlightDebug.Infinite);
        }
    }
}
