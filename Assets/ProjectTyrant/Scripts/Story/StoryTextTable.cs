using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: alle Texte der Geschichte (Text-Tabelle, vom StoryBuilder aus der CSV erzeugt). Nie Freitext im Code.</summary>
    public class StoryTextTable : ScriptableObject
    {
        public List<StoryLine> Lines = new List<StoryLine>();
        Dictionary<string, StoryLine> _map;

        public StoryLine Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null || _map.Count != Lines.Count) { _map = new Dictionary<string, StoryLine>(); foreach (var l in Lines) if (l != null && !string.IsNullOrEmpty(l.Id)) _map[l.Id] = l; }
            return _map.TryGetValue(id, out var r) ? r : null;
        }
        public string Text(string id) { var l = Get(id); return l != null ? l.Text : id; }
        public void Invalidate() => _map = null;
    }
}
