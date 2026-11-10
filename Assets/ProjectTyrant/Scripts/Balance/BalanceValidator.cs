using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Event-Stufe (0 Einführung … 4 Meta/Boss).</summary>
    public enum EventTier { Intro = 0, Easy = 1, Standard = 2, Hard = 3, Meta = 4 }

    /// <summary>Balance B1: Regeln der Event-Stufen aus „Zielbild C“ (rein).</summary>
    public static class EventTierRules
    {
        public static readonly string[] Names = { "Einführung", "Leicht", "Standard", "Schwer", "Meta" };

        /// <summary>Erlaubte Gefahrenstufen je Event-Stufe.</summary>
        public static bool AllowedIn(EventTier t, int danger)
        {
            switch (t)
            {
                case EventTier.Intro: return danger == 0;
                case EventTier.Easy: return danger <= 2;   // auch „eine Stufe unter dem Gebiet“ (erstes Event am Eingang, Dornwacht-Verteidigung)
                case EventTier.Standard: return danger >= 1 && danger <= 3;
                case EventTier.Hard: return danger >= 3;
                default: return danger >= 4;
            }
        }

        /// <summary>Höchste Stufe nach Entfernungsband um Hub/Ankunft in der Startzone (Band 0 → Einführung, Band 1 → Leicht, sonst Standard).</summary>
        public static EventTier StartZoneMax(int band) => band <= 0 ? EventTier.Intro : band == 1 ? EventTier.Easy : EventTier.Standard;

        /// <summary>Stufe aus Gefahrenstufe (Standardzuordnung; Weltbosse/Metas bleiben Meta).</summary>
        public static EventTier ForDanger(int danger, bool meta)
        {
            if (meta) return EventTier.Meta;
            if (danger <= 0) return EventTier.Intro;
            if (danger == 1) return EventTier.Easy;
            if (danger <= 2) return EventTier.Standard;
            return danger == 3 ? EventTier.Standard : EventTier.Hard;
        }

        /// <summary>Keine Auto-Starts in Stufe 0–1: Timer-Auslöser erst ab 8 min.</summary>
        public const float MinAutoTimer = 480f;
        /// <summary>Ankündigung „Neues Ereignis in der Nähe“ vor den Gegnern (Stufe 0–1).</summary>
        public const float AnnounceLead = 10f;
        public static bool TimerAllowed(EventTier t, float delay) => t > EventTier.Easy || delay >= MinAutoTimer;

        /// <summary>Werte je Stufe: Timer-Minimum (s), Ziel-Faktor, max. Trash je Welle, Wellen, Veteranen, Neustart nach Fehlschlag (s).</summary>
        public struct TierValues { public float MinTimer, TargetFactor, RestartAfterFail; public int MaxTrash, Waves; public bool Veterans, Controls, Guards; }
        public static TierValues Values(EventTier t)
        {
            switch (t)
            {
                case EventTier.Intro: return new TierValues { MinTimer = 180, TargetFactor = 0.5f, MaxTrash = 5, Waves = 1, Veterans = false, Controls = false, Guards = true, RestartAfterFail = 120 };
                case EventTier.Easy: return new TierValues { MinTimer = 150, TargetFactor = 0.75f, MaxTrash = 8, Waves = 2, Veterans = true, Controls = false, Guards = true, RestartAfterFail = 180 };
                case EventTier.Standard: return new TierValues { MinTimer = 0, TargetFactor = 1f, MaxTrash = 99, Waves = 99, Veterans = true, Controls = true };
                default: return new TierValues { MinTimer = 0, TargetFactor = 1f, MaxTrash = 99, Waves = 99, Veterans = true, Controls = true };
            }
        }
    }

    /// <summary>Balance B1: ein Spawn (Builder- oder Szenen-Daten) für den Validator.</summary>
    public struct SpawnInfo { public string Id; public Vector2 Pos; public EnemyTier Tier; public bool Neutral, Story; }

    /// <summary>Balance B1: ein Event für den Validator.</summary>
    public struct EventInfo { public string Id; public Vector2 Center; public EventTier Tier; public float MinTimer; public bool Meta; public Vector2[] SpawnPoints; }

    /// <summary>Balance B1: Builder-Validator der Stufen-Regeln (Abschnitte 4 und 6). Verstöße brechen den Rebuild.</summary>
    public static class BalanceValidator
    {
        public static List<string> Validate(ZoneDangerMap m, IList<SpawnInfo> spawns, IList<EventInfo> events)
        {
            var err = new List<string>();
            string z = m.ZoneId + ": ";
            // 1) Stufen-Folge ohne Sprünge (Raster, Städte ausgenommen)
            int n = 40; float half = m.Size * 0.5f, cell = m.Size / n;
            var grid = new int[n + 1, n + 1]; var city = new bool[n + 1, n + 1];
            for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++)
            {
                var p = new Vector2(-half + i * cell, -half + j * cell);
                grid[i, j] = m.At(p); city[i, j] = m.IsCity(p);
            }
            int jumps = 0; string firstJump = null;
            for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++)
            {
                if (city[i, j]) continue;
                if (i < n && !city[i + 1, j] && Mathf.Abs(grid[i, j] - grid[i + 1, j]) > 1) { jumps++; firstJump = firstJump ?? (grid[i, j] + "→" + grid[i + 1, j]); }
                if (j < n && !city[i, j + 1] && Mathf.Abs(grid[i, j] - grid[i, j + 1]) > 1) { jumps++; firstJump = firstJump ?? (grid[i, j] + "→" + grid[i, j + 1]); }
            }
            if (jumps > 0) err.Add(z + "Stufen-Sprung (" + jumps + "×, z. B. " + firstJump + ")");
            // 2) Stufe 0 um Hub und Ankunft (Startzone)
            if (m.IsStartZone) foreach (var a in m.Anchors) if (m.At(a) != 0) err.Add(z + "Hub/Ankunft " + a + " nicht Stufe 0");
            // 3) Spawns
            var perLevel = new int[6]; var neutralPerLevel = new int[6];
            foreach (var s in spawns)
            {
                int l = m.At(s.Pos);
                if (m.IsCity(s.Pos) && !s.Story) err.Add(z + "Spawn in Stadt: " + s.Id);
                else if (m.InSafezone(s.Pos) && !s.Story) err.Add(z + "Spawn in Sicherheitszone: " + s.Id);
                else if (!m.SpawnAllowed(s.Pos) && !s.Story) err.Add(z + "Spawn in Stufe 0 zu nah an Wegmarke: " + s.Id);
                if (l == 0 && s.Tier >= EnemyTier.Veteran && !s.Story) err.Add(z + "Veteran in Stufe 0: " + s.Id);
                if (s.Story) continue;
                perLevel[l]++; if (s.Neutral) neutralPerLevel[l]++;
            }
            for (int l = 0; l < 6; l++)
            {
                if (perLevel[l] < 4) continue;
                float share = neutralPerLevel[l] / (float)perLevel[l], want = DangerTable.Default(l).NeutralShare;
                if (Mathf.Abs(share - want) > 0.2f) err.Add(z + "Neutral-Quote Stufe " + l + ": " + share.ToString("0.00") + " (Ziel " + want.ToString("0.00") + ")");
            }
            // 4) Events
            if (events != null) foreach (var e in events)
            {
                int l = m.At(e.Center);
                if (e.Meta) { if (e.Tier != EventTier.Meta) err.Add(z + "Meta-Event " + e.Id + " nicht Stufe 4"); continue; }
                if (!EventTierRules.AllowedIn(e.Tier, l)) err.Add(z + "Event " + e.Id + " Stufe " + (int)e.Tier + " in Gefahrenstufe " + l);
                if (m.IsStartZone && e.Tier > EventTierRules.StartZoneMax(m.BandAt(e.Center))) err.Add(z + "Event " + e.Id + " zu schwer für die Entfernung vom Hub");
                if (e.Tier <= EventTier.Easy && e.MinTimer > 0f && e.MinTimer < EventTierRules.MinAutoTimer) err.Add(z + "Auto-Start < 8 min: " + e.Id);
                if (e.SpawnPoints != null && e.Tier <= EventTier.Easy)
                    foreach (var p in e.SpawnPoints)
                    {
                        if (m.IsStartZone && Vector2.Distance(m.Anchors[0], p) < m.EventSpawnClearance) { err.Add(z + "Event-Spawn nahe Hub: " + e.Id); break; }
                        bool near = false; foreach (var w in m.Waypoints) if (Vector2.Distance(w, p) < m.EventSpawnClearance) near = true;
                        if (near) { err.Add(z + "Event-Spawn nahe Wegmarke: " + e.Id); break; }
                    }
            }
            return err;
        }

        /// <summary>Profile monoton: mit der Stufe nie leichter (Aggro, Leine, Tempo, Dauer, Fessel-Quote, Respawn kürzer).</summary>
        public static List<string> ValidateProfiles(IList<DangerSettings> p)
        {
            var err = new List<string>();
            for (int i = 1; i < p.Count; i++)
            {
                var a = p[i - 1]; var b = p[i];
                if (b.AggroRange < a.AggroRange) err.Add("Aggro " + i);
                if (b.Leash < a.Leash) err.Add("Leine " + i);
                if (b.PursuitSpeed < a.PursuitSpeed) err.Add("Tempo " + i);
                if (b.MaxPursuit < a.MaxPursuit) err.Add("Dauer " + i);
                if (b.ControlChance < a.ControlChance) err.Add("Fessel " + i);
                if (b.Respawn > a.Respawn) err.Add("Respawn " + i);
                if (b.NeutralShare > a.NeutralShare) err.Add("Neutral " + i);
                if (b.TtdThreeTrash > a.TtdThreeTrash) err.Add("Zeit-bis-Tod " + i);
                if (b.HitShare < a.HitShare) err.Add("Schaden " + i);
                if (b.MinTelegraph > a.MinTelegraph) err.Add("Telegraph " + i);
            }
            return err;
        }
    }
}
