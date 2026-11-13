using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>VFX-Lauf V2+: Laufzeit-Daten der Element-Baukästen (gebaut von ElementSimBuilder, verlinkt aus der GraphicsLibrary).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/VFX/Element FX Library")]
    public class ElementFxLibrary : ScriptableObject
    {
        [System.Serializable] public class FlipbookEntry { public string Id; public Material Material; public bool Loop; public float Fps = 48f; public float PeakTime; }
        [System.Serializable] public class MaterialEntry { public string Element; public string Key; public Material Material; }
        [System.Serializable] public class GpuPreset { public string Id; public GpuParticleSettings Settings = new GpuParticleSettings(); public float Duration = 1f; }

        public List<FlipbookEntry> Flipbooks = new List<FlipbookEntry>();
        [Tooltip("Element + Schlüssel (Mesh, Pfuetze, Refract) → Material")] public List<MaterialEntry> Materials = new List<MaterialEntry>();
        public List<GpuPreset> GpuPresets = new List<GpuPreset>();
        public ComputeShader ParticleCompute;
        public Material ParticleMaterial;

        public static ElementFxLibrary Current => GraphicsLibrary.Current != null ? GraphicsLibrary.Current.ElementFx : null;

        public FlipbookEntry Flipbook(string id) { foreach (var f in Flipbooks) if (f != null && f.Id == id) return f; return null; }
        public Material Mat(string element, string key)
        {
            foreach (var m in Materials) if (m != null && m.Element == element && m.Key == key) return m.Material;
            foreach (var m in Materials) if (m != null && m.Element == "" && m.Key == key) return m.Material;
            return null;
        }
        public GpuPreset Gpu(string id) { foreach (var g in GpuPresets) if (g != null && g.Id == id) return g; return null; }
    }
}
