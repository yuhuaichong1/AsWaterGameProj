using AppsFlyerSDK;
using System;
using System.Collections.Generic;
using UnityEngine;
using XrCode;

public class AppsFlyerMono : MonoBehaviour, IAppsFlyerConversionData
{
    private Action<Dictionary<string, object>> OCDS;
    private Action<string> OCDF;
    private Action<Dictionary<string, object>> OAOA;
    private Action<string> OAOAF;

    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void onConversionDataSuccess(string conversionData)
    {
        AppsFlyer.AFLog("didReceiveConversionData", conversionData);
        D.Error($"AppsFlyer归因数据获取成功");
        Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
        OCDS?.Invoke(conversionDataDictionary);
    }

    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
        D.Error($"AppsFlyer归因数据获取失败: {error}");
        OCDF?.Invoke(error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        OAOA?.Invoke(attributionDataDictionary);
    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
        OAOAF?.Invoke(error);
    }

    internal void AddCallback(Action<Dictionary<string, object>> ocds, Action<string> ocdf, Action<Dictionary<string, object>> oaoa, Action<string> oaoaf)
    {
        OCDS = ocds;
        OCDF = ocdf;
        OAOA = oaoa;
        OAOAF = oaoaf;
    }
}
