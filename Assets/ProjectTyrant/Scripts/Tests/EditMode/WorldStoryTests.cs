using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEditor;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 6 / M29: Späher und Herzen je Zone (Anzahl, Szenen, Dankesbriefe, Vielfalt der Beitragswege) aus world.csv und den Zonen-Daten.</summary>
    public class WorldStoryTests
    {
        static readonly string[] Zones = { "sonnfurt", "nebelmoor", "frostkamm", "gluetoede", "silberkueste", "daemmerwald", "duenenmeer", "kristalltiefe", "himmelsspitzen", "tyrannenfeste" };   // Lauf 6 / M30+M32: alle zehn Zonen

        static List<ZoneObjectDefinition> Hearts(string zone) =>
            AssetDatabase.FindAssets("t:ZoneObjectDefinition", new[] { "Assets/ProjectTyrant/Data/Zone" }).Select(g => AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null && d.Kind == MapMarkerKind.Heart && WorldBuilder.Zone(zone) != null && WorldBuilder.Zone(zone).CompletionIds.Contains(d.Id)).ToList();

        [Test]
        public void EveryZone_Has3To5Scouts_WithScene()
        {
            var db = StoryBuilder.Load(); var rows = WorldStoryBuilder.Read();
            foreach (var z in Zones)
            {
                var scouts = rows.Where(r => r.Zone == z && r.Type == "Scout").ToList();
                Assert.That(scouts.Count, Is.InRange(3, 5), z);
                foreach (var s in scouts) Assert.IsNotNull(db.Scene("SP_" + s.Id), "Szene SP_" + s.Id);
            }
        }

        [Test]
        public void EveryZone_HasAtLeast4Hearts_EachWithVoiceLineAndThankYouLetter()
        {
            var db = StoryBuilder.Load(); var missing = new List<string>();
            foreach (var z in Zones)
            {
                var hearts = Hearts(z);
                Assert.GreaterOrEqual(hearts.Count, 4, z + ": " + string.Join(",", hearts.Select(h => h.Id)));
                foreach (var h in hearts)
                {
                    if (db.Scene("HZ_" + h.Id) == null) missing.Add("HZ_" + h.Id);
                    if (db.Letter("Dank_" + h.Id) == null) missing.Add("Dank_" + h.Id);
                }
            }
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [Test]
        public void Hearts_OfferAtLeast8DifferentWays()
        {
            // Bestand: Gegner (Kill), Sammeln, Reparieren (Use), Verbinden (Revive); neu aus world.csv: Tiere, Fallen, Bündel, Emote; Kombo überall
            var ways = new HashSet<HeartWay> { HeartWay.Kill, HeartWay.Collect, HeartWay.Use, HeartWay.Revive, HeartWay.Combo };
            foreach (var r in WorldStoryBuilder.Read().Where(r => r.Type == "Heart"))
            {
                if (r.Extra.TryGetValue("tasks", out var t)) foreach (var p in t.Split(',')) if (System.Enum.TryParse(p.Split(':')[0], out HeartWay w)) ways.Add(w);
                if (r.Extra.ContainsKey("bundle")) ways.Add(HeartWay.Bundle);
                if (r.Extra.ContainsKey("emote")) ways.Add(HeartWay.Emote);
            }
            Assert.GreaterOrEqual(ways.Count, 8, string.Join(",", ways));
        }

        [Test]
        public void WorldSpeakers_AreInCast_AndHaveNoStoryFigure()
        {
            var db = StoryBuilder.Load();
            foreach (var r in WorldStoryBuilder.Read().Where(r => r.Type != "Heart" || r.Npc != "mira"))
            {
                var sp = db.Cast.Get(r.Npc); Assert.IsNotNull(sp, r.Npc);
                if (sp.Look == "world") Assert.IsNull(db.Figure(r.Npc), r.Npc + " braucht keine Story-Figur");
            }
        }
    }
}
