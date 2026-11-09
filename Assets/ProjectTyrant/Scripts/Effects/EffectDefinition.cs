using UnityEngine;

namespace ProjectTyrant
{
    public enum EffectType
    {
        // 12 Segen
        Aegis, Vigor, Swiftness, Resolution, Might, Regeneration, Quickness, Protection, Stability, Alacrity, Resistance, Fury,
        // 14 Zustände
        Blind, Bleeding, Burning, Fear, Poison, Chill, Torment, Weakness, Taunt, Cripple, Slow, Confusion, Vulnerability, Immobilize,
        // Sonstige
        Stealth, Stun,
        // Lauf 5 / M25
        Revealed
    }

    public enum EffectCategory { Boon, Condition, Control, Special }
    public enum StackRule { Intensity, Duration }

    /// <summary>Daten eines Segens/Zustands (vom Builder als Asset erzeugt, Standardwerte im EffectCatalog).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Effect Definition", fileName = "Effect_")]
    public class EffectDefinition : ScriptableObject
    {
        public EffectType Type;
        public string DisplayName;
        [TextArea] public string Description;
        public EffectCategory Category;
        public StackRule Stacking = StackRule.Duration;
        public int MaxStacks = 1;
        public float TickInterval = 0f;
        public float MaxDuration = 30f;
        public bool Implemented = true;
        public Color Color = Color.white;
        public string Glyph = "?";
        public Sprite Icon;

        public bool IsBoon => Category == EffectCategory.Boon;
        public bool IsCondition => Category == EffectCategory.Condition;
    }
}
