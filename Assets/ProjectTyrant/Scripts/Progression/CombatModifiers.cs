using System.Collections.Generic;

namespace ProjectTyrant
{
    public enum TriggerType { None, Crit, Dodge, HealSkill, WeaponSwap, Kill, SkillUse, DamageTaken, Stealth, AttunementSwap, PetSwap, EliteMechanic }

    public struct TriggerContext
    {
        public TriggerType Type;
        public Combatant Self;
        public Combatant Target;
        public SkillDefinition Skill;
        public UnityEngine.Vector3 Position;
    }

    /// <summary>Basis aller Kampf-Modifikatoren (Eigenschaften, Nahrung …). Alle Hooks optional.</summary>
    public abstract class CombatModifier
    {
        public string SourceId;
        public virtual void Contribute(AttributeAccumulator acc, int level) { }
        /// <summary>Additiver Prozent-Bonus auf ausgehenden Schaden (0.1 = +10 %).</summary>
        public virtual float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill) => 0f;
        /// <summary>Additive Abklingzeit-Reduktion (0.2 = −20 %).</summary>
        public virtual float RechargeReduction(SkillDefinition skill) => 0f;
        public virtual float DurationBonus(EffectType type) => 0f;
        public virtual void OnTrigger(TriggerContext ctx) { }
    }

    /// <summary>Aggregiert alle Modifikatoren eines Kämpfers; ist zugleich Attributquelle.</summary>
    public class CombatModifierSet : IAttributeSource
    {
        readonly List<CombatModifier> _mods = new List<CombatModifier>();
        public event System.Action Changed;
        public IReadOnlyList<CombatModifier> All => _mods;

        public void Add(CombatModifier m) { if (m != null) { _mods.Add(m); Changed?.Invoke(); } }
        public void RemoveSource(string sourceId) { if (_mods.RemoveAll(m => m.SourceId == sourceId) > 0) Changed?.Invoke(); }
        public void Clear() { _mods.Clear(); Changed?.Invoke(); }

        public void Contribute(AttributeAccumulator acc, int level) { for (int i = 0; i < _mods.Count; i++) _mods[i].Contribute(acc, level); }

        public float OutgoingMultiplier(Combatant self, Combatant target, SkillDefinition skill)
        {
            float b = 0f;
            for (int i = 0; i < _mods.Count; i++) b += _mods[i].OutgoingDamageBonus(self, target, skill);
            return 1f + b;
        }

        public float RechargeMultiplier(SkillDefinition skill)
        {
            float r = 0f;
            for (int i = 0; i < _mods.Count; i++) r += _mods[i].RechargeReduction(skill);
            return UnityEngine.Mathf.Clamp(1f - r, 0.2f, 1f);
        }

        public float DurationBonus(EffectType t)
        {
            float b = 0f;
            for (int i = 0; i < _mods.Count; i++) b += _mods[i].DurationBonus(t);
            return b;
        }

        public void Trigger(TriggerContext ctx)
        {
            for (int i = 0; i < _mods.Count; i++) _mods[i].OnTrigger(ctx);
        }
    }
}
