using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Außerhalb des Kampfes (6 s ohne Schaden) regeneriert der Spieler schnell.</summary>
    [RequireComponent(typeof(Combatant))]
    public class OutOfCombatRegen : MonoBehaviour
    {
        public float Delay = 6f;
        public float FractionPerSecond = 0.1f;
        Combatant _c; float _since = float.MaxValue / 2f;   // Start: nicht im Kampf

        void Awake()
        {
            _c = GetComponent<Combatant>();
            _c.Damaged += _ => _since = 0f;
            GameEventBus.DamageDealt += OnAnyDamage;
        }

        void OnDestroy() => GameEventBus.DamageDealt -= OnAnyDamage;

        void OnAnyDamage(DamageInfo d) { if (d.Source == _c) _since = 0f; }

        public bool InCombat => _since < Delay;

        void Update()
        {
            _since += Time.deltaTime;
            if (!InCombat && !_c.IsDead && !_c.IsDowned && _c.Health < _c.MaxHealth)
                _c.Heal(_c.MaxHealth * FractionPerSecond * Time.deltaTime);
        }
    }
}
