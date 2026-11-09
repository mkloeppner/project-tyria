using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Unterbrechung (rein, GW2): Kontroll-Effekte brechen Aktivierungen ab; der unterbrochene Skill ist 5 s gesperrt.</summary>
    public static class InterruptRule
    {
        public const float Lockout = 5f;
        public static bool Interrupts(EffectType t) => t == EffectType.Stun || t == EffectType.Fear || t == EffectType.Taunt;
        /// <summary>Neue Abklingzeit des unterbrochenen Skills (nie kürzer als die laufende).</summary>
        public static float LockedCooldown(float currentCooldown) => Mathf.Max(currentCooldown, Lockout);
    }

    /// <summary>Hit-Stop und Kamera-Impuls (rein): schwere Treffer (≥ 10 % der Ziel-HP) und Krits frieren Angreifer und Ziel 40–80 ms ein.</summary>
    public static class HitStopModel
    {
        public const float Min = 0.04f, Max = 0.08f, HeavyFraction = 0.1f;
        public enum DamageClass { Light, Medium, Heavy, Huge }

        public static DamageClass Classify(float amount, float targetMaxHealth)
        {
            float f = targetMaxHealth > 0f ? amount / targetMaxHealth : 0f;
            return f >= 0.25f ? DamageClass.Huge : f >= HeavyFraction ? DamageClass.Heavy : f >= 0.04f ? DamageClass.Medium : DamageClass.Light;
        }

        /// <summary>Dauer in s (0 = kein Hit-Stop).</summary>
        public static float Duration(float amount, float targetMaxHealth, bool crit)
        {
            var c = Classify(amount, targetMaxHealth);
            if (c == DamageClass.Light && !crit) return 0f;
            float f = targetMaxHealth > 0f ? Mathf.Clamp01(amount / targetMaxHealth / 0.3f) : 0f;
            float d = Mathf.Lerp(Min, Max, f);
            if (crit) d = Mathf.Max(d, 0.05f);
            return Mathf.Clamp(d, Min, Max);
        }

        /// <summary>Kamera-Impuls-Stärke nach Schadensklasse (0 = keiner).</summary>
        public static float Impulse(DamageClass c, bool crit) => c == DamageClass.Huge ? 0.6f : c == DamageClass.Heavy ? 0.35f : crit ? 0.18f : 0f;
    }
}
