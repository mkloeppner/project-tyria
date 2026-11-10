using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Figuren der Geschichte mit Stimmprofil, Farben und Galerie-Text.</summary>
    public class StoryCast : ScriptableObject
    {
        public List<SpeakerProfile> Speakers = new List<SpeakerProfile>();
        public SpeakerProfile Get(string id) { foreach (var s in Speakers) if (s != null && s.Id == id) return s; return null; }
        public string Name(string id) { var s = Get(id); return s != null ? s.DisplayName : id; }
    }
}
