using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Kreisender Vogelschwarm / schwebende Funken (Auslöser einer Entdeckungsschleife).</summary>
    public class Circler : MonoBehaviour
    {
        public float Radius = 12f, Speed = 0.6f, Bob = 2f;
        Vector3 _c; float _phase;
        void Awake() { _c = transform.localPosition; _phase = transform.GetSiblingIndex() * 1.1f; }
        void Update()
        {
            float t = Time.time * Speed + _phase;
            transform.localPosition = _c + new Vector3(Mathf.Cos(t) * Radius, Mathf.Sin(t * 1.7f) * Bob, Mathf.Sin(t) * Radius);
        }
    }
}
