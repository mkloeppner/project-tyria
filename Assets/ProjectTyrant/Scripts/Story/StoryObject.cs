using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Story-Objekt (Gefangene befreien, Siegel berühren, Laterne aufnehmen), wiederzubelebende Gefährtin und Eskorte.</summary>
    public class StoryObject : Interactable
    {
        public StoryDirector Director;
        public ObjectiveKind Kind;
        public string Target;
        bool _used;
        Transform _follow; Vector3 _goal; bool _escort;

        public static GameObject Spawn(StoryDirector d, StoryStep st, StoryObjective o, Vector3 at)
        {
            var root = new GameObject("StoryObject_" + o.Target);
            root.transform.position = ZoneAnchors.GroundAt(at + Vector3.up * 3f);
            var fig = d.Database.Figure(o.Target);
            if (fig != null) { var v = Instantiate(fig, root.transform); v.transform.localPosition = Vector3.zero; foreach (var c in v.GetComponentsInChildren<Collider>()) c.enabled = false; }
            else
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pillar.transform.SetParent(root.transform, false);
                pillar.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f); pillar.transform.localPosition = new Vector3(0, 0.7f, 0);
                pillar.GetComponent<Renderer>().sharedMaterial = StoryPulse.GlowMaterial(StoryDirector.StoryGreen);
            }
            var so = root.AddComponent<StoryObject>(); so.Director = d; so.Kind = o.Kind; so.Target = o.Target;
            so.PromptText = string.IsNullOrEmpty(o.Label) ? "Benutzen" : o.Label; so.ChannelTime = o.Kind == ObjectiveKind.Bundle ? 0f : 1.5f; so.Range = 3f; so.Priority = 4;
            StoryPulse.Add(root, 2f);
            return root;
        }

        public static GameObject SpawnDowned(StoryDirector d, StoryStep st, StoryObjective o, Vector3 at)
        {
            var root = Spawn(d, st, o, at);
            var so = root.GetComponent<StoryObject>(); so.PromptText = "Wiederbeleben – " + d.Database.Cast.Name(o.Target); so.ChannelTime = 3f;
            var drv = root.GetComponentInChildren<AnimDriver>(); if (drv != null && drv.Anim != null) drv.Anim.PlayFull("Knockdown", 0.2f, 1f, true);
            return root;
        }

        public static GameObject SpawnEscort(StoryDirector d, StoryStep st, StoryObjective o, Vector3 at)
        {
            var root = Spawn(d, st, o, st.Position);
            var so = root.GetComponent<StoryObject>(); so._escort = true; so._goal = at; so.PromptText = "Begleiten"; so.ChannelTime = 0f;
            return root;
        }

        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_used && Director != null && !Director.Busy && !(_escort && _follow != null);

        public override void Interact(Combatant player)
        {
            if (_escort) { _follow = player.transform; GameEventBus.Notify("Bring die Leute in Sicherheit", StoryDirector.StoryGreen); return; }
            _used = true;
            Director.Report(Kind, Target);
            if (Kind == ObjectiveKind.Revive) { var drv = GetComponentInChildren<AnimDriver>(); if (drv != null && drv.Anim != null) drv.Anim.PlayFull("Getup", 0.2f); Destroy(gameObject, 2.5f); }
            else Destroy(gameObject, 0.3f);
        }

        void Update()
        {
            if (!_escort || _follow == null || _used) return;
            var to = _follow.position - transform.position; to.y = 0f;
            if (to.magnitude > 2.5f) { var step = to.normalized * Mathf.Min(to.magnitude - 2.5f, 4.5f * Time.deltaTime); transform.position = ZoneAnchors.GroundAt(transform.position + step + Vector3.up * 2f); transform.rotation = Quaternion.LookRotation(to); }
            var drv = GetComponentInChildren<AnimDriver>();
            var g = _goal - transform.position; g.y = 0f;
            if (g.magnitude < 6f) { _used = true; Director.Report(ObjectiveKind.Escort, Target); GameEventBus.Notify("In Sicherheit", StoryDirector.StoryGreen); Destroy(gameObject, 2f); }
        }
    }
}
