using System;
using System.Collections.Generic;
using UnityEngine;
using XrCode;
using YRTT;

namespace XrSDK
{
    public class YRTModule : BaseModule
    {
        private Dictionary<string, object> AttributionInfo;
        private bool initSuccess;

        public YRTModule(YRTData data)
        {
            
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            AddFacade();

            YRTTSDK.Instance.Init((success, info) =>
            {
                Debug.Log(success ? "SDK 初始化成功" : "SDK 初始化失败");
                initSuccess = success;
                if (success)
                {
                    AttributionInfo = new Dictionary<string, object>() 
                    {
                        {"ServerTimeStamp", info.ServerTimeStamp},
                        {"FromInviteCode", info.FromInviteCode},
                        {"InviteCode", info.InviteCode},
                        {"Home", info.Home},
                        {"Uk", info.Uk},
                        {"ActiveDays", info.ActiveDays},
                        {"RegisterDays", info.RegisterDays},
                        {"channel", info.Channel},
                        {"LastLoginTimeStamp", info.LastLoginTimeStamp},
                        {"Country", info.Country},
                        {"CreateTimeStamp", info.CreateTimeStamp}
                    };

                    //SCompetitionManager.Instance.CompetitionVariable("Attribution", "", 1);
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.YRTT, true, 0);
                    TDAnalyticsManager.Instance.SetAttributionData(AttributionInfo);

                    if (!YRTTSDK.Instance.HadNotifyPermission())
                    {
                        YRTTSDK.Instance.RequestNotifyPermission();
                    }
                }
                else 
                {
                    CompetitionManager.Instance.CompetitionVariable(CompetitionKey.YRTT, false, 0);
                }
            });

            
        }

        #region Facade

        private void AddFacade()
        {
            YRTDefines.GetAttributionInfo += GetAttributionInfo;
            YRTDefines.GetInitSuccess += GetInitSuccess;
        }

        private void RemoveFacade()
        {
            YRTDefines.GetAttributionInfo -= GetAttributionInfo;
            YRTDefines.GetInitSuccess -= GetInitSuccess;
        }

        #endregion

        private Dictionary<string, object> GetAttributionInfo()
        {
            return AttributionInfo;
        }

        private bool GetInitSuccess()
        {
            return initSuccess;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            RemoveFacade();
        }
    }
}

