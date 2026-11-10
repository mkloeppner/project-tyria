using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Debug-Panel (F9, nur Editor/Development Build). Befehle nach Präfix gruppiert („Admin:“, „Story:“, „Welt:“ …) in einem scrollbaren Bereich;
    /// Ergebnisse erscheinen als Toast (<see cref="UiToastStack"/>), nicht mehr als Text im Fenster.
    /// </summary>
    public class DebugPanel
    {
        public const string GeneralGroup = "Allgemein";
        public readonly UiWindow Window;
        readonly TMP_InputField _arg;
        readonly RectTransform _list;
        readonly Dictionary<string, RectTransform> _groups = new Dictionary<string, RectTransform>();

        public DebugPanel(WindowManager mgr)
        {
            Window = new UiWindow(mgr, "debug", "Debug-Panel (F9)", new Vector2(800, 640), new Vector2(520, 60));
            var c = Window.Content;
            var lbl = UiFactory.Text("ArgLabel", c, "Parameter (Level, Menge, Item-Name, Marker …):", 15, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f));
            lbl.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 22));
            _arg = UiFactory.InputField("Arg", c, "z. B. 10");
            ((RectTransform)_arg.transform).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -24), new Vector2(0, 32));
            UiScroll.List("Commands", c, out _list, 6, 4).Stretch(0, 0, 64, 0);

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

        /// <summary>Gruppe aus dem Präfix vor dem ersten Doppelpunkt („Admin: Story weiter“ → „Admin“), sonst „Allgemein“.</summary>
        public static string GroupOf(string label, out string shortLabel)
        {
            shortLabel = label ?? "";
            int i = shortLabel.IndexOf(':');
            if (i <= 0 || i > 14 || shortLabel.Substring(0, i).IndexOf(' ') >= 0) return GeneralGroup;
            string g = shortLabel.Substring(0, i).Trim();
            shortLabel = shortLabel.Substring(i + 1).Trim();
            return g;
        }

        RectTransform Group(string name)
        {
            if (_groups.TryGetValue(name, out var grid)) return grid;
            var head = UiScroll.ListText("Head_" + name, _list, "<b>" + name + "</b>", 16, UiFactory.AccentColor);
            UiFactory.Layout(head, 24);
            grid = UiFactory.Rect("Group_" + name, _list);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(244, 40); g.spacing = new Vector2(6, 6);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
            _groups[name] = grid;
            return grid;
        }

        void Add(string label, System.Func<string, string> run)
        {
            string group = GroupOf(label, out string shortLabel);
            var b = UiFactory.Button(label, Group(group), shortLabel, () => Run(label, run), 14);
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) { t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 14; t.overflowMode = TextOverflowModes.Ellipsis; }
        }

        void Run(string label, System.Func<string, string> run)
        {
            string result;
            try { result = run(_arg.text); }
            catch (System.Exception ex) { Debug.LogException(ex); GameEventBus.Notify(label + ": Fehler – " + ex.Message, new Color(1f, 0.45f, 0.4f)); return; }
            GameEventBus.Notify(string.IsNullOrEmpty(result) ? label + ": erledigt" : result, UiFactory.AccentColor);
        }
    }
}
