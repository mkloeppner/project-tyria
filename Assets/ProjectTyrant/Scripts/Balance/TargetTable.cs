using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Bot-Profile aus „Zielbild E“.</summary>
    public enum BotProfileKind { Beginner, Experienced }

    /// <summary>Balance B1: Zielwert-Korridor (NaN = offen).</summary>
    public struct Corridor
    {
        public float Min, Max;
        public Corridor(float min, float max) { Min = min; Max = max; }
        public static Corridor AtMost(float v) => new Corridor(float.NaN, v);
        public static Corridor AtLeast(float v) => new Corridor(v, float.NaN);
        public static Corridor Between(float a, float b) => new Corridor(a, b);
        public bool Open => float.IsNaN(Min) && float.IsNaN(Max);
        public bool Contains(float v) => !float.IsNaN(v) && (float.IsNaN(Min) || v >= Min - 1e-4f) && (float.IsNaN(Max) || v <= Max + 1e-4f);
        public override string ToString() => float.IsNaN(Min) ? "≤ " + Max.ToString("0.##") : float.IsNaN(Max) ? "≥ " + Min.ToString("0.##") : Min.ToString("0.##") + "–" + Max.ToString("0.##");
    }

    public enum Metric { Deaths30, EscapeRate, EventRate, TtdThreeTrash }

    public struct TargetResult
    {
        public BotProfileKind Profile; public Metric Metric; public int Danger; public float Value; public Corridor Target; public int Samples;
        public bool Pass => Target.Open || Target.Contains(Value);
        public bool Measured => Samples > 0 && !float.IsNaN(Value);
        public override string ToString() => Profile + " · " + Metric + " · Stufe " + Danger + ": " + (float.IsNaN(Value) ? "–" : Value.ToString("0.##")) + " (Ziel " + Target + ", n=" + Samples + ") " + (Pass ? "OK" : "VERFEHLT");
    }

    /// <summary>Balance B1: Zielwert-Tabelle „Zielbild E“ und ihre Auswertung (Median über mindestens drei Seeds je Zelle).</summary>
    public static class TargetTable
    {
        public const int MinSeeds = 3;
        public const float MinSessionSeconds = 1500f;

        public static Corridor Get(BotProfileKind p, Metric m, int danger)
        {
            switch (m)
            {
                case Metric.Deaths30:
                    if (p == BotProfileKind.Beginner) return danger == 0 ? Corridor.AtMost(0) : danger == 1 ? Corridor.AtMost(1) : danger == 2 ? Corridor.AtMost(2) : danger == 3 ? Corridor.AtMost(3) : danger == 4 ? Corridor.Between(3, 5) : Corridor.Between(4, 7);
                    return danger <= 1 ? Corridor.AtMost(0) : danger <= 3 ? Corridor.AtMost(1) : danger == 4 ? Corridor.Between(1, 2) : Corridor.Between(2, 4);
                case Metric.EscapeRate:
                    if (p == BotProfileKind.Beginner) return danger == 0 ? Corridor.AtLeast(0.95f) : danger == 1 ? Corridor.AtLeast(0.85f) : danger == 2 ? Corridor.AtLeast(0.70f) : danger == 3 ? Corridor.AtLeast(0.55f) : danger == 4 ? Corridor.Between(0.30f, 0.45f) : Corridor.Between(0.10f, 0.25f);
                    return danger == 0 ? Corridor.AtLeast(1f) : danger == 1 ? Corridor.AtLeast(0.95f) : danger == 2 ? Corridor.AtLeast(0.90f) : danger == 3 ? Corridor.AtLeast(0.80f) : danger == 4 ? Corridor.Between(0.55f, 0.70f) : Corridor.Between(0.30f, 0.45f);
                case Metric.EventRate:
                    if (p != BotProfileKind.Beginner || danger > 3) return new Corridor(float.NaN, float.NaN);
                    return danger == 0 ? Corridor.AtLeast(0.95f) : danger == 1 ? Corridor.AtLeast(0.85f) : danger == 2 ? Corridor.AtLeast(0.65f) : Corridor.AtLeast(0.45f);
                default:
                    return Corridor.AtLeast(DangerTable.Default(danger).TtdThreeTrash);
            }
        }

        static float Value(TelemetrySession s, Metric m, int danger)
        {
            switch (m)
            {
                case Metric.Deaths30: return s.DeathsPer30;
                case Metric.EscapeRate: return s.EscapeRate;
                case Metric.EventRate: return s.EventRate(danger);   // Event-Stufe = Gefahrenstufe der Zelle (0–3)
                default: return s.MedianValue(TelemetryKind.Ttd);
            }
        }

        /// <summary>Wertet Sessions aus (Zelle = Profil × Stufe der Session); Median über Seeds.</summary>
        public static List<TargetResult> Evaluate(IEnumerable<TelemetrySession> sessions)
        {
            var cells = new Dictionary<(BotProfileKind, Metric, int), List<float>>();
            foreach (var s in sessions)
            {
                if (s == null || s.Danger < 0 || s.Seconds < MinSessionSeconds) continue;   // nur volle Sessions (30 min)
                if (!System.Enum.TryParse(s.Profile, out BotProfileKind p)) continue;
                foreach (Metric m in System.Enum.GetValues(typeof(Metric)))
                {
                    float v = Value(s, m, s.Danger);
                    if (float.IsNaN(v)) continue;
                    var key = (p, m, s.Danger);
                    if (!cells.TryGetValue(key, out var l)) cells[key] = l = new List<float>();
                    l.Add(v);
                }
            }
            var res = new List<TargetResult>();
            foreach (var kv in cells)
                res.Add(new TargetResult { Profile = kv.Key.Item1, Metric = kv.Key.Item2, Danger = kv.Key.Item3, Value = TelemetrySession.Median(kv.Value), Target = Get(kv.Key.Item1, kv.Key.Item2, kv.Key.Item3), Samples = kv.Value.Count });
            res.Sort((a, b) => a.Profile != b.Profile ? a.Profile.CompareTo(b.Profile) : a.Metric != b.Metric ? a.Metric.CompareTo(b.Metric) : a.Danger.CompareTo(b.Danger));
            return res;
        }
    }
}
