using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Kamerafahrt am Aussichtspunkt: eigene Cinemachine-Kamera kreist 5,5 s um das Panorama, HUD aus, Esc überspringt.</summary>
    public class VistaCinematic : MonoBehaviour
    {
        public static bool Playing { get; private set; }
        static VistaCinematic _runner;

        public static void Play(Vista v, Action onDone)
        {
            if (Playing || v == null) return;
            if (_runner == null) _runner = new GameObject("PT_VistaCinematic").AddComponent<VistaCinematic>();
            _runner.StartCoroutine(_runner.Run(v, onDone));
        }

        IEnumerator Run(Vista v, Action onDone)
        {
            Playing = true;
            var ui = UIController.Instance;
            var prof = PlayerProfile.Instance;
            var pc = prof != null && prof.Player != null ? prof.Player.GetComponent<PlayerController>() : null;
            if (pc != null) pc.InputBlocked = true;
            ThirdPersonCameraRig.CinematicActive = true;
            if (ui != null) ui.SetHudHidden(true);

            var go = new GameObject("VistaCam");
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Priority = 1000;
            var main = Camera.main;
            if (main != null) cam.Lens.FieldOfView = main.fieldOfView;
            Vector3 center = v.transform.position;
            float start = main != null ? Mathf.Atan2(main.transform.position.x - center.x, main.transform.position.z - center.z) : 0f;
            float t = 0f;
            bool skipped = false;
            yield return null;
            while (t < v.Duration)
            {
                t += Time.unscaledDeltaTime;
                float a = start + t / v.Duration * Mathf.PI * 1.25f;
                float r = v.OrbitRadius * (1f + 0.25f * Mathf.Sin(t / v.Duration * Mathf.PI));
                var pos = center + new Vector3(Mathf.Sin(a) * r, v.OrbitHeight + 2f * Mathf.Sin(t * 0.7f), Mathf.Cos(a) * r);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.LookRotation(center + Vector3.up * 1.5f - pos);
                var input = InputManager.Instance;
                if (input != null && input.EscapePressed && t > 0.3f) { InputManager.EscapeConsumedFrame = Time.frameCount; skipped = true; break; }
                yield return null;
            }
            Destroy(go);
            ThirdPersonCameraRig.CinematicActive = false;
            if (pc != null) pc.InputBlocked = false;
            if (ui != null) ui.SetHudHidden(false);
            Playing = false;
            if (skipped) GameEventBus.Notify("Kamerafahrt übersprungen", new Color(0.8f, 0.8f, 0.85f));
            onDone?.Invoke();
        }
    }
}
