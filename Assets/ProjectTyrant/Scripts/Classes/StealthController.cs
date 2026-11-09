using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M25: Tarnung für den Spieler (alle Klassen): Effekt „Tarnung“ (Gegner verlieren das Ziel und suchen am letzten Punkt),
    /// ein Angriff beendet sie und gibt 3 s „Enthüllt“; Tarnungs-Optik über einen durchscheinenden Verzerrungs-Look.
    /// </summary>
    public class StealthController : MonoBehaviour
    {
        public readonly StealthModel Model = new StealthModel();
        Combatant _self; Renderer[] _renderers; MaterialPropertyBlock _mpb; bool _shown;
        public int Entered { get; private set; }

        void Awake() { _self = GetComponent<Combatant>(); _mpb = new MaterialPropertyBlock(); }
        void OnEnable() { GameEventBus.DamageDealt += OnDamage; }
        void OnDisable() { GameEventBus.DamageDealt -= OnDamage; }

        public static bool TryEnter(Combatant c, float seconds)
        {
            var s = c != null ? c.GetComponent<StealthController>() : null;
            if (s != null) return s.Enter(seconds);
            if (c != null && !c.Effects.Has(EffectType.Revealed)) { c.Effects.Apply(new EffectApplication(EffectType.Stealth, 1, seconds), c); return true; }
            return false;
        }

        public bool Enter(float seconds)
        {
            if (_self == null) return false;
            if (!Model.Enter(seconds)) { GameEventBus.Notify("Enthüllt – keine Tarnung möglich", new Color(1f, 0.6f, 0.5f)); return false; }
            _self.Effects.Apply(new EffectApplication(EffectType.Stealth, 1, seconds), _self);
            Entered++;
            _self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.Stealth, Self = _self, Position = transform.position });
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Veil, VfxPalette.Schatten, VfxSize.M), transform.position + Vector3.up, Quaternion.identity, null, 0.75f);
            GameAudio.Play("Effect/Stealth", transform.position);
            return true;
        }

        /// <summary>Tarnungsangriff/Angriff: Tarnung endet, Enthüllt 3 s.</summary>
        public void Reveal()
        {
            if (!Model.Attack()) return;
            _self.Effects.Remove(EffectType.Stealth);
            _self.Effects.Apply(new EffectApplication(EffectType.Revealed, 1, StealthModel.RevealedDuration), _self);
        }

        void OnDamage(DamageInfo d)
        {
            if (d.Source == _self && !d.IsCondition && Model.InStealth) Reveal();
        }

        void Update()
        {
            Model.Tick(Time.deltaTime);
            if (Model.InStealth && _self != null && !_self.Effects.Has(EffectType.Stealth)) Model.Attack();   // extern beendet
            bool show = Model.InStealth;
            if (show != _shown)
            {
                _shown = show;
                if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in _renderers)
                {
                    if (r == null || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                    r.GetPropertyBlock(_mpb);
                    if (show) { _mpb.SetColor("_BaseColor", new Color(0.35f, 0.3f, 0.45f, 1f)); _mpb.SetColor("_EmissionColor", new Color(0.12f, 0.08f, 0.2f)); }
                    else { _mpb.Clear(); }
                    r.SetPropertyBlock(_mpb);
                }
            }
        }
    }

    /// <summary>Lauf 5 / M25: Falle (Wildhüter): löst aus, sobald ein Gegner den Radius betritt; Lebensdauer begrenzt.</summary>
    public class TrapZone : MonoBehaviour
    {
        SkillContext _ctx; float _life, _check; bool _armed;
        public static TrapZone Spawn(SkillContext ctx, Vector3 pos)
        {
            var go = new GameObject("Trap_" + ctx.Skill.DisplayName);
            go.transform.position = ZoneAnchors.GroundAt(pos);
            var t = go.AddComponent<TrapZone>(); t._ctx = ctx; t._life = Mathf.Max(5f, ctx.Skill.TrapLifetime); t._armed = true;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Sigil, ctx.Skill.Palette, VfxSize.S), go.transform.position, Quaternion.identity, null, 0.6f);
            return t;
        }

        void Update()
        {
            _life -= Time.deltaTime; if (_life <= 0f || _ctx.Caster == null) { Destroy(gameObject); return; }
            _check -= Time.deltaTime; if (_check > 0f || !_armed) return; _check = 0.2f;
            float r = _ctx.Skill.Radius;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || !_ctx.Caster.IsHostileTo(c)) continue;
                if ((c.transform.position - transform.position).sqrMagnitude > r * r) continue;
                _armed = false;
                var ctx = _ctx; ctx.GroundPoint = transform.position; ctx.Target = c;
                SkillExecutor.TriggerArea(ctx, transform.position);
                SkillFx.Vfx(VfxIds.Compose(VfxShape.Shockwave, _ctx.Skill.Palette, VfxSize.M), transform.position, Quaternion.identity, null, 0.8f);
                Destroy(gameObject, 0.1f);
                return;
            }
        }
    }
}
