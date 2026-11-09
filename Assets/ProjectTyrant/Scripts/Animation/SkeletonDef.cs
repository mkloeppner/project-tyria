using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>
    /// Prozedurales Skelett (rein, getestet): Knochen mit Eltern-Index und Bind-Position (lokal, Bind-Rotation = Identität).
    /// Humanoid: Hüfte, Wirbelsäule ×3, Hals, Kopf, je Seite Schulter/Oberarm/Unterarm/Hand und Oberschenkel/Unterschenkel/Fuß/Zehen.
    /// Vierbeiner: Becken, Brust, Hals, Kopf, Schwanz ×2, vier Beine mit je 3 Knochen (Reittiere, Tiere, Käfer, Vögel, Rochen).
    /// </summary>
    public class SkeletonDef
    {
        public struct Bone { public string Name; public int Parent; public Vector3 Local; }
        public readonly List<Bone> Bones = new List<Bone>();
        public readonly string Kind;
        public SkeletonDef(string kind) { Kind = kind; }

        public const string BodyPath = "Body", RigPath = "Body/Rig";
        public int Count => Bones.Count;
        public int IndexOf(string name) { for (int i = 0; i < Bones.Count; i++) if (Bones[i].Name == name) return i; return -1; }
        int Add(string name, int parent, Vector3 local) { Bones.Add(new Bone { Name = name, Parent = parent, Local = local }); return Bones.Count - 1; }

        /// <summary>Pfad relativ zur Charakter-Wurzel (Animator) für AnimationClip-Kurven, z. B. „Body/Rig/Hips/Spine“.
        /// „Body“ = nicht animierter Versatzknoten (Fuß-IK senkt ihn), „Rig“ = animierter Wurzelversatz der Clips.</summary>
        public string Path(int i)
        {
            var parts = new List<string>();
            for (int b = i; b >= 0; b = Bones[b].Parent) parts.Insert(0, Bones[b].Name);
            return RigPath + "/" + string.Join("/", parts);
        }

        // ---------------- Humanoid ----------------
        public const string Hips = "Hips", Spine = "Spine", Spine1 = "Spine1", Spine2 = "Spine2", Neck = "Neck", Head = "Head";
        public const string ShoulderL = "ShoulderL", UpperArmL = "UpperArmL", LowerArmL = "LowerArmL", HandL = "HandL";
        public const string ShoulderR = "ShoulderR", UpperArmR = "UpperArmR", LowerArmR = "LowerArmR", HandR = "HandR";
        public const string UpperLegL = "UpperLegL", LowerLegL = "LowerLegL", FootL = "FootL", ToesL = "ToesL";
        public const string UpperLegR = "UpperLegR", LowerLegR = "LowerLegR", FootR = "FootR", ToesR = "ToesR";

        public static SkeletonDef Humanoid(float height = 1.8f, float bulk = 1f)
        {
            float s = height / 1.8f;
            var k = new SkeletonDef("Humanoid");
            int hips = k.Add(Hips, -1, new Vector3(0, 0.95f, 0) * s);
            int sp = k.Add(Spine, hips, new Vector3(0, 0.12f, 0) * s);
            int sp1 = k.Add(Spine1, sp, new Vector3(0, 0.16f, 0) * s);
            int sp2 = k.Add(Spine2, sp1, new Vector3(0, 0.16f, 0) * s);
            int neck = k.Add(Neck, sp2, new Vector3(0, 0.17f, 0) * s);
            k.Add(Head, neck, new Vector3(0, 0.1f, 0.01f) * s);
            for (int side = -1; side <= 1; side += 2)
            {
                string L = side < 0 ? "L" : "R";
                int sh = k.Add("Shoulder" + L, sp2, new Vector3(0.06f * side * bulk, 0.12f, 0) * s);
                int ua = k.Add("UpperArm" + L, sh, new Vector3(0.14f * side * bulk, 0f, 0) * s);
                int la = k.Add("LowerArm" + L, ua, new Vector3(0, -0.28f, 0) * s);
                k.Add("Hand" + L, la, new Vector3(0, -0.26f, 0) * s);
                int ul = k.Add("UpperLeg" + L, hips, new Vector3(0.1f * side * bulk, -0.05f, 0) * s);
                int ll = k.Add("LowerLeg" + L, ul, new Vector3(0, -0.42f, 0) * s);
                int ft = k.Add("Foot" + L, ll, new Vector3(0, -0.42f, 0) * s);
                k.Add("Toes" + L, ft, new Vector3(0, -0.06f, 0.13f) * s);
            }
            return k;
        }

        // ---------------- Vierbeiner ----------------
        public const string Pelvis = "Pelvis", Chest = "Chest", QNeck = "QNeck", QHead = "QHead", Tail1 = "Tail1", Tail2 = "Tail2";
        public static readonly string[] Legs = { "FL", "FR", "BL", "BR" };

        /// <param name="length">Rumpflänge</param><param name="legHeight">Hüfthöhe über dem Boden</param><param name="neck">Halslänge (nach vorn/oben)</param><param name="tail">Schwanzlänge</param>
        public static SkeletonDef Quadruped(float length, float legHeight, float neck, float tail, float width = 0.4f, float frontLeg = 1f)
        {
            var k = new SkeletonDef("Quadruped");
            int pelvis = k.Add(Pelvis, -1, new Vector3(0, legHeight, -length * 0.5f));
            int chest = k.Add(Chest, pelvis, new Vector3(0, 0.05f, length));
            int nk = k.Add(QNeck, chest, new Vector3(0, 0.08f, 0.08f));
            k.Add(QHead, nk, new Vector3(0, neck * 0.7f, neck * 0.7f));
            int t1 = k.Add(Tail1, pelvis, new Vector3(0, 0.02f, -0.06f));
            k.Add(Tail2, t1, new Vector3(0, 0, -Mathf.Max(0.05f, tail * 0.5f)));
            foreach (var leg in Legs)
            {
                bool front = leg[0] == 'F'; float side = leg[1] == 'L' ? -1f : 1f;
                int parent = front ? chest : pelvis;
                float lh = front ? legHeight * frontLeg : legHeight;
                int up = k.Add(leg + "_Upper", parent, new Vector3(width * 0.5f * side, -0.05f, 0));
                int lo = k.Add(leg + "_Lower", up, new Vector3(0, -lh * 0.5f + 0.025f, 0));
                k.Add(leg + "_Foot", lo, new Vector3(0, -lh * 0.5f + 0.025f, 0.02f));
            }
            return k;
        }

        /// <summary>Vorwärtskinematik: Weltpositionen (relativ zur Wurzel) für Euler-Offsets je Knochen (Grad) und Hüft-/Becken-Versatz.</summary>
        public Vector3[] WorldPositions(Vector3[] euler, Vector3 rootOffset, Quaternion[] rotOut = null)
        {
            var pos = new Vector3[Count]; var rot = new Quaternion[Count];
            for (int i = 0; i < Count; i++)
            {
                var b = Bones[i];
                var lr = euler != null && i < euler.Length ? Quaternion.Euler(euler[i]) : Quaternion.identity;
                if (b.Parent < 0) { pos[i] = b.Local + rootOffset; rot[i] = lr; }
                else { pos[i] = pos[b.Parent] + rot[b.Parent] * b.Local; rot[i] = rot[b.Parent] * lr; }
            }
            if (rotOut != null) for (int i = 0; i < Count && i < rotOut.Length; i++) rotOut[i] = rot[i];
            return pos;
        }
    }
}
