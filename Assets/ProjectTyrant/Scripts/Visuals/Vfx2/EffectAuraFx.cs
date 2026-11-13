using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G5: sichtbare Zustände und Segen am Körper – ein kleines Partikel-Bett je Kämpfer, Farbe und Bewegung nach der
    /// wichtigsten Wirkung (Zustand vor Segen), Dichte nach Stapeln (gedeckelt). Nur in Kameranähe (vom GraphicsDirector vergeben).
    /// </summary>
    public class EffectAuraFx : MonoBehaviour
    {
        public struct Look { public Color A, B; public float Rise, Swirl; public bool Condition; }
        Combatant _c; ParticleSystem _ps; EffectType? _shown; float _rate;
        public EffectType? Shown => _shown;

        /// <summary>Darstellung je Wirkung (rein). Unbekannte Typen: null.</summary>
        public static Look? LookOf(EffectType t)
        {
            switch (t)
            {
                case EffectType.Burning: return new Look { A = new Color(1f, 0.55f, 0.1f), B = new Color(1f, 0.85f, 0.3f), Rise = 1.6f, Swirl = 0.3f, Condition = true };
                case EffectType.Poison: return new Look { A = new Color(0.45f, 0.9f, 0.2f), B = new Color(0.2f, 0.5f, 0.1f), Rise = 0.4f, Swirl = 0.5f, Condition = true };
                case EffectType.Bleeding: return new Look { A = new Color(0.75f, 0.05f, 0.08f), B = new Color(0.45f, 0f, 0.05f), Rise = -1.5f, Swirl = 0.1f, Condition = true };
                case EffectType.Chill: return new Look { A = new Color(0.6f, 0.85f, 1f), B = Color.white, Rise = -0.3f, Swirl = 0.6f, Condition = true };
                case EffectType.Fear: return new Look { A = new Color(0.45f, 0.1f, 0.6f), B = new Color(0.15f, 0.05f, 0.25f), Rise = 0.8f, Swirl = 1.2f, Condition = true };
                case EffectType.Torment: return new Look { A = new Color(0.55f, 0.2f, 0.7f), B = new Color(0.9f, 0.4f, 0.9f), Rise = 0.2f, Swirl = 1.5f, Condition = true };
                case EffectType.Confusion: return new Look { A = new Color(0.9f, 0.3f, 0.9f), B = new Color(0.3f, 0.9f, 0.9f), Rise = 0f, Swirl = 2f, Condition = true };
                case EffectType.Blind: return new Look { A = new Color(0.15f, 0.15f, 0.2f), B = new Color(0.4f, 0.4f, 0.45f), Rise = 0.1f, Swirl = 0.8f, Condition = true };
                case EffectType.Weakness: return new Look { A = new Color(0.6f, 0.65f, 0.3f), B = new Color(0.4f, 0.4f, 0.2f), Rise = -0.5f, Swirl = 0.3f, Condition = true };
                case EffectType.Vulnerability: return new Look { A = new Color(0.9f, 0.5f, 0.6f), B = new Color(0.6f, 0.2f, 0.3f), Rise = 0f, Swirl = 0.4f, Condition = true };
                case EffectType.Immobilize: case EffectType.Cripple: return new Look { A = new Color(0.55f, 0.4f, 0.25f), B = new Color(0.35f, 0.25f, 0.15f), Rise = -0.8f, Swirl = 0.2f, Condition = true };
                case EffectType.Slow: return new Look { A = new Color(0.55f, 0.5f, 0.8f), B = new Color(0.3f, 0.3f, 0.6f), Rise = -0.2f, Swirl = 0.15f, Condition = true };
                case EffectType.Aegis: return new Look { A = new Color(1f, 0.9f, 0.5f), B = new Color(0.9f, 0.95f, 1f), Rise = 0.2f, Swirl = 1f };
                case EffectType.Protection: return new Look { A = new Color(0.5f, 0.7f, 1f), B = new Color(0.8f, 0.9f, 1f), Rise = 0.3f, Swirl = 0.7f };
                case EffectType.Stability: return new Look { A = new Color(0.85f, 0.75f, 0.55f), B = new Color(1f, 1f, 0.85f), Rise = -0.2f, Swirl = 0.3f };
                case EffectType.Might: return new Look { A = new Color(1f, 0.35f, 0.2f), B = new Color(1f, 0.7f, 0.3f), Rise = 0.9f, Swirl = 0.4f };
                case EffectType.Fury: return new Look { A = new Color(1f, 0.25f, 0.15f), B = new Color(1f, 0.5f, 0.2f), Rise = 1.2f, Swirl = 0.8f };
                case EffectType.Quickness: return new Look { A = new Color(1f, 0.85f, 0.3f), B = Color.white, Rise = 0.5f, Swirl = 2f };
                case EffectType.Swiftness: return new Look { A = new Color(0.6f, 1f, 0.7f), B = Color.white, Rise = 0.2f, Swirl = 1.5f };
                case EffectType.Regeneration: return new Look { A = new Color(0.4f, 1f, 0.45f), B = new Color(0.8f, 1f, 0.6f), Rise = 0.8f, Swirl = 0.3f };
                case EffectType.Vigor: return new Look { A = new Color(0.9f, 1f, 0.5f), B = Color.white, Rise = 0.6f, Swirl = 0.5f };
                case EffectType.Resolution: case EffectType.Resistance: return new Look { A = new Color(0.8f, 0.8f, 0.9f), B = new Color(0.6f, 0.6f, 0.7f), Rise = 0.2f, Swirl = 0.5f };
                case EffectType.Alacrity: return new Look { A = new Color(0.8f, 0.5f, 1f), B = Color.white, Rise = 0.4f, Swirl = 1.8f };
                default: return null;
            }
        }

        /// <summary>Wichtigste sichtbare Wirkung (rein): Zustand mit den meisten Stapeln vor Segen.</summary>
        public static EffectType? Dominant(IEnumerable<EffectType> active, System.Func<EffectType, int> stacks)
        {
            EffectType? best = null; int score = -1;
            foreach (var t in active)
            {
                var l = LookOf(t); if (l == null) continue;
                int s = stacks(t) + (l.Value.Condition ? 1000 : 0);
                if (s > score) { score = s; best = t; }
            }
            return best;
        }

        void Awake() { _c = GetComponent<Combatant>(); if (_c != null) _c.Effects.Changed += Refresh; }
        void Start() => Refresh();
        void OnDestroy() { if (_c != null) _c.Effects.Changed -= Refresh; }

        void Refresh()
        {
            if (_c == null) return;
            var dom = Dominant(_c.Effects.ActiveTypes(), _c.Effects.GetStacks);
            if (dom == _shown) { UpdateRate(); return; }
            _shown = dom;
            if (dom == null) { if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); return; }
            Ensure(); var l = LookOf(dom.Value).Value;
            var main = _ps.main; main.startColor = new ParticleSystem.MinMaxGradient(l.A, l.B);
            var v = _ps.velocityOverLifetime; v.y = l.Rise; v.orbitalY = l.Swirl;
            UpdateRate();
            if (!_ps.isPlaying) _ps.Play();
        }

        void UpdateRate()
        {
            if (_ps == null || _shown == null) return;
            int stacks = Mathf.Clamp(_c.Effects.GetStacks(_shown.Value), 1, 10);
            var em = _ps.emission; em.rateOverTime = (6f + stacks * 2.5f) * GraphicsSettingsRuntime.AmbientDensity;
        }

        void Ensure()
        {
            if (_ps != null) return;
            var amb = AmbientFx.Current; var mat = amb != null ? amb.ParticleAdditive : null; if (mat == null) return;
            var go = new GameObject("Wirkungs-Aura"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 1.1f;
            _ps = go.AddComponent<ParticleSystem>(); _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 48; main.startLifetime = 1.1f; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = _ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.45f; sh.scale = new Vector3(1f, 2.2f, 1f);
            var vel = _ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            var col = _ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) }); col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
