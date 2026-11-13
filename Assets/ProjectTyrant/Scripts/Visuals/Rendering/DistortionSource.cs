using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Komponente an jedem brechenden Effekt: meldet ihn beim Puffer an, solange er aktiv ist.</summary>
    public class DistortionSource : MonoBehaviour
    {
        void OnEnable() => DistortionAccumulator.Register(this);
        void OnDisable() => DistortionAccumulator.Unregister(this);
    }
}
