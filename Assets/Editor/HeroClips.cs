// Key the clips Quaternius's libraries do not have, as Humanoid muscle curves, so they retarget
// like the rest and stay ours (CC0):
//
//   unity command script eval 'Brimful.EditorTools.HeroClips.BuildAll(); return "ok";'
//
// Each pose starts from every muscle's value in Quaternius's A_TPose (on this avatar the
// muscles are not zero in a T-pose: arms level read Arm Down-Up 0.41 and Shoulder
// Front-Back 1.35) and overrides the muscles that make the pose. The IK goal curves are left
// out: the controller pins hands and feet itself where it needs to.
//
// Hang_Loop   hanging from a ledge by both hands, arms overhead and a little forward, knees
//             slightly bent, a slow sway. The controller's hand IK puts the hands on the lip
//             and steps them along it for the shimmy, so no shimmy clip is needed.
// Climb_Loop  climbing a wall of holds: one arm reaches while the opposite knee lifts, then
//             the other side; the controller plays it at the climbing speed.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Brimful.EditorTools
{
    public static class HeroClips
    {
        const string Dir = "Assets/Animations/Keyed";

        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Animations", "Keyed");
            var basePose = BasePose();

            var hangA = With(basePose, Hang(0f));
            var hangB = With(basePose, Hang(1f));
            Save("Hang_Loop", 2f, new[] { (0f, hangA), (1f, hangB), (2f, hangA) });

            var climbL = With(basePose, Climb(left: true));
            var climbR = With(basePose, Climb(left: false));
            Save("Climb_Loop", 1.2f, new[] { (0f, climbL), (0.6f, climbR), (1.2f, climbL) });
            AssetDatabase.SaveAssets();
        }

        // Every body curve of A_TPose at its first frame: muscles plus the root.
        static Dictionary<string, float> BasePose()
        {
            var tpose = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Quaternius/A_TPose.anim");
            var pose = new Dictionary<string, float>();
            foreach (var b in AnimationUtility.GetCurveBindings(tpose))
            {
                var n = b.propertyName;
                bool goal = (n.Contains("FootT") || n.Contains("FootQ") || n.Contains("HandT") || n.Contains("HandQ"));
                if (goal)
                    continue;
                pose[n] = AnimationUtility.GetEditorCurve(tpose, b).Evaluate(0f);
            }
            // Stand upright: A_TPose tips the body forward a little.
            pose["RootQ.x"] = 0f; pose["RootQ.y"] = 0f; pose["RootQ.z"] = 0f; pose["RootQ.w"] = 1f;
            pose["RootT.x"] = 0f; pose["RootT.z"] = 0f;
            return pose;
        }

        static Dictionary<string, float> With(Dictionary<string, float> basePose, Dictionary<string, float> overrides)
        {
            var p = new Dictionary<string, float>(basePose);
            foreach (var kv in overrides)
                p[kv.Key] = kv.Value;
            return p;
        }

        // Both sides at once: "Arm Down-Up" sets Left and Right Arm Down-Up.
        static void Both(Dictionary<string, float> p, string muscle, float value)
        {
            p[$"Left {muscle}"] = value;
            p[$"Right {muscle}"] = value;
        }

        // On this avatar a thigh lifts forward as Upper Leg Front-Back falls (standing straight
        // reads 0.6, ClimbUp_1m's raised knees about -0.85), Arm Down-Up past 1 tips the arm
        // back over the head, a positive Arm Front-Back swings the arm backward, and a positive
        // Neck or Head Nod looks up (2026-11-11 renders).
        static Dictionary<string, float> Hang(float sway)
        {
            var p = new Dictionary<string, float>();
            Both(p, "Shoulder Down-Up", 0.45f);
            Both(p, "Arm Down-Up", 0.95f);
            Both(p, "Arm Front-Back", -0.5f);
            Both(p, "Forearm Stretch", 0.85f);
            Both(p, "Hand Down-Up", -0.3f);
            p["UpperChest Front-Back"] = -0.1f;
            p["Neck Nod Down-Up"] = 0.15f;
            p["Head Nod Down-Up"] = 0.1f;
            float swing = Mathf.Lerp(-0.06f, 0.06f, sway);
            p["Left Upper Leg Front-Back"] = 0.38f + swing;
            p["Right Upper Leg Front-Back"] = 0.32f + swing;
            Both(p, "Lower Leg Stretch", 0.6f);
            Both(p, "Foot Up-Down", -0.35f);
            Both(p, "Upper Leg In-Out", -0.05f);
            return p;
        }

        static Dictionary<string, float> Climb(bool left)
        {
            var p = new Dictionary<string, float>();
            string up = left ? "Left" : "Right", down = left ? "Right" : "Left";
            // The reaching arm goes up in front of the face, the other grips at chest height.
            p[$"{up} Shoulder Down-Up"] = 0.45f;
            p[$"{up} Arm Down-Up"] = 0.95f;
            p[$"{up} Arm Front-Back"] = -0.5f;
            p[$"{up} Forearm Stretch"] = 0.8f;
            p[$"{down} Shoulder Down-Up"] = 0.05f;
            p[$"{down} Arm Down-Up"] = 0.3f;
            p[$"{down} Arm Front-Back"] = -0.55f;
            p[$"{down} Forearm Stretch"] = 0.35f;
            Both(p, "Hand Down-Up", -0.3f);
            // The opposite knee lifts forward to the next hold; the other leg stands on its hold.
            p[$"{down} Upper Leg Front-Back"] = -0.45f;
            p[$"{down} Lower Leg Stretch"] = -0.2f;
            p[$"{down} Upper Leg In-Out"] = 0.12f;
            p[$"{up} Upper Leg Front-Back"] = 0.55f;
            p[$"{up} Lower Leg Stretch"] = 0.8f;
            p[$"{up} Upper Leg In-Out"] = 0.05f;
            Both(p, "Foot Up-Down", -0.2f);
            p["UpperChest Front-Back"] = -0.1f;
            p["Neck Nod Down-Up"] = 0.2f;
            p["Head Nod Down-Up"] = 0.1f;
            p["Head Turn Left-Right"] = left ? -0.2f : 0.2f;
            return p;
        }

        static void Save(string name, float length, (float t, Dictionary<string, float> pose)[] keys)
        {
            var clip = new AnimationClip { name = name, frameRate = 30f };
            foreach (var muscle in keys[0].pose.Keys)
            {
                var curve = new AnimationCurve(keys.Select(k => new Keyframe(k.t, k.pose[muscle])).ToArray());
                for (int i = 0; i < curve.length; i++)
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                for (int i = 0; i < curve.length; i++)
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlendOrientation = settings.loopBlendPositionY = settings.loopBlendPositionXZ = true;
            settings.keepOriginalOrientation = settings.keepOriginalPositionY = settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var path = $"{Dir}/{name}.anim";
            var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (old != null)
            {
                EditorUtility.CopySerialized(clip, old);
                EditorUtility.SetDirty(old);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }
        }
    }
}
