using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 6 / M28: datengetriebene Prüfung der gebauten Story-Datenbank (Zeilen, Szenen, Hinweise, Briefe, Stimmen, Bündel) und der Stimm-Nachbearbeitung.</summary>
    public class StoryDataTests
    {
        static StoryDatabase Db { get { var db = StoryBuilder.Load(); Assert.IsNotNull(db, "Story-Datenbank fehlt – Rebuild Story ausführen"); return db; } }

        static IEnumerable<string> SceneLineIds(DialogueScript sc)
        {
            foreach (var e in sc.Entries) yield return e.Line;
            foreach (var c in sc.Choices) { if (!string.IsNullOrEmpty(c.Prompt)) yield return c.Prompt; foreach (var o in c.Options) { yield return o.Line; if (!string.IsNullOrEmpty(o.Reply)) yield return o.Reply; } }
        }

        [Test]
        public void EverySceneLine_ExistsInTextTable()
        {
            var db = Db; var missing = new List<string>();
            foreach (var sc in db.Scenes) foreach (var id in SceneLineIds(sc)) if (db.Text.Get(id) == null) missing.Add(sc.Id + "→" + id);
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.Greater(db.Scenes.Count, 10);
        }

        [Test]
        public void EveryChoiceAndReplyLine_IsReachableFromAScene()
        {
            var db = Db; var used = new HashSet<string>();
            foreach (var sc in db.Scenes) foreach (var id in SceneLineIds(sc)) used.Add(id);
            var orphan = new List<string>(); int choices = 0;
            foreach (var l in db.Text.Lines)
            {
                if (l.Id.Contains("@")) continue;
                bool opt = System.Text.RegularExpressions.Regex.IsMatch(l.Id, "_C\\d+_[A-Z]$"), rep = System.Text.RegularExpressions.Regex.IsMatch(l.Id, "_R\\d+$");
                if (opt) choices++;
                if ((opt || rep) && !used.Contains(l.Id)) orphan.Add(l.Id);
            }
            Assert.IsEmpty(orphan, string.Join(", ", orphan));
            Assert.Greater(choices, 0, "Story enthält Wahl-Stellen");
        }

        [Test]
        public void EveryStepScene_Hint_And_Letter_Exists()
        {
            var db = Db; var missing = new List<string>();
            foreach (var ch in db.Chapters) foreach (var st in ch.Steps)
            {
                foreach (var s in new[] { st.SceneStart, st.SceneEnd }) if (!string.IsNullOrEmpty(s) && db.Scene(s) == null) missing.Add(st.Id + " Szene " + s);
                if (!string.IsNullOrEmpty(st.Letter) && db.Letter(st.Letter) == null) missing.Add(st.Id + " Brief " + st.Letter);
                foreach (var o in st.Objectives)
                {
                    if (!string.IsNullOrEmpty(o.Scene) && db.Scene(o.Scene) == null) missing.Add(st.Id + " Ziel-Szene " + o.Scene);
                    if (!string.IsNullOrEmpty(o.Hint) && db.Hint(o.Hint) == null) missing.Add(st.Id + " Hinweis " + o.Hint);
                }
            }
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [Test]
        public void EveryInstanceFoe_AndTrainingPartner_HasArchetype()
        {
            var db = Db; var missing = new List<string>();
            foreach (var ch in db.Chapters) foreach (var st in ch.Steps) foreach (var o in st.Objectives)
            {
                bool foe = o.Kind == ObjectiveKind.Kill || o.Kind == ObjectiveKind.Boss || o.Kind == ObjectiveKind.Defiance;
                if (foe && st.Type == StepType.Instance && db.Archetype(o.Target) == null) missing.Add(st.Id + " " + o.Target);
                if (!string.IsNullOrEmpty(o.Partner) && db.Archetype(o.Partner) == null) missing.Add(st.Id + " Partner " + o.Partner);
            }
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [Test]
        public void EveryLine_AtMost25Words_KnownSpeaker_NoForbiddenNames()
        {
            var db = Db; var bad = new List<string>();
            foreach (var l in db.Text.Lines)
            {
                if (l.Words > 25) bad.Add(l.Id + " " + l.Words + " Wörter");
                if (db.Cast.Get(l.Speaker) == null) bad.Add(l.Id + " Sprecher " + l.Speaker);
                foreach (var f in StoryBuilder.ForbiddenNames) if (System.Text.RegularExpressions.Regex.IsMatch(l.Text, "\\b" + System.Text.RegularExpressions.Regex.Escape(f) + "\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) bad.Add(l.Id + " " + f);
            }
            Assert.IsEmpty(bad, string.Join(", ", bad));
        }

        [Test]
        public void EveryLine_HasDurationAndLipEnvelope_VoicedLinesHaveHash()
        {
            var db = Db; var bad = new List<string>(); int voiced = 0;
            foreach (var l in db.Text.Lines)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(l.Id, "^K\\d\\d_J")) continue;   // Tagebuch-Absätze werden gelesen, nicht gesprochen
                if (l.Duration <= 0.3f) bad.Add(l.Id + " Dauer");
                if (l.Voice != null) { voiced++; if (string.IsNullOrEmpty(l.Hash)) bad.Add(l.Id + " Hash"); if (l.Envelope == null || l.Envelope.Length < 5) bad.Add(l.Id + " Hüllkurve"); }
            }
            Assert.IsEmpty(bad, string.Join(", ", bad));
            Assert.Greater(voiced, db.Text.Lines.Count / 2, "Mehrheit der Zeilen vertont");
        }

        [Test]
        public void ChapterMinimumLevels_FollowPlan()
        {
            int[] plan = { 1, 5, 12, 20, 30, 30, 30, 30, 30, 30 };
            foreach (var ch in Db.Chapters) Assert.AreEqual(plan[ch.Number - 1], ch.MinLevel, "Kapitel " + ch.Number);
        }

        [Test]
        public void EveryOrigin_HasItsOwnFirstStep()
        {
            var db = Db; var seen = new HashSet<string>();
            foreach (Origin o in new[] { Origin.Hof, Origin.Posten, Origin.Ruinen })
            {
                var st = StoryModel.Current(db, StoryState.NewCharacter(o, Trait.Charme, Lodestar.Raetsel));
                Assert.IsNotNull(st, o.ToString()); seen.Add(st.Id);
            }
            Assert.AreEqual(3, seen.Count, "drei verschiedene Start-Instanzen");
        }

        [Test]
        public void Bundles_HaveSkillsTimelinesAndDrop()
        {
            var db = Db; Assert.GreaterOrEqual(db.Bundles.Count, 6);
            foreach (var b in db.Bundles)
            {
                Assert.Greater(b.Skills.Length, 0, b.Id); Assert.IsNotNull(b.Drop, b.Id);
                foreach (var s in b.Skills) { Assert.IsNotNull(s, b.Id); Assert.IsTrue(s.Id.StartsWith("Bundle_"), s.Id); Assert.IsNotNull(s.Timeline, s.Id + " Zeitleiste"); }
            }
        }

        [Test]
        public void VoicePitchShift_KeepsDuration_AndScalesPitch()
        {
            int r = VoiceBuilder.Rate; var s = new float[r * 2];
            for (int i = 0; i < s.Length; i++) { float t = i / (float)r; s[i] = 0.3f * (Mathf.Sin(2 * Mathf.PI * 180 * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 360 * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * 900 * t)); }
            Assert.AreEqual(180f, VoiceBuilder.PitchEstimate(s, r), 6f);
            foreach (var f in new[] { 0.62f, 0.8f, 1.1f })
            {
                var o = VoiceBuilder.PitchShift(s, f);
                Assert.AreEqual(s.Length, o.Length, "gleiche Dauer");
                Assert.AreEqual(180f * f, VoiceBuilder.PitchEstimate(o, r), 180f * f * 0.06f, "Faktor " + f);
            }
        }

        [Test]
        public void Biography_FromCharacterSlot_DrivesStoryState()
        {
            var slot = new CharacterSlotDto { Origin = (int)Origin.Ruinen, Trait = (int)Trait.Wildheit, Lodestar = (int)Lodestar.Schuld, VoiceB = true };
            var st = StoryState.NewCharacter((Origin)slot.Origin, (Trait)slot.Trait, (Lodestar)slot.Lodestar);
            Assert.AreEqual(Origin.Ruinen, st.OriginE); Assert.AreEqual(Trait.Wildheit, st.TraitE); Assert.AreEqual(Lodestar.Schuld, st.LodestarE);
            var none = StoryState.NewCharacter(Origin.None, Trait.None, Lodestar.None);
            Assert.AreEqual(Origin.Hof, none.OriginE, "alte Plätze ohne Biografie → Standard");
        }
    }
}
