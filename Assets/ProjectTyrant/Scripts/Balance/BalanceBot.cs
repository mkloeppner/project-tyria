using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: minimaler Autopilot (kein TestPilot im Projekt) mit den Profilen Anfänger/Erfahren. Läuft auf Herzen und Events zu, kämpft mit Skill 1–n,
    /// heilt, weicht Telegraphen aus (Anfänger jedem zweiten, 0,6 s Reaktion), flieht unter einer HP-Schwelle und schreibt Telemetrie.
    /// Deterministisch mit Seed (Zielwahl, Ausweich-Zähler); 30-Minuten-Sessions laufen beschleunigt (Time.timeScale).
    /// </summary>
    public class BalanceBot : MonoBehaviour
    {
        public static BalanceBot Active { get; private set; }
        public static readonly Queue<(BotProfileKind kind, BalancePlan.Cell cell, int seed, float seconds)> Plan = new Queue<(BotProfileKind, BalancePlan.Cell, int, float)>();
        public static float TimeScale = 4f;
        public static string LastReport = "";

        [System.NonSerialized] public BotSettings S;
        [System.NonSerialized] public string ZoneId; [System.NonSerialized] public int Seed; [System.NonSerialized] public float Seconds; [System.NonSerialized] public int Danger; [System.NonSerialized] public Vector3? StartPos; [System.NonSerialized] public int Level;
        System.Random _rng;
        Combatant _p; PlayerController _pc; SkillCaster _caster; TargetingSystem _targeting; DownedController _downed; OutOfCombatRegen _regen;
        float _t, _nextThink, _respawnAt = -1f, _stuckCheck, _goalUntil;
        Vector3 _lastPos, _goal; bool _hasGoal; string _goalName = "";
        Combatant _enemy; bool _fleeing;
        readonly HashSet<GroundTelegraph> _seenTele = new HashSet<GroundTelegraph>();
        int _teleCount; float _dodgeAt = -1f; Vector3 _dodgeDir;
        readonly Vector3[] _corners = new Vector3[48]; int _cornerCount, _cornerIdx; float _repath; NavMeshPath _path;
        bool _started; float _zoneWait;
        public int Stuck, Teleports;

        /// <summary>Startet die geplante Serie (je Eintrag eine Session).</summary>
        public static void RunPlan()
        {
            if (Active != null || Plan.Count == 0) return;
            var go = new GameObject("QA_BalanceBot");
            DontDestroyOnLoad(go);   // überlebt Zonenwechsel
            Active = go.AddComponent<BalanceBot>();
            Active.Next();
        }

        public static void Stop()
        {
            Plan.Clear();
            if (Active != null) { Active.Finish(false); }
        }

        void Next()
        {
            if (Plan.Count == 0) { EndPlan(); return; }
            var e = Plan.Dequeue();
            S = BotSettings.For(e.kind); ZoneId = e.cell.Zone; Seed = e.seed; Seconds = e.seconds; Danger = e.cell.Danger; StartPos = e.cell.Start; Level = e.cell.Level;
            _rng = new System.Random(Seed * 7919 + (int)e.kind * 31);
            _t = 0f; _started = false; _zoneWait = 0f; _seenTele.Clear(); _teleCount = 0; _hasGoal = false; Stuck = 0; Teleports = 0;
            // Zustand je Session zurücksetzen (Fehler: eine Ausweichrolle vom Ende der vorigen Session blockierte die Bewegung 30 min lang)
            _dodgeAt = -1f; _respawnAt = -1f; _deadAt = 0f; _enemy = null; _fleeing = false; _nextThink = 0f; _goalUntil = 0f;
            _seenWind.Clear(); _seenWindAt.Clear(); TelemetryProbe.FleeIntent = false;
            var gm = GameManager.Instance;
            if (gm != null && (ZoneRoot.Current == null || ZoneRoot.Current.Id != ZoneId)) gm.ChangeZone(ZoneId, null);
        }

        void Begin()
        {
            var gm = GameManager.Instance; _p = gm != null ? gm.Player : null;
            if (_p == null) return;
            _pc = _p.GetComponent<PlayerController>(); _caster = _p.GetComponent<SkillCaster>(); _targeting = _p.GetComponent<TargetingSystem>();
            _downed = _p.GetComponent<DownedController>(); _regen = _p.GetComponent<OutOfCombatRegen>();
            if (_pc != null) _pc.GodMode = false;
            var prof = PlayerProfile.Instance;
            if (prof != null) { if (!_savedLevel.HasValue) _savedLevel = prof.Progression.Level; prof.SaveSuspended = true; }
            if (Level > 0) DebugCommands.SetLevel(Level);
            BalanceLoadout.Apply(_p, Level > 0 ? Level : _p.Level);
            var start = StartPos ?? (ZoneRoot.Current != null && ZoneRoot.Current.Definition != null ? ZoneRoot.Current.Definition.DefaultSpawn : _p.transform.position);
            Respawn(start);
            Random.InitState(Seed);
            BalanceTelemetry.Begin(S.Kind.ToString(), ZoneId, Seed, Danger);
            TelemetryProbe.Attach(_p);
            _lastPos = _p.transform.position; _stuckCheck = 3f;
            Time.timeScale = TimeScale;
            _started = true;
            GameEventBus.PlayerDefeated += OnDefeated;
        }

        void OnDestroy() { GameEventBus.PlayerDefeated -= OnDefeated; if (Active == this) Active = null; }

        float _deadAt;
        void OnDefeated(Combatant c) { if (c == _p) { _respawnAt = _t + 3f; _deadAt = _t; } }

        static int? _savedLevel;
        void EndPlan()
        {
            Time.timeScale = 1f; PlayerController.QaWishWorld = null;
            BalanceLoadout.Remove();
            var prof = PlayerProfile.Instance;
            if (prof != null) { prof.SaveSuspended = false; DebugCommands.Load(); }   // Spielstand unverändert: vom Datenträger neu laden
            _savedLevel = null;
            Destroy(gameObject); Active = null;
            Debug.Log("[ProjectTyrant] Bot-Serie fertig:\n" + LastReport);
        }

        void Finish(bool next)
        {
            GameEventBus.PlayerDefeated -= OnDefeated;
            PlayerController.QaWishWorld = null;
            var s = BalanceTelemetry.End();
            if (s != null) LastReport += s.Id + ": Tode " + s.Deaths + " · Flucht " + s.EscapeSuccess + "/" + s.EscapeAttempts + " · Events 0:" + s.EventsWon(0) + "/" + s.EventsStarted(0) + " 1:" + s.EventsWon(1) + "/" + s.EventsStarted(1) + " 2:" + s.EventsWon(2) + "/" + s.EventsStarted(2) + " · hängen " + Stuck + "\n";
            Time.timeScale = 1f;
            _started = false;
            if (next) Next(); else { Plan.Clear(); EndPlan(); }
        }

        void Respawn(Vector3 at)
        {
            if (_pc != null) _pc.Teleport(at + Vector3.up * 0.3f, Quaternion.identity);
            if (_downed != null) _downed.ResetState();
            if (_p.IsDead) { _p.Revive(1f); GameEventBus.RaisePlayerRespawned(_p); }
            var ui = UIController.Instance; if (ui != null && ui.WorldMap != null) ui.WorldMap.Window.Close();
        }

        void Update()
        {
            if (!_started)
            {
                _zoneWait += Time.unscaledDeltaTime;
                if (ZoneRoot.Current != null && ZoneRoot.Current.Id == ZoneId && _zoneWait > 2f && GameManager.Instance != null && GameManager.Instance.Player != null) Begin();
                else if (_zoneWait > 60f) { Debug.LogWarning("[ProjectTyrant] Bot: Zone " + ZoneId + " nicht geladen"); Next(); }
                return;
            }
            float dt = Time.deltaTime; _t += dt;
            if (_t >= Seconds) { Finish(true); return; }
            if (_p == null) return;
            if (_p.IsDead)
            {
                PlayerController.QaWishWorld = Vector3.zero;
                if (NpcRescue.Local != null && NpcRescue.Local.Pending && _t < _deadAt + 24f) return;   // Hilfe ist unterwegs: warten
                if (_respawnAt > 0f && _t >= _respawnAt) { _respawnAt = -1f; Respawn(NearestWaypoint(_p.transform.position)); _fleeing = false; _enemy = null; }
                return;
            }
            if (_p.IsDowned) { DownedAct(); return; }
            Think(dt);
            Move(dt);
            Dodge();
        }

        // ---------------- Entscheidungen ----------------
        void Think(float dt)
        {
            _nextThink -= dt;
            if (_nextThink > 0f) return;
            _nextThink = 0.2f;
            Vector3 me = _p.transform.position;
            float hp = _p.HealthFraction;
            var attackers = Pursuers(me, out var nearest);
            // Heilen
            if (hp < S.HealBelow && _caster != null && _caster.HealSkill != null) _caster.TryCast(_caster.HealSkill, true);
            // Flucht
            if (!_fleeing && hp < S.FleeBelow && attackers > 0) { _fleeing = true; TelemetryProbe.FleeIntent = true; }
            if (_fleeing && (attackers == 0 || hp > 0.7f)) { _fleeing = false; TelemetryProbe.FleeIntent = false; }
            if (_fleeing)
            {
                if (S.FleeToSafety) SetGoal(NearestWaypoint(me), "Flucht → Wegmarke");
                else if (nearest != null) { var away = me - nearest.transform.position; away.y = 0f; SetGoal(me + away.normalized * 40f, "Flucht"); }
                _enemy = null;
                return;
            }
            // Kampf: Angreifer zuerst, sonst feindliche Gegner in der Nähe des Ziels (Herz/Event)
            if (_enemy == null || _enemy.IsDead || !_enemy.isActiveAndEnabled || Flat(_enemy.transform.position - me) > 30f)
            {
                _enemy = nearest;
                if (_enemy == null && _hasGoal && Flat(_goal - me) < 25f) _enemy = HostileNear(me, 14f);
            }
            if (_enemy != null)
            {
                if (_targeting != null && _targeting.Current != _enemy) _targeting.SetTarget(_enemy);
                float d = Flat(_enemy.transform.position - me);
                float range = 3f;
                var s1 = _caster != null ? _caster.GetSkillInSlot(0) : null; if (s1 != null) range = Mathf.Max(2.5f, s1.Range * 0.85f);
                if (d > range) SetGoal(_enemy.transform.position, "Gegner"); else { _hasGoal = false; PlayerController.QaWishWorld = Vector3.zero; _pc.FacingOverride = _enemy.transform.position; }
                if (d <= range + 1f) UseSkills();
                return;
            }
            // Ziel: Event (aktiv, in Reichweite) → Herz → zufälliger Ort
            if (!_hasGoal || _t > _goalUntil || Flat(_goal - me) < 4f) PickObjective(me);
            InteractNear(me);
        }

        int Pursuers(Vector3 me, out Combatant nearest)
        {
            nearest = null; float best = float.MaxValue; int n = 0;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != Team.Enemy) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b == null || b.Target != _p) continue;
                if (b.State == NpcState.Reset) continue;
                n++;
                float d = Flat(c.transform.position - me);
                if (d < best) { best = d; nearest = c; }
            }
            return n;
        }

        Combatant HostileNear(Vector3 me, float r)
        {
            Combatant best = null; float bd = r;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != Team.Enemy || !c.Targetable) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b == null || b.State == NpcState.Reset || b.IsYellow) continue;   // Anfänger greift neutrale (gelbe) nicht an
                if (S.ReadsDanger && _p != null && c.Level > _p.Level + S.AvoidLevelAbove) continue;
                float d = Flat(c.transform.position - me);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        void UseSkills()
        {
            if (_caster == null || _caster.IsCasting) return;
            int order = _rng.Next(S.WeaponSkills);
            for (int k = 0; k < S.WeaponSkills; k++)
            {
                int slot = (order + k) % S.WeaponSkills;
                if (slot == 0) continue;   // Auto-Angriff läuft über Skill 1
                if (_caster.TryUseSlot(slot, true)) return;
            }
            if (S.UseUtilities) for (int slot = 6; slot <= 8; slot++) if (_caster.TryUseSlot(slot, true)) return;
            _caster.TryUseSlot(0, true);
        }

        void PickObjective(Vector3 me)
        {
            _goalUntil = _t + 90f;
            // 1) aktives Event der Zone
            var dir = EventDirector.Instance;
            if (dir != null && dir.Scheduler != null)
            {
                EventRunner pick = null; float bd = float.MaxValue;
                foreach (var r in dir.Scheduler.Runners)
                {
                    if (r == null || !r.Active || r.Def == null || r.Def.Objective == EventObjective.WorldBoss) continue;
                    if (Danger >= 0 && BalanceWorld.DangerAt(r.Def.Center) != Danger) continue;
                    float d = Flat(r.Def.Center - me);
                    if (d < bd) { bd = d; pick = r; }
                }
                if (pick != null && (bd < 200f || _rng.NextDouble() < 0.5)) { SetGoal(pick.Def.Center, "Event " + pick.Def.Id); return; }
            }
            // 2) unfertiges Herz
            var hearts = new List<Heart>(FindObjectsByType<Heart>());
            hearts.RemoveAll(h => h == null || h.Done || (S.ReadsDanger && TooHigh(h.Center)) || (Danger >= 0 && BalanceWorld.DangerAt(h.Center) != Danger));
            if (hearts.Count > 0 && _rng.NextDouble() < 0.7)
            {
                hearts.Sort((a, b) => Flat(a.Center - me).CompareTo(Flat(b.Center - me)));
                var h = hearts[Mathf.Min(hearts.Count - 1, _rng.Next(2))];
                var off = Random.insideUnitCircle * h.Radius * 0.5f;
                SetGoal(h.Center + new Vector3(off.x, 0f, off.y), "Herz " + h.Id);
                return;
            }
            // 3) zufälliger Ort der Zone (Sehenswürdigkeiten, Wegmarken)
            var objs = FindObjectsByType<ZoneObject>();
            if (objs.Length > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    var o = objs[_rng.Next(objs.Length)];
                    if (S.ReadsDanger && TooHigh(o.transform.position)) continue;
                    if (Danger >= 0 && BalanceWorld.DangerAt(o.transform.position) != Danger) continue;
                    SetGoal(o.transform.position, o.name); return;
                }
            }
            var z = ZoneRoot.Current != null && ZoneRoot.Current.Definition != null ? ZoneRoot.Current.Definition.Size * 0.4f : 80f;
            for (int i = 0; i < 30; i++)
            {
                var g = new Vector3(((float)_rng.NextDouble() * 2f - 1f) * z, 0f, ((float)_rng.NextDouble() * 2f - 1f) * z);
                if (Danger < 0 || BalanceWorld.DangerAt(g) == Danger) { SetGoal(g, "Wildnis"); return; }
            }
            SetGoal(StartPos ?? me, "Start");
        }

        bool TooHigh(Vector3 p) { var r = ZoneRoot.LevelRangeAt(p); return _p != null && r.x >= _p.Level + S.AvoidLevelAbove; }

        void InteractNear(Vector3 me)
        {
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || it is Waypoint) continue;
                if (Flat(it.InteractPosition - me) > Mathf.Max(2.5f, it.Range)) continue;
                if (it.ChannelTime > 0f) continue;
                if (!it.CanInteract(_p)) continue;
                if (it is HeartTask || it.GetType().Name.Contains("Crate") || it.GetType().Name.Contains("Site") || it.GetType().Name.Contains("Pickup")) { it.Interact(_p); return; }
            }
        }

        void SetGoal(Vector3 g, string name) { _goal = g; _hasGoal = true; _goalName = name; _repath = 0f; }

        // ---------------- Bewegung ----------------
        void Move(float dt)
        {
            if (_dodgeAt > 0f) return;
            Vector3 me = _p.transform.position;
            if (!_hasGoal) { PlayerController.QaWishWorld = Vector3.zero; return; }
            _repath -= dt;
            if (_repath <= 0f)
            {
                _repath = 0.7f; _cornerCount = 0;
                if (_path == null) _path = new NavMeshPath();
                if (NavMesh.SamplePosition(me, out var a, 4f, NavMesh.AllAreas) && NavMesh.SamplePosition(_goal, out var b, 8f, NavMesh.AllAreas) && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _path) && _path.status != NavMeshPathStatus.PathInvalid)
                { _cornerCount = _path.GetCornersNonAlloc(_corners); _cornerIdx = 1; }
            }
            while (_cornerIdx < _cornerCount - 1 && Flat(_corners[_cornerIdx] - me) < 1.2f) _cornerIdx++;
            Vector3 tgt = _cornerCount >= 2 && _cornerIdx < _cornerCount ? _corners[_cornerIdx] : _goal;
            Vector3 d = tgt - me; d.y = 0f;
            if (d.magnitude < 0.6f && Flat(_goal - me) < 2f) { PlayerController.QaWishWorld = Vector3.zero; _hasGoal = _enemy != null; return; }
            PlayerController.QaWishWorld = d.normalized;
            if (_enemy == null) _pc.FacingOverride = null;
            // Hängen-Erkennung
            _stuckCheck -= dt;
            if (_stuckCheck <= 0f)
            {
                _stuckCheck = 3f;
                if (Flat(me - _lastPos) < 1f)
                {
                    Stuck++;
                    if (Stuck % 3 == 0 && _cornerCount >= 2 && _cornerIdx < _cornerCount) { _pc.Teleport(_corners[_cornerIdx] + Vector3.up * 0.5f, _p.transform.rotation); Teleports++; }
                    else { _hasGoal = false; var r = Random.insideUnitCircle.normalized * 12f; SetGoal(me + new Vector3(r.x, 0, r.y), "Ausweg"); }
                }
                _lastPos = me;
            }
        }

        // ---------------- Ausweichen ----------------
        void Dodge()
        {
            Vector3 me = _p.transform.position;
            if (_dodgeAt > 0f)
            {
                if (_t >= _dodgeAt) { _dodgeAt = -1f; if (_pc.Dodge.TryStartDodge(_dodgeDir)) GameEventBus.RaiseDodge(_p); }
                return;
            }
            foreach (var g in FindObjectsByType<GroundTelegraph>())
            {
                if (g == null || g.Resolved || _seenTele.Contains(g)) continue;
                if (g.Owner != null && !g.Owner.IsHostileTo(_p)) continue;
                if (Flat(g.transform.position - me) > g.Radius + 0.5f) continue;
                _seenTele.Add(g); _teleCount++;
                if (!S.DodgesTelegraph(_teleCount)) continue;
                var away = me - g.transform.position; away.y = 0f;
                _dodgeDir = away.sqrMagnitude > 0.01f ? away.normalized : -_p.transform.forward;
                _dodgeAt = _t + S.Reaction;
                return;
            }
            // Veteranen-/Champion-Aufladungen zählen ebenfalls als Telegraph
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != Team.Enemy || c.Tier < EnemyTier.Veteran) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b == null || !b.IsWindingUp || b.Target != _p || b.WindupProgress > 0.3f) continue;
                if (_seenWind.Contains(b) && _t - _seenWindAt[b] < 1.5f) continue;
                _seenWind.Add(b); _seenWindAt[b] = _t; _teleCount++;
                if (!S.DodgesTelegraph(_teleCount)) continue;
                var away = me - c.transform.position; away.y = 0f;
                _dodgeDir = away.sqrMagnitude > 0.01f ? away.normalized : -_p.transform.forward;
                _dodgeAt = _t + S.Reaction;
                return;
            }
        }
        readonly HashSet<NpcBrain> _seenWind = new HashSet<NpcBrain>(); readonly Dictionary<NpcBrain, float> _seenWindAt = new Dictionary<NpcBrain, float>();

        void DownedAct()
        {
            PlayerController.QaWishWorld = Vector3.zero;
            if (_caster == null) return;
            // Downed: Skill 1 auf den Gegner, Bandagieren (4) wenn niemand in Reichweite
            int near = Pursuers(_p.transform.position, out var n);
            if (near == 0 || (n != null && Flat(n.transform.position - _p.transform.position) > 6f)) { if (_caster.DownedSkills != null && _caster.DownedSkills.Length > 3 && _caster.DownedSkills[3] != null) _caster.TryCast(_caster.DownedSkills[3], true); }
            else if (_caster.DownedSkills != null && _caster.DownedSkills.Length > 0 && _caster.DownedSkills[0] != null) { _targeting?.SetTarget(n); _caster.TryCast(_caster.DownedSkills[0], true); }
        }

        Vector3 NearestWaypoint(Vector3 p)
        {
            Vector3 best = p; float bd = float.MaxValue;
            foreach (var w in FindObjectsByType<Waypoint>())
            {
                float d = Flat(w.transform.position - p);
                if (d < bd) { bd = d; best = w.transform.position; }
            }
            return best;
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    }
}
