using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G2: eigene Vollbild-Passes über den Render Graph (Kontur, Nebel, Lichtschaft, Verzerrung, Unterwasser, Schaden).
    /// Jeder Pass hat einen Schalter in <see cref="GraphicsSettingsRuntime"/> und eine Bedingung (z. B. Unterwasser nur unter der Wasserlinie),
    /// sonst wird er nicht in den Graph eingetragen. Die Stufe (Niedrig … Ultra) legt fest, welche Passes das Renderer-Asset überhaupt kennt.
    /// </summary>
    public class PTRenderFeature : ScriptableRendererFeature
    {
        public Material PostFx;
        [Header("Je Qualitätsstufe verfügbar")]
        public bool Fog = true, Outline = true, Godrays = true, Distortion = true, Underwater = true, Damage = true;

        public const int PassFog = 0, PassOutline = 1, PassGodrays = 2, PassDistortion = 3, PassUnderwater = 4, PassDamage = 5;
        public static readonly string[] PassNames = { "PT Nebel", "PT Kontur", "PT Lichtschaft", "PT Verzerrung", "PT Unterwasser", "PT Schaden" };

        PTFullscreenPass _outline, _fog, _godrays, _distortion, _underwater, _damage;
        PTOverlayPass _overlay;
        PTDistortionAccumulatePass _accum;

        public override void Create()
        {
            _outline = new PTFullscreenPass(PassNames[PassOutline], PassOutline, RenderPassEvent.AfterRenderingSkybox, ScriptableRenderPassInput.Depth, () => Outline && RenderState.OutlineOn);
            _fog = new PTFullscreenPass(PassNames[PassFog], PassFog, RenderPassEvent.AfterRenderingSkybox, ScriptableRenderPassInput.Depth, () => Fog && RenderState.FogOn);
            _godrays = new PTFullscreenPass(PassNames[PassGodrays], PassGodrays, RenderPassEvent.BeforeRenderingPostProcessing, ScriptableRenderPassInput.Depth, () => Godrays && RenderState.GodraysOn);
            _distortion = new PTFullscreenPass(PassNames[PassDistortion], PassDistortion, RenderPassEvent.BeforeRenderingTransparents, ScriptableRenderPassInput.Depth, () => Distortion && (RenderState.DistortionOn || DistortionAccumulator.Wanted));   // V1: ein Blit vor den transparenten Partikeln
            _underwater = new PTFullscreenPass(PassNames[PassUnderwater], PassUnderwater, RenderPassEvent.BeforeRenderingPostProcessing, ScriptableRenderPassInput.Depth, () => Underwater && RenderState.UnderwaterOn);
            _damage = new PTFullscreenPass(PassNames[PassDamage], PassDamage, RenderPassEvent.BeforeRenderingPostProcessing, ScriptableRenderPassInput.None, () => Damage && RenderState.DamageOn);
            _overlay = new PTOverlayPass();
            _accum = new PTDistortionAccumulatePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (PostFx == null) return;
            Enqueue(renderer, _outline); Enqueue(renderer, _fog);
            bool buffer = Distortion && DistortionAccumulator.Wanted;
            Shader.SetGlobalFloat(DistortionAccumulator.HasBufferId, buffer ? 1f : 0f);
            if (buffer && _accum != null) renderer.EnqueuePass(_accum);
            Enqueue(renderer, _godrays); Enqueue(renderer, _distortion); Enqueue(renderer, _underwater); Enqueue(renderer, _damage);
            if (_overlay != null) renderer.EnqueuePass(_overlay);
        }

        void Enqueue(ScriptableRenderer renderer, PTFullscreenPass p)
        {
            if (p == null || !p.Active) return;
            p.Material = PostFx;
            renderer.EnqueuePass(p);
        }
    }

    /// <summary>Laufzeit-Zustand der Passes (gesetzt von SceneLighting / GraphicsDirector). Statisch, damit Editor-Captures dieselben Werte nutzen.</summary>
    public static class RenderState
    {
        public static bool FogOn = true, OutlineOn = true, GodraysOn, DistortionOn, UnderwaterOn, DamageOn;
    }

    /// <summary>Ein Vollbild-Blit mit eigenem Material-Pass: Quelle = aktive Kamerafarbe, Ziel = neue Textur, danach Kamerafarbe = Ziel.</summary>
    public class PTFullscreenPass : ScriptableRenderPass
    {
        readonly int _pass;
        readonly string _name;
        readonly Func<bool> _active;
        readonly ProfilingSampler _sampler;
        public Material Material;
        public bool Active => _active == null || _active();

        class PassData { public TextureHandle Source; public Material Material; public int Pass; }

        public PTFullscreenPass(string name, int pass, RenderPassEvent evt, ScriptableRenderPassInput input, Func<bool> active)
        {
            _name = name; _pass = pass; _active = active;
            renderPassEvent = evt;
            requiresIntermediateTexture = true;
            ConfigureInput(input);
            _sampler = new ProfilingSampler(name);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (Material == null) return;
            var cam = frameData.Get<UniversalCameraData>();
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var res = frameData.Get<UniversalResourceData>();
            if (res.isActiveTargetBackBuffer) return;
            var src = res.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(src);
            desc.name = _name; desc.clearBuffer = false;   // MSAA wie die Quelle (folgende Passes erwarten dieselbe Probenzahl)
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(_name, out var data, _sampler))
            {
                data.Source = src; data.Material = Material; data.Pass = _pass;
                builder.UseTexture(src, AccessFlags.Read);
                if (res.cameraDepthTexture.IsValid()) builder.UseTexture(res.cameraDepthTexture, AccessFlags.Read);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => Blitter.BlitTexture(ctx.cmd, d.Source, new Vector4(1, 1, 0, 0), d.Material, d.Pass));
            }
            res.cameraColor = dst;
        }
    }

    /// <summary>
    /// Grafik-Lauf G7 (Lesbarkeit vor Schönheit): Telegrafen, Gefahrenflächen und Zielringe (LightMode „PTOverlay“) werden nach dem
    /// Post-Processing gezeichnet – kein Bloom-Hof auf dem Boden, keine ACES-Entsättigung, keine Nebel- oder Tageszeit-Tönung.
    /// </summary>
    public class PTOverlayPass : ScriptableRenderPass
    {
        static readonly ShaderTagId Tag = new ShaderTagId("PTOverlay");
        readonly ProfilingSampler _sampler = new ProfilingSampler("PT Telegrafen");
        class PassData { public RendererListHandle List; }

        public PTOverlayPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cam = frameData.Get<UniversalCameraData>();
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var res = frameData.Get<UniversalResourceData>();
            var rd = frameData.Get<UniversalRenderingData>();
            var light = frameData.Get<UniversalLightData>();
            var ds = RenderingUtils.CreateDrawingSettings(Tag, rd, cam, light, SortingCriteria.CommonTransparent);
            var list = renderGraph.CreateRendererList(new RendererListParams(rd.cullResults, ds, new FilteringSettings(RenderQueueRange.all)));
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PT Telegrafen", out var data, _sampler))
            {
                data.List = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(res.activeColorTexture, 0, AccessFlags.ReadWrite);
                if (res.cameraDepthTexture.IsValid()) builder.UseTexture(res.cameraDepthTexture, AccessFlags.Read);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.List));
            }
        }
    }
}
