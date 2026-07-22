using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AppsFlyerSDK;
using UnityEngine;
using UnityEngine.Networking;

namespace WZSDK
{
    [DefaultExecutionOrder(-400)]
    public class PushApiService : MonoBehaviour
    {
        private const string LogTag = "[PushApi]";
        private const float RetryDelaySeconds = 5f;

        public static PushApiService Instance { get; private set; }

        private readonly Queue<PendingPushEvent> pendingEvents = new Queue<PendingPushEvent>();
        private readonly Queue<PendingOpenReport> pendingOpenReports = new Queue<PendingOpenReport>();

        private FirebasePushService firebasePushService;
        private string afDeviceId;
        private string currentFcmToken;
        private string lastUploadedFcmToken;
        private string pushApiBaseUrl;
        private int appIndex = -1;
        private bool firebaseBound;
        private bool tokenUploaded;
        private bool appOpenQueued;
        private bool appOpenSent;
        private bool heartbeatEnabled;
        private bool heartbeatPaused;
        private bool isSendingHeartbeat;
        private bool isFlushing;
        private Coroutine heartbeatRoutine;
        private float nextAppsFlyerPollTime;
        private float nextRetryTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            if (Instance != null) return;

            var existing = FindObjectOfType<PushApiService>();
            if (existing != null)
            {
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            var go = new GameObject(nameof(PushApiService));
            go.AddComponent<PushApiService>();
        }

        private static PushApiService EnsureInstance()
        {
            if (!ShouldRunPushWorkflow())
            {
                return null;
            }

            if (Instance == null)
            {
                Bootstrap();
            }

            return Instance;
        }

        private static bool ShouldRunPushWorkflow()
        {
            return FacadeAppsFlyerExtend.CanUploadServerParams();
        }



        public static void ReportWithdrawStage3Apply()
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            EnsureInstance()?.EnqueueEvent(PushEventNames.WithdrawStage3Apply);
        }

        public static void StartHeartbeat(string appsFlyerId)
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            EnsureInstance()?.EnableHeartbeat(appsFlyerId);
        }

        private void Awake()
        {
            if (!ShouldRunPushWorkflow())
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
            ResolvePushConfig();
        }

        private void OnEnable()
        {
            FacadeAppsFlyerExtend.OnAppsFlyerIdReady += HandleAppsFlyerIdReady;
        }

        private void Start()
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            FirebasePushService.EnsureInstanceForPaidUser();
            TryBindFirebaseService();
            TryRefreshAppsFlyerId();
            QueueAppOpenIfNeeded();
            TryFlushPendingRequests();
        }

        private void Update()
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            if (!firebaseBound)
            {
                TryBindFirebaseService();
            }

            if (string.IsNullOrEmpty(afDeviceId) && Time.unscaledTime >= nextAppsFlyerPollTime)
            {
                nextAppsFlyerPollTime = Time.unscaledTime + 1f;
                TryRefreshAppsFlyerId();
            }

            if (HasPendingWork() && !isFlushing && Time.unscaledTime >= nextRetryTime)
            {
                TryFlushPendingRequests();
            }
        }

        private void OnDisable()
        {
            StopHeartbeatLoop();
            FacadeAppsFlyerExtend.OnAppsFlyerIdReady -= HandleAppsFlyerIdReady;
            UnbindFirebaseService();
        }

        private void OnDestroy()
        {
            StopHeartbeatLoop();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            heartbeatPaused = pauseStatus;
            if (pauseStatus)
            {
                PauseHeartbeatLoop();
            }
            else
            {
                ResumeHeartbeatLoop();
            }

            if (!appOpenSent) return;

            if (pauseStatus)
            {
                SendEventImmediately(PushEventNames.AppBackground);
                Debug.LogError($"{PushEventNames.AppBackground} sent.");
            }
            else
            {
                SendEventImmediately(PushEventNames.AppForeground);
                Debug.LogError($"{PushEventNames.AppForeground} sent.");
            }
        }

        private void EnableHeartbeat(string appsFlyerId)
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            if (string.IsNullOrEmpty(appsFlyerId))
            {
                return;
            }

            afDeviceId = appsFlyerId;
            heartbeatEnabled = true;
            heartbeatPaused = false;
            Debug.LogError($"{LogTag} Heartbeat enabled. Interval={GetHeartbeatIntervalSeconds()}s, afDeviceId={afDeviceId}");
            ResumeHeartbeatLoop();
        }

        private void HandleAppsFlyerIdReady(string appsFlyerId)
        {
            if (!ShouldRunPushWorkflow())
            {
                return;
            }

            if (!TryUpdateAppsFlyerId(appsFlyerId))
            {
                return;
            }

            Debug.Log($"{LogTag} AppsFlyer ID ready: {afDeviceId}");
            ResumeHeartbeatLoop();
            TryFlushPendingRequests();
        }

        private bool TryUpdateAppsFlyerId(string appsFlyerId)
        {
            if (string.IsNullOrEmpty(appsFlyerId) || string.Equals(afDeviceId, appsFlyerId, StringComparison.Ordinal))
            {
                return false;
            }

            afDeviceId = appsFlyerId;
            return true;
        }

        private void ResumeHeartbeatLoop()
        {
            if (!CanRunHeartbeatLoop())
            {
                return;
            }

            heartbeatRoutine = StartCoroutine(HeartbeatLoopCoroutine(GetHeartbeatIntervalSeconds()));
        }

        private bool CanRunHeartbeatLoop()
        {
            return heartbeatEnabled && !heartbeatPaused && heartbeatRoutine == null && !isSendingHeartbeat;
        }

        private void PauseHeartbeatLoop()
        {
            if (isSendingHeartbeat)
            {
                return;
            }

            StopHeartbeatLoop();
        }

        private void StopHeartbeatLoop()
        {
            if (heartbeatRoutine == null)
            {
                return;
            }

            StopCoroutine(heartbeatRoutine);
            heartbeatRoutine = null;
        }

        private IEnumerator HeartbeatLoopCoroutine(float initialDelaySeconds)
        {
            float nextDelaySeconds = Mathf.Max(0f, initialDelaySeconds);

            while (heartbeatEnabled && !heartbeatPaused)
            {
                if (nextDelaySeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(nextDelaySeconds);
                }

                if (!heartbeatEnabled || heartbeatPaused)
                {
                    break;
                }

                bool heartbeatSuccess = false;
                yield return SendHeartbeatCoroutine(success =>
                {
                    heartbeatSuccess = success;
                });

                nextDelaySeconds = heartbeatSuccess ? GetHeartbeatIntervalSeconds() : RetryDelaySeconds;
            }

            heartbeatRoutine = null;
        }

        private void TryBindFirebaseService()
        {
            var service = FirebasePushService.Instance;
            if (service == null)
            {
                return;
            }

            if (firebaseBound && service == firebasePushService)
            {
                return;
            }

            UnbindFirebaseService();

            firebasePushService = service;
            firebasePushService.TokenRefreshed += HandleFirebaseTokenRefreshed;
            firebasePushService.OnNotificationClicked += HandleNotificationClicked;
            firebaseBound = true;

            if (!string.IsNullOrEmpty(firebasePushService.CurrentRegistrationToken))
            {
                HandleFirebaseTokenRefreshed(firebasePushService.CurrentRegistrationToken);
            }
        }

        private void UnbindFirebaseService()
        {
            if (firebasePushService == null)
            {
                firebaseBound = false;
                return;
            }

            firebasePushService.TokenRefreshed -= HandleFirebaseTokenRefreshed;
            firebasePushService.OnNotificationClicked -= HandleNotificationClicked;
            firebasePushService = null;
            firebaseBound = false;
        }

        private void HandleFirebaseTokenRefreshed(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            if (string.Equals(currentFcmToken, token, StringComparison.Ordinal) && tokenUploaded)
            {
                return;
            }

            currentFcmToken = token;
            tokenUploaded = false;
            Debug.Log($"{LogTag} FCM token updated.");
            TryFlushPendingRequests();
        }

        private void HandleNotificationClicked(string title, string body, string intentData)
        {
            var report = ParseOpenReport(intentData);
            if (report == null)
            {
                Debug.LogWarning($"{LogTag} Notification clicked but no push payload was found.");
                return;
            }

            pendingOpenReports.Enqueue(report);
            TryFlushPendingRequests();
        }

        private void QueueAppOpenIfNeeded()
        {
            if (appOpenQueued)
            {
                return;
            }

            appOpenQueued = true;
            EnqueueEvent(PushEventNames.AppOpen);
            Debug.LogError(PushEventNames.AppOpen + "进入游戏");
        }

        private void EnqueueEvent(string eventName, int level = 0, bool hasLevel = false)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            pendingEvents.Enqueue(new PendingPushEvent
            {
                EventName = eventName,
                Timestamp = GetCurrentTimestampMs(),
                Level = level,
                HasLevel = hasLevel
            });

            TryFlushPendingRequests();
        }

        private void SendEventImmediately(string eventName, int level = 0, bool hasLevel = false)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            var pendingEvent = new PendingPushEvent
            {
                EventName = eventName,
                Timestamp = GetCurrentTimestampMs(),
                Level = level,
                HasLevel = hasLevel
            };

            StartCoroutine(SendImmediateEventCoroutine(pendingEvent));
        }

        private IEnumerator SendImmediateEventCoroutine(PendingPushEvent pendingEvent)
        {
            RefreshFlushContext();
            if (!HasResolvedPushConfig() || !HasValidAppIdentity())
            {
                Debug.LogWarning($"{LogTag} Immediate event skipped. Event={pendingEvent?.EventName}, BaseUrl={pushApiBaseUrl}, AppIndex={appIndex}, afDeviceId={afDeviceId}");
                yield break;
            }

            string eventJson = BuildEventRequestJson(pendingEvent);
            yield return SendPostRequest(GetEndpoint(GameDefines.PushEventApiPath), eventJson, null);
        }

        private void TryRefreshAppsFlyerId()
        {
            if (!string.IsNullOrEmpty(afDeviceId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(FacadeAppsFlyerExtend.CurrentAppsFlyerId))
            {
                HandleAppsFlyerIdReady(FacadeAppsFlyerExtend.CurrentAppsFlyerId);
                return;
            }

            try
            {
                string appsFlyerId = AppsFlyer.getAppsFlyerId();
                if (!string.IsNullOrEmpty(appsFlyerId))
                {
                    FacadeAppsFlyerExtend.NotifyAppsFlyerIdReady(appsFlyerId);
                }
            }
            catch
            {
            }
        }

        private void ResolvePushConfig()
        {
            if (HasResolvedPushConfig())
            {
                return;
            }

            if (string.IsNullOrEmpty(pushApiBaseUrl) && !string.IsNullOrEmpty(GameDefines.PushApiBaseUrl))
            {
                pushApiBaseUrl = GameDefines.PushApiBaseUrl.TrimEnd('/');
            }

            if (appIndex <= 0)
            {
                appIndex = GameDefines.appIndex;
            }

            if (HasResolvedPushConfig() || string.IsNullOrEmpty(GameDefines.URL))
            {
                return;
            }

            if (!Uri.TryCreate(GameDefines.URL, UriKind.Absolute, out var uri))
            {
                Debug.LogWarning($"{LogTag} Invalid GameDefines.URL: {GameDefines.URL}");
                return;
            }

            TryResolvePushConfigFromGameUrl(uri);
        }

        private bool HasResolvedPushConfig()
        {
            return appIndex > 0 && !string.IsNullOrEmpty(pushApiBaseUrl);
        }

        private void TryResolvePushConfigFromGameUrl(Uri uri)
        {
            if (string.IsNullOrEmpty(pushApiBaseUrl))
            {
                pushApiBaseUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            int parsedAppIndex = ExtractIntQueryParam(uri.Query, "appIndex");
            appIndex = parsedAppIndex > 0 ? parsedAppIndex : GameDefines.appIndex;
        }

        private void TryFlushPendingRequests()
        {
            if (!isActiveAndEnabled || isFlushing)
            {
                return;
            }

            GameManagerWZ.instance.StartCoroutine(FlushPendingRequestsCoroutine());
        }

        private IEnumerator FlushPendingRequestsCoroutine()
        {
            isFlushing = true;

            RefreshFlushContext();

            if (!HasResolvedPushConfig())
            {
                Debug.LogWarning($"{LogTag} Push API config is incomplete. BaseUrl={pushApiBaseUrl}, AppIndex={appIndex}");
                CompleteFlush(true);
                yield break;
            }

            bool tokenStepSucceeded = false;
            yield return UploadTokenIfNeededCoroutine(success =>
            {
                tokenStepSucceeded = success;
            });

            if (!tokenStepSucceeded)
            {
                CompleteFlush(true);
                yield break;
            }

            if (pendingEvents.Count > 0 && !tokenUploaded)
            {
                CompleteFlush(true);
                yield break;
            }

            bool eventStepSucceeded = false;
            yield return FlushPendingEventsCoroutine(success =>
            {
                eventStepSucceeded = success;
            });

            if (!eventStepSucceeded)
            {
                CompleteFlush(true);
                yield break;
            }

            bool openReportStepSucceeded = false;
            yield return FlushPendingOpenReportsCoroutine(success =>
            {
                openReportStepSucceeded = success;
            });

            CompleteFlush(!openReportStepSucceeded);
        }

        private void RefreshFlushContext()
        {
            ResolvePushConfig();
            TryRefreshAppsFlyerId();
        }

        private void CompleteFlush(bool shouldRetry)
        {
            if (shouldRetry)
            {
                ScheduleRetry();
            }
            else
            {
                nextRetryTime = 0f;
            }

            isFlushing = false;
        }

        private IEnumerator UploadTokenIfNeededCoroutine(Action<bool> onCompleted)
        {
            if (!CanUploadToken())
            {
                onCompleted?.Invoke(true);
                yield break;
            }

            bool uploadSuccess = false;
            string tokenJson = BuildTokenRequestJson();

            yield return SendPostRequest(GetEndpoint(GameDefines.PushTokenApiPath), tokenJson, success =>
            {
                uploadSuccess = success;
            });

            if (uploadSuccess)
            {
                tokenUploaded = true;
                lastUploadedFcmToken = currentFcmToken;
            }

            onCompleted?.Invoke(uploadSuccess);
        }

        private IEnumerator FlushPendingEventsCoroutine(Action<bool> onCompleted)
        {
            bool flushSucceeded = true;

            while (pendingEvents.Count > 0)
            {
                PendingPushEvent pendingEvent = pendingEvents.Peek();
                bool eventSuccess = false;
                string eventJson = BuildEventRequestJson(pendingEvent);

                yield return SendPostRequest(GetEndpoint(GameDefines.PushEventApiPath), eventJson, success =>
                {
                    eventSuccess = success;
                });

                if (!eventSuccess)
                {
                    flushSucceeded = false;
                    break;
                }

                pendingEvents.Dequeue();
                if (string.Equals(pendingEvent.EventName, PushEventNames.AppOpen, StringComparison.Ordinal))
                {
                    appOpenSent = true;
                }
            }

            onCompleted?.Invoke(flushSucceeded);
        }

        private IEnumerator FlushPendingOpenReportsCoroutine(Action<bool> onCompleted)
        {
            bool flushSucceeded = true;

            while (pendingOpenReports.Count > 0)
            {
                PendingOpenReport openReport = pendingOpenReports.Peek();
                NormalizeOpenReport(openReport);
                if (!CanSendOpenReport(openReport))
                {
                    flushSucceeded = false;
                    break;
                }

                bool openSuccess = false;
                string openJson = BuildOpenRequestJson(openReport);

                yield return SendPostRequest(GetEndpoint(GameDefines.PushOpenApiPath), openJson, success =>
                {
                    openSuccess = success;
                });

                if (!openSuccess)
                {
                    flushSucceeded = false;
                    break;
                }

                pendingOpenReports.Dequeue();
            }

            onCompleted?.Invoke(flushSucceeded);
        }

        private bool HasPendingWork()
        {
            return pendingEvents.Count > 0 || pendingOpenReports.Count > 0 || CanUploadToken();
        }

        private void ScheduleRetry()
        {
            nextRetryTime = Time.unscaledTime + RetryDelaySeconds;
        }

        private bool CanUploadToken()
        {
            return !string.IsNullOrEmpty(currentFcmToken)
                   && HasValidAppIdentity()
                   && (!tokenUploaded || !string.Equals(lastUploadedFcmToken, currentFcmToken, StringComparison.Ordinal));
        }

        private bool HasValidAppIdentity()
        {
            return !string.IsNullOrEmpty(afDeviceId) && appIndex > 0;
        }

        private bool CanSendOpenReport(PendingOpenReport report)
        {
            if (report == null)
            {
                return false;
            }

            if (report.HasTaskId)
            {
                return true;
            }

            return !string.IsNullOrEmpty(report.AfDeviceId)
                   && report.AppIndex > 0
                   && !string.IsNullOrEmpty(report.PushType);
        }

        private void NormalizeOpenReport(PendingOpenReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(report.AfDeviceId))
            {
                report.AfDeviceId = afDeviceId;
            }

            if (report.AppIndex <= 0)
            {
                report.AppIndex = appIndex;
            }
        }

        private IEnumerator SendPostRequest(string url, string json, Action<bool> onCompleted)
        {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{LogTag} Request failed: {url} | {request.error}");
                    onCompleted?.Invoke(false);
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                if (TryParseApiSuccess(responseText, out string responseMsg))
                {
                    Debug.Log($"{LogTag} Request success: {url}");
                    onCompleted?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"{LogTag} API rejected request: {url} | {responseMsg}");
                    onCompleted?.Invoke(false);
                }
            }
        }

        private IEnumerator SendHeartbeatCoroutine(Action<bool> onCompleted)
        {
            isSendingHeartbeat = true;
            bool heartbeatSuccess = false;

            string heartbeatUrl = GameDefines.BuildHeartbeatUrl(afDeviceId);
            if (string.IsNullOrEmpty(heartbeatUrl))
            {
                Debug.LogWarning($"{LogTag} Heartbeat skipped. Retry in {RetryDelaySeconds}s. HeartbeatApiPath={GameDefines.HeartbeatApiPath}, afDeviceId={afDeviceId}");
                isSendingHeartbeat = false;
                onCompleted?.Invoke(false);
                yield break;
            }

            Debug.Log($"{LogTag} Heartbeat GET -> {heartbeatUrl}");

            using (var request = UnityWebRequest.Get(heartbeatUrl))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{LogTag} Heartbeat GET failed: {heartbeatUrl} | {request.error} | code={request.responseCode}");
                }
                else
                {
                    heartbeatSuccess = true;
                    Debug.Log($"{LogTag} Heartbeat GET success. Next heartbeat in {GetHeartbeatIntervalSeconds()}s. Url={heartbeatUrl}");
                }
            }

            isSendingHeartbeat = false;
            onCompleted?.Invoke(heartbeatSuccess);
        }

        private bool TryParseApiSuccess(string responseText, out string responseMsg)
        {
            responseMsg = "Unknown response";
            if (string.IsNullOrEmpty(responseText))
            {
                responseMsg = "Empty response";
                return false;
            }

            try
            {
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(responseText);
                responseMsg = response != null ? response.msg : "Invalid response";
                return response != null && response.code == 200;
            }
            catch (Exception ex)
            {
                responseMsg = ex.Message;
                return false;
            }
        }

        private string GetEndpoint(string path)
        {
            return string.IsNullOrEmpty(pushApiBaseUrl) || string.IsNullOrEmpty(path)
                ? string.Empty
                : $"{pushApiBaseUrl}{path}";
        }

        private string BuildTokenRequestJson()
        {
            var builder = new StringBuilder(256);
            bool hasProperty = false;

            builder.Append('{');
            AppendRequestIdentityFields(builder, ref hasProperty, afDeviceId, appIndex);
            AppendJsonString(builder, ref hasProperty, "fcmToken", currentFcmToken);
            AppendJsonString(builder, ref hasProperty, "platform", GetPlatformName());
            AppendLocaleFields(builder, ref hasProperty);
            AppendJsonString(builder, ref hasProperty, "appVersion", Application.version);
            builder.Append('}');

            return builder.ToString();
        }

        private string BuildEventRequestJson(PendingPushEvent pendingEvent)
        {
            var builder = new StringBuilder(256);
            bool hasProperty = false;

            builder.Append('{');
            AppendRequestIdentityFields(builder, ref hasProperty, afDeviceId, appIndex);
            AppendJsonString(builder, ref hasProperty, "event", pendingEvent.EventName);
            if (pendingEvent.HasLevel)
            {
                AppendJsonNumber(builder, ref hasProperty, "level", pendingEvent.Level);
            }
            AppendJsonNumber(builder, ref hasProperty, "timestamp", pendingEvent.Timestamp);
            AppendLocaleFields(builder, ref hasProperty);
            builder.Append('}');

            return builder.ToString();
        }

        private string BuildOpenRequestJson(PendingOpenReport report)
        {
            var builder = new StringBuilder(128);
            bool hasProperty = false;

            builder.Append('{');
            if (report.HasTaskId)
            {
                AppendJsonNumber(builder, ref hasProperty, "taskId", report.TaskId);
            }

            if (!string.IsNullOrEmpty(report.AfDeviceId))
            {
                AppendJsonString(builder, ref hasProperty, "afDeviceId", report.AfDeviceId);
            }

            if (report.AppIndex > 0)
            {
                AppendJsonNumber(builder, ref hasProperty, "appIndex", report.AppIndex);
            }

            if (!string.IsNullOrEmpty(report.PushType))
            {
                AppendJsonString(builder, ref hasProperty, "pushType", report.PushType);
            }
            builder.Append('}');

            return builder.ToString();
        }

        private PendingOpenReport ParseOpenReport(string intentData)
        {
            if (string.IsNullOrEmpty(intentData))
            {
                return null;
            }

            var values = ParseQueryString(intentData);
            if (values.Count == 0)
            {
                return null;
            }

            var report = new PendingOpenReport();
            if (values.TryGetValue("taskId", out string taskIdValue) && long.TryParse(taskIdValue, out long parsedTaskId))
            {
                report.HasTaskId = true;
                report.TaskId = parsedTaskId;
            }

            if (values.TryGetValue("afDeviceId", out string payloadAfDeviceId))
            {
                report.AfDeviceId = payloadAfDeviceId;
            }

            if (values.TryGetValue("appIndex", out string payloadAppIndex) && int.TryParse(payloadAppIndex, out int parsedAppIndex))
            {
                report.AppIndex = parsedAppIndex;
            }

            if (values.TryGetValue("pushType", out string pushType))
            {
                report.PushType = pushType;
            }

            return report.HasTaskId || !string.IsNullOrEmpty(report.PushType) ? report : null;
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(queryString))
            {
                return result;
            }

            string normalizedQuery = queryString.TrimStart('?');
            string[] parts = normalizedQuery.Split('&');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryParseQueryEntry(parts[i], out string key, out string value))
                {
                    continue;
                }

                result[key] = value;
            }

            return result;
        }

        private static int ExtractIntQueryParam(string query, string key)
        {
            if (string.IsNullOrEmpty(query))
            {
                return -1;
            }

            string normalizedQuery = query.TrimStart('?');
            string[] segments = normalizedQuery.Split('&');
            for (int i = 0; i < segments.Length; i++)
            {
                if (!TryParseQueryEntry(segments[i], out string currentKey, out string valueText))
                {
                    continue;
                }

                if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(valueText, out int value))
                {
                    return value;
                }
            }

            return -1;
        }

        private static bool TryParseQueryEntry(string queryEntry, out string key, out string value)
        {
            key = null;
            value = null;
            if (string.IsNullOrEmpty(queryEntry))
            {
                return false;
            }

            int separatorIndex = queryEntry.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            key = Uri.UnescapeDataString(queryEntry.Substring(0, separatorIndex));
            value = Uri.UnescapeDataString(queryEntry.Substring(separatorIndex + 1));
            return true;
        }

        private string GetLanguageCode()
        {
            UserDataManager userDataManager = UserDataManager.Instance; //GameObject.FindObjectOfType<UserDataManager>();//FindObjectOfType<UserDataManager>();
            if (userDataManager != null && !string.IsNullOrEmpty(userDataManager.targetLanguage))
            {
                return userDataManager.targetLanguage;
            }

            try
            {
                string cultureName = CultureInfo.CurrentCulture.Name;
                if (!string.IsNullOrEmpty(cultureName))
                {
                    return cultureName;
                }
            }
            catch
            {
            }

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh-CN";
                case SystemLanguage.Japanese:
                    return "ja-JP";
                case SystemLanguage.Portuguese:
                    return "pt-BR";
                default:
                    return "en-US";
            }
        }

        private string GetTimeZoneId()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var timeZoneClass = new AndroidJavaClass("java.util.TimeZone"))
                using (var timeZone = timeZoneClass.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    string timeZoneId = timeZone.Call<string>("getID");
                    if (!string.IsNullOrEmpty(timeZoneId))
                    {
                        return timeZoneId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogTag} Failed to read Android timezone: {ex.Message}");
            }
#endif

            return TimeZoneInfo.Local.Id;
        }

        private static string GetPlatformName()
        {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#elif UNITY_STANDALONE_WIN
            return "windows";
#elif UNITY_STANDALONE_OSX
            return "mac";
#else
            return "editor";
#endif
        }

        private static long GetCurrentTimestampMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private float GetHeartbeatIntervalSeconds()
        {
            return Mathf.Max(1f, GameDefines.HeartbeatIntervalSeconds);
        }

        private static void AppendJsonString(StringBuilder builder, ref bool hasProperty, string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (hasProperty)
            {
                builder.Append(',');
            }

            builder.Append('"').Append(EscapeJson(key)).Append('"').Append(':');
            if (value == null)
            {
                builder.Append("null");
            }
            else
            {
                builder.Append('"').Append(EscapeJson(value)).Append('"');
            }

            hasProperty = true;
        }

        private static void AppendJsonNumber(StringBuilder builder, ref bool hasProperty, string key, long value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (hasProperty)
            {
                builder.Append(',');
            }

            builder.Append('"').Append(EscapeJson(key)).Append('"').Append(':').Append(value);
            hasProperty = true;
        }

        private static void AppendJsonNumber(StringBuilder builder, ref bool hasProperty, string key, int value)
        {
            AppendJsonNumber(builder, ref hasProperty, key, (long)value);
        }

        private static void AppendRequestIdentityFields(StringBuilder builder, ref bool hasProperty, string deviceId, int currentAppIndex)
        {
            AppendJsonString(builder, ref hasProperty, "afDeviceId", deviceId);
            AppendJsonNumber(builder, ref hasProperty, "appIndex", currentAppIndex);
        }

        private void AppendLocaleFields(StringBuilder builder, ref bool hasProperty)
        {
            AppendJsonString(builder, ref hasProperty, "language", GetLanguageCode());
            AppendJsonString(builder, ref hasProperty, "timezone", GetTimeZoneId());
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        [Serializable]
        private class ApiResponse
        {
            public int code;
            public string msg;
        }

        private class PendingPushEvent
        {
            public string EventName;
            public long Timestamp;
            public int Level;
            public bool HasLevel;
        }

        private class PendingOpenReport
        {
            public bool HasTaskId;
            public long TaskId;
            public string AfDeviceId;
            public int AppIndex;
            public string PushType;
        }

        private static class PushEventNames
        {
            public const string AppOpen = "app_open";
            public const string AppForeground = "app_foreground";
            public const string AppBackground = "app_background";
            public const string LevelComplete = "level_complete";
            public const string WithdrawStage3Apply = "withdraw_stage3_apply";
        }
    }
}
