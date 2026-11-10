using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum MountKind { None, Raptor, Springer, Skimmer, Griffon, Skyscale, DuneRunner, Skiff }   // Lauf 11: Skiff = Boot

    /// <summary>Werte eines Reittiers (aus <see cref="MountDefinition"/> oder Tests).</summary>
    [System.Serializable]
    public struct MountStats
    {
        public MountKind Kind;
        public float Speed;            // m/s am Boden
        public float WaterSpeed;       // Skimmer auf Wasser
        public float MaxHealth;
        public float LeapSpeed, LeapTime, LeapUp;        // Raptor-Sprung
        public float ChargeTime, MinJump, MaxJump;        // Springer
        public float HopHeight;        // einfacher Hüpfer (Skimmer/Raptor ohne Ladung)
        public float EngageCoefficient, EngageRadius, EngageRange;
        public float DodgeDistance, DodgeCooldown;

        public static MountStats Default(MountKind k)
        {
            switch (k)
            {
                case MountKind.Raptor: return new MountStats { Kind = k, Speed = 11.5f, WaterSpeed = 4f, MaxHealth = 6000, LeapSpeed = 18f, LeapTime = 0.9f, LeapUp = 10f, HopHeight = 1.4f, EngageCoefficient = 3.5f, EngageRadius = 4.5f, EngageRange = 10f, DodgeDistance = 6f, DodgeCooldown = 3f };
                case MountKind.Springer: return new MountStats { Kind = k, Speed = 9.5f, WaterSpeed = 3.5f, MaxHealth = 5000, ChargeTime = 1.2f, MinJump = 3f, MaxJump = 15f, HopHeight = 3f, EngageCoefficient = 4.5f, EngageRadius = 5f, EngageRange = 12f, DodgeDistance = 5f, DodgeCooldown = 3f };
                case MountKind.Skimmer: return new MountStats { Kind = k, Speed = 10f, WaterSpeed = 12.5f, MaxHealth = 5000, HopHeight = 1.6f, EngageCoefficient = 2.5f, EngageRadius = 4f, EngageRange = 8f, DodgeDistance = 6f, DodgeCooldown = 3f };
                case MountKind.Griffon: return new MountStats { Kind = k, Speed = 10.5f, WaterSpeed = 3.5f, MaxHealth = 5500, HopHeight = 9f, EngageCoefficient = 4f, EngageRadius = 5f, EngageRange = 14f, DodgeDistance = 6f, DodgeCooldown = 3f };   // Lauf 6: Greif (Gleitflug, Sturzflug)
                case MountKind.Skiff: return new MountStats { Kind = k, Speed = 2f, WaterSpeed = BoatModel.MaxSpeed, MaxHealth = 8000, HopHeight = 0f, EngageCoefficient = 0f, EngageRadius = 0f, EngageRange = 0f, DodgeDistance = 0f, DodgeCooldown = 99f };   // Lauf 11: Boot (nur Wasser)
                case MountKind.DuneRunner: return new MountStats { Kind = k, Speed = 12.5f, WaterSpeed = 3.5f, MaxHealth = 5000, HopHeight = 1.6f, EngageCoefficient = 3f, EngageRadius = 6f, EngageRange = 12f, DodgeDistance = 6f, DodgeCooldown = 3f };   // Lauf 11: Dünenläufer (Sprinter, Blinkschritt)
                case MountKind.Skyscale: return new MountStats { Kind = k, Speed = 9.1f, WaterSpeed = 3.5f, MaxHealth = 6000, HopHeight = 2f, EngageCoefficient = 3f, EngageRadius = 6f, EngageRange = 12f, DodgeDistance = 6f, DodgeCooldown = 3f };   // Lauf 10: Himmelsschuppe (Laufen 1,3× – langsam am Boden)
            }
            return new MountStats { Kind = MountKind.None };
        }
    }

    /// <summary>
    /// Reiner Zustand des Reitens (getestet): 1 s Aufsitzen (Schaden bricht ab), Reittier-HP fangen Schaden ab und werfen bei 0 ab,
    /// Raptor-Sprung, Springer-Ladesprung, Skimmer schwebt über Wasser, Angriff beim Absitzen (Taste 1), Reittier-Ausweichen.
    /// </summary>
    public class MountMotor
    {
        public const float MountTime = 1f;
        public const float TapThreshold = 0.35f;   // X: kürzer = Auf-/Absitzen, länger = Radialmenü
        public const float RemountCooldownAfterKnockoff = 4f;

        public MountStats Stats { get; private set; }
        public MountKind Mounted => Stats.Kind;
        public bool IsMounted => Stats.Kind != MountKind.None;
        public MountKind Channeling { get; private set; }
        public float ChannelProgress { get; private set; }
        public float Health { get; private set; }
        public float Charge { get; private set; }
        public bool Charging { get; private set; }
        public float LeapRemaining { get; private set; }
        /// <summary>Lauf 5: Zähler für Animations-Auslöser (Sprung, Angriff, Abwurf).</summary>
        public int LeapCount { get; private set; }
        public int EngageCount { get; private set; }
        public int KnockOffCount { get; private set; }
        public float DodgeCooldown { get; private set; }
        public float RemountBlock { get; private set; }
        /// <summary>Lauf 4 / M18: Bonus aus der Reittier-Beherrschung (HP-Faktor, Sprungweite, Ladezeit).</summary>
        public float HealthFactor = 1f, LeapFactor = 1f, ChargeFactor = 1f;

        public bool IsChanneling => Channeling != MountKind.None;
        public float HealthFraction => Stats.MaxHealth <= 0 ? 0 : Health / (Stats.MaxHealth * HealthFactor);

        /// <summary>Beginnt das Aufsitzen (nicht in der Luft, nicht während einer Sperre).</summary>
        public bool BeginMount(MountStats s, bool grounded, bool owned)
        {
            if (!owned || s.Kind == MountKind.None || IsMounted || IsChanneling || !grounded || RemountBlock > 0f) return false;
            Channeling = s.Kind; ChannelProgress = 0f; _pending = s;
            return true;
        }
        MountStats _pending;

        public void CancelChannel() { Channeling = MountKind.None; ChannelProgress = 0f; }

        /// <summary>Pro Frame. Gibt true zurück, wenn das Aufsitzen gerade abgeschlossen wurde.</summary>
        public bool Tick(float dt)
        {
            if (DodgeCooldown > 0f) DodgeCooldown -= dt;
            if (RemountBlock > 0f) RemountBlock -= dt;
            if (LeapRemaining > 0f) LeapRemaining -= dt;
            if (Charging) Charge = Mathf.Min(1f, Charge + dt / Mathf.Max(0.05f, Stats.ChargeTime * ChargeFactor));
            if (!IsChanneling) return false;
            ChannelProgress += dt / MountTime;
            if (ChannelProgress < 1f) return false;
            Stats = _pending; Health = Stats.MaxHealth * HealthFactor;
            Channeling = MountKind.None; ChannelProgress = 0f;
            return true;
        }

        public void Dismount() { Stats = MountStats.Default(MountKind.None); Charging = false; Charge = 0f; LeapRemaining = 0f; }

        /// <summary>Schaden während des Reitens/Aufsitzens. Rückgabe = Schaden, der beim Spieler ankommt (0 beim Reiten).</summary>
        public float AbsorbDamage(float amount, out bool knockedOff)
        {
            knockedOff = false;
            if (IsChanneling) { CancelChannel(); return amount; }
            if (!IsMounted || amount <= 0f) return amount;
            Health -= amount;
            if (Health > 0f) return 0f;
            knockedOff = true; KnockOffCount++;
            Dismount();
            RemountBlock = RemountCooldownAfterKnockoff;
            return 0f;
        }

        // ---------------- Fähigkeiten ----------------
        /// <summary>Raptor: Sprung nach vorn. Liefert horizontale Geschwindigkeit/Dauer/Steigung.</summary>
        public bool TryLeap(out float speed, out float time, out float up)
        {
            speed = time = up = 0f;
            if (Mounted != MountKind.Raptor || LeapRemaining > 0f) return false;
            speed = Stats.LeapSpeed * LeapFactor; time = Stats.LeapTime; up = Stats.LeapUp;
            LeapRemaining = time; LeapCount++;
            return true;
        }

        /// <summary>Springer: Leertaste halten lädt.</summary>
        public void BeginCharge() { if (Mounted == MountKind.Springer) { Charging = true; Charge = 0f; } }

        /// <summary>Springer: Loslassen → Sprunghöhe (MinJump…MaxJump).</summary>
        public float ReleaseCharge()
        {
            if (!Charging) return 0f;
            Charging = false;
            float h = Mathf.Lerp(Stats.MinJump, Stats.MaxJump, Charge);
            Charge = 0f;
            return h;
        }

        public static float JumpVelocity(float height, float gravity) => Mathf.Sqrt(2f * Mathf.Max(0f, height) * Mathf.Abs(gravity));
        /// <summary>Horizontale Sprungweite eines Raptor-Sprungs auf ebenem Boden.</summary>
        public static float LeapDistance(MountStats s, float factor = 1f) => s.LeapSpeed * factor * s.LeapTime;

        public bool TryDodge() { if (!IsMounted || DodgeCooldown > 0f) return false; DodgeCooldown = Stats.DodgeCooldown; return true; }

        /// <summary>Angriff beim Absitzen (Taste 1): sitzt ab, liefert Koeffizient und Radius.</summary>
        public bool TryEngage(float targetDistance, out float coefficient, out float radius)
        {
            coefficient = radius = 0f;
            if (!IsMounted || targetDistance > Stats.EngageRange) return false;
            coefficient = Stats.EngageCoefficient; radius = Stats.EngageRadius;
            EngageCount++;
            Dismount();
            return true;
        }

        public bool HoversOnWater => Mounted == MountKind.Skimmer || Mounted == MountKind.Skiff;
        public float GroundSpeed(bool onWater) => !IsMounted ? 0f : onWater ? Stats.WaterSpeed : Stats.Speed;
    }

    /// <summary>X-Taste: Antippen = Auf-/Absitzen, Halten = Radialmenü (rein, getestet).</summary>
    public static class MountInput
    {
        public enum Result { None, Toggle, OpenRadial }
        public static Result Evaluate(bool held, float heldTime, bool releasedThisFrame)
        {
            if (held && heldTime >= MountMotor.TapThreshold) return Result.OpenRadial;
            if (releasedThisFrame && heldTime < MountMotor.TapThreshold) return Result.Toggle;
            return Result.None;
        }
    }

    /// <summary>Erwerb: Herz der Zone erfüllt + Gold (rein, getestet).</summary>
    public static class MountShop
    {
        public enum Verdict { Ok, AlreadyOwned, HeartMissing, NotEnoughGold }
        public static Verdict Check(MountKind k, ICollection<string> owned, ICollection<string> completed, string requiredHeart, long copper, long price)
        {
            if (owned.Contains(k.ToString())) return Verdict.AlreadyOwned;
            if (!string.IsNullOrEmpty(requiredHeart) && !completed.Contains(requiredHeart)) return Verdict.HeartMissing;
            if (copper < price) return Verdict.NotEnoughGold;
            return Verdict.Ok;
        }

        /// <summary>Welche Reittier-Kombination die Besitzliste freischaltet (für „Benötigt: …“).</summary>
        public static Traversal Unlocked(ICollection<string> owned)
        {
            var t = Traversal.None;
            if (owned.Contains(MountKind.Raptor.ToString())) t |= Traversal.Raptor;
            if (owned.Contains(MountKind.Springer.ToString())) t |= Traversal.Springer;
            if (owned.Contains(MountKind.Skimmer.ToString())) t |= Traversal.Skimmer;
            return t;
        }
    }
}
