using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Erzeugt einfache prozedurale Sounds als WAV (kein externer Generator nötig).</summary>
    public static class AudioSynth
    {
        const int Rate = 44100;
        const string Dir = PTEditorUtil.ProcDir + "/Audio";

        public static AudioClip Make(string name, float duration, Func<float, System.Random, float> f)
        {
            PTEditorUtil.EnsureFolder(Dir);
            string path = Dir + "/" + name + ".wav";
            int n = Mathf.RoundToInt(duration * Rate);
            var rnd = new System.Random(name.GetHashCode());
            var data = new short[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = Mathf.Clamp(f(t, rnd), -1f, 1f);
                data[i] = (short)(v * 30000f);
            }
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
                foreach (var s in data) w.Write(s);
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Exp(-(t - a) / d);
        static float Noise(System.Random r) => (float)r.NextDouble() * 2f - 1f;

        public static void BuildAll(VfxLibrary lib)
        {
            lib.HitSound = Make("SFX_Hit", 0.18f, (t, r) => (Noise(r) * 0.6f + Mathf.Sin(t * 2 * Mathf.PI * 140f) * 0.5f) * Env(t, 0.003f, 0.05f));
            lib.CastSound = Make("SFX_Cast", 0.35f, (t, r) => Mathf.Sin(t * 2 * Mathf.PI * (500f + t * 900f)) * 0.35f * Env(t, 0.02f, 0.12f));
            lib.ComboSound = Make("SFX_Combo", 0.7f, (t, r) => (Mathf.Sin(t * 2 * Mathf.PI * 660f) + Mathf.Sin(t * 2 * Mathf.PI * 990f) * 0.6f + Mathf.Sin(t * 2 * Mathf.PI * 1320f) * 0.3f) * 0.3f * Env(t, 0.01f, 0.25f));
            lib.DodgeSound = Make("SFX_Dodge", 0.3f, (t, r) => Noise(r) * 0.35f * Env(t, 0.05f, 0.08f) * (0.5f + 0.5f * Mathf.Sin(t * 40f)));
            lib.EventSound = Make("SFX_Event", 1.2f, (t, r) =>
            {
                float f = t < 0.3f ? 392f : t < 0.6f ? 523f : 659f;
                return (Mathf.Sin(t * 2 * Mathf.PI * f) * 0.4f + Mathf.Sin(t * 2 * Mathf.PI * f * 2f) * 0.1f) * Env(t % 0.3f + (t > 0.6f ? 0 : 0), 0.01f, 0.4f) * (t > 0.6f ? Mathf.Exp(-(t - 0.6f) * 3f) : 1f);
            });
            // Lauf 3: neue Aktionen
            lib.LootSound = Make("SFX_Loot", 0.45f, (t, r) => (Mathf.Sin(t * 2 * Mathf.PI * (t < 0.12f ? 1320f : 1760f)) * 0.3f + Mathf.Sin(t * 2 * Mathf.PI * 2640f) * 0.08f) * Env(t % 0.12f, 0.003f, 0.08f) * Mathf.Exp(-t * 4f));
            lib.LevelUpSound = Make("SFX_LevelUp", 1.4f, (t, r) =>
            {
                float f = t < 0.2f ? 523f : t < 0.4f ? 659f : t < 0.6f ? 784f : 1047f;
                return (Mathf.Sin(t * 2 * Mathf.PI * f) * 0.35f + Mathf.Sin(t * 2 * Mathf.PI * f * 1.5f) * 0.1f) * Env(t % 0.2f, 0.01f, 0.3f) * (t > 0.6f ? Mathf.Exp(-(t - 0.6f) * 2.5f) : 1f);
            });
            lib.DiscoverSound = Make("SFX_Discover", 0.9f, (t, r) => (Mathf.Sin(t * 2 * Mathf.PI * 880f) * 0.25f + Mathf.Sin(t * 2 * Mathf.PI * 1320f) * 0.2f + Mathf.Sin(t * 2 * Mathf.PI * 1760f) * 0.1f) * Env(t, 0.02f, 0.5f));
            lib.EquipSound = Make("SFX_Equip", 0.22f, (t, r) => (Noise(r) * 0.4f + Mathf.Sin(t * 2 * Mathf.PI * 2200f) * 0.3f) * Env(t, 0.001f, 0.04f));
            lib.CoinSound = Make("SFX_Coin", 0.35f, (t, r) => (Mathf.Sin(t * 2 * Mathf.PI * 2000f) * 0.25f + Mathf.Sin(t * 2 * Mathf.PI * 3000f) * 0.15f) * Env(t % 0.09f, 0.002f, 0.05f) * Mathf.Exp(-t * 6f));
            lib.FailSound = Make("SFX_Fail", 1.0f, (t, r) => Mathf.Sin(t * 2 * Mathf.PI * (330f - t * 140f)) * 0.35f * Env(t, 0.02f, 0.5f));
            EditorUtility.SetDirty(lib);
        }
    }
}
