using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;
using UnityEngine;
using XrCode;

namespace XrSDK
{
    public class FirebasePushFrame : BaseModule
    {
        private const string PrefsTokenKey = "fcm_token";

        private readonly FirebasePushConf _conf;
        private bool _initialized;
        private string _fcmToken;
        private string _firebaseUid;

        public FirebasePushFrame(FirebasePushConf conf)
        {
            _conf = conf ?? new FirebasePushConf();
        }

        protected override void OnLoad()
        {
            BindFacade();
            InitFirebase();
        }

        private void BindFacade()
        {
            FacadeFirebasePush.GetFcmToken = () => _fcmToken;
            FacadeFirebasePush.GetFirebaseUid = () => _firebaseUid;
            FacadeFirebasePush.RequestPermission = RequestNotificationPermission;
            FacadeFirebasePush.SubscribeTopic = SubscribeTopic;
            FacadeFirebasePush.UnsubscribeTopic = UnsubscribeTopic;
        }

        private void UnbindFacade()
        {
            FacadeFirebasePush.GetFcmToken = null;
            FacadeFirebasePush.GetFirebaseUid = null;
            FacadeFirebasePush.RequestPermission = null;
            FacadeFirebasePush.SubscribeTopic = null;
            FacadeFirebasePush.UnsubscribeTopic = null;
        }

        private void InitFirebase()
        {
            if (_initialized)
                return;

            _initialized = true;
            Debug.Log("[FirebasePush] BackendMode=" + _conf.backendMode);
            Debug.Log("[FirebasePush] CheckAndFixDependenciesAsync...");

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[FirebasePush] Dependency check failed: " + task.Exception);
                    return;
                }

                DependencyStatus status = task.Result;
                if (status != DependencyStatus.Available)
                {
                    Debug.LogError("[FirebasePush] Dependencies unavailable: " + status);
                    return;
                }

                Debug.Log("[FirebasePush] Dependencies available");
                OnFirebaseReady();
            });
        }

        private void OnFirebaseReady()
        {
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;

            if (_conf.requestPermissionOnStart)
                RequestNotificationPermission();

            if (_conf.backendMode == FirebasePushBackendMode.FirebaseCloud)
                BeginCloudFlow();
            else
                FetchToken();
        }

        private void BeginCloudFlow()
        {
#if FIREBASE_USE_CLOUD_BACKEND
            FirebasePushCloudBackend.EnsureUserThen(_conf, uid =>
            {
                _firebaseUid = uid;
                FetchToken();
            });
#else
            Debug.LogError("[FirebasePush] BackendMode=FirebaseCloud but FIREBASE_USE_CLOUD_BACKEND is not defined. " +
                           "Add scripting define FIREBASE_USE_CLOUD_BACKEND and import FirebaseAuth + FirebaseFirestore, " +
                           "or switch BackendMode to LocalServer.");
            FetchToken();
#endif
        }

        private void FetchToken()
        {
            FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[FirebasePush] GetTokenAsync failed: " + task.Exception);
                    return;
                }

                ApplyToken(task.Result);
            });
        }

        private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            ApplyToken(e.Token);
        }

        private void ApplyToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            bool changed = !string.Equals(_fcmToken, token, StringComparison.Ordinal);
            _fcmToken = token;
            PlayerPrefs.SetString(PrefsTokenKey, token);
            PlayerPrefs.Save();

            Debug.Log("[FirebasePush] FCM Token: " + token);

            if (_conf.subscribeDefaultTopic && !string.IsNullOrEmpty(_conf.defaultTopic))
                SubscribeTopic(_conf.defaultTopic);

            ReportToken(token);

            if (changed)
                FacadeFirebasePush.TokenRefreshed?.Invoke(token);
        }

        private void ReportToken(string token)
        {
            switch (_conf.backendMode)
            {
                case FirebasePushBackendMode.FirebaseCloud:
#if FIREBASE_USE_CLOUD_BACKEND
                    FirebasePushCloudBackend.SaveToken(_conf, token, _firebaseUid);
#else
                    Debug.LogWarning("[FirebasePush] Skip Firestore upload: FIREBASE_USE_CLOUD_BACKEND not defined.");
#endif
                    break;

                case FirebasePushBackendMode.LocalServer:
                    FirebasePushLocalBackend.SaveToken(_conf, token);
                    break;
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            FirebaseMessage msg = e.Message;
            string title = msg.Notification != null ? msg.Notification.Title : null;
            string body = msg.Notification != null ? msg.Notification.Body : null;
            Debug.Log($"[FirebasePush] Message received. Title={title}, Body={body}, From={msg.From}");

            if (msg.Data != null && msg.Data.Count > 0)
            {
                foreach (KeyValuePair<string, string> kv in msg.Data)
                    Debug.Log($"[FirebasePush] data[{kv.Key}]={kv.Value}");
            }

            FacadeFirebasePush.MessageReceived?.Invoke();
        }

        private void RequestNotificationPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string permission = "android.permission.POST_NOTIFICATIONS";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(permission);
                Debug.Log("[FirebasePush] Requested POST_NOTIFICATIONS");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Debug.Log("[FirebasePush] iOS permission is requested by system / Firebase Messaging on device.");
#else
            Debug.Log("[FirebasePush] Permission request skipped in Editor");
#endif
        }

        private void SubscribeTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                return;

            FirebaseMessaging.SubscribeAsync(topic).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogWarning("[FirebasePush] Subscribe failed: " + topic + " / " + task.Exception);
                else
                    Debug.Log("[FirebasePush] Subscribed topic: " + topic);
            });
        }

        private void UnsubscribeTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                return;

            FirebaseMessaging.UnsubscribeAsync(topic).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogWarning("[FirebasePush] Unsubscribe failed: " + topic + " / " + task.Exception);
                else
                    Debug.Log("[FirebasePush] Unsubscribed topic: " + topic);
            });
        }

        internal static string GetPlatformNamePublic()
        {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#else
            return Application.platform.ToString().ToLowerInvariant();
#endif
        }

        protected override void OnDispose()
        {
            if (_initialized)
            {
                FirebaseMessaging.TokenReceived -= OnTokenReceived;
                FirebaseMessaging.MessageReceived -= OnMessageReceived;
            }

            UnbindFacade();
        }
    }
}
