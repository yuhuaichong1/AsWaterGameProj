using System;
using System.Collections.Generic;
using UnityEngine;

namespace XrCode
{
    public class TDAnalyticsManager : Singleton<TDAnalyticsManager>, ILoad, IDispose
    {
        private DateTime lastCBTime;

        public void Load()
        {
            
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
        public void AdStart(EAdType adtype, EAdSource diasource, double ecpm, string platform, string precision)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            ThinkingDataDefines.Track("AdStart", properties);
        }

        /// <summary>
        /// 广告观看成功
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="ecpm">ecpm</param>
        /// <param name="platform">广告平台</param>
        /// <param name="precision">广告精度</param>
        public void AdComplete(EAdType adtype, EAdSource diasource, double ecpm, string platform, string precision)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            ThinkingDataDefines.Track("AdComplete", properties);
        }

        /// <summary>
        /// 广告观看失败
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="diasource">广告位</param>
        /// <param name="errmsg">错误信息</param>
        /// <param name="platform">广告平台</param>
        public void AdFail(EAdType adtype, EAdSource diasource, string errmsg)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("errmsg", errmsg);

            ThinkingDataDefines.Track("AdFail", properties);
        }

        /// <summary>
        /// 广告收入发放成功
        /// </summary>
        /// <param name="adtype">广告类型</param>
        /// <param name="revenue">广告收入</param>
        /// <param name="precision">广告精度</param>
        /// <param name="diasource">广告位</param>
        /// <param name="platform">广告平台</param>
        public void AdRevenuePaid(EAdType adtype, double revenue, string precision, EAdSource diasource, string platform)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("revenue", revenue);
            properties.Add("precision", precision);
            properties.Add("diasource", diasource);
            properties.Add("platform", platform);

            ThinkingDataDefines.Track("AdRevenuePaid", properties);
        }

        /// <summary>
        /// 5次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_5_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            ThinkingDataDefines.Track("Times_5_Ad", properties);
        }

        /// <summary>
        /// 10次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_10_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            ThinkingDataDefines.Track("Times_10_Ad", properties);
        }

        /// <summary>
        /// 15次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_15_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            ThinkingDataDefines.Track("Times_15_Ad", properties);
        }

        /// <summary>
        /// 20次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        public void Times_20_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            ThinkingDataDefines.Track("Times_20_Ad", properties);
        }

        #endregion

        #region 账号

        /// <summary>
        /// 玩家注册成功
        /// </summary>
        /// <param name="userId">玩家Id</param>
        public void RegisterFinish(string userId)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("#first_check_id", userId);
            properties.Add("userId", userId);
            properties.Add("regtime", DateTime.Now);

            ThinkingDataDefines.Track("Register_Finish", properties);
        }

        /// <summary>
        /// 玩家登录成功
        /// </summary>
        public void LoginSuccess()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            ThinkingDataDefines.Track("Login_Success", properties);
        }

        /// <summary>
        /// 游戏加载成功
        /// </summary>
        public void LoadingStart()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            ThinkingDataDefines.Track("Loading_Start", properties);
        }

        /// <summary>
        /// 游戏进入了主界面
        /// </summary>
        public void EnterMainUI()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            ThinkingDataDefines.Track("Enter_MainUI", properties);
        }

        #endregion

        #region 游戏

        /// <summary>
        /// 按钮点击
        /// </summary>
        /// <param name="btnPath">按钮路径</param>
        public void ButtonClick(string btnPath)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("buttonpath", btnPath);

            ThinkingDataDefines.Track("ButtonClick", properties);
        }

        /// <summary>
        /// 引导步骤
        /// </summary>
        /// <param name="curStep">当前引导步骤</param>
        public void GuideStep(int curStep)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("step", curStep);

            ThinkingDataDefines.Track("GuideStep", properties);
        }

        /// <summary>
        /// 完成了某关
        /// </summary>
        /// <param name="level">关卡</param>
        public void LevelComplate(int level)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("level", level);

            ThinkingDataDefines.Track("LevelComplated", properties);
        }

        #endregion

        #endregion

        #region 用户属性

        #region 注册/登录

        /// <summary>
        /// 设置第一次注册时间
        /// </summary>
        public void SetFirstRegisterTime()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("register_time", DateTime.Now);

            ThinkingDataDefines.UserSetOnce(properties);
        }

        /// <summary>
        /// 设置第一次登录时间
        /// </summary>
        public void SetFirstLoginTime()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("first_login_time", DateTime.Now);

            ThinkingDataDefines.UserSet(properties);
        }

        /// <summary>
        /// 设置最后一次登录时间
        /// </summary>
        public void SetLastLoginTime()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("last_login_time", DateTime.Now);

            ThinkingDataDefines.UserSet(properties);
        }

        #endregion

        #region 游戏

        /// <summary>
        /// 玩家当前金额
        /// </summary>
        /// <param name="money">当前金额</param>
        public void SetUserMoney(float money)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("current_money", money);

            ThinkingDataDefines.UserSet(properties);
        }

        /// <summary>
        /// 设置玩家当前关卡
        /// </summary>
        /// <param name="level">当前关卡</param>
        public void SetLevel(int level)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("current_level", level);

            ThinkingDataDefines.UserSet(properties);
        }

        #endregion

        #region 第三方

        /// <summary>
        /// 获取设置归因信息
        /// </summary>
        /// <param name="data">归因信息</param>
        public void SetAttributionData(Dictionary<string, object> data)
        {
            Dictionary<string, object> BAP = BuildAttributionParameters_Tenjin(data);
            ThinkingDataDefines.UserSetOnce(BAP);
        }

        /// <summary>
        /// 设置玩家广告相关数据
        /// </summary>
        /// <param name="adCount">新增广告次数</param>
        /// <param name="adTime">新增广告时间</param>
        /// <param name="adRevenue">新增广告收入</param>
        public void SetAdInfo(int adCount, float adTime, float adRevenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("total_ad_num", adCount);
            properties.Add("total_ad_time", adTime);
            properties.Add("total_ad_revenue", adRevenue);

            ThinkingDataDefines.UserAdd(properties);
        }

        #endregion

        #endregion

        /// <summary>
        /// 处理归因参数
        /// </summary>
        public Dictionary<string, object> BuildAttributionParameters(Dictionary<string, object> data)
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
                D.Log($" 判断为: 自然用户");
                if (GameDefines.AFJustState)
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.IfIAA, true, 3);
                else
                    CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
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
                D.Log($" 判断为: 导量用户 - 渠道: {mediaSource}, 活动: {campaign}");
                CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
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
                D.Log($"判断为: 未知类型({afStatus})，按自然用户处理");
                CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
            }

            if (data.ContainsKey("af_siteid"))
            {
                parameters["af_siteid"] = data["af_siteid"];
                D.Log($" af_siteid: {data["af_siteid"]}");
            }

            if (data.ContainsKey("adgroup_id"))
            {
                parameters["adgroup_id"] = data["adgroup_id"];
                D.Log($" adgroup_id: {data["adgroup_id"]}");
            }

            if (data.ContainsKey("adset"))
            {
                parameters["adset"] = data["adset"];
                D.Log($" adset: {data["adset"]}");
            }

            D.Log($" 最终用户类型: {parameters["user_type"]}, is_organic: {parameters["is_organic_user"]}");
            return parameters;
        }

        public Dictionary<string, object> BuildAttributionParameters_Tenjin(Dictionary<string, object> data)
        {
            //var parameters = new Dictionary<string, object>();
            var parameters = data;

            if (data.ContainsKey("ad_network") && (string)data["ad_network"] == "organic")
            {
                D.Log($" 判断为: 自然用户");
                if (GameDefines.AFJustState)
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.IfIAA, true, 3);
                else
                    CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
            }
            else
            {
                CompetitionManager.Instance.SkipCompetition(CompetitionKey.IfIAA);
            }

            return parameters;
        }

        /// <summary>
        /// 自动设置注册和登录数据
        /// </summary>
        public void AutoRL()
        {
            SetFirstRegisterTime();
            RegisterFinish(SystemInfo.deviceUniqueIdentifier);
            SetFirstLoginTime();
            SetLastLoginTime();
            LoginSuccess();
        }

        #region 额外扩充

        public void CurTargetsCoins(double targetMoney)
        {
            ThinkingDataDefines.UserSet(new Dictionary<string, object>()
            {
                {"target_mn_sum", targetMoney}
            });
        }

        /// <summary>
        /// only广告强制播放次数
        /// </summary>
        public void OnlyAdCount()
        {
            ThinkingDataDefines.UserAdd(new Dictionary<string, object>()
            {
                {"passiveAdAllCount", 1}
            });
        }

        /// <summary>
        /// only广告强制播放但失败
        /// </summary>
        public void OnlyAdFailedCount()
        {
            ThinkingDataDefines.UserAdd(new Dictionary<string, object>()
            {
                {"passiveAdFailCount", 1}
            });
        }

        /// <summary>
        /// 思考时间埋点
        /// </summary>
        public void OperateDelay()
        {
            if(lastCBTime == (DateTime)default)
            {
                lastCBTime = DateTime.Now;
                return;
            }

            float time = (float)(DateTime.Now - lastCBTime).TotalSeconds;
            lastCBTime = DateTime.Now;

            if (time < 5)
                return;
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("delayTime", Mathf.RoundToInt(time));

            ThinkingDataDefines.Track("OperateDelay", properties);
        }

        #endregion

        public void Dispose()
        {
            
        }
    }
}

