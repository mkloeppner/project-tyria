using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Rollen-Modifikatoren der Archetypen (die einzigen Handwerte neben der Kurve).</summary>
    public enum EnemyRole { Standard, Ranged, Healer, Hunter, Netter, Caller, Lurker }

    /// <summary>
    /// Balance B1: erwarteter Spieler-Stand je Level (Ausrüstung aus Herzen/Loot, Berserker-ähnlich) – Kalibrierung über die Trainingspuppen-Messung.
    /// </summary>
    [System.Serializable]
    public class ExpectedPlayer
    {
        [Tooltip("Vitalität durch Ausrüstung als Anteil der Primärbasis")] public float GearVitality = 0.15f;
        [Tooltip("Rüstung (Verteidigung) durch Ausrüstung als Anteil der Primärbasis")] public float GearDefense = 0.9f;
        [Tooltip("Kraft durch Ausrüstung als Anteil der Primärbasis")] public float GearPower = 0.35f;
        [Tooltip("Koeffizient je Sekunde (Auto-Angriff + 2 Skills), aus der Trainingspuppen-Messung (B2: 0,9 → 1,85, Zeit-bis-Kill war halb so lang wie geplant)")] public float CoefPerSecond = 1.85f;

        public float Health(int level) => Mathf.Lerp(AttributeSet.ClassBaseHealthL1, AttributeSet.ClassBaseHealthL80, (Mathf.Clamp(level, 1, AttributeSet.MaxLevel) - 1) / (float)(AttributeSet.MaxLevel - 1))
                                         + AttributeSet.PrimaryBase(level) * (1f + GearVitality) * 10f;
        public float Armor(int level) => AttributeSet.PrimaryBase(level) * (1f + GearDefense);
        public float Power(int level) => AttributeSet.PrimaryBase(level) * (1f + GearPower);
        /// <summary>Erwarteter Direktschaden je Sekunde gegen ein Ziel mit Rüstung <paramref name="targetArmor"/>.</summary>
        public float Dps(int level, float targetArmor) => DamageFormula.WeaponStrength(level) * Power(level) * CoefPerSecond / Mathf.Max(1f, targetArmor);
    }

    /// <summary>
    /// Balance B1: Gegner-Attribute je Level, Rang und Gefahrenstufe aus einer Kurve („Zielbild D“):
    /// Lebenspunkte aus dem Zeit-bis-Kill-Korridor, Angriffskoeffizient aus dem Zeit-bis-Tod-Ziel gegen drei Trash (bzw. einen Veteranen) mit Sicherheitsabstand,
    /// gedeckelt durch den Anteil je Treffer. Archetypen tragen nur Rollen-Modifikatoren.
    /// </summary>
    public static class EnemyCurve
    {
        public static ExpectedPlayer Player = new ExpectedPlayer();
        /// <summary>Rüstung der Gegner = Faktor × Primärbasis (wie bisher Zähigkeit 1 + ArmorFactor 1,6).</summary>
        public const float EnemyArmorFactor = 2.6f;
        /// <summary>Sicherheitsabstand auf die Zeit-bis-Tod-Ziele (Krit, Zustände, Überlappung): Stufe 0–2 großzügig, Stufe 3–5 knapp (Korridore).</summary>
        public static float TtdMargin(int danger) => danger <= 2 ? 1.3f : 1.1f;
        /// <summary>Champion: Lebenspunkte und Schaden relativ zum Veteranen.</summary>
        /// <summary>Veteranen haben Sonderangriffe (Stampfer, Zug, Wurf) zusätzlich zum Auto-Angriff – Messung Stufe 3: 13 s statt ≥ 16 s.</summary>
        public const float VeteranAbilityMargin = 1.4f;
        public const float ChampionHp = 4f, ChampionDamage = 1.3f;   // Grolmok: Zeit-bis-Tod eines untätigen Spielers ≥ 20 s

        public static float RoleHealth(EnemyRole r) => r == EnemyRole.Ranged ? 0.8f : r == EnemyRole.Netter ? 0.85f : 1f;
        public static float RoleDamage(EnemyRole r) => r == EnemyRole.Healer ? 0.7f : r == EnemyRole.Caller ? 0.8f : 1f;
        public static float RoleSpeed(EnemyRole r) => r == EnemyRole.Hunter ? 1.1f : 1f;

        public static float EnemyArmor(int level) => AttributeSet.PrimaryBase(level) * EnemyArmorFactor;

        /// <summary>Ziel-Zeit-bis-Kill (Mitte des Korridors) je Rang.</summary>
        public static float TargetTtk(DangerSettings s, EnemyTier tier)
        {
            float trash = (s.TtkTrashMin + s.TtkTrashMax) * 0.5f;
            if (tier == EnemyTier.Trash || tier == EnemyTier.None) return trash;
            float vet = s.TtkVeteranMax > 0f ? (s.TtkVeteranMin + s.TtkVeteranMax) * 0.5f : trash * 6f;
            return tier == EnemyTier.Veteran ? vet : vet * ChampionHp;
        }

        /// <summary>Lebenspunkte eines Gegners: erwarteter Spieler-DPS × Ziel-Zeit-bis-Kill.</summary>
        public static float Health(int level, EnemyTier tier, DangerSettings s, EnemyRole role = EnemyRole.Standard)
            => Player.Dps(level, EnemyArmor(level)) * TargetTtk(s, tier) * RoleHealth(role);

        /// <summary>Schaden je Treffer als Anteil der Spieler-HP (gleiches Level), aus Zeit-bis-Tod-Ziel und Treffer-Deckel.</summary>
        public static float HitShare(DangerSettings s, EnemyTier tier, float attackInterval)
        {
            float interval = Mathf.Max(0.5f, attackInterval);
            float trash = Mathf.Min(s.HitShare, interval / (3f * (s.TtdThreeTrash + interval) * TtdMargin(s.Level)));   // + interval: Treffer fallen diskret
            if (tier == EnemyTier.Trash || tier == EnemyTier.None) return trash;
            float ttdVet = s.TtdVeteran > 0f ? s.TtdVeteran : s.TtdThreeTrash;
            float vet = Mathf.Min(s.HitShare * 1.5f, interval / ((ttdVet + interval) * TtdMargin(s.Level) * VeteranAbilityMargin));
            return tier == EnemyTier.Veteran ? vet : Mathf.Min(s.TelegraphHitShare * 1.5f, vet * ChampionDamage);
        }

        /// <summary>Angriffskoeffizient, der gegen den erwarteten Spieler des Levels den Treffer-Anteil ergibt (Gegner-Kraft = Primärbasis × Attributfaktor).</summary>
        public static float Coefficient(int level, EnemyTier tier, DangerSettings s, float attackInterval, float attributeFactor = 1f, EnemyRole role = EnemyRole.Standard)
        {
            float share = HitShare(s, tier, attackInterval) * RoleDamage(role);
            float power = AttributeSet.PrimaryBase(level) * attributeFactor;
            return share * Player.Health(level) * Player.Armor(level) / (DamageFormula.WeaponStrength(level) * Mathf.Max(1f, power));
        }

        /// <summary>Telegraph-Koeffizient (Bodenstoß) mit eigenem Deckel.</summary>
        public static float TelegraphCoefficient(int level, DangerSettings s, float attributeFactor = 1f)
        {
            float power = AttributeSet.PrimaryBase(level) * attributeFactor;
            return s.TelegraphHitShare * Player.Health(level) * Player.Armor(level) / (DamageFormula.WeaponStrength(level) * Mathf.Max(1f, power));
        }

        /// <summary>Aufladung eines Angriffs mit Fairness-Minimum der Stufe (0–1: 1,2 s · 2–3: 1,0 s · 4–5: 0,75 s).</summary>
        public static float Windup(float archetypeWindup, DangerSettings s) => Mathf.Max(archetypeWindup, s != null ? s.MinTelegraph : 0.6f);

        /// <summary>Simulierte Zeit-bis-Tod eines untätigen Spielers gegen n Gegner (gleiches Level, ohne Krit).</summary>
        public static float SimTtd(int level, EnemyTier tier, DangerSettings s, float attackInterval, int count)
        {
            float hp = Player.Health(level);
            float coef = Coefficient(level, tier, s, attackInterval);
            float hit = DamageFormula.Direct(DamageFormula.WeaponStrength(level), AttributeSet.PrimaryBase(level), coef, Player.Armor(level));
            float t = 0f, next = Windup(0.6f, s);
            while (hp > 0f && t < 600f) { t = next; hp -= hit * count; next += attackInterval; }
            return t;
        }
    }
}
