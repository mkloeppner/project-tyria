using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Debug-Befehle für Items (F9): Item per Name/Id, Champion-Beutel, Startausrüstung neu.</summary>
    public static class ItemDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Item"));
            DebugCommands.Extra.Add(("Item geben (Name/Id)", GiveItem));
            DebugCommands.Extra.Add(("Item: Champion-Beutel", a => GiveItem("box_champion")));
            DebugCommands.Extra.Add(("Item: 5 Plunder", a => GiveItem("junk_bone:5")));
        }

        public static string GiveItem(string arg)
        {
            var p = PlayerProfile.Instance;
            if (p == null || p.Items == null) return "Kein Profil";
            if (string.IsNullOrEmpty(arg)) return "Name oder Id eingeben (optional :Anzahl)";
            var parts = arg.Split(':');
            int n = parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 1;
            var d = p.Items.Get(parts[0].Trim()) ?? p.Items.FindByName(parts[0].Trim());
            if (d == null) return "Unbekanntes Item: " + parts[0];
            return p.GiveItem(d, n) ? "+ " + d.DisplayName + " ×" + n : "Inventar voll";
        }
    }
}
