using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Ereignis-Definitionen (Data/Events/Event_*.asset): Migration der Dornwacht-Kette aus dem EventChain-Asset
    /// + Elmhain-Kette, Eskorte, Sammeln, Boss (Zufallsfenster) und Meta-Verteidigung von Sonnfurt.
    /// </summary>
    public static class EventDefinitionBuilder
    {
        public const string Dir = DataDir + "/Events";
        static readonly List<EventDefinition> _all = new List<EventDefinition>();

        static EventDefinition E(string id, string title, string desc, EventObjective obj, Vector2 center, float radius, float duration)
        {
            var path = Dir + "/Event_" + id + ".asset";
            var e = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (e == null) { e = ScriptableObject.CreateInstance<EventDefinition>(); AssetDatabase.CreateAsset(e, path); }
            e.Id = id; e.Title = title; e.Description = desc; e.Objective = obj; e.Center = ZoneBuilder.G(center); e.Radius = radius; e.Duration = duration;
            e.Triggers = new List<EventTrigger>(); e.SuccessNext = e.FailNext = null; e.ChainId = null; e.Cooldown = 120f;
            e.SetFlagOnSuccess = e.SetFlagOnFail = e.ClearFlagOnSuccess = e.ClearFlagOnFail = null; e.FlagDuration = 0f; e.WorldHook = null;
            e.Path = new Vector3[0]; e.DeliverPoint = Vector3.zero; e.CaptureRadius = 8f; e.CaptureTime = 8f; e.WaveSize = 3; e.WaveInterval = 15f; e.TargetCount = 8;
            e.GoldContribution = 8000f; e.Experience = 400; e.Karma = 150; e.Coins = 60; e.GoldItemRoll = true;
            e.Trash = DataBuilder.Archetype("Trash_Pluenderer"); e.Veteran = DataBuilder.Archetype("Veteran_Brecher"); e.Boss = DataBuilder.Archetype("Champion_Grolmok");
            _all.Add(e);
            return e;
        }

        static EventTrigger T(EventTriggerType t, float delay = 0f, string param = null, float min = 0f, float max = 0f, float prox = 0.75f)
            => new EventTrigger { Type = t, Delay = delay, Param = param, Min = min, Max = max, ProximityFactor = prox };

        public static EventDefinition[] Build(EventChain chain)
        {
            EnsureFolder(Dir);
            _all.Clear();
            // ---- Kette 1: Dornwacht (migriert aus EventChain_Grenzposten) ----
            var c = ZoneLayout.Outpost.Center; var camp = ZoneLayout.Camp.Center;
            foreach (var s in chain.Stages)
            {
                EventObjective obj; string hook; Vector2 at;
                switch (s.Type)
                {
                    case EventStageType.DefendSupplies: obj = EventObjective.Defend; hook = "dornwacht_defend"; at = c; break;
                    case EventStageType.AssaultCamp: obj = EventObjective.Kill; hook = "camp_assault"; at = camp; break;
                    case EventStageType.RecaptureOutpost: obj = EventObjective.Capture; hook = "outpost_occupied"; at = c; break;
                    default: obj = EventObjective.Boss; hook = "champion"; at = camp; break;
                }
                var e = E("dornwacht_" + s.Id, s.Title, s.Description, obj, at, chain.EventRadius, s.Duration);
                e.ChainId = "dornwacht"; e.WorldHook = hook; e.TargetCount = s.TargetCount; e.WaveSize = s.WaveSize; e.WaveInterval = s.WaveInterval;
                e.GoldContribution = s.GoldContribution; e.Karma = s.Karma; e.Coins = s.Coins; e.Experience = s.Experience;
                e.SuccessNext = string.IsNullOrEmpty(s.SuccessNext) ? null : "dornwacht_" + s.SuccessNext;
                e.FailNext = string.IsNullOrEmpty(s.FailNext) ? null : "dornwacht_" + s.FailNext;
                e.CaptureRadius = chain.EventRadius * 0.35f; e.CaptureTime = 6f;
                e.Boss = chain.Champion; e.Trash = chain.Trash; e.Veteran = chain.Veteran;
                if (s.Id == chain.FirstStage)
                {
                    e.Triggers.Add(T(EventTriggerType.Timer, chain.AutoStartDelay));
                    e.Triggers.Add(T(EventTriggerType.Proximity, 5f, prox: 0.75f));
                    e.Cooldown = chain.RestartCooldown;
                    e.SetFlagOnFail = EventDirector.FlagDornwachtFallen; e.FlagDuration = 600f;   // Meta: Dornwacht gefallen (Rückbau nach 10 min)
                }
                else { e.Triggers.Add(T(EventTriggerType.ChainLink)); e.Cooldown = 5f; }
                if (s.Type == EventStageType.RecaptureOutpost) e.ClearFlagOnSuccess = EventDirector.FlagDornwachtFallen;
            }

            // ---- Kette 2: Elmhain (Einnehmen → Veteranen-Welle) ----
            var r = ZoneLayout.Ruins.Center;
            var cap = E("elmhain_capture", "Elmhain zurückerobern", "Vertreibt die Aschkrallen aus den Ruinen und haltet den Platz – der Kreis füllt sich nur ohne Gegner darin.", EventObjective.Capture, r, 22f, 150f);
            cap.ChainId = "elmhain"; cap.WorldHook = "elmhain_capture"; cap.CaptureRadius = 7f; cap.CaptureTime = 8f; cap.SuccessNext = "elmhain_wave"; cap.Cooldown = 240f;
            cap.Triggers.Add(T(EventTriggerType.Proximity, 20f, prox: 0.7f)); cap.Triggers.Add(T(EventTriggerType.Timer, 420f)); cap.Triggers.Add(T(EventTriggerType.Heart, 0f, "heart_elmhain"));
            cap.GoldContribution = 7000f; cap.Experience = 450; cap.Karma = 150; cap.Coins = 60;
            var wave = E("elmhain_wave", "Gegenangriff auf Elmhain", "Aschkrallen-Brecher stürmen die Ruinen. Besiegt die Veteranen!", EventObjective.Kill, r, 22f, 120f);
            wave.ChainId = "elmhain"; wave.WorldHook = "elmhain_wave"; wave.TargetCount = 3; wave.Cooldown = 5f;
            wave.Triggers.Add(T(EventTriggerType.ChainLink));
            wave.SetFlagOnSuccess = "elmhain_befreit"; wave.FlagDuration = 600f;
            wave.GoldContribution = 9000f; wave.Experience = 550; wave.Karma = 180; wave.Coins = 80;

            // ---- Eskorte: Karawane nach Dornwacht ----
            var esc = E("caravan", "Karawane nach Dornwacht", "Begleite die Händlerkarawane von Sonnfurt zur Dornwacht. Fällt der Wagen, scheitert das Ereignis.", EventObjective.Escort, new Vector2(-54, -50), 25f, 300f);
            esc.WorldHook = "caravan"; esc.WaveSize = 3; esc.Cooldown = 300f;
            esc.Path = new[] { ZoneBuilder.G(new Vector2(-52, -50)), ZoneBuilder.G(new Vector2(-35, -35)), ZoneBuilder.G(new Vector2(-12, -12)), ZoneBuilder.G(new Vector2(-6, -6)) };
            esc.Triggers.Add(T(EventTriggerType.Proximity, 40f, prox: 0.6f)); esc.Triggers.Add(T(EventTriggerType.Timer, 200f));
            esc.SetFlagOnSuccess = "karawane_angekommen"; esc.FlagDuration = 300f;
            esc.GoldContribution = 6000f; esc.Experience = 500; esc.Karma = 200; esc.Coins = 100;

            // ---- Sammeln: Vorräte für Sonnfurt ----
            var col = E("supplies", "Vorräte für Sonnfurt", "Plünderer überfallen Gerstenbrunn! Sammle die verstreuten Vorratskisten und bring sie zur Abgabestelle in Sonnfurt.", EventObjective.Collect, ZoneLayout.Farm.Center, 22f, 180f);
            col.WorldHook = "collect"; col.TargetCount = 6; col.WaveSize = 2; col.WaveInterval = 18f; col.Cooldown = 300f;
            col.DeliverPoint = ZoneBuilder.G(new Vector2(-46, -56));
            col.Triggers.Add(T(EventTriggerType.Proximity, 30f, prox: 0.8f)); col.Triggers.Add(T(EventTriggerType.Timer, 320f));
            col.SetFlagOnFail = "vorraete_knapp"; col.FlagDuration = 600f;
            col.GoldContribution = 5000f; col.Experience = 450; col.Karma = 160; col.Coins = 80;

            // ---- Boss: Jagd auf den Aschkrallen-Alpha (Zufallsfenster 8–12 min) ----
            var alpha = E("alpha", "Jagd auf den Aschkrallen-Alpha", "Ein wandernder Aschkrallen-Alpha bedroht die Grenzmark. Stellt und besiegt ihn!", EventObjective.Boss, new Vector2(-20, 15), 25f, 300f);
            alpha.WorldHook = "alpha"; alpha.Boss = DataBuilder.Archetype("Veteran_Brecher"); alpha.Cooldown = 60f;
            alpha.Path = new[] { ZoneBuilder.G(new Vector2(-20, 15)), ZoneBuilder.G(new Vector2(30, 0)), ZoneBuilder.G(new Vector2(20, -40)), ZoneBuilder.G(new Vector2(-30, -10)) };
            alpha.Triggers.Add(T(EventTriggerType.RandomWindow, min: 480f, max: 720f));
            alpha.GoldContribution = 4000f; alpha.Experience = 600; alpha.Karma = 200; alpha.Coins = 120;

            // ---- Meta: Sonnfurt unter Angriff (Welt-Zustand „Dornwacht gefallen“) ----
            var def = E("sonnfurt_defense", "Sonnfurt unter Angriff", "Nach dem Fall der Dornwacht greifen Aschkrallen den Weiler an. Schützt Sonnfurts Vorräte!", EventObjective.Defend, ZoneLayout.Hub.Center, 30f, 120f);
            def.WorldHook = "sonnfurt_defend"; def.WaveSize = 3; def.WaveInterval = 16f; def.Cooldown = 300f;
            def.Triggers.Add(T(EventTriggerType.WorldState, 20f, EventDirector.FlagDornwachtFallen));
            def.GoldContribution = 8000f; def.Experience = 600; def.Karma = 220; def.Coins = 120;

            foreach (var e in _all) EditorUtility.SetDirty(e);
            AssetDatabase.SaveAssets();
            return _all.ToArray();
        }

        public static EventDefinition[] Load()
        {
            var list = new List<EventDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:EventDefinition", new[] { Dir }))
                list.Add(AssetDatabase.LoadAssetAtPath<EventDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            // Kettenköpfe zuerst (Debug „StartChain“ nimmt den ersten Eintrag = Dornwacht-Verteidigung)
            list.Sort((a, b) => string.CompareOrdinal(Order(a), Order(b)));
            return list.ToArray();
        }
        static string Order(EventDefinition e) => (e.Id == "dornwacht_defend" ? "0" : "1") + e.Id;
    }
}
