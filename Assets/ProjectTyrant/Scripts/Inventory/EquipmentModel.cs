using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Ausrüstung: 2 Waffensets, 6 Rüstungsteile, 6 Schmuckplätze. Prüft Level/Typ, bindet beim Anlegen, liefert Attribute.</summary>
    public class EquipmentModel : IAttributeSource
    {
        readonly Dictionary<EquipSlot, ItemInstance> _slots = new Dictionary<EquipSlot, ItemInstance>();
        public event Action Changed;

        public static readonly EquipSlot[] AllSlots =
        {
            EquipSlot.Weapon1, EquipSlot.Weapon2, EquipSlot.Head, EquipSlot.Shoulders, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Legs, EquipSlot.Feet,
            EquipSlot.Back, EquipSlot.Amulet, EquipSlot.Ring1, EquipSlot.Ring2, EquipSlot.Accessory1, EquipSlot.Accessory2
        };

        public ItemInstance Get(EquipSlot s) => _slots.TryGetValue(s, out var it) ? it : null;

        public static string SlotName(EquipSlot s)
        {
            switch (s)
            {
                case EquipSlot.Weapon1: return "Waffenset 1"; case EquipSlot.Weapon2: return "Waffenset 2";
                case EquipSlot.Head: return "Kopf"; case EquipSlot.Shoulders: return "Schultern"; case EquipSlot.Chest: return "Brust";
                case EquipSlot.Hands: return "Hände"; case EquipSlot.Legs: return "Beine"; case EquipSlot.Feet: return "Füße";
                case EquipSlot.Back: return "Rücken"; case EquipSlot.Amulet: return "Amulett"; case EquipSlot.Ring1: return "Ring 1"; case EquipSlot.Ring2: return "Ring 2";
                case EquipSlot.Accessory1: return "Accessoire 1"; case EquipSlot.Accessory2: return "Accessoire 2";
            }
            return s.ToString();
        }

        public static bool Fits(ItemDefinition d, EquipSlot s)
        {
            if (d == null) return false;
            switch (d.Type)
            {
                case ItemType.Weapon: return s == EquipSlot.Weapon1 || s == EquipSlot.Weapon2;
                case ItemType.Armor: return s == EquipSlot.Head + (int)d.Armor;
                case ItemType.Trinket:
                    switch (d.Trinket)
                    {
                        case TrinketKind.Back: return s == EquipSlot.Back;
                        case TrinketKind.Amulet: return s == EquipSlot.Amulet;
                        case TrinketKind.Ring: return s == EquipSlot.Ring1 || s == EquipSlot.Ring2;
                        case TrinketKind.Accessory: return s == EquipSlot.Accessory1 || s == EquipSlot.Accessory2;
                    }
                    break;
            }
            return false;
        }

        /// <summary>Bevorzugter Zielplatz (bei Ringen/Accessoires/Waffen der freie).</summary>
        public EquipSlot PreferredSlot(ItemDefinition d, int activeWeaponSet = 0)
        {
            var a = ItemDefinition.SlotFor(d, 0); var b = ItemDefinition.SlotFor(d, 1);
            if (d.Type == ItemType.Weapon) return activeWeaponSet == 1 ? EquipSlot.Weapon2 : EquipSlot.Weapon1;
            if (a != b && Get(a) != null && Get(b) == null) return b;
            return a;
        }

        /// <summary>Lauf 5 / M25: Klassenbindung der Waffentypen (vom PlayerProfile gesetzt; null = alles erlaubt).</summary>
        public static System.Func<WeaponKind, bool> WeaponAllowed;

        public bool CanEquip(ItemDefinition d, EquipSlot s, int playerLevel, out string reason)
        {
            reason = null;
            if (d == null || !d.IsEquippable) { reason = "Nicht ausrüstbar"; return false; }
            if (!Fits(d, s)) { reason = "Passt nicht in " + SlotName(s); return false; }
            if (d.RequiredLevel > playerLevel) { reason = "Benötigt Stufe " + d.RequiredLevel; return false; }
            if (d.Type == ItemType.Weapon && WeaponAllowed != null && !WeaponAllowed(d.Weapon)) { reason = "Deine Klasse kann diese Waffe nicht führen"; return false; }
            return true;
        }

        /// <summary>Legt ein Item an und gibt das vorher angelegte zurück. Seelengebundene Items werden gebunden.</summary>
        public bool Equip(ItemInstance it, EquipSlot s, int playerLevel, out ItemInstance previous, out string reason)
        {
            previous = null;
            if (it == null) { reason = "Kein Item"; return false; }
            if (!CanEquip(it.Def, s, playerLevel, out reason)) return false;
            previous = Get(s);
            if (it.Def.Binding == ItemBinding.SoulboundOnEquip) it.Bound = true;
            _slots[s] = it;
            Changed?.Invoke();
            return true;
        }

        public ItemInstance Unequip(EquipSlot s)
        {
            var it = Get(s);
            if (it == null) return null;
            _slots.Remove(s);
            Changed?.Invoke();
            return it;
        }

        public void SetDirect(EquipSlot s, ItemInstance it) { if (it == null) _slots.Remove(s); else _slots[s] = it; Changed?.Invoke(); }

        public void Contribute(AttributeAccumulator acc, int level)
        {
            foreach (var kv in _slots)
            {
                var d = kv.Value?.Def; if (d == null) continue;
                float k = WorldModel.ItemScale(d.ItemLevel, level);   // Herabstufung: Ausrüstung proportional
                foreach (var a in d.Attributes) acc.Flat[(int)a.Attr] += a.Value * k;
                acc.Defense += d.Defense * k;
            }
        }

        public float TotalDefense { get { float t = 0; foreach (var kv in _slots) if (kv.Value?.Def != null) t += kv.Value.Def.Defense; return t; } }

        public WeaponKind WeaponKindOf(int set) { var it = Get(set == 1 ? EquipSlot.Weapon2 : EquipSlot.Weapon1); return it?.Def != null ? it.Def.Weapon : WeaponKind.None; }

        /// <summary>Waffenstärke der aktiven Waffe (Mittelwert der Spanne), 0 = keine Waffe.</summary>
        public float WeaponStrength(int set) { var it = Get(set == 1 ? EquipSlot.Weapon2 : EquipSlot.Weapon1); return it?.Def != null ? it.Def.WeaponStrengthAverage : 0f; }

        public IEnumerable<KeyValuePair<EquipSlot, ItemInstance>> Equipped => _slots;
        public void Clear() { _slots.Clear(); Changed?.Invoke(); }
    }

    /// <summary>Nahrung: Attributbonus für 30 Minuten (eine Nahrung zur Zeit).</summary>
    public class FoodModel : IAttributeSource
    {
        public ItemDefinition Food;
        public float Remaining;
        public event Action Changed;
        public bool Active => Food != null && Remaining > 0f;
        public void Eat(ItemDefinition d) { Food = d; Remaining = d.FoodDurationMinutes * 60f; Changed?.Invoke(); }
        public void Tick(float dt) { if (!Active) return; Remaining -= dt; if (Remaining <= 0f) { Food = null; Changed?.Invoke(); } }
        public void Contribute(AttributeAccumulator acc, int level)
        {
            if (!Active) return;
            foreach (var a in Food.FoodAttributes) acc.Flat[(int)a.Attr] += a.Value * UnityEngine.Mathf.Max(0.2f, AttributeSet.LevelScale(level));
        }
    }
}
