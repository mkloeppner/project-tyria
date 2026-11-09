using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Wirkungstypen der Eigenschaften (datengetrieben, siehe <see cref="TraitModifierFactory"/>).</summary>
    public enum ModifierKind
    {
        AttributeFlat,          // +Value Attribut (Wert auf Stufe 80, skaliert mit Stufe)
        AttributeConversion,    // +Value (Anteil) von Attr2 als Attr
        CritChance,             // +Value Kritische Trefferchance (0,05 = 5 %)
        DamageVsCondition,      // +Value Schaden gegen Ziele mit Effekt
        DamageVsLowHealth,      // +Value Schaden gegen Ziele unter Value2 Lebensanteil
        DamageWhileBoon,        // +Value Schaden, solange du Effekt hast
        CategoryDamage,         // +Value Schaden mit Waffe/Kategorie
        CategoryRecharge,       // −Value Abklingzeit für Waffe/Kategorie/Slot-Typ
        EffectDuration,         // +Value Dauer eines Effekts (oder aller Segen/Zustände)
        TriggerApplyEffect,     // Auslöser → Effekt (auf dich oder Ziel), interne Abklingzeit
        TriggerHeal,            // Auslöser → Heilung (Basis auf Stufe 80 + Heilkraft × Value2)
    }

    public enum ModifierTarget { Self, Target }
    public enum DurationScope { Single, AllBoons, AllConditions }

    [Serializable]
    public class ModifierSpec
    {
        public ModifierKind Kind;
        public Attr Attr;
        public Attr Attr2;
        public float Value;
        public float Value2;
        public EffectType Effect;
        public int Stacks = 1;
        public float Duration;
        public DurationScope Scope;
        public TriggerType Trigger;
        public ModifierTarget Target;
        public float InternalCooldown;
        public WeaponKind Weapon;
        public UtilityCategory Category;
        public SkillSlotType SlotType = SkillSlotType.Weapon;
        public bool MatchSlotType;
    }

    /// <summary>Eine Eigenschaft (klein = automatisch, groß = wählbar, 3 je Rang).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Trait")]
    public class TraitDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public string Glyph = "?";
        public int Tier = 1;           // 1–3
        public bool Major;
        public List<ModifierSpec> Modifiers = new List<ModifierSpec>();
    }
}
