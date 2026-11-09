using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectTyrant
{
    /// <summary>
    /// GPU-Instancing für Vegetation/Felsen großer Zonen: je Mesh-Typ Zellen (128-m-Chunks) mit Matrizen,
    /// Distanz- und Frustum-Culling je Zelle, Sichtweite je Asset-Klasse (Bäume 400 m, Felsen 300 m, Kleinkram 120 m).
    /// </summary>
    public class VegetationRenderer : MonoBehaviour
    {
        [Serializable] public class Cell { public Vector3 Center; public Matrix4x4[] Matrices = new Matrix4x4[0]; }
        [Serializable] public class Batch
        {
            public string Name;
            public Mesh Mesh;
            public Material Material;
            public float ViewDistance = 300f;
            public bool CastShadows = true;
            public List<Cell> Cells = new List<Cell>();
        }

        public List<Batch> Batches = new List<Batch>();
        public float CellSize = 128f;
        public float CellHeight = 80f;
        readonly Plane[] _planes = new Plane[6];
        public int LastDrawnInstances { get; private set; }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            var cp = cam.transform.position;
            int drawn = 0;
            foreach (var b in Batches)
            {
                if (b.Mesh == null || b.Material == null) continue;
                var rp = new RenderParams(b.Material) { shadowCastingMode = b.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows = true, layer = gameObject.layer };
                float maxD = b.ViewDistance + CellSize * 0.71f;
                foreach (var c in b.Cells)
                {
                    if (c.Matrices.Length == 0) continue;
                    float dx = c.Center.x - cp.x, dz = c.Center.z - cp.z;
                    if (dx * dx + dz * dz > maxD * maxD) continue;
                    if (!GeometryUtility.TestPlanesAABB(_planes, new Bounds(c.Center, new Vector3(CellSize, CellHeight, CellSize)))) continue;
                    for (int start = 0; start < c.Matrices.Length; start += 1023)
                    {
                        int n = Mathf.Min(1023, c.Matrices.Length - start);
                        Graphics.RenderMeshInstanced(rp, b.Mesh, 0, c.Matrices, n, start);
                        drawn += n;
                    }
                }
            }
            LastDrawnInstances = drawn;
        }

        public int TotalInstances { get { int n = 0; foreach (var b in Batches) foreach (var c in b.Cells) n += c.Matrices.Length; return n; } }
    }
}
