using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum MapMarkerKind { Waypoint, PointOfInterest, Vista, HeroChallenge, Heart, Vendor, Event, EventBoss, Ally, Target, Personal, Portal, Insight, WorldBoss }
    public enum MarkerState { Open, Done, Undiscovered, Contested, Inactive }

    /// <summary>Momentaufnahme eines Markers (Struct, keine Allokation je Frame).</summary>
    public struct MapMarkerInfo
    {
        public MapMarkerKind Kind;
        public MarkerState State;
        public Vector3 Position;
        public float Radius;          // > 0 = Kreis (Events)
        public string Label;
        public UnityEngine.Object Owner;
        public string Icon;           // optional: eigenes Icon (z. B. Event-Zieltyp)
    }

    public interface IMapMarkerProvider
    {
        void CollectMarkers(List<MapMarkerInfo> into);
        /// <summary>Tooltip-Inhalt für einen eigenen Marker (Name, Status, Belohnung …).</summary>
        TooltipContent MarkerTooltip(MapMarkerInfo info);
    }

    /// <summary>Sammelt Marker aller Anbieter (Szenen-Marker, Events, Verbündete …).</summary>
    public static class MapMarkerRegistry
    {
        static readonly List<IMapMarkerProvider> _providers = new List<IMapMarkerProvider>();
        public static IReadOnlyList<IMapMarkerProvider> Providers => _providers;
        public static int Version { get; private set; }

        public static void Register(IMapMarkerProvider p) { if (p != null && !_providers.Contains(p)) { _providers.Add(p); Version++; } }
        public static void Unregister(IMapMarkerProvider p) { if (_providers.Remove(p)) Version++; }

        public static void Collect(List<MapMarkerInfo> into)
        {
            into.Clear();
            for (int i = 0; i < _providers.Count; i++) _providers[i].CollectMarkers(into);
        }

        public static TooltipContent Tooltip(MapMarkerInfo info)
        {
            if (info.Owner is IMapMarkerProvider p) { var t = p.MarkerTooltip(info); if (t != null) return t; }
            foreach (var pr in _providers) { var t = pr.MarkerTooltip(info); if (t != null) return t; }
            return TooltipBuilder.MapMarker(info, null);
        }

        public static void ClearAll() { _providers.Clear(); Version++; }

        public static string KindName(MapMarkerKind k)
        {
            switch (k)
            {
                case MapMarkerKind.Waypoint: return "Wegmarke";
                case MapMarkerKind.PointOfInterest: return "Sehenswürdigkeit";
                case MapMarkerKind.Vista: return "Aussichtspunkt";
                case MapMarkerKind.HeroChallenge: return "Heldenherausforderung";
                case MapMarkerKind.Heart: return "Herz";
                case MapMarkerKind.Vendor: return "Händler";
                case MapMarkerKind.Event: return "Ereignis";
                case MapMarkerKind.EventBoss: return "Ereignis-Boss";
                case MapMarkerKind.Ally: return "Verbündeter";
                case MapMarkerKind.Target: return "Aktuelles Ziel";
                case MapMarkerKind.Personal: return "Persönliche Wegmarke";
                case MapMarkerKind.Portal: return "Portal";
                case MapMarkerKind.Insight: return "Beherrschungs-Einblick";
                case MapMarkerKind.WorldBoss: return "Weltboss";
            }
            return "";
        }

        public static string IconFor(MapMarkerInfo m)
        {
            if (!string.IsNullOrEmpty(m.Icon)) return m.Icon;
            switch (m.Kind)
            {
                case MapMarkerKind.Waypoint: return m.State == MarkerState.Contested ? "map_waypoint_contested" : "map_waypoint";
                case MapMarkerKind.PointOfInterest: return "map_poi";
                case MapMarkerKind.Vista: return "map_vista";
                case MapMarkerKind.HeroChallenge: return "map_hero";
                case MapMarkerKind.Heart: return "map_heart";
                case MapMarkerKind.Vendor: return "map_vendor";
                case MapMarkerKind.Event: return "map_event";
                case MapMarkerKind.EventBoss: return "map_boss";
                case MapMarkerKind.Ally: return "map_ally";
                case MapMarkerKind.Target: return "map_target";
                case MapMarkerKind.Personal: return "map_personal";
                case MapMarkerKind.Portal: return "map_portal";
                case MapMarkerKind.Insight: return "map_insight";
                case MapMarkerKind.WorldBoss: return "map_worldboss";
            }
            return "map_poi";
        }

        /// <summary>Zeigt die Minimap diesen Marker? (Normale Gegner gibt es gar nicht als Marker.)</summary>
        public static bool ShowOnMinimap(MapMarkerKind k) => true;

        /// <summary>Am Minimap-Rand festhalten, wenn außerhalb (Events, Bosse, Ziel, persönliche Wegmarke).</summary>
        public static bool ClampOnMinimap(MapMarkerKind k) => k == MapMarkerKind.Event || k == MapMarkerKind.EventBoss || k == MapMarkerKind.Personal || k == MapMarkerKind.Target;
    }

    /// <summary>Statischer Szenen-Marker (vom Builder gesetzt). Status kann von anderen Komponenten über <see cref="StateProvider"/> geliefert werden.</summary>
    public class MapMarker : MonoBehaviour, IMapMarkerProvider
    {
        public MapMarkerKind Kind = MapMarkerKind.PointOfInterest;
        public string Label;
        [TextArea] public string Description;
        public float Radius;
        [NonSerialized] public Func<MarkerState> StateProvider;
        [NonSerialized] public Func<TooltipContent> TooltipProvider;

        void OnEnable() => MapMarkerRegistry.Register(this);
        void OnDisable() => MapMarkerRegistry.Unregister(this);

        public MarkerState State => StateProvider != null ? StateProvider() : MarkerState.Open;

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            into.Add(new MapMarkerInfo { Kind = Kind, State = State, Position = transform.position, Radius = Radius, Label = Label, Owner = this });
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((UnityEngine.Object)info.Owner != this) return null;
            return TooltipProvider != null ? TooltipProvider() : TooltipBuilder.MapMarker(info, Description);
        }
    }

    /// <summary>Dynamische Marker: Verbündete (grün), Event-Bosse (Champions), aktuelles Ziel, laufendes Event (bis M13).</summary>
    public class DynamicMarkerProvider : IMapMarkerProvider
    {
        public static readonly DynamicMarkerProvider Instance = new DynamicMarkerProvider();

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c == player || !c.isActiveAndEnabled) continue;
                if (c.Team == Team.Ally && c.GetComponent<NpcBrain>() != null)
                    into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Ally, Position = c.transform.position, Label = c.DisplayName, Owner = c });
                else if (c.Team == Team.Enemy && c.Tier == EnemyTier.Champion)
                    into.Add(new MapMarkerInfo { Kind = MapMarkerKind.EventBoss, Position = c.transform.position, Label = c.DisplayName, Owner = c });
            }
            var target = player != null ? player.GetComponent<TargetingSystem>() : null;
            if (target != null && target.Current != null && !target.Current.IsDead && target.Current.Tier != EnemyTier.Champion)
                into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Target, Position = target.Current.transform.position, Label = target.Current.DisplayName, Owner = target.Current });
            CollectExtra(into);
        }

        /// <summary>Erweiterungspunkt für Events (M13) und persönliche Wegmarke.</summary>
        public static event Action<List<MapMarkerInfo>> Extra;
        static void CollectExtra(List<MapMarkerInfo> into) => Extra?.Invoke(into);

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if (info.Owner is Combatant c)
                return new TooltipContent { Title = c.DisplayName, TitleColor = info.Kind == MapMarkerKind.Ally ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.6f, 0.3f), Subtitle = MapMarkerRegistry.KindName(info.Kind) + " · Stufe " + c.Level };
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init() => MapMarkerRegistry.Register(Instance);
    }
}
