using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Richtet Namensschilder zur Kamera aus.</summary>
    public class Billboard : MonoBehaviour
    {
        Camera _cam;
        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null) transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
        }
    }
}
