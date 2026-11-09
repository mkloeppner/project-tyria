using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Zielsuchendes Projektil. Erkennt durchflogene Combo-Felder (Projektil-Finisher).</summary>
    public class Projectile : MonoBehaviour
    {
        public Combatant Source;
        public Combatant Target;
        public SkillDefinition Skill;
        public float Speed = 30f;
        public float Damage;
        public ComboTable Table;
        public float Lifetime = 3f;
        ComboFieldType _crossedField = ComboFieldType.None;
        Vector3 _lastTargetPos;

        public static Projectile Launch(Combatant source, Combatant target, SkillDefinition skill, ComboTable table, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Projectile_" + (skill != null ? skill.DisplayName : "?");
            go.transform.localScale = Vector3.one * 0.35f;
            go.transform.position = source.AimPoint + source.transform.forward * 0.6f;
            var lib = VfxLibrary.Current;
            var r = go.GetComponent<Renderer>();
            if (lib != null && lib.ProjectileGlow != null)
            {
                r.sharedMaterial = lib.ProjectileGlow;
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_EmissionColor", color * 2.5f);
                r.SetPropertyBlock(mpb);
            }
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f; trail.startWidth = 0.25f; trail.endWidth = 0f;
            if (lib != null && lib.FlashSolid != null) trail.sharedMaterial = lib.FlashSolid;
            trail.startColor = color; trail.endColor = new Color(color.r, color.g, color.b, 0f);
            var p = go.AddComponent<Projectile>();
            p.Source = source; p.Target = target; p.Skill = skill; p.Table = table;
            p.Speed = skill != null ? skill.ProjectileSpeed : 30f;
            p.Damage = skill != null ? skill.Coefficient : 0f;
            p._lastTargetPos = target != null ? target.AimPoint : go.transform.position + source.transform.forward * 20f;
            // Lauf 5 / M23: Projektil-Spur der Skill-Zeitachse folgt dem Geschoss
            if (skill != null && skill.Timeline != null && !string.IsNullOrEmpty(skill.Timeline.VfxProjectile) && VfxPool.Instance != null)
                p._fx = VfxPool.Instance.Play(skill.Timeline.VfxProjectile, go.transform.position, go.transform.rotation, go.transform, SkillFx.TeamIntensity(source != null ? source.Team : Team.Enemy));
            return p;
        }

        VfxInstance _fx;
        void OnDestroy() { if (_fx != null && _fx.Active) _fx.Stop(); }

        void Update()
        {
            Lifetime -= Time.deltaTime;
            if (Target != null && !Target.IsDead) _lastTargetPos = Target.AimPoint;
            Vector3 to = _lastTargetPos - transform.position;
            float step = Speed * Time.deltaTime;
            if (to.magnitude <= step + 0.4f) { Hit(); return; }
            transform.position += to.normalized * step;
            if (_crossedField == ComboFieldType.None)
            {
                var f = ComboFieldZone.FindAt(new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z));
                if (f != null && f.Owner != null && Source != null && !f.Owner.IsHostileTo(Source)) _crossedField = f.Field;
            }
            if (Lifetime <= 0f) Destroy(gameObject);
        }

        void Hit()
        {
            if (Target != null && !Target.IsDead && Source != null)
            {
                if (Damage > 0f) DamageCalculator.DealDirect(Source, Target, Damage, Skill);
                if (Skill != null) foreach (var b in Skill.TargetBuffs) Target.Effects.Apply(b, Source);
                if (Skill != null && Skill.Finisher == ComboFinisherType.Projectile && _crossedField != ComboFieldType.None && Table != null
                    && Table.TryGet(_crossedField, ComboFinisherType.Projectile, out var entry))
                {
                    ComboResolver.ApplyProjectile(entry, Source, Target);
                    GameEventBus.RaiseCombo(entry, transform.position);
                    GameEventBus.Notify("Combo: " + entry.ResultName, ComboFieldZone.FieldColor(entry.Field) * 1.4f);
                }
            }
            Destroy(gameObject);
        }
    }
}
