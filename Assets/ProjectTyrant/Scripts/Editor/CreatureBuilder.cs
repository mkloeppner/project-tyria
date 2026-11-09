using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Prozedurale Kreaturen (Lauf 4, ab Lauf 5 geriggt): Vierbeiner, Humanoide, Käfer, Vögel, Raptor, Springer, Rochen.
    /// Jede Kreatur ist ein geskinnter MeshKit-Charakter auf Vierbeiner- bzw. Humanoid-Skelett mit <see cref="Anim.CharacterAnimator"/>.
    /// </summary>
    public static class CreatureBuilder
    {
        public class Look
        {
            public Color Body = new Color(0.5f, 0.5f, 0.5f), Belly = new Color(0.7f, 0.7f, 0.7f), Accent = new Color(0.8f, 0.3f, 0.2f), Eye = new Color(1.6f, 0.6f, 0.1f);
            public float Scale = 1f, Glow;
        }

        static RigBuilder.BeastLook B(Look l)
        {
            float g = 1f + l.Glow * 0.25f;
            return new RigBuilder.BeastLook { Body = l.Body * g, Belly = l.Belly, Accent = l.Accent * (1f + l.Glow * 0.4f), Eye = l.Eye, Glow = l.Glow };
        }

        /// <summary>Vierbeiner (Wolf, Ziege, Hase, Kröte, Salamander): Länge, Beinlänge, Rumpfhöhe, Hals, Schwanz, Ohren/Hörner/Schnauze.</summary>
        public static GameObject Quadruped(Transform parent, string key, Look l, float length, float legLen, float bodyH, float neck, float tail, bool ears, bool horns, bool snout)
        {
            var bl = B(l); bl.Ears = ears; bl.Horns = horns; bl.Snout = snout;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Quadruped", bl, length, legLen + bodyH * 0.35f, bodyH, Mathf.Max(0.12f, neck * 0.7f), tail, Mathf.Max(0.18f, bodyH * 0.7f), l.Scale);
        }

        /// <summary>Humanoid (Moorkoloss, Frosthüter, Beschwörer, Golem, Weltboss): Humanoid-Skelett, optional Schild/Stab/Glutrisse.</summary>
        public static GameObject Humanoid(Transform parent, string key, Look l, float height, float bulk, bool hunched, bool shield, bool staff, bool cracks)
        {
            var hl = new RigBuilder.HumanLook
            {
                Cloth = l.Body, Pants = l.Body * 0.8f, Skin = l.Belly, Leather = l.Body * 0.7f, Trim = l.Accent * (1f + l.Glow * 0.5f), Metal = l.Accent, Hair = l.Body * 0.6f,
                Eye = l.Eye, GlowEyes = true, Shield = shield, Robe = staff, Cracks = cracks, Hood = hunched, Height = height * l.Scale, Bulk = bulk, Belly = hunched, Pauldrons = shield
            };
            var r = RigBuilder.Humanoid(parent, "PT_Creature_" + key, hl);
            CharacterRigs.Finish(r, staff ? "Staff" : "Hammer", true);
            if (staff)
            {
                var st = Child(r.HandSocket, "Staff", Vector3.zero).transform;
                Part(st, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.07f, 0.9f, 0.07f), M("StaffWood", new Color(0.3f, 0.2f, 0.14f)), "Staff");
                Part(st, PrimitiveType.Sphere, new Vector3(0, 1.3f, 0), Vector3.one * 0.22f, M(key + "_Acc", l.Accent, l.Glow), "StaffOrb").AddComponent<Bobber>();
            }
            else if (!shield && bulk >= 1.4f)
            {
                var fist = Child(r.HandSocket, "Fist", Vector3.zero).transform;
                Part(fist, PrimitiveType.Sphere, new Vector3(0, 0.02f, 0), Vector3.one * 0.18f * bulk, M(key + "_Fist", l.Body * 0.8f), "Fist");
            }
            return r.Visual;
        }

        static Material M(string n, Color c, float glow = 0f) => Mat("Cr_" + n, c, 0.3f, glow > 0f ? c * glow : (Color?)null, 0.5f);

        /// <summary>Käfer/Kriecher: Panzer, 4 animierte + 2 starre Beine, Fühler.</summary>
        public static GameObject Beetle(Transform parent, string key, Look l)
        {
            var bl = B(l); bl.Shell = true; bl.Antennae = true;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Beetle", bl, 0.9f, 0.4f, 0.45f, 0.12f, 0.05f, 0.76f, l.Scale);
        }

        /// <summary>Stelzvogel (Reiher): langer Hals, Schnabel, Flügel an den Vorderbein-Knochen.</summary>
        public static GameObject Bird(Transform parent, string key, Look l)
        {
            var bl = B(l); bl.Beak = true; bl.Belly = l.Body * 1.05f;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Bird", bl, 0.45f, 0.85f, 0.32f, 0.5f, 0.25f, 0.32f, l.Scale);
        }

        /// <summary>Raptor (Zweibeiner): waagrechter Körper, kräftige Hinterbeine, kleine Arme, langer Schwanz, Kamm, Sattel.</summary>
        public static GameObject Raptor(Transform parent, string key, Look l, bool mount = false)
        {
            var bl = B(l); bl.Crest = true; bl.Snout = true; bl.Saddle = mount;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Raptor", bl, 1.3f, 1.15f, 0.9f, 0.75f, 1.1f, 0.7f, l.Scale, mount);
        }

        /// <summary>Springer (hasenartig): große Hinterläufe, lange Ohren, Sattel.</summary>
        public static GameObject Springer(Transform parent, string key, Look l, bool mount = false)
        {
            var bl = B(l); bl.LongEars = true; bl.BigHind = true; bl.Snout = true; bl.Saddle = mount;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Springer", bl, 1.1f, 1.05f, 0.9f, 0.4f, 0.25f, 0.62f, l.Scale, mount);
        }

        /// <summary>Rochen/Skimmer: flacher Körper, vier Flossen-Knochen, langer Schwanz (schwebt).</summary>
        public static GameObject Ray(Transform parent, string key, Look l, bool mount = false)
        {
            var bl = B(l); bl.Spots = true; bl.Saddle = mount;
            return CharacterRigs.Beast(parent, "PT_Creature_" + key, "Ray", bl, 1.3f, 0.9f, 0.5f, 0.1f, 1.1f, 1.0f, l.Scale, mount);
        }
    }
}
