using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// Haut-Gewichte nach Knochennähe (rein): Abstand jedes Vertex zum Knochensegment (Knochen → erstes Kind),
    /// Gewicht ∝ 1/(d+ε)^p, die vier stärksten Einflüsse bleiben, normiert auf Summe 1.
    /// An Gelenken wird über eine Übergangszone (Falloff) geglättet; optional Laplace-Glättung über Mesh-Kanten.
    /// </summary>
    public static class SkinWeights
    {
        public struct Influence { public int B0, B1, B2, B3; public float W0, W1, W2, W3; public float Sum => W0 + W1 + W2 + W3; }

        /// <summary>Segment je Knochen in Bind-Weltkoordinaten (Start = Knochen, Ende = Mittel der Kinder bzw. kurzer Fortsatz).</summary>
        public static void Segments(SkeletonDef sk, out Vector3[] a, out Vector3[] b)
        {
            var pos = sk.WorldPositions(null, Vector3.zero);
            a = new Vector3[sk.Count]; b = new Vector3[sk.Count];
            for (int i = 0; i < sk.Count; i++)
            {
                a[i] = pos[i]; Vector3 sum = Vector3.zero; int n = 0;
                for (int c = 0; c < sk.Count; c++) if (sk.Bones[c].Parent == i) { sum += pos[c]; n++; }
                if (n > 0) b[i] = sum / n;
                else
                {
                    int p = sk.Bones[i].Parent;
                    Vector3 dir = p >= 0 ? (pos[i] - pos[p]) : Vector3.up;
                    b[i] = pos[i] + dir.normalized * Mathf.Max(0.05f, dir.magnitude * 0.5f);
                }
            }
        }

        public static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a; float l2 = ab.sqrMagnitude;
            float t = l2 < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
            return Vector3.Distance(p, a + ab * t);
        }

        /// <param name="preferred">optional: Knochen, dem der Vertex „gehört“ (Teil-Mesh), erhält Bonus → keine Fremd-Einflüsse über Lücken</param>
        public static Influence Compute(Vector3 p, Vector3[] a, Vector3[] b, int preferred = -1, float power = 4f, float falloff = 0.06f)
        {
            int n = a.Length;
            var w = new float[n];
            float best = float.MaxValue;
            for (int i = 0; i < n; i++) { float d = SegDist(p, a[i], b[i]); w[i] = d; if (d < best) best = d; }
            for (int i = 0; i < n; i++)
            {
                float d = w[i];
                // nur Knochen innerhalb der Übergangszone um den nächsten Knochen beeinflussen den Vertex
                float excess = d - best;
                float zone = excess >= falloff ? 0f : 1f - excess / falloff;
                float v = zone <= 0f ? 0f : Mathf.Pow(zone, 2f) / Mathf.Pow(d + 0.02f, power);
                if (i == preferred) v = Mathf.Max(v, 1f / Mathf.Pow(best + 0.02f, power)) * 1.5f;
                w[i] = v;
            }
            return Top4(w);
        }

        public static Influence Top4(float[] w)
        {
            int[] idx = { 0, 0, 0, 0 }; float[] val = { -1, -1, -1, -1 };
            for (int i = 0; i < w.Length; i++)
            {
                float v = w[i]; if (v <= 0f) continue;
                for (int k = 0; k < 4; k++)
                    if (v > val[k]) { for (int m = 3; m > k; m--) { val[m] = val[m - 1]; idx[m] = idx[m - 1]; } val[k] = v; idx[k] = i; break; }
            }
            float s = 0f; for (int k = 0; k < 4; k++) if (val[k] > 0f) s += val[k]; else val[k] = 0f;
            if (s <= 0f) { val[0] = 1f; s = 1f; }
            var r = new Influence { B0 = idx[0], B1 = idx[1], B2 = idx[2], B3 = idx[3], W0 = val[0] / s, W1 = val[1] / s, W2 = val[2] / s, W3 = val[3] / s };
            Fix(ref r);
            return r;
        }

        /// <summary>Rundungsfehler auf das stärkste Gewicht schieben, damit die Summe exakt 1 ist.</summary>
        static void Fix(ref Influence r) { float e = 1f - r.Sum; r.W0 += e; }

        /// <summary>Laplace-Glättung der Gewichte über Mesh-Nachbarn (Gelenkübergänge weicher), danach wieder Top-4 + Normierung.</summary>
        public static Influence[] Smooth(Influence[] inf, int[] triangles, int boneCount, int iterations = 1, float amount = 0.5f, bool[] locked = null)
        {
            int n = inf.Length;
            var nb = new List<int>[n];
            for (int i = 0; i < n; i++) nb[i] = new List<int>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int x = triangles[t], y = triangles[t + 1], z = triangles[t + 2];
                nb[x].Add(y); nb[x].Add(z); nb[y].Add(x); nb[y].Add(z); nb[z].Add(x); nb[z].Add(y);
            }
            var cur = inf;
            for (int it = 0; it < iterations; it++)
            {
                var next = new Influence[n];
                var acc = new float[boneCount];
                for (int i = 0; i < n; i++)
                {
                    if ((locked != null && locked[i]) || nb[i].Count == 0) { next[i] = cur[i]; continue; }
                    System.Array.Clear(acc, 0, boneCount);
                    Add(acc, cur[i], 1f - amount);
                    float share = amount / nb[i].Count;
                    foreach (var j in nb[i]) Add(acc, cur[j], share);
                    next[i] = Top4(acc);
                }
                cur = next;
            }
            return cur;
        }

        static void Add(float[] acc, Influence f, float k)
        {
            acc[f.B0] += f.W0 * k; acc[f.B1] += f.W1 * k; acc[f.B2] += f.W2 * k; acc[f.B3] += f.W3 * k;
        }

        /// <summary>Linear-Blend-Skinning eines Punkts (für Tests/Riss-Metrik): Σ w·(M_pose·M_bind⁻¹)·p.</summary>
        public static Vector3 Skin(Vector3 p, Influence f, Matrix4x4[] skinMatrices)
        {
            return skinMatrices[f.B0].MultiplyPoint3x4(p) * f.W0 + skinMatrices[f.B1].MultiplyPoint3x4(p) * f.W1
                 + skinMatrices[f.B2].MultiplyPoint3x4(p) * f.W2 + skinMatrices[f.B3].MultiplyPoint3x4(p) * f.W3;
        }

        /// <summary>Skin-Matrizen aus Euler-Pose (Bind = Identität).</summary>
        public static Matrix4x4[] SkinMatrices(SkeletonDef sk, Vector3[] euler, Vector3 rootOffset)
        {
            var bindPos = sk.WorldPositions(null, Vector3.zero);
            var rot = new Quaternion[sk.Count];
            var pos = sk.WorldPositions(euler, rootOffset, rot);
            var m = new Matrix4x4[sk.Count];
            for (int i = 0; i < sk.Count; i++)
                m[i] = Matrix4x4.TRS(pos[i], rot[i], Vector3.one) * Matrix4x4.Translate(-bindPos[i]);
            return m;
        }

        /// <summary>Riss-Metrik: größtes Verhältnis Kantenlänge Pose/Bind über alle Dreieckskanten (≈1 = sauber, &gt;2,5 = sichtbarer Riss/Spike).</summary>
        public static float MaxEdgeStretch(Vector3[] bind, Vector3[] posed, int[] tris)
        {
            float worst = 1f;
            for (int t = 0; t + 2 < tris.Length; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    int i = tris[t + e], j = tris[t + (e + 1) % 3];
                    float lb = Vector3.Distance(bind[i], bind[j]);
                    if (lb < 0.01f) continue;
                    float r = Vector3.Distance(posed[i], posed[j]) / lb;
                    if (r > worst) worst = r;
                }
            return worst;
        }
    }
}
