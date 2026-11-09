using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Debug-Panel (F9, nur Editor/Development Build).</summary>
    public class DebugPanel
    {
        public readonly UiWindow Window;
        readonly TMP_InputField _arg;
        readonly TextMeshProUGUI _status;
        readonly RectTransform _grid;

        public DebugPanel(WindowManager mgr)
        {
            Window = new UiWindow(mgr, "debug", "Debug-Panel (F9)", new Vector2(720, 560), new Vector2(560, 120));
            var c = Window.Content;
            var lbl = UiFactory.Text("ArgLabel", c, "Parameter (Level, Menge, Item-Name, Marker …):", 15, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f));
            lbl.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 22));
            _arg = UiFactory.InputField("Arg", c, "z. B. 10");
            ((RectTransform)_arg.transform).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -24), new Vector2(0, 32));
            _grid = UiFactory.Rect("Grid", c).Stretch(0, 0, 66, 34);
            var g = _grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(170, 34); g.spacing = new Vector2(6, 6);
            _status = UiFactory.Text("Status", c, "", 15, TextAlignmentOptions.Left, UiFactory.AccentColor);
            _status.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), Vector2.zero, new Vector2(0, 28));

            Add("Level setzen", a => DebugCommands.SetLevel(Int(a, 10)));
            Add("+1.000 EP", a => DebugCommands.GiveXp(Int(a, 1000)));
            Add("+10 Heldenpunkte", a => DebugCommands.GiveHeroPoints(Int(a, 10)));
            Add("+1 Gold", a => DebugCommands.GiveGold(Int(a, 1)));
            Add("+1.000 Karma", a => DebugCommands.GiveKarma(Int(a, 1000)));
            Add("Gott-Modus", a => DebugCommands.ToggleGodMode());
            Add("Macht ×10", a => DebugCommands.ApplyEffect(EffectType.Might, Int(a, 10), 20f));
            Add("Segen-Mix", a => { DebugCommands.ApplyEffect(EffectType.Fury, 1, 15f); DebugCommands.ApplyEffect(EffectType.Swiftness, 1, 15f); DebugCommands.ApplyEffect(EffectType.Protection, 1, 15f); return DebugCommands.ApplyEffect(EffectType.Aegis, 1, 15f); });
            Add("Zustands-Mix", a => { DebugCommands.ApplyEffect(EffectType.Bleeding, 5, 8f); DebugCommands.ApplyEffect(EffectType.Cripple, 1, 5f); return DebugCommands.ApplyEffect(EffectType.Vulnerability, 10, 10f); });
            Add("Speichern", a => DebugCommands.Save());
            Add("Laden", a => DebugCommands.Load());
            Add("Neues Spiel", a => DebugCommands.NewGame());
            Add("Speicher löschen", a => DebugCommands.DeleteSave());
            foreach (var e in DebugCommands.Extra) { var run = e.run; Add(e.label, run); }
        }

        static int Int(string s, int def) => int.TryParse(s, out int v) ? v : def;

        void Add(string label, System.Func<string, string> run)
        {
            UiFactory.Button(label, _grid, label, () => { _status.text = run(_arg.text); }, 15);
        }
    }
}
