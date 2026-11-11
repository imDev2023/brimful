// Import settings by folder, so a fresh clone imports every model and texture the same way.
//
// Assets/Characters/<name>/   our rigged characters (tools/rig_unity.py): Humanoid avatar from
//                             the Mixamo bone names, no clips, no materials (each character's
//                             URP material is built by HeroSetup from its textures).
// Assets/_Checks/             throwaway exports checked once, imported like a character.
// Assets/_Imported/Quaternius/ Quaternius's Universal Animation Library FBX files (CC0), kept
//                             out of git: Humanoid, axis conversion baked as Quaternius's setup
//                             asks, every clip in place (root motion baked into the pose, since
//                             the character controller moves the hero), and clips whose names
//                             end in _Loop looped. The clips the game uses are copied out to
//                             Assets/Animations by HeroSetup.

using System.Linq;
using UnityEditor;

namespace Brimful.EditorTools
{
    public class BrimfulImport : AssetPostprocessor
    {
        static bool IsCharacter(string path) =>
            path.StartsWith("Assets/Characters/") || path.StartsWith("Assets/_Checks/");

        static bool IsQuaternius(string path) => path.StartsWith("Assets/_Imported/Quaternius/");

        void OnPreprocessModel()
        {
            var mi = (ModelImporter)assetImporter;
            if (!IsCharacter(assetPath) && !IsQuaternius(assetPath))
                return;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.importVisibility = false;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            if (IsCharacter(assetPath))
            {
                // tools/rig_unity.py bakes the axis change into the FBX already.
                mi.bakeAxisConversion = false;
                mi.importAnimation = false;
                mi.importNormals = ModelImporterNormals.Import;
                mi.importTangents = ModelImporterTangents.CalculateMikk;
                mi.meshCompression = ModelImporterMeshCompression.Off;
                mi.isReadable = false;
            }
            else
            {
                mi.bakeAxisConversion = true;
                mi.importAnimation = true;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!IsQuaternius(assetPath))
                return;
            var mi = (ModelImporter)assetImporter;
            mi.clipAnimations = mi.defaultClipAnimations.Select(c =>
            {
                c.loopTime = c.name.EndsWith("_Loop");
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = true;
                return c;
            }).ToArray();
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Characters/"))
                return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.mipmapEnabled = true;
            if (assetPath.EndsWith("_normal.png"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (assetPath.EndsWith("_metallicsmoothness.png"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = false;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
            }
        }
    }
}
