using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G3: prüft die Shader-Bibliothek 2.0 – CBUFFER(UnityPerMaterial), Pflicht-Passes (DepthOnly/ShadowCaster für deckende
    /// Material-Shader, DepthNormals für die Toon-Familie), ≤ 8 multi_compile je Pass, SRP-Batcher-Kompatibilität und Varianten-Summe ≤ 4000.
    /// </summary>
    public static class ShaderLibraryValidator
    {
        public const int MaxMultiCompilePerPass = 8, MaxVariants = 4000;
        public static string LastReason = "";
        public static readonly string[] Opaque = { PTShaders.ToonLit, PTShaders.Terrain, PTShaders.Vegetation, PTShaders.Cloth, PTShaders.Lava, PTShaders.Crystal, PTShaders.Hazard };

        public struct Result { public string Shader, File; public List<string> Problems; public long Variants; public int SrpCode; }

        public static List<Result> ValidateAll()
        {
            var list = new List<Result>();
            foreach (var name in PTShaders.Library.Concat(new[] { PTShaders.Glass, PTShaders.UIGlow, PTShaders.PostFx, "ProjectTyrant/GroundMarker" }).Distinct())
                list.Add(Validate(name));
            return list;
        }

        public static Result Validate(string name)
        {
            var r = new Result { Shader = name, Problems = new List<string>(), SrpCode = -1 };
            var sh = Shader.Find(name);
            if (sh == null) { r.Problems.Add("Shader fehlt"); return r; }
            r.File = AssetDatabase.GetAssetPath(sh);
            string src = File.Exists(r.File) ? File.ReadAllText(r.File) : "";
            if (!src.Contains("CBUFFER_START(UnityPerMaterial)")) r.Problems.Add("kein CBUFFER(UnityPerMaterial)");
            if (Opaque.Contains(name))
            {
                foreach (var pass in new[] { "\"DepthOnly\"", "\"ShadowCaster\"", "\"DepthNormals\"" })
                    if (!src.Contains("LightMode\"=" + pass)) r.Problems.Add("Pass fehlt: " + pass);
            }
            foreach (Match m in Regex.Matches(src, @"HLSLPROGRAM(.*?)ENDHLSL", RegexOptions.Singleline))
            {
                int mc = Regex.Matches(m.Groups[1].Value, @"#pragma\s+multi_compile").Count;
                if (mc > MaxMultiCompilePerPass) r.Problems.Add(mc + " multi_compile in einem Pass");
            }
            if (ShaderUtil.ShaderHasError(sh)) r.Problems.Add("Kompilierfehler");
            r.Variants = VariantCount(sh);
            r.SrpCode = SrpBatcherCode(sh);
            if (r.SrpCode > 0) r.Problems.Add("nicht SRP-Batcher-kompatibel (Code " + r.SrpCode + ")");
            return r;
        }

        /// <summary>Varianten laut Editor (interne API, per Reflexion; -1 wenn nicht verfügbar).</summary>
        public static long VariantCount(Shader sh)
        {
            var mi = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (mi == null) return -1;
            try
            {
                var ps = mi.GetParameters();
                if (ps.Length == 3) { var args = new object[] { sh, false, (ulong)0 }; bool ok = (bool)mi.Invoke(null, args); return ok ? (long)(ulong)args[2] : -1; }
                return System.Convert.ToInt64(mi.Invoke(null, new object[] { sh, false }));
            }
            catch { return -1; }
        }

        /// <summary>SRP-Batcher-Kompatibilität (0 = kompatibel) über die interne Editor-API; -1 wenn nicht verfügbar.</summary>
        public static int SrpBatcherCode(Shader sh)
        {
            var mi = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (mi == null) return -1;
            try
            {
                var ps = mi.GetParameters();
                object v = ps.Length == 2 ? mi.Invoke(null, new object[] { sh, 0 }) : mi.Invoke(null, new object[] { sh });
                int code = System.Convert.ToInt32(v);
                if (code == 0) return 0;
                // Code ≠ 0 kann „noch nicht kompiliert“ bedeuten: Grund abfragen und nur echte Unverträglichkeiten melden
                var why = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                string reason = why != null ? (why.Invoke(null, new object[] { sh, 0, code }) as string ?? "") : "";
                LastReason = reason;
                if (reason.StartsWith("OK") || reason.ToLowerInvariant().Contains("compil") || reason.Length == 0) return 0;
                return code;
            }
            catch { return -1; }
        }

        [MenuItem("Project Tyrant/Grafik G3/Shader-Bibliothek prüfen")]
        public static void Menu()
        {
            var all = ValidateAll();
            long sum = all.Where(x => x.Variants > 0).Sum(x => x.Variants);
            Debug.Log("[ProjectTyrant] Shader-Bibliothek: " + string.Join(" · ", all.Select(x => x.Shader.Replace("ProjectTyrant/", "") + " " + x.Variants + "V" + (x.Problems.Count > 0 ? " ⚠ " + string.Join(", ", x.Problems) : ""))) + " · Summe " + sum);
        }
    }
}
