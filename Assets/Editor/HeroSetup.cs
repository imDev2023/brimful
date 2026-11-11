// Build the hero's material and prefab, and copy the clips the game uses out of Quaternius's
// libraries. Run after tools/rig_unity.py writes Assets/Characters/Hero, or after the
// libraries change:
//
//   unity command script eval 'Brimful.EditorTools.HeroSetup.BuildAll(); return "ok";'
//
// Every step is idempotent: it overwrites what it made before.

using System.Linq;
using UnityEditor;
using UnityEngine;

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
            BuildPrefab();
            AssetDatabase.SaveAssets();
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

        public static void CopyClips()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(ClipDir))
                AssetDatabase.CreateFolder("Assets/Animations", "Quaternius");
            foreach (var (file, name) in Clips)
            {
                var src = AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name == $"Armature|{name}");
                if (src == null)
                    throw new System.Exception($"{name} is not in {file}");
                var copy = Object.Instantiate(src);
                copy.name = name;
                AnimationUtility.SetAnimationClipSettings(copy, AnimationUtility.GetAnimationClipSettings(src));
                var path = $"{ClipDir}/{name}.anim";
                var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (old != null)
                {
                    EditorUtility.CopySerialized(copy, old);
                    old.name = name;
                    EditorUtility.SetDirty(old);
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
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/Hero.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
