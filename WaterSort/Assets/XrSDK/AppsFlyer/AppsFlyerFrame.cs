using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;
using System;
using static MaxSdkBase;
using XrCode;

namespace XrSDK
{
    public class AppsFlyerFrame : BaseModule
    {
        private string monoObjPath;
        private string devKey;
        private string appID;
        private string UWPAppID;
        private string macOSAppID;
        private bool isDebug;
        private bool getConversionData;

        private AppsFlyerMono AppsFlyerMono;
        private bool justOne = false;

        public AppsFlyerFrame(AppsFlyerConf data)
        {
            if (data != null)
            {
                monoObjPath = data.monoObjPath;
                devKey = data.devKey;
                appID = data.appID;
                UWPAppID = data.UWPAppID;
                macOSAppID = data.macOSAppID;
                isDebug = data.isDebug;
                getConversionData = data.getConversionData;
            }
        }

        protected override void OnLoad()
        {
            FacadeAppsFlyerExtend.Init += Init;

            Init();
        }

        private void Init()
        {
            if (justOne)
                return;
            else
                justOne = true;

            D.Log("Appsflyer初始化开始");

            AddEvent();

            AppsFlyerMono = GameObject.Find(monoObjPath).GetComponent<AppsFlyerMono>();
            AppsFlyerMono.AddCallback(OnConversionDataSuccess, OnConversionDataFail, OnAppOpenAttribution, OnAppOpenAttributionFailure);

            Guid guid = Guid.NewGuid();
            string guidString = guid.ToString();
            AppsFlyer.setCustomerUserId(guidString);

            AppsFlyer.setIsDebug(isDebug);
#if UNITY_WSA_10_0 && !UNITY_EDITOR
            AppsFlyer.initSDK(devKey, UWPAppID, getConversionData ? AppsFlyerMono : null);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            AppsFlyer.initSDK(devKey, macOSAppID, getConversionData ? AppsFlyerMono : null);
#else
            AppsFlyer.initSDK(devKey, appID, getConversionData ? AppsFlyerMono : null);
#endif

            AppsFlyer.startSDK();
        }

        /// <summary>
        /// 添加接口
        /// </summary>
        private void AddEvent()
        {
            FacadeAppsFlyerExtend.SendAdSuccessCountEvent = SendAdSuccessCountEvent;
            FacadeAppsFlyerExtend.SendAdRevenue = SendAdRevenue;
        }

        /// <summary>
        /// 移除接口
        /// </summary>
        private void RemoveEvent()
        {
            FacadeAppsFlyerExtend.SendAdSuccessCountEvent = null;
            FacadeAppsFlyerExtend.SendAdRevenue = null;
        }

        /// <summary>
        /// 广告成功播放接口
        /// </summary>
        /// <param name="adCount">播放数量</param>
        private void SendAdSuccessCountEvent(int adCount)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("adCount", adCount.ToString());
            AppsFlyer.sendEvent("AdSuccessEvent", dic);
        }

        /// <summary>
        /// 广告收入
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="adUnitIdentifier"></param>
        /// <param name="placement"></param>
        /// <param name="monetization"></param>
        /// <param name="Mnetwork"></param>
        /// <param name="currencyType"></param>
        /// <param name="revenue"></param>
        private void SendAdRevenue(EAppsFlyerAdType eAdType, string adUnitIdentifier, string placement, string monetization, MediationNetwork Mnetwork, string currencyType, double revenue)
        {
            string adType = GetAdType(eAdType);
            Dictionary<string, string> additionalParams = new Dictionary<string, string>
            {
                //{ AdRevenueScheme.COUNTRY, waterfallInfo},//adInfo.WaterfallInfo
                { AdRevenueScheme.AD_UNIT, adUnitIdentifier },//adInfo.AdUnitIdentifier
                { AdRevenueScheme.AD_TYPE, adType },
                { AdRevenueScheme.PLACEMENT, placement }//adInfo.Placement
            };
            var logRevenue = new AFAdRevenueData(monetization, Mnetwork, currencyType, revenue);//"monetizationNetworkEx", MediationNetwork.ApplovinMax, "USD", adInfo.Revenue
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
        }

        #region 归因相关

        /// <summary>
        /// 归因获取成功
        /// </summary>
        /// <param name="conversionData">归因数据</param>
        private void OnConversionDataSuccess(Dictionary<string, object> conversionData)
        {
            CompetitionManager.Instance.attributionData = conversionData;
            CompetitionManager.Instance.SkipCompetition(CompetitionKey.IFAF);
            //TDAnalyticsManager.Instance.SetAttributionData(conversionData);
        }

        /// <summary>
        /// 归因获取失败
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnConversionDataFail(string error)
        {

        }

        /// <summary>
        /// 归因？获取成功
        /// </summary>
        /// <param name="attributionData">归因？数据</param>
        private void OnAppOpenAttribution(Dictionary<string, object> attributionData)
        {

        }

        /// <summary>
        /// 归因？获取失败
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnAppOpenAttributionFailure(string error)
        {

        }

        #endregion

        private string GetAdType(EAppsFlyerAdType eAdType)
        {
            switch (eAdType)
            {
                case EAppsFlyerAdType.EReward:
                    return "reward";
                case EAppsFlyerAdType.EInterstitial:
                    return "interstitial";
                case EAppsFlyerAdType.EBanner:
                    return "banner";
                case EAppsFlyerAdType.ERec:
                    return "rec";
            }
            return "";
        }

        protected override void OnDispose()
        {
            FacadeAppsFlyerExtend.Init = null;
            RemoveEvent();
        }
    }
}