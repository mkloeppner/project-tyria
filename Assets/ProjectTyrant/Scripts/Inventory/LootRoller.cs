using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public class LootResult
    {
        public readonly List<(ItemDefinition def, int count)> Items = new List<(ItemDefinition, int)>();
        public long Copper;
        public int Karma;
        public bool IsEmpty => Items.Count == 0 && Copper <= 0 && Karma <= 0;
    }

    /// <summary>Deterministischer Loot-Wurf (Seed) über gewichtete Tabellen.</summary>
    public static class LootRoller
    {
        public static LootResult Roll(LootTable table, ItemDatabase db, System.Random rng, int level)
        {
            var r = new LootResult();
            if (table == null) return r;
            foreach (var e in table.Guaranteed) Resolve(e, db, rng, level, r);
            float total = table.TotalWeight();
            for (int i = 0; i < table.Rolls && total > 0f; i++)
            {
                double pick = rng.NextDouble() * total;
                foreach (var e in table.Weighted)
                {
                    pick -= Mathf.Max(0f, e.Weight);
                    if (pick < 0) { Resolve(e, db, rng, level, r); break; }
                }
            }
            if (table.MaxCopper > 0) r.Copper += rng.Next(table.MinCopper, table.MaxCopper + 1);
            r.Karma += table.KarmaBonus;
            return r;
        }

        static void Resolve(LootEntry e, ItemDatabase db, System.Random rng, int level, LootResult r)
        {
            int count = e.MaxCount > e.MinCount ? rng.Next(e.MinCount, e.MaxCount + 1) : Mathf.Max(1, e.MinCount);
            switch (e.Kind)
            {
                case LootEntryKind.Item:
                    if (e.Item != null) Add(r, e.Item, count);
                    break;
                case LootEntryKind.RandomByRarity:
                    var d = PickRandom(db, e.Rarity, e.Category, level, rng);
                    if (d != null) Add(r, d, count);
                    break;
                case LootEntryKind.Coins:
                    r.Copper += count;
                    break;
            }
        }

        static void Add(LootResult r, ItemDefinition d, int count)
        {
            for (int i = 0; i < r.Items.Count; i++) if (r.Items[i].def == d && d.Stackable) { r.Items[i] = (d, r.Items[i].count + count); return; }
            r.Items.Add((d, count));
        }

        /// <summary>Zufälliges Item einer Seltenheit/Kategorie bis Gegnerlevel + 2 (sonst niedrigste verfügbare Stufe).</summary>
        public static ItemDefinition PickRandom(ItemDatabase db, ItemRarity rarity, ItemCategory cat, int level, System.Random rng)
        {
            if (db == null) return null;
            var pool = new List<ItemDefinition>();
            foreach (var d in db.Items)
            {
                if (d == null || d.Rarity != rarity) continue;
                if (cat != ItemCategory.Any && d.Category != cat) continue;
                if (cat == ItemCategory.Any && (d.Type == ItemType.Container || d.Type == ItemType.Bag || d.Type == ItemType.SalvageKit)) continue;
                if (d.RequiredLevel > level + 2) continue;
                pool.Add(d);
            }
            if (pool.Count == 0)
                foreach (var d in db.Items) if (d != null && d.Rarity == rarity && (cat == ItemCategory.Any || d.Category == cat)) pool.Add(d);
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }
    }
}
