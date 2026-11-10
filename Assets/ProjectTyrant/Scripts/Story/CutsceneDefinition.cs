using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: In-World-Cinematic – Akteure an Markern, Kamera-Schlüssel, Musik-Zustand. Die Zeilen liegen im DialogueScript.</summary>
    public class CutsceneDefinition : ScriptableObject
    {
        public string Id;
        public string Zone;
        public Vector3 Origin;                // Bezugspunkt der Marker in der Zone
        public List<CutsceneActor> Actors = new List<CutsceneActor>();
        public List<CameraKey> Camera = new List<CameraKey>();
        public List<string> Markers = new List<string>();     // definierte Marker (Name=x,y,z relativ zu Origin)
        public string Music = "";
        public float MaxSeconds = 90f;
        public Vector3 Marker(string name)
        {
            foreach (var m in Markers)
            {
                var p = m.Split('='); if (p.Length != 2 || p[0] != name) continue;
                var v = p[1].Split(','); if (v.Length != 3) continue;
                return Origin + new Vector3(float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[2], System.Globalization.CultureInfo.InvariantCulture));
            }
            return Origin;
        }
        public bool HasMarker(string name) { foreach (var m in Markers) if (m.StartsWith(name + "=")) return true; return false; }
    }
}
