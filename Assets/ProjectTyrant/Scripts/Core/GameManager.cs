using System.Collections;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Spielzustand: Spieler-Referenz, Wegmarke/Respawn, Belohnungs-Konto.</summary>
    [DefaultExecutionOrder(-150)]
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Combatant Player;
        public Transform Waypoint;
        public float RespawnDelay = 4f;
        public VfxLibrary Vfx;
        AudioPool _audio;
        /// <summary>Lauf 5 / M24: Audio-Pool am selben Objekt (kein eigenes Singleton).</summary>
        public AudioPool Audio { get { if (_audio == null) _audio = GetComponent<AudioPool>(); return _audio; } }

        public EffectDefinition[] Effects;

        public long Karma => PlayerProfile.Instance != null ? PlayerProfile.Instance.Wallet.Karma : 0;
        public long Coins => PlayerProfile.Instance != null ? PlayerProfile.Instance.Wallet.Copper : 0;
        public int Experience => PlayerProfile.Instance != null ? PlayerProfile.Instance.Progression.Xp : 0;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Application.targetFrameRate = 120;
            EffectLibrary.Register(Effects);
        }

        void OnEnable() => GameEventBus.PlayerDefeated += OnPlayerDefeated;
        void OnDisable() => GameEventBus.PlayerDefeated -= OnPlayerDefeated;
        void OnDestroy() { if (Instance == this) { Instance = null; MapProjection.SetZoneSize(Procedural.ZoneLayout.Size); } }   // Lauf 4: Zonengröße nach Spielende zurücksetzen

        /// <summary>Leitet Belohnungen an das Spielerprofil weiter (Kupfer, Karma, XP).</summary>
        public void GrantReward(int karma, int coins, int xp)
        {
            if (PlayerProfile.Instance != null) PlayerProfile.Instance.GrantReward(karma, coins, xp);
        }

        void OnPlayerDefeated(Combatant c)
        {
            if (c == Player) StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            var ui = UIController.Instance;
            if (ui != null && ui.WorldMap != null && PlayerProfile.Instance != null)
            {
                // GW2: Besiegt → Weltkarte „Zur Wegmarke zurückkehren“; der Spieler wählt eine freie Wegmarke.
                GameEventBus.Notify("Besiegt! Wähle eine Wegmarke …", new Color(1f, 0.5f, 0.4f));
                yield return new WaitForSeconds(1.5f);
                if (Player != null && Player.IsDead) ui.ShowDefeatMap();
                yield break;
            }
            GameEventBus.Notify("Besiegt! Wiederbelebung an der Wegmarke …", new Color(1f, 0.5f, 0.4f));
            yield return new WaitForSeconds(RespawnDelay);
            if (Player == null) yield break;
            var pc = Player.GetComponent<PlayerController>();
            Vector3 pos = Waypoint != null ? Waypoint.position : Vector3.zero;
            if (pc != null) pc.Teleport(pos + Vector3.up * 0.2f, Waypoint != null ? Waypoint.rotation : Quaternion.identity);
            var downed = Player.GetComponent<DownedController>();
            if (downed != null) downed.ResetState();
            Player.Revive(1f);
            GameEventBus.RaisePlayerRespawned(Player);
        }
    }
}
