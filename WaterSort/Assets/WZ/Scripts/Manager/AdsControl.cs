namespace WZSDK
{
    using System;
    using System.Collections;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Events;
    
    
    public class AdsControl : MonoBehaviour
    {
        private static AdsControl instance;
        private static string lastAppliedMaxUserId;
    
        [Header("Max SDK设置")]
        public string maxSdkKey = "YOUR_MAX_SDK_KEY_HERE";
        public string maxBannerAdUnitId = "";
        public string maxInterstitialAdUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";
        public string maxRewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";
    
        [Header("广告设置")]
        public bool showBanner = false;
        public bool showInterstitial = true;
        public bool isTestMode = true;
        public int interstitialFrequency = 3;
    
        private int refuseCount = 0;
        private int currentMaxRefuseCount = 3;
        private Action refuseSuccessCallback;
        private Action<string> refuseFailCallback;
        private string currentRefuseSource;
    
        private bool isMaxInitialized = false;
        private bool isBannerLoaded = false;
        private bool isBannerShowing = false;
        private bool isInterstitialReady = false;
        private bool isRewardedReady = false;
    
        private Action<bool> currentRewardedCallback;
        private string currentRewardType = "";
        private bool currentRewardShouldAutoShowWithdrawMissionView = true;
        private bool currentRefuseShouldAutoShowWithdrawMissionView = true;
    
        private const string PREF_TOTAL_AD_COUNT = "td_total_ad_reward_count";
        private const string PREF_ACCU_AD_REVENUE = "td_accu_ad_revenue";
    
        public UnityEvent onAdsInitialized;
        public UnityEvent onRewardedAdReady;
        public UnityEvent onInterstitialAdReady;
    
        public static AdsControl Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<AdsControl>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("AdsManager");
                        instance = obj.AddComponent<AdsControl>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }
    
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
    
            InitializeMaxSDK();
            RandomizeMaxRefuseCount();
        }
    
        private void RandomizeMaxRefuseCount()
        {
            currentMaxRefuseCount = UnityEngine.Random.Range(3, 5);
        }
    
        private void ResetRefuseCount()
        {
            refuseCount = 0;
            RandomizeMaxRefuseCount();
        }
    
        public void HandleRefuseAd(string source, Action successCallback, Action<string> failCallback, bool autoShowWithdrawMissionView = true)
        {
            refuseCount++;
            currentRefuseSource = source;
            currentRefuseShouldAutoShowWithdrawMissionView = autoShowWithdrawMissionView;
    
            if (refuseCount >= currentMaxRefuseCount)
            {
                ResetRefuseCount();
                refuseSuccessCallback = successCallback;
                refuseFailCallback = failCallback;
    
                int currentLevel = GameManagerWZ.instance.currentLv;
                if (currentLevel >= 4 && currentLevel <= 10)
                {
                    Debug.Log($"当前关卡 {currentLevel} 在 4-10 范围内，强制显示插屏");
                    ShowInterstitialAdForRefuse();
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 100) < 50)
                        ShowInterstitialAdForRefuse();
                    else
                        ShowRewardedAdForRefuse("force_reward");
                }
            }
            else
            {
                currentRefuseShouldAutoShowWithdrawMissionView = true;
                failCallback?.Invoke("");
            }
        }
    
        private void ShowInterstitialAdForRefuse()
        {
            if (!isMaxInitialized)
            {
                refuseFailCallback?.Invoke("????????????");
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                refuseFailCallback = null;
                refuseSuccessCallback = null;
                currentRefuseShouldAutoShowWithdrawMissionView = true;
                return;
            }
    
            if (MaxSdk.IsInterstitialReady(maxInterstitialAdUnitId))
            {
                MaxSdk.ShowInterstitial(maxInterstitialAdUnitId);
            }
            else
            {
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                LoadInterstitialAd();
                refuseFailCallback?.Invoke("广告设置δ׼����");
                refuseFailCallback = null;
                refuseSuccessCallback = null;
                currentRefuseShouldAutoShowWithdrawMissionView = true;
            }
        }
    
        private void ShowRewardedAdForRefuse(string rewardType)
        {
            if (!isMaxInitialized)
            {
                refuseFailCallback?.Invoke("���δ��ʼ��");
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                refuseFailCallback = null;
                refuseSuccessCallback = null;
                currentRewardShouldAutoShowWithdrawMissionView = true;
                currentRefuseShouldAutoShowWithdrawMissionView = true;
                return;
            }
    
            if (MaxSdk.IsRewardedAdReady(maxRewardedAdUnitId))
            {
                currentRewardType = rewardType;
                currentRewardShouldAutoShowWithdrawMissionView = currentRefuseShouldAutoShowWithdrawMissionView;
                MaxSdk.ShowRewardedAd(maxRewardedAdUnitId);
            }
            else
            {
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                LoadRewardedAd();
                refuseFailCallback?.Invoke("广告设置δ׼����");
                refuseFailCallback = null;
                refuseSuccessCallback = null;
                currentRewardShouldAutoShowWithdrawMissionView = true;
                currentRefuseShouldAutoShowWithdrawMissionView = true;
            }
        }
    
        private void ShowNotice(string message)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowView(EUIType.NoticeView, message);
            }
        }
    
        #region Max SDK��ʼ��
        private void InitializeMaxSDK()
        {
            if (string.IsNullOrEmpty(maxSdkKey)) return;
    
            MaxSdkCallbacks.OnSdkInitializedEvent += OnMaxSdkInitialized;
            MaxSdk.SetSdkKey(maxSdkKey);
            SyncMaxUserId(null, "AdsControl.InitializeMaxSDK");
            MaxSdk.InitializeSdk();
        }
    
        private void OnMaxSdkInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            isMaxInitialized = true;
            SyncMaxUserId(null, "AdsControl.OnMaxSdkInitialized", true);
    
            if (showBanner && !string.IsNullOrEmpty(maxBannerAdUnitId))
            {
                InitializeBannerAds();
                LoadBannerAd();
            }
    
            if (showInterstitial && !string.IsNullOrEmpty(maxInterstitialAdUnitId))
            {
                InitializeInterstitialAds();
                LoadInterstitialAd();
            }
    
            if (!string.IsNullOrEmpty(maxRewardedAdUnitId))
            {
                InitializeRewardedAds();
                LoadRewardedAd();
            }
    
            onAdsInitialized?.Invoke();
        }

        public static void SyncMaxUserId(string afDeviceId = null, string source = null, bool force = false)
        {
            string resolvedUserId = string.IsNullOrEmpty(afDeviceId)
                ? AppsFlyerIdResolver.ResolveCurrentId()
                : afDeviceId;
            bool sdkInitialized = instance != null && instance.isMaxInitialized;

            if (string.IsNullOrEmpty(resolvedUserId))
            {
                Debug.LogWarning($"MAX user id sync skipped: source={source ?? "unknown"}, afDeviceId is empty");
                return;
            }

            if (!force && sdkInitialized && string.Equals(lastAppliedMaxUserId, resolvedUserId, StringComparison.Ordinal))
                return;

            MaxSdk.SetUserId(resolvedUserId);
            lastAppliedMaxUserId = resolvedUserId;
            Debug.Log($"MAX user id synced: source={source ?? "unknown"}, afDeviceId={resolvedUserId}");
        }
        #endregion
    
        #region ������
        private void InitializeBannerAds()
        {
            if (string.IsNullOrEmpty(maxBannerAdUnitId)) return;
    
            MaxSdk.CreateBanner(maxBannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerExtraParameter(maxBannerAdUnitId, "adaptive_banner", "true");
    
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailed;
        }
    
        public void LoadBannerAd()
        {
            if (IsRemoveAds() || !isMaxInitialized || !showBanner) return;
            if (string.IsNullOrEmpty(maxBannerAdUnitId)) return;
    
            MaxSdk.LoadBanner(maxBannerAdUnitId);
        }
    
        public void ShowBannerAd()
        {
            if (IsRemoveAds() || !isMaxInitialized || !isBannerLoaded || !showBanner) return;
    
            MaxSdk.ShowBanner(maxBannerAdUnitId);

            if (!isBannerShowing)
                ChallangeSuccessView.RecordAdSuccessProgress();

            isBannerShowing = true;
        }
    
        public void HideBannerAd()
        {
            if (!isMaxInitialized) return;
    
            MaxSdk.HideBanner(maxBannerAdUnitId);
            isBannerShowing = false;
        }
    
        public void DestroyBannerAd()
        {
            if (!isMaxInitialized) return;
    
            MaxSdk.DestroyBanner(maxBannerAdUnitId);
            isBannerShowing = false;
            isBannerLoaded = false;
        }
    
        private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            isBannerLoaded = true;
            if (!IsRemoveAds() && showBanner) ShowBannerAd();
        }
    
        private void OnBannerAdFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            isBannerLoaded = false;
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdLoadFail(EAdtype.Banner, EAdSource.Prop, PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel), AdDisplayFailed.AdLoadFailed,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
            Invoke("LoadBannerAd", 30f);
        }
    
        private static EAdSource GetAdSourceFromRewardType(string rewardType)
        {
            if (string.IsNullOrEmpty(rewardType)) return EAdSource.Prop;
            if (rewardType == "Prop_reward") return EAdSource.Prop;
            if (rewardType == "challenge_success_reward") return EAdSource.ChallengeSuccessReward;
            if (rewardType == "reward_success_reward") return EAdSource.RewardSuccessReward;
            if (rewardType == "force_reward") return EAdSource.force_reward;
            return EAdSource.Prop;
        }
    
    
        /// <summary>
        /// 广告累计类��㣺5/10/15/20 �ι������ɹ�ʱ�ϱ�
        /// </summary>
        private void ReportAdCumulativeMilestone(double revenue)
        {
            int count = PlayerPrefs.GetInt(PREF_TOTAL_AD_COUNT, 0) + 1;
            double accuRevenue = PlayerPrefs.GetFloat(PREF_ACCU_AD_REVENUE, 0f) + (float)revenue;
            PlayerPrefs.SetInt(PREF_TOTAL_AD_COUNT, count);
            PlayerPrefs.SetFloat(PREF_ACCU_AD_REVENUE, (float)accuRevenue);
            PlayerPrefs.Save();
            FacadeAppsFlyerExtend.SendAdSuccessCountEvent?.Invoke(count, revenue);
    
            if (count == 5) TDAnalyticsMgr.Instance.Times_5_Ad(accuRevenue);
            else if (count == 10) TDAnalyticsMgr.Instance.Times_10_Ad(accuRevenue);
            else if (count == 15) TDAnalyticsMgr.Instance.Times_15_Ad(accuRevenue);
            else if (count == 20) TDAnalyticsMgr.Instance.Times_20_Ad(accuRevenue);
        }
        #endregion
    
        #region ????????????
        private void InitializeRewardedAds()
        {
            if (string.IsNullOrEmpty(maxRewardedAdUnitId)) return;
    
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        }
        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                Debug.LogWarning("OnRewardedAdRevenuePaidEvent: adInfo is null");
                return;
            }

            Debug.Log("========== OnRewardedAdRevenuePaidEvent ==========");
            FacadeAppsFlyerExtend.SendAdRevenue?.Invoke(adInfo, EAdType.EReward);
            TDAnalyticsMgr.Instance.AdRevenuePaid(EAdtype.Reward, adInfo.Revenue, adInfo.RevenuePrecision, GetAdSourceFromRewardType(currentRewardType), adInfo.NetworkName, adInfo.NetworkName);
            Debug.LogError($"Rewarded广告收益回调: revenue={adInfo.Revenue}");
            Debug.Log("==========End  OnRewardedAdRevenuePaidEvent ==========");
        }
        public void LoadRewardedAd()
        {
            if (!isMaxInitialized) return;
            if (string.IsNullOrEmpty(maxRewardedAdUnitId)) return;
    
            MaxSdk.LoadRewardedAd(maxRewardedAdUnitId);
        }
    
        public void ShowRewardedAd(string rewardType, Action<bool> onComplete = null, bool autoShowWithdrawMissionView = true)
        {
            if (!isMaxInitialized)
            {
                onComplete?.Invoke(false);
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                return;
            }
    
            if (MaxSdk.IsRewardedAdReady(maxRewardedAdUnitId))
            {
                currentRewardType = rewardType;
                currentRewardedCallback = onComplete;
                currentRewardShouldAutoShowWithdrawMissionView = autoShowWithdrawMissionView;
                MaxSdk.ShowRewardedAd(maxRewardedAdUnitId);
            }
            else
            {
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                LoadRewardedAd();
                onComplete?.Invoke(false);
                currentRewardShouldAutoShowWithdrawMissionView = true;
            }
        }
    
        private void OnRewardedAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            isRewardedReady = true;
            onRewardedAdReady?.Invoke();
        }
    
        private void OnRewardedAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            isRewardedReady = false;
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdLoadFail(EAdtype.Reward, EAdSource.Prop, PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel), AdDisplayFailed.AdLoadFailed,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
            Invoke("LoadRewardedAd", 3f);
        }
    
        private void OnRewardedAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            isRewardedReady = false;
          // GameManager.instance?.PausePouringTimer();
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdStart(EAdtype.Reward, GetAdSourceFromRewardType(currentRewardType), adInfo.Revenue, adInfo.Placement, adInfo.RevenuePrecision,  PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
        }
    
        private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LoadRewardedAd();
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdFail(EAdtype.Reward, GetAdSourceFromRewardType(currentRewardType), "广告被关闭", adInfo.Placement, adInfo.RevenuePrecision);
            currentRewardedCallback?.Invoke(false);
            currentRewardedCallback = null;
            refuseFailCallback?.Invoke("广告被关闭");
            refuseFailCallback = null;
            refuseSuccessCallback = null;
            currentRewardType = "";
            currentRewardShouldAutoShowWithdrawMissionView = true;
            currentRefuseShouldAutoShowWithdrawMissionView = true;
        }
    
        private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LoadRewardedAd();
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdFail(EAdtype.Reward, GetAdSourceFromRewardType(currentRewardType), errorInfo.Message ?? "广告展示失败", adInfo.Placement, adInfo.RevenuePrecision);
            currentRewardedCallback?.Invoke(false);
            currentRewardedCallback = null;
            refuseFailCallback?.Invoke(errorInfo.Message);
            refuseFailCallback = null;
            refuseSuccessCallback = null;
            currentRewardType = "";
            currentRewardShouldAutoShowWithdrawMissionView = true;
            currentRefuseShouldAutoShowWithdrawMissionView = true;
        }
    
        private void OnRewardedAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            double rewardRevenue = adInfo != null ? adInfo.Revenue : 0d;

            TDAnalyticsMgr.Instance.AdComplete(EAdtype.Reward, GetAdSourceFromRewardType(currentRewardType), rewardRevenue, adInfo.Placement, adInfo.RevenuePrecision,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
            ChallangeSuccessView.RecordAdSuccessProgress();
            GameManagerWZ gameManager = GameManagerWZ.instance;
            gameManager?.RecordStageAdWatch();
            ReportAdCumulativeMilestone(rewardRevenue);
           // WithdrawApiService.ReportRewardedAdWalletIncomeIfNeeded(rewardRevenue);
            if (gameManager != null
                && gameManager.currentStage == 3
                && UIManager.instance != null
                && gameManager.IsStageCompleted(3))
            {
                bool fromClearAd = currentRewardType == "challenge_success_reward"
                                   || currentRewardType == "reward_success_reward";
                gameManager.NotifyStage3AdTargetReached(fromClearAd);
            }

            currentRewardedCallback?.Invoke(true);
            currentRewardedCallback = null;
            refuseSuccessCallback?.Invoke();
            refuseSuccessCallback = null;
            refuseFailCallback = null;
            currentRewardType = "";
            currentRewardShouldAutoShowWithdrawMissionView = true;
            currentRefuseShouldAutoShowWithdrawMissionView = true;
        }
    
        public bool IsRewardedVideoAvailable()
        {
            return isMaxInitialized && isRewardedReady;
        }
        #endregion
    
        #region 插屏广告
        private void InitializeInterstitialAds()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
        }
        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                Debug.LogWarning("OnInterstitialRevenuePaidEvent: adInfo is null");
                return;
            }

            FacadeAppsFlyerExtend.SendAdRevenue?.Invoke(adInfo, EAdType.EInterstitial);
            TDAnalyticsMgr.Instance.AdRevenuePaid(EAdtype.Reward, adInfo.Revenue, adInfo.RevenuePrecision, GetAdSourceFromRewardType(currentRewardType), adInfo.NetworkName, adInfo.NetworkName);
            Debug.LogError($"Interstitial广告收益回调: revenue={adInfo.Revenue:F4}, placement={adInfo.Placement}, adUnitId={adInfo.AdUnitIdentifier}, network={adInfo.NetworkName}");
        }
        public void LoadInterstitialAd()
        {
            if (IsRemoveAds() || !isMaxInitialized || !showInterstitial) return;
            if (string.IsNullOrEmpty(maxInterstitialAdUnitId)) return;
    
            MaxSdk.LoadInterstitial(maxInterstitialAdUnitId);
        }
    
        public void ShowInterstitialAd()
        {
            if (IsRemoveAds() || !isMaxInitialized || !isInterstitialReady || !showInterstitial)
            {
                return;
            }
    
            if (MaxSdk.IsInterstitialReady(maxInterstitialAdUnitId))
            {
                MaxSdk.ShowInterstitial(maxInterstitialAdUnitId);
            }
            else
            {
                ShowNotice(LocalizationManager.Instance.GetText("1032"));
                LoadInterstitialAd();
            }
        }
    
        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            isInterstitialReady = true;
            onInterstitialAdReady?.Invoke();
        }
    
        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            isInterstitialReady = false;
            // @todo 埋点
            TDAnalyticsMgr.Instance.AdLoadFail(EAdtype.Interstitial, EAdSource.Prop, PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel), AdDisplayFailed.AdLoadFailed,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
            Invoke("LoadInterstitialAd", 3f);
        }
    
        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            isInterstitialReady = false;
           // GameManager.instance?.PausePouringTimer();
            // 埋点
            TDAnalyticsMgr.Instance.AdStart(EAdtype.Interstitial, EAdSource.Prop, adInfo.Revenue, adInfo.Placement, adInfo.RevenuePrecision,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
        }
    
        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double interstitialRevenue = adInfo != null ? adInfo.Revenue : 0d;

            LoadInterstitialAd();
            // 埋点
            TDAnalyticsMgr.Instance.AdComplete(EAdtype.Interstitial, EAdSource.Prop, interstitialRevenue, adInfo.Placement, adInfo.RevenuePrecision,PlayerPrefs.GetInt(FacadePlayerPrefExtend.currentLevel));
            ChallangeSuccessView.RecordAdSuccessProgress();
            GameManagerWZ gameManager = GameManagerWZ.instance;
            gameManager?.RecordStageAdWatch();
            ReportAdCumulativeMilestone(interstitialRevenue);
            Debug.Log($"Interstitial广告完成回调: revenue={interstitialRevenue:F4}, placement={adInfo?.Placement}, adUnitId={adInfo?.AdUnitIdentifier}, network={adInfo?.NetworkName}");
            if (gameManager != null
                && gameManager.currentStage == 3
                && UIManager.instance != null
                && gameManager.IsStageCompleted(3))
            {
                // 插屏一般不是通关广告，达标后继续当前关。
                gameManager.NotifyStage3AdTargetReached(fromLevelClearAd: false);
            }

            refuseSuccessCallback?.Invoke();
            refuseSuccessCallback = null;
            refuseFailCallback = null;
            currentRefuseShouldAutoShowWithdrawMissionView = true;
            currentRewardShouldAutoShowWithdrawMissionView = true;
        }
    
        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LoadInterstitialAd();
            // 埋点
            TDAnalyticsMgr.Instance.AdFail(EAdtype.Interstitial, EAdSource.Prop, errorInfo.Message ?? "????????????", adInfo.Placement, adInfo.RevenuePrecision);
            refuseFailCallback?.Invoke(errorInfo.Message);
            refuseFailCallback = null;
            refuseSuccessCallback = null;
            currentRefuseShouldAutoShowWithdrawMissionView = true;
            currentRewardShouldAutoShowWithdrawMissionView = true;
        }
        #endregion
    
        #region 移除广告
        public void RemoveAds()
        {
            PlayerPrefs.SetInt("removeAds", 1);
            PlayerPrefs.Save();
        }
    
        public bool IsRemoveAds()
        {
            return PlayerPrefs.GetInt("removeAds", 0) == 1;
        }
        #endregion
    }
}
