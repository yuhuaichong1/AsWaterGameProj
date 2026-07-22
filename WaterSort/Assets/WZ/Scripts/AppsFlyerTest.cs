namespace WZSDK
{
    using AppsFlyerSDK;
    using System;
    using System.Collections;
    using System.Globalization;
    
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    
    
    
    public class AppsFlyerTest : MonoBehaviour, IAppsFlyerConversionData, ILoad
    {
        private const int AfReportRetryCount = 3;
    
        public string devKey;                                  //qMMYrLguTV9WjiFdtnk4rA
        public string appID;
        public string UWPAppID;
        public string macOSAppID;
        public bool isDebug;
        public bool getConversionData;
    
        private string guid;
        private string cachedAfDeviceId;
        private bool uploadFlowStarted;
    
    
        public void SetGUID(string guid)
        {
            this.guid = guid;
        }
    
        private void Start()
        {
            Load();
        }
    
        public void Load()
        {
            Debug.LogError("afInit");
            cachedAfDeviceId = AppsFlyerIdResolver.ResolveCurrentId();
            SyncMaxUserId(cachedAfDeviceId, "AppsFlyerTest.Load");
            AddEvent();
            AppsFlyer.setCustomerUserId(guid);
            AppsFlyer.setIsDebug(isDebug);
    #if UNITY_WSA_10_0 && !UNITY_EDITOR
            AppsFlyer.initSDK(devKey, UWPAppID, getConversionData ? this : null);
    #elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
        AppsFlyer.initSDK(devKey, macOSAppID, getConversionData ? this : null);
    #else
            AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);
    #endif
            AppsFlyer.startSDK();
            StartCoroutine(NotifyAppsFlyerIdWhenReady());
        }
    
        public void OnDestroy()
        {
            RemoveEvent();
        }
    
        private void AddEvent()
        {
            FacadeAppsFlyerExtend.SendAdSuccessCountEvent = SendAdSuccessCountEvent;
             FacadeAppsFlyerExtend.SendAdRevenue = SendAdRevenue;
            FacadeAppsFlyerExtend.OnAttributionResolved += HandleAttributionResolved;
            FacadeAppsFlyerExtend.OnUploadPolicyResolved += HandleUploadPolicyResolved;
        }
    
        private void RemoveEvent()
        {
            FacadeAppsFlyerExtend.SendAdSuccessCountEvent = null;
            FacadeAppsFlyerExtend.SendAdRevenue = null;
            FacadeAppsFlyerExtend.OnAttributionResolved -= HandleAttributionResolved;
            FacadeAppsFlyerExtend.OnUploadPolicyResolved -= HandleUploadPolicyResolved;
        }
    
        //广告成功播放接口
        private void SendAdSuccessCountEvent(int adCount, double singleAdRevenue)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                { "adCount", adCount.ToString() },
            };
            AppsFlyer.sendEvent("AdSuccessEvent", dic);
        }
    
        //广告收入
        private void SendAdRevenue(MaxSdkBase.AdInfo adInfo, EAdType eAdType)
        {
            if (adInfo == null)
            {
                Debug.LogWarning("AppsFlyer广告收入回调为空，跳过广告收益上报");
                return;
            }
    
            string adType = GetAdType(eAdType);
            Dictionary<string, string> additionalParams = new Dictionary<string, string>();
    
            additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnitIdentifier);
            additionalParams.Add(AdRevenueScheme.AD_TYPE, adType);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.Placement);
            var logRevenue = new AFAdRevenueData("monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
            Debug.LogError(adInfo.Revenue+"当前是");

            ReportAfAdCountToServer(adInfo.Revenue, adType);
            Debug.LogError("广告收入af");
        }
    
        private string GetAdType(EAdType eAdType)
        {
            switch (eAdType)
            {
                case EAdType.EReward:
                    return "reward";
                case EAdType.EInterstitial:
                    return "interstitial";
                case EAdType.EBanner:
                    return "banner";
                case EAdType.ERec:
                    return "rec";
            }
            return "";
        }
    
        // Mark AppsFlyer CallBacks
        public void onConversionDataSuccess(string conversionData)
        {
            AppsFlyer.AFLog("didReceiveConversionData", conversionData);
            Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
    
    
            string afDeviceId = AppsFlyerIdResolver.ResolveCurrentId();
            Debug.Log("AppsFlyer Device ID: " + afDeviceId);
            CacheAfDeviceId(afDeviceId);
    
            Debug.LogError("AppsFlyer获取归因数据");
            TDAnalyticsMgr.Instance. ProcessAndReportAttribution(conversionDataDictionary);
            TDAnalyticsMgr.Instance.AppsFlyerMsg(conversionDataDictionary);
            ResolveAttribution(conversionDataDictionary);
            StartGameAfterAttribution();
        }
    
        public void loadConversionData(string conversionData)
        {
            Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
            CacheAfDeviceId(AppsFlyerIdResolver.ResolveCurrentId());
            TDAnalyticsMgr.Instance.ProcessAndReportAttribution(conversionDataDictionary);
            ResolveAttribution(conversionDataDictionary);
            StartGameAfterAttribution();
        }
    
        public void onConversionDataFail(string error)
        {
            AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
    
             Debug.LogError($"AppsFlyer归因数据获取失败: {error}");
            CacheAfDeviceId(AppsFlyerIdResolver.ResolveCurrentId());
            ReportAsOrganicUser();
            StartGameAfterAttribution();
        }
    
        //当应用打开时的归因处理
        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
            Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
    
             CacheAfDeviceId(AppsFlyerIdResolver.ResolveCurrentId());
             TDAnalyticsMgr.Instance.ProcessAndReportAttribution(attributionDataDictionary);
            ResolveAttribution(attributionDataDictionary);
            StartGameAfterAttribution();
        }
    
        //当应用打开时归因失败
        public void onAppOpenAttributionFailure(string error)
        {
            AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
            CacheAfDeviceId(AppsFlyerIdResolver.ResolveCurrentId());
            ReportAsOrganicUser();
            StartGameAfterAttribution();
        }
    
       
    
        private string CacheAfDeviceId(string afDeviceId)
        {
            if (string.IsNullOrEmpty(afDeviceId))
            {
                return string.Empty;
            }

            cachedAfDeviceId = afDeviceId;
            SyncMaxUserId(afDeviceId, "AppsFlyerTest.CacheAfDeviceId");
            FacadeAppsFlyerExtend.NotifyAppsFlyerIdReady(afDeviceId);
            TryStartUploadFlow(afDeviceId);
            return cachedAfDeviceId;
        }
    
        private string GetCachedAfDeviceId()
        {
            if (!string.IsNullOrEmpty(cachedAfDeviceId))
            {
                return cachedAfDeviceId;
            }

            cachedAfDeviceId = AppsFlyerIdResolver.ResolveCurrentId();
            if (!string.IsNullOrEmpty(cachedAfDeviceId))
            {
                return cachedAfDeviceId;
            }

            return string.Empty;
        }
    
        private void HandleAttributionResolved(bool isPaidUser)
        {
            TryStartUploadFlow(GetCachedAfDeviceId());
        }
    
        private void HandleUploadPolicyResolved(bool forcedUpload)
        {
            TryStartUploadFlow(GetCachedAfDeviceId());
        }
    
        private bool CanUseUploadFlow()
        {
            return Application.isEditor || FacadeAppsFlyerExtend.CanUploadServerParams();
        }
    
        private void TryStartUploadFlow(string afDeviceId)
        {
            if (uploadFlowStarted || !CanUseUploadFlow() || string.IsNullOrEmpty(afDeviceId))
            {
                return;
            }
    
            uploadFlowStarted = true;
            ReportAfDeviceIdToServer(afDeviceId);
            ReportHeartbeatToServer(afDeviceId);
            PushApiService.StartHeartbeat(afDeviceId);
        }
    
        private void StartGameAfterAttribution()
        {
            Debug.Log("AppsFlyer归因已返回，启动流程继续由 GameManagerWZ 控制。");
        }
    
        private void ReportAfDeviceIdToServer(string afDeviceId)
        {
            if (!CanUseUploadFlow())
            {
                return;
            }
    
            if (string.IsNullOrEmpty(afDeviceId))
            {
                Debug.LogWarning("AppsFlyer Device ID为空，跳过上报user/af");
                return;
            }
    
            GameManagerWZ.instance.StartCoroutine(ReportAfDeviceIdCoroutine(afDeviceId));
        }
    
    
        private IEnumerator ReportAfDeviceIdCoroutine(string afDeviceId)
        {
            yield return PostAfDeviceIdCoroutine(GameDefines.AfInitApiPath, afDeviceId, "AppsFlyer Device ID");
        }
    
        private IEnumerator PostAfDeviceIdCoroutine(string endpointPath, string afDeviceId, string logName)
        {
            string url = BuildServerUrl(endpointPath);
    
            Debug.LogError("PostAfDeviceIdCoroutine路径:" + url);
            if (string.IsNullOrEmpty(url))
            {
                yield break;
            }
    
            for (int attempt = 1; attempt <= AfReportRetryCount; attempt++)
            {
                WWWForm form = new WWWForm();
                form.AddField("afDeviceId", afDeviceId);
                form.AddField("appIndex", GameDefines.appIndex);
                form.AddField("statusName", GameDefines.AfInitStatusName);
    
                using (UnityWebRequest webReq = UnityWebRequest.Post(url, form))
                {
                    webReq.timeout = 16;
                    yield return webReq.SendWebRequest();
    
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"{logName}上报成功: {afDeviceId}");
                        yield break;
                    }
    
                    Debug.LogError($"{logName}上报失败({attempt}/{AfReportRetryCount}): {webReq.error}, code={webReq.responseCode}");
                }
    
                if (attempt < AfReportRetryCount)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }
            }
        }
    
        private void ReportHeartbeatToServer(string afDeviceId)
        {
            if (!CanUseUploadFlow())
            {
                return;
            }
    
            if (string.IsNullOrEmpty(afDeviceId))
            {
                Debug.LogWarning("AppsFlyer Device ID为空，跳过上报heartbeat");
                return;
            }
    
            GameManagerWZ.instance.StartCoroutine(GetHeartbeatCoroutine(afDeviceId));
        }
    
        private void ReportAfAdCountToServer(double singleAdRevenue, string adType)
        {
            string formattedAdRevenue = FormatAdRevenue(singleAdRevenue);
            if (!CanUseUploadFlow())
            {
                Debug.Log($"AppsFlyer广告收益上报跳过: CanUseUploadFlow=false, adType={adType}, revenue={formattedAdRevenue}");
                return;
            }

            bool hasReachedLuckyWalletUnlockLevel = GameManagerWZ.instance.currentLv >= GameDefines.LuckyWalletUnlockLevel;
            string reportIncomeAmount = hasReachedLuckyWalletUnlockLevel ? formattedAdRevenue : 0d.ToString("F4", CultureInfo.InvariantCulture);

            string afDeviceId = GetCachedAfDeviceId();
            if (string.IsNullOrEmpty(afDeviceId))
            {
                Debug.LogWarning($"AppsFlyer Device ID为空，跳过上报user/af/ad/count, adType={adType}, revenue={formattedAdRevenue}");
                return;
            }

            if (!hasReachedLuckyWalletUnlockLevel)
            {
                Debug.Log($"当前关卡{GameManagerWZ.instance.currentLv}未达到LuckyWalletUnlockLevel={GameDefines.LuckyWalletUnlockLevel}，继续上报user/af/ad/count，但incomeAmount固定为{reportIncomeAmount}, adType={adType}, revenue={formattedAdRevenue}");
            }

            GameManagerWZ.instance.StartCoroutine(PostAfAdCountCoroutine(afDeviceId, formattedAdRevenue, reportIncomeAmount, adType));
        }

        private IEnumerator NotifyAppsFlyerIdWhenReady()
        {
            const int maxRetryCount = 15;
    
            for (int i = 0; i < maxRetryCount; i++)
            {
                string appsFlyerId = string.Empty;
                try
                {
                    appsFlyerId = GetCachedAfDeviceId();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"AppsFlyer ID 获取失败: {ex.Message}");
                }
    
                if (!string.IsNullOrEmpty(appsFlyerId))
                {
                    CacheAfDeviceId(appsFlyerId);
                    yield break;
                }
    
                yield return new WaitForSecondsRealtime(1f);
            }
    
            Debug.LogWarning("AppsFlyer ID 在重试窗口内仍未获取到。");
        }
    
        private IEnumerator GetHeartbeatCoroutine(string afDeviceId)
        {
            string url = GameDefines.BuildHeartbeatUrl(afDeviceId);
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("AppsFlyer心跳上报地址为空");
                yield break;
            }
    
            for (int attempt = 1; attempt <= AfReportRetryCount; attempt++)
            {
                Debug.Log($"AppsFlyer首次心跳GET -> {url}");
    
                using (UnityWebRequest webReq = UnityWebRequest.Get(url))
                {
                    webReq.timeout = 16;
                    yield return webReq.SendWebRequest();
    
                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"AppsFlyer首次心跳上报成功: {afDeviceId}, url={url}");
                        yield break;
                    }
    
                    Debug.LogError($"AppsFlyer首次心跳上报失败({attempt}/{AfReportRetryCount}): {webReq.error}, code={webReq.responseCode}, url={url}");
                }
    
                if (attempt < AfReportRetryCount)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }
            }
        }
    
        private IEnumerator PostAfAdCountCoroutine(string afDeviceId, string formattedAdRevenue, string reportIncomeAmount, string adType)
        {
            string url = BuildServerUrl(GameDefines.AfAdCountApiPath);
            if (string.IsNullOrEmpty(url))
            {
                yield break;
            }
    
            for (int attempt = 1; attempt <= AfReportRetryCount; attempt++)
            {
                WWWForm form = new WWWForm();
                form.AddField("afDeviceId", afDeviceId);
                form.AddField("appIndex", GameDefines.appIndex);
                form.AddField("incomeAmount", reportIncomeAmount);
                using (UnityWebRequest webReq = UnityWebRequest.Post(url, form))
                {
                    webReq.timeout = 16;
                    yield return webReq.SendWebRequest();

                    if (webReq.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = webReq.downloadHandler != null ? webReq.downloadHandler.text : string.Empty;
                        Debug.Log($"AppsFlyer广告收益上报成功: adType={adType}, afDeviceId={afDeviceId}, revenue={formattedAdRevenue}, adRevenue={formattedAdRevenue}, incomeAmount={reportIncomeAmount}, code={webReq.responseCode}, response={responseText}");
                        yield break;
                    }

                    string failedResponseText = webReq.downloadHandler != null ? webReq.downloadHandler.text : string.Empty;
                    Debug.LogError($"AppsFlyer广告收益上报失败({attempt}/{AfReportRetryCount}): adType={adType}, error={webReq.error}, code={webReq.responseCode}, afDeviceId={afDeviceId}, revenue={formattedAdRevenue}, adRevenue={formattedAdRevenue}, incomeAmount={reportIncomeAmount}, response={failedResponseText}");
                }
    
                if (attempt < AfReportRetryCount)
                {
                    yield return new WaitForSecondsRealtime(3f);
                }
            }
        }
    
        private string BuildServerUrl(string path)
        {
            return GameDefines.BuildPushApiUrl(path);
        }

        private static string FormatAdRevenue(double revenue)
        {
            return Math.Round(revenue, 4, MidpointRounding.AwayFromZero).ToString("F4", CultureInfo.InvariantCulture);
        }

        private void SyncMaxUserId(string afDeviceId, string source)
        {
            AdsControl.SyncMaxUserId(afDeviceId, source);
        }

        private void ResolveAttribution(Dictionary<string, object> attributionData)
        {
            bool isPaidUser = IsPaidUser(attributionData);
            string afStatus = GetAttributionValue(attributionData, "af_status");
            string mediaSource = GetAttributionValue(attributionData, "media_source", "af_media_source");
            Debug.Log($"AppsFlyer归因判定: isPaidUser={isPaidUser}, af_status={afStatus}, media_source={mediaSource}");
            FacadeAppsFlyerExtend.NotifyAttributionResolved(isPaidUser);
        }

        private static bool IsPaidUser(Dictionary<string, object> attributionData)
        {
            string afStatus = GetAttributionValue(attributionData, "af_status");
            if (string.Equals(afStatus, "Organic", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(afStatus, "Non-organic", StringComparison.OrdinalIgnoreCase))
                return true;

            string mediaSource = GetAttributionValue(attributionData, "media_source", "af_media_source");
            return !string.IsNullOrEmpty(mediaSource)
                && !string.Equals(mediaSource, "organic", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAttributionValue(Dictionary<string, object> attributionData, params string[] keys)
        {
            if (attributionData == null || keys == null)
                return string.Empty;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrEmpty(key) || !attributionData.TryGetValue(key, out object value) || value == null)
                    continue;

                return value.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        ///上报归因事件
        /// </summary>
        private void TrackAttributionEvent(Dictionary<string, object> parameters)
        {
            try
            {
                if (YarnCoveMgr.Instance != null && TDAnalyticsMgr.Instance!= null)
                {
                    TDAnalyticsMgr.Instance.TrackAttributionEvent(parameters);
                }
            }
            catch (Exception e)
            {
                
            }
        }
    
     
    
       /// <summary>
    /// 按自然用户上报（用于归因失败的情况）
    /// </summary>
    private void ReportAsOrganicUser()
    {
        FacadeAppsFlyerExtend.NotifyAttributionResolved(false);
    
        var organicParams = new Dictionary<string, object>
        {
            { "af_status", "Organic" },
            { "af_media_source", "" },
            { "af_campaign", "" },
            { "user_type", "organic" },
            { "is_organic_user", true }
        };
    
            TDAnalyticsMgr.Instance.SetUserProperties(organicParams);
    
        if (!HasReportedAttributionEvent())
        {
            TrackAttributionEvent(organicParams);
                TDAnalyticsMgr.Instance.MarkAttributionEventReported();
        }
    }
        /// <summary>
        /// 新增：检查是否已经上报过归因事件
        /// </summary>
        private bool HasReportedAttributionEvent()
        {
            return PlayerPrefs.GetInt("attribution_reported", 0) == 1;
        }
    
    }
}
