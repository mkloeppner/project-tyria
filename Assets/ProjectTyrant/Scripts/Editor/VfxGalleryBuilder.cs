using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M23: QA-Szene PT_VfxGallery – alle 13 Formen × 11 Paletten (Größe M) im Raster, jede Komposition spielt in Schleife.</summary>
    public static class VfxGalleryBuilder
    {
        public const string ScenePath = Root + "/Scenes/QA/PT_VfxGallery.unity";
        public const float Spacing = 6f;

        [MenuItem("Project Tyrant/Lauf 5/QA-Szene VFX")]
        public static void Build()
        {
            EnsureFolder(Root + "/Scenes/QA");
            var cat = VfxKitBuilder.Catalog;
            if (cat == null) { VfxKitBuilder.BuildAll(); cat = VfxKitBuilder.Catalog; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 8); ground.transform.position = new Vector3(36f, 0, 30f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Gallery_GroundDark", new Color(0.18f, 0.19f, 0.22f), 0.3f, null, 0.1f);
            var lg = new GameObject("Sun"); var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 0.6f; lg.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.25f, 0.27f, 0.33f);
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f); cam.fieldOfView = 50f;
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.requiresDepthTexture = true; data.requiresColorTexture = true;
            camGo.transform.position = new Vector3(36f, 9f, -12f); camGo.transform.rotation = Quaternion.Euler(22, 0, 0);
            var pool = new GameObject("VfxPool").AddComponent<VfxPool>(); pool.Catalog = cat;
            var shapes = (VfxShape[])System.Enum.GetValues(typeof(VfxShape));
            int row = 0;
            foreach (var pal in cat.Palettes)
            {
                var rowGo = new GameObject("Palette_" + pal.Id); rowGo.transform.position = new Vector3(0, 0, row * Spacing);
                Label(rowGo.transform, new Vector3(-4f, 0.2f, 0), pal.Id.ToString(), 4f, pal.A);
                for (int i = 0; i < shapes.Length; i++)
                {
                    var e = cat.Get(VfxIds.Compose(shapes[i], pal.Id, VfxSize.M)); if (e == null) continue;
                    var spot = new GameObject(shapes[i].ToString()); spot.transform.SetParent(rowGo.transform, false);
                    spot.transform.localPosition = new Vector3(i * Spacing, shapes[i] == VfxShape.Charge || shapes[i] == VfxShape.Projectile || shapes[i] == VfxShape.Release || shapes[i] == VfxShape.Impact || shapes[i] == VfxShape.Trail || shapes[i] == VfxShape.Cone || shapes[i] == VfxShape.Beam ? 1.2f : 0f, 0);
                    spot.transform.localRotation = shapes[i] == VfxShape.Beam || shapes[i] == VfxShape.Cone || shapes[i] == VfxShape.Release ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(e.Prefab, spot.transform);
                    var lp = spot.AddComponent<VfxGalleryLooper>(); lp.Instance = go.GetComponent<VfxInstance>(); lp.Period = Mathf.Max(1.5f, e.Duration + 0.8f); lp.Offset = (i % 4) * 0.2f;
                    lp.Orbit = shapes[i] == VfxShape.Trail || shapes[i] == VfxShape.Projectile ? 1.6f : 0f;
                    if (row == 0) Label(spot.transform, new Vector3(0, 0.2f, -2.6f), shapes[i].ToString(), 2.2f, Color.white);
                }
                row++;
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void Label(Transform parent, Vector3 pos, string text, float size, Color c)
        {
            var go = new GameObject("Label_" + text); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.rotation = Quaternion.Euler(60, 0, 0);
            var t = go.AddComponent<TextMeshPro>(); t.text = text; t.fontSize = size; t.color = c; t.alignment = TextAlignmentOptions.Center; t.rectTransform.sizeDelta = new Vector2(8, 2);
        }
    }
}
