using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M28: Story-Befehle im Debug-Panel (F9) – Kapitel/Schritt setzen, Schritt abschließen, Szene abspielen, Flags, Herkunft/Pakt, Stimm-Test, Späher/Herzen.</summary>
    public static class StoryDebugCommands
    {
        static StoryDirector D => GameManager.Instance != null ? GameManager.Instance.Story : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Story"));
            DebugCommands.Extra.Add(("Story: Kapitel (K S)", a =>
            {
                var d = D; if (d == null) return "Kein Story-Direktor";
                var p = (a ?? "").Split(' '); int k = 1, s = 0;
                if (p.Length > 0) int.TryParse(p[0], out k); if (p.Length > 1) int.TryParse(p[1], out s);
                return d.DebugSetChapter(Mathf.Max(1, k), Mathf.Max(0, s));
            }));
            DebugCommands.Extra.Add(("Story: Schritt abschließen", a => D != null ? D.DebugCompleteStep() : "Kein Story-Direktor"));
            DebugCommands.Extra.Add(("Story: Szene abspielen", a =>
            {
                var d = D; if (d == null || string.IsNullOrEmpty(a)) return "Szenen-Id angeben (z. B. K01_S01_Intro)";
                d.PlayScene(a.Trim(), null); return "Szene " + a;
            }));
            DebugCommands.Extra.Add(("Story: Flag setzen", a =>
            {
                var d = D; if (d == null) return "-"; if (string.IsNullOrEmpty(a)) return "Flags: " + string.Join(", ", d.State.Flags);
                d.State.SetFlag(a.Trim()); return "Flag " + a;
            }));
            DebugCommands.Extra.Add(("Story: Herkunft/Pakt", a =>
            {
                var d = D; if (d == null) return "-"; var s = d.State; string v = (a ?? "").Trim();
                if (System.Enum.TryParse(v, true, out Origin o) && o != Origin.None) s.OriginE = o;
                else if (System.Enum.TryParse(v, true, out Pact p) && p != Pact.None) s.PactE = p;
                else if (System.Enum.TryParse(v, true, out Trait t) && t != Trait.None) s.TraitE = t;
                return "Herkunft " + s.OriginE + " · Wesen " + s.TraitE + " · Leitstern " + s.LodestarE + " · Pakt " + s.PactE;
            }));
            DebugCommands.Extra.Add(("Story: Stimm-Test", a =>
            {
                var gm = GameManager.Instance; if (gm == null || gm.Dialogue == null || D == null) return "-";
                var line = D.Database != null ? D.Database.Text.Get(string.IsNullOrEmpty(a) ? "K01_S01_L01" : a.Trim()) : null;
                if (line == null) return "Zeile unbekannt";
                var sc = ScriptableObject.CreateInstance<DialogueScript>(); sc.Id = "DebugVoice"; sc.Form = SceneForm.Bubble; sc.Entries.Add(new DialogueEntry { Line = line.Id });
                gm.Dialogue.Play(sc, null); return line.Speaker + ": " + line.Text + (line.Voice != null ? " (Stimme)" : " (Silben)");
            }));
            DebugCommands.Extra.Add(("Story: Galerie (3 Formen)", a =>
            {
                var d = D; if (d == null || d.Database == null) return "-";
                string conv = null, cine = null, bub = null;
                foreach (var sc in d.Database.Scenes) { if (sc.Form == SceneForm.Conversation && conv == null) conv = sc.Id; if (sc.Form == SceneForm.Cinematic && cine == null) cine = sc.Id; if (sc.Form == SceneForm.Bubble && bub == null) bub = sc.Id; }
                d.PlayScene(conv, () => d.PlayScene(cine, () => d.PlayScene(bub, null)));
                return "Galerie: " + conv + " → " + cine + " → " + bub;
            }));
            DebugCommands.Extra.Add(("Story: Späher/Herzen", a =>
            {
                var d = D; if (d == null) return "-";
                return "Späher " + d.State.Scouts.Count + " · Briefe " + d.State.Mail.Count + " · Kapitel " + d.State.Chapter + "/" + d.State.Step + (d.Instance.Active ? " · in Instanz" : "");
            }));
        }
    }
}
