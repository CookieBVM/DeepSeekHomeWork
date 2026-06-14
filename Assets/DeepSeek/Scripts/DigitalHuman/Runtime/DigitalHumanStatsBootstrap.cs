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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            if (initialized) return;

            var existing = FindObjectOfType<DigitalHumanStatsBootstrap>();
            if (existing == null)
            {
                var go = new GameObject("DigitalHumanStatsBootstrap");
                DontDestroyOnLoad(go);
                go.AddComponent<DigitalHumanStatsBootstrap>();
                Debug.Log("[DigitalHumanStatsBootstrap] Auto-created bootstrap GameObject");
            }
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
            if (existing != null)
            {
                initialized = true;
                return;
            }

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
                // Only add if not already present
                if (system.GetComponent<DigitalHumanStatsLogger>() == null)
                {
                    system.AddComponent<DigitalHumanStatsLogger>();
                    Debug.Log("[DigitalHumanStatsBootstrap] Added StatsLogger to DigitalHumanSystem");
                }
            }
            else
            {
                Debug.Log("[DigitalHumanStatsBootstrap] No DigitalHumanSystem found in scene, stats disabled");
            }

            initialized = true;
        }
    }
}