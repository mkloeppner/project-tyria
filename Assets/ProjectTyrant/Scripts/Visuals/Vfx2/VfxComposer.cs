using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G5: setzt die VFX-2.0-Lagen eines Skills aus seinen Daten zusammen (rein, testbar). Die bisherigen Zeitachsen-Kennungen
    /// bleiben die Grundlage jeder Phase; dazu kommen Element-Kern, Licht, Spur, Schockwelle und Nachglühen nach Stufe.
    /// </summary>
    public static class VfxComposer
    {
        public struct Input
        {
            public string SkillId; public VfxPalette Palette; public SkillDelivery Delivery; public SkillSlotType Slot; public bool Finisher;
            public float Radius; public string Charge, Release, Projectile, Impact, After;
            /// <summary>V2+: Element-Baukasten (Keins = nur G5-Lagen), Skill-Typ und Schaustück.</summary>
            public ElementSim.SimElement KitElement; public ElementSim.FxSkillType Type; public bool Showpiece;
        }

        public static VfxTier TierOf(Input i) => i.Finisher ? VfxTier.Finisher : i.Slot == SkillSlotType.Elite ? VfxTier.Elite : VfxTier.Standard;

        static VfxSize SizeOf(Input i, VfxTier t) => t == VfxTier.Elite ? VfxSize.Elite : i.Radius >= 5f ? VfxSize.L : i.Radius >= 2.5f ? VfxSize.M : VfxSize.S;

        public static void Fill(VfxDefinition d, Input i)
        {
            var t = TierOf(i);
            var size = SizeOf(i, t);
            var pal = i.Palette;
            var el = ElementMap.Of(pal);
            Color core = SkillFxColor(pal);
            d.SkillId = i.SkillId; d.Palette = pal; d.Element = el; d.Tier = t;
            d.Phases = new List<VfxPhaseLayers>();
            string C(VfxShape s, VfxSize z) => VfxIds.Compose(s, pal, z);
            VfxSize small = size == VfxSize.Elite ? VfxSize.L : size == VfxSize.L ? VfxSize.M : VfxSize.S;

            // Aufladen: Energie sammelt sich (Vorbereitung 60–300 ms)
            var charge = Phase(d, VfxPhase.Aufladen);
            charge.Add(VfxLayer.Comp(Or(i.Charge, C(VfxShape.Charge, small))));
            if (t != VfxTier.Standard) charge.Add(VfxLayer.Comp(C(VfxShape.Sigil, small), 0.05f));
            charge.Add(VfxLayer.LightPulse(core, t == VfxTier.Standard ? 1.2f : 2f, 4f, 0.35f));

            // Auswurf: Spitze
            var rel = Phase(d, VfxPhase.Auswurf);
            rel.Add(VfxLayer.Comp(Or(i.Release, C(VfxShape.Release, small))));
            if (i.Delivery == SkillDelivery.Melee || i.Delivery == SkillDelivery.Leap) rel.Add(new VfxLayer { Kind = VfxLayerKind.Trail, Color = core, Duration = 0.35f });
            if (t != VfxTier.Standard) rel.Add(VfxLayer.LightPulse(core, 3f, 7f, 0.2f));

            // Flug: Projektil, Strahl oder Weg
            var fly = Phase(d, VfxPhase.Flug);
            if (i.Delivery == SkillDelivery.Projectile) fly.Add(VfxLayer.Comp(Or(i.Projectile, C(VfxShape.Projectile, small))));
            else if (i.Delivery == SkillDelivery.Leap) fly.Add(VfxLayer.Comp(C(VfxShape.Trail, small)));
            else if (i.Delivery == SkillDelivery.GroundTarget) fly.Add(VfxLayer.Comp(C(VfxShape.Pillar, small), 0.05f));
            else fly.Add(VfxLayer.Comp(C(i.Delivery == SkillDelivery.Self ? VfxShape.Aura : VfxShape.Cone, small)));
            if (t == VfxTier.Finisher) fly.Add(VfxLayer.LightPulse(core, 1.5f, 5f, 0.4f));

            // Einschlag: Spitze am Ziel
            var hit = Phase(d, VfxPhase.Einschlag);
            hit.Add(VfxLayer.Comp(Or(i.Impact, C(VfxShape.Impact, size))));
            hit.Add(VfxLayer.LightPulse(core, t == VfxTier.Standard ? 1.6f : 3.2f, t == VfxTier.Standard ? 4f : 8f, 0.18f));
            if (t != VfxTier.Standard || size == VfxSize.L) hit.Add(VfxLayer.Shock(Mathf.Max(3f, i.Radius), t == VfxTier.Standard ? 0.6f : 1f));
            if (t == VfxTier.Finisher) hit.Add(new VfxLayer { Kind = VfxLayerKind.CameraKick, Strength = 0.12f, Duration = 0.06f });

            // Nachwirkung: Nachklang länger als die Spitze
            var after = Phase(d, VfxPhase.Nachwirkung);
            after.Add(VfxLayer.Comp(Or(i.After, C(i.Radius >= 2.5f ? VfxShape.Veil : VfxShape.Aura, small)), 0.15f));
            if (t != VfxTier.Standard) after.Add(VfxLayer.Comp(C(VfxShape.Shockwave, small), 0.05f));

            // V2+: Element-Baukasten ergänzt die Lagen (Phasen bleiben)
            if (i.KitElement != ElementSim.SimElement.Keins)
                ElementSim.ElementKits.Append(d, new ElementSim.ElementKits.KitInput { Element = i.KitElement, Type = i.Type, Showpiece = i.Showpiece, Radius = i.Radius, Core = core, Tier = t });
        }

        static List<VfxLayer> Phase(VfxDefinition d, VfxPhase p) { var x = new VfxPhaseLayers { Phase = p }; d.Phases.Add(x); return x.Layers; }
        static string Or(string a, string b) => string.IsNullOrEmpty(a) ? b : a;

        /// <summary>Lichtfarbe einer Palette (Kern des Elements), ohne die reservierte Gefahren-Farbe zu treffen.</summary>
        public static Color SkillFxColor(VfxPalette p)
        {
            var c = SkillFx.PaletteColor(p);
            if (DangerLanguage.Conflicts(c)) c = Color.Lerp(c, Color.white, 0.35f);
            return c;
        }
    }
}
