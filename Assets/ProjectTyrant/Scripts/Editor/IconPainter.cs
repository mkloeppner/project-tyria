using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Zeichnet UI-Icons prozedural (SDF-Formen + 5×7-Glyphen) und erzeugt die IconLibrary.</summary>
    public partial class IconPainter
    {
        public const string IconDir = PTEditorUtil.Root + "/UI/Icons";
        public const string LibraryPath = PTEditorUtil.Root + "/UI/IconLibrary.asset";

        readonly int _size;
        readonly Color[] _px;

        public IconPainter(int size = 64) { _size = size; _px = new Color[size * size]; }

        public IconPainter Clear(Color c) { for (int i = 0; i < _px.Length; i++) _px[i] = c; return this; }

        /// <summary>Malt eine Form über ihre Distanzfunktion (Pixel, negativ = innen) mit Kantenglättung.</summary>
        public IconPainter Fill(Func<float, float, float> sdf, Color c)
        {
            for (int y = 0; y < _size; y++)
            for (int x = 0; x < _size; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - d) * c.a;
                if (a <= 0f) continue;
                int i = y * _size + x;
                var dst = _px[i];
                float outA = a + dst.a * (1f - a);
                if (outA <= 0f) continue;
                _px[i] = new Color((c.r * a + dst.r * dst.a * (1f - a)) / outA, (c.g * a + dst.g * dst.a * (1f - a)) / outA, (c.b * a + dst.b * dst.a * (1f - a)) / outA, outA);
            }
            return this;
        }

        /// <summary>Vertikaler Farbverlauf innerhalb einer Form.</summary>
        public IconPainter FillGradient(Func<float, float, float> sdf, Color top, Color bottom)
        {
            for (int y = 0; y < _size; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(_size - 1));
                for (int x = 0; x < _size; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d) * c.a;
                    if (a <= 0f) continue;
                    int i = y * _size + x;
                    var dst = _px[i];
                    float outA = a + dst.a * (1f - a);
                    _px[i] = new Color((c.r * a + dst.r * dst.a * (1f - a)) / outA, (c.g * a + dst.g * dst.a * (1f - a)) / outA, (c.b * a + dst.b * dst.a * (1f - a)) / outA, outA);
                }
            }
            return this;
        }

        // ---------- Distanzfunktionen ----------
        public static Func<float, float, float> Circle(float cx, float cy, float r) => (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
        public static Func<float, float, float> Ring(float cx, float cy, float r, float w) => (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w * 0.5f;
        public static Func<float, float, float> Box(float cx, float cy, float hw, float hh, float radius = 0f) => (x, y) =>
        {
            float dx = Mathf.Abs(x - cx) - hw + radius, dy = Mathf.Abs(y - cy) - hh + radius;
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
        };
        public static Func<float, float, float> Segment(float ax, float ay, float bx, float by, float w) => (x, y) =>
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(0.0001f, bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - w * 0.5f;
        };
        public static Func<float, float, float> Polygon(params Vector2[] pts) => (x, y) =>
        {
            float d = float.MaxValue; bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                Vector2 a = pts[j], b = pts[i];
                if (((b.y > y) != (a.y > y)) && (x < (a.x - b.x) * (y - b.y) / (a.y - b.y + 1e-6f) + b.x)) inside = !inside;
                float pax = x - a.x, pay = y - a.y, bax = b.x - a.x, bay = b.y - a.y;
                float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(0.0001f, bax * bax + bay * bay));
                float dx = pax - bax * h, dy = pay - bay * h;
                d = Mathf.Min(d, Mathf.Sqrt(dx * dx + dy * dy));
            }
            return inside ? -d : d;
        };
        public static Func<float, float, float> Union(params Func<float, float, float>[] fs) => (x, y) => { float d = float.MaxValue; foreach (var f in fs) d = Mathf.Min(d, f(x, y)); return d; };
        public static Func<float, float, float> Subtract(Func<float, float, float> a, Func<float, float, float> b) => (x, y) => Mathf.Max(a(x, y), -b(x, y));

        public static Vector2[] RegularPolygon(float cx, float cy, float r, int n, float rotDeg = 90f)
        {
            var p = new Vector2[n];
            for (int i = 0; i < n; i++) { float a = (rotDeg + i * 360f / n) * Mathf.Deg2Rad; p[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r); }
            return p;
        }

        // ---------- 5×7-Glyphen ----------
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            {'A', new[]{" ### ","#   #","#   #","#####","#   #","#   #","#   #"}}, {'B', new[]{"#### ","#   #","#   #","#### ","#   #","#   #","#### "}},
            {'C', new[]{" ####","#    ","#    ","#    ","#    ","#    "," ####"}}, {'D', new[]{"#### ","#   #","#   #","#   #","#   #","#   #","#### "}},
            {'E', new[]{"#####","#    ","#    ","#### ","#    ","#    ","#####"}}, {'F', new[]{"#####","#    ","#    ","#### ","#    ","#    ","#    "}},
            {'G', new[]{" ####","#    ","#    ","# ###","#   #","#   #"," ### "}}, {'H', new[]{"#   #","#   #","#   #","#####","#   #","#   #","#   #"}},
            {'I', new[]{"#####","  #  ","  #  ","  #  ","  #  ","  #  ","#####"}}, {'K', new[]{"#   #","#  # ","# #  ","##   ","# #  ","#  # ","#   #"}},
            {'L', new[]{"#    ","#    ","#    ","#    ","#    ","#    ","#####"}}, {'M', new[]{"#   #","## ##","# # #","#   #","#   #","#   #","#   #"}},
            {'N', new[]{"#   #","##  #","# # #","#  ##","#   #","#   #","#   #"}}, {'O', new[]{" ### ","#   #","#   #","#   #","#   #","#   #"," ### "}},
            {'P', new[]{"#### ","#   #","#   #","#### ","#    ","#    ","#    "}}, {'Q', new[]{" ### ","#   #","#   #","#   #","# # #","#  # "," ## #"}},
            {'R', new[]{"#### ","#   #","#   #","#### ","# #  ","#  # ","#   #"}}, {'S', new[]{" ####","#    ","#    "," ### ","    #","    #","#### "}},
            {'T', new[]{"#####","  #  ","  #  ","  #  ","  #  ","  #  ","  #  "}}, {'U', new[]{"#   #","#   #","#   #","#   #","#   #","#   #"," ### "}},
            {'V', new[]{"#   #","#   #","#   #","#   #","#   #"," # # ","  #  "}}, {'W', new[]{"#   #","#   #","#   #","# # #","# # #","## ##","#   #"}},
            {'X', new[]{"#   #","#   #"," # # ","  #  "," # # ","#   #","#   #"}}, {'Y', new[]{"#   #","#   #"," # # ","  #  ","  #  ","  #  ","  #  "}},
            {'Z', new[]{"#####","    #","   # ","  #  "," #   ","#    ","#####"}}, {'+', new[]{"     ","  #  ","  #  ","#####","  #  ","  #  ","     "}},
            {'!', new[]{"  #  ","  #  ","  #  ","  #  ","  #  ","     ","  #  "}}, {'?', new[]{" ### ","#   #","    #","   # ","  #  ","     ","  #  "}},
            {'~', new[]{"     ","     "," #  #","# ## ","     ","     ","     "}}, {'*', new[]{"     ","# # #"," ### ","#####"," ### ","# # #","     "}},
            {'>', new[]{"#    "," #   ","  #  ","   # ","  #  "," #   ","#    "}}, {'1', new[]{"  #  "," ##  ","  #  ","  #  ","  #  ","  #  "," ### "}},
            {'2', new[]{" ### ","#   #","    #","   # ","  #  "," #   ","#####"}}, {'3', new[]{"#### ","    #","    #"," ### ","    #","    #","#### "}},
        };

        /// <summary>Zeichnet Text (Großbuchstaben) zentriert. Höhe in Pixeln.</summary>
        public IconPainter Glyphs(string text, float cx, float cy, float height, Color c)
        {
            if (string.IsNullOrEmpty(text)) return this;
            text = text.ToUpperInvariant();
            float cell = height / 7f;
            float width = text.Length * 5 * cell + (text.Length - 1) * cell;
            float x0 = cx - width * 0.5f, y0 = cy + height * 0.5f;
            var boxes = new List<Func<float, float, float>>();
            for (int k = 0; k < text.Length; k++)
            {
                if (!Font.TryGetValue(text[k], out var rows)) continue;
                for (int r = 0; r < 7; r++)
                for (int col = 0; col < 5; col++)
                {
                    if (rows[r][col] != '#') continue;
                    float bx = x0 + (k * 6 + col + 0.5f) * cell, by = y0 - (r + 0.5f) * cell;
                    boxes.Add(Box(bx, by, cell * 0.55f, cell * 0.55f, cell * 0.2f));
                }
            }
            if (boxes.Count > 0) Fill(Union(boxes.ToArray()), c);
            return this;
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(_size, _size, TextureFormat.RGBA32, false);
            t.SetPixels(_px); t.Apply();
            return t;
        }

        // ---------- Speichern & Bibliothek ----------
        static readonly List<IconLibrary.Entry> _entries = new List<IconLibrary.Entry>();
        static readonly List<(string id, string path, Vector4 border)> _pending = new List<(string, string, Vector4)>();

        public static void Save(string id, IconPainter p, Vector4 border = default)
        {
            PTEditorUtil.EnsureFolder(IconDir);
            string path = IconDir + "/" + id + ".png";
            var tex = p.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            _pending.Add((id, path, border));
        }

        [MenuItem("Project Tyrant/Build Icons")]
        public static IconLibrary BuildAll()
        {
            _entries.Clear(); _pending.Clear();
            PaintUi();
            PaintEffects();
            PaintFacts();
            PaintExtra();
            AssetDatabase.Refresh();
            foreach (var (id, path, border) in _pending)
            {
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spriteBorder = border;
                imp.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                _entries.Add(new IconLibrary.Entry { Id = id, Sprite = sprite });
            }
            var lib = AssetDatabase.LoadAssetAtPath<IconLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<IconLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            lib.Entries = new List<IconLibrary.Entry>(_entries);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }

        static partial void PaintExtra();

        static void PaintUi()
        {
            Save("ui_round", new IconPainter(32).Fill(Box(16, 16, 15.5f, 15.5f, 6f), Color.white), new Vector4(8, 8, 8, 8));
            Save("ui_panel", new IconPainter(48).Fill(Box(24, 24, 23.5f, 23.5f, 8f), new Color(1, 1, 1, 1)).Fill(Subtract(Box(24, 24, 23.5f, 23.5f, 8f), Box(24, 24, 21.5f, 21.5f, 7f)), new Color(0.85f, 0.7f, 0.45f, 0.9f)), new Vector4(12, 12, 12, 12));
            Save("ui_circle", new IconPainter(128).Fill(Circle(64, 64, 63), Color.white));
            Save("ui_ring", new IconPainter(128).Fill(Ring(64, 64, 60, 6), Color.white));
            Save("ui_slot", new IconPainter(64).Fill(Box(32, 32, 31, 31, 6), new Color(0.12f, 0.11f, 0.14f)).Fill(Subtract(Box(32, 32, 31, 31, 6), Box(32, 32, 28, 28, 5)), Color.white), new Vector4(10, 10, 10, 10));
            Save("ui_frame", new IconPainter(64).Fill(Subtract(Box(32, 32, 31, 31, 6), Box(32, 32, 27, 27, 5)), Color.white), new Vector4(10, 10, 10, 10));
        }

        static void PaintEffects()
        {
            foreach (var info in EffectCatalog.All)
            {
                var p = new IconPainter(64);
                bool boon = info.Cat == EffectCategory.Boon;
                Color bgTop = boon ? new Color(0.25f, 0.45f, 0.75f) : new Color(0.55f, 0.15f, 0.12f);
                Color bgBot = boon ? new Color(0.08f, 0.15f, 0.3f) : new Color(0.25f, 0.05f, 0.05f);
                p.FillGradient(Circle(32, 32, 30), bgTop, bgBot);
                p.Fill(Ring(32, 32, 28.5f, 3.5f), info.Color);
                p.Glyphs(info.Glyph.Length > 2 ? info.Glyph.Substring(0, 2) : info.Glyph, 32, 32, info.Glyph.Length > 1 ? 18 : 26, Color.white);
                Save(IconLibrary.EffectId(info.Type), p);
            }
        }

        static void PaintFacts()
        {
            var w = new Color(0.95f, 0.92f, 0.85f);
            Save("fact_damage", new IconPainter(32).Fill(Polygon(new Vector2(6, 4), new Vector2(26, 24), new Vector2(23, 27), new Vector2(3, 7)), w).Fill(Segment(18, 24, 26, 16, 3), w));
            Save("fact_heal", new IconPainter(32).Fill(Union(Box(16, 16, 4, 12, 1), Box(16, 16, 12, 4, 1)), new Color(0.5f, 1f, 0.5f)));
            Save("fact_time", new IconPainter(32).Fill(Ring(16, 16, 12, 3), w).Fill(Segment(16, 16, 16, 24, 2.5f), w).Fill(Segment(16, 16, 22, 16, 2.5f), w));
            Save("fact_range", new IconPainter(32).Fill(Segment(4, 16, 26, 16, 3), w).Fill(Polygon(new Vector2(20, 22), new Vector2(29, 16), new Vector2(20, 10)), w));
            Save("fact_radius", new IconPainter(32).Fill(Ring(16, 16, 12, 2.5f), w).Fill(Circle(16, 16, 3), w));
            Save("fact_targets", new IconPainter(32).Fill(Circle(10, 20, 5), w).Fill(Circle(22, 20, 5), w).Fill(Circle(16, 9, 5), w));
            Save("fact_cleanse", new IconPainter(32).Fill(Circle(16, 14, 9), new Color(0.6f, 0.9f, 1f)).Fill(Polygon(new Vector2(9, 18), new Vector2(23, 18), new Vector2(16, 29)), new Color(0.6f, 0.9f, 1f)));
            Save("fact_field", new IconPainter(32).Fill(Ring(16, 16, 12, 4), new Color(1f, 0.6f, 0.2f)));
            Save("fact_finisher", new IconPainter(32).Fill(Polygon(RegularPolygon(16, 16, 13, 4, 45f)), new Color(1f, 0.85f, 0.5f)));
        }
    }
}
