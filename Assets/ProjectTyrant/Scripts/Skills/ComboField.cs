using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Combo-Feld am Boden (Feuer/Wasser/Blitz/Rauch). Pulsiert 1x/s und kann von Finishern ausgelöst werden.</summary>
    public class ComboFieldZone : MonoBehaviour
    {
        public static readonly List<ComboFieldZone> Active = new List<ComboFieldZone>();

        public ComboFieldType Field;
        public float Radius = 4f;
        public float Remaining = 5f;
        public float TickDamage;
        public Combatant Owner;

        float _pulse;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int PulseId2 = Shader.PropertyToID("_Pulse");
        float _alpha = 1f; bool _rimOnly;
        public bool RimOnly => _rimOnly; public float DisplayAlpha => _alpha;

        public static Color FieldColor(ComboFieldType f)
        {
            switch (f)
            {
                case ComboFieldType.Fire: return new Color(Palette.Fire.r, Palette.Fire.g, Palette.Fire.b, 0.55f);
                case ComboFieldType.Water: return new Color(Palette.Water.r, Palette.Water.g, Palette.Water.b, 0.55f);
                case ComboFieldType.Lightning: return new Color(Palette.Lightning.r, Palette.Lightning.g, Palette.Lightning.b, 0.6f);
                case ComboFieldType.Smoke: return new Color(0.35f, 0.35f, 0.4f, 0.7f);
            }
            return new Color(1, 1, 1, 0.4f);
        }

        public static string FieldName(ComboFieldType f)
        {
            switch (f)
            {
                case ComboFieldType.Fire: return "Feuer";
                case ComboFieldType.Water: return "Wasser";
                case ComboFieldType.Lightning: return "Blitz";
                case ComboFieldType.Smoke: return "Rauch";
            }
            return "-";
        }

        public static ComboFieldZone Spawn(ComboFieldType type, Vector3 position, float radius, float duration, Combatant owner, float tickDamage)
        {
            var go = new GameObject("ComboField_" + type);
            go.transform.position = position + Vector3.up * 0.05f;
            var f = go.AddComponent<ComboFieldZone>();
            f.Field = type; f.Radius = radius; f.Remaining = duration; f.Owner = owner; f.TickDamage = tickDamage;
            var lib = VfxLibrary.Current;
            if (lib != null && lib.GroundMarker != null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(go.transform, false);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                f._renderer = quad.GetComponent<Renderer>();
                f._renderer.sharedMaterial = lib.GroundMarker;
                f._mpb = new MaterialPropertyBlock();
                // Fix-Lauf F1: „Nur Effekt“ → dünner, pulsierender Elementrand statt gefüllter Scheibe (Feld bleibt erkennbar); Verbündete reduziert/aus
                var vis = AreaDisplay.Current(AreaDisplay.FromTeam(owner != null ? owner.Team : Team.Enemy));
                f._alpha = vis.Effect ? vis.Alpha : 0f; f._rimOnly = !vis.Outline;
                f._mpb.SetColor(ColorId, FieldColor(type) * new Color(1f, 1f, 1f, f._alpha));
                f._mpb.SetFloat(FillId, f._rimOnly ? 0f : 1f);
                f._mpb.SetFloat(PulseId2, f._rimOnly ? 1f : 0f);
                f._renderer.SetPropertyBlock(f._mpb);
                if (f._alpha <= 0f) f._renderer.enabled = false;
            }
            return f;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public bool Contains(Vector3 p)
        {
            Vector3 d = p - transform.position; d.y = 0f;
            return d.sqrMagnitude <= Radius * Radius && Mathf.Abs(p.y - transform.position.y) < 4f;
        }

        public static ComboFieldZone FindAt(Vector3 p)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] != null && Active[i].Contains(p)) return Active[i];
            return null;
        }

        void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f) { Destroy(gameObject); return; }
            _pulse += Time.deltaTime;
            if (_pulse >= 1f) { _pulse -= 1f; Pulse(); }
            if (_renderer != null && Remaining < 1f)
            {
                var c = FieldColor(Field); c.a *= Remaining * _alpha;
                _mpb.SetColor(ColorId, c);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        void Pulse()
        {
            if (Owner == null) return;
            for (int i = Combatant.All.Count - 1; i >= 0; i--)
            {
                var c = Combatant.All[i];
                if (c == null || c.IsDead || !Contains(c.transform.position)) continue;
                bool hostile = Owner.IsHostileTo(c);
                switch (Field)
                {
                    case ComboFieldType.Fire:
                        if (hostile) c.Effects.Apply(EffectType.Burning, 1, 2f, Owner);
                        break;
                    case ComboFieldType.Lightning:
                        if (hostile && TickDamage > 0) DamageCalculator.DealDirect(Owner, c, TickDamage);
                        break;
                    case ComboFieldType.Water:
                        if (!hostile) DamageCalculator.Heal(Owner, c, 300f, 0.1f);
                        break;
                }
            }
        }
    }

    /// <summary>Löst Finisher gegen Felder auf und wendet das Combo-Ergebnis an.</summary>
    public static class ComboResolver
    {
        public static bool TryFinisher(ComboTable table, ComboFinisherType finisher, Vector3 position, Combatant caster, out ComboEntry entry)
        {
            entry = null;
            if (table == null || finisher == ComboFinisherType.None || caster == null) return false;
            var field = ComboFieldZone.FindAt(position);
            if (field == null) return false;
            if (!table.TryGet(field.Field, finisher, out entry)) return false;
            if (finisher != ComboFinisherType.Projectile) ApplyArea(entry, position, caster);
            GameEventBus.RaiseCombo(entry, position);
            GameEventBus.Notify("Combo: " + entry.ResultName, ComboFieldZone.FieldColor(entry.Field) * 1.4f);
            return true;
        }

        public static void ApplyArea(ComboEntry e, Vector3 pos, Combatant caster)
        {
            float r2 = e.Radius * e.Radius;
            for (int i = Combatant.All.Count - 1; i >= 0; i--)
            {
                var c = Combatant.All[i];
                if (c == null || c.IsDead) continue;
                Vector3 d = c.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude > r2) continue;
                bool hostile = caster.IsHostileTo(c);
                switch (e.ResultType)
                {
                    case ComboResultType.AreaBuffAllies: if (!hostile) c.Effects.Apply(e.Buff, caster); break;
                    case ComboResultType.AreaHealAllies: if (!hostile) { DamageCalculator.Heal(caster, c, e.Amount, 0.5f); if (e.Cleanse > 0) c.Effects.Cleanse(e.Cleanse); } break;
                    case ComboResultType.AreaDebuffEnemies: if (hostile) c.Effects.Apply(e.Buff, caster); break;
                    case ComboResultType.AreaDamageEnemies:
                        if (hostile)
                        {
                            DamageCalculator.DealDirect(caster, c, e.Coefficient > 0f ? e.Coefficient : e.Amount / 1000f);
                            if (e.Buff.Stacks > 0) c.Effects.Apply(e.Buff, caster);
                        }
                        break;
                }
            }
        }

        /// <summary>Projektil-Finisher: wird beim Treffer angewendet.</summary>
        public static void ApplyProjectile(ComboEntry e, Combatant caster, Combatant hit)
        {
            if (e == null) return;
            if (e.ResultType == ComboResultType.ProjectileOnHitDebuff && hit != null) hit.Effects.Apply(e.Buff, caster);
            else if (e.ResultType == ComboResultType.ProjectileOnHitSelfBuff && caster != null) caster.Effects.Apply(e.Buff, caster);
        }
    }
}
