using System;

namespace ProjectTyrant
{
    /// <summary>Geldbörse: Kupfer (1 g = 100 s = 10.000 k) und Karma.</summary>
    [Serializable]
    public class WalletModel
    {
        public long Copper;
        public long Karma;
        public event Action Changed;

        public static long FromGold(int gold, int silver = 0, int copper = 0) => gold * 10000L + silver * 100L + copper;
        public static void Split(long copper, out long gold, out int silver, out int cop)
        {
            gold = copper / 10000; silver = (int)(copper / 100 % 100); cop = (int)(copper % 100);
        }

        public static string Format(long copper)
        {
            Split(Math.Abs(copper), out long g, out int s, out int c);
            string sign = copper < 0 ? "−" : "";
            if (g > 0) return sign + g + " g " + s + " s " + c + " k";
            if (s > 0) return sign + s + " s " + c + " k";
            return sign + c + " k";
        }

        public void AddCopper(long amount) { if (amount == 0) return; Copper = Math.Max(0, Copper + amount); Changed?.Invoke(); }
        public void AddKarma(long amount) { if (amount == 0) return; Karma = Math.Max(0, Karma + amount); Changed?.Invoke(); }
        public bool TrySpendCopper(long amount) { if (amount < 0 || Copper < amount) return false; Copper -= amount; Changed?.Invoke(); return true; }
        public bool TrySpendKarma(long amount) { if (amount < 0 || Karma < amount) return false; Karma -= amount; Changed?.Invoke(); return true; }
    }
}
