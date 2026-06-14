using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DeepSeek
{
    /// <summary>
    /// Python桥接器
    /// 负责与Python服务通信
    /// 支持两种通信模式：
    /// 1. 标准输入输出模式（进程启动）
    /// 2. 文件监听模式
    /// </summary>
    public class PythonBridge : MonoBehaviour
    {
        [Header("Python配置")]
        [SerializeField] private string pythonScriptPath = "python_server/main.py";
        [SerializeField] private string pythonExePath = "py";
        
        [Header("通信模式")]
        [SerializeField] private CommunicationMode mode = CommunicationMode.StdInOut;
        
        [Header("文件监听配置")]
        [SerializeField] private string inputFilePath = "python_server/data/communication/unity_to_python.json";
        [SerializeField] private string outputFilePath = "python_server/data/communication/python_to_unity.json";
        [SerializeField] private float checkInterval = 0.5f;
        
        [Header("调试")]
        [SerializeField] private bool autoStartPython = true;
        [SerializeField] private bool logMessages = true;
        
        [Header("实时监控")]
        [Tooltip("启动时是否打开Python实时统计终端窗口")]
        [SerializeField] private bool openMonitorWindow = true;
        [SerializeField] private string monitorScriptPath = "python_server/tail_log.py";
        [SerializeField] private string statsLogFilePath = "python_server/data/logs/latest_stats_实时监控.log";
        
        private Process pythonProcess;
        private Process monitorProcess;
        private StreamWriter processStdin;
        private StreamReader processStdout;
        private StreamReader processStderr;
        private bool isRunning = false;
        private Task outputReadTask;
        private Task errorReadTask;
        private CancellationTokenSource cancellationTokenSource;
        private float lastFileCheckTime;
        
        /// <summary>
        /// 通信模式枚举
        /// </summary>
        public enum CommunicationMode
        {
            StdInOut,
            FileWatch
        }
        
        /// <summary>
        /// Python响应事件
        /// </summary>
        public event Action<PythonResponse> OnResponseReceived;
        
        /// <summary>
        /// Python错误事件
        /// </summary>
        public event Action<string> OnError;
        
        /// <summary>
        /// 会话状态
        /// </summary>
        public bool IsRunning => isRunning;
        
        /// <summary>
        /// 是否打开实时统计监控窗口
        /// </summary>
        public bool OpenMonitorWindow
        {
            get => openMonitorWindow;
            set => openMonitorWindow = value;
        }
        
        /// <summary>
        /// 实时统计日志文件路径（供监控窗口使用）
        /// </summary>
        public string StatsLogFile => statsLogFilePath;
        
        private void Start()
        {
            if (autoStartPython)
            {
                StartPython();
            }
        }
        
        private void Update()
        {
            if (mode == CommunicationMode.FileWatch && isRunning)
            {
                if (Time.time - lastFileCheckTime >= checkInterval)
                {
                    CheckOutputFile();
                    lastFileCheckTime = Time.time;
                }
            }
        }
        
        private void OnDestroy()
        {
            StopPython();
        }
        
        #region 进程管理
        
        /// <summary>
        /// 启动Python服务
        /// </summary>
        public void StartPython()
        {
            if (isRunning)
            {
                UnityEngine.Debug.LogWarning("Python服务已经在运行中");
                return;
            }
            
            try
            {
                string projectRoot = Application.dataPath;
                projectRoot = projectRoot.Substring(0, projectRoot.LastIndexOf("/Assets"));
                string scriptFullPath = Path.Combine(projectRoot, pythonScriptPath);
                
                if (!File.Exists(scriptFullPath))
                {
                    UnityEngine.Debug.LogError($"找不到Python脚本: {scriptFullPath}");
                    OnError?.Invoke($"找不到Python脚本: {scriptFullPath}");
                    return;
                }
                
                if (mode == CommunicationMode.StdInOut)
                {
                    StartStdInOutProcess(scriptFullPath);
                }
                else
                {
                    StartFileWatchProcess(scriptFullPath);
                }
                
                isRunning = true;
                UnityEngine.Debug.Log("Python服务启动成功");
                
                if (openMonitorWindow)
                {
                    StartMonitorWindow(projectRoot);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"启动Python服务失败: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }
        
        /// <summary>
        /// 启动Python实时统计监控终端窗口
        /// </summary>
        private void StartMonitorWindow(string projectRoot)
        {
            try
            {
                string monitorFullPath = Path.Combine(projectRoot, monitorScriptPath);
                string logFullPath = Path.Combine(projectRoot, statsLogFilePath);
                
                if (!File.Exists(monitorFullPath))
                {
                    UnityEngine.Debug.LogWarning($"找不到监控脚本: {monitorFullPath}，跳过监控窗口启动");
                    return;
                }
                
                string logDir = Path.GetDirectoryName(logFullPath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                monitorProcess = new Process();
                monitorProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"Python实时统计监视器\" python \"{monitorFullPath}\" \"{logFullPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(monitorFullPath)
                };
                
                monitorProcess.Start();
                UnityEngine.Debug.Log($"Python实时统计终端窗口已启动");
                UnityEngine.Debug.Log($"监控日志文件: {logFullPath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"启动监控窗口失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动标准输入输出模式的Python进程
        /// </summary>

        private string ResolvePythonPath()
        {
            if (!string.IsNullOrEmpty(pythonExePath) && System.IO.File.Exists(pythonExePath))
                return pythonExePath;
            string[] candidates = { "py", "python3", "python", "python.exe" };
            foreach (var c in candidates)
            {
                try { var p = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo { FileName = c, Arguments = "--version", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } }; p.Start(); p.WaitForExit(3000); if (p.ExitCode == 0) return c; }
                catch { }
            }
            string[] paths = { @"C:\WINDOWS\py.exe", @"C:\Windows\py.exe" };
            foreach (var p in paths) if (System.IO.File.Exists(p)) return p;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            try { var found = System.IO.Directory.GetFiles(appData + "\\Programs\\Python", "python.exe", System.IO.SearchOption.AllDirectories); if (found.Length > 0) return found[0]; } catch { }
            return pythonExePath;
        }
        private void StartStdInOutProcess(string scriptPath)
        {
            cancellationTokenSource = new CancellationTokenSource();
            
            pythonProcess = new Process();
            pythonProcess.StartInfo = new ProcessStartInfo
            {
                FileName = ResolvePythonPath(),
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            pythonProcess.EnableRaisingEvents = true;
            pythonProcess.Exited += (sender, e) =>
            {
                isRunning = false;
                UnityEngine.Debug.Log($"Python进程已退出，退出码: {pythonProcess.ExitCode}");
            };
            
            pythonProcess.Start();
            processStdin = pythonProcess.StandardInput;
            processStdin.AutoFlush = true;
            processStdout = pythonProcess.StandardOutput;
            processStderr = pythonProcess.StandardError;
            
            var token = cancellationTokenSource.Token;
            outputReadTask = Task.Run(() => ReadOutputAsync(token), token);
            errorReadTask = Task.Run(() => ReadErrorAsync(token), token);
        }
        
        /// <summary>
        /// 启动文件监听模式
        /// </summary>
        private void StartFileWatchProcess(string scriptPath)
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo = new ProcessStartInfo
            {
                FileName = ResolvePythonPath(),
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };
            
            pythonProcess.EnableRaisingEvents = true;
            pythonProcess.Exited += (sender, e) =>
            {
                isRunning = false;
                UnityEngine.Debug.Log($"Python进程已退出，退出码: {pythonProcess.ExitCode}");
            };
            
            pythonProcess.Start();
            
            string projectRoot = Application.dataPath;
            projectRoot = projectRoot.Substring(0, projectRoot.LastIndexOf("/Assets"));
            string commDir = Path.Combine(projectRoot, Path.GetDirectoryName(inputFilePath));
            if (!Directory.Exists(commDir))
            {
                Directory.CreateDirectory(commDir);
            }
        }
        
        /// <summary>
        /// 停止Python服务（非阻塞版本，不会卡死）
        /// </summary>
        public void StopPython()
        {
            if (!isRunning) return;
            
            UnityEngine.Debug.Log("正在停止Python服务...");
            
            isRunning = false;
            
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"取消异步任务时出错: {ex.Message}");
            }
            
            _ = StopPythonAsync();
        }
        
        /// <summary>
        /// 异步停止Python服务（避免阻塞主线程）
        /// </summary>
        private async Task StopPythonAsync()
        {
            try
            {
                if (pythonProcess != null && !pythonProcess.HasExited)
                {
                    try
                    {
                        pythonProcess.Kill();
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"终止Python进程时出错: {ex.Message}");
                    }
                }
                
                if (outputReadTask != null && !outputReadTask.IsCompleted)
                {
                    try
                    {
                        var completedTask = await Task.WhenAny(outputReadTask, Task.Delay(500));
                        if (completedTask != outputReadTask)
                        {
                            UnityEngine.Debug.LogWarning("输出读取任务超时，强制结束");
                        }
                    }
                    catch { }
                }
                
                if (errorReadTask != null && !errorReadTask.IsCompleted)
                {
                    try
                    {
                        var completedTask = await Task.WhenAny(errorReadTask, Task.Delay(500));
                        if (completedTask != errorReadTask)
                        {
                            UnityEngine.Debug.LogWarning("错误读取任务超时，强制结束");
                        }
                    }
                    catch { }
                }
                
                CleanupResources();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"停止Python服务异常: {ex.Message}");
                CleanupResources();
            }
            
            UnityEngine.Debug.Log("Python服务已停止");
        }
        
        /// <summary>
        /// 清理所有资源
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
            catch { }
            
            try
            {
                processStdin?.Dispose();
                processStdin = null;
            }
            catch { }
            
            try
            {
                processStdout?.Dispose();
                processStdout = null;
            }
            catch { }
            
            try
            {
                processStderr?.Dispose();
                processStderr = null;
            }
            catch { }
            
            try
            {
                pythonProcess?.Dispose();
                pythonProcess = null;
            }
            catch { }
            
            outputReadTask = null;
            errorReadTask = null;
        }
        
        #endregion
        
        #region 异步读取输出
        
        /// <summary>
        /// 异步读取标准输出
        /// </summary>
        private async Task ReadOutputAsync(CancellationToken token)
        {
            char[] buffer = new char[4096];
            StringBuilder messageBuilder = new StringBuilder();
            
            while (!token.IsCancellationRequested && processStdout != null)
            {
                try
                {
                    var readTask = processStdout.ReadAsync(buffer, 0, buffer.Length);
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(-1, token));
                    
                    if (completedTask == readTask)
                    {
                        int read = await readTask;
                        if (read > 0)
                        {
                            string content = new string(buffer, 0, read);
                            
                            foreach (char c in content)
                            {
                                if (c == '\n')
                                {
                                    string line = messageBuilder.ToString().Trim();
                                    messageBuilder.Clear();
                                    
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        ProcessOutputLine(line);
                                    }
                                }
                                else
                                {
                                    messageBuilder.Append(c);
                                }
                            }
                        }
                        else
                        {
                            await Task.Delay(100, token);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("读取输出任务已取消");
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        UnityEngine.Debug.LogError($"读取Python输出错误: {ex.Message}");
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// 异步读取标准错误
        /// </summary>
        private async Task ReadErrorAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && processStderr != null)
            {
                try
                {
                    var readTask = processStderr.ReadLineAsync();
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(-1, token));
                    
                    if (completedTask == readTask)
                    {
                        string line = await readTask;
                        if (!string.IsNullOrEmpty(line))
                        {
                            UnityEngine.Debug.LogWarning($"Python错误输出: {line}");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("读取错误任务已取消");
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        UnityEngine.Debug.LogError($"读取Python错误输出错误: {ex.Message}");
                    }
                    break;
                }
            }
        }
        
        #endregion
        
        #region 消息处理
        
        /// <summary>
        /// 处理输出行
        /// </summary>
        private void ProcessOutputLine(string line)
        {
            if (logMessages)
            {
                UnityEngine.Debug.Log($"Python输出: {line}");
            }
            
            try
            {
                var response = JsonConvert.DeserializeObject<PythonResponse>(line);
                if (response != null)
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        OnResponseReceived?.Invoke(response);
                    });
                }
            }
            catch (JsonException)
            {
                // 非JSON格式的输出，可能是日志信息
            }
        }
        
        /// <summary>
        /// 检查输出文件
        /// </summary>
        private void CheckOutputFile()
        {
            string projectRoot = Application.dataPath;
            projectRoot = projectRoot.Substring(0, projectRoot.LastIndexOf("/Assets"));
            string fullOutputPath = Path.Combine(projectRoot, outputFilePath);
            
            if (!File.Exists(fullOutputPath)) return;
            
            try
            {
                string content = File.ReadAllText(fullOutputPath, Encoding.UTF8);
                if (!string.IsNullOrEmpty(content))
                {
                    ProcessOutputLine(content);
                }
                
                File.Delete(fullOutputPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"读取输出文件出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送请求到Python
        /// </summary>
        public bool SendRequest(PythonRequest request)
        {
            if (!isRunning)
            {
                UnityEngine.Debug.LogError("Python服务未运行");
                return false;
            }
            
            try
            {
                string json = JsonConvert.SerializeObject(request, Formatting.None);
                
                if (logMessages)
                {
                    UnityEngine.Debug.Log($"发送到Python: {json}");
                }
                
                if (mode == CommunicationMode.StdInOut)
                {
                    processStdin.WriteLine(json);
                    processStdin.Flush();
                }
                else
                {
                    string projectRoot = Application.dataPath;
                    projectRoot = projectRoot.Substring(0, projectRoot.LastIndexOf("/Assets"));
                    string fullInputPath = Path.Combine(projectRoot, inputFilePath);
                    
                    File.WriteAllText(fullInputPath, json, Encoding.UTF8);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"发送请求失败: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }
        
        #endregion
        
        #region 便捷方法
        
        /// <summary>
        /// 发送心跳
        /// </summary>
        public void Ping()
        {
            var request = new PythonRequest
            {
                action = "ping"
            };
            SendRequest(request);
        }
        
        /// <summary>
        /// 分析意图
        /// </summary>
        public void ParseIntent(string text, string context = "buying_vegetables")
        {
            var request = new PythonRequest
            {
                action = "parse_intent",
                text = text,
                context = context
            };
            SendRequest(request);
        }
        
        /// <summary>
        /// 记录事件
        /// </summary>
        public void LogEvent(string eventType, Dictionary<string, object> data, string context = "unknown")
        {
            var request = new PythonRequest
            {
                action = "log_event",
                event_type = eventType,
                data = data,
                context = context
            };
            SendRequest(request);
        }
        
        /// <summary>
        /// 获取统计数据
        /// </summary>
        public void GetStats()
        {
            var request = new PythonRequest
            {
                action = "get_stats"
            };
            SendRequest(request);
        }
        
        /// <summary>
        /// 生成报告
        /// </summary>
        public void GenerateReport()
        {
            var request = new PythonRequest
            {
                action = "generate_report"
            };
            SendRequest(request);
        }
        
        /// <summary>
        /// 结束会话
        /// </summary>
        public void EndSession()
        {
            var request = new PythonRequest
            {
                action = "end_session"
            };
            SendRequest(request);
        }
        
        #endregion
    }
    
    #region 数据模型
    
    /// <summary>
    /// 发送给Python的请求
    /// </summary>
    public class PythonRequest
    {
        public string action { get; set; }
        public string audio_path { get; set; }
        public string text { get; set; }
        public string context { get; set; }
        public string event_type { get; set; }
        public Dictionary<string, object> data { get; set; }
        public string format { get; set; }
        public string session_id { get; set; }
    }
    
    /// <summary>
    /// Python返回的响应
    /// </summary>
    public class PythonResponse
    {
        public bool success { get; set; }
        public string action { get; set; }
        public Dictionary<string, object> data { get; set; }
        public string error { get; set; }
        public double timestamp { get; set; }
        
        /// <summary>
        /// 获取指定类型的数据
        /// </summary>
        public T GetData<T>()
        {
            if (data == null) return default(T);
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data));
        }
    }
    
    /// <summary>
    /// 统计数据模型
    /// </summary>
    public class SessionStatsData
    {
        public string session_id { get; set; }
        public double accuracy { get; set; }
        public double total_duration { get; set; }
        public int total_events { get; set; }
        public int correct_events { get; set; }
        public int wrong_events { get; set; }
        public int consecutive_correct { get; set; }
        public int max_consecutive_correct { get; set; }
        public bool should_trigger_feedback { get; set; }
        public Dictionary<string, Dictionary<string, int>> scene_stats { get; set; }
    }
    
    /// <summary>
    /// 意图分析结果
    /// </summary>
    public class IntentResultData
    {
        public string intent { get; set; }
        public Dictionary<string, List<string>> entities { get; set; }
        public double confidence { get; set; }
        public string raw_text { get; set; }
    }
    
    #endregion
    
    #region Unity主线程调度器
    
    /// <summary>
    /// Unity主线程调度器
    /// 用于在非主线程回调中执行Unity API
    /// </summary>
    public static class UnityMainThreadDispatcher
    {
        private static readonly Queue<Action> actionQueue = new Queue<Action>();
        
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            UnityMainThreadDispatcherBehaviour.Create();
        }
        
        /// <summary>
        /// 将操作排入主线程执行
        /// </summary>
        public static void Enqueue(Action action)
        {
            lock (actionQueue)
            {
                actionQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// 尝试出队一个操作
        /// </summary>
        public static bool TryDequeue(out Action action)
        {
            lock (actionQueue)
            {
                if (actionQueue.Count > 0)
                {
                    action = actionQueue.Dequeue();
                    return true;
                }
                action = null;
                return false;
            }
        }
    }
    
    /// <summary>
    /// Unity主线程调度器行为
    /// </summary>
    public class UnityMainThreadDispatcherBehaviour : MonoBehaviour
    {
        private static UnityMainThreadDispatcherBehaviour instance;
        
        public static void Create()
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("UnityMainThreadDispatcher");
                instance = obj.AddComponent<UnityMainThreadDispatcherBehaviour>();
                DontDestroyOnLoad(obj);
            }
        }
        
        private void Update()
        {
            while (UnityMainThreadDispatcher.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"主线程调度异常: {ex.Message}");
                }
            }
        }
    }
    
    #endregion
}
