using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Aussichtspunkt: F → Kamerafahrt (5,5 s, Esc überspringt) → Abschluss.</summary>
    public class Vista : ZoneObject
    {
        public float OrbitRadius = 16f, OrbitHeight = 7f, Duration = 5.5f;
        void Reset() { PromptText = "Aussicht betrachten"; Range = 3f; }
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !VistaCinematic.Playing;
        public override void Interact(Combatant player) => VistaCinematic.Play(this, () => { if (!Done) Complete(); });
    }
}
