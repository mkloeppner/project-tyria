using System.Collections.Generic;
using ProjectTyrant.Anim;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M21: Fabrik für alle animierten Figuren (Spieler, Wachen, Gegner, Dorfbewohner, Tiere, Reittiere).
    /// Meshy-Modelle werden zuerst auf das Skelett geskinnt (3 Ansätze, <see cref="RigBuilder.TrySkinMeshy"/>); bei Rissen ersetzt
    /// die geriggte MeshKit-Version die Figur und das Meshy-Modell wird als Statue weiterverwendet (<see cref="StatueModels"/>).
    /// </summary>
    public static class CharacterRigs
    {
        static readonly Dictionary<string, bool> _meshyOk = new Dictionary<string, bool>();
        public static readonly List<string> StatueModels = new List<string>();
        static List<ClipData> _testHuman, _testBeast;

        public static void ResetCache() { _meshyOk.Clear(); StatueModels.Clear(); RigBuilder.MeshyReport.Clear(); }

        // Persistenter Prüf-Cache (Modellgröße als Schlüssel): die Riss-Prüfung dauert je Modell ~20 s und ist deterministisch.
        public const string ReportPath = ClipBuilder.SetDir + "/MeshySkinReport.txt";
        static Dictionary<string, (long size, bool ok, string report)> _persist;
        static long FbxSize(string model) { var f = new System.IO.FileInfo(MeshyIntegrator.MeshyDir + "/" + model + ".fbx"); return f.Exists ? f.Length : -1; }
        static bool Cached(string model, out bool ok, out string report)
        {
            ok = false; report = null;
            if (_persist == null)
            {
                _persist = new Dictionary<string, (long, bool, string)>();
                if (System.IO.File.Exists(ReportPath))
                    foreach (var line in System.IO.File.ReadAllLines(ReportPath))
                    {
                        var p = line.Split('|'); if (p.Length < 4) continue;
                        _persist[p[0]] = (long.Parse(p[1]), p[2] == "1", p[3]);
                    }
            }
            if (!_persist.TryGetValue(model, out var e) || e.size != FbxSize(model)) return false;
            ok = e.ok; report = e.report; return true;
        }
        static void Remember(string model, bool ok, string report)
        {
            if (_persist == null) Cached(model, out _, out _);
            _persist[model] = (FbxSize(model), ok, report.Replace('|', '/'));
            var lines = new List<string>(); foreach (var kv in _persist) lines.Add(kv.Key + "|" + kv.Value.size + "|" + (kv.Value.ok ? "1" : "0") + "|" + kv.Value.report);
            EnsureFolder(ClipBuilder.SetDir);
            System.IO.File.WriteAllLines(ReportPath, lines);
        }
        static bool SkipKnownFailure(string model)
        {
            if (!Cached(model, out bool ok, out string rep) || ok) return false;
            if (!_meshyOk.ContainsKey(model)) { _meshyOk[model] = false; RigBuilder.MeshyReport.Add(rep + " (Prüfergebnis aus Cache)"); if (!StatueModels.Contains(model)) StatueModels.Add(model); }
            return true;
        }

        static List<ClipData> HumanTests()
        {
            if (_testHuman != null) return _testHuman;
            var all = ClipLibrary.Humanoid(); _testHuman = new List<ClipData>();
            foreach (var c in all) if (c.Name == "Walk" || c.Name == "Run" || c.Name == "Greatsword_Auto1" || c.Name == "Greatsword_Heavy" || c.Name == "DodgeF" || c.Name == "Cast2H" || c.Name == "Sit") _testHuman.Add(c);
            return _testHuman;
        }

        /// <summary>Humanoid: Meshy-Skinning versuchen, sonst MeshKit-Rig.</summary>
        public static RigBuilder.Rig Human(Transform parent, string meshName, RigBuilder.HumanLook look, string meshyModel = null)
        {
            return TryMeshy(parent, meshyModel, look.Height, look.Bulk) ?? RigBuilder.Humanoid(parent, meshName, look);
        }

        /// <summary>Nur das Meshy-Skinning (Ergebnis je Modell gecacht); null = Risse oder kein Modell.</summary>
        public static RigBuilder.Rig TryMeshy(Transform parent, string meshyModel, float height, float bulk = 1f)
        {
            var look = new RigBuilder.HumanLook { Height = height, Bulk = bulk };
            if (!string.IsNullOrEmpty(meshyModel) && SkipKnownFailure(meshyModel)) return null;
            if (!string.IsNullOrEmpty(meshyModel))
            {
                bool known = _meshyOk.TryGetValue(meshyModel, out bool ok);
                if (!known || ok)
                {
                    var r = RigBuilder.TrySkinMeshy(parent, meshyModel, look.Height, SkeletonDef.Humanoid(look.Height, look.Bulk), HumanTests(), out var report);
                    if (!known) { RigBuilder.MeshyReport.Add(report); _meshyOk[meshyModel] = r != null; if (!report.EndsWith("kein Modell")) Remember(meshyModel, r != null, report); if (r == null && report.Contains("Statue") && !StatueModels.Contains(meshyModel)) StatueModels.Add(meshyModel); }
                    if (r != null) return r;
                }
            }
            return null;
        }

        /// <summary>Animator, Clip-Satz, IK und Sockets einrichten.</summary>
        public static CharacterAnimator Finish(RigBuilder.Rig r, string style = "Greatsword", bool ik = true, string prefix = "", string[] loco = null, AnimClipSet set = null)
        {
            set = set ?? ClipBuilder.HumanoidSet;
            var ca = RigBuilder.Animate(r, set, loco, ik);
            ca.ClipPrefix = prefix; ca.WeaponStyle = style;
            return ca;
        }

        public static AnimDriver Drive(GameObject root, GameObject visual, AnimDriver.Role role)
        {
            var d = GetOrAdd<AnimDriver>(root);
            d.Mode = role; d.Anim = visual != null ? visual.GetComponentInChildren<CharacterAnimator>() : null;
            if (d.Anim != null) { d.Prefix = d.Anim.ClipPrefix; d.WeaponStyle = d.Anim.WeaponStyle; }
            return d;
        }

        /// <summary>Alle Meshy-Figuren vorab skinnen (Ergebnis-Cache + Statuenliste), damit Zonen die Statuen schon kennen.</summary>
        public static void PrepareMeshy()
        {
            ResetCache();
            var tmp = new GameObject("MeshyProbe").transform;
            foreach (var (m, h, b) in new[] { ("PT_Meshy_PlayerHero", 1.8f, 1f), ("PT_Meshy_TrashRaider", 1.85f, 1.15f), ("PT_Meshy_VeteranBrute", 1.85f, 1.15f), ("PT_Meshy_ChampionWarlord", 1.85f, 1.15f), ("PT_Meshy_Frostschlund", 1.9f, 1.3f), ("PT_Meshy_HeroWildhueter", 1.8f, 1f), ("PT_Meshy_HeroElementarweber", 1.8f, 1f), ("PT_Meshy_HeroSchattenklinge", 1.8f, 1f) })
                TryMeshy(tmp, m, h, b);
            TryMeshyBeast(tmp, "PT_Meshy_MountRaptor", 2.6f, "Raptor", true);
            TryMeshyBeast(tmp, "PT_Meshy_MountSpringer", 2.4f, "Springer", true);
            TryMeshyBeast(tmp, "PT_Meshy_MountSkimmer", 1.3f, "Ray", true);
            Object.DestroyImmediate(tmp.gameObject);
            foreach (var r in RigBuilder.MeshyReport) Debug.Log("[ProjectTyrant] Meshy-Skinning: " + r);
        }

        // ------------------------------------------------------------ Figuren
        public static RigBuilder.HumanLook PlayerLook() => new RigBuilder.HumanLook
        {
            Cloth = Palette.Player, Trim = Palette.PlayerTrim, Skin = new Color(0.96f, 0.76f, 0.62f), Leather = new Color(0.42f, 0.27f, 0.18f),
            Metal = new Color(0.78f, 0.8f, 0.86f), Hair = new Color(0.55f, 0.28f, 0.14f), Pants = new Color(0.3f, 0.24f, 0.2f), Cape = true, Pauldrons = true
        };

        public static RigBuilder.HumanLook GuardLook() { var l = PlayerLook(); l.Cloth = new Color(0.22f, 0.36f, 0.62f); l.Helmet = true; l.Shield = true; l.Cape = false; return l; }

        public static RigBuilder.HumanLook EnemyLook(EnemyTier tier)
        {
            Color skinC = tier == EnemyTier.Champion ? new Color(0.45f, 0.2f, 0.18f) : tier == EnemyTier.Veteran ? new Color(0.52f, 0.36f, 0.3f) : new Color(0.58f, 0.48f, 0.38f);
            return new RigBuilder.HumanLook
            {
                Cloth = Palette.EnemyRed, Trim = new Color(0.92f, 0.87f, 0.75f), Skin = skinC, Leather = Palette.EnemyAsh, Pants = Palette.EnemyAsh, Metal = new Color(0.92f, 0.87f, 0.75f),
                Hair = new Color(0.15f, 0.1f, 0.08f), Eye = new Color(1.6f, 0.5f, 0.1f), GlowEyes = true, Horns = true, Bulk = 1.15f, Height = 1.85f,
                Pauldrons = tier == EnemyTier.Veteran || tier == EnemyTier.Champion, Crown = tier == EnemyTier.Champion, Belly = tier == EnemyTier.Veteran
            };
        }

        static readonly Color[] VillagerRobes = { new Color(0.85f, 0.65f, 0.25f), new Color(0.45f, 0.6f, 0.35f), new Color(0.6f, 0.35f, 0.3f), new Color(0.4f, 0.45f, 0.65f), new Color(0.55f, 0.45f, 0.35f) };
        static readonly Color[] HairColors = { new Color(0.55f, 0.28f, 0.14f), new Color(0.2f, 0.14f, 0.1f), new Color(0.85f, 0.7f, 0.4f), new Color(0.6f, 0.6f, 0.6f), new Color(0.75f, 0.35f, 0.15f) };
        static readonly Color[] SkinTones = { new Color(0.96f, 0.76f, 0.62f), new Color(0.82f, 0.6f, 0.45f), new Color(0.6f, 0.42f, 0.3f), new Color(0.9f, 0.72f, 0.58f) };

        /// <summary>Dorfbewohner/Händler/Herz-NPC: Robe in Rollenfarbe, Haar/Haut nach Seed (deterministisch).</summary>
        public static GameObject Villager(Transform parent, Color robe, int seed, string meshTag, string startClip = "", bool apron = false)
        {
            var rnd = new System.Random(seed);
            var l = new RigBuilder.HumanLook
            {
                Cloth = robe, Trim = robe * 0.7f + Color.white * 0.25f, Pants = new Color(0.32f, 0.26f, 0.2f), Leather = new Color(0.38f, 0.26f, 0.18f),
                Hair = HairColors[rnd.Next(HairColors.Length)], Skin = SkinTones[rnd.Next(SkinTones.Length)], Robe = rnd.Next(3) == 0 || apron, Beard = rnd.Next(3) == 0,
                Hood = rnd.Next(5) == 0, Height = 1.7f + (float)rnd.NextDouble() * 0.2f, Bulk = 0.95f + (float)rnd.NextDouble() * 0.15f, Belly = rnd.Next(4) == 0
            };
            var r = RigBuilder.Humanoid(parent, "PT_Villager_" + meshTag, l);
            var ca = Finish(r, "Sword", true);
            ca.StartClip = startClip;
            return r.Visual;
        }

        /// <summary>Stehender NPC (Herz, Händler, Stallmeister): geriggter Dorfbewohner + Kollider + Treiber (Umschauen, Blick zum Spieler).</summary>
        public static GameObject Npc(Transform npc, Color robe, string id, bool apron = false)
        {
            var v = Villager(npc, robe, id.GetHashCode(), id, "", apron);
            var col = GetOrAdd<CapsuleCollider>(npc.gameObject);
            col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 0.35f;
            var d = Drive(npc.gameObject, v, AnimDriver.Role.Villager);
            d.LookAtPlayer = true;
            return v;
        }

        /// <summary>Verletzter Wächter (Herz-Aufgabe „Verbinden“): liegt in Niedergeschlagen-Pose.</summary>
        public static GameObject Injured(Transform parent, string id)
        {
            var r = RigBuilder.Humanoid(parent, "PT_Injured", GuardLook());
            var ca = Finish(r, "Sword", false);
            ca.StartClip = "Knockdown";
            r.Visual.transform.localRotation = Quaternion.Euler(0, (id.GetHashCode() & 255) * 1.4f, 0);
            return r.Visual;
        }

        // ------------------------------------------------------------ Tiere
        public static CreatureProfile Profile(string name) { foreach (var p in ClipLibrary.Creatures) if (p.Name == name) return p; return ClipLibrary.Creatures[0]; }

        public static GameObject Beast(Transform parent, string meshName, string profile, RigBuilder.BeastLook look, float length, float legHeight, float bodyH, float neck, float tail, float width, float scale, bool mount = false)
        {
            var pr = Profile(profile);
            var r = RigBuilder.Beast(parent, meshName, pr, look, length, legHeight, bodyH, neck, tail, width);
            var ca = RigBuilder.Animate(r, ClipBuilder.CreatureSet(profile), RigBuilder.BeastLoco(profile, mount), false, false);
            ca.ClipPrefix = profile + "_";
            r.Visual.transform.localScale = Vector3.one * scale;
            ca.WalkSpeed = 1.6f * scale * Mathf.Max(0.5f, legHeight / 0.7f); ca.RunSpeed = 6f * scale * Mathf.Max(0.5f, legHeight / 0.7f); ca.SprintSpeed = ca.RunSpeed * 1.6f;
            if (mount) { ca.WalkSpeed = 3f; ca.RunSpeed = 11f; ca.SprintSpeed = 14f; }
            return r.Visual;
        }

        /// <summary>Reittier-/Tier-Meshy-Modell auf das Vierbeiner-Skelett des Profils skinnen (gecacht); null = Risse.</summary>
        public static GameObject TryMeshyBeast(Transform parent, string model, float height, string profile, bool mount)
        {
            if (_meshyOk.TryGetValue(model, out bool ok) && !ok) return null;
            if (SkipKnownFailure(model)) return null;
            var pr = Profile(profile);
            var r = RigBuilder.TrySkinMeshy(parent, model, height, pr.Skeleton(), ClipLibrary.Creature(pr, mount), out var report);
            if (!_meshyOk.ContainsKey(model)) { RigBuilder.MeshyReport.Add(report); _meshyOk[model] = r != null; if (!report.EndsWith("kein Modell")) Remember(model, r != null, report); if (r == null && report.Contains("Statue") && !StatueModels.Contains(model)) StatueModels.Add(model); }
            if (r == null) return null;
            var ca = RigBuilder.Animate(r, ClipBuilder.CreatureSet(profile), RigBuilder.BeastLoco(profile, mount), false, false);
            ca.ClipPrefix = profile + "_";
            return r.Visual;
        }

        /// <summary>Waffen-Trail (TrailRenderer, additiver HLSL-Shader) an der Klingenspitze; emittiert nur während Schlägen (SkillFxDirector).</summary>
        public static TrailRenderer AddTrail(GameObject weapon, float tipY, Color color)
        {
            var tip = new GameObject("TrailTip"); tip.transform.SetParent(weapon.transform, false); tip.transform.localPosition = new Vector3(0, tipY, 0);
            var tr = tip.AddComponent<TrailRenderer>();
            tr.sharedMaterial = FxMat("Trail_" + ColorUtility.ToHtmlStringRGB(color), "ProjectTyrant/FX/AdditiveTrail", color);
            tr.time = 0.16f; tr.minVertexDistance = 0.05f; tr.emitting = false; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.32f), new Keyframe(1, 0.02f));
            tr.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(color, 1) }, alphaKeys = new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0f, 1) } };
            return tr;
        }

        // ------------------------------------------------------------ Statuen (Meshy-Ersatz)
        /// <summary>Meshy-Modelle, die nicht rissfrei skinnbar waren, als Statuen auf Sockeln (Heldenhain).</summary>
        public static void PlaceStatues(Transform parent, Vector3 origin, float yaw)
        {
            var root = Child(parent, "Heldenhain_Statuen", origin).transform;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            var stone = Mat("Statue_Plinth", new Color(0.62f, 0.6f, 0.56f), 0.4f, null, 0.3f);
            var list = new List<string>(StatueModels);
            if (list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                var spot = Child(root, "Statue_" + list[i], new Vector3((i - (list.Count - 1) * 0.5f) * 3.2f, 0, 0)).transform;
                Part(spot, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(1.8f, 0.8f, 1.8f), stone, "Plinth", null, true);
                bool mount = list[i].StartsWith("PT_Meshy_Mount");
                var pre = MeshyIntegrator.GetOrBuild(list[i], mount ? 2.2f : 2.2f);
                if (pre == null) continue;
                var m = (GameObject)PrefabUtility.InstantiatePrefab(pre);
                m.transform.SetParent(spot, false); m.transform.localPosition = new Vector3(0, 0.8f, 0);
                if (list[i] == "PT_Meshy_MountSpringer") m.transform.localRotation = Quaternion.Euler(0, -90, 0);
                AddPlaceholder(spot.gameObject, "Statue " + list[i], "Meshy-Modell als Statue (Skinning mit Rissen, siehe DECISIONS M21)");
            }
        }
    }
}
