using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum LootEntryKind { Item, RandomByRarity, Coins, Nothing }

    [Serializable]
    public class LootEntry
    {
        public LootEntryKind Kind = LootEntryKind.Item;
        public float Weight = 1f;
        public ItemDefinition Item;
        [Tooltip("Für RandomByRarity: Seltenheit und optionale Kategorie.")]
        public ItemRarity Rarity = ItemRarity.Basic;
        public ItemCategory Category = ItemCategory.Any;
        public int MinCount = 1, MaxCount = 1;
    }

    /// <summary>Loot-Tabelle (gewichtete Würfe + garantierte Einträge).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Loot Table", fileName = "Loot_")]
    public class LootTable : ScriptableObject
    {
        public string DisplayName = "Beute";
        public int Rolls = 1;
        public List<LootEntry> Guaranteed = new List<LootEntry>();
        public List<LootEntry> Weighted = new List<LootEntry>();
        public int MinCopper, MaxCopper;
        public int KarmaBonus;

        public float TotalWeight()
        {
            float w = 0f;
            foreach (var e in Weighted) w += Mathf.Max(0f, e.Weight);
            return w;
        }
    }
}
