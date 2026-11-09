using System.Collections.Generic;
using ProjectTyrant.Anim;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M21: QA-Szene PT_AnimGallery – alle Figurtypen nebeneinander, jede spielt ihren Clip-Satz in Schleife.</summary>
    public static class AnimGalleryBuilder
    {
        public const string ScenePath = Root + "/Scenes/QA/PT_AnimGallery.unity";

        [MenuItem("Project Tyrant/Lauf 5/QA-Szene Animationen")]
        public static void Build()
        {
            EnsureFolder(Root + "/Scenes/QA");
            if (ClipBuilder.HumanoidSet == null) ClipBuilder.BuildAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = new Vector3(8, 1, 8);
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Gallery_Ground", new Color(0.55f, 0.6f, 0.5f), 0.4f, null, 0.2f);
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube); ramp.name = "IK_Rampe";
            ramp.transform.position = new Vector3(16f, 0.4f, -5f); ramp.transform.localScale = new Vector3(3f, 0.8f, 5f); ramp.transform.rotation = Quaternion.Euler(0, 0, 12f);
            var lg = new GameObject("Sun"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.3f; l.shadows = LightShadows.Soft;
            lg.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.68f);
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 4.5f, -16f); cam.transform.rotation = Quaternion.Euler(14, 0, 0); cam.fieldOfView = 55f;
            cam.backgroundColor = new Color(0.62f, 0.72f, 0.85f); cam.clearFlags = CameraClearFlags.SolidColor;

            var human = new List<string>(ClipLibrary.HumanoidCoreClips);
            var weapon = new List<string>(); foreach (var k in ClipLibrary.WeaponClipKinds) weapon.Add("Greatsword_" + k);
            var staffClips = new List<string>(); foreach (var k in ClipLibrary.WeaponClipKinds) staffClips.Add("Staff_" + k);
            float x = -12f;
            Human("Held (Spieler)", CharacterRigs.PlayerLook(), human.ToArray(), ref x, "Greatsword");
            Human("Held – Großschwert", CharacterRigs.PlayerLook(), weapon.ToArray(), ref x, "Greatsword");
            Human("Wache", CharacterRigs.GuardLook(), new[] { "GuardIdle", "Walk", "Sword_Block", "Sword_Auto1", "Sword_Auto2", "Sword_Auto3", "Sword_Heavy", "HitHeavy" }, ref x, "Sword");
            Human("Plünderer", CharacterRigs.EnemyLook(EnemyTier.Veteran), new[] { "Idle2", "Run", "Telegraph", "Hammer_Heavy", "Hammer_Whirl", "HitLight", "Death" }, ref x, "Hammer");
            Human("Dorfbewohner", new RigBuilder.HumanLook { Cloth = new Color(0.45f, 0.6f, 0.35f), Robe = true, Beard = true }, new[] { "Work", "Talk", "Sit", "Cheer", "Interact", "Walk", "Idle2" }, ref x, "Sword");
            Human("Elementar – Stab", new RigBuilder.HumanLook { Cloth = new Color(0.5f, 0.3f, 0.6f), Hood = true, Robe = true }, staffClips.ToArray(), ref x, "Staff");
            x = -12f;
            foreach (var pr in ClipLibrary.Creatures)
            {
                bool mount = pr.Name == "Raptor" || pr.Name == "Ray" || pr.Name == "Springer";
                var clips = new List<string>(); foreach (var k in ClipLibrary.CreatureClipKinds) clips.Add(pr.Name + "_" + k);
                if (mount) foreach (var k in ClipLibrary.MountClipKinds) clips.Add(pr.Name + "_" + k);
                var go = new GameObject("Tier_" + pr.Name); go.transform.position = new Vector3(x, 0, 6f); go.transform.rotation = Quaternion.Euler(0, 200, 0);
                var look = new RigBuilder.BeastLook { Body = new Color(0.6f, 0.5f, 0.4f), Belly = new Color(0.85f, 0.8f, 0.7f), Accent = new Color(0.4f, 0.55f, 0.85f), Eye = new Color(0.1f, 0.1f, 0.1f),
                    Ears = pr.Name == "Quadruped", Snout = pr.Name != "Bird" && pr.Name != "Ray", Beak = pr.Name == "Bird", Shell = pr.Name == "Beetle", Antennae = pr.Name == "Beetle", Crest = pr.Name == "Raptor", LongEars = pr.Name == "Springer", BigHind = pr.Name == "Springer", Saddle = mount, Spots = pr.Name == "Ray" };
                var v = CharacterRigs.Beast(go.transform, "PT_Gallery_" + pr.Name, pr.Name, look, pr.Length, pr.LegHeight, Mathf.Max(0.3f, pr.LegHeight * 0.75f), pr.Neck, pr.Tail, pr.Width, 1f, mount);
                Cycler(go, v, clips.ToArray(), pr.Name);
                x += 4.5f;
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void Human(string label, RigBuilder.HumanLook look, string[] clips, ref float x, string style)
        {
            var go = new GameObject(label); go.transform.position = new Vector3(x, 0, 0); go.transform.rotation = Quaternion.Euler(0, 180, 0);
            var r = RigBuilder.Humanoid(go.transform, "PT_Gallery_" + label.Replace(" ", "").Replace("–", "_").Replace("(", "").Replace(")", ""), look);
            CharacterRigs.Finish(r, style, true);
            var w = Child(r.HandSocket, "Weapon", Vector3.zero).transform;
            Part(w, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(0.08f, style == "Staff" ? 1.6f : 1.1f, 0.03f), Mat("Gallery_Weapon", new Color(0.8f, 0.8f, 0.85f), 0.1f), "Blade");
            Cycler(go, r.Visual, clips, label);
            x += 4.8f;
        }

        static void Cycler(GameObject go, GameObject visual, string[] clips, string label)
        {
            var cy = go.AddComponent<GalleryCycler>();
            cy.Anim = visual.GetComponent<CharacterAnimator>(); cy.Clips = clips;
            var lab = new GameObject("Label"); lab.transform.SetParent(go.transform, false); lab.transform.localPosition = new Vector3(0, 2.6f, 0);
            lab.transform.rotation = Quaternion.identity;
            var t = lab.AddComponent<TextMeshPro>(); t.text = label; t.fontSize = 2.4f; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            t.rectTransform.sizeDelta = new Vector2(5, 1.2f);
            cy.Label = t;
        }

        /// <summary>QA: 8-Frame-Serie eines Clips an einer Figur als Kontaktbogen (4 × 2) nach Temp/PT_QA.</summary>
        public static string ContactSheet(string figure, string clip, string file, float camDist = 3.2f, float camHeight = 1.1f, float yawOffset = 35f)
        {
            GalleryCycler cy = null;
            foreach (var c in Object.FindObjectsByType<GalleryCycler>()) if (c.name == figure) cy = c;
            if (cy == null) return "Figur fehlt: " + figure;
            var e = cy.Anim.Set.Get(clip); if (e == null) return "Clip fehlt: " + clip;
            var cam = Camera.main;
            var target = cy.transform.position + Vector3.up * camHeight;
            var dir = Quaternion.Euler(0, cy.transform.eulerAngles.y + yawOffset, 0) * Vector3.forward;
            cam.transform.position = target + dir * camDist + Vector3.up * 0.4f; cam.transform.LookAt(target);
            const int W = 320, Hh = 360;
            var rt = new RenderTexture(W, Hh, 24); var sheet = new Texture2D(W * 4, Hh * 2, TextureFormat.RGB24, false);
            var old = cam.targetTexture; cam.targetTexture = rt;
            // Skinning im Editor ohne Player-Loop: je Frame BakeMesh in einen temporären MeshRenderer (GPU-Skinning läuft hier nicht)
            var smrs = cy.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bakes = new List<(SkinnedMeshRenderer smr, GameObject go, Mesh mesh)>();
            foreach (var smr in smrs)
            {
                var bg = new GameObject("Bake"); bg.transform.SetParent(smr.transform, false);
                var bm = new Mesh(); bg.AddComponent<MeshFilter>().sharedMesh = bm; bg.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                smr.enabled = false; bakes.Add((smr, bg, bm));
            }
            for (int i = 0; i < 8; i++)
            {
                float t = e.Length * i / 7f;
                e.Clip.SampleAnimation(cy.Anim.gameObject, t);
                foreach (var b in bakes) { b.smr.BakeMesh(b.mesh, true); b.go.transform.localScale = Vector3.one; }
                cam.Render();
                RenderTexture.active = rt;
                sheet.ReadPixels(new Rect(0, 0, W, Hh), (i % 4) * W, (1 - i / 4) * Hh);
            }
            sheet.Apply(); cam.targetTexture = old; RenderTexture.active = null;
            foreach (var b in bakes) { b.smr.enabled = true; Object.DestroyImmediate(b.go); Object.DestroyImmediate(b.mesh); }
            System.IO.Directory.CreateDirectory("Temp/PT_QA");
            System.IO.File.WriteAllBytes("Temp/PT_QA/" + file + ".png", sheet.EncodeToPNG());
            Object.DestroyImmediate(rt); Object.DestroyImmediate(sheet);
            return "Temp/PT_QA/" + file + ".png";
        }

        /// <summary>QA ohne Play-Mode: alle Figuren auf eine Clip-Zeit samplen (für Captures).</summary>
        public static void SampleAll(float normalized, int clipIndex)
        {
            foreach (var cy in Object.FindObjectsByType<GalleryCycler>())
            {
                if (cy.Anim == null || cy.Clips.Length == 0) continue;
                var e = cy.Anim.Set.Get(cy.Clips[clipIndex % cy.Clips.Length]); if (e == null) continue;
                e.Clip.SampleAnimation(cy.Anim.gameObject, normalized * e.Length);
                if (cy.Label != null) cy.Label.text = cy.name + "\n<size=70%>" + e.Name + "</size>";
            }
        }
    }
}
