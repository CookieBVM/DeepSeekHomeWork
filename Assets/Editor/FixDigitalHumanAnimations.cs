using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class FixDigitalHumanAnimations
{
    private const string PrefabPath = "Assets/Resources/DigitalHuman/Avatar.prefab";
    private const string ControllerPath = "Assets/Resources/DigitalHuman/AvatarController.controller";

    private const string WavingPath = "Assets/Resources/DigitalHuman/Waving.fbx";
    private const string ClapPath = "Assets/Resources/DigitalHuman/StandingClap.fbx";
    private const string CheeringPath = "Assets/Resources/DigitalHuman/Cheering.fbx";

    [MenuItem("Tools/Digital Human/Fix Animations", false, 1)]
    public static void FixAll()
    {
        // Step 1: Force animation clip import
        ForceImportClip(WavingPath, "Waving", true);
        ForceImportClip(ClapPath, "StandingClap", true);
        ForceImportClip(CheeringPath, "Cheering", false);

        AssetDatabase.ImportAsset(WavingPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ClapPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(CheeringPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Step 2: Load clips
        AnimationClip waving = LoadClip(WavingPath);
        AnimationClip clap = LoadClip(ClapPath);
        AnimationClip cheering = LoadClip(CheeringPath);

        if (waving == null || clap == null || cheering == null)
        {
            EditorUtility.DisplayDialog("Import Failed",
                "Animation clips not found. Check Console for details.\n" +
                "waving=" + (waving != null) + " clap=" + (clap != null) + " cheering=" + (cheering != null),
                "OK");
            return;
        }

        // Step 3: Build controller
        AnimatorController ctrl = BuildController(waving, clap, cheering);

        // Step 4: Assign to prefab
        AssignToPrefab(ctrl);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done",
            "AnimatorController rebuilt.\nStates: Idle, Greeting, ImitationClap, Celebrate\n" +
            "Avatar.prefab updated (culling=AlwaysAnimate, rootMotion=false).\nRun SampleScene to test.",
            "OK");
    }

    private static void ForceImportClip(string path, string name, bool loop)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;

        var clips = imp.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
            clips = imp.clipAnimations;

        if (clips == null || clips.Length == 0)
        {
            clips = new ModelImporterClipAnimation[1];
            clips[0] = new ModelImporterClipAnimation
            {
                name = name,
                takeName = "mixamo.com",
                firstFrame = 0, lastFrame = 0,
                loopTime = loop, loopPose = loop,
                keepOriginalOrientation = true,
                keepOriginalPositionY = true,
                keepOriginalPositionXZ = true,
            };
        }
        else
        {
            clips[0].name = name;
            clips[0].loopTime = loop;
            clips[0].loopPose = loop;
        }

        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__"));
    }

    private static AnimatorController BuildController(AnimationClip w, AnimationClip c, AnimationClip ch)
    {
        if (File.Exists(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle", new Vector3(240f, 0f, 0f));
        sm.defaultState = idle;

        AddState(ctrl, sm, idle, "Greeting",      w,  new Vector3(480f, -200f, 0f));
        AddState(ctrl, sm, idle, "ImitationClap", c,  new Vector3(480f,    0f, 0f));
        AddState(ctrl, sm, idle, "Celebrate",     ch, new Vector3(480f,  200f, 0f));
        AddState(ctrl, sm, idle, "ImitationWave", w,  new Vector3(480f, -100f, 0f));
        AddState(ctrl, sm, idle, "Standing_Clap", c,  new Vector3(720f,    0f, 0f));
        AddState(ctrl, sm, idle, "Cheering",      ch, new Vector3(720f,  100f, 0f));
        AddState(ctrl, sm, idle, "Speaking",      w,  new Vector3(720f, -200f, 0f));
        AddState(ctrl, sm, idle, "ColorPrompt",   w,  new Vector3(720f, -100f, 0f));
        AddState(ctrl, sm, idle, "OfferItem",     w,  new Vector3(720f,  200f, 0f));
        AddState(ctrl, sm, idle, "ImitationNod",  w,  new Vector3(720f,  300f, 0f));

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static void AddState(AnimatorController ctrl, AnimatorStateMachine sm,
        AnimatorState idle, string name, Motion motion, Vector3 pos)
    {
        ctrl.AddParameter(name, AnimatorControllerParameterType.Trigger);
        var s = sm.AddState(name, pos);
        s.motion = motion;
        s.writeDefaultValues = true;

        var enter = sm.AddAnyStateTransition(s);
        enter.hasExitTime = false;
        enter.duration = 0.15f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, name);

        var exit = s.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = motion == null ? 0.05f : 0.92f;
        exit.duration = 0.18f;
    }

    private static void AssignToPrefab(AnimatorController ctrl)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("Avatar.prefab not found"); return; }

        var anim = prefab.GetComponent<Animator>();
        if (anim == null) { Debug.LogError("No Animator on prefab"); return; }

        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.updateMode = AnimatorUpdateMode.Normal;

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssetIfDirty(prefab);
        Debug.Log("Prefab updated: culling=AlwaysAnimate, rootMotion=false");
    }
}