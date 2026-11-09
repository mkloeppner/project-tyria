using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Tabs „Reittiere“ (M17) und „Beherrschungen“ (M18).</summary>
    public partial class HeroWindow
    {
        public RectTransform MountsPage { get; private set; }
        public RectTransform MasteryPage { get; private set; }
        RectTransform _mountList;

        partial void BuildWorldTabs()
        {
            MountsPage = AddTab("Reittiere");
            MasteryPage = AddTab("Beherrschungen");
            var title = UiFactory.Text("MountsTitle", MountsPage, "Reittiere · X antippen = auf-/absitzen · X halten = wählen · Leertaste = Fähigkeit · 1 = Absitz-Angriff · V = Ausweichen", 15, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.8f, 0.85f));
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(-20, 24));
            _mountList = UiFactory.Rect("MountList", MountsPage).Stretch(10, 10, 34, 10);
            var h = _mountList.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childForceExpandWidth = true; h.childControlWidth = true; h.childForceExpandHeight = true; h.childControlHeight = true;
            BuildMasteryPage();
        }

        partial void BuildMasteryPage();
        RectTransform _masteryList;
        TextMeshProUGUI _masteryHeader;

        partial void BuildMasteryPage()
        {
            _masteryHeader = UiFactory.Text("MasteryHeader", MasteryPage, "", 16, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.95f, 0.9f));
            _masteryHeader.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(-20, 48));
            _masteryList = UiFactory.Rect("MasteryList", MasteryPage).Stretch(10, 10, 58, 10);
            var h = _masteryList.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childForceExpandWidth = true; h.childControlWidth = true; h.childForceExpandHeight = true; h.childControlHeight = true;
        }

        partial void RefreshMastery()
        {
            UiFactory.Clear(_masteryList);
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            var m = prof.Mastery;
            bool atCap = prof.Progression.Level >= AttributeSet.MaxLevel;
            _masteryHeader.text = "<b>Beherrschungspunkte: " + m.PointsAvailable + " frei</b> (" + m.PointsEarned + " verdient)   ·   Beherrschungs-EP gesamt: " + m.TotalXp.ToString("N0")
                + "\n<size=14><color=#a8c8b8>" + (atCap ? "EP fließen in die gewählte Linie." : "Beherrschungs-EP gibt es ab Stufe " + AttributeSet.MaxLevel + " (jetzt " + prof.Progression.Level + ").") + " Punkte: Einblicke 1 · Zonenabschluss 2 · Weltboss/Meta 1 · Erfolge 1</color></size>";
            foreach (var def in prof.MasteryLines)
            {
                if (def == null) continue;
                var l = def.Line;
                bool sel = m.Selected == l;
                var card = UiFactory.Image("Line_" + l, _masteryList, sel ? new Color(0.13f, 0.17f, 0.14f) : new Color(0.1f, 0.1f, 0.11f), UiFactory.Sprite("ui_panel"));
                var icon = UiFactory.Image("Icon", card.transform, def.Color, UiFactory.Sprite(def.Icon));
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(72, 72));
                var sb = new System.Text.StringBuilder();
                sb.Append("<size=20><b>").Append(def.DisplayName).Append("</b></size>  ").Append(m.Rank(l)).Append("/").Append(def.Ranks.Length).Append(sel ? "  <color=#7fffb0>(trainiert)</color>" : "").Append("\n");
                sb.Append("<size=13><color=#bbbbbb>").Append(def.Description).Append("</color></size>\n\n");
                for (int i = 0; i < def.Ranks.Length; i++)
                {
                    var r = def.Ranks[i];
                    bool done = m.Rank(l) > i, current = m.Rank(l) == i;
                    sb.Append(done ? "<color=#7fffb0>+ " : current ? "<color=#ffffff>> " : "<color=#888888>· ").Append(r.Name).Append("</color>\n<size=13>  ").Append(r.Effect);
                    if (!done) sb.Append("  <color=#c0c0a0>(").Append(r.Xp.ToString("N0")).Append(" EP · ").Append(r.Points).Append(" Pkt.)</color>");
                    sb.Append("</size>\n");
                }
                if (!m.IsMaxed(l)) sb.Append("\nFortschritt: ").Append(Mathf.RoundToInt(m.Progress(l) * 100f)).Append(" %");
                var txt = UiFactory.Text("Info", card.transform, sb.ToString(), 15, TextAlignmentOptions.TopLeft);
                txt.rectTransform.Stretch(10, 10, 90, 54);
                if (!m.IsMaxed(l) && !sel)
                {
                    var b = UiFactory.Button("Train", card.transform, "Trainieren", () => { prof.SelectMasteryLine(l); _dirty = true; }, 16);
                    ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(160, 34));
                }
            }
        }

        partial void RefreshMounts()
        {
            UiFactory.Clear(_mountList);
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            foreach (var m in prof.MountDefinitions)
            {
                if (m == null) continue;
                bool owned = prof.OwnsMount(m.Kind);
                bool selected = prof.State.SelectedMount == m.Kind;
                var card = UiFactory.Image("Card_" + m.Kind, _mountList, owned ? new Color(0.14f, 0.13f, 0.1f) : new Color(0.09f, 0.09f, 0.1f), UiFactory.Sprite("ui_panel"));
                var icon = UiFactory.Image("Icon", card.transform, owned ? Color.white : new Color(0.4f, 0.4f, 0.45f), UiFactory.Sprite(m.Icon));
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(96, 96));
                var sb = new System.Text.StringBuilder();
                sb.Append("<size=22><b>").Append(m.DisplayName).Append("</b></size>").Append(selected ? "  <color=#ffd27a>(gewählt)</color>" : "").Append("\n");
                sb.Append("<color=#c8c8c8>").Append(m.Description).Append("</color>\n\n");
                sb.Append("<color=#9fd0ff>Leertaste:</color> ").Append(m.AbilityText).Append("\n");
                sb.Append("<color=#9fd0ff>1:</color> ").Append(m.EngageText).Append("\n");
                sb.Append("Tempo ").Append(m.Stats.Speed.ToString("0.0")).Append(" m/s").Append(m.Kind == MountKind.Skimmer ? " (Wasser " + m.Stats.WaterSpeed.ToString("0.0") + ")" : "");
                sb.Append(" · Reittier-HP ").Append(Mathf.RoundToInt(m.Stats.MaxHealth * (MountController.MountHealthFactor != null ? MountController.MountHealthFactor() : 1f)).ToString("N0")).Append("\n\n");
                if (owned) sb.Append("<color=#8f8>Freigeschaltet</color>");
                else
                {
                    var v = prof.CanBuyMount(m);
                    sb.Append("<color=#ffb070>Erwerb: ").Append(WalletModel.Format(m.PriceCopper)).Append(" beim Stallmeister (").Append(ZoneLabel(m.VendorZone)).Append(")</color>\n");
                    sb.Append(v == MountShop.Verdict.HeartMissing ? "<color=#f88>Voraussetzung: Herz „" + HeartName(m.RequiredHeart) + "“ erfüllen</color>" : v == MountShop.Verdict.NotEnoughGold ? "<color=#f88>Zu wenig Gold</color>" : "<color=#8f8>Bereit zum Kauf</color>");
                }
                var txt = UiFactory.Text("Info", card.transform, sb.ToString(), 15, TextAlignmentOptions.TopLeft);
                txt.rectTransform.Stretch(12, 12, 120, 54);
                if (owned && !selected)
                {
                    var k = m.Kind;
                    var b = UiFactory.Button("Select", card.transform, "Auswählen", () => { prof.SelectMount(k); _dirty = true; }, 16);
                    ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(160, 34));
                }
            }
        }

        static string ZoneLabel(string id)
        {
            var gm = GameManager.Instance;
            var z = gm != null && gm.World != null ? gm.World.Zone(id) : null;
            return z != null ? z.DisplayName : id;
        }

        static string HeartName(string id)
        {
            foreach (var o in ZoneObject.Objects) if (o != null && o.Definition != null && o.Definition.Id == id) return o.Definition.DisplayName;
            return id;
        }
    }
}
