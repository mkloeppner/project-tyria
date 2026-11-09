using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Giftnebel (Nebelmoor): Schaden + Gift im Radius, außer mit „Umweltkunde 1“.</summary>
    public class PoisonMist : MonoBehaviour
    {
        public float Radius = 16f;
        float _tick; static float _hint;

        void Update()
        {
            var prof = PlayerProfile.Instance;
            if (prof == null || prof.Player == null || prof.Player.IsDead) return;
            var d = prof.Player.transform.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > Radius * Radius) return;
            if (MasteryEffects.PoisonMistImmune(prof.Mastery)) return;
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 1f;
            prof.Player.TakeDamage(new DamageInfo { Source = null, Amount = prof.Player.MaxHealth * 0.03f, Point = prof.Player.transform.position, IsCondition = true });
            prof.Player.Effects.Apply(new EffectApplication(EffectType.Poison, 1, 3f), null);
            if (Time.time > _hint) { _hint = Time.time + 8f; GameEventBus.Notify("Giftnebel! „Umweltkunde 1“ schützt davor.", new Color(0.6f, 1f, 0.5f)); }
        }
    }
}
