using System.Collections.Generic;
using System.IO;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Backt die Weltkarte (2048²) top-down aus Terrain-Farben, Schummerung, Wegen, Wasser und Gebäude-Footprints
    /// der Szene in eine handgemalt wirkende Pergament-Optik → Procedural/PT_Zone_Map.png.
    /// </summary>
    public static class MapBaker
    {
        public const string MapPath = PTEditorUtil.ProcDir + "/PT_Zone_Map.png";
        public const int Resolution = 2048;

        public static Texture2D Load() => AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);

        [MenuItem("Project Tyrant/Bake Map")]
        public static void BakeFromOpenScene()
        {
            var env = GameObject.Find("Environment");
            Bake(env != null ? env.transform : null);
        }

        public static Texture2D Bake(Transform environment) => BakeCore(new LegacyTerrain(), environment, MapPath, true);

        /// <summary>Lauf 4: Karte einer beliebigen Zone aus ihrem Gelände-Sampler.</summary>
        public static Texture2D BakeSampler(ITerrainSampler s, Transform environment, string path, float size) => BakeCore(s, environment, path, false);

        static Texture2D BakeCore(ITerrainSampler s, Transform environment, string path, bool legacyOutlines)
        {
            int n = Resolution;
            float size = s.Size, half = size * 0.5f, mpp = size / n, wl = s.WaterLevel;
            // Höhen-Cache (n+2)² mit Rand für Gradienten
            int m = n + 2;
            var hc = new float[m * m];
            for (int y = 0; y < m; y++)
            {
                float z = (y - 1) * mpp - half + mpp * 0.5f;
                for (int x = 0; x < m; x++) hc[y * m + x] = s.Height((x - 1) * mpp - half + mpp * 0.5f, z);
            }
            var px = new Color32[n * n];
            var light = new Vector3(-0.55f, 0.75f, 0.35f).normalized;
            var parchment = new Color(0.86f, 0.79f, 0.62f);
            Color water = new Color(0.28f, 0.47f, 0.58f), waterDeep = new Color(0.2f, 0.36f, 0.48f);
            var bt = s as BiomeTerrain;
            if (bt != null && bt.Profile.Ambient == AmbientKind.Embers) { water = new Color(0.95f, 0.45f, 0.12f); waterDeep = new Color(0.8f, 0.25f, 0.05f); }
            else if (bt != null && bt.Profile.Ambient == AmbientKind.Swamp) { water = new Color(0.3f, 0.42f, 0.38f); waterDeep = new Color(0.18f, 0.3f, 0.3f); }
            float contourStep = Mathf.Max(3f, size / 70f);
            for (int y = 0; y < n; y++)
            {
                float z = y * mpp - half + mpp * 0.5f;
                for (int x = 0; x < n; x++)
                {
                    float wx = x * mpp - half + mpp * 0.5f;
                    int ci = (y + 1) * m + (x + 1);
                    float h = hc[ci];
                    float hx = (hc[ci + 1] - hc[ci - 1]) / (2f * mpp) * 1.2f;
                    float hz = (hc[ci + m] - hc[ci - m]) / (2f * mpp) * 1.2f;
                    var normal = new Vector3(-hx / 1.2f, 1f, -hz / 1.2f).normalized;
                    float slope = 1f - normal.y;
                    Color c;
                    if (h < wl)
                    {
                        float depth = Mathf.Clamp01((wl - h) / 2.5f);
                        c = Color.Lerp(water, waterDeep, depth);
                        float shore = Mathf.Clamp01(1f - (wl - h) / 0.35f);
                        c = Color.Lerp(c, new Color(0.62f, 0.75f, 0.78f), shore * 0.6f);
                    }
                    else
                    {
                        c = s.GroundColor(wx, z, h, slope);
                        float shade = Mathf.Clamp01(Vector3.Dot(normal, light));
                        c *= Mathf.Lerp(0.72f, 1.12f, shade);
                        float contour = Mathf.Abs(Mathf.Repeat(h, contourStep) - contourStep * 0.5f);
                        if (contour > contourStep * 0.47f && h > wl + 1.5f) c *= 0.9f;
                    }
                    c = Color.Lerp(c, parchment, 0.22f);
                    float edge = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(z)) / half;
                    c = Color.Lerp(c, new Color(0.35f, 0.28f, 0.2f), Mathf.Clamp01((edge - 0.88f) / 0.12f) * 0.55f);
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            }
            if (environment != null) PaintFootprints(environment, px, n, mpp, half);
            if (legacyOutlines) PaintAreaOutlines(px, n, mpp, half);
            PaintInstancedVegetation(environment, px, n, mpp, half);

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false);
            PTEditorUtil.EnsureFolder(PTEditorUtil.ProcDir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear;
            imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.sRGBTexture = true;
            imp.SaveAndReimport();
            Debug.Log("[ProjectTyrant] Karte gebacken: " + path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Instanzierte Bäume (VegetationRenderer) als Kronen-Tupfen.</summary>
        static void PaintInstancedVegetation(Transform env, Color32[] px, int n, float mpp, float half)
        {
            if (env == null) return;
            var tree = new Color(0.22f, 0.36f, 0.18f);
            foreach (var vr in env.GetComponentsInChildren<VegetationRenderer>(true))
                foreach (var b in vr.Batches)
                {
                    if (b.Mesh == null || b.Mesh.bounds.size.y < 2.2f) continue;
                    float rad = Mathf.Clamp(Mathf.Max(b.Mesh.bounds.size.x, b.Mesh.bounds.size.z) * 0.5f, 0.8f, 4f);
                    foreach (var c in b.Cells) foreach (var mtx in c.Matrices)
                        Disc(px, n, mpp, half, new Vector2(mtx.m03, mtx.m23), Mathf.Max(rad, mpp * 1.2f), tree, 0.8f);
                }
        }

        /// <summary>Gebäude/Wände als dunkle Footprints, Bäume als Kronen-Tupfen (nach Renderer-Bounds).</summary>
        static void PaintFootprints(Transform env, Color32[] px, int n, float mpp, float half)
        {
            Color roof = new Color(0.45f, 0.27f, 0.2f), stone = new Color(0.42f, 0.4f, 0.38f), tree = new Color(0.22f, 0.36f, 0.18f);
            var seen = new HashSet<Renderer>();
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!seen.Add(r)) continue;
                string path = r.transform.name.ToLowerInvariant();
                var root = r.transform;
                bool veg = false, boundary = false;
                for (var t = r.transform; t != null; t = t.parent)
                {
                    string nm = t.name;
                    if (nm == "Vegetation") veg = true;
                    if (nm == "Boundary" || nm.StartsWith("Terrain") || nm.StartsWith("Water")) boundary = true;
                }
                if (boundary) continue;
                var b = r.bounds;
                if (b.size.x > 40f || b.size.z > 40f) continue;
                if (veg)
                {
                    if (b.size.y < 2.2f) continue;
                    float rad = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) * 0.5f, 0.8f, 4f);
                    Disc(px, n, mpp, half, new Vector2(b.center.x, b.center.z), rad, tree, 0.85f);
                    continue;
                }
                if (b.size.y < 0.9f) continue;
                Color col = path.Contains("roof") ? roof : stone;
                Rect(px, n, mpp, half, b, col, 0.8f);
            }
        }

        static void PaintAreaOutlines(Color32[] px, int n, float mpp, float half)
        {
            var ink = new Color(0.3f, 0.22f, 0.15f);
            foreach (var a in ZoneLayout.Areas)
            {
                int steps = 360;
                for (int i = 0; i < steps; i++)
                {
                    if ((i / 6) % 2 == 1) continue;   // gestrichelt
                    float ang = i * Mathf.PI * 2f / steps;
                    var p = a.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * a.Radius;
                    Disc(px, n, mpp, half, p, 0.35f, ink, 0.6f);
                }
            }
        }

        static void Blend(Color32[] px, int i, Color c, float a)
        {
            Color d = px[i];
            px[i] = new Color(Mathf.Lerp(d.r, c.r, a), Mathf.Lerp(d.g, c.g, a), Mathf.Lerp(d.b, c.b, a), 1f);
        }

        static void Disc(Color32[] px, int n, float mpp, float half, Vector2 c, float r, Color col, float alpha)
        {
            int x0 = Mathf.FloorToInt((c.x - r + half) / mpp), x1 = Mathf.CeilToInt((c.x + r + half) / mpp);
            int y0 = Mathf.FloorToInt((c.y - r + half) / mpp), y1 = Mathf.CeilToInt((c.y + r + half) / mpp);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(n - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(n - 1, x1); x++)
                {
                    float wx = x * mpp - half, wz = y * mpp - half;
                    float d = new Vector2(wx - c.x, wz - c.y).magnitude;
                    float a = Mathf.Clamp01((r - d) / mpp) * alpha;
                    if (a > 0f) Blend(px, y * n + x, col, a);
                }
        }

        static void Rect(Color32[] px, int n, float mpp, float half, Bounds b, Color col, float alpha)
        {
            int x0 = Mathf.FloorToInt((b.min.x + half) / mpp), x1 = Mathf.CeilToInt((b.max.x + half) / mpp);
            int y0 = Mathf.FloorToInt((b.min.z + half) / mpp), y1 = Mathf.CeilToInt((b.max.z + half) / mpp);
            var outline = col * 0.6f;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(n - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(n - 1, x1); x++)
                {
                    bool edge = x == x0 || x == x1 || y == y0 || y == y1;
                    Blend(px, y * n + x, edge ? outline : col, edge ? 0.95f : alpha);
                }
        }
    }
}
