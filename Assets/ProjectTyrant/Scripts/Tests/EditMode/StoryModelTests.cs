using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 6 / M28: reine Story-Modelle (Schritte, Varianten, Ziele, Instanz, Briefe, Hinweise, Herzen, Späher, Bündel, Emotes).</summary>
    public class StoryModelTests
    {
        static StoryDatabase Db()
        {
            var db = ScriptableObject.CreateInstance<StoryDatabase>();
            db.Text = ScriptableObject.CreateInstance<StoryTextTable>();
            var c1 = ScriptableObject.CreateInstance<StoryChapter>(); c1.Number = 1; c1.MinLevel = 1;
            c1.Steps.Add(new StoryStep { Id = "K01_S01", Condition = new StoryCondition { Origin = Origin.Hof }, Objectives = { new StoryObjective { Kind = ObjectiveKind.Dodge, Count = 3 } } });
            c1.Steps.Add(new StoryStep { Id = "K01_S01p", Condition = new StoryCondition { Origin = Origin.Posten }, Objectives = { new StoryObjective { Kind = ObjectiveKind.Kill, Target = "raider", Count = 2 } } });
            c1.Steps.Add(new StoryStep { Id = "K01_S02", Objectives = { new StoryObjective { Kind = ObjectiveKind.Talk, Target = "mira" }, new StoryObjective { Kind = ObjectiveKind.Heal, Optional = true } }, SetFlags = { "met_mira" }, Letter = "brief_mira", Checkpoints = { "0" } });
            var c2 = ScriptableObject.CreateInstance<StoryChapter>(); c2.Number = 2; c2.MinLevel = 5;
            c2.Steps.Add(new StoryStep { Id = "K02_S01", Objectives = { new StoryObjective { Kind = ObjectiveKind.Revive, Target = "mira" }, new StoryObjective { Kind = ObjectiveKind.Kill, Count = 2 } }, Checkpoints = { "0" } });
            c2.Steps.Add(new StoryStep { Id = "K02_S02w", Condition = new StoryCondition { Pact = Pact.Wacht } });
            c2.Steps.Add(new StoryStep { Id = "K02_S02a", Condition = new StoryCondition { Pact = Pact.Archiv } });
            db.Chapters.Add(c1); db.Chapters.Add(c2);
            db.Text.Lines.Add(new StoryLine { Id = "L1", Text = "Grund" });
            db.Text.Lines.Add(new StoryLine { Id = "L1@Charme", Text = "Charmant" });
            db.Text.Lines.Add(new StoryLine { Id = "L1@Wacht", Text = "Wacht" });
            return db;
        }

        [Test]
        public void Variants_OriginSelectsStartStep_PactSelectsLaterStep()
        {
            var db = Db();
            var hof = StoryState.NewCharacter(Origin.Hof, Trait.Charme, Lodestar.Raetsel);
            var posten = StoryState.NewCharacter(Origin.Posten, Trait.Wuerde, Lodestar.Schuld);
            Assert.AreEqual("K01_S01", StoryModel.Current(db, hof).Id);
            Assert.AreEqual("K01_S01p", StoryModel.Current(db, posten).Id);
            Assert.AreEqual(2, StoryModel.StepsFor(db.Chapter(1), hof).Count, "fremde Herkunfts-Variante ausgeblendet");
            hof.PactE = Pact.Archiv; Assert.AreEqual("K02_S02a", StoryModel.StepsFor(db.Chapter(2), hof)[1].Id);
        }

        [Test]
        public void Objectives_Sequential_OptionalAnytime_LevelGating()
        {
            var db = Db(); var s = StoryState.NewCharacter(Origin.Hof, Trait.Charme, Lodestar.Raetsel);
            Assert.IsTrue(StoryModel.Report(db, s, ObjectiveKind.Dodge, null, 1));
            Assert.IsFalse(StoryModel.StepDone(StoryModel.Current(db, s), s));
            StoryModel.Report(db, s, ObjectiveKind.Dodge, null, 1, 5);
            Assert.IsTrue(StoryModel.StepDone(StoryModel.Current(db, s), s), "Zähler gedeckelt auf 3");
            var r = StoryModel.Complete(db, s); Assert.AreEqual("K01_S01", r.Step.Id);
            Assert.IsTrue(StoryModel.Report(db, s, ObjectiveKind.Heal, null, 1), "optionales Ziel zählt jederzeit");
            Assert.IsFalse(StoryModel.Report(db, s, ObjectiveKind.Talk, "brennar", 1), "falsches Ziel");
            Assert.IsTrue(StoryModel.Report(db, s, ObjectiveKind.Talk, "mira", 1));
            r = StoryModel.Complete(db, s);
            Assert.IsTrue(r.ChapterDone); Assert.AreEqual("brief_mira", r.Letter); Assert.IsTrue(s.HasFlag("met_mira"));
            Assert.AreEqual(2, s.Chapter); Assert.Contains(1, s.CompletedChapters);
            Assert.IsFalse(StoryModel.Available(db, s, 4), "Kapitel 2 ab Stufe 5");
            Assert.IsFalse(StoryModel.Report(db, s, ObjectiveKind.Revive, "mira", 4), "unter Mindeststufe zählt nichts");
            Assert.IsTrue(StoryModel.Report(db, s, ObjectiveKind.Revive, "mira", 5));
        }

        [Test]
        public void Instance_FailRestartsAtCheckpoint_LeaveClears()
        {
            var db = Db(); var s = StoryState.NewCharacter(Origin.Hof, Trait.Charme, Lodestar.Raetsel); s.Chapter = 2;
            var st = StoryModel.Current(db, s); var inst = new InstanceModel(); inst.Enter(st, s);
            StoryModel.Report(db, s, ObjectiveKind.Revive, "mira", 30);
            Assert.IsTrue(inst.Reach(st, s, 0)); Assert.IsFalse(inst.Reach(st, s, 0), "Kontrollpunkt nur einmal");
            StoryModel.Report(db, s, ObjectiveKind.Kill, "x", 30);
            Assert.AreEqual(1, s.Counter(st.Id, 1));
            Assert.AreEqual(1, inst.Fail(st, s), "Neustart nach Kontrollpunkt 0");
            Assert.AreEqual(1, s.Counter(st.Id, 0), "Ziel vor dem Kontrollpunkt bleibt"); Assert.AreEqual(0, s.Counter(st.Id, 1), "danach zurückgesetzt");
            inst.Paused = true; inst.Tick(5f); Assert.AreEqual(0f, inst.Timer, "Cutscene pausiert den Instanz-Timer");
            inst.Leave(); Assert.IsFalse(inst.Active);
        }

        [Test]
        public void Lines_ResolvePactThenTrait_ChoicesCountPersonalityAndSetPact()
        {
            var db = Db(); var s = StoryState.NewCharacter(Origin.Hof, Trait.Charme, Lodestar.Raetsel);
            Assert.AreEqual("Charmant", StoryModel.Resolve(db.Text, "L1", s).Text);
            s.PactE = Pact.Wacht; Assert.AreEqual("Wacht", StoryModel.Resolve(db.Text, "L1", s).Text, "Bündnis vor Charakterzug");
            s.TraitE = Trait.Wildheit; s.PactE = Pact.None; Assert.AreEqual("Grund", StoryModel.Resolve(db.Text, "L1", s).Text);
            StoryModel.Choose(s, new DialogueOption { Tendency = Trait.Wuerde, Flag = "spared" });
            StoryModel.Choose(s, new DialogueOption { Tendency = Trait.Wuerde, SetPact = Pact.Schleier });
            Assert.AreEqual(2, s.Dignity); Assert.IsTrue(s.HasFlag("spared")); Assert.AreEqual(Pact.Schleier, s.PactE); Assert.IsTrue(s.HasFlag("pact_schleier"));
            Assert.AreEqual(Trait.Wuerde, StoryModel.Dominant(s));
        }

        [Test]
        public void Mail_OncePerLetter_HintsOnce_AndSkippedForMigrated()
        {
            var s = new StoryState();
            Assert.IsTrue(MailModel.Send(s, "a")); Assert.IsFalse(MailModel.Send(s, "a"), "Brief genau einmal");
            Assert.AreEqual(1, MailModel.Unread(s)); Assert.IsTrue(MailModel.Take(s, "a")); Assert.IsFalse(MailModel.Take(s, "a")); Assert.AreEqual(0, MailModel.Unread(s));
            Assert.IsTrue(HintModel.ShouldShow(s, "dodge")); HintModel.MarkSeen(s, "dodge"); Assert.IsFalse(HintModel.ShouldShow(s, "dodge"));
            var m = StoryState.Migrated(); Assert.IsTrue(m.TutorialSkipped); Assert.AreEqual(1, m.Chapter);
            Assert.IsFalse(HintModel.ShouldShow(m, "dodge"), "Mechaniken gelten als bekannt"); Assert.IsTrue(HintModel.ShouldShow(m, "story_portal", false));
            s.HintsEnabled = false; Assert.IsFalse(HintModel.ShouldShow(s, "combo"));
        }

        [Test]
        public void Heart_NoWayAbove70Percent_CompletesWithTwoWays()
        {
            var h = new Dictionary<string, float>();
            Assert.AreEqual(0.7f, HeartModel.Add(h, "h", HeartWay.Kill, 0.9f), 1e-4f, "ein Weg allein höchstens 70 %");
            Assert.AreEqual(0.7f, HeartModel.Add(h, "h", HeartWay.Kill, 0.5f), 1e-4f);
            Assert.AreEqual(1f, HeartModel.Add(h, "h", HeartWay.Bundle, 0.5f), 1e-4f, "zweiter Weg schließt ab");
            Assert.Greater(HeartModel.Experience(15), 0);
        }

        [Test]
        public void Scout_RevealsSectorHeartsOnly_SpeaksOnce()
        {
            var hearts = new[] { new KeyValuePair<string, Vector3>("a", new Vector3(10, 0, 0)), new KeyValuePair<string, Vector3>("b", new Vector3(200, 0, 0)) };
            CollectionAssert.AreEqual(new[] { "a" }, ScoutModel.HeartsInSector(Vector3.zero, 80f, hearts));
            var s = new StoryState(); Assert.IsTrue(ScoutModel.Speak(s, "sp1")); Assert.IsFalse(ScoutModel.Speak(s, "sp1"));
        }

        [Test]
        public void Bundle_ReplacesSlots_DropOnFive_NoSwap()
        {
            var b = new BundleModel(); var s1 = ScriptableObject.CreateInstance<SkillDefinition>(); var drop = ScriptableObject.CreateInstance<SkillDefinition>();
            Assert.IsNull(b.SlotSkill(0, out bool o0)); Assert.IsFalse(o0, "ohne Bündel kein Ersatz");
            Assert.IsTrue(b.Pick("eimer", new[] { s1 }, drop));
            Assert.AreEqual(s1, b.SlotSkill(0, out bool o1)); Assert.IsTrue(o1);
            Assert.IsNull(b.SlotSkill(2, out bool o2)); Assert.IsTrue(o2, "freie Bündel-Plätze sind leer");
            Assert.AreEqual(drop, b.SlotSkill(BundleModel.DropSlot, out _));
            Assert.IsFalse(b.CanSwapWeapons); Assert.IsTrue(b.CanDodge);
            Assert.AreEqual("eimer", b.Release()); Assert.IsTrue(b.CanSwapWeapons);
            Assert.IsFalse(b.Pick("x", new SkillDefinition[4], drop), "höchstens 3 Bündel-Skills");
        }

        [Test]
        public void Emotes_ParseGermanAndEnglish()
        {
            Assert.AreEqual(Emote.Winken, EmoteModel.Parse("/winken")); Assert.AreEqual(Emote.Jubeln, EmoteModel.Parse(" /Jubeln jetzt"));
            Assert.AreEqual(Emote.Tanzen, EmoteModel.Parse("/dance")); Assert.AreEqual(Emote.None, EmoteModel.Parse("winken")); Assert.AreEqual(Emote.None, EmoteModel.Parse("/fliegen"));
            Assert.AreEqual("Emote_Verbeugen", EmoteModel.Clip(Emote.Verbeugen)); Assert.IsTrue(EmoteModel.Loops(Emote.Sitzen));
        }
    }
}
