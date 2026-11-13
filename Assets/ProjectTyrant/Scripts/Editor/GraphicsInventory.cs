using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Grafik-Lauf G1: technischer Bestand (URP, Renderer-Features, Shader, Materialien, Partikel je Zone, Volumes, System) als Markdown.</summary>
    public static class GraphicsInventory
    {
        [MenuItem("Project Tyrant/Grafik G1/Technischer Bestand")]
        public static void WriteMenu() => Debug.Log("[ProjectTyrant] Bestand: " + Write("vorher"));

        public static string Write(string tag, bool scanZones = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Technischer Grafik-Bestand (" + tag + ") – " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine();
            sb.AppendLine("## System");
            sb.AppendLine("- GPU: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ", " + SystemInfo.graphicsMemorySize + " MB)");
            sb.AppendLine("- CPU: " + SystemInfo.processorType + " · " + SystemInfo.processorCount + " Kerne · RAM " + SystemInfo.systemMemorySize + " MB");
            sb.AppendLine("- Unity " + Application.unityVersion + " · Qualitätsstufe " + QualitySettings.names[QualitySettings.GetQualityLevel()] + " (" + QualitySettings.GetQualityLevel() + ")");
            sb.AppendLine();
            sb.AppendLine("## URP-Assets je Qualitätsstufe");
            sb.AppendLine("| Stufe | Asset | Pfad | HDR | MSAA | Skala | Schatten | Kaskaden | weich | Tiefe | Opaque |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var rp = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (rp == null) { sb.AppendLine("| " + QualitySettings.names[i] + " | – |"); continue; }
                sb.AppendLine("| " + QualitySettings.names[i] + " | " + rp.name + " | " + RenderingPath(rp) + " | " + rp.supportsHDR + " | " + rp.msaaSampleCount + " | " + rp.renderScale + " | " + rp.shadowDistance + " m | " + rp.shadowCascadeCount + " | " + rp.supportsSoftShadows + " | " + rp.supportsCameraDepthTexture + " | " + rp.supportsCameraOpaqueTexture + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Renderer-Features");
            foreach (var g in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(g));
                if (rd == null) continue;
                sb.AppendLine("- **" + rd.name + "** (" + rd.renderingMode + "): " + (rd.rendererFeatures.Count == 0 ? "keine" : string.Join(", ", rd.rendererFeatures.Where(f => f != null).Select(f => f.name + (f.isActive ? "" : " (aus)")))));
            }
            sb.AppendLine();
            sb.AppendLine("## Shader (Assets/ProjectTyrant/Shaders)");
            sb.AppendLine("| Shader | Datei | Passes | Keywords | SRP-Batcher (CBUFFER) | Materialien |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var mats = AssetDatabase.FindAssets("t:Material", new[] { "Assets/ProjectTyrant" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m != null).ToList();
            var byShader = mats.GroupBy(m => m.shader != null ? m.shader.name : "(fehlt)").ToDictionary(g => g.Key, g => g.Count());
            foreach (var g in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/ProjectTyrant/Shaders" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g); var sh = AssetDatabase.LoadAssetAtPath<Shader>(path); if (sh == null) continue;
                string src = File.ReadAllText(path);
                bool cb = src.Contains("CBUFFER_START(UnityPerMaterial)");
                byShader.TryGetValue(sh.name, out int nm);
                sb.AppendLine("| " + sh.name + " | " + Path.GetFileName(path) + " | " + sh.passCount + " | " + sh.keywordSpace.keywordCount + " | " + (cb ? "ja" : "nein") + " | " + nm + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Materialien je Shader (" + mats.Count + " gesamt)");
            foreach (var kv in byShader.OrderByDescending(k => k.Value)) sb.AppendLine("- " + kv.Key + ": " + kv.Value + (kv.Key.StartsWith("Universal Render Pipeline") || kv.Key.StartsWith("Sprites") || kv.Key.StartsWith("Standard") ? " ⚠ Unity-Shader" : ""));
            sb.AppendLine();
            sb.AppendLine("## Post-Processing-Profile");
            foreach (var g in AssetDatabase.FindAssets("t:VolumeProfile", new[] { "Assets/ProjectTyrant" }))
            {
                var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(g)); if (p == null) continue;
                sb.AppendLine("- " + p.name + ": " + string.Join(", ", p.components.Where(c => c != null && c.active).Select(c => c.GetType().Name)));
            }
            if (scanZones)
            {
                sb.AppendLine();
                sb.AppendLine("## Zonen-Szenen: Partikelsysteme, Lichter, Renderer");
                sb.AppendLine("| Zone | Partikelsysteme | max. Partikel | Lichter | Renderer | Dreiecke (Szene) | Volumes |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
                {
                    var z = WorldBuilder.Zone(zone); if (z == null) continue;
                    string path = GraphicsBuilder.ScenePathOf(z); if (!File.Exists(path)) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    var roots = scene.GetRootGameObjects();
                    var ps = roots.SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)).ToList();
                    int maxP = ps.Sum(p => p.main.maxParticles);
                    int lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Count();
                    var rends = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).ToList();
                    long tris = 0; foreach (var mf in rends) if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                    int vols = roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).Count();
                    sb.AppendLine("| " + zone + " | " + ps.Count + " | " + maxP + " | " + lights + " | " + rends.Count + " | " + tris.ToString("N0") + " | " + vols + " |");
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            string outPath = Path.Combine(GraphicsCapture.Root, "bestand_" + tag + ".md");
            Directory.CreateDirectory(GraphicsCapture.Root);
            File.WriteAllText(outPath, sb.ToString());
            return outPath;
        }

        static string RenderingPath(UniversalRenderPipelineAsset rp)
        {
            var so = new SerializedObject(rp);
            var list = so.FindProperty("m_RendererDataList");
            if (list != null && list.arraySize > 0 && list.GetArrayElementAtIndex(0).objectReferenceValue is UniversalRendererData rd) return rd.renderingMode.ToString();
            return "?";
        }
    }
}
