using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Fortbewegungs-Fähigkeiten, die Gelände freischalten (Reittiere; Gleiten wird nicht als Pflicht verwendet).</summary>
    [Flags] public enum Traversal
    {
        None = 0, Raptor = 1, Springer = 2, Skimmer = 4,
        // Welt-Lauf W1: weitere Schlüssel für Reittier-Tore (Flutfüllung nutzt nur die ersten drei)
        Griffon = 8, Skyscale = 16, DuneRunner = 32, Boat = 64, JadeBot = 128, CorruptionWard = 256, LavaSkimmer = 512,
        All = 1023
    }

    /// <summary>Gelände-Raster für die Erreichbarkeit: Höhe + Art (Land, tiefes Wasser, Lava).</summary>
    public class ReachabilityGrid
    {
        public const byte Land = 0, DeepWater = 1, Lava = 2;
        public readonly int N;
        public readonly float Cell, Half;
        public readonly float[] H;
        public readonly byte[] Kind;

        public ReachabilityGrid(int n, float cell)
        {
            N = n; Cell = cell; Half = n * cell * 0.5f;
            H = new float[n * n]; Kind = new byte[n * n];
        }

        /// <summary>Aus einem Gelände-Sampler: Wasser tiefer als <paramref name="deep"/> m ist tief (bzw. Lava bei <paramref name="lava"/>).</summary>
        public static ReachabilityGrid FromSampler(ITerrainSampler s, float cell, bool lava, float deep = 1.2f)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(s.Size / cell));
            var g = new ReachabilityGrid(n, s.Size / n);
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    var w = g.World(x, z);
                    float h = s.Height(w.x, w.y);
                    int i = z * n + x;
                    g.H[i] = h;
                    if (lava && h < s.WaterLevel) g.Kind[i] = Lava;
                    else if (h < s.WaterLevel - deep) g.Kind[i] = DeepWater;
                }
            return g;
        }

        public Vector2 World(int x, int z) => new Vector2(x * Cell - Half + Cell * 0.5f, z * Cell - Half + Cell * 0.5f);
        public bool CellOf(Vector2 p, out int x, out int z)
        {
            x = Mathf.FloorToInt((p.x + Half) / Cell); z = Mathf.FloorToInt((p.y + Half) / Cell);
            return x >= 0 && z >= 0 && x < N && z < N;
        }
    }

    /// <summary>
    /// Erreichbarkeit per Flutfüllung (rein, getestet). Zu Fuß: Höhenunterschied ≤ <see cref="WalkStep"/> je Zelle, Abstieg bis <see cref="MaxDrop"/>.
    /// Springer: zusätzlich Aufstieg bis <see cref="SpringerUp"/>. Raptor: Sprung über bis zu <see cref="RaptorGap"/> m unpassierbares Gelände.
    /// Skimmer: tiefes Wasser (nicht Lava) ist begehbar.
    /// </summary>
    public static class ReachabilityModel
    {
        public static float WalkStep = 3.4f;      // je 4-m-Zelle ≈ 40° Hang (CharacterController 50°)
        public const float MaxDrop = 9f;          // knapp über der Fallschaden-Schwelle (8 m)
        public const float SpringerUp = 14f;
        public const float RaptorGap = 14f;
        public const float RaptorRise = 1.5f;

        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>Reihenfolge, in der Anforderungen geprüft werden (wenigste/früheste Reittiere zuerst).</summary>
        public static readonly Traversal[] Order =
        {
            Traversal.None, Traversal.Raptor, Traversal.Skimmer, Traversal.Springer,
            Traversal.Raptor | Traversal.Skimmer, Traversal.Raptor | Traversal.Springer, Traversal.Skimmer | Traversal.Springer,
            Traversal.Raptor | Traversal.Skimmer | Traversal.Springer,
        };

        static float Surface(ReachabilityGrid g, int i, float waterLevel) => g.Kind[i] == ReachabilityGrid.DeepWater ? Mathf.Max(g.H[i], waterLevel) : g.H[i];

        static bool Standable(ReachabilityGrid g, int i, Traversal t) =>
            g.Kind[i] == ReachabilityGrid.Land || (g.Kind[i] == ReachabilityGrid.DeepWater && (t & Traversal.Skimmer) != 0);

        public static bool[] Flood(ReachabilityGrid g, Vector2 start, Traversal t, float waterLevel)
        {
            var seen = new bool[g.N * g.N];
            if (!g.CellOf(start, out int sx, out int sz)) return seen;
            var q = new Queue<int>();
            int s0 = sz * g.N + sx;
            seen[s0] = true; q.Enqueue(s0);
            int gapCells = Mathf.FloorToInt(RaptorGap / g.Cell);
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int x = i % g.N, z = i / g.N;
                float h = Surface(g, i, waterLevel);
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + DX[d], nz = z + DZ[d];
                    if (nx < 0 || nz < 0 || nx >= g.N || nz >= g.N) continue;
                    int j = nz * g.N + nx;
                    if (seen[j]) continue;
                    if (Standable(g, j, t))
                    {
                        float dh = Surface(g, j, waterLevel) - h;
                        float step = WalkStep * (d >= 4 ? 1.41f : 1f);
                        bool ok = dh <= step && dh >= -MaxDrop;
                        if (!ok && (t & Traversal.Springer) != 0 && dh > 0f && dh <= SpringerUp) ok = true;
                        if (ok) { seen[j] = true; q.Enqueue(j); continue; }
                    }
                    if ((t & Traversal.Raptor) == 0 || g.Kind[i] != ReachabilityGrid.Land) continue;
                    // Raptor-Sprung: über 1..gapCells nicht begehbare/zu steile Zellen hinweg auf Land
                    for (int k = 2; k <= gapCells; k++)
                    {
                        int fx = x + DX[d] * k, fz = z + DZ[d] * k;
                        if (fx < 0 || fz < 0 || fx >= g.N || fz >= g.N) break;
                        if (k * g.Cell * (d >= 4 ? 1.41f : 1f) > RaptorGap) break;
                        int f = fz * g.N + fx;
                        if (g.Kind[f] != ReachabilityGrid.Land) continue;
                        float fdh = g.H[f] - h;
                        if (fdh <= RaptorRise && fdh >= -MaxDrop && !seen[f]) { seen[f] = true; q.Enqueue(f); }
                        break;
                    }
                }
            }
            return seen;
        }

        /// <summary>Nächste erreichte Zelle zum Ziel im Umkreis (Marker stehen auf Bauwerken/Rändern).</summary>
        public static bool Reached(ReachabilityGrid g, bool[] flood, Vector2 target, float radius)
        {
            if (!g.CellOf(target, out int tx, out int tz)) return false;
            int r = Mathf.CeilToInt(radius / g.Cell);
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = tx + dx, z = tz + dz;
                    if (x < 0 || z < 0 || x >= g.N || z >= g.N) continue;
                    if (dx * dx + dz * dz > r * r) continue;
                    if (flood[z * g.N + x]) return true;
                }
            return false;
        }

        /// <summary>Kleinste Fähigkeiten-Kombination, mit der das Ziel erreichbar ist (false = unerreichbar).</summary>
        public static bool Required(ReachabilityGrid g, Vector2 start, Vector2 target, float waterLevel, out Traversal req, float radius = 4f, Dictionary<Traversal, bool[]> cache = null)
        {
            foreach (var t in Order)
            {
                bool[] f;
                if (cache == null || !cache.TryGetValue(t, out f)) { f = Flood(g, start, t, waterLevel); if (cache != null) cache[t] = f; }
                if (Reached(g, f, target, radius)) { req = t; return true; }
            }
            req = Traversal.None;
            return false;
        }

        public static string Label(Traversal t)
        {
            if (t == Traversal.None) return "";
            var parts = new List<string>();
            if ((t & Traversal.Raptor) != 0) parts.Add("Raptor");
            if ((t & Traversal.Springer) != 0) parts.Add("Springer");
            if ((t & Traversal.Skimmer) != 0) parts.Add("Skimmer");
            if ((t & Traversal.LavaSkimmer) != 0) parts.Add("Skimmer mit Glutschritt");
            if ((t & Traversal.Griffon) != 0) parts.Add("Greif");
            if ((t & Traversal.Skyscale) != 0) parts.Add("Himmelsschuppe");
            if ((t & Traversal.DuneRunner) != 0) parts.Add("Dünenläufer");
            if ((t & Traversal.Boat) != 0) parts.Add("Boot");
            if ((t & Traversal.JadeBot) != 0) parts.Add("Jadegeist");
            if ((t & Traversal.CorruptionWard) != 0) parts.Add("Verderbnis-Widerstand");
            return string.Join(" + ", parts);
        }
    }
}
