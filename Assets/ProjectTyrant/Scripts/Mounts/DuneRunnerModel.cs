using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Dünenläufer (Glutwüste) – Blinkschritt mit Ladungen (Leertaste: 15 m Teleport-Dash), Ladungen regenerieren am Boden,
    /// Sandportale nur mit dem Dünenläufer, Engage „Sandsturm“. Beherrschung (3 Stufen): 1 Dritte Ladung, 2 Portalmeister (größere Portale), 3 Sandgleiten (Blink in der Luft).
    /// </summary>
    public class DuneRunnerModel
    {
        public const float BlinkDistance = 15f, ChargeRegen = 1.5f, PortalRadius = 2.5f, PortalMasterRadius = 4f, EngageRadius = 6f, EngageDefiance = 200f;
        public int Rank;
        public int MaxCharges => Rank >= 1 ? 3 : 2;
        public int Charges { get; private set; }
        float _regen;

        public DuneRunnerModel(int rank = 0) { Rank = rank; Charges = MaxCharges; }

        public bool CanBlink(bool grounded) => Charges > 0 && (grounded || Rank >= 3);

        /// <summary>Blinkschritt: liefert die Zielposition (Richtung normiert, 15 m) oder false.</summary>
        public bool TryBlink(bool grounded, Vector3 from, Vector3 dir, out Vector3 to)
        {
            to = from;
            if (!CanBlink(grounded)) return false;
            dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) return false;
            Charges--; to = from + dir.normalized * BlinkDistance; return true;
        }

        public void Tick(float dt, bool grounded)
        {
            if (!grounded || Charges >= MaxCharges) { if (Charges >= MaxCharges) _regen = 0f; return; }
            _regen += dt;
            while (_regen >= ChargeRegen && Charges < MaxCharges) { _regen -= ChargeRegen; Charges++; }
        }

        public float PortalUseRadius => Rank >= 2 ? PortalMasterRadius : PortalRadius;

        /// <summary>Sandportale: nur aufgesessen auf dem Dünenläufer, im Radius des Portals.</summary>
        public static bool CanUsePortal(MountKind mounted, float distance, int rank) => mounted == MountKind.DuneRunner && distance <= (rank >= 2 ? PortalMasterRadius : PortalRadius);
    }
}
