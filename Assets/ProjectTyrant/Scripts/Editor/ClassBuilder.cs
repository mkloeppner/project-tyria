using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M25: baut die Klassen-Daten (Wächter-Hülle + Wildhüter, Elementarweber, Schattenklinge): Skills aus kompakten Tabellen,
    /// Waffensets, Fertigkeiten-Pools, Eigenschaftslinien und die ClassLibrary. Zeitachse/VFX/SFX leitet der SkillTimelineBuilder ab.
    /// </summary>
    public static partial class ClassBuilder
    {
        public const string Root = DataDir + "/Classes";
        public const string LibraryPath = Root + "/ClassLibrary.asset";
        public static ClassLibrary Library => AssetDatabase.LoadAssetAtPath<ClassLibrary>(LibraryPath);

        static string _cls; static Color _color; static VfxPalette _pal; static string _style;
        static readonly List<Object> _dirty = new List<Object>();

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            _dirty.Add(a); return a;
        }

        static void Begin(string cls, Color c, VfxPalette pal)
        {
            _cls = cls; _color = c; _pal = pal;
            EnsureFolder(Root); EnsureFolder(Root + "/" + cls); EnsureFolder(Root + "/" + cls + "/Skills"); EnsureFolder(Root + "/" + cls + "/Weapons"); EnsureFolder(Root + "/" + cls + "/Traits");
        }

        /// <summary>Neuer Skill mit GW2-üblichen Standardwerten (Klasse, Palette, Animationsstil aus dem Kontext).</summary>
        static SkillDefinition S(string id, string name, string label, SkillDelivery del, float cast, float cd, float range, float coef, string desc)
        {
            var s = Asset<SkillDefinition>(Root + "/" + _cls + "/Skills/Skill_" + id + ".asset");
            s.Id = _cls + "_" + id; s.DisplayName = name; s.ShortLabel = label; s.IconColor = _color; s.Delivery = del;
            s.CastTime = cast; s.Cooldown = cd; s.Range = range; s.Coefficient = coef; s.Description = desc;
            s.Hits = 1; s.HitInterval = 0.25f; s.HealBase = 0; s.HealCoefficient = 0; s.LeapDistance = 0; s.ProjectileSpeed = 34f;
            s.Radius = 2.5f; s.Field = ComboFieldType.None; s.Finisher = ComboFinisherType.None; s.FieldTickCoefficient = 0; s.CleanseCount = 0; s.ShareWithAllies = false; s.TargetCount = 5;
            s.FieldDuration = 5f; s.FieldRadius = 4f; s.Special = SkillSpecial.None; s.EvadesDuringCast = false;
            s.TargetBuffs = new List<EffectApplication>(); s.SelfBuffs = new List<EffectApplication>();
            s.SlotType = SkillSlotType.Weapon; s.Category = UtilityCategory.None; s.Tier = 1; s.UnlockCost = 0; s.StartsUnlocked = true; s.Weapon = WeaponKind.None;
            s.ClassId = _cls; s.InitiativeCost = 0; s.Element = Element.None; s.StealthSeconds = 0f; s.StealthAttack = false; s.ConjureSet = null; s.TrapLifetime = 0f;
            s.Palette = _pal; s.PaletteExplicit = true; s.WeaponStyleOverride = _style ?? ""; s.Chain = new SkillDefinition[0];
            return s;
        }

        static EffectApplication B(EffectType t, int n, float d) => new EffectApplication(t, n, d);

        static SkillDefinition Heal(SkillDefinition s, float heal, float coef, bool start) { s.SlotType = SkillSlotType.Heal; s.HealBase = heal; s.HealCoefficient = coef; s.StartsUnlocked = start; s.UnlockCost = start ? 0 : 2; s.WeaponStyleOverride = ""; return s; }
        static SkillDefinition Util(SkillDefinition s, UtilityCategory cat, int tier, bool start) { s.SlotType = SkillSlotType.Utility; s.Category = cat; s.Tier = tier; s.StartsUnlocked = start; s.UnlockCost = start ? 0 : tier * 2; s.WeaponStyleOverride = ""; return s; }
        static SkillDefinition Elite(SkillDefinition s, bool start) { s.SlotType = SkillSlotType.Elite; s.StartsUnlocked = start; s.UnlockCost = start ? 0 : 8; s.WeaponStyleOverride = ""; return s; }

        static WeaponSet Set(string id, string name, WeaponKind kind, WeaponVisual vis, Color col, params SkillDefinition[] skills)
        {
            var w = Asset<WeaponSet>(Root + "/" + _cls + "/Weapons/Weapon_" + id + ".asset");
            w.DisplayName = name; w.Kind = kind; w.Visual = vis; w.WeaponColor = col; w.ClassId = _cls; w.Skills = skills; w.Attunement = new SkillDefinition[0]; w.StealthSkill = null;
            foreach (var s in skills) if (s != null) { s.Weapon = kind; if (s.Chain != null) foreach (var c in s.Chain) if (c != null) c.Weapon = kind; }
            return w;
        }

        /// <summary>Auto-Kette: Glied 2 und 3 als Kopien mit steigendem Koeffizienten, Glied 3 mit Zusatzeffekt.</summary>
        static void Chain(SkillDefinition first, string n2, string n3, float c1, float c2, float c3, EffectApplication finalEffect)
        {
            first.Coefficient = c1; string baseLabel = first.ShortLabel; first.ShortLabel = baseLabel + " I";
            var b = S(first.name.Replace("Skill_", "") + "b", n2, baseLabel + " II", first.Delivery, first.CastTime, 0f, first.Range, c2, "Zweites Glied der Kette.");
            var c = S(first.name.Replace("Skill_", "") + "c", n3, baseLabel + " III", first.Delivery, first.CastTime, 0f, first.Range, c3, "Abschluss der Kette mit Zusatzeffekt.");
            foreach (var x in new[] { b, c }) { x.Radius = first.Radius; x.ProjectileSpeed = first.ProjectileSpeed; x.Finisher = first.Finisher; x.TargetBuffs = new List<EffectApplication>(first.TargetBuffs); x.WeaponStyleOverride = first.WeaponStyleOverride; x.InitiativeCost = 0; }
            c.TargetBuffs.Add(finalEffect);
            first.Chain = new[] { b, c };
        }

        static ClassDefinition Class(string id, string name, ArmorWeight armor, Color c, Color c2, ClassMechanicKind mech, string resource, string[] fLabels, string mechText, string summary)
        {
            var d = Asset<ClassDefinition>(Root + "/" + id + "/Class_" + id + ".asset");
            d.Id = id; d.DisplayName = name; d.Armor = armor; d.Color = c; d.SecondaryColor = c2; d.Mechanic = mech; d.ResourceName = resource; d.MechanicLabels = fLabels; d.MechanicSummary = mechText; d.Summary = summary;
            d.WeaponSets = new List<WeaponSet>(); d.SkillPool = new List<SkillDefinition>(); d.ExtraSkills = new List<SkillDefinition>(); d.Specializations = new List<SpecializationDefinition>();
            return d;
        }

        [MenuItem("Project Tyrant/Lauf 5/Klassen bauen")]
        public static void BuildAll()
        {
            _dirty.Clear();
            EnsureFolder(Root);
            var lib = Asset<ClassLibrary>(LibraryPath);
            Begin("wildhueter", Color.white, VfxPalette.Natur); BuildPets();
            lib.Classes = new List<ClassDefinition> { BuildGuardian(), BuildRanger(), BuildWeaver(), BuildThief() };
            BuildElites(lib);   // Lauf 5 / M26
            NormalizeChains(lib);   // Fix-Lauf F1
            ApplyBalance(lib);      // M27: Klassen-/Spezialisierungs-Schaden ±15 % zum Wächter
            foreach (var o in _dirty) if (o != null) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            int skills = 0, elite = 0, specs = 0; foreach (var c in lib.Classes) { foreach (var _ in c.AllSkills()) skills++; foreach (var e in c.EliteSpecs) { specs++; foreach (var _ in e.AllSkills()) elite++; } }
            Debug.Log("[ProjectTyrant] Klassen: " + lib.Classes.Count + " · Skills " + skills + " · Elite-Spezialisierungen " + specs + " mit " + elite + " Skills");
        }

        // ------------------------------------------------------------------ Wächter (bestehende Daten aus DataBuilder/TraitBuilder)
        static ClassDefinition BuildGuardian()
        {
            Begin("waechter", new Color(0.95f, 0.8f, 0.35f), VfxPalette.Licht);
            var d = Class("waechter", "Wächter", ArmorWeight.Heavy, new Color(0.95f, 0.8f, 0.35f), new Color(0.35f, 0.55f, 0.95f), ClassMechanicKind.None, "",
                new[] { "", "", "", "" }, "Schwere Rüstung, Segen und Feuer: Glutklinge im Nahkampf, Sturmruf aus der Ferne.", "Schwere Rüstung · Segen, Rufe und Meditationen · Großschwert und Stab.");
            var gs = AssetDatabase.LoadAssetAtPath<WeaponSet>(DataDir + "/Weapons/Weapon_Glutklinge.asset");
            var st = AssetDatabase.LoadAssetAtPath<WeaponSet>(DataDir + "/Weapons/Weapon_Sturmruf.asset");
            if (gs != null) { gs.Kind = WeaponKind.Greatsword; gs.ClassId = "waechter"; _dirty.Add(gs); d.WeaponSets.Add(gs); }
            if (st != null) { st.Kind = WeaponKind.Staff; st.ClassId = "waechter"; _dirty.Add(st); d.WeaponSets.Add(st); }
            d.StartWeapons = new[] { WeaponKind.Greatsword, WeaponKind.Staff };
            foreach (var g in AssetDatabase.FindAssets("t:SkillDefinition", new[] { DataDir + "/Skills" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null && (s.SlotType == SkillSlotType.Heal || s.SlotType == SkillSlotType.Utility || s.SlotType == SkillSlotType.Elite)) d.SkillPool.Add(s);
            }
            d.Specializations = new List<SpecializationDefinition>(TraitBuilder.Load());
            d.Palette = VfxPalette.Licht; d.SignatureSkill = "GS5_Bergbrecher";
            return d;
        }
    }
}

namespace ProjectTyrant.EditorTools
{
    public static partial class ClassBuilder
    {
        /// <summary>Spieler-Komponente mit Gefährten-Archetypen, Familien- und gestohlenen Skills verdrahten.</summary>
        public static void ConfigureHost(ClassMechanicHost host)
        {
            var lib = Library; if (lib == null) return;
            var ranger = lib.Get("wildhueter"); var thief = lib.Get("schattenklinge");
            host.PetArchetypes = new[] { PetArch("Wolf"), PetArch("Baer"), PetArch("Falke") };
            if (ranger != null) host.PetSkills = new[] { ranger.ExtraSkills.Find(s => s.Id == "pet_wolf_howl"), ranger.ExtraSkills.Find(s => s.Id == "pet_bear_roar"), ranger.ExtraSkills.Find(s => s.Id == "pet_falcon_dive") };
            if (thief != null) host.StolenSkills = thief.ExtraSkills.ToArray();
        }

        static EnemyArchetype PetArch(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>(Root + "/wildhueter/Pet_" + n + ".asset");
    }
}
