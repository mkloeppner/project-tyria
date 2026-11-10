using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6: Sprach-Pipeline. (a) Windows-Sprachsynthese (System.Speech über PowerShell, deutsche Stimme bevorzugt) →
    /// Nachbearbeitung je Figur (Tonhöhe per Granular-Verschiebung bei gleicher Dauer, Formant-Andeutung über Kuhschwanz-Filter,
    /// Doppelstimme, Hall, Kompressor, Pegel); (c) Stimm-Laute (Silben-Synthese) als Fallback ohne SAPI.
    /// Neu erzeugt wird nur bei geänderter Zeile (Hash je Zeile). Hüllkurve (RMS je 50 ms) für Lippen/Kopf.
    /// </summary>
    public static class VoiceBuilder
    {
        public const string Dir = "Assets/ProjectTyrant/Procedural/Voice";
        public const string RawDir = "Temp/PT_Voice";
        public const int Rate = 22050;
        public const string Version = "v2";   // v2: WSOLA-Tonhöhenverschiebung
        public static string PreferredVoice = "Microsoft Hedda Desktop";
        public static readonly List<string> Report = new List<string>();
        public static int Generated, Reused;

        [MenuItem("Project Tyrant/Rebuild Voice")]
        public static void BuildMenu() { StoryBuilder.Load(); Build(StoryBuilder.Load()); }

        public static void Build(StoryDatabase db)
        {
            if (db == null || db.Text == null || db.Cast == null) return;
            Report.Clear(); Generated = Reused = 0;
            Directory.CreateDirectory(Dir); Directory.CreateDirectory(RawDir);
            var jobs = new List<(StoryLine line, SpeakerProfile prof, bool variantB, string hash, string path)>();
            foreach (var l in db.Text.Lines)
            {
                if (l == null || string.IsNullOrEmpty(l.Text) || l.Id.EndsWith("_J") || l.Id.Contains("_J@")) continue;
                var prof = db.Cast.Get(l.Speaker) ?? db.Cast.Get("held");
                Add(jobs, l, prof, false);
                if (l.Speaker == "held") { var pb = MaleHero(prof); Add(jobs, l, pb, true); }
            }
            var pending = jobs.FindAll(j => !File.Exists(j.path) || (j.variantB ? (j.line.Hash ?? "").Split('|').Length < 2 || (j.line.Hash ?? "").Split('|')[1] != j.hash : (j.line.Hash ?? "").Split('|')[0] != j.hash));
            bool sapi = pending.Count > 0 && RunSapi(pending);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var j in pending)
                {
                    float[] s;
                    string raw = RawPath(j.path);
                    if (sapi && File.Exists(raw)) { s = WavIO.Read(raw, out int r); if (r != Rate) s = Resample(s, r, Rate); j.line.VoiceSource = "SAPI"; }
                    else { s = Syllables(j.line.Text, j.prof); j.line.VoiceSource = "Laute"; }
                    s = Post(s, j.prof);
                    WriteWav(j.path, s);
                    Generated++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            foreach (var j in jobs)
            {
                var imp = AssetImporter.GetAtPath(j.path) as AudioImporter;
                if (imp != null && (imp.forceToMono == false || imp.defaultSampleSettings.compressionFormat != AudioCompressionFormat.Vorbis))
                {
                    imp.forceToMono = true; var st = imp.defaultSampleSettings; st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = 0.55f; st.loadType = AudioClipLoadType.CompressedInMemory; imp.defaultSampleSettings = st; imp.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(j.path);
                var parts = (j.line.Hash ?? "").Split('|'); string a = parts.Length > 0 ? parts[0] : "", b = parts.Length > 1 ? parts[1] : "";
                if (j.variantB) { j.line.VoiceB = clip; b = j.hash; }
                else
                {
                    j.line.Voice = clip; a = j.hash;
                    var s = File.Exists(j.path) ? WavIO.Read(j.path, out _) : new float[0];
                    j.line.Envelope = Envelope(s, Rate); j.line.Duration = s.Length / (float)Rate;
                    Analyze(j.line, j.prof, s);
                }
                j.line.Hash = a + "|" + b;
            }
            Reused = jobs.Count - Generated;
            EditorUtility.SetDirty(db.Text); AssetDatabase.SaveAssets();
            File.WriteAllLines("Assets/ProjectTyrant/Data/Story/VoiceReport.txt", Report);
            Debug.Log("[ProjectTyrant] Sprache: " + jobs.Count + " Dateien (" + Generated + " neu, " + Reused + " unverändert), Quelle " + (sapi ? "Windows-Sprachsynthese (" + PreferredVoice + ")" : Generated > 0 ? "Stimm-Laute" : "Cache"));
        }

        static void Add(List<(StoryLine, SpeakerProfile, bool, string, string)> jobs, StoryLine l, SpeakerProfile p, bool b)
        {
            string hash = Hash(Version + "|" + l.Text + "|" + p.Pitch + "|" + p.Rate + "|" + p.Reverb + "|" + p.Formant + "|" + p.Doubled + "|" + b);
            jobs.Add((l, p, b, hash, Dir + "/" + Safe(l.Id) + (b ? "_B" : "") + ".wav"));
        }

        static SpeakerProfile MaleHero(SpeakerProfile p) => new SpeakerProfile { Id = "held_b", Pitch = 0.72f, Rate = p.Rate, Reverb = p.Reverb, Formant = -0.4f, Female = false };
        static string Safe(string id) => id.Replace("@", "_v_");
        static string RawPath(string path) => RawDir + "/" + Path.GetFileName(path);
        static string Hash(string s) { using (var md5 = MD5.Create()) { var h = md5.ComputeHash(Encoding.UTF8.GetBytes(s)); return BitConverter.ToString(h, 0, 6).Replace("-", ""); } }

        // ------------------------------------------------------------ (a) SAPI
        static bool RunSapi(List<(StoryLine line, SpeakerProfile prof, bool variantB, string hash, string path)> jobs)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Add-Type -AssemblyName System.Speech");
                sb.AppendLine("$s = New-Object System.Speech.Synthesis.SpeechSynthesizer");
                sb.AppendLine("$v = $s.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Name -eq '" + PreferredVoice + "' }");
                sb.AppendLine("if ($v) { $s.SelectVoice('" + PreferredVoice + "') } else { $de = $s.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -like 'de*' } | Select-Object -First 1; if ($de) { $s.SelectVoice($de.VoiceInfo.Name) } }");
                sb.AppendLine("$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(" + Rate + ", [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)");
                foreach (var j in jobs)
                {
                    int rate = Mathf.Clamp(Mathf.RoundToInt((j.prof.Rate - 1f) * 10f), -5, 5);
                    string text = j.line.Text.Replace("'", "''").Replace("–", ",").Replace("…", "...");
                    sb.AppendLine("$s.Rate = " + rate + "; $s.SetOutputToWaveFile('" + Path.GetFullPath(RawPath(j.path)).Replace("'", "''") + "', $fmt); $s.Speak('" + text + "')");
                }
                sb.AppendLine("$s.SetOutputToNull(); $s.Dispose()");
                string ps = Path.GetFullPath(RawDir + "/batch.ps1");
                File.WriteAllText(ps, sb.ToString(), new UTF8Encoding(true));
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + ps + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd(); p.WaitForExit(600000);
                    if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[ProjectTyrant] Sprachsynthese meldet: " + err.Substring(0, Mathf.Min(300, err.Length)));
                }
                int ok = 0; foreach (var j in jobs) if (File.Exists(RawPath(j.path))) ok++;
                return ok > 0;
            }
            catch (Exception e) { Debug.LogWarning("[ProjectTyrant] Windows-Sprachsynthese nicht verfügbar (" + e.Message + ") – Stimm-Laute als Fallback"); return false; }
        }

        // ------------------------------------------------------------ Nachbearbeitung
        public static float[] Post(float[] s, SpeakerProfile p)
        {
            s = TrimSilence(s, 0.12f, 1.2f);
            if (Mathf.Abs(p.Pitch - 1f) > 0.01f) s = PitchShift(s, p.Pitch);
            if (p.Doubled) { var low = PitchShift(s, 0.5f); for (int i = 0; i < s.Length; i++) s[i] = 0.6f * s[i] + 0.55f * low[i]; }
            if (p.Formant < -0.05f) Synth.Filter(s, Synth.FilterType.LowPass, 2f * Mathf.Lerp(7000f, 2600f, -p.Formant), 0.7f);
            else if (p.Formant > 0.05f) { var hp = (float[])s.Clone(); Synth.Filter(hp, Synth.FilterType.HighPass, 2f * 1800f, 0.7f); for (int i = 0; i < s.Length; i++) s[i] += hp[i] * 0.5f * p.Formant; }
            Synth.Filter(s, Synth.FilterType.HighPass, 2f * 80f, 0.7f);
            Compress(s, 0.35f, 3f);
            if (p.Reverb > 0.01f) Synth.Reverb(s, 0.55f + p.Reverb * 0.4f, 0.35f, Mathf.Clamp01(p.Reverb));
            Synth.RemoveDc(s);
            Synth.Normalize(s, -18f, -1.5f);
            Synth.Fade(s, 8f, 40f);
            return s;
        }

        /// <summary>
        /// Tonhöhenverschiebung bei gleicher Dauer: erst umtasten (Tonhöhe × factor, Dauer ÷ factor), dann per WSOLA
        /// (Wave-Similarity Overlap-Add, 40-ms-Fenster, Suche ±10 ms nach der ähnlichsten Fortsetzung) zurück auf die Originaldauer.
        /// </summary>
        public static float[] PitchShift(float[] s, float factor)
        {
            if (s.Length < 16 || Mathf.Abs(factor - 1f) < 0.005f) return (float[])s.Clone();
            int n = (int)(s.Length / factor); var r = new float[n];
            for (int i = 0; i < n; i++) { float src = i * factor; int a = (int)src; float f = src - a; r[i] = a + 1 < s.Length ? s[a] * (1f - f) + s[a + 1] * f : (a < s.Length ? s[a] : 0f); }
            return Wsola(r, s.Length);
        }

        public static float[] Wsola(float[] r, int outLen)
        {
            int win = (int)(Rate * 0.04f), hs = win / 2, tol = (int)(Rate * 0.01f);
            var w = new float[win]; for (int i = 0; i < win; i++) w[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (win - 1));
            var o = new float[outLen]; var norm = new float[outLen];
            double ha = hs * (r.Length / (double)outLen); int prev = 0;
            for (int k = 0; k * hs < outLen; k++)
            {
                int outPos = k * hs, nominal = (int)(k * ha), best = nominal;
                if (k > 0)
                {
                    int nat = prev + hs; double bestC = double.NegativeInfinity;
                    for (int d = -tol; d <= tol; d += 2)
                    {
                        int cand = nominal + d; if (cand < 0 || cand + win >= r.Length) continue;
                        double c = 0; for (int i = 0; i < win; i += 4) { int j = nat + i; if (j >= r.Length) break; c += r[cand + i] * r[j]; }
                        if (c > bestC) { bestC = c; best = cand; }
                    }
                }
                best = Mathf.Clamp(best, 0, Mathf.Max(0, r.Length - 1));
                for (int i = 0; i < win && outPos + i < outLen; i++) { int j = best + i; float v = j < r.Length ? r[j] : 0f; o[outPos + i] += v * w[i]; norm[outPos + i] += w[i]; }
                prev = best;
            }
            for (int i = 0; i < outLen; i++) o[i] = norm[i] > 1e-3f ? o[i] / norm[i] : 0f;
            return o;
        }

        static void Compress(float[] s, float threshold, float ratio)
        {
            float env = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float a = Mathf.Abs(s[i]); env = a > env ? env + (a - env) * 0.2f : env + (a - env) * 0.0008f;
                if (env > threshold) s[i] *= (threshold + (env - threshold) / ratio) / env;
            }
        }

        /// <summary>Stille am Anfang/Ende entfernen, innere Pausen auf maxGap begrenzen (Prüfung: keine Stille über 1,5 s).</summary>
        static float[] TrimSilence(float[] s, float edge, float maxGap)
        {
            const float thr = 0.01f; int a = 0, b = s.Length - 1;
            while (a < s.Length && Mathf.Abs(s[a]) < thr) a++;
            while (b > a && Mathf.Abs(s[b]) < thr) b--;
            a = Mathf.Max(0, a - (int)(edge * Rate)); b = Mathf.Min(s.Length - 1, b + (int)(edge * Rate));
            var list = new List<float>(b - a + 1); int quiet = 0, maxQuiet = (int)(maxGap * Rate);
            for (int i = a; i <= b; i++) { if (Mathf.Abs(s[i]) < thr) { quiet++; if (quiet > maxQuiet) continue; } else quiet = 0; list.Add(s[i]); }
            return list.ToArray();
        }

        static float[] Resample(float[] s, int from, int to)
        {
            int n = (int)((long)s.Length * to / from); var o = new float[n];
            for (int i = 0; i < n; i++) { float src = i * (float)from / to; int a = (int)src; float f = src - a; o[i] = a + 1 < s.Length ? s[a] * (1 - f) + s[a + 1] * f : 0f; }
            return o;
        }

        // ------------------------------------------------------------ (c) Stimm-Laute
        public static float[] Syllables(string text, SpeakerProfile p)
        {
            var rng = new System.Random(text.GetHashCode());
            int syl = 0; foreach (char c in text.ToLowerInvariant()) if ("aeiouäöüy".IndexOf(c) >= 0) syl++;
            float secPer = 0.16f / Mathf.Max(0.5f, p.Rate), total = Mathf.Max(0.8f, syl * secPer + 0.3f);
            var s = new float[(int)(total * Rate)]; float baseHz = (p.Female ? 210f : 120f) * p.Pitch;
            for (int k = 0; k < syl; k++)
            {
                int start = (int)((0.15f + k * secPer) * Rate), len = (int)(secPer * 0.8f * Rate);
                float hz = baseHz * (1f + 0.12f * (float)(rng.NextDouble() - 0.5)), form = 600f + 900f * (float)rng.NextDouble();
                for (int i = 0; i < len && start + i < s.Length; i++)
                {
                    float t = i / (float)Rate, env = Mathf.Sin(Mathf.PI * i / len);
                    float v = Mathf.Sin(2f * Mathf.PI * hz * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * hz * 2f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * form * t) * 0.12f;
                    s[start + i] += v * env * 0.4f;
                }
            }
            return s;
        }

        // ------------------------------------------------------------ Analyse, Hüllkurve
        public static float[] Envelope(float[] s, int rate)
        {
            int win = rate / 20; int n = Mathf.Max(1, s.Length / win); var e = new float[n]; float max = 1e-5f;
            for (int k = 0; k < n; k++) { double sum = 0; for (int i = k * win; i < (k + 1) * win && i < s.Length; i++) sum += s[i] * s[i]; e[k] = (float)Math.Sqrt(sum / win); if (e[k] > max) max = e[k]; }
            for (int k = 0; k < n; k++) e[k] = Mathf.Clamp01(e[k] / max);
            return e;
        }

        /// <summary>Tonhöhen-Schätzung (normierte Autokorrelation über stimmhafte Fenster, erster Gipfel ≥ 90 % des Maximums gegen Oktavfehler) in Hz.</summary>
        public static float PitchEstimate(float[] s, int rate)
        {
            int win = rate / 25, minLag = rate / 400, maxLag = rate / 60; var est = new List<float>(); var nc = new double[maxLag + 2];
            for (int start = 0; start + win + maxLag < s.Length; start += win)
            {
                double e0 = 0; for (int i = 0; i < win; i++) e0 += s[start + i] * s[start + i];
                if (e0 / win < 0.002) continue;
                double max = 0;
                for (int lag = minLag; lag <= maxLag; lag++)
                {
                    double c = 0, el = 0; for (int i = 0; i < win; i++) { float b = s[start + i + lag]; c += s[start + i] * b; el += b * b; }
                    nc[lag] = c / Math.Sqrt(e0 * el + 1e-12); if (nc[lag] > max) max = nc[lag];
                }
                if (max < 0.5) continue;
                for (int lag = minLag + 1; lag < maxLag; lag++)
                    if (nc[lag] >= 0.9 * max && nc[lag] >= nc[lag - 1] && nc[lag] >= nc[lag + 1]) { est.Add(rate / (float)lag); break; }
            }
            if (est.Count == 0) return 0f; est.Sort(); return est[est.Count / 2];
        }

        public static float MaxSilence(float[] s, int rate)
        {
            int quiet = 0, maxQ = 0; foreach (var v in s) { if (Mathf.Abs(v) < 0.01f) { quiet++; if (quiet > maxQ) maxQ = quiet; } else quiet = 0; }
            return maxQ / (float)rate;
        }

        static void Analyze(StoryLine l, SpeakerProfile p, float[] s)
        {
            if (s.Length == 0) return;
            var st = AudioAnalysis.Analyze(s, Rate);
            float secs = s.Length / (float)Rate, perWord = secs / Mathf.Max(1, l.Words), pitch = PitchEstimate(s, Rate), sil = MaxSilence(s, Rate);
            Report.Add(l.Id + "\t" + l.Speaker + "\t" + secs.ToString("0.00") + " s\t" + perWord.ToString("0.00") + " s/Wort\tSpitze " + st.PeakDb.ToString("0.0") + "\tRMS " + st.RmsDb.ToString("0.0") + "\tStille " + sil.ToString("0.00") + " s\tTonhöhe " + pitch.ToString("0") + " Hz\t" + l.VoiceSource);
        }

        static void WriteWav(string path, float[] s) { Directory.CreateDirectory(Path.GetDirectoryName(path)); WavIO.Write(path, s, Rate); }
    }
}
