using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: grünes Story-Portal am Marker. F → Bestätigung → Story-Instanz.</summary>
    public class StoryPortal : Interactable
    {
        public StoryDirector Director;
        public string StepId;

        public static GameObject Spawn(StoryDirector d, StoryStep st)
        {
            var root = new GameObject("StoryPortal_" + st.Id);
            root.transform.position = ZoneAnchors.GroundAt(st.Position + Vector3.up * 2f);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "Ring"; ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = new Vector3(2.6f, 0.04f, 2.6f); ring.transform.localPosition = new Vector3(0, 0.05f, 0);
            Destroy(ring.GetComponent<Collider>());
            var veil = GameObject.CreatePrimitive(PrimitiveType.Quad); veil.name = "Veil"; veil.transform.SetParent(root.transform, false);
            veil.transform.localScale = new Vector3(2.2f, 3f, 1f); veil.transform.localPosition = new Vector3(0, 1.5f, 0);
            Destroy(veil.GetComponent<Collider>());
            var mat = StoryPulse.GlowMaterial(StoryDirector.StoryGreen);
            ring.GetComponent<Renderer>().sharedMaterial = mat; veil.GetComponent<Renderer>().sharedMaterial = mat;
            veil.AddComponent<Billboard>();
            var col = root.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 1.5f; col.center = new Vector3(0, 1f, 0);
            var p = root.AddComponent<StoryPortal>(); p.Director = d; p.StepId = st.Id; p.PromptText = "Story-Instanz betreten – " + st.Title; p.Range = 3.5f; p.Priority = 6;
            StoryPulse.Add(root, 3.2f);
            return root;
        }

        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Director != null && !Director.Busy && !Director.InInstance;

        public override void Interact(Combatant player)
        {
            var st = Director.Step; if (st == null || st.Id != StepId) return;
            var ui = UIController.Instance;
            if (ui != null && ui.Dialog != null) ui.Dialog.Confirm("Persönliche Geschichte", "„" + st.Title + "“ betreten?\n" + st.Description, () => Director.EnterInstance(st), "Betreten");
            else Director.EnterInstance(st);
        }
    }
}
