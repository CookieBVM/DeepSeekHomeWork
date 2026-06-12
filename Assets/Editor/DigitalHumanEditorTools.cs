using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DigitalHumanEditorTools
{
    private const string ResourcesDir = "Assets/DeepSeek/Resources/DigitalHuman";
    private const string LittleWitchDir = "Assets/DeepSeek/Resources/DigitalHuman/LittleWitch";
    private const string PrefabPath = "Assets/DeepSeek/Resources/DigitalHuman/Avatar.prefab";
    private const string ControllerPath = "Assets/DeepSeek/Resources/DigitalHuman/AvatarController.controller";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    // ── Little Witch base model (appearance) ──
    private const string ModelFbx = "LittleWitch/little witch academiaelementy.fbx";

    // ── Ch46 animation FBX files (kept for animation clips, retargeted via Humanoid) ──
    private const string WavingFbx = "Ch46_nonPBR@Waving.fbx";
    private const string StandingClapFbx = "Ch46_nonPBR@Standing Clap.fbx";
    private const string CheeringFbx = "Ch46_nonPBR@Cheering.fbx";

    private static readonly string[] WavingStates =
    {
        "Greeting", "Speaking", "OfferItem",
        "ColorPrompt", "ImitationWave", "ImitationClap", "ImitationNod"
    };

    private const string StandingClapState = "Standing_Clap";
    private const string CheeringState = "Cheering";
    private const string CelebrateState = "Celebrate";

    [MenuItem("Tools/Digital Human/Setup Little Witch Avatar", false, 1)]
    public static void SetupLittleWitchAvatar()
    {
        try
        {
            AssetDatabase.DisallowAutoRefresh();
            EnsureResourcesFolder();
            ConfigureFbxImports();
            AssetDatabase.Refresh();
            AnimatorController controller = BuildAnimatorController();
            CreateAvatarPrefab(controller);
            AssetDatabase.AllowAutoRefresh();
            AssetDatabase.Refresh();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog(
                "完成",
                "Little Witch 数字人已配置！\n\n" +
                "模型: " + ModelFbx + "\n" +
                "动画 (Humanoid 重定向):\n" +
                "  点击 / 进入模块 → Waving\n" +
                "  选择 / 涂色 → Standing Clap\n" +
                "  完成 → Cheering",
                "好的");
        }
        catch (System.Exception ex)
        {
            AssetDatabase.AllowAutoRefresh();
            Debug.LogError("[DigitalHumanEditorTools] Setup failed: " + ex);
            EditorUtility.DisplayDialog("失败", ex.Message, "好的");
        }
    }

    [MenuItem("Tools/Digital Human/Rebuild SampleScene", false, 2)]
    public static void RebuildScene()
    {
        try
        {
            DigitalHumanSampleSceneBuilder.RebuildSampleScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DigitalHumanEditorTools] Scene rebuild failed: " + ex);
            EditorUtility.DisplayDialog("重建失败", ex.Message, "好的");
        }
    }

    private static void ConfigureFbxImports()
    {
        // Little Witch model: Humanoid, import model + materials
        ConfigureSingleFbx(ModelFbx, importModel: true);

        // Ch46 animation FBX files: Humanoid, animation only (no duplicate models)
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
            Debug.LogWarning($"[DigitalHumanEditorTools] FBX not found: {fbxPath}");
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

    private static AnimatorController BuildAnimatorController()
    {
        if (File.Exists(ControllerPath))
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.SaveAssets();
        }

        AnimationClip wavingClip = LoadClipFromFbx(WavingFbx);
        AnimationClip standingClapClip = LoadClipFromFbx(StandingClapFbx);
        AnimationClip cheeringClip = LoadClipFromFbx(CheeringFbx);

        if (wavingClip == null || standingClapClip == null || cheeringClip == null)
        {
            throw new System.Exception("缺少动画片段。请确保 Ch46 FBX 已导入。");
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var rootSM = controller.layers[0].stateMachine;

        AnimatorState idleState = rootSM.AddState("Idle");
        rootSM.defaultState = idleState;

        foreach (string stateName in WavingStates)
        {
            AddTriggerState(controller, rootSM, stateName, wavingClip, idleState);
        }

        AddTriggerState(controller, rootSM, StandingClapState, standingClapClip, idleState);
        AddTriggerState(controller, rootSM, CheeringState, cheeringClip, idleState);
        AddTriggerState(controller, rootSM, CelebrateState, cheeringClip, idleState);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
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
            if (asset is AnimationClip clip)
            {
                return clip;
            }
        }

        return null;
    }

    private static void CreateAvatarPrefab(AnimatorController controller)
    {
        string fbxPath = ResourcesDir + "/" + ModelFbx;
        GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxModel == null)
        {
            throw new System.Exception($"无法加载 FBX 模型: {fbxPath}");
        }

        GameObject instance = Object.Instantiate(fbxModel);
        instance.name = "DigitalHumanAvatar";

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (File.Exists(PrefabPath))
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
        {
            string parent = Path.GetDirectoryName(ResourcesDir).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent))
            {
                Directory.CreateDirectory(parent);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateFolder(parent, "DigitalHuman");
            AssetDatabase.Refresh();
        }

        if (!AssetDatabase.IsValidFolder(LittleWitchDir))
        {
            AssetDatabase.CreateFolder(ResourcesDir, "LittleWitch");
            AssetDatabase.Refresh();
        }
    }
}
