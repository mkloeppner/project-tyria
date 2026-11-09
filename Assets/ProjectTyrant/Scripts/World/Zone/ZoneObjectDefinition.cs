using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Karten-/Zonenabschluss-Daten (GW2: Marker-Definition). Weltobjekt und Karte lesen dieselben Daten.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Zone Object")]
    public class ZoneObjectDefinition : ScriptableObject
    {
        public string Id;
        public MapMarkerKind Kind;
        public string DisplayName;
        [TextArea] public string Description;
        public Vector3 Position;
        public float Radius = 10f;
        [Header("Belohnung")]
        public int Experience;
        public int Karma;
        public int Copper;
        public int HeroPoints;
        [Header("Erreichbarkeit (Builder-Prüfung)")]
        [Tooltip("Reittiere, die zum Erreichen nötig sind (aus der Flutfüllung des Builders)")] public Traversal Requires;
        [Header("Herz")]
        public string HeartTask;
        public float KillValue, TaskValue;

        public string RewardText()
        {
            var sb = new System.Text.StringBuilder();
            if (Experience > 0) sb.Append(Experience).Append(" EP");
            if (HeroPoints > 0) sb.Append(sb.Length > 0 ? " · " : "").Append(HeroPoints).Append(" Heldenpunkte");
            if (Karma > 0) sb.Append(sb.Length > 0 ? " · " : "").Append(Karma).Append(" Karma");
            if (Copper > 0) sb.Append(sb.Length > 0 ? " · " : "").Append(WalletModel.Format(Copper));
            return sb.ToString();
        }
    }
}
