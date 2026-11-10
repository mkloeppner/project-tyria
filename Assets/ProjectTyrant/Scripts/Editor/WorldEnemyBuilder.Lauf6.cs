using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 6 / M30 + M32: je drei Gegnertypen für die sechs neuen Zonen, Champions/Weltboss-Champions und Story-Gegner (Vareth).</summary>
    public static partial class WorldEnemyBuilder
    {
        static EnemyArchetype T(string id, string name, NpcBehavior beh, float hp, float coef, float range, float interval, float windup, float speed, float scale, LootTable loot,
            System.Func<Transform, GameObject> vis, string prompt, EnemyTier tier = EnemyTier.Trash, EffectApplication onHit = default)
        {
            var a = Arch(id);
            int xp = tier == EnemyTier.Veteran ? 190 : 60; float contrib = tier == EnemyTier.Veteran ? 32f : 10f;
            Stats(a, name, tier, hp, coef, range, interval, windup, speed, tier == EnemyTier.Veteran ? 15f : 14f, scale, xp, contrib);
            a.Behavior = beh; a.Loot = loot; a.OnHitDebuff = onHit;
            a.Prefab = Prefab("PT_Enemy_" + id, a, vis, prompt);
            return a;
        }

        static EnemyArchetype Champ(string id, string name, EnemyArchetype baseA, float hp, float defiance, LootTable loot, NpcBehavior beh, float scale = 2.6f)
        {
            var a = Arch(id);
            Stats(a, name, EnemyTier.Champion, hp, 7f, Mathf.Max(3f, baseA.AttackRange), 2.6f, 0.75f, Mathf.Max(3.4f, baseA.MoveSpeed * 0.8f), 20f, scale, 800, 120f);
            a.Behavior = beh; a.BehaviorRadius = 6f; a.BehaviorInterval = 10f; a.BehaviorCoefficient = 8f; a.SummonArchetype = baseA; a.ProjectileColor = baseA.ProjectileColor;
            a.HasTelegraph = true; a.TelegraphRadius = 6f; a.TelegraphDelay = 1.5f; a.TelegraphCoefficient = 18f; a.TelegraphInterval = 11f; a.Defiance = defiance; a.Loot = loot;
            a.Prefab = baseA.Prefab; a.Palette = baseA.Palette;
            return a;
        }

        static void BuildLauf6(LootTable trashLoot, LootTable vetLoot, LootTable champBag)
        {
            // ---- Silberküste ----
            var smug = T("Kueste_Schmuggler", "Schmuggler", NpcBehavior.None, 0.8f, 3.1f, 2.3f, 1.7f, 0.6f, 5f, 1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Smug", new CreatureBuilder.Look { Body = C(0.35f, 0.3f, 0.28f), Belly = C(0.6f, 0.45f, 0.3f), Accent = C(0.7f, 0.2f, 0.15f), Eye = new Color(0.2f, 0.2f, 0.2f) }, 1.8f, 1f, false, false, false, false),
                "stylized painterly fantasy coastal smuggler with bandana and cutlass, game-ready low poly, hand-painted texture, full body", EnemyTier.Trash, new EffectApplication(EffectType.Bleeding, 1, 4f));
            var harpy = T("Kueste_Harpyie", "Klippenharpyie", NpcBehavior.Ranged, 0.6f, 2.6f, 12f, 2.2f, 0.7f, 5.5f, 1f, trashLoot,
                p => CreatureBuilder.Bird(p, "Harpy", new CreatureBuilder.Look { Body = C(0.55f, 0.5f, 0.6f), Accent = C(0.9f, 0.75f, 0.4f), Eye = new Color(1.6f, 1.2f, 0.2f), Scale = 1.8f }),
                "stylized painterly fantasy cliff harpy with grey feathers and talons, game-ready low poly, hand-painted texture");
            harpy.ProjectileColor = new Color(0.8f, 0.8f, 1f);
            var archiv = T("Kueste_Archivwaechter", "Archivwächter", NpcBehavior.FrontShield, 3.4f, 4.4f, 2.6f, 2.3f, 0.7f, 3.8f, 1.3f, vetLoot,
                p => CreatureBuilder.Humanoid(p, "Archiv", new CreatureBuilder.Look { Body = C(0.55f, 0.48f, 0.38f), Belly = C(0.7f, 0.62f, 0.45f), Accent = C(0.4f, 0.7f, 1f), Eye = new Color(0.4f, 1f, 2f), Glow = 0.8f }, 2.1f, 1.3f, false, true, false, false),
                "stylized painterly fantasy brass and stone library guardian construct with a shield, game-ready low poly, hand-painted texture, full body", EnemyTier.Veteran);
            var queen = Champ("Kueste_Schmugglerkoenigin", "Die Schmugglerkönigin", smug, 15f, 600f, champBag, NpcBehavior.Summoner, 1.5f);

            // ---- Dämmerwald ----
            var beast = T("Wald_Wildtier", "Verderbter Keiler", NpcBehavior.Charger, 0.9f, 3.2f, 2.4f, 1.9f, 0.6f, 5.6f, 1.1f, trashLoot,
                p => CreatureBuilder.Quadruped(p, "Boar", new CreatureBuilder.Look { Body = C(0.25f, 0.2f, 0.28f), Belly = C(0.35f, 0.25f, 0.4f), Accent = C(0.6f, 0.3f, 0.9f), Eye = new Color(1.4f, 0.5f, 2f), Glow = 0.6f }, 1.4f, 0.5f, 0.7f, 0.2f, 0.2f, true, true, true),
                "stylized painterly fantasy corrupted boar with violet glowing veins, game-ready low poly, hand-painted texture");
            beast.BehaviorInterval = 9f; beast.BehaviorCoefficient = 5f; beast.BehaviorRadius = 4f;
            var thorn = T("Wald_Dornenschreck", "Dornenschreck", NpcBehavior.Ambush, 0.8f, 3f, 2.2f, 1.6f, 0.6f, 4.8f, 1.1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Thorn", new CreatureBuilder.Look { Body = C(0.22f, 0.26f, 0.18f), Belly = C(0.3f, 0.35f, 0.2f), Accent = C(0.55f, 0.25f, 0.8f), Eye = new Color(1.2f, 0.4f, 1.8f) }, 1.9f, 0.9f, true, false, false, false),
                "stylized painterly fantasy thorn horror, lanky creature of brambles with violet eyes, game-ready low poly, hand-painted texture, full body", EnemyTier.Trash, new EffectApplication(EffectType.Torment, 1, 4f));
            thorn.BehaviorRadius = 6f;
            var priest = T("Wald_Schattenpriester", "Schattenpriester", NpcBehavior.Summoner, 3f, 3.4f, 11f, 2.3f, 0.8f, 3.6f, 1.2f, vetLoot,
                p => CreatureBuilder.Humanoid(p, "Priest", new CreatureBuilder.Look { Body = C(0.15f, 0.12f, 0.2f), Belly = C(0.25f, 0.18f, 0.32f), Accent = C(0.6f, 0.3f, 1f), Eye = new Color(1.4f, 0.6f, 2.2f), Glow = 1.2f }, 1.95f, 1f, false, false, true, false),
                "stylized painterly fantasy shadow priest in tattered violet robes with a staff, game-ready low poly, hand-painted texture, full body", EnemyTier.Veteran, new EffectApplication(EffectType.Torment, 1, 3f));
            priest.SummonArchetype = thorn; priest.BehaviorInterval = 13f; priest.ProjectileColor = new Color(0.6f, 0.3f, 1f);
            var hain = Champ("Champion_AlterHain", "Der Alte Hain", thorn, 40f, 1200f, champBag, NpcBehavior.Pull, 3.8f);

            // ---- Dünenmeer ----
            var raider = T("Duene_Raeuber", "Dünenräuber", NpcBehavior.None, 0.85f, 3.2f, 2.3f, 1.6f, 0.6f, 5.2f, 1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Dune", new CreatureBuilder.Look { Body = C(0.7f, 0.55f, 0.35f), Belly = C(0.85f, 0.75f, 0.55f), Accent = C(0.2f, 0.45f, 0.6f), Eye = new Color(0.2f, 0.2f, 0.2f) }, 1.8f, 1f, false, false, false, false),
                "stylized painterly fantasy desert raider with veil and curved blade, game-ready low poly, hand-painted texture, full body");
            var worm = T("Duene_Sandwurm", "Sandwurm", NpcBehavior.Ambush, 1f, 3.4f, 3f, 2f, 0.7f, 4.6f, 1.4f, trashLoot,
                p => CreatureBuilder.Beetle(p, "Worm", new CreatureBuilder.Look { Body = C(0.75f, 0.6f, 0.4f), Belly = C(0.9f, 0.8f, 0.6f), Accent = C(0.5f, 0.3f, 0.2f), Eye = new Color(1.5f, 0.6f, 0.2f), Scale = 1.6f }),
                "stylized painterly fantasy sand worm bursting from the dunes, game-ready low poly, hand-painted texture");
            worm.BehaviorRadius = 7f;
            var glass = T("Duene_Glaselementar", "Glas-Elementar", NpcBehavior.Lob, 3.6f, 4.4f, 11f, 2.4f, 0.75f, 3.6f, 1.4f, vetLoot,
                p => CreatureBuilder.Humanoid(p, "Glass", new CreatureBuilder.Look { Body = C(0.6f, 0.85f, 0.85f), Belly = C(0.85f, 0.95f, 0.95f), Accent = C(1f, 0.85f, 0.4f), Eye = new Color(2f, 1.6f, 0.6f), Glow = 1f }, 2.2f, 1.4f, false, false, false, true),
                "stylized painterly fantasy glass elemental of fused desert sand, translucent and glowing, game-ready low poly, hand-painted texture, full body", EnemyTier.Veteran, new EffectApplication(EffectType.Bleeding, 2, 4f));
            glass.BehaviorRadius = 3.5f; glass.BehaviorInterval = 7f; glass.BehaviorCoefficient = 4f; glass.ProjectileColor = new Color(0.8f, 1f, 1f);
            var wanderer = Champ("Champion_Sturmwanderer", "Der Sturmwanderer", glass, 16f, 700f, champBag, NpcBehavior.Charger, 2.4f);

            // ---- Kristalltiefe ----
            var crawler = T("Tiefe_Kristallkrabbler", "Kristallkrabbler", NpcBehavior.None, 0.7f, 2.9f, 2f, 1.6f, 0.6f, 5f, 1f, trashLoot,
                p => CreatureBuilder.Beetle(p, "Crystal", new CreatureBuilder.Look { Body = C(0.3f, 0.3f, 0.45f), Belly = C(0.4f, 0.4f, 0.6f), Accent = C(0.5f, 0.8f, 1f), Eye = new Color(0.6f, 1.4f, 2.2f), Glow = 1.4f, Scale = 1.2f }),
                "stylized painterly fantasy crystal crawler, crab-like creature with glowing blue crystals, game-ready low poly, hand-painted texture");
            var dread = T("Tiefe_Tiefenschreck", "Tiefenschreck", NpcBehavior.Ranged, 0.7f, 2.8f, 12f, 2.3f, 0.7f, 4.2f, 1.1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Dread", new CreatureBuilder.Look { Body = C(0.12f, 0.12f, 0.18f), Belly = C(0.2f, 0.2f, 0.3f), Accent = C(0.4f, 0.5f, 1f), Eye = new Color(0.6f, 0.9f, 2.2f), Glow = 1.4f }, 1.7f, 0.8f, true, false, false, false),
                "stylized painterly fantasy deep horror, pale cave creature with luminous eyes, game-ready low poly, hand-painted texture, full body", EnemyTier.Trash, new EffectApplication(EffectType.Blind, 1, 2f));
            dread.ProjectileColor = new Color(0.5f, 0.6f, 1f);
            var hunter = T("Tiefe_BlinderJaeger", "Blinder Jäger", NpcBehavior.Charger, 3.6f, 4.8f, 2.6f, 2.2f, 0.7f, 4.6f, 1.4f, vetLoot,
                p => CreatureBuilder.Quadruped(p, "Hunter", new CreatureBuilder.Look { Body = C(0.6f, 0.58f, 0.62f), Belly = C(0.75f, 0.72f, 0.75f), Accent = C(0.35f, 0.3f, 0.4f), Eye = new Color(0.1f, 0.1f, 0.1f), Scale = 1.3f }, 1.6f, 0.6f, 0.75f, 0.35f, 0.5f, true, false, true),
                "stylized painterly fantasy eyeless cave hunter beast with big ears, game-ready low poly, hand-painted texture", EnemyTier.Veteran);
            hunter.BehaviorInterval = 8f; hunter.BehaviorCoefficient = 6f; hunter.BehaviorRadius = 4.5f;
            var heart = Champ("Champion_Kristallherz", "Kristallherz-Wächter", crawler, 16f, 700f, champBag, NpcBehavior.Summoner, 3.2f);

            // ---- Himmelsspitzen ----
            var griffin = T("Himmel_Sturmgreif", "Sturmgreif", NpcBehavior.Charger, 0.9f, 3.2f, 2.4f, 1.8f, 0.6f, 6f, 1.3f, trashLoot,
                p => CreatureBuilder.Bird(p, "Griff", new CreatureBuilder.Look { Body = C(0.55f, 0.45f, 0.35f), Accent = C(0.9f, 0.9f, 0.95f), Eye = new Color(1.8f, 1.4f, 0.3f), Scale = 2.2f }),
                "stylized painterly fantasy wild storm griffin, game-ready low poly, hand-painted texture");
            griffin.BehaviorInterval = 9f; griffin.BehaviorCoefficient = 5f; griffin.BehaviorRadius = 4f;
            var pirate = T("Himmel_Pirat", "Himmelspirat", NpcBehavior.Ranged, 0.8f, 2.9f, 13f, 2.2f, 0.7f, 4.8f, 1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "SkyPirate", new CreatureBuilder.Look { Body = C(0.3f, 0.35f, 0.5f), Belly = C(0.8f, 0.75f, 0.6f), Accent = C(0.9f, 0.7f, 0.2f), Eye = new Color(0.2f, 0.2f, 0.2f) }, 1.8f, 1f, false, false, false, false),
                "stylized painterly fantasy sky pirate with goggles and a crossbow, game-ready low poly, hand-painted texture, full body");
            pirate.ProjectileColor = new Color(0.9f, 0.9f, 0.7f);
            var serpent = T("Himmel_Wolkenschlange", "Wolkenschlange", NpcBehavior.Lob, 3.4f, 4.4f, 11f, 2.4f, 0.8f, 4.2f, 1.5f, vetLoot,
                p => CreatureBuilder.Quadruped(p, "Cloud", new CreatureBuilder.Look { Body = C(0.85f, 0.9f, 0.98f), Belly = C(1f, 1f, 1f), Accent = C(0.5f, 0.7f, 1f), Eye = new Color(0.6f, 1.2f, 2.2f), Glow = 0.8f, Scale = 1.2f }, 2.4f, 0.3f, 0.5f, 0.6f, 1.2f, false, false, true),
                "stylized painterly fantasy cloud serpent with feathered fins, white and pale blue, game-ready low poly, hand-painted texture", EnemyTier.Veteran, new EffectApplication(EffectType.Cripple, 1, 3f));
            serpent.BehaviorRadius = 4f; serpent.BehaviorInterval = 7f; serpent.BehaviorCoefficient = 4.5f; serpent.ProjectileColor = new Color(0.8f, 0.9f, 1f);
            var wyrm = Champ("Champion_Himmelswyrm", "Der Himmelswyrm", serpent, 42f, 1300f, champBag, NpcBehavior.Lob, 4f);

            // ---- Tyrannenfeste ----
            var guard = T("Feste_Garde", "Herolds-Garde", NpcBehavior.None, 1f, 3.4f, 2.4f, 1.6f, 0.6f, 4.8f, 1.1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Garde", new CreatureBuilder.Look { Body = C(0.18f, 0.16f, 0.2f), Belly = C(0.3f, 0.28f, 0.34f), Accent = C(0.7f, 0.2f, 0.3f), Eye = new Color(2f, 0.4f, 0.3f), Glow = 0.6f }, 1.95f, 1.15f, false, true, false, false),
                "stylized painterly fantasy herald's guard in dark plate armor with crimson trim, game-ready low poly, hand-painted texture, full body");
            var breaker = T("Feste_Siegelbrecher", "Siegelbrecher", NpcBehavior.Summoner, 0.9f, 3f, 10f, 2.2f, 0.75f, 4.2f, 1.1f, trashLoot,
                p => CreatureBuilder.Humanoid(p, "Breaker", new CreatureBuilder.Look { Body = C(0.2f, 0.12f, 0.15f), Belly = C(0.32f, 0.18f, 0.22f), Accent = C(1f, 0.45f, 0.2f), Eye = new Color(2f, 0.8f, 0.2f), Glow = 1.2f }, 1.85f, 0.95f, false, false, true, false),
                "stylized painterly fantasy seal breaker cultist with a glowing rune staff, game-ready low poly, hand-painted texture, full body", EnemyTier.Trash, new EffectApplication(EffectType.Burning, 1, 3f));
            breaker.SummonArchetype = guard; breaker.BehaviorInterval = 16f; breaker.ProjectileColor = new Color(1f, 0.5f, 0.2f);
            var knight = T("Feste_Ritter", "Verderbter Ritter", NpcBehavior.FrontShield, 4f, 5f, 2.8f, 2.4f, 0.75f, 3.8f, 1.5f, vetLoot,
                p => CreatureBuilder.Humanoid(p, "Knight", new CreatureBuilder.Look { Body = C(0.12f, 0.1f, 0.14f), Belly = C(0.25f, 0.15f, 0.3f), Accent = C(0.7f, 0.3f, 1f), Eye = new Color(1.4f, 0.5f, 2.2f), Glow = 1.4f }, 2.3f, 1.5f, false, true, false, true),
                "stylized painterly fantasy corrupted knight in cracked black armor leaking violet light, game-ready low poly, hand-painted texture, full body", EnemyTier.Veteran, new EffectApplication(EffectType.Torment, 1, 3f));
            var heraldMeta = Champ("Meta_Herold", "Der Herold", knight, 60f, 1500f, champBag, NpcBehavior.Charger, 1.5f);
            heraldMeta.FixedLevel = 80; heraldMeta.AttributeFactor = 1.25f;
            var herold = DataBuilder.Archetype("Story_Herold"); if (herold != null && herold.Prefab != null) heraldMeta.Prefab = herold.Prefab;

            // ---- Story: Vareth (nur in der Story-Instanz) ----
            var vareth = Arch("Story_Vareth");
            Stats(vareth, "Vareth, der Tyrann", EnemyTier.Champion, 70f, 8f, 4.5f, 2.8f, 0.85f, 3.2f, 26f, 2.7f, 3000, 400f);
            vareth.FixedLevel = 80; vareth.AttributeFactor = 1.25f; vareth.Behavior = NpcBehavior.Pull; vareth.BehaviorRadius = 7f; vareth.BehaviorInterval = 9f; vareth.BehaviorCoefficient = 9f;
            vareth.HasTelegraph = true; vareth.TelegraphRadius = 8f; vareth.TelegraphDelay = 1.7f; vareth.TelegraphCoefficient = 20f; vareth.TelegraphInterval = 10f; vareth.Defiance = 1800f;
            vareth.ProjectileColor = new Color(0.9f, 0.4f, 0.2f); vareth.SummonArchetype = guard;
            vareth.Prefab = Prefab("PT_Enemy_Story_Vareth", vareth, p => MeshyOr(p, "PT_Meshy_Story_vareth", 2.7f, CreatureBuilder.Humanoid(p, "Vareth", new CreatureBuilder.Look { Body = C(0.22f, 0.1f, 0.08f), Belly = C(0.35f, 0.15f, 0.1f), Accent = C(0.85f, 0.42f, 0.29f), Eye = new Color(2f, 0.6f, 0.2f), Glow = 1.6f }, 2.7f, 1.6f, false, false, false, true)),
                "stylized painterly fantasy ancient tyrant king with horned crown and cracked glowing skin, game-ready low poly, hand-painted texture, full body");

            foreach (var a in new[] { smug, harpy, archiv, queen, beast, thorn, priest, hain, raider, worm, glass, wanderer, crawler, dread, hunter, heart, griffin, pirate, serpent, wyrm, guard, breaker, knight, heraldMeta, vareth })
                EditorUtility.SetDirty(a);
        }
    }
}
