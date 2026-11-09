using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Anim;
using ProjectTyrant.EditorTools;
using UnityEngine;
using SkinWeights = ProjectTyrant.Anim.SkinWeights;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M21: Skelett, Haut-Gewichte, Clip-Längen/Marker, Fußkontakt, keine NaN, Telegraph ≥ 0,6 s, Fortbewegungsmischung, Riss-Freiheit.</summary>
    public class AnimationTests
    {
        static List<ClipData> _human;
        static List<ClipData> Human => _human ?? (_human = ClipLibrary.Humanoid());
        static ClipData H(string n) { foreach (var c in Human) if (c.Name == n) return c; return null; }

        [Test]
        public void Skeletons_HaveRequiredBones_AndValidHierarchy()
        {
            var h = SkeletonDef.Humanoid();
            // Spezifikation zählt „20 Knochen“, die Aufzählung ergibt 6 + 2 × 8 = 22 – die Aufzählung gilt (DECISIONS M21)
            Assert.AreEqual(22, h.Count, "Humanoid: Hüfte, Wirbelsäule ×3, Hals, Kopf, je Seite Schulter/Oberarm/Unterarm/Hand + Oberschenkel/Unterschenkel/Fuß/Zehen");
            foreach (var n in new[] { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "ShoulderL", "UpperArmL", "LowerArmL", "HandL", "UpperLegR", "LowerLegR", "FootR", "ToesR" })
                Assert.GreaterOrEqual(h.IndexOf(n), 0, n);
            var q = SkeletonDef.Quadruped(1f, 0.7f, 0.3f, 0.5f);
            Assert.AreEqual(18, q.Count, "Vierbeiner: Becken, Brust, Hals, Kopf, Schwanz ×2, 4 Beine × 3");
            foreach (var sk in new[] { h, q })
            {
                var names = new HashSet<string>();
                for (int i = 0; i < sk.Count; i++)
                {
                    Assert.IsTrue(names.Add(sk.Bones[i].Name), "doppelter Knochen " + sk.Bones[i].Name);
                    Assert.Less(sk.Bones[i].Parent, i, "Eltern vor Kind");
                }
                Assert.AreEqual(-1, sk.Bones[0].Parent);
            }
            var pos = h.WorldPositions(null, Vector3.zero);
            Assert.That(pos[h.IndexOf("Head")].y, Is.InRange(1.5f, 1.8f));
            Assert.That(pos[h.IndexOf("FootL")].y, Is.InRange(0.02f, 0.12f));
            Assert.Less(pos[h.IndexOf("HandL")].x, 0f); Assert.Greater(pos[h.IndexOf("HandR")].x, 0f);
        }

        [Test]
        public void SkinWeights_SumToOne_AndPreferNearestBone()
        {
            var sk = SkeletonDef.Humanoid();
            SkinWeights.Segments(sk, out var a, out var b);
            var rnd = new System.Random(5);
            for (int i = 0; i < 400; i++)
            {
                var p = new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() * 1.9f, (float)rnd.NextDouble() * 0.4f - 0.2f);
                var f = SkinWeights.Compute(p, a, b);
                Assert.AreEqual(1f, f.Sum, 1e-5f);
                Assert.GreaterOrEqual(f.W0 + 1e-5f, f.W1); Assert.GreaterOrEqual(f.W1 + 1e-5f, f.W2); Assert.GreaterOrEqual(f.W2 + 1e-5f, f.W3);
                Assert.GreaterOrEqual(f.W3, 0f);
            }
            var head = SkinWeights.Compute(sk.WorldPositions(null, Vector3.zero)[sk.IndexOf("Head")] + Vector3.up * 0.08f, a, b);
            Assert.AreEqual(sk.IndexOf("Head"), head.B0);
            // Glättung bleibt normiert
            var inf = new[] { SkinWeights.Compute(new Vector3(0.1f, 0.5f, 0), a, b), SkinWeights.Compute(new Vector3(0.1f, 0.48f, 0), a, b), SkinWeights.Compute(new Vector3(0.1f, 0.4f, 0), a, b) };
            foreach (var f in SkinWeights.Smooth(inf, new[] { 0, 1, 2 }, sk.Count, 3)) Assert.AreEqual(1f, f.Sum, 1e-5f);
        }

        [Test]
        public void MeshKitCharacters_WeightsNormalized_AndTearFree()
        {
            var tmp = new GameObject("TearTest");
            try
            {
                var rig = RigBuilder.Humanoid(tmp.transform, "PT_Test_Humanoid", CharacterRigs.PlayerLook());
                AssertTearFree(rig, Human);
                var pr = CharacterRigs.Profile("Raptor");
                var beast = RigBuilder.Beast(tmp.transform, "PT_Test_Raptor", pr, new RigBuilder.BeastLook { Crest = true, Snout = true, Saddle = true }, 1.3f, 1.15f, 0.75f, 0.7f, 1.1f, 0.64f);
                AssertTearFree(beast, ClipLibrary.Creature(pr, true));
            }
            finally { Object.DestroyImmediate(tmp); }
        }

        static void AssertTearFree(RigBuilder.Rig rig, List<ClipData> clips)
        {
            var mesh = rig.Smr.sharedMesh; var bw = mesh.boneWeights;
            var inf = new SkinWeights.Influence[bw.Length];
            for (int i = 0; i < bw.Length; i++)
            {
                var w = bw[i];
                Assert.AreEqual(1f, w.weight0 + w.weight1 + w.weight2 + w.weight3, 1e-4f);
                inf[i] = new SkinWeights.Influence { B0 = w.boneIndex0, B1 = w.boneIndex1, B2 = w.boneIndex2, B3 = w.boneIndex3, W0 = w.weight0, W1 = w.weight1, W2 = w.weight2, W3 = w.weight3 };
            }
            RigBuilder.Tears(rig.Sk, new List<Vector3>(mesh.vertices), mesh.triangles, inf, clips, out float worst, out int tears);
            Assert.AreEqual(0, tears, "Risskanten (Streckung > 2,5) – schlimmste " + worst);
        }

        [Test]
        public void Clips_LengthFramesLoops_AndNoNaN()
        {
            var all = new List<ClipData>(Human);
            foreach (var pr in ClipLibrary.Creatures) all.AddRange(ClipLibrary.Creature(pr, true));
            foreach (var c in all)
            {
                Assert.Greater(c.Length, 0.2f, c.Name);
                Assert.AreEqual(Mathf.RoundToInt(c.Length * 30f) + 1, c.FrameCount, c.Name);
                Assert.IsFalse(ClipLibrary.HasNaN(c), c.Name + " enthält NaN/Unendlich");
                if (c.Loop)
                    for (int b = 0; b < c.Euler[0].Length; b++)
                        Assert.Less((c.Euler[0][b] - c.Euler[c.FrameCount - 1][b]).magnitude, 0.01f, c.Name + ": Schleifennaht Knochen " + b);
                foreach (var m in c.Markers) Assert.That(m.Time, Is.InRange(0f, c.Length), c.Name + " " + m.Type);
            }
        }

        [Test]
        public void ClipCatalog_Complete_ForAllRigsAndWeapons()
        {
            var names = new HashSet<string>(); foreach (var c in Human) names.Add(c.Name);
            foreach (var n in ClipLibrary.HumanoidCoreClips) Assert.IsTrue(names.Contains(n), n);
            foreach (var st in ClipLibrary.Styles) foreach (var k in ClipLibrary.WeaponClipKinds) Assert.IsTrue(names.Contains(st.Name + "_" + k), st.Name + "_" + k);
            foreach (var pr in ClipLibrary.Creatures)
            {
                var cn = new HashSet<string>(); foreach (var c in ClipLibrary.Creature(pr, true)) cn.Add(c.Name);
                foreach (var k in ClipLibrary.CreatureClipKinds) Assert.IsTrue(cn.Contains(pr.Name + "_" + k), pr.Name + "_" + k);
                foreach (var k in ClipLibrary.MountClipKinds) Assert.IsTrue(cn.Contains(pr.Name + "_" + k), pr.Name + "_" + k);
            }
        }

        [Test]
        public void Markers_AttacksHaveHitOrProjectile_AfterEffect_AndActivationEnd()
        {
            foreach (var st in ClipLibrary.Styles)
                foreach (var k in ClipLibrary.WeaponClipKinds)
                {
                    if (k == "Block") continue;
                    var c = H(st.Name + "_" + k);
                    float hit = Mathf.Max(c.MarkerTime(Marker.Hit), c.MarkerTime(Marker.Projectile));
                    Assert.Greater(hit, 0f, c.Name + ": Treffer-/Geschoss-Marker");
                    Assert.Greater(c.MarkerTime(Marker.End), hit - 1e-4f, c.Name + ": Aktivierungsende nach Treffer");
                    float eff = c.MarkerTime(Marker.Effect);
                    if (eff >= 0f) Assert.LessOrEqual(eff, hit, c.Name + ": Effektstart vor Treffer");
                }
            Assert.AreEqual(3, H("Greatsword_Whirl").MarkerCount(Marker.Hit), "Wirbel trifft dreimal");
            Assert.Greater(H("Cast1H").MarkerTime(Marker.Projectile), 0f);
            Assert.Greater(H("Death").MarkerTime(Marker.End), 0f);
        }

        [Test]
        public void FootContact_MarkersSitOnFootMinimum_AndFeetReachGround()
        {
            var sk = SkeletonDef.Humanoid();
            float rest = sk.WorldPositions(null, Vector3.zero)[sk.IndexOf("FootL")].y;
            foreach (var n in new[] { "Walk", "Run", "Sprint", "StrafeL", "StrafeR", "Back" })
            {
                var c = H(n);
                foreach (var side in new[] { "L", "R" })
                {
                    var path = ClipLibrary.BonePath(sk, c, sk.IndexOf("Foot" + side));
                    float mn = float.MaxValue; foreach (var v in path) mn = Mathf.Min(mn, v.y);
                    float t = c.MarkerTime("Foot" + side);
                    Assert.GreaterOrEqual(t, 0f, n + " Foot" + side);
                    Assert.LessOrEqual(path[Mathf.RoundToInt(t * 30f)].y, mn + 0.01f, n + ": Kontakt-Marker am Fuß-Minimum");
                    Assert.AreEqual(rest, Mathf.Min(mn, rest + 1f), 0.03f, n + ": tiefster Fuß erreicht den Boden");
                }
                Assert.AreNotEqual(c.MarkerTime(Marker.FootL), c.MarkerTime(Marker.FootR), n + ": Füße wechseln sich ab");
            }
        }

        [Test]
        public void Telegraphs_WindupAtLeast600ms()
        {
            Assert.GreaterOrEqual(NpcBrain.EffectiveWindup(null), 0.6f);
            var a = ScriptableObject.CreateInstance<EnemyArchetype>(); a.AttackWindup = 0.3f;
            Assert.AreEqual(0.6f, NpcBrain.EffectiveWindup(a), 1e-5f);
            a.AttackWindup = 0.9f; Assert.AreEqual(0.9f, NpcBrain.EffectiveWindup(a), 1e-5f);
            Object.DestroyImmediate(a);
            foreach (var pr in ClipLibrary.Creatures)
                foreach (var c in ClipLibrary.Creature(pr, true))
                    if (c.Name.EndsWith("_Attack") || c.Name.EndsWith("_MEngage")) Assert.GreaterOrEqual(c.MarkerTime(Marker.Hit), 0.6f, c.Name);
            foreach (var st in ClipLibrary.Styles) if (!st.Bow) Assert.GreaterOrEqual(H(st.Name + "_Heavy").MarkerTime(Marker.Hit), 0.6f, st.Name + "_Heavy");
            Assert.GreaterOrEqual(H("Telegraph").Length, 0.6f);
        }

        [Test]
        public void LocomotionBlend_WeightsSumToOne_AndFollowDirection()
        {
            var w = new float[LocomotionBlend.Count];
            LocomotionBlend.Weights(0, 0, w); Assert.AreEqual(1f, w[LocomotionBlend.Idle], 1e-5f);
            LocomotionBlend.Weights(2.6f, 0, w); Assert.AreEqual(1f, w[LocomotionBlend.Walk], 1e-5f);
            LocomotionBlend.Weights(7f, 0, w); Assert.AreEqual(1f, w[LocomotionBlend.Run], 1e-5f);
            LocomotionBlend.Weights(8.5f, 0, w); Assert.AreEqual(0.5f, w[LocomotionBlend.Sprint], 1e-4f);
            LocomotionBlend.Weights(0, -4f, w); Assert.AreEqual(1f, w[LocomotionBlend.StrafeL], 1e-5f);
            LocomotionBlend.Weights(-4f, 0, w); Assert.AreEqual(1f, w[LocomotionBlend.Back], 1e-5f);
            var rnd = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                LocomotionBlend.Weights((float)rnd.NextDouble() * 24f - 12f, (float)rnd.NextDouble() * 24f - 12f, w);
                float s = 0f; foreach (var x in w) { Assert.GreaterOrEqual(x, 0f); s += x; }
                Assert.AreEqual(1f, s, 1e-4f);
            }
            var len = new[] { 3f, 1.1f, 0.68f, 0.56f, 1f, 1f, 1.2f };
            LocomotionBlend.Weights(2.6f, 0, w); Assert.AreEqual(1f / 1.1f, LocomotionBlend.PhaseRate(w, len, 2.6f), 1e-3f);
        }

        [Test]
        public void SkillClip_MapsDeliveryAndChain()
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>();
            try
            {
                Assert.AreEqual("Greatsword_Auto1", AnimDriver.SkillClip(s, true, 0, "Greatsword"));
                Assert.AreEqual("Greatsword_Auto3", AnimDriver.SkillClip(s, true, 2, "Greatsword"));
                s.Delivery = SkillDelivery.Leap; Assert.AreEqual("Greatsword_Thrust", AnimDriver.SkillClip(s, false, 0, "Greatsword"));
                s.Delivery = SkillDelivery.Projectile; Assert.AreEqual("Cast1H", AnimDriver.SkillClip(s, false, 0, "Staff"));
                s.Delivery = SkillDelivery.GroundTarget; Assert.AreEqual("Cast2H", AnimDriver.SkillClip(s, false, 0, "Staff"));
                s.Delivery = SkillDelivery.Melee; s.Finisher = ComboFinisherType.Whirl; Assert.AreEqual("Greatsword_Whirl", AnimDriver.SkillClip(s, false, 0, "Greatsword"));
                s.Special = SkillSpecial.Bandage; Assert.AreEqual("Bandage", AnimDriver.SkillClip(s, false, 0, "Greatsword"));
                s.Special = SkillSpecial.None; s.SlotType = SkillSlotType.Utility; s.Category = UtilityCategory.Shout; Assert.AreEqual("Cheer", AnimDriver.SkillClip(s, false, 0, "Greatsword"));
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void Ride_HipsSitOnSaddleHeight()
        {
            var sk = SkeletonDef.Humanoid();
            foreach (var n in new[] { "Ride" })
            {
                var hips = ClipLibrary.BonePath(sk, H(n), sk.IndexOf("Hips"));
                foreach (var h in hips) Assert.That(h.y, Is.InRange(0.05f, 0.3f), n + ": Hüfte muss auf Sitzhöhe (Sattel) liegen");
            }
            var mount = H("Mount");
            Assert.That(ClipLibrary.BonePath(sk, mount, sk.IndexOf("Hips"))[mount.FrameCount - 1].y, Is.InRange(0.05f, 0.3f), "Aufsitzen endet in Sitzpose");
        }

        [Test]
        public void ClipGeneration_IsDeterministic()
        {
            var a = ClipLibrary.Humanoid(); var b = ClipLibrary.Humanoid();
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Name, b[i].Name);
                for (int f = 0; f < a[i].FrameCount; f += 7)
                    for (int k = 0; k < a[i].Euler[f].Length; k++) Assert.AreEqual(a[i].Euler[f][k], b[i].Euler[f][k]);
            }
        }
    }
}
