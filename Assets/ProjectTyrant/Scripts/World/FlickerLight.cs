using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lagerfeuer-/Kohlebecken-Flackern.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        public float BaseIntensity = 2.5f;
        public float Amount = 0.6f;
        public float Speed = 7f;
        Light _l; float _seed;

        void Awake() { _l = GetComponent<Light>(); _seed = Random.value * 100f; }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * Speed);
            _l.intensity = BaseIntensity + (n - 0.5f) * 2f * Amount;
        }
    }
}
