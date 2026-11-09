using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Markiert prozedural erzeugte Platzhalter. Enthält den Meshy-Prompt für den späteren Austausch.</summary>
    public class PlaceholderMarker : MonoBehaviour
    {
        public string AssetId;
        [TextArea(2, 6)] public string MeshyPrompt;
        public bool ReplacedByMeshy;
    }
}
