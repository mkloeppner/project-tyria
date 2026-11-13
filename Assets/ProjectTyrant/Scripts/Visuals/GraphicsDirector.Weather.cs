using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G4: Wetter-Partikel (Regen, Schnee, Sand, Asche, Jaderegen, Niesel) als Kamera-Bett mit 30-s-Überblendung,
    /// Blitze bei Gewitter (lichtempfindlich gedeckelt), Umgebungs-Volumen aus dem Zonenprofil (≥ 4 je Zone, nur in Kameranähe),
    /// Dämpfung im 15-m-Kampfradius, Deckel 30 % in Sprungrätseln und bei Champions, Leistungs-Regler. Rein visuell.
    /// </summary>
    public partial class GraphicsDirector
    {
        public readonly PerformanceGovernor Governor = new PerformanceGovernor();
        readonly Dictionary<WeatherState, ParticleSystem> _weatherBeds = new Dictionary<WeatherState, ParticleSystem>();
        readonly List<(ZoneArtProfile.AmbientVolume vol, ParticleSystem ps, float baseRate)> _volumes = new List<(ZoneArtProfile.AmbientVolume, ParticleSystem, float)>();
        float _nextBolt = 12f, _bolt, _fpsAvg = 60f, _auraScan;
        public bool ArenaCap { get; private set; }
        public float LastFps => _fpsAvg;
        public int WeatherParticles { get { int n = 0; foreach (var p in _weatherBeds.Values) if (p != null) n += p.particleCount; foreach (var v in _volumes) if (v.ps != null) n += v.ps.particleCount; return n; } }

        public const float CombatDampRadius = 15f, CombatDamp = 0.4f, VolumeActivateDistance = 110f;

        /// <summary>Dämpfung eines Umgebungs-Effekts (rein): im Kampf innerhalb von 15 m um den Spieler auf 40 %.</summary>
        public static float CombatDampFactor(float distanceToPlayer, bool inCombat) => inCombat && distanceToPlayer < CombatDampRadius ? CombatDamp : 1f;

        void WeatherZoneChanged()
        {
            foreach (var p in _weatherBeds.Values) if (p != null) Destroy(p.gameObject);
            _weatherBeds.Clear();
            foreach (var v in _volumes) if (v.ps != null) Destroy(v.ps.gameObject);
            _volumes.Clear();
            if (Profile == null) return;
            foreach (var av in Profile.AmbientVolumes) { var ps = AmbientEmitter(av.Kind, av.Radius, out float rate); if (ps != null) _volumes.Add((av, ps, rate)); }
        }

        void WeatherTick(float dt)
        {
            float fps = dt > 0f ? 1f / dt : 60f;
            _fpsAvg = Mathf.Lerp(_fpsAvg, fps, 1f - Mathf.Exp(-dt * 2f));
            if (GraphicsSettingsRuntime.AutoGovernor) Governor.Tick(_fpsAvg, dt);
            var gm = GameManager.Instance; var player = gm != null ? gm.Player : null;
            var cam = Camera.main;
            ArenaCap = Landmarks.ClimbArea.ActiveTracking() != null || ChampionNearby(player, 40f);
            float density = GraphicsSettingsRuntime.WeatherDensity * Governor.AmbientFactor;
            bool weatherOn = GraphicsSettingsRuntime.Feature("Wetter");

            // Wetter-Betten mit Überblendung Vorher → Jetzt
            if (Weather != null)
            {
                float b = Mathf.SmoothStep(0f, 1f, Weather.Blend);
                foreach (WeatherState s in System.Enum.GetValues(typeof(WeatherState)))
                {
                    float w = (Weather.Current == s ? b : 0f) + (Weather.Previous == s && Weather.Previous != Weather.Current ? 1f - b : 0f);
                    var look = WeatherModel.LookOf(s);
                    float share = WeatherModel.ParticleShare(look, ArenaCap) * w * density * (weatherOn ? 1f : 0f);
                    if (share <= 0.001f && !_weatherBeds.ContainsKey(s)) continue;
                    var bed = WeatherBed(s); if (bed == null) continue;
                    var em = bed.emission; em.rateOverTimeMultiplier = BaseRate(s) * share;
                    if (cam != null) bed.transform.position = cam.transform.position + Vector3.up * 6f + cam.transform.forward * 6f;
                    if (share > 0.001f && !bed.isPlaying) bed.Play(); else if (share <= 0.001f && bed.isPlaying && bed.particleCount == 0) bed.Stop();
                }
                // Blitze
                float storm = (Weather.Current == WeatherState.Gewitter ? b : 0f) + (Weather.Previous == WeatherState.Gewitter && Weather.Current != WeatherState.Gewitter ? 1f - b : 0f);
                _bolt = Mathf.MoveTowards(_bolt, 0f, dt * 4f);
                if (storm > 0.5f && weatherOn && (_nextBolt -= dt) <= 0f) { _bolt = Photosensitive.ClampFlash(0.9f); _nextBolt = Random.Range(6f, 18f); }
                SceneLighting.Lightning = _bolt;
            }

            // Wirkungs-Auren (G5): Kämpfer in Kameranähe bekommen ihre Aura-Komponente
            if ((_auraScan -= dt) <= 0f && cam != null)
            {
                _auraScan = 0.5f; int n = 0;
                foreach (var c in Combatant.All)
                {
                    if (c == null || n >= 32) continue;
                    if ((c.transform.position - cam.transform.position).sqrMagnitude > 45f * 45f) continue;
                    if (c.GetComponent<EffectAuraFx>() == null) c.gameObject.AddComponent<EffectAuraFx>();
                    n++;
                }
            }

            // Umgebungs-Volumen: nur in Kameranähe, nachts Glühwürmchen, Kampf-Dämpfung
            bool ambientOn = GraphicsSettingsRuntime.Feature("Umgebung");
            bool inCombat = player != null && player.TryGetComponent(out OutOfCombatRegen regen) && regen.InCombat;
            float ambDensity = GraphicsSettingsRuntime.AmbientDensity * Governor.AmbientFactor * (ArenaCap ? WeatherModel.ArenaParticleCap : 1f);
            float night = Shader.GetGlobalVector("_PT_Night").y;
            foreach (var v in _volumes)
            {
                if (v.ps == null) continue;
                float dCam = cam != null ? Vector3.Distance(cam.transform.position, v.vol.Center) : 0f;
                bool near = ambientOn && dCam < VolumeActivateDistance + v.vol.Radius;
                float dPlayer = player != null ? Vector3.Distance(player.transform.position, v.vol.Center) - v.vol.Radius : 999f;
                float k = near ? ambDensity * CombatDampFactor(Mathf.Max(0f, dPlayer), inCombat) * KindTimeFactor(v.vol.Kind, night) : 0f;
                var em = v.ps.emission; em.rateOverTimeMultiplier = v.baseRate * k;
                if (k > 0.001f && !v.ps.isPlaying) { v.ps.transform.position = v.vol.Center; v.ps.Play(); }
                else if (k <= 0.001f && v.ps.isPlaying) v.ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>Glühwürmchen nur nachts, Pollen/Schmetterlinge nur tagsüber (rein).</summary>
        public static float KindTimeFactor(AmbientFxKind k, float night)
        {
            switch (k)
            {
                case AmbientFxKind.Gluehwuermchen: return Mathf.Clamp01(night * 1.5f - 0.2f);
                case AmbientFxKind.Pollen: case AmbientFxKind.Schmetterlinge: case AmbientFxKind.Voegel: return Mathf.Clamp01(1.2f - night * 1.5f);
                default: return 1f;
            }
        }

        static bool ChampionNearby(Combatant player, float radius)
        {
            if (player == null) return false;
            var p = player.transform.position; float r2 = radius * radius;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Team != Team.Enemy) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b != null && b.Archetype != null && b.Archetype.Tier == EnemyTier.Champion && (c.transform.position - p).sqrMagnitude < r2) return true;
            }
            return false;
        }

        // ---------------- Partikel-Vorlagen ----------------
        static float BaseRate(WeatherState s)
        {
            switch (s)
            {
                case WeatherState.Regen: case WeatherState.Gewitter: return 2200f;
                case WeatherState.Jaderegen: return 900f;
                case WeatherState.Schnee: return 500f;
                case WeatherState.Schneesturm: return 1200f;
                case WeatherState.Sandsturm: return 900f;
                case WeatherState.Aschefall: return 350f;
                case WeatherState.Nieselnebel: return 40f;
                default: return 0f;
            }
        }

        ParticleSystem WeatherBed(WeatherState s)
        {
            if (_weatherBeds.TryGetValue(s, out var ps)) return ps;
            if (BaseRate(s) <= 0f) return null;
            var amb = AmbientFx.Current;
            Material add = amb != null ? amb.ParticleAdditive : null, alpha = amb != null ? amb.ParticleAlpha : null, flake = amb != null ? amb.FlakeMat : null;
            switch (s)
            {
                case WeatherState.Regen: case WeatherState.Gewitter: ps = Bed("Wetter_Regen", alpha ?? add, 4000, new Color(0.75f, 0.82f, 0.95f, 0.45f), 0.04f, 1.1f, new Vector3(1.5f, -16f, 0.6f), true, 30f); break;
                case WeatherState.Jaderegen: ps = Bed("Wetter_Jaderegen", add ?? alpha, 1600, new Color(0.45f, 1f, 0.7f, 0.55f), 0.045f, 1.4f, new Vector3(0.8f, -12f, 0.4f), true, 30f); break;
                case WeatherState.Schnee: ps = Bed("Wetter_Schnee", flake ?? alpha, 1600, new Color(1f, 1f, 1f, 0.9f), 0.09f, 6f, new Vector3(0.6f, -1.6f, 0.3f), false, 30f); break;
                case WeatherState.Schneesturm: ps = Bed("Wetter_Schneesturm", flake ?? alpha, 3000, new Color(1f, 1f, 1f, 0.85f), 0.08f, 2.5f, new Vector3(9f, -3f, 3f), true, 26f); break;
                case WeatherState.Sandsturm: ps = Bed("Wetter_Sandsturm", alpha, 2400, new Color(0.9f, 0.72f, 0.45f, 0.5f), 0.12f, 2.2f, new Vector3(14f, -0.5f, 5f), true, 26f); break;
                case WeatherState.Aschefall: ps = Bed("Wetter_Asche", alpha, 1400, new Color(0.35f, 0.33f, 0.32f, 0.7f), 0.07f, 5f, new Vector3(0.8f, -0.8f, 0.4f), false, 30f); break;
                case WeatherState.Nieselnebel: ps = Bed("Wetter_Niesel", alpha, 120, new Color(0.85f, 0.88f, 0.9f, 0.18f), 4f, 8f, new Vector3(0.5f, 0f, 0.2f), false, 30f); break;
            }
            _weatherBeds[s] = ps;
            return ps;
        }

        ParticleSystem Bed(string name, Material mat, int max, Color col, float size, float life, Vector3 vel, bool stretch, float radius)
        {
            if (mat == null) return null;
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.startLifetime = life; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f); main.startColor = col; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(radius * 2f, 2f, radius * 2f);
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = vel.x; v.y = vel.y; v.z = vel.z;
            var nz = ps.noise; nz.enabled = true; nz.strength = stretch ? 0.3f : 0.8f; nz.frequency = 0.4f; nz.quality = ParticleSystemNoiseQuality.Low;
            var col2 = ps.collision; col2.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.maxParticleSize = size > 1f ? 0.5f : 0.03f;   // keine bildfüllenden Partikel direkt vor der Kamera
            if (stretch) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.035f; r.lengthScale = 1f; }
            return ps;
        }

        ParticleSystem AmbientEmitter(AmbientFxKind k, float radius, out float rate)
        {
            rate = 0f;
            var amb = AmbientFx.Current;
            Material add = amb != null ? amb.ParticleAdditive : null, alpha = amb != null ? amb.ParticleAlpha : null, leaf = amb != null ? amb.LeafMat : null, flake = amb != null ? amb.FlakeMat : null;
            ParticleSystem ps = null;
            switch (k)
            {
                case AmbientFxKind.Gluehwuermchen: ps = Bed("Umgebung_" + k, add, 120, new Color(0.85f, 1f, 0.45f, 1f), 0.08f, 5f, Vector3.up * 0.1f, false, radius); rate = 24f; break;
                case AmbientFxKind.Pollen: ps = Bed("Umgebung_" + k, add, 200, new Color(1f, 0.95f, 0.6f, 0.7f), 0.05f, 7f, new Vector3(0.3f, 0.05f, 0.1f), false, radius); rate = 30f; break;
                case AmbientFxKind.Schmetterlinge: ps = Bed("Umgebung_" + k, leaf, 24, new Color(1f, 0.85f, 0.95f, 1f), 0.12f, 7f, Vector3.zero, false, radius); rate = 4f; break;
                case AmbientFxKind.Blaetter: ps = Bed("Umgebung_" + k, leaf, 80, new Color(0.95f, 0.65f, 0.25f, 1f), 0.14f, 7f, new Vector3(0.5f, -0.6f, 0.2f), false, radius); rate = 10f; break;
                case AmbientFxKind.Sporen: ps = Bed("Umgebung_" + k, add, 160, new Color(0.75f, 1f, 0.6f, 0.6f), 0.06f, 8f, Vector3.up * 0.25f, false, radius); rate = 20f; break;
                case AmbientFxKind.Schneeflocken: ps = Bed("Umgebung_" + k, flake ?? alpha, 240, Color.white, 0.07f, 7f, new Vector3(0.3f, -0.9f, 0.1f), false, radius); rate = 35f; break;
                case AmbientFxKind.Glut: ps = Bed("Umgebung_" + k, add, 160, new Color(1f, 0.55f, 0.15f, 1f), 0.05f, 3f, Vector3.up * 1.2f, false, radius); rate = 45f; break;
                case AmbientFxKind.Asche: ps = Bed("Umgebung_" + k, alpha, 180, new Color(0.35f, 0.32f, 0.3f, 0.6f), 0.06f, 6f, new Vector3(0.6f, -0.4f, 0.2f), false, radius); rate = 30f; break;
                case AmbientFxKind.Sand: ps = Bed("Umgebung_" + k, alpha, 200, new Color(0.9f, 0.75f, 0.5f, 0.4f), 0.07f, 3f, new Vector3(4f, 0f, 1.5f), true, radius); rate = 60f; break;
                case AmbientFxKind.Jadestaub: ps = Bed("Umgebung_" + k, add, 160, new Color(0.4f, 1f, 0.7f, 0.8f), 0.05f, 6f, Vector3.up * 0.3f, false, radius); rate = 26f; break;
                case AmbientFxKind.Lichtstaub: ps = Bed("Umgebung_" + k, add, 140, new Color(1f, 0.95f, 0.85f, 0.5f), 0.04f, 8f, Vector3.up * 0.05f, false, radius); rate = 18f; break;
                case AmbientFxKind.Rauch: ps = Bed("Umgebung_" + k, alpha, 40, new Color(0.3f, 0.28f, 0.3f, 0.3f), 2.2f, 9f, Vector3.up * 0.8f, false, radius * 0.5f); rate = 4f; break;
                case AmbientFxKind.Tropfen: ps = Bed("Umgebung_" + k, alpha ?? add, 120, new Color(0.7f, 0.85f, 1f, 0.6f), 0.03f, 1.2f, Vector3.down * 6f, true, radius); rate = 40f; break;
                case AmbientFxKind.Kristallpuls: ps = Bed("Umgebung_" + k, add, 100, new Color(0.5f, 0.9f, 1f, 0.9f), 0.1f, 4f, Vector3.up * 0.15f, false, radius); rate = 18f; break;
                case AmbientFxKind.Windstreifen: ps = Bed("Umgebung_" + k, add ?? alpha, 30, new Color(1f, 1f, 1f, 0.25f), 0.05f, 1.5f, new Vector3(9f, 0f, 3f), true, radius); rate = 8f; break;
                case AmbientFxKind.Voegel: ps = Bed("Umgebung_" + k, leaf, 12, new Color(0.2f, 0.2f, 0.25f, 1f), 0.25f, 10f, new Vector3(3f, 0.3f, 1f), false, radius); rate = 1.2f; break;
            }
            if (ps != null) { var sh = ps.shape; sh.scale = new Vector3(radius * 2f, Mathf.Min(12f, radius * 0.5f), radius * 2f); }
            return ps;
        }
    }
}
