using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Beherrschungslinie als Daten (Ränge mit EP-/Punktkosten und Wirkungstext).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Mastery Line")]
    public class MasteryLineDefinition : ScriptableObject
    {
        public MasteryLine Line;
        public string DisplayName;
        [TextArea] public string Description;
        public string Icon;
        public Color Color = Color.white;
        public MasteryRankCost[] Ranks = new MasteryRankCost[0];
    }
}
