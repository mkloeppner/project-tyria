using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Trotzleiste (GW2: Defiance/Breakbar), rein und getestet: Kontroll-Effekte werden blockiert und senken stattdessen die Leiste;
    /// bei 0 ist der Gegner „Gebrochen“ (5 s betäubt, +50 % Schaden), danach füllt sich die Leiste wieder.
    /// </summary>
    public class DefianceModel
    {
        public float Max { get; private set; }
        public float Value { get; private set; }
        public float BrokenRemaining { get; private set; }
        public float BreakDuration = 5f;
        public float RefillTime = 3f;
        float _refill;
        public int Breaks { get; private set; }

        public DefianceModel(float max) { Max = Mathf.Max(1f, max); Value = Max; }

        public bool Broken => BrokenRemaining > 0f;
        public bool Refilling => _refill > 0f;
        public float Fraction => Value / Max;
        public float DamageTakenMultiplier => Broken ? 1.5f : 1f;

        /// <summary>Wert eines Kontroll-Effekts gegen die Trotzleiste (0 = kein Einfluss).</summary>
        public static float CcValue(EffectType t, float duration)
        {
            switch (t)
            {
                case EffectType.Stun: return 100f + 100f * Mathf.Max(0f, duration);
                case EffectType.Fear: return 100f + 100f * Mathf.Max(0f, duration);
                case EffectType.Taunt: return 100f + 50f * Mathf.Max(0f, duration);
                case EffectType.Immobilize: return 60f * Mathf.Max(1f, duration);
                case EffectType.Cripple: return 15f * Mathf.Max(1f, duration);
                case EffectType.Chill: return 15f * Mathf.Max(1f, duration);
                case EffectType.Slow: return 25f * Mathf.Max(1f, duration);
                case EffectType.Blind: return 10f;
            }
            return 0f;
        }

        /// <summary>true = Effekt wird blockiert (Leiste aktiv). <paramref name="broke"/> = Leiste gerade gebrochen.</summary>
        public bool Hit(EffectType t, float duration, out bool broke)
        {
            broke = false;
            float v = CcValue(t, duration);
            if (v <= 0f) return false;
            if (Broken || Refilling) return t == EffectType.Stun || t == EffectType.Fear || t == EffectType.Taunt || t == EffectType.Immobilize ? Refilling : false;
            Value = Mathf.Max(0f, Value - v);
            if (Value <= 0f) { BrokenRemaining = BreakDuration; Breaks++; broke = true; }
            return true;
        }

        public void Tick(float dt)
        {
            if (BrokenRemaining > 0f)
            {
                BrokenRemaining -= dt;
                if (BrokenRemaining <= 0f) { BrokenRemaining = 0f; _refill = RefillTime; }
                return;
            }
            if (_refill > 0f)
            {
                _refill -= dt;
                Value = Mathf.Lerp(Max, 0f, Mathf.Clamp01(_refill / RefillTime));
                if (_refill <= 0f) { _refill = 0f; Value = Max; }
            }
        }
    }
}
