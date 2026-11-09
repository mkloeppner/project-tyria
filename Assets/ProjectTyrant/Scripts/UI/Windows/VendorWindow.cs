using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Händlerfenster: Kaufen · Verkaufen · Rückkauf (letzte 10). Items aus dem Inventar hierher ziehen = verkaufen.</summary>
    public class VendorWindow
    {
        public readonly UiWindow Window;
        readonly RectTransform _list, _tabBar;
        readonly TextMeshProUGUI _wallet;
        int _tab;
        VendorNpc _npc;
        bool _dirty = true;
        readonly Button[] _tabs = new Button[3];

        public VendorWindow(WindowManager mgr)
        {
            Window = new UiWindow(mgr, "vendor", "Händler", new Vector2(560, 640), new Vector2(-330, 20));
            var c = Window.Content;
            _tabBar = UiFactory.Rect("Tabs", c).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 36));
            var h = _tabBar.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 6; h.childControlWidth = false; h.childForceExpandWidth = false;
            string[] names = { "Kaufen", "Verkaufen", "Rückkauf" };
            for (int i = 0; i < 3; i++) { int k = i; _tabs[i] = UiFactory.Button("Tab" + i, _tabBar, names[i], () => { _tab = k; _dirty = true; }, 16); ((RectTransform)_tabs[i].transform).sizeDelta = new Vector2(150, 34); }
            var view = UiFactory.Image("View", c, new Color(0, 0, 0, 0.25f), null, true);
            view.rectTransform.Stretch(0, 0, 44, 40);
            view.gameObject.AddComponent<RectMask2D>();
            var sell = UiItemSlot.Create(view.transform, 10, SlotKind.VendorSell);
            ((RectTransform)sell.transform).Stretch(); sell.GetItem = () => null; sell.Bg.color = new Color(0, 0, 0, 0.01f); sell.Bg.sprite = null; sell.Refresh();
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            _list = UiFactory.Rect("List", view.transform);
            _list.anchorMin = new Vector2(0, 1); _list.anchorMax = new Vector2(1, 1); _list.pivot = new Vector2(0.5f, 1); _list.sizeDelta = Vector2.zero; _list.anchoredPosition = Vector2.zero;
            var v = _list.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(6, 6, 6, 6); v.spacing = 4; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            _list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _list; scroll.horizontal = false; scroll.viewport = view.rectTransform; scroll.scrollSensitivity = 30f;
            _wallet = UiFactory.Text("Wallet", c, "", 16, TextAlignmentOptions.Left, UiFactory.AccentColor);
            _wallet.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), Vector2.zero, new Vector2(0, 32));
            Window.Closed += () => { if (PlayerProfile.Instance != null) { PlayerProfile.Instance.ActiveVendor = null; PlayerProfile.Instance.ActiveVendorDef = null; } _npc = null; };
            GameEventBus.ProfileChanged += () => _dirty = true;
        }

        public void Open(VendorNpc npc)
        {
            _npc = npc;
            var p = PlayerProfile.Instance;
            if (p != null) { p.ActiveVendor = npc.Model; p.ActiveVendorDef = npc.Vendor; }
            Window.Title.text = npc.Vendor != null ? npc.Vendor.DisplayName : "Händler";
            _tab = 0; _dirty = true;
            Window.Open();
        }

        public void Refresh()
        {
            if (_npc != null && PlayerProfile.Instance != null && PlayerProfile.Instance.Player != null &&
                (_npc.transform.position - PlayerProfile.Instance.Player.transform.position).sqrMagnitude > 64f) { Window.Close(); return; }
            if (!_dirty || _npc == null) return;
            _dirty = false;
            var p = PlayerProfile.Instance;
            for (int i = 0; i < 3; i++) _tabs[i].GetComponent<Image>().color = i == _tab ? new Color(0.45f, 0.35f, 0.2f) : UiFactory.ButtonColor;
            UiFactory.Clear(_list);
            if (_tab == 0 && _npc.Vendor != null)
                foreach (var e in _npc.Vendor.Stock)
                {
                    var entry = e;
                    long price = Mathf.RoundToInt(e.PriceCopper * _npc.Model.PriceFactor);
                    string ptxt = e.PriceKarma > 0 ? e.PriceKarma.ToString("N0") + " Karma" : WalletModel.Format(price);
                    Row(new ItemInstance(e.Item, e.Count), ptxt, "Kaufen", () =>
                    {
                        if (_npc.Model.Buy(entry, p.Inventory, p.Wallet, out string err)) GameEventBus.Notify("Gekauft: " + entry.Item.DisplayName, UiFactory.AccentColor);
                        else GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f));
                    });
                }
            else if (_tab == 1)
            {
                foreach (var e in p.Inventory.All())
                {
                    if (e.item.Def.VendorValue <= 0) continue;
                    int b = e.bag, s = e.slot;
                    Row(e.item, WalletModel.Format(VendorModel.SellPrice(e.item)), "Verkaufen", () => ItemActions.Sell(b, s));
                }
            }
            else if (_tab == 2)
            {
                for (int i = 0; i < _npc.Model.Buyback.Count; i++)
                {
                    int idx = i; var bb = _npc.Model.Buyback[i];
                    Row(bb.item, WalletModel.Format(bb.price), "Zurückkaufen", () =>
                    {
                        if (!_npc.Model.BuyBack(idx, p.Inventory, p.Wallet, out string err)) GameEventBus.Notify(err, new Color(1f, 0.6f, 0.4f));
                        _dirty = true;
                    });
                }
            }
            _wallet.text = "Geldbörse: " + WalletModel.Format(p.Wallet.Copper) + "     Karma: " + p.Wallet.Karma.ToString("N0") + (_tab == 1 ? "     (Items hierher ziehen = verkaufen)" : "");
        }

        void Row(ItemInstance it, string price, string action, System.Action onClick)
        {
            var row = UiFactory.Image("Row", _list, new Color(1, 1, 1, 0.04f), UiFactory.Sprite("ui_round"));
            UiFactory.Layout(row, 50);
            var slot = UiItemSlot.Create(row.transform, 44, SlotKind.Equipment);
            ((RectTransform)slot.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(44, 44));
            slot.Kind = SlotKind.VendorSell; slot.GetItem = () => it; slot.Refresh();
            var name = UiFactory.Text("Name", row.transform, it.Def.DisplayName + (it.Count > 1 ? " ×" + it.Count : ""), 15, TextAlignmentOptions.Left, ItemDefinition.RarityColor(it.Def.Rarity));
            name.rectTransform.Stretch(54, 250, 2, 2);
            var p = UiFactory.Text("Price", row.transform, price, 14, TextAlignmentOptions.Right, UiFactory.AccentColor);
            p.rectTransform.Stretch(290, 120, 2, 2);
            var b = UiFactory.Button("Btn", row.transform, action, onClick, 14);
            ((RectTransform)b.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(110, 32));
        }
    }
}
