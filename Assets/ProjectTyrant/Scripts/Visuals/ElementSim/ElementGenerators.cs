using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    public enum SdfShape { Circle, Ring, Cone, Star, Blade, Glyph, Drop }

    /// <summary>Signed-Distance-Formen in 2D (negativ = innen), weiche Kante, Rausch-Verzerrung, Union/Subtraktion.</summary>
    public static class Sdf
    {
        public static float Circle(Vector2 p, float r) => p.magnitude - r;
        public static float Ring(Vector2 p, float r, float width) => Mathf.Abs(p.magnitude - r) - width * 0.5f;

        /// <summary>Kegel (Keil) mit Spitze im Ursprung, Öffnung nach +y.</summary>
        public static float Cone(Vector2 p, float halfAngleRad, float length)
        {
            float side = Mathf.Abs(p.x) * Mathf.Cos(halfAngleRad) - p.y * Mathf.Sin(halfAngleRad);
            return Mathf.Max(side, Mathf.Max(p.y - length, -p.y));   // angenäherte Distanz, exakt auf der Kante
        }

        public static float Star(Vector2 p, float r, int points, float inner)
        {
            float a = Mathf.Atan2(p.y, p.x), seg = Mathf.PI * 2f / points;
            float t = Mathf.Abs(Mathf.Repeat(a, seg) - seg * 0.5f) / (seg * 0.5f);   // 0 Spitze … 1 Mulde
            float rr = Mathf.Lerp(r, r * inner, 1f - t);
            return p.magnitude - rr;
        }

        /// <summary>Klingenbogen: Kreis minus versetzter Kreis (Sichel).</summary>
        public static float Blade(Vector2 p, float r, float thickness) => Subtract(Circle(p, r), Circle(p - new Vector2(0f, -thickness), r));

        /// <summary>Rune: Ring mit Kreuz-Strichen.</summary>
        public static float Glyph(Vector2 p, float r)
        {
            float ring = Ring(p, r, r * 0.12f);
            float bar1 = Mathf.Max(Mathf.Abs(p.x) - r * 0.05f, Mathf.Abs(p.y) - r * 0.8f);
            float bar2 = Mathf.Max(Mathf.Abs(p.y) - r * 0.05f, Mathf.Abs(p.x) - r * 0.8f);
            return Union(ring, Union(bar1, bar2));
        }

        /// <summary>Tropfen: Kreis unten, weich verbundene Spitze oben.</summary>
        public static float Drop(Vector2 p, float r)
        {
            float c = Circle(p, r);
            float tip = Cone(new Vector2(p.x, -(p.y - r * 2.2f)), 0.42f, r * 2.2f);
            return SmoothUnion(c, tip, r * 0.4f);
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);
        public static float Subtract(float a, float b) => Mathf.Max(a, -b);
        public static float SmoothUnion(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        /// <summary>Weiche Kante: 1 innen, 0 außen, Übergang über softness.</summary>
        public static float Edge(float d, float softness) => 1f - Mathf.Clamp01((d + softness * 0.5f) / Mathf.Max(1e-5f, softness));

        public static float Eval(SdfShape s, Vector2 p, float size)
        {
            switch (s)
            {
                case SdfShape.Ring: return Ring(p, size * 0.8f, size * 0.15f);
                case SdfShape.Cone: return Cone(p + new Vector2(0f, size), 0.45f, size * 1.8f);
                case SdfShape.Star: return Star(p, size, 5, 0.45f);
                case SdfShape.Blade: return Blade(p, size * 0.9f, size * 0.35f);
                case SdfShape.Glyph: return Glyph(p, size * 0.85f);
                case SdfShape.Drop: return Drop(p + new Vector2(0f, size * 0.5f), size * 0.45f);
                default: return Circle(p, size * 0.8f);
            }
        }

        /// <summary>Maske res × res (Mitte = Ursprung, Kante 1 = Bildrand), optional mit Rauschen verzerrt.</summary>
        public static float[] Mask(SdfShape s, int res, float softness = 0.04f, float warp = 0f, int seed = 1)
        {
            var m = new float[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    var p = new Vector2((x + 0.5f) / res * 2f - 1f, (y + 0.5f) / res * 2f - 1f);
                    if (warp > 0f) p += new Vector2(Noise.Value(p.x * 3f, p.y * 3f, seed) - 0.5f, Noise.Value(p.x * 3f + 17f, p.y * 3f, seed) - 0.5f) * warp;
                    m[x + y * res] = Edge(Eval(s, p, 1f), softness);
                }
            return m;
        }
    }

    public static class Noise
    {
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Wertrauschen 0–1, glatt interpoliert, deterministisch.</summary>
        public static float Value(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }

    /// <summary>
    /// Kaustik-Generator: gewellte Wasseroberfläche (Summe periodischer Wellen) bricht Licht auf einen Boden in Tiefe;
    /// die Lichtdichte am Boden wird durch Vorwärts-Splatten bestimmt. Wellenfrequenzen ganzzahlig in Raum und Zeit → nahtlose Kachel und nahtloser Loop.
    /// </summary>
    public static class CausticGenerator
    {
        public static float[] Frame(int res, float t01, int seed, float depth = 0.35f, float amplitude = 0.02f)
        {
            var rng = new SimRng(seed);
            const int waves = 5;
            var kx = new int[waves]; var ky = new int[waves]; var ph = new float[waves]; var sp = new int[waves];
            for (int w = 0; w < waves; w++)
            {
                kx[w] = 1 + (int)(rng.Next() * 4); ky[w] = (int)(rng.Next() * 5) - 2; ph[w] = rng.Next() * Mathf.PI * 2f; sp[w] = 1 + (int)(rng.Next() * 2);
                if (rng.Next() < 0.5f) kx[w] = -kx[w];
            }
            var dens = new float[res * res];
            int sub = 2; float cell = 1f / (res * sub), wgt = 1f / (sub * sub);
            for (int y = 0; y < res * sub; y++)
                for (int x = 0; x < res * sub; x++)
                {
                    float px = (x + 0.5f) * cell, py = (y + 0.5f) * cell, gx = 0f, gy = 0f;
                    for (int w = 0; w < waves; w++)
                    {
                        float arg = 2f * Mathf.PI * (kx[w] * px + ky[w] * py + sp[w] * t01) + ph[w];
                        float c = Mathf.Cos(arg) * amplitude * 2f * Mathf.PI / waves;
                        gx += c * kx[w]; gy += c * ky[w];
                    }
                    // Brechung ≈ Ablenkung proportional zum Oberflächengradienten
                    float qx = px - gx * depth, qy = py - gy * depth;
                    Splat(dens, res, qx, qy, wgt);
                }
            float max = 0f; for (int i = 0; i < dens.Length; i++) max = Mathf.Max(max, dens[i]);
            if (max > 0f) for (int i = 0; i < dens.Length; i++) dens[i] = Mathf.Clamp01(dens[i] / max * 1.4f);
            return dens;
        }

        static void Splat(float[] d, int res, float x, float y, float w)
        {
            float fx = Mathf.Repeat(x, 1f) * res - 0.5f, fy = Mathf.Repeat(y, 1f) * res - 0.5f;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy); float sx = fx - x0, sy = fy - y0;
            void Add(int xi, int yi, float a) { xi = ((xi % res) + res) % res; yi = ((yi % res) + res) % res; d[xi + yi * res] += a; }
            Add(x0, y0, w * (1 - sx) * (1 - sy)); Add(x0 + 1, y0, w * sx * (1 - sy)); Add(x0, y0 + 1, w * (1 - sx) * sy); Add(x0 + 1, y0 + 1, w * sx * sy);
        }
    }

    /// <summary>Blitz-Generator: Mittelpunkt-Verschiebung mit Verzweigungen (L-System-artig), Ast-Dicke nimmt je Ebene ab.</summary>
    public static class BoltGenerator
    {
        public struct Segment { public Vector3 A, B; public float Width; public int Depth; }

        public static List<Segment> Generate(int seed, Vector3 from, Vector3 to, int generations = 5, float jitter = 0.18f, float branchChance = 0.3f, float width = 0.12f, int frame = 0)
        {
            var rng = new SimRng(seed * 7919 + frame * 104729);
            var list = new List<Segment> { new Segment { A = from, B = to, Width = width, Depth = 0 } };
            float offset = (to - from).magnitude * jitter;
            for (int g = 0; g < generations; g++)
            {
                var next = new List<Segment>(list.Count * 2 + 4);
                foreach (var s in list)
                {
                    var dir = s.B - s.A; var mid = (s.A + s.B) * 0.5f;
                    var perp = Vector3.Cross(dir.normalized, Mathf.Abs(dir.normalized.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                    var perp2 = Vector3.Cross(dir.normalized, perp);
                    float o = offset / (1 + s.Depth);
                    mid += perp * rng.Range(-o, o) + perp2 * rng.Range(-o, o) * 0.5f;
                    next.Add(new Segment { A = s.A, B = mid, Width = s.Width, Depth = s.Depth });
                    next.Add(new Segment { A = mid, B = s.B, Width = s.Width, Depth = s.Depth });
                    if (s.Depth < 2 && rng.Next() < branchChance * (1f - g / (float)generations))
                    {
                        var bdir = Quaternion.AngleAxis(rng.Range(-35f, 35f), perp2) * (mid - s.A);
                        next.Add(new Segment { A = mid, B = mid + bdir * rng.Range(0.6f, 1.0f), Width = s.Width * 0.55f, Depth = s.Depth + 1 });
                    }
                }
                list = next; offset *= 0.55f;
            }
            return list;
        }
    }

    /// <summary>
    /// Bewegungs-Metrik für Bildstreifen (V1): mittlere Frame-Differenz, Richtungs-Kohärenz des optischen Flusses (Block-Matching),
    /// Anteil „toter“ Bilder ohne Änderung. Eingabe: Luminanz je Bild, Breite × Höhe.
    /// </summary>
    public static class MotionMetrics
    {
        public struct Result { public float MeanFrameDiff, Coherence, DeadShare; public int Frames; }
        public const float DeadThreshold = 0.002f;

        public static Result Measure(IList<float[]> frames, int width, int height)
        {
            var r = new Result { Frames = frames.Count };
            if (frames.Count < 2) return r;
            float diffSum = 0f; int dead = 0; Vector2 flowSum = Vector2.zero; float magSum = 0f;
            for (int f = 1; f < frames.Count; f++)
            {
                float d = 0f; var a = frames[f - 1]; var b = frames[f];
                for (int i = 0; i < a.Length; i++) d += Mathf.Abs(a[i] - b[i]);
                d /= a.Length; diffSum += d; if (d < DeadThreshold) dead++;
                foreach (var v in BlockFlow(a, b, width, height)) { flowSum += v; magSum += v.magnitude; }
            }
            r.MeanFrameDiff = diffSum / (frames.Count - 1);
            r.DeadShare = dead / (float)(frames.Count - 1);
            r.Coherence = magSum <= 1e-5f ? 0f : flowSum.magnitude / magSum;
            return r;
        }

        /// <summary>Fluss je 8×8-Block mit Kontrast, Suche ±3 Pixel (SAD).</summary>
        public static List<Vector2> BlockFlow(float[] a, float[] b, int w, int h, int block = 8, int search = 3)
        {
            var list = new List<Vector2>();
            for (int by = search; by + block + search <= h; by += block)
                for (int bx = search; bx + block + search <= w; bx += block)
                {
                    float mean = 0f, var = 0f;
                    for (int y = 0; y < block; y++) for (int x = 0; x < block; x++) mean += a[bx + x + (by + y) * w];
                    mean /= block * block;
                    for (int y = 0; y < block; y++) for (int x = 0; x < block; x++) { float q = a[bx + x + (by + y) * w] - mean; var += q * q; }
                    if (var / (block * block) < 1e-4f) continue;
                    float best = float.MaxValue, zero = 0f; int bdx = 0, bdy = 0;
                    for (int dy = -search; dy <= search; dy++)
                        for (int dx = -search; dx <= search; dx++)
                        {
                            float sad = 0f;
                            for (int y = 0; y < block; y++) for (int x = 0; x < block; x++)
                                sad += Mathf.Abs(a[bx + x + (by + y) * w] - b[bx + x + dx + (by + y + dy) * w]);
                            if (dx == 0 && dy == 0) zero = sad;
                            if (sad < best - 1e-6f || (Mathf.Abs(sad - best) <= 1e-6f && dx * dx + dy * dy < bdx * bdx + bdy * bdy)) { best = sad; bdx = dx; bdy = dy; }
                        }
                    if (best < zero - 1e-6f) list.Add(new Vector2(bdx, bdy));
                }
            return list;
        }
    }
}
