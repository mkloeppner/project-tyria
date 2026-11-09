using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Debug-Befehle (F9) für den Zonenabschluss.</summary>
    public static class ZoneDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Zone"));
            DebugCommands.Extra.Add(("Zone: alles abschließen", a => { var p = PlayerProfile.Instance; return p == null ? "Kein Profil" : "Abgeschlossen: " + p.CompleteAllZoneObjects(); }));
            DebugCommands.Extra.Add(("Zone: Stand", a => { var p = PlayerProfile.Instance; if (p == null) return "Kein Profil"; var c = p.ZoneCompletionNow(); return c.Done + " / " + c.Total + " (" + Mathf.FloorToInt(c.Percent * 100f) + " %)"; }));
        }
    }
}
