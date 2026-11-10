using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M30: Laterne am Wächterstein – Schutzinsel gegen die Verderbnis.</summary>
    public class CorruptionWard : MonoBehaviour
    {
        public static readonly List<CorruptionWard> All = new List<CorruptionWard>();
        public float Radius = 7f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static bool Covers(Vector3 p) { foreach (var w in All) if (w != null) { var d = p - w.transform.position; d.y = 0f; if (d.sqrMagnitude <= w.Radius * w.Radius) return true; } return false; }
    }
}
