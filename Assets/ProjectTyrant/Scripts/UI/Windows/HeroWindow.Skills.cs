using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Tab „Fertigkeiten“: Leiste (Heilung, 3 Hilfe, Elite) + Pool nach Typ; Freischalten mit Heldenpunkten.</summary>
    public partial class HeroWindow
    {
        class SkillButton { public SkillDefinition Skill; public Image Bg, Frame; public TextMeshProUGUI Label, Cost; public int Slot = -1; }
        readonly List<SkillButton> _barButtons = new List<SkillButton>();
        readonly List<SkillButton> _poolButtons = new List<SkillButton>();
        TextMeshProUGUI _skillsInfo;
        int _selectedSlot = 1;
        static readonly string[] SlotNames = { "Heilung", "Hilfe 1", "Hilfe 2", "Hilfe 3", "Elite" };

        partial void BuildSkillsPage()
        {
            var page = SkillsPage;
            var barTitle = UiFactory.Text("BarTitle", page, "Fertigkeitenleiste  <size=70%><color=#AAB>(Platz anklicken, dann Fertigkeit wählen)</color></size>", 18, TextAlignmentOptions.Left, UiFactory.AccentColor);
            barTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(-20, 26));
            for (int i = 0; i < SkillLoadoutModel.SlotCount; i++)
            {
                var b = MakeSkillButton(page, new Vector2(20 + i * 92 + (i == 4 ? 24 : 0) + (i >= 1 ? 12 : 0), -40), 72);
                b.Slot = i; int slot = i;
                b.Bg.gameObject.AddComponent<UiClickRelay>().Left = () => { _selectedSlot = slot; MarkDirty(); };
                var cap = UiFactory.Text("Cap", b.Bg.transform, SlotNames[i], 13, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));
                cap.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(90, 18));
                _barButtons.Add(b);
            }
            _skillsInfo = UiFactory.Text("Info", page, "", 16, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.9f));
            _skillsInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(560, -40), new Vector2(-580, 90));

            var prof = PlayerProfile.Instance;
            var pool = prof != null && prof.AllSkills != null ? prof.AllSkills : new SkillDefinition[0];
            string[] colNames = { "Heilung", "Rufe", "Meditationen", "Fallen", "Elite" };
            for (int col = 0; col < 5; col++)
            {
                float x = 20 + col * 200;
                var h = UiFactory.Text("Col" + col, page, colNames[col], 17, TextAlignmentOptions.Left, UiFactory.AccentColor);
                h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -160), new Vector2(190, 24));
                var list = new List<SkillDefinition>();
                foreach (var s in pool)
                {
                    if (!SkillLoadoutModel.IsSlottable(s)) continue;
                    int c = s.SlotType == SkillSlotType.Heal ? 0 : s.SlotType == SkillSlotType.Elite ? 4 : s.Category == UtilityCategory.Shout ? 1 : s.Category == UtilityCategory.Meditation ? 2 : 3;
                    if (c == col) list.Add(s);
                }
                list.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.UnlockCost.CompareTo(b.UnlockCost));
                for (int k = 0; k < list.Count; k++)
                {
                    var b = MakeSkillButton(page, new Vector2(x, -192 - k * 112), 72);
                    b.Skill = list[k];
                    var sk = list[k];
                    b.Bg.gameObject.AddComponent<UiClickRelay>().Left = () => OnPoolClick(sk);
                    var name = UiFactory.Text("Name", b.Bg.transform, sk.DisplayName + (sk.SlotType == SkillSlotType.Utility ? "\n<size=80%><color=#AAB>Tier " + sk.Tier + "</color></size>" : ""), 14, TextAlignmentOptions.Left);
                    name.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(112, 60));
                    _poolButtons.Add(b);
                }
            }
        }

        SkillButton MakeSkillButton(RectTransform parent, Vector2 pos, float size)
        {
            var b = new SkillButton();
            b.Bg = UiFactory.Image("Skill", parent, UiFactory.SlotColor, UiFactory.Sprite("ui_round"), true);
            b.Bg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(size, size));
            b.Label = UiFactory.Text("Label", b.Bg.transform, "", 15, TextAlignmentOptions.Center);
            b.Label.rectTransform.Stretch(2, 2, 2, 2);
            b.Label.fontStyle = FontStyles.Bold;
            b.Frame = UiFactory.Image("Frame", b.Bg.transform, Color.clear, UiFactory.Sprite("ui_frame"));
            b.Frame.rectTransform.Stretch();
            b.Cost = UiFactory.Text("Cost", b.Bg.transform, "", 13, TextAlignmentOptions.BottomRight, new Color(0.6f, 0.9f, 1f));
            b.Cost.rectTransform.Stretch(2, 4, 2, 2);
            var tt = b.Bg.gameObject.AddComponent<UiTooltipTrigger>();
            tt.Provider = () => SkillTooltip(b);
            return b;
        }

        TooltipContent SkillTooltip(SkillButton b)
        {
            var s = b.Skill;
            if (s == null) return new TooltipContent { Title = SlotNames[Mathf.Max(0, b.Slot)], Subtitle = "Leerer Platz" };
            var prof = PlayerProfile.Instance;
            string info = null;
            if (prof != null && !prof.Loadout.IsUnlocked(s)) info = "Gesperrt – Klicken zum Freischalten (" + s.UnlockCost + " Heldenpunkte)";
            else if (b.Slot < 0) info = "Klicken: in " + SlotNames[TargetSlotFor(s)] + " legen";
            var c = TooltipBuilder.Skill(s, _ui.Player, null);
            if (info != null) { c.Footer = info; c.FooterColor = prof != null && prof.Loadout.IsUnlocked(s) ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.7f, 0.4f); }
            return c;
        }

        int TargetSlotFor(SkillDefinition s)
        {
            if (s.SlotType == SkillSlotType.Heal) return 0;
            if (s.SlotType == SkillSlotType.Elite) return 4;
            return _selectedSlot >= 1 && _selectedSlot <= 3 ? _selectedSlot : 1;
        }

        void OnPoolClick(SkillDefinition s)
        {
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            if (!prof.Loadout.IsUnlocked(s))
            {
                if (prof.BuildLocked) { GameEventBus.Notify("Im Kampf nicht änderbar", new Color(1f, 0.6f, 0.4f)); return; }
                if (!prof.Loadout.CanUnlock(s, prof.Progression, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.6f, 0.4f)); return; }
                _ui.Dialog.Confirm("Fertigkeit freischalten", s.DisplayName + " für " + s.UnlockCost + " Heldenpunkte freischalten?", () => { if (prof.UnlockSkill(s)) prof.SlotSkill(s, TargetSlotFor(s)); MarkDirty(); }, "Freischalten");
                return;
            }
            UiFactory.Sound("SkillAssign");
            prof.SlotSkill(s, TargetSlotFor(s));
            MarkDirty();
        }

        void PaintSkill(SkillButton b, SkillDefinition s, bool unlocked, bool selected)
        {
            b.Skill = s;
            b.Bg.color = s == null ? UiFactory.SlotColor : unlocked ? s.IconColor * 0.75f + new Color(0, 0, 0, 0.25f) : new Color(0.18f, 0.17f, 0.2f);
            b.Label.text = s == null ? "" : s.ShortLabel;
            b.Label.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            b.Frame.color = selected ? UiFactory.AccentColor : unlocked ? new Color(1, 1, 1, 0.25f) : new Color(0, 0, 0, 0.5f);
            b.Cost.text = s != null && !unlocked ? s.UnlockCost + " HP" : "";
        }

        partial void RefreshSkills()
        {
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            for (int i = 0; i < _barButtons.Count; i++) PaintSkill(_barButtons[i], prof.Loadout.Slots[i], true, i == _selectedSlot);
            foreach (var b in _poolButtons)
            {
                bool inBar = false; for (int i = 0; i < SkillLoadoutModel.SlotCount; i++) if (prof.Loadout.Slots[i] == b.Skill) inBar = true;
                PaintSkill(b, b.Skill, prof.Loadout.IsUnlocked(b.Skill), inBar);
            }
            int unlocked = 0, total = 0, spentSkills = 0, allCost = 0;
            foreach (var b in _poolButtons) { total++; allCost += b.Skill.UnlockCost; if (prof.Loadout.IsUnlocked(b.Skill)) { unlocked++; if (!b.Skill.StartsUnlocked) spentSkills += b.Skill.UnlockCost; } }
            _skillsInfo.text = "Freigeschaltet: " + unlocked + " / " + total + "   ·   investiert " + spentSkills + " / " + allCost + " Heldenpunkte\n"
                + "Verfügbare Heldenpunkte: <color=#9CF>" + prof.Progression.AvailableHeroPoints + "</color>"
                + (prof.BuildLocked ? "\n<color=#F96>Im Kampf gesperrt – Änderungen erst außerhalb des Kampfes.</color>" : "");
        }
    }
}
