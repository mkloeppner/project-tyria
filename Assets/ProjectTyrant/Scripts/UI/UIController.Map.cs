using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Minimap, Tracker, Weltkarte, Schnellreise mit Ladeblende.</summary>
    public partial class UIController
    {
        [Header("Karte")]
        public Texture2D MapTexture;

        public MinimapHud Minimap { get; private set; }
        public TrackerHud Tracker { get; private set; }
        public WorldMapWindow WorldMap { get; private set; }
        Image _fade;

        void BuildMap()
        {
            Minimap = new MinimapHud(HudLayer, this, MapTexture);
            Tracker = new TrackerHud(HudLayer, this, 14 + Minimap.TopEdge);
            WorldMap = new WorldMapWindow(Windows, this, MapTexture);
            _fade = UiFactory.Image("Fade", OverlayLayer, new Color(0, 0, 0, 0), null);
            _fade.rectTransform.Stretch(); _fade.raycastTarget = false; _fade.gameObject.SetActive(false);
        }

        void UpdateMap()
        {
            if (DefeatMode && WorldMap != null && !WorldMap.Window.IsOpen) OpenWorldMap(Player != null ? Player.transform.position : Vector3.zero);
            if (DefeatMode && Player != null && !Player.IsDead) { DefeatMode = false; WorldMap.SetDefeatMode(false); }
            UpdateZoneTracker();
            UpdateRewardPopup();
            Minimap?.Update(Time.unscaledDeltaTime);
            if (WorldMap != null && WorldMap.Window.IsOpen) WorldMap.Refresh();
        }

        public void OpenWorldMap(Vector3 center)
        {
            if (WorldMap == null) return;
            WorldMap.OpenCentered(center);
        }

        public void ToggleWorldMap()
        {
            if (WorldMap == null) return;
            if (WorldMap.Window.IsOpen) WorldMap.Window.Close();
            else OpenWorldMap(Player != null ? Player.transform.position : Vector3.zero);
        }

        /// <summary>Klick auf eine Wegmarke: Prüfungen, Kosten, Bestätigung, Reise.</summary>
        public bool DefeatMode { get; private set; }

        /// <summary>Spieler besiegt: Weltkarte im Modus „Zur Wegmarke zurückkehren“ (nicht schließbar, bis gewählt).</summary>
        public void ShowDefeatMap()
        {
            DefeatMode = true;
            WorldMap.SetDefeatMode(true);
            OpenWorldMap(Player != null ? Player.transform.position : Vector3.zero);
            // Fix-Lauf F1: Liste der Wiederbelebungs-Orte – nie leer (Notfall-Wegmarke, andere Zonen)
            var prof = PlayerProfile.Instance;
            _defeatOptions = prof != null ? prof.DefeatOptions() : new System.Collections.Generic.List<WaypointSafety.DefeatOption>();
            WorldMap.SetDefeatOptions(_defeatOptions, PickDefeatOption);
        }

        System.Collections.Generic.List<WaypointSafety.DefeatOption> _defeatOptions = new System.Collections.Generic.List<WaypointSafety.DefeatOption>();
        public System.Collections.Generic.IReadOnlyList<WaypointSafety.DefeatOption> DefeatOptionsShown => _defeatOptions;

        public void PickDefeatOption(WaypointSafety.DefeatOption o)
        {
            var prof = PlayerProfile.Instance; if (prof == null || !DefeatMode) return;
            DefeatMode = false; WorldMap.SetDefeatMode(false); WorldMap.Window.Close();
            prof.RespawnAt(o);
        }

        public void RequestTravel(MapMarkerInfo wp)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null || Player == null) return;
            if (DefeatMode)
            {
                foreach (var o in _defeatOptions) if (o.Emergency && WaypointSafety.FlatDistance(o.Waypoint.Position, wp.Position) < 1f) { PickDefeatOption(o); return; }
                if (wp.State != MarkerState.Open) { GameEventBus.Notify(wp.State == MarkerState.Contested ? "Wegmarke umkämpft – wähle eine andere" : "Wegmarke noch nicht entdeckt", new Color(1f, 0.5f, 0.4f)); return; }
                DefeatMode = false; WorldMap.SetDefeatMode(false); WorldMap.Window.Close();
                prof.RespawnAt(wp.Position, wp.Label);
                return;
            }
            if (wp.State == MarkerState.Contested) { GameEventBus.Notify("Wegmarke umkämpft – Reisen nicht möglich", new Color(1f, 0.5f, 0.4f)); return; }
            if (wp.State == MarkerState.Undiscovered) { GameEventBus.Notify("Wegmarke noch nicht entdeckt", new Color(1f, 0.6f, 0.4f)); return; }
            int cost = prof.ZoneTravelCost(Player.transform.position, wp.Position, false);
            Dialog.Confirm("Schnellreise", "Zu „" + wp.Label + "“ reisen?\nKosten: " + WalletModel.Format(cost), () => prof.TravelTo(wp.Position, cost, wp.Label), "Reisen");
        }

        /// <summary>Wegmarke auf der Kontinentkarte: gleiche Zone → normale Reise, andere Zone → Reise mit Zonenaufschlag.</summary>
        public void RequestZoneTravel(ZoneDefinition zone, ZoneWaypointInfo wp)
        {
            var prof = PlayerProfile.Instance; var gm = GameManager.Instance;
            if (prof == null || gm == null || Player == null || zone == null || wp == null) return;
            if (!prof.State.Completed.Contains(wp.Id)) { GameEventBus.Notify("Wegmarke noch nicht entdeckt", new Color(1f, 0.6f, 0.4f)); return; }
            bool other = gm.CurrentZone != zone;
            int cost = prof.ZoneTravelCost(Player.transform.position, wp.Position, other);
            if (!other) { Dialog.Confirm("Schnellreise", "Zu „" + wp.Name + "“ reisen?\nKosten: " + WalletModel.Format(cost), () => prof.TravelTo(wp.Position, cost, wp.Name), "Reisen"); return; }
            Dialog.Confirm("Reise nach " + zone.DisplayName, "Zu „" + wp.Name + "“ (" + zone.DisplayName + ", Stufe " + zone.LevelText + ") reisen?\nKosten: " + WalletModel.Format(cost), () => prof.TravelToZone(zone, wp.Position, cost), "Reisen");
        }

        /// <summary>Kurze Ladeblende: abdunkeln → Aktion → aufhellen.</summary>
        public void FadeAction(System.Action mid, float duration = 0.35f) => StartCoroutine(FadeRoutine(mid, duration));

        IEnumerator FadeRoutine(System.Action mid, float d)
        {
            _fade.gameObject.SetActive(true); _fade.raycastTarget = true;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime) { _fade.color = new Color(0, 0, 0, t / d); yield return null; }
            _fade.color = Color.black;
            mid?.Invoke();
            yield return new WaitForSecondsRealtime(0.15f);
            for (float t = 0; t < d; t += Time.unscaledDeltaTime) { _fade.color = new Color(0, 0, 0, 1f - t / d); yield return null; }
            _fade.color = new Color(0, 0, 0, 0); _fade.raycastTarget = false; _fade.gameObject.SetActive(false);
        }
    
        float _zoneTrackerTimer;
        readonly System.Collections.Generic.List<Heart> _nearHearts = new System.Collections.Generic.List<Heart>();

        /// <summary>Tracker: aktive Ereignisse in Reichweite (nach Distanz), danach Herzen im aktuellen Gebiet – max. 4 Einträge.</summary>
        void UpdateZoneTracker()
        {
            if (Tracker == null || Player == null) return;
            if ((_zoneTrackerTimer -= Time.unscaledDeltaTime) > 0f) return;
            _zoneTrackerTimer = 0.25f;
            _nearHearts.Clear();
            foreach (var o in ZoneObject.Objects)
                if (o is Heart h && h.Contains(Player.transform.position)) _nearHearts.Add(h);
            _nearHearts.Sort((a, b) => (a.Center - Player.transform.position).sqrMagnitude.CompareTo((b.Center - Player.transform.position).sqrMagnitude));
            int slot = 0;
            var ed = EventDirector.Instance;
            if (ed != null)
                foreach (var e in ed.TrackerItems())
                {
                    if (slot >= TrackerHud.MaxEntries) break;
                    Tracker.SetEntry(slot++, true, e.Title, e.Desc, e.Progress, e.TimeLeft, e.Medal, e.MedalColor, e.Focus, new Color(1f, 0.62f, 0.2f));
                }
            foreach (var h in _nearHearts)
            {
                if (slot >= TrackerHud.MaxEntries) break;
                Tracker.SetEntry(slot++, true, h.Definition.DisplayName, h.Done ? "Erfüllt – Karma-Händler verfügbar" : h.Definition.HeartTask, h.Progress, -1f, Mathf.RoundToInt(h.Progress * 100f) + " %", new Color(1f, 0.85f, 0.4f), h.Center, new Color(1f, 0.8f, 0.3f));
            }
            Tracker.HideFrom(slot);
        }
    }
}
