using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    public static partial class SceneBuilder
    {
        static partial void AddLateSystemsImpl(GameObject go)
        {
            var ui = go.AddComponent<UIController>();
            ui.Icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(IconPainter.LibraryPath);
            ui.MapTexture = MapBaker.Load();   // Fallback; die Zone setzt ihre Karte beim Laden (BindZoneMap)
            go.AddComponent<FeedbackSystem>();
            go.AddComponent<CombatFeel>();
            go.AddComponent<VfxPool>().Catalog = VfxKitBuilder.Catalog;   // Lauf 5 / M23: VFX-Pool hängt am GameManager-Objekt
            go.AddComponent<WorldFx>().GhostMaterial = VfxKitBuilder.GhostMat();
            go.AddComponent<AudioPool>().Bank = AudioBuilder.Bank;   // Lauf 5 / M24: Audio-Pool, Musik- und Umgebungs-Direktor am GameManager-Objekt
            go.AddComponent<MusicDirector>();
            go.AddComponent<AmbienceDirector>();
            go.AddComponent<LootSystem>();
            var gm = go.GetComponent<GameManager>();
            gm.Vfx = AssetDatabase.LoadAssetAtPath<VfxLibrary>(PTEditorUtil.DataDir + "/VfxLibrary.asset");
            gm.Effects = DataBuilder.LoadEffects();
            var profile = go.AddComponent<PlayerProfile>();
            ConfigureProfileImpl(profile);
            AddEventSystemsImpl(go);
        }

        static partial void AddEventSystemsImpl(GameObject go);
        static partial void ConfigureProfileImpl(PlayerProfile profile);

        static partial void ConfigurePlayerImpl(GameObject player)
        {
            var caster = player.AddComponent<SkillCaster>();
            DataBuilder.ConfigurePlayerSkills(caster);
            player.AddComponent<HitFlash>();
            player.AddComponent<TargetOutline>().OutlineMaterial = PTEditorUtil.FxMat("Outline", "ProjectTyrant/FX/Outline", Color.white);
            player.AddComponent<OutOfCombatRegen>();
            player.AddComponent<Balance.NpcRescue>();   // Balance B1: NPC-Wiederbelebung in Stufe 0–1
            player.GetComponent<DownedController>().DownedHealthShare = 0.5f;
            player.AddComponent<MountController>();
            player.AddComponent<MasteryAbilities>();
            // Lauf 5 / M25: Klassenmechanik + Tarnung
            player.AddComponent<StealthController>();
            var host = player.AddComponent<ClassMechanicHost>();
            ClassBuilder.ConfigureHost(host);
        }
    }

    public static partial class ZoneBuilder
    {
        static Transform _npcRoot;

        static GameObject SpawnNpc(EnemyArchetype a, Vector3 pos, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(a.Prefab, _npcRoot);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.GetComponent<NpcBrain>().Archetype = a;
            return go;
        }

        static partial void PopulateGameplayImpl(ZoneRoot root, ZoneAnchors anchors)
        {
            _npcRoot = new GameObject("NPCs").transform;
            var trash = DataBuilder.Archetype("Trash_Pluenderer");
            var vet = DataBuilder.Archetype("Veteran_Brecher");
            // Wildnis: kleine Gruppen
            for (int i = 0; i < anchors.WildSpawns.Length; i++)
            {
                var p = anchors.WildSpawns[i].position;
                SpawnNpc(trash, p, i * 40f);
                if (i % 2 == 0) SpawnNpc(trash, p + new Vector3(2.5f, 0, 1.5f), i * 70f);
            }
            // Ruinen: Veteran-Wache
            SpawnNpc(vet, anchors.RuinsSpawns[0].position, 0f);
            SpawnNpc(trash, anchors.RuinsSpawns[1].position, 90f);
            SpawnNpc(trash, anchors.RuinsSpawns[2].position, 180f);
            // Lager-Garnison
            for (int i = 0; i < 4; i++) SpawnNpc(trash, anchors.CampSpawns[i * 2].position, i * 90f);
            ItemBuilder.PlaceVendors(_npcRoot);
            MountBuilder.PlaceStallmeister(_npcRoot, MountKind.Raptor, G(ZoneLayout.Hub.Center + new Vector2(-6f, 11f)), 150f, "Stallmeisterin Hedda");
            MapMarkerBuilder.PlaceMarkers(anchors, _npcRoot);
            PopulateEventImpl(root, anchors, _npcRoot);
        }

        static partial void PopulateEventImpl(ZoneRoot root, ZoneAnchors anchors, Transform npcRoot);
    }
}
