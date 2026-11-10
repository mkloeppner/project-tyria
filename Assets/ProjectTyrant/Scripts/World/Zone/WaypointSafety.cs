using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Fix-Lauf F1 – Wegmarken-Sicherheit (Soft-Lock „Besiegt ohne Wegmarke“): Eingänge je Zone, Ankunfts-Wegmarken (≤ 40 m),
    /// automatische Freischaltung beim Betreten, Besiegt-Optionen (nie leer, Notfall-Wegmarke) und Migration alter Stände. Rein, getestet.
    /// </summary>
    public static class WaypointSafety
    {
        public const float MaxDistance = 40f;
        public const string SpawnEntrance = "spawn";
        public const float PortalArrivalOffset = 6f;   // wie GameManager.World: Ankunft 6 m vor dem Portal
        /// <summary>Fix-Lauf F1 (QA-Fund Dauertest): Weltboden – wer tiefer fällt (Loch im Gelände, Teleport neben die Karte), wird zur nächsten Wegmarke zurückgeholt.</summary>
        public const float WorldFloorY = -150f;
        public static bool BelowWorld(Vector3 p) => p.y < WorldFloorY || float.IsNaN(p.y);

        public static Vector3 ArrivalPoint(PortalDef p) => p.Position + Quaternion.Euler(0f, p.Yaw, 0f) * Vector3.forward * PortalArrivalOffset;
        public static string PortalEntrance(string portalId) => "portal:" + portalId;
        public static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        /// <summary>Alle Eingänge der Zone: Charakterstart/Standard-Spawn und jede Portal-Ankunft.</summary>
        public static List<KeyValuePair<string, Vector3>> Entrances(ZoneDefinition z)
        {
            var l = new List<KeyValuePair<string, Vector3>>();
            if (z == null) return l;
            l.Add(new KeyValuePair<string, Vector3>(SpawnEntrance, z.DefaultSpawn));
            if (z.Portals != null) foreach (var p in z.Portals) if (p != null) l.Add(new KeyValuePair<string, Vector3>(PortalEntrance(p.Id), ArrivalPoint(p)));
            return l;
        }

        public static ZoneWaypointInfo Nearest(IEnumerable<ZoneWaypointInfo> wps, Vector3 pos, out float distance, Func<ZoneWaypointInfo, bool> filter = null)
        {
            distance = float.MaxValue; ZoneWaypointInfo best = null;
            if (wps == null) return null;
            foreach (var w in wps)
            {
                if (w == null || (filter != null && !filter(w))) continue;
                float d = FlatDistance(w.Position, pos);
                if (d < distance) { distance = d; best = w; }
            }
            return best;
        }

        /// <summary>Eingänge ohne Wegmarke in ≤ 40 m (Builder-Validierung und Test).</summary>
        public static List<string> Uncovered(ZoneDefinition z)
        {
            var bad = new List<string>();
            foreach (var e in Entrances(z)) { Nearest(z.Waypoints, e.Value, out float d); if (d > MaxDistance) bad.Add(z.Id + "/" + e.Key + " (" + (d > 1e8f ? "keine" : Mathf.RoundToInt(d) + " m") + ")"); }
            return bad;
        }

        /// <summary>Ankunfts-Wegmarke eines Eingangs (Builder-Zuordnung, sonst nächste in ≤ 40 m).</summary>
        public static ZoneWaypointInfo ArrivalFor(ZoneDefinition z, string entrance)
        {
            if (z == null) return null;
            if (z.Arrivals != null) foreach (var a in z.Arrivals) if (a != null && a.Entrance == entrance) { var w = z.Waypoints.Find(x => x.Id == a.WaypointId); if (w != null) return w; }
            foreach (var e in Entrances(z)) if (e.Key == entrance) { var w = Nearest(z.Waypoints, e.Value, out float d); return d <= MaxDistance ? w : null; }
            return null;
        }

        public static List<ZoneWaypointInfo> ArrivalWaypoints(ZoneDefinition z)
        {
            var l = new List<ZoneWaypointInfo>();
            foreach (var e in Entrances(z)) { var w = ArrivalFor(z, e.Key); if (w != null && !l.Contains(w)) l.Add(w); }
            return l;
        }

        /// <summary>Beim Betreten über einen Eingang: dessen Ankunfts-Wegmarke freischalten. Rückgabe = neu freigeschaltete Wegmarke (oder null).</summary>
        public static ZoneWaypointInfo UnlockOnEnter(ZoneDefinition z, string entrance, ICollection<string> unlocked)
        {
            var w = ArrivalFor(z, entrance);
            if (w == null || unlocked == null || unlocked.Contains(w.Id)) return null;
            unlocked.Add(w.Id); return w;
        }

        /// <summary>Eintrag im Besiegt-Bildschirm.</summary>
        public struct DefeatOption
        {
            public ZoneDefinition Zone; public ZoneWaypointInfo Waypoint; public bool Emergency, OtherZone; public float Distance;
            public string Label => (Emergency ? "Notfall-Wegmarke: " : "") + Waypoint.Name + (OtherZone ? " (" + Zone.DisplayName + ")" : "");
        }

        /// <summary>
        /// Besiegt-Optionen, nie leer: freie (entdeckte, nicht umkämpfte) Wegmarken der Zone nach Nähe; gibt es keine, die nächste
        /// Ankunfts-Wegmarke als Notfall-Wegmarke (bevorzugt nicht umkämpft, sonst trotzdem); dazu immer die entdeckten Wegmarken anderer Zonen.
        /// </summary>
        public static List<DefeatOption> DefeatOptions(ZoneDefinition current, IEnumerable<ZoneDefinition> zones, ICollection<string> unlocked, Func<Vector3, bool> contested, Vector3 player)
        {
            var list = new List<DefeatOption>();
            if (current != null)
            {
                foreach (var w in current.Waypoints)
                    if (w != null && unlocked != null && unlocked.Contains(w.Id) && (contested == null || !contested(w.Position)))
                        list.Add(new DefeatOption { Zone = current, Waypoint = w, Distance = FlatDistance(w.Position, player) });
                list.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                if (list.Count == 0)
                {
                    var arrivals = ArrivalWaypoints(current);
                    if (arrivals.Count == 0 && current.Waypoints.Count > 0) arrivals.AddRange(current.Waypoints);
                    var w = Nearest(arrivals, player, out float d, x => contested == null || !contested(x.Position));
                    if (w == null) w = Nearest(arrivals, player, out d);
                    if (w != null) list.Add(new DefeatOption { Zone = current, Waypoint = w, Emergency = true, Distance = d });
                }
            }
            if (zones != null)
                foreach (var z in zones)
                {
                    if (z == null || z == current) continue;
                    foreach (var w in z.Waypoints) if (w != null && unlocked != null && unlocked.Contains(w.Id)) list.Add(new DefeatOption { Zone = z, Waypoint = w, OtherZone = true, Distance = float.MaxValue });
                }
            return list;
        }

        /// <summary>Migration: für jede besuchte Zone (und die aktuelle) die Ankunfts-Wegmarken freischalten. Rückgabe = Anzahl neu.</summary>
        public static int Migrate(ICollection<string> unlocked, IEnumerable<ZoneDefinition> zones, IEnumerable<string> visitedZoneIds)
        {
            if (unlocked == null || zones == null || visitedZoneIds == null) return 0;
            var visited = new HashSet<string>(visitedZoneIds); int n = 0;
            foreach (var z in zones)
                if (z != null && visited.Contains(z.Id))
                    foreach (var w in ArrivalWaypoints(z)) if (!unlocked.Contains(w.Id)) { unlocked.Add(w.Id); n++; }
            return n;
        }
    }
}
