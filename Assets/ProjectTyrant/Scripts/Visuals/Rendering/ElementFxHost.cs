using System.Collections.Generic;
using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// VFX-Lauf V2+: spielt die Element-Lagen (Sim-Flipbook, FxMesh, Brechung, Welt-Spur, GPU-Partikel) gepoolt ab und animiert sie.
    /// Gemeinsamer Wirt für alle Elemente; Inhalte kommen aus <see cref="ElementFxLibrary"/>. Höchstens 64 aktive Instanzen, die älteste wird recycelt.
    /// Ohne Verzerrungs-Feature (Niedrig) werden Brechungs-Lagen als halbtransparente Element-Meshes gezeigt – nichts fehlt, es wird nur einfacher.
    /// </summary>
    public class ElementFxHost : MonoBehaviour
    {
        class Inst
        {
            public GameObject Go; public MeshFilter Mf; public MeshRenderer Mr; public DistortionSource Ds; public GpuParticleSystem Gpu;
            public VfxLayerKind Kind; public string Content; public float Start, Duration, Radius, Strength; public Transform Follow; public Vector3 Pos; public Quaternion Rot;
            public bool Billboard, Active; public Color BaseTint = Color.white;
        }

        public const int MaxActive = 64;
        static ElementFxHost _host;
        readonly List<Inst> _all = new List<Inst>();
        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();
        MaterialPropertyBlock _mpb;
        static readonly int PAge = Shader.PropertyToID("_Age"), PStart = Shader.PropertyToID("_StartTime"), PTint = Shader.PropertyToID("_Tint"), PStrength = Shader.PropertyToID("_Strength");
        public static int Spawned { get; private set; }
        public static int ActiveCount { get { if (_host == null) return 0; int n = 0; foreach (var i in _host._all) if (i.Active) n++; return n; } }

        static ElementFxHost Host
        {
            get
            {
                if (_host != null) return _host;
                var go = new GameObject("PT_ElementFxHost"); go.hideFlags = HideFlags.DontSave;
                if (Application.isPlaying) DontDestroyOnLoad(go);
                _host = go.AddComponent<ElementFxHost>(); _host._mpb = new MaterialPropertyBlock();
                return _host;
            }
        }

        /// <summary>Einstieg aus VfxLayerPlayer für die Lagen-Arten Sim, FxMesh, Refract, WorldMark, Gpu.</summary>
        public static void Play(VfxLayer l, Vector3 pos, Quaternion rot, Transform follow, float intensity)
        {
            var lib = ElementFxLibrary.Current; if (lib == null || !Application.isPlaying) return;
            string el = ElementKits.ElementOf(l.Id), content = ElementKits.ContentOf(l.Id);
            var h = Host;
            switch (l.Kind)
            {
                case VfxLayerKind.Sim: h.SpawnSim(lib, el + "/" + content, content, pos, follow, l.Radius, l.Duration, intensity); break;
                case VfxLayerKind.FxMesh: h.SpawnMesh(lib.Mat(el, "Mesh"), VfxLayerKind.FxMesh, content, pos, rot, follow, l.Radius, l.Duration, intensity); break;
                case VfxLayerKind.Refract:
                    if (GraphicsSettingsRuntime.Feature("Verzerrung")) h.SpawnMesh(lib.Mat(el, "Refract") ?? lib.Mat("", "Refract"), VfxLayerKind.Refract, content, pos, rot, follow, l.Radius, l.Duration, l.Strength * intensity);
                    else h.SpawnMesh(lib.Mat(el, "Mesh"), VfxLayerKind.FxMesh, content, pos, rot, follow, l.Radius, l.Duration, 0.35f * intensity);
                    break;
                case VfxLayerKind.WorldMark: h.SpawnMark(lib, el, content, pos, l.Radius, l.Duration); break;
                case VfxLayerKind.Gpu: h.SpawnGpu(lib, el + "/" + content, pos, rot, l.Duration); break;
            }
        }

        Inst Take()
        {
            foreach (var i in _all) if (!i.Active && i.Gpu == null) return i;
            if (_all.Count >= MaxActive)
            {
                Inst old = null; foreach (var i in _all) if (i.Gpu == null && (old == null || i.Start < old.Start)) old = i;
                if (old != null) { Release(old); return old; }
            }
            var go = new GameObject("ElementFx"); go.transform.SetParent(transform, false);
            var inst = new Inst { Go = go, Mf = go.AddComponent<MeshFilter>(), Mr = go.AddComponent<MeshRenderer>() };
            inst.Mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; inst.Mr.receiveShadows = false;
            _all.Add(inst); return inst;
        }

        void Release(Inst i)
        {
            i.Active = false; if (i.Ds != null) i.Ds.enabled = false;
            if (i.Go.activeSelf) i.Go.SetActive(false);
        }

        static Mesh MeshFor(string shape)
        {
            if (_meshes.TryGetValue(shape, out var m) && m != null) return m;
            FxMeshData data;
            if (shape == "QuadUp") data = Quad(false);
            else if (shape == "QuadFlat") data = Quad(true);
            else if (System.Enum.TryParse(shape, out FxMeshKit.Shape s)) data = FxMeshKit.Build(s, 7);
            else data = Quad(false);
            m = data.ToMesh("Fx_" + shape); m.hideFlags = HideFlags.DontSave; _meshes[shape] = m; return m;
        }

        static FxMeshData Quad(bool flat)
        {
            var d = new FxMeshData(); var n = flat ? Vector3.up : Vector3.back;
            Vector3 P(float x, float y) => flat ? new Vector3(x, 0f, y) : new Vector3(x, y + 0.5f, 0f);
            int a = d.Add(P(-0.5f, -0.5f), n, new Vector2(0, 0), Color.white), b = d.Add(P(0.5f, -0.5f), n, new Vector2(1, 0), Color.white);
            int c = d.Add(P(0.5f, 0.5f), n, new Vector2(1, 1), Color.white), e = d.Add(P(-0.5f, 0.5f), n, new Vector2(0, 1), Color.white);
            if (flat) d.Quad(a, e, c, b); else d.Quad(a, b, c, e);
            return d;
        }

        void Activate(Inst i, VfxLayerKind kind, string content, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Transform follow, float radius, float duration, float strength)
        {
            i.Kind = kind; i.Content = content; i.Start = Time.time; i.Duration = Mathf.Max(0.1f, duration); i.Radius = Mathf.Max(0.1f, radius); i.Strength = strength;
            i.Follow = follow; i.Pos = pos; i.Rot = rot; i.Active = true;
            i.Mf.sharedMesh = mesh; i.Mr.sharedMaterial = mat;
            if (kind == VfxLayerKind.Refract) { if (i.Ds == null) i.Ds = i.Go.AddComponent<DistortionSource>(); i.Ds.enabled = true; }
            else if (i.Ds != null) i.Ds.enabled = false;
            i.Go.transform.SetPositionAndRotation(pos, rot);
            if (!i.Go.activeSelf) i.Go.SetActive(true);
            Spawned++;
            Animate(i, 0f);
        }

        void SpawnSim(ElementFxLibrary lib, string id, string content, Vector3 pos, Transform follow, float radius, float duration, float intensity)
        {
            var fb = lib.Flipbook(id); if (fb == null || fb.Material == null) return;
            bool flat = content.Contains("kaustik");
            var i = Take();
            if (flat) pos = Ground(pos, out _, out _) + Vector3.up * 0.04f;
            float life = fb.Loop ? duration : Mathf.Max(duration, 64f / Mathf.Max(24f, fb.Fps));
            Activate(i, VfxLayerKind.Sim, content, MeshFor(flat ? "QuadFlat" : "QuadUp"), fb.Material, pos, Quaternion.identity, follow, radius, life, intensity);
            i.Billboard = !flat; i.BaseTint = fb.Material.HasProperty(PTint) ? fb.Material.GetColor(PTint) : Color.white;
            _mpb.Clear(); _mpb.SetFloat(PStart, Time.timeSinceLevelLoad); _mpb.SetColor(PTint, new Color(i.BaseTint.r, i.BaseTint.g, i.BaseTint.b, i.BaseTint.a * Mathf.Clamp01(intensity)));
            i.Mr.SetPropertyBlock(_mpb);
        }

        void SpawnMesh(Material mat, VfxLayerKind kind, string shape, Vector3 pos, Quaternion rot, Transform follow, float radius, float duration, float strength)
        {
            if (mat == null) return;
            var i = Take(); i.Billboard = false;
            if (shape == "WaveRing" || shape == "ShockDisc" || shape == "WaterColumn" || shape == "SplashCrown") { pos = Ground(pos, out _, out _) + Vector3.up * 0.05f; rot = Quaternion.identity; }
            Activate(i, kind, shape, MeshFor(shape), mat, pos, rot, follow, radius, duration, strength);
        }

        void SpawnMark(ElementFxLibrary lib, string el, string content, Vector3 pos, float radius, float duration)
        {
            var p = Ground(pos, out var normal, out var hit);
            string surface = hit != null ? (hit.sharedMaterial != null ? hit.sharedMaterial.name : hit.name).ToLowerInvariant() : "";
            if (el == "wasser" && surface.Contains("lava")) { SpawnSim(lib, "feuer/rauch_loop", "rauch_loop", p, null, radius * 1.2f, 3f, 0.8f); return; }   // Dampf auf Lava
            if (el == "wasser" && surface.Contains("water")) { SpawnMesh(lib.Mat(el, "Mesh"), VfxLayerKind.FxMesh, "WaveRing", p, Quaternion.identity, null, radius * 1.5f, 1.2f, 1f); return; }   // Ringe auf Wasser
            var mat = lib.Mat(el, content == "nass" ? "Nass" : "Pfuetze"); if (mat == null) return;
            var i = Take(); i.Billboard = false;
            Activate(i, VfxLayerKind.WorldMark, content, MeshFor("QuadFlat"), mat, p + normal * 0.03f, Quaternion.FromToRotation(Vector3.up, normal), null, radius, duration, 1f);
        }

        void SpawnGpu(ElementFxLibrary lib, string id, Vector3 pos, Quaternion rot, float duration)
        {
            var preset = lib.Gpu(id); if (preset == null) return;
            Inst i = null;
            foreach (var x in _all) if (x.Gpu != null && !x.Active && x.Content == id) { i = x; break; }
            if (i == null)
            {
                int gpus = 0; foreach (var x in _all) if (x.Gpu != null) gpus++;
                if (gpus >= 16) return;   // Obergrenze gleichzeitiger GPU-Effekte
                var go = new GameObject("ElementGpu_" + id); go.transform.SetParent(transform, false); go.SetActive(false);
                var g = go.AddComponent<GpuParticleSystem>(); g.Compute = lib.ParticleCompute; g.Material = lib.ParticleMaterial; g.Settings = preset.Settings; g.Loop = false;
                i = new Inst { Go = go, Gpu = g, Content = id }; _all.Add(i);
            }
            i.Gpu.Duration = Mathf.Max(0.05f, preset.Duration);
            i.Go.transform.SetPositionAndRotation(pos, rot);
            i.Kind = VfxLayerKind.Gpu; i.Start = Time.time; i.Duration = i.Gpu.Duration + preset.Settings.LifeMax + 0.3f; i.Active = true;
            if (i.Go.activeSelf) i.Go.SetActive(false);
            i.Go.SetActive(true); i.Gpu.enabled = true; Spawned++;
        }

        static Vector3 Ground(Vector3 pos, out Vector3 normal, out Collider col)
        {
            normal = Vector3.up; col = null;
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) { normal = hit.normal; col = hit.collider; return hit.point; }
            return pos;
        }

        void Update()
        {
            var cam = Camera.main;
            foreach (var i in _all)
            {
                if (!i.Active) continue;
                float t = (Time.time - i.Start) / i.Duration;
                if (t >= 1f) { if (i.Gpu != null) { i.Active = false; i.Go.SetActive(false); } else Release(i); continue; }
                if (i.Gpu != null) continue;
                if (i.Follow != null) i.Go.transform.position = i.Follow.position;
                if (i.Billboard && cam != null)
                {
                    var f = i.Go.transform.position - cam.transform.position; f.y = 0f;
                    if (f.sqrMagnitude > 1e-4f) i.Go.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
                }
                Animate(i, t);
            }
        }

        /// <summary>Form-Animation je Lage (rein über <see cref="ElementAnim"/>), Alter an den Shader.</summary>
        void Animate(Inst i, float t)
        {
            i.Go.transform.localScale = ElementAnim.Scale(i.Kind, i.Content, i.Radius, t);
            if (i.Kind == VfxLayerKind.Sim)
            {
                float fade = ElementAnim.Fade(t);
                i.Mr.GetPropertyBlock(_mpb); _mpb.SetColor(PTint, new Color(i.BaseTint.r, i.BaseTint.g, i.BaseTint.b, i.BaseTint.a * fade * Mathf.Clamp01(i.Strength))); i.Mr.SetPropertyBlock(_mpb);
                return;
            }
            _mpb.Clear(); _mpb.SetFloat(PAge, t);
            if (i.Kind == VfxLayerKind.Refract) _mpb.SetFloat(PStrength, DistortionAccumulator.ClampShift(24f * i.Strength, false) * ElementAnim.Fade(t));
            if (i.Kind == VfxLayerKind.FxMesh) _mpb.SetColor(PTint, new Color(1f, 1f, 1f, Mathf.Clamp01(i.Strength)));
            i.Mr.SetPropertyBlock(_mpb);
        }
    }
}
