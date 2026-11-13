using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    public enum GpuEmitterShape { Point, Sphere, Cone, RingEdge, Line }
    public enum GpuRenderMode { Billboard, Stretched }

    /// <summary>Einstellungen eines GPU-Partikel-Effekts (Kräfte wie im Compute-Shader, CPU-Referenz in <see cref="GpuParticleModel"/>).</summary>
    [System.Serializable]
    public class GpuParticleSettings
    {
        public int Capacity = 20000;
        public float EmitPerSecond = 5000f, Burst = 0f;
        public GpuEmitterShape Shape = GpuEmitterShape.Sphere;
        public float ShapeRadius = 0.5f, ConeAngle = 25f, Speed = 3f, SpeedJitter = 0.5f;
        public float LifeMin = 0.8f, LifeMax = 1.6f, SizeStart = 0.08f, SizeEnd = 0.02f;
        public Vector3 Gravity = new Vector3(0f, -9.81f, 0f);
        public float WindFactor = 0.3f, Drag = 0.4f;
        public float VortexStrength = 0f, VortexRadius = 2f;
        public float Attract = 0f;            // > 0 zieht zur Emitter-Mitte (Aufladen), < 0 stößt ab (Explosion)
        public float CurlStrength = 0f, CurlScale = 0.6f;
        public bool CollideDepth = true;
        public float Bounce = 0.3f, StickBelowSpeed = 0.6f;
        public GpuRenderMode Render = GpuRenderMode.Billboard;
        [ColorUsage(true, true)] public Color ColorStart = new Color(2f, 1.4f, 0.6f, 1f);
        [ColorUsage(true, true)] public Color ColorEnd = new Color(0.8f, 0.2f, 0.05f, 0f);
    }

    /// <summary>Partikel der CPU-Referenz (gleiches Layout wie der GPU-Puffer: 48 Byte).</summary>
    public struct GpuParticle
    {
        public Vector3 Position; public float Age;
        public Vector3 Velocity; public float Life;
        public float Size; public uint Seed; public uint Flags; public float Pad;
        public bool Alive => Age < Life;
        public const int Stride = 48;
    }

    /// <summary>
    /// Budgets und CPU-Referenz der GPU-Partikel (V1). Der Compute-Shader rechnet dieselben Kräfte; die Referenz macht sie EditMode-testbar
    /// und dient als Fallback-Logik, wenn keine Compute-Shader verfügbar sind (dann übernimmt Shuriken die Darstellung).
    /// </summary>
    public static class GpuParticleModel
    {
        /// <summary>Partikel-Obergrenze je Stufe (gesamt, alle Effekte): Hoch 100 000, Ultra 200 000; darunter Shuriken-Fallback.</summary>
        public static int Budget(QualityTier t) => t == QualityTier.Ultra ? 200000 : t == QualityTier.Hoch ? 100000 : 0;

        /// <summary>GPU-Weg nur ab Hoch und mit Compute-Unterstützung; sonst Fallback (kein Effekt fehlt, er wird nur einfacher).</summary>
        public static bool UseGpu(QualityTier t, bool supportsCompute) => supportsCompute && Budget(t) > 0;

        /// <summary>Shuriken-Fallback: Anteil der Emission je Stufe (Niedrig 10 %, Mittel 25 %, ohne Compute auf Hoch 35 %).</summary>
        public static float FallbackEmissionShare(QualityTier t) => t == QualityTier.Niedrig ? 0.1f : t == QualityTier.Mittel ? 0.25f : 0.35f;

        public static uint Hash(uint s) { s ^= 2747636419u; s *= 2654435769u; s ^= s >> 16; s *= 2654435769u; s ^= s >> 16; s *= 2654435769u; return s; }
        public static float Rand01(ref uint s) { s = Hash(s); return (s & 0xFFFFFF) / 16777216f; }

        public static GpuParticle Spawn(GpuParticleSettings c, Vector3 origin, Vector3 forward, uint seed)
        {
            uint s = seed; var p = new GpuParticle { Seed = seed };
            Vector3 dir = forward.sqrMagnitude > 0 ? forward.normalized : Vector3.up;
            Vector3 rnd = new Vector3(Rand01(ref s) * 2 - 1, Rand01(ref s) * 2 - 1, Rand01(ref s) * 2 - 1);
            if (rnd.sqrMagnitude < 1e-6f) rnd = Vector3.up;
            switch (c.Shape)
            {
                case GpuEmitterShape.Point: p.Position = origin; break;
                case GpuEmitterShape.Sphere: p.Position = origin + rnd.normalized * c.ShapeRadius * Rand01(ref s); dir = rnd.normalized; break;
                case GpuEmitterShape.Cone:
                    p.Position = origin;
                    dir = Quaternion.AngleAxis((Rand01(ref s) * 2 - 1) * c.ConeAngle, Vector3.Cross(dir, rnd).normalized) * dir; break;
                case GpuEmitterShape.RingEdge:
                    float a = Rand01(ref s) * Mathf.PI * 2f; var ring = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    p.Position = origin + ring * c.ShapeRadius; dir = (Vector3.up + ring * 0.3f).normalized; break;
                case GpuEmitterShape.Line: p.Position = origin + forward * Rand01(ref s); dir = rnd.normalized; break;
            }
            p.Velocity = dir * c.Speed * (1f + (Rand01(ref s) * 2 - 1) * c.SpeedJitter);
            p.Life = Mathf.Lerp(c.LifeMin, c.LifeMax, Rand01(ref s)); p.Age = 0f; p.Size = c.SizeStart;
            return p;
        }

        /// <summary>Ein Schritt; groundY = Tiefen-Kollision vereinfacht (CPU: Ebene; GPU: Tiefenpuffer).</summary>
        public static void Step(ref GpuParticle p, GpuParticleSettings c, Vector3 center, Vector3 wind, float dt, float groundY = float.NegativeInfinity)
        {
            if (!p.Alive) return;
            if ((p.Flags & 1u) != 0) { p.Age += dt; p.Size = Mathf.Lerp(c.SizeStart, c.SizeEnd, Mathf.Clamp01(p.Age / p.Life)); return; }   // liegt am Boden
            var acc = c.Gravity + wind * c.WindFactor - p.Velocity * c.Drag;
            var rel = p.Position - center;
            if (c.VortexStrength != 0f)
            {
                var flat = new Vector3(rel.x, 0f, rel.z); float r = flat.magnitude;
                if (r > 1e-4f) acc += Vector3.Cross(Vector3.up, flat / r) * c.VortexStrength * Mathf.Exp(-r / Mathf.Max(0.1f, c.VortexRadius));
            }
            if (c.Attract != 0f && rel.sqrMagnitude > 1e-6f) acc -= rel.normalized * c.Attract;
            if (c.CurlStrength != 0f) acc += Curl(p.Position * c.CurlScale, p.Age) * c.CurlStrength;
            p.Velocity += acc * dt; p.Position += p.Velocity * dt; p.Age += dt;
            float t = Mathf.Clamp01(p.Age / p.Life); p.Size = Mathf.Lerp(c.SizeStart, c.SizeEnd, t);
            if (c.CollideDepth && p.Position.y < groundY)
            {
                p.Position.y = groundY;
                if (Mathf.Abs(p.Velocity.y) < c.StickBelowSpeed) { p.Velocity = Vector3.zero; p.Flags |= 1u; }   // liegt am Boden (Tropfen, Glut)
                else p.Velocity = new Vector3(p.Velocity.x * 0.6f, -p.Velocity.y * c.Bounce, p.Velocity.z * 0.6f);
            }
        }

        /// <summary>Wirbel-Rauschfeld aus Sinuswellen (günstige Curl-Näherung) – gleiche Formel im Compute-Shader.</summary>
        public static Vector3 Curl(Vector3 p, float t)
        {
            float a = Mathf.Sin(p.y * 1.7f + t) + Mathf.Cos(p.z * 1.3f), b = Mathf.Sin(p.z * 1.5f + t * 0.7f) + Mathf.Cos(p.x * 1.9f), c = Mathf.Sin(p.x * 1.1f - t * 0.9f) + Mathf.Cos(p.y * 1.4f);
            return new Vector3(b - c, c - a, a - b) * 0.5f;
        }

        public static Color ColorAt(GpuParticleSettings c, float age01) => Color.Lerp(c.ColorStart, c.ColorEnd, age01);
    }
}
