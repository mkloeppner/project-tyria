using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Portal-Culling: Innen-Details nur drinnen, Außen-Zierrat nur draußen.</summary>
    public class InteriorCulling : MonoBehaviour
    {
        public Bounds LocalBounds;
        public Renderer[] InteriorOnly = new Renderer[0];
        public Renderer[] ExteriorOnly = new Renderer[0];
        public Light[] InteriorLights = new Light[0];
        int _state = -1;
        public bool Inside { get; private set; }

        void Update()
        {
            var gm = GameManager.Instance; var p = gm != null ? gm.Player : null;
            bool inside = p != null && InteriorCullingModel.Inside(LocalBounds, transform.InverseTransformPoint(p.transform.position));
            Apply(inside);
        }

        public void Apply(bool inside)
        {
            Inside = inside;
            int s = inside ? 1 : 0; if (s == _state) return; _state = s;
            InteriorCullingModel.State(inside, out bool vin, out bool vout);
            foreach (var r in InteriorOnly) if (r != null) r.enabled = vin;
            foreach (var r in ExteriorOnly) if (r != null) r.enabled = vout;
            foreach (var l in InteriorLights) if (l != null) l.enabled = vin;
        }
    }
}
