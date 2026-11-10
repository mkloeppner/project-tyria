using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: Figurenplatz in Silbermund – eine Story-Figur steht hier, sobald ihr Kapitel erreicht ist, und sagt einen Satz zum aktuellen Stand
    /// (Zeile „FP_&lt;sprecher&gt;_K&lt;kapitel&gt;“, sonst „FP_&lt;sprecher&gt;“).
    /// </summary>
    public class FigurePlaza : Interactable
    {
        public string Speaker;
        public int AfterChapter = 1;
        float _t; bool _shown = true;
        void Reset() { PromptText = "Sprechen"; Range = 3f; }
        StoryDirector Dir => GameManager.Instance != null ? GameManager.Instance.Story : null;
        bool Visible => Dir != null && (Dir.State.Chapter > AfterChapter || Dir.State.Finished);
        public override string Prompt => "Sprechen – " + (Dir != null && Dir.Database != null ? Dir.Database.Cast.Name(Speaker) : Speaker);
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Visible && Dir != null && !Dir.Busy;

        public override void Interact(Combatant player)
        {
            var d = Dir; if (d == null || d.Database == null) return;
            string id = "FP_" + Speaker + "_K" + Mathf.Clamp(d.State.Chapter, 1, 10).ToString("00");
            var sc = d.Database.Scene(id) ?? d.Database.Scene("FP_" + Speaker);
            if (sc != null) d.PlayScene(sc.Id, null);
        }

        void Update()
        {
            if ((_t -= Time.deltaTime) > 0f) return; _t = 1f;
            bool v = Visible; if (v == _shown) return; _shown = v;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = v;
        }
    }
}
