using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: sichtbare Grenze einer Story-Instanz (grüner Ring am Boden, Linienzug).</summary>
    public class StoryBoundary : MonoBehaviour
    {
        public static GameObject Create(Vector3 center, float radius)
        {
            var go = new GameObject("StoryBoundary"); go.transform.position = center;
            var lr = go.AddComponent<LineRenderer>(); lr.loop = true; lr.useWorldSpace = true; lr.widthMultiplier = 0.25f;
            lr.material = StoryPulse.GlowMaterial(StoryDirector.StoryGreen);
            const int n = 96; lr.positionCount = n;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; lr.SetPosition(i, ZoneAnchors.GroundAt(center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius + Vector3.up * 30f) + Vector3.up * 0.3f); }
            go.AddComponent<StoryBoundary>();
            return go;
        }
    }
}
