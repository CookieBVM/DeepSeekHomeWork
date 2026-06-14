﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanStatsLogger : MonoBehaviour
    {
        [Header("Python集成")]
        [SerializeField] private bool enablePythonIntegration = true;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool autoStartPython = true;

        private PythonBridge pythonBridge;
        private DigitalHumanDataTracker dataTracker;
        private int lastLoggedInteractionCount = 0;
        private int currentConsecutiveCorrect = 0;
        private int maxConsecutiveCorrect = 0;

        private readonly Dictionary<DigitalHumanModule, ModuleStats> moduleStats =
            new Dictionary<DigitalHumanModule, ModuleStats>();

        private void Awake()
        {
            Debug.Log("🔍 DigitalHumanStatsLogger.Awake 被调用");
        }

        private void Start()
        {
            Debug.Log("🔍 DigitalHumanStatsLogger.Start 被调用");
            SubscribeEvents();

            if (enablePythonIntegration)
            {
                EnsurePythonBridge();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (pythonBridge?.IsRunning == true)
            {
                pythonBridge?.StopPython();
            }
        }

        public void Initialize(DigitalHumanDataTracker tracker)
        {
            dataTracker = tracker;
        }

        private void SubscribeEvents()
        {
            Debug.Log("🔍 DigitalHumanStatsLogger.SubscribeEvents 被调用");
            DigitalHumanEventBus.SessionStarted += OnSessionStarted;
            DigitalHumanEventBus.SessionUpdated += OnSessionUpdated;
            DigitalHumanEventBus.SessionEnded += OnSessionEnded;
            DigitalHumanEventBus.InteractionRecorded += OnInteractionRecorded;
            DigitalHumanEventBus.TaskCompleted += OnTaskCompleted;
            DigitalHumanEventBus.RewardRequested += OnRewardRequested;
            DigitalHumanEventBus.ModuleChanged += OnModuleChanged;
        }

        private void UnsubscribeEvents()
        {
            DigitalHumanEventBus.SessionStarted -= OnSessionStarted;
            DigitalHumanEventBus.SessionUpdated -= OnSessionUpdated;
            DigitalHumanEventBus.SessionEnded -= OnSessionEnded;
            DigitalHumanEventBus.InteractionRecorded -= OnInteractionRecorded;
            DigitalHumanEventBus.TaskCompleted -= OnTaskCompleted;
            DigitalHumanEventBus.RewardRequested -= OnRewardRequested;
            DigitalHumanEventBus.ModuleChanged -= OnModuleChanged;
        }

            private void EnsurePythonBridge()
        {
            pythonBridge = FindObjectOfType<PythonBridge>();
            if (pythonBridge == null)
            {
                var go = new GameObject("PythonBridge");
                pythonBridge = go.AddComponent<PythonBridge>();
                Debug.Log("[StatsLogger] Created new PythonBridge GameObject");
            }
            else
            {
                Debug.Log("[StatsLogger] Found existing PythonBridge");
            }

            pythonBridge.OnResponseReceived += OnPythonResponse;
            pythonBridge.OnError += OnPythonError;

            if (autoStartPython)
            {
                pythonBridge.StartPython();
                Debug.Log("[StatsLogger] Called StartPython, IsRunning=" + pythonBridge.IsRunning);
                
                // Test communication after a short delay
                StartCoroutine(TestPythonConnection());
            }
        }

        private System.Collections.IEnumerator TestPythonConnection()
        {
            yield return new WaitForSeconds(2f);
            if (pythonBridge != null && pythonBridge.IsRunning)
            {
                Debug.Log("[StatsLogger] Python is running, sending test ping...");
                pythonBridge.Ping();
                
                yield return new WaitForSeconds(1f);
                var testData = new Dictionary<string, object>
                {
                    { "test", true },
                    { "timestamp", System.DateTime.Now.ToString("o") },
                    { "source", "Unity_DigitalHumanStatsLogger" }
                };
                pythonBridge.LogEvent("unity_startup_test", testData, "system");
                Debug.Log("[StatsLogger] Test data sent to Python");
            }
            else
            {
                Debug.LogWarning("[StatsLogger] Python NOT running, cannot send test data. IsRunning=" + (pythonBridge?.IsRunning ?? false));
            }
        }private void OnSessionStarted(DigitalHumanSessionRecord session)
        {
            Debug.Log($"🔍 DigitalHumanStatsLogger.OnSessionStarted 被调用, logToConsole={logToConsole}");
            if (!logToConsole)
            {
                Debug.LogWarning("⚠️ logToConsole=false, 跳过输出");
                return;
            }

            currentConsecutiveCorrect = 0;
            maxConsecutiveCorrect = 0;
            moduleStats.Clear();

            string moduleName = GetModuleName(session.module);
            string difficultyName = GetDifficultyName(session.difficulty);

            Debug.Log("");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("                    🎮 新会话开始                    ");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log($"会话ID: {session.sessionId}");
            Debug.Log($"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"模块: {moduleName}");
            Debug.Log($"场景: {session.scenarioId}");
            Debug.Log($"难度: {difficultyName}");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("当前统计:");
            Debug.Log("  总交互数: 0");
            Debug.Log("  正确次数: 0");
            Debug.Log("  错误次数: 0");
            Debug.Log("  正确率: 0%");
            Debug.Log("  连续正确: 0");
            Debug.Log("  最高连续正确: 0");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("");

            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                var data = new Dictionary<string, object>
                {
                    { "session_id", session.sessionId },
                    { "module", moduleName },
                    { "scenario", session.scenarioId },
                    { "difficulty", difficultyName }
                };
                pythonBridge.LogEvent("session_started", data, session.scenarioId);
            }
        }

        private void OnSessionUpdated(DigitalHumanSessionRecord session)
        {
            if (!logToConsole) return;
            if (session.totalInteractionCount <= lastLoggedInteractionCount) return;

            PrintDetailedStats(session);
            lastLoggedInteractionCount = session.totalInteractionCount;

            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                double accuracy = session.totalInteractionCount > 0
                    ? (double)session.correctInteractionCount / session.totalInteractionCount
                    : 0;

                var statsData = new Dictionary<string, object>
                {
                    { "total_interactions", session.totalInteractionCount },
                    { "correct", session.correctInteractionCount },
                    { "wrong", session.totalInteractionCount - session.correctInteractionCount },
                    { "accuracy", accuracy },
                    { "max_consecutive_correct", maxConsecutiveCorrect },
                    { "completed_tasks", session.completedTaskCount },
                    { "duration_seconds", session.durationSeconds }
                };
                pythonBridge.LogEvent("stats_updated", statsData, session.scenarioId);
            }
        }

        private void OnSessionEnded(DigitalHumanSessionRecord session)
        {
            if (!logToConsole) return;

            PrintParentReport(session);

            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                double accuracy = session.totalInteractionCount > 0
                    ? (double)session.correctInteractionCount / session.totalInteractionCount
                    : 0;

                var finalData = new Dictionary<string, object>
                {
                    { "session_id", session.sessionId },
                    { "total_interactions", session.totalInteractionCount },
                    { "correct", session.correctInteractionCount },
                    { "wrong", session.totalInteractionCount - session.correctInteractionCount },
                    { "accuracy", accuracy },
                    { "max_consecutive_correct", maxConsecutiveCorrect },
                    { "completed_tasks", session.completedTaskCount },
                    { "total_duration_seconds", session.durationSeconds }
                };
                pythonBridge.LogEvent("session_ended", finalData, session.scenarioId);
            }
        }

        private void OnInteractionRecorded(DigitalHumanInteractionRecord interaction)
        {
            if (!logToConsole) return;

            if (interaction.correct)
            {
                currentConsecutiveCorrect++;
                maxConsecutiveCorrect = Math.Max(maxConsecutiveCorrect, currentConsecutiveCorrect);
            }
            else
            {
                currentConsecutiveCorrect = 0;
            }

            var module = interaction.module;
            if (!moduleStats.ContainsKey(module))
            {
                moduleStats[module] = new ModuleStats { ModuleName = GetModuleName(module) };
            }
            moduleStats[module].RecordInteraction(interaction.correct);

            string inputModeName = GetInputModeName(interaction.inputMode);
            string participantName = GetParticipantName(interaction.participant);
            string resultStatus = interaction.correct ? "✅ 正确" : "❌ 错误";

            Debug.Log("");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log($"📝 新交互记录 [{DateTime.Now:HH:mm:ss}]");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log($"参与者: {participantName}");
            Debug.Log($"输入方式: {inputModeName}");
            Debug.Log($"输入内容: \"{interaction.input}\"");
            Debug.Log($"数字人回复: \"{interaction.response}\"");
            Debug.Log($"结果: {resultStatus}");
            Debug.Log($"耗时: {interaction.elapsedSeconds:F2}秒");
            Debug.Log($"当前连续正确: {currentConsecutiveCorrect}");
            if (currentConsecutiveCorrect >= 3)
            {
                Debug.Log($"🔥 连胜中! 已连续 {currentConsecutiveCorrect} 次正确");
            }
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log("");

            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                var interactionData = new Dictionary<string, object>
                {
                    { "module", GetModuleName(interaction.module) },
                    { "scenario", interaction.scenarioId },
                    { "participant", participantName },
                    { "input_mode", inputModeName },
                    { "input", interaction.input },
                    { "response", interaction.response },
                    { "is_correct", interaction.correct },
                    { "elapsed_seconds", interaction.elapsedSeconds },
                    { "consecutive_correct", currentConsecutiveCorrect },
                    { "max_consecutive_correct", maxConsecutiveCorrect }
                };
                pythonBridge.LogEvent("interaction_recorded", interactionData, interaction.scenarioId);
            }
        }

        private void OnTaskCompleted(DigitalHumanModule module, string taskId)
        {
            if (!logToConsole) return;

            Debug.Log("");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log($"🎯 任务完成: {taskId} (模块: {GetModuleName(module)})");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log("");
        }

        private void OnRewardRequested(string message, float visibleSeconds)
        {
            if (!logToConsole) return;

            Debug.Log("");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("                    🎉 正反馈触发!                   ");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log($"消息: {message}");
            Debug.Log($"显示时长: {visibleSeconds}秒");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("");
        }

        private void OnModuleChanged(DigitalHumanModule module)
        {
            if (!logToConsole) return;

            Debug.Log("");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log($"🔄 切换到模块: {GetModuleName(module)}");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log("");
        }

        private void OnPythonResponse(DeepSeek.PythonResponse response)
        {
            if (!logToConsole) return;

            if (!response.success)
            {
                Debug.LogWarning($"Python错误: {response.error}");
                return;
            }

            switch (response.action)
            {
                case "get_stats":
                    var stats = response.GetData<DeepSeek.SessionStatsData>();
                    Debug.Log($"📊 Python统计数据 - 正确率: {stats.accuracy:P1}, 总事件: {stats.total_events}");
                    break;
                case "log_event":
                    var data = response.GetData<Dictionary<string, object>>();
                    if (data != null && data.ContainsKey("stats"))
                    {
                        var eventStats = data["stats"] as Dictionary<string, object>;
                        if (eventStats != null && eventStats.ContainsKey("accuracy"))
                        {
                            double acc = Convert.ToDouble(eventStats["accuracy"]);
                            Debug.Log($"Python事件记录成功 - 当前正确率: {acc:P0}");
                        }
                    }
                    break;
            }
        }

        private void OnPythonError(string error)
        {
            Debug.LogWarning($"Python桥接错误: {error}");
        }

        private void PrintDetailedStats(DigitalHumanSessionRecord session)
        {
            int total = session.totalInteractionCount;
            int correct = session.correctInteractionCount;
            int wrong = total - correct;
            double accuracy = total > 0 ? (double)correct / total : 0;
            string accuracyLabel = accuracy >= 0.7 ? "优秀" : accuracy >= 0.4 ? "良好" : "需努力";

            Debug.Log("");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("                    📊 实时统计数据                 ");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log($"总交互数: {total}");
            Debug.Log($"正确次数: {correct}");
            Debug.Log($"错误次数: {wrong}");
            Debug.Log($"正确率: {accuracy * 100:F1}% ({accuracyLabel})");
            Debug.Log($"当前连续正确: {currentConsecutiveCorrect}");
            Debug.Log($"最高连续正确: {maxConsecutiveCorrect}");
            Debug.Log($"已完成任务: {session.completedTaskCount}");
            Debug.Log($"当前时长: {session.durationSeconds:F1}秒");

            if (moduleStats.Count > 0)
            {
                Debug.Log("──────────────────────────────────────────────────");
                Debug.Log("各模块表现:");
                foreach (var kvp in moduleStats)
                {
                    var ms = kvp.Value;
                    double moduleAcc = ms.Total > 0 ? (double)ms.Correct / ms.Total : 0;
                    Debug.Log($"  {ms.ModuleName}: {ms.Correct}/{ms.Total} ({moduleAcc * 100:F1}%)");
                }
            }

            if (currentConsecutiveCorrect >= 3)
            {
                Debug.Log("──────────────────────────────────────────────────");
                Debug.Log($"🔥 连胜中! 连续 {currentConsecutiveCorrect} 次正确!");
            }

            if (accuracy >= 0.8 && correct >= 5)
            {
                Debug.Log("🌟 表现出色! 继续保持!");
            }

            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("");
        }

        private void PrintParentReport(DigitalHumanSessionRecord session)
        {
            int total = session.totalInteractionCount;
            int correct = session.correctInteractionCount;
            int wrong = total - correct;
            double accuracy = total > 0 ? (double)correct / total : 0;
            int totalMinutes = (int)session.durationSeconds / 60;
            int totalSeconds = (int)session.durationSeconds % 60;

            var strengths = IdentifyStrengths(session);
            var areasToImprove = IdentifyAreasToImprove(session);

            Debug.Log("");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("                    📋 宝宝游戏报告 (家长端)          ");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log($"报告日期: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"会话ID: {session.sessionId}");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log("【游戏总结】");
            Debug.Log("──────────────────────────────────────────────────");
            string summary = GenerateSummary(session, accuracy, correct, totalMinutes);
            Debug.Log(summary);
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log("【详细数据】");
            Debug.Log("──────────────────────────────────────────────────");
            Debug.Log($"总正确率: {accuracy * 100:F1}%");
            Debug.Log($"游戏时长: {totalMinutes} 分 {totalSeconds} 秒");
            Debug.Log($"正确互动次数: {correct}");
            Debug.Log($"需要改进的互动: {wrong}");
            Debug.Log($"最高连续正确: {maxConsecutiveCorrect} 次");
            Debug.Log($"完成任务数: {session.completedTaskCount}");

            if (moduleStats.Count > 0)
            {
                Debug.Log("──────────────────────────────────────────────────");
                Debug.Log("【各模块表现】");
                Debug.Log("──────────────────────────────────────────────────");
                foreach (var kvp in moduleStats)
                {
                    var ms = kvp.Value;
                    double moduleAcc = ms.Total > 0 ? (double)ms.Correct / ms.Total : 0;
                    Debug.Log($"{ms.ModuleName}: {ms.Correct}/{ms.Total} ({moduleAcc * 100:F1}%)");
                }
            }

            if (strengths.Count > 0)
            {
                Debug.Log("──────────────────────────────────────────────────");
                Debug.Log("【宝宝的优点】✨");
                Debug.Log("──────────────────────────────────────────────────");
                for (int i = 0; i < strengths.Count; i++)
                {
                    Debug.Log($"{i + 1}. {strengths[i]}");
                }
            }

            if (areasToImprove.Count > 0)
            {
                Debug.Log("──────────────────────────────────────────────────");
                Debug.Log("【继续加油的方向】💪");
                Debug.Log("──────────────────────────────────────────────────");
                for (int i = 0; i < areasToImprove.Count; i++)
                {
                    Debug.Log($"{i + 1}. {areasToImprove[i]}");
                }
            }

            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("                    宝宝真棒! 继续加油哦! 🌟         ");
            Debug.Log("══════════════════════════════════════════════════");
            Debug.Log("");
        }

        private string GenerateSummary(DigitalHumanSessionRecord session, double accuracy,
                                        int correctCount, int durationMinutes)
        {
            var parts = new List<string> { "宝宝今天在游戏中表现很棒!" };

            if (accuracy >= 0.8)
            {
                parts.Add($"正确率达到了 {accuracy * 100:F0}%，完成了 {correctCount} 次正确的互动。");
            }
            else if (accuracy >= 0.6)
            {
                parts.Add($"正确率为 {accuracy * 100:F0}%，完成了 {correctCount} 次正确的互动，继续加油！");
            }
            else if (accuracy >= 0.4)
            {
                parts.Add($"正确率为 {accuracy * 100:F0}%，完成了 {correctCount} 次正确互动，多多练习会更好哦！");
            }
            else
            {
                parts.Add($"今天完成了 {correctCount} 次互动，让我们一起加油，下次会更好！");
            }

            if (maxConsecutiveCorrect >= 5)
            {
                parts.Add($"特别棒! 最高连续正确 {maxConsecutiveCorrect} 次!");
            }
            else if (maxConsecutiveCorrect >= 3)
            {
                parts.Add($"很好! 最高连续正确 {maxConsecutiveCorrect} 次。");
            }

            if (durationMinutes >= 10)
            {
                parts.Add($"今天玩了 {durationMinutes} 分钟，非常专注！");
            }
            else if (durationMinutes >= 5)
            {
                parts.Add($"今天玩了 {durationMinutes} 分钟，表现不错！");
            }
            else
            {
                parts.Add($"今天玩了 {durationMinutes} 分钟，下次可以多玩一会儿。");
            }

            return string.Join(" ", parts);
        }

        private List<string> IdentifyStrengths(DigitalHumanSessionRecord session)
        {
            var strengths = new List<string>();
            double accuracy = session.totalInteractionCount > 0
                ? (double)session.correctInteractionCount / session.totalInteractionCount
                : 0;

            if (accuracy >= 0.7)
            {
                strengths.Add("整体表现优秀，能够准确完成互动任务");
            }

            if (maxConsecutiveCorrect >= 5)
            {
                strengths.Add("能够持续专注，保持良好的表现状态");
            }

            foreach (var kvp in moduleStats)
            {
                var ms = kvp.Value;
                double moduleAcc = ms.Total > 0 ? (double)ms.Correct / ms.Total : 0;
                if (moduleAcc >= 0.8 && ms.Total >= 3)
                {
                    strengths.Add($"在{ms.ModuleName}中表现特别出色");
                }
            }

            if (session.durationSeconds >= 600)
            {
                strengths.Add("能够长时间保持专注力");
            }

            if (strengths.Count == 0)
            {
                strengths.Add("正在积极参与游戏互动");
            }

            return strengths;
        }

        private List<string> IdentifyAreasToImprove(DigitalHumanSessionRecord session)
        {
            var areas = new List<string>();
            double accuracy = session.totalInteractionCount > 0
                ? (double)session.correctInteractionCount / session.totalInteractionCount
                : 0;

            if (accuracy < 0.5)
            {
                areas.Add("可以多加练习，提高互动准确率");
            }

            foreach (var kvp in moduleStats)
            {
                var ms = kvp.Value;
                double moduleAcc = ms.Total > 0 ? (double)ms.Correct / ms.Total : 0;
                if (moduleAcc < 0.4 && ms.Total >= 2)
                {
                    areas.Add($"建议多加练习{ms.ModuleName}");
                }
            }

            if (session.durationSeconds < 180)
            {
                areas.Add("可以尝试延长游戏时间，保持更长时间的专注");
            }

            return areas;
        }

        public void LogCustomEvent(string eventType, Dictionary<string, object> data,
                                    string context = "unknown")
        {
            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                pythonBridge.LogEvent(eventType, data, context);
            }
        }

        public void RequestPythonStats()
        {
            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                pythonBridge.GetStats();
            }
        }

        public void EndPythonSession()
        {
            if (enablePythonIntegration && pythonBridge?.IsRunning == true)
            {
                pythonBridge.EndSession();
            }
        }

        private static string GetModuleName(DigitalHumanModule module)
        {
            return module switch
            {
                DigitalHumanModule.DeepSeekChat => "DeepSeek聊天",
                DigitalHumanModule.InterpersonalCommunication => "人际交流(买菜)",
                DigitalHumanModule.ParentChildColoring => "亲子涂色",
                DigitalHumanModule.ActionImitation => "动作模仿",
                _ => module.ToString()
            };
        }

        private static string GetDifficultyName(DigitalHumanDifficulty difficulty)
        {
            return difficulty switch
            {
                DigitalHumanDifficulty.Beginner => "初级",
                DigitalHumanDifficulty.Intermediate => "进阶",
                DigitalHumanDifficulty.Advanced => "高级",
                _ => difficulty.ToString()
            };
        }

        private static string GetInputModeName(DigitalHumanInputMode mode)
        {
            return mode switch
            {
                DigitalHumanInputMode.Option => "选项选择",
                DigitalHumanInputMode.Text => "文本输入",
                DigitalHumanInputMode.Voice => "语音识别",
                _ => mode.ToString()
            };
        }

        private static string GetParticipantName(DigitalHumanParticipant participant)
        {
            return participant switch
            {
                DigitalHumanParticipant.Child => "孩子",
                DigitalHumanParticipant.Parent => "家长",
                DigitalHumanParticipant.System => "系统",
                _ => participant.ToString()
            };
        }

        private class ModuleStats
        {
            public string ModuleName { get; set; }
            public int Total { get; private set; }
            public int Correct { get; private set; }
            public int Wrong => Total - Correct;

            public void RecordInteraction(bool isCorrect)
            {
                Total++;
                if (isCorrect) Correct++;
            }
        }
    }
}
