using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum EnemyTier { None, Trash, Veteran, Champion }

    public interface IInvulnerabilitySource { bool IsInvulnerable { get; } }

    /// <summary>Optionaler Handler für den Downed State (nur Spieler).</summary>
    public interface IDownedHandler
    {
        bool IsDowned { get; }
        bool TryEnterDowned(Combatant killer);
        void TakeDownedDamage(float amount, Combatant source);
    }

    /// <summary>
    /// Gemeinsame Basis aller Kampfeinheiten. Liest Werte aus dem AttributeSet (Basis + Ausrüstung + Eigenschaften + Effekte)
    /// und wendet zielseitige Effekte an (Aegis, Schutz, Verwundbarkeit, Entschlossenheit).
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();

        public string DisplayName = "Kämpfer";
        public Team Team = Team.Enemy;
        public EnemyTier Tier = EnemyTier.None;
        public float MaxHealth = 10000f;
        public float Height = 1.8f;
        public bool Targetable = true;
        [Tooltip("Spieler: Lebenspunkte folgen den Attributen (Vitalität).")]
        public bool DeriveHealthFromStats;
        [Tooltip(">0: Waffenstärke der angelegten Waffe (sonst Level-Basis).")]
        public float WeaponStrengthOverride;

        public float Health { get; private set; }
        public bool IsDead { get; private set; }
        public bool ManualInvulnerable { get; set; }
        public AttributeSet Stats { get; } = new AttributeSet();
        public EffectModel Effects { get; } = new EffectModel();
        public CombatModifierSet Modifiers { get; } = new CombatModifierSet();
        public IDownedHandler DownedHandler { get; set; }
        public readonly List<IInvulnerabilitySource> InvulnerabilitySources = new List<IInvulnerabilitySource>();
        public readonly HashSet<Combatant> Attackers = new HashSet<Combatant>();

        public event Action<DamageInfo> Damaged;
        /// <summary>Lauf 4: Multiplikator auf direkten Schaden (z. B. Frontalschild der Frosthüter). null = 1.</summary>
        public Func<DamageInfo, float> IncomingMultiplier;
        /// <summary>Lauf 4: fängt Schaden ab (Reittier-HP); Rückgabe = Restschaden für den Kämpfer.</summary>
        public Func<float, float> AbsorbDamage;
        public event Action<Combatant> Died;
        public event Action HealthChanged;

        bool _initialized;
        public int Level { get => Stats.Level; set => Stats.Level = value; }

        public Vector3 AimPoint => transform.position + Vector3.up * (Height * 0.6f);
        public float HealthFraction => MaxHealth <= 0 ? 0 : Health / MaxHealth;
        public bool IsDowned => DownedHandler != null && DownedHandler.IsDowned;

        public bool IsInvulnerableNow
        {
            get
            {
                if (ManualInvulnerable) return true;
                for (int i = 0; i < InvulnerabilitySources.Count; i++)
                    if (InvulnerabilitySources[i] != null && InvulnerabilitySources[i].IsInvulnerable) return true;
                return false;
            }
        }

        public bool IsControlled => Effects.Has(EffectType.Stun);
        public bool CanMove => !Effects.Has(EffectType.Stun) && !Effects.Has(EffectType.Immobilize);

        public float MoveSpeedMultiplier
        {
            get
            {
                if (!CanMove) return 0f;
                float m = 1f;
                if (Effects.Has(EffectType.Chill)) m *= EffectRules.ChillMoveFactor;
                else if (Effects.Has(EffectType.Cripple)) m *= EffectRules.CrippleMoveFactor;
                if (Effects.Has(EffectType.Swiftness)) m *= EffectRules.SwiftnessMoveFactor;
                return m;
            }
        }

        public void EnsureInitialized()
        {
            Effects.Owner = this;
            if (_initialized) return;
            _initialized = true;
            Stats.AddSource(Effects);
            Stats.AddSource(Modifiers);
            Effects.Changed += Stats.MarkDirty;
            Modifiers.Changed += Stats.MarkDirty;
            Stats.Changed += OnStatsChanged;
            if (DeriveHealthFromStats) MaxHealth = Stats.MaxHealth;
            Health = MaxHealth;
        }

        void OnStatsChanged()
        {
            if (!DeriveHealthFromStats) return;
            float frac = MaxHealth > 0 ? Health / MaxHealth : 1f;
            float newMax = Stats.MaxHealth;
            if (Mathf.Abs(newMax - MaxHealth) < 0.01f) return;
            MaxHealth = newMax;
            if (!IsDead && !IsDowned) Health = Mathf.Clamp(frac * newMax, 1f, newMax);
            HealthChanged?.Invoke();
        }

        protected virtual void Awake() => EnsureInitialized();
        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() => All.Remove(this);

        protected virtual void Update()
        {
            if (!IsDead) Effects.Tick(Time.deltaTime, this);
        }

        public bool IsHostileTo(Combatant other)
        {
            if (other == null || other == this) return false;
            if (Team == Team.Neutral || other.Team == Team.Neutral) return false;
            bool aEnemy = Team == Team.Enemy, bEnemy = other.Team == Team.Enemy;
            return aEnemy != bEnemy;
        }

        public void SetMaxHealth(float max, bool refill)
        {
            EnsureInitialized();
            MaxHealth = max;
            if (refill) Health = max; else Health = Mathf.Min(Health, max);
            HealthChanged?.Invoke();
        }

        /// <summary>Wendet Schaden an (Betrag ist bereits angreiferseitig berechnet). Gibt den zugefügten Schaden zurück.</summary>
        public float TakeDamage(DamageInfo info)
        {
            EnsureInitialized();
            info.Target = this;
            if (IsDead) return 0f;
            if (info.Missed) { info.Amount = 0f; GameEventBus.RaiseDamage(info); return 0f; }
            if (IsInvulnerableNow)
            {
                info.Amount = 0f; info.Evaded = true;
                GameEventBus.RaiseDamage(info);
                return 0f;
            }

            float dmg = info.Amount;
            if (!info.IsCondition)
            {
                if (Effects.Consume(EffectType.Aegis))
                {
                    info.Amount = 0f; info.Blocked = true;
                    GameEventBus.RaiseDamage(info);
                    return 0f;
                }
                if (Effects.Has(EffectType.Protection)) dmg *= 1f - EffectRules.ProtectionReduction;
                if (IncomingMultiplier != null) { float m = IncomingMultiplier(info); if (m < 0.999f) info.Blocked = m <= 0.3f; dmg *= m; }
            }
            else if (Effects.Has(EffectType.Resolution)) dmg *= 1f - EffectRules.ResolutionReduction;
            dmg *= 1f + Effects.GetStacks(EffectType.Vulnerability) * EffectRules.VulnerabilityPerStack;
            if (AbsorbDamage != null && !IsDowned)
            {
                float rest = AbsorbDamage(dmg);
                if (rest <= 0f) { info.Amount = dmg; info.Blocked = false; if (info.Source != null) Attackers.Add(info.Source); Damaged?.Invoke(info); GameEventBus.RaiseDamage(info); return 0f; }
                dmg = rest;
            }
            info.Amount = dmg;
            if (info.Source != null) Attackers.Add(info.Source);

            if (IsDowned)
            {
                DownedHandler.TakeDownedDamage(dmg, info.Source);
                Damaged?.Invoke(info);
                GameEventBus.RaiseDamage(info);
                return dmg;
            }

            info.Amount = dmg = Mathf.Min(dmg, Health);
            Health -= dmg;
            Damaged?.Invoke(info);
            GameEventBus.RaiseDamage(info);
            if (dmg > 0f && Modifiers != null) Modifiers.Trigger(new TriggerContext { Type = TriggerType.DamageTaken, Self = this, Target = info.Source, Position = transform.position });
            if (Health <= 0f)
            {
                Health = 0f;
                if (DownedHandler == null || !DownedHandler.TryEnterDowned(info.Source))
                    Die(info.Source);
            }
            HealthChanged?.Invoke();
            return dmg;
        }

        public float Heal(float amount)
        {
            EnsureInitialized();
            if (IsDead || IsDowned || amount <= 0f) return 0f;
            if (Effects.Has(EffectType.Poison)) amount *= EffectRules.PoisonHealingFactor;
            float before = Health;
            Health = Mathf.Min(MaxHealth, Health + amount);
            float healed = Health - before;
            if (healed > 0f) { GameEventBus.RaiseHealed(this, healed); HealthChanged?.Invoke(); }
            return healed;
        }

        /// <summary>Setzt die HP direkt (z. B. bei Rally).</summary>
        public void SetHealth(float value)
        {
            EnsureInitialized();
            Health = Mathf.Clamp(value, 0f, MaxHealth);
            HealthChanged?.Invoke();
        }

        public void Die(Combatant killer)
        {
            if (IsDead) return;
            IsDead = true;
            Health = 0f;
            Effects.Clear();
            Died?.Invoke(killer);
            GameEventBus.RaiseKilled(this, killer);
            if (killer != null && killer != this && killer.Modifiers != null) killer.Modifiers.Trigger(new TriggerContext { Type = TriggerType.Kill, Self = killer, Target = this, Position = transform.position });
            HealthChanged?.Invoke();
        }

        public void Revive(float healthFraction = 1f)
        {
            EnsureInitialized();
            IsDead = false;
            Health = MaxHealth * Mathf.Clamp01(healthFraction);
            Effects.Clear();
            Attackers.Clear();
            HealthChanged?.Invoke();
        }

        public static Combatant FindNearestHostile(Combatant from, Vector3 pos, float range, Func<Combatant, bool> filter = null)
        {
            Combatant best = null; float bestD = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var c = All[i];
                if (c == null || c.IsDead || !from.IsHostileTo(c)) continue;
                if (!c.Targetable) continue;   // ausgeblendete/eingegrabene Figuren (ActorVisibility)
                if (c.Effects.Has(EffectType.Stealth)) continue;
                if (filter != null && !filter(c)) continue;
                float d = (c.transform.position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }
    }

    /// <summary>Angreiferseitige Schadensberechnung (Formel, Krit, Blindheit, Schwäche, Modifikatoren) + Auslöser.</summary>
    public static class DamageCalculator
    {
        public static readonly System.Random Rng = new System.Random(20261109);

        /// <summary>Berechnet und wendet Direktschaden an. Gibt den zugefügten Schaden zurück.</summary>
        public static float DealDirect(Combatant src, Combatant tgt, float coefficient, SkillDefinition skill = null, Vector3? point = null)
        {
            if (tgt == null || tgt.IsDead) return 0f;
            var info = Build(src, tgt, coefficient, skill);
            info.Point = point ?? tgt.AimPoint;
            float dealt = tgt.TakeDamage(info);
            if (dealt > 0f && info.Crit && src != null)
                src.Modifiers.Trigger(new TriggerContext { Type = TriggerType.Crit, Self = src, Target = tgt, Skill = skill, Position = tgt.transform.position });
            if (dealt > 0f && info.Crit) GameEventBus.RaiseCrit(src, tgt);
            return dealt;
        }

        public static DamageInfo Build(Combatant src, Combatant tgt, float coefficient, SkillDefinition skill)
        {
            var info = new DamageInfo { Source = src, Target = tgt, Skill = skill };
            if (src == null) { info.Amount = coefficient; return info; }
            if (src.Effects.Consume(EffectType.Blind)) { info.Missed = true; return info; }
            float armor = tgt != null ? tgt.Stats.Armor : 1000f;
            float mult = src.Modifiers.OutgoingMultiplier(src, tgt, skill);
            var r = DamageFormula.Compute(src.Stats, coefficient, armor, Rng, src.WeaponStrengthOverride > 0f ? src.WeaponStrengthOverride : -1f, mult, src.Effects.Has(EffectType.Weakness));
            info.Amount = r.Amount; info.Crit = r.Crit; info.Glancing = r.Glancing;
            return info;
        }

        /// <summary>Heilung = Basis (level-skaliert) + Heilkraft × Koeffizient.</summary>
        public static float Heal(Combatant src, Combatant tgt, float baseAt80, float coefficient)
        {
            if (tgt == null) return 0f;
            int lvl = src != null ? src.Level : tgt.Level;
            float hp = src != null ? src.Stats.Get(Attr.HealingPower) : 0f;
            return tgt.Heal(DamageFormula.Heal(DamageFormula.ScaledBase(baseAt80, lvl), hp, coefficient));
        }
    }
}
