using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 4 – Welt-Fundament: Biom-Profile, Zonen-Definitionen (Portal-Kette), Welt-Definition, Core-Szene,
    /// Build-Einstellungen und das neue „Rebuild Everything“ (Daten → Startzone → Biom-Zonen → Core).
    /// </summary>
    public static partial class WorldBuilder
    {
        public const string ZoneDataDir = DataDir + "/Zones";
        public const string WorldPath = ZoneDataDir + "/PT_World.asset";
        public const string CorePath = Root + "/Scenes/PT_Core.unity";

        public static WorldDefinition LoadWorld() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(WorldPath);
        public static ZoneDefinition Zone(string id) => AssetDatabase.LoadAssetAtPath<ZoneDefinition>(ZoneDataDir + "/Zone_" + id + ".asset");

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        // ---------------- Biome ----------------
        static BiomeProfile Biome(string id, string name, System.Action<BiomeProfile> cfg)
        {
            var b = Asset<BiomeProfile>(ZoneDataDir + "/Biome_" + id + ".asset");
            var fresh = ScriptableObject.CreateInstance<BiomeProfile>();
            EditorUtility.CopySerialized(fresh, b);   // Standardwerte → deterministisch
            Object.DestroyImmediate(fresh);
            b.name = "Biome_" + id;
            b.Id = id; b.DisplayName = name;
            cfg(b);
            EditorUtility.SetDirty(b);
            return b;
        }

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static void BuildBiomes()
        {
            EnsureFolder(ZoneDataDir);
            Biome("grasland", "Grasland", b => { b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Meadow; });
            Biome("moor", "Moor", b =>
            {
                b.Amplitude = 5f; b.Frequency = 0.0055f; b.Octaves = 4; b.CanyonDepth = 3.2f; b.CanyonFrequency = 0.0038f;
                b.BaseHeight = 2.2f; b.WaterLevel = 0.9f; b.EdgeMountainHeight = 32f;
                b.GroundLow = C("#4a5a34"); b.GroundMid = C("#5d6b3c"); b.GroundHigh = C("#6f7351"); b.Cliff = C("#4d4a3f"); b.Shore = C("#5b5034"); b.Water = new Color(0.22f, 0.33f, 0.26f, 0.9f);
                b.FogColor = C("#8a9a86"); b.FogDensity = 0.0075f;
                b.SkyTop = C("#5f7a80"); b.SkyHorizon = C("#a9b7a4"); b.SkyBottom = C("#5c6650");
                b.SunColor = C("#e8e2c4"); b.SunIntensity = 0.95f; b.Saturation = -8f;
                b.SplitShadows = C("#4a6458"); b.SplitHighlights = C("#9c9a72");
                b.TreeDensity = 0.55f; b.RockDensity = 0.2f; b.TreeColor = C("#4c6230"); b.TrunkColor = C("#3c3022");
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Swamp;
            });
            Biome("gebirge", "Gebirge", b =>
            {
                b.Amplitude = 34f; b.Frequency = 0.0032f; b.Octaves = 5; b.Terrace = 7f;
                b.BaseHeight = 18f; b.WaterLevel = -100f; b.EdgeMountainHeight = 70f;
                b.GroundLow = C("#9fb0bf"); b.GroundMid = C("#b8c6d2"); b.GroundHigh = C("#d3dde6"); b.Cliff = C("#5d6570"); b.Shore = C("#8a939a");
                b.FogColor = C("#b7c7d8"); b.FogDensity = 0.0045f;
                b.SkyTop = C("#4f7fbf"); b.SkyHorizon = C("#d4e1ee"); b.SkyBottom = C("#a2b2c0");
                b.SunColor = C("#fff3e4"); b.SunIntensity = 1.05f; b.Saturation = 4f;
                b.SplitShadows = C("#5a6f96"); b.SplitHighlights = C("#c9c4b4");
                b.TreeDensity = 0.35f; b.RockDensity = 0.55f; b.TreeColor = C("#2f4c3a"); b.TrunkColor = C("#4a3a2c"); b.Conifers = true;
                b.Weather = WeatherKind.Snow; b.Ambient = AmbientKind.Wind;
            });
            Biome("asche", "Aschewüste", b =>
            {
                b.Amplitude = 12f; b.Frequency = 0.004f; b.Octaves = 4; b.Terrace = 3.5f; b.CanyonDepth = 9f; b.CanyonFrequency = 0.0048f;
                b.BaseHeight = 6f; b.WaterLevel = 0.4f; b.EdgeMountainHeight = 45f;
                b.GroundLow = C("#3b3431"); b.GroundMid = C("#57504a"); b.GroundHigh = C("#776d64"); b.Cliff = C("#2c2524"); b.Shore = C("#7a3a1c"); b.Water = new Color(1f, 0.42f, 0.12f, 1f);
                b.FogColor = C("#6a4a3e"); b.FogDensity = 0.0055f;
                b.SkyTop = C("#3d2b2e"); b.SkyHorizon = C("#b0654a"); b.SkyBottom = C("#4a3530");
                b.SunColor = C("#ffb27a"); b.SunIntensity = 1.05f; b.Saturation = -4f;
                b.SplitShadows = C("#5a3a44"); b.SplitHighlights = C("#c08a5a");
                b.TreeDensity = 0.12f; b.RockDensity = 0.6f; b.TreeColor = C("#3a3230"); b.TrunkColor = C("#2a2220"); b.DeadTrees = true;
                b.Weather = WeatherKind.AshWind; b.Ambient = AmbientKind.Embers;
            });
            BuildBiomesLauf6();
            BuildBiomesLauf11();   // Lauf 11
            AssetDatabase.SaveAssets();
        }

        static BiomeProfile LoadBiome(string id) => AssetDatabase.LoadAssetAtPath<BiomeProfile>(ZoneDataDir + "/Biome_" + id + ".asset");

        // ---------------- Zonen ----------------
        static float YawToCenter(Vector3 p) => Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;

        /// <summary>Freier, flacher, trockener Platz in der Startzone für das Portal (deterministische Rastersuche am Nordrand).</summary>
        public static Vector3 FindStartZonePortalSpot()
        {
            Vector3 best = new Vector3(0, 0, 75); float bestScore = float.MaxValue;
            for (float x = -40f; x <= 40f; x += 2f)
                for (float z = 62f; z <= 80f; z += 2f)
                {
                    float h = ZoneLayout.Height(x, z);
                    if (h < ZoneLayout.WaterLevel + 0.5f) continue;
                    if (!ZoneLayout.IsFreeForScatter(x, z, 7f)) continue;
                    float slope = 0f;
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        slope = Mathf.Max(slope, Mathf.Abs(ZoneLayout.Height(x + Mathf.Cos(a) * 4f, z + Mathf.Sin(a) * 4f) - h));
                    }
                    if (slope > 0.9f) continue;
                    float score = slope * 10f + ZoneLayout.DistanceToPaths(new Vector2(x, z)) * 0.3f + Mathf.Abs(x) * 0.05f;
                    if (score < bestScore) { bestScore = score; best = new Vector3(x, h, z); }
                }
            return best;
        }

        static ZoneDefinition Def(string id, string name, string desc, int seed, float size, int lmin, int lmax, string biome, Rect rect, Color col, Vector3 spawn)
        {
            var z = Asset<ZoneDefinition>(ZoneDataDir + "/Zone_" + id + ".asset");
            z.Id = id; z.DisplayName = name; z.Description = desc; z.SceneName = "PT_Zone_" + char.ToUpperInvariant(id[0]) + id.Substring(1);
            z.Seed = seed; z.Size = size; z.LevelMin = lmin; z.LevelMax = lmax; z.Biome = LoadBiome(biome); z.LegacyLayout = false;
            z.ContinentRect = rect; z.ContinentColor = col; z.DefaultSpawn = spawn;
            z.Portals = new List<PortalDef>(); z.Waypoints = new List<ZoneWaypointInfo>(); z.Tips = new List<string>();
            if (z.CompletionIds == null) z.CompletionIds = new List<string>();
            return z;
        }

        static PortalDef P(string id, Vector3 pos, string targetZone, string targetPortal) =>
            new PortalDef { Id = id, Position = pos, Yaw = YawToCenter(pos), TargetZone = targetZone, TargetPortal = targetPortal };

        public static void BuildZones()
        {
            BuildBiomes();
            // Kontinent: Sonnfurt im Südwesten, Nebelmoor östlich, Frostkamm nördlich davon, Glutöde im Nordosten
            var sonn = Def("sonnfurt", ZoneLayout.WildernessName, "Grüne Grenzlande um den Weiler Sonnfurt.", 4242, ZoneLayout.Size, 1, 15, "grasland",
                new Rect(0.06f, 0.08f, 0.2f, 0.2f), C("#7fa65a"), new Vector3(-56f, 0f, -52f));
            sonn.LegacyLayout = true;
            var sonnPortal = FindStartZonePortalSpot();
            sonn.Portals.Add(P("sonnfurt_nebelmoor", sonnPortal, "nebelmoor", "nebelmoor_sonnfurt"));
            sonn.Waypoints.Add(new ZoneWaypointInfo { Id = "wp_sonnfurt", Name = "Wegmarke Sonnfurt", Position = new Vector3(-56f, 0f, -52f) });
            sonn.Tips.AddRange(new[] { "Herzen zeigen dir, wo Bewohner Hilfe brauchen – sie bringen Karma und Ruf.", "Das Portal am Nordrand führt ins Nebelmoor (Stufe 15–25)." });

            var moor = Def("nebelmoor", "Nebelmoor", "Versunkene Kanäle, Nebelschwaden und Torfinseln.", 15025, 1000f, 15, 25, "moor",
                new Rect(0.28f, 0.05f, 0.3f, 0.3f), C("#5f7a52"), new Vector3(0f, 0f, -380f));
            moor.Portals.Add(P("nebelmoor_sonnfurt", new Vector3(-30f, 0f, -420f), "sonnfurt", "sonnfurt_nebelmoor"));
            moor.Portals.Add(P("nebelmoor_frostkamm", new Vector3(20f, 0f, 420f), "frostkamm", "frostkamm_nebelmoor"));
            moor.Tips.AddRange(new[] { "Im Nebelmoor sind Kanäle tief – mit dem Skimmer gleitest du über Wasser.", "Giftnebel schwächt dich, bis du die Umwelt-Beherrschung gelernt hast." });

            var frost = Def("frostkamm", "Frostkamm", "Verschneite Terrassen und Gipfel – Heimat des Frostschlunds.", 25030, 1100f, 25, 35, "gebirge",
                new Rect(0.3f, 0.4f, 0.32f, 0.32f), C("#b9cbd8"), new Vector3(0f, 0f, -440f));
            frost.Portals.Add(P("frostkamm_nebelmoor", new Vector3(-20f, 0f, -470f), "nebelmoor", "nebelmoor_frostkamm"));
            frost.Portals.Add(P("frostkamm_gluetoede", new Vector3(460f, 0f, 40f), "gluetoede", "gluetoede_frostkamm"));
            frost.Tips.AddRange(new[] { "Der Springer überwindet die Terrassen des Frostkamms mit einem Satz.", "Der Weltboss Frostschlund erscheint alle 15 Minuten." });

            var glut = Def("gluetoede", "Glutöde", "Aschefelder, Lavarisse und der ewige Sturm.", 30030, 1200f, 35, 45, "asche",
                new Rect(0.66f, 0.3f, 0.3f, 0.34f), C("#8a5a46"), new Vector3(-470f, 0f, 0f));
            glut.Portals.Add(P("gluetoede_frostkamm", new Vector3(-510f, 0f, -30f), "frostkamm", "frostkamm_gluetoede"));
            glut.Tips.AddRange(new[] { "Lavarisse brennen – erst die Umwelt-Beherrschung macht sie passierbar.", "Das Meta-Ereignis „Sturm über der Glutöde“ verläuft in vier Phasen." });

            var extra = BuildZonesLauf6(sonn, moor, frost, glut);   // Lauf 6: sechs neue Zonen, Grenzportale und Portalplatz in Silbermund
            foreach (var z in new[] { sonn, moor, frost, glut }) EditorUtility.SetDirty(z);
            var w = Asset<WorldDefinition>(WorldPath);
            w.StartZone = "sonnfurt";
            w.Zones = new List<ZoneDefinition> { sonn, moor, frost, glut };
            w.Zones.AddRange(extra);
            var all10 = new List<ZoneDefinition>(w.Zones);
            w.Zones.AddRange(BuildZonesLauf11(all10));   // Lauf 11: 20 Zonen in drei Regionen
            w.Tips = new List<string> { "Mit M öffnest du die Karte – weiter herauszoomen zeigt den Kontinent.", "Wegmarken anderer Zonen kosten einen Aufschlag.", "In niedrigeren Zonen wirst du herabgestuft – Belohnungen bleiben fair.", "Drücke F an Portalen, Einblicken und beim Stallmeister." };
            EditorUtility.SetDirty(w);
            AssetDatabase.SaveAssets();
            var errors = WorldModel.ValidatePortals(w.Zones);
            foreach (var e in errors) Debug.LogError("[ProjectTyrant] Portal: " + e);
        }

        // ---------------- Core-Szene ----------------
        public static void BuildCore()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var systems = SceneBuilder.CreateSystems();
            var start = Zone("sonnfurt");
            var spawn = start != null ? start.DefaultSpawn : Vector3.zero;
            var player = SceneBuilder.CreatePlayer(spawn + new Vector3(0, 0.5f, 3f));
            player.transform.rotation = Quaternion.LookRotation(new Vector3(1, 0, 1));
            SceneBuilder.CreateCameraRig(player);
            var gm = systems.GetComponent<GameManager>();
            gm.Player = player.GetComponent<Combatant>();
            gm.World = LoadWorld();
            // Lauf 6: Geschichte und Szenen am GameManager-Objekt (keine neuen Singletons)
            var story = systems.AddComponent<StoryDirector>(); story.Database = StoryBuilder.Load();
            systems.AddComponent<DialoguePlayer>();
            EditorSceneManager.SaveScene(scene, CorePath);
        }

        public static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CharacterSelectBuilder.ScenePath) != null) scenes.Add(new EditorBuildSettingsScene(CharacterSelectBuilder.ScenePath, true));   // Lauf 5 / M25: Start in der Charakterauswahl
            scenes.Add(new EditorBuildSettingsScene(CorePath, true));
            var w = LoadWorld();
            if (w != null) foreach (var z in w.Zones)
                {
                    string path = z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneBuilder.SandboxPath) != null) scenes.Add(new EditorBuildSettingsScene(SceneBuilder.SandboxPath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void OpenCoreWithZone(string zoneScene)
        {
            EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Single);
            if (!string.IsNullOrEmpty(zoneScene)) EditorSceneManager.OpenScene(zoneScene, OpenSceneMode.Additive);
        }

        public static void RebuildZone(string id)
        {
            var z = Zone(id);
            if (z == null) { BuildZones(); z = Zone(id); }
            BiomeZoneBuilder.LastReachReport.Clear(); BiomeZoneBuilder.LastReachErrors = 0;
            if (z.LegacyLayout) ZoneBuilder.BuildZone(); else BiomeZoneBuilder.Build(z);
            BalanceBuilder.LastErrors.Clear(); BalanceBuilder.ApplyZone(z);
            if (BalanceBuilder.LastErrors.Count > 0) Debug.LogError("[ProjectTyrant] Balance-Validator " + z.Id + ":\n" + string.Join("\n", BalanceBuilder.LastErrors));
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            OpenCoreWithZone(z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z));
        }

        [MenuItem("Project Tyrant/Rebuild Zone/Grenzmark Ashveil (Startzone)")] public static void RebuildSonnfurt() => RebuildZone("sonnfurt");
        [MenuItem("Project Tyrant/Rebuild Zone/Nebelmoor")] public static void RebuildNebelmoor() => RebuildZone("nebelmoor");
        [MenuItem("Project Tyrant/Rebuild Zone/Frostkamm")] public static void RebuildFrostkamm() => RebuildZone("frostkamm");
        [MenuItem("Project Tyrant/Rebuild Zone/Glutöde")] public static void RebuildGluetoede() => RebuildZone("gluetoede");
        [MenuItem("Project Tyrant/Build Core Scene")] public static void RebuildCore() { BuildCore(); UpdateBuildSettings(); OpenCoreWithZone(ZoneBuilder.ZonePath); }

        public static readonly Dictionary<string, double> LastTimings = new Dictionary<string, double>();

        [MenuItem("Project Tyrant/Rebuild Everything")]
        public static void RebuildEverything()
        {
            LastTimings.Clear();
            var total = System.Diagnostics.Stopwatch.StartNew();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            System.Action<string> lap = key => { LastTimings[key] = sw.Elapsed.TotalSeconds; sw.Restart(); };
            IconPainter.BuildAll(); lap("Icons");
            ClipBuilder.BuildAll(); CharacterRigs.PrepareMeshy(); lap("Animation (Clips, Meshy-Skinning)");
            AnimGalleryBuilder.Build(); lap("QA-Szene Animationen");
            VfxKitBuilder.BuildAll(); VfxGalleryBuilder.Build(); lap("VFX-Baukasten + QA-Szene");
            DataBuilder.BuildAll(); lap("Daten");
            ClassBuilder.BuildAll(); StoryBuilder.BuildBundles(); SkillTimelineBuilder.BuildAll(); lap("Klassen (Lauf 5) + Bündel (Lauf 6)");
            AudioBuilder.BuildAll(); lap("Audio (Klänge, Musik, Stinger)");
            WorldEnemyBuilder.BuildAll(); lap("Gegner & Tiere (Lauf 4)");
            MountBuilder.BuildAll(); lap("Reittiere");
            MasteryBuilder.BuildAll(); lap("Beherrschungen");
            BiomeZoneBuilder.LastReachReport.Clear(); BiomeZoneBuilder.LastReachErrors = 0;
            EventDataBuilder.Build(); lap("Ereignisdaten");
            BuildZones(); lap("Zonen-Definitionen");
            ZoneBuilder.BuildZone(); lap("Zone sonnfurt");
            var ids = new List<string>();
            foreach (var z in LoadWorld().Zones) if (!z.LegacyLayout) ids.Add(z.Id);
            foreach (var id in ids) { BiomeZoneBuilder.Build(Zone(id)); lap("Zone " + id); }
            int balanceErrors = BalanceBuilder.ApplyAll(); lap("Balance B1 (Gefahrenstufen, Spawns, Event-Stufen, Validator)");
            LoadingImageBuilder.BuildAll(); lap("Ladebilder");
            StoryBuilder.BuildAll(true); lap("Geschichte + Sprache (Lauf 6)");
            BuildCore(); lap("Core");
            CharacterSelectBuilder.Build(); lap("Charakterauswahl");
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            OpenCoreWithZone(ZoneBuilder.ZonePath);
            LastTimings["Gesamt"] = total.Elapsed.TotalSeconds;
            var sb = new System.Text.StringBuilder("[ProjectTyrant] Rebuild Everything: ");
            foreach (var kv in LastTimings) sb.Append(kv.Key).Append(' ').Append(kv.Value.ToString("0.0")).Append(" s · ");
            Debug.Log(sb.ToString());
            if (balanceErrors > 0) throw new System.Exception("[ProjectTyrant] Rebuild Everything abgebrochen: Balance-Validator meldet " + balanceErrors + " Verstöße (siehe Konsole).");
        }
    }
}
