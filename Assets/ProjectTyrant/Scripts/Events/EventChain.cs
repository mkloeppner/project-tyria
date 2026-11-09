using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum EventStageType { DefendSupplies, AssaultCamp, RecaptureOutpost, ChampionBoss }
    public enum RewardTier { None, Bronze, Silver, Gold }

    [Serializable]
    public class EventStageDef
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public EventStageType Type;
        public float Duration = 90f;
        public int TargetCount = 8;
        public int WaveSize = 3;
        public float WaveInterval = 15f;
        public string SuccessNext;
        public string FailNext;
        [Tooltip("Beitrag für Gold. Silber ab 50 %, Bronze ab 15 %.")]
        public float GoldContribution = 100f;
        public int Karma = 100;
        public int Coins = 50;
        public int Experience = 300;
    }

    [CreateAssetMenu(menuName = "Project Tyrant/Event Chain", fileName = "EventChain_")]
    public class EventChain : ScriptableObject
    {
        public string ChainName = "Event";
        public string FirstStage;
        public float EventRadius = 32f;
        public float AutoStartDelay = 60f;
        public float RestartCooldown = 60f;
        public List<EventStageDef> Stages = new List<EventStageDef>();
        public EnemyArchetype Trash, Veteran, Champion;

        public EventStageDef Get(string id) => string.IsNullOrEmpty(id) ? null : Stages.Find(s => s.Id == id);

        public static RewardTier ComputeTier(float contribution, float goldThreshold)
        {
            if (goldThreshold <= 0f) return contribution > 0f ? RewardTier.Gold : RewardTier.None;
            float k = contribution / goldThreshold;
            if (k >= 1f) return RewardTier.Gold;
            if (k >= 0.5f) return RewardTier.Silver;
            if (k >= 0.15f) return RewardTier.Bronze;
            return RewardTier.None;
        }

        public static float TierMultiplier(RewardTier t)
        {
            switch (t) { case RewardTier.Gold: return 1f; case RewardTier.Silver: return 0.6f; case RewardTier.Bronze: return 0.3f; }
            return 0f;
        }

        public static string TierName(RewardTier t)
        {
            switch (t) { case RewardTier.Gold: return "Gold"; case RewardTier.Silver: return "Silber"; case RewardTier.Bronze: return "Bronze"; }
            return "keine";
        }

        public static Color TierColor(RewardTier t)
        {
            switch (t) { case RewardTier.Gold: return new Color(1f, 0.82f, 0.25f); case RewardTier.Silver: return new Color(0.82f, 0.85f, 0.92f); case RewardTier.Bronze: return new Color(0.85f, 0.55f, 0.3f); }
            return new Color(0.7f, 0.7f, 0.7f);
        }

        /// <summary>Liefert die nächste Stufe (null = Kette endet).</summary>
        public EventStageDef Next(EventStageDef current, bool success) => current == null ? null : Get(success ? current.SuccessNext : current.FailNext);

        public static List<EventStageDef> CreateDefaultStages()
        {
            return new List<EventStageDef>
            {
                new EventStageDef { Id = "defend", Type = EventStageType.DefendSupplies, Title = "Verteidigt die Vorräte der Dornwacht",
                    Description = "Aschkrallen-Plünderer greifen den Grenzposten an. Schützt die Vorratskisten!",
                    Duration = 90f, WaveSize = 3, WaveInterval = 15f, SuccessNext = "assault", FailNext = "recapture", GoldContribution = 9000f, Karma = 120, Coins = 40, Experience = 400 },
                new EventStageDef { Id = "assault", Type = EventStageType.AssaultCamp, Title = "Gegenschlag: Stürmt die Rabenschanze",
                    Description = "Der Angriff ist abgewehrt. Tragt den Kampf ins Lager der Aschkrallen!",
                    Duration = 180f, TargetCount = 8, SuccessNext = "champion", FailNext = "", GoldContribution = 12000f, Karma = 160, Coins = 60, Experience = 600 },
                new EventStageDef { Id = "recapture", Type = EventStageType.RecaptureOutpost, Title = "Erobert den Grenzposten zurück",
                    Description = "Die Aschkrallen haben die Dornwacht überrannt. Vertreibt sie und haltet den Platz!",
                    Duration = 180f, TargetCount = 6, SuccessNext = "champion", FailNext = "", GoldContribution = 12000f, Karma = 160, Coins = 60, Experience = 600 },
                new EventStageDef { Id = "champion", Type = EventStageType.ChampionBoss, Title = "Besiegt Grolmok, den Aschenfürsten",
                    Description = "Der Anführer der Aschkrallen ist erschienen. Weicht seinen Erdstößen aus!",
                    Duration = 300f, SuccessNext = "", FailNext = "", GoldContribution = 30000f, Karma = 400, Coins = 150, Experience = 1500 },
            };
        }
    }
}
