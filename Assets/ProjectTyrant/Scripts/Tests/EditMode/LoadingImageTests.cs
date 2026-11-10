using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectTyrant.EditorTools;

namespace ProjectTyrant.Tests
{
    /// <summary>M27: jede Zone hat eine Ladebildschirm-Illustration (Builder-Aufnahme vom Aussichtspunkt) – Größe stimmt, Bild ist nicht einfarbig.</summary>
    public class LoadingImageTests
    {
        [Test]
        public void EveryZone_HasLoadingIllustration_WithContent()
        {
            var world = WorldBuilder.LoadWorld(); Assert.IsNotNull(world);
            foreach (var z in world.Zones)
            {
                Assert.IsNotNull(z.LoadingImage, z.Id + ": Ladebild fehlt");
                Assert.AreEqual(LoadingImageBuilder.Width, z.LoadingImage.width, z.Id);
                Assert.AreEqual(LoadingImageBuilder.Height, z.LoadingImage.height, z.Id);
                string path = AssetDatabase.GetAssetPath(z.LoadingImage);
                StringAssert.StartsWith(LoadingImageBuilder.Dir, path, "Spiel-Asset nur unter Data/Zones/Loading");
                var png = new Texture2D(2, 2); png.LoadImage(System.IO.File.ReadAllBytes(path));
                // Inhalt statt Leerbild: Helligkeits-Streuung über ein 16×9-Raster
                float sum = 0f, sq = 0f; int n = 0;
                for (int y = 0; y < 9; y++) for (int x = 0; x < 16; x++)
                {
                    var c = png.GetPixel(x * png.width / 16 + 4, y * png.height / 9 + 4); float l = c.grayscale; sum += l; sq += l * l; n++;
                }
                float mean = sum / n, sd = Mathf.Sqrt(Mathf.Max(0f, sq / n - mean * mean));
                Assert.Greater(sd, 0.03f, z.Id + ": Bild fast einfarbig");
                Assert.That(mean, Is.InRange(0.08f, 0.92f), z.Id + ": Bild zu dunkel/hell");
                Object.DestroyImmediate(png);
            }
        }
    }
}
