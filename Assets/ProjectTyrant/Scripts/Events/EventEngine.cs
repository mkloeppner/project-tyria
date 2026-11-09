using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum RunnerState { Active, Succeeded, Failed }

    /// <summary>Eingaben aus der Welt für einen Runner-Tick (vom EventDirector gefüllt; in Tests frei setzbar).</summary>
    public struct EventInputs
    {
        public bool PlayerInCapture;
        public int EnemiesInCapture;
    }

    /// <summary>Ein laufendes Ereignis (reines Modell): Fortschritt, Zeit, Beitrag, Ergebnis.</summary>
    public class EventRunner
    {
        public readonly EventDefinition Def;
        public RunnerState State { get; private set; } = RunnerState.Active;
        public float Time { get; private set; }
        public float Contribution;
        public int Kills, Carried, Delivered;
        public float Capture;
        public float EscortProgress;
        public bool EscortAlive = true;
        public float DefendHealth = 1f;
        public bool BossDefeated;
        public int Built;           // Bauen/Benutzen: erledigte Objekte
        public int Phase;           // Jagd: 0 = Beute jagen, 1 = Anführer · Weltboss: Bossphase
        public float BossHealth = 1f;
        public bool PlayerNotified;

        public EventRunner(EventDefinition def) { Def = def; }
        /// <summary>Jagd: Phase 1 hat gerade begonnen (Director setzt zurück, nachdem der Anführer erschienen ist).</summary>
        public bool PhaseChanged;

        public bool Active => State == RunnerState.Active;
        public float TimeLeft => Def.Duration > 0f ? Mathf.Max(0f, Def.Duration - Time) : -1f;
        public RewardTier Medal => EventChain.ComputeTier(Contribution, Def.GoldContribution);
        /// <summary>Belohnungsfaktor: Medaille × (Erfolg 100 % / Fehlschlag 25 %).</summary>
        public float RewardMultiplier => EventChain.TierMultiplier(Medal) * (State == RunnerState.Failed ? 0.25f : 1f);

        public float Progress
        {
            get
            {
                switch (Def.Objective)
                {
                    case EventObjective.Kill: return Mathf.Clamp01(Kills / (float)Mathf.Max(1, Def.TargetCount));
                    case EventObjective.Defend: return Def.Duration > 0f ? Mathf.Clamp01(Time / Def.Duration) : 0f;
                    case EventObjective.Escort: return Mathf.Clamp01(EscortProgress);
                    case EventObjective.Collect: return Mathf.Clamp01(Delivered / (float)Mathf.Max(1, Def.TargetCount));
                    case EventObjective.Capture: return Mathf.Clamp01(Capture);
                    case EventObjective.Boss: return BossDefeated ? 1f : 0f;
                    case EventObjective.Build:
                    case EventObjective.Use: return Mathf.Clamp01(Built / (float)Mathf.Max(1, Def.TargetCount));
                    case EventObjective.Hunt: return BossDefeated ? 1f : Phase == 0 ? 0.5f * Mathf.Clamp01(Kills / (float)Mathf.Max(1, Def.TargetCount)) : 0.5f + 0.5f * (1f - BossHealth);
                    case EventObjective.WorldBoss: return BossDefeated ? 1f : 1f - BossHealth;
                }
                return 0f;
            }
        }

        /// <summary>Fortschritt + Endbedingungen. Gibt true zurück, wenn das Ereignis in diesem Tick endete.</summary>
        public bool Tick(float dt, EventInputs input)
        {
            if (!Active) return false;
            Time += dt;
            switch (Def.Objective)
            {
                case EventObjective.Kill:
                    if (Kills >= Def.TargetCount) return End(true);
                    break;
                case EventObjective.Defend:
                    if (DefendHealth <= 0f) return End(false);
                    if (Def.Duration > 0f && Time >= Def.Duration) return End(true);
                    return false;
                case EventObjective.Escort:
                    if (!EscortAlive) return End(false);
                    if (EscortProgress >= 1f) return End(true);
                    break;
                case EventObjective.Collect:
                    if (Delivered >= Def.TargetCount) return End(true);
                    break;
                case EventObjective.Capture:
                    // GW2: Fortschritt nur ohne Gegner im Kreis; ohne Spieler sinkt er langsam
                    if (input.EnemiesInCapture == 0 && input.PlayerInCapture) Capture = Mathf.Min(1f, Capture + dt / Mathf.Max(0.1f, Def.CaptureTime));
                    else if (!input.PlayerInCapture) Capture = Mathf.Max(0f, Capture - dt / Mathf.Max(0.1f, Def.CaptureTime * 2f));
                    if (Capture >= 1f) return End(true);
                    break;
                case EventObjective.Boss:
                case EventObjective.WorldBoss:
                    if (BossDefeated) return End(true);
                    if (Def.Objective == EventObjective.WorldBoss) Phase = EventRules.PhaseFor(BossHealth, Def.PhaseThresholds);
                    break;
                case EventObjective.Build:
                case EventObjective.Use:
                    if (Built >= Def.TargetCount) return End(true);
                    break;
                case EventObjective.Hunt:
                    if (Phase == 0 && Kills >= Def.TargetCount) { Phase = 1; PhaseChanged = true; }
                    if (Phase == 1 && BossDefeated) return End(true);
                    break;
            }
            if (Def.Duration > 0f && Time >= Def.Duration) return End(false);
            return false;
        }

        public bool End(bool success)
        {
            if (!Active) return false;
            State = success ? RunnerState.Succeeded : RunnerState.Failed;
            return true;
        }
    }

    /// <summary>
    /// Ereignis-Scheduler (reines Modell): Auslöser auswerten, bis zu <see cref="MaxParallel"/> Ereignisse parallel,
    /// Ketten über Erfolg/Fehlschlag, Cooldowns, Welt-Zustand (Flags mit Rückbau nach Ablauf).
    /// </summary>
    public class EventScheduler
    {
        public enum DefState { Idle, Running, Cooldown }
        class Slot { public EventDefinition Def; public DefState State; public float Timer; public float RandomAt = -1f; }

        readonly List<Slot> _slots = new List<Slot>();
        readonly List<EventRunner> _runners = new List<EventRunner>();
        readonly Dictionary<string, float> _flags = new Dictionary<string, float>();   // Flag → Ablaufzeit (∞ = bleibt)
        readonly HashSet<string> _heartTriggers = new HashSet<string>();
        readonly System.Random _rng;
        readonly List<string> _expired = new List<string>();
        public int MaxParallel = 3;
        public float Now { get; private set; }

        public event Action<EventRunner> Started;
        public event Action<EventRunner> Ended;
        public event Action<string, bool> FlagChanged;        // Flag, gesetzt?

        public EventScheduler(IEnumerable<EventDefinition> defs, int seed = 1234)
        {
            _rng = new System.Random(seed);
            _lastCycleCheck = WorldClock.Seconds;
            foreach (var d in defs) if (d != null) _slots.Add(new Slot { Def = d, State = DefState.Idle });
        }

        public IReadOnlyList<EventRunner> Runners => _runners;
        public int RunningCount { get { int n = 0; foreach (var r in _runners) if (r.Active) n++; return n; } }
        public bool HasFlag(string f) => !string.IsNullOrEmpty(f) && _flags.ContainsKey(f);
        public IEnumerable<string> Flags => _flags.Keys;
        public DefState StateOf(string id) { var s = Find(id); return s != null ? s.State : DefState.Idle; }
        public float TimerOf(string id) { var s = Find(id); return s != null ? s.Timer : 0f; }
        public EventRunner RunnerOf(string id) { foreach (var r in _runners) if (r.Def.Id == id && r.Active) return r; return null; }
        Slot Find(string id) { foreach (var s in _slots) if (s.Def.Id == id) return s; return null; }

        public void SetFlag(string flag, float duration)
        {
            if (string.IsNullOrEmpty(flag)) return;
            bool had = _flags.ContainsKey(flag);
            _flags[flag] = duration > 0f ? Now + duration : float.PositiveInfinity;
            if (!had) FlagChanged?.Invoke(flag, true);
        }

        public void ClearFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && _flags.Remove(flag)) FlagChanged?.Invoke(flag, false);
        }

        /// <summary>Herz-Interaktion als Auslöser (Herz-NPC startet ein Ereignis).</summary>
        public void NotifyHeart(string heartId) { if (!string.IsNullOrEmpty(heartId)) _heartTriggers.Add(heartId); }

        bool ChainBusy(EventDefinition d)
        {
            if (string.IsNullOrEmpty(d.ChainId)) return false;
            foreach (var s in _slots) if (s.Def != d && s.Def.ChainId == d.ChainId && s.State == DefState.Running) return true;
            return false;
        }

        public void Tick(float dt, Vector3 playerPos)
        {
            Now += dt;
            _expired.Clear();
            foreach (var kv in _flags) if (kv.Value <= Now) _expired.Add(kv.Key);
            foreach (var f in _expired) ClearFlag(f);

            foreach (var s in _slots)
            {
                if (s.State == DefState.Cooldown) { s.Timer -= dt; if (s.Timer <= 0f) { s.State = DefState.Idle; s.Timer = 0f; s.RandomAt = -1f; } continue; }
                if (s.State != DefState.Idle) continue;
                s.Timer += dt;
                if (s.Def.IsChainMember || ChainBusy(s.Def)) continue;
                if (RunningCount >= MaxParallel) continue;
                if (TriggerSatisfied(s, playerPos)) Start(s);
            }
            _heartTriggers.Clear();
            _lastCycleCheck = WorldClock.Seconds;
        }
        float _lastCycleCheck = -1f;

        bool TriggerSatisfied(Slot s, Vector3 playerPos)
        {
            foreach (var t in s.Def.Triggers)
            {
                switch (t.Type)
                {
                    case EventTriggerType.Timer:
                        if (s.Timer >= t.Delay) return true; break;
                    case EventTriggerType.Proximity:
                    {
                        float dx = playerPos.x - s.Def.Center.x, dz = playerPos.z - s.Def.Center.z, r = s.Def.Radius * t.ProximityFactor;
                        if (s.Timer >= t.Delay && dx * dx + dz * dz <= r * r) return true; break;
                    }
                    case EventTriggerType.WorldState:
                        if (HasFlag(t.Param) && s.Timer >= t.Delay) return true; break;
                    case EventTriggerType.Heart:
                        if (_heartTriggers.Contains(t.Param)) return true; break;
                    case EventTriggerType.Cycle:
                        if (WorldClock.CycleStartedSince(t.Period, t.Delay, _lastCycleCheck, WorldClock.Seconds)) return true; break;
                    case EventTriggerType.RandomWindow:
                        if (s.RandomAt < 0f) s.RandomAt = t.Min + (float)_rng.NextDouble() * Mathf.Max(0f, t.Max - t.Min);
                        if (s.Timer >= s.RandomAt) return true; break;
                }
            }
            return false;
        }

        /// <summary>Startet ein Ereignis sofort (Debug, Ketten-Glied). Gibt den Runner zurück (null, wenn schon aktiv).</summary>
        public EventRunner Start(string id) { var s = Find(id); return s != null && s.State != DefState.Running ? Start(s) : null; }

        EventRunner Start(Slot s)
        {
            s.State = DefState.Running; s.Timer = 0f;
            var r = new EventRunner(s.Def);
            _runners.Add(r);
            Started?.Invoke(r);
            return r;
        }

        /// <summary>Tickt alle aktiven Runner; beendete werden ausgewertet (Flags, Ketten, Cooldown).</summary>
        public void TickRunners(float dt, Func<EventRunner, EventInputs> inputs)
        {
            for (int i = 0; i < _runners.Count; i++)
            {
                var r = _runners[i];
                if (!r.Active) continue;
                if (r.Tick(dt, inputs != null ? inputs(r) : default(EventInputs))) Finish(r);
            }
            _runners.RemoveAll(r => !r.Active);
        }

        /// <summary>Beendet ein Ereignis von außen (z. B. Eskorte gestorben) und wertet es aus.</summary>
        public void Complete(EventRunner r, bool success)
        {
            if (r == null || !r.End(success)) return;
            Finish(r);
            _runners.Remove(r);
        }

        void Finish(EventRunner r)
        {
            var d = r.Def;
            bool ok = r.State == RunnerState.Succeeded;
            var s = Find(d.Id);
            if (s != null) { s.State = DefState.Cooldown; s.Timer = Mathf.Max(0.01f, d.Cooldown); }
            if (ok) { SetFlag(d.SetFlagOnSuccess, d.FlagDuration); ClearFlag(d.ClearFlagOnSuccess); }
            else { SetFlag(d.SetFlagOnFail, d.FlagDuration); ClearFlag(d.ClearFlagOnFail); }
            Ended?.Invoke(r);
            string next = ok ? d.SuccessNext : d.FailNext;
            if (!string.IsNullOrEmpty(next)) { var ns = Find(next); if (ns != null && ns.State != DefState.Running) Start(ns); }
        }
    }
}

namespace ProjectTyrant
{
    /// <summary>Weltuhr (Sitzung, nicht gespeichert – GW2: Weltboss-Timer laufen serverseitig): Zyklen für Weltboss und Meta.</summary>
    public static class WorldClock
    {
        public static float Seconds;
        /// <summary>Beginn des nächsten Zyklus ab <paramref name="now"/> (Periode, Versatz).</summary>
        public static float NextStart(float period, float offset, float now)
        {
            if (period <= 0f) return float.PositiveInfinity;
            if (now < offset) return offset;
            return offset + (Mathf.Floor((now - offset) / period) + 1f) * period;
        }
        /// <summary>Hat zwischen <paramref name="from"/> (exklusiv) und <paramref name="to"/> (inklusiv) ein Zyklus begonnen?</summary>
        public static bool CycleStartedSince(float period, float offset, float from, float to)
        {
            if (period <= 0f || to < offset) return false;
            float k = Mathf.Floor((to - offset) / period);
            float start = offset + k * period;
            return start > from && start <= to;
        }
        public static string Format(float seconds) { int s = Mathf.Max(0, Mathf.CeilToInt(seconds)); return (s / 60) + ":" + (s % 60).ToString("00"); }
    }

    /// <summary>Reine Regeln: Bossphasen nach HP-Schwellen.</summary>
    public static class EventRules
    {
        public static int PhaseFor(float hp, float[] thresholds)
        {
            int p = 0;
            if (thresholds != null) foreach (var t in thresholds) if (hp <= t) p++;
            return p;
        }
    }
}
