using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Visuals;

namespace ProjectTyrant.Tests
{
    public class GraphicsG1PerfTests
    {
        [Test]
        public void FpsOf_ConstantFrames_AverageEqualsMinimum()
        {
            var dt = new List<float>(); for (int i = 0; i < 100; i++) dt.Add(1f / 60f);
            PerfProbe.FpsOf(dt, out float avg, out float min);
            Assert.AreEqual(60f, avg, 0.01f); Assert.AreEqual(60f, min, 0.01f);
        }

        [Test]
        public void FpsOf_OneHitch_LowersOnePercentLowNotAverageMuch()
        {
            var dt = new List<float>(); for (int i = 0; i < 99; i++) dt.Add(1f / 100f); dt.Add(0.1f);
            PerfProbe.FpsOf(dt, out float avg, out float min);
            Assert.AreEqual(10f, min, 0.01f, "schlechtestes 1-%-Bild = 100 ms");
            Assert.Greater(avg, 90f);
        }

        [Test]
        public void FpsOf_Empty_IsZero()
        {
            PerfProbe.FpsOf(new List<float>(), out float avg, out float min);
            Assert.AreEqual(0f, avg); Assert.AreEqual(0f, min);
        }

        [Test]
        public void SampleCsv_MatchesHeaderColumns()
        {
            var s = new PerfProbe.Sample { Zone = "sonnfurt", Name = "Kampf", AvgFps = 61.5f, MinFps = 48f, CpuMs = 9.1f, GpuMs = 7.25f, DrawCalls = 1200, SetPass = 210, Triangles = 900000, Particles = 3000 };
            Assert.AreEqual(PerfProbe.Sample.Header.Split(';').Length, s.ToCsv().Split(';').Length);
            StringAssert.Contains("61.5", s.ToCsv());
        }
    }
}
