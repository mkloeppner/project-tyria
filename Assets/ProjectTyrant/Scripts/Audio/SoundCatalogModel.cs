using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Mixer-Gruppen (Master → Musik, Umgebung, Effekte (Kampf, Bewegung, Welt), UI, Stimme).</summary>
    public enum MixGroup { Master, Music, Ambience, Effects, Combat, Movement, World, UI, Voice }

    /// <summary>Klang-Kategorie: bestimmt Mixer-Gruppe, Ziel-RMS, Spektral-Fenster, Dauer und Stimmen-Priorität.</summary>
    public enum SoundCategory { UI, SkillEffect, Hit, Movement, World, Ambience, Vocal, Stinger, Music }

    /// <summary>Stimmen-Prioritäten: Spieler-Skills > Gegner-Telegraphen > Treffer > Schritte > Ambient-Details.</summary>
    public static class VoicePriority
    {
        public const int PlayerSkill = 5, EnemyTelegraph = 4, Hit = 3, Step = 2, AmbientDetail = 1;
    }

    public struct CategorySpec
    {
        public MixGroup Group; public float TargetRmsDb, MinCentroid, MaxCentroid, MinSeconds, MaxSeconds; public int Priority;
    }

    public static class SoundCategories
    {
        /// <summary>Lautheits-Hierarchie (relativ zur Vollaussteuerung): Musik −12, Welt −16, Effekte −10 (Spieler; Gegner spielen −4 dB leiser = −14), UI −20.</summary>
        public static CategorySpec Spec(SoundCategory c)
        {
            switch (c)
            {
                case SoundCategory.UI: return new CategorySpec { Group = MixGroup.UI, TargetRmsDb = -20f, MinCentroid = 1000f, MaxCentroid = 4000f, MinSeconds = 0.02f, MaxSeconds = 0.8f, Priority = VoicePriority.PlayerSkill };
                case SoundCategory.SkillEffect: return new CategorySpec { Group = MixGroup.Combat, TargetRmsDb = -10f, MinCentroid = 150f, MaxCentroid = 9000f, MinSeconds = 0.08f, MaxSeconds = 1.6f, Priority = VoicePriority.PlayerSkill };
                case SoundCategory.Hit: return new CategorySpec { Group = MixGroup.Combat, TargetRmsDb = -10f, MinCentroid = 150f, MaxCentroid = 8000f, MinSeconds = 0.05f, MaxSeconds = 1.2f, Priority = VoicePriority.Hit };
                case SoundCategory.Movement: return new CategorySpec { Group = MixGroup.Movement, TargetRmsDb = -16f, MinCentroid = 100f, MaxCentroid = 9000f, MinSeconds = 0.04f, MaxSeconds = 1.2f, Priority = VoicePriority.Step };
                case SoundCategory.World: return new CategorySpec { Group = MixGroup.World, TargetRmsDb = -16f, MinCentroid = 150f, MaxCentroid = 7000f, MinSeconds = 0.05f, MaxSeconds = 3.5f, Priority = VoicePriority.Hit };
                case SoundCategory.Ambience: return new CategorySpec { Group = MixGroup.Ambience, TargetRmsDb = -16f, MinCentroid = 80f, MaxCentroid = 9000f, MinSeconds = 0.2f, MaxSeconds = 20f, Priority = VoicePriority.AmbientDetail };
                case SoundCategory.Vocal: return new CategorySpec { Group = MixGroup.Voice, TargetRmsDb = -16f, MinCentroid = 100f, MaxCentroid = 5000f, MinSeconds = 0.15f, MaxSeconds = 2.5f, Priority = VoicePriority.EnemyTelegraph };
                case SoundCategory.Stinger: return new CategorySpec { Group = MixGroup.Music, TargetRmsDb = -12f, MinCentroid = 150f, MaxCentroid = 5000f, MinSeconds = 2f, MaxSeconds = 4.2f, Priority = VoicePriority.PlayerSkill };
                default: return new CategorySpec { Group = MixGroup.Music, TargetRmsDb = -12f, MinCentroid = 40f, MaxCentroid = 6000f, MinSeconds = 59f, MaxSeconds = 91f, Priority = VoicePriority.PlayerSkill };
            }
        }
    }

    /// <summary>Mikro-Varianten je Variante: Tonhöhe ± 5 %, Dauer ± 10 %, Filter-Zufall.</summary>
    public struct SoundVariation
    {
        public float Pitch, Duration, Bright; public int Index;
        public static SoundVariation For(int index, Rng r)
        {
            if (index == 0) return new SoundVariation { Pitch = 1f, Duration = 1f, Bright = 1f, Index = 0 };
            return new SoundVariation { Pitch = r.Range(0.95f, 1.05f), Duration = r.Range(0.9f, 1.1f), Bright = r.Range(0.82f, 1.18f), Index = index };
        }
    }

    /// <summary>Ein Rezept: Id, Kategorie, Varianten-Zahl, Schleife und Render-Funktion.</summary>
    public class SoundRecipe
    {
        public string Id; public SoundCategory Category; public int Variants = 3; public bool Loop; public bool Spatial = true;
        public Func<Rng, SoundVariation, float[]> Render;
        public float VolumeDb;   // zusätzlicher Abspielpegel (z. B. Betten leiser)
    }

    /// <summary>Round-Robin ohne direkte Wiederholung derselben Variante.</summary>
    public class RoundRobin
    {
        readonly Dictionary<string, int> _last = new Dictionary<string, int>();
        readonly Rng _rng;
        public RoundRobin(int seed = 7) { _rng = new Rng(seed); }
        public int Next(string id, int count)
        {
            if (count <= 1) return 0;
            int last; bool has = _last.TryGetValue(id, out last);
            int pick = _rng.Range(0, count);
            if (has && pick == last) pick = (pick + 1 + _rng.Range(0, count - 1)) % count;
            _last[id] = pick; return pick;
        }
    }

    /// <summary>Stimmen-Verwaltung (32 Stimmen gesamt, davon ein Pool für Effekte): Priorität, Stehlen der ältesten niedrigsten Stimme.</summary>
    public class VoiceAllocator
    {
        struct Voice { public bool Busy; public int Priority; public float Start, End; }
        readonly Voice[] _v;
        public int Capacity => _v.Length;
        public VoiceAllocator(int capacity) { _v = new Voice[capacity]; }

        public int Active(float now) { int n = 0; for (int i = 0; i < _v.Length; i++) if (_v[i].Busy && _v[i].End > now) n++; return n; }

        /// <summary>Freie Stimme oder die älteste Stimme mit niedrigerer Priorität; −1 wenn alle gleich/höher (Klang entfällt).</summary>
        public int Request(int priority, float now, float duration)
        {
            int best = -1; float bestStart = float.MaxValue; int bestPrio = int.MaxValue;
            for (int i = 0; i < _v.Length; i++)
            {
                if (!_v[i].Busy || _v[i].End <= now) { best = i; bestPrio = -1; break; }
                if (_v[i].Priority < priority && (_v[i].Priority < bestPrio || (_v[i].Priority == bestPrio && _v[i].Start < bestStart)))
                { best = i; bestPrio = _v[i].Priority; bestStart = _v[i].Start; }
            }
            if (best < 0)
            {
                // gleiche Priorität: älteste Stimme gleicher Priorität ersetzen (Schritte, Treffer), damit Neues hörbar bleibt
                for (int i = 0; i < _v.Length; i++) if (_v[i].Priority == priority && _v[i].Start < bestStart) { best = i; bestStart = _v[i].Start; }
            }
            if (best < 0) return -1;
            _v[best] = new Voice { Busy = true, Priority = priority, Start = now, End = now + duration };
            return best;
        }

        public void Release(int i) { if (i >= 0 && i < _v.Length) _v[i].Busy = false; }
    }
}
