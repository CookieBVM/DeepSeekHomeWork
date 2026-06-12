using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Configuration;

public class Configuration
{
    public string ApiKey { get; }

    public Configuration(string apiKey)
    {
        ApiKey = apiKey;
    }

    //聊天对话消息完成请求
    public class ChatCompletionRequest
    {
        //消息列表
        public List<ChatMessage> messages;

        //AI模型 是聊天模型 还是推理模型
        public string model;

        //如果设置为true 将会以SSE(server sent events)的形式以流式发送消息增量,消息流以data:[DONE]结尾
        public bool stream;
    }

    public class ChatMessage
    {
        //消息内容
        public string content;

        //角色 是哪个角色的消息（是用户消息还是DeepSeek系统消息 又或者是我们自定义的NPC角色消息)
        public string role;
    }
}

//DeepSeek响应数据模型

public class ChatCompletionResponse
{
    /// <summary>
    /// id
    /// </summary>
    public string id;
    /// <summary>
    /// 创建时间
    /// </summary>
    public long created;
    //AI模型 是聊天模型还是推理模型
    public string model;

    //可选择的消息内容
    public List<ChatResponseMessage> choices;

}

public class ChatResponseMessage
{
    //消息索引
    public int index;
    //消息列表
    public ChatMessage message;

    public string finish_reason;
    
}


//{
//    "id": "8dda34fd-a045-4657-ae23-d58e082e3d4c",
//  "object": "chat.completion",
//  "created": 1769269849,
//  "model": "deepseek-chat",
//  "choices": [
//    {
//        "index": 0,
//      "message": {
//            "role": "assistant",
//        "content": "Hello! How can I assist you today? 😊"
//      },
//      "logprobs": null,
//      "finish_reason": "stop"
//    }
//  ],
//  "usage": {
//        "prompt_tokens": 10,
//    "completion_tokens": 11,
//    "total_tokens": 21,
//    "prompt_tokens_details": {
//            "cached_tokens": 0
//    },
//    "prompt_cache_hit_tokens": 0,
//    "prompt_cache_miss_tokens": 10
//  },
//  "system_fingerprint": "fp_eaab8d114b_prod0820_fp8_kvcache"
//}
