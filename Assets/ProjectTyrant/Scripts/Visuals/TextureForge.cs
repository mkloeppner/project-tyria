using System;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1: prozedurale Texturen (rein, deterministisch, kachelbar). Perlin, Simplex-artiges Gradienten-Rauschen, Worley, Voronoi-Zellen,
    /// Ridged, Domain-Warp, Kaustiken, Flow-Maps, Rampen, Flipbooks (9 Formen), Licht-Cookies, Sternenfeld, Wolken. Ausgabe RGBA32, Hash für die Prüfung.
    /// </summary>
    public static class TextureForge
    {
        public const int MaxSize = 1024;

        // ---------------- Rauschen ----------------
        static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
        static float Rand(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777215f;
        static int Wrap(int v, int p) => p <= 0 ? v : ((v % p) + p) % p;
        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        static float Grad(int ix, int iy, float fx, float fy, int seed, int period)
        {
            float a = Rand(Wrap(ix, period), Wrap(iy, period), seed) * Mathf.PI * 2f;
            return Mathf.Cos(a) * fx + Mathf.Sin(a) * fy;
        }

        /// <summary>Kachelbares Gradienten-Rauschen (−1…1) mit Periode in Zellen.</summary>
        public static float Perlin(float x, float y, int seed, int period)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y); float fx = x - ix, fy = y - iy;
            float a = Grad(ix, iy, fx, fy, seed, period), b = Grad(ix + 1, iy, fx - 1, fy, seed, period);
            float c = Grad(ix, iy + 1, fx, fy - 1, seed, period), d = Grad(ix + 1, iy + 1, fx - 1, fy - 1, seed, period);
            float u = Fade(fx), v = Fade(fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) * 1.41f;
        }

        public static float Fbm(float x, float y, int seed, int period, int octaves)
        {
            float s = 0, amp = 0.5f, norm = 0; int p = period;
            for (int o = 0; o < Math.Max(1, octaves); o++) { s += Perlin(x, y, seed + o * 31, p) * amp; norm += amp; x *= 2; y *= 2; p *= 2; amp *= 0.5f; }
            return s / norm;
        }

        /// <summary>Worley: Abstand zum nächsten (f1) und zweitnächsten (f2) Zellpunkt, kachelbar.</summary>
        public static Vector2 Worley(float x, float y, int seed, int period, out float cellId)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float f1 = 9, f2 = 9; cellId = 0;
            for (int j = -1; j <= 1; j++) for (int i = -1; i <= 1; i++)
            {
                int cx = ix + i, cy = iy + j;
                int wx = Wrap(cx, period), wy = Wrap(cy, period);
                float px = cx + Rand(wx, wy, seed), py = cy + Rand(wx, wy, seed + 7);
                float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d < f1) { f2 = f1; f1 = d; cellId = Rand(wx, wy, seed + 13); } else if (d < f2) f2 = d;
            }
            return new Vector2(f1, f2);
        }

        // ---------------- Erzeugung ----------------
        public static Color32[] Generate(TextureKind kind, int size, int seed, float scale = 8f, int octaves = 4, int tiles = 4, FlipbookShape shape = FlipbookShape.Explosion, Gradient ramp = null)
        {
            size = Mathf.Clamp(Mathf.NextPowerOfTwo(Math.Max(4, size)), 4, MaxSize);
            var px = new Color32[size * size];
            int period = Math.Max(1, Mathf.RoundToInt(scale));
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float sx = u * period, sy = v * period;
                    Color c;
                    switch (kind)
                    {
                        case TextureKind.Perlin: { float n = Fbm(sx, sy, seed, period, 1) * 0.5f + 0.5f; c = new Color(n, n, n, 1); break; }
                        case TextureKind.Simplex: { float n = Fbm(sx, sy, seed, period, octaves) * 0.5f + 0.5f; c = new Color(n, n, n, 1); break; }
                        case TextureKind.Worley: { var w = Worley(sx, sy, seed, period, out _); float n = Mathf.Clamp01(w.x); c = new Color(n, n, n, 1); break; }
                        case TextureKind.Voronoi: { var w = Worley(sx, sy, seed, period, out float id); float edge = Mathf.Clamp01((w.y - w.x) * 4f); c = new Color(id, edge, 1 - Mathf.Clamp01(w.x), 1); break; }
                        case TextureKind.Ridged: { float n = 0, amp = 0.5f, f = 1; for (int o = 0; o < octaves; o++) { n += (1 - Mathf.Abs(Perlin(sx * f, sy * f, seed + o, period * (int)f))) * amp; f *= 2; amp *= 0.5f; } c = new Color(n, n, n, 1); break; }
                        case TextureKind.DomainWarp:
                        {
                            float qx = Fbm(sx, sy, seed, period, 3), qy = Fbm(sx + 5.2f, sy + 1.3f, seed + 3, period, 3);
                            float n = Fbm(sx + 2f * qx, sy + 2f * qy, seed + 9, period, octaves) * 0.5f + 0.5f; c = new Color(n, n * 0.9f, n * 0.8f, 1); break;
                        }
                        case TextureKind.Caustics:
                        {
                            var w1 = Worley(sx, sy, seed, period, out _); var w2 = Worley(sx * 1.7f + 0.3f, sy * 1.7f, seed + 5, period * 2 - 1 > 0 ? Mathf.RoundToInt(period * 1.7f) : 1, out _);
                            float n = Mathf.Pow(Mathf.Clamp01(1 - (w1.y - w1.x) * 3f), 3f) * 0.7f + Mathf.Pow(Mathf.Clamp01(1 - (w2.y - w2.x) * 3f), 3f) * 0.5f;
                            n = Mathf.Clamp01(n); c = new Color(n, n, n, 1); break;
                        }
                        case TextureKind.FlowMap:
                        {
                            float a = Fbm(sx, sy, seed, period, 2) * Mathf.PI * 2f; c = new Color(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f, 0.5f, 1); break;
                        }
                        case TextureKind.Ramp: { c = ramp != null ? ramp.Evaluate(u) : new Color(u, u, u, 1); c.a = 1; break; }
                        case TextureKind.Flipbook: { c = Flipbook(u, v, tiles, shape, seed); break; }
                        case TextureKind.CookieLeaves: { float n = Fbm(sx, sy, seed, period, 3); float l = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(n * 3f + 0.4f)); c = new Color(l, l, l, 1); break; }
                        case TextureKind.CookieGrid:
                        {
                            float gx = Mathf.Abs(Mathf.Repeat(u * period, 1f) - 0.5f), gy = Mathf.Abs(Mathf.Repeat(v * period, 1f) - 0.5f);
                            float l = (gx > 0.42f || gy > 0.42f) ? 0f : 1f; float vign = 1 - Mathf.Clamp01(((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 4f);
                            c = new Color(l * vign, l * vign, l * vign, 1); break;
                        }
                        case TextureKind.Starfield:
                        {
                            int cx = Mathf.FloorToInt(u * size / 4f), cy = Mathf.FloorToInt(v * size / 4f);
                            float r = Rand(cx, cy, seed); float star = r > 0.985f ? Mathf.Clamp01((r - 0.985f) / 0.015f) : 0f;
                            float lx = Mathf.Repeat(u * size / 4f, 1f) - 0.5f, ly = Mathf.Repeat(v * size / 4f, 1f) - 0.5f;
                            float d = Mathf.Clamp01(1f - Mathf.Sqrt(lx * lx + ly * ly) * 3f);
                            float b = star * d * d; float tint = Rand(cx, cy, seed + 1);
                            c = new Color(b * (0.8f + tint * 0.2f), b * 0.9f, b * (1f - tint * 0.15f), Rand(cx, cy, seed + 2)); break;
                        }
                        case TextureKind.Clouds:
                        {
                            float n1 = Fbm(sx, sy, seed, period, 3) * 0.5f + 0.5f, n2 = Fbm(sx * 2f, sy * 2f, seed + 11, period * 2, 3) * 0.5f + 0.5f;
                            c = new Color(n1, n2, Mathf.Clamp01(n1 * 0.7f + n2 * 0.3f), 1); break;
                        }
                        default: c = Color.magenta; break;
                    }
                    px[y * size + x] = c;
                }
            return px;
        }

        /// <summary>Flipbook-Kachel: Form je Element, Kachel-Index = Lebensphase (0 … tiles²−1).</summary>
        static Color Flipbook(float u, float v, int tiles, FlipbookShape shape, int seed)
        {
            tiles = Mathf.Clamp(tiles, 2, 8);
            int tx = Mathf.FloorToInt(u * tiles), ty = Mathf.FloorToInt(v * tiles);
            int frame = (tiles - 1 - ty) * tiles + tx; float t = frame / (float)(tiles * tiles - 1);
            float lx = Mathf.Repeat(u * tiles, 1f) * 2f - 1f, ly = Mathf.Repeat(v * tiles, 1f) * 2f - 1f;
            float r = Mathf.Sqrt(lx * lx + ly * ly), a = Mathf.Atan2(ly, lx);
            float n = Fbm(lx * 3f + frame * 0.37f, ly * 3f, seed, 0, 3);
            float alpha;
            switch (shape)
            {
                case FlipbookShape.Explosion: alpha = Mathf.Clamp01((0.25f + t * 0.7f + n * 0.25f - r) * 4f) * (1f - t * 0.8f); break;
                case FlipbookShape.Rauch: alpha = Mathf.Clamp01((0.35f + t * 0.5f + n * 0.35f - r) * 2.5f) * (1f - t); break;
                case FlipbookShape.Feuer: alpha = Mathf.Clamp01((0.5f - Mathf.Abs(lx) * (1.4f + ly) + n * 0.4f - (ly + 1f) * 0.15f * t) * 3f) * (1f - t * 0.6f); break;
                case FlipbookShape.Blitz: { float path = Mathf.Abs(lx - Mathf.Sin(ly * 6f + frame) * 0.25f - n * 0.3f); alpha = Mathf.Clamp01(1f - path * 12f) * (1f - t * 0.5f); break; }
                case FlipbookShape.Spritzer: { float drops = Mathf.Clamp01(1f - Mathf.Abs(r - (0.2f + t * 0.6f)) * 10f) * Mathf.Clamp01(Mathf.Sin(a * 9f + seed) * 2f); alpha = drops * (1f - t * 0.7f); break; }
                case FlipbookShape.Schneeflocke: { float arm = Mathf.Abs(Mathf.Sin(a * 3f + t * 1.2f)); alpha = Mathf.Clamp01((1f - r * 1.6f) * 3f) * Mathf.Clamp01((arm - 0.85f) * 10f + (1f - r) * 0.5f); break; }
                case FlipbookShape.Blatt: { float rot = t * 6.28f; float bx = lx * Mathf.Cos(rot) - ly * Mathf.Sin(rot), by = lx * Mathf.Sin(rot) + ly * Mathf.Cos(rot); alpha = Mathf.Clamp01((1f - (bx * bx * 4f + by * by * 1.4f)) * 4f); break; }
                case FlipbookShape.Funke: alpha = Mathf.Clamp01((1f - r * (2.5f + t * 2f)) * 2f) + Mathf.Clamp01(1f - Mathf.Abs(ly) * 20f) * Mathf.Clamp01(1f - Mathf.Abs(lx) * 1.5f) * (1f - t); break;
                default: { float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.6f) * 12f); float runes = Mathf.Clamp01(Mathf.Sin(a * 8f + t * 3f) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(r - 0.4f) * 10f); alpha = Mathf.Clamp01(ring + runes) * (1f - t * 0.5f); break; }
            }
            alpha = Mathf.Clamp01(alpha);
            float heat = Mathf.Clamp01(1f - r) * (1f - t);
            return new Color(1f, Mathf.Lerp(0.6f, 1f, heat), Mathf.Lerp(0.4f, 1f, heat), alpha);
        }

        /// <summary>FNV-1a-Hash über alle Pixel (Determinismus-Prüfung).</summary>
        public static string HashOf(Color32[] px)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (var c in px) { h = (h ^ c.r) * 16777619; h = (h ^ c.g) * 16777619; h = (h ^ c.b) * 16777619; h = (h ^ c.a) * 16777619; }
                return h.ToString("x8");
            }
        }
    }
}
