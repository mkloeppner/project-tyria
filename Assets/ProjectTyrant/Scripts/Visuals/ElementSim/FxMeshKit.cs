using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>Mesh-Rohdaten (testbar ohne Unity-Mesh). UV.x läuft entlang der Form, UV.y quer bzw. nach oben (Shader nutzen das für Wachstum, Verlauf, Zacken).</summary>
    public sealed class FxMeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uv = new List<Vector2>();
        public readonly List<Color> Colors = new List<Color>();
        public readonly List<int> Triangles = new List<int>();

        public int Add(Vector3 p, Vector3 n, Vector2 uv, Color c) { Vertices.Add(p); Normals.Add(n); Uv.Add(uv); Colors.Add(c); return Vertices.Count - 1; }
        public void Tri(int a, int b, int c) { Triangles.Add(a); Triangles.Add(b); Triangles.Add(c); }
        public void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(a, c, d); }
        public int TriangleCount => Triangles.Count / 3;

        public Bounds Bounds
        {
            get
            {
                if (Vertices.Count == 0) return new Bounds();
                var b = new Bounds(Vertices[0], Vector3.zero); foreach (var v in Vertices) b.Encapsulate(v); return b;
            }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (Vertices.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(Vertices); m.SetNormals(Normals); m.SetUVs(0, Uv); m.SetColors(Colors); m.SetTriangles(Triangles, 0);
            m.RecalculateBounds(); return m;
        }

        /// <summary>Alle Dreiecks-Indizes gültig, keine entarteten Dreiecke (Fläche &gt; 0).</summary>
        public bool IsValid()
        {
            if (Triangles.Count % 3 != 0) return false;
            for (int i = 0; i < Triangles.Count; i += 3)
            {
                int a = Triangles[i], b = Triangles[i + 1], c = Triangles[i + 2];
                if (a < 0 || b < 0 || c < 0 || a >= Vertices.Count || b >= Vertices.Count || c >= Vertices.Count) return false;
                if (Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]).sqrMagnitude < 1e-12f) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// FxMeshKit 2.0 (V1): prozedurale Effekt-Formen. Alle Formen Low Poly, Größe 1 m (Effekte skalieren über den Transform),
    /// Animation über Shader-Parameter (Vertex-Verschiebung nach UV), damit ein gepooltes Mesh für alle Instanzen reicht.
    /// </summary>
    public static class FxMeshKit
    {
        public enum Shape { SplashCrown, WaveRing, WaterColumn, FlameTongue, FireWall, ShockDisc, LensSphere, Prism, LightCone, Vine, Shards, BladeArc }

        public static FxMeshData Build(Shape s, int seed = 1)
        {
            switch (s)
            {
                case Shape.SplashCrown: return SplashCrown(14, seed);
                case Shape.WaveRing: return WaveRing(32);
                case Shape.WaterColumn: return Column(12, 6, seed);
                case Shape.FlameTongue: return FlameTongue(8, 6, seed);
                case Shape.FireWall: return Strip(16, 4);
                case Shape.ShockDisc: return Disc(32, 3);
                case Shape.LensSphere: return Sphere(12, 8);
                case Shape.Prism: return Prism();
                case Shape.LightCone: return LightCone(16);
                case Shape.Vine: return Vine(24, 5, seed);
                case Shape.Shards: return Shards(14, seed);
                default: return BladeArc(20, 140f);
            }
        }

        /// <summary>Spritzkrone: Ring mit Zacken, UV.y = Höhe (Vertex-Animation hebt und weitet nach UV.y).</summary>
        public static FxMeshData SplashCrown(int spikes, int seed)
        {
            var m = new FxMeshData(); var rng = new SimRng(seed); int seg = spikes * 2;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = i % 2 == 0 ? 0.25f : 0.6f + rng.Next() * 0.4f;
                var n = (dir + Vector3.up * 0.3f).normalized;
                m.Add(dir * 0.35f, n, new Vector2(i / (float)seg, 0f), Color.white);
                m.Add(dir * (0.5f + h * 0.25f) + Vector3.up * h, n, new Vector2(i / (float)seg, 1f), new Color(1f, 1f, 1f, 0.6f));
            }
            for (int i = 0; i < seg; i++) m.Quad(i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
            return m;
        }

        /// <summary>Wellenring: flacher Torus-Streifen mit Höhenprofil; UV.y = Querschnitt (0 innen, 1 außen).</summary>
        public static FxMeshData WaveRing(int seg)
        {
            var m = new FxMeshData(); const int cross = 4;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int c = 0; c <= cross; c++)
                {
                    float t = c / (float)cross, r = Mathf.Lerp(0.8f, 1f, t), h = Mathf.Sin(t * Mathf.PI) * 0.12f;
                    m.Add(dir * r + Vector3.up * h, (Vector3.up + dir * Mathf.Cos(t * Mathf.PI) * 0.6f).normalized, new Vector2(i / (float)seg, t), Color.white);
                }
            }
            Grid(m, seg, cross); return m;
        }

        public static FxMeshData Column(int seg, int rings, int seed)
        {
            var m = new FxMeshData(); var rng = new SimRng(seed);
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings, rad = Mathf.Lerp(0.5f, 0.3f, t) * (0.9f + rng.Next() * 0.2f);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    m.Add(dir * rad + Vector3.up * t * 2f, dir, new Vector2(i / (float)seg, t), new Color(1f, 1f, 1f, 1f - t * 0.6f));
                }
            }
            for (int r = 0; r < rings; r++) for (int i = 0; i < seg; i++) { int a = r * (seg + 1) + i, b = a + seg + 1; m.Quad(a, b, b + 1, a + 1); }
            return m;
        }

        /// <summary>Flammenzunge: Kegel mit Rausch-Versatz, Spitze leicht gebogen; UV.y = Höhe (Shader: Rauschen × Höhe × Rampe).</summary>
        public static FxMeshData FlameTongue(int seg, int rings, int seed)
        {
            var m = new FxMeshData(); float bend = (new SimRng(seed).Next() - 0.5f) * 0.3f;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings, rad = 0.3f * (1f - t) * (1f - t * 0.2f) + 0.01f;
                var center = new Vector3(bend * t * t, t * 1.2f, 0f);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    float wob = 1f + (Noise.Value(i * 0.9f, r * 0.7f, seed) - 0.5f) * 0.4f;
                    m.Add(center + dir * rad * wob, dir, new Vector2(i / (float)seg, t), new Color(1f, 1f, 1f, 1f - t));
                }
            }
            for (int r = 0; r < rings; r++) for (int i = 0; i < seg; i++) { int a = r * (seg + 1) + i, b = a + seg + 1; m.Quad(a, b, b + 1, a + 1); }
            return m;
        }

        /// <summary>Streifen (Feuerwand, Flüssigkeits-Band): Breite 1 in x, Höhe 1; UV.x entlang.</summary>
        public static FxMeshData Strip(int seg, int rows)
        {
            var m = new FxMeshData();
            for (int i = 0; i <= seg; i++) for (int r = 0; r <= rows; r++)
                    m.Add(new Vector3(i / (float)seg - 0.5f, r / (float)rows, 0f), Vector3.back, new Vector2(i / (float)seg, r / (float)rows), Color.white);
            Grid(m, seg, rows); return m;
        }

        /// <summary>Schockwellen-Scheibe: Normalen kippen nach außen (Verzerrungs-Puffer liest sie), UV.y = Radius.</summary>
        public static FxMeshData Disc(int seg, int rings)
        {
            var m = new FxMeshData();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int r = 0; r <= rings; r++)
                {
                    float t = 0.15f + 0.85f * r / rings;
                    m.Add(dir * t, (Vector3.up + dir * Mathf.Sin(t * Mathf.PI)).normalized, new Vector2(i / (float)seg, t), Color.white);
                }
            }
            Grid(m, seg, rings); return m;
        }

        public static FxMeshData Sphere(int seg, int rings)
        {
            var m = new FxMeshData();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings, phi = v * Mathf.PI;
                for (int i = 0; i <= seg; i++)
                {
                    float u = i / (float)seg, th = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    m.Add(n * 0.5f, n, new Vector2(u, v), Color.white);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int i = 0; i < seg; i++)
                {
                    int a = r * (seg + 1) + i, b = a + seg + 1;
                    if (r > 0) m.Tri(a, a + 1, b);
                    if (r < rings - 1) m.Tri(a + 1, b + 1, b);
                }
            return m;
        }

        /// <summary>Prisma: dreiseitiger Keil, Dispersions-Austritt an der Rückseite (Farbe Alpha 1 = Austrittsfläche).</summary>
        public static FxMeshData Prism()
        {
            var m = new FxMeshData();
            var tri = new[] { new Vector3(0f, 0.5f, 0f), new Vector3(-0.43f, -0.25f, 0f), new Vector3(0.43f, -0.25f, 0f) };
            for (int s = 0; s < 3; s++)
            {
                var a = tri[s]; var b = tri[(s + 1) % 3]; var n = Vector3.Cross(b - a, Vector3.forward).normalized;
                var c = new Color(1f, 1f, 1f, s == 1 ? 1f : 0.3f);
                int i0 = m.Add(a + Vector3.back * 0.5f, n, new Vector2(0, 0), c), i1 = m.Add(b + Vector3.back * 0.5f, n, new Vector2(1, 0), c);
                int i2 = m.Add(b + Vector3.forward * 0.5f, n, new Vector2(1, 1), c), i3 = m.Add(a + Vector3.forward * 0.5f, n, new Vector2(0, 1), c);
                m.Quad(i0, i3, i2, i1);
            }
            for (int cap = 0; cap < 2; cap++)
            {
                float z = cap == 0 ? -0.5f : 0.5f; var n = cap == 0 ? Vector3.back : Vector3.forward;
                int a = m.Add(tri[0] + Vector3.forward * z, n, new Vector2(0.5f, 1f), Color.white), b = m.Add(tri[1] + Vector3.forward * z, n, Vector2.zero, Color.white), c = m.Add(tri[2] + Vector3.forward * z, n, Vector2.right, Color.white);
                if (cap == 0) m.Tri(a, c, b); else m.Tri(a, b, c);
            }
            return m;
        }

        /// <summary>Lichtkegel nach unten offen (Spitze oben bei y = 1); UV.y = Abstand zur Quelle (Streuung, Staub).</summary>
        public static FxMeshData LightCone(int seg)
        {
            var m = new FxMeshData(); int tip = m.Add(Vector3.up, Vector3.up, new Vector2(0.5f, 0f), Color.white);
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                m.Add(dir * 0.5f, dir, new Vector2(i / (float)seg, 1f), new Color(1f, 1f, 1f, 0f));
            }
            for (int i = 0; i < seg; i++) m.Tri(tip, 2 + i, 1 + i);
            return m;
        }

        /// <summary>Ranke: Spline (Spirale mit Rauschen), Röhre aus 4 Seiten, UV.x = Wachstum 0–1 (Shader schneidet ab).</summary>
        public static FxMeshData Vine(int seg, int sides, int seed)
        {
            var m = new FxMeshData(); var pts = new List<Vector3>();
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                pts.Add(new Vector3(Mathf.Cos(t * 7f) * 0.2f * (1f - t) + (Noise.Value(t * 4f, 0f, seed) - 0.5f) * 0.2f, t * 1.5f, Mathf.Sin(t * 7f) * 0.2f * (1f - t)));
            }
            for (int i = 0; i <= seg; i++)
            {
                var fwd = (pts[Mathf.Min(i + 1, seg)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(fwd, Vector3.forward).normalized; var up = Vector3.Cross(side, fwd);
                float rad = 0.04f * (1f - i / (float)seg) + 0.008f;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f; var n = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    m.Add(pts[i] + n * rad, n, new Vector2(i / (float)seg, s / (float)sides), Color.white);
                }
            }
            for (int i = 0; i < seg; i++) for (int s = 0; s < sides; s++) { int a = i * (sides + 1) + s, b = a + sides + 1; m.Quad(a, a + 1, b + 1, b); }
            return m;
        }

        /// <summary>Splitter: Voronoi-Bruch einer Scheibe in 8–24 Teile; Farbe.r = Teil-Index / Anzahl (Shader lässt Teile auseinanderfliegen).</summary>
        public static FxMeshData Shards(int count, int seed)
        {
            count = Mathf.Clamp(count, 8, 24);
            var m = new FxMeshData(); var rng = new SimRng(seed); var sites = new Vector2[count];
            for (int i = 0; i < count; i++) { float a = rng.Next() * Mathf.PI * 2f, r = Mathf.Sqrt(rng.Next()) * 0.45f; sites[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            const int rays = 24;
            for (int s = 0; s < count; s++)
            {
                var c = new Color(s / (float)count, rng.Next(), 0f, 1f);
                int center = m.Add(new Vector3(sites[s].x, 0f, sites[s].y), Vector3.up, sites[s] + Vector2.one * 0.5f, c);
                for (int k = 0; k <= rays; k++)
                {
                    float a = k / (float)rays * Mathf.PI * 2f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    float t = 0.5f;   // Randabstand: Schnitt mit Scheibe (r = 0,5) und den Bisektoren aller Nachbarn
                    float b = Vector2.Dot(sites[s], dir), cc = sites[s].sqrMagnitude - 0.25f; t = Mathf.Min(t, -b + Mathf.Sqrt(Mathf.Max(0f, b * b - cc)));
                    for (int o = 0; o < count; o++)
                    {
                        if (o == s) continue;
                        var dlt = sites[o] - sites[s]; float den = Vector2.Dot(dir, dlt);
                        if (den > 1e-5f) t = Mathf.Min(t, dlt.sqrMagnitude * 0.5f / den);
                    }
                    var p = sites[s] + dir * Mathf.Max(0.005f, t);
                    m.Add(new Vector3(p.x, 0f, p.y), Vector3.up, p + Vector2.one * 0.5f, c);
                }
                for (int k = 0; k < rays; k++) m.Tri(center, center + 2 + k, center + 1 + k);
            }
            return m;
        }

        /// <summary>Klingenbogen: Band entlang eines Kreisbogens, UV.x = Weg (Trail-Verlauf), UV.y = innen/außen.</summary>
        public static FxMeshData BladeArc(int seg, float degrees)
        {
            var m = new FxMeshData();
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg, a = Mathf.Deg2Rad * (-degrees * 0.5f + degrees * t); var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float w = Mathf.Sin(t * Mathf.PI) * 0.35f + 0.02f;
                m.Add(dir * (1f - w), Vector3.up, new Vector2(t, 0f), new Color(1f, 1f, 1f, t));
                m.Add(dir * 1f, Vector3.up, new Vector2(t, 1f), new Color(1f, 1f, 1f, t));
            }
            for (int i = 0; i < seg; i++) m.Quad(i * 2, i * 2 + 1, i * 2 + 3, i * 2 + 2);
            return m;
        }

        /// <summary>Blitzast als Band-Mesh aus Generator-Segmenten (kamera-unabhängig: zwei gekreuzte Bänder).</summary>
        public static FxMeshData Bolt(List<BoltGenerator.Segment> segs)
        {
            var m = new FxMeshData();
            foreach (var s in segs)
            {
                var d = (s.B - s.A); if (d.sqrMagnitude < 1e-8f) continue; d.Normalize();
                var p1 = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var p2 = Vector3.Cross(d, p1);
                foreach (var p in new[] { p1, p2 })
                {
                    var c = new Color(1f, 1f, 1f, 1f / (1 + s.Depth));
                    int a = m.Add(s.A - p * s.Width * 0.5f, p2, new Vector2(0, 0), c), b = m.Add(s.A + p * s.Width * 0.5f, p2, new Vector2(0, 1), c);
                    int e = m.Add(s.B + p * s.Width * 0.5f, p2, new Vector2(1, 1), c), f = m.Add(s.B - p * s.Width * 0.5f, p2, new Vector2(1, 0), c);
                    m.Quad(a, b, e, f);
                }
            }
            return m;
        }

        static void Grid(FxMeshData m, int seg, int cross)
        {
            for (int i = 0; i < seg; i++) for (int c = 0; c < cross; c++) { int a = i * (cross + 1) + c, b = a + cross + 1; m.Quad(a, a + 1, b + 1, b); }
        }
    }
}
