using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Truhe am Ende eines Sprungrätsels (GW2-Signatur) – einmal je Charakter, Beute + Erfolgs-Flag.</summary>
    public class JumpPuzzleChest : Interactable
    {
        public string Id;
        public string Title = "Sprungrätsel";
        void Reset() { PromptText = "Truhe öffnen"; Range = 2.5f; }
        bool Opened => GameManager.Instance != null && GameManager.Instance.Story != null && GameManager.Instance.Story.State.HasFlag("jp_" + Id);
        public override string Prompt => Opened ? "Leer" : "Truhe öffnen – " + Title;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !Opened;
        public override void Interact(Combatant player)
        {
            var gm = GameManager.Instance; if (gm == null || gm.Story == null || Opened) return;
            gm.Story.State.SetFlag("jp_" + Id);
            var prof = PlayerProfile.Instance;
            if (prof != null) { prof.GrantReward(200, 1000, 1500, "Sprungrätsel: " + Title); prof.AwardMasteryPoints(1, "Sprungrätsel"); }
            GameEventBus.RaiseBanner(Title, "Sprungrätsel geschafft", new Color(1f, 0.85f, 0.4f));
            GameEventBus.RaiseProfileChanged();
        }
    }
}
