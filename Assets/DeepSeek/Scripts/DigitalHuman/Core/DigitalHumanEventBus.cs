using System;
using System.Collections.Generic;

namespace DeepSeek.DigitalHuman
{
    public static class DigitalHumanEventBus
    {
        public static event Action<DigitalHumanModule> ModuleChanged;
        public static event Action<DigitalHumanResponse> DigitalHumanResponded;
        public static event Action<IReadOnlyList<DigitalHumanDialogueOption>> DialogueOptionsChanged;
        public static event Action<DigitalHumanAvatarPose, DigitalHumanEmotion> AvatarPoseRequested;
        public static event Action<string, float> RewardRequested;
        public static event Action<DigitalHumanModule, string> TaskCompleted;
        public static event Action<DigitalHumanSessionRecord> SessionStarted;
        public static event Action<DigitalHumanSessionRecord> SessionUpdated;
        public static event Action<DigitalHumanSessionRecord> SessionEnded;
        public static event Action<DigitalHumanInteractionRecord> InteractionRecorded;

        public static void PublishModuleChanged(DigitalHumanModule module)
        {
            ModuleChanged?.Invoke(module);
        }

        public static void PublishResponse(DigitalHumanResponse response)
        {
            DigitalHumanResponded?.Invoke(response);
            DialogueOptionsChanged?.Invoke(response.Options);
            AvatarPoseRequested?.Invoke(response.Pose, response.Emotion);
        }

        public static void PublishReward(string message, float visibleSeconds)
        {
            RewardRequested?.Invoke(message, visibleSeconds);
        }

        public static void PublishTaskCompleted(DigitalHumanModule module, string taskId)
        {
            TaskCompleted?.Invoke(module, taskId);
        }

        public static void PublishSessionStarted(DigitalHumanSessionRecord record)
        {
            SessionStarted?.Invoke(record);
        }

        public static void PublishSessionUpdated(DigitalHumanSessionRecord record)
        {
            SessionUpdated?.Invoke(record);
        }

        public static void PublishSessionEnded(DigitalHumanSessionRecord record)
        {
            SessionEnded?.Invoke(record);
        }

        public static void PublishInteractionRecorded(DigitalHumanInteractionRecord record)
        {
            InteractionRecorded?.Invoke(record);
        }
    }
}
