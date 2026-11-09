using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M23: Katalog des VFX-Baukastens (vom VfxKitBuilder erzeugt): Paletten-Daten und alle Kompositions-Prefabs.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/VFX Catalog", fileName = "VfxCatalog")]
    public class VfxCatalog : ScriptableObject
    {
        [Serializable]
        public class PaletteDef
        {
            public VfxPalette Id;
            public Color A = Color.white, B = Color.white, Light = Color.white;
            [Tooltip("Partikelverhalten: Schwerkraft (+ fällt), Auftrieb, Turbulenz, Tempo-Faktor")] public float Gravity, Rise, Turbulence = 0.5f, Speed = 1f;
            [Tooltip("Alpha-Mischung statt additiv (Rauch, Schatten)")] public bool Alpha;
            [Tooltip("Textur-Set: Glow, Spark, Smoke, Leaf, Flake, Drop, Shard, Ember")] public string Particle = "Glow", Secondary = "Spark";
            public bool Lightning;
            public string SoundFamily;
        }

        [Serializable]
        public class Entry { public string Key; public GameObject Prefab; public int MaxParticles; public float Duration; public bool Loop; }

        public List<PaletteDef> Palettes = new List<PaletteDef>();
        public List<Entry> Entries = new List<Entry>();
        public static VfxCatalog Current { get; set; }
        [NonSerialized] Dictionary<string, Entry> _map;

        public Entry Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_map == null || _map.Count != Entries.Count) { _map = new Dictionary<string, Entry>(); foreach (var e in Entries) if (e != null && e.Prefab != null) _map[e.Key] = e; }
            return _map.TryGetValue(key, out var r) ? r : null;
        }

        public PaletteDef Palette(VfxPalette p) { foreach (var d in Palettes) if (d.Id == p) return d; return null; }

        /// <summary>Partikel-Budget je Größe (Summe maxParticles einer Komposition).</summary>
        public static int Budget(VfxSize s) => s == VfxSize.S ? 80 : s == VfxSize.M ? 160 : s == VfxSize.L ? 320 : 640;
    }
}
