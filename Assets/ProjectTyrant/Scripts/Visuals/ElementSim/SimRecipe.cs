using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    public enum FlipbookMode { Burst, Loop }

    /// <summary>
    /// Rezept für ein Simulations-Flipbook (V1). Liegt unter Generated/Flipbooks/; der Baker erzeugt daraus vier Texturen
    /// (Farbe, Normalen, Bewegungsvektoren halbauflösend, Emissiv-Kern). Hash der Parameter = Grundlage für inkrementelles Backen,
    /// Hash des Ergebnisses = Nachweis der Reproduzierbarkeit.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Tyrant/VFX/Sim Recipe")]
    public class SimRecipe : ScriptableObject
    {
        public string Id = "test";
        public string Element = "Feuer";
        public int Seed = 1;
        public FlipbookMode Mode = FlipbookMode.Burst;
        [Range(32, 512)] public int Grid = 128;
        [Range(4, 8)] public int Columns = 8;
        [Range(4, 8)] public int Rows = 8;
        [Range(32, 256)] public int TileSize = 128;
        [Range(1, 8)] public int StepsPerFrame = 2;
        public int WarmupSteps = 0;
        public float Dt = 1f / 48f;
        public int Iterations = 20;

        [Header("Physik")]
        public float Buoyancy = 1.2f, Gravity = 0f, Weight = 0.05f, Vorticity = 0f, Cooling = 0.6f, DensityFade = 0.15f;
        public float Viscosity = 0.00001f, Diffusion = 0.00002f, Damping = 0.02f;
        /// <summary>Boden-Hindernis bis zu dieser Höhe (0 = keins), für Spritzer gegen den Boden.</summary>
        public float FloorHeight = 0f;
        public List<SimEmitter> Emitters = new List<SimEmitter>();

        [Header("Darstellung")]
        /// <summary>Feuer: Temperatur → Farbe; sonst Dichte → Farbe.</summary>
        public bool ColorFromTemperature = false;
        public Gradient Ramp = DefaultRamp();
        public float AlphaScale = 2f, NormalStrength = 6f, MaxVelocity = 1.5f, CoreThreshold = 0.7f, Exposure = 1.2f;

        [Header("Ergebnis (vom Baker gesetzt)")]
        public string ParamHash = "";
        public string ResultHash = "";
        public float LoopSeam = -1f;
        public float PeakTime = -1f;

        public int Frames => Columns * Rows;
        public int AtlasSize => Columns * TileSize;
        /// <summary>Wiedergabe-Bildrate der Vorlage (Regel 5: nie unter 24 FPS).</summary>
        public float PlaybackFps => 1f / (Dt * StepsPerFrame);

        public static Gradient DefaultRamp()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.2f, 0.05f, 0.1f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.45f), new GradientColorKey(new Color(1f, 0.95f, 0.7f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        /// <summary>Hash aller Parameter, die das Ergebnis beeinflussen (FNV-1a, kulturunabhängig).</summary>
        public string ComputeParamHash()
        {
            var sb = new StringBuilder();
            void A(object o) { sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture)).Append('|'); }
            A(Seed); A(Mode); A(Grid); A(Columns); A(Rows); A(TileSize); A(StepsPerFrame); A(WarmupSteps); A(Dt); A(Iterations);
            A(Buoyancy); A(Gravity); A(Weight); A(Vorticity); A(Cooling); A(DensityFade); A(Viscosity); A(Diffusion); A(Damping); A(FloorHeight);
            foreach (var e in Emitters) { A(e.Shape); A(e.X); A(e.Y); A(e.X2); A(e.Y2); A(e.Radius); A(e.Density); A(e.Temperature); A(e.VelX); A(e.VelY); A(e.Start); A(e.End); A(e.Jitter); }
            A(ColorFromTemperature); A(AlphaScale); A(NormalStrength); A(MaxVelocity); A(CoreThreshold); A(Exposure); A(FlipbookBaker.Version);
            if (Ramp != null) { foreach (var k in Ramp.colorKeys) { A(k.color.r); A(k.color.g); A(k.color.b); A(k.time); } foreach (var k in Ramp.alphaKeys) { A(k.alpha); A(k.time); } }
            return Fnv.Hex(Fnv.Hash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        public bool NeedsBake => string.IsNullOrEmpty(ResultHash) || ParamHash != ComputeParamHash();

        /// <summary>Prüfungen beim Backen (Budget-Regeln aus dem Auftrag).</summary>
        public List<string> Problems()
        {
            var p = new List<string>();
            if (AtlasSize > 2048) p.Add("Atlas " + AtlasSize + " > 2048");
            if (Frames > 64) p.Add("mehr als 8×8 Kacheln");
            if (PlaybackFps < 24f - 0.01f) p.Add("Wiedergabe " + PlaybackFps.ToString("0.0") + " FPS < 24");
            if (Emitters.Count == 0) p.Add("kein Emitter");
            if (Grid < 128 && Mode == FlipbookMode.Loop) p.Add("Loop-Rezepte brauchen Gitter ≥ 128");
            return p;
        }
    }

    public static class Fnv
    {
        public static uint Hash(byte[] data, uint h = 2166136261u)
        {
            for (int i = 0; i < data.Length; i++) { h ^= data[i]; h *= 16777619u; }
            return h;
        }
        public static uint Hash(Color32[] px, uint h = 2166136261u)
        {
            for (int i = 0; i < px.Length; i++)
            {
                h ^= px[i].r; h *= 16777619u; h ^= px[i].g; h *= 16777619u; h ^= px[i].b; h *= 16777619u; h ^= px[i].a; h *= 16777619u;
            }
            return h;
        }
        public static string Hex(uint h) => h.ToString("x8");
    }
}
