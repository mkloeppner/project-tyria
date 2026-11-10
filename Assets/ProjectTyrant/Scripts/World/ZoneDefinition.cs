using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    [Serializable]
    public class PortalDef
    {
        public string Id;
        public Vector3 Position;
        public float Yaw;
        public string TargetZone;
        public string TargetPortal;
    }

    [Serializable]
    public class ZoneWaypointInfo
    {
        public string Id, Name;
        public Vector3 Position;
    }

    /// <summary>Fix-Lauf F1: Ankunfts-Wegmarke je Eingang (Charakterstart/Spawn, Portal-Ankunft) – vom Builder geprüft (≤ 40 m) und ggf. angelegt.</summary>
    [Serializable]
    public class ZoneArrivalInfo
    {
        public string Entrance;      // "spawn" oder "portal:<Id>"
        public Vector3 Position;     // Ankunftspunkt
        public string WaypointId;
    }

    /// <summary>Gebiet innerhalb einer Zone (Gebietsname-Einblendung, Gegnerstufe).</summary>
    [Serializable]
    public class ZoneRegionInfo
    {
        public string Name;
        public Vector2 Center;
        public float Radius;
        public int LevelMin, LevelMax;
    }

    /// <summary>Zone als Daten (GW2-Karte): Biom, Größe, Level-Bereich, Portale, Spawn, Kontinent-Position, Wegmarken für die Kontinentkarte.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Zone")]
    public class ZoneDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public string SceneName;
        public int Seed = 1;
        [Tooltip("Kantenlänge in m (Zone zentriert um 0/0)")] public float Size = 200f;
        public int LevelMin = 1, LevelMax = 15;
        public BiomeProfile Biome;
        [Tooltip("Startzone: altes Lauf-3-Layout (ZoneLayout) statt Biom-Rauschen")] public bool LegacyLayout;
        public List<PortalDef> Portals = new List<PortalDef>();
        public Vector3 DefaultSpawn;
        [Tooltip("Rechteck auf der Kontinentkarte (0..1)")] public Rect ContinentRect = new Rect(0.1f, 0.1f, 0.2f, 0.2f);
        public Color ContinentColor = new Color(0.4f, 0.6f, 0.3f);
        public List<ZoneWaypointInfo> Waypoints = new List<ZoneWaypointInfo>();
        [Tooltip("Fix-Lauf F1: Eingang → Ankunfts-Wegmarke (Builder)")] public List<ZoneArrivalInfo> Arrivals = new List<ZoneArrivalInfo>();
        public List<string> Tips = new List<string>();
        [Tooltip("M27: Ladebildschirm-Illustration (Builder-Aufnahme vom Aussichtspunkt)")] public Texture2D LoadingImage;
        public List<ZoneRegionInfo> Regions = new List<ZoneRegionInfo>();
        [Header("Weltboss/Meta-Timer (Lauf 4)")]
        public string TimerLabel;
        public float TimerPeriod, TimerOffset;
        public Vector3 TimerPosition;
        public string TimerEventId;
        [Header("Balance B1")]
        [Tooltip("Gefahrenkarte (Gebiete, Bänder, Sicherheitszonen) – vom Builder gebacken")] public Balance.ZoneDangerMap DangerMap = new Balance.ZoneDangerMap();
        [Tooltip("Ids aller Zonenabschluss-Ziele (vom Builder) – für den Prozentwert auf der Kontinentkarte")] public List<string> CompletionIds = new List<string>();

        public string LevelText => LevelMin == LevelMax ? LevelMin.ToString() : LevelMin + "–" + LevelMax;
        public PortalDef Portal(string id) => Portals.Find(p => p.Id == id);

        /// <summary>Nächstes Gebiet, dessen Radius die Position enthält (null = Wildnis der Zone).</summary>
        public ZoneRegionInfo RegionAt(Vector3 pos)
        {
            ZoneRegionInfo best = null; float bestD = float.MaxValue;
            if (Regions == null) return null;
            foreach (var r in Regions)
            {
                float d = Vector2.Distance(new Vector2(pos.x, pos.z), r.Center);
                if (d <= r.Radius && d < bestD) { best = r; bestD = d; }
            }
            return best;
        }
    }
}
