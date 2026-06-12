using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

namespace DeepSeek.DigitalHuman.Editor
{
    public class MixamoAvatarSetup
    {
        private const string ResourcesDir = "Assets/DeepSeek/Resources/DigitalHuman";
        private const string ModelFbx = "LittleWitch/little witch academiaelementy.fbx";
        private const string WavingFbx = "Ch46_nonPBR@Waving.fbx";
        private const string StandingClapFbx = "Ch46_nonPBR@Standing Clap.fbx";
        private const string CheeringFbx = "Ch46_nonPBR@Cheering.fbx";
        private const string PrefabPath = "Assets/DeepSeek/Resources/DigitalHuman/Avatar.prefab";
        private const string ControllerPath = "Assets/DeepSeek/Resources/DigitalHuman/AvatarController.controller";

        private static readonly string[] WavingStates =
        {
            "Greeting", "Speaking", "OfferItem",
            "ColorPrompt", "ImitationWave", "ImitationClap", "ImitationNod"
        };

        [MenuItem("Tools/Digital Human/Setup Mixamo Avatar", false, 100)]
        public static void SetupAvatar()
        {
            if (!IsFbxImported())
            {
                EditorUtility.DisplayDialog("Import Required",
                    "Please open the Project window and expand Assets/DeepSeek/Resources/DigitalHuman " +
                    "first so Unity imports the LittleWitch FBX, then run this setup again.",
                    "OK");
                AssetDatabase.Refresh();
                return;
            }

            ConfigureFbxAsHumanoid();
            CleanupOldAssets();
            AssignAnimationsToController();
            CreateAvatarPrefab();

            Debug.Log("[MixamoAvatarSetup] âœ“ Little Witch digital human avatar is ready.");
        }

        private static bool IsFbxImported()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesDir + "/" + ModelFbx) != null;
        }

        private static void ConfigureFbxAsHumanoid()
        {
            ConfigureSingleFbx(ModelFbx, importModel: true);
            ConfigureSingleFbx(WavingFbx, importModel: true);
            ConfigureSingleFbx(StandingClapFbx, importModel: false);
            ConfigureSingleFbx(CheeringFbx, importModel: false);
        }

        private static void ConfigureSingleFbx(string relativePath, bool importModel)
        {
            string fbxPath = ResourcesDir + "/" + relativePath;
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[MixamoAvatarSetup] FBX not found at {fbxPath}.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importVisibility = importModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static void CleanupOldAssets()
        {
            string oldPlaceholder = "Assets/Resources/DigitalHuman/Avatar_placeholder.prefab";
            if (File.Exists(oldPlaceholder))
            {
                AssetDatabase.DeleteAsset(oldPlaceholder);
            }

            string oldAvatar = "Assets/Resources/DigitalHuman/Avatar_Humanoid.asset";
            if (File.Exists(oldAvatar))
            {
                AssetDatabase.DeleteAsset(oldAvatar);
            }

            AssetDatabase.Refresh();
        }

        private static void AssignAnimationsToController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) return;

            AnimationClip wavingClip = LoadClipFromFbx(WavingFbx);
            AnimationClip standingClapClip = LoadClipFromFbx(StandingClapFbx);
            AnimationClip cheeringClip = LoadClipFromFbx(CheeringFbx);

            foreach (ChildAnimatorState childState in controller.layers[0].stateMachine.states)
            {
                string stateName = childState.state.name;
                if (stateName == "Idle") continue;

                AnimationClip match = null;
                if (System.Array.IndexOf(WavingStates, stateName) >= 0)
                    match = wavingClip;
                else if (stateName == "Standing_Clap")
                    match = standingClapClip;
                else if (stateName == "Cheering" || stateName == "Celebrate")
                    match = cheeringClip;

                if (match != null)
                {
                    childState.state.motion = match;
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void CreateAvatarPrefab()
        {
            string fbxPath = ResourcesDir + "/" + ModelFbx;
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxModel == null) return;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (File.Exists(PrefabPath))
                AssetDatabase.DeleteAsset(PrefabPath);

            GameObject instance = Object.Instantiate(fbxModel);
            instance.name = "DigitalHumanAvatar";

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();

            if (controller != null)
                animator.runtimeAnimatorController = controller;

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
        }

        private static AnimationClip LoadClipFromFbx(string relativePath)
        {
            string fbxPath = ResourcesDir + "/" + relativePath;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip) return clip;
            }

            return null;
        }
    }
}
