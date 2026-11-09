using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum VendorKind { WeaponsArmor, General, Karma, Caravan }

    /// <summary>Kaufen, Verkaufen (zum Händlerwert), Rückkauf der letzten 10 verkauften Items, Plunder verkaufen.</summary>
    public class VendorModel
    {
        public const int BuybackSize = 10;
        public readonly List<(ItemInstance item, long price)> Buyback = new List<(ItemInstance, long)>();
        /// <summary>Preisfaktor (z. B. reduziertes Angebot nach gescheitertem Event).</summary>
        public float PriceFactor = 1f;

        public static long SellPrice(ItemInstance it) => it == null || it.Def == null ? 0 : (long)it.Def.VendorValue * it.Count;

        public bool Buy(VendorEntry e, InventoryModel inv, WalletModel wallet, out string error)
        {
            error = null;
            if (e == null || e.Item == null) { error = "Ungültig"; return false; }
            long copper = Mathf.RoundToInt(e.PriceCopper * PriceFactor);
            if (copper > 0 && wallet.Copper < copper) { error = "Nicht genug Münzen"; return false; }
            if (e.PriceKarma > 0 && wallet.Karma < e.PriceKarma) { error = "Nicht genug Karma"; return false; }
            if (inv.FreeSlots == 0 && inv.Count(e.Item) == 0) { error = "Inventar voll"; return false; }
            int rest = inv.TryAdd(e.Item, e.Count);
            if (rest > 0) { inv.RemoveItems(e.Item, e.Count - rest); error = "Inventar voll"; return false; }
            if (copper > 0) wallet.TrySpendCopper(copper);
            if (e.PriceKarma > 0) wallet.TrySpendKarma(e.PriceKarma);
            return true;
        }

        public bool Sell(InventoryModel inv, int bag, int slot, WalletModel wallet, out long price)
        {
            price = 0;
            var it = inv.Get(bag, slot);
            if (it == null || it.Def == null || it.Def.VendorValue <= 0) return false;
            price = SellPrice(it);
            inv.Remove(bag, slot);
            wallet.AddCopper(price);
            Buyback.Insert(0, (it, price));
            if (Buyback.Count > BuybackSize) Buyback.RemoveAt(Buyback.Count - 1);
            return true;
        }

        public bool BuyBack(int index, InventoryModel inv, WalletModel wallet, out string error)
        {
            error = null;
            if (index < 0 || index >= Buyback.Count) { error = "Ungültig"; return false; }
            var (it, price) = Buyback[index];
            if (wallet.Copper < price) { error = "Nicht genug Münzen"; return false; }
            if (inv.FreeSlots == 0) { error = "Inventar voll"; return false; }
            if (!inv.TryAdd(it.CloneWithCount(it.Count))) { error = "Inventar voll"; return false; }
            wallet.TrySpendCopper(price);
            Buyback.RemoveAt(index);
            return true;
        }

        public long SellJunk(InventoryModel inv, WalletModel wallet)
        {
            long total = 0;
            var junk = new List<(int, int)>();
            foreach (var e in inv.All()) if (e.item.Def.Rarity == ItemRarity.Junk) junk.Add((e.bag, e.slot));
            foreach (var (b, s) in junk) if (Sell(inv, b, s, wallet, out long p)) total += p;
            return total;
        }
    }

    /// <summary>Bergen: Item + Bergungs-Set (Ladung) → Materialien nach Typ/Seltenheit. Plunder ist nicht bergbar.</summary>
    public static class SalvageModel
    {
        public static bool CanSalvage(ItemDefinition d) => d != null && d.Rarity != ItemRarity.Junk && d.Salvage != null && d.Salvage.Count > 0 && d.Type != ItemType.SalvageKit;

        public static ItemInstance FindKit(InventoryModel inv, out int bag, out int slot)
        {
            foreach (var e in inv.All()) if (e.item.Def.Type == ItemType.SalvageKit && e.item.Charges > 0) { bag = e.bag; slot = e.slot; return e.item; }
            bag = slot = -1; return null;
        }

        public static List<(ItemDefinition, int)> Salvage(InventoryModel inv, int bag, int slot, System.Random rng, out string error)
        {
            error = null;
            var result = new List<(ItemDefinition, int)>();
            var it = inv.Get(bag, slot);
            if (it == null || !CanSalvage(it.Def)) { error = it != null && it.Def.Rarity == ItemRarity.Junk ? "Plunder kann nicht geborgen werden" : "Nicht bergbar"; return result; }
            var kit = FindKit(inv, out int kb, out int ks);
            if (kit == null) { error = "Kein Bergungs-Set mit Ladungen"; return result; }
            var def = it.Def;
            inv.Remove(bag, slot, 1);
            kit.Charges--;
            if (kit.Charges <= 0) inv.Remove(kb, ks);
            foreach (var y in def.Salvage)
            {
                if (y.Material == null) continue;
                int n = rng.Next(y.Min, y.Max + 1);
                if (n <= 0) continue;
                inv.TryAdd(y.Material, n);
                result.Add((y.Material, n));
            }
            inv.NotifyChanged();
            return result;
        }
    }
}
