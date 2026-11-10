using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: alle Spiel-, Umgebungs- und UI-Klänge als Rezepte (prozedural, deterministisch). Der AudioBuilder rendert jede Id
    /// in 3 Varianten (Schleifen in 1) nach Procedural/Audio/ und normalisiert auf das Kategorie-Ziel.
    /// Ids: Skill-Familien „Palette/Ereignis“ (wie <see cref="VfxIds.Sfx"/>), sonst „Bereich/Name“.
    /// </summary>
    public static class SoundRecipes
    {
        public static readonly string[] SkillEvents = { "Charge", "Release", "Swing1", "Swing2", "Swing3", "Impact", "Buff" };
        public static readonly string[] Surfaces = { "Grass", "Earth", "Stone", "Wood", "Water", "Snow", "Ash" };
        public static readonly string[] HitMaterials = { "Flesh", "Armor", "Stone", "Wood", "Elemental" };
        public static readonly string[] SwishKinds = { "Heavy", "Light", "Blunt", "Bow", "Magic", "Gun" };
        public static readonly string[] Factions = { "Beast", "Bandit", "Moor", "Frost", "Glut" };
        public static readonly string[] UiIds = { "Hover", "Click", "WindowOpen", "WindowClose", "Tab", "DragStart", "Drop", "Split", "DestroyConfirm", "Error", "Buy", "Sell", "MapOpen", "MapClose", "Zoom", "WaypointSelect", "SkillAssign", "TraitSelect", "CharSelect", "Slider" };
        public static readonly string[] Zones = { "sonnfurt", "nebelmoor", "frostkamm", "gluetoede", "silberkueste", "daemmerwald", "duenenmeer", "kristalltiefe", "himmelsspitzen", "tyrannenfeste" };   // Lauf 6: zehn Zonen

        static List<SoundRecipe> _all;
        public static IReadOnlyList<SoundRecipe> All { get { if (_all == null) _all = Build(); return _all; } }

        public static SoundRecipe Find(string id) { foreach (var r in All) if (r.Id == id) return r; return null; }

        /// <summary>Rendert eine Variante inkl. Lautheits-Normalisierung, Knackschutz und (bei Schleifen) nahtlosem Übergang.</summary>
        public static float[] Render(SoundRecipe r, int variant)
        {
            var rng = new Rng(Rng.Hash(r.Id) + variant * 7919);
            var v = SoundVariation.For(variant, new Rng(Rng.Hash(r.Id) * 31 + variant));
            var b = r.Render(rng, v);
            var spec = SoundCategories.Spec(r.Category);
            if (r.Loop) b = Synth.MakeLoop(b, Math.Min(1.5f, Synth.Seconds(b) * 0.2f));
            else Synth.Fade(b, 1.5f, Math.Min(40f, Synth.Seconds(b) * 200f));
            Synth.Normalize(b, spec.TargetRmsDb, -1f);
            if (r.Loop)
            {
                Synth.SealLoop(b);
                // Balance B1 (Lauf-6-Rest): dichte Hochton-Betten (Dämmerwald) – Naht bei Bedarf breiter glätten
                for (int k = 1; k <= 3 && AudioAnalysis.LoopSeam(b) >= 0.008f; k++) Synth.SealLoop(b, 96 << k, 24 << k);
            }
            return b;
        }

        // ------------------------------------------------------------------ Paletten-Klangfamilien
        struct Family { public float Base; public Synth.Wave Wave; public float NoiseLo, NoiseHi, Crackle, Thump, Fm, Bright; public bool Bubbles, Zap, Glass, Dark; }

        static Family Fam(VfxPalette p)
        {
            switch (p)
            {
                case VfxPalette.Feuer: return new Family { Base = 220f, Wave = Synth.Wave.Saw, NoiseLo = 400f, NoiseHi = 2500f, Crackle = 900f, Thump = 0.4f, Bright = 1f };
                case VfxPalette.Wasser: return new Family { Base = 330f, Wave = Synth.Wave.Sine, NoiseLo = 600f, NoiseHi = 3500f, Bubbles = true, Thump = 0.2f, Bright = 1f };
                case VfxPalette.Luft: return new Family { Base = 440f, Wave = Synth.Wave.Triangle, NoiseLo = 1200f, NoiseHi = 6000f, Zap = true, Bright = 1.3f };
                case VfxPalette.Erde: return new Family { Base = 110f, Wave = Synth.Wave.Triangle, NoiseLo = 200f, NoiseHi = 1200f, Thump = 1f, Crackle = 300f, Bright = 0.7f };
                case VfxPalette.Natur: return new Family { Base = 262f, Wave = Synth.Wave.Triangle, NoiseLo = 700f, NoiseHi = 4000f, Fm = 0.4f, Bright = 1f };
                case VfxPalette.Schatten: return new Family { Base = 98f, Wave = Synth.Wave.Saw, NoiseLo = 250f, NoiseHi = 1400f, Dark = true, Thump = 0.4f, Bright = 0.6f };
                case VfxPalette.Licht: return new Family { Base = 523f, Wave = Synth.Wave.Sine, NoiseLo = 1500f, NoiseHi = 7000f, Fm = 1f, Glass = true, Bright = 1.4f };
                case VfxPalette.Blut: return new Family { Base = 150f, Wave = Synth.Wave.Sine, NoiseLo = 300f, NoiseHi = 1800f, Thump = 0.8f, Bubbles = true, Bright = 0.8f };
                case VfxPalette.Arkan: return new Family { Base = 392f, Wave = Synth.Wave.Sine, NoiseLo = 900f, NoiseHi = 5000f, Fm = 0.8f, Bright = 1.1f };
                case VfxPalette.Frost: return new Family { Base = 660f, Wave = Synth.Wave.Sine, NoiseLo = 2000f, NoiseHi = 8000f, Glass = true, Fm = 0.6f, Bright = 1.4f };
                // Lauf 5 / M26: Sound-Motive der Elite-Spezialisierungen
                case VfxPalette.Mondlicht: return new Family { Base = 349f, Wave = Synth.Wave.Sine, NoiseLo = 800f, NoiseHi = 4500f, Fm = 0.6f, Glass = true, Bright = 1.2f };
                case VfxPalette.Wildblut: return new Family { Base = 123f, Wave = Synth.Wave.Saw, NoiseLo = 250f, NoiseHi = 1600f, Thump = 0.9f, Bright = 0.75f };
                case VfxPalette.Dorngift: return new Family { Base = 196f, Wave = Synth.Wave.Triangle, NoiseLo = 500f, NoiseHi = 2800f, Bubbles = true, Crackle = 500f, Bright = 0.9f };
                case VfxPalette.Himmel: return new Family { Base = 587f, Wave = Synth.Wave.Triangle, NoiseLo = 1600f, NoiseHi = 7000f, Zap = true, Bright = 1.35f };
                case VfxPalette.Gewitter: return new Family { Base = 294f, Wave = Synth.Wave.Saw, NoiseLo = 900f, NoiseHi = 6500f, Zap = true, Thump = 0.6f, Bright = 1.25f };
                case VfxPalette.Zwielicht: return new Family { Base = 370f, Wave = Synth.Wave.Sine, NoiseLo = 800f, NoiseHi = 4500f, Fm = 1f, Bright = 1.05f };
                case VfxPalette.Jade: return new Family { Base = 415f, Wave = Synth.Wave.Sine, NoiseLo = 900f, NoiseHi = 5000f, Fm = 0.8f, Glass = true, Thump = 0.3f, Bright = 1.15f };
                case VfxPalette.Lava: return new Family { Base = 98f, Wave = Synth.Wave.Saw, NoiseLo = 200f, NoiseHi = 1400f, Crackle = 700f, Bubbles = true, Thump = 0.9f, Bright = 0.7f };
                case VfxPalette.Pulver: return new Family { Base = 139f, Wave = Synth.Wave.Square, NoiseLo = 400f, NoiseHi = 3500f, Crackle = 1600f, Thump = 0.8f, Bright = 1f };
                case VfxPalette.Staubgold: return new Family { Base = 233f, Wave = Synth.Wave.Triangle, NoiseLo = 600f, NoiseHi = 3800f, Thump = 0.5f, Bright = 1f };
                case VfxPalette.Tiefviolett: return new Family { Base = 87f, Wave = Synth.Wave.Saw, NoiseLo = 200f, NoiseHi = 1200f, Dark = true, Fm = 0.4f, Bright = 0.55f };
                case VfxPalette.Stahl: return new Family { Base = 494f, Wave = Synth.Wave.Triangle, NoiseLo = 1500f, NoiseHi = 6500f, Glass = true, Bright = 1.3f };
                case VfxPalette.Richtglut: return new Family { Base = 440f, Wave = Synth.Wave.Saw, NoiseLo = 900f, NoiseHi = 4000f, Crackle = 1000f, Fm = 0.5f, Bright = 1.15f };
                case VfxPalette.Bollwerk: return new Family { Base = 262f, Wave = Synth.Wave.Sine, NoiseLo = 500f, NoiseHi = 3000f, Glass = true, Thump = 0.7f, Bright = 1f };
                case VfxPalette.Sturmblau: return new Family { Base = 330f, Wave = Synth.Wave.Saw, NoiseLo = 1000f, NoiseHi = 7000f, Zap = true, Thump = 0.5f, Bright = 1.3f };
                case VfxPalette.Smaragd: return new Family { Base = 311f, Wave = Synth.Wave.Triangle, NoiseLo = 700f, NoiseHi = 4200f, Fm = 0.5f, Crackle = 300f, Bright = 1f };
                default: return new Family { Base = 165f, Wave = Synth.Wave.Saw, NoiseLo = 300f, NoiseHi = 1800f, Crackle = 1400f, Thump = 0.7f, Bright = 0.85f };   // Glut
            }
        }

        static float[] SkillSound(VfxPalette p, string evt, Rng r, SoundVariation v)
        {
            var f = Fam(p); float pitch = v.Pitch, br = v.Bright * f.Bright;
            switch (evt)
            {
                case "Charge":
                {
                    float len = 0.7f * v.Duration; var b = Synth.Buffer(len);
                    Synth.AddTone(b, 0, len, f.Wave, t => f.Base * pitch * (1f + t * 1.6f), 0.18f, new Adsr(len * 0.8f, 0.1f, 0.6f, 0.08f));
                    Synth.AddTone(b, 0, len, Synth.Wave.Sine, t => f.Base * 2.01f * pitch * (1f + t * 1.6f), 0.12f, new Adsr(len * 0.8f, 0.1f, 0.6f, 0.08f), 7f);
                    if (f.Wave != Synth.Wave.Sine) Synth.Filter(b, Synth.FilterType.LowPass, t => 600f * br + 3000f * br * t / len, 1.4f);
                    Synth.Mix(b, Whoosh(len, f.NoiseLo, f.NoiseHi, 2f, 0.4f, r, 0.85f));
                    if (f.Crackle > 0f) SoundKit.Crackle(b, 60f, 0.2f, r);
                    if (f.Glass) Synth.Mix(b, SoundKit.Chime(len, new[] { f.Base * 4f * pitch, f.Base * 5f * pitch }, len * 0.4f, 3.5f, 1.2f, 0.08f));
                    return b;
                }
                case "Release":
                {
                    float len = 0.45f * v.Duration; var b = Whoosh(len, f.NoiseHi * br, f.NoiseLo, 1.2f, 0.9f, r, 0.08f);
                    Synth.AddTone(b, 0, len * 0.6f, f.Wave, t => f.Base * 1.5f * pitch * (1f - t * 0.8f), 0.25f, Adsr.Perc(0.002f, len * 0.25f));
                    if (f.Zap) Zap(b, f.Base * 3f * pitch, r);
                    if (f.Thump > 0f) Synth.Mix(b, SoundKit.Thump(len, 160f * pitch, 60f, 0.5f * f.Thump, 0.3f, r));
                    if (f.Fm > 0f) Synth.Mix(b, SoundKit.Chime(len, new[] { f.Base * 2f * pitch }, 0f, 2.5f, 2f * f.Fm, 0.18f));
                    return b;
                }
                case "Swing1": case "Swing2": case "Swing3":
                {
                    int k = evt[5] - '1'; float len = (0.22f + 0.06f * k) * v.Duration;
                    var b = Whoosh(len, (900f + 300f * k) * br, (2800f - 400f * k) * br, 1.6f, 0.9f, r, 0.35f);
                    Synth.AddTone(b, 0, len, f.Wave, t => f.Base * (1.2f + 0.25f * k) * pitch, 0.06f, new Adsr(len * 0.3f, len * 0.5f, 0f, 0.01f));
                    if (k == 2 && f.Thump > 0f) Synth.Mix(b, SoundKit.Thump(len, 120f * pitch, 55f, 0.4f * f.Thump, 0.2f, r));
                    return b;
                }
                case "Impact":
                {
                    float len = 0.42f * v.Duration;
                    var b = SoundKit.Thump(len, 180f * pitch, 50f, 0.7f * Math.Max(0.35f, f.Thump), 0.6f, r);
                    var n = Synth.Buffer(len); Synth.AddNoise(n, 0, len, 0.5f, Adsr.Perc(0.001f, 0.08f), r); Synth.Filter(n, Synth.FilterType.BandPass, (f.NoiseLo + f.NoiseHi) * 0.5f * br, 0.9f); Synth.Mix(b, n);
                    if (f.Crackle > 0f) SoundKit.Crackle(b, f.Crackle, 0.25f, r);
                    if (f.Bubbles) SoundKit.Bubbles(b, 6, 400f * pitch, 1200f * pitch, 0.18f, r);
                    if (f.Glass) Synth.Mix(b, SoundKit.Chime(len, new[] { f.Base * 3f * pitch, f.Base * 4.2f * pitch }, 0.03f, 3.7f, 1.5f, 0.12f));
                    if (f.Zap) Zap(b, f.Base * 2f * pitch, r);
                    if (f.Dark) Synth.Filter(b, Synth.FilterType.LowPass, 1800f);
                    return b;
                }
                default:   // Buff: weiches Schimmern
                {
                    float len = 0.65f * v.Duration;
                    var b = SoundKit.Chime(len, new[] { f.Base * 2f * pitch, f.Base * 2.5f * pitch, f.Base * 3f * pitch }, 0.07f, f.Fm > 0 ? 2f : 1f, 0.8f, 0.14f);
                    Synth.Mix(b, Whoosh(len, f.NoiseLo, f.NoiseHi, 3f, 0.15f, r, 0.4f));
                    if (f.Dark) Synth.Filter(b, Synth.FilterType.LowPass, 2000f);
                    return b;
                }
            }
        }

        static float[] Whoosh(float len, float f0, float f1, float q, float amp, Rng r, float atk) => SoundKit.Whoosh(len, f0, f1, q, amp, r, atk);

        static void Zap(float[] b, float f, Rng r)
        {
            float len = Math.Min(0.3f, Synth.Seconds(b));
            Synth.AddTone(b, 0, len, Synth.Wave.Square, t => f * (1f + 0.5f * (r.Value() - 0.5f)), 0.08f, Adsr.Perc(0.001f, len * 0.4f));
            SoundKit.Crackle(b, 400f, 0.2f, r, 3000f);
        }

        // ------------------------------------------------------------------ Liste
        static void Add(List<SoundRecipe> l, string id, SoundCategory c, Func<Rng, SoundVariation, float[]> f, bool loop = false, bool spatial = true, int variants = 3, float volDb = 0f)
            => l.Add(new SoundRecipe { Id = id, Category = c, Render = f, Loop = loop, Spatial = spatial, Variants = loop ? 1 : variants, VolumeDb = volDb });

        static List<SoundRecipe> Build()
        {
            var l = new List<SoundRecipe>();
            // Skill-Familien je Palette (Aufladung, Auswurf, Kettenglieder, Einschlag, Segen)
            foreach (VfxPalette p in Enum.GetValues(typeof(VfxPalette)))
                foreach (var e in SkillEvents) { var pp = p; var ee = e; Add(l, VfxIds.Sfx(pp, ee), SoundCategory.SkillEffect, (r, v) => SkillSound(pp, ee, r, v)); }

            // Treffer nach Material, Krit, Block, Ausweichen, Unterbrochen
            Add(l, "Hit/Flesh", SoundCategory.Hit, (r, v) => { var b = SoundKit.Thump(0.22f * v.Duration, 140f * v.Pitch, 60f, 0.8f, 0.7f, r); Synth.Filter(b, Synth.FilterType.LowPass, 2200f * v.Bright); return b; });
            Add(l, "Hit/Armor", SoundCategory.Hit, (r, v) => { var b = SoundKit.Thump(0.3f, 160f * v.Pitch, 80f, 0.5f, 0.5f, r); Synth.Mix(b, SoundKit.Chime(0.3f * v.Duration, new[] { 1240f * v.Pitch, 1870f * v.Pitch }, 0f, 1.41f, 3f, 0.2f)); return b; });
            Add(l, "Hit/Stone", SoundCategory.Hit, (r, v) => { var b = SoundKit.Thump(0.28f, 120f * v.Pitch, 50f, 0.6f, 0.4f, r); var g = SoundKit.Crack(0.2f * v.Duration, 1200f * v.Bright, 0.5f, r); Synth.Mix(b, g); return b; });
            Add(l, "Hit/Wood", SoundCategory.Hit, (r, v) => { var b = Synth.Buffer(0.25f * v.Duration); Synth.AddTone(b, 0, 0.25f, Synth.Wave.Triangle, t => 320f * v.Pitch, 0.6f, Adsr.Perc(0.001f, 0.05f)); Synth.AddNoise(b, 0, 0.06f, 0.4f, Adsr.Perc(0.0005f, 0.015f), r); Synth.Filter(b, Synth.FilterType.BandPass, 900f * v.Bright, 0.9f); return b; });
            Add(l, "Hit/Elemental", SoundCategory.Hit, (r, v) => { var b = SoundKit.Chime(0.35f * v.Duration, new[] { 700f * v.Pitch, 1050f * v.Pitch }, 0.02f, 2.7f, 2.5f, 0.3f); Synth.Mix(b, SoundKit.Whoosh(0.3f, 3000f, 800f, 1.5f, 0.4f, r, 0.05f)); return b; });
            Add(l, "Hit/Crit", SoundCategory.Hit, (r, v) => { var b = SoundKit.Thump(0.32f, 200f * v.Pitch, 55f, 0.8f, 0.6f, r); Synth.Mix(b, SoundKit.Chime(0.32f, new[] { 1760f * v.Pitch, 2640f * v.Pitch }, 0.015f, 1f, 0.5f, 0.25f)); return b; });
            Add(l, "Hit/Block", SoundCategory.Hit, (r, v) => { var b = SoundKit.Chime(0.4f * v.Duration, new[] { 880f * v.Pitch, 1320f * v.Pitch }, 0f, 1.41f, 4f, 0.35f); Synth.Mix(b, SoundKit.Thump(0.2f, 220f, 110f, 0.4f, 0.3f, r)); return b; });
            Add(l, "Combat/Interrupted", SoundCategory.Hit, (r, v) => { var b = Synth.Buffer(0.45f * v.Duration); Synth.AddTone(b, 0, 0.45f, Synth.Wave.Triangle, t => 260f * v.Pitch * (1f - t * 0.9f), 0.5f, Adsr.Perc(0.002f, 0.2f)); Synth.Mix(b, SoundKit.Crack(0.25f, 2500f, 0.3f, r)); Synth.Filter(b, Synth.FilterType.LowPass, 1600f); return b; });
            Add(l, "Combat/ComboFinish", SoundCategory.SkillEffect, (r, v) => SoundKit.Chime(0.8f * v.Duration, new[] { 660f * v.Pitch, 990f * v.Pitch, 1320f * v.Pitch }, 0.05f, 2f, 1.5f, 0.25f));
            Add(l, "Combat/DefianceBreak", SoundCategory.Hit, (r, v) => { var b = SoundKit.Crack(0.6f * v.Duration, 1500f, 0.6f, r); Synth.Mix(b, SoundKit.Chime(0.6f, new[] { 2100f * v.Pitch, 2900f * v.Pitch, 3300f * v.Pitch }, 0.03f, 3.3f, 2f, 0.2f)); Synth.Mix(b, SoundKit.Thump(0.5f, 90f, 40f, 0.6f, 0.2f, r)); return b; });
            Add(l, "Enemy/Telegraph", SoundCategory.Vocal, (r, v) => { float len = 0.9f * v.Duration; var b = Synth.Buffer(len); Synth.AddTone(b, 0, len, Synth.Wave.Saw, t => 140f * v.Pitch * (1f + t * 1.4f), 0.3f, new Adsr(len * 0.85f, 0.05f, 0.8f, 0.06f)); Synth.Filter(b, Synth.FilterType.LowPass, t => 500f + 2500f * t / len, 2f); return b; });
            Add(l, "Boss/Roar", SoundCategory.Vocal, (r, v) => Roar(1.8f * v.Duration, 70f * v.Pitch, r, 1f), variants: 2);
            foreach (var k in SwishKinds) { var kk = k; Add(l, "Swish/" + kk, SoundCategory.SkillEffect, (r, v) => Swish(kk, r, v)); }

            // Bewegung: Schritte je Untergrund, Ausweichen, Sprung/Landung, Gleiter
            foreach (var s in Surfaces) { var ss = s; Add(l, "Step/" + ss, SoundCategory.Movement, (r, v) => Step(ss, r, v)); }
            Add(l, "Move/Dodge", SoundCategory.Movement, (r, v) => SoundKit.Whoosh(0.32f * v.Duration, 500f, 2400f * v.Bright, 1.2f, 0.9f, r, 0.4f));
            Add(l, "Move/Jump", SoundCategory.Movement, (r, v) => SoundKit.Whoosh(0.2f * v.Duration, 700f, 1800f * v.Bright, 1.3f, 0.7f, r, 0.2f));
            Add(l, "Move/Land", SoundCategory.Movement, (r, v) => SoundKit.Thump(0.25f * v.Duration, 110f * v.Pitch, 50f, 0.8f, 0.6f, r));
            Add(l, "Move/GliderOpen", SoundCategory.Movement, (r, v) => { var b = SoundKit.Whoosh(0.5f * v.Duration, 300f, 2500f, 0.9f, 0.8f, r, 0.15f); Synth.Mix(b, SoundKit.Crack(0.12f, 2000f, 0.3f, r)); return b; });
            Add(l, "Move/GliderClose", SoundCategory.Movement, (r, v) => SoundKit.Whoosh(0.4f * v.Duration, 2500f, 400f, 0.9f, 0.7f, r, 0.1f));
            Add(l, "Mount/Call", SoundCategory.Vocal, (r, v) => Roar(0.8f * v.Duration, 180f * v.Pitch, r, 0.4f));
            Add(l, "Mount/Engage", SoundCategory.SkillEffect, (r, v) => { var b = SoundKit.Thump(0.5f, 140f * v.Pitch, 45f, 0.9f, 0.6f, r); Synth.Mix(b, SoundKit.Whoosh(0.5f, 2000f, 500f, 1f, 0.5f, r, 0.05f)); return b; });
            Add(l, "Mount/Dismount", SoundCategory.Movement, (r, v) => SoundKit.Concat(SoundKit.Whoosh(0.18f, 800f, 1600f, 1.2f, 0.6f, r, 0.3f), SoundKit.Thump(0.2f, 130f * v.Pitch, 60f, 0.6f, 0.4f, r)));
            Add(l, "Mount/Ability", SoundCategory.Movement, (r, v) => SoundKit.Whoosh(0.4f * v.Duration, 400f, 3000f, 1f, 0.9f, r, 0.25f));

            // Gegner-Laute je Fraktion
            foreach (var fct in Factions)
            {
                var ff = fct; float baseHz = ff == "Beast" ? 220f : ff == "Bandit" ? 160f : ff == "Moor" ? 110f : ff == "Frost" ? 300f : 90f;
                Add(l, "Vox/" + ff + "/Attack", SoundCategory.Vocal, (r, v) => Roar(0.45f * v.Duration, baseHz * v.Pitch, r, 0.6f));
                Add(l, "Vox/" + ff + "/Hurt", SoundCategory.Vocal, (r, v) => Roar(0.28f * v.Duration, baseHz * 1.3f * v.Pitch, r, 0.3f));
                Add(l, "Vox/" + ff + "/Death", SoundCategory.Vocal, (r, v) => { var b = Roar(0.9f * v.Duration, baseHz * v.Pitch, r, 0.8f); Synth.Filter(b, Synth.FilterType.LowPass, t => 2500f - 1800f * Math.Min(1f, t), 0.8f); return b; });
            }

            // Segen und Zustände: je Effekt ein kurzer, leiser Klang (Segen hell aufsteigend, Zustände dumpf)
            int bi = 0;
            foreach (EffectType e in Enum.GetValues(typeof(EffectType)))
            {
                var ee = e; int idx = bi++; bool boon = (int)e < 12;
                Add(l, "Effect/" + ee, SoundCategory.World, (r, v) => boon
                    ? SoundKit.Chime(0.35f * v.Duration, new[] { Synth.MidiToHz(72 + idx % 12) * v.Pitch, Synth.MidiToHz(79 + idx % 12) * v.Pitch }, 0.06f, 2f, 0.6f, 0.25f)
                    : EffectDull(idx, r, v), variants: 2);
            }

            // Beute, Ausrüstung, Fortschritt, Welt
            foreach (ItemRarity rar in Enum.GetValues(typeof(ItemRarity)))
            {
                var rr = rar; int n = Math.Max(1, (int)rr - 1);
                Add(l, "Loot/" + rr, SoundCategory.World, (r, v) => { var fs = new float[Math.Min(5, n + 1)]; for (int i = 0; i < fs.Length; i++) fs[i] = Synth.MidiToHz(72 + new[] { 0, 4, 7, 12, 16 }[i] + (int)rr) * v.Pitch; return SoundKit.Chime(0.35f + 0.12f * fs.Length, fs, 0.06f, (int)rr >= 5 ? 3.5f : 2f, 0.7f + 0.15f * (int)rr, 0.22f); }, variants: 2);
            }
            Add(l, "Loot/Coins", SoundCategory.World, (r, v) => { var b = Synth.Buffer(0.45f * v.Duration); for (int i = 0; i < 5; i++) Synth.Mix(b, SoundKit.Chime(0.25f, new[] { r.Range(2200f, 3000f) * v.Pitch }, 0f, 1.41f, 1.2f, 0.2f), (int)(i * 0.06f * Synth.Rate)); return b; });
            Add(l, "Loot/Salvage", SoundCategory.World, (r, v) => { var b = SoundKit.Crack(0.35f * v.Duration, 1500f, 0.5f, r); Synth.Mix(b, SoundKit.Thump(0.3f, 300f, 120f, 0.4f, 0.3f, r)); return b; });
            Add(l, "Equip/Metal", SoundCategory.World, (r, v) => SoundKit.Chime(0.35f * v.Duration, new[] { 1500f * v.Pitch, 2300f * v.Pitch }, 0.05f, 1.41f, 3f, 0.25f));
            Add(l, "Equip/Leather", SoundCategory.World, (r, v) => { var b = SoundKit.Whoosh(0.25f * v.Duration, 600f, 1400f, 2f, 0.6f, r, 0.3f); Synth.Mix(b, SoundKit.Thump(0.12f, 260f, 150f, 0.3f, 0.2f, r)); return b; });
            Add(l, "Equip/Cloth", SoundCategory.World, (r, v) => SoundKit.Whoosh(0.3f * v.Duration, 1200f, 3000f, 1.5f, 0.6f, r, 0.4f));
            Add(l, "World/Waypoint", SoundCategory.World, (r, v) => SoundKit.Chime(1.2f, new[] { 587f, 880f, 1175f }, 0.12f, 2f, 1.2f, 0.25f), variants: 1);
            Add(l, "World/POI", SoundCategory.World, (r, v) => SoundKit.Chime(0.9f, new[] { 784f, 988f }, 0.1f, 2f, 0.8f, 0.25f), variants: 1);
            Add(l, "World/Vista", SoundCategory.World, (r, v) => { var b = SoundKit.Chime(1.8f, new[] { 523f, 659f, 784f, 1047f }, 0.15f, 1f, 0.6f, 0.2f); Synth.Mix(b, SoundKit.Whoosh(1.8f, 400f, 2500f, 0.8f, 0.2f, r, 0.6f)); return b; }, variants: 1);
            Add(l, "World/ZoneComplete", SoundCategory.World, (r, v) => SoundKit.Chime(2.6f, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.18f, 2f, 1f, 0.22f), variants: 1);
            Add(l, "World/Portal", SoundCategory.World, (r, v) => { var b = SoundKit.Whoosh(1.4f, 200f, 3000f, 2f, 0.6f, r, 0.6f); Synth.Mix(b, SoundKit.Chime(1.4f, new[] { 440f, 660f }, 0.4f, 3.5f, 2f, 0.15f)); Synth.Reverb(b, 0.8f, 0.3f, 0.3f); return b; }, variants: 1);
            Add(l, "World/Chest", SoundCategory.World, (r, v) => { var b = SoundKit.Thump(0.3f, 180f, 90f, 0.5f, 0.4f, r); Synth.Mix(b, SoundKit.Chime(0.9f, new[] { 1047f, 1319f, 1568f }, 0.08f, 2f, 0.6f, 0.2f), (int)(0.15f * Synth.Rate)); return b; }, variants: 1);
            Add(l, "World/HeroPoint", SoundCategory.World, (r, v) => SoundKit.Chime(1.0f, new[] { 698f, 880f, 1047f }, 0.09f, 3f, 1f, 0.22f), variants: 1);
            Add(l, "Event/Horn", SoundCategory.World, (r, v) => Horn(1.6f, new[] { 196f, 294f }, r), variants: 1);
            Add(l, "Event/Success", SoundCategory.World, (r, v) => Horn(2.2f, new[] { 262f, 330f, 392f, 523f }, r), variants: 1);
            Add(l, "Event/Fail", SoundCategory.World, (r, v) => { var b = Synth.Buffer(2.2f); foreach (var f in new[] { 220f, 262f, 330f }) Synth.AddTone(b, 0, 2.2f, Synth.Wave.Saw, t => f, 0.12f, new Adsr(0.05f, 0.4f, 0.6f, 0.8f)); Synth.Filter(b, Synth.FilterType.LowPass, 900f); Synth.Reverb(b, 0.8f, 0.3f, 0.3f); return b; }, variants: 1);
            Add(l, "Event/Medal", SoundCategory.World, (r, v) => SoundKit.Chime(1.3f, new[] { 1319f, 1760f, 2093f }, 0.05f, 1.41f, 2.5f, 0.22f), variants: 1);
            Add(l, "Prog/LevelUp", SoundCategory.World, (r, v) => SoundKit.Chime(1.6f, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.11f, 2f, 1.2f, 0.22f), variants: 1);
            Add(l, "Prog/Mastery", SoundCategory.World, (r, v) => { var b = SoundKit.Chime(1.8f, new[] { 392f, 587f, 784f, 1175f }, 0.14f, 3.5f, 1.4f, 0.2f); Synth.Reverb(b, 0.85f, 0.3f, 0.3f); return b; }, variants: 1);

            // Umgebung: Betten (Schleifen) und Zufallsgeräusche
            Add(l, "Amb/sonnfurt", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.5f, insects: 0.35f, birds: 0.5f, water: 0f, crackle: 0f), loop: true, spatial: false);
            Add(l, "Amb/nebelmoor", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.4f, insects: 0.6f, birds: 0.1f, water: 0.5f, crackle: 0f, frogs: true), loop: true, spatial: false);
            Add(l, "Amb/frostkamm", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 1f, insects: 0f, birds: 0f, water: 0f, crackle: 0f, snow: true), loop: true, spatial: false);
            Add(l, "Amb/gluetoede", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.7f, insects: 0f, birds: 0f, water: 0f, crackle: 0.8f), loop: true, spatial: false);
            // Lauf 6: Klangbetten der neuen Zonen
            Add(l, "Amb/silberkueste", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.6f, insects: 0.1f, birds: 0.6f, water: 0.9f, crackle: 0f), loop: true, spatial: false);
            Add(l, "Amb/daemmerwald", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.3f, insects: 0.5f, birds: 0.15f, water: 0f, crackle: 0.1f, frogs: true), loop: true, spatial: false);
            Add(l, "Amb/duenenmeer", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.9f, insects: 0.05f, birds: 0f, water: 0f, crackle: 0.15f), loop: true, spatial: false);
            Add(l, "Amb/kristalltiefe", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.15f, insects: 0f, birds: 0f, water: 0.35f, crackle: 0.05f), loop: true, spatial: false);
            Add(l, "Amb/himmelsspitzen", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 1.1f, insects: 0f, birds: 0.3f, water: 0f, crackle: 0f, snow: true), loop: true, spatial: false);
            Add(l, "Amb/tyrannenfeste", SoundCategory.Ambience, (r, v) => Bed(10f, r, wind: 0.6f, insects: 0f, birds: 0f, water: 0f, crackle: 1f), loop: true, spatial: false);
            Add(l, "Amb/Water", SoundCategory.Ambience, (r, v) => Bed(8f, r, wind: 0f, insects: 0f, birds: 0f, water: 1f, crackle: 0f), loop: true);
            Add(l, "Amb/WindHigh", SoundCategory.Ambience, (r, v) => Bed(8f, r, wind: 1.2f, insects: 0f, birds: 0f, water: 0f, crackle: 0f), loop: true, spatial: false);
            Add(l, "Amb/HubMurmur", SoundCategory.Ambience, (r, v) => Murmur(8f, r), loop: true);
            Add(l, "Amb/Torch", SoundCategory.Ambience, (r, v) => { var b = Synth.Buffer(4f); Synth.AddPink(b, 0, 4f, 0.25f, new Adsr(0f, 0f, 1f, 0f), r); Synth.Filter(b, Synth.FilterType.LowPass, 700f); SoundKit.Crackle(b, 25f, 0.5f, r); return b; }, loop: true);
            Add(l, "Amb/Waterfall", SoundCategory.Ambience, (r, v) => { var b = Synth.Buffer(6f); Synth.AddPink(b, 0, 6f, 0.8f, new Adsr(0f, 0f, 1f, 0f), r); Synth.Filter(b, Synth.FilterType.LowPass, 3500f); return b; }, loop: true);
            Add(l, "Amb/Rain", SoundCategory.Ambience, (r, v) => { var b = Synth.Buffer(6f); Synth.AddNoise(b, 0, 6f, 0.3f, new Adsr(0f, 0f, 1f, 0f), r); Synth.Filter(b, Synth.FilterType.HighPass, 1200f); Synth.Filter(b, Synth.FilterType.LowPass, 6000f); SoundKit.Crackle(b, 400f, 0.2f, r, 2000f); return b; }, loop: true, spatial: false);
            Add(l, "Amb/BirdCall", SoundCategory.Ambience, (r, v) => Bird(r, v), variants: 3);
            Add(l, "Amb/Thunder", SoundCategory.Ambience, (r, v) => { float len = 3.5f * v.Duration; var b = Synth.Buffer(len); Synth.AddPink(b, 0, len, 1f, new Adsr(0.08f, 1.2f, 0f, 0.5f), r); Synth.Filter(b, Synth.FilterType.LowPass, 300f * v.Bright, 0.9f); return b; }, spatial: false);
            Add(l, "Amb/WolfHowl", SoundCategory.Ambience, (r, v) => { float len = 2.4f * v.Duration; var b = Synth.Buffer(len); Synth.AddTone(b, 0, len, Synth.Wave.Sine, t => 380f * v.Pitch * (1f + 0.35f * (float)Math.Sin(Math.Min(1f, t / len) * Math.PI)) * (1f + 0.01f * (float)Math.Sin(t * 30f)), 0.4f, new Adsr(0.4f, 0.5f, 0.7f, 0.8f)); Synth.AddTone(b, 0, len, Synth.Wave.Sine, t => 760f * v.Pitch, 0.05f, new Adsr(0.4f, 0.5f, 0.7f, 0.8f)); Synth.Reverb(b, 0.9f, 0.3f, 0.45f); return b; });
            Add(l, "Amb/Bell", SoundCategory.Ambience, (r, v) => SoundKit.Chime(3f, new[] { 392f * v.Pitch }, 0f, 1.4f, 3f, 0.4f));
            Add(l, "Amb/Hammer", SoundCategory.Ambience, (r, v) => { var b = Synth.Buffer(0.6f); for (int i = 0; i < 2; i++) Synth.Mix(b, SoundKit.Chime(0.3f, new[] { 1500f * v.Pitch, 2100f * v.Pitch }, 0f, 1.41f, 1.5f, 0.3f), (int)(i * 0.28f * Synth.Rate)); return b; });

            // UI (subtil, 1–4 kHz)
            foreach (var u in UiIds) { var uu = u; Add(l, "UI/" + uu, SoundCategory.UI, (r, v) => Ui(uu, r, v), spatial: false); }
            return l;
        }

        // ------------------------------------------------------------------ Helfer
        static float[] Swish(string kind, Rng r, SoundVariation v)
        {
            switch (kind)
            {
                case "Heavy": return SoundKit.Whoosh(0.38f * v.Duration, 400f, 1500f * v.Bright, 1.4f, 1f, r, 0.45f);
                case "Blunt": return SoundKit.Whoosh(0.32f * v.Duration, 300f, 1100f * v.Bright, 1.6f, 1f, r, 0.4f);
                case "Bow": { var b = Synth.Buffer(0.4f); Synth.AddPluck(b, 0, 0.4f, 110f * v.Pitch, 0.6f, 0.99f, r); Synth.Mix(b, SoundKit.Whoosh(0.3f, 2500f, 5000f, 2f, 0.5f, r, 0.05f), 800); return b; }
                case "Magic": return SoundKit.Chime(0.35f * v.Duration, new[] { 900f * v.Pitch, 1350f * v.Pitch }, 0.04f, 2.5f, 1.2f, 0.25f);
                case "Gun": { var b = SoundKit.Thump(0.3f, 300f * v.Pitch, 80f, 0.6f, 1f, r); Synth.Mix(b, SoundKit.Crack(0.12f, 2500f, 0.8f, r)); Synth.Reverb(b, 0.5f, 0.4f, 0.2f); return b; }
                default: return SoundKit.Whoosh(0.24f * v.Duration, 1000f, 3200f * v.Bright, 1.6f, 1f, r, 0.35f);
            }
        }

        static float[] Step(string s, Rng r, SoundVariation v)
        {
            float len = 0.16f * v.Duration;
            switch (s)
            {
                case "Grass": { var b = SoundKit.Whoosh(len, 2500f * v.Bright, 1500f, 1.2f, 0.8f, r, 0.1f); Synth.Mix(b, SoundKit.Thump(len, 120f, 70f, 0.2f, 0f, r)); return b; }
                case "Earth": { var b = SoundKit.Thump(len, 140f * v.Pitch, 70f, 0.5f, 0.6f, r); Synth.Filter(b, Synth.FilterType.LowPass, 1400f * v.Bright); return b; }
                case "Stone": { var b = SoundKit.Thump(len, 200f * v.Pitch, 110f, 0.35f, 0.4f, r); Synth.Mix(b, SoundKit.Crack(0.05f, 2500f * v.Bright, 0.4f, r)); return b; }
                case "Wood": { var b = Synth.Buffer(len); Synth.AddTone(b, 0, len, Synth.Wave.Triangle, t => 240f * v.Pitch, 0.5f, Adsr.Perc(0.001f, 0.04f)); Synth.AddNoise(b, 0, 0.04f, 0.3f, Adsr.Perc(0.0005f, 0.01f), r); Synth.Filter(b, Synth.FilterType.BandPass, 700f * v.Bright, 0.9f); return b; }
                case "Water": { var b = SoundKit.Whoosh(0.25f * v.Duration, 600f, 2500f * v.Bright, 1f, 0.8f, r, 0.1f); SoundKit.Bubbles(b, 5, 500f, 1400f, 0.2f, r); return b; }
                case "Snow": { var b = Synth.Buffer(0.22f * v.Duration); Synth.AddNoise(b, 0, 0.22f, 0.6f, new Adsr(0.03f, 0.08f, 0f, 0.01f), r); SoundKit.Crackle(b, 300f, 0.4f, r, 2500f); Synth.Filter(b, Synth.FilterType.BandPass, 3000f * v.Bright, 0.7f); return b; }
                default: { var b = Synth.Buffer(len); Synth.AddPink(b, 0, len, 0.7f, Adsr.Perc(0.004f, 0.05f), r); Synth.Filter(b, Synth.FilterType.BandPass, 1100f * v.Bright, 0.8f); SoundKit.Crackle(b, 120f, 0.2f, r, 1800f); return b; }   // Asche
            }
        }

        static float[] Roar(float len, float f, Rng r, float grit)
        {
            var b = Synth.Buffer(len);
            Synth.AddTone(b, 0, len, Synth.Wave.Saw, t => f * (1f + 0.15f * (float)Math.Sin(Math.Min(1f, t / len) * Math.PI)) * (1f + 0.02f * (float)Math.Sin(t * 37f)), 0.4f, new Adsr(len * 0.15f, len * 0.3f, 0.7f, len * 0.3f));
            Synth.AddNoise(b, 0, len, 0.3f * grit, new Adsr(len * 0.1f, len * 0.3f, 0.6f, len * 0.3f), r);
            Synth.Filter(b, Synth.FilterType.BandPass, f * 4f, 0.7f);
            Synth.Distort(b, 1f + 2f * grit);
            return b;
        }

        static float[] EffectDull(int idx, Rng r, SoundVariation v)
        {
            var b = SoundKit.Thump(0.3f * v.Duration, (160f + idx * 9f) * v.Pitch, 70f, 0.5f, 0.3f, r);
            Synth.AddTone(b, 0, 0.3f, Synth.Wave.Triangle, t => (300f + idx * 13f) * v.Pitch * (1f - t), 0.15f, Adsr.Perc(0.003f, 0.12f));
            Synth.Filter(b, Synth.FilterType.LowPass, 1800f);
            return b;
        }

        static float[] Horn(float len, float[] notes, Rng r)
        {
            var b = Synth.Buffer(len); float step = len * 0.6f / notes.Length;
            for (int i = 0; i < notes.Length; i++)
            {
                var tmp = Synth.Buffer(len - i * step);
                Synth.AddTone(tmp, 0, len - i * step, Synth.Wave.Saw, t => notes[i], 0.2f, new Adsr(0.06f, 0.2f, 0.7f, 0.4f), -4f);
                Synth.AddTone(tmp, 0, len - i * step, Synth.Wave.Saw, t => notes[i], 0.2f, new Adsr(0.06f, 0.2f, 0.7f, 0.4f), 4f);
                Synth.Filter(tmp, Synth.FilterType.LowPass, 1500f, 1f); Synth.Mix(b, tmp, (int)(i * step * Synth.Rate));
            }
            Synth.Reverb(b, 0.8f, 0.3f, 0.3f);
            return b;
        }

        static float[] Bed(float len, Rng r, float wind, float insects, float birds, float water, float crackle, bool frogs = false, bool snow = false)
        {
            var b = Synth.Buffer(len);
            if (wind > 0f)
            {
                var w = Synth.Buffer(len); Synth.AddPink(w, 0, len, wind, new Adsr(0f, 0f, 1f, 0f), r);
                float ph = r.Value() * 6f; Synth.Filter(w, Synth.FilterType.BandPass, t => 350f + 250f * (float)Math.Sin(t * 0.7f + ph) + (snow ? 500f : 0f), 0.9f); Synth.Mix(b, w);
            }
            if (water > 0f) { var w = Synth.Buffer(len); Synth.AddPink(w, 0, len, water * 0.6f, new Adsr(0f, 0f, 1f, 0f), r); Synth.Filter(w, Synth.FilterType.LowPass, 1800f); SoundKit.Bubbles(w, (int)(30 * water * len / 8f), 300f, 900f, 0.1f * water, r); Synth.Mix(b, w); }
            if (insects > 0f) { for (int i = 0; i < 3; i++) { float f = r.Range(3800f, 6200f), am = r.Range(18f, 32f); var ins = Synth.Buffer(len); Synth.AddTone(ins, 0, len, Synth.Wave.Sine, t => f, 0.025f * insects, new Adsr(0f, 0f, 1f, 0f)); for (int k = 0; k < ins.Length; k++) ins[k] *= 0.5f + 0.5f * (float)Math.Sin(k / (float)Synth.Rate * am); Synth.Mix(b, ins); } }
            if (birds > 0f) { int n = (int)(birds * len * 0.8f); for (int i = 0; i < n; i++) Synth.Mix(b, Bird(r, new SoundVariation { Pitch = r.Range(0.9f, 1.15f), Duration = 0.6f, Bright = 1f }), r.Range(0, b.Length - Synth.Rate), 0.25f); }
            if (crackle > 0f) { var c = Synth.Buffer(len); Synth.AddPink(c, 0, len, 0.25f * crackle, new Adsr(0f, 0f, 1f, 0f), r); Synth.Filter(c, Synth.FilterType.LowPass, 500f); SoundKit.Crackle(c, 18f * crackle, 0.5f, r); Synth.Mix(b, c); }
            if (frogs) for (int i = 0; i < (int)(len * 0.8f); i++) { int s = r.Range(0, b.Length - Synth.Rate / 2); float f = r.Range(180f, 260f); for (int k = 0; k < 3; k++) Synth.AddTone(b, s + k * 3000, 0.05f, Synth.Wave.Square, t => f, 0.04f, Adsr.Perc(0.002f, 0.02f)); }
            if (snow) SoundKit.Crackle(b, 10f, 0.05f, r, 4000f);
            return b;
        }

        static float[] Bird(Rng r, SoundVariation v)
        {
            float len = 0.5f * v.Duration; var b = Synth.Buffer(len); int notes = r.Range(2, 5); float f0 = r.Range(2500f, 3800f) * v.Pitch;
            for (int i = 0; i < notes; i++) { float a = f0 * r.Range(0.85f, 1.2f), dir = r.Range(-0.6f, 0.6f); Synth.AddTone(b, (int)(i * len / notes * Synth.Rate), len / notes * 0.8f, Synth.Wave.Sine, t => a * (1f + dir * t * 8f), 0.3f, new Adsr(0.005f, 0.03f, 0.5f, 0.02f)); }
            return b;
        }

        static float[] Murmur(float len, Rng r)
        {
            var b = Synth.Buffer(len);
            for (int i = 0; i < 6; i++)
            {
                var v = Synth.Buffer(len); Synth.AddPink(v, 0, len, 0.5f, new Adsr(0f, 0f, 1f, 0f), r);
                float f = r.Range(350f, 900f), rate = r.Range(2.5f, 4.5f), ph = r.Value() * 10f;
                Synth.Filter(v, Synth.FilterType.BandPass, t => f * (1f + 0.25f * (float)Math.Sin(t * rate + ph)), 4f);
                for (int k = 0; k < v.Length; k++) v[k] *= 0.5f + 0.5f * (float)Math.Sin(k / (float)Synth.Rate * rate * 1.3f + ph);
                Synth.Mix(b, v);
            }
            return b;
        }

        static float[] Ui(string id, Rng r, SoundVariation v)
        {
            float p = v.Pitch;
            switch (id)
            {
                case "Hover": return SoundKit.Tick(0.03f, 2600f * p, 0.4f, r, 0.2f);
                case "Click": return SoundKit.Tick(0.05f, 1900f * p, 0.6f, r);
                case "WindowOpen": case "MapOpen": { var b = SoundKit.Whoosh(0.18f, 1200f, 3200f, 2f, 0.8f, r, 0.5f); Synth.Mix(b, SoundKit.Tick(0.06f, 2400f * p, 0.3f, r, 0f), (int)(0.1f * Synth.Rate)); if (id == "MapOpen") SoundKit.Crackle(b, 300f, 0.08f, r, 1500f); Synth.Filter(b, Synth.FilterType.LowPass, 4500f); return b; }
                case "WindowClose": case "MapClose": { var b = SoundKit.Whoosh(0.15f, 2600f, 1100f, 2f, 0.8f, r, 0.2f); if (id == "MapClose") SoundKit.Crackle(b, 300f, 0.08f, r, 1500f); Synth.Filter(b, Synth.FilterType.LowPass, 4500f); return b; }
                case "Tab": return SoundKit.Tick(0.06f, 2200f * p, 0.5f, r);
                case "DragStart": return SoundKit.Tick(0.07f, 1500f * p, 0.5f, r, 0.6f);
                case "Drop": return SoundKit.Concat(SoundKit.Tick(0.04f, 1700f * p, 0.5f, r), SoundKit.Tick(0.05f, 1300f * p, 0.5f, r));
                case "Split": return SoundKit.Concat(SoundKit.Tick(0.04f, 2400f * p, 0.4f, r), SoundKit.Tick(0.04f, 2400f * p, 0.4f, r));
                case "DestroyConfirm": { var b = SoundKit.Crack(0.25f, 1500f, 0.5f, r); Synth.Filter(b, Synth.FilterType.LowPass, 2500f); Synth.Mix(b, SoundKit.Tick(0.1f, 1200f, 0.4f, r)); return b; }
                case "Error": { var b = Synth.Buffer(0.18f); Synth.AddTone(b, 0, 0.18f, Synth.Wave.Square, t => 1100f * p, 0.25f, Adsr.Perc(0.002f, 0.08f)); Synth.Filter(b, Synth.FilterType.BandPass, 1300f, 1.2f); return b; }
                case "Buy": case "Sell": return SoundKit.Chime(0.25f, new[] { (id == "Buy" ? 2000f : 1600f) * p, (id == "Buy" ? 2600f : 2000f) * p }, 0.05f, 1.41f, 1.5f, 0.3f);
                case "Zoom": return SoundKit.Tick(0.035f, 3000f * p, 0.35f, r, 0.1f);
                case "WaypointSelect": return SoundKit.Chime(0.3f, new[] { 1760f * p, 2200f * p }, 0.05f, 2f, 0.5f, 0.3f);
                case "SkillAssign": return SoundKit.Concat(SoundKit.Tick(0.05f, 1600f * p, 0.5f, r), SoundKit.Tick(0.08f, 2400f * p, 0.4f, r));
                case "TraitSelect": return SoundKit.Chime(0.25f, new[] { 1400f * p, 2100f * p }, 0.04f, 3f, 0.8f, 0.3f);
                case "CharSelect": { var b = SoundKit.Whoosh(0.3f, 1200f, 2800f, 1.5f, 0.5f, r, 0.4f); Synth.Mix(b, SoundKit.Tick(0.08f, 2000f * p, 0.4f, r, 0f)); return b; }
                default: return SoundKit.Tick(0.025f, 2800f * p, 0.35f, r, 0.1f);   // Slider
            }
        }
    }
}
