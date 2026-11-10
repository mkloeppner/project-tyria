using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum FishRarity { Einfach, Edel, Selten, Exotisch, Legendaer }

    /// <summary>Lauf 11: ein Fisch einer Sammlung.</summary>
    public struct FishDef { public string Id, Name, Zone; public FishRarity Rarity; public bool Night; public FishDef(string id, string name, string zone, FishRarity r, bool night = false) { Id = id; Name = name; Zone = zone; Rarity = r; Night = night; } }

    /// <summary>
    /// Lauf 11: Angeln als Mini-Spiel (rein, getestet). Wurf: Leertaste halten (Weite 5–25 m), Biss nach 2–8 s, dann Spannung halten: der Fisch zieht
    /// in Phasen, Einholen hebt die Spannung, Loslassen senkt sie; Spannung 0,2–0,8 baut Fortschritt auf, außerhalb reißt die Schnur (> 1) oder der Fisch flieht (< 0).
    /// Rute (1–3) und Köder heben die Chance auf seltene Fische; vom Boot +1 Stufe.
    /// </summary>
    public class FishingModel
    {
        public enum Phase { Idle, Casting, Waiting, Hooked, Caught, Lost }
        public Phase State = Phase.Idle;
        public float Power, Distance, BiteAt, Waited, Tension = 0.5f, Progress;
        public int RodTier = 1; public bool Bait, FromBoat;
        public FishDef? Hooked;
        readonly System.Random _rng;
        float _pullT; float _pull;
        public const float MinCast = 5f, MaxCast = 25f, MinBite = 2f, MaxBite = 8f, SafeLow = 0.2f, SafeHigh = 0.8f;

        public FishingModel(int seed) { _rng = new System.Random(seed); }

        public void BeginCast() { if (State == Phase.Idle || State == Phase.Caught || State == Phase.Lost) { State = Phase.Casting; Power = 0f; } }
        public void HoldCast(float dt) { if (State == Phase.Casting) Power = Mathf.Min(1f, Power + dt / 1.5f); }

        public void ReleaseCast()
        {
            if (State != Phase.Casting) return;
            Distance = Mathf.Lerp(MinCast, MaxCast, Power);
            BiteAt = Mathf.Lerp(MinBite, MaxBite, (float)_rng.NextDouble()); Waited = 0f; State = Phase.Waiting;
        }

        /// <summary>Wartet auf den Biss; true = Biss (Fisch hängt).</summary>
        public bool Wait(float dt, IList<FishDef> pool, bool night)
        {
            if (State != Phase.Waiting) return false;
            Waited += dt; if (Waited < BiteAt) return false;
            Hooked = Roll(pool, night); State = Phase.Hooked; Tension = 0.5f; Progress = 0f; _pullT = 0f; return true;
        }

        public FishDef Roll(IList<FishDef> pool, bool night)
        {
            int tier = RodTier + (Bait ? 1 : 0) + (FromBoat ? 1 : 0);
            var weights = new List<float>();
            float sum = 0f;
            foreach (var f in pool) { float w = f.Night && !night ? 0f : RarityWeight(f.Rarity, tier); weights.Add(w); sum += w; }
            float r = (float)_rng.NextDouble() * sum;
            for (int i = 0; i < pool.Count; i++) { r -= weights[i]; if (r <= 0f && weights[i] > 0f) return pool[i]; }
            return pool[0];
        }

        public static float RarityWeight(FishRarity r, int tier)
        {
            switch (r) { case FishRarity.Einfach: return 50f; case FishRarity.Edel: return 25f; case FishRarity.Selten: return 8f + 4f * tier; case FishRarity.Exotisch: return 1f + 2f * tier; default: return tier >= 3 ? 0.5f * tier : 0f; }
        }

        /// <summary>Spannungs-Spiel: <paramref name="reel"/> = Taste gedrückt. Rückgabe: Zustand.</summary>
        public Phase Fight(float dt, bool reel)
        {
            if (State != Phase.Hooked) return State;
            _pullT -= dt;
            if (_pullT <= 0f) { _pullT = 1f + (float)_rng.NextDouble() * 1.5f; _pull = 0.15f + (float)_rng.NextDouble() * 0.35f * (1f + (int)(Hooked?.Rarity ?? 0) * 0.25f); }
            Tension += (reel ? 0.55f : -0.45f) * dt + (reel ? _pull * dt : -_pull * 0.3f * dt);
            if (Tension > 1f) { State = Phase.Lost; return State; }
            if (Tension < 0f) { State = Phase.Lost; return State; }
            if (Tension >= SafeLow && Tension <= SafeHigh) Progress += dt / (4f + (int)(Hooked?.Rarity ?? 0) * 1.5f);
            if (Progress >= 1f) State = Phase.Caught;
            return State;
        }
    }

    /// <summary>Lauf 11: Fischsammlungen je Angel-Zone (≥ 8 Fische je Zone).</summary>
    public static class FishCatalog
    {
        static FishDef F(string zone, string id, string name, FishRarity r, bool night = false) => new FishDef(zone + "_" + id, name, zone, r, night);
        public static readonly string[] FishingZones = { "gruene_flut", "sonnenstufen", "nebelkueste", "jadehafen", "sturmbucht", "schilfmuendung", "silberkueste", "fluesterhain" };
        static readonly string[] Names = { "Jadebarsch", "Silberling", "Flussgrundel", "Nebelaal", "Sonnenkarpfen", "Laternenfisch", "Drachenschuppenwels", "Kaiserkoi", "Sturmhering", "Mondforelle" };

        public static List<FishDef> For(string zone)
        {
            var l = new List<FishDef>();
            if (System.Array.IndexOf(FishingZones, zone) < 0) return l;
            int off = Mathf.Abs(zone.GetHashCode()) % Names.Length;
            FishRarity[] rar = { FishRarity.Einfach, FishRarity.Einfach, FishRarity.Einfach, FishRarity.Edel, FishRarity.Edel, FishRarity.Selten, FishRarity.Selten, FishRarity.Exotisch, FishRarity.Legendaer };
            for (int i = 0; i < rar.Length; i++) l.Add(F(zone, "f" + i, Names[(off + i) % Names.Length] + (i >= Names.Length ? " II" : ""), rar[i], i == 6));
            return l;
        }

        /// <summary>Sammlung vollständig?</summary>
        public static bool Complete(string zone, ICollection<string> caught) { foreach (var f in For(zone)) if (!caught.Contains(f.Id)) return false; return For(zone).Count > 0; }
    }
}
