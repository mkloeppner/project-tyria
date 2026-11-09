using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Sonderverhalten der Lauf-4-Gegner (liest <see cref="EnemyArchetype.Behavior"/>):
    /// Hinterhalt (verborgen im Moor, taucht mit Festhalten auf), Fernkampf (Abstand halten, Geschoss), Heranziehen + Stampfer,
    /// Rudelheulen (Macht/Eile für Verbündete), Frontschild (80 % weniger Schaden von vorn), Wurf (Flächen-Telegraph mit Rückstoß),
    /// Explosion beim Tod, Beschwörer (Diener), Sturmangriff (Ansturm mit Niederwerfen) + Brennaura.
    /// </summary>
    [RequireComponent(typeof(NpcBrain))]
    public class NpcAbilities : MonoBehaviour
    {
        NpcBrain _brain;
        Combatant _self;
        EnemyArchetype A => _brain != null ? _brain.Archetype : null;
        float _timer, _auraTimer;
        bool _hidden, _howled, _charging;
        Vector3 _chargeTarget;
        float _chargeTime;
        int _summoned;

        void Awake()
        {
            _brain = GetComponent<NpcBrain>();
            _self = GetComponent<Combatant>();
        }

        void OnEnable()
        {
            if (_brain == null) return;
            _brain.AttackStarted += OnAttackStarted;
            _brain.StateChanged += OnState;
            _self.Died += OnDied;
            _self.IncomingMultiplier = Shield;
        }

        void OnDisable()
        {
            if (_brain == null) return;
            _brain.AttackStarted -= OnAttackStarted;
            _brain.StateChanged -= OnState;
            _self.Died -= OnDied;
            _self.IncomingMultiplier = null;
        }

        void Start()
        {
            if (A == null) return;
            _timer = A.BehaviorInterval * 0.5f;
            if (A.Behavior == NpcBehavior.Ambush) Hide(true);
        }

        // ---------------- Hinterhalt ----------------
        void Hide(bool on)
        {
            _hidden = on;
            _brain.Suspended = on;
            _self.Targetable = !on;
            if (_brain.Visual != null) _brain.Visual.gameObject.SetActive(!on);
        }

        void OnState(NpcState s)
        {
            if (A == null) return;
            if (s == NpcState.Aggro && A.Behavior == NpcBehavior.PackHowl && !_howled) Howl();
            if (s == NpcState.Idle) { _howled = false; if (A.Behavior == NpcBehavior.Ambush && !_self.IsDead) Hide(true); }
        }

        float Shield(DamageInfo info)
        {
            if (A == null || A.Behavior != NpcBehavior.FrontShield || info.Source == null || _self.Effects.Has(EffectType.Stun)) return 1f;
            Vector3 to = info.Source.transform.position - transform.position; to.y = 0f;
            return Vector3.Angle(transform.forward, to) < 70f ? 0.2f : 1f;
        }

        void Update()
        {
            if (A == null || _self.IsDead) return;
            float dt = Time.deltaTime;
            var player = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            switch (A.Behavior)
            {
                case NpcBehavior.Ambush:
                    if (_hidden && player != null && !player.IsDead && (Flat(player.transform.position) < A.BehaviorRadius || _self.HealthFraction < 0.999f)) Emerge(player);
                    break;
                case NpcBehavior.Ranged:
                    // Abstand halten: zu nah → zurückweichen
                    if (_brain.State == NpcState.Attack && _brain.Target != null && Flat(_brain.Target.transform.position) < A.AttackRange * 0.45f)
                    {
                        var away = transform.position - _brain.Target.transform.position; away.y = 0f;
                        _brain.MoveTo(transform.position + away.normalized * 4f, A.MoveSpeed * 0.8f, 0.5f);
                    }
                    break;
                case NpcBehavior.Charger:
                    TickCharge(dt);
                    _auraTimer -= dt;
                    if (_auraTimer <= 0f && _brain.State != NpcState.Idle)
                    {
                        _auraTimer = 2f;
                        foreach (var c in Combatant.All)
                            if (c != null && !c.IsDead && _self.IsHostileTo(c) && Flat(c.transform.position) < 4f) c.Effects.Apply(new EffectApplication(EffectType.Burning, 1, 3f), _self);
                    }
                    break;
            }
            if (_brain.State != NpcState.Attack || _brain.Target == null) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = A.BehaviorInterval;
            var t = _brain.Target;
            switch (A.Behavior)
            {
                case NpcBehavior.Pull: Pull(t); break;
                case NpcBehavior.Lob: Lob(t); break;
                case NpcBehavior.Summoner: Summon(); break;
                case NpcBehavior.Charger: BeginCharge(t); break;
                case NpcBehavior.PackHowl: _howled = false; Howl(); break;
            }
        }

        float Flat(Vector3 p) { var d = p - transform.position; d.y = 0f; return d.magnitude; }

        void Emerge(Combatant player)
        {
            Hide(false);
            _brain.ForceAggro(player);
            player.Effects.Apply(new EffectApplication(EffectType.Immobilize, 1, 1.5f), _self);
            SkillExecutor.VfxPulse(transform.position, 2.5f, new Color(0.35f, 0.5f, 0.25f));
            GameEventBus.Notify(_self.DisplayName + " bricht aus dem Moor hervor!", new Color(0.7f, 0.9f, 0.5f));
        }

        void OnAttackStarted(Combatant target)
        {
            if (A == null || target == null) return;
            if (A.Behavior == NpcBehavior.Ranged) Bolt.Fire(_self.AimPoint, target, NpcBrain.EffectiveWindup(A), A.ProjectileColor);
        }

        void Pull(Combatant t)
        {
            var pc = t.GetComponent<PlayerController>();
            Vector3 to = transform.position - t.transform.position; to.y = 0f;
            if (to.magnitude < 3f) { Slam(); return; }
            if (pc != null) pc.AddImpulse(to.normalized * Mathf.Min(22f, to.magnitude * 3.2f), 4f);
            Bolt.Fire(_self.AimPoint, t, 0.25f, A.ProjectileColor);
            GameEventBus.Notify(_self.DisplayName + " zieht dich heran!", new Color(1f, 0.6f, 0.4f));
            Invoke(nameof(Slam), 0.9f);
        }

        void Slam()
        {
            if (_self.IsDead) return;
            var tg = GroundTelegraph.Spawn(_self, transform.position, A.BehaviorRadius, 1.2f, 0f, A.BehaviorCoefficient);
            tg.PulseColor = A.ProjectileColor;
        }

        void Lob(Combatant t)
        {
            var tg = GroundTelegraph.Spawn(_self, t.transform.position, A.BehaviorRadius, 1.4f, 0f, A.BehaviorCoefficient);
            tg.PulseColor = A.ProjectileColor;
            var origin = transform.position;
            var eff = A.BehaviorEffect;
            tg.OnHit = v =>
            {
                if (eff.Stacks > 0) v.Effects.Apply(eff, _self);
                var pc = v.GetComponent<PlayerController>();
                if (pc != null) { var d = v.transform.position - origin; d.y = 0f; pc.AddImpulse(d.normalized * 14f, 5f); }
            };
            Bolt.Fire(_self.AimPoint, null, 1.3f, A.ProjectileColor, t.transform.position);
        }

        void Howl()
        {
            _howled = true;
            int n = 0;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != _self.Team || Flat(c.transform.position) > A.BehaviorRadius * 2f) continue;
                c.Effects.Apply(new EffectApplication(EffectType.Might, 3, 10f), _self);
                c.Effects.Apply(new EffectApplication(EffectType.Swiftness, 1, 5f), _self);
                var b = c.GetComponent<NpcBrain>();
                if (b != null && b != _brain && b.State == NpcState.Idle && _brain.Target != null) b.ForceAggro(_brain.Target);
                n++;
            }
            SkillExecutor.VfxPulse(transform.position, A.BehaviorRadius * 2f, new Color(0.7f, 0.85f, 1f));
            if (n > 1) GameEventBus.Notify(_self.DisplayName + " heult – das Rudel antwortet!", new Color(0.75f, 0.9f, 1f));
        }

        void Summon()
        {
            if (A.SummonArchetype == null || _summoned >= 4) return;
            for (int i = 0; i < 2; i++)
            {
                var off = Quaternion.Euler(0, i * 180f + Random.Range(-30f, 30f), 0) * Vector3.forward * 3f;
                var b = NpcBrain.Spawn(A.SummonArchetype, transform.position + off, transform.rotation, transform.parent);
                if (b == null) continue;
                b.GetComponent<Combatant>().DisplayName = "Beschworener " + A.SummonArchetype.DisplayName;
                if (_brain.Target != null) b.ForceAggro(_brain.Target);
                _summoned++;
            }
            SkillExecutor.VfxPulse(transform.position, 3.5f, A.ProjectileColor);
        }

        void BeginCharge(Combatant t)
        {
            if (Flat(t.transform.position) < 5f) { Slam(); return; }
            _charging = true; _chargeTime = 1.6f;
            _chargeTarget = t.transform.position;
            _brain.Suspended = true;
            GameEventBus.Notify(_self.DisplayName + " stürmt heran!", new Color(1f, 0.5f, 0.3f));
        }

        void TickCharge(float dt)
        {
            if (!_charging) return;
            _chargeTime -= dt;
            bool arrived = _brain.MoveTo(_chargeTarget, A.MoveSpeed * 3f, 1.5f);
            var t = _brain.Target;
            if (t != null && Flat(t.transform.position) < 2.2f)
            {
                DamageCalculator.DealDirect(_self, t, A.BehaviorCoefficient);
                t.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), _self);
                var pc = t.GetComponent<PlayerController>();
                if (pc != null) pc.AddImpulse(transform.forward * 12f, 4f);
                arrived = true;
            }
            if (arrived || _chargeTime <= 0f) { _charging = false; _brain.Suspended = false; }
        }

        void OnDied(Combatant killer)
        {
            if (A == null) return;
            if (A.Behavior == NpcBehavior.Exploder)
            {
                var tg = GroundTelegraph.Spawn(_self, transform.position, A.BehaviorRadius, 1.1f, 0f, 0f);
                tg.PulseColor = A.ProjectileColor;
                float dmg = EnemyArchetype.NpcBaseHealth(_self.Level) * 0.22f;
                var eff = A.BehaviorEffect;
                tg.Owner = null;   // Besitzer tot → Schaden selbst anwenden
                tg.OnHit = null;
                ExplodeLater(transform.position, dmg, eff);
            }
            if (_hidden) Hide(false);
        }

        void ExplodeLater(Vector3 pos, float dmg, EffectApplication eff) => Exploder.Schedule(pos, A.BehaviorRadius, 1.1f, dmg, eff, _self.Team, A.ProjectileColor);
    }

    /// <summary>Verzögerte Todes-Explosion (lebt unabhängig vom toten Gegner).</summary>
    public class Exploder : MonoBehaviour
    {
        float _t, _r, _dmg; EffectApplication _eff; Team _team; Color _col;
        public static void Schedule(Vector3 pos, float radius, float delay, float damage, EffectApplication eff, Team team, Color col)
        {
            var go = new GameObject("DeathBlast"); go.transform.position = pos;
            var e = go.AddComponent<Exploder>(); e._t = delay; e._r = radius; e._dmg = damage; e._eff = eff; e._team = team; e._col = col;
        }
        void Update()
        {
            _t -= Time.deltaTime;
            if (_t > 0f) return;
            foreach (var c in Combatant.All.ToArray())
            {
                if (c == null || c.IsDead || c.Team == _team || c.Team == Team.Neutral) continue;
                var d = c.transform.position - transform.position; d.y = 0f;
                if (d.magnitude > _r) continue;
                c.TakeDamage(new DamageInfo { Source = null, Amount = _dmg, Point = c.AimPoint });
                if (_eff.Stacks > 0) c.Effects.Apply(_eff, null);
            }
            SkillExecutor.VfxPulse(transform.position, _r * 1.1f, _col);
            Destroy(gameObject);
        }
    }

    /// <summary>Kosmetisches Geschoss (Schaden macht der Angriff selbst).</summary>
    public class Bolt : MonoBehaviour
    {
        Vector3 _from, _to; Combatant _target; float _t, _dur;
        public static void Fire(Vector3 from, Combatant target, float duration, Color col, Vector3? point = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Bolt"; go.transform.localScale = Vector3.one * 0.4f; go.transform.position = from;
            var lib = VfxLibrary.Current;
            var r = go.GetComponent<Renderer>();
            if (lib != null && lib.ProjectileGlow != null)
            {
                r.sharedMaterial = lib.ProjectileGlow;
                var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", col); mpb.SetColor("_EmissionColor", col * 2.5f); r.SetPropertyBlock(mpb);
            }
            var b = go.AddComponent<Bolt>();
            b._from = from; b._target = target; b._to = point ?? (target != null ? target.AimPoint : from); b._dur = Mathf.Max(0.1f, duration);
        }
        void Update()
        {
            _t += Time.deltaTime;
            if (_target != null) _to = _target.AimPoint;
            float k = Mathf.Clamp01(_t / _dur);
            transform.position = Vector3.Lerp(_from, _to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * Vector3.Distance(_from, _to) * 0.15f;
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
