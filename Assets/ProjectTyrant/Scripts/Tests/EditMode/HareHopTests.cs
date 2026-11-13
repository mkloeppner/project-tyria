using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Anim;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Spieler-Wunsch 2026-11-12: Hasen (Springer-Reittier, Schneehase) sitzen aufrecht und bewegen sich in kleinen Sprüngen vorwärts.</summary>
    public class HareHopTests
    {
        static CreatureProfile P(string n) { foreach (var p in ClipLibrary.Creatures) if (p.Name == n) return p; throw new System.Exception(n); }
        static ClipData Clip(CreatureProfile pr, string n) { foreach (var c in ClipLibrary.Creature(pr, true)) if (c.Name == pr.Name + "_" + n) return c; throw new System.Exception(n); }

        static float[] FootHeights(SkeletonDef sk, ClipData c, int f)
        {
            var pos = sk.WorldPositions(c.Euler[f], c.Root[f]); var r = new float[4];
            for (int i = 0; i < 4; i++) r[i] = pos[sk.IndexOf(SkeletonDef.Legs[i] + "_Foot")].y;
            return r;
        }

        [TestCase("Springer")] [TestCase("Hare")]
        public void UsesHopGait(string n) => Assert.AreEqual(CreatureProfile.GaitKind.Hop, P(n).Gait);

        [TestCase("Springer")] [TestCase("Hare")]
        public void Idle_SitsUpright_AllFourPawsOnTheGround(string n)
        {
            var pr = P(n); var sk = pr.Skeleton(); var c = Clip(pr, "Idle");
            float rest = sk.WorldPositions(null, Vector3.zero)[sk.IndexOf("BL_Foot")].y;
            var rot = new Quaternion[sk.Count]; var pos = sk.WorldPositions(c.Euler[0], c.Root[0], rot);
            var feet = FootHeights(sk, c, 0);
            Assert.AreEqual(rest, Mathf.Min(Mathf.Min(feet[2], feet[3]), Mathf.Min(feet[0], feet[1])), 0.03f, "tiefster Fuß auf dem Boden");
            Assert.AreEqual(rest, feet[2], 0.06f, "Hinterläufe auf dem Boden"); Assert.AreEqual(rest, feet[0], 0.12f, "Vorderpfoten am Boden");
            float chestAbovePelvis = pos[sk.IndexOf(SkeletonDef.Chest)].y - pos[sk.IndexOf(SkeletonDef.Pelvis)].y;
            Assert.Greater(chestAbovePelvis, pr.Length * 0.2f, "Oberkörper aufgerichtet (sitzend)"); Assert.That(ClipLibrary.SitPitch(pr), Is.InRange(10f, 40f));
            Assert.Less(pos[sk.IndexOf(SkeletonDef.Pelvis)].y, sk.WorldPositions(null, Vector3.zero)[sk.IndexOf(SkeletonDef.Pelvis)].y - pr.LegHeight * 0.3f, "Becken abgesenkt");
        }

        [TestCase("Springer", "Walk")] [TestCase("Springer", "Run")] [TestCase("Springer", "MRun")] [TestCase("Hare", "Walk")] [TestCase("Hare", "Run")]
        public void Locomotion_IsSmallForwardHops_NeverIntoTheGround(string n, string clip)
        {
            var pr = P(n); var sk = pr.Skeleton(); var c = Clip(pr, clip);
            float rest = sk.WorldPositions(null, Vector3.zero)[sk.IndexOf("BL_Foot")].y;
            float lowest = float.MaxValue, highestMin = float.MinValue;
            for (int f = 0; f < c.FrameCount; f++)
            {
                var h = FootHeights(sk, c, f); float mn = Mathf.Min(Mathf.Min(h[0], h[1]), Mathf.Min(h[2], h[3]));
                lowest = Mathf.Min(lowest, mn); highestMin = Mathf.Max(highestMin, mn);
            }
            Assert.GreaterOrEqual(lowest, rest - 0.02f, "kein Fuß im Boden");
            float sz = Mathf.Max(0.4f, pr.LegHeight);
            Assert.Greater(highestMin - rest, 0.08f * sz, "alle vier Pfoten in der Luft (Sprung)");
            Assert.Less(highestMin - rest, 0.45f * sz, "kleine Sprünge");
            Assert.Greater(c.MarkerTime(Marker.FootR), c.MarkerTime(Marker.FootL), "erst vorn, dann hinten landen");
        }

        [Test]
        public void OtherProfiles_Unchanged()
        {
            Assert.AreEqual(CreatureProfile.GaitKind.Legs, P("Quadruped").Gait);
            Assert.AreEqual(CreatureProfile.GaitKind.Biped, P("Raptor").Gait);
        }
    }
}
