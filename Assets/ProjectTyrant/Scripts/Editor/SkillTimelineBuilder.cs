using System.Collections.Generic;
using ProjectTyrant.Anim;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M22: leitet für jeden Skill im Projekt die Zeitachse ab (Clip + Zeitfaktor aus den Clip-Markern, Treffer-/Geschoss-Frames,
    /// Nachwirkung, Bewegungs-Regel, VFX-Kompositionen je Phase, SFX je Ereignis). Datengetrieben: neue Klassen bekommen ihre Zeitachsen automatisch.
    /// </summary>
    public static class SkillTimelineBuilder
    {
        public static string StyleFor(SkillDefinition s)
        {
            switch (s.Weapon)
            {
                case WeaponKind.Greatsword: return "Greatsword";
                case WeaponKind.Staff: return "Staff";
                default: return s.WeaponStyleOverride != null && s.WeaponStyleOverride.Length > 0 ? s.WeaponStyleOverride : "Sword";
            }
        }

        /// <summary>Palette aus dem Skill (explizit gesetzt bleibt; sonst Feld/Farbe).</summary>
        public static VfxPalette PaletteFor(SkillDefinition s)
        {
            if (s.PaletteExplicit) return s.Palette;
            switch (s.Field)
            {
                case ComboFieldType.Fire: return VfxPalette.Feuer;
                case ComboFieldType.Water: return VfxPalette.Wasser;
                case ComboFieldType.Lightning: return VfxPalette.Luft;
                case ComboFieldType.Smoke: return VfxPalette.Schatten;
            }
            if (s.Special == SkillSpecial.Bandage || s.SlotType == SkillSlotType.Downed) return VfxPalette.Blut;
            // nächstgelegene Palettenfarbe
            VfxPalette best = VfxPalette.Licht; float bd = float.MaxValue;
            foreach (var kv in PaletteColors)
            {
                Color c = s.IconColor; float d = (c.r - kv.Value.r) * (c.r - kv.Value.r) + (c.g - kv.Value.g) * (c.g - kv.Value.g) + (c.b - kv.Value.b) * (c.b - kv.Value.b);
                if (d < bd) { bd = d; best = kv.Key; }
            }
            return best;
        }

        public static readonly Dictionary<VfxPalette, Color> PaletteColors = new Dictionary<VfxPalette, Color>
        {
            { VfxPalette.Feuer, new Color(1f, 0.35f, 0.08f) }, { VfxPalette.Wasser, new Color(0.25f, 0.65f, 1f) }, { VfxPalette.Luft, new Color(0.62f, 0.52f, 1f) },
            { VfxPalette.Erde, new Color(0.6f, 0.45f, 0.25f) }, { VfxPalette.Natur, new Color(0.45f, 0.8f, 0.3f) }, { VfxPalette.Schatten, new Color(0.45f, 0.4f, 0.55f) },
            { VfxPalette.Licht, new Color(0.98f, 0.82f, 0.35f) }, { VfxPalette.Blut, new Color(0.75f, 0.12f, 0.15f) }, { VfxPalette.Arkan, new Color(0.85f, 0.35f, 0.9f) },
            { VfxPalette.Frost, new Color(0.55f, 0.85f, 1f) }, { VfxPalette.Glut, new Color(0.95f, 0.5f, 0.2f) },
        };

        [MenuItem("Project Tyrant/Lauf 5/Skill-Zeitachsen ableiten")]
        public static void BuildAll()
        {
            var set = ClipBuilder.HumanoidSet;
            if (set == null) { ClipBuilder.BuildAll(); set = ClipBuilder.HumanoidSet; }
            // Auto-Angriffe = Slot 1 jedes Waffensets (+ Kettenglieder)
            var autoIndex = new Dictionary<SkillDefinition, int>();
            foreach (var g in AssetDatabase.FindAssets("t:WeaponSet", new[] { DataDir }))
            {
                var w = AssetDatabase.LoadAssetAtPath<WeaponSet>(AssetDatabase.GUIDToAssetPath(g));
                if (w == null || w.Skills == null || w.Skills.Length == 0 || w.Skills[0] == null) continue;
                autoIndex[w.Skills[0]] = 0;
                if (w.Skills[0].Chain != null) for (int i = 0; i < w.Skills[0].Chain.Length; i++) if (w.Skills[0].Chain[i] != null) autoIndex[w.Skills[0].Chain[i]] = i + 1;
            }
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:SkillDefinition", new[] { DataDir }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if (s == null) continue;
                bool auto = autoIndex.TryGetValue(s, out int chain);
                s.Timeline = Build(s, set, auto, chain);
                EditorUtility.SetDirty(s); n++;
            }
            Debug.Log("[ProjectTyrant] Skill-Zeitachsen: " + n);
        }

        public static SkillTimeline Build(SkillDefinition s, AnimClipSet set, bool auto, int chain)
        {
            string style = StyleFor(s);
            string clip = AnimDriver.SkillClip(s, auto, chain, style);
            var e = set != null ? set.Get(clip) : null;
            if (e == null && set != null) { clip = "Cast1H"; e = set.Get(clip); }
            float len = e != null ? e.Length : 0.6f;
            float hit = -1f, last = -1f;
            if (e != null)
                foreach (var m in e.Markers)
                    if (m.Type == Marker.Hit || m.Type == Marker.Projectile) { if (hit < 0f || m.Time < hit) hit = m.Time; if (m.Time > last) last = m.Time; }
            if (hit < 0f) hit = len * 0.5f;
            var move = s.Delivery == SkillDelivery.Leap ? SkillMoveRule.Leap
                : s.Special == SkillSpecial.Bandage || (s.Delivery == SkillDelivery.GroundTarget && s.CastTime >= 1f) || (s.Delivery == SkillDelivery.Melee && s.CastTime >= 0.75f && !auto) ? SkillMoveRule.Rooted
                : clip.EndsWith("_Thrust") ? SkillMoveRule.Lunge : SkillMoveRule.Free;
            float cast = s.CastTime;
            var t = SkillTimeline.Derive(cast, s.Hits, s.HitInterval, s.Delivery == SkillDelivery.Projectile, auto, move, s.LeapDistance, clip, len, hit, last);
            if (e != null && e.Loop)
            {
                // Schleifen-Clip (Kanal, Verbinden): über die ganze Aktivierung gedehnt, keine Nachwirkung
                t.AnimScale = Mathf.Max(t.Activation, 0.25f) / len;
                t.Aftercast = len * t.AnimScale - t.Activation;
                t.Channel = true;
            }
            var pal = PaletteFor(s); s.Palette = pal;
            var size = s.SlotType == SkillSlotType.Elite ? VfxSize.Elite : s.Coefficient >= 1.5f || s.Radius >= 4.5f ? VfxSize.L : auto ? VfxSize.S : VfxSize.M;
            t.VfxCharge = cast > 0.2f ? VfxIds.Compose(VfxShape.Charge, pal, size) : "";
            t.VfxRelease = VfxIds.Compose(s.Delivery == SkillDelivery.Self && s.Radius >= 3f ? VfxShape.Shockwave : s.Delivery == SkillDelivery.GroundTarget ? VfxShape.Sigil : VfxShape.Release, pal, size);
            t.VfxProjectile = s.Delivery == SkillDelivery.Projectile ? VfxIds.Compose(VfxShape.Projectile, pal, size) : s.Delivery == SkillDelivery.Melee ? VfxIds.Compose(VfxShape.Trail, pal, size) : "";
            t.VfxImpact = VfxIds.Compose(s.Delivery == SkillDelivery.GroundTarget ? (s.Field != ComboFieldType.None ? VfxShape.GroundCircle : VfxShape.Pillar) : s.DealsDamage ? VfxShape.Impact : VfxShape.Aura, pal, size);
            t.VfxAfter = s.Field != ComboFieldType.None ? VfxIds.Compose(VfxShape.GroundCircle, pal, size) : s.SelfBuffs.Count > 0 ? VfxIds.Compose(VfxShape.Aura, pal, VfxSize.S) : "";
            t.SfxCharge = cast > 0.2f ? VfxIds.Sfx(pal, "Charge") : "";
            t.SfxRelease = VfxIds.Sfx(pal, auto ? "Swing" + (chain + 1) : "Release");
            t.SfxImpact = VfxIds.Sfx(pal, s.DealsDamage ? "Impact" : "Buff");
            return t;
        }
    }
}
