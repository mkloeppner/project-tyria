using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    static class TestItems
    {
        public static ItemDefinition Make(string id, ItemType t, ItemRarity r = ItemRarity.Basic, int stack = 1, int level = 1)
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>();
            d.Id = id; d.DisplayName = id; d.Type = t; d.Rarity = r; d.MaxStack = stack; d.RequiredLevel = level; d.ItemLevel = level; d.VendorValue = 10;
            return d;
        }
        public static ItemDefinition Bag(int size) { var d = Make("bag" + size, ItemType.Bag); d.BagSize = size; return d; }
        public static ItemDefinition Armor(ArmorPiece p, int level, params AttrValue[] a)
        {
            var d = Make("armor_" + p, ItemType.Armor, ItemRarity.Fine, 1, level); d.Armor = p; d.Defense = 50; d.Attributes = new List<AttrValue>(a); return d;
        }
    }

    public class InventoryTests
    {
        [Test]
        public void Stacking_FillsExistingStacksThenNewSlots()
        {
            var inv = new InventoryModel();
            var mat = TestItems.Make("mat", ItemType.Material, ItemRarity.Basic, 250);
            Assert.AreEqual(0, inv.TryAdd(mat, 200));
            Assert.AreEqual(0, inv.TryAdd(mat, 100));
            Assert.AreEqual(250, inv.Get(0, 0).Count);
            Assert.AreEqual(50, inv.Get(0, 1).Count);
            Assert.AreEqual(300, inv.Count(mat));
            Assert.AreEqual(18, inv.FreeSlots);
            var sword = TestItems.Make("sword", ItemType.Weapon);
            Assert.AreEqual(0, inv.TryAdd(sword, 2));
            Assert.AreEqual(16, inv.FreeSlots, "Nicht stapelbare Items belegen je einen Platz");
        }

        [Test]
        public void Full_ReturnsRemainder()
        {
            var inv = new InventoryModel();
            var sword = TestItems.Make("sword", ItemType.Weapon);
            Assert.AreEqual(5, inv.TryAdd(sword, 25));
            Assert.AreEqual(0, inv.FreeSlots);
        }

        [Test]
        public void Split_And_Move_MergeAndSwap()
        {
            var inv = new InventoryModel();
            var mat = TestItems.Make("mat", ItemType.Material, ItemRarity.Basic, 250);
            var junk = TestItems.Make("junk", ItemType.Junk, ItemRarity.Junk, 250);
            inv.TryAdd(mat, 100);
            Assert.IsTrue(inv.Split(0, 0, 30, 0, 5));
            Assert.AreEqual(70, inv.Get(0, 0).Count);
            Assert.AreEqual(30, inv.Get(0, 5).Count);
            Assert.IsFalse(inv.Split(0, 0, 70, 0, 6), "Ganzer Stapel ist kein Teilen");
            Assert.IsTrue(inv.Move(0, 5, 0, 0));
            Assert.AreEqual(100, inv.Get(0, 0).Count);
            Assert.IsNull(inv.Get(0, 5));
            inv.Set(0, 3, new ItemInstance(junk, 4));
            Assert.IsTrue(inv.Move(0, 0, 0, 3));
            Assert.AreEqual(junk, inv.Get(0, 0).Def, "Ungleiche Items werden getauscht");
            Assert.AreEqual(mat, inv.Get(0, 3).Def);
        }

        [Test]
        public void BagWithContents_EquipUnequipRules()
        {
            var inv = new InventoryModel();
            var bag8 = TestItems.Bag(8);
            var bag10 = TestItems.Bag(10);
            var sword = TestItems.Make("sword", ItemType.Weapon);
            inv.Set(0, 0, new ItemInstance(bag8, 1));
            Assert.IsTrue(inv.EquipBag(1, 0, 0, out _));
            Assert.AreEqual(28, inv.TotalSlots);
            for (int i = 0; i < 8; i++) inv.Set(1, i, new ItemInstance(sword, 1));
            // Tasche tauschen: Inhalt wandert in die größere Tasche, alte Tasche landet im Rucksack
            inv.Set(0, 0, new ItemInstance(bag10, 1));
            Assert.IsTrue(inv.EquipBag(1, 0, 0, out _));
            Assert.AreEqual(bag10, inv.GetBagItem(1).Def);
            Assert.AreEqual(bag8, inv.Get(0, 0).Def);
            int inBag = 0; foreach (var x in inv.GetBag(1)) if (x != null) inBag++;
            Assert.AreEqual(8, inBag);
            // Rucksack fast füllen → Abnehmen muss scheitern (8 Items + Tasche = 9 Plätze nötig)
            for (int i = 1; i < 13; i++) inv.Set(0, i, new ItemInstance(sword, 1));
            Assert.AreEqual(7, inv.GetBag(0).Length - 13);
            Assert.IsFalse(inv.UnequipBag(1, out string err));
            Assert.IsNotNull(err);
            inv.Set(0, 12, null); inv.Set(0, 11, null);
            Assert.IsTrue(inv.UnequipBag(1, out _));
            Assert.AreEqual(20, inv.TotalSlots);
            Assert.AreEqual(0, inv.FreeSlots);
        }

        [Test]
        public void Sort_StacksAndOrdersByCategoryThenRarity()
        {
            var inv = new InventoryModel();
            var mat = TestItems.Make("mat", ItemType.Material, ItemRarity.Basic, 250);
            var w1 = TestItems.Make("w1", ItemType.Weapon, ItemRarity.Fine);
            var w2 = TestItems.Make("w2", ItemType.Weapon, ItemRarity.Rare);
            inv.Set(0, 7, new ItemInstance(mat, 20));
            inv.Set(0, 2, new ItemInstance(w1, 1));
            inv.Set(0, 9, new ItemInstance(mat, 30));
            inv.Set(0, 4, new ItemInstance(w2, 1));
            inv.Sort();
            Assert.AreEqual(w2, inv.Get(0, 0).Def);
            Assert.AreEqual(w1, inv.Get(0, 1).Def);
            Assert.AreEqual(mat, inv.Get(0, 2).Def);
            Assert.AreEqual(50, inv.Get(0, 2).Count);
            Assert.IsNull(inv.Get(0, 3));
        }
    }

    public class WalletTests
    {
        [Test]
        public void CopperSilverGold_Conversion()
        {
            Assert.AreEqual(12345L, WalletModel.FromGold(1, 23, 45));
            WalletModel.Split(12345, out long g, out int s, out int c);
            Assert.AreEqual(1, g); Assert.AreEqual(23, s); Assert.AreEqual(45, c);
            Assert.AreEqual("1 g 23 s 45 k", WalletModel.Format(12345));
            var w = new WalletModel();
            w.AddCopper(50);
            Assert.IsFalse(w.TrySpendCopper(51));
            Assert.IsTrue(w.TrySpendCopper(50));
            Assert.AreEqual(0, w.Copper);
        }
    }

    public class EquipmentTests
    {
        [Test]
        public void Equip_AggregatesAttributesAndDefense()
        {
            var eq = new EquipmentModel();
            var stats = new AttributeSet { Level = 10 };
            stats.AddSource(eq);
            float basePower = stats.Get(Attr.Power), baseArmor = stats.Armor;
            Assert.IsTrue(eq.Equip(new ItemInstance(TestItems.Armor(ArmorPiece.Chest, 5, new AttrValue(Attr.Power, 40)), 1), EquipSlot.Chest, 10, out _, out _));
            Assert.IsTrue(eq.Equip(new ItemInstance(TestItems.Armor(ArmorPiece.Head, 5, new AttrValue(Attr.Power, 10), new AttrValue(Attr.Precision, 7)), 1), EquipSlot.Head, 10, out _, out _));
            stats.MarkDirty();
            Assert.AreEqual(basePower + 50f, stats.Get(Attr.Power), 0.01f);
            Assert.AreEqual(baseArmor + 100f, stats.Armor, 0.01f);
            eq.Unequip(EquipSlot.Chest); stats.MarkDirty();
            Assert.AreEqual(basePower + 10f, stats.Get(Attr.Power), 0.01f);
        }

        [Test]
        public void LevelLock_And_SlotFit()
        {
            var eq = new EquipmentModel();
            var helm = TestItems.Armor(ArmorPiece.Head, 12);
            Assert.IsFalse(eq.CanEquip(helm, EquipSlot.Head, 10, out string r1));
            StringAssert.Contains("12", r1);
            Assert.IsTrue(eq.CanEquip(helm, EquipSlot.Head, 12, out _));
            Assert.IsFalse(eq.CanEquip(helm, EquipSlot.Chest, 80, out _), "Helm passt nicht in die Brust");
            var ring = TestItems.Make("ring", ItemType.Trinket); ring.Trinket = TrinketKind.Ring;
            Assert.IsTrue(eq.CanEquip(ring, EquipSlot.Ring2, 1, out _));
            Assert.IsFalse(eq.CanEquip(ring, EquipSlot.Amulet, 1, out _));
        }

        [Test]
        public void SoulboundOnEquip_BindsAndReturnsPrevious()
        {
            var eq = new EquipmentModel();
            var a = TestItems.Make("gs_a", ItemType.Weapon); a.Weapon = WeaponKind.Greatsword; a.Binding = ItemBinding.SoulboundOnEquip; a.WeaponStrengthMin = 100; a.WeaponStrengthMax = 120;
            var b = TestItems.Make("gs_b", ItemType.Weapon); b.Weapon = WeaponKind.Staff;
            var ia = new ItemInstance(a, 1);
            Assert.IsFalse(ia.Bound);
            eq.Equip(ia, EquipSlot.Weapon1, 1, out var prev, out _);
            Assert.IsNull(prev);
            Assert.IsTrue(ia.Bound);
            Assert.AreEqual(110f, eq.WeaponStrength(0), 0.01f);
            Assert.AreEqual(WeaponKind.Greatsword, eq.WeaponKindOf(0));
            eq.Equip(new ItemInstance(b, 1), EquipSlot.Weapon1, 1, out prev, out _);
            Assert.AreSame(ia, prev);
            Assert.IsFalse(eq.Get(EquipSlot.Weapon1).Bound, "Ohne Bindung bleibt ungebunden");
        }
    }

    public class LootTests
    {
        static ItemDatabase Db(out LootTable table)
        {
            var db = ScriptableObject.CreateInstance<ItemDatabase>();
            db.Items = new List<ItemDefinition>();
            for (int i = 0; i < 6; i++) db.Items.Add(TestItems.Make("fine" + i, ItemType.Weapon, ItemRarity.Fine, 1, 1 + i * 3));
            var junk = TestItems.Make("junk", ItemType.Junk, ItemRarity.Junk, 250);
            db.Items.Add(junk);
            table = ScriptableObject.CreateInstance<LootTable>();
            table.Rolls = 2; table.MinCopper = 5; table.MaxCopper = 10;
            table.Weighted = new List<LootEntry> {
                new LootEntry { Kind = LootEntryKind.Item, Item = junk, Weight = 3 },
                new LootEntry { Kind = LootEntryKind.RandomByRarity, Rarity = ItemRarity.Fine, Weight = 1 } };
            return db;
        }

        [Test]
        public void Roll_IsDeterministicForSeed()
        {
            var db = Db(out var t);
            string Run() { var rng = new System.Random(99); var sb = new System.Text.StringBuilder(); for (int i = 0; i < 20; i++) { var r = LootRoller.Roll(t, db, rng, 5); foreach (var (d, n) in r.Items) sb.Append(d.Id).Append(n); sb.Append('|').Append(r.Copper); } return sb.ToString(); }
            Assert.AreEqual(Run(), Run());
        }

        [Test]
        public void Weights_DistributeProportionally_AndRespectLevel()
        {
            var db = Db(out var t);
            Assert.AreEqual(4f, t.TotalWeight(), 0.001f);
            var rng = new System.Random(1234);
            int junk = 0, fine = 0;
            for (int i = 0; i < 4000; i++)
            {
                var r = LootRoller.Roll(t, db, rng, 4);
                foreach (var (d, n) in r.Items)
                {
                    if (d.Rarity == ItemRarity.Junk) junk += n; else { fine += n; Assert.LessOrEqual(d.RequiredLevel, 6, "Nur Items bis Stufe +2"); }
                }
                Assert.That(r.Copper, Is.InRange(5, 10));
            }
            float ratio = junk / (float)(junk + fine);
            Assert.AreEqual(0.75f, ratio, 0.03f);
        }

        [Test]
        public void Guaranteed_AlwaysDrops()
        {
            var db = Db(out var t);
            t.Guaranteed = new List<LootEntry> { new LootEntry { Kind = LootEntryKind.RandomByRarity, Rarity = ItemRarity.Fine, Weight = 1 } };
            var rng = new System.Random(5);
            for (int i = 0; i < 50; i++)
            {
                var r = LootRoller.Roll(t, db, rng, 20);
                Assert.IsTrue(r.Items.Exists(x => x.def.Rarity == ItemRarity.Fine));
            }
        }
    }

    public class VendorTests
    {
        [Test]
        public void BuySellBuyback()
        {
            var inv = new InventoryModel(); var w = new WalletModel(); var v = new VendorModel();
            var potion = TestItems.Make("potion", ItemType.Consumable, ItemRarity.Basic, 250);
            var entry = new VendorEntry { Item = potion, PriceCopper = 24, Count = 5 };
            Assert.IsFalse(v.Buy(entry, inv, w, out _), "Ohne Geld kein Kauf");
            w.AddCopper(100);
            Assert.IsTrue(v.Buy(entry, inv, w, out _));
            Assert.AreEqual(76, w.Copper);
            Assert.AreEqual(5, inv.Count(potion));
            Assert.IsTrue(v.Sell(inv, 0, 0, w, out long price));
            Assert.AreEqual(50, price, "5 × Händlerwert 10");
            Assert.AreEqual(126, w.Copper);
            Assert.AreEqual(1, v.Buyback.Count);
            Assert.IsTrue(v.BuyBack(0, inv, w, out _));
            Assert.AreEqual(76, w.Copper);
            Assert.AreEqual(5, inv.Count(potion));
            Assert.AreEqual(0, v.Buyback.Count);
        }

        [Test]
        public void SellJunk_SellsOnlyJunk()
        {
            var inv = new InventoryModel(); var w = new WalletModel(); var v = new VendorModel();
            inv.TryAdd(TestItems.Make("junk", ItemType.Junk, ItemRarity.Junk, 250), 3);
            inv.TryAdd(TestItems.Make("sword", ItemType.Weapon, ItemRarity.Fine), 1);
            Assert.AreEqual(30, v.SellJunk(inv, w));
            Assert.AreEqual(19, inv.FreeSlots);
        }

        [Test]
        public void Salvage_UsesKitCharge_AndRejectsJunk()
        {
            var inv = new InventoryModel();
            var mat = TestItems.Make("mat", ItemType.Material, ItemRarity.Basic, 250);
            var kit = TestItems.Make("kit", ItemType.SalvageKit); kit.KitCharges = 2;
            var sword = TestItems.Make("sword", ItemType.Weapon, ItemRarity.Fine);
            sword.Salvage = new List<SalvageYield> { new SalvageYield { Material = mat, Min = 2, Max = 2 } };
            var junk = TestItems.Make("junk", ItemType.Junk, ItemRarity.Junk, 250);
            inv.TryAdd(kit, 1); inv.TryAdd(sword, 2); inv.TryAdd(junk, 1);
            var rng = new System.Random(1);
            SalvageModel.Salvage(inv, 0, 1, rng, out string err);
            Assert.IsNull(err);
            Assert.AreEqual(2, inv.Count(mat));
            Assert.AreEqual(1, inv.Get(0, 0).Charges);
            SalvageModel.Salvage(inv, 0, 3, rng, out err);
            Assert.IsNotNull(err, "Plunder ist nicht bergbar");
            SalvageModel.Salvage(inv, 0, 2, rng, out err);
            Assert.IsNull(err);
            Assert.AreEqual(4, inv.Count(mat));
            Assert.AreEqual(0, inv.Count(kit), "Leeres Kit verschwindet");
        }
    }
}
