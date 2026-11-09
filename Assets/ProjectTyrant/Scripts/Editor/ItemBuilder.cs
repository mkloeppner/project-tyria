using System.Collections.Generic;
using ProjectTyrant.Procedural;
using TMPro;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Erzeugt Item-Definitionen (≥ 60), Beutetabellen, Händler und die Item-Datenbank – reproduzierbar per Rebuild Everything.</summary>
    public static class ItemBuilder
    {
        public const string ItemDir = DataDir + "/Items";
        public const string LootDir = DataDir + "/Loot";
        public const string VendorDir = DataDir + "/Vendors";
        static readonly List<ItemDefinition> _all = new List<ItemDefinition>();
        static readonly Dictionary<string, ItemDefinition> _byId = new Dictionary<string, ItemDefinition>();

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static ItemDefinition Item(string id, string name, ItemType type, ItemRarity rarity, int level, string desc = "")
        {
            var d = Asset<ItemDefinition>(ItemDir + "/Item_" + id + ".asset");
            d.Id = id; d.DisplayName = name; d.Type = type; d.Rarity = rarity; d.ItemLevel = level; d.RequiredLevel = level; d.Description = desc;
            d.Icon = null; d.Weapon = WeaponKind.None; d.WeaponStrengthMin = d.WeaponStrengthMax = 0; d.Defense = 0; d.Prefix = null;
            d.Attributes = new List<AttrValue>(); d.MaxStack = 1; d.Binding = ItemBinding.None; d.Salvage = new List<SalvageYield>();
            d.BagSize = 0; d.KitCharges = 0; d.Consumable = ConsumableKind.None; d.FoodAttributes = new List<AttrValue>(); d.FoodDurationMinutes = 30f; d.HealBase = 0; d.ContainerLoot = null;
            d.VendorValue = ItemStats.VendorValue(rarity, level, type);
            _all.Add(d); _byId[id] = d;
            return d;
        }

        static ItemDefinition I(string id) => _byId.TryGetValue(id, out var d) ? d : null;
        static SalvageYield Y(string id, int min, int max) => new SalvageYield { Material = I(id), Min = min, Max = max };

        [MenuItem("Project Tyrant/Build Items")]
        public static ItemDatabase BuildAll()
        {
            EnsureFolder(ItemDir); EnsureFolder(LootDir); EnsureFolder(VendorDir);
            _all.Clear(); _byId.Clear();
            BuildMaterials();
            BuildWeapons();
            BuildArmor();
            BuildTrinkets();
            BuildConsumables();
            BuildUtility();
            var db = Asset<ItemDatabase>(ItemDir + "/ItemDatabase.asset");
            db.Items = new List<ItemDefinition>(_all);
            BuildLootTables();
            BuildVendors();
            foreach (var d in _all) EditorUtility.SetDirty(d);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Items: " + _all.Count + " Definitionen.");
            return db;
        }

        public static ItemDatabase Database => AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDir + "/ItemDatabase.asset");

        // ---------------- Materialien & Plunder ----------------
        static void BuildMaterials()
        {
            void Mat(string id, string n, ItemRarity r, string desc) { var d = Item(id, n, ItemType.Material, r, 1, desc); d.MaxStack = 250; }
            Mat("mat_iron", "Eisenerzbrocken", ItemRarity.Basic, "Handwerksmaterial aus geborgenen Waffen.");
            Mat("mat_wood", "Grünholzscheit", ItemRarity.Basic, "Handwerksmaterial aus geborgenen Stäben.");
            Mat("mat_leather", "Raues Lederstück", ItemRarity.Basic, "Handwerksmaterial aus geborgener Rüstung.");
            Mat("mat_cloth", "Juteflicken", ItemRarity.Basic, "Handwerksmaterial aus geborgener Rüstung.");
            Mat("mat_dust", "Glimmerstaub", ItemRarity.Fine, "Feiner Staub aus seltener Ausrüstung.");
            Mat("mat_ember", "Glutsplitter", ItemRarity.Masterwork, "Warm glimmender Splitter aus Aschkrallen-Beute.");
            void Junk(string id, string n, int v) { var d = Item(id, n, ItemType.Junk, ItemRarity.Junk, 1, "Plunder – nur zum Verkaufen."); d.MaxStack = 250; d.VendorValue = v; }
            Junk("junk_tooth", "Abgebrochener Hauer", 6);
            Junk("junk_buckle", "Rostige Gürtelschnalle", 9);
            Junk("junk_ash", "Handvoll Aschestaub", 4);
            Junk("junk_bone", "Geschnitzter Knochenfetisch", 14);
        }

        // ---------------- Waffen ----------------
        static void Weapon(string id, string baseName, WeaponKind k, ItemRarity r, int lvl, string prefix)
        {
            var p = ItemStats.Find(prefix).Value;
            var d = Item(id, baseName + " " + p.Genitive, ItemType.Weapon, r, lvl);
            d.Weapon = k;
            float ws = DamageFormula.WeaponStrength(lvl) * ItemStats.WeaponStrengthFactor(r);
            d.WeaponStrengthMin = Mathf.RoundToInt(ws * 0.95f); d.WeaponStrengthMax = Mathf.RoundToInt(ws * 1.05f);
            ItemStats.ApplyPrefix(d, p);
            d.Binding = r >= ItemRarity.Exotic ? ItemBinding.SoulboundOnEquip : ItemBinding.None;
            d.Salvage = new List<SalvageYield> { Y(k == WeaponKind.Staff || k == WeaponKind.Longbow || k == WeaponKind.Shortbow ? "mat_wood" : "mat_iron", 1, r >= ItemRarity.Masterwork ? 3 : 2) };
            if (r >= ItemRarity.Rare) d.Salvage.Add(Y("mat_dust", 1, 2));
            if (r >= ItemRarity.Exotic) d.Salvage.Add(Y("mat_ember", 1, 1));
            d.Description = WeaponDesc(k);
        }

        static string WeaponDesc(WeaponKind k)
        {
            switch (k)
            {
                case WeaponKind.Staff: return "Zweihändiger Stab. Wächter, Elementarweber; Elite: Hainwächter (Wildhüter), Phantomtänzer (Schattenklinge).";
                case WeaponKind.Longbow: return "Zweihändiger Langbogen (Fernkampf, 30 m). Wildhüter; Elite: Glutrichter (Wächter).";
                case WeaponKind.Hammer: return "Zweihändiger Hammer (Nahkampf). Elite: Dornenwächter (Wildhüter), Katalysator (Elementarweber), Sturmherold (Wächter).";
                case WeaponKind.Rifle: return "Zweihändiges Gewehr (Fernkampf, 30 m). Elite: Sturmfalke (Wildhüter), Wegelagerer (Schattenklinge).";
                case WeaponKind.DaggerWarhorn: return "Dolch (Haupthand) und Kriegshorn. Elite: Bestienbund (Wildhüter).";
                case WeaponKind.ScepterWarhorn: return "Zepter und Kriegshorn (Nebenhand). Elite: Sturmrufer (Elementarweber).";
                case WeaponKind.SwordFocus: return "Schwert (Haupthand) und Fokus. Elite: Weber (Elementarweber).";
                case WeaponKind.ScepterDagger: return "Zepter und Dolch (Nebenhand). Elite: Glutbinder (Elementarweber).";
                case WeaponKind.ScepterPistol: return "Zepter (Haupthand) und Pistole. Elite: Schattenhüter (Schattenklinge).";
                case WeaponKind.AxeDagger: return "Wurfaxt (Haupthand) und Dolch. Elite: Klingenwirbel (Schattenklinge).";
                case WeaponKind.SwordShield: return "Schwert und Schild (Haupthand-Schild-Stil). Elite: Bollwerkmeister (Wächter).";
                case WeaponKind.ScepterTorch: return "Zepter und Fackel. Elite: Dornenschild (Wächter).";
                case WeaponKind.AxeTorch: return "Axt (Haupthand) und Fackel (Nebenhand). Wildhüter.";
                case WeaponKind.SwordWarhorn: return "Schwert (Haupthand) und Kriegshorn (Nebenhand). Wildhüter.";
                case WeaponKind.ScepterFocus: return "Zepter (Haupthand) und Fokus (Nebenhand). Elementarweber.";
                case WeaponKind.DaggerDagger: return "Zwei Dolche. Elementarweber, Schattenklinge.";
                case WeaponKind.SwordPistol: return "Schwert (Haupthand) und Pistole (Nebenhand). Schattenklinge.";
                case WeaponKind.Shortbow: return "Zweihändiger Kurzbogen (Fernkampf, 24 m). Schattenklinge.";
                default: return "Zweihändiges Großschwert (Nahkampf). Wächter.";
            }
        }

        static void BuildWeapons()
        {
            // Lauf 5 / M25: Waffen der neuen Klassen (Rekrut, selten, exotisch)
            void Line(WeaponKind k, string recruit, string rare, string exo, string pre1, string pre2)
            {
                string p = ItemIds.Prefix(k);
                Weapon(p + "_recruit", recruit, k, ItemRarity.Basic, 1, pre1);
                Weapon(p + "_fine", recruit, k, ItemRarity.Fine, 4, pre2);
                Weapon(p + "_rare", rare, k, ItemRarity.Rare, 12, pre1);
                Weapon(p + "_moor_rare", rare, k, ItemRarity.Rare, 22, pre2);
                Weapon(p + "_exo", exo, k, ItemRarity.Exotic, 30, "Berserker");
            }
            Line(WeaponKind.Longbow, "Jägerbogen", "Eibenbogen", "Bogen des Sturmkamms", "Berserker", "Soldat");
            Line(WeaponKind.AxeTorch, "Holzfälleraxt & Fackel", "Waldläuferaxt & Pechfackel", "Glutaxt & Ewige Fackel", "Berserker", "Aasfresser");
            Line(WeaponKind.SwordWarhorn, "Jagdschwert & Horn", "Heideschwert & Hirschhorn", "Klinge & Horn der Wildnis", "Soldat", "Berserker");
            Line(WeaponKind.ScepterFocus, "Lehrlingszepter & Fokus", "Runenzepter & Kristallfokus", "Zepter & Fokus der Vier", "Aasfresser", "Wüterich");
            Line(WeaponKind.DaggerDagger, "Rekrutendolche", "Schattendolche", "Dolche der Dämmerung", "Berserker", "Wüterich");
            Line(WeaponKind.SwordPistol, "Säbel & Steinschloss", "Duellsäbel & Radschloss", "Säbel & Pistole der Nacht", "Berserker", "Soldat");
            Line(WeaponKind.Shortbow, "Kurzbogen", "Reiterbogen", "Bogen des Schattenpfads", "Berserker", "Aasfresser");
            // Lauf 5 / M26: Waffen der Elite-Spezialisierungen (nur mit aktiver Spezialisierung ab Schritt 2 führbar)
            Line(WeaponKind.Hammer, "Kriegshammer", "Felsenhammer", "Hammer des Donnerfalls", "Soldat", "Berserker");
            Line(WeaponKind.Rifle, "Steinschlossgewehr", "Präzisionsgewehr", "Gewehr der Weiten Sicht", "Berserker", "Wüterich");
            Line(WeaponKind.DaggerWarhorn, "Jagddolch & Horn", "Klauendolch & Widderhorn", "Dolch & Horn des Rudels", "Berserker", "Soldat");
            Line(WeaponKind.ScepterWarhorn, "Zepter & Signalhorn", "Sturmzepter & Donnerhorn", "Zepter & Horn der Stürme", "Wüterich", "Aasfresser");
            Line(WeaponKind.SwordFocus, "Weberdegen & Fokus", "Bänderdegen & Spiegelfokus", "Degen & Fokus der Fäden", "Berserker", "Wüterich");
            Line(WeaponKind.ScepterDagger, "Zepter & Glutdolch", "Obsidianzepter & Risskralle", "Zepter & Dolch der Lavaader", "Aasfresser", "Wüterich");
            Line(WeaponKind.ScepterPistol, "Schattenzepter & Pistole", "Schleierzepter & Radschloss", "Zepter & Pistole der Tiefe", "Kleriker", "Aasfresser");
            Line(WeaponKind.AxeDagger, "Wurfaxt & Dolch", "Kreiselaxt & Stilett", "Axt & Dolch des Wirbels", "Berserker", "Soldat");
            Line(WeaponKind.SwordShield, "Gardeschwert & Schild", "Wallschwert & Turmschild", "Schwert & Schild der Feste", "Soldat", "Ritter");
            Line(WeaponKind.ScepterTorch, "Dornenzepter & Fackel", "Rankenzepter & Pechfackel", "Zepter & Fackel der Dornen", "Aasfresser", "Kleriker");
            Weapon("gs_recruit", "Rekrutenklinge", WeaponKind.Greatsword, ItemRarity.Basic, 1, "Soldat");
            Weapon("st_recruit", "Rekrutenstab", WeaponKind.Staff, ItemRarity.Basic, 1, "Aasfresser");
            Weapon("gs_fine_bers", "Schmiedeklinge", WeaponKind.Greatsword, ItemRarity.Fine, 3, "Berserker");
            Weapon("gs_fine_sold", "Schmiedeklinge", WeaponKind.Greatsword, ItemRarity.Fine, 3, "Soldat");
            Weapon("st_fine_aas", "Eschenstab", WeaponKind.Staff, ItemRarity.Fine, 3, "Aasfresser");
            Weapon("st_fine_kler", "Eschenstab", WeaponKind.Staff, ItemRarity.Fine, 3, "Kleriker");
            Weapon("gs_mw_bers", "Gardeklinge", WeaponKind.Greatsword, ItemRarity.Masterwork, 6, "Berserker");
            Weapon("gs_mw_rit", "Gardeklinge", WeaponKind.Greatsword, ItemRarity.Masterwork, 6, "Ritter");
            Weapon("st_mw_wuet", "Runenstab", WeaponKind.Staff, ItemRarity.Masterwork, 6, "Wüterich");
            Weapon("st_mw_aas", "Runenstab", WeaponKind.Staff, ItemRarity.Masterwork, 6, "Aasfresser");
            Weapon("gs_rare_bers", "Dornwacht-Zweihänder", WeaponKind.Greatsword, ItemRarity.Rare, 10, "Berserker");
            Weapon("gs_rare_wuet", "Dornwacht-Zweihänder", WeaponKind.Greatsword, ItemRarity.Rare, 10, "Wüterich");
            Weapon("st_rare_aas", "Glutholzstab", WeaponKind.Staff, ItemRarity.Rare, 10, "Aasfresser");
            Weapon("st_rare_kler", "Glutholzstab", WeaponKind.Staff, ItemRarity.Rare, 10, "Kleriker");
            Weapon("gs_exo_bers", "Aschenfürsten-Klinge", WeaponKind.Greatsword, ItemRarity.Exotic, 15, "Berserker");
            Weapon("st_exo_himm", "Sternenfall-Stab", WeaponKind.Staff, ItemRarity.Exotic, 15, "Himmlisch");
            // Lauf 4: Stufen 18–30 (Nebelmoor, Frostkamm, Glutöde)
            Weapon("gs_moor_mw", "Moorstahl-Klinge", WeaponKind.Greatsword, ItemRarity.Masterwork, 18, "Soldat");
            Weapon("st_moor_mw", "Schilfrohr-Stab", WeaponKind.Staff, ItemRarity.Masterwork, 18, "Aasfresser");
            Weapon("gs_moor_rare", "Nebelklinge", WeaponKind.Greatsword, ItemRarity.Rare, 22, "Berserker");
            Weapon("st_moor_rare", "Irrlicht-Stab", WeaponKind.Staff, ItemRarity.Rare, 22, "Kleriker");
            Weapon("gs_frost_rare", "Firnbrecher", WeaponKind.Greatsword, ItemRarity.Rare, 26, "Ritter");
            Weapon("st_frost_rare", "Eiszapfen-Stab", WeaponKind.Staff, ItemRarity.Rare, 26, "Wüterich");
            Weapon("gs_glut_exo", "Klinge des Sturmkamms", WeaponKind.Greatsword, ItemRarity.Exotic, 30, "Berserker");
            Weapon("st_glut_exo", "Stab der Glutöde", WeaponKind.Staff, ItemRarity.Exotic, 30, "Himmlisch");
        }

        // ---------------- Rüstung ----------------
        static readonly string[] PieceNames = { "Helm", "Schulterschutz", "Brustpanzer", "Handschuhe", "Beinschutz", "Stiefel" };
        static readonly string[] PieceIds = { "head", "shoulders", "chest", "hands", "legs", "feet" };

        static void Armor(string set, string setName, ArmorPiece piece, ItemRarity r, int lvl, string prefix)
        {
            var p = ItemStats.Find(prefix).Value;
            var d = Item("ar_" + set + "_" + PieceIds[(int)piece], setName + "-" + PieceNames[(int)piece] + " " + p.Genitive, ItemType.Armor, r, lvl);
            d.Armor = piece;
            d.Defense = Mathf.RoundToInt(1.2f * AttributeSet.PrimaryBase(lvl) * ItemStats.DefenseWeight(piece) * (0.85f + 0.15f * ItemStats.RarityMultiplier(r)));
            ItemStats.ApplyPrefix(d, p);
            d.Binding = r >= ItemRarity.Exotic ? ItemBinding.SoulboundOnEquip : ItemBinding.None;
            d.Salvage = new List<SalvageYield> { Y(piece == ArmorPiece.Chest || piece == ArmorPiece.Legs ? "mat_leather" : "mat_cloth", 1, 2) };
            if (r >= ItemRarity.Rare) d.Salvage.Add(Y("mat_dust", 1, 2));
            d.Description = "Schwere Rüstung.";
        }

        static void BuildArmor()
        {
            for (int i = 0; i < 6; i++)
            {
                var pc = (ArmorPiece)i;
                Armor("recruit", "Rekruten", pc, ItemRarity.Basic, 1, "Soldat");
                Armor("fine", "Wegwacht", pc, ItemRarity.Fine, 4, i % 2 == 0 ? "Berserker" : "Ritter");
                Armor("mw", "Gerstenbrunner", pc, ItemRarity.Masterwork, 7, i % 2 == 0 ? "Soldat" : "Wüterich");
                Armor("rare", "Dornwacht", pc, ItemRarity.Rare, 11, i % 2 == 0 ? "Berserker" : "Aasfresser");
                Armor("exo", "Aschenfürsten", pc, ItemRarity.Exotic, 15, "Himmlisch");
                Armor("moor", "Moorgänger", pc, ItemRarity.Masterwork, 20, i % 2 == 0 ? "Soldat" : "Aasfresser");
                Armor("frost", "Firnwacht", pc, ItemRarity.Rare, 26, i % 2 == 0 ? "Berserker" : "Ritter");
                Armor("glut", "Glutgeschmiedete", pc, ItemRarity.Exotic, 30, i % 2 == 0 ? "Berserker" : "Himmlisch");
            }
        }

        // ---------------- Schmuck ----------------
        static void Trinket(string id, string baseName, TrinketKind k, ItemRarity r, int lvl, string prefix)
        {
            var p = ItemStats.Find(prefix).Value;
            var d = Item(id, baseName + " " + p.Genitive, ItemType.Trinket, r, lvl);
            d.Trinket = k;
            ItemStats.ApplyPrefix(d, p);
            d.Binding = r >= ItemRarity.Rare ? ItemBinding.SoulboundOnEquip : ItemBinding.None;
            d.Salvage = new List<SalvageYield> { Y("mat_dust", 1, r >= ItemRarity.Rare ? 3 : 1) };
            d.Description = "Schmuckstück.";
        }

        static void BuildTrinkets()
        {
            Trinket("tr_back_fine", "Wanderumhang", TrinketKind.Back, ItemRarity.Fine, 3, "Soldat");
            Trinket("tr_amulet_mw", "Bernsteinamulett", TrinketKind.Amulet, ItemRarity.Masterwork, 6, "Berserker");
            Trinket("tr_ring_rare_bers", "Dornenring", TrinketKind.Ring, ItemRarity.Rare, 10, "Berserker");
            Trinket("tr_ring_rare_aas", "Dornenring", TrinketKind.Ring, ItemRarity.Rare, 10, "Aasfresser");
            Trinket("tr_acc_rare", "Wacht-Ohrring", TrinketKind.Accessory, ItemRarity.Rare, 10, "Ritter");
            Trinket("tr_amulet_exo", "Aschenherz-Amulett", TrinketKind.Amulet, ItemRarity.Exotic, 15, "Himmlisch");
            Trinket("tr_ring_moor", "Torfring", TrinketKind.Ring, ItemRarity.Rare, 22, "Aasfresser");
            Trinket("tr_back_frost", "Firnmantel", TrinketKind.Back, ItemRarity.Rare, 26, "Ritter");
            Trinket("tr_acc_glut", "Glutträne", TrinketKind.Accessory, ItemRarity.Exotic, 30, "Berserker");
            Trinket("tr_amulet_glut", "Sturmherz-Amulett", TrinketKind.Amulet, ItemRarity.Exotic, 30, "Himmlisch");
        }

        // ---------------- Verbrauchsgüter ----------------
        static void BuildConsumables()
        {
            void Food(string id, string n, ItemRarity r, int lvl, params AttrValue[] attrs)
            {
                var d = Item(id, n, ItemType.Consumable, r, lvl, "Nahrung: 30 Minuten Attributbonus (skaliert mit Stufe).");
                d.Consumable = ConsumableKind.Food; d.MaxStack = 250; d.FoodAttributes = new List<AttrValue>(attrs); d.FoodDurationMinutes = 30f;
            }
            Food("food_stew", "Gerstenbrunner Eintopf", ItemRarity.Fine, 1, new AttrValue(Attr.Power, 100), new AttrValue(Attr.Vitality, 70));
            Food("food_pie", "Beerenkuchen", ItemRarity.Fine, 1, new AttrValue(Attr.Precision, 100), new AttrValue(Attr.Ferocity, 70));
            Food("food_soup", "Würzige Kürbissuppe", ItemRarity.Masterwork, 5, new AttrValue(Attr.ConditionDamage, 100), new AttrValue(Attr.Expertise, 70));
            void Potion(string id, string n, ItemRarity r, float heal)
            {
                var d = Item(id, n, ItemType.Consumable, r, 1, "Heilt sofort (skaliert mit Heilkraft).");
                d.Consumable = ConsumableKind.HealPotion; d.MaxStack = 250; d.HealBase = heal;
            }
            Potion("potion_small", "Kleiner Heiltrank", ItemRarity.Basic, 1200);
            Potion("potion_big", "Großer Heiltrank", ItemRarity.Fine, 3000);
        }

        // ---------------- Taschen, Kits, Behälter ----------------
        static void BuildUtility()
        {
            void Bag(string id, string n, ItemRarity r, int size, int value)
            {
                var d = Item(id, n, ItemType.Bag, r, 1, size + " Plätze. In einen Taschenplatz legen.");
                d.BagSize = size; d.VendorValue = value;
            }
            Bag("bag_8", "Jute-Beutel", ItemRarity.Basic, 8, 40);
            Bag("bag_10", "Wolltasche", ItemRarity.Fine, 10, 120);
            Bag("bag_15", "Lederranzen", ItemRarity.Masterwork, 15, 400);
            Bag("bag_20", "Wachmeister-Rucksack", ItemRarity.Rare, 20, 1200);
            void Kit(string id, string n, ItemRarity r, int charges)
            {
                var d = Item(id, n, ItemType.SalvageKit, r, 1, charges + " Ladungen. Rechtsklick auf ein Item → „Bergen“.");
                d.KitCharges = charges; d.VendorValue = 8;
            }
            Kit("kit_crude", "Grobes Bergungskit", ItemRarity.Basic, 15);
            Kit("kit_fine", "Feines Bergungskit", ItemRarity.Fine, 25);
            void Box(string id, string n, ItemRarity r, int lvl, string desc)
            {
                var d = Item(id, n, ItemType.Container, r, 1, desc);
                d.ItemLevel = lvl; d.MaxStack = 250; d.Binding = ItemBinding.AccountBound; d.VendorValue = 0;
            }
            Box("box_pouch", "Plünderer-Beutel", ItemRarity.Basic, 1, "Doppelklick: öffnen.");
            Box("box_champion", "Champion-Beutel", ItemRarity.Rare, 10, "Garantiert mindestens ein seltenes Item. Doppelklick: öffnen.");
            Box("box_zone", "Zonenabschluss-Truhe", ItemRarity.Exotic, 15, "Belohnung für 100 % Gebietsabschluss. Doppelklick: öffnen.");
        }

        // ---------------- Beutetabellen ----------------
        static LootEntry E(ItemDefinition d, float w, int min = 1, int max = 1) => new LootEntry { Kind = LootEntryKind.Item, Item = d, Weight = w, MinCount = min, MaxCount = max };
        static LootEntry R(ItemRarity r, ItemCategory c, float w) => new LootEntry { Kind = LootEntryKind.RandomByRarity, Rarity = r, Category = c, Weight = w };
        static LootEntry None(float w) => new LootEntry { Kind = LootEntryKind.Nothing, Weight = w };

        public static LootTable Table(string id) => AssetDatabase.LoadAssetAtPath<LootTable>(LootDir + "/Loot_" + id + ".asset");

        static void BuildLootTables()
        {
            var trash = Asset<LootTable>(LootDir + "/Loot_Trash.asset");
            trash.DisplayName = "Beute"; trash.Rolls = 1; trash.MinCopper = 4; trash.MaxCopper = 18; trash.KarmaBonus = 0;
            trash.Guaranteed = new List<LootEntry>();
            trash.Weighted = new List<LootEntry> {
                E(I("junk_tooth"), 14), E(I("junk_buckle"), 10), E(I("junk_ash"), 12), E(I("junk_bone"), 4),
                E(I("mat_iron"), 8, 1, 2), E(I("mat_leather"), 8, 1, 2), E(I("mat_cloth"), 6, 1, 2), E(I("mat_wood"), 6, 1, 2),
                R(ItemRarity.Basic, ItemCategory.Any, 12), R(ItemRarity.Fine, ItemCategory.Any, 10),
                E(I("box_pouch"), 4), E(I("potion_small"), 3), None(3) };

            var vet = Asset<LootTable>(LootDir + "/Loot_Veteran.asset");
            vet.DisplayName = "Veteranen-Beute"; vet.Rolls = 2; vet.MinCopper = 30; vet.MaxCopper = 80; vet.KarmaBonus = 0;
            vet.Guaranteed = new List<LootEntry> { R(ItemRarity.Fine, ItemCategory.Any, 1) };
            vet.Weighted = new List<LootEntry> { R(ItemRarity.Masterwork, ItemCategory.Any, 20), R(ItemRarity.Fine, ItemCategory.Any, 25),
                E(I("mat_ember"), 10), E(I("mat_dust"), 15, 1, 2), E(I("junk_bone"), 20), E(I("food_stew"), 10) };

            var champBag = Asset<LootTable>(LootDir + "/Loot_ChampionBag.asset");
            champBag.DisplayName = "Champion-Beutel"; champBag.Rolls = 2; champBag.MinCopper = 100; champBag.MaxCopper = 300; champBag.KarmaBonus = 0;
            champBag.Guaranteed = new List<LootEntry> { R(ItemRarity.Rare, ItemCategory.Any, 1) };
            champBag.Weighted = new List<LootEntry> { R(ItemRarity.Masterwork, ItemCategory.Any, 40), R(ItemRarity.Exotic, ItemCategory.Any, 5),
                E(I("mat_ember"), 25, 1, 3), E(I("bag_15"), 10), E(I("food_soup"), 20) };
            I("box_champion").ContainerLoot = champBag;

            var champ = Asset<LootTable>(LootDir + "/Loot_Champion.asset");
            champ.DisplayName = "Champion-Beute"; champ.Rolls = 1; champ.MinCopper = 150; champ.MaxCopper = 400; champ.KarmaBonus = 150;
            champ.Guaranteed = new List<LootEntry> { E(I("box_champion"), 1) };
            champ.Weighted = new List<LootEntry> { R(ItemRarity.Masterwork, ItemCategory.Any, 60), E(I("mat_ember"), 40, 1, 2) };

            var pouch = Asset<LootTable>(LootDir + "/Loot_Pouch.asset");
            pouch.DisplayName = "Plünderer-Beutel"; pouch.Rolls = 2; pouch.MinCopper = 10; pouch.MaxCopper = 40;
            pouch.Guaranteed = new List<LootEntry>();
            pouch.Weighted = new List<LootEntry> { R(ItemRarity.Basic, ItemCategory.Any, 30), R(ItemRarity.Fine, ItemCategory.Any, 20), E(I("mat_iron"), 15, 1, 3), E(I("mat_leather"), 15, 1, 3), E(I("junk_bone"), 20) };
            I("box_pouch").ContainerLoot = pouch;

            var zone = Asset<LootTable>(LootDir + "/Loot_ZoneChest.asset");
            zone.DisplayName = "Zonenabschluss-Truhe"; zone.Rolls = 1; zone.MinCopper = 2000; zone.MaxCopper = 2000; zone.KarmaBonus = 1000;
            zone.Guaranteed = new List<LootEntry> { R(ItemRarity.Exotic, ItemCategory.Any, 1), R(ItemRarity.Rare, ItemCategory.Any, 1), R(ItemRarity.Rare, ItemCategory.Any, 1), E(I("mat_ember"), 1, 5, 5), E(I("mat_dust"), 1, 5, 5) };
            zone.Weighted = new List<LootEntry> { E(I("bag_20"), 1) };
            I("box_zone").ContainerLoot = zone;

            foreach (var t in new[] { trash, vet, champBag, champ, pouch, zone }) EditorUtility.SetDirty(t);

            void Assign(string archetype, LootTable t) { var a = DataBuilder.Archetype(archetype); if (a != null) { a.Loot = t; EditorUtility.SetDirty(a); } }
            Assign("Trash_Pluenderer", trash);
            Assign("Veteran_Brecher", vet);
            Assign("Champion_Grolmok", champ);
        }

        // ---------------- Händler ----------------
        static VendorEntry V(string id, int copper, int count = 1, int karma = 0) => new VendorEntry { Item = I(id), PriceCopper = copper, Count = count, PriceKarma = karma };

        public static VendorDefinition Vendor(string id) => AssetDatabase.LoadAssetAtPath<VendorDefinition>(VendorDir + "/Vendor_" + id + ".asset");

        static void BuildVendors()
        {
            var arms = Asset<VendorDefinition>(VendorDir + "/Vendor_Ruestmeisterin.asset");
            arms.Id = "ruestmeisterin"; arms.DisplayName = "Rüstmeisterin Hella"; arms.Kind = VendorKind.WeaponsArmor;
            arms.Stock = new List<VendorEntry> {
                V("gs_fine_bers", 180), V("gs_fine_sold", 180), V("st_fine_aas", 180), V("st_fine_kler", 180),
                V("ar_fine_head", 90), V("ar_fine_shoulders", 90), V("ar_fine_chest", 160), V("ar_fine_hands", 90), V("ar_fine_legs", 130), V("ar_fine_feet", 100),
                V("tr_back_fine", 150), V("gs_mw_bers", 900), V("st_mw_wuet", 900) };
            var gen = Asset<VendorDefinition>(VendorDir + "/Vendor_Kraemer.asset");
            gen.Id = "kraemer"; gen.DisplayName = "Krämer Ottfried"; gen.Kind = VendorKind.General;
            gen.Stock = new List<VendorEntry> {
                V("kit_crude", 32), V("kit_fine", 128), V("potion_small", 24, 5), V("potion_big", 80, 3),
                V("food_stew", 40, 5), V("food_pie", 40, 5), V("bag_8", 200), V("bag_10", 640) };
            EditorUtility.SetDirty(arms); EditorUtility.SetDirty(gen);
        }

        // ---------------- Szene ----------------
        public static void PlaceVendors(Transform parent)
        {
            Place(parent, Vendor("Ruestmeisterin"), ZoneLayout.Hub.Center + new Vector2(-9f, 4f), 90f, new Color(0.55f, 0.25f, 0.2f));
            Place(parent, Vendor("Kraemer"), ZoneLayout.Hub.Center + new Vector2(-9f, -2f), 90f, new Color(0.3f, 0.45f, 0.25f));
        }

        public static VendorNpc Place(Transform parent, VendorDefinition v, Vector2 xz, float yaw, Color robe) => Place(parent, v, ZoneBuilder.G(xz), yaw, robe);

        /// <summary>Lauf 4: Händler an einer Weltposition (Biom-Zonen).</summary>
        public static VendorNpc Place(Transform parent, VendorDefinition v, Vector3 pos, float yaw, Color robe)
        {
            if (v == null) return null;
            var go = Child(parent, "Vendor_" + v.Id, pos);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var robeMat = PTEditorUtil.Mat("VendorRobe_" + v.Id, robe);
            var skin = PTEditorUtil.Mat("NpcSkin", new Color(0.9f, 0.72f, 0.58f));
            CharacterRigs.Npc(go.transform, robe, v.Id);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.55f, 0.9f), new Vector3(1.6f, 0.9f, 0.7f), PTEditorUtil.Mat("VendorStall", new Color(0.5f, 0.35f, 0.2f)), "Stall", null, true);
            var plate = new GameObject("Nameplate");
            plate.transform.SetParent(go.transform, false);
            plate.transform.localPosition = new Vector3(0, 2.7f, 0);
            var tmp = plate.AddComponent<TextMeshPro>();
            tmp.text = "<b>" + v.DisplayName + "</b>\n<size=70%>" + (v.Kind == VendorKind.WeaponsArmor ? "Waffen & Rüstung" : v.Kind == VendorKind.Karma ? "Karma-Händler" : "Allgemeinwaren") + "</size>";
            tmp.fontSize = 1.8f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color(1f, 0.9f, 0.55f);
            tmp.rectTransform.sizeDelta = new Vector2(6, 1.5f);
            plate.AddComponent<Billboard>();
            var npc = go.AddComponent<VendorNpc>();
            npc.Vendor = v; npc.PromptText = "Sprechen"; npc.Range = 3.2f;
            MapMarkerBuilder.Add(go, MapMarkerKind.Vendor, v.DisplayName, v.Kind == VendorKind.WeaponsArmor ? "Waffen & Rüstung" : "Allgemeinwaren");
            return npc;
        }

        public static void ConfigureProfile(PlayerProfile p)
        {
            p.MountDefinitions = MountBuilder.LoadAll();
            p.MasteryLines = MasteryBuilder.LoadAll();
            p.GreatswordSet = AssetDatabase.LoadAssetAtPath<WeaponSet>(DataDir + "/Weapons/Weapon_Glutklinge.asset");
            p.StaffSet = AssetDatabase.LoadAssetAtPath<WeaponSet>(DataDir + "/Weapons/Weapon_Sturmruf.asset");
            p.StarterEquipment = new[] { "gs_recruit", "st_recruit", "ar_recruit_head", "ar_recruit_shoulders", "ar_recruit_chest", "ar_recruit_hands", "ar_recruit_legs", "ar_recruit_feet" };
            p.StarterInventory = new[] { "kit_crude:1", "potion_small:5", "food_stew:3", "bag_8:1" };
        }
    }
}
