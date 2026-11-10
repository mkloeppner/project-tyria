using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Scrollbarer Bereich (Mausrad, Ziehen, Bildlaufleiste rechts, blendet sich aus, wenn alles passt).
    /// Der Inhalt wächst mit seinen Kindern (<see cref="ContentSizeFitter"/>); der Aufrufer hängt die Layout-Gruppe an <c>content</c>.
    /// </summary>
    public static class UiScroll
    {
        public const float BarWidth = 8f, BarGap = 6f;

        /// <summary>Legt den Bereich an und liefert den Viewport (zum Platzieren) sowie den Inhalt (für die Einträge).</summary>
        public static RectTransform Create(string name, Transform parent, out RectTransform content, float sensitivity = 40f)
        {
            var view = UiFactory.Image(name, parent, new Color(0f, 0f, 0f, 0.16f), null, true);   // fängt das Mausrad auch in Lücken
            var vr = view.rectTransform;
            view.gameObject.AddComponent<RectMask2D>();
            var scroll = view.gameObject.AddComponent<ScrollRect>();

            content = UiFactory.Rect("Content", vr);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(-(BarWidth + BarGap), 0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var track = UiFactory.Image("Scrollbar", vr, new Color(1f, 1f, 1f, 0.07f), UiFactory.Sprite("ui_round"), true);
            track.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-1, 0), new Vector2(BarWidth, -6));
            var area = UiFactory.Rect("Sliding", track.transform).Stretch();
            var handle = UiFactory.Image("Handle", area, new Color(1f, 0.82f, 0.4f, 0.6f), UiFactory.Sprite("ui_round"), true);
            handle.rectTransform.Stretch();
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;

            scroll.content = content; scroll.viewport = vr;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false; scroll.scrollSensitivity = sensitivity;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return vr;
        }

        /// <summary>Scrollbereich mit senkrechter Liste (Kinder steuern ihre Höhe selbst).</summary>
        public static RectTransform List(string name, Transform parent, out RectTransform content, float spacing = 4f, int padding = 4)
        {
            var vr = Create(name, parent, out content);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return vr;
        }

        /// <summary>Text, der in einer Liste seine volle Höhe bekommt (Umbruch an der Listenbreite).</summary>
        public static TextMeshProUGUI ListText(string name, Transform list, string text, float size, Color color)
        {
            var t = UiFactory.Text(name, list, text, size, TextAlignmentOptions.TopLeft, color);
            t.enableAutoSizing = false; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
