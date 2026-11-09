using System.Collections;
using UnityEngine;

namespace ProjectTyrant
{
    public struct SkillContext
    {
        public Combatant Caster;
        public SkillDefinition Skill;
        public Combatant Target;
        public Vector3 GroundPoint;
        public Vector3 Forward;
        public ComboTable Table;
        public MonoBehaviour Runner;
        public PlayerController Mover;
    }

    /// <summary>Führt die Wirkung eines Skills aus (Schaden, Buffs, Felder, Finisher).</summary>
    public static class SkillExecutor
    {
        public static void Execute(SkillContext ctx)
        {
            var s = ctx.Skill;
            if (s == null || ctx.Caster == null) return;
            GameEventBus.RaiseSkillCast(ctx.Caster, s);

            // Verwirrung: Schaden beim Fähigkeitseinsatz
            int conf = ctx.Caster.Effects.GetStacks(EffectType.Confusion);
            if (conf > 0) ApplyConfusion(ctx.Caster, conf);

            if (s.HealBase > 0f && s.Special == SkillSpecial.None) DamageCalculator.Heal(ctx.Caster, ctx.Caster, s.HealBase, s.HealCoefficient);
            foreach (var b in s.SelfBuffs) ctx.Caster.Effects.Apply(b, ctx.Caster);
            if (s.ShareWithAllies) ShareBoons(ctx);
            if (s.CleanseCount > 0) ctx.Caster.Effects.Cleanse(s.CleanseCount);
            if (s.SlotType == SkillSlotType.Heal)
            {
                GameEventBus.RaiseHealSkill(ctx.Caster, s);
                ctx.Caster.Modifiers.Trigger(new TriggerContext { Type = TriggerType.HealSkill, Self = ctx.Caster, Skill = s, Position = ctx.Caster.transform.position });
            }
            ctx.Caster.Modifiers.Trigger(new TriggerContext { Type = TriggerType.SkillUse, Self = ctx.Caster, Target = ctx.Target, Skill = s, Position = ctx.Caster.transform.position });

            // Lauf 5 / M25: Klassen-Spezialwirkungen (Tarnung, Schattenschritt, Blitzschritt, Falle, Beschwörung)
            if (s.StealthSeconds > 0f)
            {
                StealthController.TryEnter(ctx.Caster, s.StealthSeconds);
                if (s.ShareWithAllies) foreach (var a in Combatant.All) if (a != null && a != ctx.Caster && !a.IsDead && a.Team == ctx.Caster.Team && (a.transform.position - ctx.Caster.transform.position).sqrMagnitude < s.Radius * s.Radius) StealthController.TryEnter(a, s.StealthSeconds);
            }
            switch (s.Special)
            {
                case SkillSpecial.ShadowStep:
                {
                    Vector3 dest = ctx.Target != null && s.Delivery != SkillDelivery.GroundTarget ? ctx.Target.transform.position - (ctx.Target.transform.position - ctx.Caster.transform.position).normalized * 1.2f : ctx.GroundPoint;
                    Teleport(ctx, dest);
                    if (s.Delivery == SkillDelivery.GroundTarget) return;   // Infiltrationspfeil: nur Schritt
                    break;
                }
                case SkillSpecial.Blink:
                {
                    var dest = ctx.Caster.transform.position + ctx.Forward.normalized * Mathf.Max(3f, s.LeapDistance);
                    Teleport(ctx, ZoneAnchors.GroundAt(dest));
                    if (s.DealsDamage || s.TargetBuffs.Count > 0) AreaDamage(ctx, ctx.Caster.transform.position, s.Radius);
                    return;
                }
                case SkillSpecial.TrapPlace:
                    TrapZone.Spawn(ctx, ctx.Caster.transform.position);
                    return;
                case SkillSpecial.Conjure:
                {
                    var host = ctx.Caster.GetComponent<ClassMechanicHost>();
                    if (host != null && s.ConjureSet != null) host.Conjure(s.ConjureSet, 20f);
                    return;
                }
            }

            if (s.Special == SkillSpecial.DownedSelfHeal)
            {
                var d = ctx.Caster.GetComponent<DownedController>();
                if (d != null) d.HealDowned(DamageFormula.Heal(DamageFormula.ScaledBase(s.HealBase, ctx.Caster.Level), ctx.Caster.Stats.Get(Attr.HealingPower), s.HealCoefficient));
            }

            switch (s.Delivery)
            {
                case SkillDelivery.Melee:
                    if (ctx.Runner != null) ctx.Runner.StartCoroutine(MeleeRoutine(ctx));
                    else MeleeHit(ctx, 0);
                    break;
                case SkillDelivery.Self:
                    SpawnField(ctx, ctx.Caster.transform.position);
                    if (s.DealsDamage || s.TargetBuffs.Count > 0) AreaDamage(ctx, ctx.Caster.transform.position, s.Radius);
                    if (s.Finisher == ComboFinisherType.Blast) ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Blast, ctx.Caster.transform.position, ctx.Caster, out _);
                    VfxPulse(ctx.Caster.transform.position, s.Radius, s.IconColor);
                    break;
                case SkillDelivery.GroundTarget:
                    SpawnField(ctx, ctx.GroundPoint);
                    if (s.DealsDamage || s.TargetBuffs.Count > 0) AreaDamage(ctx, ctx.GroundPoint, s.Radius);
                    if (s.Finisher == ComboFinisherType.Blast) ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Blast, ctx.GroundPoint, ctx.Caster, out _);
                    VfxPulse(ctx.GroundPoint, s.Radius, s.IconColor);
                    break;
                case SkillDelivery.Projectile:
                    if (ctx.Target != null) Projectile.Launch(ctx.Caster, ctx.Target, s, ctx.Table, s.IconColor);
                    break;
                case SkillDelivery.Leap:
                    if (ctx.Runner != null) ctx.Runner.StartCoroutine(LeapRoutine(ctx));
                    break;
            }
        }

        static void Teleport(SkillContext ctx, Vector3 dest)
        {
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Veil, ctx.Skill.Palette, VfxSize.S), ctx.Caster.transform.position + Vector3.up, Quaternion.identity, null, 0.7f);
            if (ctx.Mover != null) ctx.Mover.Teleport(ZoneAnchors.GroundAt(dest), ctx.Caster.transform.rotation);
            else ctx.Caster.transform.position = ZoneAnchors.GroundAt(dest);
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Veil, ctx.Skill.Palette, VfxSize.S), ctx.Caster.transform.position + Vector3.up, Quaternion.identity, null, 0.7f);
        }

        /// <summary>Lauf 5 / M25: Flächenwirkung an einem Punkt (Fallen): Feld, Schaden/Effekte, Explosions-Finisher.</summary>
        public static void TriggerArea(SkillContext ctx, Vector3 pos)
        {
            var s = ctx.Skill;
            SpawnField(ctx, pos);
            AreaDamage(ctx, pos, s.Radius);
            if (s.Finisher == ComboFinisherType.Blast) ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Blast, pos, ctx.Caster, out _);
        }

        static void SpawnField(SkillContext ctx, Vector3 pos)
        {
            var s = ctx.Skill;
            if (s.Field == ComboFieldType.None) return;
            ComboFieldZone.Spawn(s.Field, ZoneAnchors.GroundAt(pos), s.FieldRadius, s.FieldDuration, ctx.Caster, s.FieldTickCoefficient);
        }

        static IEnumerator MeleeRoutine(SkillContext ctx)
        {
            var s = ctx.Skill;
            int hits = Mathf.Max(1, s.Hits);
            bool comboDone = false;
            for (int i = 0; i < hits; i++)
            {
                if (ctx.Caster == null || ctx.Caster.IsDead) yield break;
                MeleeHit(ctx, i);
                if (!comboDone && s.Finisher == ComboFinisherType.Whirl)
                    comboDone = ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Whirl, ctx.Caster.transform.position, ctx.Caster, out _);
                if (i < hits - 1) yield return new WaitForSeconds(s.HitInterval);
            }
        }

        static void MeleeHit(SkillContext ctx, int index)
        {
            var s = ctx.Skill;
            bool whirl = s.Finisher == ComboFinisherType.Whirl || s.Hits > 1;
            Vector3 origin = ctx.Caster.transform.position;
            Vector3 fwd = ctx.Caster.transform.forward;
            float reach = Mathf.Max(s.Range, s.Radius);
            int hitCount = 0;
            for (int i = Combatant.All.Count - 1; i >= 0; i--)
            {
                var c = Combatant.All[i];
                if (c == null || c.IsDead || !ctx.Caster.IsHostileTo(c)) continue;
                Vector3 to = c.transform.position - origin; to.y = 0f;
                if (to.magnitude > reach + 0.5f) continue;
                if (!whirl && to.sqrMagnitude > 0.25f && Vector3.Angle(fwd, to) > 70f) continue;
                Strike(ctx, c);
                if (++hitCount >= Mathf.Max(1, s.TargetCount)) break;
            }
            if (s.Special == SkillSpecial.Interrupt && ctx.Target != null && !ctx.Target.IsDead &&
                Vector3.Distance(ctx.Target.transform.position, origin) <= reach + 1f)
                Interrupt(ctx.Caster, ctx.Target);
            VfxSwing(ctx.Caster, reach, s.IconColor, whirl);
        }

        static void Strike(SkillContext ctx, Combatant c)
        {
            var s = ctx.Skill;
            if (s.DealsDamage) DamageCalculator.DealDirect(ctx.Caster, c, s.Coefficient, s);
            foreach (var b in s.TargetBuffs) c.Effects.Apply(b, ctx.Caster);
            if (s.Special == SkillSpecial.Interrupt) Interrupt(ctx.Caster, c);
        }

        static void ApplyConfusion(Combatant c, int stacks)
        {
            Combatant src = null;
            foreach (var st in c.Effects.Stacks) if (st.Type == EffectType.Confusion) { src = st.Source; break; }
            float cd = src != null ? src.Stats.Get(Attr.ConditionDamage) : 0f;
            int lvl = src != null ? src.Level : c.Level;
            c.TakeDamage(new DamageInfo { Source = src, Target = c, Amount = DamageFormula.ConfusionOnSkill(cd, lvl) * stacks, IsCondition = true, Point = c.AimPoint });
        }

        static void ShareBoons(SkillContext ctx)
        {
            var s = ctx.Skill;
            float r2 = Mathf.Max(s.Radius, 6f); r2 *= r2;
            int shared = 0;
            for (int i = Combatant.All.Count - 1; i >= 0 && shared < s.TargetCount; i--)
            {
                var c = Combatant.All[i];
                if (c == null || c == ctx.Caster || c.IsDead || ctx.Caster.IsHostileTo(c) || c.Team == Team.Neutral) continue;
                if ((c.transform.position - ctx.Caster.transform.position).sqrMagnitude > r2) continue;
                foreach (var b in s.SelfBuffs) if (EffectLibrary.IsBoon(b.Type)) c.Effects.Apply(b, ctx.Caster);
                shared++;
            }
        }

        public static void Interrupt(Combatant source, Combatant target)
        {
            if (target.Effects.Has(EffectType.Stability)) { GameEventBus.Notify("Stabilität!", new Color(0.8f, 0.8f, 1f)); return; }
            target.Effects.Apply(EffectType.Stun, 1, 1.5f, source);
            var ai = target.GetComponent<NpcBrain>();
            if (ai != null) ai.InterruptAttack();
            GameEventBus.Notify("Unterbrochen!", new Color(1f, 1f, 0.5f));
        }

        static void AreaDamage(SkillContext ctx, Vector3 center, float radius)
        {
            var s = ctx.Skill;
            float r2 = radius * radius;
            int hits = 0;
            for (int i = Combatant.All.Count - 1; i >= 0; i--)
            {
                var c = Combatant.All[i];
                if (c == null || c.IsDead || !ctx.Caster.IsHostileTo(c)) continue;
                Vector3 d = c.transform.position - center; d.y = 0f;
                if (d.sqrMagnitude > r2) continue;
                Strike(ctx, c);
                if (++hits >= Mathf.Max(1, s.TargetCount)) break;
            }
        }

        static IEnumerator LeapRoutine(SkillContext ctx)
        {
            var s = ctx.Skill;
            var caster = ctx.Caster;
            Vector3 start = caster.transform.position;
            Vector3 dest;
            if (ctx.Target != null)
            {
                Vector3 to = ctx.Target.transform.position - start; to.y = 0f;
                float dist = Mathf.Min(s.LeapDistance, Mathf.Max(0f, to.magnitude - 1.2f));
                dest = start + to.normalized * dist;
            }
            else dest = start + ctx.Forward * s.LeapDistance;
            bool startCombo = ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Leap, start, caster, out _);
            float dur = 0.35f, t = 0f;
            Vector3 prev = start;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                Vector3 p = Vector3.Lerp(start, dest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;
                if (ctx.Mover != null) ctx.Mover.MoveBy(p - prev); else caster.transform.position = p;
                prev = p;
                yield return null;
            }
            if (caster == null || caster.IsDead) yield break;
            AreaDamage(ctx, caster.transform.position, s.Radius);
            if (!startCombo) ComboResolver.TryFinisher(ctx.Table, ComboFinisherType.Leap, caster.transform.position, caster, out _);
            VfxPulse(caster.transform.position, s.Radius, s.IconColor);
        }

        // ---------- einfache VFX (prozedural) ----------
        public static void VfxPulse(Vector3 pos, float radius, Color color)
        {
            var lib = VfxLibrary.Current;
            if (lib == null || lib.GroundMarker == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "VfxPulse";
            go.transform.position = ZoneAnchors.GroundAt(pos) + Vector3.up * 0.08f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = lib.GroundMarker;
            var fx = go.AddComponent<VfxFade>();
            fx.Init(r, new Color(color.r, color.g, color.b, 0.8f), radius * 2f, 0.4f, true);
        }

        static void VfxSwing(Combatant caster, float reach, Color color, bool full)
        {
            var lib = VfxLibrary.Current;
            if (lib == null || lib.GroundMarker == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "VfxSwing";
            go.transform.position = caster.transform.position + Vector3.up * 0.9f + (full ? Vector3.zero : caster.transform.forward * reach * 0.5f);
            go.transform.rotation = Quaternion.LookRotation(Vector3.down, caster.transform.forward);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = lib.GroundMarker;
            var fx = go.AddComponent<VfxFade>();
            fx.Init(r, new Color(color.r, color.g, color.b, 0.7f), full ? reach * 2f : reach * 1.2f, 0.18f, false);
        }
    }

    /// <summary>Kurzlebiger Ring-/Flash-Effekt.</summary>
    public class VfxFade : MonoBehaviour
    {
        Renderer _r; MaterialPropertyBlock _mpb; Color _c; float _size, _life, _t; bool _grow;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int FillId = Shader.PropertyToID("_Fill");

        public void Init(Renderer r, Color c, float size, float life, bool grow)
        {
            _r = r; _c = c; _size = size; _life = life; _grow = grow; _mpb = new MaterialPropertyBlock();
            Apply(0f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            Apply(k);
            if (k >= 1f) Destroy(gameObject);
        }

        void Apply(float k)
        {
            float s = _grow ? Mathf.Lerp(_size * 0.3f, _size, Mathf.Sqrt(k)) : _size;
            transform.localScale = new Vector3(s, s, 1f);
            var c = _c; c.a *= 1f - k;
            _mpb.SetColor(ColorId, c);
            _mpb.SetFloat(FillId, _grow ? 0.2f : 1f);
            _r.SetPropertyBlock(_mpb);
        }
    }
}
