using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Emotes (/winken …) – Clip am Spieler, Meldung an Herzen (Emote-Aufgaben) und Story.</summary>
    public static class EmoteRunner
    {
        public static event System.Action<Combatant, Emote> Performed;

        public static void Play(Combatant who, Emote e)
        {
            if (who == null || e == Emote.None) return;
            var drv = who.GetComponentInChildren<AnimDriver>(); var anim = drv != null ? drv.Anim : who.GetComponentInChildren<CharacterAnimator>();
            if (anim != null) anim.PlayAuto(EmoteModel.Clip(e), 0.2f, 1f, EmoteModel.Loops(e));
            Performed?.Invoke(who, e);
        }
    }
}
