using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M22: Kampf-Lesbarkeit im HUD – Aktivierungsbalken des Gegners (Ausholen), rotes Aufblinken + Wackeln der Taste bei
    /// Fehlversuch, Markierung des eingereihten Skills, Kanal-Balken, Kampftext für Unterbrechung, Combos und Zustandsfarben.
    /// </summary>
    public partial class UIController
    {
        Bar _enemyCast;
        SkillCaster _boundCaster;
        public static string LastFailReason { get; private set; }

        public static Color ConditionTextColor(EffectType t)
        {
            switch (t)
            {
                case EffectType.Burning: return new Color(1f, 0.55f, 0.15f);
                case EffectType.Bleeding: return new Color(0.95f, 0.25f, 0.25f);
                case EffectType.Poison: return new Color(0.55f, 0.95f, 0.3f);
                case EffectType.Torment: return new Color(0.75f, 0.45f, 0.95f);
                case EffectType.Confusion: return new Color(0.95f, 0.45f, 0.85f);
                default: return new Color(1f, 0.6f, 0.2f);
            }
        }

        void UpdateCombatUi()
        {
            if (_caster != _boundCaster)
            {
                if (_boundCaster != null) _boundCaster.CastFailed -= OnCastFailed;
                _boundCaster = _caster;
                if (_boundCaster != null) _boundCaster.CastFailed += OnCastFailed;
            }
            if (_caster == null) return;
            var queued = _caster.QueuedSkill;
            foreach (var slot in _slots)
            {
                if (slot == null || slot.Root == null) continue;
                var rt = (RectTransform)slot.Root.transform;
                if (slot.BasePos == Vector2.zero) slot.BasePos = rt.anchoredPosition;
                bool q = queued != null && slot.Skill == queued;
                if (slot.FailTimer > 0f)
                {
                    slot.FailTimer -= Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(slot.FailTimer / 0.35f);
                    rt.anchoredPosition = slot.BasePos + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 5f * k, 0f);
                    slot.Frame.color = Color.Lerp(new Color(0, 0, 0, 0.75f), new Color(1f, 0.15f, 0.1f, 1f), k);
                    slot.LastCasting = false; slot.LastQueued = false;
                    if (slot.FailTimer <= 0f) rt.anchoredPosition = slot.BasePos;
                }
                else if (q != slot.LastQueued)
                {
                    slot.LastQueued = q;
                    slot.Frame.color = q ? new Color(0.35f, 0.85f, 1f, 1f) : slot.LastCasting ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0, 0, 0, 0.75f);
                }
            }
            // Kanal-Balken: mehrteilige Aktivierung (Wirbel, Verbinden) über der Skill-Leiste
            if (_caster.ChannelSkill != null && !_caster.IsCasting)
            {
                _cast.Root.SetActive(true);
                SetBar(_cast, _caster.ChannelProgress, _caster.ChannelSkill.DisplayName + " (Kanal)");
            }
        }

        void OnCastFailed(SkillDefinition s, string reason)
        {
            LastFailReason = reason;
            foreach (var slot in _slots) if (slot != null && slot.Skill == s) slot.FailTimer = 0.35f;
            GameAudio.Ui("Error");   // Lauf 5 / M24: kurzer Fehlton
        }

        /// <summary>Aktivierungsbalken des Gegners unter Trotzleiste (GW2 zeigt Gegner-Kanäle).</summary>
        void UpdateEnemyCast(Combatant t)
        {
            var brain = t != null ? t.GetComponent<NpcBrain>() : null;
            bool show = brain != null && brain.IsWindingUp;
            if (_enemyCast == null)
            {
                if (!show) return;
                var tf = _targetHp.Root.transform.parent as RectTransform;
                _enemyCast = MakeBar("TargetCast", tf, new Vector2(0.5f, 1f), new Vector2(0, -88), new Vector2(300, 14), new Color(1f, 0.55f, 0.2f), 11);
            }
            if (_enemyCast.Root.activeSelf != show) _enemyCast.Root.SetActive(show);
            if (show) SetBar(_enemyCast, brain.WindupProgress, "Holt aus …");
        }

        void OnInterruptedText(Combatant c, SkillDefinition s)
        {
            if (c == null || Player == null) return;
            bool mine = c == Player;
            bool theirs = _targeting != null && _targeting.Current == c;
            if (!mine && !theirs && (c.transform.position - Player.transform.position).sqrMagnitude > 900f) return;
            SpawnFloating(c.AimPoint + Vector3.up * 0.9f, "Unterbrochen", mine ? new Color(1f, 0.45f, 0.4f) : new Color(0.6f, 0.85f, 1f), 26);
        }

        void OnComboText(ComboEntry e, Vector3 p)
        {
            if (e == null || Player == null || (p - Player.transform.position).sqrMagnitude > 1600f) return;
            SpawnFloating(p + Vector3.up * 2f, "Combo: " + e.ResultName, new Color(0.55f, 0.95f, 1f), 24);
        }
    }
}
