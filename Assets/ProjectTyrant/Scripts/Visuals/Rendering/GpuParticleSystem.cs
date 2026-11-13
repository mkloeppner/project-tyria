using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// VFX-Lauf V1: GPU-Partikel-Effekt (Compute-HLSL, Hoch/Ultra). Ohne Compute oder unter Hoch: Shuriken-Fallback mit
    /// reduzierter Emission – kein Effekt fehlt auf Niedrig, er wird nur einfacher. Gesamtbudget je Stufe in <see cref="GpuParticleModel.Budget"/>.
    /// </summary>
    public class GpuParticleSystem : MonoBehaviour
    {
        public GpuParticleSettings Settings = new GpuParticleSettings();
        public ComputeShader Compute;
        public Material Material;
        public float Duration = 2f;
        public bool Loop;

        public static int AllocatedTotal { get; private set; }
        public bool UsingGpu => _buffer != null;
        public ParticleSystem Fallback => _fallback;

        ComputeBuffer _buffer; int _capacity; uint _emitCursor, _frame; float _emitCarry, _time; bool _burstDone;
        int _kEmit = -1, _kUpdate = -1; MaterialPropertyBlock _mpb; ParticleSystem _fallback;
        static readonly int PParticles = Shader.PropertyToID("_Particles"), PDepth = Shader.PropertyToID("_CameraDepthTexture");

        void OnEnable()
        {
            _time = 0f; _burstDone = false; _emitCarry = 0f;
            var tier = GraphicsSettingsRuntime.Tier;
            if (Compute != null && Material != null && GpuParticleModel.UseGpu(tier, SystemInfo.supportsComputeShaders))
            {
                int free = GpuParticleModel.Budget(tier) - AllocatedTotal;
                _capacity = Mathf.Min(Settings.Capacity, free);
                if (_capacity >= 256)
                {
                    _buffer = new ComputeBuffer(_capacity, GpuParticle.Stride);
                    _buffer.SetData(new GpuParticle[_capacity]);   // alle tot (Life 0)
                    AllocatedTotal += _capacity;
                    _kEmit = Compute.FindKernel("Emit"); _kUpdate = Compute.FindKernel("Update");
                    _mpb = new MaterialPropertyBlock();
                    return;
                }
            }
            BuildFallback(tier);
        }

        void OnDisable()
        {
            if (_buffer != null) { AllocatedTotal = Mathf.Max(0, AllocatedTotal - _capacity); _buffer.Release(); _buffer = null; }
        }

        void BuildFallback(QualityTier tier)
        {
            if (_fallback != null) { _fallback.Play(); return; }
            var go = new GameObject("Shuriken-Fallback"); go.transform.SetParent(transform, false);
            _fallback = go.AddComponent<ParticleSystem>();
            var s = Settings; float share = GpuParticleModel.FallbackEmissionShare(tier);
            var main = _fallback.main; main.loop = Loop; main.duration = Mathf.Max(0.1f, Duration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(s.LifeMin, s.LifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s.Speed * (1f - s.SpeedJitter), s.Speed * (1f + s.SpeedJitter));
            main.startSize = s.SizeStart * 2f; main.gravityModifier = -s.Gravity.y / 9.81f; main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(s.Capacity * share), 50, 4000);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = s.ColorStart;
            var em = _fallback.emission; em.rateOverTime = s.EmitPerSecond * share;
            if (s.Burst > 0f) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(s.Burst * share, 1, 2000)) });
            var sh = _fallback.shape; sh.radius = Mathf.Max(0.01f, s.ShapeRadius);
            sh.shapeType = s.Shape == GpuEmitterShape.Cone ? ParticleSystemShapeType.Cone : s.Shape == GpuEmitterShape.RingEdge ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
            if (s.Shape == GpuEmitterShape.Cone) sh.angle = s.ConeAngle;
            var col = _fallback.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(s.ColorStart, 0f), new GradientColorKey(s.ColorEnd, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = _fallback.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, s.SizeEnd / Mathf.Max(0.001f, s.SizeStart)));
            if (s.CollideDepth) { var c = _fallback.collision; c.enabled = true; c.type = ParticleSystemCollisionType.World; c.bounce = s.Bounce; c.quality = ParticleSystemCollisionQuality.Low; }
            var r = go.GetComponent<ParticleSystemRenderer>();
            var sh2 = Shader.Find(PTShaders.FxParticle);
            if (sh2 != null) r.sharedMaterial = new Material(sh2) { name = "GpuFallback" };
            r.renderMode = Settings.Render == GpuRenderMode.Stretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            _fallback.Play();
        }

        void Update()
        {
            if (_buffer == null) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f); _time += dt; _frame++;
            var s = Settings; int emit = 0;
            if (!_burstDone && s.Burst > 0f) { emit += Mathf.RoundToInt(s.Burst); _burstDone = true; }
            if (Loop || _time <= Duration) { _emitCarry += s.EmitPerSecond * dt; int n = (int)_emitCarry; _emitCarry -= n; emit += n; }
            emit = Mathf.Min(emit, _capacity);
            var cs = Compute;
            cs.SetFloat("_Dt", dt); cs.SetFloat("_Time", _time); cs.SetInt("_Capacity", _capacity);
            cs.SetVector("_Gravity", s.Gravity); cs.SetVector("_Wind", WindNow()); cs.SetVector("_Center", transform.position);
            cs.SetFloat("_WindFactor", s.WindFactor); cs.SetFloat("_Drag", s.Drag); cs.SetFloat("_VortexStrength", s.VortexStrength); cs.SetFloat("_VortexRadius", s.VortexRadius);
            cs.SetFloat("_Attract", s.Attract); cs.SetFloat("_CurlStrength", s.CurlStrength); cs.SetFloat("_CurlScale", s.CurlScale);
            cs.SetFloat("_Bounce", s.Bounce); cs.SetFloat("_StickBelowSpeed", s.StickBelowSpeed); cs.SetFloat("_Collide", s.CollideDepth ? 1f : 0f);
            cs.SetFloat("_SizeStart", s.SizeStart); cs.SetFloat("_SizeEnd", s.SizeEnd);
            var cam = Camera.main; var depth = Shader.GetGlobalTexture(PDepth);
            bool depthOk = cam != null && depth != null && s.CollideDepth;
            cs.SetFloat("_DepthValid", depthOk ? 1f : 0f);
            if (depthOk)
            {
                cs.SetTexture(_kUpdate, "_DepthTex", depth);
                cs.SetMatrix("_ViewProj", GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix);
                cs.SetVector("_ZParams", Shader.GetGlobalVector("_ZBufferParams"));
            }
            else cs.SetTexture(_kUpdate, "_DepthTex", Texture2D.blackTexture);
            if (emit > 0)
            {
                cs.SetInt("_EmitStart", (int)_emitCursor); cs.SetInt("_EmitCount", emit); cs.SetInt("_FrameSeed", (int)_frame); cs.SetInt("_Shape", (int)s.Shape);
                cs.SetVector("_Origin", transform.position); cs.SetVector("_Forward", transform.forward);
                cs.SetFloat("_ShapeRadius", s.ShapeRadius); cs.SetFloat("_ConeAngle", s.ConeAngle); cs.SetFloat("_Speed", s.Speed); cs.SetFloat("_SpeedJitter", s.SpeedJitter);
                cs.SetFloat("_LifeMin", s.LifeMin); cs.SetFloat("_LifeMax", s.LifeMax);
                cs.SetBuffer(_kEmit, PParticles, _buffer);
                cs.Dispatch(_kEmit, Mathf.CeilToInt(emit / 64f), 1, 1);
                _emitCursor = (uint)((_emitCursor + emit) % _capacity);
            }
            cs.SetBuffer(_kUpdate, PParticles, _buffer);
            cs.Dispatch(_kUpdate, Mathf.CeilToInt(_capacity / 256f), 1, 1);

            _mpb.SetBuffer(PParticles, _buffer);
            _mpb.SetColor("_ColorStart", s.ColorStart); _mpb.SetColor("_ColorEnd", s.ColorEnd); _mpb.SetFloat("_Stretch", s.Render == GpuRenderMode.Stretched ? 0.05f : 0f);
            var rp = new RenderParams(Material) { worldBounds = new Bounds(transform.position, Vector3.one * 60f), matProps = _mpb, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false };
            Graphics.RenderPrimitives(rp, MeshTopology.Triangles, _capacity * 6);
            if (!Loop && _time > Duration + s.LifeMax + 0.2f) enabled = false;
        }

        static Vector3 WindNow() { var w = Shader.GetGlobalVector("_PT_Wind"); return new Vector3(w.x, 0f, w.y) * w.z; }
    }
}
