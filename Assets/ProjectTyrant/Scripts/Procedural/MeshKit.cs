using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectTyrant.Procedural
{
    /// <summary>Flat-shaded Low-Poly-Meshes mit Vertex-Farben (Bäume, Felsen, Dächer, Zelte).</summary>
    public class MeshKit
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Color> _c = new List<Color>();
        readonly List<int> _t = new List<int>();
        readonly List<Vector2> _uv = new List<Vector2>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color col)
        {
            int i = _v.Count;
            _v.Add(a); _v.Add(b); _v.Add(c);
            _c.Add(col); _c.Add(col); _c.Add(col);
            _uv.Add(new Vector2(a.x, a.z)); _uv.Add(new Vector2(b.x, b.z)); _uv.Add(new Vector2(c.x, c.z));
            _t.Add(i); _t.Add(i + 1); _t.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col) { Tri(a, b, c, col); Tri(a, c, d, col); }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (_v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(_v); m.SetColors(_c); m.SetTriangles(_t, 0); m.SetUVs(0, _uv);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Color Vary(Color c, System.Random r, float amt)
        {
            float k = 1f + ((float)r.NextDouble() - 0.5f) * 2f * amt;
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f);
        }

        /// <summary>Prisma (n Seiten) von y0 bis y1, Radius r0 unten → r1 oben.</summary>
        public void Prism(Vector3 basePos, int sides, float r0, float r1, float height, Color col, System.Random rnd = null, float jitter = 0f, bool cap = true)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 b0 = basePos + new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0);
                Vector3 b1 = basePos + new Vector3(Mathf.Cos(a1) * r0, 0, Mathf.Sin(a1) * r0);
                Vector3 t0 = basePos + new Vector3(Mathf.Cos(a0) * r1, height, Mathf.Sin(a0) * r1);
                Vector3 t1 = basePos + new Vector3(Mathf.Cos(a1) * r1, height, Mathf.Sin(a1) * r1);
                var cc = rnd != null ? Vary(col, rnd, jitter) : col;
                Quad(b0, t0, t1, b1, cc);
                if (cap && r1 > 0.001f) Tri(basePos + Vector3.up * height, t1, t0, cc);
            }
        }

        public void Cone(Vector3 basePos, int sides, float radius, float height, Color col, System.Random rnd = null, float jitter = 0f)
        {
            Vector3 tip = basePos + Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 b0 = basePos + new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                Vector3 b1 = basePos + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Tri(b0, tip, b1, rnd != null ? Vary(col, rnd, jitter) : col);
                Tri(basePos, b0, b1, col * 0.7f);
            }
        }

        /// <summary>Verrauschter Ikosaeder (eine Unterteilung) – Felsen und Baumkronen.</summary>
        public void NoisyIco(Vector3 center, Vector3 scale, float noise, Color col, System.Random rnd, float colorJitter = 0.08f, int subdiv = 1)
        {
            var verts = new List<Vector3>();
            var faces = new List<int[]>();
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] baseV = {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            foreach (var v in baseV) verts.Add(v.normalized);
            int[,] f = { {0,11,5},{0,5,1},{0,1,7},{0,7,10},{0,10,11},{1,5,9},{5,11,4},{11,10,2},{10,7,6},{7,1,8},
                         {3,9,4},{3,4,2},{3,2,6},{3,6,8},{3,8,9},{4,9,5},{2,4,11},{6,2,10},{8,6,7},{9,8,1} };
            for (int i = 0; i < 20; i++) faces.Add(new[] { f[i, 0], f[i, 1], f[i, 2] });
            for (int s = 0; s < subdiv; s++)
            {
                var cache = new Dictionary<long, int>();
                var nf = new List<int[]>();
                foreach (var fc in faces)
                {
                    int a = Mid(fc[0], fc[1], verts, cache), b = Mid(fc[1], fc[2], verts, cache), c = Mid(fc[2], fc[0], verts, cache);
                    nf.Add(new[] { fc[0], a, c }); nf.Add(new[] { fc[1], b, a }); nf.Add(new[] { fc[2], c, b }); nf.Add(new[] { a, b, c });
                }
                faces = nf;
            }
            var displaced = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                float k = 1f + ((float)rnd.NextDouble() - 0.5f) * 2f * noise;
                displaced[i] = center + Vector3.Scale(verts[i] * k, scale);
            }
            foreach (var fc in faces)
            {
                // Zuerst Winding aus Sicht der Außenseite
                Tri(displaced[fc[0]], displaced[fc[1]], displaced[fc[2]], Vary(col, rnd, colorJitter));
            }
        }

        static int Mid(int a, int b, List<Vector3> verts, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        }

        /// <summary>Satteldach (Prisma mit dreieckigem Querschnitt) – Länge entlang X.</summary>
        public void GableRoof(Vector3 center, float width, float length, float height, float overhang, Color col)
        {
            float hw = width * 0.5f + overhang, hl = length * 0.5f + overhang;
            Vector3 a = center + new Vector3(-hl, 0, -hw), b = center + new Vector3(hl, 0, -hw);
            Vector3 c = center + new Vector3(hl, 0, hw), d = center + new Vector3(-hl, 0, hw);
            Vector3 r0 = center + new Vector3(-hl, height, 0), r1 = center + new Vector3(hl, height, 0);
            Quad(a, r0, r1, b, col);
            Quad(c, r1, r0, d, col * 0.92f);
            Tri(d, r0, a, col * 0.8f);
            Tri(b, r1, c, col * 0.8f);
        }

        public static Mesh Tree(int seed, bool autumn)
        {
            var r = new System.Random(seed);
            var k = new MeshKit();
            float h = 2.2f + (float)r.NextDouble() * 1.6f;
            k.Prism(Vector3.zero, 6, 0.32f, 0.2f, h, Palette.Wood, r, 0.08f);
            Color leaf = autumn ? Palette.LeafAutumn : Palette.Leaf;
            int blobs = 2 + r.Next(2);
            for (int i = 0; i < blobs; i++)
            {
                float s = 1.6f + (float)r.NextDouble() * 0.9f - i * 0.25f;
                Vector3 c = new Vector3(((float)r.NextDouble() - 0.5f) * 0.9f, h + 0.6f + i * 1.0f, ((float)r.NextDouble() - 0.5f) * 0.9f);
                k.NoisyIco(c, new Vector3(s, s * 0.85f, s), 0.18f, leaf, r, 0.1f, 1);
            }
            return k.ToMesh("Tree_" + seed + (autumn ? "_autumn" : ""));
        }

        public static Mesh Pine(int seed)
        {
            var r = new System.Random(seed);
            var k = new MeshKit();
            float h = 1.2f + (float)r.NextDouble() * 0.6f;
            k.Prism(Vector3.zero, 5, 0.25f, 0.18f, h, Palette.Wood, r, 0.08f);
            Color c = new Color(0.22f, 0.48f, 0.32f);
            float y = h * 0.7f;
            for (int i = 0; i < 3; i++)
            {
                float rad = 1.9f - i * 0.5f;
                k.Cone(new Vector3(0, y, 0), 7, rad, 2.2f - i * 0.3f, c, r, 0.1f);
                y += 1.3f;
            }
            return k.ToMesh("Pine_" + seed);
        }

        public static Mesh Rock(int seed, Color col)
        {
            var r = new System.Random(seed);
            var k = new MeshKit();
            float s = 0.8f + (float)r.NextDouble() * 1.4f;
            k.NoisyIco(new Vector3(0, s * 0.35f, 0), new Vector3(s * 1.3f, s * 0.8f, s), 0.25f, col, r, 0.06f, 1);
            return k.ToMesh("Rock_" + seed);
        }

        public static Mesh Tent(Color col)
        {
            var k = new MeshKit();
            k.Cone(Vector3.zero, 6, 2.2f, 3.2f, col, new System.Random(3), 0.06f);
            return k.ToMesh("Tent");
        }

        public static Mesh Roof(float width, float length, float height, Color col)
        {
            var k = new MeshKit();
            k.GableRoof(Vector3.zero, width, length, height, 0.35f, col);
            return k.ToMesh("Roof");
        }
    }
}
