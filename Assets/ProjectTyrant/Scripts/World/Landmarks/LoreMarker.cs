using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: Lore-Tafel – 2–3 Sätze, zählt für „Chronist der Zone“.</summary>
    public class LoreMarker : Interactable
    {
        public string Id, Title, Figure;
        [TextArea] public string Text;
        void Reset() { PromptText = "Tafel lesen"; Range = 3f; }
        static ProfileState State => PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null;
        public bool Read => State != null && State.Flags.Contains("lore:" + Id);
        public override string Prompt => (Read ? "Erneut lesen – " : "Tafel lesen – ") + Title;
        public override void Interact(Combatant player)
        {
            var ui = UIController.Instance;
            if (ui != null && ui.Dialog != null) ui.Dialog.Confirm(Title, Text + (string.IsNullOrEmpty(Figure) ? "" : "\n\n<i>– verknüpft mit " + Figure + "</i>"), null, "Schließen");
            if (Read || State == null) return;
            State.Flags.Add("lore:" + Id);
            State.Achievements.Add("w1_lore");
            PlayerProfile.Instance.GrantReward(150, 50, 0, "Lore: " + Title);
            GameEventBus.Notify("Chronik ergänzt: " + Title, new Color(0.85f, 0.8f, 0.6f));
            GameEventBus.RaiseProfileChanged();
        }
    }
}
