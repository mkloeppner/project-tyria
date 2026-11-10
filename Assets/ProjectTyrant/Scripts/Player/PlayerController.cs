using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Third-Person-Bewegung mit CharacterController: Laufen, Strafe, Sprung, Dodge-Roll.</summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Combatant))]
    public class PlayerController : MonoBehaviour
    {
        public float MoveSpeed = 7f;
        public float BackwardFactor = 0.65f;
        public float TurnSpeed = 720f;
        public float JumpHeight = 1.2f;
        [Tooltip("Reduzierte Luftsteuerung (m/s² Richtungsänderung in der Luft).")]
        public float AirAcceleration = 7f;
        [Tooltip("Fallschaden ab dieser Höhe (GW2: ~8 m).")]
        public float FallDamageHeight = 8f;
        [Tooltip("Höhe über der Schwelle, ab der ein Sturz tödlich ist.")]
        public float FallLethalExtra = 12f;
        public bool GodMode;
        public float Gravity = -24f;
        public DodgeModel Dodge = new DodgeModel();
        public Transform Visual;

        public Combatant Combatant { get; private set; }
        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public bool IsMoving { get; private set; }
        /// <summary>Wird vom SkillCaster gesetzt, damit der Charakter beim Wirken das Ziel ansieht.</summary>
        public Vector3? FacingOverride { get; set; }

        CharacterController _cc;
        float _vy;
        float _bobPhase;
        Vector3 _visualBasePos;

        void Awake()
        {
            QaMove = null; QaRmb = false; QaWishWorld = null;
            _cc = GetComponent<CharacterController>();
            Combatant = GetComponent<Combatant>();
            Dodge.Endurance.Fill();
            Combatant.InvulnerabilitySources.Add(Dodge);
            if (Visual != null) _visualBasePos = Visual.localPosition;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var input = InputManager.Instance;
            var rig = ThirdPersonCameraRig.Current;
            bool dead = Combatant.IsDead;
            bool downed = Combatant.IsDowned;
            bool stunned = Combatant.Effects.Has(EffectType.Stun);

            Vector3 camF = rig != null ? rig.FlatForward : Vector3.forward;
            Vector3 camR = Vector3.Cross(Vector3.up, camF);

            Vector2 m = Vector2.zero; float strafe = 0f;
            if (input != null && !dead && !stunned && !InputBlocked) { m = input.Move; strafe = input.Strafe; }
            if (QaMove.HasValue && !dead && !stunned) m = QaMove.Value;
            if (WaypointSafety.BelowWorld(transform.position) && Time.unscaledTime >= _voidRescueAt) { _voidRescueAt = Time.unscaledTime + 3f; var pr = PlayerProfile.Instance; if (pr != null) pr.RescueFromVoid(); }   // QA (Fix-Lauf F1): Bewegungseingabe ohne Tastatur-Fokus

            bool faceCamera = rig != null && rig.IsRotating;
            // Lauf 6: Eigene Steuerung schlägt die Blickrichtung zum Ziel – wer läuft oder dreht, darf sich vom Ziel abwenden (außer bei verwurzelten Fertigkeiten)
            bool steering = (m.sqrMagnitude > 0.01f || Mathf.Abs(strafe) > 0.01f || rmb0()) && CastMoveFactor > 0f;
            var facingOverride = steering ? null : FacingOverride; PlayerSteering = steering || (input != null && Mathf.Abs(input.Move.x) > 0.01f);
            // Fix-Lauf F1 (ersetzt M1): GW2-Standard – A/D drehen, Q/E seitwärts; rechte Maustaste/Action-Cam/Reittier/Option „Seitwärts“ = bisheriges Verhalten.
            var mode = (MovementModel.Mode)(GameOptions.Current != null ? GameOptions.Current.TurnMode : 0);
            bool rmb = (input != null && input.RotateHeld) || QaRmb;
            var mv = MovementModel.Compute(mode, rmb, rig != null && rig.ActionCam, MountedControl, m, strafe, transform.eulerAngles.y, camF, dt);
            Vector3 wish = mv.Wish;
            if (QaWishWorld.HasValue && !dead && !stunned) { wish = QaWishWorld.Value; wish.y = 0f; if (wish.sqrMagnitude > 1f) wish.Normalize(); }   // Balance B1: Bot-Steuerung in Weltrichtung
            TurnInput = mv.Turning ? m.x : 0f;
            if (mv.Turning && Mathf.Abs(m.x) > 0.1f) LearnHints.Show("ad_drehen", "A/D drehen den Charakter, Q/E gehen seitwärts; mit gehaltener rechter Maustaste werden A/D zu Seitwärts (Optionen › Steuerung).");
            if (mv.Turning && Mathf.Abs(mv.YawDelta) > 0f && !dead && !Dodge.IsDodging && !facingOverride.HasValue) { transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + mv.YawDelta, 0f); QaYawAccum += mv.YawDelta; }
            if (mv.Turning && rig != null && (Mathf.Abs(m.x) > 0.01f || m.y > 0.01f)) rig.FollowYaw(transform.eulerAngles.y);

            float speed = (SpeedOverride > 0f ? SpeedOverride : MoveSpeed) * Combatant.MoveSpeedMultiplier * (downed ? 0.2f : 1f) * CastMoveFactor;
            if (m.y < 0f && (faceCamera || mv.Turning)) speed *= BackwardFactor;

            // Dodge
            Dodge.Endurance.RegenMultiplier = (Combatant.Effects.Has(EffectType.Vigor) ? EffectRules.VigorEnduranceFactor : 1f) * (Combatant.Effects.Has(EffectType.Weakness) ? EffectRules.WeaknessEnduranceFactor : 1f);
            if (input != null && !dead && !downed && !stunned && !Dodge.IsDodging && !InputBlocked && Combatant.CanMove && !MountedControl)
            {
                Vector3 dodgeDir = Vector3.zero; bool want = false;
                if (input.DodgePressed) { want = true; dodgeDir = wish.sqrMagnitude > 0.01f ? wish : -transform.forward; }
                else if (input.DoubleTapDirection != Vector2.zero)
                {
                    want = true;
                    var d = input.DoubleTapDirection;
                    dodgeDir = camF * d.y + camR * d.x;
                }
                if (want)
                {
                    if (!Dodge.TryStartDodge(dodgeDir))
                        GameEventBus.Notify("Nicht genug Ausdauer", new Color(1f, 0.85f, 0.4f));
                    else
                    {
                        GameEventBus.RaiseDodge(Combatant);
                        Combatant.Modifiers.Trigger(new TriggerContext { Type = TriggerType.Dodge, Self = Combatant, Position = transform.position });
                    }
                }
            }

            Vector3 horizontal;
            _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, dt * Mathf.Max(6f, _impulse.magnitude * 3.5f));
            Vector3 dodgeStep = Dodge.Tick(dt);
            if (Dodge.IsDodging || dodgeStep != Vector3.zero) horizontal = dodgeStep / Mathf.Max(dt, 0.0001f);
            else horizontal = wish * speed;
            bool flying = FlightVelocity.HasValue && !Dodge.IsDodging;   // Lauf 10: Flugreittier gibt die Geschwindigkeit vor
            if (flying) horizontal = FlightVelocity.Value;
            LastWish = wish;
            bool grounded = _cc.isGrounded || IsHovering;
            if (_launchTime > 0f) { _launchTime -= dt; horizontal = _launchVel + wish * speed * 0.15f; _airVel = horizontal; }
            if (!grounded && !Dodge.IsDodging && _launchTime <= 0f && !flying)
            {
                _airVel = Vector3.MoveTowards(_airVel, horizontal, AirAcceleration * dt);
                horizontal = _airVel;
            }
            else _airVel = horizontal;
            IsMoving = horizontal.sqrMagnitude > 0.1f;
            HorizontalVelocity = horizontal;

            // Ausrichtung
            Vector3 face = Vector3.zero;
            if (facingOverride.HasValue) face = facingOverride.Value - transform.position;
            else if (Dodge.IsDodging) face = Vector3.zero;   // Lauf 6: Ausweichen dreht die Figur nicht – sie springt rückwärts/seitwärts und schaut weiter in Kamerarichtung
            else if (faceCamera) face = camF;
            else if ((mv.FaceWish || QaWishWorld.HasValue) && wish.sqrMagnitude > 0.01f) face = wish;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f && !dead)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), TurnSpeed * dt);

            // Sprung & Gravitation
            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            if (input != null && input.JumpPressed && _cc.isGrounded && !dead && !downed && !stunned && !Dodge.IsDodging && !InputBlocked && Combatant.CanMove && !MountedControl)
            {
                _vy = Mathf.Sqrt(2f * JumpHeight * -Gravity);
                JumpCount++;
            }
            _vy += Gravity * GravityScale * dt;
            if (MaxFallSpeed > 0f && _vy < -MaxFallSpeed) _vy = -MaxFallSpeed;

            if (_cc.enabled) _cc.Move((horizontal + _impulse + Vector3.up * _vy) * dt);
            // Schweben (Skimmer über Wasser): nicht unter die Schwebehöhe sinken
            IsHovering = false;
            if (HoverFloor.HasValue && transform.position.y <= HoverFloor.Value + 0.05f && _vy <= 0.5f)
            {
                var p = transform.position; p.y = HoverFloor.Value;
                if (_cc.enabled) { _cc.enabled = false; transform.position = p; _cc.enabled = true; } else transform.position = p;
                if (_vy < 0f) _vy = 0f;
                IsHovering = true;
            }
            TrackFall(grounded);
            AnimateVisual(dt, downed, dead);
        }

        /// <summary>Lauf 5: Skelett-Animation übernimmt Haltung/Rolle; hier nur noch Sitz-Versatz (Reittier).</summary>
        public bool AnimDriven { get; set; }
        /// <summary>Fix-Lauf F1: Drehrichtung im Drehmodus (−1 links … +1 rechts), für Dreh-Animation.</summary>
        public float TurnInput { get; private set; }
        /// <summary>QA (Fix-Lauf F1): Eingabe-Ersatz für Debug-Läufe ohne Fenster-Fokus; null = echte Eingabe.</summary>
        public static Vector2? QaMove; public static bool QaRmb; public float QaYawAccum;
        /// <summary>Balance B1: Bot-Bewegung als Weltrichtung (null = echte Eingabe, Vector3.zero = stehen).</summary>
        public static Vector3? QaWishWorld;
        float _voidRescueAt;

        void AnimateVisual(float dt, bool downed, bool dead)
        {
            if (Visual == null) return;
            if (AnimDriven)
            {
                Visual.localPosition = Vector3.Lerp(Visual.localPosition, _visualBasePos + VisualOffset, 1f - Mathf.Exp(-20f * dt));
                Visual.localRotation = Quaternion.identity;
                return;
            }
            Quaternion rot = Quaternion.identity;
            Vector3 pos = _visualBasePos + VisualOffset;
            if (dead || downed)
            {
                rot = Quaternion.Euler(-80f, 0f, 0f);
                pos += Vector3.up * 0.25f;
            }
            else if (Dodge.IsDodging && GetComponentInChildren<Anim.CharacterAnimator>() != null)
            {
                rot = Quaternion.identity;   // Lauf 6: Richtungs-Clips (DodgeF/B/L/R) tragen Sprung und Haltung – keine prozedurale Rolle mehr
            }
            else if (Dodge.IsDodging)
            {
                Vector3 localDir = transform.InverseTransformDirection(Dodge.Direction);
                Vector3 axis = Vector3.Cross(Vector3.up, localDir).normalized;
                if (axis.sqrMagnitude < 0.01f) axis = Vector3.right;
                rot = Quaternion.AngleAxis(Dodge.DodgeProgress * 360f, axis);
                pos += Vector3.down * 0.35f * Mathf.Sin(Dodge.DodgeProgress * Mathf.PI);
            }
            else if (IsMoving && IsGrounded && !MountedControl)
            {
                _bobPhase += dt * 12f;
                pos += Vector3.up * Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.08f;
                rot = Quaternion.Euler(6f, 0f, Mathf.Sin(_bobPhase) * 3f);
            }
            Visual.localPosition = Vector3.Lerp(Visual.localPosition, pos, 1f - Mathf.Exp(-20f * dt));
            Visual.localRotation = Dodge.IsDodging ? rot : Quaternion.Slerp(Visual.localRotation, rot, 1f - Mathf.Exp(-15f * dt));
        }

        /// <summary>Lauf 6: Spieler läuft, dreht oder hält die rechte Maustaste (Auto-Angriff dreht dann nicht zum Ziel).</summary>
        public bool PlayerSteering { get; private set; }
        bool rmb0() { var i = InputManager.Instance; return (i != null && i.RotateHeld) || QaRmb; }
        Vector3 _impulse;
        // ---------------- Lauf 4: Reittier-Steuerung ----------------
        /// <summary>Reittier steuert Sprung/Ausweichen selbst (Leertaste, V).</summary>
        public bool MountedControl { get; set; }
        /// <summary>&gt; 0: Bodengeschwindigkeit des Reittiers statt <see cref="MoveSpeed"/>.</summary>
        public float SpeedOverride { get; set; }
        public float GravityScale { get; set; } = 1f;
        /// <summary>Lauf 10: horizontale Fluggeschwindigkeit (Greif/Himmelsschuppe); null = normale Steuerung.</summary>
        public Vector3? FlightVelocity { get; set; }
        /// <summary>&gt; 0: maximale Fallgeschwindigkeit (Gleiter).</summary>
        public float MaxFallSpeed { get; set; }
        /// <summary>Schwebehöhe (Wasserspiegel) für den Skimmer; null = aus.</summary>
        public float? HoverFloor { get; set; }
        public bool IsHovering { get; private set; }
        public Vector3 VisualOffset { get; set; }
        public Vector3 LastWish { get; private set; }
        /// <summary>Waagrechte Geschwindigkeit des letzten Frames (Reittiere übernehmen damit den Schwung beim Aufsitzen/Abheben).</summary>
        public Vector3 HorizontalVelocity { get; private set; }
        public float VerticalVelocity => _vy;
        public bool Airborne => !(_cc != null && _cc.isGrounded) && !IsHovering;
        Vector3 _launchVel; float _launchTime;

        /// <summary>Sprung/Ansturm: feste horizontale Geschwindigkeit für <paramref name="duration"/> s, optional nach oben.</summary>
        public void Launch(Vector3 horizontalVelocity, float up, float duration)
        {
            horizontalVelocity.y = 0f;
            _launchVel = horizontalVelocity; _launchTime = duration;
            if (up > 0f) _vy = up;
        }

        public void SetVerticalVelocity(float vy) => _vy = vy;
        /// <summary>Lauf 4: Reittier/Gleiter fängt den Sturz ab (true = kein Spielerschaden).</summary>
        public System.Func<float, bool> FallDamageHandler;
        public void ResetFallTracking() { _peakY = transform.position.y; }
        /// <summary>Stoß (Rückstoß/Heranziehen, Lauf 4): horizontale Geschwindigkeit, klingt in ≈ 0,4 s ab; optional nach oben.</summary>
        public void AddImpulse(Vector3 horizontalVelocity, float up = 0f)
        {
            if (GodMode && horizontalVelocity.sqrMagnitude > 0f && Combatant.Effects.Has(EffectType.Stability)) return;
            if (Combatant.Effects.Has(EffectType.Stability)) return;
            horizontalVelocity.y = 0f;
            _impulse += horizontalVelocity;
            if (up > 0f) _vy = Mathf.Max(_vy, up);
        }

        public int JumpCount { get; private set; }
        /// <summary>Gesetzt von UI (Fenster/Dialog mit Fokus) – blockiert Bewegungseingaben.</summary>
        public bool InputBlocked { get; set; }
        /// <summary>Lauf 5: Bewegungs-Regel der Skill-Zeitachse (0 = verwurzelt während der Aktivierung, 1 = frei).</summary>
        public float CastMoveFactor { get; set; } = 1f;
        public float LastFallHeight { get; private set; }
        Vector3 _airVel;
        float _peakY;
        bool _wasGrounded = true;

        void TrackFall(bool groundedBefore)
        {
            bool grounded = _cc.isGrounded || IsHovering;
            if (!grounded)
            {
                if (_wasGrounded) _peakY = transform.position.y;
                _peakY = Mathf.Max(_peakY, transform.position.y);
            }
            else if (!_wasGrounded)
            {
                float fall = _peakY - transform.position.y;
                LastFallHeight = fall;
                ApplyFallDamage(fall);
            }
            _wasGrounded = grounded;
        }

        /// <summary>GW2: Fallschaden ab ~8 m, proportional zur Höhe, tödlich ab 20 m.</summary>
        public static float FallDamageFraction(float fall, float threshold, float lethalExtra) => fall <= threshold ? 0f : Mathf.Clamp01((fall - threshold) / Mathf.Max(0.1f, lethalExtra));

        void ApplyFallDamage(float fall)
        {
            float f = FallDamageFraction(fall, FallDamageHeight, FallLethalExtra);
            if (f <= 0f || GodMode || Combatant.IsDead) return;
            if (FallDamageHandler != null && FallDamageHandler(fall)) return;
            Combatant.TakeDamage(new DamageInfo { Source = null, Amount = Combatant.MaxHealth * f, Point = transform.position });
            GameEventBus.Notify("Sturzschaden!", new Color(1f, 0.6f, 0.4f));
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _cc.enabled = true;
            _vy = 0f;
            _wasGrounded = true;
            _peakY = position.y;
            Dodge.Cancel();
        }

        /// <summary>Für Sprung-Skills: kontrollierte Verschiebung.</summary>
        public void MoveBy(Vector3 delta)
        {
            if (_cc.enabled) _cc.Move(delta);
        }
    }
}
