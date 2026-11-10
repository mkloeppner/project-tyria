using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Gebiet mit fester Gefahrenstufe (Region der Zone oder Sonderfläche).</summary>
    [System.Serializable]
    public class DangerArea
    {
        public string Name;
        public Vector2 Center;
        public float Radius;
        public int Level;
        [Tooltip("Stadt: keine Gegner, keine Spawns")] public bool City;
        [Tooltip("Festungsinneres: keine Leine")] public bool NoLeash;
    }

    /// <summary>
    /// Balance B1: Gefahrenkarte einer Zone (rein). Gebiete haben feste Stufen; außerhalb gilt die Stufe des Entfernungsbandes um Hub und Ankunfts-Wegmarken.
    /// Entfernungsregeln des Auftrags (300 m / 700 m / 100 m) beziehen sich auf eine 1.000-m-Zone und werden mit <see cref="Scale"/> auf die Zonengröße umgerechnet.
    /// </summary>
    [System.Serializable]
    public class ZoneDangerMap
    {
        public string ZoneId;
        public float Size = 1000f;
        public List<Vector2> Anchors = new List<Vector2>();          // Hub + Ankunfts-Wegmarken
        public List<Vector2> Waypoints = new List<Vector2>();
        public List<float> BandEdges = new List<float>();            // Außenkante je Band (m)
        public List<int> BandLevels = new List<int>();               // Stufe je Band; letzter Eintrag gilt dahinter
        public List<DangerArea> Areas = new List<DangerArea>();
        public bool IsStartZone;
        public float HubSafeRadius = 30f, WaypointSafeRadius = 25f;

        public static float Scale(float size) => Mathf.Clamp(size / 1000f, 0.2f, 1.2f);
        public float S => Scale(Size);
        /// <summary>„keine Spawns in Stufe 0 innerhalb von 100 m um Wegmarken“, skaliert.</summary>
        public float NoSpawnRadius => 100f * S;
        /// <summary>Events spawnen nie innerhalb von 100 m (skaliert) um Wegmarke/Hub.</summary>
        public float EventSpawnClearance => 100f * S;

        public float AnchorDistance(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var a in Anchors) best = Mathf.Min(best, Vector2.Distance(a, p));
            return Anchors.Count == 0 ? 0f : best;
        }

        public int BandAt(Vector2 p)
        {
            if (BandLevels.Count == 0) return 0;
            float d = AnchorDistance(p);
            for (int i = 0; i < BandEdges.Count && i < BandLevels.Count; i++) if (d <= BandEdges[i]) return BandLevels[i];
            return BandLevels[BandLevels.Count - 1];
        }

        public DangerArea AreaAt(Vector2 p)
        {
            DangerArea best = null; float bd = float.MaxValue;
            foreach (var a in Areas)
            {
                float d = Vector2.Distance(a.Center, p);
                if (d <= a.Radius && d < bd) { bd = d; best = a; }
            }
            return best;
        }

        public int At(Vector2 p) { var a = AreaAt(p); return a != null ? a.Level : BandAt(p); }
        public int At(Vector3 p) => At(new Vector2(p.x, p.z));
        public bool IsCity(Vector2 p) { var a = AreaAt(p); return a != null && a.City; }
        public bool NoLeashAt(Vector2 p) { var a = AreaAt(p); return a != null && a.NoLeash; }

        /// <summary>Sicherheitszone (Deliverable 2): Hub und jede Wegmarke – keine Spawns, Gegner verlieren die Aggro.</summary>
        public bool InSafezone(Vector2 p)
        {
            if (IsCity(p)) return true;
            if (IsStartZone && Anchors.Count > 0 && Vector2.Distance(Anchors[0], p) <= HubSafeRadius) return true;
            foreach (var w in Waypoints) if (Vector2.Distance(w, p) <= WaypointSafeRadius) return true;
            return false;
        }

        public bool SpawnAllowed(Vector2 p)
        {
            if (InSafezone(p)) return false;
            if (At(p) == 0) foreach (var w in Waypoints) if (Vector2.Distance(w, p) < NoSpawnRadius) return false;
            return true;
        }
    }

    /// <summary>Balance B1: Zuordnung der Gefahrenstufen je Zone (Daten, vom Builder in die ZoneDefinition geschrieben).</summary>
    public static class ZoneDangerTable
    {
        public struct Spec { public int Entry, Core; public float CoreAt; public string[] Cities, NoLeash; public (string name, int level)[] Overrides; }

        public static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            { "nebelmoor",     new Spec { Entry = 2, Core = 3, CoreAt = 0.33f } },
            { "frostkamm",     new Spec { Entry = 3, Core = 4, CoreAt = 0.33f } },
            { "silberkueste",  new Spec { Entry = 3, Core = 3, CoreAt = 0.33f, Cities = new[] { "Portalplatz", "Bündnis-Viertel", "Markt der hundert Segel" } } },
            { "gluetoede",     new Spec { Entry = 4, Core = 4, CoreAt = 0.33f } },
            { "daemmerwald",   new Spec { Entry = 4, Core = 5, CoreAt = 0.35f } },
            { "duenenmeer",    new Spec { Entry = 4, Core = 4, CoreAt = 0.33f } },
            { "kristalltiefe", new Spec { Entry = 5, Core = 5, CoreAt = 0.33f } },
            { "himmelsspitzen",new Spec { Entry = 4, Core = 4, CoreAt = 0.33f, Overrides = new[] { ("Greifennester", 5), ("Höchster Gipfel", 5) } } },
            { "tyrannenfeste", new Spec { Entry = 5, Core = 5, CoreAt = 0.33f, NoLeash = new[] { "Innerer Hof", "Siegelkammern", "Thronsaal" } } },
        };

        /// <summary>
        /// Startzone (Grenzmark Ashveil, 200 m): Bänder 0/1/2 um Sonnfurt und die Ankunfts-Wegmarke, Gebiete nach Auftrag. Bandkanten 45/120 m statt skalierter 60/140 m,
        /// damit zwischen Stufe 0 und dem Grenzposten (Stufe 2) überall ein Streifen Stufe 1 liegt (Weg zur Dornwacht = Stufe 1). Radius 0 = Gebietsradius.
        /// </summary>
        public static readonly (string name, int level, float radius)[] StartRegions =
        {
            ("Weiler Sonnfurt", 0, 0), ("Hof Gerstenbrunn", 0, 0), ("Spiegelsee", 1, 0), ("Ruinen von Elmhain", 2, 0), ("Grenzposten Dornwacht", 2, 26), ("Rabenschanze", 2, 0),
        };
        public static readonly float[] StartBands = { 45f, 120f };

        public static int StartRegionLevel(string name)
        {
            foreach (var r in StartRegions) if (r.name == name) return r.level;
            return -1;
        }
        static float StartRegionRadius(string name, float fallback)
        {
            foreach (var r in StartRegions) if (r.name == name && r.radius > 0f) return r.radius;
            return fallback;
        }

        /// <summary>Baut die Karte einer Zone aus ihren Daten (Gebiete, Wegmarken, Eingänge).</summary>
        public static ZoneDangerMap Build(string zoneId, float size, Vector2 hub, IEnumerable<Vector2> arrivals, IEnumerable<Vector2> waypoints, IEnumerable<(string name, Vector2 center, float radius)> regions, bool startZone)
        {
            var m = new ZoneDangerMap { ZoneId = zoneId, Size = size, IsStartZone = startZone };
            m.Anchors.Add(hub);
            foreach (var a in arrivals) if (Vector2.Distance(a, hub) > 1f) m.Anchors.Add(a);
            m.Waypoints.AddRange(waypoints);
            float s = m.S;
            if (startZone)
            {
                m.BandEdges.AddRange(StartBands); m.BandLevels.AddRange(new[] { 0, 1, 2 });
                m.HubSafeRadius = 30f; m.WaypointSafeRadius = 12f;
                foreach (var r in regions) { int l = StartRegionLevel(r.name); if (l >= 0) m.Areas.Add(new DangerArea { Name = r.name, Center = r.center, Radius = StartRegionRadius(r.name, r.radius), Level = l }); }
                return m;
            }
            Specs.TryGetValue(zoneId, out var spec);
            if (spec.CoreAt <= 0f) spec = new Spec { Entry = 2, Core = 3, CoreAt = 0.33f };
            m.BandEdges.Add(size * spec.CoreAt); m.BandLevels.AddRange(new[] { spec.Entry, spec.Core });
            m.HubSafeRadius = 0f; m.WaypointSafeRadius = 25f;
            foreach (var r in regions)
            {
                bool city = spec.Cities != null && System.Array.IndexOf(spec.Cities, r.name) >= 0;
                bool noLeash = spec.NoLeash != null && System.Array.IndexOf(spec.NoLeash, r.name) >= 0;
                int lvl = city ? 0 : m.BandAt(r.center);
                if (spec.Overrides != null) foreach (var o in spec.Overrides) if (o.name == r.name) lvl = o.level;
                m.Areas.Add(new DangerArea { Name = r.name, Center = r.center, Radius = r.radius, Level = lvl, City = city, NoLeash = noLeash });
            }
            return m;
        }
    }
}
