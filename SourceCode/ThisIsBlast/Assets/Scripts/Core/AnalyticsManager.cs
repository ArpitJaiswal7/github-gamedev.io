using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Core
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            string paramStr = "";
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    paramStr += $" {kvp.Key}:{kvp.Value}";
                }
            }
            Debug.Log($"<color=#38B000>[Analytics] Event: {eventName}</color>{paramStr}");
        }

        public void LogLevelStart(int levelId, int tier)
        {
            LogEvent("level_start", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "tier", tier },
                { "time", DateTime.UtcNow.ToString("o") }
            });
        }

        public void LogLevelComplete(int levelId, int shotsUsed, int gemsEarned, int merges)
        {
            LogEvent("level_complete", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "shots_used", shotsUsed },
                { "gems_earned", gemsEarned },
                { "merges_count", merges }
            });
        }

        public void LogLevelFail(int levelId, string reason, int remainingEnemies)
        {
            LogEvent("level_fail", new Dictionary<string, object>
            {
                { "level_id", levelId },
                { "reason", reason },
                { "remaining_enemies", remainingEnemies }
            });
        }
    }
}
