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

        float _yaw, _pitch = 18f, _zoom = 1f;

        void Awake()
        {
            Current = this;
            if (OutputCamera == null) OutputCamera = Camera.main;
            if (Orbital != null)
            {
                _yaw = Orbital.HorizontalAxis.Value;
                _pitch = Orbital.VerticalAxis.Value;
                _zoom = Orbital.RadialAxis.Value;
            }
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Update()
        {
            var input = InputManager.Instance;
            if (input == null || Orbital == null) return;

            if (input.ActionCamPressed) SetActionCam(!ActionCam);
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
            if (Mathf.Abs(input.Zoom) > 0.01f)
                _zoom = Mathf.Clamp(_zoom - input.Zoom * ZoomSpeed, MinZoom, MaxZoom);

            Orbital.HorizontalAxis.Value = _yaw;
            Orbital.VerticalAxis.Value = _pitch;
            Orbital.RadialAxis.Value = _zoom;
        }

        public void SetActionCam(bool on)
        {
            ActionCam = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
            GameEventBus.Notify(on ? "Action-Kamera an" : "Action-Kamera aus", new Color(0.8f, 0.9f, 1f));
        }

        public void SetYaw(float yaw) { _yaw = yaw; }

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
