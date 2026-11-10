using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Jadegeist am Spieler (ab Story-Flag „jadegeist“ oder Admin). Schwebt um den Kopf (Jade-Leuchten), Rettungsprotokoll aus dem Kampfunfähig-Zustand,
    /// Beute-Magnet mit Jadeband, treibt Jade-Geräte an. Werte aus der Jadegeist-Beherrschung (<see cref="JadeBotModel"/>).
    /// </summary>
    public class JadeBotController : MonoBehaviour
    {
        public readonly JadeBotModel Model = new JadeBotModel();
        DownedController _downed; Combatant _self; Transform _orb; float _ang;

        public bool Active => PlayerProfile.Instance != null && (PlayerProfile.Instance.State.Story.HasFlag("jadegeist") || PlayerProfile.Instance.State.IsAdminMarked("mastery:Jadegeist"));

        void Start()
        {
            _self = GetComponent<Combatant>(); _downed = GetComponent<DownedController>();
            if (_downed != null) _downed.StateChanged += OnDowned;
        }

        void OnDestroy() { if (_downed != null) _downed.StateChanged -= OnDowned; }

        void OnDowned()
        {
            if (_downed == null || !_downed.IsDowned || !Active) return;
            float hp = Model.TryRescue(); if (hp <= 0f) return;
            _downed.Rally("Jadegeist: Rettungsprotokoll!");
            if (_self != null) _self.SetHealth(_self.MaxHealth * hp);
            SkillExecutor.VfxPulse(transform.position, 2.5f, new Color(0.4f, 1f, 0.7f));
        }

        void Update()
        {
            var prof = PlayerProfile.Instance;
            Model.Rank = prof != null ? MasteryEffects.JadeBotRank(prof.Mastery) : 0;
            Model.Tick(Time.deltaTime);
            bool on = Active;
            if (on && _orb == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(go.GetComponent<Collider>());
                go.name = "PT_JadeBot"; go.transform.SetParent(transform, false); go.transform.localScale = Vector3.one * 0.28f;
                var r = go.GetComponent<Renderer>(); if (r != null) { var m = new Material(r.sharedMaterial); m.color = new Color(0.35f, 0.95f, 0.7f); if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.3f, 1.4f, 0.9f)); } r.material = m; }
                _orb = go.transform;
            }
            if (_orb != null) { _orb.gameObject.SetActive(on); _ang += Time.deltaTime * 1.6f; _orb.localPosition = new Vector3(Mathf.Cos(_ang) * 0.8f, 2.3f + Mathf.Sin(_ang * 2f) * 0.1f, Mathf.Sin(_ang) * 0.8f); }
            if (on && Model.CollectsLoot && Time.frameCount % 30 == 0)
                foreach (var l in FindObjectsByType<LootBag>())
                    if (l != null && Vector3.Distance(l.transform.position, transform.position) <= Model.LootRange) l.transform.position = Vector3.MoveTowards(l.transform.position, transform.position, 6f);
        }
    }
}
