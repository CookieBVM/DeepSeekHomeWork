using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanChatController : MonoBehaviour
    {
        public event Action<IReadOnlyList<DigitalHumanChatMessage>> ChatMessagesChanged;
        public event Action<string, bool> ChatStatusChanged;

        private readonly List<DigitalHumanChatMessage> messages = new List<DigitalHumanChatMessage>();
        private readonly List<Configuration.ChatMessage> apiHistory = new List<Configuration.ChatMessage>();

        private DigitalHumanAIApiService aiApiService;
        private bool sending;

        public void Initialize(DigitalHumanAIApiService apiService)
        {
            aiApiService = apiService;
            PublishStatus();
            if (messages.Count == 0)
            {
                AddAssistantMessage("你好，我是 DeepSeek 数字人。你可以直接和我聊天。", false);
            }
        }

        public void Begin()
        {
            DigitalHumanEventBus.PublishModuleChanged(DigitalHumanModule.DeepSeekChat);
            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.DeepSeekChat,
                "你好，我在右侧陪你聊天。",
                DigitalHumanAvatarPose.Greeting,
                DigitalHumanEmotion.Friendly));
            PublishStatus();
            ChatMessagesChanged?.Invoke(messages);
        }

        public async void Submit(string text)
        {
            if (sending || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            sending = true;
            AddUserMessage(text.Trim());
            AddAssistantMessage("思考中...", true);
            PublishStatus("正在请求 DeepSeek...");

            apiHistory.Add(new Configuration.ChatMessage
            {
                role = "user",
                content = text.Trim()
            });

            string reply = await aiApiService.SendChatAsync(apiHistory);
            RemovePendingMessage();
            AddAssistantMessage(reply, false);
            apiHistory.Add(new Configuration.ChatMessage
            {
                role = "assistant",
                content = reply
            });

            DigitalHumanEventBus.PublishResponse(DigitalHumanResponse.Say(
                DigitalHumanModule.DeepSeekChat,
                reply,
                DigitalHumanAvatarPose.Speaking,
                DigitalHumanEmotion.Friendly));

            sending = false;
            PublishStatus();
        }

        public void Clear()
        {
            messages.Clear();
            apiHistory.Clear();
            AddAssistantMessage("聊天已清空，我们可以重新开始。", false);
            PublishStatus();
        }

        private void AddUserMessage(string content)
        {
            messages.Add(new DigitalHumanChatMessage
            {
                role = "user",
                content = content
            });
            ChatMessagesChanged?.Invoke(messages);
        }

        private void AddAssistantMessage(string content, bool pending)
        {
            messages.Add(new DigitalHumanChatMessage
            {
                role = "assistant",
                content = content,
                pending = pending
            });
            ChatMessagesChanged?.Invoke(messages);
        }

        private void RemovePendingMessage()
        {
            int index = messages.FindLastIndex(message => message.pending);
            if (index >= 0)
            {
                messages.RemoveAt(index);
            }
        }

        private void PublishStatus(string overrideStatus = null)
        {
            bool connected = aiApiService != null && aiApiService.IsEnabled;
            string status = string.IsNullOrWhiteSpace(overrideStatus)
                ? aiApiService?.ConnectionStatus ?? "未初始化 DeepSeek API"
                : overrideStatus;
            ChatStatusChanged?.Invoke(status, connected);
        }
    }
}
