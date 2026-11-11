using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Aufstiegs-Vorlage (rein): drei Stufen mit Rastplateaus und Kontrollpunkten.
    /// Stufe 1 zu Fuß (Serpentine/Treppe), Stufe 2 Springer (Alternative: langer Grat zu Fuß), Stufe 3 Gipfel (Greif aus dem Startturm, Himmelsschuppe, langer Kletterpfad).
    /// </summary>
    public static class ClimbTemplate
    {
        public static readonly float[] StageFractions = { 0.35f, 0.65f, 1f };

        public static List<StageDef> Create(Vector3 baseP, Vector3 peak, int zoneLevelMax)
        {
            var list = new List<StageDef>();
            string[] names = { "Rastplateau", "Hochplateau", "Gipfel" };
            RewardKind[] rewards = { RewardKind.Vista, RewardKind.Insight, RewardKind.Chest };
            for (int i = 0; i < 3; i++)
            {
                float f = StageFractions[i];
                var p = Vector3.Lerp(baseP, peak, f); p.y = Mathf.Lerp(baseP.y, peak.y, f);
                var g = i == 0 ? GateDef.Foot
                      : i == 1 ? new GateDef { Keys = Traversal.Springer, FootAlternative = true }
                      : new GateDef { Keys = Traversal.Griffon | Traversal.Skyscale, FootAlternative = true };
                g.ReturnGate = !GateRules.InBand(g.Keys, zoneLevelMax);
                var s = new StageDef { Index = i + 1, Name = names[i], Position = p, Gate = g, Reward = rewards[i] };
                // Kontrollpunkte: Plateau selbst + Zwischenpunkte an Abgründen (alle ≈ 15 Höhenmeter)
                var prev = i == 0 ? baseP : list[i - 1].Position;
                int n = Mathf.Max(1, Mathf.CeilToInt((p.y - prev.y) / 15f));
                for (int k = 1; k <= n; k++) s.Checkpoints.Add(Vector3.Lerp(prev, p, k / (float)n));
                list.Add(s);
            }
            return list;
        }

        /// <summary>Erreichbar mit genau dem vorgesehenen Schlüssel (oder zu Fuß, wenn es eine Fuß-Alternative gibt und <paramref name="allowLongFoot"/>).</summary>
        public static bool Reachable(StageDef s, Traversal owned, bool allowLongFoot) => GateRules.Satisfied(s.Gate, owned) || (allowLongFoot && s.Gate.FootAlternative);

        /// <summary>Abfahrt vom Gipfel zum Ziel im Gleitflug (Greif 1,6 × 7 m/s, Gleitzahl 6) – unter 30 s?</summary>
        public static float DescentSeconds(Vector3 peak, Vector3 target, float glideSpeed = 11.2f, float glideRatio = 6f)
        {
            float horiz = new Vector2(target.x - peak.x, target.z - peak.z).magnitude;
            float drop = peak.y - target.y;
            if (drop <= 0f) return float.PositiveInfinity;
            float reach = drop * glideRatio;
            if (reach < horiz) return float.PositiveInfinity;
            // Sturzflug + Gleiten: in der Praxis schneller als reines Gleiten – hier konservativ: Gleiten über die Strecke
            return Mathf.Max(horiz / glideSpeed, drop / 21f);
        }
    }

    /// <summary>Kontrollpunkte (GW2-Sprungrätsel-Logik): Sturz unter die Schwelle setzt an den letzten erreichten Kontrollpunkt zurück.</summary>
    public class CheckpointTracker
    {
        readonly List<Vector3> _points;
        public int Last { get; private set; } = -1;
        public float FallMargin = 6f;
        public CheckpointTracker(List<Vector3> points) { _points = points ?? new List<Vector3>(); }
        public Vector3? Current => Last >= 0 && Last < _points.Count ? _points[Last] : (Vector3?)null;

        /// <summary>Erreicht der Spieler einen Kontrollpunkt (≤ 4 m), wird er der aktuelle.</summary>
        public bool Visit(Vector3 pos, float radius = 4f)
        {
            for (int i = _points.Count - 1; i > Last; i--)
                if ((pos - _points[i]).sqrMagnitude <= radius * radius) { Last = i; return true; }
            return false;
        }

        /// <summary>Ist der Spieler tiefer gefallen als der letzte Kontrollpunkt minus Spielraum? Dann zurücksetzen (Ziel in <paramref name="reset"/>).</summary>
        public bool ShouldReset(Vector3 pos, out Vector3 reset)
        {
            reset = default;
            var c = Current; if (!c.HasValue) return false;
            if (pos.y >= c.Value.y - FallMargin) return false;
            reset = c.Value + Vector3.up * 0.5f; return true;
        }
    }

    /// <summary>Sichtbarkeit eines Wahrzeichens (rein): Sichtlinien von Stichproben-Punkten zur Spitze, verdeckt nur durch Gelände.</summary>
    public static class VisibilityModel
    {
        public const float EyeHeight = 1.7f;

        public static bool LineOfSight(System.Func<float, float, float> height, Vector3 from, Vector3 to, float step = 8f)
        {
            float dist = Vector3.Distance(from, to);
            int n = Mathf.Max(2, Mathf.CeilToInt(dist / step));
            for (int i = 1; i < n; i++)
            {
                var p = Vector3.Lerp(from, to, i / (float)n);
                if (height(p.x, p.z) > p.y + 0.5f) return false;
            }
            return true;
        }

        /// <summary>Anteil (0..1) der Punkte, von denen mindestens einer der Zielpunkte (Spitze, oberes Drittel) sichtbar ist.</summary>
        public static float Fraction(System.Func<float, float, float> height, IList<Vector2> samples, IList<Vector3> targets, float maxDistance = 1600f)
        {
            if (samples == null || samples.Count == 0) return 0f;
            int seen = 0;
            foreach (var s in samples)
            {
                var eye = new Vector3(s.x, height(s.x, s.y) + EyeHeight, s.y);
                foreach (var t in targets)
                {
                    if (Vector3.Distance(eye, t) > maxDistance) continue;
                    if (LineOfSight(height, eye, t)) { seen++; break; }
                }
            }
            return seen / (float)samples.Count;
        }

        /// <summary>Deterministische Stichprobe begehbarer Punkte (über Wasser, Hang ≤ 40°).</summary>
        public static List<Vector2> WalkableSamples(System.Func<float, float, float> height, float size, float waterLevel, int count, int seed)
        {
            var r = new System.Random(seed);
            var list = new List<Vector2>();
            float half = size * 0.45f;
            for (int tries = 0; tries < count * 20 && list.Count < count; tries++)
            {
                float x = (float)(r.NextDouble() * 2 - 1) * half, z = (float)(r.NextDouble() * 2 - 1) * half;
                float h = height(x, z);
                if (h < waterLevel + 0.2f) continue;
                float sx = height(x + 2f, z) - height(x - 2f, z), sz = height(x, z + 2f) - height(x, z - 2f);
                if (Mathf.Sqrt(sx * sx + sz * sz) / 4f > 0.84f) continue;   // tan 40°
                list.Add(new Vector2(x, z));
            }
            return list;
        }
    }

    /// <summary>Reittier-Abkürzungen (rein): Zeitersparnis gegenüber dem Fußweg.</summary>
    public static class ShortcutModel
    {
        public const float RunSpeed = 7f;
        public static float MountSpeed(Traversal k)
        {
            if ((k & Traversal.Griffon) != 0) return 7f * 1.6f;
            if ((k & Traversal.Raptor) != 0) return 10.5f;
            if ((k & Traversal.Skimmer) != 0) return 9.5f;
            if ((k & Traversal.Springer) != 0) return 8.5f;
            if ((k & Traversal.Skyscale) != 0) return 9.8f;
            if ((k & Traversal.DuneRunner) != 0) return 12f;
            return RunSpeed;
        }
        public static float SavedSeconds(ShortcutDef s) => s.FootPath / RunSpeed - s.MountPath / MountSpeed(s.Key);
        public const float MinSaving = 90f;
    }

    /// <summary>Portal-Culling der Innenräume (rein): Innen-Details nur sichtbar, wenn der Spieler drinnen ist; Außen-Zierrat nur draußen.</summary>
    public static class InteriorCullingModel
    {
        public static void State(bool playerInside, out bool interiorVisible, out bool exteriorDetailVisible)
        {
            interiorVisible = playerInside; exteriorDetailVisible = !playerInside;
        }
        public static bool Inside(Bounds b, Vector3 p) => b.Contains(p);
    }

    /// <summary>Geheimnisse liegen außerhalb aller Marker-Radien.</summary>
    public static class SecretRules
    {
        public const float MarkerClearance = 25f;
        public static bool OutsideMarkers(Vector3 p, IEnumerable<Vector3> markers, float clearance = MarkerClearance)
        {
            foreach (var m in markers) if (new Vector2(p.x - m.x, p.z - m.z).sqrMagnitude < clearance * clearance) return false;
            return true;
        }
        /// <summary>Täglich einmal: Schlüssel je Tag.</summary>
        public static string DailyFlag(string id, System.DateTime day) => "secret:" + id + ":" + day.ToString("yyyyMMdd");
    }
}
