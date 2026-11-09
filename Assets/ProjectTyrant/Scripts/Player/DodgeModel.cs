using System;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Ausdauer: 2 Ladungen à 50, volle Regeneration in 10 s.</summary>
    [Serializable]
    public class EnduranceModel
    {
        public float Max = 100f;
        public float RegenPerSecond = 10f;
        public float DodgeCost = 50f;
        [NonSerialized] public float RegenMultiplier = 1f;
        [NonSerialized] public float Current = 100f;

        public int Charges => Mathf.FloorToInt(Current / DodgeCost + 0.0001f);
        public float Fraction => Max <= 0 ? 0 : Current / Max;

        public void Fill() => Current = Max;

        public bool TryConsume(float amount)
        {
            if (Current + 0.0001f < amount) return false;
            Current = Mathf.Max(0f, Current - amount);
            return true;
        }

        public void Tick(float dt) => Current = Mathf.Min(Max, Current + RegenPerSecond * RegenMultiplier * dt);
    }

    /// <summary>Dodge-Roll: Bewegung über DodgeDuration, 0,75 s i-Frames. Reine Logik, testbar.</summary>
    [Serializable]
    public class DodgeModel : IInvulnerabilitySource
    {
        public EnduranceModel Endurance = new EnduranceModel();
        public float IFrameDuration = 0.75f;
        public float DodgeDuration = 0.55f;
        public float DodgeDistance = 6.5f;

        [NonSerialized] float _iFrames;
        [NonSerialized] float _dodgeTime;
        [NonSerialized] Vector3 _direction;

        public bool IsDodging => _dodgeTime > 0f;
        public bool IsInvulnerable => _iFrames > 0f;
        public float DodgeProgress => DodgeDuration <= 0 ? 1f : 1f - Mathf.Clamp01(_dodgeTime / DodgeDuration);
        public Vector3 Direction => _direction;

        public bool TryStartDodge(Vector3 direction)
        {
            if (IsDodging) return false;
            if (!Endurance.TryConsume(Endurance.DodgeCost)) return false;
            direction.y = 0f;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.back;
            _iFrames = IFrameDuration;
            _dodgeTime = DodgeDuration;
            return true;
        }

        /// <summary>Gibt die horizontale Verschiebung dieses Frames zurück.</summary>
        public Vector3 Tick(float dt)
        {
            Endurance.Tick(dt);
            if (_iFrames > 0f) _iFrames = Mathf.Max(0f, _iFrames - dt);
            if (_dodgeTime <= 0f) return Vector3.zero;
            float step = Mathf.Min(dt, _dodgeTime);
            _dodgeTime -= step;
            return _direction * (DodgeDistance / DodgeDuration) * step;
        }

        public void Cancel() { _dodgeTime = 0f; }
    }
}
