using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G6: Rang-Aura unter Gegnern (Veteran silbern, Champion golden) und Boss-Signatur (Element des Bioms: Bodenring,
    /// aufsteigende Funken, Licht). Liegt unter Telegrafen und Trotzbalken (kein Gefahren-Farbton, geringe Deckung).
    /// </summary>
    public class RankAuraFx : MonoBehaviour
    {
        public enum Rank { Veteran, Champion, Boss }
        public Rank Kind;
        public Color Tint = Color.white;
        ParticleSystem _ring, _motes; Light _light; Combatant _c;

        /// <summary>Rang-Farbe (rein): Veteran silbern, Champion golden, Boss = Elementfarbe.</summary>
        public static Color RankColor(Rank r, Color element) => r == Rank.Veteran ? new Color(0.78f, 0.82f, 0.9f) : r == Rank.Champion ? new Color(1f, 0.8f, 0.3f) : element;

        void Start()
        {
            _c = GetComponent<Combatant>();
            var amb = AmbientFx.Current; var mat = amb != null ? amb.ParticleAdditive : null; if (mat == null) { enabled = false; return; }
            var col = RankColor(Kind, Tint);
            float radius = Kind == Rank.Boss ? 2.6f : Kind == Rank.Champion ? 1.4f : 1f;
            _ring = Make("Rang-Ring", mat, col, radius, true);
            if (Kind != Rank.Veteran) _motes = Make("Rang-Funken", mat, col, radius * 0.7f, false);
            if (Kind == Rank.Boss)
            {
                var go = new GameObject("Boss-Licht"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 2f;
                _light = go.AddComponent<Light>(); _light.type = LightType.Point; _light.color = col; _light.range = 9f; _light.intensity = 1.6f; _light.shadows = LightShadows.None;
            }
        }

        ParticleSystem Make(string name, Material mat, Color col, float radius, bool ring)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * (ring ? 0.08f : 0.5f);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = ring ? 60 : 40; main.startLifetime = ring ? 1.2f : 1.8f; main.startSpeed = 0f;
            main.startSize = ring ? 0.12f : 0.07f; main.startColor = new Color(col.r, col.g, col.b, ring ? 0.55f : 0.8f); main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = (ring ? 40f : 14f) * GraphicsSettingsRuntime.AmbientDensity;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.radiusThickness = ring ? 0f : 1f; sh.rotation = new Vector3(90f, 0, 0);
            if (ring) { sh.arcMode = ParticleSystemShapeMultiModeValue.Loop; sh.arcSpeed = 0.25f; }
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local; v.y = ring ? 0.05f : 0.9f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        void Update()
        {
            if (_c == null || !_c.IsDead) return;
            if (_ring != null) _ring.Stop(); if (_motes != null) _motes.Stop(); if (_light != null) _light.enabled = false;
            enabled = false;
        }
    }
}
