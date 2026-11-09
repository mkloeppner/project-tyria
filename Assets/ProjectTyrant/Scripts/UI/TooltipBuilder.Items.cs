using UnityEngine;

namespace ProjectTyrant
{
    public static partial class TooltipBuilder
    {
        public static string TypeName(ItemDefinition d)
        {
            switch (d.Type)
            {
                case ItemType.Weapon: return d.Weapon == WeaponKind.Staff ? "Stab (zweihändig)" : "Großschwert (zweihändig)";
                case ItemType.Armor: return "Schwere Rüstung · " + EquipmentModel.SlotName(EquipSlot.Head + (int)d.Armor);
                case ItemType.Trinket: return d.Trinket == TrinketKind.Back ? "Rückenteil" : d.Trinket == TrinketKind.Amulet ? "Amulett" : d.Trinket == TrinketKind.Ring ? "Ring" : "Accessoire";
                case ItemType.Consumable: return d.Consumable == ConsumableKind.Food ? "Nahrung" : d.Consumable == ConsumableKind.HealPotion ? "Trank" : "Verbrauchsgut";
                case ItemType.Material: return "Handwerksmaterial";
                case ItemType.Junk: return "Plunder";
                case ItemType.Bag: return "Tasche (" + d.BagSize + " Plätze)";
                case ItemType.SalvageKit: return "Bergungs-Set";
                case ItemType.Container: return "Behälter";
            }
            return "";
        }

        public static TooltipContent Item(ItemInstance it, bool withCompare)
        {
            if (it == null || it.Def == null) return null;
            var d = it.Def;
            var c = new TooltipContent { Title = d.DisplayName + (it.Count > 1 ? " (" + it.Count + ")" : ""), TitleColor = ItemDefinition.RarityColor(d.Rarity), Subtitle = TypeName(d) + " · " + ItemDefinition.RarityName(d.Rarity) };
            c.TopRightIcon = d.Icon;
            if (d.Type == ItemType.Weapon) c.Add("Waffenstärke: " + d.WeaponStrengthMin.ToString("N0") + "–" + d.WeaponStrengthMax.ToString("N0"), Fact);
            if (d.Type == ItemType.Armor) c.Add("Verteidigung: " + d.Defense.ToString("N0"), Fact);
            foreach (var a in d.Attributes) c.Add("+" + a.Value + " " + AttributeSet.AttrName(a.Attr), new Color(0.45f, 0.95f, 0.45f));
            if (d.Type == ItemType.Consumable && d.Consumable == ConsumableKind.Food)
            {
                foreach (var a in d.FoodAttributes) c.Add("Nahrung: +" + a.Value + " " + AttributeSet.AttrName(a.Attr) + " (skaliert)", new Color(0.95f, 0.9f, 0.6f));
                c.Add("Dauer: " + Mathf.RoundToInt(d.FoodDurationMinutes) + " min", Fact);
            }
            if (d.Type == ItemType.SalvageKit) c.Add("Ladungen: " + it.Charges + " / " + d.KitCharges, Fact);
            if (!string.IsNullOrEmpty(d.Description)) c.Add(d.Description, Gray);
            if (d.IsEquippable)
            {
                int lvl = PlayerProfile.Instance != null ? PlayerProfile.Instance.Progression.Level : 1;
                c.Add("Benötigte Stufe: " + d.RequiredLevel, d.RequiredLevel > lvl ? new Color(1f, 0.35f, 0.3f) : Fact);
            }
            string bind = it.Bound ? (d.Binding == ItemBinding.AccountBound ? "Accountgebunden" : "Seelengebunden") : d.Binding == ItemBinding.SoulboundOnEquip ? "Seelengebunden beim Anlegen" : d.Binding == ItemBinding.AccountBound ? "Accountgebunden" : "";
            if (bind.Length > 0) c.Add(bind, new Color(0.7f, 0.75f, 0.9f));
            if (d.VendorValue > 0) c.Footer = "Händlerwert: " + WalletModel.Format((long)d.VendorValue * it.Count);
            else c.Footer = "Nicht verkäuflich";
            if (withCompare && d.IsEquippable && PlayerProfile.Instance != null)
            {
                var eq = PlayerProfile.Instance.Equipment;
                var slot = eq.PreferredSlot(d);
                var other = eq.Get(slot);
                if (other != null && other != it)
                {
                    c.Compare = Item(other, false);
                    if (c.Compare != null) c.Compare.Subtitle = "Angelegt · " + EquipmentModel.SlotName(slot);
                }
            }
            return c;
        }
    }
}
