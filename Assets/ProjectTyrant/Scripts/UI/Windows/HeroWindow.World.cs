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
        RectTransform _mountList, _mountPrefs;
        /// <summary>Lauf 10: frei belegbare Direkttasten (der Reihe nach durchschalten).</summary>
        public static readonly string[] MountKeyChoices = { "", "F6", "F7", "F8", "N", "J", "K", "L", "U", "O", "P" };

        partial void BuildWorldTabs()
        {
            MountsPage = AddTab("Reittiere");
            MasteryPage = AddTab("Beherrschungen");
            var title = UiFactory.Text("MountsTitle", MountsPage, "Reittiere · X antippen = auf-/absitzen · X halten = Radialmenü · Leertaste = Fähigkeit/Flügelschlag · V = Ausweichen, in der Luft: Sturzflug · 1 = Absitz-Angriff", 15, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.8f, 0.85f));
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(-20, 24));
            _mountPrefs = UiFactory.Rect("MountPrefs", MountsPage);
            _mountPrefs.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -32), new Vector2(-20, 32));
            _mountList = UiFactory.Rect("MountList", MountsPage).Stretch(10, 10, 72, 10);
            var grid = _mountList.gameObject.AddComponent<GridLayoutGroup>(); grid.spacing = new Vector2(10, 10); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;   // Lauf 10: 3 × 2 Karten
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
            var sel = prof.State.MountSelection();
            // Lauf 10: Einstellungen (Account-Ebene)
            UiFactory.Clear(_mountPrefs);
            var hl = _mountPrefs.GetComponent<HorizontalLayoutGroup>(); if (hl == null) hl = _mountPrefs.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childForceExpandWidth = false; hl.childControlWidth = false; hl.childControlHeight = true; hl.childForceExpandHeight = true;
            var b1 = UiFactory.Button("ModeTap", _mountPrefs, "X antippen: " + (sel.UseLastInsteadOfDefault ? "zuletzt genutztes" : "Standard-Reittier"), () => { var s2 = prof.State.MountSelection(); s2.UseLastInsteadOfDefault = !s2.UseLastInsteadOfDefault; prof.State.StoreMountSelection(s2); prof.RequestSave("Reittier-Einstellung"); _dirty = true; }, 15);
            ((RectTransform)b1.transform).sizeDelta = new Vector2(300, 30);
            var b2 = UiFactory.Button("ModeRadial", _mountPrefs, "Radialmenü setzt Standard: " + (sel.RadialSetsDefault ? "ja" : "nein"), () => { var s2 = prof.State.MountSelection(); s2.RadialSetsDefault = !s2.RadialSetsDefault; prof.State.StoreMountSelection(s2); prof.RequestSave("Reittier-Einstellung"); _dirty = true; }, 15);
            ((RectTransform)b2.transform).sizeDelta = new Vector2(320, 30);
            var cellW = Mathf.Max(200f, (_mountList.rect.width - 20f) / 3f); var cellH = Mathf.Max(220f, (_mountList.rect.height - 10f) / 2f);
            _mountList.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellW, cellH);
            foreach (var m in prof.MountDefinitions)
            {
                if (m == null) continue;
                bool owned = prof.OwnsMount(m.Kind);
                bool selected = prof.State.SelectedMount == m.Kind;
                var card = UiFactory.Image("Card_" + m.Kind, _mountList, owned ? new Color(0.14f, 0.13f, 0.1f) : new Color(0.09f, 0.09f, 0.1f), UiFactory.Sprite("ui_panel"));
                var icon = UiFactory.Image("Icon", card.transform, owned ? Color.white : new Color(0.4f, 0.4f, 0.45f), UiFactory.Sprite(m.Icon));
                icon.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(64, 64));
                var sb = new System.Text.StringBuilder();
                sb.Append("<size=20><b>").Append(m.DisplayName).Append("</b></size>").Append(selected ? "  <color=#ffd27a>(Standard)</color>" : "").Append(sel.LastUsed == m.Kind ? "  <color=#9fd0ff>(zuletzt)</color>" : "").Append(prof.State.IsAdminMarked("mount:" + m.Kind) ? "  <color=#ff7f7f>[Admin]</color>" : "").Append("\n");
                sb.Append("<color=#c8c8c8>").Append(m.Description).Append("</color>\n\n");
                sb.Append("<color=#9fd0ff>Leertaste:</color> ").Append(m.AbilityText).Append("\n");
                sb.Append("<color=#9fd0ff>1:</color> ").Append(m.EngageText).Append("\n");
                sb.Append("Tempo ").Append(m.Stats.Speed.ToString("0.0")).Append(" m/s").Append(m.Kind == MountKind.Skimmer ? " (Wasser " + m.Stats.WaterSpeed.ToString("0.0") + ")" : "");
                sb.Append(" · Reittier-HP ").Append(Mathf.RoundToInt(m.Stats.MaxHealth * (MountController.MountHealthFactor != null ? MountController.MountHealthFactor() : 1f)).ToString("N0")).Append("\n\n");
                if (owned) sb.Append("<color=#8f8>Freigeschaltet</color>");
                else if (m.PriceCopper <= 0)   // Lauf 10: Greif/Himmelsschuppe nicht käuflich
                    sb.Append("<color=#ffb070>Freischaltung: ").Append(m.Kind == MountKind.Skyscale ? "Sammlung „Himmelsschuppe“ in den Himmelsspitzen" : "Persönliche Geschichte, Kapitel 9").Append("</color>");
                else
                {
                    var v = prof.CanBuyMount(m);
                    sb.Append("<color=#ffb070>Erwerb: ").Append(WalletModel.Format(m.PriceCopper)).Append(" beim Stallmeister (").Append(ZoneLabel(m.VendorZone)).Append(")</color>\n");
                    sb.Append(v == MountShop.Verdict.HeartMissing ? "<color=#f88>Voraussetzung: Herz „" + HeartName(m.RequiredHeart) + "“ erfüllen</color>" : v == MountShop.Verdict.NotEnoughGold ? "<color=#f88>Zu wenig Gold</color>" : "<color=#8f8>Bereit zum Kauf</color>");
                }
                var txt = UiFactory.Text("Info", card.transform, sb.ToString(), 15, TextAlignmentOptions.TopLeft);
                txt.fontSize = 13;
                txt.rectTransform.Stretch(84, 10, 8, 46);
                if (owned)
                {
                    var k = m.Kind;
                    if (!selected)
                    {
                        var b = UiFactory.Button("Select", card.transform, "Als Standard", () => { prof.SelectMount(k); _dirty = true; }, 15);
                        ((RectTransform)b.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 8), new Vector2(150, 30));
                    }
                    string key = ""; foreach (var kb in sel.KeyBindings) if (kb.StartsWith(k + "=")) key = kb.Substring(k.ToString().Length + 1);
                    var kbtn = UiFactory.Button("Key", card.transform, "Taste: " + (key == "" ? "–" : key), () => CycleMountKey(k), 15);
                    ((RectTransform)kbtn.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 8), new Vector2(130, 30));
                }
            }
        }

        /// <summary>Lauf 10: nächste freie Direkttaste für ein Reittier (belegte/reservierte werden übersprungen).</summary>
        void CycleMountKey(MountKind k)
        {
            var prof = PlayerProfile.Instance; if (prof == null) return;
            var sel = prof.State.MountSelection();
            string cur = ""; foreach (var kb in sel.KeyBindings) if (kb.StartsWith(k + "=")) cur = kb.Substring(k.ToString().Length + 1);
            int i = System.Array.IndexOf(MountKeyChoices, cur);
            for (int n = 1; n <= MountKeyChoices.Length; n++)
            {
                string next = MountKeyChoices[(i + n + MountKeyChoices.Length) % MountKeyChoices.Length];
                if (sel.Bind(k, next == "" ? null : next, out _)) break;
            }
            prof.State.StoreMountSelection(sel); prof.RequestSave("Reittier-Taste"); _dirty = true;
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
