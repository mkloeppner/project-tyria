using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Meldungen als kurzlebige Toasts mit eigenem Hintergrund, oben mittig über allen Fenstern.
    /// Neue Meldungen stapeln sich darunter (höchstens <see cref="MaxVisible"/>), gleiche Meldungen zählen hoch („×3“) statt sich zu überlagern.
    /// Anzeigedauer wächst mit der Textlänge (<see cref="Lifetime"/>).
    /// </summary>
    public class UiToastStack
    {
        public const int MaxVisible = 5;
        public const int MaxChars = 600;
        public const float MaxTextWidth = 680f, FadeIn = 0.15f, FadeOut = 0.45f;

        class Toast
        {
            public RectTransform Root; public CanvasGroup Group; public Image Bar; public TextMeshProUGUI Text; public LayoutElement TextLayout;
            public string Message; public int Count; public float Age, Life;
        }

        readonly RectTransform _root;
        readonly List<Toast> _live = new List<Toast>();
        readonly Stack<Toast> _pool = new Stack<Toast>();

        public int VisibleCount => _live.Count;

        /// <summary>Anzeigedauer in Sekunden: 3 s Grundzeit, längere Texte bis 8 s.</summary>
        public static float Lifetime(string msg) => Mathf.Clamp(2.4f + (msg != null ? msg.Length : 0) * 0.045f, 3f, 8f);

        public UiToastStack(RectTransform layer)
        {
            _root = UiFactory.Rect("Toasts", layer).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(MaxTextWidth + 60, 0));
            var v = _root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = false; v.childForceExpandHeight = false;
            _root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Show(string msg, Color color)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (msg.Length > MaxChars) msg = msg.Substring(0, MaxChars - 1) + "…";
            foreach (var t in _live)
                if (t.Message == msg) { t.Count++; t.Age = FadeIn; t.Text.text = msg + "  <color=#C8C8C8>×" + t.Count + "</color>"; Fit(t); return; }
            while (_live.Count >= MaxVisible) Release(_live[0]);
            var n = _pool.Count > 0 ? _pool.Pop() : Build();
            n.Message = msg; n.Count = 1; n.Age = 0f; n.Life = Lifetime(msg);
            n.Bar.color = new Color(color.r, color.g, color.b, 1f);
            n.Text.text = msg; n.Text.color = Color.Lerp(color, Color.white, 0.55f);
            Fit(n);
            n.Group.alpha = 0f; n.Root.gameObject.SetActive(true); n.Root.SetAsLastSibling();
            _live.Add(n);
        }

        /// <summary>Unskaliert aufrufen (läuft auch bei Pause/Zeitlupe).</summary>
        public void Tick(float dt)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var t = _live[i]; t.Age += dt;
                if (t.Age >= t.Life) { Release(t); continue; }
                float a = t.Age < FadeIn ? t.Age / FadeIn : Mathf.Clamp01((t.Life - t.Age) / FadeOut);
                t.Group.alpha = a;
            }
        }

        public void Clear() { for (int i = _live.Count - 1; i >= 0; i--) Release(_live[i]); }

        void Release(Toast t) { _live.Remove(t); t.Root.gameObject.SetActive(false); _pool.Push(t); }

        void Fit(Toast t)
        {
            var pref = t.Text.GetPreferredValues(t.Text.text, MaxTextWidth, 0f);
            t.TextLayout.preferredWidth = Mathf.Min(MaxTextWidth, Mathf.Ceil(pref.x) + 2f);
        }

        Toast Build()
        {
            var t = new Toast();
            var bg = UiFactory.Image("Toast", _root, new Color(0.05f, 0.045f, 0.065f, 0.93f), UiFactory.Sprite("ui_round"));
            t.Root = bg.rectTransform;
            t.Group = bg.gameObject.AddComponent<CanvasGroup>(); t.Group.blocksRaycasts = false; t.Group.interactable = false;
            var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 16, 8, 9); h.spacing = 10; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            t.Bar = UiFactory.Image("Accent", bg.transform, Color.white, UiFactory.Sprite("ui_round"));
            UiFactory.Layout(t.Bar, -1, 4);
            t.Text = UiFactory.Text("Text", bg.transform, "", 18, TextAlignmentOptions.Left);
            t.Text.overflowMode = TextOverflowModes.Overflow;
            t.TextLayout = UiFactory.Layout(t.Text, -1, MaxTextWidth);
            bg.gameObject.SetActive(false);
            return t;
        }
    }
}
