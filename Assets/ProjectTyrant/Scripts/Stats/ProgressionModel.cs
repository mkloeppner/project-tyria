using System;

namespace ProjectTyrant
{
    /// <summary>Level 1–80 und Erfahrung. Kurve: 400 + 120 × (Level − 1) XP pro Stufe (Zone ≈ Level 15).</summary>
    [Serializable]
    public class ProgressionModel
    {
        public int Level = 1;
        public int Xp;
        /// <summary>Heldenpunkte je Stufe (auch Stufe 1). Ziel: ~60 % aller Freischaltungen auf Stufe 15 inkl. Heldenherausforderungen.</summary>
        public const int HeroPointsPerLevel = 4;
        public int HeroPoints = HeroPointsPerLevel;
        public int HeroPointsSpent;

        public event Action<int> LeveledUp;
        public event Action Changed;

        public static int XpToNext(int level) => LevelCurves.XpToNext(level);   // Lauf 11: 1–30 wie bisher, ab 30 konstant

        public static int TotalXpForLevel(int level)
        {
            int sum = 0;
            for (int l = 1; l < level; l++) sum += XpToNext(l);
            return sum;
        }

        public float XpFraction => Level >= AttributeSet.MaxLevel ? 1f : Xp / (float)XpToNext(Level);
        public int AvailableHeroPoints => HeroPoints - HeroPointsSpent;

        /// <summary>Fügt XP hinzu; gibt die Anzahl der Stufenaufstiege zurück.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || Level >= AttributeSet.MaxLevel) return 0;
            Xp += amount;
            int ups = 0;
            while (Level < AttributeSet.MaxLevel && Xp >= XpToNext(Level))
            {
                Xp -= XpToNext(Level);
                Level++;
                HeroPoints += LevelCurves.HeroPointsGainedAt(Level);
                ups++;
                LeveledUp?.Invoke(Level);
            }
            if (Level >= AttributeSet.MaxLevel) Xp = 0;
            Changed?.Invoke();
            return ups;
        }

        public void SetLevel(int level)
        {
            int target = Math.Max(1, Math.Min(AttributeSet.MaxLevel, level));
            while (Level < target) { Level++; HeroPoints += LevelCurves.HeroPointsGainedAt(Level); LeveledUp?.Invoke(Level); }
            if (target < Level) Level = target;
            Xp = 0;
            Changed?.Invoke();
        }

        public static int HeroPointsForLevel(int level) => LevelCurves.HeroPointsForLevel(level);   // Lauf 11: 4 je Stufe bis 30, danach 1

        public void GrantHeroPoints(int n) { HeroPoints += n; Changed?.Invoke(); }

        public bool TrySpendHeroPoints(int n)
        {
            if (n < 0 || AvailableHeroPoints < n) return false;
            HeroPointsSpent += n; Changed?.Invoke(); return true;
        }

        public void RefundHeroPoints(int n) { HeroPointsSpent = Math.Max(0, HeroPointsSpent - n); Changed?.Invoke(); }

        /// <summary>XP für einen Kill nach Gegnerlevel und Rang (GW2-ähnlich: grau unter −5 Level, Bonus über eigenem Level).</summary>
        public static int KillXp(int playerLevel, int enemyLevel, EnemyTier tier)
        {
            float baseXp = 20f + 6f * enemyLevel;
            float diff = enemyLevel - playerLevel;
            float lvlMul = diff <= -6 ? 0.2f : 1f + Math.Max(-0.6f, Math.Min(0.5f, diff * 0.1f));
            float tierMul = tier == EnemyTier.Champion ? 10f : tier == EnemyTier.Veteran ? 3f : 1f;
            return (int)Math.Round(baseXp * lvlMul * tierMul);
        }
    }
}
