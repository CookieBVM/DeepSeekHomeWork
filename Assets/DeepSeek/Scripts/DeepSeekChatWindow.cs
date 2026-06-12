using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using static Configuration;

namespace DeepSeek
{
    public class DeepSeekChatWindow : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect chatScroll;
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;

        private float contentHeight;
        private DeepSeekAI deepSeekAI = new DeepSeekAI("sk-bb26bc0c41904201b9127f5b7677d9c2");

        private List<ChatMessage> messages = new List<ChatMessage>();
        private string initialPrompt = "Act as a helpful assistant.";
        private delegate void SendMessageDelegate();
        private SendMessageDelegate sendMessage;

        private void Start()
        {
            sendMessage += SendMessageToDeepSeek;
            sendButton.onClick.AddListener(() => sendMessage?.Invoke());
        }

        /// <summary>
        /// 追加聊天消息到Canvas上
        /// </summary>
        /// <param name="message">消息模型</param>
        /// <param name="isUser">是否是用户</param>
        private void AppendMessageToCanvs(string message,bool isUser)
        {
            chatScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            var item = Instantiate(isUser ? sent : received, chatScroll.content);
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message;
            item.anchoredPosition = new Vector2(0, -contentHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            contentHeight += item.sizeDelta.y;
            chatScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            chatScroll.verticalNormalizedPosition = 0;
        }

        private async void SendMessageToDeepSeek()
        {
            //创建聊天消息
            var userMessage = new ChatMessage
            {
                role = "user",
                content = inputField.text
            };
            //显示消息
            AppendMessageToCanvs(userMessage.content, true);
            //添加消息
            //细节！！ 为什么传的是List 因为传list了deepseek才能知道
            //你以前跟他说了什么 这样才方便它之后进行回复 提高了回复的准确性
            messages.Add(userMessage);

            //创建消息交互请求
            var request = new ChatCompletionRequest
            {
                model = "deepseek-chat",
                messages = messages,
            };

            
            //发送对话完成消息到DeepSeek
            var response = await deepSeekAI.SendChatCompletionToDeepSeek(request);
            //处理响应
            if (response?.choices != null && response.choices.Count > 0)
            {
                var assistantMessage = response.choices[0].message;
                messages.Add(assistantMessage);
                //显示消息
                AppendMessageToCanvs(assistantMessage.content, false);
            }
            else
            {
                Debug.LogWarning("No response from DeepSeek.");
             }

                inputField.text = "";
        }
    }
}
