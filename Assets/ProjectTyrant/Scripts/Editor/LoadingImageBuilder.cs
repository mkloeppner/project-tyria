using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectTyrant.EditorTools
{
    /// <summary>M27: Ladebildschirm-Illustration je Zone – Aufnahme vom Aussichtspunkt (Builder, Teil von Rebuild Everything), PNG unter Data/Zones/Loading.</summary>
    public static class LoadingImageBuilder
    {
        public const string Dir = "Assets/ProjectTyrant/Data/Zones/Loading";
        public const int Width = 1280, Height = 720;

        public static int BuildAll()
        {
            int n = 0;
            var world = WorldBuilder.LoadWorld();
            if (world == null) return 0;
            foreach (var z in world.Zones) if (z != null && Build(z)) n++;
            AssetDatabase.SaveAssets();
            return n;
        }

        public static bool Build(ZoneDefinition z)
        {
            string scenePath = "Assets/ProjectTyrant/Scenes/Zones/" + z.SceneName + ".unity";
            if (!File.Exists(scenePath)) { Debug.LogWarning("[ProjectTyrant] Ladebild: Szene fehlt " + scenePath); return false; }
            EditorSceneManager.OpenScene(WorldBuilder.CorePath, OpenSceneMode.Single);
            var zs = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(zs);

            Pose(z, out var pos, out var look);
            var go = new GameObject("PT_LoadingCapture"); var cam = go.AddComponent<Camera>();
            cam.transform.position = pos; cam.transform.LookAt(look);
            cam.fieldOfView = 55f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            if (z.Biome != null) cam.backgroundColor = z.Biome.FogColor;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            // Vegetation wird zur Laufzeit instanziert gezeichnet – für die Aufnahme direkt für diese Kamera einreihen
            foreach (var vr in Object.FindObjectsByType<VegetationRenderer>()) vr.DrawFor(cam, true);
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); tex.Apply();
            AutoLevels(tex);
            RenderTexture.active = prev; cam.targetTexture = null;
            Object.DestroyImmediate(go); rt.Release(); Object.DestroyImmediate(rt);

            Directory.CreateDirectory(Dir);
            string path = Dir + "/Loading_" + z.Id + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = false; imp.maxTextureSize = 2048; imp.npotScale = TextureImporterNPOTScale.None; imp.SaveAndReimport(); }
            z.LoadingImage = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorUtility.SetDirty(z);
            return z.LoadingImage != null;
        }

        /// <summary>Tonwertkorrektur für dunkle Zonen (Kristalltiefe): 2-%/98-%-Perzentile der Helligkeit auf 0,06–0,9 strecken, nur wenn der Kontrast zu gering ist.</summary>
        static void AutoLevels(Texture2D tex)
        {
            var px = tex.GetPixels();
            var lum = new float[px.Length];
            for (int i = 0; i < px.Length; i++) lum[i] = px[i].grayscale;
            var sorted = (float[])lum.Clone(); System.Array.Sort(sorted);
            float lo = sorted[(int)(sorted.Length * 0.02f)], hi = sorted[(int)(sorted.Length * 0.98f)];
            if (hi - lo > 0.35f) return;   // genug Kontrast: Aufnahme unverändert
            float scale = (0.9f - 0.06f) / Mathf.Max(0.02f, hi - lo);
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                c.r = Mathf.Clamp01(0.06f + (c.r - lo) * scale); c.g = Mathf.Clamp01(0.06f + (c.g - lo) * scale); c.b = Mathf.Clamp01(0.06f + (c.b - lo) * scale);
                px[i] = c;
            }
            tex.SetPixels(px); tex.Apply();
        }

        /// <summary>Blick vom Aussichtspunkt über die Zone: Kamera hinter und über der Stele, Blick zur Zonenmitte (ohne Aussichtspunkt: vom Startpunkt).</summary>
        static void Pose(ZoneDefinition z, out Vector3 pos, out Vector3 look)
        {
            Vector3 from = z.DefaultSpawn;
            var vistas = Object.FindObjectsByType<Vista>();
            if (vistas.Length > 0)
            {
                // Aussichtspunkt mit dem weitesten Blick zur Mitte
                float best = -1f;
                foreach (var v in vistas) { float d = new Vector2(v.transform.position.x, v.transform.position.z).magnitude; if (d > best) { best = d; from = v.transform.position; } }
            }
            var center = ZoneAnchors.GroundAt(Vector3.zero);
            var dir = center - from; dir.y = 0f;
            if (dir.sqrMagnitude < 4f) dir = Vector3.forward;
            dir.Normalize();
            pos = from - dir * 6f + Vector3.up * 9f;
            float span = Mathf.Max(30f, (center - from).magnitude);
            look = from + dir * Mathf.Min(span, z.Size * 0.4f);
            look.y = center.y + 2f;
        }
    }
}
