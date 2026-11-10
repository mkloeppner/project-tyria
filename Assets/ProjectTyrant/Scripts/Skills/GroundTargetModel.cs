using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Fix-Lauf F1 – Bodenziel-Modi als Zustandsmaschine (rein, getestet): Idle → Vorschau → Wirken / Abbruch.
    /// Normal: Taste zeigt Vorschau, Klick oder dieselbe Taste wirkt. Drücken und Loslassen: Halten zeigt Vorschau (ab Schwelle),
    /// Loslassen wirkt, kurzes Tippen wirkt sofort. Sofort: ohne Vorschau. Sofort mit Reichweitenanzeige: zusätzlich 0,5 s Ringe.
    /// </summary>
    public class GroundTargetModel
    {
        public enum Mode { Normal = 0, PressRelease = 1, Instant = 2, InstantWithRange = 3 }
        public enum State { Idle, Preview }
        public enum Action { None, Preview, Cast, CastWithRangeFlash, Cancel }
        public const float RangeFlashSeconds = 0.5f;

        public Mode CurrentMode { get; private set; } = Mode.Normal;
        public float HoldThreshold = 0.15f;
        public bool ClampToRange = true, SnapToTarget;
        public State Current { get; private set; } = State.Idle;
        public int Slot { get; private set; } = -1;
        public float PressTime { get; private set; }

        public static string Name(Mode m) => m == Mode.Normal ? "Normal (Maus bestätigt)" : m == Mode.PressRelease ? "Drücken und Loslassen" : m == Mode.Instant ? "Sofort" : "Sofort mit Reichweitenanzeige";

        /// <summary>Modus wechseln – eine offene Vorschau wird abgebrochen.</summary>
        public Action SetMode(Mode m)
        {
            if (m == CurrentMode) return Action.None;
            CurrentMode = m;
            if (Current == State.Preview) { Reset(); return Action.Cancel; }
            return Action.None;
        }

        /// <summary>Skill-Taste eines Bodenziel-Skills gedrückt.</summary>
        public Action KeyDown(int slot, float time)
        {
            switch (CurrentMode)
            {
                case Mode.Instant: Reset(); return Action.Cast;
                case Mode.InstantWithRange: Reset(); return Action.CastWithRangeFlash;
                case Mode.PressRelease: Slot = slot; PressTime = time; Current = State.Preview; return Action.Preview;
                default:
                    if (Current == State.Preview && Slot == slot) { Reset(); return Action.Cast; }
                    Slot = slot; PressTime = time; Current = State.Preview; return Action.Preview;
            }
        }

        /// <summary>Skill-Taste losgelassen (nur „Drücken und Loslassen“ wirkt hier – auch beim kurzen Tippen).</summary>
        public Action KeyUp(int slot, float time)
        {
            if (CurrentMode != Mode.PressRelease || Current != State.Preview || slot != Slot) return Action.None;
            Reset(); return Action.Cast;
        }

        public Action Click() { if (Current != State.Preview) return Action.None; Reset(); return Action.Cast; }
        public Action Escape() { if (Current != State.Preview) return Action.None; Reset(); return Action.Cancel; }
        /// <summary>Andere Skill-Taste während der Vorschau: abbrechen.</summary>
        public Action OtherSkill(int slot) { if (Current != State.Preview || slot == Slot) return Action.None; Reset(); return Action.Cancel; }
        public void Reset() { Current = State.Idle; Slot = -1; }

        /// <summary>Ist die Vorschau gerade sichtbar? (Drücken und Loslassen: erst nach der Halte-Schwelle.)</summary>
        public bool PreviewVisible(float time) => Current == State.Preview && (CurrentMode != Mode.PressRelease || time - PressTime >= HoldThreshold);
        public bool IsTap(float pressTime, float releaseTime) => releaseTime - pressTime < HoldThreshold;

        /// <summary>Zielpunkt auflösen: Einrasten auf das Ziel (falls an und in Reichweite), Reichweite begrenzen oder abbrechen.</summary>
        public bool Resolve(Vector3 origin, Vector3 cursor, float range, Vector3? target, out Vector3 point, out bool clamped)
        {
            point = cursor; clamped = false;
            if (SnapToTarget && target.HasValue) { var td = target.Value - origin; td.y = 0f; if (td.magnitude <= range) point = target.Value; }
            var d = point - origin; d.y = 0f;
            if (d.magnitude <= range) return true;
            clamped = true;
            if (!ClampToRange) return false;
            point = origin + d.normalized * range; point.y = cursor.y;
            return true;
        }
    }
}
