using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace DeepSeek.DigitalHuman.Editor
{
    public class MixamoAnimationSetup
    {
        private const string ResourcesDir = "Assets/Resources/DigitalHuman";
        private const string ModelFile = "Ch46_nonPBR.dae";
        private const string ControllerPath = "Assets/Resources/DigitalHuman/AvatarController.controller";

        private const string IdleFbx = "Standing Idle.fbx";
        private const string WavingFbx = "Waving.fbx";
        private const string ClappingFbx = "Clapping.fbx";
        private const string CheeringFbx = "Cheering.fbx";

        private static readonly string[] WavingTriggers = { "Greeting" };

        private static readonly string[] ClappingTriggers =
        {
            "Speaking", "OfferItem", "ColorPrompt",
            "ImitationWave", "ImitationClap", "ImitationNod"
        };

        private static readonly string[] CheeringTriggers = { "Cheering", "Celebrate" };

        [MenuItem("DeepSeek/Setup Mixamo Animations", false, 100)]
        public static void SetupAnimations()
        {
            ConfigureAllFbxAsHumanoid();
            AssetDatabase.Refresh();
            BuildAnimatorController();
        }

        private static void ConfigureAllFbxAsHumanoid()
        {
            ConfigureSingleFbx(ModelFile, importModel: true);
            ConfigureSingleFbx(IdleFbx, importModel: true);
            ConfigureSingleFbx(WavingFbx, importModel: true);
            ConfigureSingleFbx(ClappingFbx, importModel: false);
            ConfigureSingleFbx(CheeringFbx, importModel: false);
        }

        private static void ConfigureSingleFbx(string relativePath, bool importModel)
        {
            string fbxPath = ResourcesDir + "/" + relativePath;
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[MixamoAnimationSetup] FBX not found: " + fbxPath);
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importVisibility = importModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static void BuildAnimatorController()
        {
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
                AssetDatabase.Refresh();
            }

            AnimationClip idleClip = LoadClipFromFbx(IdleFbx);
            AnimationClip wavingClip = LoadClipFromFbx(WavingFbx);
            AnimationClip clappingClip = LoadClipFromFbx(ClappingFbx);
            AnimationClip cheeringClip = LoadClipFromFbx(CheeringFbx);

            if (wavingClip == null)
            {
                Debug.LogError("[MixamoAnimationSetup] Waving clip not found!");
                return;
            }

            // Non-idle clips: play once then exit
            SetClipLoop(wavingClip, false);
            SetClipLoop(clappingClip, false);
            SetClipLoop(cheeringClip, false);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var rootSM = controller.layers[0].stateMachine;

            AnimatorState idleState = rootSM.AddState("Idle");
            rootSM.defaultState = idleState;
            if (idleClip != null)
            {
                idleState.motion = idleClip;
            }

            foreach (string t in WavingTriggers)
                AddTriggerState(controller, rootSM, t, wavingClip, idleState);

            if (clappingClip != null)
                foreach (string t in ClappingTriggers)
                    AddTriggerState(controller, rootSM, t, clappingClip, idleState);

            if (cheeringClip != null)
                foreach (string t in CheeringTriggers)
                    AddTriggerState(controller, rootSM, t, cheeringClip, idleState);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MixamoAnimationSetup] Done! " +
                      "Waving={" + string.Join(",", WavingTriggers) + "} " +
                      "Clapping={" + string.Join(",", ClappingTriggers) + "} " +
                      "Cheering={" + string.Join(",", CheeringTriggers) + "}");
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
            state.speed = 1f;

            AnimatorStateTransition anyTransition = stateMachine.AddAnyStateTransition(state);
            anyTransition.AddCondition(AnimatorConditionMode.If, 0, stateName);
            anyTransition.hasExitTime = false;
            anyTransition.duration = 0.1f;
            anyTransition.canTransitionToSelf = true;

            AnimatorStateTransition backTransition = state.AddTransition(idleState);
            backTransition.hasExitTime = true;
            backTransition.exitTime = 0.99f;
            backTransition.duration = 0.1f;
        }

        private static void SetClipLoop(AnimationClip clip, bool loop)
        {
            if (clip == null) return;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static AnimationClip LoadClipFromFbx(string relativePath)
        {
            string fbxPath = ResourcesDir + "/" + relativePath;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip)
                    return clip;
            }
            return null;
        }

        [MenuItem("DeepSeek/Setup Mixamo Animations", true)]
        public static bool SetupAnimationsValidate()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesDir + "/" + ModelFile) != null;
        }
    }
}