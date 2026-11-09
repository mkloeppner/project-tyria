using System.Collections.Generic;
using ProjectTyrant.Anim;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 5 / M21: backt alle prozeduralen Clips (<see cref="ClipLibrary"/>) als AnimationClip-Assets (30 Hz, Quaternion-Kurven je Knochen,
    /// Wurzelversatz auf „Body/Rig“) und legt je Skelett einen <see cref="AnimClipSet"/> mit Markern und Fußkurven an.
    /// </summary>
    public static class ClipBuilder
    {
        public const string AnimDir = ProcDir + "/Animation";
        public const string SetDir = DataDir + "/Animation";
        public const string HumanoidSetPath = SetDir + "/ClipSet_Humanoid.asset";
        public static string CreatureSetPath(string profile) => SetDir + "/ClipSet_" + profile + ".asset";

        public static AnimClipSet HumanoidSet => AssetDatabase.LoadAssetAtPath<AnimClipSet>(HumanoidSetPath);
        public static AnimClipSet CreatureSet(string profile) => AssetDatabase.LoadAssetAtPath<AnimClipSet>(CreatureSetPath(profile));

        [MenuItem("Project Tyrant/Lauf 5/Clips backen")]
        public static void BuildAll()
        {
            EnsureFolder(AnimDir); EnsureFolder(SetDir);
            var sk = SkeletonDef.Humanoid();
            BuildSet(sk, ClipLibrary.Humanoid(sk), "Humanoid", HumanoidSetPath, true);
            foreach (var pr in ClipLibrary.Creatures)
            {
                bool mount = pr.Name == "Raptor" || pr.Name == "Ray" || pr.Name == "Springer";
                BuildSet(pr.Skeleton(), ClipLibrary.Creature(pr, mount), pr.Name, CreatureSetPath(pr.Name), false);
            }
            AssetDatabase.SaveAssets();
        }

        static void BuildSet(SkeletonDef sk, List<ClipData> clips, string rig, string setPath, bool feet)
        {
            string dir = AnimDir + "/" + rig; EnsureFolder(dir);
            var set = AssetDatabase.LoadAssetAtPath<AnimClipSet>(setPath);
            if (set == null) { set = ScriptableObject.CreateInstance<AnimClipSet>(); AssetDatabase.CreateAsset(set, setPath); }
            set.Rig = rig; set.Clips.Clear();
            var refClip = ToClip(sk, BindClip(sk), dir + "/" + rig + "_Ref.anim");
            int fl = sk.IndexOf(SkeletonDef.FootL), fr = sk.IndexOf(SkeletonDef.FootR);
            foreach (var c in clips)
            {
                var clip = ToClip(sk, c, dir + "/" + c.Name + ".anim");
                if (c.Layer == ClipLayer.Additive) AnimationUtility.SetAdditiveReferencePose(clip, refClip, 0f);
                var e = new AnimClipSet.Entry { Name = c.Name, Clip = clip, Layer = c.Layer, Loop = c.Loop, Length = c.Length, Markers = new List<ClipMarker>(c.Markers) };
                if (feet && fl >= 0 && c.Layer != ClipLayer.Additive) { e.FootL = ClipLibrary.BonePath(sk, c, fl); e.FootR = ClipLibrary.BonePath(sk, c, fr); }
                set.Clips.Add(e);
            }
            EditorUtility.SetDirty(set);
        }

        static ClipData BindClip(SkeletonDef sk)
        {
            var c = new ClipData { Name = "Ref", Rig = sk.Kind, Length = 1f / ClipData.FrameRate, Loop = false, Layer = ClipLayer.FullBody, Euler = new Vector3[2][], Root = new Vector3[2] };
            c.Euler[0] = new Vector3[sk.Count]; c.Euler[1] = new Vector3[sk.Count];
            return c;
        }

        /// <summary>ClipData → AnimationClip (generisch, Transform-Pfade relativ zum Animator am Visual).</summary>
        public static AnimationClip ToClip(SkeletonDef sk, ClipData c, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            clip.frameRate = ClipData.FrameRate;
            int n = c.FrameCount;
            for (int b = 0; b < sk.Count; b++)
            {
                var kx = new Keyframe[n]; var ky = new Keyframe[n]; var kz = new Keyframe[n]; var kw = new Keyframe[n];
                Quaternion prev = Quaternion.identity;
                for (int f = 0; f < n; f++)
                {
                    float t = Mathf.Min(c.Length, f / ClipData.FrameRate);
                    var q = Quaternion.Euler(c.Euler[f][b]);
                    if (f > 0 && Quaternion.Dot(prev, q) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    prev = q;
                    kx[f] = new Keyframe(t, q.x); ky[f] = new Keyframe(t, q.y); kz[f] = new Keyframe(t, q.z); kw[f] = new Keyframe(t, q.w);
                }
                string p = sk.Path(b);
                Set(clip, p, "m_LocalRotation.x", kx); Set(clip, p, "m_LocalRotation.y", ky); Set(clip, p, "m_LocalRotation.z", kz); Set(clip, p, "m_LocalRotation.w", kw);
            }
            var rx = new Keyframe[n]; var ry = new Keyframe[n]; var rz = new Keyframe[n];
            for (int f = 0; f < n; f++) { float t = Mathf.Min(c.Length, f / ClipData.FrameRate); rx[f] = new Keyframe(t, c.Root[f].x); ry[f] = new Keyframe(t, c.Root[f].y); rz[f] = new Keyframe(t, c.Root[f].z); }
            Set(clip, SkeletonDef.RigPath, "m_LocalPosition.x", rx); Set(clip, SkeletonDef.RigPath, "m_LocalPosition.y", ry); Set(clip, SkeletonDef.RigPath, "m_LocalPosition.z", rz);
            clip.EnsureQuaternionContinuity();
            var st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopTime = c.Loop; st.stopTime = c.Length;
            AnimationUtility.SetAnimationClipSettings(clip, st);
            // Marker zusätzlich als AnimationEvents (Anzeige im Animation-Fenster; ausgelöst wird über CharacterAnimator)
            var evs = new List<AnimationEvent>();
            foreach (var m in c.Markers) evs.Add(new AnimationEvent { time = m.Time, functionName = "OnAnimMarker", stringParameter = m.Type });
            AnimationUtility.SetAnimationEvents(clip, evs.ToArray());
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void Set(AnimationClip clip, string path, string prop, Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), prop), curve);
        }
    }
}
