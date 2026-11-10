using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: NPC-Wiederbelebung (rein): Stufe 0–1, kein Gegner in 15 m, Helfer vorhanden → innerhalb von 20 s wieder auf den Beinen.</summary>
    public class NpcReviveModel
    {
        public const float MaxWait = 20f, HelpDelay = 8f, HostileRadius = 15f;
        public enum Phase { None, Waiting, Done }
        public Phase State { get; private set; }
        float _t, _clear;

        public bool Begin(DangerSettings s, bool helperNear)
        {
            if (s == null || !s.NpcRevive || !helperNear) { State = Phase.None; return false; }
            State = Phase.Waiting; _t = 0f; _clear = 0f;
            return true;
        }

        /// <summary>true = jetzt wiederbeleben. Uhr läuft nur, solange kein Gegner in der Nähe ist.</summary>
        public bool Tick(float dt, bool hostileNear)
        {
            if (State != Phase.Waiting) return false;
            _t += dt;
            if (hostileNear) { _clear = 0f; return false; }
            _clear += dt;
            if (_clear >= HelpDelay || (_t >= MaxWait && _clear >= 2f)) { State = Phase.Done; return true; }
            return false;
        }

        public void Cancel() => State = Phase.None;
    }

    /// <summary>Balance B1: Aggro-Übernahme durch Wachen (Stufe 0–2): Gegner, die einen Spieler verfolgen, wechseln auf angreifende Wachen.</summary>
    public static class ThreatModel
    {
        public static bool SwitchToAttacker(int danger, bool currentIsPlayer, bool attackerIsGuard, bool guardsHelp) => guardsHelp && danger <= 2 && currentIsPlayer && attackerIsGuard;
        /// <summary>Wachen bevorzugen Gegner, die gerade einen Spieler verfolgen.</summary>
        public static float GuardPriority(bool chasingPlayer, float distance) => (chasingPlayer ? 0f : 100f) + distance;
    }

    /// <summary>
    /// Balance B1: hängt am Spieler. Nach einer Niederlage in Stufe 0–1 kommt ein Dorfbewohner oder eine Wache zu Hilfe
    /// („Hilfe ist unterwegs“) und belebt innerhalb von 20 s wieder, sobald kein Gegner in der Nähe ist.
    /// </summary>
    public class NpcRescue : MonoBehaviour
    {
        public static NpcRescue Local { get; private set; }
        readonly NpcReviveModel _m = new NpcReviveModel();
        Combatant _p; string _helper;
        public bool Pending => _m.State == NpcReviveModel.Phase.Waiting;

        void Awake() { _p = GetComponent<Combatant>(); Local = this; }
        void OnEnable() { GameEventBus.PlayerDefeated += OnDefeated; GameEventBus.PlayerRespawned += OnRespawned; }
        void OnDisable() { GameEventBus.PlayerDefeated -= OnDefeated; GameEventBus.PlayerRespawned -= OnRespawned; }

        void OnRespawned(Combatant c) { if (c == _p) _m.Cancel(); }

        void OnDefeated(Combatant c)
        {
            if (c != _p || BalanceWorld.Legacy) return;
            var s = BalanceWorld.Profile(BalanceWorld.DangerAt(_p.transform.position));
            var helper = FindHelper(out _helper);
            if (_m.Begin(s, helper)) GameEventBus.Notify("Hilfe ist unterwegs – " + _helper + " eilt herbei …", new Color(0.6f, 1f, 0.7f));
        }

        bool FindHelper(out string name)
        {
            name = null; float best = 45f;
            foreach (var c in Combatant.All)
            {
                if (c == null || c == _p || c.IsDead || c.Team == Team.Enemy) continue;
                float d = Vector3.Distance(c.transform.position, _p.transform.position);
                if (d < best) { best = d; name = c.DisplayName; }
            }
            if (name == null && BalanceWorld.Map != null)
            {
                foreach (var w in BalanceWorld.Map.Waypoints) if (Vector2.Distance(w, new Vector2(_p.transform.position.x, _p.transform.position.z)) < 60f) { name = "Eine Wache"; break; }
                if (name == null && BalanceWorld.Map.IsStartZone && BalanceWorld.Map.Anchors.Count > 0 && Vector2.Distance(BalanceWorld.Map.Anchors[0], new Vector2(_p.transform.position.x, _p.transform.position.z)) < 90f) name = "Ein Dorfbewohner";
            }
            return name != null;
        }

        void Update()
        {
            if (!Pending || _p == null) return;
            if (!_p.IsDead) { _m.Cancel(); return; }
            bool hostile = false;
            foreach (var c in Combatant.All)
                if (c != null && !c.IsDead && c.Team == Team.Enemy && Vector3.Distance(c.transform.position, _p.transform.position) < NpcReviveModel.HostileRadius) { var b = c.GetComponent<NpcBrain>(); if (b == null || !b.IsYellow) { hostile = true; break; } }
            if (!_m.Tick(Time.deltaTime, hostile)) return;
            var dc = _p.GetComponent<DownedController>(); if (dc != null) dc.ResetState();
            _p.Revive(0.5f);
            var ui = UIController.Instance; if (ui != null && ui.WorldMap != null) ui.WorldMap.Window.Close();
            GameEventBus.RaisePlayerRespawned(_p);
            GameEventBus.Notify(_helper + " hat dich wiederbelebt.", new Color(0.6f, 1f, 0.7f));
            BalanceTelemetry.Record(TelemetryKind.NpcRevive, _p.transform.position, _helper);
        }
    }

    /// <summary>Balance B1: Wachen-Patrouille auf den Wegen (Stufe 0–2): läuft die Wegpunkte ab, solange kein Kampf läuft.</summary>
    public class GuardPatrol : MonoBehaviour
    {
        public Vector3[] Points = new Vector3[0];
        int _i, _dir = 1; NpcBrain _b;
        void Awake() { _b = GetComponent<NpcBrain>(); }
        void Update()
        {
            if (_b == null || Points == null || Points.Length < 2 || _b.Self == null || _b.Self.IsDead) return;
            if (_b.State != NpcState.Idle) { _b.Objective = null; return; }
            var p = Points[_i];
            if (new Vector2(p.x - transform.position.x, p.z - transform.position.z).magnitude < 2f)
            {
                if (_i + _dir < 0 || _i + _dir >= Points.Length) _dir = -_dir;
                _i += _dir;
                _b.HomePosition = transform.position;   // Rückkehr nach Kämpfen zum letzten Wegpunkt
            }
            _b.Objective = Points[_i];
        }
    }
}
