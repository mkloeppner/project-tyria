using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Erzeugt Tooltip-Inhalte im GW2-Layout (Name, Typ, Abklingzeit rechts oben, Fakten mit Icon).</summary>
    public static partial class TooltipBuilder
    {
        static readonly Color Gray = new Color(0.75f, 0.75f, 0.8f);
        static readonly Color Fact = new Color(0.92f, 0.92f, 0.95f);

        /// <summary>Referenz-Rüstung für Tooltip-Schaden (GW2: 2.597 auf Level 80 → 2,6 × Primärbasis).</summary>
        public static float ReferenceArmor(int level) => 2.6f * AttributeSet.PrimaryBase(level);

        public static float SkillHitDamage(SkillDefinition s, Combatant caster)
        {
            if (s == null || caster == null || s.Coefficient <= 0f) return 0f;
            float mult = caster.Modifiers.OutgoingMultiplier(caster, null, s);
            return DamageFormula.Direct(DamageFormula.WeaponStrength(caster.Level), caster.Stats.Get(Attr.Power), s.Coefficient, ReferenceArmor(caster.Level)) * mult;
        }

        public static float SkillHeal(SkillDefinition s, Combatant caster)
        {
            if (s == null || caster == null || s.HealBase <= 0f) return 0f;
            return DamageFormula.Heal(DamageFormula.ScaledBase(s.HealBase, caster.Level), caster.Stats.Get(Attr.HealingPower), s.HealCoefficient);
        }

        static string SlotTypeName(SkillDefinition s)
        {
            switch (s.SlotType)
            {
                case SkillSlotType.Weapon: return "Waffenfertigkeit · " + (s.Weapon == WeaponKind.Staff ? "Stab" : "Großschwert");
                case SkillSlotType.Heal: return "Heilfertigkeit";
                case SkillSlotType.Utility: return "Hilfsfertigkeit · " + CategoryName(s.Category) + " (Tier " + s.Tier + ")";
                case SkillSlotType.Elite: return "Elitefertigkeit";
                case SkillSlotType.Downed: return "Am Boden";
            }
            return "";
        }

        public static string CategoryName(UtilityCategory c)
        {
            switch (c)
            {
                case UtilityCategory.Shout: return "Ruf"; case UtilityCategory.Meditation: return "Meditation"; case UtilityCategory.Trap: return "Falle";
                case UtilityCategory.Signal: return "Signal"; case UtilityCategory.Wilderness: return "Wildnis"; case UtilityCategory.Glyph: return "Glyphe";
                case UtilityCategory.Conjure: return "Beschwörung"; case UtilityCategory.Arcane: return "Arkan"; case UtilityCategory.Trick: return "Trick";
                case UtilityCategory.Venom: return "Gift"; case UtilityCategory.Deception: return "Täuschung"; case UtilityCategory.Stance: return "Haltung";
                case UtilityCategory.Unleash: return "Entfesselungs-Ruf"; case UtilityCategory.Mark: return "Markierung"; case UtilityCategory.Augment: return "Verstärkung";
                case UtilityCategory.Cantrip: return "Kantrip"; case UtilityCategory.Physical: return "Körperfertigkeit"; case UtilityCategory.Well: return "Brunnen";
                case UtilityCategory.Preparation: return "Vorbereitung"; case UtilityCategory.Mantra: return "Mantra"; case UtilityCategory.Symbol: return "Symbol";
                case UtilityCategory.Consecration: return "Weihe";
            }
            return "Fertigkeit";
        }

        static string FieldName(ComboFieldType f) => ComboFieldZone.FieldName(f);
        static string FinisherName(ComboFinisherType f)
        {
            switch (f) { case ComboFinisherType.Blast: return "Explosion"; case ComboFinisherType.Leap: return "Sprung"; case ComboFinisherType.Projectile: return "Projektil"; case ComboFinisherType.Whirl: return "Wirbel"; }
            return "-";
        }

        public static TooltipContent Skill(SkillDefinition s, Combatant caster, string lockedInfo = null)
        {
            if (s == null) return null;
            var c = new TooltipContent { Title = s.DisplayName, TitleColor = UiFactory.AccentColor, Subtitle = SlotTypeName(s) };
            if (s.Cooldown > 0f) c.TopRight = Mathf.RoundToInt(s.Cooldown * (caster != null ? caster.Modifiers.RechargeMultiplier(s) : 1f)) + " s";
            if (!string.IsNullOrEmpty(s.Description)) c.Add(s.Description, Gray);
            if (caster != null && s.Coefficient > 0f)
            {
                float d = SkillHitDamage(s, caster);
                c.Add(s.Hits > 1 ? "Schaden (" + s.Hits + "×): " + Mathf.RoundToInt(d).ToString("N0") : "Schaden: " + Mathf.RoundToInt(d).ToString("N0"), Fact, UiFactory.Sprite("fact_damage"));
            }
            if (caster != null && s.HealBase > 0f) c.Add("Heilung: " + Mathf.RoundToInt(SkillHeal(s, caster)).ToString("N0"), new Color(0.6f, 1f, 0.6f), UiFactory.Sprite("fact_heal"));
            if (s.CastTime > 0f) c.Add("Wirkzeit: " + s.CastTime.ToString("0.##") + " s", Fact, UiFactory.Sprite("fact_time"));
            if (s.Range > 0f) c.Add("Reichweite: " + Mathf.RoundToInt(s.Range * 40f) + " (" + s.Range.ToString("0.#") + " m)", Fact, UiFactory.Sprite("fact_range"));
            if (s.Radius > 0f && (s.Delivery == SkillDelivery.GroundTarget || s.Delivery == SkillDelivery.Self || s.Hits > 1)) c.Add("Radius: " + s.Radius.ToString("0.#") + " m", Fact, UiFactory.Sprite("fact_radius"));
            if (s.Coefficient > 0f && s.Delivery != SkillDelivery.Projectile) c.Add("Anzahl der Ziele: " + s.TargetCount, Fact, UiFactory.Sprite("fact_targets"));
            foreach (var e in s.SelfBuffs) AddEffectFact(c, e, caster);
            foreach (var e in s.TargetBuffs) AddEffectFact(c, e, caster);
            if (s.CleanseCount > 0) c.Add("Entfernt Zustände: " + s.CleanseCount, Fact, UiFactory.Sprite("fact_cleanse"));
            if (s.Field != ComboFieldType.None) c.Add("Combo-Feld: " + FieldName(s.Field), ComboFieldZone.FieldColor(s.Field) * 1.3f, UiFactory.Sprite("fact_field"));
            if (s.Finisher != ComboFinisherType.None) c.Add("Combo-Ende: " + FinisherName(s.Finisher), new Color(1f, 0.85f, 0.5f), UiFactory.Sprite("fact_finisher"));
            // Fix-Lauf F1: Auto-Angriffs-Kette – alle Glieder mit Fakten (GW2-Darstellung „Kette“)
            var root = s.ChainRoot != null ? s.ChainRoot : s;
            if (root.Chain != null && root.Chain.Length > 0)
            {
                int pos = s.ChainPosition(out int len);
                c.Add("Kette (" + len + " Glieder):", UiFactory.AccentColor);
                for (int i = 0; i < len; i++)
                {
                    var link = i == 0 ? root : root.Chain[i - 1]; if (link == null) continue;
                    string dmg = caster != null && link.Coefficient > 0f ? " – Schaden " + Mathf.RoundToInt(SkillHitDamage(link, caster)).ToString("N0") : " – Koeffizient " + link.Coefficient.ToString("0.00");
                    string extra = link.ChainBonus ? " · Abschluss: " + BonusText(link, root) : "";
                    c.Add((i + 1 == pos ? "▶ " : "   ") + (i + 1) + "/" + len + " " + link.DisplayName + dmg + extra, i + 1 == pos ? Fact : Gray);
                }
            }
            if (!string.IsNullOrEmpty(lockedInfo)) { c.Footer = lockedInfo; c.FooterColor = new Color(1f, 0.6f, 0.4f); }
            return c;
        }

        static string BonusText(SkillDefinition link, SkillDefinition root)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (root.Coefficient > 0f) parts.Add("+" + Mathf.RoundToInt((link.Coefficient / root.Coefficient - 1f) * 100f) + " %");
            if (link.TargetCount != root.TargetCount) parts.Add(link.TargetCount + " Ziele");
            foreach (var e in link.TargetBuffs) if (!root.TargetBuffs.Contains(e)) parts.Add((e.Stacks > 1 ? e.Stacks + " " : "") + EffectLibrary.Name(e.Type));
            foreach (var e in link.SelfBuffs) if (!root.SelfBuffs.Contains(e)) parts.Add(EffectLibrary.Name(e.Type) + " (du)");
            return string.Join(", ", parts);
        }

        static void AddEffectFact(TooltipContent c, EffectApplication e, Combatant caster)
        {
            float dur = e.Duration;
            if (caster != null)
            {
                float bonus = EffectLibrary.IsBoon(e.Type) ? caster.Stats.BoonDurationBonus : EffectLibrary.IsCondition(e.Type) ? caster.Stats.ConditionDurationBonus : 0f;
                bonus += caster.Modifiers.DurationBonus(e.Type);
                dur *= 1f + bonus;
            }
            string name = EffectLibrary.Name(e.Type) + (e.Stacks > 1 ? " (" + e.Stacks + ")" : "");
            c.Add(name + ": " + dur.ToString("0.#") + " s", Fact, UiFactory.Sprite(IconLibrary.EffectId(e.Type)));
        }

        public static TooltipContent Effect(Combatant target, EffectType t)
        {
            var c = new TooltipContent { Title = EffectLibrary.Name(t), TitleColor = EffectLibrary.IsBoon(t) ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.55f, 0.5f) };
            c.Subtitle = EffectLibrary.IsBoon(t) ? "Segen" : EffectLibrary.IsCondition(t) ? "Zustand" : "Effekt";
            var a = EffectLibrary.Asset(t);
            c.Add(a != null ? a.Description : EffectCatalog.Get(t).Desc, Gray);
            if (target != null)
            {
                int n = target.Effects.GetStacks(t);
                if (n > 1) c.Add("Stapel: " + n, Fact);
                c.Add("Verbleibend: " + target.Effects.GetRemaining(t).ToString("0.0") + " s", Fact);
            }
            if (a != null && !a.Implemented) { c.Footer = "Im Prototyp nur Anzeige (siehe KNOWN_ISSUES)."; }
            return c;
        }
    }
}
