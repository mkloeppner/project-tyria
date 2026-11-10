using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Gefährte in Story-Instanzen – folgt dem Helden, greift dessen Ziel an (Schaden auf das Konto des Helden), stirbt nie außerhalb der Erzählung.</summary>
    public class StoryCompanion : MonoBehaviour
    {
        public string Speaker;
        float _cd = 1f;
        AnimDriver _drv;

        public static GameObject Spawn(StoryDirector d, string speaker, Vector3 at)
        {
            var root = new GameObject("Companion_" + speaker);
            root.transform.position = ZoneAnchors.GroundAt(at + Vector3.up * 2f);
            var fig = d.Database.Figure(speaker);
            if (fig != null) { var v = Instantiate(fig, root.transform); v.transform.localPosition = Vector3.zero; foreach (var c in v.GetComponentsInChildren<Collider>()) c.enabled = false; }
            var sc = root.AddComponent<StoryCompanion>(); sc.Speaker = speaker; sc._drv = root.GetComponentInChildren<AnimDriver>();
            GameEventBus.Notify(d.Database.Cast.Name(speaker) + " begleitet dich", StoryDirector.StoryGreen);
            return root;
        }

        void Update()
        {
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null; if (p == null) return;
            var to = p.transform.position - p.transform.forward * 1.5f + p.transform.right * 2f - transform.position; to.y = 0f;
            if (to.magnitude > 1f) { var step = to.normalized * Mathf.Min(to.magnitude, (to.magnitude > 8f ? 9f : 5f) * Time.deltaTime); transform.position = ZoneAnchors.GroundAt(transform.position + step + Vector3.up * 2f); transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 6f); }
            if (to.magnitude > 30f) transform.position = ZoneAnchors.GroundAt(p.transform.position + Vector3.up * 2f);
            if ((_cd -= Time.deltaTime) > 0f) return;
            var tg = p.GetComponent<TargetingSystem>(); var t = tg != null ? tg.Current : null;
            if (t == null || t.IsDead || t.Team != Team.Enemy || Vector3.Distance(t.transform.position, transform.position) > 18f) { _cd = 0.5f; return; }
            _cd = 2.2f;
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(t.transform.position - transform.position, Vector3.up));
            if (_drv != null && _drv.Anim != null) _drv.Anim.PlayUpper("Cast1H", 0.1f);
            DamageCalculator.DealDirect(p, t, 0.35f);
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Impact, VfxPalette.Natur, VfxSize.S), t.transform.position + Vector3.up, Quaternion.identity, null, 0.6f);
        }
    }
}
