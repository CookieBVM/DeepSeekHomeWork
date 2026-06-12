using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanDataSyncService : MonoBehaviour
    {
        [SerializeField] private string pythonEndpoint = "http://127.0.0.1:8000/api/digital-human/session";
        [SerializeField] private float requestTimeoutSeconds = 2f;

        private DigitalHumanDataTracker tracker;

        public void Initialize(DigitalHumanDataTracker dataTracker, string endpointOverride = null)
        {
            tracker = dataTracker;
            if (!string.IsNullOrWhiteSpace(endpointOverride))
            {
                pythonEndpoint = endpointOverride;
            }
        }

        public void TrySyncCachedSessions()
        {
            if (tracker == null || string.IsNullOrWhiteSpace(pythonEndpoint))
            {
                return;
            }

            StartCoroutine(SyncCachedSessionsRoutine());
        }

        private IEnumerator SyncCachedSessionsRoutine()
        {
            List<DigitalHumanSessionRecord> cachedSessions = tracker.LoadCachedSessions();
            bool changed = false;

            foreach (var session in cachedSessions)
            {
                if (session.synced)
                {
                    continue;
                }

                yield return PostSession(session, success =>
                {
                    if (success)
                    {
                        session.synced = true;
                        changed = true;
                    }
                });
            }

            if (changed)
            {
                tracker.ReplaceCachedSessions(cachedSessions);
            }
        }

        private IEnumerator PostSession(DigitalHumanSessionRecord session, System.Action<bool> completed)
        {
            string json = JsonConvert.SerializeObject(session);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(pythonEndpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(1, Mathf.RoundToInt(requestTimeoutSeconds));
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                bool ok = request.result == UnityWebRequest.Result.Success &&
                    request.responseCode >= 200 &&
                    request.responseCode < 300;

                if (!ok)
                {
                    Debug.Log($"DigitalHuman sync cached offline: {request.error}");
                }

                completed?.Invoke(ok);
            }
        }
    }
}
