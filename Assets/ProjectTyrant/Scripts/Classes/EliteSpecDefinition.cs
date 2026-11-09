using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M26: Mechanik-Variante einer Elite-Spezialisierung (Umsetzung in ClassMechanicHost.Elite).</summary>
    public enum EliteMechanic
    {
        None,
        Hainform, Verschmelzen, Entfesseln, Markieren,                 // Wildhüter
        Ueberladen, DoppelEinstimmung, Elementarkern, Erdbruch,        // Elementarweber
        Bosheit, Phantomtanz, Schattenschleier, Rueckruf,              // Schattenklinge
        Urteil, Festung, Sturmzorn, Dornenhaut                         // Wächter
    }

    /// <summary>
    /// Lauf 5 / M26: Elite-Spezialisierung wie GW2 – eine besondere Eigenschaftslinie (3 Neben + 9 Haupt), die nur den dritten
    /// Spezialisierungsplatz belegen kann; nur eine gleichzeitig aktiv. Freischaltung ab Stufe 30 in drei Schritten zu je 10 Heldenpunkten
    /// (1 Linie, 2 Waffe, 3 Skilltyp). Bringt eine Waffe, eine Mechanik-Variante, einen Skilltyp (1 Heil, 3 Hilfe, 1 Elite) und eine Signatur.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Elite Specialization")]
    public class EliteSpecDefinition : SpecializationDefinition
    {
        public const int RequiredLevel = 30, StepCost = 10, Steps = 3;
        public const int StepLine = 1, StepWeapon = 2, StepSkills = 3;

        public string ClassId;
        public EliteMechanic Mechanic;
        public string MechanicLabel;                        // F5- bzw. geänderte F-Taste
        [TextArea] public string MechanicSummary;
        public WeaponSet Weapon;
        public UtilityCategory SkillType;
        public List<SkillDefinition> TypeSkills = new List<SkillDefinition>();      // Heil, 3 Hilfe, Elite
        public List<SkillDefinition> MechanicSkills = new List<SkillDefinition>();  // Avatar-, Überlade-, Kombi-, Schleier-Skills …
        public VfxPalette Palette;
        public Color Accent = Color.white;
        public string SoundMotif;                            // Sound-Familie (= Palette)
        public string CastPose = "Cast2H";                   // Zauber-Pose der Signatur (Clip)
        public string SignatureSkill;

        public SkillDefinition Find(string id)
        {
            foreach (var s in AllSkills()) if (s != null && (s.Id == id || s.name == "Skill_" + id)) return s;
            return null;
        }

        public IEnumerable<SkillDefinition> AllSkills()
        {
            if (Weapon != null)
            {
                foreach (var s in Weapon.Skills) if (s != null) { yield return s; if (s.Chain != null) foreach (var c in s.Chain) if (c != null) yield return c; }
                if (Weapon.Attunement != null) foreach (var s in Weapon.Attunement) if (s != null && System.Array.IndexOf(Weapon.Skills, s) < 0) yield return s;
                if (Weapon.StealthSkill != null) yield return Weapon.StealthSkill;
            }
            foreach (var s in TypeSkills) if (s != null) yield return s;
            foreach (var s in MechanicSkills) if (s != null) yield return s;
        }
    }
}
