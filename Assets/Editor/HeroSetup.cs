// Build the hero's material and prefab, and copy the clips the game uses out of Quaternius's
// libraries. Run after tools/rig_unity.py writes Assets/Characters/Hero, or after the
// libraries change:
//
//   unity command script eval 'Brimful.EditorTools.HeroSetup.BuildAll(); return "ok";'
//
// Every step is idempotent: it overwrites what it made before.

using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Brimful.EditorTools
{
    public static class HeroSetup
    {
        const string Dir = "Assets/Characters/Hero";
        const string ClipDir = "Assets/Animations/Quaternius";
        const string Ual1 = "Assets/_Imported/Quaternius/UAL1_Standard.fbx";
        const string Ual2 = "Assets/_Imported/Quaternius/UAL2_Standard.fbx";

        // Universal Animation Library (CC0, quaternius.com), Standard editions.
        static readonly (string file, string clip)[] Clips =
        {
            (Ual1, "A_TPose"),
            (Ual1, "Idle_Loop"),
            (Ual1, "Walk_Loop"),
            (Ual1, "Jog_Fwd_Loop"),
            (Ual1, "Sprint_Loop"),
            (Ual1, "Jump_Start"),
            (Ual1, "Jump_Loop"),
            (Ual1, "Jump_Land"),
            (Ual1, "Roll"),
            (Ual1, "Push_Loop"),
            (Ual1, "Interact"),
            (Ual2, "ClimbUp_1m"),
            (Ual2, "NinjaJump_Start"),
            (Ual2, "NinjaJump_Idle_Loop"),
            (Ual2, "NinjaJump_Land"),
            (Ual2, "Chest_Open"),
        };

        public static void BuildAll()
        {
            BuildMaterial();
            CopyClips();
            HeroClips.BuildAll();
            BuildController();
            BuildPrefab();
            AssetDatabase.SaveAssets();
        }

        static AnimationClip Clip(string path) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Animations/{path}.anim")
            ?? throw new System.Exception($"no clip Assets/Animations/{path}.anim");

        // HeroController owns the state and cross-fades to these states by name, so the
        // controller has no transition graph beyond the two one-shots that hand back.
        public static AnimatorController BuildController()
        {
            var path = $"{Dir}/Hero.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("ClimbRate", AnimatorControllerParameterType.Float);
            var layers = ac.layers;
            layers[0].iKPass = true;   // hands on the lip while hanging
            ac.layers = layers;
            var sm = ac.layers[0].stateMachine;

            var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip("Quaternius/Idle_Loop"), 0f);
            tree.AddChild(Clip("Quaternius/Walk_Loop"), 1.6f);
            tree.AddChild(Clip("Quaternius/Jog_Fwd_Loop"), 4.2f);
            tree.AddChild(Clip("Quaternius/Sprint_Loop"), 6f);
            sm.defaultState = loco;

            AnimatorState Add(string name, string clip, float speed = 1f)
            {
                var st = sm.AddState(name);
                st.motion = Clip(clip);
                st.speed = speed;
                st.writeDefaultValues = true;
                return st;
            }
            var jumpStart = Add("JumpStart", "Quaternius/Jump_Start", 1.4f);
            var fall = Add("Fall", "Quaternius/Jump_Loop");
            var land = Add("Land", "Quaternius/Jump_Land", 1.6f);
            var climb = Add("Climb", "Keyed/Climb_Loop");
            climb.speedParameterActive = true;
            climb.speedParameter = "ClimbRate";
            Add("Hang", "Keyed/Hang_Loop");
            Add("PullUp", "Quaternius/ClimbUp_1m", 0.7f);   // 0.67 s stretched over the 0.95 s pull
            Add("Interact", "Quaternius/Interact", 1.05f);

            var t1 = jumpStart.AddTransition(fall);
            t1.hasExitTime = true; t1.exitTime = 0.85f; t1.duration = 0.2f;
            var t2 = land.AddTransition(loco);
            t2.hasExitTime = true; t2.exitTime = 0.55f; t2.duration = 0.2f;
            EditorUtility.SetDirty(ac);
            return ac;
        }

        public static Material BuildMaterial()
        {
            var path = $"{Dir}/Hero.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Hero" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/Hero_basecolor.png"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/Hero_normal.png"));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            // Tripo's roughness reads as wet skin in a neutral light (2026-11-11); the painted
            // look wants matte cloth and skin, so the map's smoothness is scaled down.
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/Hero_metallicsmoothness.png"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_SmoothnessTextureChannel", 0f); // smoothness from the metallic map's alpha
            mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Copies only the clips not yet in Assets/Animations, unless forced (after a library
        // update or an import-setting change), so a rebuild does not rewrite unchanged clips.
        public static void CopyClips(bool force = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(ClipDir))
                AssetDatabase.CreateFolder("Assets/Animations", "Quaternius");
            foreach (var (file, name) in Clips)
            {
                var path = $"{ClipDir}/{name}.anim";
                var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                // A fresh clone has the clips but not the gitignored libraries: nothing to do.
                if (old != null && !force)
                    continue;
                var src = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name == $"Armature|{name}");
                if (src == null)
                    throw new System.Exception($"{name} is not in {file} (the libraries live in raw/ual, see CLAUDE.md)");
                var copy = Object.Instantiate(src);
                copy.name = name;
                AnimationUtility.SetAnimationClipSettings(copy, AnimationUtility.GetAnimationClipSettings(src));
                if (old != null)
                {
                    EditorUtility.CopySerialized(copy, old);
                    old.name = name;
                    EditorUtility.SetDirty(old);
                    Object.DestroyImmediate(copy);
                }
                else
                {
                    AssetDatabase.CreateAsset(copy, path);
                }
            }
        }

        public static GameObject BuildPrefab()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/Hero.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = "Hero";
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/Hero.mat");
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.sharedMaterial = mat;
                r.updateWhenOffscreen = false;
            }
            var anim = go.GetComponent<Animator>();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{Dir}/Hero.controller");

            // Her collision capsule: 1.65 m tall, feet at the root.
            // (Explicit checks, not ??: a missing component can come back as Unity's fake null.)
            var cc = go.GetComponent<CharacterController>();
            if (cc == null)
                cc = go.AddComponent<CharacterController>();
            cc.height = 1.62f;
            cc.radius = 0.24f;
            cc.center = new Vector3(0f, 0.83f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0f;
            var input = go.GetComponent<Brimful.HeroInput>();
            if (input == null)
                input = go.AddComponent<Brimful.HeroInput>();
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (go.GetComponent<Brimful.HeroController>() == null)
                go.AddComponent<Brimful.HeroController>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/Hero.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
