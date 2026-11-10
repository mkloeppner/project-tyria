using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: alle Level-Kurven 1–80 als Daten (eine Quelle für Spiel, Rechner und Tests). Die Werte, die bis Lauf 10 Level 30 hatte,
    /// gehören jetzt Level 80. Primärattribute nach GW2-Art: langsam bis 20, danach steiler (Stützstellen, stückweise linear).
    /// XP: Level 1–30 wie bisher (400 + 120 × (L − 1)), ab 30 konstant 3.880 je Stufe – Inhalte geben ihr Level-XP, so dauert jedes Band gleich lang.
    /// Heldenpunkte: 4 je Stufe bis 30 (Bestand), danach 1 je Stufe bis 80.
    /// </summary>
    public static class LevelCurves
    {
        public const int Cap = 80, OldCap = 30;
        public static readonly int[] PrimaryLevels = { 1, 10, 20, 30, 40, 50, 60, 70, 80 };
        public static readonly float[] PrimaryValues = { 37f, 100f, 200f, 300f, 400f, 500f, 620f, 780f, 1000f };
        public const int XpPerLevelFrom30 = 400 + 120 * (OldCap - 1);
        public const int HeroPointsPerLevelLow = 4, HeroPointsPerLevelHigh = 1;

        public static float Primary(int level)
        {
            level = Mathf.Clamp(level, 1, Cap);
            for (int i = 1; i < PrimaryLevels.Length; i++)
                if (level <= PrimaryLevels[i])
                    return Mathf.Lerp(PrimaryValues[i - 1], PrimaryValues[i], (level - PrimaryLevels[i - 1]) / (float)(PrimaryLevels[i] - PrimaryLevels[i - 1]));
            return PrimaryValues[PrimaryValues.Length - 1];
        }

        /// <summary>0 (Level 1) … 1 (Level 80) entlang der Attributkurve – für alle level-abhängigen Basiswerte (Lebenspunkte, Waffenstärke, Gegner).</summary>
        public static float Fraction(int level) => (Primary(level) - PrimaryValues[0]) / (PrimaryValues[PrimaryValues.Length - 1] - PrimaryValues[0]);

        public static int XpToNext(int level)
        {
            if (level >= Cap) return int.MaxValue;
            return level < OldCap ? 400 + 120 * (Mathf.Max(1, level) - 1) : XpPerLevelFrom30;
        }

        public static int HeroPointsGainedAt(int level) => level <= OldCap ? HeroPointsPerLevelLow : HeroPointsPerLevelHigh;
        public static int HeroPointsForLevel(int level) { level = Mathf.Clamp(level, 1, Cap); return HeroPointsPerLevelLow * Mathf.Min(level, OldCap) + HeroPointsPerLevelHigh * Mathf.Max(0, level - OldCap); }

        /// <summary>Höchste regulär fallende Seltenheit (GW2-Verteilung): Edel bis 29, Meisterwerk ab 30, Selten ab 50, Exotisch ab 60; Aufgestiegen/Legendär nur auf 80 und nie als normale Beute.</summary>
        public static ItemRarity LootRarityCap(int level) => level >= 60 ? ItemRarity.Exotic : level >= 50 ? ItemRarity.Rare : level >= 30 ? ItemRarity.Masterwork : ItemRarity.Fine;

        /// <summary>Belohnungs-Faktor nach Inhalts-Level (1 = Level 8, wie bisher); wächst mit dem XP-Bedarf und bleibt ab 30 gleich.</summary>
        public static float RewardScale(int level) => XpToNext(Mathf.Clamp(level, 1, Cap - 1)) / (float)XpToNext(8);

        /// <summary>Elite-Spezialisierungen und Beherrschungs-XP ab dem Cap.</summary>
        public static int EliteLevel => Cap;
    }

    /// <summary>Lauf 11: Grund-EP der Inhalte auf Level 8 (× <see cref="LevelCurves.RewardScale"/>). Ziel: eine Zone allein trägt ihr Band (XP-Simulation in den Tests).</summary>
    public static class XpRewards
    {
        public const int Waypoint = 120, Poi = 150, Vista = 250, Challenge = 200, Insight = 100, Heart = 1200, Event = 600, ZoneCompletion = 1500;

        /// <summary>EP einer Zone bei einem Durchgang: Zonenabschluss + Herzen + Events (× Wiederholungen); Level je Inhalt.</summary>
        public static long ZoneXp(int[] waypointLv, int[] poiLv, int[] vistaLv, int[] challengeLv, int[] insightLv, int[] heartLv, int eventLevel, int events, int eventRepeats, int completionLevel)
        {
            double x = 0;
            foreach (var l in waypointLv) x += Waypoint * LevelCurves.RewardScale(l);
            foreach (var l in poiLv) x += Poi * LevelCurves.RewardScale(l);
            foreach (var l in vistaLv) x += Vista * LevelCurves.RewardScale(l);
            foreach (var l in challengeLv) x += Challenge * LevelCurves.RewardScale(l);
            foreach (var l in insightLv) x += Insight * LevelCurves.RewardScale(l);
            foreach (var l in heartLv) x += Heart * LevelCurves.RewardScale(l);
            x += (double)Event * LevelCurves.RewardScale(eventLevel) * events * eventRepeats;
            x += ZoneCompletion * LevelCurves.RewardScale(completionLevel);
            return (long)x;
        }

        /// <summary>EP für ein Band (von <paramref name="from"/> bis <paramref name="to"/>).</summary>
        public static long BandXp(int from, int to) { long s = 0; for (int l = from; l < to; l++) s += LevelCurves.XpToNext(l); return s; }
    }
}
