using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Erzeugt/aktualisiert alle ScriptableObject-Daten (Skills, Waffensets, Combos, Gegner) und NPC-Prefabs.</summary>
    public static class DataBuilder
    {
        public const string SkillDir = DataDir + "/Skills";
        const string WeaponDir = DataDir + "/Weapons";
        const string EnemyDir = DataDir + "/Enemies";
        const string ComboDir = DataDir + "/Combos";
        const string NpcPrefabDir = PrefabDir + "/Characters";

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static SkillDefinition Skill(string id, string name, string label, Color color, SkillDelivery delivery, float cast, float cd, float range, float dmg, string desc)
        {
            var s = Asset<SkillDefinition>(SkillDir + "/Skill_" + id + ".asset");
            s.Id = id; s.DisplayName = name; s.ShortLabel = label; s.IconColor = color; s.Delivery = delivery;
            s.CastTime = cast; s.Cooldown = cd; s.Range = range; s.Coefficient = dmg; s.Description = desc;
            s.Hits = 1; s.HitInterval = 0.25f; s.HealBase = 0; s.HealCoefficient = 0; s.LeapDistance = 0; s.ProjectileSpeed = 32f;
            s.Radius = 2.5f; s.Field = ComboFieldType.None; s.Finisher = ComboFinisherType.None; s.FieldTickCoefficient = 0; s.CleanseCount = 0; s.ShareWithAllies = false; s.TargetCount = 5;
            s.FieldDuration = 5f; s.FieldRadius = 4f; s.Special = SkillSpecial.None; s.EvadesDuringCast = false;
            s.TargetBuffs = new List<EffectApplication>(); s.SelfBuffs = new List<EffectApplication>();
            s.SlotType = SkillSlotType.Weapon; s.Category = UtilityCategory.None; s.Tier = 1; s.UnlockCost = 0; s.StartsUnlocked = false; s.Weapon = WeaponKind.None;
            return s;
        }

        static EffectApplication B(EffectType t, int n, float d) => new EffectApplication(t, n, d);

        [MenuItem("Project Tyrant/Build Data Assets")]
        public static void BuildAll()
        {
            EnsureFolder(SkillDir); EnsureFolder(WeaponDir); EnsureFolder(EnemyDir); EnsureFolder(ComboDir); EnsureFolder(NpcPrefabDir);
            BuildVfxLibrary();
            BuildEffects();
            BuildCombos();
            BuildSkills();
            BuildEnemies();
            ItemBuilder.BuildAll();
            TraitBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Daten-Assets aktualisiert.");
        }

        public static VfxLibrary BuildVfxLibrary()
        {
            var lib = Asset<VfxLibrary>(DataDir + "/VfxLibrary.asset");
            lib.GroundMarker = MarkerMat("GroundMarker", new Color(1, 0.3f, 0.1f, 0.6f));
            lib.ProjectileGlow = Mat("ProjectileGlow", Color.white, 0f, Color.white, 0.5f);
            var flash = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_FlashSolid.mat");
            if (flash == null)
            {
                flash = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(flash, MatDir + "/M_FlashSolid.mat");
            }
            flash.SetFloat("_Surface", 1f); flash.SetFloat("_Blend", 0f);
            flash.SetOverrideTag("RenderType", "Transparent");
            flash.renderQueue = 3000;
            flash.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            flash.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            flash.SetInt("_ZWrite", 0);
            flash.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            lib.FlashSolid = flash;
            lib.HitFlash = Mat("HitFlash", Color.white, 0f, new Color(1.5f, 1.5f, 1.5f), 0f);
            AudioSynth.BuildAll(lib);
            EditorUtility.SetDirty(lib);
            return lib;
        }

        const string EffectDir = DataDir + "/Effects";

        /// <summary>Alle 12 Segen, 14 Zustände sowie Tarnung/Betäubung als Effect-Assets.</summary>
        public static EffectDefinition[] BuildEffects()
        {
            EnsureFolder(EffectDir);
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(IconPainter.LibraryPath);
            var list = new List<EffectDefinition>();
            foreach (var info in EffectCatalog.All)
            {
                var d = Asset<EffectDefinition>(EffectDir + "/Effect_" + info.Type + ".asset");
                d.Type = info.Type; d.DisplayName = info.Name; d.Description = info.Desc; d.Category = info.Cat;
                d.Stacking = info.Rule; d.MaxStacks = info.Max; d.TickInterval = info.Tick; d.Implemented = info.Implemented;
                d.Color = info.Color; d.Glyph = info.Glyph; d.MaxDuration = 30f;
                d.Icon = lib != null ? lib.Get(IconLibrary.EffectId(info.Type)) : null;
                EditorUtility.SetDirty(d);
                list.Add(d);
            }
            return list.ToArray();
        }

        public static EffectDefinition[] LoadEffects()
        {
            var list = new List<EffectDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:EffectDefinition", new[] { EffectDir }))
                list.Add(AssetDatabase.LoadAssetAtPath<EffectDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            return list.ToArray();
        }

        public static ComboTable BuildCombos()
        {
            var t = Asset<ComboTable>(ComboDir + "/ComboTable.asset");
            t.Entries = ComboTable.CreateDefaultEntries();
            EditorUtility.SetDirty(t);
            return t;
        }

        public static void BuildSkills()
        {
            Color ember = new Color(0.95f, 0.45f, 0.18f), storm = new Color(0.55f, 0.45f, 0.95f), water = Palette.Water, gold = Palette.AllyGold, grey = new Color(0.55f, 0.55f, 0.6f);

            // ---- Waffenset A: Glutklinge (Zweihänder, Nahkampf) ----
            var gs1 = Skill("GS1_Glutschnitt", "Glutschnitt", "Glut", ember, SkillDelivery.Melee, 0.5f, 0f, 3.2f, 0.8f, "Schneller Hieb. Verursacht 1 Stapel Verwundbarkeit.");
            gs1.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 5f));
            var gs2 = Skill("GS2_Aschewirbel", "Aschewirbel", "Wirbel", ember, SkillDelivery.Melee, 0.2f, 8f, 3f, 0.45f, "Wirbelangriff (4 Treffer), weicht dabei aus. Wirbel-Finisher.");
            gs2.Hits = 4; gs2.HitInterval = 0.22f; gs2.Radius = 3.2f; gs2.Finisher = ComboFinisherType.Whirl; gs2.EvadesDuringCast = true;
            var gs3 = Skill("GS3_Flammensprung", "Flammensprung", "Sprung", ember, SkillDelivery.Leap, 0.2f, 10f, 10f, 1.4f, "Springt zum Ziel, setzt es in Brand. Sprung-Finisher.");
            gs3.LeapDistance = 10f; gs3.Radius = 2.6f; gs3.Finisher = ComboFinisherType.Leap; gs3.TargetBuffs.Add(B(EffectType.Burning, 2, 4f));
            var gs4 = Skill("GS4_Glutboden", "Glutboden", "Feld", ember, SkillDelivery.Self, 0.5f, 15f, 0f, 0.5f, "Entzündet den Boden: Feuerfeld (6 s).");
            gs4.Radius = 4f; gs4.Field = ComboFieldType.Fire; gs4.FieldRadius = 4f; gs4.FieldDuration = 6f;
            var gs5 = Skill("GS5_Bergbrecher", "Bergbrecher", "Stoß", ember, SkillDelivery.Self, 0.75f, 18f, 0f, 2.0f, "Wuchtiger Bodenstoß, 5 Verwundbarkeit. Explosions-Finisher.");
            gs5.Radius = 4.5f; gs5.Finisher = ComboFinisherType.Blast; gs5.TargetBuffs.Add(B(EffectType.Vulnerability, 5, 8f));

            // ---- Waffenset B: Sturmruf (Stab, Fernkampf) ----
            var st1 = Skill("ST1_Funkenbolzen", "Funkenbolzen", "Bolzen", storm, SkillDelivery.Projectile, 0.6f, 0f, 24f, 0.7f, "Magisches Geschoss. Projektil-Finisher.");
            st1.Finisher = ComboFinisherType.Projectile;
            var st2 = Skill("ST2_Kettenblitz", "Kettenblitz", "Kette", storm, SkillDelivery.Projectile, 0.5f, 6f, 24f, 0.9f, "Blitz, 3 Verwundbarkeit. Projektil-Finisher.");
            st2.Finisher = ComboFinisherType.Projectile; st2.ProjectileSpeed = 45f; st2.TargetBuffs.Add(B(EffectType.Vulnerability, 3, 8f));
            var st3 = Skill("ST3_Gewitterfeld", "Gewitterfeld", "Feld", storm, SkillDelivery.GroundTarget, 0.75f, 15f, 22f, 0.4f, "Blitzfeld am Zielort (6 s), Schaden pro Sekunde.");
            st3.Radius = 4f; st3.Field = ComboFieldType.Lightning; st3.FieldRadius = 4f; st3.FieldDuration = 6f; st3.FieldTickCoefficient = 0.25f;
            var st4 = Skill("ST4_Frostlanze", "Frostlanze", "Frost", new Color(0.5f, 0.8f, 1f), SkillDelivery.Projectile, 0.75f, 10f, 24f, 1.0f, "Eisgeschoss, verursacht Kälte.");
            st4.TargetBuffs.Add(B(EffectType.Chill, 1, 3f)); st4.Finisher = ComboFinisherType.Projectile;
            var st5 = Skill("ST5_Donnerschlag", "Donnerschlag", "Donner", storm, SkillDelivery.GroundTarget, 1.0f, 16f, 22f, 1.8f, "Einschlag am Zielort. Explosions-Finisher.");
            st5.Radius = 4.5f; st5.Finisher = ComboFinisherType.Blast; st5.TargetBuffs.Add(B(EffectType.Vulnerability, 3, 6f));

            // ---- Heal / Utility / Elite ----
            var heal = Skill("H6_Quellsegen", "Quellsegen", "Heil", water, SkillDelivery.Self, 0.75f, 25f, 0f, 0f, "Heilt dich stark und erzeugt ein Wasserfeld (5 s).");
            heal.HealBase = 5000f; heal.HealCoefficient = 1.0f; heal.Field = ComboFieldType.Water; heal.FieldRadius = 4f; heal.FieldDuration = 5f; heal.SelfBuffs.Add(B(EffectType.Regeneration, 1, 6f));
            var u7 = Skill("U7_Kriegsschrei", "Kriegsschrei", "Schrei", gold, SkillDelivery.Self, 0.25f, 25f, 0f, 0f, "6 Macht (12 s) und Eile (8 s).");
            u7.SelfBuffs.Add(B(EffectType.Might, 6, 12f)); u7.SelfBuffs.Add(B(EffectType.Swiftness, 1, 8f));
            var u8 = Skill("U8_Rauchbombe", "Rauchbombe", "Rauch", grey, SkillDelivery.GroundTarget, 0.25f, 20f, 15f, 0f, "Rauchfeld + Explosions-Finisher: Tarnung im Feld.");
            u8.Radius = 4f; u8.Field = ComboFieldType.Smoke; u8.FieldRadius = 4f; u8.FieldDuration = 5f; u8.Finisher = ComboFinisherType.Blast;
            var u9 = Skill("U9_Frostfessel", "Frostfessel", "Fessel", new Color(0.5f, 0.8f, 1f), SkillDelivery.GroundTarget, 0.5f, 20f, 18f, 0.6f, "Kälte und 2 Verwundbarkeit im Bereich.");
            u9.Radius = 4f; u9.TargetBuffs.Add(B(EffectType.Chill, 1, 4f)); u9.TargetBuffs.Add(B(EffectType.Vulnerability, 2, 6f));
            var elite = Skill("E0_Meteorzorn", "Meteorzorn", "ELITE", new Color(1f, 0.3f, 0.1f), SkillDelivery.GroundTarget, 1.5f, 75f, 25f, 4.0f, "Meteor: großer Flächenschaden, Brennen, Feuerfeld und Explosions-Finisher.");
            elite.Radius = 6f; elite.Field = ComboFieldType.Fire; elite.FieldRadius = 6f; elite.FieldDuration = 6f; elite.Finisher = ComboFinisherType.Blast;
            elite.TargetBuffs.Add(B(EffectType.Burning, 4, 6f));

            // ---- Fertigkeiten-Pool (M10): 3 Heil, 9 Hilfs (Ruf/Meditation/Falle), 2 Elite ----
            heal.SlotType = SkillSlotType.Heal; heal.Category = UtilityCategory.None; heal.StartsUnlocked = true; heal.UnlockCost = 0;
            var h7 = Skill("H7_AtemDerWacht", "Atem der Wacht", "Atem", gold, SkillDelivery.Self, 0.5f, 20f, 0f, 0f, "Ruf: heilt dich und Verbündete in der Nähe, gewährt Regeneration.");
            h7.SlotType = SkillSlotType.Heal; h7.Category = UtilityCategory.Shout; h7.UnlockCost = 2; h7.HealBase = 3600f; h7.HealCoefficient = 0.8f; h7.ShareWithAllies = true; h7.Radius = 6f;
            h7.SelfBuffs.Add(B(EffectType.Regeneration, 1, 8f));
            var h8 = Skill("H8_InnereRuhe", "Innere Ruhe", "Ruhe", new Color(0.7f, 0.85f, 1f), SkillDelivery.Self, 1f, 30f, 0f, 0f, "Meditation: starke Heilung, entfernt 2 Zustände, Entschlossenheit.");
            h8.SlotType = SkillSlotType.Heal; h8.Category = UtilityCategory.Meditation; h8.UnlockCost = 2; h8.HealBase = 6200f; h8.HealCoefficient = 1.2f; h8.CleanseCount = 2;
            h8.SelfBuffs.Add(B(EffectType.Resolution, 1, 4f));

            void Util(SkillDefinition u, UtilityCategory cat, int tier, bool start) { u.SlotType = SkillSlotType.Utility; u.Category = cat; u.Tier = tier; u.UnlockCost = start ? 0 : tier * 2; u.StartsUnlocked = start; }
            Util(u7, UtilityCategory.Shout, 1, true);
            Util(u8, UtilityCategory.Trap, 2, true);
            Util(u9, UtilityCategory.Trap, 1, true);
            var u10 = Skill("U10_HaltetStand", "Haltet stand!", "Stand", gold, SkillDelivery.Self, 0.25f, 30f, 0f, 0f, "Ruf: Schutz und Stabilität für dich und Verbündete.");
            Util(u10, UtilityCategory.Shout, 2, false); u10.ShareWithAllies = true; u10.Radius = 6f; u10.SelfBuffs.Add(B(EffectType.Protection, 1, 5f)); u10.SelfBuffs.Add(B(EffectType.Stability, 2, 5f));
            var u11 = Skill("U11_ZumAngriff", "Zum Angriff!", "Angriff", gold, SkillDelivery.Self, 0.25f, 35f, 0f, 0f, "Ruf: Wut und Schnelligkeit für dich und Verbündete.");
            Util(u11, UtilityCategory.Shout, 3, false); u11.ShareWithAllies = true; u11.Radius = 6f; u11.SelfBuffs.Add(B(EffectType.Fury, 1, 8f)); u11.SelfBuffs.Add(B(EffectType.Quickness, 1, 3f));
            var u12 = Skill("U12_Klingenfokus", "Klingenfokus", "Fokus", new Color(0.7f, 0.85f, 1f), SkillDelivery.Self, 0f, 25f, 0f, 0f, "Meditation: Wut und 5 Macht.");
            Util(u12, UtilityCategory.Meditation, 1, false); u12.SelfBuffs.Add(B(EffectType.Fury, 1, 10f)); u12.SelfBuffs.Add(B(EffectType.Might, 5, 10f));
            var u13 = Skill("U13_LeereStille", "Leere Stille", "Stille", new Color(0.7f, 0.85f, 1f), SkillDelivery.Self, 0f, 30f, 0f, 0f, "Meditation: entfernt 3 Zustände, Widerstand.");
            Util(u13, UtilityCategory.Meditation, 2, false); u13.CleanseCount = 3; u13.SelfBuffs.Add(B(EffectType.Resistance, 1, 4f));
            var u14 = Skill("U14_Sturmschritt", "Sturmschritt", "Schritt", new Color(0.7f, 0.85f, 1f), SkillDelivery.Self, 0f, 35f, 0f, 0f, "Meditation: Aegis, Elan und Eile.");
            Util(u14, UtilityCategory.Meditation, 3, false); u14.SelfBuffs.Add(B(EffectType.Aegis, 1, 6f)); u14.SelfBuffs.Add(B(EffectType.Vigor, 1, 8f)); u14.SelfBuffs.Add(B(EffectType.Swiftness, 1, 10f));
            var u15 = Skill("U15_Dornengrube", "Dornengrube", "Grube", new Color(0.5f, 0.75f, 0.3f), SkillDelivery.GroundTarget, 0.5f, 25f, 18f, 0.5f, "Falle: Blutung, Verkrüppelung und Immobilisierung im Bereich.");
            Util(u15, UtilityCategory.Trap, 3, false); u15.Radius = 4f; u15.TargetBuffs.Add(B(EffectType.Bleeding, 4, 8f)); u15.TargetBuffs.Add(B(EffectType.Cripple, 1, 4f)); u15.TargetBuffs.Add(B(EffectType.Immobilize, 1, 1.5f));
            elite.SlotType = SkillSlotType.Elite; elite.StartsUnlocked = true; elite.UnlockCost = 0;
            var e1 = Skill("E1_Waechteravatar", "Wächteravatar", "Avatar", gold, SkillDelivery.Self, 0.5f, 90f, 0f, 0f, "Elite: 10 Macht, Schutz, Stabilität, Schnelligkeit und Regeneration für dich und Verbündete.");
            e1.SlotType = SkillSlotType.Elite; e1.UnlockCost = 8; e1.ShareWithAllies = true; e1.Radius = 8f;
            e1.SelfBuffs.Add(B(EffectType.Might, 10, 15f)); e1.SelfBuffs.Add(B(EffectType.Protection, 1, 8f)); e1.SelfBuffs.Add(B(EffectType.Stability, 3, 8f)); e1.SelfBuffs.Add(B(EffectType.Quickness, 1, 5f)); e1.SelfBuffs.Add(B(EffectType.Regeneration, 1, 10f));
            foreach (var w in new[] { gs1, gs2, gs3, gs4, gs5 }) w.Weapon = WeaponKind.Greatsword;
            foreach (var w in new[] { st1, st2, st3, st4, st5 }) w.Weapon = WeaponKind.Staff;

            // ---- Lauf 5: Auto-Angriffs-Ketten (3 Glieder, steigender Koeffizient, Mittel = bisheriger Wert, Glied 3 mit Zusatzeffekt) ----
            gs1.Coefficient = 0.72f; gs1.ShortLabel = "Glut I";
            var gs1b = Skill("GS1b_Glutkreuz", "Glutkreuz", "Glut II", ember, SkillDelivery.Melee, 0.5f, 0f, 3.2f, 0.8f, "Zweiter Hieb der Kette. Verursacht 1 Stapel Verwundbarkeit.");
            gs1b.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 5f));
            var gs1c = Skill("GS1c_Glutfinale", "Glutfinale", "Glut III", ember, SkillDelivery.Melee, 0.5f, 0f, 3.2f, 0.88f, "Abschluss der Kette: Hieb von oben, 1 Verwundbarkeit und 1 Brennen (3 s).");
            gs1c.TargetBuffs.Add(B(EffectType.Vulnerability, 1, 5f)); gs1c.TargetBuffs.Add(B(EffectType.Burning, 1, 3f));
            st1.Coefficient = 0.63f; st1.ShortLabel = "Bolzen I";
            var st1b = Skill("ST1b_Funkenpaar", "Funkenpaar", "Bolzen II", storm, SkillDelivery.Projectile, 0.6f, 0f, 24f, 0.7f, "Zweites Geschoss der Kette. Projektil-Finisher.");
            st1b.Finisher = ComboFinisherType.Projectile;
            var st1c = Skill("ST1c_Funkensturm", "Funkensturm", "Bolzen III", storm, SkillDelivery.Projectile, 0.6f, 0f, 24f, 0.77f, "Abschluss der Kette: starkes Geschoss, 2 Verwundbarkeit. Projektil-Finisher.");
            st1c.Finisher = ComboFinisherType.Projectile; st1c.TargetBuffs.Add(B(EffectType.Vulnerability, 2, 6f));
            foreach (var w in new[] { gs1b, gs1c }) w.Weapon = WeaponKind.Greatsword;
            foreach (var w in new[] { st1b, st1c }) w.Weapon = WeaponKind.Staff;
            gs1.Chain = new[] { gs1b, gs1c }; st1.Chain = new[] { st1b, st1c };
            foreach (var w in new[] { gs1b, gs1c, st1b, st1c }) EditorUtility.SetDirty(w);

            // ---- Downed ----
            var d1 = Skill("D1_VerzweifelterWurf", "Verzweifelter Wurf", "Wurf", new Color(0.8f, 0.5f, 0.4f), SkillDelivery.Projectile, 0.75f, 0f, 15f, 0.35f, "Schwacher Fernangriff im Downed State.");
            var d2 = Skill("D2_Schildstoss", "Schildstoß", "Stoß", new Color(1f, 0.9f, 0.4f), SkillDelivery.Melee, 0.25f, 15f, 4f, 0.15f, "Unterbricht und betäubt den Gegner (1,5 s).");
            d2.Special = SkillSpecial.Interrupt;
            var d3 = Skill("D3_Zaehigkeit", "Zähigkeit", "Zäh", new Color(0.4f, 1f, 0.5f), SkillDelivery.Self, 0f, 20f, 0f, 0f, "Heilt 1.200 Downed-HP.");
            d3.Special = SkillSpecial.DownedSelfHeal; d3.HealBase = 3000f; d3.HealCoefficient = 0.3f;
            var d4 = Skill("D4_Bandagieren", "Bandagieren", "Band.", new Color(0.9f, 0.9f, 0.9f), SkillDelivery.Self, 4f, 0f, 0f, 0f, "Kanalisiert 4 s: heilt Downed-HP; bei voller Leiste stehst du auf.");
            d4.Special = SkillSpecial.Bandage;

            foreach (var d in new[] { d1, d2, d3, d4 }) d.SlotType = SkillSlotType.Downed;
            var all = new SkillDefinition[] { gs1, gs2, gs3, gs4, gs5, st1, st2, st3, st4, st5, heal, h7, h8, u7, u8, u9, u10, u11, u12, u13, u14, u15, elite, e1, d1, d2, d3, d4 };
            foreach (var s in all) EditorUtility.SetDirty(s);
            SkillTimelineBuilder.BuildAll();

            var wA = Asset<WeaponSet>(WeaponDir + "/Weapon_Glutklinge.asset");
            wA.DisplayName = "Glutklinge"; wA.Visual = WeaponVisual.Greatsword; wA.WeaponColor = ember; wA.Skills = new[] { gs1, gs2, gs3, gs4, gs5 };
            var wB = Asset<WeaponSet>(WeaponDir + "/Weapon_Sturmruf.asset");
            wB.DisplayName = "Sturmruf"; wB.Visual = WeaponVisual.Staff; wB.WeaponColor = storm; wB.Skills = new[] { st1, st2, st3, st4, st5 };
            EditorUtility.SetDirty(wA); EditorUtility.SetDirty(wB);
        }

        public static void ConfigurePlayerSkills(SkillCaster c)
        {
            c.WeaponSets = new[] { AssetDatabase.LoadAssetAtPath<WeaponSet>(WeaponDir + "/Weapon_Glutklinge.asset"), AssetDatabase.LoadAssetAtPath<WeaponSet>(WeaponDir + "/Weapon_Sturmruf.asset") };
            c.HealSkill = L("H6_Quellsegen");
            c.Utilities = new[] { L("U7_Kriegsschrei"), L("U8_Rauchbombe"), L("U9_Frostfessel") };
            c.EliteSkill = L("E0_Meteorzorn");
            c.DownedSkills = new[] { L("D1_VerzweifelterWurf"), L("D2_Schildstoss"), L("D3_Zaehigkeit"), L("D4_Bandagieren") };
            c.ComboTable = AssetDatabase.LoadAssetAtPath<ComboTable>(ComboDir + "/ComboTable.asset");
        }

        static SkillDefinition L(string id) => AssetDatabase.LoadAssetAtPath<SkillDefinition>(SkillDir + "/Skill_" + id + ".asset");

        // ---------------- Gegner & NPCs ----------------
        public static EnemyArchetype Archetype(string id) => AssetDatabase.LoadAssetAtPath<EnemyArchetype>(EnemyDir + "/Enemy_" + id + ".asset");

        public static void BuildEnemies()
        {
            var trash = Asset<EnemyArchetype>(EnemyDir + "/Enemy_Trash_Pluenderer.asset");
            trash.DisplayName = "Aschkrallen-Plünderer"; trash.Tier = EnemyTier.Trash; trash.Team = Team.Enemy;
            trash.MaxHealth = 0f; trash.HealthFactor = 0.75f; trash.AttackCoefficient = 3f; trash.AttributeFactor = 1f; trash.FixedLevel = 0; trash.AttackRange = 2.3f; trash.AttackInterval = 1.9f; trash.AttackWindup = 0.6f;
            trash.MoveSpeed = 4.6f; trash.AggroRange = 13f; trash.LeashRange = 40f; trash.Scale = 1f; trash.ContributionValue = 10f; trash.Experience = 40;
            trash.HasTelegraph = false; trash.OnHitDebuff = default;
            trash.Prefab = BuildNpcPrefab("PT_Enemy_Trash", EnemyTier.Trash, trash, "stylized painterly fantasy horned goblin-like raider with ash grey hide, red sash and bone club, game-ready low poly, hand-painted texture, full body");

            var vet = Asset<EnemyArchetype>(EnemyDir + "/Enemy_Veteran_Brecher.asset");
            vet.DisplayName = "Aschkrallen-Brecher"; vet.Tier = EnemyTier.Veteran; vet.Team = Team.Enemy;
            vet.MaxHealth = 0f; vet.HealthFactor = 3.5f; vet.AttackCoefficient = 5f; vet.AttributeFactor = 1.1f; vet.FixedLevel = 0; vet.AttackRange = 2.8f; vet.AttackInterval = 2.4f; vet.AttackWindup = 0.65f;
            vet.MoveSpeed = 4.2f; vet.AggroRange = 15f; vet.LeashRange = 45f; vet.Scale = 1.35f; vet.ContributionValue = 30f; vet.Experience = 150;
            vet.HasTelegraph = false; vet.OnHitDebuff = new EffectApplication(EffectType.Vulnerability, 2, 6f);
            vet.Prefab = BuildNpcPrefab("PT_Enemy_Veteran", EnemyTier.Veteran, vet, "stylized painterly fantasy hulking horned raider brute with dark iron pauldrons, red war sash and heavy bone club, game-ready low poly, hand-painted texture, full body");

            var champ = Asset<EnemyArchetype>(EnemyDir + "/Enemy_Champion_Grolmok.asset");
            champ.DisplayName = "Grolmok, Aschenfürst"; champ.Tier = EnemyTier.Champion; champ.Team = Team.Enemy;
            champ.MaxHealth = 0f; champ.HealthFactor = 18f; champ.AttackCoefficient = 7f; champ.AttributeFactor = 1.2f; champ.FixedLevel = 15; champ.AttackRange = 3.6f; champ.AttackInterval = 3.0f; champ.AttackWindup = 0.8f;
            champ.MoveSpeed = 4.0f; champ.AggroRange = 20f; champ.LeashRange = 60f; champ.Scale = 2.0f; champ.ContributionValue = 120f; champ.Experience = 800;
            champ.HasTelegraph = true; champ.TelegraphRadius = 5.5f; champ.TelegraphDelay = 1.5f; champ.TelegraphCoefficient = 20f; champ.TelegraphInterval = 10f;
            champ.OnHitDebuff = new EffectApplication(EffectType.Burning, 1, 3f);
            champ.Defiance = 500f;   // Lauf 4: Trotzleiste
            champ.Prefab = BuildNpcPrefab("PT_Enemy_Champion", EnemyTier.Champion, champ, "stylized painterly fantasy brute warlord, huge horned ash-skinned ogre chieftain with bone crown, glowing orange eyes, spiked shoulder armor, red war sash, holding a giant bone club, game-ready low poly, hand-painted texture, full body");

            var guard = Asset<EnemyArchetype>(EnemyDir + "/Ally_Dornwacht_Waechter.asset");
            guard.DisplayName = "Dornwacht-Wächter"; guard.Tier = EnemyTier.None; guard.Team = Team.Ally;
            guard.MaxHealth = 0f; guard.HealthFactor = 2.5f; guard.AttackCoefficient = 2.5f; guard.AttributeFactor = 1f; guard.FixedLevel = 10; guard.AttackRange = 2.4f; guard.AttackInterval = 1.8f; guard.AttackWindup = 0.6f;
            guard.MoveSpeed = 4.4f; guard.AggroRange = 14f; guard.LeashRange = 20f; guard.Scale = 1f; guard.ContributionValue = 0f;
            guard.Prefab = BuildGuardPrefab(guard);

            foreach (var a in new[] { trash, vet, champ, guard }) EditorUtility.SetDirty(a);
            BuildCratePrefab();
        }

        static GameObject BuildNpcPrefab(string name, EnemyTier tier, EnemyArchetype arch, string prompt)
        {
            var go = new GameObject(name);
            go.layer = CharacterLayer;
            var visual = CharacterBuilder.BuildEnemyVisual(go.transform, tier, out _);
            AddPlaceholder(visual, name, prompt);
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.5f; col.center = new Vector3(0, 0.9f, 0);
            var c = go.AddComponent<Combatant>();
            c.Team = Team.Enemy; c.Tier = tier; c.MaxHealth = arch.MaxHealth; c.DisplayName = arch.DisplayName;
            var brain = go.AddComponent<NpcBrain>();
            brain.Archetype = arch; brain.Visual = visual.transform;
            go.AddComponent<HitFlash>();
            CharacterRigs.Drive(go, visual, Anim.AnimDriver.Role.Npc);
            SetLayerRecursive(go, CharacterLayer);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, NpcPrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildGuardPrefab(EnemyArchetype arch)
        {
            var go = new GameObject("PT_Ally_Guard");
            go.layer = CharacterLayer;
            var visual = CharacterBuilder.BuildPlayerVisual(go.transform, out var gs, out var staff, CharacterRigs.GuardLook(), Anim.AnimDriver.Role.Npc);
            // Wächter: Speer statt Zweihänder, blaue Uniform, Helm und Schild (Look)
            Object.DestroyImmediate(staff);
            gs.transform.localEulerAngles = new Vector3(10, 0, 0);
            AddPlaceholder(visual, "ally_guard", "stylized painterly fantasy frontier guard in blue tabard with spear and round shield, game-ready low poly, hand-painted texture, full body");
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.4f; col.center = new Vector3(0, 0.9f, 0);
            var c = go.AddComponent<Combatant>();
            c.Team = Team.Ally; c.MaxHealth = arch.MaxHealth; c.DisplayName = arch.DisplayName; c.Targetable = false;
            var brain = go.AddComponent<NpcBrain>();
            brain.Archetype = arch; brain.Visual = visual.transform; brain.DestroyOnDeath = false; brain.WanderRadius = 1.5f;
            go.AddComponent<HitFlash>();
            CharacterRigs.Drive(go, visual, Anim.AnimDriver.Role.Npc);
            SetLayerRecursive(go, CharacterLayer);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, NpcPrefabDir + "/PT_Ally_Guard.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject CratePrefab => AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabDir + "/PT_SupplyCrate.prefab");

        static void BuildCratePrefab()
        {
            var go = new GameObject("PT_SupplyCrate");
            go.layer = CharacterLayer;
            var v = Child(go.transform, "Visual", Vector3.zero);
            var wood = Mat("Crate", new Color(0.70f, 0.48f, 0.26f));
            var band = Mat("Metal", new Color(0.78f, 0.80f, 0.86f), 0.15f, null, 0.9f);
            Part(v.transform, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(1.4f, 1f, 1f), wood, "Box");
            Part(v.transform, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(1.45f, 0.12f, 1.05f), band, "Band");
            Part(v.transform, PrimitiveType.Cube, new Vector3(0.2f, 1.3f, 0), new Vector3(0.9f, 0.6f, 0.8f), wood, "Box2", new Vector3(0, 20, 0));
            Part(v.transform, PrimitiveType.Cube, new Vector3(-0.3f, 1.12f, 0.1f), new Vector3(0.5f, 0.25f, 0.5f), Mat("Sack", new Color(0.85f, 0.78f, 0.55f)), "Sack");
            AddPlaceholder(v, "supply_crate", "stylized painterly stack of wooden supply crates and grain sacks, low poly, hand-painted");
            var col = go.AddComponent<BoxCollider>(); col.size = new Vector3(1.5f, 1.6f, 1.1f); col.center = new Vector3(0, 0.8f, 0);
            var c = go.AddComponent<Combatant>();
            c.Team = Team.Ally; c.MaxHealth = 3000f; c.DisplayName = "Vorräte der Dornwacht"; c.Targetable = false; c.Height = 1.2f;
            go.AddComponent<HitFlash>();
            SetLayerRecursive(go, CharacterLayer);
            PrefabUtility.SaveAsPrefabAsset(go, NpcPrefabDir + "/PT_SupplyCrate.prefab");
            Object.DestroyImmediate(go);
        }
    }
}
