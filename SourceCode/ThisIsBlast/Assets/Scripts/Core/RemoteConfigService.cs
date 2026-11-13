using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Core
{
    public class RemoteConfigService : MonoBehaviour
    {
        public static RemoteConfigService Instance { get; private set; }

        private readonly Dictionary<string, object> _configs = new Dictionary<string, object>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // A/B test defaults
            _configs["progression_track"] = "track_linear_a";   // "track_linear_a", "track_sawtooth_b", or "track_early_c"
            _configs["bench_merge_mode"] = "simple_accumulate"; // "simple_accumulate" or "tier_evolve"
            _configs["hidden_reveal_trigger"] = "on_hit";       // "on_hit" or "on_adjacent_clear"
            _configs["fail_ad_offer"] = true;                  // whether to show +bullets ad button
            _configs["chest_hit_threshold_mult"] = 1.0f;
            _configs["piggy_gem_per_hit"] = 1;
        }

        public string GetString(string key, string fallback = "")
        {
            return _configs.TryGetValue(key, out var val) ? val.ToString() : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            if (_configs.TryGetValue(key, out var val) && val is bool b) return b;
            return fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            if (_configs.TryGetValue(key, out var val) && val is int i) return i;
            return fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            if (_configs.TryGetValue(key, out var val) && val is float f) return f;
            return fallback;
        }

        public void SetConfigOverride(string key, object value)
        {
            _configs[key] = value;
            Debug.Log($"[RemoteConfig] Override key '{key}' = {value}");
        }
    }
}
