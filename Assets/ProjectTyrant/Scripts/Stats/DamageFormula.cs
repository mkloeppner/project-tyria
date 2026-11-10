using UnityEngine;

namespace ProjectTyrant
{
    public struct DamageResult
    {
        public float Amount;
        public bool Crit;
        public bool Glancing;
        public bool Missed;
    }

    /// <summary>GW2-Schadensformeln. Direkt = Waffenstärke × Kraft × Koeffizient / Rüstung.</summary>
    public static class DamageFormula
    {
        public static float WeaponStrength(int level) => Mathf.Lerp(55f, 1000f, LevelCurves.Fraction(level));

        public static float Direct(float weaponStrength, float power, float coefficient, float targetArmor)
            => Mathf.Max(0f, weaponStrength * power * coefficient / Mathf.Max(1f, targetArmor));

        public static float Heal(float baseHeal, float healingPower, float coefficient) => Mathf.Max(0f, baseHeal + healingPower * coefficient);

        /// <summary>Basisheilung auf Level 80 → level-skaliert.</summary>
        public static float ScaledBase(float baseAt80, int level) => baseAt80 * Mathf.Max(0.08f, AttributeSet.LevelScale(level));

        /// <summary>Schaden je Stapel und Tick (1 s) eines Schadenszustands (GW2-Basis auf Level 80, skaliert).</summary>
        public static float ConditionTick(EffectType type, float conditionDamage, int level)
        {
            float s = Mathf.Max(0.08f, AttributeSet.LevelScale(level));
            switch (type)
            {
                case EffectType.Burning: return 131.5f * s + 0.155f * conditionDamage;
                case EffectType.Bleeding: return 22f * s + 0.06f * conditionDamage;
                case EffectType.Poison: return 33.5f * s + 0.06f * conditionDamage;
                case EffectType.Torment: return 31.8f * s + 0.09f * conditionDamage;
                case EffectType.Confusion: return 11f * s + 0.0975f * conditionDamage;
            }
            return 0f;
        }

        /// <summary>Confusion-Schaden beim Einsatz einer Fähigkeit (je Stapel).</summary>
        public static float ConfusionOnSkill(float conditionDamage, int level) => 49.5f * Mathf.Max(0.08f, AttributeSet.LevelScale(level)) + 0.0975f * conditionDamage;

        /// <summary>Vollständige Berechnung aus Angreifer-Attributen gegen Ziel-Rüstung inkl. Krit/Streifen.</summary>
        public static DamageResult Compute(AttributeSet attacker, float coefficient, float targetArmor, System.Random rng, float weaponStrengthOverride = -1f, float outgoingMultiplier = 1f, bool weakened = false, float extraCritChance = 0f)
        {
            var r = new DamageResult();
            float ws = weaponStrengthOverride > 0f ? weaponStrengthOverride : WeaponStrength(attacker.Level);
            float dmg = Direct(ws, attacker.Get(Attr.Power), coefficient, targetArmor) * outgoingMultiplier;
            double roll = rng != null ? rng.NextDouble() : Random.value;
            if (roll < Mathf.Clamp01(attacker.CritChance + extraCritChance)) { r.Crit = true; dmg *= attacker.CritDamage; }
            if (weakened)
            {
                double g = rng != null ? rng.NextDouble() : Random.value;
                if (g < 0.5) { r.Glancing = true; dmg *= 0.5f; }
            }
            r.Amount = dmg;
            return r;
        }
    }
}
