using System;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>Deterministischer Zufall (xorshift32) – identisch auf jeder Plattform, unabhängig von System.Random.</summary>
    public struct SimRng
    {
        uint _s;
        public SimRng(int seed) { _s = (uint)seed * 2654435761u + 0x9E3779B9u; if (_s == 0) _s = 1; }
        public uint NextUInt() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
        /// <summary>Gleichverteilt in [0, 1).</summary>
        public float Next() => (NextUInt() >> 8) * (1f / 16777216f);
        public float Range(float a, float b) => a + (b - a) * Next();
    }

    public enum EmitterShape { Point, Line, Ring, Mask }

    /// <summary>Quelle im Fluid-Gitter (Koordinaten 0–1, y nach oben).</summary>
    [Serializable]
    public class SimEmitter
    {
        public EmitterShape Shape = EmitterShape.Point;
        public float X = 0.5f, Y = 0.15f, X2 = 0.5f, Y2 = 0.15f, Radius = 0.06f;
        public float Density = 1f, Temperature = 0f, VelX = 0f, VelY = 0f;
        /// <summary>Aktiv von Start bis Ende (Anteil der Simulationsdauer, 0–1).</summary>
        public float Start = 0f, End = 1f;
        /// <summary>Zufälliges Flackern der Stärke und der Richtung (0–1), aus dem Seed.</summary>
        public float Jitter = 0.3f;
        /// <summary>Für Mask: 1 = gefüllte Zelle, Größe N×N (leer = kein Beitrag).</summary>
        [NonSerialized] public float[] Mask;
    }

    /// <summary>
    /// 2D-Fluid nach Stable Fluids (Stam 1999): Advektion semi-Lagrange, Diffusion und Projektion mit Gauss-Seidel,
    /// Dichte-, Temperatur- und Geschwindigkeitsfeld, Auftrieb, Schwerkraft, Wirbelstärke-Verstärkung, Hindernis-Maske.
    /// Nur im Editor (Flipbook-Baker), nie zur Laufzeit. Feste Schrittweite, Seed-Rauschen → bitgleich reproduzierbar.
    /// </summary>
    public sealed class FluidSolver2D
    {
        public readonly int N;
        readonly int _s;
        public readonly float[] U, V, Dens, Temp;
        readonly float[] _u0, _v0, _d0, _t0, _curl;
        public readonly bool[] Solid;
        public float Viscosity = 0.00001f, Diffusion = 0.00002f, Buoyancy = 1.2f, Gravity = 0f, Weight = 0.05f;
        public float Vorticity = 0f, Cooling = 0.6f, DensityFade = 0.15f, Damping = 0.02f;
        public int Iterations = 20;
        SimRng _rng;

        public FluidSolver2D(int n, int seed)
        {
            if (n < 8 || n > 512) throw new ArgumentOutOfRangeException(nameof(n), "Gitter 8–512");
            N = n; _s = n + 2; int size = _s * _s;
            U = new float[size]; V = new float[size]; Dens = new float[size]; Temp = new float[size];
            _u0 = new float[size]; _v0 = new float[size]; _d0 = new float[size]; _t0 = new float[size]; _curl = new float[size];
            Solid = new bool[size]; _rng = new SimRng(seed);
        }

        public int IX(int i, int j) => i + _s * j;

        /// <summary>Hindernis-Rechteck in Gitterkoordinaten 0–1 (z. B. Boden für Spritzer).</summary>
        public void AddObstacle(float x0, float y0, float x1, float y1)
        {
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                {
                    float x = (i - 0.5f) / N, y = (j - 0.5f) / N;
                    if (x >= x0 && x <= x1 && y >= y0 && y <= y1) Solid[IX(i, j)] = true;
                }
        }

        public void Emit(SimEmitter e, float time01, float dt)
        {
            if (time01 < e.Start || time01 > e.End) return;
            float flick = 1f + (_rng.Next() * 2f - 1f) * e.Jitter;
            float angle = (_rng.Next() * 2f - 1f) * e.Jitter * 0.6f;
            float ca = (float)Math.Cos(angle), sa = (float)Math.Sin(angle);
            float vx = e.VelX * ca - e.VelY * sa, vy = e.VelX * sa + e.VelY * ca;   // Gebietslängen je Sekunde
            float r2 = e.Radius * e.Radius;
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                {
                    int k = IX(i, j); if (Solid[k]) continue;
                    float x = (i - 0.5f) / N, y = (j - 0.5f) / N, w;
                    switch (e.Shape)
                    {
                        case EmitterShape.Line: w = Falloff(DistSqToSegment(x, y, e.X, e.Y, e.X2, e.Y2), r2); break;
                        case EmitterShape.Ring:
                            float dr = (float)Math.Sqrt((x - e.X) * (x - e.X) + (y - e.Y) * (y - e.Y)) - (e.X2 - e.X);
                            w = Falloff(dr * dr, r2); break;
                        case EmitterShape.Mask: w = e.Mask != null && e.Mask.Length == N * N ? e.Mask[(i - 1) + N * (j - 1)] : 0f; break;
                        default: w = Falloff((x - e.X) * (x - e.X) + (y - e.Y) * (y - e.Y), r2); break;
                    }
                    if (w <= 0f) continue;
                    w *= flick * dt;
                    Dens[k] += e.Density * w; Temp[k] += e.Temperature * w;
                    U[k] += vx * w; V[k] += vy * w;
                }
        }

        static float Falloff(float d2, float r2) => d2 >= r2 ? 0f : 1f - d2 / r2;

        static float DistSqToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, l = dx * dx + dy * dy;
            float t = l <= 1e-8f ? 0f : Math.Max(0f, Math.Min(1f, ((px - ax) * dx + (py - ay) * dy) / l));
            float qx = ax + t * dx - px, qy = ay + t * dy - py; return qx * qx + qy * qy;
        }

        /// <summary>Ein Zeitschritt (feste Schrittweite dt in Sekunden).</summary>
        public void Step(float dt)
        {
            int size = _s * _s;
            // Kräfte: Auftrieb (Temperatur) gegen Gewicht (Dichte), Schwerkraft für Wasser, Dämpfung
            for (int k = 0; k < size; k++)
            {
                if (Solid[k]) continue;
                V[k] += dt * (Buoyancy * Temp[k] - Weight * Dens[k] - Gravity * Math.Min(Dens[k], 1f));
                float damp = 1f - Damping * dt; U[k] *= damp; V[k] *= damp;
            }
            if (Vorticity > 0f) Confine(dt);

            // Geschwindigkeit
            Copy(U, _u0); Copy(V, _v0);
            Diffuse(1, U, _u0, Viscosity, dt); Diffuse(2, V, _v0, Viscosity, dt);
            Project(U, V, _u0, _v0);
            Copy(U, _u0); Copy(V, _v0);
            Advect(1, U, _u0, _u0, _v0, dt); Advect(2, V, _v0, _u0, _v0, dt);
            Project(U, V, _u0, _v0);

            // Skalare
            Copy(Dens, _d0); Diffuse(0, Dens, _d0, Diffusion, dt); Copy(Dens, _d0); Advect(0, Dens, _d0, U, V, dt);
            Copy(Temp, _t0); Diffuse(0, Temp, _t0, Diffusion, dt); Copy(Temp, _t0); Advect(0, Temp, _t0, U, V, dt);
            float fade = 1f - DensityFade * dt, cool = 1f - Cooling * dt;
            for (int k = 0; k < size; k++)
            {
                if (Solid[k]) { Dens[k] = 0f; Temp[k] = 0f; continue; }
                Dens[k] = Math.Max(0f, Dens[k] * fade); Temp[k] = Math.Max(0f, Temp[k] * cool);
            }
        }

        static void Copy(float[] a, float[] b) => Array.Copy(a, b, a.Length);

        void Confine(float dt)
        {
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                    _curl[IX(i, j)] = 0.5f * ((V[IX(i + 1, j)] - V[IX(i - 1, j)]) - (U[IX(i, j + 1)] - U[IX(i, j - 1)]));
            for (int j = 2; j < N; j++)
                for (int i = 2; i < N; i++)
                {
                    int k = IX(i, j); if (Solid[k]) continue;
                    float gx = 0.5f * (Math.Abs(_curl[IX(i + 1, j)]) - Math.Abs(_curl[IX(i - 1, j)]));
                    float gy = 0.5f * (Math.Abs(_curl[IX(i, j + 1)]) - Math.Abs(_curl[IX(i, j - 1)]));
                    float len = (float)Math.Sqrt(gx * gx + gy * gy) + 1e-5f;
                    gx /= len; gy /= len; float w = _curl[k];
                    U[k] += dt * Vorticity * gy * w; V[k] -= dt * Vorticity * gx * w;
                }
        }

        void Diffuse(int b, float[] x, float[] x0, float diff, float dt)
        {
            float a = dt * diff * N * N; if (a <= 0f) return;
            Relax(b, x, x0, a, 1f + 4f * a);
        }

        void Relax(int b, float[] x, float[] x0, float a, float c)
        {
            float inv = 1f / c;
            for (int it = 0; it < Iterations; it++)
            {
                for (int j = 1; j <= N; j++)
                    for (int i = 1; i <= N; i++)
                    {
                        int k = IX(i, j);
                        x[k] = (x0[k] + a * (x[k - 1] + x[k + 1] + x[k - _s] + x[k + _s])) * inv;
                    }
                Bound(b, x);
            }
        }

        void Advect(int b, float[] d, float[] d0, float[] u, float[] v, float dt)
        {
            float dt0 = dt * N;
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                {
                    int k = IX(i, j);
                    float x = i - dt0 * u[k], y = j - dt0 * v[k];
                    if (x < 0.5f) x = 0.5f; if (x > N + 0.5f) x = N + 0.5f;
                    if (y < 0.5f) y = 0.5f; if (y > N + 0.5f) y = N + 0.5f;
                    int i0 = (int)x, j0 = (int)y, i1 = i0 + 1, j1 = j0 + 1;
                    float s1 = x - i0, s0 = 1f - s1, t1 = y - j0, t0 = 1f - t1;
                    d[k] = s0 * (t0 * d0[IX(i0, j0)] + t1 * d0[IX(i0, j1)]) + s1 * (t0 * d0[IX(i1, j0)] + t1 * d0[IX(i1, j1)]);
                }
            Bound(b, d);
        }

        void Project(float[] u, float[] v, float[] p, float[] div)
        {
            float h = 1f / N;
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                {
                    int k = IX(i, j);
                    div[k] = -0.5f * h * (u[k + 1] - u[k - 1] + v[k + _s] - v[k - _s]); p[k] = 0f;
                }
            Bound(0, div); Bound(0, p);
            Relax(0, p, div, 1f, 4f);
            for (int j = 1; j <= N; j++)
                for (int i = 1; i <= N; i++)
                {
                    int k = IX(i, j);
                    u[k] -= 0.5f * (p[k + 1] - p[k - 1]) / h; v[k] -= 0.5f * (p[k + _s] - p[k - _s]) / h;
                }
            Bound(1, u); Bound(2, v);
        }

        /// <summary>Ränder: Wände (b = 1 spiegelt u, b = 2 spiegelt v), Hindernis-Zellen ohne Geschwindigkeit.</summary>
        void Bound(int b, float[] x)
        {
            for (int i = 1; i <= N; i++)
            {
                x[IX(0, i)] = b == 1 ? -x[IX(1, i)] : x[IX(1, i)];
                x[IX(N + 1, i)] = b == 1 ? -x[IX(N, i)] : x[IX(N, i)];
                x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
                x[IX(i, N + 1)] = b == 2 ? -x[IX(i, N)] : x[IX(i, N)];
            }
            x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);
            x[IX(0, N + 1)] = 0.5f * (x[IX(1, N + 1)] + x[IX(0, N)]);
            x[IX(N + 1, 0)] = 0.5f * (x[IX(N, 0)] + x[IX(N + 1, 1)]);
            x[IX(N + 1, N + 1)] = 0.5f * (x[IX(N, N + 1)] + x[IX(N + 1, N)]);
            if (b != 0) for (int k = 0; k < x.Length; k++) if (Solid[k]) x[k] = 0f;
        }

        /// <summary>Bilinear abgetastetes Feld an (x, y) in 0–1.</summary>
        public float Sample(float[] f, float x, float y)
        {
            float gx = x * N + 0.5f, gy = y * N + 0.5f;
            if (gx < 0.5f) gx = 0.5f; if (gx > N + 0.5f) gx = N + 0.5f;
            if (gy < 0.5f) gy = 0.5f; if (gy > N + 0.5f) gy = N + 0.5f;
            int i0 = (int)gx, j0 = (int)gy; float s = gx - i0, t = gy - j0;
            int i1 = Math.Min(i0 + 1, N + 1), j1 = Math.Min(j0 + 1, N + 1);
            return (1 - s) * ((1 - t) * f[IX(i0, j0)] + t * f[IX(i0, j1)]) + s * ((1 - t) * f[IX(i1, j0)] + t * f[IX(i1, j1)]);
        }

        public float TotalDensity() { float s = 0f; for (int k = 0; k < Dens.Length; k++) s += Dens[k]; return s; }

        /// <summary>Mittlere Divergenz nach der Projektion (Qualitätsmaß, Ziel ≈ 0).</summary>
        public float MeanAbsDivergence()
        {
            float sum = 0f;
            for (int j = 2; j < N; j++)
                for (int i = 2; i < N; i++)
                {
                    int k = IX(i, j); if (Solid[k] || Solid[k + 1] || Solid[k - 1] || Solid[k + _s] || Solid[k - _s]) continue;
                    sum += Math.Abs(0.5f * (U[k + 1] - U[k - 1] + V[k + _s] - V[k - _s]));
                }
            return sum / ((N - 2) * (N - 2));
        }

        public float MeanSpeed()
        {
            float s = 0f; for (int k = 0; k < U.Length; k++) s += (float)Math.Sqrt(U[k] * U[k] + V[k] * V[k]); return s / U.Length;
        }

        /// <summary>Schwerpunkt der Dichte in y (0–1); steigt bei Auftrieb, fällt bei Schwerkraft.</summary>
        public float DensityCentroidY()
        {
            float s = 0f, sy = 0f;
            for (int j = 1; j <= N; j++) for (int i = 1; i <= N; i++) { float d = Dens[IX(i, j)]; s += d; sy += d * (j - 0.5f) / N; }
            return s <= 1e-6f ? 0f : sy / s;
        }
    }
}
