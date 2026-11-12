using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6 / M32: Dunkelheit der Kristalltiefe nach <see cref="DarknessModel"/>: Sichtweite (Nebel) folgt dem Licht – Laternen-Bündel oder Kristalle;
    /// im Dunkeln Schwäche. Ein Licht am Spieler zeigt den Laternenradius.
    /// </summary>
    public class DarknessDirector : MonoBehaviour
    {
        float _baseFog, _tick; Light _lantern;
        public bool PlayerInLight { get; private set; }
        public float Vision { get; private set; }

        void OnEnable() { _baseFog = RenderSettings.fogDensity; }
        void OnDisable() { Visuals.SceneLighting.ExternalFogScale = 1f; RenderSettings.fogDensity = _baseFog; if (_lantern != null) Destroy(_lantern.gameObject); }

        void Update()
        {
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null; if (p == null) return;
            bool lantern = gm.Story != null && gm.Story.Bundle.Current == "laterne";
            float crystal = CrystalLight.Nearest(p.transform.position);
            PlayerInLight = DarknessModel.InLight(lantern, crystal);
            Vision = Mathf.Lerp(Vision <= 0f ? DarknessModel.BaseVision : Vision, DarknessModel.Vision(lantern, crystal), 1f - Mathf.Exp(-2f * Time.deltaTime));
            RenderSettings.fogDensity = Mathf.Max(_baseFog * 0.5f, DarknessModel.FogDensity(Vision) * 0.35f);
            Visuals.SceneLighting.ExternalFogScale = _baseFog > 1e-5f ? RenderSettings.fogDensity / _baseFog : 1f;   // Grafik-Lauf G2: Nebel-Pass folgt der Sichtweite
            if (_lantern == null)
            {
                var g = new GameObject("PT_PlayerGlow"); _lantern = g.AddComponent<Light>(); _lantern.type = LightType.Point; _lantern.shadows = LightShadows.None; _lantern.color = new Color(1f, 0.85f, 0.6f);
            }
            _lantern.transform.position = p.transform.position + Vector3.up * 2.2f;
            _lantern.range = lantern ? DarknessModel.LanternRadius : 5f; _lantern.intensity = lantern ? 2.6f : 0.8f;
            if ((_tick -= Time.deltaTime) > 0f) return; _tick = 2f;
            if (!PlayerInLight && !p.IsDead) p.Effects.Apply(new EffectApplication(EffectType.Weakness, 1, 2.5f), null);
        }
    }
}
