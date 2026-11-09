using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M24: rendert alle Klang-Rezepte (3 Varianten), die Musik-Stems (je Zone Erkundung + Kampf, Boss, Menü) und die Stinger
    /// nach Procedural/Audio/, analysiert jede WAV (Spitze, RMS, DC, Naht, Schwerpunkt) und baut Data/Audio/SoundBank.asset.
    /// Rendern läuft parallel (reines C#); unveränderte Dateien werden nicht neu geschrieben (kein Re-Import).
    /// </summary>
    public static class AudioBuilder
    {
        public const string Dir = PTEditorUtil.ProcDir + "/Audio";
        public const string BankPath = PTEditorUtil.DataDir + "/Audio/SoundBank.asset";
        public const string ReportPath = PTEditorUtil.DataDir + "/Audio/SoundReport.txt";
        public static SoundBank Bank => AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
        public static readonly List<string> LastReport = new List<string>();
        public static int Written, Unchanged;

        class Job { public string Path; public float[] Data; public ClipStats Stats; public bool Stream, Loop; }

        [MenuItem("Project Tyrant/Lauf 5/Audio erzeugen")]
        public static void BuildAll()
        {
            PTEditorUtil.EnsureFolder(Dir); PTEditorUtil.EnsureFolder(PTEditorUtil.DataDir + "/Audio");
            foreach (var sub in new[] { "Skill", "Combat", "Move", "Vox", "World", "Amb", "UI", "Music", "Stinger" }) PTEditorUtil.EnsureFolder(Dir + "/" + sub);
            Written = Unchanged = 0; LastReport.Clear();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ---------------------------------------------- Klänge
            var recipes = SoundRecipes.All;
            var jobs = new List<(SoundRecipe r, int v, Job j)>();
            foreach (var r in recipes) for (int v = 0; v < r.Variants; v++) jobs.Add((r, v, new Job { Path = ClipPath(r.Id, v), Loop = r.Loop }));
            Parallel.ForEach(jobs, x => { x.j.Data = SoundRecipes.Render(x.r, x.v); x.j.Stats = AudioAnalysis.Analyze(x.j.Data); });

            // ---------------------------------------------- Musik
            var sets = new List<(string key, Song song)>();
            foreach (var z in SoundRecipes.Zones) { var zm = ZoneMusic.For(z); sets.Add((z + "/Explore", MusicComposer.Compose(zm, MusicKind.Explore))); sets.Add((z + "/Combat", MusicComposer.Compose(zm, MusicKind.Combat))); }
            sets.Add(("boss/Boss", MusicComposer.Compose(ZoneMusic.For("boss"), MusicKind.Boss)));
            sets.Add(("menu/Menu", MusicComposer.Compose(ZoneMusic.For("menu"), MusicKind.Menu)));
            var stemJobs = new Job[sets.Count][];
            Parallel.For(0, sets.Count, i =>
            {
                var stems = MusicComposer.Render(sets[i].song); MusicComposer.NormalizeStems(stems, -12f);
                stemJobs[i] = new Job[5];
                for (int s = 0; s < 5; s++) stemJobs[i][s] = new Job { Path = Dir + "/Music/" + sets[i].key.Replace('/', '_') + "_" + (Stem)s + ".wav", Data = stems[s], Stats = AudioAnalysis.Analyze(stems[s]), Stream = true, Loop = true };
            });
            var stingerJobs = new List<(string key, Job j)>();
            foreach (var z in SoundRecipes.Zones) foreach (StingerKind k in Enum.GetValues(typeof(StingerKind))) stingerJobs.Add((z + "/" + k, new Job { Path = Dir + "/Stinger/" + z + "_" + k + ".wav" }));
            Parallel.ForEach(stingerJobs, x => { var parts = x.key.Split('/'); x.j.Data = MusicComposer.Stinger(ZoneMusic.For(parts[0]), (StingerKind)Enum.Parse(typeof(StingerKind), parts[1])); x.j.Stats = AudioAnalysis.Analyze(x.j.Data); });
            double render = sw.Elapsed.TotalSeconds;

            // ---------------------------------------------- Schreiben + Import
            var all = new List<Job>(); foreach (var x in jobs) all.Add(x.j); foreach (var s in stemJobs) all.AddRange(s); foreach (var x in stingerJobs) all.Add(x.j);
            var changed = new List<Job>();
            foreach (var j in all) if (WriteIfChanged(j)) changed.Add(j);
            AssetDatabase.StartAssetEditing();
            try { foreach (var j in changed) AssetDatabase.ImportAsset(j.Path, ImportAssetOptions.ForceSynchronousImport); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.StartAssetEditing();
            try { foreach (var j in all) Configure(j); }
            finally { AssetDatabase.StopAssetEditing(); }

            // ---------------------------------------------- Bank
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
            bank.Entries.Clear(); bank.Music.Clear(); bank.Stingers.Clear();
            int ji = 0;
            foreach (var r in recipes)
            {
                var spec = SoundCategories.Spec(r.Category);
                var e = new SoundBank.Entry { Id = r.Id, Category = r.Category, Group = spec.Group, Loop = r.Loop, Spatial = r.Spatial, VolumeDb = r.VolumeDb, Priority = spec.Priority, Clips = new AudioClip[r.Variants], Stats = new ClipStats[r.Variants] };
                for (int v = 0; v < r.Variants; v++, ji++) { e.Clips[v] = AssetDatabase.LoadAssetAtPath<AudioClip>(jobs[ji].j.Path); e.Stats[v] = jobs[ji].j.Stats; }
                bank.Entries.Add(e);
            }
            for (int i = 0; i < sets.Count; i++)
            {
                var m = new SoundBank.MusicSet { Key = sets[i].key, Bpm = sets[i].song.Bpm, Bars = sets[i].song.Bars };
                for (int s = 0; s < 5; s++) { m.Stems[s] = AssetDatabase.LoadAssetAtPath<AudioClip>(stemJobs[i][s].Path); m.Stats[s] = stemJobs[i][s].Stats; }
                bank.Music.Add(m);
            }
            foreach (var x in stingerJobs) bank.Stingers.Add(new SoundBank.StingerEntry { Key = x.key, Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(x.j.Path), Stats = x.j.Stats });
            bank.ClearCache();
            EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();

            // ---------------------------------------------- Bericht (für AUDIO.md)
            var rep = new System.Text.StringBuilder();
            rep.AppendLine("# Klang-Analyse (AudioBuilder) – " + all.Count + " WAV, Rendern " + render.ToString("0.0") + " s, gesamt " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
            for (int i = 0; i < jobs.Count; i++) rep.AppendLine(Path.GetFileNameWithoutExtension(jobs[i].j.Path) + "\t" + jobs[i].r.Category + "\t" + jobs[i].j.Stats);
            foreach (var s in stemJobs) foreach (var j in s) rep.AppendLine(Path.GetFileNameWithoutExtension(j.Path) + "\tMusik\t" + j.Stats);
            foreach (var x in stingerJobs) rep.AppendLine(Path.GetFileNameWithoutExtension(x.j.Path) + "\tStinger\t" + x.j.Stats);
            File.WriteAllText(ReportPath, rep.ToString());
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log("[ProjectTyrant] Audio: " + bank.Entries.Count + " Klänge (" + jobs.Count + " WAV), " + bank.Music.Count + " Musik-Sätze, " + bank.Stingers.Count + " Stinger · geschrieben " + Written + ", unverändert " + Unchanged + " · Rendern " + render.ToString("0.0") + " s, gesamt " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
        }

        public static string ClipPath(string id, int variant)
        {
            string area = id.Contains("/") ? id.Substring(0, id.IndexOf('/')) : "Misc";
            string sub;
            switch (area)
            {
                case "Hit": case "Combat": case "Enemy": case "Boss": case "Swish": sub = "Combat"; break;
                case "Step": case "Move": case "Mount": sub = "Move"; break;
                case "Vox": sub = "Vox"; break;
                case "Effect": case "Loot": case "Equip": case "World": case "Event": case "Prog": sub = "World"; break;
                case "Amb": sub = "Amb"; break;
                case "UI": sub = "UI"; break;
                default: sub = "Skill"; break;
            }
            return Dir + "/" + sub + "/SFX_" + id.Replace('/', '_') + "_" + variant + ".wav";
        }

        static bool WriteIfChanged(Job j)
        {
            Directory.CreateDirectory("Temp/PT_AudioTmp");
            var tmp = "Temp/PT_AudioTmp/" + Path.GetFileName(j.Path) + ".tmp";
            WavIO.Write(tmp, j.Data);
            if (File.Exists(j.Path))
            {
                var a = File.ReadAllBytes(tmp); var b = File.ReadAllBytes(j.Path);
                if (a.Length == b.Length && System.Linq.Enumerable.SequenceEqual(a, b)) { File.Delete(tmp); Unchanged++; return false; }
                File.Delete(j.Path);
            }
            File.Move(tmp, j.Path); Written++;
            return true;
        }

        static void Configure(Job j)
        {
            var imp = AssetImporter.GetAtPath(j.Path) as AudioImporter; if (imp == null) return;
            var s = imp.defaultSampleSettings; bool dirty = false;
            AudioClipLoadType lt = j.Stream ? AudioClipLoadType.Streaming : j.Loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            AudioCompressionFormat cf = j.Stream || j.Loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            float q = j.Stream ? 0.55f : 0.7f;
            if (s.loadType != lt || s.compressionFormat != cf || Math.Abs(s.quality - q) > 0.01f) { s.loadType = lt; s.compressionFormat = cf; s.quality = q; imp.defaultSampleSettings = s; dirty = true; }
            if (!imp.forceToMono) { imp.forceToMono = true; dirty = true; }
            if (imp.loadInBackground != j.Stream) { imp.loadInBackground = j.Stream; dirty = true; }
            if (dirty) imp.SaveAndReimport();
        }
    }
}
