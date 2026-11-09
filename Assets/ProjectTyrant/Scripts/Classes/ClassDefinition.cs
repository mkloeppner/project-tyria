using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum ArmorWeight { Light, Medium, Heavy }
    public enum ClassMechanicKind { None, Pet, Attunement, Initiative }

    /// <summary>Lauf 5 / M25: Klasse (Wächter, Wildhüter, Elementarweber, Schattenklinge) – Waffen, Fertigkeiten-Pool, Linien, Mechanik.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Class Definition", fileName = "Class_")]
    public class ClassDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Summary;
        public ArmorWeight Armor = ArmorWeight.Heavy;
        public Color Color = Color.white;
        public Color SecondaryColor = Color.gray;
        public VfxPalette Palette = VfxPalette.Licht;
        public ClassMechanicKind Mechanic = ClassMechanicKind.None;
        public string ResourceName = "";
        [Tooltip("F1–F4 Beschriftung")] public string[] MechanicLabels = new string[4];
        [TextArea] public string MechanicSummary;
        public List<WeaponSet> WeaponSets = new List<WeaponSet>();
        public WeaponKind[] StartWeapons = new WeaponKind[2];
        public List<SkillDefinition> SkillPool = new List<SkillDefinition>();   // Heil 3, Hilfe 9, Elite 2
        public List<SkillDefinition> ExtraSkills = new List<SkillDefinition>(); // Gefährten-, gestohlene, Beschwörungs-Skills
        public List<SpecializationDefinition> Specializations = new List<SpecializationDefinition>();
        [Tooltip("Lauf 5 / M26: vier Elite-Spezialisierungen")] public List<EliteSpecDefinition> EliteSpecs = new List<EliteSpecDefinition>();
        [Tooltip("Waffenwechsel im Kampf erlaubt (Elementarweber: nein, Einstimmungen ersetzen ihn)")] public bool SwapInCombat = true;
        public string SignatureSkill;

        public WeaponSet SetFor(WeaponKind k) { foreach (var w in WeaponSets) if (w != null && w.Kind == k) return w; return null; }
        public bool CanWield(WeaponKind k) => SetFor(k) != null;

        /// <summary>Waffenset inklusive der Elite-Waffe (ab Schritt 2 der aktiven Spezialisierung).</summary>
        public WeaponSet SetFor(WeaponKind k, EliteSpecDefinition elite, int eliteSteps)
        {
            var w = SetFor(k); if (w != null) return w;
            return elite != null && eliteSteps >= EliteSpecDefinition.StepWeapon && elite.Weapon != null && elite.Weapon.Kind == k ? elite.Weapon : null;
        }
        public bool CanWield(WeaponKind k, EliteSpecDefinition elite, int eliteSteps) => SetFor(k, elite, eliteSteps) != null;
        public EliteSpecDefinition Elite(string id) { foreach (var e in EliteSpecs) if (e != null && e.Id == id) return e; return null; }

        public IEnumerable<SkillDefinition> AllSkills()
        {
            foreach (var w in WeaponSets)
            {
                if (w == null) continue;
                foreach (var s in w.Skills) if (s != null) { yield return s; if (s.Chain != null) foreach (var c in s.Chain) if (c != null) yield return c; }
                if (w.Attunement != null) foreach (var s in w.Attunement) if (s != null) yield return s;
                if (w.StealthSkill != null) yield return w.StealthSkill;
            }
            foreach (var s in SkillPool) if (s != null) yield return s;
            foreach (var s in ExtraSkills) if (s != null) yield return s;
        }
    }

}
