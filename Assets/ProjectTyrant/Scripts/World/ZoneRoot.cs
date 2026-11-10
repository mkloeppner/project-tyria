using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Wurzel einer Zonen-Szene (Zonen-Objekt, kein Singleton): Definition, Karten-Textur, Respawn-Wegmarke und die
    /// zonenspezifischen Referenzen der Ereignis-Engine. Szenenübergreifende Verweise laufen über <see cref="Current"/>.
    /// </summary>
    public class ZoneRoot : MonoBehaviour
    {
        public static ZoneRoot Current { get; private set; }

        public ZoneDefinition Definition;
        public Texture2D MapTexture;
        public Transform Waypoint;
        [Header("Ereignisse")]
        public EventDefinition[] Events = new EventDefinition[0];
        public EventChain Chain;
        public Combatant[] Crates = new Combatant[0];
        public Combatant[] HubCrates = new Combatant[0];
        public NpcBrain[] Guards = new NpcBrain[0];
        public VendorNpc CaravanVendor;
        [Header("Balance B1")] public Balance.BalanceConfig BalanceConfig;

        void Awake() { Current = this; if (BalanceConfig != null) Balance.BalanceWorld.Config = BalanceConfig; }
        void OnDestroy() { if (Current == this) Current = null; }

        public string Id => Definition != null ? Definition.Id : gameObject.scene.name;

        /// <summary>Gebiets-Level an einer Position (Startzone: Regionen aus Lauf 3, sonst Zonen-Bereich nach Entfernung vom Spawn).</summary>
        public static Vector2Int LevelRangeAt(Vector3 pos)
        {
            var c = Current;
            if (c == null || c.Definition == null || c.Definition.LegacyLayout) return Procedural.ZoneLayout.LevelRangeAt(pos);
            var d = c.Definition;
            var reg = d.RegionAt(pos);
            if (reg != null) return new Vector2Int(reg.LevelMin, reg.LevelMax);
            float t = Mathf.Clamp01(Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(d.DefaultSpawn.x, d.DefaultSpawn.z)) / (d.Size * 0.6f));
            int lo = Mathf.RoundToInt(Mathf.Lerp(d.LevelMin, d.LevelMax - 2, t));
            return new Vector2Int(lo, Mathf.Min(d.LevelMax, lo + 2));
        }
    
        /// <summary>Gebietsname an einer Position (Startzone: Lauf-3-Regionen; sonst Gebiete aus der Definition, sonst Zonenname).</summary>
        public static string RegionNameAt(Vector3 pos)
        {
            var c = Current;
            if (c == null || c.Definition == null || c.Definition.LegacyLayout) return Procedural.ZoneLayout.RegionNameAt(pos);
            var reg = c.Definition.RegionAt(pos);
            return reg != null ? reg.Name : c.Definition.DisplayName;
        }

        public static string ZoneName => Current != null && Current.Definition != null ? Current.Definition.DisplayName : Procedural.ZoneLayout.WildernessName;

        /// <summary>Bodenhöhe per Raycast auf die Umgebung (zonenunabhängig), Rückfall Lauf-3-Layout.</summary>
        public static float GroundHeight(float x, float z)
        {
            if (Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out var hit, 800f, 1, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return Procedural.ZoneLayout.Height(x, z);
        }
    }
}
