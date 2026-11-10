using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1 (Zielbild B/B4): Rollen der Spätspiel-Gegner – Jäger (Schattenschritt zum Spieler ab 900 Einheiten, wirft Reittiere ab),
    /// Rufer (alarmiert mit 1,5-s-Ruf das ganze Lager, unterbrechbar) und Lauerer (eingegraben bis der Spieler die Lagermitte erreicht).
    /// </summary>
    [RequireComponent(typeof(NpcBrain))]
    public class RoleBehaviour : MonoBehaviour
    {
        public const float ShadowstepRange = 900f, ShadowstepCooldown = 15f, CallTime = 1.5f, AmbushRadius = 6f;
        NpcBrain _b; Combatant _self;
        float _stepCd, _callT = -1f, _callHp; bool _called, _buried;
        public Vector3 CampCenter;

        public EnemyRole Role => _b != null ? _b.EffectiveRole : EnemyRole.Standard;

        void Awake() { _b = GetComponent<NpcBrain>(); _self = GetComponent<Combatant>(); }

        void Start()
        {
            if (CampCenter == Vector3.zero) CampCenter = transform.position;
            if (Role == EnemyRole.Lurker && _b.Danger.Ambushes) Bury(true);
            _b.StateChanged += OnState;
        }

        void OnDestroy() { if (_b != null) _b.StateChanged -= OnState; }

        void OnState(NpcState s)
        {
            if (s == NpcState.Idle || s == NpcState.Reset) { _called = false; _callT = -1f; if (Role == EnemyRole.Lurker && _b.Danger.Ambushes && s == NpcState.Idle) Bury(true); }
            if (s == NpcState.Aggro && Role == EnemyRole.Caller && _b.Danger.Callers && !_called && _callT < 0f) BeginCall();
        }

        void Bury(bool on)
        {
            _buried = on;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !on;
            _self.Targetable = !on;
            _b.Suspended = on;
        }

        void BeginCall()
        {
            _callT = CallTime; _callHp = _self.Health;
            _b.Suspended = true;
            SkillExecutor.VfxPulse(transform.position, 4f, new Color(1f, 0.85f, 0.3f));
            GameEventBus.Notify(_self.DisplayName + " ruft das Lager zu Hilfe – unterbrechen!", new Color(1f, 0.75f, 0.3f));
        }

        void Update()
        {
            if (_self == null || _self.IsDead) return;
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            float dt = Time.deltaTime;
            // Lauerer: erscheint, wenn der Spieler die Lagermitte erreicht (oder bei Schaden)
            if (_buried)
            {
                if (p != null && !p.IsDead && (Vector3.Distance(p.transform.position, CampCenter) < AmbushRadius || _self.HealthFraction < 0.999f))
                {
                    Bury(false);
                    _b.ForceAggro(p);
                    if (_b.ControlAllowed(EffectType.Immobilize)) p.Effects.Apply(new EffectApplication(EffectType.Immobilize, 1, 1.5f), _self);
                    SkillExecutor.VfxPulse(transform.position, 2.5f, new Color(0.5f, 0.4f, 0.3f));
                    GameEventBus.Notify("Hinterhalt! " + _self.DisplayName + " bricht hervor!", new Color(1f, 0.6f, 0.4f));
                }
                return;
            }
            // Rufer: Ruf mit Telegraph, Unterbrechung durch Kontrolle oder ≥ 10 % Schaden
            if (_callT >= 0f)
            {
                _callT -= dt;
                bool interrupted = _self.Effects.Has(EffectType.Stun) || _self.Health < _callHp - _self.MaxHealth * 0.1f;
                if (interrupted) { _callT = -1f; _called = true; _b.Suspended = false; GameEventBus.Notify("Ruf unterbrochen!", new Color(0.6f, 1f, 0.6f)); return; }
                if (_callT <= 0f)
                {
                    _callT = -1f; _called = true; _b.Suspended = false;
                    int n = 0;
                    foreach (var c in Combatant.All)
                    {
                        if (c == null || c.IsDead || c.Team != _self.Team) continue;
                        var ob = c.GetComponent<NpcBrain>();
                        if (ob == null || ob == _b || ob.State != NpcState.Idle || Vector3.Distance(c.transform.position, transform.position) > 40f) continue;
                        if (_b.Target != null) { ob.ForceAggro(_b.Target, false); n++; }
                    }
                    GameEventBus.Notify("Das Lager ist alarmiert (" + n + ")!", new Color(1f, 0.5f, 0.35f));
                }
                return;
            }
            // Jäger: Schattenschritt hinter den Spieler, Reittier-Abwurf
            if (Role == EnemyRole.Hunter && _b.Target != null && _b.Target == p && _b.State == NpcState.Aggro && _b.Pursuit != null && _b.Pursuit.IsChasing)
            {
                _stepCd -= dt;
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (_stepCd <= 0f && d > Units.M(ShadowstepRange) * 0.5f && _b.DangerLevel >= 4)
                {
                    _stepCd = ShadowstepCooldown;
                    var behind = p.transform.position - p.transform.forward * 3f;
                    SkillExecutor.VfxPulse(transform.position, 1.5f, new Color(0.45f, 0.2f, 0.6f));
                    transform.position = ZoneAnchors.GroundAt(behind + Vector3.up * 2f);
                    SkillExecutor.VfxPulse(transform.position, 1.5f, new Color(0.45f, 0.2f, 0.6f));
                    GameEventBus.Notify(_self.DisplayName + " springt durch die Schatten!", new Color(0.8f, 0.6f, 1f));
                }
                var mc = MountController.Local;
                if (mc != null && mc.Flight != null && mc.Flight.Airborne)   // Lauf 11: Stufe 6 – Zug aus der Luft
                {
                    var flat = p.transform.position - transform.position; flat.y = 0f;
                    if (flat.magnitude < 25f && _b.ControlAllowed(ControlKinds.AirPull)) { mc.PullFromSky(3f); GameEventBus.Notify(_self.DisplayName + " holt dich vom Himmel!", new Color(1f, 0.5f, 0.4f)); }
                }
                else if (mc != null && mc.Motor.IsMounted && d < 4f && _b.ControlAllowed(ControlKinds.Dismount))
                {
                    mc.ApplyDismount();
                    p.Effects.Apply(new EffectApplication(EffectType.Cripple, 1, 2f), _self);
                    GameEventBus.Notify(_self.DisplayName + " reißt dich vom Reittier!", new Color(1f, 0.5f, 0.4f));
                }
            }
        }

        public bool Buried => _buried;
    }
}
