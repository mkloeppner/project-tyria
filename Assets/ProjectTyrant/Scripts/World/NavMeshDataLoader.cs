using UnityEngine;
using UnityEngine.AI;

namespace ProjectTyrant
{
    /// <summary>Lädt das im Editor gebackene NavMesh der Zone zur Laufzeit.</summary>
    public class NavMeshDataLoader : MonoBehaviour
    {
        public NavMeshData Data;
        NavMeshDataInstance _instance;

        void OnEnable()
        {
            if (Data != null) _instance = NavMesh.AddNavMeshData(Data);
        }

        void OnDisable()
        {
            if (_instance.valid) NavMesh.RemoveNavMeshData(_instance);
        }
    }
}
