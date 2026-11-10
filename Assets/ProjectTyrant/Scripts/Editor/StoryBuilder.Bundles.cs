using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 6: Bündel (GW2) – Eimer, Heugabel, Fackel, Sandsack, Harpune, Laterne, Verbandskasten mit je 1–3 Bündel-Skills und „Fallen lassen“.</summary>
    public static partial class StoryBuilder
    {
        public const string BundleDir = Root + "/Bundles";

        public static List<BundleDef> BuildBundles()
        {
            PTEditorUtil.EnsureFolder(Root); PTEditorUtil.EnsureFolder(BundleDir);
            var drop = Sk("Drop", "Fallen lassen", "Ab", SkillDelivery.Self, 0f, 0f, 0f, 0f, "Legt das Bündel ab.", VfxPalette.Erde);
            var list = new List<BundleDef>
            {
                B("eimer", "Wassereimer", new Color(0.45f, 0.65f, 0.95f), 0f,
                    Sk("Eimer_Giessen", "Gießen", "Gi", SkillDelivery.Self, 0.6f, 1f, 0f, 0f, "Gießt Wasser um dich – Pflanzen, Brände, Durst.", VfxPalette.Wasser, heal: 0.1f, radius: 3f),
                    Sk("Eimer_Loeschen", "Löschen", "Lö", SkillDelivery.GroundTarget, 0.75f, 6f, 12f, 0f, "Schleudert Wasser: Wasserfeld, löscht Feuer.", VfxPalette.Wasser, field: ComboFieldType.Water)),
                B("heugabel", "Heugabel", new Color(0.65f, 0.5f, 0.3f), 0f,
                    Sk("Heugabel_Stoss", "Stoß", "St", SkillDelivery.Melee, 0.5f, 0f, 3f, 0.5f, "Stößt zu. Treibt Tiere und Plünderer zurück.", VfxPalette.Erde),
                    Sk("Heugabel_Fegen", "Fegen", "Fe", SkillDelivery.Self, 0.75f, 4f, 0f, 0.3f, "Fegt um dich herum: Heu, Glut, Schlamm.", VfxPalette.Erde, radius: 3.5f)),
                B("fackel", "Fackel", new Color(1f, 0.55f, 0.2f), 6f,
                    Sk("Fackel_Brand", "Brandschlag", "Br", SkillDelivery.Melee, 0.5f, 0f, 3f, 0.4f, "Schlag mit der Fackel, setzt in Brand.", VfxPalette.Feuer, burn: true),
                    Sk("Fackel_Anzuenden", "Anzünden", "An", SkillDelivery.GroundTarget, 0.75f, 8f, 10f, 0.2f, "Entzündet Signalfeuer, Barrikaden und Spinnweben: Feuerfeld.", VfxPalette.Feuer, field: ComboFieldType.Fire)),
                B("sandsack", "Sandsack", new Color(0.8f, 0.7f, 0.45f), 0f,
                    Sk("Sandsack_Werfen", "Werfen", "We", SkillDelivery.Projectile, 0.6f, 0f, 15f, 0.4f, "Wirft den Sack, verkrüppelt.", VfxPalette.Erde, cripple: true),
                    Sk("Sandsack_Schichten", "Aufschichten", "Au", SkillDelivery.Self, 1f, 6f, 0f, 0f, "Schichtet einen Wall: Schutz für dich und Verbündete.", VfxPalette.Erde, protect: true)),
                B("harpune", "Harpune", new Color(0.6f, 0.65f, 0.7f), 0f,
                    Sk("Harpune_Wurf", "Harpunenwurf", "Ha", SkillDelivery.Projectile, 0.75f, 0f, 20f, 1.1f, "Schleudert die Harpune.", VfxPalette.Wasser),
                    Sk("Harpune_Netz", "Netz", "Ne", SkillDelivery.Projectile, 0.5f, 10f, 15f, 0.2f, "Wirft ein Netz: festgehalten.", VfxPalette.Wasser, immob: true)),
                B("laterne", "Laterne", new Color(1f, 0.85f, 0.5f), 14f,
                    Sk("Laterne_Leuchten", "Aufleuchten", "Le", SkillDelivery.Self, 0.5f, 8f, 0f, 0f, "Grelles Licht: blendet Gegner um dich, vertreibt Tiefenschrecken.", VfxPalette.Licht, blind: true, radius: 6f)),
                B("verband", "Verbandskasten", new Color(0.9f, 0.9f, 0.9f), 0f,
                    Sk("Verband_Verbinden", "Verbinden", "Ve", SkillDelivery.Self, 1.5f, 2f, 0f, 0f, "Verbindet Verletzte in der Nähe (Herz-Aufgabe) und dich.", VfxPalette.Natur, heal: 0.4f, radius: 4f)),
            };
            foreach (var b in list) b.Drop = drop;
            var set = ClipBuilder.HumanoidSet;
            if (set != null) foreach (var b in list) { foreach (var s in b.Skills) { s.Timeline = SkillTimelineBuilder.Build(s, set, false, 0); EditorUtility.SetDirty(s); } }
            if (set != null) { drop.Timeline = SkillTimelineBuilder.Build(drop, set, false, 0); EditorUtility.SetDirty(drop); }
            var db = Load(); if (db != null) { db.Bundles = list; EditorUtility.SetDirty(db); }
            AssetDatabase.SaveAssets();
            return list;
        }

        static BundleDef B(string id, string name, Color c, float light, params SkillDefinition[] skills) => new BundleDef { Id = id, Name = name, Color = c, LightRadius = light, Skills = skills };

        static SkillDefinition Sk(string id, string name, string label, SkillDelivery d, float cast, float cd, float range, float coef, string desc, VfxPalette pal,
            ComboFieldType field = ComboFieldType.None, float heal = 0f, float radius = 2.5f, bool burn = false, bool cripple = false, bool protect = false, bool immob = false, bool blind = false)
        {
            string path = BundleDir + "/Skill_Bundle_" + id + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (s == null) { s = ScriptableObject.CreateInstance<SkillDefinition>(); AssetDatabase.CreateAsset(s, path); }
            s.Id = "Bundle_" + id; s.DisplayName = name; s.ShortLabel = label; s.Delivery = d; s.CastTime = cast; s.Cooldown = cd; s.Range = range; s.Coefficient = coef; s.Description = desc;
            s.Hits = 1; s.HitInterval = 0.25f; s.Radius = radius; s.Field = field; s.FieldDuration = 4f; s.FieldRadius = 3f; s.FieldTickCoefficient = 0f; s.TargetCount = 5; s.ProjectileSpeed = 28f;
            s.HealBase = heal > 0f ? 900f : 0f; s.HealCoefficient = heal;
            s.TargetBuffs = new List<EffectApplication>(); s.SelfBuffs = new List<EffectApplication>();
            if (burn) s.TargetBuffs.Add(new EffectApplication(EffectType.Burning, 1, 3f));
            if (cripple) s.TargetBuffs.Add(new EffectApplication(EffectType.Cripple, 1, 3f));
            if (immob) s.TargetBuffs.Add(new EffectApplication(EffectType.Immobilize, 1, 2f));
            if (blind) s.TargetBuffs.Add(new EffectApplication(EffectType.Blind, 1, 3f));
            if (protect) { s.SelfBuffs.Add(new EffectApplication(EffectType.Protection, 1, 5f)); s.ShareWithAllies = true; }
            s.SlotType = SkillSlotType.Weapon; s.Category = UtilityCategory.None; s.Tier = 1; s.UnlockCost = 0; s.StartsUnlocked = true; s.Weapon = WeaponKind.None;
            s.Palette = pal; s.PaletteExplicit = true; s.WeaponStyleOverride = d == SkillDelivery.Projectile ? "Bow" : "Sword"; s.ClassId = "";
            s.IconColor = SkillTimelineBuilder.PaletteColors.TryGetValue(pal, out var col) ? col : Color.white;
            EditorUtility.SetDirty(s);
            return s;
        }
    }
}
