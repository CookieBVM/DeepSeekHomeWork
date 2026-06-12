using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public enum DigitalHumanModule
    {
        None,
        DeepSeekChat,
        InterpersonalCommunication,
        ParentChildColoring,
        ActionImitation
    }

    public enum DigitalHumanDifficulty
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public enum DigitalHumanInputMode
    {
        Option,
        Text,
        Voice
    }

    public enum DigitalHumanParticipant
    {
        Child,
        Parent,
        System
    }

    public enum DigitalHumanAvatarPose
    {
        Idle,
        Greeting,
        Speaking,
        OfferItem,
        ColorPrompt,
        ImitationWave,
        ImitationClap,
        ImitationNod,
        Celebrate
    }

    public enum DigitalHumanEmotion
    {
        Calm,
        Friendly,
        Encouraging,
        Celebrating
    }

    [Serializable]
    public class DigitalHumanDialogueOption
    {
        public string id;
        public string label;
        public string expectedIntent;
        public string response;
        public string nextNodeId;
        public bool isCorrect = true;
        public bool triggersReward = true;
        public DigitalHumanAvatarPose responsePose = DigitalHumanAvatarPose.Speaking;
    }

    [Serializable]
    public class DigitalHumanDialogueNode
    {
        public string id;
        public string speakerLine;
        public DigitalHumanAvatarPose pose = DigitalHumanAvatarPose.Speaking;
        public bool isTerminal;
        public List<DigitalHumanDialogueOption> options = new List<DigitalHumanDialogueOption>();
    }

    [Serializable]
    public class DigitalHumanScenarioDefinition
    {
        public string scenarioId;
        public string displayName;
        public string roleName;
        public string startNodeId;
        public Color32 moduleColor = new Color32(248, 186, 118, 255);
        public List<DigitalHumanDialogueNode> nodes = new List<DigitalHumanDialogueNode>();
    }

    [Serializable]
    public class DigitalHumanColoringArea
    {
        public string areaId;
        public string displayName;
        public DigitalHumanParticipant requiredParticipant;
        public Color32 targetColor;
        public bool completed;
    }

    [Serializable]
    public class DigitalHumanImitationStep
    {
        public string stepId;
        public string instruction;
        public DigitalHumanAvatarPose pose;
        public float demonstrationSeconds = 2f;
    }

    [Serializable]
    public class DigitalHumanInteractionRecord
    {
        public string timestampUtc;
        public DigitalHumanModule module;
        public string scenarioId;
        public DigitalHumanParticipant participant;
        public DigitalHumanInputMode inputMode;
        public string input;
        public string response;
        public bool correct;
        public float elapsedSeconds;
    }

    [Serializable]
    public class DigitalHumanChatMessage
    {
        public string role;
        public string content;
        public bool pending;

        public bool IsUser => string.Equals(role, "user", StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public class DigitalHumanSessionRecord
    {
        public string sessionId;
        public DigitalHumanModule module;
        public string scenarioId;
        public DigitalHumanDifficulty difficulty;
        public string startedAtUtc;
        public string endedAtUtc;
        public float durationSeconds;
        public int totalInteractionCount;
        public int correctInteractionCount;
        public int completedTaskCount;
        public bool synced;
        public List<DigitalHumanInteractionRecord> interactions = new List<DigitalHumanInteractionRecord>();
        public List<string> notes = new List<string>();
    }

    public struct DigitalHumanResponse
    {
        public string Line;
        public bool IsCorrect;
        public bool TriggerReward;
        public bool TaskCompleted;
        public bool ScenarioFinished;
        public DigitalHumanAvatarPose Pose;
        public DigitalHumanEmotion Emotion;
        public DigitalHumanModule Module;
        public IReadOnlyList<DigitalHumanDialogueOption> Options;

        public static DigitalHumanResponse Say(
            DigitalHumanModule module,
            string line,
            DigitalHumanAvatarPose pose,
            DigitalHumanEmotion emotion,
            IReadOnlyList<DigitalHumanDialogueOption> options = null,
            bool isCorrect = true,
            bool triggerReward = false,
            bool taskCompleted = false,
            bool scenarioFinished = false)
        {
            return new DigitalHumanResponse
            {
                Module = module,
                Line = line,
                Pose = pose,
                Emotion = emotion,
                Options = options,
                IsCorrect = isCorrect,
                TriggerReward = triggerReward,
                TaskCompleted = taskCompleted,
                ScenarioFinished = scenarioFinished
            };
        }
    }

    public static class DigitalHumanScenarioLibrary
    {
        public static DigitalHumanScenarioDefinition CreateVegetableMarketScenario()
        {
            return new DigitalHumanScenarioDefinition
            {
                scenarioId = "vegetable_market",
                displayName = "买菜场景",
                roleName = "数字人老板",
                startNodeId = "greeting",
                moduleColor = new Color32(247, 171, 92, 255),
                nodes = new List<DigitalHumanDialogueNode>
                {
                    new DigitalHumanDialogueNode
                    {
                        id = "greeting",
                        speakerLine = "小朋友，想买点什么呀？",
                        pose = DigitalHumanAvatarPose.Greeting,
                        options = new List<DigitalHumanDialogueOption>
                        {
                            new DigitalHumanDialogueOption
                            {
                                id = "apple",
                                label = "我想买苹果",
                                expectedIntent = "苹果",
                                response = "给你苹果！",
                                nextNodeId = "after_buy",
                                responsePose = DigitalHumanAvatarPose.OfferItem
                            },
                            new DigitalHumanDialogueOption
                            {
                                id = "tomato",
                                label = "我想买西红柿",
                                expectedIntent = "西红柿 番茄",
                                response = "给你西红柿！",
                                nextNodeId = "after_buy",
                                responsePose = DigitalHumanAvatarPose.OfferItem
                            },
                            new DigitalHumanDialogueOption
                            {
                                id = "hello",
                                label = "老板好",
                                expectedIntent = "你好 老板好",
                                response = "你好呀！你可以告诉我想买什么。",
                                nextNodeId = "greeting",
                                triggersReward = false,
                                responsePose = DigitalHumanAvatarPose.Greeting
                            }
                        }
                    },
                    new DigitalHumanDialogueNode
                    {
                        id = "after_buy",
                        speakerLine = "还想买别的吗？",
                        pose = DigitalHumanAvatarPose.Speaking,
                        options = new List<DigitalHumanDialogueOption>
                        {
                            new DigitalHumanDialogueOption
                            {
                                id = "continue",
                                label = "还想买",
                                expectedIntent = "还想 继续 再买",
                                response = "好的，我们继续慢慢选。",
                                nextNodeId = "greeting",
                                triggersReward = false,
                                responsePose = DigitalHumanAvatarPose.Speaking
                            },
                            new DigitalHumanDialogueOption
                            {
                                id = "finish",
                                label = "不买了，谢谢",
                                expectedIntent = "不买 结束 谢谢",
                                response = "好的，谢谢你。今天交流得很好！",
                                nextNodeId = "finish",
                                responsePose = DigitalHumanAvatarPose.Celebrate
                            },
                            new DigitalHumanDialogueOption
                            {
                                id = "help",
                                label = "我想请爸爸妈妈帮忙",
                                expectedIntent = "帮忙 爸爸 妈妈 家长",
                                response = "当然可以，和爸爸妈妈一起说会更轻松。",
                                nextNodeId = "after_buy",
                                triggersReward = false,
                                responsePose = DigitalHumanAvatarPose.Speaking
                            }
                        }
                    },
                    new DigitalHumanDialogueNode
                    {
                        id = "finish",
                        speakerLine = "完成啦，你们太厉害啦！",
                        pose = DigitalHumanAvatarPose.Celebrate,
                        isTerminal = true,
                        options = new List<DigitalHumanDialogueOption>()
                    }
                }
            };
        }

        public static List<DigitalHumanColoringArea> CreateColoringAreas(DigitalHumanDifficulty difficulty)
        {
            int count = difficulty == DigitalHumanDifficulty.Beginner ? 3 :
                difficulty == DigitalHumanDifficulty.Intermediate ? 5 : 7;

            var colors = new[]
            {
                new Color32(238, 83, 80, 255),
                new Color32(66, 165, 245, 255),
                new Color32(102, 187, 106, 255),
                new Color32(255, 202, 40, 255),
                new Color32(171, 71, 188, 255),
                new Color32(255, 112, 67, 255),
                new Color32(38, 198, 218, 255)
            };

            var names = new[] { "红色区域", "蓝色区域", "绿色区域", "黄色区域", "紫色区域", "橙色区域", "青色区域" };
            var areas = new List<DigitalHumanColoringArea>();
            for (int i = 0; i < count; i++)
            {
                areas.Add(new DigitalHumanColoringArea
                {
                    areaId = $"area_{i + 1}",
                    displayName = names[i],
                    targetColor = colors[i],
                    requiredParticipant = i % 2 == 0 ? DigitalHumanParticipant.Child : DigitalHumanParticipant.Parent,
                    completed = false
                });
            }

            return areas;
        }

        public static List<DigitalHumanImitationStep> CreateImitationSteps(DigitalHumanDifficulty difficulty)
        {
            var steps = new List<DigitalHumanImitationStep>
            {
                new DigitalHumanImitationStep
                {
                    stepId = "wave",
                    instruction = "请看我挥挥手。准备好以后，宝宝也挥挥手。",
                    pose = DigitalHumanAvatarPose.ImitationWave,
                    demonstrationSeconds = 2f
                },
                new DigitalHumanImitationStep
                {
                    stepId = "clap",
                    instruction = "现在我们轻轻拍手，两下就可以。",
                    pose = DigitalHumanAvatarPose.ImitationClap,
                    demonstrationSeconds = 2.5f
                },
                new DigitalHumanImitationStep
                {
                    stepId = "nod",
                    instruction = "最后点点头，慢慢来。",
                    pose = DigitalHumanAvatarPose.ImitationNod,
                    demonstrationSeconds = 2f
                }
            };

            if (difficulty != DigitalHumanDifficulty.Beginner)
            {
                steps.Add(new DigitalHumanImitationStep
                {
                    stepId = "wave_parent",
                    instruction = "现在请爸爸妈妈一起挥挥手，我们一起完成。",
                    pose = DigitalHumanAvatarPose.ImitationWave,
                    demonstrationSeconds = 2.5f
                });
            }

            if (difficulty == DigitalHumanDifficulty.Advanced)
            {
                steps.Add(new DigitalHumanImitationStep
                {
                    stepId = "clap_together",
                    instruction = "最后大家一起拍手三下，完成挑战。",
                    pose = DigitalHumanAvatarPose.ImitationClap,
                    demonstrationSeconds = 3f
                });
            }

            return steps;
        }
    }
}
