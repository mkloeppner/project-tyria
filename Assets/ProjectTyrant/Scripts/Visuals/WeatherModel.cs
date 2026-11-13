using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Werte eines Wetterzustands (rein visuell/akustisch).</summary>
    public struct WeatherLook
    {
        public float LightScale, FogScale, CloudCover, Particles, Wetness, Snow, Dust, Wind;
        public static WeatherLook Lerp(WeatherLook a, WeatherLook b, float t) => new WeatherLook
        {
            LightScale = Mathf.Lerp(a.LightScale, b.LightScale, t), FogScale = Mathf.Lerp(a.FogScale, b.FogScale, t), CloudCover = Mathf.Lerp(a.CloudCover, b.CloudCover, t),
            Particles = Mathf.Lerp(a.Particles, b.Particles, t), Wetness = Mathf.Lerp(a.Wetness, b.Wetness, t), Snow = Mathf.Lerp(a.Snow, b.Snow, t),
            Dust = Mathf.Lerp(a.Dust, b.Dust, t), Wind = Mathf.Lerp(a.Wind, b.Wind, t)
        };
    }

    /// <summary>
    /// Grafik-Lauf G4: Wetter-Zustandsautomat (rein). Zustände je Zone aus dem Kunstprofil, Wechsel alle 6–14 min (deterministisch aus Seed),
    /// 30 s Überblendung, Sperre (Admin), Deckel 30 % Partikel in Weltboss-Arenen und Sprungrätseln. Keine Gameplay-Wirkung.
    /// </summary>
    public class WeatherModel
    {
        public const float MinInterval = 360f, MaxInterval = 840f, BlendSeconds = 30f, ArenaParticleCap = 0.3f;
        readonly List<WeatherState> _states;
        readonly System.Random _rng;
        public WeatherState Current { get; private set; }
        public WeatherState Previous { get; private set; }
        public float Blend { get; private set; } = 1f;
        public float NextChangeIn { get; private set; }
        public bool Locked;

        public WeatherModel(IList<WeatherState> states, int seed)
        {
            _states = new List<WeatherState>(states != null && states.Count > 0 ? states : new[] { WeatherState.Klar });
            _rng = new System.Random(seed);
            Current = Previous = _states[0];
            NextChangeIn = NextInterval();
        }

        public IReadOnlyList<WeatherState> States => _states;
        float NextInterval() => MinInterval + (float)_rng.NextDouble() * (MaxInterval - MinInterval);

        /// <summary>Rückgabe true = gerade ein neuer Zustand gewählt.</summary>
        public bool Tick(float dt)
        {
            if (Blend < 1f) Blend = Mathf.Min(1f, Blend + dt / BlendSeconds);
            if (Locked || _states.Count < 2) return false;
            NextChangeIn -= dt;
            if (NextChangeIn > 0f) return false;
            var next = Current; int guard = 0;
            while (next == Current && guard++ < 16) next = _states[_rng.Next(_states.Count)];
            SetState(next, false);
            return true;
        }

        public void SetState(WeatherState s, bool instant)
        {
            Previous = instant ? s : Current; Current = s; Blend = instant ? 1f : 0f; NextChangeIn = NextInterval();
        }

        public WeatherLook Look => WeatherLook.Lerp(LookOf(Previous), LookOf(Current), Mathf.SmoothStep(0f, 1f, Blend));

        /// <summary>Partikel-Anteil mit Arena-Deckel.</summary>
        public static float ParticleShare(WeatherLook l, bool arena) => arena ? Mathf.Min(l.Particles, ArenaParticleCap) : l.Particles;

        public static WeatherLook LookOf(WeatherState s)
        {
            switch (s)
            {
                case WeatherState.Bewoelkt: return new WeatherLook { LightScale = 0.75f, FogScale = 1.2f, CloudCover = 0.8f, Wind = 0.6f };
                case WeatherState.Regen: return new WeatherLook { LightScale = 0.6f, FogScale = 1.6f, CloudCover = 0.95f, Particles = 1f, Wetness = 1f, Wind = 0.8f };
                case WeatherState.Gewitter: return new WeatherLook { LightScale = 0.45f, FogScale = 1.8f, CloudCover = 1f, Particles = 1f, Wetness = 1f, Wind = 1f };
                case WeatherState.Nieselnebel: return new WeatherLook { LightScale = 0.7f, FogScale = 2.4f, CloudCover = 0.85f, Particles = 0.35f, Wetness = 0.5f, Wind = 0.3f };
                case WeatherState.Schnee: return new WeatherLook { LightScale = 0.8f, FogScale = 1.5f, CloudCover = 0.85f, Particles = 0.8f, Snow = 1f, Wind = 0.4f };
                case WeatherState.Schneesturm: return new WeatherLook { LightScale = 0.55f, FogScale = 2.6f, CloudCover = 1f, Particles = 1f, Snow = 1f, Wind = 1f };
                case WeatherState.Sandsturm: return new WeatherLook { LightScale = 0.7f, FogScale = 2.6f, CloudCover = 0.5f, Particles = 1f, Dust = 1f, Wind = 1f };
                case WeatherState.Aschefall: return new WeatherLook { LightScale = 0.65f, FogScale = 1.7f, CloudCover = 0.9f, Particles = 0.8f, Dust = 0.6f, Wind = 0.5f };
                case WeatherState.Jaderegen: return new WeatherLook { LightScale = 0.75f, FogScale = 1.4f, CloudCover = 0.85f, Particles = 0.9f, Wetness = 1f, Wind = 0.5f };
                default: return new WeatherLook { LightScale = 1f, FogScale = 1f, CloudCover = 0.35f, Wind = 0.4f };
            }
        }
    }
}
