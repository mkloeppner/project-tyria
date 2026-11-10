using System;
using UnityEngine;
using UnityEngine.AI;
using ProjectTyrant.Balance;

namespace ProjectTyrant
{
    public enum NpcState { Idle, Aggro, Attack, Reset, Dead }

    /// <summary>
    /// Einfache State Machine für Gegner und freundliche NPCs: Idle → Aggro → Attack → Reset.
    /// Champions setzen zusätzlich Boden-Telegraphs (1,5 s), die per Dodge vermeidbar sind.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class NpcBrain : MonoBehaviour
    {
        public EnemyArchetype Archetype;
        public Transform Visual;
        public bool UseLeash = true;
        public bool DestroyOnDeath = true;
        public float WanderRadius = 3f;

        /// <summary>Optionales Marschziel (Event-Angriffe). Gegner laufen dorthin, solange kein Ziel in Aggro-Reichweite ist.</summary>
        [NonSerialized] public Vector3? Objective;
        /// <summary>Bevorzugtes Ziel (z. B. Vorratskisten).</summary>
        public Combatant PreferredTarget;
        [Header("Balance B1")]
        [Tooltip("Neutral (gelber Name): greift erst an, wenn angegriffen")] public bool Neutral;
        [Tooltip("Gruppe/Lager für Rudel-Aggro")] public string SpawnGroup;
        [Tooltip("Welt-Spawn: kehrt nach der Respawn-Zeit der Gefahrenstufe zurück")] public bool WorldSpawn;
        [NonSerialized] public bool Provoked;
        [Tooltip("Rolle dieses Spawns (Standard = Rolle des Archetyps): Jäger, Rufer, Lauerer im Spätspiel")] public EnemyRole RoleOverride;
        public EnemyRole EffectiveRole => RoleOverride != EnemyRole.Standard ? RoleOverride : Archetype != null ? Archetype.Role : EnemyRole.Standard;
        /// <summary>≥ 0: Gefahrenstufe erzwingen (Messungen, Events), sonst Stufe am Heimatpunkt.</summary>
        [NonSerialized] public int DangerOverride = -1;
        /// <summary>Gelb = neutral und noch nicht angegriffen.</summary>
        public bool IsYellow => IsNeutralNow && !Provoked;
        /// <summary>Neutral nach Daten bzw. Admin-Überschreibung (/npc neutral).</summary>
        public bool IsNeutralNow => !BalanceWorld.Legacy && Self != null && Self.Team == Team.Enemy && (BalanceWorld.NeutralOverride ?? Neutral);
        PursuitModel _pursuit; DangerSettings _danger; int _dangerLevel; ControlGate _ctrl;
        float _lastHitByTargetAt = -99f, _downedSince = -1f, _attackCoef, _dmgScale = 1f, _losCheck, _ignorePlayersUntil = -1f, _respawnAt = -1f;
        bool _los = true, _hidden;
        public int DangerLevel => _dangerLevel;
        public DangerSettings Danger => _danger ?? BalanceWorld.Profile(2);
        public PursuitModel Pursuit => _pursuit;
        /// <summary>Faktor Kurve/Archetyp für Sonderangriffe (NpcAbilities).</summary>
        public float DamageScale => _dmgScale;
        /// <summary>Sonderangriffe (Stampfer, Wurf, Sturmangriff): Kurven-Faktor, gedeckelt durch den Telegraph-Anteil der Gefahrenstufe.</summary>
        public float AbilityCoefficient(float raw)
        {
            if (BalanceWorld.Legacy || Archetype == null || Archetype.CurveExempt || Self.Team != Team.Enemy || _danger == null) return raw;
            return Mathf.Min(raw * _dmgScale, EnemyCurve.TelegraphCoefficient(Self.Level, _danger, Archetype.AttributeFactor));
        }
        bool UsePursuit => _pursuit != null && !BalanceWorld.LegacyPursuit && Self.Team == Team.Enemy && (Archetype == null || !Archetype.PursuitExempt);
        static float Now => Time.time;

        public NpcState State { get; private set; } = NpcState.Idle;
        public Combatant Target { get; private set; }
        public Vector3 HomePosition { get; set; }
        public Combatant Self { get; private set; }
        public event Action<NpcState> StateChanged;
        /// <summary>Lauf 4: Angriff ausgeholt / getroffen (für Verhaltens-Komponenten, z. B. Geschosse).</summary>
        public event Action<Combatant> AttackStarted, AttackLanded;
        /// <summary>Lauf 4: Verhalten blockiert Bewegung/Angriffe (Hinterhalt, Sturmangriff).</summary>
        [NonSerialized] public bool Suspended;
        /// <summary>Lauf 5 / M25: Gefährte im Passiv-Modus – sucht sich keine Ziele.</summary>
        [NonSerialized] public bool Passive;
        Vector3? _searchPoint; float _searchTimer;
        /// <summary>Ziel aufgeben und zurückkehren (Gefährte F3).</summary>
        public void ClearTarget() { Target = null; if (State == NpcState.Aggro || State == NpcState.Attack) { InterruptAttack(); SetState(NpcState.Idle); } }

        float _attackTimer, _windup, _telegraphTimer, _deathTimer, _wanderTimer, _bob;
        bool _windingUp;
        GroundTelegraph _activeTelegraph;
        Vector3 _wanderPoint;
        Collider _collider;
        NavMeshPath _path;
        readonly Vector3[] _corners = new Vector3[32];
        int _cornerCount, _cornerIdx;
        float _repathTimer;
        Vector3 _pathTarget = new Vector3(1e6f, 0, 0);

        void Awake() => Init();

        public void Init()
        {
            if (Self != null) return;
            Self = GetComponent<Combatant>();
            _collider = GetComponent<Collider>();
            HomePosition = transform.position;
            _wanderPoint = HomePosition;
            Self.Damaged += OnDamaged;
            Self.Died += OnDied;
            ApplyArchetype();
        }

        [Tooltip(">0 überschreibt das Gebietslevel (Events, Herausforderungen).")]
        public int LevelOverride;
        FlatAttributeSource _armorSource;

        public void ApplyArchetype()
        {
            if (Archetype == null || Self == null) return;
            Self.DisplayName = Archetype.DisplayName;
            Self.Tier = Archetype.Tier;
            Self.Team = Archetype.Team;
            int lvl = LevelOverride > 0 ? LevelOverride : Archetype.PickLevel(transform.position);
            Self.Stats.Level = lvl;
            Self.Stats.PrimaryBaseFactor = Archetype.AttributeFactor;
            if (_armorSource == null) { _armorSource = new FlatAttributeSource(); Self.Stats.AddSource(_armorSource); }
            _armorSource.Defense = AttributeSet.PrimaryBase(lvl) * Archetype.ArmorFactor;
            Self.Stats.MarkDirty();
            float hp = Archetype.MaxHealth > 0f ? Archetype.MaxHealth : EnemyArchetype.NpcBaseHealth(lvl) * Archetype.HealthFactor;
            // Balance B1: Gefahrenstufe am Heimatpunkt → Kurve (Lebenspunkte, Angriff) und Verfolgungsprofil
            _dangerLevel = DangerOverride >= 0 ? DangerOverride : BalanceWorld.DangerAt(HomePosition == Vector3.zero ? transform.position : HomePosition);
            _danger = BalanceWorld.Profile(_dangerLevel);
            _attackCoef = Archetype.AttackCoefficient; _dmgScale = 1f;
            if (!BalanceWorld.Legacy && Self.Team == Team.Enemy && !Archetype.CurveExempt && Archetype.MaxHealth <= 0f)
            {
                hp = EnemyCurve.Health(lvl, Archetype.Tier, _danger, EffectiveRole);
                _attackCoef = EnemyCurve.Coefficient(lvl, Archetype.Tier, _danger, Archetype.AttackInterval, Archetype.AttributeFactor, EffectiveRole);
                _dmgScale = _attackCoef / Mathf.Max(0.01f, Archetype.AttackCoefficient);
            }
            var prof = PursuitProfile.From(_danger, EffectiveRole == EnemyRole.Hunter, IsNeutralNow);
            prof.NoLeash = BalanceWorld.NoLeashAt(HomePosition == Vector3.zero ? transform.position : HomePosition);
            _pursuit = new PursuitModel(prof);
            if (_ctrl == null) _ctrl = new ControlGate(Mathf.RoundToInt(transform.position.x * 7.31f + transform.position.z * 3.17f) * 31 + (Archetype != null ? Archetype.name.Length : 0));   // deterministisch je Spawn
            Self.SetMaxHealth(hp, true);
            Self.Height = 1.8f * Archetype.Scale;
            transform.localScale = Vector3.one * Archetype.Scale;
            _telegraphTimer = Archetype.TelegraphInterval * 0.5f;
        }

        void OnDestroy()
        {
            if (Self != null) { Self.Damaged -= OnDamaged; Self.Died -= OnDied; }
            if (_activeTelegraph != null) _activeTelegraph.Cancel();
        }

        void SetState(NpcState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        void OnDamaged(DamageInfo info)
        {
            if (State == NpcState.Reset || State == NpcState.Dead) return;
            if (info.Source != null && !info.Source.IsDead && Self.IsHostileTo(info.Source))
            {
                Provoked = true;
                if (info.Source == Target || Target == null) _lastHitByTargetAt = Now;
                // Balance B1: Wachen übernehmen die Aggro (Stufe 0–2)
                if (!BalanceWorld.Legacy && Target != null && Target.Team == Team.Player && _danger != null && ThreatModel.SwitchToAttacker(_dangerLevel, true, info.Source.Team == Team.Ally, _danger.GuardsHelp)) Target = info.Source;
                if (Target == null || State == NpcState.Idle)
                {
                    Target = info.Source; _lastHitByTargetAt = Now;
                    if (_pursuit != null) _pursuit.Engage(Now, true);
                    AlertPack(info.Source);
                    SetState(NpcState.Aggro);
                }
                // Balance B1: neutrale Tiere fliehen in Stufe 0 bei unter 30 % HP
                if (!BalanceWorld.Legacy && IsNeutralNow && _dangerLevel == 0 && Self.HealthFraction < 0.3f && !Self.IsDead) GiveUp(GiveUpReason.None, "flieht");
            }
        }

        void OnDied(Combatant killer)
        {
            SetState(NpcState.Dead);
            InterruptAttack();
            if (_collider != null) _collider.enabled = false;
            _deathTimer = 0f;
        }

        public void InterruptAttack()
        {
            _windingUp = false;
            if (_activeTelegraph != null) { _activeTelegraph.Cancel(); _activeTelegraph = null; }
        }

        /// <summary>Belebt den NPC am Heimatpunkt wieder (Event-Reset).</summary>
        public void Revive()
        {
            InterruptAttack();
            if (_hidden) Hide(false);
            Provoked = false; _respawnAt = -1f;
            if (_pursuit != null) _pursuit.Arrived();
            Self.Revive(1f);
            Self.ManualInvulnerable = false;
            Target = null;
            Objective = null;
            transform.position = HomePosition;
            if (_collider != null) _collider.enabled = true;
            if (Visual != null) { Visual.localRotation = Quaternion.identity; Visual.localPosition = Vector3.zero; }
            SetState(NpcState.Idle);
        }

        public void ForceAggro(Combatant c) => ForceAggro(c, false);

        /// <summary>Aggro erzwingen (Events, Rudel, Messungen); <paramref name="hesitate"/> = Zögern der Stufe beachten (Rudel).</summary>
        public void ForceAggro(Combatant c, bool hesitate)
        {
            if (c == null || Self.IsDead || State == NpcState.Reset) return;
            Target = c; Provoked = true;
            if (_pursuit != null) _pursuit.Engage(Now, !hesitate);
            SetState(NpcState.Aggro);
        }

        /// <summary>Balance B1: Rudel-Aggro nach Stufe (0 keine · 1 Partner · ab 2 Gruppe im Radius 300).</summary>
        void AlertPack(Combatant target)
        {
            if (BalanceWorld.Legacy || _danger == null || string.IsNullOrEmpty(SpawnGroup) || target == null) return;
            if (_danger.Pack == PackMode.None) return;
            var list = new System.Collections.Generic.List<(NpcBrain b, float d)>();
            foreach (var c in Combatant.All)
            {
                if (c == null || c == Self || c.IsDead || c.Team != Self.Team) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b == null || b.SpawnGroup != SpawnGroup || b.State != NpcState.Idle) continue;
                list.Add((b, Vector3.Distance(b.transform.position, transform.position)));
            }
            list.Sort((x, y) => x.d.CompareTo(y.d));
            for (int i = 0; i < list.Count; i++)
                if (PackRules.Joins(_danger, true, list[i].d, i)) list[i].b.ForceAggro(target, true);
        }

        /// <summary>Balance B1: Fessel erlaubt? (Stufe, Quote, 6 s je Gegner) – Schadenszustände sind immer erlaubt.</summary>
        public bool ControlAllowed(EffectType t) => ControlAllowed(DangerTable.KindOf(t));
        public bool ControlAllowed(ControlKinds k)
        {
            if (k == ControlKinds.None) return true;
            if (_ctrl == null || _danger == null || BalanceWorld.LegacyPursuit) return true;
            return _ctrl.TryUse(_danger, k, Now);
        }

        /// <summary>
        /// Balance B1: Fessel-Einsatz der Gefahrenstufe auf Treffer (Quote und 6-s-Abklingzeit über <see cref="ControlAllowed(ControlKinds)"/>):
        /// Stufe 1 Verkrüppelung, 2 +Kühle, ab 3 Immobilisierung (1 s) – so werden Verfolger im Spätspiel „klebrig“.
        /// </summary>
        void ApplyStageFetter(Combatant t)
        {
            if (BalanceWorld.Legacy || _danger == null || _danger.Controls == ControlKinds.None || Archetype == null || Archetype.CurveExempt) return;
            ControlKinds k = DangerTable.Allows(_danger, ControlKinds.Immobilize) && Mathf.Repeat(Now * 0.37f, 1f) < 0.35f ? ControlKinds.Immobilize
                           : DangerTable.Allows(_danger, ControlKinds.Chill) && Mathf.Repeat(Now * 0.53f, 1f) < 0.5f ? ControlKinds.Chill : ControlKinds.Cripple;
            if (!DangerTable.Allows(_danger, k) || !ControlAllowed(k)) return;
            var type = k == ControlKinds.Immobilize ? EffectType.Immobilize : k == ControlKinds.Chill ? EffectType.Chill : EffectType.Cripple;
            t.Effects.Apply(new EffectApplication(type, 1, k == ControlKinds.Immobilize ? 1f : 2.5f), Self);
        }

        /// <summary>Balance B1: Verfolgung aufgeben (GW2-Reset: unverwundbar, Rückkehr, volle Heilung).</summary>
        void GiveUp(GiveUpReason reason, string note = null)
        {
            if (Target != null && Target.Team == Team.Player)
            {
                BalanceTelemetry.Record(TelemetryKind.GiveUp, transform.position, Self.DisplayName, Self.Tier, note ?? reason.ToString());
                if (_danger != null && _danger.GiveUpHint && BalanceWorld.FirstTime("giveup"))
                    GameEventBus.Notify(Self.DisplayName + " gibt auf – wer wegläuft, entkommt oft.", new Color(0.7f, 1f, 0.7f));
            }
            if (Objective.HasValue) _ignorePlayersUntil = Now + 5f;
            BeginReset();
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (Self == null || Archetype == null) return;
            if (Self.IsDead) { TickDead(dt); return; }
            if (Self.Effects.Has(EffectType.Stun) || Suspended) { AnimateVisual(dt, false); return; }

            switch (State)
            {
                case NpcState.Idle: TickIdle(dt); break;
                case NpcState.Aggro: TickAggro(dt); break;
                case NpcState.Attack: TickAttack(dt); break;
                case NpcState.Reset: TickReset(dt); break;
            }
        }

        bool ValidTarget(Combatant c) => c != null && !c.IsDead && c.isActiveAndEnabled && !c.Effects.Has(EffectType.Stealth) && Self.IsHostileTo(c);

        float AggroRadius => UsePursuit ? _pursuit.Profile.AggroRange : Archetype.AggroRange;

        /// <summary>Balance B1: Ziel-Filter – Wachen greifen keine gelben (neutralen) Kreaturen an; Gegner ignorieren Spieler in Sicherheitszonen.</summary>
        bool Eligible(Combatant c)
        {
            if (c == null) return false;
            var b = c.GetComponent<NpcBrain>();
            if (Self.Team != Team.Enemy && b != null && b.IsYellow) return false;
            if (Self.Team == Team.Enemy && c.Team == Team.Player)
            {
                if (Now < _ignorePlayersUntil) return false;
                if (UsePursuit && BalanceWorld.InSafezone(c.transform.position)) return false;
            }
            return true;
        }

        Combatant Acquire()
        {
            if (Passive) return null;
            if (IsNeutralNow && !Provoked) return null;   // gelb: nur nach Angriff
            if (ValidTarget(PreferredTarget) && Vector3.Distance(PreferredTarget.transform.position, transform.position) < Archetype.AggroRange * 1.5f)
            {
                // Spieler in unmittelbarer Nähe hat Vorrang vor Kisten
                var near = Combatant.FindNearestHostile(Self, transform.position, Archetype.AttackRange * 2.5f, c => c.Team == Team.Player && Eligible(c));
                return near != null ? near : PreferredTarget;
            }
            return Combatant.FindNearestHostile(Self, transform.position, AggroRadius, Eligible);
        }

        void TickIdle(float dt)
        {
            var t = Acquire();
            if (t != null)
            {
                Target = t;
                if (_pursuit != null) _pursuit.Engage(Now, false);
                AlertPack(t);
                SetState(NpcState.Aggro); return;
            }
            if (Objective.HasValue)
            {
                MoveTowards(Objective.Value, Archetype.MoveSpeed, dt, 1.5f);
                return;
            }
            _wanderTimer -= dt;
            if (_wanderTimer <= 0f)
            {
                _wanderTimer = 3f + UnityEngine.Random.value * 4f;
                var r = UnityEngine.Random.insideUnitCircle * WanderRadius;
                _wanderPoint = HomePosition + new Vector3(r.x, 0f, r.y);
            }
            MoveTowards(_wanderPoint, Archetype.MoveSpeed * 0.35f, dt, 0.3f);
        }

        void TickAggro(float dt)
        {
            if (Passive) { ClearTarget(); return; }
            if (!ValidTarget(Target))
            {
                // Lauf 5 / M25: Tarnung – zum letzten bekannten Punkt laufen und dort kurz suchen
                if (Target != null && !Target.IsDead && Target.Effects.Has(EffectType.Stealth)) { _searchPoint = Target.transform.position; _searchTimer = 4f; }
                Target = Acquire();
                if (Target == null)
                {
                    if (_searchPoint.HasValue && _searchTimer > 0f) { _searchTimer -= dt; MoveTowards(_searchPoint.Value, Archetype.MoveSpeed * 0.7f, dt, 1f); return; }
                    _searchPoint = null; BeginReset(); return;
                }
            }
            float speed = Archetype.MoveSpeed;
            if (UsePursuit && Target.Team == Team.Player)
            {
                if (TickPursuit(dt)) return;
                if (_pursuit.State == PursuitState.Hesitate) { Face(Target.transform.position, dt); AnimateVisual(dt, false); return; }
                speed = _pursuit.ChaseSpeed(PlayerChaseBase, Now, _danger.HastePhases) * EnemyCurve.RoleSpeed(EffectiveRole);
            }
            else if (UseLeash && !Objective.HasValue && Vector3.Distance(transform.position, HomePosition) > Archetype.LeashRange) { BeginReset(); return; }
            float dist = FlatDistance(Target.transform.position);
            if (dist <= Archetype.AttackRange) { SetState(NpcState.Attack); return; }
            MoveTowards(Target.transform.position, speed, dt, Archetype.AttackRange * 0.8f);
        }

        /// <summary>Spieler-Grundtempo für die Verfolgung (m/s, PlayerController.MoveSpeed).</summary>
        public static float PlayerChaseBase = 7f;

        /// <summary>Balance B1: Aufgabe-Regeln und Downed-Gnade; true = Gegner hat aufgegeben.</summary>
        bool TickPursuit(float dt)
        {
            var t = Target;
            _losCheck -= dt;
            if (_losCheck <= 0f)
            {
                _losCheck = 0.25f;
                Vector3 a = transform.position + Vector3.up * 1.5f, b = t.transform.position + Vector3.up * 1.2f;
                _los = !Physics.Linecast(a, b, 1, QueryTriggerInteraction.Ignore);
            }
            bool isLocal = GameManager.Instance != null && t == GameManager.Instance.Player;
            var mc = isLocal ? MountController.Local : null;
            var input = new PursuitInput
            {
                Now = Now,
                DistanceFromSpawn = Objective.HasValue ? 0f : FlatDistance(HomePosition),
                DistanceToTarget = FlatDistance(t.transform.position),
                HasLineOfSight = _los,
                TargetHitMeAt = _lastHitByTargetAt,
                TargetDealtDamageAt = isLocal ? BalanceWorld.PlayerDealtDamageAt : Now,
                TargetMounted = mc != null && mc.Motor.IsMounted,
                TargetTravelled = isLocal && BalanceWorld.PlayerTravelledAt > _pursuit.ChaseStart,
                TargetInSafezone = BalanceWorld.InSafezone(t.transform.position),
            };
            var r = _pursuit.Tick(input);
            if (r != GiveUpReason.None) { GiveUp(r); return true; }
            // Downed-Gnade
            if (t.IsDowned)
            {
                if (_downedSince < 0f) _downedSince = Now;
                if (_danger.DownedMode <= 1)
                {
                    var guard = Combatant.FindNearestHostile(Self, transform.position, 15f, c => c.Team == Team.Ally);
                    if (guard != null) { Target = guard; _downedSince = -1f; return false; }
                    if (_danger.DownedMode == 0 && Now - _downedSince > 6f) { GiveUp(GiveUpReason.None, "Downed-Gnade"); return true; }
                }
            }
            else _downedSince = -1f;
            return false;
        }

        void TickAttack(float dt)
        {
            if (!ValidTarget(Target)) { InterruptAttack(); SetState(NpcState.Aggro); Target = null; return; }
            if (UsePursuit && Target.Team == Team.Player && !_windingUp && _activeTelegraph == null && TickPursuit(dt)) return;
            float dist = FlatDistance(Target.transform.position);
            Face(Target.transform.position, dt);

            if (_activeTelegraph != null)
            {
                if (_activeTelegraph.Resolved) _activeTelegraph = null;
                AnimateVisual(dt, true);
                return; // während des Telegraphs steht der Champion
            }

            if (Archetype.HasTelegraph)
            {
                _telegraphTimer -= dt;
                if (_telegraphTimer <= 0f)
                {
                    _telegraphTimer = Archetype.TelegraphInterval;
                    float tcoef = BalanceWorld.Legacy || Archetype.CurveExempt || Self.Team != Team.Enemy ? Archetype.TelegraphCoefficient : EnemyCurve.TelegraphCoefficient(Self.Level, _danger, Archetype.AttributeFactor) * (Archetype.Tier == EnemyTier.Champion ? 1.5f : 1f);
                    _activeTelegraph = GroundTelegraph.Spawn(Self, Target.transform.position, Archetype.TelegraphRadius, Mathf.Max(Archetype.TelegraphDelay, BalanceWorld.Legacy ? 0f : Danger.MinTelegraph), 0f, tcoef);
                    GameEventBus.Notify(Self.DisplayName + " holt zum Erdstoß aus – ausweichen!", new Color(1f, 0.45f, 0.3f));
                    return;
                }
            }

            if (dist > Archetype.AttackRange * 1.3f && !_windingUp) { SetState(NpcState.Aggro); return; }

            _attackTimer -= dt;
            // Balance B1: Feindland/Verderbt (Stufe 4–5) – Verfolger holen im Laufen aus und bleiben am fliehenden Spieler „kleben“
            if (_windingUp && UsePursuit && _danger.HastePhases && Target.Team == Team.Player && dist > Archetype.AttackRange * 0.8f)
                MoveTowards(Target.transform.position, _pursuit.ChaseSpeed(PlayerChaseBase, Now, true) * EnemyCurve.RoleSpeed(EffectiveRole), dt, Archetype.AttackRange * 0.8f);
            if (_windingUp)
            {
                _windup -= dt;
                if (_windup <= 0f)
                {
                    _windingUp = false;
                    if (FlatDistance(Target.transform.position) <= Archetype.AttackRange * 1.4f)
                    {
                        float coef = _attackCoef;
                        // Stufe 4–5: Veteranen „stampfen“ auf Downed-Spieler (GW2-Stomp-Äquivalent)
                        if (!BalanceWorld.Legacy && Target.IsDowned && _danger != null && _danger.DownedMode >= 3 && Archetype.Tier >= EnemyTier.Veteran) coef *= 2f;
                        float dealt = DamageCalculator.DealDirect(Self, Target, coef);
                        if (dealt > 0f && Archetype.OnHitDebuff.Stacks > 0 && ControlAllowed(Archetype.OnHitDebuff.Type)) Target.Effects.Apply(Archetype.OnHitDebuff, Self);
                        else if (dealt > 0f && Target.Team == Team.Player) ApplyStageFetter(Target);
                        if (dealt > 0f) AttackLanded?.Invoke(Target);
                    }
                }
            }
            else if (_attackTimer <= 0f)
            {
                _attackTimer = Archetype.AttackInterval;
                _windingUp = true;
                _windup = Windup;
                AttackStarted?.Invoke(Target);
            }
            AnimateVisual(dt, _windingUp);
        }

        void BeginReset()
        {
            InterruptAttack();
            Target = null; _downedSince = -1f;
            if (_pursuit != null) { _pursuit.Drop(); if (Objective.HasValue) _pursuit.Arrived(); }
            if (Objective.HasValue) { SetState(NpcState.Idle); return; }
            Self.ManualInvulnerable = true;
            SetState(NpcState.Reset);
        }

        void TickReset(float dt)
        {
            Self.Heal(Self.MaxHealth * 0.25f * dt);
            if (MoveTowards(HomePosition, Archetype.MoveSpeed * 1.5f, dt, 1.2f))
            {
                Self.ManualInvulnerable = false;
                Self.Heal(Self.MaxHealth);
                Self.Effects.ClearConditions();
                Self.Attackers.Clear();
                Provoked = false;
                if (_pursuit != null) _pursuit.Arrived();
                SetState(NpcState.Idle);
            }
        }

        void TickDead(float dt)
        {
            _deathTimer += dt;
            if (Visual != null)
            {
                if (!AnimDriven) Visual.localRotation = Quaternion.Slerp(Visual.localRotation, Quaternion.Euler(-85f, 0f, 0f), dt * 6f);
                if (DestroyOnDeath && _deathTimer > 2f) Visual.localPosition += Vector3.down * dt * 0.6f;
            }
            if (WorldSpawn)
            {
                // Balance B1: Welt-Spawns kehren nach der Respawn-Zeit der Gefahrenstufe zurück (nicht vor den Augen des Spielers)
                if (_deathTimer > 4f && !_hidden) { Hide(true); _respawnAt = Now + Mathf.Max(5f, Danger.Respawn - 4f); }
                if (_hidden && _respawnAt > 0f && Now >= _respawnAt)
                {
                    var pl = GameManager.Instance != null ? GameManager.Instance.Player : null;
                    if (pl == null || FlatDistance(pl.transform.position) > 25f || Vector3.Distance(pl.transform.position, HomePosition) > 25f) { Revive(); ApplyArchetype(); }
                    else _respawnAt = Now + 5f;
                }
                return;
            }
            if (DestroyOnDeath && _deathTimer > 4f) Destroy(gameObject);
        }

        float FlatDistance(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; return d.magnitude; }

        void Hide(bool on)
        {
            _hidden = on;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !on;
            if (_collider != null) _collider.enabled = !on;
            Self.Targetable = !on;
        }

        /// <summary>Bewegt sich in Richtung Ziel; true wenn angekommen.</summary>
        bool MoveTowards(Vector3 target, float speed, float dt, float stopDistance)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist <= stopDistance + 0.25f) { AnimateVisual(dt, false); return true; }
            Vector3 steer = SteerPoint(target, dt) - transform.position; steer.y = 0f;
            Vector3 dir = steer.sqrMagnitude > 0.01f ? steer.normalized : d / dist;
            // Separation von anderen NPCs
            Vector3 sep = Vector3.zero;
            // Balance B1: Separation nur unter Gegnern desselben Teams – schiebt nie in den Spieler
            for (int i = 0; i < Combatant.All.Count; i++)
            {
                var o = Combatant.All[i];
                if (o == null || o == Self || o.IsDead) continue;
                if (dist > 2f) sep += Separation.Push(transform.position, o.transform.position, o.Team == Self.Team, 1.4f * Archetype.Scale);
            }
            dir = (dir + sep * 0.6f).normalized;
            float step = Mathf.Min(dist - stopDistance, speed * Self.MoveSpeedMultiplier * dt);
            Vector3 next = transform.position + dir * step;
            if (Self.Team == Team.Enemy && GameManager.Instance != null && GameManager.Instance.Player != null && !BalanceWorld.LegacyPursuit)
                next = Separation.ClampToGap(transform.position, next, GameManager.Instance.Player.transform.position);
            next = ZoneAnchors.GroundAt(next + Vector3.up * 2f);
            transform.position = next;
            Face(transform.position + dir, dt);
            _bob += dt * speed * 2f;
            AnimateVisual(dt, false, true);
            return false;
        }

        /// <summary>Nächster Wegpunkt auf dem NavMesh (falls vorhanden), sonst das Ziel selbst.</summary>
        Vector3 SteerPoint(Vector3 target, float dt)
        {
            _repathTimer -= dt;
            if (_repathTimer <= 0f || (target - _pathTarget).sqrMagnitude > 2.25f)
            {
                _repathTimer = 0.5f;
                _pathTarget = target;
                _cornerCount = 0;
                if (_path == null) _path = new NavMeshPath();
                if (NavMesh.SamplePosition(transform.position, out var a, 3f, NavMesh.AllAreas) &&
                    NavMesh.SamplePosition(target, out var b, 4f, NavMesh.AllAreas) &&
                    NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _path) &&
                    _path.status != NavMeshPathStatus.PathInvalid)
                {
                    _cornerCount = _path.GetCornersNonAlloc(_corners);
                    _cornerIdx = 1;
                }
            }
            while (_cornerIdx < _cornerCount - 1 && FlatDistance(_corners[_cornerIdx]) < 0.8f) _cornerIdx++;
            if (_cornerCount >= 2 && _cornerIdx < _cornerCount) return _corners[_cornerIdx];
            return target;
        }

        /// <summary>Lauf 4: Bewegung für Verhaltens-Komponenten (Fluchtabstand, Sturmangriff).</summary>
        public bool MoveTo(Vector3 target, float speed, float stop) => MoveTowards(target, speed, Time.deltaTime, stop);

        void Face(Vector3 p, float dt)
        {
            Vector3 d = p - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 540f * dt);
        }

        /// <summary>Lauf 5: Skelett-Animation (AnimDriver) statt Kipp-Wackeln.</summary>
        public bool AnimDriven { get; set; }
        /// <summary>Lauf 5 / M21: Telegraphierte Angriffe holen mindestens 0,6 s aus (lesbar, ausweichbar).</summary>
        public const float MinWindup = 0.6f;
        public static float EffectiveWindup(EnemyArchetype a) => a == null ? MinWindup : Mathf.Max(MinWindup, a.AttackWindup);
        /// <summary>Balance B1: Aufladung mit Fairness-Minimum der Gefahrenstufe (0–1: 1,2 s · 2–3: 1,0 s · 4–5: 0,75 s).</summary>
        public float Windup => Mathf.Max(EffectiveWindup(Archetype), Self != null && Self.Team == Team.Enemy && !BalanceWorld.LegacyPursuit ? Danger.MinTelegraph : 0f);
        /// <summary>Lauf 5 / M22: Gegner-Aktivierungsbalken im Zielfenster.</summary>
        public bool IsWindingUp => _windingUp && !Self.IsDead;
        public float WindupProgress => 1f - Mathf.Clamp01(_windup / Windup);

        void AnimateVisual(float dt, bool windup, bool moving = false)
        {
            if (Visual == null || AnimDriven) return;
            Quaternion rot = windup ? Quaternion.Euler(-18f, 0f, 0f) : moving ? Quaternion.Euler(8f, 0f, Mathf.Sin(_bob) * 4f) : Quaternion.identity;
            Vector3 pos = moving ? Vector3.up * Mathf.Abs(Mathf.Sin(_bob)) * 0.08f : Vector3.zero;
            Visual.localRotation = Quaternion.Slerp(Visual.localRotation, rot, 1f - Mathf.Exp(-12f * dt));
            Visual.localPosition = Vector3.Lerp(Visual.localPosition, pos, 1f - Mathf.Exp(-12f * dt));
        }

        /// <summary>Erzeugt einen NPC aus einem Archetyp.</summary>
        public static NpcBrain Spawn(EnemyArchetype a, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (a == null || a.Prefab == null) return null;
            var go = Instantiate(a.Prefab, ZoneAnchors.GroundAt(position + Vector3.up * 2f), rotation, parent);
            var b = go.GetComponent<NpcBrain>();
            b.Archetype = a;
            b.Init();
            b.ApplyArchetype();
            b.HomePosition = go.transform.position;
            if (a.Defiance > 0f) { var d = go.GetComponent<DefianceBar>(); if (d == null) d = go.AddComponent<DefianceBar>(); d.SetMax(a.Defiance); }
            return b;
        }
    }
}
