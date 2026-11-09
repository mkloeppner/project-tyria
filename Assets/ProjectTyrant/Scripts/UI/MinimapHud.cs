using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Minimap unten rechts (≈ 280 × 230): gebackene Karte + Nebel, Marker (gepoolt), Spielerpfeil, Kompass,
    /// 5 Zoomstufen (Mausrad über der Minimap, + / −), Rotation umschaltbar, Klick öffnet die Weltkarte.
    /// </summary>
    public class MinimapHud
    {
        public static readonly Vector2 Size = new Vector2(280, 230);
        public readonly RectTransform Root;
        public int ZoomLevel { get; private set; } = 2;
        public bool Rotate { get; private set; }
        public float PixelsPerMeter => MapProjection.MinimapZoom[ZoomLevel];

        readonly UIController _ui;
        readonly RectTransform _view, _map, _circles, _markers;
        readonly RawImage _mapImage, _fogImage;
        readonly Image _player;
        readonly TextMeshProUGUI _north, _east, _south, _west, _area, _zoomText, _rotLabel;
        readonly FogTexture _fog;
        readonly List<Image> _markerPool = new List<Image>();
        readonly List<Image> _circlePool = new List<Image>();
        readonly List<MapMarkerInfo> _list = new List<MapMarkerInfo>();
        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        float _zoomTextTimer;
        string _areaName;

        public MinimapHud(RectTransform hud, UIController ui, Texture mapTexture)
        {
            _ui = ui;
            var frame = UiFactory.Image("Minimap", hud, new Color(0.05f, 0.045f, 0.06f, 0.95f), UiFactory.Sprite("ui_panel"), true);
            Root = frame.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 14), Size + new Vector2(10, 10));
            _view = UiFactory.Rect("View", Root).Stretch(5, 5, 5, 5);
            _view.gameObject.AddComponent<RectMask2D>();
            var hit = _view.gameObject.AddComponent<Image>(); hit.color = new Color(0.16f, 0.2f, 0.14f, 1f);
            var input = _view.gameObject.AddComponent<MinimapInput>(); input.Owner = this;

            _map = UiFactory.Rect("Map", _view);
            _map.anchorMin = _map.anchorMax = new Vector2(0.5f, 0.5f);
            _mapImage = _map.gameObject.AddComponent<RawImage>(); _mapImage.texture = mapTexture; _mapImage.raycastTarget = false;
            _fog = new FogTexture(128, new Color(0.08f, 0.07f, 0.09f, 0.82f));
            var fogRt = UiFactory.Rect("Fog", _map).Stretch();
            _fogImage = fogRt.gameObject.AddComponent<RawImage>(); _fogImage.texture = _fog.Texture; _fogImage.raycastTarget = false;

            _circles = UiFactory.Rect("Circles", _view).Stretch();
            _markers = UiFactory.Rect("Markers", _view).Stretch();
            _player = UiFactory.Image("Player", _view, Color.white, UiFactory.Sprite("map_player"));
            _player.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            _player.raycastTarget = false;

            _north = CompassLetter("N", 17, new Color(1f, 0.35f, 0.3f));
            _east = CompassLetter("O", 12, new Color(0.9f, 0.9f, 0.9f, 0.8f));
            _south = CompassLetter("S", 12, new Color(0.9f, 0.9f, 0.9f, 0.8f));
            _west = CompassLetter("W", 12, new Color(0.9f, 0.9f, 0.9f, 0.8f));

            var zin = UiFactory.Button("ZoomIn", Root, "+", () => SetZoom(ZoomLevel + 1), 16);
            ((RectTransform)zin.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -8), new Vector2(24, 22));
            var zout = UiFactory.Button("ZoomOut", Root, "-", () => SetZoom(ZoomLevel - 1), 16);
            ((RectTransform)zout.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -32), new Vector2(24, 22));
            var rot = UiFactory.Button("Rotate", Root, "Nord", ToggleRotate, 12);
            ((RectTransform)rot.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(58, 22));
            _rotLabel = rot.GetComponentInChildren<TextMeshProUGUI>();
            _zoomText = UiFactory.Text("ZoomText", Root, "", 13, TextAlignmentOptions.Bottom, new Color(1, 1, 1, 0.9f));
            _zoomText.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(160, 20));

            _area = UiFactory.Text("Area", hud, "", 17, TextAlignmentOptions.BottomRight, new Color(1f, 0.93f, 0.8f));
            _area.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, Size.y + 26), new Vector2(360, 24));
            _area.fontStyle = FontStyles.Italic;
            SetZoom(ZoomLevel);
        }

        public void SetTexture(Texture t) { _mapImage.texture = t; }

        public float TopEdge => Size.y + 26 + 26;   // Oberkante inkl. Gebietsname (für den Tracker)

        TextMeshProUGUI CompassLetter(string s, float size, Color c)
        {
            var t = UiFactory.Text("Compass_" + s, _view, s, size, TextAlignmentOptions.Center, c);
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            t.raycastTarget = false;
            return t;
        }

        public void SetZoom(int level)
        {
            ZoomLevel = Mathf.Clamp(level, 0, MapProjection.MinimapZoom.Length - 1);
            _zoomText.text = "Zoom " + (ZoomLevel + 1) + " / " + MapProjection.MinimapZoom.Length;
            _zoomTextTimer = 1.5f;
        }

        public void ToggleRotate()
        {
            Rotate = !Rotate;
            _rotLabel.text = Rotate ? "Kamera" : "Nord";
        }

        public void OnClick(Vector2 local)
        {
            var p = _ui.Player; if (p == null) return;
            var world = MapProjection.MinimapToWorld(local, p.transform.position, PixelsPerMeter, CameraYaw(), Rotate);
            _ui.OpenWorldMap(world);
        }

        static float CameraYaw() => Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;

        Sprite S(string id)
        {
            if (!_sprites.TryGetValue(id, out var s)) { s = UiFactory.Sprite(id); _sprites[id] = s; }
            return s;
        }

        public void Update(float dt)
        {
            var p = _ui.Player;
            if (p == null) return;
            var input = InputManager.Instance;
            if (input != null && !InputManager.TextInputActive)
            {
                if (input.ZoomInPressed) SetZoom(ZoomLevel + 1);
                if (input.ZoomOutPressed) SetZoom(ZoomLevel - 1);
            }
            if (_zoomTextTimer > 0f) { _zoomTextTimer -= dt; if (_zoomTextTimer <= 0f) _zoomText.text = ""; }

            Vector3 center = p.transform.position;
            float ppm = PixelsPerMeter, yaw = CameraYaw();
            float mapRot = Rotate ? yaw : 0f;
            var uv = MapProjection.WorldToUv(center);
            _map.pivot = uv;
            _map.anchoredPosition = Vector2.zero;
            _map.sizeDelta = Vector2.one * MapProjection.ZoneSize * ppm;
            _map.localEulerAngles = new Vector3(0, 0, mapRot);
            var prof = PlayerProfile.Instance;
            if (prof != null) _fog.Update(prof.State.Fog);
            _player.rectTransform.localEulerAngles = new Vector3(0, 0, mapRot - p.transform.eulerAngles.y);

            var half = Size * 0.5f;
            PlaceCompass(_north, new Vector2(0, 1), mapRot, half);
            PlaceCompass(_east, new Vector2(1, 0), mapRot, half);
            PlaceCompass(_south, new Vector2(0, -1), mapRot, half);
            PlaceCompass(_west, new Vector2(-1, 0), mapRot, half);

            string area = ZoneRoot.RegionNameAt(center);
            if (!ReferenceEquals(area, _areaName)) { _areaName = area; _area.text = area; }

            MapMarkerRegistry.Collect(_list);
            int mi = 0, ci = 0;
            var fog = prof != null ? prof.State.Fog : null;
            for (int i = 0; i < _list.Count; i++)
            {
                var m = _list[i];
                var local = MapProjection.WorldToMinimap(m.Position, center, ppm, yaw, Rotate);
                if (m.Radius > 0f)
                {
                    float r = m.Radius * ppm;
                    if (Mathf.Abs(local.x) - r < half.x && Mathf.Abs(local.y) - r < half.y)
                    {
                        var c = Pooled(_circlePool, _circles, ci++, "ui_ring");
                        c.rectTransform.anchoredPosition = local; c.rectTransform.sizeDelta = Vector2.one * r * 2f;
                        c.color = new Color(1f, 0.6f, 0.15f, 0.75f);
                    }
                }
                bool inside = MapProjection.InsideRect(local, half - new Vector2(6, 6));
                if (!inside)
                {
                    if (!MapMarkerRegistry.ClampOnMinimap(m.Kind)) continue;
                    local = MapProjection.ClampToRect(local, half - new Vector2(9, 9), out _);
                }
                var img = Pooled(_markerPool, _markers, mi++, null);
                img.sprite = S(MapMarkerRegistry.IconFor(m));
                img.rectTransform.anchoredPosition = local;
                float size = m.Kind == MapMarkerKind.Ally || m.Kind == MapMarkerKind.Target ? 10f : m.Kind == MapMarkerKind.Event || m.Kind == MapMarkerKind.EventBoss ? 22f : 18f;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                img.color = MarkerColor(m, fog);
            }
            for (int i = mi; i < _markerPool.Count; i++) if (_markerPool[i].enabled) _markerPool[i].enabled = false;
            for (int i = ci; i < _circlePool.Count; i++) if (_circlePool[i].enabled) _circlePool[i].enabled = false;
        }

        public static Color MarkerColor(MapMarkerInfo m, FogModel fog)
        {
            bool fogged = fog != null && m.Kind != MapMarkerKind.Event && m.Kind != MapMarkerKind.EventBoss && m.Kind != MapMarkerKind.Ally && !fog.IsRevealed(m.Position);
            if (fogged || m.State == MarkerState.Undiscovered) return new Color(0.6f, 0.6f, 0.6f, 0.55f);
            if (m.State == MarkerState.Done || m.State == MarkerState.Inactive) return new Color(0.55f, 0.55f, 0.55f, 0.9f);
            return Color.white;
        }

        void PlaceCompass(TextMeshProUGUI t, Vector2 dir, float mapRot, Vector2 half)
        {
            var d = MapProjection.Rotate(dir, mapRot) * 1000f;
            t.rectTransform.anchoredPosition = MapProjection.ClampToRect(d, half - new Vector2(11, 11), out _);
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

        public void OnScroll(float delta) { if (delta > 0) SetZoom(ZoomLevel + 1); else if (delta < 0) SetZoom(ZoomLevel - 1); }
    }

    /// <summary>Mausrad und Klick auf der Minimap.</summary>
    public class MinimapInput : MonoBehaviour, IScrollHandler, IPointerClickHandler
    {
        [System.NonSerialized] public MinimapHud Owner;
        public void OnScroll(PointerEventData e) => Owner?.OnScroll(e.scrollDelta.y);
        public void OnPointerClick(PointerEventData e)
        {
            if (Owner == null || e.button != PointerEventData.InputButton.Left) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, null, out var local);
            Owner.OnClick(local);
        }
    }
}
