using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public partial class PlayerProfile
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        /// <summary>Zone zu 100 % abgeschlossen (Zonen-Id) – Beherrschungspunkte hängen sich hier an.</summary>
        public static event System.Action<string> ZoneCompleted;

        public static string CompletionTitle(MapMarkerKind k)
        {
            switch (k)
            {
                case MapMarkerKind.Waypoint: return "Wegmarke entdeckt";
                case MapMarkerKind.PointOfInterest: return "Sehenswürdigkeit entdeckt";
                case MapMarkerKind.Vista: return "Aussichtspunkt erreicht";
                case MapMarkerKind.HeroChallenge: return "Heldenherausforderung abgeschlossen";
                case MapMarkerKind.Heart: return "Herz erfüllt";
                case MapMarkerKind.Insight: return "Beherrschungs-Einblick gefunden";
            }
            return "Abgeschlossen";
        }

        /// <summary>Markiert ein Zonen-Objekt als erledigt und vergibt die Belohnung genau einmal.</summary>
        public bool CompleteZoneObject(ZoneObjectDefinition def)
        {
            if (def == null || State.Completed.Contains(def.Id)) return false;
            State.Completed.Add(def.Id);
            GrantZoneReward(def);
            return true;
        }

        void GrantZoneReward(ZoneObjectDefinition def)
        {
            if (def.HeroPoints > 0) State.Progression.GrantHeroPoints(def.HeroPoints);
            GrantReward(def.Karma, def.Copper, def.Experience, CompletionTitle(def.Kind));
            string sub = def.DisplayName + (def.HeroPoints > 0 ? "  ·  +" + def.HeroPoints + " Heldenpunkte" : "");
            GameEventBus.RaiseBanner(CompletionTitle(def.Kind), sub, def.Kind == MapMarkerKind.Heart ? Gold : new Color(0.75f, 0.9f, 1f));
            GameEventBus.RaiseZoneProgress(def.Id);
            RequestSave("Zonenabschluss");
            CheckZoneCompletion();
        }

        public void AddHeartProgress(ZoneObjectDefinition def, float amount, string reason)
        {
            if (def == null) return;
            bool done = ZoneRules.AddHeartProgress(State.Hearts, State.Completed, def.Id, amount);
            if (done) GrantZoneReward(def);
            else
            {
                GameEventBus.Notify(reason + "  ·  " + def.DisplayName + " " + Mathf.RoundToInt(ZoneRules.HeartProgress(State.Hearts, State.Completed, def.Id) * 100f) + " %", Gold);
                GameEventBus.RaiseZoneProgress(def.Id);
                RequestSave("Herz");
            }
        }

        readonly List<ZoneObjectDefinition> _zoneDefs = new List<ZoneObjectDefinition>();

        public ZoneCompletion ZoneCompletionNow()
        {
            _zoneDefs.Clear();
            foreach (var o in ZoneObject.Objects) if (o != null && o.Definition != null) _zoneDefs.Add(o.Definition);
            return ZoneRules.Count(_zoneDefs, State.Completed);
        }

        void CheckZoneCompletion()
        {
            var c = ZoneCompletionNow();
            string key = "zonechest:" + State.CurrentZone;
            bool granted = State.Completed.Contains(key) || (State.CurrentZone == ProfileState.StartZoneId && State.ZoneChestGranted);
            if (!ZoneRules.ShouldGrantChest(c, granted)) return;
            State.Completed.Add(key);
            if (State.CurrentZone == ProfileState.StartZoneId) State.ZoneChestGranted = true;
            ZoneCompleted?.Invoke(State.CurrentZone);
            var chest = Items != null ? Items.Get("box_zone") : null;
            if (chest != null) State.Inventory.TryAdd(chest, 1, true);
            GrantXp(1500, "Zonenabschluss");
            GameEventBus.RaiseBanner("Zone abgeschlossen", ZoneRoot.ZoneName + " · Belohnungstruhe im Inventar", Gold);
            RequestSave("Zone abgeschlossen");
        }

        /// <summary>Nach „Besiegt“: an einer freien Wegmarke mit vollen HP erscheinen.</summary>
        public void RespawnAt(Vector3 waypoint, string label)
        {
            if (Player == null) return;
            var ui = UIController.Instance;
            System.Action go = () =>
            {
                var pc = Player.GetComponent<PlayerController>();
                var target = waypoint + new Vector3(0f, 0.3f, -2.5f);
                if (pc != null) pc.Teleport(target, Quaternion.identity); else Player.transform.position = target;
                var downed = Player.GetComponent<DownedController>();
                if (downed != null) downed.ResetState();
                Player.Revive(1f);
                GameEventBus.RaisePlayerRespawned(Player);
                GameEventBus.Notify("Wiederbelebt: " + label, new Color(0.6f, 0.9f, 1f));
            };
            if (ui != null) ui.FadeAction(go); else go();
        }

        /// <summary>Debug: alle Zonen-Objekte erledigen (für den Truhen-Test).</summary>
        public int CompleteAllZoneObjects()
        {
            int n = 0;
            foreach (var o in ZoneObject.Objects.ToArray()) if (o != null && o.Definition != null && CompleteZoneObject(o.Definition)) n++;
            return n;
        }
    }
}
