using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// UI-Wurzel (Singleton): Canvas, Ebenen (HUD, Fenster, Overlay), Fenster-Manager, Tooltip, Dialog, Esc-Routing.
    /// HUD und Fenster liegen in partiellen Dateien (UIController.Hud.cs, UIController.Windows.cs).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public partial class UIController : MonoBehaviour
    {
        public static UIController Instance { get; private set; }

        public Combatant Player;
        public IconLibrary Icons;

        public RectTransform Root { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform WindowLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }
        public WindowManager Windows { get; private set; }
        public UiTooltip Tooltip { get; private set; }
        public UiDialog Dialog { get; private set; }
        public float CanvasScale => _canvas != null ? _canvas.scaleFactor : 1f;
        public bool HudHidden { get; private set; }

        Canvas _canvas;
        CanvasGroup _hudGroup;
        SkillCaster _caster; PlayerController _pc; TargetingSystem _targeting; DownedController _downed; InteractionSystem _interaction;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            UiFactory.Icons = Icons;
            EnsureEventSystem();
            BuildCanvas();
            BuildHud();
            BuildWindows();
            InputManager.EscapeHandler = HandleEscape;
        }

        void OnDestroy()
        {
            if (Instance == this) { Instance = null; OnDestroyWindows(); }
            if (InputManager.EscapeHandler == (System.Func<bool>)HandleEscape) InputManager.EscapeHandler = null;
            InputManager.PointerOverUi = false; InputManager.TextInputActive = false;
            ThirdPersonCameraRig.UiWantsCursor = false;
        }

        void Start() => BindPlayer(Player != null ? Player : (GameManager.Instance != null ? GameManager.Instance.Player : null));

        public void BindPlayer(Combatant p)
        {
            Player = p;
            if (p == null) return;
            _caster = p.GetComponent<SkillCaster>();
            _pc = p.GetComponent<PlayerController>();
            _targeting = p.GetComponent<TargetingSystem>();
            _downed = p.GetComponent<DownedController>();
            _interaction = p.GetComponent<InteractionSystem>();
            BindHud();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(go);
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("PT_HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Root = (RectTransform)cgo.transform;
            HudLayer = UiFactory.Rect("HUD", Root).Stretch();
            _hudGroup = HudLayer.gameObject.AddComponent<CanvasGroup>();
            WindowLayer = UiFactory.Rect("Windows", Root).Stretch();
            OverlayLayer = UiFactory.Rect("Overlay", Root).Stretch();
            Windows = new WindowManager(WindowLayer);
            Windows.WindowClosed += OnWindowClosed;
            Tooltip = new UiTooltip(OverlayLayer);
            var dialogMgr = new WindowManager(OverlayLayer);
            _dialogManager = dialogMgr;
            Dialog = new UiDialog(dialogMgr);
        }

        WindowManager _dialogManager;

        void OnWindowClosed(UiWindow w)
        {
            Tooltip.Hide();
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.RequestSave("Fenster geschlossen");
        }

        bool HandleEscape()
        {
            if (DialogueEscape()) return true;   // Lauf 6: Szene → „Überspringen?“
            // Lauf 5 / M24 (GW2): Menü/Optionen zuerst; sonst Kontext → Dialog → Fenster; ohne Ziel öffnet Esc das Spielmenü
            if (GameMenu != null && GameMenu.IsOpen) { GameMenu.Model.Escape(false); return true; }
            if (ContextMenu != null && ContextMenu.IsOpen) { ContextMenu.Close(); return true; }
            if (_dialogManager.CloseTop()) return true;
            if (Windows.CloseTop()) return true;
            var tgt = Player != null ? Player.GetComponent<TargetingSystem>() : null;
            if (tgt != null && tgt.Current != null) return false;   // Welt: Ziel abwählen
            if (GameMenu != null) { GameMenu.Model.Escape(false); return true; }
            return false;
        }

        public void SetHudHidden(bool hidden)
        {
            HudHidden = hidden;
            _hudGroup.alpha = hidden ? 0f : 1f;
            if (hidden) { Windows.CloseAll(); Tooltip.Hide(); }
        }

        void Update()
        {
            if (Player == null && GameManager.Instance != null && GameManager.Instance.Player != null) BindPlayer(GameManager.Instance.Player);
            var input = InputManager.Instance;
            bool anyWindow = Windows.AnyOpen || _dialogManager.AnyOpen;
            Vector2 mouse = input != null ? input.PointerPosition : (Vector2)Input.mousePosition;
            InputManager.PointerOverUi = _dialogManager.AnyOpen || (anyWindow && Windows.IsPointerOverWindow(mouse)) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            ThirdPersonCameraRig.UiWantsCursor = anyWindow;
            if (input != null && !InputManager.TextInputActive && !HudHidden) HandleWindowKeys(input);
            UpdateHud();
            UpdateWindows();
        }

        partial void BuildWindows();
        partial void UpdateWindows();
        partial void HandleWindowKeys(InputManager input);
    }
}
