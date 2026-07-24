using System;
using UnityEngine;

namespace XrSDK
{
    [RegisterModule("Firebase Push Module")]
    public class FirebasePushModConfig : BaseModulePendant
    {
        [Header("Backend")]
        [Tooltip("FirebaseCloud：Auth+Firestore；LocalServer：自有服务器。Messaging 两种模式共用。")]
        public FirebasePushBackendMode BackendMode = FirebasePushBackendMode.FirebaseCloud;

        [Header("Common")]
        [Tooltip("启动时是否请求通知权限（Android 13+ / iOS）")]
        public bool RequestPermissionOnStart = false;

        [Tooltip("是否自动订阅默认 Topic（便于 Console 群发）")]
        public bool SubscribeDefaultTopic = true;

        [Tooltip("默认订阅的 Topic 名称")]
        public string DefaultTopic = "all_users";

        [Header("FirebaseCloud")]
        [Tooltip("是否使用 Firebase 匿名登录作为用户标识（需 FIREBASE_USE_CLOUD_BACKEND）")]
        public bool EnableAnonymousAuth = true;

        [Tooltip("是否将 FCM Token 写入 Firestore（需 FIREBASE_USE_CLOUD_BACKEND）")]
        public bool SaveTokenToFirestore = true;

        [Tooltip("Firestore 用户集合名")]
        public string UsersCollection = "users";

        [Header("LocalServer")]
        [Tooltip("自有服务器 Token 注册接口，如 https://api.example.com/push/register")]
        public string LocalTokenRegisterUrl = "";

        [Tooltip("是否在拿到 Token 后自动 POST 到本地服务器")]
        public bool ReportTokenToLocalServer = true;

        public override string ModuleName => "FirebasePush";

        public override void CreateModule()
        {
            FirebasePushConf data = new FirebasePushConf
            {
                backendMode = BackendMode,
                requestPermissionOnStart = RequestPermissionOnStart,
                subscribeDefaultTopic = SubscribeDefaultTopic,
                defaultTopic = string.IsNullOrEmpty(DefaultTopic) ? "all_users" : DefaultTopic,
                enableAnonymousAuth = EnableAnonymousAuth,
                saveTokenToFirestore = SaveTokenToFirestore,
                usersCollection = string.IsNullOrEmpty(UsersCollection) ? "users" : UsersCollection,
                localTokenRegisterUrl = LocalTokenRegisterUrl ?? "",
                reportTokenToLocalServer = ReportTokenToLocalServer
            };

            FirebasePushFrame module = new FirebasePushFrame(data);
            module.Load();
        }
    }
}
