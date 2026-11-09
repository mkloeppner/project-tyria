using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Sehenswürdigkeit: Entdeckung durch Annähern.</summary>
    public class PointOfInterest : ZoneObject
    {
        float _check;
        void Update()
        {
            if ((_check -= Time.deltaTime) > 0f) return;
            _check = 0.3f;
            if (!Done && PlayerWithin(Definition != null ? Definition.Radius : 12f)) Complete();
        }
        public override bool CanInteract(Combatant player) => false;
        public override void Interact(Combatant player) { }
    }
}
