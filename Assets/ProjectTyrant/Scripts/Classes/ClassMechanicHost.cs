using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M25: Klassenmechanik des Spielers (F1–F4) – Tiergefährte (Wildhüter), Einstimmungen (Elementarweber), Initiative/Stehlen
    /// (Schattenklinge). Reine Regeln in den Modellen (PetModel, AttunementModel, InitiativeModel, StolenSkillModel, StealthModel).
    /// </summary>
    public partial class ClassMechanicHost : MonoBehaviour, IClassMechanic
    {
        public ClassDefinition Class;
        public readonly AttunementModel Attunement = new AttunementModel();
        public readonly InitiativeModel Initiative = new InitiativeModel();
        public readonly StolenSkillModel Steal = new StolenSkillModel();
        public readonly PetModel Pet = new PetModel();
        public EnemyArchetype[] PetArchetypes = new EnemyArchetype[3];   // Wolf, Bär, Falke
        public SkillDefinition[] PetSkills = new SkillDefinition[3];
        public SkillDefinition[] StolenSkills = new SkillDefinition[5];  // Moor, Frost, Glut, Beast, Bandit

        Combatant _self; SkillCaster _caster; TargetingSystem _targeting; StealthController _stealth; OutOfCombatRegen _regen; PlayerController _mover;
        WeaponSet _conjure; float _conjureLeft; bool _showConj;
        readonly Dictionary<string, float> _cds = new Dictionary<string, float>();
        public int MechanicUses { get; private set; }
        public string LastMechanic { get; private set; } = "";

        public ClassMechanicKind Kind => Class != null ? Class.Mechanic : ClassMechanicKind.None;
        public WeaponSet ActiveConjure => _conjureLeft > 0f ? _conjure : null;
        public float ConjureLeft => _conjureLeft;
        bool InCombat => _regen != null && _regen.InCombat;

        void Awake()
        {
            _self = GetComponent<Combatant>(); _caster = GetComponent<SkillCaster>(); _targeting = GetComponent<TargetingSystem>();
            _stealth = GetComponent<StealthController>(); _regen = GetComponent<OutOfCombatRegen>(); _mover = GetComponent<PlayerController>();
            Attunement.Swapped += OnAttuned;
            Pet.Summoned += f => SummonPet();
        }

        /// <summary>Klasse anwenden (vom PlayerProfile beim Laden/Wechsel).</summary>
        public void SetClass(ClassDefinition c)
        {
            if (Class == c && c != null) return;
            DespawnPet();
            Class = c; _conjure = null; _conjureLeft = 0f; Initiative.Reset();
            if (_caster == null) _caster = GetComponent<SkillCaster>();
            if (_caster != null) { _caster.CancelAim(); _caster.CancelCast(); }
            if (_caster != null) _caster.Mechanic = c != null ? this : null;   // M26: auch der Wächter (Elite-Mechanik F5)
            LeaveElite(); Elite = null; EliteSteps = 0; Overload.OnSwap();
            if (Kind == ClassMechanicKind.Pet) SummonPet();
        }

        // ------------------------------------------------------------ IClassMechanic
        public SkillDefinition OverrideSlot(int slot, WeaponSet active)
        {
            if (slot < 0 || slot > 4) return null;
            var el = EliteOverride(slot, active); if (el != null) return el;
            if (_conjureLeft > 0f && _conjure != null && _conjure.Skills.Length > slot) return _conjure.Skills[slot];
            if (slot == 0 && _stealth != null && _stealth.Model.InStealth && active != null && active.StealthSkill != null) return active.StealthSkill;
            if (Kind == ClassMechanicKind.Attunement && active != null) return active.SkillFor(slot, Attunement.Current);
            return null;
        }

        public bool CanCast(SkillDefinition s, out string reason)
        {
            reason = null;
            if (Kind == ClassMechanicKind.Initiative && s.InitiativeCost > 0 && !Initiative.CanSpend(s.InitiativeCost)) { reason = "Nicht genug Initiative"; return false; }
            return true;
        }

        public void OnCast(SkillDefinition s)
        {
            if (Kind == ClassMechanicKind.Initiative && s.InitiativeCost > 0) Initiative.Spend(s.InitiativeCost);
            if (s.StealthAttack && _stealth != null) _stealth.Reveal();
            // Glyphen wirken je Einstimmung anders (Elementarweber)
            if (Kind == ClassMechanicKind.Attunement && s.Category == UtilityCategory.Glyph) GlyphBonus();
            EliteOnCast(s);
        }

        public bool AllowSwap(out string reason)
        {
            reason = null;
            if (Class != null && !Class.SwapInCombat && InCombat) { reason = "Waffenwechsel im Kampf nicht möglich (Einstimmungen nutzen)"; return false; }
            return true;
        }

        // ------------------------------------------------------------ F1–F4
        public void UseMechanic(int f)
        {
            if (_self == null || _self.IsDead || _self.IsDowned) return;
            bool ok = false;
            if (EliteIntercept(f, out ok)) { if (ok && f < 4) { MechanicUses++; LastMechanic = Kind + " F" + (f + 1); } return; }
            switch (Kind)
            {
                case ClassMechanicKind.Attunement: ok = Attune((Element)f); break;
                case ClassMechanicKind.Initiative: if (f == 0) ok = StealOrUse(); break;
                case ClassMechanicKind.Pet: ok = PetCommand(f); break;
            }
            if (ok) { MechanicUses++; LastMechanic = Kind + " F" + (f + 1); }
        }

        public bool Attune(Element e)
        {
            if (!Attunement.CanSwap(e, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); return false; }
            if (_caster != null && _caster.IsCasting) _caster.CancelCast();
            return Attunement.Swap(e);
        }

        void OnAttuned(Element from, Element to)
        {
            if (_self == null) return;
            if (EliteKind == EliteMechanic.Ueberladen) Attunement.AddRecharge(from, Overload.PenaltyOnLeave);
            Overload.OnSwap(); Dual.Swap(to);
            var pal = AttunementModel.PaletteOf(to);
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Aura, pal, VfxSize.M), transform.position, Quaternion.identity, transform, 0.75f);
            GameAudio.Play(VfxIds.Sfx(pal, "Buff"), transform.position);
            // Passives Thema
            switch (to)
            {
                case Element.Fire: _self.Effects.Apply(new EffectApplication(EffectType.Might, 2, 8f), _self); break;
                case Element.Water: DamageCalculator.Heal(_self, _self, 1200f, 0.25f); _self.Effects.Cleanse(1); break;
                case Element.Air: _self.Effects.Apply(new EffectApplication(EffectType.Swiftness, 1, 4f), _self); _self.Effects.Apply(new EffectApplication(EffectType.Fury, 1, 3f), _self); break;
                default: _self.Effects.Apply(new EffectApplication(EffectType.Protection, 1, 3f), _self); break;
            }
            // Einstimmungs-Wechsel erzeugt ein kurzes Combo-Feld (Feuer, Wasser, Blitz)
            var field = AttunementModel.FieldOf(to);
            if (field != ComboFieldType.None && InCombat) ComboFieldZone.Spawn(field, ZoneAnchors.GroundAt(transform.position), 2.5f, 2f, _self, 0f);
            _self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.AttunementSwap, Self = _self, Position = transform.position });
            if (_caster != null) _caster.NotifySlotsChanged();
        }

        void GlyphBonus()
        {
            switch (Attunement.Current)
            {
                case Element.Fire: _self.Effects.Apply(new EffectApplication(EffectType.Might, 3, 10f), _self); break;
                case Element.Water: _self.Effects.Apply(new EffectApplication(EffectType.Regeneration, 1, 6f), _self); break;
                case Element.Air: _self.Effects.Apply(new EffectApplication(EffectType.Fury, 1, 6f), _self); break;
                default: _self.Effects.Apply(new EffectApplication(EffectType.Protection, 1, 4f), _self); break;
            }
        }

        public void Conjure(WeaponSet set, float seconds)
        {
            _conjure = set; _conjureLeft = seconds * (EliteKind == EliteMechanic.Erdbruch ? 1.5f : 1f);   // Glutbinder: Beschwörungen bleiben länger
            if (_caster != null) _caster.NotifySlotsChanged();
        }

        // ------------------------------------------------------------ Schattenklinge: Stehlen
        bool StealOrUse()
        {
            var target = _targeting != null ? _targeting.Current : null;
            if (Steal.Held != null)
            {
                var id = Steal.UseHeld(); var s = FindStolen(id);
                if (s != null && _caster != null) _caster.TryCast(s, false, null, true);
                return true;
            }
            if (target == null || target.IsDead || !_self.IsHostileTo(target)) { GameEventBus.Notify("Kein Ziel", new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); return false; }
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (!Steal.CanSteal(dist, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); return false; }
            var brain = target.GetComponent<NpcBrain>();
            var anim = target.GetComponent<Anim.AnimDriver>();
            string faction = brain != null && brain.Archetype != null ? GameSoundMap.Faction(brain.Archetype.Palette, anim != null && anim.Prefix.Length > 0) : "Bandit";
            Steal.Steal(faction, dist);
            if (EliteKind == EliteMechanic.Bosheit) { Malice.Mark(target.GetHashCode()); SkillFx.Vfx(VfxIds.Compose(VfxShape.Sigil, Elite.Palette, VfxSize.S), target.transform.position, Quaternion.identity, target.transform, 0.9f); }
            // Schattenschritt zum Ziel
            var dest = target.transform.position - (target.transform.position - transform.position).normalized * 1.3f;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Veil, VfxPalette.Schatten, VfxSize.M), transform.position + Vector3.up, Quaternion.identity, null, 0.75f);
            if (_mover != null) _mover.Teleport(ZoneAnchors.GroundAt(dest), Quaternion.LookRotation(target.transform.position - dest));
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Veil, VfxPalette.Schatten, VfxSize.M), transform.position + Vector3.up, Quaternion.identity, null, 0.75f);
            GameAudio.Play(VfxIds.Sfx(VfxPalette.Schatten, "Release"), transform.position);
            GameEventBus.Notify("Gestohlen: " + (FindStolen(Steal.Held) != null ? FindStolen(Steal.Held).DisplayName : Steal.Held), new Color(0.75f, 0.6f, 1f));
            Initiative.Gain(2f);
            return true;
        }

        public SkillDefinition FindStolen(string id) { if (id == null) return null; foreach (var s in StolenSkills) if (s != null && s.Id == id) return s; return null; }

        void Update()
        {
            float dt = Time.deltaTime;
            Attunement.Tick(dt); Initiative.Tick(dt); Steal.Tick(dt); Pet.Tick(dt);
            if (_conjureLeft > 0f) { _conjureLeft -= dt; if (_conjureLeft <= 0f && _caster != null) _caster.NotifySlotsChanged(); }
            // Beschworene Waffe sichtbar in der Hand
            bool conj = _conjureLeft > 0f;
            if (conj != _showConj) { _showConj = conj; var vis = GetComponent<PlayerVisuals>(); if (vis != null) vis.ShowWeapon(conj ? WeaponVisual.Conjured : (_caster != null && _caster.ActiveSet != null ? _caster.ActiveSet.Visual : WeaponVisual.Greatsword)); }
            var keys = new List<string>(_cds.Keys); foreach (var k in keys) _cds[k] = Mathf.Max(0f, _cds[k] - dt);
            var input = InputManager.Instance;
            if (input != null && !InputManager.TextInputActive && input.MechanicPressed >= 0) UseMechanic(input.MechanicPressed);
            if (Kind == ClassMechanicKind.Pet) TickPet(dt);
            TickElite(dt);
        }

        // ------------------------------------------------------------ HUD-Daten
        public struct MechButton { public string Label; public float Cooldown, CooldownMax; public bool Active, Available; public Color Color; }

        public MechButton Button(int f)
        {
            if (f == 4) return EliteButton();
            var b = new MechButton { Label = Class != null && Class.MechanicLabels != null && Class.MechanicLabels.Length > f ? Class.MechanicLabels[f] : "", Available = true, Color = Class != null ? Class.Color : Color.white };
            switch (Kind)
            {
                case ClassMechanicKind.Attunement:
                    var e = (Element)f; b.Active = Attunement.Current == e; b.Cooldown = Attunement.CooldownOf(e); b.CooldownMax = AttunementModel.Recharge;
                    b.Color = SkillFx.PaletteColor(AttunementModel.PaletteOf(e)); break;
                case ClassMechanicKind.Initiative:
                    if (f == 0) { var held = FindStolen(Steal.Held); b.Label = held != null ? held.DisplayName : "Stehlen"; b.Cooldown = Steal.Cooldown; b.CooldownMax = StolenSkillModel.StealRecharge; b.Active = held != null; }
                    else b.Available = false;
                    break;
                case ClassMechanicKind.Pet:
                    b.Active = f == 2 && Pet.Passive;
                    if (f == 3) { b.Cooldown = Pet.SwapRemaining; b.CooldownMax = PetModel.SwapCooldown; b.Label = "→ " + PetModel.DisplayName(Pet.Active[1 - Pet.ActiveIndex]); }
                    if (f == 1) { var sk = PetSkills[(int)Pet.Current]; float cd = sk != null && _cds.TryGetValue(sk.Id, out var c) ? c : 0f; b.Cooldown = cd; b.CooldownMax = sk != null ? sk.Cooldown : 1f; b.Label = sk != null ? sk.DisplayName : "Familie"; }
                    if (Pet.Dead && f != 3) { b.Available = false; }
                    break;
                default: b.Available = false; break;
            }
            return b;
        }

        public string ResourceText
        {
            get
            {
                switch (Kind)
                {
                    case ClassMechanicKind.Initiative: return "Initiative " + Initiative.Points + "/" + InitiativeModel.Max;
                    case ClassMechanicKind.Attunement: return "Einstimmung: " + (Attunement.Current == Element.Fire ? "Feuer" : Attunement.Current == Element.Water ? "Wasser" : Attunement.Current == Element.Air ? "Luft" : "Erde") + (ActiveConjure != null ? " · " + ActiveConjure.DisplayName + " " + Mathf.CeilToInt(_conjureLeft) + " s" : "");
                    case ClassMechanicKind.Pet: return PetModel.DisplayName(Pet.Current) + (Pet.Dead ? " – besiegt, kehrt in " + Mathf.CeilToInt(Pet.ReviveRemaining) + " s zurück" : Pet.Passive ? " (passiv)" : "");
                    default: return EliteResourceText;
                }
            }
        }

        /// <summary>Ressourcenzeile inklusive Elite-Zusatz (HUD).</summary>
        public string FullResourceText { get { var a = ResourceText; var b = Kind == ClassMechanicKind.None ? "" : EliteResourceText; return string.IsNullOrEmpty(b) ? a : string.IsNullOrEmpty(a) ? b : a + " · " + b; } }
    }
}
