using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>Animations-Marker (Ereignisse im Clip).</summary>
    public static class Marker
    {
        public const string FootL = "FootL", FootR = "FootR", Hit = "Hit", Projectile = "Projectile", Effect = "Effect", End = "End";
        public static readonly string[] All = { FootL, FootR, Hit, Projectile, Effect, End };
    }

    [Serializable] public struct ClipMarker { public string Type; public float Time; public ClipMarker(string type, float time) { Type = type; Time = time; } }

    /// <summary>Schicht, für die der Clip gedacht ist.</summary>
    public enum ClipLayer { Locomotion, FullBody, UpperBody, Additive }

    /// <summary>Gebackener Clip (rein): Euler-Rotationen je Knochen und Frame (30 Hz) + Wurzelversatz + Marker.</summary>
    public class ClipData
    {
        public const float FrameRate = 30f;
        public string Name; public string Rig; public float Length; public bool Loop; public ClipLayer Layer;
        public Vector3[][] Euler; public Vector3[] Root;
        public readonly List<ClipMarker> Markers = new List<ClipMarker>();
        public int FrameCount => Root != null ? Root.Length : 0;
        public static int FramesFor(float length) => Mathf.Max(2, Mathf.RoundToInt(length * FrameRate) + 1);
        public float MarkerTime(string type) { foreach (var m in Markers) if (m.Type == type) return m.Time; return -1f; }
        public int MarkerCount(string type) { int n = 0; foreach (var m in Markers) if (m.Type == type) n++; return n; }
        public ClipData AddMarker(string type, float time) { Markers.Add(new ClipMarker(type, Mathf.Clamp(time, 0f, Length))); return this; }
    }

    public static class Ease
    {
        public static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }
        public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
        public static float OutBack(float t) { t = Mathf.Clamp01(t); const float c = 1.70158f; float u = t - 1f; return 1f + (c + 1f) * u * u * u + c * u * u; }
        public static float Linear(float t) => Mathf.Clamp01(t);
        public static float Apply(char e, float t) { switch (e) { case 'l': return Linear(t); case 'i': return InQuad(t); case 'o': return OutQuad(t); case 'b': return OutBack(t); default: return Smooth(t); } }
    }

    /// <summary>Pose während der Clip-Erzeugung (Euler-Offsets in Grad, additiv setzbar).</summary>
    public class PoseBuilder
    {
        public readonly SkeletonDef Sk; public readonly Vector3[] E; public Vector3 Root;
        readonly Dictionary<string, int> _alias;
        public PoseBuilder(SkeletonDef sk, Dictionary<string, int> alias) { Sk = sk; E = new Vector3[sk.Count]; _alias = alias; }
        public void Clear() { Array.Clear(E, 0, E.Length); Root = Vector3.zero; }
        public int Index(string n) { if (_alias != null && _alias.TryGetValue(n, out var i)) return i; return Sk.IndexOf(n); }
        public void R(string bone, float x, float y = 0f, float z = 0f) { int i = Index(bone); if (i >= 0) E[i] += new Vector3(x, y, z); }
        public void R(string bone, Vector3 v) { int i = Index(bone); if (i >= 0) E[i] += v; }
    }

    /// <summary>
    /// Schlüsselposen in Kurzschrift: „Alias:x,y,z Alias:x …“ (fehlende Knochen = Ruhe), „root:x,y,z“ = Wurzelversatz.
    /// Zwischen Schlüsseln wird mit der Easing-Art des Ziel-Schlüssels interpoliert (s=Smooth, l=linear, i/o=Quad, b=OutBack).
    /// </summary>
    public class KeyTrack
    {
        struct K { public float T; public char E; public Dictionary<int, Vector3> Rot; public Vector3 Root; }
        readonly List<K> _keys = new List<K>();
        readonly PoseBuilder _p;
        public KeyTrack(PoseBuilder p) { _p = p; }

        public KeyTrack Key(float t, string spec, char ease = 's')
        {
            var k = new K { T = t, E = ease, Rot = new Dictionary<int, Vector3>() };
            if (!string.IsNullOrEmpty(spec))
                foreach (var tok in spec.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int c = tok.IndexOf(':'); if (c <= 0) continue;
                    string name = tok.Substring(0, c);
                    var nums = tok.Substring(c + 1).Split(',');
                    var v = Vector3.zero;
                    for (int i = 0; i < nums.Length && i < 3; i++) v[i] = float.Parse(nums[i], CultureInfo.InvariantCulture);
                    if (name == "root") { k.Root += v; continue; }
                    int bi = _p.Index(name);
                    if (bi < 0) throw new ArgumentException("Unbekannter Knochen in Schlüsselpose: " + name);
                    k.Rot[bi] = k.Rot.TryGetValue(bi, out var o) ? o + v : v;
                }
            _keys.Add(k);
            return this;
        }

        /// <summary>Addiert die interpolierte Pose zur Zeit t auf den PoseBuilder.</summary>
        public void Apply(float t)
        {
            if (_keys.Count == 0) return;
            int j = 0; while (j < _keys.Count && _keys[j].T < t) j++;
            if (j == 0) { Add(_keys[0], null, 0f); return; }
            if (j >= _keys.Count) { Add(_keys[_keys.Count - 1], null, 0f); return; }
            var a = _keys[j - 1]; var b = _keys[j];
            float u = b.T - a.T <= 1e-5f ? 1f : Ease.Apply(b.E, (t - a.T) / (b.T - a.T));
            Add(a, b, u);
        }

        void Add(K a, K? b, float u)
        {
            if (b == null) { foreach (var kv in a.Rot) _p.E[kv.Key] += kv.Value; _p.Root += a.Root; return; }
            var bb = b.Value;
            foreach (var kv in a.Rot) { bb.Rot.TryGetValue(kv.Key, out var vb); _p.E[kv.Key] += Vector3.LerpUnclamped(kv.Value, vb, u); }
            foreach (var kv in bb.Rot) if (!a.Rot.ContainsKey(kv.Key)) _p.E[kv.Key] += kv.Value * u;
            _p.Root += Vector3.LerpUnclamped(a.Root, bb.Root, u);
        }
    }
}
