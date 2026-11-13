using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Anim;
using ProjectTyrant.Balance;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Belebung aller Zonen: (1) zusätzliche Gegnerlager bis zur Zieldichte der Gefahrenstufe (fair: Sicherheitszonen, Lagerabstand,
    /// Gruppengröße, Veteranen-Regel, Abstand zu Inhalten und Bauwerken; Neutral-Quote und Rollen setzt danach der Balance-Pass),
    /// (2) Bewohner mit Tagesroutine an Wegmarken und Herzen, (3) Städte mit Bürgern.
    /// </summary>
    public static class LivelinessBuilder
    {
        public static readonly List<string> LastReport = new List<string>();

        // ---------------------------------------------------------------- Gegner
        /// <summary>Vom Balance-Pass aufgerufen (Szene offen, Gefahrenkarte fertig). Rückgabe: hinzugefügte Spawns.</summary>
        public static int AddCamps(ZoneDefinition z, ZoneDangerMap map, ZoneRoot root, List<NpcBrain> brains, System.Random rng)
        {
            if (z == null || z.LegacyLayout || brains.Count == 0) return 0;   // Startzone: eigens abgestimmt (Stufe 0–2)
            var trash = brains.Where(b => b.Archetype.Tier == EnemyTier.Trash && b.Archetype.Prefab != null).Select(b => b.Archetype).Distinct().OrderBy(a => a.name).ToList();
            var vets = brains.Where(b => b.Archetype.Tier == EnemyTier.Veteran && b.Archetype.Prefab != null).Select(b => b.Archetype).Distinct().OrderBy(a => a.name).ToList();
            if (trash.Count == 0) return 0;
            var parent = brains[0].transform.parent;
            float half = z.Size * 0.5f, cell = 10f;
            // freie Fläche je Stufe schätzen
            var area = new Dictionary<int, float>(); var have = new Dictionary<int, int>();
            for (float x = -half; x < half; x += cell) for (float y = -half; y < half; y += cell)
            {
                var q = new Vector2(x + cell * 0.5f, y + cell * 0.5f);
                if (!map.SpawnAllowed(q) || map.IsCity(q) || !Walkable(z, q)) continue;
                int l = map.At(q); area.TryGetValue(l, out float a); area[l] = a + cell * cell;
            }
            foreach (var b in brains) { int l = map.At(b.transform.position); have.TryGetValue(l, out int h); have[l] = h + 1; }
            var blocked = Blockers(z);
            var centers = brains.Select(b => new Vector2(b.transform.position.x, b.transform.position.z)).ToList();
            int added = 0, camps = 0;
            foreach (var kv in area.OrderBy(k => k.Key))
            {
                var s = DangerTable.Default(kv.Key);
                have.TryGetValue(kv.Key, out int h);
                int missing = PopulationModel.Missing(h, PopulationModel.TargetSpawns(kv.Value, s));
                float spacing = PopulationModel.CampSpacing(s);
                int idx = 0;
                for (int tries = 0; missing > 0 && tries < 6000 && added < PopulationModel.MaxAddPerZone; tries++)
                {
                    var c = new Vector2((float)rng.NextDouble() * z.Size * 0.9f - half * 0.9f, (float)rng.NextDouble() * z.Size * 0.9f - half * 0.9f);
                    if (map.At(c) != kv.Key || !map.SpawnAllowed(c) || map.IsCity(c) || !Walkable(z, c)) continue;
                    if (centers.Any(o => (o - c).sqrMagnitude < spacing * spacing)) continue;
                    if (blocked.Any(bl => (new Vector2(bl.x, bl.y) - c).sqrMagnitude < bl.z * bl.z)) continue;
                    int size = Mathf.Min(PopulationModel.CampSize(s, idx), missing);
                    bool vet = PopulationModel.VeteranCamp(s, idx) && vets.Count > 0;
                    var pts = new List<Vector2>();
                    for (int i = 0; i < size; i++)
                    {
                        float ang = i * Mathf.PI * 2f / size + (float)rng.NextDouble();
                        var p = size == 1 ? c : c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 3.5f;
                        if (!map.SpawnAllowed(p) || !Walkable(z, p)) { pts.Clear(); break; }
                        pts.Add(p);
                    }
                    if (pts.Count == 0) continue;
                    var arch = trash[(camps + kv.Key) % trash.Count];
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var a = vet && i == 0 ? vets[camps % vets.Count] : arch;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(a.Prefab, parent);
                        go.name = a.Prefab.name + "_Belebt";
                        go.transform.position = new Vector3(pts[i].x, GroundY(pts[i]) + 0.05f, pts[i].y);
                        go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                        var nb = go.GetComponent<NpcBrain>(); nb.Archetype = a;
                        brains.Add(nb); added++; missing--;
                    }
                    centers.Add(c); camps++; idx++;
                }
            }
            LastReport.Add(z.Id + ": +" + added + " Gegner in " + camps + " Lagern");
            return added;
        }

        /// <summary>Gelände (kein Dach, kein Wasser, nicht zu steil) – nur Terrain-Chunks zählen.</summary>
        static bool Walkable(ZoneDefinition z, Vector2 p)
        {
            if (Mathf.Abs(p.x) > z.Size * 0.46f || Mathf.Abs(p.y) > z.Size * 0.46f) return false;
            if (!Hit(p, out var hit) || !hit.collider.name.StartsWith("Chunk_")) return false;
            float wl = z.Biome != null ? z.Biome.WaterLevel : 0f;
            if (hit.point.y < wl + 0.4f) return false;
            for (int k = 0; k < 4; k++)
            {
                var o = p + new Vector2(k == 0 ? 3f : k == 1 ? -3f : 0f, k == 2 ? 3f : k == 3 ? -3f : 0f);
                if (!Hit(o, out var h2) || Mathf.Abs(h2.point.y - hit.point.y) > 2.2f) return false;
            }
            return true;
        }

        static bool Hit(Vector2 p, out RaycastHit hit) => Physics.Raycast(new Vector3(p.x, 800f, p.y), Vector3.down, out hit, 1600f, 1, QueryTriggerInteraction.Ignore);
        static float GroundY(Vector2 p) => Hit(p, out var h) ? h.point.y : 0f;

        /// <summary>Sperrkreise (x, z, r): Inhalte, Herzen, Wahrzeichen, Hürden, Aufstiege, Fußwege, Ereignisse.</summary>
        static List<Vector3> Blockers(ZoneDefinition z)
        {
            var l = new List<Vector3>();
            var lay = BiomeZoneContent.For(z.Id);
            if (lay != null)
            {
                foreach (var s in lay.AllSpots()) l.Add(new Vector3(s.Pos.x, s.Pos.y, PopulationModel.ContentClearance));
                foreach (var h in lay.Hearts) { l.Add(new Vector3(h.NpcPos.x, h.NpcPos.y, PopulationModel.ContentClearance)); }
            }
            foreach (var p in z.Portals) l.Add(new Vector3(p.Position.x, p.Position.z, 40f));
            l.Add(new Vector3(z.DefaultSpawn.x, z.DefaultSpawn.z, 50f));
            var plan = z.WorldPlan;
            if (plan != null)
            {
                foreach (var m in plan.Landmarks) l.Add(new Vector3(m.Position.x, m.Position.z, m.Footprint * 0.6f + 12f));
                foreach (var h in plan.Hazards) l.Add(new Vector3(h.Center.x, h.Center.z, Mathf.Max(h.Size.x, h.Size.y) * 0.6f + 6f));
                foreach (var c in plan.Climbs) { l.Add(new Vector3(c.Base.x, c.Base.z, 25f)); l.Add(new Vector3(c.LaunchTower.x, c.LaunchTower.z, 15f)); }
                foreach (var s in plan.Secrets) l.Add(new Vector3(s.Position.x, s.Position.z, 10f));
                foreach (var f in plan.FootLinks) foreach (var p in f.Points) l.Add(new Vector3(p.x, p.z, 10f));
            }
            return l;
        }

        // ---------------------------------------------------------------- Bewohner
        static readonly string[] Acts = { "Talk", "Work", "Idle2", "Sit", "Interact", "Cheer" };
        static System.Random _rnd;
        static float R(float a, float b) => a + (float)_rnd.NextDouble() * (b - a);

        /// <summary>Vom Zonen-Bau aufgerufen: Reisende an Wegmarken, Helfer an Herzen, Bürger in Städten.</summary>
        public static int AddResidents(ZoneDefinition z, Transform zoneRoot, BiomeTerrain t)
        {
            var lay = BiomeZoneContent.For(z.Id); if (lay == null) return 0;
            if (zoneRoot.GetComponentsInChildren<VillagerRoutine>(true).Length >= 30) return 0;   // Silbermund hat seine Bürger schon
            _rnd = new System.Random(z.Seed + 4711);
            var root = Child(zoneRoot, "PT_Bewohner", Vector3.zero).transform;
            var spec = RegionZones.Get(z.Id);
            bool hub = spec != null && spec.Hub;
            int n = 0;
            bool Dry(Vector2 p) => t.Height(p.x, p.y) > t.WaterLevel + 0.4f;
            Vector3 G(Vector2 p) => new Vector3(p.x, t.Height(p.x, p.y), p.y);
            void Resident(Vector2 a, Vector2 b, string id, Color robe, string actA, string actB, float speed = 1.4f)
            {
                if (!Dry(a) || !Dry(b)) return;
                var npc = Child(root, "Bewohner_" + id, G(a)).transform;
                CharacterRigs.Npc(npc, robe, "res_" + z.Id + "_" + id, n % 4 == 0);
                var rt = npc.gameObject.AddComponent<VillagerRoutine>();
                rt.Stops = new[]
                {
                    new VillagerRoutine.Stop { Position = G(a), Yaw = R(0f, 360f), Activity = actA, Duration = R(8f, 18f) },
                    new VillagerRoutine.Stop { Position = G(b), Yaw = R(0f, 360f), Activity = actB, Duration = R(8f, 18f) },
                };
                rt.WalkSpeed = speed; n++;
            }
            Color Robe(int i) => Color.HSVToRGB((i * 0.137f + z.Seed * 0.001f) % 1f, R(0.3f, 0.6f), R(0.45f, 0.8f));
            if (hub)
            {
                // Stadt: 40 Bürger zwischen Plätzen (Wegmarken, Sehenswürdigkeiten, Herzen)
                var spots = lay.Waypoints.Select(s => s.Pos).Concat(lay.Pois.Select(s => s.Pos)).Concat(lay.Hearts.Select(h => h.Center)).ToList();
                for (int i = 0; i < 40 && spots.Count > 1; i++)
                {
                    var a = spots[i % spots.Count] + new Vector2(R(-12f, 12f), R(-12f, 12f));
                    var b = spots[(i * 3 + 1) % spots.Count] + new Vector2(R(-12f, 12f), R(-12f, 12f));
                    Resident(a, b, "buerger_" + i, Robe(i), Acts[i % Acts.Length], Acts[(i + 2) % Acts.Length], R(1.2f, 1.7f));
                }
            }
            else
            {
                // Reisende an Wegmarken (2), Helfer an Herzen (3)
                int i = 0;
                foreach (var w in lay.Waypoints)
                    for (int k = 0; k < 2; k++, i++)
                    {
                        var a = w.Pos + new Vector2(R(-7f, 7f), R(-7f, 7f)); var b = w.Pos + new Vector2(R(-14f, 14f), R(-14f, 14f));
                        Resident(a, b, "reisender_" + i, Robe(i), k == 0 ? "Talk" : "Sit", "Idle2");
                    }
                foreach (var h in lay.Hearts)
                    for (int k = 0; k < 3; k++, i++)
                    {
                        var a = h.Center + new Vector2(R(-14f, 14f), R(-14f, 14f)); var b = h.NpcPos + new Vector2(R(-5f, 5f), R(-5f, 5f));
                        Resident(a, b, "helfer_" + i, Robe(i), "Work", k == 2 ? "Talk" : "Interact", R(1.3f, 1.6f));
                    }
            }
            LastReport.Add(z.Id + ": +" + n + " Bewohner" + (hub ? " (Stadt)" : ""));
            return n;
        }
    }
}
