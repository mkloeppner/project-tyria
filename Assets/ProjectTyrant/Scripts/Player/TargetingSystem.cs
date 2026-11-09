using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Tab-Targeting: nächster Gegner / Durchschalten, Klick-Auswahl, Esc löscht. Ziel bleibt bei Kamerabewegung erhalten.</summary>
    public class TargetingSystem : MonoBehaviour
    {
        public float TabRange = 35f;
        public float LoseRange = 50f;
        public Combatant Owner;

        public Combatant Current { get; private set; }
        public event Action<Combatant> TargetChanged;

        readonly List<Combatant> _candidates = new List<Combatant>();
        SkillCaster _caster;

        void Awake() { if (Owner == null) Owner = GetComponent<Combatant>(); }

        void Update()
        {
            var input = InputManager.Instance;
            if (Current != null && (Current.IsDead || !Current.isActiveAndEnabled || !Current.Targetable ||
                (Current.transform.position - transform.position).sqrMagnitude > LoseRange * LoseRange))
                SetTarget(null);

            if (input == null) return;
            if (input.TabPressed) CycleTarget();
            if (_caster == null) _caster = GetComponent<SkillCaster>();
            if (_caster != null && (_caster.IsAiming || _caster.AimInputFrame == Time.frameCount)) return;
            if (input.EscapeForWorld && (ThirdPersonCameraRig.Current == null || !ThirdPersonCameraRig.Current.ActionCam)) SetTarget(null);
            if (input.PrimaryClick && (ThirdPersonCameraRig.Current == null || !ThirdPersonCameraRig.Current.ActionCam)) TryClickSelect();
        }

        public void SetTarget(Combatant c)
        {
            if (Current == c) return;
            Current = c;
            TargetChanged?.Invoke(c);
        }

        /// <summary>Wählt das nächste Ziel (sortiert nach Distanz, bevorzugt im Sichtkegel); wiederholtes Tab schaltet durch.</summary>
        public void CycleTarget()
        {
            _candidates.Clear();
            Vector3 fwd = ThirdPersonCameraRig.Current != null ? ThirdPersonCameraRig.Current.FlatForward : transform.forward;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || !c.Targetable || Owner == null || !Owner.IsHostileTo(c)) continue;
                Vector3 to = c.transform.position - transform.position;
                if (to.sqrMagnitude > TabRange * TabRange) continue;
                _candidates.Add(c);
            }
            if (_candidates.Count == 0) { SetTarget(null); return; }
            _candidates.Sort((a, b) => Score(a, fwd).CompareTo(Score(b, fwd)));
            int idx = Current == null ? -1 : _candidates.IndexOf(Current);
            SetTarget(_candidates[(idx + 1) % _candidates.Count]);
        }

        float Score(Combatant c, Vector3 fwd)
        {
            Vector3 to = c.transform.position - transform.position;
            float dist = to.magnitude;
            to.y = 0f;
            float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(fwd, to) : 0f;
            return dist + (angle > 70f ? 30f : angle * 0.05f);
        }

        void TryClickSelect()
        {
            var rig = ThirdPersonCameraRig.Current;
            if (rig == null) return;
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            if (Physics.Raycast(rig.AimRay(), out var hit, 120f, ~0, QueryTriggerInteraction.Collide))
            {
                var c = hit.collider.GetComponentInParent<Combatant>();
                if (c != null && c != Owner && !c.IsDead && c.Targetable) SetTarget(c);
            }
        }
    }
}
