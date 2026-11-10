using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Story-Tracker oben (grün, mit Distanz), Briefkasten oben links, Lern-Hinweistafel, Belohnungsfenster der Story-Instanz.</summary>
    public partial class UIController
    {
        TextMeshProUGUI _storyTracker, _mailBadge, _hintTitle, _hintText;
        RectTransform _storyTrackerRoot, _mailIcon, _hintPanel;
        readonly Queue<HintDef> _hints = new Queue<HintDef>();
        public MailWindow Mail { get; private set; }
        UiWindow _storyReward; TextMeshProUGUI _storyRewardText; Action _storyRewardDone;

        StoryDirector StoryDir => GameManager.Instance != null ? GameManager.Instance.Story : null;

        void BuildStoryUi()
        {
            var bg = UiFactory.Image("StoryTracker", HudLayer, new Color(0.03f, 0.08f, 0.04f, 0.72f), UiFactory.Sprite("ui_round"), true);
            _storyTrackerRoot = bg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(760, 34));
            _storyTracker = UiFactory.Text("Text", _storyTrackerRoot, "", 18, TextAlignmentOptions.Center, StoryDirector.StoryGreen); _storyTracker.rectTransform.Stretch(12, 12, 2, 2);
            _storyTracker.enableAutoSizing = true; _storyTracker.fontSizeMin = 12; _storyTracker.fontSizeMax = 18;
            bg.gameObject.AddComponent<Button>().onClick.AddListener(() => { Hero.Window.Open(); Hero.SelectStory(); });
            var mi = UiFactory.Button("MailIcon", HudLayer, "Post", () => Mail.Window.Toggle(), 15);
            _mailIcon = ((RectTransform)mi.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(54, 44));
            _mailBadge = UiFactory.Text("Badge", _mailIcon, "", 14, TextAlignmentOptions.TopRight, new Color(1f, 0.85f, 0.35f)); _mailBadge.rectTransform.Stretch(0, 2, 0, 0); _mailBadge.fontStyle = FontStyles.Bold;
            var hp = UiFactory.Image("Hint", HudLayer, new Color(0.06f, 0.07f, 0.1f, 0.94f), UiFactory.Sprite("ui_panel"), true);
            _hintPanel = hp.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -70), new Vector2(380, 150));
            _hintTitle = UiFactory.Text("Title", _hintPanel, "", 19, TextAlignmentOptions.TopLeft, UiFactory.AccentColor); _hintTitle.rectTransform.Stretch(14, 14, 10, 110); _hintTitle.fontStyle = FontStyles.Bold;
            _hintText = UiFactory.Text("Text", _hintPanel, "", 15, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.88f, 0.9f)); _hintText.rectTransform.Stretch(14, 14, 38, 46);
            var ok = UiFactory.Button("Ok", _hintPanel, "Verstanden", NextHint, 15); ((RectTransform)ok.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 8), new Vector2(130, 32));
            var off = UiFactory.Button("Off", _hintPanel, "Hinweise aus", () => { if (StoryDir != null && StoryDir.State != null) StoryDir.State.HintsEnabled = false; _hints.Clear(); NextHint(); }, 13); ((RectTransform)off.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 8), new Vector2(120, 32));
            _hintPanel.gameObject.SetActive(false);
            Mail = new MailWindow(Windows, this);
            _storyReward = new UiWindow(Windows, "storyreward", "Geschichte", new Vector2(460, 300));
            _storyRewardText = UiFactory.Text("Text", _storyReward.Content, "", 18, TextAlignmentOptions.Top, Color.white); _storyRewardText.rectTransform.Stretch(16, 16, 12, 64);
            var leave = UiFactory.Button("Leave", _storyReward.Content, "Instanz verlassen", () => { _storyReward.Close(); var d = _storyRewardDone; _storyRewardDone = null; d?.Invoke(); }, 18);
            ((RectTransform)leave.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(240, 44));
            _storyReward.Closed += () => { var d = _storyRewardDone; _storyRewardDone = null; d?.Invoke(); };
            BuildDialogueUi();
        }

        void UpdateStoryUi()
        {
            UpdateDialogueUi();
            UpdateHeartPopup();
            var sd = StoryDir;
            string tr = sd != null ? sd.TrackerText() : null;
            _storyTrackerRoot.gameObject.SetActive(!string.IsNullOrEmpty(tr) && !HudHidden);
            if (!string.IsNullOrEmpty(tr)) _storyTracker.text = tr;
            int unread = sd != null && sd.State != null ? MailModel.Unread(sd.State) : 0;
            _mailBadge.text = unread > 0 ? unread.ToString() : "";
            _mailIcon.gameObject.SetActive(!HudHidden);
            if (Mail.Window.IsOpen) Mail.Refresh();
        }

        public void ShowHint(HintDef h)
        {
            if (h == null) return;
            _hints.Enqueue(h);
            if (!_hintPanel.gameObject.activeSelf) NextHint();
        }

        void NextHint()
        {
            if (_hints.Count == 0) { _hintPanel.gameObject.SetActive(false); return; }
            var h = _hints.Dequeue();
            _hintTitle.text = h.Title + (string.IsNullOrEmpty(h.Key) ? "" : "   <color=#9FD8FF>[" + h.Key + "]</color>");
            _hintText.text = h.Text;
            _hintPanel.gameObject.SetActive(true);
            UiFactory.Sound("Hint");
        }

        public void ShowStoryReward(string title, StoryReward r, Action done)
        {
            _storyRewardDone = done;
            var sb = new System.Text.StringBuilder("<b>" + title + "</b>\n\n");
            if (r != null)
            {
                if (r.Experience > 0) sb.Append(r.Experience + " EP\n");
                if (r.Karma > 0) sb.Append(r.Karma + " Karma\n");
                if (r.Copper > 0) sb.Append(WalletModel.Format(r.Copper) + "\n");
                if (r.HeroPoints > 0) sb.Append(r.HeroPoints + " Heldenpunkte\n");
                if (r.MasteryPoints > 0) sb.Append(r.MasteryPoints + " Beherrschungspunkte\n");
            }
            _storyRewardText.text = sb.ToString();
            _storyReward.Open();
        }
    }
}
