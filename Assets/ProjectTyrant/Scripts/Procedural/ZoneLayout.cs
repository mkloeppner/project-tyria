using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Procedural
{
    /// <summary>Layout der Zone „Grenzmark Ashveil“ (200×200 m) und deterministische Höhen-/Farbfunktion.</summary>
    public static class ZoneLayout
    {
        public const float Size = 200f;
        public const float Half = 100f;

        public struct Area { public string Name; public Vector2 Center; public float Radius; public float Height; public Color Ground; }

        public static readonly Area Hub = new Area { Name = "Weiler Sonnfurt", Center = new Vector2(-60, -60), Radius = 20, Height = 1.0f, Ground = Palette.Stone };
        public static readonly Area Outpost = new Area { Name = "Grenzposten Dornwacht", Center = new Vector2(5, 5), Radius = 17, Height = 1.5f, Ground = Palette.Dirt };
        public static readonly Area Camp = new Area { Name = "Rabenschanze", Center = new Vector2(58, 58), Radius = 17, Height = 2.5f, Ground = new Color(0.45f, 0.36f, 0.30f) };
        public static readonly Area Ruins = new Area { Name = "Ruinen von Elmhain", Center = new Vector2(58, -55), Radius = 15, Height = 2.0f, Ground = new Color(0.62f, 0.60f, 0.52f) };
        public static readonly Area Farm = new Area { Name = "Hof Gerstenbrunn", Center = new Vector2(-8, -60), Radius = 13, Height = 1.2f, Ground = new Color(0.72f, 0.62f, 0.36f) };
        public static readonly Vector2 Lake = new Vector2(-45, 40);
        public const float LakeRadius = 16f;
        public const float WaterLevel = -0.4f;

        public static readonly Area[] Areas = { Hub, Outpost, Camp, Ruins, Farm };

        /// <summary>Gebiete mit Namen und Gegnerlevel (GW2: Umland 1–3, Elmhain 5–8, Dornwacht 8–12).</summary>
        public struct Region { public string Name; public Vector2 Center; public float Radius; public int MinLevel, MaxLevel; }

        public static readonly Region[] Regions =
        {
            new Region { Name = "Weiler Sonnfurt", Center = new Vector2(-60, -60), Radius = 34, MinLevel = 1, MaxLevel = 3 },
            new Region { Name = "Hof Gerstenbrunn", Center = new Vector2(-8, -60), Radius = 24, MinLevel = 3, MaxLevel = 5 },
            new Region { Name = "Ruinen von Elmhain", Center = new Vector2(58, -55), Radius = 32, MinLevel = 5, MaxLevel = 8 },
            new Region { Name = "Grenzposten Dornwacht", Center = new Vector2(5, 5), Radius = 32, MinLevel = 8, MaxLevel = 12 },
            new Region { Name = "Rabenschanze", Center = new Vector2(58, 58), Radius = 30, MinLevel = 10, MaxLevel = 12 },
            new Region { Name = "Spiegelsee", Center = new Vector2(-45, 40), Radius = 30, MinLevel = 4, MaxLevel = 6 },
        };
        public const string WildernessName = "Grenzmark Ashveil";

        /// <summary>Gebiet an einer Weltposition (nächstes Gebiet innerhalb seines Radius), sonst -1 = Wildnis.</summary>
        public static int RegionIndexAt(Vector3 pos)
        {
            var p = new Vector2(pos.x, pos.z);
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < Regions.Length; i++)
            {
                float d = Vector2.Distance(p, Regions[i].Center);
                if (d <= Regions[i].Radius && d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public static string RegionNameAt(Vector3 pos) { int i = RegionIndexAt(pos); return i < 0 ? WildernessName : Regions[i].Name; }

        public static Vector2Int LevelRangeAt(Vector3 pos)
        {
            int i = RegionIndexAt(pos);
            if (i >= 0) return new Vector2Int(Regions[i].MinLevel, Regions[i].MaxLevel);
            // Wildnis: Level nach Entfernung vom Hub (2 → 9)
            float d = Vector2.Distance(new Vector2(pos.x, pos.z), Hub.Center);
            int l = Mathf.Clamp(2 + Mathf.RoundToInt(d / 22f), 2, 9);
            return new Vector2Int(l, l + 1);
        }

        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(-60, -60), new Vector2(-35, -35), new Vector2(-12, -12), new Vector2(5, 5) },
            new[] { new Vector2(5, 5), new Vector2(25, 30), new Vector2(42, 45), new Vector2(58, 58) },
            new[] { new Vector2(5, 5), new Vector2(30, -18), new Vector2(45, -40), new Vector2(58, -55) },
            new[] { new Vector2(-60, -60), new Vector2(-25, -72), new Vector2(20, -70), new Vector2(58, -55) },
        };
        public const float PathWidth = 3.2f;

        static float Smooth01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        public static float DistanceToPaths(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var path in Paths)
                for (int i = 0; i < path.Length - 1; i++)
                    best = Mathf.Min(best, DistToSegment(p, path[i], path[i + 1]));
            return best;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        public static float Height(float x, float z)
        {
            var p = new Vector2(x, z);
            float h = (Mathf.PerlinNoise(x * 0.018f + 11.3f, z * 0.018f + 7.7f) - 0.5f) * 9f
                    + (Mathf.PerlinNoise(x * 0.06f + 3.1f, z * 0.06f + 1.9f) - 0.5f) * 2.0f;
            float path = DistanceToPaths(p);
            h *= Mathf.Lerp(0.35f, 1f, Smooth01((path - PathWidth) / 10f));

            foreach (var a in Areas)
            {
                float d = Vector2.Distance(p, a.Center);
                float w = 1f - Smooth01((d - a.Radius) / 12f);
                h = Mathf.Lerp(h, a.Height, w);
            }

            float ld = Vector2.Distance(p, Lake);
            if (ld < LakeRadius + 8f) h = Mathf.Lerp(h, -2.2f, 1f - Smooth01((ld - LakeRadius * 0.6f) / (LakeRadius * 0.4f + 8f)));

            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge > 78f) h += Mathf.Pow(edge - 78f, 1.45f) * 0.75f + Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * (edge - 78f) * 0.3f;
            return h;
        }

        public static Color GroundColor(float x, float z, float height, float slope)
        {
            var p = new Vector2(x, z);
            float n = Mathf.PerlinNoise(x * 0.08f + 50f, z * 0.08f + 20f);
            Color c = Color.Lerp(Palette.Grass, Palette.GrassDark, Smooth01(n * 1.4f - 0.2f));
            float warm = Mathf.PerlinNoise(x * 0.03f + 90f, z * 0.03f);
            c = Color.Lerp(c, new Color(0.70f, 0.70f, 0.30f), Smooth01((warm - 0.6f) * 4f) * 0.5f);

            float path = DistanceToPaths(p);
            c = Color.Lerp(Palette.Dirt, c, Smooth01((path - PathWidth * 0.6f) / 1.6f));

            foreach (var a in Areas)
            {
                float d = Vector2.Distance(p, a.Center);
                float w = 1f - Smooth01((d - a.Radius * 0.75f) / 4f);
                c = Color.Lerp(c, a.Ground * (0.92f + n * 0.16f), w);
            }

            float ld = Vector2.Distance(p, Lake);
            if (ld < LakeRadius + 3f) c = Color.Lerp(c, new Color(0.78f, 0.70f, 0.50f), 1f - Smooth01((ld - LakeRadius + 2f) / 4f));

            if (slope > 0.45f || height > 9f) c = Color.Lerp(c, Palette.StoneDark, Smooth01(Mathf.Max((slope - 0.45f) * 4f, (height - 9f) / 6f)));
            if (height > 22f) c = Color.Lerp(c, new Color(0.92f, 0.92f, 0.98f), Smooth01((height - 22f) / 5f));
            return c;
        }

        public static bool IsFreeForScatter(float x, float z, float margin)
        {
            var p = new Vector2(x, z);
            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > 95f) return false;
            if (DistanceToPaths(p) < PathWidth + margin) return false;
            foreach (var a in Areas) if (Vector2.Distance(p, a.Center) < a.Radius + margin) return false;
            if (Vector2.Distance(p, Lake) < LakeRadius + margin) return false;
            foreach (var f in ZoneContent.Footprints()) if (Vector2.Distance(p, f.center) < f.radius + margin) return false;
            return true;
        }

        /// <summary>Flat-shaded Terrain-Mesh (Low-Poly), Zellgröße in Metern.</summary>
        public static Mesh BuildTerrainMesh(float cell = 2f)
        {
            int n = Mathf.RoundToInt(Size / cell);
            var k = new MeshKit();
            var hs = new float[n + 1, n + 1];
            for (int i = 0; i <= n; i++)
            for (int j = 0; j <= n; j++)
                hs[i, j] = Height(-Half + i * cell, -Half + j * cell);

            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                float x0 = -Half + i * cell, z0 = -Half + j * cell, x1 = x0 + cell, z1 = z0 + cell;
                Vector3 a = new Vector3(x0, hs[i, j], z0), b = new Vector3(x0, hs[i, j + 1], z1);
                Vector3 c = new Vector3(x1, hs[i + 1, j + 1], z1), d = new Vector3(x1, hs[i + 1, j], z0);
                bool flip = ((i + j) & 1) == 0;
                if (flip) { AddTri(k, a, b, c); AddTri(k, a, c, d); }
                else { AddTri(k, a, b, d); AddTri(k, b, c, d); }
            }
            var m = k.ToMesh("PT_ZoneTerrain");
            return m;
        }

        static void AddTri(MeshKit k, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            Vector3 center = (a + b + c) / 3f;
            float slope = 1f - Mathf.Abs(n.y);
            k.Tri(a, b, c, GroundColor(center.x, center.z, center.y, slope));
        }

        public static List<Vector3> PathPoints(int pathIndex, float spacing)
        {
            var list = new List<Vector3>();
            var path = Paths[pathIndex];
            for (int i = 0; i < path.Length - 1; i++)
            {
                float len = Vector2.Distance(path[i], path[i + 1]);
                int steps = Mathf.Max(1, Mathf.RoundToInt(len / spacing));
                for (int s = 0; s < steps; s++)
                {
                    var p = Vector2.Lerp(path[i], path[i + 1], s / (float)steps);
                    list.Add(new Vector3(p.x, Height(p.x, p.y), p.y));
                }
            }
            return list;
        }
    }
}
