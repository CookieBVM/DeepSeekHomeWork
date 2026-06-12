using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanAIApiService : MonoBehaviour
    {
        [SerializeField] private bool enableRemoteApi;
        [SerializeField] private string apiKey;
        [SerializeField] private string playerPrefsApiKey = "DEEPSEEK_API_KEY";
        [SerializeField] private string model = "deepseek-chat";

        private DeepSeekAI client;

        public bool IsEnabled => enableRemoteApi && !string.IsNullOrWhiteSpace(apiKey);
        public string ConnectionStatus => IsEnabled ? "DeepSeek API 已配置，发送消息会请求接口" : "未配置 API Key";

        public void Configure(string key, bool enabled)
        {
            apiKey = ResolveApiKey(key);
            enableRemoteApi = enabled && !string.IsNullOrWhiteSpace(apiKey);
            client = null;
        }

        public async Task<string> GenerateInterpersonalReplyAsync(
            string roleName,
            string userInput,
            IReadOnlyList<DigitalHumanDialogueOption> safeOptions)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(userInput))
            {
                return null;
            }

            try
            {
                client ??= new DeepSeekAI(apiKey);
                string optionText = safeOptions == null
                    ? string.Empty
                    : string.Join("、", BuildOptionLabels(safeOptions));

                var request = new Configuration.ChatCompletionRequest
                {
                    model = model,
                    stream = false,
                    messages = new List<Configuration.ChatMessage>
                    {
                        new Configuration.ChatMessage
                        {
                            role = "system",
                            content =
                                "你是面向自闭症儿童互动游戏的数字人。" +
                                "回答必须温和、短句、正向，不批评孩子。" +
                                "如果孩子偏离任务，请用一句话引导回当前可选项。"
                        },
                        new Configuration.ChatMessage
                        {
                            role = "user",
                            content = $"{roleName}收到孩子输入：{userInput}。当前安全选项：{optionText}"
                        }
                    }
                };

                ChatCompletionResponse response = await client.SendChatCompletionToDeepSeek(request);
                if (response?.choices != null && response.choices.Count > 0)
                {
                    return response.choices[0].message?.content;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DigitalHuman AI fallback skipped: {ex.Message}");
            }

            return null;
        }

        public async Task<string> SendChatAsync(List<Configuration.ChatMessage> history)
        {
            if (!IsEnabled || history == null || history.Count == 0)
            {
                return "DeepSeek API 未配置，请在 DigitalHumanGameController 的 deepSeekApiKey 填入你的 API Key，或设置 PlayerPrefs/环境变量 DEEPSEEK_API_KEY。";
            }

            try
            {
                client ??= new DeepSeekAI(apiKey);
                var request = new Configuration.ChatCompletionRequest
                {
                    model = model,
                    stream = false,
                    messages = history
                };

                ChatCompletionResponse response = await client.SendChatCompletionToDeepSeek(request);
                if (response?.choices != null && response.choices.Count > 0)
                {
                    string content = response.choices[0].message?.content;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return content;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DeepSeek chat request failed: {ex.Message}");
                return $"DeepSeek 请求失败：{ex.Message}";
            }

            return "DeepSeek 暂时没有返回内容。";
        }

        private static IEnumerable<string> BuildOptionLabels(IReadOnlyList<DigitalHumanDialogueOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                yield return options[i].label;
            }
        }

        private string ResolveApiKey(string explicitKey)
        {
            if (!string.IsNullOrWhiteSpace(explicitKey))
            {
                return explicitKey;
            }

            if (!string.IsNullOrWhiteSpace(playerPrefsApiKey) && PlayerPrefs.HasKey(playerPrefsApiKey))
            {
                string storedKey = PlayerPrefs.GetString(playerPrefsApiKey);
                if (!string.IsNullOrWhiteSpace(storedKey))
                {
                    return storedKey;
                }
            }

            string envKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return envKey;
            }

            return Environment.GetEnvironmentVariable("DEEPSEEK_APIKEY");
        }
    }
}
