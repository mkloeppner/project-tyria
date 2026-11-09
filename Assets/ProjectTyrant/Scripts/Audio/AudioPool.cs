using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24: Audio-Pool am GameManager-Objekt (kein eigenes Singleton). 32 Stimmen gesamt: 15 Effekt-Stimmen mit Prioritäten,
    /// 2 UI-Stimmen und benannte Kanäle für Musik-Stems, Stinger und Umgebungs-Betten. Wendet das <see cref="MixerModel"/> (Gruppen-Pegel,
    /// Snapshots, Ducking, Spieler-Lautstärken, Tiefpass) jedes Bild auf alle Quellen an und führt ein Pegel-Log je Gruppe.
    /// </summary>
    public class AudioPool : MonoBehaviour
    {
        public const int MaxVoices = 32, EffectVoices = 15, UiVoices = 2;
        public SoundBank Bank;
        public readonly MixerModel Mixer = new MixerModel();
        public readonly RoundRobin Variants = new RoundRobin(11);

        public class Channel
        {
            public AudioSource Source; public AudioLowPassFilter Lowpass; public MixGroup Group; public float Volume = 1f; public float ClipRmsLin = 0.25f;
            public bool Playing => Source != null && Source.isPlaying;
        }

        readonly List<Channel> _channels = new List<Channel>();
        Channel[] _fx, _ui; VoiceAllocator _alloc; int _uiNext;
        public int ActiveVoices { get; private set; }
        public int PeakVoices { get; private set; }
        public int Dropped { get; private set; }
        public int Played { get; private set; }
        public string LastPlayed { get; private set; }

        /// <summary>Pegel-Log (QA): alle 0,5 s die wirksamen Gruppenpegel in dBFS (Clip-RMS × Lautstärke × Mixer).</summary>
        public readonly List<string> LevelLog = new List<string>();
        readonly float[] _levels = new float[System.Enum.GetValues(typeof(MixGroup)).Length];
        float _logTimer;

        public static AudioPool Current => GameManager.Instance != null ? GameManager.Instance.Audio : null;

        void Awake()
        {
            _fx = new Channel[EffectVoices]; _alloc = new VoiceAllocator(EffectVoices);
            for (int i = 0; i < EffectVoices; i++) _fx[i] = MakeChannel("Fx" + i, MixGroup.Combat, true);
            _ui = new Channel[UiVoices];
            for (int i = 0; i < UiVoices; i++) _ui[i] = MakeChannel("Ui" + i, MixGroup.UI, false);
            LoadOptions();
        }

        void OnEnable() { SkillFx.SfxHandler = OnSkillSfx; UiFactory.UiSound = PlayUi; }
        void OnDisable()
        {
            if (SkillFx.SfxHandler == (System.Action<string, Vector3, float>)OnSkillSfx) SkillFx.SfxHandler = null;
            if (UiFactory.UiSound == (System.Action<string>)PlayUi) UiFactory.UiSound = null;
        }

        void OnSkillSfx(string id, Vector3 pos, float volumeDb) => Play(id, pos, volumeDb);

        Channel MakeChannel(string name, MixGroup g, bool spatial)
        {
            var go = new GameObject("Audio_" + name); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = spatial ? 1f : 0f;
            s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = 2.5f; s.maxDistance = 45f; s.dopplerLevel = 0f;
            var lp = go.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = MixerModel.Open; lp.enabled = false;
            var c = new Channel { Source = s, Lowpass = lp, Group = g };
            _channels.Add(c); return c;
        }

        /// <summary>Benannter Dauer-Kanal (Musik-Stem, Stinger, Umgebungs-Bett). Zählt zum Stimmen-Budget, wenn er spielt.</summary>
        public Channel CreateChannel(string name, MixGroup g, bool spatial = false) => MakeChannel(name, g, spatial);

        public SoundBank.Entry Entry(string id) => Bank != null ? Bank.Get(id) : null;

        /// <summary>Klang abspielen (3D an Position, Round-Robin-Variante, Priorität aus Kategorie). Liefert false, wenn keine Stimme frei war.</summary>
        public bool Play(string id, Vector3 pos, float volumeDb = 0f, float pitch = 1f, int priority = -1)
        {
            var e = Entry(id); if (e == null || e.Clips == null || e.Clips.Length == 0) return false;
            if (e.Group == MixGroup.UI) { PlayUi(id); return true; }
            int vi = Variants.Next(id, e.Clips.Length); var clip = e.Clips[vi]; if (clip == null) return false;
            var listener = ListenerPos();
            if (e.Spatial && (pos - listener).sqrMagnitude > 50f * 50f) return false;   // außer Hörweite: keine Stimme verbrauchen
            int prio = priority >= 0 ? priority : e.Priority;
            int v = _alloc.Request(prio, Time.unscaledTime, clip.length / Mathf.Max(0.1f, pitch));
            if (v < 0) { Dropped++; return false; }
            var ch = _fx[v];
            ch.Group = e.Group; ch.Volume = Synth.DbToLin(e.VolumeDb + volumeDb);
            ch.ClipRmsLin = e.Stats != null && e.Stats.Length > vi ? Synth.DbToLin(e.Stats[vi].RmsDb) : 0.25f;
            var s = ch.Source; s.Stop(); s.clip = clip; s.pitch = pitch; s.loop = false;
            s.spatialBlend = e.Spatial ? 1f : 0f; s.transform.position = e.Spatial ? pos : listener;
            Apply(ch); s.Play();
            Played++; LastPlayed = id;
            return true;
        }

        /// <summary>UI-Klang (2D, eigene 2 Stimmen, keine direkte Wiederholung derselben Variante).</summary>
        public void PlayUi(string id)
        {
            var e = Entry(id.StartsWith("UI/") ? id : "UI/" + id); if (e == null || e.Clips.Length == 0) return;
            int vi = Variants.Next(e.Id, e.Clips.Length);
            var ch = _ui[_uiNext]; _uiNext = (_uiNext + 1) % _ui.Length;
            ch.Group = MixGroup.UI; ch.Volume = Synth.DbToLin(e.VolumeDb); ch.ClipRmsLin = e.Stats.Length > vi ? Synth.DbToLin(e.Stats[vi].RmsDb) : 0.1f;
            ch.Source.Stop(); ch.Source.clip = e.Clips[vi]; ch.Source.pitch = 1f; Apply(ch); ch.Source.Play();
            Played++; LastPlayed = e.Id;
        }

        Vector3 ListenerPos()
        {
            var cam = Camera.main; return cam != null ? cam.transform.position : transform.position;
        }

        void Apply(Channel c)
        {
            c.Source.volume = Mathf.Clamp01(c.Volume * Mixer.Gain(c.Group));
            float cut = Mixer.Cutoff(c.Group);
            bool lp = cut < 20000f;
            if (c.Lowpass.enabled != lp) c.Lowpass.enabled = lp;
            if (lp) c.Lowpass.cutoffFrequency = cut;
        }

        void Update()
        {
            Mixer.Update(Time.unscaledDeltaTime);
            int active = 0;
            for (int i = 0; i < _levels.Length; i++) _levels[i] = 0f;
            foreach (var c in _channels)
            {
                if (!c.Playing) continue;
                active++; Apply(c);
                float lvl = c.Source.volume * c.ClipRmsLin; _levels[(int)c.Group] += lvl * lvl;
            }
            ActiveVoices = active; if (active > PeakVoices) PeakVoices = active;
            _logTimer -= Time.unscaledDeltaTime;
            if (_logTimer <= 0f)
            {
                _logTimer = 0.5f;
                var sb = new System.Text.StringBuilder(); sb.Append(Time.time.ToString("0.0")).Append(" s [").Append(Mixer.Target.Name).Append("] ");
                foreach (MixGroup g in new[] { MixGroup.Music, MixGroup.Ambience, MixGroup.Combat, MixGroup.Movement, MixGroup.World, MixGroup.UI, MixGroup.Voice })
                    sb.Append(g).Append(' ').Append(GroupLevelDb(g).ToString("0")).Append(" · ");
                sb.Append("Stimmen ").Append(active);
                LevelLog.Add(sb.ToString()); if (LevelLog.Count > 400) LevelLog.RemoveAt(0);
            }
        }

        /// <summary>Wirksamer Pegel einer Gruppe in dBFS (Summe der Quellen, −80 = still).</summary>
        public float GroupLevelDb(MixGroup g) { float p = _levels[(int)g]; return p <= 1e-10f ? -80f : 10f * Mathf.Log10(p); }

        // ------------------------------------------------------------ Optionen (Account-Ebene ab M25; bis dahin PlayerPrefs)
        public void LoadOptions()
        {
            var o = GameOptions.Load();
            Mixer.SetUserVolume(MixGroup.Master, o.Master); Mixer.SetUserVolume(MixGroup.Music, o.Music); Mixer.SetUserVolume(MixGroup.Ambience, o.Ambience);
            Mixer.SetUserVolume(MixGroup.Effects, o.Effects); Mixer.SetUserVolume(MixGroup.UI, o.Ui); Mixer.SetUserVolume(MixGroup.Voice, o.Effects);
        }
    }
}
