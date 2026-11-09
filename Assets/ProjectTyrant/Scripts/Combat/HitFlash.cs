using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Hit-Feedback: kurzes Aufleuchten (Emission) beim Treffer + Lauf 5: Material-Tint bei Zuständen (Brennen orange, Kühle blau,
    /// Gift grün, Blutung rot mit Tropfen-Partikeln). Ein gemeinsamer MaterialPropertyBlock je Renderer, damit sich Flash und Tint nicht überschreiben.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class HitFlash : MonoBehaviour
    {
        public float Duration = 0.12f;
        public Color FlashColor = new Color(0.55f, 0.45f, 0.38f);   // Lauf 5: geskinnte Figuren ganz weiß bei 1,2 → gedämpft
        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        float _t, _pulse;
        Combatant _c;
        ParticleSystem _drips;
        Color _tint = Color.white, _appliedTint = Color.white;
        bool _flashOn, _dirty;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        static readonly int Base = Shader.PropertyToID("_BaseColor");

        public Color CurrentTint => _appliedTint;
        public static readonly Dictionary<EffectType, Color> TintColors = new Dictionary<EffectType, Color>
        {
            { EffectType.Burning, new Color(1f, 0.55f, 0.25f) }, { EffectType.Chill, new Color(0.55f, 0.75f, 1f) },
            { EffectType.Poison, new Color(0.55f, 0.95f, 0.4f) }, { EffectType.Bleeding, new Color(0.95f, 0.45f, 0.45f) },
        };

        /// <summary>Tint-Farbe aus aktiven Zuständen (rein; Priorität Brennen > Kühle > Gift > Blutung).</summary>
        public static Color TintFor(EffectModel fx)
        {
            if (fx == null) return Color.white;
            foreach (var t in new[] { EffectType.Burning, EffectType.Chill, EffectType.Poison, EffectType.Bleeding })
                if (fx.Has(t)) return TintColors[t];
            return Color.white;
        }

        void Awake()
        {
            _c = GetComponent<Combatant>();
            var list = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r is MeshRenderer || r is SkinnedMeshRenderer) list.Add(r);
            _renderers = list.ToArray();
            _mpb = new MaterialPropertyBlock();
            _c.Damaged += OnDamaged;
        }

        void OnDestroy() { if (_c != null) _c.Damaged -= OnDamaged; }

        void OnDamaged(DamageInfo d)
        {
            if (d.Evaded || d.Amount <= 0f || d.IsCondition) return;
            _t = Duration; _flashOn = true; _dirty = true;
        }

        void Update()
        {
            if (_t > 0f) { _t -= Time.deltaTime; if (_t <= 0f) { _flashOn = false; _dirty = true; } }
            var tint = _c != null && !_c.IsDead ? TintFor(_c.Effects) : Color.white;
            if (tint != Color.white) { _pulse += Time.deltaTime * 4f; tint = Color.Lerp(Color.white, tint, 0.55f + 0.25f * Mathf.Sin(_pulse)); _dirty = true; }
            else if (_appliedTint != Color.white) _dirty = true;
            _tint = tint;
            bool bleed = _c != null && !_c.IsDead && _c.Effects.Has(EffectType.Bleeding);
            UpdateDrips(bleed);
            if (_dirty) Apply();
        }

        void Apply()
        {
            _dirty = false; _appliedTint = _tint;
            bool clean = !_flashOn && _tint == Color.white;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                if (clean) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(Emission, _flashOn ? FlashColor : Color.black);
                // Meshy-/Toon-Material: Vertexfarbe × Grundfarbe; Tint nur auf Figuren mit weißer Grundfarbe sinnvoll → multiplikativ
                var baseCol = r.sharedMaterial != null && r.sharedMaterial.HasProperty(Base) ? r.sharedMaterial.GetColor(Base) : Color.white;
                _mpb.SetColor(Base, baseCol * _tint);
                r.SetPropertyBlock(_mpb);
            }
        }

        void UpdateDrips(bool on)
        {
            if (!on) { if (_drips != null && _drips.isEmitting) _drips.Stop(); return; }
            if (_drips == null)
            {
                var go = new GameObject("BleedDrips"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 1.2f;
                _drips = go.AddComponent<ParticleSystem>();
                _drips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var m = _drips.main; m.startLifetime = 0.6f; m.startSpeed = 0.2f; m.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f); m.gravityModifier = 1.5f;
                m.startColor = new Color(0.65f, 0.05f, 0.06f); m.maxParticles = 30; m.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = _drips.emission; em.rateOverTime = 6f;
                var sh = _drips.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
                var pm = VfxLibrary.Particle(true); if (pm != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = pm;
            }
            if (!_drips.isEmitting) _drips.Play();
        }
    }
}
