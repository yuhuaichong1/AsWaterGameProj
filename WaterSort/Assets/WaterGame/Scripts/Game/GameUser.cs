using UnityEngine;

namespace AsGame.Water
{
    public static class GameUser
    {
        const string LevelKey = "ws_current_level";

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(LevelKey, 1);
            set
            {
                PlayerPrefs.SetInt(LevelKey, value);
                PlayerPrefs.Save();
            }
        }

        public static void NextLevel() => CurrentLevel++;
    }
}
