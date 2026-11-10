using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Boden-Marker, der sich über Delay füllt und dann im Radius Schaden verursacht. Durch Dodge (i-Frames) oder Herauslaufen vermeidbar.</summary>
    public class GroundTelegraph : MonoBehaviour
    {
        public Combatant Owner;
        public float Radius = 5f;
        public float Delay = 1.5f;
        public float Damage = 6000f;
        public float Coefficient;
        /// <summary>Lauf 4: zusätzliche Wirkung je getroffenem Ziel (Zustand, Rückstoß).</summary>
        public System.Action<Combatant> OnHit;
        public Color PulseColor = new Color(1f, 0.35f, 0.1f);
        public float Elapsed { get; private set; }
        public bool Resolved { get; private set; }

        Renderer _r; MaterialPropertyBlock _mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        /// <summary>Lauf 5 / M24: Farbenblind-Modus (Optionen) – Gelb → Blau statt Orange → Rot.</summary>
        public static bool Colorblind;
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");

        public static GroundTelegraph Spawn(Combatant owner, Vector3 pos, float radius, float delay, float damage, float coefficient = 0f)
        {
            var go = new GameObject("Telegraph");
            go.transform.position = ZoneAnchors.GroundAt(pos) + Vector3.up * 0.06f;
            var t = go.AddComponent<GroundTelegraph>();
            t.Owner = owner; t.Radius = radius; t.Delay = delay; t.Damage = damage; t.Coefficient = coefficient;
            var lib = VfxLibrary.Current;
            if (lib != null && lib.GroundMarker != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(go.transform, false);
                q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                t._r = q.GetComponent<Renderer>();
                t._r.sharedMaterial = lib.GroundMarker;
                t._mpb = new MaterialPropertyBlock();
            }
            t.UpdateVisual();
            return t;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (Resolved) return;
            Elapsed += dt;
            UpdateVisual();
            if (Elapsed >= Delay) Resolve();
        }

        public void Cancel() { Resolved = true; if (Application.isPlaying) Destroy(gameObject); }

        void Resolve()
        {
            Resolved = true;
            if (Owner != null && !Owner.IsDead)
            {
                Vector3 c = transform.position;
                for (int i = Combatant.All.Count - 1; i >= 0; i--)
                {
                    var v = Combatant.All[i];
                    if (v == null || v.IsDead || !Owner.IsHostileTo(v)) continue;
                    Vector3 d = v.transform.position - c; d.y = 0f;
                    if (d.sqrMagnitude <= Radius * Radius)
                    {
                        if (Coefficient > 0f) DamageCalculator.DealDirect(Owner, v, Coefficient);
                        else if (Damage > 0f) v.TakeDamage(new DamageInfo { Source = Owner, Amount = Damage, Point = v.AimPoint });
                        OnHit?.Invoke(v);
                    }
                }
                SkillExecutor.VfxPulse(c, Radius * 1.1f, PulseColor);
            }
            if (Application.isPlaying) Destroy(gameObject, 0.05f);
        }

        /// <summary>Fix-Lauf F1: Option „Gegner-Telegraphen“ (Standard: immer anzeigen) – nur für gegnerische Telegraphen.</summary>
        public static bool ShowEnemyTelegraphs = true;

        void UpdateVisual()
        {
            if (_r == null) return;
            bool show = ShowEnemyTelegraphs || Owner == null || Owner.Team != Team.Enemy;
            if (_r.enabled != show) _r.enabled = show;
            float k = Mathf.Clamp01(Elapsed / Mathf.Max(0.01f, Delay));
            _mpb.SetColor(ColorId, Colorblind ? new Color(0.15f + 0.8f * (1 - k), 0.35f + 0.5f * (1 - k), 1f - 0.9f * (1 - k), 0.8f) : new Color(1f, 0.2f + 0.3f * (1 - k), 0.08f, 0.75f));
            _mpb.SetFloat(FillId, k);
            _mpb.SetFloat(PulseId, 1f);
            _r.SetPropertyBlock(_mpb);
        }
    }
}
