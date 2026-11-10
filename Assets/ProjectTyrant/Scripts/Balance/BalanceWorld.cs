using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: Laufzeit-Zugang zu Gefahrenstufen, Sicherheitszonen und Admin-Überschreibungen (statisch, kein Singleton).
    /// </summary>
    public static class BalanceWorld
    {
        static BalanceConfig _config;
        static readonly DangerSettings[] _defaults = new DangerSettings[DangerTable.MaxLevel + 1];
        public static BalanceConfig Config { get => _config; set { _config = value; if (value != null) EnemyCurve.Player = value.Player; } }
        /// <summary>Admin: /danger set &lt;Gebiet&gt; &lt;Stufe&gt; (Laufzeit, nicht gespeichert).</summary>
        public static readonly Dictionary<string, int> RegionOverride = new Dictionary<string, int>();
        /// <summary>Admin: /npc neutral all|none (null = Daten).</summary>
        public static bool? NeutralOverride;
        /// <summary>
        /// Messschalter „vorher“: schaltet alle B1-Laufzeitregeln ab (Kurve, Verfolgung, Neutral, Rudel, Fesseln, Telegraph-Minimum, Event-Stufen,
        /// Kampfende 6 s, Aufsitzen im Kampf, NPC-Wiederbelebung). Nur für die Baseline-Messung mit den alten Szenen; kein Spieler-Regler.
        /// </summary>
        public static bool Legacy;
        public static bool LegacyPursuit { get => Legacy; set => Legacy = value; }
        /// <summary>Letzter Zeitpunkt, an dem der Spieler Schaden verursacht hat (Fluchtgnade).</summary>
        public static float PlayerDealtDamageAt = -999f;
        /// <summary>Letzte Reise per Wegmarke/Portal/Zonenwechsel (Aufgabe-Regel 6).</summary>
        public static float PlayerTravelledAt = -999f;
        static readonly HashSet<string> _hintShown = new HashSet<string>();

        public static DangerSettings Profile(int level)
        {
            level = Mathf.Clamp(level, 0, DangerTable.MaxLevel);
            if (_config != null) return _config.Get(level);
            return _defaults[level] ?? (_defaults[level] = DangerTable.Default(level));
        }

        static ZoneDangerMap _lazy; static ZoneDefinition _lazyFor;
        /// <summary>Gefahrenkarte der aktuellen Zone: vom Builder gebacken (ZoneDefinition.DangerMap), sonst aus den Zonendaten berechnet.</summary>
        public static ZoneDangerMap Map
        {
            get
            {
                var z = ZoneRoot.Current;
                if (z == null || z.Definition == null) return null;
                var d = z.Definition;
                if (d.DangerMap != null && d.DangerMap.Anchors.Count > 0) return d.DangerMap;
                if (_lazyFor != d) { _lazy = BuildMap(d); _lazyFor = d; }
                return _lazy;
            }
        }

        /// <summary>Baut die Gefahrenkarte aus den Zonendaten (Builder und Laufzeit nutzen dieselbe Funktion).</summary>
        public static ZoneDangerMap BuildMap(ZoneDefinition d)
        {
            if (d == null) return null;
            bool start = d.LegacyLayout || d.Id == "sonnfurt";
            var arrivals = new List<Vector2>();
            if (d.Arrivals != null) foreach (var a in d.Arrivals) if (a != null) arrivals.Add(new Vector2(a.Position.x, a.Position.z));
            var wps = new List<Vector2>();
            if (d.Waypoints != null) foreach (var w in d.Waypoints) if (w != null) wps.Add(new Vector2(w.Position.x, w.Position.z));
            var regions = new List<(string, Vector2, float)>();
            Vector2 hub;
            if (start)
            {
                foreach (var r in Procedural.ZoneLayout.Regions) regions.Add((r.Name, r.Center, r.Radius));
                if (wps.Count == 0) foreach (var w in Procedural.ZoneContent.Waypoints) wps.Add(w.Pos);
                hub = Procedural.ZoneLayout.Hub.Center;
            }
            else
            {
                if (d.Regions != null) foreach (var r in d.Regions) if (r != null) regions.Add((r.Name, r.Center, r.Radius));
                hub = new Vector2(d.DefaultSpawn.x, d.DefaultSpawn.z);
            }
            return ZoneDangerTable.Build(d.Id, d.Size, hub, arrivals, wps, regions, start);
        }

        /// <summary>Gefahrenstufe an einer Position (ohne Karte: 2 = Standard).</summary>
        public static int DangerAt(Vector3 p)
        {
            var m = Map;
            if (m == null) return 2;
            if (RegionOverride.Count > 0)
            {
                var a = m.AreaAt(new Vector2(p.x, p.z));
                if (a != null && RegionOverride.TryGetValue(a.Name, out int l)) return l;
            }
            return m.At(p);
        }

        public static bool InSafezone(Vector3 p) { var m = Map; return m != null && m.InSafezone(new Vector2(p.x, p.z)); }
        /// <summary>Spieler steht im Hub (Startzone) oder in einer Stadt.</summary>
        public static bool InHub(Vector3 p)
        {
            var m = Map; if (m == null) return false;
            var q = new Vector2(p.x, p.z);
            return m.IsCity(q) || (m.IsStartZone && m.Anchors.Count > 0 && Vector2.Distance(m.Anchors[0], q) <= m.HubSafeRadius + 10f);
        }

        /// <summary>Schiebt einen Event-Spawn aus dem Abstand um Hub und Wegmarken (100 m, skaliert) heraus.</summary>
        public static Vector3 ClearSpawn(Vector3 p)
        {
            var m = Map; if (m == null) return p;
            float r = m.EventSpawnClearance;
            var q = new Vector2(p.x, p.z);
            for (int it = 0; it < 4; it++)
            {
                bool moved = false;
                var centers = new List<Vector2>(m.Waypoints); if (m.IsStartZone && m.Anchors.Count > 0) centers.Add(m.Anchors[0]);
                foreach (var c in centers)
                {
                    var d = q - c; float dist = d.magnitude;
                    if (dist < r) { q = c + (dist < 0.01f ? Vector2.right : d / dist) * (r + 0.5f); moved = true; }
                }
                if (!moved) break;
            }
            return new Vector3(q.x, p.y, q.y);
        }

        public static bool NoLeashAt(Vector3 p) { var m = Map; return m != null && m.NoLeashAt(new Vector2(p.x, p.z)); }

        public static int TierOf(EventDefinition d)
        {
            if (d == null) return -1;
            if (d.TierSet) return (int)d.Tier;
            return (int)EventTierRules.ForDanger(DangerAt(d.Center), d.Objective == EventObjective.WorldBoss || !string.IsNullOrEmpty(d.MetaId));
        }

        /// <summary>Lern-Hinweis einmal je Schlüssel (z. B. „Gegner gibt auf“).</summary>
        public static bool FirstTime(string key) => _hintShown.Add(key);

        public static void Reset() { RegionOverride.Clear(); NeutralOverride = null; LegacyPursuit = false; _hintShown.Clear(); PlayerDealtDamageAt = PlayerTravelledAt = -999f; }
    }
}
