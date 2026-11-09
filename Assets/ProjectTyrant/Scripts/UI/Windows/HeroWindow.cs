using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Heldenfenster (H) mit Tabs wie GW2: Ausrüstung · Fertigkeiten · Eigenschaften · Fortschritt.</summary>
    public partial class HeroWindow
    {
        public readonly UiWindow Window;
        readonly UIController _ui;
        readonly RectTransform _tabBar, _body;
        readonly List<(Button button, RectTransform page)> _tabs = new List<(Button, RectTransform)>();
        readonly TextMeshProUGUI _heroPoints;
        int _active;
        bool _dirty = true;

        public RectTransform EquipmentPage { get; private set; }
        public RectTransform SkillsPage { get; private set; }
        public RectTransform TraitsPage { get; private set; }
        public RectTransform ProgressPage { get; private set; }

        TextMeshProUGUI _progressText, _attrText;

        public HeroWindow(WindowManager mgr, UIController ui)
        {
            _ui = ui;
            Window = new UiWindow(mgr, "hero", "Held", new Vector2(1060, 700), new Vector2(-180, 40));
            _heroPoints = UiFactory.Text("HeroPoints", Window.Header, "", 17, TextAlignmentOptions.Right, new Color(0.6f, 0.9f, 1f));
            _heroPoints.rectTransform.Stretch(300, 44, 0, 0);
            _tabBar = UiFactory.Rect("Tabs", Window.Content).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 36));
            var h = _tabBar.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 6; h.childForceExpandWidth = false; h.childControlWidth = false;
            _body = UiFactory.Rect("Body", Window.Content).Stretch(0, 0, 44, 0);
            EquipmentPage = AddTab("Ausrüstung");
            SkillsPage = AddTab("Fertigkeiten");
            TraitsPage = AddTab("Eigenschaften");
            ProgressPage = AddTab("Fortschritt");
            BuildWorldTabs();
            BuildProgress();
            BuildEquipmentPage();
            BuildSkillsPage();
            BuildTraitsPage();
            Select(3);
            Window.Opened += () => _dirty = true;
            GameEventBus.ProfileChanged += MarkDirty;
            GameEventBus.ZoneProgress += _ => MarkDirty();
        }

        public void MarkDirty() => _dirty = true;

        RectTransform AddTab(string label)
        {
            int idx = _tabs.Count;
            var b = UiFactory.Button("Tab_" + label, _tabBar, label, () => Select(idx), 17);
            ((RectTransform)b.transform).sizeDelta = new Vector2(158, 34);
            b.GetComponent<UiButtonFeedback>().ClickSound = "Tab";   // Lauf 5 / M24
            var page = UiFactory.Rect("Page_" + label, _body).Stretch();
            page.gameObject.SetActive(false);
            _tabs.Add((b, page));
            return page;
        }

        public void Select(int i)
        {
            _active = i;
            for (int k = 0; k < _tabs.Count; k++)
            {
                _tabs[k].page.gameObject.SetActive(k == i);
                _tabs[k].button.GetComponent<Image>().color = k == i ? new Color(0.45f, 0.35f, 0.2f) : UiFactory.ButtonColor;
            }
            _dirty = true;
        }

        void BuildProgress()
        {
            _progressText = UiFactory.Text("Progress", ProgressPage, "", 18, TextAlignmentOptions.TopLeft);
            _progressText.rectTransform.Place(new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(-20, -20));
            _progressText.rectTransform.offsetMin = new Vector2(10, 10); _progressText.rectTransform.offsetMax = new Vector2(-10, -10);
            _attrText = UiFactory.Text("Attributes", ProgressPage, "", 18, TextAlignmentOptions.TopLeft);
            _attrText.rectTransform.anchorMin = new Vector2(0.5f, 0); _attrText.rectTransform.anchorMax = new Vector2(1, 1);
            _attrText.rectTransform.offsetMin = new Vector2(10, 10); _attrText.rectTransform.offsetMax = new Vector2(-10, -10);
        }

        public static string AttributeSummary(Combatant p)
        {
            if (p == null) return "";
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>Attribute</b>\n");
            for (int i = 0; i < AttributeAccumulator.Count; i++)
            {
                var a = (Attr)i;
                sb.Append(AttributeSet.AttrName(a)).Append(": <color=#ffe08a>").Append(Mathf.RoundToInt(p.Stats.Get(a)).ToString("N0")).Append("</color>\n");
            }
            sb.Append("\nRüstung: <color=#ffe08a>").Append(Mathf.RoundToInt(p.Stats.Armor).ToString("N0")).Append("</color>");
            sb.Append("\nLebenspunkte: <color=#ffe08a>").Append(Mathf.RoundToInt(p.MaxHealth).ToString("N0")).Append("</color>");
            sb.Append("\nKrit-Chance: <color=#ffe08a>").Append((p.Stats.CritChance * 100f).ToString("0.0")).Append(" %</color>");
            sb.Append("\nKrit-Schaden: <color=#ffe08a>").Append((p.Stats.CritDamage * 100f).ToString("0")).Append(" %</color>");
            sb.Append("\nZustandsdauer: <color=#ffe08a>+").Append((p.Stats.ConditionDurationBonus * 100f).ToString("0")).Append(" %</color>");
            sb.Append("\nSegensdauer: <color=#ffe08a>+").Append((p.Stats.BoonDurationBonus * 100f).ToString("0")).Append(" %</color>");
            return sb.ToString();
        }

        partial void BuildWorldTabs();
        partial void RefreshMounts();
        partial void RefreshMastery();
        partial void RefreshEquipment();
        partial void RefreshSkills();
        partial void RefreshTraits();
        partial void BuildEquipmentPage();
        partial void BuildSkillsPage();
        partial void BuildTraitsPage();
        partial void ProgressExtra(System.Text.StringBuilder sb);

        bool _lastLock;

        public void Refresh()
        {
            var lockProf = PlayerProfile.Instance;
            bool lockNow = lockProf != null && lockProf.BuildLocked;
            if (lockNow != _lastLock) { _lastLock = lockNow; _dirty = true; }
            if (!_dirty) return;
            _dirty = false;
            var prof = PlayerProfile.Instance;
            var p = _ui.Player;
            if (prof != null) _heroPoints.text = "Heldenpunkte: " + prof.Progression.AvailableHeroPoints + " / " + prof.Progression.HeroPoints;
            switch (_active)
            {
                case 0: RefreshEquipment(); break;
                case 1: RefreshSkills(); break;
                case 2: RefreshTraits(); break;
                case 4: RefreshMounts(); break;
                case 5: RefreshMastery(); break;
                case 3:
                    if (prof != null)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("<b>Stufe ").Append(prof.Progression.Level).Append("</b>\n");
                        sb.Append("Erfahrung: ").Append(prof.Progression.Xp.ToString("N0")).Append(" / ").Append(ProgressionModel.XpToNext(prof.Progression.Level).ToString("N0")).Append("\n");
                        sb.Append("Heldenpunkte: ").Append(prof.Progression.AvailableHeroPoints).Append(" verfügbar (").Append(prof.Progression.HeroPoints).Append(" gesamt)\n\n");
                        sb.Append("Geldbörse: ").Append(WalletModel.Format(prof.Wallet.Copper)).Append("\nKarma: ").Append(prof.Wallet.Karma.ToString("N0")).Append("\n\n");
                        ProgressExtra(sb);
                        sb.Append("\n<size=14><color=#a0a0a0>Letzte Speicherung: ").Append(prof.LastSaveInfo).Append("</color></size>");
                        _progressText.text = sb.ToString();
                    }
                    _attrText.text = AttributeSummary(p);
                    break;
            }
        }
    }
}
