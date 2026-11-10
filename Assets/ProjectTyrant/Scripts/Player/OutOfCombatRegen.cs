using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Außerhalb des Kampfes (Balance B1: 5 s ohne Schaden wie GW2, vorher 6 s) regeneriert der Spieler schnell.</summary>
    [RequireComponent(typeof(Combatant))]
    public class OutOfCombatRegen : MonoBehaviour
    {
        public float Delay = Balance.DangerTable.CombatEndDelay;
        public float FractionPerSecond = 0.1f;
        Combatant _c; float _since = float.MaxValue / 2f;   // Start: nicht im Kampf

        void Awake()
        {
            _c = GetComponent<Combatant>();
            Delay = Balance.DangerTable.CombatEndDelay;
            _c.Damaged += _ => _since = 0f;
            GameEventBus.DamageDealt += OnAnyDamage;
        }

        void OnDestroy() => GameEventBus.DamageDealt -= OnAnyDamage;

        void OnAnyDamage(DamageInfo d) { if (d.Source == _c) _since = 0f; }

        public bool InCombat => _since < (Balance.BalanceWorld.Legacy ? 6f : Delay);

        void Update()
        {
            _since += Time.deltaTime;
            if (!InCombat && !_c.IsDead && !_c.IsDowned && _c.Health < _c.MaxHealth)
                _c.Heal(_c.MaxHealth * FractionPerSecond * Time.deltaTime);
        }
    }
}
