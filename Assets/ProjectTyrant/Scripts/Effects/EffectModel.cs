using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    [Serializable]
    public struct EffectApplication
    {
        public EffectType Type;
        public int Stacks;
        public float Duration;
        public EffectApplication(EffectType t, int stacks, float duration) { Type = t; Stacks = stacks; Duration = duration; }
    }

    /// <summary>Unveränderliche Standardwerte aller Effekte (Quelle für Builder und Tests).</summary>
    public static class EffectCatalog
    {
        public struct Info
        {
            public EffectType Type; public string Name; public EffectCategory Cat; public StackRule Rule; public int Max; public float Tick; public bool Implemented; public string Desc; public string Glyph; public Color Color;
        }

        static Info I(EffectType t, string n, EffectCategory c, StackRule r, int max, float tick, bool impl, string glyph, Color col, string desc)
            => new Info { Type = t, Name = n, Cat = c, Rule = r, Max = max, Tick = tick, Implemented = impl, Glyph = glyph, Color = col, Desc = desc };

        static readonly Color BoonC = new Color(0.45f, 0.75f, 1f), CondC = new Color(0.9f, 0.35f, 0.3f);

        public static readonly Info[] All =
        {
            I(EffectType.Aegis, "Aegis", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "A", new Color(0.7f,0.85f,1f), "Blockt den nächsten eingehenden Angriff."),
            I(EffectType.Vigor, "Elan", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "E", new Color(0.5f,1f,0.6f), "+50 % Ausdauer-Regeneration."),
            I(EffectType.Swiftness, "Eile", EffectCategory.Boon, StackRule.Duration, 1, 0, true, ">", new Color(0.95f,0.95f,0.5f), "+33 % Bewegungstempo."),
            I(EffectType.Resolution, "Entschlossenheit", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "R", new Color(0.9f,0.75f,0.4f), "−33 % erlittener Zustandsschaden."),
            I(EffectType.Might, "Macht", EffectCategory.Boon, StackRule.Intensity, 25, 0, true, "M", new Color(1f,0.55f,0.2f), "+30 Kraft und +30 Zustandsschaden je Stapel (level-skaliert)."),
            I(EffectType.Regeneration, "Regeneration", EffectCategory.Boon, StackRule.Duration, 1, 1, true, "+", new Color(0.4f,0.95f,0.4f), "Heilt jede Sekunde."),
            I(EffectType.Quickness, "Schnelligkeit", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "Q", new Color(1f,0.8f,0.3f), "+50 % Wirkgeschwindigkeit."),
            I(EffectType.Protection, "Schutz", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "S", new Color(0.55f,0.65f,1f), "−33 % erlittener Direktschaden."),
            I(EffectType.Stability, "Stabilität", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "St", new Color(0.8f,0.7f,0.5f), "Immun gegen Kontrolleffekte."),
            I(EffectType.Alacrity, "Tatendrang", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "T", new Color(0.8f,0.5f,1f), "Abklingzeiten laufen 25 % schneller."),
            I(EffectType.Resistance, "Widerstand", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "W", new Color(0.7f,0.7f,0.75f), "Zustände sind wirkungslos."),
            I(EffectType.Fury, "Wut", EffectCategory.Boon, StackRule.Duration, 1, 0, true, "!", new Color(1f,0.35f,0.25f), "+20 % kritische Trefferchance."),

            I(EffectType.Blind, "Blindheit", EffectCategory.Condition, StackRule.Duration, 1, 0, true, "B", new Color(0.3f,0.3f,0.35f), "Der nächste Angriff verfehlt."),
            I(EffectType.Bleeding, "Blutung", EffectCategory.Condition, StackRule.Intensity, 25, 1, true, "b", new Color(0.8f,0.1f,0.1f), "Schaden pro Sekunde je Stapel."),
            I(EffectType.Burning, "Brennen", EffectCategory.Condition, StackRule.Intensity, 25, 1, true, "F", new Color(1f,0.45f,0.1f), "Hoher Schaden pro Sekunde je Stapel."),
            I(EffectType.Fear, "Furcht", EffectCategory.Condition, StackRule.Duration, 1, 0, false, "?", new Color(0.5f,0.2f,0.6f), "Ziel flieht (im Prototyp nur Anzeige)."),
            I(EffectType.Poison, "Vergiftung", EffectCategory.Condition, StackRule.Intensity, 25, 1, true, "P", new Color(0.4f,0.8f,0.2f), "Schaden pro Sekunde, −33 % erhaltene Heilung."),
            I(EffectType.Chill, "Kühle", EffectCategory.Condition, StackRule.Duration, 1, 0, true, "K", new Color(0.55f,0.85f,1f), "−66 % Tempo, Abklingzeiten −66 % langsamer."),
            I(EffectType.Torment, "Qual", EffectCategory.Condition, StackRule.Intensity, 25, 1, true, "Ql", new Color(0.45f,0.2f,0.5f), "Schaden pro Sekunde je Stapel."),
            I(EffectType.Weakness, "Schwäche", EffectCategory.Condition, StackRule.Duration, 1, 0, true, "w", new Color(0.7f,0.6f,0.3f), "50 % der Treffer streifen, −50 % Ausdauer-Regeneration."),
            I(EffectType.Taunt, "Spott", EffectCategory.Condition, StackRule.Duration, 1, 0, false, "Sp", new Color(0.9f,0.3f,0.5f), "Zwingt zum Angriff (im Prototyp nur Anzeige)."),
            I(EffectType.Cripple, "Verkrüppelung", EffectCategory.Condition, StackRule.Duration, 1, 0, true, "V", new Color(0.6f,0.45f,0.3f), "−50 % Bewegungstempo."),
            I(EffectType.Slow, "Verlangsamung", EffectCategory.Condition, StackRule.Duration, 1, 0, false, "L", new Color(0.4f,0.5f,0.7f), "Langsamere Aktionen (im Prototyp nur Anzeige)."),
            I(EffectType.Confusion, "Verwirrung", EffectCategory.Condition, StackRule.Intensity, 25, 1, true, "Vw", new Color(0.85f,0.4f,0.9f), "Schaden pro Sekunde und bei jedem Fähigkeitseinsatz."),
            I(EffectType.Vulnerability, "Verwundbarkeit", EffectCategory.Condition, StackRule.Intensity, 25, 0, true, "Vu", new Color(0.75f,0.3f,0.8f), "+1 % erlittener Schaden je Stapel."),
            I(EffectType.Immobilize, "Immobilisierung", EffectCategory.Condition, StackRule.Duration, 1, 0, true, "I", new Color(0.6f,0.6f,0.6f), "Bewegungsunfähig."),

            I(EffectType.Stealth, "Tarnung", EffectCategory.Special, StackRule.Duration, 1, 0, true, "~", new Color(0.6f,0.6f,0.7f), "Unsichtbar für Gegner."),
            I(EffectType.Stun, "Betäubung", EffectCategory.Control, StackRule.Duration, 1, 0, true, "*", new Color(1f,1f,0.3f), "Handlungsunfähig."),
            I(EffectType.Revealed, "Enthüllt", EffectCategory.Special, StackRule.Duration, 1, 0, true, "!", new Color(0.9f,0.5f,0.5f), "Kann keine Tarnung betreten."),
        };

        public static Info Get(EffectType t)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Type == t) return All[i];
            return All[0];
        }
    }

    /// <summary>Laufzeit-Zugriff auf Effektdaten (Assets, falls registriert, sonst Katalog).</summary>
    public static class EffectLibrary
    {
        static readonly Dictionary<EffectType, EffectDefinition> _defs = new Dictionary<EffectType, EffectDefinition>();

        public static void Register(IEnumerable<EffectDefinition> defs)
        {
            if (defs == null) return;
            foreach (var d in defs) if (d != null) _defs[d.Type] = d;
        }

        public static EffectDefinition Asset(EffectType t) => _defs.TryGetValue(t, out var d) ? d : null;

        public static string Name(EffectType t) { var d = Asset(t); return d != null ? d.DisplayName : EffectCatalog.Get(t).Name; }
        public static Color Color(EffectType t) { var d = Asset(t); return d != null ? d.Color : EffectCatalog.Get(t).Color; }
        public static EffectCategory Category(EffectType t) { var d = Asset(t); return d != null ? d.Category : EffectCatalog.Get(t).Cat; }
        public static StackRule Rule(EffectType t) { var d = Asset(t); return d != null ? d.Stacking : EffectCatalog.Get(t).Rule; }
        public static int MaxStacks(EffectType t) { var d = Asset(t); return d != null ? d.MaxStacks : EffectCatalog.Get(t).Max; }
        public static float MaxDuration(EffectType t) { var d = Asset(t); return d != null ? d.MaxDuration : 30f; }
        public static bool IsBoon(EffectType t) => Category(t) == EffectCategory.Boon;
        public static bool IsCondition(EffectType t) => Category(t) == EffectCategory.Condition;
        public static bool IsControl(EffectType t) => t == EffectType.Stun || t == EffectType.Fear || t == EffectType.Taunt;
    }

    /// <summary>Regelwerte (zentral für Balancing).</summary>
    public static class EffectRules
    {
        public const float MightAttributePerStack = 30f;
        public const float VulnerabilityPerStack = 0.01f;
        public const float ProtectionReduction = 0.33f;
        public const float ResolutionReduction = 0.33f;
        public const float FuryCritChance = 0.2f;
        public const float SwiftnessMoveFactor = 1.33f;
        public const float CrippleMoveFactor = 0.5f;
        public const float ChillMoveFactor = 0.34f;
        public const float ChillRechargeFactor = 0.34f;
        public const float AlacrityRechargeFactor = 1.25f;
        public const float QuicknessCastFactor = 1.5f;
        public const float VigorEnduranceFactor = 1.5f;
        public const float WeaknessEnduranceFactor = 0.5f;
        public const float PoisonHealingFactor = 0.67f;
        public const float RegenerationBaseAt80 = 130f;
        public const float RegenerationHealingCoeff = 0.125f;

        /// <summary>Macht skaliert unterhalb Level 80 (min. 20 %), damit niedrige Level nicht explodieren.</summary>
        public static float MightPerStack(int level) => MightAttributePerStack * Mathf.Max(0.2f, AttributeSet.LevelScale(level));
    }

    /// <summary>
    /// Segen/Zustände eines Kämpfers: Stapel mit Quelle und Dauer, Intensitäts- oder Dauer-Stapelung,
    /// Dauerskalierung über Konzentration/Sachkenntnis, Reinigen, Ticks (1 s). Trägt Attribute bei (Macht, Wut).
    /// </summary>
    public class EffectModel : IAttributeSource
    {
        public class Stack { public EffectType Type; public float Remaining; public float Total; public Combatant Source; }

        readonly List<Stack> _stacks = new List<Stack>();
        float _pulseTimer;

        public event Action Changed;
        /// <summary>Lauf 5 / M23: Besitzer (für Effekt-Anwendungs-VFX) und globales Ereignis „Effekt angewendet“.</summary>
        public Combatant Owner;
        public static event Action<EffectModel, EffectType> AppliedGlobal;
        public IReadOnlyList<Stack> Stacks => _stacks;
        /// <summary>Lauf 4: Filter vor dem Anwenden (Trotzleiste). false = blockiert.</summary>
        public Func<EffectType, int, float, bool> ApplyFilter;

        /// <summary>Wendet einen Effekt an. Dauer wird mit Segens-/Zustandsdauer der Quelle skaliert.</summary>
        public void Apply(EffectType type, int stacks, float duration, Combatant source = null, float durationBonus = -1f)
        {
            if (stacks <= 0 || duration <= 0f) return;
            if (EffectLibrary.IsControl(type) && Has(EffectType.Stability)) return;
            if (ApplyFilter != null && !ApplyFilter(type, stacks, duration)) return;
            if (durationBonus < 0f)
            {
                durationBonus = 0f;
                if (source != null && source.Stats != null)
                {
                    if (EffectLibrary.IsBoon(type)) durationBonus = source.Stats.BoonDurationBonus;
                    else if (EffectLibrary.IsCondition(type)) durationBonus = source.Stats.ConditionDurationBonus;
                    if (source.Modifiers != null) durationBonus += source.Modifiers.DurationBonus(type);
                }
            }
            duration *= 1f + durationBonus;
            int max = EffectLibrary.MaxStacks(type);
            if (EffectLibrary.Rule(type) == StackRule.Duration || max <= 1)
            {
                var existing = _stacks.Find(s => s.Type == type);
                float cap = EffectLibrary.MaxDuration(type);
                if (existing != null) { existing.Remaining = Mathf.Min(existing.Remaining + duration, cap); existing.Total = Mathf.Max(existing.Total, existing.Remaining); }
                else _stacks.Add(new Stack { Type = type, Remaining = Mathf.Min(duration, cap), Total = Mathf.Min(duration, cap), Source = source });
            }
            else
            {
                for (int i = 0; i < stacks; i++)
                {
                    if (GetStacks(type) < max) _stacks.Add(new Stack { Type = type, Remaining = duration, Total = duration, Source = source });
                    else
                    {
                        Stack shortest = null;
                        foreach (var s in _stacks) if (s.Type == type && (shortest == null || s.Remaining < shortest.Remaining)) shortest = s;
                        if (shortest != null && shortest.Remaining < duration) { shortest.Remaining = duration; shortest.Total = duration; shortest.Source = source; }
                    }
                }
            }
            Changed?.Invoke();
            AppliedGlobal?.Invoke(this, type);
        }

        public void Apply(EffectApplication a, Combatant source = null) => Apply(a.Type, a.Stacks, a.Duration, source);

        public int GetStacks(EffectType type)
        {
            int n = 0;
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].Type == type) n++;
            return n;
        }

        public bool Has(EffectType type)
        {
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].Type == type) return true;
            return false;
        }

        public float GetRemaining(EffectType type)
        {
            float r = 0f;
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].Type == type) r = Mathf.Max(r, _stacks[i].Remaining);
            return r;
        }

        public float GetFraction(EffectType type)
        {
            float r = 0f, t = 1f;
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].Type == type && _stacks[i].Remaining > r) { r = _stacks[i].Remaining; t = Mathf.Max(0.01f, _stacks[i].Total); }
            return Mathf.Clamp01(r / t);
        }

        /// <summary>Verbraucht einen Effekt (Aegis, Blindheit).</summary>
        public bool Consume(EffectType type)
        {
            int idx = _stacks.FindIndex(s => s.Type == type);
            if (idx < 0) return false;
            _stacks.RemoveAt(idx);
            Changed?.Invoke();
            return true;
        }

        public void Remove(EffectType type) { if (_stacks.RemoveAll(s => s.Type == type) > 0) Changed?.Invoke(); }
        public void Clear() { _stacks.Clear(); Changed?.Invoke(); }
        public void ClearConditions() { if (_stacks.RemoveAll(s => EffectLibrary.IsCondition(s.Type) || EffectLibrary.IsControl(s.Type)) > 0) Changed?.Invoke(); }

        /// <summary>Entfernt bis zu n Zustandsarten (GW2: Reinigen entfernt ganze Zustände).</summary>
        public int Cleanse(int count)
        {
            int removed = 0;
            while (removed < count)
            {
                var s = _stacks.Find(x => EffectLibrary.IsCondition(x.Type));
                if (s == null) break;
                _stacks.RemoveAll(x => x.Type == s.Type);
                removed++;
            }
            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        public int ConditionCount()
        {
            int n = 0; var seen = new HashSet<EffectType>();
            foreach (var s in _stacks) if (EffectLibrary.IsCondition(s.Type) && seen.Add(s.Type)) n++;
            return n;
        }

        public IEnumerable<EffectType> ActiveTypes()
        {
            var seen = new HashSet<EffectType>();
            foreach (var s in _stacks) if (seen.Add(s.Type)) yield return s.Type;
        }

        public void FillActiveTypes(List<EffectType> boons, List<EffectType> conditions)
        {
            boons.Clear(); conditions.Clear();
            for (int i = 0; i < _stacks.Count; i++)
            {
                var t = _stacks[i].Type;
                var list = EffectLibrary.IsBoon(t) ? boons : conditions;
                if (!list.Contains(t)) list.Add(t);
            }
        }

        public void Contribute(AttributeAccumulator acc, int level)
        {
            int might = GetStacks(EffectType.Might);
            if (might > 0)
            {
                float v = might * EffectRules.MightPerStack(level);
                acc.Add(Attr.Power, v); acc.Add(Attr.ConditionDamage, v);
            }
            if (Has(EffectType.Fury)) acc.CritChanceBonus += EffectRules.FuryCritChance;
        }

        /// <summary>Lässt Zeit vergehen. DoT/HoT pulsieren 1× pro Sekunde.</summary>
        public void Tick(float dt, Combatant owner)
        {
            if (_stacks.Count == 0) { _pulseTimer = 0f; return; }
            _pulseTimer += dt;
            while (_pulseTimer >= 1f)
            {
                _pulseTimer -= 1f;
                if (owner != null) Pulse(owner);
            }
            bool changed = false;
            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                _stacks[i].Remaining -= dt;
                if (_stacks[i].Remaining <= 0f) { _stacks.RemoveAt(i); changed = true; }
            }
            if (changed) Changed?.Invoke();
        }

        void Pulse(Combatant owner)
        {
            if (owner.IsDead) return;
            bool resist = Has(EffectType.Resistance);
            float total = 0f, best = 0f; Combatant src = null; EffectType dom = EffectType.Bleeding;
            for (int i = 0; i < _stacks.Count; i++)
            {
                var s = _stacks[i];
                if (!EffectLibrary.IsCondition(s.Type) || resist) continue;
                float cd = s.Source != null && s.Source.Stats != null ? s.Source.Stats.Get(Attr.ConditionDamage) : 0f;
                int lvl = s.Source != null && s.Source.Stats != null ? s.Source.Stats.Level : owner.Stats.Level;
                float tick = DamageFormula.ConditionTick(s.Type, cd, lvl);
                if (tick > 0f) { total += tick; if (src == null) src = s.Source; if (tick > best) { best = tick; dom = s.Type; } }
            }
            if (total > 0f) owner.TakeDamage(new DamageInfo { Source = src, Target = owner, Amount = total, IsCondition = true, Point = owner.AimPoint, Condition = dom });
            if (Has(EffectType.Regeneration) && !owner.IsDead)
            {
                var regen = _stacks.Find(x => x.Type == EffectType.Regeneration);
                float hp = regen != null && regen.Source != null && regen.Source.Stats != null ? regen.Source.Stats.Get(Attr.HealingPower) : 0f;
                owner.Heal(DamageFormula.Heal(DamageFormula.ScaledBase(EffectRules.RegenerationBaseAt80, owner.Stats.Level), hp, EffectRules.RegenerationHealingCoeff));
            }
        }
    }
}
