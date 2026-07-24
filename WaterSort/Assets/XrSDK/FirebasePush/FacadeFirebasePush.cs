using System;

namespace XrSDK
{
    public static class FacadeFirebasePush
    {
        public static Func<string> GetFcmToken;
        public static Func<string> GetFirebaseUid;
        public static Action RequestPermission;
        public static Action<string> SubscribeTopic;
        public static Action<string> UnsubscribeTopic;
        public static Action MessageReceived;
        public static Action<string> TokenRefreshed;

        /// <summary>
        /// LocalServer 模式下可选接管上报（不为 null 时优先于内置 HTTP）。
        /// 参数：fcmToken
        /// </summary>
        public static Action<string> UploadTokenToLocalServer;
    }
}
