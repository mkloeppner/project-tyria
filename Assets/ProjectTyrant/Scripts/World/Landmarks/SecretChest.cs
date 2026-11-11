using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: Geheim-Truhe – außerhalb aller Marker, täglich einmal, Inhalt wie Sprungrätsel-Truhe; zeigt danach das nächste Ziel.</summary>
    public class SecretChest : Interactable
    {
        public string Id, Title = "Geheimnis";
        public Vector3 NextGoal; public string NextGoalName;
        void Reset() { PromptText = "Truhe öffnen"; Range = 2.5f; }
        static ProfileState State => PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null;
        public bool OpenedToday => State != null && State.Flags.Contains(SecretRules.DailyFlag(Id, System.DateTime.Today));
        public bool EverFound => State != null && State.Flags.Contains("secret:" + Id);
        public override string Prompt => OpenedToday ? "Leer (morgen wieder)" : "Truhe öffnen – " + Title;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !OpenedToday;
        public override void Interact(Combatant player)
        {
            var st = State; if (st == null || OpenedToday) return;
            st.Flags.Add(SecretRules.DailyFlag(Id, System.DateTime.Today));
            bool first = !EverFound; if (first) st.Flags.Add("secret:" + Id);
            var prof = PlayerProfile.Instance;
            prof.GrantReward(200, 1000, 1500, "Geheimnis: " + Title);
            if (first) { prof.AwardMasteryPoints(1, "Geheimnis"); st.Achievements.Add("w1_secrets"); }
            GameEventBus.RaiseBanner(Title, first ? "Geheimnis entdeckt" : "Truhe erneut geöffnet", new Color(1f, 0.85f, 0.4f));
            if (NextGoal != Vector3.zero) NextGoalBeam.Show(NextGoal, NextGoalName);
            GameEventBus.RaiseProfileChanged();
        }
    }
}
