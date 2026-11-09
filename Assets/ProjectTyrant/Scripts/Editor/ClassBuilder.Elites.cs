using System.Collections.Generic;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M26: Elite-Spezialisierungen aus kompakten Tabellen – Waffe (5 Skills, beim Elementarweber 5 je Einstimmung),
    /// Skilltyp (1 Heil, 3 Hilfe, 1 Elite), Mechanik-Skills, Eigenschaftslinie (3 Neben + 9 Haupt). Zeitachse/VFX/SFX leitet
    /// der SkillTimelineBuilder wie bei allen Skills ab; die Signatur ist die eigene Palette (Farbe + Sound-Motiv).
    /// </summary>
    public static partial class ClassBuilder
    {
        struct Theme
        {
            public EffectType Cond, Boon; public Attr A1, A2, A3; public ComboFieldType Field;
            public Theme(EffectType cond, EffectType boon, Attr a1, Attr a2, Attr a3, ComboFieldType field = ComboFieldType.None) { Cond = cond; Boon = boon; A1 = a1; A2 = a2; A3 = a3; Field = field; }
        }

        static EliteSpecDefinition _spec; static string _sid; static Theme _th; static bool _thiefSet;

        static string EffName(EffectType t) => EffectCatalog.Get(t).Name;
        static string Short(string n) => n.Length <= 7 ? n : n.Substring(0, 6) + ".";

        static EliteSpecDefinition BeginSpec(string sid, string name, Color accent, VfxPalette pal, Theme th, EliteMechanic mech, string label, string mechText, UtilityCategory type, string desc, string pose)
        {
            _sid = sid; _th = th; _pal = pal; _color = accent; _style = null;
            EnsureFolder(Root + "/" + _cls + "/Elite");
            var e = Asset<EliteSpecDefinition>(Root + "/" + _cls + "/Elite/Elite_" + sid + ".asset");
            e.Id = sid; e.DisplayName = name; e.Description = desc; e.Color = accent; e.Accent = accent; e.ClassId = _cls;
            e.Mechanic = mech; e.MechanicLabel = label; e.MechanicSummary = mechText; e.SkillType = type; e.Palette = pal; e.SoundMotif = pal.ToString(); e.CastPose = pose;
            e.TypeSkills = new List<SkillDefinition>(); e.MechanicSkills = new List<SkillDefinition>();
            _spec = e; return e;
        }

        static SkillDefinition ES(string key, string name, string label, SkillDelivery del, float cast, float cd, float range, float coef, string desc)
        {
            var s = S(_sid + "_" + key, name, label, del, cast, cd, range, coef, desc);
            s.Palette = _pal; return s;
        }

        // ------------------------------------------------------------------ Waffe (nicht Elementarweber)
        /// <summary>5 Skills nach Rollen: 1 Grundangriff (Nahkampf mit Kette), 2 Mehrfachtreffer, 3 Mobilität, 4 Kontrolle, 5 großer Flächen-Skill.
        /// n = 5 Namen (+2 Kettenglieder bei Nahkampf, +1 Tarnungsangriff bei der Schattenklinge).</summary>
        static WeaponSet EWeapon(string name, WeaponKind kind, WeaponVisual vis, string style, bool ranged, float range, bool support, string[] n)
        {
            _style = style; bool thief = _cls == "schattenklinge";
            var c = _th.Cond; var b = _th.Boon;
            var w1 = ES("W1", n[0], Short(n[0]), ranged ? SkillDelivery.Projectile : SkillDelivery.Melee, ranged ? 0.6f : 0.5f, 0f, ranged ? range : 3f, 0.72f,
                ranged ? "Grundangriff auf bis zu " + range + " m; " + EffName(c) + " (1). Projektil-Finisher." : "Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht " + EffName(c) + ".");
            if (ranged) { w1.Finisher = ComboFinisherType.Projectile; w1.TargetBuffs.Add(B(c, 1, 4f)); w1.ProjectileSpeed = 40f; }
            else Chain(w1, n[5], n[6], 0.62f, 0.7f, 0.82f, B(c, 2, 5f));
            if (support) { w1.HealBase = 300f; w1.HealCoefficient = 0.05f; w1.ShareWithAllies = true; w1.Radius = 4f; w1.Description += " Heilt Verbündete nahe dem Ziel leicht."; }

            var w2 = ranged
                ? ES("W2", n[1], Short(n[1]), SkillDelivery.Projectile, 0.5f, 8f, range, 0.42f, "Drei schnelle Geschosse; je Treffer " + EffName(c) + ".")
                : ES("W2", n[1], Short(n[1]), SkillDelivery.Self, 0.3f, 8f, 0f, 0.36f, "Wirbelnder Angriff um dich (4 Treffer), " + EffName(c) + ". Wirbel-Finisher.");
            if (ranged) { w2.Hits = 3; w2.HitInterval = 0.2f; w2.TargetBuffs.Add(B(c, 1, 5f)); }
            else { w2.Hits = 4; w2.HitInterval = 0.18f; w2.Radius = 3.2f; w2.Finisher = ComboFinisherType.Whirl; w2.TargetBuffs.Add(B(c, 1, 5f)); }

            var w3 = ranged
                ? ES("W3", n[2], Short(n[2]), SkillDelivery.Projectile, 0.3f, 12f, range, 0.5f, "Schuss aus der Ausweichbewegung; " + EffName(EffectType.Swiftness) + " und " + EffName(b) + " für dich.")
                : ES("W3", n[2], Short(n[2]), SkillDelivery.Leap, 0.3f, 12f, 9f, 0.95f, "Sprung zum Ziel (9 m). Sprung-Finisher; " + EffName(b) + " für dich.");
            if (ranged) { w3.EvadesDuringCast = true; w3.SelfBuffs.Add(B(EffectType.Swiftness, 1, 5f)); }
            else { w3.LeapDistance = 9f; w3.Finisher = ComboFinisherType.Leap; }
            w3.SelfBuffs.Add(B(b, b == EffectType.Might ? 3 : 1, 6f));

            var w4 = ES("W4", n[3], Short(n[3]), ranged ? SkillDelivery.Projectile : SkillDelivery.Melee, 0.5f, 18f, ranged ? range : 3.5f, 0.65f,
                ranged ? "Kontrollschuss: Immobilisierung (2 s) und Schwäche." : "Wuchtiger Schlag, betäubt (1 s) und unterbricht.");
            if (ranged) { w4.TargetBuffs.Add(B(EffectType.Immobilize, 1, 2f)); w4.TargetBuffs.Add(B(EffectType.Weakness, 1, 4f)); }
            else { w4.Special = SkillSpecial.Interrupt; w4.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); }
            if (support) { w4.Delivery = SkillDelivery.Self; w4.Range = 0f; w4.Radius = 6f; w4.Coefficient = 0f; w4.TargetBuffs.Clear(); w4.ShareWithAllies = true; w4.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); w4.CleanseCount = 1; w4.Description = "Schützende Welle: Schutz für dich und Verbündete, entfernt einen Zustand."; }

            var w5 = ES("W5", n[4], Short(n[4]), SkillDelivery.GroundTarget, 0.75f, 25f, ranged ? range : 12f, 0.6f, "Großer Flächen-Skill am Zielort (3 Wellen), " + EffName(c) + " (2). Explosions-Finisher.");
            w5.Radius = 4.5f; w5.Hits = 3; w5.HitInterval = 0.7f; w5.Finisher = ComboFinisherType.Blast; w5.TargetBuffs.Add(B(c, 2, 5f));
            if (_th.Field != ComboFieldType.None) { w5.Field = _th.Field; w5.FieldDuration = 4f; w5.FieldTickCoefficient = 0.1f; w5.Finisher = ComboFinisherType.None; w5.Description = "Großer Flächen-Skill am Zielort (3 Wellen), " + EffName(c) + " (2); hinterlässt ein " + ComboFieldZone.FieldName(_th.Field) + "-Feld."; }
            if (support) { w5.HealBase = 3200f; w5.HealCoefficient = 0.6f; w5.ShareWithAllies = true; w5.Coefficient = 0.3f; w5.SelfBuffs.Add(B(EffectType.Regeneration, 1, 6f)); w5.Description = "Heilender Kreis am Zielort: heilt Verbündete, Regeneration; schwacher Schaden an Gegnern."; }

            var arr = new[] { w1, w2, w3, w4, w5 };
            if (thief) { int[] cost = { 0, 3, 4, 5, 6 }; for (int i = 1; i < 5; i++) { arr[i].InitiativeCost = cost[i]; arr[i].Cooldown = 0f; } }
            var set = Set(_sid + "_Waffe", name, kind, vis, _color, arr);
            if (thief)
            {
                var st = ES("WS", n[7], Short(n[7]), ranged ? SkillDelivery.Projectile : SkillDelivery.Melee, 0.4f, 0f, ranged ? range : 3f, 2.0f, "Tarnungsangriff: verlässt die Tarnung mit einem schweren Treffer, 5 Verwundbarkeit.");
                st.StealthAttack = true; st.TargetBuffs.Add(B(EffectType.Vulnerability, 5, 8f)); st.Weapon = kind;
                set.StealthSkill = st;
            }
            _style = null;
            return set;
        }

        // ------------------------------------------------------------------ Waffe Elementarweber (je Einstimmung, Vorlage)
        static readonly string[] ElemWord = { "Glut", "Gischt", "Donner", "Fels" };

        /// <summary>Elementarweber-Waffe: Plätze aus dem Kern-Set übernehmen (reuse[slot] = Basis-Platz) oder neu aus Nomen bauen.</summary>
        static WeaponSet EWeaverWeapon(string name, WeaponKind kind, WeaponVisual vis, string style, WeaponSet baseSet, bool[] newSlot, string[] nouns, bool melee, float range)
        {
            var att = new SkillDefinition[20]; int ni = 0;
            var slotNoun = new string[5]; for (int i = 0; i < 5; i++) if (newSlot[i]) slotNoun[i] = nouns[ni++];
            for (int e = 0; e < 4; e++)
                for (int slot = 0; slot < 5; slot++)
                {
                    if (!newSlot[slot] && baseSet != null && baseSet.Attunement != null && baseSet.Attunement.Length == 20) { att[e * 5 + slot] = baseSet.Attunement[e * 5 + slot]; continue; }
                    string nm = ElemWord[e] + slotNoun[slot];
                    att[e * 5 + slot] = EWeaverSkill((Element)e, slot, nm, melee, range, style);
                }
            _pal = _spec.Palette; _style = null;
            var set = Set(_sid + "_Waffe", name, kind, vis, _color, att[0], att[1], att[2], att[3], att[4]);
            set.Attunement = att;
            // Set() setzt die Waffe der ersten fünf Skills – übernommene Kern-Skills behalten ihre Waffe
            for (int i = 0; i < 20; i++) att[i].Weapon = newSlot[i % 5] ? kind : baseSet != null ? baseSet.Kind : kind;
            return set;
        }

        static SkillDefinition EWeaverSkill(Element e, int slot, string name, bool melee, float range, string style)
        {
            float k = e == Element.Fire ? 1.15f : e == Element.Air ? 1.1f : e == Element.Water ? 0.8f : 0.9f;
            _pal = AttunementModel.PaletteOf(e); _style = style;
            string key = "E" + (int)e + (slot + 1);
            SkillDefinition s;
            switch (slot)
            {
                case 0: s = ES(key, name, Short(name), melee ? SkillDelivery.Melee : SkillDelivery.Projectile, melee ? 0.5f : 0.6f, 0f, melee ? 3f : range, 0.72f * k, "Grundangriff der Einstimmung."); if (!melee) s.Finisher = ComboFinisherType.Projectile; break;
                case 1: s = ES(key, name, "2", melee ? SkillDelivery.Self : SkillDelivery.GroundTarget, 0.5f, 7f, melee ? 0f : range, 1.0f * k, "Flächenangriff."); s.Radius = 3.5f; s.Hits = e == Element.Air ? 3 : 1; s.HitInterval = 0.3f; break;
                case 2: s = ES(key, name, "3", melee ? SkillDelivery.Leap : SkillDelivery.GroundTarget, melee ? 0.3f : 0.75f, 14f, melee ? 9f : range, (melee ? 0.9f : 0.35f) * k, melee ? "Sprung zum Ziel. Sprung-Finisher." : "Elementarfeld am Zielort (5 s).");
                    if (melee) { s.LeapDistance = 9f; s.Finisher = ComboFinisherType.Leap; } else { s.Radius = 4f; s.Field = AttunementModel.FieldOf(e); s.FieldDuration = 5f; s.FieldTickCoefficient = e == Element.Water ? 0f : 0.2f; if (e == Element.Earth) { s.Finisher = ComboFinisherType.Blast; s.Field = ComboFieldType.None; } }
                    break;
                case 3: s = ES(key, name, "4", SkillDelivery.Self, 0.25f, 20f, 0f, 0.4f * k, "Kontrolle und Schutz um dich."); s.Radius = 4f; break;
                default: s = ES(key, name, "5", SkillDelivery.Self, 0.75f, 28f, 0f, 1.5f * k, "Starker Elementar-Skill um dich (Explosions-Finisher)."); s.Radius = 5f; s.Hits = 2; s.HitInterval = 0.6f; s.Finisher = ComboFinisherType.Blast; break;
            }
            switch (e)
            {
                case Element.Fire: s.TargetBuffs.Add(B(EffectType.Burning, slot == 0 ? 1 : 2, 4f)); if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Might, 3, 8f)); s.Description = "Flammen-Aura: 3 Macht, Brennen um dich."; } break;
                case Element.Water: s.TargetBuffs.Add(B(EffectType.Chill, 1, slot == 0 ? 1.5f : 2.5f)); if (slot != 0) { s.HealBase = 1400f + 400f * slot; s.HealCoefficient = 0.3f; s.ShareWithAllies = true; } if (slot == 3) { s.CleanseCount = 1; s.Description = "Heilt, entfernt einen Zustand."; } break;
                case Element.Air: s.TargetBuffs.Add(B(EffectType.Vulnerability, slot == 0 ? 1 : 2, 6f)); if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Swiftness, 1, 8f)); s.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); s.Special = SkillSpecial.Interrupt; s.Description = "Windstoß: betäubt, Eile für dich."; } break;
                default: s.TargetBuffs.Add(B(EffectType.Bleeding, slot == 0 ? 1 : 2, 6f)); if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); s.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f)); s.Description = "Erdfessel: Immobilisierung, Schutz für dich."; } break;
            }
            s.Element = e;
            return s;
        }

        // ------------------------------------------------------------------ Skilltyp: 1 Heil, 3 Hilfe (Stufe 1–3), 1 Elite
        static void ETypeSkills(UtilityCategory cat, string[] n)
        {
            var c = _th.Cond; var b = _th.Boon; string cn = TooltipBuilder.CategoryName(cat);
            var h = Heal(ES("T0_" + Clean(n[0]), n[0], Short(n[0]), SkillDelivery.Self, 0.75f, 25f, 0f, 0f, cn + ": Heilung; " + EffName(b) + " für dich."), 5200f, 1f, true);
            h.Category = cat; h.SelfBuffs.Add(B(b, b == EffectType.Might ? 3 : 1, 6f));
            var list = new List<SkillDefinition> { h };
            for (int i = 1; i <= 3; i++)
            {
                var u = Util(ES("T" + i + "_" + Clean(n[i]), n[i], Short(n[i]), SkillDelivery.Self, 0.25f, 20f + 5f * i, 0f, 0f, cn + "."), cat, i, true);
                u.UnlockCost = 0;
                TypeEffect(u, cat, i, c, b);
                list.Add(u);
            }
            var el = Elite(ES("T4_" + Clean(n[4]), n[4], "ELITE", SkillDelivery.Self, 0.75f, 90f, 0f, 1.2f, "Elite (" + cn + ")."), true);
            el.Radius = 6f; el.Finisher = ComboFinisherType.Blast; el.ShareWithAllies = true;
            el.SelfBuffs.Add(B(b, b == EffectType.Might ? 10 : 1, 10f)); el.SelfBuffs.Add(B(EffectType.Stability, 3, 6f)); el.TargetBuffs.Add(B(c, 4, 6f));
            el.Description = "Elite (" + cn + "): " + EffName(c) + " (4) an Gegnern um dich, " + EffName(b) + " und Stabilität für dich und Verbündete. Explosions-Finisher.";
            list.Add(el);
            _spec.TypeSkills = list;
        }

        /// <summary>Wirkung je Skilltyp (Stufe 1–3 unterscheiden sich in Stärke und Rolle).</summary>
        static void TypeEffect(SkillDefinition u, UtilityCategory cat, int tier, EffectType c, EffectType b)
        {
            string cn = TooltipBuilder.CategoryName(cat);
            switch (cat)
            {
                case UtilityCategory.Glyph:   // Hainwächter: Schaden + Heilung; in der Hainform zusätzlich Regeneration für Verbündete (ClassMechanicHost)
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 20f; u.Radius = 4f; u.Coefficient = 0.5f + 0.2f * tier; u.HealBase = 1200f * tier; u.HealCoefficient = 0.3f; u.ShareWithAllies = true; u.TargetBuffs.Add(B(c, tier, 4f));
                    u.Description = cn + ": trifft Gegner am Zielort (" + EffName(c) + ") und heilt Verbündete dort; in der Hainform zusätzlich Regeneration."; break;
                case UtilityCategory.Stance:
                    u.Cast(0f); u.SelfBuffs.Add(B(b, b == EffectType.Might ? 4 + 2 * tier : 1, 8f + 2f * tier)); u.SelfBuffs.Add(B(tier == 1 ? EffectType.Fury : tier == 2 ? EffectType.Swiftness : EffectType.Resistance, 1, 8f)); u.SelfBuffs.Add(B(EffectType.Vulnerability, 2, 4f));
                    u.Description = cn + ": lange Stärkung (" + EffName(b) + ", " + EffName(u.SelfBuffs[1].Type) + "), dafür 2 Verwundbarkeit für dich."; break;
                case UtilityCategory.Unleash:
                    u.Radius = 6f; u.ShareWithAllies = true; u.SelfBuffs.Add(B(EffectType.Might, 3 * tier, 8f));
                    u.Description = cn + ": dein Gefährte setzt Kontrolle ein (Betäubung/Verkrüppelung am Ziel), " + 3 * tier + " Macht für euch."; break;
                case UtilityCategory.Mark:
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 30f; u.Radius = 4f; u.Coefficient = 0.3f; u.TargetBuffs.Add(B(tier == 1 ? EffectType.Vulnerability : tier == 2 ? EffectType.Weakness : EffectType.Blind, tier == 1 ? 6 : 1, 6f));
                    u.Description = cn + ": markiert den Zielbereich – " + EffName(u.TargetBuffs[0].Type) + "; Schussfenster für Präzisionsschüsse."; break;
                case UtilityCategory.Shout:
                    u.Radius = 8f; u.ShareWithAllies = true; u.CleanseCount = tier; u.SelfBuffs.Add(B(tier == 1 ? EffectType.Swiftness : tier == 2 ? EffectType.Protection : EffectType.Fury, 1, 6f)); u.SelfBuffs.Add(B(b, 1, 6f));
                    u.Description = cn + ": Segen für dich und Verbündete, entfernt " + tier + " Zustand/Zustände."; break;
                case UtilityCategory.Augment:
                    u.Cast(0f); u.Radius = 5f; u.Finisher = ComboFinisherType.Blast; u.SelfBuffs.Add(B(tier == 1 ? EffectType.Might : tier == 2 ? EffectType.Protection : EffectType.Quickness, tier == 1 ? 5 : 1, 6f));
                    u.Description = cn + ": Kampfsegen für dich; Explosions-Finisher (Combo-Ende)."; break;
                case UtilityCategory.Conjure:
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 18f; u.Radius = 3.5f; u.Hits = 6 + 2 * tier; u.HitInterval = 1f; u.Coefficient = 0.25f; u.Field = ComboFieldType.Fire; u.FieldDuration = 8f; u.FieldTickCoefficient = 0.1f; u.TargetBuffs.Add(B(EffectType.Burning, 1, 3f));
                    u.Description = cn + ": ruft einen ortsfesten Elementar-Diener (" + (6 + 2 * tier) + " s), der Gegner im Umkreis jede Sekunde trifft und brennt (Feuerfeld)."; break;
                case UtilityCategory.Cantrip:
                    u.Cast(0f); u.SelfBuffs.Add(B(tier == 1 ? EffectType.Vigor : tier == 2 ? EffectType.Resistance : EffectType.Fury, 1, 6f)); if (tier == 2) u.CleanseCount = 2; if (tier == 3) u.SelfBuffs.Add(B(EffectType.Might, 5, 10f));
                    u.Description = cn + ": Sofort-Skill, bricht die Tarnung nicht – " + EffName(u.SelfBuffs[0].Type) + (tier == 2 ? ", entfernt 2 Zustände" : "") + "."; break;
                case UtilityCategory.Physical:
                    u.Delivery = tier == 2 ? SkillDelivery.Leap : SkillDelivery.Melee; u.Range = tier == 2 ? 10f : 3.5f; u.LeapDistance = tier == 2 ? 10f : 0f; u.Coefficient = 0.9f + 0.2f * tier; u.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); u.SelfBuffs.Add(B(EffectType.Stability, 1, 3f)); u.Special = SkillSpecial.Interrupt;
                    u.Description = cn + ": " + (tier == 2 ? "Sprungtritt" : "Tritt") + " betäubt (1 s), Stabilität für dich."; break;
                case UtilityCategory.Well:
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 18f; u.Radius = 4.5f; u.Hits = 3; u.HitInterval = 1f; u.Coefficient = 0.3f; u.ShareWithAllies = true; u.HealBase = 900f * tier; u.HealCoefficient = 0.25f; u.TargetBuffs.Add(B(c, 2, 4f));
                    if (tier == 3) { u.Special = SkillSpecial.Blink; u.LeapDistance = 12f; }
                    u.Description = cn + ": Schattenbrunnen am Zielort – heilt Verbündete, " + EffName(c) + " für Gegner" + (tier == 3 ? "; Schattenportal: du springst 12 m nach vorn" : "") + "."; break;
                case UtilityCategory.Preparation:
                case UtilityCategory.Trap:
                    u.Special = SkillSpecial.TrapPlace; u.TrapLifetime = 20f; u.Radius = 3f + 0.3f * tier; u.Coefficient = 0.6f + 0.2f * tier; u.TargetBuffs.Add(B(c, 2 + tier, 5f)); if (tier == 3) u.TargetBuffs.Add(B(EffectType.Immobilize, 1, 2f));
                    u.Description = cn + ": wird gelegt und löst bei Kontakt aus – " + EffName(c) + " (" + (2 + tier) + ")" + (tier == 3 ? ", Immobilisierung" : "") + "."; break;
                case UtilityCategory.Mantra:
                    u.Cast(0f); u.ShareWithAllies = true; u.Radius = 6f; u.SelfBuffs.Add(B(tier == 1 ? EffectType.Aegis : tier == 2 ? EffectType.Protection : EffectType.Stability, tier == 3 ? 3 : 1, 5f)); if (tier == 2) { u.HealBase = 1500f; u.HealCoefficient = 0.3f; }
                    u.Description = cn + ": aufgeladener Sofort-Skill – " + EffName(u.SelfBuffs[0].Type) + " für dich und Verbündete" + (tier == 2 ? ", heilt" : "") + "."; break;
                case UtilityCategory.Symbol:
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 15f; u.Radius = 4f; u.Hits = 4; u.HitInterval = 0.75f; u.Coefficient = 0.3f + 0.05f * tier; u.Field = ComboFieldType.Lightning; u.FieldDuration = 3f; u.TargetBuffs.Add(B(EffectType.Vulnerability, tier, 5f)); u.SelfBuffs.Add(B(EffectType.Swiftness, 1, 4f));
                    u.Description = cn + ": wanderndes Blitzsymbol am Zielort (4 Treffer, Blitzfeld), " + tier + " Verwundbarkeit."; break;
                case UtilityCategory.Consecration:
                    u.Delivery = SkillDelivery.GroundTarget; u.Range = 15f; u.Radius = 5f; u.Hits = 3; u.HitInterval = 1.5f; u.Coefficient = 0.2f; u.ShareWithAllies = true; u.SelfBuffs.Add(B(tier == 1 ? EffectType.Regeneration : tier == 2 ? EffectType.Protection : EffectType.Resolution, 1, 6f)); u.TargetBuffs.Add(B(c, 1, 4f));
                    u.Description = cn + ": Weihekreis am Zielort – " + EffName(u.SelfBuffs[0].Type) + " für Verbündete, " + EffName(c) + " für Gegner."; break;
                default:
                    u.SelfBuffs.Add(B(b, 1, 6f)); break;
            }
        }

        // ------------------------------------------------------------------ Eigenschaftslinie (3 Neben + 9 Haupt) aus Vorlage
        static string Describe(ModifierSpec m)
        {
            switch (m.Kind)
            {
                case ModifierKind.AttributeFlat: return "+" + m.Value + " " + AttributeSet.AttrName(m.Attr) + ".";
                case ModifierKind.AttributeConversion: return "+" + Mathf.RoundToInt(m.Value * 100) + " % deiner " + AttributeSet.AttrName(m.Attr2) + " als " + AttributeSet.AttrName(m.Attr) + ".";
                case ModifierKind.CritChance: return "+" + Mathf.RoundToInt(m.Value * 100) + " % kritische Trefferchance.";
                case ModifierKind.DamageVsCondition: return "+" + Mathf.RoundToInt(m.Value * 100) + " % Schaden gegen Ziele mit " + EffName(m.Effect) + ".";
                case ModifierKind.DamageVsLowHealth: return "+" + Mathf.RoundToInt(m.Value * 100) + " % Schaden gegen Ziele unter " + Mathf.RoundToInt(m.Value2 * 100) + " % Lebenspunkten.";
                case ModifierKind.DamageWhileBoon: return "+" + Mathf.RoundToInt(m.Value * 100) + " % Schaden, solange du " + EffName(m.Effect) + " hast.";
                case ModifierKind.CategoryDamage: return "+" + Mathf.RoundToInt(m.Value * 100) + " % Schaden mit " + (m.Weapon != WeaponKind.None ? "der Elite-Waffe" : TooltipBuilder.CategoryName(m.Category) + "-Fertigkeiten") + ".";
                case ModifierKind.CategoryRecharge: return (m.MatchSlotType ? (m.SlotType == SkillSlotType.Heal ? "Heilfertigkeiten" : m.SlotType == SkillSlotType.Elite ? "Elite-Fertigkeiten" : "Hilfsfertigkeiten") : m.Weapon != WeaponKind.None ? "Fertigkeiten der Elite-Waffe" : TooltipBuilder.CategoryName(m.Category) + "-Fertigkeiten") + " laden " + Mathf.RoundToInt(m.Value * 100) + " % schneller auf.";
                case ModifierKind.EffectDuration: return (m.Scope == DurationScope.AllBoons ? "Alle Segen" : m.Scope == DurationScope.AllConditions ? "Alle Zustände" : EffName(m.Effect)) + " halten " + Mathf.RoundToInt(m.Value * 100) + " % länger.";
                case ModifierKind.TriggerApplyEffect: return TriggerText(m.Trigger) + ": " + (m.Stacks > 1 ? m.Stacks + " " : "") + EffName(m.Effect) + (m.Target == ModifierTarget.Target ? " auf das Ziel" : " für dich") + " (" + m.Duration + " s, Abklingzeit " + m.InternalCooldown + " s).";
                case ModifierKind.TriggerHeal: return TriggerText(m.Trigger) + ": heilt dich (Abklingzeit " + m.InternalCooldown + " s).";
            }
            return "";
        }

        static string TriggerText(TriggerType t)
        {
            switch (t)
            {
                case TriggerType.Crit: return "Kritische Treffer"; case TriggerType.Dodge: return "Ausweichen"; case TriggerType.HealSkill: return "Heilfertigkeit";
                case TriggerType.WeaponSwap: return "Waffenwechsel"; case TriggerType.Kill: return "Besiegte Gegner"; case TriggerType.SkillUse: return "Fertigkeit eingesetzt";
                case TriggerType.DamageTaken: return "Schaden erleiden"; case TriggerType.Stealth: return "Tarnung"; case TriggerType.AttunementSwap: return "Einstimmung wechseln";
                case TriggerType.PetSwap: return "Gefährtenwechsel"; case TriggerType.EliteMechanic: return "Elite-Mechanik (F5) einsetzen";
            }
            return "Auslöser";
        }

        /// <summary>Linie aus 12 Namen; Wirkungen aus dem Thema der Spezialisierung (Attribute, Zustand, Segen, Waffe, Skilltyp).</summary>
        static void ELine(string[] names)
        {
            var w = _spec.Weapon != null ? _spec.Weapon.Kind : WeaponKind.None; var cat = _spec.SkillType; var c = _th.Cond; var b = _th.Boon;
            var mods = new ModifierSpec[][]
            {
                new[] { AttrFlat(_th.A1, 120) },
                new[] { Dur(b, 0.2f), AttrFlat(_th.A3, 60) },
                new[] { AttrFlat(_th.A2, 120) },
                new[] { WCd(w, 0.2f), WDmg(w, 0.05f) },
                new[] { CCd(cat, 0.2f), CDmg(cat, 0.07f) },
                new[] { On(TriggerType.EliteMechanic, ModifierTarget.Self, b, b == EffectType.Might ? 5 : 1, 6f, 8f) },
                new[] { VsEff(c, 0.1f) },
                new[] { On(TriggerType.Crit, ModifierTarget.Target, c, 1, 4f, 2f) },
                new[] { WhileB(b, 0.07f), Dur(b, 0.15f) },
                new[] { VsLow(0.5f, 0.12f) },
                new[] { On(TriggerType.Dodge, ModifierTarget.Self, EffectType.Vigor, 1, 3f, 10f), OnHeal(TriggerType.Dodge, 600f, 0.2f, 10f) },
                new[] { DurAll(DurationScope.AllConditions, 0.15f), Conv(_th.A1, _th.A2, 0.07f) },
            };
            string dir = Root + "/" + _cls + "/Traits";
            TraitDefinition Make(int i, int tier, bool major)
            {
                var a = Asset<TraitDefinition>(dir + "/Trait_" + _sid + "_" + i + ".asset");
                a.Id = _sid + "_t" + i; a.DisplayName = names[i]; a.Glyph = names[i].Substring(0, 1); a.Tier = tier; a.Major = major; a.Modifiers = new List<ModifierSpec>(mods[i]);
                var d = new System.Text.StringBuilder(); foreach (var m in mods[i]) { if (d.Length > 0) d.Append(' '); d.Append(Describe(m)); }
                a.Description = d.ToString();
                return a;
            }
            _spec.Minors = new TraitDefinition[3]; _spec.Majors = new TraitDefinition[9];
            for (int i = 0; i < 3; i++) _spec.Minors[i] = Make(i, i + 1, false);
            for (int i = 0; i < 9; i++) _spec.Majors[i] = Make(3 + i, i / 3 + 1, true);
        }

        /// <summary>Mechanik-Skill der Spezialisierung (Avatar, Überladen, Kombi, Schleier …).</summary>
        static SkillDefinition EM(string key, string name, SkillDelivery del, float cast, float cd, float range, float coef, string desc)
        {
            var s = ES(key, name, Short(name), del, cast, cd, range, coef, desc);
            s.StartsUnlocked = true; s.UnlockCost = 0; s.SlotType = SkillSlotType.Weapon; s.WeaponStyleOverride = _style ?? "";
            _spec.MechanicSkills.Add(s);
            return s;
        }

        static void EndSpec(string signature) { _spec.SignatureSkill = signature; _style = null; }
    }

    static class SkillDefExt
    {
        public static SkillDefinition Cast(this SkillDefinition s, float t) { s.CastTime = t; return s; }
    }
}
