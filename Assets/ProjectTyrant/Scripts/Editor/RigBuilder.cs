using System.Collections.Generic;
using ProjectTyrant.Anim;
using SkinWeights = ProjectTyrant.Anim.SkinWeights;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static ProjectTyrant.EditorTools.PTEditorUtil;
using RigBuilderComponent = UnityEngine.Animations.Rigging.RigBuilder;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M21: baut Skelette (Humanoid 20 Knochen, Vierbeiner 18 Knochen) als Transform-Hierarchie „Visual/Body/Rig/…“,
    /// erzeugt geskinnte MeshKit-Charaktere (SkinnedMeshRenderer, Gewichte nach Knochennähe, an Gelenken geglättet),
    /// richtet Animator + <see cref="CharacterAnimator"/> + Fuß-/Blick-IK (Animation Rigging) ein und versucht Meshy-Modelle auf dasselbe Skelett zu skinnen.
    /// </summary>
    public static class RigBuilder
    {
        public const string MeshDir = ProcDir + "/Meshes/Characters";
        public const float TearThreshold = 2.5f;      // Kantenstreckung, ab der ein Riss sichtbar ist
        public const float TearMinEdge = 0.02f;
        public static readonly List<string> MeshyReport = new List<string>();

        public class Rig
        {
            public SkeletonDef Sk; public GameObject Visual; public Transform Body, RigRoot; public Transform[] Bones; public SkinnedMeshRenderer Smr;
            public Transform Bone(string n) { int i = Sk.IndexOf(n); return i >= 0 ? Bones[i] : null; }
            public Transform HandSocket, BackSocket, HeadSocket;
        }

        // ------------------------------------------------------------ Skelett
        public static Rig CreateSkeleton(Transform parent, SkeletonDef sk, string visualName = "Visual")
        {
            var visual = new GameObject(visualName);
            visual.transform.SetParent(parent, false);
            var body = new GameObject("Body").transform; body.SetParent(visual.transform, false);
            var rig = new GameObject("Rig").transform; rig.SetParent(body, false);
            var bones = new Transform[sk.Count];
            for (int i = 0; i < sk.Count; i++)
            {
                var b = sk.Bones[i];
                var t = new GameObject(b.Name).transform;
                t.SetParent(b.Parent < 0 ? rig : bones[b.Parent], false);
                t.localPosition = b.Local; t.localRotation = Quaternion.identity;
                bones[i] = t;
            }
            return new Rig { Sk = sk, Visual = visual, Body = body, RigRoot = rig, Bones = bones };
        }

        /// <summary>Bind-Pose-Mesh (Koordinaten relativ zur Visual-Wurzel) an das Skelett binden und als Asset speichern.</summary>
        public static SkinnedMeshRenderer Bind(Rig r, SkinMesh sm, string meshName, Material[] mats, int smooth = 2)
        {
            EnsureFolder(MeshDir);
            var inf = sm.Weights(r.Sk, smooth);
            // Bestehendes Asset leeren und neu füllen (CopySerialized ließ GPU-Skinning-Puffer veralten → „Rendering stopped … vertex stride“)
            string path = MeshDir + "/" + meshName + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = meshName;
            mesh.indexFormat = sm.V.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(sm.V); mesh.SetColors(sm.C);
            int subs = Mathf.Min(sm.Sub.Count, mats.Length);
            mesh.subMeshCount = subs;
            // Körper (Untergruppe 0) als letztes Untermesh: ein angehängtes Umriss-Material rendert dann den Körper (M22)
            for (int s = 0; s < subs; s++) mesh.SetTriangles(sm.Sub[(s + 1) % subs], s);
            var bw = new BoneWeight[sm.V.Count];
            for (int i = 0; i < bw.Length; i++)
            {
                var f = inf[i];
                bw[i] = new BoneWeight { boneIndex0 = f.B0, boneIndex1 = f.B1, boneIndex2 = f.B2, boneIndex3 = f.B3, weight0 = f.W0, weight1 = f.W1, weight2 = f.W2, weight3 = f.W3 };
            }
            mesh.boneWeights = bw;
            var bind = new Matrix4x4[r.Bones.Length];
            for (int i = 0; i < bind.Length; i++) bind[i] = r.Bones[i].worldToLocalMatrix * r.Visual.transform.localToWorldMatrix;
            mesh.bindposes = bind;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            var go = new GameObject("Skin");
            go.transform.SetParent(r.Visual.transform, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh; smr.bones = r.Bones; smr.rootBone = r.Bones[0];
            var sub = new Material[subs]; for (int s = 0; s < subs; s++) sub[s] = mats[(s + 1) % subs];
            smr.sharedMaterials = sub;
            smr.updateWhenOffscreen = false;
            smr.localBounds = new Bounds(mesh.bounds.center - r.Bones[0].localPosition, mesh.bounds.size * 1.6f + Vector3.one * 0.5f);
            r.Smr = smr;
            return smr;
        }

        public static Material SkinMat() { var m = Mat("Character_VC", Color.white, 0.3f, null, 0.6f); if (m.HasProperty("_UseVertexColor")) m.SetFloat("_UseVertexColor", 1f); return m; }
        public static Material GlowMat(Color c) => Mat("Character_Glow_" + ColorUtility.ToHtmlStringRGB(c), Color.black, 0f, c, 0f);

        // ------------------------------------------------------------ Animator + IK
        public static CharacterAnimator Animate(Rig r, AnimClipSet set, string[] loco = null, bool humanoidIK = false, bool upperBody = true)
        {
            var v = r.Visual;
            var an = GetOrAdd<Animator>(v);
            an.runtimeAnimatorController = null; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var ca = GetOrAdd<CharacterAnimator>(v);
            ca.Set = set; ca.Body = r.Body; ca.UpperBodyLayer = upperBody && r.Sk.Kind == "Humanoid";
            ca.LocoClips = loco ?? new string[LocomotionBlend.Count];
            ca.BreathClip = r.Sk.Kind == "Humanoid" ? "Breath" : "";
            ca.GroundMask = LayerMask.GetMask("Default", "Ground", "Terrain") | 1;
            if (humanoidIK && r.Sk.Kind == "Humanoid")
            {
                var rbc = GetOrAdd<RigBuilderComponent>(v);
                var ikGo = new GameObject("IK"); ikGo.transform.SetParent(v.transform, false);
                var rig = ikGo.AddComponent<UnityEngine.Animations.Rigging.Rig>();
                rbc.layers.Clear(); rbc.layers.Add(new RigLayer(rig, true));
                ca.FootIKL = FootIK(ikGo.transform, r, "L"); ca.FootIKR = FootIK(ikGo.transform, r, "R");
                var lookT = new GameObject("LookTarget").transform; lookT.SetParent(ikGo.transform, false);
                lookT.position = r.Bone(SkeletonDef.Head).position + v.transform.forward * 3f;
                var lgo = new GameObject("LookIK"); lgo.transform.SetParent(ikGo.transform, false);
                var aim = lgo.AddComponent<MultiAimConstraint>();
                var d = aim.data;
                d.constrainedObject = r.Bone(SkeletonDef.Head);
                var src = new WeightedTransformArray(0); src.Add(new WeightedTransform(lookT, 1f)); d.sourceObjects = src;
                d.aimAxis = MultiAimConstraintData.Axis.Z; d.upAxis = MultiAimConstraintData.Axis.Y;
                d.limits = new Vector2(-60f, 60f); d.constrainedXAxis = true; d.constrainedYAxis = true; d.constrainedZAxis = true;
                aim.data = d; aim.weight = 0f;
                ca.LookIK = aim; ca.LookTarget = lookT;
            }
            return ca;
        }

        static TwoBoneIKConstraint FootIK(Transform ik, Rig r, string s)
        {
            var go = new GameObject("FootIK_" + s); go.transform.SetParent(ik, false);
            var c = go.AddComponent<TwoBoneIKConstraint>();
            var target = new GameObject("FootTarget_" + s).transform; target.SetParent(ik, false);
            var foot = r.Bone("Foot" + s); target.position = foot.position; target.rotation = foot.rotation;
            var hint = new GameObject("KneeHint_" + s).transform; hint.SetParent(ik, false);
            hint.position = r.Bone("LowerLeg" + s).position + r.Visual.transform.forward * 0.6f;
            var d = c.data;
            d.root = r.Bone("UpperLeg" + s); d.mid = r.Bone("LowerLeg" + s); d.tip = foot; d.target = target; d.hint = hint;
            d.targetPositionWeight = 1f; d.targetRotationWeight = 0f; d.hintWeight = 1f;
            c.data = d; c.weight = 0f;
            return c;
        }

        static Transform Socket(Transform bone, string name, Vector3 local, Vector3 euler)
        {
            var t = new GameObject(name).transform; t.SetParent(bone, false);
            t.localPosition = local; t.localEulerAngles = euler;
            return t;
        }

        /// <summary>Waffen-Sockets: rechte Hand (Klinge nach vorn-oben) und Rücken (schräg).</summary>
        public static void AddSockets(Rig r)
        {
            if (r.Sk.Kind != "Humanoid") return;
            float s = r.Sk.Bones[r.Sk.IndexOf(SkeletonDef.Hips)].Local.y / 0.95f;
            r.HandSocket = Socket(r.Bone(SkeletonDef.HandR), "HandSocket_R", new Vector3(0, -0.07f, 0.01f) * s, new Vector3(75, 0, 0));
            Socket(r.Bone(SkeletonDef.HandL), "HandSocket_L", new Vector3(0, -0.07f, 0.01f) * s, new Vector3(75, 0, 0));
            r.BackSocket = Socket(r.Bone(SkeletonDef.Spine2), "BackSocket", new Vector3(0.02f, 0.05f, -0.16f) * s, new Vector3(0, 0, -145));
            r.HeadSocket = Socket(r.Bone(SkeletonDef.Head), "HeadSocket", new Vector3(0, 0.12f, 0) * s, Vector3.zero);
        }

        // ------------------------------------------------------------ Humanoide
        public class HumanLook
        {
            public Color Cloth = new Color(0.3f, 0.45f, 0.75f), Trim = new Color(0.85f, 0.7f, 0.3f), Skin = new Color(0.96f, 0.76f, 0.62f),
                Leather = new Color(0.42f, 0.27f, 0.18f), Metal = new Color(0.78f, 0.8f, 0.86f), Hair = new Color(0.55f, 0.28f, 0.14f), Pants = new Color(0.35f, 0.28f, 0.22f);
            public Color Eye = new Color(0.08f, 0.06f, 0.05f); public bool GlowEyes;
            public float Height = 1.8f, Bulk = 1f;
            public bool Cape, Pauldrons, Helmet, Horns, Robe, Beard, Hood, Crown, Cracks, Shield, Belly;
            public int Seed;
        }

        public static Rig Humanoid(Transform parent, string meshName, HumanLook l)
        {
            var sk = SkeletonDef.Humanoid(l.Height, l.Bulk);
            var r = CreateSkeleton(parent, sk);
            var p = sk.WorldPositions(null, Vector3.zero);
            System.Func<string, Vector3> P = n => p[sk.IndexOf(n)];
            System.Func<string, int> I = sk.IndexOf;
            float s = l.Height / 1.8f, b = l.Bulk;
            var m = new SkinMesh();
            // Beine
            foreach (var side in new[] { "L", "R" })
            {
                float sx = side == "L" ? -1f : 1f;
                m.Tube(P("UpperLeg" + side) + Vector3.up * 0.04f * s, P("LowerLeg" + side), new Vector2(0.085f * b, 0.09f * b) * s, new Vector2(0.06f * b, 0.065f * b) * s, l.Pants, I("UpperLeg" + side));
                m.Tube(P("LowerLeg" + side), P("Foot" + side), new Vector2(0.065f * b, 0.07f * b) * s, new Vector2(0.05f * b, 0.055f * b) * s, l.Leather, I("LowerLeg" + side));
                m.Ellipsoid(P("LowerLeg" + side) + new Vector3(0, 0, 0.02f) * s, new Vector3(0.05f, 0.05f, 0.042f) * s * b, l.Leather, I("LowerLeg" + side), I("LowerLeg" + side), 4, 8);
                m.Box(P("Foot" + side) + new Vector3(0, -0.02f, 0.04f) * s, new Vector3(0.11f * b, 0.08f, 0.2f) * s, l.Leather, I("Foot" + side), I("Foot" + side));
                m.Box(P("Toes" + side) + new Vector3(0, 0.02f, 0.02f) * s, new Vector3(0.1f * b, 0.06f, 0.08f) * s, l.Leather, I("Toes" + side), I("Toes" + side));
                // Arme
                m.Ellipsoid(P("UpperArm" + side), new Vector3(0.075f, 0.075f, 0.075f) * s * b, l.Cloth, I("UpperArm" + side), -1, 5, 8);
                m.Tube(P("UpperArm" + side), P("LowerArm" + side), new Vector2(0.055f, 0.058f) * s * b, new Vector2(0.045f, 0.048f) * s * b, l.Cloth, I("UpperArm" + side));
                m.Tube(P("LowerArm" + side), P("Hand" + side), new Vector2(0.046f, 0.048f) * s * b, new Vector2(0.036f, 0.034f) * s * b, l.Leather, I("LowerArm" + side));
                m.Box(P("Hand" + side) + new Vector3(0, -0.055f, 0.005f) * s, new Vector3(0.055f, 0.1f, 0.075f) * s * b, l.Skin, I("Hand" + side), I("Hand" + side));
                if (l.Pauldrons)
                {
                    m.Ellipsoid(P("UpperArm" + side) + new Vector3(sx * 0.02f, 0.04f, 0) * s, new Vector3(0.1f, 0.065f, 0.1f) * s * b, l.Metal, I("UpperArm" + side), I("UpperArm" + side), 4, 10);
                }
                if (l.Shield && side == "L")
                    m.Ellipsoid(P("LowerArm" + side) + new Vector3(-0.07f, -0.1f, 0.04f) * s, new Vector3(0.03f, 0.26f, 0.26f) * s, l.Metal, I("LowerArm" + side), I("LowerArm" + side), 5, 12);
            }
            // Rumpf
            m.Tube(P("Hips") + Vector3.down * 0.06f * s, P("Spine"), new Vector2(0.17f * b, 0.115f * b) * s, new Vector2(0.155f * b, 0.105f * b) * s, l.Pants, I("Hips"));
            m.Tube(P("Spine"), P("Spine1"), new Vector2(0.155f * b, 0.105f * b) * s, new Vector2(0.165f * b, l.Belly ? 0.15f : 0.11f) * s, l.Cloth, I("Spine"));
            m.Tube(P("Spine1"), P("Spine2"), new Vector2(0.165f * b, l.Belly ? 0.15f : 0.11f) * s, new Vector2(0.19f * b, 0.125f * b) * s, l.Cloth, I("Spine1"));
            m.Tube(P("Spine2"), P("Neck"), new Vector2(0.19f * b, 0.125f * b) * s, new Vector2(0.11f * b, 0.08f * b) * s, l.Cloth, I("Spine2"), -1, 8, 3, 0.15f);
            m.Tube(P("Hips") + Vector3.up * 0.06f * s, P("Hips") + Vector3.up * 0.11f * s, new Vector2(0.18f * b, 0.125f * b) * s, new Vector2(0.18f * b, 0.125f * b) * s, l.Trim, I("Hips"), I("Hips"), 10, 1, 0f);
            m.Box(P("Hips") + new Vector3(0, 0.085f, 0.12f * b) * s, new Vector3(0.07f, 0.06f, 0.03f) * s, l.Metal, I("Hips"), I("Hips"));
            // Hals + Kopf
            m.Tube(P("Neck"), P("Head") + Vector3.up * 0.03f * s, new Vector2(0.055f, 0.055f) * s * b, new Vector2(0.05f, 0.05f) * s * b, l.Skin, I("Neck"));
            var head = P("Head") + new Vector3(0, 0.09f, 0.01f) * s; int hb = I("Head");
            m.Ellipsoid(head, new Vector3(0.1f, 0.12f, 0.11f) * s, l.Skin, hb, hb, 7, 12);
            m.Box(head + new Vector3(0, -0.005f, 0.105f) * s, new Vector3(0.025f, 0.04f, 0.03f) * s, l.Skin * 0.95f, hb, hb);
            if (l.Helmet) m.Ellipsoid(head + new Vector3(0, 0.035f, -0.005f) * s, new Vector3(0.115f, 0.1f, 0.122f) * s, l.Metal, hb, hb, 6, 12);
            else if (l.Hood) m.Ellipsoid(head + new Vector3(0, 0.03f, -0.02f) * s, new Vector3(0.125f, 0.13f, 0.13f) * s, l.Cloth * 0.8f, hb, hb, 6, 12);
            else m.Ellipsoid(head + new Vector3(0, 0.04f, -0.018f) * s, new Vector3(0.106f, 0.092f, 0.112f) * s, l.Hair, hb, hb, 6, 12);
            if (l.Beard) m.Ellipsoid(head + new Vector3(0, -0.07f, 0.06f) * s, new Vector3(0.07f, 0.06f, 0.05f) * s, l.Hair, hb, hb, 4, 8);
            if (l.Horns) { m.Cone(head + new Vector3(-0.08f, 0.06f, 0) * s, new Vector3(-0.08f, 0.16f, -0.02f) * s, 0.03f * s, l.Metal, hb, hb); m.Cone(head + new Vector3(0.08f, 0.06f, 0) * s, new Vector3(0.08f, 0.16f, -0.02f) * s, 0.03f * s, l.Metal, hb, hb); }
            if (l.Crown) for (int i = 0; i < 5; i++) { float a = (i - 2) * 0.35f; m.Cone(head + new Vector3(Mathf.Sin(a) * 0.08f, 0.09f, Mathf.Cos(a) * 0.08f - 0.02f) * s, new Vector3(0, 0.09f, 0) * s, 0.018f * s, l.Trim, hb, hb, 4); }
            if (l.Cape)
            {
                int sp2 = I("Spine2"), sp1 = I("Spine1"), sp = I("Spine");
                var top = P("Spine2") + new Vector3(-0.17f * b, 0.1f, -0.125f * b) * s;
                m.Sheet(top, new Vector3(0.34f * b, 0, 0) * s, new Vector3(0, -0.62f, -0.06f) * s, 4, 5, l.Trim, row => row <= 1 ? sp2 : row <= 3 ? sp1 : sp, 0.03f * s, Vector3.back);
            }
            if (l.Robe)
            {
                m.AllowedNow = new[] { I("Hips"), I("UpperLegL"), I("UpperLegR"), I("Spine") };
                m.Tube(P("Hips") + Vector3.up * 0.04f * s, P("Hips") + Vector3.down * 0.6f * s, new Vector2(0.19f * b, 0.135f * b) * s, new Vector2(0.27f * b, 0.22f * b) * s, l.Cloth * 0.85f, I("Hips"), -1, 10, 4, 0f, false);
                m.AllowedNow = null;
            }
            if (l.Cracks)
            {
                var gc = l.Trim;
                for (int i = 0; i < 6; i++) m.Box(P("Spine1") + new Vector3(Mathf.Sin(i * 1.7f) * 0.12f, Mathf.Cos(i * 2.3f) * 0.12f, 0.13f * b) * s, new Vector3(0.02f, 0.12f, 0.02f) * s, gc, I("Spine1"), I("Spine1"), Quaternion.Euler(0, 0, i * 37f));
            }
            // Augen (eigene Untergruppe, bei Glut leuchtend)
            m.UseSub(1);
            m.Ellipsoid(head + new Vector3(-0.038f, 0.012f, 0.095f) * s, new Vector3(0.016f, 0.02f, 0.012f) * s, l.Eye, hb, hb, 3, 6);
            m.Ellipsoid(head + new Vector3(0.038f, 0.012f, 0.095f) * s, new Vector3(0.016f, 0.02f, 0.012f) * s, l.Eye, hb, hb, 3, 6);
            m.UseSub(0);
            var eyeMat = l.GlowEyes ? GlowMat(l.Eye) : Mat("Character_Eye", new Color(0.06f, 0.05f, 0.05f), 0f, null, 0.2f);
            Bind(r, m, meshName, new[] { SkinMat(), eyeMat });
            AddSockets(r);
            return r;
        }

        // ------------------------------------------------------------ Tiere
        public class BeastLook
        {
            public Color Body = new Color(0.5f, 0.5f, 0.5f), Belly = new Color(0.7f, 0.7f, 0.7f), Accent = new Color(0.8f, 0.3f, 0.2f), Eye = new Color(1.6f, 0.6f, 0.1f);
            public bool Ears, Horns, Snout, Crest, Saddle, Antennae, BigHind, Spots, Beak, Shell, LongEars;
            public float Glow;
        }

        /// <summary>Vierbeiner-Skelett + Haut: Rumpf (Becken→Brust), Hals, Kopf, Schwanz, vier Beine (Profil bestimmt Flügel/Flossen/Zweibeiner).</summary>
        public static Rig Beast(Transform parent, string meshName, CreatureProfile pr, BeastLook l, float length, float legHeight, float bodyH, float neck, float tail, float width)
        {
            var sk = SkeletonDef.Quadruped(length, legHeight, neck, tail, width, pr.FrontLeg);
            var r = CreateSkeleton(parent, sk);
            var p = sk.WorldPositions(null, Vector3.zero);
            System.Func<string, Vector3> P = n => p[sk.IndexOf(n)];
            System.Func<string, int> I = sk.IndexOf;
            var m = new SkinMesh();
            float bh = bodyH;
            bool fins = pr.Gait == CreatureProfile.GaitKind.Fins, wings = pr.Gait == CreatureProfile.GaitKind.Wings, biped = pr.Gait == CreatureProfile.GaitKind.Biped;
            Vector3 pel = P(SkeletonDef.Pelvis), ch = P(SkeletonDef.Chest);
            Vector3 tailDir = (pel - ch).normalized;
            // Rumpf
            m.AllowedNow = new[] { I(SkeletonDef.Pelvis), I(SkeletonDef.Chest) };
            Vector2 rr = fins ? new Vector2(width * 0.55f, bh * 0.22f) : new Vector2(Mathf.Max(width * 0.5f, bh * 0.45f), bh * 0.5f);
            m.Tube(pel + tailDir * bh * 0.35f, ch - tailDir * bh * 0.3f, rr * 0.85f, rr * (l.Shell ? 0.8f : 1f), l.Body, I(SkeletonDef.Pelvis), -1, 10, 4, 0.22f);
            if (!fins) m.Tube(pel + tailDir * bh * 0.2f + Vector3.down * bh * 0.12f, ch - tailDir * bh * 0.15f + Vector3.down * bh * 0.12f, rr * 0.75f, rr * 0.8f, l.Belly, I(SkeletonDef.Chest), -1, 8, 3, 0.2f);
            else m.Ellipsoid((pel + ch) * 0.5f + Vector3.down * bh * 0.08f, new Vector3(width * 0.5f, bh * 0.14f, (ch - pel).magnitude * 0.5f), l.Belly, I(SkeletonDef.Chest), -1, 4, 12);
            m.AllowedNow = null;
            if (l.Shell) m.Ellipsoid((pel + ch) * 0.5f + Vector3.up * bh * 0.18f, new Vector3(width * 0.62f, bh * 0.45f, (ch - pel).magnitude * 0.72f), l.Body * 0.85f, I(SkeletonDef.Pelvis), I(SkeletonDef.Pelvis), 5, 12);
            if (l.Saddle) m.Box((pel + ch) * 0.5f + Vector3.up * (rr.y * 0.95f), new Vector3(rr.x * 1.6f, 0.08f, (ch - pel).magnitude * 0.5f), l.Accent, I(SkeletonDef.Pelvis), I(SkeletonDef.Pelvis));
            if (l.Spots) for (int i = 0; i < 4; i++) m.Ellipsoid(Vector3.Lerp(pel, ch, 0.2f + i * 0.2f) + new Vector3((i % 2 - 0.5f) * width * 0.4f, rr.y * 0.9f, 0), Vector3.one * 0.06f, l.Accent, I(SkeletonDef.Chest), I(i < 2 ? SkeletonDef.Pelvis : SkeletonDef.Chest), 3, 6);
            // Hals + Kopf
            Vector3 nk = P(SkeletonDef.QNeck), hd = P(SkeletonDef.QHead);
            if (neck > 0.05f) m.Tube(ch, hd, new Vector2(bh * 0.25f, bh * 0.28f), new Vector2(bh * 0.17f, bh * 0.19f), l.Body, I(SkeletonDef.QNeck), -1, 8, 3, 0.05f);
            int hb = I(SkeletonDef.QHead);
            float hs = Mathf.Max(0.12f, bh * 0.38f);
            if (l.Beak) hs = Mathf.Max(0.08f, bh * 0.25f);
            m.Ellipsoid(hd, new Vector3(hs * 0.85f, hs * 0.8f, hs * 1.05f), l.Body, hb, hb, 6, 10);
            if (l.Snout) m.Ellipsoid(hd + new Vector3(0, -hs * 0.25f, hs * 0.85f), new Vector3(hs * 0.55f, hs * 0.45f, hs * 0.6f), l.Belly, hb, hb, 5, 8);
            if (l.Beak) m.Cone(hd + new Vector3(0, -hs * 0.1f, hs * 0.8f), new Vector3(0, -0.02f, hs * 3.2f), hs * 0.25f, l.Accent, hb, hb);
            if (l.Ears || l.LongEars)
            {
                float el = l.LongEars ? hs * 2.2f : hs * 0.9f;
                m.Box(hd + new Vector3(-hs * 0.45f, hs * 0.7f + el * 0.4f, -hs * 0.2f), new Vector3(hs * 0.3f, el, hs * 0.14f), l.Accent, hb, hb, Quaternion.Euler(-10, 0, 14));
                m.Box(hd + new Vector3(hs * 0.45f, hs * 0.7f + el * 0.4f, -hs * 0.2f), new Vector3(hs * 0.3f, el, hs * 0.14f), l.Accent, hb, hb, Quaternion.Euler(-10, 0, -14));
            }
            if (l.Horns) { m.Cone(hd + new Vector3(-hs * 0.4f, hs * 0.6f, -hs * 0.2f), new Vector3(-hs * 0.3f, hs * 1.1f, -hs * 0.6f), hs * 0.16f, l.Accent, hb, hb); m.Cone(hd + new Vector3(hs * 0.4f, hs * 0.6f, -hs * 0.2f), new Vector3(hs * 0.3f, hs * 1.1f, -hs * 0.6f), hs * 0.16f, l.Accent, hb, hb); }
            if (l.Crest) for (int i = 0; i < 4; i++) m.Box(hd + new Vector3(0, hs * 0.85f, -hs * 0.2f - i * hs * 0.45f), new Vector3(0.05f, hs * (0.9f - i * 0.15f), hs * 0.35f), l.Accent, hb, hb, Quaternion.Euler(-20, 0, 0));
            if (l.Antennae) { m.Tube(hd + new Vector3(-hs * 0.3f, hs * 0.4f, hs * 0.5f), hd + new Vector3(-hs * 0.9f, hs * 1.4f, hs * 2.2f), Vector2.one * 0.015f, Vector2.one * 0.01f, l.Accent, hb, hb, 4, 1); m.Tube(hd + new Vector3(hs * 0.3f, hs * 0.4f, hs * 0.5f), hd + new Vector3(hs * 0.9f, hs * 1.4f, hs * 2.2f), Vector2.one * 0.015f, Vector2.one * 0.01f, l.Accent, hb, hb, 4, 1); }
            // Schwanz
            Vector3 t1 = P(SkeletonDef.Tail1), t2 = P(SkeletonDef.Tail2);
            if (tail > 0.05f)
            {
                Vector3 tip = t2 + (t2 - t1).normalized * tail * 0.5f;
                m.Tube(t1, t2, Vector2.one * bh * 0.16f, Vector2.one * bh * 0.1f, l.Body, I(SkeletonDef.Tail1), -1, 6, 2);
                m.Tube(t2, tip, Vector2.one * bh * 0.1f, Vector2.one * bh * 0.03f, fins ? l.Accent : l.Body, I(SkeletonDef.Tail2), -1, 6, 2);
            }
            // Beine / Flügel / Flossen
            foreach (var leg in SkeletonDef.Legs)
            {
                bool front = leg[0] == 'F'; float sx = leg[1] == 'L' ? -1f : 1f;
                Vector3 u = P(leg + "_Upper"), lo = P(leg + "_Lower"), ft = P(leg + "_Foot");
                if (fins)
                {
                    m.AllowedNow = new[] { I(leg + "_Upper"), I(leg + "_Lower"), I(leg + "_Foot") };
                    float fl = (u - ft).magnitude;
                    m.Ellipsoid((u + lo) * 0.5f, new Vector3(0.05f, fl * 0.32f, (front ? 0.55f : 0.4f) * (ch - pel).magnitude), l.Body, I(leg + "_Upper"), -1, 4, 10);
                    m.Ellipsoid((lo + ft) * 0.5f, new Vector3(0.04f, fl * 0.3f, (front ? 0.4f : 0.3f) * (ch - pel).magnitude), l.Body, I(leg + "_Lower"), -1, 4, 10);
                    m.AllowedNow = null;
                    continue;
                }
                if (wings && front)
                {
                    float wl = (u - ft).magnitude;
                    m.Ellipsoid((u + lo) * 0.5f + new Vector3(0, 0, -0.05f), new Vector3(0.03f, wl * 0.32f, length * 0.45f), l.Body * 0.9f, I(leg + "_Upper"), I(leg + "_Upper"), 4, 10);
                    m.Ellipsoid((lo + ft) * 0.5f + new Vector3(0, 0, -0.1f), new Vector3(0.025f, wl * 0.3f, length * 0.35f), l.Accent, I(leg + "_Lower"), I(leg + "_Lower"), 4, 10);
                    continue;
                }
                float thick = Mathf.Max(0.025f, bh * (front ? 0.13f : 0.16f) * (l.BigHind && !front ? 1.9f : 1f));
                if (biped && front) thick *= 0.5f;
                m.Tube(u + Vector3.up * thick * 0.6f, lo, new Vector2(thick * 1.15f, thick * 1.35f), new Vector2(thick * 0.8f, thick * 0.85f), l.Body, I(leg + "_Upper"));
                m.Tube(lo, ft, new Vector2(thick * 0.8f, thick * 0.85f), new Vector2(thick * 0.6f, thick * 0.6f), l.Body, I(leg + "_Lower"));
                m.Ellipsoid(ft + new Vector3(0, -thick * 0.2f, thick * 0.5f), new Vector3(thick * 0.8f, thick * 0.55f, thick * (biped ? 1.8f : 1.2f)), wings ? l.Accent : l.Belly, I(leg + "_Foot"), I(leg + "_Foot"), 3, 8);
            }
            if (l.Shell)   // mittleres Beinpaar starr am Becken
                foreach (var sx in new[] { -1f, 1f })
                {
                    var hip = (pel + ch) * 0.5f + new Vector3(sx * width * 0.5f, -0.05f, 0);
                    m.Tube(hip, hip + new Vector3(sx * 0.12f, -legHeight * 0.9f, 0), Vector2.one * 0.03f, Vector2.one * 0.02f, l.Belly, I(SkeletonDef.Pelvis), I(SkeletonDef.Pelvis), 5, 1);
                }
            m.UseSub(1);
            m.Ellipsoid(hd + new Vector3(-hs * 0.55f, hs * 0.25f, hs * 0.55f), Vector3.one * hs * 0.17f, l.Eye, hb, hb, 3, 6);
            m.Ellipsoid(hd + new Vector3(hs * 0.55f, hs * 0.25f, hs * 0.55f), Vector3.one * hs * 0.17f, l.Eye, hb, hb, 3, 6);
            m.UseSub(0);
            var eye = l.Eye.maxColorComponent > 1f ? GlowMat(l.Eye) : Mat("Character_Eye_" + ColorUtility.ToHtmlStringRGB(l.Eye), l.Eye, 0f, null, 0.2f);
            Bind(r, m, meshName, new[] { SkinMat(), eye });
            return r;
        }

        /// <summary>Clip-Namen für die Fortbewegung eines Tier-Profils (Reittiere nutzen MIdle/MRun).</summary>
        public static string[] BeastLoco(string profile, bool mount)
        {
            string P(string n) => profile + "_" + n;
            return mount
                ? new[] { P("MIdle"), P("Walk"), P("MRun"), P("MRun"), P("Walk"), P("Walk"), P("Walk") }
                : new[] { P("Idle"), P("Walk"), P("Run"), P("Run"), P("Walk"), P("Walk"), P("Walk") };
        }

        // ------------------------------------------------------------ Meshy-Skinning (3 Ansätze, sonst Ersatz)
        /// <summary>
        /// Versucht, ein statisches Meshy-Modell auf ein Humanoid-/Vierbeiner-Skelett zu skinnen. Ansätze: (1) Knochennähe roh,
        /// (2) + Laplace-Glättung über verschweißte Nachbarn, (3) + Skelett-Anpassung an die Modell-Silhouette (Arme/Beine) und Glättung ×4.
        /// Riss-Prüfung über Testposen (größte Kantenstreckung). Ergebnis: Rig oder null (dann MeshKit-Ersatz + Statue).
        /// </summary>
        public static Rig TrySkinMeshy(Transform parent, string modelName, float height, SkeletonDef template, List<ClipData> testClips, out string report)
        {
            report = modelName + ": kein Modell";
            var prefab = MeshyIntegrator.GetOrBuild(modelName, height);
            if (prefab == null) return null;
            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var sm = new SkinMesh();
            var mats = new List<Material>();
            foreach (var mf in tmp.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                MakeReadable(mesh);
                sm.AddMesh(mesh, tmp.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix, Color.white, mats.Count);
                if (mr != null) mats.AddRange(mr.sharedMaterials);
            }
            Object.DestroyImmediate(tmp);
            if (sm.V.Count == 0) { report = modelName + ": leeres Modell"; return null; }
            var tris = sm.AllTriangles();
            var weld = Weld(sm.V, tris);
            var sb = new System.Text.StringBuilder(modelName + " (" + sm.V.Count + " Vertices): ");
            SkeletonDef best = null; SkinWeights.Influence[] bestW = null; float bestScore = float.MaxValue; int bestTears = int.MaxValue;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var sk = attempt == 3 ? FitSkeleton(template, sm.V) : template;
                SkinWeights.Segments(sk, out var a, out var b);
                var inf = new SkinWeights.Influence[sm.V.Count];
                for (int i = 0; i < inf.Length; i++) inf[i] = SkinWeights.Compute(sm.V[i], a, b);
                if (attempt >= 2) inf = SkinWeights.Smooth(inf, weld, sk.Count, attempt == 2 ? 2 : 4, 0.5f);
                Tears(sk, sm.V, tris, inf, testClips, out float worst, out int tears);
                sb.Append("Ansatz ").Append(attempt).Append(": max. Streckung ").Append(worst.ToString("0.00")).Append(", Risskanten ").Append(tears).Append(" · ");
                if (tears < bestTears || (tears == bestTears && worst < bestScore)) { bestTears = tears; bestScore = worst; best = sk; bestW = inf; }
                if (tears == 0) break;
            }
            report = sb.ToString();
            if (bestTears > 0) { report += "→ Risse sichtbar, Ersatz durch MeshKit-Rig, Meshy-Modell wird Statue"; return null; }
            report += "→ übernommen";
            var r = CreateSkeleton(parent, best);
            var meshAsset = new Mesh { name = modelName + "_Skinned" };
            if (sm.V.Count > 65000) meshAsset.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshAsset.SetVertices(sm.V); meshAsset.subMeshCount = sm.Sub.Count;
            for (int s = 0; s < sm.Sub.Count; s++) meshAsset.SetTriangles(sm.Sub[s], s);
            var bw = new BoneWeight[bestW.Length];
            for (int i = 0; i < bw.Length; i++) { var f = bestW[i]; bw[i] = new BoneWeight { boneIndex0 = f.B0, boneIndex1 = f.B1, boneIndex2 = f.B2, boneIndex3 = f.B3, weight0 = f.W0, weight1 = f.W1, weight2 = f.W2, weight3 = f.W3 }; }
            meshAsset.boneWeights = bw;
            var bind = new Matrix4x4[r.Bones.Length];
            for (int i = 0; i < bind.Length; i++) bind[i] = r.Bones[i].worldToLocalMatrix * r.Visual.transform.localToWorldMatrix;
            meshAsset.bindposes = bind; meshAsset.RecalculateNormals(); meshAsset.RecalculateBounds();
            EnsureFolder(MeshDir);
            AssetDatabase.CreateAsset(meshAsset, MeshDir + "/" + modelName + "_Skinned.asset");
            var go = new GameObject("Skin"); go.transform.SetParent(r.Visual.transform, false);
            var smr = go.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = meshAsset; smr.bones = r.Bones; smr.rootBone = r.Bones[0]; smr.sharedMaterials = mats.ToArray();
            r.Smr = smr;
            AddSockets(r);
            return r;
        }

        static void MakeReadable(Mesh mesh)
        {
            string path = AssetDatabase.GetAssetPath(mesh);
            if (AssetImporter.GetAtPath(path) is ModelImporter imp && !imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
        }

        /// <summary>Verschweißte Dreiecksliste (gleiche Positionen = ein Knoten), damit die Glättung über UV-Nähte hinweg wirkt.</summary>
        static int[] Weld(List<Vector3> v, int[] tris)
        {
            var map = new Dictionary<Vector3Int, int>(); var id = new int[v.Count];
            for (int i = 0; i < v.Count; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 2000f), Mathf.RoundToInt(v[i].y * 2000f), Mathf.RoundToInt(v[i].z * 2000f));
                if (!map.TryGetValue(k, out var j)) { j = i; map[k] = i; }
                id[i] = j;
            }
            // Dreiecke auf die Repräsentanten zeigen lassen, Kanten zusätzlich zu den Duplikaten (für die Glättung je Vertex)
            var r = new List<int>(tris.Length * 2);
            r.AddRange(tris);
            for (int t = 0; t + 2 < tris.Length; t += 3) { r.Add(id[tris[t]]); r.Add(id[tris[t + 1]]); r.Add(id[tris[t + 2]]); }
            for (int i = 0; i < v.Count; i++) if (id[i] != i) { r.Add(i); r.Add(id[i]); r.Add(i); }
            return r.ToArray();
        }

        /// <summary>Skelett an die Silhouette anpassen: Hüfthöhe/Schulterhöhe aus Höhenprofil, Handpositionen aus den seitlichsten Punkten.</summary>
        static SkeletonDef FitSkeleton(SkeletonDef t, List<Vector3> v)
        {
            if (t.Kind != "Humanoid") return t;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in v) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            float h = maxY - minY;
            var fit = SkeletonDef.Humanoid(h);
            // Hände: äußerste Punkte im Bereich 35–75 % der Höhe je Seite
            Vector3 hl = Vector3.zero, hr = Vector3.zero; float xl = 0f, xr = 0f;
            foreach (var p in v)
            {
                float u = (p.y - minY) / h; if (u < 0.35f || u > 0.75f) continue;
                if (p.x < xl) { xl = p.x; hl = p; }
                if (p.x > xr) { xr = p.x; hr = p; }
            }
            var pos = fit.WorldPositions(null, Vector3.zero);
            foreach (var side in new[] { "L", "R" })
            {
                Vector3 hand = side == "L" ? hl : hr; if (hand == Vector3.zero) continue;
                int ua = fit.IndexOf("UpperArm" + side), la = fit.IndexOf("LowerArm" + side), ha = fit.IndexOf("Hand" + side);
                Vector3 shoulder = pos[ua];
                Vector3 dir = hand - shoulder; float len = dir.magnitude;
                var bones = fit.Bones;
                var b1 = bones[la]; b1.Local = dir * 0.52f; bones[la] = b1;
                var b2 = bones[ha]; b2.Local = dir * 0.42f; bones[ha] = b2;
            }
            return fit;
        }

        /// <summary>Riss-Prüfung: Testposen (Frames mit größter Auslenkung) skinnen und gestreckte Kanten zählen.</summary>
        public static void Tears(SkeletonDef sk, List<Vector3> verts, int[] tris, SkinWeights.Influence[] inf, List<ClipData> clips, out float worst, out int tears)
        {
            worst = 1f; tears = 0;
            var bind = verts.ToArray();
            var posed = new Vector3[bind.Length];
            foreach (var c in clips)
            {
                if (c == null || c.Layer == ClipLayer.Additive) continue;
                foreach (float u in new[] { 0.25f, 0.5f, 0.75f })
                {
                    int f = Mathf.Clamp(Mathf.RoundToInt(u * (c.FrameCount - 1)), 0, c.FrameCount - 1);
                    var m = SkinWeights.SkinMatrices(sk, c.Euler[f], Vector3.zero);
                    for (int i = 0; i < bind.Length; i++) posed[i] = SkinWeights.Skin(bind[i], inf[i], m);
                    for (int t = 0; t + 2 < tris.Length; t += 3)
                        for (int e = 0; e < 3; e++)
                        {
                            int i = tris[t + e], j = tris[t + (e + 1) % 3];
                            float lb = Vector3.Distance(bind[i], bind[j]); if (lb < 1e-4f) continue;
                            float ratio = Vector3.Distance(posed[i], posed[j]) / lb;
                            if (ratio > worst) worst = ratio;
                            if (ratio > TearThreshold && lb * ratio > TearMinEdge) tears++;
                        }
                }
            }
        }
    }
}
