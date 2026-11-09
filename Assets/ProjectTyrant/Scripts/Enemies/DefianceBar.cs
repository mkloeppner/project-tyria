using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Trotzleiste am Champion/Boss: blockiert Kontrolle, senkt die Leiste, beim Brechen Betäubung + 50 % Schaden.</summary>
    [RequireComponent(typeof(Combatant))]
    public class DefianceBar : MonoBehaviour
    {
        public float Max = 600f;
        public DefianceModel Model { get; private set; }
        Combatant _self;
        System.Func<DamageInfo, float> _prevMultiplier;
        static readonly Dictionary<Combatant, DefianceBar> _map = new Dictionary<Combatant, DefianceBar>();
        public static DefianceBar Of(Combatant c) => c != null && _map.TryGetValue(c, out var d) ? d : null;
        public static event System.Action<Combatant> Broken;
        bool _applyingStun;

        void Awake() { _self = GetComponent<Combatant>(); Model = new DefianceModel(Max); }

        void OnEnable()
        {
            _map[_self] = this;
            _self.Effects.ApplyFilter = Filter;
            _prevMultiplier = _self.IncomingMultiplier;
            _self.IncomingMultiplier = Multiplier;
        }

        void OnDisable()
        {
            _map.Remove(_self);
            if (_self.Effects.ApplyFilter == (System.Func<EffectType, int, float, bool>)Filter) _self.Effects.ApplyFilter = null;
            _self.IncomingMultiplier = _prevMultiplier;
        }

        public void SetMax(float max) { Max = max; Model = new DefianceModel(max); }

        float Multiplier(DamageInfo info) => (_prevMultiplier != null ? _prevMultiplier(info) : 1f) * Model.DamageTakenMultiplier;

        bool Filter(EffectType t, int stacks, float duration)
        {
            if (_applyingStun) return true;
            bool blocked = Model.Hit(t, duration, out bool broke);
            if (broke)
            {
                _applyingStun = true;
                _self.Effects.Apply(EffectType.Stun, 1, Model.BreakDuration);
                _applyingStun = false;
                var brain = GetComponent<NpcBrain>(); if (brain != null) brain.InterruptAttack();
                SkillExecutor.VfxPulse(transform.position, 4f, new Color(0.4f, 0.85f, 1f));
                GameEventBus.RaiseBanner("Trotzleiste gebrochen!", _self.DisplayName + " ist 5 s wehrlos (+50 % Schaden)", new Color(0.45f, 0.85f, 1f));
                Broken?.Invoke(_self);
            }
            return !blocked;
        }

        void Update() => Model.Tick(Time.deltaTime);
    }
}
