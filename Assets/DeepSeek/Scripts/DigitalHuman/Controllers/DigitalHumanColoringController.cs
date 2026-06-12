using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanColoringController : MonoBehaviour
    {
        public event Action<IReadOnlyList<DigitalHumanColoringArea>, int> ColoringStateChanged;

        private readonly List<DigitalHumanColoringArea> areas = new List<DigitalHumanColoringArea>();

        private DigitalHumanDataTracker dataTracker;
        private DigitalHumanDifficulty difficulty;
        private int currentAreaIndex;

        public IReadOnlyList<DigitalHumanColoringArea> Areas => areas;
        public DigitalHumanColoringArea CurrentArea =>
            currentAreaIndex >= 0 && currentAreaIndex < areas.Count ? areas[currentAreaIndex] : null;

        public void Initialize(DigitalHumanDataTracker tracker, DigitalHumanDifficulty selectedDifficulty)
        {
            dataTracker = tracker;
            difficulty = selectedDifficulty;
        }

        public void Begin()
        {
            areas.Clear();
            areas.AddRange(DigitalHumanScenarioLibrary.CreateColoringAreas(difficulty));
            currentAreaIndex = 0;

            dataTracker?.StartSession(
                DigitalHumanModule.ParentChildColoring,
                "cooperative_coloring",
                difficulty);

            DigitalHumanEventBus.PublishModuleChanged(DigitalHumanModule.ParentChildColoring);
            PublishPrompt("现在和爸爸妈妈一起完成涂色吧。宝宝先点击红色区域。", false);
            ColoringStateChanged?.Invoke(areas, currentAreaIndex);
        }

        public void SelectArea(string areaId, DigitalHumanParticipant participant)
        {
            DigitalHumanColoringArea current = CurrentArea;
            if (current == null)
            {
                return;
            }

            bool participantMatches = current.requiredParticipant == participant;
            bool areaMatches = string.Equals(current.areaId, areaId, StringComparison.OrdinalIgnoreCase);

            if (!participantMatches || !areaMatches)
            {
                string turnName = current.requiredParticipant == DigitalHumanParticipant.Child
                    ? "瀹濆疂"
                    : "鐖哥埜濡堝";
                string line = $"我们慢慢来，现在请{turnName}点击{current.displayName}。";
                var retryResponse = DigitalHumanResponse.Say(
                    DigitalHumanModule.ParentChildColoring,
                    line,
                    DigitalHumanAvatarPose.ColorPrompt,
                    DigitalHumanEmotion.Encouraging,
                    isCorrect: false);

                dataTracker?.RecordInteraction(
                    DigitalHumanModule.ParentChildColoring,
                    "cooperative_coloring",
                    participant,
                    DigitalHumanInputMode.Option,
                    areaId,
                    retryResponse);

                DigitalHumanEventBus.PublishResponse(retryResponse);
                ColoringStateChanged?.Invoke(areas, currentAreaIndex);
                return;
            }

            current.completed = true;
            var correctResponse = DigitalHumanResponse.Say(
                DigitalHumanModule.ParentChildColoring,
                $"{current.displayName}瀹屾垚鍟︼紒",
                DigitalHumanAvatarPose.ColorPrompt,
                DigitalHumanEmotion.Encouraging,
                isCorrect: true,
                triggerReward: true);

            dataTracker?.RecordInteraction(
                DigitalHumanModule.ParentChildColoring,
                "cooperative_coloring",
                participant,
                DigitalHumanInputMode.Option,
                areaId,
                correctResponse);

            DigitalHumanEventBus.PublishCustomAnimation("Standing_Clap");
            DigitalHumanEventBus.PublishReward("涂色成功！", 1.6f);
            currentAreaIndex++;

            if (currentAreaIndex >= areas.Count)
            {
                CompleteColoring();
                return;
            }

            DigitalHumanColoringArea next = CurrentArea;
            string nextName = next.requiredParticipant == DigitalHumanParticipant.Child ? "瀹濆疂" : "鐖哥埜濡堝";
            PublishPrompt($"接下来请{nextName}点击{next.displayName}。", false);
            ColoringStateChanged?.Invoke(areas, currentAreaIndex);
        }

        private void CompleteColoring()
        {
            var response = DigitalHumanResponse.Say(
                DigitalHumanModule.ParentChildColoring,
                "完成啦，你们太厉害啦！",
                DigitalHumanAvatarPose.Celebrate,
                DigitalHumanEmotion.Celebrating,
                isCorrect: true,
                triggerReward: true,
                taskCompleted: true,
                scenarioFinished: true);

            dataTracker?.MarkTaskCompleted("浜插瓙鍚堜綔娑傝壊瀹屾垚");
            dataTracker?.RecordInteraction(
                DigitalHumanModule.ParentChildColoring,
                "cooperative_coloring",
                DigitalHumanParticipant.System,
                DigitalHumanInputMode.Option,
                "coloring_complete",
                response);
            dataTracker?.EndSession();

            DigitalHumanEventBus.PublishCustomAnimation("Cheering");
            DigitalHumanEventBus.PublishReward("完成啦！", 10f);
            DigitalHumanEventBus.PublishResponse(response);
            DigitalHumanEventBus.PublishTaskCompleted(DigitalHumanModule.ParentChildColoring, "cooperative_coloring");
            ColoringStateChanged?.Invoke(areas, currentAreaIndex);
        }

        private void PublishPrompt(string line, bool taskCompleted)
        {
            var response = DigitalHumanResponse.Say(
                DigitalHumanModule.ParentChildColoring,
                line,
                DigitalHumanAvatarPose.ColorPrompt,
                DigitalHumanEmotion.Friendly,
                isCorrect: true,
                taskCompleted: taskCompleted);

            DigitalHumanEventBus.PublishResponse(response);
        }
    }
}



