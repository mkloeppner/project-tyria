using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Debug-Befehle (F9) für die offene Welt: Zonenwechsel, Level-Cap, Ladezeit.</summary>
    public static class WorldDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Welt"));
            DebugCommands.Extra.Add(("Welt: Zone wechseln (Id)", a => ChangeZone(a)));
            // Fix-Lauf F1: Wegmarken-Sicherheit live prüfen
            DebugCommands.Extra.Add(("Welt: Spieler besiegen", a =>
            {
                var gm = GameManager.Instance; if (gm == null || gm.Player == null || gm.Player.IsDead) return "Nicht möglich";
                var dc = gm.Player.GetComponent<DownedController>(); if (dc != null) dc.ResetState();
                gm.Player.Die(null); GameEventBus.RaisePlayerDefeated(gm.Player); return "Besiegt";
            }));
            DebugCommands.Extra.Add(("Welt: Wegmarken dieser Zone vergessen", a =>
            {
                var gm = GameManager.Instance; var p = PlayerProfile.Instance; if (gm == null || p == null || gm.CurrentZone == null) return "Nicht möglich";
                int n = 0; foreach (var w in gm.CurrentZone.Waypoints) if (p.State.Completed.Remove(w.Id)) n++;
                return n + " Wegmarken zurückgesetzt";
            }));
            DebugCommands.Extra.Add(("Welt: nächste Zone", a =>
            {
                var gm = GameManager.Instance; if (gm == null || gm.World == null) return "Keine Welt";
                int i = gm.CurrentZone != null ? gm.World.Zones.IndexOf(gm.CurrentZone) : -1;
                return ChangeZone(gm.World.Zones[(i + 1) % gm.World.Zones.Count].Id);
            }));
            DebugCommands.Extra.Add(("Welt: Level-Cap setzen (0 = aus)", a =>
            {
                var p = PlayerProfile.Instance; if (p == null) return "Kein Profil";
                int.TryParse(a, out int cap);
                p.SetZoneLevelCap(cap);
                return "Effektiv " + p.EffectiveLevel + " (real " + p.State.Progression.Level + ")";
            }));
            DebugCommands.Extra.Add(("Welt: Reittier freischalten (Raptor/Springer/Skimmer/alle)", a =>
            {
                var p = PlayerProfile.Instance; if (p == null) return "Kein Profil";
                string arg = (a ?? "").Trim().ToLowerInvariant();
                int n = 0;
                foreach (MountKind k in System.Enum.GetValues(typeof(MountKind)))
                    if (k != MountKind.None && (arg == "" || arg == "alle" || k.ToString().ToLowerInvariant() == arg)) { p.UnlockMount(k); n++; }
                return n + " Reittier(e) freigeschaltet";
            }));
            DebugCommands.Extra.Add(("Welt: Beherrschungspunkte +N", a => { var p = PlayerProfile.Instance; if (p == null) return "Kein Profil"; int.TryParse(a, out int n); if (n <= 0) n = 5; p.AwardMasteryPoints(n, "Debug"); return "frei: " + p.Mastery.PointsAvailable; }));
            DebugCommands.Extra.Add(("Welt: Beherrschungs-EP +N", a => { var p = PlayerProfile.Instance; if (p == null) return "Kein Profil"; long.TryParse(a, out long n); if (n <= 0) n = 10000; if (!p.Mastery.Selected.HasValue) p.SelectMasteryLine(MasteryLine.Gleiten); p.Mastery.AddXp(n); return p.Mastery.Selected + " " + Mathf.RoundToInt((p.Mastery.Selected.HasValue ? p.Mastery.Progress(p.Mastery.Selected.Value) : 1f) * 100) + " %"; }));
            DebugCommands.Extra.Add(("Welt: alle Beherrschungen", a =>
            {
                var p = PlayerProfile.Instance; if (p == null) return "Kein Profil";
                var m = p.Mastery; m.AddPoints(40);
                foreach (MasteryLine l in System.Enum.GetValues(typeof(MasteryLine))) { m.Selected = l; while (!m.IsMaxed(l)) m.AddXp(m.XpNeeded(l)); }
                return "alle Linien gemeistert";
            }));
            DebugCommands.Extra.Add(("Welt: letzte Ladezeit", a => { var gm = GameManager.Instance; return gm == null ? "-" : gm.LastLoadSeconds.ToString("0.00") + " s"; }));
        }

        static string ChangeZone(string id)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.World == null) return "Keine Welt";
            id = (id ?? "").Trim().ToLowerInvariant();
            var z = gm.World.Zone(id);
            if (z == null) { var names = ""; foreach (var d in gm.World.Zones) names += d.Id + " "; return "Unbekannt. Zonen: " + names; }
            gm.ChangeZone(id, null);
            return "Wechsel nach " + z.DisplayName;
        }
    }
}
