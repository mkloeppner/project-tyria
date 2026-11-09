using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// Charakter-Animation über die Playables-API (kein AnimatorController):
    /// Schicht 0 = Basis (2D-Fortbewegung ↔ Ganzkörper-Aktion), Schicht 1 = Oberkörper-Aktion (Maske ab Spine1),
    /// Schicht 2 = additives Atmen, Schicht 3 = additive Treffer-Reaktion. Überblendungen 0,1–0,25 s.
    /// Fuß-IK (TwoBoneIK) und Blick-IK (MultiAim) aus com.unity.animation.rigging werden in denselben Graphen gebaut.
    /// Auswertung manuell: nahe Charaktere jeden Frame, entfernte mit 15 Hz, unsichtbare mit 5 Hz.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(50)]
    public class CharacterAnimator : MonoBehaviour
    {
        public AnimClipSet Set;
        [Tooltip("Präfix der Tier-Clips, z. B. „Quadruped_“ (leer = Humanoid)")] public string ClipPrefix = "";
        [Tooltip("Waffenstil (Clip-Präfix der Waffen-Clips)")] public string WeaponStyle = "Greatsword";
        [Tooltip("Clip-Namen für [Idle, Walk, Run, Sprint, StrafeL, StrafeR, Back] (leer = Standard-Humanoid)")]
        public string[] LocoClips = new string[LocomotionBlend.Count];
        public string BreathClip = "Breath";
        [Tooltip("Optional: Haltung beim Start (z. B. Verletzte liegen, Sitzende) – Ganzkörper, gehalten")] public string StartClip = "";
        public bool UpperBodyLayer = true;
        public string UpperBodyRoot = SkeletonDef.Spine1;
        [Header("Tempo der Fortbewegungs-Clips (m/s)")]
        public float WalkSpeed = 2.6f, RunSpeed = 7f, SprintSpeed = 10f;
        [Header("IK")]
        public Transform Body;
        public TwoBoneIKConstraint FootIKL, FootIKR;
        public MultiAimConstraint LookIK;
        public Transform LookTarget;
        public LayerMask GroundMask = 1;
        public float MaxHipDrop = 0.4f;
        [Header("LOD")]
        public float NearDistance = 12f, FarDistance = 30f;
        [Tooltip("0 = jeder Frame")] public float NearHz = 0f, MidHz = 30f, FarHz = 15f, HiddenHz = 5f;
        [Tooltip("IK nur für nahe Charaktere (Kosten)")] public float IkDistance = 15f;
        bool _ikLod = true;

        public event Action<string, string> MarkerReached;   // (Markertyp, Clipname)
        public event Action<string> ActionEnded;             // Clipname

        public static double FrameMs { get; private set; }
        /// <summary>Gleitender Mittelwert der Auswertungszeit aller Charaktere je Frame (ms).</summary>
        public static double AvgFrameMs { get; private set; }
        public static double PeakFrameMs { get; set; }
        public static int EvaluatedLastFrame { get; private set; }
        static double _accMs; static int _accN, _statFrame = -1;
        public static readonly List<CharacterAnimator> Active = new List<CharacterAnimator>();

        PlayableGraph _graph;
        AnimationLayerMixerPlayable _layers;
        AnimationMixerPlayable _base, _loco;
        readonly AnimationClipPlayable[] _locoP = new AnimationClipPlayable[LocomotionBlend.Count];
        readonly AnimClipSet.Entry[] _locoE = new AnimClipSet.Entry[LocomotionBlend.Count];
        readonly float[] _w = new float[LocomotionBlend.Count], _wSmooth = new float[LocomotionBlend.Count], _len = new float[LocomotionBlend.Count];
        Slot _full, _upper, _hit;
        Animator _animator; RigBuilder _rig; Renderer _renderer;
        float _phase, _idleTime, _accDt, _forward, _right, _speed, _hipDrop, _lookWeight, _lookWeightTarget, _ikWeight = 1f;
        bool _grounded = true, _built, _ikEnabled = true;
        Vector3 _lookPoint;
        public float Phase => _phase;
        public bool Built => _built;
        public string CurrentFull => _full != null && _full.Active ? _full.CurrentName : null;
        public string CurrentUpper => _upper != null && _upper.Active ? _upper.CurrentName : null;
        public float FullWeight => _full != null ? _full.Weight : 0f;
        public float UpperWeight => _upper != null ? _upper.Weight : 0f;
        public float[] LocoWeights => _wSmooth;
        public bool IkEnabled { get => _ikEnabled; set => _ikEnabled = value; }

        class Slot
        {
            public const int N = 3;
            public AnimationMixerPlayable Mixer;
            public readonly AnimationClipPlayable[] P = new AnimationClipPlayable[N];
            public readonly AnimClipSet.Entry[] E = new AnimClipSet.Entry[N];
            public readonly float[] W = new float[N], Tgt = new float[N];
            public float Fade = 0.15f, Weight, WeightTarget, PrevTime, Speed = 1f;
            public int Cur = -1; public bool Hold, Ended;
            public bool Active => Cur >= 0 && (Weight > 0.001f || WeightTarget > 0f);
            public string CurrentName => Cur >= 0 && E[Cur] != null ? E[Cur].Name : null;
            public AnimClipSet.Entry Current => Cur >= 0 ? E[Cur] : null;
            public float Time => Cur >= 0 && P[Cur].IsValid() ? (float)P[Cur].GetTime() : 0f;
        }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.runtimeAnimatorController = null;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _rig = GetComponent<RigBuilder>();
            _renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Start() { Build(); }

        public void Build()
        {
            if (_built || Set == null) return;
            _graph = PlayableGraph.Create(name + "_Anim");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "Anim", _animator);

            _loco = AnimationMixerPlayable.Create(_graph, LocomotionBlend.Count);
            for (int i = 0; i < LocomotionBlend.Count; i++)
            {
                string n = LocoClips != null && i < LocoClips.Length && !string.IsNullOrEmpty(LocoClips[i]) ? LocoClips[i] : LocomotionBlend.Names[i];
                var e = Set.Get(n) ?? Set.Get(LocomotionBlend.Names[i == LocomotionBlend.Sprint ? LocomotionBlend.Run : i >= LocomotionBlend.StrafeL ? LocomotionBlend.Walk : i]);
                if (e == null) e = Set.Get(LocoClips != null && LocoClips.Length > 1 ? LocoClips[1] : "Walk");
                _locoE[i] = e;
                _len[i] = e != null ? Mathf.Max(0.1f, e.Length) : 1f;
                if (e == null) continue;
                var p = AnimationClipPlayable.Create(_graph, e.Clip);
                p.SetApplyFootIK(false); p.SetSpeed(0);
                _locoP[i] = p;
                _graph.Connect(p, 0, _loco, i);
            }
            _full = MakeSlot();
            _base = AnimationMixerPlayable.Create(_graph, 2);
            _graph.Connect(_loco, 0, _base, 0); _graph.Connect(_full.Mixer, 0, _base, 1);
            _base.SetInputWeight(0, 1f);

            _layers = AnimationLayerMixerPlayable.Create(_graph, 4);
            _graph.Connect(_base, 0, _layers, 0); _layers.SetInputWeight(0, 1f);
            _upper = MakeSlot();
            _graph.Connect(_upper.Mixer, 0, _layers, 1);
            var mask = BuildUpperMask();
            if (mask != null) _layers.SetLayerMaskFromAvatarMask(1, mask);
            var breath = Set.Get(BreathClip);
            if (breath != null)
            {
                var bp = AnimationClipPlayable.Create(_graph, breath.Clip);
                _graph.Connect(bp, 0, _layers, 2);
                _layers.SetLayerAdditive(2, true); _layers.SetInputWeight(2, 1f);
                bp.SetTime(UnityEngine.Random.value * breath.Length);
            }
            _hit = MakeSlot();
            _graph.Connect(_hit.Mixer, 0, _layers, 3);
            _layers.SetLayerAdditive(3, true);
            output.SetSourcePlayable(_layers);

            if (_rig != null && _rig.layers.Count > 0) _rig.Build(_graph);
            _phase = UnityEngine.Random.value; _idleTime = UnityEngine.Random.value * 3f;
            _accDt = UnityEngine.Random.value * 0.2f;   // LOD-Takte verteilen (keine Spitzen, wenn viele Charaktere im selben Frame auswerten)
            _built = true;
            if (!string.IsNullOrEmpty(StartClip)) PlayFull(StartClip, 0.05f, 1f, true);
            Tick(0f, true);
        }

        Slot MakeSlot()
        {
            var s = new Slot { Mixer = AnimationMixerPlayable.Create(_graph, Slot.N) };
            return s;
        }

        AvatarMask BuildUpperMask()
        {
            if (!UpperBodyLayer) return null;
            var all = GetComponentsInChildren<Transform>(true);
            Transform upperRoot = null;
            foreach (var t in all) if (t.name == UpperBodyRoot) { upperRoot = t; break; }
            if (upperRoot == null) return null;
            var mask = new AvatarMask();
            var paths = new List<KeyValuePair<string, bool>>();
            foreach (var t in all)
            {
                if (t == transform) continue;
                paths.Add(new KeyValuePair<string, bool>(RelPath(t), t == upperRoot || t.IsChildOf(upperRoot)));
            }
            mask.transformCount = paths.Count;
            for (int i = 0; i < paths.Count; i++) { mask.SetTransformPath(i, paths[i].Key); mask.SetTransformActive(i, paths[i].Value); }
            return mask;
        }

        string RelPath(Transform t)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != transform; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }

        /// <summary>Empfänger der AnimationEvents in den Clips (nur zur Anzeige im Editor; Marker laufen über <see cref="MarkerReached"/>).</summary>
        void OnAnimMarker(string type) { }

        float _freeze;
        /// <summary>Lauf 5 / M22: Hit-Stop – Animation läuft für <paramref name="seconds"/> mit 5 % Tempo (Angreifer und Ziel).</summary>
        public void Freeze(float seconds) { _freeze = Mathf.Max(_freeze, seconds); }
        public bool Frozen => _freeze > 0f;

        public void SetFullSpeed(float speed) { if (_full != null && _full.Cur >= 0 && _full.P[_full.Cur].IsValid() && !_full.Ended) { _full.P[_full.Cur].SetSpeed(speed); _full.Speed = speed; } }

        // ------------------------------------------------------------ Steuerung
        public void SetMotion(Vector3 worldVelocity, bool grounded)
        {
            var l = transform.InverseTransformDirection(worldVelocity);
            _forward = l.z; _right = l.x; _speed = new Vector2(l.x, l.z).magnitude; _grounded = grounded;
        }

        public AnimClipSet.Entry Find(string clip) => Set != null ? Set.Get(clip) : null;

        /// <summary>Ganzkörper-Aktion (überdeckt die Fortbewegung).</summary>
        public AnimClipSet.Entry PlayFull(string clip, float fade = 0.15f, float speed = 1f, bool hold = false) => Play(_full, clip, fade, speed, hold);
        /// <summary>Oberkörper-Aktion (Beine laufen weiter).</summary>
        public AnimClipSet.Entry PlayUpper(string clip, float fade = 0.12f, float speed = 1f, bool hold = false) => Play(_upper, clip, fade, speed, hold);
        /// <summary>Additive Reaktion (Treffer).</summary>
        public AnimClipSet.Entry PlayHit(string clip, float speed = 1f) => Play(_hit, clip, 0.05f, speed, false);
        /// <summary>Aktion nach Schicht des Clips (Ganzkörper/Oberkörper/additiv).</summary>
        public AnimClipSet.Entry PlayAuto(string clip, float fade = 0.15f, float speed = 1f, bool hold = false)
        {
            var e = Find(clip); if (e == null) return null;
            switch (e.Layer)
            {
                case ClipLayer.UpperBody: return UpperBodyLayer ? PlayUpper(clip, fade, speed, hold) : PlayFull(clip, fade, speed, hold);
                case ClipLayer.Additive: return PlayHit(clip, speed);
                default: return PlayFull(clip, fade, speed, hold);
            }
        }
        public void StopFull(float fade = 0.2f) => Stop(_full, fade);
        public void StopUpper(float fade = 0.15f) => Stop(_upper, fade);
        public void StopAll(float fade = 0.15f) { StopFull(fade); StopUpper(fade); }

        public void SetLook(Vector3? point, float weight = 0.8f)
        {
            if (point.HasValue) { _lookPoint = point.Value; _lookWeightTarget = weight; } else _lookWeightTarget = 0f;
        }

        AnimClipSet.Entry Play(Slot s, string clip, float fade, float speed, bool hold)
        {
            if (!_built) Build();
            if (s == null) return null;
            var e = Find(clip);
            if (e == null || e.Clip == null) return null;
            fade = Mathf.Clamp(fade, 0.05f, 0.25f);
            int idx = 0; float lowest = float.MaxValue;
            for (int i = 0; i < Slot.N; i++) { if (i == s.Cur) continue; if (s.W[i] < lowest) { lowest = s.W[i]; idx = i; } }
            if (s.P[idx].IsValid()) { _graph.Disconnect(s.Mixer, idx); s.P[idx].Destroy(); }
            var p = AnimationClipPlayable.Create(_graph, e.Clip);
            p.SetApplyFootIK(false); p.SetTime(0); p.SetSpeed(speed);
            _graph.Connect(p, 0, s.Mixer, idx);
            s.P[idx] = p; s.E[idx] = e; s.W[idx] = s.Weight <= 0.01f ? 1f : 0f;
            for (int i = 0; i < Slot.N; i++) s.Tgt[i] = i == idx ? 1f : 0f;
            s.Cur = idx; s.Fade = fade; s.WeightTarget = 1f; s.PrevTime = 0f; s.Hold = hold; s.Ended = false; s.Speed = speed;
            return e;
        }

        void Stop(Slot s, float fade) { if (s == null) return; s.WeightTarget = 0f; s.Fade = Mathf.Clamp(fade, 0.05f, 0.25f); s.Hold = false; }

        // ------------------------------------------------------------ Auswertung
        void LateUpdate()
        {
            if (!_built) return;
            float hz = NearHz;
            var cam = Camera.main;
            float d2 = cam != null ? (cam.transform.position - transform.position).sqrMagnitude : 0f;
            if (_renderer != null && !_renderer.isVisible) hz = HiddenHz;
            else if (d2 > FarDistance * FarDistance) hz = FarHz;
            else if (d2 > NearDistance * NearDistance) hz = MidHz;
            bool ikLod = d2 <= IkDistance * IkDistance && hz != HiddenHz;
            if (ikLod != _ikLod) { _ikLod = ikLod; if (_rig != null) foreach (var l in _rig.layers) if (l.rig != null) l.rig.weight = ikLod ? 1f : 0f; }
            _accDt += Time.deltaTime;
            if (hz > 0f && _accDt < 1f / hz) return;
            float dt = _accDt; _accDt = 0f;
            if (_freeze > 0f) { _freeze -= dt; dt *= 0.05f; }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Tick(dt, false);
            sw.Stop();
            if (_statFrame != Time.frameCount)
            {
                FrameMs = _accMs; EvaluatedLastFrame = _accN; AvgFrameMs = AvgFrameMs * 0.97 + _accMs * 0.03; if (_accMs > PeakFrameMs) PeakFrameMs = _accMs;
                _accMs = 0; _accN = 0; _statFrame = Time.frameCount;
            }
            _accMs += sw.Elapsed.TotalMilliseconds; _accN++;
        }

        /// <summary>Für Tests/Galerie: sofort um dt auswerten.</summary>
        public void Tick(float dt, bool force)
        {
            if (!_built) return;
            // Fortbewegung
            LocomotionBlend.Weights(_grounded ? _forward : 0f, _grounded ? _right : 0f, _w, WalkSpeed, RunSpeed, SprintSpeed);
            float k = dt <= 0f ? 1f : 1f - Mathf.Exp(-dt * 10f);
            float sum = 0f;
            for (int i = 0; i < _w.Length; i++) { _wSmooth[i] = Mathf.Lerp(_wSmooth[i], _w[i], force ? 1f : k); sum += _wSmooth[i]; }
            float rate = LocomotionBlend.PhaseRate(_wSmooth, _len, _speed);
            _phase = Mathf.Repeat(_phase + rate * dt, 1f);
            _idleTime += dt;
            for (int i = 0; i < _locoP.Length; i++)
            {
                if (!_locoP[i].IsValid()) continue;
                float wi = sum > 0f ? _wSmooth[i] / sum : (i == 0 ? 1f : 0f);
                if (wi < 0.002f) wi = 0f;
                _loco.SetInputWeight(i, wi);
                if (wi <= 0f) continue;
                double t = i == LocomotionBlend.Idle ? Mathf.Repeat(_idleTime, _len[i]) : _phase * _len[i];
                _locoP[i].SetTime(t);
            }
            // Markers der Fortbewegung (Fußkontakt) aus dem stärksten Clip
            LocoMarkers(dt, rate);
            // Aktionsschichten
            TickSlot(_full, dt); TickSlot(_upper, dt); TickSlot(_hit, dt);
            _base.SetInputWeight(0, 1f - _full.Weight); _base.SetInputWeight(1, _full.Weight);
            _layers.SetInputWeight(1, _upper.Weight); _layers.SetInputWeight(3, _hit.Weight);
            // IK
            UpdateIK(dt);
            if (_rig != null) _rig.SyncLayers();
            _graph.Evaluate(dt);
        }

        float _prevPhase;
        void LocoMarkers(float dt, float rate)
        {
            if (_full.Weight > 0.5f || MarkerReached == null) { _prevPhase = _phase; return; }
            int best = -1; float bw = 0.25f;
            for (int i = 1; i < _wSmooth.Length; i++) if (_wSmooth[i] > bw) { bw = _wSmooth[i]; best = i; }
            if (best < 0 || _locoE[best] == null) { _prevPhase = _phase; return; }
            float a = _prevPhase, b = _phase; var e = _locoE[best];
            foreach (var m in e.Markers)
            {
                float u = e.Length > 0f ? m.Time / e.Length : 0f;
                bool hit = a <= b ? (u > a && u <= b) : (u > a || u <= b);
                if (hit) MarkerReached(m.Type, e.Name);
            }
            _prevPhase = _phase;
        }

        void TickSlot(Slot s, float dt)
        {
            float step = s.Fade <= 0f ? 1f : dt / s.Fade;
            float sum = 0f;
            for (int i = 0; i < Slot.N; i++) { s.W[i] = Mathf.MoveTowards(s.W[i], s.Tgt[i], step); sum += s.W[i]; }
            for (int i = 0; i < Slot.N; i++) if (s.P[i].IsValid()) s.Mixer.SetInputWeight(i, sum > 0f ? s.W[i] / sum : 0f);
            s.Weight = Mathf.MoveTowards(s.Weight, s.WeightTarget, step);
            if (s.Cur < 0) return;
            var e = s.E[s.Cur]; var p = s.P[s.Cur];
            if (e == null || !p.IsValid()) return;
            float t = (float)p.GetTime();
            float next = t + dt * s.Speed;   // Zeit nach diesem Evaluate
            if (MarkerReached != null && s.WeightTarget > 0f)
            {
                if (e.Loop)
                {
                    float a = Mathf.Repeat(s.PrevTime, e.Length), b = Mathf.Repeat(next, e.Length);
                    foreach (var m in e.Markers) { bool hit = a <= b ? (m.Time > a && m.Time <= b) : (m.Time > a || m.Time <= b); if (hit && next > 0f) MarkerReached(m.Type, e.Name); }
                }
                else foreach (var m in e.Markers) if ((m.Time > s.PrevTime || (s.PrevTime == 0f && m.Time == 0f && next > 0f)) && m.Time <= next) MarkerReached(m.Type, e.Name);
            }
            s.PrevTime = next;
            if (!e.Loop && !s.Ended && next >= e.Length)
            {
                s.Ended = true;
                if (s.Hold) { p.SetTime(e.Length); p.SetSpeed(0); }
                else { s.WeightTarget = 0f; s.Fade = 0.2f; }
                ActionEnded?.Invoke(e.Name);
            }
        }

        void UpdateIK(float dt)
        {
            float k = dt <= 0f ? 1f : 1f - Mathf.Exp(-dt * 8f);
            if (LookIK != null)
            {
                _lookWeight = Mathf.MoveTowards(_lookWeight, _lookWeightTarget * (1f - _full.Weight * 0.8f), dt * 3f);
                LookIK.weight = _lookWeight;
                if (LookTarget != null && _lookWeightTarget > 0f) LookTarget.position = Vector3.Lerp(LookTarget.position, _lookPoint, k);
            }
            if (FootIKL == null || FootIKR == null || Body == null) return;
            if (!_ikLod) { FootIKL.weight = 0f; FootIKR.weight = 0f; if (LookIK != null) LookIK.weight = 0f; Body.localPosition = Vector3.zero; _hipDrop = 0f; return; }
            float want = _ikEnabled && _grounded ? 1f - _full.Weight : 0f;
            _ikWeight = Mathf.MoveTowards(_ikWeight, want, dt * 5f);
            float dropL = 0f, dropR = 0f;
            bool okL = FootTarget(true, FootIKL, out dropL), okR = FootTarget(false, FootIKR, out dropR);
            FootIKL.weight = okL ? _ikWeight : 0f; FootIKR.weight = okR ? _ikWeight : 0f;
            float drop = Mathf.Clamp(Mathf.Min(0f, Mathf.Min(okL ? dropL : 0f, okR ? dropR : 0f)), -MaxHipDrop, 0f) * _ikWeight;
            _hipDrop = Mathf.Lerp(_hipDrop, drop, k);
            Body.localPosition = new Vector3(0f, _hipDrop, 0f);
        }

        /// <summary>Fußziel = geblendete Fußposition der Clips, Höhe auf den Boden unter dem Fuß verschoben.</summary>
        bool FootTarget(bool left, TwoBoneIKConstraint ik, out float drop)
        {
            drop = 0f;
            var target = ik.data.target; if (target == null) return false;
            Vector3 local = Vector3.zero; float tw = 0f;
            float locoW = 1f - _full.Weight;
            float sum = 0f; for (int i = 0; i < _wSmooth.Length; i++) sum += _wSmooth[i];
            for (int i = 0; i < _locoE.Length; i++)
            {
                var e = _locoE[i]; if (e == null || _wSmooth[i] <= 0.001f || sum <= 0f) continue;
                float t = i == LocomotionBlend.Idle ? Mathf.Repeat(_idleTime, _len[i]) : _phase * _len[i];
                float w = _wSmooth[i] / sum * locoW;
                local += e.Foot(left, t) * w; tw += w;
            }
            var fe = _full.Current;
            if (fe != null && _full.Weight > 0.001f && fe.FootL != null && fe.FootL.Length > 0) { local += fe.Foot(left, _full.Time) * _full.Weight; tw += _full.Weight; }
            if (tw <= 0.001f) return false;
            local /= tw;
            Vector3 world = transform.TransformPoint(new Vector3(local.x, 0f, local.z));
            float rootY = transform.position.y;
            if (!Physics.Raycast(world + Vector3.up * 0.7f, Vector3.down, out var hit, 1.6f, GroundMask, QueryTriggerInteraction.Ignore)) return false;
            float gy = hit.point.y;
            drop = gy - rootY;
            target.position = new Vector3(world.x, gy + local.y * transform.lossyScale.y, world.z);
            target.rotation = transform.rotation;
            return true;
        }
    }
}
