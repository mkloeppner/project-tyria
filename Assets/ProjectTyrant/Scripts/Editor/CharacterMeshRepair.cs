using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Reparatur der geskinnten MeshKit-Charaktere (Gegner, NPCs, Reittiere, Begleiter): Bis zur Korrektur von <see cref="SkinMesh.Basis"/>
    /// entstanden Röhren und Kegel mit nach links zeigender Seitenachse (linke Gliedmaßen, Hörner, Schwanzteile) mit umgekehrter Dreiecks-Richtung.
    /// Sie zeigten nach innen und wurden vom Back-Face-Culling verworfen → Löcher im Körper. Die Reparatur dreht je zusammenhängendem Teil,
    /// das überwiegend nach innen zeigt, die Dreiecke um und kehrt die Normalen; Skelett, Gewichte und Bind-Posen bleiben unverändert.
    /// Rebuild Everything erzeugt mit der korrigierten Basis direkt richtige Meshes. Kein Sammel-Menü: auf neuen Meshes würde die Heuristik
    /// gewölbte doppelseitige Flächen (Umhänge) fälschlich drehen; <see cref="Repair"/> ist nur für einzelne Alt-Meshes gedacht.
    /// </summary>
    public static class CharacterMeshRepair
    {
        public const string Dir = "Assets/ProjectTyrant/Procedural/Meshes/Characters";

        /// <summary>Teile (zusammenhängende Vertex-Gruppen), die überwiegend nach innen zeigen. Flache Teile (Umhang, Flügelhaut) werden ausgelassen.</summary>
        public static List<int[]> InwardParts(Vector3[] v, int[] tris)
        {
            int n = v.Length; var parent = new int[n]; for (int i = 0; i < n; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            for (int i = 0; i + 2 < tris.Length; i += 3) { int a = Find(tris[i]); parent[Find(tris[i + 1])] = a; parent[Find(tris[i + 2])] = a; }
            var sum = new Dictionary<int, Vector3>(); var cnt = new Dictionary<int, int>(); var mn = new Dictionary<int, Vector3>(); var mx = new Dictionary<int, Vector3>();
            for (int i = 0; i < n; i++)
            {
                int r = Find(i);
                if (!cnt.ContainsKey(r)) { sum[r] = Vector3.zero; cnt[r] = 0; mn[r] = v[i]; mx[r] = v[i]; }
                sum[r] += v[i]; cnt[r]++; mn[r] = Vector3.Min(mn[r], v[i]); mx[r] = Vector3.Max(mx[r], v[i]);
            }
            var score = new Dictionary<int, float>(); var abs = new Dictionary<int, float>(); var triOf = new Dictionary<int, List<int>>();
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int r = Find(tris[i]); var c = sum[r] / cnt[r];
                var fn = Vector3.Cross(v[tris[i + 1]] - v[tris[i]], v[tris[i + 2]] - v[tris[i]]);
                var d = (v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f - c;
                float s = d.sqrMagnitude < 1e-12f ? 0f : Vector3.Dot(fn, d.normalized);
                if (!score.ContainsKey(r)) { score[r] = 0f; abs[r] = 0f; triOf[r] = new List<int>(); }
                score[r] += s; abs[r] += Mathf.Abs(s); triOf[r].Add(i);
            }
            var list = new List<int[]>();
            foreach (var kv in score)
            {
                var ext = mx[kv.Key] - mn[kv.Key];
                float lo = Mathf.Min(ext.x, Mathf.Min(ext.y, ext.z)), hi = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
                if (hi <= 0f || lo < hi * 0.05f || cnt[kv.Key] < 5) continue;   // flach (Fläche, einzelne Kasten-Seite – auch gedreht): kein Innen/Außen
                if (kv.Value < -0.5f * abs[kv.Key]) list.Add(triOf[kv.Key].ToArray());
            }
            return list;
        }

        /// <summary>Dreht die Dreiecke der Teile um (je Untermesh) und kehrt die Normalen ihrer Vertices. Liefert die Anzahl gedrehter Teile.</summary>
        public static int Repair(Mesh m)
        {
            if (m == null || !m.isReadable) return 0;
            var v = m.vertices; var all = new List<int>(); var subStart = new List<int>();
            for (int s = 0; s < m.subMeshCount; s++) { subStart.Add(all.Count); all.AddRange(m.GetTriangles(s)); }
            var tris = all.ToArray();
            var parts = InwardParts(v, tris); if (parts.Count == 0) return 0;
            var normals = m.normals; bool hasN = normals != null && normals.Length == v.Length; var flipped = new HashSet<int>();
            foreach (var p in parts)
                foreach (int i in p)
                {
                    int t = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = t;
                    flipped.Add(tris[i]); flipped.Add(tris[i + 1]); flipped.Add(tris[i + 2]);
                }
            if (hasN) foreach (int i in flipped) normals[i] = -normals[i];
            for (int s = 0; s < m.subMeshCount; s++)
            {
                int start = subStart[s], end = s + 1 < subStart.Count ? subStart[s + 1] : tris.Length;
                var sub = new int[end - start]; System.Array.Copy(tris, start, sub, 0, sub.Length);
                m.SetTriangles(sub, s, false);
            }
            if (hasN) m.normals = normals;
            return parts.Count;
        }

        /// <summary>Prüfung (ohne Änderung): Anzahl geskinnter Meshes mit nach innen zeigenden Teilen. Gewölbte doppelseitige Flächen (Umhänge) zählen hier als Fehlalarm mit.</summary>
        public static int CountInward(out int parts)
        {
            int meshes = 0; parts = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Mesh", new[] { Dir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g));
                if (m == null || !m.isReadable || m.bindposes.Length == 0) continue;
                var all = new List<int>(); for (int s = 0; s < m.subMeshCount; s++) all.AddRange(m.GetTriangles(s));
                int n = InwardParts(m.vertices, all.ToArray()).Count; if (n > 0) { meshes++; parts += n; }
            }
            return meshes;
        }
    }
}
