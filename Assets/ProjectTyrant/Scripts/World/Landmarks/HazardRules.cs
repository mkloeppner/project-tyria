using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Wirkung einer Hürde zu Fuß (Abschnitt 5, Tabelle).</summary>
    public struct HazardSpec
    {
        public HazardKind Kind;
        public string Name, Edge, Solution;
        public float SpeedFactor;          // Tempo-Faktor zu Fuß
        public EffectType Condition; public int StacksPerSecond;
        public float HpPerSecond;          // Anteil der max. HP je Sekunde
        public float SinkAfter;            // Versinken/Ertrinken beginnt (s), 0 = nie
        public float LethalAfter;          // Tod nach (s), 0 = nie tödlich
        public float HpCapPerTick;         // Verderbnis: Deckel je Tick
        public bool Blocks;                // zu Fuß undurchdringlich (Dornen, Steilwand)
        public bool PushesBack;            // Windwand
        public Traversal Keys;             // Schlüssel, die die Hürde überwinden
        public Color EdgeColor;
    }

    /// <summary>Ergebnis eines Hürden-Ticks.</summary>
    public struct HazardTick
    {
        public float SpeedFactor;
        public EffectType Condition; public int Stacks;
        public float HpFraction;
        public bool Sinking, Dead, Pushback;
    }

    /// <summary>
    /// Welt-Lauf W1: Hürden-Regeln (rein, getestet). Jede tödliche Hürde lässt mindestens <see cref="MinRescueWindow"/> s zum Umkehren;
    /// am Rand erkennbar (Farbe, Partikel, Warnschild „Benötigt: …“). Reittiere mit passendem Schlüssel sind unberührt.
    /// </summary>
    public static class HazardRules
    {
        public const float MinRescueWindow = 4f;
        public const float DodgeEscape = 1f;   // Treibsand: Ausweichen gibt 1 m Schub zum Rand

        public static HazardSpec Spec(HazardKind k)
        {
            switch (k)
            {
                case HazardKind.Quicksand: return new HazardSpec { Kind = k, Name = "Treibsand", Edge = "sandfarbene Wirbel, Rand-Rinnen", Solution = "Skimmer (schwebt), Raptor über schmale Streifen, Flug", SpeedFactor = 0.4f, SinkAfter = 4f, LethalAfter = 8f, Keys = Traversal.Skimmer | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.86f, 0.72f, 0.42f) };
                case HazardKind.ToxicWater: return new HazardSpec { Kind = k, Name = "Giftwasser", Edge = "grün schimmernd, Blasen, Totenkopf am Ufer", Solution = "Skimmer, Flug", SpeedFactor = 0.6f, Condition = EffectType.Poison, StacksPerSecond = 3, HpPerSecond = 0.06f, Keys = Traversal.Skimmer | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.45f, 0.95f, 0.35f) };
                case HazardKind.Lava: return new HazardSpec { Kind = k, Name = "Lava", Edge = "Glühen, Hitzeflimmern, Rauch", Solution = "Skimmer mit Glutschritt (Umwelt 3), Flug", SpeedFactor = 0.7f, Condition = EffectType.Burning, StacksPerSecond = 5, HpPerSecond = 0.10f, LethalAfter = 10f, Keys = Traversal.LavaSkimmer | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(1f, 0.42f, 0.1f) };
                case HazardKind.IceWater: return new HazardSpec { Kind = k, Name = "Eiswasser", Edge = "dunkelblau, Eisschollen, Atemnebel", Solution = "Skimmer, Flug, Eisbrücken", SpeedFactor = 0.5f, Condition = EffectType.Chill, StacksPerSecond = 10, SinkAfter = 4f, LethalAfter = 8f, Keys = Traversal.Skimmer | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.3f, 0.5f, 0.95f) };
                case HazardKind.Abyss: return new HazardSpec { Kind = k, Name = "Abgrund", Edge = "sichtbare Tiefe, Windpartikel", Solution = "Raptor (Sprung), Greif, Himmelsschuppe, Springer von unten", SpeedFactor = 1f, Keys = Traversal.Raptor | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.35f, 0.35f, 0.4f) };
                case HazardKind.SteepWall: return new HazardSpec { Kind = k, Name = "Steilwand", Edge = "Felsstruktur, Griffe als Marker", Solution = "Springer in 15-m-Etappen, Himmelsschuppe (Wandhaften), Greif von oben", SpeedFactor = 1f, Blocks = true, Keys = Traversal.Springer | Traversal.Skyscale | Traversal.Griffon, EdgeColor = new Color(0.55f, 0.48f, 0.42f) };
                case HazardKind.WindWall: return new HazardSpec { Kind = k, Name = "Windwand", Edge = "sichtbare Windbänder, Heulen", Solution = "Greif im Sturzflug, Himmelsschuppe", SpeedFactor = 0.5f, PushesBack = true, Keys = Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.85f, 0.95f, 1f) };
                case HazardKind.SandPortal: return new HazardSpec { Kind = k, Name = "Sandportal", Edge = "wirbelndes Portal mit Zielmarker", Solution = "Dünenläufer", SpeedFactor = 1f, Keys = Traversal.DuneRunner, EdgeColor = new Color(1f, 0.8f, 0.45f) };
                case HazardKind.ThornHedge: return new HazardSpec { Kind = k, Name = "Dornenhecke", Edge = "dichte Dornen, rote Blüten", Solution = "Raptor-Weitsprung, Springer, Hüpfpilze", SpeedFactor = 0.5f, Condition = EffectType.Bleeding, StacksPerSecond = 2, Blocks = true, Keys = Traversal.Raptor | Traversal.Springer | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.8f, 0.15f, 0.2f) };
                default: return new HazardSpec { Kind = HazardKind.CorruptionField, Name = "Verderbnis-Feld", Edge = "violettes Glühen, Risse", Solution = "Verderbnis-Widerstand, Flug", SpeedFactor = 0.85f, Condition = EffectType.Torment, StacksPerSecond = 1, HpPerSecond = 0.03f, HpCapPerTick = 0.10f, Keys = Traversal.CorruptionWard | Traversal.Griffon | Traversal.Skyscale, EdgeColor = new Color(0.65f, 0.3f, 0.95f) };
            }
        }

        /// <summary>Schützt einer der Schlüssel des Spielers vor der Hürde? (<paramref name="active"/> = aktuell genutzte Fortbewegung, z. B. aufgesessener Skimmer)</summary>
        public static bool Protected(HazardKind k, Traversal active) => (Spec(k).Keys & active) != 0;

        /// <summary>Hürden-Hinweis am Rand: „Benötigt: Skimmer …“.</summary>
        public static string Requirement(HazardKind k) => "Benötigt: " + ReachabilityModel.Label(Spec(k).Keys);

        /// <summary>Rettungsfenster: Zeit vom Betreten bis zum Tod (∞ = nie tödlich). Nie unter <see cref="MinRescueWindow"/>.</summary>
        public static float RescueWindow(HazardKind k)
        {
            var s = Spec(k);
            float t = s.LethalAfter > 0f ? s.LethalAfter : float.PositiveInfinity;
            if (s.HpPerSecond > 0f) t = Mathf.Min(t, 1f / s.HpPerSecond);
            return t;
        }
    }

    /// <summary>Aufenthalt in einer Hürde (rein): zählt die Zeit, liefert je Tick die Wirkung; Verlassen setzt zurück.</summary>
    public class HazardExposure
    {
        public readonly HazardKind Kind;
        public float Time { get; private set; }
        float _tickAcc;
        public HazardExposure(HazardKind k) { Kind = k; }

        public HazardTick Tick(float dt, bool inside, Traversal active)
        {
            var spec = HazardRules.Spec(Kind);
            if (!inside || HazardRules.Protected(Kind, active)) { Time = 0f; _tickAcc = 0f; return new HazardTick { SpeedFactor = 1f }; }
            Time += dt; _tickAcc += dt;
            var t = new HazardTick { SpeedFactor = spec.SpeedFactor, Pushback = spec.PushesBack };
            if (_tickAcc >= 1f)
            {
                _tickAcc -= 1f;
                t.Condition = spec.Condition; t.Stacks = spec.StacksPerSecond;
                float hp = spec.HpPerSecond;
                if (spec.HpCapPerTick > 0f) hp = Mathf.Min(hp, spec.HpCapPerTick);
                t.HpFraction = hp;
            }
            if (spec.SinkAfter > 0f && Time >= spec.SinkAfter) { t.Sinking = true; t.SpeedFactor *= 0.5f; }
            if (spec.LethalAfter > 0f && Time >= spec.LethalAfter) t.Dead = true;
            return t;
        }

        /// <summary>Treibsand: Ausweichen schiebt 1 m Richtung Rand und verkürzt die Einsink-Zeit um 1 s.</summary>
        public void Dodge() { if (Kind == HazardKind.Quicksand) Time = Mathf.Max(0f, Time - 1f); }
    }
}
