using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Baut Szenen-Grundgerüst: Systeme, Spieler, Kamera-Rig.</summary>
    public static partial class SceneBuilder
    {
        public const string SandboxPath = Root + "/Scenes/PT_Sandbox.unity";

        [MenuItem("Project Tyrant/Build Sandbox Scene (M1)")]
        public static void BuildSandbox()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSun();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = new Vector3(8, 1, 8);
            ground.GetComponent<MeshRenderer>().sharedMaterial = Mat("Grass", Palette.Grass, 0.35f, null, 0.2f);
            for (int i = 0; i < 6; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "Block" + i;
                c.transform.position = new Vector3(Mathf.Cos(i) * 12f, 1f, Mathf.Sin(i) * 12f);
                c.transform.localScale = new Vector3(2, 2, 2);
                c.GetComponent<MeshRenderer>().sharedMaterial = Mat("Stone", Palette.Stone);
            }
            var systems = CreateSystems();
            var player = CreatePlayer(new Vector3(0, 0.1f, 0));
            CreateCameraRig(player);
            systems.GetComponent<GameManager>().Player = player.GetComponent<Combatant>();
            EditorSceneManager.SaveScene(scene, SandboxPath);
        }

        public static Light CreateSun()
        {
            var go = new GameObject("Sun");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.88f, 0.70f);
            l.intensity = 1.35f;
            l.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = l;
            return l;
        }

        public static GameObject CreateSystems()
        {
            var go = new GameObject("PT_Systems");
            go.AddComponent<InputManager>();
            go.AddComponent<GameManager>();
            AddLateSystems(go);
            return go;
        }

        static partial void AddLateSystemsImpl(GameObject go);
        static void AddLateSystems(GameObject go) => AddLateSystemsImpl(go);

        public static GameObject CreatePlayer(Vector3 pos)
        {
            var go = new GameObject("PT_Player");
            go.tag = "Player";
            go.layer = CharacterLayer;
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.45f; cc.slopeLimit = 50f;
            var combatant = go.AddComponent<Combatant>();
            combatant.DisplayName = "Aeryn Glutwacht";
            combatant.Team = Team.Player;
            combatant.DeriveHealthFromStats = true;
            var visual = CharacterBuilder.BuildPlayerVisual(go.transform, out var gs, out var staff);
            var pc = go.AddComponent<PlayerController>();
            pc.Visual = visual.transform;
            var pv = go.AddComponent<PlayerVisuals>();
            pv.GreatswordModel = gs; pv.StaffModel = staff;
            go.AddComponent<TargetingSystem>().Owner = combatant;
            go.AddComponent<DownedController>();
            go.AddComponent<InteractionSystem>().Player = combatant;
            go.AddComponent<AreaTracker>();
            Child(go.transform, "CameraTarget", new Vector3(0, 1.6f, 0));
            ConfigurePlayerImpl(go);
            SetLayerRecursive(go, CharacterLayer);
            return go;
        }

        static partial void ConfigurePlayerImpl(GameObject player);

        public static Camera CreateCameraRig(GameObject player)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();
            var data = camGo.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camGo.transform.position = player.transform.position + new Vector3(0, 3, -7);

            var target = player.transform.Find("CameraTarget");
            var rigGo = new GameObject("PT_CameraRig");
            var vcam = rigGo.AddComponent<CinemachineCamera>();
            vcam.Follow = target; vcam.LookAt = target;
            var lens = vcam.Lens; lens.FieldOfView = 55f; lens.NearClipPlane = 0.1f; lens.FarClipPlane = 600f; vcam.Lens = lens;
            var orb = rigGo.AddComponent<CinemachineOrbitalFollow>();
            orb.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orb.Radius = 7.5f;
            orb.HorizontalAxis.Wrap = true;
            orb.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orb.VerticalAxis.Range = new Vector2(-15f, 70f);
            orb.VerticalAxis.Value = 18f;
            orb.RadialAxis.Range = new Vector2(0.35f, 1.8f);
            orb.RadialAxis.Value = 1f;
            var comp = rigGo.AddComponent<CinemachineRotationComposer>();
            comp.Damping = new Vector2(0.1f, 0.1f);
            rigGo.AddComponent<CinemachineImpulseListener>();   // Lauf 5: Kamera-Impuls bei schweren Treffern
            var de = rigGo.AddComponent<CinemachineDeoccluder>();
            de.CollideAgainst = 1; // Default-Layer (Umgebung), Charaktere ignorieren
            de.IgnoreTag = "Player";
            var rig = rigGo.AddComponent<ThirdPersonCameraRig>();
            rig.Orbital = orb; rig.OutputCamera = cam; rig.GroundMask = 1;
            return cam;
        }
    }
}
