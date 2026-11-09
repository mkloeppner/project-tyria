using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectTyrant
{
    public partial class HeroWindow
    {
        readonly List<UiItemSlot> _equipSlots = new List<UiItemSlot>();
        TextMeshProUGUI _equipStats;

        partial void BuildEquipmentPage()
        {
            var page = EquipmentPage;
            EquipSlot[] left = { EquipSlot.Head, EquipSlot.Shoulders, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Legs, EquipSlot.Feet };
            EquipSlot[] right = { EquipSlot.Back, EquipSlot.Amulet, EquipSlot.Ring1, EquipSlot.Ring2, EquipSlot.Accessory1, EquipSlot.Accessory2 };
            for (int i = 0; i < 6; i++)
            {
                AddEquipSlot(page, left[i], new Vector2(20, -10 - i * 74));
                AddEquipSlot(page, right[i], new Vector2(200, -10 - i * 74));
            }
            AddEquipSlot(page, EquipSlot.Weapon1, new Vector2(20, -470));
            AddEquipSlot(page, EquipSlot.Weapon2, new Vector2(200, -470));
            var doll = UiFactory.Image("Doll", page, new Color(1, 1, 1, 0.06f), UiFactory.Sprite("ui_hero_silhouette"));
            doll.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -40), new Vector2(110, 380));
            doll.raycastTarget = false; doll.transform.SetAsFirstSibling();
            _equipStats = UiFactory.Text("Stats", page, "", 17, TextAlignmentOptions.TopLeft);
            _equipStats.rectTransform.anchorMin = new Vector2(0, 0); _equipStats.rectTransform.anchorMax = new Vector2(1, 1);
            _equipStats.rectTransform.offsetMin = new Vector2(400, 10); _equipStats.rectTransform.offsetMax = new Vector2(-10, -10);
        }

        void AddEquipSlot(RectTransform page, EquipSlot s, Vector2 pos)
        {
            var slot = UiItemSlot.Create(page, 60, SlotKind.Equipment, EquipmentModel.SlotName(s));
            ((RectTransform)slot.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(60, 60));
            slot.Equip = s;
            slot.GetItem = () => PlayerProfile.Instance?.Equipment.Get(s);
            var lbl = UiFactory.Text("Label", slot.transform, EquipmentModel.SlotName(s), 13, TextAlignmentOptions.Left, new Color(0.75f, 0.75f, 0.8f));
            lbl.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(110, 40));
            _equipSlots.Add(slot);
        }

        partial void RefreshEquipment()
        {
            foreach (var s in _equipSlots) s.Refresh();
            var p = _ui.Player;
            var prof = PlayerProfile.Instance;
            string food = prof != null && prof.State.Food.Active ? "\n\nNahrung: " + prof.State.Food.Food.DisplayName + " (" + Mathf.CeilToInt(prof.State.Food.Remaining / 60f) + " min)" : "";
            _equipStats.text = AttributeSummary(p) + food;
        }
    }
}
