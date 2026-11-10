using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M30: Späher-Zelt im Dünenmeer – im Radius schützt es vor dem Sandsturm.</summary>
    public class SandShelter : MonoBehaviour
    {
        public static readonly List<SandShelter> All = new List<SandShelter>();
        public float Radius = 6f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static bool Covers(Vector3 p) { foreach (var s in All) if (s != null) { var d = p - s.transform.position; d.y = 0f; if (d.sqrMagnitude <= s.Radius * s.Radius) return true; } return false; }
    }
}
