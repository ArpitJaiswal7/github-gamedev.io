#if UNITY_EDITOR
namespace ArgosLocalization.Editor
{
    /// <summary>
    /// Demo English strings used when the open scene has no TMP_Text to harvest, and by the demo scene builder.
    /// Keys are stable so the bootstrap stays idempotent.
    /// </summary>
    public static class ArgosDemoData
    {
        public static readonly (string Key, string EnglishText)[] Entries =
        {
            ("UI_GAME_TITLE",       "Maze Adventure"),
            ("UI_PLAY_GAME",        "Play Game"),
            ("UI_CONTINUE",         "Continue"),
            ("UI_SETTINGS",         "Settings"),
            ("UI_HIGH_SCORE",       "High Score"),
            ("UI_LEADERBOARD",      "Leaderboard"),
            ("UI_LANGUAGE",         "Select Language"),
            ("UI_SOUND_ON",         "Sound On"),
            ("UI_MUSIC_VOLUME",     "Music Volume"),
            ("UI_PAUSE",            "Game Paused"),
            ("UI_RESUME",           "Resume"),
            ("UI_RESTART",          "Restart Level"),
            ("UI_LEVEL_COMPLETE",   "Level Complete!"),
            ("UI_GAME_OVER",        "Game Over"),
            ("UI_TRY_AGAIN",        "Try Again"),
            ("UI_LOADING",          "Loading, please wait..."),
            ("UI_WATCH_AD",         "Watch a video to get a free reward"),
            ("UI_EXIT_CONFIRM",     "Are you sure you want to quit?"),
        };
    }
}
#endif
