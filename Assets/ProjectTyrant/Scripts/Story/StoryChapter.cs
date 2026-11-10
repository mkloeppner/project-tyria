using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Kapitel der Persönlichen Geschichte (Titel, Zone, Mindeststufe, Schritte inkl. Varianten).</summary>
    public class StoryChapter : ScriptableObject
    {
        public int Number;
        public string Title;
        public string Zone;
        public int MinLevel = 1;
        [TextArea] public string Summary;
        public List<StoryStep> Steps = new List<StoryStep>();
        [Tooltip("Tagebuch-Absatz je Variante: Bedingung → Text-Zeilen-Id")] public List<string> JournalLines = new List<string>();
        public List<string> Mechanics = new List<string>();   // gelehrte Mechaniken (Doku/Checkliste)
    }
}
