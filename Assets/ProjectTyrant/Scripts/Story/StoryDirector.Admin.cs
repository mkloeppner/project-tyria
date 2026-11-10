using System.Collections;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 10: Admin-Schalter für die Geschichte. <see cref="Active"/> gilt nur, solange ein Admin-Ablauf läuft;
    /// dann bestimmen <see cref="PlayScenes"/> (Szenen abspielen oder überspringen) und <see cref="AutoChoose"/> (erste Antwort wählen),
    /// und das Belohnungsfenster einer Instanz schließt von selbst.
    /// </summary>
    public static class StoryAdmin
    {
        public static bool Active;
        public static bool PlayScenes = true, AutoChoose = true, Travel = true;
    }

    /// <summary>
    /// Lauf 10: Admin-Befehle der Geschichte – alle über den echten Abschluss-Ablauf (<see cref="FinishStep"/>): Endszene, Belohnung, Flags,
    /// Briefe, Kapitel-Banner, Verlassen der Instanz; danach Reise zur Zone des nächsten Schritts, wo dessen Startszene wie im Spiel läuft.
    /// Jeder per Admin abgeschlossene Schritt wird im Profil markiert („story:K03_S02“).
    /// </summary>
    public partial class StoryDirector
    {
        public bool AdminRunning { get; private set; }

        /// <summary>Weiter: <paramref name="steps"/> Schritte abschließen (oder bis Kapitelende, wenn <paramref name="wholeChapter"/>).</summary>
        public string AdminAdvance(int steps, bool wholeChapter)
        {
            if (AdminRunning) return "Admin-Ablauf läuft bereits";
            if (State == null || Database == null) return "kein Profil";
            if (State.Finished) return "Die Geschichte ist bereits abgeschlossen";
            if (_finishing) return "Schritt wird gerade abgeschlossen";
            var st = Step; if (st == null) return "kein Schritt";
            StartCoroutine(AdminRun(Mathf.Max(1, steps), wholeChapter));
            return (wholeChapter ? "Kapitel " + State.Chapter + " wird abgeschlossen" : "Weiter: " + st.Title) + (StoryAdmin.PlayScenes ? " (mit Szenen)" : " (Szenen übersprungen)");
        }

        IEnumerator AdminRun(int steps, bool wholeChapter)
        {
            AdminRunning = true; StoryAdmin.Active = true;
            int startChapter = State.Chapter;
            try
            {
                for (int n = 0; n < (wholeChapter ? 200 : steps); n++)
                {
                    var st = Step;
                    if (st == null || State.Finished) break;
                    while (_finishing) yield return null;
                    while (GameManager.Instance != null && GameManager.Instance.Transitioning) yield return null;
                    PlayerProfile.Instance?.State.AdminMark("story:" + st.Id);
                    for (int i = 0; i < st.Objectives.Count; i++) State.SetCounter(st.Id, i, Mathf.Max(1, st.Objectives[i].Count));
                    yield return FinishStep(st);
                    while (GameManager.Instance != null && GameManager.Instance.Transitioning) yield return null;
                    if (wholeChapter && (State.Chapter != startChapter || State.Finished)) break;
                }
                if (StoryAdmin.Travel) AdminTravelToStep();
            }
            finally { AdminRunning = false; StoryAdmin.Active = false; }
        }

        /// <summary>Reise zur Zone des aktuellen Schritts (an den Story-Marker); in der Zone baut sich die Präsenz wie im Spiel auf.</summary>
        public string AdminTravelToStep()
        {
            var st = Step; var gm = GameManager.Instance;
            if (st == null || gm == null || string.IsNullOrEmpty(st.Zone)) return "kein Ziel";
            if (st.Zone == Zone) { _presentFor = null; return "bereits in " + st.Zone; }
            gm.ChangeZone(st.Zone, null, st.Position != Vector3.zero ? st.Position + Vector3.up * 0.5f : (Vector3?)null);
            return "Reise nach " + st.Zone;
        }

        /// <summary>Springt an Kapitel/Schritt (setzt Zähler zurück, verlässt Instanzen) und reist hin.</summary>
        public string AdminJump(int chapter, int step)
        {
            if (AdminRunning) return "Admin-Ablauf läuft";
            string r = DebugSetChapter(chapter, step);
            PlayerProfile.Instance?.State.AdminMark("story:jump:" + State.Chapter + "_" + State.Step);
            return r + " · " + AdminTravelToStep();
        }

        /// <summary>Aktuellen Schritt zurücksetzen: Zähler leeren, Präsenz neu aufbauen, Instanz verlassen.</summary>
        public string AdminResetStep()
        {
            var st = Step; if (st == null) return "kein Schritt";
            for (int i = 0; i < st.Objectives.Count; i++) State.SetCounter(st.Id, i, 0);
            if (!string.IsNullOrEmpty(st.SceneStart)) State.SeenScenes.Remove(st.SceneStart);
            ClearPresence(); if (Instance.Active) LeaveInstance();
            _presentFor = null; GameEventBus.RaiseProfileChanged();
            return "zurückgesetzt: " + st.Title;
        }

        /// <summary>In einer Instanz: das offene Ziel über die normale Meldung erfüllen (Kontrollpunkte, Folgeziele, Abschluss wie im Spiel).</summary>
        public string AdminCompleteObjective()
        {
            var st = Step; if (st == null) return "kein Schritt";
            int open = StoryModel.FirstOpenObjective(st, State);
            if (open < 0) return "alle Ziele erfüllt";
            var o = st.Objectives[open];
            PlayerProfile.Instance?.State.AdminMark("story:obj:" + st.Id + "_" + open);
            int need = Mathf.Max(1, o.Count) - State.Counter(st.Id, open);
            Report(o.Kind, o.Target, Mathf.Max(1, need));
            if (StoryModel.FirstOpenObjective(st, State) == open)   // Meldung griff nicht (z. B. Stufe): Zähler direkt setzen
            {
                State.SetCounter(st.Id, open, Mathf.Max(1, o.Count));
                int after = StoryModel.FirstOpenObjective(st, State);
                if (after >= 0) OnObjectiveStarted(st, after);
                else StartCoroutine(FinishStep(st));
            }
            return "Ziel erfüllt: " + (string.IsNullOrEmpty(o.Text) ? o.Kind.ToString() : o.Text);
        }
    }
}
