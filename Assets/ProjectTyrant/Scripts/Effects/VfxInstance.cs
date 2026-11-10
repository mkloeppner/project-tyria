using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M23: eine abgespielte VFX-Komposition (gepoolt). Steuert Dauer (≤ 3 s außer Feldern/Auren), Folgen, Dichte-/Team-Skalierung,
    /// Mesh-Animation (Skalierung, Dissolve), Blitz-Linien mit Jitter und ein geliehenes Punktlicht aus dem Licht-Pool.
    /// </summary>
    public class VfxInstance : MonoBehaviour
    {
        public string Key;
        public float Duration = 1f;
        public bool Loop;
        [Header("Licht (aus dem Pool, max. 8)")] public bool UseLight; public Color LightColor = Color.white; public float LightRange = 5f, LightIntensity = 2f;
        [Header("Mesh-Animation")] public Renderer[] MeshFx = new Renderer[0]; public Vector3 MeshScaleFrom = Vector3.one, MeshScaleTo = Vector3.one; public float DissolveFrom = 0.6f;
        [Header("Blitze")] public LineRenderer[] Bolts = new LineRenderer[0]; public float BoltLength = 2f;

        ParticleSystem[] _ps; float[] _rate; float[] _burst; Color[] _startCol; bool[] _hasCol;
        float _t; Transform _follow; Light _light; VfxPool _pool; MaterialPropertyBlock _mpb;
        Vector3[] _meshBase;
        public bool Active { get; private set; }
        public float Age => _t;
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

        void Awake()
        {
            _ps = GetComponentsInChildren<ParticleSystem>(true);
            _rate = new float[_ps.Length]; _burst = new float[_ps.Length]; _startCol = new Color[_ps.Length]; _hasCol = new bool[_ps.Length];
            for (int i = 0; i < _ps.Length; i++)
            {
                var sc = _ps[i].main.startColor; _hasCol[i] = sc.mode == ParticleSystemGradientMode.Color; _startCol[i] = sc.color;
                var em = _ps[i].emission; _rate[i] = em.rateOverTimeMultiplier;
                _burst[i] = em.burstCount > 0 ? em.GetBurst(0).count.constant : 0f;
            }
            _mpb = new MaterialPropertyBlock();
            _meshBase = new Vector3[MeshFx.Length];
            for (int i = 0; i < MeshFx.Length; i++) if (MeshFx[i] != null) _meshBase[i] = MeshFx[i].transform.localScale;
        }

        public int ParticleCount { get { int n = 0; if (_ps != null) foreach (var p in _ps) n += p.particleCount; return n; } }

        VfxAreaLayer[] _areaLayers;

        public void Play(VfxPool pool, Vector3 pos, Quaternion rot, Transform follow, float intensity, float density)
        {
            _pool = pool; _follow = follow; _t = 0f; Active = true;
            // Fix-Lauf F1: Flächenrahmen/Effekt je Besitzer und Option ein-/ausblenden
            if (_areaLayers == null) _areaLayers = GetComponentsInChildren<VfxAreaLayer>(true);
            if (_areaLayers.Length > 0)
            {
                var vis = AreaDisplay.Current(AreaDisplay.FromIntensity(intensity));
                foreach (var l in _areaLayers) if (l != null) l.gameObject.SetActive(l.Layer == AreaLayer.AreaOutline ? vis.Outline : vis.Effect);
                intensity *= vis.Alpha;
            }
            transform.SetPositionAndRotation(pos, rot);
            gameObject.SetActive(true);
            for (int i = 0; i < _ps.Length; i++)
            {
                var em = _ps[i].emission; em.rateOverTimeMultiplier = _rate[i] * density;
                if (em.burstCount > 0) { var b = em.GetBurst(0); b.count = Mathf.Max(1f, Mathf.Round(_burst[i] * density)); em.SetBurst(0, b); }
                if (_hasCol[i]) { var main = _ps[i].main; var col = _startCol[i]; col.a = Mathf.Clamp01(col.a * intensity); main.startColor = col; }   // Intensität: Alpha
                _ps[i].Clear(true); _ps[i].Play(true);
            }
            if (UseLight && pool != null) { _light = pool.BorrowLight(); if (_light != null) { _light.color = LightColor; _light.range = LightRange; _light.intensity = LightIntensity * intensity; _light.transform.position = pos + Vector3.up * 0.5f; } }
            UpdateMeshes(0f);
        }

        public void Stop()
        {
            if (!Active) return;
            foreach (var p in _ps) p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Loop = false; if (_t < Duration - 0.5f) _t = Mathf.Max(_t, Duration - 0.5f);
        }

        void Update()
        {
            if (!Active) return;
            _t += Time.deltaTime;
            if (_follow != null) { if (!_follow.gameObject.activeInHierarchy) _follow = null; else transform.position = _follow.position; }
            float k = Duration > 0f ? Mathf.Clamp01(_t / Duration) : 1f;
            UpdateMeshes(k);
            if (Bolts.Length > 0 && Time.frameCount % 3 == 0) foreach (var b in Bolts) Jitter(b);
            if (_light != null) _light.intensity = Mathf.Lerp(_light.intensity, 0f, k * k);
            if (Loop) { if (_t >= Duration) _t = Duration * 0.5f; return; }
            if (_t >= Duration && ParticleCount == 0) Release();
            else if (_t > Duration + 2f) Release();
        }

        void UpdateMeshes(float k)
        {
            for (int i = 0; i < MeshFx.Length; i++)
            {
                var r = MeshFx[i]; if (r == null) continue;
                r.transform.localScale = Vector3.Scale(_meshBase[i], Vector3.Lerp(MeshScaleFrom, MeshScaleTo, 1f - (1f - k) * (1f - k)));
                r.GetPropertyBlock(_mpb); _mpb.SetFloat(DissolveId, k < DissolveFrom ? 0f : (k - DissolveFrom) / Mathf.Max(0.01f, 1f - DissolveFrom)); r.SetPropertyBlock(_mpb);
            }
        }

        void Jitter(LineRenderer l)
        {
            int n = l.positionCount; if (n < 2) return;
            var a = Vector3.zero; var b = Vector3.forward * BoltLength;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                var p = Vector3.Lerp(a, b, u);
                if (i > 0 && i < n - 1) p += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * 0.18f * BoltLength * Mathf.Sin(u * Mathf.PI);
                l.SetPosition(i, p);
            }
        }

        void Release()
        {
            Active = false;
            if (_light != null && _pool != null) { _pool.ReturnLight(_light); _light = null; }
            foreach (var p in _ps) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_pool != null) _pool.Return(this); else gameObject.SetActive(false);
        }
    }
}
