using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6 / M30 + M32: sechs neue Level-30-Zonen (Silberküste mit Silbermund, Dämmerwald, Dünenmeer, Kristalltiefe, Himmelsspitzen, Tyrannenfeste),
    /// Grenzportale und der Portalplatz in Silbermund (je Zone ein Portalpaar Platz ↔ Ankunft).
    /// </summary>
    public static partial class WorldBuilder
    {
        public static readonly string[] Lauf6Zones = { "silberkueste", "daemmerwald", "duenenmeer", "kristalltiefe", "himmelsspitzen", "tyrannenfeste" };
        /// <summary>Portalplatz: Mitte und Radius des Portalrings in Silbermund.</summary>
        public static readonly Vector3 PlazaCenter = new Vector3(0f, 0f, 40f);
        public const float PlazaRadius = 16f;

        static void BuildBiomesLauf6()
        {
            Biome("kueste", "Steilküste", b =>
            {
                b.Amplitude = 9f; b.Frequency = 0.0045f; b.Octaves = 4; b.Terrace = 2.5f; b.BaseHeight = 3.5f; b.WaterLevel = 0.6f; b.EdgeMountainHeight = 36f;
                b.GroundLow = C("#8fae5e"); b.GroundMid = C("#a4b86a"); b.GroundHigh = C("#c9c08a"); b.Cliff = C("#bfb4a0"); b.Shore = C("#e6d6a8"); b.Water = new Color(0.2f, 0.5f, 0.68f, 0.85f);
                b.FogColor = C("#cfe2ec"); b.FogDensity = 0.003f; b.SkyTop = C("#3f86d8"); b.SkyHorizon = C("#d6ecf6"); b.SkyBottom = C("#9fb8c0");
                b.SunColor = C("#fff4dc"); b.SunIntensity = 1.2f; b.Saturation = 12f; b.SplitShadows = C("#5a7aa0"); b.SplitHighlights = C("#e0c890");
                b.TreeDensity = 0.3f; b.RockDensity = 0.35f; b.TreeColor = C("#5f8a3a"); b.TrunkColor = C("#5a4430");
                b.Weather = WeatherKind.Pollen; b.Ambient = AmbientKind.Meadow;
            });
            Biome("wald", "Dämmerwald", b =>
            {
                b.Amplitude = 10f; b.Frequency = 0.005f; b.Octaves = 4; b.BaseHeight = 3f; b.WaterLevel = -100f; b.EdgeMountainHeight = 40f;
                b.GroundLow = C("#2f3b26"); b.GroundMid = C("#3b4a2c"); b.GroundHigh = C("#4f5038"); b.Cliff = C("#3a3530"); b.Shore = C("#3a3326");
                b.FogColor = C("#3e3a52"); b.FogDensity = 0.0095f; b.SkyTop = C("#262338"); b.SkyHorizon = C("#5d5476"); b.SkyBottom = C("#2a2a2a");
                b.SunColor = C("#c8b8e8"); b.SunIntensity = 0.7f; b.Saturation = -6f; b.SplitShadows = C("#3a2c5a"); b.SplitHighlights = C("#8a9a6a");
                b.TreeDensity = 0.85f; b.RockDensity = 0.25f; b.TreeColor = C("#2e4426"); b.TrunkColor = C("#2a2018");
                b.Weather = WeatherKind.MistWisps; b.Ambient = AmbientKind.Swamp;
            });
            Biome("wueste", "Dünenmeer", b =>
            {
                b.Amplitude = 16f; b.Frequency = 0.0028f; b.Octaves = 3; b.CanyonDepth = 12f; b.CanyonFrequency = 0.0035f; b.BaseHeight = 4f; b.WaterLevel = 0.3f; b.EdgeMountainHeight = 42f;
                b.GroundLow = C("#d8b578"); b.GroundMid = C("#e4c48a"); b.GroundHigh = C("#efd6a2"); b.Cliff = C("#b2784a"); b.Shore = C("#c9a76a"); b.Water = new Color(0.25f, 0.6f, 0.62f, 0.85f);
                b.FogColor = C("#e8cfa0"); b.FogDensity = 0.0035f; b.SkyTop = C("#4f8fd0"); b.SkyHorizon = C("#f4dfb6"); b.SkyBottom = C("#c8a878");
                b.SunColor = C("#fff0c8"); b.SunIntensity = 1.35f; b.Saturation = 8f; b.SplitShadows = C("#7a5a8a"); b.SplitHighlights = C("#f0c070");
                b.TreeDensity = 0.04f; b.RockDensity = 0.35f; b.TreeColor = C("#7a8a3a"); b.TrunkColor = C("#6a4a2a"); b.DeadTrees = true;
                b.Weather = WeatherKind.AshWind; b.Ambient = AmbientKind.Wind;
            });
            Biome("tiefe", "Kristalltiefe", b =>
            {
                b.Amplitude = 14f; b.Frequency = 0.006f; b.Octaves = 4; b.Terrace = 4f; b.BaseHeight = 2f; b.WaterLevel = 0.5f; b.EdgeMountainHeight = 90f;
                b.GroundLow = C("#2a2a36"); b.GroundMid = C("#34344a"); b.GroundHigh = C("#45405a"); b.Cliff = C("#1e1c26"); b.Shore = C("#2a3040"); b.Water = new Color(0.15f, 0.3f, 0.45f, 0.9f);
                b.FogColor = C("#0e1018"); b.FogDensity = 0.016f; b.SkyTop = C("#05060a"); b.SkyHorizon = C("#141826"); b.SkyBottom = C("#05060a");
                b.SunColor = C("#7f8fd0"); b.SunIntensity = 0.18f; b.Saturation = 6f; b.SplitShadows = C("#202a5a"); b.SplitHighlights = C("#7a5ab0");
                b.TreeDensity = 0f; b.RockDensity = 0.65f; b.TreeColor = C("#3a4a6a"); b.TrunkColor = C("#2a2a3a");
                b.Weather = WeatherKind.None; b.Ambient = AmbientKind.Swamp;
            });
            Biome("himmel", "Himmelsspitzen", b =>
            {
                b.Amplitude = 48f; b.Frequency = 0.0042f; b.Octaves = 4; b.Terrace = 12f; b.CanyonDepth = 30f; b.CanyonFrequency = 0.004f; b.BaseHeight = 30f; b.WaterLevel = -100f; b.EdgeMountainHeight = 60f;
                b.GroundLow = C("#7fa868"); b.GroundMid = C("#9cc07a"); b.GroundHigh = C("#e8eef0"); b.Cliff = C("#a0a4b0"); b.Shore = C("#b8b0a0");
                b.FogColor = C("#e6eef8"); b.FogDensity = 0.0028f; b.SkyTop = C("#3a7ae0"); b.SkyHorizon = C("#f0f6ff"); b.SkyBottom = C("#ffffff");
                b.SunColor = C("#fffaf0"); b.SunIntensity = 1.3f; b.Saturation = 10f; b.SplitShadows = C("#6a8ad0"); b.SplitHighlights = C("#f8e8c0");
                b.TreeDensity = 0.25f; b.RockDensity = 0.4f; b.TreeColor = C("#4a7a4a"); b.TrunkColor = C("#5a4a3a"); b.Conifers = true;
                b.Weather = WeatherKind.None; b.Ambient = AmbientKind.Wind;
            });
            Biome("feste", "Tyrannenfeste", b =>
            {
                b.Amplitude = 18f; b.Frequency = 0.0045f; b.Octaves = 4; b.Terrace = 5f; b.CanyonDepth = 10f; b.CanyonFrequency = 0.005f; b.BaseHeight = 6f; b.WaterLevel = 0.5f; b.EdgeMountainHeight = 55f;
                b.GroundLow = C("#1c1a1e"); b.GroundMid = C("#2a2630"); b.GroundHigh = C("#3a3442"); b.Cliff = C("#141216"); b.Shore = C("#5a2010"); b.Water = new Color(1f, 0.38f, 0.1f, 1f);
                b.FogColor = C("#3a1a2a"); b.FogDensity = 0.0065f; b.SkyTop = C("#1a0e1a"); b.SkyHorizon = C("#7a2a3a"); b.SkyBottom = C("#200a10");
                b.SunColor = C("#ff9a7a"); b.SunIntensity = 0.85f; b.Saturation = -10f; b.SplitShadows = C("#3a1a4a"); b.SplitHighlights = C("#c05a4a");
                b.TreeDensity = 0.05f; b.RockDensity = 0.7f; b.TreeColor = C("#2a1e2a"); b.TrunkColor = C("#1a1218"); b.DeadTrees = true;
                b.Weather = WeatherKind.AshWind; b.Ambient = AmbientKind.Embers;
            });
        }

        static List<ZoneDefinition> BuildZonesLauf6(ZoneDefinition sonn, ZoneDefinition moor, ZoneDefinition frost, ZoneDefinition glut)
        {
            var kueste = Def("silberkueste", "Silberküste", "Steilküste, Strände und Silbermund, die Hafenstadt der hundert Segel.", 30101, 900f, 40, 50, "kueste",
                new Rect(0.04f, 0.34f, 0.24f, 0.24f), C("#9fc0d8"), new Vector3(0f, 0f, 22f));
            kueste.Tips.AddRange(new[] { "Silbermund: Portalplatz mit Portalen in jede Zone, Bank, Bündnis-Viertel und Markt.", "Wegmarken von Silbermund aus sind günstiger." });
            var wald = Def("daemmerwald", "Dämmerwald", "Ein alter Wald, in den violettes Licht kriecht.", 30102, 1000f, 50, 60, "wald",
                new Rect(0.04f, 0.6f, 0.26f, 0.26f), C("#4a4a6a"), new Vector3(0f, 0f, -420f));
            wald.Tips.AddRange(new[] { "Violette Verderbnis brennt – Laternen und Wächtersteine schützen.", "Der Alte Hain erwacht in regelmäßigen Abständen." });
            var duene = Def("duenenmeer", "Dünenmeer", "Goldene Dünen, Oasen und versunkene Ruinen.", 30103, 1100f, 60, 70, "wueste",
                new Rect(0.62f, 0.66f, 0.34f, 0.3f), C("#e4c48a"), new Vector3(-470f, 0f, 0f));
            duene.Tips.AddRange(new[] { "Alle 12 Minuten zieht ein Sandsturm auf – Späher-Zelte bieten Schutz.", "Der Raptor springt über die Schluchten." });
            var tiefe = Def("kristalltiefe", "Kristalltiefe", "Höhlen unter den Hügeln, erleuchtet nur von Kristallen.", 30104, 900f, 65, 75, "tiefe",
                new Rect(0.62f, 0.04f, 0.18f, 0.22f), C("#4a4a7a"), new Vector3(0f, 0f, -380f));
            tiefe.Tips.AddRange(new[] { "Ohne Licht siehst du wenig – trag eine Laterne oder bleib nahe an den Kristallen.", "Blinde Jäger hören Schritte." });
            var himmel = Def("himmelsspitzen", "Himmelsspitzen", "Gipfel und schwebende Inseln über den Wolken.", 30105, 1000f, 75, 80, "himmel",
                new Rect(0.32f, 0.74f, 0.28f, 0.24f), C("#d8e8f8"), new Vector3(0f, 0f, -420f));
            himmel.Tips.AddRange(new[] { "Aufwinde tragen Gleitende nach oben.", "Wer das Vertrauen der Himmelswächter gewinnt, fliegt mit einem Greif." });
            var feste = Def("tyrannenfeste", "Tyrannenfeste", "Die verderbte Festung des Tyrannen auf schwarzem Fels.", 30106, 900f, 80, 80, "feste",
                new Rect(0.82f, 0.04f, 0.16f, 0.22f), C("#5a2a3a"), new Vector3(0f, 0f, -400f));
            feste.Tips.AddRange(new[] { "Die Belagerung der Feste: drei Tore, der Hof, der Thronsaal.", "Die Feste öffnet sich erst mit Kapitel 10 der Persönlichen Geschichte." });

            // Grenzportale (Kette am Kontinent)
            glut.Portals.Add(P("gluetoede_duenenmeer", new Vector3(520f, 0f, 60f), "duenenmeer", "duenenmeer_gluetoede"));
            duene.Portals.Add(P("duenenmeer_gluetoede", new Vector3(-520f, 0f, -40f), "gluetoede", "gluetoede_duenenmeer"));
            frost.Portals.Add(P("frostkamm_himmelsspitzen", new Vector3(-80f, 0f, 480f), "himmelsspitzen", "himmelsspitzen_frostkamm"));
            himmel.Portals.Add(P("himmelsspitzen_frostkamm", new Vector3(30f, 0f, -460f), "frostkamm", "frostkamm_himmelsspitzen"));
            kueste.Portals.Add(P("silberkueste_daemmerwald", new Vector3(-20f, 0f, 400f), "daemmerwald", "daemmerwald_silberkueste"));
            wald.Portals.Add(P("daemmerwald_silberkueste", new Vector3(-30f, 0f, -460f), "silberkueste", "silberkueste_daemmerwald"));
            kueste.Portals.Add(P("silberkueste_kristalltiefe", new Vector3(400f, 0f, -40f), "kristalltiefe", "kristalltiefe_silberkueste"));
            tiefe.Portals.Add(P("kristalltiefe_silberkueste", new Vector3(-20f, 0f, -420f), "silberkueste", "silberkueste_kristalltiefe"));
            glut.Portals.Add(P("gluetoede_tyrannenfeste", new Vector3(80f, 0f, 520f), "tyrannenfeste", "tyrannenfeste_gluetoede"));
            feste.Portals.Add(P("tyrannenfeste_gluetoede", new Vector3(-30f, 0f, -430f), "gluetoede", "gluetoede_tyrannenfeste"));

            // Portalplatz: je Zone ein Ring-Portal in Silbermund und ein Rückportal nahe der Ankunft
            var all = new[] { sonn, moor, frost, glut, wald, duene, tiefe, himmel, feste };
            for (int i = 0; i < all.Length; i++)
            {
                var z = all[i]; float a = (i + 0.5f) / all.Length * Mathf.PI * 2f;
                var plaza = PlazaCenter + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * PlazaRadius;
                kueste.Portals.Add(new PortalDef { Id = "plaza_" + z.Id, Position = plaza, Yaw = Mathf.Atan2(PlazaCenter.x - plaza.x, PlazaCenter.z - plaza.z) * Mathf.Rad2Deg + 180f, TargetZone = z.Id, TargetPortal = z.Id + "_silbermund" });
                var back = z.LegacyLayout ? FindStartZoneSpot(new Vector2(-40f, -20f), 30f) : z.DefaultSpawn + new Vector3(14f, 0f, 10f);
                z.Portals.Add(new PortalDef { Id = z.Id + "_silbermund", Position = back, Yaw = YawToCenter(back), TargetZone = "silberkueste", TargetPortal = "plaza_" + z.Id });
            }
            var list = new List<ZoneDefinition> { kueste, wald, duene, tiefe, himmel, feste };
            foreach (var z in list) EditorUtility.SetDirty(z);
            return list;
        }

        /// <summary>Freier, flacher, trockener Platz in der Startzone um <paramref name="c"/> (Rastersuche wie beim Nebelmoor-Portal).</summary>
        public static Vector3 FindStartZoneSpot(Vector2 c, float r)
        {
            Vector3 best = new Vector3(c.x, ZoneLayout.Height(c.x, c.y), c.y); float bestScore = float.MaxValue;
            for (float x = c.x - r; x <= c.x + r; x += 2f)
                for (float z = c.y - r; z <= c.y + r; z += 2f)
                {
                    float h = ZoneLayout.Height(x, z);
                    if (h < ZoneLayout.WaterLevel + 0.5f || !ZoneLayout.IsFreeForScatter(x, z, 7f)) continue;
                    float slope = 0f;
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; slope = Mathf.Max(slope, Mathf.Abs(ZoneLayout.Height(x + Mathf.Cos(a) * 4f, z + Mathf.Sin(a) * 4f) - h)); }
                    if (slope > 0.9f) continue;
                    float score = slope * 10f + ZoneLayout.DistanceToPaths(new Vector2(x, z)) * 0.3f + Vector2.Distance(new Vector2(x, z), c) * 0.05f;
                    if (score < bestScore) { bestScore = score; best = new Vector3(x, h, z); }
                }
            return best;
        }

        [MenuItem("Project Tyrant/Rebuild Zone/Silberküste (Silbermund)")] public static void RebuildSilberkueste() => RebuildZone("silberkueste");
        [MenuItem("Project Tyrant/Rebuild Zone/Dämmerwald")] public static void RebuildDaemmerwald() => RebuildZone("daemmerwald");
        [MenuItem("Project Tyrant/Rebuild Zone/Dünenmeer")] public static void RebuildDuenenmeer() => RebuildZone("duenenmeer");
        [MenuItem("Project Tyrant/Rebuild Zone/Kristalltiefe")] public static void RebuildKristalltiefe() => RebuildZone("kristalltiefe");
        [MenuItem("Project Tyrant/Rebuild Zone/Himmelsspitzen")] public static void RebuildHimmelsspitzen() => RebuildZone("himmelsspitzen");
        [MenuItem("Project Tyrant/Rebuild Zone/Tyrannenfeste")] public static void RebuildTyrannenfeste() => RebuildZone("tyrannenfeste");
    }
}
