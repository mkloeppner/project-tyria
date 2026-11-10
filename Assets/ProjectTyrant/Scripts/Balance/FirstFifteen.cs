using System.Collections.Generic;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: Modell-Simulation „Die ersten 15 Minuten“ (Pflichtfluss Sonnfurt): Herz mit 2–3 Feinden, Event Stufe 0 mit Wachen, Wegmarke, Späher,
    /// Weg nach Elmhain (Stufe 1) mit dem ersten Veteran. Anfänger-Verhalten: kämpft gegen alles, was ihn angreift, heilt unter 30 %, keine Ausweichrolle.
    /// </summary>
    public static class FirstFifteen
    {
        public struct Encounter { public string Name; public int Danger, Level, Trash, Veterans, Guards; public float WalkSeconds; }
        public struct Result { public bool Died; public float MinHp, Seconds; public List<string> Log; }

        public static readonly Encounter[] Flow =
        {
            new Encounter { Name = "Ankunft Sonnfurt", Danger = 0, Level = 1, WalkSeconds = 40 },
            new Encounter { Name = "Herz Wilma (Bündel + Rand-Trash)", Danger = 0, Level = 2, Trash = 2, WalkSeconds = 150 },
            new Encounter { Name = "Herz Wilma (zweiter Trupp)", Danger = 0, Level = 2, Trash = 1, WalkSeconds = 90 },
            new Encounter { Name = "Event Stufe 0: Vorräte verteidigen", Danger = 0, Level = 2, Trash = 5, Guards = 2, WalkSeconds = 180 },
            new Encounter { Name = "Wegmarke Gerstenbrunn + Späher", Danger = 0, Level = 3, WalkSeconds = 120 },
            new Encounter { Name = "Weg nach Elmhain (Paar)", Danger = 1, Level = 3, Trash = 2, WalkSeconds = 150 },
            new Encounter { Name = "Erster Veteran am Lager", Danger = 1, Level = 3, Veterans = 1, WalkSeconds = 90 },
        };

        /// <summary>Simuliert den Fluss; <paramref name="variance"/> streut Zeit-bis-Kill (Seed).</summary>
        public static Result Run(int seed, float healFraction = 0.3f)
        {
            var rng = new System.Random(seed);
            var log = new List<string>();
            float hpFrac = 1f, minHp = 1f, t = 0f; bool healReady = true; float healCd = 0f;
            foreach (var e in Flow)
            {
                t += e.WalkSeconds; healCd -= e.WalkSeconds; if (healCd <= 0f) healReady = true;
                hpFrac = 1f;   // Regeneration außerhalb des Kampfes (5 s, 10 %/s) füllt auf dem Weg auf
                var s = DangerTable.Default(e.Danger);
                var foes = new List<(EnemyTier tier, float left)>();
                for (int i = 0; i < e.Trash; i++) foes.Add((EnemyTier.Trash, EnemyCurve.TargetTtk(s, EnemyTier.Trash) * (0.9f + 0.4f * (float)rng.NextDouble())));
                for (int i = 0; i < e.Veterans; i++) foes.Add((EnemyTier.Veteran, EnemyCurve.TargetTtk(s, EnemyTier.Veteran) * (0.9f + 0.4f * (float)rng.NextDouble())));
                int guardsTaking = System.Math.Min(e.Guards, foes.Count);
                float dt = 0.1f, ft = 0f;
                while (foes.Count > 0 && ft < 600f)
                {
                    ft += dt; t += dt; healCd -= dt; if (healCd <= 0f) healReady = true;
                    // Spieler schlägt den ersten Gegner, Wachen binden die letzten
                    var f = foes[0]; f.left -= dt; foes[0] = f;
                    if (f.left <= 0f) foes.RemoveAt(0);
                    for (int i = 0; i < foes.Count - guardsTaking; i++)
                    {
                        float interval = foes[i].tier == EnemyTier.Veteran ? 2.2f : 2f;
                        hpFrac -= EnemyCurve.HitShare(s, foes[i].tier, interval) / interval * dt;
                    }
                    if (hpFrac < healFraction && healReady) { hpFrac += 0.35f; healReady = false; healCd = 20f; }
                    minHp = System.Math.Min(minHp, hpFrac);
                    if (hpFrac <= 0f) { log.Add(e.Name + ": besiegt nach " + ft.ToString("0") + " s"); return new Result { Died = true, MinHp = 0f, Seconds = t, Log = log }; }
                }
                log.Add(e.Name + ": " + ft.ToString("0") + " s, HP min " + (minHp * 100f).ToString("0") + " %");
            }
            return new Result { Died = false, MinHp = minHp, Seconds = t, Log = log };
        }
    }
}
