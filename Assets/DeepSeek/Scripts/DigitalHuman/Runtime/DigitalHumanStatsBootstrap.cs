using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    [DefaultExecutionOrder(-100)]
    public class DigitalHumanStatsBootstrap : MonoBehaviour
    {
        private static bool initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            initialized = false;
        }

        private void Awake()
        {
            EnsureStatsLogger();
        }

        private void Start()
        {
            EnsureStatsLogger();
        }

        private static void EnsureStatsLogger()
        {
            if (initialized) return;

            var existing = FindObjectOfType<DigitalHumanStatsLogger>();
            if (existing == null)
            {
                var system = GameObject.Find("DigitalHumanSystem");
                if (system == null)
                {
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (var go in rootObjects)
                    {
                        if (go.GetComponentInChildren<DigitalHumanGameController>() != null)
                        {
                            system = go;
                            break;
                        }
                    }
                }

                if (system != null)
                {
                    var logger = system.AddComponent<DigitalHumanStatsLogger>();
                    initialized = true;
                    Debug.Log("✅ DigitalHumanStatsLogger 已自动添加到场景");
                }
            }
            else
            {
                initialized = true;
            }
        }
    }
}
