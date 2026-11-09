using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M25: Szene PT_CharacterSelect – Biom-Kulisse (Sonnfurt-Wiese im Abendlicht), 5 Figuren im Halbkreis, Kamera, Steuerung.</summary>
    public static class CharacterSelectBuilder
    {
        public const string ScenePath = Root + "/Scenes/PT_CharacterSelect.unity";

        [MenuItem("Project Tyrant/Lauf 5/Charakterauswahl bauen")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.75f); RenderSettings.ambientEquatorColor = new Color(0.55f, 0.48f, 0.4f); RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.15f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.75f, 0.62f, 0.5f); RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.015f;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.82f, 0.62f); sun.intensity = 1.3f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f); RenderSettings.sun = sun;

            // Kulisse: Wiese, Felsen, Bäume, Steinkreis
            var env = new GameObject("Kulisse").transform;
            var ground = Part(env, PrimitiveType.Cylinder, new Vector3(0, -0.05f, 4f), new Vector3(60f, 0.05f, 60f), Mat("CS_Wiese", new Color(0.42f, 0.55f, 0.28f)), "Wiese");
            Part(env, PrimitiveType.Cylinder, new Vector3(0, 0f, 0f), new Vector3(9f, 0.06f, 9f), Mat("CS_Platz", new Color(0.62f, 0.58f, 0.5f)), "Steinplatz");
            var rock = Mat("CS_Fels", new Color(0.55f, 0.52f, 0.5f)); var bark = Mat("CS_Rinde", new Color(0.35f, 0.24f, 0.16f)); var leaf = Mat("CS_Laub", new Color(0.35f, 0.55f, 0.25f));
            var rng = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                float a = (i / 14f) * Mathf.PI * 1.2f + Mathf.PI * 0.9f, r = 13f + (float)rng.NextDouble() * 10f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, 6f + Mathf.Sin(a) * r);
                if (i % 3 == 0) { var g = Part(env, PrimitiveType.Sphere, p, new Vector3(2.5f, 1.6f, 2.2f) * (0.8f + (float)rng.NextDouble()), rock, "Fels"); g.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0); }
                else { Part(env, PrimitiveType.Cylinder, p + Vector3.up * 2f, new Vector3(0.5f, 2f, 0.5f), bark, "Stamm"); Part(env, PrimitiveType.Sphere, p + Vector3.up * 5f, new Vector3(4f, 3.5f, 4f), leaf, "Krone"); }
            }
            for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2f; Part(env, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 6.5f, 1.2f, Mathf.Sin(a) * 6.5f), new Vector3(0.7f, 2.4f, 0.5f), rock, "Stein"); }
            foreach (var r in env.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(r);

            // Figuren im Halbkreis
            var figs = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.Lerp(-36f, 36f, i / 4f) * Mathf.Deg2Rad; // Bogen um einen Mittelpunkt HINTER den Figuren: Blickrichtungen divergieren, keine Verdeckung
                var fig = new GameObject("Figur_" + (i + 1)).transform;
                fig.position = new Vector3(Mathf.Sin(a) * 7f, 0f, 7f - Mathf.Cos(a) * 7f);
                fig.rotation = Quaternion.LookRotation(new Vector3(fig.position.x, 0, fig.position.z - 7f));
                CharacterBuilder.BuildPlayerVisual(fig, out _, out _, null, Anim.AnimDriver.Role.Villager);
                figs[i] = fig;
                Part(env, PrimitiveType.Cylinder, fig.position + Vector3.up * 0.02f, new Vector3(1.4f, 0.04f, 1.4f), Mat("CS_Sockel", new Color(0.7f, 0.66f, 0.58f)), "Sockel");
            }

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 45f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.62f, 0.5f);
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); data.renderPostProcessing = true;

            var ctrlGo = new GameObject("PT_CharacterSelect");
            var ctrl = ctrlGo.AddComponent<CharacterSelectController>();
            ctrl.Classes = ClassBuilder.Library; ctrl.Bank = AudioBuilder.Bank; ctrl.Icons = AssetDatabase.LoadAssetAtPath<IconLibrary>(IconPainter.LibraryPath);
            ctrl.Figures = figs; ctrl.CameraRig = camGo.transform;
            ctrlGo.AddComponent<VfxPool>().Catalog = VfxKitBuilder.Catalog;
            ctrlGo.AddComponent<InputManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
