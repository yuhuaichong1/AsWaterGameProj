using UnityEngine;
using YRTT;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;
using UnityEngine.UI;
using UnityEngine.Scripting;
using Unity.VisualScripting;

public class Demo : MonoBehaviour, IYRTTChannelChangedListener
{
    private List<YRTTTaskInfo> taskInfos = new List<YRTTTaskInfo>();
    // Start is called before the first frame update
    public Button btnOpenOffer;
    public InputField inputFieldToponTestDebugKey;
    public Text sdkInitText;
    void Start()
    {

        YRTTSDK.Instance.Init((success, info) =>
        {
            btnOpenOffer.gameObject.SetActive(false);
            string initResult = "SDK"+(success ? "初始化成功" : "初始化失败");
            ShowLogAndToast(initResult);
            sdkInitText.text = initResult;
            
            ShowLogAndToast(YRTTSDK.Instance.IsReward() ? "激励用户" : "非激励用户");
            ShowLogAndToast($"版本号：{YRTTSDK.Instance.GetVersionCode()}版本名称：{YRTTSDK.Instance.GetVersionName()}");
            ShowLogAndToast($"是否代理网络：{YRTTSDK.Instance.IsProxy()}");
            ShowLogAndToast($"是否Root：{YRTTSDK.Instance.IsRoot()}");
            ShowLogAndToast($"是否VPN：{YRTTSDK.Instance.IsVPN()}");
            ShowLogAndToast($"AF邀请链接：{YRTTSDK.Instance.GetAFInviteLink()}");
            ShowLogAndToast($"用户被邀请码：{info.FromInviteCode}");
            ShowLogAndToast($"用户信息：{info.ToJson()}");
            Events.onAdShowEvent += (bool show) =>
            {
                ShowLogAndToast(show ? "广告展示成功" : "广告展示关闭/失败");
            };
            YRTTTaskSDK.Instance.AddTaskListener((bool taskSuccess, YRTTTaskInfo taskInfo) =>
            {
                ShowLogAndToast(taskSuccess ? $"任务接收成功，任务ID：{taskInfo?.TaskId}任务详情：{taskInfo.ToJson()}" : "任务接收失败");
                if (taskSuccess && taskInfo != null)
                {
                    taskInfos.Add(taskInfo);
                }
               

            });
            Events.onAdRevenue += (adInfo) =>
            {
                ShowLogAndToast($"广告变现回调:{adInfo.ToJson()}");
            };
            Events.AdInfo info1111 = new Events.AdInfo();
            info1111.adSource = "TestSource";
            info1111.adUnitID = "TestUnitID";
            info1111.adType = "rewarded";
            info1111.platform = "unity";
            info1111.price = "0.05";
            Events.OnAdRevenue(info1111);
            //YRTTSDK.Instance.PreLoadInterstitial();
            //YRTTSDK.Instance.PreLoadRewardVideo();
            string v = YRTTSDK.Instance.GetExtraJson("LocalValueConfig");
            ShowLogAndToast("请求到的配置LocalValueConfig：" + v);
        });

        YRTTSDK.Instance.SetChannelChangedListener(this);
    }

    public void OnChannelChanged(string newChannel)
    {
        Debug.Log("Channel 发生变化，新值为: " + newChannel);
    }

    public void GetStrategy()
    {
        YRTTSDK.Instance.FetchArkConfig("LocalValueConfig", ((success, configs) =>
        {
            ShowLogAndToast("GetStrategy:" + success);
            if (success && configs != null)
            {
                foreach (var abData in configs)
                {
                    // abData.vid 当前配置的唯一标识
                    foreach (var kv in abData.fields)
                    {
                        if (kv.fkey.Equals("LocalValueConfig"))
                        {
                            //   kv.fvalue  是配置信息

                        }
                    }
                }
            }
        }));
        //YRTTSDK.Instance.GetExrtaJson();
        //YRTTSDK.Instance.GetExtraConfig
    }
    public void GetGame()
    {
        //YRTTSDK.Instance.FetchGameConfig("LocalValueConfig", ((success, configs) =>
        //{
        //    ShowLogAndToast($"LocalValueConfig:{success}{configs?.Count}");
        //    ShowLogAndToastWarning(success);
        //    ShowLogAndToastWarning(configs);
        //}));
        string v = YRTTSDK.Instance.GetExtraJson("LocalValueConfig");
        ShowLogAndToast("请求到的配置LocalValueConfig：" + v);
    }

    public void Load2ShowRewardVideo()
    {
        /**** 加载并展示激励视频广告：如果当前没有加载成功的激励视频广告，则会进入5s的加载期，
         * 加载期内加载激励视频广告成功后会直接展示广告，5s内没有加载到广告则会回调didFail；
         * 如果已经有缓存的激励视频广告，则会直接展示广告。
         */
        YRTTSDK.Instance.ShowRewardVideo("TEST", () =>
        {
            //广告激励回调（关闭时回调）
            ShowLogAndToast("Load2ShowView" + "didHide");
        },
        () =>
        {
            //广告展示失败回调  
            ShowLogAndToast("Load2ShowView" + "didFail");
        });
    }

    public void ShowIns()
    {
        /** * 展示插屏广告，该方法需要在PreLoadInter预加载插屏广告后调用。
         * 注意：如果没有PreLoadInter预加载插屏广告，
         * 该方法会直接回调didFail展示失败
         */
        YRTTSDK.Instance.ShowInterstitial("TEST_INS", () =>
        {
            //广告关闭回调
            ShowLogAndToast("ShowInterstitial" + "didHide");

        },
        () =>
        {
            //广告展示失败回调
            ShowLogAndToast("ShowInterstitial" + "didFail");
        });
    }

    public void ShowOpen()
    {
        /** * 展示开屏广告，该方法需要在PreLoadOpen预加载开屏广告后调用。
         * 注意：如果没有PreLoadOpen预加载开屏广告，
         * 该方法会直接回调didFail展示失败
         */
        YRTTSDK.Instance.ShowOpenAd("TEST_OPEN", () =>
        {
            //广告关闭回调
            ShowLogAndToast("ShowOpenAd" + "didHide");
        },
        () =>
        {
            //广告展示失败回调
            ShowLogAndToast("ShowOpenAd" + "didFail");
        });
    }
    private void ShowLogAndToast(string msg)
    {
        Debug.Log("SDK Unity Demo: 打印的信息：" + msg);

    }
    public void CheckVideo()
    {
        /** * 检查激励视频广告是否可用,
         *  需要在PreLoadReward预加载激励视频广告后调用。
         *  或者是在调用Load2ShowRewardVideo方法加载并展示广告后调用。
         *  否则没有加载的动作，直接返回false。
         */
        bool result = YRTTSDK.Instance.IsRewardVideoReady();
        ShowLogAndToast("SDK Unity Demo: 激励可用?（CheckVideo）：" + result);
    }
  

    public void CheckH5()
    {
        bool result = YRTTSDK.Instance.IsH5Ready("lobby_h5");
        ShowLogAndToast("SDK Unity Demo: H5可用?（CheckVideo）：" + result);
    }
    public void CheckIns()
    {
        /** * 检查插屏广告是否可用,
         *  需要在PreLoadInter预加载插屏广告后调用。
         *  或者是在调用Load2ShowInterstitial方法加载并展示广告后调用。
         *  否则没有加载的动作，直接返回false。
         */
        bool result = YRTTSDK.Instance.IsInterstitialReady();
        ShowLogAndToast("SDK Unity Demo: 插屏可用?（CheckIns）：" + result);
    }

    public void CheckOpen()
    {
        /** * 检查开屏广告是否可用,
         *  需要在PreLoadOpen预加载开屏广告后调用。
         *  或者是在调用Load2ShowOpenAd方法加载并展示广告后调用。
         *  否则没有加载的动作，直接返回false。
         */
        bool result = YRTTSDK.Instance.IsOpenAdReady();
        ShowLogAndToast("SDK Unity Demo: 开屏可用?（CheckOpen）：" + result);
    }
    public void EnterHome() {
        YRTTSDK.Instance.EnterHomePage();
    }
    public void EnterAdPage()
    {
        YRTTSDK.Instance.EnterAdScene("test_enterAdPage");
    }
    public void ClickAdButton()
    {
        YRTTSDK.Instance.ClickAdButton("test_clickAdButton");
    }
    public void showMaxDebugger()
    {
        ShowLogAndToast("SDK Unity Demo:showMaxDebugger:" + YRTTConfig.IsDebug);
        if (YRTTConfig.IsDebug)
        {

            // Show Mediation Debugger
            YRTTSDK.Instance.ShowMaxTestUI();

        }
    }

    public void ShowToponDebugger()
    {
        //ShowLogAndToast("SDK Unity Demo:showToponDebugger:" + YRTTConfig.IsDebug);
        //if (YRTTConfig.IsDebug)
        //{
        //    string debugKey = inputFieldToponTestDebugKey.text;
        //    ShowLogAndToast("SetToponDebugKey success, debugKey: " + debugKey);
        //    if (!string.IsNullOrEmpty(debugKey))
        //    {
        //        YRTTSDK.Instance.ShowToponTestUI(debugKey);
                
        //    }
        //    else {
        //        YRTTSDK.Instance.ShowToponTestUI("");
        //    }
        //}
    }

    public void StartAuth(){ 
        YRTTAuthSDK.Instance.StartAuth((result) =>
        {
            ShowLogAndToast("认证结果回调: " + result.ToString());
        });
    }

    public void TrackTaskRecevice (){
        foreach (var task in taskInfos)
        {
            ShowLogAndToast($"TrackTaskReceive:客户端接收任务成功===任务ID：{task.TaskId}");
            YRTTTaskSDK.Instance.TrackTaskReceive(task.TaskId);
        }
    }
    public void TrackTaskExpire() {
        foreach (var task in taskInfos)
        {
            ShowLogAndToast($"TrackTaskExpire:客户端通知任务过期===任务ID：{task.TaskId}");
            YRTTTaskSDK.Instance.TrackTaskExpire(task.TaskId);
        }
    }

    public void RequestMicroWW()
    {
        YRTTSDK.Instance.RequestMicroWW(8, "Paypal", "test@test.com", "test", 0.1, (success, data) => {
            //处理回调结果
            ShowLogAndToast("RequestMicroWW" + success + "，data:" + data?.data?.id);
        }, "", "");
    }

    public void GetWWRecord()
    {
        YRTTSDK.Instance.GetWWRecord((success, data) =>
        {
            ShowLogAndToast("GetWWRecord" + success + "，data: " + data?.list[0]?.id);
        });
    }

    public void ShowH5()
    {
        ShowLogAndToast("click ShowH5");
        YRTTSDK.Instance.ShowH5("lobby_h5", 100, () =>
        {
            ShowLogAndToast("ShowH5 fail ");

        }, (doubleValue) =>
        {
            ShowLogAndToast("ShowH5 reward:" + doubleValue);
        });
    }

    public void Track()
    {
        //Dictionary<string, string> dict = new Dictionary<string, string>();
        //dict.Add("key1", "value1");
        //dict.Add("key2", "value2");

        //YRTTSDK.Instance.Track("test_track", dict);
        //或
        YRTTSDK.Instance.Track("eventName", "key1", "value1", "key2", 2222);
    }
    public void TrackNow()
    {
        //Dictionary<string, string> dict = new Dictionary<string, string>();
        //dict.Add("key1", "value1");
        //dict.Add("key2", "value2");

        //YRTTSDK.Instance.TrackImmediately("test_TrackNow", dict);
        //或
        YRTTSDK.Instance.TrackImmediately("eventName", "key1", "value1", "key2", 2222);
    }

    public void TrackFirebase()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("key1", "value1");
        dict.Add("key2", "value2");

        YRTTSDK.Instance.TrackFireBase("test_TrackFirebase", dict);
    }

    public void TrackAppsFlyer()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("key1", "value1");
        dict.Add("key2", "value2");

        YRTTSDK.Instance.TrackAF("test_TrackAppsFlyer", dict);
    }
    public static T GetExtraConfig<T>(string moduleName)
    {
        return YRTTSDK.Instance.GetExtraConfig<T>(moduleName);
    }
    public static string GetExrtaJson(string moduleName)
    {
        return YRTTSDK.Instance.GetExtraJson(moduleName);
    }

    public void JumpAPPStore()
    {
        ShowLogAndToast($"JumpAPPStore:跳转应用商店===包名：com.android.vending，打开谷歌商店应用对应详情页面");
        YRTTTaskSDK.Instance.JumpToAppStore("com.android.vending");
    }

    public void JumpAPP()
    {
        bool installed = YRTTTaskSDK.Instance.IsAppInstalled("com.android.vending");
        ShowLogAndToast($"JumpAPP:检查应用是否安装===包名：com.android.vending，是否安装：{installed}");
        ShowLogAndToast($"JumpAPP:跳转应用===包名：com.android.vending，打开谷歌商店");
        YRTTTaskSDK.Instance.JumpToApp("com.android.vending");
    }
    public void SetTaskFinish()
    {
        if (taskInfos.Count > 0)
        {
            foreach (var task in taskInfos)
            {
                YRTTTaskSDK.Instance.SetTaskFinish(task.TaskId);
                ShowLogAndToast($"SetTaskFinish:完成任务===任务ID：{task.TaskId}");
            }
        }
        else
        {
            ShowLogAndToast($"SetTaskFinish:没有可完成的任务");
        }
    }
    public void IsExpired()
    {
        if (taskInfos.Count > 0)
        {
            foreach (var task in taskInfos)
            {
                bool expired = YRTTTaskSDK.Instance.IsExpired(task.TaskId);
                ShowLogAndToast($"IsExpired:检查任务是否过期===任务ID：{task.TaskId}，是否过期：{expired}");
            }
        }
        else
        {
            ShowLogAndToast($"IsExpired:没有可检查的任务");
        }
    }
    public void RequestTier()
    {
        YRTTSDK.Instance.RequestTierInfo((success, tier) =>
        {
            ShowLogAndToast("用户分层结果：RequestTierInfo: " + success + ", tier: " + tier.ToJson());
        });
    }

    public void InitOffer()
    {
        ShowLogAndToast("InitOffer ");
        YRTTSDK.Instance.InitOffer((s) =>
        {
            btnOpenOffer.gameObject.SetActive(true);
            ShowLogAndToast("InitOffer show: " + s);

        }, (s) =>
        {
            btnOpenOffer.gameObject.SetActive(false);
            ShowLogAndToast("InitOffer hide: " + s);
        }, (s) =>
        {
            ShowLogAndToast("InitOffer task: " + s);
        }, (s) =>
        {
            ShowLogAndToast("InitOffer award: " + s);
        });
    }

    public void OnShowPage()
    {
        ShowLogAndToast("OnShowPage ");
        YRTTSDK.Instance.OnShowPage();
    }
    
    
    public void ShowBanner()
    {
        ShowLogAndToast($"ShowBanner banner高度${YRTTSDK.Instance.GetBannerHeight()}");
        YRTTSDK.Instance.ShowBanner("Test-Banner-Scene", () =>
        {
            //广告关闭回调
            ShowLogAndToast("ShowBanner" + "didHide");
        },
        () =>
        {
            //广告展示失败回调
            ShowLogAndToast("ShowBanner" + "didFail");
        });
    }
    public void HideBanner()
    {
        ShowLogAndToast("HideBanner ");
        YRTTSDK.Instance.HideBanner("Test-Banner-Scene");
    }
    
    public void doPost()
    {
        ArkRequestBean a = new ArkRequestBean();
        a.bizCode = "test_bizCode";
        a.packageName = "com.test.demo";
        a.bsc = "Mediation";
        bool useGetWay = true;
        string url = useGetWay ? "/snt" : "/withdrawals/channel";
        YRTTSDK.Instance.HttpPost(a,url, useGetWay,(bool success, string errMsg, int code, string data) =>
        {
            ShowLogAndToast($"doPost result: success={success}, errMsg={errMsg}, code={code}, data={data}");
        });
        ShowLogAndToast("doPost ");
    }
    public void doGet()
    {
        ArkRequestBean a = new ArkRequestBean();
        a.bizCode = "test_bizCode";
        a.packageName = "com.test.demo";
        a.bsc = "Mediation";
        bool useGetWay = false;
        string url = useGetWay ? "/snt" : "/withdrawals/channel";
        YRTTSDK.Instance.HttpGet(a, url,useGetWay, (bool success, string errMsg, int code, string data) =>
        {
            ShowLogAndToast($"doGet result: success={success}, errMsg={errMsg}, code={code}, data={data}");
        });
        ShowLogAndToast("doGet ");
    }
    public void GoToNotifyPermissionSetting (){
        ShowLogAndToast("goToNotifyPermissionSetting,开始跳转通知栏权限页面 ");
        YRTTSDK.Instance.GoToNotifyPermissionSetting();
    }
    public void RequestNotifyPermission()
    {
        ShowLogAndToast("requestNotifyPermission,开始请求通知栏权限 ");
        YRTTSDK.Instance.RequestNotifyPermission();
    }
    public void HadNotifyPermission()
    {
        bool result = YRTTSDK.Instance.HadNotifyPermission();
        ShowLogAndToast("hadNotifyPermission,是否有通知栏权限: " + result);
    }

    public void FCMNotify()
    {
        YRTTSDK.Instance.AddNotification("testuniqueid", 10, "Test Title", "Test Content", "{}", (bool suc, BaseJsonResponseBean data) =>
        {
            ShowLogAndToast("FCMNotify:通知回调 success: " + suc + ", data: " + data.data);
        });
    }

    public void CancelNotify()
    {
        YRTTSDK.Instance.RemoveNotification("testuniqueid",(bool suc, BaseJsonResponseBean data) => {
            ShowLogAndToast("CancelNotify:取消通知回调 success: " + suc + ", data: " + data.data);
        });
    }

    public class ArkRequestBean 
{
    /// <summary>
    /// 请求的业务编码,非必须
    /// </summary>
    [JsonProperty(PropertyName = "CVPGBizGEWCCodesCRONG")]
    public string bizCode;
    [JsonProperty(PropertyName = "CRONGCVPGgooGEWCId")]
    public string goID;
    [JsonProperty(PropertyName = "CVPGAndrCRONGGEWCId")]
    public string anID;
    [JsonProperty(PropertyName = "CVPGCRONGoGEWCd")]
    public string o;
    [JsonProperty(PropertyName = "GEWCCVPGCxiiCRONG")]
    public string i;
    [JsonProperty(PropertyName = "idfa")]
    public string idfa;
    [JsonProperty(PropertyName = "idfv")]
    public string idfv;
    [JsonProperty(PropertyName = "bsc")]
    public string bsc;
        /// <summary>
        /// 应用包名
        /// </summary>
        [JsonProperty(PropertyName = "CVPGpackGEWCNameCRONG")]
    public string packageName;
    /// <summary>
    /// sdk版本号
    /// </summary>
    [JsonProperty(PropertyName = "CVPGSdkVerGEWCCodeCRONG")]
    public string sdkVersion;
    /// <summary>
    /// sdk版本名
    /// </summary>
    [JsonProperty(PropertyName = "CVPGGEWCProVerNameCRONG")]
    public string pdVersionName;
    /// <summary>
    /// 产品版本号
    /// </summary>
    [JsonProperty(PropertyName = "CVPGProGEWCVerCodeCRONG")]
    public string pdVersionCode;
    [JsonProperty(PropertyName = "CVPGCountryGEWCNameCRONG")]
    public string country;
    /// <summary>
    /// 品牌
    /// </summary>
    [JsonProperty(PropertyName = "CVPGBCRONGGEWCd")]
    public string pinpai;
    /// <summary>
    /// 型号
    /// </summary>
    [JsonProperty(PropertyName = "CVPGmlGEWCCRONG")]
    public string model;
    /// <summary>
    /// 语言
    /// </summary>
    [JsonProperty(PropertyName = "CVPGCRONGGEWClg")]
    public string yuyan;
    /// <summary>
    /// 玩家游戏信息
    /// </summary>
    [JsonProperty(PropertyName = "CVPGExtenCRONGGEWC")]
    public Dictionary<string, object> extra;

    /// <summary>
    /// 仅用于混淆，无实际意义
    /// </summary>
    private int tempInt;

    public int TempInt
    {
        [Preserve]
        get => tempInt;
        [Preserve]
        set => tempInt = value;
    }

    /// <summary>
    /// 仅用于混淆，无实际意义
    /// </summary>
    private string tempString;

    public string TempString { get => tempString; set => tempString = value; }

    /// <summary>
    /// 仅用于混淆，无实际意义
    /// </summary>
    public void TeempFuc()
    {

    }
}
}
