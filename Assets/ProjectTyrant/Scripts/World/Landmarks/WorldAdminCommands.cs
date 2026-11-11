using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Welt-Lauf W1: Admin-Befehle im Debug-Panel (F9) – /landmark tp, /hazard show, /climb stage N.
    /// Teleport über <see cref="DebugCommands.Teleport"/> (gleicher Ablauf wie die übrigen Admin-Reisen).
    /// </summary>
    public static class WorldAdminCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Welt W1"));
            DebugCommands.Extra.Add(("Welt W1: /landmark tp (Name | haupt | Nr.)", a => LandmarkTp(a)));
            DebugCommands.Extra.Add(("Welt W1: /landmark list", a => LandmarkList()));
            DebugCommands.Extra.Add(("Welt W1: /hazard show", a => HazardShow()));
            DebugCommands.Extra.Add(("Welt W1: /climb stage N (1–3)", a => ClimbStage(a)));
        }

        static Vector3 PlayerPos => GameManager.Instance != null && GameManager.Instance.Player != null ? GameManager.Instance.Player.transform.position : Vector3.zero;

        static string Strip(string a, params string[] words)
        {
            a = (a ?? "").Trim();
            foreach (var w in words) if (a.StartsWith(w, System.StringComparison.OrdinalIgnoreCase)) a = a.Substring(w.Length).Trim();
            return a;
        }

        public static string LandmarkList()
        {
            if (LandmarkMarker.All.Count == 0) return "Keine Wahrzeichen in dieser Zone";
            int i = 0;
            return string.Join(" · ", LandmarkMarker.All.Select(l => (++i) + " " + l.Name + (l.Main ? " (Haupt)" : "") + " " + l.StagesReached + "/" + l.StageNames.Count));
        }

        public static string LandmarkTp(string arg)
        {
            arg = Strip(arg, "/landmark", "tp");
            var all = LandmarkMarker.All;
            if (all.Count == 0) return "Keine Wahrzeichen in dieser Zone";
            LandmarkMarker t = null;
            if (arg == "" || arg.ToLowerInvariant() == "haupt") t = all.FirstOrDefault(l => l.Main) ?? all[0];
            else if (int.TryParse(arg, out int n) && n >= 1 && n <= all.Count) t = all[n - 1];
            else t = all.FirstOrDefault(l => l.Name.ToLowerInvariant().Contains(arg.ToLowerInvariant()) || l.Id == arg);
            if (t == null) return "Kein Wahrzeichen „" + arg + "“ – " + LandmarkList();
            var fwd = t.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            var at = t.transform.position + fwd.normalized * (t.Radius * 0.6f + 6f) + Vector3.up * 2f;
            return Put(at) + " · " + t.Name;
        }

        /// <summary>Genau an die Stelle (Stufen liegen unter Dächern/Kappen – kein Boden-Suchen von oben).</summary>
        static string Put(Vector3 at)
        {
            var p = GameManager.Instance != null ? GameManager.Instance.Player : null; if (p == null) return "Kein Spieler";
            var pc = p.GetComponent<PlayerController>(); if (pc == null) return DebugCommands.Teleport(at);
            pc.Teleport(at, p.transform.rotation);
            return "Teleport " + at.ToString("F0");
        }

        public static string HazardShow()
        {
            var p = PlayerPos;
            var list = HazardSurface.All.OrderBy(h => (h.transform.position - p).sqrMagnitude).Take(6).ToList();
            if (list.Count == 0) return "Keine Hürden in dieser Zone";
            foreach (var h in list) NextGoalBeam.Show(h.transform.position, null);
            return string.Join(" · ", list.Select(h => HazardRules.Spec(h.Kind).Name + " " + Mathf.RoundToInt(Vector3.Distance(p, h.transform.position)) + " m (" + HazardRules.Requirement(h.Kind) + ")"));
        }

        public static string ClimbStage(string arg)
        {
            arg = Strip(arg, "/climb", "stage");
            if (!int.TryParse(arg, out int n)) n = 3;
            var p = PlayerPos;
            var c = ClimbArea.All.OrderBy(a => (a.transform.position - p).sqrMagnitude).FirstOrDefault();
            if (c == null) return "Kein Aufstieg in dieser Zone";
            n = Mathf.Clamp(n, 1, Mathf.Max(1, c.Stages.Count));
            if (c.Stages.Count == 0) return c.Name + ": keine Stufen";
            return Put(c.Stages[n - 1] + Vector3.up * 0.3f) + " · " + c.Name + " Stufe " + n;
        }
    }
}
