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
                TDAnalyticsManager.Instance.AutoRL();
            }
        }

        #region 接口

        private void AddFacade()
        {
            ThinkingDataDefines.Track += Track;
            ThinkingDataDefines.UserSet += UserSet;
            ThinkingDataDefines.UserSetOnce += UserSetOnce;
            ThinkingDataDefines.UserAdd += UserAdd;
        }

        private void RemoveFacade()
        {
            ThinkingDataDefines.Track -= Track;
            ThinkingDataDefines.UserSet -= UserSet;
            ThinkingDataDefines.UserSetOnce -= UserSetOnce;
            ThinkingDataDefines.UserAdd -= UserAdd;
        }

        #endregion

        /// <summary>
        /// 埋点
        /// </summary>
        /// <param name="name">埋点名称</param>
        /// <param name="data">埋点数据</param>
        private void Track(string name, Dictionary<string, object> data)
        {
            TDAnalytics.Track(name, data);
        }

        /// <summary>
        /// 设置用户属性
        /// </summary>
        /// <param name="data">用户属性数据</param>
        private void UserSet(Dictionary<string, object> data)
        {
            TDAnalytics.UserSet(data);
        }

        /// <summary>
        /// 仅一次设置用户属性
        /// </summary>
        /// <param name="data">用户属性数据</param>
        private void UserSetOnce(Dictionary<string, object> data)
        {
            TDAnalytics.UserSetOnce(data);
        }

        /// <summary>
        /// 增添用户属性
        /// </summary>
        /// <param name="data">用户属性数据</param>
        private void UserAdd(Dictionary<string, object> data)
        {
            TDAnalytics.UserAdd(data);
        }

        /// <summary>
        /// 自动采集
        /// </summary>
        /// <param name="type">自动采集类型</param>
        private void AutoTrack(TDAutoTrackEventType type)
        {
            TDAnalytics.EnableAutoTrack(type);
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            RemoveFacade();
        }
    }
}