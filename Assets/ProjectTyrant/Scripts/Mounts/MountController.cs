using UnityEngine;
using ProjectTyrant.Flight;

namespace ProjectTyrant
{
    /// <summary>
    /// Reiten am Spieler (Komponente, kein Singleton; Besitz/Auswahl im <see cref="PlayerProfile"/>):
    /// X antippen = auf-/absitzen (1 s), X halten = Radialmenü; Leertaste = Reittier-Fähigkeit, 1 = Angriff beim Absitzen,
    /// V = Reittier-Ausweichen; Reittier-HP fangen Schaden ab. Zusätzlich die Gelände-Regeln der Biom-Zonen:
    /// tiefes Wasser (ohne Skimmer zurück ans Ufer) und Lava (Schaden, zurück auf sicheren Boden).
    /// </summary>
    public partial class MountController : MonoBehaviour
    {
        public readonly MountMotor Motor = new MountMotor();
        public readonly GriffonFlight Griffon = new GriffonFlight();   // Lauf 6 (Altmodell, nur noch Tests)
        public FlightMotor Flight { get; private set; }         // Lauf 10
        public readonly DuneRunnerModel Dune = new DuneRunnerModel();   // Lauf 11
        public readonly BoatModel Boat = new BoatModel();               // Lauf 11
        public FlightVerdict FlightRule { get; private set; } = FlightVerdict.Ok(FlightRules.DefaultCeiling);
        float _ruleT, _updraftT, _airT; bool _wasBanned; Vector3 _heading;
        public static MountController Local { get; private set; }

        PlayerController _pc;
        Combatant _self;
        SkillCaster _caster;
        GameObject _visual;
        float _savedEnduranceMax = -1f;
        public const int GriffonEnduranceBars = 3;
        float _holdTime;
        bool _radialOpen;
        float _orbitalRadius = -1f;
        float _baseFallHeight;
        readonly InvulnSource _dodgeInvuln = new InvulnSource();
        class InvulnSource : IInvulnerabilitySource { public float Until; public bool IsInvulnerable => Time.time < Until; }

        // Gelände
        bool _resetSafe = true;
        Vector3 _safePos; float _safeTimer, _hazardTimer, _lavaTick;
        public static System.Func<float> LavaDamageFactor;   // M18: Umwelt-Beherrschung (0 = immun)
        public static System.Func<float> MountHealthFactor, LeapFactor, ChargeFactor, SpeedFactor;   // M18: Reittier-Beherrschung
        public bool InDeepWater { get; private set; }
        public bool InLava { get; private set; }

        void Awake()
        {
            _pc = GetComponent<PlayerController>();
            _self = GetComponent<Combatant>();
            _caster = GetComponent<SkillCaster>();
            Local = this;
            _self.AbsorbDamage = Absorb;
            _self.Damaged += d => { if (!Balance.BalanceWorld.Legacy && Motor.IsChanneling && d.Amount > 0f) { Motor.CancelChannel(); GameEventBus.Notify("Aufsitzen unterbrochen", new Color(1f, 0.7f, 0.4f)); } };   // Balance B1
            _self.InvulnerabilitySources.Add(_dodgeInvuln);
            _baseFallHeight = _pc.FallDamageHeight;
            _pc.FallDamageHandler = OnFall;
            _safePos = transform.position;
            if (GetComponent<JadeBotController>() == null) gameObject.AddComponent<JadeBotController>();   // Lauf 11: Jadegeist (aktiv ab Story-Flag)
        }

        void OnDestroy() { if (Local == this) Local = null; }

        float Absorb(float dmg)
        {
            float rest = Motor.AbsorbDamage(dmg, out bool knocked);
            if (knocked)
            {
                ApplyDismount();
                _self.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), null);
                GameEventBus.Notify("Abgeworfen! Reittier erschöpft – 4 s kein Aufsitzen", new Color(1f, 0.5f, 0.4f));
            }
            return rest;
        }

        bool OnFall(float fall)
        {
            if (!Motor.IsMounted) return false;
            // Reittiere fangen Stürze bis 30 m ab; darüber nimmt das Reittier den Schaden
            if (fall > 30f) Absorb(Motor.Stats.MaxHealth * Mathf.Clamp01((fall - 30f) / 20f));
            return true;
        }

        public MountDefinition Def(MountKind k) => PlayerProfile.Instance != null ? PlayerProfile.Instance.Mount(k) : null;

        MountStats StatsFor(MountKind k)
        {
            var d = Def(k);
            var s = d != null && d.Stats.Kind == k ? d.Stats : MountStats.Default(k);
            return s;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var input = InputManager.Instance;
            var prof = PlayerProfile.Instance;
            var gm = GameManager.Instance;
            Motor.HealthFactor = MountHealthFactor != null ? MountHealthFactor() : 1f;
            Motor.LeapFactor = LeapFactor != null ? LeapFactor() : 1f;
            Motor.ChargeFactor = ChargeFactor != null ? ChargeFactor() : 1f;
            bool blocked = _self.IsDead || _self.IsDowned || (gm != null && gm.Transitioning) || _pc.InputBlocked;
            if (gm != null && gm.Transitioning) _resetSafe = true;
            else if (_resetSafe && _pc.IsGrounded) { _resetSafe = false; _safePos = transform.position; _hazardTimer = 0f; }
            if (blocked) { if (Motor.IsMounted) ApplyDismount(); if (Motor.IsChanneling) Motor.CancelChannel(); CloseRadial(false); }

            if (Motor.Tick(dt)) ApplyMount();
            if (Motor.IsChanneling && _self.Effects.Has(EffectType.Stun)) Motor.CancelChannel();

            if (input != null && !blocked && prof != null) HandleInput(input, prof, dt);
            if (Motor.IsMounted) TickMounted(input, dt);
            TickTerrain(dt);
            UIController.Instance?.UpdateMountHud(this);
        }

        // ---------------- Eingabe ----------------
        void HandleInput(InputManager input, PlayerProfile prof, float dt)
        {
            if (input.MountPressed) _holdTime = 0f;
            if (input.MountHeld) _holdTime += dt;
            var r = MountInput.Evaluate(input.MountHeld, _holdTime, input.MountReleased);
            if (r == MountInput.Result.OpenRadial && !_radialOpen && prof.State.Mounts.Count > 0)
            {
                _radialOpen = true;
                UIController.Instance?.ShowMountRadial(true);
            }
            if (input.MountReleased && _radialOpen) { CloseRadial(true); _holdTime = 0f; return; }
            if (r == MountInput.Result.Toggle)
            {
                if (Motor.IsMounted) { ApplyDismount(); return; }
                if (Motor.IsChanneling) { Motor.CancelChannel(); return; }
                var sel = prof.State.MountSelection();   // Lauf 10: Standard- oder Zuletzt-Modus
                var tap = sel.ForTap();
                TryMount(tap != MountKind.None ? tap : prof.State.SelectedMount);
            }
            // Lauf 10: Direkttasten je Reittier (Reittier-Tab)
            if (!_radialOpen && prof.State.MountPrefs.Keys != null && prof.State.MountPrefs.Keys.Count > 0)
            {
                var sel = prof.State.MountSelection();
                foreach (var b in prof.State.MountPrefs.Keys)
                {
                    var parts = b.Split('='); if (parts.Length != 2 || !input.KeyDown(parts[1])) continue;
                    var k = sel.ForKey(parts[1]); if (k != MountKind.None) SwitchOrMount(k);
                    break;
                }
            }
            if (Motor.IsMounted && input.InteractPressed) ApplyDismount();   // GW2: Interagieren sitzt ab
        }

        void CloseRadial(bool choose)
        {
            if (!_radialOpen) return;
            _radialOpen = false;
            var k = UIController.Instance != null ? UIController.Instance.ShowMountRadial(false) : MountKind.None;
            if (!choose || k == MountKind.None) return;
            var prof = PlayerProfile.Instance;
            var sel = prof.State.MountSelection();
            k = sel.RadialPick(k);   // Lauf 10: Radial setzt den Standard nur, wenn so eingestellt
            if (k == MountKind.None) return;
            prof.State.StoreMountSelection(sel); GameEventBus.RaiseProfileChanged(); prof.RequestSave("Reittier gewählt");
            SwitchOrMount(k);
        }

        /// <summary>Lauf 10: Direktwechsel aufgesessen (am Boden immer; in der Luft Greif ↔ Himmelsschuppe mit Vertrauensband) oder Aufsitzen.</summary>
        public void SwitchOrMount(MountKind k)
        {
            if (Motor.IsMounted && Motor.Mounted == k) return;
            if (Motor.IsMounted)
            {
                bool air = Flight != null && Flight.Airborne;
                var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
                if (!MountSelectionModel.CanSwitch(Motor.Mounted, k, air, mm != null && MasteryEffects.TrustBond(mm)))
                { GameEventBus.Notify(air ? "In der Luft nur Greif ↔ Himmelsschuppe (Beherrschung „Vertrauensband“)" : "Wechsel nicht möglich", new Color(1f, 0.7f, 0.4f)); return; }
                if (air) { AirSwap(k); return; }
                ApplyDismount(); Motor.Tick(0f);
            }
            TryMount(k);
        }

        /// <summary>Lauf 10: Luftwechsel ohne Absitzen – Höhe und Tempo bleiben erhalten.</summary>
        void AirSwap(MountKind k)
        {
            var prof = PlayerProfile.Instance; if (prof == null || !prof.OwnsMount(k)) return;
            var old = Flight;
            ApplyDismount();
            Motor.Tick(0f);
            if (!Motor.BeginMount(StatsFor(k), true, true)) return;
            for (int i = 0; i < 20 && Motor.IsChanneling; i++) Motor.Tick(0.1f);   // Luftwechsel ohne Aufsitz-Kanal
            ApplyMount();
            if (Flight != null && old != null) { Flight.Forward = old.Speed; Flight.Vertical = old.Vertical; Flight.Altitude = old.Altitude; Flight.LeaveGround(); }
            GameEventBus.Notify("Luftwechsel: " + (Def(k) != null ? Def(k).DisplayName : k.ToString()), new Color(0.75f, 0.9f, 1f));
        }

        /// <summary>Nach Lauf 11: Flugreittier in der Luft rufen – sofort aufsitzen, Schwung und Höhe übernehmen, Fall abfangen.</summary>
        bool AirMount(MountKind k)
        {
            var rule = EvaluateFlightHere();
            if (!rule.Allowed && !FlightDebug.RulesOff) { GameEventBus.Notify(rule.Reason, new Color(1f, 0.7f, 0.4f)); return false; }
            if (!Motor.BeginMount(StatsFor(k), true, true))
            {
                if (Motor.RemountBlock > 0f) GameEventBus.Notify("Reittier erholt sich noch", new Color(1f, 0.7f, 0.4f));
                return false;
            }
            var glide = GetComponent<MasteryAbilities>(); if (glide != null) glide.StopGlide();   // Gleitschirm weg, Reittier übernimmt
            for (int i = 0; i < 20 && Motor.IsChanneling; i++) Motor.Tick(0.1f);   // kein Aufsitz-Kanal in der Luft
            ApplyMount();   // setzt GroundSpeed aus dem aktuellen Tempo
            if (Flight != null)
            {
                Flight.Altitude = Mathf.Max(0f, transform.position.y - GroundBelow());
                Flight.LeaveGround();
                if (Flight.P.Kind == FlyerKind.Griffon) Flight.Vertical = Mathf.Clamp(_pc.VerticalVelocity, -6f, 4f);   // Greif fängt den Fall im Gleiten ab
                _airT = 0f;
            }
            _pc.ResetFallTracking();
            SkillExecutor.VfxPulse(transform.position, 1.8f, new Color(0.85f, 0.9f, 1f));
            return true;
        }

        public bool TryMount(MountKind k)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null) return false;
            if (k == MountKind.None || !prof.OwnsMount(k))
            {
                GameEventBus.Notify(prof.State.Mounts.Count == 0 ? "Noch kein Reittier – der Stallmeister in Sonnfurt verkauft den Raptor" : "Reittier nicht freigeschaltet", new Color(1f, 0.7f, 0.4f));
                return false;
            }
            var regen = GetComponent<OutOfCombatRegen>();
            if (!Balance.BalanceWorld.Legacy && regen != null && regen.InCombat && !Motor.IsMounted) { GameEventBus.Notify("Im Kampf kein Aufsitzen – erst 5 s ohne Schaden", new Color(1f, 0.7f, 0.4f)); return false; }   // Balance B1: GW2-Regel
            if (k == MountKind.Skiff && !NearWater()) { GameEventBus.Notify("Das Boot lässt sich nur auf Wasser rufen", new Color(1f, 0.7f, 0.4f)); return false; }   // Lauf 11
            if (InDeepWater && k != MountKind.Skimmer && k != MountKind.Skiff) { GameEventBus.Notify("Im tiefen Wasser nur mit dem Skimmer", new Color(1f, 0.7f, 0.4f)); return false; }
            bool groundedNow = _pc.IsGrounded || _pc.IsHovering;
            if (!groundedNow && k != MountKind.Skiff && !Motor.IsMounted)
            {
                // Nach Lauf 11: Greif und Himmelsschuppe lassen sich im Sprung/Fall/Gleiten sofort rufen (ohne Aufsitz-Kanal)
                if (!FlightRules.IsFlyer(k)) { GameEventBus.Notify("In der Luft nur Greif oder Himmelsschuppe", new Color(1f, 0.7f, 0.4f)); return false; }
                return AirMount(k);
            }
            if (!Motor.BeginMount(StatsFor(k), groundedNow || k == MountKind.Skiff, true))
            {
                if (Motor.RemountBlock > 0f) GameEventBus.Notify("Reittier erholt sich noch", new Color(1f, 0.7f, 0.4f));
                return false;
            }
            return true;
        }

        // ---------------- Auf-/Absitzen ----------------
        void ApplyMount()
        {
            var d = Def(Motor.Mounted);
            _pc.MountedControl = true;
            _pc.SpeedOverride = Motor.Stats.Speed;
            _pc.VisualOffset = new Vector3(0f, d != null ? d.SeatHeight : 1.1f, -0.1f); _seat = _pc.VisualOffset; _tilt = 0f;
            _pc.FallDamageHeight = 30f;
            if (_caster != null) { _caster.InputSuppressed = true; _caster.CancelCast(); }
            if (_visual != null) Destroy(_visual);
            if (d != null && d.Prefab != null)
            {
                _visual = Instantiate(d.Prefab, transform);
                _visual.transform.localPosition = Vector3.zero; _visual.transform.localRotation = Quaternion.identity;
            }
            var rig = ThirdPersonCameraRig.Current;
            if (rig != null && rig.Orbital != null) { _orbitalRadius = rig.Orbital.Radius; rig.Orbital.Radius = _orbitalRadius * 1.3f; }
            SkillExecutor.VfxPulse(transform.position, 2.2f, new Color(1f, 0.85f, 0.5f));
            BeginFlight(Motor.Mounted);   // Lauf 10
            if (Motor.Mounted == MountKind.Griffon) { var e = _pc.Dodge.Endurance; _savedEnduranceMax = e.Max; e.Max = GriffonEnduranceBars * e.DodgeCost; }   // 3 Ausweich-Balken auf dem Greif
            var pr = PlayerProfile.Instance;
            if (pr != null) { var sel = pr.State.MountSelection(); sel.Used(Motor.Mounted); pr.State.StoreMountSelection(sel); }
            MountChanged?.Invoke(Motor.Mounted);
        }

        public void ApplyDismount()
        {
            bool was = Motor.IsMounted;
            if (was) Motor.Dismount();
            _pc.MountedControl = false;
            _pc.SpeedOverride = 0f;
            _pc.VisualOffset = Vector3.zero; _pc.VisualTilt = Quaternion.identity; _tilt = 0f;
            _pc.HoverFloor = null;
            _pc.FallDamageHeight = _baseFallHeight;
            _pc.MaxFallSpeed = 0f; Griffon.Land();   // Lauf 6
            EndFlight();   // Lauf 10
            if (_savedEnduranceMax > 0f) { var e = _pc.Dodge.Endurance; e.Max = _savedEnduranceMax; e.Current = Mathf.Min(e.Current, e.Max); _savedEnduranceMax = -1f; }
            if (_caster != null) _caster.InputSuppressed = false;
            if (_visual != null) { Destroy(_visual); _visual = null; }
            var rig = ThirdPersonCameraRig.Current;
            if (rig != null && rig.Orbital != null && _orbitalRadius > 0f) { rig.Orbital.Radius = _orbitalRadius; _orbitalRadius = -1f; }
            if (was) MountChanged?.Invoke(MountKind.None);
        }

        public static event System.Action<MountKind> MountChanged;

        // ---------------- Reiten ----------------
        void TickMounted(InputManager input, float dt)
        {
            bool onWater = Motor.HoversOnWater && _pc.IsHovering;
            _pc.SpeedOverride = Motor.GroundSpeed(onWater) * (Motor.Charging ? 0.5f : 1f) * (SpeedFactor != null ? SpeedFactor() : 1f);
            if (input == null) return;
            bool grounded = _pc.IsGrounded || _pc.IsHovering;
            var fwd = _pc.LastWish.sqrMagnitude > 0.01f ? _pc.LastWish.normalized : transform.forward;
            fwd.y = 0f;
            switch (Motor.Mounted)
            {
                case MountKind.Raptor:
                    if (input.JumpPressed && grounded && Motor.TryLeap(out float sp, out float t, out float up))
                    {
                        _pc.Launch(fwd * sp, up, t);
                        _pc.ResetFallTracking();
                    }
                    break;
                case MountKind.Springer:
                    if (input.JumpPressed && grounded) Motor.BeginCharge();
                    if ((input.JumpReleased || (!input.JumpHeld && Motor.Charging)) && Motor.Charging)
                    {
                        float h = Motor.ReleaseCharge();
                        _pc.SetVerticalVelocity(MountMotor.JumpVelocity(h, _pc.Gravity));
                        _pc.ResetFallTracking();
                    }
                    break;
                case MountKind.Skimmer:
                    if (input.JumpPressed && grounded) _pc.SetVerticalVelocity(MountMotor.JumpVelocity(Motor.Stats.HopHeight, _pc.Gravity));
                    break;
                case MountKind.DuneRunner:   // Lauf 11: Blinkschritt (Ladungen am Boden)
                {
                    var pm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
                    Dune.Rank = MasteryEffects.DuneRunnerRank(pm); Dune.Tick(dt, grounded);
                    if (input.JumpPressed && Dune.TryBlink(grounded, transform.position, fwd, out var to))
                    {
                        var g = ZoneAnchors.GroundAt(to + Vector3.up * 3f);
                        SkillExecutor.VfxPulse(transform.position, 1.5f, new Color(1f, 0.8f, 0.4f));
                        _pc.Teleport(new Vector3(to.x, Mathf.Max(g.y, transform.position.y - 3f) + 0.1f, to.z), transform.rotation);
                        _pc.ResetFallTracking();
                    }
                    break;
                }
                case MountKind.Skiff:   // Lauf 11: Boot mit Trägheit, Anker (Leertaste), Jade-Schub (V)
                {
                    if (!onWater && !NearWater()) { ApplyDismount(); GameEventBus.Notify("Das Boot bleibt am Ufer", new Color(0.6f, 0.85f, 1f)); break; }
                    var pm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
                    Boat.Rank = MasteryEffects.BoatRank(pm); Boat.Summoned = true;
                    if (input.JumpPressed) { Boat.ToggleAnchor(); GameEventBus.Notify(Boat.Anchored ? "Anker geworfen" : "Anker gelichtet", new Color(0.6f, 0.85f, 1f)); }
                    if (input.DodgePressed && Boat.Boost()) GameEventBus.Notify("Jade-Schub!", new Color(0.5f, 1f, 0.8f));
                    Boat.Step(dt, Mathf.Clamp01(input.Move.magnitude), 0f);
                    _pc.SpeedOverride = Boat.Anchored ? 0.01f : Mathf.Max(0.5f, Boat.Speed);
                    return;
                }
                case MountKind.Griffon:
                case MountKind.Skyscale:   // Lauf 10: kinematischer Flug (FlightMotor)
                    TickFlight(input, dt, grounded);
                    break;
            }
            if (input.DodgePressed && !(Flight != null && Flight.Airborne) && Motor.TryDodge())
            {
                var dir = fwd;
                _pc.Launch(dir * (Motor.Stats.DodgeDistance / 0.35f), 0f, 0.35f);
                _dodgeInvuln.Until = Time.time + 0.4f;
                GameEventBus.RaiseDodge(_self);
            }
            if (input.SkillPressed == 0) Engage();
        }

        /// <summary>Angriff beim Absitzen (Taste 1): Ziel oder nächster Gegner im Bereich, Flächenschaden + Wirkung je Reittier.</summary>
        void Engage()
        {
            var tsys = GetComponent<TargetingSystem>();
            var target = tsys != null && tsys.Current != null && _self.IsHostileTo(tsys.Current) ? tsys.Current : Combatant.FindNearestHostile(_self, transform.position, Motor.Stats.EngageRange);
            float dist = target != null ? Vector3.Distance(target.transform.position, transform.position) : float.MaxValue;
            var kind = Motor.Mounted;
            float diveFactor = Flight != null ? Flight.EngageFactor : 0f;   // Lauf 10: Sturzangriff skaliert mit dem Sturz-Tempo
            if (target == null || !Motor.TryEngage(dist, out float coef, out float radius)) { GameEventBus.Notify("Kein Gegner in Reichweite für den Absitz-Angriff", new Color(1f, 0.7f, 0.4f)); return; }
            var land = target.transform.position - (target.transform.position - transform.position).normalized * 1.5f;
            if (_pc != null) _pc.Teleport(ZoneAnchors.GroundAt(land + Vector3.up * 2f) + Vector3.up * 0.1f, Quaternion.LookRotation(Flat(target.transform.position - transform.position)));
            ApplyDismount();
            foreach (var c in Combatant.All.ToArray())
            {
                if (c == null || c.IsDead || !_self.IsHostileTo(c)) continue;
                if (Vector3.Distance(c.transform.position, transform.position) > radius) continue;
                DamageCalculator.DealDirect(_self, c, kind == MountKind.Griffon ? coef * (0.6f + 0.9f * diveFactor) : coef);
                switch (kind)
                {
                    case MountKind.Raptor: c.Effects.Apply(new EffectApplication(EffectType.Cripple, 1, 3f), _self); break;        // Schwanzfeger
                    case MountKind.Springer: c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1.5f), _self); break;      // Stampfer
                    case MountKind.Skimmer: c.Effects.Apply(new EffectApplication(EffectType.Vulnerability, 5, 6f), _self); break; // Wellenschlag
                    case MountKind.Griffon: c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), _self); c.Effects.Apply(new EffectApplication(EffectType.Vulnerability, 3, 5f), _self); break;   // Sturzangriff
                    case MountKind.Skyscale: c.Effects.Apply(new EffectApplication(EffectType.Burning, 3, 4f), _self); c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 0.5f), _self); break;   // Feuerstoß: 3 Brennen, Trotz 150
                }
            }
            SkillExecutor.VfxPulse(transform.position, radius, new Color(1f, 0.8f, 0.4f));
            GameEventBus.Notify(kind == MountKind.Raptor ? "Schwanzfeger!" : kind == MountKind.Springer ? "Stampfer!" : kind == MountKind.Griffon ? "Sturzangriff!" : kind == MountKind.Skyscale ? "Feuerstoß!" : "Wellenschlag!", new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>Lauf 10: Aufwind – wirkt auf Flugreittiere mit der passenden Beherrschung (Greif 2 / Himmelsschuppe 3).</summary>
        public bool ApplyUpdraft(float strength)
        {
            if (Flight == null || !Flight.Airborne) return false;
            var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
            bool ok = mm == null || (Motor.Mounted == MountKind.Griffon ? MasteryEffects.GriffonUpdrafts(mm) : MasteryEffects.SkyscaleUpdrafts(mm));
            if (!ok) return false;
            _updraftT = 0.25f;
            return true;
        }

        /// <summary>Lauf 11: steht der Spieler an/auf Wasser mit mindestens 1,2 m Tiefe in 4 m Umkreis?</summary>
        bool NearWater()
        {
            var root = ZoneRoot.Current;
            if (root == null || root.Definition == null || root.Definition.Biome == null || root.Definition.Biome.WaterLevel < -50f) return false;
            float water = root.Definition.Biome.WaterLevel;
            for (int i = 0; i < 9; i++)
            {
                var o = i == 0 ? Vector3.zero : Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 4f;
                var q = transform.position + o;
                if (BoatModel.CanSummon(water - ZoneRoot.GroundHeight(q.x, q.z))) return true;
            }
            return false;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 1e-4f ? Vector3.forward : v; }

        // ---------------- Gelände: tiefes Wasser & Lava ----------------
        void TickTerrain(float dt)
        {
            var root = ZoneRoot.Current;
            InDeepWater = false; InLava = false;
            if (root == null || root.Definition == null || root.Definition.LegacyLayout || root.Definition.Biome == null) { _pc.HoverFloor = null; MarkSafe(dt, true); return; }
            var biome = root.Definition.Biome;
            if (biome.WaterLevel < -50f) { _pc.HoverFloor = null; MarkSafe(dt, true); return; }
            float water = biome.WaterLevel;
            float ground = ZoneRoot.GroundHeight(transform.position.x, transform.position.z);
            bool lava = biome.Ambient == AmbientKind.Embers;
            float depth = water - ground;
            // Skimmer schwebt über Wasser (nicht über Lava)
            _pc.HoverFloor = Motor.HoversOnWater && !lava && depth > 0.2f ? water + 0.05f : (float?)null;
            bool submerged = transform.position.y < water - 0.3f;
            if (lava && depth > 0f && transform.position.y < water + 0.6f)
            {
                InLava = true;
                float factor = LavaDamageFactor != null ? LavaDamageFactor() : 1f;
                if (factor > 0f)
                {
                    _lavaTick -= dt;
                    if (_lavaTick <= 0f)
                    {
                        _lavaTick = 0.5f;
                        _self.TakeDamage(new DamageInfo { Source = null, Amount = _self.MaxHealth * 0.06f * factor, Point = transform.position, IsCondition = true });
                        _self.Effects.Apply(new EffectApplication(EffectType.Burning, 2, 3f), null);
                    }
                    _hazardTimer += dt;
                    if (_hazardTimer > 1.5f) ReturnToSafe("Die Lava verbrennt dich – zurück auf festen Boden. (Umwelt-Beherrschung: Lavarisse)");
                }
                return;
            }
            if (!lava && depth > 1.4f && submerged && !Motor.HoversOnWater)
            {
                InDeepWater = true;
                _hazardTimer += dt;
                if (_hazardTimer > 0.8f) ReturnToSafe("Zu tief! Mit dem Skimmer gleitest du über das Wasser.");
                return;
            }
            _hazardTimer = 0f;
            MarkSafe(dt, depth < 0.8f && !_pc.IsHovering);
        }

        void MarkSafe(float dt, bool ok)
        {
            if (!ok || !_pc.IsGrounded) return;
            _safeTimer -= dt;
            if (_safeTimer > 0f) return;
            _safeTimer = 0.5f;
            _safePos = transform.position;
        }

        void ReturnToSafe(string msg)
        {
            _hazardTimer = 0f;
            if (Motor.IsMounted && !Motor.HoversOnWater) ApplyDismount();
            _pc.Teleport(_safePos + Vector3.up * 0.3f, transform.rotation);
            GameEventBus.Notify(msg, new Color(0.6f, 0.85f, 1f));
        }
    }
}
