using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Ereignis-/Aufgaben-Tracker rechts an der Minimap (max. 4 Einträge). Klick → Weltkarte auf das Ziel zentriert.</summary>
    public class TrackerHud
    {
        public const int MaxEntries = 4;
        class Entry
        {
            public GameObject Root; public TextMeshProUGUI Title, Desc, Timer, Medal; public Image Fill;
            public Vector3 Focus; public bool HasFocus; public int LastTimer = -2;
        }
        readonly Entry[] _entries = new Entry[MaxEntries];
        public readonly RectTransform Root;
        readonly UIController _ui;

        public TrackerHud(RectTransform hud, UIController ui, float bottom)
        {
            _ui = ui;
            Root = UiFactory.Rect("Tracker", hud).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, bottom), new Vector2(330, 0));
            var v = Root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.LowerRight; v.spacing = 6; v.childControlHeight = false; v.childControlWidth = true; v.childForceExpandHeight = false;
            Root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = MaxEntries - 1; i >= 0; i--) _entries[i] = Build(i);
        }

        Entry Build(int i)
        {
            var e = new Entry();
            var bg = UiFactory.Image("Entry" + i, Root, new Color(0.05f, 0.045f, 0.06f, 0.78f), UiFactory.Sprite("ui_round"), true);
            bg.rectTransform.sizeDelta = new Vector2(330, 96);
            e.Root = bg.gameObject;
            e.Title = UiFactory.Text("Title", bg.transform, "", 17, TextAlignmentOptions.Left, new Color(1f, 0.75f, 0.35f));
            e.Title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -5), new Vector2(-20, 22));
            e.Title.fontStyle = FontStyles.Bold;
            e.Title.enableAutoSizing = true; e.Title.fontSizeMin = 12; e.Title.fontSizeMax = 17; e.Title.textWrappingMode = TextWrappingModes.NoWrap; e.Title.overflowMode = TextOverflowModes.Truncate;
            e.Desc = UiFactory.Text("Desc", bg.transform, "", 13, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.92f));
            e.Desc.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -27), new Vector2(-20, 36));
            var barBg = UiFactory.Image("Bar", bg.transform, new Color(0, 0, 0, 0.6f), UiFactory.Sprite("ui_round"));
            barBg.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(10, 24), new Vector2(-20, 10));
            e.Fill = UiFactory.Image("Fill", barBg.transform, new Color(1f, 0.62f, 0.2f), UiFactory.Sprite("ui_round"));
            e.Fill.type = Image.Type.Filled; e.Fill.fillMethod = Image.FillMethod.Horizontal; e.Fill.rectTransform.Stretch(1, 1, 1, 1);
            e.Timer = UiFactory.Text("Timer", bg.transform, "", 13, TextAlignmentOptions.Left);
            e.Timer.rectTransform.Place(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(10, 3), new Vector2(0, 20));
            e.Medal = UiFactory.Text("Medal", bg.transform, "", 13, TextAlignmentOptions.Right);
            e.Medal.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 3), new Vector2(0, 20));
            var relay = bg.gameObject.AddComponent<UiClickRelay>();
            relay.Left = () => { if (e.HasFocus) _ui.OpenWorldMap(e.Focus); };
            e.Root.SetActive(false);
            return e;
        }

        public bool AnyVisible { get { foreach (var e in _entries) if (e.Root.activeSelf) return true; return false; } }

        /// <summary>Setzt einen Eintrag (nur geänderte Texte werden neu geschrieben).</summary>
        public void SetEntry(int index, bool visible, string title = "", string desc = "", float progress = -1f, float timeLeft = -1f, string medal = "", Color medalColor = default, Vector3? focus = null, Color? barColor = null)
        {
            if (index < 0 || index >= MaxEntries) return;
            var e = _entries[index];
            if (e.Root.activeSelf != visible) e.Root.SetActive(visible);
            if (!visible) return;
            if (e.Title.text != title) e.Title.text = title;
            if (e.Desc.text != desc) e.Desc.text = desc;
            bool bar = progress >= 0f;
            if (e.Fill.transform.parent.gameObject.activeSelf != bar) e.Fill.transform.parent.gameObject.SetActive(bar);
            if (bar) e.Fill.fillAmount = Mathf.Clamp01(progress);
            if (barColor.HasValue) e.Fill.color = barColor.Value;
            int t = timeLeft >= 0f ? Mathf.CeilToInt(timeLeft) : -1;
            if (t != e.LastTimer) { e.LastTimer = t; e.Timer.text = t >= 0 ? (t / 60) + ":" + (t % 60).ToString("00") : ""; }
            if (e.Medal.text != medal) e.Medal.text = medal;
            e.Medal.color = medalColor == default ? Color.white : medalColor;
            e.HasFocus = focus.HasValue; if (focus.HasValue) e.Focus = focus.Value;
        }

        public void HideFrom(int index) { for (int i = Mathf.Max(0, index); i < MaxEntries; i++) SetEntry(i, false); }
    }
}
