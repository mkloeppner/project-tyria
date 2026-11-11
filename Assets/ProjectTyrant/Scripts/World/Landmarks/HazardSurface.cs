using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Welt-Lauf W1: Hürden-Oberfläche (Rechteck um das Objekt, gedreht um Y). Wirkt zu Fuß nach <see cref="HazardRules"/>, nie sofort tödlich;
    /// passende Reittiere/Flug sind unberührt. Am Rand: Hinweis „Benötigt: …“ (Schild baut der Builder).
    /// </summary>
    public class HazardSurface : MonoBehaviour
    {
        public static readonly List<HazardSurface> All = new List<HazardSurface>();
        public string Id;
        public HazardKind Kind;
        public Vector2 Size = new Vector2(20f, 20f);
        public bool Circular;
        [Tooltip("Höhe des Wirkbereichs über der Oberfläche (Flug darüber zählt nicht)")] public float Ceiling = 3.5f;
        public bool EdgeDecal;   // vom Builder gesetzt (Validator: Rand erkennbar)
        public float LastSpeed { get; private set; } = 1f;
        public bool PlayerInside { get; private set; }
        HazardExposure _ex;
        float _hintAt, _sinkHintAt;

        void OnEnable() { All.Add(this); _ex = new HazardExposure(Kind); GameEventBus.DodgePerformed += OnDodge; }
        void OnDisable() { All.Remove(this); GameEventBus.DodgePerformed -= OnDodge; }

        public bool Contains(Vector3 p, float margin = 0f)
        {
            var l = transform.InverseTransformPoint(p);
            if (Circular) return new Vector2(l.x, l.z).magnitude <= Size.x * 0.5f + margin && l.y > -4f && l.y < Ceiling;
            return Mathf.Abs(l.x) <= Size.x * 0.5f + margin && Mathf.Abs(l.z) <= Size.y * 0.5f + margin && l.y > -4f && l.y < Ceiling;
        }

        void OnDodge(Combatant c)
        {
            if (!PlayerInside || c == null || c != Player) return;
            _ex.Dodge();
            if (Kind != HazardKind.Quicksand) return;
            var pc = c.GetComponent<PlayerController>(); if (pc == null) return;
            var l = transform.InverseTransformPoint(c.transform.position);
            var dir = Mathf.Abs(l.x) / Size.x > Mathf.Abs(l.z) / Size.y ? new Vector3(Mathf.Sign(l.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(l.z));
            pc.AddImpulse(transform.TransformDirection(dir) * HazardRules.DodgeEscape * 6f);
        }

        static Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        void Update()
        {
            var p = Player; if (p == null || p.IsDead) { PlayerInside = false; return; }
            var pos = p.transform.position;
            bool inside = Contains(pos);
            var active = PlayerKeys.Active();
            if (!inside && Contains(pos, 6f) && Time.time >= _hintAt && !HazardRules.Protected(Kind, active))
            {
                _hintAt = Time.time + 12f;
                GameEventBus.Notify(HazardRules.Spec(Kind).Name + " – " + HazardRules.Requirement(Kind), HazardRules.Spec(Kind).EdgeColor);
            }
            PlayerInside = inside;
            var t = _ex.Tick(Time.deltaTime, inside, active);
            LastSpeed = t.SpeedFactor;
            if (!inside) return;
            var pc = p.GetComponent<PlayerController>();
            if (pc != null && t.SpeedFactor < 1f) { pc.HazardSpeed = Mathf.Min(pc.HazardFrame == Time.frameCount ? pc.HazardSpeed : 1f, t.SpeedFactor); pc.HazardFrame = Time.frameCount; }
            if (t.Stacks > 0) p.Effects.Apply(t.Condition, t.Stacks, 3f);
            if (t.HpFraction > 0f) p.TakeDamage(new DamageInfo { Amount = p.MaxHealth * t.HpFraction, IsCondition = true, Condition = t.Condition, Point = pos });
            if (t.Pushback && pc != null) pc.AddImpulse(transform.forward * 9f, 0.5f);
            if (t.Sinking && Time.time >= _sinkHintAt) { _sinkHintAt = Time.time + 3f; GameEventBus.Notify(Kind == HazardKind.IceWater ? "Eiskalt – du gehst unter! Zurück zum Ufer!" : "Du versinkst – zurück zum Rand (Ausweichen hilft)!", new Color(1f, 0.55f, 0.4f)); }
            if (t.Dead) p.Die(null);
        }

        public static HazardSurface Nearest(Vector3 p)
        {
            HazardSurface best = null; float bd = float.MaxValue;
            foreach (var h in All) { float d = (h.transform.position - p).sqrMagnitude; if (d < bd) { bd = d; best = h; } }
            return best;
        }
    }
}
