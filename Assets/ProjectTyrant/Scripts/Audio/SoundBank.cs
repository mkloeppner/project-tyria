using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: alle erzeugten Klänge (Varianten, Kategorie, Analysewerte) und Musik (Stems je Zone/Zustand, Stinger).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Id; public SoundCategory Category; public MixGroup Group; public bool Loop, Spatial = true; public float VolumeDb; public int Priority;
            public AudioClip[] Clips = new AudioClip[0];
            public ClipStats[] Stats = new ClipStats[0];
        }

        [Serializable]
        public class MusicSet
        {
            public string Key;   // „sonnfurt/Explore“, „boss/Boss“, „menu/Menu“
            public float Bpm; public int Bars;
            public AudioClip[] Stems = new AudioClip[5];   // Pad, Melodie, Bass, Perkussion, Akzente
            public ClipStats[] Stats = new ClipStats[5];
        }

        [Serializable]
        public class StingerEntry { public string Key; public AudioClip Clip; public ClipStats Stats; }   // „sonnfurt/Victory“

        public List<Entry> Entries = new List<Entry>();
        public List<MusicSet> Music = new List<MusicSet>();
        public List<StingerEntry> Stingers = new List<StingerEntry>();

        Dictionary<string, Entry> _map; Dictionary<string, MusicSet> _music; Dictionary<string, StingerEntry> _stingers;

        public Entry Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null || _map.Count != Entries.Count) { _map = new Dictionary<string, Entry>(); foreach (var e in Entries) if (e != null && !string.IsNullOrEmpty(e.Id)) _map[e.Id] = e; }
            _map.TryGetValue(id, out var r); return r;
        }

        public MusicSet GetMusic(string key)
        {
            if (_music == null || _music.Count != Music.Count) { _music = new Dictionary<string, MusicSet>(); foreach (var m in Music) _music[m.Key] = m; }
            _music.TryGetValue(key, out var r); return r;
        }

        public StingerEntry GetStinger(string key)
        {
            if (_stingers == null || _stingers.Count != Stingers.Count) { _stingers = new Dictionary<string, StingerEntry>(); foreach (var s in Stingers) _stingers[s.Key] = s; }
            _stingers.TryGetValue(key, out var r); return r;
        }

        public void ClearCache() { _map = null; _music = null; _stingers = null; }
    }
}
