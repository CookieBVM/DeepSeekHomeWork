using System.Collections;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanGameController : MonoBehaviour
    {
        [Header("Startup")]
        [SerializeField] private DigitalHumanModule startModule = DigitalHumanModule.InterpersonalCommunication;
        [SerializeField] private DigitalHumanDifficulty difficulty = DigitalHumanDifficulty.Beginner;
        [SerializeField] private bool allowRuntimeComponentCreation;

        [Header("Optional Services")]
        [SerializeField] private bool enableRemoteAi = true;
        [SerializeField] private string deepSeekApiKey;
        [SerializeField] private string pythonSyncEndpoint = "http://127.0.0.1:8000/api/digital-human/session";

        [Header("MVC References")]
        [SerializeField] private DigitalHumanUIView view;
        [SerializeField] private DigitalHumanAvatarView avatarView;
        [SerializeField] private DigitalHumanFeedbackView feedbackView;
        [SerializeField] private DigitalHumanSceneTransition sceneTransition;
        [SerializeField] private DigitalHumanInputHandler inputHandler;
        [SerializeField] private DigitalHumanDialogueController dialogueController;
        [SerializeField] private DigitalHumanChatController chatController;
        [SerializeField] private DigitalHumanColoringController coloringController;
        [SerializeField] private DigitalHumanImitationController imitationController;
        [SerializeField] private DigitalHumanDataTracker dataTracker;
        [SerializeField] private DigitalHumanDataSyncService dataSyncService;
        [SerializeField] private DigitalHumanAIApiService aiApiService;
        [SerializeField] private DigitalHumanSpeechService speechService;

        private DigitalHumanModule currentModule = DigitalHumanModule.None;
        private Coroutine switchRoutine;
        private bool initialized;

        private void Awake()
        {
            initialized = EnsureDependencies();
            if (!initialized)
            {
                enabled = false;
                return;
            }

            WireEvents();
        }

        private void Start()
        {
            if (!initialized)
            {
                return;
            }

            SwitchModule(startModule);
            dataSyncService.TrySyncCachedSessions();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                dataSyncService.TrySyncCachedSessions();
            }
        }

        public void SwitchModule(DigitalHumanModule module)
        {
            DigitalHumanEventBus.PublishModuleChanged(module);

            if (switchRoutine != null)
            {
                StopCoroutine(switchRoutine);
            }

            switchRoutine = StartCoroutine(SwitchModuleRoutine(module));
        }

        public void SetDifficulty(DigitalHumanDifficulty selectedDifficulty)
        {
            difficulty = selectedDifficulty;
            if (currentModule != DigitalHumanModule.None)
            {
                SwitchModule(currentModule);
            }
        }

        private IEnumerator SwitchModuleRoutine(DigitalHumanModule module)
        {
            DigitalHumanThemeConfig theme = DigitalHumanThemeConfig.CreateRuntime(module);
            sceneTransition.Configure(theme.sceneFadeSeconds);
            bool switchingFromExistingModule = currentModule != DigitalHumanModule.None;

            if (switchingFromExistingModule)
            {
                yield return sceneTransition.FadeOut();

                if (dataTracker.CurrentSession != null && string.IsNullOrWhiteSpace(dataTracker.CurrentSession.endedAtUtc))
                {
                    dataTracker.EndSession();
                }
            }

            view.Initialize(theme);
            avatarView.BindViewport(view.AvatarViewport);
            DigitalHumanEventBus.PublishCustomAnimation("Waving");
            currentModule = module;
            aiApiService.Configure(deepSeekApiKey, enableRemoteAi);
            chatController.Initialize(aiApiService);
            dialogueController.Initialize(dataTracker, aiApiService, difficulty);
            coloringController.Initialize(dataTracker, difficulty);
            imitationController.Initialize(dataTracker, difficulty);

            switch (module)
            {
                case DigitalHumanModule.DeepSeekChat:
                    chatController.Begin();
                    break;
                case DigitalHumanModule.ParentChildColoring:
                    coloringController.Begin();
                    break;
                case DigitalHumanModule.ActionImitation:
                    imitationController.Begin();
                    break;
                default:
                    dialogueController.BeginVegetableMarket();
                    break;
            }

            if (switchingFromExistingModule)
            {
                yield return sceneTransition.FadeIn();
            }

            switchRoutine = null;
        }

        private bool EnsureDependencies()
        {
            inputHandler = ResolveComponent(inputHandler, "InputHandler");
            dataTracker = ResolveComponent(dataTracker, "DataTracker");
            dataSyncService = ResolveComponent(dataSyncService, "DataSyncService");
            aiApiService = ResolveComponent(aiApiService, "AIApiService");
            speechService = ResolveComponent(speechService, "SpeechService");
            dialogueController = ResolveComponent(dialogueController, "DialogueController");
            chatController = ResolveComponent(chatController, "ChatController");
            coloringController = ResolveComponent(coloringController, "ColoringController");
            imitationController = ResolveComponent(imitationController, "ImitationController");
            view = ResolveComponent(view, "UIView");
            avatarView = ResolveComponent(avatarView, "AvatarView");
            feedbackView = ResolveComponent(feedbackView, "FeedbackView");
            sceneTransition = ResolveComponent(sceneTransition, "SceneTransition");

            bool hasAllDependencies =
                inputHandler != null &&
                dataTracker != null &&
                dataSyncService != null &&
                aiApiService != null &&
                speechService != null &&
                dialogueController != null &&
                chatController != null &&
                coloringController != null &&
                imitationController != null &&
                view != null &&
                avatarView != null &&
                feedbackView != null &&
                sceneTransition != null;

            if (!hasAllDependencies)
            {
                Debug.LogError("DigitalHumanGameController requires scene-assigned UGUI and module components. Run the SampleScene builder or assign references manually.");
                return false;
            }

            dataSyncService.Initialize(dataTracker, pythonSyncEndpoint);
            return true;
        }

        private T ResolveComponent<T>(T existing, string childName) where T : Component
        {
            if (existing != null)
            {
                return existing;
            }

            T found = GetComponentInChildren<T>(true);
            if (found != null)
            {
                return found;
            }

            if (!allowRuntimeComponentCreation)
            {
                return null;
            }

            GameObject child = new GameObject($"DigitalHuman_{childName}");
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        private void OnAvatarClicked()
        {
            DigitalHumanEventBus.PublishCustomAnimation("Waving");
        }

        private void WireEvents()
        {
            view.ModuleRequested += SwitchModule;
            view.DifficultyRequested += inputHandler.RequestDifficulty;
            view.OptionSelected += inputHandler.SubmitOption;
            view.TextSubmitted += inputHandler.SubmitText;
            view.VoiceTextSubmitted += inputHandler.SubmitVoiceRecognizedText;
            view.ColoringAreaSelected += inputHandler.SubmitColoringArea;
            view.ImitationPauseRequested += inputHandler.ToggleImitationPause;
            view.ImitationConfirmRequested += inputHandler.ConfirmImitation;
            view.AvatarClicked += OnAvatarClicked;

            inputHandler.ModuleRequested += SwitchModule;
            inputHandler.DifficultyRequested += SetDifficulty;
            inputHandler.OptionSubmitted += dialogueController.SelectOption;
            inputHandler.TextSubmitted += dialogueController.SubmitText;
            inputHandler.VoiceTextSubmitted += dialogueController.SubmitVoiceText;
            inputHandler.ColoringAreaSubmitted += coloringController.SelectArea;
            inputHandler.ImitationPauseRequested += imitationController.TogglePause;
            inputHandler.ImitationConfirmRequested += HandleImitationConfirmRequested;

            coloringController.ColoringStateChanged += view.RenderColoringAreas;
            imitationController.ImitationStateChanged += view.RenderImitationState;
            imitationController.AnimationSpeedChanged += avatarView.SetAnimationSpeed;
            view.ChatSubmitted += chatController.Submit;
            view.ChatClearRequested += chatController.Clear;
            chatController.ChatMessagesChanged += view.RenderChatMessages;
            chatController.ChatStatusChanged += view.RenderChatStatus;
        }

        private void UnwireEvents()
        {
            if (view != null && inputHandler != null)
            {
                view.ModuleRequested -= SwitchModule;
                view.DifficultyRequested -= inputHandler.RequestDifficulty;
                view.OptionSelected -= inputHandler.SubmitOption;
                view.TextSubmitted -= inputHandler.SubmitText;
                view.VoiceTextSubmitted -= inputHandler.SubmitVoiceRecognizedText;
                view.ColoringAreaSelected -= inputHandler.SubmitColoringArea;
                view.ImitationPauseRequested -= inputHandler.ToggleImitationPause;
                view.ImitationConfirmRequested -= inputHandler.ConfirmImitation;
                view.AvatarClicked -= avatarView.PlayInteractiveGreeting;
            }

            if (inputHandler != null)
            {
                inputHandler.ModuleRequested -= SwitchModule;
                inputHandler.DifficultyRequested -= SetDifficulty;
                inputHandler.OptionSubmitted -= dialogueController.SelectOption;
                inputHandler.TextSubmitted -= dialogueController.SubmitText;
                inputHandler.VoiceTextSubmitted -= dialogueController.SubmitVoiceText;
                inputHandler.ColoringAreaSubmitted -= coloringController.SelectArea;
                inputHandler.ImitationPauseRequested -= imitationController.TogglePause;
                inputHandler.ImitationConfirmRequested -= HandleImitationConfirmRequested;
            }

            if (view != null && chatController != null)
            {
                view.ChatSubmitted -= chatController.Submit;
                view.ChatClearRequested -= chatController.Clear;
                chatController.ChatMessagesChanged -= view.RenderChatMessages;
                chatController.ChatStatusChanged -= view.RenderChatStatus;
            }

            if (coloringController != null && view != null)
            {
                coloringController.ColoringStateChanged -= view.RenderColoringAreas;
            }

            if (imitationController != null && view != null && avatarView != null)
            {
                imitationController.ImitationStateChanged -= view.RenderImitationState;
                imitationController.AnimationSpeedChanged -= avatarView.SetAnimationSpeed;
            }
        }

        private void HandleImitationConfirmRequested()
        {
            imitationController.ConfirmImitated(DigitalHumanParticipant.Child);
        }
    }
}

