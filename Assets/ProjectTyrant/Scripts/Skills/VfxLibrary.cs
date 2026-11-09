using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Referenzen auf Runtime-Materialien (damit sie im Build enthalten sind). Wird über den GameManager bereitgestellt.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Vfx Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        public Material GroundMarker;
        public Material ProjectileGlow;
        public Material FlashSolid;
        public Material HitFlash;
        [Tooltip("Lauf 5 / M23: weiche Partikel (additiv, Glow-Textur) statt quadratischer FlashSolid-Partikel")] public Material ParticleGlow;
        [Tooltip("Lauf 5 / M23: weiche Tropfen (Alpha) für Blut")] public Material ParticleDrop;
        public AudioClip HitSound;
        public AudioClip CastSound;
        public AudioClip ComboSound;
        public AudioClip DodgeSound;
        public AudioClip EventSound;
        public AudioClip LootSound, LevelUpSound, DiscoverSound, EquipSound, CoinSound, FailSound;

        /// <summary>Material für einfache Laufzeit-Partikel: weich, falls vorhanden, sonst FlashSolid.</summary>
        public static Material Particle(bool drop = false)
        {
            var lib = Current; if (lib == null) return null;
            var m = drop ? lib.ParticleDrop : lib.ParticleGlow;
            return m != null ? m : lib.FlashSolid;
        }

        public static VfxLibrary Current => GameManager.Instance != null ? GameManager.Instance.Vfx : null;
    }
}
