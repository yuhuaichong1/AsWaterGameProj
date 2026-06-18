using System;
using UnityEngine;
using UnityEngine.UI;
using ThinkingData.Analytics;
using static MaxSdkBase;
using AppsFlyerSDK;
using XrCode;


namespace XrSDK
{
    public class MaxSdkFrame : BaseModule
    {
        private string maxSdkKey;

        private string interstitialAdUnitId;
        private string rewardedAdUnitId;
        private string bannerAdUnitId;
        private string mRecAdUnitId;
        private string appOpenId;

        private bool isBannerShowing;
        private bool isMRecShowing;

        private int interstitialRetryAttempt;
        private int rewardedRetryAttempt;

        private double RewardEcpm;
        private double InterEcpm;

        private string RAdName;
        private double RAdIncome;
        private string IAdName;
        private double IAdIncome;

        private SAndroidToastHelper SAndroidToastHelper;
        private string toastText;

        private TDAnalyticsManager TDAnalyticsManager;

        public static bool IsMaxInit = false;

        private double InsParity;//插屏比价用收入
        private double RwdParity;//激励比价用收入

        public MaxSdkFrame(MaxSdkConf data)
        {
            if (data != null)
            {
                maxSdkKey = data.MaxSdkKey;
                interstitialAdUnitId = data.InterstitialAdId;
                rewardedAdUnitId = data.RewardedAdId;
                bannerAdUnitId = data.BannerAdId;
                mRecAdUnitId = data.MRecAdId;
                appOpenId = data.AppOpenAdId;
            }

            InsParity = 0;
            RwdParity = 0;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            TDAnalyticsManager = ModuleMgr.Instance.TDAnalyticsManager;
            //showInterstitialButton.onClick.AddListener(ShowInterstitialAd);
            //showRewardedButton.onClick.AddListener(ShowRewardedAd);
            //showBannerButton.onClick.AddListener(ToggleBannerVisibilityEvent);
            //showMRecButton.onClick.AddListener(ToggleMRecVisibility);
            //mediationDgerButton.onClick.AddListener(MaxSdk.ShowMediationDger);

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                IsMaxInit = true;
                // AppLovin SDK is initialized, configure and start loading ads.
                D.Log("MAX SDK Initialized");

                if (!string.IsNullOrEmpty(interstitialAdUnitId)) InitializeInterstitialAds();
                if (!string.IsNullOrEmpty(rewardedAdUnitId)) InitializeRewardedAds();
                if (!string.IsNullOrEmpty(bannerAdUnitId)) InitializeBannerAds();
                if (!string.IsNullOrEmpty(mRecAdUnitId)) InitializeMRecAds();
                if (!string.IsNullOrEmpty(appOpenId)) InitializeAppOpenAds();

                // Initialize Adjust SDK
                //AdjustConfig adjustConfig = new AdjustConfig("YourAppToken", AdjustEnvironment.Sandbox);
                //Adjust.start(adjustConfig);
#if UNITY_ANDROID && !UNITY_EDITOR
                SAndroidToastHelper = new SAndroidToastHelper();
#endif
            };

            FacadeMaxSdkExtend.GetParity += GetParity;
            FacadeAd.GetROIAdRevenue += GetParity;
            FacadeAd.GetRewardAdReady += GetRewardAdReady;
            FacadeAd.GetInterAdReady += GetInterAdReady;
            FacadeAd.GetAppOpenAdReady += GetAppOpenAdReady;

            ConnectTD();
            MaxSdk.SetSdkKey(maxSdkKey);
            MaxSdk.InitializeSdk();
        }

        #region Interstitial Ad Methods

        private void InitializeInterstitialAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;

            FacadeAd.ShowInterAd += ShowInterstitial;
            FacadeMaxSdkExtend.ShowInterstitialAd += ShowInterstitial;
            FacadeMaxSdkExtend.GetInterAdRevenue += () =>
            {
                return InterEcpm;
            };
            FacadeMaxSdkExtend.GetInterstitialReady += GetInterstitialReady;

            // Load the first interstitial
            LoadInterstitial();
        }

        void LoadInterstitial()
        {
            //interstitialStatusText.text = "Loading...";
            MaxSdk.LoadInterstitial(interstitialAdUnitId);
        }

        void ShowInterstitial()
        {
            if (MaxSdk.IsInterstitialReady(interstitialAdUnitId))
            {
                if (string.IsNullOrEmpty(toastText))
                    toastText = ModuleMgr.Instance.LanguageMod.GetText("10128");
                if (SAndroidToastHelper != null)
                    SAndroidToastHelper.PlayToast(toastText);
                //interstitialStatusText.text = "Showing";
                MaxSdk.ShowInterstitial(interstitialAdUnitId);
            }
            else
            {
                D.Error("inter ad not ready");
                FacadeMaxSdkExtend.OnInterstitialAdNotReady?.Invoke();
                FacadeAd.InterstitialAdNotReady?.Invoke();
                LoadInterstitial();
                //interstitialStatusText.text = "Ad not ready";
            }
        }

        private bool GetInterstitialReady()
        {
            return MaxSdk.IsInterstitialReady(interstitialAdUnitId);
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
            //interstitialStatusText.text = "Loaded";
            D.Log("Interstitial loaded");
            // Reset retry attempt
            interstitialRetryAttempt = 0;

            InterEcpm = adInfo.Revenue * 1000;

            IAdIncome = adInfo.Revenue;
            IAdName = adInfo.NetworkName;
            InsParity = adInfo.Revenue;

            FacadeMaxSdkExtend.OnInterstitialAdLoadedEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.InterstitialAdLoaded?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("Interstitial ad displayed");
            FacadeMaxSdkExtend.OnInterstitialAdDisplayedEvent?.Invoke(adUnitId, adInfo);
            FacadeAd.InterstitialAdDisplayed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            interstitialRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));

            //interstitialStatusText.text = "Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...";
            D.Log("Interstitial failed to load with error code: " + errorInfo.Code);
            //广播插屏广告加载失败事件
            MonoInst.Instance.Invoke("LoadInterstitial", (float)retryDelay);

            FacadeMaxSdkExtend.OnInterstitialAdLoadFailedEvent?.Invoke(adUnitId, errorInfo);
            FacadeAd.InterstitialAdLoadFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);
        }

        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad failed to display. We recommend loading the next ad
            D.Log("Interstitial failed to display with error code: " + errorInfo.Code);
            InsParity = 0;
            //广播插屏广告展示失败事件
            LoadInterstitial();

            FacadeMaxSdkExtend.OnInterstitialAdFailedToDisplayEvent?.Invoke(adUnitId, errorInfo, adInfo);
            FacadeAd.InterstitialAdDisplayFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);
        }

        private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is hidden. Pre-load the next ad
            D.Log("Interstitial ad dismissed");
            D.Log($"Interstitial ad dismissed  ____________________________ {adInfo.Revenue}");
            InsParity = 0;
            //广播插屏广告关闭事件
            LoadInterstitial();

            FacadeMaxSdkExtend.OnInterstitialAdDismissedEvent?.Invoke(adUnitId, adInfo);
            FacadeAd.InterstitialAdClosed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("Interstitial ad clicked");

            FacadeMaxSdkExtend.OnInterstitialAdClickedEvent?.Invoke(adUnitId, adInfo);
            FacadeAd.InterstitialAdClicked?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad revenue paid. Use this callback to track user revenue.
            D.Log("Interstitial ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            

            FacadeMaxSdkExtend.OnInterstitialAdRevenuePaidEvent?.Invoke(adUnitId, adInfo);

            FacadeAppsFlyerExtend.SendAdRevenue?.Invoke(EAppsFlyerAdType.EInterstitial, adInfo.AdUnitIdentifier, adInfo.Placement, "monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
            FacadeAd.InterstitialAdRevenuePaid?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
            FacadeAd.InterstitialAdCompleted?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        #endregion

        #region Rewarded Ad Methods

        private void InitializeRewardedAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

            FacadeAd.ShowRewardAd += ShowRewardedAd;
            FacadeMaxSdkExtend.ShowRewardedAd += ShowRewardedAd;
            FacadeMaxSdkExtend.GetRewardedAdRevenue += () =>
            {
                return RewardEcpm;
            };
            FacadeMaxSdkExtend.GetRewardedReady += GetRewardedReady;

            // Load the first RewardedAd
            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            MaxSdk.LoadRewardedAd(rewardedAdUnitId);
        }

        private void ShowRewardedAd()
        {
            if (MaxSdk.IsRewardedAdReady(rewardedAdUnitId))
            {
                if (string.IsNullOrEmpty(toastText))
                {
                    toastText = ModuleMgr.Instance.LanguageMod.GetText("10285");
                }

                if (SAndroidToastHelper != null)
                    SAndroidToastHelper.PlayToast(toastText);
                MaxSdk.ShowRewardedAd(rewardedAdUnitId);
            }
            else
            {
                D.Error("reward ad not ready");
                FacadeMaxSdkExtend.OnRewardAdNotReadyEvent?.Invoke();
                FacadeAd.RewardAdNotReady?.Invoke();
                LoadRewardedAd();
            }
        }

        private bool GetRewardedReady()
        {
            return MaxSdk.IsRewardedAdReady(rewardedAdUnitId);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            D.Log("Rewarded ad loaded");

            // Reset retry attempt
            rewardedRetryAttempt = 0;

            //GetECPMCritter Crush Quest
            RewardEcpm = adInfo.Revenue * 1000;

            RAdIncome = adInfo.Revenue;
            RAdName = adInfo.NetworkName;
            RwdParity = adInfo.Revenue;

            //D.Error("广告加载完毕：" + adInfo.DspName + " | " + adInfo.Revenue);

            FacadeMaxSdkExtend.OnRewardedAdLoadEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.RewardAdLoaded?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            rewardedRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));

            D.Log("Rewarded ad failed to load with error code: " + errorInfo.Code);

            MonoInst.Instance.Invoke("LoadRewardedAd", (float)retryDelay);
            //广播激励视频加载失败事件
            FacadeMaxSdkExtend.OnRewardedAdLoadFailEvent?.Invoke(adUnitId, errorInfo);

            FacadeAd.RewardAdLoadFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad failed to display. We recommend loading the next ad
            D.Log("Rewarded ad failed to display with error code: " + errorInfo.Code);
            RwdParity = 0;
            LoadRewardedAd();
            //广播激励视频显示失败事件
            FacadeMaxSdkExtend.OnRewardedAdFailToDisplayEvent?.Invoke(adUnitId, errorInfo, adInfo);

            FacadeAd.RewardAdDisplayFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("Rewarded ad displayed");

            FacadeMaxSdkExtend.OnRewardedAdDisplayEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.RewardAdDisplayed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("Rewarded ad clicked");

            FacadeMaxSdkExtend.OnRewardAdClickEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.RewardAdClicked?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is hidden. Pre-load the next ad
            D.Log("Rewarded ad dismissed");
            RwdParity = 0;
            LoadRewardedAd();
            FacadeMaxSdkExtend.OnRewardAdHiddenEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.RewardAdClosed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad was displayed and user should receive the reward
            D.Log("Rewarded ad received reward");

            FacadeMaxSdkExtend.OnRewardAdReceivRewardEvent?.Invoke(adUnitId, reward, adInfo);

            FacadeAd.RewardAdReceivedReward?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision, reward.Amount);
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad revenue paid. Use this callback to track user revenue.
            D.Log("Rewarded ad revenue paid");
            D.Log($" _________________________________adInfo:  {adInfo.ToString()}");
            D.Log($" _________________________________adInfo.WaterfallInfo  {adInfo.WaterfallInfo.ToString()}");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            FacadeMaxSdkExtend.OnRewardAdRevenuePaidedEvent?.Invoke(adUnitId, adInfo);

            FacadeAppsFlyerExtend.SendAdRevenue?.Invoke(EAppsFlyerAdType.EReward, adInfo.AdUnitIdentifier, adInfo.Placement, "monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);

            FacadeAd.RewardAdRevenuePaid?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
            FacadeAd.RewardAdCompleted?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.RevenuePrecision);
        }

        #endregion

        #region Banner Ad Methods

        private void InitializeBannerAds()
        {
            // Attach Callbacks
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

            FacadeMaxSdkExtend.ToggleBannerVisibilityEvent += ToggleBannerVisibility;

            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
            // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
            MaxSdk.CreateBanner(bannerAdUnitId, MaxSdkBase.BannerPosition.TopCenter);

            // Set background or background color for banners to be fully functional.
            MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, Color.black);
        }

        private void ToggleBannerVisibility()
        {
            if (!isBannerShowing)
            {
                MaxSdk.ShowBanner(bannerAdUnitId);
                //showBannerButton.GetComponentInChildren<Text>().text = "Hide Banner";
            }
            else
            {
                MaxSdk.HideBanner(bannerAdUnitId);
                //showBannerButton.GetComponentInChildren<Text>().text = "Show Banner";
            }

            isBannerShowing = !isBannerShowing;
        }

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad is ready to be shown.
            // If you have already called MaxSdk.ShowBanner(BannerAdId) it will automatically be shown on the next ad refresh.
            D.Log("Banner ad loaded");
            //横幅广告加载完成
            FacadeMaxSdkExtend.BannerAdLoadEvent?.Invoke();
        }

        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Banner ad failed to load. MAX will automatically try loading a new ad internally.
            D.Log("Banner ad failed to load with error code: " + errorInfo.Code);
            //横幅广告加载失败
            FacadeMaxSdkExtend.BannerAdFailEvent?.Invoke(adUnitId, errorInfo.Code);

        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("Banner ad clicked");
        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad revenue paid. Use this callback to track user revenue.
            D.Log("Banner ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
        }

        #endregion

        #region MREC Ad Methods

        private void InitializeMRecAds()
        {
            // Attach Callbacks
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;

            FacadeMaxSdkExtend.ToggleMRecVisibility += ToggleMRecVisibility;

            // MRECs are automatically sized to 300x250.
            MaxSdk.CreateMRec(mRecAdUnitId, MaxSdkBase.AdViewPosition.BottomCenter);
        }

        private void ToggleMRecVisibility()
        {
            if (!isMRecShowing)
            {
                MaxSdk.ShowMRec(mRecAdUnitId);
                //showMRecButton.GetComponentInChildren<Text>().text = "Hide MREC";
            }
            else
            {
                MaxSdk.HideMRec(mRecAdUnitId);
                //showMRecButton.GetComponentInChildren<Text>().text = "Show MREC";
            }

            isMRecShowing = !isMRecShowing;
        }

        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MRec ad is ready to be shown.
            // If you have already called MaxSdk.ShowMRec(MRecAdId) it will automatically be shown on the next MRec refresh.
            D.Log("MRec ad loaded");
            //MRec 广告加载完成
            FacadeMaxSdkExtend.MRecAdLoadEvent?.Invoke();
        }

        private void OnMRecAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // MRec ad failed to load. MAX will automatically try loading a new ad internally.
            D.Log("MRec ad failed to load with error code: " + errorInfo.Code);
            //MRec 广告加载失败
            FacadeMaxSdkExtend.MRecAdFailEvent?.Invoke(adUnitId, errorInfo.Code);
        }

        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            D.Log("MRec ad clicked");
        }

        private void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // MRec ad revenue paid. Use this callback to track user revenue.
            D.Log("MRec ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
        }

        #endregion

        #region AppOpen Ad Methods

        private void InitializeAppOpenAds()
        {
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaidEvent;

            FacadeMaxSdkExtend.ShowAppOpenAd += ShowAppOpenAd;

            FacadeMaxSdkExtend.GetAppOpenAdReady += GetAppOpenAdReady;

            LoadAppOpenAd();
        }

        private bool GetAppOpenAdReady()
        {
            return MaxSdk.IsAppOpenAdReady(appOpenId);
        }

        private void LoadAppOpenAd()
        {
            MaxSdk.LoadAppOpenAd(appOpenId);
        }

        private void ShowAppOpenAd()
        {
            if(MaxSdk.IsAppOpenAdReady(appOpenId))
            {
                MaxSdk.ShowAppOpenAd(appOpenId);
            }
            else
            {
                D.Error("appOpen ad not ready");
                FacadeMaxSdkExtend.OnAppOpenAdNotReady?.Invoke();
            }
        }

        private void OnAppOpenAdLoadedEvent(string adUnitId, AdInfo adInfo)
        {
            D.Log("Rewarded ad loaded");

            RewardEcpm = adInfo.Revenue * 1000;

            RAdIncome = adInfo.Revenue;
            RAdName = adInfo.NetworkName;
            RwdParity = adInfo.Revenue;

            FacadeMaxSdkExtend.OnAppOpenAdLoadedEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.OnAppOpenAdLoaded?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.Placement);

        }

        private void OnAppOpenAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
        {
            FacadeMaxSdkExtend.OnAppOpenAdLoadFailedEvent?.Invoke(adUnitId, errorInfo);

            FacadeAd.OnAppOpenLoadFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);

        }

        private void OnAppOpenAdDisplayedEvent(string adUnitId, AdInfo adInfo)
        {
            FacadeMaxSdkExtend.OnAppOpenAdDisplayedEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.OnAppOpenDisplayed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.Placement);
        }

        private void OnAppOpenAdDisplayFailedEvent(string adUnitId, ErrorInfo errorInfo, AdInfo adInfo)
        {
            FacadeMaxSdkExtend.OnAppOpenAdDisplayFailedEvent?.Invoke(adUnitId, errorInfo, adInfo);

            FacadeAd.OnAppOpenDisplayFailed?.Invoke(errorInfo.Code.ToString(), errorInfo.Message);

        }

        private void OnAppOpenAdHiddenEvent(string adUnitId, AdInfo adInfo)
        {
            FacadeMaxSdkExtend.OnRewardAdHiddenEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.OnRewardAdClosed?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.Placement);
        }

        private void OnAppOpenAdClickedEvent(string adUnitId, AdInfo adInfo)
        {
            FacadeMaxSdkExtend.OnAppOpenAdClickedEvent?.Invoke(adUnitId, adInfo);

            FacadeAd.OnAppOpenClicked?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.Placement);
        }

        private void OnAppOpenAdRevenuePaidEvent(string adUnitId, AdInfo adInfo)
        {
            double revenue = adInfo.Revenue;

            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode;
            string networkName = adInfo.NetworkName;
            string adUnitIdentifier = adInfo.AdUnitIdentifier;
            string placement = adInfo.Placement;

            FacadeMaxSdkExtend.OnAppOpenAdRevenuePaidEvent?.Invoke(adUnitId, adInfo);

            FacadeAppsFlyerExtend.SendAdRevenue?.Invoke(EAppsFlyerAdType.EAppOpen, adInfo.AdUnitIdentifier, adInfo.Placement, "monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);

            FacadeAd.OnAppOpenRevenuePaid?.Invoke(adInfo.NetworkName, adInfo.Revenue, adInfo.Revenue * 1000, adInfo.Placement);

        }

        #endregion

        private void ConnectTD()
        {
            //TDConfig config = new TDConfig("APPID", "SERVER");
            //TDAnalytics.Init(config);
            var distinctId = TDAnalytics.GetDistinctId();
            MaxSdk.SetUserId(distinctId);
        }

        /// <summary>
        /// 比较激励和插屏的收入（加载时）
        /// </summary>
        /// <returns>是否插屏大于激励</returns>
        private bool GetParity()
        {
            D.Log("比价：插屏价格：" + InsParity + " | 激励价格：" + RwdParity);
            return InsParity >= RwdParity;
        }

        private bool GetInterAdReady()
        {
            return MaxSdk.IsInterstitialReady(interstitialAdUnitId);
        }

        private bool GetRewardAdReady()
        {
            return MaxSdk.IsRewardedAdReady(rewardedAdUnitId);
        }
    }
}