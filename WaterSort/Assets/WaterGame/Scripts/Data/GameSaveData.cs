using System.Collections.Generic;
using UnityEngine;
using AsGame.Core;
using XrCode;

namespace AsGame.Data
{
    /// <summary>本地存档，对应 Cocos GameUser + GameData 常用字段。</summary>
    public static class GameSaveData
    {
        const string Prefix = "ws_";

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(Prefix + "level", 1);
            set { PlayerPrefs.SetInt(Prefix + "level", value); Save(); }
        }

        public static bool Bgm
        {
            get => PlayerPrefs.GetInt(Prefix + "bgm", 1) == 1;
            set { PlayerPrefs.SetInt(Prefix + "bgm", value ? 1 : 0); Save(); }
        }

        public static bool Sfx
        {
            get => PlayerPrefs.GetInt(Prefix + "sfx", 1) == 1;
            set { PlayerPrefs.SetInt(Prefix + "sfx", value ? 1 : 0); Save(); }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(Prefix + "vib", 1) == 1;
            set { PlayerPrefs.SetInt(Prefix + "vib", value ? 1 : 0); Save(); }
        }

        public static int Heart
        {
            get => PlayerPrefs.GetInt(Prefix + "heart", GameConstants.MaxHeart);
            set { PlayerPrefs.SetInt(Prefix + "heart", Mathf.Clamp(value, 0, 99)); Save(); }
        }

        public static long HeartRecoverTimestamp
        {
            get => long.Parse(PlayerPrefs.GetString(Prefix + "heart_ts", "0"));
            set { PlayerPrefs.SetString(Prefix + "heart_ts", value.ToString()); Save(); }
        }

        public static long InfiniteHeartEndTimestamp
        {
            get => long.Parse(PlayerPrefs.GetString(Prefix + "heart_inf", "0"));
            set { PlayerPrefs.SetString(Prefix + "heart_inf", value.ToString()); Save(); }
        }

        public static int HeartInfiniteVideoCount
        {
            get => PlayerPrefs.GetInt(Prefix + "heart_vid", 0);
            set { PlayerPrefs.SetInt(Prefix + "heart_vid", value); Save(); }
        }

        public static bool DailyHeartShown
        {
            get => PlayerPrefs.GetInt(Prefix + "daily_heart", 0) == 1;
            set { PlayerPrefs.SetInt(Prefix + "daily_heart", value ? 1 : 0); Save(); }
        }

        public static bool CollectRed
        {
            get => PlayerPrefs.GetInt(Prefix + "collect_red", 1) == 1;
            set { PlayerPrefs.SetInt(Prefix + "collect_red", value ? 1 : 0); Save(); }
        }

        public static int CollectLevel
        {
            get => PlayerPrefs.GetInt(Prefix + "collect_lv", 0);
            set { PlayerPrefs.SetInt(Prefix + "collect_lv", value); Save(); }
        }

        public static string PlayerId
        {
            get
            {
                var id = PlayerPrefs.GetString(Prefix + "pid", "");
                if (string.IsNullOrEmpty(id))
                {
                    id = System.Guid.NewGuid().ToString("N").Substring(0, 12);
                    PlayerPrefs.SetString(Prefix + "pid", id);
                    Save();
                }

                return id;
            }
        }

        public static int GetPropCount(GameProp prop) =>
            PlayerPrefs.GetInt(Prefix + "prop_" + (int)prop, prop == GameProp.Shuffle ? 1 : 0);

        public static void SetPropCount(GameProp prop, int count)
        {
            PlayerPrefs.SetInt(Prefix + "prop_" + (int)prop, Mathf.Max(0, count));
            Save();
        }

        public static bool IsNewPlayUnlocked(int key) =>
            PlayerPrefs.GetInt(Prefix + "newplay_" + key, 0) == 1;

        public static void SetNewPlayUnlocked(int key)
        {
            PlayerPrefs.SetInt(Prefix + "newplay_" + key, 1);
            Save();
        }

        public static bool HasInfiniteHeart() =>
            InfiniteHeartEndTimestamp > System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static void SetFullHeart()
        {
            Heart = GameConstants.MaxHeart;
            HeartRecoverTimestamp = 0;
        }

        public static void SetInfiniteHeart(int seconds)
        {
            InfiniteHeartEndTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;
            Heart = GameConstants.MaxHeart;
        }

        public static bool UseHeart(int amount = 1)
        {
            if (HasInfiniteHeart()) return true;
            if (Heart < amount) return false;
            Heart -= amount;
            if (Heart < GameConstants.MaxHeart && HeartRecoverTimestamp == 0)
                HeartRecoverTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        public static void TickHeartRecovery()
        {
            if (HasInfiniteHeart() || Heart >= GameConstants.MaxHeart) return;
            if (HeartRecoverTimestamp <= 0) return;
            var now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var elapsed = now - HeartRecoverTimestamp;
            var gained = (int)(elapsed / GameConstants.HeartIntervalTime);
            if (gained <= 0) return;
            Heart = Mathf.Min(GameConstants.MaxHeart, Heart + gained);
            HeartRecoverTimestamp = Heart >= GameConstants.MaxHeart ? 0 : now;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
