using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Story-Figur am grünen Marker (Gespräch-Schritt). F → Szene → Ziel „Sprich mit …“.</summary>
    public class StoryNpc : Interactable
    {
        public StoryDirector Director;
        public string StepId;
        public string Speaker;
        bool _busy;

        public static GameObject Spawn(StoryDirector d, StoryStep st, bool ambient = false)
        {
            var root = new GameObject("StoryNpc_" + st.Npc);
            root.transform.position = ZoneAnchors.GroundAt(st.Position + Vector3.up * 2f);
            var fig = d.Database.Figure(st.Npc);
            if (fig != null) { var v = Instantiate(fig, root.transform); v.transform.localPosition = Vector3.zero; foreach (var c in v.GetComponentsInChildren<Collider>()) c.enabled = false; }
            var col = root.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 0.35f;
            var n = root.AddComponent<StoryNpc>(); n.Director = d; n.StepId = st.Id; n.Speaker = st.Npc; n.PromptText = "Sprechen – " + d.Database.Cast.Name(st.Npc); n.Range = 3.2f; n.Priority = 5;
            StoryPulse.Add(root, 2.2f);
            return root;
        }

        void Update()
        {
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            if (p == null) return;
            var d = p.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 100f && d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
        }

        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_busy && Director != null && !Director.Busy;

        public override void Interact(Combatant player)
        {
            var st = Director.Step; if (st == null || st.Id != StepId) return;
            _busy = true;
            if (!string.IsNullOrEmpty(st.SceneStart)) Director.PlayScene(st.SceneStart, Done); else Done();
        }

        void Done() { _busy = false; Director.Report(ObjectiveKind.Talk, Speaker); }
    }
}
