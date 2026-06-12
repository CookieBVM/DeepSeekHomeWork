using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanUIView : MonoBehaviour
    {
        public event Action<DigitalHumanModule> ModuleRequested;
        public event Action<DigitalHumanDifficulty> DifficultyRequested;
        public event Action<string> OptionSelected;
        public event Action<string> TextSubmitted;
        public event Action<string> VoiceTextSubmitted;
        public event Action<string, DigitalHumanParticipant> ColoringAreaSelected;
        public event Action ImitationPauseRequested;
        public event Action ImitationConfirmRequested;
        public event Action AvatarClicked;
        public event Action<string> ChatSubmitted;
        public event Action ChatClearRequested;

        [SerializeField] private Canvas canvas;
        [SerializeField, HideInInspector] private bool allowRuntimeBuild;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Text statusText;
        [SerializeField] private InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button voiceButton;
        [SerializeField] private Button[] optionButtons = new Button[3];
        [SerializeField] private RectTransform optionsPanel;
        [SerializeField] private RectTransform inputPanel;
        [SerializeField] private RectTransform coloringRoot;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button confirmImitationButton;
        [SerializeField] private Button moduleInterpersonalButton;
        [SerializeField] private Button moduleChatButton;
        [SerializeField] private Button moduleColoringButton;
        [SerializeField] private Button moduleImitationButton;
        [SerializeField] private Button beginnerDifficultyButton;
        [SerializeField] private Button intermediateDifficultyButton;
        [SerializeField] private Button[] coloringAreaButtons = new Button[7];
        [SerializeField] private RawImage avatarViewport;
        [SerializeField] private Button avatarHitButton;
        [SerializeField] private Text avatarHintText;
        [SerializeField] private RectTransform exercisePanel;
        [SerializeField] private RectTransform chatPanel;
        [SerializeField] private ScrollRect chatScroll;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button chatSendButton;
        [SerializeField] private Button chatClearButton;
        [SerializeField] private Text chatStatusText;
        [SerializeField] private GameObject deepSeekChatWindow;

        private readonly List<Button> coloringButtons = new List<Button>();
        private readonly List<GameObject> chatBubbleObjects = new List<GameObject>();
        private readonly Dictionary<DigitalHumanModule, Button> moduleButtons = new Dictionary<DigitalHumanModule, Button>();
        private readonly Dictionary<DigitalHumanDifficulty, Button> difficultyButtons = new Dictionary<DigitalHumanDifficulty, Button>();
        private DigitalHumanThemeConfig theme;
        private DigitalHumanModule activeModule = DigitalHumanModule.None;
        private DigitalHumanParticipant activeColoringParticipant = DigitalHumanParticipant.Child;
        private Font uiFont;
        private bool sceneBindingsInstalled;

        public RawImage AvatarViewport
        {
            get
            {
                EnsureBuilt();
                return avatarViewport;
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            DigitalHumanEventBus.ModuleChanged += RenderModule;
            DigitalHumanEventBus.DigitalHumanResponded += RenderResponse;
            DigitalHumanEventBus.DialogueOptionsChanged += RenderOptions;
            DigitalHumanEventBus.SessionUpdated += RenderSession;
        }

        private void OnDisable()
        {
            DigitalHumanEventBus.ModuleChanged -= RenderModule;
            DigitalHumanEventBus.DigitalHumanResponded -= RenderResponse;
            DigitalHumanEventBus.DialogueOptionsChanged -= RenderOptions;
            DigitalHumanEventBus.SessionUpdated -= RenderSession;
        }

        public void Initialize(DigitalHumanThemeConfig themeConfig)
        {
            theme = themeConfig != null
                ? themeConfig
                : DigitalHumanThemeConfig.CreateRuntime(DigitalHumanModule.InterpersonalCommunication);

            EnsureBuilt();
            ApplyTheme();
        }

        public void RenderColoringAreas(IReadOnlyList<DigitalHumanColoringArea> areas, int currentIndex)
        {
            EnsureBuilt();
            ClearColoringButtons();
            if (areas == null)
            {
                Debug.LogWarning("[DigitalHumanUIView] RenderColoringAreas called with null areas");
                return;
            }

            if (currentIndex >= 0 && currentIndex < areas.Count)
            {
                activeColoringParticipant = areas[currentIndex].requiredParticipant;
            }

            if (HasSceneColoringButtons())
            {
                Debug.Log($"[DigitalHumanUIView] Using scene coloring buttons. Count: {coloringAreaButtons.Length}");
                for (int i = 0; i < coloringAreaButtons.Length; i++)
                {
                    Button button = coloringAreaButtons[i];
                    if (button == null)
                    {
                        Debug.LogWarning($"[DigitalHumanUIView] Scene coloring button[{i}] is NULL");
                        continue;
                    }

                    // 确保按钮的Image开启射线检测
                    if (button.image != null)
                    {
                        button.image.raycastTarget = true;
                    }

                    bool visible = i < areas.Count;
                    button.gameObject.SetActive(visible);
                    button.onClick.RemoveAllListeners();
                    if (!visible)
                    {
                        continue;
                    }

                    DigitalHumanColoringArea area = areas[i];
                    button.image.color = area.completed ? area.targetColor : new Color32(245, 245, 245, 255);
                    Text label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = area.completed ? $"{area.displayName} 完成" : area.displayName;
                    }

                    int capturedIndex = i;
                    button.onClick.AddListener(() =>
                    {
                        ColoringAreaSelected?.Invoke(areas[capturedIndex].areaId, activeColoringParticipant);
                    });
                }

                return;
            }

            Debug.Log($"[DigitalHumanUIView] Creating dynamic coloring buttons. Count: {areas.Count}");
            for (int i = 0; i < areas.Count; i++)
            {
                DigitalHumanColoringArea area = areas[i];
                Button button = CreateButton($"ColorArea_{area.areaId}", coloringRoot, area.displayName, 22);
                button.image.color = area.completed ? area.targetColor : new Color32(245, 245, 245, 255);
                button.GetComponentInChildren<Text>().text = area.completed
                    ? $"{area.displayName} 完成"
                    : area.displayName;

                // 确保动态创建的按钮开启射线检测
                if (button.image != null)
                {
                    button.image.raycastTarget = true;
                }

                int capturedIndex = i;
                button.onClick.AddListener(() =>
                {
                    ColoringAreaSelected?.Invoke(areas[capturedIndex].areaId, activeColoringParticipant);
                });

                RectTransform rect = button.GetComponent<RectTransform>();
                float width = 1f / Mathf.Max(1, areas.Count);
                Anchor(rect, width * i, 0.08f, width * (i + 1), 0.92f, 6f, 0f, -6f, 0f);
                coloringButtons.Add(button);
            }
        }

        public void RenderImitationState(
            IReadOnlyList<DigitalHumanImitationStep> steps,
            int currentIndex,
            bool paused)
        {
            EnsureBuilt();
            string stepText = "动作模仿";
            if (steps != null && currentIndex >= 0 && currentIndex < steps.Count)
            {
                stepText = $"第 {currentIndex + 1}/{steps.Count} 个动作";
            }

            statusText.text = paused ? $"{stepText}：已暂停" : stepText;
            pauseButton.GetComponentInChildren<Text>().text = paused ? "继续" : "暂停";
        }

        public void RenderChatMessages(IReadOnlyList<DigitalHumanChatMessage> messages)
        {
            EnsureBuilt();
            if (chatContent == null || chatScroll == null)
            {
                return;
            }

            ClearChatBubbles();
            if (messages == null)
            {
                return;
            }

            float y = 0f;
            const float bubbleWidth = 500f;
            const float spacing = 14f;
            for (int i = 0; i < messages.Count; i++)
            {
                DigitalHumanChatMessage message = messages[i];
                GameObject bubble = CreateChatBubble(message, bubbleWidth);
                RectTransform rect = bubble.GetComponent<RectTransform>();
                rect.SetParent(chatContent, false);
                rect.anchorMin = new Vector2(message.IsUser ? 1f : 0f, 1f);
                rect.anchorMax = new Vector2(message.IsUser ? 1f : 0f, 1f);
                rect.pivot = new Vector2(message.IsUser ? 1f : 0f, 1f);
                rect.anchoredPosition = new Vector2(message.IsUser ? -12f : 12f, -y);

                Text text = bubble.GetComponentInChildren<Text>();
                float height = Mathf.Clamp(text.preferredHeight + 28f, 48f, 180f);
                rect.sizeDelta = new Vector2(bubbleWidth, height);
                y += height + spacing;
                chatBubbleObjects.Add(bubble);
            }

            chatContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(520f, y + 20f));
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }

        public void RenderChatStatus(string status, bool connected)
        {
            EnsureBuilt();
            if (chatStatusText == null)
            {
                return;
            }

            chatStatusText.text = status;
            chatStatusText.color = connected
                ? new Color32(116, 225, 159, 255)
                : new Color32(255, 196, 116, 255);
        }

        private void RenderModule(DigitalHumanModule module)
        {
            EnsureBuilt();
            activeModule = module;
            SetChatMode(module == DigitalHumanModule.DeepSeekChat);
            SetExerciseControls(module);
            ApplyNavigationState();
            titleText.text = module switch
            {
                DigitalHumanModule.DeepSeekChat => "Deep Seek API Chat",
                DigitalHumanModule.ParentChildColoring => "亲子互动 - 合作涂色",
                DigitalHumanModule.ActionImitation => "亲子互动 - 动作模仿",
                _ => "人际交流 - 买菜场景"
            };

            avatarHintText.text = module switch
            {
                DigitalHumanModule.DeepSeekChat => "右侧数字人会跟随聊天回应",
                DigitalHumanModule.ParentChildColoring => "点击数字人，会给孩子温柔提示",
                DigitalHumanModule.ActionImitation => "右侧数字人会示范动作",
                _ => "点击数字人可重复鼓励"
            };
        }

        private void RenderResponse(DigitalHumanResponse response)
        {
            EnsureBuilt();
            if (response.Module == DigitalHumanModule.DeepSeekChat)
            {
                SetChatMode(true);
                return;
            }

            dialogueText.text = response.Line;
            statusText.text = response.Module switch
            {
                DigitalHumanModule.ParentChildColoring => "轮流点击对应颜色区域",
                DigitalHumanModule.ActionImitation => "看右侧数字人动作，准备好后点击完成",
                _ => "选择一个大按钮，或输入/语音说出想法"
            };
        }

        private void RenderOptions(IReadOnlyList<DigitalHumanDialogueOption> options)
        {
            EnsureBuilt();
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                button.gameObject.SetActive(options != null && i < options.Count);
                button.onClick.RemoveAllListeners();

                if (options == null || i >= options.Count)
                {
                    continue;
                }

                DigitalHumanDialogueOption option = options[i];
                button.GetComponentInChildren<Text>().text = option.label;
                button.onClick.AddListener(() => OptionSelected?.Invoke(option.id));
            }
        }

        private void RenderSession(DigitalHumanSessionRecord record)
        {
            if (record == null || statusText == null)
            {
                return;
            }

            statusText.text =
                $"时长 {record.durationSeconds:0}s    正确互动 {record.correctInteractionCount}/{record.totalInteractionCount}";
        }

        private void EnsureBuilt()
        {
            uiFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 800;
            }

            if (background != null)
            {
                InstallSceneBindings();
                return;
            }

            if (allowRuntimeBuild)
            {
                Debug.LogWarning("Runtime UI creation is disabled. DigitalHumanUIView now only uses scene-assigned UGUI references.");
            }

            Debug.LogError("DigitalHumanUIView has no assigned UGUI references. Assign scene UI objects in SampleScene instead of runtime-building UI.");
        }

        private void BuildLeftPanel(RectTransform leftPanel)
        {
            RectTransform header = CreatePanel("Header", leftPanel, new Color32(255, 255, 255, 0));
            Anchor(header, 0.04f, 0.75f, 0.96f, 0.98f, 0f, 0f, 0f, 0f);

            titleText = CreateText("Title", header, "人际交流 - 买菜场景", 32, TextAnchor.MiddleLeft);
            Anchor(titleText.rectTransform, 0f, 0.56f, 0.68f, 1f, 0f, 0f, 0f, 0f);

            CreateDifficultyButton(header, "初级难度", 0.70f, 0.84f, DigitalHumanDifficulty.Beginner);
            CreateDifficultyButton(header, "进阶难度", 0.86f, 1.00f, DigitalHumanDifficulty.Intermediate);
            CreateModuleButton(header, "买菜对话", 0.00f, 0.235f, DigitalHumanModule.InterpersonalCommunication);
            CreateModuleButton(header, "DeepSeek聊天", 0.255f, 0.49f, DigitalHumanModule.DeepSeekChat);
            CreateModuleButton(header, "合作涂色", 0.51f, 0.745f, DigitalHumanModule.ParentChildColoring);
            CreateModuleButton(header, "动作模仿", 0.765f, 1.00f, DigitalHumanModule.ActionImitation);

            exercisePanel = CreatePanel("ExercisePanel", leftPanel, new Color32(255, 255, 255, 0));
            Anchor(exercisePanel, 0f, 0f, 1f, 0.73f, 0f, 0f, 0f, 0f);
            chatPanel = CreatePanel("DeepSeekChatPanel", leftPanel, new Color32(41, 42, 48, 255));
            Anchor(chatPanel, 0.02f, 0.03f, 0.98f, 0.73f, 0f, 0f, 0f, 0f);

            RectTransform dialoguePanel = CreatePanel("DialogueBox", exercisePanel, new Color32(255, 251, 244, 255));
            Anchor(dialoguePanel, 0.04f, 0.72f, 0.96f, 0.98f, 0f, 0f, 0f, 0f);
            dialogueText = CreateText("DialogueText", dialoguePanel, "小朋友，想买点什么呀？", 34, TextAnchor.MiddleCenter);
            Stretch(dialogueText.rectTransform, 26f, 16f, -26f, -16f);

            optionsPanel = CreatePanel("OptionButtons", exercisePanel, new Color32(255, 255, 255, 0));
            Anchor(optionsPanel, 0.04f, 0.51f, 0.96f, 0.70f, 0f, 0f, 0f, 0f);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i] = CreateButton($"Option_{i + 1}", optionsPanel, $"选项 {i + 1}", 28);
                float width = 1f / optionButtons.Length;
                Anchor(optionButtons[i].GetComponent<RectTransform>(), i * width, 0.04f, (i + 1) * width, 0.96f, 8f, 0f, -8f, 0f);
            }

            inputPanel = CreatePanel("InputBox", exercisePanel, new Color32(255, 255, 255, 245));
            Anchor(inputPanel, 0.04f, 0.38f, 0.96f, 0.49f, 0f, 0f, 0f, 0f);
            inputField = CreateInputField(inputPanel);
            Anchor(inputField.GetComponent<RectTransform>(), 0.02f, 0.14f, 0.66f, 0.86f, 0f, 0f, 0f, 0f);
            submitButton = CreateButton("SubmitText", inputPanel, "发送", 24);
            Anchor(submitButton.GetComponent<RectTransform>(), 0.69f, 0.14f, 0.83f, 0.86f, 0f, 0f, 0f, 0f);
            voiceButton = CreateButton("SubmitVoice", inputPanel, "语音", 24);
            Anchor(voiceButton.GetComponent<RectTransform>(), 0.85f, 0.14f, 0.98f, 0.86f, 0f, 0f, 0f, 0f);

            submitButton.onClick.AddListener(() =>
            {
                TextSubmitted?.Invoke(inputField.text);
                inputField.text = string.Empty;
            });
            voiceButton.onClick.AddListener(() => VoiceTextSubmitted?.Invoke(inputField.text));

            RectTransform taskPanel = CreatePanel("TaskArea", exercisePanel, new Color32(245, 248, 252, 255));
            Anchor(taskPanel, 0.04f, 0.04f, 0.96f, 0.35f, 0f, 0f, 0f, 0f);
            statusText = CreateText("Status", taskPanel, "选择一个大按钮，或输入/语音说出想法", 22, TextAnchor.MiddleLeft);
            Anchor(statusText.rectTransform, 0.03f, 0.62f, 0.98f, 0.95f, 0f, 0f, 0f, 0f);

            coloringRoot = CreatePanel("ColoringRoot", taskPanel, new Color32(255, 255, 255, 0));
            Anchor(coloringRoot, 0.03f, 0.08f, 0.62f, 0.56f, 0f, 0f, 0f, 0f);

            pauseButton = CreateButton("PauseImitation", taskPanel, "暂停", 22);
            Anchor(pauseButton.GetComponent<RectTransform>(), 0.66f, 0.12f, 0.80f, 0.50f, 0f, 0f, 0f, 0f);
            confirmImitationButton = CreateButton("ConfirmImitation", taskPanel, "完成动作", 22);
            Anchor(confirmImitationButton.GetComponent<RectTransform>(), 0.82f, 0.12f, 0.98f, 0.50f, 0f, 0f, 0f, 0f);
            pauseButton.onClick.AddListener(() => ImitationPauseRequested?.Invoke());
            confirmImitationButton.onClick.AddListener(() => ImitationConfirmRequested?.Invoke());

            BuildChatPanel(chatPanel);
            SetChatMode(false);
        }

        private void BuildChatPanel(RectTransform parent)
        {
            Text chatTitle = CreateText("ChatTitle", parent, "Deep Seek API Chat", 18, TextAnchor.MiddleCenter);
            chatTitle.color = Color.white;
            Anchor(chatTitle.rectTransform, 0.04f, 0.92f, 0.96f, 0.99f, 0f, 0f, 0f, 0f);

            RectTransform scrollRoot = CreatePanel("ChatScrollRoot", parent, new Color32(65, 66, 75, 255));
            Anchor(scrollRoot, 0.04f, 0.19f, 0.96f, 0.90f, 0f, 0f, 0f, 0f);
            chatScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            chatScroll.horizontal = false;
            chatScroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewportRect = CreatePanel("ChatViewport", scrollRoot, new Color32(65, 66, 75, 0));
            Stretch(viewportRect, 8f, 8f, -8f, -8f);
            Mask mask = viewportRect.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            chatScroll.viewport = viewportRect;

            chatContent = CreatePanel("ChatContent", viewportRect, new Color32(65, 66, 75, 0));
            chatContent.anchorMin = new Vector2(0f, 1f);
            chatContent.anchorMax = new Vector2(1f, 1f);
            chatContent.pivot = new Vector2(0.5f, 1f);
            chatContent.anchoredPosition = Vector2.zero;
            chatContent.sizeDelta = new Vector2(0f, 520f);
            chatScroll.content = chatContent;

            RectTransform inputArea = CreatePanel("ChatInputArea", parent, new Color32(65, 66, 75, 255));
            Anchor(inputArea, 0.04f, 0.04f, 0.96f, 0.16f, 0f, 0f, 0f, 0f);
            chatInputField = CreateInputField(inputArea);
            Anchor(chatInputField.GetComponent<RectTransform>(), 0.03f, 0.14f, 0.74f, 0.86f, 0f, 0f, 0f, 0f);
            chatInputField.placeholder.GetComponent<Text>().text = "Message Deepseek";

            chatSendButton = CreateButton("ChatSendButton", inputArea, "↑", 28);
            Anchor(chatSendButton.GetComponent<RectTransform>(), 0.78f, 0.14f, 0.88f, 0.86f, 0f, 0f, 0f, 0f);
            chatSendButton.image.color = new Color32(77, 103, 255, 255);
            chatSendButton.onClick.AddListener(() =>
            {
                ChatSubmitted?.Invoke(chatInputField.text);
                chatInputField.text = string.Empty;
            });

            chatClearButton = CreateButton("ChatClearButton", inputArea, "清空", 18);
            Anchor(chatClearButton.GetComponent<RectTransform>(), 0.90f, 0.14f, 0.98f, 0.86f, 0f, 0f, 0f, 0f);
            chatClearButton.image.color = new Color32(85, 87, 98, 255);
            chatClearButton.onClick.AddListener(() => ChatClearRequested?.Invoke());

            chatStatusText = CreateText("ChatStatus", parent, "DeepSeek API 状态检查中", 16, TextAnchor.MiddleLeft);
            Anchor(chatStatusText.rectTransform, 0.04f, 0.165f, 0.96f, 0.19f, 0f, 0f, 0f, 0f);
        }

        private void BuildRightPanel(RectTransform rightPanel)
        {
            Text avatarTitle = CreateText("AvatarTitle", rightPanel, "3D 数字人", 30, TextAnchor.MiddleCenter);
            Anchor(avatarTitle.rectTransform, 0.08f, 0.88f, 0.92f, 0.98f, 0f, 0f, 0f, 0f);

            RectTransform viewportFrame = CreatePanel("AvatarViewportFrame", rightPanel, new Color32(230, 240, 252, 255));
            Anchor(viewportFrame, 0.08f, 0.18f, 0.92f, 0.86f, 0f, 0f, 0f, 0f);

            GameObject rawObject = new GameObject("AvatarViewport");
            rawObject.transform.SetParent(viewportFrame, false);
            avatarViewport = rawObject.AddComponent<RawImage>();
            avatarViewport.color = Color.white;
            avatarViewport.raycastTarget = true;
            Stretch(avatarViewport.rectTransform, 8f, 8f, -8f, -8f);

            avatarHitButton = rawObject.AddComponent<Button>();
            avatarHitButton.targetGraphic = avatarViewport;
            avatarHitButton.transition = Selectable.Transition.None;
            avatarHitButton.onClick.AddListener(() => AvatarClicked?.Invoke());

            avatarHintText = CreateText("AvatarHint", rightPanel, "点击数字人可重复鼓励", 22, TextAnchor.MiddleCenter);
            Anchor(avatarHintText.rectTransform, 0.08f, 0.06f, 0.92f, 0.16f, 0f, 0f, 0f, 0f);
        }

        private void ApplyTheme()
        {
            background.color = theme.backgroundColor;
            titleText.color = theme.textColor;
            dialogueText.color = theme.textColor;
            statusText.color = theme.textColor;
            avatarHintText.color = theme.textColor;

            foreach (Button button in optionButtons)
            {
                button.image.color = theme.primaryColor;
                Text label = button.GetComponentInChildren<Text>();
                label.color = Color.white;
                label.fontSize = theme.optionFontSize;
            }

            submitButton.image.color = theme.primaryColor;
            voiceButton.image.color = theme.secondaryColor;
            pauseButton.image.color = theme.secondaryColor;
            confirmImitationButton.image.color = theme.successColor;
            ApplyNavigationState();
        }

        private void SetChatMode(bool enabled)
        {
            if (exercisePanel != null)
            {
                exercisePanel.gameObject.SetActive(!enabled);
            }

            if (chatPanel != null)
            {
                chatPanel.gameObject.SetActive(enabled);
            }

            if (deepSeekChatWindow != null)
            {
                deepSeekChatWindow.SetActive(enabled);
            }
        }

        private GameObject CreateChatBubble(DigitalHumanChatMessage message, float width)
        {
            GameObject bubble = new GameObject(message.IsUser ? "UserMessage" : "AssistantMessage");
            RectTransform rect = bubble.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 80f);
            Image image = bubble.AddComponent<Image>();
            image.color = message.IsUser
                ? new Color32(77, 103, 255, 255)
                : new Color32(82, 84, 96, 255);

            Text text = CreateText("MessageText", rect, message.content, 20, TextAnchor.MiddleLeft);
            text.color = Color.white;
            Stretch(text.rectTransform, 18f, 10f, -18f, -10f);
            return bubble;
        }

        private void ClearChatBubbles()
        {
            for (int i = 0; i < chatBubbleObjects.Count; i++)
            {
                if (chatBubbleObjects[i] != null)
                {
                    Destroy(chatBubbleObjects[i]);
                }
            }

            chatBubbleObjects.Clear();
        }

        private void ClearColoringButtons()
        {
            if (HasSceneColoringButtons())
            {
                for (int i = 0; i < coloringAreaButtons.Length; i++)
                {
                    if (coloringAreaButtons[i] != null)
                    {
                        coloringAreaButtons[i].gameObject.SetActive(false);
                        coloringAreaButtons[i].onClick.RemoveAllListeners();
                    }
                }

                return;
            }

            for (int i = 0; i < coloringButtons.Count; i++)
            {
                if (coloringButtons[i] != null)
                {
                    Destroy(coloringButtons[i].gameObject);
                }
            }

            coloringButtons.Clear();
        }

        private bool HasSceneColoringButtons()
        {
            return coloringAreaButtons != null && coloringAreaButtons.Length > 0 && coloringAreaButtons[0] != null;
        }

        private void InstallSceneBindings()
        {
            if (sceneBindingsInstalled)
            {
                return;
            }

            sceneBindingsInstalled = true;
            RegisterModuleButton(moduleInterpersonalButton, DigitalHumanModule.InterpersonalCommunication);
            RegisterModuleButton(moduleChatButton, DigitalHumanModule.DeepSeekChat);
            RegisterModuleButton(moduleColoringButton, DigitalHumanModule.ParentChildColoring);
            RegisterModuleButton(moduleImitationButton, DigitalHumanModule.ActionImitation);
            RegisterDifficultyButton(beginnerDifficultyButton, DigitalHumanDifficulty.Beginner);
            RegisterDifficultyButton(intermediateDifficultyButton, DigitalHumanDifficulty.Intermediate);

            if (submitButton != null && inputField != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(() =>
                {
                    TextSubmitted?.Invoke(inputField.text);
                    inputField.text = string.Empty;
                });
            }

            if (voiceButton != null && inputField != null)
            {
                voiceButton.onClick.RemoveAllListeners();
                voiceButton.onClick.AddListener(() => VoiceTextSubmitted?.Invoke(inputField.text));
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() => ImitationPauseRequested?.Invoke());
            }

            if (confirmImitationButton != null)
            {
                confirmImitationButton.onClick.RemoveAllListeners();
                confirmImitationButton.onClick.AddListener(() => ImitationConfirmRequested?.Invoke());
            }

            if (avatarHitButton != null)
            {
                avatarHitButton.onClick.RemoveAllListeners();
                avatarHitButton.onClick.AddListener(() => AvatarClicked?.Invoke());
            }

            if (chatSendButton != null && chatInputField != null)
            {
                chatSendButton.onClick.RemoveAllListeners();
                chatSendButton.onClick.AddListener(() =>
                {
                    ChatSubmitted?.Invoke(chatInputField.text);
                    chatInputField.text = string.Empty;
                });
            }

            if (chatClearButton != null)
            {
                chatClearButton.onClick.RemoveAllListeners();
                chatClearButton.onClick.AddListener(() => ChatClearRequested?.Invoke());
            }
        }

        private void RegisterModuleButton(Button button, DigitalHumanModule module)
        {
            if (button == null)
            {
                Debug.LogError($"[DigitalHumanUIView] Module button for {module} is NULL! Did you rebuild the scene? Please go to Tools > Digital Human > Rebuild SampleScene UGUI");
                return;
            }

            Debug.Log($"[DigitalHumanUIView] Registering module button for {module}: {button.name}");
            moduleButtons[module] = button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ModuleRequested?.Invoke(module));
        }

        private void RegisterDifficultyButton(Button button, DigitalHumanDifficulty difficulty)
        {
            if (button == null)
            {
                Debug.LogError($"[DigitalHumanUIView] Difficulty button for {difficulty} is NULL! Did you rebuild the scene?");
                return;
            }

            Debug.Log($"[DigitalHumanUIView] Registering difficulty button for {difficulty}: {button.name}");
            difficultyButtons[difficulty] = button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => DifficultyRequested?.Invoke(difficulty));
        }

        private void CreateModuleButton(RectTransform parent, string label, float minX, float maxX, DigitalHumanModule module)
        {
            Button button = CreateButton($"Module_{module}", parent, label, 22);
            Anchor(button.GetComponent<RectTransform>(), minX, 0.03f, maxX, 0.47f, 0f, 0f, 0f, 0f);
            moduleButtons[module] = button;
            button.onClick.AddListener(() => ModuleRequested?.Invoke(module));
        }

        private void CreateDifficultyButton(RectTransform parent, string label, float minX, float maxX, DigitalHumanDifficulty difficulty)
        {
            Button button = CreateButton($"Difficulty_{difficulty}", parent, label, 18);
            Anchor(button.GetComponent<RectTransform>(), minX, 0.62f, maxX, 0.96f, 0f, 0f, 0f, 0f);
            difficultyButtons[difficulty] = button;
            button.onClick.AddListener(() => DifficultyRequested?.Invoke(difficulty));
        }

        private Button CreateButton(string name, Transform parent, string label, int fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, new Color32(248, 177, 91, 255));
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", rect, label, fontSize, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = fontSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(text.rectTransform, 8f, 4f, -8f, -4f);
            return button;
        }

        private void ApplyNavigationState()
        {
            Color32 inactive = new Color32(236, 242, 249, 255);
            Color32 active = theme != null ? theme.primaryColor : new Color32(248, 177, 91, 255);
            Color32 textDark = new Color32(47, 52, 64, 255);

            foreach (KeyValuePair<DigitalHumanModule, Button> entry in moduleButtons)
            {
                bool selected = entry.Key == activeModule;
                entry.Value.image.color = selected ? active : inactive;
                Text label = entry.Value.GetComponentInChildren<Text>();
                label.color = selected ? Color.white : textDark;
            }

            foreach (KeyValuePair<DigitalHumanDifficulty, Button> entry in difficultyButtons)
            {
                entry.Value.image.color = new Color32(247, 249, 252, 255);
                Text label = entry.Value.GetComponentInChildren<Text>();
                label.color = textDark;
            }
        }

        private InputField CreateInputField(Transform parent)
        {
            RectTransform rect = CreatePanel("TextInput", parent, new Color32(246, 248, 252, 255));
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            InputField field = rect.gameObject.AddComponent<InputField>();
            field.targetGraphic = image;
            Text text = CreateText("Text", rect, string.Empty, 24, TextAnchor.MiddleLeft);
            Text placeholder = CreateText("Placeholder", rect, "可自由输入，不限提示词；也可放入语音识别结果", 20, TextAnchor.MiddleLeft);
            placeholder.color = new Color32(130, 136, 150, 255);
            Stretch(text.rectTransform, 14f, 4f, -14f, -4f);
            Stretch(placeholder.rectTransform, 14f, 4f, -14f, -4f);
            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }

        private void SetExerciseControls(DigitalHumanModule module)
        {
            bool dialogue = module == DigitalHumanModule.InterpersonalCommunication;
            bool coloring = module == DigitalHumanModule.ParentChildColoring;
            bool imitation = module == DigitalHumanModule.ActionImitation;

            if (optionsPanel != null)
            {
                optionsPanel.gameObject.SetActive(dialogue);
            }

            if (inputPanel != null)
            {
                inputPanel.gameObject.SetActive(dialogue);
            }

            if (coloringRoot != null)
            {
                coloringRoot.gameObject.SetActive(coloring);
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(imitation);
            }

            if (confirmImitationButton != null)
            {
                confirmImitationButton.gameObject.SetActive(imitation);
            }
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.font = uiFont;
            label.text = text;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = new Color32(47, 52, 64, 255);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
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

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
