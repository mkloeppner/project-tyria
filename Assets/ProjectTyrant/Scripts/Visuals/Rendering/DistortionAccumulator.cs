using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// VFX-Lauf V1: gemeinsamer Verzerrungs-Puffer. Alle brechenden Effekte (Wasser, Hitze, Linsen, Kristall, Druckwellen-Meshes)
    /// zeichnen ihren Pass „PTDistort“ additiv in einen RGBA16F-Puffer (rg Verschiebung in UV, b Dispersion, a Deckung).
    /// Nur ein Puffer und ein Blit je Bild (PT_PostFx Pass 3, vor den transparenten Partikeln), keine Grab-Passes je Material.
    /// </summary>
    public static class DistortionAccumulator
    {
        public static readonly int BufferId = Shader.PropertyToID("_PT_DistortBuffer"), HasBufferId = Shader.PropertyToID("_PT_HasDistortBuffer");
        static readonly HashSet<Object> _sources = new HashSet<Object>();
        public static int ActiveSources => _sources.Count;
        public static void Register(Object o) { if (o != null) _sources.Add(o); }
        public static void Unregister(Object o) { if (!ReferenceEquals(o, null)) _sources.Remove(o); _sources.RemoveWhere(x => x == null); }
        public static void Clear() => _sources.Clear();
        /// <summary>Editor-Captures und Tests: Puffer erzwingen, auch ohne registrierte Quelle.</summary>
        public static bool Force;
        public static bool Wanted => Force || _sources.Count > 0;

        /// <summary>Maximal zulässige Verschiebung und Farbtrennung (Regel 8), in Pixeln bei 1080p.</summary>
        public const float MaxShiftPx = 24f, MaxDispersionPx = 6f;
        public static float ClampShift(float px, bool photosensitive) => Mathf.Min(px, MaxShiftPx) * (photosensitive ? 0.5f : 1f);
    }

    /// <summary>Raster-Pass nach Opaque (AfterRenderingSkybox): Liste aller „PTDistort“-Renderer → _PT_DistortBuffer (global für den Blit).</summary>
    public class PTDistortionAccumulatePass : ScriptableRenderPass
    {
        static readonly ShaderTagId Tag = new ShaderTagId("PTDistort");
        readonly ProfilingSampler _sampler = new ProfilingSampler("PT Verzerrung sammeln");
        class PassData { public RendererListHandle List; }

        public PTDistortionAccumulatePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cam = frameData.Get<UniversalCameraData>();
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var res = frameData.Get<UniversalResourceData>();
            var rd = frameData.Get<UniversalRenderingData>();
            var light = frameData.Get<UniversalLightData>();
            var desc = renderGraph.GetTextureDesc(res.activeColorTexture);
            desc.name = "_PT_DistortBuffer"; desc.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            desc.msaaSamples = MSAASamples.None; desc.depthBufferBits = DepthBits.None; desc.clearBuffer = true; desc.clearColor = Color.clear;
            var buffer = renderGraph.CreateTexture(desc);
            var ds = RenderingUtils.CreateDrawingSettings(Tag, rd, cam, light, SortingCriteria.CommonTransparent);
            var list = renderGraph.CreateRendererList(new RendererListParams(rd.cullResults, ds, new FilteringSettings(RenderQueueRange.all)));
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PT Verzerrung sammeln", out var data, _sampler))
            {
                data.List = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(buffer, 0, AccessFlags.Write);
                if (res.cameraDepthTexture.IsValid()) builder.UseTexture(res.cameraDepthTexture, AccessFlags.Read);
                builder.UseAllGlobalTextures(true);
                builder.SetGlobalTextureAfterPass(buffer, DistortionAccumulator.BufferId);
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.List));
            }
        }
    }
}
