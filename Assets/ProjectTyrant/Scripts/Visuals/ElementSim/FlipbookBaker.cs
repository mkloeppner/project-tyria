using System;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>Ergebnis eines Backvorgangs: vier Atlanten (Spalten × Zeilen Kacheln, Zeile 0 oben wie Unity-Flipbooks).</summary>
    public sealed class FlipbookResult
    {
        public int Columns, Rows, Tile, MotionTile;
        public Color32[] Color, Normal, Emissive, Motion;
        /// <summary>Dichte je Bild auf Kachelauflösung (für Metriken), Länge Frames × Tile².</summary>
        public float[][] FrameDensity;
        public float LoopSeam, MeanFrameDelta, PeakTime;
        public int PeakFrame;
        public uint Hash;
        public int Frames => Columns * Rows;
        public int AtlasSize => Columns * Tile;
        public int MotionAtlasSize => Columns * MotionTile;
        /// <summary>Unkomprimierte Größe aller vier Atlanten in Bytes (RGBA32).</summary>
        public long RawBytes => 4L * (3L * AtlasSize * AtlasSize + (long)MotionAtlasSize * MotionAtlasSize);
    }

    /// <summary>
    /// Backt ein SimRecipe zu Flipbook-Atlanten. Burst: Start aus Ruhe, Emitter nach Rezept, letzte 20 % blenden auf leer.
    /// Loop: Simulation läuft Frames + 25 % weiter; die ersten 25 % werden mit dem Überhang überblendet → nahtlos.
    /// </summary>
    public static class FlipbookBaker
    {
        public const int Version = 1;

        struct FrameFields { public float[] D, T, U, V; }

        public static FlipbookResult Bake(SimRecipe r)
        {
            int frames = r.Frames, tile = r.TileSize, mt = Math.Max(8, tile / 2);
            int overlap = r.Mode == FlipbookMode.Loop ? Math.Max(1, frames / 4) : 0;
            int total = frames + overlap;
            var sim = new FluidSolver2D(r.Grid, r.Seed)
            {
                Buoyancy = r.Buoyancy, Gravity = r.Gravity, Weight = r.Weight, Vorticity = r.Vorticity, Cooling = r.Cooling,
                DensityFade = r.DensityFade, Viscosity = r.Viscosity, Diffusion = r.Diffusion, Damping = r.Damping, Iterations = r.Iterations
            };
            if (r.FloorHeight > 0f) sim.AddObstacle(0f, 0f, 1f, r.FloorHeight);

            int totalSteps = r.WarmupSteps + total * r.StepsPerFrame, step = 0;
            // im Loop-Modus laufen die Emitter dauerhaft (Zeitanteil bleibt in [Start, End]), im Burst-Modus nach Zeitanteil
            for (int w = 0; w < r.WarmupSteps; w++) { EmitAll(r, sim, r.Mode == FlipbookMode.Loop ? 0.5f : 0f, r.Dt); sim.Step(r.Dt); step++; }

            var raw = new FrameFields[total];
            for (int f = 0; f < total; f++)
            {
                for (int s = 0; s < r.StepsPerFrame; s++)
                {
                    float t01 = r.Mode == FlipbookMode.Loop ? 0.5f : (float)(f * r.StepsPerFrame + s) / (frames * r.StepsPerFrame);
                    EmitAll(r, sim, t01, r.Dt); sim.Step(r.Dt); step++;
                }
                raw[f] = Capture(sim, tile, mt);
            }

            var outF = new FrameFields[frames];
            for (int f = 0; f < frames; f++)
            {
                if (r.Mode == FlipbookMode.Loop && f < overlap)
                {
                    float w = (float)f / overlap;   // f = 0 entspricht dem Bild nach dem letzten → Naht stetig
                    outF[f] = Lerp(raw[f + frames], raw[f], w);
                }
                else outF[f] = raw[f];
                if (r.Mode == FlipbookMode.Burst)
                {
                    float fadeStart = frames * 0.8f;
                    if (f >= fadeStart) { float k = 1f - (f - fadeStart + 1) / (frames - fadeStart); outF[f] = Scale(outF[f], Mathf.Clamp01(k)); }
                }
            }
            return Render(r, outF, tile, mt);
        }

        static void EmitAll(SimRecipe r, FluidSolver2D sim, float t01, float dt) { foreach (var e in r.Emitters) sim.Emit(e, t01, dt); }

        static FrameFields Capture(FluidSolver2D sim, int tile, int mt)
        {
            var f = new FrameFields { D = new float[tile * tile], T = new float[tile * tile], U = new float[mt * mt], V = new float[mt * mt] };
            for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    float sx = (x + 0.5f) / tile, sy = (y + 0.5f) / tile; int k = x + y * tile;
                    f.D[k] = sim.Sample(sim.Dens, sx, sy); f.T[k] = sim.Sample(sim.Temp, sx, sy);
                }
            for (int y = 0; y < mt; y++)
                for (int x = 0; x < mt; x++)
                {
                    float sx = (x + 0.5f) / mt, sy = (y + 0.5f) / mt; int k = x + y * mt;
                    f.U[k] = sim.Sample(sim.U, sx, sy); f.V[k] = sim.Sample(sim.V, sx, sy);
                }
            return f;
        }

        static FrameFields Lerp(FrameFields a, FrameFields b, float w)
        {
            float[] L(float[] x, float[] y) { var o = new float[x.Length]; for (int i = 0; i < o.Length; i++) o[i] = x[i] + (y[i] - x[i]) * w; return o; }
            return new FrameFields { D = L(a.D, b.D), T = L(a.T, b.T), U = L(a.U, b.U), V = L(a.V, b.V) };
        }

        static FrameFields Scale(FrameFields a, float k)
        {
            float[] S(float[] x) { var o = new float[x.Length]; for (int i = 0; i < o.Length; i++) o[i] = x[i] * k; return o; }
            return new FrameFields { D = S(a.D), T = S(a.T), U = S(a.U), V = S(a.V) };
        }

        static FlipbookResult Render(SimRecipe r, FrameFields[] frames, int tile, int mt)
        {
            int cols = r.Columns, rows = r.Rows, atlas = cols * tile, matlas = cols * mt;
            var res = new FlipbookResult
            {
                Columns = cols, Rows = rows, Tile = tile, MotionTile = mt,
                Color = new Color32[atlas * atlas], Normal = new Color32[atlas * atlas], Emissive = new Color32[atlas * atlas], Motion = new Color32[matlas * matlas],
                FrameDensity = new float[frames.Length][]
            };
            var ramp = r.Ramp ?? SimRecipe.DefaultRamp();
            float peak = -1f; int peakFrame = 0;
            for (int f = 0; f < frames.Length; f++)
            {
                var fr = frames[f]; res.FrameDensity[f] = fr.D;
                int cx = f % cols, cy = rows - 1 - f / cols;   // Kachel 0 oben links
                float energy = 0f;
                for (int y = 0; y < tile; y++)
                    for (int x = 0; x < tile; x++)
                    {
                        int k = x + y * tile; float d = fr.D[k], t = fr.T[k];
                        float key = Mathf.Clamp01((r.ColorFromTemperature ? t : d) * r.Exposure);
                        float a = Mathf.Clamp01(d * r.AlphaScale);
                        energy += key * a;
                        Color c = ramp.Evaluate(key); c.a = a;
                        float dx = (x + 1 < tile ? fr.D[k + 1] : d) - (x > 0 ? fr.D[k - 1] : d);
                        float dy = (y + 1 < tile ? fr.D[k + tile] : d) - (y > 0 ? fr.D[k - tile] : d);
                        var n = new Vector3(-dx * r.NormalStrength, -dy * r.NormalStrength, 1f).normalized;
                        float core = Mathf.Clamp01((key - r.CoreThreshold) / Mathf.Max(0.01f, 1f - r.CoreThreshold)) * a;
                        int px = cx * tile + x + (cy * tile + y) * atlas;
                        res.Color[px] = c;
                        res.Normal[px] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, a);
                        res.Emissive[px] = new Color(core, core, core, 1f);
                    }
                for (int y = 0; y < mt; y++)
                    for (int x = 0; x < mt; x++)
                    {
                        int k = x + y * mt;
                        float u = Mathf.Clamp(fr.U[k] / r.MaxVelocity, -1f, 1f), v = Mathf.Clamp(fr.V[k] / r.MaxVelocity, -1f, 1f);
                        res.Motion[cx * mt + x + (cy * mt + y) * matlas] = new Color(u * 0.5f + 0.5f, v * 0.5f + 0.5f, 0f, 1f);
                    }
                if (energy > peak) { peak = energy; peakFrame = f; }
            }
            res.PeakFrame = peakFrame; res.PeakTime = peakFrame / r.PlaybackFps;
            res.LoopSeam = FrameDelta(frames[frames.Length - 1].D, frames[0].D);
            float sum = 0f; for (int f = 1; f < frames.Length; f++) sum += FrameDelta(frames[f - 1].D, frames[f].D);
            res.MeanFrameDelta = frames.Length > 1 ? sum / (frames.Length - 1) : 0f;
            uint h = Fnv.Hash(res.Color); h = Fnv.Hash(res.Normal, h); h = Fnv.Hash(res.Motion, h); h = Fnv.Hash(res.Emissive, h);
            res.Hash = h;
            return res;
        }

        /// <summary>Mittlere absolute Differenz der (auf 0–1 geklemmten) Dichte zweier Bilder.</summary>
        public static float FrameDelta(float[] a, float[] b)
        {
            float s = 0f; for (int i = 0; i < a.Length; i++) s += Math.Abs(Mathf.Clamp01(a[i]) - Mathf.Clamp01(b[i]));
            return s / a.Length;
        }

        /// <summary>Naht relativ zur Bildfläche mit Inhalt (Regel 5: ≤ 2 %).</summary>
        public static float SeamRatio(FlipbookResult r)
        {
            var last = r.FrameDensity[r.Frames - 1]; float cover = 0f;
            for (int i = 0; i < last.Length; i++) cover += Mathf.Clamp01(last[i]);
            cover /= last.Length;
            return cover <= 1e-5f ? 0f : r.LoopSeam / Mathf.Max(cover, 0.05f);
        }
    }
}
