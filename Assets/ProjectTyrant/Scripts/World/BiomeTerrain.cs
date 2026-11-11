using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Höhen-/Farbquelle einer Zone (Startzone: altes Layout, sonst Biom-Rauschen).</summary>
    public interface ITerrainSampler
    {
        float Size { get; }
        float WaterLevel { get; }
        float Height(float x, float z);
        Color GroundColor(float x, float z, float h, float slope);
    }

    public class LegacyTerrain : ITerrainSampler
    {
        public float Size => Procedural.ZoneLayout.Size;
        public float WaterLevel => Procedural.ZoneLayout.WaterLevel;
        public float Height(float x, float z) => Procedural.ZoneLayout.Height(x, z);
        public Color GroundColor(float x, float z, float h, float slope) => Procedural.ZoneLayout.GroundColor(x, z, h, slope);
    }

    /// <summary>
    /// Deterministisches Biom-Gelände (Seed): Fraktal-Rauschen, Terrassen (Plateaus), geriffelte Schluchten (Kanäle/Risse),
    /// Randgebirge, abgeflachte Plätze (Schauplätze, Wegmarken) über <see cref="Flatten"/>.
    /// </summary>
    public class BiomeTerrain : ITerrainSampler
    {
        readonly BiomeProfile _p;
        readonly float _size, _ox, _oz, _cx, _cz;
        readonly System.Collections.Generic.List<Vector4> _flats = new System.Collections.Generic.List<Vector4>();   // x, z, radius, height

        public BiomeTerrain(BiomeProfile p, int seed, float size)
        {
            _p = p; _size = size;
            var r = new System.Random(seed);
            _ox = (float)r.NextDouble() * 1000f; _oz = (float)r.NextDouble() * 1000f;
            _cx = (float)r.NextDouble() * 1000f; _cz = (float)r.NextDouble() * 1000f;
        }

        public float Size => _size;
        public float WaterLevel => _p.WaterLevel;
        public BiomeProfile Profile => _p;

        // Gelände-Merkmale (vor den Plätzen angewandt): 0 = Tafelberg, 1 = Tümpel, 2 = Graben-Ring
        struct Feature { public int Kind; public Vector2 C; public float R0, R1, Value; }
        readonly System.Collections.Generic.List<Feature> _features = new System.Collections.Generic.List<Feature>();

        /// <summary>Tafelberg: Innenfläche auf <paramref name="top"/> m, Steilkante 2,5 m breit (nur mit Springer erklimmbar, wenn hoch genug).</summary>
        public void Mesa(Vector2 c, float radius, float top) => _features.Add(new Feature { Kind = 0, C = c, R0 = radius, R1 = radius + 2.5f, Value = top });
        /// <summary>Tiefer Tümpel/Kanal: Boden <paramref name="depth"/> m unter dem Wasserspiegel (Rand 6 m).</summary>
        public void Pool(Vector2 c, float radius, float depth) => _features.Add(new Feature { Kind = 1, C = c, R0 = radius, R1 = radius + 6f, Value = depth });
        /// <summary>Graben-Ring zwischen r0 und r1 (Lava/Wasser), Tiefe unter dem Spiegel.</summary>
        public void Ring(Vector2 c, float r0, float r1, float depth) => _features.Add(new Feature { Kind = 2, C = c, R0 = r0, R1 = r1, Value = depth });

        float ApplyFeatures(float x, float z, float h)
        {
            for (int i = 0; i < _features.Count; i++)
            {
                var f = _features[i];
                float d = Mathf.Sqrt((x - f.C.x) * (x - f.C.x) + (z - f.C.y) * (z - f.C.y));
                switch (f.Kind)
                {
                    case 0:
                        if (d < f.R1) { float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f.R1 - d) / (f.R1 - f.R0))); h = Mathf.Max(h, Mathf.Lerp(h, f.Value, w)); }
                        break;
                    case 1:
                        if (d < f.R1) { float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f.R1 - d) / (f.R1 - f.R0))); h = Mathf.Min(h, Mathf.Lerp(h, _p.WaterLevel - f.Value, w)); }
                        break;
                    case 2:
                        float edge = Mathf.Min(d - f.R0, f.R1 - d);
                        if (edge > -1.5f) { float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge + 1.5f) / 1.5f)); h = Mathf.Min(h, Mathf.Lerp(h, _p.WaterLevel - f.Value, w)); }
                        break;
                }
            }
            return h;
        }

        // Wege (nach den Merkmalen, vor den Plätzen): gleichmäßige Rampe zwischen zwei Höhen, 6 m weicher Rand
        struct RoadSeg { public Vector2 A, B; public float HA, HB, HalfWidth; }
        readonly System.Collections.Generic.List<RoadSeg> _roads = new System.Collections.Generic.List<RoadSeg>();
        public const float RoadEdge = 6f;
        public void Road(Vector2 a, Vector2 b, float ha, float hb, float halfWidth = 3.5f) => _roads.Add(new RoadSeg { A = a, B = b, HA = ha, HB = hb, HalfWidth = halfWidth });
        public int RoadCount => _roads.Count;

        /// <summary>Abstand zum nächsten Weg (∞ ohne Wege).</summary>
        public float RoadDistance(float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _roads.Count; i++) { SegParam(_roads[i], x, z, out float d, out _); if (d - _roads[i].HalfWidth < best) best = d - _roads[i].HalfWidth; }
            return best;
        }

        static void SegParam(RoadSeg r, float x, float z, out float dist, out float t)
        {
            Vector2 ab = r.B - r.A, ap = new Vector2(x, z) - r.A;
            t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            dist = (ap - ab * t).magnitude;
        }

        float ApplyRoads(float x, float z, float h)
        {
            for (int i = 0; i < _roads.Count; i++)
            {
                var r = _roads[i];
                SegParam(r, x, z, out float d, out float t);
                if (d >= r.HalfWidth + RoadEdge) continue;
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r.HalfWidth + RoadEdge - d) / RoadEdge));
                float rh = Mathf.Lerp(r.HA, r.HB, Mathf.SmoothStep(0f, 1f, t));
                h = Mathf.Lerp(h, rh, w);
            }
            return h;
        }

        /// <summary>Ebnet einen Kreis auf eine Höhe ein (weicher Rand, 40 % des Radius).</summary>
        public void Flatten(Vector2 center, float radius, float height) => _flats.Add(new Vector4(center.x, center.y, radius, height));

        public float RawHeight(float x, float z)
        {
            float h = 0f, amp = 1f, f = _p.Frequency, norm = 0f;
            for (int i = 0; i < Mathf.Max(1, _p.Octaves); i++)
            {
                h += (Mathf.PerlinNoise(_ox + x * f, _oz + z * f) * 2f - 1f) * amp;
                norm += amp; amp *= 0.5f; f *= 2.03f;
            }
            h = h / norm * _p.Amplitude + _p.BaseHeight;
            if (_p.Terrace > 0.01f)
            {
                float t = h / _p.Terrace, fl = Mathf.Floor(t), fr = t - fl;
                h = (fl + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fr - 0.7f) / 0.3f))) * _p.Terrace;
            }
            if (_p.CanyonDepth > 0.01f)
            {
                float n = Mathf.Abs(Mathf.PerlinNoise(_cx + x * _p.CanyonFrequency, _cz + z * _p.CanyonFrequency) * 2f - 1f);
                float c = Mathf.Clamp01(1f - n / 0.08f);
                h -= c * c * _p.CanyonDepth;
            }
            float half = _size * 0.5f;
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) / half;
            h += Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 0.9f) / 0.1f)) * _p.EdgeMountainHeight;
            return h;
        }

        /// <summary>Welt-Lauf W1: Terrain-Operationen (Massiv, Gipfel, Schlucht, Krater …) – nach den Merkmalen, vor Wegen und Plätzen.</summary>
        public System.Collections.Generic.List<Landmarks.TerrainOpDef> Ops = new System.Collections.Generic.List<Landmarks.TerrainOpDef>();

        public float Height(float x, float z)
        {
            float h = _features.Count > 0 ? ApplyFeatures(x, z, RawHeight(x, z)) : RawHeight(x, z);
            if (Ops.Count > 0) h = Landmarks.TerrainOps.ApplyAll(Ops, x, z, h, _p.WaterLevel);
            if (_roads.Count > 0) h = ApplyRoads(x, z, h);
            for (int i = 0; i < _flats.Count; i++)
            {
                var f = _flats[i];
                float d = Mathf.Sqrt((x - f.x) * (x - f.x) + (z - f.y) * (z - f.y));
                if (d >= f.z) continue;
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f.z - d) / (f.z * 0.4f)));
                h = Mathf.Lerp(h, f.w, w);
            }
            return h;
        }

        public Color GroundColor(float x, float z, float h, float slope)
        {
            float t = Mathf.InverseLerp(_p.BaseHeight - _p.Amplitude * 0.6f, _p.BaseHeight + _p.Amplitude * 0.9f, h);
            Color c = t < 0.5f ? Color.Lerp(_p.GroundLow, _p.GroundMid, t * 2f) : Color.Lerp(_p.GroundMid, _p.GroundHigh, (t - 0.5f) * 2f);
            float n = Mathf.PerlinNoise(x * 0.05f + _ox, z * 0.05f + _oz);
            c *= 0.92f + n * 0.16f;
            if (h < _p.WaterLevel + 0.8f) c = Color.Lerp(c, _p.Shore, Mathf.Clamp01((_p.WaterLevel + 0.8f - h) / 0.8f));
            if (slope > 0.22f) c = Color.Lerp(c, _p.Cliff, Mathf.Clamp01((slope - 0.22f) * 2.5f));
            if (_roads.Count > 0 && h > _p.WaterLevel)
            {
                float rd = RoadDistance(x, z);
                if (rd < 1.5f) c = Color.Lerp(c, Color.Lerp(_p.Shore, _p.GroundMid, 0.35f), Mathf.Clamp01((1.5f - rd) / 1.5f) * 0.75f);
            }
            c.a = 1f;
            return c;
        }
    }
}
