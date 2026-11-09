using System;

namespace ProjectTyrant
{
    /// <summary>Laufzeit-Item: Definition, Anzahl, Bindung, Ladungen (Bergungs-Sets).</summary>
    [Serializable]
    public class ItemInstance
    {
        public ItemDefinition Def;
        public int Count = 1;
        public bool Bound;
        public int Charges;

        public ItemInstance(ItemDefinition def, int count = 1, bool bound = false)
        {
            Def = def; Count = count; Bound = bound || (def != null && def.Binding == ItemBinding.AccountBound);
            Charges = def != null ? def.KitCharges : 0;
        }

        public bool CanStackWith(ItemInstance o) => o != null && Def != null && o.Def == Def && Def.Stackable && Bound == o.Bound;
        public int Space => Def == null ? 0 : Def.MaxStack - Count;
        public ItemInstance CloneWithCount(int n) => new ItemInstance(Def, n, Bound) { Charges = Charges };
        public override string ToString() => Def == null ? "?" : Def.DisplayName + (Count > 1 ? " ×" + Count : "");
    }

    /// <summary>Attribut-Präfixe (Haupt ≈ 2 × Neben) und Wertebudgets nach Item-Level und Seltenheit.</summary>
    public static class ItemStats
    {
        public struct Prefix { public string Name, Genitive; public Attr Major; public Attr[] Minors; public bool Celestial; }

        public static readonly Prefix[] Prefixes =
        {
            new Prefix { Name = "Berserker", Genitive = "des Berserkers", Major = Attr.Power, Minors = new[] { Attr.Precision, Attr.Ferocity } },
            new Prefix { Name = "Ritter", Genitive = "des Ritters", Major = Attr.Toughness, Minors = new[] { Attr.Power, Attr.Precision } },
            new Prefix { Name = "Soldat", Genitive = "des Soldaten", Major = Attr.Power, Minors = new[] { Attr.Toughness, Attr.Vitality } },
            new Prefix { Name = "Kleriker", Genitive = "des Klerikers", Major = Attr.HealingPower, Minors = new[] { Attr.Power, Attr.Toughness } },
            new Prefix { Name = "Wüterich", Genitive = "des Wüterichs", Major = Attr.Precision, Minors = new[] { Attr.Power, Attr.ConditionDamage } },
            new Prefix { Name = "Aasfresser", Genitive = "des Aasfressers", Major = Attr.ConditionDamage, Minors = new[] { Attr.Power, Attr.Vitality } },
            new Prefix { Name = "Himmlisch", Genitive = "des Himmlischen", Major = Attr.Power, Minors = new Attr[0], Celestial = true },
        };

        public static Prefix? Find(string name) { foreach (var p in Prefixes) if (p.Name == name) return p; return null; }

        public static float RarityMultiplier(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Junk: return 0f;
                case ItemRarity.Basic: return 0.4f;
                case ItemRarity.Fine: return 0.6f;
                case ItemRarity.Masterwork: return 0.8f;
                case ItemRarity.Rare: return 1.0f;
                case ItemRarity.Exotic: return 1.15f;
                default: return 1.25f;
            }
        }

        public static float WeaponStrengthFactor(ItemRarity r)
        {
            switch (r) { case ItemRarity.Basic: return 0.9f; case ItemRarity.Fine: return 0.95f; case ItemRarity.Masterwork: return 1f; case ItemRarity.Rare: return 1.03f; case ItemRarity.Exotic: return 1.06f; default: return 1.1f; }
        }

        /// <summary>Anteil eines Slots am Gesamt-Attributbudget der Ausrüstung.</summary>
        public static float SlotWeight(ItemDefinition d)
        {
            switch (d.Type)
            {
                case ItemType.Weapon: return 0.25f;
                case ItemType.Armor:
                    switch (d.Armor) { case ArmorPiece.Chest: return 0.12f; case ArmorPiece.Legs: return 0.08f; case ArmorPiece.Head: return 0.06f; default: return 0.05f; }
                case ItemType.Trinket:
                    switch (d.Trinket) { case TrinketKind.Back: return 0.05f; case TrinketKind.Amulet: return 0.12f; case TrinketKind.Ring: return 0.09f; default: return 0.08f; }
            }
            return 0f;
        }

        public static float DefenseWeight(ArmorPiece p)
        {
            switch (p) { case ArmorPiece.Chest: return 0.3f; case ArmorPiece.Legs: return 0.2f; case ArmorPiece.Feet: return 0.14f; default: return 0.12f; }
        }

        /// <summary>Attributbudget (Hauptattribut = 50 %, Nebenattribute je 25 %).</summary>
        public static float Budget(ItemDefinition d) => SlotWeight(d) * AttributeSet.PrimaryBase(d.ItemLevel) * RarityMultiplier(d.Rarity);

        public static void ApplyPrefix(ItemDefinition d, Prefix p)
        {
            d.Prefix = p.Name;
            d.Attributes.Clear();
            float b = Budget(d);
            if (b <= 0f) return;
            if (p.Celestial)
            {
                int each = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(b * 0.16f));
                for (int i = 0; i < AttributeAccumulator.Count; i++) d.Attributes.Add(new AttrValue((Attr)i, each));
                return;
            }
            d.Attributes.Add(new AttrValue(p.Major, UnityEngine.Mathf.Max(2, UnityEngine.Mathf.RoundToInt(b * 0.5f))));
            foreach (var m in p.Minors) d.Attributes.Add(new AttrValue(m, UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(b * 0.25f))));
        }

        public static int RequiredLevel(ItemRarity r)
        {
            switch (r) { case ItemRarity.Basic: return 1; case ItemRarity.Fine: return 3; case ItemRarity.Masterwork: return 6; case ItemRarity.Rare: return 10; default: return 15; }
        }

        public static int VendorValue(ItemRarity r, int level, ItemType t)
        {
            float baseV = t == ItemType.Junk ? 8f : t == ItemType.Material ? 4f : t == ItemType.Consumable ? 10f : 12f;
            float rm = r == ItemRarity.Junk ? 1f : 1f + (int)r * 0.9f;
            return UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(baseV * rm * (1f + level * 0.25f)));
        }
    }
}
