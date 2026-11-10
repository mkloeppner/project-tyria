using UnityEngine;

namespace ProjectTyrant
{
    public enum WeaponVisual { Greatsword, Staff, Longbow, AxeTorch, SwordWarhorn, ScepterFocus, DaggerDagger, SwordPistol, Shortbow, Conjured, Hammer, Rifle, DaggerWarhorn, ScepterWarhorn, SwordFocus, ScepterDagger, ScepterPistol, AxeDagger, SwordShield, ScepterTorch, ElementStaff }

    [CreateAssetMenu(menuName = "Project Tyrant/Weapon Set", fileName = "Weapon_")]
    public class WeaponSet : ScriptableObject
    {
        public string DisplayName = "Waffe";
        public WeaponVisual Visual = WeaponVisual.Greatsword;
        public Color WeaponColor = Color.white;
        [Tooltip("Slots 1–5")]
        public SkillDefinition[] Skills = new SkillDefinition[5];
        [Header("Lauf 5 / M25")]
        public WeaponKind Kind = WeaponKind.None;
        [Tooltip("Klasse, der dieses Set gehört (leer = Wächter)")] public string ClassId = "";
        [Tooltip("Elementarweber: Waffenskills je Einstimmung (Feuer, Wasser, Luft, Erde × Slots 1–5)")] public SkillDefinition[] Attunement = new SkillDefinition[0];
        [Tooltip("Schattenklinge: Tarnungsangriff für Slot 1")] public SkillDefinition StealthSkill;

        public SkillDefinition SkillFor(int slot, Element e)
        {
            if (e != Element.None && Attunement != null && Attunement.Length >= 20) { var a = Attunement[(int)e * 5 + slot]; if (a != null) return a; }
            return Skills != null && Skills.Length > slot ? Skills[slot] : null;
        }
    }
}
