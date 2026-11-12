using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 4 / M19: generische Welt-Aktoren der Biom-Zonen – Bauen/Reparieren und Benutzen (Kanal-Objekte), Jagd in Phasen
    /// (Beute → Anführer), Weltboss mit HP-Phasen und Zeitlimit, Weltboss-/Meta-Timer auf Karte und Kontinent.
    /// </summary>
    public partial class EventDirector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RegisterTimerHook() => WorldMapWindow.ZoneTimerText = TimerText;

        /// <summary>„Frostschlund in 7:20“ / „Frostschlund: aktiv“ für Zonen mit Zyklus.</summary>
        public static string TimerText(ZoneDefinition z)
        {
            if (z == null || z.TimerPeriod <= 0f) return null;
            var d = Instance;
            if (d != null && d.Scheduler != null && d._zone != null && d._zone.Definition == z)
                foreach (var r in d.Scheduler.Runners) if (r.Active && (r.Def.ChainId == z.TimerEventId || r.Def.Id == z.TimerEventId))
                        return "<color=#ff8060>" + z.TimerLabel + ": aktiv</color>";
            float next = WorldClock.NextStart(z.TimerPeriod, z.TimerOffset, WorldClock.Seconds);
            return "<color=#ffb070>" + z.TimerLabel + " in " + WorldClock.Format(next - WorldClock.Seconds) + "</color>";
        }

        void CollectTimerMarkers(List<MapMarkerInfo> into)
        {
            var z = _zone != null ? _zone.Definition : null;
            if (z == null || z.TimerPeriod <= 0f) return;
            into.Add(new MapMarkerInfo { Kind = MapMarkerKind.WorldBoss, Position = z.TimerPosition, Label = z.TimerLabel, Owner = this });
        }

        TooltipContent TimerTooltip(MapMarkerInfo info)
        {
            var z = _zone != null ? _zone.Definition : null;
            if (z == null) return null;
            var c = new TooltipContent { Title = z.TimerLabel, TitleColor = new Color(1f, 0.45f, 0.35f), Subtitle = z.DisplayName };
            c.Add(TimerText(z), Color.white);
            c.Add("Vor-Ereignisse starten den Zyklus; Weltboss/Meta-Finale gibt einmalig 1 Beherrschungspunkt.", new Color(0.8f, 0.8f, 0.85f));
            return c;
        }

        // ---------------- Start ----------------
        void StartGeneric(World w)
        {
            var d = w.R.Def;
            switch (d.Objective)
            {
                case EventObjective.Build:
                case EventObjective.Use:
                    for (int i = 0; i < d.Sites.Length; i++) w.Props.Add(CreateSite(w, d.Sites[i], d.Objective == EventObjective.Use));
                    for (int i = 0; i < 2; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.7f), null, null);
                    break;
                case EventObjective.Hunt:
                    for (int i = 0; i < Mathf.Min(5, d.TargetCount); i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.6f), null, null);
                    break;
                case EventObjective.WorldBoss:
                    w.Boss = Spawn(w, d.Boss, d.Center, null, null);
                    if (w.Boss != null) w.Boss.UseLeash = false;
                    GameEventBus.RaiseBanner(d.Title, "Weltboss erscheint – " + Mathf.RoundToInt(d.Duration / 60f) + " Minuten Zeit", new Color(1f, 0.45f, 0.35f));
                    break;
                case EventObjective.Boss:
                    w.Boss = Spawn(w, d.Boss, d.Center, null, null);
                    if (w.Boss != null) w.Boss.UseLeash = false;
                    break;
                case EventObjective.Kill:
                    for (int i = 0; i < Mathf.Min(4, d.TargetCount); i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.5f), null, null);
                    break;
                case EventObjective.Defend:
                    if (Crates != null) foreach (var c in Crates) if (c != null && c.IsDead) { c.Revive(1f); SetCrateVisual(c, true); }
                    break;
            }
            if (d.MetaPhase > 0) GameEventBus.RaiseBanner("Meta-Ereignis · Phase " + d.MetaPhase + "/4", d.Title, new Color(1f, 0.6f, 0.3f));
        }

        // ---------------- Takt ----------------
        void TickGeneric(World w, float dt)
        {
            var r = w.R; var d = r.Def;
            switch (d.Objective)
            {
                case EventObjective.Build:
                case EventObjective.Use:
                    if ((w.WaveTimer -= dt) <= 0f && AliveOf(w) < 4)
                    {
                        w.WaveTimer = d.WaveInterval;
                        for (int i = 0; i < d.WaveSize; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.9f), d.Center, null);
                    }
                    break;
                case EventObjective.Hunt:
                    if (r.Phase == 0)
                    {
                        if ((w.WaveTimer -= dt) <= 0f && AliveOf(w) < 4) { w.WaveTimer = 6f; for (int i = 0; i < 2; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.6f), null, null); }
                    }
                    else
                    {
                        if (r.PhaseChanged)
                        {
                            r.PhaseChanged = false;
                            w.Boss = Spawn(w, d.Boss, d.Center, null, null);
                            if (w.Boss != null) w.Boss.UseLeash = false;
                            GameEventBus.RaiseBanner("Der Anführer zeigt sich!", d.Boss != null ? d.Boss.DisplayName : d.Title, new Color(1f, 0.55f, 0.3f));
                        }
                        r.BossHealth = w.Boss != null ? w.Boss.Self.HealthFraction : 0f;
                        if (w.Boss == null || w.Boss.Self.IsDead) r.BossDefeated = true;
                    }
                    break;
                case EventObjective.WorldBoss:
                {
                    int before = r.Phase;
                    r.BossHealth = w.Boss != null ? w.Boss.Self.HealthFraction : 0f;
                    if (w.Boss == null || w.Boss.Self.IsDead) { r.BossDefeated = true; break; }
                    int now = EventRules.PhaseFor(r.BossHealth, d.PhaseThresholds);
                    if (now > before || now > w.PathIndex - 1)
                    {
                        if (now > w.PathIndex - 1)
                        {
                            w.PathIndex = now + 1;
                            GameEventBus.RaiseBanner(w.Boss.Self.DisplayName + " · Phase " + (now + 1), "Diener erscheinen – die Trotzleiste ist wieder voll", new Color(1f, 0.45f, 0.35f));
                            for (int i = 0; i < 4; i++) Spawn(w, d.Trash, RingPoint(d.Center, 10f), null, null);
                            if (d.Veteran != null) Spawn(w, d.Veteran, RingPoint(d.Center, 8f), null, null);
                            var bar = w.Boss.GetComponent<DefianceBar>(); if (bar != null) bar.SetMax(bar.Max * 1.25f);
                        }
                    }
                    break;
                }
            }
        }

        GameObject CreateSite(World w, Vector3 pos, bool use)
        {
            pos.y = ZoneRoot.GroundHeight(pos.x, pos.z);
            var go = new GameObject(use ? "EventUse" : "EventBuild");
            go.transform.SetParent(SpawnParent, false);
            go.transform.position = pos;
            var mat = new Material(Visuals.PTShaders.Lit()) { color = use ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.45f, 0.4f, 0.35f) };
            var a = GameObject.CreatePrimitive(use ? PrimitiveType.Cylinder : PrimitiveType.Cube); Object.Destroy(a.GetComponent<Collider>());
            a.transform.SetParent(go.transform, false);
            a.transform.localPosition = new Vector3(0, use ? 1f : 0.6f, 0); a.transform.localScale = use ? new Vector3(0.6f, 1f, 0.6f) : new Vector3(1.6f, 1.2f, 0.4f);
            a.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(go.transform, false); ring.transform.localPosition = new Vector3(0, 0.03f, 0); ring.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = new Material(Visuals.PTShaders.Lit()) { color = new Color(1f, 0.7f, 0.25f) };
            var site = go.AddComponent<EventSite>();
            site.Runner = w.R; site.Body = a.GetComponent<MeshRenderer>(); site.Use = use;
            site.PromptText = w.R.Def.SiteLabel; site.Range = 2.8f; site.ChannelTime = Mathf.Max(0.5f, w.R.Def.SiteChannel);
            return go;
        }
    }

    /// <summary>Bau-/Benutz-Objekt eines Ereignisses (F, Kanal): zählt den Fortschritt einmalig, Schaden bricht ab.</summary>
    public class EventSite : Interactable
    {
        [System.NonSerialized] public EventRunner Runner;   // Laufzeit-Bezug (nicht serialisierbar)
        public MeshRenderer Body;
        public bool Use;
        bool _done;

        public override bool CanInteract(Combatant player) => isActiveAndEnabled && !_done && Runner != null && Runner.Active;
        public override void Interact(Combatant player)
        {
            if (_done || Runner == null || !Runner.Active) return;
            _done = true;
            Runner.Built++;
            EventDirector.Instance?.AddInteraction(Runner, 150f);
            if (Body != null) Body.sharedMaterial.color = Use ? new Color(1f, 0.8f, 0.35f) : new Color(0.62f, 0.45f, 0.25f);
            if (Use)
            {
                var l = new GameObject("Lit").AddComponent<Light>(); l.transform.SetParent(transform, false); l.transform.localPosition = Vector3.up * 2.4f;
                l.type = LightType.Point; l.range = 8f; l.intensity = 2.5f; l.color = new Color(1f, 0.75f, 0.4f); l.shadows = LightShadows.None;
            }
            else Body.transform.localScale = new Vector3(1.8f, 2.2f, 0.5f);
            GameEventBus.Notify(Runner.Def.SiteLabel + ": " + Runner.Built + "/" + Runner.Def.TargetCount, new Color(1f, 0.8f, 0.4f));
        }
    }
}
