using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Reiten am Spieler (Komponente, kein Singleton; Besitz/Auswahl im <see cref="PlayerProfile"/>):
    /// X antippen = auf-/absitzen (1 s), X halten = Radialmenü; Leertaste = Reittier-Fähigkeit, 1 = Angriff beim Absitzen,
    /// V = Reittier-Ausweichen; Reittier-HP fangen Schaden ab. Zusätzlich die Gelände-Regeln der Biom-Zonen:
    /// tiefes Wasser (ohne Skimmer zurück ans Ufer) und Lava (Schaden, zurück auf sicheren Boden).
    /// </summary>
    public class MountController : MonoBehaviour
    {
        public readonly MountMotor Motor = new MountMotor();
        public readonly GriffonFlight Griffon = new GriffonFlight();   // Lauf 6
        bool _diveArmed;
        public static MountController Local { get; private set; }

        PlayerController _pc;
        Combatant _self;
        SkillCaster _caster;
        GameObject _visual;
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
            _self.InvulnerabilitySources.Add(_dodgeInvuln);
            _baseFallHeight = _pc.FallDamageHeight;
            _pc.FallDamageHandler = OnFall;
            _safePos = transform.position;
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
                TryMount(prof.State.SelectedMount);
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
            prof.SelectMount(k);
            if (Motor.IsMounted && Motor.Mounted != k) { ApplyDismount(); Motor.Tick(0f); }
            if (!Motor.IsMounted) TryMount(k);
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
            if (InDeepWater && k != MountKind.Skimmer) { GameEventBus.Notify("Im tiefen Wasser nur mit dem Skimmer", new Color(1f, 0.7f, 0.4f)); return false; }
            if (!Motor.BeginMount(StatsFor(k), _pc.IsGrounded || _pc.IsHovering, true))
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
            _pc.VisualOffset = new Vector3(0f, d != null ? d.SeatHeight : 1.1f, -0.1f);
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
            MountChanged?.Invoke(Motor.Mounted);
        }

        public void ApplyDismount()
        {
            bool was = Motor.IsMounted;
            if (was) Motor.Dismount();
            _pc.MountedControl = false;
            _pc.SpeedOverride = 0f;
            _pc.VisualOffset = Vector3.zero;
            _pc.HoverFloor = null;
            _pc.FallDamageHeight = _baseFallHeight;
            _pc.MaxFallSpeed = 0f; Griffon.Land();   // Lauf 6
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
                case MountKind.Griffon:   // Lauf 6: Leertaste am Boden = Abheben, in der Luft halten = Sturzflug, loslassen = Abfangen
                    var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
                    Griffon.DiveUnlocked = mm == null || MasteryEffects.GriffonDive(mm); Griffon.Endurance = mm != null && MasteryEffects.GriffonEndurance(mm);
                    if (input.JumpPressed && grounded) { _pc.SetVerticalVelocity(MountMotor.JumpVelocity(Griffon.TakeoffHeight, _pc.Gravity)); _pc.ResetFallTracking(); _diveArmed = false; }
                    if (input.JumpReleased) _diveArmed = true;
                    Griffon.Tick(dt, !grounded, input.JumpHeld && _diveArmed && !grounded, out float climb);
                    if (!grounded)
                    {
                        _pc.SpeedOverride = Griffon.Speed * (SpeedFactor != null ? SpeedFactor() : 1f);
                        _pc.MaxFallSpeed = Griffon.Sink;
                        if (climb > 0f) _pc.SetVerticalVelocity(climb);
                        _pc.ResetFallTracking();
                    }
                    else _pc.MaxFallSpeed = 0f;
                    break;
            }
            if (input.DodgePressed && Motor.TryDodge())
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
            if (target == null || !Motor.TryEngage(dist, out float coef, out float radius)) { GameEventBus.Notify("Kein Gegner in Reichweite für den Absitz-Angriff", new Color(1f, 0.7f, 0.4f)); return; }
            var land = target.transform.position - (target.transform.position - transform.position).normalized * 1.5f;
            if (_pc != null) _pc.Teleport(ZoneAnchors.GroundAt(land + Vector3.up * 2f) + Vector3.up * 0.1f, Quaternion.LookRotation(Flat(target.transform.position - transform.position)));
            ApplyDismount();
            foreach (var c in Combatant.All.ToArray())
            {
                if (c == null || c.IsDead || !_self.IsHostileTo(c)) continue;
                if (Vector3.Distance(c.transform.position, transform.position) > radius) continue;
                DamageCalculator.DealDirect(_self, c, coef);
                switch (kind)
                {
                    case MountKind.Raptor: c.Effects.Apply(new EffectApplication(EffectType.Cripple, 1, 3f), _self); break;        // Schwanzfeger
                    case MountKind.Springer: c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1.5f), _self); break;      // Stampfer
                    case MountKind.Skimmer: c.Effects.Apply(new EffectApplication(EffectType.Vulnerability, 5, 6f), _self); break; // Wellenschlag
                    case MountKind.Griffon: c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), _self); c.Effects.Apply(new EffectApplication(EffectType.Vulnerability, 3, 5f), _self); break;   // Sturzangriff
                }
            }
            SkillExecutor.VfxPulse(transform.position, radius, new Color(1f, 0.8f, 0.4f));
            GameEventBus.Notify(kind == MountKind.Raptor ? "Schwanzfeger!" : kind == MountKind.Springer ? "Stampfer!" : kind == MountKind.Griffon ? "Sturzangriff!" : "Wellenschlag!", new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>Lauf 6: Aufwind hebt den fliegenden Greif an.</summary>
        public bool ApplyUpdraft(float strength)
        {
            if (Motor.Mounted != MountKind.Griffon || _pc.IsGrounded) return false;
            _pc.SetVerticalVelocity(Mathf.Max(_pc.VerticalVelocity, strength)); _pc.ResetFallTracking();
            return true;
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
