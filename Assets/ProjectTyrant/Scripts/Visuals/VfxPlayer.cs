using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1 (Gerüst) / G5: spielt die Effekte eines Skills vor dem Spieler ab (/vfx play).</summary>
    public static partial class VfxPlayer
    {
        public static string PlaySkillById(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Skill-Id fehlt";
            var gm = GameManager.Instance; if (gm == null || gm.Player == null) return "Kein Spieler";
            SkillDefinition s = null;
            foreach (var sd in Resources.FindObjectsOfTypeAll<SkillDefinition>()) if (sd != null && (sd.name == id || sd.name.EndsWith("_" + id) || sd.Id == id)) { s = sd; break; }
            if (s == null) return "Skill „" + id + "“ nicht geladen";
            var p = gm.Player.transform; var target = p.position + p.forward * 8f;
            return Play(s, p.position + Vector3.up * 1.2f, target, p) ? "VFX: " + s.name : "Keine Effekte für " + s.name;
        }

        /// <summary>G1-Fallback: Zeitachsen-Spuren des Lauf-5-Baukastens. G5 ersetzt dies durch VfxKit 2.0.</summary>
        static bool PlayLegacy(SkillDefinition s, Vector3 from, Vector3 to, Transform owner)
        {
            var tl = s.Timeline; if (tl == null || !tl.IsBuilt) return false;
            SkillFx.Vfx(tl.VfxCharge, from, owner.rotation, owner, 1f);
            SkillFx.Vfx(tl.VfxRelease, from, owner.rotation, null, 1f);
            SkillFx.Vfx(tl.VfxImpact, to, Quaternion.identity, null, 1f);
            SkillFx.Vfx(tl.VfxAfter, to, Quaternion.identity, null, 1f);
            return true;
        }

        static partial void PlayV2(SkillDefinition s, Vector3 from, Vector3 to, Transform owner, ref bool handled);

        public static bool Play(SkillDefinition s, Vector3 from, Vector3 to, Transform owner)
        {
            bool handled = false; PlayV2(s, from, to, owner, ref handled);
            return handled || PlayLegacy(s, from, to, owner);
        }
    }
}
