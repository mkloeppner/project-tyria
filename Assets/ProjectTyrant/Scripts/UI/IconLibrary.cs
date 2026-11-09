using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Prozedural gezeichnete UI-Sprites (Effekte, Items, Marker, Rahmen). Vom Builder erzeugt.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Icon Library", fileName = "IconLibrary")]
    public class IconLibrary : ScriptableObject
    {
        [Serializable] public struct Entry { public string Id; public Sprite Sprite; }
        public List<Entry> Entries = new List<Entry>();
        Dictionary<string, Sprite> _map;

        public Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null || _map.Count != Entries.Count)
            {
                _map = new Dictionary<string, Sprite>();
                foreach (var e in Entries) if (!string.IsNullOrEmpty(e.Id) && e.Sprite != null) _map[e.Id] = e.Sprite;
            }
            return _map.TryGetValue(id, out var s) ? s : null;
        }

        public static string EffectId(EffectType t) => "effect_" + t;
        public static string ItemId(ItemDefinition d)
        {
            if (d == null) return "item_unknown";
            switch (d.Type)
            {
                case ItemType.Weapon: return d.Weapon == WeaponKind.Staff ? "item_staff" : "item_greatsword";
                case ItemType.Armor: return "item_armor_" + d.Armor.ToString().ToLowerInvariant();
                case ItemType.Trinket: return "item_trinket_" + d.Trinket.ToString().ToLowerInvariant();
                case ItemType.Consumable: return d.Consumable == ConsumableKind.HealPotion ? "item_potion" : d.Consumable == ConsumableKind.OpenContainer ? "item_container" : "item_food";
                case ItemType.Material: return "item_material";
                case ItemType.Junk: return "item_junk";
                case ItemType.Bag: return "item_bag";
                case ItemType.SalvageKit: return "item_kit";
                case ItemType.Container: return "item_container";
            }
            return "item_unknown";
        }
    }
}
