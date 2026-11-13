using System.Collections.Generic;
using System.IO;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class GraphicsBuilder
    {
        static readonly (string id, TextureKind kind, int size, int seed, float scale, int oct, int tiles, FlipbookShape shape, bool srgb)[] Recipes =
        {
            ("Noise_Perlin", TextureKind.Perlin, 256, 11, 8, 1, 4, FlipbookShape.Explosion, false),
            ("Noise_Simplex", TextureKind.Simplex, 512, 12, 6, 5, 4, FlipbookShape.Explosion, false),
            ("Noise_Worley", TextureKind.Worley, 256, 13, 8, 1, 4, FlipbookShape.Explosion, false),
            ("Noise_Voronoi", TextureKind.Voronoi, 256, 14, 10, 1, 4, FlipbookShape.Explosion, false),
            ("Noise_Ridged", TextureKind.Ridged, 256, 15, 6, 4, 4, FlipbookShape.Explosion, false),
            ("Noise_Warp", TextureKind.DomainWarp, 512, 16, 4, 4, 4, FlipbookShape.Explosion, false),
            ("Caustics", TextureKind.Caustics, 512, 17, 6, 1, 4, FlipbookShape.Explosion, false),
            ("FlowMap", TextureKind.FlowMap, 256, 18, 4, 2, 4, FlipbookShape.Explosion, false),
            ("Clouds", TextureKind.Clouds, 512, 19, 5, 3, 4, FlipbookShape.Explosion, false),
            ("Starfield", TextureKind.Starfield, 1024, 20, 1, 1, 4, FlipbookShape.Explosion, false),
            ("Cookie_Leaves", TextureKind.CookieLeaves, 512, 21, 6, 3, 4, FlipbookShape.Explosion, false),
            ("Cookie_Grid", TextureKind.CookieGrid, 256, 22, 4, 1, 4, FlipbookShape.Explosion, false),
            ("Ramp_Toon", TextureKind.Ramp, 256, 23, 1, 1, 4, FlipbookShape.Explosion, false),
            ("Flip_Explosion", TextureKind.Flipbook, 512, 31, 1, 1, 8, FlipbookShape.Explosion, true),
            ("Flip_Rauch", TextureKind.Flipbook, 512, 32, 1, 1, 8, FlipbookShape.Rauch, true),
            ("Flip_Feuer", TextureKind.Flipbook, 512, 33, 1, 1, 4, FlipbookShape.Feuer, true),
            ("Flip_Blitz", TextureKind.Flipbook, 512, 34, 1, 1, 4, FlipbookShape.Blitz, true),
            ("Flip_Spritzer", TextureKind.Flipbook, 512, 35, 1, 1, 4, FlipbookShape.Spritzer, true),
            ("Flip_Schneeflocke", TextureKind.Flipbook, 256, 36, 1, 1, 4, FlipbookShape.Schneeflocke, true),
            ("Flip_Blatt", TextureKind.Flipbook, 256, 37, 1, 1, 4, FlipbookShape.Blatt, true),
            ("Flip_Funke", TextureKind.Flipbook, 256, 38, 1, 1, 4, FlipbookShape.Funke, true),
            ("Flip_Glyphe", TextureKind.Flipbook, 512, 39, 1, 1, 4, FlipbookShape.Glyphe, true),
        };

        static Gradient ToonRamp()
        {
            var g = new Gradient();
            g.mode = GradientMode.Fixed;
            g.SetKeys(new[] { new GradientColorKey(new Color(0.18f, 0.18f, 0.18f), 0.32f), new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 0.52f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 0.8f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        /// <summary>Erzeugt alle Rezepte und Texturen; Rückgabe: Anzahl. Prüft den Hash gegen die letzte Erzeugung.</summary>
        public static int BuildTextures()
        {
            EnsureFolder(Dir); EnsureFolder(RecipeDir); EnsureFolder(Root + "/Generated"); EnsureFolder(TexDir);
            int n = 0, changed = 0; long bytes = 0;
            foreach (var r in Recipes)
            {
                string rp = RecipeDir + "/Tex_" + r.id + ".asset";
                var rec = AssetDatabase.LoadAssetAtPath<TextureRecipe>(rp);
                if (rec == null) { rec = ScriptableObject.CreateInstance<TextureRecipe>(); AssetDatabase.CreateAsset(rec, rp); }
                rec.Id = r.id; rec.Kind = r.kind; rec.Size = r.size; rec.Seed = r.seed; rec.Scale = r.scale; rec.Octaves = r.oct; rec.Tiles = r.tiles; rec.Shape = r.shape; rec.Srgb = r.srgb;
                rec.Ramp = r.kind == TextureKind.Ramp ? ToonRamp() : null; rec.Repeat = r.kind != TextureKind.Flipbook && r.kind != TextureKind.Ramp && r.kind != TextureKind.CookieGrid;
                var px = TextureForge.Generate(rec.Kind, rec.Size, rec.Seed, rec.Scale, rec.Octaves, rec.Tiles, rec.Shape, rec.Ramp);
                string hash = TextureForge.HashOf(px);
                if (!string.IsNullOrEmpty(rec.Hash) && rec.Hash != hash) changed++;
                rec.Hash = hash; EditorUtility.SetDirty(rec);
                string path = TexDir + "/T_" + r.id + ".png";
                int size = Mathf.RoundToInt(Mathf.Sqrt(px.Length));
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply(false);
                File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.sRGBTexture = rec.Srgb; imp.mipmapEnabled = rec.Mipmaps; imp.wrapMode = rec.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.alphaIsTransparency = r.kind == TextureKind.Flipbook; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
                bytes += (long)size * size * 4; n++;
            }
            LastReport.Add(n + " Texturen (" + (bytes / (1024 * 1024)) + " MB roh" + (changed > 0 ? ", " + changed + " Hash geändert" : ", Hashes stabil") + ")");
            return n;
        }

        public static Texture2D Tex(string id) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_" + id + ".png");

        /// <summary>Determinismus: zweimal erzeugen, Hashes vergleichen (ohne zu schreiben).</summary>
        public static int VerifyDeterminism()
        {
            int bad = 0;
            foreach (var r in Recipes)
            {
                var a = TextureForge.HashOf(TextureForge.Generate(r.kind, r.size, r.seed, r.scale, r.oct, r.tiles, r.shape, r.kind == TextureKind.Ramp ? ToonRamp() : null));
                var b = TextureForge.HashOf(TextureForge.Generate(r.kind, r.size, r.seed, r.scale, r.oct, r.tiles, r.shape, r.kind == TextureKind.Ramp ? ToonRamp() : null));
                if (a != b) bad++;
            }
            return bad;
        }
    }
}
