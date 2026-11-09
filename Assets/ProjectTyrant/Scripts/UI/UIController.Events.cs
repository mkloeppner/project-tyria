using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Belohnungs-Pop-up rechts (Medaille, EP, Karma, Münzen, Item) nach einem Ereignis.</summary>
    public partial class UIController
    {
        RectTransform _reward;
        TextMeshProUGUI _rewardTitle, _rewardMedal, _rewardBody;
        Image _rewardMedalIcon;
        float _rewardTimer;

        void BuildRewardPopup()
        {
            var bg = UiFactory.Image("EventReward", HudLayer, new Color(0.05f, 0.045f, 0.06f, 0.9f), UiFactory.Sprite("ui_panel"));
            _reward = bg.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 120), new Vector2(330, 150));
            _rewardMedalIcon = UiFactory.Image("Medal", _reward, Color.white, UiFactory.Sprite("ui_circle"));
            _rewardMedalIcon.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(56, 56));
            _rewardMedal = UiFactory.Text("MedalText", _rewardMedalIcon.transform, "", 13, TextAlignmentOptions.Center, new Color(0.1f, 0.08f, 0.05f));
            _rewardMedal.rectTransform.Stretch(); _rewardMedal.fontStyle = FontStyles.Bold;
            _rewardTitle = UiFactory.Text("Title", _reward, "", 17, TextAlignmentOptions.TopLeft, UiFactory.AccentColor);
            _rewardTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(80, -12), new Vector2(-92, 44));
            _rewardTitle.fontStyle = FontStyles.Bold;
            _rewardBody = UiFactory.Text("Body", _reward, "", 14, TextAlignmentOptions.TopLeft);
            _rewardBody.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -78), new Vector2(-28, 70));
            _reward.gameObject.SetActive(false);
        }

        public void ShowEventReward(string title, bool success, RewardTier medal, int xp, int karma, int coins, ItemDefinition item)
        {
            if (_reward == null) BuildRewardPopup();
            _rewardTitle.text = (success ? "Ereignis erfolgreich" : "Ereignis fehlgeschlagen") + "\n<size=80%><color=#DDD>" + title + "</color></size>";
            _rewardMedalIcon.color = EventChain.TierColor(medal);
            _rewardMedal.text = EventChain.TierName(medal);
            _rewardBody.text = "+" + xp.ToString("N0") + " EP   +" + karma.ToString("N0") + " Karma   +" + WalletModel.Format(coins)
                + (item != null ? "\n<color=#" + ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(item.Rarity)) + ">" + item.DisplayName + "</color>" : "")
                + (success ? "" : "\n<color=#F98>Fehlschlag: 25 % der Belohnung</color>");
            _reward.gameObject.SetActive(true);
            _rewardTimer = 6f;
        }

        void UpdateRewardPopup()
        {
            if (_reward == null || _rewardTimer <= 0f) return;
            _rewardTimer -= Time.unscaledDeltaTime;
            if (_rewardTimer <= 0f) _reward.gameObject.SetActive(false);
        }
    }
}
