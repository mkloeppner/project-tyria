using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        static ModifierSpec AttrFlat(Attr a, float v) => new ModifierSpec { Kind = ModifierKind.AttributeFlat, Attr = a, Value = v };
        static ModifierSpec Conv(Attr from, Attr to, float f) => new ModifierSpec { Kind = ModifierKind.AttributeConversion, Attr = to, Attr2 = from, Value = f };
        static ModifierSpec Crit(float v) => new ModifierSpec { Kind = ModifierKind.CritChance, Value = v };
        static ModifierSpec VsEff(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.DamageVsCondition, Effect = e, Value = v };
        static ModifierSpec VsLow(float th, float v) => new ModifierSpec { Kind = ModifierKind.DamageVsLowHealth, Value = v, Value2 = th };
        static ModifierSpec WhileB(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.DamageWhileBoon, Effect = e, Value = v };
        static ModifierSpec WDmg(WeaponKind w, float v) => new ModifierSpec { Kind = ModifierKind.CategoryDamage, Weapon = w, Value = v };
        static ModifierSpec CDmg(UtilityCategory c, float v) => new ModifierSpec { Kind = ModifierKind.CategoryDamage, Category = c, Value = v };
        static ModifierSpec WCd(WeaponKind w, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, Weapon = w, Value = v };
        static ModifierSpec CCd(UtilityCategory c, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, Category = c, Value = v };
        static ModifierSpec SCd(SkillSlotType t, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, MatchSlotType = true, SlotType = t, Value = v };
        static ModifierSpec Dur(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.EffectDuration, Effect = e, Value = v };
        static ModifierSpec DurAll(DurationScope sc, float v) => new ModifierSpec { Kind = ModifierKind.EffectDuration, Scope = sc, Value = v };
        static ModifierSpec On(TriggerType t, ModifierTarget who, EffectType e, int n, float dur, float icd) => new ModifierSpec { Kind = ModifierKind.TriggerApplyEffect, Trigger = t, Target = who, Effect = e, Stacks = n, Duration = dur, InternalCooldown = icd };
        static ModifierSpec OnHeal(TriggerType t, float b, float c, float icd) => new ModifierSpec { Kind = ModifierKind.TriggerHeal, Trigger = t, Value = b, Value2 = c, InternalCooldown = icd };

        struct Tr { public string Id, Name, Glyph, Desc; public ModifierSpec[] Mods; }
        static Tr R(string id, string name, string glyph, string desc, params ModifierSpec[] mods) => new Tr { Id = id, Name = name, Glyph = glyph, Desc = desc, Mods = mods };

        static SpecializationDefinition Line(string id, string name, Color c, string desc, Tr[] minors, Tr[] majors)
        {
            string dir = Root + "/" + _cls + "/Traits";
            TraitDefinition Make(Tr t, int tier, bool major)
            {
                var a = Asset<TraitDefinition>(dir + "/Trait_" + id + "_" + t.Id + ".asset");
                a.Id = id + "_" + t.Id; a.DisplayName = t.Name; a.Glyph = t.Glyph; a.Tier = tier; a.Major = major; a.Description = t.Desc; a.Modifiers = new List<ModifierSpec>(t.Mods);
                return a;
            }
            var s = Asset<SpecializationDefinition>(dir + "/Spec_" + id + ".asset");
            s.Id = id; s.DisplayName = name; s.Color = c; s.Description = desc;
            s.Minors = new TraitDefinition[3]; s.Majors = new TraitDefinition[9];
            for (int i = 0; i < 3; i++) s.Minors[i] = Make(minors[i], i + 1, false);
            for (int i = 0; i < 9; i++) s.Majors[i] = Make(majors[i], i / 3 + 1, true);
            return s;
        }

        static List<SpecializationDefinition> RangerTraits()
        {
            var jagd = Line("jagd", "Jagdinstinkt", new Color(0.8f, 0.7f, 0.3f), "Kraft, Präzision und Langbogen.",
                new[] { R("m1", "Scharfes Auge", "A", "+100 Präzision.", AttrFlat(Attr.Precision, 100)), R("m2", "Jägerkraft", "K", "+120 Kraft.", AttrFlat(Attr.Power, 120)), R("m3", "Tödliche Spur", "W", "+120 Wildheit.", AttrFlat(Attr.Ferocity, 120)) },
                new[] {
                    R("a1", "Langbogen-Meisterschaft", "L", "Langbogen-Fertigkeiten laden 20 % schneller auf; +5 % Langbogen-Schaden.", WCd(WeaponKind.Longbow, 0.2f), WDmg(WeaponKind.Longbow, 0.05f)),
                    R("a2", "Hetzjagd", "H", "+10 % Schaden gegen verkrüppelte Ziele.", VsEff(EffectType.Cripple, 0.1f)),
                    R("a3", "Schnelle Pfeile", "S", "Kritische Treffer gewähren 2 s Eile (Abklingzeit 5 s).", On(TriggerType.Crit, ModifierTarget.Self, EffectType.Swiftness, 1, 2f, 5f)),
                    R("b1", "Raubtierblick", "R", "+5 % kritische Trefferchance; +7 % Schaden mit Wut.", Crit(0.05f), WhileB(EffectType.Fury, 0.07f)),
                    R("b2", "Axtwerfer", "X", "Axt-Fertigkeiten laden 20 % schneller auf; +5 % Axt-Schaden.", WCd(WeaponKind.AxeTorch, 0.2f), WDmg(WeaponKind.AxeTorch, 0.05f)),
                    R("b3", "Zweiter Pfeil", "Z", "Kritische Treffer verursachen 1 Blutung (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Bleeding, 1, 6f, 1f)),
                    R("c1", "Fangschuss", "F", "+15 % Schaden gegen Ziele unter 50 % Lebenspunkten.", VsLow(0.5f, 0.15f)),
                    R("c2", "Jagdfieber", "J", "Besiegte Gegner gewähren 10 s Wut und 5 Macht (Abklingzeit 10 s).", On(TriggerType.Kill, ModifierTarget.Self, EffectType.Fury, 1, 10f, 10f), On(TriggerType.Kill, ModifierTarget.Self, EffectType.Might, 5, 10f, 10f)),
                    R("c3", "Klingenmeister", "K", "Schwert-Fertigkeiten laden 20 % schneller auf; +7 % Schwert-Schaden.", WCd(WeaponKind.SwordWarhorn, 0.2f), WDmg(WeaponKind.SwordWarhorn, 0.07f)) });
            var wild = Line("wildnis", "Wildniskunde", new Color(0.45f, 0.7f, 0.35f), "Fallen, Wildnisfertigkeiten und Zustände.",
                new[] { R("m1", "Dornenblut", "Z", "+100 Zustandsschaden.", AttrFlat(Attr.ConditionDamage, 100)), R("m2", "Giftkunde", "G", "Blutung hält 20 % länger.", Dur(EffectType.Bleeding, 0.2f)), R("m3", "Wildnisgespür", "S", "+120 Sachkenntnis.", AttrFlat(Attr.Expertise, 120)) },
                new[] {
                    R("a1", "Fallensteller", "F", "Fallen laden 20 % schneller auf, +10 % Fallen-Schaden.", CCd(UtilityCategory.Trap, 0.2f), CDmg(UtilityCategory.Trap, 0.1f)),
                    R("a2", "Kräuterkunde", "K", "Wildnisfertigkeiten laden 20 % schneller auf.", CCd(UtilityCategory.Wilderness, 0.2f)),
                    R("a3", "Brennende Pfeile", "B", "Kritische Treffer setzen 2 s in Brand (Abklingzeit 4 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Burning, 1, 2f, 4f)),
                    R("b1", "Schlangenbiss", "S", "+10 % Schaden gegen vergiftete Ziele.", VsEff(EffectType.Poison, 0.1f)),
                    R("b2", "Ausweichrolle", "A", "Ausweichen verkrüppelt dein Ziel (Abklingzeit 5 s).", On(TriggerType.Dodge, ModifierTarget.Target, EffectType.Cripple, 1, 2f, 5f)),
                    R("b3", "Zersetzende Dornen", "D", "Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision.", Conv(Attr.Precision, Attr.ConditionDamage, 0.1f)),
                    R("c1", "Wildwuchs", "W", "Alle Zustände halten 15 % länger.", DurAll(DurationScope.AllConditions, 0.15f)),
                    R("c2", "Tarnung des Jägers", "T", "Aus der Tarnung: 3 s Eile und 3 Macht (Abklingzeit 10 s).", On(TriggerType.Stealth, ModifierTarget.Self, EffectType.Might, 3, 8f, 10f), On(TriggerType.Stealth, ModifierTarget.Self, EffectType.Swiftness, 1, 3f, 10f)),
                    R("c3", "Feuerhorn", "H", "Fackel und Axt: +7 % Schaden; Brennen hält 25 % länger.", WDmg(WeaponKind.AxeTorch, 0.07f), Dur(EffectType.Burning, 0.25f)) });
            var bund = Line("tierbund", "Tierbund", new Color(0.6f, 0.5f, 0.3f), "Gefährte, Signale, Heilung und Segen.",
                new[] { R("m1", "Zäher Pelz", "V", "+100 Vitalität.", AttrFlat(Attr.Vitality, 100)), R("m2", "Heilende Natur", "H", "+120 Heilkraft.", AttrFlat(Attr.HealingPower, 120)), R("m3", "Rudelführer", "K", "+120 Konzentration.", AttrFlat(Attr.Concentration, 120)) },
                new[] {
                    R("a1", "Lauter Ruf", "R", "Signale laden 20 % schneller auf.", CCd(UtilityCategory.Signal, 0.2f)),
                    R("a2", "Gefährtenwechsel", "G", "Gefährten wechseln gewährt 5 Macht und Wut (Abklingzeit 10 s).", On(TriggerType.PetSwap, ModifierTarget.Self, EffectType.Might, 5, 10f, 10f), On(TriggerType.PetSwap, ModifierTarget.Self, EffectType.Fury, 1, 5f, 10f)),
                    R("a3", "Quellkraft", "Q", "Heilfertigkeiten gewähren Regeneration und Schutz (Abklingzeit 15 s).", On(TriggerType.HealSkill, ModifierTarget.Self, EffectType.Protection, 1, 3f, 15f)),
                    R("b1", "Ausdauernde Segen", "S", "Alle Segen halten 20 % länger.", DurAll(DurationScope.AllBoons, 0.2f)),
                    R("b2", "Wildes Herz", "W", "Schaden erleiden gewährt 3 s Stabilität (Abklingzeit 25 s).", On(TriggerType.DamageTaken, ModifierTarget.Self, EffectType.Stability, 1, 3f, 25f)),
                    R("b3", "Rudeltaktik", "T", "+7 % Schaden mit Macht; Macht hält 20 % länger.", WhileB(EffectType.Might, 0.07f), Dur(EffectType.Might, 0.2f)),
                    R("c1", "Ewiger Frühling", "E", "Heilfertigkeiten laden 33 % schneller auf.", SCd(SkillSlotType.Heal, 0.33f)),
                    R("c2", "Naturverbunden", "N", "Ausweichen heilt dich (Abklingzeit 3 s).", OnHeal(TriggerType.Dodge, 1200f, 0.25f, 3f)),
                    R("c3", "Urkraft", "U", "Elitefertigkeit lädt 20 % schneller auf; besiegte Gegner heilen dich (Abklingzeit 2 s).", SCd(SkillSlotType.Elite, 0.2f), OnHeal(TriggerType.Kill, 1500f, 0.4f, 2f)) });
            return new List<SpecializationDefinition> { jagd, wild, bund };
        }

        static List<SpecializationDefinition> WeaverTraits()
        {
            var glut = Line("glutlehre", "Glutlehre", new Color(1f, 0.45f, 0.15f), "Feuer, Brennen und Kraft.",
                new[] { R("m1", "Brennende Kraft", "K", "+100 Kraft.", AttrFlat(Attr.Power, 100)), R("m2", "Glimmende Spur", "B", "Brennen hält 20 % länger.", Dur(EffectType.Burning, 0.2f)), R("m3", "Hitzeschild", "Z", "+120 Zustandsschaden.", AttrFlat(Attr.ConditionDamage, 120)) },
                new[] {
                    R("a1", "Flammenwechsel", "F", "Einstimmung wechseln gewährt 3 Macht (Abklingzeit 3 s).", On(TriggerType.AttunementSwap, ModifierTarget.Self, EffectType.Might, 3, 8f, 3f)),
                    R("a2", "Stabkunde", "S", "Stab-Fertigkeiten laden 20 % schneller auf; +5 % Stab-Schaden.", WCd(WeaponKind.Staff, 0.2f), WDmg(WeaponKind.Staff, 0.05f)),
                    R("a3", "Funkenschlag", "Fu", "Kritische Treffer setzen 2 s in Brand (Abklingzeit 3 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Burning, 1, 2f, 3f)),
                    R("b1", "Glühender Zorn", "G", "+10 % Schaden gegen brennende Ziele.", VsEff(EffectType.Burning, 0.1f)),
                    R("b2", "Beschwörerglut", "B", "Beschwörungen laden 20 % schneller auf; +10 % Beschwörungs-Schaden.", CCd(UtilityCategory.Conjure, 0.2f), CDmg(UtilityCategory.Conjure, 0.1f)),
                    R("b3", "Feuerseele", "S", "+7 % Schaden mit Macht.", WhileB(EffectType.Might, 0.07f)),
                    R("c1", "Aschenwind", "A", "Brennen hält 33 % länger.", Dur(EffectType.Burning, 0.33f)),
                    R("c2", "Flammenherz", "H", "Erhalte Kraft in Höhe von 8 % deines Zustandsschadens.", Conv(Attr.ConditionDamage, Attr.Power, 0.08f)),
                    R("c3", "Sternenfall", "St", "Elitefertigkeit lädt 20 % schneller auf; +10 % Schaden gegen Ziele unter 50 %.", SCd(SkillSlotType.Elite, 0.2f), VsLow(0.5f, 0.1f)) });
            var sturm = Line("sturmlehre", "Sturmlehre", new Color(0.75f, 0.7f, 1f), "Luft, kritische Treffer und Tempo.",
                new[] { R("m1", "Scharfer Wind", "A", "+100 Präzision.", AttrFlat(Attr.Precision, 100)), R("m2", "Blitzschnell", "E", "Eile hält 25 % länger.", Dur(EffectType.Swiftness, 0.25f)), R("m3", "Donnergewalt", "W", "+120 Wildheit.", AttrFlat(Attr.Ferocity, 120)) },
                new[] {
                    R("a1", "Sturmwechsel", "S", "Einstimmung wechseln gewährt 4 s Wut (Abklingzeit 5 s).", On(TriggerType.AttunementSwap, ModifierTarget.Self, EffectType.Fury, 1, 4f, 5f)),
                    R("a2", "Dolchtanz", "D", "Dolch-Fertigkeiten laden 20 % schneller auf; +5 % Dolch-Schaden.", WCd(WeaponKind.DaggerDagger, 0.2f), WDmg(WeaponKind.DaggerDagger, 0.05f)),
                    R("a3", "Statische Ladung", "L", "+10 % Schaden gegen verwundbare Ziele.", VsEff(EffectType.Vulnerability, 0.1f)),
                    R("b1", "Arkane Schnelle", "Ar", "Arkan-Fertigkeiten laden 20 % schneller auf.", CCd(UtilityCategory.Arcane, 0.2f)),
                    R("b2", "Windgeschwind", "W", "Ausweichen gewährt 2 s Schnelligkeit (Abklingzeit 10 s).", On(TriggerType.Dodge, ModifierTarget.Self, EffectType.Quickness, 1, 2f, 10f)),
                    R("b3", "Gewitterauge", "G", "+5 % kritische Trefferchance; +7 % Schaden mit Wut.", Crit(0.05f), WhileB(EffectType.Fury, 0.07f)),
                    R("c1", "Blitzableiter", "B", "Kritische Treffer: 1 Verwundbarkeit (Abklingzeit 0,5 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Vulnerability, 1, 6f, 0.5f)),
                    R("c2", "Zepterfokus", "Z", "Zepter und Fokus: Abklingzeit −20 %, +7 % Schaden.", WCd(WeaponKind.ScepterFocus, 0.2f), WDmg(WeaponKind.ScepterFocus, 0.07f)),
                    R("c3", "Sturmbringer", "St", "+15 % Schaden gegen Ziele unter 50 % Lebenspunkten.", VsLow(0.5f, 0.15f)) });
            var stein = Line("steinlehre", "Stein- und Quelllehre", new Color(0.55f, 0.7f, 0.85f), "Erde, Wasser, Schutz und Heilung.",
                new[] { R("m1", "Steinhaut", "Z", "+100 Zähigkeit.", AttrFlat(Attr.Toughness, 100)), R("m2", "Quellwasser", "H", "+120 Heilkraft.", AttrFlat(Attr.HealingPower, 120)), R("m3", "Erdverbunden", "V", "+120 Vitalität.", AttrFlat(Attr.Vitality, 120)) },
                new[] {
                    R("a1", "Glyphenkunde", "G", "Glyphen laden 20 % schneller auf.", CCd(UtilityCategory.Glyph, 0.2f)),
                    R("a2", "Gezeitenwechsel", "W", "Einstimmung wechseln heilt dich (Abklingzeit 5 s).", OnHeal(TriggerType.AttunementSwap, 900f, 0.2f, 5f)),
                    R("a3", "Felsenpanzer", "F", "Schaden erleiden gewährt 3 s Schutz (Abklingzeit 20 s).", On(TriggerType.DamageTaken, ModifierTarget.Self, EffectType.Protection, 1, 3f, 20f)),
                    R("b1", "Ausdauernde Segen", "S", "Alle Segen halten 20 % länger.", DurAll(DurationScope.AllBoons, 0.2f)),
                    R("b2", "Erdblut", "E", "Kritische Treffer: 1 Blutung (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Bleeding, 1, 6f, 1f)),
                    R("b3", "Heilende Ströme", "Q", "Heilfertigkeiten laden 33 % schneller auf.", SCd(SkillSlotType.Heal, 0.33f)),
                    R("c1", "Bergesruhe", "B", "Erhalte Kraft in Höhe von 8 % deiner Zähigkeit.", Conv(Attr.Toughness, Attr.Power, 0.08f)),
                    R("c2", "Lebensquell", "L", "Ausweichen heilt dich (Abklingzeit 3 s).", OnHeal(TriggerType.Dodge, 1200f, 0.25f, 3f)),
                    R("c3", "Unbeugsam", "U", "+7 % Schaden mit Schutz; Schutz hält 25 % länger.", WhileB(EffectType.Protection, 0.07f), Dur(EffectType.Protection, 0.25f)) });
            return new List<SpecializationDefinition> { glut, sturm, stein };
        }

        static List<SpecializationDefinition> ThiefTraits()
        {
            var gift = Line("giftkunst", "Tödliche Künste", new Color(0.5f, 0.75f, 0.3f), "Gift, Zustände und Gifte.",
                new[] { R("m1", "Giftblut", "Z", "+100 Zustandsschaden.", AttrFlat(Attr.ConditionDamage, 100)), R("m2", "Schleichendes Gift", "G", "Gift hält 25 % länger.", Dur(EffectType.Poison, 0.25f)), R("m3", "Giftmischer", "S", "+120 Sachkenntnis.", AttrFlat(Attr.Expertise, 120)) },
                new[] {
                    R("a1", "Giftmeister", "M", "Gifte laden 20 % schneller auf; +10 % Gift-Schaden.", CCd(UtilityCategory.Venom, 0.2f), CDmg(UtilityCategory.Venom, 0.1f)),
                    R("a2", "Blutende Klingen", "B", "Kritische Treffer: 1 Blutung (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Bleeding, 1, 6f, 1f)),
                    R("a3", "Vergiftete Spitzen", "V", "+10 % Schaden gegen vergiftete Ziele.", VsEff(EffectType.Poison, 0.1f)),
                    R("b1", "Zersetzung", "Z", "Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision.", Conv(Attr.Precision, Attr.ConditionDamage, 0.1f)),
                    R("b2", "Hinterhältig", "H", "Aus der Tarnung: 3 Gift auf das nächste Ziel (Abklingzeit 8 s).", On(TriggerType.Stealth, ModifierTarget.Target, EffectType.Poison, 3, 6f, 8f)),
                    R("b3", "Qualvoll", "Q", "Qual hält 33 % länger.", Dur(EffectType.Torment, 0.33f)),
                    R("c1", "Siechtum", "S", "Alle Zustände halten 15 % länger.", DurAll(DurationScope.AllConditions, 0.15f)),
                    R("c2", "Kurzbogen-Kunst", "K", "Kurzbogen-Fertigkeiten: +7 % Schaden.", WDmg(WeaponKind.Shortbow, 0.07f)),
                    R("c3", "Gnadenstoß", "G", "+15 % Schaden gegen Ziele unter 50 % Lebenspunkten.", VsLow(0.5f, 0.15f)) });
            var klinge = Line("klingenkunst", "Klingenkunst", new Color(0.75f, 0.75f, 0.8f), "Kraft, kritische Treffer und Dolche.",
                new[] { R("m1", "Scharfe Klinge", "K", "+100 Kraft.", AttrFlat(Attr.Power, 100)), R("m2", "Präzise Hand", "A", "+120 Präzision.", AttrFlat(Attr.Precision, 120)), R("m3", "Mordlust", "W", "+120 Wildheit.", AttrFlat(Attr.Ferocity, 120)) },
                new[] {
                    R("a1", "Dolchmeister", "D", "Dolch-Fertigkeiten: +7 % Schaden.", WDmg(WeaponKind.DaggerDagger, 0.07f)),
                    R("a2", "Verletzlichkeit", "V", "+10 % Schaden gegen verwundbare Ziele.", VsEff(EffectType.Vulnerability, 0.1f)),
                    R("a3", "Schwachstelle", "S", "+5 % kritische Trefferchance.", Crit(0.05f)),
                    R("b1", "Säbeltanz", "T", "Schwert und Pistole: +7 % Schaden.", WDmg(WeaponKind.SwordPistol, 0.07f)),
                    R("b2", "Kaltblütig", "B", "Kritische Treffer gewähren 1 Macht (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Self, EffectType.Might, 1, 8f, 1f)),
                    R("b3", "Hinterhalt", "H", "+10 % Schaden mit Wut.", WhileB(EffectType.Fury, 0.1f)),
                    R("c1", "Henkersmeister", "He", "+15 % Schaden gegen Ziele unter 50 % Lebenspunkten.", VsLow(0.5f, 0.15f)),
                    R("c2", "Tricktaschen", "Tr", "Tricks laden 20 % schneller auf.", CCd(UtilityCategory.Trick, 0.2f)),
                    R("c3", "Tödlicher Schwung", "Ts", "Besiegte Gegner gewähren 3 s Schnelligkeit (Abklingzeit 10 s).", On(TriggerType.Kill, ModifierTarget.Self, EffectType.Quickness, 1, 3f, 10f)) });
            var schatten = Line("schattenkunst", "Schattenkunst", new Color(0.45f, 0.35f, 0.6f), "Tarnung, Heilung und Täuschungen.",
                new[] { R("m1", "Schattenhaut", "V", "+100 Vitalität.", AttrFlat(Attr.Vitality, 100)), R("m2", "Schattenheilung", "H", "+120 Heilkraft.", AttrFlat(Attr.HealingPower, 120)), R("m3", "Leises Wesen", "Z", "+120 Zähigkeit.", AttrFlat(Attr.Toughness, 120)) },
                new[] {
                    R("a1", "Schattenzehrer", "S", "Tarnung betreten heilt dich (Abklingzeit 3 s).", OnHeal(TriggerType.Stealth, 1500f, 0.3f, 3f)),
                    R("a2", "Täuschungskunst", "T", "Täuschungen laden 20 % schneller auf.", CCd(UtilityCategory.Deception, 0.2f)),
                    R("a3", "Zweiter Atem", "A", "Ausweichen heilt dich (Abklingzeit 3 s).", OnHeal(TriggerType.Dodge, 1200f, 0.25f, 3f)),
                    R("b1", "Schattenschleier", "Sc", "Tarnung betreten gewährt 3 s Schutz (Abklingzeit 10 s).", On(TriggerType.Stealth, ModifierTarget.Self, EffectType.Protection, 1, 3f, 10f)),
                    R("b2", "Fluchtweg", "F", "Schaden erleiden gewährt 3 s Eile (Abklingzeit 10 s).", On(TriggerType.DamageTaken, ModifierTarget.Self, EffectType.Swiftness, 1, 3f, 10f)),
                    R("b3", "Reinigende Schatten", "R", "Heilfertigkeiten laden 33 % schneller auf.", SCd(SkillSlotType.Heal, 0.33f)),
                    R("c1", "Schattenrückkehr", "Rk", "Besiegte Gegner heilen dich (Abklingzeit 2 s).", OnHeal(TriggerType.Kill, 1500f, 0.4f, 2f)),
                    R("c2", "Verborgene Klinge", "V", "Aus der Tarnung: 5 s Wut (Abklingzeit 8 s).", On(TriggerType.Stealth, ModifierTarget.Self, EffectType.Fury, 1, 5f, 8f)),
                    R("c3", "Schattenfürst", "Sf", "Elitefertigkeit lädt 20 % schneller auf; alle Segen halten 15 % länger.", SCd(SkillSlotType.Elite, 0.2f), DurAll(DurationScope.AllBoons, 0.15f)) });
            return new List<SpecializationDefinition> { gift, klinge, schatten };
        }
    }
}
