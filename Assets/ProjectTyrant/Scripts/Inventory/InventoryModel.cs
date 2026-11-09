using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// GW2-Inventar: Rucksack (20 Plätze) + 4 Taschenplätze. Stapel bis MaxStack (250), Verschieben/Tauschen/Zusammenführen,
    /// Teilen, Taschen anlegen/abnehmen (nur wenn der Inhalt umverteilt werden kann), Aufräumen (Stapeln + Sortieren).
    /// </summary>
    public class InventoryModel
    {
        public const int BackpackSize = 20;
        public const int BagSlots = 4;

        readonly List<ItemInstance[]> _bags = new List<ItemInstance[]>();
        readonly ItemInstance[] _bagItems = new ItemInstance[BagSlots];
        public event Action Changed;

        public InventoryModel()
        {
            _bags.Add(new ItemInstance[BackpackSize]);
            for (int i = 0; i < BagSlots; i++) _bags.Add(null);
        }

        public int BagCount => _bags.Count;                       // 0 = Rucksack, 1..4 = Taschenplätze
        public ItemInstance[] GetBag(int i) => i >= 0 && i < _bags.Count ? _bags[i] : null;
        public ItemInstance GetBagItem(int bagSlot) => bagSlot >= 1 && bagSlot <= BagSlots ? _bagItems[bagSlot - 1] : null;
        public ItemInstance Get(int bag, int slot) { var b = GetBag(bag); return b != null && slot >= 0 && slot < b.Length ? b[slot] : null; }
        public void NotifyChanged() => Changed?.Invoke();

        public int TotalSlots { get { int n = 0; foreach (var b in _bags) if (b != null) n += b.Length; return n; } }
        public int FreeSlots { get { int n = 0; foreach (var b in _bags) if (b != null) foreach (var it in b) if (it == null) n++; return n; } }

        public IEnumerable<(int bag, int slot, ItemInstance item)> All()
        {
            for (int b = 0; b < _bags.Count; b++)
            {
                var bag = _bags[b]; if (bag == null) continue;
                for (int s = 0; s < bag.Length; s++) if (bag[s] != null) yield return (b, s, bag[s]);
            }
        }

        public int Count(ItemDefinition def) { int n = 0; foreach (var e in All()) if (e.item.Def == def) n += e.item.Count; return n; }

        /// <summary>Fügt Items hinzu (erst auf vorhandene Stapel, dann in freie Plätze). Gibt den nicht untergebrachten Rest zurück.</summary>
        public int TryAdd(ItemDefinition def, int count, bool bound = false, int charges = -1)
        {
            if (def == null || count <= 0) return count;
            var probe = new ItemInstance(def, 1, bound);
            if (def.Stackable)
            {
                foreach (var e in All())
                {
                    if (count <= 0) break;
                    if (!e.item.CanStackWith(probe)) continue;
                    int add = Math.Min(e.item.Space, count);
                    e.item.Count += add; count -= add;
                }
            }
            for (int b = 0; b < _bags.Count && count > 0; b++)
            {
                var bag = _bags[b]; if (bag == null) continue;
                for (int s = 0; s < bag.Length && count > 0; s++)
                {
                    if (bag[s] != null) continue;
                    int add = Math.Min(def.Stackable ? def.MaxStack : 1, count);
                    bag[s] = new ItemInstance(def, add, bound);
                    if (charges >= 0) bag[s].Charges = charges;
                    count -= add;
                }
            }
            Changed?.Invoke();
            return count;
        }

        public bool TryAdd(ItemInstance it)
        {
            if (it == null) return true;
            int rest = TryAdd(it.Def, it.Count, it.Bound, it.Def != null && it.Def.Type == ItemType.SalvageKit ? it.Charges : -1);
            it.Count = rest;
            return rest == 0;
        }

        /// <summary>Verschiebt: leeres Ziel → bewegen, gleicher Stapel → zusammenführen, sonst tauschen.</summary>
        public bool Move(int fb, int fs, int tb, int ts)
        {
            var from = GetBag(fb); var to = GetBag(tb);
            if (from == null || to == null || fs < 0 || ts < 0 || fs >= from.Length || ts >= to.Length) return false;
            if (fb == tb && fs == ts) return false;
            var a = from[fs]; var b = to[ts];
            if (a == null) return false;
            if (b != null && a.CanStackWith(b))
            {
                int add = Math.Min(b.Space, a.Count);
                if (add <= 0) { from[fs] = b; to[ts] = a; }
                else { b.Count += add; a.Count -= add; if (a.Count <= 0) from[fs] = null; }
            }
            else { from[fs] = b; to[ts] = a; }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Teilt einen Stapel: amount Stück in einen leeren (oder gleichartigen) Zielplatz.</summary>
        public bool Split(int fb, int fs, int amount, int tb, int ts)
        {
            var from = GetBag(fb); var to = GetBag(tb);
            if (from == null || to == null || fs >= from.Length || ts >= to.Length) return false;
            var a = from[fs]; if (a == null || amount <= 0 || amount >= a.Count) return false;
            var b = to[ts];
            if (b == null) { to[ts] = a.CloneWithCount(amount); a.Count -= amount; }
            else if (a.CanStackWith(b) && b.Space >= amount) { b.Count += amount; a.Count -= amount; }
            else return false;
            Changed?.Invoke();
            return true;
        }

        public ItemInstance Remove(int bag, int slot, int count = int.MaxValue)
        {
            var b = GetBag(bag); if (b == null || slot < 0 || slot >= b.Length || b[slot] == null) return null;
            var it = b[slot];
            if (count >= it.Count) { b[slot] = null; Changed?.Invoke(); return it; }
            it.Count -= count;
            Changed?.Invoke();
            return it.CloneWithCount(count);
        }

        public bool RemoveItems(ItemDefinition def, int count)
        {
            if (Count(def) < count) return false;
            for (int bi = 0; bi < _bags.Count && count > 0; bi++)
            {
                var bag = _bags[bi]; if (bag == null) continue;
                for (int s = 0; s < bag.Length && count > 0; s++)
                {
                    if (bag[s] == null || bag[s].Def != def) continue;
                    int take = Math.Min(count, bag[s].Count);
                    bag[s].Count -= take; count -= take;
                    if (bag[s].Count <= 0) bag[s] = null;
                }
            }
            Changed?.Invoke();
            return true;
        }

        public void Set(int bag, int slot, ItemInstance it) { var b = GetBag(bag); if (b != null && slot >= 0 && slot < b.Length) { b[slot] = it; Changed?.Invoke(); } }

        /// <summary>Legt eine Tasche aus dem Inventar in einen Taschenplatz (1–4).</summary>
        public bool EquipBag(int bagSlot, int fromBag, int fromSlot, out string error)
        {
            error = null;
            if (bagSlot < 1 || bagSlot > BagSlots) { error = "Ungültiger Taschenplatz"; return false; }
            var it = Get(fromBag, fromSlot);
            if (it == null || it.Def == null || it.Def.Type != ItemType.Bag || it.Def.BagSize <= 0) { error = "Keine Tasche"; return false; }
            if (fromBag == bagSlot) { error = "Eine Tasche kann nicht in sich selbst liegen"; return false; }
            var old = _bagItems[bagSlot - 1];
            var oldContents = _bags[bagSlot];
            GetBag(fromBag)[fromSlot] = null;
            var newBag = new ItemInstance[it.Def.BagSize];
            if (old != null)
            {
                int used = 0; foreach (var x in oldContents) if (x != null) used++;
                if (used > newBag.Length) { GetBag(fromBag)[fromSlot] = it; error = "Die neue Tasche ist zu klein für den Inhalt"; return false; }
                int k = 0; foreach (var x in oldContents) if (x != null) newBag[k++] = x;
            }
            _bagItems[bagSlot - 1] = it;
            _bags[bagSlot] = newBag;
            if (old != null) GetBag(fromBag)[fromSlot] = old; // alte Tasche landet am Platz der neuen
            Changed?.Invoke();
            return true;
        }

        /// <summary>Nimmt eine Tasche ab – nur wenn Inhalt und Tasche selbst in den übrigen Taschen Platz finden.</summary>
        public bool UnequipBag(int bagSlot, out string error)
        {
            error = null;
            if (bagSlot < 1 || bagSlot > BagSlots || _bagItems[bagSlot - 1] == null) { error = "Kein Beutel"; return false; }
            var contents = _bags[bagSlot];
            int needed = 1; foreach (var x in contents) if (x != null) needed++;
            int free = 0;
            for (int b = 0; b < _bags.Count; b++) { if (b == bagSlot || _bags[b] == null) continue; foreach (var x in _bags[b]) if (x == null) free++; }
            if (free < needed) { error = "Nicht genug freie Plätze, um den Inhalt der Tasche umzuverteilen"; return false; }
            var bagItem = _bagItems[bagSlot - 1];
            _bagItems[bagSlot - 1] = null; _bags[bagSlot] = null;
            foreach (var x in contents) if (x != null) PlaceInFree(x);
            PlaceInFree(bagItem);
            Changed?.Invoke();
            return true;
        }

        void PlaceInFree(ItemInstance it)
        {
            for (int b = 0; b < _bags.Count; b++)
            {
                var bag = _bags[b]; if (bag == null) continue;
                for (int s = 0; s < bag.Length; s++) if (bag[s] == null) { bag[s] = it; return; }
            }
        }

        public static int CategoryOrder(ItemDefinition d)
        {
            switch (d.Type)
            {
                case ItemType.Weapon: return 0; case ItemType.Armor: return 1; case ItemType.Trinket: return 2; case ItemType.Bag: return 3;
                case ItemType.Consumable: return 4; case ItemType.Container: return 5; case ItemType.SalvageKit: return 6; case ItemType.Material: return 7; case ItemType.Junk: return 9;
            }
            return 8;
        }

        /// <summary>„Inventar aufräumen“: Stapel zusammenführen, nach Typ, Seltenheit (absteigend) und Name sortieren.</summary>
        public void Sort()
        {
            var items = new List<ItemInstance>();
            foreach (var e in All()) items.Add(e.item);
            var merged = new List<ItemInstance>();
            foreach (var it in items)
            {
                if (it.Def.Stackable)
                {
                    foreach (var m in merged)
                    {
                        if (it.Count <= 0) break;
                        if (!m.CanStackWith(it)) continue;
                        int add = Math.Min(m.Space, it.Count); m.Count += add; it.Count -= add;
                    }
                }
                if (it.Count > 0) merged.Add(it);
            }
            merged.Sort((a, b) =>
            {
                int c = CategoryOrder(a.Def).CompareTo(CategoryOrder(b.Def)); if (c != 0) return c;
                c = b.Def.Rarity.CompareTo(a.Def.Rarity); if (c != 0) return c;
                return string.Compare(a.Def.DisplayName, b.Def.DisplayName, StringComparison.Ordinal);
            });
            foreach (var bag in _bags) if (bag != null) Array.Clear(bag, 0, bag.Length);
            int k = 0;
            for (int b = 0; b < _bags.Count && k < merged.Count; b++)
            {
                var bag = _bags[b]; if (bag == null) continue;
                for (int s = 0; s < bag.Length && k < merged.Count; s++) bag[s] = merged[k++];
            }
            Changed?.Invoke();
        }

        /// <summary>Für Laden: Taschenplatz direkt mit einer Tasche belegen.</summary>
        public void SetBagSlot(int bagSlot, ItemInstance bagItem)
        {
            if (bagSlot < 1 || bagSlot > BagSlots) return;
            _bagItems[bagSlot - 1] = bagItem;
            _bags[bagSlot] = bagItem != null && bagItem.Def != null ? new ItemInstance[bagItem.Def.BagSize] : null;
        }

        public void Clear()
        {
            _bags[0] = new ItemInstance[BackpackSize];
            for (int i = 1; i <= BagSlots; i++) { _bags[i] = null; _bagItems[i - 1] = null; }
            Changed?.Invoke();
        }
    }
}
