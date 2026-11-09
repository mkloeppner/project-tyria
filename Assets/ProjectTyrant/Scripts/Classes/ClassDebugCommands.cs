using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M25: Debug-Panel (F9) – Klasse wechseln, alle Skills freischalten, Mechanik-Status.</summary>
    public static class ClassDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Klasse") || e.label.StartsWith("Alle Skills") || e.label.StartsWith("Duell") || e.label.StartsWith("Elite"));
            DebugCommands.Extra.Add(("Klasse wechseln (Id)", a =>
            {
                var p = PlayerProfile.Instance; if (p == null) return "Kein Profil";
                if (string.IsNullOrEmpty(a)) { var ids = new System.Collections.Generic.List<string>(); if (p.Classes != null) foreach (var c in p.Classes.Classes) ids.Add(c.Id); return "Ids: " + string.Join(", ", ids); }
                return p.DebugSwitchClass(a.Trim().ToLowerInvariant());
            }));
            DebugCommands.Extra.Add(("Duell: alle Klassen je 180 s (x3)", a =>
            {
                var p = PlayerProfile.Instance; if (p == null || p.Classes == null) return "Kein Profil";
                var ids = new System.Collections.Generic.List<string>(); foreach (var c in p.Classes.Classes) ids.Add(c.Id);
                ClassDuelBot.Run(ids.ToArray(), 180f, 3f); return "Klassen-Duell gestartet (Bericht im Log)";
            }));
            DebugCommands.Extra.Add(("Elite aktivieren (Id, leer = Liste)", a => PlayerProfile.Instance != null ? PlayerProfile.Instance.DebugActivateElite((a ?? "").Trim().ToLowerInvariant()) : "Kein Profil"));
            DebugCommands.Extra.Add(("Elite ablegen", a => PlayerProfile.Instance != null && PlayerProfile.Instance.ActivateElite(null) ? "Elite abgelegt" : "Nicht möglich"));
            DebugCommands.Extra.Add(("Alle Skills freischalten", a => PlayerProfile.Instance != null ? PlayerProfile.Instance.DebugUnlockAll() : "Kein Profil"));
            DebugCommands.Extra.Add(("Klasse: Status", a =>
            {
                var gm = GameManager.Instance; var h = gm != null && gm.Player != null ? gm.Player.GetComponent<ClassMechanicHost>() : null;
                return h == null || h.Class == null ? "Keine Klasse" : h.Class.DisplayName + " · " + h.ResourceText + " · Mechanik-Einsätze " + h.MechanicUses;
            }));
        }
    }
}
