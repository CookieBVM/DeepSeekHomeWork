using DeepSeek;
using DeepSeek.DigitalHuman;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DigitalHumanSampleSceneBuilder
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string SentMessagePrefabPath = "Assets/DeepSeek/Prefabs/Sent Message.prefab";

    [MenuItem("Tools/Digital Human/Rebuild SampleScene UGUI")]
    public static void RebuildSampleScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        DeleteRootIfExists("DigitalHumanSystem");
        DeleteRootIfExists("DigitalHumanCanvas");
        DeleteRootIfExists("EventSystem");

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();

        GameObject system = new GameObject("DigitalHumanSystem");
        DigitalHumanGameController controller = system.AddComponent<DigitalHumanGameController>();
        DigitalHumanInputHandler inputHandler = system.AddComponent<DigitalHumanInputHandler>();
        DigitalHumanDataTracker dataTracker = system.AddComponent<DigitalHumanDataTracker>();
        DigitalHumanDataSyncService dataSyncService = system.AddComponent<DigitalHumanDataSyncService>();
        DigitalHumanAIApiService aiApiService = system.AddComponent<DigitalHumanAIApiService>();
        DigitalHumanSpeechService speechService = system.AddComponent<DigitalHumanSpeechService>();
        DigitalHumanDialogueController dialogueController = system.AddComponent<DigitalHumanDialogueController>();
        DigitalHumanChatController chatController = system.AddComponent<DigitalHumanChatController>();
        DigitalHumanColoringController coloringController = system.AddComponent<DigitalHumanColoringController>();
        DigitalHumanImitationController imitationController = system.AddComponent<DigitalHumanImitationController>();
        DigitalHumanUIView view = system.AddComponent<DigitalHumanUIView>();
        DigitalHumanAvatarView avatarView = system.AddComponent<DigitalHumanAvatarView>();
        DigitalHumanFeedbackView feedbackView = system.AddComponent<DigitalHumanFeedbackView>();
        DigitalHumanSceneTransition sceneTransition = system.AddComponent<DigitalHumanSceneTransition>();

        Canvas canvas = CreateCanvas("DigitalHumanCanvas", 800);
        RectTransform root = CreatePanel("DigitalHumanRoot", canvas.transform, new Color32(255, 247, 236, 255), true);
        Stretch(root);

        RectTransform leftPanel = CreatePanel("LeftDialoguePanel", root, new Color32(255, 255, 255, 242), true);
        Anchor(leftPanel, 0.03f, 0.05f, 0.62f, 0.95f);
        RectTransform rightPanel = CreatePanel("RightAvatarPanel", root, new Color32(248, 252, 255, 242), true);
        Anchor(rightPanel, 0.65f, 0.05f, 0.97f, 0.95f);

        RectTransform header = CreatePanel("Header", leftPanel, new Color32(255, 255, 255, 0), false);
        Anchor(header, 0.04f, 0.75f, 0.96f, 0.98f);
        Text titleText = CreateText("Title", header, "人际交流 - 买菜场景", 32, TextAnchor.MiddleLeft, font);
        Anchor(titleText.rectTransform, 0f, 0.56f, 0.68f, 1f);

        Button beginnerButton = CreateButton("BeginnerDifficultyButton", header, "初级难度", 18, font, new Color32(247, 249, 252, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)beginnerButton.transform, 0.70f, 0.62f, 0.84f, 0.96f);
        Button intermediateButton = CreateButton("IntermediateDifficultyButton", header, "进阶难度", 18, font, new Color32(247, 249, 252, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)intermediateButton.transform, 0.86f, 0.62f, 1f, 0.96f);

        Button moduleInterpersonal = CreateButton("Module_Interpersonal", header, "买菜对话", 22, font, new Color32(248, 177, 91, 255), Color.white);
        Anchor((RectTransform)moduleInterpersonal.transform, 0f, 0.03f, 0.235f, 0.47f);
        Button moduleChat = CreateButton("Module_DeepSeekChat", header, "DeepSeek聊天", 22, font, new Color32(236, 242, 249, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)moduleChat.transform, 0.255f, 0.03f, 0.49f, 0.47f);
        Button moduleColoring = CreateButton("Module_Coloring", header, "合作涂色", 22, font, new Color32(236, 242, 249, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)moduleColoring.transform, 0.51f, 0.03f, 0.745f, 0.47f);
        Button moduleImitation = CreateButton("Module_Imitation", header, "动作模仿", 22, font, new Color32(236, 242, 249, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)moduleImitation.transform, 0.765f, 0.03f, 1f, 0.47f);

        RectTransform exercisePanel = CreatePanel("ExercisePanel", leftPanel, new Color32(255, 255, 255, 0), false);
        Anchor(exercisePanel, 0f, 0f, 1f, 0.73f);
        RectTransform chatPanel = CreatePanel("DeepSeekChatPanel", leftPanel, new Color32(41, 42, 48, 255), true);
        Anchor(chatPanel, 0.02f, 0.03f, 0.98f, 0.73f);

        RectTransform dialoguePanel = CreatePanel("DialogueBox", exercisePanel, new Color32(255, 251, 244, 255), true);
        Anchor(dialoguePanel, 0.04f, 0.72f, 0.96f, 0.98f);
        Text dialogueText = CreateText("DialogueText", dialoguePanel, "小朋友，想买点什么呀？", 34, TextAnchor.MiddleCenter, font);
        Stretch(dialogueText.rectTransform, 26f, 16f, -26f, -16f);

        RectTransform optionsPanel = CreatePanel("OptionButtons", exercisePanel, new Color32(255, 255, 255, 0), false);
        Anchor(optionsPanel, 0.04f, 0.51f, 0.96f, 0.70f);
        Button[] optionButtons = new Button[3];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i] = CreateButton($"Option_{i + 1}", optionsPanel, $"选项 {i + 1}", 28, font, new Color32(248, 177, 91, 255), Color.white);
            float width = 1f / optionButtons.Length;
            Anchor((RectTransform)optionButtons[i].transform, width * i, 0.04f, width * (i + 1), 0.96f, 8f, 0f, -8f, 0f);
        }

        RectTransform inputPanel = CreatePanel("InputBox", exercisePanel, new Color32(255, 255, 255, 245), true);
        Anchor(inputPanel, 0.04f, 0.38f, 0.96f, 0.49f);
        InputField inputField = CreateInputField(inputPanel, font, "可自由输入，不限提示词；也可放入语音识别结果");
        Anchor((RectTransform)inputField.transform, 0.02f, 0.14f, 0.66f, 0.86f);
        Button submitButton = CreateButton("SubmitText", inputPanel, "发送", 24, font, new Color32(248, 177, 91, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)submitButton.transform, 0.69f, 0.14f, 0.83f, 0.86f);
        Button voiceButton = CreateButton("SubmitVoice", inputPanel, "语音", 24, font, new Color32(255, 216, 151, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)voiceButton.transform, 0.85f, 0.14f, 0.98f, 0.86f);

        RectTransform taskPanel = CreatePanel("TaskArea", exercisePanel, new Color32(245, 248, 252, 255), true);
        Anchor(taskPanel, 0.04f, 0.04f, 0.96f, 0.35f);
        Text statusText = CreateText("Status", taskPanel, "选择一个大按钮，或输入/语音说出想法", 22, TextAnchor.MiddleLeft, font);
        Anchor(statusText.rectTransform, 0.03f, 0.62f, 0.98f, 0.95f);

        RectTransform coloringRoot = CreatePanel("ColoringRoot", taskPanel, new Color32(255, 255, 255, 0), false);
        Anchor(coloringRoot, 0.03f, 0.08f, 0.64f, 0.56f);
        Button[] coloringButtons = new Button[7];
        Color32[] colorButtonColors =
        {
            new Color32(238, 83, 80, 255),
            new Color32(66, 165, 245, 255),
            new Color32(102, 187, 106, 255),
            new Color32(255, 202, 40, 255),
            new Color32(171, 71, 188, 255),
            new Color32(255, 112, 67, 255),
            new Color32(38, 198, 218, 255)
        };

        for (int i = 0; i < coloringButtons.Length; i++)
        {
            coloringButtons[i] = CreateButton($"ColorArea_{i + 1}", coloringRoot, $"区域{i + 1}", 18, font, colorButtonColors[i], new Color32(47, 52, 64, 255));
            float width = 1f / coloringButtons.Length;
            Anchor((RectTransform)coloringButtons[i].transform, width * i, 0.08f, width * (i + 1), 0.92f, 3f, 0f, -3f, 0f);
            coloringButtons[i].gameObject.SetActive(false);
        }

        Button pauseButton = CreateButton("PauseImitation", taskPanel, "暂停", 22, font, new Color32(255, 216, 151, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)pauseButton.transform, 0.66f, 0.12f, 0.80f, 0.50f);
        Button confirmImitationButton = CreateButton("ConfirmImitation", taskPanel, "完成动作", 22, font, new Color32(82, 182, 112, 255), new Color32(47, 52, 64, 255));
        Anchor((RectTransform)confirmImitationButton.transform, 0.82f, 0.12f, 0.98f, 0.50f);

        GameObject deepSeekChatWindow = CreateDeepSeekChatWindow(chatPanel);

        Text avatarTitle = CreateText("AvatarTitle", rightPanel, "3D 数字人", 30, TextAnchor.MiddleCenter, font);
        Anchor(avatarTitle.rectTransform, 0.08f, 0.88f, 0.92f, 0.98f);
        RectTransform viewportFrame = CreatePanel("AvatarViewportFrame", rightPanel, new Color32(230, 240, 252, 255), true);
        Anchor(viewportFrame, 0.08f, 0.18f, 0.92f, 0.86f);
        RawImage avatarViewport = CreateRawImage("AvatarViewport", viewportFrame);
        Stretch(avatarViewport.rectTransform, 8f, 8f, -8f, -8f);
        Button avatarButton = avatarViewport.gameObject.AddComponent<Button>();
        avatarButton.targetGraphic = avatarViewport;
        Text avatarHintText = CreateText("AvatarHint", rightPanel, "点击数字人可重复鼓励", 22, TextAnchor.MiddleCenter, font);
        Anchor(avatarHintText.rectTransform, 0.08f, 0.06f, 0.92f, 0.16f);

        CanvasGroup rewardGroup = CreateRewardSticker(canvas.transform, font, out Text rewardText);
        CanvasGroup fadeGroup = CreateFadeGroup(canvas.transform);
        AudioSource audioSource = system.AddComponent<AudioSource>();

        AssignController(controller, inputHandler, dataTracker, dataSyncService, aiApiService, speechService, dialogueController, chatController, coloringController, imitationController, view, avatarView, feedbackView, sceneTransition);
        AssignView(view, canvas, root.GetComponent<Image>(), titleText, dialogueText, statusText, inputField, submitButton, voiceButton, optionButtons, optionsPanel, inputPanel, coloringRoot, pauseButton, confirmImitationButton, moduleInterpersonal, moduleChat, moduleColoring, moduleImitation, beginnerButton, intermediateButton, coloringButtons, avatarViewport, avatarButton, avatarHintText, chatPanel, deepSeekChatWindow);
        AssignFeedback(feedbackView, canvas, rewardGroup, rewardText, audioSource);
        AssignTransition(sceneTransition, fadeGroup);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Rebuilt SampleScene digital human UGUI.");
    }

    private static void AssignController(
        DigitalHumanGameController controller,
        DigitalHumanInputHandler inputHandler,
        DigitalHumanDataTracker dataTracker,
        DigitalHumanDataSyncService dataSyncService,
        DigitalHumanAIApiService aiApiService,
        DigitalHumanSpeechService speechService,
        DigitalHumanDialogueController dialogueController,
        DigitalHumanChatController chatController,
        DigitalHumanColoringController coloringController,
        DigitalHumanImitationController imitationController,
        DigitalHumanUIView view,
        DigitalHumanAvatarView avatarView,
        DigitalHumanFeedbackView feedbackView,
        DigitalHumanSceneTransition sceneTransition)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("startModule").enumValueIndex = (int)DigitalHumanModule.InterpersonalCommunication;
        so.FindProperty("allowRuntimeComponentCreation").boolValue = false;
        SetObject(so, "inputHandler", inputHandler);
        SetObject(so, "dataTracker", dataTracker);
        SetObject(so, "dataSyncService", dataSyncService);
        SetObject(so, "aiApiService", aiApiService);
        SetObject(so, "speechService", speechService);
        SetObject(so, "dialogueController", dialogueController);
        SetObject(so, "chatController", chatController);
        SetObject(so, "coloringController", coloringController);
        SetObject(so, "imitationController", imitationController);
        SetObject(so, "view", view);
        SetObject(so, "avatarView", avatarView);
        SetObject(so, "feedbackView", feedbackView);
        SetObject(so, "sceneTransition", sceneTransition);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignView(
        DigitalHumanUIView view,
        Canvas canvas,
        Image background,
        Text titleText,
        Text dialogueText,
        Text statusText,
        InputField inputField,
        Button submitButton,
        Button voiceButton,
        Button[] optionButtons,
        RectTransform optionsPanel,
        RectTransform inputPanel,
        RectTransform coloringRoot,
        Button pauseButton,
        Button confirmImitationButton,
        Button moduleInterpersonal,
        Button moduleChat,
        Button moduleColoring,
        Button moduleImitation,
        Button beginnerButton,
        Button intermediateButton,
        Button[] coloringButtons,
        RawImage avatarViewport,
        Button avatarButton,
        Text avatarHintText,
        RectTransform chatPanel,
        GameObject deepSeekChatWindow)
    {
        SerializedObject so = new SerializedObject(view);
        SetObject(so, "canvas", canvas);
        so.FindProperty("allowRuntimeBuild").boolValue = false;
        SetObject(so, "background", background);
        SetObject(so, "titleText", titleText);
        SetObject(so, "dialogueText", dialogueText);
        SetObject(so, "statusText", statusText);
        SetObject(so, "inputField", inputField);
        SetObject(so, "submitButton", submitButton);
        SetObject(so, "voiceButton", voiceButton);
        SetArray(so, "optionButtons", optionButtons);
        SetObject(so, "optionsPanel", optionsPanel);
        SetObject(so, "inputPanel", inputPanel);
        SetObject(so, "coloringRoot", coloringRoot);
        SetObject(so, "pauseButton", pauseButton);
        SetObject(so, "confirmImitationButton", confirmImitationButton);
        SetObject(so, "moduleInterpersonalButton", moduleInterpersonal);
        SetObject(so, "moduleChatButton", moduleChat);
        SetObject(so, "moduleColoringButton", moduleColoring);
        SetObject(so, "moduleImitationButton", moduleImitation);
        SetObject(so, "beginnerDifficultyButton", beginnerButton);
        SetObject(so, "intermediateDifficultyButton", intermediateButton);
        SetArray(so, "coloringAreaButtons", coloringButtons);
        SetObject(so, "avatarViewport", avatarViewport);
        SetObject(so, "avatarHitButton", avatarButton);
        SetObject(so, "avatarHintText", avatarHintText);
        SetObject(so, "exercisePanel", (RectTransform)optionsPanel.parent);
        SetObject(so, "chatPanel", chatPanel);
        SetObject(so, "deepSeekChatWindow", deepSeekChatWindow);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignFeedback(DigitalHumanFeedbackView feedbackView, Canvas canvas, CanvasGroup stickerGroup, Text stickerText, AudioSource audioSource)
    {
        SerializedObject so = new SerializedObject(feedbackView);
        SetObject(so, "canvas", canvas);
        SetObject(so, "stickerGroup", stickerGroup);
        SetObject(so, "stickerText", stickerText);
        SetObject(so, "audioSource", audioSource);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignTransition(DigitalHumanSceneTransition transition, CanvasGroup fadeGroup)
    {
        SerializedObject so = new SerializedObject(transition);
        SetObject(so, "fadeGroup", fadeGroup);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateDeepSeekChatWindow(RectTransform parent)
    {
        RectTransform root = CreatePanel("DeepSeekChatWindow", parent, new Color32(41, 42, 48, 255), true);
        Stretch(root);
        DeepSeekChatWindow chatWindow = root.gameObject.AddComponent<DeepSeekChatWindow>();

        Text title = CreateText("Title", root, "Deep Seek API Chat", 18, TextAnchor.MiddleCenter, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        title.color = Color.white;
        Anchor(title.rectTransform, 0.04f, 0.92f, 0.96f, 0.99f);

        RectTransform messageArea = CreatePanel("Message_Area", root, new Color32(65, 66, 75, 255), true);
        Anchor(messageArea, 0.04f, 0.19f, 0.96f, 0.90f);
        ScrollRect scrollRect = CreateScrollRect(messageArea, out RectTransform content);

        RectTransform inputArea = CreatePanel("InputArea", root, new Color32(65, 66, 75, 255), true);
        Anchor(inputArea, 0.04f, 0.04f, 0.96f, 0.16f);
        TMP_InputField tmpInput = CreateTMPInputField(inputArea);
        Anchor((RectTransform)tmpInput.transform, 0.03f, 0.14f, 0.74f, 0.86f);
        Button sendButton = CreateButton("Send_Button", inputArea, "↑", 26, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), new Color32(77, 103, 255, 255), Color.white);
        Anchor((RectTransform)sendButton.transform, 0.80f, 0.14f, 0.90f, 0.86f);

        RectTransform messagePrefab = AssetDatabase.LoadAssetAtPath<RectTransform>(SentMessagePrefabPath);
        SerializedObject so = new SerializedObject(chatWindow);
        SetObject(so, "inputField", tmpInput);
        SetObject(so, "sendButton", sendButton);
        SetObject(so, "chatScroll", scrollRect);
        SetObject(so, "sent", messagePrefab);
        SetObject(so, "received", messagePrefab);
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private static ScrollRect CreateScrollRect(RectTransform parent, out RectTransform content)
    {
        GameObject scrollObject = new GameObject("Scroll View");
        scrollObject.layer = 5;
        scrollObject.transform.SetParent(parent, false);
        RectTransform scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        Stretch(scrollRectTransform, 8f, 8f, -8f, -8f);

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = CreatePanel("Viewport", scrollRectTransform, new Color32(65, 66, 75, 0), false);
        Stretch(viewport);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scrollRect.viewport = viewport;

        content = CreatePanel("Content", viewport, new Color32(65, 66, 75, 0), false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        scrollRect.content = content;
        return scrollRect;
    }

    private static TMP_InputField CreateTMPInputField(RectTransform parent)
    {
        RectTransform rect = CreatePanel("InputField (TMP)", parent, new Color32(246, 248, 252, 255), true);
        TMP_InputField field = rect.gameObject.AddComponent<TMP_InputField>();

        RectTransform textArea = CreatePanel("Text Area", rect, new Color32(255, 255, 255, 0), false);
        Stretch(textArea, 12f, 8f, -12f, -8f);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateTMPText("Placeholder", textArea, "Message Deepseek", 18, new Color32(130, 136, 150, 255));
        Stretch(placeholder.rectTransform);
        TextMeshProUGUI text = CreateTMPText("Text", textArea, string.Empty, 18, new Color32(47, 52, 64, 255));
        Stretch(text.rectTransform);

        field.textViewport = textArea;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.targetGraphic = rect.GetComponent<Image>();
        return field;
    }

    private static CanvasGroup CreateRewardSticker(Transform parent, Font font, out Text text)
    {
        RectTransform sticker = CreatePanel("RewardSticker", parent, new Color32(83, 181, 111, 235), true);
        Anchor(sticker, 0.36f, 0.64f, 0.64f, 0.82f);
        CanvasGroup group = sticker.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        text = CreateText("StickerText", sticker, "做得真好！", 38, TextAnchor.MiddleCenter, font);
        text.color = Color.white;
        Stretch(text.rectTransform, 12f, 8f, -12f, -8f);
        return group;
    }

    private static CanvasGroup CreateFadeGroup(Transform parent)
    {
        RectTransform fade = CreatePanel("SceneFade", parent, new Color32(255, 255, 255, 255), true);
        Stretch(fade);
        CanvasGroup group = fade.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        return group;
    }

    private static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static Button CreateButton(string name, Transform parent, string label, int size, Font font, Color background, Color textColor)
    {
        RectTransform rect = CreatePanel(name, parent, background, true);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Text text = CreateText("Label", rect, label, size, TextAnchor.MiddleCenter, font);
        text.color = textColor;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        Stretch(text.rectTransform, 8f, 4f, -8f, -4f);
        return button;
    }

    private static InputField CreateInputField(Transform parent, Font font, string placeholderText)
    {
        RectTransform rect = CreatePanel("TextInput", parent, new Color32(246, 248, 252, 255), true);
        InputField input = rect.gameObject.AddComponent<InputField>();
        Text text = CreateText("Text", rect, string.Empty, 24, TextAnchor.MiddleLeft, font);
        Text placeholder = CreateText("Placeholder", rect, placeholderText, 20, TextAnchor.MiddleLeft, font);
        placeholder.color = new Color32(130, 136, 150, 255);
        Stretch(text.rectTransform, 14f, 4f, -14f, -4f);
        Stretch(placeholder.rectTransform, 14f, 4f, -14f, -4f);
        input.textComponent = text;
        input.placeholder = placeholder;
        input.targetGraphic = rect.GetComponent<Image>();
        return input;
    }

    private static RawImage CreateRawImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        RawImage image = go.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = true;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Font font)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = new Color32(47, 52, 64, 255);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string value, int size, Color color)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private static void EnsureEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void DeleteRootIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void SetObject(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty property = so.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetArray<T>(SerializedObject so, string fieldName, T[] values) where T : Object
    {
        SerializedProperty property = so.FindProperty(fieldName);
        if (property == null)
        {
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        Anchor(rect, 0f, 0f, 1f, 1f, left, bottom, right, top);
    }
}
