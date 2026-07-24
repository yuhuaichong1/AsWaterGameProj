namespace XrSDK
{
    /// <summary>
    /// Token 上报后端模式。Messaging 收发推送与模式无关。
    /// </summary>
    public enum FirebasePushBackendMode
    {
        /// <summary>匿名登录 + 写入 Firestore（需导入 Auth/Firestore，并定义 FIREBASE_USE_CLOUD_BACKEND）</summary>
        FirebaseCloud = 0,

        /// <summary>把 Token HTTP 上报到自有服务器（只需 Messaging，不依赖 Auth/Firestore）</summary>
        LocalServer = 1,
    }
}
