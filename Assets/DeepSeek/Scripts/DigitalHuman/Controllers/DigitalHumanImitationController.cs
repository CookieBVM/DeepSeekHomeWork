using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanImitationController : MonoBehaviour
    {
        public event Action<IReadOnlyList<DigitalHumanImitationStep>, int, bool> ImitationStateChanged;
        public event Action<float> AnimationSpeedChanged;

        [SerializeField] private float beginnerSpeed = 0.5f;
        [SerializeField] private float normalSpeed = 1f;

        private readonly List<DigitalHumanImitationStep> steps = new List<DigitalHumanImitationStep>();

        private DigitalHumanDataTracker dataTracker;
        private DigitalHumanDifficulty difficulty;
        private int currentStepIndex = -1;
        private bool paused;

        public IReadOnlyList<DigitalHumanImitationStep> Steps => steps;
        public DigitalHumanImitationStep CurrentStep =>
            currentStepIndex >= 0 && currentStepIndex < steps.Count ? steps[currentStepIndex] : null;

        public void Initialize(DigitalHumanDataTracker tracker, DigitalHumanDifficulty selectedDifficulty)
        {
            dataTracker = tracker;
            difficulty = selectedDifficulty;
        }

        public void Begin()
        {
            steps.Clear();
            steps.AddRange(DigitalHumanScenarioLibrary.CreateImitationSteps(difficulty));
            currentStepIndex = -1;
            paused = false;

            dataTracker?.StartSession(
                DigitalHumanModule.ActionImitation,
                "simple_action_imitation",
                difficulty);

            DigitalHumanEventBus.PublishModuleChanged(DigitalHumanModule.ActionImitation);
            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                "准备好动作模仿了吗？跟我一起来！",
                DigitalHumanAvatarPose.Greeting,
                DigitalHumanEmotion.Friendly));
            SetSlowMode(difficulty == DigitalHumanDifficulty.Beginner);
            NextStep();
        }

        public void TogglePause()
        {
            if (paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            paused = true;
            AnimationSpeedChanged?.Invoke(0f);
            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                "我们先暂停一下，准备好了再继续。",
                DigitalHumanAvatarPose.Idle,
                DigitalHumanEmotion.Calm));
            ImitationStateChanged?.Invoke(steps, currentStepIndex, paused);
        }

        public void Resume()
        {
            paused = false;
            SetSlowMode(difficulty == DigitalHumanDifficulty.Beginner);
            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                "好，我们继续，慢慢来。",
                CurrentStep?.pose ?? DigitalHumanAvatarPose.Speaking,
                DigitalHumanEmotion.Encouraging));
            ImitationStateChanged?.Invoke(steps, currentStepIndex, paused);
        }

        public void ConfirmImitated(DigitalHumanParticipant participant)
        {
            if (paused || CurrentStep == null)
            {
                return;
            }

            var response = DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                "模仿得很好！",
                CurrentStep.pose,
                DigitalHumanEmotion.Encouraging,
                isCorrect: true,
                triggerReward: true);

            dataTracker?.RecordInteraction(
                DigitalHumanModule.ActionImitation,
                "simple_action_imitation",
                participant,
                DigitalHumanInputMode.Option,
                CurrentStep.stepId,
                response);

            DigitalHumanEventBus.PublishReward("太棒了！", 1.6f);
            NextStep();
        }

        public void SetSlowMode(bool slow)
        {
            float speed = slow ? beginnerSpeed : normalSpeed;
            if (paused)
            {
                speed = 0f;
            }

            AnimationSpeedChanged?.Invoke(speed);
        }

        private void NextStep()
        {
            currentStepIndex++;
            if (currentStepIndex >= steps.Count)
            {
                CompleteImitation();
                return;
            }

            DigitalHumanImitationStep step = CurrentStep;
            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                step.instruction,
                step.pose,
                DigitalHumanEmotion.Friendly));
            ImitationStateChanged?.Invoke(steps, currentStepIndex, paused);
        }

        private void CompleteImitation()
        {
            var response = DigitalHumanResponse.Say(
                DigitalHumanModule.ActionImitation,
                "动作模仿完成啦，你们太厉害啦！",
                DigitalHumanAvatarPose.Celebrate,
                DigitalHumanEmotion.Celebrating,
                isCorrect: true,
                triggerReward: true,
                taskCompleted: true,
                scenarioFinished: true);

            dataTracker?.MarkTaskCompleted("动作模仿流程完成");
            dataTracker?.RecordInteraction(
                DigitalHumanModule.ActionImitation,
                "simple_action_imitation",
                DigitalHumanParticipant.System,
                DigitalHumanInputMode.Option,
                "imitation_complete",
                response);
            dataTracker?.EndSession();

            DigitalHumanEventBus.PublishReward("完成啦！", 10f);
            DigitalHumanEventBus.PublishResponse(response);
            DigitalHumanEventBus.PublishTaskCompleted(DigitalHumanModule.ActionImitation, "simple_action_imitation");
            ImitationStateChanged?.Invoke(steps, currentStepIndex, paused);
        }
    }
}
