using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Händler-NPC: „[F] Sprechen“ öffnet das Händlerfenster.</summary>
    public class VendorNpc : Interactable
    {
        public VendorDefinition Vendor;
        [System.NonSerialized] public VendorModel Model = new VendorModel();
        [Tooltip("Optional: nur aktiv, wenn diese Bedingung erfüllt ist (z. B. Herz erfüllt).")]
        public string RequiredCompletion;
        public bool Available = true;

        void Reset() { PromptText = "Sprechen"; Range = 3f; }

        public override bool CanInteract(Combatant player)
        {
            if (!Available || !isActiveAndEnabled) return false;
            if (!string.IsNullOrEmpty(RequiredCompletion))
                return PlayerProfile.Instance != null && PlayerProfile.Instance.State.Completed.Contains(RequiredCompletion);
            return true;
        }

        public override string Prompt => "Sprechen – " + (Vendor != null ? Vendor.DisplayName : "Händler");

        public override void Interact(Combatant player)
        {
            var ui = UIController.Instance;
            if (ui != null) ui.OpenVendor(this);
        }
    }
}
