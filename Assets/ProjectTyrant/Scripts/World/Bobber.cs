using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Langsames Schweben/Rotieren (Wegmarke, Brazier-Funken).</summary>
    public class Bobber : MonoBehaviour
    {
        public float Amplitude = 0.25f, Speed = 1.5f, Spin = 30f;
        Vector3 _base;
        void Awake() => _base = transform.localPosition;
        void Update()
        {
            transform.localPosition = _base + Vector3.up * Mathf.Sin(Time.time * Speed) * Amplitude;
            transform.Rotate(0f, Spin * Time.deltaTime, 0f, Space.World);
        }
    }
}
