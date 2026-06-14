using UnityEngine;
using UnityEditor;

public class SetupAvatarScene : EditorWindow
{
    [MenuItem("DeepSeek/Setup Avatar Scene")]
    public static void ShowWindow()
    {
        GetWindow<SetupAvatarScene>("Setup Avatar");
    }

    private GameObject avatarModel;

    void OnGUI()
    {
        GUILayout.Label("Setup Digital Human Avatar Scene", EditorStyles.boldLabel);
        GUILayout.Space(10);

        avatarModel = (GameObject)EditorGUILayout.ObjectField("Avatar Model", avatarModel, typeof(GameObject), false);

        if (GUILayout.Button("Create Avatar Scene Structure"))
        {
            CreateAvatarScene();
        }

        GUILayout.Space(20);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Drag your avatar model to the field above");
        GUILayout.Label("2. Click 'Create Avatar Scene Structure'");
        GUILayout.Label("3. The avatar will be placed under DigitalHumanCanvas/DigitalHumanAvatarScene");
    }

    private void CreateAvatarScene()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene!");
            return;
        }

        Transform existingScene = canvas.transform.Find("DigitalHumanAvatarScene");
        if (existingScene != null)
        {
            DestroyImmediate(existingScene.gameObject);
        }

        GameObject sceneRoot = new GameObject("DigitalHumanAvatarScene");
        sceneRoot.transform.SetParent(canvas.transform, false);
        sceneRoot.transform.localPosition = Vector3.zero;
        sceneRoot.transform.localRotation = Quaternion.identity;
        sceneRoot.transform.localScale = Vector3.one;

        GameObject cameraObj = new GameObject("AvatarCamera");
        cameraObj.transform.SetParent(sceneRoot.transform, false);
        cameraObj.transform.localPosition = new Vector3(0f, -0.2f, 3.8f);
        cameraObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(230, 240, 252, 255);
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;

        GameObject keyLight = new GameObject("AvatarKeyLight");
        keyLight.transform.SetParent(sceneRoot.transform, false);
        keyLight.transform.localPosition = new Vector3(3f, 3f, 3f);
        keyLight.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
        Light key = keyLight.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = Color.white;
        key.intensity = 1.2f;

        GameObject fillLight = new GameObject("AvatarFillLight");
        fillLight.transform.SetParent(sceneRoot.transform, false);
        fillLight.transform.localPosition = new Vector3(-2f, 1f, -2f);
        fillLight.transform.localRotation = Quaternion.Euler(-30f, -30f, 0f);
        Light fill = fillLight.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.8f, 0.8f, 1f);
        fill.intensity = 0.4f;

        if (avatarModel != null)
        {
            GameObject avatarInstance = PrefabUtility.InstantiatePrefab(avatarModel) as GameObject;
            avatarInstance.name = "RuntimeDigitalHumanAvatar";
            avatarInstance.transform.SetParent(sceneRoot.transform, false);
            avatarInstance.transform.localPosition = new Vector3(0f, -0.6f, 200f);
            avatarInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            avatarInstance.transform.localScale = Vector3.one * 1.2f;

            Animator animator = avatarInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        Selection.activeGameObject = sceneRoot;
        Debug.Log("Avatar scene structure created successfully!");
    }
}