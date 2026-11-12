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
            if (def.Kind == MapMarkerKind.Heart && GameManager.Instance != null && GameManager.Instance.Story != null) GameManager.Instance.Story.OnHeartCompleted(def.Id);   // Lauf 6: Dankesbrief
            GameEventBus.RaiseZoneProgress(def.Id);
            RequestSave("Zonenabschluss");
            CheckZoneCompletion();
        }

        /// <summary>Lauf 6: Herz-Beitrag über einen Weg (je Weg höchstens 70 %, <see cref="HeartModel"/>).</summary>
        public void AddHeartWay(ZoneObjectDefinition def, HeartWay way, float amount, string reason)
        {
            if (def == null || State.Completed.Contains(def.Id)) return;
            float before = ZoneRules.HeartProgress(State.Hearts, State.Completed, def.Id);
            float total = HeartModel.Add(State.Hearts, def.Id, way, amount);
            if (total <= before + 1e-5f) { GameEventBus.Notify(reason + "  ·  " + def.DisplayName + ": anderer Weg nötig (" + Mathf.RoundToInt(before * 100f) + " %)", Gold); return; }
            State.Hearts[def.Id] = before;   // AddHeartProgress bucht die Differenz und schließt bei 100 % ab
            AddHeartProgress(def, total - before, reason);
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
            GrantXp(Mathf.RoundToInt(XpRewards.ZoneCompletion * LevelCurves.RewardScale(GameManager.Instance != null && GameManager.Instance.CurrentZone != null ? GameManager.Instance.CurrentZone.LevelMax : 15)), "Zonenabschluss");   // Lauf 11: nach Zonen-Level
            GameEventBus.RaiseBanner("Zone abgeschlossen", ZoneRoot.ZoneName + " · Belohnungstruhe im Inventar", Gold);
            RequestSave("Zone abgeschlossen");
        }

        /// <summary>Fix-Lauf F1: Ankunfts-Wegmarke eines Eingangs freischalten – mit normaler Entdeckungs-EP und Banner, wenn sie in der Szene steht.</summary>
        public bool UnlockArrivalWaypoint(ZoneDefinition z, string entrance)
        {
            var w = WaypointSafety.ArrivalFor(z, entrance);
            if (w == null || State.Completed.Contains(w.Id)) return false;
            foreach (var o in ZoneObject.Objects) if (o != null && o.Definition != null && o.Definition.Id == w.Id) return CompleteZoneObject(o.Definition);
            State.Completed.Add(w.Id); RequestSave("Wegmarke");
            return true;
        }

        /// <summary>Fix-Lauf F1: Besiegt-Optionen (nie leer) für die aktuelle Zone und alle anderen.</summary>
        public List<WaypointSafety.DefeatOption> DefeatOptions()
        {
            var gm = GameManager.Instance;
            var zone = gm != null ? gm.CurrentZone : null;
            var areas = ZoneRules.ActiveEventAreas();
            return WaypointSafety.DefeatOptions(zone, gm != null && gm.World != null ? gm.World.Zones : null, State.Completed,
                p => ZoneRules.IsContested(p, areas), Player != null ? Player.transform.position : Vector3.zero);
        }

        /// <summary>Besiegt-Wahl ausführen: Notfall-Wegmarke wird dabei freigeschaltet; andere Zone → Wiederbelebung und Zonenwechsel (kostenlos).</summary>
        public void RespawnAt(WaypointSafety.DefeatOption o)
        {
            if (Player == null || o.Waypoint == null) return;
            if (o.Emergency && !State.Completed.Contains(o.Waypoint.Id)) { State.Completed.Add(o.Waypoint.Id); GameEventBus.RaiseBanner("Notfall-Wegmarke", o.Waypoint.Name + " ist jetzt freigeschaltet", new Color(0.75f, 0.9f, 1f)); }
            if (!o.OtherZone) { RespawnAt(o.Waypoint.Position, o.Waypoint.Name); return; }
            var downed = Player.GetComponent<DownedController>(); if (downed != null) downed.ResetState();
            Player.Revive(1f); GameEventBus.RaisePlayerRespawned(Player);
            var gm = GameManager.Instance; if (gm != null) gm.ChangeZone(o.Zone.Id, null, o.Waypoint.Position + new Vector3(0f, 0.3f, -2.5f));
            GameEventBus.Notify("Wiederbelebt: " + o.Waypoint.Name + " (" + o.Zone.DisplayName + ")", new Color(0.6f, 0.9f, 1f));
        }

        /// <summary>Fix-Lauf F1: unter den Weltboden gefallen → ohne Niederlage zur ersten Besiegt-Option (nächste freie Wegmarke, notfalls Notfall-Wegmarke).</summary>
        public bool RescueFromVoid()
        {
            var opts = DefeatOptions(); if (opts.Count == 0) return false;
            GameEventBus.Notify("Aus der Welt gefallen – zurück zur Wegmarke", new Color(1f, 0.8f, 0.5f));
            RespawnAt(opts[0]); return true;
        }

        /// <summary>Fix-Lauf F1 Migration: Ankunfts-Wegmarken aller besuchten Zonen und der aktuellen freischalten.</summary>
        int MigrateArrivalWaypoints()
        {
            var gm = GameManager.Instance; if (gm == null || gm.World == null) return 0;
            var visited = new List<string>(State.FogZones); if (!string.IsNullOrEmpty(State.CurrentZone)) visited.Add(State.CurrentZone);
            visited.Add(ProfileState.StartZoneId);
            return WaypointSafety.Migrate(State.Completed, gm.World.Zones, visited);
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

        /// <summary>Niederlage auf einem Aufstieg: genau am letzten Kontrollpunkt mit vollen HP erscheinen (ohne Versatz – Plattformen sind schmal).</summary>
        public void RespawnAtCheckpoint(Vector3 checkpoint, string climbName)
        {
            if (Player == null) return;
            var ui = UIController.Instance;
            System.Action go = () =>
            {
                var pc = Player.GetComponent<PlayerController>();
                if (pc != null) { pc.Teleport(checkpoint, Player.transform.rotation); pc.ResetFallTracking(); } else Player.transform.position = checkpoint;
                var downed = Player.GetComponent<DownedController>();
                if (downed != null) downed.ResetState();
                Player.Revive(1f);
                GameEventBus.RaisePlayerRespawned(Player);
                GameEventBus.Notify("Wiederbelebt am Kontrollpunkt: " + climbName, new Color(0.6f, 0.9f, 1f));
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
