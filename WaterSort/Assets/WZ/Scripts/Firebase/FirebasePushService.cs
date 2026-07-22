using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Messaging;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
using Unity.Notifications;
#endif

namespace WZSDK
{
    /// <summary>
    /// Bootstraps Firebase Cloud Messaging and exposes simple hooks for other systems.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class FirebasePushService : MonoBehaviour
    {
        public static FirebasePushService Instance { get; private set; }

        [Header("Firebase 设置")]
        [SerializeField] private bool requestPermissionOnStart = true;
        [SerializeField] private bool logVerbose = true;

        [Header("通知显示设置")]
        [SerializeField] private bool showForegroundNotification = true;
        [SerializeField] private string notificationChannelId = "firebase_default";
        [SerializeField] private string notificationChannelName = "Firebase 通知";
        [SerializeField] private string notificationChannelDescription = "接收游戏推送通知";
        [SerializeField] private string smallIconId = "icon_small";

        private Task<bool> _initializationTask;
        private bool _isReady;
        private string _cachedToken;
        private bool _notificationChannelRegistered;

        public bool IsReady => _isReady;
        public string CurrentRegistrationToken => _cachedToken;

        public event Action<string> TokenRefreshed;
        public event Action<FirebaseMessage> ForegroundMessageReceived;

        /// <summary>
        /// 通知点击事件 (title, body, intentData)
        /// </summary>
        public event Action<string, string, string> OnNotificationClicked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

            if (Instance != null) return;

            var existing = FindObjectOfType<FirebasePushService>();
            if (existing != null)
            {
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            var go = new GameObject(nameof(FirebasePushService));
            go.AddComponent<FirebasePushService>();
        }

        public static void EnsureInstanceForPaidUser()
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

            Bootstrap();
        }

        private void Awake()
        {
            if (!ShouldEnableFirebasePush())
            {
                Destroy(gameObject);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

            await InitializeAsync();
            CheckNotificationOpenedApp();
        }

        public Task<bool> InitializeAsync()
        {
            if (!ShouldEnableFirebasePush())
            {
                return Task.FromResult(false);
            }

            if (_initializationTask != null)
                return _initializationTask;

            _initializationTask = InitializeInternalAsync();
            return _initializationTask;
        }

        private async Task<bool> InitializeInternalAsync()
        {
            try
            {
                if (!ShouldEnableFirebasePush())
                {
                    return false;
                }

                if (logVerbose) Debug.Log("[FirebasePush] 开始初始化...");

#if UNITY_ANDROID && !UNITY_EDITOR && UNITY_NOTIFICATIONS_VERSION_2_0_OR_NEWER
                try
                {

                    var settingsType = Type.GetType("Unity.Notifications.NotificationSettings, Unity.Notifications");
                    if (settingsType != null)
                    {
                        var androidSettingsProperty = settingsType.GetProperty("AndroidSettings");
                        if (androidSettingsProperty != null)
                        {
                            var androidSettings = androidSettingsProperty.GetValue(null);
                            var useCustomActivityProp = androidSettings.GetType().GetProperty("UseCustomActivity");
                            var customActivityStringProp = androidSettings.GetType().GetProperty("CustomActivityString");
                            
                            if (useCustomActivityProp != null && customActivityStringProp != null)
                            {
                                useCustomActivityProp.SetValue(androidSettings, true);
                                customActivityStringProp.SetValue(androidSettings, "com.google.firebase.MessagingUnityPlayerActivity");
                                
                                if (logVerbose)
                                {
                                    Debug.Log("[FirebasePush] Custom Activity 已设置: com.google.firebase.MessagingUnityPlayerActivity");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebasePush] 设置 Custom Activity 失败（可能需要手动在 Project Settings 中配置）: {ex.Message}");
                }
#elif UNITY_ANDROID && !UNITY_EDITOR

              
#endif

                // 2. 检查并修复依赖
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"[FirebasePush] Firebase 依赖不可用: {dependencyStatus}");
                    return false;
                }

                // 3. 获取默认实例
                FirebaseApp app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    Debug.LogError("[FirebasePush] 无法创建默认 FirebaseApp 实例");
                    return false;
                }

                // 4. 注册事件
                FirebaseMessaging.TokenReceived += HandleTokenReceived;
                FirebaseMessaging.MessageReceived += HandleMessageReceived;

                // 5. iOS 权限请求
#if UNITY_IOS
                if (requestPermissionOnStart)
                {
                    var status = await FirebaseMessaging.RequestPermissionAsync();
                    if (logVerbose)
                    {
                        Debug.Log($"[FirebasePush] iOS 通知权限状态: {status}");
                    }
                }
#endif

                // 6. Android 权限 + 通知渠道
#if UNITY_ANDROID && !UNITY_EDITOR
                if (requestPermissionOnStart)
                {
                    RequestAndroidNotificationPermission();
                    RegisterNotificationChannel();
                }
#endif

                // 7. 获取 Token
                await FetchTokenAsync();

                _isReady = true;
                if (logVerbose)
                {
                    Debug.Log("[FirebasePush] ✅ Firebase Cloud Messaging 初始化完成");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebasePush] 初始化失败: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 注册 Android 通知渠道
        /// </summary>
        private void RegisterNotificationChannel()
        {
            if (_notificationChannelRegistered) return;

            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                AndroidNotificationCenter.Initialize();
                
                    var channel = new Unity.Notifications.Android.AndroidNotificationChannel
                {
                    Id = notificationChannelId,
                    Name = notificationChannelName,
                    Description = notificationChannelDescription,
                    Importance = Importance.High,
                    EnableVibration = true,
                    EnableLights = true,
                    //LockScreenVisibility = LockScreenVisibility.Public
                };
                
                AndroidNotificationCenter.RegisterNotificationChannel(channel);
                _notificationChannelRegistered = true;
                
                if (logVerbose)
                {
                    Debug.Log($"[FirebasePush] 通知渠道注册成功: {notificationChannelId}");
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebasePush] 通知渠道注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示本地通知（前台推送时使用）
        /// </summary>
        public void ShowLocalNotification(string title, string body, string intentData = "", int delayMs = 100)
        {
            if (!showForegroundNotification)
            {
                if (logVerbose) Debug.Log("[FirebasePush] 前台通知已禁用，跳过显示");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_notificationChannelRegistered)
            {
                RegisterNotificationChannel();
            }
            
            try
            {
                var notification = new AndroidNotification
                {
                    Title = title,
                    Text = body,
                    FireTime = DateTime.Now.AddMilliseconds(delayMs),
                    ShouldAutoCancel = true,
                    ShowTimestamp = true,
                    SmallIcon = smallIconId,
                    IntentData = intentData
                };
                
                int notificationId = AndroidNotificationCenter.SendNotification(notification, notificationChannelId);
                
                if (logVerbose)
                {
                    Debug.Log($"[FirebasePush] 前台通知已显示: {title}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebasePush] 发送本地通知失败: {ex.Message}");
            }
#elif UNITY_IOS
            if (logVerbose)
            {
                Debug.Log($"[FirebasePush] iOS 前台通知: {title} - {body}");
            }
#else
            if (logVerbose)
            {
                Debug.Log($"[FirebasePush] 编辑器模式，不显示通知: {title} - {body}");
            }
#endif
        }

        /// <summary>
        /// 检查并处理从通知点击打开应用的情况
        /// </summary>
        private void CheckNotificationOpenedApp()
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var notificationIntent = AndroidNotificationCenter.GetLastNotificationIntent();
                if (notificationIntent != null)
                {
                    Debug.Log($"[FirebasePush] 应用从通知打开: Title={notificationIntent.Notification.Title}");
                    OnNotificationClicked?.Invoke(
                        notificationIntent.Notification.Title,
                        notificationIntent.Notification.Text,
                        notificationIntent.Notification.IntentData
                    );
                }
                else if (TryGetFirebaseIntentData(out string title, out string body, out string payload))
                {
                    Debug.Log($"[FirebasePush] 应用从 Firebase 通知打开: Title={title}");
                    OnNotificationClicked?.Invoke(title, body, payload);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebasePush] 获取上次通知意图失败: {ex.Message}");
            }
#endif
        }

        private async Task FetchTokenAsync()
        {
            try
            {
                var token = await FirebaseMessaging.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogWarning("[FirebasePush] GetTokenAsync 返回空，请稍后重试");
                    return;
                }
                UpdateCachedToken(token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebasePush] GetTokenAsync 失败: {ex}");
            }
        }

        private void HandleTokenReceived(object sender, TokenReceivedEventArgs eventArgs)
        {
            UpdateCachedToken(eventArgs.Token);
        }

        private void HandleMessageReceived(object sender, MessageReceivedEventArgs eventArgs)
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

            var message = eventArgs.Message;
            if (message == null) return;

            string title = message.Notification?.Title ?? GetMessageDataValue(message, "title", "新消息");
            string body = message.Notification?.Body ?? GetMessageDataValue(message, "body", "你收到一条推送");
            string intentData = BuildIntentData(message);

            Debug.Log($"[FirebasePush] 收到推送 | 标题: {title} | 内容: {body} | 前台: {Application.isFocused}");

            if (Application.isFocused && showForegroundNotification)
            {
                ShowLocalNotification(title, body, intentData);
            }

            ForegroundMessageReceived?.Invoke(message);
        }

        private void UpdateCachedToken(string token)
        {
            if (!ShouldEnableFirebasePush())
            {
                return;
            }

            if (string.IsNullOrEmpty(token) || token == _cachedToken)
                return;

            _cachedToken = token;
            if (logVerbose)
            {
                Debug.Log($"[FirebasePush] FCM Token: {token}");
            }

            TokenRefreshed?.Invoke(token);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RequestAndroidNotificationPermission()
        {
            const string permission = "android.permission.POST_NOTIFICATIONS";
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                return;

            UnityEngine.Android.Permission.RequestUserPermission(permission);
            Debug.Log("[FirebasePush] 请求 Android 通知权限");
        }

        private static bool TryGetFirebaseIntentData(out string title, out string body, out string payload)
        {
            title = string.Empty;
            body = string.Empty;
            payload = string.Empty;

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = currentActivity?.Call<AndroidJavaObject>("getIntent"))
                using (var extras = intent?.Call<AndroidJavaObject>("getExtras"))
                {
                    if (extras == null)
                    {
                        return false;
                    }

                    string taskId = GetBundleString(extras, "taskId");
                    string pushType = GetBundleString(extras, "pushType");
                    string deeplink = GetBundleString(extras, "deeplink");
                    string afDeviceId = GetBundleString(extras, "afDeviceId");
                    string appIndex = GetBundleString(extras, "appIndex");
                    string imageUrl = GetBundleString(extras, "imageUrl");
                    string messageId = GetBundleString(extras, "google.message_id");

                    title = GetBundleString(extras, "gcm.notification.title");
                    if (string.IsNullOrEmpty(title))
                    {
                        title = GetBundleString(extras, "title");
                    }

                    body = GetBundleString(extras, "gcm.notification.body");
                    if (string.IsNullOrEmpty(body))
                    {
                        body = GetBundleString(extras, "body");
                    }

                    payload = BuildIntentData(taskId, pushType, deeplink, afDeviceId, appIndex, imageUrl, null);
                    return !string.IsNullOrEmpty(payload) || !string.IsNullOrEmpty(messageId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebasePush] 读取 Firebase 通知 Intent 失败: {ex.Message}");
                return false;
            }
        }

        private static string GetBundleString(AndroidJavaObject bundle, string key)
        {
            if (bundle == null || string.IsNullOrEmpty(key) || !bundle.Call<bool>("containsKey", key))
            {
                return null;
            }

            using (var valueObject = bundle.Call<AndroidJavaObject>("get", key))
            {
                return valueObject?.Call<string>("toString");
            }
        }
#endif

        public async Task SubscribeToTopicAsync(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("Topic name cannot be empty.", nameof(topic));

            await InitializeAsync();
            await FirebaseMessaging.SubscribeAsync(topic);

            if (logVerbose)
            {
                Debug.Log($"[FirebasePush] 订阅主题 '{topic}' 成功");
            }
        }

        public async Task UnsubscribeFromTopicAsync(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("Topic name cannot be empty.", nameof(topic));

            await InitializeAsync();
            await FirebaseMessaging.UnsubscribeAsync(topic);

            if (logVerbose)
            {
                Debug.Log($"[FirebasePush] 取消订阅主题 '{topic}' 成功");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                FirebaseMessaging.TokenReceived -= HandleTokenReceived;
                FirebaseMessaging.MessageReceived -= HandleMessageReceived;
                Instance = null;
            }
        }

        private static bool ShouldEnableFirebasePush()
        {
            return FacadeAppsFlyerExtend.CanUploadServerParams();
        }

        private static string GetMessageDataValue(FirebaseMessage message, string key, string fallback = "")
        {
            if (message?.Data == null || string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            return message.Data.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value)
                ? value
                : fallback;
        }

        private static string BuildIntentData(FirebaseMessage message)
        {
            return BuildIntentData(
                GetMessageDataValue(message, "taskId"),
                GetMessageDataValue(message, "pushType"),
                GetMessageDataValue(message, "deeplink"),
                GetMessageDataValue(message, "afDeviceId"),
                GetMessageDataValue(message, "appIndex"),
                GetMessageDataValue(message, "imageUrl"),
                GetMessageDataValue(message, "intent_data"));
        }

        private static string BuildIntentData(
            string taskId,
            string pushType,
            string deeplink,
            string afDeviceId,
            string appIndex,
            string imageUrl,
            string fallbackIntentData)
        {
            var parts = new List<string>();
            AppendIntentValue(parts, "taskId", taskId);
            AppendIntentValue(parts, "pushType", pushType);
            AppendIntentValue(parts, "deeplink", deeplink);
            AppendIntentValue(parts, "afDeviceId", afDeviceId);
            AppendIntentValue(parts, "appIndex", appIndex);
            AppendIntentValue(parts, "imageUrl", imageUrl);

            if (parts.Count > 0)
            {
                return string.Join("&", parts);
            }

            return fallbackIntentData ?? string.Empty;
        }

        private static void AppendIntentValue(List<string> parts, string key, string value)
        {
            if (parts == null || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }

            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }
}
