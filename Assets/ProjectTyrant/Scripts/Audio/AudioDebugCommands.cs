using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: Sound-Test im Debug-Panel (F9) – Klänge der Bank durchspielen, Musik-Zustände/Stinger erzwingen, Pegel-Log.</summary>
    public static class AudioDebugCommands
    {
        static int _idx;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Sound") || e.label.StartsWith("Musik"));
            DebugCommands.Extra.Add(("Sound-Test: nächster", a =>
            {
                var p = AudioPool.Current; if (p == null || p.Bank == null) return "Kein Audio-Pool";
                var list = p.Bank.Entries; if (list.Count == 0) return "Bank leer";
                if (!string.IsNullOrEmpty(a)) { int f = list.FindIndex(e => e.Id.StartsWith(a)); if (f >= 0) _idx = f; }
                var e2 = list[_idx % list.Count]; _idx++;
                var pl = GameManager.Instance.Player; p.Play(e2.Id, pl != null ? pl.transform.position + pl.transform.forward * 2f : Vector3.zero);
                return e2.Id + " (" + e2.Category + ") · " + (e2.Stats.Length > 0 ? e2.Stats[0].ToString() : "");
            }));
            DebugCommands.Extra.Add(("Musik: Stinger", a =>
            {
                var md = GameManager.Instance != null ? GameManager.Instance.GetComponent<MusicDirector>() : null; if (md == null) return "Kein Direktor";
                var k = StingerKind.Victory; if (!string.IsNullOrEmpty(a)) System.Enum.TryParse(a, true, out k);
                md.PlayStinger(k); return "Stinger " + md.LastStinger;
            }));
            DebugCommands.Extra.Add(("Sound: Pegel-Log", a =>
            {
                var p = AudioPool.Current; var md = GameManager.Instance != null ? GameManager.Instance.GetComponent<MusicDirector>() : null;
                if (p == null) return "Kein Audio-Pool";
                return (md != null ? md.Model.State + " · " + md.CurrentKey + " · " : "") + (p.LevelLog.Count > 0 ? p.LevelLog[p.LevelLog.Count - 1] : "") + " · Spitze " + p.PeakVoices + " Stimmen";
            }));
        }
    }
}
