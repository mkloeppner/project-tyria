using UnityEngine;

namespace ProjectTyrant
{
    public static partial class TooltipBuilder
    {
        public static TooltipContent Trait(TraitDefinition t, SpecializationDefinition spec, string state)
        {
            if (t == null) return null;
            var c = new TooltipContent
            {
                Title = t.DisplayName,
                TitleColor = spec != null ? spec.Color : UiFactory.AccentColor,
                Subtitle = (t.Major ? "Große Eigenschaft" : "Kleine Eigenschaft") + " · " + (spec != null ? spec.DisplayName : "") + " · Rang " + t.Tier,
            };
            c.Add(t.Description, Fact);
            foreach (var m in t.Modifiers)
            {
                string fact = ModifierFact(m);
                if (fact != null) c.Add(fact, Gray);
            }
            if (!string.IsNullOrEmpty(state)) { c.Footer = state; c.FooterColor = state.StartsWith("Aktiv") ? new Color(0.6f, 1f, 0.6f) : new Color(0.6f, 0.9f, 1f); }
            return c;
        }

        /// <summary>Berechnete Kurzfakten zu einem Modifikator (aktueller Stufenwert bei Attributen).</summary>
        static string ModifierFact(ModifierSpec m)
        {
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            int lvl = p != null ? p.Level : 1;
            switch (m.Kind)
            {
                case ModifierKind.AttributeFlat: return "• +" + Mathf.RoundToInt(m.Value * AttributeSet.LevelScale(lvl)) + " " + AttributeSet.AttrName(m.Attr) + " auf deiner Stufe (" + m.Value + " auf Stufe 80)";
                case ModifierKind.AttributeConversion: return p != null ? "• derzeit +" + Mathf.RoundToInt(p.Stats.Get(m.Attr2) * m.Value) + " " + AttributeSet.AttrName(m.Attr) : null;
                case ModifierKind.TriggerApplyEffect: return "• " + EffectLibrary.Name(m.Effect) + (m.Stacks > 1 ? " ×" + m.Stacks : "") + ", " + m.Duration.ToString("0.#") + " s · interne Abklingzeit " + m.InternalCooldown.ToString("0.#") + " s";
                case ModifierKind.TriggerHeal:
                    if (p == null) return null;
                    return "• Heilung: " + Mathf.RoundToInt(DamageFormula.Heal(DamageFormula.ScaledBase(m.Value, lvl), p.Stats.Get(Attr.HealingPower), m.Value2)).ToString("N0");
            }
            return null;
        }
    }
}
