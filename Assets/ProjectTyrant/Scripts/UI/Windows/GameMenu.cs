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

            var audio = Tab(bar, "Audio");
            SliderRow(audio, "Gesamt", () => O.Master, v => O.Master = v);
            SliderRow(audio, "Musik", () => O.Music, v => O.Music = v);
            SliderRow(audio, "Umgebung", () => O.Ambience, v => O.Ambience = v);
            SliderRow(audio, "Effekte", () => O.Effects, v => O.Effects = v);
            SliderRow(audio, "Benutzeroberfläche", () => O.Ui, v => O.Ui = v);

            var cam = Tab(bar, "Kamera");
            SliderRow(cam, "Abstand", () => O.CameraDistance, v => O.CameraDistance = v, 0.6f, 1.6f);
            SliderRow(cam, "Empfindlichkeit", () => O.CameraSensitivity, v => O.CameraSensitivity = v, 0.3f, 2.5f);
            ToggleRow(cam, "Kamera-Impuls bei Treffern", () => O.CameraImpulse, v => O.CameraImpulse = v);
            ToggleRow(cam, "Action-Kamera-Fadenkreuz", () => O.ActionCrosshair, v => O.ActionCrosshair = v);

            var gfx = Tab(bar, "Grafik");
            SliderRow(gfx, "Qualitätsstufe", () => O.Quality < 0 ? QualitySettings.GetQualityLevel() : O.Quality, v => O.Quality = Mathf.RoundToInt(v), 0, Mathf.Max(1, QualitySettings.names.Length - 1), true);
            SliderRow(gfx, "Sichtweite", () => O.ViewDistance, v => O.ViewDistance = v, 0.5f, 1.5f);
            SliderRow(gfx, "Effekt-Dichte (Voll · Reduziert · Minimal)", () => O.EffectDensity, v => O.EffectDensity = Mathf.RoundToInt(v), 0, 2, true);
            ToggleRow(gfx, "Schatten", () => O.Shadows, v => O.Shadows = v);

            var game = Tab(bar, "Spiel");
            ToggleRow(game, "Auto-Beute", () => O.AutoLoot, v => O.AutoLoot = v);
            SliderRow(game, "Kampftext-Dichte", () => O.CombatTextDensity, v => O.CombatTextDensity = Mathf.RoundToInt(v), 0, 2, true);
            ToggleRow(game, "Hit-Stop", () => O.HitStop, v => O.HitStop = v);
            ToggleRow(game, "Telegraphen: Farbenblind-Modus", () => O.ColorblindTelegraphs, v => O.ColorblindTelegraphs = v);
            SliderRow(game, "UI-Skalierung", () => O.UiScale, v => O.UiScale = v, 0.8f, 1.3f);
            ToggleRow(game, "Minimap dreht mit", () => O.MinimapRotate, v => O.MinimapRotate = v);

            var keys = Tab(bar, "Steuerung");
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

        void SliderRow(RectTransform rows, string label, Func<float> get, Action<float> set, float min = 0f, float max = 1f, bool whole = false)
        {
            var row = UiFactory.Rect("Row_" + label, rows); UiFactory.Layout(row, 40);
            var l = UiFactory.Text("Label", row, label, 16, TextAlignmentOptions.Left, Color.white); l.rectTransform.Place(new Vector2(0, 0), new Vector2(0.45f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var valueText = UiFactory.Text("Value", row, "", 15, TextAlignmentOptions.Right, UiFactory.AccentColor); valueText.rectTransform.Place(new Vector2(0.9f, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            var s = UiFactory.Slider("Slider", row, min, max, get(), whole);
            ((RectTransform)s.transform).Place(new Vector2(0.46f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 18));
            Action show = () => valueText.text = whole ? Mathf.RoundToInt(s.value).ToString() : Mathf.RoundToInt(s.value * (max <= 1.01f ? 100f : 100f / max * max)) + (max <= 1.01f ? " %" : "");
            if (!whole && max > 1.01f) show = () => valueText.text = s.value.ToString("0.00") + "×";
            show();
            int lastStep = Mathf.RoundToInt(s.value * 20f);
            s.onValueChanged.AddListener(v =>
            {
                set(v); O.Save(); show();
                int step = whole ? Mathf.RoundToInt(v) : Mathf.RoundToInt((v - min) / Mathf.Max(0.0001f, max - min) * 20f);   // Tick je Stufe (5 %)
                if (step != lastStep) { lastStep = step; UiFactory.Sound("Slider"); }
            });
            Window.Opened += () => { s.SetValueWithoutNotify(get()); show(); };
        }

        void ToggleRow(RectTransform rows, string label, Func<bool> get, Action<bool> set)
        {
            var row = UiFactory.Rect("Row_" + label, rows); UiFactory.Layout(row, 40);
            Button b = null;
            b = UiFactory.Button("Toggle", row, "", () => { set(!get()); O.Save(); UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus")); }, 16);
            ((RectTransform)b.transform).Stretch();
            UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus"));
            Window.Opened += () => UiFactory.SetLabel(b, label + ":  " + (get() ? "an" : "aus"));
        }
    }
}
