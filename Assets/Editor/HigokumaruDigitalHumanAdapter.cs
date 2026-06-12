using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HigokumaruDigitalHumanAdapter
{
    private const string ModelPath = "Assets/External/DigitalHuman/Higokumaru/source/Higokumaru.fbx";
    private const string WavingPath = "Assets/External/DigitalHuman/Higokumaru/Animations/Ch46_nonPBR@Waving.fbx";
    private const string ClapPath = "Assets/External/DigitalHuman/Higokumaru/Animations/Ch46_nonPBR@Standing Clap.fbx";
    private const string CheeringPath = "Assets/External/DigitalHuman/Higokumaru/Animations/Ch46_nonPBR@Cheering.fbx";
    private const string ControllerPath = "Assets/External/DigitalHuman/Higokumaru/HigokumaruDigitalHuman.controller";
    private const string RuntimePrefabPath = "Assets/Resources/DigitalHuman/Avatar.prefab";

    [MenuItem("Tools/Digital Human/Setup Higokumaru Avatar")]
    [MenuItem("Higokumaru/Setup Avatar")]
    public static void SetupHigokumaruAvatar()
    {
        if (!ValidateRequiredAssets())
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
        Directory.CreateDirectory(Path.GetDirectoryName(RuntimePrefabPath));

        ConfigureModelAsHumanoid(ModelPath, ModelImporterAvatarSetup.CreateFromThisModel, null, importAnimation: false);
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);

        Avatar higokumaruAvatar = LoadHumanoidAvatar(ModelPath);
        if (higokumaruAvatar == null || !higokumaruAvatar.isValid || !higokumaruAvatar.isHuman)
        {
            EditorUtility.DisplayDialog(
                "Higokumaru Humanoid setup failed",
                "Unity could not create a valid Humanoid Avatar for Higokumaru. Open the model Rig tab, click Configure, and map the bones manually. If the model cannot become Humanoid, retarget the animations in Blender first.",
                "OK");
            return;
        }

        ConfigureModelAsHumanoid(WavingPath, ModelImporterAvatarSetup.CopyFromOther, higokumaruAvatar, importAnimation: true, clipName: "Waving", loop: true);
        ConfigureModelAsHumanoid(ClapPath, ModelImporterAvatarSetup.CopyFromOther, higokumaruAvatar, importAnimation: true, clipName: "StandingClap", loop: true);
        ConfigureModelAsHumanoid(CheeringPath, ModelImporterAvatarSetup.CopyFromOther, higokumaruAvatar, importAnimation: true, clipName: "Cheering", loop: false);
        AssetDatabase.ImportAsset(WavingPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ClapPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(CheeringPath, ImportAssetOptions.ForceUpdate);

        AnimationClip waving = LoadAnimationClip(WavingPath);
        AnimationClip clap = LoadAnimationClip(ClapPath);
        AnimationClip cheering = LoadAnimationClip(CheeringPath);
        AnimatorController controller = CreateController(waving, clap, cheering);
        CreateRuntimePrefab(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Higokumaru setup complete",
            "Created Resources/DigitalHuman/Avatar.prefab. Run SampleScene and test Greeting, ImitationWave, ImitationClap, and Celebrate. If a motion looks twisted, the FBX files need manual Humanoid bone mapping or Blender retargeting.",
            "OK");
    }

    private static bool ValidateRequiredAssets()
    {
        string[] paths = { ModelPath, WavingPath, ClapPath, CheeringPath };
        string missing = string.Join("\n", paths.Where(path => !File.Exists(path)));
        if (string.IsNullOrWhiteSpace(missing))
        {
            return true;
        }

        EditorUtility.DisplayDialog("Missing Higokumaru assets", missing, "OK");
        return false;
    }

    [MenuItem("Higokumaru/Dump Model Hierarchy")]
    public static void DumpModelHierarchy()
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog("Higokumaru model missing", ModelPath, "OK");
            return;
        }

        Directory.CreateDirectory("Assets/Resources/DigitalHuman");
        string reportPath = "Assets/Resources/DigitalHuman/HigokumaruHierarchy.txt";
        using (var writer = new StreamWriter(reportPath, false))
        {
            writer.WriteLine("Higokumaru Transform Hierarchy");
            writer.WriteLine(ModelPath);
            writer.WriteLine();
            WriteTransform(writer, modelPrefab.transform, 0);
        }

        AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceUpdate);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(reportPath);
        EditorUtility.DisplayDialog("Hierarchy dumped", reportPath, "OK");
    }

    private static void WriteTransform(StreamWriter writer, Transform transform, int depth)
    {
        string indent = new string(' ', depth * 2);
        writer.WriteLine($"{indent}{transform.name}");
        for (int i = 0; i < transform.childCount; i++)
        {
            WriteTransform(writer, transform.GetChild(i), depth + 1);
        }
    }

    private static void ConfigureModelAsHumanoid(
        string assetPath,
        ModelImporterAvatarSetup avatarSetup,
        Avatar sourceAvatar,
        bool importAnimation,
        string clipName = null,
        bool loop = false)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"ModelImporter not found for {assetPath}");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = avatarSetup;
        importer.sourceAvatar = sourceAvatar;
        importer.importAnimation = importAnimation;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        if (importAnimation && !string.IsNullOrWhiteSpace(clipName))
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                clips[0].name = clipName;
                clips[0].loopTime = loop;
                clips[0].loopPose = loop;
                importer.clipAnimations = clips;
            }
        }

        importer.SaveAndReimport();
    }

    private static Avatar LoadHumanoidAvatar(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Avatar>()
            .FirstOrDefault(avatar => avatar != null && avatar.isValid && avatar.isHuman);
    }

    private static AnimationClip LoadAnimationClip(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__"));
    }

    private static AnimatorController CreateController(AnimationClip waving, AnimationClip clap, AnimationClip cheering)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(240f, 0f, 0f));
        stateMachine.defaultState = idle;

        AddPoseState(controller, stateMachine, idle, "Greeting", waving, new Vector3(520f, -180f, 0f));
        AddPoseState(controller, stateMachine, idle, "ImitationWave", waving, new Vector3(520f, -90f, 0f));
        AddPoseState(controller, stateMachine, idle, "ImitationClap", clap, new Vector3(520f, 0f, 0f));
        AddPoseState(controller, stateMachine, idle, "Celebrate", cheering, new Vector3(520f, 90f, 0f));

        AddPoseState(controller, stateMachine, idle, "Speaking", waving, new Vector3(840f, -180f, 0f));
        AddPoseState(controller, stateMachine, idle, "OfferItem", waving, new Vector3(840f, -90f, 0f));
        AddPoseState(controller, stateMachine, idle, "ColorPrompt", waving, new Vector3(840f, 0f, 0f));
        AddPoseState(controller, stateMachine, idle, "ImitationNod", waving, new Vector3(840f, 90f, 0f));
        AddPoseState(controller, stateMachine, idle, "Standing_Clap", clap, new Vector3(840f, 120f, 0f));
        AddPoseState(controller, stateMachine, idle, "Cheering", cheering, new Vector3(840f, 150f, 0f));

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddPoseState(
        AnimatorController controller,
        AnimatorStateMachine stateMachine,
        AnimatorState idle,
        string triggerName,
        Motion motion,
        Vector3 position)
    {
        controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);

        AnimatorState state = stateMachine.AddState(triggerName, position);
        state.motion = motion;
        state.writeDefaultValues = true;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0.15f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

        AnimatorStateTransition exit = state.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = motion == null ? 0.05f : 0.92f;
        exit.duration = 0.18f;
    }

    private static void CreateRuntimePrefab(RuntimeAnimatorController controller)
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"Could not load Higokumaru model prefab at {ModelPath}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        instance.name = "HigokumaruDigitalHumanAvatar";

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        PrefabUtility.SaveAsPrefabAsset(instance, RuntimePrefabPath);
        Object.DestroyImmediate(instance);
    }
}
