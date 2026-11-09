using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M22: spielt die VFX-/SFX-Spuren der Zeitachse eines Zauberers ab: Aufladung an der Hand (Aktivierungsbeginn), Auswurf
    /// (erster Treffer), Waffen-Trail während Nahkampf-Schlägen, Einschlag am Trefferpunkt, Nachwirkung.
    /// </summary>
    public class SkillFxDirector : MonoBehaviour
    {
        public Transform Hand;
        public TrailRenderer[] Trails = new TrailRenderer[0];
        SkillCaster _caster; Combatant _self;
        float _trailOff;

        void Awake() { _caster = GetComponent<SkillCaster>(); _self = GetComponent<Combatant>(); SetTrails(false); }

        void OnEnable()
        {
            if (_caster != null) { _caster.CastStarted += OnStarted; _caster.CastReleased += OnReleased; }
            GameEventBus.DamageDealt += OnDamage;
        }

        void OnDisable()
        {
            if (_caster != null) { _caster.CastStarted -= OnStarted; _caster.CastReleased -= OnReleased; }
            GameEventBus.DamageDealt -= OnDamage;
        }

        float Intensity => _self != null ? SkillFx.TeamIntensity(_self.Team) : 1f;
        float VolDb => _self != null ? SkillFx.TeamVolumeDb(_self.Team) : 0f;
        Vector3 HandPos => Hand != null ? Hand.position : transform.position + Vector3.up * 1.2f + transform.forward * 0.4f;

        void OnStarted(SkillDefinition s)
        {
            var tl = s.Timeline; if (tl == null || !tl.IsBuilt) return;
            SkillFx.Vfx(tl.VfxCharge, HandPos, transform.rotation, Hand, Intensity);
            SkillFx.Sfx(tl.SfxCharge, HandPos, VolDb);
            if (s.Delivery == SkillDelivery.Melee || s.Delivery == SkillDelivery.Leap) { SetTrails(true); _trailOff = Time.time + Mathf.Max(0.25f, tl.Total * (_self != null && _self.Effects.Has(EffectType.Quickness) ? EffectRules.QuicknessCastFactor : 1f)); }
        }

        void OnReleased(SkillDefinition s)
        {
            var tl = s.Timeline; if (tl == null || !tl.IsBuilt) return;
            var pos = s.Delivery == SkillDelivery.Self ? transform.position : HandPos;
            SkillFx.Vfx(tl.VfxRelease, pos, transform.rotation, null, Intensity);
            SkillFx.Sfx(tl.SfxRelease, pos, VolDb);
            if (!string.IsNullOrEmpty(tl.VfxAfter) && s.Field == ComboFieldType.None) SkillFx.Vfx(tl.VfxAfter, transform.position, Quaternion.identity, transform, Intensity);
            // kurzer Boden-Kreis beim Wirken von Flächen-Skills (Reichweite/Radius lesbar)
            if (s.Delivery == SkillDelivery.GroundTarget || (s.Delivery == SkillDelivery.Self && s.DealsDamage && s.Radius >= 2.5f))
                FlashCircle(s.Delivery == SkillDelivery.GroundTarget && _caster != null ? _caster.LastGroundPoint : transform.position, s.Radius, SkillFx.PaletteColor(s.Palette));
        }

        GameObject _circle; Renderer _circleR; MaterialPropertyBlock _mpb; float _circleT;
        void FlashCircle(Vector3 pos, float radius, Color c)
        {
            var lib = VfxLibrary.Current; if (lib == null || lib.GroundMarker == null) return;
            if (_circle == null)
            {
                _circle = GameObject.CreatePrimitive(PrimitiveType.Quad); _circle.name = "CastCircle"; Destroy(_circle.GetComponent<Collider>());
                _circle.transform.rotation = Quaternion.Euler(90f, 0f, 0f); _circleR = _circle.GetComponent<Renderer>(); _circleR.sharedMaterial = lib.GroundMarker; _mpb = new MaterialPropertyBlock();
                _circleR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _circle.SetActive(true); _circle.transform.position = pos + Vector3.up * 0.08f; _circle.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            c.a = 0.7f; _circleR.GetPropertyBlock(_mpb); _mpb.SetColor("_Color", c); _mpb.SetFloat("_Fill", 1f); _mpb.SetFloat("_Pulse", 0f); _circleR.SetPropertyBlock(_mpb);
            _circleT = 0.4f;
        }

        void OnDestroy() { if (_circle != null) Destroy(_circle); }

        void OnDamage(DamageInfo d)
        {
            if (d.Source != _self || d.Skill == null || d.IsCondition || d.Evaded) return;
            var tl = d.Skill.Timeline; if (tl == null || !tl.IsBuilt) return;
            SkillFx.Vfx(tl.VfxImpact, d.Point, Quaternion.identity, null, Intensity);
            SkillFx.Sfx(tl.SfxImpact, d.Point, VolDb);
        }

        void Update()
        {
            if (_trailOff > 0f && Time.time >= _trailOff) { _trailOff = 0f; SetTrails(false); }
            if (_circleT > 0f) { _circleT -= Time.deltaTime; if (_circleT <= 0f && _circle != null) _circle.SetActive(false); }
        }

        void SetTrails(bool on) { foreach (var t in Trails) if (t != null) t.emitting = on; }
    }
}
