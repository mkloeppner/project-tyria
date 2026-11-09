using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Fenster: Debug (F9), Held (H), Inventar (I), Weltkarte (M), Händler, Kontextmenü.</summary>
    public partial class UIController
    {
        public DebugPanel Debug { get; private set; }
        public HeroWindow Hero { get; private set; }
        public InventoryWindow Inventory { get; private set; }
        public VendorWindow Vendor { get; private set; }
        public UiContextMenu ContextMenu { get; private set; }
        public GameMenu GameMenu { get; private set; }

        partial void BuildWindows()
        {
            Hero = new HeroWindow(Windows, this);
            Inventory = new InventoryWindow(Windows, this);
            Vendor = new VendorWindow(Windows);
            ContextMenu = new UiContextMenu(OverlayLayer);
            GameMenu = new GameMenu(_dialogManager);   // Lauf 5 / M24: Esc-Spielmenü + Optionen
            GameOptions.Changed += OnOptionsChanged;
            BuildMap();
            if (Application.isEditor || UnityEngine.Debug.isDebugBuild) Debug = new DebugPanel(Windows);
            UiItemSlot.Dropped += ItemActions.HandleDrop;
            UiItemSlot.RightClicked += ItemActions.ContextMenu;
            UiItemSlot.DoubleClicked += ItemActions.DoubleClick;
            GameEventBus.ProfileChanged += () => { Hero.MarkDirty(); Inventory.MarkDirty(); };
        }

        void OnOptionsChanged(GameOptions o)
        {
            var sc = Root != null ? Root.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
            if (sc != null) sc.referenceResolution = new Vector2(1920, 1080) / Mathf.Max(0.5f, o.UiScale);
            if (Minimap != null && Minimap.Rotate != o.MinimapRotate) Minimap.ToggleRotate();
        }

        void OnDestroyWindows()
        {
            GameOptions.Changed -= OnOptionsChanged;
            UiItemSlot.Dropped -= ItemActions.HandleDrop;
            UiItemSlot.RightClicked -= ItemActions.ContextMenu;
            UiItemSlot.DoubleClicked -= ItemActions.DoubleClick;
        }

        public void OpenVendor(VendorNpc npc)
        {
            Hero.Window.Close();
            Vendor.Open(npc);
            Inventory.Window.Open();
        }

        partial void HandleWindowKeys(InputManager input)
        {
            if (input.DebugPressed && Debug != null) Debug.Window.Toggle();
            if (input.HeroPressed) Hero.Window.Toggle();
            if (input.InventoryPressed) Inventory.Window.Toggle();
            if (input.MapPressed) ToggleWorldMap();
            if (input.HelpPressed) ToggleHelp();
        }

        partial void UpdateWindows()
        {
            UpdateMap();
            if (Hero.Window.IsOpen) Hero.Refresh();
            if (Inventory.Window.IsOpen) Inventory.Refresh();
            if (Vendor.Window.IsOpen) Vendor.Refresh();
        }
    }
}
