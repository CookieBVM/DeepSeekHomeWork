using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using static Configuration;

public class DeepSeekAI
{
    /// <summary>
    /// DeepSeek api访问地址
    /// </summary>
    private const string BASE_PATH = "https://api.deepseek.com/chat/completions";

    //DeepSeek配置
    private Configuration configuration;
    /// <summary>
   
    /// </summary>
    /// <param name="apiKey"></param>
    /// <exception cref="ArgumentException"></exception>
    public DeepSeekAI(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("api key is null", nameof(apiKey));
        }

        configuration = new Configuration(apiKey);
    }

    /// <summary>
    /// 发送对话结束消息内容到DeepSeek
    /// </summary>
    public async Task<ChatCompletionResponse> SendChatCompletionToDeepSeek(ChatCompletionRequest requestMessage)
    {
        //把消息对象序列化成Json字符串
        string jsonMessage = JsonConvert.SerializeObject(requestMessage);
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, BASE_PATH);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Authorization", $"Bearer {configuration.ApiKey}");
        var content = new StringContent(jsonMessage,null,"application/json");

        Debug.Log("DeepSeek SendRequest:" + jsonMessage);
        request.Content = content;

        //发送API请求
        var response = await client.SendAsync(request);
        //验证响应是否是200 如果是200则说明接口请求成功
        response.EnsureSuccessStatusCode();

        //读取API响应内容
        string resultJson = await response.Content.ReadAsStringAsync();
        
        Debug.Log("DeepSeek Response:"+ resultJson );
        return JsonConvert.DeserializeObject<ChatCompletionResponse>(resultJson );
    }



}
