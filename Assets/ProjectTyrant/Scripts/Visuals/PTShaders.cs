using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G2/G3: Shader-Namen der Bibliothek 2.0 an einer Stelle; Laufzeit-Materialien nutzen nie Unity-Shader.</summary>
    public static class PTShaders
    {
        public const string ToonLit = "ProjectTyrant/ToonLit", Sky = "ProjectTyrant/Sky", PostFx = "ProjectTyrant/Fullscreen/PostFx", FxParticle = "ProjectTyrant/FX/Particle";
        public const string Terrain = "ProjectTyrant/Terrain", Vegetation = "ProjectTyrant/Vegetation", Cloth = "ProjectTyrant/Cloth", Lava = "ProjectTyrant/Lava",
            Crystal = "ProjectTyrant/Crystal", Hazard = "ProjectTyrant/Hazard", Glass = "ProjectTyrant/Glass", Water2 = "ProjectTyrant/Water2", UIGlow = "ProjectTyrant/UI/Glow";
        /// <summary>Material-Shader der Bibliothek 2.0 (SRP-Batcher, Depth-/Shadow-Passes, Validator).</summary>
        public static readonly string[] Library = { ToonLit, Terrain, Vegetation, Cloth, Lava, Crystal, Hazard, Glass, Water2, Sky };
        /// <summary>Alte Shader, die nach der Migration kein Material mehr nutzen darf.</summary>
        public static readonly string[] Retired = { "ProjectTyrant/ToonRim", "ProjectTyrant/GradientSkybox", "ProjectTyrant/Water" };

        static Shader _lit;
        /// <summary>ToonLit für Laufzeit-Materialien (Ereignis-Requisiten, Gesprächsbühne); <c>material.color</c> setzt _BaseColor.</summary>
        public static Shader Lit()
        {
            if (_lit == null) _lit = Shader.Find(ToonLit);
            return _lit;
        }

        public static bool IsProjectShader(Shader s) => s != null && (s.name.StartsWith("ProjectTyrant/") || s.name.StartsWith("TextMeshPro/") || s.name.StartsWith("Hidden/") || s.name == "GUI/Text Shader" || s.name.StartsWith("UI/"));
        public static bool IsRetired(Shader s) { if (s == null) return false; foreach (var r in Retired) if (s.name == r) return true; return false; }
    }
}
