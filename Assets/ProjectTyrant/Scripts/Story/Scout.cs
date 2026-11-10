using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Späher mit Fernrohr (GW2). F → Sprechblase mit Kontext → Kartensektor und Herzen aufgedeckt, Entdeckungs-EP. Zählt nicht zum Zonenabschluss.</summary>
    public class Scout : Interactable, IMapMarkerProvider
    {
        public string Id;
        public string DisplayName = "Späher";
        public string Scene;                 // Szene „SP_&lt;zone&gt;_&lt;n&gt;“ (Sprechblase)
        public Vector3 SectorCenter;
        public float SectorRadius = 120f;
        bool _busy;

        void Reset() { PromptText = "Späher ansprechen"; Range = 3f; }
        protected override void OnEnable() { base.OnEnable(); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MapMarkerRegistry.Unregister(this); }

        StoryDirector Dir => GameManager.Instance != null ? GameManager.Instance.Story : null;
        bool Spoken => Dir != null && Dir.State != null && Dir.State.Scouts.Contains(Id);

        public override string Prompt => "Sprechen – " + DisplayName;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_busy && Dir != null && !Dir.Busy;
        public override void Interact(Combatant player) { _busy = true; Dir.SpeakToScout(this); Invoke(nameof(Free), 1f); }
        void Free() => _busy = false;

        public void CollectMarkers(List<MapMarkerInfo> into) => into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Scout, State = Spoken ? MarkerState.Done : MarkerState.Open, Position = transform.position, Label = DisplayName, Owner = this, Icon = "map_scout" });

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            var c = new TooltipContent { Title = DisplayName, TitleColor = new Color(0.7f, 0.9f, 1f) };
            c.Add("Späher – kennt die Gegend", Color.white);
            c.Add(Spoken ? "Gesprochen" : "Ansprechen deckt Karte und Herzen der Umgebung auf", new Color(0.8f, 0.8f, 0.75f));
            return c;
        }
    }
}
