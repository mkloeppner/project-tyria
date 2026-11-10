using System.Collections.Generic;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        // Namen je Waffe × Einstimmung (Feuer, Wasser, Luft, Erde) × Slot 1–5 (eigene Erfindungen)
        static readonly string[,,] WeaverNames =
        {
            { // Stab
                { "Glutkugel", "Lavaquell", "Flammenwall", "Brandschritt", "Sternenglut" },
                { "Wasserpfeil", "Eisregen", "Heilbrunnen", "Frostkegel", "Gezeitenregen" },
                { "Funkenbogen", "Kugelblitz", "Sturmfeld", "Windstoß", "Gewittersturm" },
                { "Steinsplitter", "Schlammgrube", "Erdwall", "Erdfessel", "Bergbeben" },
            },
            { // Zepter + Fokus
                { "Glutfaden", "Zündfunke", "Feuerstrahl", "Flammenschild", "Feuerring" },
                { "Wassernadel", "Quellschlag", "Nebelhauch", "Wellenschutz", "Eiswall" },
                { "Blitznadel", "Donnerkeil", "Funkenregen", "Windmantel", "Sturmauge" },
                { "Kieselwurf", "Felsspitze", "Staubwirbel", "Steinhaut", "Felsenkäfig" },
            },
            { // Dolch + Dolch
                { "Glutschnitt", "Flammenfächer", "Feuersprung", "Brandwirbel", "Aschemantel" },
                { "Eisschnitt", "Frostfächer", "Gischtsprung", "Heilwirbel", "Kälteschleier" },
                { "Blitzschnitt", "Funkenfächer", "Sturmsprung", "Wirbelsturm", "Donnerhall" },
                { "Steinschnitt", "Erdfächer", "Felssprung", "Sandwirbel", "Erdpanzer" },
            },
        };

        /// <summary>Slot-Rolle × Einstimmung → ein Waffenskill (Element-Vorlage).</summary>
        static SkillDefinition WeaverSkill(int weapon, Element e, int slot)
        {
            string wid = weapon == 0 ? "ST" : weapon == 1 ? "SF" : "DD";
            string name = WeaverNames[weapon, (int)e, slot];
            float dmgK = e == Element.Fire ? 1.15f : e == Element.Air ? 1.1f : e == Element.Water ? 0.8f : 0.9f;
            bool melee = weapon == 2;
            float range = weapon == 0 ? 24f : weapon == 1 ? 22f : 3f;
            _pal = AttunementModel.PaletteOf(e);
            _style = weapon == 0 ? "Staff" : weapon == 1 ? "Scepter" : "Dagger";
            SkillDefinition s;
            switch (slot)
            {
                case 0:   // Auto-Angriff
                    s = S(wid + (int)e + "1_" + Clean(name), name, name.Substring(0, Mathf.Min(6, name.Length)), melee ? SkillDelivery.Melee : SkillDelivery.Projectile, melee ? 0.5f : 0.6f, 0f, range, 0.7f * dmgK, "Grundangriff der Einstimmung.");
                    if (!melee) s.Finisher = ComboFinisherType.Projectile;
                    break;
                case 1:   // Flächen-/Kegel-Schaden
                    s = S(wid + (int)e + "2_" + Clean(name), name, "2", melee ? SkillDelivery.Self : SkillDelivery.GroundTarget, 0.5f, 6f + weapon, melee ? 0f : range, 1.0f * dmgK, "Flächenangriff.");
                    s.Radius = melee ? 3.5f : 3.5f; s.Hits = e == Element.Air ? 3 : 1; s.HitInterval = 0.3f;
                    break;
                case 2:   // Feld / Sprung
                    if (melee)
                    {
                        s = S(wid + (int)e + "3_" + Clean(name), name, "3", SkillDelivery.Leap, 0.25f, 12f, 9f, 0.9f * dmgK, "Sprung zum Ziel. Sprung-Finisher.");
                        s.LeapDistance = 9f; s.Finisher = ComboFinisherType.Leap;
                    }
                    else
                    {
                        s = S(wid + (int)e + "3_" + Clean(name), name, "3", SkillDelivery.GroundTarget, 0.75f, 18f, range, 0.3f * dmgK, "Elementarfeld am Zielort (6 s).");
                        s.Radius = 4f; s.Field = AttunementModel.FieldOf(e); s.FieldDuration = 6f; s.FieldTickCoefficient = e == Element.Water ? 0f : 0.2f;
                        if (e == Element.Earth) { s.Finisher = ComboFinisherType.Blast; s.Field = ComboFieldType.None; }
                    }
                    break;
                case 3:   // Kontrolle / Mobilität / Verteidigung
                    s = S(wid + (int)e + "4_" + Clean(name), name, "4", SkillDelivery.Self, 0.25f, 20f, 0f, 0.4f * dmgK, "Kontrolle und Schutz.");
                    s.Radius = 4f;
                    break;
                default:  // Großer Skill
                    s = S(wid + (int)e + "5_" + Clean(name), name, "5", melee ? SkillDelivery.Self : SkillDelivery.GroundTarget, 1.0f, 30f, melee ? 0f : range, 1.6f * dmgK, "Starker Elementar-Skill.");
                    s.Radius = 5f; s.Hits = melee ? 1 : 3; s.HitInterval = 0.8f; s.Finisher = ComboFinisherType.Blast;
                    break;
            }
            // Element-Thema
            switch (e)
            {
                case Element.Fire:
                    s.TargetBuffs.Add(B(EffectType.Burning, slot == 0 ? 1 : 2, 4f));
                    if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Might, 3, 8f)); s.Description = "Flammen-Aura: 3 Macht, Brennen um dich."; }
                    break;
                case Element.Water:
                    s.TargetBuffs.Add(B(EffectType.Chill, 1, slot == 0 ? 1.5f : 2.5f));
                    if (slot != 0) { s.HealBase = 1600f + 400f * slot; s.HealCoefficient = 0.3f; s.ShareWithAllies = true; }
                    if (slot == 3) { s.CleanseCount = 1; s.SelfBuffs.Add(B(EffectType.Regeneration, 1, 6f)); s.Description = "Heilt, reinigt einen Zustand, Regeneration."; }
                    break;
                case Element.Air:
                    s.TargetBuffs.Add(B(EffectType.Vulnerability, slot == 0 ? 1 : 2, 6f));
                    if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Swiftness, 1, 8f)); s.SelfBuffs.Add(B(EffectType.Fury, 1, 5f)); s.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); s.Special = SkillSpecial.Interrupt; s.Description = "Windstoß: betäubt, Eile und Wut für dich."; }
                    break;
                default:
                    s.TargetBuffs.Add(B(EffectType.Bleeding, slot == 0 ? 1 : 2, 6f));
                    if (slot == 3) { s.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); s.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f)); s.Description = "Erdfessel: Immobilisierung, Schutz für dich."; }
                    break;
            }
            if (slot == 4 && e == Element.Water) { s.HealBase = 4000f; s.HealCoefficient = 0.8f; s.Coefficient *= 0.5f; s.Description = "Heilender Regen: heilt Verbündete im Bereich."; }
            s.Element = e;
            return s;
        }

        static string Clean(string n)
        {
            var t = n.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue").Replace("ß", "ss");
            var sb = new System.Text.StringBuilder(); foreach (var ch in t) if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);   // nur gültige Dateinamen-Zeichen
            return sb.ToString();
        }

        /// <summary>Elementarweber: leichte Rüstung, Einstimmungen F1–F4, Stab · Zepter+Fokus · Dolch+Dolch; Glyphen, Beschwörungen, Arkan.</summary>
        static ClassDefinition BuildWeaver()
        {
            var col = new Color(0.95f, 0.55f, 0.25f);
            Begin("elementarweber", col, VfxPalette.Arkan);
            var d = Class("elementarweber", "Elementarweber", ArmorWeight.Light, col, new Color(0.35f, 0.6f, 1f), ClassMechanicKind.Attunement, "Einstimmung",
                new[] { "Feuer", "Wasser", "Luft", "Erde" },
                "Einstimmungen: F1 Feuer, F2 Wasser, F3 Luft, F4 Erde tauschen die Waffenskills 1–5 (10 s Abklingzeit der verlassenen Einstimmung). Wechsel lässt ein kurzes Elementarfeld zurück.",
                "Leichte Rüstung · vier Einstimmungen · Stab, Zepter + Fokus, Dolch + Dolch · Glyphen, Beschwörungen, Arkan.");
            d.SwapInCombat = false;
            var sets = new List<WeaponSet>();
            var kinds = new[] { WeaponKind.Staff, WeaponKind.ScepterFocus, WeaponKind.DaggerDagger };
            var vis = new[] { WeaponVisual.ElementStaff, WeaponVisual.ScepterFocus, WeaponVisual.DaggerDagger };   // M27: eigener Elementarstab (Meshy)
            var names = new[] { "Elementarstab", "Zepter & Fokus", "Doppeldolche" };
            for (int w = 0; w < 3; w++)
            {
                var att = new SkillDefinition[20];
                for (int e = 0; e < 4; e++) for (int slot = 0; slot < 5; slot++) att[e * 5 + slot] = WeaverSkill(w, (Element)e, slot);
                _pal = VfxPalette.Arkan; _style = null;
                var set = Set("W" + w, names[w], kinds[w], vis[w], col, att[0], att[1], att[2], att[3], att[4]);
                set.Attunement = att; foreach (var a in att) a.Weapon = kinds[w];
                sets.Add(set);
            }
            _pal = VfxPalette.Arkan; _style = null;

            // ---- Beschwörungen (je 5 Skills, ersetzen 1–5 für 20 s)
            WeaponSet Conjure(string id, string name, WeaponVisual v, VfxPalette pal, string style, string[] skillNames, EffectType eff)
            {
                _pal = pal; _style = style;
                var arr = new SkillDefinition[5];
                for (int i = 0; i < 5; i++)
                {
                    var s = S("CJ_" + id + (i + 1), skillNames[i], skillNames[i].Substring(0, Mathf.Min(6, skillNames[i].Length)), i == 4 ? SkillDelivery.Self : style == "Bow" ? SkillDelivery.Projectile : SkillDelivery.Melee, 0.5f, i == 0 ? 0f : 6f + 3f * i, style == "Bow" ? 26f : 3.2f, 0.7f + 0.25f * i, "Beschworene Waffe.");
                    s.TargetBuffs.Add(B(eff, 1 + i / 2, 4f)); if (i == 4) { s.Radius = 4f; s.Finisher = ComboFinisherType.Blast; }
                    arr[i] = s;
                }
                var set = Set("Conjure_" + id, name, WeaponKind.None, WeaponVisual.Conjured, pal == VfxPalette.Feuer ? new Color(1f, 0.5f, 0.15f) : pal == VfxPalette.Frost ? new Color(0.6f, 0.85f, 1f) : new Color(0.7f, 0.6f, 1f), arr);
                foreach (var s in arr) s.Weapon = WeaponKind.None;
                _style = null; _pal = VfxPalette.Arkan;
                return set;
            }
            var cjFire = Conjure("Glutbeil", "Glutbeil", WeaponVisual.Conjured, VfxPalette.Feuer, "Axe", new[] { "Glutbeilhieb", "Beilwurf", "Feuerspur", "Glutsprung", "Flammenausbruch" }, EffectType.Burning);
            var cjFrost = Conjure("Frostbogen", "Frostbogen", WeaponVisual.Conjured, VfxPalette.Frost, "Bow", new[] { "Frostpfeil", "Eissalve", "Frostfessel", "Kältepfeil", "Eissturm" }, EffectType.Chill);
            var cjStorm = Conjure("Blitzhammer", "Blitzhammer", WeaponVisual.Conjured, VfxPalette.Luft, "Hammer", new[] { "Hammerschlag", "Donnerwurf", "Blitzwelle", "Funkenhieb", "Sturmschlag" }, EffectType.Vulnerability);

            var h1 = Heal(S("H1_GlypheDerErneuerung", "Glyphe der Erneuerung", "Glyphe", SkillDelivery.Self, 0.75f, 25f, 0f, 0f, "Glyphe: Heilung; je Einstimmung Zusatz (Feuer Macht, Wasser Regeneration, Luft Eile, Erde Schutz)."), 5000f, 1f, true);
            h1.Category = UtilityCategory.Glyph;
            var h2 = Heal(S("H2_Quellstroemung", "Quellströmung", "Quelle", SkillDelivery.Self, 0.5f, 20f, 0f, 0f, "Heilt und erzeugt ein Wasserfeld."), 4200f, 0.9f, false);
            h2.Field = ComboFieldType.Water; h2.FieldDuration = 5f; h2.Palette = VfxPalette.Wasser;
            var h3 = Heal(S("H3_Zaubertau", "Zaubertau", "Tau", SkillDelivery.Self, 1f, 30f, 0f, 0f, "Starke Heilung, entfernt 2 Zustände."), 6200f, 1.2f, false);
            h3.CleanseCount = 2; h3.Palette = VfxPalette.Wasser;
            var gl1 = Util(S("G1_GlypheDerKraft", "Glyphe der Kraft", "Kraft", SkillDelivery.Self, 0f, 25f, 0f, 0f, "Glyphe: je Einstimmung Macht, Regeneration, Wut oder Schutz für dich und Verbündete."), UtilityCategory.Glyph, 1, true);
            gl1.ShareWithAllies = true; gl1.Radius = 6f; gl1.SelfBuffs.Add(B(EffectType.Might, 3, 10f));
            var gl2 = Util(S("G2_GlypheDerStaerke", "Glyphe der Stärke", "Stärke", SkillDelivery.Self, 0f, 30f, 0f, 0f, "Glyphe: Stabilität und Entschlossenheit; je Einstimmung Zusatz."), UtilityCategory.Glyph, 2, false);
            gl2.SelfBuffs.Add(B(EffectType.Stability, 2, 5f)); gl2.SelfBuffs.Add(B(EffectType.Resolution, 1, 5f));
            var gl3 = Util(S("G3_GlypheDesSturms", "Glyphe des Sturms", "Sturm", SkillDelivery.GroundTarget, 0.75f, 40f, 20f, 0.4f, "Glyphe: Sturm am Zielort (8 Treffer), Element nach Einstimmung."), UtilityCategory.Glyph, 3, false);
            gl3.Radius = 4.5f; gl3.Hits = 8; gl3.HitInterval = 0.6f;
            var c1 = Util(S("C1_GlutbeilBeschwoeren", "Glutbeil beschwören", "Beil", SkillDelivery.Self, 0.5f, 40f, 0f, 0f, "Beschwörung: Glutbeil ersetzt die Waffenskills für 20 s."), UtilityCategory.Conjure, 1, false);
            c1.Special = SkillSpecial.Conjure; c1.ConjureSet = cjFire; c1.Palette = VfxPalette.Feuer;
            var c2 = Util(S("C2_FrostbogenBeschwoeren", "Frostbogen beschwören", "Bogen", SkillDelivery.Self, 0.5f, 40f, 0f, 0f, "Beschwörung: Frostbogen ersetzt die Waffenskills für 20 s."), UtilityCategory.Conjure, 2, false);
            c2.Special = SkillSpecial.Conjure; c2.ConjureSet = cjFrost; c2.Palette = VfxPalette.Frost;
            var c3 = Util(S("C3_BlitzhammerBeschwoeren", "Blitzhammer beschwören", "Hammer", SkillDelivery.Self, 0.5f, 40f, 0f, 0f, "Beschwörung: Blitzhammer ersetzt die Waffenskills für 20 s."), UtilityCategory.Conjure, 3, false);
            c3.Special = SkillSpecial.Conjure; c3.ConjureSet = cjStorm; c3.Palette = VfxPalette.Luft;
            var a1 = Util(S("A1_Blitzschritt", "Blitzschritt", "Schritt", SkillDelivery.Self, 0f, 25f, 0f, 0.3f, "Arkan: teleportiert dich 12 m nach vorn; Feinde am Ziel werden betäubt."), UtilityCategory.Arcane, 1, false);
            a1.Special = SkillSpecial.Blink; a1.LeapDistance = 12f; a1.Radius = 3f; a1.TargetBuffs.Add(B(EffectType.Stun, 1, 1f)); a1.Palette = VfxPalette.Luft;
            var a2 = Util(S("A2_ArkanerSchild", "Arkaner Schild", "Schild", SkillDelivery.Self, 0f, 40f, 0f, 0f, "Arkan: Aegis und Schutz; weicht 2 s aus."), UtilityCategory.Arcane, 2, false);
            a2.SelfBuffs.Add(B(EffectType.Aegis, 1, 5f)); a2.SelfBuffs.Add(B(EffectType.Protection, 1, 4f)); a2.EvadesDuringCast = true;
            var a3 = Util(S("A3_ArkaneExplosion", "Arkane Explosion", "Explo", SkillDelivery.Self, 0.25f, 20f, 0f, 1.2f, "Arkan: Explosion um dich (Explosions-Finisher, Combo-Ende)."), UtilityCategory.Arcane, 3, false);
            a3.Radius = 4.5f; a3.Finisher = ComboFinisherType.Blast;
            var e1 = Elite(S("E1_Elementarsturm", "Elementarsturm", "ELITE", SkillDelivery.GroundTarget, 1.5f, 90f, 25f, 3.5f, "Elite: alle vier Elemente schlagen am Zielort ein – Brennen, Kälte, Verwundbarkeit, Blutung; Explosions-Finisher."), true);
            e1.Radius = 6f; e1.Finisher = ComboFinisherType.Blast; e1.TargetBuffs.Add(B(EffectType.Burning, 3, 5f)); e1.TargetBuffs.Add(B(EffectType.Chill, 1, 3f)); e1.TargetBuffs.Add(B(EffectType.Vulnerability, 5, 8f)); e1.TargetBuffs.Add(B(EffectType.Bleeding, 3, 8f));
            var e2 = Elite(S("E2_Elementargestalt", "Elementargestalt", "Gestalt", SkillDelivery.Self, 0.5f, 120f, 0f, 0f, "Elite: 15 s Stabilität, Schnelligkeit, Wut und 10 Macht."), false);
            e2.SelfBuffs.Add(B(EffectType.Stability, 5, 15f)); e2.SelfBuffs.Add(B(EffectType.Quickness, 1, 8f)); e2.SelfBuffs.Add(B(EffectType.Fury, 1, 15f)); e2.SelfBuffs.Add(B(EffectType.Might, 10, 15f));

            d.WeaponSets.AddRange(sets);
            d.StartWeapons = new[] { WeaponKind.Staff, WeaponKind.DaggerDagger };
            d.SkillPool.AddRange(new[] { h1, h2, h3, gl1, gl2, gl3, c1, c2, c3, a1, a2, a3, e1, e2 });
            foreach (var set in new[] { cjFire, cjFrost, cjStorm }) d.ExtraSkills.AddRange(set.Skills);
            d.Palette = VfxPalette.Arkan; d.SignatureSkill = "ST05_Sternenglut";
            d.Specializations = WeaverTraits();
            return d;
        }
    }
}
