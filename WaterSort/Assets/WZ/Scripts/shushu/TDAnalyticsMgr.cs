using System;
using System.Collections.Generic;
using ThinkingData.Analytics;
using UnityEngine;

namespace WZSDK
{
    public class TDAnalyticsMgr : BaseModFrame
    {

        private static TDAnalyticsMgr _instance;
        public static TDAnalyticsMgr Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TDAnalyticsMgr();
                    _instance.Load();
                }
                return _instance;
            }
        }
        //开关
#if UNITY_EDITOR
        bool isOpenTD = true;
#else
    bool isOpenTD = true;
#endif

        //玩家id
        private string accoundId = "";
        //游戏版本
        private string GameVersion = "1.0.0.0";
        //游戏编号，区分上线平台
        private string GameAppId = "605001";


        private DateTime AdTime;//广告时长统计
        private DateTime loginTime;//登录时的时间

        private const string ATTRIBUTION_REPORTED_KEY = "attribution_reported";
        private bool hasReportedAttribution = false;

        protected override void OnLoad()
        {
            GameLoad(PlayerPrefs.GetString("accoundId"));
        }

        //游戏加载
        public async void GameLoad(string accoundId)
        {
            if (!isOpenTD) return;
            //设置公共事件属性以后，每个事件都会带有公共事件属性
            Dictionary<string, object> superProperties = new Dictionary<string, object>();
            superProperties["GameVersion"] = GameVersion;
            superProperties["GameAppId"] = GameAppId;

            this.accoundId = GetRandomID();//FacadeUserExtend.GetUserIDHandle();

            string register = PlayerPrefs.GetString("is_register");
            if (string.IsNullOrEmpty(register))
            {
                RegisterFinish();
                PlayerPrefs.SetString("is_register", "register");
                PlayerPrefs.Save();
            }

            TDAnalytics.SetSuperProperties(superProperties);//设置公共事件属性

            TDAnalytics.UserSet(new Dictionary<string, object>() { { "accountId", this.accoundId }, { "GameVersion", GameVersion }, { "GameAppId", GameAppId } }); //设置用户属性

            if (isOpenTD)
            {
                TDAnalytics.EnableAutoTrack(TDAutoTrackEventType.All);
                //TDAnalytics.EnableAutoTrack(TDAutoTrackEventType.AppEnd, new LoginOut2());
            }

            loginTime = DateTime.Now;
            Debug.LogError("自动采集开始");
        }
        private string GetRandomID()
        {
            if (SPlayerPrefs.HasKey(FacadePlayerPrefExtend.SuserID))
            {
                return SPlayerPrefs.GetString(FacadePlayerPrefExtend.SuserID);
            }
            else
            {
                Guid GID = Guid.NewGuid();
                string target = GID.ToString();
                SPlayerPrefs.SetString(FacadePlayerPrefExtend.SuserID, target);
                SPlayerPrefs.Save();
                return target;
            }
        }

        //关卡完成
        public void LevelComplete(int levelId)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("levelId", levelId);
            TDAnalytics.Track("YM_LevelComplete", properties);
        }

        //关卡失败
        public void LevelFail(int levelId)
        {
            if (!isOpenTD) return;
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("levelId", levelId);
            TDAnalytics.Track("YM_LevelFail", properties);
        }


        /// <summary>
        /// 广告开始播放
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        public void AdStart(EAdtype adtype, EAdSource diasource, double ecpm, string platform, string precision,int currentLevel)
        {
            if (!isOpenTD) return;
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);
            properties.Add("currentLevel", currentLevel);
            //SetUserMD();
            TDAnalytics.Track("YM_AdStart", properties);

            AdTime = DateTime.Now;
        }

        /// <summary>
        /// 广告观看失败
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        public void AdFail(EAdtype adtype, EAdSource diasource, string errmsg, string platform, string precision)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("errmsg", errmsg);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            //SetUserMD();
            TDAnalytics.Track("YM_AdFail", properties);
        }

        /// <summary>
        /// 未进广告观看失败
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        public void AdLoadFail(EAdtype adtype, EAdSource diasource, int levelId, AdDisplayFailed adDisplayFailed,int currentLevel)
        {
            if (!isOpenTD) return;
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("levelId", levelId);
            properties.Add("adDisplayFailed", adDisplayFailed.ToString());
            properties.Add("currentLevel", currentLevel);
            TDAnalytics.Track("YM_AdLoadFail", properties);
        }

        /// <summary>
        /// 广告观看成功
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        public void AdComplete(EAdtype adtype, EAdSource diasource, double ecpm, string platform, string precision,int currentLevel)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);
            properties.Add("currentLevel", currentLevel);

            //SetUserMD();
            TDAnalytics.Track("YM_AdComplete", properties);

            TotalAdData((int)Math.Round((DateTime.Now - AdTime).TotalSeconds), ecpm / 1000);
            Debug.Log("广告观看成功");
        }

        /// <summary>
        /// 广告收入发放成功
        /// </summary>
        /// <param name="revenue">广告收入</param>
        /// <param name="precision">广告精度</param>
        /// <param name="diasource">广告位</param>
        /// <param name="platform">广告平台</param>
        public void AdRevenuePaid(EAdtype adtype, double revenue, string precision, EAdSource diasource, string platform, string networkName)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("revenue", revenue);
            properties.Add("precision", precision);
            properties.Add("diasource", diasource);
            properties.Add("platform", platform);
            properties.Add("networkNmae", networkName);
            properties.Add("currentLevel", GameManagerWZ.instance.currentLv);

            //SetUserMD();
            TDAnalytics.Track("YM_AdRevenuePaid", properties);
            Debug.Log("广告收入发放成功");
        }

        /// <summary>
        /// 5次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_5_Ad(double accu_revenue)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("YM_Times_5_Ad", properties);
        }

        /// <summary>
        /// 10次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_10_Ad(double accu_revenue)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("YM_Times_10_Ad", properties);
        }

        /// <summary>
        /// 15次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_15_Ad(double accu_revenue)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("YM_Times_15_Ad", properties);
        }

        /// <summary>
        /// 20次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_20_Ad(double accu_revenue)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("YM_Times_20_Ad", properties);
        }

        /// <summary>
        /// 注册成功，首次登录
        /// </summary>
        /// <param name="firstCheckId">首次事件校验</param>
        /// <param name="regtime">注册时间</param>
        public void RegisterFinish()
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            //properties.Add("#first_check_id", firstCheckId);
            properties.Add("first_check_id", SystemInfo.deviceUniqueIdentifier);
            properties.Add("regtime", DateTime.Now);

            TDAnalytics.Track("YM_RegisterFinish", properties);
        }

        /// <summary>
        /// 加载开始
        /// </summary>
        public void LoadingStart()
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("YM_LoadingStart", properties);
        }

        /// <summary>
        /// 加载完成
        /// </summary>
        public void LoadFinish()
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("YM_LoadFinish", properties);
        }

        /// <summary>
        /// 进入主界面
        /// </summary>
        public void EnterMainUI()
        {
            if (!isOpenTD) return;


            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("YM_EnterMainUI", properties);
        }

        /// <summary>
        /// 登录成功
        /// </summary>
        public void LoginSuccess()
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("data", GameDefines.ifIAA);

            TDAnalytics.Track("YM_LoginSuccess", properties);

            First_Register_And_Login();
            Last_Login();

        }

        /// <summary>
        /// 当前引导进度
        /// </summary>
        /// <param name="step">当前引导步骤</param>
        public void GuideStep(int step)
        {
            if (!isOpenTD) return;

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("step", step * 100);

            TDAnalytics.Track("YM_GuideStep", properties);
        }

        /// <summary>
        /// 点击btn
        /// </summary>
        /// <param name="buttonpath">btn对应路径</param>
        public void ButtonClick(GameObject buttonObj)
        {
            if (!isOpenTD) return;
            if (buttonObj == null) return;
            List<string> pathList = new List<string>();
            pathList.Add(buttonObj.name);
            Transform trans = buttonObj.transform.parent;
            while (trans.parent != null)
            {
                pathList.Add(trans.gameObject.name);
                trans = trans.parent;
            }
            pathList.Reverse();
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("buttonPath", string.Join("/", pathList));

            TDAnalytics.Track("YM_ButtonClick", properties);
        }

        /// <summary>
        /// 第一次注册和登录（第一次进入游戏）
        /// </summary>
        public void First_Register_And_Login()
        {
            TDAnalytics.UserSetOnce(new Dictionary<string, object>()
            {
                {"register_time", DateTime.Now},
                {"first_login_time", DateTime.Now}
            });
        }

        /// <summary>
        /// 每次登录时间
        /// </summary>
        public void Last_Login()
        {
            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"last_login_time", DateTime.Now}
            });
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void LoginOut()
        {
            TDAnalytics.UserAdd(new Dictionary<string, object>()
            {
                //{ "total_taptime", 1},
                { "total_runtime", (int)Math.Round((DateTime.Now - loginTime).TotalSeconds)},
            });

            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"current_money", FacadeUserExtend.GetUserCoinHandle()},
                {"current_runtime", (int)Math.Round((DateTime.Now - loginTime).TotalSeconds)},
            });
        }

        /// <summary>
        /// 累计广告数据
        /// </summary>
        public void TotalAdData(float time, double revenue)
        {
            TDAnalytics.UserAdd(new Dictionary<string, object>()
            {
                {"total_ad_num", 1},
                {"total_ad_time", time},
                {"total_ad_revenue", revenue},
            });
        }

        /// <summary>
        /// AppsFlyer归因信息
        /// </summary>
        /// <param name="attribution">AppsFlyer归因信息</param>
        public void AppsFlyerMsg(Dictionary<string, object> msg)
        {
            TDAnalytics.UserSetOnce(msg);
        }

        public string GetAccoundId()
        {
            return accoundId;
        }

        /// <summary>
        /// 当前玩家等级（关卡）
        /// </summary>
        /// <param name="level">关卡</param>
        public void CurLevel(int level)
        {
            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"user_level", level},
            });
        }


        /// <summary>
        /// 设置用户归因属性
        /// </summary>
        /// <param name="properties">归因属性</param>
        public void SetUserAttributionProperties(Dictionary<string, object> properties)
        {
            if (!isOpenTD) return;
            TDAnalytics.UserSet(properties);
            Debug.Log($"[TDAnalytics] 设置用户归因属性: {string.Join(", ", properties.Keys)}");
        }

        /// <summary>
        /// 上报归因事件
        /// </summary>
        /// <param name="parameters">归因事件参数</param>
        public void TrackAttributionEvent(Dictionary<string, object> parameters)
        {
            if (!isOpenTD) return;
            var eventParams = new Dictionary<string, object>(parameters);
            eventParams["attribution_report_time"] = DateTime.Now;
            TDAnalytics.Track("YM_Attribution", eventParams);
            Debug.Log($"[TDAnalytics] 上报归因事件: YM_Attribution");
        }

        /// <summary>
        /// 标记归因事件已上报
        /// </summary>
        public void MarkAttributionEventReported()
        {
            hasReportedAttribution = true;
            PlayerPrefs.SetInt(ATTRIBUTION_REPORTED_KEY, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        ///处理归因数据并上报给数数
        /// </summary>
        public void ProcessAndReportAttribution(Dictionary<string, object> attributionData)
        {
            try
            {

                hasReportedAttribution = PlayerPrefs.GetInt(ATTRIBUTION_REPORTED_KEY, 0) == 1;
                Dictionary<string, object> eventParameters = BuildAttributionParameters(attributionData);
                SetUserProperties(eventParameters);

                if (!hasReportedAttribution)
                {
                    TrackAttributionEvent(eventParameters);
                    MarkAttributionEventReported();
                    Debug.Log($"AppsFlyer: 归因事件上报成功 - {JsonUtility.ToJson(eventParameters)}");
                }
                else
                {
                    Debug.Log("AppsFlyer: 归因事件已上报过，仅更新用户属性");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"处理归因数据时出错: {e.Message}");
            }
        }


        /// <summary>
        ///构建归因参数
        /// </summary>
        /// <summary>
        /// 构建归因参数
        /// </summary>
        private Dictionary<string, object> BuildAttributionParameters(Dictionary<string, object> data)
        {
            var parameters = new Dictionary<string, object>();
            string afStatus = data.ContainsKey("af_status") ? data["af_status"].ToString() : "Unknown";
            string mediaSource = data.ContainsKey("media_source") ? data["media_source"].ToString() : "";
            string campaign = data.ContainsKey("campaign") ? data["campaign"].ToString() : "";

            if (afStatus == "Organic")
            {
                // 自然用户
                parameters["ad_network"] = "Organic";
                parameters["af_status"] = "Organic";
                parameters["af_media_source"] = "";
                parameters["af_campaign"] = "";
                parameters["user_type"] = "organic";
                parameters["is_organic_user"] = true;
                Debug.Log($" 判断为: 自然用户");
            }
            else if (afStatus == "Non-organic")
            {
                // 导量用户（买量用户）
                parameters["af_status"] = $"{"Non - organic"}_{mediaSource}";
                parameters["af_media_source"] = mediaSource;
                parameters["ad_network"] = mediaSource;
                parameters["af_campaign"] = campaign;
                parameters["user_type"] = "paid";
                parameters["is_organic_user"] = false;
                Debug.Log($" 判断为: 导量用户 - 渠道: {mediaSource}, 活动: {campaign}");
            }
            else
            {
                // 未知状态，按自然流量处理
                parameters["ad_network"] = "unknown";
                parameters["af_status"] = afStatus;
                parameters["af_media_source"] = "";
                parameters["af_campaign"] = campaign;
                parameters["user_type"] = "unknown";
                parameters["is_organic_user"] = true;
                Debug.Log($"判断为: 未知类型({afStatus})，按自然用户处理");
            }

            if (data.ContainsKey("af_siteid"))
            {
                parameters["af_siteid"] = data["af_siteid"];
                Debug.Log($" af_siteid: {data["af_siteid"]}");
            }

            if (data.ContainsKey("adgroup_id"))
            {
                parameters["adgroup_id"] = data["adgroup_id"];
                Debug.Log($" adgroup_id: {data["adgroup_id"]}");
            }

            if (data.ContainsKey("adset"))
            {
                parameters["adset"] = data["adset"];
                Debug.Log($" adset: {data["adset"]}");
            }

            Debug.Log($" 最终用户类型: {parameters["user_type"]}, is_organic: {parameters["is_organic_user"]}");
            return parameters;
        }

        /// <summary>
        /// 设置用户属性到数数
        /// </summary>
        public void SetUserProperties(Dictionary<string, object> properties)
        {
            try
            {
                if (!isOpenTD) return;

                TDAnalytics.UserSet(properties);
                Debug.Log($"[TDAnalytics] 设置用户属性: {string.Join(", ", properties.Keys)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"设置用户属性失败: {e.Message}");
            }
        }


    }


    public class LoginOut2 : TDAutoTrackEventHandler
    {
        public Dictionary<string, object> GetAutoTrackEventProperties(int type, Dictionary<string, object> properties)
        {
            //YarnCoveMgr.Instance.TDAnalyticsManager.LoginOut();

            return new Dictionary<string, object>()
            {
                {"AutoTrackEventProperty", DateTime.Today}
            };
        }
    }
}