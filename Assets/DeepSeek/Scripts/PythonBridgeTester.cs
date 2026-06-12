using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace DeepSeek
{
    /// <summary>
    /// Python功能测试UI
    /// 用于在Unity编辑器中测试Python服务的各项功能
    /// </summary>
    public class PythonBridgeTester : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private PythonBridge pythonBridge;
        
        [Header("UI元素")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button pingButton;
        [SerializeField] private Button parseIntentButton;
        [SerializeField] private Button logEventButton;
        [SerializeField] private Button getStatsButton;
        [SerializeField] private Button generateReportButton;
        [SerializeField] private Button endSessionButton;
        
        [SerializeField] private TMP_InputField intentInputField;
        [SerializeField] private TMP_Dropdown sceneDropdown;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect logScroll;
        
        private int logLineCount = 0;
        private const int MAX_LOG_LINES = 100;
        
        private void Start()
        {
            if (pythonBridge == null)
            {
                pythonBridge = FindObjectOfType<PythonBridge>();
                if (pythonBridge == null)
                {
                    var go = new GameObject("PythonBridge");
                    pythonBridge = go.AddComponent<PythonBridge>();
                }
            }
            
            InitUI();
            SubscribeEvents();
            
            UpdateStatusText();
        }
        
        private void OnDestroy()
        {
            if (pythonBridge != null)
            {
                pythonBridge.OnResponseReceived -= HandleResponse;
                pythonBridge.OnError -= HandleError;
            }
        }
        
        private void InitUI()
        {
            startButton.onClick.AddListener(() => pythonBridge.StartPython());
            stopButton.onClick.AddListener(() => pythonBridge.StopPython());
            pingButton.onClick.AddListener(() => pythonBridge.Ping());
            parseIntentButton.onClick.AddListener(OnParseIntentClicked);
            logEventButton.onClick.AddListener(OnLogEventClicked);
            getStatsButton.onClick.AddListener(() => pythonBridge.GetStats());
            generateReportButton.onClick.AddListener(() => pythonBridge.GenerateReport());
            endSessionButton.onClick.AddListener(() => pythonBridge.EndSession());
            
            sceneDropdown.ClearOptions();
            sceneDropdown.AddOptions(new List<string> { "buying_vegetables (买菜场景)", "coloring (涂色场景)" });
            
            AddLog("Python测试面板已就绪");
            AddLog("请先点击【启动Python】按钮");
        }
        
        private void SubscribeEvents()
        {
            pythonBridge.OnResponseReceived += HandleResponse;
            pythonBridge.OnError += HandleError;
        }
        
        private void Update()
        {
            UpdateStatusText();
        }
        
        private void UpdateStatusText()
        {
            if (pythonBridge == null) return;
            
            if (pythonBridge.IsRunning)
            {
                statusText.text = "状态: <color=green>运行中</color>";
                startButton.interactable = false;
                stopButton.interactable = true;
                pingButton.interactable = true;
                parseIntentButton.interactable = true;
                logEventButton.interactable = true;
                getStatsButton.interactable = true;
                generateReportButton.interactable = true;
                endSessionButton.interactable = true;
            }
            else
            {
                statusText.text = "状态: <color=red>未运行</color>";
                startButton.interactable = true;
                stopButton.interactable = false;
                pingButton.interactable = false;
                parseIntentButton.interactable = false;
                logEventButton.interactable = false;
                getStatsButton.interactable = false;
                generateReportButton.interactable = false;
                endSessionButton.interactable = false;
            }
        }
        
        private void OnParseIntentClicked()
        {
            string text = intentInputField.text;
            string context = sceneDropdown.value == 0 ? "buying_vegetables" : "coloring";
            
            if (string.IsNullOrEmpty(text))
            {
                AddLog("请输入要分析的文本");
                return;
            }
            
            AddLog($"分析意图: \"{text}\" (场景: {context})");
            pythonBridge.ParseIntent(text, context);
        }
        
        private void OnLogEventClicked()
        {
            var data = new Dictionary<string, object>
            {
                { "text", "买苹果" },
                { "intent", "buy_item" },
                { "confidence", 0.95 }
            };
            
            AddLog("记录测试事件: intent_detected");
            pythonBridge.LogEvent("intent_detected", data, "buying_vegetables");
        }
        
        private void HandleResponse(PythonResponse response)
        {
            if (!response.success)
            {
                AddLog($"<color=red>错误: {response.error}</color>");
                return;
            }
            
            switch (response.action)
            {
                case "ping":
                    HandlePingResponse(response);
                    break;
                case "parse_intent":
                    HandleParseIntentResponse(response);
                    break;
                case "log_event":
                    HandleLogEventResponse(response);
                    break;
                case "get_stats":
                    HandleGetStatsResponse(response);
                    break;
                case "generate_report":
                    HandleGenerateReportResponse(response);
                    break;
                case "end_session":
                    HandleEndSessionResponse(response);
                    break;
                default:
                    AddLog($"收到响应: {response.action}");
                    break;
            }
        }
        
        private void HandlePingResponse(PythonResponse response)
        {
            var data = response.GetData<Dictionary<string, object>>();
            string status = data.ContainsKey("status") ? data["status"].ToString() : "unknown";
            string sessionId = data.ContainsKey("session_id") ? data["session_id"].ToString() : "unknown";
            
            AddLog($"<color=green>Ping成功!</color> 状态: {status}, 会话ID: {sessionId}");
        }
        
        private void HandleParseIntentResponse(PythonResponse response)
        {
            var result = response.GetData<IntentResultData>();
            
            AddLog("--- 意图分析结果 ---");
            AddLog($"  原始文本: {result.raw_text}");
            AddLog($"  意图: <color=yellow>{result.intent}</color>");
            AddLog($"  置信度: {result.confidence:P1}");
            
            if (result.entities != null && result.entities.Count > 0)
            {
                AddLog("  实体:");
                foreach (var kvp in result.entities)
                {
                    AddLog($"    {kvp.Key}: {string.Join(", ", kvp.Value)}");
                }
            }
            
            AddLog("--------------------");
        }
        
        private void HandleLogEventResponse(PythonResponse response)
        {
            var data = response.GetData<Dictionary<string, object>>();
            string eventId = data.ContainsKey("event_id") ? data["event_id"].ToString() : "unknown";
            string correctness = data.ContainsKey("correctness") ? data["correctness"].ToString() : "unknown";
            
            AddLog($"<color=green>事件记录成功!</color> ID: {eventId}, 正确性: {correctness}");
            
            if (data.ContainsKey("stats"))
            {
                var stats = data["stats"] as Dictionary<string, object>;
                if (stats != null && stats.ContainsKey("accuracy"))
                {
                    double accuracy = System.Convert.ToDouble(stats["accuracy"]);
                    AddLog($"  当前正确率: {accuracy:P0}");
                    
                    if (stats.ContainsKey("should_trigger_feedback") && 
                        System.Convert.ToBoolean(stats["should_trigger_feedback"]))
                    {
                        AddLog("<color=yellow>🎉 触发正反馈!</color>");
                    }
                }
            }
        }
        
        private void HandleGetStatsResponse(PythonResponse response)
        {
            var stats = response.GetData<SessionStatsData>();
            
            AddLog("--- 当前统计数据 ---");
            AddLog($"  会话ID: {stats.session_id}");
            AddLog($"  总时长: {stats.total_duration:F1} 秒");
            AddLog($"  正确率: <color=yellow>{stats.accuracy:P0}</color>");
            AddLog($"  总事件数: {stats.total_events}");
            AddLog($"  正确: {stats.correct_events}");
            AddLog($"  错误: {stats.wrong_events}");
            AddLog($"  连续正确: {stats.consecutive_correct}");
            AddLog($"  最高连续正确: {stats.max_consecutive_correct}");
            AddLog($"  触发正反馈: {(stats.should_trigger_feedback ? "<color=yellow>是</color>" : "否")}");
            
            if (stats.scene_stats != null)
            {
                AddLog("  各场景统计:");
                foreach (var kvp in stats.scene_stats)
                {
                    int correct = kvp.Value.ContainsKey("correct") ? kvp.Value["correct"] : 0;
                    int total = kvp.Value.ContainsKey("total") ? kvp.Value["total"] : 0;
                    double acc = total > 0 ? (double)correct / total : 0;
                    AddLog($"    {kvp.Key}: {correct}/{total} ({acc:P0})");
                }
            }
            
            AddLog("---------------------");
        }
        
        private void HandleGenerateReportResponse(PythonResponse response)
        {
            var data = response.GetData<Dictionary<string, object>>();
            
            AddLog("--- 报告生成 ---");
            if (data.ContainsKey("summary_text"))
            {
                AddLog($"  总结: {data["summary_text"]}");
            }
            AddLog("<color=green>报告已生成!</color>");
            AddLog("-------------------");
        }
        
        private void HandleEndSessionResponse(PythonResponse response)
        {
            var data = response.GetData<Dictionary<string, object>>();
            
            AddLog("--- 会话结束 ---");
            if (data.ContainsKey("session_id"))
            {
                AddLog($"  会话ID: {data["session_id"]}");
            }
            AddLog("<color=green>会话已结束，数据已保存!</color>");
            AddLog("----------------");
        }
        
        private void HandleError(string error)
        {
            AddLog($"<color=red>错误: {error}</color>");
        }
        
        private void AddLog(string message)
        {
            if (logText == null) return;
            
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logLine = $"[{timestamp}] {message}\n";
            
            logText.text += logLine;
            logLineCount++;
            
            if (logLineCount > MAX_LOG_LINES)
            {
                int linesToRemove = logLineCount - MAX_LOG_LINES;
                string[] lines = logText.text.Split('\n');
                if (lines.Length > MAX_LOG_LINES)
                {
                    logText.text = string.Join("\n", lines, lines.Length - MAX_LOG_LINES, MAX_LOG_LINES) + "\n";
                }
                logLineCount = MAX_LOG_LINES;
            }
            
            Canvas.ForceUpdateCanvases();
            logScroll.verticalNormalizedPosition = 0;
        }
    }
}
