using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 11: neue Gegnerfraktionen der drei Regionen (Kernland 9, Glutwüste 4, Dracheninseln 4), je Zone ein Champion für die Ereigniskette,
    /// vier neue Weltbosse, vier Meta-Bosse und sechs Ambient-Tiere.
    /// </summary>
    public static partial class WorldEnemyBuilder
    {
        /// <summary>Vorhandenen Archetyp nur lesen (<see cref="Arch"/> setzt Felder zurück).</summary>
        static EnemyArchetype Find(string id) => UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>(EnemyDir + "/Enemy_" + id + ".asset");

        static CreatureBuilder.Look L(Color body, Color belly, Color accent, Color eye, float glow = 0f, float scale = 1f) => new CreatureBuilder.Look { Body = body, Belly = belly, Accent = accent, Eye = eye, Glow = glow, Scale = scale };
        static GameObject Hum(Transform p, string n, CreatureBuilder.Look l, float h = 1.9f, float w = 1f, bool lanky = false, bool shield = false, bool staff = false) => CreatureBuilder.Humanoid(p, n, l, h, w, lanky, shield, staff, false);
        static GameObject Beast(Transform p, string n, CreatureBuilder.Look l, float len = 1.4f) => CreatureBuilder.Quadruped(p, n, l, len, 0.5f, 0.7f, 0.2f, 0.2f, true, true, true);

        static void BuildLauf11(LootTable trashLoot, LootTable vetLoot, LootTable champBag)
        {
            string pr(string what) => "stylized painterly fantasy " + what + ", game-ready low poly, hand-painted texture, full body";
            // ---- Kernland ----
            var nomad = T("Kern_Nomade", "Steppennomade", NpcBehavior.Ranged, 0.8f, 2.8f, 12f, 2f, 0.7f, 5.4f, 1f, trashLoot, p => Hum(p, "Nomad", L(C(0.6f, 0.45f, 0.3f), C(0.8f, 0.7f, 0.5f), C(0.8f, 0.3f, 0.2f), new Color(0.2f, 0.2f, 0.2f))), pr("steppe nomad archer with fur cloak"));
            nomad.ProjectileColor = new Color(0.9f, 0.8f, 0.5f);
            var bull = T("Kern_Steppenbulle", "Steppenbulle", NpcBehavior.Charger, 1.2f, 3.4f, 2.6f, 2.1f, 0.6f, 5.8f, 1.3f, trashLoot, p => Beast(p, "Bull", L(C(0.45f, 0.32f, 0.22f), C(0.6f, 0.5f, 0.4f), C(0.9f, 0.85f, 0.7f), new Color(0.2f, 0.1f, 0.1f)), 1.7f), pr("shaggy steppe bull with long horns"));
            bull.BehaviorInterval = 9f; bull.BehaviorCoefficient = 5f;
            var spirit = T("Kern_Steingeist", "Steinkreisgeist", NpcBehavior.Ambush, 0.9f, 3f, 2.3f, 1.8f, 0.6f, 4.8f, 1.1f, trashLoot, p => Hum(p, "StoneSpirit", L(C(0.5f, 0.55f, 0.6f), C(0.6f, 0.65f, 0.7f), C(0.5f, 0.9f, 1f), new Color(0.6f, 1.4f, 2f), 1f), 2f, 1f, true), pr("ghostly stone circle spirit with glowing runes"));
            spirit.BehaviorRadius = 6f;
            var pirate = T("Kern_Pirat", "Buchtpirat", NpcBehavior.None, 0.85f, 3.1f, 2.3f, 1.7f, 0.6f, 5f, 1f, trashLoot, p => Hum(p, "Pirate", L(C(0.3f, 0.25f, 0.3f), C(0.7f, 0.6f, 0.45f), C(0.8f, 0.15f, 0.15f), new Color(0.2f, 0.2f, 0.2f))), pr("cove pirate with tricorn hat and cutlass"), EnemyTier.Trash, new EffectApplication(EffectType.Bleeding, 1, 4f));
            var magma = T("Kern_Magmabrut", "Magmabrut", NpcBehavior.Exploder, 0.7f, 3.4f, 2f, 2f, 0.7f, 5.2f, 0.9f, trashLoot, p => CreatureBuilder.Beetle(p, "Magma", L(C(0.25f, 0.15f, 0.1f), C(0.5f, 0.2f, 0.1f), C(1f, 0.5f, 0.1f), new Color(2f, 0.8f, 0.2f), 1.4f, 0.8f)), pr("magma brood beetle with glowing lava cracks"), EnemyTier.Trash, new EffectApplication(EffectType.Burning, 1, 3f));
            var stalker = T("Kern_Dschungelpirscher", "Dschungelpirscher", NpcBehavior.Ambush, 0.85f, 3.3f, 2.3f, 1.6f, 0.55f, 6f, 1.1f, trashLoot, p => Beast(p, "Stalker", L(C(0.2f, 0.35f, 0.15f), C(0.6f, 0.6f, 0.3f), C(0.9f, 0.7f, 0.1f), new Color(1.6f, 1.4f, 0.2f)), 1.5f), pr("jungle stalker big cat with leaf camouflage"));
            stalker.BehaviorRadius = 7f;
            var mangrove = T("Kern_Mangrovenschreck", "Mangrovenschreck", NpcBehavior.Pull, 1f, 3f, 9f, 2.2f, 0.7f, 4.4f, 1.2f, trashLoot, p => Hum(p, "Mangrove", L(C(0.25f, 0.3f, 0.2f), C(0.35f, 0.4f, 0.25f), C(0.4f, 0.7f, 0.5f), new Color(0.8f, 1.6f, 0.8f)), 2.1f, 1.1f, true), pr("mangrove horror of roots and mud"));
            var giant = T("Kern_Frostriese", "Frostriese", NpcBehavior.FrontShield, 3.6f, 4.6f, 2.8f, 2.4f, 0.75f, 3.8f, 1.6f, vetLoot, p => Hum(p, "FrostGiant", L(C(0.7f, 0.8f, 0.9f), C(0.85f, 0.9f, 0.95f), C(0.4f, 0.6f, 1f), new Color(0.6f, 1.2f, 2f), 0.6f), 2.4f, 1.4f, false, true), pr("frost giant with ice armor and shield"), EnemyTier.Veteran, new EffectApplication(EffectType.Chill, 2, 3f));
            var undead = T("Kern_Untoter", "Ertrunkener", NpcBehavior.None, 0.95f, 3.2f, 2.3f, 1.8f, 0.6f, 4.6f, 1f, trashLoot, p => Hum(p, "Drowned", L(C(0.3f, 0.35f, 0.35f), C(0.4f, 0.45f, 0.42f), C(0.5f, 0.3f, 0.7f), new Color(1.2f, 0.5f, 1.8f), 0.6f)), pr("drowned undead sailor covered in seaweed"), EnemyTier.Trash, new EffectApplication(EffectType.Poison, 1, 4f));

            // ---- Glutwüste ----
            var sandg = T("Wueste_Sandgeist", "Sandgeist", NpcBehavior.Ambush, 0.85f, 3.2f, 2.3f, 1.6f, 0.55f, 5.6f, 1.1f, trashLoot, p => Hum(p, "SandSpirit", L(C(0.85f, 0.7f, 0.45f), C(0.9f, 0.8f, 0.6f), C(1f, 0.85f, 0.4f), new Color(2f, 1.6f, 0.6f), 0.8f), 2f, 0.9f, true), pr("swirling sand spirit with glowing eyes"), EnemyTier.Trash, new EffectApplication(EffectType.Blind, 1, 2f));
            sandg.BehaviorRadius = 7f;
            var cultist = T("Wueste_Flammenkultist", "Flammenkultist", NpcBehavior.Ranged, 0.8f, 3f, 12f, 2.1f, 0.7f, 5f, 1f, trashLoot, p => Hum(p, "Cultist", L(C(0.5f, 0.15f, 0.1f), C(0.7f, 0.3f, 0.15f), C(1f, 0.6f, 0.1f), new Color(2f, 0.8f, 0.2f), 0.6f), 1.9f, 1f, false, false, true), pr("flame cult zealot in red robes with fire staff"), EnemyTier.Trash, new EffectApplication(EffectType.Burning, 1, 3f));
            cultist.ProjectileColor = new Color(1f, 0.5f, 0.1f);
            var fpriest = T("Wueste_Flammenpriester", "Flammenpriester", NpcBehavior.Summoner, 3.2f, 3.6f, 11f, 2.3f, 0.8f, 3.6f, 1.25f, vetLoot, p => Hum(p, "FlamePriest", L(C(0.35f, 0.08f, 0.05f), C(0.6f, 0.2f, 0.1f), C(1f, 0.75f, 0.2f), new Color(2.2f, 1f, 0.2f), 1.2f), 2f, 1.05f, false, false, true), pr("flame high priest with golden mask and burning staff"), EnemyTier.Veteran, new EffectApplication(EffectType.Burning, 2, 3f));
            fpriest.SummonArchetype = cultist; fpriest.BehaviorInterval = 13f; fpriest.ProjectileColor = new Color(1f, 0.6f, 0.2f); fpriest.Defiance = 400f;
            var bone = T("Wueste_Knochenkonstrukt", "Knochenkonstrukt", NpcBehavior.FrontShield, 3.8f, 4.6f, 2.8f, 2.4f, 0.75f, 3.6f, 1.6f, vetLoot, p => Hum(p, "BoneConstruct", L(C(0.85f, 0.8f, 0.7f), C(0.75f, 0.7f, 0.6f), C(1f, 0.5f, 0.2f), new Color(2f, 0.8f, 0.3f), 0.8f), 2.5f, 1.4f, false, true), pr("giant bone construct with sand-filled ribcage"), EnemyTier.Veteran);

            // ---- Dracheninseln ----
            var jpirate = T("Jade_Pirat", "Jade-Pirat", NpcBehavior.None, 0.85f, 3.1f, 2.3f, 1.7f, 0.6f, 5.2f, 1f, trashLoot, p => Hum(p, "JadePirate", L(C(0.2f, 0.3f, 0.3f), C(0.6f, 0.55f, 0.4f), C(0.3f, 0.9f, 0.6f), new Color(0.2f, 0.2f, 0.2f))), pr("jade sea pirate with curved blade and jade earrings"), EnemyTier.Trash, new EffectApplication(EffectType.Bleeding, 1, 4f));
            var construct = T("Jade_Konstrukt", "Jade-Konstrukt", NpcBehavior.FrontShield, 3.6f, 4.4f, 2.6f, 2.3f, 0.75f, 3.8f, 1.4f, vetLoot, p => Hum(p, "JadeConstruct", L(C(0.3f, 0.6f, 0.5f), C(0.4f, 0.7f, 0.6f), C(0.6f, 1f, 0.8f), new Color(0.6f, 2f, 1.4f), 1.2f), 2.3f, 1.3f, false, true), pr("jade mechanical construct with glowing green core"), EnemyTier.Veteran);
            construct.Defiance = 450f;
            var ghost = T("Jade_Geisterwesen", "Geisterwesen", NpcBehavior.Ambush, 0.75f, 3.2f, 2.2f, 1.6f, 0.55f, 5.4f, 1.05f, trashLoot, p => Hum(p, "Ghost", L(C(0.6f, 0.75f, 0.8f), C(0.7f, 0.85f, 0.9f), C(0.6f, 1f, 1f), new Color(1f, 2f, 2f), 1.4f), 2f, 0.9f, true), pr("translucent forest ghost in flowing robes"), EnemyTier.Trash, new EffectApplication(EffectType.Fear, 1, 1f));
            ghost.BehaviorRadius = 7f;
            var rebel = T("Jade_Abtruenniger", "Kaiserlicher Abtrünniger", NpcBehavior.Ranged, 0.85f, 3f, 13f, 2f, 0.65f, 5.4f, 1f, trashLoot, p => Hum(p, "Rebel", L(C(0.55f, 0.15f, 0.15f), C(0.7f, 0.6f, 0.4f), C(0.9f, 0.75f, 0.2f), new Color(0.2f, 0.2f, 0.2f))), pr("renegade imperial crossbowman in lacquered armor"));
            rebel.ProjectileColor = new Color(0.9f, 0.9f, 0.6f);

            // ---- Champions je Zone (Ereigniskette) ----
            foreach (var s in RegionZones.All)
            {
                var baseA = Find(s.Archetypes[0]);
                if (baseA == null || baseA.Prefab == null) baseA = Find(s.Archetypes[1]);
                if (baseA == null) continue;
                Champ("Champion_" + s.Id, "Anführer: " + s.Enemy, baseA, 14f, 600f, champBag, NpcBehavior.Summoner, 1.6f);
            }
            // ---- Weltbosse (Fahrplan) und Meta-Bosse ----
            var gw = Champ("Champion_Gletscherwaechter", "Gletscherwächter", giant, 60f, 1500f, champBag, NpcBehavior.Pull, 3.6f); gw.FixedLevel = 75; gw.OnHitDebuff = new EffectApplication(EffectType.Chill, 2, 3f);
            var tw = Champ("Champion_Tempelwaechter", "Tempelwächter", undead, 60f, 1500f, champBag, NpcBehavior.Summoner, 3.8f); tw.FixedLevel = 80;
            var sk = Champ("Champion_Salzkoloss", "Salzkoloss", bone, 70f, 1800f, champBag, NpcBehavior.Charger, 4.2f); sk.FixedLevel = 80;
            var ns = Champ("Champion_Nebelschlange", "Nebelschlange", Find("Himmel_Wolkenschlange") ?? ghost, 65f, 1600f, champBag, NpcBehavior.Pull, 4f); ns.FixedLevel = 80;
            var vh = Champ("Champion_Vulkanherz", "Herz des Vulkans", Find("Glut_Golem") ?? magma, 55f, 1400f, champBag, NpcBehavior.Summoner, 3.6f); vh.FixedLevel = 60; vh.SummonArchetype = magma;
            var hp = Champ("Champion_Hohepriester", "Hohepriester der Flamme", fpriest, 55f, 1400f, champBag, NpcBehavior.Summoner, 2.4f); hp.FixedLevel = 80;
            var fk = Champ("Champion_Flammenkoenig", "Zarath, der Flammenkönig", fpriest, 90f, 2400f, champBag, NpcBehavior.Pull, 3f); fk.FixedLevel = 80; fk.OnHitDebuff = new EffectApplication(EffectType.Burning, 3, 4f);
            var md = Champ("Champion_Meeresdrache", "Der Meeresdrache", Find("Himmel_Wolkenschlange") ?? construct, 100f, 2600f, champBag, NpcBehavior.Pull, 5f); md.FixedLevel = 80;
            foreach (var x in new[] { gw, tw, sk, ns, vh, hp, fk, md }) { x.TelegraphRadius = 8f; x.TelegraphCoefficient = 22f; x.TelegraphInterval = 10f; UnityEditor.EditorUtility.SetDirty(x); }

            // ---- Ambient-Tiere ----
            CritterPrefab("Steppenbison", p => Beast(p, "Bison", L(C(0.4f, 0.3f, 0.2f), C(0.55f, 0.45f, 0.35f), C(0.3f, 0.2f, 0.15f), new Color(0.1f, 0.1f, 0.1f), 0f, 0.9f), 1.5f), 0.9f, 3.5f, false, 8f);
            CritterPrefab("Moorfuchs", p => CreatureBuilder.Quadruped(p, "Fox", L(C(0.8f, 0.45f, 0.2f), C(0.95f, 0.9f, 0.85f), C(0.3f, 0.2f, 0.15f), new Color(0.1f, 0.1f, 0.1f), 0f, 0.45f), 1.1f, 0.35f, 0.5f, 0.15f, 0.25f, false, false, false), 1.2f, 4f, false, 7f);
            CritterPrefab("Wuestenspringmaus", p => CreatureBuilder.Quadruped(p, "Jerboa", L(C(0.85f, 0.7f, 0.5f), C(0.95f, 0.9f, 0.8f), C(0.6f, 0.45f, 0.3f), new Color(0.1f, 0.1f, 0.1f), 0f, 0.25f), 0.8f, 0.3f, 0.5f, 0.1f, 0.3f, true, false, false), 1.4f, 4.5f, true, 6f);
            CritterPrefab("Sandechse", p => CreatureBuilder.Quadruped(p, "Lizard", L(C(0.75f, 0.6f, 0.35f), C(0.9f, 0.8f, 0.55f), C(0.5f, 0.35f, 0.2f), new Color(0.2f, 0.2f, 0.1f), 0f, 0.4f), 1.4f, 0.18f, 0.35f, 0.05f, 0.9f, false, false, false), 1.3f, 4f, false, 6f);
            CritterPrefab("Jadekranich", p => CreatureBuilder.Bird(p, "Crane", L(C(0.9f, 0.95f, 0.92f), C(0.95f, 0.98f, 0.95f), C(0.3f, 0.8f, 0.6f), new Color(0.1f, 0.1f, 0.1f))), 1.0f, 3.5f, false, 8f);
            CritterPrefab("Bambusmarder", p => CreatureBuilder.Quadruped(p, "Marten", L(C(0.35f, 0.25f, 0.2f), C(0.9f, 0.85f, 0.75f), C(0.2f, 0.15f, 0.1f), new Color(0.1f, 0.1f, 0.1f), 0f, 0.35f), 1.2f, 0.25f, 0.45f, 0.12f, 0.4f, false, false, false), 1.3f, 4.5f, false, 7f);
        }
    }
}
