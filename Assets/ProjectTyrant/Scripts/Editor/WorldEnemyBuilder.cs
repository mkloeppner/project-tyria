using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 4: 9 neue Gegnertypen (3 je Biom-Zone) mit Sonderverhalten + Umgebungstiere – alle prozedural (ab Lauf 5 geriggt, CharacterAnimator).</summary>
    public static partial class WorldEnemyBuilder
    {
        const string EnemyDir = DataDir + "/Enemies";
        const string NpcPrefabDir = PrefabDir + "/Characters";
        const string CritterDir = PrefabDir + "/Critters";

        static EnemyArchetype Arch(string id)
        {
            string path = EnemyDir + "/Enemy_" + id + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<EnemyArchetype>(); AssetDatabase.CreateAsset(a, path); }
            a.Team = Team.Enemy; a.FixedLevel = 0; a.MaxHealth = 0f; a.HasTelegraph = false; a.OnHitDebuff = default; a.BehaviorEffect = default; a.SummonArchetype = null;
            a.Loot = null; a.Defiance = 0f;
            return a;
        }

        static void Stats(EnemyArchetype a, string name, EnemyTier tier, float hp, float coef, float range, float interval, float windup, float speed, float aggro, float scale, int xp, float contrib)
        {
            a.DisplayName = name; a.Tier = tier; a.HealthFactor = hp; a.AttackCoefficient = coef; a.AttackRange = range; a.AttackInterval = interval; a.AttackWindup = Mathf.Max(NpcBrain.MinWindup, windup);   // Lauf 5: Telegraph ≥ 0,6 s
            a.MoveSpeed = speed; a.AggroRange = aggro; a.LeashRange = 45f; a.Scale = scale; a.Experience = xp; a.ContributionValue = contrib; a.AttributeFactor = tier == EnemyTier.Veteran ? 1.1f : 1f;
            a.ArmorFactor = 1.6f;
        }

        public static void BuildAll()
        {
            EnsureFolder(EnemyDir); EnsureFolder(NpcPrefabDir); EnsureFolder(CritterDir);
            var trashLoot = ItemBuilder.Table("Trash"); var vetLoot = ItemBuilder.Table("Veteran");

            // ---- Nebelmoor ----
            var lurk = Arch("Moor_Schleicher");
            Stats(lurk, "Moorschleicher", EnemyTier.Trash, 0.8f, 3.2f, 2.3f, 1.8f, 0.4f, 4.8f, 10f, 1f, 55, 10f);
            lurk.Behavior = NpcBehavior.Ambush; lurk.BehaviorRadius = 6f; lurk.OnHitDebuff = new EffectApplication(EffectType.Poison, 1, 4f); lurk.Loot = trashLoot;
            lurk.Prefab = Prefab("PT_Enemy_Moor_Schleicher", lurk, p => CreatureBuilder.Humanoid(p, "Lurk", new CreatureBuilder.Look { Body = C(0.3f, 0.4f, 0.25f), Belly = C(0.42f, 0.5f, 0.3f), Accent = C(0.2f, 0.3f, 0.15f), Eye = new Color(0.6f, 1.6f, 0.3f) }, 1.6f, 0.9f, true, false, false, false),
                "stylized painterly fantasy swamp lurker, hunched mossy humanoid with long claws and glowing green eyes, game-ready low poly, hand-painted texture, full body");

            var spit = Arch("Moor_Spucker");
            Stats(spit, "Sumpfspucker", EnemyTier.Trash, 0.65f, 2.6f, 13f, 2.4f, 0.7f, 3.8f, 15f, 1f, 55, 10f);
            spit.Behavior = NpcBehavior.Ranged; spit.OnHitDebuff = new EffectApplication(EffectType.Poison, 2, 5f); spit.ProjectileColor = new Color(0.55f, 0.95f, 0.25f); spit.Loot = trashLoot;
            spit.Prefab = Prefab("PT_Enemy_Moor_Spucker", spit, p => CreatureBuilder.Quadruped(p, "Spit", new CreatureBuilder.Look { Body = C(0.38f, 0.48f, 0.22f), Belly = C(0.75f, 0.72f, 0.45f), Accent = C(0.6f, 0.75f, 0.2f), Eye = new Color(1.5f, 1.2f, 0.2f), Scale = 1.1f }, 0.9f, 0.3f, 0.6f, 0f, 0f, false, false, true),
                "stylized painterly fantasy giant swamp toad that spits poison, warty olive skin, game-ready low poly, hand-painted texture");

            var koloss = Arch("Moor_Koloss");
            Stats(koloss, "Moorkoloss", EnemyTier.Veteran, 3.8f, 5f, 3f, 2.6f, 0.7f, 3.9f, 15f, 1.5f, 170, 30f);
            koloss.Behavior = NpcBehavior.Pull; koloss.BehaviorRadius = 5f; koloss.BehaviorInterval = 9f; koloss.BehaviorCoefficient = 6f; koloss.ProjectileColor = new Color(0.4f, 0.6f, 0.3f); koloss.Loot = vetLoot;
            koloss.Prefab = Prefab("PT_Enemy_Moor_Koloss", koloss, p => CreatureBuilder.Humanoid(p, "Koloss", new CreatureBuilder.Look { Body = C(0.32f, 0.3f, 0.22f), Belly = C(0.28f, 0.42f, 0.22f), Accent = C(0.45f, 0.55f, 0.3f), Eye = new Color(0.9f, 1.4f, 0.4f) }, 2.2f, 1.5f, true, false, false, false),
                "stylized painterly fantasy hulking bog colossus made of peat, roots and moss, game-ready low poly, hand-painted texture, full body");

            // ---- Frostkamm ----
            var wolf = Arch("Frost_Eiswolf");
            Stats(wolf, "Eiswolf", EnemyTier.Trash, 0.7f, 3.0f, 2.2f, 1.6f, 0.35f, 6.2f, 14f, 1f, 60, 10f);
            wolf.Behavior = NpcBehavior.PackHowl; wolf.BehaviorRadius = 8f; wolf.BehaviorInterval = 14f; wolf.OnHitDebuff = new EffectApplication(EffectType.Chill, 1, 2f); wolf.Loot = trashLoot;
            wolf.Prefab = Prefab("PT_Enemy_Frost_Eiswolf", wolf, p => CreatureBuilder.Quadruped(p, "Wolf", new CreatureBuilder.Look { Body = C(0.78f, 0.84f, 0.9f), Belly = C(0.92f, 0.95f, 1f), Accent = C(0.55f, 0.65f, 0.8f), Eye = new Color(0.4f, 1.2f, 1.8f) }, 1.3f, 0.55f, 0.55f, 0.35f, 0.6f, true, false, true),
                "stylized painterly fantasy ice wolf with frosty white fur and glowing blue eyes, game-ready low poly, hand-painted texture");

            var huter = Arch("Frost_Huter");
            Stats(huter, "Frosthüter", EnemyTier.Veteran, 3.6f, 4.6f, 2.6f, 2.2f, 0.6f, 4.0f, 14f, 1.35f, 170, 30f);
            huter.Behavior = NpcBehavior.FrontShield; huter.OnHitDebuff = new EffectApplication(EffectType.Chill, 1, 3f); huter.Loot = vetLoot;
            huter.Prefab = Prefab("PT_Enemy_Frost_Huter", huter, p => CreatureBuilder.Humanoid(p, "Huter", new CreatureBuilder.Look { Body = C(0.45f, 0.55f, 0.7f), Belly = C(0.7f, 0.8f, 0.9f), Accent = C(0.75f, 0.9f, 1f), Eye = new Color(0.5f, 1.3f, 2f), Glow = 0.4f }, 2f, 1.25f, false, true, false, false),
                "stylized painterly fantasy frost guardian knight with a huge ice shield, pale blue armor, game-ready low poly, hand-painted texture, full body");

            var werfer = Arch("Frost_Werfer");
            Stats(werfer, "Lawinenwerfer", EnemyTier.Trash, 0.85f, 2.8f, 12f, 2.6f, 0.7f, 4.0f, 16f, 1.15f, 60, 12f);
            werfer.Behavior = NpcBehavior.Lob; werfer.BehaviorRadius = 3.5f; werfer.BehaviorInterval = 7f; werfer.BehaviorCoefficient = 4f;
            werfer.BehaviorEffect = new EffectApplication(EffectType.Chill, 2, 3f); werfer.ProjectileColor = new Color(0.85f, 0.95f, 1f); werfer.Loot = trashLoot;
            werfer.Prefab = Prefab("PT_Enemy_Frost_Werfer", werfer, p => CreatureBuilder.Humanoid(p, "Werfer", new CreatureBuilder.Look { Body = C(0.9f, 0.92f, 0.95f), Belly = C(0.7f, 0.75f, 0.82f), Accent = C(0.5f, 0.6f, 0.75f), Eye = new Color(1.6f, 0.4f, 0.3f) }, 1.9f, 1.2f, true, false, false, false),
                "stylized painterly fantasy shaggy snow yeti throwing snowballs, white fur, game-ready low poly, hand-painted texture, full body");

            // ---- Glutöde ----
            var crawl = Arch("Glut_Kriecher");
            Stats(crawl, "Aschenkriecher", EnemyTier.Trash, 0.6f, 2.8f, 2f, 1.7f, 0.4f, 5.4f, 13f, 1.1f, 60, 10f);
            crawl.Behavior = NpcBehavior.Exploder; crawl.BehaviorRadius = 3.5f; crawl.BehaviorEffect = new EffectApplication(EffectType.Burning, 2, 4f); crawl.ProjectileColor = new Color(1f, 0.45f, 0.1f); crawl.Loot = trashLoot;
            crawl.Prefab = Prefab("PT_Enemy_Glut_Kriecher", crawl, p => CreatureBuilder.Beetle(p, "Crawl", new CreatureBuilder.Look { Body = C(0.2f, 0.17f, 0.16f), Belly = C(0.35f, 0.25f, 0.2f), Accent = C(1f, 0.45f, 0.1f), Eye = new Color(2f, 0.7f, 0.1f), Glow = 1.5f, Scale = 1.1f }),
                "stylized painterly fantasy ash beetle with glowing lava cracks on its shell, game-ready low poly, hand-painted texture");

            var caller = Arch("Glut_Beschwoerer");
            Stats(caller, "Glutbeschwörer", EnemyTier.Veteran, 3f, 3.6f, 11f, 2.2f, 0.6f, 3.8f, 16f, 1.2f, 180, 30f);
            caller.Behavior = NpcBehavior.Summoner; caller.BehaviorInterval = 12f; caller.SummonArchetype = crawl; caller.OnHitDebuff = new EffectApplication(EffectType.Burning, 1, 3f);
            caller.ProjectileColor = new Color(1f, 0.55f, 0.15f); caller.Loot = vetLoot;
            caller.Prefab = Prefab("PT_Enemy_Glut_Beschwoerer", caller, p => CreatureBuilder.Humanoid(p, "Caller", new CreatureBuilder.Look { Body = C(0.3f, 0.12f, 0.1f), Belly = C(0.45f, 0.18f, 0.12f), Accent = C(1f, 0.5f, 0.1f), Eye = new Color(2f, 0.8f, 0.1f), Glow = 1.4f }, 1.9f, 1f, false, false, true, false),
                "stylized painterly fantasy ember summoner in tattered crimson robes holding a staff with a burning orb, game-ready low poly, hand-painted texture, full body");

            var golem = Arch("Glut_Golem");
            Stats(golem, "Lavagolem", EnemyTier.Veteran, 4.4f, 5.4f, 3f, 2.8f, 0.75f, 3.6f, 15f, 1.6f, 190, 32f);
            golem.Behavior = NpcBehavior.Charger; golem.BehaviorInterval = 10f; golem.BehaviorCoefficient = 7f; golem.BehaviorRadius = 5f; golem.ProjectileColor = new Color(1f, 0.4f, 0.1f); golem.Loot = vetLoot;
            golem.Prefab = Prefab("PT_Enemy_Glut_Golem", golem, p => CreatureBuilder.Humanoid(p, "Golem", new CreatureBuilder.Look { Body = C(0.22f, 0.2f, 0.2f), Belly = C(0.3f, 0.26f, 0.24f), Accent = C(1f, 0.45f, 0.1f), Eye = new Color(2f, 0.9f, 0.2f), Glow = 2f }, 2.4f, 1.6f, false, false, false, true),
                "stylized painterly fantasy lava golem made of black basalt with glowing orange cracks, game-ready low poly, hand-painted texture, full body");

            // ---- Champions, Weltboss, Meta-Boss (M19, mit Trotzleiste) ----
            var champBag = ItemBuilder.Table("Champion");
            var cKoloss = Arch("Champion_Moorkoloss");
            Stats(cKoloss, "Uralter Moorkoloss", EnemyTier.Champion, 16f, 7f, 3.6f, 3f, 0.8f, 3.8f, 18f, 2.4f, 800, 120f);
            cKoloss.Behavior = NpcBehavior.Pull; cKoloss.BehaviorRadius = 6f; cKoloss.BehaviorInterval = 10f; cKoloss.BehaviorCoefficient = 9f; cKoloss.ProjectileColor = new Color(0.4f, 0.6f, 0.3f);
            cKoloss.HasTelegraph = true; cKoloss.TelegraphRadius = 6f; cKoloss.TelegraphDelay = 1.6f; cKoloss.TelegraphCoefficient = 18f; cKoloss.TelegraphInterval = 12f; cKoloss.Defiance = 600f; cKoloss.Loot = champBag;
            cKoloss.Prefab = koloss.Prefab;
            var cWolf = Arch("Champion_Weisszahn");
            Stats(cWolf, "Rudelführer Weißzahn", EnemyTier.Champion, 14f, 6f, 2.8f, 1.8f, 0.5f, 6.5f, 18f, 2.2f, 800, 120f);
            cWolf.Behavior = NpcBehavior.PackHowl; cWolf.BehaviorRadius = 12f; cWolf.BehaviorInterval = 12f; cWolf.OnHitDebuff = new EffectApplication(EffectType.Chill, 1, 3f);
            cWolf.HasTelegraph = true; cWolf.TelegraphRadius = 5f; cWolf.TelegraphDelay = 1.3f; cWolf.TelegraphCoefficient = 16f; cWolf.TelegraphInterval = 11f; cWolf.Defiance = 500f; cWolf.Loot = champBag;
            cWolf.Prefab = wolf.Prefab;
            var cBrut = Arch("Champion_Brutmutter");
            Stats(cBrut, "Brutmutter der Asche", EnemyTier.Champion, 16f, 6f, 3.2f, 2.6f, 0.7f, 3.4f, 18f, 3f, 800, 120f);
            cBrut.Behavior = NpcBehavior.Summoner; cBrut.BehaviorInterval = 9f; cBrut.SummonArchetype = crawl; cBrut.OnHitDebuff = new EffectApplication(EffectType.Burning, 2, 4f); cBrut.ProjectileColor = new Color(1f, 0.45f, 0.1f);
            cBrut.HasTelegraph = true; cBrut.TelegraphRadius = 6f; cBrut.TelegraphDelay = 1.5f; cBrut.TelegraphCoefficient = 18f; cBrut.TelegraphInterval = 12f; cBrut.Defiance = 600f; cBrut.Loot = champBag;
            cBrut.Prefab = crawl.Prefab;
            var wb = Arch("WorldBoss_Frostschlund");
            Stats(wb, "Frostschlund", EnemyTier.Champion, 90f, 9f, 5.5f, 3.2f, 0.9f, 3.2f, 28f, 4.5f, 3000, 400f);
            wb.FixedLevel = 30; wb.AttributeFactor = 1.25f; wb.OnHitDebuff = new EffectApplication(EffectType.Chill, 2, 3f);
            wb.HasTelegraph = true; wb.TelegraphRadius = 9f; wb.TelegraphDelay = 1.8f; wb.TelegraphCoefficient = 22f; wb.TelegraphInterval = 9f; wb.Defiance = 1500f; wb.Loot = champBag;
            wb.Behavior = NpcBehavior.Lob; wb.BehaviorRadius = 5f; wb.BehaviorInterval = 7f; wb.BehaviorCoefficient = 8f; wb.BehaviorEffect = new EffectApplication(EffectType.Chill, 3, 4f); wb.ProjectileColor = new Color(0.7f, 0.9f, 1f);
            wb.Prefab = Prefab("PT_WorldBoss_Frostschlund", wb, p => MeshyOr(p, "PT_Meshy_Frostschlund", 1.9f, CreatureBuilder.Humanoid(p, "Frostschlund", new CreatureBuilder.Look { Body = C(0.55f, 0.7f, 0.85f), Belly = C(0.85f, 0.93f, 1f), Accent = C(0.6f, 0.9f, 1f), Eye = new Color(0.6f, 1.6f, 2.2f), Glow = 1.2f }, 2.4f, 1.8f, true, false, false, true)),
                "stylized painterly fantasy colossal ice giant world boss with glacier shoulders and glowing blue cracks, game-ready low poly, hand-painted texture, full body");
            var meta = Arch("Meta_Sturmtyrann");
            Stats(meta, "Der Sturmtyrann", EnemyTier.Champion, 70f, 8.5f, 4.5f, 2.8f, 0.8f, 3.6f, 26f, 3.6f, 3000, 400f);
            meta.FixedLevel = 30; meta.AttributeFactor = 1.25f; meta.Behavior = NpcBehavior.Charger; meta.BehaviorInterval = 9f; meta.BehaviorCoefficient = 10f; meta.ProjectileColor = new Color(0.7f, 0.8f, 1f);
            meta.HasTelegraph = true; meta.TelegraphRadius = 8f; meta.TelegraphDelay = 1.6f; meta.TelegraphCoefficient = 20f; meta.TelegraphInterval = 10f; meta.Defiance = 1200f; meta.Loot = champBag;
            meta.Prefab = Prefab("PT_Meta_Sturmtyrann", meta, p => CreatureBuilder.Humanoid(p, "Sturmtyrann", new CreatureBuilder.Look { Body = C(0.25f, 0.24f, 0.3f), Belly = C(0.35f, 0.32f, 0.4f), Accent = C(0.6f, 0.75f, 1f), Eye = new Color(1.2f, 1.4f, 2.2f), Glow = 2f }, 2.4f, 1.6f, false, false, false, true),
                "stylized painterly fantasy storm titan made of dark basalt with crackling blue lightning veins, game-ready low poly, hand-painted texture, full body");

            foreach (var a in new[] { lurk, spit, koloss, wolf, huter, werfer, crawl, caller, golem, cKoloss, cWolf, cBrut, wb, meta }) EditorUtility.SetDirty(a);
            BuildCritters();
            BuildLauf6(trashLoot, vetLoot, champBag);   // Lauf 6: Gegner der sechs neuen Zonen, Story-Gegner
            AssetDatabase.SaveAssets();
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);

        /// <summary>Lauf 5: Meshy-Modell auf das Humanoid-Skelett skinnen (3 Ansätze); bei Rissen bleibt die geriggte MeshKit-Fassung, das Modell wird Statue.</summary>
        static GameObject MeshyOr(Transform parent, string model, float height, GameObject procedural)
        {
            var r = CharacterRigs.TryMeshy(parent, model, height, 1.3f);
            if (r == null) return procedural;
            Object.DestroyImmediate(procedural);
            CharacterRigs.Finish(r, "Hammer", true);
            return r.Visual;
        }

        static GameObject Prefab(string name, EnemyArchetype arch, System.Func<Transform, GameObject> visualFn, string prompt)
        {
            arch.Palette = name.Contains("_Wald_") ? VfxPalette.Zwielicht : name.Contains("_Duene_") ? VfxPalette.Staubgold : name.Contains("_Tiefe_") ? VfxPalette.Arkan : name.Contains("_Himmel_") ? VfxPalette.Himmel : name.Contains("_Feste_") || name.Contains("Vareth") ? VfxPalette.Tiefviolett : name.Contains("_Kueste_") ? VfxPalette.Wasser : name.Contains("_Moor_") ? VfxPalette.Natur : name.Contains("_Frost") ? VfxPalette.Frost : name.Contains("_Glut_") ? VfxPalette.Glut : name.Contains("Sturm") ? VfxPalette.Luft : VfxPalette.Blut;
            EditorUtility.SetDirty(arch);
            var go = new GameObject(name);
            go.layer = CharacterLayer;
            var visual = visualFn(go.transform);
            AddPlaceholder(visual, name, prompt);
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.55f; col.center = new Vector3(0, 0.9f, 0);
            var c = go.AddComponent<Combatant>();
            c.Team = Team.Enemy; c.Tier = arch.Tier; c.MaxHealth = arch.MaxHealth; c.DisplayName = arch.DisplayName;
            var brain = go.AddComponent<NpcBrain>();
            brain.Archetype = arch; brain.Visual = visual.transform; brain.WanderRadius = 5f;
            go.AddComponent<NpcAbilities>();
            go.AddComponent<HitFlash>();
            CharacterRigs.Drive(go, visual, Anim.AnimDriver.Role.Npc);
            SetLayerRecursive(go, CharacterLayer);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, NpcPrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------- Umgebungstiere ----------------
        public static GameObject Critter(string kind) => AssetDatabase.LoadAssetAtPath<GameObject>(CritterDir + "/PT_Critter_" + kind + ".prefab");

        static void BuildCritters()
        {
            CritterPrefab("Reiher", p => CreatureBuilder.Bird(p, "Heron", new CreatureBuilder.Look { Body = C(0.75f, 0.78f, 0.82f), Accent = C(0.85f, 0.7f, 0.3f), Eye = Color.black * 0f + new Color(0.1f, 0.1f, 0.1f) }), 1.0f, 3.5f, false, 8f);
            CritterPrefab("Kroete", p => CreatureBuilder.Quadruped(p, "Toad", new CreatureBuilder.Look { Body = C(0.42f, 0.5f, 0.25f), Belly = C(0.8f, 0.78f, 0.5f), Accent = C(0.3f, 0.4f, 0.2f), Eye = new Color(1.2f, 1f, 0.2f), Scale = 0.35f }, 0.9f, 0.25f, 0.55f, 0f, 0f, false, false, true), 0.8f, 4f, true, 6f);
            CritterPrefab("Schneehase", p => CreatureBuilder.Quadruped(p, "Hare", new CreatureBuilder.Look { Body = C(0.95f, 0.96f, 1f), Belly = C(0.88f, 0.88f, 0.9f), Accent = C(0.9f, 0.85f, 0.85f), Eye = new Color(0.1f, 0.1f, 0.1f), Scale = 0.4f }, 0.9f, 0.4f, 0.55f, 0.15f, 0.15f, true, false, false), 1.6f, 7f, true, 9f);
            CritterPrefab("Bergziege", p => CreatureBuilder.Quadruped(p, "Goat", new CreatureBuilder.Look { Body = C(0.85f, 0.82f, 0.75f), Belly = C(0.95f, 0.93f, 0.88f), Accent = C(0.45f, 0.38f, 0.3f), Eye = new Color(0.15f, 0.12f, 0.1f), Scale = 0.85f }, 1.1f, 0.6f, 0.6f, 0.3f, 0.15f, false, true, true), 1.2f, 5f, false, 12f);
            CritterPrefab("Aschekaefer", p => CreatureBuilder.Beetle(p, "AshBug", new CreatureBuilder.Look { Body = C(0.25f, 0.22f, 0.2f), Belly = C(0.4f, 0.3f, 0.25f), Accent = C(1f, 0.6f, 0.2f), Eye = new Color(1.5f, 0.6f, 0.1f), Glow = 0.8f, Scale = 0.35f }), 0.9f, 3f, false, 5f);
            CritterPrefab("Salamander", p => CreatureBuilder.Quadruped(p, "Sala", new CreatureBuilder.Look { Body = C(0.9f, 0.35f, 0.1f), Belly = C(1f, 0.75f, 0.3f), Accent = C(0.3f, 0.1f, 0.05f), Eye = new Color(1.8f, 1.2f, 0.2f), Glow = 0.6f, Scale = 0.45f }, 1.4f, 0.18f, 0.35f, 0.05f, 0.9f, false, false, true), 1.4f, 5f, false, 10f);
        }

        static void CritterPrefab(string kind, System.Func<Transform, GameObject> visualFn, float speed, float flee, bool hops, float wander)
        {
            var go = new GameObject("PT_Critter_" + kind);
            var visual = visualFn(go.transform);
            var cr = go.AddComponent<AmbientCritter>();
            cr.Speed = speed; cr.FleeSpeed = flee; cr.Hops = hops; cr.WanderRadius = wander; cr.Visual = visual.transform;
            CharacterRigs.Drive(go, visual, Anim.AnimDriver.Role.Critter);
            foreach (var col in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            AddPlaceholder(visual, "PT_Critter_" + kind, "stylized painterly fantasy " + kind + " ambient animal, game-ready low poly, hand-painted texture");
            PrefabUtility.SaveAsPrefabAsset(go, CritterDir + "/PT_Critter_" + kind + ".prefab");
            Object.DestroyImmediate(go);
        }
    }
}
