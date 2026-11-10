using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum MasteryLine { Reittiere, Gleiten, Umwelt, Kommandant, Greif }

    /// <summary>Kosten eines Rangs: Beherrschungs-EP (Training) + Beherrschungspunkte (Freischaltung).</summary>
    [System.Serializable]
    public struct MasteryRankCost { public string Name; [TextArea] public string Effect; public long Xp; public int Points; }

    /// <summary>
    /// Reine Beherrschungs-Logik (getestet, GW2): EP ab der Stufenobergrenze fließen in die gewählte Linie; ist der Rang voll trainiert
    /// und sind genug Punkte frei, wird er erlernt. Punkte aus Einblicken, Zonenabschluss, Weltboss/Meta und Erfolgen.
    /// </summary>
    public class MasteryModel
    {
        public class Track { public int Rank; public long Xp; }
        readonly Dictionary<MasteryLine, MasteryRankCost[]> _costs = new Dictionary<MasteryLine, MasteryRankCost[]>();
        public readonly Dictionary<MasteryLine, Track> Tracks = new Dictionary<MasteryLine, Track>();
        public MasteryLine? Selected;
        public int PointsEarned;
        public long TotalXp;
        public event System.Action<MasteryLine, int> RankLearned;

        public MasteryModel()
        {
            foreach (MasteryLine l in System.Enum.GetValues(typeof(MasteryLine))) Tracks[l] = new Track();
        }

        public void SetCosts(MasteryLine l, MasteryRankCost[] costs) => _costs[l] = costs ?? new MasteryRankCost[0];
        public MasteryRankCost[] Costs(MasteryLine l) => _costs.TryGetValue(l, out var c) ? c : new MasteryRankCost[0];
        public int MaxRank(MasteryLine l) => Costs(l).Length;
        public int Rank(MasteryLine l) => Tracks[l].Rank;
        public bool Has(MasteryLine l, int rank) => Tracks[l].Rank >= rank;

        public int PointsSpent
        {
            get
            {
                int n = 0;
                foreach (var kv in Tracks) { var c = Costs(kv.Key); for (int i = 0; i < kv.Value.Rank && i < c.Length; i++) n += c[i].Points; }
                return n;
            }
        }
        public int PointsAvailable => PointsEarned - PointsSpent;

        public bool IsMaxed(MasteryLine l) => Tracks[l].Rank >= MaxRank(l);
        public long XpNeeded(MasteryLine l) => IsMaxed(l) ? 0 : Costs(l)[Tracks[l].Rank].Xp;
        public int PointsNeeded(MasteryLine l) => IsMaxed(l) ? 0 : Costs(l)[Tracks[l].Rank].Points;
        public float Progress(MasteryLine l) => IsMaxed(l) ? 1f : Mathf.Clamp01(Tracks[l].Xp / (float)Mathf.Max(1, XpNeeded(l)));

        /// <summary>Beherrschungs-EP: gehen in die gewählte Linie (Überschuss bleibt im Rang stehen, bis Punkte da sind).</summary>
        public void AddXp(long xp)
        {
            if (xp <= 0) return;
            TotalXp += xp;
            if (!Selected.HasValue || IsMaxed(Selected.Value)) return;
            var t = Tracks[Selected.Value];
            t.Xp = System.Math.Min(t.Xp + xp, XpNeeded(Selected.Value));
            TryLearn(Selected.Value);
        }

        public void AddPoints(int n) { if (n <= 0) return; PointsEarned += n; foreach (MasteryLine l in System.Enum.GetValues(typeof(MasteryLine))) TryLearn(l); }

        /// <summary>Rang erlernen, wenn trainiert und genug Punkte frei. true = gerade erlernt.</summary>
        public bool TryLearn(MasteryLine l)
        {
            if (IsMaxed(l)) return false;
            var t = Tracks[l];
            if (t.Xp < XpNeeded(l) || PointsAvailable < PointsNeeded(l)) return false;
            t.Rank++; t.Xp = 0;
            RankLearned?.Invoke(l, t.Rank);
            if (Selected == l && IsMaxed(l)) Selected = null;
            return true;
        }

        public static string LineName(MasteryLine l)
        {
            switch (l)
            {
                case MasteryLine.Reittiere: return "Reittier-Training";
                case MasteryLine.Gleiten: return "Gleiten";
                case MasteryLine.Umwelt: return "Umweltkunde";
                case MasteryLine.Greif: return "Greif";
                default: return "Kommandant";
            }
        }
    }

    /// <summary>Wirkungen der Ränge (rein, getestet).</summary>
    public static class MasteryEffects
    {
        public static float MountHealth(MasteryModel m) => m.Has(MasteryLine.Reittiere, 1) ? 1.25f : 1f;
        public static float Leap(MasteryModel m) => m.Has(MasteryLine.Reittiere, 2) ? 1.2f : 1f;
        public static float Charge(MasteryModel m) => m.Has(MasteryLine.Reittiere, 3) ? 0.7f : 1f;
        public static float MountSpeed(MasteryModel m) => m.Has(MasteryLine.Reittiere, 4) ? 1.1f : 1f;
        public static bool CanGlide(MasteryModel m) => m.Has(MasteryLine.Gleiten, 1);
        public static bool UsesUpdrafts(MasteryModel m) => m.Has(MasteryLine.Gleiten, 2);
        public static float GlideSpeed(MasteryModel m) => m.Has(MasteryLine.Gleiten, 3) ? 11f : 8.5f;
        public static float GlideSink(MasteryModel m) => m.Has(MasteryLine.Gleiten, 3) ? 2.2f : 3f;
        public static bool PoisonMistImmune(MasteryModel m) => m.Has(MasteryLine.Umwelt, 1);
        public static bool CanBounce(MasteryModel m) => m.Has(MasteryLine.Umwelt, 2);
        public static float LavaFactor(MasteryModel m) => m.Has(MasteryLine.Umwelt, 3) ? 0f : 1f;
        public static bool SeesHiddenChests(MasteryModel m) => m.Has(MasteryLine.Umwelt, 4);
        public static bool AutoLoot(MasteryModel m) => m.Has(MasteryLine.Kommandant, 1);
        public static bool GriffonDive(MasteryModel m) => m.Has(MasteryLine.Greif, 1);        // Lauf 6: Sturzflug
        public static bool GriffonEndurance(MasteryModel m) => m.Has(MasteryLine.Greif, 2);   // Lauf 6: flacheres Gleiten
        public static float TravelDiscount(MasteryModel m) => m.Has(MasteryLine.Kommandant, 3) ? 0.4f : m.Has(MasteryLine.Kommandant, 2) ? 0.2f : 0f;
    }
}
