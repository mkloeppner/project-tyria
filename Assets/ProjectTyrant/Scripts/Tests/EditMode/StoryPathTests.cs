using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 6 / M31 + M33, erweitert Lauf 11 (26 Kapitel): alle 27 Biografie-×-Bündnis-Kombinationen vom ersten bis zum letzten Schritt (Ziele erfüllen, Wahl in K05, Varianten, Belohnungen).</summary>
    public class StoryPathTests
    {
        static readonly Origin[] Origins = { Origin.Hof, Origin.Posten, Origin.Ruinen };
        static readonly Trait[] Traits = { Trait.Charme, Trait.Wuerde, Trait.Wildheit };
        static readonly Pact[] Pacts = { Pact.Wacht, Pact.Archiv, Pact.Schleier };

        static IEnumerable<object[]> Combos()
        {
            foreach (var o in Origins) foreach (var t in Traits) foreach (var p in Pacts) yield return new object[] { o, t, p };
        }

        [TestCaseSource(nameof(Combos))]
        public void FullStory_IsPlayableToTheEnd(Origin o, Trait t, Pact p)
        {
            var db = StoryBuilder.Load(); Assert.IsNotNull(db);
            Assert.AreEqual(26, db.Chapters.Count, "26 Kapitel (Grundspiel 10 + Zwischenspiel 2 + Thron der Glut 7 + Jadeherz 7)");
            bool sturm = t != Trait.Wildheit, kaiserin = p != Pact.Schleier;   // Wahl in K17 / K23 je Kombination variieren
            var s = StoryState.NewCharacter(o, t, (Lodestar)(1 + ((int)o + (int)t) % 3));
            int steps = 0, xp = 0; var seen = new List<string>();
            while (!s.Finished && steps < 200)
            {
                var st = StoryModel.Current(db, s); Assert.IsNotNull(st, "Kapitel " + s.Chapter + " Schritt " + s.Step);
                seen.Add(st.Id);
                if (st.Id == "K05_S04") s.PactE = p;   // Bündnis-Wahl
                if (st.Id == "K17_S01") s.SetFlag(sturm ? "weg_sturm" : "weg_list");          // Wahl Sturm/List
                if (st.Id == "K23_S01") s.SetFlag(kaiserin ? "pakt_kaiserin" : "pakt_daro");   // Wahl Kaiserin/Daro
                if (st.Id == "K25_S02") s.SetFlag(o == Origin.Hof ? "drache_frei" : "stadt_gesichert");
                Assert.IsTrue(StoryModel.Meets(st.Condition, s), st.Id + ": Variante passt nicht");
                for (int i = 0; i < st.Objectives.Count; i++) if (!st.Objectives[i].Optional) s.SetCounter(st.Id, i, st.Objectives[i].Count);
                Assert.IsTrue(StoryModel.StepDone(st, s), st.Id);
                var r = StoryModel.Complete(db, s); xp += r.Reward.Experience;
                steps++;
            }
            Assert.IsTrue(s.Finished, "Geschichte endet");
            Assert.AreEqual(p, s.PactE);
            Assert.Contains("K01_S01" + "HPR"[(int)o - 1], seen, "Herkunfts-Instanz");
            Assert.Contains("K06_S02" + "WAS"[(int)p - 1], seen, "Bündnis-Instanz");
            Assert.IsFalse(seen.Any(id => id.StartsWith("K06_S02") && id != "K06_S02" + "WAS"[(int)p - 1]), "fremde Bündnis-Instanz");
            Assert.Greater(xp, 50000, "Belohnungen summiert");
            for (int k = 11; k <= 26; k++) Assert.IsTrue(seen.Any(id => id.StartsWith("K" + k + "_")), "Kapitel " + k + " erreicht");
            Assert.Contains(sturm ? "K17_S02A" : "K17_S02B", seen, "K17-Variante");
            Assert.IsFalse(seen.Contains(sturm ? "K17_S02B" : "K17_S02A"), "fremde K17-Variante");
            Assert.Contains(kaiserin ? "K23_S02A" : "K23_S02B", seen, "K23-Variante");
            Assert.IsFalse(seen.Contains(kaiserin ? "K23_S02B" : "K23_S02A"), "fremde K23-Variante");
            Assert.Contains("K26_S03", seen, "Finale Jadeherz");
            Assert.Greater(xp, 150000, "Belohnungen inkl. Erweiterungen summiert");
            Assert.That(seen.Count, Is.InRange(75, 95), "Schritte gesamt (90 Definitionen abzüglich nicht gewählter Varianten)");
        }

        [Test]
        public void EveryChapter_HasThreeChoices_OneWithFlag_And3To5Steps()
        {
            var db = StoryBuilder.Load();
            foreach (var ch in db.Chapters)
            {
                string pre = "K" + ch.Number.ToString("00") + "_";
                var choices = db.Scenes.Where(sc => sc.Id.StartsWith(pre)).SelectMany(sc => sc.Choices).ToList();
                Assert.GreaterOrEqual(choices.Count, 3, "Kapitel " + ch.Number + ": Wahl-Stellen");
                Assert.IsTrue(choices.Any(c => c.Options.Any(op => !string.IsNullOrEmpty(op.Flag))), "Kapitel " + ch.Number + ": Wahl mit Flag");
                int distinct = ch.Steps.Select(st => st.Id.Substring(0, 7)).Distinct().Count();   // Varianten zählen als ein Schritt
                Assert.That(distinct, Is.InRange(3, 5), "Kapitel " + ch.Number + ": Schritte");
            }
        }

        [Test]
        public void LodestarVariants_ExistForChapter7And10()
        {
            var db = StoryBuilder.Load();
            foreach (var l in new[] { "Verlorener", "Raetsel", "Schuld" })
            {
                Assert.IsTrue(db.Text.Lines.Any(x => x.Id.StartsWith("K07_") && (x.Id.EndsWith("@" + l))) || db.Scenes.Any(sc => sc.Id.StartsWith("K07_") && sc.Entries.Any(e => e.Condition.Lodestar.ToString() == l)), "K07 " + l);
                Assert.IsTrue(db.Text.Lines.Any(x => x.Id.StartsWith("K10_") && x.Id.EndsWith("@" + l)), "K10 " + l);
            }
        }
    }
}
