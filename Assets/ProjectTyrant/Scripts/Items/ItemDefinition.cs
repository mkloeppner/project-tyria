using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum ItemRarity { Junk, Basic, Fine, Masterwork, Rare, Exotic, Ascended, Legendary }
    public enum ItemType { Weapon, Armor, Trinket, Consumable, Material, Junk, Bag, SalvageKit, Container }
    public enum ItemCategory { Any, Weapon, Armor, Trinket, Consumable, Material, Junk, Bag, Tool, Container }
    public enum EquipSlot { None, Weapon1, Weapon2, Head, Shoulders, Chest, Hands, Legs, Feet, Back, Amulet, Ring1, Ring2, Accessory1, Accessory2 }
    public enum ArmorPiece { Head, Shoulders, Chest, Hands, Legs, Feet }
    public enum TrinketKind { Back, Amulet, Ring, Accessory }
    public enum ItemBinding { None, AccountBound, SoulboundOnEquip }
    public enum ConsumableKind { None, Food, HealPotion, OpenContainer }

    [Serializable]
    public struct AttrValue
    {
        public Attr Attr;
        public int Value;
        public AttrValue(Attr a, int v) { Attr = a; Value = v; }
    }

    [Serializable]
    public struct SalvageYield
    {
        public ItemDefinition Material;
        public int Min, Max;
    }

    /// <summary>Item-Definition (vom Builder aus Familien × Seltenheit × Präfix erzeugt).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Item Definition", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;
        public ItemType Type;
        public ItemRarity Rarity = ItemRarity.Basic;
        public int RequiredLevel = 1;
        public int ItemLevel = 1;
        public WeaponKind Weapon = WeaponKind.None;
        public ArmorPiece Armor;
        public TrinketKind Trinket;
        public int WeaponStrengthMin, WeaponStrengthMax;
        public int Defense;
        public string Prefix;
        public List<AttrValue> Attributes = new List<AttrValue>();
        public int MaxStack = 1;
        public int VendorValue;
        public ItemBinding Binding = ItemBinding.None;
        public List<SalvageYield> Salvage = new List<SalvageYield>();
        [Header("Typ-spezifisch")]
        public int BagSize;
        public int KitCharges;
        public ConsumableKind Consumable;
        public List<AttrValue> FoodAttributes = new List<AttrValue>();
        public float FoodDurationMinutes = 30f;
        public float HealBase;
        public LootTable ContainerLoot;

        public bool Stackable => MaxStack > 1;
        public bool IsEquippable => Type == ItemType.Weapon || Type == ItemType.Armor || Type == ItemType.Trinket;

        public ItemCategory Category
        {
            get
            {
                switch (Type)
                {
                    case ItemType.Weapon: return ItemCategory.Weapon;
                    case ItemType.Armor: return ItemCategory.Armor;
                    case ItemType.Trinket: return ItemCategory.Trinket;
                    case ItemType.Consumable: return ItemCategory.Consumable;
                    case ItemType.Material: return ItemCategory.Material;
                    case ItemType.Junk: return ItemCategory.Junk;
                    case ItemType.Bag: return ItemCategory.Bag;
                    case ItemType.SalvageKit: return ItemCategory.Tool;
                    case ItemType.Container: return ItemCategory.Container;
                }
                return ItemCategory.Any;
            }
        }

        public float WeaponStrengthAverage => (WeaponStrengthMin + WeaponStrengthMax) * 0.5f;

        public static Color RarityColor(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Junk: return new Color(0.62f, 0.62f, 0.62f);
                case ItemRarity.Basic: return Color.white;
                case ItemRarity.Fine: return new Color(0.38f, 0.6f, 1f);
                case ItemRarity.Masterwork: return new Color(0.2f, 0.75f, 0.2f);
                case ItemRarity.Rare: return new Color(1f, 0.86f, 0.15f);
                case ItemRarity.Exotic: return new Color(1f, 0.55f, 0.05f);
                case ItemRarity.Ascended: return new Color(0.98f, 0.35f, 0.6f);
                case ItemRarity.Legendary: return new Color(0.6f, 0.35f, 0.95f);
            }
            return Color.white;
        }

        public static string RarityName(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Junk: return "Plunder";
                case ItemRarity.Basic: return "Einfach";
                case ItemRarity.Fine: return "Edel";
                case ItemRarity.Masterwork: return "Meisterwerk";
                case ItemRarity.Rare: return "Selten";
                case ItemRarity.Exotic: return "Exotisch";
                case ItemRarity.Ascended: return "Aufgestiegen";
                case ItemRarity.Legendary: return "Legendär";
            }
            return r.ToString();
        }

        public static EquipSlot SlotFor(ItemDefinition d, int index = 0)
        {
            if (d == null) return EquipSlot.None;
            switch (d.Type)
            {
                case ItemType.Weapon: return index == 1 ? EquipSlot.Weapon2 : EquipSlot.Weapon1;
                case ItemType.Armor: return EquipSlot.Head + (int)d.Armor;
                case ItemType.Trinket:
                    switch (d.Trinket)
                    {
                        case TrinketKind.Back: return EquipSlot.Back;
                        case TrinketKind.Amulet: return EquipSlot.Amulet;
                        case TrinketKind.Ring: return index == 1 ? EquipSlot.Ring2 : EquipSlot.Ring1;
                        case TrinketKind.Accessory: return index == 1 ? EquipSlot.Accessory2 : EquipSlot.Accessory1;
                    }
                    break;
            }
            return EquipSlot.None;
        }
    }
}
