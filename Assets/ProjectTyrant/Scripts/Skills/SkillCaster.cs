using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Skill-Leiste des Spielers: Slots 1–5 (Waffe), 6 Heal, 7–9 Utility, 0 Elite. Weapon Swap mit 10 s Cooldown.
    /// Im Downed State werden Slots 1–4 durch Downed-Skills ersetzt.
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        public const int SlotCount = 10;

        public WeaponSet[] WeaponSets = new WeaponSet[2];
        public SkillDefinition HealSkill;
        public SkillDefinition[] Utilities = new SkillDefinition[3];
        public SkillDefinition EliteSkill;
        public SkillDefinition[] DownedSkills = new SkillDefinition[4];
        public ComboTable ComboTable;
        public float WeaponSwapCooldown = 10f;
        /// <summary>Lauf 5 / M25: Klassenmechanik (Einstimmung, Initiative, Gefährte) – ersetzt Slots, prüft Kosten.</summary>
        public IClassMechanic Mechanic;
        public event Action SlotsChanged;
        public void NotifySlotsChanged() => SlotsChanged?.Invoke();
        public bool AutoAttack = true;
        /// <summary>Fix-Lauf F1: Bodenziel-Modi (Normal, Drücken und Loslassen, Sofort, Sofort mit Reichweitenanzeige) als Zustandsmaschine.</summary>
        public readonly GroundTargetModel Ground = new GroundTargetModel();
        /// <summary>Alias aus Lauf 3 (Tests/Debug): an = Modus „Sofort“.</summary>
        public bool FastCastGroundTargets
        {
            get => Ground.CurrentMode == GroundTargetModel.Mode.Instant || Ground.CurrentMode == GroundTargetModel.Mode.InstantWithRange;
            set => SetGroundMode(value ? GroundTargetModel.Mode.Instant : GroundTargetModel.Mode.Normal);
        }

        public void SetGroundMode(GroundTargetModel.Mode m) { if (Ground.SetMode(m) == GroundTargetModel.Action.Cancel) CancelAimVisual(); }

        void ApplyGroundOptions(GameOptions o)
        {
            if (o == null) return;
            SetGroundMode((GroundTargetModel.Mode)o.GroundTargetMode);
            Ground.HoldThreshold = o.GroundHoldMs / 1000f; Ground.ClampToRange = o.GroundClampToRange; Ground.SnapToTarget = o.GroundSnapToTarget;
        }
        void OnEnable() { GameOptions.Changed += ApplyGroundOptions; ApplyGroundOptions(GameOptions.Current); GameEventBus.DodgePerformed += OnDodgeChain; }
        void OnDisable() { GameOptions.Changed -= ApplyGroundOptions; GameEventBus.DodgePerformed -= OnDodgeChain; }
        void OnDodgeChain(Combatant c) { if (c != null && c == _self) Chain.Break(); }   // Fix-Lauf F1: Ausweichen setzt die Kette zurück

        public int ActiveSetIndex { get; private set; }
        public WeaponSet ActiveSet => WeaponSets != null && WeaponSets.Length > ActiveSetIndex ? WeaponSets[ActiveSetIndex] : null;
        public float SwapCooldownRemaining { get; private set; }
        public SkillDefinition CastingSkill { get; private set; }
        public float CastRemaining { get; private set; }
        public float CastTotal { get; private set; }
        public float CastProgress => CastTotal <= 0f ? 1f : 1f - Mathf.Clamp01(CastRemaining / CastTotal);
        public bool IsCasting => CastingSkill != null;

        public event Action Swapped;
        /// <summary>Lauf 5: Aktivierung beginnt (Animation/VFX/SFX lesen die Zeitachse).</summary>
        public event Action<SkillDefinition> CastStarted;
        /// <summary>Lauf 5: Wirkung ausgelöst (erster Treffer/Auswurf).</summary>
        public event Action<SkillDefinition> CastReleased;
        /// <summary>Lauf 5: Skill startet nicht – Grund („Kein Ziel“, „Außer Reichweite“, „Keine Sichtlinie“, „Lädt noch“).</summary>
        public event Action<SkillDefinition, string> CastFailed;
        /// <summary>Lauf 5: Aktivierung durch Kontrolle unterbrochen (Skill 5 s gesperrt).</summary>
        public event Action<SkillDefinition> Interrupted;
        public readonly AutoChainModel Chain = new AutoChainModel();
        public readonly SkillQueueModel<SkillDefinition> Queue = new SkillQueueModel<SkillDefinition>();
        bool _busyByAuto;
        float _activationStart, _activationEnd;
        /// <summary>Kanal-Skill in seiner Mehrfach-Treffer-Phase (für den Kanal-Balken), sonst null.</summary>
        public SkillDefinition ChannelSkill { get; private set; }
        public float ChannelProgress => _activationEnd <= _activationStart ? 1f : Mathf.Clamp01((Time.time - _activationStart) / (_activationEnd - _activationStart));
        public SkillDefinition QueuedSkill => Queue.Queued;
        public bool InAftercast => !IsCasting && Queue.IsBusy(Time.time);
        public static int LineOfSightMask = 1;
        public Vector3 LastGroundPoint { get; private set; }

        public SkillDefinition AimingSkill { get; private set; }
        public bool IsAiming => AimingSkill != null;
        public Vector3 AimPoint { get; private set; }
        public bool AimClamped { get; private set; }
        /// <summary>Frame, in dem eine Aim-Eingabe verbraucht wurde (damit Targeting LMB/Esc ignoriert).</summary>
        public int AimInputFrame { get; private set; } = -1;
        int _aimSlot = -1;
        GameObject _aimRing;
        Renderer _aimRenderer;
        MaterialPropertyBlock _aimMpb;

        Combatant _self;
        PlayerController _mover;
        TargetingSystem _targeting;
        DownedController _downed;
        PlayerVisuals _visuals;
        readonly Dictionary<SkillDefinition, float> _cooldowns = new Dictionary<SkillDefinition, float>();
        Combatant _castTarget;
        Vector3 _castPoint;
        bool _autoAttackArmed;
        float _channelTick;
        readonly InvulnWindow _evadeWindow = new InvulnWindow();

        class InvulnWindow : IInvulnerabilitySource { public float Remaining; public bool IsInvulnerable => Remaining > 0f; }

        void Awake() => Init();

        public void Init()
        {
            if (_self != null) return;
            _self = GetComponent<Combatant>();
            _mover = GetComponent<PlayerController>();
            _targeting = GetComponent<TargetingSystem>();
            _downed = GetComponent<DownedController>();
            _visuals = GetComponent<PlayerVisuals>();
            if (_self != null) _self.InvulnerabilitySources.Add(_evadeWindow);
        }

        void Start() { if (_visuals != null && ActiveSet != null) _visuals.ShowWeapon(ActiveSet.Visual); }

        public SkillDefinition GetSkillInSlot(int slot)
        {
            if (_downed != null && _downed.IsDowned)
                return slot >= 0 && slot < 4 && DownedSkills != null && DownedSkills.Length > slot ? DownedSkills[slot] : null;
            if (SlotOverride != null && slot < 5) { var bo = SlotOverride(slot); if (bo != null) return bo; }   // Lauf 6: Bündel
            if (Mechanic != null && slot < 5) { var o = Mechanic.OverrideSlot(slot, ActiveSet); if (o != null) return slot == 0 ? ChainLink(o) : o; }
            if (slot == 0) return ChainLink(ActiveSet != null && ActiveSet.Skills.Length > 0 ? ActiveSet.Skills[0] : null);
            if (slot < 5) return ActiveSet != null && ActiveSet.Skills.Length > slot ? ActiveSet.Skills[slot] : null;
            if (slot == 5) return HealSkill;
            if (slot < 9) return Utilities != null && Utilities.Length > slot - 6 ? Utilities[slot - 6] : null;
            return EliteSkill;
        }

        /// <summary>Aktuelles Kettenglied des Auto-Angriffs (Glied 1 = Basis-Skill).</summary>
        SkillDefinition ChainLink(SkillDefinition b)
        {
            if (b == null || b.Chain == null || b.Chain.Length == 0) return b;
            int i = Chain.Current(Time.time);
            if (i == 0) return b;
            var c = b.Chain[Mathf.Min(i - 1, b.Chain.Length - 1)];
            return c != null ? c : b;
        }

        public bool IsAutoAttack(SkillDefinition s)
        {
            var b = ActiveSet != null && ActiveSet.Skills.Length > 0 ? ActiveSet.Skills[0] : null;
            if (Mechanic != null) { var o = Mechanic.OverrideSlot(0, ActiveSet); if (o != null && !o.StealthAttack) b = o; }
            if (b == null || s == null) return false;
            if (s == b) return true;
            if (b.Chain != null) foreach (var c in b.Chain) if (c == s) return true;
            return false;
        }

        public float GetCooldownRemaining(SkillDefinition s) => s != null && _cooldowns.TryGetValue(s, out var r) ? r : 0f;
        public float GetCooldownFraction(SkillDefinition s) => s == null || s.Cooldown <= 0f ? 0f : Mathf.Clamp01(GetCooldownRemaining(s) / s.Cooldown);

        void Update()
        {
            Tick(Time.deltaTime);
            HandleInput();
        }

        public void Tick(float dt)
        {
            float rate = 1f;
            if (_self != null)
            {
                if (_self.Effects.Has(EffectType.Chill)) rate *= EffectRules.ChillRechargeFactor;
                if (_self.Effects.Has(EffectType.Alacrity)) rate *= EffectRules.AlacrityRechargeFactor;
            }
            if (_cooldowns.Count > 0)
            {
                var keys = new List<SkillDefinition>(_cooldowns.Keys);
                foreach (var k in keys)
                {
                    float v = _cooldowns[k] - dt * rate;
                    if (v <= 0f) _cooldowns.Remove(k); else _cooldowns[k] = v;
                }
            }
            if (SwapCooldownRemaining > 0f) SwapCooldownRemaining = Mathf.Max(0f, SwapCooldownRemaining - dt);
            if (_evadeWindow.Remaining > 0f) _evadeWindow.Remaining -= dt;
            if (IsAiming && (_self == null || _self.IsDead || (_downed != null && _downed.IsDowned))) CancelAim();

            bool dodging = _mover != null && _mover.Dodge.IsDodging;
            if (dodging && Queue.IsBusy(Time.time)) Queue.Cancel(Time.time);
            if (ChannelSkill != null && Time.time >= _activationEnd) ChannelSkill = null;   // Ausweichen bricht Aktivierung, Nachwirkung und Warteschlange
            if (CastingSkill == null && Queue.Queued != null && !Queue.IsBusy(Time.time) && !InputSuppressed)
            {
                var q = Queue.PopReady(Time.time);
                if (q != null) TryCast(q, false);
            }
            if (CastingSkill != null)
            {
                bool control = _self != null && (_self.Effects.Has(EffectType.Stun) || _self.Effects.Has(EffectType.Fear) || _self.Effects.Has(EffectType.Taunt));
                if (control && _self != null && !_self.IsDead) { Interrupt(); return; }
                bool interrupted = _self == null || _self.IsDead || dodging
                    || (CastingSkill.Special != SkillSpecial.Bandage && CastingSkill.Special != SkillSpecial.DownedSelfHeal && _downed != null && _downed.IsDowned && Array.IndexOf(DownedSkills, CastingSkill) < 0);
                if (interrupted) { CancelCast(); Queue.Cancel(Time.time); return; }

                if (CastingSkill.Special == SkillSpecial.Bandage)
                {
                    _channelTick += dt;
                    if (_channelTick >= 1f && _downed != null)
                    {
                        _channelTick -= 1f;
                        _downed.Bandage(_downed.DownedMaxHealth * 0.25f);
                        if (!_downed.IsDowned) { CancelCast(); return; }
                    }
                }
                if (_castTarget != null && _mover != null && !IsAutoAttack(CastingSkill)) _mover.FacingOverride = _castTarget.transform.position;   // Lauf 6: Auto-Angriff dreht nicht dauerhaft zum Ziel
                if (_mover != null) _mover.CastMoveFactor = CastingSkill.Timeline != null && CastingSkill.Timeline.Movement == SkillMoveRule.Rooted ? 0f : 1f;
                CastRemaining -= dt;
                if (CastRemaining <= 0f) CompleteCast();
            }
        }

        /// <summary>Lauf 4: Reittier übernimmt die Tasten (1 = Angriff beim Absitzen).</summary>
        public bool InputSuppressed { get; set; }

        void HandleInput()
        {
            var input = InputManager.Instance;
            if (input == null || _self == null || _self.IsDead) return;
            if (InputSuppressed) { _autoAttackArmed = false; return; }
            if (input.SwapPressed) { CancelAim(); TrySwapWeapons(); }
            if (_rangeFlash > 0f) { _rangeFlash -= Time.deltaTime; if (_rangeFlash <= 0f) HideRangeRing(); }
            if (IsAiming)
            {
                UpdateAim();
                if (input.EscapeForWorld) { AimInputFrame = Time.frameCount; Ground.Escape(); CancelAim(); return; }
                if (input.SkillReleased == _aimSlot && Ground.KeyUp(_aimSlot, Time.time) == GroundTargetModel.Action.Cast) { AimInputFrame = Time.frameCount; ConfirmAim(); return; }
                if (input.PrimaryClick) { AimInputFrame = Time.frameCount; Ground.Click(); ConfirmAim(); return; }
                if (input.SkillPressed == _aimSlot && Ground.CurrentMode == GroundTargetModel.Mode.Normal) { AimInputFrame = Time.frameCount; Ground.KeyDown(_aimSlot, Time.time); ConfirmAim(); return; }
                if (input.SkillPressed >= 0 && input.SkillPressed != _aimSlot) { Ground.OtherSkill(input.SkillPressed); CancelAim(); } // anderer Skill: Zielen abbrechen und normal fortfahren
                else return;
            }
            if (input.SkillPressed >= 0)
            {
                if (input.SkillPressed == 0) _autoAttackArmed = true;
                TryUseSlot(input.SkillPressed, false, input.ShiftHeld);
            }
            var rig = ThirdPersonCameraRig.Current;
            if (rig != null && rig.ActionCam && input.PrimaryClick) { _autoAttackArmed = true; TryUseSlot(0); }

            if (_targeting != null && _targeting.Current == null) { _autoAttackArmed = false; if (_hadTarget) Chain.Break(); }   // Fix-Lauf F1: Zielverlust setzt die Kette zurück
            _hadTarget = _targeting != null && _targeting.Current != null;
            if (AutoAttack && _autoAttackArmed && !IsCasting && !Queue.IsBusy(Time.time) && Queue.Queued == null && _targeting != null && _targeting.Current != null)
            {
                var s = GetSkillInSlot(0);
                if (s != null && GetCooldownRemaining(s) <= 0f && InRange(s, _targeting.Current)) TryUseSlot(0, true);
            }
        }

        bool InRange(SkillDefinition s, Combatant t)
        {
            if (t == null) return false;
            float d = Vector3.Distance(transform.position, t.transform.position);
            float r = s.Delivery == SkillDelivery.Melee ? Mathf.Max(s.Range, s.Radius) + 0.8f : s.Range;
            return d <= r;
        }

        /// <summary>Setzt die Waffensets aus der Ausrüstung (null = leer).</summary>
        public void SetWeaponSets(WeaponSet a, WeaponSet b)
        {
            if (WeaponSets == null || WeaponSets.Length < 2) WeaponSets = new WeaponSet[2];
            WeaponSets[0] = a; WeaponSets[1] = b;
            if (ActiveSet == null && WeaponSets[1 - ActiveSetIndex] != null) ActiveSetIndex = 1 - ActiveSetIndex;
            if (_visuals != null && ActiveSet != null) _visuals.ShowWeapon(ActiveSet.Visual);
            Swapped?.Invoke();
        }

        /// <summary>Lauf 6: Bündel ersetzen Skills 1–5 (Story-Director); Waffenwechsel mit Bündel gesperrt.</summary>
        public static System.Func<int, SkillDefinition> SlotOverride;
        public static System.Func<bool> SwapBlocked;

        public bool TrySwapWeapons()
        {
            if (_downed != null && _downed.IsDowned) return false;
            if (SwapBlocked != null && SwapBlocked()) { GameEventBus.Notify("Mit Bündel kein Waffenwechsel – 5 legt es ab", new Color(1f, 0.85f, 0.4f)); return false; }
            if (SwapCooldownRemaining > 0f) { GameEventBus.Notify("Waffenwechsel lädt noch …", new Color(1f, 0.85f, 0.4f)); return false; }
            if (WeaponSets == null || WeaponSets.Length < 2 || WeaponSets[1 - ActiveSetIndex] == null) { GameEventBus.Notify("Kein zweites Waffenset angelegt", new Color(1f, 0.85f, 0.4f)); return false; }
            if (Mechanic != null && !Mechanic.AllowSwap(out string swapWhy)) { GameEventBus.Notify(swapWhy, new Color(1f, 0.85f, 0.4f)); return false; }
            if (IsCasting) CancelCast();
            ActiveSetIndex = 1 - ActiveSetIndex;
            SwapCooldownRemaining = WeaponSwapCooldown;
            Chain.Break();   // Fix-Lauf F1: Waffenwechsel setzt die Kette zurück
            if (_visuals != null) _visuals.ShowWeapon(ActiveSet.Visual);
            Swapped?.Invoke();
            GameEventBus.RaiseWeaponSwap(_self);
            _self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.WeaponSwap, Self = _self, Position = transform.position });
            return true;
        }

        public bool TryUseSlot(int slot, bool silent = false, bool force = false)
        {
            var s = GetSkillInSlot(slot);
            if (s == null) return false;
            if (s.IsGroundTargeted && !silent)
            {
                LearnHints.Show("bodenziel", "Bodenziel-Modus: " + GroundTargetModel.Name(Ground.CurrentMode) + " (Optionen › Steuerung).");
                var act = Ground.KeyDown(slot, Time.time);
                if (act == GroundTargetModel.Action.Preview) { if (BeginAim(s, slot)) return true; Ground.Reset(); return false; }
                if (act == GroundTargetModel.Action.Cast || act == GroundTargetModel.Action.CastWithRangeFlash)
                {
                    if (!ResolveGroundPoint(s, out var gp)) { Fail(s, "Außer Reichweite", false); return false; }
                    if (act == GroundTargetModel.Action.CastWithRangeFlash) { ShowAimRing(gp, s.Radius, AimClamped); ShowRangeRing(s.Range); _rangeFlash = GroundTargetModel.RangeFlashSeconds; }
                    return TryCast(s, silent, gp, force);
                }
            }
            return TryCast(s, silent, null, force);
        }

        void Fail(SkillDefinition s, string reason, bool silent)
        {
            if (silent) return;
            GameEventBus.Notify(reason, new Color(1f, 0.85f, 0.4f));
            CastFailed?.Invoke(s, reason);
        }

        /// <summary>Kontrolle während der Aktivierung: abbrechen, Skill 5 s sperren (GW2), Kampftext „Unterbrochen“.</summary>
        bool _hadTarget;

        public void Interrupt()
        {
            var s = CastingSkill;
            if (s == null) return;
            Chain.Break();   // Fix-Lauf F1
            CancelCast();
            Queue.Cancel(Time.time);
            _cooldowns[s] = InterruptRule.LockedCooldown(GetCooldownRemaining(s));
            Interrupted?.Invoke(s);
            GameEventBus.RaiseInterrupted(_self, s);
        }

        /// <summary>Sichtlinie zwischen Zielpunkten (nur Umgebung, Charaktere blockieren nicht).</summary>
        public static bool HasLineOfSight(Combatant a, Combatant b)
        {
            if (a == null || b == null) return true;
            return !Physics.Linecast(a.AimPoint, b.AimPoint, LineOfSightMask, QueryTriggerInteraction.Ignore);
        }

        // ---------- Ground-Target-Zielen ----------
        public bool BeginAim(SkillDefinition s, int slot)
        {
            if (_self == null) Init();
            if (_self.IsDead || _self.Effects.Has(EffectType.Stun)) return false;
            if (GetCooldownRemaining(s) > 0f) { GameEventBus.Notify(s.DisplayName + " lädt noch", new Color(1f, 0.85f, 0.4f)); return false; }
            AimingSkill = s; _aimSlot = slot;
            UpdateAim();
            return true;
        }

        public void CancelAim()
        {
            Ground.Reset();
            CancelAimVisual();
        }

        void CancelAimVisual()
        {
            AimingSkill = null; _aimSlot = -1;
            if (_aimRing != null) _aimRing.SetActive(false);
        }

        public bool ConfirmAim()
        {
            var s = AimingSkill;
            if (s == null) return false;
            UpdateAim();
            Vector3 p = AimPoint; bool outside = AimOutOfRange;
            CancelAim();
            if (outside) { Fail(s, "Außer Reichweite – abgebrochen", false); return false; }   // „Auf maximale Reichweite begrenzen“ aus
            return TryCast(s, false, p);
        }

        /// <summary>Zielpunkt aus Cursor/Fadenkreuz nach den Bodenziel-Regeln (Einrasten, Reichweite).</summary>
        bool ResolveGroundPoint(SkillDefinition s, out Vector3 point)
        {
            Vector3 cursor;
            var rig = ThirdPersonCameraRig.Current;
            if (rig != null && rig.TryGetGroundPoint(out var gp)) cursor = gp;
            else cursor = transform.position + transform.forward * Mathf.Min(6f, s.Range);
            Vector3? target = _targeting != null && _targeting.Current != null && !_targeting.Current.IsDead && _self.IsHostileTo(_targeting.Current) ? _targeting.Current.transform.position : (Vector3?)null;
            bool ok = Ground.Resolve(transform.position, cursor, s.Range, target, out point, out bool clamped);
            AimClamped = clamped; AimOutOfRange = !ok;
            if (!ok) { var d = cursor - transform.position; d.y = 0f; point = transform.position + d.normalized * s.Range; }
            point = ZoneAnchors.GroundAt(point);
            return ok;
        }
        public bool AimOutOfRange { get; private set; }

        // Reichweitenring für „Sofort mit Reichweitenanzeige“
        GameObject _rangeRing; float _rangeFlash;
        void ShowRangeRing(float range)
        {
            var lib = VfxLibrary.Current;
            if (lib == null || lib.GroundMarker == null || !Application.isPlaying) return;
            if (_rangeRing == null)
            {
                _rangeRing = GameObject.CreatePrimitive(PrimitiveType.Quad); _rangeRing.name = "GroundTargetRange";
                Destroy(_rangeRing.GetComponent<Collider>()); _rangeRing.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var r = _rangeRing.GetComponent<Renderer>(); r.sharedMaterial = lib.GroundMarker;
                var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", new Color(1f, 1f, 1f, 0.35f)); mpb.SetFloat("_Fill", 0f); mpb.SetFloat("_Pulse", 0f); r.SetPropertyBlock(mpb);
            }
            _rangeRing.SetActive(true);
            _rangeRing.transform.position = transform.position + Vector3.up * 0.08f;
            _rangeRing.transform.localScale = new Vector3(range * 2f, range * 2f, 1f);
        }
        void HideRangeRing() { if (_rangeRing != null) _rangeRing.SetActive(false); if (!IsAiming && _aimRing != null) _aimRing.SetActive(false); }

        void UpdateAim()
        {
            var s = AimingSkill;
            if (s == null) return;
            ResolveGroundPoint(s, out var p);
            AimPoint = p;
            if (Ground.PreviewVisible(Time.time)) ShowAimRing(AimPoint, s.Radius, AimClamped || AimOutOfRange);
            else if (_aimRing != null && _rangeFlash <= 0f) _aimRing.SetActive(false);
        }

        void ShowAimRing(Vector3 pos, float radius, bool clamped)
        {
            var lib = VfxLibrary.Current;
            if (lib == null || lib.GroundMarker == null || !Application.isPlaying) return;
            if (_aimRing == null)
            {
                _aimRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _aimRing.name = "GroundTargetPreview";
                Destroy(_aimRing.GetComponent<Collider>());
                _aimRing.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                _aimRenderer = _aimRing.GetComponent<Renderer>();
                _aimRenderer.sharedMaterial = lib.GroundMarker;
                _aimMpb = new MaterialPropertyBlock();
            }
            _aimRing.SetActive(true);
            _aimRing.transform.position = pos + Vector3.up * 0.1f;
            _aimRing.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            _aimMpb.SetColor("_Color", clamped ? new Color(1f, 0.75f, 0.4f, 0.7f) : new Color(0.55f, 0.85f, 1f, 0.7f));
            _aimMpb.SetFloat("_Fill", 0f);
            _aimMpb.SetFloat("_Pulse", 1f);
            _aimRenderer.SetPropertyBlock(_aimMpb);
        }

        public bool TryCast(SkillDefinition s, bool silent = false, Vector3? forcedGroundPoint = null, bool force = false)
        {
            if (_self == null) Init();
            if (s == null || _self.IsDead) return false;
            if (_self.Effects.Has(EffectType.Stun)) { Fail(s, "Betäubt", silent); return false; }
            if (GetCooldownRemaining(s) > 0f) { if (!silent) { GameEventBus.Notify(s.DisplayName + " lädt noch", new Color(1f, 0.85f, 0.4f)); CastFailed?.Invoke(s, "Lädt noch"); } return false; }
            if (Mechanic != null && !Mechanic.CanCast(s, out string mechWhy)) { Fail(s, mechWhy, silent); return false; }

            Combatant target = _targeting != null ? _targeting.Current : null;
            if (target != null && !_self.IsHostileTo(target)) target = null;
            if (s.NeedsHostileTarget)
            {
                if (target == null && _targeting != null) { _targeting.CycleTarget(); target = _targeting.Current; }
                if (target == null) { Fail(s, "Kein Ziel", silent); return false; }
                if (Vector3.Distance(transform.position, target.transform.position) > s.Range + 0.5f) { Fail(s, "Außer Reichweite", silent); return false; }
                if (!HasLineOfSight(_self, target)) { Fail(s, "Keine Sichtlinie", silent); return false; }
            }

            // Warteschlange (Lauf 5): während Aktivierung/Nachwirkung einreihen; Sofort-Skills laufen nebenher; Shift erzwingt.
            float now = Time.time;
            bool busy = IsCasting || Queue.IsBusy(now);
            if (busy)
            {
                bool autoBusy = (IsCasting && IsAutoAttack(CastingSkill)) || (!IsCasting && _busyByAuto);
                if (s.IsInstant && !force && !IsAutoAttack(s)) { ExecuteAlongside(s, target, forcedGroundPoint); return true; }
                if (force || (autoBusy && !IsAutoAttack(s))) { if (IsCasting) CancelCast(); Queue.Cancel(now); }
                else if (CastingSkill == s || IsAutoAttack(s)) return false;
                else { if (!silent) Queue.Press(s, false, false, now); return !silent; }
            }

            _castTarget = target;
            _castPoint = forcedGroundPoint.HasValue ? forcedGroundPoint.Value : ResolveGroundPoint(s, target);
            LastGroundPoint = _castPoint;
            float quick = _self.Effects.Has(EffectType.Quickness) ? EffectRules.QuicknessCastFactor : 1f;
            var tl = s.Timeline != null && s.Timeline.IsBuilt ? s.Timeline : null;
            float first = tl != null ? tl.FirstHit : s.CastTime;
            float rest = tl != null ? (tl.Activation - tl.FirstHit) + tl.Aftercast : Mathf.Max(0, s.Hits - 1) * s.HitInterval;
            if (s.EvadesDuringCast) _evadeWindow.Remaining = s.CastTime + Mathf.Max(0, s.Hits - 1) * s.HitInterval + 0.1f;
            CastingSkill = s;
            float castTime = first / quick;
            CastTotal = castTime;
            CastRemaining = castTime;
            _channelTick = 0f;
            Queue.Begin(now, castTime, rest / quick);
            _activationStart = now; _activationEnd = now + (tl != null ? tl.Activation : s.CastTime + Mathf.Max(0, s.Hits - 1) * s.HitInterval) / quick;
            ChannelSkill = tl != null && tl.Channel && s.Hits > 1 ? s : null;
            _busyByAuto = IsAutoAttack(s);
            Mechanic?.OnCast(s);
            if (target != null && _mover != null && !IsAutoAttack(s)) _mover.FacingOverride = target.transform.position;
            else if (target != null && _mover != null && !_mover.PlayerSteering) { var dir = target.transform.position - transform.position; dir.y = 0f; if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir); }   // Auto-Angriff: einmal zum Ziel drehen, nur wenn der Spieler steht
            CastStarted?.Invoke(s);
            if (CastRemaining <= 0f) CompleteCast();
            return true;
        }

        /// <summary>Sofort-Skill neben einer laufenden Aktivierung (unterbricht sie nicht).</summary>
        void ExecuteAlongside(SkillDefinition s, Combatant target, Vector3? point)
        {
            Mechanic?.OnCast(s);
            if (s.Cooldown > 0f) _cooldowns[s] = s.Cooldown * _self.Modifiers.RechargeMultiplier(s);
            CastStarted?.Invoke(s);
            CastReleased?.Invoke(s);
            SkillExecutor.Execute(new SkillContext
            {
                Caster = _self, Skill = s, Target = target, GroundPoint = point ?? ResolveGroundPoint(s, target),
                Forward = transform.forward, Table = ComboTable, Runner = this, Mover = _mover
            });
        }

        Vector3 ResolveGroundPoint(SkillDefinition s, Combatant target)
        {
            if (!s.IsGroundTargeted) return transform.position;
            Vector3 p;
            var rig = ThirdPersonCameraRig.Current;
            if (target != null && Vector3.Distance(target.transform.position, transform.position) <= s.Range) p = target.transform.position;
            else if (rig != null && rig.TryGetGroundPoint(out var gp)) p = gp;
            else p = transform.position + transform.forward * Mathf.Min(6f, s.Range);
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.magnitude > s.Range) p = transform.position + d.normalized * s.Range;
            return ZoneAnchors.GroundAt(p);
        }

        void CompleteCast()
        {
            var s = CastingSkill;
            CastingSkill = null;
            if (_mover != null) { _mover.FacingOverride = null; _mover.CastMoveFactor = 1f; }
            if (s == null) return;
            if (s.Cooldown > 0f) _cooldowns[s] = s.Cooldown * _self.Modifiers.RechargeMultiplier(s);
            if (IsAutoAttack(s)) Chain.Completed(Time.time);
            CastReleased?.Invoke(s);
            if (s.Special == SkillSpecial.Bandage) return; // Wirkung erfolgt kanalisiert
            SkillExecutor.Execute(new SkillContext
            {
                Caster = _self, Skill = s, Target = _castTarget, GroundPoint = _castPoint,
                Forward = transform.forward, Table = ComboTable, Runner = this, Mover = _mover
            });
        }

        public void CancelCast()
        {
            CastingSkill = null;
            CastRemaining = 0f;
            if (_mover != null) { _mover.FacingOverride = null; _mover.CastMoveFactor = 1f; }
        }

        /// <summary>Für Tests: Cooldown direkt setzen/abfragen.</summary>
        public void SetCooldown(SkillDefinition s, float value) { if (value <= 0f) _cooldowns.Remove(s); else _cooldowns[s] = value; }
    }
}
