using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Umgebungstier (nicht kämpfend): wandert um seinen Ursprung, pickt/ruht, flieht vor dem Spieler.
    /// Bewegung über Boden-Raycast; Beine animiert der <see cref="ProceduralLimbAnimator"/>.
    /// </summary>
    public class AmbientCritter : MonoBehaviour
    {
        public float WanderRadius = 10f;
        public float Speed = 1.6f;
        public float FleeSpeed = 5f;
        public float FleeDistance = 6f;
        public bool Hops;
        public Transform Visual;
        Vector3 _home, _target;
        float _wait, _hop, _check;
        bool _fleeing;

        void Start()
        {
            _home = transform.position;
            _target = _home;
            _wait = Random.Range(0f, 3f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var p = PlayerProfile.Instance != null && PlayerProfile.Instance.Player != null ? PlayerProfile.Instance.Player.transform : null;
            if ((_check -= dt) <= 0f && p != null)
            {
                _check = 0.25f;
                var d = transform.position - p.position; d.y = 0f;
                if (d.magnitude < FleeDistance && !_fleeing)
                {
                    _fleeing = true;
                    var away = (d.sqrMagnitude > 0.01f ? d.normalized : transform.forward) * (FleeDistance * 2.2f);
                    _target = ClampHome(transform.position + away);
                }
            }
            Vector3 to = _target - transform.position; to.y = 0f;
            if (to.magnitude < 0.4f)
            {
                _fleeing = false;
                _wait -= dt;
                if (_wait <= 0f)
                {
                    _wait = Random.Range(2f, 6f);
                    var r = Random.insideUnitCircle * WanderRadius;
                    _target = _home + new Vector3(r.x, 0f, r.y);
                }
                Animate(dt, false);
                return;
            }
            float sp = _fleeing ? FleeSpeed : Speed;
            var step = to.normalized * Mathf.Min(to.magnitude, sp * dt);
            var next = ZoneAnchors.GroundAt(transform.position + step + Vector3.up * 2f);
            transform.position = next;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 360f * dt);
            Animate(dt, true);
        }

        Vector3 ClampHome(Vector3 p)
        {
            var d = p - _home; d.y = 0f;
            if (d.magnitude > WanderRadius * 1.6f) p = _home + d.normalized * WanderRadius * 1.6f;
            return p;
        }

        void Animate(float dt, bool moving)
        {
            if (Visual == null || !Hops) return;
            _hop = moving ? _hop + dt * 9f : Mathf.MoveTowards(_hop, Mathf.Ceil(_hop / Mathf.PI) * Mathf.PI, dt * 9f);
            Visual.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(_hop)) * 0.25f;
        }
    }
}
