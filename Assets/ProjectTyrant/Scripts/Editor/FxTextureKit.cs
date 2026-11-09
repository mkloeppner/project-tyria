using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M23: prozedural gezeichnete Effekt-Texturen (Glühen, Funke, Rauch, Ring, Siegel, Risse, Rauschen, Blatt, Flocke, Tropfen, Splitter, Glut, Strahl).</summary>
    public static class FxTextureKit
    {
        public const string Dir = ProcDir + "/Textures/FX";
        public static readonly string[] Names = { "Glow", "Spark", "Smoke", "Ring", "Sigil", "Crack", "Noise", "Leaf", "Flake", "Drop", "Shard", "Ember", "Beam" };

        public static Texture2D Get(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/FX_" + name + ".png");

        [MenuItem("Project Tyrant/Lauf 5/FX-Texturen zeichnen")]
        public static void BuildAll()
        {
            EnsureFolder(Dir);
            foreach (var n in Names) Save(n, Draw(n, n == "Noise" || n == "Beam" ? 128 : 64));
            AssetDatabase.Refresh();
            foreach (var n in Names)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(Dir + "/FX_" + n + ".png");
                if (imp == null) continue;
                imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.sRGBTexture = n != "Noise";
                imp.wrapMode = n == "Noise" || n == "Beam" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
        }

        static void Save(string n, Texture2D t) { System.IO.File.WriteAllBytes(Dir + "/FX_" + n + ".png", t.EncodeToPNG()); Object.DestroyImmediate(t); }

        static float Hash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 2147483647; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float ValueNoise(float x, float y, int seed, int period)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
            float a = Hash(((xi % period) + period) % period, ((yi % period) + period) % period, seed), b = Hash((((xi + 1) % period) + period) % period, ((yi % period) + period) % period, seed);
            float c = Hash(((xi % period) + period) % period, (((yi + 1) % period) + period) % period, seed), d = Hash((((xi + 1) % period) + period) % period, (((yi + 1) % period) + period) % period, seed);
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float Fbm(float u, float v, int seed, int baseFreq)
        {
            float s = 0f, amp = 0.5f; int f = baseFreq;
            for (int o = 0; o < 4; o++) { s += amp * ValueNoise(u * f, v * f, seed + o, f); amp *= 0.5f; f *= 2; }
            return s;
        }

        static Texture2D Draw(string n, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float dx = u * 2 - 1, dy = v * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float a = 0f, g = 1f;
                    switch (n)
                    {
                        case "Glow": a = Mathf.Pow(Mathf.Clamp01(1 - r), 2.2f); break;
                        case "Spark": a = Mathf.Clamp01(1 - Mathf.Abs(dy) * 6f) * Mathf.Clamp01(1 - Mathf.Abs(dx)) ; a = Mathf.Pow(a, 1.5f); break;
                        case "Smoke": { float nn = Fbm(u, v, 7, 4); a = Mathf.Clamp01(1 - r) * Mathf.Clamp01(nn * 1.6f - 0.25f); a = Mathf.Pow(a, 0.9f); g = 0.8f + 0.2f * nn; break; }
                        case "Ring": a = Mathf.Clamp01(1 - Mathf.Abs(r - 0.78f) * 9f) + 0.25f * Mathf.Clamp01(1 - Mathf.Abs(r - 0.78f) * 3f); break;
                        case "Sigil":
                        {
                            float ring1 = Mathf.Clamp01(1 - Mathf.Abs(r - 0.9f) * 22f), ring2 = Mathf.Clamp01(1 - Mathf.Abs(r - 0.62f) * 26f);
                            float spokes = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sin(ang * 3f)) * 14f) * (r > 0.62f && r < 0.9f ? 1f : 0f);
                            float tri = 0f; for (int k = 0; k < 3; k++) { float th = k * 2.094f + 1.5708f; float d = Mathf.Abs(dx * Mathf.Cos(th) + dy * Mathf.Sin(th) - 0.31f); tri = Mathf.Max(tri, Mathf.Clamp01(1 - d * 30f) * (r < 0.62f ? 1f : 0f)); }
                            float runes = (r > 0.68f && r < 0.84f) ? Mathf.Clamp01(Hash(Mathf.FloorToInt((ang + 3.1416f) * 6f), Mathf.FloorToInt(r * 20f), 3) * 2f - 1.2f) : 0f;
                            a = Mathf.Max(Mathf.Max(ring1, ring2), Mathf.Max(spokes, Mathf.Max(tri, runes))); break;
                        }
                        case "Crack": { float c = 0f; for (int k = 0; k < 7; k++) { float th = k * 0.9f + Hash(k, 1, 9) * 0.6f; float d = Mathf.Abs(Mathf.Sin(ang - th)) * r * 12f + Mathf.Abs(Mathf.Sin(r * 9f + k)) * 0.4f; c = Mathf.Max(c, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - r)); } a = c; break; }
                        case "Noise": { t.SetPixel(x, y, new Color(Fbm(u, v, 21, 4), Fbm(u + 0.37f, v + 0.11f, 33, 4), 0.5f, 1f)); continue; }
                        case "Leaf": { float lx = dx / 0.45f, ly = dy / 0.9f; float e = lx * lx + ly * ly; a = Mathf.Clamp01((1 - e) * 4f) * (Mathf.Abs(dx) < 0.03f ? 0.6f : 1f); g = 0.8f + 0.2f * v; break; }
                        case "Flake": { float arms = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sin(ang * 3f)) * 6f) * Mathf.Clamp01(1 - r); a = Mathf.Max(arms, Mathf.Clamp01(1 - r * 4f)); break; }
                        case "Drop": { float yy = dy + 0.2f; float e = dx * dx / 0.2f + (yy > 0 ? yy * yy / 0.6f : yy * yy / 0.2f); a = Mathf.Clamp01((1 - e) * 3f); break; }
                        case "Shard": { a = Mathf.Clamp01(1 - (Mathf.Abs(dx) * 2.4f + Mathf.Abs(dy) * 1.1f)) * 3f; a = Mathf.Clamp01(a); g = 0.75f + 0.25f * (dx + 1) * 0.5f; break; }
                        case "Ember": a = Mathf.Pow(Mathf.Clamp01(1 - r * 1.4f), 3f) + 0.4f * Mathf.Pow(Mathf.Clamp01(1 - r), 6f); break;
                        case "Beam": { float nn = Fbm(u, v, 5, 4); a = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(dy)), 2f) * (0.55f + 0.45f * nn); break; }
                    }
                    t.SetPixel(x, y, new Color(g, g, g, Mathf.Clamp01(a)));
                }
            t.Apply();
            return t;
        }

    }
}
