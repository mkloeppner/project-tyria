using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: reine Story-Logik (GW2-Persönliche Geschichte): gültige Schritte je Biografie/Bündnis/Flags, Mindeststufe,
    /// Ziel-Zähler (sequentiell; optionale Ziele jederzeit), Schrittabschluss mit Flags/Belohnung/Brief, Zeilen-Varianten, Tagebuch.
    /// </summary>
    public static class StoryModel
    {
        public static bool Meets(StoryCondition c, StoryState s)
        {
            if (c == null || s == null) return true;
            if (c.Origin != Origin.None && s.OriginE != c.Origin) return false;
            if (c.Pact != Pact.None && s.PactE != c.Pact) return false;
            if (c.Trait != Trait.None && s.TraitE != c.Trait) return false;
            if (c.Lodestar != Lodestar.None && s.LodestarE != c.Lodestar) return false;
            if (!string.IsNullOrEmpty(c.Flag) && !s.HasFlag(c.Flag)) return false;
            if (!string.IsNullOrEmpty(c.NotFlag) && s.HasFlag(c.NotFlag)) return false;
            return true;
        }

        public static List<StoryStep> StepsFor(StoryChapter ch, StoryState s)
        {
            var list = new List<StoryStep>();
            if (ch == null) return list;
            foreach (var st in ch.Steps) if (st != null && Meets(st.Condition, s)) list.Add(st);
            return list;
        }

        public static StoryChapter CurrentChapter(StoryDatabase db, StoryState s) => db == null || s == null || s.Finished ? null : db.Chapter(s.Chapter);

        public static StoryStep Current(StoryDatabase db, StoryState s)
        {
            var ch = CurrentChapter(db, s); if (ch == null) return null;
            var steps = StepsFor(ch, s);
            return s.Step >= 0 && s.Step < steps.Count ? steps[s.Step] : null;
        }

        /// <summary>Mindeststufe des aktuellen Kapitels erreicht (GW2-Staffelung)?</summary>
        public static bool Available(StoryDatabase db, StoryState s, int level) { var ch = CurrentChapter(db, s); return ch != null && level >= ch.MinLevel; }

        public static int FirstOpenObjective(StoryStep st, StoryState s)
        {
            if (st == null) return -1;
            for (int i = 0; i < st.Objectives.Count; i++) { var o = st.Objectives[i]; if (o.Optional) continue; if (s.Counter(st.Id, i) < Math.Max(1, o.Count)) return i; }
            return -1;
        }

        public static bool StepDone(StoryStep st, StoryState s) => st != null && FirstOpenObjective(st, s) < 0;

        /// <summary>Meldet ein Spielereignis. Zählt das erste offene Pflichtziel (sequentiell) und passende optionale Ziele. true, wenn sich etwas geändert hat.</summary>
        public static bool Report(StoryDatabase db, StoryState s, ObjectiveKind kind, string target, int level, int amount = 1)
        {
            var st = Current(db, s);
            if (st == null || !Available(db, s, level)) return false;
            bool changed = false;
            int open = FirstOpenObjective(st, s);
            for (int i = 0; i < st.Objectives.Count; i++)
            {
                var o = st.Objectives[i];
                if (!o.Optional && i != open) continue;
                if (o.Kind != kind || !TargetMatches(o.Target, target)) continue;
                int c = s.Counter(st.Id, i), need = Math.Max(1, o.Count);
                if (c >= need) continue;
                s.SetCounter(st.Id, i, Math.Min(need, c + Math.Max(1, amount)));
                changed = true;
            }
            return changed;
        }

        static bool TargetMatches(string want, string got) => string.IsNullOrEmpty(want) || want == "*" || (got != null && (got == want || got.StartsWith(want + ":") || got.Contains(want)));

        public struct StepResult { public StoryStep Step; public StoryReward Reward; public string Letter; public bool ChapterDone; public int NewChapter; public bool StoryDone; }

        /// <summary>Schritt abschließen: Flags, Belohnung, Brief; weiter zum nächsten gültigen Schritt bzw. Kapitel.</summary>
        public static StepResult Complete(StoryDatabase db, StoryState s)
        {
            var r = new StepResult();
            var st = Current(db, s); if (st == null) return r;
            r.Step = st; r.Reward = st.Reward; r.Letter = st.Letter;
            foreach (var f in st.SetFlags) s.SetFlag(f);
            s.ClearCounters(st.Id); s.Checkpoint = -1;
            s.Step++;
            var ch = CurrentChapter(db, s);
            if (s.Step >= StepsFor(ch, s).Count)
            {
                if (!s.CompletedChapters.Contains(s.Chapter)) s.CompletedChapters.Add(s.Chapter);
                r.ChapterDone = true; s.Chapter++; s.Step = 0;
                if (db.Chapter(s.Chapter) == null) { s.Finished = true; r.StoryDone = true; }
                r.NewChapter = s.Chapter;
            }
            return r;
        }

        /// <summary>Antwort in einem Dialog: Persönlichkeit zählen, Flag setzen, Bündnis wählen.</summary>
        public static void Choose(StoryState s, DialogueOption o)
        {
            if (s == null || o == null) return;
            if (o.Tendency == Trait.Charme) s.Charm++; else if (o.Tendency == Trait.Wuerde) s.Dignity++; else if (o.Tendency == Trait.Wildheit) s.Ferocity++;
            s.SetFlag(o.Flag);
            if (o.SetPact != Pact.None) { s.PactE = o.SetPact; s.SetFlag("pact_" + o.SetPact.ToString().ToLowerInvariant()); }
        }

        /// <summary>Varianten-Zeile: Id + "@" + Bündnis/Herkunft/Charakterzug/Leitstern (erste vorhandene gewinnt), sonst die Grundzeile.</summary>
        public static StoryLine Resolve(StoryTextTable t, string id, StoryState s)
        {
            if (t == null || string.IsNullOrEmpty(id)) return null;
            if (s != null)
                foreach (var suf in new[] { s.PactE != Pact.None ? s.PactE.ToString() : null, s.OriginE != Origin.None ? s.OriginE.ToString() : null, s.TraitE != Trait.None ? s.TraitE.ToString() : null, s.LodestarE != Lodestar.None ? s.LodestarE.ToString() : null })
                {
                    if (suf == null) continue;
                    var v = t.Get(id + "@" + suf); if (v != null) return v;
                }
            return t.Get(id);
        }

        /// <summary>Zeilen einer Szene in Abspiel-Reihenfolge (Varianten-Bedingungen ausgewertet).</summary>
        public static List<DialogueEntry> Entries(DialogueScript sc, StoryState s)
        {
            var list = new List<DialogueEntry>();
            if (sc == null) return list;
            foreach (var e in sc.Entries) if (e != null && Meets(e.Condition, s)) list.Add(e);
            return list;
        }

        /// <summary>Tagebuch: je abgeschlossenem Kapitel ein Absatz (Varianten wie Zeilen).</summary>
        public static List<string> Journal(StoryDatabase db, StoryState s)
        {
            var list = new List<string>();
            if (db == null || s == null) return list;
            foreach (var ch in db.Chapters)
            {
                if (ch == null || !s.CompletedChapters.Contains(ch.Number)) continue;
                foreach (var id in ch.JournalLines) { var l = Resolve(db.Text, id, s); if (l != null) list.Add(l.Text); }
            }
            return list;
        }

        public static Trait Dominant(StoryState s) => s.Charm >= s.Dignity && s.Charm >= s.Ferocity ? (s.Charm == 0 ? Trait.None : Trait.Charme) : s.Dignity >= s.Ferocity ? Trait.Wuerde : Trait.Wildheit;
    }

    /// <summary>Story-Instanz: Kontrollpunkte (Ziel-Index), Scheitern = Neustart am letzten Kontrollpunkt, Verlassen, Timer pausierbar.</summary>
    public class InstanceModel
    {
        public string StepId { get; private set; }
        public bool Active { get; private set; }
        public int Checkpoint { get; private set; } = -1;
        public float Timer { get; private set; }
        public bool Paused;
        public int Failures { get; private set; }

        public void Enter(StoryStep st, StoryState s) { StepId = st != null ? st.Id : null; Active = st != null; Checkpoint = s != null ? s.Checkpoint : -1; Timer = 0f; Failures = 0; }
        public void Tick(float dt) { if (Active && !Paused) Timer += dt; }
        /// <summary>Nach jedem abgeschlossenen Pflichtziel, das im Schritt als Kontrollpunkt markiert ist.</summary>
        public bool Reach(StoryStep st, StoryState s, int objectiveIndex)
        {
            if (!Active || st == null || st.Id != StepId) return false;
            if (!st.Checkpoints.Contains(objectiveIndex.ToString()) || objectiveIndex <= Checkpoint) return false;
            Checkpoint = objectiveIndex; s.Checkpoint = objectiveIndex; return true;
        }
        /// <summary>Scheitern: Zähler nach dem letzten Kontrollpunkt zurücksetzen; gibt den Ziel-Index zurück, ab dem neu gestartet wird.</summary>
        public int Fail(StoryStep st, StoryState s)
        {
            Failures++;
            if (st == null) return 0;
            for (int i = Checkpoint + 1; i < st.Objectives.Count; i++) s.SetCounter(st.Id, i, 0);
            return Checkpoint + 1;
        }
        public void Leave() { Active = false; StepId = null; Paused = false; }
    }

    /// <summary>Briefe (GW2-Post): jeder Brief genau einmal, Beilage einmal nehmbar.</summary>
    public static class MailModel
    {
        public static bool Send(StoryState s, string id, string day = "")
        {
            if (s == null || string.IsNullOrEmpty(id)) return false;
            foreach (var m in s.Mail) if (m.Id == id) return false;
            s.Mail.Add(new MailEntry { Id = id, Day = day }); return true;
        }
        public static int Unread(StoryState s) { int n = 0; if (s != null) foreach (var m in s.Mail) if (!m.Read) n++; return n; }
        public static void Read(StoryState s, string id) { if (s != null) foreach (var m in s.Mail) if (m.Id == id) m.Read = true; }
        public static bool Take(StoryState s, string id) { if (s != null) foreach (var m in s.Mail) if (m.Id == id && !m.Taken) { m.Taken = true; m.Read = true; return true; } return false; }
    }

    /// <summary>Lern-Hinweise: jeder genau einmal, abschaltbar; migrierte Charaktere sehen Tutorial-Hinweise nicht.</summary>
    public static class HintModel
    {
        public static bool ShouldShow(StoryState s, string id, bool tutorial = true)
        {
            if (s == null || string.IsNullOrEmpty(id) || !s.HintsEnabled || s.HintsSeen.Contains(id)) return false;
            if (tutorial && s.TutorialSkipped) return false;
            return true;
        }
        public static void MarkSeen(StoryState s, string id) { if (s != null && !string.IsNullOrEmpty(id) && !s.HintsSeen.Contains(id)) s.HintsSeen.Add(id); }
    }
}
