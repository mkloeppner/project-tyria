using UnityEngine;
using ProjectTyrant.Flight;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 10: Admin-Menü im Debug-Panel (F9, nur Editor/Dev-Build). Reittiere und Beherrschungen freischalten (mit Admin-Kennzeichnung
    /// im Profil), Flug-Debug (/flight …) und Story-Steuerung über den echten Abschluss-Ablauf (<see cref="StoryDirector.AdminAdvance"/>).
    /// </summary>
    public static class AdminCommands
    {
        static StoryDirector D => GameManager.Instance != null ? GameManager.Instance.Story : null;
        static PlayerProfile P => PlayerProfile.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Admin"));
            DebugCommands.Extra.Add(("Admin: Reittier freischalten (Name | alle)", a => UnlockMounts(a)));
            DebugCommands.Extra.Add(("Admin: Beherrschung meistern (Linie | alle)", a => MasterLines(a)));
            DebugCommands.Extra.Add(("Admin: alles freischalten", a => UnlockMounts("alle") + " · " + MasterLines("alle")));
            DebugCommands.Extra.Add(("Admin: /flight (infinite | ceiling off | rules off | show | reset)", a => Flight(a)));
            DebugCommands.Extra.Add(("Admin: Story weiter (N Schritte)", a => { var d = D; if (d == null) return "Kein Story-Direktor"; int.TryParse(a, out int n); return d.AdminAdvance(Mathf.Max(1, n), false); }));
            DebugCommands.Extra.Add(("Admin: Kapitel abschließen", a => D != null ? D.AdminAdvance(1, true) : "Kein Story-Direktor"));
            DebugCommands.Extra.Add(("Admin: zu Kapitel springen (K S)", a =>
            {
                var d = D; if (d == null) return "Kein Story-Direktor";
                var p = (a ?? "").Trim().Split(' '); int k = 1, s = 1;
                if (p.Length > 0) int.TryParse(p[0], out k); if (p.Length > 1) int.TryParse(p[1], out s);
                return d.AdminJump(Mathf.Clamp(k, 1, 10), Mathf.Max(0, s - 1));
            }));
            DebugCommands.Extra.Add(("Admin: Schritt zurücksetzen", a => D != null ? D.AdminResetStep() : "Kein Story-Direktor"));
            DebugCommands.Extra.Add(("Admin: Instanz-Ziel erfüllen", a => D != null ? D.AdminCompleteObjective() : "Kein Story-Direktor"));
            DebugCommands.Extra.Add(("Admin: Story-Szenen an/aus", a => { StoryAdmin.PlayScenes = !StoryAdmin.PlayScenes; return "Szenen beim Weiter: " + (StoryAdmin.PlayScenes ? "abspielen" : "überspringen"); }));
            DebugCommands.Extra.Add(("Admin: Story-Auto-Wahl an/aus", a => { StoryAdmin.AutoChoose = !StoryAdmin.AutoChoose; return "Wahl-Stellen: " + (StoryAdmin.AutoChoose ? "erste Antwort automatisch" : "selbst wählen"); }));
            DebugCommands.Extra.Add(("Admin: Story-Reisen an/aus", a => { StoryAdmin.Travel = !StoryAdmin.Travel; return "Nach dem Weiter zur Zone reisen: " + (StoryAdmin.Travel ? "ja" : "nein"); }));
            DebugCommands.Extra.Add(("Admin: Kennzeichnungen anzeigen", a => { var p = P; if (p == null) return "Kein Profil"; return p.State.AdminMarks.Count == 0 ? "keine Admin-Freischaltungen" : p.State.AdminMarks.Count + ": " + string.Join(", ", p.State.AdminMarks); }));
        }

        public static string UnlockMounts(string arg)
        {
            var p = P; if (p == null) return "Kein Profil";
            arg = (arg ?? "").Trim().ToLowerInvariant();
            int n = 0; string names = "";
            foreach (MountKind k in System.Enum.GetValues(typeof(MountKind)))
            {
                if (k == MountKind.None) continue;
                bool match = arg == "" || arg == "alle" || k.ToString().ToLowerInvariant() == arg || (p.Mount(k) != null && p.Mount(k).DisplayName.ToLowerInvariant() == arg);
                if (!match || p.OwnsMount(k)) continue;
                p.UnlockMount(k); p.State.AdminMark("mount:" + k); n++; names += k + " ";
            }
            p.RequestSave("Admin");
            return n == 0 ? "nichts freizuschalten" : n + " Reittier(e) freigeschaltet: " + names.Trim();
        }

        public static string MasterLines(string arg)
        {
            var p = P; if (p == null) return "Kein Profil";
            arg = (arg ?? "").Trim().ToLowerInvariant();
            var m = p.Mastery; int n = 0;
            foreach (MasteryLine l in System.Enum.GetValues(typeof(MasteryLine)))
            {
                bool match = arg == "" || arg == "alle" || l.ToString().ToLowerInvariant() == arg || MasteryModel.LineName(l).ToLowerInvariant() == arg;
                if (!match || m.IsMaxed(l)) continue;
                int need = 0; var c = m.Costs(l); for (int i = m.Rank(l); i < c.Length; i++) need += c[i].Points;
                int missing = Mathf.Max(0, need - m.PointsAvailable);
                if (missing > 0) m.AddPoints(missing);
                var prev = m.Selected; m.Selected = l;
                while (!m.IsMaxed(l)) m.AddXp(m.XpNeeded(l));
                m.Selected = prev.HasValue && prev.Value != l && !m.IsMaxed(prev.Value) ? prev : null;
                p.State.AdminMark("mastery:" + l); n++;
            }
            GameEventBus.RaiseProfileChanged(); p.RequestSave("Admin");
            if (MountController.Local != null && MountController.Local.Motor.IsMounted) MountController.Local.RefreshFlightParams();
            return n == 0 ? "nichts zu meistern" : n + " Linie(n) gemeistert";
        }

        public static string Flight(string arg)
        {
            string a = (arg ?? "").Trim().ToLowerInvariant().Replace("/flight", "").Trim();
            switch (a)
            {
                case "infinite": FlightDebug.Infinite = !FlightDebug.Infinite; break;
                case "ceiling off": case "ceiling": FlightDebug.CeilingOff = !FlightDebug.CeilingOff; break;
                case "rules off": case "rules": FlightDebug.RulesOff = !FlightDebug.RulesOff; break;
                case "show": FlightDebug.Show = !FlightDebug.Show; break;
                case "reset": FlightDebug.Reset(); break;
                case "": break;
                default: return "Unbekannt. infinite | ceiling off | rules off | show | reset";
            }
            return "Flug: unendlich " + OnOff(FlightDebug.Infinite) + " · Decke aus " + OnOff(FlightDebug.CeilingOff) + " · Regeln aus " + OnOff(FlightDebug.RulesOff) + " · Anzeige " + OnOff(FlightDebug.Show);
        }

        static string OnOff(bool b) => b ? "an" : "aus";
    }
}
