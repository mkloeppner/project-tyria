using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectTyrant
{
    /// <summary>
    /// Kapselt sämtliche Eingaben (New Input System, Actions im Code definiert).
    /// Fallback auf Legacy-Input, falls das neue Backend (noch) keine Geräte liefert.
    /// Skill-Index: 0..4 = Tasten 1–5, 5 = 6 (Heal), 6..8 = 7–9 (Utility), 9 = 0 (Elite).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public float DoubleTapWindow = 0.28f;
        public float MouseSensitivityLegacy = 6f;

        public Vector2 Move { get; private set; }
        public float Strafe { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public bool RotateHeld { get; private set; }
        public bool PrimaryClick { get; private set; }
        public float Zoom { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }
        /// <summary>Lauf 4: X = Reittier (antippen/halten).</summary>
        public bool MountHeld { get; private set; }
        public bool MountPressed { get; private set; }
        public bool MountReleased { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool DodgeHeld { get; private set; }   // Lauf 10: Sturzflug in der Luft

        /// <summary>Lauf 10: frei belegbare Taste (Reittier-Direkttasten), Name wie KeyCode/Key („F6“, „N“). Gesperrt bei Texteingabe.</summary>
        public bool KeyDown(string name)
        {
            if (string.IsNullOrEmpty(name) || TextInputActive) return false;
            if (UsingNewInputSystem)
                return Keyboard.current != null && System.Enum.TryParse<Key>(name, out var k) && k != Key.None && Keyboard.current[k].wasPressedThisFrame;
            return System.Enum.TryParse<KeyCode>(name, out var kc) && Input.GetKeyDown(kc);
        }
        public bool TabPressed { get; private set; }
        public bool SwapPressed { get; private set; }
        public bool ActionCamPressed { get; private set; }
        /// <summary>C gehalten – auf der fliegenden Himmelsschuppe „Sinken“ (statt Action-Kamera).</summary>
        public bool DescendHeld { get; private set; }
        public bool EscapePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool HeroPressed { get; private set; }
        public bool MapPressed { get; private set; }
        public bool DebugPressed { get; private set; }
        public bool HelpPressed { get; private set; }
        public bool ZoomInPressed { get; private set; }
        public bool ZoomOutPressed { get; private set; }
        public bool AltHeld { get; private set; }
        public bool ShiftHeld { get; private set; }
        /// <summary>Gesetzt von der UI, solange ein Textfeld Fokus hat – alle Spieltasten werden unterdrückt.</summary>
        public static bool TextInputActive;
        /// <summary>Gesetzt von der UI, wenn der Mauszeiger über einem Fenster liegt – Skills/Klicks gehen nicht in die Welt.</summary>
        public static bool PointerOverUi;
        /// <summary>Esc wurde in diesem Frame bereits von der UI verbraucht (Fenster geschlossen).</summary>
        public static int EscapeConsumedFrame = -1;
        public bool EscapeForWorld => EscapePressed && EscapeConsumedFrame != Time.frameCount;
        /// <summary>Von der UI registriert: schließt das oberste Fenster und gibt true zurück, wenn Esc verbraucht wurde.</summary>
        public static System.Func<bool> EscapeHandler;
        public int SkillPressed { get; private set; } = -1;
        /// <summary>Fix-Lauf F1: losgelassene Skill-Taste (Bodenziel „Drücken und Loslassen“).</summary>
        public int SkillReleased { get; private set; } = -1;
        /// <summary>Lauf 5 / M25: F1–F4 Klassenmechanik (0–3), F5 Elite-Mechanik (4); −1 = keine.</summary>
        public int MechanicPressed { get; private set; } = -1;
        /// <summary>Doppeltipp-Richtung im Input-Raum (x = rechts, y = vorne), sonst Vector2.zero.</summary>
        public Vector2 DoubleTapDirection { get; private set; }
        public bool UsingNewInputSystem { get; private set; }

        InputAction _move, _strafe, _look, _point, _rmb, _lmb, _zoom, _jump, _dodge, _tab, _swap, _actionCam, _escape;
        InputAction _interact, _inventory, _hero, _map, _debug, _help, _zoomIn, _zoomOut, _alt, _shift, _mount;
        readonly InputAction[] _skills = new InputAction[10];
        readonly InputAction[] _mech = new InputAction[5];
        static readonly KeyCode[] MechKeyCodes = { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5 };
        readonly float[] _lastTap = new float[4];
        static readonly string[] SkillKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        static readonly KeyCode[] SkillKeyCodes = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildActions();
            for (int i = 0; i < _lastTap.Length; i++) _lastTap[i] = -10f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DisposeActions();
        }

        void BuildActions()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");
            _strafe = new InputAction("Strafe", InputActionType.Value);
            _strafe.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            _look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            _point = new InputAction("Point", InputActionType.Value, "<Mouse>/position");
            _rmb = new InputAction("RotateCamera", InputActionType.Button, "<Mouse>/rightButton");
            _lmb = new InputAction("Primary", InputActionType.Button, "<Mouse>/leftButton");
            _zoom = new InputAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");
            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _dodge = new InputAction("Dodge", InputActionType.Button, "<Keyboard>/v");
            _tab = new InputAction("Target", InputActionType.Button, "<Keyboard>/tab");
            _swap = new InputAction("WeaponSwap", InputActionType.Button, "<Keyboard>/backquote");
            _swap.AddBinding("<Keyboard>/y");   // Lauf 4: X gehört dem Reittier → Waffenwechsel auf ^ oder Y
            _mount = new InputAction("Mount", InputActionType.Button, "<Keyboard>/x");
            _actionCam = new InputAction("ActionCam", InputActionType.Button, "<Keyboard>/c");
            _escape = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            _interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/f");
            _inventory = new InputAction("Inventory", InputActionType.Button, "<Keyboard>/i");
            _hero = new InputAction("Hero", InputActionType.Button, "<Keyboard>/h");
            _map = new InputAction("WorldMap", InputActionType.Button, "<Keyboard>/m");
            _debug = new InputAction("DebugPanel", InputActionType.Button, "<Keyboard>/f9");
            _help = new InputAction("Help", InputActionType.Button, "<Keyboard>/f10");   // Lauf 5 / M25: Hilfe von F1 nach F10 (F1–F4 = Klassenmechanik)
            for (int i = 0; i < 5; i++) _mech[i] = new InputAction("Mechanic" + (i + 1), InputActionType.Button, "<Keyboard>/f" + (i + 1));
            _zoomIn = new InputAction("MinimapZoomIn", InputActionType.Button, "<Keyboard>/numpadPlus");
            _zoomIn.AddBinding("<Keyboard>/equals"); _zoomIn.AddBinding("<Keyboard>/rightBracket");
            _zoomOut = new InputAction("MinimapZoomOut", InputActionType.Button, "<Keyboard>/numpadMinus");
            _zoomOut.AddBinding("<Keyboard>/minus"); _zoomOut.AddBinding("<Keyboard>/slash");
            _alt = new InputAction("Alt", InputActionType.Button, "<Keyboard>/leftAlt");
            _alt.AddBinding("<Keyboard>/rightAlt");
            _shift = new InputAction("Shift", InputActionType.Button, "<Keyboard>/leftShift");
            _shift.AddBinding("<Keyboard>/rightShift");
            for (int i = 0; i < 10; i++) _skills[i] = new InputAction("Skill" + SkillKeys[i], InputActionType.Button, "<Keyboard>/" + SkillKeys[i]);
            foreach (var a in AllActions()) a.Enable();
        }

        System.Collections.Generic.IEnumerable<InputAction> AllActions()
        {
            yield return _move; yield return _strafe; yield return _look; yield return _point; yield return _rmb; yield return _lmb;
            yield return _zoom; yield return _jump; yield return _dodge; yield return _tab; yield return _swap; yield return _actionCam; yield return _escape;
            yield return _interact; yield return _inventory; yield return _hero; yield return _map; yield return _debug; yield return _help;
            yield return _zoomIn; yield return _zoomOut; yield return _alt; yield return _shift; yield return _mount;
            foreach (var s in _skills) yield return s;
            foreach (var m in _mech) if (m != null) yield return m;
        }

        void DisposeActions()
        {
            if (_move == null) return;
            foreach (var a in AllActions()) { a.Disable(); a.Dispose(); }
        }

        void Update()
        {
            UsingNewInputSystem = Keyboard.current != null && Mouse.current != null;
            if (UsingNewInputSystem) ReadNew(); else ReadLegacy();
            DetectDoubleTap();
            if (EscapePressed && EscapeHandler != null && EscapeHandler()) EscapeConsumedFrame = Time.frameCount;
            if (TextInputActive) SuppressGameplay();
            else if (PointerOverUi) { PrimaryClick = false; Zoom = 0f; SkillPressed = -1; }
        }

        void ReadNew()
        {
            Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            Strafe = _strafe.ReadValue<float>();
            LookDelta = _look.ReadValue<Vector2>();
            PointerPosition = _point.ReadValue<Vector2>();
            RotateHeld = _rmb.IsPressed();
            PrimaryClick = _lmb.WasPressedThisFrame();
            Zoom = _zoom.ReadValue<float>();
            JumpPressed = _jump.WasPressedThisFrame();
            JumpHeld = _jump.IsPressed(); JumpReleased = _jump.WasReleasedThisFrame();
            MountHeld = _mount.IsPressed(); MountPressed = _mount.WasPressedThisFrame(); MountReleased = _mount.WasReleasedThisFrame();
            DodgePressed = _dodge.WasPressedThisFrame(); DodgeHeld = _dodge.IsPressed();
            TabPressed = _tab.WasPressedThisFrame();
            SwapPressed = _swap.WasPressedThisFrame();
            ActionCamPressed = _actionCam.WasPressedThisFrame(); DescendHeld = _actionCam.IsPressed();
            EscapePressed = _escape.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            InventoryPressed = _inventory.WasPressedThisFrame();
            HeroPressed = _hero.WasPressedThisFrame();
            MapPressed = _map.WasPressedThisFrame();
            DebugPressed = _debug.WasPressedThisFrame();
            HelpPressed = _help.WasPressedThisFrame();
            MechanicPressed = -1; for (int i = 0; i < 5; i++) if (_mech[i].WasPressedThisFrame()) { MechanicPressed = i; break; }
            ZoomInPressed = _zoomIn.WasPressedThisFrame();
            ZoomOutPressed = _zoomOut.WasPressedThisFrame();
            AltHeld = _alt.IsPressed();
            ShiftHeld = _shift.IsPressed();
            SkillPressed = -1;
            for (int i = 0; i < 10; i++) if (_skills[i].WasPressedThisFrame()) { SkillPressed = i; break; }
            SkillReleased = -1; for (int i = 0; i < 10; i++) if (_skills[i].WasReleasedThisFrame()) { SkillReleased = i; break; }
        }

        void ReadLegacy()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            Strafe = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
            LookDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * MouseSensitivityLegacy;
            PointerPosition = Input.mousePosition;
            RotateHeld = Input.GetMouseButton(1);
            PrimaryClick = Input.GetMouseButtonDown(0);
            Zoom = Input.mouseScrollDelta.y * 120f;
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            JumpHeld = Input.GetKey(KeyCode.Space); JumpReleased = Input.GetKeyUp(KeyCode.Space);
            MountHeld = Input.GetKey(KeyCode.X); MountPressed = Input.GetKeyDown(KeyCode.X); MountReleased = Input.GetKeyUp(KeyCode.X);
            DodgePressed = Input.GetKeyDown(KeyCode.V); DodgeHeld = Input.GetKey(KeyCode.V);
            TabPressed = Input.GetKeyDown(KeyCode.Tab);
            SwapPressed = Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Y);
            ActionCamPressed = Input.GetKeyDown(KeyCode.C); DescendHeld = Input.GetKey(KeyCode.C);
            EscapePressed = Input.GetKeyDown(KeyCode.Escape);
            InteractPressed = Input.GetKeyDown(KeyCode.F);
            InventoryPressed = Input.GetKeyDown(KeyCode.I);
            HeroPressed = Input.GetKeyDown(KeyCode.H);
            MapPressed = Input.GetKeyDown(KeyCode.M);
            DebugPressed = Input.GetKeyDown(KeyCode.F9);
            HelpPressed = Input.GetKeyDown(KeyCode.F10);
            MechanicPressed = -1; for (int i = 0; i < 5; i++) if (Input.GetKeyDown(MechKeyCodes[i])) { MechanicPressed = i; break; }
            ZoomInPressed = Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.RightBracket);
            ZoomOutPressed = Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Slash);
            AltHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            ShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            SkillPressed = -1;
            for (int i = 0; i < 10; i++) if (Input.GetKeyDown(SkillKeyCodes[i])) { SkillPressed = i; break; }
            SkillReleased = -1; for (int i = 0; i < 10; i++) if (Input.GetKeyUp(SkillKeyCodes[i])) { SkillReleased = i; break; }
        }

        void SuppressGameplay()
        {
            Move = Vector2.zero; Strafe = 0f; JumpPressed = false; DodgePressed = false; DodgeHeld = false; TabPressed = false; SwapPressed = false;
            JumpHeld = false; JumpReleased = false; MountHeld = false; MountPressed = false; MountReleased = false;
            ActionCamPressed = false; DescendHeld = false; InteractPressed = false; InventoryPressed = false; HeroPressed = false; MapPressed = false;
            ZoomInPressed = false; ZoomOutPressed = false; SkillPressed = -1; DoubleTapDirection = Vector2.zero; PrimaryClick = false; HelpPressed = false; MechanicPressed = -1;
        }

        void DetectDoubleTap()
        {
            DoubleTapDirection = Vector2.zero;
            bool w, a, s, d;
            if (UsingNewInputSystem)
            {
                var k = Keyboard.current;
                w = k.wKey.wasPressedThisFrame; a = k.aKey.wasPressedThisFrame; s = k.sKey.wasPressedThisFrame; d = k.dKey.wasPressedThisFrame;
            }
            else
            {
                w = Input.GetKeyDown(KeyCode.W); a = Input.GetKeyDown(KeyCode.A); s = Input.GetKeyDown(KeyCode.S); d = Input.GetKeyDown(KeyCode.D);
            }
            CheckTap(0, w, Vector2.up);
            CheckTap(1, a, Vector2.left);
            CheckTap(2, s, Vector2.down);
            CheckTap(3, d, Vector2.right);
        }

        void CheckTap(int idx, bool pressed, Vector2 dir)
        {
            if (!pressed) return;
            float t = Time.unscaledTime;
            if (t - _lastTap[idx] <= DoubleTapWindow) { DoubleTapDirection = dir; _lastTap[idx] = -10f; }
            else _lastTap[idx] = t;
        }
    }
}
