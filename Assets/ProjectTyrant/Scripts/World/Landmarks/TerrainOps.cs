using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Welt-Lauf W1: Terrain-Operationen (rein, deterministisch). Jede Operation verändert nur ihren Radius; mehrere werden der Reihe nach angewandt.
    /// Die Werte sind Meter über bzw. unter dem Grund an der Stelle. Wasserspiegel/Lava-Spiegel kommen vom Biom.
    /// </summary>
    public static class TerrainOps
    {
        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        static float Hash(float x, float z) { float s = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f; return s - Mathf.Floor(s); }

        /// <summary>Lokale Achsen der Operation (gedreht um Yaw).</summary>
        static Vector2 Local(TerrainOpDef op, float x, float z)
        {
            float a = -op.Yaw * Mathf.Deg2Rad, dx = x - op.Center.x, dz = z - op.Center.y;
            return new Vector2(dx * Mathf.Cos(a) - dz * Mathf.Sin(a), dx * Mathf.Sin(a) + dz * Mathf.Cos(a));
        }

        public static bool Affects(TerrainOpDef op, float x, float z)
        {
            float r = op.Radius * (op.Kind == TerrainOpKind.Canyon || op.Kind == TerrainOpKind.Delta ? 1.1f : 1.25f) + 12f;
            return (new Vector2(x, z) - op.Center).sqrMagnitude <= r * r;
        }

        public static float Apply(TerrainOpDef op, float x, float z, float h, float waterLevel)
        {
            if (!Affects(op, x, z)) return h;
            var l = Local(op, x, z);
            float d = l.magnitude, r = Mathf.Max(1f, op.Radius);
            switch (op.Kind)
            {
                case TerrainOpKind.Massif:
                {
                    // Gebirgsblock: Grate + zwei Plateaus (35 % / 65 %) – Hänge über 45°, Plateaus eben
                    float t = 1f - d / r; if (t <= 0f) return h;
                    float ridge = 1f + 0.18f * Mathf.Sin(Mathf.Atan2(l.y, l.x) * 5f) * Mathf.Clamp01(t * 2f);
                    float m = Mathf.Pow(Smooth(t * ridge), 1.2f) * op.Height;
                    float step = op.Height / 3f, k = m / step, fl = Mathf.Floor(k), fr = k - fl;
                    m = (fl + Smooth((fr - 0.55f) / 0.45f)) * step;
                    return Mathf.Max(h, h * (1f - Smooth(t * 3f)) + m);
                }
                case TerrainOpKind.Peak:
                {
                    // Gipfel: steiler Kegel mit kleiner Gipfelfläche (3 % des Radius)
                    float t = 1f - d / r; if (t <= 0f) return h;
                    float m = Mathf.Pow(Mathf.Clamp01(t / 0.97f), 1.6f) * op.Height;
                    return Mathf.Max(h, m + h * (1f - Smooth(t * 4f)));
                }
                case TerrainOpKind.Mesa:
                {
                    float edge = Mathf.Max(2.5f, op.Param);
                    if (d > r + edge) return h;
                    float w = Smooth((r + edge - d) / edge);
                    return Mathf.Max(h, Mathf.Lerp(h, op.Height, w));   // Height = absolute Plateauhöhe
                }
                case TerrainOpKind.Canyon:
                {
                    // Schlucht entlang der lokalen X-Achse: Länge 2·Radius, Breite Param, Tiefe Height; steile Wände
                    float half = Mathf.Max(4f, op.Param * 0.5f);
                    if (Mathf.Abs(l.x) > r) return h;
                    float endFade = Smooth((r - Mathf.Abs(l.x)) / Mathf.Min(40f, r * 0.3f));
                    float across = Mathf.Abs(l.y);
                    if (across > half + 6f) return h;
                    float w = Smooth((half + 6f - across) / 6f) * endFade;
                    return Mathf.Lerp(h, Mathf.Min(h, waterLevel + 0.5f - op.Height * 0.15f) - op.Height * 0.85f * endFade, w);
                }
                case TerrainOpKind.Crater:
                {
                    // Kraterrand (Height über Grund) bei 0,75 r, Innenbecken bis Param unter dem Spiegel
                    float rim = r * 0.75f;
                    if (d > r * 1.25f) return h;
                    if (d >= rim) return Mathf.Max(h, h + op.Height * Smooth((r * 1.25f - d) / (r * 0.5f)));
                    float inner = Smooth((rim - d) / (rim * 0.35f));
                    return Mathf.Lerp(h + op.Height, waterLevel - op.Param, inner);
                }
                case TerrainOpKind.CliffCoast:
                {
                    // Terrassen zum Meer (lokale +X Richtung Meer): 4 Stufen à Height/4, unten unter dem Spiegel
                    if (d > r) return h;
                    float u = Mathf.Clamp01((l.x + r) / (2f * r));
                    float steps = 4f, k = Mathf.Floor((1f - u) * steps);
                    float target = waterLevel - 2f + k / steps * op.Height;
                    float w = Smooth((r - d) / (r * 0.25f));
                    return Mathf.Lerp(h, target, w);
                }
                case TerrainOpKind.Basin:
                {
                    // Senke (Treibsand/Giftwasser/Eiswasser): Boden Param unter dem Spiegel, 10 m weicher Rand
                    if (d > r + 10f) return h;
                    float w = Smooth((r + 10f - d) / 10f);
                    return Mathf.Lerp(h, Mathf.Min(h, waterLevel - Mathf.Max(0.6f, op.Param)), w);
                }
                case TerrainOpKind.Cave:
                {
                    // Höhle: hufeisenförmiger Felsring (Height) mit ebenem Inneren (0,4 r) und Eingang in lokaler +Z-Richtung (±28°); Decke = Bauwerk
                    if (d > r) return h;
                    float inner = r * 0.4f;
                    float ang = Mathf.Abs(Mathf.Atan2(l.x, l.y)) * Mathf.Rad2Deg;
                    float gap = Smooth((ang - 28f) / 10f);
                    float ring = Smooth((d - inner) / 3f) * Smooth((r - d) / (r * 0.35f));
                    return h + op.Height * ring * gap;
                }
                case TerrainOpKind.FloatingIsland:
                {
                    // Insel ist ein Bauwerk; darunter der Abrisskrater, aus dem sie einst gerissen wurde (flache Mulde, Tiefe 15 % der Höhe)
                    if (d > r) return h;
                    return h - op.Height * 0.15f * Smooth((r - d) / (r * 0.5f));
                }
                case TerrainOpKind.Terraces:
                {
                    // Stufenfelder: Ringe à Param Höhe bis Height
                    if (d > r) return h;
                    float stepH = Mathf.Max(0.5f, op.Param);
                    float lvl = Mathf.Floor((1f - d / r) * op.Height / stepH) * stepH;
                    return Mathf.Max(h, h + lvl * Smooth((r - d) / 8f));
                }
                case TerrainOpKind.Delta:
                {
                    // Flussdelta: drei Arme (lokale X), dazwischen Inseln
                    if (Mathf.Abs(l.x) > r || Mathf.Abs(l.y) > r) return h;
                    float arms = Mathf.Abs(Mathf.Sin(l.y / r * Mathf.PI * 1.5f + l.x * 0.01f));
                    float ch = Smooth((0.35f - arms) / 0.2f) * Smooth((r - Mathf.Abs(l.x)) / 30f);
                    return Mathf.Lerp(h, Mathf.Min(h, waterLevel - Mathf.Max(1.5f, op.Param)), ch);
                }
            }
            return h;
        }

        public static float ApplyAll(IList<TerrainOpDef> ops, float x, float z, float h, float waterLevel)
        {
            if (ops == null) return h;
            for (int i = 0; i < ops.Count; i++) h = Apply(ops[i], x, z, h, waterLevel);
            return h;
        }

        /// <summary>Prüfsumme über ein Raster (Determinismus-Test).</summary>
        public static long Hash(IList<TerrainOpDef> ops, System.Func<float, float, float> ground, float waterLevel, float size, float cell)
        {
            long acc = 17;
            for (float x = -size * 0.5f; x < size * 0.5f; x += cell)
                for (float z = -size * 0.5f; z < size * 0.5f; z += cell)
                    acc = acc * 31 + Mathf.RoundToInt(ApplyAll(ops, x, z, ground(x, z), waterLevel) * 100f);
            return acc;
        }

        /// <summary>Höchster Punkt, den eine Operation erzeugt (für Validator und Karten-Höhenlinien).</summary>
        public static float PeakOf(TerrainOpDef op, float ground) =>
            op.Kind == TerrainOpKind.Mesa ? Mathf.Max(ground, op.Height) : op.Kind == TerrainOpKind.Massif || op.Kind == TerrainOpKind.Peak || op.Kind == TerrainOpKind.Crater || op.Kind == TerrainOpKind.Terraces || op.Kind == TerrainOpKind.CliffCoast ? ground + op.Height : ground;

        /// <summary>Serpentine an einem Hang: Wegpunkte (lokal um das Zentrum) mit Steigung ≤ <paramref name="maxSlope"/> (Höhe je Meter).</summary>
        public static List<Vector3> Serpentine(Vector2 center, float baseRadius, float topRadius, float baseH, float topH, float maxSlope = 0.18f, int turns = 3)
        {
            var pts = new List<Vector3>();
            float rise = topH - baseH; if (rise <= 0f) { pts.Add(new Vector3(center.x, topH, center.y)); return pts; }
            int seg = Mathf.Max(8, turns * 8);
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg;
                float ang = t * turns * Mathf.PI * 2f;
                float rr = Mathf.Lerp(baseRadius, topRadius, t);
                pts.Add(new Vector3(center.x + Mathf.Cos(ang) * rr, baseH + rise * t, center.y + Mathf.Sin(ang) * rr));
            }
            return pts;
        }

        /// <summary>Steigung je Segment (Höhe/Länge), für Validator „Fußweg ≤ 1,2 m Stufen“.</summary>
        public static float MaxSlope(IList<Vector3> path)
        {
            float m = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1]; var b = path[i];
                float len = new Vector2(b.x - a.x, b.z - a.z).magnitude;
                if (len > 0.01f) m = Mathf.Max(m, Mathf.Abs(b.y - a.y) / len);
            }
            return m;
        }

        public static float Length(IList<Vector3> path) { float l = 0f; for (int i = 1; i < path.Count; i++) l += Vector3.Distance(path[i - 1], path[i]); return l; }
    }
}
