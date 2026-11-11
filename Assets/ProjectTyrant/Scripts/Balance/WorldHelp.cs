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

}
