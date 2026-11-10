using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Objekt (z. B. Brennars Grabmal) erscheint erst, wenn ein Story-Flag gesetzt ist.</summary>
    public class StoryFlagVisibility : MonoBehaviour
    {
        public string Flag;
        float _t;
        void Update()
        {
            if ((_t -= Time.deltaTime) > 0f) return; _t = 1f;
            var gm = GameManager.Instance; bool on = gm != null && gm.Story != null && gm.Story.State.HasFlag(Flag);
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.enabled != on) r.enabled = on;
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (c.enabled != on) c.enabled = on;
        }
    }
}
