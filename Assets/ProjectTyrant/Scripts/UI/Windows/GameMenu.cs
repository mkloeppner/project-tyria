using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: Spielmenü wie GW2 (Esc ohne offenes Fenster): Weiterspielen, Optionen, Charakterauswahl, Beenden. Die Welt läuft weiter;
    /// der Musik-Direktor schaltet auf Zustand/Snapshot Menü. Zustände über <see cref="MenuStateModel"/>.
    /// </summary>
    public class GameMenu
    {
        public readonly UiWindow Window;
        public readonly OptionsWindow Options;
        public readonly MenuStateModel Model = new MenuStateModel();
        /// <summary>Charakterauswahl (ab M25 gesetzt).</summary>
        public static Action OpenCharacterSelect;

        public GameMenu(WindowManager mgr)
        {
            Window = new UiWindow(mgr, "gamemenu", "Spielmenü", new Vector2(360, 330));
            Window.Modal = true;
            var list = UiFactory.VList(Window.Content, 10, 16); ((RectTransform)list.transform).Stretch();
            Add(list, "Weiterspielen", () => Model.Resume());
            Add(list, "Optionen", () => Model.OpenOptions());
            Add(list, "Charakterauswahl", () => { if (OpenCharacterSelect != null) { Model.Resume(); OpenCharacterSelect(); } else GameEventBus.Notify("Charakterauswahl folgt mit den Klassen (M25)", new Color(1f, 0.8f, 0.5f)); });
            Add(list, "Spiel beenden", Quit);
            Options = new OptionsWindow(mgr, () => Model.Escape(false));
            Model.StateChanged += Apply;
            Window.Closed += () => { if (Model.State == MenuState.Menu) Model.Resume(); };
            Options.Window.Closed += () => { if (Model.State == MenuState.Options) Model.Escape(false); };
        }

        void Add(VerticalLayoutGroup list, string label, Action a)
        {
            var b = UiFactory.Button(label, list.transform, label, a, 19); UiFactory.Layout(b, 46);
        }

        public bool IsOpen => Model.State != MenuState.Game;

        void Apply(MenuState s)
        {
            if (s == MenuState.Menu) { Options.Window.Close(); Window.Open(); }
            else if (s == MenuState.Options) { Window.Close(); Options.Open(); }
            else { Window.Close(); Options.Window.Close(); }
            var md = GameManager.Instance != null ? GameManager.Instance.GetComponent<MusicDirector>() : null;
            if (md != null) md.MenuActive = s != MenuState.Game;
        }

        static void Quit()
        {
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.RequestSave("Beenden");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Lauf 5 / M24: Optionen (Audio, Kamera, Grafik, Spiel, Steuerung). Jede Änderung wirkt sofort und hörbar.</summary>
    public class OptionsWindow
    {
        public readonly UiWindow Window;
        readonly RectTransform _body;
        readonly System.Collections.Generic.List<(Button b, RectTransform page)> _tabs = new System.Collections.Generic.List<(Button, RectTransform)>();
        GameOptions O => GameOptions.Load();

        public OptionsWindow(WindowManager mgr, Action back)
        {
            Window = new UiWindow(mgr, "options", "Optionen", new Vector2(760, 620));
            var bar = UiFactory.Rect("Tabs", Window.Content); bar.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 38));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 6; h.childForceExpandWidth = true; h.childControlWidth = true; h.childControlHeight = true;
            _body = UiFactory.Rect("Body", Window.Content).Stretch(0, 0, 46, 48);
            var backB = UiFactory.Button("Back", Window.Content, "Zurück", () => back(), 17);
            ((RectTransform)backB.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(150, 38));

            // Fix-Lauf F1: Zeilen aus dem OptionCatalog (Tooltip je Einstellung, „Standard“ je Tab)
            foreach (var tab in OptionCatalog.Tabs)
            {
                var rows = Tab(bar, tab);
                foreach (var d in OptionCatalog.All) if (d.Tab == tab) CatalogRow(rows, d);
                string tt = tab;
                var std = UiFactory.Button("Standard_" + tab, rows, "Standard", () => { OptionCatalog.ResetTab(O, tt); O.Save(); ApplyCameraDefaults(tt); RefreshRows(); UiFactory.Sound("TraitSelect"); }, 15);
                UiFactory.Layout(std, 34);
                std.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => { var c = new TooltipContent { Title = "Standard" }; c.Add("Setzt alle Einstellungen des Tabs „" + tt + "“ auf die Werkseinstellungen zurück.", new Color(0.85f, 0.85f, 0.85f)); return c; };
            }
            var keys = Tab(bar, "Tasten");
            var t = UiFactory.Text("Keys", keys, UIController.HelpTextFull, 16, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.88f, 0.9f));
            UiFactory.Layout(t, 420);
            Select(0);
        }

        public void Open() { Window.Open(); Select(0); }

        RectTransform Tab(RectTransform bar, string label)
        {
            int idx = _tabs.Count;
            var b = UiFactory.Button("Tab_" + label, bar, label, () => Select(idx), 16);
            b.GetComponent<UiButtonFeedback>().ClickSound = "Tab";
            var page = UiFactory.Rect("Page_" + label, _body).Stretch();
            ((RectTransform)UiFactory.VList(page, 8, 8, "Rows").transform).Stretch();
            page.gameObject.SetActive(false);
            _tabs.Add((b, page));
            return (RectTransform)page.Find("Rows");
        }

        void Select(int i)
        {
            for (int k = 0; k < _tabs.Count; k++)
            {
                _tabs[k].page.gameObject.SetActive(k == i);
                _tabs[k].b.GetComponent<Image>().color = k == i ? new Color(0.45f, 0.35f, 0.2f) : UiFactory.ButtonColor;
            }
        }

        readonly System.Collections.Generic.List<Action> _refresh = new System.Collections.Generic.List<Action>();
        void RefreshRows() { foreach (var r in _refresh) r(); }

        static void ApplyCameraDefaults(string tab)
        {
            if (tab != "Kamera") return;
            var rig = ThirdPersonCameraRig.Current; if (rig != null) rig.Zoom.Snap(GameOptions.Load().CameraDefaultDistance);
        }

        static readonly System.Collections.Generic.Dictionary<string, Vector3> Ranges = new System.Collections.Generic.Dictionary<string, Vector3>
        {   // min, max, ganzzahlig (1)
            { "ZoomStep", new Vector3(0.25f, 1.5f, 0) }, { "CameraDefaultDistance", new Vector3(1.5f, 12f, 0) }, { "CameraSensitivity", new Vector3(0.3f, 2.5f, 0) },
            { "ViewDistance", new Vector3(0.5f, 1.5f, 0) }, { "EffectDensity", new Vector3(0, 2, 1) }, { "CombatTextDensity", new Vector3(0, 2, 1) },
            { "UiScale", new Vector3(0.8f, 1.3f, 0) }, { "GroundHoldMs", new Vector3(100, 300, 1) },
            { "SubtitleSize", new Vector3(18, 40, 1) }, { "TextSpeed", new Vector3(0.5f, 2f, 0) },
        };

        /// <summary>Zeile nach Feldtyp: bool → Schalter, int mit Auswahl → Wechsel-Button, sonst Schieberegler; Tooltip aus dem Katalog.</summary>
        void CatalogRow(RectTransform rows, OptionCatalog.Def d)
        {
            var f = OptionCatalog.Field(d.Field); if (f == null) return;
            GameObject rowGo;
            if (d.Field == "Quality")
                rowGo = SliderRow(rows, d.Label, () => O.Quality < 0 ? QualitySettings.GetQualityLevel() : O.Quality, v => O.Quality = Mathf.RoundToInt(v), 0, Mathf.Max(1, QualitySettings.names.Length - 1), true);
            else if (f.FieldType == typeof(bool))
                rowGo = ToggleRow(rows, d.Label, () => (bool)f.GetValue(O), v => f.SetValue(O, v));
            else if (d.Choices != null)
                rowGo = ChoiceRow(rows, d.Label, d.Choices, () => (int)f.GetValue(O), v => f.SetValue(O, v));
            else
            {
                var r = Ranges.TryGetValue(d.Field, out var rr) ? rr : new Vector3(0f, 1f, 0f);
                bool whole = r.z > 0.5f;
                if (f.FieldType == typeof(int)) rowGo = SliderRow(rows, d.Label, () => (int)f.GetValue(O), v => f.SetValue(O, Mathf.RoundToInt(v)), r.x, r.y, true);
                else rowGo = SliderRow(rows, d.Label, () => (float)f.GetValue(O), v => { f.SetValue(O, v); if (d.Field == "CameraDefaultDistance") ApplyCameraDefaults("Kamera"); }, r.x, r.y, whole);
            }
            var def = d;
            foreach (var g in rowGo.GetComponentsInChildren<Graphic>(true)) if (g.raycastTarget && g.GetComponent<UiTooltipTrigger>() == null) { g.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => OptionTooltip(def); break; }
            var trig = rowGo.GetComponent<UiTooltipTrigger>(); if (trig == null) { var img = rowGo.GetComponent<Image>() ?? rowGo.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0.001f); rowGo.AddComponent<UiTooltipTrigger>().Provider = () => OptionTooltip(def); }
        }

        static TooltipContent OptionTooltip(OptionCatalog.Def d)
        {
            var c = new TooltipContent { Title = d.Label, Subtitle = "Optionen · " + d.Tab };
            c.Add(d.Tooltip, d.Field == "EnemyTelegraphs" ? new Color(1f, 0.7f, 0.45f) : new Color(0.85f, 0.85f, 0.85f));
            return c;
        }

        GameObject ChoiceRow(RectTransform rows, string label, string[] choices, Func<int> get, Action<int> set)
        {
            var row = UiFactory.Rect("Row_" + label, rows); UiFactory.Layout(row, 40);
            Button b = null;
            Action show = () => UiFactory.SetLabel(b, label + ":  " + choices[Mathf.Clamp(get(), 0, choices.Length - 1)]);
            b = UiFactory.Button("Choice", row, "", () => { set((get() + 1) % choices.Length); O.Save(); show(); }, 16);
            ((RectTransform)b.transform).Stretch();
            show(); Window.Opened += show; _refresh.Add(show);
            return row.gameObject;
        }

        GameObject SliderRow(RectTransform rows, string label, Func<float> get, Action<float> set, float min = 0f, float max = 1f, bool whole = false)
        {
            var row = UiFactory.Rect("Row_" + label, rows); UiFactory.Layout(row, 40);
            var l = UiFactory.Text("Label", row, label, 16, TextAlignmentOptions.Left, Color.white); l.rectTransform.Place(new Vector2(0, 0), new Vector2(0.45f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var valueText = UiFactory.Text("Value", row, "", 15, TextAlignmentOptions.Right, UiFactory.AccentColor); valueText.rectTransform.Place(new Vector2(0.9f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            var s = UiFactory.Slider("Slider", row, min, max, get(), whole);
            ((RectTransform)s.transform).Place(new Vector2(0.46f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 18));
            Action show = () => valueText.text = whole ? Mathf.RoundToInt(s.value).ToString() : Mathf.RoundToInt(s.value * (max <= 1.01f ? 100f : 100f / max * max)) + (max <= 1.01f ? " %" : "");
            if (!whole && max > 1.01f) show = () => valueText.text = s.value.ToString("0.00") + "×";
            if (label.Contains("Kameraabstand") || label.Contains("Zoom")) show = () => valueText.text = s.value.ToString("0.00") + " m";
            show();
            int lastStep = Mathf.RoundToInt(s.value * 20f);
            s.onValueChanged.AddListener(v =>
            {
                set(v); O.Save(); show();
                int step = whole ? Mathf.RoundToInt(v) : Mathf.RoundToInt((v - min) / Mathf.Max(0.0001f, max - min) * 20f);   // Tick je Stufe (5 %)
                if (step != lastStep) { lastStep = step; UiFactory.Sound("Slider"); }
            });
            Action sync = () => { s.SetValueWithoutNotify(get()); show(); };
            Window.Opened += sync; _refresh.Add(sync);
            return row.gameObject;
        }

        GameObject ToggleRow(RectTransform rows, string label, Func<bool> get, Action<bool> set)
        {
            var row = UiFactory.Rect("Row_" + label, rows); UiFactory.Layout(row, 40);
            Button b = null;
            b = UiFactory.Button("Toggle", row, "", () => { set(!get()); O.Save(); UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus")); }, 16);
            ((RectTransform)b.transform).Stretch();
            UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus"));
            Action sync = () => UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus"));
            Window.Opened += sync; _refresh.Add(sync);
            return row.gameObject;
        }
    }
}
