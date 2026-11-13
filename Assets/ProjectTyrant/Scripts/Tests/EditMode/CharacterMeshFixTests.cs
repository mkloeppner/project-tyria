using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Fix 2026-11-12: Röhren und Kegel der MeshKit-Charaktere zeigen in jeder Richtung nach außen (keine Löcher durch Back-Face-Culling).</summary>
    public class CharacterMeshFixTests
    {
        static IEnumerable<Vector3> Directions()
        {
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++) if (x != 0 || y != 0 || z != 0) yield return new Vector3(x, y, z);
            yield return new Vector3(-0.9f, -0.3f, 0.1f); yield return new Vector3(0.05f, -1f, 0.02f); yield return new Vector3(-0.2f, 0.1f, 0.97f);
        }

        [Test]
        public void Basis_IsRightHandedForEveryDirection()
        {
            foreach (var d in Directions())
            {
                SkinMesh.Basis(d, out var side, out var front);
                // Ring läuft von side nach front; Wände zeigen nach außen, wenn side × front = −dir (für jede Richtung gleich)
                Assert.Less(Vector3.Dot(Vector3.Cross(side, front), d.normalized), -0.5f, "gespiegelte Basis für " + d);
            }
        }

        static float OutwardShare(SkinMesh m, Vector3 a, Vector3 b)
        {
            var tris = m.Sub[0]; int outward = 0, n = 0; var axis = (b - a).normalized;
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 p0 = m.V[tris[i]], p1 = m.V[tris[i + 1]], p2 = m.V[tris[i + 2]];
                var fn = Vector3.Cross(p1 - p0, p2 - p0); if (fn.sqrMagnitude < 1e-12f) continue;
                var c = (p0 + p1 + p2) / 3f; float t = Vector3.Dot(c - a, axis); var onAxis = a + axis * Mathf.Clamp(t, 0f, (b - a).magnitude);
                float len = (b - a).magnitude;
                var outDir = t <= 1e-4f ? -axis : t >= len - 1e-4f ? axis : c - onAxis;   // Deckel/Kegelboden zeigen entlang der Achse
                n++; if (Vector3.Dot(fn, outDir) > 0f) outward++;
            }
            return n == 0 ? 0f : outward / (float)n;
        }

        [Test]
        public void Tube_FacesOutward_IncludingLeftSideLimbs()
        {
            foreach (var d in Directions())
            {
                var m = new SkinMesh(); var a = new Vector3(0.3f, 1f, 0.2f); var b = a + d.normalized * 0.5f;
                m.Tube(a, b, new Vector2(0.08f, 0.06f), new Vector2(0.06f, 0.05f), Color.white, 0);
                Assert.AreEqual(1f, OutwardShare(m, a, b), 1e-4f, "Röhre nach innen für Richtung " + d);
            }
        }

        [Test]
        public void Tube_WithFrontBias_StaysOutward()
        {
            var m = new SkinMesh(); var a = Vector3.up; var b = Vector3.up + new Vector3(-0.4f, -0.3f, 0f);
            m.Tube(a, b, new Vector2(0.1f, 0.08f), new Vector2(0.08f, 0.06f), Color.white, 0, -1, 8, 3, 0.08f, true, Vector3.back);
            Assert.AreEqual(1f, OutwardShare(m, a, b), 1e-4f);
            foreach (var p in m.V) Assert.IsFalse(float.IsNaN(p.x), "NaN durch aufgehobene Vorderachse");
        }

        [Test]
        public void Cone_FacesOutward_LeftHorn()
        {
            var m = new SkinMesh(); var basePos = new Vector3(-0.08f, 1.7f, 0f); var dir = new Vector3(-0.08f, 0.16f, -0.02f);
            m.Cone(basePos, dir, 0.03f, Color.white, 0);
            Assert.AreEqual(1f, OutwardShare(m, basePos, basePos + dir), 1e-4f);
        }

        static float OutwardShareCentered(SkinMesh m, Vector3 center)
        {
            var tris = m.Sub[0]; int outward = 0, n = 0;
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 p0 = m.V[tris[i]], p1 = m.V[tris[i + 1]], p2 = m.V[tris[i + 2]];
                var fn = Vector3.Cross(p1 - p0, p2 - p0); if (fn.sqrMagnitude < 1e-12f) continue;
                n++; if (Vector3.Dot(fn, (p0 + p1 + p2) / 3f - center) > 0f) outward++;
            }
            return n == 0 ? 0f : outward / (float)n;
        }

        [Test]
        public void Box_FacesOutward()
        {
            var m = new SkinMesh(); var c = new Vector3(0.2f, 1f, -0.1f);
            m.Box(c, new Vector3(0.1f, 0.08f, 0.2f), Color.white, 0);
            Assert.AreEqual(1f, OutwardShareCentered(m, c), 1e-4f);
            var r = new SkinMesh(); r.Box(c, new Vector3(0.1f, 0.08f, 0.2f), Color.white, 0, -1, Quaternion.Euler(20, 35, -10));
            Assert.AreEqual(1f, OutwardShareCentered(r, c), 1e-4f, "gedreht");
        }

        [Test]
        public void Ellipsoid_FacesOutward()
        {
            var m = new SkinMesh(); var c = new Vector3(0f, 1.5f, 0f);
            m.Ellipsoid(c, new Vector3(0.1f, 0.12f, 0.09f), Color.white, 0);
            Assert.AreEqual(1f, OutwardShareCentered(m, c), 1e-4f);
        }

        static Mesh TubeMesh(bool inverted)
        {
            var m = new SkinMesh(); m.Tube(Vector3.zero, Vector3.up, Vector2.one * 0.2f, Vector2.one * 0.2f, Color.white, 0);
            var tris = m.Sub[0].ToArray();
            if (inverted) for (int i = 0; i < tris.Length; i += 3) { int t = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = t; }
            var mesh = new Mesh(); mesh.SetVertices(m.V); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals();
            return mesh;
        }

        [Test]
        public void Repair_FlipsInwardPartAndNormals_LeavesCorrectPartAlone()
        {
            var bad = TubeMesh(true); var good = TubeMesh(false);
            Assert.AreEqual(1, CharacterMeshRepair.InwardParts(bad.vertices, bad.triangles).Count);
            Assert.AreEqual(0, CharacterMeshRepair.InwardParts(good.vertices, good.triangles).Count);
            var nBefore = bad.normals[0];
            Assert.AreEqual(1, CharacterMeshRepair.Repair(bad));
            Assert.AreEqual(0, CharacterMeshRepair.InwardParts(bad.vertices, bad.triangles).Count, "nach der Reparatur nach außen");
            Assert.AreEqual(-nBefore, bad.normals[0]);
            Assert.AreEqual(0, CharacterMeshRepair.Repair(good));
            Object.DestroyImmediate(bad); Object.DestroyImmediate(good);
        }

        [Test]
        public void Repair_IgnoresFlatSheets()
        {
            var v = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            Assert.AreEqual(0, CharacterMeshRepair.InwardParts(v, new[] { 0, 2, 1, 0, 3, 2 }).Count);
        }
    }
}
