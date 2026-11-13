using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G1: erzeugt Kunstprofile je Zone (aus der Biom-Tabelle der ART_BIBLE, Hue-Verschiebung je Zone), Element-Paletten,
    /// Textur-Rezepte und deren Texturen (deterministisch, Generated/Textures).
    /// </summary>
    public static partial class GraphicsBuilder
    {
        public const string Dir = DataDir + "/Graphics";
        public const string ProfileDir = Dir + "/ZoneArt", ElementDir = Dir + "/Elements", BookmarkDir = Dir + "/Bookmarks", RecipeDir = Dir + "/TextureRecipes";
        public const string TexDir = Root + "/Generated/Textures";
        public static readonly List<string> LastReport = new List<string>();

        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        /// <summary>Biom-Vorgaben (ART_BIBLE): Palette, Licht, Himmel, Nebel, Post, Wetter, Umgebung.</summary>
        public class BiomeArt
        {
            public string Id; public string Ground, Veg, Sky, Accent, Hazard, Light, Shadow, Zenith, Horizon, SkyGround, Fog, Cloud, Outline;
            public float Az = 135, Elev = 55, Intensity = 1.3f, FogDensity = 0.004f, FogHeight = 8f, Falloff = 0.08f, Scatter = 0.5f, CloudCover = 0.4f;
            public float Exposure = 0.1f, Contrast = 14f, Saturation = 12f, TargetSat = 0.42f, WB = 0f, Bloom = 0.5f, BloomThreshold = 1.1f, Vignette = 0.2f, Under = 0f, Wind = 0.6f;
            public string SplitS = "#5a6688", SplitH = "#b09a78", WaterShallow = "#4fb3b8", WaterDeep = "#123a5c";
            public bool Stars = true, Aurora, Ash, Sand, Underground;
            public WeatherState[] Weathers; public AmbientFxKind[] Fx;
        }

        static readonly Dictionary<string, BiomeArt> Biomes = new Dictionary<string, BiomeArt>
        {
            ["wiese"] = new BiomeArt { Id = "wiese", Ground = "#6f9a46", Veg = "#3f7a34", Sky = "#7fb4ea", Accent = "#e9c46a", Hazard = "#8a5a9a", Light = "#fff0d4", Shadow = "#5a6aa0", Zenith = "#3f7fd6", Horizon = "#cfe3f2", SkyGround = "#8aa070", Fog = "#c8dbe8", Cloud = "#ffffff", Outline = "#1f2a1a",
                Az = 140, Elev = 58, Intensity = 1.35f, FogDensity = 0.0028f, FogHeight = 10f, Falloff = 0.06f, Scatter = 0.45f, CloudCover = 0.45f, Saturation = 14f, TargetSat = 0.45f,
                Weathers = new[] { WeatherState.Klar, WeatherState.Bewoelkt, WeatherState.Regen, WeatherState.Gewitter }, Fx = new[] { AmbientFxKind.Pollen, AmbientFxKind.Schmetterlinge, AmbientFxKind.Blaetter, AmbientFxKind.Voegel, AmbientFxKind.Gluehwuermchen } },
            ["moor"] = new BiomeArt { Id = "moor", Ground = "#4d5a34", Veg = "#5b4632", Sky = "#8c86a8", Accent = "#d8d05a", Hazard = "#b6c43a", Light = "#ffd9a0", Shadow = "#4a3f6e", Zenith = "#4b4f7a", Horizon = "#b7a9b8", SkyGround = "#4a4a3a", Fog = "#8f9a8c", Cloud = "#d8d0dc", Outline = "#171a12",
                Az = 220, Elev = 28, Intensity = 1.1f, FogDensity = 0.0048f, FogHeight = 6f, Falloff = 0.15f, Scatter = 0.7f, CloudCover = 0.65f, Saturation = 6f, TargetSat = 0.34f, Contrast = 12f, SplitS = "#4a3f6e", SplitH = "#c2a868",
                Weathers = new[] { WeatherState.Nieselnebel, WeatherState.Bewoelkt, WeatherState.Regen, WeatherState.Klar }, Fx = new[] { AmbientFxKind.Gluehwuermchen, AmbientFxKind.Sporen, AmbientFxKind.Tropfen, AmbientFxKind.Lichtstaub, AmbientFxKind.Voegel } },
            ["frost"] = new BiomeArt { Id = "frost", Ground = "#e8eef4", Veg = "#6f7a88", Sky = "#9cc4e8", Accent = "#f0a8c0", Hazard = "#4ab8e0", Light = "#e8f2ff", Shadow = "#6878b8", Zenith = "#3a6ab8", Horizon = "#f2d8e2", SkyGround = "#c8d4e0", Fog = "#dce6f0", Cloud = "#ffffff", Outline = "#202838",
                Az = 160, Elev = 32, Intensity = 0.95f, FogDensity = 0.0035f, FogHeight = 14f, Falloff = 0.05f, Scatter = 0.35f, CloudCover = 0.35f, Saturation = 4f, TargetSat = 0.22f, Exposure = -0.1f, Contrast = 18f, SplitS = "#5868b0", SplitH = "#f0d8e0", Aurora = true,
                Weathers = new[] { WeatherState.Klar, WeatherState.Schnee, WeatherState.Schneesturm, WeatherState.Bewoelkt }, Fx = new[] { AmbientFxKind.Schneeflocken, AmbientFxKind.Windstreifen, AmbientFxKind.Lichtstaub, AmbientFxKind.Voegel } },
            ["glut"] = new BiomeArt { Id = "glut", Ground = "#2a2224", Veg = "#5a3226", Sky = "#6a4a44", Accent = "#e8c840", Hazard = "#ff6a18", Light = "#ffb070", Shadow = "#3a2848", Zenith = "#2a1e22", Horizon = "#a86450", SkyGround = "#3a2a26", Fog = "#7a5448", Cloud = "#8a6a60", Outline = "#100808",
                Az = 250, Elev = 40, Intensity = 1.05f, FogDensity = 0.0032f, FogHeight = 5f, Falloff = 0.1f, Scatter = 0.6f, CloudCover = 0.7f, Saturation = 10f, TargetSat = 0.48f, Under = 0.45f, Bloom = 0.8f, BloomThreshold = 1.0f, SplitS = "#3a2848", SplitH = "#ff9a58", Stars = false, Ash = true,
                Weathers = new[] { WeatherState.Aschefall, WeatherState.Klar, WeatherState.Bewoelkt }, Fx = new[] { AmbientFxKind.Glut, AmbientFxKind.Asche, AmbientFxKind.Rauch, AmbientFxKind.Windstreifen } },
            ["kueste"] = new BiomeArt { Id = "kueste", Ground = "#e2cf9a", Veg = "#3e8a5a", Sky = "#62b8e8", Accent = "#e86a5a", Hazard = "#7a4a9a", Light = "#fff6e0", Shadow = "#4a7aa8", Zenith = "#2a8ad8", Horizon = "#d8f0f4", SkyGround = "#a8c8c0", Fog = "#c8e4ec", Cloud = "#ffffff", Outline = "#1a2a2a",
                Az = 120, Elev = 68, Intensity = 1.45f, FogDensity = 0.0025f, FogHeight = 6f, Falloff = 0.09f, Scatter = 0.4f, CloudCover = 0.35f, Saturation = 16f, TargetSat = 0.5f, WaterShallow = "#40d0c8", WaterDeep = "#0a4a78",
                Weathers = new[] { WeatherState.Klar, WeatherState.Bewoelkt, WeatherState.Regen, WeatherState.Gewitter }, Fx = new[] { AmbientFxKind.Voegel, AmbientFxKind.Windstreifen, AmbientFxKind.Tropfen, AmbientFxKind.Schmetterlinge } },
            ["wueste"] = new BiomeArt { Id = "wueste", Ground = "#e0b06a", Veg = "#b8603a", Sky = "#8ac0e0", Accent = "#3ac0c0", Hazard = "#c04a9a", Light = "#fff0c8", Shadow = "#7a5aa8", Zenith = "#4a90d0", Horizon = "#f0dcb8", SkyGround = "#c8a070", Fog = "#e8d0a8", Cloud = "#fff4e0", Outline = "#2a1a10",
                Az = 100, Elev = 72, Intensity = 1.3f, FogDensity = 0.0022f, FogHeight = 4f, Falloff = 0.12f, Scatter = 0.5f, CloudCover = 0.15f, Saturation = 14f, TargetSat = 0.5f, Exposure = -0.05f, SplitS = "#7a5aa8", SplitH = "#ffd890", Sand = true,
                Weathers = new[] { WeatherState.Klar, WeatherState.Sandsturm, WeatherState.Bewoelkt }, Fx = new[] { AmbientFxKind.Sand, AmbientFxKind.Windstreifen, AmbientFxKind.Voegel, AmbientFxKind.Lichtstaub } },
            ["kristall"] = new BiomeArt { Id = "kristall", Ground = "#2a2440", Veg = "#5a3a8a", Sky = "#18183a", Accent = "#40e0f0", Hazard = "#d050f0", Light = "#b8a0ff", Shadow = "#1a1a48", Zenith = "#0a0a20", Horizon = "#2a2858", SkyGround = "#141428", Fog = "#2a2a58", Cloud = "#3a3a6a", Outline = "#08060f",
                Az = 200, Elev = 50, Intensity = 0.7f, FogDensity = 0.009f, FogHeight = 4f, Falloff = 0.12f, Scatter = 0.8f, CloudCover = 0f, Saturation = 18f, TargetSat = 0.55f, Exposure = 0.3f, Bloom = 0.9f, BloomThreshold = 0.95f, SplitS = "#1a1a58", SplitH = "#80f0ff", Stars = false, Underground = true,
                Weathers = new[] { WeatherState.Klar, WeatherState.Nieselnebel }, Fx = new[] { AmbientFxKind.Kristallpuls, AmbientFxKind.Lichtstaub, AmbientFxKind.Tropfen, AmbientFxKind.Sporen } },
            ["himmel"] = new BiomeArt { Id = "himmel", Ground = "#d8dce4", Veg = "#7a8aa0", Sky = "#a8c8f0", Accent = "#e8c060", Hazard = "#9a5ad0", Light = "#ffffff", Shadow = "#7a7ac8", Zenith = "#2a62c8", Horizon = "#e0e0f8", SkyGround = "#e8ecf4", Fog = "#e4e8f8", Cloud = "#ffffff", Outline = "#202638",
                Az = 150, Elev = 62, Intensity = 1.05f, FogDensity = 0.0018f, FogHeight = 20f, Falloff = 0.03f, Scatter = 0.3f, CloudCover = 0.55f, Saturation = 8f, TargetSat = 0.3f, Exposure = -0.1f, Contrast = 16f, SplitS = "#6a6ac0", SplitH = "#ffe8b0", Wind = 1f,
                Weathers = new[] { WeatherState.Klar, WeatherState.Bewoelkt, WeatherState.Schnee }, Fx = new[] { AmbientFxKind.Windstreifen, AmbientFxKind.Voegel, AmbientFxKind.Lichtstaub, AmbientFxKind.Schneeflocken } },
            ["jade"] = new BiomeArt { Id = "jade", Ground = "#3a6a52", Veg = "#2a5a3a", Sky = "#8ad0b8", Accent = "#e8c048", Hazard = "#e040a0", Light = "#e8ffe8", Shadow = "#2a3a5a", Zenith = "#2a7a8a", Horizon = "#c8ecd8", SkyGround = "#4a6a5a", Fog = "#a8d8c4", Cloud = "#e8f8f0", Outline = "#0a1a12",
                Az = 130, Elev = 50, Intensity = 1.25f, FogDensity = 0.0042f, FogHeight = 8f, Falloff = 0.08f, Scatter = 0.55f, CloudCover = 0.55f, Saturation = 14f, TargetSat = 0.46f, Bloom = 0.65f, SplitS = "#2a3a6a", SplitH = "#d8f0a0", WaterShallow = "#50e0b0", WaterDeep = "#0a4a48",
                Weathers = new[] { WeatherState.Jaderegen, WeatherState.Klar, WeatherState.Bewoelkt, WeatherState.Regen }, Fx = new[] { AmbientFxKind.Jadestaub, AmbientFxKind.Tropfen, AmbientFxKind.Gluehwuermchen, AmbientFxKind.Blaetter } },
            ["verderbnis"] = new BiomeArt { Id = "verderbnis", Ground = "#3a3040", Veg = "#5a4a6a", Sky = "#6a4a8a", Accent = "#a0e040", Hazard = "#b040e0", Light = "#d8c0ff", Shadow = "#2a1a3a", Zenith = "#2a1a4a", Horizon = "#8a6a9a", SkyGround = "#2a2030", Fog = "#5a4868", Cloud = "#7a6a8a", Outline = "#0c0812",
                Az = 230, Elev = 38, Intensity = 1.0f, FogDensity = 0.006f, FogHeight = 4f, Falloff = 0.14f, Scatter = 0.6f, CloudCover = 0.75f, Saturation = 8f, TargetSat = 0.38f, Contrast = 16f, SplitS = "#2a1a4a", SplitH = "#c0f080", Stars = false,
                Weathers = new[] { WeatherState.Aschefall, WeatherState.Gewitter, WeatherState.Bewoelkt }, Fx = new[] { AmbientFxKind.Sporen, AmbientFxKind.Asche, AmbientFxKind.Rauch, AmbientFxKind.Lichtstaub } },
        };

        /// <summary>Zone → Biom und Hue-Verschiebung (Grad) – keine zwei Zonen teilen eine Palette.</summary>
        public static readonly (string zone, string biome, float hue)[] ZoneBiomes =
        {
            ("sonnfurt", "wiese", 0), ("weite_von_ahrn", "wiese", 22), ("gruener_schlund", "wiese", -24),
            ("nebelmoor", "moor", 0), ("daemmerwald", "moor", 24), ("nebelkamm", "moor", -22), ("schilfmuendung", "moor", 40),
            ("frostkamm", "frost", 0), ("weisse_stille", "frost", 22),
            ("gluetoede", "glut", 0), ("aschenkessel", "glut", 20), ("glutkessel", "glut", -20),
            ("silberkueste", "kueste", 0), ("sturmbucht", "kueste", 22), ("nebelkueste", "kueste", -24),
            ("duenenmeer", "wueste", 0), ("grosse_leere", "wueste", 20), ("klingenschlucht", "wueste", -20), ("spiegel_der_sonne", "wueste", 36), ("sonnentor", "wueste", -36),
            ("kristalltiefe", "kristall", 0), ("himmelsspitzen", "himmel", 0),
            ("jadehafen", "jade", 0), ("gruene_flut", "jade", 22), ("fluesterhain", "jade", -22), ("sonnenstufen", "jade", 38), ("herz_der_flut", "jade", -38),
            ("tyrannenfeste", "verderbnis", 0), ("schaedelkueste", "verderbnis", 24), ("zarathis", "verderbnis", -24),
        };

        static Color Shift(Color c, float deg)
        {
            if (Mathf.Abs(deg) < 0.01f) return c;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(Mathf.Repeat(h + deg / 360f, 1f), s, v);
        }

        [MenuItem("Project Tyrant/Grafik G1/Profile, Elemente, Texturen")]
        public static void BuildAll() { BuildProfiles(); BuildElements(); BuildTextures(); AssetDatabase.SaveAssets(); BuildRendering(); MaterialMigrator.MigrateAll(); Debug.Log("[ProjectTyrant] Grafik G1: " + string.Join(" · ", LastReport)); }

        public static List<ZoneArtProfile> BuildProfiles()
        {
            EnsureFolder(Dir); EnsureFolder(ProfileDir);
            var list = new List<ZoneArtProfile>();
            foreach (var (zone, biome, hue) in ZoneBiomes)
            {
                var b = Biomes[biome];
                string path = ProfileDir + "/ZoneArt_" + zone + ".asset";
                var p = AssetDatabase.LoadAssetAtPath<ZoneArtProfile>(path);
                if (p == null) { p = ScriptableObject.CreateInstance<ZoneArtProfile>(); AssetDatabase.CreateAsset(p, path); }
                p.ZoneId = zone; p.Biome = biome;
                Color S(string hex) => Shift(H(hex), hue);
                p.Ground = S(b.Ground); p.VegetationRock = S(b.Veg); p.Sky = S(b.Sky); p.Accent = S(b.Accent); p.Hazard = H(b.Hazard);
                p.LightColor = H(b.Light); p.ShadowColor = S(b.Shadow);
                p.SunAzimuth = b.Az + hue * 0.5f; p.SunNoonElevation = b.Elev; p.SunIntensity = b.Intensity; p.UnderLight = b.Under;
                p.SkyZenith = S(b.Zenith); p.SkyHorizon = S(b.Horizon); p.SkyGround = S(b.SkyGround); p.CloudColor = H(b.Cloud); p.CloudCover = b.CloudCover;
                p.AmbientSky = Color.Lerp(p.SkyZenith, Color.white, 0.35f); p.AmbientEquator = Color.Lerp(p.SkyHorizon, p.ShadowColor, 0.25f); p.AmbientGround = Color.Lerp(p.Ground, p.ShadowColor, 0.5f) * 0.8f;
                if (b.Under > 0f) p.AmbientGround = Color.Lerp(p.AmbientGround, p.Hazard, b.Under);
                p.Stars = b.Stars; p.Aurora = b.Aurora; p.AshSky = b.Ash; p.SandVeil = b.Sand; p.Underground = b.Underground;
                p.FogColor = S(b.Fog); p.FogDensity = b.FogDensity; p.FogHeight = b.FogHeight; p.FogHeightFalloff = b.Falloff; p.SunScatter = b.Scatter;
                p.Exposure = b.Exposure; p.Contrast = b.Contrast; p.Saturation = b.Saturation; p.TargetSaturation = b.TargetSat; p.WhiteBalance = b.WB;
                p.SplitShadows = S(b.SplitS); p.SplitHighlights = H(b.SplitH); p.BloomThreshold = b.BloomThreshold; p.BloomIntensity = b.Bloom; p.Vignette = b.Vignette;
                p.OutlineColor = S(b.Outline); p.WindStrength = b.Wind; p.WaterShallow = S(b.WaterShallow); p.WaterDeep = S(b.WaterDeep);
                p.Weathers = new List<WeatherState>(b.Weathers); p.AmbientFx = new List<AmbientFxKind>(b.Fx);
                EditorUtility.SetDirty(p); list.Add(p);
            }
            LastReport.Add(list.Count + " Kunstprofile");
            return list;
        }

        public static ZoneArtProfile Profile(string zoneId) => AssetDatabase.LoadAssetAtPath<ZoneArtProfile>(ProfileDir + "/ZoneArt_" + zoneId + ".asset");

        static readonly (FxElement el, string p, string s, string core, float coreBoost, FlipbookShape shape, ElementMotion motion, float tw, float tt, float jit, bool shock, bool shards, bool sparks, string after, string sound, string desc)[] ElementTable =
        {
            (FxElement.Feuer, "#ff6a1a", "#ffc23a", "#fff0b0", 3.0f, FlipbookShape.Feuer, ElementMotion.Steigend, 0.35f, 0.3f, 0.05f, true, false, true, "brennend", "Feuer", "steigt und flackert, Glut als Nachwirkung"),
            (FxElement.Eis, "#7ad8ff", "#e8faff", "#ffffff", 2.2f, FlipbookShape.Schneeflocke, ElementMotion.Fallend, 0.25f, 0.4f, 0f, true, true, false, "vereist", "Frost", "kantig und still, Frostfläche"),
            (FxElement.Blitz, "#a8b8ff", "#ffffff", "#f0f4ff", 4.0f, FlipbookShape.Blitz, ElementMotion.Zuckend, 0.12f, 0.12f, 0.35f, true, false, true, "elektrisiert", "Gewitter", "verzweigt und hart, Funken"),
            (FxElement.Natur, "#5ad84a", "#c8f070", "#f0ffd0", 2.0f, FlipbookShape.Blatt, ElementMotion.Wachsend, 0.3f, 0.45f, 0.05f, false, false, false, "vergiftet", "Natur", "wächst und wiegt, Ranken und Blätter"),
            (FxElement.Schatten, "#5a3a8a", "#2a1a3a", "#c090ff", 1.8f, FlipbookShape.Rauch, ElementMotion.Saugend, 0.4f, 0.5f, 0.1f, false, false, false, "verderbt", "Schatten", "saugt und verwischt, dunkle Schleier"),
            (FxElement.Licht, "#ffe07a", "#fff8e0", "#ffffff", 3.5f, FlipbookShape.Funke, ElementMotion.Strahlend, 0.3f, 0.3f, 0f, true, false, true, "", "Licht", "strahlt und blüht, Lichtsäulen"),
            (FxElement.Jade, "#3ae0a0", "#b0ffe0", "#e8fff4", 2.4f, FlipbookShape.Glyphe, ElementMotion.Traege, 0.35f, 0.6f, 0f, true, true, false, "", "Jade", "gläsern und träge, Splitter"),
            (FxElement.Sand, "#e0b060", "#f8e0a0", "#fff4d0", 1.8f, FlipbookShape.Rauch, ElementMotion.Stroemend, 0.45f, 0.5f, 0.15f, true, false, false, "", "Erde", "strömt und flirrt, Staubschleier"),
            (FxElement.Chaos, "#e040a0", "#40e0e0", "#ffffff", 3.0f, FlipbookShape.Explosion, ElementMotion.Schillernd, 0.3f, 0.35f, 0.4f, true, true, true, "verderbt", "Zwielicht", "unregelmäßig und schillernd"),
            (FxElement.Arkan, "#c060ff", "#80c0ff", "#f8e8ff", 2.8f, FlipbookShape.Glyphe, ElementMotion.Wirbelnd, 0.3f, 0.35f, 0.05f, true, false, true, "", "Arkan", "wirbelt in Glyphen, Runenringe"),
        };

        public static List<ElementPalette> BuildElements()
        {
            EnsureFolder(Dir); EnsureFolder(ElementDir);
            var list = new List<ElementPalette>();
            foreach (var e in ElementTable)
            {
                string path = ElementDir + "/Element_" + e.el + ".asset";
                var p = AssetDatabase.LoadAssetAtPath<ElementPalette>(path);
                if (p == null) { p = ScriptableObject.CreateInstance<ElementPalette>(); AssetDatabase.CreateAsset(p, path); }
                p.Element = e.el; p.Primary = H(e.p); p.Secondary = H(e.s); p.Core = H(e.core) * e.coreBoost; p.Particle = e.shape; p.Motion = e.motion;
                p.TrailWidth = e.tw; p.TrailTime = e.tt; p.TrailJitter = e.jit; p.ImpactShockwave = e.shock; p.ImpactShards = e.shards; p.ImpactSparks = e.sparks;
                p.AfterOverlay = e.after; p.SoundFamily = e.sound; p.Description = e.desc;
                EditorUtility.SetDirty(p); list.Add(p);
            }
            LastReport.Add(list.Count + " Elemente");
            return list;
        }

        public static ElementPalette Element(FxElement e) => AssetDatabase.LoadAssetAtPath<ElementPalette>(ElementDir + "/Element_" + e + ".asset");
    }
}
