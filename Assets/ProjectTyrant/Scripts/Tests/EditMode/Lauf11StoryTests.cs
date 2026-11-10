using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 11 / M63 + M65: Zwischenspiel und Erweiterungen – Struktur, Gates, Verzweigungen, Szenen/Briefe/Hinweise, Modell-Durchlauf aller Pfade.</summary>
    public class Lauf11StoryTests
    {
        static StoryDatabase Db { get { var db = StoryBuilder.Load(); Assert.IsNotNull(db); return db; } }

        [Test] public void Chapters11To26_At80()
        {
            var db = Db;
            Assert.AreEqual(26, db.Chapters.Count);
            for (int n = 11; n <= 26; n++) { var ch = db.Chapter(n); Assert.IsNotNull(ch, "Kapitel " + n); Assert.AreEqual(80, ch.MinLevel); Assert.GreaterOrEqual(ch.Steps.Count, 2, "Kapitel " + n); }
        }

        [Test] public void Expansions_Zones_AndRegions()
        {
            var db = Db;
            for (int n = 13; n <= 19; n++) foreach (var st in db.Chapter(n).Steps) { var s = RegionZones.Get(st.Zone); Assert.IsTrue(s != null && (s.Region == WorldRegion.Glutwueste), st.Id + " in der Glutwüste"); }
            for (int n = 20; n <= 26; n++) foreach (var st in db.Chapter(n).Steps) { var s = RegionZones.Get(st.Zone); Assert.IsTrue(s != null && (s.Region == WorldRegion.Dracheninseln), st.Id + " auf den Dracheninseln"); }
        }

        [Test] public void ScenesLettersHints_Exist()
        {
            var db = Db;
            for (int n = 11; n <= 26; n++)
                foreach (var st in db.Chapter(n).Steps)
                {
                    if (!string.IsNullOrEmpty(st.SceneStart)) Assert.IsNotNull(db.Scene(st.SceneStart), st.Id + " Start " + st.SceneStart);
                    if (!string.IsNullOrEmpty(st.SceneEnd)) Assert.IsNotNull(db.Scene(st.SceneEnd), st.Id + " Ende " + st.SceneEnd);
                    if (!string.IsNullOrEmpty(st.Letter)) Assert.IsNotNull(db.Letter(st.Letter), st.Letter);
                    foreach (var o in st.Objectives) if (!string.IsNullOrEmpty(o.Hint)) Assert.IsNotNull(db.Hint(o.Hint), st.Id + " Hinweis " + o.Hint);
                }
        }

        [Test] public void Mechanics_TaughtByStory()
        {
            var hints = new HashSet<string>();
            foreach (var ch in Db.Chapters) if (ch.Number >= 11) foreach (var st in ch.Steps) foreach (var o in st.Objectives) if (!string.IsNullOrEmpty(o.Hint)) hints.Add(o.Hint);
            foreach (var h in new[] { "region_travel", "corruption_res", "bounty", "dunerunner", "jadebot", "boat", "fishing" }) Assert.IsTrue(hints.Contains(h), "Lern-Hinweis " + h);
        }

        [Test] public void Branches_Ch17_Ch23_HaveVariantSteps()
        {
            var db = Db;
            var a = new StoryState { Chapter = 17 }; a.SetFlag("weg_sturm");
            var b = new StoryState { Chapter = 17 }; b.SetFlag("weg_list");
            Assert.AreEqual("K17_S02A", StoryModel.StepsFor(db.Chapter(17), a)[1].Id);
            Assert.AreEqual("K17_S02B", StoryModel.StepsFor(db.Chapter(17), b)[1].Id);
            var k = new StoryState { Chapter = 23 }; k.SetFlag("pakt_kaiserin");
            var d = new StoryState { Chapter = 23 }; d.SetFlag("pakt_daro");
            Assert.AreEqual("K23_S02A", StoryModel.StepsFor(db.Chapter(23), k)[1].Id);
            Assert.AreEqual("K23_S02B", StoryModel.StepsFor(db.Chapter(23), d)[1].Id);
        }

        [Test] public void Decisions_SetFlagsInDialogue()
        {
            var db = Db;
            foreach (var (scene, flags) in new[] { ("K17_S01", new[] { "weg_sturm", "weg_list" }), ("K23_S01", new[] { "pakt_kaiserin", "pakt_daro" }), ("K25_S02", new[] { "drache_frei", "stadt_gesichert" }) })
            {
                var sc = db.Scene(scene); Assert.IsNotNull(sc, scene); Assert.AreEqual(1, sc.Choices.Count, scene);
                var got = new HashSet<string>(); foreach (var o in sc.Choices[0].Options) got.Add(o.Flag);
                foreach (var f in flags) Assert.IsTrue(got.Contains(f), scene + " → " + f);
            }
        }

        [Test] public void ModelRunThrough_AllPathPairs_Reach26AndFinish()
        {
            var db = Db;
            foreach (var p17 in new[] { "weg_sturm", "weg_list" })
                foreach (var p23 in new[] { "pakt_kaiserin", "pakt_daro" })
                    foreach (var p25 in new[] { "drache_frei", "stadt_gesichert" })
                    {
                        var s = new StoryState { Chapter = 11 };
                        int guard = 0;
                        while (!s.Finished && guard++ < 200)
                        {
                            if (s.Chapter == 17 && s.Step == 1) s.SetFlag(p17);
                            if (s.Chapter == 23 && s.Step == 1) s.SetFlag(p23);
                            if (s.Chapter == 25 && s.Step == 1) s.SetFlag(p25);
                            var st = StoryModel.Current(db, s); Assert.IsNotNull(st, "Kapitel " + s.Chapter + " Schritt " + s.Step);
                            StoryModel.Complete(db, s);
                        }
                        Assert.IsTrue(s.Finished, p17 + "/" + p23 + "/" + p25);
                        Assert.IsTrue(s.HasFlag("glut_abgeschlossen") && s.HasFlag("jade_abgeschlossen"));
                        Assert.IsTrue(s.HasFlag("duenenlaeufer") && s.HasFlag("jadegeist"));
                    }
        }

        [Test] public void Migration_FinishedTyrantStory_ContinuesWithInterlude()
        {
            var dto = UnityEngine.JsonUtility.FromJson<SaveGameDto>("{\"Version\":4,\"Level\":30,\"Story\":{\"Chapter\":11,\"Step\":0,\"Finished\":true}}");
            var st = new ProfileState(); st.LoadDto(dto, null, new List<SkillDefinition>());
            Assert.IsFalse(st.Story.Finished); Assert.AreEqual(11, st.Story.Chapter);
        }

        [Test] public void Cast_NewCharactersPresent()
        {
            var db = Db;
            foreach (var id in new[] { "nadira", "ilyas", "zarath", "linsao", "yuen", "kael", "mei", "daro" }) Assert.IsNotNull(db.Cast.Get(id), id);
        }

        [Test] public void LinesComplete_EverySceneLineHasText()
        {
            var db = Db; int n = 0;
            foreach (var sc in db.Scenes)
            {
                if (!sc.Id.StartsWith("K1") && !sc.Id.StartsWith("K2") && !sc.Id.StartsWith("SP_scout")) continue;
                if (sc.Id.StartsWith("K1") && sc.Id.Length > 2 && (sc.Id[2] == '0' || sc.Id.StartsWith("K10"))) continue;
                foreach (var e in sc.Entries) { var l = db.Text.Get(e.Line); Assert.IsNotNull(l, sc.Id + " " + e.Line); Assert.IsNotEmpty(l.Text); n++; }
            }
            Assert.GreaterOrEqual(n, 250);
        }
    }
}
