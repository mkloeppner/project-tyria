using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// 2D-Fortbewegungsmischung (rein): Gewichte für [Idle, Walk, Run, Sprint, StrafeL, StrafeR, Back] aus lokaler Geschwindigkeit.
    /// Summe = 1. Vorwärts-Familie wird nach Tempo zwischen Gehen/Laufen/Sprinten interpoliert; Phasenrate gemeinsam (keine Fußrutscher beim Mischen).
    /// </summary>
    public static class LocomotionBlend
    {
        public const int Idle = 0, Walk = 1, Run = 2, Sprint = 3, StrafeL = 4, StrafeR = 5, Back = 6, Count = 7;
        public static readonly string[] Names = { "Idle", "Walk", "Run", "Sprint", "StrafeL", "StrafeR", "Back" };
        /// <summary>Nenngeschwindigkeit je Clip (m/s), bei der die Schrittlänge zur Bewegung passt.</summary>
        public static readonly float[] NominalSpeed = { 0f, 2.6f, 7f, 10f, 4.2f, 4.2f, 4.5f };

        public static void Weights(float forward, float right, float[] w, float walkSpeed = 2.6f, float runSpeed = 7f, float sprintSpeed = 10f)
        {
            for (int i = 0; i < Count; i++) w[i] = 0f;
            float s = Mathf.Sqrt(forward * forward + right * right);
            float move = Mathf.Clamp01(s / 1.2f);
            w[Idle] = 1f - move;
            if (move <= 0f) return;
            float f = Mathf.Max(0f, forward) / s, b = Mathf.Max(0f, -forward) / s, l = Mathf.Max(0f, -right) / s, r = Mathf.Max(0f, right) / s;
            float sum = f + b + l + r; f /= sum; b /= sum; l /= sum; r /= sum;
            // Vorwärts: Gehen → Laufen → Sprinten
            float fw = move * f;
            if (s <= walkSpeed) w[Walk] += fw;
            else if (s <= runSpeed) { float u = (s - walkSpeed) / (runSpeed - walkSpeed); w[Walk] += fw * (1f - u); w[Run] += fw * u; }
            else { float u = Mathf.Clamp01((s - runSpeed) / (sprintSpeed - runSpeed)); w[Run] += fw * (1f - u); w[Sprint] += fw * u; }
            w[StrafeL] = move * l; w[StrafeR] = move * r; w[Back] = move * b;
        }

        /// <summary>Phasenrate (Zyklen/s): gewichtetes Mittel aus 1/Länge × Tempoverhältnis (begrenzt 0,6–1,7).</summary>
        public static float PhaseRate(float[] w, float[] lengths, float speed)
        {
            float rate = 0f, tw = 0f;
            for (int i = 1; i < Count; i++)
            {
                if (w[i] <= 0f || lengths[i] <= 0f) continue;
                float k = Mathf.Clamp(speed / Mathf.Max(0.1f, NominalSpeed[i]), 0.6f, 1.7f);
                rate += w[i] * k / lengths[i]; tw += w[i];
            }
            return tw <= 0f ? 0f : rate / tw;
        }
    }
}
