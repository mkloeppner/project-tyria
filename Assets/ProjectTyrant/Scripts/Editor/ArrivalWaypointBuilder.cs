using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Fix-Lauf F1: Builder-Validierung der Eingänge. Jeder Eingang (Spawn, Portal-Ankunft) braucht eine Wegmarke in ≤ 40 m; fehlt sie,
    /// wird eine Ankunfts-Wegmarke auf ebenem, begehbarem Boden mit Sichtlinie zum Eingang angelegt. Schreibt <see cref="ZoneDefinition.Arrivals"/>.
    /// </summary>
    public static class ArrivalWaypointBuilder
    {
        public static int LastCreated;

        public static void Ensure(ZoneDefinition z, Transform parent, string defDir, int xp)
        {
            LastCreated = 0;
            if (z == null) return;
            Physics.SyncTransforms();
            z.Arrivals = new List<ZoneArrivalInfo>();
            var crystal = Mat("WaypointCrystal", new Color(0.45f, 0.75f, 1f), 0.1f, new Color(0.3f, 0.6f, 1f) * 2.2f);
            var stone = Mat("WaypointStone", new Color(0.62f, 0.62f, 0.66f));
            foreach (var e in WaypointSafety.Entrances(z))
            {
                var near = WaypointSafety.Nearest(z.Waypoints, e.Value, out float d);
                if (near == null || d > WaypointSafety.MaxDistance - 2f)
                {
                    var pos = FindSpot(e.Value);
                    string key = e.Key == WaypointSafety.SpawnEntrance ? "ankunft" : "ankunft_" + e.Key.Substring("portal:".Length);
                    string id = "wp_" + z.Id + "_" + key.Replace(z.Id + "_", "");
                    string name = e.Key == WaypointSafety.SpawnEntrance ? "Ankunfts-Wegmarke " + z.DisplayName : "Portal-Wegmarke " + PortalTargetName(z, e.Key);
                    var def = AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(defDir + "/Zone_" + id + ".asset");
                    if (def == null) { def = ScriptableObject.CreateInstance<ZoneObjectDefinition>(); AssetDatabase.CreateAsset(def, defDir + "/Zone_" + id + ".asset"); }
                    def.Id = id; def.Kind = MapMarkerKind.Waypoint; def.DisplayName = name; def.Description = "Ankunfts-Wegmarke: wird beim Betreten über diesen Eingang automatisch freigeschaltet.";
                    def.Position = pos; def.Radius = ZoneContent.WaypointDiscoverRadius; def.Experience = xp; def.Karma = 0; def.Copper = 0; def.HeroPoints = 0; def.HeartTask = null; def.KillValue = def.TaskValue = 0f;
                    EditorUtility.SetDirty(def);
                    var go = new GameObject("Waypoint_" + id); go.transform.SetParent(parent, false); go.transform.position = pos;
                    ZoneContentBuilder.BuildMonument(go.transform, stone, crystal);
                    var wp = go.AddComponent<Waypoint>(); wp.Definition = def; wp.PromptText = "Reisen"; wp.Range = 3.5f;
                    near = new ZoneWaypointInfo { Id = id, Name = name, Position = pos };
                    z.Waypoints.Add(near); LastCreated++;
                    Debug.Log("[ProjectTyrant] Ankunfts-Wegmarke angelegt: " + z.Id + "/" + e.Key + " → " + id + " (" + (d > 1e8f ? "keine" : Mathf.RoundToInt(d) + " m") + " → " + Mathf.RoundToInt(WaypointSafety.FlatDistance(pos, e.Value)) + " m)");
                }
                z.Arrivals.Add(new ZoneArrivalInfo { Entrance = e.Key, Position = e.Value, WaypointId = near.Id });
            }
            EditorUtility.SetDirty(z);
        }

        static string PortalTargetName(ZoneDefinition z, string entrance)
        {
            var p = z.Portal(entrance.Substring("portal:".Length));
            var w = p != null ? WorldBuilder.Zone(p.TargetZone) : null;
            return w != null ? w.DisplayName : z.DisplayName;
        }

        /// <summary>Ebener Boden (Normale ≥ 0,85) mit Sichtlinie zum Eingang, 8–30 m entfernt; sonst direkt am Eingang.</summary>
        static Vector3 FindSpot(Vector3 entrance)
        {
            var e = Ground(entrance);
            for (float r = 8f; r <= 30f; r += 4f)
                for (int k = 0; k < 12; k++)
                {
                    float a = k * 30f * Mathf.Deg2Rad;
                    var c = entrance + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (!Physics.Raycast(new Vector3(c.x, c.y + 120f, c.z), Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < 0.85f || hit.collider.GetComponentInParent<Waypoint>() != null) continue;
                    var n = hit.collider.name.ToLowerInvariant(); if (n.Contains("water") || n.Contains("wasser") || n.Contains("lava")) continue;
                    if (Physics.Linecast(hit.point + Vector3.up * 1.5f, e + Vector3.up * 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;   // Sichtlinie
                    return hit.point;
                }
            return e;
        }

        static Vector3 Ground(Vector3 p) => Physics.Raycast(new Vector3(p.x, p.y + 120f, p.z), Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : p;
    }
}
