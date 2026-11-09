using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Fünfter erlaubter Singleton: hält Fortschritt, Geldbörse, Inventar, Ausrüstung, Freischaltungen und Zonenabschluss
    /// des Spielers (ProfileState) und kümmert sich um Speichern/Laden (Autosave alle 60 s und bei Fortschritt).
    /// </summary>
    [DefaultExecutionOrder(-140)]
    public partial class PlayerProfile : MonoBehaviour
    {
        public static PlayerProfile Instance { get; private set; }

        public ItemDatabase Items;
        public SkillDefinition[] AllSkills;
        public Combatant Player;
        public float AutosaveInterval = 60f;
        public bool LoadOnStart = true;
        public float FogRevealRadius = 60f;

        public ProfileState State { get; private set; } = new ProfileState();
        public float LastSaveTime { get; private set; } = -1f;
        public string LastSaveInfo { get; private set; } = "";

        float _autosaveTimer, _fogTimer;
        bool _started;

        public ProgressionModel Progression => State.Progression;

        /// <summary>Höchstes Level der aktuellen Zone (0 = keine Herabstufung).</summary>
        public int ZoneMaxLevel { get; private set; }
        /// <summary>Effektives Level nach Herabstufung (GW2: Gebiets-Maximum + 1).</summary>
        public int EffectiveLevel => WorldModel.EffectiveLevel(State.Progression.Level, ZoneMaxLevel);

        /// <summary>Wendet die Herabstufung für eine Zone an (Attribute, Ausrüstung, Waffenstärke skalieren mit).</summary>
        public void SetZoneLevelCap(int zoneMaxLevel)
        {
            ZoneMaxLevel = zoneMaxLevel;
            if (Player == null) return;
            Player.Level = EffectiveLevel;
            ApplyExtra();
            Player.Stats.MarkDirty();
            GameEventBus.RaiseProfileChanged();
        }
        public WalletModel Wallet => State.Wallet;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) { Instance = null; EquipmentModel.WeaponAllowed = null; } TeardownWorld(); }   // statische Klassenbindung nicht über die Spielsitzung hinaus behalten
        partial void TeardownWorld();

        void OnEnable() { GameEventBus.EntityKilled += OnKilled; }
        void OnDisable() { GameEventBus.EntityKilled -= OnKilled; }

        void Start()
        {
            if (Player == null && GameManager.Instance != null) Player = GameManager.Instance.Player;
            EnsureAccount();
            GameMenu.OpenCharacterSelect = () => { Save("Charakterwechsel"); UnityEngine.SceneManagement.SceneManager.LoadScene(CharacterSelectController.SceneName); };
            ApplyClassData(State.ClassId);
            InitRuntime();
            EnsureMastery();
            if (LoadOnStart && System.IO.File.Exists(AccountStore.CharacterPath(ActiveSlot))) Load();
            else StartFreshCharacter();
            _started = true;
        }

        partial void InitRuntimeExtra();
        partial void ApplyExtra();

        void InitRuntime()
        {
            HookState();
            InitRuntimeExtra();
        }

        void HookState()
        {
            State.Progression.LeveledUp += OnLevelUp;
            State.Progression.Changed += GameEventBus.RaiseProfileChanged;
            State.Wallet.Changed += GameEventBus.RaiseProfileChanged;
        }

        void UnhookState()
        {
            State.Progression.LeveledUp -= OnLevelUp;
            State.Progression.Changed -= GameEventBus.RaiseProfileChanged;
            State.Wallet.Changed -= GameEventBus.RaiseProfileChanged;
        }

        /// <summary>Überträgt den Profilzustand auf den Spieler (Level, Attribute, HP, optional Position).</summary>
        public void ApplyToPlayer(bool usePosition)
        {
            if (Player == null) return;
            Player.EnsureInitialized();
            Player.Level = EffectiveLevel;
            ApplyExtra();
            Player.Stats.MarkDirty();
            Player.Revive(1f);
            if (usePosition && State.HasPosition)
            {
                var pc = Player.GetComponent<PlayerController>();
                if (pc != null) pc.Teleport(State.Position, Quaternion.Euler(0, State.RotationY, 0));
            }
            GameEventBus.RaiseProfileChanged();
        }

        void Update()
        {
            if (!_started || Player == null) return;
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= AutosaveInterval) { _autosaveTimer = 0f; Save("Autosave"); }
            _fogTimer -= Time.deltaTime;
            if (_fogTimer <= 0f) { _fogTimer = 0.5f; State.Fog.Reveal(Player.transform.position, FogRevealRadius); }
        }

        // ---------------- XP & Level ----------------
        public void GrantXp(int amount, string reason)
        {
            if (amount <= 0) return;
            amount = Mathf.RoundToInt(amount * WorldModel.XpFactor(State.Progression.Level, EffectiveLevel));
            if (State.Progression.Level >= AttributeSet.MaxLevel) { Mastery.AddXp(amount); GameEventBus.RaiseXp(amount, reason); return; }   // Lauf 4: Beherrschungs-EP ab Stufe 30
            State.Progression.AddXp(amount);
            GameEventBus.RaiseXp(amount, reason);
        }

        void OnLevelUp(int level)
        {
            if (Player != null)
            {
                Player.Level = EffectiveLevel;
                Player.Stats.MarkDirty();
                if (!Player.IsDead && !Player.IsDowned) Player.SetHealth(Player.MaxHealth);
                GameEventBus.RaiseLevelUp(Player, level);
            }
            GameEventBus.RaiseBanner("Stufe " + level, "+" + ProgressionModel.HeroPointsPerLevel + " Heldenpunkte · Attribute gestiegen", new Color(1f, 0.85f, 0.35f));
            RequestSave("Stufenaufstieg");
        }

        void OnKilled(Combatant victim, Combatant killer)
        {
            if (Player == null || victim == null || victim.Team != Team.Enemy) return;
            if (killer != Player && !victim.Attackers.Contains(Player)) return;
            GrantXp(ProgressionModel.KillXp(State.Progression.Level, victim.Level, victim.Tier), "Kill");
        }

        public void GrantReward(int karma, long copper, int xp, string reason = "Belohnung")
        {
            if (karma > 0) State.Wallet.AddKarma(karma);
            if (copper > 0) State.Wallet.AddCopper(copper);
            if (xp > 0) GrantXp(xp, reason);
        }

        // ---------------- Speichern / Laden ----------------
        bool _saveRequested;
        public void RequestSave(string reason) { _saveRequested = true; _saveReason = reason; }
        string _saveReason;

        void LateUpdate()
        {
            if (_saveRequested) { _saveRequested = false; Save(_saveReason); }
        }

        public void CapturePosition()
        {
            if (Player == null) return;
            State.HasPosition = true;
            State.Position = Player.transform.position;
            State.RotationY = Player.transform.eulerAngles.y;
        }

        /// <summary>Lauf 5 / M25: true, solange per Debug eine fremde Klasse getestet wird – der echte Charakter wird dann nicht überschrieben.</summary>
        public bool SaveSuspended { get; set; }

        public void Save(string reason = "Manuell")
        {
            if (SaveSuspended) { Debug.Log("[ProjectTyrant] Speichern ausgesetzt (Debug-Klassentest): " + reason); return; }
            if (Player != null && !Player.IsDead) CapturePosition();
            try
            {
                var dto = State.ToDto();
                if (Account != null && ActiveSlot >= 0) { SaveSystem.Write(dto, AccountStore.CharacterPath(ActiveSlot)); SaveAccount(dto); }
                else SaveSystem.Write(dto);
                LastSaveTime = Time.unscaledTime;
                LastSaveInfo = reason + " · " + System.DateTime.Now.ToString("HH:mm:ss");
            }
            catch (System.Exception e) { Debug.LogWarning("[ProjectTyrant] Speichern fehlgeschlagen: " + e.Message); }
        }

        public bool Load()
        {
            EnsureAccount();
            var dto = SaveSystem.Read(AccountStore.CharacterPath(ActiveSlot));
            if (dto == null) return false;
            AccountModel.ApplyAccount(Account, dto);
            UnhookState();
            State = new ProfileState();
            HookState();
            ApplyClassData(string.IsNullOrEmpty(dto.ClassId) ? "waechter" : dto.ClassId);   // Pool/Linien der Klasse vor dem Einlesen
            State.LoadDto(dto, Items, AllSkills);
            ApplyClassData(State.ClassId);
            InitRuntimeExtra();
            ApplyToPlayer(true);
            GameEventBus.Notify("Spielstand geladen", new Color(0.7f, 0.9f, 1f));
            return true;
        }

        /// <summary>Neuer Charakter (aus der Charaktererstellung): Klasse/Name/Farben aus dem Platz, Account-Fortschritt bleibt.</summary>
        void StartFreshCharacter()
        {
            var slot = ActiveCharacter;
            UnhookState();
            State = new ProfileState();
            HookState();
            if (slot != null) { State.ClassId = slot.ClassId; State.CharacterName = slot.Name; State.PrimaryHex = slot.PrimaryHex; State.SecondaryHex = slot.SecondaryHex; }
            if (Account != null) { var d = State.ToDto(); AccountModel.ApplyAccount(Account, d); State.LoadDto(d, Items, AllSkills); if (slot != null) { State.ClassId = slot.ClassId; State.CharacterName = slot.Name; State.PrimaryHex = slot.PrimaryHex; State.SecondaryHex = slot.SecondaryHex; } }
            ApplyClassData(State.ClassId);
            InitRuntimeExtra();
            ApplyToPlayer(false);
            Save("Neuer Charakter");
        }

        public void NewGame()
        {
            SaveSystem.Delete();
            UnhookState();
            State = new ProfileState();
            HookState();
            InitRuntimeExtra();
            ApplyToPlayer(false);
            if (GameManager.Instance != null && GameManager.Instance.Waypoint != null && Player != null)
            {
                var pc = Player.GetComponent<PlayerController>();
                if (pc != null) pc.Teleport(GameManager.Instance.Waypoint.position + Vector3.up * 0.2f, Quaternion.identity);
            }
            GameEventBus.Notify("Neues Spiel gestartet", new Color(0.7f, 0.9f, 1f));
        }
    }

}
