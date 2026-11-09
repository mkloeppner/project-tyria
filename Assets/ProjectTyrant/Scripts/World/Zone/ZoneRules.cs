using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Reine Regeln des Zonenabschlusses (getestet).</summary>
    public static class ZoneRules
    {
        /// <summary>Eine Wegmarke ist umkämpft, wenn sie im Radius eines laufenden Events liegt.</summary>
        public static bool IsContested(Vector3 waypoint, IList<(Vector3 center, float radius)> events)
        {
            if (events == null) return false;
            foreach (var e in events)
            {
                float dx = waypoint.x - e.center.x, dz = waypoint.z - e.center.z;
                if (dx * dx + dz * dz <= e.radius * e.radius) return true;
            }
            return false;
        }

        /// <summary>Fügt Herz-Fortschritt hinzu (0..1). Gibt true zurück, wenn das Herz genau jetzt erfüllt wurde.</summary>
        public static bool AddHeartProgress(Dictionary<string, float> hearts, ICollection<string> completed, string id, float amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0f || completed.Contains(id)) return false;
            hearts.TryGetValue(id, out float v);
            v = Mathf.Min(1f, v + amount);
            hearts[id] = v;
            if (v >= 1f - 1e-4f) { completed.Add(id); return true; }
            return false;
        }

        public static float HeartProgress(Dictionary<string, float> hearts, ICollection<string> completed, string id)
            => completed.Contains(id) ? 1f : hearts.TryGetValue(id, out float v) ? v : 0f;

        /// <summary>Zählt den Zonenabschluss aus Definitionen und erledigten Ids (Wegmarken = entdeckt).</summary>
        public static ZoneCompletion Count(IEnumerable<ZoneObjectDefinition> defs, ICollection<string> completed)
        {
            var c = new ZoneCompletion();
            foreach (var d in defs)
            {
                if (d == null) continue;
                var one = new Vector2Int(completed.Contains(d.Id) ? 1 : 0, 1);
                switch (d.Kind)
                {
                    case MapMarkerKind.Waypoint: c.Waypoints += one; break;
                    case MapMarkerKind.PointOfInterest: c.Pois += one; break;
                    case MapMarkerKind.Vista: c.Vistas += one; break;
                    case MapMarkerKind.Heart: c.Hearts += one; break;
                    case MapMarkerKind.HeroChallenge: c.Heroes += one; break;
                }
            }
            return c;
        }

        /// <summary>Truhe genau einmal bei 100 %.</summary>
        public static bool ShouldGrantChest(ZoneCompletion c, bool alreadyGranted) => c.Complete && !alreadyGranted;

        /// <summary>Laufende Event-Flächen (für „umkämpft“). Bis M13 speist die Dornwacht-Kette, danach die Ereignis-Engine.</summary>
        public static event Action<List<(Vector3 center, float radius)>> CollectEventAreas;
        static readonly List<(Vector3, float)> _areas = new List<(Vector3, float)>();
        public static List<(Vector3 center, float radius)> ActiveEventAreas()
        {
            _areas.Clear();
            CollectEventAreas?.Invoke(_areas);
            return _areas;
        }
    }
}
