using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 11 / M60–M65: 20 neue Zonen in drei Regionen (Kernland-Erweiterung, Glutwüste, Dracheninseln) aus <see cref="RegionZones"/>:
    /// Biom-Profile, Zonen-Definitionen, Portale (Kernland paarweise an Bestandszonen gleichen Bands, Glutwüste über die Tyrannenfeste
    /// und das Schiff von Silbermund, Dracheninseln über Schiffe von Silbermund und Sonnentor) und Regionen-Ketten.
    /// </summary>
    public static partial class WorldBuilder
    {
        static void BuildBiomesLauf11()
        {
            Biome("steppe", "Steppe", b =>
            {
                b.Amplitude = 7f; b.Frequency = 0.0025f; b.Octaves = 3; b.BaseHeight = 3f; b.WaterLevel = 0.4f; b.EdgeMountainHeight = 34f;
                b.GroundLow = C("#a8a860"); b.GroundMid = C("#bcb46a"); b.GroundHigh = C("#d0c27c"); b.Cliff = C("#a08a6a"); b.Shore = C("#c8b880"); b.Water = new Color(0.25f, 0.5f, 0.6f, 0.85f);
                b.FogColor = C("#e8e0c0"); b.FogDensity = 0.0022f; b.SkyTop = C("#4a8ae0"); b.SkyHorizon = C("#f0ecd0"); b.SkyBottom = C("#c0b890");
                b.SunColor = C("#fff4d0"); b.SunIntensity = 1.3f; b.Saturation = 6f; b.SplitShadows = C("#6a7aa0"); b.SplitHighlights = C("#f0d890");
                b.TreeDensity = 0.06f; b.RockDensity = 0.2f; b.TreeColor = C("#7a9a4a"); b.TrunkColor = C("#5a4430");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Wind;
            });
            Biome("hochmoor", "Hochmoor", b =>
            {
                b.Amplitude = 9f; b.Frequency = 0.004f; b.Octaves = 4; b.Terrace = 2f; b.BaseHeight = 5f; b.WaterLevel = 1.2f; b.EdgeMountainHeight = 40f;
                b.GroundLow = C("#5a6a4a"); b.GroundMid = C("#6e7a56"); b.GroundHigh = C("#8a8a6a"); b.Cliff = C("#6a6660"); b.Shore = C("#4a4a38"); b.Water = new Color(0.22f, 0.28f, 0.24f, 0.9f);
                b.FogColor = C("#a8b0a8"); b.FogDensity = 0.008f; b.SkyTop = C("#5a6a80"); b.SkyHorizon = C("#b8c0c0"); b.SkyBottom = C("#5a6050");
                b.SunColor = C("#e8e8d8"); b.SunIntensity = 0.85f; b.Saturation = -4f; b.SplitShadows = C("#4a5a6a"); b.SplitHighlights = C("#c8c0a0");
                b.TreeDensity = 0.1f; b.RockDensity = 0.45f; b.TreeColor = C("#4a5a3a"); b.TrunkColor = C("#3a3028"); b.DeadTrees = true;
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Swamp;
            });
            Biome("dschungel", "Dschungel", b =>
            {
                b.Amplitude = 14f; b.Frequency = 0.0048f; b.Octaves = 4; b.BaseHeight = 4f; b.WaterLevel = 0.8f; b.EdgeMountainHeight = 50f;
                b.GroundLow = C("#2e5a26"); b.GroundMid = C("#3a6a2c"); b.GroundHigh = C("#5a7a3a"); b.Cliff = C("#4a4a3a"); b.Shore = C("#5a5030"); b.Water = new Color(0.2f, 0.45f, 0.35f, 0.85f);
                b.FogColor = C("#a0c8a0"); b.FogDensity = 0.006f; b.SkyTop = C("#3a8ac0"); b.SkyHorizon = C("#d0f0d0"); b.SkyBottom = C("#4a6a40");
                b.SunColor = C("#f8ffe0"); b.SunIntensity = 1.1f; b.Saturation = 18f; b.SplitShadows = C("#2a5a4a"); b.SplitHighlights = C("#d0e890");
                b.TreeDensity = 1f; b.RockDensity = 0.2f; b.TreeColor = C("#2a6a2a"); b.TrunkColor = C("#4a3a20");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Meadow;
            });
            Biome("delta", "Flussdelta", b =>
            {
                b.Amplitude = 4f; b.Frequency = 0.005f; b.Octaves = 3; b.BaseHeight = 1.4f; b.WaterLevel = 1.1f; b.EdgeMountainHeight = 30f;
                b.GroundLow = C("#6a8a4a"); b.GroundMid = C("#7a9a56"); b.GroundHigh = C("#a0a870"); b.Cliff = C("#8a7a60"); b.Shore = C("#b8a878"); b.Water = new Color(0.25f, 0.45f, 0.42f, 0.85f);
                b.FogColor = C("#c8d8c8"); b.FogDensity = 0.004f; b.SkyTop = C("#4a90d0"); b.SkyHorizon = C("#e0f0e8"); b.SkyBottom = C("#90a888");
                b.SunColor = C("#fff8e0"); b.SunIntensity = 1.15f; b.Saturation = 10f; b.SplitShadows = C("#4a6a8a"); b.SplitHighlights = C("#e0d8a0");
                b.TreeDensity = 0.45f; b.RockDensity = 0.1f; b.TreeColor = C("#3a6a3a"); b.TrunkColor = C("#4a3a2a");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Swamp;
            });
            Biome("eis", "Eiswüste", b =>
            {
                b.Amplitude = 18f; b.Frequency = 0.0035f; b.Octaves = 4; b.Terrace = 5f; b.BaseHeight = 6f; b.WaterLevel = -100f; b.EdgeMountainHeight = 70f;
                b.GroundLow = C("#d8e4ee"); b.GroundMid = C("#e6eef6"); b.GroundHigh = C("#f6fafe"); b.Cliff = C("#9ab0c8"); b.Shore = C("#c0d0e0");
                b.FogColor = C("#b8c8e0"); b.FogDensity = 0.0075f; b.SkyTop = C("#101a3a"); b.SkyHorizon = C("#5a7aa8"); b.SkyBottom = C("#20304a");
                b.SunColor = C("#a8c8ff"); b.SunIntensity = 0.55f; b.Saturation = 0f; b.SplitShadows = C("#2a3a7a"); b.SplitHighlights = C("#a0e0d0");
                b.TreeDensity = 0.08f; b.RockDensity = 0.5f; b.TreeColor = C("#3a5a5a"); b.TrunkColor = C("#3a3a3a"); b.Conifers = true;
                b.Weather = WeatherKind.Snow; b.Ambient = AmbientKind.Wind;
            });
            Biome("ruinen", "Versunkene Ruinen", b =>
            {
                b.Amplitude = 10f; b.Frequency = 0.0045f; b.Octaves = 4; b.Terrace = 3f; b.BaseHeight = 2.5f; b.WaterLevel = 1.0f; b.EdgeMountainHeight = 45f;
                b.GroundLow = C("#3a3a3e"); b.GroundMid = C("#4a4650"); b.GroundHigh = C("#5a5460"); b.Cliff = C("#2a2830"); b.Shore = C("#5a5048"); b.Water = new Color(0.2f, 0.3f, 0.32f, 0.9f);
                b.FogColor = C("#4a4a5a"); b.FogDensity = 0.009f; b.SkyTop = C("#20202e"); b.SkyHorizon = C("#6a5a7a"); b.SkyBottom = C("#202024");
                b.SunColor = C("#c8b8e0"); b.SunIntensity = 0.7f; b.Saturation = -8f; b.SplitShadows = C("#3a2a5a"); b.SplitHighlights = C("#9a8aa0");
                b.TreeDensity = 0.08f; b.RockDensity = 0.7f; b.TreeColor = C("#3a3a3a"); b.TrunkColor = C("#2a2420"); b.DeadTrees = true;
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Swamp;
            });
            Biome("salz", "Salzsee", b =>
            {
                b.Amplitude = 3f; b.Frequency = 0.002f; b.Octaves = 2; b.BaseHeight = 2f; b.WaterLevel = 1.6f; b.EdgeMountainHeight = 38f;
                b.GroundLow = C("#e8e4d8"); b.GroundMid = C("#f0ece0"); b.GroundHigh = C("#f8f4ea"); b.Cliff = C("#c8b8a0"); b.Shore = C("#ffffff"); b.Water = new Color(0.7f, 0.85f, 0.9f, 0.6f);
                b.FogColor = C("#f8f0e0"); b.FogDensity = 0.0025f; b.SkyTop = C("#3a80e0"); b.SkyHorizon = C("#fff8e8"); b.SkyBottom = C("#e8e0d0");
                b.SunColor = C("#ffffff"); b.SunIntensity = 1.45f; b.Saturation = 4f; b.SplitShadows = C("#6a7ab0"); b.SplitHighlights = C("#fff0c0");
                b.TreeDensity = 0f; b.RockDensity = 0.12f; b.TreeColor = C("#8a8a5a"); b.TrunkColor = C("#6a5a4a"); b.DeadTrees = true;
                b.Weather = WeatherKind.None; b.Ambient = AmbientKind.Wind;
            });
            Biome("jadestadt", "Jadehafen", b =>
            {
                b.Amplitude = 12f; b.Frequency = 0.004f; b.Octaves = 3; b.Terrace = 8f; b.BaseHeight = 4f; b.WaterLevel = 0.8f; b.EdgeMountainHeight = 45f;
                b.GroundLow = C("#5a8a6a"); b.GroundMid = C("#6a9a76"); b.GroundHigh = C("#a8b8a0"); b.Cliff = C("#6a7070"); b.Shore = C("#c8c0a0"); b.Water = new Color(0.2f, 0.6f, 0.55f, 0.85f);
                b.FogColor = C("#c0e0d8"); b.FogDensity = 0.003f; b.SkyTop = C("#3a8ad0"); b.SkyHorizon = C("#e0f8f0"); b.SkyBottom = C("#90b8a8");
                b.SunColor = C("#fff8e8"); b.SunIntensity = 1.2f; b.Saturation = 12f; b.SplitShadows = C("#3a6a7a"); b.SplitHighlights = C("#f0e0a0");
                b.TreeDensity = 0.2f; b.RockDensity = 0.2f; b.TreeColor = C("#4a8a4a"); b.TrunkColor = C("#5a4a3a");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Meadow;
            });
            Biome("jademeer", "Jade-Meer", b =>
            {
                b.Amplitude = 5f; b.Frequency = 0.003f; b.Octaves = 3; b.BaseHeight = 1.2f; b.WaterLevel = 1.4f; b.EdgeMountainHeight = 36f;
                b.GroundLow = C("#4ab08a"); b.GroundMid = C("#5ac09a"); b.GroundHigh = C("#9ad8b8"); b.Cliff = C("#3a7a6a"); b.Shore = C("#c8e8d0"); b.Water = new Color(0.2f, 0.7f, 0.55f, 0.75f);
                b.FogColor = C("#b8f0d8"); b.FogDensity = 0.0035f; b.SkyTop = C("#3a90d0"); b.SkyHorizon = C("#d8fff0"); b.SkyBottom = C("#80c0a8");
                b.SunColor = C("#f0fff0"); b.SunIntensity = 1.25f; b.Saturation = 14f; b.SplitShadows = C("#2a7a7a"); b.SplitHighlights = C("#d0ffd0");
                b.TreeDensity = 0.05f; b.RockDensity = 0.3f; b.TreeColor = C("#4a9a6a"); b.TrunkColor = C("#4a4a3a");
                b.Weather = WeatherKind.None; b.Ambient = AmbientKind.Wind;
            });
            Biome("bambus", "Bambuswald", b =>
            {
                b.Amplitude = 12f; b.Frequency = 0.005f; b.Octaves = 4; b.Terrace = 3f; b.BaseHeight = 4f; b.WaterLevel = 1.0f; b.EdgeMountainHeight = 48f;
                b.GroundLow = C("#4a7a3a"); b.GroundMid = C("#5a8a44"); b.GroundHigh = C("#7a9a5a"); b.Cliff = C("#5a5a4a"); b.Shore = C("#6a6040"); b.Water = new Color(0.25f, 0.5f, 0.45f, 0.85f);
                b.FogColor = C("#c0d8c0"); b.FogDensity = 0.0065f; b.SkyTop = C("#4a80b0"); b.SkyHorizon = C("#e0f0e0"); b.SkyBottom = C("#5a7a50");
                b.SunColor = C("#f0ffe8"); b.SunIntensity = 0.95f; b.Saturation = 8f; b.SplitShadows = C("#3a5a5a"); b.SplitHighlights = C("#d8e8a8");
                b.TreeDensity = 0.95f; b.RockDensity = 0.25f; b.TreeColor = C("#6aa04a"); b.TrunkColor = C("#8a9a4a");
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Meadow;
            });
            Biome("terrassen", "Reisterrassen", b =>
            {
                b.Amplitude = 20f; b.Frequency = 0.004f; b.Octaves = 3; b.Terrace = 3f; b.BaseHeight = 6f; b.WaterLevel = 0.6f; b.EdgeMountainHeight = 55f;
                b.GroundLow = C("#7ab05a"); b.GroundMid = C("#8ac064"); b.GroundHigh = C("#b0c880"); b.Cliff = C("#8a7a5a"); b.Shore = C("#c8b880"); b.Water = new Color(0.35f, 0.6f, 0.6f, 0.8f);
                b.FogColor = C("#e0f0e0"); b.FogDensity = 0.003f; b.SkyTop = C("#4a90e0"); b.SkyHorizon = C("#f8f8e8"); b.SkyBottom = C("#a0c090");
                b.SunColor = C("#fff8e0"); b.SunIntensity = 1.25f; b.Saturation = 14f; b.SplitShadows = C("#5a7aa0"); b.SplitHighlights = C("#f8e0a0");
                b.TreeDensity = 0.25f; b.RockDensity = 0.15f; b.TreeColor = C("#e8a0b8"); b.TrunkColor = C("#5a3a30");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Meadow;
            });
            Biome("nebelkueste", "Nebelküste", b =>
            {
                b.Amplitude = 16f; b.Frequency = 0.0045f; b.Octaves = 4; b.Terrace = 4f; b.BaseHeight = 4f; b.WaterLevel = 0.8f; b.EdgeMountainHeight = 50f;
                b.GroundLow = C("#4a5a58"); b.GroundMid = C("#5a6a66"); b.GroundHigh = C("#7a8480"); b.Cliff = C("#3a4040"); b.Shore = C("#8a8478"); b.Water = new Color(0.2f, 0.32f, 0.38f, 0.9f);
                b.FogColor = C("#a0aab0"); b.FogDensity = 0.012f; b.SkyTop = C("#40505a"); b.SkyHorizon = C("#a8b4b8"); b.SkyBottom = C("#40484a");
                b.SunColor = C("#d8e0e8"); b.SunIntensity = 0.7f; b.Saturation = -6f; b.SplitShadows = C("#3a4a6a"); b.SplitHighlights = C("#b8c0b0");
                b.TreeDensity = 0.12f; b.RockDensity = 0.55f; b.TreeColor = C("#3a4a3a"); b.TrunkColor = C("#2a2a28"); b.DeadTrees = true;
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Wind;
            });
        }

        /// <summary>Biom-Zuordnung: neue Profile oder Bestandsprofile (Wüste, Asche, Küste, Feste).</summary>
        static string BiomeFor(string specBiome)
        {
            switch (specBiome)
            {
                case "klippen": return "kueste";
                case "vulkan": case "glutkessel": return "asche";
                case "oase": case "duenen": case "schlucht": return "wueste";
                case "zarathis": return "feste";
                case "drachenende": return "jademeer";
                default: return specBiome;
            }
        }

        /// <summary>Kernland-Zonen hängen paarweise an der Bestandszone gleichen Bands (Portal am Zonenrand).</summary>
        public static readonly Dictionary<string, string> KernlandAnchor = new Dictionary<string, string>
        {
            { "weite_von_ahrn", "frostkamm" }, { "nebelkamm", "gluetoede" }, { "sturmbucht", "silberkueste" }, { "aschenkessel", "daemmerwald" },
            { "gruener_schlund", "daemmerwald" }, { "schilfmuendung", "duenenmeer" }, { "weisse_stille", "himmelsspitzen" }, { "schaedelkueste", "tyrannenfeste" },
        };

        /// <summary>Regionen-Ketten (Nachbarn per Grenzportal) für Glutwüste und Dracheninseln.</summary>
        public static readonly (string a, string b)[] RegionChains =
        {
            ("weite_von_ahrn", "nebelkamm"), ("sturmbucht", "aschenkessel"), ("gruener_schlund", "schilfmuendung"), ("weisse_stille", "schaedelkueste"),
            ("sonnentor", "grosse_leere"), ("grosse_leere", "klingenschlucht"), ("sonnentor", "spiegel_der_sonne"), ("klingenschlucht", "glutkessel"), ("glutkessel", "zarathis"),
            ("jadehafen", "gruene_flut"), ("jadehafen", "fluesterhain"), ("fluesterhain", "sonnenstufen"), ("gruene_flut", "nebelkueste"), ("nebelkueste", "herz_der_flut"),
        };

        static int _portalSlot;
        static Vector3 EdgePos(ZoneDefinition z, int slot)
        {
            float a = slot * 1.1f + 0.6f, r = z.Size * 0.42f;
            return new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
        }

        static void Link(ZoneDefinition a, ZoneDefinition b, string tag = "")
        {
            string ida = a.Id + "_" + b.Id + tag, idb = b.Id + "_" + a.Id + tag;
            if (a.Portal(ida) != null) return;
            a.Portals.Add(P(ida, EdgePos(a, a.Portals.Count + _portalSlot), b.Id, idb));
            b.Portals.Add(P(idb, EdgePos(b, b.Portals.Count + _portalSlot + 3), a.Id, ida));
        }

        /// <summary>Lauf 11: die 20 neuen Zonen samt Portalen; Schiffe = Portalpaare am Hafen (Ladebildschirm „Schiffsreise“).</summary>
        static List<ZoneDefinition> BuildZonesLauf11(List<ZoneDefinition> existing)
        {
            var map = new Dictionary<string, ZoneDefinition>();
            foreach (var z in existing) map[z.Id] = z;
            var list = new List<ZoneDefinition>();
            foreach (var s in RegionZones.All)
            {
                var z = Def(s.Id, s.Name, s.Description, s.Seed, s.Size, s.LevelMin, s.LevelMax, BiomeFor(s.Biome), s.MapRect, s.MapColor, RegionZones.Spawn(s));
                z.Region = s.Region; z.IsHub = s.Hub;
                z.Tips.Add(s.Tip); z.Tips.Add("Signatur: " + s.Signature);
                map[s.Id] = z; list.Add(z);
            }
            _portalSlot = 0;
            foreach (var kv in KernlandAnchor) Link(map[kv.Key], map[kv.Value], "_k");
            foreach (var (a, b) in RegionChains) Link(map[a], map[b]);
            // Glutwüste: Portal im Tyrannenfeste-Vorland, Schiff von Silbermund; Dracheninseln: Schiffe von Silbermund und Sonnentor
            Link(map["tyrannenfeste"], map["sonnentor"], "_portal");
            Link(map["silberkueste"], map["sonnentor"], "_schiff");
            Link(map["silberkueste"], map["jadehafen"], "_schiff");
            Link(map["sonnentor"], map["jadehafen"], "_schiff");
            foreach (var z in list) EditorUtility.SetDirty(z);
            foreach (var z in existing) EditorUtility.SetDirty(z);
            return list;
        }

        public static readonly string[] Lauf11Zones = System.Array.ConvertAll(RegionZones.All, s => s.Id);

        [MenuItem("Project Tyrant/Rebuild Region/Kernland (8 neue Zonen)")] public static void RebuildRegionKernland() => RebuildRegion(WorldRegion.Kernland);
        [MenuItem("Project Tyrant/Rebuild Region/Glutwüste")] public static void RebuildRegionGlut() => RebuildRegion(WorldRegion.Glutwueste);
        [MenuItem("Project Tyrant/Rebuild Region/Dracheninseln")] public static void RebuildRegionDrachen() => RebuildRegion(WorldRegion.Dracheninseln);

        /// <summary>Lauf 11: alle neuen Zonen einer Region neu bauen (Zonen-Definitionen, Ereignisse, Gegner bleiben; Balance je Zone).</summary>
        public static void RebuildRegion(WorldRegion r)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            BuildZones();
            foreach (var s in RegionZones.InRegion(r)) { var z = Zone(s.Id); if (z == null) continue; BiomeZoneBuilder.Build(z); BalanceBuilder.ApplyZone(Zone(s.Id)); }
            UpdateBuildSettings(); AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Rebuild Region " + RegionZones.RegionName(r) + ": " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
            OpenCoreWithZone(ZoneBuilder.ZonePath);
        }
    }
}
