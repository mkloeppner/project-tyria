using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Inventar (I) wie GW2: Rucksack + 4 Taschenplätze, Stapel, Drag-and-drop, Alt+Ziehen teilt, Rechtsklick-Menü, Aufräumen, Geldbörse.</summary>
    public class InventoryWindow
    {
        public readonly UiWindow Window;
        readonly UIController _ui;
        readonly RectTransform _content;
        readonly TextMeshProUGUI _wallet;
        readonly Button _sellJunk;
        readonly List<UiItemSlot> _slots = new List<UiItemSlot>();
        readonly UiItemSlot[] _bagSlots = new UiItemSlot[InventoryModel.BagSlots];
        bool _dirty = true;
        InventoryModel _bound;

        public InventoryWindow(WindowManager mgr, UIController ui)
        {
            _ui = ui;
            Window = new UiWindow(mgr, "inventory", "Inventar", new Vector2(540, 660), new Vector2(600, 20));
            var c = Window.Content;
            var bagRow = UiFactory.Rect("BagSlots", c).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 48));
            var lbl = UiFactory.Text("BagLabel", bagRow, "Taschen:", 15, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f));
            lbl.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(80, 0));
            for (int i = 0; i < InventoryModel.BagSlots; i++)
            {
                int bagSlot = i + 1;
                var s = UiItemSlot.Create(bagRow, 44, SlotKind.BagSlot, "Tasche");
                ((RectTransform)s.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(84 + i * 50, 0), new Vector2(44, 44));
                s.Bag = bagSlot;
                s.GetItem = () => PlayerProfile.Instance?.Inventory.GetBagItem(bagSlot);
                _bagSlots[i] = s;
            }
            // Scrollbereich
            var view = UiFactory.Image("View", c, new Color(0, 0, 0, 0.25f), null, true);
            view.rectTransform.Stretch(0, 0, 54, 82);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            _content = UiFactory.Rect("Content", view.transform);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1); _content.sizeDelta = new Vector2(0, 100);
            var v = _content.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(6, 6, 6, 6); v.spacing = 6; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 30f; scroll.viewport = view.rectTransform;

            _wallet = UiFactory.Text("Wallet", c, "", 17, TextAlignmentOptions.Left, UiFactory.AccentColor);
            _wallet.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 44), new Vector2(0, 30));
            var sort = UiFactory.Button("Sort", c, "Inventar aufräumen", () => { PlayerProfile.Instance?.Inventory.Sort(); }, 15);
            ((RectTransform)sort.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(200, 34));
            _sellJunk = UiFactory.Button("SellJunk", c, "Plunder verkaufen", SellJunk, 15);
            ((RectTransform)_sellJunk.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(200, 34));
            Window.Opened += () => _dirty = true;
            GameEventBus.ProfileChanged += () => _dirty = true;
        }

        void SellJunk()
        {
            var p = PlayerProfile.Instance;
            if (p == null || p.ActiveVendor == null) { GameEventBus.Notify("Nur bei einem Händler möglich", new Color(1f, 0.7f, 0.4f)); return; }
            long g = p.ActiveVendor.SellJunk(p.Inventory, p.Wallet);
            GameEventBus.Notify("Plunder verkauft: " + WalletModel.Format(g), UiFactory.AccentColor);
        }

        public void MarkDirty() => _dirty = true;

        public void Refresh()
        {
            var p = PlayerProfile.Instance;
            if (p == null) return;
            if (_bound != p.Inventory) { _bound = p.Inventory; _dirty = true; }
            _sellJunk.interactable = p.ActiveVendor != null;
            if (!_dirty) return;
            _dirty = false;
            Rebuild(p.Inventory);
            foreach (var b in _bagSlots) b.Refresh();
            _wallet.text = "Geldbörse: " + WalletModel.Format(p.Wallet.Copper) + "     Karma: " + p.Wallet.Karma.ToString("N0") + "     Frei: " + p.Inventory.FreeSlots + "/" + p.Inventory.TotalSlots;
        }

        void Rebuild(InventoryModel inv)
        {
            // Slots wiederverwenden, Struktur nur bei geänderter Taschenbelegung neu bauen
            int needed = 0;
            for (int b = 0; b < inv.BagCount; b++) { var bag = inv.GetBag(b); if (bag != null) needed += bag.Length; }
            bool structureChanged = needed != _slots.Count;
            if (structureChanged)
            {
                UiFactory.Clear(_content); _slots.Clear();
                for (int b = 0; b < inv.BagCount; b++)
                {
                    var bag = inv.GetBag(b); if (bag == null) continue;
                    string name = b == 0 ? "Rucksack" : inv.GetBagItem(b).Def.DisplayName;
                    var head = UiFactory.Text("Head" + b, _content, name + "  (" + bag.Length + " Plätze)", 15, TextAlignmentOptions.Left, new Color(0.85f, 0.75f, 0.55f));
                    UiFactory.Layout(head, 20);
                    var grid = UiFactory.Rect("Grid" + b, _content);
                    var g = grid.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(46, 46); g.spacing = new Vector2(4, 4); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 10;
                    UiFactory.Layout(grid, Mathf.CeilToInt(bag.Length / 10f) * 50f);
                    for (int s = 0; s < bag.Length; s++)
                    {
                        var slot = UiItemSlot.Create(grid, 46, SlotKind.Inventory);
                        int bi = b, si = s;
                        slot.Bag = bi; slot.Index = si;
                        slot.GetItem = () => PlayerProfile.Instance?.Inventory.Get(bi, si);
                        _slots.Add(slot);
                    }
                }
            }
            foreach (var s in _slots) s.Refresh();
        }
    }

    /// <summary>Zentrale Logik für Drag-and-drop und Kontextmenü aller Item-Plätze.</summary>
    public static class ItemActions
    {
        public static void HandleDrop(UiItemSlot src, UiItemSlot dst, bool alt)
        {
            var p = PlayerProfile.Instance; var ui = UIController.Instance;
            if (p == null) return;
            var inv = p.Inventory;
            if (src.Kind == SlotKind.Inventory && dst.Kind == SlotKind.Inventory)
            {
                var it = inv.Get(src.Bag, src.Index);
                if (it == null) return;
                if (alt && it.Count > 1 && inv.Get(dst.Bag, dst.Index) == null)
                {
                    int sb = src.Bag, ss = src.Index, db = dst.Bag, ds = dst.Index;
                    ui.Dialog.AskAmount("Stapel teilen", it.Def.DisplayName + " (" + it.Count + ")", it.Count - 1, it.Count / 2, n => inv.Split(sb, ss, n, db, ds));
                }
                else inv.Move(src.Bag, src.Index, dst.Bag, dst.Index);
            }
            else if (src.Kind == SlotKind.Inventory && dst.Kind == SlotKind.Equipment) p.EquipFromInventory(src.Bag, src.Index, dst.Equip);
            else if (src.Kind == SlotKind.Equipment && dst.Kind == SlotKind.Inventory)
            {
                var eq = p.Equipment.Get(src.Equip);
                if (eq == null) return;
                if (inv.Get(dst.Bag, dst.Index) == null) { p.Equipment.Unequip(src.Equip); inv.Set(dst.Bag, dst.Index, eq); }
                else p.EquipFromInventory(dst.Bag, dst.Index, src.Equip);
            }
            else if (src.Kind == SlotKind.Inventory && dst.Kind == SlotKind.BagSlot)
            {
                if (!inv.EquipBag(dst.Bag, src.Bag, src.Index, out string err)) GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f));
            }
            else if (src.Kind == SlotKind.BagSlot && dst.Kind == SlotKind.Inventory)
            {
                if (!inv.UnequipBag(src.Bag, out string err)) GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f));
            }
            else if (src.Kind == SlotKind.Inventory && dst.Kind == SlotKind.VendorSell) Sell(src.Bag, src.Index);
        }

        public static void Sell(int bag, int slot)
        {
            var p = PlayerProfile.Instance;
            if (p == null || p.ActiveVendor == null) { GameEventBus.Notify("Nur bei einem Händler möglich", new Color(1f, 0.7f, 0.4f)); return; }
            var it = p.Inventory.Get(bag, slot);
            if (it == null) return;
            if (p.ActiveVendor.Sell(p.Inventory, bag, slot, p.Wallet, out long price))
                GameEventBus.Notify("Verkauft: " + it.Def.DisplayName + " für " + WalletModel.Format(price), UiFactory.AccentColor);
            else GameEventBus.Notify("Nicht verkäuflich", new Color(1f, 0.7f, 0.4f));
        }

        public static void ContextMenu(UiItemSlot s)
        {
            var p = PlayerProfile.Instance; var ui = UIController.Instance;
            if (p == null || ui == null) return;
            var pos = InputManager.Instance != null ? InputManager.Instance.PointerPosition : (Vector2)Input.mousePosition;
            if (s.Kind == SlotKind.Equipment)
            {
                var eq = p.Equipment.Get(s.Equip); if (eq == null) return;
                var slot = s.Equip;
                ui.ContextMenu.Open(pos, ui.CanvasScale, ("Ablegen", () => p.Unequip(slot), true));
                return;
            }
            if (s.Kind == SlotKind.BagSlot)
            {
                int bs = s.Bag;
                ui.ContextMenu.Open(pos, ui.CanvasScale, ("Tasche abnehmen", () => { if (!p.Inventory.UnequipBag(bs, out string e)) GameEventBus.Notify(e, new Color(1f, 0.6f, 0.4f)); }, p.Inventory.GetBagItem(bs) != null));
                return;
            }
            if (s.Kind != SlotKind.Inventory) return;
            var it = p.Inventory.Get(s.Bag, s.Index);
            if (it == null) return;
            int b = s.Bag, i = s.Index;
            string use = it.Def.IsEquippable ? "Anlegen" : it.Def.Type == ItemType.Bag ? "Tasche anlegen" : it.Def.Type == ItemType.Container || it.Def.Consumable == ConsumableKind.OpenContainer ? "Öffnen" : "Benutzen";
            bool usable = it.Def.IsEquippable || it.Def.Type == ItemType.Consumable || it.Def.Type == ItemType.Container || it.Def.Type == ItemType.Bag;
            bool salvage = SalvageModel.CanSalvage(it.Def) && SalvageModel.FindKit(p.Inventory, out _, out _) != null;
            ui.ContextMenu.Open(pos, ui.CanvasScale,
                (use, () => p.UseItem(b, i), usable),
                ("Bergen", () => p.Salvage(b, i), salvage),
                ("Verkaufen", () => Sell(b, i), p.ActiveVendor != null && it.Def.VendorValue > 0),
                ("Stapel teilen", () => SplitDialog(b, i), it.Count > 1),
                ("Zerstören", () => ui.Dialog.Confirm("Zerstören", "„" + it.Def.DisplayName + "“ wirklich zerstören?", () => { UiFactory.Sound("DestroyConfirm"); p.Inventory.Remove(b, i); }, "Zerstören"), true));
        }

        static void SplitDialog(int b, int i)
        {
            var p = PlayerProfile.Instance; var ui = UIController.Instance;
            var it = p.Inventory.Get(b, i); if (it == null || it.Count < 2) return;
            ui.Dialog.AskAmount("Stapel teilen", it.Def.DisplayName + " (" + it.Count + ")", it.Count - 1, it.Count / 2, n =>
            {
                for (int bb = 0; bb < p.Inventory.BagCount; bb++)
                {
                    var bag = p.Inventory.GetBag(bb); if (bag == null) continue;
                    for (int ss = 0; ss < bag.Length; ss++) if (bag[ss] == null) { p.Inventory.Split(b, i, n, bb, ss); return; }
                }
                GameEventBus.Notify("Kein freier Platz", new Color(1f, 0.6f, 0.4f));
            });
        }

        public static void DoubleClick(UiItemSlot s)
        {
            var p = PlayerProfile.Instance; if (p == null) return;
            if (s.Kind == SlotKind.Inventory) p.UseItem(s.Bag, s.Index);
            else if (s.Kind == SlotKind.Equipment) p.Unequip(s.Equip);
        }
    }
}
