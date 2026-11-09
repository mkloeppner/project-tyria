using UnityEngine;

namespace ProjectTyrant
{
    public partial class ProfileState
    {
        public InventoryModel Inventory = new InventoryModel();
        public EquipmentModel Equipment = new EquipmentModel();
        public FoodModel Food = new FoodModel();
        public bool StarterKitGranted;

        partial void WriteItems(SaveGameDto d)
        {
            d.StarterKitGranted = StarterKitGranted;
            for (int b = 1; b <= InventoryModel.BagSlots; b++)
            {
                var bag = Inventory.GetBagItem(b);
                d.Bags.Add(bag != null && bag.Def != null ? bag.Def.Id : "");
            }
            foreach (var e in Inventory.All())
                d.Inventory.Add(new ItemStackDto { Bag = e.bag, Slot = e.slot, Id = e.item.Def.Id, Count = e.item.Count, Bound = e.item.Bound, Charges = e.item.Charges });
            foreach (var kv in Equipment.Equipped)
                if (kv.Value?.Def != null) d.Equipment.Add(new EquipDto { Slot = (int)kv.Key, Id = kv.Value.Def.Id, Bound = kv.Value.Bound });
            if (Food.Active) d.Food = new FoodDto { Id = Food.Food.Id, Remaining = Food.Remaining };
        }

        partial void ReadItems(SaveGameDto d, ItemDatabase items)
        {
            StarterKitGranted = d.StarterKitGranted;
            Inventory.Clear(); Equipment.Clear();
            if (items == null) return;
            if (d.Bags != null)
                for (int i = 0; i < d.Bags.Count && i < InventoryModel.BagSlots; i++)
                {
                    var def = items.Get(d.Bags[i]);
                    if (def != null) Inventory.SetBagSlot(i + 1, new ItemInstance(def));
                }
            if (d.Inventory != null)
                foreach (var s in d.Inventory)
                {
                    var def = items.Get(s.Id);
                    if (def == null) continue;
                    var bag = Inventory.GetBag(s.Bag);
                    var it = new ItemInstance(def, Mathf.Max(1, s.Count), s.Bound) { Charges = s.Charges };
                    if (bag != null && s.Slot >= 0 && s.Slot < bag.Length && bag[s.Slot] == null) bag[s.Slot] = it;
                    else Inventory.TryAdd(it);
                }
            if (d.Equipment != null)
                foreach (var e in d.Equipment)
                {
                    var def = items.Get(e.Id);
                    if (def != null) Equipment.SetDirect((EquipSlot)e.Slot, new ItemInstance(def, 1, e.Bound));
                }
            if (d.Food != null)
            {
                var f = items.Get(d.Food.Id);
                if (f != null) { Food.Food = f; Food.Remaining = d.Food.Remaining; }
            }
            Inventory.NotifyChanged();
        }
    }
}
