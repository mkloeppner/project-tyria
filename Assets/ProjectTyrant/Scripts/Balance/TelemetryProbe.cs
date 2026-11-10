using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: hängt während einer Telemetrie-Session am Spieler und protokolliert Kampf, Tode (Ursache = letzter Schadensverursacher),
    /// Downed/Rally, Kills mit Zeit-bis-Kill, Fluchtversuche und ihr Ergebnis sowie Event-Ergebnisse mit Stufe.
    /// </summary>
    public class TelemetryProbe : MonoBehaviour
    {
        public static TelemetryProbe Current { get; private set; }
        /// <summary>Bot meldet Fluchtabsicht (Anfänger: unter 25 % HP).</summary>
        public static bool FleeIntent;
        Combatant _p; OutOfCombatRegen _regen;
        bool _inCombat; float _combatStart, _lastAttack = -99f, _escapeStart;
        DamageInfo _lastHit; bool _hasHit;
        readonly EscapeTracker _escape = new EscapeTracker();
        readonly Dictionary<Combatant, float> _firstHit = new Dictionary<Combatant, float>();
        readonly HashSet<NpcBrain> _escapePursuers = new HashSet<NpcBrain>();
        Vector3 _prevPos; float _prevDist;

        public static void Attach(Combatant player)
        {
            if (player == null) return;
            var p = player.GetComponent<TelemetryProbe>();
            if (p == null) p = player.gameObject.AddComponent<TelemetryProbe>();
            Current = p;
        }

        void OnEnable()
        {
            _p = GetComponent<Combatant>(); _regen = GetComponent<OutOfCombatRegen>();
            _p.Damaged += OnDamaged;
            GameEventBus.DamageDealt += OnDamageDealt;
            GameEventBus.EntityKilled += OnKilled;
            GameEventBus.PlayerDowned += OnDowned;
            GameEventBus.PlayerRallied += OnRallied;
            GameEventBus.PlayerDefeated += OnDefeated;
            var dir = EventDirector.Instance;
            if (dir != null && dir.Scheduler != null) { dir.Scheduler.Started += OnEventStarted; dir.Scheduler.Ended += OnEventEnded; }
        }

        void OnDisable()
        {
            if (_p != null) _p.Damaged -= OnDamaged;
            GameEventBus.DamageDealt -= OnDamageDealt;
            GameEventBus.EntityKilled -= OnKilled;
            GameEventBus.PlayerDowned -= OnDowned;
            GameEventBus.PlayerRallied -= OnRallied;
            GameEventBus.PlayerDefeated -= OnDefeated;
            var dir = EventDirector.Instance;
            if (dir != null && dir.Scheduler != null) { dir.Scheduler.Started -= OnEventStarted; dir.Scheduler.Ended -= OnEventEnded; }
            if (Current == this) Current = null;
        }

        float Now => BalanceTelemetry.Clock();

        void OnDamaged(DamageInfo d) { if (d.Amount > 0f && d.Source != null) { _lastHit = d; _hasHit = true; } }

        void OnDamageDealt(DamageInfo d)
        {
            if (d.Source != _p || d.Target == null) return;
            _lastAttack = Now;
            if (!_firstHit.ContainsKey(d.Target)) _firstHit[d.Target] = Now;
        }

        void OnKilled(Combatant victim, Combatant killer)
        {
            if (victim == null || victim == _p) return;
            if (_firstHit.TryGetValue(victim, out float t0))
            {
                _firstHit.Remove(victim);
                if (killer == _p || victim.Attackers.Contains(_p)) BalanceTelemetry.Record(TelemetryKind.Ttk, victim.transform.position, victim.DisplayName, victim.Tier, value: Now - t0);
            }
        }

        void OnDowned(Combatant c) { if (c == _p) BalanceTelemetry.Record(TelemetryKind.PlayerDowned, _p.transform.position, _hasHit && _lastHit.Source != null ? _lastHit.Source.DisplayName : null); }
        void OnRallied(Combatant c) { if (c == _p) BalanceTelemetry.Record(TelemetryKind.Rally, _p.transform.position); }

        void OnDefeated(Combatant c)
        {
            if (c != _p) return;
            var src = _hasHit ? _lastHit.Source : null;
            string cause = _hasHit ? (_lastHit.Skill != null ? _lastHit.Skill.DisplayName : "Angriff") : "unbekannt";
            string ev = null; int tier = -1;
            var dir = EventDirector.Instance;
            if (dir != null && dir.Scheduler != null)
                foreach (var r in dir.Scheduler.Runners) if (r != null && r.Active && r.Def != null && Flat(r.Def.Center - _p.transform.position) <= r.Def.Radius) { ev = r.Def.Id; tier = BalanceWorld.TierOf(r.Def); break; }
            BalanceTelemetry.Record(TelemetryKind.Death, _p.transform.position, src != null ? src.DisplayName : null, src != null ? src.Tier : EnemyTier.None, cause, _inCombat ? Now - _combatStart : 0f, eventId: ev, eventTier: tier);
            if (_escape.InAttempt) { BalanceTelemetry.Record(TelemetryKind.EscapeResult, _p.transform.position, result: "gestorben", value: Now - _escapeStart); _escape.Close(); }
            _inCombat = false;
        }

        void OnEventStarted(EventRunner r) { if (r?.Def != null) BalanceTelemetry.Record(TelemetryKind.EventStart, r.Def.Center, eventId: r.Def.Id, eventTier: BalanceWorld.TierOf(r.Def)); }

        void OnEventEnded(EventRunner r)
        {
            if (r?.Def == null) return;
            // nur Events, an denen der Spieler teilnahm (im Radius oder mit Beitrag)
            bool took = Flat(r.Def.Center - _p.transform.position) <= r.Def.Radius * 1.5f || r.Contribution > 0f;
            if (!took) return;
            BalanceTelemetry.Record(TelemetryKind.EventEnd, r.Def.Center, eventId: r.Def.Id, eventTier: BalanceWorld.TierOf(r.Def), result: r.State == RunnerState.Succeeded ? "erfolg" : "fehlschlag");
        }

        void Update()
        {
            if (BalanceTelemetry.Current == null || _p == null) return;
            bool combat = _regen != null ? _regen.InCombat : false;
            if (combat && !_inCombat) { _inCombat = true; _combatStart = Now; BalanceTelemetry.Record(TelemetryKind.CombatStart, _p.transform.position); }
            else if (!combat && _inCombat) { _inCombat = false; BalanceTelemetry.Record(TelemetryKind.CombatEnd, _p.transform.position, value: Now - _combatStart); }

            // Flucht: Abstand zu den Verfolgern wächst, kein eigener Angriff
            float dist = NearestPursuer(out int count);
            bool movingAway = count > 0 && dist > _prevDist + 0.01f && (_p.transform.position - _prevPos).sqrMagnitude > 0.0004f;
            bool attacked = Now - _lastAttack < 0.5f;
            if (_escape.Tick(Now, combat && (count > 0 || _escape.InAttempt), movingAway || FleeIntent, attacked))
            {
                _escapeStart = Now; _escapePursuers.Clear();
                foreach (var b in Pursuers()) _escapePursuers.Add(b);
                BalanceTelemetry.Record(TelemetryKind.EscapeAttempt, _p.transform.position, value: count);
            }
            if (_escape.InAttempt)
            {
                bool allGaveUp = true;
                foreach (var b in _escapePursuers) if (b != null && !b.Self.IsDead && b.Target == _p && b.State != NpcState.Reset) allGaveUp = false;
                if (allGaveUp) { BalanceTelemetry.Record(TelemetryKind.EscapeResult, _p.transform.position, result: "aufgegeben", value: Now - _escapeStart); _escape.Close(); }
                else if (attacked) { BalanceTelemetry.Record(TelemetryKind.EscapeResult, _p.transform.position, result: "kämpft", value: Now - _escapeStart); _escape.Close(); }
                else if (Now - _escapeStart > 60f) { BalanceTelemetry.Record(TelemetryKind.EscapeResult, _p.transform.position, result: "verfolgt", value: Now - _escapeStart); _escape.Close(); }
            }
            _prevDist = dist; _prevPos = _p.transform.position;
        }

        IEnumerable<NpcBrain> Pursuers()
        {
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != Team.Enemy) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b != null && b.Target == _p && b.State != NpcState.Reset) yield return b;
            }
        }

        float NearestPursuer(out int count)
        {
            count = 0; float best = 999f;
            foreach (var b in Pursuers()) { count++; best = Mathf.Min(best, Flat(b.transform.position - _p.transform.position)); }
            return best;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    }
}
