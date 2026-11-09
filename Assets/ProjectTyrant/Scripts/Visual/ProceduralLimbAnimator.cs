using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Einfache prozedurale Lauf-Animation für Primitive-Charaktere (Beine/Arme schwingen gegenphasig).</summary>
    public class ProceduralLimbAnimator : MonoBehaviour
    {
        public Transform Root;
        public float Amplitude = 28f;
        public float StepRate = 2.2f;
        Transform _legL, _legR, _armL, _armR;
        Quaternion _bLL, _bLR, _bAL, _bAR;
        Vector3 _last;
        float _phase, _weight;

        void Awake()
        {
            if (Root == null) Root = transform;
            _legL = Find("LegL"); _legR = Find("LegR"); _armL = Find("ArmL"); _armR = Find("ArmR");
            if (_legL) _bLL = _legL.localRotation; if (_legR) _bLR = _legR.localRotation;
            if (_armL) _bAL = _armL.localRotation; if (_armR) _bAR = _armR.localRotation;
            _last = transform.position;
        }

        Transform Find(string n)
        {
            foreach (var t in Root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
            return null;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 d = transform.position - _last; d.y = 0f;
            _last = transform.position;
            float speed = d.magnitude / dt;
            _weight = Mathf.MoveTowards(_weight, Mathf.Clamp01(speed / 4f), dt * 4f);
            _phase += speed * dt * StepRate;
            float a = Mathf.Sin(_phase) * Amplitude * _weight;
            if (_legL) _legL.localRotation = _bLL * Quaternion.Euler(a, 0, 0);
            if (_legR) _legR.localRotation = _bLR * Quaternion.Euler(-a, 0, 0);
            if (_armL) _armL.localRotation = _bAL * Quaternion.Euler(-a * 0.8f, 0, 0);
            if (_armR) _armR.localRotation = _bAR * Quaternion.Euler(a * 0.8f, 0, 0);
        }
    }
}
