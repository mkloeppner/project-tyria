using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum SkillDelivery { Melee, Projectile, GroundTarget, Self, Leap }
    public enum ComboFieldType { None, Fire, Water, Lightning, Smoke }
    public enum ComboFinisherType { None, Blast, Leap, Projectile, Whirl }
    public enum SkillSpecial { None, Interrupt, DownedSelfHeal, Bandage, ShadowStep, Blink, Conjure, TrapPlace, PetCommand }
    public enum SkillSlotType { Weapon, Heal, Utility, Elite, Downed }
    public enum UtilityCategory { None, Shout, Meditation, Trap, Signal, Wilderness, Glyph, Conjure, Arcane, Trick, Venom, Deception, Stance, Unleash, Mark, Augment, Cantrip, Physical, Well, Preparation, Mantra, Symbol, Consecration }
    public enum WeaponKind { None, Greatsword, Staff, Longbow, AxeTorch, SwordWarhorn, ScepterFocus, DaggerDagger, SwordPistol, Shortbow, Hammer, Rifle, DaggerWarhorn, ScepterWarhorn, SwordFocus, ScepterDagger, ScepterPistol, AxeDagger, SwordShield, ScepterTorch }

    [CreateAssetMenu(menuName = "Project Tyrant/Skill Definition", fileName = "Skill_")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Anzeige")]
        public string Id;
        public string DisplayName = "Skill";
        [TextArea] public string Description;
        public string ShortLabel = "SK";
        public Color IconColor = Color.white;
        public Sprite Icon;

        [Header("Typ & Freischaltung")]
        public SkillSlotType SlotType = SkillSlotType.Weapon;
        public UtilityCategory Category = UtilityCategory.None;
        [Tooltip("Tier innerhalb der Kategorie (1–3). Tier n setzt Tier n−1 voraus.")]
        public int Tier = 1;
        public int UnlockCost;
        public bool StartsUnlocked;
        public WeaponKind Weapon = WeaponKind.None;

        [Header("Timing")]
        public float CastTime = 0.5f;
        public float Cooldown = 0f;

        [Header("Wirkung")]
        public SkillDelivery Delivery = SkillDelivery.Melee;
        public float Range = 3f;
        public float Radius = 2.5f;
        public int TargetCount = 5;
        [Tooltip("Fix-Lauf F1: drittes Glied einer Auto-Angriffs-Kette (Bonus, Kampftext „Kettenabschluss“)")] public bool ChainBonus;
        [Tooltip("Fix-Lauf F1: erstes Glied der Kette (für Glied 2/3; Tooltip zeigt alle Glieder)")] public SkillDefinition ChainRoot;
        /// <summary>Kettenposition 1..n und Länge (1/1 ohne Kette).</summary>
        public int ChainPosition(out int length)
        {
            var root = ChainRoot != null ? ChainRoot : this;
            length = root.Chain != null ? root.Chain.Length + 1 : 1;
            if (root == this) return 1;
            int i = System.Array.IndexOf(root.Chain, this); return i < 0 ? 1 : i + 2;
        }
        [Tooltip("Schadens-Koeffizient je Treffer (GW2: Waffenstärke × Kraft × Koeffizient / Rüstung).")]
        public float Coefficient = 1f;
        public int Hits = 1;
        public float HitInterval = 0.25f;
        [Tooltip("Basisheilung auf Level 80 (wird level-skaliert).")]
        public float HealBase = 0f;
        public float HealCoefficient = 0f;
        public float ProjectileSpeed = 30f;
        public float LeapDistance = 0f;
        public List<EffectApplication> TargetBuffs = new List<EffectApplication>();
        public List<EffectApplication> SelfBuffs = new List<EffectApplication>();
        [Tooltip("Segen aus SelfBuffs gehen auch an Verbündete im Radius (Rufe).")]
        public bool ShareWithAllies;
        public int CleanseCount;
        public SkillSpecial Special = SkillSpecial.None;
        public bool EvadesDuringCast = false;

        [Header("Combo")]
        public ComboFieldType Field = ComboFieldType.None;
        public float FieldDuration = 5f;
        public float FieldRadius = 4f;
        [Tooltip("Koeffizient je Feld-Puls (Blitzfeld).")]
        public float FieldTickCoefficient = 0f;
        public ComboFinisherType Finisher = ComboFinisherType.None;

        [Header("Lauf 5: Zeitachse, Kette, Effekt-Palette")]
        public SkillTimeline Timeline = new SkillTimeline();
        [Tooltip("Auto-Angriffs-Kette: Glied 2 und 3 (Glied 1 = dieser Skill)")] public SkillDefinition[] Chain = new SkillDefinition[0];
        public VfxPalette Palette = VfxPalette.Licht;
        [Tooltip("Grafik-Lauf G5: VFX 2.0 (Lagen je Phase); leer = Zeitachsen-Kennungen aus Lauf 5")] public Visuals.VfxDefinition Vfx2;
        [Tooltip("VFX-Lauf V1: Hauptelement der Simulations-Lagen (SkillInventoryV1)")] public Visuals.ElementSim.SimElement SimElementV1 = Visuals.ElementSim.SimElement.Keins;
        [Tooltip("VFX-Lauf V1: Sekundär-Beteiligung eines Leuchtturm-Elements (nie Umbenennung)")] public Visuals.ElementSim.SimElement SimSecondaryV1 = Visuals.ElementSim.SimElement.Keins;
        [Tooltip("VFX-Lauf V1: Schaustück – volle Behandlung (≥ 7 Lagen, Simulation, Mesh, Brechung oder Licht, Welt-Nachwirkung)")] public bool Showpiece;
        [Tooltip("Palette fest vorgegeben (sonst aus Feld/Farbe abgeleitet)")] public bool PaletteExplicit;
        [Tooltip("Waffenstil der Animation, wenn Weapon = None (z. B. „Dagger“, „Bow“)")] public string WeaponStyleOverride = "";

        [Header("Lauf 5 / M25: Klassen")]
        [Tooltip("Klasse (leer = Wächter/alle)")] public string ClassId = "";
        [Tooltip("Schattenklinge: Initiative-Kosten (statt Abklingzeit)")] public int InitiativeCost;
        [Tooltip("Elementarweber: Einstimmung dieses Waffenskills")] public Element Element = Element.None;
        [Tooltip("Tarnung für den Zauberer (s)")] public float StealthSeconds;
        [Tooltip("Tarnungsangriff (ersetzt Skill 1 in Tarnung)")] public bool StealthAttack;
        [Tooltip("Beschwörung: ersetzt die Waffenskills 1–5 für 20 s")] public WeaponSet ConjureSet;
        [Tooltip("Falle: Auslöse-Radius / Lebensdauer (s)")] public float TrapLifetime;

        public bool IsGroundTargeted => Delivery == SkillDelivery.GroundTarget;
        public bool IsInstant => Timeline != null && Timeline.IsBuilt ? Timeline.Instant : CastTime <= 0f;
        public bool NeedsHostileTarget => Delivery == SkillDelivery.Projectile || Delivery == SkillDelivery.Leap;
        public bool DealsDamage => Coefficient > 0f;
    }
}
