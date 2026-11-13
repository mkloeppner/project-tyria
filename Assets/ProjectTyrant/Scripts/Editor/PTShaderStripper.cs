using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G7: entfernt im Build Varianten der Projekt-Shader, die keine Stufe nutzt – alle Stufen sind Forward+
    /// (ohne `_CLUSTER_LIGHT_LOOP` unbenutzt), Bildschirm-Schatten und Lightmaps gibt es nicht.
    /// </summary>
    public class PTShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 10;
        public static int Stripped, Kept;
        static readonly ShaderKeyword Cluster = new ShaderKeyword("_CLUSTER_LIGHT_LOOP"), ScreenShadows = new ShaderKeyword("_MAIN_LIGHT_SHADOWS_SCREEN"), Lightmap = new ShaderKeyword("LIGHTMAP_ON");

        /// <summary>Rein: wird diese Variante eines Projekt-Shaders entfernt?</summary>
        public static bool Strip(string shaderName, PassType pass, bool hasCluster, bool hasScreenShadows, bool hasLightmap)
        {
            if (shaderName == null || !shaderName.StartsWith("ProjectTyrant/")) return false;
            if (hasScreenShadows || hasLightmap) return true;
            if (pass == PassType.ScriptableRenderPipeline && !hasCluster && IsToonFamily(shaderName)) return true;
            return false;
        }

        public static bool IsToonFamily(string n) => n == "ProjectTyrant/ToonLit" || n == "ProjectTyrant/Terrain" || n == "ProjectTyrant/Vegetation" || n == "ProjectTyrant/Cloth" || n == "ProjectTyrant/Lava" || n == "ProjectTyrant/Crystal" || n == "ProjectTyrant/Hazard";

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || !shader.name.StartsWith("ProjectTyrant/")) return;
            bool forward = snippet.passName == "ForwardToon";
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var ks = data[i].shaderKeywordSet;
                bool cluster = ks.IsEnabled(Cluster);
                if (Strip(shader.name, forward ? PassType.ScriptableRenderPipeline : PassType.Normal, cluster, ks.IsEnabled(ScreenShadows), ks.IsEnabled(Lightmap))) { data.RemoveAt(i); Stripped++; }
                else Kept++;
            }
        }
    }
}
