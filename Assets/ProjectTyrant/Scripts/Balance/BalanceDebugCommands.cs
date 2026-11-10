using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: Admin-Befehle (Debug-Panel F9, Parameterfeld): /danger show|set, /pursuit profile, /npc neutral, /telemetry, /bot run, /event tier, Zielwerte.
    /// </summary>
    public static class BalanceDebugCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Balance"));
            BalanceTelemetry.Context = p => (ZoneRoot.Current != null ? ZoneRoot.Current.Id : "", ZoneRoot.RegionNameAt(p), BalanceWorld.DangerAt(p));
            BalanceTelemetry.PlayerLevel = () => Player != null ? Player.Level : 1;
            GameEventBus.DamageDealt -= OnDamage; GameEventBus.DamageDealt += OnDamage;
            DebugCommands.Extra.Add(("Balance: Stufe hier / Overlay (danger show)", a => DangerShow()));
            DebugCommands.Extra.Add(("Balance: Stufe setzen (Gebiet Stufe)", a => DangerSet(a)));
            DebugCommands.Extra.Add(("Balance: Verfolgungsprofil (Stufe)", a => PursuitProfileText(a)));
            DebugCommands.Extra.Add(("Balance: Neutral (all/none/data)", a => Neutral(a)));
            DebugCommands.Extra.Add(("Balance: Telemetrie (start/stop)", a => Telemetry(a)));
            DebugCommands.Extra.Add(("Balance: Heatmaps (deaths/escapes)", a => Heatmaps(a)));
            DebugCommands.Extra.Add(("Balance: Bot (beginner|experienced Stufe Minuten Seed)", a => Bot(a)));
            DebugCommands.Extra.Add(("Balance: Bot stoppen", a => { BalanceBot.Stop(); return "Bot gestoppt"; }));
            DebugCommands.Extra.Add(("Balance: Event-Stufe (Id Stufe)", a => EventTierSet(a)));
            DebugCommands.Extra.Add(("Balance: Zielwerte auswerten", a => Evaluate()));
            DebugCommands.Extra.Add(("Balance: TTD-Messung (Stufe Anzahl)", a => BalanceProbe.StartTtd(a)));
        }

        static void OnDamage(DamageInfo d) { if (d.Source != null && d.Source == Player && d.Amount > 0f) BalanceWorld.PlayerDealtDamageAt = Time.time; }

        static Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        public static string DangerShow()
        {
            var p = Player; if (p == null) return "Kein Spieler";
            int l = BalanceWorld.DangerAt(p.transform.position);
            DangerOverlay.Toggle();
            return "Gefahrenstufe hier: " + l + " (" + DangerTable.Names[l] + ")" + (BalanceWorld.InSafezone(p.transform.position) ? " · Sicherheitszone" : "") + " · Overlay " + (DangerOverlay.Visible ? "an" : "aus");
        }

        public static string DangerSet(string a)
        {
            var parts = (a ?? "").Trim();
            int sp = parts.LastIndexOf(' ');
            if (sp < 0 || !int.TryParse(parts.Substring(sp + 1), out int lvl)) return "Format: <Gebiet> <Stufe>";
            string region = parts.Substring(0, sp).Trim();
            BalanceWorld.RegionOverride[region] = Mathf.Clamp(lvl, 0, 5);
            return region + " → Stufe " + lvl + " (bis Neustart; Gegner übernehmen die Stufe beim nächsten Spawn)";
        }

        public static string PursuitProfileText(string a)
        {
            int.TryParse(a, out int l); var s = BalanceWorld.Profile(l);
            var p = PursuitProfile.From(s);
            return "Stufe " + l + ": Aggro " + s.AggroRange + " (" + p.AggroRange.ToString("0.0") + " m) · Leine " + s.Leash + " (" + p.Leash.ToString("0") + " m) · Tempo " + s.PursuitSpeed + "× · max " + s.MaxPursuit + " s · Sicht " + s.LosForget + " s · Zögern " + s.Hesitation + " s · Fessel " + (s.ControlChance * 100f).ToString("0") + " % · Rudel " + s.Pack + " · Respawn " + s.Respawn + " s · neutral " + (s.NeutralShare * 100f).ToString("0") + " %";
        }

        public static string Neutral(string a)
        {
            a = (a ?? "").Trim().ToLowerInvariant();
            BalanceWorld.NeutralOverride = a == "all" ? true : a == "none" ? (bool?)false : null;
            return "Neutral: " + (BalanceWorld.NeutralOverride.HasValue ? (BalanceWorld.NeutralOverride.Value ? "alle neutral" : "alle feindlich") : "Daten");
        }

        public static string Telemetry(string a)
        {
            a = (a ?? "").Trim().ToLowerInvariant();
            if (a == "stop") { var s = BalanceTelemetry.End(); return s != null ? "Gespeichert: " + s.Id + " (" + s.Records.Count + " Einträge)" : "Keine Session"; }
            var z = ZoneRoot.Current != null ? ZoneRoot.Current.Id : "?";
            BalanceTelemetry.Begin("Manual", z, 0, Player != null ? BalanceWorld.DangerAt(Player.transform.position) : -1);
            TelemetryProbe.Attach(Player);
            return "Telemetrie läuft (" + z + ")";
        }

        public static string Heatmaps(string a)
        {
            var kind = (a ?? "").Contains("esc") ? TelemetryKind.EscapeAttempt : TelemetryKind.Death;
            return HeatmapsFor(BalanceTelemetry.ReadAll(), kind, "");
        }

        public static string HeatmapsFor(List<TelemetrySession> sessions, TelemetryKind kind, string suffix)
        {
            var gm = GameManager.Instance; if (gm == null || gm.World == null) return "Keine Welt";
            var sb = new StringBuilder();
            foreach (var z in gm.World.Zones)
            {
                var pts = Heatmap.Points(sessions, z.Id, kind);
                bool any = false; foreach (var s in sessions) if (s != null && s.Zone == z.Id) any = true;
                if (!any) continue;
                var tex = Heatmap.Render(z.Id == (ZoneRoot.Current != null ? ZoneRoot.Current.Id : "") ? ZoneRoot.Current.MapTexture : null, z.Size, pts, kind == TelemetryKind.Death ? new Color(1f, 0.1f, 0.1f) : new Color(0.2f, 0.6f, 1f), BalanceWorld.BuildMap(z));
                sb.AppendLine(Heatmap.Save(tex, "heat_" + (kind == TelemetryKind.Death ? "deaths" : "escapes") + "_" + z.Id + suffix) + " (" + pts.Count + ")");
            }
            return sb.Length > 0 ? sb.ToString() : "Keine Sessions";
        }

        public static string Bot(string a)
        {
            var parts = (a ?? "").Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var kind = parts.Length > 0 && parts[0].StartsWith("exp") ? BotProfileKind.Experienced : BotProfileKind.Beginner;
            int danger = parts.Length > 1 && int.TryParse(parts[1], out int d) ? d : 0;
            float minutes = parts.Length > 2 && float.TryParse(parts[2], out float m) ? m : 30f;
            var cell = BalancePlan.Find(danger); if (cell == null) return "Unbekannte Stufe";
            if (parts.Length > 3 && int.TryParse(parts[3], out int seed)) BalanceBot.Plan.Enqueue((kind, cell.Value, seed, minutes * 60f));
            else foreach (var s in BalancePlan.Seeds) BalanceBot.Plan.Enqueue((kind, cell.Value, s, minutes * 60f));
            BalanceBot.RunPlan();
            return "Bot " + kind + " · Stufe " + danger + " · " + minutes + " min · " + BalanceBot.Plan.Count + " weitere";
        }

        /// <summary>Gesamte Serie für ein Profil (Stufen-Liste, z. B. "0 1 2").</summary>
        public static string Series(BotProfileKind kind, IEnumerable<int> dangers, float seconds = BalancePlan.SessionSeconds)
        {
            foreach (var e in BalancePlan.Build(kind, dangers)) BalanceBot.Plan.Enqueue((e.Item1, e.Item2, e.Item3, seconds));
            BalanceBot.RunPlan();
            return BalanceBot.Plan.Count + 1 + " Sessions geplant";
        }

        public static string EventTierSet(string a)
        {
            var parts = (a ?? "").Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int t)) return "Format: <Id> <Stufe>";
            var dir = EventDirector.Instance; if (dir == null || dir.Scheduler == null) return "Keine Engine";
            EventDefinition def = null; foreach (var e in dir.Definitions) if (e != null && e.Id == parts[0]) def = e; if (def == null) return "Unbekannt: " + parts[0];
            def.Tier = (EventTier)Mathf.Clamp(t, 0, 4); def.TierSet = true;
            return def.Title + " → Stufe " + t + " (" + EventTierRules.Names[t] + ")";
        }

        public static string Evaluate()
        {
            var res = TargetTable.Evaluate(BalanceTelemetry.ReadAll());
            var sb = new StringBuilder();
            int fail = 0; foreach (var r in res) { sb.AppendLine(r.ToString()); if (!r.Pass) fail++; }
            sb.Insert(0, res.Count + " Zellen, " + fail + " verfehlt\n");
            Debug.Log("[ProjectTyrant] Zielwerte:\n" + sb);
            return res.Count + " Zellen, " + fail + " verfehlt (Konsole)";
        }
    }
}
