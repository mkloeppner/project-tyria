using System;

namespace ProjectTyrant
{
    /// <summary>
    /// Fix-Lauf F1 – Kamera-Zoom (rein, getestet): Abstand in Metern, Raster je Mausrad-Raste (0,25–1,5 m, Standard 0,75),
    /// Bereich zu Fuß 1,5–12 m, beritten 2,5–16 m, weich gedämpft (0,15 s), Richtung umkehrbar, gesperrt in Kamerafahrten.
    /// </summary>
    public class CameraZoomModel
    {
        public const float FootMin = 1.5f, FootMax = 12f, MountMin = 2.5f, MountMax = 16f, DefaultDistance = 6f, DefaultStep = 0.75f, Damping = 0.15f;
        public const float StepMin = 0.25f, StepMax = 1.5f;

        public float Min { get; private set; } = FootMin;
        public float Max { get; private set; } = FootMax;
        public float Target { get; private set; } = DefaultDistance;
        public float Distance { get; private set; } = DefaultDistance;
        public float Step = DefaultStep;
        public bool Invert;
        public bool Locked;
        float _vel;

        /// <summary>Mausrad-Rohwert → Rasten. Input System ≥ 1.10 liefert ±1 je Raste (normalisiert), ältere/Legacy-Pfade ±120.</summary>
        public static float Notches(float raw) => Math.Abs(raw) >= 20f ? raw / 120f : raw;

        /// <summary>Rasten anwenden: vorwärts (positiv) zoomt heran.</summary>
        public void Scroll(float notches)
        {
            if (Locked || Math.Abs(notches) < 0.0001f) return;
            float dir = Invert ? -1f : 1f;
            Target = Clamp(Target - notches * dir * Clamp(Step, StepMin, StepMax), Min, Max);
        }

        public void SetMounted(bool mounted) => SetRange(mounted ? MountMin : FootMin, mounted ? MountMax : FootMax);

        public void SetRange(float min, float max) { Min = min; Max = max; Target = Clamp(Target, min, max); }

        /// <summary>Standardabstand setzen (Optionen/Laden) – sofort, ohne Dämpfung.</summary>
        public void Snap(float distance) { Target = Distance = Clamp(distance, Min, Max); _vel = 0f; }

        /// <summary>Kritisch gedämpfte Annäherung (SmoothDamp-Formel), ~0,15 s Glättungszeit.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            float omega = 2f / Damping, x = omega * dt, exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = Distance - Target, temp = (_vel + omega * change) * dt;
            _vel = (_vel - omega * temp) * exp;
            float output = Target + (change + temp) * exp;
            if ((Target - Distance > 0f) == (output > Target)) { output = Target; _vel = 0f; }
            Distance = output;
        }

        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }
}
