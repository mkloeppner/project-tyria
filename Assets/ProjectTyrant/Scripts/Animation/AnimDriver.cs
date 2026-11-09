using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// Verbindet Spiellogik mit dem <see cref="CharacterAnimator"/>: liest Bewegung, Sprung, Ausweichen, Gleiten, Reiten, Niedergeschlagen,
    /// Tod, Fertigkeiten, Waffenwechsel, Treffer und NPC-Angriffe und spielt die passenden Clips. Liegt auf der Spielobjekt-Wurzel.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public class AnimDriver : MonoBehaviour
    {
        public enum Role { Player, Npc, Villager, Mount, Critter }
        public Role Mode = Role.Npc;
        public CharacterAnimator Anim;
        [Tooltip("Präfix der Tier-Clips (z. B. „Quadruped_“), leer = Humanoid")] public string Prefix = "";
        [Tooltip("Waffenstil der Humanoiden (Clip-Präfix): Greatsword, Staff, Sword, …")] public string WeaponStyle = "Greatsword";
        public string NpcAttackClip = "";
        [Header("Waffen (Spieler/Wachen)")]
        public Transform HandSocket, BackSocket;
        public Transform[] Weapons;
        public float SheatheDelay = 6f;
        [Tooltip("Dorfbewohner/Händler drehen den Kopf zum Spieler (≤ 6 m)")] public bool LookAtPlayer;
        static Transform _player;

        InteractionSystem _interact; bool _wasChannel;
        PlayerController _pc; SkillCaster _caster; Combatant _self; NpcBrain _brain; TargetingSystem _targeting; MonoBehaviour _mastery;
        System.Func<bool> _gliding;
        Vector3 _lastPos; Vector3 _vel;
        bool _wasAir, _wasDodge, _wasDead, _wasDowned, _wasMounted, _wasGlide, _jumpPlayed;
        float _airTime, _combatTimer, _idleTimer = 4f;
        int _mountLeapSeen, _mountEngageSeen, _mountKnockSeen;
        bool _drawn;

        public float Speed => new Vector2(_vel.x, _vel.z).magnitude;
        public static string LastSkillClip { get; private set; }
        public static float LastSkillSpeed { get; private set; }
        /// <summary>QA: zuletzt gestartete Skill-Clips des Spielers (Zeit, Clip, Tempo).</summary>
        public static readonly System.Collections.Generic.List<string> ClipHistory = new System.Collections.Generic.List<string>();

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<CharacterAnimator>();
            if (Anim != null && string.IsNullOrEmpty(Prefix)) Prefix = Anim.ClipPrefix ?? "";
            if (Anim != null && !string.IsNullOrEmpty(Anim.WeaponStyle) && WeaponStyle == "Greatsword") WeaponStyle = Anim.WeaponStyle;
            _pc = GetComponent<PlayerController>(); _caster = GetComponent<SkillCaster>(); _self = GetComponent<Combatant>();
            _brain = GetComponent<NpcBrain>(); _targeting = GetComponent<TargetingSystem>(); _interact = GetComponent<InteractionSystem>();
            var ma = GetComponent<MasteryAbilities>(); if (ma != null) _gliding = () => ma.Gliding;
            _lastPos = transform.position;
        }

        void OnEnable()
        {
            if (_self != null) { _self.Damaged += OnDamaged; }
            if (_caster != null) { _caster.Swapped += OnSwapped; _caster.CastStarted += OnCastStarted; _caster.Interrupted += OnInterrupted; }
            if (_brain != null) { _brain.AttackStarted += OnNpcAttack; _brain.AttackLanded += OnNpcLanded; }
            if (Anim != null) Anim.MarkerReached += OnMarker;
            if (_pc != null) _pc.AnimDriven = true;
            if (_brain != null) _brain.AnimDriven = true;
        }

        void OnDisable()
        {
            if (_self != null) { _self.Damaged -= OnDamaged; }
            if (_caster != null) { _caster.Swapped -= OnSwapped; _caster.CastStarted -= OnCastStarted; _caster.Interrupted -= OnInterrupted; }
            if (_brain != null) { _brain.AttackStarted -= OnNpcAttack; _brain.AttackLanded -= OnNpcLanded; }
            if (Anim != null) Anim.MarkerReached -= OnMarker;
        }

        // ------------------------------------------------------------ Lauf 5 / M24: Klänge aus Animations-Markern und Zuständen
        float _hurtCd;
        string Faction => _brain != null && _brain.Archetype != null ? GameSoundMap.Faction(_brain.Archetype.Palette, Prefix.Length > 0) : (Prefix.Length > 0 ? "Beast" : "Bandit");

        void OnMarker(string type, string clip)
        {
            if (type != Marker.FootL && type != Marker.FootR) return;
            if (Speed < 0.4f || (_pc != null && _pc.Airborne && Mode == Role.Player)) return;
            switch (Mode)
            {
                case Role.Player: GameAudio.Step(transform, 0f, Random.Range(0.96f, 1.04f)); break;
                case Role.Mount: GameAudio.Step(transform, 1f, 0.72f); break;
                case Role.Critter: GameAudio.Step(transform, -12f, 1.4f, VoicePriority.AmbientDetail); break;
                default: GameAudio.Step(transform, -6f, Random.Range(0.92f, 1.06f)); break;
            }
        }

        string C(string n) => Prefix + n;
        string W(string n) => WeaponStyle + "_" + n;

        void Update()
        {
            if (Anim == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var raw = (transform.position - _lastPos) / dt; _lastPos = transform.position;
            if (raw.sqrMagnitude > 900f) raw = Vector3.zero;   // Teleport
            _vel = Vector3.Lerp(_vel, raw, 1f - Mathf.Exp(-dt * 12f));
            switch (Mode)
            {
                case Role.Player: TickPlayer(dt); break;
                case Role.Mount: TickMount(dt); break;
                default: TickNpc(dt); break;
            }
        }

        // ------------------------------------------------------------ Spieler
        void TickPlayer(float dt)
        {
            bool dead = _self != null && _self.IsDead, downed = _self != null && _self.IsDowned;
            bool mounted = _pc != null && _pc.MountedControl;
            bool glide = _gliding != null && _gliding();
            bool air = _pc != null && _pc.Airborne && !mounted;
            bool dodge = _pc != null && _pc.Dodge.IsDodging;
            Anim.SetMotion(mounted ? Vector3.zero : _vel, !air);
            Anim.IkEnabled = !mounted && !glide;

            if (dead) { if (!_wasDead) Anim.PlayFull("Death", 0.15f, 1f, true); _wasDead = true; return; }
            if (_wasDead) { Anim.StopAll(0.25f); _wasDead = false; }
            if (downed)
            {
                if (!_wasDowned) Anim.PlayFull("DownedCrawl", 0.2f, 0f);
                Anim.SetFullSpeed(Mathf.Clamp01(Speed / 1.5f));
                _wasDowned = true;
                if (_caster != null && _caster.IsCasting && _caster.CastingSkill.Special == SkillSpecial.Bandage && Anim.CurrentFull != "Bandage") Anim.PlayFull("Bandage", 0.2f);
                return;
            }
            if (_wasDowned) { Anim.PlayFull("Getup", 0.15f); _wasDowned = false; }
            if (mounted) { if (!_wasMounted) { Anim.PlayFull("Mount", 0.1f); GameAudio.Play("Mount/Call", transform.position); } else if (Anim.CurrentFull != "Ride" && Anim.CurrentFull != "Mount") Anim.PlayFull("Ride", 0.15f); _wasMounted = true; SheatheWeapons(true); return; }
            if (_wasMounted) { Anim.StopFull(0.2f); _wasMounted = false; }
            if (glide) { if (!_wasGlide) { Anim.PlayFull("Glide", 0.2f); GameAudio.Play("Move/GliderOpen", transform.position); } _wasGlide = true; return; }
            if (_wasGlide) { _wasGlide = false; Anim.PlayFull("JumpAir", 0.15f); GameAudio.Play("Move/GliderClose", transform.position); }
            if (dodge && !_wasDodge)
            {
                var l = transform.InverseTransformDirection(_pc.Dodge.Direction);
                string clip = Mathf.Abs(l.x) > Mathf.Abs(l.z) ? (l.x < 0f ? "DodgeL" : "DodgeR") : l.z < 0f ? "DodgeB" : "DodgeF";
                var e = Anim.Find(clip);
                Anim.PlayFull(clip, 0.08f, e != null ? e.Length / Mathf.Max(0.2f, _pc.Dodge.DodgeDuration) : 1f);
            }
            _wasDodge = dodge;
            if (!dodge)
            {
                if (air) { _airTime += dt; if (!_wasAir && _pc.VerticalVelocity > 1f) { Anim.PlayFull("JumpStart", 0.08f); _jumpPlayed = true; GameAudio.Play("Move/Jump", transform.position); } else if (_airTime > 0.25f && Anim.CurrentFull != "JumpAir") Anim.PlayFull("JumpAir", 0.15f); }
                else if (_wasAir) { if (_airTime > 0.35f || _jumpPlayed) { Anim.PlayFull("JumpLand", 0.06f); GameAudio.Play("Move/Land", transform.position, _airTime > 1f ? 2f : -2f); } else Anim.StopFull(0.12f); _airTime = 0f; _jumpPlayed = false; }
                _wasAir = air;
            }
            TickCasting();
            bool channel = _interact != null && _interact.IsChanneling;
            if (channel && !_wasChannel) { if (_interact.Channeling.ChannelTime >= 2f) Anim.PlayFull("Communion", 0.2f); else Anim.PlayUpper("Interact", 0.12f, Mathf.Clamp(1f / Mathf.Max(0.3f, _interact.Channeling.ChannelTime), 0.5f, 2f)); }
            else if (!channel && _wasChannel && Anim.CurrentFull == "Communion") Anim.StopFull(0.2f);
            _wasChannel = channel;
            if (_targeting != null && _targeting.Current != null && !_targeting.Current.IsDead)
                Anim.SetLook(_targeting.Current.transform.position + Vector3.up * 1.4f, 0.7f);
            else Anim.SetLook(null);
            _combatTimer -= dt;
            SheatheWeapons(_combatTimer <= 0f);
        }

        string _skillClip; bool _skillLoop;

        /// <summary>Lauf 5 / M22: Aktivierung beginnt → Clip und Tempo aus der Skill-Zeitachse (Clip-Länge × Faktor = Aktivierung + Nachwirkung).</summary>
        void OnCastStarted(SkillDefinition s)
        {
            if (Anim == null || s == null) return;
            var tl = s.Timeline;
            string clip = tl != null && tl.IsBuilt ? tl.Clip : SkillClip(s, _caster != null && _caster.IsAutoAttack(s), _caster != null ? _caster.Chain.Index : 0, WeaponStyle);
            float speed = tl != null && tl.IsBuilt ? tl.AnimSpeed : 1f;
            if (_self != null && _self.Effects.Has(EffectType.Quickness)) speed /= EffectRules.QuicknessCastFactor;
            var e = Anim.PlayAuto(clip, 0.1f, speed, s.Special == SkillSpecial.Bandage);
            _skillClip = e != null ? clip : null; _skillLoop = e != null && e.Loop;
            LastSkillClip = clip; LastSkillSpeed = speed;
            if (Mode == Role.Player) { ClipHistory.Add(Time.time.ToString("0.00") + " " + clip + " ×" + speed.ToString("0.00") + " " + (tl != null ? tl.SfxRelease : "")); if (ClipHistory.Count > 30) ClipHistory.RemoveAt(0); }
            _combatTimer = SheatheDelay;
        }

        void OnInterrupted(SkillDefinition s)
        {
            if (Anim == null) return;
            Anim.StopAll(0.08f);
            Anim.PlayHit("HitHeavy");
            _skillClip = null;
        }

        void TickCasting()
        {
            if (_caster == null) return;
            // Schleifen-Clips (Kanal, Verbinden) enden mit der Aktivierung
            if (_skillLoop && !_caster.IsCasting) { Anim.StopAll(0.2f); _skillLoop = false; _skillClip = null; }
        }

        /// <summary>Fertigkeit → Clip (rein; Tests). Waffenfertigkeiten nach Lieferart, Hilfsfertigkeiten nach Kategorie.</summary>
        public static string SkillClip(SkillDefinition s, bool auto, int chain, string style)
        {
            if (s == null) return null;
            if (s.Special == SkillSpecial.Bandage) return "Bandage";
            if (s.SlotType == SkillSlotType.Downed) return "Cast1H";
            if (auto) return style + "_Auto" + (Mathf.Clamp(chain, 0, 2) + 1);
            if (s.SlotType == SkillSlotType.Weapon)
            {
                if (s.Finisher == ComboFinisherType.Whirl || (s.Hits >= 3 && s.Delivery == SkillDelivery.Self)) return style + "_Whirl";
                switch (s.Delivery)
                {
                    case SkillDelivery.Leap: return style + "_Thrust";
                    case SkillDelivery.Projectile: return style == "Staff" || style == "Scepter" ? "Cast1H" : style + "_Shoot";
                    case SkillDelivery.GroundTarget: return "Cast2H";
                    case SkillDelivery.Self:
                        if (s.EvadesDuringCast) return "DodgeF";
                        if (s.DealsDamage && s.Field == ComboFieldType.None) return style + "_Heavy";   // Bodenstoß
                        if (s.Field != ComboFieldType.None) return "Cast2H";                           // Feld setzen
                        return style + "_Block";
                    default: return s.CastTime >= 0.6f ? style + "_Heavy" : style + "_Thrust";
                }
            }
            if (s.SlotType == SkillSlotType.Elite) return "Cast2H";
            if (s.Category == UtilityCategory.Shout) return "Cheer";
            if (s.Category == UtilityCategory.Meditation) return s.CastTime > 0f ? "CastChannel" : "Cast1H";
            if (s.Category == UtilityCategory.Trap) return "Interact";
            return "Cast1H";
        }

        void OnSwapped() { if (Anim != null) Anim.PlayUpper("WeaponSwap", 0.1f); _combatTimer = SheatheDelay; }

        void OnDamaged(DamageInfo d)
        {
            if (Anim == null || d.Amount <= 0f || d.Evaded || d.Missed) return;
            bool heavy = _self != null && _self.MaxHealth > 0f && d.Amount > _self.MaxHealth * 0.12f;
            if (_brain != null && Time.time > _hurtCd) { _hurtCd = Time.time + 1.5f; GameAudio.Play("Vox/" + Faction + "/Hurt", transform.position, -6f, Random.Range(0.94f, 1.06f), VoicePriority.Hit); }
            if (Prefix.Length > 0) { Anim.PlayHit(C("Hit")); _combatTimer = SheatheDelay; return; }
            Anim.PlayHit(heavy ? "HitHeavy" : HitClipFor(transform, d.Source != null ? d.Source.transform.position : d.Point));
            _combatTimer = SheatheDelay;
        }

        /// <summary>Treffer-Reaktion passend zur Richtung des Angreifers (vorn/hinten/links/rechts).</summary>
        public static string HitClipFor(Transform self, Vector3 from)
        {
            var l = self.InverseTransformPoint(from); l.y = 0f;
            if (l.sqrMagnitude < 1e-4f) return "HitLight";
            if (Mathf.Abs(l.x) > Mathf.Abs(l.z)) return l.x < 0f ? "HitLightL" : "HitLightR";
            return l.z >= 0f ? "HitLight" : "HitLightB";
        }

        System.Collections.Generic.Dictionary<Transform, Transform> _homes;
        void SheatheWeapons(bool sheathe)
        {
            if (HandSocket == null || BackSocket == null || Weapons == null) return;
            if (_drawn == !sheathe && Weapons.Length > 0 && Weapons[0] != null && Weapons[0].parent == (sheathe ? BackSocket : HandSocket)) return;
            _drawn = !sheathe;
            if (_homes == null) { _homes = new System.Collections.Generic.Dictionary<Transform, Transform>(); foreach (var w in Weapons) if (w != null) _homes[w] = w.parent == BackSocket ? HandSocket : w.parent; }
            foreach (var w in Weapons)
            {
                if (w == null) continue;
                w.SetParent(sheathe ? BackSocket : (_homes.TryGetValue(w, out var home) && home != null ? home : HandSocket), false);
                w.localPosition = Vector3.zero; w.localRotation = Quaternion.identity;
            }
        }

        // ------------------------------------------------------------ NPC / Dorfbewohner / Tiere
        void TickNpc(float dt)
        {
            bool dead = _self != null && _self.IsDead;
            Anim.SetMotion(_vel, true);
            if (dead) { if (!_wasDead) { Anim.PlayFull(C("Death"), 0.12f, 1f, true); if (_brain != null) GameAudio.Play("Vox/" + Faction + "/Death", transform.position, -3f, 1f, VoicePriority.Hit); } _wasDead = true; return; }
            if (_wasDead) { Anim.StopAll(0.2f); _wasDead = false; }
            if (_brain != null && _brain.Target != null && !_brain.Target.IsDead) Anim.SetLook(_brain.Target.transform.position + Vector3.up * 1.4f, 0.6f);
            else if (LookAtPlayer)
            {
                if (_player == null) { var p = GameObject.FindWithTag("Player"); if (p != null) _player = p.transform; }
                if (_player != null && (_player.position - transform.position).sqrMagnitude < 36f) Anim.SetLook(_player.position + Vector3.up * 1.6f, 0.8f); else Anim.SetLook(null);
            }
            else Anim.SetLook(null);
            // gelegentliche Idle-Variation (Umschauen / Schnüffeln)
            if (Speed < 0.2f && Anim.CurrentFull == null && Anim.CurrentUpper == null && Mode != Role.Villager)
            {
                _idleTimer -= dt;
                if (_idleTimer <= 0f) { _idleTimer = 6f + Random.value * 6f; Anim.PlayFull(C(Prefix.Length > 0 ? "Sniff" : "Idle2"), 0.25f); }
            }
            else if (Anim.CurrentFull != null && (Anim.CurrentFull.EndsWith("Sniff") || Anim.CurrentFull == "Idle2") && Speed > 0.4f) Anim.StopFull(0.2f);
        }

        /// <summary>NPC-Angriff: Ausholen exakt über die Wind-up-Zeit (≥ 0,6 s), Treffer-Marker fällt auf den Schadenszeitpunkt.</summary>
        void OnNpcAttack(Combatant target)
        {
            if (Anim == null || _brain == null || _brain.Archetype == null) return;
            float windup = NpcBrain.EffectiveWindup(_brain.Archetype);
            string clip = Prefix.Length > 0 ? C("Attack") : !string.IsNullOrEmpty(NpcAttackClip) ? NpcAttackClip : W("Heavy");
            var e = Anim.Find(clip); if (e == null) return;
            float hit = e.MarkerTime(Marker.Hit); if (hit < 0f) hit = e.MarkerTime(Marker.Projectile); if (hit < 0f) hit = e.Length * 0.6f;
            Anim.PlayAuto(clip, 0.1f, Mathf.Clamp(hit / windup, 0.3f, 3f));
            // Lauf 5 / M23: Aufladung an der Hand (Gegner-Telegraphen lauter als Spieler-Effekte)
            var pal = _brain.Archetype.Palette;
            var size = _self != null && _self.Tier == EnemyTier.Champion ? VfxSize.L : VfxSize.M;
            var hand = Anim.transform.Find("Body/Rig") != null ? FindDeep(Anim.transform, "HandSocket_R") : null;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Charge, pal, size), hand != null ? hand.position : transform.position + Vector3.up * 1.2f, transform.rotation, hand, 1f);
            // Lauf 5 / M24: Telegraph-Aufladung (steigender Ton) + Angriffslaut der Fraktion
            GameAudio.Play("Enemy/Telegraph", transform.position, -6f, Mathf.Clamp(0.9f / windup, 0.7f, 1.4f), VoicePriority.EnemyTelegraph);
            GameAudio.Play("Vox/" + Faction + "/Attack", transform.position, -8f, Random.Range(0.94f, 1.06f), VoicePriority.Hit);
        }

        void OnNpcLanded(Combatant target)
        {
            if (_brain == null || _brain.Archetype == null || target == null) return;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Impact, _brain.Archetype.Palette, VfxSize.M), target.AimPoint, Quaternion.identity, null, 1f);
            if (Prefix.Length == 0) GameAudio.Play("Swish/Heavy", transform.position, -4f, 1f, VoicePriority.Hit);
        }

        static Transform FindDeep(Transform t, string n) { foreach (var c in t.GetComponentsInChildren<Transform>()) if (c.name == n) return c; return null; }

        // ------------------------------------------------------------ Reittier
        void TickMount(float dt)
        {
            var mc = GetComponentInParent<MountController>();
            Anim.SetMotion(_vel, true);
            if (mc == null) return;
            var m = mc.Motor;
            var pc = mc.GetComponent<PlayerController>();
            bool air = pc != null && pc.Airborne;
            if (m.LeapCount != _mountLeapSeen) { _mountLeapSeen = m.LeapCount; Anim.PlayFull(C("MLeap"), 0.08f); GameAudio.Play("Mount/Ability", transform.position); }
            if (m.EngageCount != _mountEngageSeen) { _mountEngageSeen = m.EngageCount; Anim.PlayFull(C("MEngage"), 0.08f, 1.5f); GameAudio.Play("Mount/Engage", transform.position, 0f, 1f, VoicePriority.PlayerSkill); }
            if (m.KnockOffCount != _mountKnockSeen) { _mountKnockSeen = m.KnockOffCount; Anim.PlayFull(C("MKnockOff"), 0.08f); GameAudio.Play("Mount/Dismount", transform.position); }
            if (m.Charging) { if (Anim.CurrentFull != C("MCharge")) Anim.PlayFull(C("MCharge"), 0.15f); }
            else if (pc != null && pc.IsHovering && Prefix.StartsWith("Ray")) { if (Anim.CurrentFull != C("MHover")) Anim.PlayFull(C("MHover"), 0.2f); }
            else if (air && Anim.CurrentFull == null) Anim.PlayFull(C("MHover"), 0.2f);
            else if (!air && (Anim.CurrentFull == C("MCharge") || Anim.CurrentFull == C("MHover"))) Anim.StopFull(0.15f);
        }
    }
}
