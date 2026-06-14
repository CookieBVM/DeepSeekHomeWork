using UnityEngine;

namespace DeepSeek
{
    /// <summary>
    /// 游戏启动时自动创建PythonBridge并启动Python服务
    /// 不依赖DigitalHuman系统，任何场景都生效
    /// </summary>
    public static class PythonAutoStart
    {
        private static bool started = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            if (started) return;
            started = true;

            var existing = Object.FindObjectOfType<PythonBridge>();
            if (existing != null)
            {
                Debug.Log("[PythonAutoStart] PythonBridge already exists in scene");
                if (!existing.IsRunning) existing.StartPython();
                return;
            }

            var go = new GameObject("PythonBridge");
            Object.DontDestroyOnLoad(go);
            var bridge = go.AddComponent<PythonBridge>();
            Debug.Log("[PythonAutoStart] PythonBridge created and starting...");
            bridge.StartPython();
        }
    }
}