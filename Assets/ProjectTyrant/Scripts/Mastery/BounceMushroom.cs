using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Sprungpilz: schleudert den Spieler ≈ 11 m hoch (ab „Umweltkunde 2“).</summary>
    public class BounceMushroom : MonoBehaviour
    {
        public float Radius = 1.6f, LaunchVelocity = 23f;
        float _cd; static float _hint;

        void Update()
        {
            _cd -= Time.deltaTime;
            var prof = PlayerProfile.Instance;
            if (_cd > 0f || prof == null || prof.Player == null) return;
            var pc = prof.Player.GetComponent<PlayerController>();
            var d = prof.Player.transform.position - transform.position;
            if (pc == null || d.y < -0.5f || d.y > 2.2f) return;
            d.y = 0f;
            if (d.sqrMagnitude > Radius * Radius) return;
            if (!MasteryEffects.CanBounce(prof.Mastery))
            {
                if (Time.time > _hint) { _hint = Time.time + 8f; GameEventBus.Notify("Ein Sprungpilz – „Umweltkunde 2“ lässt dich darauf springen.", new Color(0.6f, 1f, 0.8f)); }
                return;
            }
            _cd = 1f;
            pc.SetVerticalVelocity(LaunchVelocity);
            pc.ResetFallTracking();
            SkillExecutor.VfxPulse(transform.position, 2f, new Color(0.8f, 0.5f, 1f));
        }
    }
}
