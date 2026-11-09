using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M26: Mechanik-Varianten der 16 Elite-Spezialisierungen (F5 bzw. veränderte F1–F4). Regeln in den reinen Modellen
    /// (HainformModel, MaliceModel, OverloadModel, DualAttunementModel, ShroudModel, EnergyModel, AxeRecallModel); hier nur Verdrahtung.
    /// </summary>
    public partial class ClassMechanicHost
    {
        public EliteSpecDefinition Elite { get; private set; }
        public int EliteSteps { get; private set; }
        public EliteMechanic EliteKind => Elite != null ? Elite.Mechanic : EliteMechanic.None;

        public readonly HainformModel Hainform = new HainformModel();
        public readonly MaliceModel Malice = new MaliceModel();
        public readonly OverloadModel Overload = new OverloadModel();
        public readonly DualAttunementModel Dual = new DualAttunementModel();
        public readonly ShroudModel Shroud = new ShroudModel();
        public readonly EnergyModel Energy = new EnergyModel();
        public readonly AxeRecallModel Axe = new AxeRecallModel();

        bool _merged, _unleashedPlayer, _markCharged, _hooked; int _dodgeChoice;
        float _eliteCd, _eliteCdMax = 1f, _stormLeft, _thornLeft, _fortLeft, _procCd, _thornTick, _judgeAt = -1f; Vector3 _judgePos;
        Combatant _judgeTarget;
        System.Func<float, float> _prevAbsorb; System.Func<DamageInfo, float> _prevIncoming;
        const string EliteSource = "elite";

        public bool Merged => _merged;
        public bool UnleashedPlayer => _unleashedPlayer;
        public int DodgeChoice => _dodgeChoice;

        /// <summary>Spezialisierung anwenden (vom Profil): Zustände zurücksetzen, Gefährte/Ausdauer/Modifikatoren anpassen.</summary>
        public void SetElite(EliteSpecDefinition spec, int steps)
        {
            if (Elite == spec && EliteSteps == steps) return;
            LeaveElite();
            Elite = spec; EliteSteps = steps;
            if (_caster == null) _caster = GetComponent<SkillCaster>();
            if (_caster != null) _caster.Mechanic = Class != null ? this : null;
            if (_mover != null) { _mover.Dodge.Endurance.Max = EliteKind == EliteMechanic.Phantomtanz ? 150f : 100f; _mover.Dodge.Endurance.Fill(); }
            if (!_hooked) { GameEventBus.Healed += OnAnyHealed; GameEventBus.DamageDealt += OnAnyDamage; GameEventBus.DodgePerformed += OnDodge; _hooked = true; }
            if (_caster != null) _caster.NotifySlotsChanged();
        }

        void LeaveElite()
        {
            if (Hainform.Active) Hainform.Exit();
            if (Shroud.Active) ExitShroud();
            if (_merged) { _merged = false; if (Kind == ClassMechanicKind.Pet) SummonPet(); }
            _unleashedPlayer = false; _markCharged = false; _stormLeft = _thornLeft = _fortLeft = 0f; _judgeAt = -1f;
            Malice.ClearMark(); if (Malice.Kneeling) Malice.ToggleKneel();
            if (_self != null) { _self.Modifiers.RemoveSource(EliteSource); if (_fortLeft <= 0f && _self.IncomingMultiplier == FortressMultiplier) _self.IncomingMultiplier = _prevIncoming; }
        }

        void OnDestroyElite() { if (_hooked) { GameEventBus.Healed -= OnAnyHealed; GameEventBus.DamageDealt -= OnAnyDamage; GameEventBus.DodgePerformed -= OnDodge; _hooked = false; } }

        SkillDefinition MS(string key)
        {
            if (Elite == null) return null;
            foreach (var s in Elite.MechanicSkills) if (s != null && s.Id.EndsWith("_" + key)) return s;
            return null;
        }

        // ------------------------------------------------------------ Slot-Überschreibungen (vor Beschwörung/Tarnung/Einstimmung)
        SkillDefinition EliteOverride(int slot, WeaponSet active)
        {
            if (Elite == null) return null;
            if (Hainform.Active) return MS("HF" + (slot + 1));
            if (Shroud.Active) return MS("SS" + (slot + 1));
            switch (EliteKind)
            {
                case EliteMechanic.Entfesseln: if (_unleashedPlayer && slot <= 1) return MS(slot == 0 ? "DW_Hieb" : "DW_Sprung"); break;
                case EliteMechanic.Markieren: if (_markCharged && slot == 0) return MS("SF_Praezision"); break;
                case EliteMechanic.Bosheit: if (slot == 4 && Malice.Malice >= MaliceModel.Max) return MS("WL_Todesschuss"); break;
                case EliteMechanic.DoppelEinstimmung:
                    if (slot == 2 && Kind == ClassMechanicKind.Attunement && Dual.IsDual && Dual.Primary == Attunement.Current && _conjureLeft <= 0f) return MS("KO_" + Dual.Combo); break;
            }
            return null;
        }

        void EliteOnCast(SkillDefinition s)
        {
            if (Elite == null || s == null) return;
            switch (EliteKind)
            {
                case EliteMechanic.Markieren: if (_markCharged && s == MS("SF_Praezision")) { _markCharged = false; _caster.NotifySlotsChanged(); } break;
                case EliteMechanic.Bosheit:
                    if (s == MS("WL_Todesschuss")) { float k = Malice.Finish(); AddTimedBonus(k - 1f, 1.5f); _caster.NotifySlotsChanged(); }
                    break;
                case EliteMechanic.Rueckruf: if (s.Weapon == WeaponKind.AxeDagger && s.Delivery == SkillDelivery.Projectile) Axe.Throw(); break;
                case EliteMechanic.Sturmzorn: if (_stormLeft > 0f && _procCd <= 0f && s != MS("SH_Blitz")) { _procCd = 1f; CastAtTarget(MS("SH_Blitz")); } break;
                case EliteMechanic.Schattenschleier: if (s.InitiativeCost > 0) Shroud.Gain(3f * s.InitiativeCost); break;
            }
            // Skilltypen mit Zusatzwirkung über die Mechanik
            var tgt = _targeting != null ? _targeting.Current : null;
            if (s.Category == UtilityCategory.Unleash && _pet != null && _pet.Self != null && !_pet.Self.IsDead && HostileTarget(tgt))
            {   // Entfesselungs-Ruf: der Gefährte setzt Kontrolle ein
                _pet.ForceAggro(tgt); tgt.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), _pet.Self); tgt.Effects.Apply(new EffectApplication(EffectType.Cripple, 1, 3f), _pet.Self);
                SkillFx.Vfx(VfxIds.Compose(VfxShape.Impact, Elite.Palette, VfxSize.S), tgt.transform.position + Vector3.up, Quaternion.identity, null, 0.9f);
            }
            if (s.Category == UtilityCategory.Glyph && EliteKind == EliteMechanic.Hainform && Hainform.Active)
                foreach (var c in Combatant.All) if (c != null && !c.IsDead && (c == _self || c.Team == Team.Ally) && (c.transform.position - transform.position).sqrMagnitude < 100f) c.Effects.Apply(new EffectApplication(EffectType.Regeneration, 1, 6f), _self);
        }

        // ------------------------------------------------------------ F5 und veränderte F-Tasten
        /// <summary>true = die Taste wurde von der Spezialisierung übernommen.</summary>
        bool EliteIntercept(int f, out bool ok)
        {
            ok = false;
            if (Elite == null) return false;
            if (f == 4) { ok = UseElite(); return true; }
            switch (EliteKind)
            {
                case EliteMechanic.Verschmelzen:
                    if (!_merged) return false;
                    if (f == 3) { GameEventBus.Notify("Im Verschmelzen kein Gefährtenwechsel", new Color(1f, 0.85f, 0.4f)); return true; }
                    if (f == 2) { ok = CastAtTarget(MS("BB_Raserei")); return true; }
                    { var s = PetSkills[(int)(f == 0 ? Pet.Current : Pet.Active[1 - Pet.ActiveIndex])]; ok = CastAtTarget(s); }
                    return true;
                case EliteMechanic.Ueberladen:
                    if (Kind == ClassMechanicKind.Attunement && f == (int)Attunement.Current) { ok = TryOverload(); return true; }
                    return false;
                case EliteMechanic.Rueckruf:
                    if (f == 0 && Axe.Thrown) { ok = Recall(); return true; }
                    return false;
                case EliteMechanic.Schattenschleier:
                    if (f == 1) { ok = ToggleShroud(); return true; }
                    return false;
            }
            return false;
        }

        bool UseElite()
        {
            if (_eliteCd > 0f && EliteKind != EliteMechanic.Hainform && EliteKind != EliteMechanic.Schattenschleier && EliteKind != EliteMechanic.Elementarkern)
            { GameEventBus.Notify(Elite.MechanicLabel + " lädt noch", new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); return false; }
            bool ok = false; var target = _targeting != null ? _targeting.Current : null;
            switch (EliteKind)
            {
                case EliteMechanic.Hainform:
                    if (Hainform.Active) { Hainform.Exit(); ok = true; }
                    else if (Hainform.Enter()) { ok = true; Burst(VfxShape.Pillar, VfxSize.L); }
                    else Fail("Hainform-Leiste nicht voll (" + Mathf.FloorToInt(Hainform.Gauge) + " %)");
                    break;
                case EliteMechanic.Verschmelzen:
                    _merged = !_merged;
                    if (_merged) { DespawnPet(); AddMod(0f, Attr.Power, 150f); AddMod(0f, Attr.Precision, 150f); AddMod(0f, Attr.Ferocity, 100f); }
                    else { _self.Modifiers.RemoveSource(EliteSource); SummonPet(); }
                    _self.Stats.MarkDirty(); Burst(VfxShape.Veil, VfxSize.M); StartCd(5f); ok = true;
                    break;
                case EliteMechanic.Entfesseln:
                    _unleashedPlayer = !_unleashedPlayer;
                    _self.Modifiers.RemoveSource(EliteSource);
                    if (_unleashedPlayer) AddMod(0.10f, Attr.Power, 0f);
                    else if (_pet != null && _pet.Self != null) { _pet.Self.Effects.Apply(new EffectApplication(EffectType.Might, 10, 12f), _pet.Self); _pet.Self.Effects.Apply(new EffectApplication(EffectType.Fury, 1, 12f), _pet.Self); }
                    Burst(VfxShape.Shockwave, VfxSize.M); StartCd(1f); ok = true;
                    break;
                case EliteMechanic.Markieren:
                    if (!HostileTarget(target)) { Fail("Kein Ziel"); break; }
                    target.Effects.Apply(new EffectApplication(EffectType.Vulnerability, 5, 8f), _self);
                    if (_pet != null && !Pet.Passive) _pet.ForceAggro(target);
                    CastAtTarget(MS("SF_Markierung"));
                    _markCharged = true; StartCd(15f); ok = true;
                    break;
                case EliteMechanic.Ueberladen: ok = TryOverload(); break;
                case EliteMechanic.DoppelEinstimmung: Fail("Doppel-Einstimmung wirkt über F1–F4"); break;
                case EliteMechanic.Elementarkern:
                    if (Energy.Sphere()) { ok = CastAtTarget(MS("KE_" + (int)Attunement.Current)); }
                    else Fail(Energy.Cooldown > 0f ? "Kern lädt noch" : "Zu wenig Energie (" + Mathf.FloorToInt(Energy.Energy) + "/" + (int)EnergyModel.SphereCost + ")");
                    break;
                case EliteMechanic.Erdbruch: ok = CastAtTarget(MS("EB_Lava")); if (ok) StartCd(20f); break;
                case EliteMechanic.Bosheit:
                    Malice.ToggleKneel(); _self.Modifiers.RemoveSource(EliteSource);
                    if (Malice.Kneeling) AddMod(0.10f, Attr.Precision, 150f);
                    Burst(VfxShape.Sigil, VfxSize.S); StartCd(0.5f); ok = true;
                    break;
                case EliteMechanic.Phantomtanz:
                    _dodgeChoice = (_dodgeChoice + 1) % 3; var ds = MS("PT_" + _dodgeChoice);
                    GameEventBus.Notify("Ausweich-Fertigkeit: " + (ds != null ? ds.DisplayName : "?"), Elite.Accent); StartCd(0.5f); ok = true;
                    break;
                case EliteMechanic.Schattenschleier: ok = ToggleShroud(); break;
                case EliteMechanic.Rueckruf: ok = Axe.Thrown ? Recall() : false; if (!ok) Fail("Keine Axt im Flug"); break;
                case EliteMechanic.Urteil:
                    if (!HostileTarget(target)) { Fail("Kein Ziel"); break; }
                    _judgeTarget = target; _judgePos = target.transform.position; _judgeAt = 2.5f;
                    SkillFx.Vfx(VfxIds.Compose(VfxShape.Sigil, Elite.Palette, VfxSize.M), target.transform.position, Quaternion.identity, target.transform, 0.9f);
                    target.Effects.Apply(new EffectApplication(EffectType.Burning, 2, 3f), _self); StartCd(20f); ok = true;
                    break;
                case EliteMechanic.Festung:
                    _fortLeft = 10f; _prevIncoming = _self.IncomingMultiplier; _self.IncomingMultiplier = FortressMultiplier;
                    foreach (var c in Combatant.All) if (c != null && !c.IsDead && (c == _self || c.Team == Team.Ally) && (c.transform.position - transform.position).sqrMagnitude < 64f) c.Effects.Apply(new EffectApplication(EffectType.Protection, 1, 10f), _self);
                    Burst(VfxShape.Aura, VfxSize.L); CastAtTarget(MS("BM_Festung")); StartCd(30f); ok = true;
                    break;
                case EliteMechanic.Sturmzorn:
                    _stormLeft = 10f;
                    foreach (var c in Combatant.All) if (c != null && !c.IsDead && (c == _self || c.Team == Team.Ally) && (c.transform.position - transform.position).sqrMagnitude < 100f) c.Effects.Apply(new EffectApplication(EffectType.Quickness, 1, 5f), _self);
                    Burst(VfxShape.Pillar, VfxSize.M); StartCd(25f); ok = true;
                    break;
                case EliteMechanic.Dornenhaut:
                    _thornLeft = 8f; _self.Effects.Apply(new EffectApplication(EffectType.Resolution, 1, 8f), _self);
                    Burst(VfxShape.Aura, VfxSize.M); CastAtTarget(MS("DS_Dornenhaut")); StartCd(30f); ok = true;
                    break;
            }
            if (ok)
            {
                MechanicUses++; LastMechanic = Elite.DisplayName + " F5";
                _self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.EliteMechanic, Self = _self, Position = transform.position });
                GameAudio.Play(VfxIds.Sfx(Elite.Palette, "Buff"), transform.position);
                if (_caster != null) _caster.NotifySlotsChanged();
            }
            return ok;
        }

        void Fail(string why) { GameEventBus.Notify(why, new Color(1f, 0.85f, 0.4f)); GameAudio.Ui("Error"); }
        void StartCd(float s) { _eliteCd = s; _eliteCdMax = s; }
        static bool HostileTarget(Combatant t) => t != null && !t.IsDead && t.Team == Team.Enemy;
        void Burst(VfxShape shape, VfxSize size) => SkillFx.Vfx(VfxIds.Compose(shape, Elite.Palette, size), transform.position, Quaternion.identity, transform, 0.9f);

        bool CastAtTarget(SkillDefinition s)
        {
            if (s == null || _caster == null) return false;
            var t = _targeting != null ? _targeting.Current : null;
            Vector3? ground = s.IsGroundTargeted ? (t != null ? t.transform.position : transform.position + transform.forward * 6f) : (Vector3?)null;
            return _caster.TryCast(s, false, ground, true);
        }

        void AddMod(float damage, Attr a, float value) => _self.Modifiers.Add(new EliteModifier { SourceId = EliteSource, Damage = damage, A = a, Value = value });
        void AddTimedBonus(float damage, float seconds) { if (damage <= 0f) return; _self.Modifiers.Add(new EliteModifier { SourceId = "elite_finisher", Damage = damage }); _finisherLeft = seconds; }
        float _finisherLeft;

        float FortressMultiplier(DamageInfo d)
        {
            float prev = _prevIncoming != null ? _prevIncoming(d) : 1f;
            if (d.Skill != null && d.Skill.Delivery == SkillDelivery.Projectile) return 0f;   // Projektilschutz
            return prev * 0.67f;                                                               // Umleitung: ein Drittel trägt die Kuppel
        }

        bool TryOverload()
        {
            if (!Overload.Ready) { Fail(Overload.Used ? "Bereits überladen" : "Überladen ab " + (int)OverloadModel.Hold + " s in der Einstimmung (" + Overload.TimeInAttunement.ToString("0.0") + " s)"); return false; }
            var s = MS("UL_" + (int)Attunement.Current); if (s == null) return false;
            Overload.Use();
            return _caster.TryCast(s, false, null, true);
        }

        bool Recall()
        {
            if (!Axe.Recall()) return false;
            var s = MS("KW_Rueckruf"); CastAtTarget(s);
            Initiative.Gain(AxeRecallModel.Refund);
            return true;
        }

        bool ToggleShroud()
        {
            if (Shroud.Active) { ExitShroud(); return true; }
            if (!Shroud.Enter(_self.MaxHealth)) { Fail(Shroud.Cooldown > 0f ? "Schleier lädt noch" : "Zu wenig Schattenkraft (" + Mathf.FloorToInt(Shroud.Force) + "/" + (int)ShroudModel.EnterMin + ")"); return false; }
            _prevAbsorb = _self.AbsorbDamage; _self.AbsorbDamage = ShroudAbsorb;
            Burst(VfxShape.Veil, VfxSize.L);
            return true;
        }

        void ExitShroud()
        {
            Shroud.Exit();
            if (_self != null && _self.AbsorbDamage == ShroudAbsorb) _self.AbsorbDamage = _prevAbsorb;
            if (_caster != null) _caster.NotifySlotsChanged();
        }

        float ShroudAbsorb(float dmg)
        {
            float rest = Shroud.Absorb(dmg);
            if (!Shroud.Active) { if (_self.AbsorbDamage == ShroudAbsorb) _self.AbsorbDamage = _prevAbsorb; if (_caster != null) _caster.NotifySlotsChanged(); }
            return _prevAbsorb != null && rest > 0f ? _prevAbsorb(rest) : rest;
        }

        // ------------------------------------------------------------ Ereignisse
        void OnAnyHealed(Combatant who, float amount)
        {
            if (EliteKind != EliteMechanic.Hainform || who == null || _self == null) return;
            if (who == _self || (who.Team == Team.Ally && (who.transform.position - transform.position).sqrMagnitude < 225f)) Hainform.AddHealing(amount, _self.MaxHealth);
        }

        void OnAnyDamage(DamageInfo d)
        {
            if (Elite == null || _self == null) return;
            bool mine = d.Source == _self || (_pet != null && d.Source == _pet.Self);
            if (mine && d.Target != null && d.Amount > 0f)
            {
                switch (EliteKind)
                {
                    case EliteMechanic.Bosheit: if (Malice.OnHit(d.Target.GetHashCode(), Time.time) && Malice.Malice == MaliceModel.Max && _caster != null) _caster.NotifySlotsChanged(); break;
                    case EliteMechanic.Elementarkern: Energy.OnHit(Time.time); break;
                    case EliteMechanic.Schattenschleier: Shroud.Gain(2f); break;
                }
            }
            if (d.Target == _self && d.Source != null && _thornLeft > 0f && !d.IsCondition && _procCd <= 0f)
            {   // Vergeltung
                _procCd = 0.5f; DamageCalculator.DealDirect(_self, d.Source, 0.25f, null, d.Source.AimPoint);
            }
        }

        void OnDodge(Combatant who)
        {
            if (who != _self || EliteKind != EliteMechanic.Phantomtanz) return;
            var s = MS("PT_" + _dodgeChoice); if (s == null || _caster == null) return;
            var t = _targeting != null ? _targeting.Current : null;
            var ctx = new SkillContext { Caster = _self, Skill = s, Target = t != null && !t.IsDead && _self.IsHostileTo(t) ? t : null, GroundPoint = t != null ? t.transform.position : transform.position, Forward = transform.forward, Runner = this };
            SkillExecutor.Execute(ctx);
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Shockwave, Elite.Palette, VfxSize.S), transform.position, Quaternion.identity, null, 0.75f);
            GameAudio.Play(VfxIds.Sfx(Elite.Palette, "Impact"), transform.position);
        }

        void TickElite(float dt)
        {
            if (Elite == null) return;
            _eliteCd = Mathf.Max(0f, _eliteCd - dt); _procCd = Mathf.Max(0f, _procCd - dt);
            if (_finisherLeft > 0f) { _finisherLeft -= dt; if (_finisherLeft <= 0f) _self.Modifiers.RemoveSource("elite_finisher"); }
            bool wasAvatar = Hainform.Active, wasShroud = Shroud.Active;
            Hainform.Tick(dt, InCombat); Energy.Tick(dt); Axe.Tick(dt); Overload.Tick(dt);
            Shroud.Tick(dt); if (wasShroud && !Shroud.Active && _self.AbsorbDamage == ShroudAbsorb) _self.AbsorbDamage = _prevAbsorb;
            if (wasAvatar != Hainform.Active || wasShroud != Shroud.Active) if (_caster != null) _caster.NotifySlotsChanged();
            if (Malice.Kneeling) _self.Effects.Apply(new EffectApplication(EffectType.Immobilize, 1, 0.3f), _self);
            if (_stormLeft > 0f) _stormLeft -= dt;
            if (_fortLeft > 0f) { _fortLeft -= dt; if (_fortLeft <= 0f && _self.IncomingMultiplier == FortressMultiplier) _self.IncomingMultiplier = _prevIncoming; }
            if (_thornLeft > 0f)
            {
                _thornLeft -= dt; _thornTick -= dt;
                if (_thornTick <= 0f)
                {   // Zustände zurückwerfen: 1 Zustand je Sekunde auf den nächsten Angreifer
                    _thornTick = 1f;
                    if (_self.Effects.Cleanse(1) > 0) { var a = Combatant.FindNearestHostile(_self, transform.position, 12f); if (a != null) a.Effects.Apply(new EffectApplication(EffectType.Burning, 1, 3f), _self); }
                }
            }
            if (_judgeAt > 0f)
            {
                _judgeAt -= dt;
                if (_judgeAt <= 0f) { _judgeAt = -1f; var s = MS("GR_Urteil"); var pos = _judgeTarget != null && !_judgeTarget.IsDead ? _judgeTarget.transform.position : _judgePos; if (s != null && _caster != null) _caster.TryCast(s, true, pos, true); }
            }
        }

        // ------------------------------------------------------------ HUD
        public MechButton EliteButton()
        {
            var b = new MechButton { Label = Elite != null ? Elite.MechanicLabel : "", Color = Elite != null ? Elite.Accent : Color.white, Available = Elite != null && EliteKind != EliteMechanic.DoppelEinstimmung };
            if (Elite == null) return b;
            b.Cooldown = _eliteCd; b.CooldownMax = _eliteCdMax;
            switch (EliteKind)
            {
                case EliteMechanic.Hainform: b.Active = Hainform.Active; b.Cooldown = Hainform.Cooldown; b.CooldownMax = HainformModel.Recharge; b.Available = Hainform.Active || Hainform.CanEnter; break;
                case EliteMechanic.Verschmelzen: b.Active = _merged; break;
                case EliteMechanic.Entfesseln: b.Active = _unleashedPlayer; b.Label = _unleashedPlayer ? "Selbst" : "Gefährte"; break;
                case EliteMechanic.Markieren: b.Active = _markCharged; break;
                case EliteMechanic.Ueberladen: b.Active = Overload.Ready; b.Available = Overload.Ready; break;
                case EliteMechanic.Elementarkern: b.Cooldown = Energy.Cooldown; b.CooldownMax = EnergyModel.SphereRecharge; b.Available = Energy.CanSphere; break;
                case EliteMechanic.Bosheit: b.Active = Malice.Kneeling; b.Label = Malice.Kneeling ? "Aufstehen" : Elite.MechanicLabel; break;
                case EliteMechanic.Phantomtanz: var ds = MS("PT_" + _dodgeChoice); b.Label = ds != null ? ds.ShortLabel : "Tanz"; break;
                case EliteMechanic.Schattenschleier: b.Active = Shroud.Active; b.Cooldown = Shroud.Cooldown; b.CooldownMax = ShroudModel.Recharge; b.Available = Shroud.Active || Shroud.CanEnter; break;
                case EliteMechanic.Rueckruf: b.Available = Axe.Thrown; b.Active = Axe.Thrown; break;
                case EliteMechanic.Festung: b.Active = _fortLeft > 0f; break;
                case EliteMechanic.Sturmzorn: b.Active = _stormLeft > 0f; break;
                case EliteMechanic.Dornenhaut: b.Active = _thornLeft > 0f; break;
            }
            return b;
        }

        public string EliteResourceText
        {
            get
            {
                switch (EliteKind)
                {
                    case EliteMechanic.Hainform: return Hainform.Active ? "Hainform " + Mathf.CeilToInt(Hainform.Remaining) + " s" : "Hain " + Mathf.FloorToInt(Hainform.Gauge) + " %";
                    case EliteMechanic.Verschmelzen: return _merged ? "verschmolzen" : "";
                    case EliteMechanic.Entfesseln: return _unleashedPlayer ? "entfesselt: du" : "entfesselt: Gefährte";
                    case EliteMechanic.Markieren: return _markCharged ? "Präzisionsschuss bereit" : "";
                    case EliteMechanic.Ueberladen: return Overload.Ready ? "Überladen bereit" : "";
                    case EliteMechanic.DoppelEinstimmung: return Dual.IsDual ? "Doppel: " + Name(Dual.Primary) + " + " + Name(Dual.Secondary) : "";
                    case EliteMechanic.Elementarkern: return "Energie " + Mathf.FloorToInt(Energy.Energy) + "/" + (int)EnergyModel.Max;
                    case EliteMechanic.Bosheit: return "Bosheit " + Malice.Malice + "/" + MaliceModel.Max + (Malice.Kneeling ? " · kniend" : "");
                    case EliteMechanic.Phantomtanz: return "Ausdauer " + Mathf.FloorToInt(_mover != null ? _mover.Dodge.Endurance.Current : 0f) + "/150";
                    case EliteMechanic.Schattenschleier: return Shroud.Active ? "Schleier " + Mathf.CeilToInt(Shroud.ShroudHealth) : "Schattenkraft " + Mathf.FloorToInt(Shroud.Force) + " %";
                    case EliteMechanic.Rueckruf: return Axe.Thrown ? "Axt im Flug " + Axe.Remaining.ToString("0.0") + " s" : "";
                    default: return "";
                }
            }
        }

        static string Name(Element e) => e == Element.Fire ? "Feuer" : e == Element.Water ? "Wasser" : e == Element.Air ? "Luft" : "Erde";
    }

    /// <summary>Lauf 5 / M26: Modifikator der Elite-Mechanik (Schadensbonus und/oder Attributbonus).</summary>
    public class EliteModifier : CombatModifier
    {
        public float Damage; public Attr A; public float Value;
        public override float OutgoingDamageBonus(Combatant self, Combatant target, SkillDefinition skill) => Damage;
        public override void Contribute(AttributeAccumulator acc, int level) { if (Value != 0f) acc.Flat[(int)A] += Value * AttributeSet.LevelScale(level); }
    }
}
