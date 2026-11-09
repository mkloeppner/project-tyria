using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Procedural
{
    /// <summary>
    /// Reine Layout-Daten des Zonenabschlusses (Builder und Tests lesen dieselben Zahlen):
    /// 5 Wegmarken, 5 Sehenswürdigkeiten, 3 Aussichtspunkte mit Treppen, 4 Heldenherausforderungen, 3 Herzen.
    /// </summary>
    public static class ZoneContent
    {
        public struct Spot { public string Id, Name, Desc; public Vector2 Pos; public Spot(string id, string name, Vector2 pos, string desc = "") { Id = id; Name = name; Pos = pos; Desc = desc; } }

        public static readonly Spot[] Waypoints =
        {
            new Spot("wp_sonnfurt", "Wegmarke Sonnfurt", new Vector2(-54, -54), "Zentrale Wegmarke im Weiler Sonnfurt."),
            new Spot("wp_gerstenbrunn", "Wegmarke Gerstenbrunn", new Vector2(-8, -44), "Am Hof Gerstenbrunn."),
            new Spot("wp_dornwacht", "Wegmarke Dornwacht", new Vector2(-15, -3), "Vor dem Grenzposten. Während der Dornwacht-Kette umkämpft."),
            new Spot("wp_elmhain", "Wegmarke Elmhain", new Vector2(40, -50), "Am Rand der Ruinen von Elmhain."),
            new Spot("wp_spiegelsee", "Wegmarke Spiegelsee", new Vector2(-25, 52), "Am Ufer des Spiegelsees."),
        };

        public static Spot[] Pois => new[]
        {
            new Spot("poi_sonnfurt", ZoneLayout.Hub.Name, ZoneLayout.Hub.Center, "Weiler mit Händlern und Leuchtfeuer."),
            new Spot("poi_gerstenbrunn", ZoneLayout.Farm.Name, ZoneLayout.Farm.Center, "Gehöft vor den Toren Sonnfurts."),
            new Spot("poi_dornwacht", ZoneLayout.Outpost.Name, ZoneLayout.Outpost.Center, "Befestigter Grenzposten."),
            new Spot("poi_elmhain", ZoneLayout.Ruins.Name, ZoneLayout.Ruins.Center, "Verfallene Ruinen voller Relikte."),
            new Spot("poi_rabenschanze", ZoneLayout.Camp.Name, ZoneLayout.Camp.Center, "Lager der Aschkrallen."),
        };
        public const float PoiRadius = 12f;
        public const float WaypointDiscoverRadius = 10f;

        public struct VistaSpot { public string Id, Name; public Vector2 Pos, StairDir; public float Height; }
        public static readonly VistaSpot[] Vistas =
        {
            new VistaSpot { Id = "vista_leuchtfeuer", Name = "Ausguck am Leuchtfeuer", Pos = new Vector2(-34, -60), StairDir = new Vector2(1, 0), Height = 4.5f },
            new VistaSpot { Id = "vista_elmhain", Name = "Felsturm vor Elmhain", Pos = new Vector2(47, -27), StairDir = new Vector2(0, 1), Height = 5f },
            new VistaSpot { Id = "vista_dornwacht", Name = "Wachturm der Dornwacht", Pos = new Vector2(27, 5), StairDir = new Vector2(1, 0), Height = 4.5f },
        };
        public const float StairRise = 0.45f, StairTread = 0.9f, PlatformHalf = 1.6f;

        public enum ChallengeKind { Communion, Combat, Relic }
        public struct ChallengeSpot { public string Id, Name, Desc; public Vector2 Pos; public ChallengeKind Kind; public float PillarHeight; }
        public static readonly ChallengeSpot[] Challenges =
        {
            new ChallengeSpot { Id = "hc_schrein_sonnfurt", Name = "Schrein der Ahnen", Pos = new Vector2(-50, -36), Kind = ChallengeKind.Communion, PillarHeight = 2.7f, Desc = "Auf einem Felsen – nur springend erreichbar. Kommuniziere 5 s." },
            new ChallengeSpot { Id = "hc_herausforderer", Name = "Ruf des Herausforderers", Pos = new Vector2(-23, -3), Kind = ChallengeKind.Combat, Desc = "Rufe einen Veteranen herbei und besiege ihn." },
            new ChallengeSpot { Id = "hc_relikt_elmhain", Name = "Verfluchtes Relikt", Pos = new Vector2(27, -42), Kind = ChallengeKind.Relic, Desc = "Berühre das Relikt und überstehe den Fluch (3 Gegner, Zustandsdruck)." },
            new ChallengeSpot { Id = "hc_schrein_see", Name = "Quellschrein am See", Pos = new Vector2(-24, 61), Kind = ChallengeKind.Communion, Desc = "Kommuniziere 5 s mit der Quelle." },
        };
        public const int ChallengeHeroPoints = 10;
        public const float JumpStepHeight = 0.9f;

        public enum TaskKind { Pickup, Repair, Bandage }
        public struct HeartTaskSpot { public TaskKind Kind; public Vector2 Pos; }
        public struct HeartSpot
        {
            public string Id, Name, Npc, Task, VendorId; public Vector2 Center, NpcPos; public float Radius, KillValue, TaskValue;
            public HeartTaskSpot[] Tasks; public Vector2[] ExtraSpawns;
        }
        public static readonly HeartSpot[] Hearts =
        {
            new HeartSpot { Id = "heart_gerstenbrunn", Name = "Hilf Bäuerin Wilma", Npc = "Bäuerin Wilma", Task = "Vertreibe Plünderer, sammle Vorratskisten ein und repariere Zäune.",
                Center = ZoneLayout.Farm.Center, NpcPos = new Vector2(-14, -55), Radius = 24f, KillValue = 0.08f, TaskValue = 0.08f, VendorId = "karma_wilma",
                Tasks = new[] { T(TaskKind.Pickup, -2, -66), T(TaskKind.Pickup, -16, -64), T(TaskKind.Pickup, 2, -55), T(TaskKind.Pickup, -12, -70), T(TaskKind.Repair, 6, -62), T(TaskKind.Repair, -20, -58), T(TaskKind.Repair, -4, -50) },
                ExtraSpawns = new[] { new Vector2(10, -60), new Vector2(-25, -53), new Vector2(5, -47) } },
            new HeartSpot { Id = "heart_elmhain", Name = "Unterstütze Archivar Bertold", Npc = "Archivar Bertold", Task = "Säubere die Ruinen von Aschkrallen und berge Relikte.",
                Center = ZoneLayout.Ruins.Center, NpcPos = new Vector2(50, -46), Radius = 22f, KillValue = 0.07f, TaskValue = 0.11f, VendorId = "karma_bertold",
                Tasks = new[] { T(TaskKind.Pickup, 62, -50), T(TaskKind.Pickup, 54, -62), T(TaskKind.Pickup, 66, -60), T(TaskKind.Pickup, 52, -52) },
                ExtraSpawns = new[] { new Vector2(64, -66), new Vector2(68, -48) } },
            new HeartSpot { Id = "heart_dornwacht", Name = "Hilf Hauptfrau Kerla", Npc = "Hauptfrau Kerla", Task = "Repariere die Palisade und bandagiere verletzte Wachen.",
                Center = ZoneLayout.Outpost.Center, NpcPos = new Vector2(-2, 12), Radius = 22f, KillValue = 0.05f, TaskValue = 0.12f, VendorId = "karma_kerla",
                Tasks = new[] { T(TaskKind.Repair, 18, 12), T(TaskKind.Repair, -6, 12), T(TaskKind.Repair, 14, -8), T(TaskKind.Bandage, 2, 0), T(TaskKind.Bandage, 10, 8) },
                ExtraSpawns = new Vector2[0] },
        };
        static HeartTaskSpot T(TaskKind k, float x, float z) => new HeartTaskSpot { Kind = k, Pos = new Vector2(x, z) };

        /// <summary>Lauf 4: Höhenquelle für Treppen/Säulen (Biom-Zonen setzen ihren Sampler, sonst Startzonen-Layout).</summary>
        public static System.Func<float, float, float> HeightOverride;
        static float Hgt(float x, float z) => HeightOverride != null ? HeightOverride(x, z) : ZoneLayout.Height(x, z);

        /// <summary>Treppenstufen (Oberkante) zu einer Aussichts-Plattform: Stufe 0 = unterste, letzte = Plattformhöhe.</summary>
        public static List<Vector3> StairSteps(VistaSpot v)
        {
            float top = PlatformTop(v).y;
            var dir = v.StairDir.normalized;
            // Stufenzahl iterativ an den Boden am Treppenfuß anpassen (Hanglage): gleichmäßige Stufen ≤ StairRise.
            int n = Mathf.CeilToInt(v.Height / StairRise);
            float foot = 0f;
            for (int it = 0; it < 4; it++)
            {
                var far = v.Pos + dir * (PlatformHalf + StairTread * (n - 0.5f));
                foot = Hgt(far.x, far.y);
                n = Mathf.Max(1, Mathf.CeilToInt((top - foot) / StairRise));
            }
            var foot0 = v.Pos + dir * (PlatformHalf + StairTread * (n - 0.5f));
            foot = Hgt(foot0.x, foot0.y);
            float rise = (top - foot) / (n + 1);
            var steps = new List<Vector3>(n);
            for (int i = 0; i < n; i++)
            {
                var p = v.Pos + dir * (PlatformHalf + StairTread * (n - i - 0.5f));
                steps.Add(new Vector3(p.x, foot + rise * (i + 1), p.y));
            }
            return steps;
        }

        public static Vector3 PlatformTop(VistaSpot v) => new Vector3(v.Pos.x, Hgt(v.Pos.x, v.Pos.y) + v.Height, v.Pos.y);

        /// <summary>Größter Höhensprung entlang des Weges Boden → Stufen → Plattform (muss ≤ 1 m sein).</summary>
        public static float MaxStepRise(VistaSpot v)
        {
            var steps = StairSteps(v);
            float max = steps[0].y - Hgt(steps[0].x, steps[0].z);
            for (int i = 1; i < steps.Count; i++) max = Mathf.Max(max, steps[i].y - steps[i - 1].y);
            max = Mathf.Max(max, PlatformTop(v).y - steps[steps.Count - 1].y);
            return max;
        }

        /// <summary>Felsstufen zum Kommunions-Schrein (nur springend: 0,9 m je Stufe).</summary>
        public static List<Vector3> PillarSteps(ChallengeSpot c)
        {
            var list = new List<Vector3>();
            if (c.PillarHeight <= 0f) return list;
            float g = Hgt(c.Pos.x, c.Pos.y);
            int n = Mathf.CeilToInt(c.PillarHeight / JumpStepHeight);
            for (int i = 0; i < n - 1; i++)
            {
                float ang = i * 70f * Mathf.Deg2Rad;
                var p = c.Pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 2.6f;
                list.Add(new Vector3(p.x, g + c.PillarHeight * (i + 1) / n, p.y));
            }
            return list;
        }

        /// <summary>Flächen, die die Vegetation frei halten muss.</summary>
        public static IEnumerable<(Vector2 center, float radius)> Footprints()
        {
            foreach (var w in Waypoints) yield return (w.Pos, 3f);
            foreach (var v in Vistas) { yield return (v.Pos, 3f); yield return (v.Pos + v.StairDir.normalized * (PlatformHalf + v.Height / StairRise * StairTread * 0.5f), v.Height / StairRise * StairTread * 0.5f + 1.5f); }
            foreach (var c in Challenges) yield return (c.Pos, 4f);
            foreach (var h in Hearts) yield return (h.NpcPos, 4f);
        }
    }
}
