using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: Ziel für eine Bündel-Fertigkeit in einem Herz (Glutnest mit dem Eimer löschen, Lawinenwall mit Sandsäcken, Treibgut mit der Harpune).
    /// Fertigkeit des passenden Bündels in ≤ 5 m → Herz-Beitrag „Bündel“, Ziel wechselt in den erledigten Zustand und kehrt nach einer Weile zurück.
    /// </summary>
    public class BundleTarget : MonoBehaviour
    {
        public string BundleId;
        public Heart Heart;
        public float Value = 0.12f;
        public string Label = "Bündel";
        public float Range = 5f, Respawn = 40f;
        public GameObject Open, Done;     // Zustand vorher / nachher (z. B. Glut / Rauch)
        float _timer; bool _done;

        void OnEnable() { GameEventBus.SkillCast += OnCast; Show(false); }
        void OnDisable() { GameEventBus.SkillCast -= OnCast; }

        void Show(bool done) { _done = done; if (Open != null) Open.SetActive(!done); if (Done != null) Done.SetActive(done); }

        void OnCast(Combatant c, SkillDefinition s)
        {
            var gm = GameManager.Instance; var prof = PlayerProfile.Instance;
            if (_done || gm == null || prof == null || c != gm.Player || s == null || string.IsNullOrEmpty(s.Id) || !s.Id.StartsWith("Bundle_") || s.Id == "Bundle_Drop") return;
            var d = gm.Story; if (d == null || d.Bundle.Current != BundleId) return;
            if ((c.transform.position - transform.position).sqrMagnitude > Range * Range) return;
            if (Heart == null || Heart.Done || Heart.Definition == null) return;
            prof.AddHeartWay(Heart.Definition, HeartWay.Bundle, Value, Label);
            Show(true); _timer = Respawn;
        }

        void Update()
        {
            if (!_done) return;
            if ((_timer -= Time.deltaTime) <= 0f) Show(false);
        }
    }
}
