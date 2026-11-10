using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M32: Leuchtkristall in der Kristalltiefe – Lichtquelle für <see cref="DarknessModel"/>.</summary>
    public class CrystalLight : MonoBehaviour
    {
        public static readonly List<CrystalLight> All = new List<CrystalLight>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static float Nearest(Vector3 p) { float best = float.MaxValue; foreach (var c in All) if (c != null) { float d = (c.transform.position - p).magnitude; if (d < best) best = d; } return best; }
    }
}
