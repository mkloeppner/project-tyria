using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Wurzel der Story-Daten (Kapitel, Text-Tabelle, Figuren, Szenen, Briefe, Hinweise).</summary>
    public class StoryDatabase : ScriptableObject
    {
        public List<StoryChapter> Chapters = new List<StoryChapter>();
        public List<EnemyArchetype> Archetypes = new List<EnemyArchetype>();   // Instanz-Gegner (vom Builder gesammelt)
        public List<Texture2D> Backdrops = new List<Texture2D>();               // gemalte Kulissen (Backdrop_<Zone>), vom Builder
        public List<GameObject> Figures = new List<GameObject>();               // Story-Figuren (PT_Story_<Sprecher>), vom Builder
        public GameObject Figure(string speaker) { foreach (var f in Figures) if (f != null && f.name == "PT_Story_" + speaker) return f; return null; }
        public EnemyArchetype Archetype(string id) { foreach (var a in Archetypes) if (a != null && a.name == id) return a; return null; }
        public StoryTextTable Text;
        public StoryCast Cast;
        public List<DialogueScript> Scenes = new List<DialogueScript>();
        public List<LetterDef> Letters = new List<LetterDef>();
        public List<HintDef> Hints = new List<HintDef>();
        public List<BundleDef> Bundles = new List<BundleDef>();
        public BundleDef Bundle(string id) { foreach (var b in Bundles) if (b != null && b.Id == id) return b; return null; }

        public StoryChapter Chapter(int n) { foreach (var c in Chapters) if (c != null && c.Number == n) return c; return null; }
        public DialogueScript Scene(string id) { if (string.IsNullOrEmpty(id)) return null; foreach (var s in Scenes) if (s != null && s.Id == id) return s; return null; }
        public LetterDef Letter(string id) { foreach (var l in Letters) if (l != null && l.Id == id) return l; return null; }
        public HintDef Hint(string id) { foreach (var h in Hints) if (h != null && h.Id == id) return h; return null; }

        public static StoryDatabase Loaded;   // vom GameManager gesetzt (kein Singleton, Daten-Referenz)
    }
}
