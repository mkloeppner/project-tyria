using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Absturz 2026-11-12 (Charakterauswahl, Erstellen schließen): Vorschaufiguren mit Animator dürfen nie per SetActive(false) ausgeblendet werden.</summary>
    public class CharacterSelectFigureTests
    {
        static void Show(Transform fig, bool show)
        {
            var m = typeof(CharacterSelectController).GetMethod("ShowFigure", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "ShowFigure fehlt");
            m.Invoke(null, new object[] { fig, show });
        }

        [Test]
        public void HideFigure_KeepsGameObjectActive_DisablesRenderers()
        {
            var root = new GameObject("Fig");
            try
            {
                root.AddComponent<Animator>();
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.transform.SetParent(root.transform);
                var r = body.GetComponent<Renderer>();
                Show(root.transform, false);
                Assert.IsTrue(root.activeSelf, "Figur wurde deaktiviert (Animator-Absturz)");
                Assert.IsFalse(r.enabled);
                Show(root.transform, true);
                Assert.IsTrue(root.activeSelf);
                Assert.IsTrue(r.enabled);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ShowFigure_ActivatesStoredInactiveFigureOnce()
        {
            var root = new GameObject("Fig");
            try
            {
                root.SetActive(false);
                Show(root.transform, false);
                Assert.IsTrue(root.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
