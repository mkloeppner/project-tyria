using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>QA-Szene PT_VfxGallery: spielt die Komposition dieses Platzes alle <see cref="Period"/> s erneut ab (ohne Pool).</summary>
    public class VfxGalleryLooper : MonoBehaviour
    {
        public VfxInstance Instance;
        public float Period = 3f, Offset;
        /// <summary>Radius einer Kreisbahn für bewegungsabhängige Formen (Trail, Projectile); 0 = stehend.</summary>
        public float Orbit;
        float _t;

        void Start() { _t = -Offset; }

        void Update()
        {
            if (Instance == null) return;
            _t -= Time.deltaTime;
            if (Orbit > 0f)
            {
                float a = Time.time * 2.4f;
                Instance.transform.position = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Orbit;
            }
            if (_t > 0f) return;
            _t = Period;
            Instance.Play(null, Instance.transform.position, transform.rotation, null, 1f, 1f);
        }
    }
}
