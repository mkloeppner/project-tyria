using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>Clip-Satz eines Skeletts (vom ClipBuilder erzeugt): AnimationClips + Marker + Fußkurven für die IK.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Anim Clip Set", fileName = "ClipSet_")]
    public class AnimClipSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Name; public AnimationClip Clip; public ClipLayer Layer; public bool Loop; public float Length;
            public List<ClipMarker> Markers = new List<ClipMarker>();
            [Tooltip("Fußpositionen relativ zur Wurzel je Frame (30 Hz), nur Humanoid")] public Vector3[] FootL, FootR;
            public float MarkerTime(string type) { foreach (var m in Markers) if (m.Type == type) return m.Time; return -1f; }
            public Vector3 Foot(bool left, float t)
            {
                var a = left ? FootL : FootR;
                if (a == null || a.Length == 0) return Vector3.zero;
                float ft = (Loop ? Mathf.Repeat(t, Length) : Mathf.Clamp(t, 0f, Length)) * ClipData.FrameRate;
                int i = Mathf.Clamp(Mathf.FloorToInt(ft), 0, a.Length - 1), j = Mathf.Min(i + 1, a.Length - 1);
                return Vector3.Lerp(a[i], a[j], ft - i);
            }
        }
        public string Rig;
        public List<Entry> Clips = new List<Entry>();
        [NonSerialized] Dictionary<string, Entry> _map;
        public Entry Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_map == null) { _map = new Dictionary<string, Entry>(); foreach (var e in Clips) if (e != null && e.Clip != null) _map[e.Name] = e; }
            return _map.TryGetValue(name, out var r) ? r : null;
        }
        /// <summary>Clip mit Präfix-Rückfall: „Hammer_Auto1“ → falls fehlend „Greatsword_Auto1“.</summary>
        public Entry GetOr(string name, string fallback) => Get(name) ?? Get(fallback);
    }
}
