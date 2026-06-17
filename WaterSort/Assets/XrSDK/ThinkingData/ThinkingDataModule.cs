using System;
using System.Collections.Generic;
using ThinkingData.Analytics;
using UnityEngine;
using XrCode;

namespace XrSDK
{
    public class ThinkingDataModule : BaseModule
    {
        private bool enableLog;
        private TDNetworkType networkType;
        private string appID;
        private string serverURL;
        private TDMode mode;
        private TDTimeZone timeZone;
        private TDAutoTrackEventType autoTrackType;
        private bool autoRL;

        public ThinkingDataModule(ThinkingDataData data)
        {
            enableLog = data.EnableLog;
            networkType = data.NetworkType;
            appID = data.APPID;
            serverURL = data.SERVERURL;
            mode = data.MODE;
            timeZone = data.TimeZone;
            autoTrackType = data.AutoTrackType;
            autoRL = data.AutoRL;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            AddFacade();

            TDConfig config = new TDConfig(appID, serverURL);
            config.mode = mode;
            config.timeZone = timeZone;
            TDAnalytics.EnableLog(enableLog);
            TDAnalytics.SetNetworkType(networkType);
            TDAnalytics.Init(config);

            AutoTrack(autoTrackType);

            if(autoRL)
            {
                SetFirstRegisterTime();
                RegisterFinish(SystemInfo.deviceUniqueIdentifier);
                SetFirstLoginTime();
                SetLastLoginTime();
                LoginSuccess();
            }
        }

        #region 接口

        private void AddFacade()
        {
            ThinkingDataDefines.AdStart += AdStart;
            ThinkingDataDefines.AdFail += AdFail;
            ThinkingDataDefines.AdComplete += AdComplete;
            ThinkingDataDefines.AdRevenuePaid += AdRevenuePaid;
            ThinkingDataDefines.Times_5_Ad += Times_5_Ad;
            ThinkingDataDefines.Times_10_Ad += Times_10_Ad;
            ThinkingDataDefines.Times_15_Ad += Times_15_Ad;
            ThinkingDataDefines.Times_20_Ad += Times_20_Ad;
            ThinkingDataDefines.RegisterFinish += RegisterFinish;
            ThinkingDataDefines.LoginSuccess += LoginSuccess;
            ThinkingDataDefines.LoadingStart += LoadingStart;
            ThinkingDataDefines.EnterMainUI += EnterMainUI;
            ThinkingDataDefines.ButtonClick += ButtonClick;
            ThinkingDataDefines.GuideStep += GuideStep;
            ThinkingDataDefines.SetFirstRegisterTime += SetFirstRegisterTime;
            ThinkingDataDefines.SetFirstLoginTime += SetFirstLoginTime;
            ThinkingDataDefines.SetLastRegisterTime += SetLastLoginTime;
            ThinkingDataDefines.SetUserMoney += SetUserMoney;
            ThinkingDataDefines.SetLevel += SetLevel;
            ThinkingDataDefines.SetAttributionData += SetAttributionData;
            ThinkingDataDefines.SetAdInfo += SetAdInfo;
        }

        private void RemoveFacade()
        {
            ThinkingDataDefines.AdStart -= AdStart;
            ThinkingDataDefines.AdFail -= AdFail;
            ThinkingDataDefines.AdComplete -= AdComplete;
            ThinkingDataDefines.AdRevenuePaid -= AdRevenuePaid;
            ThinkingDataDefines.Times_5_Ad -= Times_5_Ad;
            ThinkingDataDefines.Times_10_Ad -= Times_10_Ad;
            ThinkingDataDefines.Times_15_Ad -= Times_15_Ad;
            ThinkingDataDefines.Times_20_Ad -= Times_20_Ad;
            ThinkingDataDefines.RegisterFinish -= RegisterFinish;
            ThinkingDataDefines.LoginSuccess -= LoginSuccess;
            ThinkingDataDefines.LoadingStart -= LoadingStart;
            ThinkingDataDefines.EnterMainUI -= EnterMainUI;
            ThinkingDataDefines.ButtonClick -= ButtonClick;
            ThinkingDataDefines.GuideStep -= GuideStep;
            ThinkingDataDefines.SetFirstRegisterTime -= SetFirstRegisterTime;
            ThinkingDataDefines.SetFirstLoginTime -= SetFirstLoginTime;
            ThinkingDataDefines.SetLastRegisterTime -= SetLastLoginTime;
            ThinkingDataDefines.SetUserMoney -= SetUserMoney;
            ThinkingDataDefines.SetLevel -= SetLevel;
            ThinkingDataDefines.SetAttributionData -= SetAttributionData;
            ThinkingDataDefines.SetAdInfo -= SetAdInfo;
        }

        #endregion

        /// <summary>
        /// 自动采集
        /// </summary>
        /// <param name="type">自动采集类型</param>
        private void AutoTrack(TDAutoTrackEventType type)
        {
            TDAnalytics.EnableAutoTrack(type);
        }

        #region 埋点

        #region 广告

        /// <summary>
        /// 广告开始播放
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        private void AdStart(EAdType adtype, EAdSource diasource, double ecpm, string platform, string precision)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            TDAnalytics.Track("AdStart", properties);
        }

        /// <summary>
        /// 广告观看成功
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        private void AdComplete(EAdType adtype, EAdSource diasource, double ecpm, string platform, string precision)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            TDAnalytics.Track("AdComplete", properties);
        }

        /// <summary>
        /// 广告观看失败
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="errmsg">错误信息</param>
        /// <param name="platform">广告平台</param>
        private void AdFail(EAdType adtype, EAdSource diasource, string errmsg)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("errmsg", errmsg);

            TDAnalytics.Track("AdFail", properties);
        }

        /// <summary>
        /// 广告收入发放成功
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="revenue">广告收入</param>
        /// <param name="precision">广告精度</param>
        /// <param name="diasource">广告位</param>
        /// <param name="platform">广告平台</param>
        private void AdRevenuePaid(EAdType adtype, double revenue, string precision, EAdSource diasource, string platform)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("revenue", revenue);
            properties.Add("precision", precision);
            properties.Add("diasource", diasource);
            properties.Add("platform", platform);

            TDAnalytics.Track("AdRevenuePaid", properties);
        }

        /// <summary>
        /// 5次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_5_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("Times_5_Ad", properties);
        }

        /// <summary>
        /// 10次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_10_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("Times_10_Ad", properties);
        }

        /// <summary>
        /// 15次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_15_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("Times_15_Ad", properties);
        }

        /// <summary>
        /// 20次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_20_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            TDAnalytics.Track("Times_20_Ad", properties);
        }

        #endregion

        #region 账号

        /// <summary>
        /// 玩家注册成功
        /// </summary>
        /// <param name="userId">玩家Id</param>
        private void RegisterFinish(string userId)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("#first_check_id", userId);
            properties.Add("userId", userId);
            properties.Add("regtime", DateTime.Now);

            TDAnalytics.Track("Register_Finish", properties);
        }

        /// <summary>
        /// 玩家登录成功
        /// </summary>
        private void LoginSuccess()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("Login_Success", properties);
        }

        /// <summary>
        /// 游戏加载成功
        /// </summary>
        private void LoadingStart()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("Loading_Start", properties);
        }

        /// <summary>
        /// 游戏进入了主界面
        /// </summary>
        private void EnterMainUI()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            TDAnalytics.Track("Enter_MainUI", properties);
        }

        #endregion

        #region 游戏

        /// <summary>
        /// 按钮点击
        /// </summary>
        /// <param name="btnPath">按钮路径</param>
        private void ButtonClick(string btnPath)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("buttonpath", btnPath);

            TDAnalytics.Track("ButtonClick", properties);
        }
        
        /// <summary>
        /// 引导步骤
        /// </summary>
        /// <param name="curStep">当前引导步骤</param>
        private void GuideStep(int curStep)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("step", curStep);

            TDAnalytics.Track("GuideStep", properties);
        }

        #endregion

        #endregion

        #region 用户属性

        #region 注册/登录

        /// <summary>
        /// 设置第一次注册时间
        /// </summary>
        private void SetFirstRegisterTime()
        {
            TDAnalytics.UserSetOnce(new Dictionary<string, object>()
            {
                {"register_time", DateTime.Now}
            });
        }

        /// <summary>
        /// 设置第一次登录时间
        /// </summary>
        private void SetFirstLoginTime()
        {
            TDAnalytics.UserSetOnce(new Dictionary<string, object>()
            {
                {"first_login_time", DateTime.Now}
            });
        }

        /// <summary>
        /// 设置最后一次登录时间
        /// </summary>
        private void SetLastLoginTime()
        {
            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"last_login_time", DateTime.Now}
            });
        }

        #endregion

        #region 游戏

        /// <summary>
        /// 设置玩家当前金额
        /// </summary>
        /// <param name="money">当前金额</param>
        private void SetUserMoney(float money)
        {
            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"current_money", money}
            });
        }

        /// <summary>
        /// 设置玩家当前关卡
        /// </summary>
        /// <param name="level">当前关卡</param>
        private void SetLevel(int level)
        {
            TDAnalytics.UserSet(new Dictionary<string, object>()
            {
                {"current_level", level}
            });
        }

        #endregion

        #region 第三方

        /// <summary>
        /// 获取设置归因信息
        /// </summary>
        /// <param name="data">归因信息</param>
        private void SetAttributionData(Dictionary<string, object> data)
        {
            TDAnalytics.UserSetOnce(data);
        }

        /// <summary>
        /// 设置玩家广告相关数据
        /// </summary>
        /// <param name="adCount">新增广告次数</param>
        /// <param name="adTime">新增广告时间</param>
        /// <param name="adRevenue">新增广告收入</param>
        private void SetAdInfo(int adCount, float adTime, float adRevenue)
        {
            TDAnalytics.UserAdd(new Dictionary<string, object>()
            {
                {"total_ad_num", adCount},
                {"total_ad_time", adTime},
                {"total_ad_revenue", adRevenue},
            });
        }

        #endregion

        #endregion

        #region 额外扩充



        #endregion

        protected override void OnDispose()
        {
            base.OnDispose();

            RemoveFacade();
        }
    }
}