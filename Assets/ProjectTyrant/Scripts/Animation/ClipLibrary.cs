using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Anim
{
    /// <summary>Waffen-Stil für die Waffen-Clips (je Stil: Autokette ×3, Schwer, Stoß, Wirbel, Schuss/Wurf, Block).</summary>
    public struct WeaponStyle
    {
        public string Name; public bool TwoHand; public bool Bow; public float Tempo;
        public WeaponStyle(string name, bool twoHand, bool bow, float tempo) { Name = name; TwoHand = twoHand; Bow = bow; Tempo = tempo; }
    }

    /// <summary>Körperbau eines Vierbeiners/Tiers für Skelett und Gangarten.</summary>
    public struct CreatureProfile
    {
        public enum GaitKind { Legs, Wings, Fins, Biped }
        public string Name; public float Length, LegHeight, Neck, Tail, Width, FrontLeg; public GaitKind Gait;
        public CreatureProfile(string name, float length, float legHeight, float neck, float tail, float width, GaitKind gait, float frontLeg = 1f)
        { Name = name; Length = length; LegHeight = legHeight; Neck = neck; Tail = tail; Width = width; Gait = gait; FrontLeg = frontLeg; }
        public SkeletonDef Skeleton() => SkeletonDef.Quadruped(Length, LegHeight, Neck, Tail, Width, FrontLeg);
    }

    /// <summary>
    /// Alle prozeduralen Clips (rein, deterministisch, 30 Hz). Fortbewegung als Phasenfunktionen, Aktionen als Schlüsselposen mit Easing,
    /// Sekundärbewegung als Nachziehen (Kopf, Hals, Brust, Hände, Schwanz) mit 2 Frames Verzögerung.
    /// Konvention (Bind = Identität, Gliedmaßen zeigen nach −Y): Bein/Arm nach vorn = −X, Knie beugen = +X am Unterschenkel,
    /// Ellbogen beugen = −X am Unterarm, Arm links abspreizen = −Z / rechts = +Z, Rumpf vorbeugen = +X, Drehung nach rechts = +Y.
    /// </summary>
    public static class ClipLibrary
    {
        public static readonly WeaponStyle[] Styles =
        {
            new WeaponStyle("Greatsword", true, false, 1.15f),
            new WeaponStyle("Staff", true, false, 1.0f),
            new WeaponStyle("Sword", false, false, 0.85f),
            new WeaponStyle("Dagger", false, false, 0.7f),
            new WeaponStyle("Hammer", true, false, 1.3f),
            new WeaponStyle("Axe", false, false, 0.9f),
            new WeaponStyle("Bow", true, true, 1.0f),
            new WeaponStyle("Scepter", false, false, 0.9f),
        };

        public static readonly CreatureProfile[] Creatures =
        {
            new CreatureProfile("Quadruped", 1.0f, 0.7f, 0.35f, 0.5f, 0.42f, CreatureProfile.GaitKind.Legs),
            new CreatureProfile("Beetle", 0.9f, 0.35f, 0.15f, 0.05f, 0.76f, CreatureProfile.GaitKind.Legs),
            new CreatureProfile("Bird", 0.4f, 0.7f, 0.25f, 0.2f, 0.5f, CreatureProfile.GaitKind.Wings, 0.6f),
            new CreatureProfile("Ray", 1.2f, 0.9f, 0.1f, 0.8f, 0.9f, CreatureProfile.GaitKind.Fins),
            new CreatureProfile("Raptor", 1.3f, 1.05f, 0.6f, 1.0f, 0.64f, CreatureProfile.GaitKind.Biped, 0.4f),
            new CreatureProfile("Springer", 0.9f, 0.9f, 0.5f, 0.3f, 0.6f, CreatureProfile.GaitKind.Legs),
        };

        public static readonly string[] HumanoidCoreClips =
        {
            "Idle", "Idle2", "Walk", "Run", "StrafeL", "StrafeR", "Back", "Sprint", "JumpStart", "JumpAir", "JumpLand",
            "DodgeF", "DodgeB", "DodgeL", "DodgeR", "Glide", "Mount", "Ride", "HitLight", "HitLightB", "HitLightL", "HitLightR", "HitHeavy", "Knockdown", "Getup",
            "DownedCrawl", "Bandage", "Death", "Interact", "Communion", "CastChannel", "Cast1H", "Cast2H", "WeaponSwap",
            "Breath", "Work", "Talk", "GuardIdle", "Sit", "Cheer", "Telegraph"
        };
        public static readonly string[] WeaponClipKinds = { "Auto1", "Auto2", "Auto3", "Heavy", "Thrust", "Whirl", "Shoot", "Block" };
        public static readonly string[] CreatureClipKinds = { "Idle", "Walk", "Run", "Jump", "Attack", "Hit", "Death", "Sniff" };
        public static readonly string[] MountClipKinds = { "MIdle", "MRun", "MLeap", "MCharge", "MHover", "MEngage", "MKnockOff" };

        // ------------------------------------------------------------ Grundlagen
        public static Dictionary<string, int> HumanoidAlias(SkeletonDef sk)
        {
            var m = new Dictionary<string, int>();
            void A(string a, string b) => m[a] = sk.IndexOf(b);
            A("H", SkeletonDef.Hips); A("S", SkeletonDef.Spine); A("S1", SkeletonDef.Spine1); A("S2", SkeletonDef.Spine2);
            A("N", SkeletonDef.Neck); A("Hd", SkeletonDef.Head);
            foreach (var s in new[] { "L", "R" })
            {
                A("SH" + s, "Shoulder" + s); A("UA" + s, "UpperArm" + s); A("LA" + s, "LowerArm" + s); A("HA" + s, "Hand" + s);
                A("UL" + s, "UpperLeg" + s); A("LL" + s, "LowerLeg" + s); A("FT" + s, "Foot" + s); A("TO" + s, "Toes" + s);
            }
            return m;
        }

        public static Dictionary<string, int> CreatureAlias(SkeletonDef sk)
        {
            var m = new Dictionary<string, int>();
            m["P"] = sk.IndexOf(SkeletonDef.Pelvis); m["C"] = sk.IndexOf(SkeletonDef.Chest); m["N"] = sk.IndexOf(SkeletonDef.QNeck);
            m["Hd"] = sk.IndexOf(SkeletonDef.QHead); m["T1"] = sk.IndexOf(SkeletonDef.Tail1); m["T2"] = sk.IndexOf(SkeletonDef.Tail2);
            foreach (var l in SkeletonDef.Legs) { m[l + "U"] = sk.IndexOf(l + "_Upper"); m[l + "L"] = sk.IndexOf(l + "_Lower"); m[l + "F"] = sk.IndexOf(l + "_Foot"); }
            return m;
        }

        public static ClipData Bake(SkeletonDef sk, Dictionary<string, int> alias, string name, float length, bool loop, ClipLayer layer,
                                    Action<float, PoseBuilder> pose, int[] secondary = null)
        {
            int frames = ClipData.FramesFor(length);
            var c = new ClipData { Name = name, Rig = sk.Kind, Length = length, Loop = loop, Layer = layer, Euler = new Vector3[frames][], Root = new Vector3[frames] };
            var p = new PoseBuilder(sk, alias);
            for (int f = 0; f < frames; f++)
            {
                float t = loop ? (f == frames - 1 ? 0f : f / ClipData.FrameRate) : Mathf.Min(length, f / ClipData.FrameRate);
                p.Clear(); pose(t, p);
                c.Euler[f] = (Vector3[])p.E.Clone(); c.Root[f] = p.Root;
            }
            if (secondary != null && secondary.Length > 0)
            {
                var src = new Vector3[frames][];
                for (int f = 0; f < frames; f++) src[f] = (Vector3[])c.Euler[f].Clone();
                int cyc = loop ? frames - 1 : frames;
                for (int f = 0; f < frames; f++)
                {
                    int g = f - 2; if (g < 0) g = loop ? (g + cyc) % cyc : 0;
                    foreach (var b in secondary) if (b >= 0) c.Euler[f][b] = src[f][b] * 0.6f + src[g][b] * 0.4f;
                }
                if (loop) c.Euler[frames - 1] = (Vector3[])c.Euler[0].Clone();
            }
            return c;
        }

        static int[] Sec(PoseBuilder pb, params string[] n) { var r = new int[n.Length]; for (int i = 0; i < n.Length; i++) r[i] = pb.Index(n[i]); return r; }
        static int[] Sec(SkeletonDef sk, Dictionary<string, int> a, params string[] n) { var r = new List<int>(); foreach (var x in n) if (a.TryGetValue(x, out var i)) r.Add(i); return r.ToArray(); }

        /// <summary>
        /// Fußkontakt-Marker an das echte Höhenminimum des Fußes im ganzen Zyklus (je Fuß) um den erwarteten Kontakt legen
        /// und die Wurzel so absenken, dass der tiefste Fuß Ruhehöhe erreicht (keine schwebenden Läufe).
        /// </summary>
        public static void GroundContacts(SkeletonDef sk, ClipData c, int footL, int footR, float restY, float expectL, float expectR)
        {
            var pl = BonePath(sk, c, footL); var pr = BonePath(sk, c, footR);
            float mn = float.MaxValue; foreach (var v in pl) mn = Mathf.Min(mn, v.y); foreach (var v in pr) mn = Mathf.Min(mn, v.y);
            float shift = restY - mn;
            for (int f = 0; f < c.FrameCount; f++) c.Root[f].y += shift;
            c.AddMarker(Marker.FootL, MinIn(pl, c, expectL)).AddMarker(Marker.FootR, MinIn(pr, c, expectR));
        }

        static float MinIn(Vector3[] path, ClipData c, float expect)
        {
            int n = c.FrameCount - 1, best = 0; float by = float.MaxValue;
            for (int k = -(n / 2); k < n - n / 2; k++)
            {
                int f = ((Mathf.RoundToInt(expect * n) + k) % n + n) % n;
                if (path[f].y < by - 1e-5f) { by = path[f].y; best = f; }
            }
            return best / ClipData.FrameRate;
        }

        /// <summary>Hüft-Absenkung, damit der Fuß in der Standphase etwa auf gleicher Höhe bleibt (Beinlänge × (1−cos A)/2).</summary>
        public static float BobFor(float legLength, float amplitudeDeg) => legLength * (1f - Mathf.Cos(amplitudeDeg * Mathf.Deg2Rad)) * 0.5f;

        const float TwoPi = Mathf.PI * 2f;

        // ------------------------------------------------------------ Humanoid
        public static List<ClipData> Humanoid(SkeletonDef sk = null)
        {
            sk = sk ?? SkeletonDef.Humanoid();
            var al = HumanoidAlias(sk);
            float leg = Mathf.Abs(sk.Bones[sk.IndexOf(SkeletonDef.LowerLegL)].Local.y) + Mathf.Abs(sk.Bones[sk.IndexOf(SkeletonDef.FootL)].Local.y);
            var list = new List<ClipData>();
            float restFoot = sk.WorldPositions(null, Vector3.zero)[sk.IndexOf(SkeletonDef.FootL)].y;
            var sec = Sec(sk, al, "Hd", "N", "S2", "HAL", "HAR");
            ClipData B(string n, float len, bool loop, ClipLayer layer, Action<float, PoseBuilder> f, bool stance = true)
            {
                var c = Bake(sk, al, n, len, loop, layer, (t, p) => { if (stance) Stance(p); f(t, p); }, sec);
                list.Add(c); return c;
            }
            ClipData K(string n, float len, ClipLayer layer, Action<KeyTrack> keys, bool loop = false, bool stance = true)
            {
                KeyTrack kt = null;
                return B(n, len, loop, layer, (t, p) => { if (kt == null) { kt = new KeyTrack(p); keys(kt); } kt.Apply(t); }, stance);
            }

            // Fortbewegung (Phasen gemeinsam, Fußkontakt L bei ¼, R bei ¾ des Zyklus)
            void Loco(string n, float len, float amp, float knee, float arm, float elbow, float lean, int mode, float bobScale = 1f)
            {
                float bob = BobFor(leg, amp) * bobScale;
                var c = B(n, len, true, ClipLayer.Locomotion, (t, p) => Gait(p, TwoPi * t / len, amp, knee, arm, elbow, bob, lean, mode));
                GroundContacts(sk, c, sk.IndexOf(SkeletonDef.FootL), sk.IndexOf(SkeletonDef.FootR), restFoot, 0.25f, 0.75f);
            }
            B("Idle", 3f, true, ClipLayer.Locomotion, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 3f);
                p.R("S1", 1.5f * b); p.R("S2", 1f * b); p.R("Hd", -1f * b); p.R("UAL", 0, 0, -1.5f * b); p.R("UAR", 0, 0, 1.5f * b);
                p.R("ULL", -2f, 0, -2f); p.R("ULR", 1f, 0, 3f); p.Root.y = -0.005f * (1f + b);
            });
            B("Idle2", 4f, true, ClipLayer.Locomotion, (t, p) =>
            {
                float w = Mathf.Sin(TwoPi * t / 4f), look = Mathf.Sin(TwoPi * t / 4f * 2f);
                p.R("H", 0, 0, 4f * w); p.R("S", 0, 0, -3f * w); p.R("ULL", 0, 0, -4f * w); p.R("ULR", 0, 0, -4f * w);
                p.R("LLL", 6f * Mathf.Max(0, -w)); p.R("LLR", 6f * Mathf.Max(0, w));
                p.R("Hd", 0, 28f * look * Mathf.Abs(w), 0); p.R("N", 0, 8f * look, 0); p.Root.x = 0.03f * w;
                p.R("UAL", 2f * look); p.R("UAR", -2f * look);
            });
            Loco("Walk", 1.1f, 26f, 40f, 18f, 12f, 3f, 0);
            Loco("Run", 0.68f, 40f, 95f, 35f, 75f, 10f, 0, 0.8f);
            Loco("Sprint", 0.56f, 50f, 115f, 50f, 85f, 18f, 0, 0.7f);
            Loco("StrafeL", 1.0f, 20f, 30f, 8f, 15f, 2f, -1);
            Loco("StrafeR", 1.0f, 20f, 30f, 8f, 15f, 2f, 1);
            Loco("Back", 1.2f, 20f, 30f, 10f, 15f, -2f, 2);

            K("JumpStart", 0.25f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.12f, "ULL:-40 ULR:-40 LLL:70 LLR:70 FTL:-30 FTR:-30 S:15 UAL:30 UAR:30 root:0,-0.18,0", 'o')
                .Key(0.25f, "ULL:-10 ULR:-5 LLL:15 LLR:10 FTL:25 FTR:25 UAL:-60,0,-15 UAR:-60,0,15 S:-4 root:0,0.05,0", 'o'))
                .AddMarker(Marker.Effect, 0.2f);
            B("JumpAir", 0.6f, true, ClipLayer.FullBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t / 0.6f);
                p.R("ULL", -45f + 6f * s); p.R("ULR", -20f - 6f * s); p.R("LLL", 70f); p.R("LLR", 45f); p.R("FTL", 20f); p.R("FTR", 15f);
                p.R("UAL", -30f, 0, -40f + 6f * s); p.R("UAR", -20f, 0, 40f - 6f * s); p.R("LAL", -20f); p.R("LAR", -20f); p.R("S", 6f);
            });
            K("JumpLand", 0.35f, ClipLayer.FullBody, k => k
                .Key(0f, "ULL:-30 ULR:-25 LLL:35 LLR:30 UAL:-20,0,-30 UAR:-20,0,30")
                .Key(0.08f, "ULL:-55 ULR:-55 LLL:95 LLR:95 FTL:-38 FTR:-38 S:22 Hd:-8 UAL:-25,0,-20 UAR:-25,0,20 root:0,-0.28,0", 'o')
                .Key(0.35f, "", 's'))
                .AddMarker(Marker.FootL, 0.08f).AddMarker(Marker.FootR, 0.08f).AddMarker(Marker.End, 0.3f);

            // Ausweichen: Fortbewegung kommt aus dem Spielcode (DodgeModel), der Clip trägt nur Höhe/Rotation – kein Doppelversatz
            const string tuck = "ULL:-100 ULR:-100 LLL:125 LLR:125 S:35 S1:15 Hd:25 UAL:-70,0,-10 UAR:-70,0,10 LAL:-90 LAR:-90";
            K("DodgeF", 0.6f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.08f, "ULL:-30 ULR:-30 LLL:40 LLR:40 S:25 root:0,-0.15,0", 'o')
                .Key(0.18f, tuck + " H:90 root:0,-0.45,0", 'l')
                .Key(0.32f, tuck + " H:210 root:0,-0.55,0", 'l')
                .Key(0.46f, tuck + " H:330 root:0,-0.4,0", 'l')
                .Key(0.6f, "H:360 root:0,0,0", 'o'))
                .AddMarker(Marker.Effect, 0.05f).AddMarker(Marker.End, 0.5f);
            K("DodgeB", 0.55f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.1f, "ULL:-30 ULR:-30 LLL:50 LLR:50 S:10 root:0,-0.12,0", 'o')
                .Key(0.28f, "H:-25 ULL:-40 ULR:-20 LLL:60 LLR:40 S:-10 Hd:12 UAL:-50,0,-35 UAR:-50,0,35 root:0,0.25,0", 'o')
                .Key(0.42f, "ULL:-40 ULR:-40 LLL:70 LLR:70 S:18 root:0,-0.15,0", 'i')
                .Key(0.55f, ""))
                .AddMarker(Marker.Effect, 0.05f).AddMarker(Marker.FootL, 0.42f).AddMarker(Marker.End, 0.48f);
            foreach (var side in new[] { -1, 1 })
            {
                string n = side < 0 ? "DodgeL" : "DodgeR"; float s = side;
                string far = side < 0 ? "R" : "L", near = side < 0 ? "L" : "R";
                K(n, 0.5f, ClipLayer.FullBody, k => k
                    .Key(0f, "")
                    .Key(0.08f, $"ULL:-25 ULR:-25 LLL:45 LLR:45 S:10 root:0,-0.12,0", 'o')
                    .Key(0.25f, $"H:0,0,{-22 * s} S:0,0,{10 * s} UL{near}:-20,0,{30 * s} UL{far}:-20,0,{-10 * s} LL{near}:40 LL{far}:60 UA{far}:-30,0,{-50 * s} root:0,0.2,0", 'o')
                    .Key(0.4f, $"ULL:-35 ULR:-35 LLL:60 LLR:60 S:15 root:0,-0.15,0", 'i')
                    .Key(0.5f, ""))
                    .AddMarker(Marker.Effect, 0.05f).AddMarker(side < 0 ? Marker.FootL : Marker.FootR, 0.4f).AddMarker(Marker.End, 0.45f);
            }
            B("Glide", 1.6f, true, ClipLayer.FullBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t / 1.6f);
                p.R("UAL", -10f, 0, -80f + 5f * s); p.R("UAR", -10f, 0, 80f + 5f * s); p.R("LAL", -5f); p.R("LAR", -5f);
                p.R("ULL", 12f); p.R("ULR", 18f); p.R("LLL", 25f); p.R("LLR", 35f); p.R("FTL", 30f); p.R("FTR", 30f);
                p.R("H", 0, 0, 6f * s); p.R("S", 8f); p.R("Hd", -12f);
            });
            // Sitzen: Hüfte 0,82 m tiefer → Gesäß auf der Sitzhöhe des Reittiers (MountDefinition.SeatHeight = Satteloberkante)
            const string ride = "ULL:-75,0,-28 ULR:-75,0,28 LLL:85 LLR:85 FTL:-10 FTR:-10 S:8 UAL:-35,0,-5 UAR:-35,0,5 LAL:-55 LAR:-55 root:0,-0.82,0";
            K("Mount", 0.6f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.12f, "ULL:-30 ULR:-30 LLL:50 LLR:50 S:15 root:0,-0.12,0", 'o')
                .Key(0.32f, "ULR:-60,0,70 LLR:40 ULL:-15 LLL:30 S:20 UAL:-60 UAR:-60 root:0.2,0.45,0", 'o')
                .Key(0.6f, ride, 's'))
                .AddMarker(Marker.Effect, 0.35f).AddMarker(Marker.End, 0.55f);
            B("Ride", 0.6f, true, ClipLayer.FullBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t / 0.6f);
                new KeyTrack(p).Key(0, ride).Apply(0);
                p.R("S", 4f * s); p.R("Hd", -3f * s); p.R("UAL", 4f * s); p.R("UAR", 4f * s); p.Root.y += 0.03f * s;
            });
            K("HitLight", 0.3f, ClipLayer.Additive, k => k.Key(0, "").Key(0.06f, "S:-10 S1:-6 Hd:-12,0,6 UAL:-8 UAR:-8", 'o').Key(0.3f, ""), false, false);
            // Richtungs-Varianten (Angreifer hinten / links / rechts): Rumpf weicht vom Angreifer weg
            K("HitLightB", 0.3f, ClipLayer.Additive, k => k.Key(0, "").Key(0.06f, "S:12 S1:6 Hd:14 UAL:10 UAR:10", 'o').Key(0.3f, ""), false, false);
            K("HitLightL", 0.3f, ClipLayer.Additive, k => k.Key(0, "").Key(0.06f, "S:0,8,-10 S1:0,4,-6 Hd:0,10,-12 UAL:-12,0,-10", 'o').Key(0.3f, ""), false, false);
            K("HitLightR", 0.3f, ClipLayer.Additive, k => k.Key(0, "").Key(0.06f, "S:0,-8,10 S1:0,-4,6 Hd:0,-10,12 UAR:-12,0,10", 'o').Key(0.3f, ""), false, false);
            K("HitHeavy", 0.6f, ClipLayer.Additive, k => k.Key(0, "").Key(0.08f, "S:-22 S1:-12 Hd:-20,0,10 UAL:-30,0,-20 UAR:-30,0,20 LAL:-30 LAR:-30 H:0,0,6", 'o')
                .Key(0.3f, "S:-8 S1:-4 Hd:-5", 's').Key(0.6f, ""), false, false);
            B("Breath", 4f, true, ClipLayer.Additive, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 4f);
                p.R("S1", 0.8f * b); p.R("S2", 1.2f * b); p.R("SHL", 0, 0, 0.8f * b); p.R("SHR", 0, 0, -0.8f * b); p.R("Hd", -0.6f * b);
            }, false);

            const string lying = "H:-90 S:5 Hd:15 UAL:-20,0,-60 UAR:-20,0,60 ULL:-10 ULR:-5 LLL:15 LLR:10 FTL:-30 FTR:-30 root:0,-0.82,-0.5";
            K("Knockdown", 0.8f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.22f, "H:-35 S:-15 Hd:-20 ULL:-40 ULR:-30 LLL:30 LLR:20 UAL:-70,0,-40 UAR:-70,0,40 root:0,-0.35,-0.2", 'i')
                .Key(0.5f, lying, 'i')
                .Key(0.8f, lying, 's'))
                .AddMarker(Marker.Hit, 0.5f).AddMarker(Marker.End, 0.8f);
            K("Getup", 1.0f, ClipLayer.FullBody, k => k
                .Key(0f, lying)
                .Key(0.35f, "H:-25 S:35 ULL:-95 ULR:-95 LLL:110 LLR:110 FTL:-20 FTR:-20 UAL:30,0,-20 UAR:30,0,20 root:0,-0.62,-0.3", 's')
                .Key(0.7f, "S:20 ULL:-90 LLL:90 ULR:0 LLR:90 FTR:40 UAL:-20 UAR:-20 root:0,-0.38,0", 's')
                .Key(1.0f, "", 's'))
                .AddMarker(Marker.FootL, 0.7f).AddMarker(Marker.End, 0.95f);
            B("DownedCrawl", 1.2f, true, ClipLayer.FullBody, (t, p) =>
            {
                float ph = TwoPi * t / 1.2f, s = Mathf.Sin(ph);
                new KeyTrack(p).Key(0, "H:20 S:30 S1:10 Hd:-30 ULL:-80 ULR:-80 LLL:120 LLR:120 FTL:30 FTR:30 root:0,-0.55,0").Apply(0);
                p.R("UAL", -70f - 25f * s, 0, -10f); p.R("UAR", -70f + 25f * s, 0, 10f); p.R("LAL", -20f - 15f * Mathf.Max(0, s)); p.R("LAR", -20f - 15f * Mathf.Max(0, -s));
                p.R("ULL", 10f * s); p.R("ULR", -10f * s); p.R("H", 0, 0, 4f * s);
            });
            const string kneel = "ULL:-90 LLL:90 ULR:-5 LLR:95 FTR:40 FTL:-5 root:0,-0.42,0";
            B("Bandage", 1.0f, true, ClipLayer.FullBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t);
                new KeyTrack(p).Key(0, kneel + " S:20 Hd:25 UAL:-45,0,25 UAR:-45,0,-25 LAL:-100 LAR:-100").Apply(0);
                p.R("UAL", 0, 15f * s, 0); p.R("UAR", 0, -15f * s, 0); p.R("HAL", 0, 0, 25f * s); p.R("HAR", 0, 0, -25f * s);
            }).AddMarker(Marker.Effect, 0.5f);
            K("Death", 1.2f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.3f, "ULL:-30 ULR:-30 LLL:65 LLR:65 FTL:-30 FTR:-30 S:30 Hd:25 UAL:-20 UAR:-20 root:0,-0.3,0", 'i')
                .Key(0.75f, "H:60,0,25 S:20 Hd:20 ULL:-40 ULR:-20 LLL:60 LLR:30 UAL:-60,0,-30 UAR:-40,0,40 root:0,-0.65,0.3", 'i')
                .Key(1.2f, "H:88,0,15 S:5 Hd:-10,30,0 ULL:-5 ULR:5 LLL:15 LLR:5 UAL:-120,0,-40 UAR:-100,0,50 root:0,-0.84,0.5", 'b'))
                .AddMarker(Marker.Hit, 0.75f).AddMarker(Marker.End, 1.2f);
            K("Interact", 1.0f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.45f, "S:18 S1:6 Hd:15 UAR:-75,0,-10 LAR:-25 UAL:-15", 's')
                .Key(0.6f, "S:18 S1:6 Hd:15 UAR:-70,0,-10 LAR:-35 HAR:30 UAL:-15", 's')
                .Key(1.0f, ""))
                .AddMarker(Marker.Effect, 0.5f).AddMarker(Marker.End, 0.9f);
            B("Communion", 2.0f, true, ClipLayer.FullBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t / 2f);
                new KeyTrack(p).Key(0, kneel + " S:-6 Hd:-25 UAL:-155,0,-25 UAR:-155,0,25 LAL:-10 LAR:-10").Apply(0);
                p.R("UAL", 0, 0, -6f * s); p.R("UAR", 0, 0, 6f * s); p.R("S", 3f * s); p.R("Hd", -4f * s);
            }).AddMarker(Marker.Effect, 0.2f);
            B("CastChannel", 1.0f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t * 2f);
                p.R("UAL", -80f + 3f * s, 0, 12f); p.R("UAR", -80f - 3f * s, 0, -12f); p.R("LAL", -20f); p.R("LAR", -20f);
                p.R("HAL", -30f, 0, 10f * s); p.R("HAR", -30f, 0, -10f * s); p.R("S", 6f); p.R("Hd", -5f);
            }).AddMarker(Marker.Effect, 0f);
            // kurze Nachwirkung (Rest ≈ 45 % des Ausholens), damit Fernkampf-Rotationen flüssig bleiben
            K("Cast1H", 0.48f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.2f, "UAR:40,0,20 LAR:-80 S1:0,30,0 S:-4", 'o')
                .Key(0.33f, "UAR:-95,0,-5 LAR:-5 HAR:-20 S1:0,-15,0 S:10", 'i')
                .Key(0.48f, "", 's'))
                .AddMarker(Marker.Effect, 0.15f).AddMarker(Marker.Projectile, 0.33f).AddMarker(Marker.End, 0.42f);
            K("Cast2H", 0.78f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.4f, "UAL:-165,0,-15 UAR:-165,0,15 LAL:-30 LAR:-30 S:-12 Hd:-15", 'o')
                .Key(0.55f, "UAL:-70,0,10 UAR:-70,0,-10 LAL:-5 LAR:-5 S:22 Hd:5", 'i')
                .Key(0.78f, "", 's'))
                .AddMarker(Marker.Effect, 0.3f).AddMarker(Marker.Hit, 0.55f).AddMarker(Marker.End, 0.72f);
            K("WeaponSwap", 0.5f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.22f, "UAR:-150,0,25 LAR:-95 S1:0,-20,0 Hd:0,-15,0", 'o')
                .Key(0.5f, ""))
                .AddMarker(Marker.Effect, 0.22f).AddMarker(Marker.End, 0.45f);
            K("Telegraph", 0.6f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.6f, "UAL:-150,0,-30 UAR:-150,0,30 LAL:-40 LAR:-40 S:-15 Hd:-20", 'o'))
                .AddMarker(Marker.Effect, 0f).AddMarker(Marker.End, 0.6f);

            // Dorfbewohner / Wachen
            B("Work", 1.2f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = t / 1.2f;
                float a = u < 0.55f ? Ease.Smooth(u / 0.55f) : 1f - Ease.InQuad((u - 0.55f) / 0.25f);
                if (u >= 0.8f) a = 0f;
                p.R("UAR", -40f - 80f * a, 0, -5f); p.R("LAR", -60f + 10f * a); p.R("S", 18f - 6f * a); p.R("UAL", -35f, 0, 10f); p.R("LAL", -50f); p.R("Hd", 20f);
            }).AddMarker(Marker.Hit, 0.96f);
            B("Talk", 2.5f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 2.5f;
                p.R("UAR", -30f - 20f * Mathf.Max(0, Mathf.Sin(u)), 0, -10f); p.R("LAR", -60f - 15f * Mathf.Sin(u * 2f)); p.R("HAR", 0, 0, 20f * Mathf.Sin(u * 2f));
                p.R("UAL", -15f * Mathf.Max(0, Mathf.Sin(u + 2f)), 0, -5f); p.R("LAL", -30f);
                p.R("Hd", 4f * Mathf.Sin(u * 3f), 10f * Mathf.Sin(u), 0); p.R("S1", 0, 6f * Mathf.Sin(u), 0);
            });
            B("GuardIdle", 3f, true, ClipLayer.Locomotion, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 3f);
                p.R("ULL", 0, 0, -8f); p.R("ULR", 0, 0, 8f); p.R("FTL", 0, 0, 8f); p.R("FTR", 0, 0, -8f);
                p.R("UAR", -25f, 0, -12f); p.R("LAR", -70f); p.R("UAL", -10f, 0, -8f); p.R("LAL", -15f);
                p.R("S1", 1.5f * b - 3f); p.R("Hd", -1f * b, 6f * Mathf.Sin(TwoPi * t / 1.5f) * 0.3f, 0); p.Root.y = -0.01f;
            });
            B("Sit", 3f, true, ClipLayer.FullBody, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 3f);
                new KeyTrack(p).Key(0, "ULL:-88,0,-10 ULR:-88,0,10 LLL:20 LLR:30 FTL:-20 FTR:-20 S:15 UAL:-15,0,-20 UAR:-15,0,20 LAL:-30 LAR:-30 root:0,-0.62,-0.1").Apply(0);
                p.R("S", 2f * b); p.R("Hd", -2f * b, 10f * Mathf.Sin(TwoPi * t / 6f), 0);
            });
            K("Cheer", 1.2f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.3f, "UAL:-160,0,-30 UAR:-160,0,30 LAL:-20 LAR:-20 S:-8 Hd:-20", 'b')
                .Key(0.6f, "UAL:-150,0,-20 UAR:-150,0,20 LAL:-50 LAR:-50 S:-5 Hd:-15")
                .Key(0.9f, "UAL:-160,0,-30 UAR:-160,0,30 LAL:-20 LAR:-20 S:-8 Hd:-20")
                .Key(1.2f, ""))
                .AddMarker(Marker.Effect, 0.3f);

            // Lauf 6: Gesprächs-Posen je Emotion (Oberkörper-Schleifen) und Emotes
            B("Talk_Neutral", 2.5f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 2.5f;
                p.R("UAR", -25f - 15f * Mathf.Max(0, Mathf.Sin(u)), 0, -10f); p.R("LAR", -55f - 12f * Mathf.Sin(u * 2f)); p.R("UAL", -10f * Mathf.Max(0, Mathf.Sin(u + 2f)), 0, -6f); p.R("LAL", -25f);
                p.R("Hd", 3f * Mathf.Sin(u * 3f), 8f * Mathf.Sin(u), 0); p.R("S1", 0, 4f * Mathf.Sin(u), 0);
            });
            B("Talk_Entschlossen", 2.2f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 2.2f, beat = Mathf.Max(0, Mathf.Sin(u * 2f));
                p.R("UAR", -55f - 25f * beat, 0, -18f); p.R("LAR", -85f + 20f * beat); p.R("HAR", 0, 0, -10f); p.R("UAL", -8f, 0, -10f); p.R("LAL", -20f);
                p.R("S", -4f); p.R("S1", -3f, 6f * Mathf.Sin(u), 0); p.R("Hd", -6f + 4f * beat, 4f * Mathf.Sin(u), 0);
            });
            B("Talk_Besorgt", 3f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 3f;
                p.R("UAR", -35f, 0, 25f); p.R("LAR", -110f + 6f * Mathf.Sin(u)); p.R("UAL", -35f, 0, -25f); p.R("LAL", -110f + 6f * Mathf.Sin(u + 1f));
                p.R("S", 8f); p.R("S1", 4f); p.R("Hd", 12f + 3f * Mathf.Sin(u), 6f * Mathf.Sin(u * 0.5f), 4f);
            });
            B("Talk_Amuesiert", 2f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 2f, sh = Mathf.Abs(Mathf.Sin(u * 3f));
                p.R("UAR", -20f, 0, -14f - 6f * sh); p.R("LAR", -70f); p.R("UAL", -20f, 0, 14f + 6f * sh); p.R("LAL", -70f);
                p.R("S1", -5f - 3f * sh, 0, 2f * Mathf.Sin(u)); p.R("Hd", -10f - 4f * sh, 8f * Mathf.Sin(u * 0.5f), 6f);
            });
            B("Talk_Zornig", 1.8f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 1.8f, jab = Mathf.Max(0, Mathf.Sin(u * 2f));
                p.R("UAR", -80f - 20f * jab, 0, -25f); p.R("LAR", -20f - 20f * (1f - jab)); p.R("HAR", 0, 0, 20f); p.R("UAL", -15f, 0, -20f); p.R("LAL", -95f);
                p.R("S", -8f); p.R("S1", -4f, -8f, 0); p.R("Hd", -8f + 5f * jab, -6f, 0);
            });
            B("Talk_Trauernd", 4f, true, ClipLayer.UpperBody, (t, p) =>
            {
                float u = TwoPi * t / 4f;
                p.R("UAR", 4f, 0, 4f); p.R("LAR", -12f); p.R("UAL", 4f, 0, -4f); p.R("LAL", -12f);
                p.R("S", 14f + 2f * Mathf.Sin(u)); p.R("S1", 8f); p.R("Hd", 26f + 3f * Mathf.Sin(u), 0, 3f);
            });
            K("Emote_Winken", 1.6f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.3f, "UAR:-160,0,-35 LAR:-40 HAR:0,0,25 Hd:-5", 'b')
                .Key(0.55f, "UAR:-160,0,-35 LAR:-20 HAR:0,0,-25 Hd:-5")
                .Key(0.8f, "UAR:-160,0,-35 LAR:-40 HAR:0,0,25 Hd:-5")
                .Key(1.05f, "UAR:-160,0,-35 LAR:-20 HAR:0,0,-25 Hd:-5")
                .Key(1.6f, ""))
                .AddMarker(Marker.Effect, 0.3f);
            K("Emote_Jubeln", 1.2f, ClipLayer.UpperBody, k => k
                .Key(0f, "")
                .Key(0.3f, "UAL:-160,0,-30 UAR:-160,0,30 LAL:-20 LAR:-20 S:-8 Hd:-20", 'b')
                .Key(0.6f, "UAL:-150,0,-20 UAR:-150,0,20 LAL:-50 LAR:-50 S:-5 Hd:-15")
                .Key(0.9f, "UAL:-160,0,-30 UAR:-160,0,30 LAL:-20 LAR:-20 S:-8 Hd:-20")
                .Key(1.2f, ""))
                .AddMarker(Marker.Effect, 0.3f);
            K("Emote_Verbeugen", 2.2f, ClipLayer.FullBody, k => k
                .Key(0f, "")
                .Key(0.6f, "S:45 S1:15 Hd:10 UAR:-20,0,-10 LAR:-90 UAL:15,0,-5 LAL:-10 ULL:-10 ULR:-10", 'o')
                .Key(1.5f, "S:45 S1:15 Hd:10 UAR:-20,0,-10 LAR:-90 UAL:15,0,-5 LAL:-10 ULL:-10 ULR:-10")
                .Key(2.2f, ""))
                .AddMarker(Marker.Effect, 0.6f);
            B("Emote_Tanzen", 1.6f, true, ClipLayer.FullBody, (t, p) =>
            {
                float u = TwoPi * t / 1.6f, b = Mathf.Sin(u), b2 = Mathf.Sin(u * 2f);
                p.R("UAR", -120f + 30f * b, 0, -30f); p.R("LAR", -60f + 30f * b2); p.R("UAL", -120f - 30f * b, 0, 30f); p.R("LAL", -60f - 30f * b2);
                p.R("ULL", -15f * Mathf.Max(0, b)); p.R("LLL", 30f * Mathf.Max(0, b)); p.R("ULR", -15f * Mathf.Max(0, -b)); p.R("LLR", 30f * Mathf.Max(0, -b));
                p.R("S", 0, 10f * b, 4f * b2); p.R("Hd", -5f, -12f * b, 0); p.Root.y = 0.03f * Mathf.Abs(b2);
            });
            B("Emote_Sitzen", 3f, true, ClipLayer.FullBody, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 3f);
                new KeyTrack(p).Key(0, "ULL:-88,0,-10 ULR:-88,0,10 LLL:20 LLR:30 FTL:-20 FTR:-20 S:15 UAL:-15,0,-20 UAR:-15,0,20 LAL:-30 LAR:-30 root:0,-0.62,-0.1").Apply(0);
                p.R("S", 2f * b); p.R("Hd", -2f * b, 10f * Mathf.Sin(TwoPi * t / 6f), 0);
            });

            foreach (var st in Styles) WeaponClips(st, list, K);
            return list;
        }

        static void Stance(PoseBuilder p)
        {
            p.R("UAL", 0, 0, -6f); p.R("UAR", 0, 0, 6f); p.R("LAL", -10f); p.R("LAR", -10f);
        }

        /// <param name="mode">0 vorwärts, 2 rückwärts, −1/1 seitlich links/rechts</param>
        public static void Gait(PoseBuilder p, float ph, float amp, float knee, float arm, float elbow, float bob, float lean, int mode)
        {
            for (int side = 0; side < 2; side++)
            {
                string s = side == 0 ? "L" : "R";
                float q = ph + side * Mathf.PI;
                if (mode == 2) q = -q;  // rückwärts: Zeit gespiegelt, Kontakt am hinteren Umkehrpunkt
                float swing = Mathf.Sin(q), vel = Mathf.Cos(q);
                float kneeBend = knee * Mathf.Pow(Mathf.Max(0f, vel), 1.5f) + 6f;
                if (mode == -1 || mode == 1)
                {
                    float z = amp * swing * mode;
                    p.R("UL" + s, -6f, 0, z); p.R("LL" + s, kneeBend); p.R("FT" + s, -kneeBend * 0.4f, 0, -z * 0.6f);
                }
                else
                {
                    float x = -amp * swing;
                    p.R("UL" + s, x); p.R("LL" + s, kneeBend);
                    p.R("FT" + s, -x * 0.35f - kneeBend * 0.35f + 10f * Mathf.Max(0, -swing) * (mode == 2 ? 0 : 1));
                }
                float a = arm * swing;
                p.R("UA" + s, mode == 2 ? -a * 0.5f : a); p.R("LA" + s, -elbow);
            }
            p.Root.y = bob * Mathf.Cos(2f * ph);
            p.R("S", lean); p.R("Hd", -lean * 0.6f);
            if (mode == 0 || mode == 2) { p.R("H", 0, 6f * Mathf.Sin(ph), 2.5f * Mathf.Cos(2f * ph)); p.R("S1", 0, -9f * Mathf.Sin(ph), 0); }
            else { p.R("H", 0, 0, 3f * Mathf.Sin(ph * 2f)); p.R("S1", 0, -15f * mode, 0); p.R("H", 0, 15f * mode, 0); }
        }

        static void WeaponClips(WeaponStyle st, List<ClipData> list, Func<string, float, ClipLayer, Action<KeyTrack>, bool, bool, ClipData> K0)
        {
            ClipData K(string n, float len, Action<KeyTrack> k) => K0(st.Name + "_" + n, len, ClipLayer.UpperBody, k, false, true);
            float m = st.Tempo;
            // Zweihänder: linke Hand folgt der rechten (gespiegelt zur Mitte)
            string Both(string r) => st.TwoHand ? r + " " + r.Replace("UAR:", "UAL:").Replace("LAR:", "LAL:").Replace("HAR:", "HAL:") + " UAL:0,0,-30" : r + " UAL:-20,0,-25 LAL:-40";
            if (st.Bow)
            {
                const string hold = "UAL:-88,0,8 LAL:-5 UAR:-85,-10,-20 LAR:-110 S1:0,-25,0 Hd:0,25,0";
                const string drawn = "UAL:-90,0,8 LAL:-2 UAR:-80,40,30 LAR:-150 S1:0,-35,0 Hd:0,35,0 S:-3";
                K("Auto1", 0.6f, k => k.Key(0, "").Key(0.15f, hold, 'o').Key(0.4f, drawn).Key(0.45f, hold, 'o').Key(0.6f, ""))
                    .AddMarker(Marker.Effect, 0.15f).AddMarker(Marker.Projectile, 0.42f).AddMarker(Marker.End, 0.55f);
                K("Auto2", 0.55f, k => k.Key(0, "").Key(0.12f, hold, 'o').Key(0.35f, drawn).Key(0.4f, hold, 'o').Key(0.55f, ""))
                    .AddMarker(Marker.Effect, 0.12f).AddMarker(Marker.Projectile, 0.37f).AddMarker(Marker.End, 0.5f);
                K("Auto3", 0.8f, k => k.Key(0, "").Key(0.2f, hold + " S:-10", 'o').Key(0.55f, drawn + " S:-15").Key(0.6f, hold, 'o').Key(0.8f, ""))
                    .AddMarker(Marker.Effect, 0.2f).AddMarker(Marker.Projectile, 0.57f).AddMarker(Marker.End, 0.75f);
                K("Heavy", 1.3f, k => k.Key(0, "").Key(0.3f, hold, 'o').Key(0.8f, drawn + " S:-20 Hd:-10,35,0").Key(0.85f, hold + " S:-20", 'o').Key(1.3f, ""))
                    .AddMarker(Marker.Effect, 0.2f).AddMarker(Marker.Projectile, 0.82f).AddMarker(Marker.End, 1.2f);
                K("Thrust", 0.6f, k => k.Key(0, "").Key(0.2f, "UAL:-70,0,10 LAL:-60 UAR:-70 LAR:-60 S:5", 'o').Key(0.32f, "UAL:-95,0,0 LAL:-5 UAR:-95 LAR:-5 S:18", 'i').Key(0.6f, ""))
                    .AddMarker(Marker.Hit, 0.32f).AddMarker(Marker.End, 0.5f);
                K("Whirl", 1.0f, k => k.Key(0, "").Key(0.15f, hold).Key(0.4f, drawn + " S:-30").Key(0.45f, hold + " S:-30").Key(0.65f, drawn + " S:-30").Key(0.7f, hold + " S:-30").Key(1.0f, ""))
                    .AddMarker(Marker.Projectile, 0.42f).AddMarker(Marker.Projectile, 0.67f).AddMarker(Marker.End, 0.9f);
                K("Shoot", 0.8f, k => k.Key(0, "").Key(0.2f, hold, 'o').Key(0.55f, drawn).Key(0.58f, hold, 'o').Key(0.8f, ""))
                    .AddMarker(Marker.Effect, 0.2f).AddMarker(Marker.Projectile, 0.56f).AddMarker(Marker.End, 0.72f);
                K("Block", 1.0f, k => k.Key(0, "UAL:-80,0,30 LAL:-70 UAR:-60,0,-20 LAR:-80 S:5").Key(0.5f, "UAL:-82,0,30 LAL:-72 UAR:-62,0,-20 LAR:-82 S:6").Key(1.0f, "UAL:-80,0,30 LAL:-70 UAR:-60,0,-20 LAR:-80 S:5"))
                    .AddMarker(Marker.Effect, 0f);
                return;
            }
            float a1 = 0.5f * m, a3 = 0.75f * m, hv = Mathf.Max(1.3f, 1.2f * m), th = 0.6f * m;
            K("Auto1", a1, k => k.Key(0, "")
                .Key(a1 * 0.35f, Both("UAR:-110,-35,45 LAR:-40 S1:0,30,0 S:-3"), 'o')
                .Key(a1 * 0.55f, Both("UAR:-65,55,-10 LAR:-10 HAR:0,0,-20 S1:0,-30,0 S:12"), 'i')
                .Key(a1, "", 's'))
                .AddMarker(Marker.Effect, a1 * 0.3f).AddMarker(Marker.Hit, a1 * 0.5f).AddMarker(Marker.End, a1 * 0.85f);
            K("Auto2", a1, k => k.Key(0, Both("UAR:-65,55,-10 LAR:-10 S1:0,-25,0 S:8"))
                .Key(a1 * 0.3f, Both("UAR:-80,70,-25 LAR:-30 S1:0,-35,0"), 'o')
                .Key(a1 * 0.55f, Both("UAR:-100,-40,60 LAR:-15 HAR:0,0,20 S1:0,35,0 S:10"), 'i')
                .Key(a1, "", 's'))
                .AddMarker(Marker.Effect, a1 * 0.25f).AddMarker(Marker.Hit, a1 * 0.5f).AddMarker(Marker.End, a1 * 0.85f);
            K("Auto3", a3, k => k.Key(0, "")
                .Key(a3 * 0.4f, Both("UAR:-170,0,10 LAR:-35 S:-12 Hd:-10"), 'o')
                .Key(a3 * 0.55f, Both("UAR:-45,0,-5 LAR:-5 HAR:25 S:28 Hd:10") + " root:0,-0.06,0.12", 'i')
                .Key(a3, "", 's'))
                .AddMarker(Marker.Effect, a3 * 0.35f).AddMarker(Marker.Hit, a3 * 0.55f).AddMarker(Marker.End, a3 * 0.9f);
            K("Heavy", hv, k => k.Key(0, "")
                .Key(0.62f, Both("UAR:-175,0,15 LAR:-45 S:-18 Hd:-15 S1:0,15,0") + " root:0,-0.04,-0.05", 's')
                .Key(0.78f, Both("UAR:-35,0,-5 LAR:-5 HAR:30 S:35 Hd:10") + " root:0,-0.16,0.25", 'i')
                .Key(hv, "", 's'))
                .AddMarker(Marker.Effect, 0f).AddMarker(Marker.Hit, 0.78f).AddMarker(Marker.End, hv - 0.15f);
            K("Thrust", th, k => k.Key(0, "")
                .Key(th * 0.35f, Both("UAR:20,0,15 LAR:-110 S1:0,30,0 S:-4") + " root:0,0,-0.05", 'o')
                .Key(th * 0.5f, Both("UAR:-90,0,-5 LAR:0 S1:0,-10,0 S:16") + " root:0,-0.05,0.3", 'i')
                .Key(th, "", 's'))
                .AddMarker(Marker.Effect, th * 0.3f).AddMarker(Marker.Hit, th * 0.5f).AddMarker(Marker.End, th * 0.85f);
            K("Whirl", 1.0f, k => k.Key(0, "")
                .Key(0.1f, Both("UAR:-90,0,70 LAR:-10") + " UAL:-90,0,-70 H:0,30,0", 'o')
                .Key(0.325f, Both("UAR:-90,0,70 LAR:-10") + " UAL:-90,0,-70 H:0,270,0", 'l')
                .Key(0.55f, Both("UAR:-90,0,70 LAR:-10") + " UAL:-90,0,-70 H:0,510,0", 'l')
                .Key(0.775f, Both("UAR:-90,0,70 LAR:-10") + " UAL:-90,0,-70 H:0,750,0", 'l')
                .Key(1.0f, "H:0,720,0", 'o'))
                .AddMarker(Marker.Effect, 0.1f).AddMarker(Marker.Hit, 0.3f).AddMarker(Marker.Hit, 0.55f).AddMarker(Marker.Hit, 0.8f).AddMarker(Marker.End, 0.92f);
            K("Shoot", 0.7f, k => k.Key(0, "")
                .Key(0.3f, "UAR:-160,0,35 LAR:-90 S1:0,35,0 S:-10 UAL:-60,0,-20", 'o')
                .Key(0.45f, "UAR:-70,0,-10 LAR:-5 HAR:30 S1:0,-25,0 S:15 UAL:-10,0,-25", 'i')
                .Key(0.7f, "", 's'))
                .AddMarker(Marker.Effect, 0.25f).AddMarker(Marker.Projectile, 0.45f).AddMarker(Marker.End, 0.62f);
            string guard = st.TwoHand ? "UAR:-75,0,-35 LAR:-75 UAL:-75,0,35 LAL:-75 HAR:0,0,-40 S:6" : "UAR:-55,0,-20 LAR:-80 UAL:-80,0,25 LAL:-85 HAL:0,90,0 S:6";
            string guard2 = guard.Replace("S:6", "S:7 Hd:-2");
            K("Block", 1.0f, k => k.Key(0, guard).Key(0.5f, guard2).Key(1.0f, guard)).AddMarker(Marker.Effect, 0f);
        }

        // ------------------------------------------------------------ Tiere / Reittiere
        public static List<ClipData> Creature(CreatureProfile pr, bool mountClips)
        {
            var sk = pr.Skeleton(); var al = CreatureAlias(sk);
            var list = new List<ClipData>();
            float L = pr.LegHeight, sz = Mathf.Max(0.4f, pr.LegHeight);
            var sec = Sec(sk, al, "Hd", "N", "T1", "T2");
            float restFoot = sk.WorldPositions(null, Vector3.zero)[sk.IndexOf("BL_Foot")].y;
            ClipData B(string n, float len, bool loop, ClipLayer layer, Action<float, PoseBuilder> f)
            {
                var c = Bake(sk, al, pr.Name + "_" + n, len, loop, layer, (t, p) => { if (layer != ClipLayer.Additive) BasePose(p, pr); f(t, p); }, sec);
                list.Add(c); return c;
            }
            ClipData K(string n, float len, ClipLayer layer, Action<KeyTrack> keys, bool loop = false)
            {
                KeyTrack kt = null;
                return B(n, len, loop, layer, (t, p) => { if (kt == null) { kt = new KeyTrack(p); keys(kt); } kt.Apply(t); });
            }
            string R(float y) => "root:0," + (y * sz).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",0";
            string RZ(float y, float z) => "root:0," + (y * sz).ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (z * sz).ToString(System.Globalization.CultureInfo.InvariantCulture);

            void Loco(string n, float len, float amp, float knee, float[] off, float spine, float bobK, float wingBase = 15f, float wingAmp = 5f)
            {
                var c = B(n, len, true, ClipLayer.Locomotion, (t, p) => QGait(p, pr, TwoPi * t / len, amp, knee, off, spine, bobK, wingBase, wingAmp));
                if (pr.Gait == CreatureProfile.GaitKind.Fins) { c.AddMarker(Marker.FootL, 0f); return; }
                bool back = pr.Gait == CreatureProfile.GaitKind.Wings || pr.Gait == CreatureProfile.GaitKind.Biped;
                int a = back ? 2 : 0;
                GroundContacts(sk, c, sk.IndexOf(SkeletonDef.Legs[a] + "_Foot"), sk.IndexOf(SkeletonDef.Legs[a + 1] + "_Foot"), restFoot,
                    Mathf.Repeat(off[a] + 0.25f, 1f), Mathf.Repeat(off[a + 1] + 0.25f, 1f));
            }
            // Reihenfolge der Phasen: FL, FR, BL, BR
            var walk = new[] { 0.25f, 0.75f, 0f, 0.5f };
            var trot = new[] { 0f, 0.5f, 0.5f, 0f };
            var gallop = new[] { 0f, 0.12f, 0.5f, 0.62f };
            if (pr.Gait == CreatureProfile.GaitKind.Biped) { walk = new[] { 0.5f, 0f, 0f, 0.5f }; trot = walk; gallop = walk; }

            B("Idle", 3f, true, ClipLayer.Locomotion, (t, p) =>
            {
                float b = Mathf.Sin(TwoPi * t / 3f), w = Mathf.Sin(TwoPi * t / 1.5f);
                p.R("C", 1.5f * b); p.R("N", -2f * b); p.R("Hd", 3f * Mathf.Sin(TwoPi * t / 3f * 2f), 6f * Mathf.Sin(TwoPi * t / 6f * 2f), 0);
                p.R("T1", 5f * b, 15f * w, 0); p.R("T2", 0, 20f * w, 0); p.Root.y = -0.01f * sz * (1 + b);
                Wings(p, pr, t, 0.3f, 4f, 12f);
                if (pr.Gait == CreatureProfile.GaitKind.Fins) FinWave(p, TwoPi * t / 3f, 10f);
            });
            Loco("Walk", 1.2f, 22f, 30f, walk, 2f, 0.4f);
            Loco("Run", 0.6f, 38f, 60f, pr.Gait == CreatureProfile.GaitKind.Legs ? trot : walk, 6f, 0.7f, 60f, 35f);
            K("Jump", 0.8f, ClipLayer.FullBody, k => k
                .Key(0, "")
                .Key(0.15f, "FLU:-10 FLL:25 FRU:-10 FRL:25 BLU:20 BLL:-45 BRU:20 BRL:-45 P:-6 " + R(-0.25f), 'o')
                .Key(0.35f, "FLU:-60 FLL:40 FRU:-60 FRL:40 BLU:55 BLL:-20 BRU:55 BRL:-20 C:-12 Hd:-10 T1:-15 " + R(0.5f), 'o')
                .Key(0.6f, "FLU:-30 FLL:20 FRU:-30 FRL:20 BLU:-20 BLL:-30 BRU:-20 BRL:-30 C:8 " + R(0.15f), 'i')
                .Key(0.8f, ""))
                .AddMarker(Marker.Effect, 0.15f).AddMarker(Marker.FootL, 0.6f).AddMarker(Marker.FootR, 0.62f).AddMarker(Marker.End, 0.75f);
            // Angriff: ≥ 0,6 s Ausholen (Telegraph), Treffer bei 0,72 s
            K("Attack", 1.1f, ClipLayer.FullBody, k => k
                .Key(0, "")
                .Key(0.6f, "C:-18 P:-6 N:-20 Hd:-25 FLU:-25 FLL:30 FRU:-25 FRL:30 BLU:15 BLL:-30 BRU:15 BRL:-30 T1:-20 " + RZ(0.08f, -0.12f), 's')
                .Key(0.72f, "C:15 N:25 Hd:20 FLU:-45 FRU:-45 BLU:30 BRU:30 T1:10 " + RZ(-0.05f, 0.45f), 'i')
                .Key(1.1f, "", 's'))
                .AddMarker(Marker.Effect, 0f).AddMarker(Marker.Hit, 0.72f).AddMarker(Marker.End, 1.0f);
            K("Hit", 0.35f, ClipLayer.Additive, k => k.Key(0, "").Key(0.07f, "C:-10 N:-12 Hd:-15 T1:-10 P:0,0,5", 'o').Key(0.35f, ""));
            K("Death", 1.2f, ClipLayer.FullBody, k => k
                .Key(0, "")
                .Key(0.35f, "FLU:-10 FLL:40 FRU:-10 FRL:40 BLU:20 BLL:-40 BRU:20 BRL:-40 Hd:20 " + R(-0.2f), 'i')
                .Key(1.2f, "P:0,0,85 C:0,0,5 FLU:-30,0,-10 FRU:-20,0,10 BLU:20 BRU:30 Hd:-20,0,20 T1:0,20,0 " + R(-0.62f), 'b'))
                .AddMarker(Marker.Hit, 1.0f).AddMarker(Marker.End, 1.2f);
            B("Sniff", 2f, true, ClipLayer.Locomotion, (t, p) =>
            {
                float s = Mathf.Sin(TwoPi * t * 4f), b = Mathf.Sin(TwoPi * t / 2f);
                p.R("C", 10f); p.R("N", 35f + 4f * s); p.R("Hd", 15f + 4f * s, 15f * b, 0); p.R("FLU", -8f); p.R("FRU", -8f); p.R("FLL", 15f); p.R("FRL", 15f);
                p.R("T1", 0, 10f * b, 0); p.Root.y = -0.06f * sz;
            }).AddMarker(Marker.Effect, 0.5f);

            if (mountClips)
            {
                B("MIdle", 3f, true, ClipLayer.Locomotion, (t, p) =>
                {
                    float b = Mathf.Sin(TwoPi * t / 3f), w = Mathf.Sin(TwoPi * t / 1.5f);
                    p.R("C", 2f * b); p.R("Hd", -4f * b, 10f * Mathf.Sin(TwoPi * t / 3f), 0); p.R("T1", 0, 18f * w, 0); p.R("T2", 0, 22f * w, 0);
                    p.Root.y = -0.015f * sz * (1 + b); Wings(p, pr, t, 0.3f, 4f, 12f);
                    if (pr.Gait == CreatureProfile.GaitKind.Fins) FinWave(p, TwoPi * t / 3f, 10f);
                });
                Loco("MRun", 0.55f, 45f, 70f, gallop, 10f, 0.9f, 60f, 30f);
                K("MLeap", 0.9f, ClipLayer.FullBody, k => k
                    .Key(0, "")
                    .Key(0.15f, "BLU:25 BLL:-50 BRU:25 BRL:-50 FLU:-5 FRU:-5 P:-8 " + R(-0.2f), 'o')
                    .Key(0.4f, "FLU:-70 FLL:60 FRU:-70 FRL:60 BLU:60 BLL:-10 BRU:60 BRL:-10 C:-15 Hd:-15 T1:-20 " + R(0.7f), 'o')
                    .Key(0.7f, "FLU:-35 FLL:15 FRU:-35 FRL:15 BLU:-15 BRU:-15 C:10 " + R(0.2f), 'i')
                    .Key(0.9f, ""))
                    .AddMarker(Marker.Effect, 0.15f).AddMarker(Marker.FootL, 0.7f).AddMarker(Marker.End, 0.85f);
                B("MCharge", 0.5f, true, ClipLayer.Locomotion, (t, p) =>
                {
                    QGait(p, pr, TwoPi * t / 0.5f, 42f, 65f, gallop, 6f, 0.6f, 50f, 20f);
                    p.R("N", 25f); p.R("Hd", 20f); p.R("C", 8f); p.Root.y -= 0.08f * sz;
                }).AddMarker(Marker.FootL, 0.125f).AddMarker(Marker.FootR, 0.185f);
                B("MHover", 1.2f, true, ClipLayer.Locomotion, (t, p) =>
                {
                    float s = Mathf.Sin(TwoPi * t / 1.2f);
                    if (pr.Gait == CreatureProfile.GaitKind.Fins) { FinWave(p, TwoPi * t / 1.2f, 30f); }
                    else
                    {
                        p.R("FLU", -40f + 5f * s); p.R("FRU", -40f - 5f * s); p.R("FLL", 60f); p.R("FRL", 60f);
                        p.R("BLU", 40f + 5f * s); p.R("BRU", 40f - 5f * s); p.R("BLL", -40f); p.R("BRL", -40f);
                    }
                    p.R("T1", 8f * s); p.R("T2", 12f * s); p.Root.y = 0.05f * sz * s; Wings(p, pr, t, 1f, 35f, 60f);
                });
                K("MEngage", 1.0f, ClipLayer.FullBody, k => k
                    .Key(0, "")
                    .Key(0.6f, "P:-35 C:-10 N:-15 Hd:-20 FLU:-80 FLL:90 FRU:-70 FRL:80 BLU:25 BRU:25 BLL:-20 BRL:-20 T1:20 " + R(0.15f), 's')
                    .Key(0.72f, "P:12 C:12 N:20 Hd:20 FLU:-30 FRU:-30 FLL:10 FRL:10 BLU:-10 BRU:-10 " + RZ(-0.12f, 0.5f), 'i')
                    .Key(1.0f, "", 's'))
                    .AddMarker(Marker.Effect, 0f).AddMarker(Marker.Hit, 0.72f).AddMarker(Marker.End, 0.92f);
                K("MKnockOff", 0.8f, ClipLayer.FullBody, k => k
                    .Key(0, "")
                    .Key(0.2f, "P:15 C:-20 BLU:-40 BRU:-40 BLL:-20 BRL:-20 FLU:10 FRU:10 Hd:25 " + R(0.1f), 'o')
                    .Key(0.4f, "P:-20 C:10 FLU:-50 FRU:-50 FLL:50 FRL:50 Hd:-20 " + R(0.15f), 's')
                    .Key(0.8f, ""))
                    .AddMarker(Marker.Hit, 0.2f).AddMarker(Marker.End, 0.75f);
            }
            return list;
        }

        /// <summary>Grundhaltung je Körperbau (Flossen waagrecht, Flügel angelegt), auf alle nicht-additiven Clips addiert.</summary>
        public static void BasePose(PoseBuilder p, CreatureProfile pr)
        {
            if (pr.Gait == CreatureProfile.GaitKind.Fins) foreach (var l in SkeletonDef.Legs) p.R(l + "U", 0, 0, l[1] == 'L' ? -82f : 82f);
            if (pr.Gait == CreatureProfile.GaitKind.Wings) { p.R("FLU", 0, 0, -12f); p.R("FRU", 0, 0, 12f); }
        }

        static void Wings(PoseBuilder p, CreatureProfile pr, float t, float rate, float amp, float baseAngle)
        {
            if (pr.Gait != CreatureProfile.GaitKind.Wings) return;
            float s = Mathf.Sin(TwoPi * t * Mathf.Max(0.2f, rate) * 2f);
            baseAngle -= 12f;
            p.R("FLU", 0, 0, -(baseAngle + amp * s)); p.R("FRU", 0, 0, baseAngle + amp * s); p.R("FLL", 0, 0, -amp * 0.3f * s); p.R("FRL", 0, 0, amp * 0.3f * s);
        }

        static void FinWave(PoseBuilder p, float ph, float amp)
        {
            float f = Mathf.Sin(ph), b = Mathf.Sin(ph - 1.2f);
            p.R("FLU", 0, 0, -amp * f); p.R("FRU", 0, 0, amp * f); p.R("FLL", 0, 0, -amp * 0.6f * f); p.R("FRL", 0, 0, amp * 0.6f * f);
            p.R("BLU", 0, 0, -amp * b); p.R("BRU", 0, 0, amp * b); p.R("BLL", 0, 0, -amp * 0.6f * b); p.R("BRL", 0, 0, amp * 0.6f * b);
        }

        public static void QGait(PoseBuilder p, CreatureProfile pr, float ph, float amp, float knee, float[] off, float spine, float bobK, float wingBase = 15f, float wingAmp = 5f)
        {
            if (pr.Gait == CreatureProfile.GaitKind.Fins) { FinWave(p, ph, amp * 0.8f); p.R("T1", 0, 10f * Mathf.Sin(ph), 0); p.R("T2", 0, 18f * Mathf.Sin(ph - 1f), 0); return; }
            for (int i = 0; i < 4; i++)
            {
                string l = SkeletonDef.Legs[i]; bool front = i < 2;
                if (front && pr.Gait == CreatureProfile.GaitKind.Wings) continue;
                float q = ph - TwoPi * off[i];
                if (front && pr.Gait == CreatureProfile.GaitKind.Biped) { p.R(l + "U", -30f - amp * 0.15f * Mathf.Sin(q)); p.R(l + "L", -50f); continue; }
                float swing = Mathf.Sin(q), vel = Mathf.Cos(q);
                float bend = knee * Mathf.Pow(Mathf.Max(0f, vel), 1.5f) + 5f;
                p.R(l + "U", -amp * swing);
                p.R(l + "L", front ? bend : -bend);
                p.R(l + "F", front ? -bend * 0.5f : bend * 0.5f);
            }
            float bob = BobFor(pr.LegHeight, amp) * bobK * (pr.Gait == CreatureProfile.GaitKind.Wings ? 0.3f : 1f);
            p.Root.y = bob * Mathf.Cos(2f * ph);
            p.R("C", spine * Mathf.Sin(ph)); p.R("P", -spine * 0.5f * Mathf.Sin(ph));
            p.R("N", -spine * 0.5f * Mathf.Sin(ph)); p.R("Hd", spine * 0.3f * Mathf.Sin(ph + 0.5f));
            p.R("T1", 6f * Mathf.Sin(ph + 1f), 8f * Mathf.Sin(ph * 0.5f), 0); p.R("T2", 0, 12f * Mathf.Sin(ph * 0.5f - 0.6f), 0);
            Wings(p, pr, ph / TwoPi, 1f, wingAmp, wingBase);
        }

        // ------------------------------------------------------------ Auswertung (Tests, IK-Fußkurven)
        /// <summary>Fußpositionen relativ zur Charakter-Wurzel je Frame (Vorwärtskinematik inkl. Wurzelversatz).</summary>
        public static Vector3[] BonePath(SkeletonDef sk, ClipData c, int bone)
        {
            var r = new Vector3[c.FrameCount];
            for (int f = 0; f < c.FrameCount; f++) r[f] = sk.WorldPositions(c.Euler[f], c.Root[f])[bone];
            return r;
        }

        public static Vector3[] Sample(ClipData c, float t, out Vector3 root)
        {
            float ft = Mathf.Clamp(t, 0f, c.Length) * ClipData.FrameRate;
            int a = Mathf.Clamp(Mathf.FloorToInt(ft), 0, c.FrameCount - 1), b = Mathf.Min(a + 1, c.FrameCount - 1);
            float u = ft - a;
            var e = new Vector3[c.Euler[a].Length];
            for (int i = 0; i < e.Length; i++) e[i] = Vector3.Lerp(c.Euler[a][i], c.Euler[b][i], u);
            root = Vector3.Lerp(c.Root[a], c.Root[b], u);
            return e;
        }

        public static bool HasNaN(ClipData c)
        {
            for (int f = 0; f < c.FrameCount; f++)
            {
                if (float.IsNaN(c.Root[f].x + c.Root[f].y + c.Root[f].z) || float.IsInfinity(c.Root[f].sqrMagnitude)) return true;
                foreach (var v in c.Euler[f]) if (float.IsNaN(v.x + v.y + v.z) || float.IsInfinity(v.sqrMagnitude)) return true;
            }
            return false;
        }
    }
}
