using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum EventObjective { Kill, Defend, Escort, Collect, Capture, Boss, Build, Hunt, Use, WorldBoss }
    public enum EventTriggerType { Timer, Proximity, ChainLink, WorldState, Heart, RandomWindow, Cycle }

    [Serializable]
    public class EventTrigger
    {
        public EventTriggerType Type;
        [Tooltip("Timer: Sekunden im Leerlauf · Proximity: Mindest-Leerlauf")] public float Delay;
        [Tooltip("RandomWindow: Fenster in Sekunden")] public float Min, Max;
        [Tooltip("WorldState: Flag · Heart: Herz-Id")] public string Param;
        [Tooltip("Proximity: Anteil des Event-Radius")] public float ProximityFactor = 0.75f;
        [Tooltip("Cycle: Periode in s (Delay = Versatz) – Weltboss/Meta nach Weltuhr")] public float Period;
    }

    /// <summary>Dynamisches Ereignis (GW2): Ziel, Auslöser, Zweige, Cooldown, Radius, Belohnungsstufen, Welt-Folgen.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Event")]
    public class EventDefinition : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public string ChainId;
        public EventObjective Objective;
        public Vector3 Center;
        public float Radius = 30f;
        public float Duration = 120f;
        public int TargetCount = 8;
        [Header("Auslöser & Zweige")]
        public List<EventTrigger> Triggers = new List<EventTrigger>();
        public string SuccessNext, FailNext;
        public float Cooldown = 120f;
        [Header("Belohnung")]
        public float GoldContribution = 1000f;
        public int Experience = 300, Karma = 100, Coins = 50;
        public bool GoldItemRoll = true;
        [Header("Welt-Folgen")]
        public string SetFlagOnSuccess, SetFlagOnFail, ClearFlagOnSuccess, ClearFlagOnFail;
        [Tooltip("Sekunden bis zum Rückbau (0 = bis gelöscht)")] public float FlagDuration;
        [Tooltip("Schlüssel für die Welt-Aktoren (Spawns, Objekte) im EventDirector")] public string WorldHook;
        [Header("Ziel-Daten")]
        public Vector3[] Path = new Vector3[0];          // Eskorte
        public Vector3 DeliverPoint;                      // Sammeln
        public float CaptureRadius = 8f, CaptureTime = 8f;
        public int WaveSize = 3;
        public float WaveInterval = 15f;
        public EnemyArchetype Trash, Veteran, Boss;
        [Header("Lauf 4")]
        [Tooltip("Bauen/Benutzen: Positionen der Bau-/Benutz-Objekte (Anzahl = TargetCount)")] public Vector3[] Sites = new Vector3[0];
        [Tooltip("Bauen/Benutzen: Kanalzeit je Objekt")] public float SiteChannel = 3f;
        public string SiteLabel = "Reparieren";
        [Tooltip("Mehrphasiger Boss: HP-Schwellen für Phasenwechsel (z. B. 0,5)")] public float[] PhaseThresholds = new float[0];
        [Tooltip("Beherrschungspunkt bei Erfolg (Weltboss/Meta-Finale)")] public int MasteryPoints;
        public string MetaId;
        public int MetaPhase;

        /// <summary>Skalierungs-Hook (GW2: mehr Spieler → mehr Gegner/HP). Im Prototyp konstant 1.</summary>
        public static float EventScale(int playerCount) => 1f;

        public bool IsChainMember => Triggers.Exists(t => t.Type == EventTriggerType.ChainLink);

        public static string ObjectiveName(EventObjective o)
        {
            switch (o)
            {
                case EventObjective.Kill: return "Töten";
                case EventObjective.Defend: return "Verteidigen";
                case EventObjective.Escort: return "Eskorte";
                case EventObjective.Collect: return "Sammeln";
                case EventObjective.Capture: return "Einnehmen";
                case EventObjective.Boss: return "Boss";
                case EventObjective.Build: return "Bauen";
                case EventObjective.Hunt: return "Jagd";
                case EventObjective.Use: return "Benutzen";
                case EventObjective.WorldBoss: return "Weltboss";
            }
            return "";
        }

        public static string IconFor(EventObjective o)
        {
            switch (o)
            {
                case EventObjective.Defend: return "map_ev_defend";
                case EventObjective.Escort: return "map_ev_escort";
                case EventObjective.Collect: return "map_ev_collect";
                case EventObjective.Capture: return "map_ev_capture";
                case EventObjective.Build: return "map_ev_defend";
                case EventObjective.Use: return "map_ev_collect";
                case EventObjective.WorldBoss: return "map_worldboss";
                default: return "map_ev_kill";
            }
        }
    }
}
