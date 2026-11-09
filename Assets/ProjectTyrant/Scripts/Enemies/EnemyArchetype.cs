using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Sonderverhalten neuer Gegnertypen (ausgeführt von <see cref="NpcAbilities"/>).</summary>
    public enum NpcBehavior { None, Ambush, Ranged, Pull, PackHowl, FrontShield, Lob, Exploder, Summoner, Charger }

    [CreateAssetMenu(menuName = "Project Tyrant/Enemy Archetype", fileName = "Enemy_")]
    public class EnemyArchetype : ScriptableObject
    {
        public string DisplayName = "Gegner";
        public EnemyTier Tier = EnemyTier.Trash;
        public Team Team = Team.Enemy;
        public GameObject Prefab;

        [Header("Level & Attribute (GW2-Formel)")]
        [Tooltip("0 = Level aus dem Gebiet (ZoneLayout.Regions).")]
        public int FixedLevel;
        [Tooltip("Faktor auf die Primärattribute der Stufe.")]
        public float AttributeFactor = 1f;
        [Tooltip("Rüstung = Zähigkeit + Faktor × Primärbasis.")]
        public float ArmorFactor = 1.6f;
        [Tooltip("Lebenspunkte = NPC-Basis(Level) × Faktor.")]
        public float HealthFactor = 1f;
        [Tooltip(">0: feste Lebenspunkte (Tests/Objekte).")]
        public float MaxHealth = 0f;
        public float AttackCoefficient = 4f;
        public float AttackRange = 2.4f;
        public float AttackInterval = 2f;
        public float AttackWindup = 0.6f;
        [Tooltip("Lauf 5: Effekt-Palette der Angriffe (Aufladung, Einschlag)")] public VfxPalette Palette = VfxPalette.Blut;
        public float MoveSpeed = 4.5f;
        public float AggroRange = 14f;
        public float LeashRange = 40f;
        public float Scale = 1f;
        public EffectApplication OnHitDebuff;

        [Header("Telegraph (Champion)")]
        public bool HasTelegraph;
        public float TelegraphRadius = 5f;
        public float TelegraphDelay = 1.5f;
        public float TelegraphCoefficient = 25f;
        public float TelegraphInterval = 9f;

        [Header("Sonderverhalten (Lauf 4)")]
        public NpcBehavior Behavior;
        public float BehaviorRadius = 6f;
        public float BehaviorInterval = 8f;
        public float BehaviorCoefficient = 4f;
        public EffectApplication BehaviorEffect;
        public EnemyArchetype SummonArchetype;
        public Color ProjectileColor = new Color(0.5f, 0.9f, 0.3f);

        [Tooltip("Lauf 4: Trotzleiste (0 = keine)")] public float Defiance;

        [Header("Belohnung")]
        public float ContributionValue = 10f;
        public int Experience = 50;
        public LootTable Loot;

        /// <summary>NPC-Basis-HP, skaliert wie Spielerschaden (Waffenstärke).</summary>
        public static float NpcBaseHealth(int level) => 160f * DamageFormula.WeaponStrength(level) / 55f;

        public int PickLevel(Vector3 pos)
        {
            if (FixedLevel > 0) return FixedLevel;
            var r = ZoneRoot.LevelRangeAt(pos);
            int span = Mathf.Max(0, r.y - r.x);
            int h = Mathf.Abs(Mathf.RoundToInt(pos.x * 7.31f + pos.z * 3.17f)) % (span + 1);
            int lvl = r.x + h;
            if (Tier == EnemyTier.Veteran) lvl += 1;
            return Mathf.Clamp(lvl, 1, AttributeSet.MaxLevel);
        }
    }
}
