using System.Collections;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G5: /vfx play und Galerie spielen alle fünf Phasen einer VFX-2.0-Definition mit typischem Zeitversatz.</summary>
    public static partial class VfxPlayer
    {
        public static readonly float[] PhaseOffsets = { 0f, 0.3f, 0.35f, 0.65f, 0.8f };

        static partial void PlayV2(SkillDefinition s, Vector3 from, Vector3 to, Transform owner, ref bool handled)
        {
            if (s == null || s.Vfx2 == null) return;
            var gm = GameManager.Instance;
            if (gm == null || !Application.isPlaying) { VfxLayerPlayer.Play(s.Vfx2, VfxPhase.Einschlag, to, Quaternion.identity, null, 1f); handled = true; return; }
            gm.StartCoroutine(Sequence(s.Vfx2, from, to, owner));
            handled = true;
        }

        static IEnumerator Sequence(VfxDefinition d, Vector3 from, Vector3 to, Transform owner)
        {
            var rot = owner != null ? owner.rotation : Quaternion.identity;
            VfxLayerPlayer.Play(d, VfxPhase.Aufladen, from, rot, owner, 1f);
            yield return new WaitForSeconds(PhaseOffsets[1]);
            VfxLayerPlayer.Play(d, VfxPhase.Auswurf, from, rot, null, 1f, true);
            yield return new WaitForSeconds(PhaseOffsets[2] - PhaseOffsets[1]);
            VfxLayerPlayer.Play(d, VfxPhase.Flug, Vector3.Lerp(from, to, 0.5f), rot, null, 1f);
            yield return new WaitForSeconds(PhaseOffsets[3] - PhaseOffsets[2]);
            VfxLayerPlayer.Play(d, VfxPhase.Einschlag, to, Quaternion.identity, null, 1f);
            yield return new WaitForSeconds(PhaseOffsets[4] - PhaseOffsets[3]);
            VfxLayerPlayer.Play(d, VfxPhase.Nachwirkung, to, Quaternion.identity, null, 1f);
        }
    }
}
