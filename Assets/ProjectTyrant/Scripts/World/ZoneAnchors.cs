using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Vom Zonen-Builder befüllte Ankerpunkte (Spawns, POIs, Banner). Einzige Stelle, an der Systeme Orte nachschlagen.</summary>
    public class ZoneAnchors : MonoBehaviour
    {
        public static ZoneAnchors Instance { get; private set; }

        public Transform Waypoint;
        public Transform HubCenter, OutpostCenter, CampCenter, RuinsCenter;
        public Transform[] CrateSpots;
        public Transform[] GuardSpots;
        public Transform[] OutpostAttackSpawns;
        public Transform[] OutpostOccupierSpots;
        public Transform[] CampSpawns;
        public Transform[] RuinsSpawns;
        public Transform[] WildSpawns;
        public Transform ChampionSpawn;
        public WorldBanner[] OutpostBanners;
        public WorldBanner[] CampBanners;
        public GameObject CampFireFx;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static Vector3 GroundAt(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, 200f, p.z), Vector3.down, out var hit, 400f, 1, QueryTriggerInteraction.Ignore))
                return hit.point;
            return p;
        }
    }
}
