using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: Musik-Direktor am GameManager-Objekt. Zustände über <see cref="MusicStateModel"/> (Menü, Erkundung je Zone, Kampf ab 3 s,
    /// zurück 8 s nach Kampfende, Boss, Tod); zwei Stem-Sätze (je 5 Stems) blenden über 2 s über; Stems nach Intensität ein-/ausgeblendet;
    /// Stinger im Tonart-Kontext der laufenden Zone mit Musik-Ducking (−4 dB). Setzt die Mixer-Snapshots.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public const float SetFade = 2f;
        /// <summary>Kanäle: 2 Stem-Sätze à 5 + Stinger.</summary>
        public const int Channels = 11;
        public readonly MusicStateModel Model = new MusicStateModel();
        /// <summary>Esc-Menü / Charakterauswahl aktiv (Zustand Menü).</summary>
        public bool MenuActive;
        public string CurrentKey { get; private set; }
        public float Intensity { get; private set; }
        public string LastStinger { get; private set; }
        public readonly List<string> StateLog = new List<string>();

        AudioPool _pool;
        class StemSet { public AudioPool.Channel[] Ch = new AudioPool.Channel[5]; public float Fade; public string Key; }
        StemSet _a, _b; AudioPool.Channel _stinger;
        float _scanTimer, _lastDamage = -99f; bool _bossNear; int _enemiesOnPlayer;
        readonly List<NpcBrain> _brains = new List<NpcBrain>();
        MusicState _lastState = (MusicState)(-1);

        void Start()
        {
            _pool = GetComponent<AudioPool>();
            if (_pool == null) { enabled = false; return; }
            _a = MakeSet("A"); _b = MakeSet("B");
            _stinger = _pool.CreateChannel("Stinger", MixGroup.Music);
        }

        StemSet MakeSet(string n)
        {
            var s = new StemSet();
            for (int i = 0; i < 5; i++) { s.Ch[i] = _pool.CreateChannel("Music" + n + (Stem)i, MixGroup.Music); s.Ch[i].Source.loop = true; s.Ch[i].Source.priority = 0; }
            return s;
        }

        void OnEnable()
        {
            GameEventBus.DamageDealt += OnDamage; GameEventBus.Banner += OnBanner; GameEventBus.Notification += OnNotify;
            GameEventBus.PlayerDefeated += OnDefeated;
        }
        void OnDisable()
        {
            GameEventBus.DamageDealt -= OnDamage; GameEventBus.Banner -= OnBanner; GameEventBus.Notification -= OnNotify;
            GameEventBus.PlayerDefeated -= OnDefeated;
        }

        Combatant Player => GameManager.Instance != null ? GameManager.Instance.Player : null;

        void OnDamage(DamageInfo d)
        {
            var p = Player; if (p == null) return;
            if ((d.Source == p && d.Target != null && d.Target.Team == Team.Enemy) || (d.Target == p && d.Source != null && d.Source.Team == Team.Enemy)) _lastDamage = Time.time;
        }

        void OnBanner(string title, string sub, Color c)
        {
            var cue = GameSoundMap.ForBanner(title); if (cue == null) return;
            var p = Player; GameAudio.Play(cue.Value.Sfx, p != null ? p.transform.position : transform.position);
            if (cue.Value.Stinger.HasValue) PlayStinger(cue.Value.Stinger.Value);
        }

        void OnNotify(string msg, Color c)
        {
            var cue = GameSoundMap.ForNotification(msg); if (cue == null) return;
            var p = Player; GameAudio.Play(cue.Value.Sfx, p != null ? p.transform.position : transform.position);
            if (cue.Value.Stinger.HasValue) PlayStinger(cue.Value.Stinger.Value);
        }

        void OnDefeated(Combatant c) { if (c == Player) PlayStinger(StingerKind.Death); }

        string ZoneKey => GameManager.Instance != null && GameManager.Instance.CurrentZone != null ? GameManager.Instance.CurrentZone.Id : "sonnfurt";

        public void PlayStinger(StingerKind k)
        {
            if (_pool == null || _pool.Bank == null) return;
            var e = _pool.Bank.GetStinger(ZoneKey + "/" + k) ?? _pool.Bank.GetStinger("sonnfurt/" + k);
            if (e == null || e.Clip == null) return;
            _stinger.Source.Stop(); _stinger.Source.clip = e.Clip; _stinger.Volume = 1f; _stinger.ClipRmsLin = Synth.DbToLin(e.Stats.RmsDb); _stinger.Source.Play();
            _pool.Mixer.DuckForStinger(e.Clip.length);
            LastStinger = e.Key;
        }

        void Update()
        {
            if (_pool == null) return;
            var p = Player; float dt = Time.unscaledDeltaTime;
            _scanTimer -= dt;
            if (_scanTimer <= 0f && p != null)
            {
                _scanTimer = 0.5f; _enemiesOnPlayer = 0; _bossNear = false; _brains.Clear();
                _brains.AddRange(Object.FindObjectsByType<NpcBrain>());
                foreach (var b in _brains)
                {
                    if (b == null || b.Self == null || b.Self.IsDead || b.Self.Team != Team.Enemy) continue;
                    if (b.Target != p || (b.State != NpcState.Aggro && b.State != NpcState.Attack)) continue;
                    if ((b.transform.position - p.transform.position).sqrMagnitude > 45f * 45f) continue;
                    _enemiesOnPlayer++;
                    if (b.Self.Tier == EnemyTier.Champion) _bossNear = true;
                }
            }
            bool engaged = p != null && !p.IsDead && (_enemiesOnPlayer > 0 || Time.time - _lastDamage < 3f);
            bool dead = p != null && p.IsDead;
            var state = Model.Update(dt, engaged, engaged && _bossNear, MenuActive, dead);
            Intensity = Mathf.MoveTowards(Intensity, state == MusicState.Combat || state == MusicState.Boss ? Mathf.Clamp01(0.5f + 0.15f * _enemiesOnPlayer) : 0.3f, dt * 0.25f);

            // Snapshot
            bool downed = p != null && p.IsDowned;
            string snap = state == MusicState.Menu ? MixerModel.Menue : downed || dead ? MixerModel.Downed : state == MusicState.Boss ? MixerModel.Boss : state == MusicState.Combat ? MixerModel.Kampf : MixerModel.Erkundung;
            _pool.Mixer.TransitionTo(snap, snap == MixerModel.Menue ? 0.4f : 1.2f);
            _pool.Mixer.MenuOpen = UIController.Instance != null && UIController.Instance.Windows != null && UIController.Instance.Windows.AnyOpen;

            // Stem-Satz wählen
            string key = state == MusicState.Menu ? "menu/Menu" : state == MusicState.Boss ? "boss/Boss" : state == MusicState.Combat ? ZoneKey + "/Combat" : ZoneKey + "/Explore";
            if (key != CurrentKey) Switch(key);
            if (state != _lastState) { StateLog.Add(Time.time.ToString("0.0") + " s → " + state + " (" + key + ")"); if (StateLog.Count > 100) StateLog.RemoveAt(0); _lastState = state; }

            // Überblendung und Stem-Intensität
            _a.Fade = Mathf.MoveTowards(_a.Fade, _a.Key == CurrentKey ? 1f : 0f, dt / SetFade);
            _b.Fade = Mathf.MoveTowards(_b.Fade, _b.Key == CurrentKey ? 1f : 0f, dt / SetFade);
            ApplySet(_a, state); ApplySet(_b, state);
        }

        void Switch(string key)
        {
            var set = _pool.Bank != null ? _pool.Bank.GetMusic(key) : null;
            if (set == null && key.EndsWith("/Combat")) set = _pool.Bank != null ? _pool.Bank.GetMusic("sonnfurt/Combat") : null;
            if (set == null) { CurrentKey = key; return; }
            var target = _a.Fade <= _b.Fade ? _a : _b;   // leiseren Satz übernehmen
            target.Key = key; target.Fade = 0f;
            for (int i = 0; i < 5; i++)
            {
                var ch = target.Ch[i]; ch.Source.Stop(); ch.Source.clip = set.Stems[i];
                ch.ClipRmsLin = set.Stats != null && set.Stats.Length > i ? Synth.DbToLin(set.Stats[i].RmsDb) : 0.1f;
                if (ch.Source.clip != null) ch.Source.Play();
            }
            CurrentKey = key;
        }

        /// <summary>Stem-Pegel je Zustand und Intensität: Perkussion und Akzente folgen der Intensität, im Tod nur Pad.</summary>
        public static float StemLevel(Stem s, MusicState state, float intensity)
        {
            switch (state)
            {
                case MusicState.Death: return s == Stem.Pad ? 0.6f : 0f;
                case MusicState.Menu: return s == Stem.Percussion ? 0f : 0.9f;
                case MusicState.Combat: case MusicState.Boss: return s == Stem.Percussion ? 0.6f + 0.4f * intensity : s == Stem.Accents ? 0.4f + 0.6f * intensity : 1f;
                default: return s == Stem.Percussion ? 0.35f + 0.4f * intensity : s == Stem.Accents ? 0.7f : s == Stem.Melody ? 0.85f : 1f;
            }
        }

        void ApplySet(StemSet s, MusicState state)
        {
            for (int i = 0; i < 5; i++)
            {
                var ch = s.Ch[i];
                ch.Volume = s.Fade * StemLevel((Stem)i, state, Intensity);
                if (s.Fade <= 0f && ch.Source.isPlaying && s.Key != CurrentKey) ch.Source.Stop();
            }
        }

        public bool IsPlaying(string key) => CurrentKey == key && (_a.Key == key ? _a : _b).Ch[0].Playing;
    }
}
