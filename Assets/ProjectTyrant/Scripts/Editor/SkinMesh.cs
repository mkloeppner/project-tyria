using System.Collections.Generic;
using ProjectTyrant.Anim;
using SkinWeights = ProjectTyrant.Anim.SkinWeights;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Baukasten für geskinnte MeshKit-Charaktere: Röhren, Ellipsoide, Kästen, Flächen in Bind-Pose (Weltkoordinaten relativ zur Wurzel),
    /// je Vertex Farbe, bevorzugter Knochen (Hint), optional starre Bindung oder erlaubte Knochen. Untermeshes nach Material-Index.
    /// </summary>
    public class SkinMesh
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<int> Hint = new List<int>(), Rigid = new List<int>();
        public readonly List<int[]> Allowed = new List<int[]>();
        public readonly List<List<int>> Sub = new List<List<int>> { new List<int>() };
        int _sub;
        public int[] AllowedNow;   // gilt für neu erzeugte Vertices
        public void UseSub(int i) { while (Sub.Count <= i) Sub.Add(new List<int>()); _sub = i; }

        int AddV(Vector3 p, Color c, int hint, int rigid)
        {
            V.Add(p); C.Add(c); Hint.Add(hint); Rigid.Add(rigid); Allowed.Add(AllowedNow);
            return V.Count - 1;
        }
        void T(int a, int b, int c) { var l = Sub[_sub]; l.Add(a); l.Add(b); l.Add(c); }

        static void Basis(Vector3 dir, out Vector3 side, out Vector3 front)
        {
            Vector3 r = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.forward)) > 0.9f ? Vector3.up : Vector3.forward;
            side = Vector3.Cross(dir, r).normalized; if (side.sqrMagnitude < 0.5f) side = Vector3.right;
            front = Vector3.Cross(side, dir).normalized;
            if (Vector3.Dot(side, Vector3.right) < 0f && Mathf.Abs(Vector3.Dot(side, Vector3.right)) > 0.5f) side = -side;
        }

        /// <summary>Röhre von a nach b mit elliptischem Querschnitt (rx seitlich, rz vorne bzw. oben), Ringe mit leichter Wölbung.</summary>
        public void Tube(Vector3 a, Vector3 b, Vector2 ra, Vector2 rb, Color col, int hint, int rigid = -1, int sides = 8, int rings = 3, float bulge = 0.08f, bool caps = true, Vector3 frontBias = default)
        {
            Vector3 dir = b - a; float len = dir.magnitude; if (len < 1e-4f) return;
            Basis(dir, out var side, out var front);
            if (frontBias != default) front = (front + frontBias).normalized;
            int start = V.Count;
            for (int r = 0; r <= rings; r++)
            {
                float u = r / (float)rings;
                Vector2 rad = Vector2.Lerp(ra, rb, u) * (1f + bulge * Mathf.Sin(u * Mathf.PI));
                Vector3 c = a + dir * u;
                for (int s = 0; s < sides; s++)
                {
                    float ang = s / (float)sides * Mathf.PI * 2f;
                    AddV(c + side * Mathf.Cos(ang) * rad.x + front * Mathf.Sin(ang) * rad.y, col, hint, rigid);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int i0 = start + r * sides + s, i1 = start + r * sides + (s + 1) % sides, j0 = i0 + sides, j1 = i1 + sides;
                    T(i0, j0, j1); T(i0, j1, i1);
                }
            if (caps)
            {
                int ca = AddV(a - dir.normalized * Mathf.Min(ra.x, ra.y) * 0.4f, col, hint, rigid);
                int cb = AddV(b + dir.normalized * Mathf.Min(rb.x, rb.y) * 0.4f, col, hint, rigid);
                int last = start + rings * sides;
                for (int s = 0; s < sides; s++)
                {
                    T(ca, start + (s + 1) % sides, start + s);
                    T(cb, last + s, last + (s + 1) % sides);
                }
            }
        }

        public void Ellipsoid(Vector3 center, Vector3 radii, Color col, int hint, int rigid = -1, int lat = 6, int lon = 10, Quaternion? rot = null)
        {
            var q = rot ?? Quaternion.identity;
            int start = V.Count;
            for (int i = 0; i <= lat; i++)
            {
                float th = i / (float)lat * Mathf.PI;
                for (int j = 0; j < lon; j++)
                {
                    float ph = j / (float)lon * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * radii.x, Mathf.Cos(th) * radii.y, Mathf.Sin(th) * Mathf.Sin(ph) * radii.z);
                    AddV(center + q * d, col, hint, rigid);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = start + i * lon + j, b = start + i * lon + (j + 1) % lon, c = a + lon, d = b + lon;
                    T(a, b, d); T(a, d, c);
                }
        }

        public void Box(Vector3 center, Vector3 size, Color col, int hint, int rigid = -1, Quaternion? rot = null)
        {
            var q = rot ?? Quaternion.identity; var h = size * 0.5f;
            Vector3 P(float x, float y, float z) => center + q * new Vector3(x * h.x, y * h.y, z * h.z);
            void F(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int i = AddV(a, col, hint, rigid), j = AddV(b, col, hint, rigid), k = AddV(c, col, hint, rigid), l = AddV(d, col, hint, rigid); T(i, j, k); T(i, k, l); }
            F(P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), P(1, -1, 1));
            F(P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), P(-1, -1, -1));
            F(P(-1, 1, 1), P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1));
            F(P(-1, -1, -1), P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1));
            F(P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), P(1, -1, -1));
            F(P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1));
        }

        /// <summary>Kegel (Hörner, Stacheln) von der Basis entlang dir.</summary>
        public void Cone(Vector3 basePos, Vector3 dir, float radius, Color col, int hint, int rigid = -1, int sides = 6)
        {
            Basis(dir, out var side, out var front);
            int tip = AddV(basePos + dir, col, hint, rigid); int start = V.Count;
            for (int s = 0; s < sides; s++) { float a = s / (float)sides * Mathf.PI * 2f; AddV(basePos + side * Mathf.Cos(a) * radius + front * Mathf.Sin(a) * radius, col, hint, rigid); }
            int center = AddV(basePos, col, hint, rigid);
            for (int s = 0; s < sides; s++) { T(tip, start + (s + 1) % sides, start + s); T(center, start + s, start + (s + 1) % sides); }
        }

        /// <summary>Doppelseitige Fläche (Umhang, Flügelhaut) als Gitter: Ecke + u-Achse + v-Achse; rigidRow liefert den starren Knochen je Zeile.</summary>
        public void Sheet(Vector3 origin, Vector3 uAxis, Vector3 vAxis, int nu, int nv, Color col, System.Func<int, int> rigidRow, float curve = 0f, Vector3 curveDir = default)
        {
            // Vorder- und Rückseite mit getrennten Vertices (sonst heben sich die Normalen auf → schwarze Flächen)
            int w = nu + 1;
            Vector3 n = Vector3.Cross(uAxis, vAxis).normalized * 0.004f;
            for (int side = 0; side < 2; side++)
            {
                int start = V.Count;
                for (int j = 0; j <= nv; j++)
                    for (int i = 0; i <= nu; i++)
                    {
                        float u = i / (float)nu, v = j / (float)nv;
                        var p = origin + uAxis * u + vAxis * v + curveDir * (curve * Mathf.Sin(u * Mathf.PI)) + (side == 0 ? n : -n);
                        int rb = rigidRow(j); AddV(p, col, rb, rb);
                    }
                for (int j = 0; j < nv; j++)
                    for (int i = 0; i < nu; i++)
                    {
                        int a = start + j * w + i, b = a + 1, c = a + w, d = c + 1;
                        if (side == 0) { T(a, c, d); T(a, d, b); } else { T(a, d, c); T(a, b, d); }
                    }
            }
        }

        public void AddMesh(Mesh m, Matrix4x4 toRoot, Color tint, int subOffset = 0)
        {
            var v = m.vertices; var cols = m.colors;
            int start = V.Count;
            for (int i = 0; i < v.Length; i++) AddV(toRoot.MultiplyPoint3x4(v[i]), cols != null && cols.Length == v.Length ? cols[i] * tint : tint, -1, -1);
            for (int s = 0; s < m.subMeshCount; s++)
            {
                UseSub(subOffset + s);
                var t = m.GetTriangles(s);
                for (int i = 0; i + 2 < t.Length; i += 3) T(start + t[i], start + t[i + 1], start + t[i + 2]);
            }
            UseSub(0);
        }

        /// <summary>Gelenk-Nachbarschaft: Hint-Knochen + Eltern + Kinder (keine Fernwirkung, z. B. Hand → Oberschenkel).</summary>
        static readonly Dictionary<long, int[]> _adj = new Dictionary<long, int[]>();
        public static int[] Adjacent(SkeletonDef sk, int bone)
        {
            if (bone < 0) return null;
            long key = ((long)sk.GetHashCode() << 8) | (uint)bone;
            if (_adj.TryGetValue(key, out var r)) return r;
            var l = new List<int> { bone };
            int p = sk.Bones[bone].Parent; if (p >= 0) l.Add(p);
            for (int c = 0; c < sk.Count; c++) if (sk.Bones[c].Parent == bone) l.Add(c);
            r = l.ToArray(); _adj[key] = r; return r;
        }

        public int[] AllTriangles() { var r = new List<int>(); foreach (var s in Sub) r.AddRange(s); return r.ToArray(); }

        /// <summary>Gewichte: starr → 1; sonst Knochennähe mit Hint-Bonus, eingeschränkt auf erlaubte Knochen; danach Laplace-Glättung.</summary>
        public SkinWeights.Influence[] Weights(SkeletonDef sk, int smoothIterations = 2)
        {
            SkinWeights.Segments(sk, out var a, out var b);
            var inf = new SkinWeights.Influence[V.Count];
            var locked = new bool[V.Count];
            for (int i = 0; i < V.Count; i++)
            {
                if (Rigid[i] >= 0) { inf[i] = new SkinWeights.Influence { B0 = Rigid[i], W0 = 1f }; locked[i] = true; continue; }
                var allowed = Allowed[i] ?? Adjacent(sk, Hint[i]);
                if (allowed != null)
                {
                    var aa = new Vector3[allowed.Length]; var bb = new Vector3[allowed.Length]; int pref = -1;
                    for (int k = 0; k < allowed.Length; k++) { aa[k] = a[allowed[k]]; bb[k] = b[allowed[k]]; if (allowed[k] == Hint[i]) pref = k; }
                    var f = SkinWeights.Compute(V[i], aa, bb, pref);
                    inf[i] = new SkinWeights.Influence { B0 = allowed[f.B0], B1 = allowed[f.B1], B2 = allowed[f.B2], B3 = allowed[f.B3], W0 = f.W0, W1 = f.W1, W2 = f.W2, W3 = f.W3 };
                }
                else inf[i] = SkinWeights.Compute(V[i], a, b, Hint[i]);
            }
            return smoothIterations > 0 ? SkinWeights.Smooth(inf, AllTriangles(), sk.Count, smoothIterations, 0.5f, locked) : inf;
        }
    }
}
