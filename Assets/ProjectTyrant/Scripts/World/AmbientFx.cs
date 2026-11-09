using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M23: Welt-Lebendigkeit je Zone (im Zonen-Root, vom Builder konfiguriert): Wind global für Laub/Gras/Banner,
    /// Partikel-Bett um die Kamera (Pollen + Glühwürmchen, Nebelschwaden, Schneetreiben, Aschewind + Funken, fallende Blätter),
    /// Vogelschwärme (Boids mit Flügelschlag) und Schmetterlinge. Partikelzahl skaliert mit der Effekt-Dichte.
    /// </summary>
    public class AmbientFx : MonoBehaviour
    {
        public WeatherKind Weather;
        public Vector2 WindDir = new Vector2(1f, 0.3f);
        public float WindStrength = 0.25f;
        public bool Leaves, Fireflies, Butterflies;
        public int Birds;
        public Material ParticleAdditive, ParticleAlpha, LeafMat, FlakeMat, BirdMat;
        public Mesh BirdMesh;
        public float BedRadius = 28f;
        readonly List<ParticleSystem> _beds = new List<ParticleSystem>();
        readonly List<Transform> _birds = new List<Transform>(); readonly List<Vector3> _vel = new List<Vector3>();
        Vector3 _flockCenter;
        public static AmbientFx Current { get; private set; }
        public int ParticleCount { get { int n = 0; foreach (var p in _beds) if (p != null) n += p.particleCount; return n; } }

        void OnEnable() { Current = this; VfxPool.AmbientCounter = () => Current != null ? Current.ParticleCount : 0; }
        void OnDisable() { if (Current == this) Current = null; }

        void Start()
        {
            var d = WindDir.normalized;
            Shader.SetGlobalVector("_PT_Wind", new Vector4(d.x, d.y, WindStrength, 0f));
            float dens = VfxPool.DensityFactor;
            switch (Weather)
            {
                case WeatherKind.Pollen: Bed("Pollen", ParticleAdditive, 250, new Color(1f, 0.95f, 0.6f, 0.7f), 0.05f, 6f, -0.02f, 0.4f, 0.5f); break;
                case WeatherKind.MistWisps: Bed("Nebelschwaden", ParticleAlpha, 60, new Color(0.8f, 0.85f, 0.85f, 0.25f), 3.5f, 9f, 0f, 0.15f, 0.2f, 2.5f); break;
                case WeatherKind.Snow: Bed("Schneetreiben", FlakeMat, 900, new Color(1f, 1f, 1f, 0.9f), 0.08f, 7f, 0.25f, 0.4f, 1.6f); break;
                case WeatherKind.AshWind: Bed("Aschewind", ParticleAlpha, 500, new Color(0.35f, 0.32f, 0.3f, 0.6f), 0.07f, 5f, 0.05f, 2.2f, 0.6f); Bed("Funkenflug", ParticleAdditive, 160, new Color(1f, 0.55f, 0.15f, 1f), 0.05f, 3f, -0.3f, 1.2f, 0.4f); break;
            }
            if (Fireflies) Bed("Glühwürmchen", ParticleAdditive, 120, new Color(0.85f, 1f, 0.45f, 1f), 0.07f, 5f, 0f, 0.4f, 0.3f);
            if (Leaves) Bed("Blätter", LeafMat, 90, new Color(0.95f, 0.65f, 0.25f, 1f), 0.12f, 8f, 0.2f, 0.8f, 0.6f);
            if (Butterflies) Bed("Schmetterlinge", LeafMat, 30, new Color(1f, 0.85f, 0.95f, 1f), 0.1f, 6f, 0f, 0.8f, 0.9f);
            foreach (var b in _beds) { var em = b.emission; em.rateOverTimeMultiplier *= dens; b.Play(); }
            for (int i = 0; i < Birds && BirdMesh != null; i++)
            {
                var go = new GameObject("Vogel_" + i); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = BirdMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = BirdMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.localScale = Vector3.one * 0.6f;
                _birds.Add(go.transform); _vel.Add(Random.insideUnitSphere * 4f);
            }
        }

        /// <param name="life">Lebensdauer (s)</param><param name="grav">Schwerkraft</param><param name="wind">Seitenwind-Anteil</param><param name="turb">Turbulenz</param>
        void Bed(string name, Material mat, int max, Color col, float size, float life, float grav, float wind, float turb, float sizeVar = 1f)
        {
            if (mat == null) return;
            var go = new GameObject("Bett_" + name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.startLifetime = life; main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(size, size * sizeVar); main.startColor = col; main.gravityModifier = grav; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = ps.emission; em.rateOverTime = max / life;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(BedRadius * 2f, 10f, BedRadius * 2f); sh.position = Vector3.up * 5f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = WindDir.x * wind; vel.z = WindDir.y * wind; vel.y = 0f;
            var nz = ps.noise; nz.enabled = true; nz.strength = turb; nz.frequency = 0.3f; nz.quality = ParticleSystemNoiseQuality.Low;
            var c = ps.colorOverLifetime; c.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) }); c.color = g;
            if (name == "Schmetterlinge" || name == "Blätter") { var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f); }
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _beds.Add(ps);
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            var c = cam.transform.position; c.y = Mathf.Max(c.y - 4f, 0f);
            foreach (var b in _beds) if (b != null) b.transform.position = new Vector3(c.x, c.y, c.z);
            if (_birds.Count == 0) return;
            // Boids: Zusammenhalt, Abstand, Ausrichtung + Zug zum Kreis über der Kamera
            float dt = Time.deltaTime; _flockCenter = Vector3.Lerp(_flockCenter, cam.transform.position + new Vector3(Mathf.Sin(Time.time * 0.05f) * 40f, 22f, Mathf.Cos(Time.time * 0.05f) * 40f), dt * 0.2f);
            Vector3 avgP = Vector3.zero, avgV = Vector3.zero; for (int i = 0; i < _birds.Count; i++) { avgP += _birds[i].position; avgV += _vel[i]; }
            avgP /= _birds.Count; avgV /= _birds.Count;
            for (int i = 0; i < _birds.Count; i++)
            {
                var p = _birds[i].position; var v = _vel[i];
                v += (avgP - p) * 0.4f * dt + (avgV - v) * 0.6f * dt + (_flockCenter - p) * 0.25f * dt;
                for (int j = 0; j < _birds.Count; j++) if (j != i) { var d = p - _birds[j].position; float m = d.sqrMagnitude; if (m < 4f) v += d.normalized * (2f - Mathf.Sqrt(m)) * 2f * dt; }
                v = Vector3.ClampMagnitude(v, 9f); if (v.magnitude < 4f) v = v.normalized * 4f;
                _vel[i] = v; _birds[i].position = p + v * dt;
                float flap = Mathf.Sin(Time.time * 9f + i) * 0.5f + 0.5f;
                _birds[i].rotation = Quaternion.LookRotation(v) ;
                _birds[i].localScale = new Vector3(0.6f * (0.5f + flap * 0.8f), 0.6f, 0.6f);   // Flügelschlag (Spannweite)
            }
        }
    }
}
