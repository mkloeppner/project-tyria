using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Ein Tageszeit-Schlüsselbild (Faktoren auf das Zonenprofil).</summary>
    public struct DayKey
    {
        public float Hour;
        public Color LightTint;
        public float LightIntensity, AmbientScale, FogScale, Exposure, SkyBrightness, StarAlpha;
        public Color SkyTint;
        public static DayKey Lerp(DayKey a, DayKey b, float t) => new DayKey
        {
            Hour = Mathf.Lerp(a.Hour, b.Hour, t), LightTint = Color.Lerp(a.LightTint, b.LightTint, t), LightIntensity = Mathf.Lerp(a.LightIntensity, b.LightIntensity, t),
            AmbientScale = Mathf.Lerp(a.AmbientScale, b.AmbientScale, t), FogScale = Mathf.Lerp(a.FogScale, b.FogScale, t), Exposure = Mathf.Lerp(a.Exposure, b.Exposure, t),
            SkyBrightness = Mathf.Lerp(a.SkyBrightness, b.SkyBrightness, t), StarAlpha = Mathf.Lerp(a.StarAlpha, b.StarAlpha, t), SkyTint = Color.Lerp(a.SkyTint, b.SkyTint, t)
        };
    }

    /// <summary>
    /// Grafik-Lauf G1/G2: Tageszeit (rein). Ein Zyklus = 48 Echtminuten (Einstellung), Sonnenbahn mit Azimut je Zone, Schlüsselbilder Nacht (0),
    /// Morgen (6), Mittag (12), Abend (18) mit stetiger Interpolation; Mond als schwaches Hauptlicht nachts; Erzwingen (Cutscenes) und Freigeben.
    /// Rein visuell – kein Gameplay liest die Tageszeit.
    /// </summary>
    public class TimeOfDayModel
    {
        public const float DefaultCycleMinutes = 48f;
        public float CycleMinutes = DefaultCycleMinutes;
        public float Hour { get; private set; } = 10f;
        public bool Paused;
        public float Speed = 1f;
        float? _forced;
        public bool Forced => _forced.HasValue;
        public float EffectiveHour => _forced ?? Hour;

        public static readonly DayKey[] Keys =
        {
            new DayKey { Hour = 0f,  LightTint = new Color(0.55f, 0.65f, 1f),  LightIntensity = 0.28f, AmbientScale = 0.55f, FogScale = 1.25f, Exposure = 0.35f, SkyBrightness = 0.18f, StarAlpha = 1f,   SkyTint = new Color(0.25f, 0.32f, 0.6f) },
            new DayKey { Hour = 6f,  LightTint = new Color(1f, 0.72f, 0.5f),   LightIntensity = 0.75f, AmbientScale = 0.8f,  FogScale = 1.4f,  Exposure = 0.15f, SkyBrightness = 0.75f, StarAlpha = 0.15f, SkyTint = new Color(1f, 0.78f, 0.7f) },
            new DayKey { Hour = 12f, LightTint = Color.white,                    LightIntensity = 1f,    AmbientScale = 1f,    FogScale = 1f,    Exposure = 0f,    SkyBrightness = 1f,    StarAlpha = 0f,    SkyTint = Color.white },
            new DayKey { Hour = 18f, LightTint = new Color(1f, 0.62f, 0.38f),  LightIntensity = 0.7f,  AmbientScale = 0.78f, FogScale = 1.2f,  Exposure = 0.12f, SkyBrightness = 0.7f,  StarAlpha = 0.2f,  SkyTint = new Color(1f, 0.62f, 0.5f) },
            new DayKey { Hour = 24f, LightTint = new Color(0.55f, 0.65f, 1f),  LightIntensity = 0.28f, AmbientScale = 0.55f, FogScale = 1.25f, Exposure = 0.35f, SkyBrightness = 0.18f, StarAlpha = 1f,   SkyTint = new Color(0.25f, 0.32f, 0.6f) },
        };

        public void Set(float hour) => Hour = Mathf.Repeat(hour, 24f);
        public void Force(float hour) => _forced = Mathf.Repeat(hour, 24f);
        public void Release() => _forced = null;

        /// <summary>Fortschritt um dt Echtsekunden (24 Spielstunden je Zyklus).</summary>
        public void Tick(float dt)
        {
            if (Paused || CycleMinutes <= 0f) return;
            Hour = Mathf.Repeat(Hour + dt * Speed * 24f / (CycleMinutes * 60f), 24f);
        }

        /// <summary>Stetige Interpolation (Smoothstep) zwischen den Schlüsselbildern.</summary>
        public static DayKey Sample(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            for (int i = 0; i < Keys.Length - 1; i++)
                if (hour >= Keys[i].Hour && hour <= Keys[i + 1].Hour)
                {
                    float t = Mathf.InverseLerp(Keys[i].Hour, Keys[i + 1].Hour, hour);
                    return DayKey.Lerp(Keys[i], Keys[i + 1], Mathf.SmoothStep(0f, 1f, t));
                }
            return Keys[0];
        }

        /// <summary>Sonnenhöhe in Grad (−90…90): Aufgang 6 Uhr, Zenit 12 Uhr auf <paramref name="noonElevation"/>.</summary>
        public static float SunElevation(float hour, float noonElevation)
        {
            float a = (Mathf.Repeat(hour, 24f) - 6f) / 12f * Mathf.PI;   // 0 bei 6 Uhr, π bei 18 Uhr
            return Mathf.Sin(a) * noonElevation;
        }

        /// <summary>Sonnenrichtung (zur Sonne zeigend) aus Uhrzeit, Zonen-Azimut und Mittagshöhe.</summary>
        public static Vector3 SunDirection(float hour, float azimuth, float noonElevation)
        {
            float elev = SunElevation(hour, noonElevation) * Mathf.Deg2Rad;
            float az = (azimuth + (Mathf.Repeat(hour, 24f) - 12f) * 15f) * Mathf.Deg2Rad;   // 15°/h Wanderung um den Zonen-Azimut
            return new Vector3(Mathf.Sin(az) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Cos(az) * Mathf.Cos(elev)).normalized;
        }

        /// <summary>Nacht: Mond übernimmt das Hauptlicht (Sonne unter −4°).</summary>
        public static bool IsNight(float hour, float noonElevation) => SunElevation(hour, noonElevation) < -4f;
    }
}
