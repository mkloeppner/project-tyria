using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: Persönliche Geschichte zur Laufzeit (am GameManager-Objekt, kein Singleton).
    /// Übersetzt Spielereignisse in Story-Ziele, setzt den aktuellen Schritt in die Welt um (grüner Marker, Story-NPC,
    /// Story-Portal, Instanz mit Grenze, Gegnern, Objekten und Gefährte), spielt Start-/End-Szenen, vergibt Belohnungen und Briefe.
    /// </summary>
    public partial class StoryDirector : MonoBehaviour, IMapMarkerProvider
    {
        public StoryDatabase Database;
        public readonly InstanceModel Instance = new InstanceModel();
        public readonly BundleModel Bundle = new BundleModel();
        public static readonly Color StoryGreen = new Color(0.35f, 0.95f, 0.45f);

        public StoryState State => PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Story : null;
        public StoryStep Step => StoryModel.Current(Database, State);
        public StoryChapter Chapter => StoryModel.CurrentChapter(Database, State);
        int Level => PlayerProfile.Instance != null ? PlayerProfile.Instance.State.Progression.Level : 1;
        string Zone => GameManager.Instance != null && GameManager.Instance.CurrentZone != null ? GameManager.Instance.CurrentZone.Id : ProfileState.StartZoneId;
        Combatant Player => PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;

        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly List<Combatant> _instanceEnemies = new List<Combatant>();
        StoryStep _presentFor; string _presentZone; int _spawnedObjective = -2;
        bool _finishing; float _tick; float _surviveAcc;
        Vector3 _instanceEntrance; string _instanceZone;
        GameObject _boundary;
        public bool InInstance => Instance.Active;
        public bool Busy => _finishing || (GameManager.Instance != null && GameManager.Instance.Dialogue != null && GameManager.Instance.Dialogue.Playing);

        void Awake() { if (Database != null) StoryDatabase.Loaded = Database; }

        void OnEnable()
        {
            MapMarkerRegistry.Register(this);
            GameEventBus.EntityKilled += OnKilled;
            GameEventBus.DodgePerformed += OnDodge;
            GameEventBus.PlayerRallied += OnRallied;
            GameEventBus.HealSkillUsed += OnHealSkill;
            GameEventBus.WeaponSwapped += OnSwap;
            GameEventBus.ComboTriggered += OnCombo;
            GameEventBus.ZoneProgress += OnZoneProgress;
            GameEventBus.CastInterrupted += OnInterrupted;
            GameEventBus.PlayerDefeated += OnDefeated;
            DefianceBar.Broken += OnDefianceBroken;
            MountController.MountChanged += OnMount;
            EventDirector.ZoneBound += OnZoneBound;
        }

        void OnDisable()
        {
            MapMarkerRegistry.Unregister(this);
            GameEventBus.EntityKilled -= OnKilled;
            GameEventBus.DodgePerformed -= OnDodge;
            GameEventBus.PlayerRallied -= OnRallied;
            GameEventBus.HealSkillUsed -= OnHealSkill;
            GameEventBus.WeaponSwapped -= OnSwap;
            GameEventBus.ComboTriggered -= OnCombo;
            GameEventBus.ZoneProgress -= OnZoneProgress;
            GameEventBus.CastInterrupted -= OnInterrupted;
            GameEventBus.PlayerDefeated -= OnDefeated;
            DefianceBar.Broken -= OnDefianceBroken;
            MountController.MountChanged -= OnMount;
            EventDirector.ZoneBound -= OnZoneBound;
        }

        // ------------------------------------------------------------ Ereignisse → Ziele
        void OnKilled(Combatant victim, Combatant killer)
        {
            if (victim == null || victim.Team != Team.Enemy || Player == null) return;
            if (killer != Player && !victim.Attackers.Contains(Player) && !_instanceEnemies.Contains(victim)) return;
            var brain = victim.GetComponent<NpcBrain>();
            string id = (brain != null && brain.Archetype != null ? brain.Archetype.name : "") + ":" + victim.DisplayName;
            Report(ObjectiveKind.Kill, id); Report(ObjectiveKind.KillTag, id); Report(ObjectiveKind.Boss, id);
            _instanceEnemies.Remove(victim);
        }
        void OnDodge(Combatant c) { if (c == Player) Report(ObjectiveKind.Dodge, "*"); }
        void OnRallied(Combatant c) { if (c == Player) Report(ObjectiveKind.Rally, "*"); }
        void OnHealSkill(Combatant c, SkillDefinition s) { if (c == Player) Report(ObjectiveKind.Heal, "*"); }
        void OnSwap(Combatant c) { if (c == Player) Report(ObjectiveKind.WeaponSwap, "*"); }
        void OnCombo(ComboEntry e, Vector3 p) { if (Player != null && (p - Player.transform.position).sqrMagnitude < 40f * 40f) { Report(ObjectiveKind.Combo, "*"); HeartContribution(HeartWay.Combo, p, 0.1f, "Kombo"); } }
        void OnInterrupted(Combatant c, SkillDefinition s) { if (c != null && c.Team == Team.Enemy) Report(ObjectiveKind.Interrupt, "*"); }
        void OnDefianceBroken(Combatant c) { var b = c != null ? c.GetComponent<NpcBrain>() : null; Report(ObjectiveKind.Defiance, (b != null && b.Archetype != null ? b.Archetype.name : "") + ":" + (c != null ? c.DisplayName : "")); }
        void OnMount(MountKind k) { if (k != MountKind.None) Report(ObjectiveKind.Mount, k.ToString()); }
        void OnZoneProgress(string id)
        {
            if (string.IsNullOrEmpty(id) || PlayerProfile.Instance == null || !PlayerProfile.Instance.State.Completed.Contains(id)) return;
            Report(ObjectiveKind.Heart, id); Report(ObjectiveKind.Waypoint, id); Report(ObjectiveKind.Vista, id);
        }
        void OnZoneBound(ZoneRoot root)
        {
            _presentFor = null;   // Zonenwechsel: Präsenz neu aufbauen
            var ed = EventDirector.Instance; if (ed != null && ed.Scheduler != null) { ed.Scheduler.Ended -= OnEventEnded; ed.Scheduler.Ended += OnEventEnded; }
        }
        void OnEventEnded(EventRunner r) { if (r != null && r.State == RunnerState.Succeeded && r.Def != null) { Report(ObjectiveKind.Event, r.Def.Id); Report(ObjectiveKind.Meta, r.Def.Id); } }
        void OnDefeated(Combatant c)
        {
            if (c != Player || !Instance.Active) return;
            var st = Step; int from = Instance.Fail(st, State);
            GameEventBus.RaiseBanner("Instanz gescheitert", "Neustart am letzten Kontrollpunkt", new Color(1f, 0.6f, 0.4f));
            StartCoroutine(RestartInstance(from));
        }

        /// <summary>Zentrale Meldung (auch von Späher, Bündel, Herzen, Debug).</summary>
        public void Report(ObjectiveKind kind, string target, int amount = 1)
        {
            var s = State; if (s == null || Database == null || _finishing) return;
            var st = Step; if (st == null) return;
            int before = StoryModel.FirstOpenObjective(st, s);
            if (!StoryModel.Report(Database, s, kind, target, Level, amount)) return;
            int after = StoryModel.FirstOpenObjective(st, s);
            if (after != before)
            {
                if (Instance.Active && before >= 0 && Instance.Reach(st, s, before)) GameEventBus.Notify("Kontrollpunkt erreicht", StoryGreen);
                OnObjectiveStarted(st, after);
            }
            GameEventBus.RaiseProfileChanged();
            if (StoryModel.StepDone(st, s)) StartCoroutine(FinishStep(st));
        }

        // ------------------------------------------------------------ Ablauf
        void Update()
        {
            if (Database == null || State == null || Player == null) return;
            Instance.Tick(Time.deltaTime);
            ScanHeartPopups();
            if ((_tick += Time.deltaTime) < 0.25f) return;
            float dt = _tick; _tick = 0f;
            var st = Step;
            if (st != _presentFor || Zone != _presentZone) BuildPresence(st);
            if (st == null || _finishing) return;
            PollStateObjectives(st, dt);
            if (Instance.Active) KeepInBoundary(st);
        }

        EquipmentModel _hookedEquip; TraitModel _hookedTraits;
        void HookProfileModels()
        {
            var ps = PlayerProfile.Instance != null ? PlayerProfile.Instance.State : null; if (ps == null) return;
            if (_hookedEquip != ps.Equipment) { if (_hookedEquip != null) _hookedEquip.Changed -= OnEquip; _hookedEquip = ps.Equipment; _hookedEquip.Changed += OnEquip; }
            if (_hookedTraits != ps.Traits) { if (_hookedTraits != null) _hookedTraits.Changed -= OnTraits; _hookedTraits = ps.Traits; _hookedTraits.Changed += OnTraits; }
        }
        void OnEquip() => Report(ObjectiveKind.Equip, "*");
        void OnTraits() => Report(ObjectiveKind.Trait, "*");

        void PollStateObjectives(StoryStep st, float dt)
        {
            HookProfileModels();
            var s = State; int open = StoryModel.FirstOpenObjective(st, s);
            for (int i = 0; i < st.Objectives.Count; i++)
            {
                var o = st.Objectives[i]; if (!o.Optional && i != open) continue;
                switch (o.Kind)
                {
                    case ObjectiveKind.Reach: if (st.Zone == Zone && Flat(Player.transform.position - (st.Position + o.Offset)) < (o.Scale > 1f ? o.Scale : 14f)) Report(ObjectiveKind.Reach, st.Id); break;
                    case ObjectiveKind.Glide: { var m = Player.GetComponent<MasteryAbilities>(); if (m != null && m.Gliding) Report(ObjectiveKind.Glide, "*"); } break;
                    case ObjectiveKind.Survive: if (Instance.Active && !Busy) { _surviveAcc += dt; if (_surviveAcc >= 1f) { _surviveAcc -= 1f; Report(ObjectiveKind.Survive, "*"); } } break;
                    case ObjectiveKind.Mastery: if (PlayerProfile.Instance.Account != null && !string.IsNullOrEmpty(PlayerProfile.Instance.Account.SelectedMastery)) Report(ObjectiveKind.Mastery, "*"); break;
                    case ObjectiveKind.Elite: if (!string.IsNullOrEmpty(PlayerProfile.Instance.State.Traits.ActiveElite)) Report(ObjectiveKind.Elite, "*"); break;
                    case ObjectiveKind.Cleanse: if (_cleansed > 0) { Report(ObjectiveKind.Cleanse, "*", _cleansed); _cleansed = 0; } break;
                }
            }
        }

        int _cleansed;
        /// <summary>Vom Effekt-System (Reinigung) gemeldet.</summary>
        public void NotifyCleanse(int n) { if (n > 0) _cleansed += n; }

        static float Flat(Vector3 d) { d.y = 0f; return d.magnitude; }

        Combatant _partner; int _partnerObjective = -1;

        void OnObjectiveStarted(StoryStep st, int index)
        {
            if (index < 0 || index >= st.Objectives.Count) return;
            var o = st.Objectives[index];
            if (_partner != null && _partnerObjective != index) { var pg = _partner.gameObject; _instanceEnemies.Remove(_partner); _spawned.Remove(pg); VfxPool.Instance?.Play(VfxIds.Compose(VfxShape.Veil, VfxPalette.Schatten, VfxSize.M), pg.transform.position, Quaternion.identity); Destroy(pg); _partner = null; }   // Übungs-Gegner zieht sich zurück
            ShowHint(o.Hint);
            if (!string.IsNullOrEmpty(o.Scene)) PlayScene(o.Scene, null);
            if (Instance.Active || st.Type != StepType.Instance) SpawnFor(st, index);
        }

        IEnumerator FinishStep(StoryStep st)
        {
            if (_finishing) yield break;
            _finishing = true;
            while (GameManager.Instance.Dialogue != null && GameManager.Instance.Dialogue.Playing) yield return null;
            if (!string.IsNullOrEmpty(st.SceneEnd)) { bool done = false; PlayScene(st.SceneEnd, () => done = true); while (!done) yield return null; }
            bool wasInstance = Instance.Active;
            var ch = Chapter;
            var r = StoryModel.Complete(Database, State);
            Grant(r.Reward, st.Title);
            if (State.HasFlag("greif_vertrauen") && PlayerProfile.Instance != null && !PlayerProfile.Instance.OwnsMount(MountKind.Griffon)) { PlayerProfile.Instance.UnlockMount(MountKind.Griffon); GameEventBus.RaiseBanner("Greif", "Neues Reittier – X halten zum Wählen", StoryGreen); }   // Lauf 6: Greif über die Geschichte
            if (!string.IsNullOrEmpty(r.Letter)) SendLetter(r.Letter);
            if (r.ChapterDone && ch != null)
            {
                GameEventBus.RaiseBanner("Kapitel " + ch.Number + " abgeschlossen", ch.Title, StoryGreen);
                if (r.StoryDone) { GameEventBus.RaiseBanner("Die Geschichte ist erzählt", "Chronik des Tyrannen", StoryGreen); PlayerProfile.Instance?.AwardMasteryPoints(3, "Persönliche Geschichte"); }
            }
            PlayerProfile.Instance?.RequestSave("Geschichte");
            if (wasInstance && UIController.Instance != null && !StoryAdmin.Active) { bool left = false; UIController.Instance.ShowStoryReward(st.Title, r.Reward, () => left = true); while (!left) yield return null; LeaveInstance(); }
            else if (wasInstance) LeaveInstance();   // Lauf 10: Admin-Ablauf / ohne UI
            _presentFor = null; _finishing = false;
            GameEventBus.RaiseProfileChanged();
        }

        void Grant(StoryReward rw, string title)
        {
            var p = PlayerProfile.Instance; if (p == null || rw == null) return;
            p.GrantReward(rw.Karma, rw.Copper, rw.Experience, "Geschichte: " + title);
            if (rw.HeroPoints > 0) p.State.Progression.GrantHeroPoints(rw.HeroPoints);
            if (rw.MasteryPoints > 0) p.AwardMasteryPoints(rw.MasteryPoints, "Geschichte");
            if (!string.IsNullOrEmpty(rw.Item) && p.Items != null) { var it = p.Items.Get(rw.Item); if (it != null) p.GiveItem(it); }
        }

        public void SendLetter(string id)
        {
            if (Database == null || Database.Letter(id) == null) return;
            if (MailModel.Send(State, id, System.DateTime.Now.ToString("dd.MM. HH:mm"))) { GameEventBus.Notify("Neuer Brief: " + Database.Letter(id).Subject, new Color(0.95f, 0.9f, 0.7f)); UiFactory.Sound("Mail"); GameEventBus.RaiseProfileChanged(); }
        }

        public void ShowHint(string id)
        {
            if (string.IsNullOrEmpty(id) || Database == null) return;
            var h = Database.Hint(id); if (h == null || !HintModel.ShouldShow(State, id, !id.StartsWith("story_"))) return;
            HintModel.MarkSeen(State, id);
            UIController.Instance?.ShowHint(h);
        }

        public void PlayScene(string id, System.Action done)
        {
            var sc = Database != null ? Database.Scene(id) : null;
            var dp = GameManager.Instance != null ? GameManager.Instance.Dialogue : null;
            if (sc == null || dp == null) { done?.Invoke(); return; }
            Instance.Paused = true;
            dp.Play(sc, () => { Instance.Paused = false; if (!State.SeenScenes.Contains(id)) State.SeenScenes.Add(id); done?.Invoke(); });
        }

        // ------------------------------------------------------------ Präsenz in der Welt
        void ClearPresence()
        {
            foreach (var g in _spawned) if (g != null) Destroy(g);
            _spawned.Clear(); _spawnedObjective = -2;
        }

        void BuildPresence(StoryStep st)
        {
            _presentFor = st; _presentZone = Zone;
            if (!Instance.Active) ClearPresence();
            if (st == null || st.Zone != Zone || !StoryModel.Available(Database, State, Level)) return;
            if (Instance.Active) return;
            switch (st.Type)
            {
                case StepType.Talk: _spawned.Add(StoryNpc.Spawn(this, st)); break;
                case StepType.Instance: _spawned.Add(StoryPortal.Spawn(this, st)); break;
                default:
                    if (!string.IsNullOrEmpty(st.SceneStart) && !State.SeenScenes.Contains(st.SceneStart)) PlayScene(st.SceneStart, null);
                    if (!string.IsNullOrEmpty(st.Npc)) _spawned.Add(StoryNpc.Spawn(this, st, true));
                    SpawnFor(st, StoryModel.FirstOpenObjective(st, State));
                    break;
            }
            ShowHintForCurrent(st);
        }

        void ShowHintForCurrent(StoryStep st) { int i = StoryModel.FirstOpenObjective(st, State); if (i >= 0) ShowHint(st.Objectives[i].Hint); }

        /// <summary>Gegner, Objekte und Figuren für ein Ziel (Instanz: immer; offene Welt: Objekte/Figuren).</summary>
        void SpawnFor(StoryStep st, int index)
        {
            if (index < 0 || index >= st.Objectives.Count || index == _spawnedObjective) return;
            _spawnedObjective = index;
            var o = st.Objectives[index];
            Vector3 at = st.Position + o.Offset;
            if (Instance.Active && !string.IsNullOrEmpty(o.Partner))   // Lern-Ziel mit Übungs-Gegner (z. B. Ausweichen vor einem Brecher)
            {
                var pa = Database.Archetype(o.Partner);
                var pb = pa != null ? NpcBrain.Spawn(pa, at, Quaternion.LookRotation(Player.transform.position - at)) : null;
                if (pb != null)
                {
                    pb.LevelOverride = Level; pb.ApplyArchetype(); pb.PreferredTarget = Player;
                    var pc = pb.GetComponent<Combatant>(); if (!string.IsNullOrEmpty(o.Label)) pc.DisplayName = o.Label;
                    _instanceEnemies.Add(pc); _spawned.Add(pb.gameObject); _partner = pc; _partnerObjective = index;
                }
            }
            switch (o.Kind)
            {
                case ObjectiveKind.Kill: case ObjectiveKind.Boss: case ObjectiveKind.Defiance: case ObjectiveKind.KillTag:
                    if (!Instance.Active) break;
                    var a = Database.Archetype(o.Target); if (a == null) break;
                    int n = o.Kind == ObjectiveKind.Kill || o.Kind == ObjectiveKind.KillTag ? Mathf.Max(1, o.Count) : 1;
                    for (int i = 0; i < n; i++)
                    {
                        float ang = i * Mathf.PI * 2f / n; var pos = at + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (n > 1 ? 4f : 0f);
                        var b = NpcBrain.Spawn(a, pos, Quaternion.LookRotation(Player.transform.position - pos));
                        if (b == null) continue;
                        b.LevelOverride = Level; b.ApplyArchetype();
                        var c = b.GetComponent<Combatant>();
                        if (!string.IsNullOrEmpty(o.Label)) c.DisplayName = o.Label;
                        if (Mathf.Abs(o.Scale - 1f) > 0.01f) c.SetMaxHealth(c.MaxHealth * o.Scale, true);   // <1: Lern-Gegner (Kapitel 1)
                        b.PreferredTarget = Player;
                        _instanceEnemies.Add(c); _spawned.Add(b.gameObject);
                    }
                    break;
                case ObjectiveKind.Interact: case ObjectiveKind.Bundle:
                    for (int i = 0; i < Mathf.Max(1, o.Count); i++)
                    {
                        float ang = i * Mathf.PI * 2f / Mathf.Max(1, o.Count);
                        _spawned.Add(StoryObject.Spawn(this, st, o, at + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * (o.Count > 1 ? 6f : 0f)));
                    }
                    break;
                case ObjectiveKind.Revive:
                    _spawned.Add(StoryObject.SpawnDowned(this, st, o, at));
                    break;
                case ObjectiveKind.Escort:
                    _spawned.Add(StoryObject.SpawnEscort(this, st, o, at));
                    break;
            }
        }

        // ------------------------------------------------------------ Instanz
        public void EnterInstance(StoryStep st)
        {
            if (st == null || Instance.Active || Busy) return;
            StartCoroutine(EnterRoutine(st));
        }

        IEnumerator EnterRoutine(StoryStep st)
        {
            var ui = UIController.Instance;
            var z = GameManager.Instance.CurrentZone;
            _instanceEntrance = Player.transform.position; _instanceZone = Zone;
            if (ui != null) { ui.ShowLoading(z, "Story-Instanz: " + st.Title, 0.2f); }
            yield return null;
            ClearPresence();
            Instance.Enter(st, State);
            SetZoneQuiet(st, true);
            var pc = Player.GetComponent<PlayerController>();
            var pos = ZoneAnchors.GroundAt(st.Position + Vector3.up * 3f);
            if (pc != null) pc.Teleport(pos, Quaternion.identity); else Player.transform.position = pos;
            if (ui != null) ui.ShowLoading(z, null, 1f);
            yield return new WaitForSeconds(0.25f);
            ui?.HideLoading();
            _boundary = StoryBoundary.Create(st.Position, st.Radius);
            if (st.Props != null) foreach (var pr in st.Props) if (!string.IsNullOrEmpty(pr)) _spawned.Add(StoryProps.Spawn(pr, st.Position));   // Brand, Barrikaden, Zelte …
            if (!string.IsNullOrEmpty(st.Companion)) _spawned.Add(StoryCompanion.Spawn(this, ResolveCompanion(st.Companion), Player.transform.position + Player.transform.right * 2f));
            GameEventBus.RaiseBanner(st.Title, "Story-Instanz", StoryGreen);
            ShowHint("story_instance");
            if (!string.IsNullOrEmpty(st.SceneStart)) { bool done = false; PlayScene(st.SceneStart, () => done = true); while (!done) yield return null; }
            int from = Mathf.Max(0, StoryModel.FirstOpenObjective(st, State));
            _spawnedObjective = -2; OnObjectiveStarted(st, from);
        }

        IEnumerator RestartInstance(int from)
        {
            yield return new WaitForSeconds(1.5f);
            var st = Step; if (st == null) yield break;
            foreach (var c in _instanceEnemies) if (c != null) Destroy(c.gameObject);
            _instanceEnemies.Clear();
            var pc = Player.GetComponent<PlayerController>();
            var dc = Player.GetComponent<DownedController>(); if (dc != null) dc.ResetState();
            Player.Revive(1f);
            var pos = ZoneAnchors.GroundAt(st.Position + Vector3.up * 3f);
            if (pc != null) pc.Teleport(pos, Quaternion.identity);
            _spawnedObjective = -2; OnObjectiveStarted(st, Mathf.Max(0, StoryModel.FirstOpenObjective(st, State)));
        }

        public string ResolveCompanion(string id) => id == "pact" ? (State.PactE == Pact.Wacht ? "haldis" : State.PactE == Pact.Archiv ? "tovin" : State.PactE == Pact.Schleier ? "veyra" : "mira") : id;

        public void LeaveInstance()
        {
            if (!Instance.Active) return;
            var st = Step;
            Instance.Leave();
            ClearPresence();
            foreach (var c in _instanceEnemies) if (c != null) Destroy(c.gameObject);
            _instanceEnemies.Clear();
            if (_boundary != null) Destroy(_boundary);
            SetZoneQuiet(null, false);
            var pc = Player != null ? Player.GetComponent<PlayerController>() : null;
            if (pc != null && _instanceZone == Zone) pc.Teleport(ZoneAnchors.GroundAt(_instanceEntrance + Vector3.up * 2f), Player.transform.rotation);
            _presentFor = null;
            GameEventBus.Notify("Instanz verlassen", StoryGreen);
        }

        readonly List<GameObject> _quieted = new List<GameObject>();
        /// <summary>Instanz-Modus: Ereignisse pausiert, Zonen-Gegner und Ambient im Instanzbereich ausgeblendet (dieselbe Zonen-Szene).</summary>
        void SetZoneQuiet(StoryStep st, bool quiet)
        {
            var ed = EventDirector.Instance; if (ed != null) ed.Paused = quiet;
            if (quiet)
            {
                float r = st.Radius * 1.6f;
                foreach (var c in new List<Combatant>(Combatant.All))   // Kopie: SetActive(false) meldet sich aus Combatant.All ab
                {
                    // nur Gegner ausblenden: Verbündete/Ereignis-Figuren mit Rig + Navmesh-Agent hängen beim Reaktivieren (Editor-Freeze im M29-Test)
                    if (c == null || c == Player || c.Team != Team.Enemy || _instanceEnemies.Contains(c) || c.GetComponentInParent<StoryCompanion>() != null || c.GetComponentInParent<StoryObject>() != null) continue;
                    if (Flat(c.transform.position - st.Position) > r) continue;
                    ActorVisibility.SetHidden(c.gameObject, true); _quieted.Add(c.gameObject);
                }
            }
            else { foreach (var g in _quieted) if (g != null) ActorVisibility.SetHidden(g, false); _quieted.Clear(); }
        }

        void KeepInBoundary(StoryStep st)
        {
            var d = Player.transform.position - st.Position; d.y = 0f;
            if (d.magnitude <= st.Radius) return;
            var pc = Player.GetComponent<PlayerController>();
            var back = st.Position + d.normalized * (st.Radius - 2f);
            if (pc != null) pc.Teleport(ZoneAnchors.GroundAt(back + Vector3.up * 2f), Player.transform.rotation);
            GameEventBus.Notify("Grenze der Story-Instanz", StoryGreen);
        }

        // ------------------------------------------------------------ Karte, Tracker
        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            var st = Step; if (st == null || Database == null) return;
            if (st.Zone == Zone || string.IsNullOrEmpty(st.Zone))
                into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Story, State = MarkerState.Open, Position = st.Position, Label = "Geschichte: " + st.Title, Owner = this, Icon = "map_story" });
        }

        public TooltipContent MarkerTooltip(MapMarkerInfo info)
        {
            if ((Object)info.Owner != this) return null;
            var st = Step; if (st == null) return null;
            var c = new TooltipContent(); c.Title = st.Title; c.TitleColor = StoryGreen;
            c.Add("Kapitel " + State.Chapter + ": " + (Chapter != null ? Chapter.Title : ""), new Color(0.8f, 0.9f, 0.8f));
            c.Add(st.Description, Color.white);
            foreach (var line in ObjectiveLines()) c.Add(line, new Color(0.85f, 0.85f, 0.8f));
            if (!StoryModel.Available(Database, State, Level)) c.Add("Ab Stufe " + Chapter.MinLevel, new Color(1f, 0.5f, 0.4f));
            return c;
        }

        public List<string> ObjectiveLines()
        {
            var list = new List<string>(); var st = Step; var s = State; if (st == null) return list;
            int open = StoryModel.FirstOpenObjective(st, s);
            for (int i = 0; i < st.Objectives.Count; i++)
            {
                var o = st.Objectives[i]; int c = s.Counter(st.Id, i), need = Mathf.Max(1, o.Count);
                if (!o.Optional && i > open && open >= 0) continue;
                list.Add((c >= need ? "√ " : "• ") + o.Text + (need > 1 ? "  " + Mathf.Min(c, need) + "/" + need : "") + (o.Optional ? " (optional)" : ""));
            }
            return list;
        }

        /// <summary>Tracker-Zeile oben im HUD („Geschichte: … – Dornwacht, 420 m“).</summary>
        public string TrackerText()
        {
            var st = Step; if (st == null || Player == null) return State != null && State.Finished ? "" : null;
            if (!StoryModel.Available(Database, State, Level)) return "Geschichte: Kapitel " + State.Chapter + " ab Stufe " + Chapter.MinLevel;
            int open = StoryModel.FirstOpenObjective(st, State);
            string goal = open >= 0 ? st.Objectives[open].Text : st.Title;
            if (open >= 0) { int c = State.Counter(st.Id, open), need = Mathf.Max(1, st.Objectives[open].Count); if (need > 1) goal += " " + Mathf.Min(c, need) + "/" + need; }
            string where = st.Zone != Zone ? " – " + ZoneName(st.Zone) : " – " + Mathf.RoundToInt(Flat(Player.transform.position - st.Position)) + " m";
            return "Geschichte: " + goal + where;
        }

        public static string ZoneName(string id)
        {
            var w = GameManager.Instance != null ? GameManager.Instance.World : null;
            if (w != null) foreach (var z in w.Zones) if (z != null && z.Id == id) return z.DisplayName;
            return id;
        }

        // ------------------------------------------------------------ Debug
        public string DebugSetChapter(int chapter, int step)
        {
            var s = State; if (s == null) return "kein Profil";
            ClearPresence(); if (Instance.Active) LeaveInstance();
            s.Chapter = Mathf.Clamp(chapter, 1, 10); s.Step = Mathf.Max(0, step); s.Finished = false; s.Counters.Clear();
            _presentFor = null; GameEventBus.RaiseProfileChanged();
            return "Kapitel " + s.Chapter + " Schritt " + (s.Step + 1) + ": " + (Step != null ? Step.Title : "–");
        }

        public string DebugCompleteStep()
        {
            var st = Step; if (st == null) return "kein Schritt";
            for (int i = 0; i < st.Objectives.Count; i++) State.SetCounter(st.Id, i, Mathf.Max(1, st.Objectives[i].Count));
            StartCoroutine(FinishStep(st)); return "abgeschlossen: " + st.Title;
        }
    }
}
