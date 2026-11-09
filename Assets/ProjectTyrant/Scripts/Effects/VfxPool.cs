using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M23: Pool des VFX-Baukastens (hängt am GameManager-Objekt, kein Singleton): Kompositionen nach Schlüssel, Licht-Pool (max. 8),
    /// globales Partikel-Budget (≤ 20.000) und Effekt-Dichte (Voll/Reduziert/Minimal). Registriert sich als Handler der Skill-VFX-Spur.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        public enum Density { Voll, Reduziert, Minimal }
        public const int MaxLights = 8, ParticleBudget = 20000;
        public VfxCatalog Catalog;
        public static VfxPool Instance { get; private set; }
        public static Density EffectDensity = Density.Voll;
        public static float DensityFactor => EffectDensity == Density.Voll ? 1f : EffectDensity == Density.Reduziert ? 0.5f : 0.2f;
        public int LiveParticles { get; private set; }
        public int PeakParticles { get; private set; }
        public int ActiveInstances => _active.Count;
        public int LightsInUse => MaxLights - _freeLights.Count;
        public int Spawned { get; private set; }
        public int Skipped { get; private set; }

        readonly Dictionary<string, Stack<VfxInstance>> _free = new Dictionary<string, Stack<VfxInstance>>();
        readonly List<VfxInstance> _active = new List<VfxInstance>();
        readonly Stack<Light> _freeLights = new Stack<Light>();
        float _sampleT;

        void Awake()
        {
            Instance = this;
            EffectDensity = (Density)PlayerPrefs.GetInt("PT_EffectDensity", 0);
            if (Catalog != null) VfxCatalog.Current = Catalog;
            for (int i = 0; i < MaxLights; i++)
            {
                var go = new GameObject("VfxLight" + i); go.transform.SetParent(transform, false);
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.shadows = LightShadows.None; l.enabled = false;
                _freeLights.Push(l);
            }
            SkillFx.VfxHandler = (id, pos, rot, follow) => Play(id, pos, rot, follow, SkillFx.CurrentIntensity);
        }

        void OnDestroy() { if (Instance == this) Instance = null; SkillFx.VfxHandler = null; }

        public VfxInstance Play(string key, Vector3 pos, Quaternion rot, Transform follow = null, float intensity = 1f)
        {
            if (Catalog == null) return null;
            var e = Catalog.Get(key);
            if (e == null) return null;
            // Budget: ab 90 % nur noch kleine Effekte, ab 100 % keine neuen
            if (LiveParticles + e.MaxParticles * DensityFactor > ParticleBudget) { Skipped++; return null; }
            if (!_free.TryGetValue(key, out var stack)) { stack = new Stack<VfxInstance>(); _free[key] = stack; }
            VfxInstance inst = null;
            while (stack.Count > 0 && inst == null) inst = stack.Pop();
            if (inst == null)
            {
                var go = Instantiate(e.Prefab, transform);
                inst = go.GetComponent<VfxInstance>(); if (inst == null) inst = go.AddComponent<VfxInstance>();
                inst.Key = key;
            }
            inst.Play(this, pos, rot, follow, intensity, DensityFactor);
            _active.Add(inst); Spawned++;
            LiveParticles += (int)(e.MaxParticles * DensityFactor * 0.5f);   // grobe Vorab-Schätzung bis zur nächsten Messung
            return inst;
        }

        public void Return(VfxInstance i)
        {
            _active.Remove(i);
            i.gameObject.SetActive(false);
            if (!_free.TryGetValue(i.Key, out var s)) { s = new Stack<VfxInstance>(); _free[i.Key] = s; }
            s.Push(i);
        }

        public Light BorrowLight() { if (_freeLights.Count == 0) return null; var l = _freeLights.Pop(); l.enabled = true; return l; }
        public void ReturnLight(Light l) { if (l == null) return; l.enabled = false; _freeLights.Push(l); }

        void Update()
        {
            _sampleT -= Time.unscaledDeltaTime;
            if (_sampleT > 0f) return;
            _sampleT = 0.25f;
            int n = 0; foreach (var a in _active) if (a != null) n += a.ParticleCount;
            n += AmbientParticles();
            LiveParticles = n; if (n > PeakParticles) PeakParticles = n;
        }

        public static System.Func<int> AmbientCounter;
        static int AmbientParticles() => AmbientCounter != null ? AmbientCounter() : 0;
    }
}
