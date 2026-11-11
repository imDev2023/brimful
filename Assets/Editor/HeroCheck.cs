// Render the hero prefab in a set of clips, from the front and the side, into one contact
// sheet, to check the rig and the retargeting before trusting them in play:
//
//   unity command script eval 'return Brimful.EditorTools.HeroCheck.Render("Screenshots/hero-check.png");'
//
// Each cell samples one clip at one moment on the prefab, in a neutral scene: a grey floor, a
// warm sun from the upper right and a soft sky. The first cell is the bind pose (no clip).
// Returns the joints' heights in each pose, so a foot through the floor or an arm through the
// body shows in numbers as well as in the picture.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Brimful.EditorTools
{
    public static class HeroCheck
    {
        static readonly (string clip, float t)[] Poses =
        {
            (null, 0f),
            ("A_TPose", 0f),
            ("Idle_Loop", 0.6f),
            ("Jog_Fwd_Loop", 0.2f),
            ("Jog_Fwd_Loop", 0.65f),
            ("Jump_Loop", 0.5f),
            ("ClimbUp_1m", 0.3f),
            ("Push_Loop", 1.0f),
        };

        public static string Render(string outPath, int cell = 384)
        {
            var report = new StringBuilder();
            // Work in a scene of its own, and put the open scenes back afterwards. (An untitled
            // scene cannot be restored, and Unity refuses to add a scene beside one.)
            var setup = EditorSceneManager.GetSceneManagerSetup();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "cancelled: the open scenes have unsaved changes";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var made = new List<Object>();
            try
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
                RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
                RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.26f);

                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.93f, 0.82f);
                sun.intensity = 1.6f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.transform.localScale = new Vector3(2f, 1f, 2f);
                var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.55f, 0.55f, 0.55f) };
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                made.Add(floorMat);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Hero/Hero.prefab");
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var anim = hero.GetComponent<Animator>();
                // In edit mode a skinned mesh catches up with its bones only at the next update,
                // so without this every cell showed the pose sampled before it (2026-11-11).
                foreach (var smr in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
                    smr.forceMatrixRecalculationPerRender = true;

                var cam = new GameObject("CheckCam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.42f, 0.42f, 0.42f);
                cam.fieldOfView = 30f;
                var rt = new RenderTexture(cell, cell * 4 / 3, 24) { antiAliasing = 4 };
                made.Add(rt);
                cam.targetTexture = rt;

                var views = new[] { ("front", Quaternion.Euler(0f, 180f, 0f)), ("side", Quaternion.Euler(0f, -90f, 0f)) };
                var sheet = new Texture2D(cell * Poses.Length, rt.height * views.Length, TextureFormat.RGB24, false);
                made.Add(sheet);

                AnimationMode.StartAnimationMode();
                try
                {
                    for (int i = 0; i < Poses.Length; i++)
                    {
                        var (clipName, t) = Poses[i];
                        AnimationMode.BeginSampling();
                        if (clipName != null)
                        {
                            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Animations/Quaternius/{clipName}.anim");
                            AnimationMode.SampleAnimationClip(hero, clip, t * clip.length);
                        }
                        AnimationMode.EndSampling();
                        Vector3 B(HumanBodyBones b) => anim.GetBoneTransform(b).position;
                        var sole = Mathf.Min(B(HumanBodyBones.LeftToes).y, B(HumanBodyBones.RightToes).y, B(HumanBodyBones.LeftFoot).y, B(HumanBodyBones.RightFoot).y);
                        report.AppendLine($"{clipName ?? "bind"} @{t:0.00}: head {B(HumanBodyBones.Head).y:0.00} m, hands L {B(HumanBodyBones.LeftHand).y:0.00} R {B(HumanBodyBones.RightHand).y:0.00}, lowest foot joint {sole:0.00}, hips {B(HumanBodyBones.Hips)}");

                        for (int v = 0; v < views.Length; v++)
                        {
                            var dir = views[v].Item2 * Vector3.forward;
                            var target = new Vector3(0f, 0.85f, 0f);
                            cam.transform.position = target - dir * 4.2f;
                            cam.transform.LookAt(target);
                            cam.Render();
                            RenderTexture.active = rt;
                            sheet.ReadPixels(new Rect(0, 0, rt.width, rt.height), i * cell, (views.Length - 1 - v) * rt.height);
                            RenderTexture.active = null;
                        }
                    }
                }
                finally
                {
                    AnimationMode.StopAnimationMode();
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllBytes(outPath, sheet.EncodeToPNG());
                report.AppendLine($"wrote {outPath}: poses {string.Join(", ", Poses.Select(p => p.clip ?? "bind"))}; rows front, side");
            }
            finally
            {
                foreach (var o in made)
                    Object.DestroyImmediate(o);
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            return report.ToString();
        }
    }
}
