using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Basis aller Zonenabschluss-Objekte: Definition, Erledigt-Status (aus dem Profil), Karten-Marker.</summary>
    public abstract class ZoneObject : Interactable, IMapMarkerProvider
    {
        public static readonly List<ZoneObject> Objects = new List<ZoneObject>();
        public ZoneObjectDefinition Definition;

        public string Id => Definition != null ? Definition.Id : name;
        public bool Done => PlayerProfile.Instance != null && PlayerProfile.Instance.State.Completed.Contains(Id);

        protected override void OnEnable() { base.OnEnable(); if (!Objects.Contains(this)) Objects.Add(this); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); Objects.Remove(this); MapMarkerRegistry.Unregister(this); }

        public virtual MarkerState MarkerState => Done ? MarkerState.Done : MarkerState.Open;

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            if (Definition == null) return;
            into.Add(new MapMarkerInfo { Kind = Definition.Kind, State = MarkerState, Position = transform.position, Label = Definition.DisplayName, Owner = this });
        }

        public virtual TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this || Definition == null) return null;
            var c = TooltipBuilder.MapMarker(info, Definition.Description);
            string reward = Definition.RewardText();
            if (!string.IsNullOrEmpty(reward) && info.Kind != MapMarkerKind.Waypoint) c.Add("Belohnung: " + reward, new Color(0.9f, 0.85f, 0.6f));
            if (Definition.Requires != Traversal.None)
            {
                bool has = RequirementMet(Definition.Requires);
                c.Add("Benötigt: " + ReachabilityModel.Label(Definition.Requires), has ? new Color(0.6f, 0.9f, 0.6f) : new Color(1f, 0.5f, 0.4f));
            }
            return c;
        }

        /// <summary>Hook für M17 (Reittiere freigeschaltet?). Ohne Reittier-System: nicht erfüllt.</summary>
        public static System.Func<Traversal, bool> RequirementCheck;
        public static bool RequirementMet(Traversal t) => t == Traversal.None || (RequirementCheck != null && RequirementCheck(t));

        protected Combatant PlayerC => PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;

        protected bool PlayerWithin(float radius)
        {
            var p = PlayerC;
            if (p == null || p.IsDead) return false;
            var d = p.transform.position - transform.position; d.y = 0f;
            return d.sqrMagnitude <= radius * radius;
        }

        protected void Complete() { if (PlayerProfile.Instance != null) PlayerProfile.Instance.CompleteZoneObject(Definition); }
    }
}
