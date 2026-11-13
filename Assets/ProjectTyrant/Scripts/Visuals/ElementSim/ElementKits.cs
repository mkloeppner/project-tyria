using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>
    /// VFX-Lauf V2+: Element-Baukästen als reine Lagen-Listen je Skill-Typ (testbar). Sie ergänzen die G5-Lagen eines Skills,
    /// ersetzen nie die Phasen-Struktur. Lagen-Kennung „element/inhalt“: Sim = Flipbook-Rezept, FxMesh = FxMeshKit-Form,
    /// Refract = Brechungs-Form, WorldMark = Spur am Boden (Pfütze, Brandspur …), Gpu = GPU-Partikel-Vorlage.
    /// </summary>
    public static class ElementKits
    {
        public struct KitInput { public SimElement Element; public FxSkillType Type; public bool Showpiece; public float Radius; public Color Core; public VfxTier Tier; }

        public static readonly Color WaterBody = new Color(0.25f, 0.62f, 0.95f), WaterHalo = new Color(0.35f, 0.9f, 0.85f), WaterCore = new Color(2.2f, 2.6f, 3f);

        static VfxLayer L(VfxLayerKind k, string id, float radius, float duration, float strength = 1f, float delay = 0f, Color? c = null) =>
            new VfxLayer { Kind = k, Id = id, Radius = radius, Duration = duration, Strength = strength, Delay = delay, Color = c ?? Color.white };

        /// <summary>Lagen je Phase für das Element; leer, wenn das Element (noch) keinen Baukasten hat.</summary>
        public static Dictionary<VfxPhase, List<VfxLayer>> Layers(KitInput i)
        {
            var d = new Dictionary<VfxPhase, List<VfxLayer>>();
            foreach (VfxPhase p in System.Enum.GetValues(typeof(VfxPhase))) d[p] = new List<VfxLayer>();
            if (i.Element == SimElement.Wasser) Water(i, d);
            return d;
        }

        public static int Count(Dictionary<VfxPhase, List<VfxLayer>> d) { int n = 0; foreach (var kv in d) n += kv.Value.Count; return n; }

        /// <summary>Wasser (Auftrag Abschnitt 6): Tropfen sammeln sich, Spritzer, Linsen-Brechung, Spritzkrone, Wellenring, Pfütze, Tropfen.</summary>
        static void Water(KitInput i, Dictionary<VfxPhase, List<VfxLayer>> d)
        {
            float r = Mathf.Clamp(i.Radius, 1f, 8f);
            switch (i.Type)
            {
                case FxSkillType.Projektil:
                case FxSkillType.Strahl:
                case FxSkillType.Kette:
                    d[VfxPhase.Aufladen].Add(L(VfxLayerKind.Gpu, "wasser/sammeln", 0.8f, 0.3f));
                    d[VfxPhase.Auswurf].Add(L(VfxLayerKind.Sim, "wasser/wasser_spritzer_burst", 0.8f, 0.6f));
                    d[VfxPhase.Auswurf].Add(L(VfxLayerKind.FxMesh, "wasser/WaveRing", 0.9f, 0.5f));
                    d[VfxPhase.Flug].Add(L(VfxLayerKind.Refract, "wasser/LensSphere", 0.6f, 0.35f, 0.7f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/SplashCrown", 1.2f, 0.6f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.Sim, "wasser/wasser_spritzer_burst", 1.6f, 0.8f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/WaveRing", 2f, 0.9f, 1f, 0.05f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.WorldMark, "wasser/pfuetze", 1.2f, 12f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.Gpu, "wasser/tropfen", 1f, 1f));
                    break;
                case FxSkillType.Flaeche:
                case FxSkillType.Heilung:
                    d[VfxPhase.Aufladen].Add(L(VfxLayerKind.WorldMark, "wasser/nass", r, 1.2f));
                    d[VfxPhase.Auswurf].Add(L(VfxLayerKind.FxMesh, "wasser/WaterColumn", Mathf.Min(r * 0.4f, 2f), 0.8f));
                    d[VfxPhase.Flug].Add(L(VfxLayerKind.Sim, "wasser/wasser_gischt_loop", Mathf.Min(r * 0.4f, 1.5f), 1.2f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/WaveRing", r, 1.2f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.Sim, "wasser/wasser_kaustik_loop", r, 2.5f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.Gpu, "wasser/gischt", r * 0.5f, 1.2f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.WorldMark, "wasser/pfuetze", r * 0.8f, 16f));
                    break;
                case FxSkillType.Aura:
                case FxSkillType.Blockade:
                case FxSkillType.Beschwoerung:
                    d[VfxPhase.Aufladen].Add(L(VfxLayerKind.Refract, "wasser/LensSphere", 1.3f, 1.2f, 0.4f));   // Wasserhaut, schwach
                    d[VfxPhase.Auswurf].Add(L(VfxLayerKind.Sim, "wasser/wasser_gischt_loop", 1f, 0.8f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/WaveRing", 1.5f, 0.8f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.Gpu, "wasser/tropfen", 1f, 1f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.WorldMark, "wasser/pfuetze", 0.9f, 8f));
                    break;
                default:   // Nahkampf, Sprung, Wirbel, Finisher
                    d[VfxPhase.Aufladen].Add(L(VfxLayerKind.Sim, "wasser/wasser_gischt_loop", 0.7f, 0.4f));
                    d[VfxPhase.Auswurf].Add(L(VfxLayerKind.FxMesh, i.Type == FxSkillType.Sprung ? "wasser/WaterColumn" : "wasser/BladeArc", 1.4f, 0.4f));
                    d[VfxPhase.Flug].Add(L(VfxLayerKind.Refract, "wasser/BladeArc", 1.4f, 0.3f, 0.6f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/SplashCrown", 1.3f, 0.6f));
                    d[VfxPhase.Einschlag].Add(L(VfxLayerKind.FxMesh, "wasser/WaveRing", 1.8f, 0.8f, 1f, 0.05f));
                    d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.WorldMark, "wasser/pfuetze", 1f, 10f));
                    break;
            }
            if (i.Showpiece)
            {
                // volle Behandlung: starke Brechung (Auftraggeber: „stark“), Gischt-Nebel, Kaustik, Glitzer-Licht, Spritzer-Burst
                d[VfxPhase.Einschlag].Add(L(VfxLayerKind.Refract, "wasser/WaveRing", Mathf.Max(2f, r), 1f, 1f));
                d[VfxPhase.Einschlag].Add(L(VfxLayerKind.Light, "", Mathf.Max(5f, r * 1.5f), 0.6f, 2.5f, 0f, WaterCore * 0.5f));
                d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.Gpu, "wasser/gischt", Mathf.Max(1.5f, r * 0.6f), 1.5f));
                if (i.Type != FxSkillType.Flaeche && i.Type != FxSkillType.Heilung) d[VfxPhase.Nachwirkung].Add(L(VfxLayerKind.Sim, "wasser/wasser_kaustik_loop", Mathf.Max(1.5f, r * 0.7f), 2f));
                d[VfxPhase.Auswurf].Add(L(VfxLayerKind.Sim, "wasser/wasser_spritzer_seitlich_burst", 1.2f, 0.6f));
            }
        }

        /// <summary>Hängt die Baukasten-Lagen an eine bestehende Definition (Phasen bleiben, G5-Lagen bleiben).</summary>
        public static int Append(VfxDefinition def, KitInput i)
        {
            var add = Layers(i); int n = 0;
            foreach (var kv in add)
            {
                var ph = def.Get(kv.Key);
                if (ph == null) { ph = new VfxPhaseLayers { Phase = kv.Key }; def.Phases.Add(ph); }
                foreach (var l in kv.Value) { ph.Layers.Add(l); n++; }
            }
            return n;
        }

        public static bool IsElementLayer(VfxLayerKind k) => k == VfxLayerKind.Sim || k == VfxLayerKind.FxMesh || k == VfxLayerKind.Refract || k == VfxLayerKind.WorldMark || k == VfxLayerKind.Gpu;

        /// <summary>V2-Prüfung für Skills eines umgesetzten Elements: Mindest-Lagen V1 (4 bzw. 7 bei Schaustücken), ein Simulations-Flipbook, Welt-Spur in der Nachwirkung.</summary>
        public static List<string> ProblemsV2(VfxDefinition d, bool kitElement, bool showpiece)
        {
            var list = new List<string>(); if (d == null || !kitElement) return list;
            if (d.LayerCount < SimElements.MinLayersV1(showpiece)) list.Add("Lagen " + d.LayerCount + " < " + SimElements.MinLayersV1(showpiece) + " (V1)");
            bool sim = false, mark = false;
            foreach (var p in d.Phases) if (p != null) foreach (var l in p.Layers)
                    {
                        if (l.Kind == VfxLayerKind.Sim) sim = true;
                        if (p.Phase == VfxPhase.Nachwirkung && (l.Kind == VfxLayerKind.WorldMark || l.Kind == VfxLayerKind.Gpu)) mark = true;
                    }
            if (!sim) list.Add("kein Simulations-Flipbook");
            if (!mark) list.Add("Nachwirkung ohne Welt-Kontakt");
            return list;
        }

        public static string ElementOf(string id) { int k = id != null ? id.IndexOf('/') : -1; return k > 0 ? id.Substring(0, k) : ""; }
        public static string ContentOf(string id) { int k = id != null ? id.IndexOf('/') : -1; return k >= 0 ? id.Substring(k + 1) : id ?? ""; }
    }
}
