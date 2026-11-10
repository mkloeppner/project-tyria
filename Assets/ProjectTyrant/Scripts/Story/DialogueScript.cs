using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: eine Szene – Zeilenfolge mit Sprechern, Cues und Wahl-Stellen (Gesprächsszene, Cinematic oder Sprechblase).</summary>
    public class DialogueScript : ScriptableObject
    {
        public string Id;
        public SceneForm Form;
        public string Left, Right;            // Sprecher links/rechts (Gesprächsszene)
        public string Backdrop;               // Zonen-Id der Kulisse
        public string Music = "";             // Musik-Zustand während der Szene ("" = unverändert)
        public bool SkipAllowed = true;       // Wiederholung immer überspringbar
        public List<DialogueEntry> Entries = new List<DialogueEntry>();
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
        public CutsceneDefinition Cutscene;   // nur Form Cinematic
    }
}
