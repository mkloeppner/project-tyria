using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M25+M27: QA-Duell – spielt eine Klasse automatisch gegen eine Folge von Veteranen (Veteran_Brecher, Stufe = Spielerstufe)
    /// und misst Schaden/s, Kampfdauer je Kill, erlittenen Schaden, Niederlagen und Exceptions. Grundlage des Live-Checks (M25)
    /// und des Balancings (M27, Ziel ±15 %). Läuft über den Debug-Klassentest, der echte Spielstand wird dabei nicht gespeichert.
    /// </summary>
    public class ClassDuelBot : MonoBehaviour
    {
        public struct Result
        {
            public string ClassId; public float Seconds, DamageDealt, DamageTaken; public int Kills, Downs, Casts, Mechanic, Exceptions, Dodges;
            public float Dps => Seconds > 0f ? DamageDealt / Seconds : 0f;
            public float TakenPerSecond => Seconds > 0f ? DamageTaken / Seconds : 0f;
            public float AvgKill => Kills > 0 ? Seconds / Kills : Seconds;
            public override string ToString() => ClassId + ": " + Seconds.ToString("0") + " s · Kills " + Kills + " · Ø " + AvgKill.ToString("0.0") + " s/Kill · DPS " + Dps.ToString("0") + " · erlitten " + TakenPerSecond.ToString("0") + "/s · Niederlagen " + Downs + " · Casts " + Casts + " · Ausweichen " + Dodges + " · Mechanik " + Mechanic + " · Exceptions " + Exceptions;
        }

        public static readonly List<Result> Results = new List<Result>();
        public static bool Running { get; private set; }
        public static string Report { get { var sb = new StringBuilder(); foreach (var r in Results) sb.AppendLine(r.ToString()); return sb.ToString(); } }

        /// <summary>Startet die Duellserie: jede Klasse <paramref name="seconds"/> Spielzeit, mit <paramref name="timeScale"/> beschleunigt.</summary>
        public static void Run(string[] classIds, float seconds, float timeScale, string archetypeId = "Veteran_Brecher")
        {
            if (Running) return;
            Results.Clear();
            var go = new GameObject("QA_ClassDuelBot"); var b = go.AddComponent<ClassDuelBot>();
            b._queue = new Queue<string>(classIds); b._seconds = seconds; b._scale = timeScale; b._archetypeId = archetypeId;
            Running = true;
        }

        Queue<string> _queue; float _seconds, _scale; string _archetypeId;
        Result _cur; float _t, _nextAct, _respawnAt, _nextMech, _nextDodge, _nextElite; int _mechStep; Combatant _player, _enemy; SkillCaster _caster; ClassMechanicHost _host;
        EnemyArchetype _arch; Vector3 _arena; string _ownClass; bool _active; string _eliteId; bool _sigCast; float _sigShot;
        /// <summary>Captures der Signatur-Skills (Temp/PT_QA/m26_&lt;spez&gt;.png).</summary>
        public static bool CaptureSignatures = true;

        void Start()
        {
            Application.logMessageReceived += OnLog;
            var p = PlayerProfile.Instance; _ownClass = p != null && p.ActiveCharacter != null ? p.ActiveCharacter.ClassId : "waechter";
            foreach (var a in Resources.FindObjectsOfTypeAll<EnemyArchetype>()) if (a.name == _archetypeId) _arch = a;
            if (_arch == null) // nicht geladen: einen Veteranen der aktuellen Zone verwenden
                foreach (var n in FindObjectsByType<NpcBrain>()) if (n.Archetype != null && n.Archetype.Tier == EnemyTier.Veteran) { _arch = n.Archetype; break; }
            if (_arch != null) Debug.Log("[ProjectTyrant] Duell gegen " + _arch.name);
            Time.timeScale = _scale;
            NextClass();
        }

        void OnDestroy() { Application.logMessageReceived -= OnLog; Time.timeScale = 1f; Running = false; }

        void OnLog(string msg, string stack, LogType type) { if (_active && (type == LogType.Exception || type == LogType.Error)) _cur.Exceptions++; }

        void NextClass()
        {
            Cleanup();
            if (_queue.Count == 0)
            {
                var p0 = PlayerProfile.Instance; if (p0 != null) p0.DebugSwitchClass(_ownClass);
                Debug.Log("[ProjectTyrant] Klassen-Duell fertig:\n" + Report);
                Destroy(gameObject); return;
            }
            var entry = _queue.Dequeue(); var parts = entry.Split('/'); var id = parts[0]; _eliteId = parts.Length > 1 ? parts[1] : null;
            var p = PlayerProfile.Instance; var gm = GameManager.Instance;
            if (p == null || gm == null || gm.Player == null || _arch == null) { Debug.LogWarning("[ProjectTyrant] Duell: Profil/Spieler/Archetyp fehlt"); Destroy(gameObject); return; }
            p.DebugSwitchClass(id); p.SaveSuspended = true; p.DebugUnlockAll();
            p.State.Loadout.FillDefaults(p.CurrentClass.SkillPool);
            if (_eliteId != null) Debug.Log("[ProjectTyrant] Duell-Elite: " + p.DebugActivateElite(_eliteId));
            _player = gm.Player; _caster = _player.GetComponent<SkillCaster>(); _host = _player.GetComponent<ClassMechanicHost>();
            p.State.Loadout.ApplyTo(_caster);
            for (int i = 0; i < 10; i++) { var s = _caster.GetSkillInSlot(i); if (s != null) _caster.SetCooldown(s, 0f); }
            _player.Revive(1f); var dc = _player.GetComponent<DownedController>(); if (dc != null) dc.ResetState();
            _arena = FindQuietSpot(_player.transform.position);
            var pcon = _player.GetComponent<PlayerController>(); if (pcon != null) pcon.Teleport(_arena, _player.transform.rotation); else _player.transform.position = _arena;
            _cur = new Result { ClassId = _eliteId != null ? id + "/" + _eliteId : id }; _sigCast = _eliteId == null; _sigShot = -1f; _t = 0f; _nextAct = 0f; _respawnAt = 0.5f; _nextMech = 2f; _nextElite = 3f; _mechStep = 0; _active = true;
            _player.Damaged += OnPlayerDamaged;
        }

        void Cleanup()
        {
            if (_player != null) _player.Damaged -= OnPlayerDamaged;
            if (_enemy != null) { _enemy.Damaged -= OnEnemyDamaged; Destroy(_enemy.gameObject); _enemy = null; }
            if (_active)
            {
                _cur.Seconds = _t; Results.Add(_cur);
                var list = new List<KeyValuePair<string, float>>(_sources); list.Sort((a, b) => b.Value.CompareTo(a.Value));
                var sb = new StringBuilder(); for (int i = 0; i < Mathf.Min(4, list.Count); i++) sb.Append(list[i].Key + " " + list[i].Value.ToString("0") + "; ");
                Debug.Log("[ProjectTyrant] Duell " + _cur + "\n   Schadensquellen: " + sb);
            }
            _sources.Clear();
            _active = false;
        }

        readonly Dictionary<string, float> _sources = new Dictionary<string, float>();
        void OnPlayerDamaged(DamageInfo d)
        {
            _cur.DamageTaken += d.Amount;
            var k = d.Source != null ? d.Source.DisplayName + (d.Skill != null ? "/" + d.Skill.DisplayName : "") : "?";
            _sources.TryGetValue(k, out var v); _sources[k] = v + d.Amount;
        }
        void OnEnemyDamaged(DamageInfo d) { _cur.DamageDealt += d.Amount; }

        void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime; _t += dt;
            if (_t >= _seconds) { NextClass(); return; }

            // Niederlage: zählen, aufstehen lassen, weiterkämpfen
            if (_player.IsDowned || _player.IsDead)
            {
                _cur.Downs++; var dc = _player.GetComponent<DownedController>(); if (dc != null) dc.ResetState(); _player.Revive(1f);
                if (_enemy != null) _enemy.SetHealth(_enemy.MaxHealth);
            }

            // Gegner (neu) erzeugen
            if (_enemy == null || _enemy.IsDead)
            {
                if (_enemy != null) { _cur.Kills++; _enemy.Damaged -= OnEnemyDamaged; Destroy(_enemy.gameObject, 2f); _enemy = null; _respawnAt = _t + 1f; }
                if (_t < _respawnAt) return;
                var npc = NpcBrain.Spawn(_arch, _arena + _player.transform.forward * 7f, Quaternion.LookRotation(-_player.transform.forward));
                if (npc == null) return;
                npc.LevelOverride = _player.Level + (_arch.Tier == EnemyTier.Veteran ? 1 : 0); npc.ApplyArchetype();
                _enemy = npc.GetComponent<Combatant>(); _enemy.Damaged += OnEnemyDamaged;
                _player.Effects.ClearConditions(); _player.Heal(_player.MaxHealth);
            }

            // Ausrichten + Abstand halten
            var to = _enemy.transform.position - _player.transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f) _player.transform.rotation = Quaternion.LookRotation(to);
            var targeting = _player.GetComponent<TargetingSystem>(); if (targeting != null && targeting.Current != _enemy) targeting.SetTarget(_enemy);
            var auto = _caster.GetSkillInSlot(0); float want = auto != null ? Mathf.Max(2f, auto.Range * 0.8f) : 3f;
            if (to.magnitude > want && _player.CanMove && !_caster.IsCasting)
            {
                var pc = _player.GetComponent<PlayerController>(); var target = _player.transform.position + to.normalized * Mathf.Min(to.magnitude - want, 7f * dt);
                if (pc != null) pc.Teleport(ZoneAnchors.GroundAt(target + Vector3.up * 2f), _player.transform.rotation); else _player.transform.position = target;
            }

            // Ausweichen wie ein aufmerksamer Spieler: bei spätem Gegner-Windup oder einem Boden-Telegraphen kurz vor dem Auslösen
            var pc2 = _player.GetComponent<PlayerController>(); var brain = _enemy.GetComponent<NpcBrain>();
            if (pc2 != null && !pc2.Dodge.IsDodging && _t >= _nextDodge)
            {
                bool threat = brain != null && brain.IsWindingUp && brain.WindupProgress > 0.55f && to.magnitude < 9f;
                if (!threat) foreach (var g in FindObjectsByType<GroundTelegraph>()) if (g != null && !g.Resolved && g.Delay - g.Elapsed < 0.45f && (g.transform.position - _player.transform.position).sqrMagnitude < g.Radius * g.Radius) { threat = true; break; }
                if (threat && pc2.Dodge.TryStartDodge(-_player.transform.forward)) { _cur.Dodges++; _nextDodge = _t + 0.8f; }
            }

            // Signatur-Skill der Spezialisierung einmal gezielt wirken und festhalten
            if (!_sigCast && _t > 6f && !_caster.IsCasting && _host != null && _host.Elite != null)
            {
                _sigCast = true; var spec = _host.Elite; SkillDefinition sig = null;
                foreach (var sk in spec.AllSkills()) if (sk != null && sk.Id.EndsWith(spec.SignatureSkill)) sig = sk;
                if (sig != null && _caster.TryCast(sig, true, sig.IsGroundTargeted ? _enemy.transform.position : (Vector3?)null, true)) { _cur.Casts++; _sigShot = _t + Mathf.Max(0.35f, sig.CastTime + 0.2f); }
            }
            if (_sigShot > 0f && _t >= _sigShot) { _sigShot = -1f; if (CaptureSignatures) ScreenCapture.CaptureScreenshot("Temp/PT_QA/m26_" + _eliteId + ".png"); }
            // Elite-Mechanik (F5) regelmäßig
            if (_host != null && _host.Elite != null && _t >= _nextElite) { _host.UseMechanic(4); _nextElite = _t + 7f; _cur.Mechanic++; }

            // Klassenmechanik
            if (_host != null && _host.Kind != ClassMechanicKind.None && _t >= _nextMech)
            {
                _nextMech = _t + 4f;
                switch (_host.Kind)
                {
                    case ClassMechanicKind.Pet: _host.UseMechanic(_mechStep % 3 == 2 ? 1 : 0); _nextMech = _t + 4f; break;
                    case ClassMechanicKind.Attunement: _host.UseMechanic((_mechStep + 1) % 4); _nextMech = _t + 8f; break;
                    case ClassMechanicKind.Initiative: _host.UseMechanic(0); _nextMech = _t + 3f; break;
                }
                _mechStep++; _cur.Mechanic++;
            }

            // Fertigkeiten: Heilung bei < 50 %, dann 5→2, Hilfe, Elite, Auto-Angriff
            if (_t < _nextAct || _caster.IsCasting || _caster.InAftercast) return;
            _nextAct = _t + 0.15f;
            if (_player.HealthFraction < 0.5f && TryCast(5)) return;
            int[] order = { 4, 3, 2, 1, 6, 7, 8, 9, 0 };
            foreach (var i in order) if (TryCast(i)) return;
        }

        /// <summary>Ruhiger Platz: kein feindlicher NPC im Umkreis von 45 m (sonst greifen Zonengegner mit ein und verfälschen die Messung).</summary>
        Vector3 FindQuietSpot(Vector3 from)
        {
            for (int ring = 0; ring < 6; ring++)
                for (int k = 0; k < 12; k++)
                {
                    float r = ring * 40f, a = k * 30f * Mathf.Deg2Rad;
                    var p = ZoneAnchors.GroundAt(from + new Vector3(Mathf.Cos(a) * r, 30f, Mathf.Sin(a) * r));
                    bool quiet = true;
                    foreach (var c in Combatant.All) if (c != null && !c.IsDead && c.Team == Team.Enemy && (c.transform.position - p).sqrMagnitude < 45f * 45f) { quiet = false; break; }
                    if (quiet) return p;
                    if (ring == 0) break;
                }
            return from;
        }

        bool TryCast(int slot)
        {
            var s = _caster.GetSkillInSlot(slot);
            if (s == null || _caster.GetCooldownRemaining(s) > 0f) return false;
            if (s.Range > 0f && s.Delivery != SkillDelivery.Self && Vector3.Distance(_player.transform.position, _enemy.transform.position) > s.Range + 1f) return false;
            Vector3? ground = s.IsGroundTargeted ? _enemy.transform.position : (Vector3?)null;
            if (!_caster.TryCast(s, true, ground)) return false;
            _cur.Casts++; return true;
        }
    }
}
