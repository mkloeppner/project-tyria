using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Klang als Wegweiser: spielt den Clip alle <see cref="Interval"/> s (Glocken zur „Stunde“, Windorgel, Vogelrufe).</summary>
    public class PeriodicSound : MonoBehaviour
    {
        public float Interval = 45f, Jitter = 8f;
        AudioSource _src; float _next;
        void Awake() { _src = GetComponent<AudioSource>(); _next = Time.time + Random.Range(2f, Interval); }
        void Update()
        {
            if (_src == null || _src.clip == null || Time.time < _next) return;
            _next = Time.time + Interval + Random.Range(-Jitter, Jitter);
            _src.Play();
        }
    }
}
