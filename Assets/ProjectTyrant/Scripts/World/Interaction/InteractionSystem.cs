using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Spieler-Komponente: wählt das nächste Interaktionsziel, zeigt den Prompt, führt Kanäle aus.</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public Combatant Player;
        public Interactable Current { get; private set; }
        public Interactable Channeling { get; private set; }
        public float ChannelElapsed { get; private set; }
        public float ChannelProgress => Channeling == null || Channeling.ChannelTime <= 0f ? 0f : Mathf.Clamp01(ChannelElapsed / Channeling.ChannelTime);
        public bool IsChanneling => Channeling != null;
        public event System.Action<Interactable> CurrentChanged;

        Vector3 _channelStartPos;

        void Awake()
        {
            if (Player == null) Player = GetComponent<Combatant>();
            Player.Damaged += OnDamaged;
        }

        void OnDestroy() { if (Player != null) Player.Damaged -= OnDamaged; }

        void OnDamaged(DamageInfo d)
        {
            if (Channeling != null && d.Amount > 0f && !d.IsCondition) Interrupt("Unterbrochen – Schaden erlitten");
        }

        public static Interactable FindBest(Vector3 pos, Vector3 forward, Combatant player)
        {
            Interactable best = null; float bestScore = float.MaxValue;
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                var it = Interactable.All[i];
                if (it == null || !it.CanInteract(player)) continue;
                Vector3 d = it.InteractPosition - pos;
                float dist = new Vector2(d.x, d.z).magnitude;
                if (dist > it.Range || Mathf.Abs(d.y) > 3.5f) continue;
                float score = dist - it.Priority * 10f;
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }

        void Update()
        {
            if (Player == null) return;
            var input = InputManager.Instance;
            if (Channeling != null)
            {
                if (Player.IsDead || Player.IsDowned || (transform.position - _channelStartPos).sqrMagnitude > 0.6f || !Channeling.CanInteract(Player)) { Interrupt("Unterbrochen"); return; }
                ChannelElapsed += Time.deltaTime;
                if (ChannelElapsed >= Channeling.ChannelTime)
                {
                    var it = Channeling; Channeling = null; ChannelElapsed = 0f;
                    it.Interact(Player);
                }
                return;
            }
            var best = Player.IsDead || Player.IsDowned ? null : FindBest(transform.position, transform.forward, Player);
            if (best != Current) { Current = best; CurrentChanged?.Invoke(best); }
            if (input != null && input.InteractPressed && Current != null) Begin(Current);
        }

        public void Begin(Interactable it)
        {
            if (it == null || !it.CanInteract(Player)) return;
            if (it.ChannelTime > 0f)
            {
                Channeling = it; ChannelElapsed = 0f; _channelStartPos = transform.position;
                it.OnChannelStarted(Player);
            }
            else it.Interact(Player);
        }

        public void Interrupt(string msg)
        {
            if (Channeling == null) return;
            var it = Channeling; Channeling = null; ChannelElapsed = 0f;
            it.OnChannelInterrupted(Player);
            GameEventBus.Notify(msg, new Color(1f, 0.6f, 0.4f));
        }
    }
}
