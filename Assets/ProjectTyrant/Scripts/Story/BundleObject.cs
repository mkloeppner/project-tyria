using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: aufhebbares Bündel in der Welt (F). Ersetzt die Fertigkeiten 1–5 (Skill 5 = Fallen lassen), kehrt nach dem Ablegen zurück.</summary>
    public class BundleObject : Interactable
    {
        public string BundleId;
        public float Respawn = 20f;
        GameObject _visual; float _timer; bool _taken;

        void Awake() { _visual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null; if (string.IsNullOrEmpty(PromptText) || PromptText == "Interagieren") PromptText = "Aufheben"; Range = 2.5f; }

        StoryDirector Dir => GameManager.Instance != null ? GameManager.Instance.Story : null;

        public override string Prompt { get { var d = Dir; var b = d != null && d.Database != null ? d.Database.Bundle(BundleId) : null; return "Aufheben – " + (b != null ? b.Name : BundleId); } }
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_taken && Dir != null && !Dir.Bundle.Active;

        public override void Interact(Combatant player)
        {
            var d = Dir; if (d == null || !d.PickBundle(BundleId, this)) return;
            _taken = true; _timer = Respawn; if (_visual != null) _visual.SetActive(false);
        }

        void Update()
        {
            if (!_taken) return;
            var d = Dir; if (d != null && d.Bundle.Active && d.BundleSource == this) return;
            if ((_timer -= Time.deltaTime) > 0f) return;
            _taken = false; if (_visual != null) _visual.SetActive(true);
        }
    }
}
