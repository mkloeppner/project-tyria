using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Reittier als Daten: Werte, Erwerb (Herz + Preis), Aussehen.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Mount")]
    public class MountDefinition : ScriptableObject
    {
        public MountKind Kind;
        public string DisplayName;
        [TextArea] public string Description;
        public string AbilityText, EngageText;
        public MountStats Stats;
        [Header("Erwerb")]
        public long PriceCopper;
        public string RequiredHeart;
        public string VendorZone;
        public string Icon;
        public GameObject Prefab;
        public float SeatHeight = 1.2f;
    }
}
