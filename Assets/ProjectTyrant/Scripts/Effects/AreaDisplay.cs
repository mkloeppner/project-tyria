using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Fix-Lauf F1: Ebene einer Boden-Komposition – Flächenrahmen (Ring, Rand, Füllscheibe, Siegel) oder Effekt (Partikel, Licht).</summary>
    public enum AreaLayer { AreaOutline, AreaEffect }


    /// <summary>
    /// Fix-Lauf F1 – Flächeneffekt-Anzeige (rein, getestet): eigene Skills „Fläche und Effekt“ / „Nur Effekt“, Verbündete „Voll / Nur Effekt /
    /// Reduziert / Aus“, Gegner immer voll (Telegraphen sind Spielinformation, eigener Schalter).
    /// </summary>
    public static class AreaDisplay
    {
        public enum Owner { Player, Ally, Enemy }
        public struct Visibility { public bool Outline, Effect; public float Alpha; }

        /// <summary>Besitzer aus der Team-Intensität des Aufrufs (SkillFx.TeamIntensity: Spieler 0,75, Verbündete 0,6, sonst 1).</summary>
        public static Owner FromIntensity(float i) => Mathf.Abs(i - 0.75f) < 0.005f ? Owner.Player : Mathf.Abs(i - 0.6f) < 0.005f ? Owner.Ally : Owner.Enemy;
        public static Owner FromTeam(Team t) => t == Team.Player ? Owner.Player : t == Team.Ally ? Owner.Ally : Owner.Enemy;

        public static Visibility For(Owner o, int own, int allies)
        {
            switch (o)
            {
                case Owner.Player: return new Visibility { Outline = own == 0, Effect = true, Alpha = 1f };
                case Owner.Ally:
                    switch (allies)
                    {
                        case 0: return new Visibility { Outline = true, Effect = true, Alpha = 1f };
                        case 1: return new Visibility { Outline = false, Effect = true, Alpha = 1f };
                        case 3: return new Visibility { Outline = false, Effect = false, Alpha = 0f };
                        default: return new Visibility { Outline = true, Effect = true, Alpha = 0.5f };   // Reduziert
                    }
                default: return new Visibility { Outline = true, Effect = true, Alpha = 1f };
            }
        }

        public static Visibility Current(Owner o) { var g = GameOptions.Current; return For(o, g != null ? g.AreaOwn : 0, g != null ? g.AreaAllies : 2); }
    }
}
