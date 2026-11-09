using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Gemeinsame Helfer für die Builder (Materials, Primitive-Komposition, Ordner).</summary>
    public static class PTEditorUtil
    {
        public const string Root = "Assets/ProjectTyrant";
        public const string MatDir = Root + "/Materials";
        public const string ProcDir = Root + "/Procedural";
        public const string PrefabDir = Root + "/Prefabs";
        public const string DataDir = Root + "/Data";
        public const int CharacterLayer = 8;

        static Texture2D _painterly;

        /// <summary>Komponente holen oder anlegen (Unity-Null-sicher, statt „??“).</summary>
        public static T GetOrAdd<T>(GameObject g) where T : Component { var c = g.GetComponent<T>(); return c != null ? c : g.AddComponent<T>(); }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Texture2D PainterlyTexture()
        {
            if (_painterly != null) return _painterly;
            string path = ProcDir + "/T_Painterly.png";
            _painterly = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (_painterly != null) return _painterly;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var rnd = new System.Random(1337);
            float ox = (float)rnd.NextDouble() * 100f, oy = (float)rnd.NextDouble() * 100f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                // gestreckte Pinselstriche + Grundrauschen, kachelbar über Sinus-Torus
                float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f;
                float n1 = Mathf.PerlinNoise(ox + Mathf.Cos(a) * 1.5f + Mathf.Sin(b) * 0.3f, oy + Mathf.Sin(a) * 1.5f + Mathf.Cos(b) * 4f);
                float n2 = Mathf.PerlinNoise(ox + Mathf.Cos(a) * 6f, oy + Mathf.Sin(b) * 6f + Mathf.Sin(a) * 2f);
                float val = Mathf.Clamp01(n1 * 0.65f + n2 * 0.35f);
                val = Mathf.Round(val * 6f) / 6f; // gestufte Tonwerte = gemalter Look
                tex.SetPixel(x, y, new Color(val, val, val, 1f));
            }
            tex.Apply();
            EnsureFolder(ProcDir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat; imp.sRGBTexture = false; imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            _painterly = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return _painterly;
        }

        public static Material Mat(string name, Color color, float paint = 0.25f, Color? emission = null, float rim = 0.6f)
        {
            EnsureFolder(MatDir);
            string path = MatDir + "/M_" + name + ".mat";
            var shader = Shader.Find("ProjectTyrant/ToonRim");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", PainterlyTexture());
            m.SetFloat("_PaintStrength", paint);
            m.SetColor("_ShadowColor", new Color(0.50f, 0.55f, 0.85f));
            m.SetColor("_RimColor", new Color(1f, 0.88f, 0.65f));
            m.SetFloat("_RimStrength", rim);
            m.SetColor("_EmissionColor", emission ?? Color.black);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Lauf 5: Effekt-Material aus einem HLSL-Shader unter Shaders/FX (Farbe in _Color bzw. _OutlineColor).</summary>
        public static Material FxMat(string name, string shaderName, Color color)
        {
            EnsureFolder(MatDir);
            string path = MatDir + "/M_FX_" + name + ".mat";
            var shader = Shader.Find(shaderName);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (shader != null) m.shader = shader;
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material MarkerMat(string name, Color color)
        {
            EnsureFolder(MatDir);
            string path = MatDir + "/M_" + name + ".mat";
            var shader = Shader.Find("ProjectTyrant/GroundMarker");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_Color", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, string name = null, Vector3? euler = null, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name ?? type.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.layer = parent != null ? parent.gameObject.layer : 0;
            return go;
        }

        public static GameObject Child(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.layer = parent != null ? parent.gameObject.layer : 0;
            return go;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public static void AddPlaceholder(GameObject go, string id, string prompt)
        {
            var m = go.GetComponent<PlaceholderMarker>();
            if (m == null) m = go.AddComponent<PlaceholderMarker>();
            m.AssetId = id; m.MeshyPrompt = prompt;
        }

        public static GameObject SavePrefab(GameObject go, string name)
        {
            EnsureFolder(PrefabDir);
            string path = PrefabDir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            return prefab;
        }
    }
}
