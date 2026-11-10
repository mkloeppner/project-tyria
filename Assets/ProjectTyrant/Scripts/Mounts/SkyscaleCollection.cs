using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 10: Sammlung „Himmelsschuppe“ (rein, getestet) in drei Schritten: 1 Ei im Horst auf dem höchsten Gipfel der Himmelsspitzen bergen,
    /// 2 Ausbrüten – fünf Glutsteine sammeln und der Brutpflegerin bringen, 3 Sattel bei der Brutpflegerin kaufen → Himmelsschuppe freigeschaltet.
    /// Fortschritt als Einträge in <see cref="ProfileState.Completed"/>.
    /// </summary>
    public static class SkyscaleCollection
    {
        public const string Egg = "sky_egg", Hatch = "sky_hatch", Saddle = "sky_saddle";
        public const int Stones = 5;
        public const long SaddleCopper = 3 * 10000;   // 3 Gold
        public enum Result { Ok, WrongStep, Already, NeedStones, NeedGold }

        public static string StoneId(int i) => "sky_stone_" + i;

        public static int StonesFound(ICollection<string> c) { int n = 0; for (int i = 0; i < Stones; i++) if (c.Contains(StoneId(i))) n++; return n; }

        /// <summary>0 = Ei suchen, 1 = Ausbrüten (Glutsteine), 2 = Sattel, 3 = fertig.</summary>
        public static int Step(ICollection<string> c) => c.Contains(Saddle) ? 3 : c.Contains(Hatch) ? 2 : c.Contains(Egg) ? 1 : 0;

        public static string StepText(ICollection<string> c)
        {
            switch (Step(c))
            {
                case 0: return "Himmelsschuppe 1/3: Finde das Ei im Horst auf dem höchsten Gipfel der Himmelsspitzen.";
                case 1: return "Himmelsschuppe 2/3: Sammle Glutsteine zum Ausbrüten (" + StonesFound(c) + "/" + Stones + ") und bring sie der Brutpflegerin.";
                case 2: return "Himmelsschuppe 3/3: Kaufe den Sattel bei der Brutpflegerin (" + WalletModel.Format(SaddleCopper) + ").";
                default: return "Himmelsschuppe: abgeschlossen.";
            }
        }

        public static Result TakeEgg(ICollection<string> c)
        {
            if (c.Contains(Egg)) return Result.Already;
            c.Add(Egg); return Result.Ok;
        }

        public static Result PickStone(ICollection<string> c, int i)
        {
            if (Step(c) != 1) return Result.WrongStep;
            if (c.Contains(StoneId(i))) return Result.Already;
            c.Add(StoneId(i)); return Result.Ok;
        }

        public static Result HatchEgg(ICollection<string> c)
        {
            int s = Step(c);
            if (s == 0) return Result.WrongStep;
            if (s >= 2) return Result.Already;
            if (StonesFound(c) < Stones) return Result.NeedStones;
            c.Add(Hatch); return Result.Ok;
        }

        public static Result BuySaddle(ICollection<string> c, WalletModel w)
        {
            int s = Step(c);
            if (s < 2) return Result.WrongStep;
            if (s == 3) return Result.Already;
            if (w == null || !w.TrySpendCopper(SaddleCopper)) return Result.NeedGold;
            c.Add(Saddle); return Result.Ok;
        }
    }
}
