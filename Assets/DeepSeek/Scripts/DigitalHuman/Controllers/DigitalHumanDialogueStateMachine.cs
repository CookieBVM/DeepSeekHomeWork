using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanDialogueStateMachine
    {
        private readonly Dictionary<string, DigitalHumanDialogueNode> nodes = new Dictionary<string, DigitalHumanDialogueNode>();
        private DigitalHumanScenarioDefinition scenario;
        private DigitalHumanDialogueNode currentNode;

        public string ScenarioId => scenario?.scenarioId;
        public IReadOnlyList<DigitalHumanDialogueOption> CurrentOptions => currentNode?.options;

        public DigitalHumanResponse Start(DigitalHumanScenarioDefinition definition)
        {
            scenario = definition ?? throw new ArgumentNullException(nameof(definition));
            nodes.Clear();

            foreach (var node in scenario.nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.id))
                {
                    nodes[node.id] = node;
                }
            }

            currentNode = nodes.TryGetValue(scenario.startNodeId, out var startNode)
                ? startNode
                : scenario.nodes.FirstOrDefault();

            if (currentNode == null)
            {
                return DigitalHumanResponse.Say(
                    DigitalHumanModule.InterpersonalCommunication,
                    "我们先从一个简单的问候开始吧。",
                    DigitalHumanAvatarPose.Greeting,
                    DigitalHumanEmotion.Friendly);
            }

            return NodeResponse(currentNode, false);
        }

        public DigitalHumanResponse SelectOption(string optionIdOrLabel)
        {
            var option = FindOption(optionIdOrLabel, DigitalHumanInputMode.Option);
            return ApplyOption(option, optionIdOrLabel);
        }

        public DigitalHumanResponse SubmitText(string input, DigitalHumanInputMode inputMode)
        {
            var option = FindOption(input, inputMode);
            return ApplyOption(option, input);
        }

        private DigitalHumanDialogueOption FindOption(string input, DigitalHumanInputMode inputMode)
        {
            if (currentNode?.options == null || currentNode.options.Count == 0 || string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            string normalized = Normalize(input);
            foreach (var option in currentNode.options)
            {
                if (Normalize(option.id) == normalized || Normalize(option.label) == normalized)
                {
                    return option;
                }
            }

            if (inputMode == DigitalHumanInputMode.Option)
            {
                return null;
            }

            foreach (var option in currentNode.options)
            {
                if (ContainsAnyKeyword(normalized, option.expectedIntent) ||
                    ContainsAnyKeyword(normalized, option.label))
                {
                    return option;
                }
            }

            return null;
        }

        private DigitalHumanResponse ApplyOption(DigitalHumanDialogueOption option, string input)
        {
            if (option == null)
            {
                string helper = currentNode?.options != null && currentNode.options.Count > 0
                    ? $"我们慢慢来，可以试试：{currentNode.options[0].label}"
                    : "没关系，我们可以再试一次。";

                return DigitalHumanResponse.Say(
                    DigitalHumanModule.InterpersonalCommunication,
                    helper,
                    DigitalHumanAvatarPose.Speaking,
                    DigitalHumanEmotion.Encouraging,
                    currentNode?.options,
                    isCorrect: false,
                    triggerReward: false);
            }

            DigitalHumanDialogueNode nextNode = currentNode;
            if (!string.IsNullOrWhiteSpace(option.nextNodeId) && nodes.TryGetValue(option.nextNodeId, out var foundNode))
            {
                nextNode = foundNode;
            }

            currentNode = nextNode;
            bool finished = currentNode != null && currentNode.isTerminal;
            IReadOnlyList<DigitalHumanDialogueOption> options = finished ? null : currentNode?.options;
            string line = option.response;

            if (finished && !string.IsNullOrWhiteSpace(currentNode.speakerLine))
            {
                line = currentNode.speakerLine;
            }

            return DigitalHumanResponse.Say(
                DigitalHumanModule.InterpersonalCommunication,
                line,
                option.responsePose,
                finished ? DigitalHumanEmotion.Celebrating : DigitalHumanEmotion.Encouraging,
                options,
                option.isCorrect,
                option.triggersReward,
                finished,
                finished);
        }

        private static DigitalHumanResponse NodeResponse(DigitalHumanDialogueNode node, bool triggerReward)
        {
            return DigitalHumanResponse.Say(
                DigitalHumanModule.InterpersonalCommunication,
                node.speakerLine,
                node.pose,
                DigitalHumanEmotion.Friendly,
                node.options,
                isCorrect: true,
                triggerReward: triggerReward,
                taskCompleted: node.isTerminal,
                scenarioFinished: node.isTerminal);
        }

        private static bool ContainsAnyKeyword(string normalizedInput, string keywordSource)
        {
            if (string.IsNullOrWhiteSpace(keywordSource))
            {
                return false;
            }

            string[] keywords = keywordSource
                .Split(new[] { ' ', ',', '，', '/', '|', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);

            return keywords.Any(keyword => normalizedInput.Contains(Normalize(keyword)));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        }
    }
}
