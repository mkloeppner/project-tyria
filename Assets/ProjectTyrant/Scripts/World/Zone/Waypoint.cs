using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Wegmarke: Entdeckung durch Annähern (10 m), „[F] Reisen“ öffnet die Weltkarte, umkämpft während Events.</summary>
    public class Waypoint : ZoneObject
    {
        float _check;
        void Reset() { PromptText = "Reisen"; Range = 3.5f; }

        public bool Contested => ZoneRules.IsContested(transform.position, ZoneRules.ActiveEventAreas());
        public override MarkerState MarkerState => !Done ? MarkerState.Undiscovered : Contested ? MarkerState.Contested : MarkerState.Open;

        void Update()
        {
            if ((_check -= Time.deltaTime) > 0f) return;
            _check = 0.25f;
            if (!Done && PlayerWithin(Definition != null ? Definition.Radius : 10f)) Complete();
        }

        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Done;
        public override void Interact(Combatant player) { if (UIController.Instance != null) UIController.Instance.OpenWorldMap(transform.position); }
    }
}
