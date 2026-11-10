using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6 / M29: Requisiten einer Story-Instanz („feuer@-3,2“ relativ zum Schritt): Brand, Rauch, Barrikade, Zelt, Käfig, Kisten.
    /// Laufzeit-Primitive (keine Szenen-Handarbeit), verschwinden mit dem Verlassen der Instanz.
    /// </summary>
    public static class StoryProps
    {
        static Material _ember, _wood, _cloth, _iron, _smoke;

        static Material M(ref Material m, Color c, Color? emission = null)
        {
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
            return m;
        }

        static GameObject P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localEulerAngles = euler;
            g.GetComponent<Renderer>().sharedMaterial = m; return g;
        }

        public static GameObject Spawn(string spec, Vector3 origin)
        {
            var at = spec.Split('@'); string kind = at[0].Trim().ToLowerInvariant();
            Vector3 off = Vector3.zero; if (at.Length > 1) { var c = at[1].Split(','); if (c.Length >= 2) off = new Vector3(F(c[0]), 0f, F(c[1])); }
            var root = new GameObject("StoryProp_" + kind).transform;
            root.position = ZoneAnchors.GroundAt(origin + off + Vector3.up * 3f);
            root.rotation = Quaternion.Euler(0f, (spec.GetHashCode() & 0xff) * 1.4f, 0f);
            var ember = M(ref _ember, new Color(0.75f, 0.2f, 0.05f), new Color(1f, 0.32f, 0.05f) * 1.1f);
            var wood = M(ref _wood, new Color(0.38f, 0.26f, 0.16f));
            var cloth = M(ref _cloth, new Color(0.55f, 0.45f, 0.32f));
            var iron = M(ref _iron, new Color(0.25f, 0.25f, 0.27f));
            var smoke = M(ref _smoke, new Color(0.3f, 0.3f, 0.32f, 0.38f));
            if (smoke.HasProperty("_Surface") && smoke.GetFloat("_Surface") < 0.5f)   // Rauch halbtransparent
            { smoke.SetFloat("_Surface", 1f); smoke.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); smoke.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); smoke.SetFloat("_ZWrite", 0f); smoke.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); smoke.renderQueue = 3000; }
            switch (kind)
            {
                case "feuer":
                    P(root, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.05f, 2.2f), ember);
                    for (int i = 0; i < 3; i++) P(root, PrimitiveType.Cylinder, new Vector3(-0.5f + i * 0.5f, 0.25f, (i - 1) * 0.3f), new Vector3(0.15f, 0.7f, 0.15f), wood, new Vector3(0, i * 50f, 80f));
                    for (int i = 0; i < 3; i++) P(root, PrimitiveType.Sphere, new Vector3((i - 1) * 0.4f, 0.6f + i * 0.25f, 0), new Vector3(0.5f, 0.9f, 0.5f) * (1f - i * 0.2f), ember);
                    Light(root, 7f, 2.4f);
                    break;
                case "rauch":
                    for (int i = 0; i < 5; i++) P(root, PrimitiveType.Sphere, new Vector3(Mathf.Sin(i) * 0.4f, 1f + i * 0.9f, Mathf.Cos(i) * 0.3f), Vector3.one * (0.9f + i * 0.35f), smoke);
                    break;
                case "barrikade":
                    for (int i = 0; i < 5; i++) P(root, PrimitiveType.Cylinder, new Vector3(-1.6f + i * 0.8f, 0.7f, 0), new Vector3(0.18f, 0.9f, 0.18f), wood, new Vector3(18f * ((i % 2) * 2 - 1), 0, 0));
                    P(root, PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(3.6f, 0.15f, 0.15f), wood);
                    break;
                case "zelt":
                    P(root, PrimitiveType.Cube, new Vector3(0, 1.1f, 0), new Vector3(2.6f, 0.08f, 2.4f), cloth, new Vector3(0, 0, 38f));
                    P(root, PrimitiveType.Cube, new Vector3(0, 1.1f, 0), new Vector3(2.6f, 0.08f, 2.4f), cloth, new Vector3(0, 0, -38f));
                    break;
                case "kaefig":
                    for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; P(root, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.9f, 0.9f, Mathf.Sin(a) * 0.9f), new Vector3(0.06f, 0.9f, 0.06f), iron); }
                    P(root, PrimitiveType.Cylinder, new Vector3(0, 1.85f, 0), new Vector3(2f, 0.05f, 2f), iron);
                    break;
                default:   // kisten
                    for (int i = 0; i < 3; i++) P(root, PrimitiveType.Cube, new Vector3((i - 1) * 0.75f, 0.35f + (i == 1 ? 0.7f : 0f), 0), Vector3.one * 0.7f, wood, new Vector3(0, i * 15f, 0));
                    break;
            }
            return root.gameObject;
        }

        static void Light(Transform root, float range, float intensity)
        {
            var g = new GameObject("Glow"); g.transform.SetParent(root, false); g.transform.localPosition = Vector3.up * 1.2f;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = new Color(1f, 0.55f, 0.2f); l.shadows = LightShadows.None;
            g.AddComponent<FlickerLight>();
        }

        static float F(string s) => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
}
