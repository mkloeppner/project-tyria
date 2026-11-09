using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    public enum ChainPhase { Waiting, Running, Cooldown }

    /// <summary>
    /// Ereignis-Engine in der Szene (Singleton aus Lauf 1): hostet den <see cref="EventScheduler"/> und setzt Runner in die Welt um
    /// (Spawns, Eskorte, Sammel-Objekte, Kreis, Boss), zählt Beitrag, vergibt Belohnungen, verwaltet Welt-Folgen und Meta-Zustand.
    /// </summary>
    public partial class EventDirector : MonoBehaviour, IMapMarkerProvider
    {
        public static EventDirector Instance { get; private set; }
        public const string FlagDornwachtFallen = "dornwacht_fallen";

        public EventDefinition[] Definitions = new EventDefinition[0];
        public EventChain Chain;
        public Combatant[] Crates;
        public Combatant[] HubCrates;
        public NpcBrain[] Guards;
        public LootTable GoldLoot;
        public VendorNpc CaravanVendor;
        public int MaxParallel = 3;
        public float ChainResetDelay = 30f;

        public EventScheduler Scheduler { get; private set; }
        public readonly List<string> History = new List<string>();

        class World
        {
            public EventRunner R;
            public readonly List<NpcBrain> Spawned = new List<NpcBrain>();
            public float WaveTimer = 3f;
            public NpcBrain Boss;
            public Combatant Escort;
            public int PathIndex = 1;
            public float PathTotal, PathDone;
            public bool Raid1, Raid2;
            public readonly List<GameObject> Props = new List<GameObject>();
        }
        readonly List<World> _worlds = new List<World>();
        readonly List<NpcBrain> _ruinsLocals = new List<NpcBrain>();
        float _notifyTimer, _chainResetTimer = -1f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            GameEventBus.DamageDealt += OnDamage;
            GameEventBus.EntityKilled += OnKilled;
            GameEventBus.Healed += OnHealed;
            MapMarkerRegistry.Register(this);
            ZoneRules.CollectEventAreas -= Areas; ZoneRules.CollectEventAreas += Areas;
        }

        void OnDisable()
        {
            GameEventBus.DamageDealt -= OnDamage;
            GameEventBus.EntityKilled -= OnKilled;
            GameEventBus.Healed -= OnHealed;
            MapMarkerRegistry.Unregister(this);
            ZoneRules.CollectEventAreas -= Areas;
        }

        /// <summary>Zone geladen: Ereignisse der Zone frisch starten (GW2: neue Karten-Instanz), Referenzen aus dem ZoneRoot.</summary>
        public void BindZone(ZoneRoot root)
        {
            UnbindZone();
            _zone = root;
            Definitions = root.Events ?? new EventDefinition[0];
            Chain = root.Chain; Crates = root.Crates; HubCrates = root.HubCrates; Guards = root.Guards; CaravanVendor = root.CaravanVendor;
            Scheduler = new EventScheduler(Definitions, 20261109 + root.Id.GetHashCode()) { MaxParallel = MaxParallel };
            Scheduler.Started += OnStarted;
            Scheduler.Ended += OnEnded;
            Scheduler.FlagChanged += OnFlagChanged;
            ApplyNeutralWorld();
            if (CaravanVendor != null) CaravanVendor.Available = false;
            ZoneBound?.Invoke(root);
        }

        /// <summary>Zone verlassen: Ereignis-Zustände verwerfen (Gegner/Objekte gehören der Zonen-Szene und werden mit ihr entladen).</summary>
        public void UnbindZone()
        {
            _worlds.Clear(); _ruinsLocals.Clear();
            Scheduler = null; _zone = null; _chainResetTimer = -1f;
            Definitions = new EventDefinition[0]; Crates = null; HubCrates = null; Guards = null; CaravanVendor = null;
            GameEventBus.RaiseEventStateChanged();
        }

        ZoneRoot _zone;
        public static event System.Action<ZoneRoot> ZoneBound;
        Transform SpawnParent => _zone != null ? _zone.transform : transform;

        ZoneAnchors A => ZoneAnchors.Instance;
        Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        // ---------------- Kompatibilität (Karte/Tracker aus M11) ----------------
        public ChainPhase Phase => _worlds.Count > 0 ? ChainPhase.Running : ChainPhase.Waiting;
        public Vector3 CurrentCenter => _worlds.Count > 0 ? CenterOf(_worlds[0]) : transform.position;
        public void StartChain() => StartEvent(Chain != null && Definitions.Length > 0 ? Definitions[0].Id : null);
        public EventRunner StartEvent(string id) => Scheduler != null && !string.IsNullOrEmpty(id) ? Scheduler.Start(id) : null;
        public void NotifyHeart(string heartId) => Scheduler?.NotifyHeart(heartId);
        public IReadOnlyList<EventRunner> Active => Scheduler != null ? Scheduler.Runners : (IReadOnlyList<EventRunner>)new EventRunner[0];

        Vector3 CenterOf(World w)
        {
            if (w.R.Def.Objective == EventObjective.Escort && w.Escort != null) return w.Escort.transform.position;
            if (w.R.Def.Objective == EventObjective.Boss && w.Boss != null && w.R.Def.WorldHook == "alpha") return w.Boss.transform.position;
            return w.R.Def.Center;
        }

        bool PlayerNear(World w, float factor)
        {
            var p = Player; if (p == null || p.IsDead) return false;
            var d = p.transform.position - CenterOf(w); d.y = 0f;
            return d.magnitude <= w.R.Def.Radius * factor;
        }

        World WorldOf(EventRunner r) { foreach (var w in _worlds) if (w.R == r) return w; return null; }

        // ---------------- Takt ----------------
        void Update()
        {
            WorldClock.Seconds += Time.deltaTime;
            if (Scheduler == null) return;
            float dt = Time.deltaTime;
            var p = Player;
            Scheduler.Tick(dt, p != null ? p.transform.position : Vector3.one * 9999f);
            for (int i = 0; i < _worlds.Count; i++) TickWorld(_worlds[i], dt);
            Scheduler.TickRunners(dt, Inputs);
            if (_chainResetTimer > 0f && (_chainResetTimer -= dt) <= 0f) ResetDornwachtWorld();
            if ((_notifyTimer -= dt) <= 0f)
            {
                _notifyTimer = 0.5f;
                foreach (var w in _worlds)
                    if (!w.R.PlayerNotified && PlayerNear(w, 1f))
                    {
                        w.R.PlayerNotified = true;
                        GameEventBus.Notify("Neues Ereignis in der Nähe: " + w.R.Def.Title, new Color(1f, 0.7f, 0.3f));
                    }
            }
        }

        EventInputs Inputs(EventRunner r)
        {
            var w = WorldOf(r);
            var input = new EventInputs();
            if (w == null || r.Def.Objective != EventObjective.Capture) return input;
            var p = Player;
            var c = r.Def.Center;
            if (p != null && !p.IsDead) { var d = p.transform.position - c; d.y = 0f; input.PlayerInCapture = d.magnitude <= r.Def.CaptureRadius; }
            foreach (var e in Combatant.All)
            {
                if (e == null || e.IsDead || e.Team != Team.Enemy) continue;
                var d = e.transform.position - c; d.y = 0f;
                if (d.magnitude <= r.Def.CaptureRadius) input.EnemiesInCapture++;
            }
            return input;
        }

        void TickWorld(World w, float dt)
        {
            var r = w.R; var d = r.Def;
            w.Spawned.RemoveAll(b => b == null);
            switch (d.Objective)
            {
                case EventObjective.Defend:
                {
                    var objs = d.WorldHook == "sonnfurt_defend" ? HubCrates : Crates;
                    r.DefendHealth = objs == null || objs.Length == 0 ? 1f : AliveOf(objs) / (float)objs.Length;
                    if ((w.WaveTimer -= dt) <= 0f && r.Time < d.Duration - 10f)
                    {
                        w.WaveTimer = d.WaveInterval;
                        SpawnWaveAgainst(w, objs, d.WaveSize + (r.Time > d.Duration * 0.5f ? 1 : 0), r.Time > d.Duration * 0.6f);
                    }
                    break;
                }
                case EventObjective.Kill:
                    if ((w.WaveTimer -= dt) <= 0f && AliveOf(w) < 3 && r.Kills < d.TargetCount)
                    {
                        w.WaveTimer = 8f;
                        for (int i = 0; i < 2; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.5f), null, null);
                    }
                    break;
                case EventObjective.Collect:
                    if ((w.WaveTimer -= dt) <= 0f && AliveOf(w) < 4)
                    {
                        w.WaveTimer = d.WaveInterval;
                        for (int i = 0; i < d.WaveSize; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.Radius * 0.8f), null, null);
                    }
                    break;
                case EventObjective.Boss:
                    if (w.Boss == null || w.Boss.Self.IsDead) r.BossDefeated = true;
                    else if (d.WorldHook == "alpha" && d.Path.Length > 1 && (w.WaveTimer -= dt) <= 0f)
                    {
                        w.WaveTimer = 20f;
                        if (w.Boss.Self.Attackers.Count == 0) { w.PathIndex = (w.PathIndex + 1) % d.Path.Length; w.Boss.Objective = d.Path[w.PathIndex]; }
                    }
                    break;
                case EventObjective.Escort:
                    TickEscort(w, dt);
                    break;
                case EventObjective.Build:
                case EventObjective.Use:
                case EventObjective.Hunt:
                case EventObjective.WorldBoss:
                    TickGeneric(w, dt);
                    break;
            }
        }

        // ---------------- Start / Ende ----------------
        void OnStarted(EventRunner r)
        {
            var w = new World { R = r };
            _worlds.Add(w);
            var d = r.Def;
            History.Add(d.Id);
            GameEventBus.Notify("Ereignis: " + d.Title, new Color(1f, 0.8f, 0.4f));
            switch (d.WorldHook)
            {
                case "dornwacht_defend": _chainResetTimer = -1f; ApplyNeutralWorld(); break;
                case "camp_assault": SpawnCampDefenders(w); break;
                case "outpost_occupied": ApplyOccupiedOutpost(w, d.TargetCount); break;
                case "champion":
                    w.Boss = Spawn(w, Chain != null ? Chain.Champion : d.Boss, A.ChampionSpawn.position, null, null);
                    Spawn(w, d.Trash, A.CampSpawns[1].position, null, null); Spawn(w, d.Trash, A.CampSpawns[5].position, null, null);
                    GameEventBus.Notify("Grolmok, der Aschenfürst, ist in der Rabenschanze erschienen!", new Color(1f, 0.5f, 0.2f));
                    break;
                case "alpha":
                {
                    var pos = d.Path.Length > 0 ? d.Path[Random.Range(0, d.Path.Length)] : d.Center;
                    w.Boss = Spawn(w, d.Boss, pos, null, null);
                    if (w.Boss != null) { w.Boss.Self.DisplayName = "Aschkrallen-Alpha"; w.Boss.UseLeash = false; }
                    GameEventBus.Notify("Der Aschkrallen-Alpha streift durch die Grenzmark!", new Color(1f, 0.5f, 0.2f));
                    break;
                }
                case "elmhain_capture":
                    for (int i = 0; i < 4; i++) Spawn(w, d.Trash, RingPoint(d.Center, d.CaptureRadius * 0.6f), null, null);
                    break;
                case "elmhain_wave":
                    for (int i = 0; i < d.TargetCount; i++) Spawn(w, d.Veteran, RingPoint(d.Center, d.Radius * 0.5f), null, null);
                    break;
                case "sonnfurt_defend":
                    if (HubCrates != null) foreach (var c in HubCrates) if (c != null && c.IsDead) { c.Revive(1f); SetCrateVisual(c, true); }
                    GameEventBus.RaiseBanner("Sonnfurt unter Angriff!", "Die Dornwacht ist gefallen – verteidige den Weiler", new Color(1f, 0.45f, 0.35f));
                    break;
                case "caravan": StartEscort(w); break;
                case "collect": StartCollect(w); break;
                default: StartGeneric(w); break;
            }
            GameEventBus.RaiseEventStateChanged();
        }

        void OnEnded(EventRunner r)
        {
            var w = WorldOf(r);
            var d = r.Def;
            bool ok = r.State == RunnerState.Succeeded;
            History.Add(d.Id + (ok ? ":ok" : ":fail"));
            GrantRewards(r, ok);
            OutcomeWorld(w, ok);
            if (ok && d.MasteryPoints > 0 && PlayerNear(w, 2f) && PlayerProfile.Instance != null)
            {
                var prof = PlayerProfile.Instance; string key = "mastery:" + d.Id;
                if (!prof.State.Completed.Contains(key)) { prof.State.Completed.Add(key); prof.AwardMasteryPoints(d.MasteryPoints, d.Title); }
            }
            if (w != null)
            {
                foreach (var b in w.Spawned) if (b != null && b != w.Boss) Destroy(b.gameObject, ok ? 6f : 12f);
                if (w.Boss != null && !w.Boss.Self.IsDead) Destroy(w.Boss.gameObject, 3f);
                foreach (var g in w.Props) if (g != null) Destroy(g, 4f);
                _worlds.Remove(w);
            }
            if (d.ChainId == "dornwacht" && string.IsNullOrEmpty(ok ? d.SuccessNext : d.FailNext)) _chainResetTimer = ChainResetDelay;
            GameEventBus.RaiseEventStateChanged();
        }

        void GrantRewards(EventRunner r, bool ok)
        {
            var d = r.Def;
            var medal = r.Medal;
            float mult = r.RewardMultiplier;
            int xp = Mathf.RoundToInt(d.Experience * mult), karma = Mathf.RoundToInt(d.Karma * mult), coins = Mathf.RoundToInt(d.Coins * mult);
            ItemDefinition item = null;
            var prof = PlayerProfile.Instance;
            if (mult > 0f && prof != null)
            {
                prof.GrantReward(karma, coins, xp, "Ereignis");
                if (ok && medal == RewardTier.Gold && d.GoldItemRoll && GoldLoot != null && prof.Items != null)
                {
                    var loot = LootRoller.Roll(GoldLoot, prof.Items, new System.Random(Random.Range(0, int.MaxValue)), prof.Progression.Level);
                    if (loot.Items.Count > 0) item = loot.Items[0].def;
                    prof.GrantLoot(loot, d.Title);
                }
            }
            GameEventBus.RaiseBanner(ok ? "Ereignis erfolgreich" : "Ereignis fehlgeschlagen", d.Title, ok ? new Color(1f, 0.85f, 0.45f) : new Color(1f, 0.5f, 0.4f));
            if (UIController.Instance != null && mult > 0f) UIController.Instance.ShowEventReward(d.Title, ok, medal, xp, karma, coins, item);
        }

        // ---------------- Beitrag ----------------
        void OnDamage(DamageInfo info)
        {
            var p = Player;
            if (info.Source == null || info.Source != p || info.Target == null || info.Target.Team != Team.Enemy || info.Amount <= 0f) return;
            foreach (var w in _worlds) if (PlayerNear(w, 1.5f)) { w.R.Contribution += info.Amount; GameEventBus.RaiseContribution(info.Amount); }
        }

        void OnKilled(Combatant victim, Combatant killer)
        {
            if (victim == null) return;
            var p = Player;
            foreach (var w in _worlds)
            {
                var d = w.R.Def;
                if (w.Escort == victim) { Scheduler.Complete(w.R, false); return; }
                if (victim.Team != Team.Enemy) continue;
                var off = victim.transform.position - CenterOf(w); off.y = 0f;
                if (off.magnitude > d.Radius * 1.3f) continue;
                w.R.Kills++;
                if (p != null && (killer == p || victim.Attackers.Contains(p)) && PlayerNear(w, 1.5f))
                {
                    var brain = victim.GetComponent<NpcBrain>();
                    w.R.Contribution += (brain != null && brain.Archetype != null ? brain.Archetype.ContributionValue : 10f) * 40f;
                }
            }
            if (Crates != null && System.Array.IndexOf(Crates, victim) >= 0) { SetCrateVisual(victim, false); GameEventBus.Notify("Eine Vorratskiste wurde zerstört! (" + AliveOf(Crates) + "/" + Crates.Length + ")", new Color(1f, 0.45f, 0.35f)); }
            if (HubCrates != null && System.Array.IndexOf(HubCrates, victim) >= 0) { SetCrateVisual(victim, false); GameEventBus.Notify("Sonnfurts Vorräte brennen! (" + AliveOf(HubCrates) + "/" + HubCrates.Length + ")", new Color(1f, 0.45f, 0.35f)); }
        }

        void OnHealed(Combatant c, float amount)
        {
            foreach (var w in _worlds)
                if ((c == w.Escort || (Guards != null && c != null && c.Team == Team.Ally && c != Player)) && PlayerNear(w, 1.5f)) w.R.Contribution += amount * 0.5f;
        }

        /// <summary>Interaktion (Aufheben/Abliefern) als Beitrag.</summary>
        public void AddInteraction(EventRunner r, float amount) { if (r != null && r.Active) r.Contribution += amount; }

        // ---------------- Karte & Tracker ----------------
        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            foreach (var w in _worlds)
            {
                into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Event, Position = CenterOf(w), Radius = w.R.Def.Radius, Label = w.R.Def.Title, Owner = this, Icon = EventDefinition.IconFor(w.R.Def.Objective) });
                if (w.Boss != null && !w.Boss.Self.IsDead && w.Boss.Self.Tier != EnemyTier.Champion)
                    into.Add(new MapMarkerInfo { Kind = MapMarkerKind.EventBoss, Position = w.Boss.transform.position, Label = w.Boss.Self.DisplayName, Owner = w.Boss.Self });
            }
            CollectTimerMarkers(into);
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            if (info.Kind == MapMarkerKind.WorldBoss) return TimerTooltip(info);
            foreach (var w in _worlds)
            {
                if (w.R.Def.Title != info.Label) continue;
                var d = w.R.Def; var r = w.R;
                var c = new TooltipContent { Title = d.Title, TitleColor = new Color(1f, 0.65f, 0.25f), Subtitle = "Ereignis · " + EventDefinition.ObjectiveName(d.Objective) };
                c.Add(d.Description, new Color(0.9f, 0.9f, 0.92f));
                c.Add("Ziel: " + ObjectiveText(r), new Color(0.92f, 0.92f, 0.95f));
                if (r.TimeLeft >= 0f) c.Add("Restzeit: " + Mathf.FloorToInt(r.TimeLeft / 60f) + ":" + Mathf.FloorToInt(r.TimeLeft % 60f).ToString("00"), new Color(0.92f, 0.92f, 0.95f), UiFactory.Sprite("fact_time"));
                c.Add("Belohnung (Gold): " + d.Experience + " EP · " + d.Karma + " Karma · " + WalletModel.Format(d.Coins), new Color(0.9f, 0.85f, 0.6f));
                c.Add("Dein Beitrag: " + EventChain.TierName(r.Medal), EventChain.TierColor(r.Medal));
                return c;
            }
            return null;
        }

        public string ObjectiveText(EventRunner r)
        {
            var d = r.Def; var w = WorldOf(r);
            switch (d.Objective)
            {
                case EventObjective.Kill: return "Besiegt: " + r.Kills + "/" + d.TargetCount;
                case EventObjective.Defend: { var objs = d.WorldHook == "sonnfurt_defend" ? HubCrates : Crates; return "Vorräte: " + AliveOf(objs) + "/" + (objs != null ? objs.Length : 0); }
                case EventObjective.Escort: return "Karawane: " + Mathf.RoundToInt(r.EscortProgress * 100f) + " %" + (w != null && w.Escort != null ? " · HP " + Mathf.RoundToInt(w.Escort.HealthFraction * 100f) + " %" : "");
                case EventObjective.Collect: return "Abgeliefert: " + r.Delivered + "/" + d.TargetCount + (r.Carried > 0 ? " · getragen: " + r.Carried : "");
                case EventObjective.Capture: return "Einnahme: " + Mathf.RoundToInt(r.Capture * 100f) + " % (nur ohne Gegner im Kreis)";
                case EventObjective.Boss: return w != null && w.Boss != null ? w.Boss.Self.DisplayName + ": " + Mathf.RoundToInt(w.Boss.Self.HealthFraction * 100f) + " %" : "Besiegt";
                case EventObjective.Build:
                case EventObjective.Use: return d.SiteLabel + ": " + r.Built + "/" + d.TargetCount;
                case EventObjective.Hunt: return r.Phase == 0 ? "Gejagt: " + Mathf.Min(r.Kills, d.TargetCount) + "/" + d.TargetCount : (w != null && w.Boss != null ? "Anführer: " + w.Boss.Self.DisplayName + " " + Mathf.RoundToInt(w.Boss.Self.HealthFraction * 100f) + " %" : "Anführer besiegt");
                case EventObjective.WorldBoss: return (w != null && w.Boss != null ? w.Boss.Self.DisplayName + ": " + Mathf.RoundToInt(w.Boss.Self.HealthFraction * 100f) + " %" : "Besiegt") + " · Phase " + (r.Phase + 1) + "/" + ((d.PhaseThresholds != null ? d.PhaseThresholds.Length : 0) + 1);
            }
            return "";
        }

        public struct TrackerItem { public string Title, Desc, Medal; public float Progress, TimeLeft, Distance; public Color MedalColor; public Vector3 Focus; }
        readonly List<TrackerItem> _tracker = new List<TrackerItem>();

        /// <summary>Aktive Ereignisse in Reichweite, nach Distanz sortiert (max. 4).</summary>
        public List<TrackerItem> TrackerItems()
        {
            _tracker.Clear();
            var p = Player;
            foreach (var w in _worlds)
            {
                if (!PlayerNear(w, 2.5f)) continue;
                var r = w.R;
                float dist = p != null ? Vector3.Distance(p.transform.position, CenterOf(w)) : 0f;
                string extra = PlayerNear(w, 1.5f) ? "" : "\n<color=#ffb080>Außerhalb des Ereignis-Bereichs</color>";
                _tracker.Add(new TrackerItem { Title = r.Def.Title, Desc = ObjectiveText(r) + extra, Progress = r.Progress, TimeLeft = r.TimeLeft, Medal = "Beitrag: " + EventChain.TierName(r.Medal), MedalColor = EventChain.TierColor(r.Medal), Focus = CenterOf(w), Distance = dist });
            }
            _tracker.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            if (_tracker.Count > TrackerHud.MaxEntries) _tracker.RemoveRange(TrackerHud.MaxEntries, _tracker.Count - TrackerHud.MaxEntries);
            return _tracker;
        }

        void Areas(List<(Vector3 center, float radius)> into)
        {
            foreach (var w in _worlds) into.Add((CenterOf(w), w.R.Def.Radius));
            if (Scheduler != null && Scheduler.HasFlag(FlagDornwachtFallen) && A != null) into.Add((A.OutpostCenter.position, 30f));
        }
    }
}
