using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Basis aller Interaktionen per F (Aufheben, Kommunizieren, Sprechen, Reisen, Reparieren …).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [Tooltip("Text hinter „[F]“, z. B. Kommunizieren, Aufheben, Sprechen.")]
        public string PromptText = "Interagieren";
        public float Range = 2.5f;
        [Tooltip(">0: Kanal mit Fortschrittsbalken (Schaden und Bewegung unterbrechen).")]
        public float ChannelTime = 0f;
        public int Priority = 0;

        public virtual Vector3 InteractPosition => transform.position;
        public virtual string Prompt => PromptText;
        public virtual bool CanInteract(Combatant player) => isActiveAndEnabled;
        public abstract void Interact(Combatant player);
        public virtual void OnChannelStarted(Combatant player) { }
        public virtual void OnChannelInterrupted(Combatant player) { }

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() => All.Remove(this);
    }
}
