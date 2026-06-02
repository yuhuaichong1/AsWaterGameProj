using UnityEngine;
using AsGame.Data;

namespace AsGame.Platform
{
    /// <summary>Google Play 用平台能力：震动、统计占位。不含微信/分享/小游戏 API。</summary>
    public static class PlatformService
    {
        public static bool VibrationEnabled
        {
            get => GameSaveData.Vibration;
            set => GameSaveData.Vibration = value;
        }

        public static void VibrateShort()
        {
            if (!VibrationEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public static void SendEvent(string eventName, string param = null)
        {
            Debug.Log($"[Analytics] {eventName} {param}");
            // TODO: Firebase Analytics / 你们的数据上报
        }

        public static void SetClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
        }
    }
}
