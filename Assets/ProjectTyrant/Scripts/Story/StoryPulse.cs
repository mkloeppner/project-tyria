using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: grünes Pulsen (Licht + Ring) an Story-Zielen.</summary>
    public class StoryPulse : MonoBehaviour
    {
        Light _light; float _t;
        static Material _mat;

        public static void Add(GameObject root, float height)
        {
            var go = new GameObject("StoryPulse"); go.transform.SetParent(root.transform, false); go.transform.localPosition = new Vector3(0, height, 0);
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = StoryDirector.StoryGreen; l.range = 6f; l.intensity = 2f; l.shadows = LightShadows.None;
            go.AddComponent<StoryPulse>()._light = l;
        }

        public static Material GlowMaterial(Color c)
        {
            if (_mat != null) return _mat;
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _mat = new Material(sh); var col = c; col.a = 0.55f;
            if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", col); else _mat.color = col;
            if (_mat.HasProperty("_Surface")) { _mat.SetFloat("_Surface", 1f); _mat.SetFloat("_Blend", 0f); _mat.SetOverrideTag("RenderType", "Transparent"); _mat.renderQueue = 3000; _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); _mat.SetInt("_ZWrite", 0); _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
            return _mat;
        }

        void Update() { _t += Time.deltaTime; if (_light != null) _light.intensity = 1.4f + Mathf.Sin(_t * 3f) * 0.8f; transform.localRotation = Quaternion.Euler(0, _t * 40f, 0); }
    }
}
