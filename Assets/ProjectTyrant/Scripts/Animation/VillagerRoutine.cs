using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// Lauf 5 / M21: Ambient-Routine eines Dorfbewohners zwischen 2–3 Punkten. Geht im Schritttempo zum nächsten Punkt,
    /// dreht sich zur Blickrichtung des Punkts und spielt dort eine Tätigkeit (Arbeiten, Reden, Sitzen, Umschauen) für eine feste Dauer.
    /// </summary>
    public class VillagerRoutine : MonoBehaviour
    {
        [System.Serializable] public struct Stop { public Vector3 Position; public float Yaw; public string Activity; public float Duration; }
        public Stop[] Stops = new Stop[0];
        public float WalkSpeed = 1.5f;
        public CharacterAnimator Anim;
        int _i; float _timer; bool _working;

        public int CurrentStop => _i;
        public bool Working => _working;

        void Start() { if (Anim == null) Anim = GetComponentInChildren<CharacterAnimator>(); if (Stops.Length > 0) transform.position = Ground(Stops[0].Position); Arrive(); }

        static Vector3 Ground(Vector3 p) => ZoneAnchors.GroundAt(p + Vector3.up * 1.5f);

        void Arrive()
        {
            if (Stops.Length == 0) return;
            var s = Stops[_i];
            _working = true; _timer = Mathf.Max(2f, s.Duration);
            transform.rotation = Quaternion.Euler(0, s.Yaw, 0);
            if (Anim != null && !string.IsNullOrEmpty(s.Activity)) Anim.PlayAuto(s.Activity, 0.25f);
        }

        void Update()
        {
            if (Stops.Length < 2) return;
            float dt = Time.deltaTime;
            if (_working)
            {
                _timer -= dt;
                if (_timer > 0f) return;
                _working = false; _i = (_i + 1) % Stops.Length;
                if (Anim != null) Anim.StopAll(0.25f);
            }
            var target = Ground(Stops[_i].Position);
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.magnitude < 0.25f) { Arrive(); return; }
            var dir = d.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 300f * dt);
            var next = transform.position + dir * Mathf.Min(d.magnitude, WalkSpeed * dt);
            transform.position = Ground(next);
        }
    }
}
