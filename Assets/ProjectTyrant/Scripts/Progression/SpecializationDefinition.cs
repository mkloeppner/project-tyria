using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Spezialisierung: 3 Ränge, je Rang 1 kleine + 3 große Eigenschaften (12 gesamt).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Specialization")]
    public class SpecializationDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Color Color = Color.white;
        public TraitDefinition[] Minors = new TraitDefinition[3];   // je Rang
        public TraitDefinition[] Majors = new TraitDefinition[9];   // Rang r: Index (r−1)·3 … +2

        public TraitDefinition Major(int tier, int choice) => Majors[(tier - 1) * 3 + choice];
    }
}
