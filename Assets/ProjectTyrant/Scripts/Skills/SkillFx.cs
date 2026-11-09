using System;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M22: Abspiel-Schnittstelle der VFX- und SFX-Spuren einer Skill-Zeitachse. Der VFX-Baukasten (M23) und die Audio-Engine (M24)
    /// hängen sich hier ein; bis dahin nutzen die Standard-Handler die bestehenden Pulse/Klänge. Zählt Aufrufe für Tests/QA.
    /// </summary>
    public static class SkillFx
    {
        public static Action<string, Vector3, Quaternion, Transform> VfxHandler;
        public static Action<string, Vector3, float> SfxHandler;   // Id, Position, Pegel (dB)
        public static int VfxCalls { get; private set; }
        public static int SfxCalls { get; private set; }
        public static string LastVfx { get; private set; }
        public static string LastSfx { get; private set; }

        /// <summary>Intensität des laufenden Aufrufs (Spieler 0,75, Verbündete 0,6, Gegner 1) – Lesbarkeits-Regel.</summary>
        public static float CurrentIntensity { get; private set; } = 1f;
        public static float TeamIntensity(Team t) => t == Team.Player ? 0.75f : t == Team.Ally ? 0.6f : 1f;

        public static void Vfx(string id, Vector3 pos, Quaternion rot, Transform follow = null, float intensity = 1f)
        {
            if (string.IsNullOrEmpty(id)) return;
            VfxCalls++; LastVfx = id; CurrentIntensity = intensity;
            if (VfxHandler != null) { VfxHandler(id, pos, rot, follow); return; }
            if (!VfxIds.TryParse(id, out var shape, out var pal, out var size)) return;
            if (shape == VfxShape.Charge || shape == VfxShape.Trail || shape == VfxShape.Projectile) return;   // Standard: nur Auswurf/Einschlag als Puls
            float r = size == VfxSize.S ? 0.8f : size == VfxSize.M ? 1.4f : size == VfxSize.L ? 2.4f : 3.5f;
            SkillExecutor.VfxPulse(pos, r, PaletteColor(pal));
        }

        public static void Sfx(string id, Vector3 pos, float volumeDb = 0f)
        {
            if (string.IsNullOrEmpty(id)) return;
            SfxCalls++; LastSfx = id;
            SfxHandler?.Invoke(id, pos, volumeDb);
        }

        /// <summary>Lauf 5 / M24: Effekte −10 dB (Spieler) / −14 dB (Gegner): Gegner und Verbündete spielen 4 dB leiser.</summary>
        public static float TeamVolumeDb(Team t) => t == Team.Player ? 0f : -4f;

        public static Color PaletteColor(VfxPalette p)
        {
            switch (p)
            {
                case VfxPalette.Feuer: return new Color(1f, 0.35f, 0.08f);
                case VfxPalette.Wasser: return new Color(0.25f, 0.65f, 1f);
                case VfxPalette.Luft: return new Color(0.62f, 0.52f, 1f);
                case VfxPalette.Erde: return new Color(0.6f, 0.45f, 0.25f);
                case VfxPalette.Natur: return new Color(0.45f, 0.8f, 0.3f);
                case VfxPalette.Schatten: return new Color(0.45f, 0.4f, 0.55f);
                case VfxPalette.Licht: return new Color(0.98f, 0.82f, 0.35f);
                case VfxPalette.Blut: return new Color(0.75f, 0.12f, 0.15f);
                case VfxPalette.Arkan: return new Color(0.85f, 0.35f, 0.9f);
                case VfxPalette.Frost: return new Color(0.55f, 0.85f, 1f);
                case VfxPalette.Mondlicht: return new Color(0.8f, 0.88f, 1f);
                case VfxPalette.Wildblut: return new Color(0.7f, 0.3f, 0.15f);
                case VfxPalette.Dorngift: return new Color(0.5f, 0.9f, 0.2f);
                case VfxPalette.Himmel: return new Color(0.55f, 0.82f, 1f);
                case VfxPalette.Gewitter: return new Color(0.88f, 0.9f, 1f);
                case VfxPalette.Zwielicht: return new Color(0.8f, 0.5f, 0.75f);
                case VfxPalette.Jade: return new Color(0.35f, 0.9f, 0.55f);
                case VfxPalette.Lava: return new Color(1f, 0.42f, 0.08f);
                case VfxPalette.Pulver: return new Color(0.85f, 0.15f, 0.15f);
                case VfxPalette.Staubgold: return new Color(0.92f, 0.75f, 0.4f);
                case VfxPalette.Tiefviolett: return new Color(0.5f, 0.2f, 0.8f);
                case VfxPalette.Stahl: return new Color(0.55f, 0.88f, 0.95f);
                case VfxPalette.Richtglut: return new Color(1f, 0.7f, 0.25f);
                case VfxPalette.Bollwerk: return new Color(0.6f, 0.75f, 1f);
                case VfxPalette.Sturmblau: return new Color(0.3f, 0.6f, 1f);
                case VfxPalette.Smaragd: return new Color(0.3f, 0.85f, 0.45f);
                default: return new Color(0.95f, 0.5f, 0.2f);
            }
        }
    }
}
