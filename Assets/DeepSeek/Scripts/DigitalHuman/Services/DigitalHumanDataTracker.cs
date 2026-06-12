using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public class DigitalHumanDataTracker : MonoBehaviour
    {
        [SerializeField] private bool cacheSessionsLocally = true;

        private readonly Stopwatch stopwatch = new Stopwatch();
        private DigitalHumanSessionRecord currentSession;
        private int completedTaskCount;

        public DigitalHumanSessionRecord CurrentSession => currentSession;

        public DigitalHumanSessionRecord StartSession(
            DigitalHumanModule module,
            string scenarioId,
            DigitalHumanDifficulty difficulty)
        {
            stopwatch.Restart();
            completedTaskCount = 0;
            currentSession = new DigitalHumanSessionRecord
            {
                sessionId = Guid.NewGuid().ToString("N"),
                module = module,
                scenarioId = scenarioId,
                difficulty = difficulty,
                startedAtUtc = DateTime.UtcNow.ToString("o"),
                synced = false
            };

            DigitalHumanEventBus.PublishSessionStarted(currentSession);
            return currentSession;
        }

        public void RecordInteraction(
            DigitalHumanModule module,
            string scenarioId,
            DigitalHumanParticipant participant,
            DigitalHumanInputMode inputMode,
            string input,
            DigitalHumanResponse response)
        {
            if (currentSession == null)
            {
                StartSession(module, scenarioId, DigitalHumanDifficulty.Beginner);
            }

            var interaction = new DigitalHumanInteractionRecord
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                module = module,
                scenarioId = scenarioId,
                participant = participant,
                inputMode = inputMode,
                input = input,
                response = response.Line,
                correct = response.IsCorrect,
                elapsedSeconds = (float)stopwatch.Elapsed.TotalSeconds
            };

            currentSession.interactions.Add(interaction);
            currentSession.totalInteractionCount++;
            if (response.IsCorrect)
            {
                currentSession.correctInteractionCount++;
            }

            currentSession.durationSeconds = (float)stopwatch.Elapsed.TotalSeconds;
            DigitalHumanEventBus.PublishInteractionRecorded(interaction);
            DigitalHumanEventBus.PublishSessionUpdated(currentSession);
        }

        public void MarkTaskCompleted(string note)
        {
            if (currentSession == null)
            {
                return;
            }

            completedTaskCount++;
            currentSession.completedTaskCount = completedTaskCount;
            if (!string.IsNullOrWhiteSpace(note))
            {
                currentSession.notes.Add(note);
            }

            DigitalHumanEventBus.PublishSessionUpdated(currentSession);
        }

        public DigitalHumanSessionRecord EndSession()
        {
            if (currentSession == null)
            {
                return null;
            }

            stopwatch.Stop();
            currentSession.endedAtUtc = DateTime.UtcNow.ToString("o");
            currentSession.durationSeconds = (float)stopwatch.Elapsed.TotalSeconds;

            if (cacheSessionsLocally)
            {
                SaveSessionToCache(currentSession);
            }

            DigitalHumanEventBus.PublishSessionEnded(currentSession);
            return currentSession;
        }

        public List<DigitalHumanSessionRecord> LoadCachedSessions()
        {
            string path = GetCachePath();
            if (!File.Exists(path))
            {
                return new List<DigitalHumanSessionRecord>();
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<List<DigitalHumanSessionRecord>>(json)
                    ?? new List<DigitalHumanSessionRecord>();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"DigitalHuman cache read failed: {ex.Message}");
                return new List<DigitalHumanSessionRecord>();
            }
        }

        public void ReplaceCachedSessions(List<DigitalHumanSessionRecord> sessions)
        {
            string path = GetCachePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(sessions, Formatting.Indented));
        }

        private void SaveSessionToCache(DigitalHumanSessionRecord session)
        {
            var sessions = LoadCachedSessions();
            int existingIndex = sessions.FindIndex(item => item.sessionId == session.sessionId);
            if (existingIndex >= 0)
            {
                sessions[existingIndex] = session;
            }
            else
            {
                sessions.Add(session);
            }

            ReplaceCachedSessions(sessions);
        }

        private static string GetCachePath()
        {
            return Path.Combine(Application.persistentDataPath, "digital_human_sessions.json");
        }
    }
}
