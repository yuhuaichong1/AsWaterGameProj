using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace XrCode
{
    public class TrackingManager : Singleton<TrackingManager>, ILoad, IDispose
    {
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
        private void AdStart(EAdType adtype, EAdSource diasource, double ecpm, string platform, string precision)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("adtype", adtype.ToString());
            properties.Add("diasource", diasource.ToString());
            properties.Add("ecpm", ecpm);
            properties.Add("platform", platform);
            properties.Add("precision", precision);

            FacadeTracking.AdStart(properties);
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

            FacadeTracking.AdComplete?.Invoke(properties);
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

            FacadeTracking.AdFail?.Invoke(properties);
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

            FacadeTracking.AdRevenuePaid?.Invoke(properties);
        }

        /// <summary>
        /// 5次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_5_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            FacadeTracking.Times_5_Ad?.Invoke(properties);
        }

        /// <summary>
        /// 10次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_10_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            FacadeTracking.Times_10_Ad?.Invoke(properties);
        }

        /// <summary>
        /// 15次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_15_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            FacadeTracking.Times_15_Ad?.Invoke(properties);
        }

        /// <summary>
        /// 20次广告收入成功时累计收入
        /// </summary>
        /// <param name="accu_revenue">累计值</param>
        private void Times_20_Ad(double accu_revenue)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("accu_revenue", accu_revenue);

            FacadeTracking.Times_20_Ad?.Invoke(properties);
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

            FacadeTracking.RegisterFinish?.Invoke(properties);
        }

        /// <summary>
        /// 玩家登录成功
        /// </summary>
        private void LoginSuccess()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            FacadeTracking.LoginSuccess?.Invoke(properties);
        }

        /// <summary>
        /// 游戏加载成功
        /// </summary>
        private void LoadingStart()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            FacadeTracking.LoadingStart?.Invoke(properties);
        }

        /// <summary>
        /// 游戏进入了主界面
        /// </summary>
        private void EnterMainUI()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            FacadeTracking.EnterMainUI?.Invoke(properties);
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

            FacadeTracking.ButtonClick?.Invoke(properties);
        }

        /// <summary>
        /// 引导步骤
        /// </summary>
        /// <param name="curStep">当前引导步骤</param>
        private void GuideStep(int curStep)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("step", curStep);

            FacadeTracking.GuideStep?.Invoke(properties);
        }

        #endregion

        #endregion

        #region 用户属性

        #region 注册/登录

        /// <summary>
        /// 设置第一次注册时间
        /// </summary>
        private void SetFirstRegisterTime(DateTime date)
        {
            FacadeTracking.SetFirstRegisterTime?.Invoke(date);
        }

        /// <summary>
        /// 设置第一次登录时间
        /// </summary>
        private void SetFirstLoginTime(DateTime date)
        {
            FacadeTracking.SetFirstLoginTime?.Invoke(date);
        }

        /// <summary>
        /// 设置最后一次登录时间
        /// </summary>
        private void SetLastLoginTime(DateTime date)
        {
            FacadeTracking.SetLastLoginTime?.Invoke(date);
        }

        #endregion

        #region 游戏

        /// <summary>
        /// 设置玩家当前金额
        /// </summary>
        /// <param name="money">当前金额</param>
        private void SetUserMoney(float money)
        {
            FacadeTracking.SetUserMoney?.Invoke(money);
        }

        /// <summary>
        /// 设置玩家当前关卡
        /// </summary>
        /// <param name="level">当前关卡</param>
        private void SetLevel(int level)
        {
            FacadeTracking.SetLevel?.Invoke(level);
        }

        #endregion

        #region 第三方

        /// <summary>
        /// 获取设置归因信息
        /// </summary>
        /// <param name="data">归因信息</param>
        private void SetAttributionData(Dictionary<string, object> data)
        {
            FacadeTracking.SetAttributionData?.Invoke(data);
        }

        /// <summary>
        /// 设置玩家广告相关数据
        /// </summary>
        /// <param name="adCount">新增广告次数</param>
        /// <param name="adTime">新增广告时间</param>
        /// <param name="adRevenue">新增广告收入</param>
        private void SetAdInfo(int adCount, float adTime, float adRevenue)
        {
            FacadeTracking.SetAdInfo?.Invoke(adCount, adTime, adRevenue);
        }

        #endregion

        #endregion

        #region 额外扩充



        #endregion

        public void Dispose()
        {

        }
    }
}
