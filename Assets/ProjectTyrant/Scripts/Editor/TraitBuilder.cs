using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Erzeugt 3 Spezialisierungen × 12 Eigenschaften (3 klein, 9 groß) als Assets.</summary>
    public static class TraitBuilder
    {
        public const string Dir = DataDir + "/Traits";

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        // ---- Spec-Bausteine ----
        static ModifierSpec AttrFlat(Attr a, float v) => new ModifierSpec { Kind = ModifierKind.AttributeFlat, Attr = a, Value = v };
        static ModifierSpec Convert(Attr from, Attr to, float f) => new ModifierSpec { Kind = ModifierKind.AttributeConversion, Attr = to, Attr2 = from, Value = f };
        static ModifierSpec Crit(float v) => new ModifierSpec { Kind = ModifierKind.CritChance, Value = v };
        static ModifierSpec VsEffect(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.DamageVsCondition, Effect = e, Value = v };
        static ModifierSpec VsLow(float threshold, float v) => new ModifierSpec { Kind = ModifierKind.DamageVsLowHealth, Value = v, Value2 = threshold };
        static ModifierSpec WhileBoon(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.DamageWhileBoon, Effect = e, Value = v };
        static ModifierSpec WeaponDmg(WeaponKind w, float v) => new ModifierSpec { Kind = ModifierKind.CategoryDamage, Weapon = w, Value = v };
        static ModifierSpec CatDmg(UtilityCategory c, float v) => new ModifierSpec { Kind = ModifierKind.CategoryDamage, Category = c, Value = v };
        static ModifierSpec WeaponCd(WeaponKind w, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, Weapon = w, Value = v };
        static ModifierSpec CatCd(UtilityCategory c, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, Category = c, Value = v };
        static ModifierSpec SlotCd(SkillSlotType t, float v) => new ModifierSpec { Kind = ModifierKind.CategoryRecharge, MatchSlotType = true, SlotType = t, Value = v };
        static ModifierSpec Dur(EffectType e, float v) => new ModifierSpec { Kind = ModifierKind.EffectDuration, Effect = e, Value = v };
        static ModifierSpec DurAll(DurationScope sc, float v) => new ModifierSpec { Kind = ModifierKind.EffectDuration, Scope = sc, Value = v };
        static ModifierSpec On(TriggerType t, ModifierTarget who, EffectType e, int stacks, float dur, float icd)
            => new ModifierSpec { Kind = ModifierKind.TriggerApplyEffect, Trigger = t, Target = who, Effect = e, Stacks = stacks, Duration = dur, InternalCooldown = icd };
        static ModifierSpec OnHeal(TriggerType t, float baseAt80, float coeff, float icd)
            => new ModifierSpec { Kind = ModifierKind.TriggerHeal, Trigger = t, Value = baseAt80, Value2 = coeff, InternalCooldown = icd };

        static TraitDefinition T(string spec, string id, string name, string glyph, int tier, bool major, string desc, params ModifierSpec[] mods)
        {
            var t = Asset<TraitDefinition>(Dir + "/Trait_" + spec + "_" + id + ".asset");
            t.Id = spec + "_" + id; t.DisplayName = name; t.Glyph = glyph; t.Tier = tier; t.Major = major; t.Description = desc;
            t.Modifiers = new List<ModifierSpec>(mods);
            EditorUtility.SetDirty(t);
            return t;
        }

        static SpecializationDefinition Spec(string id, string name, Color c, string desc, TraitDefinition[] minors, TraitDefinition[] majors)
        {
            var s = Asset<SpecializationDefinition>(Dir + "/Spec_" + id + ".asset");
            s.Id = id; s.DisplayName = name; s.Color = c; s.Description = desc; s.Minors = minors; s.Majors = majors;
            EditorUtility.SetDirty(s);
            return s;
        }

        [MenuItem("Project Tyrant/Build Traits")]
        public static SpecializationDefinition[] BuildAll()
        {
            EnsureFolder(Dir);
            const string G = "glut", S = "sturm", W = "wacht";
            var glut = Spec(G, "Glutzorn", new Color(0.95f, 0.45f, 0.2f), "Kraft, kritische Treffer und Großschwert.",
                new[] {
                    T(G, "m1", "Glühende Kraft", "K", 1, false, "+100 Kraft.", AttrFlat(Attr.Power, 100)),
                    T(G, "m2", "Schwelende Wunden", "B", 2, false, "Brennen hält 20 % länger.", Dur(EffectType.Burning, 0.2f)),
                    T(G, "m3", "Grausamkeit", "W", 3, false, "+120 Wildheit.", AttrFlat(Attr.Ferocity, 120)) },
                new[] {
                    T(G, "a1", "Scharfe Schneide", "S", 1, true, "+10 % Schaden gegen verwundbare Ziele.", VsEffect(EffectType.Vulnerability, 0.10f)),
                    T(G, "a2", "Großschwert-Meisterschaft", "G", 1, true, "Großschwert-Fertigkeiten laden 20 % schneller auf; +5 % Großschwert-Schaden.", WeaponCd(WeaponKind.Greatsword, 0.20f), WeaponDmg(WeaponKind.Greatsword, 0.05f)),
                    T(G, "a3", "Funkenschlag", "F", 1, true, "Kritische Treffer setzen das Ziel 2 s in Brand (Abklingzeit 3 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Burning, 1, 2f, 3f)),
                    T(G, "b1", "Blutrausch", "R", 2, true, "+10 % Schaden, solange du Wut hast.", WhileBoon(EffectType.Fury, 0.10f)),
                    T(G, "b2", "Zornesmacht", "M", 2, true, "Kritische Treffer gewähren 1 Macht für 8 s (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Self, EffectType.Might, 1, 8f, 1f)),
                    T(G, "b3", "Aschenhaut", "A", 2, true, "+150 Zähigkeit; Schaden erleiden gewährt 3 s Schutz (Abklingzeit 20 s).", AttrFlat(Attr.Toughness, 150), On(TriggerType.DamageTaken, ModifierTarget.Self, EffectType.Protection, 1, 3f, 20f)),
                    T(G, "c1", "Henkersstoß", "H", 3, true, "+15 % Schaden gegen Ziele unter 50 % Lebenspunkten.", VsLow(0.5f, 0.15f)),
                    T(G, "c2", "Glutentfesselung", "E", 3, true, "Waffenwechsel: 6 s Wut und 3 Macht (Abklingzeit 9 s).", On(TriggerType.WeaponSwap, ModifierTarget.Self, EffectType.Fury, 1, 6f, 9f), On(TriggerType.WeaponSwap, ModifierTarget.Self, EffectType.Might, 3, 8f, 9f)),
                    T(G, "c3", "Feuersturm", "Ö", 3, true, "Brennen hält 33 % länger; Elitefertigkeit lädt 20 % schneller auf.", Dur(EffectType.Burning, 0.33f), SlotCd(SkillSlotType.Elite, 0.20f)) });

            var sturm = Spec(S, "Sturmweisheit", new Color(0.55f, 0.5f, 1f), "Zustände, Kontrolle, Stab und Fallen.",
                new[] {
                    T(S, "m1", "Funkenblut", "Z", 1, false, "+100 Zustandsschaden.", AttrFlat(Attr.ConditionDamage, 100)),
                    T(S, "m2", "Eisige Hände", "K", 2, false, "Kälte hält 25 % länger.", Dur(EffectType.Chill, 0.25f)),
                    T(S, "m3", "Gelehrsamkeit", "S", 3, false, "+120 Sachkenntnis.", AttrFlat(Attr.Expertise, 120)) },
                new[] {
                    T(S, "a1", "Stabmeisterschaft", "St", 1, true, "Stab-Fertigkeiten laden 20 % schneller auf; +5 % Stab-Schaden.", WeaponCd(WeaponKind.Staff, 0.20f), WeaponDmg(WeaponKind.Staff, 0.05f)),
                    T(S, "a2", "Splitterfunken", "B", 1, true, "Kritische Treffer verursachen 1 Blutung für 6 s (Abklingzeit 1 s).", On(TriggerType.Crit, ModifierTarget.Target, EffectType.Bleeding, 1, 6f, 1f)),
                    T(S, "a3", "Frostbiss", "F", 1, true, "+10 % Schaden gegen unterkühlte Ziele.", VsEffect(EffectType.Chill, 0.10f)),
                    T(S, "b1", "Fallensteller", "Fa", 2, true, "Fallen laden 20 % schneller auf und verursachen 10 % mehr Schaden.", CatCd(UtilityCategory.Trap, 0.20f), CatDmg(UtilityCategory.Trap, 0.10f)),
                    T(S, "b2", "Zersetzung", "Zs", 2, true, "Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision.", Convert(Attr.Precision, Attr.ConditionDamage, 0.10f)),
                    T(S, "b3", "Kältewelle", "Kw", 2, true, "Ausweichen lässt dein Ziel 2 s unterkühlen (Abklingzeit 5 s).", On(TriggerType.Dodge, ModifierTarget.Target, EffectType.Chill, 1, 2f, 5f)),
                    T(S, "c1", "Gewittergeist", "G", 3, true, "Besiegte Gegner gewähren 3 s Schnelligkeit (Abklingzeit 10 s).", On(TriggerType.Kill, ModifierTarget.Self, EffectType.Quickness, 1, 3f, 10f)),
                    T(S, "c2", "Siechtum", "Si", 3, true, "Alle Zustände halten 15 % länger; +5 % Schaden gegen blutende Ziele.", DurAll(DurationScope.AllConditions, 0.15f), VsEffect(EffectType.Bleeding, 0.05f)),
                    T(S, "c3", "Sturmauge", "Au", 3, true, "+150 Präzision und +5 % kritische Trefferchance.", AttrFlat(Attr.Precision, 150), Crit(0.05f)) });

            var wacht = Spec(W, "Wachtbund", new Color(0.95f, 0.8f, 0.35f), "Verteidigung, Segen, Rufe und Meditationen.",
                new[] {
                    T(W, "m1", "Eiserner Wille", "V", 1, false, "+100 Vitalität.", AttrFlat(Attr.Vitality, 100)),
                    T(W, "m2", "Segenshand", "K", 2, false, "+120 Konzentration.", AttrFlat(Attr.Concentration, 120)),
                    T(W, "m3", "Heilende Gabe", "H", 3, false, "+120 Heilkraft.", AttrFlat(Attr.HealingPower, 120)) },
                new[] {
                    T(W, "a1", "Lautstarke Wacht", "R", 1, true, "Rufe laden 20 % schneller auf.", CatCd(UtilityCategory.Shout, 0.20f)),
                    T(W, "a2", "Stille Disziplin", "M", 1, true, "Meditationen laden 20 % schneller auf.", CatCd(UtilityCategory.Meditation, 0.20f)),
                    T(W, "a3", "Heiliger Schild", "Ae", 1, true, "Heilfertigkeiten gewähren Aegis (Abklingzeit 15 s).", On(TriggerType.HealSkill, ModifierTarget.Self, EffectType.Aegis, 1, 5f, 15f)),
                    T(W, "b1", "Ausdauernde Segen", "S", 2, true, "Alle Segen halten 20 % länger.", DurAll(DurationScope.AllBoons, 0.20f)),
                    T(W, "b2", "Zweiter Atem", "A", 2, true, "Ausweichen heilt dich (Abklingzeit 3 s).", OnHeal(TriggerType.Dodge, 1200f, 0.25f, 3f)),
                    T(W, "b3", "Bollwerk", "B", 2, true, "Erhalte Kraft in Höhe von 8 % deiner Zähigkeit; Schaden erleiden gewährt 3 s Stabilität (Abklingzeit 25 s).", Convert(Attr.Toughness, Attr.Power, 0.08f), On(TriggerType.DamageTaken, ModifierTarget.Self, EffectType.Stability, 1, 3f, 25f)),
                    T(W, "c1", "Ewige Wacht", "E", 3, true, "Heilfertigkeiten laden 33 % schneller auf.", SlotCd(SkillSlotType.Heal, 0.33f)),
                    T(W, "c2", "Siegesmut", "Sg", 3, true, "Besiegte Gegner heilen dich (Abklingzeit 2 s); +7 % Schaden mit Schutz.", OnHeal(TriggerType.Kill, 1500f, 0.4f, 2f), WhileBoon(EffectType.Protection, 0.07f)),
                    T(W, "c3", "Unerschütterlich", "U", 3, true, "Macht hält 25 % länger; +7 % Schaden mit Stabilität.", Dur(EffectType.Might, 0.25f), WhileBoon(EffectType.Stability, 0.07f)) });

            AssetDatabase.SaveAssets();
            return new[] { glut, sturm, wacht };
        }

        public static SpecializationDefinition[] Load()
        {
            var list = new List<SpecializationDefinition>();
            foreach (var id in new[] { "glut", "sturm", "wacht" })
            {
                var s = AssetDatabase.LoadAssetAtPath<SpecializationDefinition>(Dir + "/Spec_" + id + ".asset");
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }
    }
}
