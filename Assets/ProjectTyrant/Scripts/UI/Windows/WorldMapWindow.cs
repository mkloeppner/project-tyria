using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Weltkarte (M): Vollbild, Pan (Ziehen), Zoom (Mausrad um den Zeiger), Marker mit Tooltip, Legende, Zonenabschluss-Zähler, Schnellreise.</summary>
    public partial class WorldMapWindow
    {
        public readonly UiWindow Window;
        readonly UIController _ui;
        readonly RectTransform _view, _map, _circles, _markers;
        readonly Image _player;
        readonly FogTexture _fog;
        readonly TextMeshProUGUI _completion, _hint;
        readonly List<Image> _markerPool = new List<Image>();
        readonly List<Image> _circlePool = new List<Image>();
        readonly List<MapMarkerInfo> _list = new List<MapMarkerInfo>();
        readonly List<(Vector2 pos, int index)> _hits = new List<(Vector2, int)>();
        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        public Vector2 CenterUv { get; private set; } = new Vector2(0.5f, 0.5f);
        public float Zoom { get; private set; } = 2f;
        public const float MinZoom = 1f, MaxZoom = 6f;
        int _hover = -1;
        float _completionTimer;

        public WorldMapWindow(WindowManager mgr, UIController ui, Texture mapTexture)
        {
            _ui = ui;
            Window = new UiWindow(mgr, "worldmap", "Weltkarte – " + Procedural.ZoneLayout.WildernessName, new Vector2(1600, 900));
            Window.OpenSound = "MapOpen"; Window.CloseSound = "MapClose";   // Lauf 5 / M24: Papier
            Window.Root.anchorMin = Vector2.zero; Window.Root.anchorMax = Vector2.one;
            Window.Root.offsetMin = new Vector2(24, 24); Window.Root.offsetMax = new Vector2(-24, -24);
            var drag = Window.Header.GetComponent<UiDragHandle>(); if (drag != null) Object.Destroy(drag);

            var viewBg = UiFactory.Image("View", Window.Content, new Color(0.1f, 0.12f, 0.1f, 1f), null, true);
            _view = viewBg.rectTransform.Stretch(0, 300, 0, 0);
            _view.gameObject.AddComponent<RectMask2D>();
            var input = _view.gameObject.AddComponent<WorldMapInput>(); input.Owner = this;

            _map = UiFactory.Rect("Map", _view);
            _map.anchorMin = _map.anchorMax = new Vector2(0.5f, 0.5f); _map.pivot = new Vector2(0.5f, 0.5f);
            var img = _map.gameObject.AddComponent<RawImage>(); img.texture = mapTexture; img.raycastTarget = false;
            _fog = new FogTexture(128, new Color(0.07f, 0.065f, 0.08f, 0.88f));
            var fog = UiFactory.Rect("Fog", _map).Stretch().gameObject.AddComponent<RawImage>(); fog.texture = _fog.Texture; fog.raycastTarget = false;
            _circles = UiFactory.Rect("Circles", _view).Stretch();
            _markers = UiFactory.Rect("Markers", _view).Stretch();
            _player = UiFactory.Image("Player", _view, Color.white, UiFactory.Sprite("map_player"));
            _player.rectTransform.anchorMin = _player.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _player.rectTransform.sizeDelta = new Vector2(30, 30); _player.raycastTarget = false;
            _hint = UiFactory.Text("Hint", _view, "Ziehen: verschieben · Mausrad: zoomen · Wegmarke anklicken: reisen", 14, TextAlignmentOptions.BottomLeft, new Color(1, 1, 1, 0.8f));
            _hint.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(10, 6), new Vector2(-20, 22));

            var side = UiFactory.Image("Side", Window.Content, new Color(0, 0, 0, 0.3f), UiFactory.Sprite("ui_round"));
            side.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(290, 0));
            var compTitle = UiFactory.Text("CompTitle", side.transform, "Zonenabschluss", 19, TextAlignmentOptions.Left, UiFactory.AccentColor);
            compTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(-24, 26));
            _completion = UiFactory.Text("Completion", side.transform, "", 15, TextAlignmentOptions.TopLeft);
            _completion.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -40), new Vector2(-24, 170));
            var legTitle = UiFactory.Text("LegTitle", side.transform, "Legende", 19, TextAlignmentOptions.Left, UiFactory.AccentColor);
            legTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -220), new Vector2(-24, 26));
            (string icon, string text)[] legend = {
                ("map_waypoint", "Wegmarke (Schnellreise)"), ("map_waypoint_contested", "Wegmarke – umkämpft"), ("map_poi", "Sehenswürdigkeit"), ("map_vista", "Aussichtspunkt"),
                ("map_hero", "Heldenherausforderung"), ("map_heart", "Herz (offen golden, erledigt grau)"), ("map_vendor", "Händler"), ("map_event", "Ereignis (Kreis = Radius)"),
                ("map_boss", "Ereignis-Boss"), ("map_ally", "Verbündete"), ("map_target", "Aktuelles Ziel"), ("map_player", "Du") };
            for (int i = 0; i < legend.Length; i++)
            {
                float y = -252 - i * 30;
                var li = UiFactory.Image("L" + i, side.transform, Color.white, UiFactory.Sprite(legend[i].icon));
                li.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y), new Vector2(24, 24));
                var lt = UiFactory.Text("LT" + i, side.transform, legend[i].text, 14, TextAlignmentOptions.Left);
                lt.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(46, y), new Vector2(-56, 24));
            }
            Window.Closed += () => { _ui.Tooltip.Hide(); _hover = -1; };
        }

        public bool DefeatMode { get; private set; }
        static string NormalTitle => "Weltkarte – " + ZoneRoot.ZoneName;
        const string NormalHint = "Ziehen: verschieben · Mausrad: zoomen · Wegmarke anklicken: reisen";

        public void SetDefeatMode(bool on)
        {
            DefeatMode = on;
            Window.Title.text = on ? "Besiegt – Zur Wegmarke zurückkehren" : NormalTitle;
            Window.Title.color = on ? new Color(1f, 0.5f, 0.4f) : UiFactory.AccentColor;
            _hint.text = on ? "Wähle eine entdeckte, nicht umkämpfte Wegmarke – du erscheinst dort mit vollen Lebenspunkten." : NormalHint;
        }

        RawImage _mapRaw;
        public void SetTexture(Texture t) { if (_mapRaw == null) _mapRaw = _map.GetComponent<RawImage>(); _mapRaw.texture = t; }

        Sprite S(string id) { if (!_sprites.TryGetValue(id, out var s)) { s = UiFactory.Sprite(id); _sprites[id] = s; } return s; }

        float MapPixels => Mathf.Min(_view.rect.width, _view.rect.height);

        public void OpenCentered(Vector3 world)
        {
            Window.Open();
            ShowContinent(false);
            CenterUv = MapProjection.WorldToUv(world);
            Zoom = Mathf.Max(Zoom, 2f);
            _completionTimer = 0f;
        }

        public void Pan(Vector2 deltaPixels)
        {
            CenterUv -= deltaPixels / (MapPixels * Zoom);
            CenterUv = MapProjection.ClampView(CenterUv, _view.rect.size, MapPixels, Zoom);
        }

        public void ZoomAt(Vector2 local, float wheel)
        {
            // GW2: über die Zonengrenze hinaus herauszoomen → Kontinentansicht
            if (wheel < 0 && Zoom <= MinZoom + 0.001f && !ContinentMode) { ShowContinent(true); return; }
            if (ContinentMode) { if (wheel > 0) ShowContinent(false); return; }
            float nz = Mathf.Clamp(Zoom * (wheel > 0 ? 1.18f : 1f / 1.18f), MinZoom, MaxZoom);
            CenterUv = MapProjection.ZoomAround(CenterUv, local, MapPixels, Zoom, nz);
            if (Mathf.Abs(nz - Zoom) > 0.001f) UiFactory.Sound("Zoom");
            Zoom = nz;
            CenterUv = MapProjection.ClampView(CenterUv, _view.rect.size, MapPixels, Zoom);
        }

        public void Refresh()
        {
            if (ContinentMode) { RefreshContinent(); return; }
            float mp = MapPixels;
            if (mp <= 1f) return;
            CenterUv = MapProjection.ClampView(CenterUv, _view.rect.size, mp, Zoom);
            _map.sizeDelta = Vector2.one * mp * Zoom;
            _map.anchoredPosition = MapProjection.UvToView(new Vector2(0.5f, 0.5f), CenterUv, mp, Zoom);
            var prof = PlayerProfile.Instance;
            var fogModel = prof != null ? prof.State.Fog : null;
            _fog.Update(fogModel);
            var p = _ui.Player;
            if (p != null)
            {
                _player.rectTransform.anchoredPosition = MapProjection.UvToView(MapProjection.WorldToUv(p.transform.position), CenterUv, mp, Zoom);
                _player.rectTransform.localEulerAngles = new Vector3(0, 0, -p.transform.eulerAngles.y);
            }
            MapMarkerRegistry.Collect(_list);
            _hits.Clear();
            int mi = 0, ci = 0;
            float ppm = mp * Zoom / MapProjection.ZoneSize;
            for (int i = 0; i < _list.Count; i++)
            {
                var m = _list[i];
                if (m.Kind == MapMarkerKind.Target) continue;
                var pos = MapProjection.UvToView(MapProjection.WorldToUv(m.Position), CenterUv, mp, Zoom);
                if (m.Radius > 0f)
                {
                    var c = Pooled(_circlePool, _circles, ci++, "ui_ring");
                    c.rectTransform.anchoredPosition = pos; c.rectTransform.sizeDelta = Vector2.one * m.Radius * 2f * ppm;
                    c.color = new Color(1f, 0.6f, 0.15f, 0.8f);
                }
                var img = Pooled(_markerPool, _markers, mi++, null);
                img.sprite = S(MapMarkerRegistry.IconFor(m));
                img.rectTransform.anchoredPosition = pos;
                float size = m.Kind == MapMarkerKind.Ally ? 14f : m.Kind == MapMarkerKind.Event || m.Kind == MapMarkerKind.EventBoss || m.Kind == MapMarkerKind.Waypoint ? 32f : 26f;
                img.rectTransform.sizeDelta = Vector2.one * (i == _hover ? size * 1.25f : size);
                img.color = MinimapHud.MarkerColor(m, fogModel);
                _hits.Add((pos, i));
            }
            for (int i = mi; i < _markerPool.Count; i++) if (_markerPool[i].enabled) _markerPool[i].enabled = false;
            for (int i = ci; i < _circlePool.Count; i++) if (_circlePool[i].enabled) _circlePool[i].enabled = false;

            _completionTimer -= Time.unscaledDeltaTime;
            if (_completionTimer <= 0f) { _completionTimer = 0.5f; _completion.text = CompletionText(_list); }
        }

        /// <summary>Zähler aus den Markern (Wegmarken entdeckt, sonst erledigt).</summary>
        public static string CompletionText(List<MapMarkerInfo> list)
        {
            var c = ZoneCompletion.Count(list);
            return "Wegmarken " + c.Waypoints.x + "/" + c.Waypoints.y + "\nSehenswürdigkeiten " + c.Pois.x + "/" + c.Pois.y + "\nAussichtspunkte " + c.Vistas.x + "/" + c.Vistas.y
                + "\nHerzen " + c.Hearts.x + "/" + c.Hearts.y + "\nHeldenherausforderungen " + c.Heroes.x + "/" + c.Heroes.y
                + "\n\n<size=130%><color=#FFD27A>" + Mathf.FloorToInt(c.Percent * 100f) + " %</color></size> abgeschlossen";
        }

        int Nearest(Vector2 local, float maxDist)
        {
            int best = -1; float bd = maxDist * maxDist;
            foreach (var h in _hits)
            {
                float d = (h.pos - local).sqrMagnitude;
                if (d < bd) { bd = d; best = h.index; }
            }
            return best;
        }

        public void OnHover(Vector2 local, Vector2 screen)
        {
            if (ContinentMode) return;
            int i = Nearest(local, 18f);
            _hover = i;
            if (i < 0 || i >= _list.Count) { _ui.Tooltip.Hide(); return; }
            _ui.Tooltip.Show(this, MapMarkerRegistry.Tooltip(_list[i]), screen, _ui.CanvasScale);
        }

        public void OnExit() { _hover = -1; _ui.Tooltip.Hide(); }

        public void OnClick(Vector2 local)
        {
            if (ContinentMode) return;
            int i = Nearest(local, 18f);
            if (i < 0 || i >= _list.Count) return;
            var m = _list[i];
            if (m.Kind != MapMarkerKind.Waypoint) return;
            UiFactory.Sound("WaypointSelect");
            _ui.RequestTravel(m);
        }

        static Image Pooled(List<Image> pool, RectTransform parent, int index, string sprite)
        {
            while (pool.Count <= index)
            {
                var img = UiFactory.Image("M", parent, Color.white, sprite != null ? UiFactory.Sprite(sprite) : null);
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                pool.Add(img);
            }
            var r = pool[index];
            if (!r.enabled) r.enabled = true;
            return r;
        }
    }

    public class WorldMapInput : MonoBehaviour, IDragHandler, IScrollHandler, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler
    {
        [System.NonSerialized] public WorldMapWindow Owner;
        bool _dragging;
        Vector2 Local(PointerEventData e) { RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, null, out var l); return l; }
        float Scale => UIController.Instance != null ? UIController.Instance.CanvasScale : 1f;
        public void OnBeginDrag(PointerEventData e) { _dragging = e.button == PointerEventData.InputButton.Left; }
        public void OnEndDrag(PointerEventData e) { _dragging = false; }
        public void OnDrag(PointerEventData e) { if (_dragging) Owner?.Pan(e.delta / Scale); }
        public void OnScroll(PointerEventData e) => Owner?.ZoomAt(Local(e), e.scrollDelta.y);
        public void OnPointerClick(PointerEventData e) { if (!e.dragging && e.button == PointerEventData.InputButton.Left) Owner?.OnClick(Local(e)); }
        public void OnPointerMove(PointerEventData e) => Owner?.OnHover(Local(e), e.position);
        public void OnPointerExit(PointerEventData e) => Owner?.OnExit();
    }
}
