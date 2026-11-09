using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    public enum MusicMode { Ionian, Lydian, Mixolydian, Dorian, Aeolian, Phrygian }
    public enum Stem { Pad, Melody, Bass, Percussion, Accents }
    public enum MusicKind { Explore, Combat, Boss, Menu }
    public enum MelodyVoice { Flute, Pluck, Bell, Reed, Brass }
    public enum StingerKind { Victory, Defeat, LevelUp, Discovery, Mastery, Death, Travel }

    /// <summary>Komponist-Parameter je Zone (Tonart, Modus, Tempo, Akkordfolge, Instrument).</summary>
    [Serializable]
    public class ZoneMusic
    {
        public string ZoneId; public int TonicMidi = 62; public MusicMode Mode = MusicMode.Ionian;
        public float ExploreBpm = 80f, CombatBpm = 128f; public int Seed = 1;
        public int[] Progression = { 0, 3, 4, 0 }; public bool OpenFifths; public MelodyVoice Voice = MelodyVoice.Flute;

        /// <summary>Zonen laut Auftrag: Grasland Dur/Lydisch, Nebelmoor Dorisch, Frostkamm Äolisch mit offenen Quinten, Glutöde Phrygisch.</summary>
        public static ZoneMusic For(string zoneId)
        {
            switch (zoneId)
            {
                case "nebelmoor": return new ZoneMusic { ZoneId = zoneId, TonicMidi = 57, Mode = MusicMode.Dorian, ExploreBpm = 76f, CombatBpm = 126f, Seed = 21, Progression = new[] { 0, 3, 6, 0, 0, 4, 3, 6 }, Voice = MelodyVoice.Pluck };
                case "frostkamm": return new ZoneMusic { ZoneId = zoneId, TonicMidi = 59, Mode = MusicMode.Aeolian, ExploreBpm = 72f, CombatBpm = 132f, Seed = 33, Progression = new[] { 0, 5, 2, 6, 0, 5, 3, 4 }, OpenFifths = true, Voice = MelodyVoice.Bell };
                case "gluetoede": return new ZoneMusic { ZoneId = zoneId, TonicMidi = 52, Mode = MusicMode.Phrygian, ExploreBpm = 84f, CombatBpm = 138f, Seed = 47, Progression = new[] { 0, 1, 0, 6, 0, 1, 5, 6 }, Voice = MelodyVoice.Reed };
                case "menu": return new ZoneMusic { ZoneId = zoneId, TonicMidi = 62, Mode = MusicMode.Ionian, ExploreBpm = 70f, CombatBpm = 70f, Seed = 5, Progression = new[] { 0, 5, 3, 4 }, Voice = MelodyVoice.Flute };
                case "boss": return new ZoneMusic { ZoneId = zoneId, TonicMidi = 50, Mode = MusicMode.Aeolian, ExploreBpm = 140f, CombatBpm = 140f, Seed = 61, Progression = new[] { 0, 5, 6, 4 }, Voice = MelodyVoice.Brass };
                default: return new ZoneMusic { ZoneId = zoneId ?? "sonnfurt", TonicMidi = 62, Mode = MusicMode.Lydian, ExploreBpm = 84f, CombatBpm = 128f, Seed = 11, Progression = new[] { 0, 4, 5, 3, 0, 1, 4, 4 }, Voice = MelodyVoice.Flute };
            }
        }
    }

    public struct NoteEvent
    {
        public Stem Stem; public int Bar; public float Beat, Beats; public int Midi; public float Velocity; public int Drum;
        public float StartBeat => Bar * 4 + Beat;
    }

    public class Song
    {
        public ZoneMusic Def; public MusicKind Kind; public float Bpm; public int Bars; public const int BeatsPerBar = 4;
        public readonly List<NoteEvent> Notes = new List<NoteEvent>();
        public double SecondsPerBeat => 60.0 / Bpm;
        public double BarSeconds => SecondsPerBeat * BeatsPerBar;
        public int LoopSamples => (int)Math.Round(Bars * BarSeconds * Synth.Rate);
    }

    /// <summary>
    /// Lauf 5 / M24: generativer Komponist. Tonart und Modus je Zone, Akkordfolgen, Arpeggien, Hauptthema als wiederkehrendes Motiv
    /// mit Variation (Transposition, Rhythmus, Umkehrung), Tempo je Zustand (Erkundung 70–90, Kampf 120–140 bpm). Rendert fünf
    /// gleich lange, nahtlos schleifende Stems.
    /// </summary>
    public static class MusicComposer
    {
        public const float DrumKick = 0, DrumSnare = 1, DrumHat = 2, DrumTom = 3, DrumShaker = 4, DrumFrame = 5;

        public static int[] Intervals(MusicMode m)
        {
            switch (m)
            {
                case MusicMode.Lydian: return new[] { 0, 2, 4, 6, 7, 9, 11 };
                case MusicMode.Mixolydian: return new[] { 0, 2, 4, 5, 7, 9, 10 };
                case MusicMode.Dorian: return new[] { 0, 2, 3, 5, 7, 9, 10 };
                case MusicMode.Aeolian: return new[] { 0, 2, 3, 5, 7, 8, 10 };
                case MusicMode.Phrygian: return new[] { 0, 1, 3, 5, 7, 8, 10 };
                default: return new[] { 0, 2, 4, 5, 7, 9, 11 };
            }
        }

        /// <summary>Tonleiter-Stufe (beliebig, auch negativ) → MIDI-Note relativ zur Tonika.</summary>
        public static int Degree(ZoneMusic z, int degree, int octaveShift = 0)
        {
            var iv = Intervals(z.Mode); int o = (int)Math.Floor(degree / 7.0); int d = degree - o * 7;
            return z.TonicMidi + iv[d] + 12 * (o + octaveShift);
        }

        public static bool InScale(ZoneMusic z, int midi)
        {
            int pc = ((midi - z.TonicMidi) % 12 + 12) % 12;
            return Array.IndexOf(Intervals(z.Mode), pc) >= 0;
        }

        /// <summary>Hauptthema (Stufen, Dauer in Schlägen) – kehrt in allen Zonen im jeweiligen Modus wieder.</summary>
        public static readonly int[] ThemeDegrees = { 0, 2, 4, 5, 4, 2, 3, 1 };
        public static readonly float[] ThemeBeats = { 1f, 0.5f, 0.5f, 1.5f, 0.5f, 1f, 1f, 2f };

        public static int BarsFor(float bpm, float minSeconds = 60f) => (int)Math.Ceiling(minSeconds / (60.0 / bpm * 4));

        public static Song Compose(ZoneMusic z, MusicKind kind)
        {
            float bpm = kind == MusicKind.Combat ? z.CombatBpm : kind == MusicKind.Boss ? 140f : kind == MusicKind.Menu ? 70f : z.ExploreBpm;
            bool fight = kind == MusicKind.Combat || kind == MusicKind.Boss;
            var s = new Song { Def = z, Kind = kind, Bpm = bpm };
            int bars = BarsFor(bpm); int chordBars = fight ? 1 : 2;
            bars = (int)Math.Ceiling(bars / (double)(4 * chordBars)) * 4 * chordBars;   // ganze Phrasen
            s.Bars = bars;
            var r = new Rng(z.Seed * 97 + (int)kind * 13);
            var prog = z.Progression;
            for (int bar = 0; bar < bars; bar++)
            {
                int chord = prog[(bar / chordBars) % prog.Length];
                // Pad: Dreiklang (oder offene Quinte) über die Akkorddauer
                if (bar % chordBars == 0)
                {
                    int[] tones = z.OpenFifths ? new[] { chord, chord + 4, chord + 7 } : new[] { chord, chord + 2, chord + 4 };
                    foreach (var t in tones) s.Notes.Add(new NoteEvent { Stem = Stem.Pad, Bar = bar, Beat = 0f, Beats = 4f * chordBars, Midi = Degree(z, t, 0), Velocity = 0.55f });
                }
                // Bass
                if (fight)
                {
                    for (int e = 0; e < 8; e++)
                        s.Notes.Add(new NoteEvent { Stem = Stem.Bass, Bar = bar, Beat = e * 0.5f, Beats = 0.45f, Midi = Degree(z, chord, e % 4 == 3 ? -1 : -2), Velocity = e % 2 == 0 ? 0.9f : 0.65f });
                }
                else
                {
                    s.Notes.Add(new NoteEvent { Stem = Stem.Bass, Bar = bar, Beat = 0f, Beats = 2f, Midi = Degree(z, chord, -2), Velocity = 0.8f });
                    s.Notes.Add(new NoteEvent { Stem = Stem.Bass, Bar = bar, Beat = 2f, Beats = 2f, Midi = Degree(z, chord + (bar % 2 == 0 ? 4 : 0), -2), Velocity = 0.65f });
                }
                // Arpeggio (Akzente) in Achteln, Erkundung leise und luftig, Kampf durchgehend
                int[] arp = { chord, chord + 2, chord + 4, chord + 7, chord + 4, chord + 2, chord + 4, chord + 7 };
                for (int e = 0; e < 8; e++)
                {
                    if (!fight && (e % 2 == 1) && r.Value() < 0.5f) continue;
                    s.Notes.Add(new NoteEvent { Stem = Stem.Accents, Bar = bar, Beat = e * 0.5f, Beats = 0.5f, Midi = Degree(z, arp[e], 1), Velocity = fight ? 0.5f : 0.35f });
                }
                if (bar % 4 == 0) s.Notes.Add(new NoteEvent { Stem = Stem.Accents, Bar = bar, Beat = 0f, Beats = 3f, Midi = Degree(z, chord + 7, 1), Velocity = 0.6f, Drum = -1 });
                // Perkussion
                AddDrums(s, bar, fight, bars, r);
            }
            // Melodie: Phrasen à 4 Takte, Form A A' B A'' (Hauptthema, Variation, Kontrast, Rückkehr)
            for (int ph = 0; ph < bars / 4; ph++)
            {
                int startBar = ph * 4; int form = ph % 4; int chord = prog[(startBar / chordBars) % prog.Length];
                if (!fight && form == 2 && r.Value() < 0.35f) continue;   // Erkundung: gelegentlich Atempause
                AddPhrase(s, z, startBar, form, chord, fight, r);
            }
            return s;
        }

        static void AddPhrase(Song s, ZoneMusic z, int startBar, int form, int chord, bool fight, Rng r)
        {
            float beat = 0f; int shift = form == 1 ? 2 : form == 3 ? (chord % 7) : 0;
            int reps = fight ? 2 : 1;   // Kampf: Thema doppelt so dicht
            for (int rep = 0; rep < reps; rep++)
            {
                for (int i = 0; i < ThemeDegrees.Length; i++)
                {
                    int deg = ThemeDegrees[i];
                    if (form == 2) deg = 4 - deg;                  // Kontrast: Umkehrung um die Quinte
                    deg += shift;
                    float len = ThemeBeats[i] * (fight ? 0.5f : 1f) * 2f;
                    if (form == 1 && i % 3 == 1) len *= 0.5f;     // Variation: Rhythmus verdichten
                    if (form == 3 && i == ThemeDegrees.Length - 1) { deg = 0; }   // Rückkehr endet auf der Tonika
                    if (beat + len > 16f) len = 16f - beat;
                    if (len <= 0.05f) break;
                    int bar = startBar + (int)(beat / 4f);
                    s.Notes.Add(new NoteEvent { Stem = Stem.Melody, Bar = bar, Beat = beat - (bar - startBar) * 4f, Beats = len * 0.92f, Midi = Degree(z, deg, 1), Velocity = 0.7f + r.Range(-0.08f, 0.08f) });
                    beat += len;
                }
            }
        }

        static void AddDrums(Song s, int bar, bool fight, int bars, Rng r)
        {
            void D(float beat, float drum, float vel) => s.Notes.Add(new NoteEvent { Stem = Stem.Percussion, Bar = bar, Beat = beat, Beats = 0.25f, Drum = (int)drum, Velocity = vel });
            if (fight)
            {
                D(0f, DrumKick, 1f); D(2f, DrumKick, 0.95f); D(2.5f, DrumKick, 0.7f);
                D(1f, DrumSnare, 0.9f); D(3f, DrumSnare, 0.95f);
                for (int e = 0; e < 8; e++) D(e * 0.5f, DrumHat, e % 2 == 0 ? 0.55f : 0.35f);
                if (bar % 4 == 3) { D(3f, DrumTom, 0.8f); D(3.25f, DrumTom, 0.7f); D(3.5f, DrumTom, 0.75f); D(3.75f, DrumTom, 0.85f); }
            }
            else
            {
                D(0f, DrumFrame, 0.7f); D(2f, DrumFrame, 0.5f);
                for (int e = 0; e < 8; e++) if (e % 2 == 1 || r.Value() < 0.3f) D(e * 0.5f, DrumShaker, 0.3f);
            }
        }

        // ------------------------------------------------------------------ Rendern
        public const float Tail = 3f;

        /// <summary>Rendert alle fünf Stems mit exakt gleicher Länge (= Schleifenlänge); der Nachhall wird an den Anfang gefaltet (nahtlose Schleife).</summary>
        public static float[][] Render(Song s)
        {
            int loop = s.LoopSamples, total = loop + (int)(Tail * Synth.Rate);
            var stems = new float[5][];
            for (int i = 0; i < 5; i++) stems[i] = new float[total];
            var rng = new Rng(s.Def.Seed * 7 + 3);
            var table = Synth.NoiseWavetable(new Rng(s.Def.Seed + 101));
            double spb = s.SecondsPerBeat;
            bool fight = s.Kind == MusicKind.Combat || s.Kind == MusicKind.Boss;
            foreach (var n in s.Notes)
            {
                int start = (int)Math.Round(n.StartBeat * spb * Synth.Rate);
                float dur = (float)(n.Beats * spb);
                float f = Synth.MidiToHz(n.Midi);
                switch (n.Stem)
                {
                    case Stem.Pad:
                    {
                        var env = Adsr.Pad(Math.Min(1.2f, dur * 0.3f), Math.Min(1.5f, dur * 0.35f));
                        for (int v = -1; v <= 1; v++) Synth.AddWavetable(stems[0], start, dur + 0.8f, table, f, 0.12f * n.Velocity, env, v * 9f);
                        break;
                    }
                    case Stem.Melody: RenderMelody(stems[1], start, dur, f, n.Velocity, fight ? (s.Kind == MusicKind.Boss ? MelodyVoice.Brass : MelodyVoice.Reed) : s.Def.Voice, rng); break;
                    case Stem.Bass:
                    {
                        var env = fight ? new Adsr(0.005f, 0.12f, 0.6f, 0.05f) : new Adsr(0.02f, 0.4f, 0.7f, 0.25f);
                        Synth.AddTone(stems[2], start, dur, Synth.Wave.Sine, t => f, 0.5f * n.Velocity, env);
                        Synth.AddTone(stems[2], start, dur, Synth.Wave.Triangle, t => f * 2f, 0.12f * n.Velocity, env);
                        break;
                    }
                    case Stem.Percussion: RenderDrum(stems[3], start, n.Drum, n.Velocity, rng); break;
                    case Stem.Accents:
                        if (n.Drum == -1) Synth.AddFm(stems[4], start, dur, f, 3.5f, 2.2f, 3f, 0.18f * n.Velocity, Adsr.Perc(0.003f, 1.4f));
                        else Synth.AddPluck(stems[4], start, Math.Min(1.4f, dur * 3f), f, 0.35f * n.Velocity, 0.996f, rng);
                        break;
                }
            }
            // Klangformung je Stem
            Synth.Filter(stems[0], Synth.FilterType.LowPass, 2200f, 0.6f); Synth.Chorus(stems[0], 5f, 0.4f, 0.4f); Synth.Reverb(stems[0], 0.9f, 0.4f, 0.35f);
            Synth.Delay(stems[1], (float)(spb * 0.75), 0.3f, 0.22f); Synth.Reverb(stems[1], 0.8f, 0.35f, 0.25f);
            Synth.Filter(stems[2], Synth.FilterType.LowPass, 280f, 0.8f);
            Synth.Reverb(stems[3], 0.5f, 0.5f, 0.12f);
            Synth.Filter(stems[4], Synth.FilterType.HighPass, 300f, 0.7f); Synth.Delay(stems[4], (float)(spb * 0.5), 0.35f, 0.25f); Synth.Reverb(stems[4], 0.85f, 0.35f, 0.3f);
            var outStems = new float[5][];
            for (int i = 0; i < 5; i++) { outStems[i] = Fold(stems[i], loop); Synth.SealLoop(outStems[i]); }
            return outStems;
        }

        /// <summary>Schleife falten: was über das Schleifenende hinausklingt, wird an den Anfang addiert (phasengleiche Enden).</summary>
        public static float[] Fold(float[] buf, int loop)
        {
            var o = new float[loop];
            Array.Copy(buf, o, Math.Min(loop, buf.Length));
            for (int i = loop; i < buf.Length; i++) o[(i - loop) % loop] += buf[i];
            return o;
        }

        static void RenderMelody(float[] buf, int start, float dur, float f, float vel, MelodyVoice voice, Rng rng)
        {
            switch (voice)
            {
                case MelodyVoice.Pluck: Synth.AddPluck(buf, start, dur + 1.2f, f, 0.5f * vel, 0.997f, rng); break;
                case MelodyVoice.Bell: Synth.AddFm(buf, start, dur + 1.5f, f, 2.0f, 2.5f, 2.5f, 0.3f * vel, Adsr.Perc(0.002f, 1.6f)); break;
                case MelodyVoice.Reed:
                {
                    var env = new Adsr(0.03f, 0.15f, 0.75f, 0.12f);
                    var tmp = new float[(int)((dur + 0.2f) * Synth.Rate)];
                    Synth.AddTone(tmp, 0, dur, Synth.Wave.Saw, t => f * (1f + 0.005f * (float)Math.Sin(t * Synth.TwoPi * 5.5f)), 0.22f * vel, env);
                    Synth.Filter(tmp, Synth.FilterType.LowPass, 1600f, 1.2f); Synth.Mix(buf, tmp, start); break;
                }
                case MelodyVoice.Brass:
                {
                    var env = new Adsr(0.04f, 0.2f, 0.8f, 0.1f);
                    var tmp = new float[(int)((dur + 0.2f) * Synth.Rate)];
                    Synth.AddTone(tmp, 0, dur, Synth.Wave.Saw, t => f, 0.2f * vel, env, -5f); Synth.AddTone(tmp, 0, dur, Synth.Wave.Saw, t => f, 0.2f * vel, env, 5f);
                    Synth.Filter(tmp, Synth.FilterType.LowPass, t => 700f + 1800f * (float)Math.Exp(-t * 4f), 1.0f); Synth.Mix(buf, tmp, start); break;
                }
                default:
                {
                    var env = new Adsr(0.06f, 0.2f, 0.8f, 0.15f);
                    Synth.AddTone(buf, start, dur, Synth.Wave.Sine, t => f * (1f + 0.006f * (float)Math.Sin(t * Synth.TwoPi * 5f) * Math.Min(1f, t * 3f)), 0.35f * vel, env);
                    Synth.AddTone(buf, start, dur, Synth.Wave.Triangle, t => f * 2f, 0.05f * vel, env);
                    break;
                }
            }
        }

        static void RenderDrum(float[] buf, int start, int drum, float vel, Rng rng)
        {
            switch (drum)
            {
                case 0: Synth.AddTone(buf, start, 0.35f, Synth.Wave.Sine, t => 45f + 110f * (float)Math.Exp(-t * 30f), 0.9f * vel, Adsr.Perc(0.001f, 0.18f)); break;
                case 1:
                {
                    var tmp = new float[(int)(0.25f * Synth.Rate)];
                    Synth.AddNoise(tmp, 0, 0.25f, 0.6f * vel, Adsr.Perc(0.001f, 0.08f), rng); Synth.Filter(tmp, Synth.FilterType.BandPass, 1800f, 0.8f);
                    Synth.AddTone(tmp, 0, 0.12f, Synth.Wave.Triangle, t => 190f, 0.4f * vel, Adsr.Perc(0.001f, 0.05f)); Synth.Mix(buf, tmp, start); break;
                }
                case 2:
                {
                    var tmp = new float[(int)(0.08f * Synth.Rate)];
                    Synth.AddNoise(tmp, 0, 0.08f, 0.3f * vel, Adsr.Perc(0.001f, 0.025f), rng); Synth.Filter(tmp, Synth.FilterType.HighPass, 7000f, 0.7f); Synth.Mix(buf, tmp, start); break;
                }
                case 3: Synth.AddTone(buf, start, 0.3f, Synth.Wave.Sine, t => 90f + 60f * (float)Math.Exp(-t * 18f), 0.6f * vel, Adsr.Perc(0.001f, 0.14f)); break;
                case 4:
                {
                    var tmp = new float[(int)(0.1f * Synth.Rate)];
                    Synth.AddNoise(tmp, 0, 0.1f, 0.2f * vel, new Adsr(0.015f, 0.04f, 0f, 0.01f), rng); Synth.Filter(tmp, Synth.FilterType.BandPass, 5000f, 1.2f); Synth.Mix(buf, tmp, start); break;
                }
                default:
                {
                    Synth.AddTone(buf, start, 0.4f, Synth.Wave.Sine, t => 70f + 40f * (float)Math.Exp(-t * 12f), 0.55f * vel, Adsr.Perc(0.002f, 0.2f));
                    var tmp = new float[(int)(0.12f * Synth.Rate)];
                    Synth.AddNoise(tmp, 0, 0.12f, 0.15f * vel, Adsr.Perc(0.001f, 0.03f), rng); Synth.Filter(tmp, Synth.FilterType.LowPass, 900f); Synth.Mix(buf, tmp, start); break;
                }
            }
        }

        /// <summary>Gesamtmischung auf Ziel-RMS bringen: gemeinsamer Faktor für alle Stems (Balance bleibt), danach Spitze je Stem ≤ −1 dBFS.</summary>
        public static void NormalizeStems(float[][] stems, float mixRmsDb = -12f)
        {
            int n = stems[0].Length; double sq = 0;
            for (int i = 0; i < n; i++) { float m = 0f; foreach (var s in stems) m += s[i]; sq += m * m; }
            float rms = (float)Math.Sqrt(sq / Math.Max(1, n)); if (rms < 1e-7f) return;
            float g = Synth.DbToLin(mixRmsDb) / rms;
            foreach (var s in stems)
            {
                Synth.RemoveDc(s);
                float lim = Synth.DbToLin(-1f), knee = lim * 0.8f;
                for (int i = 0; i < s.Length; i++)
                {
                    float v = s[i] * g, a = Math.Abs(v);
                    if (a > knee) { a = knee + (lim - knee) * (float)Math.Tanh((a - knee) / (lim - knee)); v = Math.Sign(v) * a; }
                    s[i] = v;
                }
            }
        }

        // ------------------------------------------------------------------ Stinger
        /// <summary>Stinger 2–4 s im Tonart-Kontext des laufenden Themas.</summary>
        public static float[] Stinger(ZoneMusic z, StingerKind k)
        {
            var rng = new Rng(z.Seed * 31 + (int)k);
            float len = k == StingerKind.Death ? 3.8f : k == StingerKind.Travel ? 2.4f : 3.2f;
            var b = Synth.Buffer(len);
            var table = Synth.NoiseWavetable(new Rng(z.Seed + 101));
            int[] degs; float step;
            switch (k)
            {
                case StingerKind.Victory: degs = new[] { 0, 2, 4, 7, 9, 11, 14 }; step = 0.12f; break;
                case StingerKind.Defeat: degs = new[] { 4, 3, 2, 0, -1, -3 }; step = 0.22f; break;
                case StingerKind.LevelUp: degs = new[] { 0, 2, 4, 5, 4, 7 }; step = 0.14f; break;
                case StingerKind.Discovery: degs = new[] { 4, 7, 11 }; step = 0.28f; break;
                case StingerKind.Mastery: degs = new[] { 0, 2, 4, 5, 7 }; step = 0.18f; break;
                case StingerKind.Death: degs = new[] { 2, 0, -2, -3 }; step = 0.45f; break;
                default: degs = new[] { 7, 4, 0 }; step = 0.16f; break;
            }
            for (int i = 0; i < degs.Length; i++)
            {
                int s = (int)(i * step * Synth.Rate); float f = Synth.MidiToHz(Degree(z, degs[i], k == StingerKind.Death ? 0 : 1));
                if (k == StingerKind.Discovery || k == StingerKind.Travel) Synth.AddFm(b, s, 1.8f, f, 3.5f, 2f, 2.5f, 0.3f, Adsr.Perc(0.002f, 1.2f));
                else Synth.AddPluck(b, s, 1.6f, f, 0.45f, 0.997f, rng);
            }
            int chordStart = (int)(degs.Length * step * Synth.Rate);
            int root = k == StingerKind.Defeat || k == StingerKind.Death ? -2 : 0;
            int[] chord = k == StingerKind.Defeat || k == StingerKind.Death ? new[] { root, root + 2, root + 4 } : new[] { 0, 4, 7 };
            foreach (var c in chord) for (int v = -1; v <= 1; v++)
                Synth.AddWavetable(b, Math.Min(chordStart, b.Length - 1), len - chordStart / (float)Synth.Rate, table, Synth.MidiToHz(Degree(z, c, 0)), 0.1f, new Adsr(0.05f, 0.3f, 0.8f, 0.9f), v * 8f);
            if (k == StingerKind.Travel) { var nz = Synth.Buffer(0.8f); Synth.AddNoise(nz, 0, 0.8f, 0.25f, new Adsr(0.3f, 0.2f, 0.3f, 0.3f), rng); Synth.Filter(nz, Synth.FilterType.LowPass, 2500f); Synth.Mix(b, nz); }
            Synth.Reverb(b, 0.85f, 0.3f, 0.3f);
            Synth.Fade(b, 2f, 120f);
            Synth.Normalize(b, -12f, -1f);
            return b;
        }
    }

    /// <summary>Musik-Zustand: Menü, Erkundung, Kampf (ab 3 s Kampf, zurück 8 s nach Kampfende), Boss, Tod.</summary>
    public enum MusicState { Menu, Explore, Combat, Boss, Death }

    public class MusicStateModel
    {
        public const float CombatDelay = 3f, CombatRelease = 8f;
        float _engagedTime, _sinceEngaged = 999f; bool _boss;
        public MusicState State { get; private set; } = MusicState.Explore;
        public float EngagedTime => _engagedTime;

        public MusicState Update(float dt, bool engaged, bool bossEngaged, bool menu, bool dead)
        {
            if (engaged) { _engagedTime += dt; _sinceEngaged = 0f; if (bossEngaged) _boss = true; }
            else { _sinceEngaged += dt; if (_sinceEngaged > CombatRelease) { _engagedTime = 0f; _boss = false; } }
            if (menu) State = MusicState.Menu;
            else if (dead) State = MusicState.Death;
            else if (_engagedTime >= CombatDelay && _sinceEngaged <= CombatRelease) State = _boss ? MusicState.Boss : MusicState.Combat;
            else State = MusicState.Explore;
            return State;
        }
    }
}
