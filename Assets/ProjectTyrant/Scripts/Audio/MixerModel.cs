using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Mixer-Snapshot: Pegel-Versatz (dB) je Gruppe und Tiefpass-Grenzfrequenz (Hz) je Gruppe.</summary>
    public class MixSnapshot
    {
        public string Name;
        public readonly float[] OffsetDb = new float[Enum.GetValues(typeof(MixGroup)).Length];
        public readonly float[] CutoffHz = new float[Enum.GetValues(typeof(MixGroup)).Length];
        public MixSnapshot(string name) { Name = name; for (int i = 0; i < CutoffHz.Length; i++) CutoffHz[i] = MixerModel.Open; }
        public MixSnapshot Set(MixGroup g, float db, float cutoff = MixerModel.Open) { OffsetDb[(int)g] = db; CutoffHz[(int)g] = cutoff; return this; }
    }

    /// <summary>
    /// Lauf 5 / M24: Mixer als reines Modell (Gruppen-Hierarchie, Snapshots mit Überblendung, Ducking, Spieler-Lautstärken).
    /// Die Laufzeit (<see cref="AudioPool"/>) setzt daraus Lautstärke und Tiefpass jeder Quelle. Kein AudioMixer-Asset:
    /// Gruppen/Snapshots lassen sich in Unity nicht über öffentliche API erzeugen (DECISIONS M24).
    /// </summary>
    public class MixerModel
    {
        public const float Open = 22000f;
        public const string Erkundung = "Erkundung", Kampf = "Kampf", Boss = "Boss", Menue = "Menü", Downed = "Downed", Unterwasser = "Unterwasser";
        static readonly int Count = Enum.GetValues(typeof(MixGroup)).Length;

        public static MixGroup? Parent(MixGroup g)
        {
            switch (g)
            {
                case MixGroup.Master: return null;
                case MixGroup.Combat: case MixGroup.Movement: case MixGroup.World: return MixGroup.Effects;
                default: return MixGroup.Master;
            }
        }

        /// <summary>Grundpegel je Gruppe (dB): UI −10, damit UI im Mix ≥ 18 dB unter der Musik liegt (Clip-RMS UI −20 + Gruppe −10 = −30 gegenüber Musik −12).</summary>
        public static float BaseDb(MixGroup g) => g == MixGroup.UI ? -10f : 0f;

        public readonly Dictionary<string, MixSnapshot> Snapshots = new Dictionary<string, MixSnapshot>();
        readonly float[] _offset = new float[Count], _cutoff = new float[Count], _fromOffset = new float[Count], _fromCutoff = new float[Count];
        readonly float[] _user = new float[Count];
        float _blend = 1f, _blendTime = 0.01f;
        float _stingerDuck, _menuDuck;
        public MixSnapshot Target { get; private set; }

        public MixerModel()
        {
            Add(new MixSnapshot(Erkundung));
            Add(new MixSnapshot(Kampf).Set(MixGroup.Music, 1.5f).Set(MixGroup.Ambience, -4f));
            Add(new MixSnapshot(Boss).Set(MixGroup.Music, 3f).Set(MixGroup.Ambience, -8f).Set(MixGroup.World, -2f));
            Add(new MixSnapshot(Menue).Set(MixGroup.Music, -6f).Set(MixGroup.Ambience, -6f, 900f).Set(MixGroup.Combat, -6f, 1200f).Set(MixGroup.Movement, -6f, 1200f).Set(MixGroup.World, -4f, 900f).Set(MixGroup.Voice, -6f, 1200f));
            Add(new MixSnapshot(Downed).Set(MixGroup.Music, -8f, 1800f).Set(MixGroup.Ambience, -6f, 600f).Set(MixGroup.Combat, -3f, 800f).Set(MixGroup.Movement, -6f, 600f).Set(MixGroup.World, -6f, 600f).Set(MixGroup.Voice, -4f, 800f));
            Add(new MixSnapshot(Unterwasser).Set(MixGroup.Music, -4f, 1500f).Set(MixGroup.Ambience, -2f, 400f).Set(MixGroup.Combat, -2f, 500f).Set(MixGroup.Movement, 0f, 400f).Set(MixGroup.World, -2f, 400f).Set(MixGroup.Voice, -2f, 500f));
            for (int i = 0; i < Count; i++) { _user[i] = 1f; _cutoff[i] = _fromCutoff[i] = Open; }
            Target = Snapshots[Erkundung];
        }

        void Add(MixSnapshot s) => Snapshots[s.Name] = s;

        /// <summary>Snapshot überblenden (Sekunden).</summary>
        public void TransitionTo(string name, float seconds)
        {
            if (!Snapshots.TryGetValue(name, out var s) || s == Target) return;
            for (int i = 0; i < Count; i++) { _fromOffset[i] = _offset[i]; _fromCutoff[i] = _cutoff[i]; }
            Target = s; _blend = 0f; _blendTime = Math.Max(0.01f, seconds);
        }

        /// <summary>Ducking: Musik −4 dB während eines Stingers; Welt-Effekte −6 dB solange ein Menü/Fenster offen ist.</summary>
        public void DuckForStinger(float seconds) => _stingerDuck = Math.Max(_stingerDuck, seconds);
        public bool MenuOpen { get; set; }
        public const float StingerDuckDb = -4f, MenuDuckDb = -6f;
        /// <summary>Lauf 6: Sprache über Musik während Szenen (Ducking).</summary>
        public bool Dialogue; public const float DialogueMusicDb = -9f, DialogueAmbienceDb = -6f;

        public void Update(float dt)
        {
            _blend = Math.Min(1f, _blend + dt / _blendTime);
            float k = _blend * _blend * (3f - 2f * _blend);
            for (int i = 0; i < Count; i++)
            {
                _offset[i] = _fromOffset[i] + (Target.OffsetDb[i] - _fromOffset[i]) * k;
                // Grenzfrequenz logarithmisch überblenden
                double a = Math.Log(_fromCutoff[i]), b = Math.Log(Target.CutoffHz[i]);
                _cutoff[i] = (float)Math.Exp(a + (b - a) * k);
            }
            _stingerDuck = Math.Max(0f, _stingerDuck - dt);
            float m = MenuOpen ? 1f : 0f;
            _menuDuck += (m - _menuDuck) * Math.Min(1f, dt * 6f);
        }

        /// <summary>Spieler-Lautstärke 0..1 (Optionen: Master, Musik, Umgebung, Effekte, UI); wahrgenommene Kurve v².</summary>
        public void SetUserVolume(MixGroup g, float v) => _user[(int)g] = Math.Max(0f, Math.Min(1f, v));
        public float UserVolume(MixGroup g) => _user[(int)g];

        /// <summary>Gesamtpegel einer Gruppe in dB (eigene + Eltern-Ketten: Grundpegel, Snapshot, Ducking, Spieler-Lautstärke).</summary>
        public float GainDb(MixGroup g)
        {
            float db = 0f; MixGroup? cur = g;
            while (cur.HasValue)
            {
                int i = (int)cur.Value;
                db += BaseDb(cur.Value) + _offset[i];
                float u = _user[i]; db += u <= 0.0001f ? -80f : Synth.LinToDb(u * u);
                cur = Parent(cur.Value);
            }
            if (g == MixGroup.Music) db += _stingerDuck > 0f ? StingerDuckDb : 0f;
            if (Dialogue) { if (g == MixGroup.Music) db += DialogueMusicDb; else if (g == MixGroup.Ambience) db += DialogueAmbienceDb; else if (g == MixGroup.Combat || g == MixGroup.World) db -= 4f; }
            if (g == MixGroup.Combat || g == MixGroup.Movement || g == MixGroup.World) db += MenuDuckDb * _menuDuck;
            return Math.Max(-80f, db);
        }

        public float Gain(MixGroup g) => GainDb(g) <= -79.9f ? 0f : Synth.DbToLin(GainDb(g));

        /// <summary>Tiefpass einer Gruppe (Minimum über die Kette).</summary>
        public float Cutoff(MixGroup g)
        {
            float c = Open; MixGroup? cur = g;
            while (cur.HasValue) { c = Math.Min(c, _cutoff[(int)cur.Value]); cur = Parent(cur.Value); }
            return c;
        }

        public bool StingerDucking => _stingerDuck > 0f;
    }
}
