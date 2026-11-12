using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M30: Sandsturm im Dünenmeer nach <see cref="SandstormModel"/> (Weltuhr): Nebel dichter, Spieler langsamer (außer in Späher-Zelten), Vorwarnung und Ende als Meldung.</summary>
    public class SandstormDirector : MonoBehaviour
    {
        public static SandstormDirector Current { get; private set; }
        float _baseFog; Color _baseFogColor; SandstormModel.Phase _last = SandstormModel.Phase.Ruhig;
        public float Intensity { get; private set; }
        public bool PlayerSheltered { get; private set; }

        void OnEnable() { Current = this; _baseFog = RenderSettings.fogDensity; _baseFogColor = RenderSettings.fogColor; }
        void OnDisable() { Visuals.SceneLighting.ExternalFogScale = 1f; Visuals.SceneLighting.ExternalFogTint = Color.clear; if (Current == this) Current = null; RenderSettings.fogDensity = _baseFog; RenderSettings.fogColor = _baseFogColor; }

        void Update()
        {
            float t = WorldClock.Seconds;
            var phase = SandstormModel.At(t);
            if (phase != _last)
            {
                if (phase == SandstormModel.Phase.Warnung) GameEventBus.Notify("Ein Sandsturm zieht auf – Späher-Zelte bieten Schutz!", new Color(1f, 0.85f, 0.5f));
                else if (phase == SandstormModel.Phase.Sturm) GameEventBus.RaiseBanner("Sandsturm", "Sicht und Tempo sinken", new Color(1f, 0.8f, 0.45f));
                else if (_last == SandstormModel.Phase.Sturm) GameEventBus.Notify("Der Sandsturm legt sich.", new Color(1f, 0.9f, 0.7f));
                _last = phase;
            }
            Intensity = SandstormModel.Intensity(t);
            RenderSettings.fogDensity = _baseFog * SandstormModel.FogFactor(Intensity);
            RenderSettings.fogColor = Color.Lerp(_baseFogColor, new Color(0.85f, 0.68f, 0.42f), Intensity * 0.8f);
            Visuals.SceneLighting.ExternalFogScale = SandstormModel.FogFactor(Intensity); Visuals.SceneLighting.ExternalFogTint = new Color(0.85f, 0.68f, 0.42f, Intensity * 0.8f);   // Grafik-Lauf G2
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            if (p == null) return;
            PlayerSheltered = SandShelter.Covers(p.transform.position);
            if (Intensity > 0.3f && !PlayerSheltered && !p.Effects.Has(EffectType.Slow)) p.Effects.Apply(new EffectApplication(EffectType.Cripple, 1, 1.2f), null);
        }
    }
}
