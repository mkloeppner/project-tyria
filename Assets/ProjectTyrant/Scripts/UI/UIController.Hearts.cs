using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Herz-Pop-up beim Betreten des Gebiets („Hilf Mira, Sonnfurt zu sichern“ mit Aufgaben-Icons, GW2).</summary>
    public partial class UIController
    {
        RectTransform _heartPop; TextMeshProUGUI _heartPopText; float _heartPopTimer;

        public void ShowHeartPopup(Heart h)
        {
            if (h == null || h.Definition == null) return;
            if (_heartPop == null)
            {
                var bg = UiFactory.Image("HeartPopup", HudLayer, new Color(0.1f, 0.07f, 0.03f, 0.88f), UiFactory.Sprite("ui_round"));
                _heartPop = bg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(620, 92));
                _heartPopText = UiFactory.Text("Text", _heartPop, "", 18, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.45f)); _heartPopText.rectTransform.Stretch(12, 12, 6, 6);
            }
            string ways = h.TaskSummary;
            if (string.IsNullOrEmpty(ways) && h.Ways != null && h.Ways.Length > 0) { var parts = new System.Collections.Generic.List<string>(); foreach (var w in h.Ways) parts.Add(WayLabel(w)); ways = string.Join("  ·  ", parts.ToArray()); }
            _heartPopText.text = "♥ <b>" + h.Definition.DisplayName + "</b>\n<size=15><color=#E8E0C8>" + (string.IsNullOrEmpty(ways) ? h.Definition.HeartTask : ways) + "</color></size>";
            _heartPop.gameObject.SetActive(true); _heartPopTimer = 6f;
        }

        public static string WayLabel(HeartWay w)
        {
            switch (w)
            {
                case HeartWay.Kill: return "× Gegner besiegen"; case HeartWay.Collect: return "○ Einsammeln"; case HeartWay.Use: return "■ Reparieren";
                case HeartWay.Animals: return "♣ Tiere versorgen"; case HeartWay.Revive: return "+ Verletzte versorgen"; case HeartWay.Bundle: return "□ Mit Bündel arbeiten";
                case HeartWay.Escort: return "→ Begleiten"; case HeartWay.Disarm: return "◊ Fallen entschärfen"; case HeartWay.Combo: return "▲ Kombos auslösen"; case HeartWay.Emote: return "☺ Emote zeigen";
            }
            return w.ToString();
        }

        void UpdateHeartPopup() { if (_heartPop != null && _heartPop.gameObject.activeSelf && (_heartPopTimer -= Time.deltaTime) <= 0f) _heartPop.gameObject.SetActive(false); }
    }
}
