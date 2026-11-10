using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Fix-Lauf F1: markiert ein Kind einer VFX-Komposition als Flächen- oder Effekt-Ebene (Komponente statt Unity-Tag, weil Tags außerhalb von Assets liegen). Eigene Datei, damit Unity die Komponente im Prefab serialisiert.</summary>
    public class VfxAreaLayer : MonoBehaviour { public AreaLayer Layer; }
}
