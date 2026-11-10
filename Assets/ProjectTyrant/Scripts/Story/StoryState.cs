using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Story-Zustand je Charakter (wie GW2): Kapitel, Schritt, Flags, Biografie, Bündnis, Persönlichkeit, Briefe, Hinweise, Späher. Wird direkt im Speicherstand (JSON) abgelegt.</summary>
    [Serializable]
    public class StoryState
    {
        public int Chapter = 1;
        public int Step;                                   // Index in den für diesen Charakter gültigen Schritten
        public List<string> Flags = new List<string>();
        public int Origin, Trait, Lodestar, Pact;          // Enums als int (JsonUtility)
        public int Charm, Dignity, Ferocity;               // Persönlichkeits-Zählung aus Antworten
        public List<string> SeenScenes = new List<string>();
        public List<string> Scouts = new List<string>();
        public List<string> HintsSeen = new List<string>();
        public List<MailEntry> Mail = new List<MailEntry>();
        public List<string> Counters = new List<string>(); // "StepId#Index=Count"
        public bool TutorialSkipped;                       // migrierte Charaktere: Mechaniken gelten als bekannt
        public bool Finished;
        public int Checkpoint = -1;                        // Instanz: letzter erreichter Ziel-Index
        public List<int> CompletedChapters = new List<int>();
        public bool HintsEnabled = true;

        public Origin OriginE { get => (Origin)Origin; set => Origin = (int)value; }
        public Trait TraitE { get => (Trait)Trait; set => Trait = (int)value; }
        public Lodestar LodestarE { get => (Lodestar)Lodestar; set => Lodestar = (int)value; }
        public Pact PactE { get => (Pact)Pact; set => Pact = (int)value; }
        public bool HasFlag(string f) => !string.IsNullOrEmpty(f) && Flags.Contains(f);
        public void SetFlag(string f) { if (!string.IsNullOrEmpty(f) && !Flags.Contains(f)) Flags.Add(f); }

        public int Counter(string stepId, int index)
        {
            string key = stepId + "#" + index + "=";
            foreach (var c in Counters) if (c.StartsWith(key)) return int.Parse(c.Substring(key.Length));
            return 0;
        }
        public void SetCounter(string stepId, int index, int value)
        {
            string key = stepId + "#" + index + "=";
            for (int i = 0; i < Counters.Count; i++) if (Counters[i].StartsWith(key)) { Counters[i] = key + value; return; }
            Counters.Add(key + value);
        }
        public void ClearCounters(string stepId) => Counters.RemoveAll(c => c.StartsWith(stepId + "#"));

        /// <summary>Neuer Charakter (Biografie aus der Erstellung).</summary>
        public static StoryState NewCharacter(Origin o, Trait t, Lodestar l) => new StoryState { OriginE = o == ProjectTyrant.Origin.None ? ProjectTyrant.Origin.Hof : o, TraitE = t == ProjectTyrant.Trait.None ? ProjectTyrant.Trait.Wuerde : t, LodestarE = l == ProjectTyrant.Lodestar.None ? ProjectTyrant.Lodestar.Raetsel : l };

        /// <summary>Migration (Lauf 5 → 6): Kapitel 1, Tutorial-Zwang übersprungen, Biografie-Standard.</summary>
        public static StoryState Migrated() => new StoryState { OriginE = ProjectTyrant.Origin.Hof, TraitE = ProjectTyrant.Trait.Wuerde, LodestarE = ProjectTyrant.Lodestar.Raetsel, TutorialSkipped = true };
    }

    [Serializable]
    public class MailEntry
    {
        public string Id;
        public bool Read;
        public bool Taken;                                 // Beilage genommen
        public string Day;                                  // Zeitstempel (Anzeige)
    }
}
