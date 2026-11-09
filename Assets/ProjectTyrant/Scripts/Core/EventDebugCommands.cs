using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Debug-Befehle (F9) für die Ereignis-Engine.</summary>
    public static class EventDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Event"));
            DebugCommands.Extra.Add(("Event starten (Id)", a => { var d = EventDirector.Instance; if (d == null) return "Keine Engine"; var r = d.StartEvent(a); return r != null ? "Gestartet: " + r.Def.Title : "Unbekannt/aktiv: " + a; }));
            DebugCommands.Extra.Add(("Events: Status", a =>
            {
                var d = EventDirector.Instance; if (d == null || d.Scheduler == null) return "Keine Engine";
                var sb = new System.Text.StringBuilder("Aktiv " + d.Scheduler.RunningCount + ": ");
                foreach (var r in d.Scheduler.Runners) sb.Append(r.Def.Id).Append(" ").Append(Mathf.RoundToInt(r.Progress * 100)).Append("%; ");
                sb.Append(" Flags: ").Append(string.Join(",", new System.Collections.Generic.List<string>(d.Scheduler.Flags).ToArray()));
                return sb.ToString();
            }));
        }
    }
}
