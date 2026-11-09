using System;
using UnityEngine;
using UnityEngine.AI;

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
            if (info.Source != null && !info.Source.IsDead && Self.IsHostileTo(info.Source) && (Target == null || State == NpcState.Idle))
            {
                Target = info.Source;
                SetState(NpcState.Aggro);
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
            Self.Revive(1f);
            Self.ManualInvulnerable = false;
            Target = null;
            Objective = null;
            transform.position = HomePosition;
            if (_collider != null) _collider.enabled = true;
            if (Visual != null) { Visual.localRotation = Quaternion.identity; Visual.localPosition = Vector3.zero; }
            SetState(NpcState.Idle);
        }

        public void ForceAggro(Combatant c)
        {
            if (c == null || Self.IsDead) return;
            Target = c;
            SetState(NpcState.Aggro);
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

        Combatant Acquire()
        {
            if (Passive) return null;
            if (ValidTarget(PreferredTarget) && Vector3.Distance(PreferredTarget.transform.position, transform.position) < Archetype.AggroRange * 1.5f)
            {
                // Spieler in unmittelbarer Nähe hat Vorrang vor Kisten
                var near = Combatant.FindNearestHostile(Self, transform.position, Archetype.AttackRange * 2.5f, c => c.Team == Team.Player);
                return near != null ? near : PreferredTarget;
            }
            return Combatant.FindNearestHostile(Self, transform.position, Archetype.AggroRange);
        }

        void TickIdle(float dt)
        {
            var t = Acquire();
            if (t != null) { Target = t; SetState(NpcState.Aggro); return; }
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
            if (UseLeash && !Objective.HasValue && Vector3.Distance(transform.position, HomePosition) > Archetype.LeashRange) { BeginReset(); return; }
            float dist = FlatDistance(Target.transform.position);
            if (dist <= Archetype.AttackRange) { SetState(NpcState.Attack); return; }
            MoveTowards(Target.transform.position, Archetype.MoveSpeed, dt, Archetype.AttackRange * 0.8f);
        }

        void TickAttack(float dt)
        {
            if (!ValidTarget(Target)) { InterruptAttack(); SetState(NpcState.Aggro); Target = null; return; }
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
                    _activeTelegraph = GroundTelegraph.Spawn(Self, Target.transform.position, Archetype.TelegraphRadius, Archetype.TelegraphDelay, 0f, Archetype.TelegraphCoefficient);
                    GameEventBus.Notify(Self.DisplayName + " holt zum Erdstoß aus – ausweichen!", new Color(1f, 0.45f, 0.3f));
                    return;
                }
            }

            if (dist > Archetype.AttackRange * 1.3f && !_windingUp) { SetState(NpcState.Aggro); return; }

            _attackTimer -= dt;
            if (_windingUp)
            {
                _windup -= dt;
                if (_windup <= 0f)
                {
                    _windingUp = false;
                    if (FlatDistance(Target.transform.position) <= Archetype.AttackRange * 1.4f)
                    {
                        float dealt = DamageCalculator.DealDirect(Self, Target, Archetype.AttackCoefficient);
                        if (dealt > 0f && Archetype.OnHitDebuff.Stacks > 0) Target.Effects.Apply(Archetype.OnHitDebuff, Self);
                        if (dealt > 0f) AttackLanded?.Invoke(Target);
                    }
                }
            }
            else if (_attackTimer <= 0f)
            {
                _attackTimer = Archetype.AttackInterval;
                _windingUp = true;
                _windup = EffectiveWindup(Archetype);
                AttackStarted?.Invoke(Target);
            }
            AnimateVisual(dt, _windingUp);
        }

        void BeginReset()
        {
            InterruptAttack();
            Target = null;
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
            if (DestroyOnDeath && _deathTimer > 4f) Destroy(gameObject);
        }

        float FlatDistance(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; return d.magnitude; }

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
            for (int i = 0; i < Combatant.All.Count; i++)
            {
                var o = Combatant.All[i];
                if (o == null || o == Self || o.IsDead) continue;
                Vector3 od = transform.position - o.transform.position; od.y = 0f;
                float m = od.magnitude;
                if (m > 0.01f && m < 1.4f * Archetype.Scale && dist > 2f) sep += od / (m * m);
            }
            dir = (dir + sep * 0.6f).normalized;
            float step = Mathf.Min(dist - stopDistance, speed * Self.MoveSpeedMultiplier * dt);
            Vector3 next = transform.position + dir * step;
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
        /// <summary>Lauf 5 / M22: Gegner-Aktivierungsbalken im Zielfenster.</summary>
        public bool IsWindingUp => _windingUp && !Self.IsDead;
        public float WindupProgress => 1f - Mathf.Clamp01(_windup / EffectiveWindup(Archetype));

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
