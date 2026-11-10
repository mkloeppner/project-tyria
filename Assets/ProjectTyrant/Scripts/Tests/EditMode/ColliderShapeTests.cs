using NUnit.Framework;
using UnityEngine;
using ProjectTyrant.EditorTools;

namespace ProjectTyrant.Tests
{
    /// <summary>Fix nach Lauf 11: flache Zylinder (Plätze, Brunnen) dürfen keine unsichtbare Kuppel als Collider tragen.</summary>
    public class ColliderShapeTests
    {
        [Test]
        public void FlatCylinderPart_ColliderMatchesVisibleDisc()
        {
            var root = new GameObject("ColliderShapeTest");
            try
            {
                var plaza = PTEditorUtil.Part(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(48f, 0.08f, 48f), null, "Plaza", null, true);
                Assert.IsNull(plaza.GetComponent<CapsuleCollider>(), "keine Kapsel (würde zur 48-m-Kugel)");
                var mc = plaza.GetComponent<MeshCollider>();
                Assert.IsNotNull(mc, "Mesh-Collider in Zylinderform");
                Assert.IsFalse(mc.convex);
                Physics.SyncTransforms();
                Assert.Less(mc.bounds.size.y, 0.5f, "Höhe der Scheibe, nicht ihr Durchmesser");
                Assert.AreEqual(48f, mc.bounds.size.x, 0.6f, "Breite unverändert begehbar");
                // Über der Scheibe stehen: ein Strahl von 3 m Höhe trifft die Oberfläche knapp über 0, nicht eine Kuppel bei 24 m
                Assert.IsTrue(mc.Raycast(new Ray(new Vector3(10f, 30f, 0f), Vector3.down), out var hit, 100f));
                Assert.Less(hit.point.y, 0.5f, "Oberfläche flach");

                var noCol = PTEditorUtil.Part(root.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.one, null, "Deko");
                Assert.IsNull(noCol.GetComponent<Collider>(), "ohne keepCollider weiterhin kein Collider");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
