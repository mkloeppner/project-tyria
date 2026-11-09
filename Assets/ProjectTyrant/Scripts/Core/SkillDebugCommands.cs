using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Debug-Befehle (F9) für Fertigkeiten und Eigenschaften.</summary>
    public static class SkillDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Build"));
            DebugCommands.Extra.Add(("Build: alles zurückerstatten", a => { var p = PlayerProfile.Instance; return p == null ? "Kein Profil" : "Zurückerstattet: " + p.RespecAll() + " Heldenpunkte"; }));
        }
    }
}
