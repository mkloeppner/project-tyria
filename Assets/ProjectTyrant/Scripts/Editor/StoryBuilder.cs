using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6: baut die Story-Daten aus der Text-Tabelle (CSV unter Data/Story/Source): Figuren, Zeilen, Szenen (mit Wahl-Stellen
    /// und Varianten), Kapitel (StoryBuilder.Chapters), Briefe, Hinweise, Kulissen, Story-Figuren; exportiert TEXT_TABLE.csv und
    /// Kapitel-Lesefassungen nach Temp/PT_QA. Menü „Rebuild Story“.
    /// </summary>
    public static partial class StoryBuilder
    {
        public const string Root = "Assets/ProjectTyrant/Data/Story";
        public const string SourceDir = Root + "/Source";
        public const string DbPath = Root + "/StoryDatabase.asset";
        public const string SceneDir = Root + "/Scenes";
        public const string ChapterDir = Root + "/Chapters";
        public const string BackdropDir = Root + "/Backdrops";
        public static readonly List<string> Warnings = new List<string>();
        static readonly string[] Forbidden = { "Tyria", "Rytlock", "Logan", "Caithe", "Zojja", "Eir", "Trahearne", "Zhaitan", "Mordremoth", "Kralkatorrik", "Divinity", "Lion's Arch", "Löwenstein", "Charr", "Asura", "Sylvari", "Norn", "Destiny's Edge", "Klinge des Schicksals", "Abaddon", "Balthazar", "Kormir", "Grenth", "Dwayna", "Melandru", "Lyssa" };
        public static IReadOnlyList<string> ForbiddenNames => Forbidden;

        public static StoryDatabase Load() => AssetDatabase.LoadAssetAtPath<StoryDatabase>(DbPath);

        [MenuItem("Project Tyrant/Rebuild Story")]
        public static void BuildMenu() { BuildAll(true); }

        public static StoryDatabase BuildAll(bool voice)
        {
            Warnings.Clear();
            PTEditorUtil.EnsureFolder(Root); PTEditorUtil.EnsureFolder(SceneDir); PTEditorUtil.EnsureFolder(ChapterDir); PTEditorUtil.EnsureFolder(BackdropDir);
            var db = GetOrCreate<StoryDatabase>(DbPath);
            db.Text = GetOrCreate<StoryTextTable>(Root + "/StoryText.asset");
            db.Cast = GetOrCreate<StoryCast>(Root + "/StoryCast.asset");
            BuildCast(db.Cast);
            var rows = ReadLines(db.Text);
            BuildScenes(db, rows);
            db.Letters = ReadLetters(); db.Hints = ReadHints();
            db.Bundles = BuildBundles();
            BuildChapters(db);
            db.Archetypes = AssetDatabase.FindAssets("t:EnemyArchetype", new[] { "Assets/ProjectTyrant/Data" }).Select(g => AssetDatabase.LoadAssetAtPath<EnemyArchetype>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).ToList();
            db.Figures = StoryFigureBuilder.BuildAll(db.Cast);
            db.Backdrops = BuildBackdrops();
            EditorUtility.SetDirty(db); EditorUtility.SetDirty(db.Text); EditorUtility.SetDirty(db.Cast);
            AssetDatabase.SaveAssets();
            if (voice) VoiceBuilder.Build(db);
            Export(db);
            Validate(db);
            Debug.Log("[ProjectTyrant] Geschichte: " + db.Chapters.Count + " Kapitel · " + db.Chapters.Sum(c => c.Steps.Count) + " Schritte · " + db.Text.Lines.Count + " Zeilen · " + db.Scenes.Count + " Szenen · " + db.Letters.Count + " Briefe · " + db.Hints.Count + " Hinweise · " + Warnings.Count + " Warnungen");
            foreach (var w in Warnings.Take(20)) Debug.LogWarning("[ProjectTyrant] Story: " + w);
            return db;
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        // ------------------------------------------------------------ CSV
        public static List<string[]> Csv(string file, int cols)
        {
            var list = new List<string[]>();
            string path = SourceDir + "/" + file; if (!File.Exists(path)) return list;
            bool head = true;
            foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (head) { head = false; continue; }
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                var p = raw.Split(new[] { ';' }, cols);
                if (p.Length < cols) Array.Resize(ref p, cols);
                for (int i = 0; i < p.Length; i++) p[i] = (p[i] ?? "").Trim();
                list.Add(p);
            }
            return list;
        }

        static Color Hex(string h, Color d) => ColorUtility.TryParseHtmlString(h, out var c) ? c : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : d;

        static void BuildCast(StoryCast cast)
        {
            var list = new List<SpeakerProfile>();
            foreach (var p in Csv("cast.csv", 15))
                list.Add(new SpeakerProfile
                {
                    Id = p[0], DisplayName = p[1], Color = Hex(p[2], Color.white), Pitch = F(p[3], 1f), Rate = F(p[4], 1f), Reverb = F(p[5], 0.1f), Formant = F(p[6], 0f),
                    Female = p[7] == "1", Doubled = p[8] == "1", UnlockChapter = (int)F(p[9], 1), Look = p[10], Cloth = Hex(p[11], Color.gray), Trim = Hex(p[12], Color.white), Role = p[13], Bio = p[14]
                });
            cast.Speakers = list;
        }

        public class Row { public string Id, Speaker, Emotion, Camera, Text, Extra, File; }

        public static Emotion ParseEmotion(string e)
        {
            switch ((e ?? "").ToLowerInvariant()) { case "entschlossen": return Emotion.Entschlossen; case "besorgt": return Emotion.Besorgt; case "amuesiert": case "amüsiert": return Emotion.Amuesiert; case "zornig": return Emotion.Zornig; case "trauernd": return Emotion.Trauernd; }
            return Emotion.Neutral;
        }

        static List<Row> ReadLines(StoryTextTable table)
        {
            var keep = new Dictionary<string, StoryLine>(); foreach (var l in table.Lines) if (l != null && l.Id != null) keep[l.Id] = l;
            var rows = new List<Row>(); var lines = new List<StoryLine>(); var seen = new HashSet<string>();
            foreach (var f in Directory.GetFiles(SourceDir, "lines_*.csv").OrderBy(x => x))
            {
                string name = Path.GetFileName(f);
                foreach (var p in Csv(name, 6))
                {
                    if (string.IsNullOrEmpty(p[0])) continue;
                    if (!seen.Add(p[0])) Warnings.Add("Doppelte Zeilen-Id " + p[0]);
                    var r = new Row { Id = p[0], Speaker = p[1], Emotion = p[2], Camera = p[3], Text = p[4], Extra = p[5], File = name };
                    rows.Add(r);
                    keep.TryGetValue(r.Id, out var old);
                    var l = new StoryLine { Id = r.Id, Speaker = r.Speaker, Emotion = ParseEmotion(r.Emotion), Camera = r.Camera, Text = r.Text };
                    if (old != null && old.Text == l.Text && old.Speaker == l.Speaker) { l.Voice = old.Voice; l.VoiceB = old.VoiceB; l.Envelope = old.Envelope; l.Duration = old.Duration; l.Hash = old.Hash; l.VoiceSource = old.VoiceSource; }
                    else if (old != null) { l.Voice = old.Voice; l.VoiceB = old.VoiceB; l.Hash = old.Hash; }   // Text geändert: Hash weicht ab → Pipeline erzeugt neu
                    lines.Add(l);
                }
            }
            table.Lines = lines; table.Invalidate();
            return rows;
        }

        static string SceneOf(string id)
        {
            int at = id.IndexOf('@'); if (at >= 0) id = id.Substring(0, at);
            int k = id.LastIndexOf('_'); if (k < 0) return id;
            string last = id.Substring(k + 1);
            if (last.StartsWith("L") || last.StartsWith("R")) return id.Substring(0, k);
            if ((last == "A" || last == "B" || last == "C" || last == "D") && id.Substring(0, k).Contains("_C")) { var b = id.Substring(0, k); return b.Substring(0, b.LastIndexOf('_')); }
            return id.Substring(0, k);
        }

        static Dictionary<string, string> ExtraMap(string extra)
        {
            var m = new Dictionary<string, string>();
            foreach (var part in (extra ?? "").Split('|'))
            {
                var t = part.Trim(); if (t.Length == 0) continue;
                int c = t.IndexOf(':'); if (c < 0) m[t] = ""; else m[t.Substring(0, c)] = t.Substring(c + 1);
            }
            return m;
        }

        static StoryCondition Cond(string expr)
        {
            var c = new StoryCondition();
            foreach (var part in (expr ?? "").Split(','))
            {
                var kv = part.Split('='); if (kv.Length != 2) continue;
                string k = kv[0].Trim(), v = kv[1].Trim();
                if (k == "Origin") c.Origin = (Origin)Enum.Parse(typeof(Origin), v);
                else if (k == "Pact") c.Pact = (Pact)Enum.Parse(typeof(Pact), v);
                else if (k == "Trait") c.Trait = (Trait)Enum.Parse(typeof(Trait), v);
                else if (k == "Lodestar") c.Lodestar = (Lodestar)Enum.Parse(typeof(Lodestar), v);
                else if (k == "Flag") c.Flag = v; else if (k == "NotFlag") c.NotFlag = v;
            }
            return c;
        }

        static void BuildScenes(StoryDatabase db, List<Row> rows)
        {
            var defs = new Dictionary<string, string[]>();
            foreach (var p in Csv("scenes.csv", 7)) defs[p[0]] = p;
            var byScene = new Dictionary<string, List<Row>>();
            foreach (var r in rows)
            {
                var ex = ExtraMap(r.Extra);
                if (ex.ContainsKey("journal") || ex.ContainsKey("letter") || ex.ContainsKey("scout") || ex.ContainsKey("heart")) { if (!ex.ContainsKey("scene")) continue; }
                string sid = ex.ContainsKey("scene") ? ex["scene"] : SceneOf(r.Id);
                if (r.Id.Contains("@")) continue;   // Text-Varianten werden über die Grundzeile aufgelöst
                if (!byScene.TryGetValue(sid, out var l)) byScene[sid] = l = new List<Row>();
                l.Add(r);
            }
            var scenes = new List<DialogueScript>();
            foreach (var kv in byScene)
            {
                string path = SceneDir + "/Scene_" + kv.Key + ".asset";
                var sc = GetOrCreate<DialogueScript>(path);
                sc.Id = kv.Key; sc.Entries = new List<DialogueEntry>(); sc.Choices = new List<DialogueChoice>();
                defs.TryGetValue(kv.Key, out var d);
                if (d == null) { Warnings.Add("Szene ohne Eintrag in scenes.csv: " + kv.Key + " (Sprechblase angenommen)"); d = new[] { kv.Key, "Bubble", "", "", "", "", "" }; }
                sc.Form = d[1] == "Conversation" ? SceneForm.Conversation : d[1] == "Cinematic" ? SceneForm.Cinematic : SceneForm.Bubble;
                sc.Left = d[2]; sc.Right = d[3]; sc.Backdrop = d[4]; sc.Music = d[5]; sc.SkipAllowed = true;
                var choiceIdx = new Dictionary<int, int>();
                DialogueEntry last = null;
                foreach (var r in kv.Value)
                {
                    var ex = ExtraMap(r.Extra);
                    if (ex.ContainsKey("reply") && !ex.ContainsKey("opt")) continue;   // reine Gegenrede-Zeile (wird über die Option abgespielt)
                    if (ex.TryGetValue("opt", out var optN))
                    {
                        int n = int.Parse(optN);
                        if (!choiceIdx.TryGetValue(n, out int ci)) { ci = sc.Choices.Count; choiceIdx[n] = ci; sc.Choices.Add(new DialogueChoice()); if (last != null) last.ChoiceIndex = ci; else Warnings.Add(kv.Key + ": Wahl ohne vorherige Zeile"); }
                        var o = new DialogueOption { Line = r.Id, Reply = ex.ContainsKey("reply") ? ex["reply"] : null };
                        if (ex.TryGetValue("tend", out var t)) o.Tendency = (Trait)Enum.Parse(typeof(Trait), t);
                        if (ex.TryGetValue("flag", out var f)) o.Flag = f;
                        if (ex.TryGetValue("pact", out var pa)) o.SetPact = (Pact)Enum.Parse(typeof(Pact), pa);
                        if (ex.TryGetValue("reply", out var rp)) o.Reply = rp;
                        sc.Choices[ci].Options.Add(o);
                        continue;
                    }
                    var e = new DialogueEntry { Line = r.Id, CameraCue = r.Camera, Condition = ex.ContainsKey("if") ? Cond(ex["if"]) : new StoryCondition() };
                    if (ex.TryGetValue("wait", out var w)) e.Wait = F(w, 0f);
                    if (ex.TryGetValue("anim", out var an)) e.AnimCue = an;
                    if (ex.TryGetValue("vfx", out var vf)) e.VfxCue = vf;
                    if (ex.TryGetValue("sfx", out var sf)) e.SfxCue = sf;
                    sc.Entries.Add(e); last = e;
                }
                if (sc.Form == SceneForm.Cinematic)
                {
                    var cut = GetOrCreate<CutsceneDefinition>(SceneDir + "/Cut_" + kv.Key + ".asset");
                    cut.Id = kv.Key; cut.Zone = sc.Backdrop; cut.Music = sc.Music; cut.Actors = new List<CutsceneActor>(); cut.Markers = new List<string>(); cut.Camera = new List<CameraKey>();
                    foreach (var a in (d[6] ?? "").Split('|'))
                    {
                        if (string.IsNullOrWhiteSpace(a)) continue;
                        var at = a.Split('@'); if (at.Length != 2) continue;
                        var pc = at[1].Split('/');
                        cut.Actors.Add(new CutsceneActor { Speaker = at[0], Marker = "m_" + at[0], Clip = pc.Length > 1 ? pc[1] : "" });
                        cut.Markers.Add("m_" + at[0] + "=" + pc[0]);
                    }
                    EditorUtility.SetDirty(cut); sc.Cutscene = cut;
                }
                EditorUtility.SetDirty(sc); scenes.Add(sc);
            }
            db.Scenes = scenes;
        }

        static List<LetterDef> ReadLetters()
        {
            var list = new List<LetterDef>();
            foreach (var p in Csv("letters.csv", 7)) list.Add(new LetterDef { Id = p[0], From = p[1], Subject = p[2], Body = p[3].Replace("\\n", "\n"), Karma = (int)F(p[4], 0), Copper = (int)F(p[5], 0), Item = p[6] });
            return list;
        }

        static List<HintDef> ReadHints()
        {
            var list = new List<HintDef>();
            foreach (var p in Csv("hints.csv", 4)) list.Add(new HintDef { Id = p[0], Title = p[1], Text = p[2], Key = p[3] });
            return list;
        }

        // ------------------------------------------------------------ Kulissen
        /// <summary>Gemalte Kulisse je Zone: Ladebild der Zone, weichgezeichnet und in der Sättigung angehoben (Toon-„Malerei“).</summary>
        static List<Texture2D> BuildBackdrops()
        {
            var list = new List<Texture2D>();
            foreach (var src in Directory.GetFiles(LoadingImageBuilder.Dir, "Loading_*.png"))
            {
                string zone = Path.GetFileNameWithoutExtension(src).Substring("Loading_".Length);
                string dst = BackdropDir + "/Backdrop_" + zone + ".png";
                if (!File.Exists(dst) || File.GetLastWriteTimeUtc(dst) < File.GetLastWriteTimeUtc(src))
                {
                    var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(src));
                    var small = Downscale(t, 320, 180);
                    var px = small.GetPixels();
                    for (int pass = 0; pass < 2; pass++) px = Blur(px, 320, 180, 2);
                    for (int i = 0; i < px.Length; i++) { Color.RGBToHSV(px[i], out float h, out float s, out float v); px[i] = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.25f), Mathf.Lerp(v, Mathf.Round(v * 6f) / 6f, 0.35f) * 0.92f); }
                    small.SetPixels(px); small.Apply();
                    File.WriteAllBytes(dst, small.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(t); UnityEngine.Object.DestroyImmediate(small);
                    AssetDatabase.ImportAsset(dst);
                }
                var imp = AssetImporter.GetAtPath(dst) as TextureImporter;   // lesbar: Bühne liest den Bodenfarbton aus dem unteren Rand
                if (imp != null && (imp.mipmapEnabled || !imp.isReadable || imp.npotScale != TextureImporterNPOTScale.None)) { imp.mipmapEnabled = false; imp.isReadable = true; imp.npotScale = TextureImporterNPOTScale.None; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst); if (tex != null) list.Add(tex);
            }
            return list;
        }

        static Texture2D Downscale(Texture2D t, int w, int h)
        {
            var o = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) o.SetPixel(x, y, t.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h));
            o.Apply(); return o;
        }

        static Color[] Blur(Color[] px, int w, int h, int r)
        {
            var o = new Color[px.Length];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear; int n = 0;
                    for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) { int xx = Mathf.Clamp(x + dx, 0, w - 1), yy = Mathf.Clamp(y + dy, 0, h - 1); sum += px[yy * w + xx]; n++; }
                    o[y * w + x] = sum / n;
                }
            return o;
        }

        // ------------------------------------------------------------ Export, Prüfung
        static void Export(StoryDatabase db)
        {
            var sb = new StringBuilder("Id;Sprecher;Emotion;Kamera;Text;Woerter;Sprachdatei\n");
            foreach (var l in db.Text.Lines) sb.Append(l.Id).Append(';').Append(l.Speaker).Append(';').Append(l.Emotion).Append(';').Append(l.Camera).Append(';').Append(l.Text).Append(';').Append(l.Words).Append(';').Append(l.Voice != null ? l.Voice.name : "").Append('\n');
            File.WriteAllText("Assets/ProjectTyrant/Docs/TEXT_TABLE.csv", sb.ToString(), new UTF8Encoding(true));
            Directory.CreateDirectory("Temp/PT_QA");
            foreach (var ch in db.Chapters)
            {
                var md = new StringBuilder("# Kapitel " + ch.Number + ": " + ch.Title + "\n\n");
                string prefix = "K" + ch.Number.ToString("00") + "_";
                foreach (var st in ch.Steps)
                {
                    md.Append("## " + st.Id + " " + st.Title + " (" + st.Type + ", " + st.Zone + ")\n" + st.Description + "\n\n");
                    foreach (var o in st.Objectives) md.Append("- Ziel: " + o.Text + (o.Count > 1 ? " ×" + o.Count : "") + "\n");
                }
                md.Append("\n## Zeilen\n");
                foreach (var l in db.Text.Lines) if (l.Id.StartsWith(prefix)) md.Append("**" + l.Id + "** " + db.Cast.Name(l.Speaker) + " (" + l.Emotion + "): " + l.Text + "\n\n");
                File.WriteAllText("Temp/PT_QA/story_K" + ch.Number.ToString("00") + ".md", md.ToString(), Encoding.UTF8);
            }
        }

        static void Validate(StoryDatabase db)
        {
            foreach (var l in db.Text.Lines)
            {
                if (l.Words > 25) Warnings.Add(l.Id + ": " + l.Words + " Wörter (> 25)");
                if (db.Cast.Get(l.Speaker) == null) Warnings.Add(l.Id + ": unbekannter Sprecher " + l.Speaker);
                foreach (var f in Forbidden) if (l.Text.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 && !(f.Length <= 4 && !System.Text.RegularExpressions.Regex.IsMatch(l.Text, "\\b" + f + "\\b"))) Warnings.Add(l.Id + ": verbotener Eigenname " + f);
            }
        }
    }
}
