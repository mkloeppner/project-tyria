using ProjectTyrant.Anim;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M22: Treffer-Gefühl (hängt am GameManager-Objekt, kein Singleton): Hit-Stop 40–80 ms auf Angreifer und Ziel bei schweren
    /// Treffern/Krits, Kamera-Impuls nach Schadensklasse (Cinemachine Impulse, abschaltbar), Splitter + dumpfer Ton bei Unterbrechung.
    /// </summary>
    public class CombatFeel : MonoBehaviour
    {
        public static bool HitStopEnabled = true, CameraImpulseEnabled = true;
        public static float LastHitStop { get; private set; }
        public static float LastImpulse { get; private set; }
        public static int HitStops { get; private set; }
        CinemachineImpulseSource _impulse;
        ParticleSystem _shards;

        Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        void Awake()
        {
            HitStopEnabled = PlayerPrefs.GetInt("PT_HitStop", 1) == 1;
            CameraImpulseEnabled = PlayerPrefs.GetInt("PT_CamImpulse", 1) == 1;
            _impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            _impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _impulse.ImpulseDefinition.ImpulseDuration = 0.18f;
            _impulse.DefaultVelocity = new Vector3(0f, -0.25f, 0f);
            var go = new GameObject("InterruptShards"); go.transform.SetParent(transform, false);
            _shards = go.AddComponent<ParticleSystem>();
            _shards.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _shards.main; main.playOnAwake = false; main.loop = false; main.startLifetime = 0.45f; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f); main.gravityModifier = 1.2f; main.maxParticles = 120; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.85f, 1f);
            var em = _shards.emission; em.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); var pm = VfxLibrary.Particle(); if (pm != null) r.sharedMaterial = pm;
        }

        void OnEnable() { GameEventBus.DamageDealt += OnDamage; GameEventBus.CastInterrupted += OnInterrupted; }
        void OnDisable() { GameEventBus.DamageDealt -= OnDamage; GameEventBus.CastInterrupted -= OnInterrupted; }

        void OnDamage(DamageInfo d)
        {
            if (d.IsCondition || d.Evaded || d.Missed || d.Amount <= 0f || d.Target == null) return;
            var p = Player;
            bool involvesPlayer = p != null && (d.Source == p || d.Target == p);
            if (!involvesPlayer) return;
            float max = Mathf.Max(1f, d.Target.MaxHealth);
            // Spieler-Ziel: Klasse nach Spieler-HP; Gegner-Ziel: nach Gegner-HP (Krits/Schwere spürbar, Kleinkram nicht)
            float stop = HitStopEnabled ? HitStopModel.Duration(d.Amount, max, d.Crit) : 0f;
            if (stop > 0f)
            {
                Freeze(d.Source, stop); Freeze(d.Target, stop);
                LastHitStop = stop; HitStops++;
            }
            var cls = HitStopModel.Classify(d.Amount, max);
            float imp = HitStopModel.Impulse(cls, d.Crit) * (d.Target == p ? 1.2f : 0.8f);
            if (CameraImpulseEnabled && imp > 0f && _impulse != null) { _impulse.GenerateImpulseWithForce(imp); LastImpulse = imp; }
        }

        static void Freeze(Combatant c, float t)
        {
            if (c == null) return;
            var a = c.GetComponentInChildren<CharacterAnimator>();
            if (a != null) a.Freeze(t);
        }

        void OnInterrupted(Combatant c, SkillDefinition s)
        {
            if (c == null) return;
            if (_shards != null) _shards.Emit(new ParticleSystem.EmitParams { position = c.AimPoint, applyShapeToPosition = true }, 24);
            GameAudio.Play("Combat/Interrupted", c.AimPoint, 0f, 1f, VoicePriority.PlayerSkill);   // Lauf 5 / M24: dumpfer Ton über den Pool
        }
    }
}
