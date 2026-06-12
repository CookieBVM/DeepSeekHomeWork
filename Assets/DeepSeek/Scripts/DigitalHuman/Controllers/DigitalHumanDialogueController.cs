using System.Threading.Tasks;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanDialogueController : MonoBehaviour
    {
        private readonly DigitalHumanDialogueStateMachine stateMachine = new DigitalHumanDialogueStateMachine();

        private DigitalHumanScenarioDefinition scenario;
        private DigitalHumanDataTracker dataTracker;
        private DigitalHumanAIApiService aiApiService;
        private DigitalHumanDifficulty difficulty;

        public string ScenarioId => scenario?.scenarioId;

        public void Initialize(
            DigitalHumanDataTracker tracker,
            DigitalHumanAIApiService apiService,
            DigitalHumanDifficulty selectedDifficulty)
        {
            dataTracker = tracker;
            aiApiService = apiService;
            difficulty = selectedDifficulty;
        }

        public void BeginVegetableMarket()
        {
            scenario = DigitalHumanScenarioLibrary.CreateVegetableMarketScenario();
            dataTracker?.StartSession(
                DigitalHumanModule.InterpersonalCommunication,
                scenario.scenarioId,
                difficulty);

            DigitalHumanEventBus.PublishModuleChanged(DigitalHumanModule.InterpersonalCommunication);
            DigitalHumanEventBus.PublishResponse(stateMachine.Start(scenario));
        }

        public void SelectOption(string optionIdOrLabel)
        {
            DigitalHumanResponse response = stateMachine.SelectOption(optionIdOrLabel);
            RecordAndPublish(optionIdOrLabel, DigitalHumanInputMode.Option, response);
        }

        public async void SubmitText(string text)
        {
            await SubmitFreeInput(text, DigitalHumanInputMode.Text);
        }

        public async void SubmitVoiceText(string recognizedText)
        {
            await SubmitFreeInput(recognizedText, DigitalHumanInputMode.Voice);
        }

        private async Task SubmitFreeInput(string input, DigitalHumanInputMode inputMode)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            DigitalHumanResponse response = stateMachine.SubmitText(input, inputMode);
            if (!response.IsCorrect && aiApiService != null)
            {
                string aiLine = await aiApiService.GenerateInterpersonalReplyAsync(
                    scenario?.roleName ?? "数字人",
                    input,
                    stateMachine.CurrentOptions);

                if (!string.IsNullOrWhiteSpace(aiLine))
                {
                    response = DigitalHumanResponse.Say(
                        DigitalHumanModule.InterpersonalCommunication,
                        aiLine,
                        DigitalHumanAvatarPose.Speaking,
                        DigitalHumanEmotion.Encouraging,
                        stateMachine.CurrentOptions,
                        isCorrect: false,
                        triggerReward: false);
                }
            }

            RecordAndPublish(input, inputMode, response);
        }

        private void RecordAndPublish(string input, DigitalHumanInputMode inputMode, DigitalHumanResponse response)
        {
            dataTracker?.RecordInteraction(
                DigitalHumanModule.InterpersonalCommunication,
                ScenarioId,
                DigitalHumanParticipant.Child,
                inputMode,
                input,
                response);

            if (response.TriggerReward)
            {
                DigitalHumanEventBus.PublishCustomAnimation("Standing_Clap");
                DigitalHumanEventBus.PublishReward("做得真好！", 1.6f);
            }

            if (response.TaskCompleted)
            {
                DigitalHumanEventBus.PublishCustomAnimation("Cheering");
                dataTracker?.MarkTaskCompleted("涔拌彍瀵硅瘽娴佺▼瀹屾垚");
                dataTracker?.EndSession();
                DigitalHumanEventBus.PublishTaskCompleted(DigitalHumanModule.InterpersonalCommunication, ScenarioId);
            }

            DigitalHumanEventBus.PublishResponse(response);
        }
    }
}





