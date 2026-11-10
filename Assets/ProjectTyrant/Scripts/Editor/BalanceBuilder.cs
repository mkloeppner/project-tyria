using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Balance;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Balance B1: Profile 0–5 als Assets, Archetyp-Rollen, Gefahrenkarte je Zone (in ZoneDefinition gebacken), Spawn-Durchgang je Zonen-Szene
    /// (Umverteilung, Gruppen, Neutral-Quote, Respawn, neutrale Tiere in der Startzone), Event-Stufen und Validator. Verstöße brechen den Rebuild.
    /// </summary>
    public static class BalanceBuilder
    {
        public const string Dir = DataDir + "/Balance";
        public static readonly List<string> LastReport = new List<string>();
        public static readonly List<string> LastErrors = new List<string>();

        [MenuItem("Project Tyrant/Balance/Profile und Archetypen")]
        public static BalanceConfig BuildData()
        {
            EnsureFolder(Dir);
            var cfgPath = Dir + "/BalanceConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<BalanceConfig>(cfgPath);
            if (cfg == null) { cfg = ScriptableObject.CreateInstance<BalanceConfig>(); AssetDatabase.CreateAsset(cfg, cfgPath); }
            cfg.Levels = new DangerProfile[6];
            for (int i = 0; i <= 5; i++)
            {
                var p = Dir + "/DangerProfile_" + i + ".asset";
                var a = AssetDatabase.LoadAssetAtPath<DangerProfile>(p);
                if (a == null) { a = ScriptableObject.CreateInstance<DangerProfile>(); AssetDatabase.CreateAsset(a, p); }
                a.Settings = DangerTable.Default(i);
                EditorUtility.SetDirty(a);
                cfg.Levels[i] = a;
            }
            cfg.Player = new ExpectedPlayer();
            EditorUtility.SetDirty(cfg);
            var perr = BalanceValidator.ValidateProfiles(cfg.Levels.Select(l => l.Settings).ToList());
            if (perr.Count > 0) throw new System.Exception("[ProjectTyrant] Balance: Profile nicht monoton: " + string.Join(", ", perr));
            ApplyArchetypes();
            AssetDatabase.SaveAssets();
            return cfg;
        }

        static readonly string[] NeutralBeasts = { "Wild_Eber", "Wild_Hirsch", "Moor_Spucker", "Glut_Kriecher", "Wald_Wildtier", "Tiefe_Kristallkrabbler", "Himmel_Sturmgreif", "Duene_Sandwurm" };

        /// <summary>Rollen und Ausnahmen je Archetyp (Weltbosse, Metas, Story-Bosse behalten Handwerte).</summary>
        public static void ApplyArchetypes()
        {
            foreach (var g in AssetDatabase.FindAssets("t:EnemyArchetype", new[] { DataDir + "/Enemies" }))
            {
                var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(AssetDatabase.GUIDToAssetPath(g));
                if (a == null) continue;
                string id = a.name.StartsWith("Enemy_") ? a.name.Substring(6) : a.name;
                bool boss = id.StartsWith("WorldBoss_") || id.StartsWith("Meta_") || id.StartsWith("Story_");
                a.CurveExempt = boss || (id.StartsWith("Champion_") && id != "Champion_Grolmok") || a.Team != Team.Enemy || a.MaxHealth > 0f;
                a.PursuitExempt = boss || a.Team != Team.Enemy;
                a.Role = a.Behavior == NpcBehavior.Ranged || a.Behavior == NpcBehavior.Lob || a.Behavior == NpcBehavior.Summoner ? EnemyRole.Ranged
                       : id.Contains("Jaeger") || id.Contains("Hunter") ? EnemyRole.Hunter
                       : a.Behavior == NpcBehavior.Ambush ? EnemyRole.Lurker
                       : a.Behavior == NpcBehavior.PackHowl ? EnemyRole.Caller : EnemyRole.Standard;
                a.NeutralCreature = System.Array.IndexOf(NeutralBeasts, id) >= 0;
                if (id == "Champion_Grolmok") { a.FixedLevel = 13; a.TelegraphDelay = Mathf.Max(1.2f, a.TelegraphDelay); a.Defiance = Mathf.Max(a.Defiance, 300f); }   // Stufe-2-Finale für Level 12–15
                EditorUtility.SetDirty(a);
            }
        }

        static float Ground(ZoneDefinition z, Vector2 p)
        {
            if (z.LegacyLayout) return ZoneLayout.Height(p.x, p.y);
            if (Physics.Raycast(new Vector3(p.x, 500f, p.y), Vector3.down, out var hit, 1000f, 1, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return 0f;
        }

        static bool Land(ZoneDefinition z, Vector2 p)
        {
            if (z.LegacyLayout) return ZoneLayout.IsFreeForScatter(p.x, p.y, 2f) && ZoneLayout.Height(p.x, p.y) > ZoneLayout.WaterLevel + 0.4f;
            return Mathf.Abs(p.x) < z.Size * 0.47f && Mathf.Abs(p.y) < z.Size * 0.47f && Ground(z, p) > 0.2f;
        }

        /// <summary>Alle Zonen: Karte backen, Szene durchgehen, validieren. Rückgabe = Fehlerzahl.</summary>
        [MenuItem("Project Tyrant/Balance/Alle Zonen anwenden")]
        public static int ApplyAll()
        {
            LastReport.Clear(); LastErrors.Clear();
            var cfg = BuildData();
            // Ids zuerst sammeln: OpenScene(Single) entlädt unbenutzte Assets (Welt-/Zonen-Definitionen würden ungültig)
            var ids = new List<string>();
            foreach (var z in WorldBuilder.LoadWorld().Zones) if (z != null) ids.Add(z.Id);
            foreach (var id in ids) ApplyZone(WorldBuilder.Zone(id), cfg);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Balance B1: " + string.Join(" · ", LastReport));
            if (LastErrors.Count > 0) Debug.LogError("[ProjectTyrant] Balance-Validator: " + LastErrors.Count + " Verstöße\n" + string.Join("\n", LastErrors));
            return LastErrors.Count;
        }

        public static void ApplyZone(ZoneDefinition z, BalanceConfig cfg = null)
        {
            cfg = cfg ?? BuildData();
            string path = z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
            string zid = z.Id;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            z = WorldBuilder.Zone(zid);                                        // nach dem Szenenwechsel neu laden
            cfg = AssetDatabase.LoadAssetAtPath<BalanceConfig>(Dir + "/BalanceConfig.asset") ?? cfg;
            var root = Object.FindAnyObjectByType<ZoneRoot>();
            if (root == null) return;
            var map = BalanceWorld.BuildMap(z);
            z.DangerMap = map; EditorUtility.SetDirty(z);
            root.BalanceConfig = cfg; EditorUtility.SetDirty(root);
            var rng = new System.Random(z.Seed * 977 + 13);

            // ---- Spawns sammeln ----
            // ganze Szene (Startzone: NPCs liegen nicht unter der ZoneRoot)
            var brains = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NpcBrain>(true)).Where(b => b.Archetype != null && b.Archetype.Team == Team.Enemy && !b.Archetype.PursuitExempt).ToList();
            int moved = 0, added = 0;
            // Startzone: höchstens 3 Feinde in Stufe 0 (Rand des ersten Herzens), Rest nach Stufe 1 umverteilen
            if (map.IsStartZone)
            {
                var hostile0 = brains.Where(b => map.At(b.transform.position) == 0 && !b.Archetype.NeutralCreature).OrderBy(b => Vector3.Distance(b.transform.position, new Vector3(-8, 0, -60))).ToList();
                for (int i = 3; i < hostile0.Count; i++) if (Relocate(z, map, hostile0[i].transform, 1)) moved++;
            }
            foreach (var b in brains)
            {
                var p = new Vector2(b.transform.position.x, b.transform.position.z);
                bool vet = b.Archetype.Tier >= EnemyTier.Veteran;
                if (!map.SpawnAllowed(p) || (vet && map.At(p) == 0)) if (Relocate(z, map, b.transform, vet ? 1 : 0)) moved++;
            }
            // Startzone: neutrale Tiere je Stufe (80/60/40 %)
            if (map.IsStartZone)
            {
                var boar = DataBuilder.Archetype("Wild_Eber"); var stag = DataBuilder.Archetype("Wild_Hirsch");
                var parent = brains.Count > 0 ? brains[0].transform.parent : root.transform;
                for (int lvl = 0; lvl <= 2; lvl++)
                {
                    int hostile = brains.Count(b => map.At(b.transform.position) == lvl && !b.Archetype.NeutralCreature);
                    float share = DangerTable.Default(lvl).NeutralShare;
                    int want = Mathf.CeilToInt(hostile * share / Mathf.Max(0.05f, 1f - share));
                    int have = brains.Count(b => map.At(b.transform.position) == lvl && b.Archetype.NeutralCreature);
                    for (int tries = 0; have < want && tries < 4000; tries++)
                    {
                        var q = new Vector2((float)rng.NextDouble() * 180f - 90f, (float)rng.NextDouble() * 180f - 90f);
                        if (map.At(q) != lvl || !map.SpawnAllowed(q) || !Land(z, q)) continue;
                        if (brains.Any(o => Vector2.Distance(new Vector2(o.transform.position.x, o.transform.position.z), q) < 7f)) continue;
                        var arch = (have % 3 == 2) ? stag : boar;
                        if (arch == null || arch.Prefab == null) break;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(arch.Prefab, parent);
                        go.transform.position = new Vector3(q.x, Ground(z, q) + 0.05f, q.y);
                        go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                        var nb = go.GetComponent<NpcBrain>(); nb.Archetype = arch;
                        brains.Add(nb); have++; added++;
                    }
                }
            }
            // Startzone: Wachen-Patrouillen auf den Wegen (Stufe 0–2) – greifen Verfolger an, beleben wieder
            if (map.IsStartZone)
            {
                foreach (var old in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GuardPatrol>(true)).ToList()) Object.DestroyImmediate(old.gameObject);
                var guard = DataBuilder.Archetype("Ally_Dornwacht_Waechter");
                var parent = brains.Count > 0 ? brains[0].transform.parent : root.transform;
                if (guard != null && guard.Prefab != null)
                    foreach (int pi in new[] { 0, 3 })
                    {
                        var route = ZoneLayout.Paths[pi];
                        var pts = route.Select(v => new Vector3(v.x, ZoneLayout.Height(v.x, v.y) + 0.05f, v.y)).ToArray();
                        for (int k = 0; k < 2; k++)
                        {
                            var at = Vector3.Lerp(pts[1], pts[2], 0.3f + 0.15f * k) + new Vector3(k * 1.5f, 0, 0);
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(guard.Prefab, parent);
                            go.name = "Patrouille_" + pi + "_" + k;
                            go.transform.position = at;
                            var gb = go.GetComponent<NpcBrain>(); gb.Archetype = guard; gb.DestroyOnDeath = false; gb.WorldSpawn = true;
                            var gp = go.AddComponent<GuardPatrol>(); gp.Points = pts;
                            added++;
                        }
                    }
            }
            // ---- Gruppen (Lager) ----
            int gi = 0;
            var grouped = new HashSet<NpcBrain>();
            foreach (var b in brains)
            {
                if (grouped.Contains(b)) continue;
                string id = z.Id + "_g" + (gi++);
                var stack = new Stack<NpcBrain>(); stack.Push(b); grouped.Add(b);
                while (stack.Count > 0)
                {
                    var c = stack.Pop(); c.SpawnGroup = id;
                    foreach (var o in brains) if (!grouped.Contains(o) && Vector3.Distance(o.transform.position, c.transform.position) < 8f) { grouped.Add(o); stack.Push(o); }
                }
            }
            // ---- Neutral-Quote je Stufe (ganze Gruppen; Tiere zuerst) ----
            foreach (var b in brains) b.Neutral = false;
            for (int lvl = 0; lvl <= 5; lvl++)
            {
                var inLvl = brains.Where(b => map.At(b.transform.position) == lvl).ToList();
                if (inLvl.Count == 0) continue;
                float share = DangerTable.Default(lvl).NeutralShare;
                int want = Mathf.RoundToInt(inLvl.Count * share), have = 0;
                var groups = inLvl.GroupBy(b => b.SpawnGroup).OrderByDescending(g => g.Count(b => b.Archetype.NeutralCreature)).ThenBy(g => g.Key).ToList();
                foreach (var g in groups)
                {
                    if (have >= want) break;
                    if (have + g.Count() > want + 1) continue;   // nicht über die Quote hinaus (Tiere zuerst, Humanoide nur bei Bedarf)
                    foreach (var b in g) { b.Neutral = true; have++; }
                }
                // Rest nur, wenn die Quote dadurch näher am Ziel liegt (ganze Lager)
                if (have < want) foreach (var g in groups)
                {
                    if (have >= want) break; if (g.First().Neutral) continue;
                    float cur = Mathf.Abs(have / (float)inLvl.Count - share), next = Mathf.Abs((have + g.Count()) / (float)inLvl.Count - share);
                    if (next >= cur) continue;
                    foreach (var b in g) { b.Neutral = true; have++; }
                }
            }
            // ---- Spätspiel-Rollen (Stufe 4–5): Lauerer und Rufer je Lager, Jäger in jedem zweiten Lager ----
            int roles = 0;
            foreach (var old in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RoleBehaviour>(true)).ToList()) Object.DestroyImmediate(old);
            foreach (var b in brains) b.RoleOverride = EnemyRole.Standard;
            int gix = 0;
            foreach (var g in brains.Where(b => !b.Neutral).GroupBy(b => b.SpawnGroup).OrderBy(g => g.Key))
            {
                var list = g.Where(b => b.Archetype.Tier == EnemyTier.Trash).ToList();
                int lvl = map.At(g.First().transform.position);
                if (lvl < 4 || list.Count == 0) continue;
                var center = new Vector3(list.Average(b => b.transform.position.x), list.Average(b => b.transform.position.y), list.Average(b => b.transform.position.z));
                void Give(NpcBrain b, EnemyRole r) { b.RoleOverride = r; var rb = b.gameObject.AddComponent<RoleBehaviour>(); rb.CampCenter = center; roles++; }
                if (list.Count >= 2) Give(list[0], EnemyRole.Lurker);
                if (lvl >= 5 && list.Count >= 3) Give(list[1], EnemyRole.Caller);
                if (gix++ % 2 == 0) Give(list[list.Count - 1], EnemyRole.Hunter);
            }
            // ---- Respawn ----
            foreach (var b in brains) { b.WorldSpawn = true; b.DestroyOnDeath = false; EditorUtility.SetDirty(b); EditorUtility.SetDirty(b.transform); }

            // ---- Event-Stufen ----
            var events = new List<EventInfo>();
            if (root.Events != null) foreach (var e in root.Events) if (e != null) { AssignTier(z, map, e, root.Events); events.Add(new EventInfo { Id = e.Id, Center = new Vector2(e.Center.x, e.Center.z), Tier = e.Tier, Meta = e.Tier == EventTier.Meta, MinTimer = MinTimer(e) }); }

            // ---- Validator ----
            var spawns = brains.Select(b => new SpawnInfo { Id = b.name + "@" + b.transform.position.ToString("F0"), Pos = new Vector2(b.transform.position.x, b.transform.position.z), Tier = b.Archetype.Tier, Neutral = b.Neutral }).ToList();
            var errs = BalanceValidator.Validate(map, spawns, events);
            LastErrors.AddRange(errs);
            int[] perLvl = new int[6]; foreach (var b in brains) perLvl[map.At(b.transform.position)]++;
            LastReport.Add(z.Id + ": " + brains.Count + " Spawns (Stufen " + string.Join("/", perLvl) + "), " + brains.Count(b => b.Neutral) + " neutral, " + moved + " verschoben, " + added + " Tiere/Wachen, " + roles + " Rollen, " + events.Count + " Events, " + errs.Count + " Verstöße");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static float MinTimer(EventDefinition e)
        {
            float m = 0f;
            foreach (var t in e.Triggers) if (t.Type == EventTriggerType.Timer) m = m <= 0f ? t.Delay : Mathf.Min(m, t.Delay);
            return m;
        }

        /// <summary>Spawn nach außen schieben, bis er erlaubt ist (und mindestens Stufe <paramref name="minLevel"/> hat).</summary>
        static bool Relocate(ZoneDefinition z, ZoneDangerMap map, Transform t, int minLevel)
        {
            var p = new Vector2(t.position.x, t.position.z);
            Vector2 c = map.Anchors.Count > 0 ? map.Anchors[0] : Vector2.zero;
            var dir = (p - c).sqrMagnitude > 0.01f ? (p - c).normalized : Vector2.right;
            for (int ring = 0; ring < 40; ring++)
            {
                for (int k = 0; k < 8; k++)
                {
                    float ang = k * 45f * Mathf.Deg2Rad * (ring % 2 == 0 ? 1 : -1);
                    var d = new Vector2(dir.x * Mathf.Cos(ang) - dir.y * Mathf.Sin(ang), dir.x * Mathf.Sin(ang) + dir.y * Mathf.Cos(ang));
                    var q = p + d * (4f + ring * 4f);
                    if (!map.SpawnAllowed(q) || map.At(q) < minLevel || map.At(q) > Mathf.Max(minLevel, map.At(p)) + 1 || !Land(z, q)) continue;
                    t.position = new Vector3(q.x, Ground(z, q) + 0.05f, q.y);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Event-Stufe nach Gefahrenstufe, Entfernung und Auftrag; Werte der Stufen 0–1 (Timer, Ziele, Auslöser, Welt-Folgen).</summary>
        static void AssignTier(ZoneDefinition z, ZoneDangerMap map, EventDefinition e, EventDefinition[] all)
        {
            int danger = map.At(e.Center);
            bool meta = e.Objective == EventObjective.WorldBoss || !string.IsNullOrEmpty(e.MetaId);
            EventTier t;
            if (meta) t = EventTier.Meta;
            else if (map.IsStartZone)
            {
                switch (e.Id)
                {
                    case "dornwacht_defend": t = EventTier.Easy; break;                       // Stufe-1-Event mit Wachen und 2 Wellen
                    case "caravan": case "supplies": case "sonnfurt_defense": t = EventTier.Intro; break;
                    case "alpha": t = EventTier.Easy; break;
                    default: t = (EventTier)Mathf.Min((int)EventTierRules.StartZoneMax(map.BandAt(e.Center)) + (danger >= 2 ? 1 : 0), 2); break;
                }
            }
            else
            {
                t = EventTierRules.ForDanger(danger, false);
                // erstes Event auf dem Weg vom Eingang: eine Stufe leichter (ab Nebelmoor: Stufe 1 am Eingang)
                var first = all.Where(x => x != null && x.Objective != EventObjective.WorldBoss && string.IsNullOrEmpty(x.MetaId) && !x.IsChainMember)
                               .OrderBy(x => Vector2.Distance(new Vector2(x.Center.x, x.Center.z), new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z))).FirstOrDefault();
                if (first == e) t = (EventTier)Mathf.Max(1, (int)EventTierRules.ForDanger(Mathf.Max(0, danger - 1), false));
            }
            if (!EventTierRules.AllowedIn(t, danger) && !meta) t = EventTierRules.ForDanger(danger, false);
            e.Tier = t; e.TierSet = true;
            var v = EventTierRules.Values(t);
            if (t <= EventTier.Easy)
            {
                e.Duration = Mathf.Max(e.Duration, v.MinTimer);
                if (e.Objective != EventObjective.Boss && e.Objective != EventObjective.Escort && e.Objective != EventObjective.Capture)
                {
                    // idempotent: Builder setzen TargetCount bei jedem Bau zurück → Basis neu merken, sonst gemerkte Basis nutzen
                    int applied = Mathf.Max(1, Mathf.RoundToInt(e.BalanceBaseTargets * v.TargetFactor));
                    if (e.BalanceBaseTargets <= 0 || e.TargetCount != applied) e.BalanceBaseTargets = e.TargetCount;
                    e.TargetCount = Mathf.Max(1, Mathf.RoundToInt(e.BalanceBaseTargets * v.TargetFactor));
                }
                e.WaveSize = Mathf.Min(e.WaveSize, t == EventTier.Intro ? 3 : 4);
                e.Triggers.RemoveAll(tr => tr.Type == EventTriggerType.Timer && tr.Delay < EventTierRules.MinAutoTimer);
                if (!e.Triggers.Exists(tr => tr.Type != EventTriggerType.Timer) && !e.IsChainMember) e.Triggers.Add(new EventTrigger { Type = EventTriggerType.Proximity, Delay = 5f, ProximityFactor = 0.75f });
                e.Cooldown = Mathf.Max(e.Cooldown, v.RestartAfterFail);
                if (t == EventTier.Intro) { e.SetFlagOnFail = null; e.FailNext = null; }
                if (e.Id == "dornwacht_defend") { e.SetFlagOnFail = null; e.WaveInterval = (e.Duration - 12f) / 2f; e.WaveSize = 3; }   // 2 Wellen, kein „Dornwacht gefallen“ mehr bei Fehlschlag
            }
            // Meta-Zustand „Dornwacht gefallen“ erst, wenn auch das Zurückerobern (Stufe 2) scheitert
            if (e.Id == "dornwacht_recapture") { e.SetFlagOnFail = EventDirector.FlagDornwachtFallen; e.FlagDuration = 600f; }
            EditorUtility.SetDirty(e);
        }
    }
}
