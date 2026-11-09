using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    public enum Element { None = -1, Fire = 0, Water = 1, Air = 2, Earth = 3 }
    public enum PetFamily { Wolf, Bear, Falcon }

    /// <summary>
    /// Lauf 5 / M25 – Elementarweber: Einstimmungen F1 Feuer, F2 Wasser, F3 Luft, F4 Erde. Wechsel tauscht die Waffenskills 1–5;
    /// die verlassene Einstimmung lädt 10 s (GW2), ein globaler Wechsel-Takt von 1 s verhindert Spam.
    /// </summary>
    public class AttunementModel
    {
        public const float Recharge = 10f, Global = 1f;
        public Element Current { get; private set; } = Element.Fire;
        readonly float[] _cd = new float[4];
        float _global;
        public int Swaps { get; private set; }
        public event Action<Element, Element> Swapped;   // von, nach

        public float CooldownOf(Element e) => e == Element.None || (int)e < 0 || (int)e >= _cd.Length ? 0f : Math.Max(_cd[(int)e], e == Current ? 0f : _global);
        public bool CanSwap(Element e, out string reason)
        {
            reason = null;
            if (e == Element.None || (int)e < 0 || (int)e >= _cd.Length) { reason = "Keine Einstimmung"; return false; }
            if (e == Current) { reason = "Bereits eingestimmt"; return false; }
            if (_cd[(int)e] > 0f || _global > 0f) { reason = "Einstimmung lädt noch"; return false; }
            return true;
        }

        public bool Swap(Element e)
        {
            if (!CanSwap(e, out _)) return false;
            var from = Current; _cd[(int)from] = Recharge; _global = Global; Current = e; Swaps++;
            Swapped?.Invoke(from, e);
            return true;
        }

        /// <summary>Lauf 5 / M26 (Sturmrufer): zusätzliche Wiederaufladung einer Einstimmung nach dem Überladen.</summary>
        public void AddRecharge(Element e, float seconds) { if (e == Element.None || (int)e < 0 || (int)e >= _cd.Length || seconds <= 0f) return; _cd[(int)e] += seconds; }

        public void Tick(float dt) { for (int i = 0; i < 4; i++) _cd[i] = Math.Max(0f, _cd[i] - dt); _global = Math.Max(0f, _global - dt); }

        /// <summary>Passives Thema je Einstimmung (Effekt beim Einstimmen).</summary>
        public static string Theme(Element e) => e == Element.Fire ? "Macht" : e == Element.Water ? "Heilung/Reinigung" : e == Element.Air ? "Schnelligkeit/Krit" : "Schutz/Blutung";
        public static VfxPalette PaletteOf(Element e) => e == Element.Fire ? VfxPalette.Feuer : e == Element.Water ? VfxPalette.Wasser : e == Element.Air ? VfxPalette.Luft : VfxPalette.Erde;
        public static ComboFieldType FieldOf(Element e) => e == Element.Fire ? ComboFieldType.Fire : e == Element.Water ? ComboFieldType.Water : e == Element.Air ? ComboFieldType.Lightning : ComboFieldType.None;
    }

    /// <summary>Lauf 5 / M25 – Schattenklinge: 12 Initiative, Waffenskills 2–5 kosten Initiative (Regeneration 1/s).</summary>
    public class InitiativeModel
    {
        public const int Max = 12; public const float RegenPerSecond = 1f;
        float _value = Max;
        public float Value => _value;
        public int Points => (int)Math.Floor(_value + 1e-4f);
        public bool CanSpend(int cost) => cost <= 0 || Points >= cost;
        public bool Spend(int cost) { if (!CanSpend(cost)) return false; _value -= cost; return true; }
        public void Gain(float n) => _value = Math.Min(Max, _value + n);
        public void Tick(float dt) => _value = Math.Min(Max, _value + RegenPerSecond * dt);
        public void Reset() => _value = Max;
    }

    /// <summary>Lauf 5 / M25 – Tarnung: 3–4 s unsichtbar für Gegner; ein Angriff beendet sie und gibt 3 s „Enthüllt“ (keine neue Tarnung).</summary>
    public class StealthModel
    {
        public const float RevealedDuration = 3f;
        public float Remaining { get; private set; }
        public float Revealed { get; private set; }
        public bool InStealth => Remaining > 0f;
        public bool IsRevealed => Revealed > 0f;

        /// <summary>Tarnung betreten; „Enthüllt“ verhindert es (GW2).</summary>
        public bool Enter(float seconds) { if (IsRevealed) return false; Remaining = Math.Max(Remaining, seconds); return true; }
        /// <summary>Angriff aus der Tarnung: Tarnung endet, Enthüllt 3 s. Liefert true, wenn es ein Tarnungsangriff war.</summary>
        public bool Attack() { if (!InStealth) return false; Remaining = 0f; Revealed = RevealedDuration; return true; }
        public void Tick(float dt) { Remaining = Math.Max(0f, Remaining - dt); Revealed = Math.Max(0f, Revealed - dt); }
    }

    /// <summary>Lauf 5 / M25 – Stehlen: gestohlener Skill nach Gegner-Fraktion; ein Diebstahl je Abklingzeit, ein Skill im Speicher.</summary>
    public class StolenSkillModel
    {
        public const float StealRecharge = 21f, StealRange = 22.5f;   // 900 Einheiten ≈ 22,5 m
        public string Held { get; private set; }
        public float Cooldown { get; private set; }

        public static string SkillFor(string faction)
        {
            switch (faction)
            {
                case "Moor": return "stolen_moor";      // Giftwolke
                case "Frost": return "stolen_frost";    // Eissplitter
                case "Glut": return "stolen_glut";      // Blitz/Glutstoß
                case "Beast": return "stolen_beast";    // Fell der Bestie (Segen)
                default: return "stolen_bandit";        // Felsbrocken-Wurf
            }
        }

        public bool CanSteal(float distance, out string reason)
        {
            reason = null;
            if (Cooldown > 0f) { reason = "Stehlen lädt noch"; return false; }
            if (distance > StealRange) { reason = "Außer Reichweite"; return false; }
            return true;
        }

        public bool Steal(string faction, float distance)
        {
            if (!CanSteal(distance, out _)) return false;
            Held = SkillFor(faction); Cooldown = StealRecharge; return true;
        }

        public string UseHeld() { var h = Held; Held = null; return h; }
        public void Tick(float dt) => Cooldown = Math.Max(0f, Cooldown - dt);
    }

    /// <summary>
    /// Lauf 5 / M25 – Wildhüter: Tiergefährte. 2 aktive Gefährten, F4 wechselt (20 s Abklingzeit, außerhalb des Kampfes sofort);
    /// stirbt der Gefährte, kehrt er nach 20 s zurück (oder per F4 als anderer Gefährte). F3 schaltet Passiv/Angriff.
    /// </summary>
    public class PetModel
    {
        public const float SwapCooldown = 20f, ReviveTime = 20f;
        public readonly PetFamily[] Active = { PetFamily.Wolf, PetFamily.Bear };
        public int ActiveIndex { get; private set; }
        public PetFamily Current => Active[ActiveIndex];
        public bool Dead { get; private set; }
        public bool Passive { get; set; }
        public float SwapRemaining { get; private set; }
        public float ReviveRemaining { get; private set; }
        public event Action<PetFamily> Summoned;

        public bool CanSwap(bool inCombat, out string reason)
        {
            reason = null;
            if (inCombat && SwapRemaining > 0f) { reason = "Gefährtenwechsel lädt noch"; return false; }
            return true;
        }

        public bool Swap(bool inCombat)
        {
            if (!CanSwap(inCombat, out _)) return false;
            ActiveIndex = 1 - ActiveIndex; Dead = false; ReviveRemaining = 0f;
            if (inCombat) SwapRemaining = SwapCooldown;
            Summoned?.Invoke(Current);
            return true;
        }

        public void Died() { if (Dead) return; Dead = true; ReviveRemaining = ReviveTime; }

        public void Tick(float dt)
        {
            SwapRemaining = Math.Max(0f, SwapRemaining - dt);
            if (Dead) { ReviveRemaining -= dt; if (ReviveRemaining <= 0f) { Dead = false; ReviveRemaining = 0f; Summoned?.Invoke(Current); } }
        }

        public void SetPair(PetFamily a, PetFamily b) { Active[0] = a; Active[1] = b; }

        /// <summary>Familien-Eigenschaften (Leben-Faktor, Tempo, fliegend, F2-Skill-Id).</summary>
        public static float HealthFactor(PetFamily f) => f == PetFamily.Bear ? 1.4f : f == PetFamily.Falcon ? 0.7f : 1f;
        public static bool Flying(PetFamily f) => f == PetFamily.Falcon;
        public static string FamilySkill(PetFamily f) => f == PetFamily.Wolf ? "pet_wolf_howl" : f == PetFamily.Bear ? "pet_bear_roar" : "pet_falcon_dive";
        public static string DisplayName(PetFamily f) => f == PetFamily.Wolf ? "Wolf" : f == PetFamily.Bear ? "Bär" : "Falke";
    }
}
