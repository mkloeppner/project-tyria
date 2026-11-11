using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Welt-Lauf W1: QA-Szene PT_LandmarkGallery – alle elf Baukasten-Typen, alle zehn Hürden und ein Aufstieg auf ebenem Grund (Kamera-Aufnahmen, Sichtprüfung).</summary>
    public static partial class WorldW1Builder
    {
        public const string GalleryPath = Root + "/Scenes/QA/PT_LandmarkGallery.unity";

        [MenuItem("Project Tyrant/Welt W1/Galerie-Szene bauen")]
        public static void BuildGallery()
        {
            EnsureFolder(Root + "/Scenes/QA");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneBuilder.CreateSun();
            var ground = Mat("W1_GalleryGround", new Color(0.42f, 0.5f, 0.36f), 0.3f);
            var gz = ScriptableObject.CreateInstance<ZoneDefinition>();
            gz.Id = "galerie"; gz.LevelMin = 1; gz.LevelMax = 80; gz.Seed = 77; gz.DisplayName = "Wahrzeichen-Galerie";
            _z = gz; _h = (x, z) => 0f; _defs.Clear();
            var rootGo = new GameObject("PT_Gallery");
            _root = Child(rootGo.transform, RootName, Vector3.zero).transform;
            Part(rootGo.transform, PrimitiveType.Cube, new Vector3(0, -0.5f, 300f), new Vector3(1400f, 1f, 900f), ground, "Ground", null, true);
            var kinds = (LandmarkKind[])System.Enum.GetValues(typeof(LandmarkKind));
            var colors = new[] { "#c8c0b0", "#e0c890", "#8a8f99", "#a8402c", "#6a4a2a", "#9a9488", "#d8d0c0", "#bfe4f2", "#5a7a80", "#c0b090", "#7a6e62" };
            float x0 = -600f;
            for (int i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i]; float h = k == LandmarkKind.Massif ? 150f : 90f;
                var layout = LandmarkKit.Build(k, h, i);
                ColorUtility.TryParseHtmlString(colors[i], out var prim);
                var l = new LandmarkDef { Id = "galerie_" + k, Name = TypeName(k), Kind = k, Position = new Vector3(x0 + i * 120f, 0f, 120f), Height = h, BuildHeight = h, Footprint = layout.Footprint, Yaw = 180f, Primary = prim, Accent = new Color(1f, 0.8f, 0.4f), Interior = layout.HasInterior, SoundProfile = "SFX_Amb_WindHigh_0" };
                BuildLandmark(l);
                Sign(_root, l.Position + new Vector3(0, 0, -Mathf.Max(20f, layout.Footprint * 0.6f)), TypeName(k), Mathf.RoundToInt(layout.MaxTop) + " m · Grundriss " + Mathf.RoundToInt(layout.Footprint) + " m", Color.white);
            }
            var plan = new ZoneWorldPlan();
            var hz = (HazardKind[])System.Enum.GetValues(typeof(HazardKind));
            for (int i = 0; i < hz.Length; i++)
            {
                var h = new HazardDef { Id = "galerie_" + hz[i], Kind = hz[i], Center = new Vector3(-540f + i * 120f, 0f, -60f), Size = new Vector2(40f, 40f), Circular = i % 2 == 0, Keys = HazardRules.Spec(hz[i]).Keys };
                plan.Hazards.Add(h);
                BuildHazard(h, plan);
            }
            var c = new ClimbDef { Id = "galerie_aufstieg", Name = "Galerie-Aufstieg", Base = new Vector3(0, 0, 420f), Peak = new Vector3(0, 120f, 520f), LaunchTower = new Vector3(30f, 0, 400f) };
            c.Stages = ClimbTemplate.Create(c.Base, c.Peak, 80);
            for (int k = 1; k <= 4; k++) c.SpringerLedges.Add(Vector3.Lerp(c.Base, c.Stages[1].Position, k / 4f));
            for (int k = 1; k <= 60; k++) c.ClimbPath.Add(Vector3.Lerp(c.Stages[1].Position, c.Peak, k / 60f) + new Vector3(Mathf.Sin(k * 0.4f) * 12f, 0, 0));
            BuildClimb(c, plan);
            Part(_root, PrimitiveType.Cylinder, c.Peak + Vector3.down * 60f, new Vector3(30f, 60f, 30f), Mat("W1_GalleryRock", new Color(0.5f, 0.47f, 0.43f)), "GalleryPeak", null, true);
            var cam = new GameObject("GalleryCamera").AddComponent<Camera>(); cam.transform.position = new Vector3(0, 120f, -380f); cam.transform.rotation = Quaternion.Euler(18f, 0, 0); cam.farClipPlane = 3000f;
            Object.DestroyImmediate(gz);
            _z = null;
            EditorSceneManager.SaveScene(scene, GalleryPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectTyrant] Wahrzeichen-Galerie gebaut: " + kinds.Length + " Typen, " + hz.Length + " Hürden, 1 Aufstieg → " + GalleryPath);
        }
    }
}
