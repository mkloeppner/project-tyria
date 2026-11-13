using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G2: Licht, Himmel, Nebel und Vollbild-Passes je Bild aus Profil, Tageszeit und Wetter.</summary>
    public partial class GraphicsDirector
    {
        float _cubemapHour = -99f, _hitFlash, _waterLevel = -100f;
        bool _subscribed;

        partial void ZoneChanged()
        {
            if (!_subscribed) { GameEventBus.DamageDealt += OnDamage; _subscribed = true; }
            var z = GameManager.Instance != null ? GameManager.Instance.CurrentZone : null;
            _waterLevel = z != null && z.Biome != null ? z.Biome.WaterLevel : -100f;
            WeatherZoneChanged();
            Tick();
            SceneLighting.RefreshCubemap(); _cubemapHour = Time.EffectiveHour;
        }

        void OnDestroy()
        {
            if (_subscribed) GameEventBus.DamageDealt -= OnDamage;
            if (_sigSubscribed) { DefianceBar.Broken -= OnBroken; GameEventBus.EntityKilled -= OnKilled; MountController.MountChanged -= OnMount; }
        }

        void OnDamage(DamageInfo d)
        {
            var gm = GameManager.Instance;
            if (gm == null || d.Target != gm.Player || d.Amount <= 0f || d.Evaded || d.Missed) return;
            float share = gm.Player.MaxHealth > 0f ? d.Amount / gm.Player.MaxHealth : 0f;
            _hitFlash = Mathf.Max(_hitFlash, Photosensitive.ClampFlash(Mathf.Clamp(share * 2.5f, 0.08f, 0.35f)));
        }

        static readonly int Underwater = Shader.PropertyToID("_PT_Underwater"), Damage = Shader.PropertyToID("_PT_Damage"), DamageFlash = Shader.PropertyToID("_PT_DamageFlash");

        partial void Tick()
        {
            if (Profile == null) return;
            WeatherTick(UnityEngine.Time.unscaledDeltaTime);
            SignaturesTick(UnityEngine.Time.deltaTime);
            var look = Weather != null ? Weather.Look : WeatherModel.LookOf(WeatherState.Klar);
            SceneLighting.Apply(Profile, Time.EffectiveHour, look, GraphicsLibrary.Current);
            var cam = Camera.main;
            SceneLighting.Shockwaves.Update(cam);
            if (SceneLighting.Shockwaves.Count > 0 && GraphicsSettingsRuntime.Feature("Verzerrung")) RenderState.DistortionOn = true;

            bool under = cam != null && GraphicsSettingsRuntime.Feature("Unterwasser") && cam.transform.position.y < _waterLevel - 0.15f;
            RenderState.UnderwaterOn = under;
            Shader.SetGlobalVector(Underwater, new Vector4(under ? 1f : 0f, 0.06f, 1f, 1f));

            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            float low = p != null && !p.IsDead ? Mathf.Clamp01((0.35f - p.HealthFraction) / 0.35f) : 0f;
            _hitFlash = Mathf.MoveTowards(_hitFlash, 0f, UnityEngine.Time.unscaledDeltaTime * 1.6f);
            bool dmg = GraphicsSettingsRuntime.Feature("Schaden") && (low > 0.01f || _hitFlash > 0.01f);
            RenderState.DamageOn = dmg;
            Shader.SetGlobalVector(Damage, new Vector4(0.55f, 0.04f, 0.05f, low * 0.55f));
            Shader.SetGlobalFloat(DamageFlash, _hitFlash);

            float h = Time.EffectiveHour;
            if (Mathf.Abs(Mathf.DeltaAngle(h * 15f, _cubemapHour * 15f)) >= 30f) { SceneLighting.RefreshCubemap(); _cubemapHour = h; }   // alle 2 Spielstunden
        }
    }
}
