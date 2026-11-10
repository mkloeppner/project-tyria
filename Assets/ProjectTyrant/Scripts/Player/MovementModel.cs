using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Fix-Lauf F1 – Bewegungsmodus (rein, getestet). GW2-Standard „Drehen“: A/D drehen den Charakter (180°/s), W/S laufen entlang
    /// seiner Blickrichtung, Q/E gehen seitwärts. Bei gehaltener rechter Maustaste, in der Action-Cam, auf Reittieren oder im Modus
    /// „Seitwärts“ gilt das bisherige Verhalten (A/D seitwärts relativ zur Kamera, Charakter dreht in Laufrichtung bzw. zur Kamera).
    /// </summary>
    public static class MovementModel
    {
        public enum Mode { Turn = 0, Strafe = 1 }
        public const float TurnRate = 180f;

        public struct Result
        {
            public bool Turning;        // Drehmodus aktiv (A/D drehen)
            public float YawDelta;      // Grad in diesem Frame
            public Vector3 Wish;        // gewünschte Laufrichtung (Welt, Länge ≤ 1)
            public bool FaceWish;       // Charakter dreht sich in Laufrichtung (bisheriges Verhalten)
        }

        public static bool TurnActive(Mode mode, bool rmbHeld, bool actionCam, bool mounted) => mode == Mode.Turn && !rmbHeld && !actionCam && !mounted;

        public static Result Compute(Mode mode, bool rmbHeld, bool actionCam, bool mounted, Vector2 move, float strafe, float characterYaw, Vector3 camForward, float dt)
        {
            var r = new Result();
            if (TurnActive(mode, rmbHeld, actionCam, mounted))
            {
                r.Turning = true;
                r.YawDelta = Mathf.Clamp(move.x, -1f, 1f) * TurnRate * dt;
                var rot = Quaternion.Euler(0f, characterYaw + r.YawDelta, 0f);
                Vector3 f = rot * Vector3.forward, right = rot * Vector3.right;
                r.Wish = f * move.y + right * Mathf.Clamp(strafe, -1f, 1f);
                r.FaceWish = false;
            }
            else
            {
                camForward.y = 0f; if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward; camForward.Normalize();
                Vector3 camRight = Vector3.Cross(Vector3.up, camForward);
                r.Wish = camForward * move.y + camRight * Mathf.Clamp(move.x + strafe, -1f, 1f);
                r.FaceWish = true;
            }
            if (r.Wish.sqrMagnitude > 1f) r.Wish.Normalize();
            return r;
        }
    }
}
