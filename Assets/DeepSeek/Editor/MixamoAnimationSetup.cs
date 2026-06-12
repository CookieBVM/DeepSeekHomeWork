using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

namespace DeepSeek.DigitalHuman.Editor
{
    public class MixamoAnimationSetup
    {
        private const string ResourcesDir = "Assets/DeepSeek/Resources/DigitalHuman";
        private const string ModelFbx = "LittleWitch/little witch academiaelementy.fbx";
        private const string PrefabPath = "Assets/DeepSeek/Resources/DigitalHuman/Avatar.prefab";
        private const string ControllerPath = "Assets/DeepSeek/Resources/DigitalHuman/AvatarController.controller";

        private const string WavingFbx = "Ch46_nonPBR@Waving.fbx";
        private const string StandingClapFbx = "Ch46_nonPBR@Standing Clap.fbx";
        private const string CheeringFbx = "Ch46_nonPBR@Cheering.fbx";

        private static readonly string[] WavingStates =
        {
            "Greeting", "Speaking", "OfferItem",
            "ColorPrompt", "ImitationWave", "ImitationClap", "ImitationNod"
        };

        [MenuItem("Tools/DigitalHuman/Setup Mixamo Animations", false, 100)]
        public static void SetupAnimations()
        {
            ConfigureAllFbxAsHumanoid();
            BuildAnimatorController();
            CreateAvatarPrefab();

            Debug.Log("[MixamoAnimationSetup] ✓ Little Witch + Ch46 animations ready.");
        }

        private static void ConfigureAllFbxAsHumanoid()
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
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importVisibility = importModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static void BuildAnimatorController()
        {
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
                AssetDatabase.SaveAssets();
            }

            AnimationClip wavingClip = LoadClipFromFbx(WavingFbx);
            AnimationClip standingClapClip = LoadClipFromFbx(StandingClapFbx);
            AnimationClip cheeringClip = LoadClipFromFbx(CheeringFbx);

            if (wavingClip == null) return;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var rootSM = controller.layers[0].stateMachine;

            AnimatorState idleState = rootSM.AddState("Idle");
            rootSM.defaultState = idleState;

            foreach (string stateName in WavingStates)
                AddTriggerState(controller, rootSM, stateName, wavingClip, idleState);

            if (standingClapClip != null)
                AddTriggerState(controller, rootSM, "Standing_Clap", standingClapClip, idleState);

            if (cheeringClip != null)
            {
                AddTriggerState(controller, rootSM, "Cheering", cheeringClip, idleState);
                AddTriggerState(controller, rootSM, "Celebrate", cheeringClip, idleState);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddTriggerState(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            AnimatorState idleState)
        {
            controller.AddParameter(stateName, AnimatorControllerParameterType.Trigger);
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;

            AnimatorStateTransition anyTransition = stateMachine.AddAnyStateTransition(state);
            anyTransition.AddCondition(AnimatorConditionMode.If, 0, stateName);
            anyTransition.hasExitTime = false;
            anyTransition.duration = 0.25f;

            AnimatorStateTransition backTransition = state.AddTransition(idleState);
            backTransition.hasExitTime = true;
            backTransition.exitTime = 0.95f;
            backTransition.duration = 0.2f;
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
    }
}
