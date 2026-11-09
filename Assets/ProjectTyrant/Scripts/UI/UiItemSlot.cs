using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectTyrant
{
    public enum SlotKind { Inventory, Equipment, BagSlot, VendorSell }

    /// <summary>Item-Platz mit Drag-and-drop, Tooltip (Shift = Vergleich), Rechtsklick-Menü und Doppelklick = Benutzen.</summary>
    public class UiItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [NonSerialized] public SlotKind Kind;
        [NonSerialized] public int Bag, Index;
        [NonSerialized] public EquipSlot Equip;
        [NonSerialized] public Image Icon, Frame, Bg;
        [NonSerialized] public TextMeshProUGUI CountText;
        [NonSerialized] public Func<ItemInstance> GetItem;
        public static event Action<UiItemSlot, UiItemSlot, bool> Dropped;   // Quelle, Ziel, Alt gehalten
        public static event Action<UiItemSlot> RightClicked, DoubleClicked;

        static Image _ghost;
        bool _over;
        float _lastClick;

        public static UiItemSlot Create(Transform parent, float size, SlotKind kind, string emptyLabel = null)
        {
            var bg = UiFactory.Image("Slot", parent, UiFactory.SlotColor, UiFactory.Sprite("ui_round"), true);
            bg.rectTransform.sizeDelta = new Vector2(size, size);
            var s = bg.gameObject.AddComponent<UiItemSlot>();
            s.Kind = kind; s.Bg = bg;
            s.Icon = UiFactory.Image("Icon", bg.transform, Color.white); s.Icon.rectTransform.Stretch(4, 4, 4, 4);
            s.Frame = UiFactory.Image("Frame", bg.transform, Color.clear, UiFactory.Sprite("ui_frame")); s.Frame.rectTransform.Stretch();
            s.CountText = UiFactory.Text("Count", bg.transform, "", size * 0.3f, TextAlignmentOptions.BottomRight); s.CountText.rectTransform.Stretch(2, 3, 2, 1);
            if (emptyLabel != null)
            {
                var l = UiFactory.Text("Empty", bg.transform, emptyLabel, Mathf.Max(9f, size * 0.2f), TextAlignmentOptions.Center, new Color(1, 1, 1, 0.25f));
                l.rectTransform.Stretch(2, 2, 2, 2);
            }
            return s;
        }

        public void Refresh()
        {
            var it = GetItem?.Invoke();
            bool has = it != null && it.Def != null;
            Icon.enabled = has;
            if (has)
            {
                Icon.sprite = it.Def.Icon != null ? it.Def.Icon : UiFactory.Sprite(IconLibrary.ItemId(it.Def));
                Icon.color = Icon.sprite != null ? Color.white : ItemDefinition.RarityColor(it.Def.Rarity);
                Frame.color = ItemDefinition.RarityColor(it.Def.Rarity) * new Color(1, 1, 1, it.Def.Rarity <= ItemRarity.Basic ? 0.35f : 0.95f);
                CountText.text = it.Count > 1 ? it.Count.ToString() : "";
            }
            else { Frame.color = Color.clear; CountText.text = ""; }
            var empty = transform.Find("Empty");
            if (empty != null) empty.gameObject.SetActive(!has);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            var it = GetItem?.Invoke();
            if (it == null || UIController.Instance == null) { e.pointerDrag = null; return; }
            if (_ghost == null)
            {
                _ghost = UiFactory.Image("DragGhost", UIController.Instance.OverlayLayer, Color.white);
                _ghost.rectTransform.sizeDelta = new Vector2(48, 48);
            }
            _ghost.sprite = Icon.sprite; _ghost.color = new Color(Icon.color.r, Icon.color.g, Icon.color.b, 0.6f);   // Drag-Vorschau halbtransparent
            UiFactory.Sound("DragStart");
            _ghost.gameObject.SetActive(true);
            _ghost.transform.SetAsLastSibling();
            OnDrag(e);
            UIController.Instance.Tooltip.Hide();
        }

        public void OnDrag(PointerEventData e)
        {
            if (_ghost == null) return;
            var ui = UIController.Instance;
            _ghost.rectTransform.anchorMin = _ghost.rectTransform.anchorMax = Vector2.zero;
            _ghost.rectTransform.anchoredPosition = e.position / Mathf.Max(0.01f, ui.CanvasScale);
        }

        public void OnEndDrag(PointerEventData e) { if (_ghost != null) _ghost.gameObject.SetActive(false); }

        public void OnDrop(PointerEventData e)
        {
            var src = e.pointerDrag != null ? e.pointerDrag.GetComponent<UiItemSlot>() : null;
            if (src == null || src == this) return;
            bool alt = InputManager.Instance != null && InputManager.Instance.AltHeld;
            UiFactory.Sound(alt ? "Split" : "Drop");
            Dropped?.Invoke(src, this, alt);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            if (e.button == PointerEventData.InputButton.Right) { RightClicked?.Invoke(this); return; }
            if (e.button == PointerEventData.InputButton.Left)
            {
                if (Time.unscaledTime - _lastClick < 0.35f) { DoubleClicked?.Invoke(this); _lastClick = 0f; }
                else _lastClick = Time.unscaledTime;
            }
        }

        public void OnPointerEnter(PointerEventData e) { _over = true; if (GetItem?.Invoke() != null) UiFactory.Sound("Hover"); }
        public void OnPointerExit(PointerEventData e) { _over = false; UIController.Instance?.Tooltip.Hide(this); }
        void OnDisable() { if (_over) { _over = false; UIController.Instance?.Tooltip.Hide(this); } }

        void Update()
        {
            if (!_over || UIController.Instance == null) return;
            var it = GetItem?.Invoke();
            var ui = UIController.Instance;
            if (it == null) { ui.Tooltip.Hide(this); return; }
            bool shift = InputManager.Instance != null && InputManager.Instance.ShiftHeld;
            var pos = InputManager.Instance != null ? InputManager.Instance.PointerPosition : (Vector2)Input.mousePosition;
            if (it != _shownItem || shift != _shownShift) { _shownItem = it; _shownShift = shift; _content = TooltipBuilder.Item(it, shift); }
            ui.Tooltip.Show(this, _content, pos, ui.CanvasScale);
        }

        ItemInstance _shownItem; bool _shownShift; TooltipContent _content;
    }

    /// <summary>Kontextmenü am Mauszeiger (Rechtsklick).</summary>
    public class UiContextMenu
    {
        readonly RectTransform _blocker, _panel;
        public bool IsOpen => _blocker.gameObject.activeSelf;

        public UiContextMenu(RectTransform layer)
        {
            var b = UiFactory.Image("ContextBlocker", layer, new Color(0, 0, 0, 0.001f), null, true);
            _blocker = b.rectTransform.Stretch();
            b.gameObject.AddComponent<UiClickRelay>().Left = Close;
            b.gameObject.GetComponent<UiClickRelay>().Right = Close;
            var p = UiFactory.Image("ContextMenu", _blocker, new Color(0.05f, 0.05f, 0.07f, 0.97f), UiFactory.Sprite("ui_panel"), true);
            _panel = p.rectTransform;
            _panel.anchorMin = _panel.anchorMax = Vector2.zero; _panel.pivot = new Vector2(0, 1);
            var v = p.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(6, 6, 6, 6); v.spacing = 3; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            var fit = p.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _panel.sizeDelta = new Vector2(190, 10);
            _blocker.gameObject.SetActive(false);
        }

        public void Open(Vector2 screenPos, float scale, params (string label, Action action, bool enabled)[] entries)
        {
            UiFactory.Clear(_panel);
            foreach (var e in entries)
            {
                var act = e.action;
                var btn = UiFactory.Button(e.label, _panel, e.label, () => { Close(); act?.Invoke(); }, 15);
                UiFactory.Layout(btn, 30);
                btn.interactable = e.enabled;
            }
            _panel.anchoredPosition = screenPos / Mathf.Max(0.01f, scale);
            _blocker.gameObject.SetActive(true);
            _blocker.SetAsLastSibling();
        }

        public void Close() { _blocker.gameObject.SetActive(false); }
    }
}
