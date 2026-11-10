using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Bausteine für die komplett im Code erzeugte uGUI-Oberfläche.</summary>
    public static class UiFactory
    {
        /// <summary>Lauf 5 / M24: UI-Klang-Ausgabe (vom AudioPool gesetzt). Ids ohne „UI/“-Präfix, z. B. „Click“.</summary>
        public static Action<string> UiSound;
        public static void Sound(string id) { UiSound?.Invoke(id); }
        public static IconLibrary Icons;
        public static readonly Color PanelColor = new Color(0.07f, 0.065f, 0.09f, 0.985f);
        public static readonly Color HeaderColor = new Color(0.16f, 0.12f, 0.08f, 0.98f);
        public static readonly Color AccentColor = new Color(1f, 0.82f, 0.4f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.19f, 0.15f, 1f);
        public static readonly Color SlotColor = new Color(0.12f, 0.11f, 0.14f, 1f);

        public static Sprite Sprite(string id) => Icons != null ? Icons.Get(id) : null;
        static Material _outlineMat;

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            return r;
        }

        public static RectTransform Place(this RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(this RectTransform r, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
            return r;
        }

        public static Image Image(string name, Transform parent, Color c, Sprite sprite = null, bool raycast = false)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = c; img.sprite = sprite; img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (t.font == null && TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            if (t.font != null)
            {
                if (_outlineMat == null)
                {
                    _outlineMat = new Material(t.font.material) { name = "PT_TMP_Outline" };
                    _outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
                    _outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0, 0, 0, 0.7f));
                    _outlineMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                }
                t.fontSharedMaterial = _outlineMat;
            }
            t.text = text; t.fontSize = size; t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.color = color ?? Color.white;

            return t;
        }

        public static Button Button(string name, Transform parent, string label, Action onClick, float fontSize = 16f)
        {
            var img = Image(name, parent, ButtonColor, Sprite("ui_round"), true);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.25f, 1.15f, 0.95f); colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<UiButtonFeedback>();   // Lauf 5 / M24: Hover-/Klick-Klang, Drücken 0,96 für 60 ms
            var t = Text("Label", img.transform, label, fontSize, TextAlignmentOptions.Center, AccentColor);
            t.rectTransform.Stretch(4, 4, 2, 2);
            return b;
        }

        /// <summary>Lauf 5 / M24: Schieberegler (Hintergrund, Füllung, Griff) für die Optionen.</summary>
        public static Slider Slider(string name, Transform parent, float min, float max, float value, bool whole)
        {
            var root = Rect(name, parent);
            var bg = Image("Background", root, new Color(0.16f, 0.15f, 0.18f), Sprite("ui_round"), true); bg.rectTransform.Stretch();
            var fillArea = Rect("FillArea", root).Stretch(4, 4, 4, 4);
            var fill = Image("Fill", fillArea, new Color(0.75f, 0.58f, 0.28f), Sprite("ui_round")); fill.rectTransform.Stretch();
            var handleArea = Rect("HandleArea", root).Stretch(8, 8, 0, 0);
            var handle = Image("Handle", handleArea, AccentColor, Sprite("ui_round"), true); handle.rectTransform.sizeDelta = new Vector2(18, 0);
            var s = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole; s.SetValueWithoutNotify(value);
            root.gameObject.AddComponent<UiButtonFeedback>().ClickSound = null;
            return s;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null && t.text != text) t.text = text;
        }

        public static TMP_InputField InputField(string name, Transform parent, string placeholder)
        {
            var bg = Image(name, parent, new Color(0.02f, 0.02f, 0.03f, 1f), Sprite("ui_round"), true);
            var field = bg.gameObject.AddComponent<TMP_InputField>();
            var area = Rect("Text Area", bg.transform).Stretch(8, 8, 4, 4);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text("Placeholder", area, placeholder, 16, TextAlignmentOptions.Left, new Color(1, 1, 1, 0.35f));
            ph.rectTransform.Stretch(); ph.textWrappingMode = TextWrappingModes.NoWrap;
            var tx = Text("Text", area, "", 16, TextAlignmentOptions.Left);
            tx.rectTransform.Stretch(); tx.textWrappingMode = TextWrappingModes.NoWrap;
            field.textViewport = area; field.textComponent = tx; field.placeholder = ph;
            field.fontAsset = tx.font;
            field.onSelect.AddListener(_ => InputManager.TextInputActive = true);
            field.onDeselect.AddListener(_ => InputManager.TextInputActive = false);
            field.onEndEdit.AddListener(_ => InputManager.TextInputActive = false);
            return field;
        }

        /// <summary>Vertikale Liste mit automatischer Höhe.</summary>
        public static VerticalLayoutGroup VList(Transform parent, float spacing = 4f, int padding = 6, string name = "List")
        {
            var r = Rect(name, parent);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }

        public static LayoutElement Layout(Component c, float prefHeight = -1, float prefWidth = -1, float flexW = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (prefHeight >= 0) le.preferredHeight = prefHeight;
            if (prefWidth >= 0) le.preferredWidth = prefWidth;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>Verschiebt das Fenster beim Ziehen der Titelleiste.</summary>
    public class UiDragHandle : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform Target;
        Canvas _canvas;
        public void OnBeginDrag(PointerEventData e) { _canvas = GetComponentInParent<Canvas>(); }
        public void OnDrag(PointerEventData e)
        {
            if (Target == null) return;
            float s = _canvas != null ? _canvas.scaleFactor : 1f;
            Target.anchoredPosition += e.delta / Mathf.Max(0.01f, s);
        }
    }

    /// <summary>Bringt ein Fenster beim Anklicken nach vorn (Fokus-Stapel).</summary>
    public class UiFocusOnClick : MonoBehaviour, IPointerDownHandler
    {
        [NonSerialized] public UiWindow Window;
        public void OnPointerDown(PointerEventData e) { Window?.Manager.Focus(Window); }
    }

    /// <summary>Gemeinsames Fenster-Fundament: Titelleiste, Schließen, Esc, Fokus, Ziehen.</summary>
    public class UiWindow
    {
        public readonly string Id;
        public readonly WindowManager Manager;
        public readonly RectTransform Root;
        public readonly RectTransform Content;
        public readonly TextMeshProUGUI Title;
        public readonly RectTransform Header;
        public bool IsOpen => Root.gameObject.activeSelf;
        public bool Modal;
        public event Action Opened, Closed;

        public UiWindow(WindowManager mgr, string id, string title, Vector2 size, Vector2? position = null)
        {
            Id = id; Manager = mgr;
            var bg = UiFactory.Image("Window_" + id, mgr.Layer, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            Root = bg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position ?? Vector2.zero, size);
            bg.gameObject.AddComponent<UiFocusOnClick>().Window = this;
            var header = UiFactory.Image("Header", Root, UiFactory.HeaderColor, UiFactory.Sprite("ui_round"), true);
            Header = header.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 34));
            header.gameObject.AddComponent<UiDragHandle>().Target = Root;
            Title = UiFactory.Text("Title", Header, title, 19, TextAlignmentOptions.Left, UiFactory.AccentColor);
            Title.rectTransform.Stretch(12, 44, 0, 0);
            Title.fontStyle = FontStyles.Bold;
            var close = UiFactory.Button("Close", Header, "×", Close, 18);
            ((RectTransform)close.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(30, 28));
            UiFactory.SetLabel(close, "X");
            Content = UiFactory.Rect("Content", Root).Stretch(10, 10, 42, 10);
            Root.gameObject.SetActive(false);
            mgr.Register(this);
        }

        public string OpenSound = "WindowOpen", CloseSound = "WindowClose";
        public void Open() { if (IsOpen) { Manager.Focus(this); return; } Root.gameObject.SetActive(true); Manager.OnOpened(this); UiFactory.Sound(OpenSound); UiWindowAnim.Play(Root); Opened?.Invoke(); }
        public void Close() { if (!IsOpen) return; Root.gameObject.SetActive(false); Manager.OnClosed(this); UiFactory.Sound(CloseSound); Closed?.Invoke(); }
        public void Toggle() { if (IsOpen) Close(); else Open(); }
    }

    /// <summary>Verwaltet offene Fenster (Fokus-Stapel), Esc schließt das oberste.</summary>
    public class WindowManager
    {
        public readonly RectTransform Layer;
        readonly List<UiWindow> _all = new List<UiWindow>();
        readonly List<UiWindow> _stack = new List<UiWindow>();
        public event Action<UiWindow> WindowClosed;

        public WindowManager(RectTransform layer) { Layer = layer; }
        public void Register(UiWindow w) => _all.Add(w);
        public bool AnyOpen => _stack.Count > 0;
        public UiWindow Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public IReadOnlyList<UiWindow> Open => _stack;

        public void OnOpened(UiWindow w) { _stack.Remove(w); _stack.Add(w); w.Root.SetAsLastSibling(); }
        public void OnClosed(UiWindow w) { _stack.Remove(w); WindowClosed?.Invoke(w); }
        public void Focus(UiWindow w) { if (w.IsOpen) { _stack.Remove(w); _stack.Add(w); w.Root.SetAsLastSibling(); } }
        public bool CloseTop() { var t = Top; if (t == null) return false; t.Close(); return true; }
        public void CloseAll() { for (int i = _stack.Count - 1; i >= 0; i--) _stack[i].Close(); }

        public bool IsPointerOverWindow(Vector2 screen)
        {
            for (int i = 0; i < _stack.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(_stack[i].Root, screen, null)) return true;
            return false;
        }
    }

    // ---------------- Tooltips ----------------
    public class TooltipLine
    {
        public Sprite Icon; public Color IconColor = Color.white; public string Text; public Color TextColor = Color.white; public float Size = 15f;
    }

    public class TooltipContent
    {
        public string Title; public Color TitleColor = Color.white; public string Subtitle; public string TopRight; public Sprite TopRightIcon;
        public readonly List<TooltipLine> Lines = new List<TooltipLine>();
        public string Footer; public Color FooterColor = new Color(0.75f, 0.75f, 0.75f);
        public TooltipContent Compare; // optional (Shift)
        public TooltipLine Add(string text, Color? color = null, Sprite icon = null, Color? iconColor = null)
        {
            var l = new TooltipLine { Text = text, TextColor = color ?? Color.white, Icon = icon, IconColor = iconColor ?? Color.white };
            Lines.Add(l); return l;
        }
    }

    /// <summary>Ein Tooltip-Controller für Items, Skills, Eigenschaften und Karten-Marker (GW2-Layout).</summary>
    public class UiTooltip
    {
        readonly RectTransform _root, _compareRoot;
        readonly Panel _main, _compare;
        object _owner;

        class Panel
        {
            public RectTransform Root; public TextMeshProUGUI Title, Subtitle, TopRight, Footer; public RectTransform Lines;
            public readonly List<(Image icon, TextMeshProUGUI text, GameObject row)> Rows = new List<(Image, TextMeshProUGUI, GameObject)>();
        }

        public UiTooltip(RectTransform layer)
        {
            _root = UiFactory.Rect("Tooltip", layer);
            _root.Place(Vector2.zero, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(360, 10));
            _main = Build(_root, "Main");
            _compareRoot = UiFactory.Rect("TooltipCompare", layer);
            _compareRoot.Place(Vector2.zero, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(360, 10));
            _compare = Build(_compareRoot, "Compare");
            _root.gameObject.SetActive(false); _compareRoot.gameObject.SetActive(false);
        }

        Panel Build(RectTransform parent, string name)
        {
            var p = new Panel();
            var bg = UiFactory.Image(name, parent, new Color(0.03f, 0.03f, 0.05f, 0.96f), UiFactory.Sprite("ui_panel"));
            p.Root = bg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(360, 10));
            var v = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 8, 8); v.spacing = 3; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var head = UiFactory.Rect("Head", bg.transform); UiFactory.Layout(head, 24);
            p.Title = UiFactory.Text("Title", head, "", 18, TextAlignmentOptions.Left); p.Title.rectTransform.Stretch(0, 70, 0, 0); p.Title.fontStyle = FontStyles.Bold;
            p.TopRight = UiFactory.Text("TopRight", head, "", 15, TextAlignmentOptions.Right, new Color(0.8f, 0.85f, 1f)); p.TopRight.rectTransform.Stretch(200, 0, 0, 0);
            p.Subtitle = UiFactory.Text("Subtitle", bg.transform, "", 14, TextAlignmentOptions.Left, new Color(0.75f, 0.75f, 0.8f));
            p.Lines = UiFactory.Rect("Lines", bg.transform);
            var lv = p.Lines.gameObject.AddComponent<VerticalLayoutGroup>(); lv.spacing = 2; lv.childControlHeight = true; lv.childControlWidth = true; lv.childForceExpandHeight = false;
            p.Footer = UiFactory.Text("Footer", bg.transform, "", 13, TextAlignmentOptions.Left);
            return p;
        }

        void Fill(Panel p, TooltipContent c)
        {
            p.Title.text = c.Title; p.Title.color = c.TitleColor;
            p.TopRight.text = c.TopRight ?? "";
            p.Subtitle.text = c.Subtitle ?? ""; p.Subtitle.gameObject.SetActive(!string.IsNullOrEmpty(c.Subtitle));
            while (p.Rows.Count < c.Lines.Count)
            {
                var row = UiFactory.Rect("Row", p.Lines);
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 6; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childAlignment = TextAnchor.MiddleLeft;
                var ic = UiFactory.Image("Icon", row, Color.white); UiFactory.Layout(ic, 18, 18);
                var tx = UiFactory.Text("Text", row, "", 15); UiFactory.Layout(tx, -1, -1, 1);
                p.Rows.Add((ic, tx, row.gameObject));
            }
            for (int i = 0; i < p.Rows.Count; i++)
            {
                bool on = i < c.Lines.Count;
                p.Rows[i].row.SetActive(on);
                if (!on) continue;
                var l = c.Lines[i];
                p.Rows[i].icon.gameObject.SetActive(l.Icon != null);
                p.Rows[i].icon.sprite = l.Icon; p.Rows[i].icon.color = l.IconColor;
                p.Rows[i].text.text = l.Text; p.Rows[i].text.color = l.TextColor; p.Rows[i].text.fontSize = l.Size;
            }
            p.Footer.text = c.Footer ?? ""; p.Footer.color = c.FooterColor; p.Footer.gameObject.SetActive(!string.IsNullOrEmpty(c.Footer));
        }

        public void Show(object owner, TooltipContent c, Vector2 screenPos, float canvasScale)
        {
            if (c == null) { Hide(owner); return; }
            _owner = owner;
            Fill(_main, c);
            _root.gameObject.SetActive(true);
            Vector2 pos = screenPos / Mathf.Max(0.01f, canvasScale) + new Vector2(18, -18);
            float maxX = Screen.width / Mathf.Max(0.01f, canvasScale) - 370f;
            bool compare = c.Compare != null;
            if (compare) maxX -= 370f;
            pos.x = Mathf.Min(pos.x, maxX);
            float h = Mathf.Max(60f, _main.Root.rect.height);
            pos.y = Mathf.Max(pos.y, h + 10f);
            _root.anchoredPosition = pos;
            _compareRoot.gameObject.SetActive(compare);
            if (compare)
            {
                Fill(_compare, c.Compare);
                _compareRoot.anchoredPosition = pos + new Vector2(370f, 0f);
            }
            _root.SetAsLastSibling(); _compareRoot.SetAsLastSibling();
        }

        public void Hide(object owner = null)
        {
            if (owner != null && owner != _owner) return;
            _owner = null;
            _root.gameObject.SetActive(false); _compareRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>Löst den Tooltip beim Überfahren aus (Inhalt wird erst beim Hover erzeugt).</summary>
    public class UiTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public Func<TooltipContent> Provider;
        bool _over;
        public void OnPointerEnter(PointerEventData e) { _over = true; Show(e.position); }
        public void OnPointerMove(PointerEventData e) { if (_over) Show(e.position); }
        public void OnPointerExit(PointerEventData e) { _over = false; UIController.Instance?.Tooltip.Hide(this); }
        void OnDisable() { if (_over) { _over = false; UIController.Instance?.Tooltip.Hide(this); } }
        void Show(Vector2 p)
        {
            var ui = UIController.Instance;
            if (ui == null || Provider == null) return;
            ui.Tooltip.Show(this, Provider(), p, ui.CanvasScale);
        }
    }

    /// <summary>Klick-Ereignisse (links/rechts) für UI-Elemente.</summary>
    public class UiClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public Action Left, Right;
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Left?.Invoke();
            else if (e.button == PointerEventData.InputButton.Right) Right?.Invoke();
        }
    }

    /// <summary>Modaler Dialog (Bestätigung oder Mengenwahl).</summary>
    public class UiDialog
    {
        readonly UiWindow _w;
        readonly TextMeshProUGUI _text;
        readonly TMP_InputField _amount;
        readonly Button _ok, _cancel;
        Action<int> _onOk;

        public bool IsOpen => _w.IsOpen;

        public UiDialog(WindowManager mgr)
        {
            _w = new UiWindow(mgr, "dialog", "Bestätigen", new Vector2(420, 210), new Vector2(0, 80));
            _w.Modal = true;
            _text = UiFactory.Text("Text", _w.Content, "", 17, TextAlignmentOptions.Center);
            _text.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 80));
            _amount = UiFactory.InputField("Amount", _w.Content, "Menge");
            ((RectTransform)_amount.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(140, 32));
            _amount.contentType = TMP_InputField.ContentType.IntegerNumber;
            _ok = UiFactory.Button("Ok", _w.Content, "OK", Confirm);
            ((RectTransform)_ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-8, 6), new Vector2(130, 34));
            _cancel = UiFactory.Button("Cancel", _w.Content, "Abbrechen", () => _w.Close());
            ((RectTransform)_cancel.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(8, 6), new Vector2(130, 34));
        }

        public void Confirm(string title, string text, Action onOk, string okLabel = "OK")
        {
            _w.Title.text = title; _text.text = text; _amount.gameObject.SetActive(false);
            UiFactory.SetLabel(_ok, okLabel);
            _onOk = _ => onOk?.Invoke();
            _w.Open();
        }

        public void AskAmount(string title, string text, int max, int initial, Action<int> onOk)
        {
            _w.Title.text = title; _text.text = text; _amount.gameObject.SetActive(true);
            _amount.text = initial.ToString();
            UiFactory.SetLabel(_ok, "Teilen");
            _onOk = v => onOk?.Invoke(Mathf.Clamp(v, 1, max));
            _w.Open();
            _amount.Select(); _amount.ActivateInputField();
        }

        void Confirm()
        {
            int v = 1;
            if (_amount.gameObject.activeSelf) int.TryParse(_amount.text, out v);
            var cb = _onOk; _onOk = null;
            _w.Close();
            InputManager.TextInputActive = false;
            cb?.Invoke(v);
        }
    }

    /// <summary>Lauf 5 / M24: Hover-Klang + Klick-Klang + kurzes Drücken (Skalierung 0,96 für 60 ms) auf jedem Button.</summary>
    public class UiButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler
    {
        public string HoverSound = "Hover", ClickSound = "Click";
        float _pressT;
        public void OnPointerEnter(PointerEventData e) { var b = GetComponent<Button>(); if (!string.IsNullOrEmpty(HoverSound) && (b == null || b.interactable)) UiFactory.Sound(HoverSound); }
        public void OnPointerDown(PointerEventData e) { _pressT = 0.06f; transform.localScale = Vector3.one * 0.96f; }
        public void OnPointerClick(PointerEventData e) { var b = GetComponent<Button>(); if (!string.IsNullOrEmpty(ClickSound) && (b == null || b.interactable)) UiFactory.Sound(ClickSound); }
        void Update() { if (_pressT > 0f) { _pressT -= Time.unscaledDeltaTime; if (_pressT <= 0f) transform.localScale = Vector3.one; } }
        void OnDisable() { transform.localScale = Vector3.one; _pressT = 0f; }
    }

    /// <summary>Lauf 5 / M24: Fenster öffnen mit kurzem Einblenden und Hochskalieren (120 ms).</summary>
    public class UiWindowAnim : MonoBehaviour
    {
        CanvasGroup _g; float _t;
        public static void Play(RectTransform root)
        {
            var a = root.GetComponent<UiWindowAnim>(); if (a == null) a = root.gameObject.AddComponent<UiWindowAnim>();
            a._t = 0f; if (a._g == null) { a._g = root.GetComponent<CanvasGroup>(); if (a._g == null) a._g = root.gameObject.AddComponent<CanvasGroup>(); }
            a.enabled = true; a.Step();
        }
        void Update() { Step(); }
        void Step()
        {
            _t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(_t / 0.12f), e = 1f - (1f - k) * (1f - k);
            if (_g != null) _g.alpha = e; transform.localScale = Vector3.one * (0.94f + 0.06f * e);
            if (k >= 1f) enabled = false;
        }
    }
}
