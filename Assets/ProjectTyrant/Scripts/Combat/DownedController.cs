using System;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Downed State: bei 0 HP kniet der Spieler, eigene Downed-HP, 30 s Bleed-out.
    /// Rally, sobald ein Gegner stirbt, den der Spieler getötet oder beschädigt hat.
    /// Bandagieren (Downed-Skill 4) bis volle Downed-HP → ebenfalls Wiederaufstehen.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class DownedController : MonoBehaviour, IDownedHandler
    {
        public float DownedMaxHealth = 4000f;
        public float BleedOutTime = 30f;
        public float RallyHealthFraction = 0.3f;
        [Tooltip("Downed-HP als Anteil der max. Lebenspunkte (0 = fester Wert DownedMaxHealth).")]
        public float DownedHealthShare = 0f;

        public Combatant Combatant { get; private set; }
        public bool IsDowned { get; private set; }
        public float DownedHealth { get; private set; }
        public float TimeRemaining { get; private set; }
        public float DownedHealthFraction => DownedMaxHealth <= 0 ? 0 : DownedHealth / DownedMaxHealth;

        public event Action StateChanged;

        void Awake() => Init();

        public void Init()
        {
            if (Combatant != null) return;
            Combatant = GetComponent<Combatant>();
            Combatant.DownedHandler = this;
            GameEventBus.EntityKilled += OnEntityKilled;
        }

        void OnDestroy() => GameEventBus.EntityKilled -= OnEntityKilled;

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (!IsDowned) return;
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f) Defeat();
        }

        public bool TryEnterDowned(Combatant killer)
        {
            if (IsDowned) return true;
            IsDowned = true;
            if (DownedHealthShare > 0f) DownedMaxHealth = Mathf.Max(50f, Combatant.MaxHealth * DownedHealthShare);
            DownedHealth = DownedMaxHealth;
            TimeRemaining = BleedOutTime;
            Combatant.Effects.ClearConditions();
            GameEventBus.RaisePlayerDowned(Combatant);
            GameEventBus.Notify("Am Boden! Besiege einen Gegner, um wieder aufzustehen.", new Color(1f, 0.55f, 0.35f));
            StateChanged?.Invoke();
            return true;
        }

        public void TakeDownedDamage(float amount, Combatant source)
        {
            if (!IsDowned) return;
            DownedHealth -= amount;
            if (DownedHealth <= 0f) Defeat();
            StateChanged?.Invoke();
        }

        /// <summary>Selbstheilung im Downed State (Downed-Skill 3).</summary>
        public void HealDowned(float amount)
        {
            if (!IsDowned) return;
            DownedHealth = Mathf.Min(DownedMaxHealth, DownedHealth + amount);
            StateChanged?.Invoke();
        }

        /// <summary>Bandagieren (Downed-Skill 4): heilt Downed-HP; bei voller Leiste steht der Spieler auf.</summary>
        public void Bandage(float amount)
        {
            if (!IsDowned) return;
            HealDowned(amount);
            if (DownedHealth >= DownedMaxHealth - 0.01f) Rally("Bandagiert – wieder auf den Beinen!");
        }

        void OnEntityKilled(Combatant victim, Combatant killer)
        {
            if (!IsDowned || victim == null || victim == Combatant) return;
            if (!Combatant.IsHostileTo(victim)) return;
            if (killer == Combatant || victim.Attackers.Contains(Combatant)) Rally("Rally! Wieder im Kampf!");
        }

        public void Rally(string message = "Rally!")
        {
            if (!IsDowned) return;
            IsDowned = false;
            Combatant.SetHealth(Combatant.MaxHealth * RallyHealthFraction);
            GameEventBus.RaisePlayerRallied(Combatant);
            GameEventBus.Notify(message, new Color(0.5f, 1f, 0.5f));
            StateChanged?.Invoke();
        }

        void Defeat()
        {
            if (!IsDowned) return;
            IsDowned = false;
            DownedHealth = 0f;
            Combatant.Die(null);
            GameEventBus.RaisePlayerDefeated(Combatant);
            StateChanged?.Invoke();
        }

        public void ResetState()
        {
            IsDowned = false;
            DownedHealth = DownedMaxHealth;
            TimeRemaining = BleedOutTime;
            StateChanged?.Invoke();
        }
    }
}
