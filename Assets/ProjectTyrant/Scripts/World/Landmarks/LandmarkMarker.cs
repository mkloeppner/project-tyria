using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: Wahrzeichen auf der Karte (benanntes Icon mit Tooltip: Typ, Tore, erreichte Stufen) + „besucht“.</summary>
    public class LandmarkMarker : MonoBehaviour, IMapMarkerProvider
    {
        public static readonly List<LandmarkMarker> All = new List<LandmarkMarker>();
        public string Id, Name, TypeName, GateText;
        public bool Main;
        public float Height, Radius = 40f;
        public List<Vector3> StagePositions = new List<Vector3>();
        public List<string> StageNames = new List<string>();
        void OnEnable() { All.Add(this); MapMarkerRegistry.Register(this); }
        void OnDisable() { All.Remove(this); MapMarkerRegistry.Unregister(this); }
        static ProfileState State => PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null;
        public bool Visited => State != null && State.Flags.Contains("lm:" + Id);
        public int StagesReached { get { var s = State; int n = 0; if (s == null) return 0; for (int i = 0; i < StageNames.Count; i++) if (s.Flags.Contains("lm:" + Id + ":" + i)) n++; return n; } }

        void Update()
        {
            var st = State; var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            if (st == null || p == null) return;
            var pos = p.transform.position;
            var d = pos - transform.position; d.y = 0f;
            if (!Visited && d.sqrMagnitude <= Radius * Radius)
            {
                st.Flags.Add("lm:" + Id);
                if (Main) st.Achievements.Add("w1_landmarks");
                GameEventBus.RaiseBanner(Name, "Wahrzeichen entdeckt", new Color(0.95f, 0.85f, 0.55f));
                PlayerProfile.Instance.GrantReward(250, 0, 0, "Wahrzeichen: " + Name);
            }
            for (int i = 0; i < StagePositions.Count; i++)
            {
                if ((StagePositions[i] - pos).sqrMagnitude > 36f || st.Flags.Contains("lm:" + Id + ":" + i)) continue;
                st.Flags.Add("lm:" + Id + ":" + i);
                GameEventBus.Notify(Name + ": " + StageNames[i] + " erreicht", new Color(0.95f, 0.85f, 0.55f));
            }
        }

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Landmark, State = Visited ? MarkerState.Done : MarkerState.Open, Position = transform.position, Label = Name, Owner = this, Icon = "map_landmark" });
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            var c = new TooltipContent { Title = Name, TitleColor = new Color(0.95f, 0.85f, 0.55f), Subtitle = TypeName + (Main ? " · Haupt-Wahrzeichen" : "") + " · " + Mathf.RoundToInt(Height) + " m" };
            if (!string.IsNullOrEmpty(GateText)) c.Add(GateText, new Color(0.9f, 0.75f, 0.5f));
            c.Add("Stufen erreicht: " + StagesReached + "/" + StageNames.Count, new Color(0.8f, 0.9f, 0.8f));
            if (!Visited) c.Add("Noch nicht besucht", new Color(0.7f, 0.7f, 0.75f));
            return c;
        }
    }
}
