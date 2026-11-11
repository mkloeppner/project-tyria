using Unity.Cinemachine;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Steuert die Cinemachine-Orbitalkamera: Rechtsklick-Drehen (freie Kamera), Action-Cam-Toggle (C),
    /// Zoom (Mausrad). Liefert Zielpunkte für Ground-Target-Skills.
    /// </summary>
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        public static ThirdPersonCameraRig Current { get; private set; }

        public CinemachineOrbitalFollow Orbital;
        public Camera OutputCamera;
        public float Sensitivity = 0.15f;
        public float MinPitch = -15f, MaxPitch = 70f;
        public float MinZoom = 0.35f, MaxZoom = 1.8f;
        public float ZoomSpeed = 0.0015f;
        public LayerMask GroundMask = ~0;

        public bool ActionCam { get; private set; }
        /// <summary>Von der UI gesetzt, solange ein Fenster offen ist (Cursor frei, Action-Cam pausiert).</summary>
        public static bool UiWantsCursor;
        /// <summary>Kamerafahrt (Aussichtspunkt) übernimmt die Kamera.</summary>
        public static bool CinematicActive;
        public bool IsRotating => !CinematicActive && ((ActionCam && !UiWantsCursor) || (InputManager.Instance != null && InputManager.Instance.RotateHeld));

        float _yaw, _pitch = 18f;
        /// <summary>Fix-Lauf F1: Zoom in Metern (Mausrad), Bereich zu Fuß/beritten, Dämpfung, Raster/Umkehr aus den Optionen.</summary>
        public readonly CameraZoomModel Zoom = new CameraZoomModel();
        public float Distance => Zoom.Distance;
        float _saveZoomAt = -1f;

        void Awake()
        {
            Current = this;
            if (OutputCamera == null) OutputCamera = Camera.main;
            if (Orbital != null)
            {
                _yaw = Orbital.HorizontalAxis.Value;
                _pitch = Orbital.VerticalAxis.Value;
                Orbital.RadialAxis.Range = new Vector2(1f, 1f); Orbital.RadialAxis.Value = 1f;   // Abstand läuft jetzt über Orbital.Radius
            }
            var o = GameOptions.Load(); ApplyOptions(o);
            Zoom.Snap(o.CameraCurrentDistance > 0f ? o.CameraCurrentDistance : o.CameraDefaultDistance);
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Update()
        {
            var input = InputManager.Instance;
            if (input == null || Orbital == null) return;

            if (input.ActionCamPressed && !(MountController.Local != null && MountController.Local.UsesDescendKey)) SetActionCam(!ActionCam);   // C = Sinken auf der fliegenden Himmelsschuppe
            if (input.EscapeForWorld && ActionCam && !UiWantsCursor) SetActionCam(false);

            bool rotating = IsRotating;
            if (!ActionCam || UiWantsCursor || CinematicActive)
            {
                Cursor.lockState = rotating ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !rotating;
            }
            if (rotating)
            {
                _yaw += input.LookDelta.x * Sensitivity;
                _pitch = Mathf.Clamp(_pitch - input.LookDelta.y * Sensitivity, MinPitch, MaxPitch);
            }
            // Fix-Lauf F1 – Befund: Scroll war gebunden, aber das Input System 1.20 liefert ±1 je Raste (normalisiert) statt 120;
            // mit 0,0015 je Einheit änderte eine Raste den Radius um 0,1 % – praktisch kein Zoom. Jetzt Rasten → Meter über CameraZoomModel.
            var pc = PlayerControllerRef();
            Zoom.SetMounted(pc != null && pc.MountedControl);
            Zoom.Locked = CinematicActive;
            if (Mathf.Abs(input.Zoom) > 0.001f) { Zoom.Scroll(CameraZoomModel.Notches(input.Zoom)); _saveZoomAt = Time.unscaledTime + 1f; }
            Zoom.Tick(Time.unscaledDeltaTime);
            if (_saveZoomAt > 0f && Time.unscaledTime > _saveZoomAt) { _saveZoomAt = -1f; var o = GameOptions.Load(); o.CameraCurrentDistance = Zoom.Target; o.SaveQuiet(); }

            Orbital.HorizontalAxis.Value = _yaw;
            Orbital.VerticalAxis.Value = _pitch;
            Orbital.Radius = Zoom.Distance;
        }

        public void SetActionCam(bool on)
        {
            ActionCam = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
            GameEventBus.Notify(on ? "Action-Kamera an" : "Action-Kamera aus", new Color(0.8f, 0.9f, 1f));
        }

        public void SetYaw(float yaw) { _yaw = yaw; }

        /// <summary>Fix-Lauf F1: Kamera gleicht ihren Yaw im Drehmodus gedämpft dem Charakter an (nicht beim Drehen per Maus).</summary>
        public void FollowYaw(float characterYaw)
        {
            if (IsRotating || CinematicActive) return;
            _yaw = Mathf.LerpAngle(_yaw, characterYaw, 1f - Mathf.Exp(-Time.deltaTime / 0.12f));
            if (_yaw > 180f) _yaw -= 360f; if (_yaw < -180f) _yaw += 360f;
        }
        public float Yaw => _yaw;

        public void ApplyOptions(GameOptions o)
        {
            if (o == null) return;
            Zoom.Step = o.ZoomStep; Zoom.Invert = o.ZoomInvert;
        }

        PlayerController _pc;
        PlayerController PlayerControllerRef() { if (_pc == null && GameManager.Instance != null && GameManager.Instance.Player != null) _pc = GameManager.Instance.Player.GetComponent<PlayerController>(); return _pc; }

        /// <summary>Flache Vorwärtsrichtung der Kamera.</summary>
        public Vector3 FlatForward
        {
            get
            {
                var cam = OutputCamera != null ? OutputCamera.transform : transform;
                var f = cam.forward; f.y = 0f;
                return f.sqrMagnitude < 0.001f ? Vector3.forward : f.normalized;
            }
        }

        public Ray AimRay()
        {
            if (OutputCamera == null) OutputCamera = Camera.main;
            if (OutputCamera == null) return new Ray(transform.position, transform.forward);
            if (ActionCam || InputManager.Instance == null)
                return OutputCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return OutputCamera.ScreenPointToRay(InputManager.Instance.PointerPosition);
        }

        /// <summary>Bodenpunkt unter Fadenkreuz/Mauszeiger, falls vorhanden.</summary>
        public bool TryGetGroundPoint(out Vector3 point, float maxDistance = 200f)
        {
            var ray = AimRay();
            var hits = Physics.RaycastAll(ray, maxDistance, GroundMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; point = Vector3.zero; bool found = false;
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<Combatant>() != null) continue;
                if (h.distance < best) { best = h.distance; point = h.point; found = true; }
            }
            return found;
        }
    }
}
