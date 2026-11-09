using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Beherrschungs-Einblick (GW2: Mastery Insight): mit F untersuchen (2 s). Zählt nicht zum Zonenabschluss,
    /// gibt einen Beherrschungspunkt der Zonen-Region (über <see cref="Found"/>, M18) und etwas EP.
    /// </summary>
    public class MasteryInsight : ZoneObject
    {
        public static event System.Action<ZoneObjectDefinition> Found;

        void Reset() { PromptText = "Untersuchen"; Range = 3f; ChannelTime = 2f; }
        public override string Prompt => "Einblick untersuchen";
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !Done;
        public override void Interact(Combatant player)
        {
            if (Done) return;
            Complete();
            Found?.Invoke(Definition);
        }
    }
}
