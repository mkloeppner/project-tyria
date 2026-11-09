using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Zonen-Portal (GW2: leuchtender Torbogen an der Grenze). Betreten startet den Zonenwechsel.</summary>
    public class Portal : MonoBehaviour, IMapMarkerProvider
    {
        public string PortalId;
        public string TargetZone;
        public string TargetPortal;
        public string Label;
        public float Radius = 3f;
        float _armedAt;

        void OnEnable() { _armedAt = Time.time + 3f; MapMarkerRegistry.Register(this); }
        void OnDisable() => MapMarkerRegistry.Unregister(this);

        void Update()
        {
            if (Time.time < _armedAt) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Transitioning || gm.Player == null || gm.Player.IsDead) return;
            var d = gm.Player.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude <= Radius * Radius && Mathf.Abs(gm.Player.transform.position.y - transform.position.y) < 4f)
            {
                _armedAt = Time.time + 5f;
                gm.ChangeZone(TargetZone, TargetPortal);
            }
        }

        public void CollectMarkers(System.Collections.Generic.List<MapMarkerInfo> into)
            => into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Portal, State = MarkerState.Open, Position = transform.position, Label = Label, Owner = this });

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            return new TooltipContent { Title = Label, TitleColor = new Color(0.7f, 0.85f, 1f), Subtitle = "Portal · betreten für den Zonenwechsel" };
        }
    }
}
