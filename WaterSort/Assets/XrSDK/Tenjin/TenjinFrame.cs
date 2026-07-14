using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XrCode;

namespace XrSDK
{
    public class TenjinFrame : BaseModule
    {
        private int appSubversion;
        private BaseTenjin instance;
        private string SDK;
        private AppStoreType appStoreType;
        private bool ifDebug;
        private bool ifOptIn;
        private bool ifUseCMP;

        public TenjinFrame(TenjinConf data)
        {
            if (data != null)
            {
                appSubversion = data.AppSubversioin;
                SDK = data.SDK;
                appStoreType = data.appStoreType;
                ifDebug = data.ifDebug;
                ifOptIn = data.ifOptIn;
                ifUseCMP = data.ifUseCMP;
            }
        }

        protected override void OnLoad()
        {
            Init();
        }

        private void Init()
        {
            AddEvent();

            instance = GetTenjinInstance();

            if (ifDebug) instance.DebugLogs();
            if(ifOptIn) instance.OptIn();
            if(ifUseCMP) instance.OptInOutUsingCMP();

            Connect();
        }

        public BaseTenjin GetTenjinInstance()
        {
            if (appSubversion != 0)
            {
                return Tenjin.getInstanceWithAppSubversion(SDK, appSubversion);
            }
            return Tenjin.getInstance(SDK);
        }

        private void Connect()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Debug.LogError("IOS is temporarily missing, it will be supplemented in the future"):
            //if (requestTrackingAuthorization)
            //{
            //    instance.RequestTrackingAuthorizationWithCompletionHandler((status) =>
            //    {
            //        instance.Connect();
            //    });
            //}
            //else
            //{
            //    instance.Connect();
            //}
            //GetAttributionInfo();
#elif UNITY_ANDROID && !UNITY_EDITOR
            instance.Connect();
            if (appStoreType != AppStoreType.unspecified)
            {
                instance.SetAppStoreType(appStoreType);
            }
            GetAttributionInfo();
            CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
#else
            instance.Connect();
            GetAttributionInfo();
            CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
#endif
        }

        private void SetEvent(string key)
        {
            instance.SendEvent(key);
        }

        private void SetEvent2(string key, string value)
        {
            instance.SendEvent(key, value);
        }

        private void GetAttributionInfo()
        {
            instance.GetAttributionInfo((Dictionary<string, string> data) => 
            {
                if (data.ContainsKey("ad_network"))
                {
                    Dictionary<string, object> arrtMsg = new Dictionary<string, object>();
                    foreach (string key in data.Keys)
                    {
                        arrtMsg.Add(key, data[key]);
                    }
                    CompetitionManager.Instance.attributionData = arrtMsg;
                    CompetitionManager.Instance.SkipCompetition(CompetitionKey.IFAF);
                }
                else
                {
                    D.Error("Tenjin未拿到归因数据");
                }
            });
        }

        private void AppLovinImpressionFromJSON(TenjinAdImpressionJson impressionData)
        {
            //TenjinAdImpressionJson impressionData = new TenjinAdImpressionJson();
            //impressionData.revenue = adInfo.Revenue;
            //impressionData.ad_revenue_currency = "USD";
            //impressionData.country = MaxSdk.GetSdkConfiguration()?.CountryCode ?? "Unknown";
            //impressionData.network_name = adInfo.NetworkName;
            //impressionData.ad_unit_id = adInfo.AdUnitIdentifier;
            //impressionData.format = adInfo.AdFormat;
            //impressionData.placement = adInfo.Placement;
            //impressionData.network_placement = adInfo.NetworkPlacement;
            //impressionData.creative_id = adInfo.CreativeIdentifier;
            //impressionData.revenue_precision = adInfo.RevenuePrecision;

            string json = JsonUtility.ToJson(impressionData);
            instance.AppLovinImpressionFromJSON(json);
        }

        /// <summary>
        /// 添加接口
        /// </summary>
        private void AddEvent()
        {
            FacadeTenjinExtend.SetEvent += SetEvent;
            FacadeTenjinExtend.SetEvent2 += SetEvent2;
            FacadeTenjinExtend.AppLovinImpressionFromJSON += AppLovinImpressionFromJSON;
        }

        /// <summary>
        /// 移除接口
        /// </summary>
        private void RemoveEvent()
        {
            FacadeTenjinExtend.SetEvent -= SetEvent;
            FacadeTenjinExtend.SetEvent2 -= SetEvent2;
            FacadeTenjinExtend.AppLovinImpressionFromJSON += AppLovinImpressionFromJSON;
        }
        protected override void OnDispose()
        {
            RemoveEvent();
        }
    }
}

