using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public partial class PlayerProfile
    {
        [Header("Items")]
        public WeaponSet GreatswordSet;
        public WeaponSet StaffSet;
        public string[] StarterEquipment = new string[0];
        public string[] StarterInventory = new string[0];

        public InventoryModel Inventory => State.Inventory;
        public EquipmentModel Equipment => State.Equipment;
        public VendorModel ActiveVendor { get; set; }
        public VendorDefinition ActiveVendorDef { get; set; }
        readonly System.Random _itemRng = new System.Random(7781);

        EquipmentModel _hookedEquipment;
        FoodModel _hookedFood;

        /// <summary>Verbindet die Modelle des Profils mit dem Spieler (Attributquellen, Waffensets, Ereignisse).</summary>
        partial void InitRuntimeExtra()
        {
            if (Player == null) return;
            Player.EnsureInitialized();
            InitSkills();
            if (_hookedEquipment != null) { Player.Stats.RemoveSource(_hookedEquipment); _hookedEquipment.Changed -= OnEquipmentChanged; }
            if (_hookedFood != null) { Player.Stats.RemoveSource(_hookedFood); _hookedFood.Changed -= OnFoodChanged; }
            _hookedEquipment = State.Equipment; _hookedFood = State.Food;
            Player.Stats.AddSource(_hookedEquipment); Player.Stats.AddSource(_hookedFood);
            _hookedEquipment.Changed += OnEquipmentChanged; _hookedFood.Changed += OnFoodChanged;
            State.Inventory.Changed += GameEventBus.RaiseProfileChanged;
            if (!State.StarterKitGranted) GrantStarterKit();
            var caster = Player.GetComponent<SkillCaster>();
            if (caster != null) caster.Swapped += UpdateWeaponStrength;
            OnEquipmentChanged();
        }

        partial void ApplyExtra() { OnEquipmentChanged(); ApplySkills(); }

        void OnFoodChanged() { Player.Stats.MarkDirty(); GameEventBus.RaiseProfileChanged(); }

        void GrantStarterKit()
        {
            State.StarterKitGranted = true;
            if (Items == null) return;
            var starter = new System.Collections.Generic.List<string>(StarterEquipment);
            if (CurrentClass != null && CurrentClass.Id != "waechter") { starter.Clear(); foreach (var k in CurrentClass.StartWeapons) starter.Add(ItemIds.Recruit(k)); }
            foreach (var id in starter)
            {
                var d = Items.Get(id); if (d == null) continue;
                var slot = State.Equipment.PreferredSlot(d, State.Equipment.Get(EquipSlot.Weapon1) == null ? 0 : 1);
                State.Equipment.SetDirect(slot, new ItemInstance(d, 1, d.Binding == ItemBinding.SoulboundOnEquip));
            }
            foreach (var entry in StarterInventory)
            {
                var parts = entry.Split(':');
                var d = Items.Get(parts[0]); if (d == null) continue;
                int n = parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 1;
                State.Inventory.TryAdd(d, n);
            }
        }

        WeaponSet SetFor(WeaponKind k)
        {
            if (CurrentClass != null) return CurrentClass.SetFor(k, ActiveElite, State.Traits.ActiveEliteSteps);
            return k == WeaponKind.Greatsword ? GreatswordSet : k == WeaponKind.Staff ? StaffSet : null;
        }

        void OnEquipmentChanged()
        {
            if (Player == null) return;
            var caster = Player.GetComponent<SkillCaster>();
            if (caster != null) caster.SetWeaponSets(SetFor(State.Equipment.WeaponKindOf(0)), SetFor(State.Equipment.WeaponKindOf(1)));
            UpdateWeaponStrength();
            Player.Stats.MarkDirty();
            GameEventBus.RaiseProfileChanged();
        }

        void UpdateWeaponStrength()
        {
            if (Player == null) return;
            var caster = Player.GetComponent<SkillCaster>();
            int set = caster != null ? caster.ActiveSetIndex : 0;
            float ws = State.Equipment.WeaponStrength(set);
            var w = State.Equipment.Get(set == 1 ? EquipSlot.Weapon2 : EquipSlot.Weapon1);
            if (w != null && w.Def != null && EffectiveLevel < w.Def.ItemLevel) ws *= DamageFormula.WeaponStrength(EffectiveLevel) / DamageFormula.WeaponStrength(w.Def.ItemLevel);
            Player.WeaponStrengthOverride = ws;
        }

        void FixedUpdate() { State.Food.Tick(Time.fixedDeltaTime); }

        // ---------------- Item-Aktionen ----------------
        public bool EquipFromInventory(int bag, int slot, EquipSlot? target = null)
        {
            var it = State.Inventory.Get(bag, slot);
            if (it == null || Player == null) return false;
            var caster = Player.GetComponent<SkillCaster>();
            var s = target ?? State.Equipment.PreferredSlot(it.Def, caster != null ? caster.ActiveSetIndex : 0);
            if (Player.Stats != null && it.Def.IsEquippable && caster != null && caster.IsCasting) caster.CancelCast();
            if (!State.Equipment.CanEquip(it.Def, s, State.Progression.Level, out string reason)) { GameEventBus.Notify(reason, new Color(1f, 0.6f, 0.4f)); return false; }
            State.Inventory.Remove(bag, slot);
            State.Equipment.Equip(it, s, State.Progression.Level, out var prev, out _);
            if (prev != null) { State.Inventory.Set(bag, slot, prev); }
            GameEventBus.Notify("Angelegt: " + it.Def.DisplayName, ItemDefinition.RarityColor(it.Def.Rarity));
            RequestSave("Ausrüstung");
            return true;
        }

        public bool Unequip(EquipSlot s)
        {
            var it = State.Equipment.Get(s);
            if (it == null) return false;
            if (State.Inventory.FreeSlots == 0) { GameEventBus.Notify("Inventar voll", new Color(1f, 0.6f, 0.4f)); return false; }
            State.Equipment.Unequip(s);
            State.Inventory.TryAdd(it);
            return true;
        }

        /// <summary>Benutzen/Anlegen per Rechtsklick oder Doppelklick.</summary>
        public void UseItem(int bag, int slot)
        {
            var it = State.Inventory.Get(bag, slot);
            if (it == null) return;
            var d = it.Def;
            switch (d.Type)
            {
                case ItemType.Weapon: case ItemType.Armor: case ItemType.Trinket:
                    EquipFromInventory(bag, slot); break;
                case ItemType.Bag:
                    for (int b = 1; b <= InventoryModel.BagSlots; b++)
                        if (State.Inventory.GetBagItem(b) == null)
                        {
                            if (!State.Inventory.EquipBag(b, bag, slot, out string err)) GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f));
                            return;
                        }
                    GameEventBus.Notify("Alle Taschenplätze belegt", new Color(1f, 0.6f, 0.4f)); break;
                case ItemType.Consumable:
                    if (d.Consumable == ConsumableKind.Food) { State.Food.Eat(d); State.Inventory.Remove(bag, slot, 1); GameEventBus.Notify("Nahrung: " + d.DisplayName + " (30 min)", new Color(0.9f, 0.9f, 0.6f)); }
                    else if (d.Consumable == ConsumableKind.HealPotion) { DamageCalculator.Heal(Player, Player, d.HealBase, 0.5f); State.Inventory.Remove(bag, slot, 1); }
                    else if (d.Consumable == ConsumableKind.OpenContainer) OpenContainer(bag, slot);
                    break;
                case ItemType.Container: OpenContainer(bag, slot); break;
                case ItemType.SalvageKit: GameEventBus.Notify("Rechtsklick auf ein Item → „Bergen“", new Color(0.9f, 0.9f, 0.9f)); break;
                default: GameEventBus.Notify(d.DisplayName + " kann nicht benutzt werden", new Color(0.8f, 0.8f, 0.8f)); break;
            }
        }

        public void OpenContainer(int bag, int slot)
        {
            var it = State.Inventory.Get(bag, slot);
            if (it == null || it.Def.ContainerLoot == null) return;
            var loot = LootRoller.Roll(it.Def.ContainerLoot, Items, _itemRng, Mathf.Max(State.Progression.Level, it.Def.ItemLevel));
            State.Inventory.Remove(bag, slot, 1);
            GrantLoot(loot, it.Def.DisplayName);
        }

        /// <summary>Fügt Beute dem Inventar hinzu; gibt nicht untergebrachte Items zurück.</summary>
        public LootResult GrantLoot(LootResult loot, string source)
        {
            var rest = new LootResult();
            foreach (var (d, n) in loot.Items)
            {
                int left = State.Inventory.TryAdd(d, n);
                if (left > 0) rest.Items.Add((d, left));
                if (n - left > 0) { GameEventBus.RaiseItemAcquired(d.Id, n - left); GameEventBus.Notify("+ " + d.DisplayName + (n - left > 1 ? " ×" + (n - left) : ""), ItemDefinition.RarityColor(d.Rarity)); }
            }
            if (loot.Copper > 0) State.Wallet.AddCopper(loot.Copper);
            if (loot.Karma > 0) State.Wallet.AddKarma(loot.Karma);
            if (rest.Items.Count > 0) GameEventBus.Notify("Inventar voll!", new Color(1f, 0.5f, 0.4f));
            return rest;
        }

        public void Salvage(int bag, int slot)
        {
            var got = SalvageModel.Salvage(State.Inventory, bag, slot, _itemRng, out string err);
            if (err != null) { GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f)); return; }
            var sb = new System.Text.StringBuilder("Geborgen: ");
            foreach (var (d, n) in got) sb.Append(d.DisplayName).Append(" ×").Append(n).Append("  ");
            GameEventBus.Notify(sb.ToString(), new Color(0.8f, 0.9f, 1f));
        }

        public bool GiveItem(ItemDefinition d, int count = 1) => d != null && State.Inventory.TryAdd(d, count) == 0;
    }
}
