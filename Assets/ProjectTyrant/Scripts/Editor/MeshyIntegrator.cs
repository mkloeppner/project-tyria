using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Normalisiert importierte Meshy-Modelle: Höhe, Pivot auf Bodenebene, Toon-Material mit Albedo, Prefab.</summary>
    public static class MeshyIntegrator
    {
        public const string MeshyDir = PrefabDir + "/Meshy";

        /// <summary>Gibt das normalisierte Prefab zurück (oder null, falls kein Modell vorhanden ist).</summary>
        public static GameObject GetOrBuild(string modelName, float targetHeight) => GetOrBuild(modelName, targetHeight, false, 0f, false);

        /// <param name="alignLongestAxis">Längste Achse auf lokales Y drehen (Waffen).</param>
        /// <param name="pivotFraction">Pivot-Höhe als Anteil der Gesamthöhe (0 = Boden, z. B. 0,2 = Griff).</param>
        /// <param name="addCollider">BoxCollider aus den Bounds ergänzen (Gebäude).</param>
        public static GameObject GetOrBuild(string modelName, float targetHeight, bool alignLongestAxis, float pivotFraction, bool addCollider) => GetOrBuild(modelName, targetHeight, alignLongestAxis, pivotFraction, addCollider, 0);

        /// <param name="widestEnd">−1: breiteste Stelle (Parierstange) muss unten liegen; +1: oben (Stabkopf); 0: egal. Sonst 180° drehen.</param>
        public static GameObject GetOrBuild(string modelName, float targetHeight, bool alignLongestAxis, float pivotFraction, bool addCollider, int widestEnd)
        {
            string fbx = MeshyDir + "/" + modelName + ".fbx";
            string prefabPath = MeshyDir + "/" + modelName + "_Normalized.prefab";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) return null;

            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            Quaternion align = Quaternion.identity;
            if (alignLongestAxis)
            {
                var sz = MeasureHeightVector(model);
                if (sz.x >= sz.y && sz.x >= sz.z) align = Quaternion.Euler(0, 0, 90);
                else if (sz.z >= sz.y && sz.z >= sz.x) align = Quaternion.Euler(90, 0, 0);
            }
            if (widestEnd != 0 && WidestInUpperHalf(model, align) != (widestEnd > 0)) align = Quaternion.Euler(0, 0, 180) * align;
            float h = MeasureHeight(model, align);
            if (Mathf.Abs(h - targetHeight) > 0.05f && h > 0.0001f)
            {
                imp.globalScale = imp.globalScale * targetHeight / h;
                imp.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            }
            string texDir = MeshyDir + "/Textures";
            EnsureFolder(texDir);
            Texture2D albedo = null;
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains(modelName)) { albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(p); break; }
            }
            if (albedo == null)
            {
                string sub = texDir + "/" + modelName;
                EnsureFolder(sub);
                imp.ExtractTextures(sub);
                AssetDatabase.Refresh();
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { sub })) { albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)); break; }
            }

            var mat = Mat("Meshy_" + modelName, Color.white, 0.15f, null, 0.5f);
            mat.SetTexture("_AlbedoMap", albedo);
            mat.SetFloat("_UseVertexColor", 0f);
            EditorUtility.SetDirty(mat);

            var root = new GameObject(modelName + "_Normalized");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(root.transform, false);
            inst.transform.localRotation = align * model.transform.localRotation;
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            var b = Bounds(inst);
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y - b.size.y * pivotFraction, -b.center.z);
            if (addCollider)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = new Vector3(0, b.size.y * (0.5f - pivotFraction), 0);
                bc.size = new Vector3(b.size.x * 0.7f, b.size.y, b.size.z * 0.7f);
            }
            var marker = root.AddComponent<PlaceholderMarker>();
            marker.AssetId = modelName; marker.ReplacedByMeshy = true; marker.MeshyPrompt = "(Meshy-generiert)";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Liegt die breiteste Querschnitts-Stelle (10 Höhenscheiben) in der oberen Hälfte?</summary>
        public static bool WidestInUpperHalf(GameObject model, Quaternion align)
        {
            var holder = new GameObject("measure");
            var inst = (GameObject)Object.Instantiate(model, holder.transform);
            inst.transform.localRotation = align * model.transform.localRotation;
            float minY = float.MaxValue, maxY = float.MinValue;
            var pts = new System.Collections.Generic.List<Vector3>();
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                foreach (var v in mf.sharedMesh.vertices) { var w = holder.transform.InverseTransformPoint(mf.transform.TransformPoint(v)); pts.Add(w); minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y); }
            Object.DestroyImmediate(holder);
            const int bins = 10;
            var lo = new Vector2[bins]; var hi = new Vector2[bins];
            for (int b = 0; b < bins; b++) { lo[b] = new Vector2(float.MaxValue, float.MaxValue); hi[b] = new Vector2(float.MinValue, float.MinValue); }
            foreach (var p in pts)
            {
                int b = Mathf.Clamp((int)((p.y - minY) / Mathf.Max(0.0001f, maxY - minY) * bins), 0, bins - 1);
                lo[b] = Vector2.Min(lo[b], new Vector2(p.x, p.z)); hi[b] = Vector2.Max(hi[b], new Vector2(p.x, p.z));
            }
            int best = 0; float bestW = -1f;
            for (int b = 1; b < bins - 1; b++) { var d = hi[b] - lo[b]; float w = Mathf.Max(d.x, d.y); if (w > bestW) { bestW = w; best = b; } }
            return best >= bins / 2;
        }

        static float MeasureHeight(GameObject model, Quaternion align)
        {
            var holder = new GameObject("measure");
            var inst = (GameObject)Object.Instantiate(model, holder.transform);
            inst.transform.localRotation = align * model.transform.localRotation;
            var b = Bounds(inst);
            Object.DestroyImmediate(holder);
            return b.size.y;
        }

        static Vector3 MeasureHeightVector(GameObject model)
        {
            var inst = (GameObject)Object.Instantiate(model);
            inst.transform.localRotation = model.transform.localRotation;
            var b = Bounds(inst);
            Object.DestroyImmediate(inst);
            return b.size;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
