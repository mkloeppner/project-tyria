using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Erzeugt aus <see cref="ModifierSpec"/>-Daten konkrete Kampf-Modifikatoren.</summary>
    public static class TraitModifierFactory
    {
        public static CombatModifier Create(ModifierSpec s, string sourceId)
        {
            CombatModifier m;
            switch (s.Kind)
            {
                case ModifierKind.AttributeFlat: m = new AttributeFlatModifier(s); break;
                case ModifierKind.AttributeConversion: m = new AttributeConversionModifier(s); break;
                case ModifierKind.CritChance: m = new CritChanceModifier(s); break;
                case ModifierKind.DamageVsCondition: m = new DamageVsEffectModifier(s); break;
                case ModifierKind.DamageVsLowHealth: m = new DamageVsLowHealthModifier(s); break;
                case ModifierKind.DamageWhileBoon: m = new DamageWhileEffectModifier(s); break;
                case ModifierKind.CategoryDamage: m = new CategoryDamageModifier(s); break;
                case ModifierKind.CategoryRecharge: m = new CategoryRechargeModifier(s); break;
                case ModifierKind.EffectDuration: m = new EffectDurationModifier(s); break;
                case ModifierKind.TriggerApplyEffect: m = new TriggerEffectModifier(s); break;
                case ModifierKind.TriggerHeal: m = new TriggerHealModifier(s); break;
                default: return null;
            }
            m.SourceId = sourceId;
            return m;
        }

        /// <summary>Passt die Skill-Auswahl (Waffe, Kategorie, Slot-Typ) eines Specs?</summary>
        public static bool Matches(ModifierSpec s, SkillDefinition skill)
        {
            if (skill == null) return false;
            if (s.Weapon != WeaponKind.None && skill.Weapon != s.Weapon) return false;
            if (s.Category != UtilityCategory.None && skill.Category != s.Category) return false;
            if (s.MatchSlotType && skill.SlotType != s.SlotType) return false;
            return s.Weapon != WeaponKind.None || s.Category != UtilityCategory.None || s.MatchSlotType;
        }
    }

    public abstract class SpecModifier : CombatModifier
    {
        protected readonly ModifierSpec Spec;
        protected SpecModifier(ModifierSpec s) { Spec = s; }
    }

    public class AttributeFlatModifier : SpecModifier
    {
        public AttributeFlatModifier(ModifierSpec s) : base(s) { }
        public override void Contribute(AttributeAccumulator acc, int level) => acc.Flat[(int)Spec.Attr] += Spec.Value * AttributeSet.LevelScale(level);
    }

    public class AttributeConversionModifier : SpecModifier
    {
        public AttributeConversionModifier(ModifierSpec s) : base(s) { }
        public override void Contribute(AttributeAccumulator acc, int level) => acc.Conversions.Add((Spec.Attr2, Spec.Attr, Spec.Value));
    }

    public class CritChanceModifier : SpecModifier
    {
        public CritChanceModifier(ModifierSpec s) : base(s) { }
        public override void Contribute(AttributeAccumulator acc, int level) => acc.CritChanceBonus += Spec.Value;
    }

    public class DamageVsEffectModifier : SpecModifier
    {
        public DamageVsEffectModifier(ModifierSpec s) : base(s) { }
        public override float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill)
            => target != null && target.Effects != null && target.Effects.Has(Spec.Effect) ? Spec.Value : 0f;
    }

    public class DamageVsLowHealthModifier : SpecModifier
    {
        public DamageVsLowHealthModifier(ModifierSpec s) : base(s) { }
        public override float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill)
            => target != null && target.MaxHealth > 0f && target.Health / target.MaxHealth < Spec.Value2 ? Spec.Value : 0f;
    }

    public class DamageWhileEffectModifier : SpecModifier
    {
        public DamageWhileEffectModifier(ModifierSpec s) : base(s) { }
        public override float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill)
            => self != null && self.Effects != null && self.Effects.Has(Spec.Effect) ? Spec.Value : 0f;
    }

    public class CategoryDamageModifier : SpecModifier
    {
        public CategoryDamageModifier(ModifierSpec s) : base(s) { }
        public override float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill) => TraitModifierFactory.Matches(Spec, skill) ? Spec.Value : 0f;
    }

    public class CategoryRechargeModifier : SpecModifier
    {
        public CategoryRechargeModifier(ModifierSpec s) : base(s) { }
        public override float RechargeReduction(SkillDefinition skill) => TraitModifierFactory.Matches(Spec, skill) ? Spec.Value : 0f;
    }

    public class EffectDurationModifier : SpecModifier
    {
        public EffectDurationModifier(ModifierSpec s) : base(s) { }
        public override float DurationBonus(EffectType type)
        {
            switch (Spec.Scope)
            {
                case DurationScope.AllBoons: return EffectLibrary.IsBoon(type) ? Spec.Value : 0f;
                case DurationScope.AllConditions: return EffectLibrary.IsCondition(type) ? Spec.Value : 0f;
                default: return type == Spec.Effect ? Spec.Value : 0f;
            }
        }
    }

    /// <summary>Basis für Auslöser mit interner Abklingzeit (Spielzeit; in Tests über <see cref="Clock"/> steuerbar).</summary>
    public abstract class TriggerModifier : SpecModifier
    {
        public static System.Func<float> Clock = () => Time.time;
        float _readyAt = float.MinValue;
        protected TriggerModifier(ModifierSpec s) : base(s) { }
        public int Procs { get; private set; }

        public override void OnTrigger(TriggerContext ctx)
        {
            if (ctx.Type != Spec.Trigger || ctx.Self == null) return;
            float now = Clock();
            if (now < _readyAt) return;
            if (!Fire(ctx)) return;
            Procs++;
            _readyAt = now + Spec.InternalCooldown;
        }

        protected abstract bool Fire(TriggerContext ctx);
    }

    public class TriggerEffectModifier : TriggerModifier
    {
        public TriggerEffectModifier(ModifierSpec s) : base(s) { }
        protected override bool Fire(TriggerContext ctx)
        {
            var who = Spec.Target == ModifierTarget.Self ? ctx.Self : ctx.Target;
            if (who == null || who.IsDead || who.Effects == null) return false;
            who.Effects.Apply(new EffectApplication(Spec.Effect, Spec.Stacks, Spec.Duration), ctx.Self);
            return true;
        }
    }

    public class TriggerHealModifier : TriggerModifier
    {
        public TriggerHealModifier(ModifierSpec s) : base(s) { }
        protected override bool Fire(TriggerContext ctx)
        {
            if (ctx.Self.IsDead) return false;
            float amount = DamageFormula.Heal(DamageFormula.ScaledBase(Spec.Value, ctx.Self.Level), ctx.Self.Stats.Get(Attr.HealingPower), Spec.Value2);
            ctx.Self.Heal(amount);
            return true;
        }
    }
}
