using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>QA (Fix-Lauf F1): Auto-Angriffs-Kette live prüfen – drei Glieder nacheinander, dann 2,5 s Pause und ein weiterer Angriff (muss wieder Glied 1 sein). Captures nach Temp/PT_QA/fixlauf_f1.</summary>
    public class QaChainProbe : MonoBehaviour
    {
        public static string Report = "";
        string _prefix; SkillCaster _caster; Combatant _player, _enemy; int _released; float _t, _pauseUntil = -1f, _shotAt = -1f; string _shotName; bool _done;

        public static void Begin(string prefix)
        {
            Report = "";
            var pc = FindAnyObjectByType<PlayerController>(); if (pc == null) { Report = "kein Spieler"; return; }
            EnemyArchetype arch = null;
            foreach (var n in FindObjectsByType<NpcBrain>()) if (n.Archetype != null && n.Archetype.Tier == EnemyTier.Veteran) { arch = n.Archetype; break; }
            if (arch == null) foreach (var a in Resources.FindObjectsOfTypeAll<EnemyArchetype>()) if (a.Tier == EnemyTier.Veteran) { arch = a; break; }
            if (arch == null) { Report = "kein Gegner-Typ"; return; }
            var go = new GameObject("QaChainProbe"); var p = go.AddComponent<QaChainProbe>();
            p._prefix = prefix; p._caster = pc.GetComponent<SkillCaster>(); p._player = pc.GetComponent<Combatant>();
            var npc = NpcBrain.Spawn(arch, pc.transform.position + pc.transform.forward * 2.5f, Quaternion.LookRotation(-pc.transform.forward));
            npc.LevelOverride = p._player.Level; npc.ApplyArchetype();
            p._enemy = npc.GetComponent<Combatant>();
            pc.GetComponent<TargetingSystem>().SetTarget(p._enemy);
            p._caster.Chain.Reset();
            p._caster.CastReleased += p.OnReleased;
        }

        void OnReleased(SkillDefinition s)
        {
            if (s == null || (s != _caster.GetSkillInSlot(0) && s.ChainRoot == null && (s.Chain == null || s.Chain.Length == 0))) return;
            _released++;
            int pos = s.ChainPosition(out int len);
            Report += "#" + _released + " " + s.DisplayName + " (Glied " + pos + "/" + len + ", Koeff. " + s.Coefficient.ToString("0.000") + (s.ChainBonus ? ", Abschluss" : "") + ") t=" + _t.ToString("0.00") + " · ";
            _shotName = _prefix + "_" + _released; _shotAt = _t + 0.12f;
            if (_released == 3) _pauseUntil = _t + 2.5f;
        }

        void Update()
        {
            if (_done) return;
            _t += Time.deltaTime;
            if (_shotAt >= 0f && _t >= _shotAt) { _shotAt = -1f; ScreenCapture.CaptureScreenshot("Temp/PT_QA/fixlauf_f1/" + _shotName + ".png"); }
            if (_enemy == null || _enemy.IsDead || _t > 20f || _released >= 4) { Finish(); return; }
            if (_player != null) _player.Heal(_player.MaxHealth);
            var to = _enemy.transform.position - _player.transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f) _player.transform.rotation = Quaternion.LookRotation(to);
            if (_released == 3 && _t < _pauseUntil) return;
            if (!_caster.IsCasting) _caster.TryUseSlot(0, true);
        }

        void Finish()
        {
            _done = true;
            if (_caster != null) _caster.CastReleased -= OnReleased;
            Report += "Ende t=" + _t.ToString("0.0") + (_enemy != null && !_enemy.IsDead ? "" : " (Gegner besiegt)");
            if (_enemy != null) Destroy(_enemy.gameObject, 3f);
            Destroy(gameObject, 1f);
        }
    }
}
