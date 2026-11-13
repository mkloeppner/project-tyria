using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Die fünf Effekt-Phasen jedes Skills (Grafik-Lauf G5).</summary>
    public enum VfxPhase { Aufladen, Auswurf, Flug, Einschlag, Nachwirkung }

    /// <summary>Lagen-Arten: Komposition aus dem Baukasten (Form/Palette/Größe), Licht, Schockwelle (Verzerrung), Waffen-Spur, Kamera-Impuls.</summary>
    /// <summary>V2+: Sim (Simulations-Flipbook), FxMesh (prozedurale Form), Refract (Verzerrungs-Puffer), WorldMark (Spur am Boden), Gpu (GPU-Partikel mit Fallback).</summary>
    public enum VfxLayerKind { Composite, Light, Shockwave, Trail, CameraKick, Sim, FxMesh, Refract, WorldMark, Gpu }

    public enum VfxTier { Standard, Elite, Finisher }

    [System.Serializable]
    public struct VfxLayer
    {
        public VfxLayerKind Kind;
        [Tooltip("Komposition „Form/Palette/Größe“ (nur Composite)")] public string Id;
        public Color Color;
        [Tooltip("Licht: Intensität · Schockwelle: Stärke · Kamera: Amplitude")] public float Strength;
        [Tooltip("Licht-Reichweite bzw. Schockwellen-Radius (m)")] public float Radius;
        public float Duration;
        [Tooltip("Verzögerung ab Phasenbeginn (s)")] public float Delay;
        public static VfxLayer Comp(string id, float delay = 0f) => new VfxLayer { Kind = VfxLayerKind.Composite, Id = id, Delay = delay };
        public static VfxLayer LightPulse(Color c, float intensity, float range, float duration, float delay = 0f) => new VfxLayer { Kind = VfxLayerKind.Light, Color = c, Strength = intensity, Radius = range, Duration = duration, Delay = delay };
        public static VfxLayer Shock(float radius, float strength, float duration = 0.35f) => new VfxLayer { Kind = VfxLayerKind.Shockwave, Radius = radius, Strength = strength, Duration = duration };
    }

    [System.Serializable]
    public class VfxPhaseLayers
    {
        public VfxPhase Phase;
        public List<VfxLayer> Layers = new List<VfxLayer>();
    }

    /// <summary>
    /// Grafik-Lauf G5: VFX 2.0 eines Skills – Element, Stufe (Standard/Elite/Abschluss) und je Phase eine Liste von Lagen.
    /// Mindestens 3 Lagen (Elite 5, Abschluss 6), jede der fünf Phasen ist belegt. Erweitert die Zeitachsen-Kennungen aus Lauf 5.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/VFX Definition")]
    public class VfxDefinition : ScriptableObject
    {
        public string SkillId;
        public FxElement Element;
        public VfxPalette Palette;
        public VfxTier Tier;
        public List<VfxPhaseLayers> Phases = new List<VfxPhaseLayers>();

        public VfxPhaseLayers Get(VfxPhase p) { foreach (var x in Phases) if (x != null && x.Phase == p) return x; return null; }
        public int LayerCount { get { int n = 0; foreach (var p in Phases) if (p != null) n += p.Layers.Count; return n; } }

        public static int MinLayers(VfxTier t) => t == VfxTier.Finisher ? 6 : t == VfxTier.Elite ? 5 : 3;

        /// <summary>Regelprüfung (rein): fünf Phasen belegt, Mindest-Lagen, Kompositionen gültig, keine Gefahren-Farbe als Licht.</summary>
        public List<string> Problems()
        {
            var list = new List<string>();
            foreach (VfxPhase ph in System.Enum.GetValues(typeof(VfxPhase)))
            {
                var p = Get(ph);
                if (p == null || p.Layers.Count == 0) list.Add("Phase leer: " + ph);
            }
            if (LayerCount < MinLayers(Tier)) list.Add("Lagen " + LayerCount + " < " + MinLayers(Tier) + " (" + Tier + ")");
            foreach (var p in Phases)
                foreach (var l in p.Layers)
                {
                    if (l.Kind == VfxLayerKind.Composite && !VfxIds.TryParse(l.Id, out _, out _, out _)) list.Add("ungültige Komposition: " + l.Id);
                    if (l.Kind == VfxLayerKind.Light && l.Strength > 6f) list.Add("Licht zu hell: " + l.Strength);
                }
            return list;
        }
    }
}
